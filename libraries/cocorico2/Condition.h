#ifndef MESO
#include "C:\Users\pierr\OneDrive\Documents\Arduino\libraries\cocorico2\Mesocosmes.h"
#endif // !MESO
#include <EEPROMex.h>

#include <PID_v1.h>

class Regul {
public:
    
    double sortiePID;
    double consigne;
    double Kp;
    double Ki;
    double Kd;
    double sortiePID_pc;
    bool autorisationForcage;
    int consigneForcage;
    double offset;
    PID pid;

    int save(int startAddress) {
        int add = startAddress;
        EEPROM.updateDouble(add, consigne); add += sizeof(double);
        EEPROM.updateDouble(add, Kp); add += sizeof(double);
        EEPROM.updateDouble(add, Ki); add += sizeof(double);
        EEPROM.updateDouble(add, Kd); add += sizeof(double);
        EEPROM.updateDouble(add, offset); add += sizeof(double);

        EEPROM.updateInt(add, autorisationForcage); add += sizeof(int);
        EEPROM.updateInt(add, consigneForcage); add += sizeof(int);
        return add;
    }

    int load(int startAddress) {
        int add = startAddress;
        consigne = EEPROM.readDouble(add); add += sizeof(double);
        Kp = EEPROM.readDouble(add); add += sizeof(double);
        Ki = EEPROM.readDouble(add); add += sizeof(double);
        Kd = EEPROM.readDouble(add); add += sizeof(double);
        offset = EEPROM.readDouble(add); add += sizeof(double);

        autorisationForcage = EEPROM.readInt(add); add += sizeof(int);
        consigneForcage = EEPROM.readInt(add); add += sizeof(int);
        return add;
    }
};


class Condition {
public:
    uint8_t socketID;
    uint8_t condID;
    Mesocosme Meso[3];
    double mesureTemperature;
    double mesurepH;

    Regul regulTemp, regulpH;

    int startAddress;

    Condition() {
        regulTemp = Regul();
        regulpH = Regul();
    }

    int save() {
        Serial.print("SAVE condID:");
        Serial.println(condID);
        int add = startAddress;
        EEPROM.updateInt(add, condID); add += sizeof(int);
        add = regulpH.save(add);
        add = regulTemp.save(add);
        return add;
    }

    int load() {
        int add = startAddress;
        condID = EEPROM.readInt(add); add += sizeof(int);
        add = regulpH.load(add);
        add = regulTemp.load(add);
        return add;
    }


    bool serializeData(char* buffer, uint32_t timeString, uint8_t sender) {
        //Serial.println("SENDDATA");
        //DynamicJsonDocument doc(512);
        StaticJsonDocument<512> doc;

        doc[F("command")] = 3;
        doc[F("condID")] = condID;
        doc[F("senderID")] = sender;
        doc[F("temperature")] = mesureTemperature;
        doc[F("pH")] = mesurepH;
        

        //Serial.print(F("CONDID:")); Serial.println(condID);
        //Serial.print(F("socketID:")); Serial.println(socketID);
        doc[F("time")] = timeString;

        JsonArray data = doc.createNestedArray(F("data"));
        JsonObject dataArray[3];

        JsonObject regulT= doc.createNestedObject(F("regulTemp"));
        regulT[F("consigne")] = regulTemp.consigne;
        regulT[F("sortiePID_pc")] = regulTemp.sortiePID_pc;

        JsonObject regulp = doc.createNestedObject(F("regulpH"));
        regulp[F("consigne")] = regulpH.consigne;
        regulp[F("sortiePID_pc")] = regulpH.sortiePID_pc;

        for (int i = 0; i < 3; i++) {
            dataArray[i] = data.createNestedObject();
            dataArray[i][F("MesoID")] = Meso[i]._mesocosmeIndex;
            dataArray[i][F("temperature")] = Meso[i].temperature;
            dataArray[i][F("pH")] = Meso[i].pH;

            dataArray[i][F("debit")] = Meso[i].debit;
            dataArray[i][F("LevelH")] = Meso[i].alarmeNiveauHaut;
            dataArray[i][F("LevelL")] = !Meso[i].alarmeNiveauBas;
            dataArray[i][F("LevelLL")] = !Meso[i].alarmeNiveauTresBas;
        }
        serializeJson(doc, buffer, 600);
        return true;
    }

