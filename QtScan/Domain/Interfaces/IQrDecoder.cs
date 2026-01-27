using System.Threading;
using System.Threading.Tasks;

namespace QtScan.Domain.Interfaces;

public interface IQrDecoder
{
    Task<QrScanResult?> DecodeAsync(byte[] imageBytes, CancellationToken cancellationToken);
}
