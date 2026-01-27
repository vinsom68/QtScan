namespace QtScan.Domain;

public sealed record QrScanResult(byte[] PngBytes, string? Text);
