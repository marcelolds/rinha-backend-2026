FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build

WORKDIR /src
COPY . .

RUN dotnet publish src/Rinha.Indexer/Rinha.Indexer.csproj -c Release -o /out/indexer
RUN dotnet publish src/Rinha.Api/Rinha.Api.csproj -c Release -o /out/api /p:UseAppHost=false

RUN mkdir -p /out/api/data \
    && if [ -f resources/references.json.gz ]; then \
        dotnet /out/indexer/Rinha.Indexer.dll resources/references.json.gz /out/api/data/references.bin; \
    else \
        echo "resources/references.json.gz not found; image will start without an index"; \
    fi

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine

WORKDIR /app
COPY --from=build /out/api ./

ENV ASPNETCORE_URLS=http://+:8080
ENV INDEX_PATH=/app/data/references.bin
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true

EXPOSE 8080
ENTRYPOINT ["dotnet", "Rinha.Api.dll"]
