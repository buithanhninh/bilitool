#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
compose_file="$repo_root/sandbox/docker-compose.yml"
evidence_dir="${DAST_EVIDENCE_DIR:-$repo_root/artifacts/security-dast}"
zap_image="${ZAP_IMAGE:-ghcr.io/zaproxy/zaproxy:2.17.0}"
export SANDBOX_DB_PASSWORD="${SANDBOX_DB_PASSWORD:-dast-synthetic-db-password}"
export SANDBOX_API_KEY="${SANDBOX_API_KEY:-dast-synthetic-api-key-with-more-than-32-chars}"
export SANDBOX_EMERGENCY_KILL_SWITCH=false

cleanup() {
  docker compose -f "$compose_file" logs --no-color > "$evidence_dir/sandbox.log" 2>&1 || true
  docker compose -f "$compose_file" down --volumes --remove-orphans >/dev/null 2>&1 || true
}
trap cleanup EXIT

rm -rf "$evidence_dir"
mkdir -p "$evidence_dir"
chmod 0777 "$evidence_dir"
docker compose -f "$compose_file" down --volumes --remove-orphans >/dev/null 2>&1 || true
docker compose -f "$compose_file" up --build --detach
for attempt in $(seq 1 60); do
  if curl --fail --silent http://127.0.0.1:18080/health/ready >/dev/null; then break; fi
  if [[ "$attempt" == 60 ]]; then exit 1; fi
  sleep 2
done

docker run --rm --network sandbox_default \
  -e ZAP_AUTH_HEADER=X-API-Key \
  -e "ZAP_AUTH_HEADER_VALUE=$SANDBOX_API_KEY" \
  -e ZAP_AUTH_HEADER_SITE=sandbox-api \
  -v "$evidence_dir:/zap/wrk/:rw" \
  "$zap_image" zap-api-scan.py \
  -t http://sandbox-api:8080/openapi/v3.json \
  -f openapi -I -T 10 -J zap-report.json -r zap-report.html -w zap-report.md \
  -z '-config replacer.full_list(0).description=IdempotencyHeader -config replacer.full_list(0).enabled=true -config replacer.full_list(0).matchtype=REQ_HEADER -config replacer.full_list(0).matchstr=Idempotency-Key -config replacer.full_list(0).replacement=zap-security-001' \
  | tee "$evidence_dir/zap-console.log"

high_count="$(jq '[.site[].alerts[]? | select((.riskcode | tonumber) >= 3)] | length' "$evidence_dir/zap-report.json")"
medium_count="$(jq '[.site[].alerts[]? | select((.riskcode | tonumber) == 2)] | length' "$evidence_dir/zap-report.json")"
low_count="$(jq '[.site[].alerts[]? | select((.riskcode | tonumber) == 1)] | length' "$evidence_dir/zap-report.json")"
server_error_count="$(grep -c 'HTTP Server Error' "$evidence_dir/zap-report.md" || true)"
test "$high_count" = 0
test "$server_error_count" = 0
jq -n \
  --arg image "$zap_image" \
  --arg scannedAt "$(date -u +%Y-%m-%dT%H:%M:%SZ)" \
  --argjson highFindings "$high_count" \
  --argjson mediumFindings "$medium_count" \
  --argjson lowFindings "$low_count" \
  --argjson serverErrors "$server_error_count" \
  '{scanner:"OWASP ZAP API Scan",image:$image,scannedAt:$scannedAt,highFindings:$highFindings,mediumFindings:$mediumFindings,lowFindings:$lowFindings,httpServerErrorFindings:$serverErrors,status:"PASS"}' \
  > "$evidence_dir/dast-summary.json"
sha256sum "$evidence_dir"/* > "$evidence_dir/SHA256SUMS"
printf 'OWASP ZAP API DAST PASS. Evidence: %s\n' "$evidence_dir"
