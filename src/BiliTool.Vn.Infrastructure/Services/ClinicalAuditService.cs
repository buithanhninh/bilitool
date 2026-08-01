using System.Text.Json;
using BiliTool.Vn.Application.Services;
using BiliTool.Vn.Domain.Clinical.Bilirubin;
using BiliTool.Vn.Domain.Entities;
using BiliTool.Vn.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace BiliTool.Vn.Infrastructure.Services;

public class ClinicalAuditService : IClinicalAuditService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly BiliToolDbContext _dbContext;
    private readonly ILogger<ClinicalAuditService> _logger;
    private readonly IClinicalRequestContext? _requestContext;

    public ClinicalAuditService(
        BiliToolDbContext dbContext,
        ILogger<ClinicalAuditService> logger,
        IClinicalRequestContext? requestContext = null)
    {
        _dbContext = dbContext;
        _logger = logger;
        _requestContext = requestContext;
    }

    public async Task TryRecordCalculationAsync(
        object request,
        object response,
        BilirubinCalculationTrace trace,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var audit = new ClinicalAuditLog
            {
                GuidelineCode = trace.GuidelineCode,
                EngineMode = trace.EngineMode,
                EngineVersion = trace.EngineVersion,
                TenantId = _requestContext?.TenantId,
                ApiClientId = _requestContext?.ApiClientId,
                CorrelationId = _requestContext?.CorrelationId,
                ResultId = _requestContext?.ResultId,
                RequestJson = ClinicalAuditPayloadRedactor.Redact(request, JsonOptions),
                ResponseJson = ClinicalAuditPayloadRedactor.Redact(response, JsonOptions),
                TraceJson = JsonSerializer.Serialize(trace, JsonOptions),
            };

            _dbContext.ClinicalAuditLogs.Add(audit);
            await EnqueueWebhookEventsAsync(request, response, trace, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể ghi clinical audit log. Calculation vẫn trả kết quả bình thường.");
        }
    }

    private async Task EnqueueWebhookEventsAsync(
        object request,
        object response,
        BilirubinCalculationTrace trace,
        CancellationToken cancellationToken)
    {
        const string eventType = "clinical.calculation.completed";
        var tenantId = _requestContext?.TenantId;
        var apiClientId = _requestContext?.ApiClientId;
        var resultId = _requestContext?.ResultId;
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(apiClientId) || string.IsNullOrWhiteSpace(resultId))
            return;

        var subscriptions = await _dbContext.HisWebhookSubscriptions
            .Where(item => item.IsActive && item.TenantId == tenantId && item.ApiClientId == apiClientId)
            .ToListAsync(cancellationToken);
        foreach (var subscription in subscriptions.Where(item =>
                     item.EventTypes.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains(eventType, StringComparer.Ordinal)))
        {
            var outboxEvent = new HisOutboxEvent
            {
                WebhookSubscriptionId = subscription.Id,
                TenantId = tenantId,
                ApiClientId = apiClientId,
                EventType = eventType,
                ResultId = resultId,
                CorrelationId = _requestContext?.CorrelationId
            };
            outboxEvent.PayloadJson = JsonSerializer.Serialize(new
            {
                eventId = outboxEvent.Id.ToString("N"),
                type = eventType,
                occurredAt = DateTimeOffset.UtcNow,
                tenantId,
                apiClientId,
                resultId,
                correlationId = _requestContext?.CorrelationId,
                guideline = new
                {
                    code = trace.GuidelineCode,
                    revision = trace.GuidelineRevision,
                    effectiveDate = trace.GuidelineEffectiveDate,
                    engineMode = trace.EngineMode,
                    engineVersion = trace.EngineVersion,
                    datasetRevision = trace.DatasetRevision
                },
                request = JsonSerializer.Deserialize<JsonElement>(ClinicalAuditPayloadRedactor.Redact(request, JsonOptions)),
                response = JsonSerializer.Deserialize<JsonElement>(ClinicalAuditPayloadRedactor.Redact(response, JsonOptions))
            }, JsonOptions);
            _dbContext.HisOutboxEvents.Add(outboxEvent);
        }
    }
}
