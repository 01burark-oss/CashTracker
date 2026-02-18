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
            const int sidebarExpandedWidth = 360;
            const int sidebarCollapsedWidth = 92;
            var isSidebarExpanded = true;

            var sidebarBackground = BrandTheme.NavyDeep;
            var sidebarButton = BrandTheme.Navy;
            var sidebarButtonHover = Color.FromArgb(36, 77, 119);
            var sidebarAccent = Color.FromArgb(79, 199, 175);

            var contentBackground = Color.FromArgb(234, 240, 249);
            var surface = BrandTheme.Surface;
            var border = BrandTheme.Border;
            var heading = BrandTheme.Heading;
            var textMuted = BrandTheme.MutedText;

            SuspendLayout();
            Controls.Clear();

            var shell = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = contentBackground
            };
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, sidebarExpandedWidth));
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(shell);

            var sidebar = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = sidebarBackground,
                Padding = new Padding(18, 18, 18, 14)
            };
            shell.Controls.Add(sidebar, 0, 0);

            var sidebarLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };
            sidebarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            sidebar.Controls.Add(sidebarLayout);

            var brandRow = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 18)
            };
            brandRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            brandRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            brandRow.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            sidebarLayout.Controls.Add(brandRow, 0, 0);

            var logo = new BrandLogoControl
            {
                Size = new Size(52, 52),
                Margin = new Padding(0, 0, 10, 0),
                Anchor = AnchorStyles.Left
            };
            brandRow.Controls.Add(logo, 0, 0);

            var brandText = new Label
            {
                Text = "CASHTRACKER",
                ForeColor = Color.White,
                Font = BrandTheme.CreateHeadingFont(15f, FontStyle.Bold),
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 13, 0, 0)
            };
            brandRow.Controls.Add(brandText, 1, 0);

            var navContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };
            navContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            navContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            navContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            navContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            sidebarLayout.Controls.Add(navContainer, 0, 1);

            var navCaption = new Label
            {
                Text = "MENÜ",
                ForeColor = Color.FromArgb(157, 180, 207),
                Font = BrandTheme.CreateFont(8.8f, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(3, 0, 0, 10)
            };
            navContainer.Controls.Add(navCaption, 0, 0);

            var navButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Margin = new Padding(0)
            };
            navContainer.Controls.Add(navButtons, 0, 1);

            var btnGelirGider = CreateNavButton("Gelir / Gider Kayıtları", sidebarButton, Color.White, sidebarAccent, sidebarButtonHover);
            var btnSettings = CreateNavButton("Ayarlar", sidebarButton, Color.White, sidebarAccent, sidebarButtonHover);
            var btnChangeBot = CreateNavButton("Botu Değiştir", sidebarButton, Color.White, sidebarAccent, sidebarButtonHover);
            var btnUpdate = CreateNavButton("Güncellemeleri Denetle", sidebarButton, Color.White, sidebarAccent, sidebarButtonHover);

            navButtons.Controls.Add(btnGelirGider);
            navButtons.Controls.Add(btnSettings);
            navButtons.Controls.Add(btnChangeBot);
            navButtons.Controls.Add(btnUpdate);

            btnGelirGider.Click += (_, __) =>
            {
                using var form = new KasaForm(_kasaService, _isletmeService, _kalemTanimiService, _telegramApprovalService);
                form.ShowDialog(this);
                _ = RefreshSummariesAsync();
            };
            btnSettings.Click += (_, __) =>
            {
                using var form = new SettingsForm(_isletmeService, _kalemTanimiService, _telegramApprovalService);
                form.ShowDialog(this);
                _ = RefreshSummariesAsync();
            };
            btnChangeBot.Click += (_, __) => OpenBotSettings();
            btnUpdate.Click += async (_, __) => await CheckForUpdatesAsync(btnUpdate);

            var footerCredit = new Label
            {
                Text = "Burak Özmen",
                ForeColor = Color.FromArgb(159, 182, 208),
                Font = BrandTheme.CreateFont(9.2f, FontStyle.Regular),
                AutoSize = true,
                Margin = new Padding(3, 0, 0, 0)
            };
            navContainer.Controls.Add(footerCredit, 0, 2);

            var sidebarFooter = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                Margin = new Padding(0, 10, 0, 0)
            };
            sidebarLayout.Controls.Add(sidebarFooter, 0, 2);

            var btnSidebarToggle = new Button
            {
                Text = "<",
                Width = 36,
                Height = 32,
                BackColor = BrandTheme.Navy,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = BrandTheme.CreateFont(10f, FontStyle.Bold),
                Margin = new Padding(0)
            };
            btnSidebarToggle.FlatAppearance.BorderColor = Color.FromArgb(21, 38, 61);
            btnSidebarToggle.FlatAppearance.BorderSize = 1;
            btnSidebarToggle.FlatAppearance.MouseOverBackColor = Color.FromArgb(18, 53, 92);
            sidebarFooter.Controls.Add(btnSidebarToggle);

            var contentShell = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 1,
                BackColor = contentBackground
            };
            contentShell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            contentShell.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
            contentShell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            shell.Controls.Add(contentShell, 1, 0);

            var topPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = surface,
                Padding = new Padding(24, 16, 24, 14)
            };
            topPanel.Paint += (_, e) =>
            {
                using var pen = new Pen(border);
                e.Graphics.DrawLine(pen, 0, topPanel.Height - 1, topPanel.Width, topPanel.Height - 1);
            };
            contentShell.Controls.Add(topPanel, 0, 0);

            var topLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            topLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            topPanel.Controls.Add(topLayout);

            var titleStack = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            titleStack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            titleStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            titleStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            topLayout.Controls.Add(titleStack, 0, 0);

            var topTitle = new Label
            {
                Text = "Komut Paneli",
                Font = BrandTheme.CreateHeadingFont(16f, FontStyle.Bold),
                ForeColor = heading,
                AutoSize = true,
                Margin = new Padding(0, 2, 0, 4)
            };
            titleStack.Controls.Add(topTitle, 0, 0);

            _lblActiveBusinessTop = new Label
            {
                Text = "Aktif Isletme: -",
                Font = BrandTheme.CreateFont(9.6f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 74, 120),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 0)
            };
            titleStack.Controls.Add(_lblActiveBusinessTop, 0, 1);

            var badgeFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoSize = true,
                Margin = new Padding(0, 6, 0, 0)
            };
            topLayout.Controls.Add(badgeFlow, 1, 0);

            var dateBadge = CreateTopBadge(DateTime.Now.ToString("yyyy-MM-dd"), BrandTheme.Navy, Color.FromArgb(229, 239, 251));
            var telegramBadge = CreateTopBadge(
                _telegramSettings.IsEnabled ? "Telegram Aktif" : "Telegram Pasif",
                _telegramSettings.IsEnabled ? Color.FromArgb(22, 122, 87) : Color.FromArgb(166, 57, 54),
                _telegramSettings.IsEnabled ? Color.FromArgb(229, 246, 239) : Color.FromArgb(251, 237, 236));
            var localBadge = CreateTopBadge("Yerel Veri", Color.FromArgb(39, 75, 120), Color.FromArgb(229, 239, 251));

            badgeFlow.Controls.Add(dateBadge);
            badgeFlow.Controls.Add(telegramBadge);
            badgeFlow.Controls.Add(localBadge);

            var contentScroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = contentBackground,
                Padding = new Padding(24, 20, 24, 24)
            };
            contentShell.Controls.Add(contentScroll, 0, 1);

            var content = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 6
            };
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            contentScroll.Controls.Add(content);

            var sectionTitle = new Label
            {
                Text = "Finans Snapshot",
                Font = BrandTheme.CreateHeadingFont(18f, FontStyle.Bold),
                ForeColor = heading,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4)
            };
            content.Controls.Add(sectionTitle, 0, 0);

            var sectionSubtitle = new Label
            {
                Text = "Kritik KPI kartları ve rapor panelleri",
                Font = BrandTheme.CreateFont(9.8f, FontStyle.Regular),
                ForeColor = textMuted,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 14)
            };
            content.Controls.Add(sectionSubtitle, 0, 1);

            _lblActiveBusinessReport = new Label
            {
                Text = "Raporlar Aktif Isletme: -",
                Font = BrandTheme.CreateFont(9.2f, FontStyle.Bold),
                ForeColor = Color.FromArgb(42, 89, 139),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 14)
            };
            content.Controls.Add(_lblActiveBusinessReport, 0, 2);

            var dailyOverviewPanel = CreateDailyOverviewPanel(surface, border);
            content.Controls.Add(dailyOverviewPanel, 0, 3);

            var cardsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Margin = new Padding(0, 0, 0, 12)
            };
            content.Controls.Add(cardsPanel, 0, 4);

            _cardDaily = CreateSummaryCard("Günlük", surface, BrandTheme.Teal, "Telegram'a Gönder", border);
            _card30 = CreateSummaryCard("Son 30 Gün", surface, BrandTheme.Navy, "Telegram'a Gönder", border);
            _card365 = CreateSummaryCard("Son 365 Gün", surface, Color.FromArgb(88, 101, 178), "Telegram'a Gönder", border);

            _cardDaily.SendButton.Click += async (_, __) => await SendDailySummaryAsync(_cardDaily.SendButton);
            _card30.SendButton.Click += async (_, __) => await SendLast30SummaryAsync(_card30.SendButton);
            _card365.SendButton.Click += async (_, __) => await SendLast365SummaryAsync(_card365.SendButton);

            cardsPanel.Controls.Add(_cardDaily.Root);
            cardsPanel.Controls.Add(_card30.Root);
            cardsPanel.Controls.Add(_card365.Root);

            ResizeSummaryCards(cardsPanel, _cardDaily.Root, _card30.Root, _card365.Root);
            cardsPanel.Resize += (_, __) => ResizeSummaryCards(cardsPanel, _cardDaily.Root, _card30.Root, _card365.Root);

            var reportGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0)
            };
            reportGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            reportGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            reportGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            content.Controls.Add(reportGrid, 0, 5);

            var monthlyPanel = CreatePeriodReportPanel(
                "Aylık Finans",
                "Seçili ay özeti",
                "Aylığı Gönder",
                BrandTheme.Navy,
                out _cmbMonth,
                out _lblMonthIncome,
                out _lblMonthExpense,
                out _lblMonthNet,
                out var btnSendMonth);

            var yearlyPanel = CreatePeriodReportPanel(
                "Yıllık Finans",
                "Seçili yıl özeti",
                "Yıllığı Gönder",
                Color.FromArgb(80, 96, 174),
                out _cmbYear,
                out _lblYearIncome,
                out _lblYearExpense,
                out _lblYearNet,
                out var btnSendYear);

            reportGrid.Controls.Add(monthlyPanel, 0, 0);
            reportGrid.Controls.Add(yearlyPanel, 1, 0);

            _cmbMonth.SelectedIndexChanged += async (_, __) => await RefreshMonthlyAsync();
            _cmbYear.SelectedIndexChanged += async (_, __) => await RefreshYearlyAsync();
            btnSendMonth.Click += async (_, __) => await SendMonthlySummaryAsync(btnSendMonth);
            btnSendYear.Click += async (_, __) => await SendYearlySummaryAsync(btnSendYear);

            void ApplyReportGridLayout()
            {
                var compact = contentScroll.ClientSize.Width < 980;
                reportGrid.SuspendLayout();
                reportGrid.ColumnStyles.Clear();
                reportGrid.RowStyles.Clear();

                if (compact)
                {
                    reportGrid.ColumnCount = 1;
                    reportGrid.RowCount = 1;
                    reportGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                    reportGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                    reportGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                    reportGrid.SetColumn(monthlyPanel, 0);
                    reportGrid.SetRow(monthlyPanel, 0);
                    reportGrid.SetColumn(yearlyPanel, 0);
                    reportGrid.SetRow(yearlyPanel, 1);
                    monthlyPanel.Margin = new Padding(0, 0, 0, 12);
                    yearlyPanel.Margin = new Padding(0);
                }
                else
                {
                    reportGrid.ColumnCount = 2;
                    reportGrid.RowCount = 1;
                    reportGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
                    reportGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
                    reportGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                    reportGrid.SetColumn(monthlyPanel, 0);
                    reportGrid.SetRow(monthlyPanel, 0);
                    reportGrid.SetColumn(yearlyPanel, 1);
                    reportGrid.SetRow(yearlyPanel, 0);
                    monthlyPanel.Margin = new Padding(0, 0, 8, 0);
                    yearlyPanel.Margin = new Padding(8, 0, 0, 0);
                }

                reportGrid.ResumeLayout();
            }

            var navItems = new[]
            {
                new { Button = btnGelirGider, ExpandedText = "Gelir / Gider Kayıtları", CollapsedText = "KG" },
                new { Button = btnSettings, ExpandedText = "Ayarlar", CollapsedText = "SET" },
                new { Button = btnChangeBot, ExpandedText = "Botu Değiştir", CollapsedText = "BOT" },
                new { Button = btnUpdate, ExpandedText = "Güncellemeleri Denetle", CollapsedText = "UPD" }
            };

            void ApplySidebarState()
            {
                shell.ColumnStyles[0].Width = isSidebarExpanded ? sidebarExpandedWidth : sidebarCollapsedWidth;
                navCaption.Visible = isSidebarExpanded;
                footerCredit.Visible = isSidebarExpanded;
                brandText.Visible = isSidebarExpanded;

                var buttonWidth = isSidebarExpanded ? 300 : 54;
                foreach (var item in navItems)
                {
                    item.Button.Text = isSidebarExpanded ? item.ExpandedText : item.CollapsedText;
                    item.Button.Width = buttonWidth;
                    item.Button.TextAlign = isSidebarExpanded ? ContentAlignment.MiddleLeft : ContentAlignment.MiddleCenter;
                    item.Button.Padding = isSidebarExpanded ? new Padding(14, 0, 0, 0) : new Padding(0);
                }

                logo.Margin = isSidebarExpanded ? new Padding(0, 0, 10, 0) : new Padding(0);
                btnSidebarToggle.Text = isSidebarExpanded ? "<" : ">";
            }

            btnSidebarToggle.Click += (_, __) =>
            {
                isSidebarExpanded = !isSidebarExpanded;
                ApplySidebarState();
            };

            contentScroll.Resize += (_, __) => ApplyReportGridLayout();
            ApplyReportGridLayout();
            ApplySidebarState();

            LoadMonths();
            LoadYears();

            ResumeLayout(true);
        }

        private static Panel CreatePeriodReportPanel(
            string title,
            string subtitle,
            string actionText,
            Color actionColor,
            out ComboBox selector,
            out Label income,
            out Label expense,
            out Label net,
            out Button actionButton)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BrandTheme.Surface,
                Padding = new Padding(18, 16, 18, 16),
                MinimumSize = new Size(360, 224)
            };
            panel.Paint += (_, e) => ControlPaint.DrawBorder(e.Graphics, panel.ClientRectangle, BrandTheme.Border, ButtonBorderStyle.Solid);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.Controls.Add(layout);

            var header = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 1,
                RowCount = 1,
                AutoSize = true
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            header.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            header.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(header, 0, 0);

            var titleLabel = new Label
            {
                Text = title,
                Font = BrandTheme.CreateHeadingFont(13f, FontStyle.Bold),
                ForeColor = BrandTheme.Heading,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4)
            };
            header.Controls.Add(titleLabel, 0, 0);

            var subtitleLabel = new Label
            {
                Text = subtitle,
                Font = BrandTheme.CreateFont(9.3f, FontStyle.Regular),
                ForeColor = BrandTheme.MutedText,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 10)
            };
            header.Controls.Add(subtitleLabel, 0, 1);

            var actionRow = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 12)
            };
            actionRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            actionRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            actionRow.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(actionRow, 0, 1);

            selector = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 2, 10, 2)
            };
            actionRow.Controls.Add(selector, 0, 0);

            actionButton = CreatePanelActionButton(actionText, actionColor, Color.White);
            actionButton.Width = 160;
            actionButton.Margin = new Padding(0);
            actionRow.Controls.Add(actionButton, 1, 0);

            var stats = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };
            stats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            stats.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            stats.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            stats.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(stats, 0, 2);

            income = new Label
            {
                AutoSize = true,
                ForeColor = Color.FromArgb(17, 121, 85),
                Font = BrandTheme.CreateFont(10f, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 8)
            };
            expense = new Label
            {
                AutoSize = true,
                ForeColor = Color.FromArgb(173, 59, 56),
                Font = BrandTheme.CreateFont(10f, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 8)
            };
            net = new Label
            {
                AutoSize = true,
                ForeColor = Color.FromArgb(35, 52, 75),
                Font = BrandTheme.CreateHeadingFont(11.3f, FontStyle.Bold),
                Margin = new Padding(0)
            };

            stats.Controls.Add(income, 0, 0);
            stats.Controls.Add(expense, 0, 1);
            stats.Controls.Add(net, 0, 2);

            return panel;
        }
    }
}

