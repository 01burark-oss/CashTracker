using System;
using System.Drawing;
using System.Windows.Forms;
using CashTracker.App.Controls;
using CashTracker.App.Forms;
using CashTracker.App.UI;

namespace CashTracker.App
{
    internal sealed partial class MainForm
    {
        private void BuildUi()
        {
            var sidebarBg = BrandTheme.NavyDeep;
            var sidebarButton = BrandTheme.Navy;
            var sidebarButtonHover = Color.FromArgb(39, 69, 106);
            var accent = BrandTheme.Navy;
            var accentSecondary = BrandTheme.Teal;
            var mainBg = Color.FromArgb(239, 244, 250);
            var surface = BrandTheme.Surface;
            var border = BrandTheme.Border;
            var heading = BrandTheme.Heading;
            var textMuted = BrandTheme.MutedText;
            const int sidebarExpandedWidth = 272;
            const int sidebarCollapsedWidth = 96;
            var isSidebarExpanded = true;

            var shell = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, sidebarExpandedWidth));
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(shell);

            var left = new Panel { Dock = DockStyle.Fill, BackColor = sidebarBg };
            var rightShell = new Panel { Dock = DockStyle.Fill, BackColor = mainBg };
            var top = new Panel { Dock = DockStyle.Top, Height = 92, BackColor = surface };
            var main = new Panel { Dock = DockStyle.Fill, BackColor = mainBg };
            rightShell.Controls.Add(main);
            rightShell.Controls.Add(top);
            shell.Controls.Add(left, 0, 0);
            shell.Controls.Add(rightShell, 1, 0);

            var btnSidebarToggle = new Button
            {
                Text = "<",
                Width = 36,
                Height = 32,
                Location = new Point(24, 28),
                BackColor = Color.FromArgb(232, 241, 252),
                ForeColor = accent,
                FlatStyle = FlatStyle.Flat,
                Font = BrandTheme.CreateFont(10f, FontStyle.Bold),
                TabStop = false
            };
            btnSidebarToggle.FlatAppearance.BorderColor = border;
            btnSidebarToggle.FlatAppearance.BorderSize = 1;
            btnSidebarToggle.FlatAppearance.MouseOverBackColor = Color.FromArgb(219, 234, 250);
            top.Controls.Add(btnSidebarToggle);

            left.Paint += (_, e) =>
            {
                ControlPaint.DrawBorder(
                    e.Graphics,
                    left.ClientRectangle,
                    Color.FromArgb(31, 47, 71),
                    ButtonBorderStyle.Solid);
            };
            top.Paint += (_, e) =>
            {
                using var pen = new Pen(border);
                e.Graphics.DrawLine(pen, 0, top.Height - 1, top.Width, top.Height - 1);
            };

