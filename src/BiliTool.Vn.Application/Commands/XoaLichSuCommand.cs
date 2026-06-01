using MediatR;

namespace BiliTool.Vn.Application.Commands;

public record XoaLichSuCommand(Guid PhienId) : IRequest<bool>;
