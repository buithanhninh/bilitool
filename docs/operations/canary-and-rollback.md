# HIS/EMR Canary & Rollback Runbook

## Rollout controls

```json
{
  "HisRollout": {
    "V3Enabled": true,
    "EmergencyKillSwitch": false,
    "EnabledTenants": [],
    "DisabledTenants": []
  }
}
```

- `V3Enabled=false`: dừng toàn bộ REST v3, FHIR R4 và HL7 v2.5.1 calculation routes.
- `EmergencyKillSwitch=true`: emergency stop toàn bộ v3 protocols, ưu tiên hơn allowlist.
- `EnabledTenants`: nếu không rỗng, chỉ tenant code trong danh sách được dùng v3.
- `DisabledTenants`: denylist ưu tiên hơn allowlist.
- Request bị chặn trả `503`, `errorCode=tenant_rollout_disabled`, `retryable=true`, `Retry-After: 60`.
- API v1/v2 không chịu rollout filter, dùng làm compatibility rollback path.

## Canary sequence

1. Đạt Checkpoint F, backup và migration rehearsal.
2. Đặt `EnabledTenants` chỉ chứa tenant canary; giữ tenant khác trên v1/v2.
3. Chạy REST/FHIR/HL7 conformance và credential rotation drill.
4. Theo dõi ít nhất một cửa sổ tải đã duyệt: availability, p95, 5xx, auth failures, idempotency conflicts, audit failures, outbox age/dead-letter.
5. So sánh thresholds REST v3/FHIR/HL7 và baseline v2 trên synthetic/golden dataset; yêu cầu 100% match.
6. Mở rộng allowlist theo từng tenant, không chuyển global trước khi evidence được ký duyệt.

## Rollback triggers

- Clinical mismatch bất kỳ.
- Readiness fail hoặc database dependency lỗi kéo dài.
- 5xx vượt 2%, p95 vượt 2 giây hoặc dead-letter xuất hiện.
- Auth rotation khóa client hoặc tenant isolation finding.
- Audit/outbox không được ghi bền vững.

## Rollback actions

1. Đặt tenant vào `DisabledTenants`; nếu nhiều tenant ảnh hưởng, bật `EmergencyKillSwitch`.
2. Giữ v1/v2 route hoạt động; client retry phải dùng contract/key phù hợp phiên bản cũ.
3. Không xóa idempotency/outbox/audit records khi rollback.
4. Nếu release artifact lỗi, deploy artifact trước và kiểm tra migration compatibility trước khi downgrade.
5. Chạy `/health/ready`, REST v2 smoke, queue health và audit query.
6. Ghi incident timeline, correlation IDs, impacted tenants, result IDs và quyết định clinical reviewer.

## Deprecation gate

Không loại bỏ v1/v2 khi chưa có telemetry chứng minh không còn active client trong thời gian thông báo đã phê duyệt. Deprecation phải có ngày công bố, sunset date, migration guide, owner và rollback window cụ thể.
