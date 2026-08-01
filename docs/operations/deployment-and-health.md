# HIS/EMR Deployment, Health & Operations

## Health endpoints

- `GET /health/live`: kiểm tra process sống; không truy cập DB.
- `GET /health/ready`: kiểm tra PostgreSQL và clinical engine smoke test. Trả `503` nếu dependency lõi lỗi.
- `GET /admin/operations/health`: snapshot chi tiết, yêu cầu role `Admin`; gồm engine version, API client, webhook subscription, pending/dead-letter outbox và audit gần nhất.
- `GET /admin/operations/metrics`: cửa sổ 15 phút, yêu cầu role `Admin`; gồm request count, 5xx rate, p50/p95/p99, route bucket và HIS integration event counters.

## Required environment variables

```bash
ConnectionStrings__PostgreSQL="Host=postgres;Port=5432;Database=bilitool_vn;Username=bilitool;Password=<secret>"
Authentication__Google__ClientId="<google-client-id>"
Authentication__Google__ClientSecret="<google-client-secret>"
HisBootstrap__TenantId="<tenant-id>"
HisBootstrap__ClientId="<client-id>"
HisBootstrap__ApiKey="<random-api-key-minimum-32-characters>"
```

Bootstrap credential chỉ dùng một lần khi registry chưa có client. Production phải xóa biến `HisBootstrap__ApiKey` sau provisioning. `ApiSettings:EnableLegacyApiKeys` giữ `false`; nếu migration tạm thời bắt buộc, đặt `ApiSettings:LegacyApiKeysDisableAfter` theo UTC để credential legacy tự hết hiệu lực. Danh sách `AllowedApiKeys` không phải đường production.

Client production có thể bật mTLS bằng `RequireMutualTls=true` và SHA-256 certificate fingerprint 64 ký tự hex khi provision. Filter yêu cầu đồng thời API key hợp lệ và client certificate khớp. Rotate certificate qua `/admin/operations/his-clients/{tenantCode}/{clientCode}/rotate-certificate`; overlap tối đa 7 ngày, mọi thao tác được admin audit.

## SLO và alerts mặc định

- Availability HIS calculation: 99.9% theo cửa sổ tháng, loại trừ maintenance công bố.
- Latency: p95 dưới `2000 ms` cho request API tại app boundary.
- Server error: dưới `2%` trong cửa sổ 15 phút, tối thiểu 10 request.
- Outbox: pending dưới 100; dead-letter bằng 0; oldest pending dưới 10 phút.
- Alert cooldown: 15 phút để tránh alert storm.

Cấu hình qua `Operations__AlertP95Milliseconds`, `Operations__AlertErrorRatePercent`, `Operations__AlertMinimumRequests`, `Operations__AlertPendingOutbox`, `Operations__AlertDeadLetterOutbox`, `Operations__AlertOldestPendingMinutes`, `Operations__AlertCooldownMinutes`.

Alert evaluator chạy mỗi 60 giây mặc định, cấu hình qua `Operations__AlertEvaluationIntervalSeconds` trong khoảng 5–3.600 giây. Automated incident simulation xác minh SLO breach phát đúng một structured warning và cooldown chặn alert storm.

## On-call incident matrix

| Tín hiệu | Kiểm tra đầu tiên | Giảm thiểu | Escalation |
|---|---|---|---|
| Auth failure tăng | API client expiry/revoke, scope, mTLS fingerprint, trusted proxy | Rotate credential/certificate trong overlap; disable client bị lộ | Security owner nếu nghi credential compromise |
| p95 hoặc 5xx vượt SLO | `/admin/operations/metrics`, correlation span, saturation/rate-limit | Giảm canary allowlist, bật kill switch khi clinical route không ổn định | Incident commander nếu vượt hai evaluation windows |
| Readiness/DB lỗi | `/health/ready`, PostgreSQL connection, migration và storage | Dừng rollout; failover/restore theo backup-DR runbook | DBA/on-call; kích hoạt DR khi không phục hồi trong RTO budget |
| Outbox pending/dead-letter | Queue age, subscription active, circuit state, dependency status | Tạm dừng webhook lỗi; replay dead-letter sau khi dependency ổn định | Integration owner và bệnh viện nhận webhook |
| Webhook timeout/5xx | DNS/IP policy, TLS, endpoint response, bulkhead/circuit | Giữ circuit open, không tăng concurrency mù; rotate secret nếu nghi lộ | Security owner nếu signature/secret bất thường |

Mọi incident phải lưu UTC timeline, correlation IDs, impacted tenant/client, metric snapshot, quyết định kill-switch/rollback, người chỉ huy và thời điểm đóng. Không đưa PHI hoặc raw clinical payload vào ticket/log.

## Request resilience

