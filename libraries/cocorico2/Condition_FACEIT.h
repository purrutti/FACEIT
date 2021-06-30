
#include <EEPROMex.h>

#include <PID_v1.h>

const int bufferSize = 600;
const int jsonDocSize = 512;


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
        //Serial.print("SAVE condID:");
        Serial.println(condID);
        int add = startAddress;
        add += sizeof(int);
        add = regulSalinite.save(add);
        add = regulTemp.save(add);
        
        return add;
    }

    int load() {
        int add = startAddress;
        add += sizeof(int);
        add = regulSalinite.load(add);
        add = regulTemp.load(add);
        return add;
    }


    bool serializeData(char* buffer, uint32_t timeString, uint8_t sender, uint8_t condID, bool sendConsignes, StaticJsonDocument<jsonDocSize> doc) {
        //Serial.println("SENDDATA");
        //DynamicJsonDocument doc(512);
        //StaticJsonDocument<jsonDocSize> doc;

        doc[F("command")] = 3;
        doc[F("condID")] = condID;
        doc[F("senderID")] = sender;

        //Serial.print("CondID:"); Serial.println(condID);
        

        //Serial.print(F("CONDID:")); Serial.println(condID);
        //Serial.print(F("socketID:")); Serial.println(socketID);
        doc[F("time")] = timeString;

        JsonArray data = doc.createNestedArray(F("data"));
        JsonObject dataArray[3];

        if (sendConsignes) {
            JsonObject regulS = doc.createNestedObject(F("regulS"));
            JsonObject regulT = doc.createNestedObject(F("regulT"));
            regulT[F("cons")] = regulTemp.consigne;
            regulS[F("cons")] = regulSalinite.consigne;
        }

        for (int i = 0; i < 3; i++) {
            dataArray[i] = data.createNestedObject();
            dataArray[i][F("MID")] = Meso[i]._mesocosmeIndex;
            dataArray[i][F("temp")] = Meso[i].temperature;
            dataArray[i][F("cond")] = Meso[i].cond;
            dataArray[i][F("sali")] = Meso[i].salinite;
            //dataArray[i][F("oxy")] = Meso[i].oxy;
            dataArray[i][F("flow")] = Meso[i].debit;
            dataArray[i][F("salSPID_pc")] = Meso[i].salSortiePID_pc;
            dataArray[i][F("tempSPID_pc")] = Meso[i].tempSortiePID_pc;
            dataArray[i][F("oxy_pc")] = Meso[i].oxy_pc;
            //dataArray[i][F("oxy_temp")] = Meso[i].oxy_temp;
        }
        serializeJson(doc, buffer, bufferSize);
        return true;
    }

    bool serializeParams(char* buffer, uint32_t timeString, uint8_t sender, StaticJsonDocument<jsonDocSize> doc) {

        //Serial.println(F("SEND PARAMS"));
        //StaticJsonDocument<jsonDocSize> doc;

        doc[F("command")] = 2;
        doc[F("condID")] = condID;
        doc[F("senderID")] = sender;
        doc[F("time")] = timeString;
        /*doc["mesureTemp"] = Hamilton[3].temp_sensorValue;
        doc["mesurepH"] = Hamilton[3].pH_sensorValue;*/

        JsonObject regulT = doc.createNestedObject(F("regulT"));
        regulT[F("cons")] = regulTemp.consigne;
        regulT[F("Kp")] = regulTemp.Kp;
        regulT[F("Ki")] = regulTemp.Ki;
        regulT[F("Kd")] = regulTemp.Kd;
        if (regulTemp.autorisationForcage) regulT[F("autForcage")] = "true";
        else regulT[F("autForcage")] = "false";
        regulT[F("consForcage")] = regulTemp.consigneForcage;
        regulT[F("offset")] = regulTemp.offset;


        JsonObject regulS = doc.createNestedObject(F("regulS"));
        regulS[F("cons")] = regulSalinite.consigne;
        regulS[F("Kp")] = regulSalinite.Kp;
        regulS[F("Ki")] = regulSalinite.Ki;
        regulS[F("Kd")] = regulSalinite.Kd;
        if (regulSalinite.autorisationForcage) regulS[F("autForcage")] = "true";
        else regulS[F("autForcage")] = "false";
        regulS[F("consForcage")] = regulSalinite.consigneForcage;
        regulS[F("offset")] = regulSalinite.offset;


        serializeJson(doc, buffer, bufferSize);
    }

    void deserializeParams(StaticJsonDocument<jsonDocSize> doc) {
        Serial.print("CONDID:"); Serial.println(condID);
        Serial.println("DESERIALIZE PARAMS:"); 
        int condID = doc[F("condID")]; // 0, 2, 3

        JsonObject regulT = doc[F("regulT")];

        regulTemp.consigne = regulT[F("cons")]; // 24.2
        regulTemp.Kp = regulT[F("Kp")]; // 2.1
        regulTemp.Ki = regulT[F("Ki")]; // 2.1
        regulTemp.Kd = regulT[F("Kd")]; // 2.1
        const char* regulTemp_autorisationForcage = regulT[F("autForcage")];
        if (strcmp(regulTemp_autorisationForcage, "true") == 0 || strcmp(regulTemp_autorisationForcage, "True") == 0) regulTemp.autorisationForcage = true;
        else regulTemp.autorisationForcage = false;
        regulTemp.consigneForcage = regulT[F("consForcage")]; // 2.1
        regulTemp.offset = regulT[F("offset")];

        JsonObject regulS = doc[F("regulS")];

        regulSalinite.consigne = regulS[F("cons")]; // 24.2
        regulSalinite.Kp = regulS[F("Kp")]; // 2.1
        regulSalinite.Ki = regulS[F("Ki")]; // 2.1
        regulSalinite.Kd = regulS[F("Kd")]; // 2.1
        const char* regulS_autorisationForcage = regulS[F("autForcage")];
        if (strcmp(regulS_autorisationForcage, "true") == 0 || strcmp(regulS_autorisationForcage, "True") == 0) regulSalinite.autorisationForcage = true;
        else regulSalinite.autorisationForcage = false;
        regulSalinite.consigneForcage = regulS[F("consForcage")]; // 2.1
        regulSalinite.offset = regulS[F("offset")];

        Serial.print("regulSalinite.consigne :"); Serial.println(regulSalinite.consigne);

    }

    void deserializeData(StaticJsonDocument<jsonDocSize> doc) {
        

        Serial.print("DESERIALIZE DATA :"); 

        for (JsonObject elem : doc[F("data")].as<JsonArray>()) {
            int MesoID = elem[F("MID")]; // 0, 2, 3
            Meso[MesoID].temperature = elem[F("temp")]; // 0, 0, 0
            Meso[MesoID].cond = elem[F("cond")]; // 0, 0, 0
            Meso[MesoID].salinite = elem[F("sali")]; // 0, 0, 0
            //Meso[MesoID].oxy = elem[F("oxy")]; // 0, 0, 0
            Meso[MesoID].debit = elem[F("flow")]; // 0, 0, 0
            Meso[MesoID].tempSortiePID_pc = elem[F("tempSPID_pc")]; // 2
            Meso[MesoID].salSortiePID_pc = elem[F("salSPID_pc")]; // 2
            Meso[MesoID].oxy_pc = elem[F("oxy_pc")]; // 2
           // Meso[MesoID].oxy_temp = elem[F("oxy_temp")]; // 2
            Serial.print("oxy_pc :"); Serial.println(Meso[MesoID].oxy_pc);
        }
    }
}; 
