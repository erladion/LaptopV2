using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using System.IO;
using System.IO.Ports;
using DevExpress.XtraEditors.Controls;

namespace LaptopV2
{
    enum Commands
    {
        None,
        Forwards,
        Backwards,
        LeftRot,
        RightRot,
        LeftTurn,
        RightTurn
    }

    public partial class Form1 : DevExpress.XtraEditors.XtraForm
    {
        Commands command = Commands.None;

        const int minSpeed = 1;
        const int maxSpeed = 10;
        const int minTurn = 1;
        const int maxTurn = 10;
        const int speedFreq = 1;
        const int turnFreq = 1;        

        SerialPort bluetooth;
        List<Sensordata> dataList;

        public Random rand = new Random();

        string comPort;
        bool connected = false;

        public Form1()
        {
            InitializeComponent();            

            dataList = new List<Sensordata>();

            speedBar.Properties.Maximum = maxSpeed;
            speedBar.Properties.Minimum = minSpeed;
            speedBar.Properties.TickFrequency = speedFreq;

            turnBar.Properties.Maximum = maxTurn;
            turnBar.Properties.Minimum = minTurn;
            turnBar.Properties.TickFrequency = turnFreq;

            progressBarControl1.Properties.Step = 5;
            progressBarControl1.Properties.Maximum = 100;
            progressBarControl1.Properties.Minimum = 0;

            front.Text = "N/A";
            back.Text = "N/A";
            leftBack.Text = "N/A";
            leftFront.Text = "N/A";
            rightBack.Text = "N/A";
            rightFront.Text = "N/A";            

            modeLabel.Text = "Autonomous";

            if (modeLabel.Text == "Autonomous")
            {
                currentCommandLabel.Hide();
                currentCommand.Hide();
            }

            currentCommand.Text = Commands.None.ToString();                       
        }              

        private bool connect()
        {
            bluetooth = new SerialPort(comPort, 115200, Parity.None, 8, StopBits.One);
                     
            while (bluetooth.IsOpen != true)
            {
                progressBarControl1.PerformStep();
                progressBarControl1.Update();
                if (progressBarControl1.Position == 100)
                {
                    progressBarControl1.Position = 0;
                }    
                try
                {
                    bluetooth.Open();
                    connected = true;
                    connectedLabel.Text = "Connected";                   
                    progressBarControl1.Position = 0;
                    
                    return true;
                }                    
                catch (IOException)
                {
                    bluetooth = null;
                    throw;
                }                            
            }
            connectedLabel.Text = "Disconnected";
            return false;
        }

        private void disconnect()
        {
            if (bluetooth != null && bluetooth.IsOpen)
            {
                bluetooth.Close();
                bluetooth = null;
                connectedLabel.Text = "Disconnected";
            }
            else
            {
                bluetooth = null;
                connectedLabel.Text = "Disconnected";
            }
        }

        private void sendBluetooth()
        {
            if (bluetooth != null && bluetooth.IsOpen)
            {                
                bluetooth.Write(((int)command).ToString() + (speedBar.Value - 1) + (turnBar.Value - 1));
            }
        }

        private void readBluetooth()
        {
            if (bluetooth != null && bluetooth.IsOpen)
            {
                int bytes = bluetooth.BytesToRead;
                if (bytes > 0)
                {                    
                    bluetooth.ReadTimeout = 200;
                    try
                    {
                        string data = bluetooth.ReadLine();
                        if (data.Length >= 12)
                        {
                            Sensordata sensor = new Sensordata(data);
                            dataList.Insert(0, sensor);
                        }
                    }
                    catch (Exception e) { }                    

                }
            }
        }

        void checkIfListFull()
        {
            if (dataList.Count > 10)
            {
                for (int i = 0; i < dataList.Count; i++)
                {
                    if (i >= 10)
                    {
                        dataList.RemoveAt(10);
                    }
                }
            }
        }


        /*
         * Converts a number to a binary number represented in a string
         * 
         */
        string GetIntBinaryString(int n)
        {
            char[] result = new char[11];
            int pos = 10;
            int i = 0;

            while (i < 11)
            {
                if ((n & (1 << i)) != 0)
                {
                    result[pos] = '1';
                }
                else
                {
                    result[pos] = '0';
                }
                pos--;
                i++;
            }
            return new string(result);
        }

        byte reverseByte(byte originalByte){
            int result = 0;
            for (int i = 0; i < 8; i++)
            {
                result = result << 1;
                result += originalByte & 1;
                originalByte = (byte)(originalByte >> 1);
            }

            return (byte)result;
        }

