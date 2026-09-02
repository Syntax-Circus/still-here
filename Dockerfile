FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_VERSION=0.0.0
ARG BUILD_INFORMATIONAL_VERSION=0.0.0-local
WORKDIR /src
COPY Directory.Build.props Directory.Packages.props global.json GitVersion.yml ./
COPY src/ ./src/
RUN dotnet restore src/StillHere.Web/StillHere.Web.csproj
RUN dotnet publish src/StillHere.Web/StillHere.Web.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    -p:Version="$BUILD_VERSION" \
    -p:InformationalVersion="$BUILD_INFORMATIONAL_VERSION" \
    -p:DisableGitVersionTask=true

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl gosu \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /data
COPY --from=build /app/publish .
COPY docker-entrypoint.sh /usr/local/bin/
RUN chmod +x /usr/local/bin/docker-entrypoint.sh
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    ConnectionStrings__Default="Data Source=/data/stillhere.db" \
    Logging__FilePath=/data/logs/log-.txt \
    DataProtection__KeysPath=/data/dataprotection-keys
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 CMD curl --fail --silent http://localhost:8080/healthz || exit 1
ENTRYPOINT ["docker-entrypoint.sh"]
CMD ["dotnet", "StillHere.Web.dll"]
