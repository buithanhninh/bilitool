# HIS/EMR contract field policy

## Shared transport

- JSON property naming: camelCase.
- REST timestamp: ISO-8601 `date-time` có UTC offset; không nhận local time mơ hồ.
- FHIR timestamp: FHIR R4 `dateTime` có offset.
- HL7 timestamp: DTM `YYYYMMDDHHMMSS+/-HHMM`.
- Correlation ID: tùy chọn, tối đa 64 ký tự; chỉ chữ, số, `-`, `_`, `.`.
- `X-API-Key`: bắt buộc; client mTLS cần thêm certificate fingerprint khớp registry.
- `Idempotency-Key`: bắt buộc cho calculation; 8–128 ký tự theo policy filter.
- Unknown JSON fields: bị từ chối.
- Request size: REST v3 64 KiB; FHIR/HL7 128 KiB.

## Canonical REST v3 request

| Path | Required | Nullable | Type/limit |
|---|---:|---:|---|
| `source.system` | Yes | No | string, source-system ID |
| `source.facility` | Yes | No | string, facility ID |
| `source.messageId` | Yes | No | string, tenant-scoped message ID |
| `patient.identifier` | Yes | No | string |
| `patient.assigningAuthority` | Yes | No | string |
| `patient.birthTime` | Conditional | Yes | ISO-8601 with offset |
| `patient.ageHours` | Conditional | Yes | finite number, 1–336 |
| `patient.gestationalAgeWeeks` | Yes | No | integer, 35–42 |
| `patient.phototherapyStatus` | Yes | No | `none`, `phototherapy`, `intensive-phototherapy`, `stopped` |
| `encounter.identifier` | Yes | No | string |
| `order.identifier` | Yes | No | string |
| `specimen.identifier` | Yes | No | string |
| `specimen.collectedAt` | Yes | No | ISO-8601 with offset |
| `observation.identifier` | Yes | No | string |
| `observation.effectiveAt` | Yes | No | ISO-8601 with offset |
| `observation.value` | Yes | No | finite positive number |
| `observation.unit` | Yes | No | UCUM `mg/dL` hoặc `umol/L` |
| `riskFactors` | Yes | No | object; boolean members |

`patient.birthTime` hoặc `patient.ageHours` phải đủ để xác định tuổi. Khi cùng gửi, normalized age phải nằm trong tolerance validator.

## Compatibility REST v1/v2

- `tuoiTheoGio`: nullable finite number, 1–336; thay thế bằng birth/sample timestamps khi không gửi.
- `tongBilirubin`: required positive decimal.
- `donViDo`: enum `MgDl` hoặc `UmolL`; numeric ngoài enum bị từ chối.
- `tuoiThaiTuan`: required integer, 35–42.
- `trangThaiChieuDen`: enum hợp lệ; numeric ngoài enum bị từ chối.
- `yeuToNguyCo`: required non-null object.

## Breaking-change policy

`contracts/his-emr/contract-baseline.json` là baseline machine-readable. CI phải fail khi required path mất, JSON type đổi, media type đổi hoặc status contract không còn khớp. Chỉ giá trị tại `dynamicValueAllowlist` được phép thay đổi giữa request; thay đổi schema cần sửa manifest có review rõ ràng.
