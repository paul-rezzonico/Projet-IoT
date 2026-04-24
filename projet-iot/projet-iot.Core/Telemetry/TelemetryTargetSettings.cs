using System;
using System.Collections.Generic;
using System.IO;

namespace projet_iot.Core;

public sealed class TelemetryTargetSettings
{
    public bool? MeadowEnabled { get; set; }
    public bool? AzureEnabled { get; set; }
    public string? AzureHostname { get; set; }
    public string? AzureDeviceId { get; set; }
    public string? AzureSasToken { get; set; }
    public bool? AzureAllowUntrustedCertificates { get; set; }

    public static TelemetryTargetSettings Load(string filePath = "app.config.yaml")
    {
        if (!File.Exists(filePath))
        {
            return new TelemetryTargetSettings();
        }

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var sections = new Stack<(int Indent, string Key)>();

        foreach (var rawLine in File.ReadLines(filePath))
        {
            if (string.IsNullOrWhiteSpace(rawLine))
            {
                continue;
            }

            var line = rawLine.TrimEnd();
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            var colon = trimmed.IndexOf(':');
            if (colon <= 0)
            {
                continue;
            }

            var indent = line.Length - trimmed.Length;
            while (sections.Count > 0 && indent <= sections.Peek().Indent)
            {
                sections.Pop();
            }

            var key = trimmed[..colon].Trim();
            var value = trimmed[(colon + 1)..].Trim();

            if (value.Length == 0)
            {
                sections.Push((indent, key));
                continue;
            }

            var pathParts = new List<string>(sections.Count + 1);
            foreach (var section in sections)
            {
                pathParts.Insert(0, section.Key);
            }

            pathParts.Add(key);
            var path = string.Join(".", pathParts);
            values[path] = value;
        }

        return new TelemetryTargetSettings
        {
            MeadowEnabled = ReadBool(values, "Telemetry.MeadowEnabled"),
            AzureEnabled = ReadBool(values, "Telemetry.AzureEnabled") ?? ReadBool(values, "Telemetry.Azure.Enabled"),
            AzureHostname = ReadString(values, "Telemetry.Azure.Hostname"),
            AzureDeviceId = ReadString(values, "Telemetry.Azure.DeviceId"),
            AzureSasToken = ReadString(values, "Telemetry.Azure.SasToken"),
            AzureAllowUntrustedCertificates = ReadBool(values, "Telemetry.Azure.AllowUntrustedCertificates")
        };
    }

    private static string? ReadString(IReadOnlyDictionary<string, string> values, string key)
    {
        return values.TryGetValue(key, out var value) ? value : null;
    }

    private static bool? ReadBool(IReadOnlyDictionary<string, string> values, string key)
    {
        if (!values.TryGetValue(key, out var value))
        {
            return null;
        }

        return value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("on", StringComparison.OrdinalIgnoreCase);
    }
}
