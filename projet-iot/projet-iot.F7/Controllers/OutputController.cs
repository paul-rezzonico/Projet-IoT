using System;
using System.Threading.Tasks;
using Meadow.Peripherals.Leds;
using projet_iot.Core;

namespace projet_iot.F7;

internal class OutputController : IOutputController
{
    private IRgbLed Led { get; }

    public OutputController(IRgbLed led)
    {
        Led = led;
    }

    public Task SetState(bool state)
    {
        if (state)
        {
            Led.SetColor(RgbLedColors.Red);
        }
        else
        {
            Led.IsOn = false;
        }

        return Task.CompletedTask;
    }
}