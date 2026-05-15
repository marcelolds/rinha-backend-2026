# Rinha Backend 2026

Implementação em `.NET 10` para a Rinha de Backend 2026.

## Stack

- ASP.NET Core Minimal API
- nginx na porta `9999`
- duas instâncias da API
- índice binário gerado no build a partir de `resources/references.json.gz`
- vetores quantizados em 1 byte por dimensão

## Requisitos

Baixe os arquivos oficiais do repositório da Rinha para `resources/`:

- `resources/references.json.gz`
- `resources/mcc_risk.json`
- `resources/normalization.json`

No Windows:

```powershell
.\scripts\download-resources.ps1
```

## Rodar Localmente

```powershell
$env:DOCKER_CONFIG="$PWD\.docker-local"
docker compose up --build
```

Health check:

```powershell
curl http://localhost:9999/ready
```

Teste manual:

```powershell
curl -X POST http://localhost:9999/fraud-score `
  -H "Content-Type: application/json" `
  -d '{"id":"tx-1","transaction":{"amount":41.12,"installments":2,"requested_at":"2026-03-11T18:45:53Z"},"customer":{"avg_amount":82.24,"tx_count_24h":3,"known_merchants":["MERC-003","MERC-016"]},"merchant":{"id":"MERC-016","mcc":"5411","avg_amount":60.25},"terminal":{"is_online":false,"card_present":true,"km_from_home":29.23},"last_transaction":null}'
```

## Desenvolvimento

```powershell
& "C:\Program Files\dotnet\dotnet.exe" test
& "C:\Program Files\dotnet\dotnet.exe" run --project src\Rinha.Indexer -- resources\references.json.gz data\references.bin
& "C:\Program Files\dotnet\dotnet.exe" run --project src\Rinha.Api
```

## Validação

```powershell
.\scripts\build-index.ps1
.\scripts\smoke.ps1
.\scripts\run-k6.ps1
```
