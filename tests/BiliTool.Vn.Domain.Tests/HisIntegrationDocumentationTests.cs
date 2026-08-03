using Xunit;

namespace BiliTool.Vn.Domain.Tests;

public sealed class HisIntegrationDocumentationTests
{
    private const string CurrentVersion = "1.4.2";

    [Fact]
    public void PublicGuide_DocumentsCurrentProductionContract()
    {
        var page = Read("documentation", "TichHopHis.razor");

        Assert.Contains($"v{CurrentVersion}", page);
        Assert.Contains("2026-08-03", page);
        Assert.Contains("/api/v3/clinical/bilirubin/calculate", page);
        Assert.Contains("/api/v3/fhir/R4/metadata", page);
        Assert.Contains("/api/v3/fhir/R4/$bilirubin-calculate", page);
        Assert.Contains("/api/v3/hl7/v251/oru-r01", page);
        Assert.Contains("/openapi/v3.json", page);
        Assert.Contains("X-API-Key", page);
        Assert.Contains("Idempotency-Key", page);
        Assert.Contains("X-Correlation-ID", page);
        Assert.Contains("64 KiB", page);
        Assert.Contains("128 KiB", page);
        Assert.Contains("request_too_large", page);
        Assert.Contains("tenant_rollout_disabled", page);
        Assert.Contains("webhook", page, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("sandbox", page, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Legacy REST API v1/v2", page);
        Assert.DoesNotContain("supports two integration modes", page, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ReleaseMetadata_UsesCurrentVersionConsistently()
    {
        var currentReleaseHeader = Read("release-metadata", "CHANGELOG.md")
            .Split('\n')
            .First(line => line.StartsWith("## [", StringComparison.Ordinal));
        Assert.Equal($"## [{CurrentVersion}] - 2026-08-03", currentReleaseHeader);
        Assert.Contains($"@T[\"Version\"] {CurrentVersion}", Read("release-metadata", "MainLayout.razor"));
        Assert.Contains($"\"PhienBan\": \"{CurrentVersion}\"", Read("release-metadata", "appsettings.json"));

        var host = Read("release-metadata", "_Host.cshtml");
        Assert.Contains($"?v={CurrentVersion}", host);
        Assert.DoesNotContain("?v=1.3.16", host);
        Assert.DoesNotContain("?v=1.4.1", host);

        Assert.Contains("bilitool-vn-shell-v25", Read("release-metadata", "service-worker.js"));
    }

    private static string Read(params string[] path) =>
        File.ReadAllText(Path.Combine([AppContext.BaseDirectory, .. path]));
}
