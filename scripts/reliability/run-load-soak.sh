#!/usr/bin/env bash
set -euo pipefail

readonly K6_IMAGE="grafana/k6:2.0.0"
readonly ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
: "${LOAD_PROFILE:=smoke}"
: "${BASE_URL:=http://127.0.0.1:18080}"
: "${SANDBOX_API_KEY:?SANDBOX_API_KEY is required}"
: "${LOAD_EVIDENCE_DIR:=${ROOT_DIR}/artifacts/reliability}"

case "${LOAD_PROFILE}" in
  smoke|load|soak) ;;
  *) echo "LOAD_PROFILE phải là smoke, load hoặc soak." >&2; exit 2 ;;
esac

mkdir -p "${LOAD_EVIDENCE_DIR}"
docker run --rm --network host \
  --user "$(id -u):$(id -g)" \
  -e LOAD_PROFILE \
  -e BASE_URL \
  -e SANDBOX_API_KEY \
  -v "${ROOT_DIR}:/workspace:ro" \
  -v "${LOAD_EVIDENCE_DIR}:/evidence" \
  "${K6_IMAGE}" run \
  --summary-export "/evidence/k6-${LOAD_PROFILE}-summary.json" \
  "/workspace/scripts/reliability/his-load.js"

echo "HIS reliability ${LOAD_PROFILE} PASS"
