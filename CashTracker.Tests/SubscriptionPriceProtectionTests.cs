using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Payments;
using CashTracker.Infrastructure.Persistence;
using CashTracker.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CashTracker.Tests;

public sealed class SubscriptionPriceProtectionTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"systemcel_price_notice_{Guid.NewGuid():N}.db");
    private readonly Factory _factory;
    private readonly SubscriptionPriceProtectionService _protection;
    private int _subscriptionId;
    private int _businessId;

    public SubscriptionPriceProtectionTests()
    {
        _factory = new Factory(_dbPath);
        using var db = _factory.CreateDbContext();
        SchemaMigrator.EnsureKasaSchema(db);
        var business = new Isletme { Ad = "Koruma testi", TenantTipi = HesapTipleri.Isletme, IsAktif = true };
        var user = new Kullanici { AuthProviderUserId = "price-user", Eposta = "owner@systemcel.local", AdSoyad = "Owner", Durum = "Aktif" };
        db.AddRange(business, user);
        db.SaveChanges();
        _businessId = business.Id;
        db.IsletmeUyelikleri.Add(new IsletmeUyelik { IsletmeId = business.Id, KullaniciId = user.Id, Rol = "isletme_sahibi", Durum = "Aktif", DavetEposta = user.Eposta });
        var subscription = new Abonelik
        {
            IsletmeId = business.Id,
            HesapTipi = HesapTipleri.Isletme,
            PlanKodu = PlanKodlari.IsletmeBaslangic,
            Durum = "Aktif",
            DonemTutari = 690m,
            ParaBirimi = "TRY",
            DonemBaslangicAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            DonemBitisAt = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        db.Abonelikler.Add(subscription);
        db.SaveChanges();
        _subscriptionId = subscription.Id;
        _protection = new SubscriptionPriceProtectionService(_factory);
    }

    [Fact]
    public async Task IncreasedRenewal_IsBlockedUntilBothChannelsHaveTimelyDeliveryProof()
    {
        var noticeAt = new DateTime(2026, 11, 30, 0, 0, 0, DateTimeKind.Utc);
        var renewalAt = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var evidenceId = await _protection.SchedulePriceIncreaseAsync(new(
            _subscriptionId, "price-user", "owner@systemcel.local", 790m, renewalAt,
            "price-v1", "Yeni dönem net tutarı 790 TRY olacaktır."), noticeAt);

        var beforeDelivery = await _protection.EvaluateRenewalAsync(_subscriptionId, 790m, renewalAt);
        Assert.False(beforeDelivery.CanCharge);
        Assert.Equal(690m, beforeDelivery.AllowedNetAmount);

        await using (var db = _factory.CreateDbContext())
        {
            var proof = await db.AbonelikFiyatBildirimKanitlari.SingleAsync(x => x.Id == evidenceId);
            var outbox = await db.BildirimTeslimOutboxlari.Where(x => x.Id == proof.UygulamaOutboxId || x.Id == proof.EpostaOutboxId).ToListAsync();
            foreach (var row in outbox)
            {
                row.Durum = BildirimTeslimDurumlari.TeslimEdildi;
                row.TeslimEdildiAt = noticeAt.AddMinutes(1);
            }
            await db.SaveChangesAsync();
        }

        var allowed = await _protection.EvaluateRenewalAsync(_subscriptionId, 790m, renewalAt);
        Assert.True(allowed.CanCharge);
        Assert.Equal(790m, allowed.AllowedNetAmount);
        Assert.Equal(evidenceId, allowed.EvidenceId);
    }

    [Fact]
    public async Task LateNoticeAndPeriodEndCancellation_BlockIncreasedCharge()
    {
        var renewalAt = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        await _protection.SchedulePriceIncreaseAsync(new(
            _subscriptionId, "price-user", "owner@systemcel.local", 790m, renewalAt,
            "price-v1", "Yeni fiyat."), renewalAt.AddDays(-29));
        await MarkAllDeliveredAsync(renewalAt.AddDays(-29));

        var late = await _protection.EvaluateRenewalAsync(_subscriptionId, 790m, renewalAt);
        Assert.False(late.CanCharge);
        Assert.Equal(690m, late.AllowedNetAmount);

        await using (var db = _factory.CreateDbContext())
        {
            var subscription = await db.Abonelikler.SingleAsync(x => x.Id == _subscriptionId);
            subscription.DonemSonundaIptal = true;
            await db.SaveChangesAsync();
        }
        var cancelled = await _protection.EvaluateRenewalAsync(_subscriptionId, 690m, renewalAt);
        Assert.False(cancelled.CanCharge);
        Assert.Equal(0m, cancelled.AllowedNetAmount);
    }

    [Fact]
    public async Task NoticeCreatedEarlyButDeliveredTwentyNineDaysBeforeRenewal_IsBlocked()
    {
        var renewalAt = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        await _protection.SchedulePriceIncreaseAsync(new(
            _subscriptionId, "price-user", "owner@systemcel.local", 790m, renewalAt,
            "price-v1", "Yeni fiyat."), renewalAt.AddDays(-40));
        await MarkAllDeliveredAsync(renewalAt.AddDays(-29));

        var decision = await _protection.EvaluateRenewalAsync(_subscriptionId, 790m, renewalAt);
        Assert.False(decision.CanCharge);
        Assert.Equal(690m, decision.AllowedNetAmount);
        Assert.Contains("teslim kanıtı", decision.Reason);
    }

    [Fact]
    public async Task DuplicateNotice_ReusesImmutableSnapshotAndOutboxPair()
    {
        var now = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        var notice = new SubscriptionPriceIncreaseNotice(_subscriptionId, "price-user", "owner@systemcel.local", 790m, now.AddDays(61), "price-v1", "Yeni fiyat.");
        var first = await _protection.SchedulePriceIncreaseAsync(notice, now);
        var second = await _protection.SchedulePriceIncreaseAsync(notice, now.AddMinutes(1));

        Assert.Equal(first, second);
        await using var db = _factory.CreateDbContext();
        Assert.Equal(1, await db.AbonelikFiyatBildirimKanitlari.CountAsync());
        Assert.Equal(2, await db.BildirimTeslimOutboxlari.CountAsync());
    }

    [Fact]
    public async Task NoticeRecipient_MustMatchActiveTenantMemberEmail()
    {
        var effectiveAt = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var wrongEmail = new SubscriptionPriceIncreaseNotice(
            _subscriptionId, "price-user", "other@systemcel.local", 790m, effectiveAt, "price-v1", "Yeni fiyat.");
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _protection.SchedulePriceIncreaseAsync(wrongEmail, effectiveAt.AddDays(-40)));

        await using (var db = _factory.CreateDbContext())
        {
            var user = await db.Kullanicilar.SingleAsync(x => x.AuthProviderUserId == "price-user");
            user.Durum = "Pasif";
            await db.SaveChangesAsync();
        }
        var inactive = wrongEmail with { RecipientEmail = "owner@systemcel.local" };
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _protection.SchedulePriceIncreaseAsync(inactive, effectiveAt.AddDays(-40)));
    }

    [Fact]
    public async Task RenewalCheckout_UsesOldPriceWhenNoticeConditionsAreMissing()
    {
        await using (var db = _factory.CreateDbContext())
        {
            var subscription = await db.Abonelikler.SingleAsync(x => x.Id == _subscriptionId);
            subscription.DonemTutari = 590m;
            subscription.DonemBitisAt = DateTime.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        }

        var lifecycle = new SubscriptionLifecycleService(
            _factory,
            new FakePaymentProvider("price-protection-secret"),
            new PaymentPricingService(),
            priceProtection: _protection);
        var result = await lifecycle.BeginCheckoutAsync(new SubscriptionCheckoutCommand(
            _businessId,
            HesapTipleri.Isletme,
            PlanKodlari.IsletmeBaslangic,
            PaymentBillingPeriods.Monthly,
            0,
            string.Empty,
            "renewal-protected-1",
            "price-user",
            "owner@systemcel.local",
            "renewal-v1",
            "Aylık abonelik yenilemesini onaylıyorum.",
            "127.0.0.1",
            "SystemcelTests/1.0",
            new Uri("https://systemcel.local/success"),
            new Uri("https://systemcel.local/failure"),
            new Uri("https://systemcel.local/webhook")));

        Assert.Equal(590m, result.Quote.NetAmount);
        Assert.Equal(708m, result.Quote.TotalAmount);
        await using var verified = _factory.CreateDbContext();
        Assert.Equal(590m, (await verified.OdemeIslemleri.SingleAsync()).NetTutar);
    }

    [Fact]
    public async Task AnnualRenewalCheckout_UsesOldAnnualPriceWithoutTimelyProof()
    {
        await using (var db = _factory.CreateDbContext())
        {
            var subscription = await db.Abonelikler.SingleAsync(x => x.Id == _subscriptionId);
            subscription.FaturalamaDonemi = PaymentBillingPeriods.Annual;
            subscription.DonemTutari = 6000m;
            subscription.DonemBaslangicAt = DateTime.UtcNow.AddYears(-1).AddMinutes(-1);
            subscription.DonemBitisAt = DateTime.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        }

        var lifecycle = new SubscriptionLifecycleService(
            _factory,
            new FakePaymentProvider("price-protection-secret"),
            new PaymentPricingService(),
            priceProtection: _protection);
        var result = await lifecycle.BeginCheckoutAsync(new SubscriptionCheckoutCommand(
            _businessId, HesapTipleri.Isletme, PlanKodlari.IsletmeBaslangic,
            PaymentBillingPeriods.Annual, 0, string.Empty, "annual-renewal-protected-1",
            "price-user", "owner@systemcel.local", "renewal-v1",
            "Yıllık abonelik yenilemesini onaylıyorum.", "127.0.0.1", "SystemcelTests/1.0",
            new Uri("https://systemcel.local/success"), new Uri("https://systemcel.local/failure"),
            new Uri("https://systemcel.local/webhook")));

        Assert.Equal(6000m, result.Quote.NetAmount);
        Assert.Equal(7200m, result.Quote.TotalAmount);
        await using var verified = _factory.CreateDbContext();
        Assert.Equal(6000m, (await verified.OdemeIslemleri.SingleAsync()).NetTutar);
    }

    [Fact]
    public async Task ReconcileThenRenewalCheckout_StillUsesOldPriceWithoutTimelyProof()
    {
        var now = DateTime.UtcNow;
        await using (var db = _factory.CreateDbContext())
        {
            var subscription = await db.Abonelikler.SingleAsync(x => x.Id == _subscriptionId);
            subscription.FaturalamaDonemi = PaymentBillingPeriods.Monthly;
            subscription.DonemTutari = 590m;
            subscription.DonemBaslangicAt = now.AddMonths(-1).AddMinutes(-2);
            subscription.DonemBitisAt = now.AddMinutes(-2);
            await db.SaveChangesAsync();
        }

        var lifecycle = new SubscriptionLifecycleService(
            _factory,
            new FakePaymentProvider("price-protection-secret"),
            new PaymentPricingService(),
            priceProtection: _protection);
        var reconciliation = await lifecycle.ReconcileAsync(now);
        Assert.Equal(1, reconciliation.ExpiredSubscriptions);
        await using (var reconciled = _factory.CreateDbContext())
            Assert.Equal("SonaErdi", (await reconciled.Abonelikler.SingleAsync(x => x.Id == _subscriptionId)).Durum);

        var result = await lifecycle.BeginCheckoutAsync(new SubscriptionCheckoutCommand(
            _businessId, HesapTipleri.Isletme, PlanKodlari.IsletmeBaslangic,
            PaymentBillingPeriods.Monthly, 0, string.Empty, "reconciled-renewal-protected-1",
            "price-user", "owner@systemcel.local", "renewal-v1",
            "Aylık abonelik yenilemesini onaylıyorum.", "127.0.0.1", "SystemcelTests/1.0",
            new Uri("https://systemcel.local/success"), new Uri("https://systemcel.local/failure"),
            new Uri("https://systemcel.local/webhook")));

        Assert.Equal(590m, result.Quote.NetAmount);
        Assert.Equal(708m, result.Quote.TotalAmount);
        await using var verified = _factory.CreateDbContext();
        Assert.Equal(590m, (await verified.OdemeIslemleri.SingleAsync()).NetTutar);
        Assert.Empty(await verified.KurucuKampanyaHaklari.ToListAsync());
    }

    [Fact]
    public async Task ExpiredDifferentPlan_IsNotTreatedAsProtectedSamePlanRenewal()
    {
        var now = DateTime.UtcNow;
        await using (var db = _factory.CreateDbContext())
        {
            var subscription = await db.Abonelikler.SingleAsync(x => x.Id == _subscriptionId);
            subscription.DonemTutari = 590m;
            subscription.DonemBitisAt = now.AddMinutes(-1);
            subscription.Durum = "SonaErdi";
            await db.SaveChangesAsync();
        }

        var lifecycle = new SubscriptionLifecycleService(
            _factory, new FakePaymentProvider("price-protection-secret"), new PaymentPricingService(),
            priceProtection: _protection);
        var result = await lifecycle.BeginCheckoutAsync(new SubscriptionCheckoutCommand(
            _businessId, HesapTipleri.Isletme, PlanKodlari.IsletmeBuyume,
            PaymentBillingPeriods.Monthly, 0, string.Empty, "expired-new-plan-1",
            "price-user", "owner@systemcel.local", "plan-change-v1", "Yeni planı onaylıyorum.",
            "127.0.0.1", "SystemcelTests/1.0", new Uri("https://systemcel.local/success"),
            new Uri("https://systemcel.local/failure"), new Uri("https://systemcel.local/webhook")));

        Assert.Equal(1290m, result.Quote.NetAmount);
        Assert.Equal(1548m, result.Quote.TotalAmount);
    }

    private async Task MarkAllDeliveredAsync(DateTime deliveredAt)
    {
        await using var db = _factory.CreateDbContext();
        foreach (var row in await db.BildirimTeslimOutboxlari.ToListAsync())
        {
            row.Durum = BildirimTeslimDurumlari.TeslimEdildi;
            row.TeslimEdildiAt = deliveredAt;
        }
        await db.SaveChangesAsync();
    }

    public void Dispose()
    {
        try { if (File.Exists(_dbPath)) File.Delete(_dbPath); } catch { }
    }

    private sealed class Factory : IDbContextFactory<CashTrackerDbContext>
    {
        private readonly DbContextOptions<CashTrackerDbContext> _options;
        public Factory(string path) => _options = new DbContextOptionsBuilder<CashTrackerDbContext>().UseSqlite($"Data Source={path}").Options;
        public CashTrackerDbContext CreateDbContext() => new(_options);
        public Task<CashTrackerDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(CreateDbContext());
    }
}
