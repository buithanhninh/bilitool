# Deployment & Health Checks

## Health endpoints

- `GET /health/live`: app process còn sống.
- `GET /health/ready`: app kết nối được PostgreSQL và clinical engine baseline sẵn sàng.

## Required environment variables

```bash
ConnectionStrings__PostgreSQL="Host=postgres;Port=5432;Database=bilitool_vn;Username=bilitool;Password=<secret>"
Authentication__Google__ClientId="<google-client-id>"
Authentication__Google__ClientSecret="<google-client-secret>"
ApiSettings__AllowedApiKeys__0="<long-random-api-key>"
```

## API key behavior

Nếu `ApiSettings:AllowedApiKeys` trống, HIS API trả `503`. Đây là fail-closed để tránh public API ngoài ý muốn.

## Audit

Mỗi calculation qua application handler cố gắng ghi vào `clinical_audit_logs`. Nếu audit DB lỗi, calculation vẫn trả kết quả; lỗi audit được log warning.

## PWA/offline

Service worker cache app shell, CSS/JS, `offline.html`, và metadata phác đồ. Vì app là Blazor Server, tính toán lâm sàng vẫn cần kết nối server để dùng engine đã kiểm định và ghi audit.
