using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using CashTracker.App.UI;

namespace CashTracker.App.Controls
{
    internal sealed class DashboardSparkBarsControl : Control
    {
        private IReadOnlyList<decimal> _values = Array.Empty<decimal>();

        public DashboardSparkBarsControl()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true);

            BackColor = Color.White;
            Size = new Size(240, 78);
        }

        public IReadOnlyList<decimal> Values
        {
            get => _values;
            set
            {
                _values = value ?? Array.Empty<decimal>();
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
            if (rect.Width <= 8 || rect.Height <= 8)
                return;

            var values = _values.Count == 0 ? new decimal[] { 0m } : _values;
            var min = values.Min();
            var max = values.Max();
            var maxAbs = Math.Max(Math.Abs(min), Math.Abs(max));
            if (maxAbs == 0m)
                maxAbs = 1m;

            var bottom = rect.Height - 18f;
            var top = 12f;
            var chartHeight = Math.Max(bottom - top, 12f);

            using var baselinePen = new Pen(Color.FromArgb(157, 167, 183), 1.4f) { DashStyle = DashStyle.Dash };
            e.Graphics.DrawLine(baselinePen, 0, bottom, rect.Width, bottom);

            var gap = 8f;
            var barWidth = Math.Max((rect.Width - ((values.Count - 1) * gap)) / Math.Max(values.Count, 1), 6f);
            var x = 0f;
            using var barBrush = new SolidBrush(Color.FromArgb(142, 154, 176));

            foreach (var value in values)
            {
                var ratio = (float)(Math.Abs(value) / maxAbs);
                var height = Math.Max(chartHeight * ratio, 8f);
                var y = bottom - height;
                var barRect = new RectangleF(x, y, barWidth, height);

                using var path = CreateRoundedPath(barRect, Math.Min(4f, barWidth / 2f));
                e.Graphics.FillPath(barBrush, path);
                x += barWidth + gap;
            }
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
}
