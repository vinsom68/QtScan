#if !IOS
using OpenCvSharp;
using QtScan.Domain;
using QtScan.Domain.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QtScan.Infrastructure.OpenCv;

public sealed class OpenCvQrDecoder : IQrDecoder
{
    public Task<QrScanResult?> DecodeAsync(byte[] imageBytes, CancellationToken cancellationToken)
    {
        if (imageBytes.Length == 0)
        {
            return Task.FromResult<QrScanResult?>(null);
        }

        cancellationToken.ThrowIfCancellationRequested();

        using var mat = Cv2.ImDecode(imageBytes, ImreadModes.Color);
        if (mat.Empty())
        {
            return Task.FromResult<QrScanResult?>(null);
        }

        using var detector = new QRCodeDetector();
        string? decodedText = null;

        var single = detector.DetectAndDecode(mat, out _);
        if (!string.IsNullOrWhiteSpace(single))
        {
            decodedText = single;
        }
        else if (detector.DetectMulti(mat, out Point2f[] points) && points.Length > 0)
        {
            if (detector.DecodeMulti(mat, points, out string[] results) && results.Length > 0)
            {
                decodedText = results.FirstOrDefault(text => !string.IsNullOrWhiteSpace(text));
            }
        }

        Cv2.ImEncode(".png", mat, out var pngBytes);
        return Task.FromResult<QrScanResult?>(new QrScanResult(pngBytes, decodedText));
    }
}
#endif
