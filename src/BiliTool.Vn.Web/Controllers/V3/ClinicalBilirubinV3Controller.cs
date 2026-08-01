using BiliTool.Vn.Application;
using BiliTool.Vn.Application.Commands;
using BiliTool.Vn.Application.Clinical;
using BiliTool.Vn.Application.DTOs;
using BiliTool.Vn.Application.Services;
using BiliTool.Vn.Domain.Clinical.Bilirubin;
using BiliTool.Vn.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Diagnostics;
using Microsoft.AspNetCore.Http.Timeouts;

namespace BiliTool.Vn.Web.Controllers.V3;

[ApiController]
[Route("api/v3/clinical/bilirubin")]
[EnableRateLimiting("ApiPolicy")]
[TypeFilter(typeof(Filters.ApiKeyAuthFilter), Order = -100)]
[TypeFilter(typeof(Filters.HisRolloutFilter), Order = -90)]
public sealed class ClinicalBilirubinV3Controller(
    IMediator mediator,
    IValidator<HisClinicalCalculationRequest> validator,
    IClinicalRequestContext requestContext,
    ILogger<ClinicalBilirubinV3Controller> logger) : ControllerBase
{
    [HttpPost("calculate")]
    [TypeFilter(typeof(Filters.HisIdempotencyFilter), Order = -50)]
    [RequestTimeout("HisApi")]
    [RequestSizeLimit(64 * 1024)]
    [ProducesResponseType(typeof(HisClinicalCalculationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Calculate(
        [FromBody] HisClinicalCalculationRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ProblemResponse(
                StatusCodes.Status400BadRequest,
                "invalid_request",
                "Dữ liệu HIS/EMR không hợp lệ.",
                validation.Errors.GroupBy(error => error.PropertyName).ToDictionary(
                    group => group.Key,
                    group => group.Select(error => error.ErrorMessage).Distinct().ToArray()));
        }

        var resultId = $"calc_{Guid.NewGuid():N}";
        requestContext.ResultId = resultId;
        using var activity = HisIntegrationDiagnostics.ActivitySource.StartActivity("his.calculate", ActivityKind.Internal);
        activity?.SetTag("his.protocol", "rest-v3");
        activity?.SetTag("his.tenant_id", requestContext.TenantId);
        activity?.SetTag("his.api_client_id", requestContext.ApiClientId);
        activity?.SetTag("his.result_id", resultId);

        try
        {
            var legacyRequest = HisClinicalRequestMapper.Map(request);
            var result = await mediator.Send(new TinhToanBilirubinCommand(legacyRequest), cancellationToken);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return Ok(MapResponse(resultId, request, result));
        }
        catch (LoiXacThucException ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "clinical_validation_failed");
            return ProblemResponse(
                StatusCodes.Status400BadRequest,
                "clinical_validation_failed",
                "Dữ liệu không đáp ứng điều kiện áp dụng phác đồ.",
                ex.LoiXacThuc);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.GetType().Name);
            logger.LogError(ex, "Lỗi API v3 HIS/EMR. CorrelationId: {CorrelationId}", HttpContext.TraceIdentifier);
            return ProblemResponse(
                StatusCodes.Status500InternalServerError,
                "clinical_calculation_failed",
                "Không thể xử lý yêu cầu tại thời điểm này.",
                retryable: true);
        }
    }

    private ObjectResult ProblemResponse(
        int status,
        string errorCode,
        string detail,
        IDictionary<string, string[]>? errors = null,
        bool retryable = false)
    {
        var problem = new ProblemDetails
        {
            Type = $"https://bilitool.vn/problems/{errorCode}",
            Title = errorCode,
            Status = status,
            Detail = detail,
            Instance = HttpContext.Request.Path
        };
        problem.Extensions["errorCode"] = errorCode;
        problem.Extensions["correlationId"] = HttpContext.TraceIdentifier;
        problem.Extensions["retryable"] = retryable;
        if (errors != null) problem.Extensions["errors"] = errors;
        return new ObjectResult(problem) { StatusCode = status };
    }

    private HisClinicalCalculationResponse MapResponse(
        string resultId,
        HisClinicalCalculationRequest request,
        KetQuaTinhToanDto result) => new(
            resultId,
            HttpContext.TraceIdentifier,
            new HisRequestReferences(
                request.Source.MessageId,
                request.Patient.Identifier,
                request.Encounter.Identifier,
                request.Order.Identifier,
                request.Specimen.Identifier,
                request.Observation.Identifier),
            new HisClinicalProvenance(
                BilirubinEngineMetadata.GuidelineCode,
                BilirubinEngineMetadata.GuidelineRevision,
                BilirubinEngineMetadata.GuidelineEffectiveDate,
                BilirubinEngineMetadata.EngineMode,
                BilirubinEngineMetadata.EngineVersion,
                BilirubinEngineMetadata.DatasetMode,
                BilirubinEngineMetadata.DatasetRevision,
                result.PhacDoQuyetDinh.ToString()),
            new HisNormalizedObservation(
                result.TuoiGio,
                result.TuoiThaiTuan,
                result.BilirubinMgDl,
                result.BilirubinUmolL,
                request.Observation.EffectiveAt),
            new HisClinicalThresholds(
                result.NguongChieuDen,
                result.NguongChieuDenTichCuc,
                result.NguongThayCuuMau,
                result.NguongChieuDen_NICE_UmolL,
                result.NguongThayCuuMau_NICE_UmolL),
            new HisClinicalRecommendation(
                result.MucDoNguyHiemEnum.ToString(),
                result.CanChieuDenNgay,
                result.CanChieuDenTichCuc,
                result.CanXemXetThayCuuMau,
                result.GioDoLapTiepTheo,
                result.ChuThichThamChieu));
}

public record HisClinicalCalculationResponse(
    string ResultId,
    string CorrelationId,
    HisRequestReferences References,
    HisClinicalProvenance Provenance,
    HisNormalizedObservation Observation,
    HisClinicalThresholds Thresholds,
    HisClinicalRecommendation Recommendation);

public record HisRequestReferences(
    string MessageId,
    string PatientId,
    string EncounterId,
    string OrderId,
    string SpecimenId,
    string ObservationId);

public record HisClinicalProvenance(
    string GuidelineCode,
    string GuidelineRevision,
    string GuidelineEffectiveDate,
    string EngineMode,
    string EngineVersion,
    string DatasetMode,
    string DatasetRevision,
    string DecisionProtocol);

public record HisNormalizedObservation(
    double AgeHours,
    int GestationalAgeWeeks,
    decimal BilirubinMgDl,
    decimal BilirubinUmolL,
    DateTimeOffset EffectiveAt);

public record HisClinicalThresholds(
    decimal PhototherapyMgDl,
    decimal EscalationOfCareMgDl,
    decimal ExchangeTransfusionMgDl,
    decimal NicePhototherapyUmolL,
    decimal NiceExchangeTransfusionUmolL);

public record HisClinicalRecommendation(
    string Level,
    bool StartPhototherapy,
    bool IntensivePhototherapy,
    bool ConsiderExchangeTransfusion,
    int? RepeatInHours,
    IReadOnlyList<string> References);
