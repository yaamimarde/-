# Run from repo root: .\scripts\start-local.ps1
# Prepares Docker MySQL/Redis and frees ports 5100/5246 for local dotnet run.

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root

Write-Host "=== Local dotnet run setup ===" -ForegroundColor Cyan

if (-not (Test-Path ".env")) {
    Write-Host "FAIL: missing .env — run: copy .env.example .env" -ForegroundColor Red
    exit 1
}

$envPassword = $null
Get-Content ".env" | ForEach-Object {
    if ($_ -match '^\s*MYSQL_ROOT_PASSWORD\s*=\s*(.+)\s*$') {
        $envPassword = $matches[1].Trim().Trim('"').Trim("'")
    }
}

$devSettingsPath = "Pharmaceutical.WebAPI\appsettings.Development.json"
if ($envPassword -and (Test-Path $devSettingsPath)) {
    $devJson = Get-Content $devSettingsPath -Raw
    if ($devJson -notmatch [regex]::Escape($envPassword)) {
        Write-Host "WARN: appsettings.Development.json password may not match .env MYSQL_ROOT_PASSWORD" -ForegroundColor Yellow
        Write-Host "      Update ConnectionStrings:DefaultConnection in $devSettingsPath" -ForegroundColor Yellow
    } else {
        Write-Host "OK: Development connection string matches .env password" -ForegroundColor Green
    }
}

Write-Host "`nStopping Docker blazor/webapi (free ports 5100, 5246)..." -ForegroundColor Cyan
docker compose stop blazor webapi

Write-Host "Starting MySQL and Redis..." -ForegroundColor Cyan
docker compose up -d mysql redis

Write-Host "Waiting for MySQL..." -ForegroundColor Cyan
$ready = $false
for ($i = 0; $i -lt 30; $i++) {
    $status = docker compose ps mysql --format "{{.Status}}" 2>$null
    if ($status -match "healthy") {
        $ready = $true
        break
    }
    Start-Sleep -Seconds 2
}

if ($ready) {
    Write-Host "OK: MySQL is healthy" -ForegroundColor Green
} else {
    Write-Host "WARN: MySQL not healthy yet — wait a few seconds before dotnet run" -ForegroundColor Yellow
}

Write-Host @"

Apply EF migrations (optional, first time or after model changes):
  `$env:ASPNETCORE_ENVIRONMENT='Development'
  dotnet ef database update --project Pharmaceutical.Infrastructure --startup-project Pharmaceutical.WebAPI

If you see 'Access denied for user root', MySQL was initialized with a different password.
Either align .env + appsettings.Development.json with that password, or reset the DB volume:
  docker compose down -v
  docker compose up -d mysql redis

"@ -ForegroundColor DarkGray

Write-Host @"

Next: open TWO terminals in this folder and run:

  Terminal 1 (WebAPI):
    dotnet run --project Pharmaceutical.WebAPI

  Terminal 2 (Blazor):
    dotnet run --project 前端页面/Pharmaceutical.Blazor

Then open: http://localhost:5100/login
Login: admin / Admin@123

To switch back to Docker-only mode:
  docker compose up -d

"@ -ForegroundColor White
