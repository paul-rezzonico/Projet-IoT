using System;
using System.Collections.Generic;
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
    private readonly SemaphoreSlim semaphore = new(1, 1);

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

        // If the year is 2021 or earlier, the clock is likely not synced.
        // TLS handshakes (required for Azure) will fail if the device time is incorrect.
        if (DateTime.UtcNow.Year <= 2021)
        {
            Resolver.Log.Warn("Azure MQTT: Time not synced yet. SSL handshake will likely fail. Skipping publish.");
            return;
        }

        // Prevent concurrent connection attempts on resource-constrained hardware
        await semaphore.WaitAsync(cancellationToken);

        try
        {
            var hostname = options.Hostname;
            var deviceId = options.DeviceId;
            var username = $"{hostname}/{deviceId}/?api-version=2021-04-12";
            var topic = $"devices/{deviceId}/messages/events/";

            var payload = BuildPayload(sample);
            using var client = new MqttFactory().CreateMqttClient();

            var clientOptions = new MqttClientOptionsBuilder()
                .WithClientId(deviceId)
                .WithWebSocketServer(o =>
                {
                    o.WithUri($"wss://{hostname}:443/$iothub/websocket");
                })
                .WithCredentials(username, options.SasToken)
                .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V311)
                .WithTlsOptions(new MqttClientTlsOptions
                {
                    UseTls = true,
                    CertificateValidationHandler = _ => options.AllowUntrustedCertificates
                })
                .WithTimeout(TimeSpan.FromSeconds(45))
                .Build();

            Resolver.Log.Info($"Azure MQTT: Attempting WebSocket connection to {hostname}:443 (Timeout: 45s, Protocol: 3.1.1)...");
            
            if (options.SasToken.Length > 10)
            {
                Resolver.Log.Info($"Azure MQTT: SAS Token starts with: {options.SasToken[..10]}...");
            }
            
            try
            {
                var addresses = await System.Net.Dns.GetHostAddressesAsync(hostname);
                Resolver.Log.Info($"Azure MQTT: DNS resolved {hostname} to {string.Join(", ", (IEnumerable<System.Net.IPAddress>)addresses)}");
            }
            catch (Exception dnsEx)
            {
                Resolver.Log.Warn($"Azure MQTT: DNS resolution failed for {hostname}: {dnsEx.Message}");
            }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(50)); // Safety margin above MQTT internal timeout

            client.ConnectedAsync += e =>
            {
                Resolver.Log.Info("Azure MQTT: Client connected to broker.");
                return Task.CompletedTask;
            };

            client.DisconnectedAsync += e =>
            {
                Resolver.Log.Info($"Azure MQTT: Client disconnected. Reason: {e.Reason}. Exception: {e.Exception?.Message ?? "None"}");
                return Task.CompletedTask;
            };

            var connectResult = await client.ConnectAsync(clientOptions, timeoutCts.Token);
            Resolver.Log.Info($"Azure MQTT: Connection result: {connectResult.ResultCode}");

            if (connectResult.ResultCode != MqttClientConnectResultCode.Success)
            {
                Resolver.Log.Warn($"Azure MQTT connection failed: {connectResult.ResultCode}. Reason: {connectResult.ReasonString}");
                return;
            }

            Resolver.Log.Info($"Azure MQTT: Publishing to topic {topic}...");
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            var publishResult = await client.PublishAsync(message, timeoutCts.Token);
            if (publishResult.IsSuccess)
            {
                Resolver.Log.Info("Azure MQTT: Publish successful.");
            }
            else
            {
                Resolver.Log.Warn($"Azure MQTT publish failed: {publishResult.ReasonCode}. Reason: {publishResult.ReasonString}");
            }

            await client.DisconnectAsync();
            Resolver.Log.Info("Azure MQTT: Disconnected.");
        }
        catch (OperationCanceledException)
        {
            Resolver.Log.Warn("Azure MQTT: Connection or publish operation timed out (canceled). Increasing timeout might be needed or network is unstable.");
        }
        catch (Exception ex)
        {
            Resolver.Log.Warn($"Azure MQTT publish error: {ex.GetType().Name}: {ex.Message}");
            if (ex.InnerException != null)
            {
                Resolver.Log.Warn($"Inner Exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            }
        }
        finally
        {
            semaphore.Release();
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
