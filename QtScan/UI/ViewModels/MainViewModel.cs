using Avalonia.Media.Imaging;
using Avalonia.Threading;
using QtScan.Domain;
using QtScan.Domain.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace QtScan.UI.ViewModels;

public sealed class MainViewModel : ViewModelBase, IDisposable
{
    private readonly IQrScanner _scanner;
    private readonly IQrDecoder _decoder;
    private readonly IQrCodeGenerator _generator;

    private CancellationTokenSource? _scanCts;
    private CancellationTokenSource? _textCts;
    private bool _isScanning;
    private CameraDevice? _selectedCamera;
    private Bitmap? _previewImage;
    private string _qrText = string.Empty;

    public MainViewModel(IQrScanner scanner, IQrDecoder decoder, IQrCodeGenerator generator)
    {
        _scanner = scanner;
        _decoder = decoder;
        _generator = generator;

        CameraDevices = new ObservableCollection<CameraDevice>();
        StartScanCommand = new AsyncRelayCommand(StartScanAsync, () => SelectedCamera != null && !IsScanning);
        StopScanCommand = new RelayCommand(() => StopScan(true), () => IsScanning);
    }

    public ObservableCollection<CameraDevice> CameraDevices { get; }

    public CameraDevice? SelectedCamera
    {
        get => _selectedCamera;
        set
        {
            if (SetProperty(ref _selectedCamera, value))
            {
                RaiseCommandState();
            }
        }
    }

    public Bitmap? PreviewImage
    {
        get => _previewImage;
        private set => SetProperty(ref _previewImage, value);
    }

    public string QrText
    {
        get => _qrText;
        set
        {
            if (SetProperty(ref _qrText, value))
            {
                _ = UpdateQrFromTextAsync(value);
            }
        }
    }

    public bool IsScanning
    {
        get => _isScanning;
        private set
        {
            if (SetProperty(ref _isScanning, value))
            {
                OnPropertyChanged(nameof(IsNotScanning));
                RaiseCommandState();
            }
        }
    }

    public bool IsNotScanning => !IsScanning;

    public ICommand StartScanCommand { get; }
    public ICommand StopScanCommand { get; }

    public async Task InitializeAsync()
    {
        var devices = await _scanner.GetDevicesAsync(CancellationToken.None).ConfigureAwait(false);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            CameraDevices.Clear();
            foreach (var device in devices)
            {
                CameraDevices.Add(device);
            }

            SelectedCamera = CameraDevices.FirstOrDefault();
        });
    }

    public async Task DecodeImageBytesAsync(byte[] imageBytes)
    {
        if (imageBytes.Length == 0)
        {
            return;
        }

        var result = await _decoder.DecodeAsync(imageBytes, CancellationToken.None).ConfigureAwait(false);
        if (result == null)
        {
            await Dispatcher.UIThread.InvokeAsync(() => QrText = string.Empty);
            return;
        }

        var bitmap = ToBitmap(result.PngBytes);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            PreviewImage = bitmap;
            QrText = result.Text ?? string.Empty;
        });
    }

    public void Dispose()
    {
        StopScan(false);
        _textCts?.Cancel();
        _textCts?.Dispose();
    }

    private async Task StartScanAsync()
    {
        if (SelectedCamera == null || IsScanning)
        {
            return;
        }

        StopScan(false);
        IsScanning = true;
        QrText = string.Empty;
        PreviewImage = null;

        _scanCts = new CancellationTokenSource();
        var token = _scanCts.Token;

        try
        {
            await foreach (var result in _scanner.ScanAsync(SelectedCamera.Id, token).ConfigureAwait(false))
            {
                var bitmap = ToBitmap(result.PngBytes);
                await Dispatcher.UIThread.InvokeAsync(() => PreviewImage = bitmap);

                if (!string.IsNullOrWhiteSpace(result.Text))
                {
                    await Dispatcher.UIThread.InvokeAsync(() => QrText = result.Text ?? string.Empty);
                    StopScan(false);
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (IsScanning)
            {
                StopScan(false);
            }
        }
    }

    private void StopScan(bool clearImage)
    {
        if (_scanCts != null)
        {
            _scanCts.Cancel();
            _scanCts.Dispose();
            _scanCts = null;
        }

        IsScanning = false;
        if (clearImage)
        {
            PreviewImage = null;
        }
    }

    private async Task UpdateQrFromTextAsync(string text)
    {
        _textCts?.Cancel();
        _textCts?.Dispose();
        _textCts = new CancellationTokenSource();
        var token = _textCts.Token;

        if (string.IsNullOrWhiteSpace(text))
        {
            await Dispatcher.UIThread.InvokeAsync(() => PreviewImage = null);
            return;
        }

        try
        {
            var pngBytes = await _generator.GeneratePngAsync(text, token).ConfigureAwait(false);
            var bitmap = ToBitmap(pngBytes);
            await Dispatcher.UIThread.InvokeAsync(() => PreviewImage = bitmap);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static Bitmap? ToBitmap(byte[] pngBytes)
    {
        if (pngBytes.Length == 0)
        {
            return null;
        }

        using var stream = new MemoryStream(pngBytes);
        return new Bitmap(stream);
    }

    private void RaiseCommandState()
    {
        if (StartScanCommand is AsyncRelayCommand start)
        {
            start.RaiseCanExecuteChanged();
        }

        if (StopScanCommand is RelayCommand stop)
        {
            stop.RaiseCanExecuteChanged();
        }
    }
}
