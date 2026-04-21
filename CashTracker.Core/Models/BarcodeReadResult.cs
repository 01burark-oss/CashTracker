namespace CashTracker.Core.Models
{
    public sealed class BarcodeReadResult
    {
        public bool Success { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;

        public static BarcodeReadResult Found(string barcode)
        {
            return new BarcodeReadResult
            {
                Success = true,
                Barcode = barcode?.Trim() ?? string.Empty
            };
        }

        public static BarcodeReadResult Failed(string message)
        {
            return new BarcodeReadResult
            {
                Success = false,
                ErrorMessage = message
            };
        }
    }
}
