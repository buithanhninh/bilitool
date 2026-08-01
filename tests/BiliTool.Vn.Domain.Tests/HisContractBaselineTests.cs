using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BiliTool.Vn.Application.DTOs;
using BiliTool.Vn.Application.Commands;
using BiliTool.Vn.Application.Services;
using BiliTool.Vn.Domain.Enums;
using BiliTool.Vn.Web.Controllers;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BiliTool.Vn.Domain.Tests;

public sealed class HisContractBaselineTests : IClassFixture<HisApiFactory>
{
    private readonly HttpClient _client;
    private readonly JsonElement _contracts;

    public HisContractBaselineTests(HisApiFactory factory)
    {
        _client = factory.CreateClient();
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory, "contracts", "his-emr", "contract-baseline.json")));
        _contracts = document.RootElement.GetProperty("contracts").Clone();
    }

    [Fact]
    public async Task GoldenContracts_V1V2V3AndProblems_MatchSourceControlledBaseline()
    {
        await AssertContractAsync("v1.calculate.200", await SendAsync("/api/v1/bilirubin/calculate", "baseline-v1-001", LegacyRequest()));
        await AssertContractAsync("v2.calculate.200", await SendAsync("/api/v2/clinical/bilirubin/calculate", "baseline-v2-001", LegacyRequest()));
        await AssertContractAsync("v3.calculate.200", await SendAsync("/api/v3/clinical/bilirubin/calculate", "baseline-v3-001", V3Request()));

        var unauthorized = await _client.PostAsJsonAsync("/api/v3/clinical/bilirubin/calculate", V3Request());
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);
        await AssertContractAsync("problem.401", unauthorized);

        using var invalid = new HttpRequestMessage(HttpMethod.Post, "/api/v3/clinical/bilirubin/calculate")
        {
            Content = JsonContent.Create(new { unsupported = true })
        };
        invalid.Headers.Add("X-API-Key", HisApiFactory.ApiKey);
        invalid.Headers.Add("Idempotency-Key", "baseline-invalid-001");
        var badRequest = await _client.SendAsync(invalid);
        Assert.Equal(HttpStatusCode.BadRequest, badRequest.StatusCode);
        await AssertContractAsync("problem.400", badRequest);

        using var disabledFactory = new HisApiFactory(new Dictionary<string, string?>
        {
            ["HisRollout:V3Enabled"] = "false"
        });
        using var disabledClient = disabledFactory.CreateClient();
        using var disabledRequest = CreateRequest("/api/v3/clinical/bilirubin/calculate", "baseline-disabled-001", V3Request());
        var unavailable = await disabledClient.SendAsync(disabledRequest);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, unavailable.StatusCode);
        await AssertContractAsync("problem.503", unavailable);

        HttpResponseMessage? rateLimited = null;
        for (var attempt = 0; attempt < 40 && rateLimited?.StatusCode != HttpStatusCode.TooManyRequests; attempt++)
        {
            using var metadataRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v2/clinical/bilirubin/guidelines/active");
            metadataRequest.Headers.Add("X-API-Key", HisApiFactory.ApiKey);
            rateLimited = await _client.SendAsync(metadataRequest);
        }
        Assert.NotNull(rateLimited);
        Assert.Equal(HttpStatusCode.TooManyRequests, rateLimited!.StatusCode);
        Assert.True(rateLimited.Headers.Contains("Retry-After"));
        await AssertContractAsync("problem.429", rateLimited);
    }

    [Fact]
    public async Task GoldenContract_InternalFailure_Matches500Baseline()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(item => item.Send(It.IsAny<TinhToanBilirubinCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("synthetic failure"));
        var controller = new BilirubinApiController(
            mediator.Object,
            NullLogger<BilirubinApiController>.Instance,
            Mock.Of<IClinicalRequestContext>())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = Assert.IsType<ObjectResult>(await controller.Calculate(LegacyRequest()));
        Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
        var body = JsonSerializer.SerializeToElement(result.Value, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        AssertContract("problem.500", body);
    }

    private async Task<HttpResponseMessage> SendAsync(string path, string idempotencyKey, object payload)
    {
        using var request = CreateRequest(path, idempotencyKey, payload);
        return await _client.SendAsync(request);
    }

    private static HttpRequestMessage CreateRequest(string path, string idempotencyKey, object payload)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(payload) };
        request.Headers.Add("X-API-Key", HisApiFactory.ApiKey);
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return request;
    }

    private async Task AssertContractAsync(string contractName, HttpResponseMessage response)
    {
        var contract = _contracts.GetProperty(contractName);
        Assert.Equal(contract.GetProperty("mediaType").GetString(), response.Content.Headers.ContentType?.MediaType);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        AssertRequired(contract, body.RootElement);
    }

    private void AssertContract(string contractName, JsonElement body)
    {
        AssertRequired(_contracts.GetProperty(contractName), body);
    }

    private static void AssertRequired(JsonElement contract, JsonElement body)
    {
        foreach (var required in contract.GetProperty("required").EnumerateObject())
        {
            var value = Resolve(body, required.Name);
            Assert.Equal(required.Value.GetString(), TypeName(value));
        }
    }

    private static JsonElement Resolve(JsonElement root, string path)
    {
        var current = root;
        foreach (var segment in path[2..].Split('.'))
        {
            Assert.True(current.TryGetProperty(segment, out var next), $"Contract path thiếu: {path}");
            current = next;
        }
        return current;
    }

    private static string TypeName(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Object => "object",
        JsonValueKind.Array => "array",
        JsonValueKind.String => "string",
        JsonValueKind.Number => "number",
        JsonValueKind.True or JsonValueKind.False => "boolean",
        JsonValueKind.Null => "null",
        _ => throw new InvalidOperationException($"Unsupported JSON kind {value.ValueKind}.")
    };

    private static YeuCauTinhToanBilirubinDto LegacyRequest() => new()
    {
        TuoiTheoGio = 48,
        TongBilirubin = 12,
        DonViDo = DonViDo.MgDl,
        TuoiThaiTuan = 38,
        TrangThaiChieuDen = TrangThaiChieuDen.KhongChieuDen,
        YeuToNguyCo = new YeuToNguyCoThanKinhDto()
    };

    private static object V3Request() => new
    {
        source = new { system = "HIS-A", facility = "FAC-A", messageId = "baseline-message" },
        patient = new { identifier = "baseline-patient", assigningAuthority = "FAC-A", ageHours = 48, gestationalAgeWeeks = 38, phototherapyStatus = "none" },
        encounter = new { identifier = "baseline-encounter" },
        order = new { identifier = "baseline-order" },
        specimen = new { identifier = "baseline-specimen", collectedAt = "2026-08-01T08:00:00Z" },
        observation = new { identifier = "baseline-observation", effectiveAt = "2026-08-01T08:00:00Z", value = 12, unit = "mg/dL" },
        riskFactors = new { }
    };
}
