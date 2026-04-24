using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Meadow.Logging;

namespace projet_iot.Core;

public sealed class MeadowCloudTelemetrySink : ITelemetrySink
{
    private const int TelemetryEventId = 1000;
    private const int ThresholdEventId = 1001;
    private readonly CloudLogger cloudLogger;

    public MeadowCloudTelemetrySink(CloudLogger cloudLogger)
    {
        this.cloudLogger = cloudLogger ?? throw new ArgumentNullException(nameof(cloudLogger));
    }

    public Task PublishAsync(TelemetrySample sample, CancellationToken cancellationToken = default)
    {
        if (sample.IsThresholdEvent)
        {
            var thresholdMeasurements = new Dictionary<string, object>
            {
                { "tempC", Math.Round(sample.TemperatureC, 2) },
                { "thresholdC", Math.Round(sample.ThresholdC, 2) },
                { "below", sample.IsBelowThreshold ?? false }
            };

            cloudLogger.LogEvent(ThresholdEventId, "threshold-cross", thresholdMeasurements);
            return Task.CompletedTask;
        }

        var measurements = new Dictionary<string, object>
        {
            { "tempC", Math.Round(sample.TemperatureC, 2) },
            { "thresholdC", Math.Round(sample.ThresholdC, 2) },
            { "net", sample.NetworkConnected },
            { "reason", sample.Reason }
        };

        if (sample.PressurePa is { } pressure)
        {
            measurements["pressurePa"] = Math.Round(pressure, 0);
        }

        cloudLogger.LogEvent(TelemetryEventId, "env-reading", measurements);
        return Task.CompletedTask;
    }
}
