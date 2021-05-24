/**
* MASTER FACE IT
 */
#define HAVE_RTC
#define HAVE_RTC_DS1307

#include <TimeLib.h>
#include <WebSockets.h>
#include <ModbusRtu.h>
#include <PID_v1.h>
#include <EEPROMex.h>
#include <ArduinoJson.h>
#include "C:\Users\pierr\OneDrive\Documents\Arduino\libraries\cocorico2\Mesocosmes_FACEIT.h"
#include "C:\Users\pierr\OneDrive\Documents\Arduino\libraries\cocorico2\Condition_FACEIT.h"
#include <WebSocketsServer.h>
#include <C:\Users\pierr\OneDrive\Documents\Arduino\libraries\Mduino\Ethernet\src\Ethernet.h>

#include <SD.h>

#include <RTC.h>

//#include <IndustrialShields.h>
//#include <RS485.h>

//Pinout for mduino 42

const byte PIN_DEBITMETRE_M0 = 57;
const byte PIN_DEBITMETRE_M1 = 58;
const byte PIN_DEBITMETRE_M2 = 59;
const byte PIN_DEBITMETRE_EA = 54;
const byte PIN_DEBITMETRE_EF = 55;
const byte PIN_DEBITMETRE_EC = 56;

const byte PIN_PRESSION_EA = 60;
const byte PIN_PRESSION_EF = 61;
const byte PIN_PRESSION_EC = 62;


const byte PIN_V3V_M0 = 4;
const byte PIN_V3V_M1 = 5;
const byte PIN_V3V_M2 = 6;
const byte PIN_V2V_M0 = 8;
const byte PIN_V2V_M1 = 9;
const byte PIN_V2V_M2 = 7;

// Enter a MAC address for your controller below.
// Newer Ethernet shields have a MAC address printed on a sticker on the shield
byte mac[] = { 0xDE, 0xAD, 0xBE, 0xEF, 0xFE, 0xEA };
// Set the static IP address to use if the DHCP fails to assign
IPAddress ip(192, 168, 1, 1);


WebSocketsServer webSocket = WebSocketsServer(81);


/**
 *  Modbus object declaration
 *  u8id : node id = 0 for master, = 1..247 for slave
 *  u8serno : serial port (use 0 for Serial)
 *  u8txenpin : 0 for RS-232 and USB-FTDI
 *               or any pin number > 1 for RS-485
 */
//Modbus master(0, 2, 45); // CONTROLLINO
Modbus master(0, 3, 46); // MDUINO


typedef struct tempo {
    unsigned long debut;
    unsigned long interval;
}tempo;

tempo tempoSensorRead;
tempo tempoRegul;
tempo tempoCheckMeso;
tempo tempoSendValues;
tempo tempoSendParams;
tempo tempowriteSD;
tempo tempoUpdateSetPoints;
tempo tempoCO2ValvePWM_on;
tempo tempoCO2ValvePWM_off;

typedef struct LoopData {
    bool sensor;
    int meso;
}LoopData;
LoopData loopData{ false, 0 };

typedef struct SetPointsData {
    double cond;
    double temperature;
}SetPointsData;
SetPointsData setPointsData{ 0.55, 10.33 };

bool calib = false;
bool pH = true;

bool toggleCO2Valve = false;

char buffer[800];
uint8_t AppSocketId = -1;


enum {
    REQ_PARAMS = 0,
    REQ_DATA = 1,
    SEND_PARAMS = 2,
    SEND_DATA = 3,
    CALIBRATE_SENSOR = 4,
    REQ_MASTER_DATA = 5,
    SEND_MASTER_DATA = 6,
    SEND_TIME =7
};

Condition condition[4];
PID pid[6];

typedef struct MasterData {
    double debitEA;
    double debitEF;
    double debitEC;
    double pressionEA;
    double pressionEF;
    double pressionEC;
}MasterData;

MasterData masterData;

void save(int startAddress) {
    int address = startAddress;
    for (int i = 0; i < 4; i++) {
        address = condition[i].save();
    }
}

void load(int startAddress) {
    int address = startAddress;
    for (int i = 0; i < 4; i++) {
        condition[i].startAddress = address;
        address = condition[i].load();
    }
}

int currentDay;

ModbusSensor mbSensor = ModbusSensor(10, &master);

