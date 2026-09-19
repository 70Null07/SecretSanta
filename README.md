# SecretSanta

SecretSanta is a .NET 10 application composed of a Blazor Server frontend, an
ASP.NET Core API, and PostgreSQL. Docker Compose is the canonical way to run the
complete stack; .NET Aspire remains available as an optional local-development
orchestrator.

## Architecture

```text
Browser -> SecretSanta.Web -> SecretSanta.ApiService -> PostgreSQL
```

Compose also runs a one-shot `migrate` service before starting the API. Database
schema changes are therefore applied once, rather than by every API instance.

## Quick start

Prerequisites: Git and Docker with Compose v2.

```bash
git clone https://github.com/70Null07/SecretSanta.git
cd SecretSanta
cp .env.example .env
```

Set a database password and a cryptographically random JWT key of at least 32
bytes in `.env`, then start the stack:

```bash
docker compose up --build
```

Open <http://localhost:8080>. Change `WEB_PORT` in `.env` if that port is in use.
PostgreSQL and the API are available only on the internal Compose network.

## Services

- `postgres`: PostgreSQL 18 with a readiness check and the `postgres_data` volume.
- `migrate`: applies committed EF Core migrations and exits.
- `api`: ASP.NET Core API; waits for successful migrations.
- `web`: Blazor Server frontend and the only published application endpoint.

Useful commands:

```bash
docker compose ps
docker compose logs -f migrate api web
docker compose down
```

`docker compose down` keeps database data. `docker compose down -v` permanently
deletes the named volume and all database data.

## Database migrations

Create a migration after changing `AppDbContext`:

```bash
dotnet tool restore --tool-manifest SecretSanta.ApiService/dotnet-tools.json
dotnet ef migrations add <Name> \
  --project SecretSanta.ApiService/SecretSanta.ApiService.csproj \
  --startup-project SecretSanta.ApiService/SecretSanta.ApiService.csproj
```

Review generated SQL before deployment:

```bash
dotnet ef migrations script --idempotent \
  --project SecretSanta.ApiService/SecretSanta.ApiService.csproj \
  --startup-project SecretSanta.ApiService/SecretSanta.ApiService.csproj
```

Production deployments must run one migration job before rolling out the API.
Do not enable automatic `Database.Migrate()` calls in production API replicas.

## Backup and restore

Create a custom-format backup outside the container:

```bash
docker compose exec -T postgres pg_dump \
  -U "$POSTGRES_USER" -d "$POSTGRES_DB" -Fc > secretsanta.dump
```

Restore into a prepared database:

```bash
docker compose exec -T postgres pg_restore \
  -U "$POSTGRES_USER" -d "$POSTGRES_DB" --clean --if-exists < secretsanta.dump
```

Store backups away from the Docker host and regularly test restoration into a
separate database.

## Aspire development

Developers with the .NET 10 SDK, Docker, and the Aspire workload/tooling can run:

```bash
dotnet run --project SecretSanta.AppHost/SecretSanta.AppHost.csproj
```

The API applies committed migrations automatically only in `Development`, where
Aspire runs a single instance. Production continues to use the dedicated migration
job.

Configure `Jwt:Key` through API user secrets; never commit signing keys:

```bash
dotnet user-secrets set "Jwt:Key" "<at-least-32-byte-random-key>" \
  --project SecretSanta.ApiService/SecretSanta.ApiService.csproj
```

## Verification

```bash
dotnet restore SecretSanta.slnx
dotnet build SecretSanta.slnx --configuration Release --no-restore
dotnet test SecretSanta.slnx --configuration Release --no-build
docker compose config --quiet
docker compose build
```

Health endpoints are available at `/alive` for process liveness and `/health`
for readiness. API readiness includes its PostgreSQL connection.

## Production notes

- Supply passwords and the JWT key from the deployment platform's secret store.
- Use immutable image tags and run exactly one migration job per deployment.
- Do not expose PostgreSQL publicly.
- Terminate HTTPS at the hosting platform or a deliberately configured reverse
  proxy, and trust forwarded headers only from that proxy.
- Configure resource limits, off-host backups, log collection, and a graceful
  shutdown window appropriate to the target platform.
