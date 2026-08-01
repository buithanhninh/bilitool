#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
evidence_dir="${DR_EVIDENCE_DIR:-$repo_root/artifacts/dr}"
rto_seconds="${DR_RTO_SECONDS:-3600}"
rpo_seconds="${DR_RPO_SECONDS:-900}"
run_id="$(date -u +%Y%m%dT%H%M%SZ)-$$"
network="bilitool-dr-$run_id"
source_container="bilitool-dr-source-$run_id"
restore_container="bilitool-dr-restore-$run_id"
pitr_container="bilitool-dr-pitr-$run_id"
source_volume="bilitool-dr-source-$run_id"
restore_volume="bilitool-dr-restore-$run_id"
base_dir="$(mktemp -d /tmp/bilitool-pitr-base.XXXXXX)"
archive_dir="$(mktemp -d /tmp/bilitool-wal-archive.XXXXXX)"
backup_dir="$(mktemp -d /tmp/bilitool-dr-backup.XXXXXX)"
password="dr-$run_id"
latest_migration="20260801124937_AddHisIdempotencyResponseMetadata"
previous_migration="20260801112000_AddHisMutualTlsBinding"

cleanup() {
  docker rm -f "$source_container" "$restore_container" "$pitr_container" >/dev/null 2>&1 || true
  docker volume rm "$source_volume" "$restore_volume" >/dev/null 2>&1 || true
  docker network rm "$network" >/dev/null 2>&1 || true
  rm -rf "$base_dir" "$archive_dir" "$backup_dir"
}
trap cleanup EXIT

