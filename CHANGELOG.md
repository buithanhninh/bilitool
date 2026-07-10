# BiliTool.Vn - Changelog


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
