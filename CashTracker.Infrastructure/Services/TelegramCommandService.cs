using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;

namespace CashTracker.Infrastructure.Services
{
    public sealed class TelegramCommandService
    {
        private readonly TelegramBotService _telegram;
        private readonly TelegramSettings _settings;
        private readonly IKasaService _kasaService;
        private readonly IKalemTanimiService _kalemTanimiService;
        private readonly ISummaryService _summaryService;
        private readonly BackupReportService _backupReport;

        public TelegramCommandService(
            TelegramBotService telegram,
            TelegramSettings settings,
            IKasaService kasaService,
            IKalemTanimiService kalemTanimiService,
            ISummaryService summaryService,
            BackupReportService backupReport)
        {
            _telegram = telegram;
            _settings = settings;
            _kasaService = kasaService;
            _kalemTanimiService = kalemTanimiService;
            _summaryService = summaryService;
            _backupReport = backupReport;
        }

        public async Task ProcessUpdateAsync(TelegramUpdate update, CancellationToken ct = default)
        {
            if (!_settings.IsEnabled || !_settings.EnableCommands)
                return;

            if (!_settings.IsTargetChat(update.ChatId))
                return;

            if (!_settings.IsAllowedUser(update.UserId))
            {
                await _telegram.SendTextAsync(ToChatId(update.ChatId), "Bu kullan\u0131c\u0131 yetkili de\u011Fil.", ct);
                return;
            }

            var text = update.Text?.Trim();
            if (string.IsNullOrWhiteSpace(text) || !text.StartsWith('/'))
                return;

            if (!TryParseCommand(text, out var command, out var args))
                return;

            try
            {
                switch (command)
                {
                    case "/start":
                    case "/help":
                    case "/yardim":
                    case "/yard\u0131m":
                        await SendHelpAsync(update.ChatId, ct);
                        break;

                    case "/today":
                    case "/bugun":
                    case "/bug\u00FCn":
                        await _backupReport.SendDailyReportAsync(DateTime.Today, "Telegram komutu");
                        break;

                    case "/summary":
                    case "/ozet":
                    case "/\u00F6zet":
                        await SendSummaryAsync(args, update.ChatId, ct);
                        break;

                    case "/report":
                    case "/rapor":
                        await SendReadableReportAsync(args, update.ChatId, ct);
                        break;

                    case "/backup":
                    case "/yedek":
                        await _telegram.SendTextAsync(ToChatId(update.ChatId), "Yedek alınıyor, lütfen bekleyin.", ct);
                        await _backupReport.SendBackupAsync("Telegram komutu");
                        break;

                    case "/add":
                    case "/ekle":
                        await AddTransactionWithTypeAsync(args, update.ChatId, ct);
                        break;

                    case "/gelir":
                        await AddTransactionAsync("Gelir", args, update.ChatId, ct);
                        break;

                    case "/gider":
                        await AddTransactionAsync("Gider", args, update.ChatId, ct);
                        break;

                    default:
                        await _telegram.SendTextAsync(
                            ToChatId(update.ChatId),
                            "Bilinmeyen komut. Komutlar\u0131 g\u00F6rmek i\u00E7in /yard\u0131m yaz.",
                            ct);
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"TelegramCommandService command error: {ex}");
                await _telegram.SendTextAsync(
                    ToChatId(update.ChatId),
                    "Komut i\u015Flenirken bir hata olu\u015Ftu. L\u00FCtfen tekrar deneyin.",
                    ct);
            }
        }

        private static bool TryParseCommand(string text, out string command, out string[] args)
        {
            command = string.Empty;
            args = Array.Empty<string>();

            var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return false;

            var commandToken = parts[0];
            var argStart = 1;

            // Tolerate accidental "/ backup" style.
            if (commandToken == "/" && parts.Length > 1)
            {
                commandToken = "/" + parts[1];
                argStart = 2;
            }

            command = commandToken.Split('@')[0].Trim().ToLowerInvariant();
            args = parts.Skip(argStart).ToArray();

            return command.StartsWith('/');
        }

