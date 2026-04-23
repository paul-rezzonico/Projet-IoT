using System;
using System.Threading.Tasks;
using Meadow;
using Meadow.Devices;
using Meadow.Logging;
using projet_iot.Core;

namespace projet_iot.F7;

public class MeadowF7FeatherApp : App<F7FeatherV2>
{
    private MainController? mainController;

    public override Task Initialize()
    {
        ConfigureCloudLogging();

        var hardware = new projet_iotF7FeatherHardware(Device);
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

    private static void ConfigureCloudLogging()
    {
        var cloudLogger = new CloudLogger();
        Resolver.Log.AddProvider(cloudLogger);
        Resolver.Services.Add(cloudLogger);
    }
}
