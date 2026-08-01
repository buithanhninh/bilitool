# Clinical Safety & Release Governance

## Nguyên tắc bất biến

- `MayTinhBilirubin` là baseline engine đang phục vụ production.
- Không chỉnh threshold/công thức khi chưa có golden diff report và duyệt lâm sàng.
- `BilirubinClinicalFacade` chỉ bọc engine, tạo trace/audit, không thay đổi quyết định.
- Dataset metadata hiện ở chế độ `shadow-metadata-only`; không dùng để ra quyết định.

## Quy trình thay đổi công thức/phác đồ

1. Tạo issue mô tả nguồn thay đổi phác đồ.
2. Thêm hoặc cập nhật golden cases.
3. Chạy `dotnet test BiliTool.Vn.sln --no-build`.
4. So sánh output cũ/mới theo sai số đã duyệt.
5. Bác sĩ reviewer duyệt diff report.
6. Cập nhật `wwwroot/datasets/clinical-guidelines.json` và changelog.
7. Release có rollback plan.

## Rollback

- Tắt dataset engine nếu sau này bật: `ClinicalEngine__UseDatasetEngine=false`.
- API v1 luôn giữ nguyên route `/api/v1/bilirubin/calculate`.
- REST v3/FHIR/HL7 có tenant allowlist/denylist và emergency kill switch tại `HisRollout`.
- Không drop audit/outbox/legal-hold tables khi rollback; dữ liệu traceability phải giữ nguyên.
- Migration rollback chỉ chạy sau rehearsal trên bản restore production-like và xác nhận không mất dữ liệu.

## Checklist release

- [ ] `dotnet restore BiliTool.Vn.sln`
- [ ] `dotnet build BiliTool.Vn.sln --no-restore --configuration Release`
- [ ] `dotnet test BiliTool.Vn.sln --no-build --configuration Release`
- [ ] `dotnet list /root/bilitool/BiliTool.Vn.sln package --vulnerable --include-transitive`
- [ ] Không có secret trong `appsettings.json` hoặc git diff.
- [ ] Nếu thay clinical path, baseline/golden tests pass.
- [ ] Backup DB trước deploy production.
- [ ] `RELEASE_VERSION=<version> ./scripts/release/build-evidence.sh`
- [ ] `SHA256SUMS` được verify, `sbom.cdx.json` parse hợp lệ và `release-manifest.json` đúng commit.
- [ ] GitHub build provenance attestation tồn tại cho artifact phát hành từ `main`.
