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

    # Update download link href
    $html = $html -replace 'downloads/DesktopTranslation-v[\d]+\.[\d]+\.[\d]+-Setup\.exe',
                           "downloads/DesktopTranslation-v$Version-Setup.exe"

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

# ── 6. GitHub Release ─────────────────────────────────────────────
Write-Host "`n[6/8] Creating GitHub release..." -ForegroundColor Yellow
$SetupExe = "$Root/dist/DesktopTranslation-v$Version-Setup.exe"
if (Test-Path $SetupExe) {
    gh release create "v$Version" $SetupExe `
        --title "v$Version" `
        --generate-notes
    if ($LASTEXITCODE -ne 0) { throw "gh release create failed" }
    Write-Host "  GitHub release v$Version created"
} else {
    Write-Host "  Setup exe not found at $SetupExe, skipping release" -ForegroundColor DarkYellow
}

# ── 7. Copy to website downloads and deploy ───────────────────────
Write-Host "`n[7/8] Deploying website..." -ForegroundColor Yellow
$WebDownloads = "$Root/website/downloads"
if ((Test-Path $SetupExe) -and (Test-Path $WebDownloads)) {
    Copy-Item $SetupExe "$WebDownloads/"
    Write-Host "  Copied installer to website/downloads/"

    Push-Location "$Root/website"
    npx wrangler pages deploy . --project-name desktop-translation
    if ($LASTEXITCODE -ne 0) { Write-Host "  wrangler deploy failed" -ForegroundColor Red }
    else { Write-Host "  Website deployed" }
    Pop-Location
} else {
    Write-Host "  Skipping website deploy (missing files)" -ForegroundColor DarkYellow
}

# ── 8. Git commit and push ────────────────────────────────────────
Write-Host "`n[8/8] Git commit..." -ForegroundColor Yellow
Push-Location $Root
git add -A
git commit -m "release: v$Version"
git tag "v$Version"
git push origin main --tags
Pop-Location

Write-Host "`n=== Release v$Version complete ===" -ForegroundColor Green
