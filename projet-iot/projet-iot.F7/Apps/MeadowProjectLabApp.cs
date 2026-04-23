using System;
using System.Threading.Tasks;
using Meadow;
using Meadow.Devices;
using projet_iot.Core;

namespace projet_iot.F7;

public class MeadowProjectLabApp : App<F7CoreComputeV2>
{
    private MainController? mainController;

    public override Task Initialize()
    {
        var hardware = new projet_iotProjectLabHardware(Device);
        mainController = new MainController();
        return mainController.Initialize(hardware);
    }

    public override Task Run()
    {
        if (mainController is null)
        {
            throw new InvalidOperationException("Application is not initialized.");
        }

        return mainController.Run();
    }
}
