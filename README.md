# Rinha Backend 2026 - .NET

Implementacao em .NET 10 para a Rinha de Backend 2026.

## Arquitetura

- nginx na porta `9999`
- duas instancias da API em ASP.NET Core Minimal API
- indice binario gerado no build da imagem a partir de `resources/references.json.gz`
- vetores de referencia quantizados em 1 byte por dimensao
- busca inicial restrita por buckets derivados de dimensoes discretas/baratas

## Preparacao

Baixe os arquivos oficiais do repositorio da Rinha para `resources/`:

- `resources/references.json.gz`
- `resources/mcc_risk.json`
- `resources/normalization.json`

O `mcc_risk` e as constantes de normalizacao ja estao embutidos no codigo para reduzir I/O no runtime.

No Windows, use:

```powershell
.\scripts\download-resources.ps1
```

## Rodar

```bash
docker compose up --build
```

Se o Docker no Windows reclamar de `docker-credential-desktop`, use uma config local sem `credsStore`:

```powershell
$env:DOCKER_CONFIG="$PWD\.docker-local"
docker compose up --build
```

Health check:

```bash
curl http://localhost:9999/ready
```

Endpoint:

```bash
curl -X POST http://localhost:9999/fraud-score \
  -H 'Content-Type: application/json' \
  -d '{"id":"tx-1","transaction":{"amount":41.12,"installments":2,"requested_at":"2026-03-11T18:45:53Z"},"customer":{"avg_amount":82.24,"tx_count_24h":3,"known_merchants":["MERC-003","MERC-016"]},"merchant":{"id":"MERC-016","mcc":"5411","avg_amount":60.25},"terminal":{"is_online":false,"card_present":true,"km_from_home":29.23},"last_transaction":null}'
```

## Desenvolvimento

Quando o SDK .NET estiver instalado:

```bash
dotnet test
dotnet run --project src/Rinha.Indexer -- resources/references.json.gz data/references.bin
dotnet run --project src/Rinha.Api
```

Ou:

```powershell
.\scripts\build-index.ps1
```
