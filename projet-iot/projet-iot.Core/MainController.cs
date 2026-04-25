using System;
using System.Threading.Tasks;
using Meadow;
using Meadow.Units;
using projet_iot.Core.Contracts;

namespace projet_iot.Core;

public class MainController
{
    private readonly ITelemetryPublisher telemetryPublisher;

    private Iprojet_iotHardware? hardware;

    private CloudController? cloudController;
    private ConfigurationController? configurationController;
    private DisplayController? displayController;
    private InputController? inputController;
    private SensorController? sensorController;

    private Iprojet_iotHardware Hardware => hardware ?? throw new InvalidOperationException("MainController is not initialized.");
    private CloudController CloudController => cloudController ?? throw new InvalidOperationException("MainController is not initialized.");
    private ConfigurationController ConfigurationController => configurationController ?? throw new InvalidOperationException("MainController is not initialized.");
    private DisplayController DisplayController => displayController ?? throw new InvalidOperationException("MainController is not initialized.");
    private InputController InputController => inputController ?? throw new InvalidOperationException("MainController is not initialized.");
    private SensorController SensorController => sensorController ?? throw new InvalidOperationException("MainController is not initialized.");
    private IOutputController OutputController => Hardware.OutputController;
    private INetworkController NetworkController => Hardware.NetworkController;

    private Temperature.UnitType units;
    private Temperature currentTemperature;
    private Temperature thresholdTemperature;
    private bool? isBelowThreshold;
    private DateTime lastTelemetryPublishUtc = DateTime.MinValue;

    private static readonly TimeSpan TelemetryInterval = TimeSpan.FromSeconds(30);

    public MainController(ITelemetryPublisher? telemetryPublisher = null)
    {
        this.telemetryPublisher = telemetryPublisher ?? NoOpTelemetryPublisher.Instance;
    }

    public async Task Initialize(Iprojet_iotHardware hardware)
    {
        if (hardware is null)
        {
            throw new ArgumentNullException(nameof(hardware));
        }

        this.hardware = hardware;

        this.thresholdTemperature = 68.Fahrenheit();

        // create generic services
        configurationController = new ConfigurationController();
        cloudController = new CloudController(Resolver.CommandService, telemetryPublisher);
        sensorController = new SensorController(hardware);
        inputController = new InputController(hardware);

        units = ConfigurationController.Units;
        thresholdTemperature = ConfigurationController.ThresholdTemp;

        displayController = new DisplayController(
            Hardware.Display,
            Hardware.DisplayRotation,
            units);

        // connect events
        SensorController.CurrentTemperatureChanged += OnCurrentTemperatureChanged;
        CloudController.UnitsChangeRequested += OnUnitsChangeChangeRequested;
        CloudController.ThresholdTemperatureChangeRequested += OnThresholdTemperatureChangeRequested;
        InputController.UnitDownRequested += OnUnitDownRequested;
        InputController.UnitUpRequested += OnUnitUpRequested;
        NetworkController.NetworkStatusChanged += OnNetworkStatusChanged;

        await SensorController.InitializeAsync();

        _ = NetworkController.Connect();
    }

    private void OnNetworkStatusChanged(object sender, EventArgs e)
    {
        Resolver.Log.Info($"Network state changed to {NetworkController.IsConnected}");
        DisplayController.SetNetworkStatus(NetworkController.IsConnected);
    }

    private void CheckTemperaturesAndSetOutput()
    {
        var belowThreshold = currentTemperature < thresholdTemperature;
        OutputController?.SetState(belowThreshold);

        if (isBelowThreshold is null)
        {
            isBelowThreshold = belowThreshold;
            return;
        }

        if (belowThreshold != isBelowThreshold.Value)
        {
            isBelowThreshold = belowThreshold;
            _ = CloudController.PublishThresholdEventAsync(currentTemperature, thresholdTemperature, belowThreshold);
            _ = CloudController.PublishTelemetryAsync(
                currentTemperature,
                TryGetPressurePa(),
                thresholdTemperature,
                NetworkController.IsConnected,
                "threshold-cross");
        }
    }

    private void OnCurrentTemperatureChanged(object sender, Temperature temperature)
    {
        currentTemperature = temperature;

        CheckTemperaturesAndSetOutput();

        if (DateTime.UtcNow - lastTelemetryPublishUtc >= TelemetryInterval)
        {
            lastTelemetryPublishUtc = DateTime.UtcNow;
            _ = CloudController.PublishTelemetryAsync(
                currentTemperature,
                TryGetPressurePa(),
                thresholdTemperature,
                NetworkController.IsConnected,
                "interval");
        }

        // update the UI
        DisplayController.UpdateCurrentTemperature(currentTemperature);
    }

    private double? TryGetPressurePa()
    {
        return Hardware.PressureSensor is null ? null : SensorController.CurrentPressure.Pascal;
    }

    private void OnUnitsChangeChangeRequested(object sender, Temperature.UnitType units)
    {
        DisplayController.UpdateDisplayUnits(units);
    }

    private void OnThresholdTemperatureChangeRequested(object sender, Temperature e)
    {
        thresholdTemperature = e;
        ConfigurationController.ThresholdTemp = e;
        ConfigurationController.Save();
    }

    private void OnUnitDownRequested(object sender, EventArgs e)
    {
        units = units switch
        {
            Temperature.UnitType.Celsius => Temperature.UnitType.Kelvin,
            Temperature.UnitType.Fahrenheit => Temperature.UnitType.Celsius,
            _ => Temperature.UnitType.Fahrenheit,
        };

        DisplayController.UpdateDisplayUnits(units);
        ConfigurationController.Units = units;
        ConfigurationController.Save();
    }

    private void OnUnitUpRequested(object sender, EventArgs e)
    {
        units = units switch
        {
            Temperature.UnitType.Celsius => Temperature.UnitType.Fahrenheit,
            Temperature.UnitType.Fahrenheit => Temperature.UnitType.Kelvin,
            _ => Temperature.UnitType.Celsius,
        };

        DisplayController.UpdateDisplayUnits(units);
        ConfigurationController.Units = units;
        ConfigurationController.Save();
    }

    public async Task Run()
    {
        while (true)
        {
            // print internet status
            Console.WriteLine($"Network connected: {NetworkController.IsConnected}");
            // Print the current temperature.
            Console.WriteLine($"Current Temperature: {currentTemperature.ToString()}");
            //Print Ip address
            Console.WriteLine($"IP Address: {NetworkController.IpAddress}");
            

            await Task.Delay(10000);
        }
    }
}
