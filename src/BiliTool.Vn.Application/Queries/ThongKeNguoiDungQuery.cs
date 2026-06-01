using MediatR;
using BiliTool.Vn.Application.DTOs;

namespace BiliTool.Vn.Application.Queries;

public record ThongKeNguoiDungQuery(string Id) : IRequest<ThongKeDto>;
