using System.Diagnostics;

namespace BiliTool.Vn.Application.Services;

public interface IHisIntegrationMetrics
{
    void Increment(string eventName);
}

public static class HisIntegrationDiagnostics
{
    public const string ActivitySourceName = "BiliTool.Vn.HisIntegration";
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName, "1.0.0");
}
