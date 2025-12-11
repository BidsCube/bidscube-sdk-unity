<#
.SYNOPSIS
Copies runtime files from Assets/BidscubeSDK into a Runtime package folder.

.DESCRIPTION
Windows PowerShell port of the project's `scripts/copy-to-runtime.sh`.
It uses Robocopy when available and supports mirroring, exclusions and a dry-run mode.

.PARAMETER Dest
Destination base folder. Default: <repo-root>\Runtime

.PARAMETER IncludeEditor
Include the Editor folder in the copy.

.PARAMETER NoAssets
Copy SDK directly under <dest>\BidscubeSDK instead of <dest>\Assets\BidscubeSDK.

.PARAMETER Flatten
Copy the contents of Assets/BidscubeSDK directly into <dest> (no wrapping folder).

.PARAMETER DryRun
List actions without performing file changes.
#>
[CmdletBinding()]
param(
    [string]$Dest,
    [switch]$IncludeEditor,
    [switch]$NoAssets,
    [switch]$Flatten,
    [switch]$DryRun
)

function Show-Usage {
    Write-Host "Usage: .\scripts\copy-to-runtime.ps1 [-Dest <path>] [-IncludeEditor] [-NoAssets] [-Flatten] [-DryRun]" -ForegroundColor Yellow
    Write-Host "Default destination is <repo-root>\Runtime" -ForegroundColor Yellow
}

# Determine repository root based on script location
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path -Path (Join-Path $scriptDir "..")
$repoRoot = $repoRoot.Path

if (-not $Dest) {
    $Dest = Join-Path $repoRoot 'Runtime'
} else {
    # allow relative paths
    if (-not (Split-Path -IsAbsolute $Dest)) {
        $Dest = Join-Path $repoRoot $Dest
    }
}

# Sources: BidscubeSDK folder and Plugins folder
$srcBidscubeDir = Join-Path $repoRoot 'Assets\BidscubeSDK'
$srcPluginsDir = Join-Path $repoRoot 'Assets\Plugins'

if (-not (Test-Path -Path $srcBidscubeDir -PathType Container)) {
    Write-Error "Source folder '$srcBidscubeDir' not found. Run this script from the repository or ensure Assets/BidscubeSDK exists."
    exit 3
}

# Destinations: ensure Runtime contains 'BidscubeSDK' and 'Plugins' folders
$destBidscubeDir = Join-Path $Dest 'BidscubeSDK'
$destPluginsDir = Join-Path $Dest 'Plugins'

Write-Host "Will copy:"
Write-Host "  $srcBidscubeDir -> $destBidscubeDir"
Write-Host "  $srcPluginsDir -> $destPluginsDir"

# If user requested a legacy flatten/no-assets behavior, respect it by mapping accordingly
if ($Flatten) { $destBidscubeDir = $Dest }    # flatten BidscubeSDK into root
if ($NoAssets)  { $destBidscubeDir = Join-Path $Dest 'BidscubeSDK' }  # explicit

Write-Host "Destination BidscubeSDK: $destBidscubeDir"
Write-Host "Destination Plugins: $destPluginsDir"
if ($IncludeEditor) {
    Write-Host "Including Editor folder in copy."
} else {
    Write-Host "Excluding Editor folder from copy."
}
if ($DryRun) { Write-Host "Dry run: no files will be written." }

# Build exclusion lists
$excludeDirs = @('Library','Temp','.git','.vs')
if (-not $IncludeEditor) { $excludeDirs += 'Editor' }
$excludeFiles = @('*.csproj','*.sln','*.user','*.unitypackage')
# Note: we intentionally do not exclude *.meta by default (Unity needs meta files for imports). Adjust if desired.

# Ensure destination exists (create parent folders)
if (-not $DryRun) {
    New-Item -ItemType Directory -Path $destBidscubeDir -Force | Out-Null
    New-Item -ItemType Directory -Path $destPluginsDir -Force | Out-Null
}

# Choose copy method: robocopy preferred. We'll run it twice: for BidscubeSDK and for Plugins (if present)
$robocopy = Get-Command -Name robocopy -ErrorAction SilentlyContinue
function Run-CopyRobocopy($src, $dst, $excludeDirsLocal, $excludeFilesLocal) {
    $robocopyArgs = @()
    if ($excludeDirsLocal.Count -gt 0) { $robocopyArgs += ('/XD'); $robocopyArgs += $excludeDirsLocal }
    if ($excludeFilesLocal.Count -gt 0) { $robocopyArgs += ('/XF'); $robocopyArgs += $excludeFilesLocal }
    $robocopyArgs += '/MIR'; $robocopyArgs += '/Z'; $robocopyArgs += '/R:2'; $robocopyArgs += '/W:1'; $robocopyArgs += '/NFL'; $robocopyArgs += '/NDL'; $robocopyArgs += '/NP'; $robocopyArgs += '/V'
    if ($DryRun) { $robocopyArgs += '/L' }
    $joinedArgs = $robocopyArgs -join ' '
    Write-Host "robocopy `"$src`" `"$dst`" $joinedArgs"
    & robocopy $src $dst "*" @robocopyArgs
    if ($LASTEXITCODE -ge 8) { Write-Error "Robocopy failed for $src -> $dst with exit code $LASTEXITCODE"; exit $LASTEXITCODE }
}

