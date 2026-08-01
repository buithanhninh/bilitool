using System.Text.Json;
using BiliTool.Vn.Infrastructure.Persistence;
using BiliTool.Vn.Web.Services.Operations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace BiliTool.Vn.Domain.Tests;

public class OperationalMetricsTests
{
    [Fact]
    public void Snapshot_ContainsHisIntegrationCounters()
    {
        var metrics = new OperationalMetrics();
        metrics.Increment("auth.invalid_key");
        metrics.Increment("auth.invalid_key");
        metrics.Increment("idempotency.replay");

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(metrics.Snapshot()));

        var his = json.RootElement.GetProperty("his");
        Assert.Equal(2, his.GetProperty("auth.invalid_key").GetInt64());
        Assert.Equal(1, his.GetProperty("idempotency.replay").GetInt64());
    }

    [Fact]
    public async Task AlertEvaluation_TriggersOnSloBreach_AndHonorsCooldown()
    {
        var metrics = new OperationalMetrics();
        for (var index = 0; index < 10; index++) metrics.Record("rest-v3", 500, 2500);
        var logger = new CapturingLogger<OperationalAlertService>();
        await using var provider = BuildProvider();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Operations:AlertMinimumRequests"] = "10",
            ["Operations:AlertP95Milliseconds"] = "2000",
            ["Operations:AlertErrorRatePercent"] = "2",
            ["Operations:AlertCooldownMinutes"] = "15"
        }).Build();
        var service = new OperationalAlertService(metrics, provider.GetRequiredService<IServiceScopeFactory>(), configuration, logger);

        Assert.True(await service.EvaluateOnceAsync());
        Assert.False(await service.EvaluateOnceAsync());
        Assert.Single(logger.Warnings);
        Assert.Contains("requests=10", logger.Warnings[0], StringComparison.Ordinal);
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddDbContext<BiliToolDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString("N")));
        return services.BuildServiceProvider();
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Warnings { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning) Warnings.Add(formatter(state, exception));
        }
    }
}
