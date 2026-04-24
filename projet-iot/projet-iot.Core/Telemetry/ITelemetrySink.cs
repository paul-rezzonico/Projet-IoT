using System.Threading;
using System.Threading.Tasks;

namespace projet_iot.Core;

public interface ITelemetrySink
{
    Task PublishAsync(TelemetrySample sample, CancellationToken cancellationToken = default);
}
