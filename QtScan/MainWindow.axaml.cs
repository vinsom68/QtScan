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

            int deviceCount = 0;
            Console.WriteLine("Video capture devices:");
            while (true) 
            {
                if (!camera.Open(deviceCount)) 
                    break; // Open the default video capture device
                
                cboDevices.Items.Add($"{camera.CaptureType.ToString()}-{deviceCount+1}");
                camera.Release();
                deviceCount++;
            }

            if(deviceCount>0)
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
            if(camera.Open(cboDevices.SelectedIndex))
            {
                btnStartScan.IsVisible=false;
                btnStopScan.IsVisible=true;
              _ = Task.Run(async () => {await ScanQrCode();}, tokenSource2.Token);
            }
            else
                Console.WriteLine("Could not grab from the video capture device.");

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
            // Were we already canceled?
            ct.ThrowIfCancellationRequested();
            Avalonia.Media.Imaging.Bitmap AvIrBitmap=null;
            string[]? stringResult=null;
            Mat frame= new Mat();

            while (true) 
            {
                if (ct.IsCancellationRequested)
                {
                    ct.ThrowIfCancellationRequested();
                    break;
                }

                camera.Read(frame);
                
                MemoryStream memory=frame.ToMemoryStream(".png");
                AvIrBitmap = new Avalonia.Media.Imaging.Bitmap(memory);
                Dispatcher.UIThread.Post(async () => await SetImage(AvIrBitmap));
                memory.Dispose();
                
                var decoder = new OpenCvSharp.QRCodeDetector();
                Point2f[] points;
                
                if(decoder.DetectMulti(frame,out points))
                {
                    if(decoder.DecodeMulti(frame,points,out stringResult))
                    {
                        camera.Release();
                        Dispatcher.UIThread.Post(async () => await SetText(stringResult[0]));
                        Dispatcher.UIThread.Post( () =>  StopScan(false));
                        break;
                    }
                }
                else
                    Task.Delay(100).GetAwaiter().GetResult();

            }

            return(AvIrBitmap,stringResult[0]);
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