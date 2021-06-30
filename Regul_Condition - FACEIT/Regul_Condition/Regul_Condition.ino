/*Name:		Regul_Condition.ino
Created : 05 / 01 / 2021 09 : 41 : 36
Author : pierr
*/

#include <ModbusRtu.h>
#include <TimeLib.h>
#include <EEPROMex.h>
#include <ArduinoJson.h>
#include "C:\Users\pierr\OneDrive\Documents\Arduino\libraries\cocorico2\Hamilton.h"
#include "C:\Users\pierr\OneDrive\Documents\Arduino\libraries\cocorico2\Mesocosmes_FACEIT.h"
#include "C:\Users\pierr\OneDrive\Documents\Arduino\libraries\cocorico2\Condition_FACEIT.h"
#include <Ethernet.h>
#include <WebSocketsClient.h>
#include <RTC.h>

const uint8_t CONDID = 3;

// Enter a MAC address for your controller below.
// Newer Ethernet shields have a MAC address printed on a sticker on the shield
byte mac[] = { 0xDE, 0xAD, 0xBE, 0xEF, 0xFE, CONDID };

// Set the static IP address to use if the DHCP fails to assign
//IPAddress ip(192, 168, 1, 3);
IPAddress ip(172, 16, 253, 10 + CONDID);

/***** PIN ASSIGNMENTS *****/
const byte PIN_DEBITMETRE_M0 = 57;
const byte PIN_DEBITMETRE_M1 = 58;
const byte PIN_DEBITMETRE_M2 = 59;


const byte PIN_V3V_M0 = 4;
const byte PIN_V3V_M1 = 5;
const byte PIN_V3V_M2 = 6;
const byte PIN_V2V_M0 = 8;
const byte PIN_V2V_M1 = 9;
const byte PIN_V2V_M2 = 7;
/***************************/


enum {
    REQ_PARAMS = 0,
    REQ_DATA = 1,
    SEND_PARAMS = 2,
    SEND_DATA = 3,
    CALIBRATE_SENSOR = 4,
    REQ_MASTER_DATA = 5,
    SEND_MASTER_DATA = 6,
    SEND_TIME = 7,
    REQ_CALIBRATION_PARAMS = 8
};

Condition condition;

typedef struct Calibration {
    int mesoID;
    int sensorID;
    int calibParam;
    float value;
    bool calibEnCours;
    bool calibRequested;
}Calibration;

Calibration calib;

typedef struct LoopData {
    bool sensor;
    int meso;
}LoopData;
LoopData loopData{ true, 0 };


int state = 0;



Modbus master(0, 3, 46); // this is master and RS-232 or USB-FTDI
ModbusSensor mbSensor = ModbusSensor(10, &master);

typedef struct tempo {
    unsigned long debut;
    unsigned long interval;
}tempo;

tempo tempoSensorRead;
tempo tempoRegul;
tempo tempoCheckMeso;
tempo tempoSendValues;

int sensorIndex = 0;

bool pH = true;

int salOutputLimit = 100;

WebSocketsClient webSocket;



PID pid[6];


void webSocketEvent(WStype_t type, uint8_t* payload, size_t lenght) {
    Serial.println(" WEBSOCKET EVENT:");
    Serial.println(type);
    switch (type) {
    case WStype_DISCONNECTED:
        //Serial.print(num); Serial.println(" Disconnected!");
        break;
    case WStype_CONNECTED:
        Serial.println(" Connected!");

        // send message to client
        webSocket.sendTXT("Connected");
        break;
    case WStype_TEXT:

        Serial.print(" Payload:"); Serial.println((char*)payload);
        readJSON((char*)payload);

        // send message to client
        // webSocket.sendTXT(num, "message here");

        // send data to all connected clients
        // webSocket.broadcastTXT("message here");
        break;
    case WStype_ERROR:
        Serial.println(" ERROR!");
        break;
    }
}

unsigned long dateToTimestamp(int year, int month, int day, int hour, int minute) {

    tmElements_t te;  //Time elements structure
    time_t unixTime; // a time stamp
    te.Day = day;
    te.Hour = hour;
    te.Minute = minute;
    te.Month = month;
    te.Second = 0;
    te.Year = year - 1970;
    unixTime = makeTime(te);
    return unixTime;
}

