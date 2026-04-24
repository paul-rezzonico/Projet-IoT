using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Meadow;
using Meadow.Devices;
using Meadow.Logging;
using projet_iot.Core;

namespace projet_iot.F7;

public class MeadowProjectLabApp : App<F7CoreComputeV2>
{
    private MainController? mainController;

    public override Task Initialize()
    {
        var telemetryPublisher = ConfigureTelemetryPublisher();

        var hardware = new projet_iotProjectLabHardware(Device);
        mainController = new MainController(telemetryPublisher);
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

    private static ITelemetryPublisher ConfigureTelemetryPublisher()
    {
        var settings = TelemetryTargetSettings.Load();
        var sinks = new List<ITelemetrySink>();

        if (ReadBool("TELEMETRY_MEADOW_ENABLED", settings.MeadowEnabled ?? true))
        {
            var cloudLogger = new CloudLogger();
            Resolver.Log.AddProvider(cloudLogger);
            Resolver.Services.Add(cloudLogger);
            sinks.Add(new MeadowCloudTelemetrySink(cloudLogger));
        }

        var azureOptions = AzureMqttTelemetryOptions.FromEnvironment(settings);
        if (azureOptions.Enabled)
        {
            sinks.Add(new AzureMqttTelemetrySink(azureOptions));
        }

        return new MultiSinkTelemetryPublisher(sinks);
    }

    private static bool ReadBool(string variable, bool defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(variable);
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("on", StringComparison.OrdinalIgnoreCase);
    }
}
