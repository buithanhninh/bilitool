using MediatR;

namespace BiliTool.Vn.Application.Commands;

public record KhoaTaiKhoanCommand(string Id, bool TrangThaiKhoaMoi) : IRequest<bool>;
