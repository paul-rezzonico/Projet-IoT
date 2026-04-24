using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Meadow;
using Meadow.Foundation.Displays;
using Meadow.Logging;
using projet_iot.Core;

namespace projet_iot.DT;

internal class MeadowApp : App<Desktop>
{
    private MainController? mainController;

    public override Task Initialize()
    {
        // output log messages to the VS debug window
        Resolver.Log.AddProvider(new DebugLogProvider());

        var telemetryPublisher = ConfigureTelemetryPublisher();

        var hardware = new projet_iotHardware(Device);
        mainController = new MainController(telemetryPublisher);
        return mainController.Initialize(hardware);
    }

    public override Task Run()
    {
        if (mainController is null)
        {
            throw new InvalidOperationException("Application is not initialized.");
        }

        // this must be spawned in a worker because the UI needs the main thread
        _ = mainController.Run();

        ExecutePlatformDisplayRunner();

        return base.Run();
    }

    private void ExecutePlatformDisplayRunner()
    {
        if (Device.Display is SilkDisplay silkDisplay)
        {
            silkDisplay.Run();
        }
    }

    private static ITelemetryPublisher ConfigureTelemetryPublisher()
    {
        var settings = TelemetryTargetSettings.Load();
        var sinks = new List<ITelemetrySink>();
        var azureOptions = AzureMqttTelemetryOptions.FromEnvironment(settings);
        if (azureOptions.Enabled)
        {
            sinks.Add(new AzureMqttTelemetrySink(azureOptions));
        }

        return new MultiSinkTelemetryPublisher(sinks);
    }
}
