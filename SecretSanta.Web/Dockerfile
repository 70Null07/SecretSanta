FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
ARG VERSION=dev
ARG REVISION=unknown
LABEL org.opencontainers.image.source="https://github.com/70Null07/SecretSanta" \
      org.opencontainers.image.version=$VERSION \
      org.opencontainers.image.revision=$REVISION
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["SecretSanta.Web/SecretSanta.Web.csproj", "SecretSanta.Web/"]
COPY ["SecretSanta.ServiceDefaults/SecretSanta.ServiceDefaults.csproj", "SecretSanta.ServiceDefaults/"]
RUN dotnet restore "SecretSanta.Web/SecretSanta.Web.csproj"
COPY . .
RUN dotnet publish "SecretSanta.Web/SecretSanta.Web.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM runtime AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/keys && chown "$APP_UID" /app/keys
USER $APP_UID
HEALTHCHECK --interval=10s --timeout=5s --start-period=15s --retries=10 \
    CMD curl --fail --silent http://127.0.0.1:8080/health || exit 1
ENTRYPOINT ["dotnet", "SecretSanta.Web.dll"]