- REST v2/v3, FHIR calculation và HL7 ORU có deadline mặc định 5 giây qua `Operations:HisRequestTimeoutSeconds`.
- Middleware trước model binding trả `413 application/problem+json` khi REST v3 vượt 64 KiB hoặc FHIR/HL7 vượt 128 KiB; không để malformed parser che mất lỗi kích thước.
- Giá trị timeout bị giới hạn 1-30 giây. Request timeout/client disconnect truyền cancellation xuống validator, MediatR, EF và audit path; controller không chuyển cancellation thành lỗi 500.
- Rate limit mặc định 30 request/60 giây theo API-key fingerprint. Tuning qua `Operations:HisRateLimitPermit` và `Operations:HisRateLimitWindowSeconds`, với guard 1-10.000 permit và 1-3.600 giây.
- CI capacity smoke gửi 20 calculation đồng thời, yêu cầu 0 lỗi và p95 dưới 2 giây. Đây là regression gate, không thay thế soak test production-like.
- K6 profiles nằm tại `scripts/reliability/his-load.js`: `smoke` 10 rps/10 giây, `load` 100 rps/2 phút, `soak` 50 rps/30 phút. Mọi profile yêu cầu 100% checks, lỗi dưới 2%, p95 dưới 2 giây, p99 dưới 5 giây và không dropped iteration. Chạy bằng `LOAD_PROFILE=load|soak scripts/reliability/run-load-soak.sh`; JSON evidence ghi vào `artifacts/reliability` hoặc `LOAD_EVIDENCE_DIR`.
- Load drill ngày 2026-08-01 trên Docker app + PostgreSQL 16 đạt 12.000/12.000 request tại 100 rps, 0% HTTP error, p95 63,19 ms, p99 185,45 ms và 0 dropped iteration. Soak drill cùng ngày đạt 90.000/90.000 request tại 50 rps trong 30 phút, 270.000/270.000 checks, 0% HTTP error, p95 36,92 ms, p99 63,37 ms, max 269,20 ms, 0 dropped/interrupted iteration; readiness sau soak là `Ready` và có 90.002 idempotency records. CI smoke trên clean volume đạt 101 request, 0% error, p95 49,21 ms. Evidence soak gồm JSON summary, readiness, DB row count và SHA-256 manifest đã xác minh.
- Webhook outbound dùng bulkhead mặc định 8 concurrent/100 ms queue và circuit breaker 5 dependency failures/30 giây. 5xx, timeout và network error mở circuit; 4xx không làm trip. Half-open chỉ cho một probe. Tuning qua `Webhooks:Resilience:*`.
- Idempotency response được lưu dưới dạng opaque payload được JSON-encode cùng media type. Cách này hỗ trợ JSON, FHIR và raw HL7 ACK trong PostgreSQL `jsonb`, đồng thời đọc tương thích JSON records cũ.

## Tracing và correlation

ActivitySource: `BiliTool.Vn.HisIntegration`. Các span REST v3, FHIR, HL7 và webhook có tenant/client/result metadata nhưng không ghi clinical identifiers hoặc payload PHI. Mọi response phát `X-Correlation-ID`; input chỉ nhận ký tự chữ, số, `-`, `_`, `.` và tối đa 64 ký tự.

## Dead-letter replay

`POST /admin/operations/outbox/{eventId}/replay` yêu cầu role `Admin`. Chỉ event `DeadLetter` với subscription còn active được reset. Hành động ghi `admin_audit_logs` với action `his.outbox.dead_letter.replay`; worker gửi lại theo lease và retry policy.

## Webhook security

- Chỉ HTTPS.
- Redirect bị tắt.
- Private, loopback, link-local và unique-local address bị chặn mặc định.
- Chữ ký HMAC-SHA256 trên chuỗi `timestamp.payload`.
- Consumer phải kiểm tra timestamp tolerance, signature constant-time và event ID chống replay.

## Audit và retention

Clinical audit, result provenance và outbox event được lưu cùng transaction. `Audit:ClinicalRetentionDays` mặc định 180, tối thiểu 30 ngày. Idempotency record hết hạn được purge cùng retention worker. Backup phải hoàn tất trước purge và trước migration production.

Legal hold quản lý bằng admin operations API và có audit hành động. Retention worker loại trừ mọi record khớp hold active, tạo immutable purge report cho cả dry-run và execution.

## Deployment gates

1. `dotnet test tests/BiliTool.Vn.Domain.Tests/BiliTool.Vn.Domain.Tests.csproj --no-restore`
2. `dotnet build BiliTool.Vn.sln --no-restore`
3. `git diff --check`
4. Chạy migration rehearsal trên bản restore production-like.
5. Kiểm tra `/health/ready`, `/admin/operations/health`, REST/FHIR/HL7 smoke tests.
6. Canary một tenant/client, theo dõi error rate, p95 và outbox.
7. Rollback nếu readiness lỗi, 5xx vượt 2%, p95 vượt 2 giây hoặc xuất hiện dead-letter.
