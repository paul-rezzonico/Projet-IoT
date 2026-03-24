using Meadow;
using Meadow.Devices;
using Meadow.Foundation.Sensors.Atmospheric;
using System;
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

            // 0x76 (SDO=GND) ou 0x77 (SDO=3V3)
            bmp = new Bmp280(i2CBus, address:0x76);

            // Abonnement à l'événement Updated (pattern Meadow.Foundation)
            bmp.Updated += (s, result) =>
            {
                var (t, p) = result.New;

                if (p is not null)
                    Console.WriteLine($"Pressure: {p.Value.Pascal:F2 / 100} hPa");

                if (t is not null)
                    Console.WriteLine($"Temp: {t.Value.Celsius:F2} °C");
            };

            // Lecture ponctuelle
            var (temperature, pressure) = await bmp.Read();
            if (pressure is not null)
                Console.WriteLine($"(Read) Pressure: {pressure.Value.Pascal:F2 / 100} hPa");
            if (temperature is not null)
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