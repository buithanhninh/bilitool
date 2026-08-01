using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using BiliTool.Vn.Application.DTOs;
using BiliTool.Vn.Application.Services;
using BiliTool.Vn.Domain.Clinical.Bilirubin;
using BiliTool.Vn.Domain.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using BiliTool.Vn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BiliTool.Vn.Domain.Tests;

public sealed class HisApiContractTests : IClassFixture<HisApiFactory>
{
    private readonly HttpClient _client;

    public HisApiContractTests(HisApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task V2Calculate_WithoutApiKey_ReturnsProblemDetails401()
    {
        var response = await _client.PostAsJsonAsync("/api/v2/clinical/bilirubin/calculate", ValidRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task V2Calculate_WithInvalidEnum_Returns400InsteadOf500()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v2/clinical/bilirubin/calculate")
        {
            Content = JsonContent.Create(new
            {
                tuoiTheoGio = 48,
                tongBilirubin = 12,
                donViDo = 99,
                tuoiThaiTuan = 38,
                trangThaiChieuDen = 0,
                yeuToNguyCo = new { }
            })
        };
        request.Headers.Add("X-API-Key", HisApiFactory.ApiKey);
        request.Headers.Add("Idempotency-Key", "invalid-enum-request-001");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Đơn vị đo bilirubin không hợp lệ", body);
    }

    [Fact]
    public async Task V2Calculate_WithValidRequest_ReturnsStableClinicalWrapper()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v2/clinical/bilirubin/calculate")
        {
            Content = JsonContent.Create(ValidRequest())
        };
        request.Headers.Add("X-API-Key", HisApiFactory.ApiKey);
        request.Headers.Add("Idempotency-Key", "valid-calculation-request-001");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.StartsWith("calc_", root.GetProperty("resultId").GetString());
        Assert.Equal(BilirubinEngineMetadata.GuidelineCode, root.GetProperty("guideline").GetProperty("code").GetString());
        Assert.True(root.TryGetProperty("patientContext", out _));
        Assert.True(root.TryGetProperty("thresholds", out _));
        Assert.True(root.TryGetProperty("recommendation", out _));
        Assert.True(root.TryGetProperty("legacyResult", out _));
    }

    [Fact]
    public async Task ActiveGuidelines_ReturnsCentralizedEngineMetadata()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v2/clinical/bilirubin/guidelines/active");
        request.Headers.Add("X-API-Key", HisApiFactory.ApiKey);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.Equal(BilirubinEngineMetadata.EngineMode, root.GetProperty("activeEngine").GetString());
        Assert.Equal(BilirubinEngineMetadata.EngineVersion, root.GetProperty("engineVersion").GetString());
        Assert.Equal(BilirubinEngineMetadata.DatasetMode, root.GetProperty("datasetMode").GetString());
        Assert.False(root.GetProperty("useDatasetEngine").GetBoolean());
    }