// the setup function runs once when you press reset or power the board
void setup() {
    pinMode(PIN_DEBITMETRE_M0, OUTPUT);
    pinMode(PIN_DEBITMETRE_M1, OUTPUT);
    pinMode(PIN_DEBITMETRE_M2, OUTPUT);
    pinMode(PIN_V3V_M0, OUTPUT);
    pinMode(PIN_V3V_M1, OUTPUT);
    pinMode(PIN_V3V_M2, OUTPUT);
    pinMode(PIN_V2V_M0, OUTPUT);
    pinMode(PIN_V2V_M1, OUTPUT);
    pinMode(PIN_V2V_M2, OUTPUT);

    Serial.begin(115200);
    master.begin(9600); // baud-rate at 19200
    master.setTimeOut(2000); // if there is no answer in 5000 ms, roll over

    Serial.println("START");

    condition = Condition();
    condition.startAddress = 2;

    load(2);
    condition.condID = CONDID;

    condition.Meso[0] = Mesocosme(PIN_DEBITMETRE_M0, PIN_V3V_M0, PIN_V2V_M0, 0);
    condition.Meso[1] = Mesocosme(PIN_DEBITMETRE_M1, PIN_V3V_M1, PIN_V2V_M1, 1);
    condition.Meso[2] = Mesocosme(PIN_DEBITMETRE_M2, PIN_V3V_M2, PIN_V2V_M2, 2);

    for (int j = 0; j < 3; j++) {
        condition.Meso[j] = Mesocosme(j);
        condition.Meso[j].temperature = -90 - CONDID;
        condition.Meso[j].cond = -90 - CONDID;
        condition.Meso[j].salinite = -90 - CONDID;
        condition.Meso[j].oxy = -90 - CONDID;
        condition.Meso[j].debit = -90 - CONDID;
    }

    for (int i = 0; i < 3; i++) {
        pid[i] = PID((double*)&condition.Meso[i].temperature, &condition.Meso[i].tempSortiePID, &condition.regulTemp.consigne, condition.regulTemp.Kp, condition.regulTemp.Ki, condition.regulTemp.Kd, REVERSE);
        pid[i].SetOutputLimits(50, 255);
        pid[i].SetMode(AUTOMATIC);
    }
    for (int i = 3; i < 6; i++) {
        pid[i] = PID((double*)&condition.Meso[i - 3].salinite, &condition.Meso[i - 3].salSortiePID, &condition.regulSalinite.consigne, condition.regulSalinite.Kp, condition.regulSalinite.Ki, condition.regulSalinite.Kd, REVERSE);
        pid[i].SetOutputLimits(50, salOutputLimit);
        pid[i].SetMode(AUTOMATIC);
    }


    tempoSensorRead.interval = 200;
    tempoRegul.interval = 100;
    tempoCheckMeso.interval = 200;
    tempoSendValues.interval = 5000;

    tempoSensorRead.debut = millis() + 2000;
    Serial.println("ETHER");
    Ethernet.begin(mac, ip);


    if (Ethernet.hardwareStatus() == EthernetNoHardware) {
        while (true) {
            delay(1); // do nothing, no point running without Ethernet hardware
        }
    }
    Serial.println("Ethernet connected");

    webSocket.begin("172.16.253.10", 81);

    webSocket.onEvent(webSocketEvent);

    RTC.read();
    delay(CONDID * 1000);
}

// the loop function runs over and over again until power down or reset
void loop() {
    readMBSensors();

    if (elapsed(&tempoRegul.debut, tempoRegul.interval)) {
        for (uint8_t i = 0; i < 3; i++) {
            regulationTemperature(i);
            regulationSalinite(i);
        }
    }
    checkMesocosmes();

    webSocket.loop();
    sendData();

}

void sendData() {
    if (elapsed(&tempoSendValues.debut, tempoSendValues.interval)) {
        StaticJsonDocument<jsonDocSize> doc;
        char buffer[bufferSize];
        Serial.println("SEND DATA");
        condition.serializeData(buffer, RTC.getTime(), CONDID, CONDID, false, doc);
        Serial.println(buffer);
        webSocket.sendTXT(buffer);
    }
}

void setPIDparams() {
    for (int i = 0; i < 3; i++) {
        pid[i].SetTunings(condition.regulTemp.Kp, condition.regulTemp.Ki, condition.regulTemp.Kd);
        pid[i].SetControllerDirection(REVERSE);
        pid[i].SetOutputLimits(50, 255);
        pid[i].SetMode(AUTOMATIC);
    }
    for (int i = 3; i < 6; i++) {
        pid[i].SetTunings(condition.regulSalinite.Kp, condition.regulSalinite.Ki, condition.regulSalinite.Kd);
        pid[i].SetOutputLimits(50, salOutputLimit);
        pid[i].SetControllerDirection(REVERSE);
        pid[i].SetMode(AUTOMATIC);
    }
}


