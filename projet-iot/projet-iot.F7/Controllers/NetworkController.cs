using System;
using System.Threading.Tasks;
using Meadow.Devices;
using Meadow.Hardware;
using projet_iot.Core;

namespace projet_iot.F7;

internal class NetworkController : INetworkController
{
    private const string WIFI_NAME = "[SOME_NAME]";
    private const string WIFI_PASSWORD = "[SOME_SECRET]";

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
        // Handle logic when disconnected.
    }

    private void OnNetworkConnected(INetworkAdapter sender, NetworkConnectionEventArgs args)
    {
        // Handle logic when connected.
    }

    public bool IsConnected
    {
        get => wifi.IsConnected;
    }

    public async Task Connect()
    {
        await wifi.Connect(WIFI_NAME, WIFI_PASSWORD, TimeSpan.FromSeconds(45));
    }
}
