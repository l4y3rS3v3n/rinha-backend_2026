# syntax=docker/dockerfile:1.7

FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
WORKDIR /src

RUN apt-get update \
 && apt-get install -y --no-install-recommends clang zlib1g-dev \
 && rm -rf /var/lib/apt/lists/*

COPY src/Api/Api.csproj src/Api/
RUN dotnet restore src/Api/Api.csproj -r linux-x64

COPY src/Api/ src/Api/
RUN dotnet publish src/Api/Api.csproj \
    -c Release \
    -r linux-x64 \
    --no-restore \
    -o /publish

FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/runtime-deps:10.0-noble-chiseled-extra AS runtime
WORKDIR /app

COPY --from=build /publish/rinha-fraud-api ./rinha-fraud-api
COPY resources/ ./resources/

ENV RINHA_RESOURCES_DIR=/app/resources \
    DOTNET_TieredCompilation=0 \
    DOTNET_ReadyToRun=0 \
    DOTNET_gcServer=0 \
    DOTNET_gcConcurrent=1

ENTRYPOINT ["./rinha-fraud-api"]
