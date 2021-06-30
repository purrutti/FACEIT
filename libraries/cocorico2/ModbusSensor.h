#pragma once

class ModbusSensor {
public:
    union u_tag {
        uint16_t b[2];
        float fval;
    } u;

    float params[4];

    bool querySent = false;
    byte status[5];

    uint16_t data[16];
    modbus_t query;

    /*uint16_t dataW[1];
    modbus_t queryW;

    uint16_t dataR[10];
    modbus_t queryR;

    uint16_t dataStatus[1];
    modbus_t queryStatus;

    uint16_t dataCalibration[2];
    modbus_t queryCalibration;

    uint16_t dataCalValidation[16];
    modbus_t queryCalValidation;*/

    //uint16_t calibrationAddresses[12];
    //uint16_t validationAddresses[6];
    ModbusSensor() {}

    ModbusSensor(uint8_t slaveAddress, Modbus *m) {
        

        query.u8id = slaveAddress; // slave address
        query.u8fct = 6; // function code (this one is registers read)
        query.u16RegAdd = 1; // start address in slave
        query.u16CoilsNo = 1; // number of elements (coils or registers) to read
        query.au16reg = data; // pointer to a memory array in the Arduino
        data[0] = 5;


        /*queryW.u8id = slaveAddress; // slave address
        queryW.u8fct = 6; // function code (this one is registers read)
        queryW.u16RegAdd = 1; // start address in slave
        queryW.u16CoilsNo = 1; // number of elements (coils or registers) to read
        queryW.au16reg = dataW; // pointer to a memory array in the Arduino
        dataW[0] = 5;

        queryR.u8id = slaveAddress; // slave address
        queryR.u8fct = 3; // function code (this one is registers read)
        queryR.u16RegAdd = 83; // start address in slave
        queryR.u16CoilsNo = 10; // number of elements (coils or registers) to read
        queryR.au16reg = dataR; // pointer to a memory array in the Arduino

        queryCalibration.u8id = slaveAddress; // slave address
        queryCalibration.u8fct = 16; // function code (this one is registers read)
        queryCalibration.u16RegAdd = 512; // start address in slave
        queryCalibration.u16CoilsNo = 2; // number of elements (coils or registers) to read
        queryCalibration.au16reg = dataCalibration; // pointer to a memory array in the Arduino

        queryCalValidation.u8id = slaveAddress; // slave address
        queryCalValidation.u8fct = 16; // function code (this one is registers read)
        queryCalValidation.u16RegAdd = 638; // start address in slave
        queryCalValidation.u16CoilsNo = 16; // number of elements (coils or registers) to read
        queryCalValidation.au16reg = dataCalValidation; // pointer to a memory array in the Arduino
        */

        master = m;

        /*
        512: offset temperature
        514: pente temperature
        516: offset oxy (0%) **** offset gamme 1
        518: **** pente gamme 1
        520: **** offset gamme 2
        522: pente oxy (100%) **** pente gamme 2
        524: **** offset gamme 3
        526: **** pente gamme 3
        528: **** offset gamme 4
        530: **** pente gamme 4
        532: **** offset TU
        534: **** pente TU
        */

        /*
        638: validation calibration param 0 (temperature)
        654: validation calibration param 1
        670: validation calibration param 2
        686: validation calibration param 3
        702: validation calibration param 4
        718: validation calibration param 5
        */
        //for (int i = 0; i < 12; i++) calibrationAddresses[i] = 512 + 2 * i;
        //for (int i = 0; i < 6; i++) validationAddresses[i] = 638 + 16 * i;
    }
    



    Modbus *master;

    void setQuery(uint8_t fct, uint16_t RegAdd, uint16_t CoilsNb) {
        query.u8fct = fct; // function code (this one is registers read)
        query.u16RegAdd = RegAdd; // start address in slave
        query.u16CoilsNo = CoilsNb; // number of elements (coils or registers) to read
    }

    void setQueryW() {
        setQuery(6, 1, 1);
        data[0] = 5;
    }
    void setQueryR() {
        setQuery(3, 83, 10);
        data[0] = 5;
    }
    void setQueryCalibration(int offset) {
        setQuery(16, offset, 2);
    }
    void setQueryCalValidation() {
        setQuery(16, 638, 16);
    }


