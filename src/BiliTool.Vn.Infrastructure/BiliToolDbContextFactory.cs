using BiliTool.Vn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace BiliTool.Vn.Infrastructure;

/// <summary>
/// Factory dùng bởi EF Design-time tools (dotnet ef).
/// Đọc connection string từ appsettings.json của Web project.
/// </summary>
public class BiliToolDbContextFactory : IDesignTimeDbContextFactory<BiliToolDbContext>
{
    public BiliToolDbContext CreateDbContext(string[] args)
    {
        // Load appsettings.json từ Web project
        var config = new ConfigurationBuilder()
            .AddJsonFile(
                Path.Combine(Directory.GetCurrentDirectory(), 
                    "..", "BiliTool.Vn.Web", "appsettings.json"),
                optional: false)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = config.GetConnectionString("PostgreSQL");

        var optionsBuilder = new DbContextOptionsBuilder<BiliToolDbContext>();
        optionsBuilder.UseNpgsql(connectionString,
            npgsql => npgsql.MigrationsAssembly(typeof(BiliToolDbContext).Assembly.FullName));

        return new BiliToolDbContext(optionsBuilder.Options);
    }
}
