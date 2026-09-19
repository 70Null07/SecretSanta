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
- `web`: Blazor Server frontend and the only published application endpoint. Its
  Data Protection key ring is stored in the `web_dataprotection` volume so that
  antiforgery tokens and other protected data survive container replacement.

Useful commands:

```bash
docker compose ps
docker compose logs -f migrate api web
docker compose down
```

`docker compose down` keeps database data and Data Protection keys. `docker
compose down -v` permanently deletes both named volumes; browsers with cookies
issued before the key volume was deleted may need to reload once to receive a
new antiforgery token.

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

Use the restore script to stop application traffic, recreate the target database,
restore the dump, validate connectivity, and restart only the application services
that were running before the restore:

```bash
CONFIRM_RESTORE=YES ./ops/restore-postgres.sh secretsanta.dump
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

CI additionally starts the complete Compose stack against a fresh PostgreSQL
volume and requires the Web/API health dependency chain to become ready.

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

## Single-server production deployment

Production uses pre-built immutable images from GHCR and file-backed Docker
secrets. Create a directory outside the repository and use the filenames from
`deploy/secrets.example`. File-backed Compose secrets retain their host numeric
ownership and permissions, so each secret must be readable by the UID of the
container that consumes it. Keep the directory root-owned and non-listable, and
make the individual files `0400` and owned by the relevant container UID.

```bash
sudo install -d -m 0711 /opt/secretsanta/secrets
sudo install -d -m 0700 /opt/secretsanta/backups
sudo cp deploy/secrets.example/*.example /opt/secretsanta/secrets/
# Rename the three files and replace every placeholder. Then obtain the UIDs
# from the exact images being deployed and assign each secret to its consumer:
API_UID="$(docker run --rm --entrypoint id ghcr.io/70null07/secretsanta-api:$APP_VERSION -u)"
POSTGRES_UID="$(docker run --rm --entrypoint id postgres:18 -u postgres)"
sudo chown "$API_UID" /opt/secretsanta/secrets/ConnectionStrings__Default \
  /opt/secretsanta/secrets/Jwt__Key
sudo chown "$POSTGRES_UID" /opt/secretsanta/secrets/postgres_password
sudo chmod 0400 /opt/secretsanta/secrets/*
```

Configure `.env` with `SECRETS_DIR=/opt/secretsanta/secrets`, a release
`APP_VERSION` (a `v*` tag or `sha-*` tag published by CI), and the non-secret
PostgreSQL/JWT settings. Deploy in this order:

```bash
docker compose -f compose.yaml -f compose.production.yaml pull
docker compose -f compose.yaml -f compose.production.yaml up -d postgres
docker compose -f compose.yaml -f compose.production.yaml run --rm migrate
docker compose -f compose.yaml -f compose.production.yaml up -d api web
docker compose -f compose.yaml -f compose.production.yaml ps
```

Only Web is published on the host. API and PostgreSQL use the internal backend
network. The base Compose configuration uses a persistent Data Protection key
volume; the production overlay adds read-only application filesystems, dropped
Linux capabilities, bounded Docker logs, and graceful shutdown periods. Until a
domain is available, expose Web only on a trusted network or behind a VPN. Do
not send credentials over public plain HTTP. Add an HTTPS reverse proxy and
forwarded-header allow-list before public Internet exposure.

### Secrets

The API reads `/run/secrets` with the ASP.NET Core key-per-file provider. A
double underscore in a filename represents a configuration section delimiter.
The PostgreSQL image reads its password through `POSTGRES_PASSWORD_FILE`.
Secret files survive application container recreation because they live on the
host, but they must also be backed up separately in encrypted form. Never copy
them into an image or commit them to Git.

### Six-hour RPO backup policy

`ops/backup-postgres.sh` creates a custom-format dump, verifies that PostgreSQL
can read its catalog, writes a SHA-256 checksum, and retains 14 days by default.
Install `ops/secretsanta-backup.cron` on the Docker host to run it every six
hours. Copy each completed dump and checksum to encrypted storage outside the
Docker host; a local dump alone does not satisfy disaster recovery requirements.

The supplied schedule is a root crontab because it needs access to the
root-owned backup directory, `/var/log`, and the Docker daemon. Install it with
`sudo crontab ops/secretsanta-backup.cron`; do not install it as the ordinary
deployment user's crontab.

```bash
BACKUP_DIR=/opt/secretsanta/backups ./ops/backup-postgres.sh
```

Test restoration at least monthly into an isolated environment:

```bash
CONFIRM_RESTORE=YES ./ops/restore-postgres.sh \
  /opt/secretsanta/backups/secretsanta-YYYYMMDDTHHMMSSZ.dump
```

Restoration is destructive for the selected database. The script stops API and
Web traffic before dropping and recreating the database and validates the restored
database. It always leaves application traffic stopped. After it completes, check
migration history, select and deploy an application image compatible with the
restored schema, and only then start API and Web and execute the registration,
login, game, invitation, gift, and draw smoke scenarios. The
recovery target is RTO 24 hours and RPO 6 hours. Record every restore drill,
including duration and the newest restored transaction time.

### Upgrade and rollback

Before every schema change, create and copy an off-host backup. Pull a specific
immutable release, run exactly one migration job, then update API and Web and
wait for readiness. Roll back only application images when the previous release
is compatible with the migrated schema. Use additive expand/contract migrations
so the previous application remains usable during the rollback window. For a
destructive incompatible schema change, prefer a forward fix or a rehearsed
database restore, explicitly accounting for data written after the backup.
