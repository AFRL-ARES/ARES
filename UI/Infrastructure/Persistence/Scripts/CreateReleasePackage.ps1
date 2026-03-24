<#
.SYNOPSIS
    Mimics the Windows portion of the ARES OS Release Pipeline for local builds.
.DESCRIPTION
    Builds the Ares Device Drivers from a local path, restores/publishes the UI project, 
    copies plugins, and packages the result into a ZIP file.
.PARAMETER TagName
    The release version tag (e.g., v1.0.0). Defaults to 'local-dev'.
.PARAMETER DriversRepoPath
    Relative path to drivers from THIS script's location.
.PARAMETER UiProjectPath
    Relative path to UI.csproj from THIS script's location.
#>
param (
    [string]$TagName = "local-dev",
    [string]$DriversRepoPath = "..\..\..\..\..\Ares.Device.Drivers",
    [string]$UiProjectPath = "..\..\..\UI.csproj" 
)

$ErrorActionPreference = "Stop"

# Get the exact directory where THIS script is located (failsafe for older PS versions)
$ScriptDir = $PSScriptRoot
if (-not $ScriptDir) { $ScriptDir = (Get-Location).Path }

# ====================================================================
# ABSOLUTE PATH RESOLUTION
# Forcing absolute paths prevents all bugs related to the terminal's 
# current working directory.
# ====================================================================
$AbsoluteUiProject   = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($ScriptDir, $UiProjectPath))
$AbsoluteDriversRepo = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($ScriptDir, $DriversRepoPath))
$AbsoluteOutputPath  = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($ScriptDir, "publish"))
$AbsoluteZipPath     = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($ScriptDir, "ARES-OS-$TagName-windows.zip"))
$RUNTIME_ID          = "win-x64"

Write-Host "======================================================" -ForegroundColor Cyan
Write-Host " Starting ARES OS Local Windows Release Build" -ForegroundColor Cyan
Write-Host " Tag: $TagName | Target: $RUNTIME_ID" -ForegroundColor Cyan
Write-Host "------------------------------------------------------" -ForegroundColor Cyan
Write-Host " Script Dir:   $ScriptDir" -ForegroundColor DarkGray
Write-Host " UI Project:   $AbsoluteUiProject" -ForegroundColor DarkGray
Write-Host " Drivers Repo: $AbsoluteDriversRepo" -ForegroundColor DarkGray
Write-Host " Output Dir:   $AbsoluteOutputPath" -ForegroundColor DarkGray
Write-Host "======================================================" -ForegroundColor Cyan

# 1. Validate Target Paths
Write-Host "`n[1/6] Validating Target Paths..." -ForegroundColor Yellow
if (-not (Test-Path $AbsoluteDriversRepo)) {
    Write-Error "Device drivers repository not found at '$AbsoluteDriversRepo'."
    exit 1
}
if (-not (Test-Path $AbsoluteUiProject)) {
    Write-Error "UI Project file not found at '$AbsoluteUiProject'. Check your relative path parameters."
    exit 1
}
Write-Host "Paths validated."

# 2. Build Device Drivers
Write-Host "`n[2/6] Building Device Drivers..." -ForegroundColor Yellow
Push-Location $AbsoluteDriversRepo
dotnet build Ares.Device.Drivers.slnx --configuration Release
Pop-Location

# 3. Restore UI Dependencies
Write-Host "`n[3/6] Restoring Dependencies for UI..." -ForegroundColor Yellow
dotnet restore $AbsoluteUiProject

# 4. Build and Publish Self-Contained Release Artifacts
Write-Host "`n[4/6] Publishing ARES Project ($RUNTIME_ID)..." -ForegroundColor Yellow
dotnet publish $AbsoluteUiProject --configuration Release --output $AbsoluteOutputPath /p:UseAppHost=true -r $RUNTIME_ID --self-contained true

# 5. Copy Driver Plugins to ARES Output
Write-Host "`n[5/6] Copying Driver Plugins to Output Directory..." -ForegroundColor Yellow
$PluginSource = Join-Path $AbsoluteDriversRepo "plugins"
$PluginTarget = Join-Path $AbsoluteOutputPath "plugins"

if (Test-Path $PluginSource) {
    if (-not (Test-Path $PluginTarget)) {
        New-Item -ItemType Directory -Path $PluginTarget -Force | Out-Null
    }
    Copy-Item -Path "$PluginSource\*" -Destination $PluginTarget -Recurse -Force
    Write-Host "Plugins copied successfully."
} else {
    Write-Warning "Source plugin directory '$PluginSource' not found. Skipping plugin copy."
}

# 6. Create ZIP Archive
Write-Host "`n[6/6] Creating ZIP Archive..." -ForegroundColor Yellow
if (Test-Path $AbsoluteZipPath) { Remove-Item $AbsoluteZipPath -Force }

if (Get-Command "7z.exe" -ErrorAction SilentlyContinue) {
    Write-Host "Using 7-Zip..."
    7z a -r "$AbsoluteZipPath" "$AbsoluteOutputPath\*"
} else {
    Write-Host "7-Zip not found in PATH. Using PowerShell Compress-Archive..."
    Compress-Archive -Path "$AbsoluteOutputPath\*" -DestinationPath $AbsoluteZipPath -Force
}

Write-Host "`n======================================================" -ForegroundColor Green
Write-Host " Build Complete!" -ForegroundColor Green
Write-Host " Artifact saved to: $AbsoluteZipPath" -ForegroundColor Green
Write-Host "======================================================" -ForegroundColor Green