#Requires -Version 5.1
<#
.SYNOPSIS
    ExamShield first-time setup script (Windows PowerShell / PowerShell Core)
.PARAMETER Demo
    Enable automatic demo data seeding on first API startup.
.EXAMPLE
    .\setup.ps1
    .\setup.ps1 -Demo
#>
param([switch]$Demo)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

function Write-Ok   { param($msg) Write-Host "  [OK] $msg"   -ForegroundColor Green }
function Write-Warn { param($msg) Write-Host "  [!!] $msg"   -ForegroundColor Yellow }
function Write-Err  { param($msg) Write-Host "  [XX] $msg"   -ForegroundColor Red; exit 1 }
function Write-Info { param($msg) Write-Host "  --> $msg"    -ForegroundColor Cyan }
function Write-Sep  { param($msg) Write-Host "`n=== $msg ===" -ForegroundColor Cyan }

Write-Host @"
  +--------------------------------------+
  |        ExamShield  Setup             |
  |   Secure Exam Scanning System        |
  +--------------------------------------+
"@ -ForegroundColor Cyan

# ── 1. Prerequisites ──────────────────────────────────────────────────────────
Write-Sep "Checking prerequisites"

function Require-Command {
    param($Name)
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        Write-Err "$Name is required. Install it and re-run."
    }
    Write-Ok "$Name found"
}

Require-Command "docker"
Require-Command "dotnet"

$dotnetVersion = (dotnet --version) -split '\.' | Select-Object -First 1
if ([int]$dotnetVersion -lt 9) {
    Write-Err ".NET 9+ SDK required (found $(dotnet --version))"
}
Write-Ok ".NET SDK $(dotnet --version)"

if (Get-Command "node" -ErrorAction SilentlyContinue) {
    Write-Ok "Node.js $(node --version)"
} else {
    Write-Warn "Node.js not found — dashboard dev server unavailable"
}

# ── 2. Environment file ───────────────────────────────────────────────────────
Write-Sep "Configuring environment"

$EnvFile    = Join-Path $ScriptDir "infra\.env"
$EnvExample = Join-Path $ScriptDir "infra\.env.example"

if (Test-Path $EnvFile) {
    Write-Ok ".env already exists — skipping generation"
} else {
    Write-Info "Generating .env from .env.example with secure random values…"
    Copy-Item $EnvExample $EnvFile

    function New-RandomString {
        param([int]$Length = 32, [string]$Chars = 'ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789@#%^&*')
        -join ((1..$Length) | ForEach-Object { $Chars[(Get-Random -Maximum $Chars.Length)] })
    }

    $PgPass      = New-RandomString -Length 24
    $RmqPass     = New-RandomString -Length 24
    $MinioSecret = New-RandomString -Length 32

    $content = Get-Content $EnvFile -Raw
    $content = $content -replace 'POSTGRES_PASSWORD=.*',  "POSTGRES_PASSWORD=$PgPass"
    $content = $content -replace 'RABBITMQ_PASSWORD=.*',  "RABBITMQ_PASSWORD=$RmqPass"
    $content = $content -replace 'MINIO_SECRET_KEY=.*',   "MINIO_SECRET_KEY=$MinioSecret"
    Set-Content $EnvFile -Value $content -NoNewline

    # Update appsettings.Development.json
    $SettingsPath = Join-Path $ScriptDir "src\ExamShield.Api\appsettings.Development.json"
    if (Test-Path $SettingsPath) {
        $JwtSecret = New-RandomString -Length 64
        $AesBytes  = [byte[]]::new(32)
        [System.Security.Cryptography.RandomNumberGenerator]::Fill($AesBytes)
        $AesKey = [Convert]::ToBase64String($AesBytes)

        $cfg = Get-Content $SettingsPath -Raw | ConvertFrom-Json
        $cfg.Jwt.Secret = $JwtSecret
        $cfg.Encryption.MasterKeyBase64 = $AesKey
        $cfg | ConvertTo-Json -Depth 10 | Set-Content $SettingsPath
        Write-Info "JWT secret and AES key regenerated in appsettings.Development.json"
    }

    Write-Ok ".env generated"
}

