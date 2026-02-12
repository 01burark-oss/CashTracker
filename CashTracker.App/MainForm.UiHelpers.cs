using System;
using System.Drawing;
using System.Windows.Forms;
using CashTracker.App.UI;

namespace CashTracker.App
{
    internal sealed partial class MainForm
    {
        private static Label CreateTopBadge(string text, Color fore, Color back)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Font = BrandTheme.CreateFont(9.2f, FontStyle.Bold),
                ForeColor = fore,
                BackColor = back,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(12, 7, 12, 7)
            };
        }

        private static void PositionTopBadges(Control parent, Control rightMost, params Control[] badges)
        {
            var x = Math.Max(parent.ClientSize.Width - rightMost.Width - 24, 0);
            rightMost.Location = new Point(x, 26);

            foreach (var badge in badges)
            {
                x -= badge.Width + 10;
                badge.Location = new Point(Math.Max(x, 0), 26);
            }
        }

        private static void PositionRightOnPanel(Panel panel, Control child, int right, int top)
        {
            child.Location = new Point(Math.Max(panel.ClientSize.Width - child.Width - right, 0), top);
        }

        private static void LayoutPeriodPanel(
            Panel panel,
            ComboBox selector,
            Label income,
            Label expense,
            Label net,
            Button actionButton)
        {
            var compact = panel.ClientSize.Width < 900;
            var rightPadding = 20;

            var buttonTop = compact ? 178 : 50;
            PositionRightOnPanel(panel, actionButton, rightPadding, buttonTop);

            if (compact)
            {
                selector.Width = Math.Max(panel.ClientSize.Width - panel.Padding.Horizontal - 4, 220);
                income.Location = new Point(0, 96);
                expense.Location = new Point(0, 122);
                net.Location = new Point(0, 149);
            }
            else
            {
                var selectorMax = Math.Max(panel.ClientSize.Width - panel.Padding.Horizontal - actionButton.Width - 120, 220);
                selector.Width = Math.Min(selectorMax, 320);

                var statsX = selector.Right + 30;
                income.Location = new Point(statsX, 96);
                expense.Location = new Point(statsX, 122);
                net.Location = new Point(statsX, 149);
            }
        }

        private static void ResizeSummaryCards(FlowLayoutPanel panel, params Panel[] cards)
        {
            if (cards.Length == 0)
                return;

            var availableWidth = Math.Max(panel.ClientSize.Width - panel.Padding.Horizontal, 0);
            var columns = availableWidth switch
            {
                >= 1020 => 3,
                >= 680 => 2,
                _ => 1
            };

            const int gap = 18;
            var totalGap = gap * Math.Max(columns - 1, 0);
            var cardWidth = Math.Max((availableWidth - totalGap) / columns, 260);
            cardWidth = Math.Min(cardWidth, 420);

            for (var i = 0; i < cards.Length; i++)
            {
                cards[i].Width = cardWidth;
                cards[i].Margin = new Padding(0, 0, gap, 14);
            }
        }

        private static Button CreateNavButton(string text, Color back, Color fore, Color border, Color hover)
        {
            var button = new Button
            {
                Text = text,
                Width = 226,
                Height = 42,
                Margin = new Padding(0, 0, 0, 10),
                BackColor = back,
                ForeColor = fore,
                Font = BrandTheme.CreateFont(10f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };

            button.FlatAppearance.BorderColor = border;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(27, 48, 76);
            button.FlatAppearance.MouseOverBackColor = hover;

            return button;
        }

        private static Button CreatePanelActionButton(string text, Color back, Color fore)
        {
            var button = new Button
            {
                Text = text,
                Width = 188,
                Height = 34,
                BackColor = back,
                ForeColor = fore,
                Font = BrandTheme.CreateFont(10f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };

            button.FlatAppearance.BorderColor = back;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(
                Math.Max(back.R - 15, 0),
                Math.Max(back.G - 15, 0),
                Math.Max(back.B - 15, 0));

            return button;
        }
    }
}
