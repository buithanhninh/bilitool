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
