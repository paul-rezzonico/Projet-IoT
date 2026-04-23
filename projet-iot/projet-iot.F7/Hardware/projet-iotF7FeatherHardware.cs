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

internal class projet_iotF7FeatherHardware : Iprojet_iotHardware
{
    private readonly ITemperatureSensor temperatureSensor;
    private readonly IBarometricPressureSensor pressureSensor;

    public RotationType DisplayRotation => RotationType.Default;
    public ITemperatureSensor? TemperatureSensor => temperatureSensor;
    public IBarometricPressureSensor? PressureSensor => pressureSensor;
    public ISamplingSensor<Temperature>? TemperatureSamplingSensor => temperatureSensor as ISamplingSensor<Temperature>;
    public ISamplingSensor<Pressure>? PressureSamplingSensor => pressureSensor as ISamplingSensor<Pressure>;
    public IOutputController OutputController { get; }
    public IButton? RightButton => null;
    public IButton? LeftButton => null;
    public IPixelDisplay? Display => null;
    public INetworkController NetworkController { get; }

    public projet_iotF7FeatherHardware(F7FeatherBase device)
    {
        temperatureSensor = new SimulatedTemperatureSensor(22.Celsius(), 20.Celsius(), 24.Celsius());
        pressureSensor = new SimulatedBarometricPressureSensor();

        OutputController = new OutputController(
            new RgbLed(
                device.Pins.OnboardLedRed.CreateDigitalOutputPort(),
                device.Pins.OnboardLedGreen.CreateDigitalOutputPort(),
                device.Pins.OnboardLedBlue.CreateDigitalOutputPort()));

        NetworkController = new NetworkController(device);
    }
}
