using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CashTracker.App.UI;

namespace CashTracker.App.Controls
{
    internal sealed class BrandLogoControl : Control
    {
        public BrandLogoControl()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true);

            BackColor = Color.Transparent;
            Size = new Size(52, 52);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = new Rectangle(1, 1, Width - 3, Height - 3);
            using var baseBrush = new SolidBrush(BrandTheme.Navy);
            using var innerBrush = new SolidBrush(BrandTheme.Teal);
            using var ringPen = new Pen(BrandTheme.Amber, 2.2f);
            using var markPen = new Pen(Color.White, 3.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };

            e.Graphics.FillEllipse(baseBrush, rect);

            var inner = Rectangle.Inflate(rect, -8, -8);
            e.Graphics.FillEllipse(innerBrush, inner);
            e.Graphics.DrawEllipse(ringPen, rect);

            // Minimal "C" mark inspired by the shared logo's circular geometry.
            var mark = Rectangle.Inflate(inner, -5, -5);
            e.Graphics.DrawArc(markPen, mark, 35, 290);
        }
    }
}
