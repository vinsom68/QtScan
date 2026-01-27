#if IOS
using AVFoundation;
using CoreFoundation;
using CoreGraphics;
using CoreImage;
using CoreMedia;
using CoreVideo;
using Foundation;
using QtScan.Domain;
using QtScan.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using UIKit;

namespace QtScan.Infrastructure.Ios;

public sealed class IosQrScanner : IQrScanner
{
    public Task<IReadOnlyList<CameraDevice>> GetDevicesAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<CameraDevice>>(new List<CameraDevice> { new(0, "Back Camera") });

    public async IAsyncEnumerable<QrScanResult> ScanAsync(int deviceId, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<QrScanResult>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        using var session = new AVCaptureSession
        {
            SessionPreset = AVCaptureSession.PresetHigh
        };

        var device = AVCaptureDevice.GetDefaultDevice(AVMediaTypes.Video);
        if (device == null)
        {
            yield break;
        }

        var input = AVCaptureDeviceInput.FromDevice(device, out var error);
        if (error != null || input == null)
        {
            yield break;
        }

        if (session.CanAddInput(input))
        {
            session.AddInput(input);
        }

        var metadataOutput = new AVCaptureMetadataOutput();
        if (session.CanAddOutput(metadataOutput))
        {
            session.AddOutput(metadataOutput);
        }

        var videoOutput = new AVCaptureVideoDataOutput
        {
            AlwaysDiscardsLateVideoFrames = true,
            VideoSettings = new CVPixelBufferAttributes
            {
                PixelFormatType = CVPixelFormatType.CV32BGRA
            }.Dictionary
        };

        if (session.CanAddOutput(videoOutput))
        {
            session.AddOutput(videoOutput);
        }

        var frameState = new FrameState();
        var metadataDelegate = new MetadataDelegate(channel, frameState);
        var videoDelegate = new VideoOutputDelegate(channel, frameState);

        metadataOutput.SetDelegate(metadataDelegate, new DispatchQueue("qr.metadata"));
        metadataOutput.MetadataObjectTypes = new[] { AVMetadataObjectType.QRCode };
        videoOutput.SetSampleBufferDelegate(videoDelegate, new DispatchQueue("qr.video"));

        session.StartRunning();

        using var registration = cancellationToken.Register(() =>
        {
            channel.Writer.TryComplete();
        });

        try
        {
            while (await channel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                while (channel.Reader.TryRead(out var item))
                {
                    yield return item;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            session.StopRunning();
            channel.Writer.TryComplete();
        }
    }

    private sealed class FrameState
    {
        public byte[]? LastFrameBytes;
        public long FrameCounter;
    }

    private sealed class MetadataDelegate : AVCaptureMetadataOutputObjectsDelegate
    {
        private readonly Channel<QrScanResult> _channel;
        private readonly FrameState _state;

        public MetadataDelegate(Channel<QrScanResult> channel, FrameState state)
        {
            _channel = channel;
            _state = state;
        }

        public override void DidOutputMetadataObjects(AVCaptureMetadataOutput captureOutput, AVMetadataObject[] metadataObjects, AVCaptureConnection connection)
        {
            if (metadataObjects == null || metadataObjects.Length == 0)
            {
                return;
            }

            var text = metadataObjects
                .OfType<AVMetadataMachineReadableCodeObject>()
                .Select(obj => obj.StringValue)
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            var bytes = _state.LastFrameBytes ?? Array.Empty<byte>();
            _channel.Writer.TryWrite(new QrScanResult(bytes, text));
        }
    }

    private sealed class VideoOutputDelegate : AVCaptureVideoDataOutputSampleBufferDelegate
    {
        private readonly Channel<QrScanResult> _channel;
        private readonly FrameState _state;

        public VideoOutputDelegate(Channel<QrScanResult> channel, FrameState state)
        {
            _channel = channel;
            _state = state;
        }

        public override void DidOutputSampleBuffer(AVCaptureOutput captureOutput, CMSampleBuffer sampleBuffer, AVCaptureConnection connection)
        {
            using var imageBuffer = sampleBuffer.GetImageBuffer();
            if (imageBuffer is not CVPixelBuffer pixelBuffer)
            {
                return;
            }

            var bytes = ConvertToPng(pixelBuffer);
            if (bytes.Length == 0)
            {
                return;
            }

            _state.LastFrameBytes = bytes;
            var count = System.Threading.Interlocked.Increment(ref _state.FrameCounter);
            if (count % 3 == 0)
            {
                _channel.Writer.TryWrite(new QrScanResult(bytes, null));
            }
        }

        private static byte[] ConvertToPng(CVPixelBuffer pixelBuffer)
        {
            using var ciImage = new CIImage(pixelBuffer);
            using var context = CIContext.FromOptions(null);
            using var cgImage = context.CreateCGImage(ciImage, ciImage.Extent);
            if (cgImage == null)
            {
                return Array.Empty<byte>();
            }

            using var uiImage = UIImage.FromImage(cgImage);
            using var pngData = uiImage.AsPNG();
            return pngData?.ToArray() ?? Array.Empty<byte>();
        }
    }
}
#endif
