using BiliTool.Vn.Application.DTOs;
using BiliTool.Vn.Application.Queries;
using BiliTool.Vn.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

using BiliTool.Vn.Application.Commands;
using BiliTool.Vn.Domain.Clinical.Bilirubin;

namespace BiliTool.Vn.Infrastructure.Handlers;

public class AdminHandlers : 
    IRequestHandler<GetThongKeHeThongQuery, ThongKeHeThongDto>,
    IRequestHandler<GetDanhSachTaiKhoanQuery, List<TaiKhoanAdminDto>>,
    IRequestHandler<GetDanhSachBenhNhanAdminQuery, List<BenhNhanAdminDto>>,
    IRequestHandler<KhoaTaiKhoanCommand, bool>,
    IRequestHandler<GetThongKeBieuDoAdminQuery, ThongKeBieuDoAdminDto>,
    IRequestHandler<LayChiTietTaiKhoanAdminQuery, TaiKhoanDetailAdminDto?>,
    IRequestHandler<LayChiTietBenhNhanAdminQuery, HoSoBenhNhanDto?>,
    IRequestHandler<GetThongKeBacSiQuery, ThongKeBacSiAdminDto>,
    IRequestHandler<GetThongKeBenhNhanQuery, ThongKeBenhNhanAdminDto>,
    IRequestHandler<GetNhatKyTinhToanQuery, PagedResultDto<NhatKyTinhToanAdminDto>>
{
    private readonly BiliToolDbContext _db;

    public AdminHandlers(BiliToolDbContext db) => _db = db;

    public async Task<ThongKeHeThongDto> Handle(GetThongKeHeThongQuery request, CancellationToken cancellationToken)
    {
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        
        return new ThongKeHeThongDto
        {
            TongNguoiDung = await _db.HoSoNguoiDung.CountAsync(cancellationToken),
            NguoiDungMoiTrongThang = await _db.HoSoNguoiDung.CountAsync(x => x.NgayTao >= thirtyDaysAgo, cancellationToken),
            
            TongBenhNhan = await _db.HoSoBenhNhan.CountAsync(x => !x.IsTestData, cancellationToken),
            BenhNhanMoiTrongThang = await _db.HoSoBenhNhan.CountAsync(x => !x.IsTestData && x.NgayTao >= thirtyDaysAgo, cancellationToken),
            
            TongPhienTinhToan = await _db.LichSuTinhToan.CountAsync(cancellationToken),
            PhienTinhToanTrongThang = await _db.LichSuTinhToan.CountAsync(x => x.NgayTinhToan >= thirtyDaysAgo, cancellationToken),
            PhienTinhToanAnDanhTrongThang = await _db.LichSuTinhToan.CountAsync(x => x.NgayTinhToan >= thirtyDaysAgo && (x.Phien == null || x.Phien.NguoiDungId == null), cancellationToken),
            PhienTinhToanDaDangNhapTrongThang = await _db.LichSuTinhToan.CountAsync(x => x.NgayTinhToan >= thirtyDaysAgo && x.Phien != null && x.Phien.NguoiDungId != null, cancellationToken)
        };
    }

    public async Task<List<TaiKhoanAdminDto>> Handle(GetDanhSachTaiKhoanQuery request, CancellationToken cancellationToken)
    {
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        
        var query = from u in _db.HoSoNguoiDung
                    select new TaiKhoanAdminDto
                    {
                        Id = u.Id,
                        HoTen = u.HoTen,
                        Email = u.Email,
                        ChuyenKhoa = u.ChuyenKhoa,
                        DonViCongTac = u.DonViCongTac,
                        SoDienThoai = u.SoDienThoai,
                        NgayTao = u.NgayTao,
                        NgayDangNhapCuoi = u.NgayDangNhapCuoi,
                        IsActive = u.IsActive,
                        
                        TongBenhNhan = _db.HoSoBenhNhan.Count(b => b.NguoiDungId == u.Id),
                        TongPhienTinhToan = _db.LichSuTinhToan.Count(l => l.Phien != null && l.Phien.NguoiDungId == u.Id),
                        TinhToanTrong30NgayQua = _db.LichSuTinhToan.Count(l => l.Phien != null && l.Phien.NguoiDungId == u.Id && l.NgayTinhToan >= thirtyDaysAgo)
                    };
                    
        return await query.OrderByDescending(x => x.NgayDangNhapCuoi).ToListAsync(cancellationToken);
    }

    public async Task<List<BenhNhanAdminDto>> Handle(GetDanhSachBenhNhanAdminQuery request, CancellationToken cancellationToken)
    {
        var query = from b in _db.HoSoBenhNhan.Where(x => !x.IsTestData)
                    join u in _db.HoSoNguoiDung on b.NguoiDungId equals u.Id into userJoin
                    from u in userJoin.DefaultIfEmpty()
                    select new BenhNhanAdminDto
                    {
                        Id = b.Id,
                        HoTenBenhNhan = b.HoTenBenhNhan,
                        NgayGioSinh = b.NgayGioSinh,
                        TuoiThaiTuan = b.TuoiThaiTuan,
                        CoNguyCoThanKinh = b.CoNguyCoThanKinh,
                        NgayTao = b.NgayTao,
                        TenBacSi = u != null ? u.HoTen : "Không rõ",
                        EmailBacSi = u != null ? u.Email : "",
                        SoLanXetNghiem = _db.XetNghiemBilirubin.Count(x => x.BenhNhanId == b.Id)
                    };
                    
        return await query.OrderByDescending(x => x.NgayTao).ToListAsync(cancellationToken);
    }

    public async Task<bool> Handle(KhoaTaiKhoanCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.HoSoNguoiDung.FindAsync(new object[] { request.Id }, cancellationToken);
        if (user == null) return false;
        
        user.IsActive = request.TrangThaiKhoaMoi;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ThongKeBieuDoAdminDto> Handle(GetThongKeBieuDoAdminQuery request, CancellationToken cancellationToken)
    {
        var thirtyDaysAgo = DateTime.UtcNow.Date.AddDays(-30);
        var result = new ThongKeBieuDoAdminDto();

        // Helpers to group by date
        var tkGroups = await _db.HoSoNguoiDung
            .Where(x => x.NgayTao >= thirtyDaysAgo)
            .GroupBy(x => x.NgayTao.Date)
            .Select(g => new { Ngay = g.Key, SoLuong = g.Count() })
            .ToListAsync(cancellationToken);

        var bnGroups = await _db.HoSoBenhNhan
            .Where(x => x.NgayTao >= thirtyDaysAgo)
            .GroupBy(x => x.NgayTao.Date)
            .Select(g => new { Ngay = g.Key, SoLuong = g.Count() })
            .ToListAsync(cancellationToken);

        var ltGroups = await _db.LichSuTinhToan
            .Where(x => x.NgayTinhToan >= thirtyDaysAgo)
            .GroupBy(x => x.NgayTinhToan.Date)
            .Select(g => new { Ngay = g.Key, SoLuong = g.Count() })
            .ToListAsync(cancellationToken);

        // Fill 30 days securely
        for (int i = 0; i < 30; i++)
        {
            var d = DateTime.UtcNow.Date.AddDays(-29 + i);
            result.TaiKhoanMoi30Ngay.Add(new ThongKeTheoNgayDto { Ngay = d.ToString("dd/MM"), SoLuong = tkGroups.FirstOrDefault(x => x.Ngay == d)?.SoLuong ?? 0 });
            result.BenhNhanMoi30Ngay.Add(new ThongKeTheoNgayDto { Ngay = d.ToString("dd/MM"), SoLuong = bnGroups.FirstOrDefault(x => x.Ngay == d)?.SoLuong ?? 0 });
            result.LuotTinhToan30Ngay.Add(new ThongKeTheoNgayDto { Ngay = d.ToString("dd/MM"), SoLuong = ltGroups.FirstOrDefault(x => x.Ngay == d)?.SoLuong ?? 0 });
        }

        // Tỉ lệ mức độ 7 ngày qua
        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
        var xetNghiems = await _db.XetNghiemBilirubin
            .Where(x => x.ThoiGianLayMau >= sevenDaysAgo)
            .Select(x => new { x.BilirubinMgDl, x.NguongChieuDen, x.NguongThayCuuMau })
            .ToListAsync(cancellationToken);

        var categories = xetNghiems
            .Select(x => ClinicalInterventionClassifier.Classify(x.BilirubinMgDl, x.NguongChieuDen, x.NguongThayCuuMau))
            .ToList();
        var total = categories.Count;
        if (total > 0)
        {
            var binhThuong = categories.Count(x => x == ClinicalInterventionCategory.BinhThuong);
            var canChieuDen = categories.Count(x => x == ClinicalInterventionCategory.ChieuDen);
            var escalation = categories.Count(x => x == ClinicalInterventionCategory.EscalationOfCare);
            var canThayMau = categories.Count(x => x == ClinicalInterventionCategory.ThayMau);
            var khongDuDuLieu = categories.Count(x => x == ClinicalInterventionCategory.KhongDuDuLieu);

            result.TiLeChieuDen = (int)Math.Round((double)canChieuDen / total * 100);
            result.TiLeEscalation = (int)Math.Round((double)escalation / total * 100);
            result.TiLeThayMau = (int)Math.Round((double)canThayMau / total * 100);
            result.TiLeKhongDuDuLieu = (int)Math.Round((double)khongDuDuLieu / total * 100);
            result.TiLeBinhThuong = (int)Math.Round((double)binhThuong / total * 100);
        }

        return result;
    }

    public async Task<TaiKhoanDetailAdminDto?> Handle(LayChiTietTaiKhoanAdminQuery request, CancellationToken cancellationToken)
    {
        var u = await _db.HoSoNguoiDung.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (u == null) return null;

        var thirtyDaysAgo = DateTime.UtcNow.Date.AddDays(-30);

        var tkDto = new TaiKhoanAdminDto
        {
            Id = u.Id,
            HoTen = u.HoTen,
            Email = u.Email,
            ChuyenKhoa = u.ChuyenKhoa,
            DonViCongTac = u.DonViCongTac,
            SoDienThoai = u.SoDienThoai,
            NgayTao = u.NgayTao,
            NgayDangNhapCuoi = u.NgayDangNhapCuoi,
            IsActive = u.IsActive,
            TongBenhNhan = await _db.HoSoBenhNhan.CountAsync(b => b.NguoiDungId == u.Id, cancellationToken),
            TongPhienTinhToan = await _db.LichSuTinhToan.CountAsync(l => l.Phien != null && l.Phien.NguoiDungId == u.Id, cancellationToken),
        };

        var dsBenhNhan = await _db.HoSoBenhNhan
            .Where(b => b.NguoiDungId == u.Id)
            .Select(b => new BenhNhanAdminDto
            {
                Id = b.Id,
                HoTenBenhNhan = b.HoTenBenhNhan,
                NgayGioSinh = b.NgayGioSinh,
                TuoiThaiTuan = b.TuoiThaiTuan,
                CoNguyCoThanKinh = b.CoNguyCoThanKinh,
                NgayTao = b.NgayTao,
                TenBacSi = u.HoTen,
                SoLanXetNghiem = _db.XetNghiemBilirubin.Count(x => x.BenhNhanId == b.Id)
            })
            .OrderByDescending(x => x.NgayTao)
            .ToListAsync(cancellationToken);

        // Lấy heatmap hoạt động (tính toán)
        var ltGroups = await _db.LichSuTinhToan
            .Where(x => x.Phien != null && x.Phien.NguoiDungId == request.Id && x.NgayTinhToan >= thirtyDaysAgo)
            .GroupBy(x => x.NgayTinhToan.Date)
            .Select(g => new { Ngay = g.Key, SoLuong = g.Count() })
            .ToListAsync(cancellationToken);

        var hoatDongDays = new List<ThongKeTheoNgayDto>();
        for (int i = 0; i < 30; i++)
        {
            var d = DateTime.UtcNow.Date.AddDays(-29 + i);
            hoatDongDays.Add(new ThongKeTheoNgayDto { Ngay = d.ToString("dd/MM"), SoLuong = ltGroups.FirstOrDefault(x => x.Ngay == d)?.SoLuong ?? 0 });
        }

        return new TaiKhoanDetailAdminDto
        {
            ThongTinChung = tkDto,
            DanhSachBenhNhan = dsBenhNhan,
            HoatDong30Ngay = hoatDongDays
        };
    }

    public async Task<HoSoBenhNhanDto?> Handle(LayChiTietBenhNhanAdminQuery request, CancellationToken cancellationToken)
    {
        var h = await _db.HoSoBenhNhan
            .Include(x => x.DsXetNghiem.OrderByDescending(xn => xn.ThoiGianLayMau))
            .FirstOrDefaultAsync(h => h.Id == request.BenhNhanId, cancellationToken);

        if (h == null) return null;

        return new HoSoBenhNhanDto
        {
            Id = h.Id,
            HoTenBenhNhan = h.HoTenBenhNhan,
            NgayGioSinh = h.NgayGioSinh,
            TuoiThaiTuan = h.TuoiThaiTuan,
            CoNguyCoThanKinh = h.CoNguyCoThanKinh,
            AnhChiBiVangDaCanChieuDen = h.AnhChiBiVangDaCanChieuDen,
            MeBuMeHoanToan = h.MeBuMeHoanToan,
            VangDaTrong24hDau = h.VangDaTrong24hDau,
            BenhTanHuyetRh = h.BenhTanHuyetRh,
            BenhTanHuyetABO = h.BenhTanHuyetABO,
            GhiChu = h.GhiChu,
            NgayTao = h.NgayTao,
            DsXetNghiem = h.DsXetNghiem.Select(x => new XetNghiemBilirubinDto
            {
                Id = x.Id,
                BenhNhanId = x.BenhNhanId,
                ThoiGianLayMau = x.ThoiGianLayMau,
                BilirubinMgDl = x.BilirubinMgDl,
                TuoiGioTuDong = x.TuoiGioTuDong,
                MucDoNguyHiem = x.MucDoNguyHiem,
                NguongChieuDen = x.NguongChieuDen,
                NguongThayCuuMau = x.NguongThayCuuMau,
                NgayTao = x.NgayTao
            }).ToList()
        };
    }

    // ═══════════════════════════════════════════════════════════════════════
    // HANDLER: GetThongKeBacSiQuery — 14 chỉ số thống kê bác sĩ
    // ═══════════════════════════════════════════════════════════════════════
    public async Task<ThongKeBacSiAdminDto> Handle(GetThongKeBacSiQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var thangNay = now.Date.AddDays(-(now.Day - 1));       // đầu tháng này
        var thangTruoc = thangNay.AddMonths(-1);               // đầu tháng trước
        var bay7Ngay = now.AddDays(-7);
        var ba30Ngay = now.AddDays(-30);

        var allUsers = await _db.HoSoNguoiDung.ToListAsync(cancellationToken);
        var tongBacSi = allUsers.Count;

        // KPI cơ bản
        var bacSiMoiThangNay = allUsers.Count(u => u.NgayTao >= thangNay);
        var bacSiMoiThangTruoc = allUsers.Count(u => u.NgayTao >= thangTruoc && u.NgayTao < thangNay);
        var bacSiHoatDong7Ngay = allUsers.Count(u => u.NgayDangNhapCuoi >= bay7Ngay);
        var bacSiHoatDong30Ngay = allUsers.Count(u => u.NgayDangNhapCuoi >= ba30Ngay);
        var bacSiInactive = allUsers.Count(u => u.NgayDangNhapCuoi < ba30Ngay);
        var bacSiDuHoSo = allUsers.Count(u => !string.IsNullOrEmpty(u.SoDienThoai) && !string.IsNullOrEmpty(u.DonViCongTac));

        // Avg bệnh nhi / bác sĩ
        var tongBenhNhi = await _db.HoSoBenhNhan.CountAsync(cancellationToken);
        var avgBenhNhan = tongBacSi > 0 ? (double)tongBenhNhi / tongBacSi : 0;

        // Avg lượt tính toán / bác sĩ / tháng
        var luotTinhToanThang = await _db.LichSuTinhToan.CountAsync(x => x.NgayTinhToan >= thangNay, cancellationToken);
        var avgLuotTinhToan = tongBacSi > 0 ? (double)luotTinhToanThang / tongBacSi : 0;

        // Bác sĩ có BN nguy cơ thần kinh
        var dsBacSiCoNguyCoTK = await _db.HoSoBenhNhan
            .Where(b => b.CoNguyCoThanKinh)
            .Select(b => b.NguoiDungId)
            .Distinct()
            .CountAsync(cancellationToken);

        // Tỉ lệ tính toán vượt ngưỡng (≠ Bình thường) — must use ToList() first (EF cannot translate StringComparison)
        var allLichSu = await _db.LichSuTinhToan.Select(x => x.MucDoNguyHiem).ToListAsync(cancellationToken);
        var tongLuot = allLichSu.Count;
        var luotVuotNguong = allLichSu.Count(m => !string.IsNullOrEmpty(m) && !m.Contains("bình thường", StringComparison.OrdinalIgnoreCase));
        var tiLeTinhToanVuot = tongLuot > 0 ? (double)luotVuotNguong / tongLuot * 100 : 0;


        // Time-to-Value: ngày từ đăng ký đến bệnh nhi đầu tiên
        var userFirstPatient = await _db.HoSoBenhNhan
            .GroupBy(b => b.NguoiDungId)
            .Select(g => new { UserId = g.Key, FirstPatient = g.Min(b => b.NgayTao) })
            .ToListAsync(cancellationToken);
        double avgTimeToValue = 0;
        if (userFirstPatient.Any())
        {
            var ttv = userFirstPatient
                .Join(allUsers, fp => fp.UserId, u => u.Id, (fp, u) => (fp.FirstPatient - u.NgayTao).TotalDays)
                .Where(d => d >= 0)
                .ToList();
            avgTimeToValue = ttv.Any() ? ttv.Average() : 0;
        }

        // Phân bố chuyên khoa
        var chuyenKhoaGroups = allUsers
            .GroupBy(u => string.IsNullOrEmpty(u.ChuyenKhoa) ? "Chưa cập nhật" : u.ChuyenKhoa)
            .Select(g => new PhanBoNhomDto { NhanNhom = g.Key, SoLuong = g.Count(), PhanTram = tongBacSi > 0 ? (double)g.Count() / tongBacSi * 100 : 0 })
            .OrderByDescending(x => x.SoLuong).ToList();

        // Top 10 đơn vị công tác
        var donViGroups = allUsers
            .Where(u => !string.IsNullOrEmpty(u.DonViCongTac))
            .GroupBy(u => u.DonViCongTac!)
            .Select(g => new PhanBoNhomDto { NhanNhom = g.Key, SoLuong = g.Count(), PhanTram = tongBacSi > 0 ? (double)g.Count() / tongBacSi * 100 : 0 })
            .OrderByDescending(x => x.SoLuong).Take(10).ToList();

        // Phân bố cường độ sử dụng (lượt tính toán 30 ngày/bác sĩ)
        var userActivity = await _db.LichSuTinhToan
            .Where(l => l.NgayTinhToan >= ba30Ngay && l.Phien != null && l.Phien.NguoiDungId != null)
            .GroupBy(l => l.Phien!.NguoiDungId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        
        var tongHoatDong = tongBacSi;
        var nhom0 = tongBacSi - userActivity.Count;
        var nhom1_5 = userActivity.Count(x => x.Count >= 1 && x.Count <= 5);
        var nhom6_20 = userActivity.Count(x => x.Count >= 6 && x.Count <= 20);
        var nhomGT20 = userActivity.Count(x => x.Count > 20);
        var phanBoCuongDo = new List<PhanBoNhomDto>
        {
            new() { NhanNhom = "Không hoạt động (0)", SoLuong = nhom0, PhanTram = tongBacSi > 0 ? (double)nhom0 / tongBacSi * 100 : 0 },
            new() { NhanNhom = "Thấp (1–5)", SoLuong = nhom1_5, PhanTram = tongBacSi > 0 ? (double)nhom1_5 / tongBacSi * 100 : 0 },
            new() { NhanNhom = "Trung bình (6–20)", SoLuong = nhom6_20, PhanTram = tongBacSi > 0 ? (double)nhom6_20 / tongBacSi * 100 : 0 },
            new() { NhanNhom = "Cao (>20)", SoLuong = nhomGT20, PhanTram = tongBacSi > 0 ? (double)nhomGT20 / tongBacSi * 100 : 0 },
        };

        // Onboarding 12 tuần
        var onboarding = new List<ThongKeTheoNgayDto>();
        for (int i = 11; i >= 0; i--)
        {
            var start = now.Date.AddDays(-7 * (i + 1));
            var end = now.Date.AddDays(-7 * i);
            var cnt = allUsers.Count(u => u.NgayTao >= start && u.NgayTao < end);
            onboarding.Add(new ThongKeTheoNgayDto { Ngay = start.ToString("dd/MM"), SoLuong = cnt });
        }

        // Leaderboard Top 10
        var benhNhanPerUser = await _db.HoSoBenhNhan
            .GroupBy(b => b.NguoiDungId)
            .Select(g => new { UserId = g.Key, Count = g.Count(), CoNguyCoTK = g.Any(b => b.CoNguyCoThanKinh) })
            .ToListAsync(cancellationToken);
        var luotTinhToan30 = await _db.LichSuTinhToan
            .Where(l => l.NgayTinhToan >= ba30Ngay && l.Phien != null)
            .GroupBy(l => l.Phien!.NguoiDungId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var luotTinhToanTong = await _db.LichSuTinhToan
            .Where(l => l.Phien != null)
            .GroupBy(l => l.Phien!.NguoiDungId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var top10 = allUsers
            .Select(u => new LeaderboardBacSiDto
            {
                Id = u.Id,
                HoTen = u.HoTen,
                Email = u.Email,
                ChuyenKhoa = u.ChuyenKhoa,
                DonViCongTac = u.DonViCongTac,
                SoBenhNhan = benhNhanPerUser.FirstOrDefault(x => x.UserId == u.Id)?.Count ?? 0,
                LuotTinhToan30Ngay = luotTinhToan30.FirstOrDefault(x => x.UserId == u.Id)?.Count ?? 0,
                TongLuotTinhToan = luotTinhToanTong.FirstOrDefault(x => x.UserId == u.Id)?.Count ?? 0,
                CoNguoiBenhNhiNguyCoThanKinh = benhNhanPerUser.FirstOrDefault(x => x.UserId == u.Id)?.CoNguyCoTK ?? false,
                NgayDangNhapCuoi = u.NgayDangNhapCuoi
            })
            .OrderByDescending(x => x.SoBenhNhan)
            .Take(10).ToList();

        return new ThongKeBacSiAdminDto
        {
            TongBacSi = tongBacSi,
            BacSiMoiThangNay = bacSiMoiThangNay,
            BacSiMoiThangTruoc = bacSiMoiThangTruoc,
            BacSiHoatDong7Ngay = bacSiHoatDong7Ngay,
            BacSiHoatDong30Ngay = bacSiHoatDong30Ngay,
            BacSiInactive30Ngay = bacSiInactive,
            AvgBenhNhanPerBacSi = Math.Round(avgBenhNhan, 1),
            AvgLuotTinhToanPerBacSiPerThang = Math.Round(avgLuotTinhToan, 1),
            BacSiCoBenhNhiNguyCoThanKinh = dsBacSiCoNguyCoTK,
            TiLeTinhToanVuotNguong = Math.Round(tiLeTinhToanVuot, 1),
            AvgNgayDenBenhNhanDauTien = Math.Round(avgTimeToValue, 1),
            BacSiDuHoSo = bacSiDuHoSo,
            PhanBoChuyenKhoa = chuyenKhoaGroups,
            TopDonViCongTac = donViGroups,
            PhanBoCuongDoSuDung = phanBoCuongDo,
            OnboardingTuanQua = onboarding,
            Top10BacSiHoatDong = top10
        };
    }

    // ═══════════════════════════════════════════════════════════════════════
    // HANDLER: GetThongKeBenhNhanQuery — 23 chỉ số thống kê bệnh nhi
    // ═══════════════════════════════════════════════════════════════════════
    public async Task<ThongKeBenhNhanAdminDto> Handle(GetThongKeBenhNhanQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var thangNay = now.Date.AddDays(-(now.Day - 1));
        var thangTruoc = thangNay.AddMonths(-1);
        var ba30Ngay = now.Date.AddDays(-30);

        var lichSuKhongHoSo = await _db.LichSuTinhToan
            .Select(x => new
            {
                x.TuoiGio,
                x.TuoiThaiTuan,
                x.BilirubinMgDl,
                x.NguongChieuDen,
                x.NguongThayCuuMau,
                x.NgayTinhToan,
                NguoiDungId = x.Phien != null ? x.Phien.NguoiDungId : null
            })
            .ToListAsync(cancellationToken);
        var tongLuotKhongHoSo = lichSuKhongHoSo.Count;
        var luotAnDanh = lichSuKhongHoSo.Count(x => string.IsNullOrEmpty(x.NguoiDungId));
        var luotDaDangNhapKhongHoSo = tongLuotKhongHoSo - luotAnDanh;
        var luotKhongHoSo30Ngay = lichSuKhongHoSo.Count(x => x.NgayTinhToan >= ba30Ngay);

        // --- 2.1 Dân số học ---
        var allBN = await _db.HoSoBenhNhan.Where(x => !x.IsTestData).ToListAsync(cancellationToken);
        var tongBN = allBN.Count;
        var bnMoiThangNay = allBN.Count(b => b.NgayTao >= thangNay);
        var bnMoiThangTruoc = allBN.Count(b => b.NgayTao >= thangTruoc && b.NgayTao < thangNay);
        var tongNguyCoTK = allBN.Count(b => b.CoNguyCoThanKinh);
        var tongSinhNon = allBN.Count(b => b.TuoiThaiTuan < 37);
        var tongCucKyNon = allBN.Count(b => b.TuoiThaiTuan < 28);
        var tiLeNguyCoTK = tongBN > 0 ? (double)tongNguyCoTK / tongBN * 100 : 0;
        var tiLeSinhNon = tongBN > 0 ? (double)tongSinhNon / tongBN * 100 : 0;

        // --- 2.2 Biochemical Analytics ---
        // Peak bilirubin per patient
        var peakBilirubin = await _db.XetNghiemBilirubin
            .GroupBy(x => x.BenhNhanId)
            .Select(g => (double)g.Max(x => x.BilirubinMgDl))
            .ToListAsync(cancellationToken);
        var bilirubinMean = peakBilirubin.Any() ? peakBilirubin.Average() : 0;
        var bilirubinStdDev = 0.0;
        if (peakBilirubin.Count > 1)
        {
            var sumSq = peakBilirubin.Sum(x => (x - bilirubinMean) * (x - bilirubinMean));
            bilirubinStdDev = Math.Sqrt(sumSq / (peakBilirubin.Count - 1));
        }
        var tiLeBilirubinNang = peakBilirubin.Any() ? (double)peakBilirubin.Count(x => x > 20) / peakBilirubin.Count * 100 : 0;

        // Tốc độ tăng Bili từ MauBilirubinLuuTru
        var avgTocDo = await _db.PhienLamViec
            .SelectMany(p => p.MauBilirubin)
            .Where(m => m.TocDoThayDoi.HasValue && m.TocDoThayDoi > 0)
            .AverageAsync(m => (double?)m.TocDoThayDoi, cancellationToken) ?? 0;

        // --- 2.3 AAP Outcome ---
        var allXN = await _db.XetNghiemBilirubin.Where(x => !x.BenhNhan.IsTestData).ToListAsync(cancellationToken);
        var tongXN = allXN.Count;
        var tongMauLamSang = tongXN + tongLuotKhongHoSo;
        var avgLanDo = tongBN > 0 ? (double)tongXN / tongBN : 0;
        var interventionCategories = allXN.ToDictionary(
            x => x.Id,
            x => ClinicalInterventionClassifier.Classify(x.BilirubinMgDl, x.NguongChieuDen, x.NguongThayCuuMau));
        var validCategoryCount = interventionCategories.Values.Count(x => x != ClinicalInterventionCategory.KhongDuDuLieu);
        var tongPhotoTherapy = interventionCategories.Values.Count(x => x is ClinicalInterventionCategory.ChieuDen or ClinicalInterventionCategory.EscalationOfCare);
        var tongEscalation = interventionCategories.Values.Count(x => x == ClinicalInterventionCategory.EscalationOfCare);
        var tongExchange = interventionCategories.Values.Count(x => x == ClinicalInterventionCategory.ThayMau);
        var tiLePhoto = validCategoryCount > 0 ? (double)tongPhotoTherapy / validCategoryCount * 100 : 0;
        var tiLeExchange = validCategoryCount > 0 ? (double)tongExchange / validCategoryCount * 100 : 0;

        // --- 2.4 Safety Indicators ---
        var xnCountPerBN = allXN.GroupBy(x => x.BenhNhanId).ToDictionary(g => g.Key, g => g.Count());
        var lostFollowUp = allBN.Count(b => (xnCountPerBN.TryGetValue(b.Id, out var cnt) ? cnt : 0) == 1);
        var tiLeLostFollowUp = tongBN > 0 ? (double)lostFollowUp / tongBN * 100 : 0;
        var bnCritical = allBN.Count(b => b.TuoiThaiTuan < 35 && b.CoNguyCoThanKinh);
        var heSoKhaiThac = tongBN > 0 ? (double)tongXN / tongBN : 0;

        // Phân bố tuổi thai (GA)
        var phanBoGA = new List<PhanBoNhomDto>
        {
            new() { NhanNhom = "Cực non (<28w)", SoLuong = allBN.Count(b => b.TuoiThaiTuan < 28) },
            new() { NhanNhom = "Rất non (28-33w)", SoLuong = allBN.Count(b => b.TuoiThaiTuan >= 28 && b.TuoiThaiTuan <= 33) },
            new() { NhanNhom = "Non muộn (34-36w)", SoLuong = allBN.Count(b => b.TuoiThaiTuan >= 34 && b.TuoiThaiTuan <= 36) },
            new() { NhanNhom = "Đủ tháng (≥37w)", SoLuong = allBN.Count(b => b.TuoiThaiTuan >= 37) },
        };
        phanBoGA.ForEach(p => p.PhanTram = tongBN > 0 ? p.SoLuong * 100.0 / tongBN : 0);

        // Histogram bilirubin đỉnh
        var phanBoBili = new List<PhanBoNhomDto>
        {
            new() { NhanNhom = "0–5 mg/dL", SoLuong = peakBilirubin.Count(x => x <= 5) },
            new() { NhanNhom = "5–10 mg/dL", SoLuong = peakBilirubin.Count(x => x > 5 && x <= 10) },
            new() { NhanNhom = "10–15 mg/dL", SoLuong = peakBilirubin.Count(x => x > 10 && x <= 15) },
            new() { NhanNhom = "15–20 mg/dL", SoLuong = peakBilirubin.Count(x => x > 15 && x <= 20) },
            new() { NhanNhom = ">20 mg/dL", SoLuong = peakBilirubin.Count(x => x > 20) },
        };
        phanBoBili.ForEach(p => p.PhanTram = peakBilirubin.Count > 0 ? p.SoLuong * 100.0 / peakBilirubin.Count : 0);

        // Phân bố giờ tuổi lần đo đầu tiên
        var firstMeasureHours = await _db.XetNghiemBilirubin
            .GroupBy(x => x.BenhNhanId)
            .Select(g => g.Min(x => x.TuoiGioTuDong))
            .ToListAsync(cancellationToken);
        var phanBoGioTuoi = new List<PhanBoNhomDto>
        {
            new() { NhanNhom = "<24 giờ tuổi", SoLuong = firstMeasureHours.Count(x => x < 24) },
            new() { NhanNhom = "24–48 giờ", SoLuong = firstMeasureHours.Count(x => x >= 24 && x < 48) },
            new() { NhanNhom = "48–72 giờ", SoLuong = firstMeasureHours.Count(x => x >= 48 && x < 72) },
            new() { NhanNhom = ">72 giờ", SoLuong = firstMeasureHours.Count(x => x >= 72) },
        };
        phanBoGioTuoi.ForEach(p => p.PhanTram = firstMeasureHours.Count > 0 ? p.SoLuong * 100.0 / firstMeasureHours.Count : 0);

        // Donut AAP - tổng all-time
        var phanBoAAP = new List<PhanBoNhomDto>
        {
            new() { NhanNhom = "Bình thường", SoLuong = interventionCategories.Values.Count(x => x == ClinicalInterventionCategory.BinhThuong) },
            new() { NhanNhom = "Cần chiếu đèn", SoLuong = interventionCategories.Values.Count(x => x == ClinicalInterventionCategory.ChieuDen) },
            new() { NhanNhom = "Escalation of care", SoLuong = tongEscalation },
            new() { NhanNhom = "Cần thay máu", SoLuong = tongExchange },
            new() { NhanNhom = "Không đủ dữ liệu", SoLuong = interventionCategories.Values.Count(x => x == ClinicalInterventionCategory.KhongDuDuLieu) },
        };
        phanBoAAP.ForEach(p => p.PhanTram = tongXN > 0 ? p.SoLuong * 100.0 / tongXN : 0);

        // Rolling mean Bilirubin 30 ngày
        var bilirubinMean30 = new List<ThongKeTheoNgayDto>();
        var xnByDay = allXN.GroupBy(x => x.ThoiGianLayMau.Date).ToDictionary(g => g.Key, g => g.Average(x => (double)x.BilirubinMgDl));
        for (int i = 0; i < 30; i++)
        {
            var d = now.Date.AddDays(-29 + i);
            bilirubinMean30.Add(new ThongKeTheoNgayDto
            {
                Ngay = d.ToString("dd/MM"),
                SoLuong = xnByDay.TryGetValue(d, out var avgBili) ? (int)Math.Round(avgBili) : 0,
                GiaTri = xnByDay.TryGetValue(d, out var avgBili2) ? Math.Round(avgBili2, 1) : 0
            });
        }

        // XN per day 30 ngày
        var xnByDayCount = allXN.GroupBy(x => x.ThoiGianLayMau.Date).ToDictionary(g => g.Key, g => g.Count());
        var xetNghiem30Ngay = new List<ThongKeTheoNgayDto>();
        for (int i = 0; i < 30; i++)
        {
            var d = now.Date.AddDays(-29 + i);
            xetNghiem30Ngay.Add(new ThongKeTheoNgayDto
            {
                Ngay = d.ToString("dd/MM"),
                SoLuong = xnByDayCount.TryGetValue(d, out var cnt) ? cnt : 0
            });
        }

        // Heatmap giờ trong ngày
        var heatmap = new int[24];
        foreach (var xn in allXN)
            heatmap[xn.ThoiGianLayMau.Hour]++;

        // Top 5 bệnh nhi nguy cơ cao nhất
        var top5 = await (
            from b in _db.HoSoBenhNhan.Where(x => !x.IsTestData)
            join u in _db.HoSoNguoiDung on b.NguoiDungId equals u.Id into uJoin
            from u in uJoin.DefaultIfEmpty()
            select new
            {
                b.Id, b.HoTenBenhNhan, b.TuoiThaiTuan, b.CoNguyCoThanKinh, b.NgayGioSinh,
                TenBacSi = u != null ? u.HoTen : "Không rõ",
                EmailBacSi = u != null ? u.Email : "",
                BilirubinMax = (decimal?)_db.XetNghiemBilirubin.Where(x => x.BenhNhanId == b.Id).Max(x => (decimal?)x.BilirubinMgDl) ?? 0,
                RiskMargin = (decimal?)_db.XetNghiemBilirubin
                    .Where(x => x.BenhNhanId == b.Id && x.NguongThayCuuMau.HasValue)
                    .Max(x => (decimal?)(x.BilirubinMgDl - x.NguongThayCuuMau!.Value)) ?? decimal.MinValue,
                SoLanXN = _db.XetNghiemBilirubin.Count(x => x.BenhNhanId == b.Id),
                SoLanVuotThayMau = _db.XetNghiemBilirubin.Count(x => x.BenhNhanId == b.Id && x.NguongThayCuuMau.HasValue && x.BilirubinMgDl >= x.NguongThayCuuMau.Value)
            }
        ).OrderByDescending(x => x.RiskMargin)
         .ThenByDescending(x => x.CoNguyCoThanKinh)
         .ThenBy(x => x.TuoiThaiTuan)
         .Take(5)
         .ToListAsync(cancellationToken);

        var top5Dto = top5.Select(x => new TopBenhNhiNguyCoDto
        {
            Id = x.Id,
            HoTenBenhNhan = x.HoTenBenhNhan,
            TenBacSi = x.TenBacSi,
            EmailBacSi = x.EmailBacSi,
            TuoiThaiTuan = x.TuoiThaiTuan,
            CoNguyCoThanKinh = x.CoNguyCoThanKinh,
            BilirubinMax = x.BilirubinMax,
            SoLanXetNghiem = x.SoLanXN,
            SoLanVuotNguongThayMau = x.SoLanVuotThayMau,
            NgayGioSinh = x.NgayGioSinh
        }).ToList();

        return new ThongKeBenhNhanAdminDto
        {
            TongLuotTinhToanKhongHoSo = tongLuotKhongHoSo,
            LuotTinhToanAnDanh = luotAnDanh,
            LuotTinhToanDaDangNhapKhongHoSo = luotDaDangNhapKhongHoSo,
            LuotTinhToanKhongThongTinBacSi = luotAnDanh,
            LuotTinhToanKhongThongTinBenhNhi = tongLuotKhongHoSo,
            LuotTinhToanKhongHoSo30Ngay = luotKhongHoSo30Ngay,
            TongMauLamSang = tongMauLamSang,
            TongBenhNhi = tongBN,
            BenhNhiMoiThangNay = bnMoiThangNay,
            BenhNhiMoiThangTruoc = bnMoiThangTruoc,
            TongCoNguyCoThanKinh = tongNguyCoTK,
            TiLeNguyCoThanKinh = Math.Round(tiLeNguyCoTK, 1),
            TongSinhNon = tongSinhNon,
            TiLeSinhNon = Math.Round(tiLeSinhNon, 1),
            TongCucKyNon = tongCucKyNon,
            BilirubinDinhTrungBinh = Math.Round(bilirubinMean, 2),
            BilirubinDinhStdDev = Math.Round(bilirubinStdDev, 2),
            TiLeBilirubinNang = Math.Round(tiLeBilirubinNang, 1),
            AvgTocDoTangBilirubin = Math.Round(avgTocDo, 2),
            TongXetNghiem = tongXN,
            AvgLanDoPerBenhNhi = Math.Round(avgLanDo, 1),
            TongPhotoTherapy = tongPhotoTherapy,
            TongExchangeTransfusion = tongExchange,
            TiLePhotoTherapy = Math.Round(tiLePhoto, 1),
            TiLeExchangeTransfusion = Math.Round(tiLeExchange, 1),
            BenhNhiLostFollowUp = lostFollowUp,
            TiLeLostFollowUp = Math.Round(tiLeLostFollowUp, 1),
            BenhNhiCritical = bnCritical,
            HeSoKhaiThacHeThong = Math.Round(heSoKhaiThac, 1),
            PhanBoTuoiThai = phanBoGA,
            PhanBoBilirubinDinh = phanBoBili,
            PhanBoGioTuoiLanDoDau = phanBoGioTuoi,
            PhanBoChiDinhAAP = phanBoAAP,
            BilirubinTrungBinh30Ngay = bilirubinMean30,
            XetNghiem30Ngay = xetNghiem30Ngay,
            HeatmapGioTrongNgay = heatmap,
            Top5BenhNhiNguyCoNhat = top5Dto
        };
    }

    public async Task<PagedResultDto<NhatKyTinhToanAdminDto>> Handle(GetNhatKyTinhToanQuery request, CancellationToken cancellationToken)
    {
        var query = from l in _db.LichSuTinhToan
                    join p in _db.PhienLamViec on l.PhienId equals p.Id
                    join u in _db.HoSoNguoiDung on p.NguoiDungId equals u.Id into uJoin
                    from u in uJoin.DefaultIfEmpty()
                    select new NhatKyTinhToanAdminDto
                    {
                        Id = l.Id,
                        PhienId = l.PhienId,
                        DiaChiIP = p.DiaChiIP,
                        ThietBi = p.ThietBi,
                        LaAnDanh = p.LaAnDanh,
                        NguoiDungId = u != null ? u.Id : string.Empty,
                        NguoiDungEmail = u != null ? u.Email : string.Empty,
                        NguoiDungTen = u != null ? u.HoTen : string.Empty,
                        TuoiGio = l.TuoiGio,
                        TuoiThaiTuan = l.TuoiThaiTuan,
                        BilirubinMgDl = l.BilirubinMgDl,
                        CoNguyCoThanKinh = l.CoNguyCoThanKinh,
                        NguongChieuDen = l.NguongChieuDen,
                        NguongChieuDenTichCuc = l.NguongChieuDenTichCuc,
                        NguongThayCuuMau = l.NguongThayCuuMau,
                        MucDoNguyHiem = l.MucDoNguyHiem,
                        ChiTietYeuCauJson = l.ChiTietYeuCauJson,
                        NgayTinhToan = l.NgayTinhToan
                    };

        if (!string.IsNullOrEmpty(request.SearchKeyword))
        {
            var keyword = request.SearchKeyword.ToLower();
            query = query.Where(x => 
                (x.NguoiDungEmail != null && x.NguoiDungEmail.ToLower().Contains(keyword)) ||
                (x.NguoiDungTen != null && x.NguoiDungTen.ToLower().Contains(keyword)) ||
                (x.DiaChiIP != null && x.DiaChiIP.Contains(keyword)) ||
                x.Id.ToString() == keyword);
        }

        var totalItems = await query.CountAsync(cancellationToken);
        
        var items = await query
            .OrderByDescending(x => x.NgayTinhToan)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResultDto<NhatKyTinhToanAdminDto>
        {
            Items = items,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling(totalItems / (double)request.PageSize),
            CurrentPage = request.Page
        };
    }
}
