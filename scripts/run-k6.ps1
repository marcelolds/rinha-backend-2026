$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$k6 = Join-Path $root "tools/k6/k6-v2.0.0-windows-amd64/k6.exe"
$testData = Join-Path $root "test/test-data.json"
$results = Join-Path $root "test/results.json"

if (!(Test-Path $k6)) {
    throw "k6 not found at $k6"
}

if (!(Test-Path $testData)) {
    throw "test/test-data.json not found. Generate it before running k6."
}

Push-Location $root
try {
    $env:K6_NO_USAGE_REPORT = "true"
    & $k6 run test/test.js
}
finally {
    Pop-Location
}

if (Test-Path $results) {
    Get-Content $results
}
