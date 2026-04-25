using System;
using System.Threading.Tasks;
using Meadow;
using Meadow.Devices;
using Meadow.Hardware;
using projet_iot.Core;

namespace projet_iot.F7;

internal class NetworkController : INetworkController
{
    private const string WIFI_NAME = "tà dương";
    private const string WIFI_PASSWORD = "H5ebUd3GC7U9j5HAFQ";

    public event EventHandler? NetworkStatusChanged;

    private readonly IWiFiNetworkAdapter wifi;

    public NetworkController(F7MicroBase device)
    {
        if (device is null)
        {
            throw new ArgumentNullException(nameof(device));
        }

        wifi = device.NetworkAdapters.Primary<IWiFiNetworkAdapter>()
            ?? throw new InvalidOperationException("No WiFi network adapter is available.");

        wifi.NetworkConnected += OnNetworkConnected;
        wifi.NetworkDisconnected += OnNetworkDisconnected;
    }

    private void OnNetworkDisconnected(INetworkAdapter sender, NetworkDisconnectionEventArgs args)
    {
        NetworkStatusChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnNetworkConnected(INetworkAdapter sender, NetworkConnectionEventArgs args)
    {
        NetworkStatusChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool IsConnected
    {
        get => wifi.IsConnected;
    }

    public string IpAddress => wifi.IpAddress?.ToString() ?? "0.0.0.0";

    public async Task Connect()
    {
        await wifi.Connect(WIFI_NAME, WIFI_PASSWORD, TimeSpan.FromSeconds(45));
    }
}
