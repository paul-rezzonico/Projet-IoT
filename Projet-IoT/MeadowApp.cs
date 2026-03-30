using Meadow;
using Meadow.Devices;
using Meadow.Foundation.Sensors.Atmospheric;
using Meadow.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MeadowApp
{
    public class MeadowApp : App<F7FeatherV2>
    {
        Bmp280? bmp;

        public override async Task Initialize()
        {
            Console.WriteLine("Initialize...");

            var i2CBus = Device.CreateI2cBus();

            bmp = new Bmp280(i2CBus, address: 0x76);

            bmp.Updated += (s, result) =>
            {
                var (t, p) = result.New;

                double? tempC = t?.Celsius;
                double? pressureHpa = p != null ? p.Value.Pascal / 100.0 : null;

                if (pressureHpa != null)
                    Console.WriteLine($"Pressure: {pressureHpa:F2} hPa");

                if (tempC != null)
                    Console.WriteLine($"Temp: {tempC:F2} °C");

                // Envoi vers Meadow.Cloud
                if (tempC != null || pressureHpa != null)
                {
                    var measurements = new Dictionary<string, object>();

                    if (tempC != null)
                        measurements["temperature"] = tempC.Value;

                    if (pressureHpa != null)
                        measurements["pressure"] = pressureHpa.Value;

                    Resolver.Log.Info("Sending data to Meadow.Cloud...");

                    Resolver.Services.Get<CloudLogger>()?
                        .LogEvent(
                            eventId: 1,
                            description: "BMP280 readings",
                            measurements: measurements
                        );
                }
            };

            // Lecture initiale
            var (temperature, pressure) = await bmp.Read();

            if (pressure != null)
                Console.WriteLine($"(Read) Pressure: {pressure.Value.Pascal / 100.0:F2} hPa");

            if (temperature != null)
                Console.WriteLine($"(Read) Temp: {temperature.Value.Celsius:F2} °C");
        }

        public override Task Run()
        {
            if (bmp is null) return Task.CompletedTask;

            Console.WriteLine("StartUpdating every 1s...");
            bmp.StartUpdating(TimeSpan.FromSeconds(1));

            return Task.CompletedTask;
        }
    }
}