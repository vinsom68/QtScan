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

    
        public MainWindow()
        {
            InitializeComponent();
            camera = new VideoCapture();

            int deviceCount = 0;
            Console.WriteLine("Video capture devices:");
            while (true) {
                if (!camera.Open(deviceCount)) 
                    break; // Open the default video capture device
                
                var x=camera.Get(VideoCaptureProperties.HwDevice);
                var x2=VideoCaptureProperties.HwDevice.GetStringValue();
                Console.WriteLine(deviceCount + ". " + camera.Guid);
                cboDevices.Items.Add(deviceCount + ". " + camera.Guid);
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
            if(camera.Open(cboDevices.SelectedIndex))
              _ = Task.Run(async () => await ScanQrCode());
            else
                Console.WriteLine("Could not grab from the video capture device.");

        }

        private async Task<(IImage? image, string? text)> ScanQrCode()
        {
            Avalonia.Media.Imaging.Bitmap AvIrBitmap;
            string[]? stringResult=null;
            Mat frame= new Mat();

            while (true) 
            {
                camera.Read(frame);
                
                MemoryStream memory=frame.ToMemoryStream(".png");
                AvIrBitmap = new Avalonia.Media.Imaging.Bitmap(memory);
                //QrImage.Source=AvIrBitmap;
                Dispatcher.UIThread.Post(async () => await SetImage(AvIrBitmap));
                memory.Dispose();
                
                var decoder = new OpenCvSharp.QRCodeDetector();
                Point2f[] points;
                
                if(decoder.DetectMulti(frame,out points))
                {
                    if(decoder.DecodeMulti(frame,points,out stringResult))
                    {
                        camera.Release();
                        //QrText.Text=stringResult[0];
                        Dispatcher.UIThread.Post(async () => await SetText(stringResult[0]));
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

//https://docs.avaloniaui.net/docs/guides/development-guides/accessing-the-ui-thread