void setup() {

    pinMode(PIN_DEBITMETRE_M0, OUTPUT);
    pinMode(PIN_DEBITMETRE_M1, OUTPUT);
    pinMode(PIN_DEBITMETRE_M2, OUTPUT);
    pinMode(PIN_DEBITMETRE_EA, OUTPUT);
    pinMode(PIN_DEBITMETRE_EF, OUTPUT);
    pinMode(PIN_DEBITMETRE_EC, OUTPUT);
    pinMode(PIN_V3V_M0, OUTPUT);
    pinMode(PIN_V3V_M1, OUTPUT);
    pinMode(PIN_V3V_M2, OUTPUT);
    pinMode(PIN_V2V_M0, OUTPUT);
    pinMode(PIN_V2V_M1, OUTPUT);
    pinMode(PIN_V2V_M2, OUTPUT);
    

    //Controllino_RTC_init();
    //Controllino_SetTimeDateStrings(__DATE__, __TIME__); /* set compilation time to the RTC chip */

    Serial.begin(115200);
    master.begin(9600); // baud-rate at 19200
    master.setTimeOut(2000); // if there is no answer in 5000 ms, roll over

    /*
    Condition 0 = Ambiant temperature
    */


    condition[0].Meso[0] = Mesocosme(PIN_DEBITMETRE_M0,PIN_V3V_M0,PIN_V2V_M0,0);
    condition[0].Meso[1] = Mesocosme(PIN_DEBITMETRE_M1, PIN_V3V_M1, PIN_V2V_M1,1);
    condition[0].Meso[2] = Mesocosme(PIN_DEBITMETRE_M2, PIN_V3V_M2, PIN_V2V_M2, 2);


    for (int i = 0; i < 4; i++) {
        condition[i].condID = i;
        for (int j = 0; j < 3; j++) {
            condition[i].Meso[j] = Mesocosme(j);
            condition[i].Meso[j].temperature = -99;
            condition[i].Meso[j].cond = -99;
            condition[i].Meso[j].salinite = -99;
            condition[i].Meso[j].oxy = -99;
            condition[i].Meso[j].debit = -99;
        }
    }

    

    load(2);

    tempoSensorRead.interval = 200;
    tempoRegul.interval = 100;
    tempoCheckMeso.interval = 200;
    tempoSendValues.interval = 5000;
    tempowriteSD.interval = 5000;
    tempoUpdateSetPoints.interval = 300*1000; //5 minutes
    tempoSendParams.interval = 5000;

    tempoSensorRead.debut = millis() + 2000;

    
    Ethernet.begin(mac, ip);

    Serial.println(F("START"));
    int i = 0;
    while (i<20) {
        if (Ethernet.hardwareStatus() != EthernetNoHardware) {
            Serial.println(F("Ethernet STARTED"));
            break;
        }
        delay(500); // do nothing, no point running without Ethernet hardware
        i++;
    }

    Serial.println("websocket begin");
    webSocket.begin();
    webSocket.onEvent(webSocketEvent);

    for (int i = 0; i < 3; i++) {
        pid[i] = PID((double*)&condition[0].Meso[i].temperature, &condition[0].Meso[i].tempSortiePID, &condition[0].regulTemp.consigne, condition[0].regulTemp.Kp, condition[0].regulTemp.Ki, condition[0].regulTemp.Kd, DIRECT);
        pid[i].SetOutputLimits(0, 255);
        pid[i].SetMode(AUTOMATIC);
    }

    pid[3] = PID((double*)&masterData.pressionEA, &condition[0].Meso[0].salSortiePID, &condition[0].regulSalinite.consigne, condition[0].regulSalinite.Kp, condition[0].regulSalinite.Ki, condition[0].regulSalinite.Kd, DIRECT);
    pid[4] = PID((double*)&masterData.pressionEF, &condition[0].Meso[1].salSortiePID, &condition[0].regulSalinite.consigne, condition[0].regulSalinite.Kp, condition[0].regulSalinite.Ki, condition[0].regulSalinite.Kd, DIRECT);
    pid[5] = PID((double*)&masterData.pressionEC, &condition[0].Meso[2].salSortiePID, &condition[0].regulSalinite.consigne, condition[0].regulSalinite.Kp, condition[0].regulSalinite.Ki, condition[0].regulSalinite.Kd, DIRECT);
    pid[3].SetOutputLimits(0, 255);
    pid[3].SetMode(AUTOMATIC);
    pid[4].SetOutputLimits(0, 255);
    pid[4].SetMode(AUTOMATIC);
    pid[5].SetOutputLimits(0, 255);
    pid[5].SetMode(AUTOMATIC);

    

    /*RTC.setYear(YEAR);                      //sets year
    RTC.setMonth(MONTH);                   //sets month
    RTC.setMonthDay(DAY);                   //sets day
    RTC.setHour(HOUR);                      //sets hour
    RTC.setMinute(MINUTE);                  //sets minute
    RTC.setSecond(SECOND);                  //sets second

    RTC.write();*/
    

    Serial.println(F("RTC READ"));
    RTC.read();
    currentDay = RTC.getMonthDay();

    Serial.println(F("setPID params"));
    setPIDparams();

    delay(1000);
    Serial.println(F("SD BEGIN"));
    SD.begin(53);
    Serial.println(F("end setup"));
}

