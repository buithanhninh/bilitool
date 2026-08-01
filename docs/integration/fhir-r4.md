# BiliTool.Vn FHIR R4 Implementation Guide

## Base URL

`/api/v3/fhir/R4`

- `GET /metadata`: CapabilityStatement
- `POST /$bilirubin-calculate`: bilirubin calculation operation

Authentication dùng `X-API-Key`; operation yêu cầu `Idempotency-Key`.

## Input Bundle

Input là FHIR R4 `Bundle.type=transaction`, media type `application/fhir+json`, gồm đúng một resource mỗi loại:

- `Patient`
- `Encounter`
- `ServiceRequest`
- `Specimen`
- `Observation`

`Bundle.identifier.system` xác định source system; `Bundle.identifier.value` là message ID. Mã cơ sở nằm trong `Bundle.meta.tag` với `system=https://bilitool.vn/fhir/CodeSystem/facility` và `code` là facility ID. `Bundle` R4 không hỗ trợ `extension`, nên facility extension cũ bị từ chối.

## Required profiles and extensions

Base extension URL: `https://bilitool.vn/fhir/StructureDefinition/`

| Resource | Extension | Type | Required |
|---|---|---:|---:|
| Patient | `age-hours` | `valueDecimal` | Yes |
| ServiceRequest | `gestational-age-weeks` | `valueInteger` | Yes |
| ServiceRequest | `phototherapy-status` | `valueString` | Yes |
| ServiceRequest | `immune-hemolysis-or-g6pd` | `valueBoolean` | No |
| ServiceRequest | `sepsis-or-suspected-sepsis` | `valueBoolean` | No |
| ServiceRequest | `albumin-below-3-g-dl` | `valueBoolean` | No |
| ServiceRequest | `clinical-instability` | `valueBoolean` | No |
| ServiceRequest | `jaundice-first-24-hours` | `valueBoolean` | No |
| ServiceRequest | `rh-hemolysis` | `valueBoolean` | No |
| ServiceRequest | `abo-hemolysis` | `valueBoolean` | No |
| ServiceRequest | `acute-bilirubin-encephalopathy` | `valueBoolean` | No |

`phototherapy-status`: `none`, `phototherapy`, `intensive-phototherapy`, `stopped`.

Package IG máy đọc nằm tại `fhir/ig/package`; canonical Bundle profile là `https://bilitool.vn/fhir/StructureDefinition/bilitool-bilirubin-bundle`. Chạy validator HL7 chính thức bằng `./scripts/fhir/validate-r4.sh`; script pin validator `6.10.0` và kiểm tra SHA-256 trước khi chạy.

## Bilirubin Observation

- `Observation.status`: client nên gửi `final`.
- `Observation.code.coding.system`: `http://loinc.org`.
- Supported LOINC codes: `1975-2`, `14631-6`.
- `valueQuantity.system`: `http://unitsofmeasure.org`.
- `valueQuantity.code`: `mg/dL` hoặc `umol/L`.
- `subject`, `encounter`, `specimen` phải tham chiếu resource tương ứng trong Bundle.

## Output

Response là `Bundle.type=collection` gồm:

- Derived `Observation` với bilirubin đã chuẩn hóa và threshold components.
- `DiagnosticReport` với recommendation, result reference, guideline code và engine version.

FHIR validation errors trả `OperationOutcome`. Retry cùng `Idempotency-Key` và payload trả cùng response.
