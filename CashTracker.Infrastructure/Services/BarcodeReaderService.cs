using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using ZXing;
using ZXing.Common;

namespace CashTracker.Infrastructure.Services
{
    [SupportedOSPlatform("windows")]
    public sealed class BarcodeReaderService : IBarcodeReaderService
    {
        private static readonly BarcodeFormat[] SupportedFormats =
        {
            BarcodeFormat.EAN_13,
            BarcodeFormat.EAN_8,
            BarcodeFormat.UPC_A,
            BarcodeFormat.UPC_E,
            BarcodeFormat.CODE_128,
            BarcodeFormat.CODE_39,
            BarcodeFormat.QR_CODE
        };

        public Task<BarcodeReadResult> TryReadAsync(string imagePath, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
                return Task.FromResult(BarcodeReadResult.Failed("Barkod fotografi bulunamadi."));

            return Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    var value = Decode(imagePath);
                    return string.IsNullOrWhiteSpace(value)
                        ? BarcodeReadResult.Failed("Barkod okunamadi.")
                        : BarcodeReadResult.Found(value);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    return BarcodeReadResult.Failed(ex.Message);
                }
            }, ct);
        }

        private static string Decode(string imagePath)
        {
            using var original = new Bitmap(imagePath);
            using var bitmap = new Bitmap(original.Width, original.Height, PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.DrawImage(original, 0, 0, original.Width, original.Height);
            }

            var rectangle = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var data = bitmap.LockBits(rectangle, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                var length = Math.Abs(data.Stride) * bitmap.Height;
                var buffer = new byte[length];
                Marshal.Copy(data.Scan0, buffer, 0, length);

                var luminance = new RGBLuminanceSource(
                    buffer,
                    bitmap.Width,
                    bitmap.Height,
                    RGBLuminanceSource.BitmapFormat.BGRA32);

                var binaryBitmap = new BinaryBitmap(new HybridBinarizer(luminance));
                var reader = new MultiFormatReader
                {
                    Hints = new Dictionary<DecodeHintType, object>
                    {
                        [DecodeHintType.TRY_HARDER] = true,
                        [DecodeHintType.POSSIBLE_FORMATS] = SupportedFormats
                    }
                };

                return reader.decodeWithState(binaryBitmap)?.Text?.Trim() ?? string.Empty;
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
        }
    }
}
