#include "FS.h"
#include <ESP8266WiFi.h>
#include <ESP8266HTTPClient.h>
#include <RTCVars.h>
#include <ArduinoJson.h>
ADC_MODE(ADC_VCC);

RTCVars state;

//#define DEBUG
#define N 2 // максимальное количество счетчиков

const char stateFile[] = "/state123456.bin";
const char sendedFile[] = "/sended123456.bin";
const int pins[N] = {5, 4/*, 0, 2, 14, 12, 13, 15*/}; // пины счетчиков (не изменяются)

int sleepTime = 4000; // время между циклами сна в мс
int serverSendDelta = 50; // периодичность отправки на сервер в литрах
int vcc = 0; // текущее напряжение питания в мВ

struct
{
  uint32_t userID = 1;              // идентификатор пользователя на сервере
  char url[128] = "http://194.54.66.110/api/MeterReadings"; // адрес для отправки на сервер
  char names[N][32] = {"44958160", "44354311"};            // идентификаторы счетчиков
  uint32_t valueOfDivisions[N] = {10, 10}; // литров на импульс счетчиков
  uint32_t offsets[N] = {3566, 5015};          // начальное значение отсчета счетчиков в литрах
} conf;

int rtcPinStates[N];
int rtcLitStates[N];
int rtcLastSend;
int rtcShouldSend = 0;

struct
{
  uint32_t states[N + 1]; // сначала текущее кол-во литров, затем сумма последних отправленных значений на сервер
} spiffState;

uint32_t IntFromByteArr(uint8_t *bytes);
void IntToByteArr(uint32_t n, uint8_t *bytes);
bool sendToServer(uint32_t *values);
void registerAndReadRtc();
void writeSpiffState();
void writeSpiffSended();
void checkCycle();
void loadStateFromSpiff();


void setup()
{
  vcc = ESP.getVcc();

  for (int i = 0; i < N; i++)
    pinMode(pins[i], INPUT_PULLUP);

#ifdef DEBUG
  Serial.begin(115200);
  Serial.setTimeout(2000);
  while (!Serial) { }
  Serial.println();
  Serial.print("vcc: ");
  Serial.println(vcc);
#endif

  registerAndReadRtc();
  checkCycle();

  int sum = 0;
  for (int i = 0; i < N; i++)
    sum += rtcLitStates[i];

  if (rtcShouldSend)
  {
    sendToServer();
    rtcLastSend = sum;
    rtcShouldSend = 0;
    state.saveToRTC();

  }

  if (sum - serverSendDelta >= rtcLastSend)
  {
    rtcShouldSend++;
    state.saveToRTC();
    ESP.deepSleep(sleepTime * 1000);
  }

  ESP.deepSleep(sleepTime * 1000, WAKE_RF_DISABLED);
}

void loop()
{

}

bool sendToServer()
{
  unsigned long timems = millis();
#ifdef DEBUG
  Serial.println("Connecting to wi-fi...");
#endif
  WiFi.mode(WIFI_STA);
  WiFi.begin("HUAWEI-42", "123asdqwe");

  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    checkCycle();
    if (millis() - timems > 10000)
    {
#ifdef DEBUG
      Serial.println("Couldn't connect to wi-fi.");
#endif
      return false;
    }
  }

#ifdef DEBUG
  Serial.println("Wi-Fi connected!");
#endif

  if ((WiFi.status() == WL_CONNECTED)) {

    WiFiClient client;
    HTTPClient http;

#ifdef DEBUG
    Serial.print("[HTTP] begin...\n");
#endif

    // configure traged server and url
    http.begin(client, conf.url); //HTTP
    http.addHeader("Content-Type", "application/json");

DynamicJsonDocument doc(1024);    
doc["UserId"] = conf.userID;
doc["Vcc"]   = vcc;
for (int i = 0; i < N; i++)
{
  doc["Meters"][i]["Number"] = conf.names[i];
  doc["Meters"][i]["Value"] = rtcLitStates[i];
}
String postJson;
serializeJson(doc, postJson);

#ifdef DEBUG
    Serial.print("[HTTP] POST...\n");
#endif
    // start connection and send HTTP header and body
    int httpCode = http.POST(postJson);

    // httpCode will be negative on error
    if (httpCode > 0) {
      // HTTP header has been send and Server response header has been handled
#ifdef DEBUG
      Serial.printf("[HTTP] POST... code: %d\n", httpCode);
#endif

      // file found at server
      if (httpCode == HTTP_CODE_OK) {
#ifdef DEBUG
        const String& payload = http.getString();
        Serial.println("received payload:\n<<");
        Serial.println(payload);
        Serial.println(">>");
#endif
      }
    } else {
#ifdef DEBUG
      Serial.printf("[HTTP] POST... failed, error: %s\n", http.errorToString(httpCode).c_str());
#endif
      http.end();
      return false;
    }

    http.end();
  }

  return true;
}

