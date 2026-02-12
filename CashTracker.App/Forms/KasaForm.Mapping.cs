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
    }
}
