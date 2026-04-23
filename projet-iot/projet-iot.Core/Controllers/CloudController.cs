using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Meadow;
using Meadow.Cloud;
using Meadow.Logging;
using Meadow.Units;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;

namespace projet_iot.Core;

public class CloudController
{
    // Legacy Azure MQTT support is optional and disabled unless configured.
    private static readonly string Hostname = Environment.GetEnvironmentVariable("AZURE_IOT_HUB_HOSTNAME") ?? string.Empty;
    private static readonly string DeviceId = Environment.GetEnvironmentVariable("AZURE_IOT_DEVICE_ID") ?? string.Empty;
    private static readonly string SasToken = Environment.GetEnvironmentVariable("AZURE_IOT_SAS_TOKEN") ?? string.Empty;

    private const int TelemetryEventId = 1000;
    private const int ThresholdEventId = 1001;

    private readonly CloudLogger? cloudLogger;

    public event EventHandler<Temperature.UnitType>? UnitsChangeRequested;
    public event EventHandler<Temperature>? ThresholdTemperatureChangeRequested;

    public CloudController(ICommandService? commandService, CloudLogger? cloudLogger = null)
    {
        this.cloudLogger = cloudLogger;

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
        if (cloudLogger is null)
        {
            return Task.CompletedTask;
        }

        var measurements = new Dictionary<string, object>
        {
            { "tempC", Math.Round(temperature.Celsius, 2) },
            { "thresholdC", Math.Round(thresholdTemperature.Celsius, 2) },
            { "net", networkConnected },
            { "reason", reason }
        };

        if (pressurePa is { } pressure)
        {
            measurements["pressurePa"] = Math.Round(pressure, 0);
        }

        cloudLogger.LogEvent(TelemetryEventId, "env-reading", measurements);
        return Task.CompletedTask;
    }

    public Task PublishThresholdEventAsync(
        Temperature temperature,
        Temperature thresholdTemperature,
        bool isBelowThreshold)
    {
        if (cloudLogger is null)
        {
            return Task.CompletedTask;
        }

        var measurements = new Dictionary<string, object>
        {
            { "tempC", Math.Round(temperature.Celsius, 2) },
            { "thresholdC", Math.Round(thresholdTemperature.Celsius, 2) },
            { "below", isBelowThreshold }
        };

        cloudLogger.LogEvent(ThresholdEventId, "threshold-cross", measurements);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Sends temperature and pressure data to Azure IoT Hub via MQTT over TLS.
    /// </summary>
    public async Task<bool> SendDataToAzureAsync(double temperature, double pressure)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Hostname) ||
                string.IsNullOrWhiteSpace(DeviceId) ||
                string.IsNullOrWhiteSpace(SasToken))
            {
                Resolver.Log.Warn("Azure MQTT settings are not configured. Skipping Azure publish.");
                return false;
            }

            string clientId = $"{Hostname}/{DeviceId}/?api-version=2021-04-12";
            string username = $"{Hostname}/{DeviceId}/?api-version=2021-04-12";
            string topic = $"devices/{DeviceId}/messages/events/";

            var client = new MqttFactory().CreateMqttClient();

            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(Hostname, 8883)
                .WithTlsOptions(new MqttClientTlsOptionsBuilder()
                    .WithAllowUntrustedCertificates()
                    .Build())
                .WithCredentials(username, SasToken)
                .WithClientId(clientId)
                .Build();

            var connectResult = await client.ConnectAsync(options);

            if (connectResult.ResultCode != MqttClientConnectResultCode.Success)
            {
                Resolver.Log.Info($"Failed to connect to Azure IoT Hub: {connectResult.ResultCode}");
                return false;
            }

            Resolver.Log.Info("Successfully connected to Azure IoT Hub");

            string jsonPayload = $"{{\"temperature\":{temperature},\"pressure\":{pressure},\"timestamp\":\"{DateTime.UtcNow:O}\"}}";

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(jsonPayload)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            var publishResult = await client.PublishAsync(message);

            if (publishResult.IsSuccess)
                Resolver.Log.Info($"Message sent successfully: {jsonPayload}");
            else
                Resolver.Log.Info($"Failed to send message: {publishResult.ReasonCode}");

            await client.DisconnectAsync();

            return publishResult.IsSuccess;
        }
        catch (Exception ex)
        {
            Resolver.Log.Info($"Error sending data to Azure: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Sends fake/test data to Azure IoT Hub — useful for Desktop emulation.
    /// </summary>
    public Task<bool> SendFakeDataAsync()
    {
        return SendDataToAzureAsync(temperature: 25.0, pressure: 101325.0);
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
