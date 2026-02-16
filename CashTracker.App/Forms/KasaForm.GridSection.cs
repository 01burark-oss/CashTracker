using System.Drawing;
using System.Windows.Forms;
using CashTracker.App.UI;

namespace CashTracker.App.Forms
{
    public sealed partial class KasaForm
    {
        private void BuildGridSection(Panel left)
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            left.Controls.Add(layout);

            var header = CreateSectionHeader("Kayıtlar", "Tum hareketler ve detay listesi");
            header.Margin = new Padding(0, 0, 0, 10);
            layout.Controls.Add(header, 0, 0);

            _grid = CreateGrid();
            _grid.SelectionChanged += async (_, __) => await GridToFormAsync();
            _grid.CellFormatting += GridCellFormatting;
            layout.Controls.Add(_grid, 0, 1);
        }

        private static DataGridView CreateGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                GridColor = Color.FromArgb(224, 228, 234),
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(240, 245, 249),
                ForeColor = Color.FromArgb(53, 61, 73),
                Font = BrandTheme.CreateFont(10f, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleLeft
            };
            grid.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Color.FromArgb(33, 41, 52),
                SelectionBackColor = Color.FromArgb(220, 239, 242),
                SelectionForeColor = Color.FromArgb(20, 34, 48)
            };
            grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(250, 251, 253)
            };

            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Tarih", HeaderText = "Tarih", FillWeight = 16, MinimumWidth = 120 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Tip", HeaderText = "Tip", FillWeight = 9, MinimumWidth = 70 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "OdemeYontemi", HeaderText = "Yontem", FillWeight = 14, MinimumWidth = 100 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Tutar", HeaderText = "Tutar", FillWeight = 14, MinimumWidth = 90 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Kalem", HeaderText = "Kalem", FillWeight = 18, MinimumWidth = 110 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Aciklama", HeaderText = "Açıklama", FillWeight = 29, MinimumWidth = 140 });
            grid.Columns[0].DefaultCellStyle = new DataGridViewCellStyle { Format = "dd.MM.yyyy HH:mm" };
            grid.Columns[3].DefaultCellStyle = new DataGridViewCellStyle
            {
                Format = "N2",
                Alignment = DataGridViewContentAlignment.MiddleRight
            };

            return grid;
        }

        private void GridCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            if (e.ColumnIndex == 2)
            {
                e.Value = MapOdemeYontemiLabel(e.Value?.ToString());
                e.FormattingApplied = true;
                return;
            }

            if (e.ColumnIndex != 3)
                return;

            var row = _grid.Rows[e.RowIndex];
            var tip = MapTip(row.Cells[1].Value?.ToString());
            var style = e.CellStyle;
            if (style is null)
                return;

            if (tip == "Gelir")
            {
                style.ForeColor = Color.FromArgb(17, 121, 85);
                style.SelectionForeColor = Color.FromArgb(17, 121, 85);
                return;
            }

            if (tip == "Gider")
            {
                style.ForeColor = Color.FromArgb(173, 59, 56);
                style.SelectionForeColor = Color.FromArgb(173, 59, 56);
            }
        }
    }
}

