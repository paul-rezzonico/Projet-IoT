using System;
using System.Threading.Tasks;
using projet_iot.Core.Contracts;

namespace projet_iot.Core;

public class InputController
{
    public event EventHandler? UnitDownRequested;
    public event EventHandler? UnitUpRequested;

    public InputController(Iprojet_iotHardware platform)
    {
        if (platform.LeftButton is { } ub)
        {
            ub.PressStarted += (s, e) => UnitDownRequested?.Invoke(this, EventArgs.Empty);
        }

        if (platform.RightButton is { } db)
        {
            db.PressStarted += (s, e) => UnitDownRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}