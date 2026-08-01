using BiliTool.Vn.Application;
using BiliTool.Vn.Application.Commands;
using BiliTool.Vn.Application.DTOs;
using BiliTool.Vn.Application.Services;
using BiliTool.Vn.Domain.Clinical.Bilirubin;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Http.Timeouts;

namespace BiliTool.Vn.Web.Controllers.V2;

[ApiController]
[Route("api/v2/clinical/bilirubin")]
[EnableRateLimiting("ApiPolicy")]
[TypeFilter(typeof(Filters.ApiKeyAuthFilter), Order = -100)]
public class ClinicalBilirubinController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ClinicalBilirubinController> _logger;
    private readonly IClinicalRequestContext _requestContext;

    public ClinicalBilirubinController(
        IMediator mediator,
        ILogger<ClinicalBilirubinController> logger,
        IClinicalRequestContext requestContext)
    {
        _mediator = mediator;
        _logger = logger;
        _requestContext = requestContext;
    }

    [HttpGet("guidelines/active")]
    [ProducesResponseType(typeof(ClinicalGuidelineMetadataResponse), StatusCodes.Status200OK)]
    public IActionResult GetActiveGuidelines()
    {
        return Ok(new ClinicalGuidelineMetadataResponse(
            BilirubinEngineMetadata.EngineMode,
            BilirubinEngineMetadata.EngineVersion,
            BilirubinEngineMetadata.DatasetMode,
            BilirubinEngineMetadata.UsesExternalDatasetEngine,
            new[]
            {
                new ClinicalGuidelineDto("AAP2022", BilirubinEngineMetadata.GuidelineRevision, BilirubinEngineMetadata.GuidelineEffectiveDate, "combined-threshold-source", BilirubinEngineMetadata.EngineMode, BilirubinEngineMetadata.EngineVersion, BilirubinEngineMetadata.DatasetRevision),
                new ClinicalGuidelineDto("NICE_CG98", BilirubinEngineMetadata.GuidelineRevision, BilirubinEngineMetadata.GuidelineEffectiveDate, "combined-threshold-source", BilirubinEngineMetadata.EngineMode, BilirubinEngineMetadata.EngineVersion, BilirubinEngineMetadata.DatasetRevision)
            }));
    }

    [HttpPost("calculate")]
    [TypeFilter(typeof(Filters.HisIdempotencyFilter), Order = -50)]
    [RequestTimeout("HisApi")]
    [ProducesResponseType(typeof(ClinicalBilirubinV2Response), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Calculate([FromBody] YeuCauTinhToanBilirubinDto request, CancellationToken cancellationToken)
    {
        try
        {
            var resultId = $"calc_{Guid.NewGuid():N}";
            _requestContext.ResultId = resultId;
            var result = await _mediator.Send(new TinhToanBilirubinCommand(request), cancellationToken);
            return Ok(ClinicalBilirubinV2Response.From(resultId, result));
        }
        catch (LoiXacThucException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Lỗi xác thực dữ liệu (Validation Error)",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Dữ liệu đầu vào không hợp lệ hoặc thiếu bắt buộc.",
                Extensions = { { "errors", ex.LoiXacThuc } }
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            var traceId = HttpContext.TraceIdentifier;
            _logger.LogError(ex, "Lỗi hệ thống khi tính bilirubin qua API v2. TraceId: {TraceId}", traceId);

            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Lỗi hệ thống (System Error)",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "Không thể xử lý yêu cầu tại thời điểm này. Vui lòng thử lại hoặc liên hệ quản trị hệ thống.",
                Extensions = { { "traceId", traceId } }
            });
        }
    }

    public record ClinicalBilirubinV2Response(
        string ResultId,
        ClinicalGuidelineDto Guideline,
        ClinicalPatientContextDto PatientContext,
        ClinicalThresholdDto Thresholds,
        ClinicalRecommendationDto Recommendation,
        KetQuaTinhToanDto LegacyResult)
    {
        public static ClinicalBilirubinV2Response From(string resultId, KetQuaTinhToanDto result)
        {
            return new ClinicalBilirubinV2Response(
                ResultId: resultId,
                Guideline: new ClinicalGuidelineDto(
                    BilirubinEngineMetadata.GuidelineCode,
                    BilirubinEngineMetadata.GuidelineRevision,
                    BilirubinEngineMetadata.GuidelineEffectiveDate,
                    result.PhacDoQuyetDinh.ToString(),
                    BilirubinEngineMetadata.EngineMode,
                    BilirubinEngineMetadata.EngineVersion,
                    BilirubinEngineMetadata.DatasetRevision),
                PatientContext: new ClinicalPatientContextDto(result.TuoiGio, result.TuoiThaiTuan, result.CoNguyCoThanKinh),
                Thresholds: new ClinicalThresholdDto(
                    result.NguongChieuDen,
                    result.NguongChieuDenTichCuc,
                    result.NguongThayCuuMau,
                    result.NguongChieuDen_NICE_UmolL,
                    result.NguongThayCuuMau_NICE_UmolL),
                Recommendation: new ClinicalRecommendationDto(
                    result.MucDoNguyHiemEnum.ToString(),
                    result.CanChieuDenNgay,
                    result.CanChieuDenTichCuc,
                    result.CanXemXetThayCuuMau,
                    result.GioDoLapTiepTheo,
                    result.ChuThichThamChieu),
                LegacyResult: result);
        }
    }

    public record ClinicalGuidelineDto(
        string Code,
        string Revision,
        string EffectiveDate,
        string DecisionProtocol,
        string EngineMode,
        string EngineVersion,
        string DatasetRevision);
    public record ClinicalGuidelineMetadataResponse(
        string ActiveEngine,
        string EngineVersion,
        string DatasetMode,
        bool UseDatasetEngine,
        IReadOnlyList<ClinicalGuidelineDto> Guidelines);
    public record ClinicalPatientContextDto(double AgeHours, int GestationalAgeWeeks, bool HasNeurotoxicityRisk);
    public record ClinicalThresholdDto(
        decimal PhototherapyMgDl,
        decimal EscalationOfCareMgDl,
        decimal ExchangeTransfusionMgDl,
        decimal NicePhototherapyUmolL,
        decimal NiceExchangeTransfusionUmolL);
    public record ClinicalRecommendationDto(
        string Level,
        bool StartPhototherapy,
        bool IntensivePhototherapy,
        bool ConsiderExchangeTransfusion,
        int? RepeatInHours,
        IReadOnlyList<string> References);
}
