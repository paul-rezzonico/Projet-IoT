using System.Threading;
using System.Threading.Tasks;

namespace projet_iot.Core;

public interface ITelemetryPublisher
{
    Task PublishAsync(TelemetrySample sample, CancellationToken cancellationToken = default);
}
