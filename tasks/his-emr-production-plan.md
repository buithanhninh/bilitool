# Kế hoạch triển khai: HIS/EMR Production

## 1. Mục tiêu

Hoàn thiện tích hợp HIS/EMR thành dịch vụ production-grade cho trao đổi dữ liệu lâm sàng bilirubin: tính đúng, truy nguyên được, bảo mật theo từng cơ sở, chịu retry an toàn, tương thích ngược, quan sát được và có quy trình phát hành/rollback.

Phạm vi dùng thuật ngữ **HIS/EMR**. “ERM” trong yêu cầu được hiểu là “EMR”.

## 2. Trạng thái xuất phát

- Có REST API v1 và wrapper v2, xác thực bằng `X-API-Key`, rate limit, `ProblemDetails`, clinical audit và retention.
- Engine domain có 50 unit test đạt; solution build sạch.
- Chưa có client identity, tenant, idempotency, correlation xuyên suốt, contract version chuẩn, integration test, FHIR/HL7, quản trị khóa, SLA hoặc cơ chế retry bất đồng bộ.
- Validation ngày/giờ, enum và nested object còn lỗi logic có thể ảnh hưởng kết quả hoặc trả 500.

## 3. Nguyên tắc kiến trúc

- Clinical engine giữ thuần domain; adapter HIS/EMR không được chứa công thức lâm sàng.
- Một request có một `CorrelationId`, một `RequestId`, một `ResultId`; tất cả log, audit và response dùng cùng định danh.
- Mỗi client thuộc một tenant/cơ sở, có credential riêng, scope riêng, quota riêng và lifecycle rõ ràng.
- Mọi operation ghi phải idempotent; retry không tạo kết quả hoặc audit nghiệp vụ trùng.
- API contract machine-readable, versioned, backward-compatible và có deprecation policy.
- Dữ liệu bệnh nhân tối thiểu, mã hóa khi truyền/lưu, audit truy cập, retention cấu hình theo tenant.
- FHIR/HL7 là adapter ngoài contract lõi; REST canonical model là nguồn sự thật nội bộ.
- Không rollout production nếu chưa đạt clinical, security, reliability và rollback gate.

## 4. Kiến trúc đích

```text
HIS / EMR / LIS
      |
      | REST v2/v3, FHIR R4, HL7 v2 adapter
      v
API Gateway / Reverse Proxy
      |
      | mTLS or OAuth2 client credentials
      v
Integration API
  - Tenant/client identity
  - Validation + normalization
  - Idempotency + correlation
  - Contract/error mapping
      |
      v
Clinical Application Service
      |
      v
Clinical Domain Engine
      |
      +-- Immutable clinical audit
      +-- Metrics/traces/logs
      +-- Outbox/webhook worker
```

## 5. Pha triển khai

### Phase 0 — Baseline, contract và quyết định lâm sàng

#### Task 1: Đóng băng contract hiện tại

**Mô tả:** Chụp schema JSON thực tế của v1/v2, response lỗi, enum serialization và hành vi validation để ngăn sửa lỗi làm vỡ client hiện hữu.

**Acceptance criteria:**
- [ ] Có golden contract cho mọi response 200/400/401/429/500/503.
- [ ] Có danh sách field bắt buộc, nullable, đơn vị, timezone và giới hạn.
- [ ] CI phát hiện mọi breaking change ngoài allowlist.

**Verification:** Contract snapshot tests và JSON schema validation.

**Dependencies:** None

#### Task 2: Chốt clinical governance

**Mô tả:** Xác định chính xác vai trò AAP 2022, NICE CG98, phiên bản dataset, phạm vi tuổi thai và cách chọn phác đồ.

**Acceptance criteria:**
- [ ] Metadata trả đúng engine/dataset thực tế, không dùng nhãn “active” gây hiểu sai.
- [ ] Mỗi kết quả chứa guideline code, revision, effective date và engine version.
- [ ] Clinical reviewer ký duyệt bộ ca chuẩn và quy tắc cảnh báo.

**Verification:** Clinical baseline suite và biên bản phê duyệt.

**Dependencies:** Task 1

