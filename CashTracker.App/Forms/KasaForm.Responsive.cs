using System.Windows.Forms;

namespace CashTracker.App.Forms
{
    public sealed partial class KasaForm
    {
        private void ApplyResponsiveLayout()
        {
            if (_rootLayout is null || _leftPanel is null || _rightPanel is null)
                return;

            var compact = ClientSize.Width < 1180;

            _rootLayout.SuspendLayout();
            _rootLayout.Padding = compact ? new Padding(10) : new Padding(14);
            _rootLayout.ColumnStyles.Clear();
            _rootLayout.RowStyles.Clear();

            if (compact)
            {
                _rootLayout.ColumnCount = 1;
                _rootLayout.RowCount = 3;
                _rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                _rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                _rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
                _rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
                _rootLayout.SetColumn(_lblActiveBusiness, 0);
                _rootLayout.SetRow(_lblActiveBusiness, 0);
                _rootLayout.SetColumnSpan(_lblActiveBusiness, 1);
                _rootLayout.SetColumn(_leftPanel, 0);
                _rootLayout.SetRow(_leftPanel, 1);
                _rootLayout.SetColumn(_rightPanel, 0);
                _rootLayout.SetRow(_rightPanel, 2);
                _leftPanel.Margin = new Padding(0, 0, 0, 10);
                _rightPanel.Margin = new Padding(0);
            }
            else
            {
                _rootLayout.ColumnCount = 2;
                _rootLayout.RowCount = 2;
                _rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
                _rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
                _rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                _rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                _rootLayout.SetColumn(_lblActiveBusiness, 0);
                _rootLayout.SetRow(_lblActiveBusiness, 0);
                _rootLayout.SetColumnSpan(_lblActiveBusiness, 2);
                _rootLayout.SetColumn(_leftPanel, 0);
                _rootLayout.SetRow(_leftPanel, 1);
                _rootLayout.SetColumn(_rightPanel, 1);
                _rootLayout.SetRow(_rightPanel, 1);
                _leftPanel.Margin = new Padding(0, 0, 10, 0);
                _rightPanel.Margin = new Padding(10, 0, 0, 0);
            }

            _rootLayout.ResumeLayout();
        }
    }
}
