using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaptopV2
{
    class Sensordata
    {
        public bool startButton;
        public bool manualMode;

        public double sensorFront;
        public double sensorBack;
        public double sensorFrontLeft;
        public double sensorFrontRight;
        public double sensorBackLeft;
        public double sensorBackRight;
        public double gyro;

        public int reflexmodule;

        public Sensordata(string data)
        {
            startButton = (data[0] & (1 << 6)) != 0;
            manualMode = (data[0] & (1 << 5)) != 0;

            reflexmodule = (data[1] << 8) + data[2];
            gyro = (data[3] << 8) + data[4];
            sensorFront = (data[5] << 8) + data[6];
            sensorBack = (data[7] << 8) + data[8];
            sensorFrontRight = data[9];
            sensorBackRight = data[10];
            sensorFrontLeft = data[11];
            sensorBackLeft = data[12];
        }
    }
}
