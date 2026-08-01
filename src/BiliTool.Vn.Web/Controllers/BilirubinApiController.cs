using BiliTool.Vn.Application;
using BiliTool.Vn.Application.Commands;
using BiliTool.Vn.Application.DTOs;
using BiliTool.Vn.Application.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BiliTool.Vn.Web.Controllers;

[ApiController]
[Route("api/v1/bilirubin")]
[EnableRateLimiting("ApiPolicy")]
[TypeFilter(typeof(Filters.ApiKeyAuthFilter), Order = -100)]
public class BilirubinApiController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<BilirubinApiController> _logger;
    private readonly IClinicalRequestContext _requestContext;

    public BilirubinApiController(
        IMediator mediator,
        ILogger<BilirubinApiController> logger,
        IClinicalRequestContext requestContext)
    {
        _mediator = mediator;
        _logger = logger;
        _requestContext = requestContext;
    }

    [HttpPost("calculate")]
    [ProducesResponseType(typeof(KetQuaTinhToanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Calculate([FromBody] YeuCauTinhToanBilirubinDto request)
    {
        try
        {
            _requestContext.ResultId = $"calc_{Guid.NewGuid():N}";
            var command = new TinhToanBilirubinCommand(request);
            var result = await _mediator.Send(command);
            return Ok(result);
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
            _logger.LogError(ex, "Lỗi hệ thống khi tính bilirubin qua API HIS. TraceId: {TraceId}", traceId);

            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Lỗi hệ thống (System Error)",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "Không thể xử lý yêu cầu tại thời điểm này. Vui lòng thử lại hoặc liên hệ quản trị hệ thống.",
                Extensions = { { "traceId", traceId } }
            });
        }
    }
}
