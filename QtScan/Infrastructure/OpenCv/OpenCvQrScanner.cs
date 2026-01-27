#if !IOS
using OpenCvSharp;
using QtScan.Domain;
using QtScan.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace QtScan.Infrastructure.OpenCv;

public sealed class OpenCvQrScanner : IQrScanner
{
    public Task<IReadOnlyList<CameraDevice>> GetDevicesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var devices = new List<CameraDevice>();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            foreach (var devicePath in Directory.GetFiles("/dev", "video*").OrderBy(path => path))
            {
                if (!TryParseDeviceIndex(devicePath, out var index))
                {
                    continue;
                }

                if (CanOpen(index))
                {
                    devices.Add(new CameraDevice(index, $"Camera {index} ({devicePath})"));
                }
            }
        }
        else
        {
            for (var index = 0; index < 4; index++)
            {
                if (CanOpen(index))
                {
                    devices.Add(new CameraDevice(index, $"Camera {index}"));
                }
            }
        }

        return Task.FromResult<IReadOnlyList<CameraDevice>>(devices);
    }

    public async IAsyncEnumerable<QrScanResult> ScanAsync(int deviceId, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var capture = new VideoCapture();
        if (!OpenCapture(capture, deviceId))
        {
            yield break;
        }

        using var detector = new QRCodeDetector();
        using var frame = new Mat();

        while (!cancellationToken.IsCancellationRequested)
        {
            if (!capture.Read(frame) || frame.Empty())
            {
                await Task.Delay(30, cancellationToken).ConfigureAwait(false);
                continue;
            }

            Cv2.ImEncode(".png", frame, out var pngBytes);

            string? decodedText = null;
            if (detector.DetectMulti(frame, out Point2f[] points) && points.Length > 0)
            {
                if (detector.DecodeMulti(frame, points, out string[] results) && results.Length > 0)
                {
                    decodedText = results.FirstOrDefault(text => !string.IsNullOrWhiteSpace(text));
                }
            }

            yield return new QrScanResult(pngBytes, decodedText);
            await Task.Delay(30, cancellationToken).ConfigureAwait(false);
        }
    }

    private static bool TryParseDeviceIndex(string devicePath, out int index)
    {
        index = -1;
        var fileName = Path.GetFileName(devicePath);
        if (!fileName.StartsWith("video", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return int.TryParse(fileName.Replace("video", string.Empty, StringComparison.OrdinalIgnoreCase), out index);
    }

    private static bool CanOpen(int index)
    {
        using var capture = new VideoCapture();
        var opened = OpenCapture(capture, index);
        if (opened)
        {
            capture.Release();
        }

        return opened;
    }

    private static bool OpenCapture(VideoCapture capture, int index)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return capture.Open(index, VideoCaptureAPIs.V4L2);
        }

        return capture.Open(index);
    }
}
#endif
