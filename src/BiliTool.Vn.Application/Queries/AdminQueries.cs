using BiliTool.Vn.Application.DTOs;
using MediatR;

namespace BiliTool.Vn.Application.Queries;

public record GetThongKeHeThongQuery : IRequest<ThongKeHeThongDto>;

public record GetDanhSachTaiKhoanQuery : IRequest<List<TaiKhoanAdminDto>>;

public record GetDanhSachBenhNhanAdminQuery : IRequest<List<BenhNhanAdminDto>>;

// ─── Lớp 1: Giao diện tổng quan ───────────────────────────────────────────
public record GetThongKeBieuDoAdminQuery : IRequest<ThongKeBieuDoAdminDto>;

// ─── Lớp 2 & Lớp 3: Drill-down ────────────────────────────────────────────
public record LayChiTietTaiKhoanAdminQuery(string Id) : IRequest<TaiKhoanDetailAdminDto?>;

public record LayChiTietBenhNhanAdminQuery(Guid BenhNhanId) : IRequest<HoSoBenhNhanDto?>;

// ─── Nhóm Thống kê Chuyên sâu ─────────────────────────────────────────────
public record GetThongKeBacSiQuery : IRequest<ThongKeBacSiAdminDto>;

public record GetThongKeBenhNhanQuery : IRequest<ThongKeBenhNhanAdminDto>;

