# QtScan

## Main features

- Live QR scanning from connected cameras (Windows/Linux; iOS camera support in progress)
- Decode QR codes from image files (desktop) or photo picker (iOS)
- Generate QR codes from typed text
- Real-time preview of the current scan/frame

## Build

### Desktop (Windows/Linux)

```bash
# Builds the desktop target only

dotnet build QtScan/QtScan.csproj -f net9.0 -p:TargetFrameworks=net9.0
```

Or use the helper script:

```bash
./build.sh
```

### iOS

iOS builds must be run on macOS with Xcode installed. The iOS workload is not supported on Linux, so `net9.0-ios` will fail to build on Ubuntu.

```bash
# On macOS only

dotnet workload install ios
DOTNET_IOS_XCODE_VERSION=15.0 dotnet build QtScan/QtScan.csproj -f net9.0-ios -p:TargetFrameworks=net9.0-ios
```

Or use the helper scripts:

```bash
./build.sh ios
```

```powershell
.\build.ps1 -Target ios
```

## Notes

- The project is multi-targeted (`net9.0;net9.0-ios`). Use the `-p:TargetFrameworks=...` override when building a single target.

## Testing

```bash
dotnet test QtScan.Tests/QtScan.Tests.csproj
```