            var sidebarLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(20, 18, 20, 14),
                BackColor = sidebarBg
            };
            sidebarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            left.Controls.Add(sidebarLayout);

            var brandPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70
            };
            sidebarLayout.Controls.Add(brandPanel, 0, 0);

            var logo = new BrandLogoControl
            {
                Location = new Point(0, 4)
            };
            brandPanel.Controls.Add(logo);

            var title = new Label
            {
                Text = "CASHTRACKER",
                Font = BrandTheme.CreateHeadingFont(15.5f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = false,
                AutoEllipsis = false,
                Location = new Point(66, 12),
                Height = 28,
                TextAlign = ContentAlignment.MiddleLeft
            };
            brandPanel.Controls.Add(title);
            void LayoutBrandTitle()
            {
                if (isSidebarExpanded)
                {
                    title.Visible = true;
                    title.Location = new Point(66, 12);
                    title.Width = Math.Max(brandPanel.ClientSize.Width - 68, 160);
                    logo.Location = new Point(0, 4);
                }
                else
                {
                    title.Visible = false;
                    logo.Location = new Point(Math.Max((brandPanel.ClientSize.Width - logo.Width) / 2, 0), 4);
                }
            }
            brandPanel.Resize += (_, __) => LayoutBrandTitle();
            LayoutBrandTitle();

            var navPanel = new Panel
            {
                Dock = DockStyle.Fill
            };
            sidebarLayout.Controls.Add(navPanel, 0, 1);

            var navSection = new Label
            {
                Text = "MODÜLLER",
                Font = BrandTheme.CreateFont(8.7f, FontStyle.Bold),
                ForeColor = Color.FromArgb(146, 167, 190),
                AutoSize = true,
                Location = new Point(2, 8)
            };
            navPanel.Controls.Add(navSection);

            var navButtons = new FlowLayoutPanel
            {
                Location = new Point(0, 32),
                Width = 228,
                Height = 220,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            navPanel.Controls.Add(navButtons);

            var btnGelirGider = CreateNavButton("Gelir / Gider Kayıtları", sidebarButton, Color.White, accent, sidebarButtonHover);
            navButtons.Controls.Add(btnGelirGider);
            btnGelirGider.Click += (_, __) =>
            {
                using var form = new KasaForm(_kasaService);
                form.ShowDialog(this);
                _ = RefreshSummariesAsync();
            };

            var btnChangeBot = CreateNavButton("Botu Değiştir", sidebarButton, Color.White, accent, sidebarButtonHover);
            navButtons.Controls.Add(btnChangeBot);
            btnChangeBot.Click += (_, __) => OpenBotSettings();

            var btnUpdate = CreateNavButton("Güncellemeleri Denetle", sidebarButton, Color.White, accent, sidebarButtonHover);
            navButtons.Controls.Add(btnUpdate);
            btnUpdate.Click += async (_, __) => await CheckForUpdatesAsync(btnUpdate);

            var navItems = new[]
            {
                new { Button = btnGelirGider, ExpandedText = btnGelirGider.Text, CollapsedText = "KG" },
                new { Button = btnChangeBot, ExpandedText = btnChangeBot.Text, CollapsedText = "BOT" },
                new { Button = btnUpdate, ExpandedText = btnUpdate.Text, CollapsedText = "GNC" }
            };

            var sidebarTooltips = new ToolTip { ShowAlways = true };
            foreach (var item in navItems)
            {
                sidebarTooltips.SetToolTip(item.Button, item.ExpandedText);
            }

            var credit = new Label
            {
                Text = "Burak Özmen tarafından hazırlandı",
                ForeColor = Color.FromArgb(150, 168, 189),
                Font = BrandTheme.CreateFont(9f, FontStyle.Regular),
                AutoSize = true,
                Margin = new Padding(0, 4, 0, 0)
            };
            sidebarLayout.Controls.Add(credit, 0, 2);

            void ApplySidebarState()
            {
                shell.ColumnStyles[0].Width = isSidebarExpanded ? sidebarExpandedWidth : sidebarCollapsedWidth;
                sidebarLayout.Padding = isSidebarExpanded
                    ? new Padding(20, 18, 20, 14)
                    : new Padding(12, 18, 12, 14);

                navSection.Visible = isSidebarExpanded;
                credit.Visible = isSidebarExpanded;
                navButtons.Location = new Point(0, isSidebarExpanded ? 32 : 8);
                navButtons.Width = Math.Max(navPanel.ClientSize.Width, isSidebarExpanded ? 180 : 64);

                var buttonWidth = Math.Max(navButtons.ClientSize.Width - 2, isSidebarExpanded ? 170 : 64);
                foreach (var item in navItems)
                {
                    item.Button.Text = isSidebarExpanded ? item.ExpandedText : item.CollapsedText;
                    item.Button.Width = buttonWidth;
                    item.Button.TextAlign = isSidebarExpanded
                        ? ContentAlignment.MiddleLeft
                        : ContentAlignment.MiddleCenter;
                    item.Button.Padding = isSidebarExpanded ? new Padding(14, 0, 0, 0) : new Padding(0);
                }

                btnSidebarToggle.Text = isSidebarExpanded ? "<" : ">";
                LayoutBrandTitle();
            }

            btnSidebarToggle.Click += (_, __) =>
            {
                isSidebarExpanded = !isSidebarExpanded;
                ApplySidebarState();
            };
            navPanel.Resize += (_, __) => ApplySidebarState();
            ApplySidebarState();

            var topTitle = new Label
            {
                Text = "Finans Yönetim Paneli",
                Font = BrandTheme.CreateFont(15f, FontStyle.Bold),
                ForeColor = heading,
                AutoSize = true,
                Location = new Point(76, 16)
            };
            top.Controls.Add(topTitle);

            var topSubtitle = new Label
            {
                Text = "Gelir, gider ve dönemsel performans özetleri",
                Font = BrandTheme.CreateFont(9.75f, FontStyle.Regular),
                ForeColor = textMuted,
                AutoSize = true,
                Location = new Point(77, 46)
            };
            top.Controls.Add(topSubtitle);

            var dateBadge = new Label
            {
                Text = DateTime.Now.ToString("yyyy-MM-dd"),
                Font = BrandTheme.CreateFont(10f, FontStyle.Bold),
                ForeColor = accent,
                BackColor = Color.FromArgb(232, 241, 252),
                BorderStyle = BorderStyle.FixedSingle,
                AutoSize = true,
                Padding = new Padding(16, 7, 16, 7)
            };
            top.Controls.Add(dateBadge);

            var telegramBadge = CreateTopBadge(
                _telegramSettings.IsEnabled ? "Telegram: Aktif" : "Telegram: Pasif",
                _telegramSettings.IsEnabled ? Color.FromArgb(22, 122, 87) : Color.FromArgb(166, 57, 54),
                _telegramSettings.IsEnabled ? Color.FromArgb(232, 248, 241) : Color.FromArgb(251, 237, 236));
            top.Controls.Add(telegramBadge);

            var dataBadge = CreateTopBadge(
                "Veri: Yerel",
                Color.FromArgb(31, 72, 117),
                Color.FromArgb(232, 241, 252));
            top.Controls.Add(dataBadge);

            PositionTopBadges(top, dateBadge, telegramBadge, dataBadge);
            top.Resize += (_, __) => PositionTopBadges(top, dateBadge, telegramBadge, dataBadge);

            var content = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(28, 18, 28, 20),
                ColumnCount = 1,
                RowCount = 4
            };
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 36));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 32));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 32));
            main.Controls.Add(content);

            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56
            };
            content.Controls.Add(headerPanel, 0, 0);

            var header = new Label
            {
                Text = "Genel Finans Özeti",
                Font = BrandTheme.CreateHeadingFont(18f, FontStyle.Bold),
                ForeColor = heading,
                AutoSize = true,
                Location = new Point(0, 0)
            };
            headerPanel.Controls.Add(header);

            var headerMeta = new Label
            {
                Text = "Güncel dönem KPI kartları ve rapor aksiyonları",
                Font = BrandTheme.CreateFont(9.6f, FontStyle.Regular),
                ForeColor = textMuted,
                AutoSize = true,
                Location = new Point(1, 30)
            };
            headerPanel.Controls.Add(headerMeta);

            var cardsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoScroll = false,
                Padding = new Padding(0, 10, 0, 0),
                Margin = new Padding(0, 6, 0, 0)
            };
            content.Controls.Add(cardsPanel, 0, 1);

            _cardDaily = CreateSummaryCard("Günlük (Bugün)", surface, accentSecondary, "Telegram'a Gönder", border);
            _card30 = CreateSummaryCard("Son 30 Gün", surface, accent, "Telegram'a Gönder", border);
            _card365 = CreateSummaryCard("Son 365 Gün", surface, Color.FromArgb(69, 95, 153), "Telegram'a Gönder", border);

            _cardDaily.SendButton.Click += async (_, __) => await SendDailySummaryAsync(_cardDaily.SendButton);
            _card30.SendButton.Click += async (_, __) => await SendLast30SummaryAsync(_card30.SendButton);
            _card365.SendButton.Click += async (_, __) => await SendLast365SummaryAsync(_card365.SendButton);

            cardsPanel.Controls.Add(_cardDaily.Root);
            cardsPanel.Controls.Add(_card30.Root);
            cardsPanel.Controls.Add(_card365.Root);
            ResizeSummaryCards(cardsPanel, _cardDaily.Root, _card30.Root, _card365.Root);
            cardsPanel.Resize += (_, __) => ResizeSummaryCards(cardsPanel, _cardDaily.Root, _card30.Root, _card365.Root);

            var monthlyPanel = new Panel
            {
                BackColor = surface,
                Padding = new Padding(20, 18, 20, 16),
                Margin = new Padding(0, 14, 0, 0),
                Dock = DockStyle.Fill
            };
            monthlyPanel.Paint += (_, e) => ControlPaint.DrawBorder(e.Graphics, monthlyPanel.ClientRectangle, border, ButtonBorderStyle.Solid);
            content.Controls.Add(monthlyPanel, 0, 2);

            var monthlyTitle = new Label
            {
                Text = "Aylık Finans Raporu",
                Font = BrandTheme.CreateFont(13f, FontStyle.Bold),
                ForeColor = heading,
                AutoSize = true,
                Location = new Point(0, 0)
            };
            monthlyPanel.Controls.Add(monthlyTitle);

            var monthlyMeta = new Label
            {
                Text = "Seçili ay için gelir, gider ve net durum",
                Font = BrandTheme.CreateFont(9.25f, FontStyle.Regular),
                ForeColor = textMuted,
                AutoSize = true,
                Location = new Point(0, 22)
            };
            monthlyPanel.Controls.Add(monthlyMeta);

            _cmbMonth = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 240,
                Location = new Point(0, 50),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _cmbMonth.SelectedIndexChanged += async (_, __) => await RefreshMonthlyAsync();
            monthlyPanel.Controls.Add(_cmbMonth);

            _lblMonthIncome = new Label
            {
                AutoSize = true,
                Location = new Point(0, 96),
                ForeColor = Color.FromArgb(20, 117, 92),
                Font = BrandTheme.CreateFont(10f, FontStyle.Bold)
            };
            _lblMonthExpense = new Label
            {
                AutoSize = true,
                Location = new Point(0, 122),
                ForeColor = Color.FromArgb(166, 57, 54),
                Font = BrandTheme.CreateFont(10f, FontStyle.Bold)
            };
            _lblMonthNet = new Label
            {
                AutoSize = true,
                Location = new Point(0, 149),
                ForeColor = Color.FromArgb(35, 52, 75),
                Font = BrandTheme.CreateFont(11.2f, FontStyle.Bold)
            };

            monthlyPanel.Controls.Add(_lblMonthIncome);
            monthlyPanel.Controls.Add(_lblMonthExpense);
            monthlyPanel.Controls.Add(_lblMonthNet);

            var btnSendMonth = CreatePanelActionButton("Aylığı Gönder", accent, Color.White);
            btnSendMonth.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSendMonth.Click += async (_, __) => await SendMonthlySummaryAsync(btnSendMonth);
            monthlyPanel.Controls.Add(btnSendMonth);
            LayoutPeriodPanel(monthlyPanel, _cmbMonth, _lblMonthIncome, _lblMonthExpense, _lblMonthNet, btnSendMonth);
            monthlyPanel.Resize += (_, __) => LayoutPeriodPanel(monthlyPanel, _cmbMonth, _lblMonthIncome, _lblMonthExpense, _lblMonthNet, btnSendMonth);

            var yearlyPanel = new Panel
            {
                BackColor = surface,
                Padding = new Padding(20, 18, 20, 16),
                Margin = new Padding(0, 14, 0, 0),
                Dock = DockStyle.Fill
            };
            yearlyPanel.Paint += (_, e) => ControlPaint.DrawBorder(e.Graphics, yearlyPanel.ClientRectangle, border, ButtonBorderStyle.Solid);
            content.Controls.Add(yearlyPanel, 0, 3);

            var yearlyTitle = new Label
            {
                Text = "Yıllık Finans Raporu",
                Font = BrandTheme.CreateFont(13f, FontStyle.Bold),
                ForeColor = heading,
                AutoSize = true,
                Location = new Point(0, 0)
            };
            yearlyPanel.Controls.Add(yearlyTitle);

            var yearlyMeta = new Label
            {
                Text = "Seçili yıl için özet performans",
                Font = BrandTheme.CreateFont(9.25f, FontStyle.Regular),
                ForeColor = textMuted,
                AutoSize = true,
                Location = new Point(0, 22)
            };
            yearlyPanel.Controls.Add(yearlyMeta);

            _cmbYear = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 240,
                Location = new Point(0, 50),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _cmbYear.SelectedIndexChanged += async (_, __) => await RefreshYearlyAsync();
            yearlyPanel.Controls.Add(_cmbYear);

            _lblYearIncome = new Label
            {
                AutoSize = true,
                Location = new Point(0, 96),
                ForeColor = Color.FromArgb(20, 117, 92),
                Font = BrandTheme.CreateFont(10f, FontStyle.Bold)
            };
            _lblYearExpense = new Label
            {
                AutoSize = true,
                Location = new Point(0, 122),
                ForeColor = Color.FromArgb(166, 57, 54),
                Font = BrandTheme.CreateFont(10f, FontStyle.Bold)
            };
            _lblYearNet = new Label
            {
                AutoSize = true,
                Location = new Point(0, 149),
                ForeColor = Color.FromArgb(35, 52, 75),
                Font = BrandTheme.CreateFont(11.2f, FontStyle.Bold)
            };

            yearlyPanel.Controls.Add(_lblYearIncome);
            yearlyPanel.Controls.Add(_lblYearExpense);
            yearlyPanel.Controls.Add(_lblYearNet);

            var btnSendYear = CreatePanelActionButton("Yıllığı Gönder", accent, Color.White);
            btnSendYear.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSendYear.Click += async (_, __) => await SendYearlySummaryAsync(btnSendYear);
            yearlyPanel.Controls.Add(btnSendYear);
            LayoutPeriodPanel(yearlyPanel, _cmbYear, _lblYearIncome, _lblYearExpense, _lblYearNet, btnSendYear);
            yearlyPanel.Resize += (_, __) => LayoutPeriodPanel(yearlyPanel, _cmbYear, _lblYearIncome, _lblYearExpense, _lblYearNet, btnSendYear);

            LoadMonths();
            LoadYears();
        }
    }
}

