using System;
using System.Threading.Tasks;
using Meadow;
using Meadow.Devices;
using Meadow.Foundation.Leds;
using Meadow.Foundation.Sensors;
using Meadow.Peripherals.Displays;
using Meadow.Peripherals.Sensors;
using Meadow.Peripherals.Sensors.Atmospheric;
using Meadow.Peripherals.Sensors.Buttons;
using Meadow.Units;
using projet_iot.Core;
using projet_iot.Core.Contracts;

namespace projet_iot.F7;

internal class projet_iotProjectLabHardware : Iprojet_iotHardware
{
    private readonly IProjectLabHardware projLab;

    public RotationType DisplayRotation => RotationType._270Degrees;
    public IOutputController OutputController { get; }
    public IButton? LeftButton => projLab.LeftButton;
    public IButton? RightButton => projLab.RightButton;
    public ITemperatureSensor? TemperatureSensor => projLab.TemperatureSensor;
    public IBarometricPressureSensor? PressureSensor => projLab.BarometricPressureSensor;
    public ISamplingSensor<Temperature>? TemperatureSamplingSensor => TemperatureSensor as ISamplingSensor<Temperature>;
    public ISamplingSensor<Pressure>? PressureSamplingSensor => PressureSensor as ISamplingSensor<Pressure>;
    public IPixelDisplay? Display => projLab.Display;
    public INetworkController NetworkController { get; }

    public projet_iotProjectLabHardware(F7CoreComputeV2 device)
    {
        projLab = ProjectLab.Create();

        OutputController = new OutputController(projLab.RgbLed
            ?? throw new InvalidOperationException("Project Lab RGB LED is not available."));
        NetworkController = new NetworkController(device);
    }
}
