using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QtScan.Domain.Interfaces;

public interface IQrScanner
{
    Task<IReadOnlyList<CameraDevice>> GetDevicesAsync(CancellationToken cancellationToken);
    IAsyncEnumerable<QrScanResult> ScanAsync(int deviceId, CancellationToken cancellationToken);
}
