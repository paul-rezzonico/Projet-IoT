using System;
using System.Threading.Tasks;
using Meadow;
using Meadow.Cloud;
using Meadow.Units;

namespace projet_iot.Core;

public class CloudController
{
    private readonly ITelemetryPublisher telemetryPublisher;

    public event EventHandler<Temperature.UnitType>? UnitsChangeRequested;
    public event EventHandler<Temperature>? ThresholdTemperatureChangeRequested;

    public CloudController(ICommandService? commandService, ITelemetryPublisher? telemetryPublisher = null)
    {
        this.telemetryPublisher = telemetryPublisher ?? NoOpTelemetryPublisher.Instance;

        // On Desktop, commandService is null — skip cloud command wiring entirely
        if (commandService is null)
        {
            Resolver.Log.Info("No command service available (Desktop mode), cloud commands disabled.");
            return;
        }

        commandService.Subscribe<ChangeDisplayUnitsCommand>(OnChangeDisplayUnitsCommandReceived);
        commandService.Subscribe<ChangeThresholdCommand>(OnChangeThresholdCommandReceived);
    }

    public Task PublishTelemetryAsync(
        Temperature temperature,
        double? pressurePa,
        Temperature thresholdTemperature,
        bool networkConnected,
        string reason)
    {
        var sample = TelemetrySample.CreateInterval(
            temperature.Celsius,
            pressurePa,
            thresholdTemperature.Celsius,
            networkConnected,
            reason);

        return telemetryPublisher.PublishAsync(sample);
    }

    public Task PublishThresholdEventAsync(
        Temperature temperature,
        Temperature thresholdTemperature,
        bool isBelowThreshold)
    {
        var sample = TelemetrySample.CreateThresholdCross(
            temperature.Celsius,
            thresholdTemperature.Celsius,
            isBelowThreshold);

        return telemetryPublisher.PublishAsync(sample);
    }

    private void OnChangeDisplayUnitsCommandReceived(ChangeDisplayUnitsCommand command)
    {
        Temperature.UnitType? requestedUnits = command.Units.ToUpper() switch
        {
            "CELSIUS" or "C"    => Temperature.UnitType.Celsius,
            "FAHRENHEIT" or "F" => Temperature.UnitType.Fahrenheit,
            "KELVIN" or "K"     => Temperature.UnitType.Kelvin,
            _                   => null
        };

        if (requestedUnits is null)
            Resolver.Log.Info($"Unknown unit requested: {command.Units}");
        else
            UnitsChangeRequested?.Invoke(this, requestedUnits.Value);
    }

    private void OnChangeThresholdCommandReceived(ChangeThresholdCommand command)
    {
        if (command.TempC is not null)
        {
            var threshold = command.TempC.Value.Celsius();
            Resolver.Log.Info($"Threshold change: {threshold.Celsius:N1}°C");
            ThresholdTemperatureChangeRequested?.Invoke(this, threshold);
        }
        else if (command.TempF is not null)
        {
            var threshold = command.TempF.Value.Fahrenheit();
            Resolver.Log.Info($"Threshold change: {threshold.Fahrenheit:N1}°F");
            ThresholdTemperatureChangeRequested?.Invoke(this, threshold);
        }
        else
        {
            Resolver.Log.Info("Threshold command received but no temperature value provided.");
        }
    }
}
