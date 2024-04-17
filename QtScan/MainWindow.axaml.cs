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
//using System.Drawing.Common;
//using ZXing.Windows.Compatibility;
using ZXing.Common;
using Avalonia.Media;
//using Emgu.CV;
//using Accord.Video.DirectShow;
using OpenCvSharp;
using QRCoder.Extensions;
using SkiaSharp;

namespace QtScan
{
    public partial class MainWindow : Avalonia.Controls.Window
    {
        //private FilterInfoCollection CaptureDevice;
        //private VideoCaptureDevice FinalFrame;
        private System.Timers.Timer timer1;
        //private System.Drawing.Bitmap bmpCamera;
        //private CoreCompact.System,Drawing bmpCamera;
        private VideoCapture camera;

        

        public MainWindow()
        {
            InitializeComponent();
            camera = new VideoCapture();
            //camera.Open(0); // Open the default camera (webcam)

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


            /*CaptureDevice = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach (FilterInfo Device in CaptureDevice)
            {
                cboDevices.Items.Add(Device.Name);
            }*/

            cboDevices.SelectedIndex = 0;
            //FinalFrame = new VideoCaptureDevice();

            timer1 = new System.Timers.Timer(60000);
            timer1.Elapsed += timer1_Tick;



        }


        /*private static int SelectCameraIndex()
        {
            var cameras = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
            if (cameras.Length == 1) return 0;
            foreach (var (camera, index) in WithIndex(cameras))
            {
                Console.WriteLine($"{index}:{camera.Name}");
            }
            Console.WriteLine("Select a camera from the list above:");
            var camIndex = Convert.ToInt32(Console.ReadLine());
            return camIndex;
        }*/

        private void QrText_TextChanged(object? sender, TextChangedEventArgs e)
        {
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(QrText.Text, QRCodeGenerator.ECCLevel.Q))
            using (var qrCode = new QRCoder.BitmapByteQRCode(qrCodeData))
                QrImage.Source = new Bitmap(new MemoryStream(qrCode.GetGraphic(20)));
        }


        public void OnBtnSelectCamera(object source, RoutedEventArgs args)
        {
            if(camera.Open(cboDevices.SelectedIndex))
            {
                timer1.Enabled = true;
                timer1.Start();
            }
            else
                Console.WriteLine("Could not grab from the video capture device.");

        
        }

        /*private void FinalFrame_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            //QrImage.Source =(Bitmap)eventArgs.Frame.Clone();
            bmpCamera = (System.Drawing.Bitmap)eventArgs.Frame.Clone();
        }*/

        private void OnBtnStartScan(object source, RoutedEventArgs args)
        {
            //timer1.Enabled = true;
            //timer1.Start();
            //Console.WriteLine("Scanner Strated");

        }


        private void timer1_Tick(object sender, EventArgs e)
        {
            
            Mat frame= new Mat();
            while (true) {
                // Capture a new frame from the camera
                camera.Read(frame);
                //frame.SaveImage("Test.png");
                byte[] byteImage;
                frame.GetArray(out byteImage);

                var decoder = new OpenCvSharp.QRCodeDetector();
                Point2f[] points;
                var result=decoder.DetectAndDecode(frame,out points);
                if(string.IsNullOrEmpty(result))
                    return;
                else
                {
                    timer1.Stop();
                    camera.Release();
                    QrText.Text=result;
                }


            }


        }

        private void Dispose()
        {
            /*if (FinalFrame.IsRunning == true)
            {
                FinalFrame.Stop();
            }*/
        }

        //https://stackoverflow.com/questions/17424360/qr-code-webcam-scanner-c-sharp

//https://github.com/kekyo/FlashCap
//https://stackoverflow.com/questions/69420976/capture-images-from-webcam-using-c-sharp-with-net5
//https://blog.dotnetframework.org/2020/12/29/capture-a-webcam-image-using-net-core-and-opencv/
//https://blog.dotnetframework.org/2020/12/30/record-mp4-h264-video-from-a-webcam-in-c-net-core/



    }


}