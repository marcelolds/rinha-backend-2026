$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$input = Join-Path $root "resources/references.json.gz"
$output = Join-Path $root "data/references.bin"

if (!(Test-Path $input)) {
    throw "references.json.gz not found. Run scripts/download-resources.ps1 first."
}

$dotnet = "dotnet"
if (!(Get-Command $dotnet -ErrorAction SilentlyContinue)) {
    $dotnet = "C:\Program Files\dotnet\dotnet.exe"
}

& $dotnet run -c Release --project (Join-Path $root "src/Rinha.Indexer/Rinha.Indexer.csproj") -- $input $output
