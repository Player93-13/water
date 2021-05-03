#include "FS.h"
#include <ESP8266WiFi.h>
#include <ESP8266HTTPClient.h>
ADC_MODE(ADC_VCC);

#define DEBUG
#define N 2 // максимальное количество счетчиков

uint32_t states[N]; // сохраненное предыдущее состояние счетчика

int pins[N] = {5, 4/*, 0, 2, 14, 12, 13, 15*/}; // пины счетчиков (не изменяются)

int sleepTime = 2000; // время между циклами сна в мс
int serverSendDelta = 20; // периодичность отправки на сервер в литрах
int vcc = 0; // текущее напряжение питания в мВ

struct
{
  uint32_t userID = 12345;              // идентификатор пользователя на сервере
  char url[128] = "http://csz.mrsu.ru/"; // адрес для отправки на сервер
  char names[N][32] = {"1212", "2323"};            // идентификаторы счетчиков
  uint32_t valueOfDivisions[N] = {10, 10}; // литров на импульс счетчиков
  uint32_t offsets[N] = {1300, 1600};          // начальное значение отсчета счетчиков в литрах
} conf;

struct
{
  uint32_t crc32;
  uint32_t states[N * 2 + 1]; // сначала состояния пинов счетчика, затем текущее кол-во литров, затем сумма последних отправленных значений на сервер
} rtcState;

struct
{
  uint32_t states[N + 1]; // сначала текущее кол-во литров, затем сумма последних отправленных значений на сервер
} spiffState;

uint32_t calculateCRC32(const uint8_t *data, size_t length) {
  uint32_t crc = 0xffffffff;
  while (length--) {
    uint8_t c = *data++;
    for (uint32_t i = 0x80; i > 0; i >>= 1) {
      bool bit = crc & 0x80000000;
      if (c & i) {
        bit = !bit;
      }
      crc <<= 1;
      if (bit) {
        crc ^= 0x04c11db7;
      }
    }
  }
  return crc;
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

bool sendToServer(uint32_t *values)
{
  unsigned long timems = millis();
#ifdef DEBUG
  Serial.println("Connecting to wi-fi...");
#endif
  WiFi.mode(WIFI_STA);
  WiFi.begin("HUAWEI-42", "123asdqwe");

  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }

#ifdef DEBUG
  Serial.println("Wi-Fi connected!");
#endif

  HTTPClient http;
  http.begin(conf.url);

  String payload;
  if (http.GET() == HTTP_CODE_OK)
    payload = http.getString();
#ifdef DEBUG
  Serial.println(payload);
#endif



  return true;
}

void getSpiffState()
{

}

// return true если значение найдено в памяти, false - если первая инициализация при включении
bool reedRtcState()
{
  if (ESP.rtcUserMemoryRead(0, (uint32_t*) &rtcState, sizeof(rtcState)))
  {
    uint32_t crcOfData = calculateCRC32((uint8_t*) &rtcState.states[0], sizeof(states) * 4);
    if (crcOfData != rtcState.crc32) {
#ifdef DEBUG
      Serial.println("CRC32 in RTC memory doesn't match CRC32 of data. Data is probably invalid!");
#endif

      getSpiffState();
      for (int i = N; i < N * 2 + 1; i++)
      {
        rtcState.states[i] = spiffState.states[i];
      }

      return false;
    }
    return true;
  }
  return false;
}

void writeRtcState()
{
  rtcState.crc32 = calculateCRC32((uint8_t*) &rtcState.states[0], sizeof(states) * 4);
  if (ESP.rtcUserMemoryWrite(0, (uint32_t*) &rtcState, sizeof(rtcState))) { }
}

bool checkCycle()
{
  bool result = true; // возвращаемый результат успешной операции
  int values[N];
  bool isChanged = false;
  
  // считываем показания датчиков
  for (int i = 0; i < N; i++)
    values[i] = digitalRead(pins[i]);

  if (reedRtcState())
  {
    for (int i = 0; i < N; i++)
    {
#ifdef DEBUG
      Serial.print(states[i]);
      Serial.print(" -> ");
      Serial.println(values[i]);
#endif

      if (states[i] - values[i] == 1)
        isChanged = true;
    }

    // если показания изменились то сохраняем во флеш и отправляем на сервер по необходимости
    if (isChanged)
    {
      if (SPIFFS.begin())
      {
        //SPIFFS.remove("/state.bin");
        uint8_t a[4]; // временный массив байтов для записи в бинарный файл
        File fileState = SPIFFS.open("/state.bin", "r+");
        if (!fileState)
        {
          fileState = SPIFFS.open("/state.bin", "w");
          for (int i = 0; i < N; i++)
          {
            IntToByteArr(conf.offsets[i] + conf.valueOfDivisions[i], a);
            fileState.write(a, 4);
          }
          IntToByteArr(0, a);
          fileState.write(a, 4);
        }
        else
        {
          uint32_t spValues[N];
          uint32_t sum = 0;
          for (int i = 0; i < N; i++)
          {
            fileState.read(a, 4);
            spValues[i] = IntFromByteArr(a);

            if (states[i] - values[i] == 1)
              spValues[i] += conf.valueOfDivisions[i];

            sum += spValues[i];
          }

          fileState.read(a, 4);
          uint32_t serverSended = IntFromByteArr(a);
          if (sum - serverSended >= serverSendDelta)
          {
            result = sendToServer(spValues);
            serverSended = sum;
#ifdef DEBUG
            Serial.print("Server sended: ");
            Serial.println(serverSended);
#endif
          }

          fileState.seek(0);

          for (int i = 0; i < N; i++)
          {
            IntToByteArr(spValues[i], a);
            fileState.write(a, 4);

#ifdef DEBUG
            Serial.println(spValues[i]);
#endif
          }

          IntToByteArr(serverSended, a);
          fileState.write(a, 4);
        }

        fileState.close();
      }
    }
  }


  // сохраняем показания датчиков в память RTC
  for (int i = 0; i < N; i++)
    states[i] = values[i];
  writeRtcState();

  return result;
}

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

  if (checkCycle())
    ESP.deepSleep(sleepTime * 1000, WAKE_RF_DISABLED);
  //ESP.deepSleep(sleepTime * 1000);
}

void loop()
{
  if (checkCycle()) { }

  ESP.deepSleep(sleepTime, WAKE_RF_DISABLED);
}
