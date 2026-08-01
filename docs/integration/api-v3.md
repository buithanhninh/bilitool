# BiliTool.Vn HIS/EMR Clinical API v3

## Endpoint

```http
POST /api/v3/clinical/bilirubin/calculate
X-API-Key: <tenant-client-credential>
Idempotency-Key: <unique-request-key>
X-Correlation-ID: <optional-correlation-id>
Content-Type: application/json
```

OpenAPI 3.1.1 document: `/openapi/v3.json`. Interactive UI: `/openapi`.

Document khai báo security scheme `ApiKey` qua header `X-API-Key`. Mọi operation lâm sàng yêu cầu credential tenant/client hợp lệ và scope phù hợp. Client có `RequireMutualTls=true` còn phải gửi certificate TLS có SHA-256 fingerprint khớp registry.

## Canonical request

```json
{
  "source": {
    "system": "HIS-A",
    "facility": "FAC-A",
    "messageId": "msg-001"
  },
  "patient": {
    "identifier": "patient-001",
    "assigningAuthority": "FAC-A",
    "birthTime": null,
    "ageHours": 48,
    "gestationalAgeWeeks": 38,
    "phototherapyStatus": "none"
  },
  "encounter": { "identifier": "enc-001" },
  "order": { "identifier": "order-001" },
  "specimen": {
    "identifier": "spec-001",
    "collectedAt": "2026-07-31T08:00:00Z"
  },
  "observation": {
    "identifier": "obs-001",
    "effectiveAt": "2026-07-31T08:00:00Z",
    "value": 12,
    "unit": "mg/dL"
  },
  "riskFactors": {}
}
```

`observation.unit` chỉ nhận UCUM-compatible code `mg/dL` hoặc `umol/L`. `patient.phototherapyStatus` nhận `none`, `phototherapy`, `intensive-phototherapy`, `stopped`. Trường JSON không công bố bị từ chối.

## Response

Response không chứa legacy DTO. Các nhóm ổn định:

- `resultId`, `correlationId`
- `references`: message, patient, encounter, order, specimen, observation IDs
- `provenance`: guideline code/revision/effective date, engine mode/version, dataset mode/revision, decision protocol
- `observation`: tuổi và bilirubin đã chuẩn hóa song song hai đơn vị
- `thresholds`: ngưỡng AAP/NICE kèm đơn vị trong tên trường
- `recommendation`: mức cảnh báo, can thiệp và thời gian đo lại

## Error contract

Mọi lỗi v3 dùng `application/problem+json` và có:

- `errorCode`: mã ổn định cho máy đọc
- `correlationId`: truy vết log/audit
- `retryable`: client có nên retry
- `errors`: lỗi theo field khi áp dụng

Mã chính: `invalid_json`, `invalid_request`, `clinical_validation_failed`, `missing_api_key`, `invalid_api_key`, `insufficient_scope`, `invalid_idempotency_key`, `idempotency_payload_conflict`, `idempotency_request_in_progress`, `rate_limit_exceeded`, `clinical_calculation_failed`.
