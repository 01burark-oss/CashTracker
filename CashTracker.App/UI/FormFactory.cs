using System.Drawing;
using System.Windows.Forms;

namespace CashTracker.App.UI
{
    internal static class FormFactory
    {
        public static Panel CreateInputFrame(Control child, int bottomMargin, Padding? padding = null)
        {
            var framePadding = padding ?? new Padding(10, 8, 10, 8);
            var panel = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, bottomMargin),
                Padding = framePadding
            };

            panel.Paint += (_, e) =>
            {
                using var pen = new Pen(Color.FromArgb(204, 204, 204), 1f);
                e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
            };

            child.Dock = DockStyle.Top;
            panel.Controls.Add(child);
            return panel;
        }
    }
}
