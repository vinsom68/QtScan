using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
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
                ScanQrCode();
            else
                Console.WriteLine("Could not grab from the video capture device.");

        }

        private void ScanQrCode()
        {
            Mat frame= new Mat();
            while (true) 
            {
                camera.Read(frame);
                
                MemoryStream memory=frame.ToMemoryStream(".png");
                Avalonia.Media.Imaging.Bitmap AvIrBitmap = new Avalonia.Media.Imaging.Bitmap(memory);
                QrImage.Source=AvIrBitmap;
                memory.Dispose();
                
                var decoder = new OpenCvSharp.QRCodeDetector();
                Point2f[] points;
                string[]? stringResult=null;

                if(decoder.DetectMulti(frame,out points))
                {
                    if(decoder.DecodeMulti(frame,points,out stringResult))
                    {
                        camera.Release();
                        QrText.Text=stringResult[0];
                        break;
                    }
                }
                else
                    Task.Delay(100).GetAwaiter().GetResult();

            }
        }

        private void Dispose()
        {
            camera.Dispose();
        }
    }
}