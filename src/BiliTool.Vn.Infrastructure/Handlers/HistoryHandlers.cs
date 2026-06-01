using BiliTool.Vn.Application.Commands;
using BiliTool.Vn.Application.Queries;
using BiliTool.Vn.Application.DTOs;
using BiliTool.Vn.Domain.Entities;
using BiliTool.Vn.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BiliTool.Vn.Infrastructure.Handlers;

public class HistoryHandlers : 
    IRequestHandler<LayPhienLamViecQuery, PhienLamViec>,
    IRequestHandler<LuuLichSuCommand, LichSuTinhToan>,
    IRequestHandler<XoaLichSuCommand, bool>,
    IRequestHandler<ThongKeNguoiDungQuery, ThongKeDto>
{
    private readonly BiliToolDbContext _db;

    public HistoryHandlers(BiliToolDbContext db)
    {
        _db = db;
    }

    public async Task<PhienLamViec> Handle(LayPhienLamViecQuery request, CancellationToken cancellationToken)
    {
        var phien = await _db.PhienLamViec
            .Include(p => p.LichSuTinhToan)
            .FirstOrDefaultAsync(p => p.Id == request.PhienId, cancellationToken);
            
        if (phien == null)
        {
            // Tạo mới phiên
            if (!string.IsNullOrEmpty(request.NguoiDungId))
                phien = PhienLamViec.TaoDaDangNhap(request.NguoiDungId, request.IpAddress, request.UserAgent);
            else
                phien = PhienLamViec.TaoAnDanh(request.IpAddress, request.UserAgent);

            // Bắt buộc ID phải giống request.PhienId để tránh desync
            var propertyInfo = typeof(PhienLamViec).GetProperty(nameof(PhienLamViec.Id));
            if (propertyInfo != null && propertyInfo.CanWrite)
            {
                propertyInfo.SetValue(phien, request.PhienId);
            }
            else
            {
                // Vì Entity ID private set, ta dùng reflection
                var field = typeof(PhienLamViec).GetField("<Id>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (field != null) field.SetValue(phien, request.PhienId);
            }

            _db.PhienLamViec.Add(phien);
            await _db.SaveChangesAsync(cancellationToken);
        }
        else if (!string.IsNullOrEmpty(request.NguoiDungId) && phien.NguoiDungId != request.NguoiDungId)
        {
            // Gắn version ẩn danh vào user hiện tại
            var field = typeof(PhienLamViec).GetField("<NguoiDungId>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (field != null) field.SetValue(phien, request.NguoiDungId);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return phien;
    }

    public async Task<LichSuTinhToan> Handle(LuuLichSuCommand request, CancellationToken cancellationToken)
    {
        var phien = await _db.PhienLamViec.FindAsync(new object[] { request.PhienId }, cancellationToken);
        if (phien == null) throw new Exception("Phiên làm việc không tồn tại.");

        var lichSu = new LichSuTinhToan
        {
            PhienId = request.PhienId,
            TuoiGio = (int)Math.Round(request.KetQua.TuoiGio),
            TuoiThaiTuan = request.KetQua.TuoiThaiTuan,
            BilirubinMgDl = request.KetQua.BilirubinMgDl,
            CoNguyCoThanKinh = request.KetQua.CoNguyCoThanKinh,
            NguongChieuDen = request.KetQua.NguongChieuDen,
            NguongChieuDenTichCuc = request.KetQua.NguongChieuDenTichCuc,
            NguongThayCuuMau = request.KetQua.NguongThayCuuMau,
            MucDoNguyHiem = request.KetQua.MucDoNguyHiemEnum.ToString(),
            KhuyenNghiChinh = "", // Không lưu text cứng theo kiến trúc mới
            ChiTietYeuCauJson = System.Text.Json.JsonSerializer.Serialize(request.YeuCau),
            NgayTinhToan = DateTime.UtcNow
        };

        _db.LichSuTinhToan.Add(lichSu);
        await _db.SaveChangesAsync(cancellationToken);

        return lichSu;
    }

    public async Task<bool> Handle(XoaLichSuCommand request, CancellationToken cancellationToken)
    {
        var phien = await _db.PhienLamViec
            .Include(p => p.LichSuTinhToan)
            .FirstOrDefaultAsync(p => p.Id == request.PhienId, cancellationToken);

        if (phien != null && phien.LichSuTinhToan.Any())
        {
            _db.LichSuTinhToan.RemoveRange(phien.LichSuTinhToan);
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        return false;
    }

    public async Task<ThongKeDto> Handle(ThongKeNguoiDungQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Id))
            return new ThongKeDto();

        var query = _db.PhienLamViec
            .Include(p => p.LichSuTinhToan)
            .Where(p => p.NguoiDungId == request.Id);

        var tongSoCa = await query.CountAsync(cancellationToken);
        
        // Sum total historical records across all these sessions
        var tongLichSu = await query.SelectMany(p => p.LichSuTinhToan).CountAsync(cancellationToken);

        return new ThongKeDto
        {
            TongSoCa = tongSoCa,
            TongSoLanTinhToan = tongLichSu
        };
    }
}
