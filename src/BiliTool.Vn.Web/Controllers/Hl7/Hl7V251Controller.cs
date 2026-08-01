using BiliTool.Vn.Application.Commands;
using BiliTool.Vn.Application.Clinical;
using BiliTool.Vn.Application.DTOs;
using BiliTool.Vn.Application.Services;
using BiliTool.Vn.Domain.Clinical.Bilirubin;
using BiliTool.Vn.Web.Services.Hl7;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Http.Timeouts;
using System.Diagnostics;

namespace BiliTool.Vn.Web.Controllers.Hl7;

[ApiController]
[Route("api/v3/hl7/v251")]
[EnableRateLimiting("ApiPolicy")]
[TypeFilter(typeof(Filters.ApiKeyAuthFilter), Order = -100)]
[TypeFilter(typeof(Filters.HisRolloutFilter), Order = -90)]
public sealed class Hl7V251Controller(
    Hl7V251OruAdapter adapter,
    IValidator<HisClinicalCalculationRequest> validator,
    IMediator mediator,
    IClinicalRequestContext requestContext,
    ILogger<Hl7V251Controller> logger) : ControllerBase
{
    [HttpPost("oru-r01")]
    [TypeFilter(typeof(Filters.HisIdempotencyFilter), Order = -50)]
    [Consumes("application/hl7-v2", "text/plain")]
    [Produces("application/hl7-v2")]
    [RequestSizeLimit(128 * 1024)]
    [RequestTimeout("HisApi")]
    public async Task<IActionResult> OruR01([FromBody] string message, CancellationToken cancellationToken)
    {
        Hl7OruParseResult? parsed = null;
        try
        {
            parsed = adapter.Parse(message);
            var idempotencyKey = Request.Headers["Idempotency-Key"].ToString();
            if (!string.Equals(idempotencyKey, parsed.MessageControlId, StringComparison.Ordinal))
                return Ack(adapter.BuildAck(parsed.Msh, parsed.MessageControlId, "AE", "Idempotency-Key phải bằng MSH-10."));

            var validation = await validator.ValidateAsync(parsed.Request, cancellationToken);
            if (!validation.IsValid)
                return Ack(adapter.BuildAck(parsed.Msh, parsed.MessageControlId, "AE", string.Join("; ", validation.Errors.Select(error => error.ErrorMessage))));

            var resultId = $"calc_{Guid.NewGuid():N}";
            requestContext.ResultId = resultId;
            using var activity = HisIntegrationDiagnostics.ActivitySource.StartActivity("his.calculate", ActivityKind.Internal);
            activity?.SetTag("his.protocol", "hl7-v251");
            activity?.SetTag("his.tenant_id", requestContext.TenantId);
            activity?.SetTag("his.api_client_id", requestContext.ApiClientId);
            activity?.SetTag("his.result_id", resultId);
            activity?.SetTag("hl7.message_control_id", parsed.MessageControlId);
            var result = await mediator.Send(new TinhToanBilirubinCommand(HisClinicalRequestMapper.Map(parsed.Request)), cancellationToken);
            activity?.SetStatus(ActivityStatusCode.Ok);
            var zbr = string.Join('|',
                "ZBR",
                resultId,
                result.MucDoNguyHiemEnum,
                result.NguongChieuDen.ToString(System.Globalization.CultureInfo.InvariantCulture),
                result.NguongChieuDenTichCuc.ToString(System.Globalization.CultureInfo.InvariantCulture),
                result.NguongThayCuuMau.ToString(System.Globalization.CultureInfo.InvariantCulture),
                "mg/dL",
                BilirubinEngineMetadata.GuidelineCode,
                BilirubinEngineMetadata.EngineVersion,
                BilirubinEngineMetadata.GuidelineRevision,
                BilirubinEngineMetadata.GuidelineEffectiveDate,
                BilirubinEngineMetadata.DatasetRevision);
            return Ack(adapter.BuildAck(parsed.Msh, parsed.MessageControlId, "AA", "Application accept", zbr));
        }
        catch (Hl7ValidationException ex)
        {
            return Ack(adapter.BuildAck(parsed?.Msh, ex.MessageControlId ?? parsed?.MessageControlId ?? "UNKNOWN", "AE", ex.Message));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi HL7 v2.5.1 ORU. CorrelationId: {CorrelationId}", HttpContext.TraceIdentifier);
            return Ack(adapter.BuildAck(parsed?.Msh, parsed?.MessageControlId ?? "UNKNOWN", "AE", "Application internal error"));
        }
    }

    private ContentResult Ack(string message) => new()
    {
        StatusCode = StatusCodes.Status200OK,
        ContentType = "application/hl7-v2; charset=utf-8",
        Content = message
    };
}
