#!/usr/bin/env bash
set -euo pipefail

: "${RESTORE_DATABASE_URL:?RESTORE_DATABASE_URL is required}"

dump_path="${1:?Usage: restore-postgres.sh <backup.dump>}"
checksum_path="$dump_path.sha256"

test -f "$dump_path"
test -f "$checksum_path"
sha256sum --check "$checksum_path"

if [[ "${CONFIRM_RESTORE:-}" != "RESTORE" ]]; then
  printf 'Refusing destructive restore. Set CONFIRM_RESTORE=RESTORE.\n' >&2
  exit 2
fi

started_at="$(date +%s)"
if command -v pg_restore >/dev/null 2>&1 && command -v psql >/dev/null 2>&1; then
  pg_restore --dbname="$RESTORE_DATABASE_URL" --clean --if-exists --no-owner --no-acl --exit-on-error "$dump_path"
  psql "$RESTORE_DATABASE_URL" --set ON_ERROR_STOP=1 --command='SELECT 1;'
elif command -v docker >/dev/null 2>&1; then
  absolute_dump_dir="$(cd "$(dirname "$dump_path")" && pwd)"
  dump_name="$(basename "$dump_path")"
  docker run --rm --network host -v "$absolute_dump_dir:/backup:ro" postgres:16-alpine \
    pg_restore --dbname="$RESTORE_DATABASE_URL" --clean --if-exists --no-owner --no-acl --exit-on-error "/backup/$dump_name"
  docker run --rm --network host postgres:16-alpine \
    psql "$RESTORE_DATABASE_URL" --set ON_ERROR_STOP=1 --command='SELECT 1;'
else
  printf 'pg_restore/psql or Docker is required.\n' >&2
  exit 127
fi
elapsed_seconds="$(( $(date +%s) - started_at ))"

printf 'Restore verified in %s seconds.\n' "$elapsed_seconds"