if ($null -ne $robocopy) {
    Write-Host "Using robocopy to copy folders..." -ForegroundColor Cyan
    # Ensure destination parent folders exist (robocopy will create dest but we create parent to be safe)
    if (-not $DryRun) { New-Item -ItemType Directory -Path $destBidscubeDir -Force | Out-Null }
    # For BidscubeSDK we may want to exclude Editor unless included
    $bidExcludeDirs = @('Library','Temp','.git','.vs')
    if (-not $IncludeEditor) { $bidExcludeDirs += 'Editor' }
    $bidExcludeFiles = @('*.csproj','*.sln','*.user','*.unitypackage')
    Run-CopyRobocopy $srcBidscubeDir $destBidscubeDir $bidExcludeDirs $bidExcludeFiles

    # Copy Plugins if exists
    if (Test-Path -Path $srcPluginsDir -PathType Container) {
        if (-not $DryRun) { New-Item -ItemType Directory -Path $destPluginsDir -Force | Out-Null }
        # Plugins typically don't need excludes
        Run-CopyRobocopy $srcPluginsDir $destPluginsDir @() @()
    } else {
        Write-Host "Plugins source not found at $srcPluginsDir; skipping Plugins copy." -ForegroundColor Yellow
    }
} else {
    Write-Warning "robocopy not found. Falling back to Copy-Item. This will not perform deletes (no mirror)."
    # BidscubeSDK copy via Copy-Item with exclusions
    if ($DryRun) { Write-Host "Dry-run: listing BidscubeSDK files that would be copied..." }
    Get-ChildItem -Path $srcBidscubeDir -Recurse -File | Where-Object {
        foreach ($ex in $excludeDirs) { if ($_.FullName -like "*\\$ex\\*") { return $false } }
        foreach ($ef in $excludeFiles) { if ($_.Name -like $ef) { return $false } }
        return $true
    } | ForEach-Object {
        $relative = $_.FullName.Substring($srcBidscubeDir.Length).TrimStart('\')
        $destPath = Join-Path $destBidscubeDir $relative
        $destDir = Split-Path -Parent $destPath
        if (-not $DryRun -and -not (Test-Path -Path $destDir)) { New-Item -ItemType Directory -Path $destDir -Force | Out-Null }
        if ($DryRun) { Write-Host $_.FullName } else { Copy-Item -Path $_.FullName -Destination $destPath -Force }
    }
    # Plugins via Copy-Item
    if (Test-Path -Path $srcPluginsDir -PathType Container) {
        if ($DryRun) { Write-Host "Dry-run: listing Plugins files that would be copied..." }
        Get-ChildItem -Path $srcPluginsDir -Recurse -File | ForEach-Object {
            $relative = $_.FullName.Substring($srcPluginsDir.Length).TrimStart('\')
            $destPath = Join-Path $destPluginsDir $relative
            $destDir = Split-Path -Parent $destPath
            if (-not $DryRun -and -not (Test-Path -Path $destDir)) { New-Item -ItemType Directory -Path $destDir -Force | Out-Null }
            if ($DryRun) { Write-Host $_.FullName } else { Copy-Item -Path $_.FullName -Destination $destPath -Force }
        }
    } else { Write-Host "Plugins source not found at $srcPluginsDir; skipping Plugins copy." -ForegroundColor Yellow }
}

# Copy metadata files
$metaFiles = @('package.json','README.md','LICENSE.md')
foreach ($mf in $metaFiles) {
    $srcMf = Join-Path $repoRoot $mf
    if (Test-Path -Path $srcMf -PathType Leaf) {
        $destMfDir = $Dest
        if (-not $DryRun) { New-Item -ItemType Directory -Path $destMfDir -Force | Out-Null }
        $destMf = Join-Path $destMfDir $mf
        Write-Host "Including $mf -> $destMf"
        if (-not $DryRun) { Copy-Item -Path $srcMf -Destination $destMf -Force }
    }
}

Write-Host "Done. Runtime package prepared at: $Dest" -ForegroundColor Green

# Summary
if ($DryRun) { Write-Host "Dry-run completed." -ForegroundColor Yellow } else { Write-Host "Copy completed." -ForegroundColor Green }

exit 0
