# BiliTool.Vn - Changelog

## [1.3.5] - 2026-07-10

### Changed
- Expanded Vietnamese FAQ from short answers into detailed clinical explanations with checklists and caution notes.
- Added FAQ topics for age-hour calculation, neurotoxicity risk factors, NICE CG98 clinical risk factors, direct evaluation triggers, normal-result interpretation, and dual AAP/NICE display rationale.
- Restyled FAQ accordion with premium cards, clearer hierarchy, high-contrast text, structured bullet blocks, and clinical note callouts.

### Verified
- Release build: 0 warnings, 0 errors.
- Domain tests: 26/26 passed.
- UI calculator smoke test passed.
- Axe WCAG smoke test: 0 violations on home, calculator result, and offline page.

## [1.3.4] - 2026-07-10

### Changed
- Restyled contact page to match the premium academic clinical UI system with consistent blue-teal palette, glass surface, and stronger contrast.
- Removed mixed English text from the Vietnamese contact heading and labels.
- Updated working-hours copy to Vietnamese 24-hour format.

### Verified
- Release build: 0 warnings, 0 errors.
- Domain tests: 26/26 passed.
- UI calculator smoke test passed.
- Axe WCAG smoke test: 0 violations on home, calculator result, and offline page.

## [1.3.3] - 2026-07-10

### Changed
- Reworked AAP/NICE risk-factor checkboxes into consistent card controls with hidden native inputs, clearer checked state, and responsive two-column layout.
- Replaced remaining visible English labels in the calculator result/chart area with Vietnamese copy.
- Bumped PWA shell cache and CSS query version so production browsers receive the newest UI immediately.

### Verified
- Release build: 0 warnings, 0 errors.
- Domain tests: 26/26 passed.
- UI calculator smoke test passed.
- Axe WCAG smoke test: 0 violations on home, calculator result, and offline page.

## [1.3.2] - 2026-07-10

### Changed
- Refined result panel hierarchy with clearer clinical status, metric cards, threshold distance cards, and recommendation layout.
- Redesigned BiliGraph nomogram with lighter academic chart surface, readable axes, legend chips, and emphasized patient marker.
- Polished AAP/NICE guidance panel into structured premium cards for better scanning and professional presentation.
- Bumped PWA shell cache and CSS query version so production browsers receive the newest UI immediately.

### Verified
- Release build: 0 warnings, 0 errors.
- Domain tests: 26/26 passed.
- UI calculator smoke test passed.
- Axe WCAG smoke test: 0 violations on home, calculator result, and offline page.

## [1.3.1] - 2026-07-10

### Added
- Premium academic UI layer with refined clinical glass cards, improved navigation polish, responsive spacing, and high-end visual depth.
- CSS-only motion and hover states with `prefers-reduced-motion` support.

### Changed
- Improved mobile navigation behavior so sidebar starts closed on small screens and opens only on user action.
- Added premium UI stylesheet to the PWA service-worker asset cache.

### Verified
- Release build: 0 warnings, 0 errors.
- Domain tests: 26/26 passed.
- UI calculator smoke test passed.
- Axe WCAG smoke test: 0 violations on home, calculator result, and offline page.
- API v2 clinical smoke test preserved baseline thresholds and `BaselineMayTinhBilirubin` engine.
- NuGet vulnerability scan: no vulnerable packages.

## [1.3.0] - 2026-07-10

### Added
- Clinical baseline guard tests to prevent bilirubin calculation drift.
- Clinical facade and trace metadata around the frozen baseline engine.
- Clinical audit log table and best-effort audit service.
- API v2 clinical bilirubin endpoint and guideline metadata endpoint.
- PWA shell, offline fallback page, and guideline metadata cache.
- Health endpoints for live/ready checks.
- CI workflow with build, test, and vulnerability scan.
- Operations, security, API v2, and release governance documentation.

### Changed
- Hardened HIS API key behavior to fail closed when keys are not configured.
- Removed unsafe default secrets from appsettings.
- Hardened OTP generation, password hash comparison, and API error responses.
- Improved WCAG contrast and accessible names across calculator flows.
- Removed duplicate security headers from Nginx.