        /*
        *
        * Checks which keys are currently pressed if any.
        *
        */
        private void CheckKeys()
        {
            if (Keyboard.IsKeyDown(Key.Up))
            {
                //readBluetooth();                
                if (Keyboard.IsKeyDown(Key.Left) && Keyboard.IsKeyDown(Key.Right))
                    command = Commands.Forwards;                
                else if (Keyboard.IsKeyDown(Key.Left))
                    command = Commands.LeftTurn;
                else if (Keyboard.IsKeyDown(Key.Right))
                    command = Commands.RightTurn;
                else if (Keyboard.IsKeyDown(Key.Down))
                    command = Commands.None;
                else
                    command = Commands.Forwards;

            }
            else if (Keyboard.IsKeyDown(Key.Left))
            {
                if (Keyboard.IsKeyDown(Key.Right))
                    command = Commands.None;
                else if (Keyboard.IsKeyDown(Key.Down))
                    command = Commands.LeftRot;
                else
                    command = Commands.LeftRot;

            }
            else if (Keyboard.IsKeyDown(Key.Right))
            {
                if (Keyboard.IsKeyDown(Key.Down))
                    command = Commands.RightRot;
                else
                    command = Commands.RightRot;

            }
            else if (Keyboard.IsKeyDown(Key.Down))
            {
                command = Commands.Backwards;
            }
            else
                command = Commands.None;
            currentCommand.Text = command.ToString();
        }

        void changeCommand(Commands newCommand)
        {

            if (newCommand != command)
            {
                command = newCommand;
                sendBluetooth();
            }
        }

        void updateLabels()
        {
            if (dataList[0].manualMode == true)
            {
                modeLabel.Text = "Manual";
                currentCommandLabel.Show();
                currentCommand.Show();
            }
            else
            {
                modeLabel.Text = "Autonomous";
                currentCommandLabel.Hide();
                currentCommand.Hide();
            }

            front.Text = dataList[0].sensorFront.ToString();
            back.Text = dataList[0].sensorBack.ToString();
            leftBack.Text = dataList[0].sensorFrontLeft.ToString();
            leftFront.Text = dataList[0].sensorFrontRight.ToString();
            rightBack.Text = dataList[0].sensorBackLeft.ToString();
            rightFront.Text = dataList[0].sensorBackRight.ToString();
        }

        void updateGraphs()
        {
            DevExpress.XtraCharts.SeriesPoint frontLeft;
            DevExpress.XtraCharts.SeriesPoint frontRight;
            DevExpress.XtraCharts.SeriesPoint backLeft;
            DevExpress.XtraCharts.SeriesPoint backRight;
            DevExpress.XtraCharts.SeriesPoint front;
            DevExpress.XtraCharts.SeriesPoint back;

            for (int i = 0; i < dataList.Count; i++)
            {
                frontLeft = new DevExpress.XtraCharts.SeriesPoint(i, new double[] { dataList[i].sensorFrontLeft });
                frontRight = new DevExpress.XtraCharts.SeriesPoint(i, new double[] { dataList[i].sensorFrontRight });
                backLeft = new DevExpress.XtraCharts.SeriesPoint(i, new double[] { dataList[i].sensorBackLeft });
                backRight = new DevExpress.XtraCharts.SeriesPoint(i, new double[] { dataList[i].sensorBackRight });
                front = new DevExpress.XtraCharts.SeriesPoint(i, new double[] { dataList[i].sensorFront });
                back = new DevExpress.XtraCharts.SeriesPoint(i, new double[] { dataList[i].sensorBack });

                sensorsLeftGraph.Series[0].Points.Add(frontLeft);
                sensorsLeftGraph.Series[1].Points.Add(frontRight);

                sensorsRightGraph.Series[0].Points.Add(backLeft);
                sensorsRightGraph.Series[1].Points.Add(backRight);

                sensorsFrontBackGraph.Series[0].Points.Add(front);
                sensorsFrontBackGraph.Series[1].Points.Add(back);
            }            
        }

        void drawReflexsensor()
        {
            int size = 15;
            int startLocationX = 135;
            int startLocationY = 150;
            int labelLocationX = startLocationX + 1;
            int labelLocationY = 135;

            reflexSensor.Show();
            reflexSensor.Location = new Point(labelLocationX, labelLocationY);

            Graphics g = this.CreateGraphics();

            string reflexmoduleDraw;

            if (dataList.Count != 0)
            {
                reflexmoduleDraw = GetIntBinaryString(dataList[0].reflexmodule);
            }
            else
            {
                reflexmoduleDraw = GetIntBinaryString(0);
            }            

            for (int i = 0; i < 11; i++)
            {
                if(reflexmoduleDraw[i] == '1'){
                    g.FillEllipse(new SolidBrush(Color.Red), startLocationX + size * i, startLocationY, size, size);
                }
                else
                {
                    g.FillEllipse(new SolidBrush(Color.Black), startLocationX + size * i, startLocationY, size, size);
                }
            }
        }

        private void disconnectButton_Click(object sender, EventArgs e)
        {            
            disconnect();                  
        }

        private void connectButton_Click(object sender, EventArgs e)
        {
            comPort = comBox.Text;
            connected = connect();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            CheckKeys();
            readBluetooth();
            checkIfListFull();

            drawReflexsensor();
            
            if (dataList.Count != 0)
            {
                updateGraphs();
                updateLabels();
            }
        }                                 
    }
}