void loadStateFromSpiff()
{
  if (SPIFFS.begin())
  {
    File fileState = SPIFFS.open(stateFile, "r");
    File fileSended = SPIFFS.open(sendedFile, "r");
    if (fileState && fileSended)
    {
      uint8_t a[4]; // временный массив байтов для записи в бинарный файл
      for (int i = 0; i < N; i++)
      {
        fileState.read(a, 4);
        rtcLitStates[i] = IntFromByteArr(a);
      }

      fileSended.read(a, 4);
      rtcLastSend = IntFromByteArr(a);

      fileState.close();
      fileSended.close();
      SPIFFS.end();
    }
    else
    {
      for (int i = 0; i < N; i++)
      {
        rtcPinStates[i] = digitalRead(pins[i]);
        rtcLitStates[i] = conf.offsets[i];
      }

      rtcLastSend = -serverSendDelta;

      writeSpiffState();
      writeSpiffSended();
    }
  }
}

void registerAndReadRtc()
{
  state.registerVar(&rtcLastSend);
  state.registerVar(&rtcShouldSend);
  for (int i = 0; i < N; i++)
  {
    state.registerVar(&(rtcPinStates[i]));
    state.registerVar(&(rtcLitStates[i]));
  }

  if (state.loadFromRTC())
  {
#ifdef DEBUG
    Serial.println("Data successfully read from RTC");
#endif
  }
  else
  {
#ifdef DEBUG
    Serial.println("This seems to be a cold boot. We don't have a valid state on RTC memory");
#endif

    loadStateFromSpiff();
  }
}

void checkCycle()
{
  int values[N];
  bool isChanged = false;

  // считываем показания датчиков
  for (int i = 0; i < N; i++)
    values[i] = digitalRead(pins[i]);

  for (int i = 0; i < N; i++)
  {
#ifdef DEBUG
    Serial.print(rtcPinStates[i]);
    Serial.print(" -> ");
    Serial.println(values[i]);
#endif

    if (rtcPinStates[i] - values[i] == 1)
    {
      isChanged = true;
#ifdef DEBUG
      Serial.print(rtcLitStates[i]);
      Serial.print(" -> ");
#endif
      rtcLitStates[i] += conf.valueOfDivisions[i];
#ifdef DEBUG
      Serial.println(rtcLitStates[i]);
#endif
    }

    rtcPinStates[i] = values[i];
  }

  state.saveToRTC();

  if (isChanged)
  {
    writeSpiffState();
  }
}

void writeSpiffState()
{
  if (SPIFFS.begin())
  {
    uint8_t a[4]; // временный массив байтов для записи в бинарный файл

    File fileState = SPIFFS.open(stateFile, "w");
    for (int i = 0; i < N; i++)
    {
      IntToByteArr(rtcLitStates[i], a);
      fileState.write(a, 4);
    }

    fileState.close();
    SPIFFS.end();
  }
}

void writeSpiffSended()
{
  if (SPIFFS.begin())
  {
    uint8_t a[4]; // временный массив байтов для записи в бинарный файл

    File fileState = SPIFFS.open(sendedFile, "w");
    IntToByteArr(rtcLastSend, a);
    fileState.write(a, 4);

    fileState.close();
    SPIFFS.end();
  }
}

uint32_t IntFromByteArr(uint8_t *bytes)
{
  uint32_t myInt1 = (bytes[0] << 24) + (bytes[1] << 16) + (bytes[2] << 8) + bytes[3];
  return myInt1;
}

void IntToByteArr(uint32_t n, uint8_t *bytes)
{
  bytes[0] = (n >> 24) & 0xFF;
  bytes[1] = (n >> 16) & 0xFF;
  bytes[2] = (n >> 8) & 0xFF;
  bytes[3] = n & 0xFF;
}
