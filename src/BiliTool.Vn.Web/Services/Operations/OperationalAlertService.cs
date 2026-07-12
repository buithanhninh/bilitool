using BiliTool.Vn.Web.Services.Operations;

namespace BiliTool.Vn.Web.Services.Operations;

public sealed class OperationalAlertService(OperationalMetrics metrics, IConfiguration configuration, ILogger<OperationalAlertService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var snapshot = metrics.GetMonitoringSnapshot();
            var p95Threshold = configuration.GetValue("Operations:AlertP95Milliseconds", 2000);
            var errorThreshold = configuration.GetValue("Operations:AlertErrorRatePercent", 2d);

            if (snapshot.Requests >= 10 && (snapshot.P95Milliseconds > p95Threshold || snapshot.ErrorRatePercent > errorThreshold))
            {
                logger.LogWarning(
                    "Operational alert: requests={Requests}, p95Ms={P95Milliseconds}, errorRatePercent={ErrorRatePercent}",
                    snapshot.Requests, snapshot.P95Milliseconds, snapshot.ErrorRatePercent);
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
