using System.Text.Json;
using BiliTool.Vn.Application.Commands;
using BiliTool.Vn.Application.Clinical;
using BiliTool.Vn.Application.DTOs;
using BiliTool.Vn.Application.Services;
using BiliTool.Vn.Domain.Clinical.Bilirubin;
using BiliTool.Vn.Web.Services.Fhir;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Diagnostics;
using Microsoft.AspNetCore.Http.Timeouts;

namespace BiliTool.Vn.Web.Controllers.Fhir;

[ApiController]
[Route("api/v3/fhir/R4")]
[EnableRateLimiting("ApiPolicy")]
[TypeFilter(typeof(Filters.ApiKeyAuthFilter), Order = -100)]
[TypeFilter(typeof(Filters.HisRolloutFilter), Order = -90)]
public sealed class FhirR4BilirubinController(
    FhirR4BilirubinBundleAdapter adapter,
    IValidator<HisClinicalCalculationRequest> validator,
    IMediator mediator,
    IClinicalRequestContext requestContext,
    ILogger<FhirR4BilirubinController> logger) : ControllerBase
{
    [HttpGet("metadata")]
    [Produces("application/fhir+json")]
    public IActionResult Metadata() => new JsonResult(new
    {
        resourceType = "CapabilityStatement",
        status = "active",
        date = "2026-07-31",
        kind = "instance",
        fhirVersion = "4.0.1",
        format = new[] { "application/fhir+json" },
        software = new { name = "BiliTool.Vn", version = BilirubinEngineMetadata.EngineVersion },
        rest = new[]
        {
            new
            {
                mode = "server",
                resource = new object[]
                {
                    new { type = "Bundle", interaction = new[] { new { code = "create" } } },
                    new { type = "OperationOutcome", interaction = Array.Empty<object>() },
                    new { type = "DiagnosticReport", interaction = Array.Empty<object>() },
                    new { type = "Observation", interaction = Array.Empty<object>() }
                },
                operation = new[]
                {
                    new
                    {
                        name = "bilirubin-calculate",
                        definition = "https://bilitool.vn/fhir/OperationDefinition/bilirubin-calculate"
                    }
                }
            }
        }
    }) { ContentType = "application/fhir+json" };

    [HttpPost("$bilirubin-calculate")]
    [TypeFilter(typeof(Filters.HisIdempotencyFilter), Order = -50)]
    [Consumes("application/fhir+json", "application/json")]
    [Produces("application/fhir+json")]
    [RequestSizeLimit(128 * 1024)]
    [RequestTimeout("HisApi")]
    public async Task<IActionResult> Calculate([FromBody] JsonElement bundle, CancellationToken cancellationToken)
    {
        try
        {
            var canonical = adapter.Parse(bundle);
            var validation = await validator.ValidateAsync(canonical, cancellationToken);
            if (!validation.IsValid)
                return OperationOutcome(StatusCodes.Status400BadRequest, "invalid", string.Join("; ", validation.Errors.Select(error => error.ErrorMessage)));

            var resultId = $"calc_{Guid.NewGuid():N}";
            requestContext.ResultId = resultId;
            using var activity = HisIntegrationDiagnostics.ActivitySource.StartActivity("his.calculate", ActivityKind.Internal);
            activity?.SetTag("his.protocol", "fhir-r4");
            activity?.SetTag("his.tenant_id", requestContext.TenantId);
            activity?.SetTag("his.api_client_id", requestContext.ApiClientId);
            activity?.SetTag("his.result_id", resultId);
            var result = await mediator.Send(new TinhToanBilirubinCommand(HisClinicalRequestMapper.Map(canonical)), cancellationToken);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return new JsonResult(BuildResponseBundle(resultId, canonical, result))
            {
                ContentType = "application/fhir+json",
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (FhirBundleValidationException ex)
        {
            return OperationOutcome(StatusCodes.Status400BadRequest, "structure", ex.Message);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi FHIR R4 bilirubin. CorrelationId: {CorrelationId}", HttpContext.TraceIdentifier);
            return OperationOutcome(StatusCodes.Status500InternalServerError, "exception", "Không thể xử lý FHIR Bundle.");
        }
    }

    private ObjectResult OperationOutcome(int status, string code, string diagnostics) => new(new
    {
        resourceType = "OperationOutcome",
        issue = new[]
        {
            new
            {
                severity = status >= 500 ? "error" : "error",
                code,
                diagnostics,
                details = new { text = $"CorrelationId: {HttpContext.TraceIdentifier}" }
            }
        }
    })
    {
        StatusCode = status,
        ContentTypes = { "application/fhir+json" }
    };

    private static object BuildResponseBundle(
        string resultId,
        HisClinicalCalculationRequest request,
        KetQuaTinhToanDto result)
    {
        var observationId = $"bilirubin-result-{resultId[5..]}";
        var reportId = $"diagnostic-report-{resultId[5..]}";
        return new
        {
            resourceType = "Bundle",
            type = "collection",
            identifier = new { system = "https://bilitool.vn/results", value = resultId },
            timestamp = DateTimeOffset.UtcNow,
            entry = new object[]
            {
                new
                {
                    fullUrl = $"https://bilitool.vn/fhir/Observation/{observationId}",
                    resource = new
                    {
                        resourceType = "Observation",
                        id = observationId,
                        status = "final",
                        code = new { coding = new[] { new { system = "http://loinc.org", code = "1975-2", display = "Bilirubin.total in Serum or Plasma" } } },
                        subject = new { identifier = new { system = request.Patient.AssigningAuthority, value = request.Patient.Identifier } },
                        encounter = new { reference = $"Encounter/{request.Encounter.Identifier}" },
                        effectiveDateTime = request.Observation.EffectiveAt,
                        valueQuantity = new { value = result.BilirubinMgDl, system = "http://unitsofmeasure.org", code = "mg/dL" },
                        component = new object[]
                        {
                            Component("phototherapy-threshold", result.NguongChieuDen, "mg/dL"),
                            Component("escalation-of-care-threshold", result.NguongChieuDenTichCuc, "mg/dL"),
                            Component("exchange-transfusion-threshold", result.NguongThayCuuMau, "mg/dL")
                        }
                    }
                },
                new
                {
                    fullUrl = $"https://bilitool.vn/fhir/DiagnosticReport/{reportId}",
                    resource = new
                    {
                        resourceType = "DiagnosticReport",
                        id = reportId,
                        status = "final",
                        code = new { text = "BiliTool.Vn bilirubin clinical decision support" },
                        subject = new { identifier = new { system = request.Patient.AssigningAuthority, value = request.Patient.Identifier } },
                        encounter = new { reference = $"Encounter/{request.Encounter.Identifier}" },
                        basedOn = new[] { new { reference = $"ServiceRequest/{request.Order.Identifier}" } },
                        result = new[] { new { reference = $"Observation/{observationId}" } },
                        conclusion = result.MucDoNguyHiemEnum.ToString(),
                        extension = new object[]
                        {
                            Extension("result-id", resultId),
                            Extension("guideline-code", BilirubinEngineMetadata.GuidelineCode),
                            Extension("guideline-revision", BilirubinEngineMetadata.GuidelineRevision),
                            Extension("guideline-effective-date", BilirubinEngineMetadata.GuidelineEffectiveDate),
                            Extension("dataset-revision", BilirubinEngineMetadata.DatasetRevision),
                            Extension("engine-version", BilirubinEngineMetadata.EngineVersion)
                        }
                    }
                }
            }
        };
    }

    private static object Component(string code, decimal value, string unit) => new
    {
        code = new { coding = new[] { new { system = "https://bilitool.vn/fhir/CodeSystem/clinical-threshold", code } } },
        valueQuantity = new { value, system = "http://unitsofmeasure.org", code = unit }
    };

    private static object Extension(string name, string value) => new
    {
        url = FhirR4BilirubinBundleAdapter.ProfileBase + name,
        valueString = value
    };
}
