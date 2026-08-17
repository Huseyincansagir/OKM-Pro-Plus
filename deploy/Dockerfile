# syntax=docker/dockerfile:1.7

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY FactoryErp.sln ./
COPY src/FactoryErp.Domain/FactoryErp.Domain.csproj src/FactoryErp.Domain/
COPY src/FactoryErp.Application/FactoryErp.Application.csproj src/FactoryErp.Application/
COPY src/FactoryErp.Infrastructure/FactoryErp.Infrastructure.csproj src/FactoryErp.Infrastructure/
COPY src/FactoryErp.Api/FactoryErp.Api.csproj src/FactoryErp.Api/
COPY src/FactoryErp.Migrator/FactoryErp.Migrator.csproj src/FactoryErp.Migrator/
COPY tests/FactoryErp.Domain.UnitTests/FactoryErp.Domain.UnitTests.csproj tests/FactoryErp.Domain.UnitTests/
COPY tests/FactoryErp.ArchitectureTests/FactoryErp.ArchitectureTests.csproj tests/FactoryErp.ArchitectureTests/
COPY tests/FactoryErp.Infrastructure.UnitTests/FactoryErp.Infrastructure.UnitTests.csproj tests/FactoryErp.Infrastructure.UnitTests/

RUN dotnet restore FactoryErp.sln

COPY src/ src/

RUN dotnet publish src/FactoryErp.Api/FactoryErp.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /out/api \
    /p:UseAppHost=false

RUN dotnet publish src/FactoryErp.Migrator/FactoryErp.Migrator.csproj \
    --configuration Release \
    --no-restore \
    --output /out/migrator \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim AS runtime-base

RUN apt-get update \
    && apt-get install --no-install-recommends --yes curl ca-certificates \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER app

FROM runtime-base AS api
COPY --from=build /out/api ./
HEALTHCHECK --interval=10s --timeout=3s --start-period=10s --retries=12 \
    CMD curl --fail --silent http://127.0.0.1:8080/health/live || exit 1
ENTRYPOINT ["dotnet", "FactoryErp.Api.dll"]

FROM runtime-base AS migrator
COPY --from=build /out/migrator ./
ENTRYPOINT ["dotnet", "FactoryErp.Migrator.dll"]
