# BiliTool.Vn API v2

## Authentication

Gửi header:

```http
X-API-Key: <hospital-api-key>
Content-Type: application/json
```

## Active guideline metadata

```http
GET /api/v2/clinical/bilirubin/guidelines/active
```

Response cho biết engine hiện active và trạng thái dataset metadata.

## Calculate bilirubin

```http
POST /api/v2/clinical/bilirubin/calculate
```

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
