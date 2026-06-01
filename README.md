# BiliTool.Vn - Phần Mềm Hỗ Trợ Lâm Sàng Quản Lý Vàng Da Sơ Sinh

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![Blazor](https://img.shields.io/badge/Blazor-Server-512BD4?logo=blazor)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791?logo=postgresql)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker)

> **BiliTool.Vn** là sản phẩm được phát triển bởi **ThS.BS. Ninh Thị Phương Mai**, nhằm cung cấp phiên bản tiếng Việt hỗ trợ đắc lực cho các bác sĩ sơ sinh Việt Nam trong việc quản lý và tính toán điều trị vàng da sơ sinh, tuân thủ nghiêm ngặt **Phác đồ AAP 2022** (Pediatrics Vol.150 No.3) và **NICE CG98**.

---

## Tính năng

- ✅ **Máy tính ngưỡng bilirubin** — nhập ngày giờ hoặc tuổi theo giờ
- ✅ **Hỗ trợ 2 đơn vị** — mg/dL và μmol/L (tự chuyển đổi)
- ✅ **Đánh giá yếu tố nguy cơ thần kinh** — G6PD, tan huyết, sepsis, albumin thấp, ETCOc
- ✅ **Theo dõi xu hướng** — tính tốc độ thay đổi bilirubin theo giờ
- ✅ **Khuyến nghị lâm sàng** bằng tiếng Việt — tự động theo AAP 2022
- ✅ **Xuất báo cáo** — sao chép vào HIS/EMR bằng 1 click
- ✅ **API tích hợp HIS** — REST API cho hệ thống bệnh viện
- ✅ **Đăng nhập Gmail** hoặc **ẩn danh**
- ✅ **Responsive** — hoạt động tốt trên điện thoại bác sĩ

---

## Cấu Trúc Dự Án

```
BiliTool.Vn/
├── src/
│   ├── BiliTool.Vn.Domain/          # Business logic, engine tính toán
│   ├── BiliTool.Vn.Application/     # CQRS (MediatR), DTOs, Validators
│   ├── BiliTool.Vn.Infrastructure/  # EF Core, PostgreSQL, Auth
│   └── BiliTool.Vn.Web/             # Blazor Server UI
├── tests/
│   └── BiliTool.Vn.Domain.Tests/    # xUnit tests
├── docker/
│   ├── Dockerfile
│   └── nginx.conf
└── docker-compose.yml
```

---

## Cài Đặt & Chạy

### Yêu cầu
- .NET 8 SDK
- Docker & Docker Compose
- Tài khoản Google Cloud (để cấu hình OAuth)

### 1. Clone & Cấu hình
```bash
git clone <repo-url>
cd bilitool-vn
cp .env.example .env
# Chỉnh sửa .env — điền Google OAuth credentials
```

### 2. Chạy với Docker
```bash
docker compose up -d
```
Ứng dụng chạy tại: `http://localhost:8888` (hoặc domain của bạn cấu hình qua Nginx/Reverse Proxy)

### 3. Chạy development
```bash
cd src/BiliTool.Vn.Web
dotnet run
```

### 4. Chạy Tests
```bash
dotnet test tests/BiliTool.Vn.Domain.Tests/
```

---

## Cấu Hình Google OAuth

### Hướng dẫn kích hoạt đăng nhập Gmail:

1. **Tạo dự án Google Cloud** tại [console.cloud.google.com](https://console.cloud.google.com/)

2. **Kích hoạt Google+ API** → APIs & Services → Library → tìm "Google+ API" → Enable

3. **Tạo OAuth Client ID** → APIs & Services → Credentials → Create Credentials → OAuth 2.0 Client IDs
   - Application type: **Web application**
   - Name: `BiliTool.Vn`
   - Authorized redirect URIs (thêm cả hai):
     ```
     http://localhost:5050/signin-google
     https://your-domain.com/signin-google
     ```

4. **Điền credentials vào `appsettings.json`**:
   ```json
   "Authentication": {
     "Google": {
       "ClientId": "xxxxxxxxxxxx.apps.googleusercontent.com",
       "ClientSecret": "GOCSPX-xxxxxxxxxxxx"
     }
   }
   ```
   > ⚠️ Không commit file `appsettings.json` có thật vào git. Dùng `.env` hoặc User Secrets.

5. **Khởi động lại** ứng dụng — nút "Đăng nhập với Google" sẽ tự kích hoạt.

---

## Tích Hợp Hệ Thống HIS / Bệnh Án Điện Tử (EMR)

BiliTool.Vn cung cấp sẵn hai phương án tích hợp cho các kỹ sư IT bệnh viện:

### 1. Gọi trực tiếp RESTful API (Headless Call)
* **Endpoint:** `POST /api/v1/bilirubin/calculate`
* **Header bắt buộc:** `X-API-Key: {khoa_tich_hop}`
* **Content-Type:** `application/json`
* **Mẫu Request/Response:** Xem chi tiết ví dụ đầy đủ và giải nghĩa các trường kết quả trả về tại trang `/tich-hop-his` trên giao diện web.

**Cấu hình API Keys (Bảo mật B2B):**
Trong file `appsettings.json`, khai báo các khóa được phép gọi API (hỗ trợ đa bệnh viện/đơn vị):
```json
"ApiSettings": {
  "AllowedApiKeys": [
    "bilitool_his_integration_secure_key_2026",
    "your_custom_key_here"
  ]
}
```

### 2. Deep Link (URL Parameters)
Cho phép phần mềm EMR mở trực tiếp giao diện tính toán của BiliTool.Vn và điền sẵn thông tin bệnh nhi:
```
GET https://bilitool.vn/may-tinh?ngaysinh=15/03/2026&giosinh=08:30&ngaymau=16/03/2026&giomau=10:00&bili=12.5&donvi=MgDl&tuoithai=38&autocalc=true
```

---


## Phác Đồ Y Tế

Phần mềm tuân thủ nghiêm ngặt:
> **"Clinical Practice Guideline Revision: Management of Hyperbilirubinemia in the Newborn Infant 35 or More Weeks of Gestation"**
> American Academy of Pediatrics, Pediatrics. 2022;150(3):e2022058859

**Lưu ý quan trọng**: Phần mềm này chỉ là công cụ hỗ trợ lâm sàng và **không thay thế** phán đoán y khoa của bác sĩ.

---

## Công Nghệ

| Thành phần | Công nghệ |
|-----------|-----------|
| Framework | ASP.NET Core 8, Blazor Server |
| CQRS | MediatR 12 |
| Validation | FluentValidation 11 |
| ORM | Entity Framework Core 8 |
| Database | PostgreSQL 16 |
| Auth | Google OAuth 2.0 + Cookie |
| Charts | Chart.js 4 |
| Logging | Serilog |
| Tests | xUnit + FluentAssertions |
| Deploy | Docker Compose + Nginx |
