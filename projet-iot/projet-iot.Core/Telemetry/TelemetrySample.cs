using System;

namespace projet_iot.Core;

public sealed class TelemetrySample
{
    public DateTime TimestampUtc { get; set; }
    public double TemperatureC { get; set; }
    public double ThresholdC { get; set; }
    public double? PressurePa { get; set; }
    public bool NetworkConnected { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool IsThresholdEvent { get; set; }
    public bool? IsBelowThreshold { get; set; }

    public static TelemetrySample CreateInterval(
        double temperatureC,
        double? pressurePa,
        double thresholdC,
        bool networkConnected,
        string reason)
    {
        return new TelemetrySample
        {
            TimestampUtc = DateTime.UtcNow,
            TemperatureC = temperatureC,
            PressurePa = pressurePa,
            ThresholdC = thresholdC,
            NetworkConnected = networkConnected,
            Reason = reason,
            IsThresholdEvent = false,
            IsBelowThreshold = null
        };
    }

    public static TelemetrySample CreateThresholdCross(
        double temperatureC,
        double thresholdC,
        bool isBelowThreshold)
    {
        return new TelemetrySample
        {
            TimestampUtc = DateTime.UtcNow,
            TemperatureC = temperatureC,
            ThresholdC = thresholdC,
            NetworkConnected = false,
            Reason = "threshold-cross",
            IsThresholdEvent = true,
            IsBelowThreshold = isBelowThreshold
        };
    }
}
