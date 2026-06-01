using BiliTool.Vn.Domain.Entities;
using BiliTool.Vn.Application.DTOs;
using MediatR;

namespace BiliTool.Vn.Application.Queries;

public record LayPhienLamViecQuery(Guid PhienId, string? NguoiDungId = null, string? IpAddress = null, string? UserAgent = null)
    : IRequest<PhienLamViec>;
