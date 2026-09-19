FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["SecretSanta.ApiService/SecretSanta.ApiService.csproj", "SecretSanta.ApiService/"]
COPY ["SecretSanta.ServiceDefaults/SecretSanta.ServiceDefaults.csproj", "SecretSanta.ServiceDefaults/"]
COPY ["SecretSanta.ApiService/dotnet-tools.json", "SecretSanta.ApiService/"]
RUN dotnet restore "SecretSanta.ApiService/SecretSanta.ApiService.csproj"
COPY . .
RUN dotnet publish "SecretSanta.ApiService/SecretSanta.ApiService.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM build AS migrations
WORKDIR /src/SecretSanta.ApiService
RUN dotnet tool restore
ENTRYPOINT ["dotnet", "ef", "database", "update", "--no-build", "--configuration", "Release", "--project", "SecretSanta.ApiService.csproj"]

FROM runtime AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
HEALTHCHECK --interval=10s --timeout=5s --start-period=15s --retries=10 \
    CMD curl --fail --silent http://127.0.0.1:8080/health || exit 1
ENTRYPOINT ["dotnet", "SecretSanta.ApiService.dll"]
