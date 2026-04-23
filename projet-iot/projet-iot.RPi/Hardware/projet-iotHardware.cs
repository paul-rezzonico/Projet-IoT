using System;
using System.Threading.Tasks;
using Meadow;
using Meadow.Foundation.Displays;
using Meadow.Foundation.Sensors;
using Meadow.Peripherals.Displays;
using Meadow.Peripherals.Sensors;
using Meadow.Peripherals.Sensors.Atmospheric;
using Meadow.Peripherals.Sensors.Buttons;
using Meadow.Units;
using projet_iot.Core;
using projet_iot.Core.Contracts;

namespace projet_iot.RPi;

internal class projet_iotHardware : Iprojet_iotHardware
{
    private readonly RaspberryPi device;
    private readonly IPixelDisplay? display = null;
    private readonly ITemperatureSensor temperatureSensor;
    private readonly IOutputController outputService;

    public RotationType DisplayRotation => RotationType.Default;
    public IPixelDisplay? Display => display;
    public IOutputController OutputController => outputService;
    public ITemperatureSensor? TemperatureSensor => temperatureSensor;
    public IBarometricPressureSensor? PressureSensor => null;
    public ISamplingSensor<Temperature>? TemperatureSamplingSensor => TemperatureSensor as ISamplingSensor<Temperature>;
    public ISamplingSensor<Pressure>? PressureSamplingSensor => null;
    public IButton? RightButton => null;
    public IButton? LeftButton => null;
    public INetworkController NetworkController { get; }


    public projet_iotHardware(RaspberryPi device, bool supportDisplay)
    {
        this.device = device;
        temperatureSensor = new SimulatedTemperatureSensor(22.Celsius(), 20.Celsius(), 24.Celsius());
        outputService = new OutputController();
        NetworkController = new NetworkController();

        if (supportDisplay)
        {
            // only if we have a display attached
            display = new GtkDisplay(ColorMode.Format16bppRgb565);
        }
    }
}
