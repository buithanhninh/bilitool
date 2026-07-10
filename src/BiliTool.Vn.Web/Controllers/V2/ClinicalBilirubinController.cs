using BiliTool.Vn.Application;
using BiliTool.Vn.Application.Commands;
using BiliTool.Vn.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BiliTool.Vn.Web.Controllers.V2;

[ApiController]
[Route("api/v2/clinical/bilirubin")]
[EnableRateLimiting("ApiPolicy")]
[TypeFilter(typeof(Filters.ApiKeyAuthFilter))]
public class ClinicalBilirubinController : ControllerBase
{
    private const string GuidelineCode = "AAP2022+NICECG98";
    private const string EngineMode = "BaselineMayTinhBilirubin";
    private readonly IMediator _mediator;
    private readonly ILogger<ClinicalBilirubinController> _logger;

    public ClinicalBilirubinController(IMediator mediator, ILogger<ClinicalBilirubinController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet("guidelines/active")]
    [ProducesResponseType(typeof(ClinicalGuidelineMetadataResponse), StatusCodes.Status200OK)]
    public IActionResult GetActiveGuidelines()
    {
        return Ok(new ClinicalGuidelineMetadataResponse(
            EngineMode,
            "shadow-metadata-only",
            false,
            new[]
            {
                new ClinicalGuidelineDto("AAP2022", "combined-threshold-source", EngineMode),
                new ClinicalGuidelineDto("NICE_CG98", "combined-threshold-source", EngineMode)
            }));
    }

    [HttpPost("calculate")]
    [ProducesResponseType(typeof(ClinicalBilirubinV2Response), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Calculate([FromBody] YeuCauTinhToanBilirubinDto request)
    {
        try
        {
            var result = await _mediator.Send(new TinhToanBilirubinCommand(request));
            return Ok(ClinicalBilirubinV2Response.From(result));
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
        public static ClinicalBilirubinV2Response From(KetQuaTinhToanDto result)
        {
            return new ClinicalBilirubinV2Response(
                ResultId: $"calc_{Guid.NewGuid():N}",
                Guideline: new ClinicalGuidelineDto(GuidelineCode, result.PhacDoQuyetDinh.ToString(), EngineMode),
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

    public record ClinicalGuidelineDto(string Code, string DecisionProtocol, string EngineMode);
    public record ClinicalGuidelineMetadataResponse(
        string ActiveEngine,
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