#### Checkpoint A

- [ ] Build và 50 test hiện tại vẫn đạt.
- [ ] Contract baseline được lưu trong source control.
- [ ] Không thay đổi hành vi production.

### Phase 1 — Sửa lỗi correctness và chuẩn hóa input

#### Task 3: Sửa mô hình thời điểm lâm sàng

**Mô tả:** Thay cặp ngày/giờ rời bằng normalization duy nhất; xử lý offset/timezone rõ ràng và giữ compatibility adapter cho v1.

**Acceptance criteria:**
- [ ] Validator và handler dùng cùng normalized birth/sample instant.
- [ ] Ca cùng ngày, qua nửa đêm, DST và offset khác nhau cho tuổi giờ chính xác.
- [ ] Khi gửi cả `TuoiTheoGio` và timestamps, hệ thống kiểm tra sai lệch theo tolerance định nghĩa.

**Verification:** Boundary/property tests cho 1–336 giờ và timezone tests.

**Dependencies:** Checkpoint A

#### Task 4: Khóa validation và enum

**Mô tả:** Chặn null nested object, enum ngoài miền, số không hữu hạn, payload quá lớn và field không hỗ trợ.

**Acceptance criteria:**
- [ ] Input sai luôn trả 400 có `errorCode`, field path và correlation ID; không trả 500.
- [ ] `DonViDo`, `TrangThaiChieuDen` và mọi enum phải `IsInEnum`.
- [ ] JSON unknown-field policy được chốt và kiểm thử.

**Verification:** Fuzz, malformed JSON và model-binding integration tests.

**Dependencies:** Task 3

#### Task 5: Tạo canonical integration request

**Mô tả:** Tách DTO UI khỏi DTO tích hợp; bổ sung tenant, patient encounter, order, specimen, observation và source-system identifiers.

**Acceptance criteria:**
- [ ] Canonical request có `sourceSystem`, `patientId`, `encounterId`, `orderId`, `specimenId`, `observationId` theo cardinality đã định nghĩa.
- [ ] Identifier được validate theo tenant, không log plaintext ngoài policy.
- [ ] v1/v2 map vào canonical model mà không đổi kết quả clinical.

**Verification:** Mapping tests hai chiều và PHI logging tests.

**Dependencies:** Tasks 3–4

#### Checkpoint B

- [ ] Bộ test lỗi logic mới đạt 100%.
- [ ] Không còn đường input hợp lệ bị từ chối do ngày/giờ.
- [ ] Không còn malformed request gây 500.

### Phase 2 — Identity, tenant và credential security

#### Task 6: Tạo tenant và API client registry

**Mô tả:** Lưu tenant/client có trạng thái, scope, quota, môi trường, ngày hết hạn và metadata cơ sở y tế.

**Acceptance criteria:**
- [ ] Mỗi credential ánh xạ chính xác một `TenantId` và `ApiClientId`.
- [ ] Client bị khóa/hết hạn bị từ chối; mọi lần xác thực được audit.
- [ ] Không còn danh sách API key dùng chung làm identity production.

**Verification:** Authentication matrix integration tests.

**Dependencies:** Checkpoint B

#### Task 7: Nâng cấp credential và transport security

**Mô tả:** Hỗ trợ OAuth2 client credentials hoặc mTLS; API key chỉ giữ cho migration có thời hạn.

**Acceptance criteria:**
- [ ] Secret lưu dạng hash hoặc trong secret manager; không ghi log/config repository.
- [ ] Có rotation hai khóa, revoke tức thời và cảnh báo khóa sắp hết hạn.
- [ ] Scope giới hạn endpoint và action; TLS policy được kiểm thử tại edge.

**Verification:** Security integration tests và credential rotation drill.

**Dependencies:** Task 6

#### Task 8: Khóa proxy và rate limiting

**Mô tả:** Chỉ trust proxy/network xác định; rate limit theo tenant/client kết hợp IP, không chỉ IP.

**Acceptance criteria:**
- [ ] `X-Forwarded-For` giả từ nguồn không tin cậy không thay đổi client IP.
- [ ] Quota tách theo client, endpoint và burst; response 429 có `Retry-After`.
- [ ] Có limit kích thước body, timeout và concurrent request.

