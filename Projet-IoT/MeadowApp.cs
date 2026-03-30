using Meadow;
using Meadow.Devices;
using Meadow.Foundation.Sensors.Atmospheric;
using System;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using System.Text;

namespace MeadowApp
{
    public class MeadowApp : App<F7FeatherV2>
    {
        Bmp280? bmp;
        
        public bool sendDataToAzure(double temperature, double pressure)
        {
            try
            {
                // Azure IoT Hub Configuration
                string hostname = "meadow-iot-hub.azure-devices.net";
                string deviceId = "meadow-device";
                string sasToken = "SharedAccessSignature sr=meadow-iot-hub.azure-devices.net%2Fdevices%2Fmeadow-device&sig=NNGAYMWdTucxAe%2BSysHUMMl%2BWNNd3XJ3d2uqS7o2PXo%3D&se=1774344713";
                
                // MQTT Configuration
                string clientId = $"{hostname}/{deviceId}/?api-version=2021-04-12";
                string username = $"{hostname}/{deviceId}/?api-version=2021-04-12";
                string topic = $"devices/{deviceId}/messages/events/";
                
                // Create MQTT client
                var client = new MqttFactory().CreateMqttClient();
                
                var options = new MqttClientOptionsBuilder()
                    .WithTcpServer(hostname, 8883)
                    .WithTlsOptions(new MqttClientTlsOptionsBuilder()
                        .WithAllowUntrustedCertificates()
                        .Build())
                    .WithCredentials(username, sasToken)
                    .WithClientId(clientId)
                    .Build();
                
                // Connect to Azure IoT Hub
                var connectResult = client.ConnectAsync(options).Result;
                
                if (connectResult.ResultCode != MqttClientConnectResultCode.Success)
                {
                    Console.WriteLine($"Failed to connect to Azure IoT Hub: {connectResult.ResultCode}");
                    return false;
                }
                
                Console.WriteLine("Successfully connected to Azure IoT Hub");
                
                // Prepare JSON payload using string formatting (compatible with netstandard2.1)
                string jsonPayload = $"{{\"temperature\":{temperature},\"pressure\":{pressure},\"timestamp\":\"{DateTime.UtcNow:O}\"}}";
                
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(jsonPayload)
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build();
                
                // Send message
                var publishResult = client.PublishAsync(message).Result;
                
                if (publishResult.IsSuccess)
                {
                    Console.WriteLine($"Message sent successfully: {jsonPayload}");
                }
                else
                {
                    Console.WriteLine($"Failed to send message: {publishResult.ReasonCode}");
                }
                
                // Disconnect
                client.DisconnectAsync().Wait();
                
                return publishResult.IsSuccess;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending data to Azure: {ex.Message}");
                return false;
            }
        }

        public void sendFakeData()
        {            
            double fakeTemperature = 25.0;
            double fakePressure = 101325.0;
            sendDataToAzure(fakeTemperature, fakePressure);
        }

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