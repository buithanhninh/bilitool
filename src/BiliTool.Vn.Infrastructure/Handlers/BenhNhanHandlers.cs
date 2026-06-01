using BiliTool.Vn.Application.Commands;
using BiliTool.Vn.Application.DTOs;
using BiliTool.Vn.Application.Queries;
using BiliTool.Vn.Domain.Entities;
using BiliTool.Vn.Domain.Services;
using BiliTool.Vn.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BiliTool.Vn.Infrastructure.Handlers;

public class BenhNhanHandlers :
    IRequestHandler<LayDsBenhNhanQuery, List<HoSoBenhNhanDto>>,
    IRequestHandler<LayChiTietBenhNhanQuery, HoSoBenhNhanDto?>,
    IRequestHandler<TaoBenhNhanCommand, HoSoBenhNhanDto>,
    IRequestHandler<XoaBenhNhanCommand, bool>,
    IRequestHandler<ThemXetNghiemCommand, XetNghiemBilirubinDto>,
    IRequestHandler<XoaXetNghiemCommand, bool>
{
    private readonly BiliToolDbContext _db;
    private readonly IMayTinhBilirubin _mayTinh;

    public BenhNhanHandlers(BiliToolDbContext db, IMayTinhBilirubin mayTinh)
    {
        _db = db;
        _mayTinh = mayTinh;
    }

    // ─── Lấy danh sách bệnh nhân ─────────────────────────────────────────
    public async Task<List<HoSoBenhNhanDto>> Handle(LayDsBenhNhanQuery request, CancellationToken ct)
    {
        var list = await _db.HoSoBenhNhan
            .Where(h => h.NguoiDungId == request.NguoiDungId)
            .Include(h => h.DsXetNghiem)
            .OrderByDescending(h => h.NgayTao)
            .ToListAsync(ct);

        return list.Select(MapToDto).ToList();
    }

    // ─── Lấy chi tiết một bệnh nhân ──────────────────────────────────────
    public async Task<HoSoBenhNhanDto?> Handle(LayChiTietBenhNhanQuery request, CancellationToken ct)
    {
        var h = await _db.HoSoBenhNhan
            .Include(x => x.DsXetNghiem.OrderByDescending(xn => xn.ThoiGianLayMau))
            .FirstOrDefaultAsync(h => h.Id == request.BenhNhanId && h.NguoiDungId == request.NguoiDungId, ct);

        return h == null ? null : MapToDto(h);
    }

    // ─── Tạo mới bệnh nhân ───────────────────────────────────────────────
    public async Task<HoSoBenhNhanDto> Handle(TaoBenhNhanCommand request, CancellationToken ct)
    {
        var dto = request.Data;

        // Parse NgayGioSinh từ string "dd/MM/yyyy HH:mm"
        if (!DateTime.TryParseExact(dto.NgayGioSinhStr, "dd/MM/yyyy HH:mm",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var ngayGioSinh))
        {
            throw new InvalidOperationException("Ngày giờ sinh không hợp lệ. Định dạng đúng: dd/MM/yyyy HH:mm");
        }

        var maBenhNhan = string.IsNullOrWhiteSpace(dto.MaBenhNhan) 
            ? $"BN-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}"
            : dto.MaBenhNhan.Trim();

        var entity = new HoSoBenhNhan
        {
            Id = Guid.NewGuid(),
            MaBenhNhan = maBenhNhan,
            NguoiDungId = request.NguoiDungId,
            HoTenBenhNhan = dto.HoTenBenhNhan.Trim(),
            NgayGioSinh = ngayGioSinh.ToUniversalTime(),
            TuoiThaiTuan = dto.TuoiThaiTuan,
            CoNguyCoThanKinh = dto.CoNguyCoThanKinh,
            AnhChiBiVangDaCanChieuDen = dto.AnhChiBiVangDaCanChieuDen,
            MeBuMeHoanToan = dto.MeBuMeHoanToan,
            VangDaTrong24hDau = dto.VangDaTrong24hDau,
            BenhTanHuyetRh = dto.BenhTanHuyetRh,
            BenhTanHuyetABO = dto.BenhTanHuyetABO,
            GhiChu = dto.GhiChu?.Trim(),
            NgayTao = DateTime.UtcNow
        };

        _db.HoSoBenhNhan.Add(entity);
        await _db.SaveChangesAsync(ct);

        return MapToDto(entity);
    }

    // ─── Xóa bệnh nhân ───────────────────────────────────────────────────
    public async Task<bool> Handle(XoaBenhNhanCommand request, CancellationToken ct)
    {
        var entity = await _db.HoSoBenhNhan
            .FirstOrDefaultAsync(h => h.Id == request.BenhNhanId && h.NguoiDungId == request.NguoiDungId, ct);

        if (entity == null) return false;

        var childRecords = await _db.XetNghiemBilirubin.Where(x => x.BenhNhanId == entity.Id).ToListAsync(ct);
        if (childRecords.Any())
        {
            _db.XetNghiemBilirubin.RemoveRange(childRecords);
        }

        _db.HoSoBenhNhan.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    // ─── Thêm xét nghiệm mới ─────────────────────────────────────────────
    public async Task<XetNghiemBilirubinDto> Handle(ThemXetNghiemCommand request, CancellationToken ct)
    {
        var dto = request.Data;

        // Lấy hồ sơ bệnh nhân – xác minh thuộc về bác sĩ
        var benhNhan = await _db.HoSoBenhNhan
            .FirstOrDefaultAsync(h => h.Id == dto.BenhNhanId && h.NguoiDungId == request.NguoiDungId, ct)
            ?? throw new InvalidOperationException("Không tìm thấy bệnh nhân.");

        // Mặc định = Now; nếu có tùy chọn → dùng giá trị đó
        var thoiGianLayMau = dto.ThoiGianLayMauTuyChon?.ToUniversalTime() ?? DateTime.UtcNow;

        // Tính giờ tuổi (từ mốc sinh→lúc lấy mẫu)
        var tuoiGioTuDong = (thoiGianLayMau - benhNhan.NgayGioSinh).TotalHours;

        // Tính toán ngưỡng (reuse domain logic)
        var yeuToNguyCo = new Domain.ValueObjects.YeuToNguyCoThanKinh 
        { 
            TinhTrangLamSangKhongOnDinh = benhNhan.CoNguyCoThanKinh, // Đại diện nguy cơ AAP
            AnhChiBiVangDaCanChieuDen = benhNhan.AnhChiBiVangDaCanChieuDen,
            MeBuMeHoanToan = benhNhan.MeBuMeHoanToan,
            VangDaTrong24hDau = benhNhan.VangDaTrong24hDau,
            BenhTanHuyetRh = benhNhan.BenhTanHuyetRh,
            BenhTanHuyetABO = benhNhan.BenhTanHuyetABO
        };
        var ketQua = _mayTinh.TinhToan(
            tuoiGioTuDong,
            dto.BilirubinMgDl,
            benhNhan.TuoiThaiTuan,
            yeuToNguyCo);

        var entity = new XetNghiemBilirubin
        {
            Id = Guid.NewGuid(),
            BenhNhanId = dto.BenhNhanId,
            ThoiGianLayMau = thoiGianLayMau,
            BilirubinMgDl = dto.BilirubinMgDl,
            TuoiGioTuDong = tuoiGioTuDong,
            MucDoNguyHiem = MoTaMucDo(ketQua.MucDoNguyHiem),
            NguongChieuDen = ketQua.NguongChieuDen,
            NguongThayCuuMau = ketQua.NguongThayCuuMau,
            NgayTao = DateTime.UtcNow
        };

        _db.XetNghiemBilirubin.Add(entity);
        await _db.SaveChangesAsync(ct);

        return MapXetNghiemToDto(entity);
    }

    // ─── Xóa xét nghiệm ──────────────────────────────────────────────────
    public async Task<bool> Handle(XoaXetNghiemCommand request, CancellationToken ct)
    {
        // Chỉ xóa nếu xét nghiệm thuộc về bác sĩ đang xác thực
        var entity = await _db.XetNghiemBilirubin
            .Include(x => x.BenhNhan)
            .FirstOrDefaultAsync(x => x.Id == request.XetNghiemId && x.BenhNhan.NguoiDungId == request.NguoiDungId, ct);

        if (entity == null) return false;

        _db.XetNghiemBilirubin.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    // ─── Helpers ─────────────────────────────────────────────────────────
    private static HoSoBenhNhanDto MapToDto(HoSoBenhNhan h) => new()
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
        DsXetNghiem = h.DsXetNghiem.Select(MapXetNghiemToDto).ToList()
    };

    private static XetNghiemBilirubinDto MapXetNghiemToDto(XetNghiemBilirubin x) => new()
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
    };

    private static string MoTaMucDo(Domain.Enums.MucDoNguyHiem mucDo) => mucDo switch
    {
        Domain.Enums.MucDoNguyHiem.BinhThuong => "Bình thường",
        Domain.Enums.MucDoNguyHiem.CanTheoDoiSat => "Cần theo dõi sát",
        Domain.Enums.MucDoNguyHiem.CanChieuDen => "Cần chiếu đèn",
        Domain.Enums.MucDoNguyHiem.CanChieuDenTichCuc => "Cần chiếu đèn tích cực",
        Domain.Enums.MucDoNguyHiem.CanXemXetThayMau => "Cân nhắc thay máu",
        Domain.Enums.MucDoNguyHiem.KhanCapThayCuuMau => "KHẨN CẤP - Thay máu",
        _ => "Không xác định"
    };
}
