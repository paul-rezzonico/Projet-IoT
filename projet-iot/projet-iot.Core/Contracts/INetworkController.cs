using System;
using System.Threading.Tasks;

namespace projet_iot.Core;

public interface INetworkController
{
    event EventHandler NetworkStatusChanged;

    Task Connect();
    bool IsConnected { get; }
}