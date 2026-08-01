using BiliTool.Vn.Application.Services;
using BiliTool.Vn.Infrastructure.Persistence;
using BiliTool.Vn.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace BiliTool.Vn.Domain.Tests;

public class HisClientProvisioningTests
{
    [Fact]
    public async Task Provision_StoresHashAndAuthenticatesClientIdentity()
    {
        await using var db = CreateDbContext();
        var provisioning = new HisClientProvisioningService(db);
        const string apiKey = "hospital-secret-key-with-more-than-32-characters";

        await provisioning.ProvisionAsync(new HisClientProvisioningRequest(
            "Hospital-A",
            "Hospital A",
            "HIS-Main",
            "HIS Main",
            apiKey,
            "bilirubin:metadata bilirubin:calculate"));

        var stored = await db.HisApiClients.Include(client => client.Tenant).SingleAsync();
        Assert.DoesNotContain(apiKey, Convert.ToHexString(stored.ApiKeyHash), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(32, stored.ApiKeyHash.Length);
        Assert.Equal("hospital-a", stored.Tenant.Code);
        Assert.Equal("his-main", stored.ClientCode);

        var authenticator = new HisApiClientAuthenticator(db, EmptyConfiguration());
        var identity = await authenticator.AuthenticateAsync(apiKey);
        Assert.NotNull(identity);
        Assert.False(identity!.IsLegacy);
        Assert.Contains("bilirubin:calculate", identity.Scopes);
    }

    [Fact]
    public async Task RotateAndRevoke_InvalidatesPreviousCredentials()
    {
        await using var db = CreateDbContext();
        var provisioning = new HisClientProvisioningService(db);
        const string oldKey = "old-hospital-secret-key-more-than-32-characters";
        const string newKey = "new-hospital-secret-key-more-than-32-characters";
        var request = new HisClientProvisioningRequest(
            "hospital-a", "Hospital A", "his-main", "HIS Main", oldKey, "bilirubin:calculate");
        await provisioning.ProvisionAsync(request);
        var authenticator = new HisApiClientAuthenticator(db, EmptyConfiguration());

        await provisioning.RotateKeyAsync("hospital-a", "his-main", newKey, TimeSpan.FromHours(1));
        Assert.NotNull(await authenticator.AuthenticateAsync(oldKey));
        Assert.NotNull(await authenticator.AuthenticateAsync(newKey));

        await provisioning.RevokeAsync("hospital-a", "his-main");
        Assert.Null(await authenticator.AuthenticateAsync(newKey));
    }

    [Fact]
    public async Task MutualTlsClient_RequiresMatchingCertificateAndSupportsRotationOverlap()
    {
        await using var db = CreateDbContext();
        var provisioning = new HisClientProvisioningService(db);
        const string apiKey = "mtls-hospital-secret-key-more-than-32-characters";
        const string oldCertificate = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
        const string newCertificate = "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB";

        await provisioning.ProvisionAsync(new HisClientProvisioningRequest(
            "hospital-a",
            "Hospital A",
            "his-mtls",
            "HIS mTLS",
            apiKey,
            "bilirubin:calculate",
            RequireMutualTls: true,
            CertificateFingerprint: oldCertificate));

        var authenticator = new HisApiClientAuthenticator(db, EmptyConfiguration());
        Assert.Null(await authenticator.AuthenticateAsync(apiKey));
        Assert.Null(await authenticator.AuthenticateAsync(apiKey, newCertificate));
        Assert.NotNull(await authenticator.AuthenticateAsync(apiKey, oldCertificate));

        await provisioning.RotateCertificateAsync("hospital-a", "his-mtls", newCertificate, TimeSpan.FromHours(1));
        Assert.NotNull(await authenticator.AuthenticateAsync(apiKey, oldCertificate));
        Assert.NotNull(await authenticator.AuthenticateAsync(apiKey, newCertificate));
    }

    [Fact]
    public async Task MutualTlsProvision_RejectsMissingOrMalformedCertificateFingerprint()
    {
        await using var db = CreateDbContext();
        var provisioning = new HisClientProvisioningService(db);
        const string apiKey = "mtls-hospital-secret-key-more-than-32-characters";

        await Assert.ThrowsAsync<ArgumentException>(() => provisioning.ProvisionAsync(new HisClientProvisioningRequest(
            "hospital-a", "Hospital A", "his-mtls", "HIS mTLS", apiKey, "bilirubin:calculate",
            RequireMutualTls: true)));
        await Assert.ThrowsAsync<ArgumentException>(() => provisioning.ProvisionAsync(new HisClientProvisioningRequest(
            "hospital-a", "Hospital A", "his-mtls", "HIS mTLS", apiKey, "bilirubin:calculate",
            RequireMutualTls: true, CertificateFingerprint: "not-a-sha256-fingerprint")));
    }

    [Fact]
    public async Task LegacyApiKey_IsRejectedAfterConfiguredMigrationDeadline()
    {
        await using var db = CreateDbContext();
        const string legacyKey = "legacy-secret-key-with-more-than-32-characters";
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ApiSettings:EnableLegacyApiKeys"] = "true",
            ["ApiSettings:AllowedApiKeys:0"] = legacyKey,
            ["ApiSettings:LegacyApiKeysDisableAfter"] = DateTime.UtcNow.AddMinutes(-1).ToString("O")
        }).Build();

        var authenticator = new HisApiClientAuthenticator(db, configuration);

        Assert.Null(await authenticator.AuthenticateAsync(legacyKey));
    }

    private static BiliToolDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BiliToolDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new BiliToolDbContext(options);
    }

    private static IConfiguration EmptyConfiguration() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ApiSettings:EnableLegacyApiKeys"] = "false"
        })
        .Build();
}
