#!/usr/bin/env bash
set -Eeuo pipefail

repo_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_dir"

: "${BACKUP_DIR:?Set BACKUP_DIR to an off-repository backup directory}"
retention_days="${BACKUP_RETENTION_DAYS:-14}"
timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
mkdir -p "$BACKUP_DIR"
umask 077

output="$BACKUP_DIR/secretsanta-$timestamp.dump"
docker compose -f compose.yaml -f compose.production.yaml exec -T postgres \
  pg_dump -U "${POSTGRES_USER:-secretsanta}" -d "${POSTGRES_DB:-secretsanta}" \
  --format=custom --no-owner --file=- > "$output"

test -s "$output"
docker compose -f compose.yaml -f compose.production.yaml exec -T postgres \
  pg_restore --list < "$output" > /dev/null
(cd "$BACKUP_DIR" && sha256sum "$(basename "$output")" > "$(basename "$output").sha256")
find "$BACKUP_DIR" -type f \( -name 'secretsanta-*.dump' -o -name 'secretsanta-*.dump.sha256' \) \
  -mtime "+$retention_days" -delete

printf 'Backup created: %s\n' "$output"
