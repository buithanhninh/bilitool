#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
compose_file="$repo_root/sandbox/docker-compose.yml"
evidence_dir="${REHEARSAL_EVIDENCE_DIR:-$repo_root/artifacts/production-rehearsal}"
export SANDBOX_DB_PASSWORD="${SANDBOX_DB_PASSWORD:-rehearsal-synthetic-db-password}"
export SANDBOX_API_KEY="${SANDBOX_API_KEY:-rehearsal-synthetic-api-key-with-more-than-32-chars}"
export SANDBOX_EMERGENCY_KILL_SWITCH=false
export LOAD_PROFILE=smoke
export LOAD_EVIDENCE_DIR="$evidence_dir"
base_url="${BASE_URL:-http://127.0.0.1:18080}"
fixture="$repo_root/sandbox/fixtures/rest-v3.json"
started_at="$(date -u +%Y-%m-%dT%H:%M:%SZ)"

cleanup() {
  docker compose -f "$compose_file" logs --no-color > "$evidence_dir/docker.log" 2>&1 || true
  docker compose -f "$compose_file" down --volumes --remove-orphans >/dev/null 2>&1 || true
}
trap cleanup EXIT

wait_ready() {
  for attempt in $(seq 1 60); do
    if curl --fail --silent "$base_url/health/ready" >/dev/null; then return; fi
    sleep 2
  done
  return 1
}

calculate() {
  local key="$1"
  curl --silent --show-error --output "$evidence_dir/response.json" --write-out '%{http_code}' \
    --header "X-API-Key: $SANDBOX_API_KEY" \
    --header "Idempotency-Key: $key" \
    --header 'Content-Type: application/json' \
    --data-binary "@$fixture" \
    "$base_url/api/v3/clinical/bilirubin/calculate"
}

rm -rf "$evidence_dir"
mkdir -p "$evidence_dir"
docker compose -f "$compose_file" down --volumes --remove-orphans >/dev/null 2>&1 || true
docker compose -f "$compose_file" up --build --detach
wait_ready

"$repo_root/scripts/sandbox/run-conformance.sh" | tee "$evidence_dir/conformance.log"
"$repo_root/scripts/sandbox/run-generated-client.sh" | tee "$evidence_dir/generated-client.log"
"$repo_root/scripts/reliability/run-load-soak.sh" | tee "$evidence_dir/load-smoke.log"
before_rows="$(docker compose -f "$compose_file" exec -T sandbox-db psql -U bilitool_sandbox -d bilitool_sandbox -Atc 'SELECT count(*) FROM his_idempotency_records')"

export SANDBOX_EMERGENCY_KILL_SWITCH=true
docker compose -f "$compose_file" up --detach --no-deps --force-recreate sandbox-api >/dev/null
wait_ready
blocked_status="$(calculate rehearsal-kill-switch-001)"
test "$blocked_status" = 503
jq -e '.errorCode == "tenant_rollout_disabled" and .retryable == true' "$evidence_dir/response.json" >/dev/null

export SANDBOX_EMERGENCY_KILL_SWITCH=false
docker compose -f "$compose_file" up --detach --no-deps --force-recreate sandbox-api >/dev/null
wait_ready
restored_status="$(calculate rehearsal-rollback-001)"
test "$restored_status" = 200
jq -e '.resultId != null' "$evidence_dir/response.json" >/dev/null
after_rows="$(docker compose -f "$compose_file" exec -T sandbox-db psql -U bilitool_sandbox -d bilitool_sandbox -Atc 'SELECT count(*) FROM his_idempotency_records')"
(( after_rows >= before_rows ))

jq -n \
  --arg startedAt "$started_at" \
  --arg completedAt "$(date -u +%Y-%m-%dT%H:%M:%SZ)" \
  --argjson blockedStatus "$blocked_status" \
  --argjson restoredStatus "$restored_status" \
  --argjson rowsBefore "$before_rows" \
  --argjson rowsAfter "$after_rows" \
  '{startedAt:$startedAt,completedAt:$completedAt,cleanDeploy:true,conformance:true,generatedClient:true,loadSmoke:true,killSwitchStatus:$blockedStatus,rollbackStatus:$restoredStatus,idempotencyRowsBefore:$rowsBefore,idempotencyRowsAfter:$rowsAfter,dataContinuity:($rowsAfter >= $rowsBefore),status:"PASS"}' \
  > "$evidence_dir/rehearsal-summary.json"
sha256sum "$evidence_dir"/* > "$evidence_dir/SHA256SUMS"
printf 'Production rehearsal PASS. Evidence: %s\n' "$evidence_dir"
