using System.Data.SqlClient;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Packets;
using System.Text.Json;
using Microsoft.Data.SqlClient;


namespace DatabaseConsole
{
    class Program
    {
        static IMqttClient mqttClient;
        static string connectionString = "Server=LAPTOP-MDVNPCGM;Database=meter_readings;Trusted_Connection=true;TrustServerCertificate=true";
        static string brokerIp = "172.16.103.199";
        static string dataTopic = "esp32/data/team_b_proj/meter_1123";

        static async Task Main(string[] args)
        {
            await StartMqtt();
            Console.ReadLine();
        }

        static async Task StartMqtt()
        {
            var factory = new MqttFactory();
            mqttClient = factory.CreateMqttClient();

            mqttClient.ApplicationMessageReceivedAsync += async e => await HandleMessage(e);
            mqttClient.ConnectedAsync += async e => await HandleConnected();
            mqttClient.DisconnectedAsync += async e => await HandleDisconnected();

            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(brokerIp,1881)
                .WithClientId("DatabaseConsole")
                .Build();

            await mqttClient.ConnectAsync(options);
            await mqttClient.SubscribeAsync(dataTopic);
        }

        static Task HandleMessage(MqttApplicationMessageReceivedEventArgs e)
        {
            string json = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
            SaveData(json);
            Console.WriteLine(json);
            return Task.CompletedTask;
        }

        static Task HandleConnected()
        {
            Console.WriteLine("Connected to MQTT broker");
            return Task.CompletedTask;
        }

        static Task HandleDisconnected()
        {
            Console.WriteLine("Disconnected from MQTT broker");
            return Task.CompletedTask;
        }

        static void SaveData(string json)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(json);
                JsonElement root = doc.RootElement;

                string timestamp =root.GetProperty("timestamp").GetString();
                string meterId = root.GetProperty("meter_id").GetString();
                string customerId = root.GetProperty("customer_id").GetString();
                JsonElement data = root.GetProperty("data");
                decimal voltage = data.GetProperty("voltage_reading").GetDecimal();
                decimal current = data.GetProperty("current_reading").GetDecimal();

                using SqlConnection connection = new SqlConnection(connectionString);
                connection.Open();

                string sql = "INSERT INTO MeterReadings (Timestamp, MeterId, CustomerId, VoltageReading, CurrentReading) VALUES (@t, @m, @c, @v, @a)";

                using SqlCommand command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@t", timestamp);
                command.Parameters.AddWithValue("@m", meterId);
                command.Parameters.AddWithValue("@c", customerId);
                command.Parameters.AddWithValue("@v", voltage);
                command.Parameters.AddWithValue("@a", current);

                command.ExecuteNonQuery();
                Console.WriteLine($"Saved data for meter: {meterId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}