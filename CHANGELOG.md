# BiliTool.Vn - Changelog

## [1.3.16] - 2026-07-11

### Changed
- Rebuilt responsive layout foundations for 320px–1440px viewports with zero page-level horizontal overflow.
- Versioned every application stylesheet so CDN and browser caches receive responsive fixes immediately.
- Improved mobile navigation, footer, cookie consent, clinical guidance tables, and HIS/EMR technical documentation.
- Added consistent 44px touch targets and reduced-motion behavior for key controls.

### Accessibility
- Added valid names and expanded-state semantics to sidebar, language, and mobile navigation controls.
- Made horizontally scrollable clinical tables, endpoints, and code regions keyboard accessible.
- Fixed sign-in divider contrast to meet WCAG 2.2 AA.

### Testing
- Added a 55-case Playwright + axe quality sweep covering 11 public pages at five responsive breakpoints.
- Quality gate now fails on horizontal overflow, browser console errors, HTTP failures, or serious/critical WCAG violations.

## [1.3.15] - 2026-07-11
### Added
- Added smart language detection using the priority order: user language cookie, query string, browser `Accept-Language`, then Cloudflare/IP country headers.
- Added unit tests for browser-language and country-code detection across Vietnamese, English, and French mappings.

### Changed
- Bumped cache/version metadata to `1.3.15`.

## [1.3.14] - 2026-07-10
### Changed
- Localized the bilirubin result header and nomogram/chart labels for English and French.
- Extended English and French Playwright language sweeps to include an auto-calculated result view.
- Bumped cache/version metadata to `1.3.14`.

## [1.3.13] - 2026-07-10
### Added
- Added a Playwright French-language sweep test covering all public pages after switching the UI to French.

### Changed
- Added complete French content branches for `/huong-dan` and `/tich-hop-his`.
- Localized French reference titles on the calculator guidance panel and `/gioi-thieu`.
- Bumped cache/version metadata to `1.3.13`.

## [1.3.12] - 2026-07-10
### Added
- Added a Playwright English-language sweep test covering all public pages after switching the UI to English.

### Changed
- Added complete English content branches for `/huong-dan` and `/tich-hop-his`.
- Localized English SEO metadata and advisor display text on `/gioi-thieu`.
- Bumped cache/version metadata to `1.3.12`.

## [1.3.11] - 2026-07-10
### Added
- Rebuilt `/huong-dan` as a detailed clinical guidance page for neonatal jaundice using AAP 2022 and NICE CG98 references.
- Added modern clinical UI sections for workflow, neurotoxicity risks, intervention levels, NICE monitoring, red flags, AAP quick tables, and source references.

### Changed
- Bumped cache/version metadata to `1.3.11`.

## [1.3.10] - 2026-07-10

### Changed
- Added a new cache-safe Open Graph image URL `og-bilitool-1200x630.png` so crawlers and social platforms do not reuse the older immutable cached image.
- Updated Open Graph, Twitter, JSON-LD, sitemap image, and service worker references to the new 1200×630 PNG asset.
- Bumped cache/version metadata to `1.3.10`.

### Verified
- Release build: 0 warnings, 0 errors.
- Domain tests: 26/26 passed.
- SEO smoke test passed with versioned favicon/manifest, canonical, description, and JSON-LD.
- UI calculator smoke test passed.
- Axe WCAG smoke test: 0 violations on home, calculator result, and offline page.
- New Open Graph image URL returns HTTP 200 as `image/png` and is a valid 1200×630 PNG.
- Vulnerability scan: no vulnerable NuGet packages.

## [1.3.9] - 2026-07-10

### Changed
- Added site-wide structured data (`Organization`, `WebSite`, `SoftwareApplication`) for clearer brand/entity understanding by search engines.
- Rebuilt Open Graph image as a true 1200×630 PNG and added it to service worker precache.
- Improved sitemap with `lastmod`, image sitemap metadata, privacy policy URL, and removed noindex-only legal page from the sitemap.
- Hardened `robots.txt` by disallowing API/private routes while explicitly allowing crawlable static brand assets.
- Consolidated calculator page `HeadContent` so description, canonical, Open Graph, and Twitter metadata render reliably with JSON-LD.
- Bumped cache/version metadata to `1.3.9`.

### Verified
- Release build: 0 warnings, 0 errors.
- Domain tests: 26/26 passed.
- SEO smoke test passed: title, description, canonical, favicon, manifest, `Organization`, `WebSite`, and `SoftwareApplication` JSON-LD.
- UI calculator smoke test passed.
- Axe WCAG smoke test: 0 violations on home, calculator result, and offline page.
- Sitemap XML and manifest JSON parse successfully.
- Open Graph image is a valid 1200×630 PNG served as `image/png`.
- Vulnerability scan: no vulnerable NuGet packages.

## [1.3.8] - 2026-07-10

### Changed
- Added a real BiliTool.Vn favicon set, Apple touch icon, and PWA icon set so browser tabs and installed shortcuts show the correct software logo.
- Standardized browser/PWA application names to `BiliTool.Vn` and added a default fallback title.
- Updated manifest metadata, theme color, cache busting, and service worker precache entries for the new icon assets.

### Verified
- Release build: 0 warnings, 0 errors.
- Domain tests: 26/26 passed.
- Browser title, application name, favicon, Apple touch icon, and manifest smoke test passed.
- UI calculator smoke test passed.
- Axe WCAG smoke test: 0 violations on home, calculator result, and offline page.
- Favicon and PWA icon assets return HTTP 200 with correct image content types.
- Vulnerability scan: no vulnerable NuGet packages.

## [1.3.7] - 2026-07-10

### Changed
- Reworked HIS/EMR integration documentation to match the actual Deep Link query contract, API routes, DTO fields, enum values, validation limits, response groups, and error handling.
- Added explicit notes that Deep Link does not support risk-factor transfer and full risk-factor integration requires REST API.
- Clarified API key behavior, rate limiting, `donViDo` numeric enum values, `trangThaiChieuDen`, and the `1–336` hour API validation range.
- Restyled the HIS/EMR page with high-contrast clinical documentation cards, tables, callouts, and code blocks.

### Verified
- Release build: 0 warnings, 0 errors.
- Domain tests: 26/26 passed.
- HIS/EMR page desktop/mobile smoke test passed.
- UI calculator smoke test passed.
- API without `X-API-Key` returns 401 as expected.
- Axe WCAG smoke test: 0 violations on home, calculator result, and offline page.
- Vulnerability scan: no vulnerable NuGet packages.

## [1.3.6] - 2026-07-10

### Changed
- Redesigned the About page with a premium clinical-academic hero, glass cards, and stronger visual hierarchy.
- Improved typography, spacing rhythm, protocol badges, scientific references, and clinical advisor profile presentation.
- Added subtle motion with reduced-motion support for accessible polish.
- Refactored localized About page markup to reduce repeated layout code across languages.

### Verified
- Release build: 0 warnings, 0 errors.
- Domain tests: 26/26 passed.
- About page desktop/mobile smoke test passed.
- UI calculator smoke test passed.
- Axe WCAG smoke test: 0 violations on home, calculator result, and offline page.
- Vulnerability scan: no vulnerable NuGet packages.

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
