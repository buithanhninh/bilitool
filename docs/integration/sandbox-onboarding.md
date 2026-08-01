# HIS/EMR Sandbox Onboarding

## Phạm vi

Sandbox dùng database và Docker volume riêng, chỉ bind `127.0.0.1:18080`, bootstrap một tenant/client test và không được nhập PHI production. Mọi identifier fixture bắt đầu bằng `TEST-` hoặc `SANDBOX-`.

## Khởi động

```bash
cd sandbox
cp .env.example .env
# Thay cả hai giá trị bằng random secrets riêng môi trường test.
docker compose --env-file .env up -d --build
curl --fail http://127.0.0.1:18080/health/ready
```

Sau bootstrap thành công, không chia sẻ `.env`; khi chuyển sandbox dùng lâu dài phải xóa bootstrap API key khỏi environment và rotate key qua admin lifecycle API.

## Automated conformance

Yêu cầu `curl` và `jq`:

```bash
SANDBOX_API_KEY='<sandbox-key>' \
BASE_URL='http://127.0.0.1:18080' \
../scripts/sandbox/run-conformance.sh
```

Script kiểm tra REST v3/FHIR R4/HL7 v2.5.1 happy path, semantic idempotent replay, payload conflict, malformed/unknown/oversized request, invalid UCUM unit, missing/wrong authentication và FHIR CapabilityStatement. Postman collection ở `sandbox/postman/`.

OpenAPI generated-client gate tạo TypeScript types trực tiếp từ `/openapi/v3.json`, compile strict và gửi request thật bằng client typed:

```bash
SANDBOX_API_KEY='<sandbox-key>' \
BASE_URL='http://127.0.0.1:18080' \
../scripts/sandbox/run-generated-client.sh
```

Production rehearsal tự dựng clean sandbox, chạy toàn bộ conformance/generated-client/load smoke, bật emergency kill switch để xác minh `503`, rollback về `200` và kiểm tra idempotency rows không mất:

```bash
REHEARSAL_EVIDENCE_DIR=artifacts/production-rehearsal \
../scripts/release/run-production-rehearsal.sh
```

## Credential lifecycle

Admin endpoints yêu cầu authenticated role `Admin`:

- `POST /admin/operations/his-clients`: provision/reactivate client.
- `POST /admin/operations/his-clients/{tenantCode}/{clientCode}/rotate`: rotate key, overlap 0-10.080 phút.
- `POST /admin/operations/his-clients/{tenantCode}/{clientCode}/rotate-certificate`: rotate SHA-256 client-certificate fingerprint, overlap 0-10.080 phút.
- `DELETE /admin/operations/his-clients/{tenantCode}/{clientCode}`: revoke client.

Secret chỉ xuất hiện trong request quản trị qua TLS; response không echo secret. Rotation procedure:

1. Sinh key mới tối thiểu 32 ký tự bằng CSPRNG.
2. Rotate với overlap ngắn đã duyệt.
3. Xác minh key cũ và mới cùng hoạt động trong overlap.
4. Chuyển client sang key mới.
5. Sau overlap, xác minh key cũ trả `401`.
6. Ghi evidence correlation ID và admin audit action.

## Hospital checklist

- Network: allowlist egress/ingress, DNS ổn định, timeout và proxy ownership rõ.
- TLS: TLS 1.2+, certificate chain hợp lệ, không disable hostname validation.
- Identifiers: source, facility, patient, encounter, order, specimen, observation namespace thống nhất.
- Timezone: REST/FHIR dùng ISO-8601 có offset; HL7 DTM có `+/-HHMM`.
- Units: UCUM `mg/dL` hoặc `umol/L`; LOINC `1975-2` hoặc `14631-6`.
- Retry: giữ nguyên payload và idempotency key/MSH-10; exponential backoff; tôn trọng `Retry-After`.
- Privacy: không dùng production PHI trong sandbox; fixture phải synthetic.
- Escalation: lưu correlation ID, timestamp UTC, protocol, tenant/client code và error code; không gửi API key hoặc PHI qua ticket thường.

## Dọn môi trường

```bash
docker compose --env-file .env down -v --remove-orphans
```

Lệnh xóa toàn bộ sandbox database volume. Không chạy với production compose project.
