using BiliTool.Vn.Application.Services;
using BiliTool.Vn.Domain.Entities;
using BiliTool.Vn.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace BiliTool.Vn.Infrastructure.Services;

public sealed class AdminAuditService(BiliToolDbContext db, ILogger<AdminAuditService> logger) : IAdminAuditService
{
    public async Task RecordAsync(string actorId, string actorEmail, string action, string targetType, string? targetId, bool succeeded, string? remoteIp, string? correlationId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            db.Add(new AdminAuditLog
            {
                ActorId = actorId,
                ActorEmail = actorEmail,
                Action = action,
                TargetType = targetType,
                TargetId = targetId,
                Succeeded = succeeded,
                RemoteIp = remoteIp,
                CorrelationId = correlationId
            });
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Không thể ghi admin audit action {Action}", action);
        }
    }
}
