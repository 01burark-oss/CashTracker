using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using CashTracker.App.Controls;
using CashTracker.App.UI;
using CashTracker.Infrastructure.Services;

namespace CashTracker.App.Forms
{
    public sealed class InitialSetupForm : Form
    {
        private readonly TextBox _txtBotToken;
        private readonly TextBox _txtUserId;
        private readonly Label _lblTokenStatus;
        private readonly Label _lblUserStatus;
        private readonly Label _lblConnectionStatus;
        private readonly Button _btnSave;
        private readonly Button _btnTestConnection;
        private readonly Button _btnFetchChatId;

        private bool _isTesting;
        private bool _connectionVerified;
        private string _verifiedBotToken = string.Empty;
        private string _verifiedUserId = string.Empty;

        public string BotToken => _txtBotToken.Text.Trim();
        public string UserId => _txtUserId.Text.Trim();

        public InitialSetupForm(string initialBotToken, string initialUserId, bool isReconfigureMode = false)
        {
            var navy = BrandTheme.Navy;
            var teal = BrandTheme.Teal;
            var amber = BrandTheme.Amber;

            Text = "CashTracker • İlk Kurulum";
            Width = 980;
            Height = 640;
            MinimumSize = new Size(900, 600);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = true;
            BackColor = Color.FromArgb(241, 246, 252);
            Font = BrandTheme.CreateFont(10f);
            if (AppIconProvider.Current is Icon appIcon)
                Icon = appIcon;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 37f));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 63f));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            Controls.Add(root);

            var brandPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = navy,
                Padding = new Padding(24, 24, 24, 20)
            };
            root.Controls.Add(brandPanel, 0, 0);

            var brandLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4
            };
            brandLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            brandLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            brandLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            brandLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            brandLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            brandPanel.Controls.Add(brandLayout);

            var brandHeader = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 1,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 14)
            };
            brandHeader.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            brandHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            brandLayout.Controls.Add(brandHeader, 0, 0);

            var logo = new BrandLogoControl
            {
                Size = new Size(56, 56)
            };
            brandHeader.Controls.Add(logo, 0, 0);

            var brandTitleWrap = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                Margin = new Padding(10, 2, 0, 0)
            };
            brandHeader.Controls.Add(brandTitleWrap, 1, 0);

            var brandTitle = new Label
            {
                Text = "CASHTRACKER",
                AutoSize = true,
                ForeColor = Color.White,
                Font = BrandTheme.CreateHeadingFont(18f, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 2)
            };
            brandTitleWrap.Controls.Add(brandTitle);

            var brandSubtitle = new Label
            {
                Text = "Güvenli başlangıç ayarları",
                AutoSize = true,
                ForeColor = Color.FromArgb(205, 222, 240),
                Font = BrandTheme.CreateFont(10f, FontStyle.Regular),
                Margin = new Padding(0)
            };
            brandTitleWrap.Controls.Add(brandSubtitle);

            var stripe = new Panel
            {
                Height = 4,
                Dock = DockStyle.Top,
                BackColor = teal,
                Margin = new Padding(0, 4, 0, 14)
            };
            brandLayout.Controls.Add(stripe, 0, 1);

            var summary = new Label
            {
                Text =
                    "Uygulama ilk açılışta Telegram bağlantısı ister.\n\n" +
                    "• Bot Token\n" +
                    "• Telegram User ID (Chat ID)\n\n" +
                    "Kurulumu tamamlamak için test mesajı gönderilir.",
                AutoSize = true,
                MaximumSize = new Size(340, 0),
                ForeColor = Color.FromArgb(219, 231, 244),
                Font = BrandTheme.CreateFont(10f, FontStyle.Regular),
                Dock = DockStyle.Top,
                Margin = new Padding(0, 0, 0, 14)
            };
            brandLayout.Controls.Add(summary, 0, 2);

            if (isReconfigureMode)
            {
                var sideBack = new Button
                {
                    Text = "Geri Dön",
                    Width = 132,
                    Height = 36,
                    BackColor = Color.FromArgb(31, 57, 90),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Anchor = AnchorStyles.Left
                };
                sideBack.FlatAppearance.BorderColor = Color.FromArgb(93, 118, 150);
                sideBack.FlatAppearance.BorderSize = 1;
                sideBack.Click += (_, __) =>
                {
                    DialogResult = DialogResult.Cancel;
                    Close();
                };
                brandLayout.Controls.Add(sideBack, 0, 3);
            }
            else
            {
                brandLayout.Controls.Add(new Panel { Height = 1, Dock = DockStyle.Top }, 0, 3);
            }

            var rightPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 20, 24, 20),
                BackColor = BackColor
            };
            root.Controls.Add(rightPanel, 1, 0);

            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(26, 24, 26, 22)
            };
            card.Paint += (_, e) => ControlPaint.DrawBorder(
                e.Graphics,
                card.ClientRectangle,
                Color.FromArgb(211, 221, 234),
                ButtonBorderStyle.Solid);
            rightPanel.Controls.Add(card);

            var cardLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 7
            };
            cardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            cardLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            cardLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            cardLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            cardLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            cardLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            cardLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            cardLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            card.Controls.Add(cardLayout);

            var cardTitle = new Label
            {
                Text = "Telegram Bağlantı Bilgileri",
                AutoSize = true,
                Font = BrandTheme.CreateHeadingFont(14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(22, 35, 53),
                Margin = new Padding(0, 0, 0, 4)
            };
            cardLayout.Controls.Add(cardTitle, 0, 0);

            var cardMeta = new Label
            {
                Text = "Canlı doğrulama yapılır, ardından test mesajı gönderilir.",
                AutoSize = true,
                Font = BrandTheme.CreateFont(9.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(99, 113, 132),
                Margin = new Padding(0, 0, 0, 14)
            };
            cardLayout.Controls.Add(cardMeta, 0, 1);

            var formGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 1,
                RowCount = 8,
                AutoSize = true,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            formGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            for (var i = 0; i < 8; i++)
                formGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            cardLayout.Controls.Add(formGrid, 0, 2);

            var lblToken = new Label
            {
                Text = "Bot Token",
                AutoSize = true,
                ForeColor = Color.FromArgb(36, 51, 74),
                Font = BrandTheme.CreateHeadingFont(10f, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 6)
            };
            formGrid.Controls.Add(lblToken);

            _txtBotToken = new TextBox
            {
                BorderStyle = BorderStyle.FixedSingle,
                Height = 32,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 0, 0, 4)
            };
            _txtBotToken.Text = initialBotToken ?? string.Empty;
            _txtBotToken.TextChanged += (_, __) => RefreshValidationState();
            formGrid.Controls.Add(_txtBotToken);

            _lblTokenStatus = new Label
            {
                AutoSize = true,
                Font = BrandTheme.CreateFont(9.2f, FontStyle.Regular),
                Margin = new Padding(0, 0, 0, 12)
            };
            formGrid.Controls.Add(_lblTokenStatus);

            var lblUser = new Label
            {
                Text = "Telegram User ID (Chat ID)",
                AutoSize = true,
                ForeColor = Color.FromArgb(36, 51, 74),
                Font = BrandTheme.CreateHeadingFont(10f, FontStyle.Bold),
                Margin = new Padding(0, 2, 0, 6)
            };
            formGrid.Controls.Add(lblUser);

            _txtUserId = new TextBox
            {
                BorderStyle = BorderStyle.FixedSingle,
                Height = 32,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 0, 0, 4)
            };
            _txtUserId.Text = initialUserId ?? string.Empty;
            _txtUserId.TextChanged += (_, __) => RefreshValidationState();
            formGrid.Controls.Add(_txtUserId);

            _lblUserStatus = new Label
            {
                AutoSize = true,
                Font = BrandTheme.CreateFont(9.2f, FontStyle.Regular),
                Margin = new Padding(0, 0, 0, 8)
            };
            formGrid.Controls.Add(_lblUserStatus);

            _btnFetchChatId = new Button
            {
                Text = "Chat ID'yi Otomatik Al",
                Width = 174,
                Height = 34,
                BackColor = Color.FromArgb(236, 241, 247),
                ForeColor = Color.FromArgb(31, 57, 90),
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 0, 8, 8)
            };
            _btnFetchChatId.FlatAppearance.BorderColor = Color.FromArgb(201, 213, 228);
            _btnFetchChatId.FlatAppearance.BorderSize = 1;
            _btnFetchChatId.Click += async (_, __) => await FetchChatIdAsync();

            var btnOpenBotFather = new Button
            {
                Text = "BotFather'a Git",
                Width = 132,
                Height = 34,
                BackColor = Color.FromArgb(31, 57, 90),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 0, 0, 8)
            };
            btnOpenBotFather.FlatAppearance.BorderSize = 0;
            btnOpenBotFather.FlatAppearance.MouseOverBackColor = Color.FromArgb(18, 53, 92);
            btnOpenBotFather.Click += (_, __) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo("https://t.me/BotFather") { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "BotFather bağlantısı açılamadı: " + ex.Message,
                        "Bağlantı Hatası",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            };

            var actionRow = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 0, 0, 8)
            };
            actionRow.Controls.Add(_btnFetchChatId);
            actionRow.Controls.Add(btnOpenBotFather);
            formGrid.Controls.Add(actionRow);

            var helper = new Label
            {
                AutoSize = true,
                Text = "Not: Bota /start yazdıktan sonra \"Chat ID'yi Otomatik Al\" butonunu kullanabilirsiniz.",
                ForeColor = Color.FromArgb(113, 122, 138),
                Font = BrandTheme.CreateFont(9.2f, FontStyle.Regular),
                MaximumSize = new Size(620, 0),
                Margin = new Padding(0, 2, 0, 8)
            };
            cardLayout.Controls.Add(helper, 0, 3);

            _lblConnectionStatus = new Label
            {
                AutoSize = true,
                Font = BrandTheme.CreateHeadingFont(9.6f, FontStyle.Bold),
                ForeColor = Color.FromArgb(94, 106, 122),
                MaximumSize = new Size(620, 0),
                Margin = new Padding(0, 0, 0, 10),
                Text = "Bağlantı testi bekleniyor."
            };
            cardLayout.Controls.Add(_lblConnectionStatus, 0, 4);

            var btnBar = new FlowLayoutPanel
            {
                AutoSize = true,
                Height = 42,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Dock = DockStyle.Right,
                Margin = new Padding(0)
            };
            cardLayout.Controls.Add(btnBar, 0, 6);

            _btnSave = new Button
            {
                Text = "Kurulumu Tamamla",
                Width = 140,
                Height = 34,
                BackColor = amber,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(8, 0, 0, 0)
            };
            _btnSave.FlatAppearance.BorderSize = 0;
            _btnSave.FlatAppearance.MouseOverBackColor = Color.FromArgb(205, 137, 42);
            _btnSave.Click += (_, __) => Submit();
            btnBar.Controls.Add(_btnSave);

            _btnTestConnection = new Button
            {
                Text = "Bağlantıyı Test Et",
                Width = 136,
                Height = 34,
                BackColor = navy,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 0, 8, 0)
            };
            _btnTestConnection.FlatAppearance.BorderSize = 0;
            _btnTestConnection.FlatAppearance.MouseOverBackColor = Color.FromArgb(18, 53, 92);
            _btnTestConnection.Click += async (_, __) => await TestConnectionAsync();
            btnBar.Controls.Add(_btnTestConnection);

            var btnCancel = new Button
            {
                Text = isReconfigureMode ? "Geri Dön" : "Çıkış",
                Width = 84,
                Height = 34,
                BackColor = Color.FromArgb(236, 241, 247),
                ForeColor = Color.FromArgb(42, 58, 82),
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0)
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(201, 213, 228);
            btnCancel.FlatAppearance.BorderSize = 1;
            btnCancel.Click += (_, __) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };
            btnBar.Controls.Add(btnCancel);

            AcceptButton = _btnSave;
            CancelButton = btnCancel;

            RefreshValidationState();
        }

        private async Task TestConnectionAsync()
        {
            if (!TryValidateInputs(showMessage: true))
                return;

            var token = BotToken;
            var userId = UserId;

            _isTesting = true;
            RefreshValidationState();
            SetConnectionStatus(
                "Test mesaj\u0131 g\u00F6nderiliyor...",
                Color.FromArgb(31, 72, 117));

            try
            {
                using var httpClient = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(12)
                };
                var telegram = new TelegramBotService(httpClient, token);
                var stamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                await telegram.SendTextAsync(userId, $"CashTracker kurulum testi ({stamp})");

                _connectionVerified = true;
                _verifiedBotToken = token;
                _verifiedUserId = userId;

                SetConnectionStatus(
                    "Ba\u011Flant\u0131 do\u011Fruland\u0131. Test mesaj\u0131 g\u00F6nderildi.",
                    Color.FromArgb(22, 122, 87));
                MessageBox.Show(
                    "Test mesaj\u0131 ba\u015Far\u0131yla g\u00F6nderildi. Kurulumu tamamlayabilirsiniz.",
                    "Ba\u015Far\u0131l\u0131",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _connectionVerified = false;
                _verifiedBotToken = string.Empty;
                _verifiedUserId = string.Empty;

                SetConnectionStatus(
                    "Ba\u011Flant\u0131 do\u011Frulanamad\u0131. Bilgileri kontrol edip tekrar deneyin.",
                    Color.FromArgb(166, 57, 54));
                MessageBox.Show(
                    "Telegram test hatas\u0131: " + ex.Message,
                    "Ba\u011Flant\u0131 Hatas\u0131",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                _isTesting = false;
                RefreshValidationState();
            }
        }

        private async Task FetchChatIdAsync()
        {
            if (!TryValidateBotToken(BotToken, out var tokenMessage))
            {
                MessageBox.Show(tokenMessage, "Bot Token Hatas\u0131", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtBotToken.Focus();
                return;
            }

            _isTesting = true;
            RefreshValidationState();
            SetConnectionStatus(
                "Chat ID i\u00E7in son bot mesajlar\u0131 okunuyor...",
                Color.FromArgb(31, 72, 117));

            try
            {
                using var httpClient = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(12)
                };
                var telegram = new TelegramBotService(httpClient, BotToken);
                var updates = await telegram.GetUpdatesAsync(timeoutSeconds: 2);

                if (updates.Count == 0)
                {
                    SetConnectionStatus(
                        "Mesaj bulunamad\u0131. Bota /start yaz\u0131p tekrar deneyin.",
                        Color.FromArgb(176, 118, 30));
                    MessageBox.Show(
                        "Hi\u00E7 mesaj bulunamad\u0131.\n\nAd\u0131m:\n1) Telegram'da botunuza /start yaz\u0131n.\n2) Sonra bu butona tekrar bas\u0131n.",
                        "Chat ID Bulunamad\u0131",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                var distinctChatIds = updates
                    .Select(x => x.ChatId)
                    .Distinct()
                    .ToArray();

                if (distinctChatIds.Length > 1)
                {
                    var startChatIds = updates
                        .Where(x => !string.IsNullOrWhiteSpace(x.Text) &&
                                    x.Text.Trim().StartsWith("/start", StringComparison.OrdinalIgnoreCase))
                        .OrderByDescending(x => x.UpdateId)
                        .Select(x => x.ChatId)
                        .Distinct()
                        .ToArray();

                    if (startChatIds.Length == 1)
                    {
                        var selectedChatId = startChatIds[0];
                        _txtUserId.Text = selectedChatId.ToString(CultureInfo.InvariantCulture);
                        _txtUserId.SelectionStart = _txtUserId.TextLength;
                        _txtUserId.SelectionLength = 0;

                        SetConnectionStatus(
                            $"Birden fazla sohbet i\u00E7inden /start mesaj\u0131na g\u00F6re Chat ID se\u00E7ildi: {selectedChatId}",
                            Color.FromArgb(176, 118, 30));
                        MessageBox.Show(
                            $"Birden fazla sohbet bulundu ({distinctChatIds.Length}).\n/start mesaj\u0131na g\u00F6re se\u00E7ilen Chat ID: {selectedChatId}",
                            "Chat ID Se\u00E7ildi",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                        return;
                    }

                    SetConnectionStatus(
                        "Birden fazla sohbet bulundu. Yanl\u0131\u015F atamay\u0131 \u00F6nlemek i\u00E7in Chat ID otomatik se\u00E7ilmedi.",
                        Color.FromArgb(166, 57, 54));
                    MessageBox.Show(
                        $"Birden fazla sohbet bulundu ({distinctChatIds.Length}).\n\nL\u00FCtfen botunuza sadece kendi hesab\u0131n\u0131zdan /start g\u00F6nderip tekrar deneyin veya Chat ID alan\u0131na de\u011Feri manuel girin.",
                        "Chat ID Belirsiz",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                var singleChatId = distinctChatIds[0];
                _txtUserId.Text = singleChatId.ToString(CultureInfo.InvariantCulture);
                _txtUserId.SelectionStart = _txtUserId.TextLength;
                _txtUserId.SelectionLength = 0;

                SetConnectionStatus(
                    $"Chat ID otomatik al\u0131nd\u0131: {singleChatId}",
                    Color.FromArgb(22, 122, 87));
            }
            catch (Exception ex)
            {
                SetConnectionStatus(
                    "Chat ID al\u0131namad\u0131. Token veya a\u011F ba\u011Flant\u0131s\u0131n\u0131 kontrol edin.",
                    Color.FromArgb(166, 57, 54));
                MessageBox.Show(
                    "Chat ID alma hatas\u0131: " + ex.Message,
                    "Telegram Hatas\u0131",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                _isTesting = false;
                RefreshValidationState();
            }
        }

        private void Submit()
        {
            if (!TryValidateInputs(showMessage: true))
                return;

            if (!IsCurrentInputVerified())
            {
                MessageBox.Show(
                    "\u00D6nce \"Ba\u011Flant\u0131y\u0131 Test Et\" ad\u0131m\u0131n\u0131 tamamlay\u0131n.",
                    "Test Gerekli",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private bool TryValidateInputs(bool showMessage)
        {
            if (!TryValidateBotToken(BotToken, out var tokenMessage))
            {
                if (showMessage)
                {
                    MessageBox.Show(tokenMessage, "Eksik Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _txtBotToken.Focus();
                }

                return false;
            }

            if (!TryValidateUserId(UserId, out var userMessage))
            {
                if (showMessage)
                {
                    MessageBox.Show(userMessage, "Ge\u00E7ersiz Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _txtUserId.Focus();
                }

                return false;
            }

            return true;
        }

        private void RefreshValidationState()
        {
            var tokenValid = TryValidateBotToken(BotToken, out var tokenMessage);
            var userValid = TryValidateUserId(UserId, out var userMessage);

            SetFieldStatus(_lblTokenStatus, tokenMessage, tokenValid);
            SetFieldStatus(_lblUserStatus, userMessage, userValid);

            if (!IsCurrentInputVerified())
            {
                _connectionVerified = false;
                _verifiedBotToken = string.Empty;
                _verifiedUserId = string.Empty;

                if (tokenValid && userValid)
                {
                    SetConnectionStatus(
                        "Ba\u011Flant\u0131 testi bekleniyor.",
                        Color.FromArgb(176, 118, 30));
                }
                else
                {
                    SetConnectionStatus(
                        "Ba\u011Flant\u0131 testi i\u00E7in alanlar\u0131 d\u00FCzeltin.",
                        Color.FromArgb(94, 106, 122));
                }
            }

            _btnFetchChatId.Enabled = !_isTesting && tokenValid;
            _btnTestConnection.Enabled = !_isTesting && tokenValid && userValid;
            _btnSave.Enabled = !_isTesting && tokenValid && userValid && IsCurrentInputVerified();
        }

        private bool IsCurrentInputVerified()
        {
            return _connectionVerified &&
                   string.Equals(BotToken, _verifiedBotToken, StringComparison.Ordinal) &&
                   string.Equals(UserId, _verifiedUserId, StringComparison.Ordinal);
        }

        private static bool TryValidateBotToken(string value, out string message)
        {
            var token = value?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(token))
            {
                message = "Bot token zorunludur.";
                return false;
            }

            var parts = token.Split(':');
            if (parts.Length != 2 ||
                parts[0].Length < 6 ||
                parts[1].Length < 10 ||
                !long.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
            {
                message = "Bi\u00E7im ge\u00E7ersiz. \u00D6rnek: 123456789:ABCDEF...";
                return false;
            }

            message = "Bot token bi\u00E7imi uygun.";
            return true;
        }

        private static bool TryValidateUserId(string value, out string message)
        {
            var userId = value?.Trim() ?? string.Empty;
            if (!long.TryParse(userId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ||
                parsed == 0)
            {
                message = "User ID say\u0131sal olmal\u0131d\u0131r.";
                return false;
            }

            message = "User ID ge\u00E7erli g\u00F6r\u00FCn\u00FCyor.";
            return true;
        }

        private static void SetFieldStatus(Label label, string text, bool isValid)
        {
            label.Text = text;
            label.ForeColor = isValid
                ? Color.FromArgb(23, 122, 88)
                : Color.FromArgb(166, 57, 54);
        }

        private void SetConnectionStatus(string text, Color foreColor)
        {
            _lblConnectionStatus.Text = text;
            _lblConnectionStatus.ForeColor = foreColor;
        }
    }
}


