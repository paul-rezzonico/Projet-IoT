using Meadow;
using Meadow.Devices;
using Meadow.Foundation.Leds;
using Meadow.Foundation.Sensors.Atmospheric;
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
    private readonly Bmp280 bmp280;

    public RotationType DisplayRotation => RotationType.Default;

    public ITemperatureSensor? TemperatureSensor => bmp280;
    public IBarometricPressureSensor? PressureSensor => bmp280;
    
    public ISamplingSensor<Temperature>? TemperatureSamplingSensor => bmp280;
    public ISamplingSensor<Pressure>? PressureSamplingSensor => bmp280;

    public IOutputController OutputController { get; }

    public IButton? RightButton => null;
    public IButton? LeftButton => null;

    public IPixelDisplay? Display => null;

    public INetworkController NetworkController { get; }

    public projet_iotF7FeatherHardware(F7FeatherBase device)
    {
        var i2cBus = device.CreateI2cBus();
        bmp280 = new Bmp280(i2cBus);

        OutputController = new OutputController(
            new RgbLed(
                device.Pins.OnboardLedRed.CreateDigitalOutputPort(),
                device.Pins.OnboardLedGreen.CreateDigitalOutputPort(),
                device.Pins.OnboardLedBlue.CreateDigitalOutputPort()
            )
        );

        NetworkController = new NetworkController(device);
    }
}