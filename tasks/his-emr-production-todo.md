# HIS/EMR Production — Execution Checklist

Trạng thái: `[x]` hoàn tất bằng technical evidence hoặc release-owner waiver có ghi nhận; `[-]` một phần; `[ ]` chưa thực hiện.

Quyết định release `v1.4.0` ngày `2026-08-01`: repository release owner chấp nhận production release và waive các external validation gates chưa thực hiện. Waiver là quyết định chấp nhận rủi ro, không phải bằng chứng clinical review, hospital pilot hoặc independent penetration test.

## Baseline hiện tại — 2026-08-01

- [x] Solution build: 0 warning, 0 error.
- [x] Domain/application/API/contracts/security/mTLS/FHIR/HL7/outbox/operations/PostgreSQL/capacity/chaos/rollout/alert tests: 115/115 PASS.
- [x] Fresh PostgreSQL migration chain được kiểm thử bằng Testcontainers.
- [x] Release build: 0 warning, 0 error; NuGet vulnerability scan: 0 finding.
- [x] API v1/v2, API key, rate limit và clinical audit cơ bản tồn tại.
- [x] API contract baseline khóa v1/v2/v3 success, 400/401/429/500/503 problem shapes, media types, dynamic-value allowlist, DB/security/concurrency evidence.
- [x] Production acceptance: `APPROVED WITH WAIVERS` cho release `v1.4.0`.

## Phase 0 — Baseline và governance

- [x] T1 Có source-controlled structural golden baseline cho v1/v2/v3, 200/400/401/429/500/503, field/nullability/unit/timezone catalog và CI verifier bắt breaking path/type/media changes ngoài dynamic allowlist.
- [x] T2 Metadata engine/guideline, revision, effective date, dataset revision và sign-off artifact đã tập trung; independent clinical signature được release owner waive cho `v1.4.0`.
- [x] Checkpoint A: build/tests xanh, contract baseline lưu source control, additive compatibility changes có automated verification.

## Phase 1 — Correctness

- [x] T3 Validator và handler dùng chung normalization ngày/giờ; có test cùng ngày và consistency.
- [x] T4 Đã khóa null nested object, enum, non-finite age, giờ ngoài ngày, malformed JSON, unknown-field rejection và pre-model-binding `413 request_too_large` cho giới hạn 64 KiB REST/128 KiB FHIR-HL7.
- [x] T5 Canonical request có source/patient/encounter/order/specimen/observation identifiers, DateTimeOffset và UCUM units.
- [x] Checkpoint B: normalized timestamps, strict validation, request-size policy và canonical integration DTO có automated evidence.

## Phase 2 — Identity và security

- [x] T6 Có tenant/API client schema, bootstrap provisioning, hash-only storage, scope, expiry, revoke, dual-key rotation và audited admin lifecycle API.
- [x] T7 Có hash-only API key, scoped credential, dual-key rotation/revoke/expiry, hard legacy migration deadline và optional per-client mTLS SHA-256 binding với certificate rotation overlap, audited admin lifecycle và real Kestrel TLS edge tests.
- [x] T8 Trusted proxy allowlist, forward limit, per-client fingerprint rate limit và `Retry-After`.
- [x] Checkpoint C: tenant/client identity, scoped credentials, mTLS option, trusted proxy và bounded per-client rate limiting có automated security evidence.

## Phase 3 — Idempotency và audit

- [x] T9 Có DB unique key, request hash, replay, payload conflict, in-progress guard, 5xx release, expiry purge và PostgreSQL concurrency/tenant-boundary tests.
- [x] T10 Correlation header, tenant/client/result ID, engine provenance và PHI redaction được kiểm chứng bằng PostgreSQL-backed audit integration test.
- [x] T11 Clinical/admin audit immutable qua change tracker, payload redaction, retention tối thiểu, DB-backed legal hold, audited admin lifecycle và immutable purge reports.
- [x] Checkpoint D: idempotency concurrency, audit identity/provenance, immutability, retention và legal hold có PostgreSQL evidence.

## Phase 4 — Interoperability

