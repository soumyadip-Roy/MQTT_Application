using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;

namespace AdminApplication
{
    internal class Program
    {
        private static IMqttClient mqttClient;
        private static string brokerIp = "172.16.103.199";
        private static int brokerPort = 1881;
        private static string requestTopic = "esp32/admin/team_b_proj/request";
        private static string responseTopic = "esp32/admin/team_b_proj/response";

        static async Task Main(string[] args)
        {
            await InitializeMqtt();
            await ShowMenu();
        }

        static async Task InitializeMqtt()
        {
            var factory = new MqttFactory();
            mqttClient = factory.CreateMqttClient();

            mqttClient.ApplicationMessageReceivedAsync += e => HandleResponse(e);
            mqttClient.ConnectedAsync += e => HandleConnected();
            mqttClient.DisconnectedAsync += e => HandleDisconnected();

            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(brokerIp, brokerPort)
                .WithClientId("AdminConsole")
                .Build();

            await mqttClient.ConnectAsync(options);
            await mqttClient.SubscribeAsync(responseTopic);
        }

        static Task HandleResponse(MqttApplicationMessageReceivedEventArgs e)
        {
            var message = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
            Console.WriteLine($"\n[{DateTime.UtcNow}] Response: {message}");
            return Task.CompletedTask;
        }

        static Task HandleConnected()
        {
            Console.WriteLine($"\n[{DateTime.UtcNow}] Connected to MQTT broker");
            return Task.CompletedTask;
        }

        static Task HandleDisconnected()
        {
            Console.WriteLine($"\n[{DateTime.UtcNow}] Disconnected from MQTT broker");
            return Task.CompletedTask;
        }

        static async Task ShowMenu()
        {
            while (true)
            {
                Console.WriteLine("\n<========== Admin Console ==========>");
                Console.WriteLine("[1]> Start Device");
                Console.WriteLine("[2]> Stop Device");
                Console.WriteLine("[3]> Check Status");
                Console.WriteLine("[4]> Exit");
                Console.Write("Select option: ");

                var input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        await SendCommand("start");
                        break;
                    case "2":
                        await SendCommand("stop");
                        break;
                    case "3":
                        await SendCommand("status");
                        break;
                    case "4":
                        await mqttClient.DisconnectAsync();
                        return;
                    default:
                        Console.WriteLine("Invalid option");
                        break;
                }

                await Task.Delay(1000);
            }
        }

        static async Task SendCommand(string command)
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(requestTopic)
                .WithPayload(command)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            await mqttClient.PublishAsync(message);
            Console.WriteLine($"\n[{DateTime.UtcNow}] Sent command: {command}");
        }
    }
}