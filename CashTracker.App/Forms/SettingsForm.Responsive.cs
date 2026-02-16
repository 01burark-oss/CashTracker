using System.Drawing;
using System.Windows.Forms;

namespace CashTracker.App.Forms
{
    public sealed partial class SettingsForm
    {
        private void ApplyResponsiveLayout()
        {
            if (_rootLayout is null || _businessPanel is null || _categoryPanel is null)
                return;

            var compact = ClientSize.Width < 1060;
            var narrow = ClientSize.Width < 920;

            _rootLayout.SuspendLayout();
            _rootLayout.Padding = compact ? new Padding(10) : new Padding(16, 14, 16, 16);
            _rootLayout.ColumnStyles.Clear();
            _rootLayout.RowStyles.Clear();

            if (compact)
            {
                _rootLayout.ColumnCount = 1;
                _rootLayout.RowCount = 2;
                _rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                _rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
                _rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
                _rootLayout.SetColumn(_businessPanel, 0);
                _rootLayout.SetRow(_businessPanel, 0);
                _rootLayout.SetColumn(_categoryPanel, 0);
                _rootLayout.SetRow(_categoryPanel, 1);
                _businessPanel.Margin = new Padding(0, 0, 0, 10);
                _categoryPanel.Margin = new Padding(0);
            }
            else
            {
                _rootLayout.ColumnCount = 2;
                _rootLayout.RowCount = 1;
                _rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
                _rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
                _rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                _rootLayout.SetColumn(_businessPanel, 0);
                _rootLayout.SetRow(_businessPanel, 0);
                _rootLayout.SetColumn(_categoryPanel, 1);
                _rootLayout.SetRow(_categoryPanel, 0);
                _businessPanel.Margin = new Padding(0, 0, 10, 0);
                _categoryPanel.Margin = new Padding(10, 0, 0, 0);
            }

            _rootLayout.ResumeLayout();

            var actionWidth = narrow ? 160 : 176;
            SetActionRowButtonWidth(_rowActiveBusiness, actionWidth);
            SetActionRowButtonWidth(_rowRenameBusiness, actionWidth);
            SetActionRowButtonWidth(_rowNewBusiness, actionWidth);
            SetActionRowButtonWidth(_rowEditKalem, actionWidth);
            SetActionRowButtonWidth(_rowAddKalem, actionWidth);
        }

        private static void SetActionRowButtonWidth(TableLayoutPanel row, int width)
        {
            if (row is null || row.ColumnStyles.Count < 2)
                return;

            row.ColumnStyles[1].SizeType = SizeType.Absolute;
            row.ColumnStyles[1].Width = ResolveRequiredButtonWidth(row, width);
        }

        private static int ResolveRequiredButtonWidth(TableLayoutPanel row, int fallbackWidth)
        {
            if (row.GetControlFromPosition(1, 0) is not Button button)
                return fallbackWidth;

            var measured = TextRenderer.MeasureText(button.Text ?? string.Empty, button.Font).Width;
            var required = measured + button.Padding.Horizontal + button.Margin.Horizontal + 24;
            return Math.Max(fallbackWidth, required);
        }
    }
}
