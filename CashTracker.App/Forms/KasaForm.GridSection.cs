using System.Drawing;
using System.Windows.Forms;
using CashTracker.App.UI;

namespace CashTracker.App.Forms
{
    public sealed partial class KasaForm
    {
        private void BuildGridSection(Panel left)
        {
            left.Controls.Add(CreateSectionHeader("Kayıtlar", "Tüm hareketler ve detay listesi"));

            _grid = CreateGrid();
            _grid.SelectionChanged += (_, __) => GridToForm();
            left.Controls.Add(_grid);
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

            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Tarih", HeaderText = "Tarih", FillWeight = 18, MinimumWidth = 120 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Tip", HeaderText = "Tip", FillWeight = 10, MinimumWidth = 70 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Tutar", HeaderText = "Tutar", FillWeight = 15, MinimumWidth = 90 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "GiderTuru", HeaderText = "Gider Türü", FillWeight = 20, MinimumWidth = 110 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Aciklama", HeaderText = "Açıklama", FillWeight = 37, MinimumWidth = 140 });
            grid.Columns[0].DefaultCellStyle = new DataGridViewCellStyle { Format = "dd.MM.yyyy HH:mm" };
            grid.Columns[2].DefaultCellStyle = new DataGridViewCellStyle
            {
                Format = "N2",
                Alignment = DataGridViewContentAlignment.MiddleRight
            };

            return grid;
        }
    }
}

