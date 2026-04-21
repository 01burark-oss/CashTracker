using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using CashTracker.App.UI;

namespace CashTracker.App.Controls
{
    internal sealed class DashboardDonutChartControl : Control
    {
        private IReadOnlyList<DashboardDonutSlice> _slices = Array.Empty<DashboardDonutSlice>();

        public DashboardDonutChartControl()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true);

            BackColor = Color.White;
            Size = new Size(420, 220);
        }

        public bool ShowOuterLabels { get; set; }

        public IReadOnlyList<DashboardDonutSlice> Slices
        {
            get => _slices;
            set
            {
                _slices = value ?? Array.Empty<DashboardDonutSlice>();
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var resolvedBackColor = UiMetrics.ResolveEffectiveBackColor(this, Color.White);
            e.Graphics.Clear(resolvedBackColor);

            var rect = ClientRectangle;
            if (rect.Width < 180 || rect.Height < 120)
                return;

            var slices = _slices.Count == 0
                ? new[]
                {
                    new DashboardDonutSlice { Label = "Nakit", Amount = 0m, Color = Color.FromArgb(205, 214, 228) }
                }
                : _slices;

            var total = slices.Sum(x => Math.Abs(x.Amount));
            var normalizedTotal = total <= 0m ? 1m : total;

            var donutSize = Math.Min(rect.Height - 26, 170);
            var donutRect = new RectangleF(rect.Width * 0.5f - donutSize * 0.5f, rect.Height * 0.5f - donutSize * 0.5f, donutSize, donutSize);
            var ringThickness = Math.Max(18f, donutSize * 0.2f);

            using var backgroundPen = new Pen(Color.FromArgb(232, 236, 242), ringThickness)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };
            e.Graphics.DrawArc(backgroundPen, donutRect, 0, 360);

            var startAngle = -90f;
            DashboardDonutSlice? maxSlice = null;
            foreach (var slice in slices)
            {
                if (maxSlice is null || Math.Abs(slice.Amount) > Math.Abs(maxSlice.Amount))
                    maxSlice = slice;

                var sweep = total <= 0m ? 360f / slices.Count : (float)(Math.Abs(slice.Amount) / normalizedTotal * 360m);
                using var pen = new Pen(slice.Color, ringThickness)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round
                };
                e.Graphics.DrawArc(pen, donutRect, startAngle, sweep);
                if (ShowOuterLabels)
                    DrawLabel(e.Graphics, donutRect, startAngle + (sweep / 2f), slice, normalizedTotal, total > 0m);
                startAngle += sweep;
            }

            var centerText = total > 0m ? "100%" : "0%";
            using var centerFont = BrandTheme.CreateHeadingFont(16f, FontStyle.Bold);
            using var centerBrush = new SolidBrush(BrandTheme.Heading);
            using var centerFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            e.Graphics.DrawString(centerText, centerFont, centerBrush, donutRect, centerFormat);

            if (maxSlice is not null)
                DrawTooltip(e.Graphics, donutRect, maxSlice, normalizedTotal, total > 0m);
        }

        private static void DrawLabel(
            Graphics graphics,
            RectangleF donutRect,
            float angle,
            DashboardDonutSlice slice,
            decimal total,
            bool hasRealTotal)
        {
            var radians = angle * (float)Math.PI / 180f;
            var center = new PointF(donutRect.Left + donutRect.Width / 2f, donutRect.Top + donutRect.Height / 2f);
            var outerRadius = donutRect.Width / 2f;
            var anchor = new PointF(
                center.X + (float)Math.Cos(radians) * (outerRadius + 4f),
                center.Y + (float)Math.Sin(radians) * (outerRadius + 4f));
            var elbow = new PointF(
                center.X + (float)Math.Cos(radians) * (outerRadius + 26f),
                center.Y + (float)Math.Sin(radians) * (outerRadius + 26f));
            var rightSide = Math.Cos(radians) >= 0;
            var end = new PointF(elbow.X + (rightSide ? 22f : -22f), elbow.Y);

            using var linePen = new Pen(Color.FromArgb(176, 184, 197), 1.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            graphics.DrawLine(linePen, anchor, elbow);
            graphics.DrawLine(linePen, elbow, end);

            var percentage = hasRealTotal ? Math.Round(Math.Abs(slice.Amount) / total * 100m) : 0m;
            var labelText = $"{percentage.ToString(CultureInfo.InvariantCulture)}%{Environment.NewLine}{slice.Label}";
            using var font = BrandTheme.CreateFont(10f, FontStyle.Regular);
            using var brush = new SolidBrush(BrandTheme.Heading);
            var size = graphics.MeasureString(labelText, font);
            var textRect = new RectangleF(
                rightSide ? end.X + 6f : end.X - size.Width - 6f,
                end.Y - (size.Height / 2f),
                size.Width + 2f,
                size.Height + 2f);
            graphics.DrawString(labelText, font, brush, textRect);
        }

        private static void DrawTooltip(
            Graphics graphics,
            RectangleF donutRect,
            DashboardDonutSlice slice,
            decimal total,
            bool hasRealTotal)
        {
            if (!hasRealTotal)
                return;

            var percentage = Math.Round(Math.Abs(slice.Amount) / total * 100m);
            var text = $"{percentage.ToString(CultureInfo.InvariantCulture)}%{Environment.NewLine}Total: {FormatAmount(Math.Abs(slice.Amount))}";
            using var font = BrandTheme.CreateFont(10f, FontStyle.Regular);
            var textSize = graphics.MeasureString(text, font);
            var bubbleRect = new RectangleF(
                donutRect.Right + 18f,
                donutRect.Top + donutRect.Height * 0.26f,
                textSize.Width + 22f,
                textSize.Height + 16f);

            using var shadowBrush = new SolidBrush(Color.FromArgb(32, 0, 0, 0));
            using var bubbleBrush = new SolidBrush(Color.FromArgb(58, 58, 58));
            using var textBrush = new SolidBrush(Color.White);
            using var path = CreateRoundedPath(new RectangleF(bubbleRect.X + 2f, bubbleRect.Y + 3f, bubbleRect.Width, bubbleRect.Height), 12f);
            graphics.FillPath(shadowBrush, path);

            using var bubblePath = CreateRoundedPath(bubbleRect, 12f);
            graphics.FillPath(bubbleBrush, bubblePath);
            graphics.DrawString(text, font, textBrush, bubbleRect.X + 11f, bubbleRect.Y + 8f);
        }

        private static string FormatAmount(decimal value)
        {
            return value.ToString("c2", AppLocalization.CurrentCulture);
        }

        private static GraphicsPath CreateRoundedPath(RectangleF rect, float radius)
        {
            var diameter = radius * 2f;
            var path = new GraphicsPath();
            path.AddArc(rect.Left, rect.Top, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    internal sealed class DashboardDonutSlice
    {
        public string Label { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public Color Color { get; set; } = Color.Gray;
    }
}
