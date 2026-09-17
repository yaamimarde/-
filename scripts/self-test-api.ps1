$ErrorActionPreference = "Stop"
$base = "http://localhost:5246"
$results = @()

function Test-Login($user, $pass) {
    $body = @{ username = $user; password = $pass } | ConvertTo-Json
    $r = Invoke-RestMethod -Method Post -Uri "$base/api/auth/login" -ContentType "application/json" -Body $body
    if (-not $r.success) { throw "Login failed for $user" }
    return $r.data.token
}

function Test-Endpoint($name, $token, $path, $expectSuccess = $true) {
    try {
        $headers = @{ Authorization = "Bearer $token" }
        $r = Invoke-RestMethod -Uri "$base/$path" -Headers $headers
        $ok = $r.success -eq $true
        if ($expectSuccess -and -not $ok) { return @{ Name = $name; Ok = $false; Detail = "success=false" } }
        if (-not $expectSuccess -and $ok) { return @{ Name = $name; Ok = $false; Detail = "expected fail but success" } }
        return @{ Name = $name; Ok = $true; Detail = "OK" }
    }
    catch {
        if ($expectSuccess) { return @{ Name = $name; Ok = $false; Detail = $_.Exception.Message } }
        return @{ Name = $name; Ok = $true; Detail = "expected fail: $($_.Exception.Message)" }
    }
}

# Admin tests
$adminToken = Test-Login "admin" "Admin@123"
$results += Test-Endpoint "admin/dashboard" $adminToken "api/dashboard"
$results += Test-Endpoint "admin/users" $adminToken "api/users"
$results += Test-Endpoint "admin/supplier" $adminToken "api/supplier"
$results += Test-Endpoint "admin/drug" $adminToken "api/drug?page=1&pageSize=5"
$results += Test-Endpoint "admin/audit" $adminToken "api/audit?count=5"
$results += Test-Endpoint "admin/purchaseorder" $adminToken "api/purchaseorder"
$results += Test-Endpoint "admin/auth/me" $adminToken "api/auth/me"

# Operator tests
$opToken = Test-Login "operator1" "Operator@123"
$results += Test-Endpoint "operator/dashboard" $opToken "api/dashboard"
$results += Test-Endpoint "operator/users-deny" $opToken "api/users" $false
$results += Test-Endpoint "operator/supplier" $opToken "api/supplier"

# Viewer tests
$viewToken = Test-Login "viewer1" "Viewer@123"
$results += Test-Endpoint "viewer/dashboard" $viewToken "api/dashboard"
$results += Test-Endpoint "viewer/drug" $viewToken "api/drug?page=1&pageSize=5"

# Blazor health
try {
    $blazor = Invoke-WebRequest -Uri "http://localhost:5100/" -UseBasicParsing -TimeoutSec 10
    $results += @{ Name = "blazor-home"; Ok = ($blazor.StatusCode -eq 200); Detail = "status=$($blazor.StatusCode)" }
}
catch {
    $results += @{ Name = "blazor-home"; Ok = $false; Detail = $_.Exception.Message }
}

$failed = $results | Where-Object { -not $_.Ok }
Write-Output "=== API Self-Test Results ==="
$results | ForEach-Object { Write-Output ("{0}: {1} ({2})" -f $_.Name, $(if ($_.Ok) { "PASS" } else { "FAIL" }), $_.Detail) }
Write-Output "=== Summary: $($results.Count - $failed.Count)/$($results.Count) passed ==="
if ($failed) { exit 1 }
