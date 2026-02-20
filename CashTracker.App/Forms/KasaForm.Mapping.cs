namespace CashTracker.App.Forms
{
    public sealed partial class KasaForm
    {
        private static string MapTip(string? value)
        {
            return value switch
            {
                "Giris" => "Gelir",
                "Giri\u015F" => "Gelir",
                "Cikis" => "Gider",
                "\u00C7\u0131k\u0131\u015F" => "Gider",
                _ => value ?? "Gelir"
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
                "KrediKarti" => "Kredi Karti",
                "OnlineOdeme" => "Online Odeme",
                "Havale" => "Havale",
                _ => "Nakit"
            };
        }
    }
}