# ── 3. Docker infrastructure ──────────────────────────────────────────────────
Write-Sep "Starting infrastructure (Docker Compose)"

Push-Location (Join-Path $ScriptDir "infra")
docker compose up -d
Write-Info "Waiting for services to become healthy…"

function Wait-Healthy {
    param([string]$ServiceName, [int]$TimeoutSec = 90)
    $elapsed = 0
    while ($true) {
        $containerId = (docker compose ps -q $ServiceName 2>$null) -join ''
        if ($containerId) {
            $health = docker inspect --format '{{.State.Health.Status}}' $containerId 2>$null
            if ($health -eq 'healthy') { Write-Ok $ServiceName; return }
        }
        Start-Sleep -Seconds 3
        $elapsed += 3
        Write-Host "    ${ServiceName}… ${elapsed}s" -NoNewline "`r"
        if ($elapsed -ge $TimeoutSec) { Write-Err "$ServiceName did not become healthy after ${TimeoutSec}s" }
    }
}

Wait-Healthy "postgres"
Wait-Healthy "redis"
Wait-Healthy "rabbitmq"
Wait-Healthy "minio"
Pop-Location

# ── 4. Database migrations ────────────────────────────────────────────────────
Write-Sep "Applying database migrations"

Push-Location $ScriptDir
try {
    dotnet ef database update `
        --project src/ExamShield.Infrastructure `
        --startup-project src/ExamShield.Api `
        --no-build 2>$null
} catch {
    dotnet ef database update `
        --project src/ExamShield.Infrastructure `
        --startup-project src/ExamShield.Api
}
Write-Ok "Migrations applied"
Pop-Location

# ── 5. Build ──────────────────────────────────────────────────────────────────
Write-Sep "Building backend"
Push-Location $ScriptDir
dotnet build --configuration Release -q
Write-Ok "Build succeeded"
Pop-Location

# ── 6. Demo mode ──────────────────────────────────────────────────────────────
if ($Demo) {
    Write-Sep "Demo mode: enabling AutoSeedDemo"
    $SettingsPath = Join-Path $ScriptDir "src\ExamShield.Api\appsettings.Development.json"
    if (Test-Path $SettingsPath) {
        $cfg = Get-Content $SettingsPath -Raw | ConvertFrom-Json
        if (-not $cfg.PSObject.Properties['Features']) {
            $cfg | Add-Member -NotePropertyName 'Features' -NotePropertyValue @{}
        }
        $cfg.Features.AutoSeedDemo = $true
        $cfg | ConvertTo-Json -Depth 10 | Set-Content $SettingsPath
        Write-Info "AutoSeedDemo = true — demo data will seed on first API startup"
    }
}

# ── Done ──────────────────────────────────────────────────────────────────────
Write-Sep "Setup complete"
Write-Host ""
Write-Host "  Run these commands to start ExamShield:" -ForegroundColor Green
Write-Host ""
Write-Host "    # Terminal 1 - API" -ForegroundColor Cyan
Write-Host "    dotnet run --project src/ExamShield.Api"
Write-Host ""
Write-Host "    # Terminal 2 - Dashboard" -ForegroundColor Cyan
Write-Host "    cd src/ExamShield.Dashboard; npm install; npm run dev"
Write-Host ""
Write-Host "  Then open http://localhost:5173/setup to complete installation." -ForegroundColor Cyan
Write-Host ""
if ($Demo) {
    Write-Host "  Demo credentials (after API starts):" -ForegroundColor Yellow
    Write-Host "    Email:    admin@examshield.local"
    Write-Host "    Password: Demo@1234"
    Write-Host ""
}
