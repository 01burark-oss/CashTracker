using System.Windows.Forms;

namespace CashTracker.App.Forms
{
    public sealed partial class KasaForm
    {
        private void BuildEditorSection(Panel right)
        {
            right.Controls.Add(CreateSectionHeader("\u0130\u015Flem Formu", "Kay\u0131t d\u00FCzenleme ve i\u015Flem komutlar\u0131"));

            var form = CreateEditorForm();
            right.Controls.Add(form);

            AddRow(form, "Tarih", out _dtTarih);
            _dtTarih.Enabled = false;

            AddTypeRow(form);
            AddAmountRow(form);
            AddRow(form, "Gider T\u00FCr\u00FC", out _txtGiderTuru);
            AddRow(form, "A\u00E7\u0131klama", out _txtAciklama);

            var buttons = CreateButtonPanel();
            buttons.Controls.AddRange(new Control[] { _btnSave, _btnNew, _btnDelete, _btnRefresh });
            right.Controls.Add(buttons);
        }
    }
}


