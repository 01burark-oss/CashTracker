using System;
using System.Drawing;
using System.Windows.Forms;

namespace CashTracker.App.UI
{
    internal static class AppIconProvider
    {
        private static Icon? _current;

        public static Icon? Current => _current ??= Load();

        private static Icon? Load()
        {
            try
            {
                return Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch
            {
                return null;
            }
        }
    }
}
