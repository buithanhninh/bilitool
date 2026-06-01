using BiliTool.Vn.Application.DTOs;
using MediatR;

namespace BiliTool.Vn.Application.Queries;

public record LayHoSoNguoiDungQuery(string Id) : IRequest<HoSoNguoiDungDto?>;