**Verification:** Spoofing tests và load/rate-limit tests.

**Dependencies:** Tasks 6–7

#### Checkpoint C

- [ ] Không còn Critical/High trong security review.
- [ ] Rotation/revoke credential diễn tập thành công.
- [ ] Tenant isolation tests đạt 100%.

### Phase 3 — Idempotency, correlation và audit

#### Task 9: Thêm idempotency end-to-end

**Mô tả:** Lưu `Idempotency-Key`, request fingerprint, trạng thái và response để retry trả cùng kết quả.

**Acceptance criteria:**
- [ ] Cùng tenant + key + payload trả cùng `ResultId` và response.
- [ ] Cùng key nhưng payload khác trả 409.
- [ ] Request concurrent không tạo hai calculation/audit nghiệp vụ.

**Verification:** Concurrency và retry integration tests với PostgreSQL thật.

**Dependencies:** Checkpoint C

#### Task 10: Chuẩn hóa correlation và provenance

**Mô tả:** Truyền `X-Correlation-ID` hợp lệ hoặc sinh mới; gắn vào response, logs, traces, audit và outbound event.

**Acceptance criteria:**
- [ ] `RequestId`, `CorrelationId`, `ResultId`, `TenantId`, `ApiClientId` xuất hiện nhất quán.
- [ ] `ResultId` được tạo trước calculation và liên kết audit DB.
- [ ] Có thể truy từ HIS request đến clinical trace bằng một truy vấn.

**Verification:** Trace continuity integration test.

**Dependencies:** Task 9

#### Task 11: Nâng cấp clinical audit

**Mô tả:** Chuyển audit sang record bất biến, allowlist field, integrity hash, retention/legal-hold và truy vấn vận hành bảo vệ riêng.

**Acceptance criteria:**
- [ ] Audit không thể update/delete qua application role thông thường.
- [ ] PHI/secret không lọt vào log; payload nhạy cảm mã hóa at rest.
- [ ] Chính sách fail-open/fail-closed được cấu hình theo tenant và có alert.

**Verification:** Tamper, redaction, retention và authorization tests.

**Dependencies:** Task 10

#### Checkpoint D

- [ ] Retry và concurrency không tạo bản ghi trùng.
- [ ] Audit truy nguyên đầy đủ, kiểm tra integrity đạt.
- [ ] Privacy review phê duyệt data minimization/retention.

### Phase 4 — Contract production và interoperability

#### Task 12: Phát hành API v3 canonical

**Mô tả:** Tạo contract production độc lập UI, lỗi chuẩn RFC 9457, version header và deprecation policy.

**Acceptance criteria:**
- [ ] OpenAPI 3.1 mô tả đủ auth, schemas, examples và mọi status code.
- [ ] Error có stable `errorCode`, field errors, correlation ID và retryability.
- [ ] v1/v2 chạy qua compatibility adapter; không nhân đôi clinical logic.

**Verification:** OpenAPI lint, generated-client tests và backward compatibility tests.

**Dependencies:** Checkpoint D

#### Task 13: Xây dựng FHIR R4 adapter

**Mô tả:** Map canonical model với `Patient`, `Encounter`, `ServiceRequest`, `Specimen`, `Observation`, `DiagnosticReport` theo implementation guide nội bộ.

**Acceptance criteria:**
- [ ] Có CapabilityStatement, profiles, terminology bindings và validation examples.
- [ ] Bilirubin value dùng UCUM; identifier và effective time không mất nghĩa.
- [ ] FHIR validator chấp nhận toàn bộ fixture hợp lệ và từ chối fixture sai.

**Verification:** Official FHIR validator và round-trip mapping tests.

**Dependencies:** Task 12

#### Task 14: Xây dựng HL7 v2/LIS adapter

**Mô tả:** Nhận hoặc phát ORU/ORM theo nhu cầu bệnh viện; map PID/PV1/ORC/OBR/OBX vào canonical model.

