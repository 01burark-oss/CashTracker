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
                Width = 330,
                Height = 236,
                MinimumSize = new Size(280, 220),
                BackColor = backColor,
                Margin = new Padding(0, 0, 16, 16),
                Padding = new Padding(18, 16, 18, 14)
            };

            panel.Paint += (_, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, panel.ClientRectangle, borderColor, ButtonBorderStyle.Solid);
                using var accentPen = new Pen(accent, 3f);
                e.Graphics.DrawLine(accentPen, 10, 9, panel.Width - 12, 9);
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.Controls.Add(layout);

            var titleLabel = new Label
            {
                Text = title,
                Font = BrandTheme.CreateHeadingFont(11.5f, FontStyle.Bold),
                ForeColor = accent,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 12)
            };
            layout.Controls.Add(titleLabel, 0, 0);

            var metrics = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 1,
                RowCount = 3,
                AutoSize = true,
                Margin = new Padding(0)
            };
            metrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            metrics.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            metrics.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            metrics.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(metrics, 0, 1);

            var income = new Label
            {
                AutoSize = true,
                ForeColor = Color.FromArgb(17, 121, 85),
                Font = BrandTheme.CreateFont(10f, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 8)
            };
            var expense = new Label
            {
                AutoSize = true,
                ForeColor = Color.FromArgb(173, 59, 56),
                Font = BrandTheme.CreateFont(10f, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 8)
            };
            var net = new Label
            {
                AutoSize = true,
                ForeColor = Color.FromArgb(35, 49, 74),
                Font = BrandTheme.CreateHeadingFont(11.6f, FontStyle.Bold),
                Margin = new Padding(0)
            };

            metrics.Controls.Add(income, 0, 0);
            metrics.Controls.Add(expense, 0, 1);
            metrics.Controls.Add(net, 0, 2);

            var sendButton = new Button
            {
                Text = buttonText,
                Dock = DockStyle.Top,
                Height = 38,
                BackColor = BrandTheme.Navy,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = BrandTheme.CreateFont(10f, FontStyle.Bold),
                Margin = new Padding(0, 10, 0, 0)
            };
            sendButton.FlatAppearance.BorderColor = Color.FromArgb(21, 38, 61);
            sendButton.FlatAppearance.BorderSize = 1;
            sendButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(18, 53, 92);
            layout.Controls.Add(sendButton, 0, 3);

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
