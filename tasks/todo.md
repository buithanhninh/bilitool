# Admin 10/10 — Execution Checklist

Trạng thái: `[x]` hoàn tất và đã kiểm thử, `[-]` đã triển khai một phần, `[ ]` chưa hoàn tất hoặc cần phê duyệt ngoài hệ thống.

## Kết quả xác nhận 2026-07-11
- [x] Build solution: 0 warning, 0 error.
- [x] Domain tests: 50/50 PASS.
- [x] Production readiness: `Ready`.
- [x] Chromium/Firefox/WebKit × mobile nhỏ/mobile lớn/tablet/desktop: 12/12 PASS.
- [x] Không có HTTP 5xx, console error, tràn ngang, mất focus bàn phím trong ma trận E2E.
- [x] Hash admin kiểm thử đã được khôi phục về hash production sau kiểm thử.

## Phase 0: Baseline
- [x] T1 Data dictionary
- [x] T2 Dataset chuẩn
- [x] Checkpoint A

## Phase 1: Correctness
- [x] T3 Liên kết hoạt động bác sĩ
- [x] T4 Classifier AAP thống nhất
- [x] T5 Encoding dữ liệu
- [x] T6 Phân tách test data
- [x] Checkpoint B

## Phase 2: Security
- [-] T7 RBAC — có `AdminRead`/`AdminWrite`; chưa đủ bốn vai trò nghiệp vụ.
- [-] T8 MFA và session — session tối đa 2 giờ; chưa có MFA/recovery/break-glass.
- [-] T9 Append-only audit — đã ghi login/page view; chưa phủ toàn bộ hành động nhạy cảm.
- [-] T10 PHI và retention — đã redact và retention 180 ngày; chưa có legal hold/dry-run.
- [ ] Checkpoint C — chờ MFA, role matrix, security E2E đầy đủ.

## Phase 3: Reliability
- [-] T11 Metrics và tracing — có request/error/p50/p95/p99; chưa có distributed tracing đầy đủ.
- [-] T12 Dependency health — public readiness tối giản; chưa có dependency detail bảo vệ riêng.
- [-] T13 Alerting và runbook — evaluator hoạt động; chưa nối Slack/PagerDuty/Email/Telegram.
- [ ] Checkpoint D — chờ alert routing, restore drill và incident drill.

## Phase 4: Dashboard
- [-] T14 KPI contract và freshness — có data dictionary/KPI thật; freshness/stale state chưa đủ mọi card.
- [ ] T15 Filter và drill-down
- [ ] T16 Export an toàn
- [x] T17 Accessibility và responsive
- [ ] Checkpoint E — chờ clinical reviewer và các task 14–16.

## Phase 5: Release
- [-] T18 Test pyramid — domain + production E2E có sẵn; integration/authorization matrix chưa đủ.
- [x] T19 Safe release pipeline
- [ ] T20 Final acceptance — chờ các checkpoint ngoài hệ thống phía trên.
