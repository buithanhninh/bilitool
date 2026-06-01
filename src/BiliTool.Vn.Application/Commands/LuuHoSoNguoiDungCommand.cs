using BiliTool.Vn.Application.DTOs;
using MediatR;

namespace BiliTool.Vn.Application.Commands;

public record LuuHoSoNguoiDungCommand(HoSoNguoiDungDto HoSo) : IRequest<bool>;
