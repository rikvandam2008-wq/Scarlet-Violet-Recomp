# Builds Tandem (the launcher and the engine) into one folder, installing what's missing first. What `Build Tandem.bat`
# runs. No game files are touched or needed: games are set up afterwards in the launcher, from the player's own dumps.
#   build.ps1 [-To <folder>]    (default: build\Tandem in this source tree; an existing install is updated in place)
param([string]$To)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if (-not $To) { $To = Join-Path $root 'build\Tandem' }

function Step($t) { Write-Host "`n== $t" -ForegroundColor Cyan }

Step 'Build tools'
$msys = 'C:\msys64'
if (-not (Test-Path "$msys\ucrt64\bin\clang.exe") -or -not (Test-Path "$root\tools\venv\Scripts\python.exe")) {
    Write-Host 'Installing MSYS2 (clang, make, SDL3, FFmpeg, Vulkan) and Python 3.12 (a few minutes, once)...'
    & powershell -NoProfile -ExecutionPolicy Bypass -File "$root\tools\setup_windows.ps1"
    if ($LASTEXITCODE -ne 0) { throw 'tools\setup_windows.ps1 failed' }
} else { Write-Host 'MSYS2 and the generator''s Python: found' }

$sdk = (& dotnet --list-sdks 2>$null) -match '^(8|9|1\d)\.'
if (-not $sdk) {
    Write-Host 'Installing the .NET 8 SDK...'
    winget install --id Microsoft.DotNet.SDK.8 -e --accept-source-agreements --accept-package-agreements --silent
    $env:PATH = [Environment]::GetEnvironmentVariable('PATH', 'Machine') + ';' + [Environment]::GetEnvironmentVariable('PATH', 'User')
} else { Write-Host '.NET SDK: found' }

Step "Building Tandem into $To"
git -C $root config core.autocrlf false 2>$null
$env:PATH = "$msys\ucrt64\bin;$msys\usr\bin;" + $env:PATH
$tmp = Join-Path $root 'build\tmp'
New-Item -ItemType Directory -Force $tmp | Out-Null
$t = $tmp -replace '\\', '/'
$dest = $To -replace '\\', '/'
Push-Location $root
try {
    & "$msys\usr\bin\make.exe" -j $env:NUMBER_OF_PROCESSORS tandem "TANDEM=$dest" "TMP=$t" "TEMP=$t"
    if ($LASTEXITCODE -ne 0) { throw "the build failed (make exit code $LASTEXITCODE)" }
} finally { Pop-Location }

Step 'Done'
Write-Host "Tandem is in $To. Open it, choose Scarlet or Violet, then Set up game with your own dumps."
Start-Process (Join-Path $To 'Tandem Launcher.exe')
