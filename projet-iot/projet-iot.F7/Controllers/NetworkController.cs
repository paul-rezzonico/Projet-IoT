using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Meadow;
using Meadow.Devices;
using Meadow.Hardware;
using projet_iot.Core;

namespace projet_iot.F7;

internal class NetworkController : INetworkController
{
    private const string UnknownValue = "unknown";
    private static readonly string[] EmptyDnsServers = Array.Empty<string>();

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

    public string HostName => Environment.MachineName;

    public string MacAddress => ReadAdapterString("MacAddress");

    public string Gateway => ReadAdapterString("Gateway");

    public string SubnetMask => ReadAdapterString("SubnetMask");

    public string[] DnsServers => ReadAdapterStringArray("DnsAddresses", "DnsServers");

    public async Task Connect()
    {
        // Connect using meadow default connection system
        // try to trigger the connection process by accessing the network adapter
        var _ = wifi.IpAddress;
    }
    
    public void ShowNetworkInfo()
    {
        Console.WriteLine($"Connected: {IsConnected}");
        Console.WriteLine($"Host Name: {HostName}");
        Console.WriteLine($"IP Address: {IpAddress}");
        Console.WriteLine($"MAC Address: {MacAddress}");
        Console.WriteLine($"Gateway: {Gateway}");
        Console.WriteLine($"Subnet Mask: {SubnetMask}");
        Console.WriteLine("DNS Servers:");
        foreach (var dns in DnsServers)
        {
            Console.WriteLine($"  - {dns}");
        }
    }

    private string ReadAdapterString(params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            var property = wifi.GetType().GetProperty(propertyName);
            if (property is null)
            {
                continue;
            }

            var rawValue = property.GetValue(wifi);
            if (rawValue is null)
            {
                continue;
            }

            if (rawValue is string stringValue)
            {
                return string.IsNullOrWhiteSpace(stringValue) ? UnknownValue : stringValue;
            }

            var value = rawValue.ToString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return UnknownValue;
    }

    private string[] ReadAdapterStringArray(params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            var property = wifi.GetType().GetProperty(propertyName);
            if (property is null)
            {
                continue;
            }

            if (property.GetValue(wifi) is not IEnumerable rawCollection)
            {
                continue;
            }

            var values = new List<string>();
            foreach (var item in rawCollection)
            {
                var value = item?.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    values.Add(value);
                }
            }

            if (values.Count > 0)
            {
                return values.ToArray();
            }
        }

        return EmptyDnsServers;
    }
}
