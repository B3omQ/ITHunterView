# ─── Stage 1: Build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution & project files first (for layer caching)
COPY ITHunterview_V1.slnx ./
COPY ITHunterview.Domain/ITHunterview.Domain.csproj             ITHunterview.Domain/
COPY ITHunterview.Service/ITHunterview.Service.csproj           ITHunterview.Service/
COPY ITHunterview.WebAPI/ITHunterview.WebAPI.csproj             ITHunterview.WebAPI/

RUN dotnet restore ITHunterview.WebAPI/ITHunterview.WebAPI.csproj

# Copy everything else and publish
COPY . .
RUN dotnet publish ITHunterview.WebAPI/ITHunterview.WebAPI.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ─── Stage 2: Runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install curl for healthcheck
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false

HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "ITHunterview.WebAPI.dll"]
