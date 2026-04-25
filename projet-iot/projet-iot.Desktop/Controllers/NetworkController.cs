using System;
using System.Threading.Tasks;
using Meadow;
using Meadow.Foundation.Sensors.Hid;
using Meadow.Hardware;
using projet_iot.Core;

namespace projet_iot.DT;

internal class NetworkController : INetworkController
{
    private bool isConnected;

    public event EventHandler? NetworkStatusChanged;

    public NetworkController(Keyboard? keyboard)
    {
        if (keyboard != null)
        {
            // allow the app to simulate network up/down with the keyboard
            keyboard.Pins.Plus.CreateDigitalInterruptPort(InterruptMode.EdgeRising).Changed +=
                (s, e) => { _ = Connect(); };
            keyboard.Pins.Minus.CreateDigitalInterruptPort(InterruptMode.EdgeRising).Changed +=
                (s, e) => { IsConnected = false; };
        }
    }

    public bool IsConnected
    {
        get => isConnected;
        private set
        {
            if (value == IsConnected)
            {
                return;
            }

            isConnected = value;
            NetworkStatusChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public string IpAddress => IsConnected ? "127.0.0.1" : "0.0.0.0";

    public string HostName => Environment.MachineName;

    public string MacAddress => IsConnected ? "02:00:00:00:00:01" : "00:00:00:00:00:00";

    public string Gateway => IsConnected ? "127.0.0.1" : "0.0.0.0";

    public string SubnetMask => IsConnected ? "255.0.0.0" : "0.0.0.0";

    public string[] DnsServers => IsConnected ? new[] { "127.0.0.1" } : Array.Empty<string>();

    public async Task Connect()
    {
        // simulate connection delay
        await Task.Delay(1000);

        IsConnected = true;
    }
    public void ShowNetworkInfo()
    {
        Console.WriteLine($"Connected: {IsConnected}");
        Console.WriteLine($"Host Name: {HostName}");
        Console.WriteLine($"IP Address: {IpAddress}");
        Console.WriteLine($"MAC Address: {MacAddress}");
        Console.WriteLine($"Gateway: {Gateway}");
        Console.WriteLine($"Subnet Mask: {SubnetMask}");
    }

    public async Task<bool> Ping(string host)
    {
        try
        {
            var ping = new System.Net.NetworkInformation.Ping();
            var reply = await ping.SendPingAsync(host);
            return reply.Status == System.Net.NetworkInformation.IPStatus.Success;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ping failed: {ex.Message}");
            return false;
        }
    }
}