    bool serializeParams(char* buffer, uint32_t timeString, uint8_t sender) {

        //Serial.println(F("SEND PARAMS"));
        StaticJsonDocument<512> doc;

        doc[F("command")] = 2;
        doc[F("condID")] = condID;
        doc[F("senderID")] = sender;
        doc[F("time")] = timeString;
        /*doc["mesureTemp"] = Hamilton[3].temp_sensorValue;
        doc["mesurepH"] = Hamilton[3].pH_sensorValue;*/

        JsonObject regulT = doc.createNestedObject(F("regulTemp"));
        regulT[F("consigne")] = regulTemp.consigne;
        regulT[F("Kp")] = regulTemp.Kp;
        regulT[F("Ki")] = regulTemp.Ki;
        regulT[F("Kd")] = regulTemp.Kd;
        if (this->regulTemp.autorisationForcage) regulT[F("autorisationForcage")] = "true";
        else regulT[F("autorisationForcage")] = "false";
        regulT[F("consigneForcage")] = regulTemp.consigneForcage;
        regulT[F("offset")] = regulTemp.offset;

        JsonObject regulp = doc.createNestedObject(F("regulpH"));
        regulp[F("consigne")] = regulpH.consigne;
        regulp[F("Kp")] = regulpH.Kp;
        regulp[F("Ki")] = regulpH.Ki;
        regulp[F("Kd")] = regulpH.Kd;
        if (regulpH.autorisationForcage) regulp[F("autorisationForcage")] = "true";
        else regulp[F("autorisationForcage")] = "false";
        regulp[F("consigneForcage")] = regulpH.consigneForcage;
        regulp[F("offset")] = regulpH.offset;
        serializeJson(doc, buffer, 600);
    }

    void deserializeParams(StaticJsonDocument<512> doc) {
        
        JsonObject regulp = doc[F("regulpH")];
        regulpH.consigne = regulp[F("consigne")]; // 24.2
        regulpH.Kp = regulp[F("Kp")]; // 2.1
        regulpH.Ki = regulp[F("Ki")]; // 2.1
        regulpH.Kd = regulp[F("Kd")]; // 2.1
        const char* regulpH_autorisationForcage = regulp[F("autorisationForcage")];
        if (strcmp(regulpH_autorisationForcage, "true") == 0 || strcmp(regulpH_autorisationForcage, "True") == 0) regulpH.autorisationForcage = true;
        else regulpH.autorisationForcage = false;
        regulpH.consigneForcage = regulp[F("consigneForcage")]; // 2.1
        regulpH.offset = regulp[F("offset")];

        JsonObject regulT = doc[F("regulTemp")];

        regulTemp.consigne = regulT[F("consigne")]; // 24.2
        regulTemp.Kp = regulT[F("Kp")]; // 2.1
        regulTemp.Ki = regulT[F("Ki")]; // 2.1
        regulTemp.Kd = regulT[F("Kd")]; // 2.1
        const char* regulTemp_autorisationForcage = regulT[F("autorisationForcage")];
        if (strcmp(regulTemp_autorisationForcage, "true") == 0 || strcmp(regulTemp_autorisationForcage, "True") == 0) regulTemp.autorisationForcage = true;
        else regulTemp.autorisationForcage = false;
        regulTemp.consigneForcage = regulT[F("consigneForcage")]; // 2.1
        regulTemp.offset = regulT[F("offset")];
        
    }

    void deserializeData(StaticJsonDocument<512> doc) {

        for (JsonObject elem : doc[F("data")].as<JsonArray>()) {

            int MesoID = elem[F("MesoID")]; // 0, 2, 3
            Meso[MesoID] = MesoID;
            Meso[MesoID].temperature = elem[F("temperature")]; // 0, 0, 0
            Meso[MesoID].pH = elem[F("pH")]; // 0, 0, 0
            Meso[MesoID].debit = elem[F("debit")]; // 0, 0, 0
            Meso[MesoID].alarmeNiveauHaut = elem[F("LevelH")]; // 0, 0, 0
            Meso[MesoID].alarmeNiveauBas = elem[F("LevelL")]; // 0, 0, 0
            Meso[MesoID].alarmeNiveauTresBas = elem[F("LevelLL")]; // 0, 0, 0
        }
        regulTemp.consigne = doc[F("regulTemp")][F("consigne")]; // 1
        regulTemp.sortiePID_pc = doc[F("regulTemp")][F("sortiePID_pc")]; // 2

        regulpH.consigne = doc[F("regulpH")][F("consigne")]; // 3
        regulpH.sortiePID_pc = doc[F("regulpH")][F("sortiePID_pc")]; // 4
        
        mesureTemperature = doc[F("temperature")];
        mesurepH = doc[F("pH")];

    }


}; 
