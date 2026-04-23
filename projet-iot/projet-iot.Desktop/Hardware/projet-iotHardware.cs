using System;
using System.Threading.Tasks;
using Meadow;
using Meadow.Foundation.Sensors;
using Meadow.Foundation.Sensors.Buttons;
using Meadow.Foundation.Sensors.Hid;
using Meadow.Hardware;
using Meadow.Peripherals.Displays;
using Meadow.Peripherals.Sensors;
using Meadow.Peripherals.Sensors.Atmospheric;
using Meadow.Peripherals.Sensors.Buttons;
using Meadow.Units;
using projet_iot.Core;
using projet_iot.Core.Contracts;

namespace projet_iot.DT;

internal class projet_iotHardware : Iprojet_iotHardware
{
    private readonly Desktop device;
    private readonly Keyboard keyboard;

    public RotationType DisplayRotation => RotationType.Default;
    public IOutputController OutputController { get; }
    public INetworkController NetworkController { get; }
    public IPixelDisplay? Display => device.Display;
    public ITemperatureSensor? TemperatureSensor { get; }
    public IBarometricPressureSensor? PressureSensor { get; }
    public ISamplingSensor<Temperature>? TemperatureSamplingSensor => TemperatureSensor as ISamplingSensor<Temperature>;
    public ISamplingSensor<Pressure>? PressureSamplingSensor => PressureSensor as ISamplingSensor<Pressure>;
    public IButton? RightButton { get; }
    public IButton? LeftButton { get; }

    public projet_iotHardware(Desktop device)
    {
        this.device = device;

        keyboard = new Keyboard();
        NetworkController = new NetworkController(keyboard);

        TemperatureSensor = new SimulatedTemperatureSensor(
            new Temperature(70, Temperature.UnitType.Fahrenheit),
            keyboard.Pins.Up.CreateDigitalInterruptPort(InterruptMode.EdgeRising),
            keyboard.Pins.Down.CreateDigitalInterruptPort(InterruptMode.EdgeRising));

        PressureSensor = new SimulatedBarometricPressureSensor();

        LeftButton = new PushButton(keyboard.Pins.Left.CreateDigitalInterruptPort(InterruptMode.EdgeFalling));
        RightButton = new PushButton(keyboard.Pins.Right.CreateDigitalInterruptPort(InterruptMode.EdgeFalling));

        OutputController = new OutputController();
    }
}
