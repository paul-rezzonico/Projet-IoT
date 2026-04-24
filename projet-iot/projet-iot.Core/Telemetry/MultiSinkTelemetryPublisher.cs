using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Meadow;

namespace projet_iot.Core;

public sealed class MultiSinkTelemetryPublisher : ITelemetryPublisher
{
    private readonly IReadOnlyList<ITelemetrySink> sinks;

    public MultiSinkTelemetryPublisher(IEnumerable<ITelemetrySink>? sinks)
    {
        this.sinks = sinks is null ? Array.Empty<ITelemetrySink>() : new List<ITelemetrySink>(sinks);
    }

    public async Task PublishAsync(TelemetrySample sample, CancellationToken cancellationToken = default)
    {
        if (sinks.Count == 0)
        {
            return;
        }

        foreach (var sink in sinks)
        {
            try
            {
                await sink.PublishAsync(sample, cancellationToken);
            }
            catch (Exception ex)
            {
                Resolver.Log.Warn($"Telemetry sink '{sink.GetType().Name}' failed: {ex.Message}");
            }
        }
    }
}