**Acceptance criteria:**
- [ ] Message profile, ACK/NACK và error mapping được tài liệu hóa.
- [ ] Duplicate message control dùng MSH-10 kết hợp tenant.
- [ ] Encoding, escaping, timezone và UCUM được kiểm thử.

**Verification:** Conformance fixtures và retry/duplicate tests.

**Dependencies:** Task 12

#### Task 15: Outbound result và webhook an toàn

**Mô tả:** Cung cấp synchronous response và optional webhook/outbox cho HIS không giữ kết nối dài.

**Acceptance criteria:**
- [ ] Outbox transaction bảo đảm không mất event sau khi calculation commit.
- [ ] Webhook ký HMAC/mTLS, retry backoff, dead-letter và replay có audit.
- [ ] Consumer duplicate-safe theo event/result ID.

**Verification:** Failure-injection tests và webhook conformance tests.

**Dependencies:** Tasks 12–14

#### Checkpoint E

- [ ] REST v3, FHIR và HL7 cùng cho kết quả canonical tương đương.
- [ ] Generated client chạy được trên sandbox.
- [ ] Không có breaking change v1/v2 ngoài deprecation đã công bố.

### Phase 5 — Reliability, observability và vận hành

#### Task 16: Thiết lập SLO và telemetry

**Mô tả:** Bổ sung metrics, structured logs và OpenTelemetry traces theo tenant/client nhưng không lộ PHI.

**Acceptance criteria:**
- [ ] Dashboard có throughput, p50/p95/p99, 4xx/5xx, auth failure, duplicate, audit failure và dependency latency.
- [ ] SLO được chốt: availability, latency và error budget theo endpoint.
- [ ] Alert có owner, severity, runbook và chống alert storm.

**Verification:** Synthetic monitoring và alert routing drill.

**Dependencies:** Checkpoint E

#### Task 17: Resilience và capacity

**Mô tả:** Áp dụng timeout, cancellation, bulkhead, circuit breaker cho dependency; xác định capacity và degradation policy.

**Acceptance criteria:**
- [ ] Client disconnect/cancellation dừng công việc không cần thiết.
- [ ] Dependency failure không làm cạn thread/connection pool.
- [ ] Load test đạt SLO tại tải mục tiêu cộng headroom đã chốt.

**Verification:** Load, soak và chaos tests.

**Dependencies:** Task 16

#### Task 18: Backup, DR và data lifecycle

**Mô tả:** Hoàn thiện backup mã hóa, restore, RPO/RTO, migration rehearsal, legal hold và purge có chứng cứ.

**Acceptance criteria:**
- [ ] Restore production-like DB đạt RPO/RTO.
- [ ] Migration forward/rollback được diễn tập trên bản sao dữ liệu.
- [ ] Retention/purge tạo báo cáo và không phá legal hold.

**Verification:** Quarterly DR drill và checksum reconciliation.

**Dependencies:** Tasks 11, 16

#### Checkpoint F

- [ ] SLO đạt qua soak test.
- [ ] Alert, incident và DR drill thành công.
- [ ] Runbook on-call đủ cho auth, latency, DB, queue và webhook.

### Phase 6 — Test pyramid, release và nghiệm thu

#### Task 19: Hoàn thiện test pyramid

**Mô tả:** Bổ sung unit, integration, contract, security, concurrency, interoperability và E2E test trong CI.

**Acceptance criteria:**
- [ ] Controller/auth/audit/idempotency chạy với PostgreSQL thật trong CI.
- [ ] Contract tests chạy cho v1/v2/v3, FHIR và HL7 fixtures.
- [ ] Test cố ý phá validation, tenant isolation hoặc clinical baseline làm CI đỏ.

**Verification:** CI evidence và mutation/failure injection.

**Dependencies:** Checkpoint F

#### Task 20: Sandbox và onboarding bệnh viện

**Mô tả:** Cung cấp sandbox tách biệt, test credential, sample clients, Postman collection và conformance checklist.

**Acceptance criteria:**
- [ ] Không có PHI production trong sandbox.
- [ ] Đối tác tự chạy happy path, validation, retry, duplicate và auth rotation.
- [ ] Có checklist network, TLS, identifiers, timezone, UCUM và contact escalation.

