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
        
        // ── Infrastructure Services ───────────────────────────
        services.AddTransient<BiliTool.Vn.Application.Services.IEmailService, BiliTool.Vn.Infrastructure.Services.SmtpEmailService>();
        services.AddScoped<BiliTool.Vn.Application.Services.IAuthService, BiliTool.Vn.Infrastructure.Services.AuthService>();

        // ── CQRS Handlers trong Infrastructure ────────────────
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(InfrastructureServiceRegistration).Assembly));

        return services;
    }
}
