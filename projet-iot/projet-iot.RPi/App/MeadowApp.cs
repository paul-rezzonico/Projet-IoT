using System;
using System.Threading.Tasks;
using Meadow;
using Meadow.Foundation.Displays;
using Meadow.Pinouts;
using projet_iot.Core;

namespace projet_iot.RPi;

internal class MeadowApp : App<RaspberryPi>
{
    private projet_iotHardware? hardware;
    private MainController? mainController;

    public bool SupportDisplay { get; set; } = false;

    public override Task Initialize()
    {
        hardware = new projet_iotHardware(Device, SupportDisplay);
        mainController = new MainController();
        return mainController.Initialize(hardware);
    }

    public override Task Run()
    {
        if (hardware is null || mainController is null)
        {
            throw new InvalidOperationException("Application is not initialized.");
        }

        if (hardware.Display is GtkDisplay gtk)
        {
            _ = mainController.Run();
            gtk.Run();
        }

        return mainController.Run();
    }
}
