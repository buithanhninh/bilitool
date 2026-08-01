using Microsoft.Extensions.Configuration;

namespace BiliTool.Vn.Infrastructure.Services;

public sealed class HisWebhookResilienceGate : IDisposable
{
    private readonly SemaphoreSlim _bulkhead;
    private readonly TimeSpan _queueTimeout;
    private readonly TimeSpan _breakDuration;
    private readonly int _failureThreshold;
    private readonly object _stateLock = new();
    private int _consecutiveFailures;
    private DateTimeOffset? _openUntil;
    private bool _halfOpenProbeActive;

    public HisWebhookResilienceGate(IConfiguration configuration)
    {
        var concurrency = Math.Clamp(configuration.GetValue("Webhooks:Resilience:MaxConcurrency", 8), 1, 128);
        _bulkhead = new SemaphoreSlim(concurrency, concurrency);
        _queueTimeout = TimeSpan.FromMilliseconds(Math.Clamp(
            configuration.GetValue("Webhooks:Resilience:BulkheadQueueTimeoutMs", 100), 0, 5000));
        _failureThreshold = Math.Clamp(configuration.GetValue("Webhooks:Resilience:CircuitFailureThreshold", 5), 2, 100);
        _breakDuration = TimeSpan.FromSeconds(Math.Clamp(
            configuration.GetValue("Webhooks:Resilience:CircuitBreakSeconds", 30), 1, 300));
    }

    public async Task<HisWebhookDeliveryResult> ExecuteAsync(
        Func<Task<HisWebhookDeliveryResult>> action,
        CancellationToken cancellationToken)
    {
        if (!TryEnterCircuit(out var halfOpenProbe))
            return new HisWebhookDeliveryResult(false, null, "Webhook circuit breaker đang mở.");

        if (!await _bulkhead.WaitAsync(_queueTimeout, cancellationToken))
        {
            ReleaseHalfOpenProbe(halfOpenProbe);
            return new HisWebhookDeliveryResult(false, null, "Webhook bulkhead đã đầy.");
        }

        try
        {
            var result = await action();
            RecordResult(result, halfOpenProbe);
            return result;
        }
        catch
        {
            RecordFailure(halfOpenProbe);
            throw;
        }
        finally
        {
            _bulkhead.Release();
        }
    }

    private bool TryEnterCircuit(out bool halfOpenProbe)
    {
        lock (_stateLock)
        {
            halfOpenProbe = false;
            if (!_openUntil.HasValue) return true;
            if (_openUntil > DateTimeOffset.UtcNow) return false;
            if (_halfOpenProbeActive) return false;
            _halfOpenProbeActive = true;
            halfOpenProbe = true;
            return true;
        }
    }

    private void RecordResult(HisWebhookDeliveryResult result, bool halfOpenProbe)
    {
        var dependencyFailure = !result.Succeeded && (!result.StatusCode.HasValue || result.StatusCode >= 500);
        if (dependencyFailure)
        {
            RecordFailure(halfOpenProbe);
            return;
        }

        lock (_stateLock)
        {
            _consecutiveFailures = 0;
            _openUntil = null;
            if (halfOpenProbe) _halfOpenProbeActive = false;
        }
    }

    private void RecordFailure(bool halfOpenProbe)
    {
        lock (_stateLock)
        {
            _consecutiveFailures++;
            if (halfOpenProbe || _consecutiveFailures >= _failureThreshold)
                _openUntil = DateTimeOffset.UtcNow.Add(_breakDuration);
            if (halfOpenProbe) _halfOpenProbeActive = false;
        }
    }

    private void ReleaseHalfOpenProbe(bool halfOpenProbe)
    {
        if (!halfOpenProbe) return;
        lock (_stateLock) _halfOpenProbeActive = false;
    }

    public void Dispose() => _bulkhead.Dispose();
}
