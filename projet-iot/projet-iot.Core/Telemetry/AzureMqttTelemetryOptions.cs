using System;

namespace projet_iot.Core;

public sealed class AzureMqttTelemetryOptions
{
    public bool Enabled { get; set; }
    public string Hostname { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string SasToken { get; set; } = string.Empty;
    public bool AllowUntrustedCertificates { get; set; }

    public static AzureMqttTelemetryOptions FromEnvironment(TelemetryTargetSettings? settings = null)
    {
        var options = new AzureMqttTelemetryOptions
        {
            Hostname = ReadString("AZURE_IOT_HUB_HOSTNAME", settings?.AzureHostname),
            DeviceId = ReadString("AZURE_IOT_DEVICE_ID", settings?.AzureDeviceId),
            SasToken = ReadString("AZURE_IOT_SAS_TOKEN", settings?.AzureSasToken),
            AllowUntrustedCertificates = ReadBool(
                "AZURE_IOT_ALLOW_UNTRUSTED_CERTS",
                settings?.AzureAllowUntrustedCertificates ?? false)
        };

        var explicitEnabled = Environment.GetEnvironmentVariable("TELEMETRY_AZURE_ENABLED");
        if (!string.IsNullOrWhiteSpace(explicitEnabled))
        {
            options.Enabled = IsTrue(explicitEnabled);
        }
        else if (settings?.AzureEnabled is { } azureEnabledFromConfig)
        {
            options.Enabled = azureEnabledFromConfig;
        }
        else
        {
            options.Enabled =
                !string.IsNullOrWhiteSpace(options.Hostname) &&
                !string.IsNullOrWhiteSpace(options.DeviceId) &&
                !string.IsNullOrWhiteSpace(options.SasToken);
        }

        return options;
    }

    public bool HasCredentials()
    {
        return
            !string.IsNullOrWhiteSpace(Hostname) &&
            !string.IsNullOrWhiteSpace(DeviceId) &&
            !string.IsNullOrWhiteSpace(SasToken);
    }

    private static bool ReadBool(string variable, bool defaultValue)
    {
        var raw = Environment.GetEnvironmentVariable(variable);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return defaultValue;
        }

        return IsTrue(raw);
    }

    private static string ReadString(string variable, string? defaultValue)
    {
        var raw = Environment.GetEnvironmentVariable(variable);
        return string.IsNullOrWhiteSpace(raw) ? defaultValue ?? string.Empty : raw;
    }

    private static bool IsTrue(string value)
    {
        return value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("on", StringComparison.OrdinalIgnoreCase);
    }
}
