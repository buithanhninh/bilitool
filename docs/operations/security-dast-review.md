# HIS/EMR Security DAST Review

## Scanner và phạm vi

- Scanner: OWASP ZAP API Scan `ghcr.io/zaproxy/zaproxy:2.17.0`.
- Target: clean synthetic sandbox, OpenAPI 3.1 document và 46 imported operations.
- Active scan: 164 URLs, API-key header và synthetic idempotency header.
- Runner: `scripts/security/run-zap-api-scan.sh`.
- Không dùng PHI production.

## Kết quả ngày 2026-08-01

- High findings: 0.
- HTTP server-error findings: 0.
- SQL injection, XSS, path traversal, command injection, XXE, SSRF/cloud metadata và common RCE rules: PASS.
- CSP missing và Subresource Integrity findings trên login page: đã xử lý bằng CSP response header và bỏ CDN khỏi standalone login page.
- Malformed FHIR từng tạo `500`: đã sửa adapter boundary validation và khóa bằng regression test trả FHIR OperationOutcome `400`.

## Finding còn lại

`Application Error Disclosure` trên `/openapi/v3.json` là false-positive của text scanner: evidence chỉ là cụm từ chuẩn `Internal Server Error` trong OpenAPI response documentation. Endpoint trả `200`, không chứa stack trace, file path, exception type hoặc runtime diagnostic. Không xóa documented `500` response khỏi API contract để né scanner.

Low/informational findings về content-type expectations và CSP source breadth phải được security owner xem lại cùng reverse proxy/TLS production. Chúng không được tự động nâng thành accepted risk; quyết định cuối nằm trong independent penetration test.

## Gate

Automated DAST chỉ là pre-production regression gate, không thay thế independent penetration test. Go-live vẫn bị chặn nếu pentest phát hiện Critical/High hoặc Medium chưa có owner và deadline.
