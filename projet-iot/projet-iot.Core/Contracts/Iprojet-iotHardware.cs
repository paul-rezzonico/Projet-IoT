using System;
using System.Threading.Tasks;
using Meadow.Foundation.Graphics;
using Meadow.Peripherals.Displays;
using Meadow.Peripherals.Sensors;
using Meadow.Peripherals.Sensors.Atmospheric;
using Meadow.Peripherals.Sensors.Buttons;
using Meadow.Units;

namespace projet_iot.Core.Contracts;

public interface Iprojet_iotHardware
{
    // basic hardware
    IButton? LeftButton { get; }
    IButton? RightButton { get; }

    // complex hardware
    ITemperatureSensor? TemperatureSensor { get; }
    IBarometricPressureSensor? PressureSensor { get; }
    ISamplingSensor<Temperature>? TemperatureSamplingSensor { get; }
    ISamplingSensor<Pressure>? PressureSamplingSensor { get; }
    IPixelDisplay? Display { get; }
    RotationType DisplayRotation { get; }

    // platform-dependent services
    IOutputController OutputController { get; }
    INetworkController NetworkController { get; }
}