        private async Task SendHelpAsync(long chatId, CancellationToken ct)
        {
            var help =
                "Komutlar:\n" +
                "/yard\u0131m - Komut listesi\n" +
                "/bug\u00FCn - Bug\u00FCn\u00FCn raporu\n" +
                "/\u00F6zet [g\u00FCn] - Son N g\u00FCn \u00F6zeti (varsay\u0131lan 30)\n" +
                "/rapor [g\u00FCn] - \u0130nsan okunur TXT rapor (varsay\u0131lan 30)\n" +
                "/yedek - Veritaban\u0131 yede\u011Fi g\u00F6nder\n" +
                "/ekle gelir <tutar> [kalem] [a\u00E7\u0131klama]\n" +
                "/ekle gider <tutar> <kalem> [a\u00E7\u0131klama]\n" +
                "/gelir <tutar> [kalem] [a\u00E7\u0131klama]\n" +
                "/gider <tutar> <kalem> [a\u00E7\u0131klama]";

            await _telegram.SendTextAsync(ToChatId(chatId), help, ct);
        }

        private async Task SendSummaryAsync(string[] args, long chatId, CancellationToken ct)
        {
            var days = 30;
            if (args.Length > 0)
            {
                if (!int.TryParse(args[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedDays))
                {
                    await _telegram.SendTextAsync(ToChatId(chatId), "Kullan\u0131m: /\u00F6zet [g\u00FCn]", ct);
                    return;
                }

                days = Math.Clamp(parsedDays, 1, 3650);
            }

            var to = DateTime.Today;
            var from = to.AddDays(-(days - 1));
            var summary = await _summaryService.GetSummaryAsync(from, to);

            var text =
                $"\u00D6zet ({from:yyyy-MM-dd} - {to:yyyy-MM-dd})\n" +
                $"Gelir: {summary.IncomeTotal:n2}\n" +
                $"Gider: {summary.ExpenseTotal:n2}\n" +
                $"Net: {summary.Net:n2}\n" +
                $"\u0130\u015Flem: {summary.IncomeCount + summary.ExpenseCount} (Gelir {summary.IncomeCount}, Gider {summary.ExpenseCount})";

            await _telegram.SendTextAsync(ToChatId(chatId), text, ct);
        }

        private async Task SendReadableReportAsync(string[] args, long chatId, CancellationToken ct)
        {
            var days = 30;
            if (args.Length > 0)
            {
                if (!int.TryParse(args[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedDays))
                {
                    await _telegram.SendTextAsync(ToChatId(chatId), "Kullan\u0131m: /rapor [g\u00FCn]", ct);
                    return;
                }

                days = Math.Clamp(parsedDays, 1, 3650);
            }

            var to = DateTime.Today;
            var from = to.AddDays(-(days - 1));
            var summary = await _summaryService.GetSummaryAsync(from, to);
            var records = await _kasaService.GetAllAsync(from, to);

            var sb = new StringBuilder();
            sb.AppendLine("CashTracker Rapor");
            sb.AppendLine($"Aral\u0131k: {from:yyyy-MM-dd} - {to:yyyy-MM-dd}");
            sb.AppendLine();
            sb.AppendLine($"Gelir Toplam: {summary.IncomeTotal:n2}");
            sb.AppendLine($"Gider Toplam: {summary.ExpenseTotal:n2}");
            sb.AppendLine($"Net: {summary.Net:n2}");
            sb.AppendLine($"\u0130\u015Flem Say\u0131s\u0131: {summary.IncomeCount + summary.ExpenseCount}");
            sb.AppendLine();
            sb.AppendLine("Hareketler:");

            if (records.Count == 0)
            {
                sb.AppendLine("- Bu aral\u0131kta hareket yok.");
            }
            else
            {
                foreach (var row in records.OrderBy(x => x.Tarih))
                {
                    var kalem = !string.IsNullOrWhiteSpace(row.Kalem)
                        ? row.Kalem
                        : (string.IsNullOrWhiteSpace(row.GiderTuru) ? "-" : row.GiderTuru);
                    var aciklama = string.IsNullOrWhiteSpace(row.Aciklama) ? "-" : row.Aciklama;
                    sb.AppendLine($"{row.Tarih:yyyy-MM-dd HH:mm} | {row.Tip} | {row.Tutar:n2} | {kalem} | {aciklama}");
                }
            }

            var reportDir = Path.Combine(Path.GetTempPath(), "CashTrackerReports");
            Directory.CreateDirectory(reportDir);

            var reportPath = Path.Combine(reportDir, $"cashtracker_rapor_{DateTime.Now:yyyyMMdd_HHmmss_fff}.txt");

            await File.WriteAllTextAsync(reportPath, sb.ToString(), Encoding.UTF8, ct);

            try
            {
                var caption = $"Rapor ({from:yyyy-MM-dd} - {to:yyyy-MM-dd})";
                await _telegram.SendDocumentAsync(ToChatId(chatId), reportPath, caption, ct);
            }
            finally
            {
                try
                {
                    if (File.Exists(reportPath))
                        File.Delete(reportPath);
                }
                catch
                {
                }
            }
        }

        private async Task AddTransactionWithTypeAsync(string[] args, long chatId, CancellationToken ct)
        {
            if (args.Length < 2)
            {
                await _telegram.SendTextAsync(ToChatId(chatId), "Kullanim: /ekle gelir|gider <tutar> [kalem] [aciklama]", ct);
                return;
            }

            var tip = NormalizeTip(args[0]);
            if (tip is null)
            {
                await _telegram.SendTextAsync(ToChatId(chatId), "Tip sadece gelir veya gider olabilir.", ct);
                return;
            }

            await AddTransactionAsync(tip, args.Skip(1).ToArray(), chatId, ct);
        }

        private async Task AddTransactionAsync(string tip, string[] args, long chatId, CancellationToken ct)
        {
            if (args.Length < 1)
            {
                if (tip == "Gider")
                    await _telegram.SendTextAsync(ToChatId(chatId), "Kullanim: /gider <tutar> <kalem> [aciklama]", ct);
                else
                    await _telegram.SendTextAsync(ToChatId(chatId), "Kullanim: /gelir <tutar> [kalem] [aciklama]", ct);
                return;
            }

            if (!TryParseAmount(args[0], out var amount) || amount <= 0)
            {
                await _telegram.SendTextAsync(ToChatId(chatId), "Tutar sayisal ve sifirdan buyuk olmali.", ct);
                return;
            }

            var remaining = args.Skip(1).ToArray();
            var kalemler = await _kalemTanimiService.GetByTipAsync(tip);
            var kalemParse = ParseKalemAndAciklama(tip, remaining, kalemler);

            if (tip == "Gider" && string.IsNullOrWhiteSpace(kalemParse.Kalem))
            {
                await _telegram.SendTextAsync(ToChatId(chatId), "Gider icin kalem zorunlu. Ornek: /gider 150 market", ct);
                return;
            }

            var kasa = new Kasa
            {
                Tarih = DateTime.Now,
                Tip = tip,
                Tutar = amount,
                Kalem = kalemParse.Kalem,
                GiderTuru = tip == "Gider" ? kalemParse.Kalem : null,
                Aciklama = kalemParse.Aciklama
            };

            var id = await _kasaService.CreateAsync(kasa);
            var kalemText = string.IsNullOrWhiteSpace(kalemParse.Kalem)
                ? GetDefaultKalem(tip, kalemler)
                : kalemParse.Kalem;

            await _telegram.SendTextAsync(
                ToChatId(chatId),
                $"Kaydedildi. Id: {id}\nTip: {tip}\nKalem: {kalemText}\nTutar: {amount:n2}",
                ct);
        }

        private static (string Kalem, string? Aciklama) ParseKalemAndAciklama(
            string tip,
            string[] remainingArgs,
            IReadOnlyList<KalemTanimi> kalemler)
        {
            if (remainingArgs.Length == 0)
                return (GetDefaultKalem(tip, kalemler), null);

            if (TryMatchKalemPrefix(kalemler, remainingArgs, out var matchedKalem, out var consumedTokenCount))
            {
                var aciklama = remainingArgs.Length > consumedTokenCount
                    ? string.Join(' ', remainingArgs.Skip(consumedTokenCount))
                    : null;
                return (matchedKalem, aciklama);
            }

            if (tip == "Gider")
            {
                var fallbackKalem = remainingArgs[0].Trim();
                var aciklama = remainingArgs.Length > 1
                    ? string.Join(' ', remainingArgs.Skip(1))
                    : null;
                return (fallbackKalem, aciklama);
            }

            var gelirAciklama = string.Join(' ', remainingArgs);
            return (GetDefaultKalem(tip, kalemler), string.IsNullOrWhiteSpace(gelirAciklama) ? null : gelirAciklama);
        }

        private static bool TryMatchKalemPrefix(
            IReadOnlyList<KalemTanimi> kalemler,
            string[] remainingArgs,
            out string kalem,
            out int consumedTokenCount)
        {
            kalem = string.Empty;
            consumedTokenCount = 0;

            if (remainingArgs.Length == 0 || kalemler.Count == 0)
                return false;

            KalemTanimi? bestMatch = null;
            var joinedArgs = string.Join(' ', remainingArgs);
            var joinedArgsNormalized = NormalizeForMatch(joinedArgs);

            foreach (var row in kalemler)
            {
                var ad = row.Ad?.Trim();
                if (string.IsNullOrWhiteSpace(ad))
                    continue;

                var normalizedAd = NormalizeForMatch(ad);
                var isExact = string.Equals(joinedArgsNormalized, normalizedAd, StringComparison.Ordinal);
                var isPrefix = joinedArgsNormalized.StartsWith(normalizedAd + " ", StringComparison.Ordinal);
                if (!isExact && !isPrefix)
                    continue;

                if (bestMatch == null || ad.Length > bestMatch.Ad.Length)
                    bestMatch = row;
            }

            if (bestMatch == null)
                return false;

            kalem = bestMatch.Ad;
            consumedTokenCount = bestMatch.Ad
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Length;
            return true;
        }

        private static string GetDefaultKalem(string tip, IReadOnlyList<KalemTanimi> kalemler)
        {
            var preferred = tip == "Gider" ? "Genel Gider" : "Genel Gelir";
            if (kalemler.Count == 0)
                return preferred;

            foreach (var row in kalemler)
            {
                if (string.Equals(row.Ad, preferred, StringComparison.OrdinalIgnoreCase))
                    return row.Ad;
            }

            return kalemler[0].Ad;
        }

        private static string NormalizeForMatch(string value)
        {
            var normalized = (value ?? string.Empty)
                .Trim()
                .ToLowerInvariant()
                .Replace('ı', 'i')
                .Replace('ş', 's')
                .Replace('ğ', 'g')
                .Replace('ü', 'u')
                .Replace('ö', 'o')
                .Replace('ç', 'c');

            var builder = new StringBuilder(normalized.Length);
            foreach (var ch in normalized.Normalize(NormalizationForm.FormD))
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (category != UnicodeCategory.NonSpacingMark)
                    builder.Append(ch);
            }

            return builder.ToString().Normalize(NormalizationForm.FormC);
        }

        private static bool TryParseAmount(string raw, out decimal amount)
        {
            if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out amount))
                return true;

            var normalized = raw.Replace(',', '.');
            return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
        }

        private static string? NormalizeTip(string value)
        {
            var normalized = value.Trim().ToLowerInvariant();
            return normalized switch
            {
                "gelir" => "Gelir",
                "giris" => "Gelir",
                "giri\u015F" => "Gelir",
                "income" => "Gelir",
                "gider" => "Gider",
                "cikis" => "Gider",
                "\u00E7\u0131k\u0131\u015F" => "Gider",
                "expense" => "Gider",
                _ => null
            };
        }

        private static string ToChatId(long chatId)
        {
            return chatId.ToString(CultureInfo.InvariantCulture);
        }
    }
}
