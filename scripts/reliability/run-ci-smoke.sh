#!/usr/bin/env bash
set -euo pipefail

readonly ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
export SANDBOX_DB_PASSWORD="${SANDBOX_DB_PASSWORD:-ci-synthetic-db-password}"
export SANDBOX_API_KEY="${SANDBOX_API_KEY:-ci-synthetic-api-key-with-more-than-32-chars}"
export LOAD_PROFILE=smoke

cleanup() {
  docker compose -f "${ROOT_DIR}/sandbox/docker-compose.yml" down --volumes --remove-orphans
}
trap cleanup EXIT

docker compose -f "${ROOT_DIR}/sandbox/docker-compose.yml" up --build --detach
for attempt in $(seq 1 60); do
  if curl --fail --silent http://127.0.0.1:18080/health/ready >/dev/null; then break; fi
  if [[ "${attempt}" == 60 ]]; then
    docker compose -f "${ROOT_DIR}/sandbox/docker-compose.yml" logs
    exit 1
  fi
  sleep 2
done

for attempt in $(seq 1 10); do
  if "${ROOT_DIR}/scripts/sandbox/run-conformance.sh"; then break; fi
  if [[ "${attempt}" == 10 ]]; then
    docker compose -f "${ROOT_DIR}/sandbox/docker-compose.yml" logs
    exit 1
  fi
  sleep 2
done
"${ROOT_DIR}/scripts/sandbox/run-generated-client.sh"
"${ROOT_DIR}/scripts/reliability/run-load-soak.sh"
