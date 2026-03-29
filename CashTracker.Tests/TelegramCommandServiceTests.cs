using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net;
using System.Linq;
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

            var (_, handler, service, _, _, _) = BuildService(kasa, kalem, summary, isletme);

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

            var (_, handler, service, _, _, _) = BuildService(kasa, kalem, summary, isletme);

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
        public async Task ProcessUpdateAsync_SifreCommand_IsRejected()
        {
            var kasa = new FakeKasaService();
            var kalem = new FakeKalemTanimiService();
            var summary = new FakeSummaryService();
            var isletme = new FakeIsletmeService
            {
                Active = new Isletme { Id = 7, Ad = "Demo Isletme", IsAktif = true }
            };

            var (_, handler, service, security, _, _) = BuildService(kasa, kalem, summary, isletme);

            await service.ProcessUpdateAsync(new TelegramUpdate
            {
                UpdateId = 3,
                ChatId = 123,
                UserId = 42,
                Text = "/sifre 2468"
            });

            var text = handler.GetLastFormFieldValue("/sendMessage", "text");
            Assert.Equal("0000", security.Pin);
            Assert.Contains("PIN degisimi Telegram uzerinden kapatildi.", text!);
        }

        [Fact]
        public async Task ProcessUpdateAsync_PhotoReceipt_FollowsPromptAndCreatesGroupedExpenseRecords()
        {
            var kasa = new FakeKasaService();
            var kalem = new FakeKalemTanimiService(new[]
            {
                new KalemTanimi { Id = 1, Tip = "Gider", Ad = "Genel Gider" },
                new KalemTanimi { Id = 2, Tip = "Gider", Ad = "Mutfak Giderleri" },
                new KalemTanimi { Id = 3, Tip = "Gider", Ad = "Temizlik Giderleri" }
            });
            var summary = new FakeSummaryService();
            var isletme = new FakeIsletmeService
            {
                Active = new Isletme { Id = 7, Ad = "Demo Isletme", IsAktif = true }
            };

            var (_, handler, service, _, ocr, _) = BuildService(kasa, kalem, summary, isletme, BuildPhotoResponder());
            ocr.NextResult = new ReceiptOcrResult
            {
                Merchant = "Migros",
                ReceiptDate = new DateTime(2026, 3, 29, 10, 30, 0),
                PaymentMethod = "KrediKarti",
                ReceiptTotal = 60m,
                Items = new List<ReceiptOcrLineItem>
                {
                    new() { RawName = "Sut", Amount = 40m, CandidateKalem = "Mutfak Giderleri", Confidence = 0.97m, NeedsUserInput = false },
                    new() { RawName = "Deterjan", Amount = 20m, CandidateKalem = "", Confidence = 0.12m, NeedsUserInput = true }
                }
            };

            await service.ProcessUpdateAsync(new TelegramUpdate
            {
                UpdateId = 10,
                MessageId = 100,
                ChatId = 123,
                UserId = 42,
                PhotoFileId = "photo-1"
            });

            var prompt = handler.GetLastFormFieldValue("/sendMessage", "text");
            Assert.Contains("Urun icin gider kalemi sec: Deterjan", prompt!);
            Assert.Contains("Kalemler genel gider gruplari olmali", prompt!);

            await service.ProcessUpdateAsync(new TelegramUpdate
            {
                UpdateId = 11,
                ChatId = 123,
                UserId = 42,
                Text = "3"
            });

            var summaryText = handler.GetLastFormFieldValue("/sendMessage", "text");
            Assert.Contains("Fis ozeti", summaryText!);
            Assert.Contains("Odeme: KrediKarti", summaryText!);

            await service.ProcessUpdateAsync(new TelegramUpdate
            {
                UpdateId = 12,
                ChatId = 123,
                UserId = 42,
                Text = "onayla"
            });

            Assert.Equal(2, kasa.Rows.Count);
            Assert.Contains(kasa.Rows, x => x.Kalem == "Mutfak Giderleri" && x.Tutar == 40m);
            Assert.Contains(kasa.Rows, x => x.Kalem == "Temizlik Giderleri" && x.Tutar == 20m);
            Assert.All(kasa.Rows, x => Assert.Equal("OCR Fis | Migros", x.Aciklama));
            Assert.All(kasa.Rows, x => Assert.DoesNotContain("Sut", x.Aciklama));
            Assert.All(kasa.Rows, x => Assert.DoesNotContain("Deterjan", x.Aciklama));
        }

        [Fact]
        public async Task ProcessUpdateAsync_PhotoReceipt_UsesDefaultGeneralCategoriesWhenNoExpenseCategoryExists()
        {
            var kasa = new FakeKasaService();
            var kalem = new FakeKalemTanimiService();
            var summary = new FakeSummaryService();
            var isletme = new FakeIsletmeService
            {
                Active = new Isletme { Id = 7, Ad = "Demo Isletme", IsAktif = true }
            };

            var (_, handler, service, _, ocr, _) = BuildService(kasa, kalem, summary, isletme, BuildPhotoResponder());
            ocr.NextResult = new ReceiptOcrResult
            {
                Merchant = "Bim",
                ReceiptDate = new DateTime(2026, 3, 29, 12, 0, 0),
                PaymentMethod = "Nakit",
                Items = new List<ReceiptOcrLineItem>
                {
                    new() { RawName = "Belirsiz Urun", Amount = 10m, CandidateKalem = "", NeedsUserInput = true }
                }
            };

            await service.ProcessUpdateAsync(new TelegramUpdate
            {
                UpdateId = 19,
                MessageId = 190,
                ChatId = 123,
                UserId = 42,
                PhotoFileId = "photo-defaults"
            });

            var prompt = handler.GetLastFormFieldValue("/sendMessage", "text");
            Assert.Contains("Mutfak Giderleri", prompt!);
            Assert.Contains("Personel Giderleri", prompt!);
            Assert.Contains("Kalemler genel gider gruplari olmali", prompt!);
        }

        [Fact]
        public async Task ProcessUpdateAsync_PhotoReceipt_WhenGeminiRateLimited_ShowsSpecificError()
        {
            var kasa = new FakeKasaService();
            var kalem = new FakeKalemTanimiService(new[]
            {
                new KalemTanimi { Id = 1, Tip = "Gider", Ad = "Genel Gider" }
            });
            var summary = new FakeSummaryService();
            var isletme = new FakeIsletmeService
            {
                Active = new Isletme { Id = 7, Ad = "Demo Isletme", IsAktif = true }
            };

            var (_, handler, service, _, ocr, _) = BuildService(kasa, kalem, summary, isletme, BuildPhotoResponder());
            ocr.NextException = new InvalidOperationException("Gemini OCR failed: 429 - rate limit");

            await service.ProcessUpdateAsync(new TelegramUpdate
            {
                UpdateId = 19,
                MessageId = 191,
                ChatId = 123,
                UserId = 42,
                PhotoFileId = "photo-rate-limit"
            });

            var text = handler.GetLastFormFieldValue("/sendMessage", "text");
            Assert.Contains("Fis OCR islenemedi.", text!);
            Assert.Contains("Gemini kota veya hiz limiti asildi.", text!);
        }

        [Fact]
        public async Task ProcessUpdateAsync_PhotoReceipt_NewCategoryFlow_CreatesCategoryAndUsesIt()
        {
            var kasa = new FakeKasaService();
            var kalem = new FakeKalemTanimiService(new[]
            {
                new KalemTanimi { Id = 1, Tip = "Gider", Ad = "Genel Gider" },
                new KalemTanimi { Id = 2, Tip = "Gider", Ad = "Mutfak Giderleri" }
            });
            var summary = new FakeSummaryService();
            var isletme = new FakeIsletmeService
            {
                Active = new Isletme { Id = 7, Ad = "Demo Isletme", IsAktif = true }
            };

            var (_, handler, service, _, ocr, _) = BuildService(kasa, kalem, summary, isletme, BuildPhotoResponder());
            ocr.NextResult = new ReceiptOcrResult
            {
                Merchant = "Carrefour",
                ReceiptDate = new DateTime(2026, 3, 29, 18, 15, 0),
                PaymentMethod = "Nakit",
                Items = new List<ReceiptOcrLineItem>
                {
                    new() { RawName = "Paspas", Amount = 55m, CandidateKalem = "", NeedsUserInput = true }
                }
            };

            await service.ProcessUpdateAsync(new TelegramUpdate
            {
                UpdateId = 20,
                MessageId = 200,
                ChatId = 123,
                UserId = 42,
                PhotoFileId = "photo-2"
            });

            await service.ProcessUpdateAsync(new TelegramUpdate
            {
                UpdateId = 21,
                ChatId = 123,
                UserId = 42,
                Text = "yeni: Sarf Giderleri"
            });

            var confirmText = handler.GetLastFormFieldValue("/sendMessage", "text");
            Assert.Contains("'Sarf Giderleri' adli gider kalemi olusturayim mi?", confirmText!);

            await service.ProcessUpdateAsync(new TelegramUpdate
            {
                UpdateId = 22,
                ChatId = 123,
                UserId = 42,
                Text = "evet"
            });

            await service.ProcessUpdateAsync(new TelegramUpdate
            {
                UpdateId = 23,
                ChatId = 123,
                UserId = 42,
                Text = "onayla"
            });

            var allCategories = await kalem.GetAllAsync();
            Assert.Contains(allCategories, x => x.Tip == "Gider" && x.Ad == "Sarf Giderleri");
            Assert.Contains(kasa.Rows, x => x.Kalem == "Sarf Giderleri" && x.Tutar == 55m);
        }

        [Fact]
        public async Task ProcessUpdateAsync_PhotoReceipt_AsksForDateAndPaymentWhenMissing()
        {
            var kasa = new FakeKasaService();
            var kalem = new FakeKalemTanimiService(new[]
            {
                new KalemTanimi { Id = 1, Tip = "Gider", Ad = "Genel Gider" },
                new KalemTanimi { Id = 2, Tip = "Gider", Ad = "Market" }
            });
            var summary = new FakeSummaryService();
            var isletme = new FakeIsletmeService
            {
                Active = new Isletme { Id = 7, Ad = "Demo Isletme", IsAktif = true }
            };

            var (_, handler, service, _, ocr, _) = BuildService(kasa, kalem, summary, isletme, BuildPhotoResponder());
            ocr.NextResult = new ReceiptOcrResult
            {
                Merchant = "A101",
                Items = new List<ReceiptOcrLineItem>
                {
                    new() { RawName = "Ekmek", Amount = 15m, CandidateKalem = "Market", NeedsUserInput = false }
                }
            };

            await service.ProcessUpdateAsync(new TelegramUpdate
            {
                UpdateId = 30,
                MessageId = 300,
                ChatId = 123,
                UserId = 42,
                PhotoFileId = "photo-3"
            });

            var datePrompt = handler.GetLastFormFieldValue("/sendMessage", "text");
            Assert.Contains("Fis tarihi eksik", datePrompt!);

            await service.ProcessUpdateAsync(new TelegramUpdate
            {
                UpdateId = 31,
                ChatId = 123,
                UserId = 42,
                Text = "atla"
            });

            var paymentPrompt = handler.GetLastFormFieldValue("/sendMessage", "text");
            Assert.Contains("Odeme yontemi eksik", paymentPrompt!);

            await service.ProcessUpdateAsync(new TelegramUpdate
            {
                UpdateId = 32,
                ChatId = 123,
                UserId = 42,
                Text = "Havale"
            });

            var finalSummary = handler.GetLastFormFieldValue("/sendMessage", "text");
            Assert.Contains("Fis ozeti", finalSummary!);
            Assert.Contains("Odeme: Havale", finalSummary!);
        }

        [Fact]
        public async Task ProcessUpdateAsync_WhenSessionActive_RejectsSecondPhoto()
        {
            var kasa = new FakeKasaService();
            var kalem = new FakeKalemTanimiService(new[]
            {
                new KalemTanimi { Id = 1, Tip = "Gider", Ad = "Genel Gider" },
                new KalemTanimi { Id = 2, Tip = "Gider", Ad = "Market" }
            });
            var summary = new FakeSummaryService();
            var isletme = new FakeIsletmeService
            {
                Active = new Isletme { Id = 7, Ad = "Demo Isletme", IsAktif = true }
            };

            var (_, handler, service, _, ocr, _) = BuildService(kasa, kalem, summary, isletme, BuildPhotoResponder());
            ocr.NextResult = new ReceiptOcrResult
            {
                Merchant = "A101",
                ReceiptDate = new DateTime(2026, 3, 29, 9, 0, 0),
                PaymentMethod = "Nakit",
                Items = new List<ReceiptOcrLineItem>
                {
                    new() { RawName = "Belirsiz", Amount = 10m, CandidateKalem = "", NeedsUserInput = true }
                }
            };

            await service.ProcessUpdateAsync(new TelegramUpdate
            {
                UpdateId = 40,
                MessageId = 400,
                ChatId = 123,
                UserId = 42,
                PhotoFileId = "photo-4"
            });

            await service.ProcessUpdateAsync(new TelegramUpdate
            {
                UpdateId = 41,
                MessageId = 401,
                ChatId = 123,
                UserId = 42,
                PhotoFileId = "photo-5"
            });

            var text = handler.GetLastFormFieldValue("/sendMessage", "text");
            Assert.Contains("Devam eden bir fis oturumu var", text!);
        }

        private static (TelegramBotService Bot, RecordingHttpMessageHandler Handler, TelegramCommandService Service, FakeAppSecurityService Security, FakeReceiptOcrService Ocr, FakeTelegramReceiptSessionStore SessionStore) BuildService(
            FakeKasaService kasa,
            FakeKalemTanimiService kalem,
            FakeSummaryService summary,
            FakeIsletmeService isletme,
            Func<HttpRequestMessage, string, HttpResponseMessage>? responder = null)
        {
            var handler = new RecordingHttpMessageHandler(responder);
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
            var ocr = new FakeReceiptOcrService();
            var sessionStore = new FakeTelegramReceiptSessionStore();
            var receiptOcrSettings = new ReceiptOcrSettings
            {
                Provider = "Gemini",
                ApiKey = "test-key",
                Model = "gemini-2.5-flash",
                SessionTimeoutMinutes = 30
            };

            var service = new TelegramCommandService(
                bot,
                settings,
                kasa,
                kalem,
                summary,
                isletme,
                security,
                backup,
                approvals,
                ocr,
                sessionStore,
                receiptOcrSettings);

            return (bot, handler, service, security, ocr, sessionStore);
        }

        private static Func<HttpRequestMessage, string, HttpResponseMessage> BuildPhotoResponder()
        {
            return (request, _) =>
            {
                var url = request.RequestUri?.ToString() ?? string.Empty;
                if (url.Contains("/getFile", StringComparison.OrdinalIgnoreCase))
                {
                    return RecordingHttpMessageHandler.OkJson("{\"ok\":true,\"result\":{\"file_path\":\"photos/test.jpg\"}}");
                }

                if (url.Contains("/file/bottest-token/photos/test.jpg", StringComparison.OrdinalIgnoreCase))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new ByteArrayContent(new byte[] { 1, 2, 3, 4 })
                    };
                }

                return RecordingHttpMessageHandler.OkJson("{\"ok\":true,\"result\":{}}");
            };
        }
    }
}
