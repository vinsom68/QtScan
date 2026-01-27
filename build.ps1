param(
  [Parameter(Mandatory = $false)]
  [ValidateSet('desktop','ios','test')]
  [string]$Target = 'desktop'
)

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'QtScan/QtScan.csproj'

if ($Target -eq 'test') {
  Write-Host 'Running tests...'
  dotnet test (Join-Path $root 'QtScan.Tests/QtScan.Tests.csproj')
} elseif ($Target -eq 'ios') {
  if ($env:OS -ne 'Darwin') {
    Write-Error 'iOS builds require macOS with Xcode installed.'
    exit 1
  }
  Write-Host 'Building iOS target (macOS only)...'
  dotnet build $project -f net9.0-ios -p:TargetFrameworks=net9.0-ios
} else {
  Write-Host 'Building desktop target...'
  dotnet build $project -f net9.0 -p:TargetFrameworks=net9.0
}
