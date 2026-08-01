# Security Hardening Notes

## Đã áp dụng

- API HIS fail-closed khi thiếu API key config.
- Không trả exception message nội bộ qua API; response có `traceId`.
- OTP sinh bằng `RandomNumberGenerator`.
- Password hash compare dùng `CryptographicOperations.FixedTimeEquals`.
- Tên người dùng trong email OTP được HTML encode.
- Security headers: `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`.
- CI chạy vulnerability scan.

## Cần giữ khi mở rộng

- Không log API key, OTP, password, token.
- Không lưu auth token vào localStorage.
- Không thêm wildcard CORS nếu chưa có threat model.
- Không thêm dependency mới nếu chưa scan vulnerability.

## HIS/EMR API clients

- Production mặc định `ApiSettings:EnableLegacyApiKeys=false`.
- Credential được lưu dưới dạng SHA-256 hash và fingerprint; plaintext chỉ được truyền qua secret manager/environment khi bootstrap.
- Bootstrap một lần qua `ApiSettings:BootstrapClient:*`. Sau log thành công, xóa ngay `BootstrapClient__ApiKey` khỏi environment và redeploy.
- Mỗi client thuộc một tenant, có scope, expiry và trạng thái revoke riêng.
- Rotation giữ khóa cũ trong overlap window tối đa 7 ngày; sau đó khóa cũ tự hết hiệu lực.
- Rate limit phân vùng theo fingerprint credential, không ghi plaintext key vào log.
- Chỉ proxy/CIDR trong `ReverseProxy:KnownProxies` hoặc `ReverseProxy:KnownNetworks` được tin cậy cho `X-Forwarded-*`.
