using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Infrastructure.Services;
using CashTracker.Tests.Support;
using Xunit;

namespace CashTracker.Tests
{
    public sealed class TelegramCommandServiceTests
    {
        [Fact]
        public async Task ProcessUpdateAsync_OzetCommand_IncludesBusinessAndKalemBreakdown()
        {
            var now = DateTime.Now;
            var kasa = new FakeKasaService(new[]
            {
                new Kasa { Id = 1, Tarih = now.AddDays(-1), Tip = "Gelir", Tutar = 120m, OdemeYontemi = "Nakit", Kalem = "Nakit Satis" },
                new Kasa { Id = 2, Tarih = now.AddDays(-1), Tip = "Gelir", Tutar = 80m, OdemeYontemi = "KrediKarti", Kalem = "Nakit Satis" },
                new Kasa { Id = 3, Tarih = now.AddDays(-1), Tip = "Gider", Tutar = 50m, OdemeYontemi = "Havale", Kalem = "Kira", GiderTuru = "Kira" }
            });

            var summary = new FakeSummaryService
            {
                SummaryToReturn = new PeriodSummary
                {
                    IncomeTotal = 200m,
                    ExpenseTotal = 50m,
                    IncomeCount = 2,
                    ExpenseCount = 1
                }
            };

            var kalem = new FakeKalemTanimiService();
            var isletme = new FakeIsletmeService
            {
                Active = new Isletme { Id = 7, Ad = "Demo Isletme", IsAktif = true }
            };

            var (_, handler, service, _) = BuildService(kasa, kalem, summary, isletme);

            await service.ProcessUpdateAsync(new TelegramUpdate
            {
                UpdateId = 1,
                ChatId = 123,
                UserId = 42,
                Text = "/ozet 7"
            });

            var text = handler.GetLastFormFieldValue("/sendMessage", "text");
            Assert.False(string.IsNullOrWhiteSpace(text));
            Assert.Contains("Isletme: Demo Isletme", text!);
            Assert.Contains("Odeme Yontemleri:", text!);
            Assert.Contains("- Nakit:", text!);
            Assert.Contains("- Kredi Karti:", text!);
            Assert.Contains("- Online Odeme:", text!);
            Assert.Contains("- Havale:", text!);
            Assert.Contains("Gelir Kalemleri:", text!);
            Assert.Contains("Gider Kalemleri:", text!);
            Assert.Contains("- Nakit Satis:", text!);
            Assert.Contains("- Kira:", text!);
        }

        [Fact]
        public async Task ProcessUpdateAsync_GelirCommand_ResponseContainsBusinessAndKalem()
        {
            var kasa = new FakeKasaService();
            var kalem = new FakeKalemTanimiService(new[]
            {
                new KalemTanimi { Id = 1, Tip = "Gelir", Ad = "Genel Gelir" }
            });
            var summary = new FakeSummaryService();
            var isletme = new FakeIsletmeService
            {
                Active = new Isletme { Id = 7, Ad = "Demo Isletme", IsAktif = true }
            };

            var (_, handler, service, _) = BuildService(kasa, kalem, summary, isletme);

            await service.ProcessUpdateAsync(new TelegramUpdate
            {
                UpdateId = 2,
                ChatId = 123,
                UserId = 42,
                Text = "/gelir 125"
            });

            var text = handler.GetLastFormFieldValue("/sendMessage", "text");
            Assert.False(string.IsNullOrWhiteSpace(text));
            Assert.Contains("Kaydedildi. Id:", text!);
            Assert.Contains("Isletme: Demo Isletme", text!);
            Assert.Contains("Tip: Gelir", text!);
            Assert.Contains("Kalem: Genel Gelir", text!);

            Assert.NotNull(kasa.LastCreated);
            Assert.Equal("Gelir", kasa.LastCreated!.Tip);
            Assert.Equal("Genel Gelir", kasa.LastCreated.Kalem);
        }

        [Fact]
        public async Task ProcessUpdateAsync_SifreCommand_UpdatesPin()
        {
            var kasa = new FakeKasaService();
            var kalem = new FakeKalemTanimiService();
            var summary = new FakeSummaryService();
            var isletme = new FakeIsletmeService
            {
                Active = new Isletme { Id = 7, Ad = "Demo Isletme", IsAktif = true }
            };

            var (_, handler, service, security) = BuildService(kasa, kalem, summary, isletme);

            await service.ProcessUpdateAsync(new TelegramUpdate
            {
                UpdateId = 3,
                ChatId = 123,
                UserId = 42,
                Text = "/sifre 2468"
            });

            var text = handler.GetLastFormFieldValue("/sendMessage", "text");
            Assert.Equal("2468", security.Pin);
            Assert.Contains("Uygulama sifresi guncellendi.", text!);
        }

        private static (TelegramBotService Bot, RecordingHttpMessageHandler Handler, TelegramCommandService Service, FakeAppSecurityService Security) BuildService(
            FakeKasaService kasa,
            FakeKalemTanimiService kalem,
            FakeSummaryService summary,
            FakeIsletmeService isletme)
        {
            var handler = new RecordingHttpMessageHandler();
            var http = new HttpClient(handler);
            var bot = new TelegramBotService(http, "test-token");
            var settings = new TelegramSettings
            {
                BotToken = "test-token",
                ChatId = "123",
                EnableCommands = true,
                AllowedUserIds = "42"
            };

            var backup = new BackupReportService(
                bot,
                settings,
                new FakeDailyReportService(),
                new DatabaseBackupService(new DatabasePaths(Path.Combine(
                    Path.GetTempPath(),
                    $"cashtracker_tests_{Guid.NewGuid():N}.db"))));

            var security = new FakeAppSecurityService();
            var approvals = new FakeTelegramApprovalService();

            var service = new TelegramCommandService(
                bot,
                settings,
                kasa,
                kalem,
                summary,
                isletme,
                security,
                backup,
                approvals);

            return (bot, handler, service, security);
        }
    }
}
