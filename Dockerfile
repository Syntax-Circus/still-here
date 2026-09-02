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
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /data \
    && chown "$APP_UID":"$APP_UID" /data
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    ConnectionStrings__Default="Data Source=/data/stillhere.db" \
    Logging__FilePath=/data/logs/log-.txt \
    DataProtection__KeysPath=/data/dataprotection-keys
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 CMD curl --fail --silent http://localhost:8080/healthz || exit 1
USER $APP_UID
ENTRYPOINT ["dotnet", "StillHere.Web.dll"]
