
#include <EEPROMex.h>

#include <PID_v1.h>


class Condition {
public:
    uint8_t condID;
    Mesocosme Meso[3];
    Regul regulTemp;
    Regul regulSalinite;

    int startAddress;

    Condition() {
    }

    int save() {
        Serial.print("SAVE condID:");
        Serial.println(condID);
        int add = startAddress;
        EEPROM.updateInt(add, condID); add += sizeof(int);
        add = regulSalinite.save(add);
        add = regulTemp.save(add);
        
        return add;
    }

    int load() {
        int add = startAddress;
        condID = EEPROM.readInt(add); add += sizeof(int);
        add = regulSalinite.load(add);
        add = regulTemp.load(add);
        return add;
    }


    bool serializeData(char* buffer, uint32_t timeString, uint8_t sender, uint8_t condID, bool sendConsignes) {
        //Serial.println("SENDDATA");
        //DynamicJsonDocument doc(512);
        StaticJsonDocument<600> doc;

        doc[F("command")] = 3;
        doc[F("condID")] = condID;
        doc[F("senderID")] = sender;

        Serial.print("CondID:"); Serial.println(condID);
        

        //Serial.print(F("CONDID:")); Serial.println(condID);
        //Serial.print(F("socketID:")); Serial.println(socketID);
        doc[F("time")] = timeString;

        JsonArray data = doc.createNestedArray(F("data"));
        JsonObject dataArray[3];

        if (sendConsignes) {
            JsonObject regulS = doc.createNestedObject(F("regulSalinite"));
            JsonObject regulT = doc.createNestedObject(F("regulTemp"));
            regulT[F("consigne")] = regulTemp.consigne;
            regulS[F("consigne")] = regulSalinite.consigne;
        }

        for (int i = 0; i < 3; i++) {
            dataArray[i] = data.createNestedObject();
            dataArray[i][F("MesoID")] = Meso[i]._mesocosmeIndex;
            dataArray[i][F("temperature")] = Meso[i].temperature;
            dataArray[i][F("cond")] = Meso[i].cond;
            dataArray[i][F("salinite")] = Meso[i].salinite;
            dataArray[i][F("oxy")] = Meso[i].oxy;
            dataArray[i][F("debit")] = Meso[i].debit;
            dataArray[i][F("salSortiePID_pc")] = Meso[i].salSortiePID_pc;
            dataArray[i][F("tempSortiePID_pc")] = Meso[i].tempSortiePID_pc;
        }
        serializeJson(doc, buffer, 800);
        return true;
    }

    bool serializeParams(char* buffer, uint32_t timeString, uint8_t sender) {

        //Serial.println(F("SEND PARAMS"));
        StaticJsonDocument<600> doc;

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
        if (regulTemp.autorisationForcage) regulT[F("autorisationForcage")] = "true";
        else regulT[F("autorisationForcage")] = "false";
        regulT[F("consigneForcage")] = regulTemp.consigneForcage;
        regulT[F("offset")] = regulTemp.offset;


        JsonObject regulS = doc.createNestedObject(F("regulSalinite"));
        regulS[F("consigne")] = regulSalinite.consigne;
        regulS[F("Kp")] = regulSalinite.Kp;
        regulS[F("Ki")] = regulSalinite.Ki;
        regulS[F("Kd")] = regulSalinite.Kd;
        if (regulSalinite.autorisationForcage) regulS[F("autorisationForcage")] = "true";
        else regulS[F("autorisationForcage")] = "false";
        regulS[F("consigneForcage")] = regulSalinite.consigneForcage;
        regulS[F("offset")] = regulSalinite.offset;


        serializeJson(doc, buffer, 800);
    }

    void deserializeParams(StaticJsonDocument<600> doc) {

        int condID = doc[F("condID")]; // 0, 2, 3

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

        JsonObject regulS = doc[F("regulSalinite")];

        regulSalinite.consigne = regulS[F("consigne")]; // 24.2
        regulSalinite.Kp = regulS[F("Kp")]; // 2.1
        regulSalinite.Ki = regulS[F("Ki")]; // 2.1
        regulSalinite.Kd = regulS[F("Kd")]; // 2.1
        const char* regulS_autorisationForcage = regulS[F("autorisationForcage")];
        if (strcmp(regulS_autorisationForcage, "true") == 0 || strcmp(regulS_autorisationForcage, "True") == 0) regulSalinite.autorisationForcage = true;
        else regulSalinite.autorisationForcage = false;
        regulSalinite.consigneForcage = regulS[F("consigneForcage")]; // 2.1
        regulSalinite.offset = regulS[F("offset")];

    }

    void deserializeData(StaticJsonDocument<600> doc) {
        /*JsonObject regulS = doc[F("regulSalinite")];
        JsonObject regulT = doc[F("regulTemp")];
        regulSalinite.consigne = regulS[F("consigne")]; // 1
        regulTemp.consigne = regulT[F("consigne")]; // 1
        */
        for (JsonObject elem : doc[F("data")].as<JsonArray>()) {
            int MesoID = elem[F("MesoID")]; // 0, 2, 3
            Meso[MesoID].temperature = elem[F("temperature")]; // 0, 0, 0
            Meso[MesoID].cond = elem[F("cond")]; // 0, 0, 0
            Meso[MesoID].salinite = elem[F("salinite")]; // 0, 0, 0
            Meso[MesoID].oxy = elem[F("oxy")]; // 0, 0, 0
            Meso[MesoID].tempSortiePID_pc = elem[F("tempSortiePID_pc")]; // 2
            Meso[MesoID].salSortiePID_pc = elem[F("salSortiePID_pc")]; // 2
        }
    }
}; 