void loop() {
    RTC.read();
    //if(!calib) requestSensors();
    //else calibration();
    //requestAllStatus();
    readMBSensors();
    webSocket.loop();

    if (elapsed(&tempoRegul.debut, tempoRegul.interval)) {
        for (uint8_t i = 0; i < 3; i++) {
            regulationTemperature(i);
            regulationPression(i);
        }
    }
    checkMesocosmes();


    printToSD();
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


unsigned long dateToTimestamp(int year, int month, int day, int hour, int minute) {

    tmElements_t te;  //Time elements structure
    time_t unixTime; // a time stamp
    te.Day = day;
    te.Hour = hour;
    te.Minute = minute;
    te.Month = month;
    te.Second = 0;
    te.Year = year-1970;
    unixTime = makeTime(te);
    return unixTime;
}
void checkMesocosmes() {
    if (elapsed(&tempoCheckMeso.debut, tempoCheckMeso.interval)) {
        for (int i = 0; i < 3; i++) {
            condition[0].Meso[i].readFlow(10);
        }
        masterData.debitEA = readFlow(10, PIN_DEBITMETRE_EA, masterData.debitEA);
        masterData.debitEC = readFlow(10, PIN_DEBITMETRE_EC, masterData.debitEC);
        masterData.debitEF = readFlow(10, PIN_DEBITMETRE_EF, masterData.debitEF);
        masterData.pressionEA = readPressure(10, PIN_PRESSION_EA, masterData.pressionEA);
        masterData.pressionEF = readPressure(10, PIN_PRESSION_EF, masterData.pressionEF);
        masterData.pressionEC = readPressure(10, PIN_PRESSION_EC, masterData.pressionEC);
    }
}

float readFlow(int lissage, uint8_t pinDebitmetre, double debit) {

    int ana = analogRead(pinDebitmetre); // 0-1023 value corresponding to 0-5 V corresponding to 0-20 mA
    int mA = map(ana, 0, 1023, 0, 2000); //map to milli amps with 2 extra digits
    double ancientDebit = debit;
    debit = (0.625 * (mA - 400)) / 100.0; // flowrate in l/mn
    debit = (lissage * debit + (100.0 - lissage) * ancientDebit) / 100.0;
    return debit;
}

float readPressure(int lissage, uint8_t pin, double pression) {

    int ana = analogRead(pin); // 0-1023 value corresponding to 0-5 V corresponding to 0-20 mA
    int mA = map(ana, 0, 1023, 0, 2000); //map to milli amps with 2 extra digits
    int mbars = map(mA, 400, 2000, 0, 4000); //map to milli amps with 2 extra digits
    double anciennePression = pression;
    pression = ((double) mbars)/1000.0; // pressure in bars
    pression = (lissage * pression + (100.0 - lissage) * anciennePression) / 100.0;
    return pression;
}



void printToSD() {

    if (elapsed(&tempowriteSD.debut, tempowriteSD.interval)) {
        // open the file. note that only one file can be open at a time,
  // so you have to close this one before opening another.
        String path =  String("data/"+RTC.getMonth()) + "_" + String(RTC.getYear()) + ".csv";
        //Serial.println(path);
        
        if (!SD.exists(path)) {
            File dataFile = SD.open(path, FILE_WRITE);
            //Serial.println(F("file does not exist. Writing headers"));

            if (dataFile) {
                dataFile.print(F("timestamp;Ambiant.Conductivity;Ambiant.Temperature;"));
                for (int i = 0; i < 4; i++) {
                    String header = F("Condition["); header += i; header += F("].ConsigneTemperature;");
                    if (i == 0) { header += F("Condition["); header += i; header += F("].ConsignePression;"); }
                    else { header += F("Condition["); header += i; header += F("].ConsigneSalinite;"); }
                    dataFile.print(header);
                    for (int j = 0; j < 3; j++) {
                        header = F("Condition["); header += i; header += F("].Meso["); header += j; header += F("].Temperature;"); dataFile.print(header);
                        header = F("Condition["); header += i; header += F("].Meso["); header += j; header += F("].Cond;"); dataFile.print(header);
                        header = F("Condition["); header += i; header += F("].Meso["); header += j; header += F("].Oxy;"); dataFile.print(header);
                        header = F("Condition["); header += i; header += F("].Meso["); header += j; header += F("].FlowRate;"); dataFile.print(header);
                        header = F("Condition["); header += i; header += F("].Meso["); header += j; header += F("].RegulTemp.sortiePID;"); dataFile.print(header);
                        if (i == 0) {
                            header = F("Condition["); header += i; header += F("].Meso["); header += j; header += F("].RegulPression.sortiePID;"); dataFile.print(header);
                        }
                        else {
                            header = F("Condition["); header += i; header += F("].Meso["); header += j; header += F("].RegulSalinite.sortiePID;"); dataFile.print(header);
                        }
                    }
                }
                dataFile.println();
                dataFile.close();
                // print to the serial port too:
            }
            else {
                //Serial.println(F("Error opening file"));
            }
        }
        File dataFile = SD.open(path, FILE_WRITE);
        
        
        // if the file is available, write to it:
        if (dataFile) {
            char sep = ';';
            dataFile.print(RTC.getTime()); dataFile.print(sep); dataFile.print(setPointsData.cond); dataFile.print(sep); dataFile.print(setPointsData.temperature); dataFile.print(sep); 
            
            for (int i = 0; i < 4; i++) {
                dataFile.print(condition[i].regulTemp.consigne); dataFile.print(sep);
                dataFile.print(condition[i].regulSalinite.consigne); dataFile.print(sep);
                for (int j = 0; j < 3; j++) {
                    dataFile.print(condition[i].Meso[j].temperature); dataFile.print(sep);
                    dataFile.print(condition[i].Meso[j].cond); dataFile.print(sep);
                    dataFile.print(condition[i].Meso[j].oxy); dataFile.print(sep);
                    dataFile.print(condition[i].Meso[j].debit); dataFile.print(sep);
                    //dataFile.print(condition[i].Meso[j].regulTemp.sortiePID_pc); dataFile.print(sep);
                    //dataFile.print(condition[i].Meso[j].regulSalinite.sortiePID_pc); dataFile.print(sep);
                }
            }
            dataFile.println("");
            dataFile.close();
            // print to the serial port too:
        }
        // if the file isn't open, pop up an error:
        else {
            Serial.println(F("error opening file"));
        }
    }
    
}


void setPIDparams() {

    for (int i = 0; i < 3; i++) {
        pid[i].SetTunings(condition[0].regulTemp.Kp, condition[0].regulTemp.Ki, condition[0].regulTemp.Kd);
        pid[i].SetOutputLimits(0, 255);
        pid[i].SetMode(AUTOMATIC);

        
    }
    for (int i = 3; i < 6; i++) {
        pid[i].SetTunings(condition[0].regulSalinite.Kp, condition[0].regulSalinite.Ki, condition[0].regulSalinite.Kd);
        pid[i].SetOutputLimits(0, 255);
        pid[i].SetMode(AUTOMATIC);


    }


}

int regulationTemperature(uint8_t mesoID) {
    if (condition[0].regulTemp.autorisationForcage) {
        if (condition[0].regulTemp.consigneForcage > 0 && condition[0].regulTemp.consigneForcage <= 100) {
            analogWrite(condition[0].Meso[mesoID]._pin_V3V, (int)(condition[0].regulTemp.consigneForcage * 255 / 100));
        }
        else {
            analogWrite(condition[0].Meso[mesoID]._pin_V3V, 0);
        }
    }
    else {
        
            //condition.load();
        pid[mesoID].Compute();
            condition[0].Meso[mesoID].tempSortiePID_pc = (int)(condition[0].Meso[mesoID].tempSortiePID / 2.55);
            analogWrite(condition[0].Meso[mesoID]._pin_V3V, condition[0].Meso[mesoID].tempSortiePID);
            return condition[0].Meso[mesoID].tempSortiePID_pc;
    }
}

int regulationPression(uint8_t mesoID) {//0 = Eau ambiante, 1 = Eau Froide, 2 = Eau Chaude
    if (condition[0].regulSalinite.autorisationForcage) {
        if (condition[0].regulSalinite.consigneForcage > 0 && condition[0].regulSalinite.consigneForcage <= 100) {
            analogWrite(condition[0].Meso[mesoID]._pin_V2V, (int)(condition[0].regulSalinite.consigneForcage * 255 / 100));
        }
        else {
            analogWrite(condition[0].Meso[mesoID]._pin_V2V, 0);
        }
        return condition[0].regulSalinite.consigneForcage;
    }
    else {
        //condition.load();
        pid[mesoID+3].Compute();
        condition[0].Meso[mesoID].salSortiePID_pc = (int)(condition[0].Meso[mesoID].salSortiePID / 2.55);
        analogWrite(condition[0].Meso[mesoID]._pin_V2V, condition[0].Meso[mesoID].salSortiePID);
        return condition[0].Meso[mesoID].salSortiePID_pc;
    }
    
}

//ONLY FOR SLAVES

/*int regulationSalinite(uint8_t mesoID) {
    if (meso[mesoID].regulSalinite->autorisationForcage) {
        if (meso[mesoID].regulSalinite->consigneForcage > 0 && meso[mesoID].regulSalinite->consigneForcage <= 100) {
            analogWrite(meso[mesoID]._pin_V2V, (int)(meso[mesoID].regulSalinite->consigneForcage * 255 / 100));
        }
        else {
            analogWrite(meso[mesoID]._pin_V2V, 0);
        }
    }
    else {
        //condition.load();
        meso[mesoID].regulSalinite->pid.Compute();
        meso[mesoID].regulSalinite->sortiePID_pc = (int)(meso[mesoID].regulSalinite->sortiePID / 2.55);
        analogWrite(meso[mesoID]._pin_V2V, meso[mesoID].regulSalinite->sortiePID);
        return meso[mesoID].regulSalinite->sortiePID_pc;
    }
}*/

void webSocketEvent(uint8_t num, WStype_t type, uint8_t* payload, size_t lenght) {

    switch (type) {
    case WStype_DISCONNECTED:
        //Serial.print(num); Serial.println(" Disconnected!");
        break;
    case WStype_CONNECTED:
        Serial.print(num); Serial.println(F(" Connected!"));

        // send message to client
        webSocket.sendTXT(num, F("Connected"));
        break;
    case WStype_TEXT:

        //Serial.print(num); Serial.print(F(" Payload:")); Serial.println((char*)payload);
        Serial.print("Payload received from "); Serial.println(num); Serial.println(": "); Serial.println((char*)payload);
        readJSON((char*)payload, num);


        // send message to client
        // webSocket.sendTXT(num, "message here");

        // send data to all connected clients
        // webSocket.broadcastTXT("message here");
        break;
    case WStype_ERROR:
        Serial.print(num); Serial.println(F(" ERROR!"));
        break;
    }
}

void readJSON(char* json, uint8_t num) {
    StaticJsonDocument<600> doc;
    deserializeJson(doc, json);

    uint8_t command = doc["command"];
    uint8_t condID = doc["condID"];
    uint8_t senderID = doc["senderID"];


    if (senderID == 4) {
        uint32_t time = doc["time"];
        if (time > 0) {
            RTC.setTime(time);
            RTC.write();
        }
    }
        
    Serial.print("CondID:"); Serial.println(condID);
    //if (senderID == 4) {//La trame vient du PC de supervision
        //AppSocketId = num;
        switch (command) {
        case REQ_PARAMS:
            condition[condID].load();
            
            condition[condID].serializeParams(buffer, RTC.getTime(),0);
            Serial.print("SEND PARAMS:"); Serial.println(buffer);
            webSocket.sendTXT(num, buffer);
            break;
        case REQ_DATA:
            
            if(senderID ==4) condition[condID].serializeData(buffer, RTC.getTime(), 0, condID, true);
            else condition[condID].serializeData(buffer, RTC.getTime(), 0, condID, false);
            Serial.print("SEND DATA:"); Serial.println(buffer);
            webSocket.sendTXT(num, buffer);
            break;
        case SEND_PARAMS:
            condition[condID].load();
            condition[condID].deserializeParams(doc);
            
            condition[condID].save();
            
            if (senderID == 4) {
                condition[condID].serializeParams(buffer, RTC.getTime(), 0);
                webSocket.sendTXT(num, buffer); 
            }
            if(condID == 0) setPIDparams();
            break;
        case SEND_DATA:
            condition[condID].load();
            condition[condID].deserializeData(doc);
            condition[condID].serializeParams(buffer, RTC.getTime(), 0);
            Serial.print("SEND PARAMS!!:"); Serial.println(buffer);
            webSocket.sendTXT(num, buffer);
            break;
        case REQ_MASTER_DATA:
            SerializeMasterData(buffer, RTC.getTime());
            webSocket.sendTXT(num, buffer);
            break;

        

            /*case CALIBRATE_SENSOR:
                readCalibRequest(doc);
                break;*/
        default:
            webSocket.sendTXT(num, F("wrong request1"));
            break;
        }
}





float checkValue(float val, float min, float max, float def) {
    if (val > min && val <= max) return val;
    return def;
}


/*void requestAllStatus() {
    if (elapsed(&tempoSensorRead.debut, tempoSensorRead.interval)) {
        switch (state) {
        case 0:
            Serial.println("PODOC:");
            if (Podoc.requestStatus()) {
                state++;
            }
            break;
        case 1:
            Serial.println("NTU:");
            if (NTU.requestStatus()) {
                state++;
            }
            break;
        case 2:
            Serial.println("PC4E:");
            if (PC4E.requestStatus()) {
                state = 0;
            }
            break;
        }
    }
}*/


bool SerializeMasterData(char* buffer, uint32_t timeString) {
    //Serial.println(F("SENDDATA"));
    StaticJsonDocument<512> doc;

    doc[F("command")] = SEND_MASTER_DATA;
    doc[F("condID")] = 0;
    doc[F("senderID")] = 0;
    doc[F("time")] = timeString;

    doc[F("debitEA")] = masterData.debitEA;
    doc[F("debitEF")] = masterData.debitEF;
    doc[F("debitEC")] = masterData.debitEC;

    doc[F("pressionEA")] = masterData.pressionEA;
    doc[F("pressionEF")] = masterData.pressionEF;
    doc[F("pressionEC")] = masterData.pressionEC;

    serializeJson(doc, buffer, 600);
    return true;
}


int state = 0;

void readMBSensors() {
    if (elapsed(&tempoSensorRead.debut, tempoSensorRead.interval)) {
        if (loopData.sensor) {
            mbSensor.query.u8id = loopData.meso + 10;
            if (state == 0) {
                if (mbSensor.requestValues()) {
                    state = 1;
                }
            }
            else if (mbSensor.readValues()) {
                
                /*Serial.print(F("READ PODOC:")); Serial.println(loopData.meso);
                Serial.print(F("Temperature:")); Serial.println(mbSensor.params[0]);
                Serial.print(F("oxy %:")); Serial.println(mbSensor.params[1]);
                Serial.print(F("oxy mg/L:")); Serial.println(mbSensor.params[2]);
                Serial.print(F("oxy ppm:")); Serial.println(mbSensor.params[3]);*/
                condition[0].Meso[loopData.meso].oxy = mbSensor.params[2];
                state = 0;
                loopData.sensor = 0;
            }
        }else {
            mbSensor.query.u8id = loopData.meso + 30;
            if (state == 0) {
                if (mbSensor.requestValues()) {
                    state = 1;
                }
            }
            else if (mbSensor.readValues()) {
                /*Serial.print(F("READ PC4E:")); Serial.println(loopData.meso);
                Serial.print(F("Temperature:")); Serial.println(mbSensor.params[0]);
                Serial.print(F("cond uS/cm:")); Serial.println(mbSensor.params[1]);
                Serial.print(F("sali g/kg:")); Serial.println(mbSensor.params[2]);*/
                condition[0].Meso[loopData.meso].temperature = mbSensor.params[0];
                condition[0].Meso[loopData.meso].cond = mbSensor.params[1];
                condition[0].Meso[loopData.meso].salinite = mbSensor.params[2];
                
                state = 0;
                loopData.sensor = 1;
                loopData.meso++;
                if (loopData.meso == 3) loopData.meso = 0;
            }
        }
    }
}

/*void calibration() {

    if (elapsed(&debutReq, 500)) {
        switch (stateCalibration) {
        case 0:
            if (Podoc.calibrateCoeff(17.0, 514)) {
                Serial.println("calibrate temp");
                stateCalibration++;
            }
            break;
        case 1:
            if (Podoc.validateCalibration(638)) {
                Serial.println("validate calibration temp");
                stateCalibration++;
            }

            break;
        case 2:
            step = Hamilton.calibrate(5.0, step);
            if (step == 3) {
                state = 0;
                calib = false;
                stateCalibration = 0;
            }
            break;
        }
    }
}*/



