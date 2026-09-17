# Run from repo root: .\scripts\verify-deployment.ps1

$ErrorActionPreference = "Continue"
Write-Host "=== Pharmaceutical deployment check ===" -ForegroundColor Cyan

if (-not (Test-Path ".env")) {
    Write-Host "FAIL: missing .env (copy from .env.example)" -ForegroundColor Red
} else {
    Write-Host "OK: .env exists" -ForegroundColor Green
}

try {
    $health = Invoke-RestMethod -Uri "http://localhost:5246/health" -TimeoutSec 5
    Write-Host "OK: API health = $health" -ForegroundColor Green
} catch {
    Write-Host "FAIL: API not reachable at :5246/health" -ForegroundColor Red
}

try {
    $r = Invoke-WebRequest -Uri "http://localhost:5100/" -UseBasicParsing -TimeoutSec 15
    Write-Host "OK: Blazor status $($r.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "WARN: Blazor not ready at :5100" -ForegroundColor Yellow
}

try {
    $login = Invoke-RestMethod -Uri "http://localhost:5246/api/auth/login" -Method POST `
        -Body '{"username":"admin","password":"Admin@123"}' -ContentType "application/json" -TimeoutSec 5
    $h = @{ Authorization = "Bearer $($login.data.token)" }
    $drugUrl = "http://localhost:5246/api/drug?page=1&pageSize=1"
    $drugs = Invoke-RestMethod -Uri $drugUrl -Headers $h
    Write-Host "OK: login, drug count = $($drugs.data.totalCount)" -ForegroundColor Green
    $po = Invoke-RestMethod -Uri "http://localhost:5246/api/purchaseorder" -Headers $h
    Write-Host "OK: purchase orders = $($po.data.Count)" -ForegroundColor Green
} catch {
    Write-Host "FAIL: login or API check failed" -ForegroundColor Red
}

Write-Host "Open http://localhost:5100 in browser" -ForegroundColor Cyan
