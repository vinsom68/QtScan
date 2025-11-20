using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System.Diagnostics;
using QRCoder;
using Avalonia.Media.Imaging;
using System.IO;
using Bitmap = Avalonia.Media.Imaging.Bitmap;
using System;
using ZXing;
using System.Threading;
using ZXing.Common;
using Avalonia.Media;
using OpenCvSharp;
using QRCoder.Extensions;
using SkiaSharp;
using Microsoft.VisualBasic.FileIO;
using System.Threading.Tasks;
using System.Linq;
using System.Runtime.CompilerServices;

namespace QtScan
{
    public partial class MainWindow : Avalonia.Controls.Window
    {
        private VideoCapture camera;
        CancellationTokenSource tokenSource2;
        CancellationToken ct;
    
        public MainWindow()
        {
            InitializeComponent();
            camera = new VideoCapture();

            var videoDevices = Directory.GetFiles("/dev", "video*")
                .OrderBy(f => f)
                .ToList();

            foreach (var dev in videoDevices)
            {
                int index = int.Parse(Path.GetFileName(dev).Replace("video", ""));
                using var cap = new VideoCapture(index, VideoCaptureAPIs.V4L2);
                
                // Add BOTH text and real camera index as the Tag
                if (cap.IsOpened())
                    cboDevices.Items.Add(new ComboBoxItem
                    {
                        Content = $"Camera {index} ({dev})",
                        Tag = index
                    });
            }
            
            cboDevices.SelectedIndex = 0;
        }


        private void QrText_TextChanged(object? sender, TextChangedEventArgs e)
        {
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(QrText.Text, QRCodeGenerator.ECCLevel.Q))
            using (var qrCode = new QRCoder.BitmapByteQRCode(qrCodeData))
                QrImage.Source = new Bitmap(new MemoryStream(qrCode.GetGraphic(20)));
        }


        private void OnBtnStartScan(object source, RoutedEventArgs args)
        {
            QrText.Text=string.Empty;
            tokenSource2=new();
            ct=tokenSource2.Token;
            
            if (cboDevices.SelectedItem is ComboBoxItem item &&
                item.Tag is int deviceIndex)
            {
                camera.Release(); // safety

                if (camera.Open(deviceIndex, VideoCaptureAPIs.V4L2))
                {
                    btnStartScan.IsVisible = false;
                    btnStopScan.IsVisible = true;
                    _ = Task.Run(async () => await ScanQrCode(), ct);
                }
                else
                {
                    Console.WriteLine($"Failed to open camera {deviceIndex}");
                }
            }

        }

        private void OnBtnStopScan(object source, RoutedEventArgs args)
        {
            StopScan(true) ;
        }

        private void StopScan(bool nullImage)
        {
                tokenSource2.Cancel();
                btnStartScan.IsVisible=true;
                btnStopScan.IsVisible=false;
                camera.Release();
                if(nullImage)
                 SetImage(null);
                tokenSource2.Dispose();
        }

        private async Task<(IImage? image, string? text)> ScanQrCode()
        {
            ct.ThrowIfCancellationRequested();

            var detector = new QRCodeDetector();
            var frame = new Mat();
            Bitmap? lastImage = null;
            string? foundText = null;

            while (!ct.IsCancellationRequested)
            {
                // Read frame
                if (!camera.Read(frame) || frame.Empty())
                {
                    await Task.Delay(30, ct);
                    continue;
                }

                // Convert to Avalonia Bitmap
                using (var memory = frame.ToMemoryStream(".png"))
                {
                    lastImage = new Bitmap(memory);
                }

                Dispatcher.UIThread.Post(() => SetImage(lastImage));

                // Detect QR codes
                if (detector.DetectMulti(frame, out Point2f[] points))
                {
                    // Decode
                    if (detector.DecodeMulti(frame, points, out string[] results))
                    {
                        if (results.Length > 0 && ! string.IsNullOrEmpty(results[0]))
                        {
                            foundText = results[0];

                            Dispatcher.UIThread.Post(() =>
                            {
                                SetText(foundText);
                                StopScan(false);
                            });

                            break;
                        }
                    }
                }
                await Task.Delay(30, ct);
            }

            return (lastImage, foundText);
            
        }
        
        private async void OnBtnOpenFile(object? sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Open QR Image",
                AllowMultiple = false,
                Filters = new System.Collections.Generic.List<FileDialogFilter>
                {
                    new FileDialogFilter { Name = "Images", Extensions = new System.Collections.Generic.List<string> { "png", "jpg", "jpeg", "bmp", "gif" } }
                }
            };

            var result = await dlg.ShowAsync(this);
            if (result == null || result.Length == 0)
                return;

            var path = result[0];

            Mat mat;
            try
            {
                var bytes = File.ReadAllBytes(path);
                mat = Cv2.ImDecode(bytes, ImreadModes.Color);
                if (mat.Empty())
                    return;
            }
            catch
            {
                return;
            }

            // Display image in UI
            using (var ms = mat.ToMemoryStream(".png"))
            {
                var avaloniaBitmap = new Bitmap(ms);
                Dispatcher.UIThread.Post(() => SetImage(avaloniaBitmap));
            }

            // Try OpenCv QR detection/decoding
            using var detector = new QRCodeDetector();

            // Single decode: use overload with out points
            var decoded = detector.DetectAndDecode(mat, out Point2f[] points);
            if (!string.IsNullOrEmpty(decoded))
            {
                Dispatcher.UIThread.Post(() => SetText(decoded));
                return;
            }

            // Try multi decode (if multiple QR codes)
            if (detector.DetectMulti(mat, out Point2f[] multiPoints) &&
                detector.DecodeMulti(mat, multiPoints, out string[] results))
            {
                if (results.Length > 0 && !string.IsNullOrEmpty(results[0]))
                {
                    Dispatcher.UIThread.Post(() => SetText(results[0]));
                    return;
                }
            }

            // No QR found: clear text
            Dispatcher.UIThread.Post(() => SetText(string.Empty));
        }

        

        private async Task SetImage(IImage? image)
        {
            QrImage.Source=image;
        }

        private async Task SetText(string? text)
        {
            QrText.Text=text;
        }

        private void Dispose()
        {
            camera.Dispose();
        }
    }
}

//https://learn.microsoft.com/en-us/dotnet/standard/parallel-programming/task-cancellation