using MediatR;
using BiliTool.Vn.Application.DTOs;

namespace BiliTool.Vn.Application.Queries;

public record GetNhatKyTinhToanQuery(int Page = 1, int PageSize = 50, string? SearchKeyword = null) : IRequest<PagedResultDto<NhatKyTinhToanAdminDto>>;
