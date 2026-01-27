using Avalonia.Headless.XUnit;
using QtScan.Domain;
using QtScan.Domain.Interfaces;
using QtScan.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace QtScan.Tests;

public sealed class MainViewModelTests
{
    [AvaloniaFact]
    public async Task InitializeAsync_PopulatesDevicesAndSelectsFirst()
    {
        var scanner = new FakeScanner(new[] { new CameraDevice(1, "Cam A"), new CameraDevice(2, "Cam B") });
        var decoder = new FakeDecoder(_ => null);
        var generator = new FakeGenerator();
        var viewModel = new MainViewModel(scanner, decoder, generator);

        await viewModel.InitializeAsync();

        Assert.Equal(2, viewModel.CameraDevices.Count);
        Assert.Equal(1, viewModel.SelectedCamera?.Id);
    }

    [AvaloniaFact]
    public async Task StartScanCommand_SetsTextAndStopsWhenDecoded()
    {
        var results = new[]
        {
            new QrScanResult(Array.Empty<byte>(), null),
            new QrScanResult(Array.Empty<byte>(), "hello")
        };
        var scanner = new FakeScanner(new[] { new CameraDevice(0, "Cam") }, results);
        var decoder = new FakeDecoder(_ => null);
        var generator = new FakeGenerator();
        var viewModel = new MainViewModel(scanner, decoder, generator);
        await viewModel.InitializeAsync();

        viewModel.StartScanCommand.Execute(null);

        await WaitForAsync(() => viewModel.QrText == "hello");

        Assert.False(viewModel.IsScanning);
        Assert.Equal("hello", viewModel.QrText);
    }

    [AvaloniaFact]
    public async Task DecodeImageBytesAsync_UpdatesQrText()
    {
        var scanner = new FakeScanner(Array.Empty<CameraDevice>());
        var decoder = new FakeDecoder(_ => new QrScanResult(Array.Empty<byte>(), "decoded"));
        var generator = new FakeGenerator();
        var viewModel = new MainViewModel(scanner, decoder, generator);

        await viewModel.DecodeImageBytesAsync(new byte[] { 1, 2, 3 });

        await WaitForAsync(() => viewModel.QrText == "decoded");
        Assert.Equal("decoded", viewModel.QrText);
    }

    [AvaloniaFact]
    public async Task QrText_InvokesGenerator()
    {
        var scanner = new FakeScanner(Array.Empty<CameraDevice>());
        var decoder = new FakeDecoder(_ => null);
        var generator = new FakeGenerator();
        var viewModel = new MainViewModel(scanner, decoder, generator);

        viewModel.QrText = "generate";

        await WaitForAsync(() => generator.LastText == "generate");
        Assert.Equal("generate", generator.LastText);
    }

    private static async Task WaitForAsync(Func<bool> condition, int timeoutMs = 2000)
    {
        var start = DateTime.UtcNow;
        while (!condition())
        {
            if ((DateTime.UtcNow - start).TotalMilliseconds > timeoutMs)
            {
                throw new TimeoutException("Condition not met in time.");
            }

            await Task.Delay(10);
        }
    }

    private sealed class FakeScanner : IQrScanner
    {
        private readonly IReadOnlyList<CameraDevice> _devices;
        private readonly IReadOnlyList<QrScanResult> _results;

        public FakeScanner(IReadOnlyList<CameraDevice> devices, IReadOnlyList<QrScanResult>? results = null)
        {
            _devices = devices;
            _results = results ?? Array.Empty<QrScanResult>();
        }

        public Task<IReadOnlyList<CameraDevice>> GetDevicesAsync(CancellationToken cancellationToken)
            => Task.FromResult(_devices);

        public async IAsyncEnumerable<QrScanResult> ScanAsync(int deviceId, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var result in _results)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
                yield return result;
            }
        }
    }

    private sealed class FakeDecoder : IQrDecoder
    {
        private readonly Func<byte[], QrScanResult?> _decoder;

        public FakeDecoder(Func<byte[], QrScanResult?> decoder)
        {
            _decoder = decoder;
        }

        public Task<QrScanResult?> DecodeAsync(byte[] imageBytes, CancellationToken cancellationToken)
            => Task.FromResult(_decoder(imageBytes));
    }

    private sealed class FakeGenerator : IQrCodeGenerator
    {
        public string? LastText { get; private set; }

        public Task<byte[]> GeneratePngAsync(string text, CancellationToken cancellationToken)
        {
            LastText = text;
            return Task.FromResult(Array.Empty<byte>());
        }
    }
}
