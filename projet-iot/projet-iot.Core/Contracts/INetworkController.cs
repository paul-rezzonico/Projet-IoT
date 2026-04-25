using System;
using System.Threading.Tasks;

namespace projet_iot.Core;

public interface INetworkController
{
    event EventHandler NetworkStatusChanged;

    Task Connect();
    bool IsConnected { get; }
    string IpAddress { get; }
    string HostName { get; }
    string MacAddress { get; }
    string Gateway { get; }
    string SubnetMask { get; }
    string[] DnsServers { get; }
}