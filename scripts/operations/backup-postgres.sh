#!/usr/bin/env bash
set -euo pipefail

: "${DATABASE_URL:?DATABASE_URL is required}"

backup_dir="${BACKUP_DIR:-./backups/manual}"
timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
mkdir -p "$backup_dir"
umask 077

dump_path="$backup_dir/bilitool-$timestamp.dump"
manifest_path="$dump_path.manifest"

if command -v pg_dump >/dev/null 2>&1; then
  pg_dump --dbname="$DATABASE_URL" --format=custom --compress=9 --no-owner --no-acl --file="$dump_path"
elif command -v docker >/dev/null 2>&1; then
  absolute_backup_dir="$(cd "$backup_dir" && pwd)"
  docker run --rm --network host -v "$absolute_backup_dir:/backup" postgres:16-alpine \
    pg_dump --dbname="$DATABASE_URL" --format=custom --compress=9 --no-owner --no-acl \
    --file="/backup/$(basename "$dump_path")"
else
  printf 'pg_dump or Docker is required.\n' >&2
  exit 127
fi
sha256sum "$dump_path" > "$dump_path.sha256"

cat > "$manifest_path" <<EOF
created_at_utc=$timestamp
format=postgres-custom
sha256=$(cut -d' ' -f1 "$dump_path.sha256")
size_bytes=$(stat -c%s "$dump_path")
EOF

printf 'Backup created: %s\n' "$dump_path"
