using System.Threading;
using System.Threading.Tasks;

namespace projet_iot.Core;

public sealed class NoOpTelemetryPublisher : ITelemetryPublisher
{
    public static NoOpTelemetryPublisher Instance { get; } = new();

    private NoOpTelemetryPublisher()
    {
    }

    public Task PublishAsync(TelemetrySample sample, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
