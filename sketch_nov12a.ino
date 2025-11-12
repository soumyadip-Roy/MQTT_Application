#include <NTPClient.h>


#include <WiFiAP.h>

#include <WiFiUdp.h>

#include <TimeLib.h>

#include <ArduinoJson.h>
#include <ArduinoJson.hpp>

#include <WiFi.h>

#include <PubSubClient.h>

WiFiClient espClient;
WiFiUDP ntpUDP;
PubSubClient client(espClient);
NTPClient timeClient(ntpUDP, "pool.ntp.org",19800,60000);
char meter_data[500];
char time_string[20];

const char* ssid = "Esya-Training";
const char* passwd = "P@$$w0rd@123";
const char* mqtt_ip = "172.16.103.199";

const char* topic_data = "esp32/data/team_b_proj/meter_1123";
const char* topic_admin_request = "esp32/admin/team_b_proj/request";
const char* topic_admin_response = "esp32/admin/team_b_proj/response";

#define LED_PIN 2

bool deviceRunning = false;
String inputString = "";
bool stringComplete = false;

void callback(char *topic, uint8_t *payload, unsigned int length) {
  String message = "";
  for (int i = 0; i < length; i++) {
    message += (char)payload[i];
  }
  
  message.trim();
  message.toLowerCase();
  
  String response = "";
  
  if (message == "start") {
    deviceRunning = true;
    digitalWrite(LED_PIN, HIGH);
    response = "Device started";
    Serial.println("MQTT: Device started");
  }
  else if (message == "stop") {
    deviceRunning = false;
    digitalWrite(LED_PIN, LOW);
    response = "Device stopped";
    Serial.println("MQTT: Device stopped");
  }
  else if (message == "status") {
    if (deviceRunning) {
      response = "Device is running";
    } else {
      response = "Device is stopped";
    }
  }
  else {
    response = "Unknown command";
  }
  client.publish(topic_admin_response, response.c_str());
}

float getVoltage() {
  return 220.0 + (random(0, 75) / 10.0);
}

float getCurrent(){
  return 10 + (random(10,90)/10.0);

}

void setup() {
  Serial.begin(115200);
  
  pinMode(LED_PIN, OUTPUT);
  digitalWrite(LED_PIN, LOW);
  
  WiFi.begin(ssid, passwd);
  timeClient.begin();
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }
  Serial.println("WiFi Connected!");
  
  client.setServer(mqtt_ip, 1881);
  
  if (client.connect("ESP32_Soumyadip")) {
    client.subscribe(topic_admin_request);
    client.setCallback(callback);
  }
  
  // randomSeed(analogRead(0));
}

void reconnect() {
  while (!client.connected()) {
    if (client.connect("ESP32_Soumyadip")) {
      client.subscribe(topic_admin_request);
    } else {
      delay(5000);
    }
  }
}

void loop() {
  if (!client.connected()) {
    reconnect();
  }
  client.loop();
  timeClient.update();
  
  if (deviceRunning) {
    
    JsonDocument meterDataObject;
    meterDataObject["timestamp"] = String(timeClient.getFormattedTime());
    meterDataObject["meter_id"] = "1123";
    meterDataObject["customer_id"] = "UAIN1234";
    JsonObject data = meterDataObject.createNestedObject("data");
    data["voltage_reading"] = getVoltage();
    data["current_reading"] = getCurrent();
    
    serializeJson(meterDataObject,meter_data,sizeof(meter_data)); 
    
    client.publish(topic_data, meter_data);
    Serial.println("Message Sent: " + String(meter_data));
    delay(5000);
  }
  delay(100);
}