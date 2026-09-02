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

# Pinned to the `app` user mcr.microsoft.com/dotnet/aspnet:10.0 currently creates at this
# UID/GID, so it's explicit here rather than left implicit from the base image. The assertion
# below fails the build loudly if a future base image bump ever changes that, instead of
# docker-entrypoint.sh silently chown'ing /data to the wrong UID.
ARG APP_UID=1654
ENV APP_UID=$APP_UID
RUN test "$(id -u app)" = "$APP_UID" || { \
      echo "Base image's 'app' user is UID $(id -u app), not the pinned APP_UID=$APP_UID -- update the ARG in the Dockerfile." >&2; \
      exit 1; \
    }

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
