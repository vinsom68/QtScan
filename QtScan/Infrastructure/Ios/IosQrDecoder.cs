#if IOS
using CoreGraphics;
using Foundation;
using QtScan.Domain;
using QtScan.Domain.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;
using UIKit;
using ZXing;
using ZXing.Common;

namespace QtScan.Infrastructure.Ios;

public sealed class IosQrDecoder : IQrDecoder
{
    public Task<QrScanResult?> DecodeAsync(byte[] imageBytes, CancellationToken cancellationToken)
    {
        if (imageBytes.Length == 0)
        {
            return Task.FromResult<QrScanResult?>(null);
        }

        cancellationToken.ThrowIfCancellationRequested();

        using var data = NSData.FromArray(imageBytes);
        using var image = UIImage.LoadFromData(data);
        if (image?.CGImage == null)
        {
            return Task.FromResult<QrScanResult?>(null);
        }

        var luminance = CreateLuminanceSource(image.CGImage);
        if (luminance == null)
        {
            return Task.FromResult<QrScanResult?>(null);
        }

        var reader = new BarcodeReaderGeneric
        {
            AutoRotate = true,
            Options = new DecodingOptions
            {
                TryHarder = true,
                PossibleFormats = new[] { BarcodeFormat.QR_CODE }
            }
        };

        var result = reader.Decode(luminance);
        var text = result?.Text;

        using var pngData = image.AsPNG();
        var pngBytes = pngData?.ToArray() ?? imageBytes;

        return Task.FromResult<QrScanResult?>(new QrScanResult(pngBytes, text));
    }

    private static RGBLuminanceSource? CreateLuminanceSource(CGImage image)
    {
        var width = (int)image.Width;
        var height = (int)image.Height;
        if (width == 0 || height == 0)
        {
            return null;
        }

        var bytesPerPixel = 4;
        var bytesPerRow = bytesPerPixel * width;
        var length = bytesPerRow * height;
        var buffer = new byte[length];

        using var colorSpace = CGColorSpace.CreateDeviceRGB();
        using var context = new CGBitmapContext(
            buffer,
            width,
            height,
            8,
            bytesPerRow,
            colorSpace,
            CGBitmapFlags.ByteOrder32Big | CGBitmapFlags.PremultipliedLast);

        context.DrawImage(new CGRect(0, 0, width, height), image);
        return new RGBLuminanceSource(buffer, width, height, RGBLuminanceSource.BitmapFormat.RGBA32);
    }
}
#endif
