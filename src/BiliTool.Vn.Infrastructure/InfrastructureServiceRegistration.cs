using BiliTool.Vn.Domain.Clinical.Bilirubin;
using BiliTool.Vn.Domain.Services;
using BiliTool.Vn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BiliTool.Vn.Infrastructure;

/// <summary>Cấu hình Dependency Injection cho Infrastructure Layer</summary>
public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── PostgreSQL + EF Core ──────────────────────────────
        services.AddDbContext<BiliToolDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("PostgreSQL"),
                npgsql => npgsql.MigrationsAssembly(typeof(BiliToolDbContext).Assembly.FullName)
            )
        );

        // ── Domain Services ───────────────────────────────────
        services.AddScoped<IMayTinhBilirubin, MayTinhBilirubin>();
        services.AddScoped<IBilirubinClinicalFacade, BilirubinClinicalFacade>();
        
        // ── Infrastructure Services ───────────────────────────
        services.AddTransient<BiliTool.Vn.Application.Services.IEmailService, BiliTool.Vn.Infrastructure.Services.SmtpEmailService>();
        services.AddScoped<BiliTool.Vn.Application.Services.IAuthService, BiliTool.Vn.Infrastructure.Services.AuthService>();
        services.AddScoped<BiliTool.Vn.Application.Services.IClinicalAuditService, BiliTool.Vn.Infrastructure.Services.ClinicalAuditService>();
        services.AddScoped<BiliTool.Vn.Application.Services.IAdminAuditService, BiliTool.Vn.Infrastructure.Services.AdminAuditService>();
        services.AddScoped<BiliTool.Vn.Application.Services.IHisApiClientAuthenticator, BiliTool.Vn.Infrastructure.Services.HisApiClientAuthenticator>();
        services.AddScoped<BiliTool.Vn.Application.Services.IHisIdempotencyService, BiliTool.Vn.Infrastructure.Services.HisIdempotencyService>();
        services.AddScoped<BiliTool.Vn.Application.Services.IHisClientProvisioningService, BiliTool.Vn.Infrastructure.Services.HisClientProvisioningService>();
        services.AddScoped<BiliTool.Vn.Application.Services.IHisWebhookProvisioningService, BiliTool.Vn.Infrastructure.Services.HisWebhookProvisioningService>();
        services.AddScoped<BiliTool.Vn.Application.Services.IHisOutboxOperationsService, BiliTool.Vn.Infrastructure.Services.HisOutboxOperationsService>();
        services.AddScoped<BiliTool.Vn.Application.Services.IClinicalAuditGovernanceService, BiliTool.Vn.Infrastructure.Services.ClinicalAuditGovernanceService>();
        services.AddScoped<BiliTool.Vn.Infrastructure.Services.HisWebhookSender>();
        services.AddSingleton<BiliTool.Vn.Infrastructure.Services.HisWebhookResilienceGate>();
        services.AddHttpClient("HisWebhook", client => client.Timeout = TimeSpan.FromSeconds(10))
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                ConnectTimeout = TimeSpan.FromSeconds(5),
                PooledConnectionLifetime = TimeSpan.FromMinutes(5)
            });
        services.AddHostedService<BiliTool.Vn.Infrastructure.Services.HisOutboxDeliveryService>();
        services.AddHostedService<BiliTool.Vn.Infrastructure.Services.HisClientBootstrapService>();
        services.AddHostedService<BiliTool.Vn.Infrastructure.Services.ClinicalAuditRetentionService>();

        // ── CQRS Handlers trong Infrastructure ────────────────
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(InfrastructureServiceRegistration).Assembly));

        return services;
    }
}
