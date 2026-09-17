# 写入或补充 Docker 演示数据（需 docker compose 已启动）
# 用法（在仓库根目录）:
#   .\scripts\seed-test-data.ps1          # 补充缺失的审计日志等（不删库）
#   .\scripts\seed-test-data.ps1 -Reset   # 清空业务表后全量重灌（保留用户账号）

param(
    [switch]$Reset
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Set-Location $root

if (-not (Test-Path ".env")) {
    Write-Host "请先复制 .env.example 为 .env 并配置 MYSQL_ROOT_PASSWORD" -ForegroundColor Red
    exit 1
}

$mysqlPass = (Get-Content .env | Where-Object { $_ -match '^MYSQL_ROOT_PASSWORD=' }) -replace '^MYSQL_ROOT_PASSWORD=', ''
$db = (Get-Content .env | Where-Object { $_ -match '^MYSQL_DATABASE=' }) -replace '^MYSQL_DATABASE=', ''
if (-not $db) { $db = "pharmaceutical" }

$mysql = "docker exec pharmaceutical-mysql mysql -uroot -p$mysqlPass $db"

if ($Reset) {
    Write-Host "清空业务数据表（保留用户与角色）..." -ForegroundColor Yellow
    $sql = @"
SET FOREIGN_KEY_CHECKS=0;
DELETE FROM purchase_order_lines;
DELETE FROM purchase_orders;
DELETE FROM stock_transactions;
DELETE FROM drug_batches;
DELETE FROM drugs;
DELETE FROM suppliers;
DELETE FROM audit_logs;
SET FOREIGN_KEY_CHECKS=1;
"@
    Invoke-Expression "$mysql -e `"$sql`""
    Write-Host "业务表已清空。" -ForegroundColor Green
}

if ((Get-Content .env) -notmatch 'SEED_DEMO_DATA=true') {
    Write-Host "提示：建议在 .env 中设置 SEED_DEMO_DATA=true" -ForegroundColor Yellow
}

Write-Host "重启 WebAPI 以执行 DatabaseSeeder..." -ForegroundColor Cyan
docker compose up -d --build webapi
docker compose restart webapi

Start-Sleep -Seconds 15

Write-Host "`n=== 当前数据量 ===" -ForegroundColor Cyan
Invoke-Expression "$mysql -e `"SELECT 'drugs' AS t, COUNT(*) AS n FROM drugs UNION SELECT 'suppliers', COUNT(*) FROM suppliers UNION SELECT 'purchase_orders', COUNT(*) FROM purchase_orders UNION SELECT 'drug_batches', COUNT(*) FROM drug_batches UNION SELECT 'stock_transactions', COUNT(*) FROM stock_transactions UNION SELECT 'audit_logs', COUNT(*) FROM audit_logs UNION SELECT 'users', COUNT(*) FROM AspNetUsers;`""

Write-Host "`n测试账号: admin / Admin@123 | operator1 / Operator@123 | viewer1 / Viewer@123" -ForegroundColor Green
Write-Host "Blazor: http://localhost:5100  API: http://localhost:5246" -ForegroundColor Cyan
