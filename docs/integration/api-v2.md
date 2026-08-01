# BiliTool.Vn API v2

## Authentication

Gửi header:

```http
X-API-Key: <hospital-api-key>
Idempotency-Key: <unique-key-for-this-clinical-request>
Content-Type: application/json
```

## Active guideline metadata

```http
GET /api/v2/clinical/bilirubin/guidelines/active
```

Response cho biết engine thực tế, `engineVersion`, chế độ dataset nhúng và trạng thái external dataset engine. Metadata dùng chung với clinical audit để tránh sai lệch giữa tài liệu, response và trace.

## Calculate bilirubin

```http
POST /api/v2/clinical/bilirubin/calculate
```

`Idempotency-Key` bắt buộc cho endpoint calculate. Retry cùng key và cùng payload trả lại đúng response trước đó với header `Idempotency-Replayed: true`. Cùng key nhưng payload khác trả `409 Conflict`.

Request dùng cùng DTO với API v1 để không tạo rủi ro mapping mới trong giai đoạn đầu.

Response v2 bọc kết quả legacy cùng metadata:

- `resultId`
- `guideline`
- `patientContext`
- `thresholds`
- `recommendation`
- `legacyResult`

## Compatibility

- API v1 `/api/v1/bilirubin/calculate` giữ nguyên cho HIS hiện tại.
- API v2 chạy song song, không thay công thức.
- `legacyResult` trong v2 giúp hệ thống cũ migrate dần.
