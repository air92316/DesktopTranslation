# scripts/release.ps1
# DesktopTranslation Release Script
# Usage: .\scripts\release.ps1 -Version "1.2.0"

param(
    [Parameter(Mandatory = $true)]
    [string]$Version
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if (-not (Test-Path "$Root/src/DesktopTranslation")) {
    $Root = Split-Path -Parent $PSScriptRoot
}
if (-not (Test-Path "$Root/src/DesktopTranslation")) {
    $Root = $PSScriptRoot | Split-Path
}

Write-Host "=== DesktopTranslation Release v$Version ===" -ForegroundColor Cyan
Write-Host "Root: $Root"

# ── Pre-flight checks ────────────────────────────────────────────
Write-Host "`nPre-flight checks..." -ForegroundColor Yellow

# Check git working tree is clean
$gitStatus = git -C $Root status --porcelain
if ($gitStatus) {
    Write-Host "  ERROR: Git working tree is not clean. Commit or stash changes first." -ForegroundColor Red
    Write-Host $gitStatus
    exit 1
}

# Check required tools
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "dotnet CLI not found in PATH"
}

# Find GitHub CLI (check PATH first, then common install locations)
$GhCmd = Get-Command gh -ErrorAction SilentlyContinue
$GhExe = if ($GhCmd) { $GhCmd.Source } else { $null }
if (-not $GhExe) {
    $GhPaths = @(
        "C:\Program Files\GitHub CLI\gh.exe",
        "C:\Program Files (x86)\GitHub CLI\gh.exe",
        "$env:LOCALAPPDATA\Programs\GitHub CLI\gh.exe"
    )
    $GhExe = $GhPaths | Where-Object { Test-Path $_ } | Select-Object -First 1
}
if (-not $GhExe) {
    throw "GitHub CLI (gh) not found in PATH or common install locations"
}
Write-Host "  GitHub CLI: $GhExe"

Write-Host "  All checks passed" -ForegroundColor Green

# ── 1. Update csproj version ─────────────────────────────────────
Write-Host "`n[1/6] Updating csproj version..." -ForegroundColor Yellow
$CsprojPath = "$Root/src/DesktopTranslation/DesktopTranslation.csproj"
$csproj = [System.IO.File]::ReadAllText($CsprojPath, [System.Text.Encoding]::UTF8)
$csproj = $csproj -replace '<Version>[^<]+</Version>', "<Version>$Version</Version>"
[System.IO.File]::WriteAllText($CsprojPath, $csproj, [System.Text.Encoding]::UTF8)
Write-Host "  csproj version set to $Version"

# ── 2. Update Inno Setup version ─────────────────────────────────
Write-Host "`n[2/6] Updating Inno Setup script..." -ForegroundColor Yellow
$IssPath = "$Root/installer/setup.iss"
if (Test-Path $IssPath) {
    $iss = [System.IO.File]::ReadAllText($IssPath, [System.Text.Encoding]::UTF8)
    $iss = $iss -replace '#define MyAppVersion "[^"]*"', "#define MyAppVersion `"$Version`""
    [System.IO.File]::WriteAllText($IssPath, $iss, [System.Text.Encoding]::UTF8)
    Write-Host "  setup.iss version set to $Version"
} else {
    Write-Host "  installer/setup.iss not found, skipping" -ForegroundColor DarkYellow
}

# ── 3. dotnet publish ─────────────────────────────────────────────
Write-Host "`n[3/6] Publishing..." -ForegroundColor Yellow
$PublishDir = "$Root/publish"
if (Test-Path $PublishDir) { Remove-Item -Recurse -Force $PublishDir }
dotnet publish "$Root/src/DesktopTranslation/DesktopTranslation.csproj" `
    -c Release -r win-x64 --self-contained false `
    -o $PublishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }
Write-Host "  Published to $PublishDir"

# ── 4. Inno Setup compile ────────────────────────────────────────
Write-Host "`n[4/6] Compiling installer..." -ForegroundColor Yellow
$IsccPaths = @(
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe"
)
$Iscc = $IsccPaths | Where-Object { Test-Path $_ } | Select-Object -First 1

if ($Iscc) {
    & $Iscc "$Root/installer/setup.iss"
    if ($LASTEXITCODE -ne 0) { throw "Inno Setup compilation failed" }
    Write-Host "  Installer compiled"
} else {
    throw "ISCC.exe not found. Install Inno Setup 6 first."
}

# ── 5. Git commit and tag ────────────────────────────────────────
Write-Host "`n[5/6] Git commit and tag..." -ForegroundColor Yellow
Push-Location $Root
git add src/DesktopTranslation/DesktopTranslation.csproj
git add installer/setup.iss
git commit -m "release: v$Version"
git tag "v$Version"
git push origin master --tags
Pop-Location

# ── 6. GitHub Release ─────────────────────────────────────────────
Write-Host "`n[6/6] Creating GitHub release..." -ForegroundColor Yellow
$SetupExe = "$Root/dist/DesktopTranslation-v$Version-Setup.exe"
if (Test-Path $SetupExe) {
    & $GhExe release create "v$Version" $SetupExe `
        --title "v$Version" `
        --generate-notes
    if ($LASTEXITCODE -ne 0) { throw "gh release create failed" }
    Write-Host "  GitHub release v$Version created"
} else {
    throw "Setup exe not found at $SetupExe"
}

Write-Host "`n=== Release v$Version complete ===" -ForegroundColor Green
