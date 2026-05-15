$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$resources = Join-Path $root "resources"
New-Item -ItemType Directory -Force -Path $resources | Out-Null

$base = "https://raw.githubusercontent.com/zanfranceschi/rinha-de-backend-2026/main/resources"

$files = @(
    "references.json.gz",
    "mcc_risk.json",
    "normalization.json",
    "example-payloads.json",
    "example-references.json"
)

foreach ($file in $files) {
    $target = Join-Path $resources $file
    if (Test-Path $target) {
        Write-Host "exists: $file"
        continue
    }

    Write-Host "downloading: $file"
    Invoke-WebRequest -Uri "$base/$file" -OutFile $target
}
