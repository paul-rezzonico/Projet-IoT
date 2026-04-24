using System;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Meadow;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;

namespace projet_iot.Core;

public sealed class AzureMqttTelemetrySink : ITelemetrySink
{
    private readonly AzureMqttTelemetryOptions options;

    public AzureMqttTelemetrySink(AzureMqttTelemetryOptions options)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task PublishAsync(TelemetrySample sample, CancellationToken cancellationToken = default)
    {
        if (!options.Enabled)
        {
            return;
        }

        if (!options.HasCredentials())
        {
            Resolver.Log.Warn("Azure MQTT is enabled but required settings are missing. Skipping publish.");
            return;
        }

        var hostname = options.Hostname;
        var deviceId = options.DeviceId;
        var username = $"{hostname}/{deviceId}/?api-version=2021-04-12";
        var topic = $"devices/{deviceId}/messages/events/";

        var payload = BuildPayload(sample);
        using var client = new MqttFactory().CreateMqttClient();

        var clientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(hostname, 8883)
            .WithTlsOptions(new MqttClientTlsOptionsBuilder()
                .WithAllowUntrustedCertificates(options.AllowUntrustedCertificates)
                .Build())
            .WithCredentials(username, options.SasToken)
            .WithClientId(deviceId)
            .Build();

        try
        {
            var connectResult = await client.ConnectAsync(clientOptions, cancellationToken);
            if (connectResult.ResultCode != MqttClientConnectResultCode.Success)
            {
                Resolver.Log.Warn($"Azure MQTT connection failed: {connectResult.ResultCode}");
                return;
            }

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            var publishResult = await client.PublishAsync(message, cancellationToken);
            if (!publishResult.IsSuccess)
            {
                Resolver.Log.Warn($"Azure MQTT publish failed: {publishResult.ReasonCode}");
            }

            await client.DisconnectAsync();
        }
        catch (Exception ex)
        {
            Resolver.Log.Warn($"Azure MQTT publish error: {ex.Message}");
        }
    }

    private static string BuildPayload(TelemetrySample sample)
    {
        var temperature = sample.TemperatureC.ToString("0.##", CultureInfo.InvariantCulture);
        var threshold = sample.ThresholdC.ToString("0.##", CultureInfo.InvariantCulture);
        var pressure = sample.PressurePa?.ToString("0", CultureInfo.InvariantCulture) ?? "null";
        var net = sample.NetworkConnected ? "true" : "false";
        var below = sample.IsBelowThreshold.HasValue
            ? (sample.IsBelowThreshold.Value ? "true" : "false")
            : "null";

        var sb = new StringBuilder();
        sb.Append('{');
        sb.Append("\"tempC\":").Append(temperature).Append(',');
        sb.Append("\"thresholdC\":").Append(threshold).Append(',');
        sb.Append("\"pressurePa\":").Append(pressure).Append(',');
        sb.Append("\"net\":").Append(net).Append(',');
        sb.Append("\"reason\":\"").Append(Escape(sample.Reason)).Append("\",");
        sb.Append("\"below\":").Append(below).Append(',');
        sb.Append("\"ts\":\"").Append(sample.TimestampUtc.ToString("O", CultureInfo.InvariantCulture)).Append("\"");
        sb.Append('}');
        return sb.ToString();
    }

    private static string Escape(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
