using System.Security.Cryptography;
using System.Text;
using BiliTool.Vn.Application.Services;
using BiliTool.Vn.Domain.Entities;
using BiliTool.Vn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BiliTool.Vn.Infrastructure.Services;

public sealed class HisClientProvisioningService(BiliToolDbContext dbContext) : IHisClientProvisioningService
{
    public async Task ProvisionAsync(
        HisClientProvisioningRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateApiKey(request.ApiKey);
        var certificateFingerprint = NormalizeCertificateFingerprint(request.CertificateFingerprint, request.RequireMutualTls);
        var tenantCode = NormalizeCode(request.TenantCode);
        var clientCode = NormalizeCode(request.ClientCode);
        var tenant = await dbContext.HisTenants.SingleOrDefaultAsync(
            item => item.Code == tenantCode,
            cancellationToken);

        if (tenant == null)
        {
            tenant = new HisTenant
            {
                Code = tenantCode,
                Name = request.TenantName.Trim(),
                IsActive = true
            };
            dbContext.HisTenants.Add(tenant);
        }

        var client = await dbContext.HisApiClients.SingleOrDefaultAsync(
            item => item.TenantId == tenant.Id && item.ClientCode == clientCode,
            cancellationToken);
        var hash = Hash(request.ApiKey);

        if (client == null)
        {
            client = new HisApiClient
            {
                Tenant = tenant,
                ClientCode = clientCode
            };
            dbContext.HisApiClients.Add(client);
        }

        client.DisplayName = request.DisplayName.Trim();
        client.ApiKeyHash = hash;
        client.KeyFingerprint = HisApiClientAuthenticator.CreateFingerprint(hash);
        client.PreviousApiKeyHash = null;
        client.PreviousKeyFingerprint = null;
        client.PreviousKeyExpiresAt = null;
        client.RequireMutualTls = request.RequireMutualTls;
        client.CertificateFingerprint = certificateFingerprint;
        client.PreviousCertificateFingerprint = null;
        client.PreviousCertificateExpiresAt = null;
        client.Scopes = NormalizeScopes(request.Scopes);
        client.ExpiresAt = request.ExpiresAt;
        client.IsActive = true;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RotateKeyAsync(
        string tenantCode,
        string clientCode,
        string newApiKey,
        TimeSpan overlapWindow,
        CancellationToken cancellationToken = default)
    {
        ValidateApiKey(newApiKey);
        if (overlapWindow < TimeSpan.Zero || overlapWindow > TimeSpan.FromDays(7))
            throw new ArgumentOutOfRangeException(nameof(overlapWindow), "Cửa sổ overlap phải từ 0 đến 7 ngày.");
        var client = await FindClientAsync(tenantCode, clientCode, cancellationToken);
        var hash = Hash(newApiKey);
        client.PreviousApiKeyHash = client.ApiKeyHash;
        client.PreviousKeyFingerprint = client.KeyFingerprint;
        client.PreviousKeyExpiresAt = DateTime.UtcNow.Add(overlapWindow);
        client.ApiKeyHash = hash;
        client.KeyFingerprint = HisApiClientAuthenticator.CreateFingerprint(hash);
        client.IsActive = true;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAsync(
        string tenantCode,
        string clientCode,
        CancellationToken cancellationToken = default)
    {
        var client = await FindClientAsync(tenantCode, clientCode, cancellationToken);
        client.IsActive = false;
        client.PreviousApiKeyHash = null;
        client.PreviousKeyFingerprint = null;
        client.PreviousKeyExpiresAt = null;
        client.PreviousCertificateFingerprint = null;
        client.PreviousCertificateExpiresAt = null;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RotateCertificateAsync(
        string tenantCode,
        string clientCode,
        string newCertificateFingerprint,
        TimeSpan overlapWindow,
        CancellationToken cancellationToken = default)
    {
        if (overlapWindow < TimeSpan.Zero || overlapWindow > TimeSpan.FromDays(7))
            throw new ArgumentOutOfRangeException(nameof(overlapWindow), "Cửa sổ overlap phải từ 0 đến 7 ngày.");
        var normalizedFingerprint = NormalizeCertificateFingerprint(newCertificateFingerprint, true)!;
        var client = await FindClientAsync(tenantCode, clientCode, cancellationToken);
        client.PreviousCertificateFingerprint = client.CertificateFingerprint;
        client.PreviousCertificateExpiresAt = client.CertificateFingerprint == null ? null : DateTime.UtcNow.Add(overlapWindow);
        client.CertificateFingerprint = normalizedFingerprint;
        client.RequireMutualTls = true;
        client.IsActive = true;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<HisApiClient> FindClientAsync(
        string tenantCode,
        string clientCode,
        CancellationToken cancellationToken)
    {
        var normalizedTenant = NormalizeCode(tenantCode);
        var normalizedClient = NormalizeCode(clientCode);
        return await dbContext.HisApiClients.Include(item => item.Tenant).SingleOrDefaultAsync(
                   item => item.Tenant.Code == normalizedTenant && item.ClientCode == normalizedClient,
                   cancellationToken)
               ?? throw new KeyNotFoundException("Không tìm thấy HIS API client.");
    }

    private static byte[] Hash(string apiKey) => SHA256.HashData(Encoding.UTF8.GetBytes(apiKey));

    private static void ValidateApiKey(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Length < 32)
            throw new ArgumentException("API key phải có ít nhất 32 ký tự ngẫu nhiên.", nameof(apiKey));
    }

    private static string? NormalizeCertificateFingerprint(string? value, bool required)
    {
        var normalized = value?.Replace(":", string.Empty, StringComparison.Ordinal).Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(normalized))
        {
            if (required) throw new ArgumentException("Certificate fingerprint SHA-256 là bắt buộc khi bật mTLS.", nameof(value));
            return null;
        }
        if (normalized.Length != 64 || normalized.Any(character => !Uri.IsHexDigit(character)))
            throw new ArgumentException("Certificate fingerprint phải là SHA-256 gồm 64 ký tự hex.", nameof(value));
        return normalized;
    }

    private static string NormalizeCode(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        if (normalized.Length is < 2 or > 64 ||
            normalized.Any(character => !char.IsLetterOrDigit(character) && character is not '-' and not '_'))
            throw new ArgumentException("Code phải dài 2-64 ký tự và chỉ gồm chữ, số, '-' hoặc '_'.");
        return normalized;
    }

    private static string NormalizeScopes(string scopes)
    {
        var normalized = scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (normalized.Length == 0) throw new ArgumentException("API client phải có ít nhất một scope.", nameof(scopes));
        return string.Join(' ', normalized);
    }
}
