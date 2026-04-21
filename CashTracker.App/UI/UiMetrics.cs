using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace CashTracker.App.UI
{
    internal static class UiMetrics
    {
        private const string ProbeText = "AgIiQ9SsGgCcOoUu";

        public static Padding SurfacePadding => new Padding(16, 14, 16, 16);

        public static Padding CardPadding => new Padding(20, 18, 20, 18);

        public static Padding ButtonPadding => new Padding(16, 4, 16, 4);

        public static void ApplyFormDefaults(Form form)
        {
            form.AutoScaleMode = AutoScaleMode.Dpi;
        }

        public static void EnableDoubleBuffer(Control control)
        {
            typeof(Control)
                .GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(control, true, null);
        }

        public static Color ResolveEffectiveBackColor(Control? control, Color fallback)
        {
            while (control is not null)
            {
                var color = control.BackColor;
                if (color.A == byte.MaxValue)
                    return color;

                control = control.Parent;
            }

            return fallback;
        }

        public static void ApplyFullscreenDialogDefaults(
            Form form,
            FormStartPosition startPosition = FormStartPosition.CenterParent)
        {
            ApplyFormDefaults(form);
            form.StartPosition = startPosition;
            form.FormBorderStyle = FormBorderStyle.Sizable;
            form.WindowState = FormWindowState.Maximized;
            form.MaximizeBox = true;
            form.MinimizeBox = true;
        }

        public static int GetTextLineHeight(Font font)
        {
            var measured = TextRenderer.MeasureText(
                ProbeText,
                font,
                new Size(int.MaxValue, int.MaxValue),
                TextFormatFlags.NoPadding);

            return Math.Max(measured.Height, (int)Math.Ceiling(font.GetHeight()) + 2);
        }

        public static int GetInputHeight(Font font, int min = 34, int verticalPadding = 10)
        {
            return Math.Max(min, GetTextLineHeight(font) + verticalPadding);
        }

        public static int GetButtonHeight(Font font, int min = 40, int verticalPadding = 16)
        {
            return Math.Max(min, GetTextLineHeight(font) + verticalPadding);
        }

        public static int GetCompactButtonHeight(Font font)
        {
            return GetButtonHeight(font, 38, 14);
        }

        public static int GetBadgeHeight(Font font)
        {
            return Math.Max(32, GetTextLineHeight(font) + 14);
        }

        public static int GetHeaderHeight(Font titleFont, Font subtitleFont, int verticalPadding = 18, int gap = 4)
        {
            return GetTextLineHeight(titleFont) + GetTextLineHeight(subtitleFont) + verticalPadding + gap;
        }

        public static int GetBannerHeight(Font font, int verticalPadding = 16, int min = 48)
        {
            return Math.Max(min, GetTextLineHeight(font) + verticalPadding);
        }

        public static int GetTopBarHeight(Font titleFont, Font badgeFont)
        {
            var titleHeight = GetTextLineHeight(titleFont) + 26;
            var badgeHeight = GetBadgeHeight(badgeFont) + 18;
            return Math.Max(96, Math.Max(titleHeight, badgeHeight));
        }

        public static int GetNoteBoxHeight(Font font, int lines = 4, int verticalPadding = 26)
        {
            return Math.Max(96, (GetTextLineHeight(font) * lines) + verticalPadding);
        }

        public static Size GetButtonMinimumSize(Font font, int minWidth = 118)
        {
            return new Size(minWidth, GetButtonHeight(font));
        }
    }
}
