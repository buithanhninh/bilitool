using System.Collections.Concurrent;
using System.Diagnostics;

namespace BiliTool.Vn.Web.Services.Operations;

public sealed class OperationalMetrics
{
    private readonly ConcurrentQueue<RequestSample> _samples = new();
    private long _totalRequests;
    private long _failedRequests;

    public void Record(string bucket, int statusCode, long elapsedMilliseconds)
    {
        Interlocked.Increment(ref _totalRequests);
        if (statusCode >= 500) Interlocked.Increment(ref _failedRequests);
        _samples.Enqueue(new RequestSample(DateTime.UtcNow, bucket, statusCode, elapsedMilliseconds));
        Trim(DateTime.UtcNow.AddMinutes(-15));
    }

    public object Snapshot()
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-15);
        Trim(cutoff);
        var recent = _samples.ToArray();
        var latencies = recent.Select(x => x.ElapsedMilliseconds).OrderBy(x => x).ToArray();
        return new
        {
            windowMinutes = 15,
            requests = recent.Length,
            serverErrors = recent.Count(x => x.StatusCode >= 500),
            errorRatePercent = recent.Length == 0 ? 0 : Math.Round(recent.Count(x => x.StatusCode >= 500) * 100d / recent.Length, 2),
            latencyMs = new { p50 = Percentile(latencies, .50), p95 = Percentile(latencies, .95), p99 = Percentile(latencies, .99) },
            routes = recent.GroupBy(x => x.Bucket).Select(x => new { route = x.Key, requests = x.Count(), serverErrors = x.Count(y => y.StatusCode >= 500) }),
            lifetime = new { requests = Interlocked.Read(ref _totalRequests), serverErrors = Interlocked.Read(ref _failedRequests) }
        };
    }

    public MonitoringSnapshot GetMonitoringSnapshot()
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-15);
        Trim(cutoff);
        var recent = _samples.ToArray();
        var latencies = recent.Select(x => x.ElapsedMilliseconds).OrderBy(x => x).ToArray();
        return new MonitoringSnapshot(
            recent.Length,
            recent.Length == 0 ? 0 : Math.Round(recent.Count(x => x.StatusCode >= 500) * 100d / recent.Length, 2),
            Percentile(latencies, .95));
    }

    private void Trim(DateTime cutoff)
    {
        while (_samples.TryPeek(out var sample) && sample.OccurredAt < cutoff) _samples.TryDequeue(out _);
    }

    private static long Percentile(long[] values, double percentile)
    {
        if (values.Length == 0) return 0;
        var index = Math.Clamp((int)Math.Ceiling(values.Length * percentile) - 1, 0, values.Length - 1);
        return values[index];
    }

    private sealed record RequestSample(DateTime OccurredAt, string Bucket, int StatusCode, long ElapsedMilliseconds);
}

public sealed record MonitoringSnapshot(int Requests, double ErrorRatePercent, long P95Milliseconds);
