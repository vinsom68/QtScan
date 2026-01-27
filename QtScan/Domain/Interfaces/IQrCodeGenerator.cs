using System.Threading;
using System.Threading.Tasks;

namespace QtScan.Domain.Interfaces;

public interface IQrCodeGenerator
{
    Task<byte[]> GeneratePngAsync(string text, CancellationToken cancellationToken);
}
