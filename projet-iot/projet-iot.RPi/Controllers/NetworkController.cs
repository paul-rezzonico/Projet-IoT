using System;
using System.Threading.Tasks;
using projet_iot.Core;

namespace projet_iot.RPi;

internal class NetworkController : INetworkController
{
    public event EventHandler? NetworkStatusChanged;

    public bool IsConnected { get; private set; }

    public string IpAddress => IsConnected ? "127.0.0.1" : "0.0.0.0";

    public Task Connect()
    {
        IsConnected = true;
        NetworkStatusChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }
}
