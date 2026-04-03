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
$GhExe = (Get-Command gh -ErrorAction SilentlyContinue)?.Source
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
Write-Host "`n[1/8] Updating csproj version..." -ForegroundColor Yellow
$CsprojPath = "$Root/src/DesktopTranslation/DesktopTranslation.csproj"
$csproj = Get-Content $CsprojPath -Raw
$csproj = $csproj -replace '<Version>[^<]+</Version>', "<Version>$Version</Version>"
Set-Content -Path $CsprojPath -Value $csproj -NoNewline
Write-Host "  csproj version set to $Version"

# ── 2. Update website version and download link ──────────────────
Write-Host "`n[2/8] Updating website..." -ForegroundColor Yellow
$IndexPath = "$Root/website/index.html"
if (Test-Path $IndexPath) {
    $html = Get-Content $IndexPath -Raw

    # Update version badge
    $html = $html -replace 'v[\d]+\.[\d]+\.[\d]+ — 免費開源', "v$Version — 免費開源"

    # Update download link href (GitHub Release direct download)
    $html = $html -replace 'https://github\.com/air92316/DesktopTranslation/releases/latest/download/DesktopTranslation-v[\d]+\.[\d]+\.[\d]+-Setup\.exe',
                           "https://github.com/air92316/DesktopTranslation/releases/latest/download/DesktopTranslation-v$Version-Setup.exe"

    # Update download button text
    $html = $html -replace '免費下載 v[\d]+\.[\d]+\.[\d]+',
                           "免費下載 v$Version"

    Set-Content -Path $IndexPath -Value $html -NoNewline
    Write-Host "  website index.html updated"
} else {
    Write-Host "  website/index.html not found, skipping" -ForegroundColor DarkYellow
}

# ── 3. Update Inno Setup version ─────────────────────────────────
Write-Host "`n[3/8] Updating Inno Setup script..." -ForegroundColor Yellow
$IssPath = "$Root/installer/setup.iss"
if (Test-Path $IssPath) {
    $iss = Get-Content $IssPath -Raw
    $iss = $iss -replace '#define MyAppVersion "[^"]*"', "#define MyAppVersion `"$Version`""
    Set-Content -Path $IssPath -Value $iss -NoNewline
    Write-Host "  setup.iss version set to $Version"
} else {
    Write-Host "  installer/setup.iss not found, skipping" -ForegroundColor DarkYellow
}

# ── 4. dotnet publish ─────────────────────────────────────────────
Write-Host "`n[4/8] Publishing..." -ForegroundColor Yellow
$PublishDir = "$Root/publish"
if (Test-Path $PublishDir) { Remove-Item -Recurse -Force $PublishDir }
dotnet publish "$Root/src/DesktopTranslation/DesktopTranslation.csproj" `
    -c Release -r win-x64 --self-contained false `
    -o $PublishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }
Write-Host "  Published to $PublishDir"

# ── 5. Inno Setup compile ────────────────────────────────────────
Write-Host "`n[5/8] Compiling installer..." -ForegroundColor Yellow
$IsccPaths = @(
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe"
)
$Iscc = $IsccPaths | Where-Object { Test-Path $_ } | Select-Object -First 1

if ($Iscc) {
    & $Iscc "$Root/installer/setup.iss"
    if ($LASTEXITCODE -ne 0) { throw "Inno Setup compilation failed" }
    Write-Host "  Installer compiled"
} else {
    Write-Host "  ISCC.exe not found, skipping installer compilation" -ForegroundColor DarkYellow
}

# ── 6. Git commit and tag ────────────────────────────────────────
Write-Host "`n[6/8] Git commit and tag..." -ForegroundColor Yellow
Push-Location $Root
git add src/DesktopTranslation/DesktopTranslation.csproj
git add installer/setup.iss
if (Test-Path "$Root/website/index.html") { git add website/index.html }
git commit -m "release: v$Version"
git tag "v$Version"
git push origin main --tags
Pop-Location

# ── 7. GitHub Release ─────────────────────────────────────────────
Write-Host "`n[7/8] Creating GitHub release..." -ForegroundColor Yellow
$SetupExe = "$Root/dist/DesktopTranslation-v$Version-Setup.exe"
if (Test-Path $SetupExe) {
    & $GhExe release create "v$Version" $SetupExe `
        --title "v$Version" `
        --generate-notes
    if ($LASTEXITCODE -ne 0) { throw "gh release create failed" }
    Write-Host "  GitHub release v$Version created"
} else {
    Write-Host "  Setup exe not found at $SetupExe, skipping release" -ForegroundColor DarkYellow
}

# ── 8. Deploy website ─────────────────────────────────────────────
Write-Host "`n[8/8] Deploying website..." -ForegroundColor Yellow
$WebDir = "$Root/website"
if (Test-Path $WebDir) {
    Push-Location $WebDir
    npx wrangler pages deploy . --project-name desktop-translation
    if ($LASTEXITCODE -ne 0) { Write-Host "  wrangler deploy failed" -ForegroundColor Red }
    else { Write-Host "  Website deployed" }
    Pop-Location
} else {
    Write-Host "  Skipping website deploy (missing website dir)" -ForegroundColor DarkYellow
}

Write-Host "`n=== Release v$Version complete ===" -ForegroundColor Green
