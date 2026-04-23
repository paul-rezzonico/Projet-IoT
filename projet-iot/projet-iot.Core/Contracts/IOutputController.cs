using System;
using System.Threading.Tasks;

namespace projet_iot.Core;

public interface IOutputController
{
    Task SetState(bool state);
}