bool elapsed(unsigned long* previousMillis, unsigned long interval) {
    if (*previousMillis == 0) {
        *previousMillis = millis();
    }
    else {
        if ((unsigned long)(millis() - *previousMillis) >= interval) {
            *previousMillis = 0;
            return true;
        }
    }
    return false;
}

void checkMesocosmes() {
    if (elapsed(&tempoCheckMeso.debut, tempoCheckMeso.interval)) {
        for (int i = 0; i < 3; i++) {
            condition.Meso[i].readFlow(10);
        }
    }

}



void readMBSensors() {
    if (elapsed(&tempoSensorRead.debut, tempoSensorRead.interval)) {
        if (state == 0 && calib.calibRequested) {
            calib.calibRequested = false;
            calib.calibEnCours = true;
        }
        if (calib.calibEnCours) {
            calibrateSensor();
        }
        else {
            if (loopData.sensor) {
                mbSensor.query.u8id = loopData.meso + 10;
                if (state == 0) {
                    if (mbSensor.requestValues()) {
                        state = 1;
                    }
                }
                else if (mbSensor.readValues()) {

                    Serial.print(F("READ PODOC:")); Serial.println(loopData.meso);
                    Serial.print(F("Temperature:")); Serial.println(mbSensor.params[0]);
                    Serial.print(F("oxy %:")); Serial.println(mbSensor.params[1]);
                    Serial.print(F("oxy mg/L:")); Serial.println(mbSensor.params[2]);
                    Serial.print(F("oxy ppm:")); Serial.println(mbSensor.params[3]);
                    condition.Meso[loopData.meso].oxy_temp = mbSensor.params[0];
                    condition.Meso[loopData.meso].oxy = mbSensor.params[2];
                    condition.Meso[loopData.meso].oxy_pc = mbSensor.params[1];
                    state = 0;
                    loopData.sensor = 0;
                }
            }
            else {
                mbSensor.query.u8id = loopData.meso + 30;
                if (state == 0) {
                    if (mbSensor.requestValues()) {
                        state = 1;
                    }
                }
                else if (mbSensor.readValues()) {
                    Serial.print(F("READ PC4E:")); Serial.println(loopData.meso);
                    Serial.print(F("Temperature:")); Serial.println(mbSensor.params[0]);
                    Serial.print(F("cond uS/cm:")); Serial.println(mbSensor.params[1]);
                    Serial.print(F("sali g/kg:")); Serial.println(mbSensor.params[2]);
                    condition.Meso[loopData.meso].temperature = mbSensor.params[0];
                    condition.Meso[loopData.meso].cond = mbSensor.params[1];
                    condition.Meso[loopData.meso].salinite = mbSensor.params[2];

                    state = 0;
                    loopData.sensor = 1;
                    loopData.meso++;
                    if (loopData.meso == 3) loopData.meso = 0;
                }
            }
        }
    }
}

void calibrateSensor() {
    if (calib.sensorID == 1) mbSensor.query.u8id = calib.mesoID + 10;
    else mbSensor.query.u8id = calib.mesoID + 30;

    if (calib.calibParam == 99) {
        if (state == 0) {
            Serial.println("factory reset ?");
            if (mbSensor.factoryReset()) state = 1;
        }
        else {
            calib.calibEnCours = false;
            state = 0;
            Serial.println("factory reset OKKKKK");
        }
    }
    else {
        int offset;
        Serial.print("value"); Serial.println(calib.value);
        if (state == 0) {
            Serial.println("calibrate sensor");
            switch (calib.sensorID) {
            case 0: //PC4E temperature
                Serial.println("CALIB temp");
                if (calib.calibParam == 0) {
                    Serial.println("offset");
                    offset = 512;
                }
                else {
                    Serial.println("slope");
                    offset = 514;
                }
                break;
            case 1: //PODOC oxy
                if (calib.calibParam == 0) {
                    offset = 516;
                }
                else {
                    offset = 522;
                }
                break;
            case 2: //PC4E cond
                if (calib.calibParam == 0) {
                    offset = 528;
                }
                else {
                    offset = 530;
                }
                break;
            }
            if (mbSensor.calibrateCoeff(calib.value, offset)) state = 1;
        }
        else {
            switch (calib.sensorID) {
            case 0: //PC4E temperature
                offset = 638;
                break;
            case 1: //PODOC oxy
                if (calib.calibParam == 0) {
                    offset = 654;
                }
                else {
                    offset = 654;
                }
                break;
            case 2: //PC4E cond
                offset = 654;
                break;
            }
            Serial.println("validate Calibration");
            if (mbSensor.validateCalibration(offset)) {
                state = 0;
                calib.calibEnCours = false;
                Serial.println("validate Calibration OKKKKK");
            }
        }
    }

}

