using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using CashTracker.App;
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
        private readonly Label _lblStep1;
        private readonly Label _lblStep2;
        private readonly Label _lblStep3;
        private readonly Label _lblStep4;
        private readonly Label _lblStep5;
        private readonly Label _lblStep6;
        private readonly Label _lblStep7;
        private readonly Label _lblStep8;
        private readonly Label _lblStepHint;
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

            Text = T("initial.title");
            Width = 980;
            Height = 640;
            MinimumSize = new Size(1120, 680);
            UiMetrics.ApplyFormDefaults(this);
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
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42f));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58f));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            Controls.Add(root);

            var brandPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = navy,
                Padding = new Padding(24, 24, 24, 20),
                AutoScroll = true
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
            brandLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            brandLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
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

            var brandTitleWrap = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(10, 2, 0, 0)
            };
            brandTitleWrap.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            brandTitleWrap.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            brandTitleWrap.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            brandHeader.Controls.Add(brandTitleWrap, 1, 0);

            var brandTitleFont = BrandTheme.CreateHeadingFont(18f, FontStyle.Bold);
            var brandTitle = new Label
            {
                Text = "CASHTRACKER",
                AutoSize = false,
                AutoEllipsis = false,
                Dock = DockStyle.Top,
                Height = UiMetrics.GetTextLineHeight(brandTitleFont) + 4,
                ForeColor = Color.White,
                Font = brandTitleFont,
                Margin = new Padding(0, 0, 0, 2)
            };
            brandTitleWrap.Controls.Add(brandTitle, 0, 0);

            var brandSubtitleFont = BrandTheme.CreateFont(10f, FontStyle.Regular);
            var brandSubtitle = new Label
            {
                Text = T("initial.brand.subtitle"),
                AutoSize = false,
                AutoEllipsis = false,
                Dock = DockStyle.Top,
                Height = UiMetrics.GetTextLineHeight(brandSubtitleFont) + 2,
                ForeColor = Color.FromArgb(205, 222, 240),
                Font = brandSubtitleFont,
                Margin = new Padding(0)
            };
            brandTitleWrap.Controls.Add(brandSubtitle, 0, 1);

            var stripe = new Panel
            {
                Height = 4,
                Dock = DockStyle.Top,
                BackColor = teal,
                Margin = new Padding(0, 4, 0, 14)
            };
            brandLayout.Controls.Add(stripe, 0, 1);

            var stepsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 10,
                Margin = new Padding(0, 0, 0, 14)
            };
            stepsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            for (var i = 0; i < 10; i++)
                stepsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            brandLayout.Controls.Add(stepsPanel, 0, 2);

            var stepTitleFont = BrandTheme.CreateHeadingFont(11f, FontStyle.Bold);
            var stepTitle = new Label
            {
                Text = T("initial.steps.title"),
                AutoSize = false,
                AutoEllipsis = false,
                Height = UiMetrics.GetTextLineHeight(stepTitleFont) + 2,
                Dock = DockStyle.Top,
                ForeColor = Color.White,
                Font = stepTitleFont,
                Margin = new Padding(0, 0, 0, 8)
            };
            stepsPanel.Controls.Add(stepTitle, 0, 0);

            const int stepWrapWidth = 360;
            _lblStep1 = CreateGuideStepLabel(T("initial.steps.1"), stepWrapWidth);
            _lblStep2 = CreateGuideStepLabel(T("initial.steps.2"), stepWrapWidth);
            _lblStep3 = CreateGuideStepLabel(T("initial.steps.3"), stepWrapWidth);
            _lblStep4 = CreateGuideStepLabel(T("initial.steps.4"), stepWrapWidth);
            _lblStep5 = CreateGuideStepLabel(T("initial.steps.5"), stepWrapWidth);
            _lblStep6 = CreateGuideStepLabel(T("initial.steps.6"), stepWrapWidth);
            _lblStep7 = CreateGuideStepLabel(T("initial.steps.7"), stepWrapWidth);
            _lblStep8 = CreateGuideStepLabel(T("initial.steps.8"), stepWrapWidth);
            _lblStepHint = new Label
            {
                AutoSize = true,
                AutoEllipsis = false,
                MaximumSize = new Size(stepWrapWidth, 0),
                Dock = DockStyle.Top,
                ForeColor = Color.FromArgb(219, 231, 244),
                Font = BrandTheme.CreateFont(9.5f, FontStyle.Bold),
                Margin = new Padding(0, 8, 0, 0)
            };

            stepsPanel.Controls.Add(_lblStep1, 0, 1);
            stepsPanel.Controls.Add(_lblStep2, 0, 2);
            stepsPanel.Controls.Add(_lblStep3, 0, 3);
            stepsPanel.Controls.Add(_lblStep4, 0, 4);
            stepsPanel.Controls.Add(_lblStep5, 0, 5);
            stepsPanel.Controls.Add(_lblStep6, 0, 6);
            stepsPanel.Controls.Add(_lblStep7, 0, 7);
            stepsPanel.Controls.Add(_lblStep8, 0, 8);
            stepsPanel.Controls.Add(_lblStepHint, 0, 9);

            if (isReconfigureMode)
            {
                var sideBack = CreateActionButton(T("initial.button.back"), Color.FromArgb(31, 57, 90), 132);
                sideBack.Anchor = AnchorStyles.Left;
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
                BackColor = BackColor,
                AutoScroll = true
            };
            root.Controls.Add(rightPanel, 1, 0);

            var card = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
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
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
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
                Text = T("initial.card.title"),
                AutoSize = true,
                Font = BrandTheme.CreateHeadingFont(14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(22, 35, 53),
                Margin = new Padding(0, 0, 0, 4)
            };
            cardLayout.Controls.Add(cardTitle, 0, 0);

            var cardMeta = new Label
            {
                Text = T("initial.card.meta"),
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
                Text = T("initial.label.botToken"),
                AutoSize = true,
                ForeColor = Color.FromArgb(36, 51, 74),
                Font = BrandTheme.CreateHeadingFont(10f, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 6)
            };
            formGrid.Controls.Add(lblToken);

            _txtBotToken = CreateSingleLineInput();
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
                Text = T("initial.label.userId"),
                AutoSize = true,
                ForeColor = Color.FromArgb(36, 51, 74),
                Font = BrandTheme.CreateHeadingFont(10f, FontStyle.Bold),
                Margin = new Padding(0, 2, 0, 6)
            };
            formGrid.Controls.Add(lblUser);

            _txtUserId = CreateSingleLineInput();
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

            _btnFetchChatId = CreateActionButton(T("initial.button.fetchChatId"), navy, 220);
            _btnFetchChatId.Margin = new Padding(0, 0, 8, 8);
            _btnFetchChatId.FlatAppearance.BorderColor = Color.FromArgb(93, 118, 150);
            _btnFetchChatId.FlatAppearance.BorderSize = 1;
            _btnFetchChatId.FlatAppearance.MouseOverBackColor = Color.FromArgb(18, 53, 92);
            _btnFetchChatId.Click += async (_, __) => await FetchChatIdAsync();

            var btnOpenBotFather = CreateActionButton(T("initial.button.openBotFather"), Color.FromArgb(31, 57, 90), 150);
            btnOpenBotFather.Margin = new Padding(0, 0, 0, 8);
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
                        F("initial.error.botFatherOpenBody", ex.Message),
                        T("initial.error.botFatherOpenTitle"),
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
                AutoEllipsis = false,
                MaximumSize = new Size(620, 0),
                Dock = DockStyle.Top,
                Text = T("initial.hint.fetchChatId"),
                ForeColor = Color.FromArgb(113, 122, 138),
                Font = BrandTheme.CreateFont(9.2f, FontStyle.Regular),
                Margin = new Padding(0, 2, 0, 8)
            };
            cardLayout.Controls.Add(helper, 0, 3);

            _lblConnectionStatus = new Label
            {
                AutoSize = true,
                AutoEllipsis = false,
                MaximumSize = new Size(620, 0),
                Dock = DockStyle.Top,
                Font = BrandTheme.CreateHeadingFont(9.6f, FontStyle.Bold),
                ForeColor = Color.FromArgb(94, 106, 122),
                Margin = new Padding(0, 0, 0, 10),
                Text = T("initial.status.connectionWait")
            };
            cardLayout.Controls.Add(_lblConnectionStatus, 0, 4);

            var btnBar = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Dock = DockStyle.Fill,
                Margin = new Padding(0)
            };
            cardLayout.Controls.Add(btnBar, 0, 6);

            _btnSave = CreateActionButton(T("initial.button.finish"), navy, 188);
            _btnSave.Margin = new Padding(8, 0, 0, 0);
            _btnSave.FlatAppearance.BorderSize = 0;
            _btnSave.FlatAppearance.MouseOverBackColor = Color.FromArgb(18, 53, 92);
            _btnSave.Click += (_, __) => Submit();
            btnBar.Controls.Add(_btnSave);

            _btnTestConnection = CreateActionButton(T("initial.button.testConnection"), navy, 170);
            _btnTestConnection.Margin = new Padding(0, 0, 8, 0);
            _btnTestConnection.FlatAppearance.BorderSize = 0;
            _btnTestConnection.FlatAppearance.MouseOverBackColor = Color.FromArgb(18, 53, 92);
            _btnTestConnection.Click += async (_, __) => await TestConnectionAsync();
            btnBar.Controls.Add(_btnTestConnection);

            var btnCancel = CreateActionButton(isReconfigureMode ? T("initial.button.back") : T("initial.button.exit"), navy, 96);
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(93, 118, 150);
            btnCancel.FlatAppearance.BorderSize = 1;
            btnCancel.FlatAppearance.MouseOverBackColor = Color.FromArgb(18, 53, 92);
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
                T("initial.status.testing"),
                Color.FromArgb(31, 72, 117));

            try
            {
                using var httpClient = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(12)
                };
                var telegram = new TelegramBotService(httpClient, token);
                var stamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                await telegram.SendTextAsync(userId, F("initial.telegram.testMessage", stamp));

                _connectionVerified = true;
                _verifiedBotToken = token;
                _verifiedUserId = userId;

                SetConnectionStatus(
                    T("initial.status.verified"),
                    Color.FromArgb(22, 122, 87));
                MessageBox.Show(
                    T("initial.message.testSuccessBody"),
                    T("initial.message.successTitle"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _connectionVerified = false;
                _verifiedBotToken = string.Empty;
                _verifiedUserId = string.Empty;

                SetConnectionStatus(
                    T("initial.status.verifyFailed"),
                    Color.FromArgb(166, 57, 54));
                MessageBox.Show(
                    F("initial.error.connectionBody", ex.Message),
                    T("initial.error.connectionTitle"),
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
                MessageBox.Show(tokenMessage, T("initial.error.botTokenTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtBotToken.Focus();
                return;
            }

            _isTesting = true;
            RefreshValidationState();
            SetConnectionStatus(
                T("initial.status.fetchingChatId"),
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
                        T("initial.status.noMessage"),
                        Color.FromArgb(176, 118, 30));
                    MessageBox.Show(
                        T("initial.message.noMessageBody"),
                        T("initial.message.chatIdNotFoundTitle"),
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
                            F("initial.status.chatIdSelectedFromStart", selectedChatId),
                            Color.FromArgb(176, 118, 30));
                        MessageBox.Show(
                            F("initial.message.chatIdSelectedFromStartBody", distinctChatIds.Length, selectedChatId),
                            T("initial.message.chatIdSelectedTitle"),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                        return;
                    }

                    SetConnectionStatus(
                        T("initial.status.chatIdAmbiguous"),
                        Color.FromArgb(166, 57, 54));
                    MessageBox.Show(
                        F("initial.message.chatIdAmbiguousBody", distinctChatIds.Length),
                        T("initial.message.chatIdAmbiguousTitle"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                var singleChatId = distinctChatIds[0];
                _txtUserId.Text = singleChatId.ToString(CultureInfo.InvariantCulture);
                _txtUserId.SelectionStart = _txtUserId.TextLength;
                _txtUserId.SelectionLength = 0;

                SetConnectionStatus(
                    F("initial.status.chatIdAuto", singleChatId),
                    Color.FromArgb(22, 122, 87));
            }
            catch (Exception ex)
            {
                SetConnectionStatus(
                    T("initial.status.chatIdFetchFailed"),
                    Color.FromArgb(166, 57, 54));
                MessageBox.Show(
                    F("initial.error.chatIdFetchBody", ex.Message),
                    T("initial.error.telegramTitle"),
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
                    T("initial.error.testRequiredBody"),
                    T("initial.error.testRequiredTitle"),
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
                    MessageBox.Show(tokenMessage, T("initial.error.missingInfoTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _txtBotToken.Focus();
                }

                return false;
            }

            if (!TryValidateUserId(UserId, out var userMessage))
            {
                if (showMessage)
                {
                    MessageBox.Show(userMessage, T("initial.error.invalidInfoTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                        T("initial.status.connectionWait"),
                        Color.FromArgb(176, 118, 30));
                }
                else
                {
                    SetConnectionStatus(
                        T("initial.status.fixFields"),
                        Color.FromArgb(94, 106, 122));
                }
            }

            _btnFetchChatId.Enabled = !_isTesting && tokenValid;
            _btnTestConnection.Enabled = !_isTesting && tokenValid && userValid;
            _btnSave.Enabled = !_isTesting && tokenValid && userValid && IsCurrentInputVerified();
            UpdateStepGuide(tokenValid, userValid);
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
                message = T("initial.validation.botTokenRequired");
                return false;
            }

            var parts = token.Split(':');
            if (parts.Length != 2 ||
                parts[0].Length < 6 ||
                parts[1].Length < 10 ||
                !long.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
            {
                message = T("initial.validation.botTokenFormat");
                return false;
            }

            message = T("initial.validation.botTokenValid");
            return true;
        }

        private static bool TryValidateUserId(string value, out string message)
        {
            var userId = value?.Trim() ?? string.Empty;
            if (!long.TryParse(userId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ||
                parsed == 0)
            {
                message = T("initial.validation.userIdNumeric");
                return false;
            }

            message = T("initial.validation.userIdValid");
            return true;
        }

        private static void SetFieldStatus(Label label, string text, bool isValid)
        {
            label.Text = text;
            label.ForeColor = isValid
                ? Color.FromArgb(23, 122, 88)
                : Color.FromArgb(166, 57, 54);
        }

        private static TextBox CreateSingleLineInput()
        {
            var font = BrandTheme.CreateFont(10f);
            return new TextBox
            {
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Top,
                AutoSize = false,
                Margin = new Padding(0, 0, 0, 4),
                Font = font,
                Height = UiMetrics.GetInputHeight(font),
                MinimumSize = new Size(0, UiMetrics.GetInputHeight(font))
            };
        }

        private static Button CreateActionButton(string text, Color backColor, int width)
        {
            var font = BrandTheme.CreateHeadingFont(9.6f, FontStyle.Bold);
            var button = new Button
            {
                Text = text,
                Width = width,
                MinimumSize = new Size(width, UiMetrics.GetButtonHeight(font)),
                BackColor = backColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = font,
                Margin = new Padding(0),
                Padding = UiMetrics.ButtonPadding
            };

            return button;
        }

        private static string T(string key) => AppLocalization.T(key);

        private static string F(string key, params object[] args) => AppLocalization.F(key, args);

        private static Label CreateGuideStepLabel(string text, int maxWidth)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                AutoEllipsis = false,
                MaximumSize = new Size(maxWidth, 0),
                Dock = DockStyle.Top,
                ForeColor = Color.FromArgb(190, 210, 232),
                Font = BrandTheme.CreateFont(9.6f, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 4)
            };
        }

        private void UpdateStepGuide(bool tokenValid, bool userValid)
        {
            var testValid = IsCurrentInputVerified();
            var activeStep = !tokenValid
                ? 1
                : !userValid
                    ? 5
                    : !testValid
                        ? 7
                        : 8;

            ApplyStepState(_lblStep1, tokenValid, activeStep == 1);
            ApplyStepState(_lblStep2, tokenValid, activeStep == 2);
            ApplyStepState(_lblStep3, tokenValid, activeStep == 3);
            ApplyStepState(_lblStep4, tokenValid, activeStep == 4);
            ApplyStepState(_lblStep5, userValid, activeStep == 5);
            ApplyStepState(_lblStep6, userValid, activeStep == 6);
            ApplyStepState(_lblStep7, testValid, activeStep == 7);
            ApplyStepState(_lblStep8, testValid, activeStep == 8);

            _lblStepHint.Text = activeStep switch
            {
                1 => T("initial.stepHint.token"),
                5 => T("initial.stepHint.user"),
                7 => T("initial.stepHint.test"),
                _ => T("initial.stepHint.finish")
            };
        }

        private static void ApplyStepState(Label label, bool completed, bool isActive)
        {
            if (completed)
            {
                label.ForeColor = Color.FromArgb(117, 226, 177);
                return;
            }

            if (isActive)
            {
                label.ForeColor = Color.FromArgb(255, 214, 138);
                return;
            }

            label.ForeColor = Color.FromArgb(190, 210, 232);
        }

        private void SetConnectionStatus(string text, Color foreColor)
        {
            _lblConnectionStatus.Text = text;
            _lblConnectionStatus.ForeColor = foreColor;
        }
    }
}


