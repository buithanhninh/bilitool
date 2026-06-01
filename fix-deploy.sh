#!/bin/bash
# ============================================================
# BiliTool.Vn - Script fix và deploy lại server
# Chạy lệnh này trên VPS: bash fix-deploy.sh
# ============================================================
set -e

# Màu sắc terminal
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}╔══════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║   BiliTool.Vn - Fix Deploy Script        ║${NC}"
echo -e "${BLUE}╚══════════════════════════════════════════╝${NC}"
echo ""

# ── Bước 1: Kiểm tra trạng thái hiện tại ─────────────────────
echo -e "${YELLOW}[1/5] Kiểm tra trạng thái containers...${NC}"
docker ps -a --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" 2>/dev/null || true
echo ""

# ── Bước 2: Kiểm tra .env ────────────────────────────────────
echo -e "${YELLOW}[2/5] Kiểm tra file .env...${NC}"
if [ ! -f ".env" ]; then
    echo -e "${RED}  Không tìm thấy .env! Tạo từ .env.example...${NC}"
    cp .env.example .env
    echo -e "${RED}  ⚠️  Hãy cập nhật .env với thông tin thực trước khi tiếp tục!${NC}"
    echo ""
    echo "  Mở file .env và điền:"
    echo "    DB_PASSWORD=<mật_khẩu_thực>"
    echo "    GOOGLE_CLIENT_ID=YOUR_GOOGLE_CLIENT_ID"
    echo "    GOOGLE_CLIENT_SECRET=YOUR_GOOGLE_CLIENT_SECRET"
fi

# Kiểm tra placeholder
if grep -q "your-client-id" .env 2>/dev/null; then
    echo -e "${YELLOW}  ⚠️  .env có chứa placeholder Google credentials.${NC}"
    echo -e "${YELLOW}  App vẫn chạy được (ẩn danh), nhưng đăng nhập Google sẽ không hoạt động.${NC}"
fi
echo ""

# ── Bước 3: Dừng containers cũ ───────────────────────────────
echo -e "${YELLOW}[3/5] Dừng và xóa containers cũ...${NC}"
docker compose down --remove-orphans 2>/dev/null || docker-compose down --remove-orphans 2>/dev/null || true
echo -e "${GREEN}  ✓ Đã dọn dẹp${NC}"
echo ""

# ── Bước 4: Pull/Build image mới ─────────────────────────────
echo -e "${YELLOW}[4/5] Build image mới...${NC}"
if command -v docker compose &> /dev/null; then
    docker compose build --no-cache bilitool-web
    echo ""
    echo -e "${YELLOW}[5/5] Khởi động tất cả services...${NC}"
    docker compose up -d
else
    docker-compose build --no-cache bilitool-web
    echo ""
    echo -e "${YELLOW}[5/5] Khởi động tất cả services...${NC}"
    docker-compose up -d
fi
echo ""

# ── Bước 5: Kiểm tra sau khi khởi động ──────────────────────
echo -e "${YELLOW}Đợi services khởi động (15 giây)...${NC}"
sleep 15

echo ""
echo -e "${GREEN}╔══════════════════════════════════════════╗${NC}"
echo -e "${GREEN}║   Kết quả sau deploy                     ║${NC}"
echo -e "${GREEN}╚══════════════════════════════════════════╝${NC}"
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
echo ""

# Test kết nối nội bộ
echo -e "${YELLOW}Test kết nối local (port 80)...${NC}"
if curl -s -o /dev/null -w "%{http_code}" http://localhost:80/ | grep -q "200\|301\|302"; then
    echo -e "${GREEN}  ✓ Nginx port 80 → OK${NC}"
else
    HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:80/ 2>/dev/null || echo "timeout")
    echo -e "${RED}  ✗ Nginx port 80 → $HTTP_CODE${NC}"
    echo ""
    echo "  Xem log nginx:"
    docker logs bilitool-vn-nginx --tail 20
fi

echo ""
echo -e "${YELLOW}Test kết nối app (port 8080)...${NC}"
if curl -s -o /dev/null -w "%{http_code}" http://localhost:8080/ | grep -q "200\|301"; then
    echo -e "${GREEN}  ✓ Blazor app port 8080 → OK${NC}"
else
    HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:8080/ 2>/dev/null || echo "timeout")
    echo -e "${RED}  ✗ Blazor app port 8080 → $HTTP_CODE${NC}"
    echo ""
    echo "  Xem log app:"
    docker logs bilitool-vn-app --tail 30
fi

echo ""
echo -e "${GREEN}══════════════════════════════════════════${NC}"
echo -e "${GREEN}Hoàn tất! Thử truy cập https://bilitool.vn${NC}"
echo -e "${GREEN}══════════════════════════════════════════${NC}"
echo ""
echo "Lưu ý Cloudflare: Đảm bảo SSL/TLS mode là 'Full' (không phải 'Full Strict')"
echo "Truy cập: Cloudflare Dashboard → bilitool.vn → SSL/TLS → Overview"
