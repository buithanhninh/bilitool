using BiliTool.Vn.Application.Services;
using BiliTool.Vn.Domain.Entities;
using BiliTool.Vn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BiliTool.Vn.Infrastructure.Services;

public sealed class HisIdempotencyService(BiliToolDbContext dbContext) : IHisIdempotencyService
{
    public async Task<HisIdempotencyAcquireResult> AcquireAsync(
        string tenantId,
        string apiClientId,
        string idempotencyKey,
        string requestHash,
        CancellationToken cancellationToken = default)
    {
        var existing = await FindAsync(tenantId, apiClientId, idempotencyKey, cancellationToken);
        if (existing != null) return ToDecision(existing, requestHash);

        dbContext.HisIdempotencyRecords.Add(new HisIdempotencyRecord
        {
            TenantId = tenantId,
            ApiClientId = apiClientId,
            IdempotencyKey = idempotencyKey,
            RequestHash = requestHash
        });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return new HisIdempotencyAcquireResult(HisIdempotencyDecision.Acquired);
        }
        catch (DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();
            existing = await FindAsync(tenantId, apiClientId, idempotencyKey, cancellationToken);
            if (existing == null) throw;
            return ToDecision(existing, requestHash);
        }
    }

    public async Task CompleteAsync(
        string tenantId,
        string apiClientId,
        string idempotencyKey,
        string resultId,
        int responseStatusCode,
        string responseJson,
        string responseContentType,
        CancellationToken cancellationToken = default)
    {
        var record = await FindAsync(tenantId, apiClientId, idempotencyKey, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy idempotency record cần hoàn tất.");

        record.Status = HisIdempotencyStatus.Completed;
        record.ResultId = resultId;
        record.ResponseStatusCode = responseStatusCode;
        record.ResponseJson = JsonSerializer.Serialize(responseJson);
        record.ResponseContentType = responseContentType;
        record.CompletedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ReleaseAsync(
        string tenantId,
        string apiClientId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var record = await FindAsync(tenantId, apiClientId, idempotencyKey, cancellationToken);
        if (record == null || record.Status == HisIdempotencyStatus.Completed) return;
        dbContext.HisIdempotencyRecords.Remove(record);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private Task<HisIdempotencyRecord?> FindAsync(
        string tenantId,
        string apiClientId,
        string idempotencyKey,
        CancellationToken cancellationToken) => dbContext.HisIdempotencyRecords.SingleOrDefaultAsync(
            record => record.TenantId == tenantId &&
                      record.ApiClientId == apiClientId &&
                      record.IdempotencyKey == idempotencyKey,
            cancellationToken);

    private static HisIdempotencyAcquireResult ToDecision(HisIdempotencyRecord record, string requestHash)
    {
        if (!string.Equals(record.RequestHash, requestHash, StringComparison.Ordinal))
            return new HisIdempotencyAcquireResult(HisIdempotencyDecision.PayloadConflict);

        return record.Status == HisIdempotencyStatus.Completed
            ? new HisIdempotencyAcquireResult(
                HisIdempotencyDecision.Replay,
                record.ResultId,
                record.ResponseStatusCode,
                DecodePayload(record.ResponseJson),
                record.ResponseContentType)
            : new HisIdempotencyAcquireResult(HisIdempotencyDecision.InProgress);
    }

    private static string? DecodePayload(string? storedPayload)
    {
        if (string.IsNullOrEmpty(storedPayload) || storedPayload[0] != '"') return storedPayload;
        try
        {
            return JsonSerializer.Deserialize<string>(storedPayload);
        }
        catch (JsonException)
        {
            return storedPayload;
        }
    }
}
