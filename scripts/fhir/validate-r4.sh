#!/usr/bin/env bash
set -euo pipefail

readonly VALIDATOR_VERSION="6.10.0"
readonly VALIDATOR_SHA256="fc663ae55dd31bbfde19788dddfb49cacbeebc3c64498fa7b7779df90000434b"
readonly CACHE_DIR="${FHIR_VALIDATOR_CACHE:-${HOME}/.cache/bilitool-fhir-validator}"
readonly VALIDATOR_JAR="${CACHE_DIR}/validator_cli-${VALIDATOR_VERSION}.jar"
readonly ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
readonly OUTPUT_FILE="${ROOT_DIR}/artifacts/fhir-validation.json"

mkdir -p "${CACHE_DIR}"
mkdir -p "$(dirname "${OUTPUT_FILE}")"
if [[ ! -f "${VALIDATOR_JAR}" ]]; then
  curl --fail --location --retry 3 \
    "https://github.com/hapifhir/org.hl7.fhir.core/releases/download/${VALIDATOR_VERSION}/validator_cli.jar" \
    --output "${VALIDATOR_JAR}.tmp"
  printf '%s  %s\n' "${VALIDATOR_SHA256}" "${VALIDATOR_JAR}.tmp" | sha256sum --check --status
  mv "${VALIDATOR_JAR}.tmp" "${VALIDATOR_JAR}"
fi

printf '%s  %s\n' "${VALIDATOR_SHA256}" "${VALIDATOR_JAR}" | sha256sum --check --status
validator_args=(
  "/workspace/fhir/examples/bilirubin-request-bundle.json"
  -version 4.0.1
  -ig "/workspace/fhir/ig/package"
  -profile "https://bilitool.vn/fhir/StructureDefinition/bilitool-bilirubin-bundle"
  -tx n/a
  -output "/workspace/artifacts/fhir-validation.json"
)

if command -v java >/dev/null 2>&1; then
  java -jar "${VALIDATOR_JAR}" \
    "${ROOT_DIR}/fhir/examples/bilirubin-request-bundle.json" \
    -version 4.0.1 \
    -ig "${ROOT_DIR}/fhir/ig/package" \
    -profile "https://bilitool.vn/fhir/StructureDefinition/bilitool-bilirubin-bundle" \
    -tx n/a \
    -output "${OUTPUT_FILE}"
elif command -v docker >/dev/null 2>&1; then
  mkdir -p "${CACHE_DIR}/packages"
  docker run --rm \
    -v "${ROOT_DIR}:/workspace" \
    -v "${VALIDATOR_JAR}:/validator_cli.jar:ro" \
    -v "${CACHE_DIR}/packages:/root/.fhir/packages" \
    eclipse-temurin:17-jre \
    java -jar /validator_cli.jar "${validator_args[@]}"
else
  echo "Java 17 hoặc Docker là bắt buộc để chạy HL7 FHIR Validator." >&2
  exit 1
fi

echo "FHIR R4 official validator PASS"
