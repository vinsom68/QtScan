using QRCoder;
using QtScan.Domain.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace QtScan.Infrastructure;

public sealed class QrCodeGeneratorService : IQrCodeGenerator
{
    public Task<byte[]> GeneratePngAsync(string text, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new BitmapByteQRCode(data);
        var bytes = qrCode.GetGraphic(20);
        return Task.FromResult(bytes);
    }
}
