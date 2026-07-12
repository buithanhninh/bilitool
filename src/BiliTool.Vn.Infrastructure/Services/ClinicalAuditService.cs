using System.Text.Json;
using BiliTool.Vn.Application.Services;
using BiliTool.Vn.Domain.Clinical.Bilirubin;
using BiliTool.Vn.Domain.Entities;
using BiliTool.Vn.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace BiliTool.Vn.Infrastructure.Services;

public class ClinicalAuditService : IClinicalAuditService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly BiliToolDbContext _dbContext;
    private readonly ILogger<ClinicalAuditService> _logger;

    public ClinicalAuditService(BiliToolDbContext dbContext, ILogger<ClinicalAuditService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
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
                RequestJson = ClinicalAuditPayloadRedactor.Redact(request, JsonOptions),
                ResponseJson = ClinicalAuditPayloadRedactor.Redact(response, JsonOptions),
                TraceJson = JsonSerializer.Serialize(trace, JsonOptions),
            };

            _dbContext.ClinicalAuditLogs.Add(audit);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể ghi clinical audit log. Calculation vẫn trả kết quả bình thường.");
        }
    }
}
