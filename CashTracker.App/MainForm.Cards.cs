using System;
using System.Drawing;
using System.Windows.Forms;
using CashTracker.App.UI;
using CashTracker.Core.Models;

namespace CashTracker.App
{
    internal sealed partial class MainForm
    {
        private static SummaryCard CreateSummaryCard(string title, Color backColor, Color accent, string buttonText, Color borderColor)
        {
            var panel = new Panel
            {
                Width = 292,
                Height = 196,
                MinimumSize = new Size(260, 196),
                BackColor = backColor,
                Margin = new Padding(0, 0, 18, 14),
                Padding = new Padding(16, 14, 16, 12)
            };
            panel.Paint += (_, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, panel.ClientRectangle, borderColor, ButtonBorderStyle.Solid);
                using var accentPen = new Pen(Color.FromArgb(accent.R, accent.G, accent.B), 1.8f);
                e.Graphics.DrawLine(accentPen, 8, 8, panel.Width - 10, 8);
            };

            var titleLabel = new Label
            {
                Text = title,
                Font = BrandTheme.CreateFont(10.5f, FontStyle.Bold),
                ForeColor = accent,
                AutoSize = true,
                Location = new Point(10, 12)
            };
            panel.Controls.Add(titleLabel);

            var income = new Label
            {
                AutoSize = true,
                Location = new Point(10, 52),
                ForeColor = Color.FromArgb(20, 117, 92)
            };
            var expense = new Label
            {
                AutoSize = true,
                Location = new Point(10, 79),
                ForeColor = Color.FromArgb(166, 57, 54)
            };
            var net = new Label
            {
                AutoSize = true,
                Location = new Point(10, 108),
                Font = BrandTheme.CreateFont(11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(35, 52, 75)
            };

            var sendButton = new Button
            {
                Text = buttonText,
                Width = 240,
                Height = 34,
                Location = new Point(10, 145),
                BackColor = accent,
                ForeColor = Color.White,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                FlatStyle = FlatStyle.Flat
            };
            sendButton.FlatAppearance.BorderColor = accent;
            sendButton.FlatAppearance.BorderSize = 0;
            sendButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(
                Math.Max(accent.R - 15, 0),
                Math.Max(accent.G - 15, 0),
                Math.Max(accent.B - 15, 0));

            panel.Controls.Add(income);
            panel.Controls.Add(expense);
            panel.Controls.Add(net);
            panel.Controls.Add(sendButton);

            return new SummaryCard
            {
                Root = panel,
                Income = income,
                Expense = expense,
                Net = net,
                SendButton = sendButton
            };
        }

        private static void ApplySummary(SummaryCard card, PeriodSummary s)
        {
            card.Income.Text = $"Gelir: {s.IncomeTotal:n2}";
            card.Expense.Text = $"Gider: {s.ExpenseTotal:n2}";
            card.Net.Text = $"Net: {s.Net:n2}";
        }
    }
}
