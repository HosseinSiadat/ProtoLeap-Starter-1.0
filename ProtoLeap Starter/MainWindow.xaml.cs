/* This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
    */

using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Collections.Generic;
using mcp2210_dll_m;
using System.ComponentModel;

namespace ProtoLeap_Starter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// Program to interact with MCP2210 USB-SPI
    /// </summary>
    /// <company> Arshon Technology Incorporated </company>
    /// <copyright> Open-source License </copyright>
    /// <datecreated> 2016-10-31 </datecreated>
    /// <author> Hossein Siadatkhoo </author>
    public partial class MainWindow : Window
    {
        //Default VID and PID for MCP2210
        public const ushort DEFAULT_VID = 0x4d8;
        public const ushort DEFAULT_PID = 0xde;
        //Device handlers
        IntPtr[] deviceHandle;
        //Errors are negative integers
        int error;
        //Path for device
        StringBuilder path = new StringBuilder();
        //Serial Number
        StringBuilder SN = new StringBuilder();
        //Dictionaries used to store Serial Number and Name of devices
        Dictionary<string, string> SerialtoName = new Dictionary<string, string>();
        Dictionary<string, string> NametoSerial = new Dictionary<string, string>();
        //Dictionary for Character to 7 Segment 
        Dictionary<char, byte> CharToSeg = new Dictionary<char, byte>()
        {
            {'0',0x01},
            {'1',0x4F},
            {'2',0x12},
            {'3',0x06},
            {'4',0x4C},
            {'5',0x24},
            {'6',0x20},
            {'7',0x0F},
            {'8',0x00},
            {'9',0x0C},
            {'a',0x08},
            {'b',0x60},
            {'c',0x31},
            {'d',0x42},
            {'e',0x30},
            {'f',0x38},
            {'A',0x08},
            {'B',0x60},
            {'C',0x31},
            {'D',0x42},
            {'E',0x30},
            {'F',0x38}
        };
        //Timer ON/OFF variables 
        bool timerTemp = false;
        bool timerADC = false;
        bool timerSeg = false;
        bool timerMotor = false;
        //Timers used to update outputs and inputs in preset intervals
        private DispatcherTimer dispatcherTimerTemp;
        private DispatcherTimer dispatcherTimerADC;
        private DispatcherTimer dispatcherTimerMotor;
        //Chars used for dual seven segment display
        char DigitOne, DigitTwo;
        uint digitOne, digitTwo;
        uint digitOneTemp, digitTwoTemp;
        //Chars used for temperature sensors  
        char DigitOneTemp, DigitTwoTemp;
        char[] Digit = new char[2];
        char[] DigitTemp = new char[2];
        //GPIO bytes for configuring between CS and GPIO
        byte[] GpioPinDes = new byte[9];
        //Incrementing int for devices which have not been modified and stored with given name
        uint check;
        //Temperature variables
        double temp = 0;
        int temp1 = 0;


        public MainWindow()
        {
            InitializeComponent();

            //Setup dictionary of known devices
            var fs = File.Open("SerialNumber.txt", FileMode.OpenOrCreate, FileAccess.ReadWrite);
            var SR = new StreamReader(fs);
            using (SR)
            {
                string line;
                while ((line = SR.ReadLine()) != null)
                {
                    string[] parts = line.Split(',');
                    if (parts.Length > 1)
                    {
                        SerialtoName.Add(parts[0], parts[1]);
                        NametoSerial.Add(parts[1], parts[0]);
                    }
                }
            }
            SR.Close();

            //check for connected devices
            int count = MCP2210.M_Mcp2210_GetConnectedDevCount(DEFAULT_VID, DEFAULT_PID);
            if (count > 0)
                deviceHandle = new IntPtr[count];
            //Display devices available
            DeviceCount.Text = count.ToString();
            //Check that atleast one device is avalaible, then check if they are stored in memory with a given name
            //If they are not stored, label them with SPI#
            //Connect to all available devices
            if (count > 0)
            {
                for (uint i = 0; i < count; i++)
                {
                    deviceHandle[i] = MCP2210.M_Mcp2210_OpenByIndex(DEFAULT_VID, DEFAULT_PID, i, path);
                    MCP2210.M_Mcp2210_GetSerialNumber(deviceHandle[i], SN);
                    //Check for any errors
                    error = MCP2210.M_Mcp2210_GetLastError();
                    //Display any error and connection status
                    if (error != MCP2210.M_E_SUCCESS)
                    {
                        LastError.Items.Add(error.ToString());
                        LastError.Items.Add("Device cannot be opened!");
                        LastError.SelectedIndex = LastError.Items.Count - 1;
                        LastError.ScrollIntoView(LastError.SelectedItem);
                    }
                    else
                    {
                        LastError.Items.Add(error.ToString());
                        LastError.Items.Add("Device(s) Found!");
                        LastError.SelectedIndex = LastError.Items.Count - 1;
                        LastError.ScrollIntoView(LastError.SelectedItem);
                    }
                    //Check for stored device name
                    if (SerialtoName.ContainsKey(SN.ToString()))
                    {
                        Temp.Items.Add(SerialtoName[SN.ToString()]);
                        SevenSeg.Items.Add(SerialtoName[SN.ToString()]);
                        ADC.Items.Add(SerialtoName[SN.ToString()]);
                        DacDevice.Items.Add(SerialtoName[SN.ToString()]);
                        Motor.Items.Add(SerialtoName[SN.ToString()]);
                    }
                    //assign temperary device name
                    else
                    {
                        bool check = true;
                        int spi = 0;
                        while (check)
                        {
                            spi++;
                            if (!NametoSerial.ContainsKey("SPI" + spi.ToString()))
                            {
                                check = false;
                                SerialtoName.Add(SN.ToString(), ("SPI" + spi.ToString()));
                                NametoSerial.Add(("SPI" + spi.ToString()), SN.ToString());
                            }
                        }
                        Temp.Items.Add("SPI" + spi.ToString());
                        SevenSeg.Items.Add("SPI" + spi.ToString());
                        ADC.Items.Add("SPI" + spi.ToString());
                        DacDevice.Items.Add("SPI" + spi.ToString());
                        Motor.Items.Add("SPI" + spi.ToString());
                    }
                }
            }
            else
            {
                LastError.Items.Add(error.ToString());
                LastError.Items.Add("No Devices Found!");
                LastError.SelectedIndex = LastError.Items.Count - 1;
                LastError.ScrollIntoView(LastError.SelectedItem);
            }

            //Setup GPIO array
            for (int i = 0; i < 9; i++)
            {
                GpioPinDes[i] = 0;
            }
        }

        //Button sends command to DAC, representing 0-VREF voltage output
        private void BDACON_Click(object sender, RoutedEventArgs e)
        {
            double Vref = 5.00;                 //Reference voltage provided to DAC from circuit
            int DeviceNumber = DacDevice.SelectedIndex; //Device chosen by user on GUI
            int size = 2;                       //Size of transmit and receive
            uint baudRate = 1000000;            //Baud rate of device
            uint idleCsVal = 0x1ff;             //Bit is 1 for clock idle low 
            uint activeCsVal = 0x1ef;           //Bit is 0 for CS active low, GP4 set as active low CS 
            uint csToDataDly = 0;               //Time delay from CS to data
            uint dataToDataDly = 0;             //Time delay from data to data
            uint dataToCsDly = 0;               //Time delay from data to CS
            uint txferSize = (uint)(size);      //tXfer size set to size bytes
            byte spiMd = 0;                     //SPI mode from connected devices datasheet
            uint csmask = 0x10;                 //1 represents CS, set GP4 as CS

            //Setup transfer and receive bytes
            byte[] txData1 = new byte[size], rxData1 = new byte[size];
            //Read value from DAC textbox and parse it
            double value = Double.Parse(TextBoxDAC.Text);
            //Set boundaries for DAC input
            if (value > Vref)
                value = Vref;
            else if (value < 0)
                value = 0;
            //Make 0-5V input into 12 bit transfer for DAC
            int DAC = (int)(value * 4095.0 / Vref);
            txData1[0] = (byte)((DAC & 0x0F00) >> 8);
            txData1[0] = (byte)(txData1[0] | 0x70);
            txData1[1] = (byte)(DAC & 0xFF);

            //Start communication with DAC
            error = MCP2210.M_Mcp2210_xferSpiDataEx(deviceHandle[DeviceNumber], txData1, rxData1, ref baudRate, ref txferSize, csmask, ref idleCsVal, ref activeCsVal,
            ref csToDataDly, ref dataToCsDly, ref dataToDataDly, ref spiMd);
            //Check for error and update status if required
            if (error != MCP2210.M_E_SUCCESS)
            {
                LastError.Items.Add(error.ToString());
                LastError.Items.Add("Device Disconnected!");
                LastError.SelectedIndex = LastError.Items.Count - 1;
                LastError.ScrollIntoView(LastError.SelectedItem);
            }
        }

        //Button to send OFF command to DAC
        private void BDACOFF_Click(object sender, RoutedEventArgs e)
        {
            int size = 2;                       //Size of transmit and receive
            int DeviceNumber = DacDevice.SelectedIndex; //Device chosen by user on GUI
            uint baudRate = 1000000;            //Baud rate of device
            uint idleCsVal = 0x1ff;             //Bit is 1 for clock idle low 
            uint activeCsVal = 0x1ef;           //Bit is 0 for CS active low, GP4 set as active low CS 
            uint csToDataDly = 0;               //Time delay from CS to data
            uint dataToDataDly = 0;             //Time delay from data to data
            uint dataToCsDly = 0;               //Time delay from data to CS
            uint txferSize = (uint)(size);      //tXfer size set to size bytes
            byte spiMd = 0;                     //SPI mode from connected devices datasheet
            uint csmask = 0x10;                 //1 represents CS, set GP4 as CS

            //Setup transfer and receive bytes
            byte[] txData1 = new byte[size], rxData1 = new byte[size];
            txData1[0] = 0x70;
            txData1[1] = 0x00;

            //Start communication with DAC
            error = MCP2210.M_Mcp2210_xferSpiDataEx(deviceHandle[DeviceNumber], txData1, rxData1, ref baudRate, ref txferSize, csmask, ref idleCsVal, ref activeCsVal,
            ref csToDataDly, ref dataToCsDly, ref dataToDataDly, ref spiMd);
            //Check for error and update status if required
            if (error != MCP2210.M_E_SUCCESS)
            {
                LastError.Items.Add(error.ToString());
                LastError.Items.Add("Device Disconnected!");
                LastError.SelectedIndex = LastError.Items.Count - 1;
                LastError.ScrollIntoView(LastError.SelectedItem);
            }
        }

        //Button to send start command to temperature sensor
        private void BTempStart_Click(object sender, RoutedEventArgs e)
        {
            //Checking whether timer is already on
            if (!timerTemp)
            {
                // DispatcherTimer setup
                dispatcherTimerTemp = new System.Windows.Threading.DispatcherTimer();
                dispatcherTimerTemp.Tick += new EventHandler(dispatcherTimerTemp_Tick);
                dispatcherTimerTemp.Interval = new TimeSpan(0, 0, 0, 0, 100);
                dispatcherTimerTemp.Start();
                timerTemp = true;
            }
        }

        //Button to send stop command to temperature sensor
        private void BTempStop_Click(object sender, RoutedEventArgs e)
        {
            //checking if timer is already off
            if (timerTemp)
            {
                dispatcherTimerTemp.Stop();
                timerTemp = false;
            }
        }

        //Timer setup and communication for Temperature sensor
        private void dispatcherTimerTemp_Tick(object sender, EventArgs e)
        {
            int DeviceNumber = Temp.SelectedIndex; //Device chosen by user on GUI
            uint pbaudRate77 = 1000000;  //baud rate
            uint pidleCsVal77 = 0x1ff;   //Bit is 1 for clock idle low 
            uint pactiveCsVal77 = 0x1ef; //GP4 set as active low CS 
            uint pcsToDataDly77 = 0;     //time delay from CS to data, quanta of 100us
            uint pdataToDataDly77 = 0;   //time delay from data to data, quanta of 100us
            uint pdataToCsDly77 = 0;     //time delay from data to CS, quanta of 100us
            uint ptxferSize77 = 2;       //TC77 txfer size set to 2 bytes
            byte pspiMd77 = 0;           //SPI mode from connected devices datasheet
            uint csmask77 = 0x10;        //1 represents CS, set GP4 as CS

            //Setup transfer and receive bytes
            byte[] txData77 = new byte[2], rxData77 = new byte[2];
            txData77[0] = 0x00;
            txData77[1] = 0x00;
            rxData77[0] = 0x00;
            rxData77[1] = 0x00;

            //Tx and Rx data from TC77
            error = MCP2210.M_Mcp2210_xferSpiDataEx(deviceHandle[DeviceNumber], txData77, rxData77, ref pbaudRate77, ref ptxferSize77, csmask77, ref pidleCsVal77, ref pactiveCsVal77,
              ref pcsToDataDly77, ref pdataToCsDly77, ref pdataToDataDly77, ref pspiMd77);
            if (error != MCP2210.M_E_SUCCESS)
            {
                MCP2210.M_Mcp2210_Close(deviceHandle[DeviceNumber]);
                LastError.Items.Add(" Transfer error: " + error);
                LastError.SelectedIndex = LastError.Items.Count - 1;
                LastError.ScrollIntoView(LastError.SelectedItem);
            }

            //Setting last three bits to zero as they are don't cares from datasheet
            rxData77[1] = (byte)(rxData77[1] & 0xF8);

            byte[] temper = new byte[1];
            temper[0] = rxData77[1];
            rxData77[1] = rxData77[0];
            rxData77[0] = temper[0];

            //Calculations and display
            temp = BitConverter.ToInt16(rxData77, 0);
            temp /= 128;
            //GUI string display
            Temperature.Text = temp.ToString("0.00");
            //7-Seg display setup
            temp1 = (int)temp;
            DigitTemp = temp1.ToString().ToCharArray();
            if (DigitTemp.Length > 1)
            {
                DigitOneTemp = DigitTemp[0];
                DigitTwoTemp = DigitTemp[1];
            }
            else if (DigitTemp.Length > 0)
            {
                DigitOneTemp = '0';
                DigitTwoTemp = Digit[0];
            }

            if (CharToSeg.ContainsKey(DigitOneTemp) && CharToSeg.ContainsKey(DigitTwoTemp))
            {
                digitOneTemp = (uint)(0x100 | CharToSeg[DigitOneTemp]);
                digitTwoTemp = (uint)(0x080 | CharToSeg[DigitTwoTemp]);
            }
            else
            {
                digitOneTemp = (uint)(0x100 | CharToSeg['0']);
                digitTwoTemp = (uint)(0x080 | CharToSeg['0']);
            }

            //GUI bar display
            if (temp < 15)
            {
                temp = 15;
            }
            else if (temp > 40)
            {
                temp = 40;
            }
            temp = (temp - 15) * 100 / 25;
            TempBar.Value = temp;

            //Forcing the CommandManager to raise the RequerySuggested event
            CommandManager.InvalidateRequerySuggested();
        }

        //Button to send start command to ADC
        private void BADCStart_Click(object sender, RoutedEventArgs e)
        {
            //Checking whether timer is already on
            if (!timerADC)
            {
                //  DispatcherTimer setup
                dispatcherTimerADC = new System.Windows.Threading.DispatcherTimer();
                dispatcherTimerADC.Tick += new EventHandler(dispatcherTimerADC_Tick);
                dispatcherTimerADC.Interval = new TimeSpan(0, 0, 0, 0, 100);
                dispatcherTimerADC.Start();
                timerADC = true;
            }
        }

        //Button to send stop command to ADC
        private void BADCStop_Click(object sender, RoutedEventArgs e)
        {
            //Checking whether timer is already off
            if (timerADC)
            {
                dispatcherTimerADC.Stop();
                timerADC = false;
            }
        }

        //Timer setup and communication for ADC
        private void dispatcherTimerADC_Tick(object sender, EventArgs e)
        {            
            double Vref = 5.00;                 //Reference voltage provided to ADC from circuit
            int size = 2;                       //Size of txfers
            int DeviceNumber = ADC.SelectedIndex;   //Device chosen by user on GUI
            uint baudRate = 1000000;            //Baud rate
            uint idleCsVal = 0x1ff;             //Bit is 1 for clock idle low 
            uint activeCsVal = 0x1df;           //Bit is 0 for CS active low, GP5 set as active low CS 
            uint csToDataDly = 0;               //Time delay from CS to data
            uint dataToDataDly = 0;             //Time delay from data to data
            uint dataToCsDly = 0;               //Time delay from data to CS
            uint txferSize = (uint)(size);      //Xfer size set to size bytes
            byte spiMd = 0;                     //SPI mode from connected devices datasheet
            uint csmask = 0x20;                 //1 represents CS, set GP5 as CS

            //Setup transfer and receive bytes
            byte[] txData1 = new byte[size], rxData1 = new byte[size];
            //ADC does not take input, tx is don't care
            txData1[0] = 0x00;
            txData1[1] = 0x00;

            //Start communication with ADC
            error = MCP2210.M_Mcp2210_xferSpiDataEx(deviceHandle[DeviceNumber], txData1, rxData1, ref baudRate, ref txferSize, csmask, ref idleCsVal, ref activeCsVal,
            ref csToDataDly, ref dataToCsDly, ref dataToDataDly, ref spiMd);

            //Convert 16 bit receive to 10 bit unsigned value
            byte[] tempADC = new byte[1];            
            tempADC[0] = rxData1[1];
            rxData1[1] = rxData1[0];
            rxData1[0] = tempADC[0];
            UInt16 value = (UInt16)(BitConverter.ToInt16(rxData1, 0));
            value = (UInt16)((value & 0x1FF8) >> 3);
            //Convert 10 bit unsigned to 0-Vref double
            double Volt = value * Vref / 1023;
            //Check for error and update status
            if (error != MCP2210.M_E_SUCCESS)
            {
                LastError.Items.Add(error.ToString());
                LastError.Items.Add("Device Disconnected!");
                LastError.SelectedIndex = LastError.Items.Count - 1;
                LastError.ScrollIntoView(LastError.SelectedItem);
            }
            else
            {
                ADCV.Text = Volt.ToString("0.00");
                ADCBar.Value = Volt * 20;
            }

            //Forcing the CommandManager to raise the RequerySuggested event
            CommandManager.InvalidateRequerySuggested();
        }

        //Button to turn on Motor with selected voltage
        private void MOTON_Click(object sender, RoutedEventArgs e)
        {
            int DeviceNumber = Motor.SelectedIndex; //Device chosen by user on GUI
            double Vref = 5.00;                 //Reference voltage provided to Motor DAC by circuit
            int size = 2;                       //Size of txfers
            uint baudRate = 1000000;            //Baud rate
            uint idleCsVal = 0x1ff;             //Bit is 1 for clock idle low 
            uint activeCsVal = 0x1ef;           //Bit is 0 for CS active low, GP4 set as active low CS 
            uint csToDataDly = 0;               //Time delay from CS to data
            uint dataToDataDly = 0;             //Time delay from data to data
            uint dataToCsDly = 0;               //Time delay from data to CS
            uint txferSize = (uint)(size);      //tXfer size set to size bytes
            byte spiMd = 0;                     //SPI mode from connected devices datasheet
            uint csmask = 0x10;                 //1 represents CS, set GP4 as CS

            //Setup transfer and receive bytes
            byte[] txData1 = new byte[size], rxData1 = new byte[size];
            //Read value from DAC textbox and parse it
            double value = Double.Parse(TextBoxMOTOR.Text);
            //Set boundaries for DAC input
            if (value > Vref)
                value = Vref;
            else if (value < 0)
                value = 0;
            //Make 0-5V input into 10 bit transfer for DAC
            int DAC = (int)(value * 1023.0 / Vref);
            txData1[0] = (byte)((DAC & 0x03C0) >> 6);
            txData1[0] = (byte)(txData1[0] | 0x70);
            txData1[1] = (byte)((DAC & 0x3F) << 2);

            //Start communication with DAC
            error = MCP2210.M_Mcp2210_xferSpiDataEx(deviceHandle[DeviceNumber], txData1, rxData1, ref baudRate, ref txferSize, csmask, ref idleCsVal, ref activeCsVal,
            ref csToDataDly, ref dataToCsDly, ref dataToDataDly, ref spiMd);
            //Check for error and update status if required
            if (error != MCP2210.M_E_SUCCESS)
            {
                LastError.Items.Add(error.ToString());
                LastError.Items.Add("Device Disconnected!");
                LastError.SelectedIndex = LastError.Items.Count - 1;
                LastError.ScrollIntoView(LastError.SelectedItem);
            }
        }

        //Button to turn off Motor 
        private void MOTOFF_Click(object sender, RoutedEventArgs e)
        {
            if (timerMotor)
            {
                dispatcherTimerMotor.Stop();
                timerMotor = false;
            }
            int DeviceNumber = Motor.SelectedIndex; //Device chosen by user on GUI
            int size = 2;                       //Size of xfers
            uint baudRate = 1000000;            //Baud rate
            uint idleCsVal = 0x1ff;             //Bit is 1 for clock idle low 
            uint activeCsVal = 0x1ef;           //Bit is 0 for CS active low, GP4 set as active low CS 
            uint csToDataDly = 0;               //Time delay from CS to data
            uint dataToDataDly = 0;             //Time delay from data to data
            uint dataToCsDly = 0;               //Time delay from data to CS
            uint txferSize = (uint)(size);      //tXfer size set to size bytes
            byte spiMd = 0;                     //SPI mode from connected devices datasheet
            uint csmask = 0x10;                 //1 represents CS, set GP4 as CS

            //Setup transfer and receive bytes
            byte[] txData1 = new byte[size], rxData1 = new byte[size];
            txData1[0] = 0x70;
            txData1[1] = 0x00;

            //Start communication with DAC
            error = MCP2210.M_Mcp2210_xferSpiDataEx(deviceHandle[DeviceNumber], txData1, rxData1, ref baudRate, ref txferSize, csmask, ref idleCsVal, ref activeCsVal,
            ref csToDataDly, ref dataToCsDly, ref dataToDataDly, ref spiMd);
            //Check for error and update status if required
            if (error != MCP2210.M_E_SUCCESS)
            {
                LastError.Items.Add(error.ToString());
                LastError.Items.Add("Device Disconnected!");
                LastError.SelectedIndex = LastError.Items.Count - 1;
                LastError.ScrollIntoView(LastError.SelectedItem);
            }
        }

        //Button to setup timer for Motor control using ADC voltage
        private void MotorVolt(object sender, RoutedEventArgs e)
        {
            //Checking whether timer is already on
            if (!timerMotor)
            {
                //  DispatcherTimer setup
                dispatcherTimerMotor = new System.Windows.Threading.DispatcherTimer();
                dispatcherTimerMotor.Tick += new EventHandler(dispatcherTimerMotor_Tick);
                dispatcherTimerMotor.Interval = new TimeSpan(0, 0, 0, 0, 100);
                dispatcherTimerMotor.Start();
                timerMotor = true;
            }
        }

        //Timer setup and communication for Motor control using ADC voltage
        private void dispatcherTimerMotor_Tick(object sender, EventArgs e)
        {

            int DeviceNumber = Motor.SelectedIndex; //Device chosen by user on GUI
            double Vref = 5.00;                 //Reference voltage provided to Motor DAC by circuit
            int size = 2;                       //Size of txfers
            uint baudRate = 1000000;            //Baud rate
            uint idleCsVal = 0x1ff;             //Bit is 1 for clock idle low 
            uint activeCsVal = 0x1ef;           //Bit is 0 for CS active low, GP4 set as active low CS 
            uint csToDataDly = 0;               //Time delay from CS to data
            uint dataToDataDly = 0;             //Time delay from data to data
            uint dataToCsDly = 0;               //Time delay from data to CS
            uint txferSize = (uint)(size);      //tXfer size set to size bytes
            byte spiMd = 0;                     //SPI mode from connected devices datasheet
            uint csmask = 0x10;                 //1 represents CS, set GP4 as CS

            //Setup transfer and receive bytes
            byte[] txData1 = new byte[size], rxData1 = new byte[size];
            //Read value from ADC textbox and parse it
            double value = Double.Parse(ADCV.Text);
            //Set boundaries for DAC input
            if (value > Vref)
                value = Vref;
            else if (value < 0)
                value = 0;
            //Make 0-5V input into 10 bit transfer for DAC
            int DAC = (int)(value * 1023.0 / Vref);
            txData1[0] = (byte)((DAC & 0x03C0) >> 6);
            txData1[0] = (byte)(txData1[0] | 0x70);
            txData1[1] = (byte)((DAC & 0x3F) << 2);

            //Start communication with Motor
            error = MCP2210.M_Mcp2210_xferSpiDataEx(deviceHandle[DeviceNumber], txData1, rxData1, ref baudRate, ref txferSize, csmask, ref idleCsVal, ref activeCsVal,
            ref csToDataDly, ref dataToCsDly, ref dataToDataDly, ref spiMd);
            //Check for error and update status if required
            if (error != MCP2210.M_E_SUCCESS)
            {
                LastError.Items.Add(error.ToString());
                LastError.Items.Add("Device Disconnected!");
                LastError.SelectedIndex = LastError.Items.Count - 1;
                LastError.ScrollIntoView(LastError.SelectedItem);
            }

            //Forcing the CommandManager to raise the RequerySuggested event
            CommandManager.InvalidateRequerySuggested();
        }

        //Button used to modify ADC chosen device
        private void ADCMod_Click(object sender, RoutedEventArgs e)
        {
            //Call modify function
            Modify("ADC");
        }

        //Button used to disconnect ADC chosen device
        private void ADCDC_Click(object sender, RoutedEventArgs e)
        {
            //Call disconnect function
            Disconnect("ADC");
        }

        //Button used to connect ADC chosen device
        private void ADCCON_Click(object sender, RoutedEventArgs e)
        {
            //Call connect function
            Connect("ADC");
        }

        //Button used to modify Motor chosen device
        private void MotorMod_Click(object sender, RoutedEventArgs e)
        {
            //Call modify function
            Modify("Motor");
        }

        //Button used to disconnect Motor chosen device
        private void MotorDC_Click(object sender, RoutedEventArgs e)
        {
            //Call disconnect function
            Disconnect("Motor");
        }

        //Button used to connect Motor chosen device
        private void MotorCON_Click(object sender, RoutedEventArgs e)
        {
            //Call connect function
            Connect("Motor");
        }

        //Button used to modify Temperature chosen device
        private void TempMod_Click(object sender, RoutedEventArgs e)
        {
            //Call modify function
            Modify("Temp");
        }

        //Button used to disconnect Temperature chosen device
        private void TempDC_Click(object sender, RoutedEventArgs e)
        {
            //Call disconnect function
            Disconnect("Temp");
        }

        //Button used to connect Temperature chosen device
        private void TempCON_Click(object sender, RoutedEventArgs e)
        {
            //Call connect function
            Connect("Temp");
        }

        //Button used to modify 7-seg chosen device
        private void SegMod_Click(object sender, RoutedEventArgs e)
        {
            //Call modify function
            Modify("SevenSeg");
        }

        //Button used to disconnect 7seg chosen device
        private void SegDC_Click(object sender, RoutedEventArgs e)
        {
            //Call disconnect function
            Disconnect("SevenSeg");
        }

        //Button used to connect 7seg chosen device
        private void SegCON_Click(object sender, RoutedEventArgs e)
        {
            //Call connect function
            Connect("SevenSeg");
        }

        //Button used to modify DAC chosen device
        private void DACMod_Click(object sender, RoutedEventArgs e)
        {
            //Call modify function
            Modify("DAC");
        }

        //Button used to disconnect DAC chosen device
        private void DACDC_Click(object sender, RoutedEventArgs e)
        {
            //Call disconnect function
            Disconnect("DAC");
        }

        //Button used to connect DAC chosen device
        private void DACCon_Click(object sender, RoutedEventArgs e)
        {
            //Call connect function
            Connect("DAC");
        }

        //Button used to flip Motor card over to second state
        private void BMotor_transition(object sender, RoutedEventArgs e)
        {
            //Checking whether any devices were available, then using the selected ones Name and Serial Number
            if (Convert.ToInt32(DeviceCount.Text) > 0)
            {
                MotorName.Text = Motor.SelectedItem.ToString();
                MotorSN.Text = NametoSerial[MotorName.Text];
            }
        }

        //Button used to flip ADC card over to second state
        private void BADC_transition(object sender, RoutedEventArgs e)
        {
            //Checking whether any devices were available, then using the selected ones Name and Serial Number
            if (Convert.ToInt32(DeviceCount.Text) > 0)
            {
                ADCName.Text = ADC.SelectedItem.ToString();
                ADCSN.Text = NametoSerial[ADCName.Text];
            }
        }

        //Button used to flip Temperature card over to second state
        private void BTemp_transition(object sender, RoutedEventArgs e)
        {
            //Checking whether any devices were available, then using the selected ones Name and Serial Number
            if (Convert.ToInt32(DeviceCount.Text) > 0)
            {
                TempName.Text = Temp.SelectedItem.ToString();
                TempSN.Text = NametoSerial[TempName.Text];
            }
        }

        //Button used to flip DAC card over to second state
        private void BDAC_transition(object sender, RoutedEventArgs e)
        {
            //Checking whether any devices were available, then using the selected ones Name and Serial Number
            if (Convert.ToInt32(DeviceCount.Text) > 0)
            {
                DACName.Text = DacDevice.SelectedItem.ToString();
                DACSN.Text = NametoSerial[DACName.Text];
            }
        }

        //Button used to flip 7seg card over to second state
        private void B7seg_transition(object sender, RoutedEventArgs e)
        {
            //Checking whether any devices were available, then using the selected ones Name and Serial Number
            if (Convert.ToInt32(DeviceCount.Text) > 0)
            {
                SegName.Text = SevenSeg.SelectedItem.ToString();
                SegSN.Text = NametoSerial[SegName.Text];
            }
        }

        //Button to setup timer for Motor control using ADC voltage
        private void TempSegDisplay(object sender, RoutedEventArgs e)
        {
            //Checking whether timer is already on
            if (!timerSeg)
            {
                timerSeg = true;
            }
            //Getting temperature display to show on 7seg
            DigitTemp = temp1.ToString().ToCharArray();
            int DeviceNumber = SevenSeg.SelectedIndex; //Device selected by user on GUI
            //Checking if values entered can be displayed
            if (DigitTemp.Length > 1)
            {
                DigitOneTemp = DigitTemp[0];
                DigitTwoTemp = DigitTemp[1];
            }
            else if (DigitTemp.Length > 0)
            {
                DigitOneTemp = '0';
                DigitTwoTemp = DigitTemp[0];
            }
            else
            {
                DigitOneTemp = '0';
                DigitTwoTemp = '0';
            }

            //converting two digit chars to unsigned int
            digitOneTemp = (uint)(0x100 | CharToSeg[DigitOneTemp]);
            digitTwoTemp = (uint)(0x080 | CharToSeg[DigitTwoTemp]);
            //Declare new background thread
            BackgroundWorker bw = new BackgroundWorker();
            error = MCP2210.M_Mcp2210_SetGpioConfig(deviceHandle[DeviceNumber], 0, GpioPinDes, digitOne,
              0x000, 0x1, 0x0, 0x1);
            //Background thread event specified
            bw.DoWork += new DoWorkEventHandler(
            delegate (object o, DoWorkEventArgs args)
            {
                BackgroundWorker b = o as BackgroundWorker;
                //Do some simple processing until timerSeg is set to false
                while (timerSeg)
                {
                    error = MCP2210.M_Mcp2210_SetGpioPinVal(deviceHandle[DeviceNumber], digitOneTemp, ref check);
                    error = MCP2210.M_Mcp2210_SetGpioPinVal(deviceHandle[DeviceNumber], digitTwoTemp, ref check);
                }
            });
            //Start asynchronous thread
            bw.RunWorkerAsync();
        }

        //Button to turn on 7seg 
        private void SegStart_Click(object sender, RoutedEventArgs e)
        {
            //Checking whether timer is already on
            if (!timerSeg)
            {
                timerSeg = true;
            }
            //Get value entered by user on GUI
            Digit = SegNum.Text.ToCharArray();
            int DeviceNumber = SevenSeg.SelectedIndex;  //Device selected by user on GUI
            //Checking if values entered can be displayed
            if (Digit.Length > 1)
            {
                DigitOne = Digit[0];
                DigitTwo = Digit[1];
            }
            else if (Digit.Length > 0)
            {
                DigitOne = '0';
                DigitTwo = Digit[0];
            }
            else
            {
                DigitOne = '0';
                DigitTwo = '0';
            }

            //converting two digit chars to unsigned int
            digitOne = (uint)(0x100 | CharToSeg[DigitOne]);
            digitTwo = (uint)(0x080 | CharToSeg[DigitTwo]);
            //Declare new background thread
            BackgroundWorker bw = new BackgroundWorker();
            error = MCP2210.M_Mcp2210_SetGpioConfig(deviceHandle[DeviceNumber], 0, GpioPinDes, digitOne,
              0x000, 0x1, 0x0, 0x1);
            //Background thread event specified
            bw.DoWork += new DoWorkEventHandler(
            delegate (object o, DoWorkEventArgs args)
            {
                BackgroundWorker b = o as BackgroundWorker;
                //Do some simple processing until timerSeg is set to false
                while (timerSeg)
                {
                    error = MCP2210.M_Mcp2210_SetGpioPinVal(deviceHandle[DeviceNumber], digitOne, ref check);
                    error = MCP2210.M_Mcp2210_SetGpioPinVal(deviceHandle[DeviceNumber], digitTwo, ref check);
                }
            });
            //Start asynchronous thread
            bw.RunWorkerAsync();
        }

        //Button to turn off 7seg 
        private void SegStop_Click(object sender, RoutedEventArgs e)
        {
            //Checking whether timer is already off
            if (timerSeg)
            {
                timerSeg = false;
            }
        }

        //Disconnect function
        private void Disconnect(string Device)
        {
            int DeviceNumber; //Device number used to select which device will be disconnected
            //Use input string to select Device number
            if (Device == "SevenSeg")
                DeviceNumber = SevenSeg.SelectedIndex;
            else if (Device == "ADC")
                DeviceNumber = ADC.SelectedIndex;
            else if (Device == "Temp")
                DeviceNumber = Temp.SelectedIndex;
            else if (Device == "Motor")
                DeviceNumber = Motor.SelectedIndex;
            else
                DeviceNumber = DacDevice.SelectedIndex;

            //Disconnect command
            MCP2210.M_Mcp2210_Close(deviceHandle[DeviceNumber]);
            //Get error
            error = MCP2210.M_Mcp2210_GetLastError();
            //Update error and connection status
            if (error != MCP2210.M_E_SUCCESS)
            {
                LastError.Items.Add(error.ToString());
                LastError.Items.Add("Device Error!");
                LastError.SelectedIndex = LastError.Items.Count - 1;
                LastError.ScrollIntoView(LastError.SelectedItem);
            }
            else
            {
                LastError.Items.Add(error.ToString());
                LastError.Items.Add("Device Disconnected!");
                LastError.SelectedIndex = LastError.Items.Count - 1;
                LastError.ScrollIntoView(LastError.SelectedItem);
            }
        }

        //Connect function
        private void Connect(string Device)
        {
            int DeviceNumber; //Device number used to select which device will be connected
            string Serial; //Selected device Serial Number
            //Use input string to select Device number and selected device Serial Number
            if (Device == "SevenSeg")
            {
                DeviceNumber = SevenSeg.SelectedIndex;
                Serial = SevenSeg.SelectedItem.ToString();
            }
            else if (Device == "ADC")
            {
                DeviceNumber = ADC.SelectedIndex;
                Serial = ADC.SelectedItem.ToString();
            }
            else if (Device == "Temp")
            {
                DeviceNumber = Temp.SelectedIndex;
                Serial = Temp.SelectedItem.ToString();
            }
            else if (Device == "Motor")
            {
                DeviceNumber = Motor.SelectedIndex;
                Serial = Motor.SelectedItem.ToString();
            }
            else
            {
                DeviceNumber = DacDevice.SelectedIndex;
                Serial = DacDevice.SelectedItem.ToString();
            }

            //Check if selected device serial number is in our stored list
            if (NametoSerial.ContainsKey(Serial))
            {
                //open device
                deviceHandle[DeviceNumber] = MCP2210.M_Mcp2210_OpenBySN(DEFAULT_VID, DEFAULT_PID, NametoSerial[Serial], path);
                //check for any errors
                error = MCP2210.M_Mcp2210_GetLastError();
            }
            //////////////////////////////
            else
            {
                //open device
                deviceHandle[DeviceNumber] = MCP2210.M_Mcp2210_OpenBySN(DEFAULT_VID, DEFAULT_PID, Serial, path);
                //check for any errors
                error = MCP2210.M_Mcp2210_GetLastError();
            }
            //Update error and connection status
            if (error != MCP2210.M_E_SUCCESS)
            {
                LastError.Items.Add(error.ToString());
                LastError.Items.Add("Device cannot be opened!");
                LastError.SelectedIndex = LastError.Items.Count - 1;
                LastError.ScrollIntoView(LastError.SelectedItem);
            }
            else
            {
                LastError.Items.Add(error.ToString());
                LastError.Items.Add("Device Connected!");
                LastError.SelectedIndex = LastError.Items.Count - 1;
                LastError.ScrollIntoView(LastError.SelectedItem);
            }
        }

        //Modify function
        private void Modify(string Device)
        {
            //Check if any devices are connected
            int count = MCP2210.M_Mcp2210_GetConnectedDevCount(DEFAULT_VID, DEFAULT_PID);
            //If any devices are connected open the modify window
            if (count > 0)
            {
                string DeviceString;
                string SN;
                if (Device == "SevenSeg")
                {
                    DeviceString = SevenSeg.SelectedItem.ToString();
                    SN = SegSN.Text;
                }
                else if (Device == "ADC")
                {
                    DeviceString = ADC.SelectedItem.ToString();
                    SN = ADCSN.Text;
                }
                else if (Device == "Temp")
                {
                    DeviceString = Temp.SelectedItem.ToString();
                    SN = TempSN.Text;
                }
                else if (Device == "Motor")
                {
                    DeviceString = Motor.SelectedItem.ToString();
                    SN = MotorSN.Text;
                }
                else
                {
                    DeviceString = DacDevice.SelectedItem.ToString();
                    SN = ADCSN.Text;
                }
                Window1 subWindow = new Window1(SN, DeviceString);
                subWindow.Show();
            }
        }

        //Button to close the program
        private void BClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

    }
}

