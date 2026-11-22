# Builds the Rigshift per-machine installer from a self-contained win-x64 publish.
# Publish output: <repo>\artifacts\publish
# Installer: <repo>\artifacts\installer\Rigshift-Setup-<version>-x64.exe
#
# Requires Inno Setup 6 (pinned 6.7.1). Install with:
#   winget install --id JRSoftware.InnoSetup -e --version 6.7.1
#
# Usage: installer\build.ps1 [-Version <v>]
# When -Version is empty the script reads <Version> from Rigshift/Rigshift.csproj.
# Never elevates and never installs anything.

param([string]$Version)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root

function Fail([string]$message) {
    Write-Error $message
    exit 1
}

# --- Version -------------------------------------------------------------
if (-not $Version) {
    [xml]$csproj = Get-Content (Join-Path $Root 'Rigshift\Rigshift.csproj') -Raw
    $Version = $csproj.Project.PropertyGroup.Version
}
if (-not $Version) {
    $Version = '0.0.0-dev'
}

# --- Publish -------------------------------------------------------------
$PublishDir = Join-Path $Root 'artifacts\publish'
if (Test-Path $PublishDir) { Remove-Item -Recurse -Force $PublishDir }

dotnet publish (Join-Path $Root 'Rigshift\Rigshift.csproj') -c Release -r win-x64 --self-contained true -o $PublishDir
if ($LASTEXITCODE -ne 0) {
    Fail "dotnet publish failed (exit $LASTEXITCODE)."
}

# --- Locate ISCC ---------------------------------------------------------
$iscc = $null
$candidate = Get-Command 'ISCC.exe' -ErrorAction SilentlyContinue
if ($candidate) {
    $iscc = $candidate.Source
}
if (-not $iscc) {
    foreach ($dir in @(
        (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6'),
        (Join-Path $env:ProgramFiles 'Inno Setup 6'),
        (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6')
    )) {
        $probe = Join-Path $dir 'ISCC.exe'
        if (Test-Path $probe) {
            $iscc = $probe
            break
        }
    }
}
if (-not $iscc) {
    Fail "Inno Setup 6 not found. Install it with: winget install --id JRSoftware.InnoSetup -e --version 6.7.1"
}

# --- Compile installer ---------------------------------------------------
$ScriptPath = Join-Path $Root 'installer\rigshift.iss'
& $iscc "/DAppVersion=$Version" "/DSourceDir=$PublishDir" $ScriptPath
if ($LASTEXITCODE -ne 0) {
    Fail "ISCC failed (exit $LASTEXITCODE)."
}

# --- Verify --------------------------------------------------------------
$Setup = Join-Path $Root "artifacts\installer\Rigshift-Setup-$Version-x64.exe"
if (-not (Test-Path $Setup)) {
    Fail "Expected installer not produced: $Setup"
}

Write-Output "Installer: $Setup"
