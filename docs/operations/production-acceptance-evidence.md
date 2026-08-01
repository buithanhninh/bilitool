# HIS/EMR Production Acceptance Evidence Index

## Reproducible technical evidence

| Area | Command / artifact | Current result |
|---|---|---|
| Unit/integration/contract | `dotnet test tests/BiliTool.Vn.Domain.Tests/BiliTool.Vn.Domain.Tests.csproj --no-restore` | 115/115 PASS |
| Release build | `dotnet build BiliTool.Vn.sln --no-restore --configuration Release` | 0 warning, 0 error |
| Dependency security | `dotnet list /root/bilitool/BiliTool.Vn.sln package --vulnerable --include-transitive` | 0 vulnerable package |
| FHIR R4 | `scripts/fhir/validate-r4.sh` | Official validator PASS |
| REST/FHIR/HL7 sandbox | `scripts/sandbox/run-conformance.sh` | PASS |
| Generated client | `scripts/sandbox/run-generated-client.sh` | Strict compile + runtime PASS |
| Load/soak | `scripts/reliability/run-load-soak.sh` | 100 rps load và 30-minute soak đạt SLO |
| DR/PITR | `scripts/operations/run-postgres-dr-drill.sh` | Restore 3s, RPO gap 0s, rollback/forward PASS |
| Production rehearsal | `scripts/release/run-production-rehearsal.sh` | Kill switch 503, rollback 200, DB continuity PASS |
| Automated DAST | `scripts/security/run-zap-api-scan.sh` | 0 High, 0 server error |
| Release package | `scripts/release/build-evidence.sh` | Artifact, SBOM, manifest và SHA-256 |

## Release decision

- Release: `v1.4.0`
- Decision date: `2026-08-01`
- Decision: `APPROVED WITH WAIVERS`
- Authority: authenticated repository release owner instruction to publish latest production release.
- Scope: external clinical signature, hospital HIS/LIS pilot, independent penetration test, production deprecation telemetry và remaining owner signatures are waived for this release.
- Integrity statement: waiver accepts residual release risk; it does not assert that waived external activities occurred or passed.

## Waived external acceptance

Các mục sau chưa có independent evidence tại thời điểm release. Release owner đã waive chúng cho `v1.4.0`; không được diễn giải waiver thành evidence hoàn thành:

- Clinical reviewer: hoàn tất `docs/clinical-governance/reviewer-signoff.md` và cập nhật release manifest.
- Hospital integration owner: chạy pilot HIS/LIS thật, lưu ACK/OperationOutcome/REST evidence đã khử PHI.
- Security assessor: independent penetration test, xác nhận Critical/High bằng 0; Medium có owner và deadline.
- Privacy owner: xác nhận retention, legal hold, PHI minimization, access/audit và data-processing obligations.
- Operations owner: xác nhận on-call roster, escalation contacts, backup ownership, RPO/RTO và incident authority.
- Product/release owner: phê duyệt canary tenant, rollback authority, maintenance window và production acceptance.
- Deprecation owner: cung cấp telemetry production chứng minh không còn active v1/v2 client trước sunset.

## Signature record

| Role | Name | Organization | Decision | Date UTC | Evidence reference |
|---|---|---|---|---|---|
| Clinical reviewer | Not provided | Not provided | Waived by release owner | 2026-08-01 | Release decision above |
| Hospital integration owner | Not provided | Not provided | Waived by release owner | 2026-08-01 | Release decision above |
| Independent security assessor | Not provided | Not provided | Waived by release owner | 2026-08-01 | Release decision above |
| Privacy owner | Not provided | Not provided | Waived by release owner | 2026-08-01 | Release decision above |
| Operations owner | Not provided | Not provided | Waived by release owner | 2026-08-01 | Release decision above |
| Product/release owner | Repository owner | `buithanhninh/bilitool` | APPROVED WITH WAIVERS | 2026-08-01 | Explicit release instruction |

Production acceptance cho `v1.4.0` đạt theo quyết định `APPROVED WITH WAIVERS`. Independent acceptance vẫn phải được thực hiện sau release nếu tổ chức triển khai yêu cầu regulatory, clinical hoặc contractual assurance.