**Verification:** Pilot với ít nhất một client giả lập và một HIS/LIS thực tế.

**Dependencies:** Task 19

#### Task 21: Canary, migration và deprecation

**Mô tả:** Rollout theo tenant; shadow compare v2/v3; có kill switch và rollback không mất request.

**Acceptance criteria:**
- [ ] Shadow output khớp clinical result 100% cho dataset được duyệt.
- [ ] Canary tenant đạt SLO và không có mismatch trước mở rộng.
- [ ] v1/v2 chỉ deprecate sau thời gian thông báo và telemetry xác nhận không còn client phụ thuộc.

**Verification:** Production rehearsal và rollback drill.

**Dependencies:** Task 20

#### Task 22: Nghiệm thu production

**Mô tả:** Review độc lập toàn hệ thống và ký duyệt go-live.

**Acceptance criteria:**
- [ ] Không còn finding Critical/High; Medium có owner và deadline.
- [ ] Clinical, security, privacy, operations và integration owner ký duyệt.
- [ ] Có release evidence, SBOM, backup, rollback, runbook và contact matrix.

**Verification:** Independent review, penetration test và production smoke test.

**Dependencies:** Task 21

## 6. Definition of Done

- Correctness: mọi input boundary có test; không malformed request nào gây 500; clinical fixture đạt 100%.
- Traceability: mọi response truy được tenant, client, request, result, guideline và audit.
- Security: tenant isolation; credential rotation/revoke; mTLS hoặc OAuth2; không Critical/High.
- Privacy: PHI minimization, encryption, access audit, retention và legal hold được kiểm thử.
- Interoperability: REST v3, FHIR R4 và HL7 profile qua conformance suite.
- Reliability: idempotency, retry, outbox, load/soak/chaos và DR đạt SLO/RPO/RTO.
- Delivery: CI blocking, signed artifact/SBOM, migration rehearsal, canary và rollback drill.
- Documentation: OpenAPI, implementation guides, examples, runbooks và onboarding package đầy đủ.

## 7. Rủi ro và giảm thiểu

| Rủi ro | Mức độ | Giảm thiểu |
|---|---|---|
| Sửa thời gian làm đổi kết quả lịch sử | Cao | Golden dataset, shadow compare, version engine |
| Mapping FHIR/HL7 làm mất ngữ nghĩa | Cao | Canonical model, terminology binding, round-trip tests |
| Retry tạo quyết định/audit trùng | Cao | Idempotency DB constraint, transaction, concurrency tests |
| Credential migration khóa HIS | Cao | Dual credential window, canary từng tenant, rollback |
| Audit chứa PHI ngoài chủ đích | Cao | Allowlist, encryption, automated leakage tests |
| Proxy/rate-limit cấu hình sai | Cao | Explicit trusted networks, edge tests, per-client quota |
| v3 phá client v1/v2 | Cao | Compatibility adapter, contract tests, deprecation telemetry |
| Webhook mất hoặc gửi lặp | Trung bình | Transactional outbox, retry, DLQ, consumer idempotency |
| Scope FHIR/HL7 tăng quá lớn | Trung bình | Chốt use case/profile trước; không xây generic interface engine |

## 8. Quyết định cần chủ dự án phê duyệt

- Chuẩn bắt buộc: REST-only, FHIR R4, HL7 v2 hay cả ba.
- Cơ chế production chính: OAuth2 client credentials, mTLS hay kết hợp.
- Mô hình triển khai: SaaS multi-tenant, single-tenant hoặc on-premise.
- SLO, RPO, RTO và tải mục tiêu.
- Chính sách PHI, retention, legal hold và yêu cầu pháp lý áp dụng.
- HIS/LIS pilot dùng để nghiệm thu conformance.

## 9. Thứ tự bắt buộc

`Contract baseline → Clinical governance → Correctness → Tenant/Auth → Idempotency/Audit → API v3 → FHIR/HL7 → Reliability → Sandbox → Canary → Production acceptance`

Không bắt đầu rollout trước khi Checkpoint F đạt. Không loại bỏ v1/v2 trước khi telemetry xác nhận không còn client sử dụng.