### Verified
- Release build: 0 warnings, 0 errors.
- Domain tests: 26/26 passed.
- API v2 smoke test passed.
- UI calculator deep-link smoke test passed.
- Axe WCAG smoke test: 0 violations on home, calculator result, and offline page.
- NuGet vulnerability scan: no vulnerable packages.

Tất cả các thay đổi đáng chú ý của dự án sẽ được ghi nhận tại tệp này.

## [v1.2.0 - Dual-Protocol Clinical Engine] - 2026-05-03

### 🚀 Tính Năng Mới & Thuật Toán (Core Engine)
- **Kiến trúc Đa phác đồ (Dual-Protocol):** Tích hợp thành công và chạy song song cả hai phác đồ lâm sàng hàng đầu thế giới: **AAP 2022** (Hoa Kỳ) và **NICE CG98** (Vương quốc Anh).
- **Phát hiện Vàng da kéo dài (Prolonged Jaundice):** Mở rộng hệ thống để hỗ trợ bệnh nhi lên đến 672 giờ tuổi (28 ngày) nhằm phát hiện và cảnh báo vàng da kéo dài ở trẻ sinh non (<37 tuần) và trẻ đủ tháng (≥37 tuần) theo tiêu chuẩn NICE §1.7.1.
- **Tiêu chuẩn "Intensify Phototherapy":** Cập nhật thuật toán tính toán ngưỡng chiếu đèn tích cực dựa trên điểm giới hạn của NICE §1.4.9 (nằm giữa ngưỡng chiếu đèn và thay máu).
- **Hệ thống An toàn Bệnh nhân:** Tự động so sánh chéo kết quả giữa AAP và NICE, hệ thống sẽ đề xuất ngưỡng can thiệp thấp hơn (an toàn hơn) nhằm đảm bảo tối đa an toàn cho bệnh nhi.

### 🎨 Cập Nhật Giao Diện (UI/UX)
- **Biểu đồ BiliGraph Thresholds (Nomogram):** 
  - Khai tử giao diện "Progress bar" tĩnh cũ.
  - Xây dựng component `BiliNomogramChart.razor` dựng biểu đồ tuyến tính bằng SVG tốc độ cao, hỗ trợ vẽ chính xác đường cong quang phổ của 3 mức độ (Chiếu đèn, Chiếu đèn tích cực, Thay máu) chạy dài theo thời gian.
  - Tự động co giãn trục hoành (X-axis) linh hoạt từ 0 -> 336+ giờ.
  - Tối ưu hóa tính tương thích Responsive (co giãn tự động `max-width: 100%` cho component đồ thị), khắc phục tình trạng tràn chiều ngang (overflow-x) gây lỗi hiển thị trên màn hình nhỏ hoặc bị cắt cúp khi xuất file báo cáo PDF.
  - Fix triệt để lỗi "Culture Format" trên server Linux khiến dấu thập phân bị lỗi hiển thị zigzag, nay đồ thị vẽ chuẩn xác nội suy theo `CultureInfo.InvariantCulture`.
- **Trang Căn cứ khoa học (Giới thiệu & Hướng dẫn):**
  - Viết lại toàn bộ nội dung `/gioi-thieu` và panel hướng dẫn trên trang tính toán.
  - Gắn đầy đủ liên kết tài nguyên y khoa gốc đến trang của Viện Nhi khoa Hoa Kỳ (AAP) và Viện Y tế Quốc gia Anh (NICE).
  - Trình bày bổ sung các hướng dẫn cận lâm sàng mới từ NICE (Chỉ định IVIG, theo dõi Rebound sau ngưng chiếu đèn).

### 🛠 Kỹ thuật & Hạ Tầng
- **Backend Service:** Thêm cấu trúc `ChartDataPoint` vào `KetQuaTinhToanDto` cho phép Application Layer chủ động sinh điểm tọa độ trước khi đẩy ra UI.
- Cập nhật cấu hình Docker và tái triển khai (Rebuild & Redeploy) hệ thống `bilitool-web` lên môi trường Production.
