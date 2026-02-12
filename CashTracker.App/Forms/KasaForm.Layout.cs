using System.Drawing;
using System.Windows.Forms;
using CashTracker.App.UI;

namespace CashTracker.App.Forms
{
    public sealed partial class KasaForm
    {
        private void BuildUi()
        {
            _rootLayout = CreateRootLayout();
            _leftPanel = CreateSurfacePanel();
            _rightPanel = CreateSurfacePanel();
            _leftPanel.Margin = new Padding(0, 0, 10, 0);
            _rightPanel.Margin = new Padding(10, 0, 0, 0);
            _rightPanel.AutoScroll = true;

            _rootLayout.Controls.Add(_leftPanel, 0, 0);
            _rootLayout.Controls.Add(_rightPanel, 1, 0);
            Controls.Add(_rootLayout);

            BuildGridSection(_leftPanel);
            BuildEditorSection(_rightPanel);
            ToggleGiderTuru();
            ApplyResponsiveLayout();
        }

        private static TableLayoutPanel CreateRootLayout()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(14, 12, 14, 14),
                ColumnCount = 2,
                RowCount = 1,
                BackColor = BrandTheme.AppBackground
            };

            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            return root;
        }

        private static Panel CreateSurfacePanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(14),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            return panel;
        }
    }
}
