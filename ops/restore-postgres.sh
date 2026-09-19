#!/usr/bin/env bash
set -Eeuo pipefail

if [[ $# -ne 1 ]]; then
  echo "Usage: $0 /path/to/secretsanta-TIMESTAMP.dump" >&2
  exit 64
fi

backup="$(realpath "$1")"
test -s "$backup"
if [[ -f "$backup.sha256" ]]; then
  (cd "$(dirname "$backup")" && sha256sum --check "$(basename "$backup").sha256")
fi

repo_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_dir"

if [[ "${CONFIRM_RESTORE:-}" != "YES" ]]; then
  echo "Refusing destructive restore. Set CONFIRM_RESTORE=YES." >&2
  exit 65
fi

compose=(docker compose -f compose.yaml -f compose.production.yaml)
database="${POSTGRES_DB:-secretsanta}"
database_user="${POSTGRES_USER:-secretsanta}"

running_services="$("${compose[@]}" ps --services --filter status=running)"
services_to_restart=()
for service in api web; do
  if grep -qx "$service" <<< "$running_services"; then
    services_to_restart+=("$service")
  fi
done

echo "Stopping application traffic for the duration of the restore..."
"${compose[@]}" stop api web

restore_succeeded=false
restore_cleanup() {
  if [[ "$restore_succeeded" != true ]]; then
    echo "Restore failed; api and web have been left stopped." >&2
  fi
}
trap restore_cleanup EXIT

# Restoring into a newly created database prevents objects introduced after the
# backup from surviving a rollback to an older schema.
"${compose[@]}" exec -T postgres \
  dropdb -U "$database_user" --force --if-exists "$database"
"${compose[@]}" exec -T postgres \
  createdb -U "$database_user" --owner "$database_user" "$database"
"${compose[@]}" exec -T postgres \
  pg_restore -U "$database_user" -d "$database" --exit-on-error --no-owner < "$backup"

"${compose[@]}" exec -T postgres \
  psql -U "$database_user" -d "$database" -v ON_ERROR_STOP=1 \
  -c 'SELECT 1' >/dev/null

restore_succeeded=true
trap - EXIT

if (( ${#services_to_restart[@]} > 0 )); then
  echo "Restore validated; restarting services that were previously running..."
  "${compose[@]}" start "${services_to_restart[@]}"
fi

echo "Restore completed and database connectivity validated; run application smoke tests."
