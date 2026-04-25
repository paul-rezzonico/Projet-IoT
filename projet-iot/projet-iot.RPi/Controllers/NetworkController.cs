using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using projet_iot.Core;

namespace projet_iot.RPi;

internal class NetworkController : INetworkController
{
    private static readonly string[] EmptyDnsServers = Array.Empty<string>();

    public event EventHandler? NetworkStatusChanged;

    public bool IsConnected { get; private set; }

    public string IpAddress { get; private set; } = "0.0.0.0";

    public string HostName { get; private set; } = Environment.MachineName;

    public string MacAddress { get; private set; } = "00:00:00:00:00:00";

    public string Gateway { get; private set; } = "0.0.0.0";

    public string SubnetMask { get; private set; } = "0.0.0.0";

    public string[] DnsServers { get; private set; } = EmptyDnsServers;

    public Task Connect()
    {
        var wasConnected = IsConnected;

        RefreshNetworkInfo();

        if (IsConnected != wasConnected)
        {
            NetworkStatusChanged?.Invoke(this, EventArgs.Empty);
        }

        return Task.CompletedTask;
    }

    private void RefreshNetworkInfo()
    {
        HostName = SafeHostName();

        var activeInterface = FindActiveInterface();
        if (activeInterface is null)
        {
            IsConnected = false;
            IpAddress = "0.0.0.0";
            MacAddress = "00:00:00:00:00:00";
            Gateway = "0.0.0.0";
            SubnetMask = "0.0.0.0";
            DnsServers = EmptyDnsServers;
            return;
        }

        var properties = activeInterface.GetIPProperties();

        IsConnected = true;
        IpAddress = FirstIPv4Address(properties) ?? "0.0.0.0";
        MacAddress = FormatMacAddress(activeInterface.GetPhysicalAddress());
        Gateway = properties.GatewayAddresses
            .Select(x => x.Address)
            .FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork)?
            .ToString() ?? "0.0.0.0";
        SubnetMask = FirstIPv4Mask(properties) ?? "0.0.0.0";
        DnsServers = properties.DnsAddresses
            .Where(x => x.AddressFamily == AddressFamily.InterNetwork)
            .Select(x => x.ToString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();

        if (DnsServers.Length == 0)
        {
            DnsServers = EmptyDnsServers;
        }
    }

    private static string SafeHostName()
    {
        try
        {
            return Dns.GetHostName();
        }
        catch
        {
            return Environment.MachineName;
        }
    }

    private static NetworkInterface? FindActiveInterface()
    {
        return NetworkInterface.GetAllNetworkInterfaces()
            .Where(x => x.OperationalStatus == OperationalStatus.Up)
            .Where(x => x.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .Where(x => x.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
            .FirstOrDefault(x => HasIPv4Address(x.GetIPProperties()));
    }

    private static bool HasIPv4Address(IPInterfaceProperties properties)
    {
        return properties.UnicastAddresses.Any(x => x.Address.AddressFamily == AddressFamily.InterNetwork);
    }

    private static string? FirstIPv4Address(IPInterfaceProperties properties)
    {
        return properties.UnicastAddresses
            .FirstOrDefault(x => x.Address.AddressFamily == AddressFamily.InterNetwork)?
            .Address
            .ToString();
    }

    private static string? FirstIPv4Mask(IPInterfaceProperties properties)
    {
        var mask = properties.UnicastAddresses
            .FirstOrDefault(x => x.Address.AddressFamily == AddressFamily.InterNetwork)?
            .IPv4Mask;

        return mask?.ToString();
    }

    private static string FormatMacAddress(PhysicalAddress? address)
    {
        if (address is null)
        {
            return "00:00:00:00:00:00";
        }

        var bytes = address.GetAddressBytes();
        if (bytes.Length == 0)
        {
            return "00:00:00:00:00:00";
        }

        return string.Join(":", bytes.Select(x => x.ToString("X2")));
    }
}
