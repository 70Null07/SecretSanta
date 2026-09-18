FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["SecretSanta.ApiService/SecretSanta.ApiService.csproj", "SecretSanta.ApiService/"]
COPY ["SecretSanta.ServiceDefaults/SecretSanta.ServiceDefaults.csproj", "SecretSanta.ServiceDefaults/"]
RUN dotnet restore "SecretSanta.ApiService/SecretSanta.ApiService.csproj"
COPY . .
RUN dotnet publish "SecretSanta.ApiService/SecretSanta.ApiService.csproj" -c Release -o /app/publish --no-restore

FROM runtime AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SecretSanta.ApiService.dll"]
