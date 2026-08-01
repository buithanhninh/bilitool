using BiliTool.Vn.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BiliTool.Vn.Infrastructure.Services;

public sealed class HisClientBootstrapService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<HisClientBootstrapService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var section = configuration.GetSection("ApiSettings:BootstrapClient");
        var apiKey = section["ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey)) return;

        try
        {
            using var scope = scopeFactory.CreateScope();
            var provisioning = scope.ServiceProvider.GetRequiredService<IHisClientProvisioningService>();
            await provisioning.ProvisionAsync(new HisClientProvisioningRequest(
                section["TenantCode"] ?? string.Empty,
                section["TenantName"] ?? string.Empty,
                section["ClientCode"] ?? string.Empty,
                section["DisplayName"] ?? string.Empty,
                apiKey,
                section["Scopes"] ?? "bilirubin:calculate bilirubin:metadata",
                DateTime.TryParse(section["ExpiresAt"], out var expiresAt) ? expiresAt.ToUniversalTime() : null), stoppingToken);
            logger.LogInformation(
                "HIS API client bootstrap hoàn tất cho tenant {TenantCode}, client {ClientCode}. Xóa bootstrap secret khỏi environment.",
                section["TenantCode"], section["ClientCode"]);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "HIS API client bootstrap thất bại.");
        }
    }
}
