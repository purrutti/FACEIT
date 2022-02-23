#pragma once

#define MESO 

#include "C:\Users\FACE-IT\Desktop\FACEIT\libraries\cocorico2\ModbusSensor.h"

class Regul {
public:

    double consigne;
    double Kp;
    double Ki;
    double Kd;
    bool autorisationForcage;
    int consigneForcage;
    double offset;
    

    Regul() {};

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

class Mesocosme {
public:

    uint8_t _pinDebitmetre;
    uint8_t _pin_V3V;
    uint8_t _pin_V2V;
    uint8_t _mesocosmeIndex;

    float debit;
    float temperature;
    float cond;
    float oxy;
    float salinite;

    float oxy_pc;
    float oxy_temp;

    int startAddress;

    double tempSortiePID;
    double tempSortiePID_pc;
    double salSortiePID;
    double salSortiePID_pc;

    Mesocosme() {
    };
    Mesocosme(uint8_t mesocosmeIndex) {
        _mesocosmeIndex = mesocosmeIndex;
    };
    Mesocosme(uint8_t pinDebitmetre, uint8_t pinV3V, uint8_t pinV2V, uint8_t mesocosmeIndex) {
        _pinDebitmetre = pinDebitmetre;
        _mesocosmeIndex = mesocosmeIndex;
        _pin_V3V = pinV3V;
        _pin_V2V = pinV2V;
    };


    float readFlow(int lissage) {
        
        int ana = analogRead(_pinDebitmetre); // 0-1023 value corresponding to 0-5 V corresponding to 0-20 mA
        //int mA = map(ana, 0, 1023, 0, 2000); //map to milli amps with 2 extra digits
        double ancientDebit = debit;
        //debit = (0.625 * (mA - 400)) / 100.0; // flowrate in l/mn

        int v = map(ana, 0, 165, 0, 1000);//map to 0-10v with 2 digits

        debit = map(v, 200, 1000, 0, 1000)/100.0;//map to l/mn with 2 digits

        debit = (lissage * debit + (100.0 - lissage) * ancientDebit)/100.0;

        if (debit < 0) debit = 0;

        return debit;
    }

    int save() {
        int add = startAddress;
        EEPROM.updateInt(add, _mesocosmeIndex); add += sizeof(int);
        
        return add;
    }

    int load() {
        int add = startAddress;
        _mesocosmeIndex = EEPROM.readInt(add); add += sizeof(int);
        
        return add;
    }

};