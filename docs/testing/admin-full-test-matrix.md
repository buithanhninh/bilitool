# Admin Full Acceptance Test Matrix

## Auth & Security
| ID | Case | Expected |
|---|---|---|
| AUTH-01 | Mở `/admin` chưa login | Chuyển về login/không lộ dashboard |
| AUTH-02 | Login đúng | Vào `/admin`, có cookie Secure/HttpOnly |
| AUTH-03 | Metrics chưa login | Không trả JSON metrics |
| AUTH-04 | Metrics đã login | HTTP 200, đúng schema, không có PHI |
| AUTH-05 | Admin route đã login | Không 403/500 |
| AUTH-06 | Audit login/page view | Có actor/result/correlation ID |

## Dashboard Correctness
| ID | Case | Expected |
|---|---|---|
| DATA-01 | Version | `1.3.19` |
| DATA-02 | Test data | Không xuất hiện hồ sơ automation |
| DATA-03 | Unicode | Không có mojibake, tên Việt đúng |
| DATA-04 | Activity split | Hiển thị anonymous/authenticated, tổng số có thể thay đổi theo dữ liệu sống |
| DATA-05 | AAP categories | Đủ 5 nhóm |
| DATA-06 | Patient total | Khớp production records không-test |
| DATA-07 | Operational metrics | Có p95 và error rate |
| DATA-08 | Health | Ready, không lộ tên DB/engine |

## Navigation & Drill-down
| ID | Case | Expected |
|---|---|---|
| NAV-01 | Dashboard → doctors | Trang tải, không error |
| NAV-02 | Dashboard → patients | Trang tải, không error |
| NAV-03 | Dashboard → calculation log | Trang tải, có table/search |
| NAV-04 | Dashboard → doctor analytics | Trang tải |
| NAV-05 | Dashboard → patient analytics | Trang tải |
| NAV-06 | Back navigation | Trở lại dashboard, giữ auth |

## Responsive Matrix
- Mobile small: 360×800
- Mobile large: 430×932
- Tablet: 768×1024
- Desktop: 1440×1100
- Engines: Chromium, Firefox, WebKit

Mỗi tổ hợp phải đạt:
- Không horizontal overflow quá 2px.
- Heading và KPI không bị mất.
- Link/button có thể focus bằng bàn phím.
- Charts có text alternative hoặc `role=img`.
- Không console error và không HTTP 500.
- Dashboard không bị login redirect sau khi đã xác thực.

## Tables & Interaction
| ID | Case | Expected |
|---|---|---|
| UI-01 | Calculation search | Input nhận keyboard |
| UI-02 | Refresh calculation log | Không error |
| UI-03 | Open calculation detail | Modal/dialog hiển thị khi có record |
| UI-04 | Pagination | Nút trước/sau có trạng thái đúng |
| UI-05 | Patient table | Header và rows hiển thị |
| UI-06 | Doctor table | Header và rows hiển thị |

## Accessibility
| ID | Case | Expected |
|---|---|---|
| A11Y-01 | Một `h1` chính | Có |
| A11Y-02 | Keyboard first Tab | Focus không ở body |
| A11Y-03 | Chart semantics | Dashboard charts có `role=img` |
| A11Y-04 | Buttons | Có accessible name |
| A11Y-05 | Inputs | Có label/placeholder/name |
| A11Y-06 | Mobile touch target | Link/button chính ≥32px |

## Operations
| ID | Case | Expected |
|---|---|---|
| OPS-01 | `/health/live` | 200 |
| OPS-02 | `/health/ready` | 200 + minimal JSON |
| OPS-03 | Metrics schema | requests/errorRate/latency p50-p99 |
| OPS-04 | Error scan | Container không có unhandled exception |
| OPS-05 | Alert evaluator | Cảnh báo khi vượt ngưỡng |
| OPS-06 | Backup | File backup mới nhất tồn tại và khác 0 byte |

## Release Gate
PASS chỉ khi toàn bộ P0: AUTH-01..06, DATA-01..08, NAV-01..06, responsive 12 tổ hợp, OPS-01..04 đều đạt. Không dùng cụm “100%” nếu còn FAIL/PARTIAL/SKIP.
