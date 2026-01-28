# QtScan

## Main features

- Live QR scanning from connected cameras (Windows/Linux; iOS camera support in progress)
- Decode QR codes from image files (desktop) or photo picker (iOS)
- Generate QR codes from typed text
- Real-time preview of the current scan/frame

## Screenshot

![QtScan screenshot](Readme.png)

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

## Snap packaging (Ubuntu App Store)

QtScan can be published to the Ubuntu App Store (Snap Store). The Snap configuration lives at `snap/snapcraft.yaml`.

Basic workflow (run on Ubuntu):

```bash
snapcraft
snapcraft login
snapcraft register qtscan
snapcraft push --release=edge qtscan_*.snap
```

Or use the helper script:

```bash
./build.sh snap
```

Notes:
- You may need to install the `snapcraft` snap first: `sudo snap install snapcraft --classic`
- If `dotnet-sdk-9.0` is unavailable in your build environment, adjust the `build-packages` entry in `snap/snapcraft.yaml` or build with a compatible SDK.