int regulationTemperature(uint8_t mesoID) {
    if (condition.regulTemp.autorisationForcage) {
        if (condition.regulTemp.consigneForcage > 0 && condition.regulTemp.consigneForcage <= 100) {
            analogWrite(condition.Meso[mesoID]._pin_V3V, (int)(condition.regulTemp.consigneForcage * 255 / 100));
        }
        else {
            analogWrite(condition.Meso[mesoID]._pin_V3V, 0);
        }
    }
    else {
        //condition.load();
        pid[mesoID].Compute();
        //condition.Meso[mesoID].tempSortiePID_pc = (int)(condition.Meso[mesoID].tempSortiePID / 2.55);
        condition.Meso[mesoID].tempSortiePID_pc = (int)map(condition.Meso[mesoID].tempSortiePID, 50, 255, 0, 100);
        if (condition.Meso[mesoID].tempSortiePID_pc < 0) condition.Meso[mesoID].tempSortiePID_pc = 0;
        analogWrite(condition.Meso[mesoID]._pin_V3V, condition.Meso[mesoID].tempSortiePID);
        return condition.Meso[mesoID].tempSortiePID_pc;
    }
}

int regulationSalinite(uint8_t mesoID) {//0 = Eau ambiante, 1 = Eau Froide, 2 = Eau Chaude
    if (condition.regulSalinite.autorisationForcage) {
        if (condition.regulSalinite.consigneForcage > 0 && condition.regulSalinite.consigneForcage <= 100) {
            analogWrite(condition.Meso[mesoID]._pin_V2V, (int)(condition.regulSalinite.consigneForcage * 255 / 100));
        }
        else {
            analogWrite(condition.Meso[mesoID]._pin_V2V, 0);
        }
        return condition.regulSalinite.consigneForcage;
    }
    else {
        pid[mesoID + 3].Compute();
        //condition.Meso[mesoID].salSortiePID_pc = (int)(condition.Meso[mesoID].salSortiePID / 2.55);
        condition.Meso[mesoID].salSortiePID_pc = (int)map(condition.Meso[mesoID].salSortiePID, 50, salOutputLimit, 0, 100);
        if (condition.Meso[mesoID].salSortiePID_pc < 0) condition.Meso[mesoID].salSortiePID_pc = 0;
        analogWrite(condition.Meso[mesoID]._pin_V2V, condition.Meso[mesoID].salSortiePID);
        return condition.Meso[mesoID].salSortiePID_pc;
    }
}

void save(int address) {
    condition.save();
}

void load(int address) {
    condition.load();
}



void readJSON(char* json) {
    StaticJsonDocument<jsonDocSize> doc;
    deserializeJson(doc, json);
    char buffer[bufferSize];


    uint8_t command = doc["command"];
    uint8_t condID = doc["condID"];
    uint8_t senderID = doc["senderID"];

    uint32_t time = doc["time"];
    if (time > 0) RTC.setTime(time);
    if (condID == CONDID) {
        switch (command) {
        case REQ_PARAMS:

            condition.serializeParams(buffer, RTC.getTime(), CONDID, doc);
            webSocket.sendTXT(buffer);
            break;
        case REQ_DATA:
            condition.serializeData(buffer, RTC.getTime(), CONDID, CONDID, false, doc);
            webSocket.sendTXT(buffer);
            Serial.print("BUFFER:");
            Serial.println(buffer);
            break;
        case SEND_PARAMS:
            condition.deserializeParams(doc);
            condition.save();
            setPIDparams();
            //condition.serializeParams(buffer, RTC.getTime(), CONDID, doc);
            //webSocket.sendTXT("ok");
            break;
        case CALIBRATE_SENSOR:
            Serial.println("CALIB REQ received");
            if (condID == CONDID) {
                calib.mesoID = doc[F("mesoID")];
                calib.sensorID = doc[F("sensorID")];
                calib.calibParam = doc[F("calibParam")];
                calib.value = doc[F("value")];
                calib.calibRequested = true;
            }
            break;
        default:
            //webSocket.sendTXT(F("wrong request"));
            break;

        }
    }

}