namespace CashTracker.App.Forms
{
    public sealed partial class KasaForm
    {
        private static string MapTip(string? value)
        {
            var normalized = AppLocalization.NormalizeTip(value);
            return normalized switch
            {
                "Gider" => "Gider",
                _ => "Gelir"
            };
        }

        private static string NormalizeOdemeYontemi(string? value)
        {
            var normalized = (value ?? string.Empty)
                .Trim()
                .ToLowerInvariant()
                .Replace('\u0131', 'i')
                .Replace('\u015f', 's')
                .Replace('\u011f', 'g')
                .Replace('\u00fc', 'u')
                .Replace('\u00f6', 'o')
                .Replace('\u00e7', 'c');
            return normalized switch
            {
                "nakit" => "Nakit",
                "cash" => "Nakit",
                "kredikarti" => "KrediKarti",
                "kredi karti" => "KrediKarti",
                "kredi kartı" => "KrediKarti",
                "kart" => "KrediKarti",
                "creditcard" => "KrediKarti",
                "credit card" => "KrediKarti",
                "online" => "OnlineOdeme",
                "onlineodeme" => "OnlineOdeme",
                "online odeme" => "OnlineOdeme",
                "online payment" => "OnlineOdeme",
                "havale" => "Havale",
                "transfer" => "Havale",
                "bank transfer" => "Havale",
                _ => "Nakit"
            };
        }

        private static string MapOdemeYontemiLabel(string? value)
        {
            return NormalizeOdemeYontemi(value) switch
            {
                "KrediKarti" => AppLocalization.T("payment.card"),
                "OnlineOdeme" => AppLocalization.T("payment.online"),
                "Havale" => AppLocalization.T("payment.transfer"),
                _ => AppLocalization.T("payment.cash")
            };
        }
    }
}
