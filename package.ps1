#Requires -Version 5.1
<#
.SYNOPSIS
    ExamShield deployment package builder (Windows PowerShell / PowerShell Core)
.PARAMETER Push
    Docker registry to push the image to (e.g. ghcr.io/yourorg).
.PARAMETER SkipTests
    Skip the test suite.
.PARAMETER Version
    Override the version string (default: git tag).
.EXAMPLE
    .\package.ps1
    .\package.ps1 -Push ghcr.io/yourorg -Version 1.2.3
#>
param(
    [string]$Push = "",
    [switch]$SkipTests,
    [string]$Version = ""
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

function Write-Ok   { param($m) Write-Host "  [OK] $m" -ForegroundColor Green }
function Write-Warn { param($m) Write-Host "  [!!] $m" -ForegroundColor Yellow }
function Write-Err  { param($m) Write-Host "  [XX] $m" -ForegroundColor Red; exit 1 }
function Write-Info { param($m) Write-Host "  --> $m"  -ForegroundColor Cyan }
function Write-Sep  { param($m) Write-Host "`n=== $m ===" -ForegroundColor Cyan }

Write-Host @"
  +--------------------------------------+
  |       ExamShield  Packager           |
  |  Build . Test . Bundle . Release     |
  +--------------------------------------+
"@ -ForegroundColor Cyan

# ── Version ───────────────────────────────────────────────────────────────────
Write-Sep "Version"

if ($Version) {
    $VersionClean = $Version.TrimStart('v')
    Write-Warn "Version overridden: $VersionClean"
} else {
    try {
        $VersionRaw = git -C $ScriptDir describe --tags --exact-match HEAD 2>$null
        if ($VersionRaw) {
            $VersionClean = $VersionRaw.TrimStart('v')
            Write-Ok "Git tag: $VersionClean"
        } else {
            $VersionRaw = git -C $ScriptDir describe --tags 2>$null
            if ($VersionRaw) {
                $VersionClean = $VersionRaw.TrimStart('v')
                Write-Warn "Not on exact tag — using describe: $VersionClean"
            } else {
                $ShortHash = git -C $ScriptDir rev-parse --short HEAD 2>$null
                $VersionClean = "0.0.0-dev-$ShortHash"
                Write-Warn "No git tags — using: $VersionClean"
            }
        }
    } catch {
        $VersionClean = "0.0.0-dev"
        Write-Warn "Git not available — using: $VersionClean"
    }
}

$PackageName = "examshield-$VersionClean"
$DistDir     = Join-Path $ScriptDir "dist"
$StageDir    = Join-Path $DistDir $PackageName

Write-Host "  Version : $VersionClean" -ForegroundColor Cyan
Write-Host "  Package : $PackageName.tar.gz" -ForegroundColor Cyan
if ($Push) { Write-Host "  Registry: $Push" -ForegroundColor Cyan }

# ── Prerequisites ─────────────────────────────────────────────────────────────
Write-Sep "Prerequisites"

foreach ($cmd in @('docker','dotnet','node','npm')) {
    if (-not (Get-Command $cmd -ErrorAction SilentlyContinue)) {
        Write-Err "$cmd is required but not found."
    }
    Write-Ok $cmd
}

# ── Tests ─────────────────────────────────────────────────────────────────────
if ($SkipTests) {
    Write-Warn "Skipping tests (-SkipTests)"
} else {
    Write-Sep "Running tests"
    Push-Location $ScriptDir
    dotnet test --configuration Release -q
    Write-Ok "All tests passed"
    Pop-Location
}

# ── Build API Docker image ────────────────────────────────────────────────────
Write-Sep "Building API Docker image"

$ApiImage  = "examshield-api"
$ApiTag    = "${ApiImage}:${VersionClean}"
$ApiLatest = "${ApiImage}:latest"

Push-Location $ScriptDir
$BuildDate = (Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ")
$GitRev    = (git rev-parse HEAD 2>$null) ?? ""

docker build `
    --file "src\ExamShield.Api\Dockerfile" `
    --tag $ApiTag `
    --tag $ApiLatest `
    --build-arg BUILD_CONFIGURATION=Release `
    --label "org.opencontainers.image.version=$VersionClean" `
    --label "org.opencontainers.image.created=$BuildDate" `
    --label "org.opencontainers.image.revision=$GitRev" `
    .
Write-Ok "Image built: $ApiTag"
Pop-Location

# ── Build Dashboard ───────────────────────────────────────────────────────────
Write-Sep "Building Dashboard"

Push-Location (Join-Path $ScriptDir "src\ExamShield.Dashboard")

if (-not (Test-Path "node_modules")) {
    Write-Info "Installing npm dependencies…"
    npm ci --prefer-offline --quiet
}

$env:VITE_API_URL = "/api"
npm run build -- --mode production
Write-Ok "Dashboard built"
Pop-Location

# ── Assemble staging directory ────────────────────────────────────────────────
Write-Sep "Assembling package"

if (Test-Path $StageDir) { Remove-Item $StageDir -Recurse -Force }
foreach ($sub in @('', 'dashboard', 'k8s', 'nginx', 'scripts')) {
    New-Item -ItemType Directory -Path (Join-Path $StageDir $sub) -Force | Out-Null
}

# Dashboard
Copy-Item (Join-Path $ScriptDir "src\ExamShield.Dashboard\dist\*") `
          (Join-Path $StageDir "dashboard") -Recurse -Force
Write-Ok "Dashboard → package/dashboard/"

# nginx config
Copy-Item (Join-Path $ScriptDir "infra\nginx\nginx.conf") (Join-Path $StageDir "nginx\")
Write-Ok "nginx.conf → package/nginx/"

# K8s manifests (stamp image tag)
$K8sSource = Join-Path $ScriptDir "infra\k8s"
Copy-Item "$K8sSource\*.yaml" (Join-Path $StageDir "k8s")
$ApiYaml = Join-Path $StageDir "k8s\api.yaml"
(Get-Content $ApiYaml) -replace 'image: examshield-api:latest', "image: $ApiTag" |
    Set-Content $ApiYaml
Write-Ok "k8s manifests → package/k8s/"

# Production compose (stamp version)
Copy-Item (Join-Path $ScriptDir "infra\docker-compose.prod.yml") $StageDir
$ProdCompose = Join-Path $StageDir "docker-compose.prod.yml"
(Get-Content $ProdCompose) -replace 'examshield-api}:latest', "examshield-api}:$VersionClean" |
    Set-Content $ProdCompose
Write-Ok "docker-compose.prod.yml → package/"

# Secrets template
@"
# ExamShield production environment — copy to .env and fill in all values.
# NEVER commit .env to version control.
# Generated for version: $VersionClean

APP_VERSION=$VersionClean

POSTGRES_PASSWORD=CHANGE_ME_min24chars
REDIS_PASSWORD=CHANGE_ME_min24chars
RABBITMQ_USER=examshield
RABBITMQ_PASSWORD=CHANGE_ME_min24chars
MINIO_ACCESS_KEY=CHANGE_ME
MINIO_SECRET_KEY=CHANGE_ME_min32chars
MINIO_BUCKET=examshield
MINIO_ENABLE_LOCK=true
JWT_SECRET=CHANGE_ME_at_least_64_chars
JWT_ISSUER=ExamShield
JWT_AUDIENCE=ExamShield
ENCRYPTION_MASTER_KEY=CHANGE_ME_base64_32bytes
WATERMARK_HMAC_KEY=CHANGE_ME_base64_32bytes
SERVER_SIGNING_KEY=-----BEGIN PRIVATE KEY-----\nCHANGE_ME\n-----END PRIVATE KEY-----
DASHBOARD_URL=https://examshield.yourorganization.com
DASHBOARD_PORT=80
API_PORT=8080
OCR_TYPE=Stub
"@ | Set-Content (Join-Path $StageDir ".env.template")
Write-Ok ".env.template → package/"

# Health check file for nginx
"ok" | Set-Content (Join-Path $StageDir "dashboard\health.txt")

# ── Save Docker image ─────────────────────────────────────────────────────────
Write-Sep "Saving Docker image"

$ImageTar = Join-Path $StageDir "api-image.tar"
Write-Info "docker save $ApiTag | gzip → api-image.tar…"
docker save $ApiTag | & { param() $input } | Out-File -FilePath "${ImageTar}.gz" -Encoding Byte
# Fallback: save without gzip if piping is unsupported
docker save --output $ImageTar $ApiTag
Write-Ok "api-image.tar saved"

# ── Create tarball ────────────────────────────────────────────────────────────
Write-Sep "Creating package archive"

$TarFile = Join-Path $DistDir "${PackageName}.tar.gz"
Push-Location $DistDir
tar -czf $TarFile $PackageName
Write-Ok "${PackageName}.tar.gz"

# Checksum
$Hash = (Get-FileHash $TarFile -Algorithm SHA256).Hash.ToLower()
"$Hash  ${PackageName}.tar.gz" | Set-Content "${TarFile}.sha256"
Write-Ok "SHA-256: $Hash"
Pop-Location

# ── Registry push (optional) ──────────────────────────────────────────────────
if ($Push) {
    Write-Sep "Pushing to registry: $Push"

    $RemoteTag    = "$Push/examshield-api:$VersionClean"
    $RemoteLatest = "$Push/examshield-api:latest"

    docker tag $ApiTag    $RemoteTag
    docker tag $ApiTag    $RemoteLatest
    docker push $RemoteTag
    docker push $RemoteLatest
    Write-Ok "Pushed: $RemoteTag"
    Write-Ok "Pushed: $RemoteLatest"

    # Update k8s manifest in staged dir with registry reference
    (Get-Content $ApiYaml) -replace [regex]::Escape("image: $ApiTag"), "image: $RemoteTag" |
        Set-Content $ApiYaml

    Push-Location $DistDir
    tar -czf $TarFile $PackageName
    $Hash = (Get-FileHash $TarFile -Algorithm SHA256).Hash.ToLower()
    "$Hash  ${PackageName}.tar.gz" | Set-Content "${TarFile}.sha256"
    Pop-Location
    Write-Info "Tarball re-created with registry image reference"
}

# ── Summary ───────────────────────────────────────────────────────────────────
Write-Sep "Done"
Write-Host ""
Write-Host "  Package:  dist\${PackageName}.tar.gz" -ForegroundColor Green
Write-Host "  Checksum: dist\${PackageName}.tar.gz.sha256" -ForegroundColor Green
Write-Host ""
Write-Host "  Contents:" -ForegroundColor Cyan
Write-Host "    api-image.tar           - Docker image"
Write-Host "    docker-compose.prod.yml - production Compose"
Write-Host "    dashboard/              - compiled React SPA"
Write-Host "    nginx/nginx.conf        - nginx serving config"
Write-Host "    k8s/                    - Kubernetes manifests"
Write-Host "    .env.template           - secrets template"
Write-Host "    DEPLOY.md               - operator guide"
Write-Host ""
if ($Push) { Write-Host "  Registry: $Push/examshield-api:$VersionClean" -ForegroundColor Green }
