using System.Drawing;
using System.Linq;

namespace CashTracker.App.UI
{
    internal static class BrandTheme
    {
        // Palette derived from the provided SVG.
        public static Color Navy => Color.FromArgb(25, 66, 110);
        public static Color NavyDeep => Color.FromArgb(18, 33, 53);
        public static Color Teal => Color.FromArgb(79, 199, 175);
        public static Color Amber => Color.FromArgb(223, 152, 52);
        public static Color Surface => Color.White;
        public static Color AppBackground => Color.FromArgb(236, 241, 248);
        public static Color Border => Color.FromArgb(212, 221, 233);
        public static Color Heading => Color.FromArgb(23, 34, 52);
        public static Color MutedText => Color.FromArgb(96, 109, 126);

        public static Font CreateFont(float size, FontStyle style = FontStyle.Regular)
        {
            var family = ResolveFamily(
                "Aptos",
                "Segoe UI Variable Text",
                "Segoe UI Variable",
                "Bahnschrift",
                "Segoe UI");
            return new Font(family, size, style, GraphicsUnit.Point);
        }

        public static Font CreateHeadingFont(float size, FontStyle style = FontStyle.Bold)
        {
            var family = ResolveFamily(
                "Aptos Display",
                "Bahnschrift SemiBold",
                "Bahnschrift",
                "Segoe UI Semibold",
                "Segoe UI");
            return new Font(family, size, style, GraphicsUnit.Point);
        }

        private static string ResolveFamily(params string[] candidates)
        {
            foreach (var candidate in candidates)
            {
                if (FontFamily.Families.Any(f => f.Name == candidate))
                    return candidate;
            }

            return "Segoe UI";
        }
    }
}
