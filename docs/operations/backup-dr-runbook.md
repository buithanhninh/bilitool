# PostgreSQL Backup & DR Runbook

## Mục tiêu

- RPO mục tiêu: 24 giờ với daily backup hiện tại; production hospital nên dùng WAL/PITR để đạt RPO 15 phút.
- RTO mục tiêu: 60 phút cho database production-like.
- Backup chứa PHI phải mã hóa tại storage, giới hạn IAM và có immutable retention.

## Tạo backup thủ công

```bash
DATABASE_URL='postgresql://user:password@host:5432/bilitool_vn' \
BACKUP_DIR=/secure/backups \
./scripts/operations/backup-postgres.sh
```

Script tạo PostgreSQL custom dump, SHA-256 checksum và manifest gồm timestamp, kích thước, checksum. Không truyền credential qua argument hoặc ghi vào manifest.

## Restore rehearsal

Không restore đè production. Tạo DB đích cô lập, rồi chạy:

```bash
RESTORE_DATABASE_URL='postgresql://user:password@restore-host:5432/bilitool_restore' \
CONFIRM_RESTORE=RESTORE \
./scripts/operations/restore-postgres.sh /secure/backups/bilitool-YYYYMMDDTHHMMSSZ.dump
```

Sau restore:

1. Ghi thời gian restore thực tế và so với RTO 60 phút.
2. Chạy migration bằng artifact release dự kiến.
3. Chạy `/health/ready` và REST/FHIR/HL7 smoke tests.
4. Đối chiếu số dòng `clinical_audit_logs`, `his_idempotency_records`, `his_outbox_events` và checksum fixture đã chọn.
5. Thử rollback artifact/migration theo release plan.
6. Xóa DB rehearsal theo chính sách môi trường.

Drill tự động production-like dùng PostgreSQL 16, database/volume/network cô lập, custom dump restore, checksum reconciliation, migration rollback/forward và WAL point-in-time recovery:

```bash
DR_EVIDENCE_DIR=artifacts/dr \
DR_RTO_SECONDS=3600 \
DR_RPO_SECONDS=900 \
./scripts/operations/run-postgres-dr-drill.sh
```

Script dùng `dotnet-ef` 8.0.11 đã pin trong tool manifest, không restore đè database nguồn và luôn dọn container/volume tạm. Evidence gồm backup manifest/checksum, migration logs, restore log, `dr-summary.json` và `SHA256SUMS`.

Drill mới nhất ngày 2026-08-01 đạt restore 3 giây so với RTO 3.600 giây, PITR gap 0 giây so với RPO 900 giây, checksum nguồn/restore khớp, rollback từ `20260801124937_AddHisIdempotencyResponseMetadata` về `20260801112000_AddHisMutualTlsBinding` rồi forward lại thành công. Bản ghi trước recovery target tồn tại; bản ghi sau target không tồn tại.

## Lịch và evidence

- Daily: kiểm tra backup mới, kích thước khác 0, checksum hợp lệ.
- Monthly: chạy restore tự động vào DB cô lập bằng `run-postgres-dr-drill.sh`.
- Quarterly: DR drill đầy đủ, migration forward/rollback, incident escalation.
- Evidence: manifest, checksum output, restore duration, migration logs, smoke-test result, người duyệt.

## Data lifecycle

- Clinical audit retention mặc định 180 ngày, tối thiểu 30 ngày.
- Idempotency record purge theo `ExpiresAt`.
- Trước purge phải xác nhận backup thành công.
- Legal hold lưu tại `clinical_audit_legal_holds`, áp dụng theo tenant hoặc tenant + result ID.
- `POST /admin/operations/audit/legal-holds` tạo hold; `DELETE /admin/operations/audit/legal-holds/{holdId}` release hold.
- `POST /admin/operations/audit/retention/dry-run` tạo report không xóa dữ liệu.
- Mỗi retention run lưu `clinical_audit_purge_reports` gồm cutoff, eligible, protected và deleted counts.
- Clinical/admin audit và purge report bị chặn update/delete qua EF change tracker; retention chỉ xóa clinical audit đủ hạn và không thuộc hold active.
