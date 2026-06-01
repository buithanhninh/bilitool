using BiliTool.Vn.Application.Commands;
using BiliTool.Vn.Application.DTOs;
using BiliTool.Vn.Application.Queries;
using BiliTool.Vn.Domain.Entities;
using BiliTool.Vn.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BiliTool.Vn.Infrastructure.Handlers;

public class HoSoHandlers :
    IRequestHandler<LayHoSoNguoiDungQuery, HoSoNguoiDungDto?>,
    IRequestHandler<LuuHoSoNguoiDungCommand, bool>
{
    private readonly BiliToolDbContext _db;

    public HoSoHandlers(BiliToolDbContext db) => _db = db;

    public async Task<HoSoNguoiDungDto?> Handle(LayHoSoNguoiDungQuery request, CancellationToken cancellationToken)
    {
        var hoSo = await _db.HoSoNguoiDung
            .FirstOrDefaultAsync(h => h.Id == request.Id, cancellationToken);

        if (hoSo == null) return null;

        return new HoSoNguoiDungDto
        {
            Id = hoSo.Id,
            HoTen = hoSo.HoTen,
            NgaySinh = hoSo.NgaySinh,
            SoDienThoai = hoSo.SoDienThoai,
            DonViCongTac = hoSo.DonViCongTac,
            ChuyenKhoa = hoSo.ChuyenKhoa,
            ChucDanh = hoSo.ChucDanh,
            NgayCapNhat = hoSo.NgayCapNhat
        };
    }

    public async Task<bool> Handle(LuuHoSoNguoiDungCommand request, CancellationToken cancellationToken)
    {
        var dto = request.HoSo;
        var existing = await _db.HoSoNguoiDung
            .FirstOrDefaultAsync(h => h.Id == dto.Id, cancellationToken);

        if (existing == null)
        {
            // Tạo mới
            _db.HoSoNguoiDung.Add(new HoSoNguoiDung
            {
                Id    = dto.Id,
                HoTen       = dto.HoTen,
                NgaySinh    = dto.NgaySinh.HasValue ? DateTime.SpecifyKind(dto.NgaySinh.Value, DateTimeKind.Utc) : null,
                SoDienThoai = dto.SoDienThoai,
                DonViCongTac = dto.DonViCongTac,
                ChuyenKhoa  = dto.ChuyenKhoa,
                ChucDanh    = dto.ChucDanh,
                NgayCapNhat = DateTime.UtcNow
            });
        }
        else
        {
            // Cập nhật (Upsert)
            existing.HoTen        = dto.HoTen;
            existing.NgaySinh     = dto.NgaySinh.HasValue ? DateTime.SpecifyKind(dto.NgaySinh.Value, DateTimeKind.Utc) : null;
            existing.SoDienThoai  = dto.SoDienThoai;
            existing.DonViCongTac = dto.DonViCongTac;
            existing.ChuyenKhoa   = dto.ChuyenKhoa;
            existing.ChucDanh     = dto.ChucDanh;
            existing.NgayCapNhat  = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
