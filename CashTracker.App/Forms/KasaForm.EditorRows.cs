using System.Drawing.Drawing2D;
using System.Drawing;
using System.Windows.Forms;
using CashTracker.App.UI;

namespace CashTracker.App.Forms
{
    public sealed partial class KasaForm
    {
        private const int OdemeButtonWidth = 150;
        private const int OdemeButtonHeight = 38;
        private const int OdemeButtonGapX = 12;
        private const int OdemeButtonGapY = 8;

        private static TableLayoutPanel CreateEditorForm()
        {
            var form = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                AutoSize = false,
                AutoScroll = true,
                GrowStyle = TableLayoutPanelGrowStyle.AddRows,
                Padding = new Padding(0, 2, 0, 2)
            };

            form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36));
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64));
            return form;
        }

        private void AddTypeRow(TableLayoutPanel form)
        {
            var label = new Label { Text = "Tip", AutoSize = true, Anchor = AnchorStyles.Left };
            _cmbTip = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.White,
                Margin = new Padding(0, 8, 0, 8),
                FlatStyle = FlatStyle.Flat
            };
            label.Font = BrandTheme.CreateHeadingFont(9.4f, FontStyle.Bold);
            label.Margin = new Padding(0, 8, 10, 8);

            _cmbTip.Items.AddRange(new object[] { "Gelir", "Gider" });
            _cmbTip.SelectedIndex = 0;
            _cmbTip.SelectedIndexChanged += async (_, __) => await LoadKalemlerForTipAsync();
            form.Controls.Add(label);
            form.Controls.Add(_cmbTip);
        }

        private void AddAmountRow(TableLayoutPanel form)
        {
            var label = new Label { Text = "Tutar", AutoSize = true, Anchor = AnchorStyles.Left };
            _numTutar = new NumericUpDown
            {
                DecimalPlaces = 2,
                Maximum = 100000000,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(0, 8, 0, 8),
                ThousandsSeparator = true
            };
            label.Font = BrandTheme.CreateHeadingFont(9.4f, FontStyle.Bold);
            label.Margin = new Padding(0, 8, 10, 8);

            form.Controls.Add(label);
            form.Controls.Add(_numTutar);
        }

        private void AddPaymentMethodRow(TableLayoutPanel form)
        {
            var label = new Label { Text = "Yontem", AutoSize = true, Anchor = AnchorStyles.Left };
            label.Font = BrandTheme.CreateHeadingFont(9.4f, FontStyle.Bold);
            label.Margin = new Padding(0, 8, 10, 8);

            var methodsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 3,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                GrowStyle = TableLayoutPanelGrowStyle.FixedSize,
                Margin = new Padding(0, 6, 0, 10),
                Padding = new Padding(0)
            };
            methodsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, OdemeButtonWidth));
            methodsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, OdemeButtonGapX));
            methodsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, OdemeButtonWidth));
            methodsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, OdemeButtonHeight));
            methodsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, OdemeButtonGapY));
            methodsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, OdemeButtonHeight));

            _btnOdemeNakit = CreateOdemeYontemiButton("Nakit", "Nakit");
            _btnOdemeKrediKarti = CreateOdemeYontemiButton("Kredi Karti", "KrediKarti");
            _btnOdemeOnlineOdeme = CreateOdemeYontemiButton("Online Odeme", "OnlineOdeme");
            _btnOdemeHavale = CreateOdemeYontemiButton("Havale", "Havale");

            _btnOdemeNakit.Margin = Padding.Empty;
            _btnOdemeKrediKarti.Margin = Padding.Empty;
            _btnOdemeOnlineOdeme.Margin = Padding.Empty;
            _btnOdemeHavale.Margin = Padding.Empty;

            _btnOdemeNakit.Dock = DockStyle.Fill;
            _btnOdemeKrediKarti.Dock = DockStyle.Fill;
            _btnOdemeOnlineOdeme.Dock = DockStyle.Fill;
            _btnOdemeHavale.Dock = DockStyle.Fill;

            methodsPanel.Controls.Add(_btnOdemeNakit, 0, 0);
            methodsPanel.Controls.Add(_btnOdemeKrediKarti, 2, 0);
            methodsPanel.Controls.Add(_btnOdemeOnlineOdeme, 0, 2);
            methodsPanel.Controls.Add(_btnOdemeHavale, 2, 2);

            form.Controls.Add(label);
            form.Controls.Add(methodsPanel);

            SetSelectedOdemeYontemi("Nakit");
        }

        private static Button CreateOdemeYontemiBaseButton(string text)
        {
            var button = new Button
            {
                Text = text,
                Width = OdemeButtonWidth,
                Height = OdemeButtonHeight,
                MinimumSize = new Size(OdemeButtonWidth, OdemeButtonHeight),
                MaximumSize = new Size(OdemeButtonWidth, OdemeButtonHeight),
                AutoSize = false,
                AutoEllipsis = true,
                Margin = Padding.Empty,
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                Font = BrandTheme.CreateHeadingFont(8.9f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                ImageAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 8, 0),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(38, 53, 72),
                UseVisualStyleBackColor = false
            };

            button.FlatAppearance.BorderColor = Color.FromArgb(190, 202, 216);
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 247, 252);
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(229, 239, 247);
            return button;
        }

        private Button CreateOdemeYontemiButton(string text, string value)
        {
            var button = CreateOdemeYontemiBaseButton(text);
            button.Tag = value;
            button.Image = CreateOdemeYontemiIcon(value);
            button.Click += (_, __) => SetSelectedOdemeYontemi(value);
            return button;
        }

        private static Bitmap CreateOdemeYontemiIcon(string value)
        {
            var icon = new Bitmap(16, 16);
            using var g = Graphics.FromImage(icon);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using var pen = new Pen(Color.FromArgb(49, 75, 106), 1.6f);
            using var fill = new SolidBrush(Color.FromArgb(223, 238, 249));
            using var accent = new SolidBrush(Color.FromArgb(36, 95, 163));

            var normalized = NormalizeOdemeYontemi(value);
            if (normalized == "KrediKarti")
            {
                var frame = new Rectangle(1, 3, 14, 10);
                using var path = CreateRoundedPath(frame, 3);
                g.FillPath(fill, path);
                g.DrawPath(pen, path);
                g.DrawLine(pen, 2, 6, 14, 6);
                g.FillRectangle(accent, 3, 9, 4, 2);
                return icon;
            }

            if (normalized == "OnlineOdeme")
            {
                var globe = new Rectangle(2, 2, 12, 12);
                g.FillEllipse(fill, globe);
                g.DrawEllipse(pen, globe);
                g.DrawLine(pen, 4, 8, 12, 8);
                g.FillEllipse(accent, 7, 7, 2, 2);
                return icon;
            }

            if (normalized == "Havale")
            {
                g.DrawLine(pen, 1, 5, 12, 5);
                g.FillPolygon(accent, new[]
                {
                    new Point(12, 2),
                    new Point(15, 5),
                    new Point(12, 8)
                });

                g.DrawLine(pen, 15, 11, 4, 11);
                g.FillPolygon(accent, new[]
                {
                    new Point(4, 8),
                    new Point(1, 11),
                    new Point(4, 14)
                });
                return icon;
            }

            var note = new Rectangle(1, 3, 14, 10);
            using (var path = CreateRoundedPath(note, 2))
            {
                g.FillPath(fill, path);
                g.DrawPath(pen, path);
            }

            g.FillEllipse(accent, 6, 6, 4, 4);
            return icon;
        }

        private static GraphicsPath CreateRoundedPath(Rectangle bounds, int radius)
        {
            var diameter = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
