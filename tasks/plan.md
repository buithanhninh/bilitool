# Implementation Plan: Admin 10/10

## Mục tiêu
Đưa toàn bộ khu vực admin BiliTool.Vn lên chuẩn production y tế: số liệu có thể truy nguyên, bảo mật theo vai trò, audit đầy đủ, quan sát được, dễ sử dụng và có quy trình phát hành/rollback an toàn.

## Nguyên tắc kiến trúc
- Dữ liệu lâm sàng là nguồn sự thật; không dùng chỉ số ước lượng hoặc công thức không được định nghĩa.
- Mỗi KPI có tên, định nghĩa, tử số, mẫu số, múi giờ, thời điểm chốt và nguồn dữ liệu.
- Tách dữ liệu test khỏi production bằng thuộc tính rõ ràng; không nhận diện qua tên.
- Audit nghiệp vụ lưu có cấu trúc, append-only, không chứa secret và hạn chế PHI.
- Quyền tối thiểu theo vai trò; hành động nhạy cảm yêu cầu xác nhận và audit.
- Mỗi phase có backup, migration rehearsal, smoke test và rollback độc lập.

## Phase 0 — Baseline và khóa phạm vi

### Task 1: Lập data dictionary admin
**Mô tả:** Liệt kê mọi KPI, nguồn bảng, quy tắc lọc, múi giờ, đơn vị và ý nghĩa lâm sàng.

**Acceptance criteria:**
- [ ] 100% KPI hiện có được định nghĩa bằng truy vấn hoặc công thức xác định.
- [ ] KPI chưa đủ dữ liệu được đánh dấu “không khả dụng”, không hiển thị 0 gây hiểu nhầm.
- [ ] Clinical reviewer phê duyệt thuật ngữ AAP và cảnh báo.

**Verification:** Review chéo DTO, handler, SQL và UI.

**Dependencies:** None

### Task 2: Tạo bộ dữ liệu chuẩn có kỳ vọng
**Mô tả:** Tạo fixture ẩn danh gồm ca bình thường, chiếu đèn, escalation-of-care, thay máu, sinh non và yếu tố nguy cơ.

**Acceptance criteria:**
- [ ] Mỗi ca có expected threshold, category và dashboard aggregate.
- [ ] Không chứa dữ liệu bệnh nhân thật.
- [ ] Fixture chạy lặp lại cho cùng kết quả.

**Verification:** Integration test PostgreSQL + clinical engine.

**Dependencies:** Task 1

### Checkpoint A
- [ ] Backup production được kiểm tra restore.
- [ ] Build sạch, baseline test chạy xanh.
- [ ] Không thay đổi hành vi production.

## Phase 1 — Tính đúng và chất lượng dữ liệu

### Task 3: Sửa liên kết hoạt động bác sĩ
**Mô tả:** Chuẩn hóa liên kết `PhienLamViec`, `LichSuTinhToan` và `HoSoNguoiDung` để activity không bị mất.

**Acceptance criteria:**
- [ ] 111 lượt tính có thể phân loại authenticated, anonymous hoặc API.
- [ ] Active doctor 7/30 ngày khớp truy vấn đối soát SQL.
- [ ] Không gán nhầm hoạt động anonymous cho bác sĩ.

**Verification:** SQL reconciliation + integration tests.

**Dependencies:** Tasks 1–2

### Task 4: Chuẩn hóa phân tầng AAP
**Mô tả:** Dùng một classifier chuẩn cho dashboard, nhật ký và hồ sơ bệnh nhi.

**Acceptance criteria:**
- [ ] Tổng Bình thường + Chiếu đèn + Escalation + Thay máu bằng tổng mẫu hợp lệ.
- [ ] Fixture chuẩn đạt 100% expected category.
- [ ] UI không dùng bilirubin tuyệt đối để kết luận nguy cơ.

**Verification:** Unit tests clinical classifier + aggregate tests.

**Dependencies:** Tasks 1–2

### Task 5: Sửa encoding và dữ liệu lỗi
**Mô tả:** Phát hiện, sửa dữ liệu mojibake và khóa đường ghi sai encoding.

**Acceptance criteria:**
- [ ] Không còn tên lỗi UTF-8 trên admin.
- [ ] Import/API từ chối chuỗi encoding không hợp lệ.
- [ ] Migration có backup và báo cáo bản ghi thay đổi.

**Verification:** Query scan + UI E2E tiếng Việt.

**Dependencies:** Task 2

### Task 6: Phân tách dữ liệu test
**Mô tả:** Thêm `DataClassification` hoặc tenant test rõ ràng cho user, patient, session và calculation.

**Acceptance criteria:**
- [ ] Dashboard production mặc định loại toàn bộ test/automation.
- [ ] Admin có filter riêng để xem test data.
- [ ] Không xóa dữ liệu thật bằng cleanup automation.

