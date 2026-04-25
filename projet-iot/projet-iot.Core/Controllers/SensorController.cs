using System;
using System.Threading.Tasks;
using Meadow;
using Meadow.Peripherals.Sensors;
using Meadow.Units;
using projet_iot.Core.Contracts;

namespace projet_iot.Core;

public class SensorController
{
    private readonly Iprojet_iotHardware platform;

    private Temperature _temperature;
    private Pressure _pressure;

    public event EventHandler<Temperature>? CurrentTemperatureChanged;

    public Temperature CurrentTemperature
    {
        get => _temperature;
        private set
        {
            if (value == _temperature) return;
            _temperature = value;
            CurrentTemperatureChanged?.Invoke(this, _temperature);
        }
    }

    public Pressure CurrentPressure
    {
        get => _pressure;
        private set
        {
            if (value == _pressure) return;
            _pressure = value;
        }
    }

    public SensorController(Iprojet_iotHardware platform)
    {
        this.platform = platform;

        if (platform.TemperatureSamplingSensor is { } temperatureSensor)
        {
            temperatureSensor.Updated += OnTemperatureUpdated;
        }

        if (platform.PressureSamplingSensor is { } pressureSensor)
        {
            pressureSensor.Updated += OnPressureUpdated;
        }
    }

    /// <summary>?
    /// Performs an initial one-shot read of the sensor (mirrors the bmp.Read() call
    /// from MeadowApp.Initialize()) and starts continuous updates every second.
    /// Call this from MainController.Initialize().
    /// </summary>
    public async Task InitializeAsync()
    {
        if (platform.TemperatureSensor is ISensor<Temperature> temperatureReader)
        {
            var temperature = await temperatureReader.Read();
            // Resolver.Log.Info($(Read) Temp: {temperature.Celsius:F2} °C");
            CurrentTemperature = temperature;
        }

        if (platform.PressureSensor is ISensor<Pressure> pressureReader)
        {
            var pressure = await pressureReader.Read();
            // Resolver.Log.Info($"(Read) Pressure: {pressure.Pascal / 100:F2} hPa");
            CurrentPressure = pressure;
        }

        platform.TemperatureSamplingSensor?.StartUpdating(TimeSpan.FromSeconds(1));
        platform.PressureSamplingSensor?.StartUpdating(TimeSpan.FromSeconds(1));
    }

    private void OnTemperatureUpdated(object sender, IChangeResult<Temperature> result)
    {
        // Resolver.Log.Info($"Temp: {result.New.Celsius:F2} °C");
        CurrentTemperature = result.New;
    }

    private void OnPressureUpdated(object sender, IChangeResult<Pressure> result)
    {
        // Resolver.Log.Info($"Pressure: {result.New.Pascal / 100:F2} hPa");
        CurrentPressure = result.New;
    }
}
