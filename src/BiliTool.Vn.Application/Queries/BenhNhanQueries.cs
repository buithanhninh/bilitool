using BiliTool.Vn.Application.DTOs;
using MediatR;

namespace BiliTool.Vn.Application.Queries;

/// <summary>Lấy danh sách bệnh nhân của một bác sĩ</summary>
public record LayDsBenhNhanQuery(string NguoiDungId) : IRequest<List<HoSoBenhNhanDto>>;

/// <summary>Lấy chi tiết một bệnh nhân kèm lịch sử xét nghiệm</summary>
public record LayChiTietBenhNhanQuery(Guid BenhNhanId, string NguoiDungId) : IRequest<HoSoBenhNhanDto?>;
