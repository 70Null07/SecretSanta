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

docker compose -f compose.yaml -f compose.production.yaml exec -T postgres \
  pg_restore -U "${POSTGRES_USER:-secretsanta}" -d "${POSTGRES_DB:-secretsanta}" \
  --clean --if-exists --no-owner < "$backup"

echo "Restore completed; run application smoke tests before opening traffic."
