using BiliTool.Vn.Domain.Entities;
using MediatR;
using BiliTool.Vn.Application.DTOs;

namespace BiliTool.Vn.Application.Commands;

public record LuuLichSuCommand(
    Guid PhienId,
    YeuCauTinhToanBilirubinDto YeuCau,
    KetQuaTinhToanDto KetQua
) : IRequest<LichSuTinhToan>;