- [x] T12 API v3 canonical, stable error codes và machine-readable OpenAPI 3.1.1 đã có; API-key security scheme và contract test được khóa, dependency scan không có vulnerability.
- [x] T13 Có FHIR R4 IG package, canonical Bundle/extension StructureDefinitions, valid `Bundle.meta.tag` facility coding, Patient/Encounter/ServiceRequest/Specimen/Observation mapping, UCUM/LOINC checks, OperationOutcome, DiagnosticReport, CapabilityStatement và CI gate bằng HL7 FHIR Validator CLI 6.10.0 đã khóa SHA-256.
- [x] T14 Có ORU^R01 v2.5.1 parser, PID/PV1/ORC/OBR/OBX mapping, MSH-10 duplicate control, UCUM/LOINC checks, ACK AE/AA và ZBR result; external harness/pilot LIS được release owner waive cho `v1.4.0`.
- [x] T15 Có transactional audit+outbox, protected secret, HTTPS/SSRF policy, HMAC timestamp signature, lease claim/reclaim, retry backoff, dead-letter, authorized audited replay và HTTPS delivery integration test qua server thật.
- [x] Checkpoint E: REST/FHIR/HL7 threshold equivalence, sandbox conformance và generated OpenAPI TypeScript client compile/runtime đạt; external HIS/LIS pilot được release owner waive cho `v1.4.0`.

## Phase 5 — Reliability

- [x] T16 Có SLO mặc định, request/HIS event metrics, structured alerts, correlation, ActivitySource spans và readiness dependency checks.
- [x] T17 Có request deadline/cancellation, bounded rate limit, webhook timeout, bulkhead/circuit breaker, deterministic saturation/failure/half-open chaos tests, clean-volume CI smoke, production-like Docker load 12.000 requests tại 100 rps và soak 90.000 requests tại 50 rps trong 30 phút; cả hai đạt 0% lỗi, không dropped iteration, SLO latency và post-soak readiness/DB health, có SHA-256 evidence.
- [x] T18 Có backup custom dump, SHA-256 manifest, guarded restore, DR runbook, legal hold và PostgreSQL-tested purge evidence; production-like PostgreSQL 16 drill mới nhất đạt restore 3 giây/RTO 60 phút, PITR gap 0 giây/RPO 15 phút, checksum reconciliation và migration rollback/forward.
- [x] Checkpoint F: soak/load đạt SLO; alert breach/cooldown simulation, PostgreSQL restore/PITR và migration rollback/forward drill PASS; on-call matrix đủ auth, latency, DB, queue và webhook.

## Phase 6 — Release

- [x] T19 CI chạy Release build, 114 tests gồm PostgreSQL Testcontainers, REST/FHIR/HL7 contract và replay media type, generated-client runtime, validation/security negative cases, tenant/idempotency concurrency, TRX artifact, whitespace/vulnerability/FHIR gates, sustained load/soak, pinned CycloneDX SBOM, SHA-256 release manifest và GitHub provenance attestation.
- [x] T20 Có isolated Docker sandbox, synthetic fixtures, bootstrap test credential, Postman collection, automated conformance script, credential lifecycle và hospital checklist; hospital pilot được release owner waive cho `v1.4.0`.
- [x] T21 Có tenant allowlist/denylist, global/emergency kill switch, retryable rollout response, 100% REST/FHIR/HL7 shadow-threshold gate và canary/rollback/deprecation runbook; clean production rehearsal đạt conformance, generated client, load smoke, kill switch 503, rollback 200 và DB continuity; production deprecation telemetry được release owner waive cho `v1.4.0`.
- [x] T22 Có reproducible release evidence bundle, SBOM, checksums, provenance attestation workflow, backup/rollback/runbooks và OWASP ZAP active API DAST 164 URLs đạt 0 High/0 server error; independent review/pentest được release owner waive và production release được phê duyệt cho `v1.4.0`.

## Go-live gates

- [x] Không còn Critical/High security finding trong NuGet direct/transitive dependency scan.
- [x] Independent clinical signature được release owner waive cho `v1.4.0`; clinical evidence không được tuyên bố là đã thực hiện.
- [x] Tenant isolation và idempotency concurrency tests đạt 100%.
- [x] Automated REST/FHIR/HL7 conformance suite đạt; external hospital conformance được release owner waive cho `v1.4.0`.
- [x] SLO, RPO, RTO và tải mục tiêu đạt.
- [x] Credential rotation automated tests, incident alert simulation, restore/PITR và rollback rehearsal thành công; release owner phê duyệt `APPROVED WITH WAIVERS`.
- [x] Runbook, OpenAPI, implementation guide, SBOM và release evidence nội bộ đầy đủ.
