using System.Drawing;
using System.Windows.Forms;
using CashTracker.App.UI;

namespace CashTracker.App.Forms
{
    public sealed partial class SettingsForm
    {
        private void BuildUi()
        {
            SuspendLayout();
            Controls.Clear();

            _rootLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 14, 16, 16),
                ColumnCount = 2,
                RowCount = 1,
                BackColor = BrandTheme.AppBackground
            };
            _rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
            _rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            _businessPanel = CreateSurfacePanel();
            _categoryPanel = CreateSurfacePanel();
            _businessPanel.Margin = new Padding(0, 0, 10, 0);
            _categoryPanel.Margin = new Padding(10, 0, 0, 0);

            _rootLayout.Controls.Add(_businessPanel, 0, 0);
            _rootLayout.Controls.Add(_categoryPanel, 1, 0);
            Controls.Add(_rootLayout);

            BuildBusinessSection();
            BuildCategorySection();
            ApplyResponsiveLayout();

            ResumeLayout(true);
        }

        private void BuildBusinessSection()
        {
            _businessSectionLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 8
            };
            _businessSectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _businessSectionLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _businessSectionLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _businessSectionLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _businessSectionLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _businessSectionLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _businessSectionLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _businessSectionLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _businessSectionLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _businessSectionLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            _businessPanel.Controls.Add(_businessSectionLayout);

            var header = CreateSectionHeader("Isletme Ayarlari", "Aktif isletmeyi sec, adini guncelle ve yeni isletme ekle.");
            header.Margin = new Padding(0, 0, 0, 12);
            _businessSectionLayout.Controls.Add(header, 0, 0);

            _businessSectionLayout.Controls.Add(CreateFieldLabel("Aktif Isletme"), 0, 1);

            _rowActiveBusiness = CreateTwoColumnRow(100, 146);
            _cmbBusinesses = CreateFlatComboBox();
            _btnSetActiveBusiness = CreateActionButton("Aktif Yap", BrandTheme.Navy, Color.White);
            _rowActiveBusiness.Controls.Add(_cmbBusinesses, 0, 0);
            _rowActiveBusiness.Controls.Add(_btnSetActiveBusiness, 1, 0);
            _businessSectionLayout.Controls.Add(_rowActiveBusiness, 0, 2);

            _businessSectionLayout.Controls.Add(CreateFieldLabel("Isletme Adi"), 0, 3);

            _rowRenameBusiness = CreateTwoColumnRow(100, 146);
            _txtRenameBusiness = CreateInputBox();
            _btnRenameBusiness = CreateActionButton("Yeniden Adlandir", BrandTheme.Navy, Color.White);
            _btnDeleteBusiness = CreateActionButton("Isletmeyi Sil", BrandTheme.Navy, Color.White);
            _btnDeleteBusiness.Enabled = false;
            _btnRenameBusiness.AutoSize = true;
            _btnRenameBusiness.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _btnRenameBusiness.Dock = DockStyle.None;
            _btnRenameBusiness.MinimumSize = new Size(0, 34);
            _btnDeleteBusiness.AutoSize = true;
            _btnDeleteBusiness.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _btnDeleteBusiness.Dock = DockStyle.None;
            _btnDeleteBusiness.MinimumSize = new Size(0, 34);

            var renameActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            _btnRenameBusiness.Margin = new Padding(0);
            _btnDeleteBusiness.Margin = new Padding(8, 0, 0, 0);
            renameActions.Controls.Add(_btnRenameBusiness);
            renameActions.Controls.Add(_btnDeleteBusiness);

            _rowRenameBusiness.Controls.Add(_txtRenameBusiness, 0, 0);
            _rowRenameBusiness.Controls.Add(renameActions, 1, 0);
            _businessSectionLayout.Controls.Add(_rowRenameBusiness, 0, 4);

            _businessSectionLayout.Controls.Add(CreateFieldLabel("Yeni Isletme"), 0, 5);

            _rowNewBusiness = CreateTwoColumnRow(100, 146);
            _txtNewBusiness = CreateInputBox();
            _btnAddBusiness = CreateActionButton("Isletme Ekle", BrandTheme.Teal, Color.White);
            _rowNewBusiness.Controls.Add(_txtNewBusiness, 0, 0);
            _rowNewBusiness.Controls.Add(_btnAddBusiness, 1, 0);
            _businessSectionLayout.Controls.Add(_rowNewBusiness, 0, 6);

            _lblBusinessHint = new Label
            {
                Dock = DockStyle.Top,
                Text = string.Empty,
                ForeColor = BrandTheme.MutedText,
                Font = BrandTheme.CreateFont(9f),
                Margin = new Padding(2, 10, 2, 0),
                AutoSize = true
            };
            _businessSectionLayout.Controls.Add(_lblBusinessHint, 0, 7);

            _cmbBusinesses.SelectedIndexChanged += (_, __) => SyncSelectedBusinessToEditor();
            _btnSetActiveBusiness.Click += async (_, __) => await SetActiveBusinessAsync();
            _btnRenameBusiness.Click += async (_, __) => await RenameSelectedBusinessAsync();
            _btnAddBusiness.Click += async (_, __) => await AddBusinessAsync();
            _btnDeleteBusiness.Click += async (_, __) => await DeleteSelectedBusinessAsync();
            _txtNewBusiness.KeyDown += async (_, e) =>
            {
                if (e.KeyCode != Keys.Enter)
                    return;

                e.SuppressKeyPress = true;
                await AddBusinessAsync();
            };
            _txtRenameBusiness.KeyDown += async (_, e) =>
            {
                if (e.KeyCode != Keys.Enter)
                    return;

                e.SuppressKeyPress = true;
                await RenameSelectedBusinessAsync();
            };
        }

        private void BuildCategorySection()
        {
            _categorySectionLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6
            };
            _categorySectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _categorySectionLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _categorySectionLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _categorySectionLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            _categorySectionLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _categorySectionLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _categorySectionLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _categoryPanel.Controls.Add(_categorySectionLayout);

            var header = CreateSectionHeader("Gelir / Gider Kalemleri", "Secili isletme icin kalemleri yonet.");
            header.Margin = new Padding(0, 0, 0, 12);
            _categorySectionLayout.Controls.Add(header, 0, 0);

            var tipRow = CreateTwoColumnRow(100, 0);
            tipRow.ColumnStyles[1] = new ColumnStyle(SizeType.Percent, 0);
            _cmbKalemTip = CreateFlatComboBox();
            _cmbKalemTip.Items.AddRange(new object[] { "Gelir", "Gider" });
            _cmbKalemTip.SelectedIndex = 0;
            tipRow.Controls.Add(_cmbKalemTip, 0, 0);
            _categorySectionLayout.Controls.Add(tipRow, 0, 1);

            _lstKalemler = new ListBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                IntegralHeight = false,
                BackColor = Color.White,
                Font = BrandTheme.CreateFont(10f),
                Margin = new Padding(0, 6, 0, 8)
            };
            _categorySectionLayout.Controls.Add(_lstKalemler, 0, 2);

            _rowEditKalem = CreateTwoColumnRow(100, 146);
            _rowEditKalem.Margin = new Padding(0, 0, 0, 8);
            _txtEditKalem = CreateInputBox();
            _txtEditKalem.Enabled = false;
            _btnUpdateKalem = CreateActionButton("Seciliyi Guncelle", BrandTheme.Navy, Color.White);
            _btnUpdateKalem.Enabled = false;
            _btnDeleteKalem = CreateActionButton("Secili Kalemi Sil", BrandTheme.Navy, Color.White);
            _btnDeleteKalem.Enabled = false;
            _btnUpdateKalem.AutoSize = true;
            _btnUpdateKalem.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _btnUpdateKalem.Dock = DockStyle.None;
            _btnUpdateKalem.MinimumSize = new Size(0, 34);
            _btnDeleteKalem.AutoSize = true;
            _btnDeleteKalem.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _btnDeleteKalem.Dock = DockStyle.None;
            _btnDeleteKalem.MinimumSize = new Size(0, 34);

            var editActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            _btnUpdateKalem.Margin = new Padding(0);
            _btnDeleteKalem.Margin = new Padding(8, 0, 0, 0);
            editActions.Controls.Add(_btnUpdateKalem);
            editActions.Controls.Add(_btnDeleteKalem);

            _rowEditKalem.Controls.Add(_txtEditKalem, 0, 0);
            _rowEditKalem.Controls.Add(editActions, 1, 0);
            _categorySectionLayout.Controls.Add(_rowEditKalem, 0, 3);

            _rowAddKalem = CreateTwoColumnRow(100, 146);
            _txtNewKalem = CreateInputBox();
            _btnAddKalem = CreateActionButton("Kalem Ekle", BrandTheme.Teal, Color.White);
            _rowAddKalem.Controls.Add(_txtNewKalem, 0, 0);
            _rowAddKalem.Controls.Add(_btnAddKalem, 1, 0);
            _categorySectionLayout.Controls.Add(_rowAddKalem, 0, 4);

            _lblCategoryHint = new Label
            {
                Dock = DockStyle.Top,
                Text = string.Empty,
                ForeColor = BrandTheme.MutedText,
                Font = BrandTheme.CreateFont(9f),
                Margin = new Padding(2, 10, 2, 0),
                AutoSize = true
            };
            _categorySectionLayout.Controls.Add(_lblCategoryHint, 0, 5);

            _cmbKalemTip.SelectedIndexChanged += async (_, __) => await LoadKalemlerAsync();
            _lstKalemler.SelectedIndexChanged += (_, __) => SyncSelectedKalemToEditor();
            _btnAddKalem.Click += async (_, __) => await AddKalemAsync();
            _btnUpdateKalem.Click += async (_, __) => await UpdateSelectedKalemAsync();
            _btnDeleteKalem.Click += async (_, __) => await DeleteSelectedKalemAsync();
            _txtEditKalem.TextChanged += (_, __) => UpdateKalemActionStates();
            _txtNewKalem.KeyDown += async (_, e) =>
            {
                if (e.KeyCode != Keys.Enter)
                    return;

                e.SuppressKeyPress = true;
                await AddKalemAsync();
            };
            _txtEditKalem.KeyDown += async (_, e) =>
            {
                if (e.KeyCode != Keys.Enter)
                    return;

                e.SuppressKeyPress = true;
                await UpdateSelectedKalemAsync();
            };
        }
    }
}
