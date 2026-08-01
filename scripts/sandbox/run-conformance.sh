#!/usr/bin/env bash
set -euo pipefail

: "${BASE_URL:=http://127.0.0.1:18080}"
: "${SANDBOX_API_KEY:?SANDBOX_API_KEY is required}"

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
fixture="$repo_root/sandbox/fixtures/rest-v3.json"
fhir_fixture="$repo_root/fhir/examples/bilirubin-request-bundle.json"
hl7_fixture="$repo_root/sandbox/fixtures/hl7-oru-r01.hl7"
work_dir="$(mktemp -d)"
trap 'rm -rf "$work_dir"' EXIT
tr '\n' '\r' < "$hl7_fixture" > "$work_dir/hl7-oru-r01.hl7"

request() {
  local key="$1"
  local body="$2"
  curl --silent --show-error --output "$work_dir/body" --dump-header "$work_dir/headers" \
    --write-out '%{http_code}' \
    --header "X-API-Key: $SANDBOX_API_KEY" \
    --header "Idempotency-Key: $key" \
    --header 'Content-Type: application/json' \
    --data-binary "@$body" \
    "$BASE_URL/api/v3/clinical/bilirubin/calculate"
}

status="$(request sandbox-rest-001 "$fixture")"
test "$status" = 200
grep -q '"resultId"' "$work_dir/body"
jq --sort-keys --compact-output . "$work_dir/body" > "$work_dir/first-body.canonical.json"

status="$(request sandbox-rest-001 "$fixture")"
test "$status" = 200
grep -qi '^Idempotency-Replayed: true' "$work_dir/headers"
jq --sort-keys --compact-output . "$work_dir/body" > "$work_dir/replayed-body.canonical.json"
cmp --silent "$work_dir/first-body.canonical.json" "$work_dir/replayed-body.canonical.json"

jq '.observation.value = 13' "$fixture" > "$work_dir/conflict.json"
status="$(request sandbox-rest-001 "$work_dir/conflict.json")"
test "$status" = 409
grep -q 'idempotency_payload_conflict' "$work_dir/body"

jq '.observation.unit = "invalid-unit"' "$fixture" > "$work_dir/invalid.json"
status="$(request sandbox-invalid-001 "$work_dir/invalid.json")"
test "$status" = 400
grep -q 'invalid_request' "$work_dir/body"

jq '.unexpectedField = true' "$fixture" > "$work_dir/unknown.json"
status="$(request sandbox-unknown-001 "$work_dir/unknown.json")"
test "$status" = 400

printf '{"source":' > "$work_dir/malformed.json"
status="$(request sandbox-malformed-001 "$work_dir/malformed.json")"
test "$status" = 400

head -c 70000 /dev/zero | tr '\0' 'x' > "$work_dir/oversized.txt"
status="$(curl --silent --output "$work_dir/body" --write-out '%{http_code}' \
  --header "X-API-Key: $SANDBOX_API_KEY" \
  --header 'Idempotency-Key: sandbox-oversized-001' \
  --header 'Content-Type: application/json' \
  --data-binary "@$work_dir/oversized.txt" \
  "$BASE_URL/api/v3/clinical/bilirubin/calculate")"
test "$status" = 413

status="$(curl --silent --output "$work_dir/body" --write-out '%{http_code}' \
  --header 'Idempotency-Key: sandbox-no-auth-001' \
  --header 'Content-Type: application/json' \
  --data-binary "@$fixture" \
  "$BASE_URL/api/v3/clinical/bilirubin/calculate")"
test "$status" = 401

status="$(curl --silent --output "$work_dir/body" --write-out '%{http_code}' \
  --header 'X-API-Key: invalid-sandbox-key-with-more-than-32-chars' \
  --header 'Idempotency-Key: sandbox-wrong-auth-001' \
  --header 'Content-Type: application/json' \
  --data-binary "@$fixture" \
  "$BASE_URL/api/v3/clinical/bilirubin/calculate")"
test "$status" = 401

status="$(curl --silent --show-error --output "$work_dir/fhir-capability.json" --write-out '%{http_code}' \
  --header "X-API-Key: $SANDBOX_API_KEY" \
  --header 'Accept: application/fhir+json' \
  "$BASE_URL/api/v3/fhir/R4/metadata")"
test "$status" = 200
jq -e '.resourceType == "CapabilityStatement" and .fhirVersion == "4.0.1"' "$work_dir/fhir-capability.json" >/dev/null

status="$(curl --silent --show-error --output "$work_dir/fhir-response.json" --write-out '%{http_code}' \
  --header "X-API-Key: $SANDBOX_API_KEY" \
  --header 'Idempotency-Key: sandbox-fhir-001' \
  --header 'Content-Type: application/fhir+json' \
  --header 'Accept: application/fhir+json' \
  --data-binary "@$fhir_fixture" \
  "$BASE_URL/api/v3/fhir/R4/\$bilirubin-calculate")"
test "$status" = 200
jq -e '.resourceType == "Bundle" and ([.entry[].resource.resourceType] | index("DiagnosticReport") != null)' "$work_dir/fhir-response.json" >/dev/null

status="$(curl --silent --show-error --output "$work_dir/hl7-response.hl7" --write-out '%{http_code}' \
  --header "X-API-Key: $SANDBOX_API_KEY" \
  --header 'Idempotency-Key: SANDBOX-HL7-001' \
  --header 'Content-Type: application/hl7-v2; charset=utf-8' \
  --header 'Accept: application/hl7-v2' \
  --data-binary "@$work_dir/hl7-oru-r01.hl7" \
  "$BASE_URL/api/v3/hl7/v251/oru-r01")"
test "$status" = 200
grep -q 'MSA|AA|' "$work_dir/hl7-response.hl7"
grep -q 'ZBR|' "$work_dir/hl7-response.hl7"

printf 'Sandbox conformance PASS: REST/FHIR/HL7, duplicate/conflict, validation boundaries, request limit and auth rejection.\n'