**Verification:** Seed mixed dataset + aggregate assertions.

**Dependencies:** Task 2

### Checkpoint B
- [ ] Tất cả KPI đối soát SQL đạt 100%.
- [ ] Dataset chuẩn đạt 100% expected result.
- [ ] Clinical reviewer ký duyệt.
- [ ] Deploy canary và rollback thử thành công.

## Phase 2 — Bảo mật admin và audit

### Task 7: RBAC admin
**Mô tả:** Tạo `SuperAdmin`, `ClinicalReviewer`, `Support`, `ReadOnly` với policy authorization.

**Acceptance criteria:**
- [ ] Mỗi route và command có policy rõ ràng.
- [ ] ReadOnly không sửa, khóa hoặc export dữ liệu nhạy cảm.
- [ ] Unauthorized trả 403 và được audit.

**Verification:** Authorization integration matrix.

**Dependencies:** Checkpoint B

### Task 8: MFA và quản lý phiên
**Mô tả:** Bổ sung TOTP/WebAuthn, timeout ngắn cho admin và revoke session.

**Acceptance criteria:**
- [ ] Admin bắt buộc MFA.
- [ ] Session admin hết hạn tối đa 2 giờ, revoke có hiệu lực ngay.
- [ ] Recovery codes được hash và dùng một lần.

**Verification:** E2E login, expiry, revoke và recovery.

**Dependencies:** Task 7

### Task 9: Audit trail append-only
**Mô tả:** Ghi login, logout, view patient, search, export, lock account, config change và failed authorization.

**Acceptance criteria:**
- [ ] Có actor, action, target, timestamp UTC, correlation ID, IP chuẩn hóa và result.
- [ ] Không ghi password, token hoặc payload PHI nguyên bản.
- [ ] Bản ghi audit không sửa/xóa qua ứng dụng.

**Verification:** Integration tests từng action + tamper checks.

**Dependencies:** Task 7

### Task 10: PHI minimization và retention
**Mô tả:** Mask log, giới hạn trường clinical audit và thêm retention policy.

**Acceptance criteria:**
- [ ] Log scan không thấy email, tên, token hoặc request y tế nguyên bản ngoài allowlist.
- [ ] Retention job có dry-run, report và legal hold.
- [ ] Export audit và patient data yêu cầu quyền riêng.

**Verification:** Automated secret/PHI scan + retention integration test.

**Dependencies:** Task 9

### Checkpoint C
- [ ] Security test không còn finding Critical/High.
- [ ] MFA/RBAC/audit E2E xanh.
- [ ] Có break-glass runbook và rollback auth.

## Phase 3 — Observability và độ ổn định

### Task 11: Metrics và tracing
**Mô tả:** Ghi request rate, error rate, p50/p95/p99, DB latency, calculation latency và audit-write failures.

**Acceptance criteria:**
- [ ] Metrics có route/service/result, không có PHI label.
- [ ] Correlation ID xuyên suốt web, DB, clinical engine và audit.
- [ ] Dashboard vận hành dùng số liệu thật từ metrics backend.

**Verification:** Load test + trace inspection.

**Dependencies:** Checkpoint C

### Task 12: Health và dependency checks
**Mô tả:** Tách liveness/readiness cho DB, backup freshness, disk, OAuth, email và clinical engine.

**Acceptance criteria:**
- [ ] Public health chỉ trả trạng thái tối thiểu.
- [ ] Chi tiết dependency chỉ admin/monitoring đọc được.
- [ ] Readiness fail đúng khi dependency bắt buộc hỏng.

**Verification:** Fault injection từng dependency.

**Dependencies:** Task 11

### Task 13: Alerting và runbook
**Mô tả:** Cảnh báo calculation error, audit drop, backup stale, disk pressure, latency và login attack.

**Acceptance criteria:**
- [ ] Mỗi alert có owner, severity, threshold và runbook.
- [ ] Alert test đến đúng kênh và tự resolve.
- [ ] Không alert storm khi một dependency hỏng.

**Verification:** Synthetic failure drill.

**Dependencies:** Tasks 11–12

### Checkpoint D
- [ ] SLO dashboard ≥99.9% availability.
- [ ] p95 admin load đạt mục tiêu đã chốt.
- [ ] Backup restore drill và incident drill thành công.

## Phase 4 — Dashboard chuyên nghiệp

### Task 14: KPI contract và freshness
**Mô tả:** Mỗi card hiển thị định nghĩa, nguồn, last-updated và trạng thái stale/unavailable.

**Acceptance criteria:**
- [ ] Không dùng thời gian mở trang làm thời gian cập nhật dữ liệu.
- [ ] KPI stale hiển thị cảnh báo, không giữ số cũ im lặng.
- [ ] Tooltip thuật ngữ y học được clinical reviewer duyệt.

