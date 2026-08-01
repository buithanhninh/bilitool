using System.Security.Cryptography;
using System.Text;
using BiliTool.Vn.Application.Services;
using BiliTool.Vn.Domain.Entities;
using BiliTool.Vn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BiliTool.Vn.Infrastructure.Services;

public sealed class HisApiClientAuthenticator(
    BiliToolDbContext dbContext,
    IConfiguration configuration) : IHisApiClientAuthenticator
{
    public async Task<HisApiClientIdentity?> AuthenticateAsync(
        string apiKey,
        string? certificateFingerprint = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) return null;

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(apiKey));
        var fingerprint = Convert.ToHexString(hash.AsSpan(0, 8));
        var now = DateTime.UtcNow;

        if (LegacyKeysAreEnabled(now))
        {
            var legacyIdentity = AuthenticateLegacyKey(hash, fingerprint);
            if (legacyIdentity != null) return legacyIdentity;
        }

        var candidates = await dbContext.HisApiClients
            .Include(client => client.Tenant)
            .Where(client => (client.KeyFingerprint == fingerprint || client.PreviousKeyFingerprint == fingerprint) &&
                             client.IsActive &&
                             client.Tenant.IsActive &&
                             (!client.ExpiresAt.HasValue || client.ExpiresAt > now))
            .ToListAsync(cancellationToken);

        var normalizedCertificate = NormalizeCertificateFingerprint(certificateFingerprint);
        var client = candidates.FirstOrDefault(candidate =>
            (MatchesCurrent(candidate, hash, fingerprint) || MatchesPrevious(candidate, hash, fingerprint, now)) &&
            MatchesCertificate(candidate, normalizedCertificate, now));

        if (client != null)
        {
            client.LastUsedAt = now;
            await dbContext.SaveChangesAsync(cancellationToken);
            return new HisApiClientIdentity(
                client.TenantId.ToString("N"),
                client.Tenant.Code,
                client.Id.ToString("N"),
                client.ClientCode,
                ParseScopes(client.Scopes),
                false);
        }

        return null;
    }

    private bool LegacyKeysAreEnabled(DateTime now)
    {
        if (!configuration.GetValue("ApiSettings:EnableLegacyApiKeys", true)) return false;
        var disableAfter = configuration.GetValue<DateTime?>("ApiSettings:LegacyApiKeysDisableAfter");
        return !disableAfter.HasValue || disableAfter.Value > now;
    }

    public static string CreateFingerprint(byte[] hash) => Convert.ToHexString(hash.AsSpan(0, 8));

    private static bool MatchesCertificate(HisApiClient client, string? certificateFingerprint, DateTime now)
    {
        if (!client.RequireMutualTls) return true;
        if (certificateFingerprint == null) return false;
        if (string.Equals(client.CertificateFingerprint, certificateFingerprint, StringComparison.Ordinal)) return true;
        return client.PreviousCertificateExpiresAt > now &&
               string.Equals(client.PreviousCertificateFingerprint, certificateFingerprint, StringComparison.Ordinal);
    }

    private static string? NormalizeCertificateFingerprint(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Replace(":", string.Empty, StringComparison.Ordinal).Trim().ToUpperInvariant();

    private HisApiClientIdentity? AuthenticateLegacyKey(byte[] hash, string fingerprint)
    {
        var legacyKeys = configuration.GetSection("ApiSettings:AllowedApiKeys").Get<string[]>() ?? Array.Empty<string>();
        foreach (var legacyKey in legacyKeys.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            var legacyHash = SHA256.HashData(Encoding.UTF8.GetBytes(legacyKey));
            if (CryptographicOperations.FixedTimeEquals(hash, legacyHash))
            {
                return new HisApiClientIdentity(
                    "legacy",
                    "legacy",
                    $"legacy-{fingerprint.ToLowerInvariant()}",
                    "legacy-api-key",
                    new HashSet<string>(StringComparer.Ordinal) { "bilirubin:calculate", "bilirubin:metadata" },
                    true);
            }
        }

        return null;
    }

    private static IReadOnlySet<string> ParseScopes(string scopes) => scopes
        .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .ToHashSet(StringComparer.Ordinal);

    private static bool MatchesCurrent(HisApiClient client, byte[] hash, string fingerprint) =>
        string.Equals(client.KeyFingerprint, fingerprint, StringComparison.Ordinal) &&
        client.ApiKeyHash.Length == hash.Length &&
        CryptographicOperations.FixedTimeEquals(client.ApiKeyHash, hash);

    private static bool MatchesPrevious(HisApiClient client, byte[] hash, string fingerprint, DateTime now) =>
        client.PreviousKeyExpiresAt > now &&
        string.Equals(client.PreviousKeyFingerprint, fingerprint, StringComparison.Ordinal) &&
        client.PreviousApiKeyHash is { } previousHash &&
        previousHash.Length == hash.Length &&
        CryptographicOperations.FixedTimeEquals(previousHash, hash);
}
