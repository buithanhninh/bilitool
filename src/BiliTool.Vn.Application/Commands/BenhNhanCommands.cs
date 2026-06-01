using BiliTool.Vn.Application.DTOs;
using MediatR;

namespace BiliTool.Vn.Application.Commands;

/// <summary>Tạo mới hồ sơ bệnh nhân</summary>
public record TaoBenhNhanCommand(string NguoiDungId, TaoBenhNhanDto Data) : IRequest<HoSoBenhNhanDto>;

/// <summary>Xóa hồ sơ bệnh nhân</summary>
public record XoaBenhNhanCommand(Guid BenhNhanId, string NguoiDungId) : IRequest<bool>;

/// <summary>Thêm một lần xét nghiệm Bilirubin mới cho bệnh nhân</summary>
public record ThemXetNghiemCommand(string NguoiDungId, ThemXetNghiemDto Data) : IRequest<XetNghiemBilirubinDto>;

/// <summary>Xóa một lần xét nghiệm</summary>
public record XoaXetNghiemCommand(Guid XetNghiemId, string NguoiDungId) : IRequest<bool>;
