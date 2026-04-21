using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace CashTracker.App.UI
{
    internal static class DashboardIconFactory
    {
        public static Bitmap Create(string kind, Color color, int size = 22)
        {
            var bitmap = new Bitmap(size, size);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(Color.Transparent);

            using var pen = new Pen(color, Math.Max(1.8f, size / 11f))
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };
            using var thinPen = new Pen(color, Math.Max(1.35f, size / 14f))
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };
            using var brush = new SolidBrush(color);

            var code = (kind ?? string.Empty).Trim().ToLowerInvariant();
            switch (code)
            {
                case "records":
                    DrawRecords(graphics, pen, thinPen, size);
                    break;
                case "cari":
                    DrawWallet(graphics, pen, thinPen, size);
                    break;
                case "stock":
                    DrawCart(graphics, pen, thinPen, size);
                    break;
                case "invoice":
                    DrawDocument(graphics, pen, thinPen, size);
                    break;
                case "payment":
                    DrawPayment(graphics, pen, thinPen, size);
                    break;
                case "report":
                    DrawReport(graphics, pen, thinPen, size);
                    break;
                case "globe":
                    DrawGlobe(graphics, pen, thinPen, size);
                    break;
                case "settings":
                    DrawGear(graphics, pen, brush, size);
                    break;
                case "settingsmodern":
                    DrawSettingsModern(graphics, pen, thinPen, brush, size);
                    break;
                case "swap":
                    DrawSwap(graphics, pen, thinPen, size);
                    break;
                case "print":
                    DrawPrinter(graphics, pen, thinPen, size);
                    break;
                case "refresh":
                    DrawRefresh(graphics, pen, thinPen, size);
                    break;
                case "telegram":
                    DrawTelegram(graphics, brush, size);
                    break;
                case "bell":
                    DrawBell(graphics, pen, thinPen, brush, size);
                    break;
                case "mail":
                    DrawMail(graphics, pen, thinPen, size);
                    break;
                case "chat":
                    DrawChat(graphics, pen, thinPen, size);
                    break;
                case "pdf":
                    DrawPdf(graphics, pen, thinPen, brush, size);
                    break;
                case "chevrondown":
                    DrawChevronDown(graphics, pen, size);
                    break;
                default:
                    DrawDot(graphics, brush, size);
                    break;
            }

            return bitmap;
        }

        private static void DrawRecords(Graphics g, Pen pen, Pen thinPen, int size)
        {
            var midX = size / 2f;
            g.DrawLine(pen, midX, size * 0.16f, midX, size * 0.66f);
            g.DrawLine(pen, midX, size * 0.66f, size * 0.32f, size * 0.48f);
            g.DrawLine(pen, midX, size * 0.66f, size * 0.68f, size * 0.48f);
            g.DrawLine(thinPen, size * 0.22f, size * 0.8f, size * 0.78f, size * 0.8f);
        }

        private static void DrawWallet(Graphics g, Pen pen, Pen thinPen, int size)
        {
            var rect = new RectangleF(size * 0.18f, size * 0.28f, size * 0.64f, size * 0.44f);
            g.DrawRoundedRectangle(pen, rect, size * 0.12f);
            g.DrawLine(thinPen, rect.Left, rect.Top + rect.Height * 0.28f, rect.Right, rect.Top + rect.Height * 0.28f);
        }

        private static void DrawCart(Graphics g, Pen pen, Pen thinPen, int size)
        {
            g.DrawLine(pen, size * 0.2f, size * 0.28f, size * 0.3f, size * 0.28f);
            g.DrawLine(pen, size * 0.3f, size * 0.28f, size * 0.38f, size * 0.58f);
            g.DrawLine(pen, size * 0.38f, size * 0.58f, size * 0.74f, size * 0.58f);
            g.DrawLine(pen, size * 0.74f, size * 0.58f, size * 0.8f, size * 0.36f);
            g.DrawLine(thinPen, size * 0.42f, size * 0.7f, size * 0.42f, size * 0.7f);
            g.DrawLine(thinPen, size * 0.68f, size * 0.7f, size * 0.68f, size * 0.7f);
            g.DrawEllipse(thinPen, size * 0.33f, size * 0.65f, size * 0.12f, size * 0.12f);
            g.DrawEllipse(thinPen, size * 0.59f, size * 0.65f, size * 0.12f, size * 0.12f);
        }

        private static void DrawDocument(Graphics g, Pen pen, Pen thinPen, int size)
        {
            var rect = new RectangleF(size * 0.26f, size * 0.14f, size * 0.48f, size * 0.66f);
            g.DrawRoundedRectangle(pen, rect, size * 0.08f);
            g.DrawLine(thinPen, rect.Right - size * 0.14f, rect.Top, rect.Right, rect.Top + size * 0.14f);
            g.DrawLine(thinPen, rect.Right - size * 0.14f, rect.Top, rect.Right - size * 0.14f, rect.Top + size * 0.14f);
            g.DrawLine(thinPen, rect.Left + size * 0.1f, rect.Top + size * 0.28f, rect.Right - size * 0.1f, rect.Top + size * 0.28f);
            g.DrawLine(thinPen, rect.Left + size * 0.1f, rect.Top + size * 0.42f, rect.Right - size * 0.1f, rect.Top + size * 0.42f);
        }

        private static void DrawPayment(Graphics g, Pen pen, Pen thinPen, int size)
        {
            var rect = new RectangleF(size * 0.18f, size * 0.26f, size * 0.64f, size * 0.46f);
            g.DrawRoundedRectangle(pen, rect, size * 0.1f);
            g.DrawLine(thinPen, rect.Left + size * 0.1f, rect.Top + size * 0.16f, rect.Right - size * 0.1f, rect.Top + size * 0.16f);
            g.DrawLine(thinPen, rect.Left + size * 0.12f, rect.Bottom - size * 0.14f, rect.Left + size * 0.32f, rect.Bottom - size * 0.14f);
        }

        private static void DrawReport(Graphics g, Pen pen, Pen thinPen, int size)
        {
            var bars = new[]
            {
                new RectangleF(size * 0.18f, size * 0.5f, size * 0.1f, size * 0.22f),
                new RectangleF(size * 0.36f, size * 0.38f, size * 0.1f, size * 0.34f),
                new RectangleF(size * 0.54f, size * 0.26f, size * 0.1f, size * 0.46f)
            };

            foreach (var bar in bars)
                g.DrawRoundedRectangle(pen, bar, size * 0.04f);

            g.DrawLine(thinPen, size * 0.16f, size * 0.78f, size * 0.82f, size * 0.78f);
        }

        private static void DrawGlobe(Graphics g, Pen pen, Pen thinPen, int size)
        {
            var rect = new RectangleF(size * 0.18f, size * 0.18f, size * 0.64f, size * 0.64f);
            g.DrawEllipse(pen, rect);
            g.DrawArc(thinPen, rect, 0, 180);
            g.DrawArc(thinPen, rect, 180, 180);
            g.DrawArc(thinPen, rect.Left + size * 0.12f, rect.Top, rect.Width - size * 0.24f, rect.Height, 90, 180);
            g.DrawArc(thinPen, rect.Left + size * 0.22f, rect.Top, rect.Width - size * 0.44f, rect.Height, 90, 180);
            g.DrawLine(thinPen, rect.Left, rect.Top + rect.Height * 0.5f, rect.Right, rect.Top + rect.Height * 0.5f);
        }

        private static void DrawGear(Graphics g, Pen pen, Brush brush, int size)
        {
            var center = size / 2f;
            var outer = size * 0.24f;
            var inner = size * 0.11f;
            for (var i = 0; i < 8; i++)
            {
                var angle = (float)(Math.PI * 2 * i / 8);
                var x = center + (float)Math.Cos(angle) * size * 0.28f;
                var y = center + (float)Math.Sin(angle) * size * 0.28f;
                g.FillEllipse(brush, x - size * 0.04f, y - size * 0.04f, size * 0.08f, size * 0.08f);
            }

            g.DrawEllipse(pen, center - outer, center - outer, outer * 2, outer * 2);
            g.DrawEllipse(pen, center - inner, center - inner, inner * 2, inner * 2);
        }

        private static void DrawSettingsModern(Graphics g, Pen pen, Pen thinPen, Brush brush, int size)
        {
            var center = size / 2f;
            var ringRect = new RectangleF(size * 0.18f, size * 0.18f, size * 0.64f, size * 0.64f);
            g.DrawArc(pen, ringRect, 22, 316);

            var innerRect = new RectangleF(size * 0.34f, size * 0.34f, size * 0.32f, size * 0.32f);
            g.DrawEllipse(thinPen, innerRect);
            g.FillEllipse(brush, center - size * 0.06f, center - size * 0.06f, size * 0.12f, size * 0.12f);

            foreach (var angle in new[] { -90f, -18f, 54f, 126f, 198f })
            {
                var radians = angle * (float)Math.PI / 180f;
                var x = center + (float)Math.Cos(radians) * size * 0.34f;
                var y = center + (float)Math.Sin(radians) * size * 0.34f;
                g.FillEllipse(brush, x - size * 0.04f, y - size * 0.04f, size * 0.08f, size * 0.08f);
            }

            g.DrawLine(thinPen, size * 0.28f, size * 0.28f, size * 0.38f, size * 0.38f);
            g.DrawLine(thinPen, size * 0.72f, size * 0.28f, size * 0.62f, size * 0.38f);
            g.DrawLine(thinPen, size * 0.74f, size * 0.6f, size * 0.62f, size * 0.56f);
        }

        private static void DrawSwap(Graphics g, Pen pen, Pen thinPen, int size)
        {
            g.DrawLine(pen, size * 0.18f, size * 0.34f, size * 0.7f, size * 0.34f);
            g.DrawLine(pen, size * 0.7f, size * 0.34f, size * 0.56f, size * 0.2f);
            g.DrawLine(pen, size * 0.7f, size * 0.34f, size * 0.56f, size * 0.48f);
            g.DrawLine(pen, size * 0.82f, size * 0.66f, size * 0.3f, size * 0.66f);
            g.DrawLine(pen, size * 0.3f, size * 0.66f, size * 0.44f, size * 0.52f);
            g.DrawLine(pen, size * 0.3f, size * 0.66f, size * 0.44f, size * 0.8f);
            g.DrawLine(thinPen, size * 0.28f, size * 0.34f, size * 0.18f, size * 0.34f);
        }

        private static void DrawPrinter(Graphics g, Pen pen, Pen thinPen, int size)
        {
            g.DrawRoundedRectangle(pen, new RectangleF(size * 0.22f, size * 0.42f, size * 0.56f, size * 0.3f), size * 0.06f);
            g.DrawRoundedRectangle(thinPen, new RectangleF(size * 0.28f, size * 0.18f, size * 0.44f, size * 0.2f), size * 0.04f);
            g.DrawLine(thinPen, size * 0.32f, size * 0.56f, size * 0.68f, size * 0.56f);
            g.DrawLine(thinPen, size * 0.34f, size * 0.78f, size * 0.66f, size * 0.78f);
        }

        private static void DrawRefresh(Graphics g, Pen pen, Pen thinPen, int size)
        {
            g.DrawArc(pen, size * 0.18f, size * 0.2f, size * 0.48f, size * 0.48f, 40, 250);
            g.DrawLine(pen, size * 0.58f, size * 0.2f, size * 0.76f, size * 0.22f);
            g.DrawLine(pen, size * 0.76f, size * 0.22f, size * 0.66f, size * 0.38f);

            g.DrawArc(pen, size * 0.34f, size * 0.32f, size * 0.48f, size * 0.48f, 220, 250);
            g.DrawLine(pen, size * 0.42f, size * 0.8f, size * 0.24f, size * 0.78f);
            g.DrawLine(pen, size * 0.24f, size * 0.78f, size * 0.34f, size * 0.62f);
            g.DrawLine(thinPen, size * 0.5f, size * 0.5f, size * 0.5f, size * 0.5f);
        }

        private static void DrawTelegram(Graphics g, Brush brush, int size)
        {
            var points = new[]
            {
                new PointF(size * 0.18f, size * 0.48f),
                new PointF(size * 0.8f, size * 0.22f),
                new PointF(size * 0.64f, size * 0.8f),
                new PointF(size * 0.5f, size * 0.58f)
            };
            g.FillPolygon(brush, points);
            g.FillPolygon(Brushes.White, new[]
            {
                new PointF(size * 0.24f, size * 0.46f),
                new PointF(size * 0.74f, size * 0.28f),
                new PointF(size * 0.48f, size * 0.6f)
            });
        }

        private static void DrawBell(Graphics g, Pen pen, Pen thinPen, Brush brush, int size)
        {
            g.DrawArc(pen, size * 0.24f, size * 0.18f, size * 0.52f, size * 0.56f, 200, 140);
            g.DrawLine(pen, size * 0.28f, size * 0.62f, size * 0.72f, size * 0.62f);
            g.DrawLine(thinPen, size * 0.4f, size * 0.7f, size * 0.6f, size * 0.7f);
            g.FillEllipse(brush, size * 0.44f, size * 0.74f, size * 0.12f, size * 0.12f);
        }

        private static void DrawMail(Graphics g, Pen pen, Pen thinPen, int size)
        {
            var rect = new RectangleF(size * 0.18f, size * 0.28f, size * 0.64f, size * 0.42f);
            g.DrawRoundedRectangle(pen, rect, size * 0.06f);
            g.DrawLine(thinPen, rect.Left, rect.Top, rect.Left + rect.Width * 0.5f, rect.Top + rect.Height * 0.42f);
            g.DrawLine(thinPen, rect.Right, rect.Top, rect.Left + rect.Width * 0.5f, rect.Top + rect.Height * 0.42f);
        }

        private static void DrawChat(Graphics g, Pen pen, Pen thinPen, int size)
        {
            var rect = new RectangleF(size * 0.18f, size * 0.2f, size * 0.6f, size * 0.46f);
            g.DrawRoundedRectangle(pen, rect, size * 0.1f);
            g.DrawLine(thinPen, size * 0.38f, size * 0.66f, size * 0.3f, size * 0.82f);
            g.DrawLine(thinPen, size * 0.38f, size * 0.66f, size * 0.5f, size * 0.72f);
        }

        private static void DrawPdf(Graphics g, Pen pen, Pen thinPen, Brush brush, int size)
        {
            var rect = new RectangleF(size * 0.24f, size * 0.12f, size * 0.52f, size * 0.68f);
            g.DrawRoundedRectangle(pen, rect, size * 0.08f);
            g.DrawLine(thinPen, rect.Right - size * 0.14f, rect.Top, rect.Right, rect.Top + size * 0.14f);
            using var font = new Font(BrandTheme.CreateFont(7f, FontStyle.Bold).FontFamily, Math.Max(6f, size / 4.3f), FontStyle.Bold, GraphicsUnit.Point);
            var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString("PDF", font, brush, new RectangleF(rect.Left, rect.Top + size * 0.34f, rect.Width, size * 0.18f), format);
        }

        private static void DrawChevronDown(Graphics g, Pen pen, int size)
        {
            g.DrawLine(pen, size * 0.26f, size * 0.38f, size * 0.5f, size * 0.62f);
            g.DrawLine(pen, size * 0.5f, size * 0.62f, size * 0.74f, size * 0.38f);
        }

        private static void DrawDot(Graphics g, Brush brush, int size)
        {
            g.FillEllipse(brush, size * 0.34f, size * 0.34f, size * 0.32f, size * 0.32f);
        }

        private static void DrawRoundedRectangle(this Graphics g, Pen pen, RectangleF rect, float radius)
        {
            using var path = CreateRoundedPath(rect, radius);
            g.DrawPath(pen, path);
        }

        private static GraphicsPath CreateRoundedPath(RectangleF rect, float radius)
        {
            var diameter = radius * 2;
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
