using System.Drawing;

namespace CashTracker.LicenseAdmin;

internal static class AdminTheme
{
    public static Color AppBackground => Color.FromArgb(234, 240, 249);
    public static Color Surface => Color.White;
    public static Color Border => Color.FromArgb(211, 221, 234);
    public static Color Heading => Color.FromArgb(19, 33, 56);
    public static Color MutedText => Color.FromArgb(103, 117, 138);
    public static Color Navy => Color.FromArgb(27, 59, 93);
    public static Color Teal => Color.FromArgb(34, 143, 131);
    public static Color Success => Color.FromArgb(17, 121, 85);
    public static Color Warning => Color.FromArgb(173, 111, 24);
    public static Color Error => Color.FromArgb(173, 59, 56);

    public static Font CreateFont(float size, FontStyle style = FontStyle.Regular)
    {
        return new Font("Segoe UI", size, style, GraphicsUnit.Point);
    }

    public static Font CreateHeadingFont(float size, FontStyle style = FontStyle.Bold)
    {
        return CreateFont(size, style);
    }
}