mkdir -p "$evidence_dir"
rm -f "$evidence_dir"/*
docker network create "$network" >/dev/null
docker volume create "$source_volume" >/dev/null
docker volume create "$restore_volume" >/dev/null
chmod 0777 "$base_dir" "$archive_dir"

docker run -d --name "$source_container" --network "$network" -p 127.0.0.1::5432 \
  -e POSTGRES_DB=bilitool_dr -e POSTGRES_USER=bilitool -e POSTGRES_PASSWORD="$password" \
  -v "$source_volume:/var/lib/postgresql/data" -v "$archive_dir:/archive" postgres:16-alpine \
  postgres -c wal_level=replica -c archive_mode=on \
  -c "archive_command=test ! -f /archive/%f && cp %p /archive/%f" -c archive_timeout=1 >/dev/null

until docker exec "$source_container" pg_isready -U bilitool -d bilitool_dr >/dev/null 2>&1; do sleep 1; done
dotnet tool restore >/dev/null
source_port="$(docker inspect -f '{{(index (index .NetworkSettings.Ports "5432/tcp") 0).HostPort}}' "$source_container")"
docker exec -u postgres "$source_container" sh -c \
  "printf '%s\n' 'host replication all 0.0.0.0/0 scram-sha-256' >> /var/lib/postgresql/data/pg_hba.conf"
docker exec "$source_container" psql -U bilitool -d bilitool_dr -v ON_ERROR_STOP=1 -c 'SELECT pg_reload_conf();' >/dev/null
connection="Host=127.0.0.1;Port=$source_port;Database=bilitool_dr;Username=bilitool;Password=$password"

dotnet tool run dotnet-ef database update --project "$repo_root/src/BiliTool.Vn.Infrastructure" \
  --startup-project "$repo_root/src/BiliTool.Vn.Web" --connection "$connection" >"$evidence_dir/migration-initial.log"
docker exec "$source_container" psql -U bilitool -d bilitool_dr -v ON_ERROR_STOP=1 -c \
  "CREATE TABLE dr_markers(id text PRIMARY KEY, created_at timestamptz NOT NULL DEFAULT clock_timestamp()); INSERT INTO dr_markers(id) VALUES ('base');" >/dev/null

DATABASE_URL="postgresql://bilitool:$password@127.0.0.1:$source_port/bilitool_dr" BACKUP_DIR="$backup_dir" \
  "$repo_root/scripts/operations/backup-postgres.sh" >"$evidence_dir/backup.log"
dump_path="$(find "$backup_dir" -name '*.dump' -print -quit)"
cp "$dump_path.manifest" "$dump_path.sha256" "$evidence_dir/"

docker run -d --name "$restore_container" --network "$network" -p 127.0.0.1::5432 \
  -e POSTGRES_DB=bilitool_restore -e POSTGRES_USER=bilitool -e POSTGRES_PASSWORD="$password" \
  -v "$restore_volume:/var/lib/postgresql/data" postgres:16-alpine >/dev/null
until docker exec "$restore_container" pg_isready -U bilitool -d bilitool_restore >/dev/null 2>&1; do sleep 1; done
restore_port="$(docker inspect -f '{{(index (index .NetworkSettings.Ports "5432/tcp") 0).HostPort}}' "$restore_container")"
restore_started="$(date +%s)"
RESTORE_DATABASE_URL="postgresql://bilitool:$password@127.0.0.1:$restore_port/bilitool_restore" CONFIRM_RESTORE=RESTORE \
  "$repo_root/scripts/operations/restore-postgres.sh" "$dump_path" >"$evidence_dir/restore.log"
restore_elapsed="$(( $(date +%s) - restore_started ))"
(( restore_elapsed <= rto_seconds ))

restore_connection="Host=127.0.0.1;Port=$restore_port;Database=bilitool_restore;Username=bilitool;Password=$password"
dotnet tool run dotnet-ef database update "$previous_migration" --project "$repo_root/src/BiliTool.Vn.Infrastructure" \
  --startup-project "$repo_root/src/BiliTool.Vn.Web" --connection "$restore_connection" >"$evidence_dir/migration-rollback.log"
dotnet tool run dotnet-ef database update "$latest_migration" --project "$repo_root/src/BiliTool.Vn.Infrastructure" \
  --startup-project "$repo_root/src/BiliTool.Vn.Web" --connection "$restore_connection" >"$evidence_dir/migration-forward.log"

source_checksum="$(docker exec "$source_container" psql -U bilitool -d bilitool_dr -Atc "SELECT md5(string_agg(id || '|' || created_at::text, ',' ORDER BY id)) FROM dr_markers")"
restore_checksum="$(docker exec "$restore_container" psql -U bilitool -d bilitool_restore -Atc "SELECT md5(string_agg(id || '|' || created_at::text, ',' ORDER BY id)) FROM dr_markers")"
[[ "$source_checksum" == "$restore_checksum" ]]

docker run --rm --network "$network" -e PGPASSWORD="$password" -v "$base_dir:/base" postgres:16-alpine \
  pg_basebackup -h "$source_container" -U bilitool -D /base -Fp -X none -c fast
docker exec "$source_container" psql -U bilitool -d bilitool_dr -v ON_ERROR_STOP=1 -c "INSERT INTO dr_markers(id) VALUES ('before-target');" >/dev/null
target_time="$(docker exec "$source_container" psql -U bilitool -d bilitool_dr -Atc "SELECT clock_timestamp()")"
sleep 2
docker exec "$source_container" psql -U bilitool -d bilitool_dr -v ON_ERROR_STOP=1 -c "INSERT INTO dr_markers(id) VALUES ('after-target'); SELECT pg_switch_wal();" >/dev/null
sleep 2
docker stop "$source_container" >/dev/null

cat >>"$base_dir/postgresql.auto.conf" <<EOF
restore_command = 'cp /archive/%f %p'
recovery_target_time = '$target_time'
recovery_target_action = 'promote'
EOF
touch "$base_dir/recovery.signal"
docker run -d --name "$pitr_container" --network "$network" \
  -v "$base_dir:/var/lib/postgresql/data" -v "$archive_dir:/archive:ro" postgres:16-alpine >/dev/null
until docker exec "$pitr_container" pg_isready -U bilitool -d bilitool_dr >/dev/null 2>&1; do sleep 1; done
pitr_before="$(docker exec "$pitr_container" psql -U bilitool -d bilitool_dr -Atc "SELECT count(*) FROM dr_markers WHERE id='before-target'")"
pitr_after="$(docker exec "$pitr_container" psql -U bilitool -d bilitool_dr -Atc "SELECT count(*) FROM dr_markers WHERE id='after-target'")"
[[ "$pitr_before" == "1" && "$pitr_after" == "0" ]]
pitr_recovery_time="$(docker exec "$pitr_container" psql -U bilitool -d bilitool_dr -Atc "SELECT max(created_at) FROM dr_markers")"
rpo_observed="$(date -u -d "$target_time" +%s)"
rpo_recovered="$(date -u -d "$pitr_recovery_time" +%s)"
rpo_gap="$(( rpo_observed - rpo_recovered ))"
(( rpo_gap >= 0 && rpo_gap <= rpo_seconds ))

cat >"$evidence_dir/dr-summary.json" <<EOF
{"runId":"$run_id","postgres":"16","restoreSeconds":$restore_elapsed,"rtoTargetSeconds":$rto_seconds,"rpoGapSeconds":$rpo_gap,"rpoTargetSeconds":$rpo_seconds,"customRestoreChecksumMatch":true,"migrationRollback":"$previous_migration","migrationForward":"$latest_migration","pitrBeforeTargetPresent":true,"pitrAfterTargetAbsent":true,"status":"PASS"}
EOF
sha256sum "$evidence_dir"/* >"$evidence_dir/SHA256SUMS"
printf 'PostgreSQL DR drill PASS. Evidence: %s\n' "$evidence_dir"