    /*int requestStatus()
    {
        if (!querySent) {
            master->query(queryStatus);
            querySent = true;
        }
        else {
            master->poll();
            if (master->getState() == COM_IDLE) {
                querySent = false;

                for (int i = 0; i < 5; i++) {
                    for (int y = 0; y < 3; y++) {
                        bitWrite(status[i], y, bitRead(dataStatus[0], y * i));
                    }
                    //Serial.println(i);
                    switch (status[i]) {
                    case 0:
                        //Serial.println("Mesure OK");
                        break;
                    case 1:
                        //Serial.println("Mesure OK mais hors gamme");
                        break;
                    case 2:
                        //Serial.println("Mesure OK avec INFO 1");
                        break;
                    case 3:
                        //Serial.println("Mesure OK avec INFO 2");
                        break;
                    case 4:
                        //Serial.println("Mesure Impossible (hors gamme)");
                        break;
                    case 5:
                        //Serial.println("Mesure Impossible avec INFO 3");
                        break;
                    case 6:
                        //Serial.println("Mesure Impossible avec INFO 4");
                        break;
                    case 7:
                        //Serial.println("Mesure en cours");
                        break;
                    }
                }
                return 1;
            }
        }
        return 0;

    }*/

    bool requestValues()
    {
        setQueryW();
        if (!querySent) {
            master->query(query);
            querySent = true;
        }
        else {
            master->poll();
            if (master->getState() == COM_IDLE) {
                querySent = false;
                return 1;
            }
        }
        return 0;
    }

    bool readValues()
    {
        setQueryR();
        if (!querySent) {
            master->query(query);
            querySent = true;
        }
        else {
            master->poll();
            if (master->getState() == COM_IDLE) {
                querySent = false;
                u.b[0] = data[1];
                u.b[1] = data[0];
                params[0] = u.fval;
                u.b[0] = data[3];
                u.b[1] = data[2];
                params[1] = u.fval;
                u.b[0] = data[5];
                u.b[1] = data[4];
                params[2] = u.fval;
                u.b[0] = data[7];
                u.b[1] = data[6];
                params[3] = u.fval;
                return 1;
            }
        }
        return 0;
    }

    bool calibrateCoeff(float value, int offset)
    {
        setQueryCalibration(offset);
        u.fval = value;
        data[0] = u.b[1];
        data[1] = u.b[0];

        Serial.println("calibfrate coeff");
        Serial.print("sensor address:"); Serial.println(query.u8id);
        Serial.print("Offset:"); Serial.println(query.u16RegAdd);
        Serial.print("coils number:"); Serial.println(query.u16CoilsNo);
        Serial.print("function:"); Serial.println(query.u8fct);

        if (!querySent) {
            master->query(query);
            querySent = true;
            Serial.println("query sent");
        }
        else {
            master->poll();
            if (master->getState() == COM_IDLE) {
                querySent = false;
                Serial.println("query OK");
                return 1;
            }
        }
        return 0;
    }

    bool factoryReset()
    {
        Serial.println("factory reset");
        setQuery(16, 2, 1);
        data[0] = 31;

        if (!querySent) {
            master->query(query);
            querySent = true;
            Serial.println("query sent");
        }
        else {
            master->poll();
            if (master->getState() == COM_IDLE) {
                querySent = false;
                Serial.println("query OK");
                return 1;
            }
        }
        return 0;
    }

    bool validateCalibration(int offset)
    {
        setQueryCalValidation();
        setQuery(16, offset, 16);
        for (int i = 0; i < 16; i++) data[i] = 0;
        data[0] = 'P';
        data[1] = 'i';
        data[2] = 'e';
        data[3] = 'r';
        data[4] = 'r';
        data[5] = 'e';

        data[8] = 32; //minutes
        data[9] = 17; //heures
        data[10] = 31; //jour
        data[11] = 12; //mois
        data[12] = 2020; //année

        if (!querySent) {
            master->query(query);
            querySent = true;
            Serial.println("query sent");
        }
        else {
            master->poll();
            if (master->getState() == COM_IDLE) {
                querySent = false;
                
                for (int i = 0; i < 16; i++) data[i] = 0;
                return 1;
            }
        }
        return 0;
    }

};