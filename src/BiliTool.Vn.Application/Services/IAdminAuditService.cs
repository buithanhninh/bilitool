namespace BiliTool.Vn.Application.Services;

public interface IAdminAuditService
{
    Task RecordAsync(
        string actorId,
        string actorEmail,
        string action,
        string targetType,
        string? targetId,
        bool succeeded,
        string? remoteIp,
        string? correlationId = null,
        CancellationToken cancellationToken = default);
}
