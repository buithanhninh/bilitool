namespace BiliTool.Vn.Application.Services;

public enum HisOutboxReplayResult
{
    Replayed,
    NotFound,
    NotDeadLetter,
    SubscriptionInactive
}

public interface IHisOutboxOperationsService
{
    Task<HisOutboxReplayResult> ReplayDeadLetterAsync(Guid eventId, CancellationToken cancellationToken = default);
}
