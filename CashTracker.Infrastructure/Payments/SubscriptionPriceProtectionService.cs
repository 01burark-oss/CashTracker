using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CashTracker.Infrastructure.Payments;

public sealed class SubscriptionPriceProtectionService : ISubscriptionPriceProtectionService
{
    public static readonly TimeSpan MinimumNotice = TimeSpan.FromDays(30);
    private readonly IDbContextFactory<CashTrackerDbContext> _dbFactory;

    public SubscriptionPriceProtectionService(IDbContextFactory<CashTrackerDbContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<long> SchedulePriceIncreaseAsync(
        SubscriptionPriceIncreaseNotice notice,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        var now = EnsureUtc(nowUtc);
        var effectiveAt = EnsureUtc(notice.EffectiveAt);
        if (notice.SubscriptionId <= 0) throw new ArgumentOutOfRangeException(nameof(notice.SubscriptionId));
        if (notice.NewNetAmount <= 0) throw new ArgumentOutOfRangeException(nameof(notice.NewNetAmount));
        if (string.IsNullOrWhiteSpace(notice.UserReference) || string.IsNullOrWhiteSpace(notice.RecipientEmail))
            throw new ArgumentException("Fiyat bildirimi için kullanıcı ve e-posta zorunludur.");
        if (string.IsNullOrWhiteSpace(notice.TextVersion) || string.IsNullOrWhiteSpace(notice.Text))
            throw new ArgumentException("Fiyat bildirimi metni ve sürümü zorunludur.");

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var subscription = await db.Abonelikler.SingleOrDefaultAsync(x => x.Id == notice.SubscriptionId, ct)
            ?? throw new InvalidOperationException("Abonelik bulunamadı.");
        if (subscription.DonemBitisAt is not { } periodEnd)
            throw new InvalidOperationException("Abonelik dönem sonu bulunamadı.");
        if (notice.NewNetAmount <= subscription.DonemTutari)
            throw new InvalidOperationException("Yeni tutar mevcut dönem tutarından yüksek değil.");
        var isMember = await (
            from user in db.Kullanicilar.AsNoTracking()
            join membership in db.IsletmeUyelikleri.AsNoTracking() on user.Id equals membership.KullaniciId
            where user.AuthProviderUserId == notice.UserReference.Trim() &&
                  user.Eposta == notice.RecipientEmail.Trim() &&
                  user.Durum == "Aktif" && membership.IsletmeId == subscription.IsletmeId && membership.Durum == "Aktif"
            select user.Id).AnyAsync(ct);
        if (!isMember)
            throw new InvalidOperationException("Fiyat bildirimi alıcısı aktif işletme üyeliğiyle doğrulanamadı.");

        var existing = await db.AbonelikFiyatBildirimKanitlari.AsNoTracking().SingleOrDefaultAsync(x =>
            x.AbonelikId == notice.SubscriptionId && x.YururlukAt == effectiveAt && x.YeniNetTutar == notice.NewNetAmount, ct);
        if (existing is not null) return existing.Id;

        var subject = "Systemcel abonelik fiyatı değişikliği";
        var messageText = notice.Text.Trim().Length <= 1000 ? notice.Text.Trim() : notice.Text.Trim()[..1000];
        var payload = JsonSerializer.Serialize(new { Baslik = subject, Mesaj = messageText, Url = "/app/abonelik", AliciEposta = notice.RecipientEmail.Trim() });
        var notification = new BildirimKaydi
        {
            IsletmeId = subscription.IsletmeId,
            KullaniciRef = notice.UserReference.Trim(),
            KaynakAnahtari = $"subscription-price:{subscription.Id}:{effectiveAt:yyyyMMddHHmmss}:{notice.NewNetAmount}",
            Tur = "abonelik",
            Onem = "yuksek",
            Baslik = subject,
            Mesaj = messageText,
            Aksiyon = "Aboneliği incele",
            Url = "/app/abonelik",
            CreatedAt = now,
            UpdatedAt = now
        };
        var proof = new AbonelikFiyatBildirimKaniti
        {
            AbonelikId = subscription.Id,
            IsletmeId = subscription.IsletmeId,
            KullaniciRef = notice.UserReference.Trim(),
            AliciEposta = notice.RecipientEmail.Trim(),
            EskiNetTutar = subscription.DonemTutari,
            YeniNetTutar = notice.NewNetAmount,
            ParaBirimi = subscription.ParaBirimi,
            DonemBaslangicAt = EnsureUtc(subscription.DonemBaslangicAt),
            DonemBitisAt = EnsureUtc(periodEnd),
            YururlukAt = effectiveAt,
            MetinSurumu = notice.TextVersion.Trim(),
            MetinHash = Sha256(notice.Text),
            BildirimOlusturulduAt = now,
            CreatedAt = now
        };
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        db.BildirimKayitlari.Add(notification);
        db.AbonelikFiyatBildirimKanitlari.Add(proof);
        await db.SaveChangesAsync(ct);
        var appOutbox = CreateOutbox(proof, notification.Id, BildirimKanallari.Uygulama, payload, now, effectiveAt);
        var emailOutbox = CreateOutbox(proof, notification.Id, BildirimKanallari.Eposta, payload, now, effectiveAt);
        db.BildirimTeslimOutboxlari.AddRange(appOutbox, emailOutbox);
        await db.SaveChangesAsync(ct);
        proof.UygulamaOutboxId = appOutbox.Id;
        proof.EpostaOutboxId = emailOutbox.Id;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return proof.Id;
    }

    public async Task<SubscriptionPriceProtectionDecision> EvaluateRenewalAsync(
        int subscriptionId,
        decimal proposedNetAmount,
        DateTime renewalAtUtc,
        CancellationToken ct = default)
    {
        var renewalAt = EnsureUtc(renewalAtUtc);
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var subscription = await db.Abonelikler.AsNoTracking().SingleOrDefaultAsync(x => x.Id == subscriptionId, ct)
            ?? throw new InvalidOperationException("Abonelik bulunamadı.");
        if (subscription.DonemSonundaIptal)
            return new(false, 0m, "Abonelik dönem sonunda iptal edilecek.");
        if (proposedNetAmount <= subscription.DonemTutari)
            return new(true, proposedNetAmount, "Fiyat artışı yok.");

        var proof = await db.AbonelikFiyatBildirimKanitlari.AsNoTracking()
            .Where(x => x.AbonelikId == subscriptionId && x.EskiNetTutar == subscription.DonemTutari &&
                        x.YeniNetTutar == proposedNetAmount && x.YururlukAt <= renewalAt &&
                        x.BildirimOlusturulduAt <= renewalAt.Subtract(MinimumNotice))
            .OrderByDescending(x => x.BildirimOlusturulduAt)
            .FirstOrDefaultAsync(ct);
        if (proof is null)
            return new(false, subscription.DonemTutari, "Fiyat artışı en az 30 gün önce bildirilmedi.");

        var delivered = await db.BildirimTeslimOutboxlari.AsNoTracking()
            .Where(x => x.Id == proof.UygulamaOutboxId || x.Id == proof.EpostaOutboxId)
            .Select(x => new { x.Id, x.Durum, x.TeslimEdildiAt })
            .ToListAsync(ct);
        var deliveryDeadline = renewalAt.Subtract(MinimumNotice);
        var appDelivered = delivered.Any(x => x.Id == proof.UygulamaOutboxId && x.Durum == BildirimTeslimDurumlari.TeslimEdildi &&
                                              x.TeslimEdildiAt >= proof.BildirimOlusturulduAt && x.TeslimEdildiAt <= deliveryDeadline);
        var emailDelivered = delivered.Any(x => x.Id == proof.EpostaOutboxId && x.Durum == BildirimTeslimDurumlari.TeslimEdildi &&
                                                x.TeslimEdildiAt >= proof.BildirimOlusturulduAt && x.TeslimEdildiAt <= deliveryDeadline);
        return appDelivered && emailDelivered
            ? new(true, proposedNetAmount, "Fiyat artışı bildirimi zamanında iki kanaldan teslim edildi.", proof.Id)
            : new(false, subscription.DonemTutari, "E-posta ve uygulama içi teslim kanıtı tamamlanmadı.", proof.Id);
    }

    private static BildirimTeslimOutbox CreateOutbox(
        AbonelikFiyatBildirimKaniti proof,
        int notificationId,
        string channel,
        string payload,
        DateTime now,
        DateTime effectiveAt) => new()
    {
        IsletmeId = proof.IsletmeId,
        KullaniciRef = proof.KullaniciRef,
        BildirimId = notificationId,
        Kanal = channel,
        IdempotencyAnahtari = Sha256($"price:{proof.AbonelikId}:{effectiveAt:O}:{proof.YeniNetTutar}:{channel}"),
        PayloadJson = payload,
        SonrakiDenemeAt = now,
        CreatedAt = now,
        UpdatedAt = now
    };

    private static string Sha256(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private static DateTime EnsureUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