    [Fact]
    public async Task SuppliedCorrelationId_IsReturnedUnchanged()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v2/clinical/bilirubin/guidelines/active");
        request.Headers.Add("X-API-Key", HisApiFactory.ApiKey);
        request.Headers.Add("X-Correlation-ID", "his-order-20260731-001");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("his-order-20260731-001", response.Headers.GetValues("X-Correlation-ID").Single());
    }

    [Fact]
    public async Task InvalidCorrelationId_IsReplaced()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v2/clinical/bilirubin/guidelines/active");
        request.Headers.Add("X-API-Key", HisApiFactory.ApiKey);
        request.Headers.TryAddWithoutValidation("X-Correlation-ID", "invalid correlation id");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("req_", response.Headers.GetValues("X-Correlation-ID").Single());
    }

    [Fact]
    public async Task V2Calculate_WithoutIdempotencyKey_Returns400()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v2/clinical/bilirubin/calculate")
        {
            Content = JsonContent.Create(ValidRequest())
        };
        request.Headers.Add("X-API-Key", HisApiFactory.ApiKey);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Idempotency-Key", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task V2Calculate_RetrySameKeyAndPayload_ReplaysSameResult()
    {
        const string key = "replay-same-request-001";
        var first = await SendCalculationAsync(key, ValidRequest());
        var second = await SendCalculationAsync(key, ValidRequest());

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(await first.Content.ReadAsStringAsync(), await second.Content.ReadAsStringAsync());
        Assert.Equal("true", second.Headers.GetValues("Idempotency-Replayed").Single());
    }

    [Fact]
    public async Task V2Calculate_SameKeyDifferentPayload_Returns409()
    {
        const string key = "conflicting-request-001";
        await SendCalculationAsync(key, ValidRequest());
        var changed = ValidRequest();
        changed.TongBilirubin = 13m;

        var response = await SendCalculationAsync(key, changed);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ApiRateLimit_Returns429WithRetryAfter()
    {
        using var factory = new HisApiFactory();
        using var client = factory.CreateClient();
        HttpResponseMessage? rejected = null;

        for (var attempt = 0; attempt < 31; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v2/clinical/bilirubin/guidelines/active");
            request.Headers.Add("X-API-Key", HisApiFactory.ApiKey);
            rejected = await client.SendAsync(request);
        }

        Assert.NotNull(rejected);
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected!.StatusCode);
        Assert.True(rejected.Headers.Contains("Retry-After"));
    }

    [Fact]
    public async Task V3Calculate_ReturnsCanonicalResponseWithReferencesAndProvenance()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v3/clinical/bilirubin/calculate")
        {
            Content = JsonContent.Create(ValidV3Request())
        };
        request.Headers.Add("X-API-Key", HisApiFactory.ApiKey);
        request.Headers.Add("Idempotency-Key", "canonical-v3-request-001");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.Equal("msg-001", root.GetProperty("references").GetProperty("messageId").GetString());
        Assert.Equal("patient-001", root.GetProperty("references").GetProperty("patientId").GetString());
        Assert.Equal(BilirubinEngineMetadata.EngineVersion, root.GetProperty("provenance").GetProperty("engineVersion").GetString());
        Assert.Equal(BilirubinEngineMetadata.GuidelineRevision, root.GetProperty("provenance").GetProperty("guidelineRevision").GetString());
        Assert.Equal(BilirubinEngineMetadata.GuidelineEffectiveDate, root.GetProperty("provenance").GetProperty("guidelineEffectiveDate").GetString());
        Assert.Equal(BilirubinEngineMetadata.DatasetRevision, root.GetProperty("provenance").GetProperty("datasetRevision").GetString());
        Assert.Equal(48d, root.GetProperty("observation").GetProperty("ageHours").GetDouble());
        Assert.False(root.TryGetProperty("legacyResult", out _));
    }

    [Fact]
    public async Task V3Calculate_UnknownJsonField_ReturnsStableInvalidJsonProblem()
    {
        var payload = """
        {
          "source":{"system":"HIS-A","facility":"FAC-A","messageId":"msg-002"},
          "patient":{"identifier":"patient-002","assigningAuthority":"FAC-A","ageHours":48,"gestationalAgeWeeks":38,"phototherapyStatus":"none"},
          "encounter":{"identifier":"enc-002"},
          "order":{"identifier":"order-002"},
          "specimen":{"identifier":"spec-002","collectedAt":"2026-07-31T08:00:00Z"},
          "observation":{"identifier":"obs-002","effectiveAt":"2026-07-31T08:00:00Z","value":12,"unit":"mg/dL","unsupportedField":true},
          "riskFactors":{}
        }
        """;
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v3/clinical/bilirubin/calculate")
        {
            Content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-API-Key", HisApiFactory.ApiKey);
        request.Headers.Add("Idempotency-Key", "canonical-v3-request-002");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("invalid_json", json.RootElement.GetProperty("errorCode").GetString());
        Assert.True(json.RootElement.TryGetProperty("correlationId", out _));
    }

    [Fact]
    public async Task OpenApiDocument_ContainsV3ContractAndApiKeySecurityScheme()
    {
        var response = await _client.GetAsync("/openapi/v3.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("3.1.1", json.RootElement.GetProperty("openapi").GetString());
        Assert.True(json.RootElement.GetProperty("paths").TryGetProperty("/api/v3/clinical/bilirubin/calculate", out _));
        Assert.True(json.RootElement.GetProperty("components").GetProperty("securitySchemes").TryGetProperty("ApiKey", out _));
    }

    [Fact]
    public async Task Readiness_ReturnsClinicalEngineVersionWhenDependenciesPass()
    {
        var response = await _client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Ready", json.RootElement.GetProperty("status").GetString());
        Assert.Equal(BilirubinEngineMetadata.EngineVersion, json.RootElement.GetProperty("clinicalEngine").GetString());
    }

    [Fact]
    public async Task V3Calculate_ConcurrentCapacitySmoke_MeetsP95AndErrorGate()
    {
        using var factory = new HisApiFactory();
        using var client = factory.CreateClient();
        var calls = Enumerable.Range(0, 20).Select(async index =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v3/clinical/bilirubin/calculate")
            {
                Content = JsonContent.Create(ValidV3Request())
            };
            request.Headers.Add("X-API-Key", HisApiFactory.ApiKey);
            request.Headers.Add("Idempotency-Key", $"capacity-smoke-{Guid.NewGuid():N}-{index}");
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var response = await client.SendAsync(request);
            stopwatch.Stop();
            return (response.StatusCode, stopwatch.ElapsedMilliseconds);
        });

        var results = await Task.WhenAll(calls);
        var latencies = results.Select(result => result.ElapsedMilliseconds).OrderBy(value => value).ToArray();
        var p95 = latencies[(int)Math.Ceiling(latencies.Length * .95) - 1];

        Assert.All(results, result => Assert.Equal(HttpStatusCode.OK, result.StatusCode));
        Assert.True(p95 < 2000, $"p95 {p95}ms vượt SLO smoke gate 2000ms.");
    }

    [Fact]
    public async Task V3Calculate_TenantKillSwitch_ReturnsRetryable503()
    {
        using var factory = new HisApiFactory(new Dictionary<string, string?>
        {
            ["HisRollout:DisabledTenants:0"] = "legacy"
        });
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v3/clinical/bilirubin/calculate")
        {
            Content = JsonContent.Create(ValidV3Request())
        };
        request.Headers.Add("X-API-Key", HisApiFactory.ApiKey);
        request.Headers.Add("Idempotency-Key", "rollout-disabled-001");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.True(response.Headers.Contains("Retry-After"));
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("tenant_rollout_disabled", json.RootElement.GetProperty("errorCode").GetString());
        Assert.True(json.RootElement.GetProperty("retryable").GetBoolean());
    }

    [Fact]
    public async Task FhirR4Bundle_ReturnsDiagnosticReportAndDerivedObservation()
    {
        var response = await SendFhirAsync("fhir-valid-request-001", BuildFhirBundle("mg/dL", "1975-2"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/fhir+json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Bundle", json.RootElement.GetProperty("resourceType").GetString());
        var resourceTypes = json.RootElement.GetProperty("entry").EnumerateArray()
            .Select(entry => entry.GetProperty("resource").GetProperty("resourceType").GetString())
            .ToArray();
        Assert.Contains("Observation", resourceTypes);
        Assert.Contains("DiagnosticReport", resourceTypes);
    }

    [Fact]
    public async Task FhirR4Bundle_InvalidUcumUnit_ReturnsOperationOutcome()
    {
        var response = await SendFhirAsync("fhir-invalid-unit-001", BuildFhirBundle("mg", "1975-2"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("OperationOutcome", json.RootElement.GetProperty("resourceType").GetString());
        Assert.Contains("mg/dL", json.RootElement.GetProperty("issue")[0].GetProperty("diagnostics").GetString());
    }

    [Fact]
    public async Task FhirR4Bundle_InvalidLoincCode_ReturnsOperationOutcome()
    {
        var response = await SendFhirAsync("fhir-invalid-loinc-001", BuildFhirBundle("mg/dL", "0000-0"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("OperationOutcome", json.RootElement.GetProperty("resourceType").GetString());
        Assert.Contains("LOINC", json.RootElement.GetProperty("issue")[0].GetProperty("diagnostics").GetString());
    }

    [Fact]
    public async Task FhirR4Bundle_MissingFacilityTag_ReturnsOperationOutcome()
    {
        var payload = JsonSerializer.SerializeToNode(BuildFhirBundle("mg/dL", "1975-2"))!.AsObject();
        payload.Remove("meta");

        var response = await SendFhirAsync("fhir-missing-facility-001", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("OperationOutcome", json.RootElement.GetProperty("resourceType").GetString());
        Assert.Contains("meta", json.RootElement.GetProperty("issue")[0].GetProperty("diagnostics").GetString());
    }

    [Fact]
    public async Task FhirR4Metadata_ReturnsCapabilityStatement()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v3/fhir/R4/metadata");
        request.Headers.Add("X-API-Key", HisApiFactory.ApiKey);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("CapabilityStatement", json.RootElement.GetProperty("resourceType").GetString());
        Assert.Equal("4.0.1", json.RootElement.GetProperty("fhirVersion").GetString());
    }

    [Fact]
    public async Task Hl7OruR01_ValidMessage_ReturnsApplicationAcceptAndZbr()
    {
        const string controlId = "HL7MSG001";
        var response = await SendHl7Async(controlId, BuildHl7Message(controlId, "mg/dL"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/hl7-v2", response.Content.Headers.ContentType?.MediaType);
        var ack = await response.Content.ReadAsStringAsync();
        Assert.Contains($"MSA|AA|{controlId}", ack);
        Assert.Contains("\rZBR|calc_", ack);
        Assert.Contains("|AAP2022+NICECG98|baseline-1", ack);
    }

    [Fact]
    public async Task Hl7OruR01_InvalidUnit_ReturnsApplicationError()
    {
        const string controlId = "HL7MSG002";
        var response = await SendHl7Async(controlId, BuildHl7Message(controlId, "mg"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var ack = await response.Content.ReadAsStringAsync();
        Assert.Contains($"MSA|AE|{controlId}", ack);
        Assert.Contains("\rERR|", ack);
    }

    [Fact]
    public async Task Hl7OruR01_IdempotencyKeyMustEqualMsh10()
    {
        var response = await SendHl7Async("DIFFERENTKEY", BuildHl7Message("HL7MSG003", "mg/dL"));

        var ack = await response.Content.ReadAsStringAsync();
        Assert.Contains("MSA|AE|HL7MSG003", ack);
        Assert.Contains("Idempotency-Key", ack);
    }

    [Fact]
    public async Task Hl7OruR01_DuplicateMessageControlId_ReplaysSameAck()
    {
        const string controlId = "HL7MSG004";
        var message = BuildHl7Message(controlId, "mg/dL");
        var first = await SendHl7Async(controlId, message);
        var second = await SendHl7Async(controlId, message);

        Assert.Equal(await first.Content.ReadAsStringAsync(), await second.Content.ReadAsStringAsync());
        Assert.Equal("true", second.Headers.GetValues("Idempotency-Replayed").Single());
    }

    [Fact]
    public async Task FhirDuplicate_ReplaysWithFhirMediaType()
    {
        var payload = BuildFhirBundle("mg/dL", "1975-2");
        var first = await SendFhirAsync("fhir-media-replay-001", payload);
        var second = await SendFhirAsync("fhir-media-replay-001", payload);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal("application/fhir+json", second.Content.Headers.ContentType?.MediaType);
        Assert.Equal("true", second.Headers.GetValues("Idempotency-Replayed").Single());
    }

    [Fact]
    public async Task FhirNonObjectPayload_ReturnsOperationOutcome400_Not500()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v3/fhir/R4/$bilirubin-calculate")
        {
            Content = new StringContent("\"not-a-bundle\"", System.Text.Encoding.UTF8, "application/fhir+json")
        };
        request.Headers.Add("X-API-Key", HisApiFactory.ApiKey);
        request.Headers.Add("Idempotency-Key", "fhir-non-object-001");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/fhir+json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("OperationOutcome", json.RootElement.GetProperty("resourceType").GetString());
    }

    [Fact]
    public async Task RestFhirHl7_ShadowThresholdsAreEquivalent()
    {
        using var restRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v3/clinical/bilirubin/calculate")
        {
            Content = JsonContent.Create(ValidV3Request())
        };
        restRequest.Headers.Add("X-API-Key", HisApiFactory.ApiKey);
        restRequest.Headers.Add("Idempotency-Key", "shadow-rest-001");
        var restResponse = await _client.SendAsync(restRequest);
        var fhirResponse = await SendFhirAsync("shadow-fhir-001", BuildFhirBundle("mg/dL", "1975-2"));
        const string hl7ControlId = "SHADOWHL7001";
        var hl7Response = await SendHl7Async(hl7ControlId, BuildHl7Message(hl7ControlId, "mg/dL"));

        Assert.Equal(HttpStatusCode.OK, restResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, fhirResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, hl7Response.StatusCode);

        using var restJson = JsonDocument.Parse(await restResponse.Content.ReadAsStringAsync());
        var restThresholds = restJson.RootElement.GetProperty("thresholds");
        var expected = new[]
        {
            restThresholds.GetProperty("phototherapyMgDl").GetDecimal(),
            restThresholds.GetProperty("escalationOfCareMgDl").GetDecimal(),
            restThresholds.GetProperty("exchangeTransfusionMgDl").GetDecimal()
        };

        using var fhirJson = JsonDocument.Parse(await fhirResponse.Content.ReadAsStringAsync());
        var observation = fhirJson.RootElement.GetProperty("entry").EnumerateArray()
            .Select(entry => entry.GetProperty("resource"))
            .Single(resource => resource.GetProperty("resourceType").GetString() == "Observation");
        var fhirThresholds = observation.GetProperty("component").EnumerateArray()
            .ToDictionary(
                component => component.GetProperty("code").GetProperty("coding")[0].GetProperty("code").GetString()!,
                component => component.GetProperty("valueQuantity").GetProperty("value").GetDecimal(),
                StringComparer.Ordinal);
        var actualFhir = new[]
        {
            fhirThresholds["phototherapy-threshold"],
            fhirThresholds["escalation-of-care-threshold"],
            fhirThresholds["exchange-transfusion-threshold"]
        };

        var ack = await hl7Response.Content.ReadAsStringAsync();
        var zbr = ack.Split('\r', StringSplitOptions.RemoveEmptyEntries)
            .Single(segment => segment.StartsWith("ZBR|", StringComparison.Ordinal))
            .Split('|');
        var actualHl7 = new[]
        {
            decimal.Parse(zbr[3], System.Globalization.CultureInfo.InvariantCulture),
            decimal.Parse(zbr[4], System.Globalization.CultureInfo.InvariantCulture),
            decimal.Parse(zbr[5], System.Globalization.CultureInfo.InvariantCulture)
        };

        Assert.Equal(expected, actualFhir);
        Assert.Equal(expected, actualHl7);
    }

    private async Task<HttpResponseMessage> SendCalculationAsync(
        string idempotencyKey,
        YeuCauTinhToanBilirubinDto payload)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v2/clinical/bilirubin/calculate")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("X-API-Key", HisApiFactory.ApiKey);
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> SendFhirAsync(string idempotencyKey, object payload)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v3/fhir/R4/$bilirubin-calculate")
        {
            Content = JsonContent.Create(
                payload,
                mediaType: System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/fhir+json"))
        };
        request.Headers.Add("X-API-Key", HisApiFactory.ApiKey);
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> SendHl7Async(string idempotencyKey, string message)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v3/hl7/v251/oru-r01")
        {
            Content = new StringContent(message, System.Text.Encoding.UTF8, "application/hl7-v2")
        };
        request.Headers.Add("X-API-Key", HisApiFactory.ApiKey);
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return await _client.SendAsync(request);
    }

    private static YeuCauTinhToanBilirubinDto ValidRequest() => new()
    {
        TuoiTheoGio = 48,
        TongBilirubin = 12m,
        DonViDo = DonViDo.MgDl,
        TuoiThaiTuan = 38,
        TrangThaiChieuDen = TrangThaiChieuDen.KhongChieuDen,
        YeuToNguyCo = new YeuToNguyCoThanKinhDto()
    };

    private static HisClinicalCalculationRequest ValidV3Request() => new()
    {
        Source = new HisSourceSystemDto { System = "HIS-A", Facility = "FAC-A", MessageId = "msg-001" },
        Patient = new HisPatientContextDto
        {
            Identifier = "patient-001",
            AssigningAuthority = "FAC-A",
            AgeHours = 48,
            GestationalAgeWeeks = 38,
            PhototherapyStatus = "none"
        },
        Encounter = new HisEncounterReferenceDto { Identifier = "enc-001" },
        Order = new HisOrderReferenceDto { Identifier = "order-001" },
        Specimen = new HisSpecimenReferenceDto
        {
            Identifier = "spec-001",
            CollectedAt = new DateTimeOffset(2026, 7, 31, 8, 0, 0, TimeSpan.Zero)
        },
        Observation = new HisBilirubinObservationDto
        {
            Identifier = "obs-001",
            EffectiveAt = new DateTimeOffset(2026, 7, 31, 8, 0, 0, TimeSpan.Zero),
            Value = 12m,
            Unit = "mg/dL"
        },
        RiskFactors = new HisRiskFactorsDto()
    };

    private static object BuildFhirBundle(string unit, string loincCode) => new
    {
        resourceType = "Bundle",
        meta = new
        {
            profile = new[] { "https://bilitool.vn/fhir/StructureDefinition/bilitool-bilirubin-bundle" },
            tag = new[] { new { system = "https://bilitool.vn/fhir/CodeSystem/facility", code = "FAC-A" } }
        },
        type = "transaction",
        identifier = new { system = "https://hospital.example/his", value = "msg-fhir-001" },
        entry = new object[]
        {
            new
            {
                resource = new
                {
                    resourceType = "Patient",
                    id = "patient-fhir-001",
                    identifier = new[] { new { system = "https://hospital.example/mrn", value = "MRN-001" } },
                    extension = new object[]
                    {
                        new { url = "https://bilitool.vn/fhir/StructureDefinition/age-hours", valueDecimal = 48m }
                    }
                }
            },
            new { resource = new { resourceType = "Encounter", id = "enc-fhir-001" } },
            new
            {
                resource = new
                {
                    resourceType = "ServiceRequest",
                    id = "order-fhir-001",
                    status = "active",
                    intent = "order",
                    extension = new object[]
                    {
                        new { url = "https://bilitool.vn/fhir/StructureDefinition/gestational-age-weeks", valueInteger = 38 },
                        new { url = "https://bilitool.vn/fhir/StructureDefinition/phototherapy-status", valueString = "none" }
                    }
                }
            },
            new
            {
                resource = new
                {
                    resourceType = "Specimen",
                    id = "spec-fhir-001",
                    collection = new { collectedDateTime = "2026-07-31T08:00:00Z" }
                }
            },
            new
            {
                resource = new
                {
                    resourceType = "Observation",
                    id = "obs-fhir-001",
                    status = "final",
                    code = new { coding = new[] { new { system = "http://loinc.org", code = loincCode } } },
                    subject = new { reference = "Patient/patient-fhir-001" },
                    encounter = new { reference = "Encounter/enc-fhir-001" },
                    specimen = new { reference = "Specimen/spec-fhir-001" },
                    effectiveDateTime = "2026-07-31T08:00:00Z",
                    valueQuantity = new { value = 12m, system = "http://unitsofmeasure.org", code = unit }
                }
            }
        }
    };

    private static string BuildHl7Message(string controlId, string unit) => string.Join('\r', new[]
    {
        $"MSH|^~\\&|HIS-A|FAC-A|BILITOOL|BILITOOL|20260731080000+0000||ORU^R01^ORU_R01|{controlId}|P|2.5.1",
        "PID|1||MRN-001^^^FAC-A||",
        "PV1|1|I|||||||||||||||||ENC-001",
        "ORC|NW|ORDER-001",
        "OBR|1|ORDER-001|SPEC-001|1975-2^Bilirubin.total^LN|||20260731080000+0000",
        $"OBX|1|NM|1975-2^Bilirubin.total^LN||12|{unit}^{unit}^UCUM|||||F|||20260731080000+0000",
        "OBX|2|NM|BILI_AGE_HOURS^Age hours^99BILITOOL||48|h^hour^UCUM|||||F",
        "OBX|3|NM|BILI_GA_WEEKS^Gestational age weeks^99BILITOOL||38|wk^week^UCUM|||||F",
        "OBX|4|ST|BILI_PHOTOTHERAPY_STATUS^Phototherapy status^99BILITOOL||none||||||F"
    }) + '\r';
}

public class HisApiFactory : WebApplicationFactory<Program>
{
    public const string ApiKey = "integration-test-api-key-at-least-32-characters";
    private readonly IReadOnlyDictionary<string, string?> _configurationOverrides;

    public HisApiFactory() : this(null)
    {
    }

    internal HisApiFactory(IReadOnlyDictionary<string, string?>? configurationOverrides)
    {
        _configurationOverrides = configurationOverrides ?? new Dictionary<string, string?>();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiSettings:EnableLegacyApiKeys"] = "true",
                ["ApiSettings:AllowedApiKeys:0"] = ApiKey
            });
            configuration.AddInMemoryCollection(_configurationOverrides);
        });
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IHostedService>();
            services.RemoveAll<DbContextOptions<BiliToolDbContext>>();
            services.RemoveAll<BiliToolDbContext>();
            services.AddDbContext<BiliToolDbContext>(options =>
                options.UseInMemoryDatabase($"his-api-tests-{Guid.NewGuid():N}"));
            services.RemoveAll<IClinicalAuditService>();
            services.AddSingleton<IClinicalAuditService, NoOpClinicalAuditService>();
            services.RemoveAll<IHisIdempotencyService>();
            services.AddSingleton<IHisIdempotencyService, InMemoryHisIdempotencyService>();
        });
    }

    private sealed class NoOpClinicalAuditService : IClinicalAuditService
    {
        public Task TryRecordCalculationAsync(
            object request,
            object response,
            BilirubinCalculationTrace trace,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class InMemoryHisIdempotencyService : IHisIdempotencyService
    {
        private readonly object _lock = new();
        private readonly Dictionary<string, Entry> _entries = new(StringComparer.Ordinal);

        public Task<HisIdempotencyAcquireResult> AcquireAsync(
            string tenantId,
            string apiClientId,
            string idempotencyKey,
            string requestHash,
            CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                var key = $"{tenantId}:{apiClientId}:{idempotencyKey}";
                if (!_entries.TryGetValue(key, out var entry))
                {
                    _entries[key] = new Entry(requestHash);
                    return Task.FromResult(new HisIdempotencyAcquireResult(HisIdempotencyDecision.Acquired));
                }

                if (!string.Equals(entry.RequestHash, requestHash, StringComparison.Ordinal))
                    return Task.FromResult(new HisIdempotencyAcquireResult(HisIdempotencyDecision.PayloadConflict));

                return Task.FromResult(entry.Completed
                    ? new HisIdempotencyAcquireResult(
                        HisIdempotencyDecision.Replay,
                        entry.ResultId,
                        entry.StatusCode,
                        entry.ResponseJson,
                        entry.ResponseContentType)
                    : new HisIdempotencyAcquireResult(HisIdempotencyDecision.InProgress));
            }
        }

        public Task CompleteAsync(
            string tenantId,
            string apiClientId,
            string idempotencyKey,
            string resultId,
            int responseStatusCode,
            string responseJson,
            string responseContentType,
            CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                var entry = _entries[$"{tenantId}:{apiClientId}:{idempotencyKey}"];
                entry.Completed = true;
                entry.ResultId = resultId;
                entry.StatusCode = responseStatusCode;
                entry.ResponseJson = responseJson;
                entry.ResponseContentType = responseContentType;
            }
            return Task.CompletedTask;
        }

        public Task ReleaseAsync(
            string tenantId,
            string apiClientId,
            string idempotencyKey,
            CancellationToken cancellationToken = default)
        {
            lock (_lock) _entries.Remove($"{tenantId}:{apiClientId}:{idempotencyKey}");
            return Task.CompletedTask;
        }

        private sealed class Entry(string requestHash)
        {
            public string RequestHash { get; } = requestHash;
            public bool Completed { get; set; }
            public string? ResultId { get; set; }
            public int? StatusCode { get; set; }
            public string? ResponseJson { get; set; }
            public string? ResponseContentType { get; set; }
        }
    }
}