**Verification:** Component tests + stale-data E2E.

**Dependencies:** Checkpoint D

### Task 15: Filter và drill-down nhất quán
**Mô tả:** Thêm thời gian, cơ sở, chuyên khoa, guideline version và data classification.

**Acceptance criteria:**
- [ ] Mọi KPI/chart/table dùng cùng filter context.
- [ ] URL lưu filter để chia sẻ và reload.
- [ ] Drill-down tổng cộng khớp KPI nguồn.

**Verification:** E2E filter reconciliation.

**Dependencies:** Task 14

### Task 16: Export an toàn
**Mô tả:** CSV/XLSX theo quyền, masking và audit.

**Acceptance criteria:**
- [ ] Formula injection được vô hiệu hóa.
- [ ] Export lớn chạy background, có giới hạn và expiry.
- [ ] Mọi export ghi audit đầy đủ.

**Verification:** Security tests + E2E export.

**Dependencies:** Tasks 9, 15

### Task 17: Accessibility và responsive
**Mô tả:** Hoàn thiện WCAG 2.2 AA, keyboard, screen reader và mobile/tablet.

**Acceptance criteria:**
- [ ] Axe không có Critical/Serious.
- [ ] Toàn bộ flow admin dùng được bằng bàn phím.
- [ ] Charts có bảng dữ liệu hoặc text alternative.

**Verification:** Axe, keyboard E2E, manual screen-reader pass.

**Dependencies:** Tasks 14–16

### Checkpoint E
- [ ] Desktop/tablet/mobile visual regression xanh.
- [ ] Filter, drill-down, export đối soát 100%.
- [ ] Clinical reviewer và admin owner nghiệm thu.

## Phase 5 — CI/CD và release 10/10

### Task 18: Test pyramid admin
**Mô tả:** Bổ sung unit, integration, contract, security, accessibility và E2E.

**Acceptance criteria:**
- [ ] Mọi công thức KPI có unit/integration test.
- [ ] Auth, audit, filter, export và dashboard có E2E.
- [ ] Test cố ý phá công thức làm CI đỏ.

**Verification:** Mutation/failure injection.

**Dependencies:** Checkpoint E

### Task 19: Pipeline release an toàn
**Mô tả:** Tự động backup, migration rehearsal, canary, smoke test và rollback.

**Acceptance criteria:**
- [ ] Không deploy nếu test, backup hoặc readiness fail.
- [ ] Canary production được kiểm tra trước full rollout.
- [ ] Rollback app và migration được diễn tập.

**Verification:** Staging release drill.

**Dependencies:** Task 18

### Task 20: Final acceptance
**Mô tả:** Nghiệm thu toàn diện theo ma trận 10/10.

**Acceptance criteria:**
- [ ] Correctness, Security, Observability, UX, Accessibility, Performance đều đạt gate.
- [ ] Không còn finding Critical/High; Medium có owner và deadline.
- [ ] Runbook, data dictionary, audit policy và release evidence đầy đủ.

**Verification:** Independent review + production smoke test.

**Dependencies:** Task 19

## Definition of Done 10/10
- Correctness: KPI và drill-down đối soát 100%; dataset chuẩn đạt 100%.
- Clinical safety: classifier duy nhất, versioned guideline, reviewer sign-off.
- Security: MFA, RBAC, append-only audit, không Critical/High.
- Privacy: PHI minimization, retention, export control, legal hold.
- Reliability: SLO ≥99.9%, tested backup/restore, tested rollback.
- Observability: metrics, traces, actionable alerts, correlation ID.
- Performance: p95 trong ngưỡng được chốt bằng load test.
- Accessibility: WCAG 2.2 AA, keyboard và screen reader pass.
- Delivery: CI blocking, canary, migration rehearsal và release evidence.

## Rủi ro và giảm thiểu
| Rủi ro | Mức độ | Giảm thiểu |
|---|---|---|
| Migration làm sai dữ liệu y tế | Cao | Backup, dry-run, checksum, canary, rollback script |
| KPI đổi nghĩa sau sửa | Cao | Data dictionary, version KPI, reviewer sign-off |
| Audit chứa PHI | Cao | Allowlist field, masking test, retention |
| MFA khóa toàn bộ admin | Cao | Break-glass account offline, recovery drill |
| Dashboard chậm vì query tổng hợp | Trung bình | Index, materialized aggregate, cache có freshness |
| Test data lẫn production | Cao | DataClassification bắt buộc và default exclusion |

## Thứ tự bắt buộc
`Baseline → Correctness → Security → Observability → UX → CI/CD → Final acceptance`

Không bắt đầu phase sau khi checkpoint trước chưa đạt.
