#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
project="$script_dir/QtScan/QtScan.csproj"

if [[ "${1:-}" == "test" ]]; then
  echo "Running tests..."
  dotnet test "$script_dir/QtScan.Tests/QtScan.Tests.csproj"
elif [[ "${1:-}" == "snap" ]]; then
  echo "Building snap package..."
  if ! command -v snapcraft >/dev/null 2>&1; then
    echo "snapcraft is not installed. Run: sudo snap install snapcraft --classic" >&2
    exit 1
  fi
  snapcraft
elif [[ "${1:-}" == "ios" ]]; then
  if [[ "$(uname -s)" != "Darwin" ]]; then
    echo "iOS builds require macOS with Xcode installed." >&2
    exit 1
  fi
  echo "Building iOS target (macOS only)..."
  dotnet build "$project" -f net9.0-ios -p:TargetFrameworks=net9.0-ios
else
  echo "Building desktop target..."
  dotnet build "$project" -f net9.0 -p:TargetFrameworks=net9.0
fi
