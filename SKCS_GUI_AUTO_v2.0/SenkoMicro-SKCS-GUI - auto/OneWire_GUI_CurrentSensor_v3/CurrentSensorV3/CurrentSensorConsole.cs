﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using ADI.DMY2;
using System.Windows.Forms;
using System.Threading;
using System.IO;

namespace CurrentSensorV3
{
    public partial class CurrentSensorConsole : Form
    {
        public CurrentSensorConsole()
        {
            InitializeComponent();
            UserInit();
        }

        public struct ModuleAttribute
        { 
            public double dIQ;
            public double dVoutIPNative;
            public double dVout0ANative;
            public double dVoutIPMiddle;
            public double dVout0AMiddle;
            public double dVoutIPTrimmed;
            public double dVout0ATrimmed;
            public uint iErrorCode;
            public bool bDigitalCommFail;
            public bool bNormalModeFail;
            public bool bReadMarginal;
            public bool bReadSafety;
            public bool bTrimmed;
        }

        public struct BrakeAttribute
        {
            public double targetStartPoint;
            public double targetStopPoint;
            public double StartPoint;
            public double StopPoint;
        }

        public struct ProdcutAttribute
        {
            public uint uProductID;
            public bool bDebug;
            public uint uIP;
            public double dIQp;
            public double dIQn;
            public double dVoutIPPreTrim;
            public double dVoffsetPreTrim;
            public double dVref;
            public double dVtargetOutIP;
            public double dVtargetOffset;
            public double dVipPostGainTrim;
            public double dVoffsetPostGainTrim;
            public uint[] uRegTable;
            public uint uCoarseGainIndex;
            public uint uFineGainIndex;
            public uint uCoarseOffsetIndex;
            public uint uFineOffsetIndex;
            public uint uReturnCode;
        }


        #region Param Definition

        bool bUsbConnected = false;
        bool bAutoTrimTest = true;          //Debug mode, display engineer tab
        //bool bAutoTrimTest = false;       //Release mode, bon't display engineer tab
        //bool bPretrimOrAuto = false;        //For operator, only auto tab
        //bool bPretrimOrAuto = true;         //For engineer, only PreTrim tab
        uint uTabVisibleCode = 1201;
        bool bMRE = false;
        bool bMASK = false;
        bool bSAFEREAD = false;
        bool bUartInit = false;

        bool bDualRelayIpOn = false;

        uint DeviceAddress = 0x73;
        uint SampleRateNum = 1024;
        uint SampleRate = 1000;     //KHz
        string SerialNum = "None";

        /// <summary>
        /// Delay Define
        /// </summary>
        int Delay_Power = 100;      //ms
        int Delay_Sync =10;   //ms
        int Delay_Fuse = 300;       //ms
        //int Delay_Sync = 50;        //ms

        double ADCOffset = 0;
        double AdcOffset
        {
            set
            {
                this.ADCOffset = Math.Round(value, 0);
                //Set three adcofset combobox on the GUI
                //this.txt_IP_EngT.Text = this.ip.ToString("F0");
                //this.txt_AdcOffset_PreT.Text = this.ADCOffset.ToString("F0");
                //this.txt_AdcOffset_AutoT.Text = this.ADCOffset.ToString("F0");
            }
            get { return this.ADCOffset; }
        }

        double VoutIPThreshold = 0.010;
        double ThresholdOfGain = 0.999;
        double RefVoltOffset = -0.007;
        double dCurrentUpLimit = 15;
        double dCurrentDownLimit = 5;

        double Vout_0A = 0;
        double Vout_IP = 0;
        double Mout_0A = 0;
        double Mout_IP = 0;
        double AMPout_0A = 0;
        double AMPout_IP = 0;
        double ip = 20;
        double IP
        {
            set
            {
                this.ip = Math.Round(value,0);
                //Set three ip combobox on the GUI
                this.txt_IP_EngT.Text = this.ip.ToString("F0");
                this.txt_IP_PreT.Text = this.ip.ToString("F0");
                this.txt_IP_AutoT.Text = this.ip.ToString("F0");
            }
            get { return this.ip; }
        }

        string StrIPx_Auto = "15A";
        double selectedCurrent_Auto = 20;   //A
        double targetGain_customer = 100;    //mV/A

        double targetOffset = 2.5;
        double TargetOffset
        {
            get { return this.targetOffset; }
            set 
            {
                this.targetOffset = value;
                //this.txt_VoutOffset_AutoT.Text = (string)this.cmb_Voffset_PreT.SelectedItem;

                if (this.targetOffset == 1.65)
                    saturationVout = 4.9;
                else
                    saturationVout = 4.9;

                //Update trim code table 
                FilledRoughTable_Customer();
                FilledPreciseTable_Customer();
            }
        }
        double saturationVout = 4.90;
        double minimumVoutIP = 1.5;
        double bin2accuracy = 1.4;
        double bin3accuracy = 2;

        double targetVoltage_customer = 2;
        double TargetVoltage_customer
        {
            get { return this.targetVoltage_customer; }
            set
            {
                this.targetVoltage_customer = value;

                //Update GUI
                this.txt_targetvoltage_PreT.Text = this.targetVoltage_customer.ToString();
                //this.txt_TargetGain_PreT.Text = this.targetVoltage_customer.ToString();
                this.txt_TargertVoltage_AutoT.Text = this.targetVoltage_customer.ToString();
            }
        }

        double TargetGain_customer
        {
            get { return this.targetGain_customer; }
            set
            {
                this.targetGain_customer = value;

                //Update GUI
                this.txt_TargetGain_EngT.Text = this.targetGain_customer.ToString();
                this.txt_TargetGain_PreT.Text = this.targetGain_customer.ToString();
                //this.txt_TargetGain_AutoT.Text = this.targetGain_customer.ToString();
            }
        }

        uint reg80Value = 0;
        uint Reg80Value
        {
            get { return this.reg80Value; }
            set
            {
                this.reg80Value = value;
                //Update GUI
                this.txt_reg80_EngT.Text = "0x" + this.reg80Value.ToString("X2");
                this.txt_Reg80_PreT.Text = "0x" + this.reg80Value.ToString("X2");
            }
        }

        uint reg81Value = 0;
        uint Reg81Value
        {
            get { return this.reg81Value; }
            set
            {
                this.reg81Value = value;
                //Update GUI
                this.txt_reg81_EngT.Text = "0x" + this.reg81Value.ToString("X2");
                this.txt_Reg81_PreT.Text = "0x" + this.reg81Value.ToString("X2");
            }
        }

        uint reg82Value = 0;
        uint Reg82Value
        {
            get { return this.reg82Value; }
            set
            {
                this.reg82Value = value;
                //Update GUI
                this.txt_reg82_EngT.Text = "0x" + this.reg82Value.ToString("X2");
                this.txt_Reg82_PreT.Text = "0x" + this.reg82Value.ToString("X2");
            }
        }

        uint reg83Value = 0;
        uint Reg83Value
        {
            get { return this.reg83Value; }
            set
            {
                this.reg83Value = value;
                //Update GUI
                this.txt_reg83_EngT.Text = "0x" + this.reg83Value.ToString("X2");
                this.txt_Reg83_PreT.Text = "0x" + this.reg83Value.ToString("X2");
            }
        }

        uint Reg84Value = 0;
        uint Reg85Value = 0;
        uint Reg86Value = 0;
        uint Reg87Value = 0;

        //uint Reg84Value = 0;

        //Just used for auto trim, will be updated when auto tirm tabe entering and loading config file
        uint[] Reg80ToReg88Backup = new uint[8];

        uint[] tempReadback = new uint[5];

        int moduleTypeindex = 0;
        int ModuleTypeIndex
        {
            set 
            {
                this.moduleTypeindex = value; 
                //Set both combobox on GUI
                this.cmb_Module_EngT.SelectedIndex = this.moduleTypeindex;
                this.cmb_Module_PreT.SelectedIndex = this.moduleTypeindex;
                this.txt_ModuleType_AutoT.Text = (string)this.cmb_Module_EngT.SelectedItem;

                //Set Voffset
                if (this.moduleTypeindex == 2)
                {
                    TargetOffset = 1.65;
                    //saturationVout = 3.25;
                }
                else if (this.moduleTypeindex == 1)
                {
                    TargetOffset = 2.5;
                    //saturationVout = 4.9;
                }
                else
                {
                    //TargetOffset = 2.5;
                    //saturationVout = 4.9;
                }

                //if (this.moduleTypeindex == 2)
                //{
                //    this.cmb_Voffset_PreT.SelectedItem = (object)(this.TargetOffset + "V");
                //    //this.cmb_Voffset_PreT.Enabled = false;
                //}
                //else if (this.moduleTypeindex == 1)
                //{
                //    this.cmb_Voffset_PreT.SelectedItem = (object)(this.TargetOffset + "V");
                //    //this.cmb_Voffset_PreT.Enabled = false;
                //}
                //else
                //{
                //    //this.cmb_Voffset_PreT.Enabled = true;
                //}
            }
            get { return this.moduleTypeindex; }
        }

        int productType = 0;

        int ProductType
        {
            set
            {
                this.productType = value;
                //Set combobox on GUI
                this.cb_ProductSeries_AutoTab.SelectedIndex = this.productType;
                //this.cmb_Module_PreT.SelectedIndex = this.moduleTypeindex;
                //this.cb_ProductSeries_AutoTab.Text = (string)this.cmb_Module_EngT.SelectedItem;
            }
            get { return this.productType; }
        }

        int programMode = 0;
        int ProgramMode
        {
            set
            {
                this.programMode = value;
                this.cmb_ProgramMode_AutoT.SelectedIndex = this.programMode;
            }
            get { return this.programMode; }
        }

        uint ix_forRoughGainCtrl = 15;
        uint Ix_ForRoughGainCtrlBackup = 15;
        uint Ix_ForRoughGainCtrl
        {
            get { return this.ix_forRoughGainCtrl; }
            set
            {
                this.ix_forRoughGainCtrl = value;
                this.txt_ChosenGain_AutoT.Text = RoughTable_Customer[0][ix_forRoughGainCtrl].ToString("F2");
                this.txt_ChosenGain_PreT.Text = RoughTable_Customer[0][ix_forRoughGainCtrl].ToString("F2");
            }
        }

        int ix_forPrecisonGainCtrl = 0;
        int Ix_ForPrecisonGainCtrl
        {
            get { return this.ix_forPrecisonGainCtrl; }
            set { this.ix_forPrecisonGainCtrl = value; }
        }

        int ix_forOffsetATable = 0;
        int Ix_ForOffsetATable
        {
            get { return this.ix_forOffsetATable; }
            set { this.ix_forOffsetATable = value; }
        }

        int ix_forOffsetBTable = 0;
        int Ix_ForOffsetBTable
        {
            get { return this.ix_forOffsetBTable; }
            set { this.ix_forOffsetBTable = value; }
        }

        uint preSetCoareseGainCode = 0;

        double k_slope = 0.5;
        double b_offset = 0;

        BrakeAttribute BkAttri;

        ProdcutAttribute newPart;
        ProdcutAttribute refPart;

        double[][] RoughTable = new double[3][];        //3x16: 0x80,0x81,Rough
        double[][] PreciseTable = new double[2][];      //2x32: 0x80,Precise
        double[][] OffsetTableA = new double[3][];      //3x16: 0x81,0x82,OffsetA
        double[][] OffsetTableB = new double[2][];      //2x16: 0x83,OffsetB

        //Trim code for 2.5V offset
        double[][] RoughTable_Customer = new double[3][];        //3x16: Rough,0x80,0x81
        double[][] PreciseTable_Customer = new double[2][];      //2x32: 0x80,Precise
        double[][] OffsetTableA_Customer = new double[3][];      //3x16: 0x81,0x82,OffsetA
        double[][] OffsetTableB_Customer = new double[2][];      //2x16: 0x83,OffsetB

        double[][] sl620CoarseGainTable = new double[2][];
        double[][] sl620FineGainTable = new double[3][];
        double[][] sl620CoarseOffsetTable = new double[2][];
        double[][] sl620FineOffsetTable = new double[2][];

        //Gain trim code for 1.65V offset
        //double[][] RoughTable_1v65offset = new double[3][];        //3x16: Rough,0x80,0x81
        //double[][] PreciseTable_1v65offset = new double[2][];      //2x32: 0x80,Precise

        uint[] MultiSiteReg0 = new uint[16];
        uint[] MultiSiteReg1 = new uint[16];
        uint[] MultiSiteReg2 = new uint[16];
        uint[] MultiSiteReg3 = new uint[16];
        uint[] MultiSiteReg4 = new uint[16];
        uint[] MultiSiteReg5 = new uint[16];
        uint[] MultiSiteReg6 = new uint[16];
        uint[] MultiSiteReg7 = new uint[16];
        uint[] MultiSiteRoughGainCodeIndex = new uint[16];

        uint[] BrakeReg = new uint[5];                          //Brake usage
        int Ix_OffsetA_Brake = 0;
        int Ix_OffsetB_Brake = 0;
        int Ix_GainRough_Brake = 0;
        int Ix_GainPrecision_Brake = 0;

        enum PRGMRSULT{
            DUT_BIN_1 = 1,
            DUT_BIN_2 = 2,
            DUT_BIN_3 = 3,
            DUT_BIN_4 = 4,
            DUT_BIN_5 = 5,
            DUT_BIN_6 = 6,
            DUT_BIN_NORMAL = 21,
            DUT_BIN_MARGINAL = 22,
            DUT_VOUT_SHORT = 90,
            DUT_CURRENT_HIGH = 91,
            DUT_TRIMMED_SOMEBITS = 92,
            DUT_TRIMMRD_ALREADY = 97,
            DUT_COMM_FAIL = 98,
            DUT_VOUT_SATURATION = 93,
            DUT_LOW_SENSITIVITY = 94,
            DUT_VOUT_LOW = 95,
            DUT_VOUT_VDD = 96,
            DUT_OFFSET_ABN = 99
        }

        #region Bit Operation Mask
        readonly uint bit0_Mask = Convert.ToUInt32(Math.Pow(2, 0));
        readonly uint bit1_Mask = Convert.ToUInt32(Math.Pow(2, 1));
        readonly uint bit2_Mask = Convert.ToUInt32(Math.Pow(2, 2));
        readonly uint bit3_Mask = Convert.ToUInt32(Math.Pow(2, 3));
        readonly uint bit4_Mask = Convert.ToUInt32(Math.Pow(2, 4));
        readonly uint bit5_Mask = Convert.ToUInt32(Math.Pow(2, 5));
        readonly uint bit6_Mask = Convert.ToUInt32(Math.Pow(2, 6));
        readonly uint bit7_Mask = Convert.ToUInt32(Math.Pow(2, 7));

        uint bit_op_mask;
        #endregion Bit Mask

        #endregion

        #region Device Connection
        OneWireInterface oneWrie_device = new OneWireInterface();

        private int WM_DEVICECHANGE = 0x0219;
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == WM_DEVICECHANGE)
            {
                ConnectDevice();
            }
        }

        private void ConnectDevice()
        {
            bool result = false;
            #region One wire
            if(!bUsbConnected)
                result = oneWrie_device.ConnectDevice();

            if (result)
            {
                this.toolStripStatusLabel_Connection.BackColor = Color.YellowGreen;
                this.toolStripStatusLabel_Connection.Text = "Connected";
                btn_GetFW_OneWire_Click(null, null);
                bUsbConnected = true;
            }
            else
            {
                this.toolStripStatusLabel_Connection.BackColor = Color.IndianRed;
                this.toolStripStatusLabel_Connection.Text = "Disconnected";
            }
            #endregion
        }
        #endregion Device Connection

        #region Device Setting
        private decimal pilotwidth_ow_value_backup = 80000;
        private void numUD_pilotwidth_ow_ValueChanged(object sender, EventArgs e)
        {
            this.numUD_pilotwidth_ow_EngT.Value = (decimal)((int)Math.Round((double)this.numUD_pilotwidth_ow_EngT.Value / 20d) * 20);
            if (this.numUD_pilotwidth_ow_EngT.Value % 20 == 0 & this.numUD_pilotwidth_ow_EngT.Value != pilotwidth_ow_value_backup)
            {
                this.pilotwidth_ow_value_backup = this.numUD_pilotwidth_ow_EngT.Value;
                Console.WriteLine("Set pilot width result->{0}", oneWrie_device.SetPilotWidth((uint)this.numUD_pilotwidth_ow_EngT.Value));
            }
        }

        private void num_UD_pulsewidth_ow_ValueChanged(object sender, EventArgs e)
        {
            //this.num_UD_pulsewidth_ow_EngT.Value = (decimal)((int)Math.Round((double)this.num_UD_pulsewidth_ow_EngT.Value / 5d) * 5);
            //this.num_UD_pulsewidth_ow_EngT.Value = (double)this.num_UD_pulsewidth_ow_EngT.Value;
        }

        private void btn_fuse_action_ow_Click(object sender, EventArgs e)
        {
            //bool fuseMasterBit = false;
            

            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_CONFIG_TO_VOUT);
            rbt_signalPathSeting_Config_EngT.Checked = true;

            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITHOUT_CAP);
            //rbt_withCap_Vout.Checked = false;
            rbt_withoutCap_Vout_EngT.Checked = true;
            //rbt_signalPathSeting_Vout.Checked = false;

            //0x03->0x43
            uint _reg_addr = 0x43;
            uint _reg_data = 0x03;
            oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data);

            //0xAA->0x44
            _reg_addr = 0x44;
            _reg_data = 0xAA;
            oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data);
            
            //Console.WriteLine("Fuse write result->{0}", oneWrie_device.FuseClockSwitch((double)this.num_UD_pulsewidth_ow_EngT.Value, (double)this.numUD_pulsedurationtime_ow_EngT.Value));
        }

        private void btn_fuse_clock_ow_EngT_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("Please Change Power To 6V", "Change Power", MessageBoxButtons.OKCancel);
            if (dr == DialogResult.Cancel)
                return;

            Console.WriteLine("Fuse write result->{0}", oneWrie_device.FuseClockSwitch((double)this.num_UD_pulsewidth_ow_EngT.Value, (double)this.numUD_pulsedurationtime_ow_EngT.Value));
        }

        private void btn_flash_onewire_Click(object sender, EventArgs e)
        {
            Console.WriteLine("Flash result->{0}", oneWrie_device.FlashLED());
        }

        private void btn_GetFW_OneWire_Click(object sender, EventArgs e)
        {
            Console.WriteLine("Enter Get FW Interface");
            byte[] info = oneWrie_device.GetFirmwareInfo();

            if (info == null)
                return;

            string fwVersion = "v" + info[1].ToString() + "." + info[0].ToString() + " ";

            char[] dataInfo = new char[12];
            Array.Copy(info, 8, dataInfo, 0, 12);

            char[] timeInfo = new char[8];
            Array.Copy(info, 20, timeInfo, 0, 8);

            SerialNum = ((info[29] << 8) + info[28]).ToString();

            string data = new string(dataInfo);
            string time = "Build @ " + new string(timeInfo);

            this.toolStripStatusLabel_FWInfo.Text = fwVersion + time + " " + data;
            //this.lbl_FW_onewire.Text = "FW Version:" + oneWrie_device.GetFWVersion();
        }

        #endregion Device Setting

        #region Methods

        private void UserInit()
        {
            //Connect device first.
            ConnectDevice();

            //Refresh pilot width
            //Console.WriteLine("Set pilot width result->{0}", oneWrie_device.SetPilotWidth(8000));
            numUD_pilotwidth_ow_ValueChanged(null, null);
            //oneWrie_device.SetPilotWidth(80000);

            //Fill all the tables for internal tab
            FilledRoughTable();
            FilledPreciseTable();
            FilledOffsetTableA();
            FilledOffsetTableB();

            //Fill all the tables for customer tab
            FilledRoughTable_Customer();
            FilledPreciseTable_Customer();
            FilledOffsetTableA_Customer();
            FilledOffsetTableB_Customer();

            Fillsl620CoarseGainTable();
            Fillsl620FineGainTable();

            //FilledRoughTable_1v65offset();
            //FilledPreciseTable_1v65offse();

            //Init combobox
            //1. Engineering
            this.cmb_SensingDirection_EngT.SelectedIndex = 0;
            this.cmb_OffsetOption_EngT.SelectedIndex = 0;
            this.cmb_PolaritySelect_EngT.SelectedIndex = 0;
            this.cmb_Module_EngT.SelectedIndex = 0;
            //2. PreTrim
            this.cmb_SensitivityAdapt_PreT.SelectedIndex = 0;
            //this.cmb_TempCmp_PreT.SelectedIndex = 0;
            //-350ppm
            this.cmb_TempCmp_PreT.SelectedIndex = 1;

            this.cmb_IPRange_PreT.SelectedIndex = 1;
            this.cmb_Module_PreT.SelectedIndex = 0;
            this.cmb_Voffset_PreT.SelectedIndex = 0;
            this.cb_ProductSeries_AutoTab.SelectedIndex = 2;
            this.cmb_ProgramMode_AutoT.SelectedIndex = 0;
            this.cmb_PreTrim_SensorDirection.SelectedIndex = 0;

            this.cb_AutoTab_Retest.SelectedIndex = 0;
            //this.cb_ProductSeries_AutoTab.SelectedIndex = 2;

            this.cb_SelectedDut_AutoTab.SelectedIndex = 0;
            this.cb_V0AOption_AutoTab.SelectedIndex = 0;
            this.cb_iHallOption_AutoTab.SelectedIndex = 0;

            //Serial Num
            this.txt_SerialNum_EngT.Text = SerialNum;
            this.txt_SerialNum_PreT.Text = SerialNum;

            //load config
            //btn_loadconfig_AutoT_Click(null, null);
            //initConfigFile();

            loadLogFile();

            //this.tabControl1.Controls.Remove(BrakeTab);
            //BrakeAttribute BkAttri;
            BkAttri.targetStartPoint = 0;
            BkAttri.targetStopPoint = 0;
            BkAttri.StartPoint = 0;
            BkAttri.StopPoint = 0; 

            //Display Tab
            if (uTabVisibleCode == 1)
            {
                this.tabControl1.Controls.Remove(BrakeTab);
                this.tabControl1.Controls.Remove(EngineeringTab);
                this.tabControl1.Controls.Remove(PriTrimTab);
                DisplayOperateMes("Load config profile success!");
            }
            else if (uTabVisibleCode == 2)
            {
                this.tabControl1.Controls.Remove(BrakeTab);
                this.tabControl1.Controls.Remove(AutoTrimTab);
                this.tabControl1.Controls.Remove(EngineeringTab);
                DisplayOperateMes("Load config profile success!");
            }
            else if (uTabVisibleCode == 3 || uTabVisibleCode == 4)
            {
                //this.tabControl1.Controls.Remove(AutoTrimTab);
                this.tabControl1.Controls.Remove(BrakeTab);
                this.tabControl1.Controls.Remove(EngineeringTab);
                DisplayOperateMes("Load config profile success!");
            }
            else if (uTabVisibleCode == 1201)
            {
                DisplayOperateMes("Load config profile success!");
            }
            else
            {
                this.tabControl1.Controls.Remove(BrakeTab);
                this.tabControl1.Controls.Remove(EngineeringTab);
                this.tabControl1.Controls.Remove(PriTrimTab);
                //DisplayOperateMes("Invalid config profile!", Color.DarkRed);
                //MessageBox.Show("Invalid config profile!");
                //MessageBox.Show("Invalid config profile!", "Change Current", MessageBoxButtons.OKCancel);
            }

            //DisplayOperateMes("AadcOffset = " + AadcOffset.ToString());
            DisplayOperateMes("MRE = "+ bMRE.ToString());
            DisplayOperateMes("MASK = " + bMASK .ToString());
            DisplayOperateMes("SAFETY = " + bSAFEREAD .ToString());
            DisplayOperateMes("<------- " + DateTime.Now.ToString() + " ------->");
        }

        private double AverageVout()
        {
            double result = oneWrie_device.AverageADCSamples(oneWrie_device.ADCSampleTransfer(SampleRate, SampleRateNum));
            Delay(Delay_Sync);
            result += oneWrie_device.AverageADCSamples(oneWrie_device.ADCSampleTransfer(SampleRate, SampleRateNum));
            Delay(Delay_Sync);
            result += oneWrie_device.AverageADCSamples(oneWrie_device.ADCSampleTransfer(SampleRate, SampleRateNum));

            result /= 3d;

            result = RefVoltOffset - AdcOffset/1000d + (result * 5d / 4096d);
            return result;
        }

        private double GetVout()
        {
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);
            Delay(Delay_Sync);
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);
            Delay(Delay_Sync);
            return AverageVout();
        }

        private double GetVref()
        {
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VREF_WITH_CAP);
            Delay(Delay_Sync);
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VREF);
            Delay(Delay_Sync);
            return AverageVout();
        }

        private double AverageVout_Customer(uint sampleNum)
        {
            double result = oneWrie_device.AverageADCSamples(oneWrie_device.ADCSampleTransfer(SampleRate, sampleNum));

            result = RefVoltOffset - AdcOffset/1000d + (result * 5d / 4096d);
            return result;
        }

        private double GetModuleCurrent()
        {
            double result = oneWrie_device.AverageADCSamples(oneWrie_device.ADCSampleTransfer(SampleRate, SampleRateNum));

            result = 1000d * (RefVoltOffset + (result * 5d / 4096d)) / 100d;
            return result;
        }

        private void SaveMultiSiteRegData(uint indexDut)
        {
            MultiSiteReg0[indexDut] = Reg80Value;
            MultiSiteReg1[indexDut] = Reg81Value;
            MultiSiteReg2[indexDut] = Reg82Value;
            MultiSiteReg3[indexDut] = Reg83Value;
        }

        /// <summary>
        /// 根据采集的Vout@0A，Vout@IP计算出Gain
        /// </summary>
        /// <returns>计算出的Gain供查表用,单位mV/A</returns>
        private double GainCalculate()
        {
            double result = 0;

            result = 1000d * ((Vout_IP - Vout_0A) / IP);

            return result;
        }

        /// <summary>
        /// 根据采集的Vout@0A，Vout@IP计算出Gain
        /// </summary>
        /// <returns>计算出的Gain供查表用,单位mV/A</returns>
        private double GainCalculate(double v_0A, double v_ip)
        {
            return 1000d * ((v_ip - v_0A) / IP);
        }

        /// <summary>
        /// 根据第二次计算的IP0计算，公式：2.5/IP0
        /// </summary>
        /// <returns>计算出的Offset供查表用</returns>
        private double OffsetTuningCalc_Customer()
        {
            //return 2.5 / Vout_0A;
            return TargetOffset / Vout_0A;
        }

        private double GainTuningCalc_Customer(double testValue, double targetValue)
        {
            return targetValue / testValue;
        }

        private void FilledRoughTable()
        {
            for (int i = 0; i < RoughTable.Length; i++)
            {
                switch (i)
                {
                    case 0: //Rough
                        RoughTable[i] = new double[]{
                            -87.75,
                            -85.91,
                            -83.76,
                            -81.26,
                            -78.44,
                            -75.19,
                            -71.27,
                            -67.16,
                            -62.28,
                            -56.52,
                            -50.05,
                            -42.45,
                            -33.83,
                            -24.01,
                            -12.47,
                            0.00
                            };
                        break;
                    case 2: //0x81
                        RoughTable[i] = new double[]{
                            1,
                            0,
                            1,
                            0,
                            1,
                            0,
                            1,
                            0,
                            1,
                            0,
                            1,
                            0,
                            1,
                            0,
                            1,
                            0
                        };
                        break;
                    case 1: //0x80
                        RoughTable[i] = new double[]{
                        0xE0,
                        0xE0,
                        0x60,
                        0x60,
                        0xA0,
                        0xA0,
                        0x20,
                        0x20,
                        0xC0,
                        0xC0,
                        0x40,
                        0x40,
                        0x80,
                        0x80,
                        0x0,
                        0x0
                        };
                        break;
                    default:
                        break;
                }
            }
        }

        private void FilledPreciseTable()
        {
            for (int i = 0; i < PreciseTable.Length; i++)
            {
                switch (i)
                {
                    case 0: //Precise
                        PreciseTable[i] = new double[]{
                            0.00,
                            -0.45,
                            -0.90,
                            -1.35,
                            -1.80,
                            -2.25,
                            -2.69,
                            -3.14,
                            -3.59,
                            -4.04,
                            -4.49,
                            -4.94,
                            -5.38,
                            -5.83,
                            -6.28,
                            -6.73,
                            -7.18,
                            -7.63,
                            -8.08,
                            -8.52,
                            -8.97,
                            -9.42,
                            -9.87,
                            -10.32,
                            -10.77,
                            -11.21,
                            -11.66,
                            -12.11,
                            -12.56,
                            -13.01,
                            -13.46,
                            -13.90
                        };
                        break;
                    case 1: //0x80
                        PreciseTable[i] = new double[]{
                            0x0,
                            0x8,
                            0x4,
                            0xC,
                            0x2,
                            0xA,
                            0x6,
                            0xE,
                            0x1,
                            0x9,
                            0x5,
                            0xD,
                            0x3,
                            0xB,
                            0x7,
                            0xF,
                            0x10,
                            0x18,
                            0x14,
                            0x1C,
                            0x12,
                            0x1A,
                            0x16,
                            0x1E,
                            0x11,
                            0x19,
                            0x15,
                            0x1D,
                            0x13,
                            0x1B,
                            0x17,
                            0x1F        
                        };
                        break;
                    default:
                        break;
                }
            }
        }

        private void FilledOffsetTableA()
        {
            for (int i = 0; i < OffsetTableA.Length; i++)
            {
                switch (i)
                {
                    case 0: //Offset
                        OffsetTableA[i] = new double[]{
                            0,
                            -1.08,
                            -2.160,
                            -3.240,
                            -4.320,
                            -5.400,
                            -6.480,
                            -7.560,
                            8.28,
                            7.2450,
                            6.2100,
                            5.1750,
                            4.1400,
                            3.1050,
                            2.0700,
                            1.0350
                            };
                        break;
                    case 1: //0x81
                        OffsetTableA[i] = new double[]{
                            0x0,
                            0x0,
                            0x0,
                            0x0,
                            0x0,
                            0x0,
                            0x0,
                            0x0,
                            0x80,
                            0x80,
                            0x80,
                            0x80,
                            0x80,
                            0x80,
                            0x80,
                            0x80    
                        };
                        break;
                    case 2: //0x82
                        OffsetTableA[i] = new double[]{
                            0x0,
                            0x4,
                            0x2,
                            0x6,
                            0x1,
                            0x5,
                            0x3,
                            0x7,
                            0x0,
                            0x4,
                            0x2,
                            0x6,
                            0x1,
                            0x5,
                            0x3,
                            0x7   
                        };
                        break;
                    default:
                        break;
                }
            }
        }

        private void FilledOffsetTableB()
        {
            for (int i = 0; i < OffsetTableB.Length; i++)
            {
                switch (i)
                {
                    case 0: //Offset
                        OffsetTableB[i] = new double[]{
                            0,
                            -0.29,
                            -0.58,
                            -0.87,
                            -1.16,
                            -1.45,
                            -1.74,
                            -2.03,
                            2.32,
                            2.03,
                            1.74,
                            1.45,
                            1.16,
                            0.87,
                            0.58,
                            0.29   
                        };
                        break;
                    case 1: //0x83
                        OffsetTableB[i] = new double[]{
                            0x0,
                            0x20,
                            0x10,
                            0x30,
                            0x8,
                            0x28,
                            0x18,
                            0x38,
                            0x4,
                            0x24,
                            0x14,
                            0x34,
                            0xC,
                            0x2C,
                            0x1C,
                            0x3C  
                        };
                        break;
                    default:
                        break;
                }
            }
        }

        private void FilledRoughTable_Customer()
        {
            if (TargetOffset != 1.65)
            {
                for (int i = 0; i < RoughTable.Length; i++)
                {
                    switch (i)
                    {
                        case 0: //Rough
                            RoughTable_Customer[i] = new double[]{
                                12.3565,
                                14.1316,
                                16.2615,
                                18.7339,
                                21.5248,
                                24.7433,
                                28.6430,
                                32.7433,
                                37.7594,
                                43.5103,
                                49.9655,
                                57.5622,
                                66.1880,
                                75.9998,
                                87.5380,
                                100.0000
                            };
                            break;
                        case 2: //0x81
                            RoughTable_Customer[i] = new double[]{
                                1,
                                0,
                                1,
                                0,
                                1,
                                0,
                                1,
                                0,
                                1,
                                0,
                                1,
                                0,
                                1,
                                0,
                                1,
                                0
                            };
                            break;
                        case 1: //0x80
                            RoughTable_Customer[i] = new double[]{
                            0xE0,
                            0xE0,
                            0x60,
                            0x60,
                            0xA0,
                            0xA0,
                            0x20,
                            0x20,
                            0xC0,
                            0xC0,
                            0x40,
                            0x40,
                            0x80,
                            0x80,
                            0x0,
                            0x0
                            };
                            break;
                        default:
                            break;
                    }
                }
            }
            else if (TargetOffset == 1.65)
            {
                for (int i = 0; i < RoughTable.Length; i++)
                {
                    switch (i)
                    {
                        case 0: //Rough
                            RoughTable_Customer[i] = new double[]{
                                12.5545,
                                14.4698,
                                16.6670,
                                19.1870,
                                22.0822,
                                25.4006,
                                29.1830,
                                33.5584,
                                38.7607,
                                44.6210,
                                51.2550,
                                58.9272,
                                67.5783,
                                77.3808,
                                88.4811,
                                100.0000

                            };
                            break;
                        case 2: //0x81
                            RoughTable_Customer[i] = new double[]{
                                1,
                                0,
                                1,
                                0,
                                1,
                                0,
                                1,
                                0,
                                1,
                                0,
                                1,
                                0,
                                1,
                                0,
                                1,
                                0
                            };
                            break;
                        case 1: //0x80
                            RoughTable_Customer[i] = new double[]{
                            0xE0,
                            0xE0,
                            0x60,
                            0x60,
                            0xA0,
                            0xA0,
                            0x20,
                            0x20,
                            0xC0,
                            0xC0,
                            0x40,
                            0x40,
                            0x80,
                            0x80,
                            0x0,
                            0x0
                            };
                            break;
                        default:
                            break;
                    }
                }
            }
            else
                DisplayOperateMes("Offset Selection Error!",Color.DarkRed);
        }

        private void FilledPreciseTable_Customer()
        {
            if (TargetOffset != 1.65)
            {
                for (int i = 0; i < PreciseTable.Length; i++)
                {
                    switch (i)
                    {
                        case 0: //Precise
                            PreciseTable_Customer[i] = new double[]{
                            100.0000,
                            99.5107,
                            99.0909,
                            98.6436,
                            98.1915,
                            97.7258,
                            97.2238,
                            96.7933,
                            96.3252,
                            95.9057,
                            95.4774,
                            94.9961,
                            94.5857,
                            94.2226,
                            93.6917,
                            93.2525,
                            92.8312,
                            92.3754,
                            91.8996,
                            91.5016,
                            91.0280,
                            90.5525,
                            90.0948,
                            89.6563,
                            89.2102,
                            88.7558,
                            88.2891,
                            87.8519,
                            87.3960,
                            86.9635,
                            86.4919,
                            86.0669
                        };
                            break;
                        case 1: //0x80
                            PreciseTable_Customer[i] = new double[]{
                            0x0,
                            0x8,
                            0x4,
                            0xC,
                            0x2,
                            0xA,
                            0x6,
                            0xE,
                            0x1,
                            0x9,
                            0x5,
                            0xD,
                            0x3,
                            0xB,
                            0x7,
                            0xF,
                            0x10,
                            0x18,
                            0x14,
                            0x1C,
                            0x12,
                            0x1A,
                            0x16,
                            0x1E,
                            0x11,
                            0x19,
                            0x15,
                            0x1D,
                            0x13,
                            0x1B,
                            0x17,
                            0x1F        
                        };
                            break;
                        default:
                            break;
                    }
                }
            }
            else if (TargetOffset == 1.65)
            {
                for (int i = 0; i < PreciseTable.Length; i++)
                {
                    switch (i)
                    {
                        case 0: //Precise
                            PreciseTable_Customer[i] = new double[]{
                            100.0000,
                            99.5621,
                            99.0883,
                            98.6571,
                            98.2420,
                            97.8018,
                            97.3733,
                            96.9106,
                            96.5204,
                            96.0668,
                            95.6047,
                            95.1692,
                            94.7649,
                            94.3190,
                            93.8573,
                            93.4373,
                            93.0262,
                            92.5910,
                            92.1260,
                            91.7141,
                            91.2899,
                            90.8342,
                            90.4010,
                            89.9252,
                            89.4676,
                            89.0324,
                            88.5949,
                            88.1567,
                            87.6997,
                            87.2675,
                            86.8323,
                            86.3821

                        };
                            break;
                        case 1: //0x80
                            PreciseTable_Customer[i] = new double[]{
                            0x0,
                            0x8,
                            0x4,
                            0xC,
                            0x2,
                            0xA,
                            0x6,
                            0xE,
                            0x1,
                            0x9,
                            0x5,
                            0xD,
                            0x3,
                            0xB,
                            0x7,
                            0xF,
                            0x10,
                            0x18,
                            0x14,
                            0x1C,
                            0x12,
                            0x1A,
                            0x16,
                            0x1E,
                            0x11,
                            0x19,
                            0x15,
                            0x1D,
                            0x13,
                            0x1B,
                            0x17,
                            0x1F       
                        };
                            break;
                        default:
                            break;
                    }
                }
            }
            else
                DisplayOperateMes("Offset Selection Error!", Color.DarkRed);
        }

        private void FilledOffsetTableA_Customer()
        {
            for (int i = 0; i < OffsetTableA_Customer.Length; i++)
            {
                switch (i)
                {
                    case 0: //Offset
                        OffsetTableA_Customer[i] = new double[]{
                            100.00,
                            98.94,
                            97.87,
                            96.78,
                            95.68,
                            94.60,
                            93.50,
                            92.39,
                            108.27,
                            107.27,
                            106.26,
                            105.23,
                            104.20,
                            103.16,
                            102.12,
                            101.07
                        };
                        break;
                    case 1: //0x81
                        OffsetTableA_Customer[i] = new double[]{
                            0x0,
                            0x0,
                            0x0,
                            0x0,
                            0x0,
                            0x0,
                            0x0,
                            0x0,
                            0x80,
                            0x80,
                            0x80,
                            0x80,
                            0x80,
                            0x80,
                            0x80,
                            0x80    
                        };
                        break;
                    case 2: //0x82
                        OffsetTableA_Customer[i] = new double[]{
                            0x0,
                            0x4,
                            0x2,
                            0x6,
                            0x1,
                            0x5,
                            0x3,
                            0x7,
                            0x0,
                            0x4,
                            0x2,
                            0x6,
                            0x1,
                            0x5,
                            0x3,
                            0x7   
                        };
                        break;
                    default:
                        break;
                }
            }
        }

        private void FilledOffsetTableB_Customer()
        {
            for (int i = 0; i < OffsetTableB_Customer.Length; i++)
            {
                switch (i)
                {
                    case 0: //Offset
                        OffsetTableB_Customer[i] = new double[]{
                            100.00,
                            99.72,
                            99.43,
                            99.14,
                            98.85,
                            98.56,
                            98.28,
                            98.00,
                            102.39,
                            102.10,
                            101.80,
                            101.48,
                            101.19,
                            100.89,
                            100.60,
                            100.31
                        };
                        break;
                    case 1: //0x83
                        OffsetTableB_Customer[i] = new double[]{
                            0x0,
                            0x20,
                            0x10,
                            0x30,
                            0x8,
                            0x28,
                            0x18,
                            0x38,
                            0x4,
                            0x24,
                            0x14,
                            0x34,
                            0xC,
                            0x2C,
                            0x1C,
                            0x3C  
                        };
                        break;
                    default:
                        break;
                }
            }
        }

        private void Fillsl620CoarseGainTable()
        {
            for (int i = 0; i < sl620CoarseGainTable.Length; i++)
            {
                switch (i)
                {
                    case 0: //Precise
                        sl620CoarseGainTable[i] = new double[]{
                            100,
                            86.61852399,
                            75.09163521,
                            65.39197987,
                            57.22960775,
                            49.93161552,
                            43.50894469,
                            37.96159527,
                            33.17468133,
                            28.91843099,
                            25.16549045,
                            21.9596258,
                            19.16953882,
                            16.69675584,
                            14.54674763,
                            12.70310192
                        };
                        break;
                    case 1: //0x80
                        sl620CoarseGainTable[i] = new double[]{
                            0,
                            16,
                            32,
                            48,
                            64,
                            80,
                            96,
                            112,
                            128,
                            144,
                            160,
                            176,
                            192,
                            208,
                            224,
                            240
      
                        };
                        break;
                    default:
                        break;
                }
            }
        }

        private void Fillsl620FineGainTable()
        {
            for (int i = 0; i < sl620FineGainTable.Length; i++)
            {
                switch (i)
                {
                    case 0: //Precise
                        sl620FineGainTable[i] = new double[]{
                            100,
                            99.84154737,
                            99.54103377,
                            99.34433395,
                            99.0929953,
                            98.90175937,
                            98.75423451,
                            98.51382363,
                            98.27887663,
                            98.07671293,
                            97.86362146,
                            97.60135504,
                            97.47568572,
                            97.2462026,
                            96.99486395,
                            96.75991695,
                            96.51404218,
                            96.3064146,
                            96.06053983,
                            95.9020872,
                            95.67806797,
                            95.46497651,
                            95.24095727,
                            94.98961862,
                            94.72188832,
                            94.62353841,
                            94.36127199,
                            94.10993334,
                            93.95148071,
                            93.67282264,
                            93.53622555,
                            93.24117583,
                            93.06086766,
                            92.89148727,
                            92.6128292,
                            92.4106655,
                            92.24674899,
                            91.92437985,
                            91.79871052,
                            91.49273303,
                            91.31242487,
                            91.06108622,
                            90.88077806,
                            90.61304775,
                            90.42727571,
                            90.19779259,
                            89.98470113,
                            89.71150694,
                            89.51480712,
                            89.27986012,
                            89.01759371,
                            88.87006884,
                            88.63512184,
                            88.36192766,
                            88.17615561,
                            88.00131133,
                            87.71172549,
                            87.51502568,
                            87.31832587,
                            87.1052344,
                            86.83204021,
                            86.58070156,
                            86.38946563,
                            86.18730193
                        };
                        break;
                    case 1: //0x86
                        sl620FineGainTable[i] = new double[]{
                            0,
                            32,
                            64,
                            96,
                            128,
                            160,
                            192,
                            224,
                            0,
                            32,
                            64,
                            96,
                            128,
                            160,
                            192,
                            224,
                            0,
                            32,
                            64,
                            96,
                            128,
                            160,
                            192,
                            224,
                            0,
                            32,
                            64,
                            96,
                            128,
                            160,
                            192,
                            224,
                            0,
                            32,
                            64,
                            96,
                            128,
                            160,
                            192,
                            224,
                            0,
                            32,
                            64,
                            96,
                            128,
                            160,
                            192,
                            224,
                            0,
                            32,
                            64,
                            96,
                            128,
                            160,
                            192,
                            224,
                            0,
                            32,
                            64,
                            96,
                            128,
                            160,
                            192,
                            224

                        };
                        break;
                    case 2: //0x87
                        sl620FineGainTable[i] = new double[]{
                            0,
                            0,
                            0,
                            0,
                            0,
                            0,
                            0,
                            0,
                            32,
                            32,
                            32,
                            32,
                            32,
                            32,
                            32,
                            32,
                            64,
                            64,
                            64,
                            64,
                            64,
                            64,
                            64,
                            64,
                            96,
                            96,
                            96,
                            96,
                            96,
                            96,
                            96,
                            96,
                            128,
                            128,
                            128,
                            128,
                            128,
                            128,
                            128,
                            128,
                            160,
                            160,
                            160,
                            160,
                            160,
                            160,
                            160,
                            160,
                            192,
                            192,
                            192,
                            192,
                            192,
                            192,
                            192,
                            192,
                            224,
                            224,
                            224,
                            224,
                            224,
                            224,
                            224,
                            224

                        };
                        break;
                    default:
                        break;
                }
            }
        }

        private void Fillsl620CoarseOffsetTable()
        {
            for (int i = 0; i < sl620CoarseOffsetTable.Length; i++)
            {
                switch (i)
                {
                    case 0: //Precise
                        sl620CoarseOffsetTable[i] = new double[]{
                            100,
                            86.61852399,
                            75.09163521,
                            65.39197987,
                            57.22960775,
                            49.93161552,
                            43.50894469,
                            37.96159527,
                            33.17468133,
                            28.91843099,
                            25.16549045,
                            21.9596258,
                            19.16953882,
                            16.69675584,
                            14.54674763,
                            12.70310192
                        };
                        break;
                    case 1: //0x80
                        sl620CoarseOffsetTable[i] = new double[]{
                            0,
                            16,
                            32,
                            48,
                            64,
                            80,
                            96,
                            112,
                            128,
                            144,
                            160,
                            176,
                            192,
                            208,
                            224,
                            240
      
                        };
                        break;
                    default:
                        break;
                }
            }
        }

        private void Fillsl620FineOffsetTable()
        {
            for (int i = 0; i < sl620FineOffsetTable.Length; i++)
            {
                switch (i)
                {
                    case 0: //Precise
                        sl620FineOffsetTable[i] = new double[]{
                            100,
                            86.61852399,
                            75.09163521,
                            65.39197987,
                            57.22960775,
                            49.93161552,
                            43.50894469,
                            37.96159527,
                            33.17468133,
                            28.91843099,
                            25.16549045,
                            21.9596258,
                            19.16953882,
                            16.69675584,
                            14.54674763,
                            12.70310192
                        };
                        break;
                    case 1: //0x80
                        sl620FineOffsetTable[i] = new double[]{
                            0,
                            16,
                            32,
                            48,
                            64,
                            80,
                            96,
                            112,
                            128,
                            144,
                            160,
                            176,
                            192,
                            208,
                            224,
                            240
      
                        };
                        break;
                    default:
                        break;
                }
            }
        }

        //private void FilledRoughTable_1v65offset()
        //{
        //    for (int i = 0; i < RoughTable.Length; i++)
        //    {
        //        switch (i)
        //        {
        //            case 0: //Rough
        //                RoughTable_Customer[i] = new double[]{
        //                    12.216,
        //                    13.862,
        //                    16.147,
        //                    18.348,
        //                    21.401,
        //                    24.653,
        //                    29.975,
        //                    34.020,
        //                    38.312,
        //                    44.412,
        //                    52.052,
        //                    59.549,
        //                    68.440,
        //                    77.870,
        //                    88.259,
        //                    100.000

        //                };
        //                break;
        //            case 2: //0x81
        //                RoughTable_Customer[i] = new double[]{
        //                    1,
        //                    0,
        //                    1,
        //                    0,
        //                    1,
        //                    0,
        //                    1,
        //                    0,
        //                    1,
        //                    0,
        //                    1,
        //                    0,
        //                    1,
        //                    0,
        //                    1,
        //                    0
        //                };
        //                break;
        //            case 1: //0x80
        //                RoughTable_Customer[i] = new double[]{
        //                0xE0,
        //                0xE0,
        //                0x60,
        //                0x60,
        //                0xA0,
        //                0xA0,
        //                0x20,
        //                0x20,
        //                0xC0,
        //                0xC0,
        //                0x40,
        //                0x40,
        //                0x80,
        //                0x80,
        //                0x0,
        //                0x0
        //                };
        //                break;
        //            default:
        //                break;
        //        }
        //    }
        
        //}

        //private void FilledPreciseTable_1v65offse()
        //{
        //    for (int i = 0; i < PreciseTable.Length; i++)
        //    {
        //        switch (i)
        //        {
        //            case 0: //Precise
        //                PreciseTable_Customer[i] = new double[]{
        //                    100,
        //                    100.9133872,
        //                    100.6252553,
        //                    98.38610861,
        //                    97.60862141,
        //                    96.76134507,
        //                    96.72656851,
        //                    96.58239657,
        //                    95.42803881,
        //                    95.39503088,
        //                    94.97995235,
        //                    93.9553052,
        //                    93.75533338,
        //                    93.14090708,
        //                    92.67175348,
        //                    92.1915706,
        //                    91.78149903,
        //                    91.73470358,
        //                    91.59799441,
        //                    91.51023103,
        //                    90.70553856,
        //                    90.51250858,
        //                    90.15082225,
        //                    90.03869214,
        //                    89.96089341,
        //                    89.84972078,
        //                    89.7525681,
        //                    89.3287556,
        //                    88.18534057,
        //                    87.14040232,
        //                    86.86542726,
        //                    85.50573369

        //                };
        //                break;
        //            case 1: //0x80
        //                PreciseTable_Customer[i] = new double[]{
        //                    0x0,
        //                    0x8,
        //                    0x4,
        //                    0x2,
        //                    0x1,
        //                    0xC,
        //                    0xE,
        //                    0x9,
        //                    0x5,
        //                    0x3,
        //                    0x6,
        //                    0xA,
        //                    0xB,
        //                    0x13,
        //                    0x18,
        //                    0x1D,
        //                    0xD ,
        //                    0x15,
        //                    0x1B,
        //                    0x10,
        //                    0x19,
        //                    0x12,
        //                    0x16,
        //                    0x1A,
        //                    0x14,
        //                    0x11,
        //                    0x1C,
        //                    0x17,
        //                    0x7,
        //                    0x1F,
        //                    0x1E,
        //                    0xF
        
        //                };
        //                break;
        //            default:
        //                break;
        //        }
        //    }
        //}

        //Abs(Value) decreased table

        private int LookupRoughGain(double tuningGain, double[][] gainTable)
        {
            if (tuningGain.ToString() == "Infinity")
            {
                return gainTable[0].Length - 1;
            }

            double temp = Math.Abs(tuningGain);
            for (int i = 0; i < gainTable[0].Length; i++)
            {
                if (temp - Math.Abs(gainTable[0][i]) >= 0)
                    return i;
            }
            return gainTable[0].Length - 1;
        }

        //Abs(Value) increased table
        private int LookupPreciseGain(double tuningGain, double[][] gainTable)
        {
            double temp = Math.Abs(tuningGain);
            for (int i = 0; i < gainTable[0].Length; i++)
            {
                if (temp - Math.Abs(gainTable[0][i]) >= 0)
                {
                    if ((i > 0) && (i < gainTable[0].Length - 1))
                    {
                        if (Math.Abs(temp - Math.Abs(gainTable[0][i - 1])) <= Math.Abs(temp - Math.Abs(gainTable[0][i])))
                            return (i - 1);
                        else
                            return i;
                    }
                }
            }
            return 0;
        }

        private int LookupOffset(ref double offset, double[][] offsetTable)
        {
            double temp = offset - offsetTable[0][0];
            int ix = 0;
            for (int i = 1; i < offsetTable[0].Length; i++)
            {
                if (Math.Abs(temp) > Math.Abs(offset - offsetTable[0][i]))
                {
                    temp = offset - offsetTable[0][i];
                    ix = i;
                }
            }
            //offset = temp;
            offset = 100 * offset / offsetTable[0][ix];
            return ix;
        }

        private int LookupOffsetIndex(uint regData, double[][] offsetTable)
        {
            for (int i = 0; i < offsetTable[0].Length; i++)
            {
                if (regData == offsetTable[1][i])
                    return i;
            }

            return 0;
        }

        //Abs(Value) increased table
        private int LookupRoughGain_Customer(double tuningGain, double[][] gainTable)
        {
            if (tuningGain.ToString() == "Infinity")
            {
                return gainTable[0].Length - 1;
            }

            double temp = Math.Abs(tuningGain);
            for (int i = 0; i < gainTable[0].Length; i++)
            {
                if (temp - Math.Abs(gainTable[0][i]) <= 0)
                    return i;
            }
            return gainTable[0].Length - 1;
        }

        private int LookupCoarseGain_SL620(double tuningGain, double[][] gainTable)
        {
            if (tuningGain.ToString() == "Infinity")
            {
                return gainTable[0].Length - 1;
            }

            double temp = Math.Abs(tuningGain);
            if (temp >= 100)
                return 0;

            if (temp <= 12.7)
                return gainTable[0].Length - 1;

            for (int i = 0; i < gainTable[0].Length; i++)
            {
                if (temp - Math.Abs(gainTable[0][i]) > 0)
                    return i-1;
            }
            return gainTable[0].Length - 1;
        }

        //Abs(Value) decreased table
        private int LookupPreciseGain_Customer(double tuningGain, double[][] gainTable)
        {
            double temp = Math.Abs(tuningGain);
            for (int i = 0; i < gainTable[0].Length; i++)
            {
                if (temp - Math.Abs(gainTable[0][i]) >= 0)
                {
                    if ((i > 0) && (i < gainTable[0].Length - 1))
                    {
                        if (Math.Abs(temp - Math.Abs(gainTable[0][i - 1])) <= Math.Abs(temp - Math.Abs(gainTable[0][i])))
                            return (i - 1);
                        else
                            return i;
                    }
                    else
                        return (gainTable[0].Length - 1);
                }
            }
            return 0;
        }

        private int LookupFineGain_SL620(double tuningGain, double[][] gainTable)
        {
            double temp = Math.Abs(tuningGain);
            for (int i = 0; i < gainTable[0].Length; i++)
            {
                if (temp - Math.Abs(gainTable[0][i]) >= 0)
                {
                    if ((i > 0) && (i < gainTable[0].Length - 1))
                    {
                        if (Math.Abs(temp - Math.Abs(gainTable[0][i - 1])) <= Math.Abs(temp - Math.Abs(gainTable[0][i])))
                            return (i - 1);
                        else
                            return i;
                    }
                    else
                        return (gainTable[0].Length - 1);
                }
            }
            return 0;
        }

        private int LookupOffset_Customer(ref double offset, double[][] offsetTable)
        {
            //Offset = 2.5/IP0_Auto
            double temp = offset - offsetTable[0][0];
            int ix = 0;
            for (int i = 1; i < offsetTable[0].Length; i++)
            {
                if (Math.Abs(temp) > Math.Abs(offset - offsetTable[0][i]))
                {
                    temp = offset - offsetTable[0][i];
                    ix = i;
                }
            }

            offset = 100 * offset / offsetTable[0][ix];  //Return (2.5/IP0_Auto)/offsetTable[ix] which will used for next lookup table operation
            return ix;
        }

        public void DisplayOperateMes(string strError, Color fontColor)
        {
            int length = strError.Length;
            int beginIndex = txt_OutputLogInfo.Text.Length;
            txt_OutputLogInfo.AppendText(strError + "\r\n");
            //txt_OutputLogInfo.ForeColor = Color.Chartreuse;
            txt_OutputLogInfo.Select(beginIndex, length);
            txt_OutputLogInfo.SelectionColor = fontColor;
            txt_OutputLogInfo.Select(txt_OutputLogInfo.Text.Length, 0);//.SelectedText = "";
            txt_OutputLogInfo.ScrollToCaret();
            txt_OutputLogInfo.Refresh();
        }

        public void DisplayOperateMes(string strError)
        {
            int length = strError.Length;
            int beginIndex = txt_OutputLogInfo.Text.Length;
            txt_OutputLogInfo.AppendText(strError + "\r\n");
            //txt_OutputLogInfo.ForeColor = Color.Chartreuse;
            txt_OutputLogInfo.Select(beginIndex, length);
            //txt_OutputLogInfo.SelectionColor = fontColor;
            txt_OutputLogInfo.Select(txt_OutputLogInfo.Text.Length, 0);//.SelectedText = "";
            txt_OutputLogInfo.ScrollToCaret();
            txt_OutputLogInfo.Refresh();
        }

        public void DisplayOperateMesClear( )
        {
            txt_OutputLogInfo.Clear();
        }

        private void DisplayAutoTrimOperateMes(string strMes, bool ifSucceeded, int step)
        {
            if (bAutoTrimTest)
            {
                if (step == 0)
                {
                    if (ifSucceeded)
                        DisplayOperateMes("-------------------Automatica Trim Start(Debug Mode)-------------------\r\n");
                    else
                        DisplayOperateMes("-------------------Automatica Trim Finished(Debug Mode)-------------------\r\n");

                    return;
                }

                //DisplayOperateMes("Step " + step + ":");
                strMes = "Step" + step.ToString() + ":" + strMes;
                if (ifSucceeded)
                {
                    strMes += " succeeded!";
                    DisplayOperateMes(strMes);
                }
                else
                {
                    strMes += " Failed!";
                    DisplayOperateMes(strMes, Color.Red);
                }
            }
        }

        private void DisplayAutoTrimOperateMes(string strMes, bool ifSucceeded)
        {
            if (bAutoTrimTest)
            {
                //DisplayOperateMes("Step " + step + ":");
                if (ifSucceeded)
                {
                    strMes += " succeeded!";
                    DisplayOperateMes(strMes);
                }
                else
                {
                    strMes += " Failed!";
                    DisplayOperateMes(strMes, Color.Red);
                }
            }
        }

        private void DisplayAutoTrimOperateMes(string strMes, int step)
        {
            if (bAutoTrimTest)
            {
                strMes = "Step" + step.ToString() + ":" + strMes;
                DisplayOperateMes(strMes);
            }
        }

        private void DisplayAutoTrimOperateMes(string strMes)
        {
            if (bAutoTrimTest)
            {
                DisplayOperateMes(strMes);
            }
        }

        private void DisplayAutoTrimResult(bool ifPass)
        {
            if (ifPass)
            {
                this.txt_Status_AutoTab.ForeColor = Color.DarkGreen;
                this.txt_Status_AutoTab.Text = "PASS!";
            }
            else
            {
                this.txt_Status_AutoTab.ForeColor = Color.Red;
                this.txt_Status_AutoTab.Text = "FAIL!";
            }
        }

        private void DisplayAutoTrimResult( UInt16 errorCode)
        {
            switch ( errorCode & 0x000F )
            {
                case 0x0000:
                    this.txt_Status_AutoTab.ForeColor = Color.DarkGreen;
                    this.txt_Status_AutoTab.Text = "PASS!";
                    break;

                case 0x0001:
                    this.txt_Status_AutoTab.ForeColor = Color.Red;
                    this.txt_Status_AutoTab.Text = "S.N.E";
                    break;

                case 0x0002:
                    this.txt_Status_AutoTab.ForeColor = Color.Red;
                    this.txt_Status_AutoTab.Text = "M.R.E";
                    break;

                case 0x0003:
                    this.txt_Status_AutoTab.ForeColor = Color.Red;
                    this.txt_Status_AutoTab.Text = "O.P.E";
                    break;

                case 0x0004:
                    this.txt_Status_AutoTab.ForeColor = Color.Red;
                    this.txt_Status_AutoTab.Text = "M.T.E";
                    break;

                case 0x0005:
                    this.txt_Status_AutoTab.ForeColor = Color.Red;
                    this.txt_Status_AutoTab.Text = "FAIL!";
                    break;

                default:
                    break;

            
            }
        }

        private void DisplayAutoTrimResult(bool ifPass, UInt16 errorCode,string strResult)
        {
            if (ifPass)
            {
                this.txt_Status_AutoTab.ForeColor = Color.DarkGreen;
                this.txt_Status_AutoTab.Text = "PASS!";

                autoTrimResultIndicator.Clear();
                autoTrimResultIndicator.AppendText( "PASS!\t\t" + strResult);
                autoTrimResultIndicator.Refresh();

            }
            else
            {
                switch (errorCode & 0x000F)
                {
                    case 0x0001:
                        this.txt_Status_AutoTab.ForeColor = Color.Red;
                        this.txt_Status_AutoTab.Text = "S.N.E";

                        autoTrimResultIndicator.Clear();
                        autoTrimResultIndicator.SelectionColor = Color.DarkRed;
                        autoTrimResultIndicator.AppendText("Sentisivity NOT Enough!\t\t"+ strResult);
                        autoTrimResultIndicator.Refresh();
                        break;

                    case 0x0002:
                        this.txt_Status_AutoTab.ForeColor = Color.Red;
                        this.txt_Status_AutoTab.Text = "M.R.E";

                        autoTrimResultIndicator.Clear();
                        autoTrimResultIndicator.SelectionColor = Color.DarkRed;
                        autoTrimResultIndicator.AppendText("Marginal Read Error!\t\t"+ strResult);
                        autoTrimResultIndicator.Refresh();
                        break;

                    case 0x0003:
                        this.txt_Status_AutoTab.ForeColor = Color.Red;
                        this.txt_Status_AutoTab.Text = "O.P.E";

                        autoTrimResultIndicator.Clear();
                        autoTrimResultIndicator.SelectionColor = Color.DarkRed;
                        autoTrimResultIndicator.AppendText("Output Error!\t\t"+ strResult);
                        autoTrimResultIndicator.Refresh();
                        break;

                    case 0x0004:
                        this.txt_Status_AutoTab.ForeColor = Color.Red;
                        this.txt_Status_AutoTab.Text = "M.T.E";

                        autoTrimResultIndicator.Clear();
                        autoTrimResultIndicator.SelectionColor = Color.DarkRed;
                        autoTrimResultIndicator.AppendText("Master Bits Trim Error!\t\t"+ strResult);
                        autoTrimResultIndicator.Refresh();
                        break;

                    case 0x0005:
                        this.txt_Status_AutoTab.ForeColor = Color.Red;
                        this.txt_Status_AutoTab.Text = "H.W.E";

                        autoTrimResultIndicator.Clear();
                        autoTrimResultIndicator.SelectionColor = Color.DarkRed;
                        autoTrimResultIndicator.AppendText("No Hardware!\t\t" + strResult);
                        autoTrimResultIndicator.Refresh();
                        break;

                    case 0x0006:
                        this.txt_Status_AutoTab.ForeColor = Color.Red;
                        this.txt_Status_AutoTab.Text = "I2C.E";

                        autoTrimResultIndicator.Clear();
                        autoTrimResultIndicator.SelectionColor = Color.DarkRed;
                        autoTrimResultIndicator.AppendText("I2C Comunication Error\t\t" + strResult);
                        autoTrimResultIndicator.Refresh();
                        break;

                    case 0x0007:
                        this.txt_Status_AutoTab.ForeColor = Color.Red;
                        this.txt_Status_AutoTab.Text = "O.P.C";

                        autoTrimResultIndicator.Clear();
                        autoTrimResultIndicator.SelectionColor = Color.DarkRed;
                        autoTrimResultIndicator.AppendText("Operation Canceled!\t\t" + strResult);
                        autoTrimResultIndicator.Refresh();
                        break;

                    case 0x0008:
                        this.txt_Status_AutoTab.ForeColor = Color.Red;
                        this.txt_Status_AutoTab.Text = "T.M.E";

                        autoTrimResultIndicator.Clear();
                        autoTrimResultIndicator.SelectionColor = Color.DarkRed;
                        autoTrimResultIndicator.AppendText("Trim Master Bits Again!\t\t" + strResult);
                        autoTrimResultIndicator.Refresh();
                        break;

                    case 0x000F:
                        this.txt_Status_AutoTab.ForeColor = Color.Red;
                        this.txt_Status_AutoTab.Text = "FAIL!";

                        autoTrimResultIndicator.Clear();
                        autoTrimResultIndicator.SelectionColor = Color.DarkRed;
                        autoTrimResultIndicator.AppendText("FAIL!\t\t"+ strResult);
                        autoTrimResultIndicator.Refresh();
                        break;

                    default:
                        break;


                }           
            }
        }

        private void DisplayLogInfo(string strError, Color fontColor)
        {
            int length = strError.Length;
            int beginIndex = txt_OutputLogInfo.Text.Length;
            txt_OutputLogInfo.AppendText(strError + "\r\n");
            txt_OutputLogInfo.Select(beginIndex, length);
            txt_OutputLogInfo.SelectionColor = fontColor;
            txt_OutputLogInfo.Select(txt_OutputLogInfo.Text.Length, 0);//.SelectedText = "";
            txt_OutputLogInfo.ScrollToCaret();
            txt_OutputLogInfo.Refresh();
        }

        private void DisplayLogInfo(string strError)
        {
            int length = strError.Length;
            int beginIndex = txt_OutputLogInfo.Text.Length;
            txt_OutputLogInfo.AppendText(strError + "\r\n");
            txt_OutputLogInfo.Select(beginIndex, length);
            txt_OutputLogInfo.Select(txt_OutputLogInfo.Text.Length, 0);//.SelectedText = "";
            txt_OutputLogInfo.ScrollToCaret();
            txt_OutputLogInfo.Refresh();
        }

        private void MultiSiteDisplayResult(uint[] uResult)
        {
            //bool FF = false;
            autoTrimResultIndicator.Clear();
            autoTrimResultIndicator.SelectionColor = Color.Black;
            autoTrimResultIndicator.AppendText("\r\n");
            autoTrimResultIndicator.AppendText("--00--\t--01--\t--02--\t--03--\t--04--\t--05--\t--06--\t--07--\t--08--\t--09--\t--10--\t--11--\t--12--\t--13--\t--14--\t--15--\r\n\r\n");
            for (uint idut = 0; idut < 16; idut++)
            {
                if ( uResult[idut] < 4 && uResult[idut] >0 )
                {
                    autoTrimResultIndicator.SelectionColor = Color.Green;
                    autoTrimResultIndicator.AppendText("PASS\t");
                }
                else if (uResult[idut] < 7 && uResult[idut] > 3 )
                {
                    if (bMRE)
                    {
                        autoTrimResultIndicator.SelectionColor = Color.Green;
                        autoTrimResultIndicator.AppendText("MRE!\t");
                    }
                    else
                    {
                        autoTrimResultIndicator.SelectionColor = Color.Green;
                        autoTrimResultIndicator.AppendText("PASS\t");
                    }
                }
                else
                {
                    autoTrimResultIndicator.SelectionColor = Color.Red;
                    autoTrimResultIndicator.AppendText("**" + uResult[idut].ToString() + "**\t");
                }
            }
        }

        private void MultiSiteDisplayVout(double[] uResult)
        {
            //bool FF = false;
            autoTrimResultIndicator.Clear();
            autoTrimResultIndicator.SelectionColor = Color.Black;
            autoTrimResultIndicator.AppendText("\r\n");
            autoTrimResultIndicator.AppendText("--00--\t--01--\t--02--\t--03--\t--04--\t--05--\t--06--\t--07--\t--08--\t--09--\t--10--\t--11--\t--12--\t--13--\t--14--\t--15--\r\n\r\n");
            for (uint idut = 0; idut < 16; idut++)
            {
                autoTrimResultIndicator.SelectionColor = Color.Green;
                autoTrimResultIndicator.AppendText( uResult[idut].ToString("F3") + "\t");             
            }
        }

        private void MultiSiteSocketSelect(UInt32 uDut)
        {
            Delay(Delay_Sync);
            if( uDut < 8)
                oneWrie_device.SDPSignalPathGroupSel(OneWireInterface.SPControlCommand.SP_MULTISITTE_GROUP_A);
            else
                oneWrie_device.SDPSignalPathGroupSel(OneWireInterface.SPControlCommand.SP_MULTISITTE_GROUP_B);

            Delay(Delay_Sync);
            oneWrie_device.SDPSignalPathSocketSel(uDut);
            Delay(Delay_Sync);
        }

        private string CreateSingleLogInfo(int index)
        {
            return string.Format("{0}\t{1}\t", "DUT" + index, DateTime.Now.ToString());
        }

        private uint[] ReadBackReg1ToReg4(uint DevAddr)
        {
            uint _dev_addr = DevAddr;

            uint _reg_addr = 0x55;
            uint _reg_data = 0xAA;
            oneWrie_device.I2CWrite_Single(_dev_addr, _reg_addr, _reg_data);

            //Read Back 0x80~0x84
            uint _reg_addr_start = 0x80;
            uint[] _readBack_data = new uint[4];

            if (oneWrie_device.I2CRead_Burst(_dev_addr, _reg_addr_start, 4, _readBack_data) != 0)
            {
                DisplayAutoTrimOperateMes("Burst Read Back failed!");
                return null;
            }
            else
            {
                DisplayAutoTrimOperateMes("Reg1 = 0x" + _readBack_data[0].ToString("X") +
                    "\r\nReg2 = 0x" + _readBack_data[1].ToString("X") +
                    "\r\nReg3 = 0x" + _readBack_data[2].ToString("X") +
                    "\r\nReg4 = 0x" + _readBack_data[3].ToString("X"));
            }

            return _readBack_data;
        }

        private uint[] ReadBackReg1ToReg5(uint DevAddr)
        {
            uint _dev_addr = DevAddr;

            uint _reg_addr = 0x55;
            uint _reg_data = 0xAA;
            oneWrie_device.I2CWrite_Single(_dev_addr, _reg_addr, _reg_data);

            //Read Back 0x80~0x85
            uint _reg_addr_start = 0x80;
            uint[] _readBack_data = new uint[5];

            if (oneWrie_device.I2CRead_Burst(_dev_addr, _reg_addr_start, 5, _readBack_data) != 0)
            {
                DisplayAutoTrimOperateMes("Burst Read Back failed!");
                return null;
            }
            else
            {
                DisplayAutoTrimOperateMes("Reg1 = 0x" + _readBack_data[0].ToString("X") +
                    "\r\nReg2 = 0x" + _readBack_data[1].ToString("X") +
                    "\r\nReg3 = 0x" + _readBack_data[2].ToString("X") +
                    "\r\nReg4 = 0x" + _readBack_data[3].ToString("X") +
                    "\r\nReg5 = 0x" + _readBack_data[4].ToString("X"));
            }

            return _readBack_data;
        }

        private bool CheckReg1ToReg4(uint[] readBackData, uint Reg1, uint Reg2, uint Reg3, uint Reg4)
        {
            if (readBackData == null)
                return false;

            if ((readBackData[0] >= Reg1) &&
                (readBackData[1] >= Reg2) &&
                (readBackData[2] >= Reg3) &&
                (readBackData[3] >= Reg4))
                return true;
            else if((readBackData[0] == Reg1) &&
                    (readBackData[1] == Reg2) &&
                    (readBackData[2] == Reg3) &&
                    (readBackData[3] == Reg4))
                return true;
            else
                return false;
        }

        private bool MarginalCheckReg1ToReg4(uint[] readBackData, uint _dev_addr, double testGain_Auto)
        {
            if (readBackData == null)
                return false;

            #region Setup Marginal Read
            uint _reg_addr = 0x55;
            uint _reg_data = 0xAA;
            oneWrie_device.I2CWrite_Single(_dev_addr, _reg_addr, _reg_data);

            _reg_addr = 0x43;
            _reg_data = 0x0E;

            bool writeResult = oneWrie_device.I2CWrite_Single(_dev_addr, _reg_addr, _reg_data);
            if (writeResult)
                DisplayOperateMes("Marginal Read succeeded!");
            else
                DisplayOperateMes("I2C write failed, Marginal Read Failed!", Color.Red);

            //Delay 50ms
            Thread.Sleep(50);
            DisplayOperateMes("Delay 50ms");

            _reg_addr = 0x43;
            _reg_data = 0x0;

            writeResult = oneWrie_device.I2CWrite_Single(_dev_addr, _reg_addr, _reg_data);
            //Console.WriteLine("I2C write result->{0}", oneWrie_device.I2CWrite_Single(_dev_addr, _reg_addr, _reg_data));
            if (writeResult)
                DisplayOperateMes("Reset Reg0x43 succeeded!");
            else
                DisplayOperateMes("Reset Reg0x43 failed!", Color.Red);
            #endregion Setup Marginal Read

            uint[] _MarginalreadBack_data = new uint[4];
            _MarginalreadBack_data = ReadBackReg1ToReg4(_dev_addr);

            if ((readBackData[0] == _MarginalreadBack_data[0]) &&
                (readBackData[1] == _MarginalreadBack_data[1]) &&
                (readBackData[2] == _MarginalreadBack_data[2]) &&
                (readBackData[3] == _MarginalreadBack_data[3]))
                return true;
            else
            {
                //if (((readBackData[0] ^ _MarginalreadBack_data[0]) & 0x20) == 0x20 )
                //{
                //    return false;
                //}
                //else if (((readBackData[0] ^ _MarginalreadBack_data[0]) & 0x40) == 0x40 && (readBackData[0] & 0x20) == 0x20 )
                //{
                //    return false;
                //}
                //else if (((readBackData[0] ^ _MarginalreadBack_data[0]) & 0x80) == 0x80 && (readBackData[0] & 0x40) == 0x40 && (readBackData[0] & 0x20) == 0x20)
                //{
                //    return false;
                //}
                //else if (((readBackData[1] ^ _MarginalreadBack_data[1]) & 0x80)==0x80 || ((readBackData[2] ^ _MarginalreadBack_data[2]) & 0x01 )== 0x01 )
                //{
                //    return false;
                //}
                //else
                //{
                //    return true;
                //}
                return false;
            }
        }

        private bool FuseClockOn(uint _dev_addr, double fusePulseWidth, double fuseDurationTime)
        {
            //0x03->0x43
            uint _reg_Addr = 0x43;
            uint _reg_Value = 0x03;
            if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
            {
                if (bAutoTrimTest)
                {
                    DisplayAutoTrimOperateMes("I2C Write 1 before Fuse Clock", true);
                }
            }
            else
            {
                return false;
            }

            //Delay(Delay_Operation);

            //0xAA->0x44
            _reg_Addr = 0x44;
            _reg_Value = 0xAA;
            if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
            {
                if (bAutoTrimTest)
                {
                    DisplayAutoTrimOperateMes("I2C Write 2 before Fuse Clock", true);
                }
            }
            else
            {
                return false; ;
            }

            Delay(Delay_Sync);

            //Fuse 
            if (oneWrie_device.FuseClockSwitch(fusePulseWidth, fuseDurationTime))
            {
                if (bAutoTrimTest)
                {
                    DisplayAutoTrimOperateMes("Fuse Clock On", true);
                }
            }
            else
            {
                return false;
            }

            Delay(Delay_Fuse);
            return true;
        }

        private bool FuseClockOn(uint _dev_addr, double fusePulseWidth, double fuseDurationTime, int delayTime , int step)
        {
            //0x03->0x43
            uint _reg_Addr = 0x43;
            uint _reg_Value = 0x03;
            if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
                DisplayAutoTrimOperateMes("I2C Write 1 before Fuse Clock", true, step);
            else
            {
                return false;
            }

            //Delay 50ms
            Thread.Sleep(50);
            DisplayAutoTrimOperateMes("Delay 50ms", step);

            //0xAA->0x44
            _reg_Addr = 0x44;
            _reg_Value = 0xAA;
            if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
                DisplayAutoTrimOperateMes("I2C Write 2 before Fuse Clock", true, step);
            else
            {
                return false; ;
            }

            //Delay 50ms
            Thread.Sleep(delayTime);
            DisplayAutoTrimOperateMes("Delay x00ms", step);

            //Fuse 
            if (oneWrie_device.FuseClockSwitch(fusePulseWidth, fuseDurationTime))
                DisplayAutoTrimOperateMes("Fuse Clock On", true, step);
            else
            {
                return false;
            }

            //Delay 700ms -> changed to 100ms @ 2014-09-04
            Thread.Sleep(100);
            DisplayAutoTrimOperateMes("Delay 100ms", step);
            return true;
        }

        private bool WriteBlankFuseCode(uint _dev_addr, uint _reg1Addr, uint _reg2Addr, uint _reg3Addr, int step)
        {
            uint _reg_Value = 00;

            if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg1Addr, _reg_Value))
                DisplayAutoTrimOperateMes(string.Format("Write 0 to other 3 Regs:No.1"), true, step);
            else
            {
                DisplayAutoTrimResult(false);
                return false;
            }

            if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg2Addr, _reg_Value))
                DisplayAutoTrimOperateMes(string.Format("Write 0 to other 3 Regs:No.2"), true, step);
            else
            {
                DisplayAutoTrimResult(false);
                return false;
            }

            if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg3Addr, _reg_Value))
                DisplayAutoTrimOperateMes(string.Format("Write 0 to other 3 Regs:No.3"), true, step);
            else
            {
                DisplayAutoTrimResult(false);
                return false;
            }

            return true;
        }

        private bool WriteMasterBit(uint _dev_addr, int step)
        {
            if (!WriteBlankFuseCode(_dev_addr, 0x80, 0x81, 0x82, step))
                return false;
            //Reg83 <-- 0x0
            uint _reg_Addr = 0x83;
            uint _reg_Value = 0x0;
            if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
                DisplayAutoTrimOperateMes("Write 0 to Reg4", true, step);
            else
            {
                DisplayAutoTrimResult(false);
                return false;
            }

            //Reg84, Fuse with master bit
            _reg_Addr = 0x84;
            _reg_Value = 0x07;

            if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
                DisplayAutoTrimOperateMes("Write Reg5(0x" + _reg_Value.ToString("X") + ")", true, step);
            else
            {
                DisplayAutoTrimResult(false);
                return false;
            }
            return true;
        }

        private bool WriteMasterBit0(uint _dev_addr, int step)
        {
            if (!WriteBlankFuseCode(_dev_addr, 0x80, 0x81, 0x82, step))
                return false;
            //Reg83 <-- 0x0
            uint _reg_Addr = 0x83;
            uint _reg_Value = 0x0;
            if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
                DisplayAutoTrimOperateMes("Write 0 to Reg4", true, step);
            else
            {
                DisplayAutoTrimResult(false);
                return false;
            }

            //Reg84, Fuse with master bit
            _reg_Addr = 0x84;
            _reg_Value = 0x01;

            if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
                DisplayAutoTrimOperateMes("Write Reg5(0x" + _reg_Value.ToString("X") + ")", true, step);
            else
            {
                DisplayAutoTrimResult(false);
                return false;
            }
            return true;
        }

        private bool WriteMasterBit1(uint _dev_addr, int step)
        {
            if (!WriteBlankFuseCode(_dev_addr, 0x80, 0x81, 0x82, step))
                return false;
            //Reg83 <-- 0x0
            uint _reg_Addr = 0x83;
            uint _reg_Value = 0x0;
            if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
                DisplayAutoTrimOperateMes("Write 0 to Reg4", true, step);
            else
            {
                DisplayAutoTrimResult(false);
                return false;
            }

            //Reg84, Fuse with master bit
            _reg_Addr = 0x84;
            _reg_Value = 0x02;

            if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
                DisplayAutoTrimOperateMes("Write Reg5(0x" + _reg_Value.ToString("X") + ")", true, step);
            else
            {
                DisplayAutoTrimResult(false);
                return false;
            }
            return true;
        }

        private bool ResetReg43And44(uint _dev_addr, int step)
        {
            //0x00->0x43
            uint _reg_Addr = 0x43;
            uint _reg_Value = 0x0;
            if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
                DisplayAutoTrimOperateMes("Reset Reg0x43 before new bit Fuse", true, step);
            else
            {
                return false;
            }

            //Delay 50ms
            Thread.Sleep(50);
            DisplayAutoTrimOperateMes("Delay 50ms", step);

            //0xAA->0x44
            _reg_Addr = 0x44;
            _reg_Value = 0x0;
            if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
                DisplayAutoTrimOperateMes("Reset Reg0x44 before new bit Fuse", true, step);
            else
            {
                return false;
            }

            //Delay 50ms
            Thread.Sleep(50);
            DisplayAutoTrimOperateMes("Delay 50ms", step);
            return true;
        }

        private void EnterNomalMode()
        {
            Delay(Delay_Sync);
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITHOUT_CAP);
            //rbt_signalPathSeting_Config_EngT.Checked = true;
            //Thread.Sleep(100);
            Delay(Delay_Sync);

            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_CONFIG_TO_VOUT);
            //rbt_signalPathSeting_Config_EngT.Checked = true;
            //Thread.Sleep(100);
            Delay(Delay_Sync);

            uint _reg_addr = 0x55;
            uint _reg_data = 0xAA;
            oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data);

            Delay(Delay_Sync);

            _reg_addr = 0x42;

            //if (ProductType == 0 || ProductType == 1 )
            //    _reg_data = 0x04;
            //else
                _reg_data = 0x02;

            bool writeResult = oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data);
            //Console.WriteLine("I2C write result->{0}", oneWrie_device.I2CWrite_Single(_dev_addr, _reg_addr, _reg_data));
            if (!writeResult)
                DisplayOperateMes("I2C write failed, Enter Normal Mode Failed!", Color.Red);

            //Thread.Sleep(100);
            Delay(Delay_Sync);
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);

            Delay(Delay_Sync);
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);
            //rbt_signalPathSeting_AIn_EngT.Checked = true;

            //Delay(Delay_Sync);
            //rbt_withCap_Vout_EngT.Checked = true;
        }

        private void EnterTestMode()
        {
            Delay(Delay_Sync);
            //set pilot firstly
            numUD_pilotwidth_ow_ValueChanged(null, null);
            Delay(Delay_Sync);

            //set CONFIG without cap
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITHOUT_CAP);
            Delay(Delay_Sync);
            //set CONFIG to VOUT
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_CONFIG_TO_VOUT);
            Delay(Delay_Sync);
            //Enter test mode
            uint _reg_addr = 0x55;
            uint _reg_data = 0xAA;
            if (oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data))
            {
                if (bAutoTrimTest)
                {
                    DisplayOperateMes("Enter test mode succeeded!");
                }
            }
            else
                DisplayOperateMes("Enter test mode failed!");
        }

        private bool RegisterWrite(int wrNum, uint[] data)
        {
            Delay(Delay_Sync);
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_CONFIG_TO_VOUT);
            Delay(Delay_Sync);
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITHOUT_CAP);
            //rbt_signalPathSeting_Config_EngT.Checked = true;
            Delay(Delay_Sync);
            oneWrie_device.I2CWrite_Single(this.DeviceAddress, 0x55, 0xAA);

            bool rt = false;
            if (data.Length < wrNum * 2)
                return false;

            //if (bAutoTrimTest)
            //    DisplayOperateMes("Write In Data is:");

            for (int ix = 0; ix < wrNum; ix++)
            {
                rt = oneWrie_device.I2CWrite_Single(this.DeviceAddress, data[ix * 2], data[ix * 2 + 1]);
            }

            return rt;
        }

        private double CalcTargetXFromDetectiveY(double y)
        {
            /* y = k*x + b -> x = (y - b) / k */
            return (y - b_offset) / k_slope;
        }

        /// <summary>
        /// Use Y = kX +b to calculate the real vout X, and modify the index of precison table to
        /// find the best gain code. 
        /// ** Enter Current is 0A.
        /// ** Exit Current is also 0A.
        /// </summary>
        /// <returns></returns>
        private bool GainCodeCalcWithLoop()
        {
            double vout_0A_Convert;
            double vout_IP_Convert;
            double target_Gain1 = 0; //new
            double target_Gain2 = 0; //older
            bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask | bit3_Mask | bit4_Mask;
            /* 1. Write Reg0x80 and enter normal mode */
            EnterTestMode();
            RegisterWrite(1, new uint[2] { 0x80, Reg80Value });
            EnterNomalMode();

            /* 2.Get Vout@0A and Vout@IP */
            Vout_0A = AverageVout();
            DialogResult dr = MessageBox.Show(String.Format("Please Change Current To {0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
            if (dr == DialogResult.Cancel)
            {
                DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                return false;
            }
            Vout_IP = AverageVout();

            vout_0A_Convert = CalcTargetXFromDetectiveY(Vout_0A);
            vout_IP_Convert = CalcTargetXFromDetectiveY(Vout_IP);
            target_Gain1 = GainCalculate(vout_0A_Convert, vout_IP_Convert);
            target_Gain2 = target_Gain1;

            if (target_Gain1 == TargetGain_customer)
            {
                /* make sure exit current is 0A */
                dr = MessageBox.Show(String.Format("Please Change Current To {0}A", 0), "Change Current", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    return false;
                }
                return true;
            }

            // if (testGain > targetGain) then index++
            bool IncreaseOrDecrease = (target_Gain1 > TargetGain_customer) ? true : false;

            while (true)
            {
                /* get the right value, will record the Ix_ForPrecisonGainCtrl and break loop */
                if ((target_Gain1 - TargetGain_customer) * (target_Gain2 - TargetGain_customer) <= 0)
                {
                    /* Judge which target gain is the best */
                    if (Math.Abs(target_Gain1 - TargetGain_customer) <= Math.Abs(target_Gain2 - TargetGain_customer)) //The new value is needed
                    {
                        break;
                    }
                    else // Back to older gain
                    {
                        /* Increase/decrease the Ix_ForPrecisonGainCtrl; update reg80; Get Vaout*/
                        if (!IncreaseOrDecrease)
                        {
                            if (Ix_ForPrecisonGainCtrl < 15)
                                Ix_ForPrecisonGainCtrl++;
                            else
                                break;
                        }
                        else
                        {
                            if (Ix_ForPrecisonGainCtrl > 0)
                                Ix_ForPrecisonGainCtrl--;
                            else
                                break;
                        }

                        /* 1. Write Reg0x80 and enter normal mode */
                        Reg80Value &= ~bit_op_mask;
                        Reg80Value |= Convert.ToUInt32(PreciseTable[1][Ix_ForPrecisonGainCtrl]);
                        EnterTestMode();
                        RegisterWrite(1, new uint[2] { 0x80, Reg80Value });
                        EnterNomalMode();
                        /* 2.Get Vout@IP and Vout@0A */
                        Vout_IP = AverageVout();
                        dr = MessageBox.Show(String.Format("Please Change Current To {0}A", 0), "Change Current", MessageBoxButtons.OKCancel);
                        if (dr == DialogResult.Cancel)
                        {
                            DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                            return false;
                        }
                        Vout_0A = AverageVout();
                        return true;
                    }
                }
                else
                {
                    /* Increase/decrease the Ix_ForPrecisonGainCtrl; update reg80; Get Vaout*/
                    if (IncreaseOrDecrease)
                    {
                        if (Ix_ForPrecisonGainCtrl < 15)
                            Ix_ForPrecisonGainCtrl++;
                        else
                            break;
                    }
                    else
                    {
                        if (Ix_ForPrecisonGainCtrl > 0)
                            Ix_ForPrecisonGainCtrl--;
                        else
                            break;
                    }

                    /* 1. Write Reg0x80 and enter normal mode */
                    Reg80Value &= ~bit_op_mask;
                    Reg80Value |= Convert.ToUInt32(PreciseTable[1][Ix_ForPrecisonGainCtrl]);
                    EnterTestMode();
                    RegisterWrite(1, new uint[2] { 0x80, Reg80Value });
                    EnterNomalMode();
                    /* 2.Get Vout@IP and Vout@0A */
                    Vout_IP = AverageVout();
                    dr = MessageBox.Show(String.Format("Please Change Current To {0}A", 0), "Change Current", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                    {
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        return false;
                    }
                    Vout_0A = AverageVout();

                    vout_0A_Convert = CalcTargetXFromDetectiveY(Vout_0A);
                    vout_IP_Convert = CalcTargetXFromDetectiveY(Vout_IP);
                    target_Gain2 = target_Gain1;    //backup history gain
                    target_Gain1 = GainCalculate(vout_0A_Convert, vout_IP_Convert);
                    dr = MessageBox.Show(String.Format("Please Change Current To {0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                    {
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        return false;
                    }
                }
            }

            /* make sure exit current is 0A */
            dr = MessageBox.Show(String.Format("Please Change Current To {0}A", 0), "Change Current", MessageBoxButtons.OKCancel);
            if (dr == DialogResult.Cancel)
            {
                DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                return false;
            }
            return true;
        }

        private bool OffsetCalcWithLoop()
        {
            /* 如果小与2.5那么就从最后一行往上索引到102.40%(ix: 15 -> 8),如果大于2.5那么就从第一行向下索引到97.97%(ix:0 -> 7) */
            double delta_offset1 = 0; //new
            double delta_offset2 = 0; //older
            bit_op_mask = bit2_Mask | bit3_Mask | bit4_Mask | bit5_Mask;
            /* 1. Write Reg0x81, 0x82, 0x83 and enter normal mode */
            EnterTestMode();
            RegisterWrite(3, new uint[6] { 0x81, Reg81Value, 0x82, Reg82Value, 0x83, Reg83Value});
            EnterNomalMode();

            /* 2.Get Vout@0A */
            Vout_0A = AverageVout();

            if (Vout_0A == b_offset)
            {
                return true;
            }

            delta_offset1 = Vout_0A - b_offset;
            delta_offset2 = delta_offset1;
            // if (Vout_0A > b_offset) then index++ ,else index--
            bool IncreaseOrDecrease = (delta_offset1 > 0) ? true : false;
            Ix_ForOffsetBTable = IncreaseOrDecrease ? 0 : 15;
            
            while(true)
            {
                /* get the right offset code */
                if (delta_offset1 * delta_offset2 <= 0)
                {
                    /* the latest one is the right code, do nothing then */
                    if (Math.Abs(delta_offset1) <= Math.Abs(delta_offset2))
                    {
                        break;
                    }
                    /* Back to older one */
                    else
                    {
                        if (!IncreaseOrDecrease)
                        {
                            if (Ix_ForOffsetBTable > 0)
                                Ix_ForOffsetBTable--;
                            else
                                break;
                        }
                        else
                        {
                            if (Ix_ForOffsetBTable < 15)
                                Ix_ForOffsetBTable++;
                            else
                                break;
                        }

                        Reg83Value &= ~bit_op_mask;
                        Reg83Value |= Convert.ToUInt32(OffsetTableB_Customer[1][Ix_ForOffsetBTable]);
                        /* 1. Write Reg0x83 and enter normal mode */
                        EnterTestMode();
                        RegisterWrite(1, new uint[2] { 0x83, Reg83Value });
                        EnterNomalMode();
                        /* 2.Get Vout@0A */
                        Vout_0A = AverageVout();
                        break;
                    }
                }
                /* Increase/decrease the Ix_ForPrecisonGainCtrl; update reg80; Get Vaout*/
                else
                {
                    if (IncreaseOrDecrease)
                    {
                        if (Ix_ForOffsetBTable < 7)
                            Ix_ForOffsetBTable++;
                        else
                            break;
                    }
                    else
                    {
                        if (Ix_ForOffsetBTable > 8)
                            Ix_ForOffsetBTable--;
                        else
                            break;
                    }
                    Reg83Value &= ~bit_op_mask;
                    Reg83Value |= Convert.ToUInt32(OffsetTableB_Customer[1][Ix_ForOffsetBTable]);
                    /* 1. Write Reg0x83 and enter normal mode */
                    EnterTestMode();
                    RegisterWrite(1, new uint[2] { 0x83, Reg83Value });
                    EnterNomalMode();
                    /* 2.Get Vout@0A */
                    Vout_0A = AverageVout();
                    delta_offset2 = delta_offset1;
                    delta_offset1 = Vout_0A - b_offset;
                }
            }

            return true;
        }

        private void RePower()
        {
            Delay(Delay_Sync);
            //1. Power Off
            PowerOff();

            Delay(Delay_Power);

            //2. Power On
            PowerOn();
        }

        private void PowerOff()
        {
            if (!oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_POWER_OFF))
            {
                DisplayOperateMes("Power off failed!");
                return;
            }
        }

        private void PowerOn()
        {
            if (oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_POWER_ON))
            {
                if (bAutoTrimTest)
                {
                    //DisplayOperateMes("Power on succeeded!");
                }
            }
            else
                DisplayOperateMes("Power on failed!");
        }

        private void Delay(int time)
        {
            Thread.Sleep(time);
            if (newPart.bDebug)
                DisplayOperateMes("Delay: " + time.ToString() + "ms");
        }

        private void StoreRegValue()
        {
            Reg80ToReg88Backup[0] = Reg80Value;
            Reg80ToReg88Backup[1] = Reg81Value;
            Reg80ToReg88Backup[2] = Reg82Value;
            Reg80ToReg88Backup[3] = reg83Value;
            Reg80ToReg88Backup[4] = Reg84Value;
            Reg80ToReg88Backup[5] = Reg85Value;
            Reg80ToReg88Backup[6] = Reg86Value;
            Reg80ToReg88Backup[7] = Reg87Value;
            Ix_ForRoughGainCtrlBackup = Ix_ForRoughGainCtrl;

            
        }

        private void RestoreRegValue()
        {
            Reg80Value = Reg80ToReg88Backup[0];
            Reg81Value = Reg80ToReg88Backup[1];
            Reg82Value = Reg80ToReg88Backup[2];
            Reg83Value = Reg80ToReg88Backup[3];
            Reg84Value = Reg80ToReg88Backup[4];
            Reg85Value = Reg80ToReg88Backup[5];
            Reg86Value = Reg80ToReg88Backup[6];
            Reg87Value = Reg80ToReg88Backup[7];
            Ix_ForRoughGainCtrl = Ix_ForRoughGainCtrlBackup;
        }

        private void MarginalReadPreset()
        {
            EnterTestMode();

            uint _reg_addr = 0x43;
            uint _reg_data = 0x06;
            bool writeResult = oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data);
            if (!writeResult)
            {
                DisplayOperateMes("I2C write failed, Marginal Read Failed!", Color.Red);
                return;
            }

            Delay(Delay_Sync);

            _reg_addr = 0x43;
            _reg_data = 0x0E;
            writeResult = oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data);
            if (writeResult)
            {
                if (bAutoTrimTest)
                {
                    DisplayOperateMes("Marginal Read succeeded!");
                }
            }
            else
            {
                DisplayOperateMes("I2C write failed, Marginal Read Failed!", Color.Red);
                return;
            }

            Delay(Delay_Sync);

            _reg_addr = 0x43;
            _reg_data = 0x0;
            writeResult = oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data);
            if (writeResult)
            {
                if (bAutoTrimTest)
                {
                    DisplayOperateMes("Marginal Read Setup succeeded!");
                }
            }
            else
                DisplayOperateMes("Marginal Read Setup failed!", Color.Red);
        }

        private void ReloadPreset()
        {
            EnterTestMode();

            uint _reg_addr = 0x43;
            uint _reg_data = 0x0B;
            bool writeResult = oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data);
            if (writeResult)
            {
                if (bAutoTrimTest)
                {
                    DisplayOperateMes("Reload succeeded!");
                }
            }
            else
            {
                DisplayOperateMes("I2C write failed, Relaod Failed!", Color.Red);
                return;
            }

            Delay(Delay_Sync);

            _reg_addr = 0x43;
            _reg_data = 0x0;
            writeResult = oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data);
            if (writeResult)
            {
                if (bAutoTrimTest)
                {
                    DisplayOperateMes("Reload Setup succeeded!");
                }
            }
            else
                DisplayOperateMes("Reload Setup failed!", Color.Red);
        
        }

        private void SafetyReadPreset()
        {
            EnterTestMode();

            uint _reg_addr = 0x84;
            uint _reg_data = 0xC0;
            bool writeResult = oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data);
            if (!writeResult)
            {
                DisplayOperateMes("1st I2C write failed, Safety Read Failed!", Color.Red);
                return;
            }

            _reg_addr = 0x84;
            _reg_data = 0x00;
            writeResult = oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data);
            if (!writeResult)
            {
                DisplayOperateMes("1st I2C write failed, Safety Read Failed!", Color.Red);
                return;
            }

            //Delay(Delay_Operation);

            _reg_addr = 0x43;
            _reg_data = 0x06;
            writeResult = oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data);
            if (!writeResult)
            {
                DisplayOperateMes("2nd I2C write failed, Safety Read Failed!", Color.Red);
                return;
            }

            //Delay(Delay_Operation);

            _reg_addr = 0x43;
            _reg_data = 0x0E;
            writeResult = oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data);
            if (!writeResult)
            {
                DisplayOperateMes("3rd I2C write failed, Safety Read Failed!", Color.Red);
                return;
            }

            //Delay(Delay_Operation); //delay 300ms

            _reg_addr = 0x43;
            _reg_data = 0x0;
            writeResult = oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data);
            if (writeResult)
            {
                if (bAutoTrimTest)
                {
                    DisplayOperateMes("Reset Reg0x43 succeeded!");
                }
            }
            else
            {
                DisplayOperateMes("Reset Reg0x43 failed!", Color.Red);
                return;
            }

            Delay(Delay_Sync);    //delay 300ms

            //_reg_addr = 0x84;
            //_reg_data = 0x0;
            //writeResult = oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data);
            //if (writeResult)
            //{
            //    if (bAutoTrimTest)
            //    {
            //        DisplayOperateMes("Safety Read Setup succeeded!\r\n");
            //    }
            //}
            //else
            //    DisplayOperateMes("Safety Read Setup failed!\r\n", Color.Red);
        }

        private void SafetyHighReadPreset()
        {
            EnterTestMode();

            uint _reg_addr = 0x84;
            uint _reg_data = 0xC0;
            bool writeResult = oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data);
            if (!writeResult)
            {
                DisplayOperateMes("1st I2C write failed, Safety Read Failed!", Color.Red);
                return;
            }

            _reg_addr = 0x84;
            _reg_data = 0x00;
            writeResult = oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data);
            if (!writeResult)
            {
                DisplayOperateMes("1st I2C write failed, Safety Read Failed!", Color.Red);
                return;
            }

            //Delay(Delay_Operation);

            _reg_addr = 0x43;
            _reg_data = 0x06;
            writeResult = oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data);
            if (!writeResult)
            {
                DisplayOperateMes("2nd I2C write failed, Safety Read Failed!", Color.Red);
                return;
            }

            //Delay(Delay_Operation);

            _reg_addr = 0x43;
            _reg_data = 0x03;
            writeResult = oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data);
            if (!writeResult)
            {
                DisplayOperateMes("3rd I2C write failed, Safety Read Failed!", Color.Red);
                return;
            }

            _reg_addr = 0x43;
            _reg_data = 0x0B;
            writeResult = oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data);
            if (!writeResult)
            {
                DisplayOperateMes("3rd I2C write failed, Safety Read Failed!", Color.Red);
                return;
            }

            //Delay(Delay_Operation); //delay 300ms

            _reg_addr = 0x43;
            _reg_data = 0x0;
            writeResult = oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data);
            if (writeResult)
            {
                if (bAutoTrimTest)
                {
                    DisplayOperateMes("Reset Reg0x43 succeeded!");
                }
            }
            else
            {
                DisplayOperateMes("Reset Reg0x43 failed!", Color.Red);
                return;
            }

            Delay(Delay_Sync);    //delay 300ms
        }

        private bool BurstRead(uint _reg_addr_start, int num, uint[] _readBack_data)
        {
            Delay(Delay_Sync);
            //set pilot firstly
            numUD_pilotwidth_ow_ValueChanged(null, null);

            if (bAutoTrimTest)
                DisplayOperateMes("Read Out Data is:");

            if (oneWrie_device.I2CRead_Burst(this.DeviceAddress, _reg_addr_start, 5, _readBack_data) == 0)
            {
                for (int ix = 0; ix < num; ix++)
                {
                    if (bAutoTrimTest)
                        DisplayOperateMes(string.Format("Reg0x{0} = 0x{1}", (_reg_addr_start + ix).ToString("X2"), _readBack_data[ix].ToString("X2")));
                }
                return true;
            }
            else
            {
                DisplayOperateMes("Read Back Failed!");
                return false;
            }
        }

        private void TrimFinish()
        {
            //DisplayOperateMes("AutoTrim Canceled!", Color.Red);
            if(ProgramMode == 0)
                oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0u);
            else if (ProgramMode == 2)
            {
                MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                Delay(Delay_Sync);
                MultiSiteSocketSelect(9);       //set epio1 = low; epio3 = high
            }
            Delay(Delay_Sync);
            PowerOff();
            RestoreRegValue();
            DisplayOperateMes("Return!");
            //return;
        }

        private void PrintDutAttribute(ModuleAttribute sDUT)
        {
            DisplayOperateMes("<--------------------------->");
            DisplayOperateMes("IQ = " + sDUT.dIQ.ToString("F3"));
            DisplayOperateMes("dVoutIPNative = " + sDUT.dVoutIPNative.ToString("F3"));
            DisplayOperateMes("dVout0ANative = " + sDUT.dVout0ANative.ToString("F3"));
            //DisplayOperateMes("dVoutIPMiddle = " + sDUT.dVoutIPMiddle.ToString("F3"));
            //DisplayOperateMes("dVout0AMiddle = " + sDUT.dVout0AMiddle.ToString("F3"));
            DisplayOperateMes("dVoutIPTrimmed = " + sDUT.dVoutIPTrimmed.ToString("F3"));
            DisplayOperateMes("dVout0ATrimmed = " + sDUT.dVout0ATrimmed.ToString("F3"));
            DisplayOperateMes("iErrorCode = " + sDUT.iErrorCode.ToString("D2"));
            DisplayOperateMes("bDigitalCommFail = " + sDUT.bDigitalCommFail.ToString());
            DisplayOperateMes("bNormalModeFail = " + sDUT.bNormalModeFail.ToString());
            DisplayOperateMes("bReadMarginal = " + sDUT.bReadMarginal.ToString());
            DisplayOperateMes("bReadSafety = " + sDUT.bReadSafety.ToString());
            DisplayOperateMes("bTrimmed = " + sDUT.bTrimmed.ToString());
            DisplayOperateMes("<--------------------------->");

            //open file for prodcution record
            string filename = System.Windows.Forms.Application.StartupPath; ;
            filename += @"\Record.dat";

            int iFileLine = 0;

            StreamReader sr = new StreamReader(filename);
            while (sr.ReadLine() != null)
            {
                //sr.ReadLine();
                iFileLine++;
            }
            sr.Close();

            StreamWriter sw;
            if (iFileLine < 65535)
                sw = new StreamWriter(filename, true);
            else
                sw = new StreamWriter(filename, false);

            string msg;

            msg = string.Format("# # # {0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11} {12} {13} {14}", IP.ToString(), sDUT.dIQ.ToString("F3"),
                sDUT.dVoutIPNative.ToString("F3"), sDUT.dVout0ANative.ToString("F3"),
                MultiSiteReg0[0].ToString("X2"), MultiSiteReg1[0].ToString("X2"),
                MultiSiteReg2[0].ToString("X2"), MultiSiteReg3[0].ToString("X2"),
                MultiSiteReg4[0].ToString("X2"), MultiSiteReg5[0].ToString("X2"),
                MultiSiteReg6[0].ToString("X2"), MultiSiteReg7[0].ToString("X2"),
                sDUT.dVoutIPTrimmed.ToString("F3"), sDUT.dVout0ATrimmed.ToString("F3"),
                sDUT.iErrorCode.ToString("D2"));
            sw.WriteLine(msg);

            //msg = System.DateTime.Now.ToString();
            //sw.WriteLine(msg);

            sw.Close();
        }

        private void loadLogFile()
        {
            //open file for prodcution record
            string filename = System.Windows.Forms.Application.StartupPath; ;
            filename += @"\Record.dat";

            int iFileLine = 0;

            StreamReader sr = new StreamReader(filename);
            while (sr.ReadLine() != null)
            {
                //sr.ReadLine();
                iFileLine++;
            }
            sr.Close();

            StreamWriter sw;
            if (iFileLine < 65535)
                sw = new StreamWriter(filename, true);
            else
                sw = new StreamWriter(filename, false);

            string msg;

            msg = System.DateTime.Now.ToString();
            sw.WriteLine(msg);

            sw.Close();
        }

        #endregion Methods

        #region Events

        private void contextMenuStrip_Copy_MouseUp(object sender, MouseEventArgs e)
        {
            this.txt_OutputLogInfo.Copy();
        }

        private void contextMenuStrip_Paste_Click(object sender, EventArgs e)
        {
            this.txt_OutputLogInfo.Paste();
        }

        private void contextMenuStrip_Clear_MouseUp(object sender, MouseEventArgs e)
        {
            this.txt_OutputLogInfo.Text = null;
            //解决Scroll Bar的刷新问题。
            this.txt_OutputLogInfo.ScrollBars = RichTextBoxScrollBars.None;
            this.txt_OutputLogInfo.ScrollBars = RichTextBoxScrollBars.Both;
        }

        private void contextMenuStrip_SelAll_Click(object sender, EventArgs e)
        {
            this.txt_OutputLogInfo.SelectAll();
        }

        private void txt_TargetGain_TextChanged(object sender, EventArgs e)
        {
            try
            {
                //temp = (4500d - 2000d) / double.Parse(this.txt_TargetGain.Text);
                if ((sender as TextBox).Text.ToCharArray()[(sender as TextBox).Text.Length - 1].ToString() == ".")
                    return;
                TargetGain_customer = double.Parse((sender as TextBox).Text);
                //TargetGain_customer = (double.Parse((sender as TextBox).Text) * 2000d)/IP;
            }
            catch
            {
                string tempStr = string.Format("Target gain set failed, will use default value {0}", this.TargetGain_customer);
                DisplayOperateMes(tempStr, Color.Red);
            }
            finally
            {
                //TargetGain_customer = TargetGain_customer;      //Force to update text to default.
            }

            //double temp = 2000d / TargetGain_customer;
            //this.IP = temp;  
            //this.txt_IP_EngT.Text = temp.ToString();
            //this.txt_IP_PreT.Text = temp.ToString();
            //this.txt_IP_AutoT.Text = temp.ToString();
        }

        private void btn_PowerOn_OWCI_ADC_Click(object sender, EventArgs e)
        {
            if ( ! oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_POWER_ON))
                DisplayOperateMes("Power on failed!");

            btn_ModuleCurrent_EngT_Click(null, null);
        }

        private void btn_PowerOff_OWCI_ADC_Click(object sender, EventArgs e)
        {
            if (oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_POWER_OFF))
                DisplayOperateMes("Power off succeeded!");
            else
                DisplayOperateMes("Power off failed!");
            //uint[] uTest = new uint[16];
            //for (uint i = 0; i < 16; i++)
            //{
            //    uTest[i] = i;
            //}

            //MultiSiteDisplayResult(uTest);


        }

        private void btn_enterNomalMode_Click(object sender, EventArgs e)
        {
            EnterNomalMode();
        }
        
        private void btn_ADCReset_Click(object sender, EventArgs e)
        {
            if (!oneWrie_device.ADCReset())
                DisplayOperateMes("ADC Reset Failed!", Color.Red);
            else
                DisplayOperateMes("ADC Reset succeeded!");
        }

        private void btn_CalcGainCode_EngT_Click(object sender, EventArgs e)
        {
            //Rough Trim
            string baseMes = "Calculate Gain Operation:";
            if (bAutoTrimTest)
            {
                DisplayOperateMes(baseMes);
            }

            double testGain = GainCalculate();
            if (bAutoTrimTest)
            {
                DisplayOperateMes("Test Gain = " + testGain.ToString());
            }

            double gainTuning = 100 * GainTuningCalc_Customer(testGain, TargetGain_customer);   //计算修正值，供查表用
            if (bAutoTrimTest)
            {
                DisplayOperateMes("Ideal Gain = " + gainTuning.ToString("F4") + "%");
            }

            Ix_ForPrecisonGainCtrl = LookupPreciseGain(gainTuning, PreciseTable_Customer);
            if (bAutoTrimTest)
            {
                DisplayOperateMes("Precise Gain Index = " + Ix_ForPrecisonGainCtrl.ToString() +
                    ";Choosed Gain = " + PreciseTable_Customer[0][Ix_ForPrecisonGainCtrl].ToString() + "%");
            }

            bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask | bit3_Mask | bit4_Mask;
            Reg80Value &= ~bit_op_mask;
            Reg80Value |= Convert.ToUInt32(PreciseTable_Customer[1][Ix_ForPrecisonGainCtrl]);
            if (bAutoTrimTest)
            {
                DisplayOperateMes("Reg1 Value = " + Reg80Value.ToString() +
                    "(+ 0x" + Convert.ToInt32(PreciseTable_Customer[1][Ix_ForPrecisonGainCtrl]).ToString("X") + ")");
            }
        }

        private bool MultiSiteOffsetAlg(uint[] reg_TMS )
        {
            string baseMes = "Offset Trim Operation:";
            if (bAutoTrimTest)
            {
                DisplayOperateMes(baseMes);
            }
            double offsetTuning = 100 * OffsetTuningCalc_Customer();
            if (bAutoTrimTest)
            {
                DisplayOperateMes("Lookup offset = " + offsetTuning.ToString("F4") + "%");
            }

            Ix_ForOffsetATable = LookupOffset(ref offsetTuning, OffsetTableA_Customer);
            //offsetTuning = offsetTuning / OffsetTableA_Customer[0][Ix_ForOffsetATable]; 
            Ix_ForOffsetBTable = LookupOffset(ref offsetTuning, OffsetTableB_Customer);

            if (bAutoTrimTest)
            {
                DisplayOperateMes("Offset TableA chose Index = " + Ix_ForOffsetATable.ToString() +
                    ";Choosed OffsetA = " + OffsetTableA_Customer[0][Ix_ForOffsetATable].ToString("F4"));
                DisplayOperateMes("Offset TableB chose Index = " + Ix_ForOffsetBTable.ToString() +
                    ";Choosed OffsetB = " + OffsetTableB_Customer[0][Ix_ForOffsetBTable].ToString("F4"));
            }

            reg_TMS[0] += Convert.ToUInt32(OffsetTableA_Customer[1][Ix_ForOffsetATable]);
            reg_TMS[1] += Convert.ToUInt32(OffsetTableA_Customer[2][Ix_ForOffsetATable]);

            if (bAutoTrimTest)
            {
                DisplayOperateMes("Reg0x81 Value = 0x" + reg_TMS[0].ToString("X2") + "(+ 0x" + Convert.ToInt32(OffsetTableA_Customer[1][Ix_ForOffsetATable]).ToString("X") + ")");
                DisplayOperateMes("Reg0x82 Value = 0x" + reg_TMS[1].ToString("X2") + "(+ 0x" + Convert.ToInt32(OffsetTableA_Customer[2][Ix_ForOffsetATable]).ToString("X") + ")");
            }

            bit_op_mask = bit2_Mask | bit3_Mask | bit4_Mask | bit5_Mask;
            reg_TMS[2] &= ~bit_op_mask;
            reg_TMS[2] |= Convert.ToUInt32(OffsetTableB_Customer[1][Ix_ForOffsetBTable]);
            if (bAutoTrimTest)
            {
                DisplayOperateMes("Reg0x83 Value = 0x" + reg_TMS[2].ToString("X2") + "(+ 0x" + Convert.ToInt32(OffsetTableB_Customer[1][Ix_ForOffsetBTable]).ToString("X") + ")");
            }
            return true;
        }

        private bool DiffModeOffsetAlg(uint[] reg_TMS)
        {
            string baseMes = "Offset Trim Operation:";
            if (bAutoTrimTest)
            {
                DisplayOperateMes(baseMes);
            }
            double offsetTuning = 100 * OffsetTuningCalc_Customer();
            if (bAutoTrimTest)
            {
                DisplayOperateMes("Lookup offset = " + offsetTuning.ToString("F4") + "%");
            }

            //Ix_ForOffsetATable = LookupOffset(ref offsetTuning, OffsetTableA_Customer);
            //offsetTuning = offsetTuning / OffsetTableA_Customer[0][Ix_ForOffsetATable]; 
            Ix_ForOffsetBTable = LookupOffset(ref offsetTuning, OffsetTableB_Customer);

            if (bAutoTrimTest)
            {
                DisplayOperateMes("Offset TableA chose Index = " + Ix_ForOffsetATable.ToString() +
                    ";Choosed OffsetA = " + OffsetTableA_Customer[0][Ix_ForOffsetATable].ToString("F4"));
                DisplayOperateMes("Offset TableB chose Index = " + Ix_ForOffsetBTable.ToString() +
                    ";Choosed OffsetB = " + OffsetTableB_Customer[0][Ix_ForOffsetBTable].ToString("F4"));
            }

            reg_TMS[0] += Convert.ToUInt32(OffsetTableA_Customer[1][Ix_ForOffsetATable]);
            reg_TMS[1] += Convert.ToUInt32(OffsetTableA_Customer[2][Ix_ForOffsetATable]);

            if (bAutoTrimTest)
            {
                DisplayOperateMes("Reg0x81 Value = 0x" + reg_TMS[0].ToString("X2") + "(+ 0x" + Convert.ToInt32(OffsetTableA_Customer[1][Ix_ForOffsetATable]).ToString("X") + ")");
                DisplayOperateMes("Reg0x82 Value = 0x" + reg_TMS[1].ToString("X2") + "(+ 0x" + Convert.ToInt32(OffsetTableA_Customer[2][Ix_ForOffsetATable]).ToString("X") + ")");
            }

            bit_op_mask = bit2_Mask | bit3_Mask | bit4_Mask | bit5_Mask;
            reg_TMS[2] &= ~bit_op_mask;
            reg_TMS[2] |= Convert.ToUInt32(OffsetTableB_Customer[1][Ix_ForOffsetBTable]);
            if (bAutoTrimTest)
            {
                DisplayOperateMes("Reg0x83 Value = 0x" + reg_TMS[2].ToString("X2") + "(+ 0x" + Convert.ToInt32(OffsetTableB_Customer[1][Ix_ForOffsetBTable]).ToString("X") + ")");
            }
            return true;
        }

        private void btn_offset_Click(object sender, EventArgs e)
        {
            string baseMes = "Offset Trim Operation:";
            if (bAutoTrimTest)
            {
                DisplayOperateMes(baseMes);
            }
            double offsetTuning = 100 * OffsetTuningCalc_Customer();
            if (bAutoTrimTest)
            {
                DisplayOperateMes("Lookup offset = " + offsetTuning.ToString("F4") + "%");
            }

            Ix_ForOffsetATable = LookupOffset(ref offsetTuning, OffsetTableA_Customer);
            //offsetTuning = offsetTuning / OffsetTableA_Customer[0][Ix_ForOffsetATable]; 
            Ix_ForOffsetBTable = LookupOffset(ref offsetTuning, OffsetTableB_Customer);

            if (bAutoTrimTest)
            {
                DisplayOperateMes("Offset TableA chose Index = " + Ix_ForOffsetATable.ToString() +
                    ";Choosed OffsetA = " + OffsetTableA_Customer[0][Ix_ForOffsetATable].ToString("F4"));
                DisplayOperateMes("Offset TableB chose Index = " + Ix_ForOffsetBTable.ToString() +
                    ";Choosed OffsetB = " + OffsetTableB_Customer[0][Ix_ForOffsetBTable].ToString("F4"));
            }

            Reg81Value += Convert.ToUInt32(OffsetTableA_Customer[1][Ix_ForOffsetATable]);
            Reg82Value += Convert.ToUInt32(OffsetTableA_Customer[2][Ix_ForOffsetATable]);

            if (bAutoTrimTest)
            {
                DisplayOperateMes("Reg2 Value = " + Reg81Value.ToString() + "(+ 0x" + Convert.ToInt32(OffsetTableA_Customer[1][Ix_ForOffsetATable]).ToString("X") + ")");
                DisplayOperateMes("Reg3 Value = " + Reg82Value.ToString() + "(+ 0x" + Convert.ToInt32(OffsetTableA_Customer[2][Ix_ForOffsetATable]).ToString("X") + ")");
            }

            bit_op_mask = bit2_Mask | bit3_Mask | bit4_Mask | bit5_Mask;
            Reg83Value &= ~bit_op_mask;
            Reg83Value |= Convert.ToUInt32(OffsetTableB_Customer[1][Ix_ForOffsetBTable]);
            if (bAutoTrimTest)
            {
                DisplayOperateMes("Reg4 Value = " + Reg83Value.ToString() + "(+ 0x" + Convert.ToInt32(OffsetTableB_Customer[1][Ix_ForOffsetBTable]).ToString("X") + ")");
            }
        }

        private void btn_writeFuseCode_Click(object sender, EventArgs e)
        {
            //set pilot firstly
            numUD_pilotwidth_ow_ValueChanged(null, null);

            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_CONFIG_TO_VOUT);
            rbt_signalPathSeting_Config_EngT.Checked = true;


            bool fuseMasterBit = false;
            DialogResult dr = MessageBox.Show("Do you want to Fuse master bit?", "Fuse master bit??", MessageBoxButtons.YesNoCancel);
            if (dr == DialogResult.Cancel)
                return;
            else if (dr == System.Windows.Forms.DialogResult.Yes)
                fuseMasterBit = true;

            try
            {
                string temp;
                uint _dev_addr = this.DeviceAddress;

                //Enter test mode
                uint _reg_addr = 0x55;
                uint _reg_data = 0xAA;
                oneWrie_device.I2CWrite_Single(_dev_addr, _reg_addr, _reg_data);

                //Reg80
                temp = this.txt_reg80_EngT.Text.TrimStart("0x".ToCharArray()).TrimEnd("H".ToCharArray());
                uint _reg_Addr = 0x80;
                uint _reg_Value = UInt32.Parse((temp == "" ? "0" : temp), System.Globalization.NumberStyles.HexNumber);

                if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
                    DisplayOperateMes("Write Reg1(0x" + _reg_Value.ToString("X") + ") succeeded!");
                else
                    DisplayOperateMes("Write Reg1 Failed!", Color.Red);

                //Reg81
                temp = this.txt_reg81_EngT.Text.TrimStart("0x".ToCharArray()).TrimEnd("H".ToCharArray());
                _reg_Addr = 0x81;
                _reg_Value = UInt32.Parse((temp == "" ? "0" : temp), System.Globalization.NumberStyles.HexNumber);

                if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
                    DisplayOperateMes("Write Reg2(0x" + _reg_Value.ToString("X") + ") succeeded!");
                else
                    DisplayOperateMes("Write Reg2 Failed!", Color.Red);

                //Reg82
                temp = this.txt_reg82_EngT.Text.TrimStart("0x".ToCharArray()).TrimEnd("H".ToCharArray());
                _reg_Addr = 0x82;
                _reg_Value = UInt32.Parse((temp == "" ? "0" : temp), System.Globalization.NumberStyles.HexNumber);

                if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
                    DisplayOperateMes("Write Reg3(0x" + _reg_Value.ToString("X") + ") succeeded!");
                else
                    DisplayOperateMes("Write Reg3 Failed!", Color.Red);

                //Reg83
                temp = this.txt_reg83_EngT.Text.TrimStart("0x".ToCharArray()).TrimEnd("H".ToCharArray());
                _reg_Addr = 0x83;
                _reg_Value = UInt32.Parse((temp == "" ? "0" : temp), System.Globalization.NumberStyles.HexNumber);

                if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
                    DisplayOperateMes("Write Reg4(0x" + _reg_Value.ToString("X") + ") succeeded!");
                else
                    DisplayOperateMes("Write Reg4 Failed!", Color.Red);

                if (fuseMasterBit)
                {
                    //Reg84
                    _reg_Addr = 0x84;
                    _reg_Value = 0x07;

                    if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
                        DisplayOperateMes("Master bit fused succeeded!");
                    else
                        DisplayOperateMes("Master bit fused Failed!", Color.Red);
                }

            }
            catch
            {
                MessageBox.Show("Write data format error!");
            }
        }

        private void txt_reg80_TextChanged(object sender, EventArgs e)
        {

        }

        private void txt_reg81_TextChanged(object sender, EventArgs e)
        {

        }

        private void txt_reg82_TextChanged(object sender, EventArgs e)
        {
            try
            {
                string temp = this.txt_reg82_EngT.Text.TrimStart("0x".ToCharArray()).TrimEnd("H".ToCharArray());
                if (temp.Length > 2)
                    temp = temp.Substring(0, 2);
                uint regValue = UInt32.Parse((temp == "" ? "0" : temp), System.Globalization.NumberStyles.HexNumber);

                if (Reg82Value == regValue)
                    return;
                else
                {
                    this.Reg82Value = regValue;
                    DisplayOperateMes("Enter Reg3 value succeeded!");
                }
            }
            catch
            {
                DisplayOperateMes("Enter Reg3 value failed!", Color.Red);
            }
            finally
            {
                this.txt_reg82_EngT.Text = "0x" + this.Reg82Value.ToString("X2");
            }
        }

        private void txt_reg83_TextChanged(object sender, EventArgs e)
        {
            try
            {
                string temp = this.txt_reg83_EngT.Text.TrimStart("0x".ToCharArray()).TrimEnd("H".ToCharArray());
                if (temp.Length > 2)
                    temp = temp.Substring(0, 2);
                uint regValue = UInt32.Parse((temp == "" ? "0" : temp), System.Globalization.NumberStyles.HexNumber);
                if (Reg83Value == regValue)
                    return;
                else
                {
                    this.Reg83Value = regValue;
                    DisplayOperateMes("Enter Reg3 value succeeded!");
                }
            }
            catch
            {
                DisplayOperateMes("Enter Reg value failed!", Color.Red);
            }
            finally
            {
                this.txt_reg83_EngT.Text = "0x" + this.Reg83Value.ToString("X2");
            }
        }

        private void txt_RegValue_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox txt_Regx = sender as TextBox;
            e.KeyChar = Convert.ToChar(e.KeyChar.ToString().ToUpper());
            string str = "\r\b0123456789abcdefABCDEF";//This will allow the user to enter numeric HEX values only.

            e.Handled = !(str.Contains(e.KeyChar.ToString()));

            if (e.Handled)
                return;
            else
            {
                if (e.KeyChar.ToString() == "\r")
                {
                    RegTextChangedDisplay(txt_Regx);
                    txt_Regx.SelectionStart = txt_Regx.Text.Length;
                    //try
                    //{
                    //    //string temp = txt_Regx.Text.TrimStart("0x".ToCharArray()).TrimEnd("H".ToCharArray());
                    //    //uint _reg_value = UInt32.Parse((temp == "" ? "0" : temp), System.Globalization.NumberStyles.HexNumber);
                    //    RegTextChangedDisplay(txt_Regx);
                    //}
                    //catch
                    //{
                    //    txt_Regx.Text = this.
                    //}
                }
            }

            #region Comment out
            //if (txt_Regx.Text.Length >= 2)
            //{
            //    if (txt_Regx.Text.StartsWith("0x") | txt_Regx.Text.StartsWith("0X"))
            //    {
            //        if (txt_Regx.Text.Length >= 4)
            //        {
            //            if ((e.KeyChar == '\b') | ((txt_Regx.SelectionLength >= 1) & (txt_Regx.SelectionStart >= 2)) |
            //                           (txt_Regx.SelectionLength == txt_Regx.Text.Length))
            //            {
            //                e.Handled = !(str.Contains(e.KeyChar.ToString()));
            //                RegTextChangedDisplay(txt_Regx);
            //                return;
            //            }
            //            else
            //            {
            //                e.Handled = true;
            //                return;
            //            }
            //        }
            //    }
            //    else
            //    {
            //        if (e.KeyChar != '\b' | (txt_Regx.SelectionLength == txt_Regx.Text.Length))
            //        {
            //            e.Handled = true;
            //            txt_Regx.Text = "0x" + txt_Regx.Text;
            //            RegTextChangedDisplay(txt_Regx);
            //            return;
            //        }

            //    }
            //}
            //e.Handled = !(str.Contains(e.KeyChar.ToString()));
            //if (e.Handled | txt_Regx.Text.StartsWith("0x") | txt_Regx.Text.StartsWith("0X"))
            //{
            //    return;
            //}
            //else
            //{
            //    txt_Regx.Text = "0x" + txt_Regx.Text;
            //    RegTextChangedDisplay(txt_Regx);
            //    txt_Regx.SelectionStart = txt_Regx.Text.Length;
            //}
            #endregion Comment out
        }

        private void RegTextChangedDisplay(TextBox txtReg)
        {
            if ((txtReg == this.txt_reg80_EngT) | (txtReg == this.txt_Reg80_PreT))
                this.txt_reg80_TextChanged(null, null);
            else if ((txtReg == this.txt_reg81_EngT) | (txtReg == this.txt_Reg81_PreT))
                this.txt_reg81_TextChanged(null, null);
            else if ((txtReg == this.txt_reg82_EngT) | (txtReg == this.txt_Reg82_PreT))
                this.txt_reg82_TextChanged(null, null);
            else if ((txtReg == this.txt_reg83_EngT) | (txtReg == this.txt_Reg83_PreT))
                this.txt_reg83_TextChanged(null, null);
        }

        private void rbt_5V_CheckedChanged(object sender, EventArgs e)
        {
            bool setResult;
            if (rbt_VDD_5V_EngT.Checked)
                setResult = oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_5V);
            else
                setResult = oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_EXT);

            string message;
            if (rbt_VDD_5V_EngT.Checked)
                message = "VDD chose 5V";
            else
                message = "VDD chose external power";

            if (setResult)
            {
                if (bAutoTrimTest)
                {
                    message += " succeeded!";
                    DisplayOperateMes(message);
                }
            }
            else
            {
                if (bAutoTrimTest)
                {
                    message += " Failed!";
                    DisplayOperateMes(message, Color.Red);
                }
            }
        }

        private void rbt_withCap_Vout_CheckedChanged(object sender, EventArgs e)
        {
            bool setResult;
            if (rbt_withCap_Vout_EngT.Checked)
                setResult = oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);
            else
                setResult = oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITHOUT_CAP);

            string message;
            if (rbt_withCap_Vout_EngT.Checked)
                message = "Vout with Cap set";
            else
                message = "Vout without Cap set";

            if (setResult)
            {
                if (bAutoTrimTest)
                {
                    message += " succeeded!";
                    DisplayOperateMes(message);
                }
            }
            else
            {
                if (bAutoTrimTest)
                {
                    message += " Failed!";
                    DisplayOperateMes(message, Color.Red);
                }
            }
        }

        private void rbt_withCap_Vref_CheckedChanged(object sender, EventArgs e)
        {
            bool setResult;
            if (rbt_withCap_Vref.Checked)
                setResult = oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VREF_WITH_CAP);
            else
                setResult = oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VREF_WITHOUT_CAP);

            string message;
            if (rbt_withCap_Vref.Checked)
                message = "Vref with Cap set";
            else
                message = "Vref without Cap set";
            if (setResult)
            {
                if (bAutoTrimTest)
                {
                    message += " succeeded!";
                    DisplayOperateMes(message);
                }
            }
            else
            {
                if (bAutoTrimTest)
                {
                    message += " Failed!";
                    DisplayOperateMes(message, Color.Red);
                }
            }
        }

        private void rbt_signalPathSeting_CheckedChanged(object sender, EventArgs e)
        {
            bool setResult;
            string message;
            //L-Vout
            if (rbt_signalPathSeting_Vout_EngT.Checked && rbt_signalPathSeting_AIn_EngT.Checked)
            {
                setResult = oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);
                message = "Vout to VIn set";
            }
            else if (rbt_signalPathSeting_Vout_EngT.Checked && rbt_signalPathSeting_Config_EngT.Checked)
            {
                setResult = oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_CONFIG_TO_VOUT);
                message = "Vout to CONFIG set";
            }
            //L-Vref
            else if (rbt_signalPathSeting_Vref_EngT.Checked && rbt_signalPathSeting_AIn_EngT.Checked)
            {
                setResult = oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VREF);
                message = "Vref to VIn set";
            }
            else if (rbt_signalPathSeting_Vref_EngT.Checked && rbt_signalPathSeting_Config_EngT.Checked)
            {
                setResult = oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_CONFIG_TO_VREF);
                message = "Vref to CONFIG set";
            }
            //L-VCS
            else if (rbt_signalPathSeting_VCS_EngT.Checked && rbt_signalPathSeting_AIn_EngT.Checked)
            {
                setResult = oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VCS);
                message = "VCS to VIn set";
            }
            else if (rbt_signalPathSeting_VCS_EngT.Checked && rbt_signalPathSeting_Config_EngT.Checked)
            {
                setResult = oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_CONFIG_TO_VCS);
                message = "VSC to CONFIG set";
            }
            //L-510out
            else if (rbt_signalPathSeting_510Out_EngT.Checked && rbt_signalPathSeting_AIn_EngT.Checked)
            {
                setResult = oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_510OUT);
                message = "510out to VIn set";
            }
            else if (rbt_signalPathSeting_510Out_EngT.Checked && rbt_signalPathSeting_Config_EngT.Checked)
            {
                setResult = oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_CONFIG_TO_510OUT);
                message = "510out to CONFIG set";
            }
            //L-Mout
            else if (rbt_signalPathSeting_Mout_EngT.Checked && rbt_signalPathSeting_AIn_EngT.Checked)
            {
                setResult = oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_MOUT);
                message = "Mout to VIn set";
            }
            else if (rbt_signalPathSeting_Mout_EngT.Checked && rbt_signalPathSeting_Config_EngT.Checked)
            {
                setResult = oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_CONFIG_TO_MOUT);
                message = "Mout to CONFIG set";
            }
            else
            {
                message = "Signal path routing failed!";
                return;
            }

            if (setResult)
            {
                if (bAutoTrimTest)
                {
                    message += " succeeded!";
                    DisplayOperateMes(message);
                }
            }
            else
            {
                if (bAutoTrimTest)
                {
                    message += " Failed!";
                    DisplayOperateMes(message, Color.Red);
                }
            }
        }

        private void rbtn_CSResistorByPass_EngT_CheckedChanged(object sender, EventArgs e)
        {
            bool setResult;
            string message;
            if (rbtn_CSResistorByPass_EngT.Checked)
            {
                setResult = oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_BYPASS_CURRENT_SENCE);
                message = "Vout to VIn set";
            }
            else
            {
                setResult = oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_SET_CURRENT_SENCE);
                message = "Vout to CONFIG set";
            }

            if (setResult)
            {
                if (bAutoTrimTest)
                {
                    message += " succeeded!";
                    DisplayOperateMes(message);
                }
            }
            else
            {
                if (bAutoTrimTest)
                {
                    message += " Failed!";
                    DisplayOperateMes(message, Color.Red);
                }
            }
        }

        private void btn_burstRead_Click(object sender, EventArgs e)
        {
            //set pilot firstly
            numUD_pilotwidth_ow_ValueChanged(null, null);

            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_CONFIG_TO_VOUT);
            rbt_signalPathSeting_Config_EngT.Checked = true;

            EnterTestMode();

            //Read Back 0x80~0x85
            uint _reg_addr_start = 0x80;
            uint[] _readBack_data = new uint[5];
            BurstRead(_reg_addr_start, 5, _readBack_data);


            //uint data = 0;
            //data = oneWrie_device.I2CRead_Single(this.DeviceAddress, 0x80);
            //DisplayOperateMes(string.Format("Reg0x80 = 0x{0}", data.ToString("X2")));
        }

        private void btn_MarginalRead_Click(object sender, EventArgs e)
        {
            //oneWrie_device.ADCSigPathSet(OneWireInterface.ADCControlCommand.ADC_CONFIG_TO_VOUT);
            rbt_signalPathSeting_Vout_EngT.Checked = true;
            rbt_signalPathSeting_Config_EngT.Checked = true;

            MarginalReadPreset();
        }

        private void btn_SafetyRead_EngT_Click(object sender, EventArgs e)
        {
            //oneWrie_device.ADCSigPathSet(OneWireInterface.ADCControlCommand.ADC_CONFIG_TO_VOUT);
            rbt_signalPathSeting_Vout_EngT.Checked = true;
            rbt_signalPathSeting_Config_EngT.Checked = true;

            SafetyReadPreset();
        }

        private void btn_Reload_Click(object sender, EventArgs e)
        {
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_CONFIG_TO_VOUT);
            rbt_signalPathSeting_Config_EngT.Checked = true;

            try
            {
                uint _reg_addr = 0x55;
                uint _reg_data = 0xAA;
                oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data);

                _reg_addr = 0x43;
                _reg_data = 0x0B;

                bool writeResult = oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data);
                //Console.WriteLine("I2C write result->{0}", oneWrie_device.I2CWrite_Single(_dev_addr, _reg_addr, _reg_data));
                if (writeResult)
                    DisplayOperateMes("Reload succeeded!");
                else
                    DisplayOperateMes("I2C write failed, Reload Failed!", Color.Red);

                //Delay 100ms
                Thread.Sleep(100);
                DisplayOperateMes("Delay 100ms");

                _reg_addr = 0x43;
                _reg_data = 0x0;

                writeResult = oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data);
                //Console.WriteLine("I2C write result->{0}", oneWrie_device.I2CWrite_Single(_dev_addr, _reg_addr, _reg_data));
                if (writeResult)
                    DisplayOperateMes("Reset Reg0x43 succeeded!");
                else
                    DisplayOperateMes("Reset Reg0x43 failed!", Color.Red);
            }
            catch
            {
                DisplayOperateMes("Reload Failed!", Color.Red);
            }
        }
        
        private void numUD_TargetGain_Customer_ValueChanged(object sender, EventArgs e)
        {
            targetGain_customer = (double)(sender as NumericUpDown).Value;
        }

        private void numUD_IPxForCalc_Customer_ValueChanged(object sender, EventArgs e)
        {
            StrIPx_Auto = (sender as NumericUpDown).Value.ToString("F1") + "A";
            selectedCurrent_Auto = (double)(sender as NumericUpDown).Value;
        }

        private void AutoTrimTab_Enter(object sender, EventArgs e)
        {
            //Backup value for autotrim
            StoreRegValue();
        }


        //bool bAutoTrimTest = false;
        private void btn_AutomaticaTrim_Click(object sender, EventArgs e)
        {
            DialogResult dr;

            DisplayOperateMesClear();

            #region Check HW connection
            if (!bUsbConnected)
            {
                DisplayOperateMes("Please Confirm HW Connection!", Color.Red);
                return;
            }
            #endregion

            #region IP Initialize
            if (ProgramMode == 0)
            {
                //if (ProgramMode == 0 && bUartInit == false)
                //{

                //UART Initialization
                if ( ! oneWrie_device.UARTInitilize(9600, 1) )
                    DisplayOperateMes("UART Initilize failed!");
                //ding hao
                Delay(Delay_Power);

                //1. Current Remote CTL
                if ( ! oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_REMOTE, 0))
                    DisplayOperateMes("Set Current Remote failed!");

                Delay(Delay_Power);
     
                if (! oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, Convert.ToUInt32(IP)))
                    DisplayOperateMes("Set Current to IP failed!");

                Delay(Delay_Power);

                //3. Set Voltage
                if (! oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETVOLT, 6u))
                    DisplayOperateMes(string.Format("Set Voltage to {0}V failed!", 6));

                Delay(Delay_Power);
               
            }
            else if (ProgramMode == 2) 
            {
                MultiSiteSocketSelect(1);   //epio1,3 = high

                if(!bDualRelayIpOn)
                {
                    bDualRelayIpOn = true;
                    dr = MessageBox.Show(String.Format("请将电流升至{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                    {
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        PowerOff();
                        RestoreRegValue();
                        return;
                    }
                }
            }
            #endregion IP Initialize

            #region Trim Routines 
            DateTime StartTime = System.DateTime.Now;

            //this.TargetOffset = Convert.ToDouble(this.txt_VoutOffset_AutoT.Text);

            DisplayOperateMes("Target Offset = " + TargetOffset.ToString("F3"));

            if (this.cmb_Module_PreT.SelectedItem.ToString() == "5V" || this.cmb_Module_PreT.SelectedItem.ToString() == "3.3V")
            {             
                if (ProductType == 0)
                    AutoTrim_SL510_SingleSite();
                else if (ProductType == 1)
                    AutoTrim_SL510_DiffMode();
                else if (ProductType == 2)
                {
                    #region SL622 routines
                    if (this.cb_iHallOption_AutoTab.SelectedIndex == 2)
                        Reg80Value = 0x80;      //iHall decrease 33%
                    else
                        Reg80Value = 0x00;

                    if (this.cb_ChopCkDis_AutoTab.Checked)
                        Reg83Value = 0x08;
                    else
                        Reg83Value = 0x00;
                    
                    if (this.cmb_PreTrim_SensorDirection.SelectedIndex == 0)
                    {
                        if (this.cmb_Voffset_PreT.SelectedIndex == 0)
                            Reg80Value |= 0x04;
                        else if (this.cmb_Voffset_PreT.SelectedIndex == 1)
                            Reg80Value |= 0x04;
                        else if (this.cmb_Voffset_PreT.SelectedIndex == 2)
                            Reg80Value |= 0x05;
                        else if (this.cmb_Voffset_PreT.SelectedIndex == 3)
                            Reg80Value |= 0x06;
                    }
                    else if (this.cmb_PreTrim_SensorDirection.SelectedIndex == 1)
                    {
                        DisplayOperateMes("Inverted Sensor Direction");
                        if (this.cmb_Voffset_PreT.SelectedIndex == 0)
                            Reg80Value |= 0x00;
                        else if (this.cmb_Voffset_PreT.SelectedIndex == 1)
                            Reg80Value |= 0x00;
                        else if (this.cmb_Voffset_PreT.SelectedIndex == 2)
                            Reg80Value |= 0x01;
                        else if (this.cmb_Voffset_PreT.SelectedIndex == 3)
                            Reg80Value |= 0x02;
                    }

                    
                    double Vip_Pretrim = 0;
                    double V0A_Pretrim = 0;
                    double coarse_PretrimGain = 0;
                    preSetCoareseGainCode = 15;
                    Reg81Value = 0x03 + preSetCoareseGainCode * 16;
                    Reg82Value = Convert.ToUInt32(this.txt_SL620TC_AutoTab.Text, 16);
                    Reg83Value += 0x30;
                    Reg84Value = 0x00;
                    Reg85Value = 0x00;
                    Reg86Value = 0x00;
                    Reg87Value = 0x00; 

                    RePower();
                    Delay(Delay_Sync);
                    RegisterWrite(4, new uint[8] { 0x80, Reg80Value, 0x81, Reg81Value, 0x82, Reg82Value, 0x83, Reg83Value });
                    Delay(Delay_Sync);
                    BurstRead(0x80, 5, tempReadback);
                    EnterNomalMode();
                    Delay(Delay_Fuse);
                    V0A_Pretrim = AverageVout();

                    #region /* Change Current to IP  */
                    if (ProgramMode == 0)
                    {
                        if (!oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                        {
                            DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                            TrimFinish();
                            return;
                        }
                    }
                    else if (ProgramMode == 1)
                    {
                        dr = MessageBox.Show(String.Format("请将电流升至{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                        if (dr == DialogResult.Cancel)
                        {
                            DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                            PowerOff();
                            RestoreRegValue();
                            return;
                        }
                    }
                    else if (ProgramMode == 2)
                    {
                        MultiSiteSocketSelect(1);   //epio1,3 = high
                        Delay(Delay_Sync);
                        MultiSiteSocketSelect(0);   //set epio1 = high; epio3 = low
                    }
                    #endregion

                    Delay(Delay_Fuse);
                    Vip_Pretrim = AverageVout();
                    DisplayOperateMes("Vout@0A = " + V0A_Pretrim.ToString("F3"));
                    DisplayOperateMes("Vout@IP_1 = " + Vip_Pretrim.ToString("F3"));


                    if (Vip_Pretrim - V0A_Pretrim < 0)
                    {
                        DisplayOperateMes("请确认IP方向！");
                        TrimFinish();
                        return;
                    }
                    else if (Vip_Pretrim - V0A_Pretrim < 0.005d)
                    {
                        DisplayOperateMes("请确认IP是否ON！");
                        TrimFinish();
                        return;
                    }

                    if (Vip_Pretrim > 4.8)
                    {
                        Reg80Value |= 0x80;     //iHall down 33%

                        RePower();
                        Delay(Delay_Sync);
                        RegisterWrite(4, new uint[8] { 0x80, Reg80Value, 0x81, Reg81Value, 0x82, Reg82Value, 0x83, Reg83Value });
                        Delay(Delay_Sync);
                        BurstRead(0x80, 5, tempReadback);
                        EnterNomalMode();
                        Delay(Delay_Fuse);
                        Vip_Pretrim = AverageVout();

                        if (Vip_Pretrim > 4.8)
                        {
                            Reg80Value &= 0x7F;     //iHall back to default
                            Reg83Value |= 0x08;     //dis ckop ck, gain down 50%

                            RePower();
                            Delay(Delay_Sync);
                            RegisterWrite(4, new uint[8] { 0x80, Reg80Value, 0x81, Reg81Value, 0x82, Reg82Value, 0x83, Reg83Value });
                            Delay(Delay_Sync);
                            BurstRead(0x80, 5, tempReadback);
                            EnterNomalMode();
                            Delay(Delay_Fuse);
                            Vip_Pretrim = AverageVout();

                            if (Vip_Pretrim > 4.8)
                            {
                                Reg80Value |= 0x80;     //iHall down 33%
                                Reg83Value |= 0x08;     //dis ckop ck, gain down 50%

                                RePower();
                                Delay(Delay_Sync);
                                RegisterWrite(4, new uint[8] { 0x80, Reg80Value, 0x81, Reg81Value, 0x82, Reg82Value, 0x83, Reg83Value });
                                Delay(Delay_Sync);
                                BurstRead(0x80, 5, tempReadback);
                                EnterNomalMode();
                                Delay(Delay_Fuse);
                                Vip_Pretrim = AverageVout();

                                if (Vip_Pretrim > 4.8)
                                {
                                    DisplayOperateMes("编程电流过大，输出饱和！");
                                    TrimFinish();
                                    return;
                                }
                            }
                        }
                    }

                    //coarse_PretrimGain = 2.0d * 12.7d / (Vip_Pretrim - V0A_Pretrim);
                    coarse_PretrimGain = TargetVoltage_customer * 12.7d / (Vip_Pretrim - V0A_Pretrim);
                    DisplayOperateMes("coarse_PretrimGain = " + coarse_PretrimGain.ToString("F3"));

                    if (TargetOffset == 2.5)
                    {
                        if (coarse_PretrimGain > 100 && coarse_PretrimGain <= 150)
                        {
                            Reg83Value += 0x03;
                            preSetCoareseGainCode = Convert.ToUInt32(LookupCoarseGain_SL620(coarse_PretrimGain / 1.5d, sl620CoarseGainTable));
                            Reg81Value = 0x03 + preSetCoareseGainCode * 16;
                        }
                        else if (coarse_PretrimGain <= 100 && coarse_PretrimGain > 12.7)
                        {
                            preSetCoareseGainCode = Convert.ToUInt32(LookupCoarseGain_SL620(coarse_PretrimGain, sl620CoarseGainTable));
                            Reg81Value = 0x03 + preSetCoareseGainCode * 16;
                        }
                        else if (coarse_PretrimGain > 150 && coarse_PretrimGain < 200)
                        {
                            Reg83Value += 0x03;
                            Reg80Value += 0x20;
                            preSetCoareseGainCode = Convert.ToUInt32(LookupCoarseGain_SL620(coarse_PretrimGain / 2.0d, sl620CoarseGainTable));
                            Reg81Value = 0x03 + preSetCoareseGainCode * 16;
                        }
                        else if (coarse_PretrimGain >= 200)
                        {
                            DisplayOperateMes("产品灵敏度要求过高！");
                            TrimFinish();
                            return;
                        }
                        else if (coarse_PretrimGain < 11)
                        {
                            DisplayOperateMes("产品灵敏度要求过低！");
                            TrimFinish();
                            return;
                        }
                    }
                    else if (TargetOffset == 1.65)
                    {
                        if (coarse_PretrimGain > 73 * 1.5)
                        {
                            Reg83Value += 0x03;
                            Reg80Value += 0x20;
                            preSetCoareseGainCode = Convert.ToUInt32(LookupCoarseGain_SL620(coarse_PretrimGain / 2.0d, sl620CoarseGainTable));
                            Reg81Value = 0x03 + preSetCoareseGainCode * 16;

                            
                        }
                        else if (coarse_PretrimGain <= 73) 
                        {
                            preSetCoareseGainCode = Convert.ToUInt32(LookupCoarseGain_SL620(coarse_PretrimGain, sl620CoarseGainTable));
                            Reg81Value = 0x03 + preSetCoareseGainCode * 16;
                        }
                        else if (coarse_PretrimGain > 73 * 1.5)
                        {
                            Reg83Value += 0x03;
                            preSetCoareseGainCode = Convert.ToUInt32(LookupCoarseGain_SL620(coarse_PretrimGain / 1.5d, sl620CoarseGainTable));
                            Reg81Value = 0x03 + preSetCoareseGainCode * 16;
                        }
                        else
                        {
                            DisplayOperateMes("产品灵敏度要求过高！");
                            TrimFinish();
                            return;
                        }
                    }

                    RePower();
                    Delay(Delay_Sync);
                    RegisterWrite(4, new uint[8] { 0x80, Reg80Value, 0x81, Reg81Value, 0x82, Reg82Value, 0x83, Reg83Value });
                    Delay(Delay_Sync);
                    BurstRead(0x80, 5, tempReadback);
                    EnterNomalMode();
                    Delay(Delay_Fuse);
                    Vip_Pretrim = AverageVout();
                    DisplayOperateMes("Vout@IP_2 = " + Vip_Pretrim.ToString("F3"));
                    if (Vip_Pretrim < 4.5)
                    {
                        if (preSetCoareseGainCode > 0)
                            preSetCoareseGainCode--;
                        Reg81Value = 0x03 + preSetCoareseGainCode * 16;
                    }
                    else if (Vip_Pretrim > 4.9)
                    {
                        if (preSetCoareseGainCode == 15)
                        {
                            DisplayOperateMes("Saturation!", Color.Red);
                            TrimFinish();
                            return;
                        }
                        else
                        {
                            preSetCoareseGainCode++;
                            Reg81Value = 0x03 + preSetCoareseGainCode * 16;
                        }
                    }

                    #region /* Change Current to 0A */
                    if (ProgramMode == 0)
                    {
                        if (!oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0u))
                        {
                            DisplayOperateMes(string.Format("Set Current to {0}A failed!", 0u));
                            DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                            TrimFinish();
                            return;
                        }
                    }
                    else if (ProgramMode == 1)
                    {
                        dr = MessageBox.Show(String.Format("请将IP降至0A!"), "Try Again", MessageBoxButtons.OKCancel);
                        if (dr == DialogResult.Cancel)
                        {
                            DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                            PowerOff();
                            RestoreRegValue();
                            return;
                        }
                    }
                    else if (ProgramMode == 2)
                    {
                        //set epio1 and epio3 to low
                        MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                        Delay(Delay_Sync);
                        MultiSiteSocketSelect(9);       //set epio1 = low; epio3 = high
                    }
                    #endregion

                    Delay(Delay_Fuse);
                    V0A_Pretrim = AverageVout();
                    DisplayOperateMes("Vout@0A_2 = " + V0A_Pretrim.ToString("F3"));

                    if (this.cmb_Voffset_PreT.SelectedIndex == 2)
                    {
                        DisplayOperateMes("SL622 half VDD Signle End");
                        AutoTrim_SL620_SingleEnd_HalfVDD();
                    }
                    else
                    {
                        DisplayOperateMes("SL622 2.5V Single End");
                        AutoTrim_SL620_SingleEnd();
                    }
                    #endregion
                }
                else if (ProductType == 3)
                {
                    #region SL622 Diff Mode routines
                    TargetOffset = 2.5;

                    if (this.cb_iHallOption_AutoTab.SelectedIndex == 2)
                        Reg80Value = 0x80;      //iHall decrease 33%
                    else
                        Reg80Value = 0x00;

                    if (this.cb_ChopCkDis_AutoTab.Checked)
                        Reg83Value = 0x08;
                    else
                        Reg83Value = 0x00;


                    if (this.cmb_PreTrim_SensorDirection.SelectedIndex == 0)
                    {
                        //if (this.cmb_Voffset_PreT.SelectedIndex == 0)
                        //    Reg80Value |= 0x04;
                        //else if (this.cmb_Voffset_PreT.SelectedIndex == 1)
                        //    Reg80Value |= 0x04;
                        //else if (this.cmb_Voffset_PreT.SelectedIndex == 2)
                            Reg80Value |= 0x05;
                        //else if (this.cmb_Voffset_PreT.SelectedIndex == 3)
                        //    Reg80Value |= 0x06;
                    }
                    else if (this.cmb_PreTrim_SensorDirection.SelectedIndex == 1)
                    {
                        DisplayOperateMes("Inverted Sensor Direction");
                        //if (this.cmb_Voffset_PreT.SelectedIndex == 0)
                        //    Reg80Value |= 0x00;
                        //else if (this.cmb_Voffset_PreT.SelectedIndex == 1)
                        //    Reg80Value |= 0x00;
                        //else if (this.cmb_Voffset_PreT.SelectedIndex == 2)
                            Reg80Value |= 0x01;
                        //else if (this.cmb_Voffset_PreT.SelectedIndex == 3)
                        //    Reg80Value |= 0x02;
                    }


                    double Vip_Pretrim = 0;
                    double V0A_Pretrim = 0;
                    double coarse_PretrimGain = 0;
                    preSetCoareseGainCode = 15;
                    Reg81Value = 0x03 + preSetCoareseGainCode * 16;
                    Reg82Value = Convert.ToUInt32(this.txt_SL620TC_AutoTab.Text, 16);
                    Reg83Value += 0x30;
                    Reg84Value = 0x00;
                    Reg85Value = 0x00;
                    Reg86Value = 0x00;
                    Reg87Value = 0x00;

                    RePower();
                    Delay(Delay_Sync);
                    RegisterWrite(4, new uint[8] { 0x80, Reg80Value, 0x81, Reg81Value, 0x82, Reg82Value, 0x83, Reg83Value });
                    Delay(Delay_Sync);
                    BurstRead(0x80, 5, tempReadback);
                    EnterNomalMode();
                    Delay(Delay_Fuse);
                    V0A_Pretrim = AverageVout();

                    #region /* Change Current to IP  */
                    if (ProgramMode == 0)
                    {
                        if (!oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                        {
                            DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                            TrimFinish();
                            return;
                        }
                    }
                    else if (ProgramMode == 1)
                    {
                        dr = MessageBox.Show(String.Format("请将电流升至{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                        if (dr == DialogResult.Cancel)
                        {
                            DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                            PowerOff();
                            RestoreRegValue();
                            return;
                        }
                    }
                    else if (ProgramMode == 2)
                    {
                        MultiSiteSocketSelect(1);   //epio1,3 = high
                        Delay(Delay_Sync);
                        MultiSiteSocketSelect(0);   //set epio1 = high; epio3 = low
                    }
                    #endregion

                    Delay(Delay_Fuse);
                    Vip_Pretrim = AverageVout();
                    DisplayOperateMes("Vout@0A = " + V0A_Pretrim.ToString("F3"));
                    DisplayOperateMes("Vout@IP_1 = " + Vip_Pretrim.ToString("F3"));


                    if (Vip_Pretrim - V0A_Pretrim < 0)
                    {
                        DisplayOperateMes("请确认IP方向！");
                        TrimFinish();
                        return;
                    }
                    else if (Vip_Pretrim - V0A_Pretrim < 0.005d)
                    {
                        DisplayOperateMes("请确认IP是否ON！");
                        TrimFinish();
                        return;
                    }
                    //coarse_PretrimGain = 2.0d * 12.7d / (Vip_Pretrim - V0A_Pretrim);
                    coarse_PretrimGain = TargetVoltage_customer * 12.7d / (Vip_Pretrim - V0A_Pretrim);
                    DisplayOperateMes("coarse_PretrimGain = " + coarse_PretrimGain.ToString("F3"));

                    if (TargetOffset == 2.5)
                    {
                        if (coarse_PretrimGain > 100 && coarse_PretrimGain < 150)
                        {
                            Reg83Value += 0x03;
                            preSetCoareseGainCode = Convert.ToUInt32(LookupCoarseGain_SL620(coarse_PretrimGain / 1.5d, sl620CoarseGainTable));
                            Reg81Value = 0x03 + preSetCoareseGainCode * 16;
                        }
                        else if (coarse_PretrimGain <= 100)
                        {
                            preSetCoareseGainCode = Convert.ToUInt32(LookupCoarseGain_SL620(coarse_PretrimGain, sl620CoarseGainTable));
                            Reg81Value = 0x03 + preSetCoareseGainCode * 16;
                        }
                        else if (coarse_PretrimGain > 150 && coarse_PretrimGain < 200)
                        {
                            Reg83Value += 0x03;
                            Reg80Value += 0x20;
                            preSetCoareseGainCode = Convert.ToUInt32(LookupCoarseGain_SL620(coarse_PretrimGain / 2.0d, sl620CoarseGainTable));
                            Reg81Value = 0x03 + preSetCoareseGainCode * 16;
                        }
                        else
                        {
                            DisplayOperateMes("产品灵敏度要求过高！");
                            TrimFinish();
                            return;
                        }
                    }
                    else if (TargetOffset == 1.65)
                    {
                        if (coarse_PretrimGain > 73 * 1.5)
                        {
                            Reg83Value += 0x03;
                            Reg80Value += 0x20;
                            preSetCoareseGainCode = Convert.ToUInt32(LookupCoarseGain_SL620(coarse_PretrimGain / 2.0d, sl620CoarseGainTable));
                            Reg81Value = 0x03 + preSetCoareseGainCode * 16;


                        }
                        else if (coarse_PretrimGain <= 73)
                        {
                            preSetCoareseGainCode = Convert.ToUInt32(LookupCoarseGain_SL620(coarse_PretrimGain, sl620CoarseGainTable));
                            Reg81Value = 0x03 + preSetCoareseGainCode * 16;
                        }
                        else if (coarse_PretrimGain > 73 * 1.5)
                        {
                            Reg83Value += 0x03;
                            preSetCoareseGainCode = Convert.ToUInt32(LookupCoarseGain_SL620(coarse_PretrimGain / 1.5d, sl620CoarseGainTable));
                            Reg81Value = 0x03 + preSetCoareseGainCode * 16;
                        }
                        else
                        {
                            DisplayOperateMes("产品灵敏度要求过高！");
                            TrimFinish();
                            return;
                        }
                    }

                    RePower();
                    Delay(Delay_Sync);
                    RegisterWrite(4, new uint[8] { 0x80, Reg80Value, 0x81, Reg81Value, 0x82, Reg82Value, 0x83, Reg83Value });
                    Delay(Delay_Sync);
                    BurstRead(0x80, 5, tempReadback);
                    EnterNomalMode();
                    Delay(Delay_Fuse);
                    Vip_Pretrim = AverageVout();
                    DisplayOperateMes("Vout@IP_2 = " + Vip_Pretrim.ToString("F3"));
                    if (Vip_Pretrim < 4.5)
                    {
                        if (preSetCoareseGainCode > 0)
                            preSetCoareseGainCode--;
                        Reg81Value = 0x03 + preSetCoareseGainCode * 16;
                    }
                    else if (Vip_Pretrim > 4.9)
                    {
                        if (preSetCoareseGainCode == 15)
                        {
                            DisplayOperateMes("Saturation!", Color.Red);
                            TrimFinish();
                            return;
                        }
                        else
                        {
                            preSetCoareseGainCode++;
                            Reg81Value = 0x03 + preSetCoareseGainCode * 16;
                        }
                    }

                    #region /* Change Current to 0A */
                    if (ProgramMode == 0)
                    {
                        if (!oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0u))
                        {
                            DisplayOperateMes(string.Format("Set Current to {0}A failed!", 0u));
                            DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                            TrimFinish();
                            return;
                        }
                    }
                    else if (ProgramMode == 1)
                    {
                        dr = MessageBox.Show(String.Format("请将IP降至0A!"), "Try Again", MessageBoxButtons.OKCancel);
                        if (dr == DialogResult.Cancel)
                        {
                            DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                            PowerOff();
                            RestoreRegValue();
                            return;
                        }
                    }
                    else if (ProgramMode == 2)
                    {
                        //set epio1 and epio3 to low
                        MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                        Delay(Delay_Sync);
                        MultiSiteSocketSelect(9);       //set epio1 = low; epio3 = high
                    }
                    #endregion

                    //if (this.cmb_Voffset_PreT.SelectedIndex == 2)
                    {
                        DisplayOperateMes("SL622 half VDD Diff Mode");
                        AutoTrim_SL620_DiffMode();
                    }
                    //else
                    //{
                    //    DisplayOperateMes("SL622 2.5V Single End");
                    //    AutoTrim_SL620_SingleEnd();
                    //}
                    #endregion
                }
                else if (ProductType == 4)
                {
                    #region SC780 routines
                    if (this.cb_iHallOption_AutoTab.SelectedIndex == 2)
                        Reg80Value = 0x80;      //iHall decrease 33%
                    else
                        Reg80Value = 0x00;

                    if (this.cb_ChopCkDis_AutoTab.Checked)
                        Reg83Value = 0x08;
                    else
                        Reg83Value = 0x00;

                    if (this.cb_s3drv_autoTab.Checked)
                        Reg83Value += 0x02;

                    if (this.cb_s2double_AutoTab.Checked)
                        Reg83Value += 0x04;


                    if (this.cmb_PreTrim_SensorDirection.SelectedIndex == 0)
                    {
                        if (this.cmb_Voffset_PreT.SelectedIndex == 0)
                            Reg80Value |= 0x04;
                        else if (this.cmb_Voffset_PreT.SelectedIndex == 1)
                            Reg80Value |= 0x04;
                        else if (this.cmb_Voffset_PreT.SelectedIndex == 2)
                            Reg80Value |= 0x05;
                        else if (this.cmb_Voffset_PreT.SelectedIndex == 3)
                            Reg80Value |= 0x06;
                    }
                    else if (this.cmb_PreTrim_SensorDirection.SelectedIndex == 1)
                    {
                        DisplayOperateMes("Inverted Sensor Direction");
                        if (this.cmb_Voffset_PreT.SelectedIndex == 0)
                            Reg80Value |= 0x00;
                        else if (this.cmb_Voffset_PreT.SelectedIndex == 1)
                            Reg80Value |= 0x00;
                        else if (this.cmb_Voffset_PreT.SelectedIndex == 2)
                            Reg80Value |= 0x01;
                        else if (this.cmb_Voffset_PreT.SelectedIndex == 3)
                            Reg80Value |= 0x02;
                    }


                    double Vip_Pretrim = 0;
                    double V0A_Pretrim = 0;
                    double coarse_PretrimGain = 0;
                    preSetCoareseGainCode = 15;
                    Reg81Value = 0x03 + preSetCoareseGainCode * 16;
                    Reg82Value = Convert.ToUInt32(this.txt_SL620TC_AutoTab.Text, 16);
                    Reg83Value += 0x30;
                    Reg84Value = 0x00;
                    Reg85Value = 0x00;
                    Reg86Value = 0x00;
                    Reg87Value = 0x00;

                    RePower();
                    Delay(Delay_Sync);
                    RegisterWrite(4, new uint[8] { 0x80, Reg80Value, 0x81, Reg81Value, 0x82, Reg82Value, 0x83, Reg83Value });
                    Delay(Delay_Sync);
                    BurstRead(0x80, 5, tempReadback);
                    EnterNomalMode();
                    Delay(Delay_Fuse);
                    V0A_Pretrim = AverageVout();

                    #region /* Change Current to IP  */
                    if (ProgramMode == 0)
                    {
                        if (!oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                        {
                            DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                            TrimFinish();
                            return;
                        }
                    }
                    else if (ProgramMode == 1)
                    {
                        dr = MessageBox.Show(String.Format("请将电流升至{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                        if (dr == DialogResult.Cancel)
                        {
                            DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                            PowerOff();
                            RestoreRegValue();
                            return;
                        }
                    }
                    else if (ProgramMode == 2)
                    {
                        MultiSiteSocketSelect(1);   //epio1,3 = high
                        Delay(Delay_Sync);
                        MultiSiteSocketSelect(0);   //set epio1 = high; epio3 = low
                    }
                    #endregion

                    Delay(Delay_Fuse);
                    Vip_Pretrim = AverageVout();
                    DisplayOperateMes("Vout@0A = " + V0A_Pretrim.ToString("F3"));
                    DisplayOperateMes("Vout@IP_1 = " + Vip_Pretrim.ToString("F3"));


                    if (Vip_Pretrim - V0A_Pretrim < 0)
                    {
                        DisplayOperateMes("请确认IP方向！");
                        TrimFinish();
                        return;
                    }
                    else if (Vip_Pretrim - V0A_Pretrim < 0.005d)
                    {
                        DisplayOperateMes("请确认IP是否ON！");
                        TrimFinish();
                        return;
                    }

                    if (Vip_Pretrim > 4.8)
                    {
                                
                        DisplayOperateMes("编程电流过大，输出饱和！");
                        TrimFinish();
                        return;
                                   
                    }

                    //coarse_PretrimGain = 2.0d * 12.7d / (Vip_Pretrim - V0A_Pretrim);
                    coarse_PretrimGain = TargetVoltage_customer * 12.7d / (Vip_Pretrim - V0A_Pretrim);
                    DisplayOperateMes("coarse_PretrimGain = " + coarse_PretrimGain.ToString("F3"));


                    if(coarse_PretrimGain >= 200)
                    {
                        DisplayOperateMes("产品灵敏度要求过高！");
                        TrimFinish();
                        return;
                    }
                    else if (coarse_PretrimGain < 11)
                    {
                        DisplayOperateMes("产品灵敏度要求过低！");
                        TrimFinish();
                        return;
                    }
                   


                    RePower();
                    Delay(Delay_Sync);
                    RegisterWrite(4, new uint[8] { 0x80, Reg80Value, 0x81, Reg81Value, 0x82, Reg82Value, 0x83, Reg83Value });
                    Delay(Delay_Sync);
                    BurstRead(0x80, 5, tempReadback);
                    EnterNomalMode();
                    Delay(Delay_Fuse);
                    Vip_Pretrim = AverageVout();
                    DisplayOperateMes("Vout@IP_2 = " + Vip_Pretrim.ToString("F3"));
                    if (Vip_Pretrim < 4.5)
                    {
                        if (preSetCoareseGainCode > 0)
                            preSetCoareseGainCode--;
                        Reg81Value = 0x03 + preSetCoareseGainCode * 16;
                    }
                    else if (Vip_Pretrim > 4.9)
                    {
                        if (preSetCoareseGainCode == 15)
                        {
                            DisplayOperateMes("Saturation!", Color.Red);
                            TrimFinish();
                            return;
                        }
                        else
                        {
                            preSetCoareseGainCode++;
                            Reg81Value = 0x03 + preSetCoareseGainCode * 16;
                        }
                    }

                    #region /* Change Current to 0A */
                    if (ProgramMode == 0)
                    {
                        if (!oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0u))
                        {
                            DisplayOperateMes(string.Format("Set Current to {0}A failed!", 0u));
                            DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                            TrimFinish();
                            return;
                        }
                    }
                    else if (ProgramMode == 1)
                    {
                        dr = MessageBox.Show(String.Format("请将IP降至0A!"), "Try Again", MessageBoxButtons.OKCancel);
                        if (dr == DialogResult.Cancel)
                        {
                            DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                            PowerOff();
                            RestoreRegValue();
                            return;
                        }
                    }
                    else if (ProgramMode == 2)
                    {
                        //set epio1 and epio3 to low
                        MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                        Delay(Delay_Sync);
                        MultiSiteSocketSelect(9);       //set epio1 = low; epio3 = high
                    }
                    #endregion

                    Delay(Delay_Fuse);
                    V0A_Pretrim = AverageVout();
                    DisplayOperateMes("Vout@0A_2 = " + V0A_Pretrim.ToString("F3"));

                    if (this.cmb_Voffset_PreT.SelectedIndex == 2)
                    {
                        DisplayOperateMes("SL622 half VDD Signle End");
                        AutoTrim_SL620_SingleEnd_HalfVDD();
                    }
                    else
                    {
                        DisplayOperateMes("SL622 2.5V Single End");
                        AutoTrim_SL620_SingleEnd();
                    }
                    #endregion
                }
                else if (ProductType == 5)
                {
                    #region SC810 routines
                    Reg80Value = 0x00;

                    if (this.cmb_PreTrim_SensorDirection.SelectedIndex == 0)
                    {
                        if (this.cmb_Voffset_PreT.SelectedIndex == 0)
                            Reg80Value |= 0x04;
                        else if (this.cmb_Voffset_PreT.SelectedIndex == 1)
                            Reg80Value |= 0x04;
                        else if (this.cmb_Voffset_PreT.SelectedIndex == 2)
                            Reg80Value |= 0x05;
                        else if (this.cmb_Voffset_PreT.SelectedIndex == 3)
                            Reg80Value |= 0x06;
                        else if (this.cmb_Voffset_PreT.SelectedIndex == 4)
                            Reg80Value |= 0x07;
                    }
                    else if (this.cmb_PreTrim_SensorDirection.SelectedIndex == 1)
                    {
                        DisplayOperateMes("Inverted Sensor Direction");
                        if (this.cmb_Voffset_PreT.SelectedIndex == 0)
                            Reg80Value |= 0x00;
                        else if (this.cmb_Voffset_PreT.SelectedIndex == 1)
                            Reg80Value |= 0x00;
                        else if (this.cmb_Voffset_PreT.SelectedIndex == 2)
                            Reg80Value |= 0x01;
                        else if (this.cmb_Voffset_PreT.SelectedIndex == 3)
                            Reg80Value |= 0x02;
                        else if (this.cmb_Voffset_PreT.SelectedIndex == 4)
                            Reg80Value |= 0x03;
                    }

                    preSetCoareseGainCode = 0;
                    Reg81Value = 0x00 + preSetCoareseGainCode * 16;
                    Reg82Value = Convert.ToUInt32(this.txt_SL620TC_AutoTab.Text, 16);
                    Reg83Value = 0x30;
                    Reg84Value = 0x00;
                    Reg85Value = 0x00;
                    Reg86Value = 0x00;
                    Reg87Value = 0x00;


                    if (this.TargetGain_customer > 80 && this.TargetGain_customer < 120)     //---------------------------------------> 100mv/A
                    {
                        Reg80Value += 0xC0;      //iHall decrease 17%
                        //Reg80Value += 0x80;      //iHall decrease 33%
                        //Reg81Value += 0x0C;         //tcth = 2'b00; vbg = 2'b11
                        Reg81Value += 0x0F;         //tcth = 2'b11; vbg = 2'b11
                        if(!this.cb_CustTc_AutoTab.Checked)
                            Reg82Value = 0x46;
                    }
                    else if (this.TargetGain_customer > 120 && this.TargetGain_customer < 180)     //---------------------------------------> 133mv/A
                    {
                        Reg80Value += 0x00;      //iHall decrease 17%
                        //Reg80Value += 0x80;      //iHall decrease 33%
                        //Reg81Value += 0x0C;         //tcth = 2'b00; vbg = 2'b11
                        Reg81Value += 0x0F;         //tcth = 2'b11; vbg = 2'b11
                        if (!this.cb_CustTc_AutoTab.Checked)
                            Reg82Value = 0x23;
                    }
                    else if (this.TargetGain_customer > 180 && this.TargetGain_customer < 220) //--------------------------------------> 200mv/A
                    {
                        Reg80Value += 0x80;
                        Reg81Value += 0x07;
                        Reg83Value += 0x04;

                        if (!this.cb_CustTc_AutoTab.Checked)
                            Reg82Value = 0x00;
                    }
                    else if (this.TargetGain_customer > 60 && this.TargetGain_customer < 70) //-------->30A
                    {
                        Reg80Value += 0xC0;      //iHall decrease 17%
                        Reg81Value += 0x0F;

                        if (!this.cb_CustTc_AutoTab.Checked)
                            Reg82Value = 0x68;
                    }
                    else if (this.TargetGain_customer == 264)    //------------------------------------->ACS725, 264mV/A
                    {
                        Reg81Value += 0x03;
                        Reg83Value += 0x04;
                    }
                    else
                    {
                        DisplayOperateMes("Customized Gain!");

                        if (this.cb_s2double_AutoTab.Checked)
                            Reg83Value += 0x04;

                        if (this.cb_s3drv_autoTab.Checked)
                            Reg83Value += 0x02;

                        if (this.cb_iHallOption_AutoTab.SelectedIndex == 2)
                            Reg80Value += 0x80;      //iHall decrease 33%

                        if (this.cb_ChopCkDis_AutoTab.Checked)
                            Reg83Value += 0x08;
                    }


                    if (this.cmb_Voffset_PreT.SelectedIndex == 2 || this.cmb_Voffset_PreT.SelectedIndex == 4)
                    {
                        DisplayOperateMes("SC810 half VDD Signle End");
                        AutoTrim_SL620_SingleEnd_HalfVDD();
                    }
                    else
                    {
                        DisplayOperateMes("SC810 2.5V Single End");
                        AutoTrim_SL620_SingleEnd();
                    }
                    #endregion
                }
                else if (ProductType == 6)
                {
                    #region SC810b routines

                    preSetCoareseGainCode = 0;
                    Reg80Value = 0x00;                
                    Reg81Value = 0x00 + preSetCoareseGainCode * 16;
                    Reg82Value = Convert.ToUInt32(this.txt_SL620TC_AutoTab.Text, 16);
                    Reg83Value = 0x30;
                    Reg84Value = 0x00;
                    Reg85Value = 0x00;
                    Reg86Value = 0x00;
                    Reg87Value = 0x00;

                    if (this.cmb_PreTrim_SensorDirection.SelectedIndex == 1)
                    {
                        if (this.cmb_Voffset_PreT.SelectedIndex == 0)
                            Reg80Value |= 0x04;
                        else if (this.cmb_Voffset_PreT.SelectedIndex == 1)
                            Reg80Value |= 0x04;
                        else if (this.cmb_Voffset_PreT.SelectedIndex == 2)
                            Reg80Value |= 0x05;
                        else if (this.cmb_Voffset_PreT.SelectedIndex == 3)
                            Reg80Value |= 0x06;
                        else if (this.cmb_Voffset_PreT.SelectedIndex == 4)
                            Reg80Value |= 0x07;
                    }
                    else if (this.cmb_PreTrim_SensorDirection.SelectedIndex == 0)
                    {
                        DisplayOperateMes("Inverted Sensor Direction");
                        if (this.cmb_Voffset_PreT.SelectedIndex == 0)
                            Reg80Value |= 0x00;
                        else if (this.cmb_Voffset_PreT.SelectedIndex == 1)
                            Reg80Value |= 0x00;
                        else if (this.cmb_Voffset_PreT.SelectedIndex == 2)
                            Reg80Value |= 0x01;
                        else if (this.cmb_Voffset_PreT.SelectedIndex == 3)
                            Reg80Value |= 0x02;
                        else if (this.cmb_Voffset_PreT.SelectedIndex == 4)
                            Reg80Value |= 0x03;
                    }
                   
                    if (this.TargetGain_customer > 80 && this.TargetGain_customer < 120)     //---------------------------------------> 100mv/A
                    {
                        Reg80Value += 0x10;         //4 halls, M + R
                        //Reg80Value += 0x80;      //iHall decrease 33%
                        //Reg81Value += 0x0C;         //tcth = 2'b00; vbg = 2'b11
                        Reg81Value += 0x0F;         //tcth = 2'b11; vbg = 2'b11
                        if (!this.cb_CustTc_AutoTab.Checked)
                            Reg82Value = 0x46;
                    }
                    else if (this.TargetGain_customer > 120 && this.TargetGain_customer < 180)     //---------------------------------------> 133mv/A
                    {
                        Reg80Value += 0x20;      //iHall decrease 17%
                        //Reg80Value += 0x80;      //iHall decrease 33%
                        //Reg81Value += 0x0C;         //tcth = 2'b00; vbg = 2'b11
                        Reg81Value += 0x0F;         //tcth = 2'b11; vbg = 2'b11
                        if (!this.cb_CustTc_AutoTab.Checked)
                            Reg82Value = 0x23;
                    }
                    else if (this.TargetGain_customer > 180 && this.TargetGain_customer < 220) //--------------------------------------> 200mv/A
                    {
                        Reg80Value += 0x20;
                        Reg81Value += 0x07;
                        //Reg83Value += 0x04;

                        if (!this.cb_CustTc_AutoTab.Checked)
                            Reg82Value = 0x00;
                    }
                    else if (this.TargetGain_customer > 60 && this.TargetGain_customer < 70) //-------->30A
                    {
                        Reg80Value += 0xA0;      //iHall decrease 17%
                        Reg81Value += 0x0F;

                        if (!this.cb_CustTc_AutoTab.Checked)
                            Reg82Value = 0x68;
                    }
                    else if (this.TargetGain_customer == 264)    //------------------------------------->ACS725, 264mV/A
                    {
                        Reg81Value += 0x03;
                        Reg83Value += 0x04;
                    }
                    else
                    {
                        DisplayOperateMes("Customized Gain!");

                        if (this.cb_s2double_AutoTab.Checked)
                            Reg83Value += 0x04;

                        if (this.cb_s3drv_autoTab.Checked)
                            Reg83Value += 0x02;

                        if (this.cb_iHallOption_AutoTab.SelectedIndex == 2)
                            Reg80Value += 0x80;      //iHall decrease 33%

                        if (this.cb_ChopCkDis_AutoTab.Checked)
                            Reg83Value += 0x08;
                    }


                    if (this.cmb_Voffset_PreT.SelectedIndex == 2 || this.cmb_Voffset_PreT.SelectedIndex == 4)
                    {
                        DisplayOperateMes("SC810 half VDD Signle End");
                        AutoTrim_SL620_SingleEnd_HalfVDD();
                    }
                    else
                    {
                        DisplayOperateMes("SC810 2.5V Single End");
                        AutoTrim_SL620_SingleEnd();
                    }
                    #endregion
                }
                else
                    return;
            }

            DateTime StopTime = System.DateTime.Now;
            TimeSpan ts = StopTime - StartTime;

            DisplayOperateMes("Program Time Span = " + ts.Minutes.ToString() + "m");
            DisplayOperateMes("Program Time Span = " + ts.Seconds.ToString() + "s");
            #endregion
        }

        private void btn_NewAutomaticaTrim_Click(object sender, EventArgs e)
        {
            //int i = 0;

            //while (true)
            //{
            //    i++;
            //    Delay(100);
            //    if (oneWrie_device.SDPSingalPathReadSot())
            //    {
            //        DisplayOperateMes("SOT is assert! --- " + i.ToString());
            //        Delay(2000);
            //        //oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_WRITE_EOT); //EPIO9
            //        //oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_WRITE_BIN_ONE); //EPIO10
            //        //oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_WRITE_BIN_TWO); //EPIO11
            //        oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_WRITE_BIN_FAIL); //EPIO8
            //        //oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_WRITE_BIN_RECYCLE); //EPIO12
                

            #region Var Definetion
            DialogResult dr;
            DisplayOperateMesClear();

            IP = Convert.ToDouble(this.txt_IP_AutoT.Text);
            TargetOffset = Convert.ToDouble(this.txt_VoutOffset_AutoT.Text);
            TargetGain_customer = Convert.ToDouble(this.txt_TargetGain_AutoT.Text);
            TargetVoltage_customer = Convert.ToDouble(this.txt_TargertVoltage_AutoT.Text);

            Delay_Fuse = Convert.ToInt32(this.txt_IpDelay_AutoT.Text);
            AdcOffset = Convert.ToDouble(this.txt_AdcOffset_AutoT.Text);

            bin2accuracy = Convert.ToDouble(this.txt_BinError_AutoT.Text);
            bin3accuracy = Convert.ToDouble(this.txt_BinError_AutoT.Text);


            #endregion

            #region Check HW connection
            if (!bUsbConnected)
            {
                DisplayOperateMes("Please Confirm HW Connection!", Color.Red);
                return;
            }
            #endregion

            #region IP Initialize
            if (ProgramMode == 0)
            {
                //if (ProgramMode == 0 && bUartInit == false)
                //{

                //UART Initialization
                if (!oneWrie_device.UARTInitilize(9600, 1))
                    DisplayOperateMes("UART Initilize failed!");
                //ding hao
                Delay(Delay_Power);

                //1. Current Remote CTL
                if (!oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_REMOTE, 0))
                    DisplayOperateMes("Set Current Remote failed!");

                Delay(Delay_Power);

                if (!oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, Convert.ToUInt32(IP)))
                    DisplayOperateMes("Set Current to IP failed!");

                Delay(Delay_Power);

                //3. Set Voltage
                if (!oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETVOLT, 6u))
                    DisplayOperateMes(string.Format("Set Voltage to {0}V failed!", 6));

                Delay(Delay_Power);

            }
            else if (ProgramMode == 2)
            {
                MultiSiteSocketSelect(1);   //epio1,3 = high

                if (!bDualRelayIpOn)
                {
                    bDualRelayIpOn = true;
                    dr = MessageBox.Show(String.Format("请将电流升至{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                    {
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        PowerOff();
                        RestoreRegValue();
                        return;
                    }
                }
            }
            #endregion IP Initialize          

            #region Reg Value
            Reg80Value = 0x00;
            Reg81Value = 0x03 + preSetCoareseGainCode * 16;     //TCTH = 2'b11
            Reg82Value = 0x00;
            Reg83Value = 0x30;      //multi-driven mode
            Reg84Value = 0x00;
            Reg85Value = 0x00;
            Reg86Value = 0x00;
            Reg87Value = 0x00;
            #endregion

            #region Vout0A Option
            if (this.cb_V0AOption_AutoTab.SelectedIndex == 0)
            {
                Reg80Value &= 0xFC;
                Reg80Value |= 0x00;
            }
            else if (this.cb_V0AOption_AutoTab.SelectedIndex == 1)
            {
                Reg80Value &= 0xFC;
                Reg80Value |= 0x01;
            }
            else if (this.cb_V0AOption_AutoTab.SelectedIndex == 2)
            {
                Reg80Value &= 0xFC;
                Reg80Value |= 0x02;
            }
            else if (this.cb_V0AOption_AutoTab.SelectedIndex == 3)
            {
                Reg80Value &= 0xFC;
                Reg80Value |= 0x03;
            }
            #endregion

            #region iHall option
            if (this.cb_iHallOption_AutoTab.SelectedIndex == 0)
            {
                Reg80Value &= 0x3F;
                Reg80Value |= 0x00;
            }
            else if (this.cb_iHallOption_AutoTab.SelectedIndex == 1)
            {
                Reg80Value &= 0x3F;
                Reg80Value |= 0x40;
            }
            else if (this.cb_iHallOption_AutoTab.SelectedIndex == 2)
            {
                Reg80Value &= 0x3F;
                Reg80Value |= 0x80;
            }
            else if (this.cb_iHallOption_AutoTab.SelectedIndex == 3)
            {
                Reg80Value &= 0x3F;
                Reg80Value |= 0xC0;
            }
            #endregion

            #region Gain option
            //gain * 2
            if (this.cb_s2double_AutoTab.Checked)
                Reg83Value |= 0x04;
            else
                Reg83Value &= 0xFB;

            //gain * 1.25
            if(this.cb_s3drv_autoTab.Checked)
                Reg83Value |= 0x02;
            else
                Reg83Value &= 0xFD;

            //gain / 2
            if (this.cb_ChopCkDis_AutoTab.Checked)
                Reg83Value |= 0x08;
            else
                Reg83Value &= 0xF7;
            #endregion

            #region Sensing direction
            if (this.cb_InvertSens_AutoTab.Checked)
            {
                Reg80Value &= 0xF3;
                Reg80Value |= 0x04;
            }
            else
            {
                Reg80Value &= 0xF3;
            }
            #endregion

            #region BigCap
            if (this.cb_BigCap_AutoTab.Checked)
            {
                Reg83Value &= 0xFE;
                Reg83Value |= 0x01;
            }
            else
            {
                Reg83Value &= 0xFE;
            }

            #endregion

            #region Fast Start Up
            if (this.cb_FastStart_AutoTab.Checked)
            {
                Reg83Value &= 0x3F;
                Reg83Value |= 0xC0;
            }
            else
            {
                Reg83Value &= 0x3F;
            }
            #endregion

            #region cust TC
            if (this.cb_CustTc_AutoTab.Checked)
                Reg82Value = Convert.ToUInt32(this.txt_SL620TC_AutoTab.Text, 16);
            else
                Reg82Value = 0x00;
            #endregion cust TC

            #region Product Series
            //813/820 anti ext magenatic
            if (this.cb_ProductSeries_AutoTab.SelectedIndex == 4 || this.cb_ProductSeries_AutoTab.SelectedIndex == 5)
            {
                if (this.cb_8xHalls_AutoTab.Checked)            //8x halls, M4 - L2 - R2
                {
                    Reg80Value &= 0xCF;
                    Reg80Value |= 0x20;
                }
                else                                            //4 halls, M2 - L2
                {
                    Reg80Value &= 0xCF;
                    Reg80Value |= 0x10;
                }
            }
            #endregion         

            #region Program mode

            DateTime StartTime = System.DateTime.Now;

            // diff mode for SL622
            if (this.cb_ProductSeries_AutoTab.SelectedIndex == 1)
            {
                DisplayOperateMes("SL622 half VDD Diff Mode");
                AutoTrim_SL620_DiffMode();
            }
            else if (this.cb_ProductSeries_AutoTab.SelectedIndex == 6)
            {
                DisplayOperateMes("2.5V Single End of silicon A");
                AutoTrim_SL620A_SingleEnd();
            }
            //single end mode
            else
            {
                if (this.cb_V0AOption_AutoTab.SelectedIndex == 1 || this.cb_V0AOption_AutoTab.SelectedIndex == 3)
                {
                    DisplayOperateMes("half VDD Signle End");
                    AutoTrim_SL620_SingleEnd_HalfVDD();
                }
                else if (this.cb_V0AOption_AutoTab.SelectedIndex == 0)
                {
                    DisplayOperateMes("2.5V Single End");
                    AutoTrim_SL620_SingleEnd();
                }
                else if (this.cb_V0AOption_AutoTab.SelectedIndex == 2)
                {
                    DisplayOperateMes("1.65V Single End");
                    AutoTrim_SL620_1V65();
                }
            }

            DateTime StopTime = System.DateTime.Now;
            TimeSpan ts = StopTime - StartTime;

            DisplayOperateMes("Program Time Span = " + ts.Minutes.ToString() + "m");
            DisplayOperateMes("Program Time Span = " + ts.Seconds.ToString() + "s");
            #endregion

                //}
                //else
                //    DisplayOperateMes("No SOT! --- " + i.ToString());
            //}
        }



        //Single Site
        private void AutoTrim_SL510_SingleSite()
        {
            #region Define Parameters
            DialogResult dr;
            bool bMarginal = false;
            bool bSafety = false;
            //uint[] tempReadback = new uint[5];
            double dVout_0A_Temp = 0;
            double dVip_Target = TargetOffset + TargetVoltage_customer;
            double dGainTestMinusTarget = 1;
            double dGainTest = 0;
            ModuleAttribute sDUT;
            sDUT.dIQ = 0;
            sDUT.dVoutIPNative = 0;
            sDUT.dVout0ANative = 0;
            sDUT.dVoutIPMiddle = 0;
            sDUT.dVout0AMiddle = 0;
            sDUT.dVoutIPTrimmed = 0;
            sDUT.dVout0ATrimmed = 0;
            sDUT.iErrorCode = 00;
            sDUT.bDigitalCommFail = false;
            sDUT.bNormalModeFail = false;
            sDUT.bReadMarginal = false;
            sDUT.bReadSafety = false;
            sDUT.bTrimmed = false;

            // PARAMETERS DEFINE FOR MULTISITE
            uint idut = 0;
            uint uDutCount = 16;
            //bool bValidRound = false;
            //bool bSecondCurrentOn = false;
            double dModuleCurrent = 0;
            bool[] bGainBoost = new bool[16];
            bool[] bDutValid = new bool[16];
            bool[] bDutNoNeedTrim = new bool[16];
            uint[] uDutTrimResult = new uint[16];
            double[] dMultiSiteVoutIP = new double[16];
            double[] dMultiSiteVout0A = new double[16];

            /* autoAdaptingGoughGain algorithm*/
            double autoAdaptingGoughGain = 0;
            double autoAdaptingPresionGain = 0;
            double tempG1 = 0;
            double tempG2 = 0;
            double dGainPreset = 0;
            int Ix_forAutoAdaptingRoughGain = 0;
            int Ix_forAutoAdaptingPresionGain = 0;

            int ix_forOffsetIndex_Rough = 0;
            int ix_forOffsetIndex_Rough_Complementary = 0;
            double dMultiSiteVout_0A_Complementary = 0;

            DisplayOperateMes("\r\n**************" + DateTime.Now.ToString() + "**************");
            DisplayOperateMes("Start...");
            this.txt_Status_AutoTab.ForeColor = Color.Black;
            this.txt_Status_AutoTab.Text = "START!";

            for (uint i = 0; i < uDutCount; i++)
            {
                dMultiSiteVoutIP[i] = 0d;
                dMultiSiteVout0A[i] = 0d;

                MultiSiteReg0[i] = Reg80Value;
                MultiSiteReg1[i] = Reg81Value;
                MultiSiteReg2[i] = Reg82Value;
                MultiSiteReg3[i] = Reg83Value;

                MultiSiteRoughGainCodeIndex[i] = Ix_ForRoughGainCtrl;

                uDutTrimResult[i] = 0u;
                bDutNoNeedTrim[i] = false;
                bDutValid[i] = false;
                bGainBoost[i] = false;
            }
            #endregion Define Parameters

            #region Get module current
            //clear log
            DisplayOperateMesClear();
            /*  power on */
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_5V);
            RePower();
            Delay(Delay_Sync);
            this.txt_Status_AutoTab.Text = "Trimming!";
            /* Get module current */
            if (oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VCS))
            {
                if (bAutoTrimTest)
                    DisplayOperateMes("Set ADC VIN to VCS");
            }
            else
            {
                DisplayOperateMes("Set ADC VIN to VCS failed", Color.Red);
                PowerOff();
                return;
            }
            Delay(Delay_Sync);
            if (oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_SET_CURRENT_SENCE))
            {
                if (bAutoTrimTest)
                    DisplayOperateMes("Set ADC current sensor");
            }

            this.txt_ModuleCurrent_EngT.Text = GetModuleCurrent().ToString("F1");
            this.txt_ModuleCurrent_PreT.Text = this.txt_ModuleCurrent_EngT.Text;


            dModuleCurrent = GetModuleCurrent();
            sDUT.dIQ = dModuleCurrent;
            if (dCurrentDownLimit > dModuleCurrent)
            {
                DisplayOperateMes("Module " + " current is " + dModuleCurrent.ToString("F3"), Color.Red);
                //uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_CURRENT_ABNORMAL;
                PowerOff();
                //PrintDutAttribute(sDUT);
                MessageBox.Show(String.Format("电流偏低，检查模组是否连接！"), "Warning", MessageBoxButtons.OK);
                return;
            }
            else if (dModuleCurrent > dCurrentUpLimit)
            {
                DisplayOperateMes("Module " + " current is " + dModuleCurrent.ToString("F3"), Color.Red);
                uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_CURRENT_HIGH;
                PowerOff();
                sDUT.iErrorCode = uDutTrimResult[idut];
                PrintDutAttribute(sDUT);
                //MessageBox.Show(String.Format("电流异常，模块短路或损坏！"), "Error", MessageBoxButtons.OK);
                this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                this.txt_Status_AutoTab.Text = "短路!";
                return;
            }
            else
                DisplayOperateMes("Module " + " current is " + dModuleCurrent.ToString("F3"));

            #endregion Get module current

            #region Saturation judgement

            Delay(Delay_Sync);
            EnterTestMode();

            #region Comm test
            BurstRead(0x80, 5, tempReadback);
            if (tempReadback[0] + tempReadback[1] + tempReadback[2] + tempReadback[3] + tempReadback[4] != 0)
            {
                DisplayOperateMes("DUT" + " has some bits Blown!", Color.Red);
                uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_TRIMMED_SOMEBITS;
                TrimFinish();
                sDUT.bTrimmed = false;
                sDUT.iErrorCode = uDutTrimResult[idut];
                PrintDutAttribute(sDUT);
                this.txt_Status_AutoTab.ForeColor = Color.Red;
                this.txt_Status_AutoTab.Text = "FAIL!";
                return;
            }
            #endregion

            #region Pre-Bin 
            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            BurstRead(0x80, 5, tempReadback);
            if (tempReadback[0] != MultiSiteReg0[idut] || tempReadback[1] != MultiSiteReg1[idut]
                || tempReadback[2] != MultiSiteReg2[idut] || tempReadback[3] != MultiSiteReg3[idut])
            {
                if (tempReadback[0] + tempReadback[1] + tempReadback[2] + tempReadback[3] + tempReadback[4] == 0)
                {
                    RePower();
                    Delay(Delay_Sync);
                    oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);
                    Delay(Delay_Sync);
                    oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);
                    Delay(Delay_Fuse);
                    dMultiSiteVout0A[idut] = AverageVout();
                    if (dMultiSiteVout0A[idut] < 2.55 && dMultiSiteVout0A[idut] > 1.6)
                    {
                        DisplayOperateMes("DUT Trimmed!", Color.Red);
                        uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_TRIMMRD_ALREADY;
                        TrimFinish();
                        sDUT.bDigitalCommFail = true;
                        sDUT.bTrimmed = true;
                        sDUT.iErrorCode = uDutTrimResult[idut];
                        PrintDutAttribute(sDUT);
                        //MessageBox.Show(String.Format("模组已编程，交至研发部！"), "Error", MessageBoxButtons.OK);
                        this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                        this.txt_Status_AutoTab.Text = "混料!";
                        return;
                    }
                    else if (dMultiSiteVout0A[idut] < 0.5 && dMultiSiteVout0A[idut] > 4.5)
                    {
                        DisplayOperateMes("VOUT Short!", Color.Red);
                        uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_VOUT_SHORT;
                        TrimFinish();
                        sDUT.bDigitalCommFail = true;
                        sDUT.iErrorCode = uDutTrimResult[idut];
                        PrintDutAttribute(sDUT);
                        //MessageBox.Show(String.Format("输出管脚短路！", Color.YellowGreen), "Warning", MessageBoxButtons.OK);
                        this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                        this.txt_Status_AutoTab.Text = "短路!";
                        return;
                    }
                    else
                    {
                        DisplayOperateMes("DUT digital communication fail!", Color.Red);
                        uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_COMM_FAIL;
                        TrimFinish();
                        sDUT.bDigitalCommFail = true;
                        sDUT.iErrorCode = uDutTrimResult[idut];
                        PrintDutAttribute(sDUT);
                        //MessageBox.Show(String.Format("输出管脚短路！", Color.YellowGreen), "Warning", MessageBoxButtons.OK);
                        this.txt_Status_AutoTab.ForeColor = Color.Red;
                        this.txt_Status_AutoTab.Text = "FAIL!";
                        return;
                    }
                }
                else
                {
                    DisplayOperateMes("DUT digital communication fail!", Color.Red);
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_COMM_FAIL;
                    TrimFinish();
                    sDUT.bDigitalCommFail = true;
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    //MessageBox.Show(String.Format("输出管脚短路！", Color.YellowGreen), "Warning", MessageBoxButtons.OK);
                    this.txt_Status_AutoTab.ForeColor = Color.Red;
                    this.txt_Status_AutoTab.Text = "FAIL!";
                    return;
                }
            }

            #endregion
            /* Get vout @ IP */
            EnterNomalMode();

            #region /* Change Current to IP  */
            if (ProgramMode == 0)
            {
                if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                    DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", IP));
                else
                {
                    DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                    TrimFinish();
                    return;
                }
            }
            else if (ProgramMode == 1)
            {
                dr = MessageBox.Show(String.Format("请将电流升至{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    PowerOff();
                    RestoreRegValue();
                    return;
                }
            }
            else if (ProgramMode == 2)
            {
                //IP ON
                MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                Delay(Delay_Sync);
                MultiSiteSocketSelect(0);       //set epio1 = high; epio3 = low
            }
            #endregion

            Delay(Delay_Fuse);
            dMultiSiteVoutIP[idut] = AverageVout();
            sDUT.dVoutIPNative = dMultiSiteVoutIP[idut];
            DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));

            #region /*Judge PreSet gain; delta Vout target >= delta Vout test * 86.07% */
            if (dMultiSiteVoutIP[idut] > saturationVout)
            {
                if (ProgramMode == 0)
                {
                    oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0);
                    Delay(Delay_Sync);
                    oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, 5);
                    Delay(Delay_Sync);
                    if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                        DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", 5));
                    else
                    {
                        DisplayOperateMes(string.Format("Set Current to {0}A failed!", 5));
                        TrimFinish();
                        return;
                    }
                }
                else if (ProgramMode == 1)
                {
                    dr = MessageBox.Show(String.Format("请将电流升至{0}A", 5), "Change Current", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                    {
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        PowerOff();
                        RestoreRegValue();
                        return;
                    }
                }
                else if (ProgramMode == 2)
                {
                    //IP ON
                    MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                    Delay(Delay_Sync);
                    MultiSiteSocketSelect(0);       //set epio1 = high; epio3 = low
                }

                Delay(Delay_Fuse);
                dMultiSiteVoutIP[idut] = AverageVout();
                sDUT.dVoutIPNative = dMultiSiteVoutIP[idut];
                DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));

                //set current back to IP
                if (ProgramMode == 0 )
                {
                    oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0);
                    Delay(Delay_Sync);
                    oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, Convert.ToUInt32(IP));
                }

                if (dMultiSiteVoutIP[idut] > saturationVout)
                {
                    DisplayOperateMes("Module" + " Vout is VDD!", Color.Red);
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_VOUT_VDD;
                    TrimFinish();
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                    this.txt_Status_AutoTab.Text = "短路!";
                    return;
                }
                else
                {
                    //dr = MessageBox.Show(String.Format("输出饱和，交研发部重新编程！"), "Warning", MessageBoxButtons.OK);
                    TrimFinish();
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_VOUT_SATURATION;
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                    this.txt_Status_AutoTab.Text = "饱和!";
                    return;
                }
            }
            else if (dMultiSiteVoutIP[idut] < minimumVoutIP)
            {
                DisplayOperateMes("Module" + " Vout is too Low!", Color.Red);
                uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_VOUT_LOW;
                TrimFinish();
                sDUT.iErrorCode = uDutTrimResult[idut];
                PrintDutAttribute(sDUT);
                this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                this.txt_Status_AutoTab.Text = "短路!";
                return;
            }
            #endregion

            #endregion Saturation judgement

            #region Get Vout@0A
            /* Change Current to 0A */
            //3. Set Voltage
            if (ProgramMode == 0)
            {
                //if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, 0u))
                if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0u))
                    DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", 0u));
                else
                {
                    DisplayOperateMes(string.Format("Set Current to {0}A failed!", 0u));
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    TrimFinish();
                    return;
                }
            }
            else if(ProgramMode == 1)
            {
                dr = MessageBox.Show(String.Format("请将IP降至0A!"), "Try Again", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    PowerOff();
                    RestoreRegValue();
                    return;
                }
            }
            else if (ProgramMode == 2)
            {
                //IP OFF
                MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                Delay(Delay_Sync);
                MultiSiteSocketSelect(9);       //set epio1 = low; epio3 = high
            }

            Delay(Delay_Fuse);
            dMultiSiteVout0A[idut] = AverageVout();
            sDUT.dVout0ANative = dMultiSiteVout0A[idut];
            DisplayOperateMes("Vout" + " @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

            if (dMultiSiteVoutIP[idut] < dMultiSiteVout0A[idut])
            {
                TrimFinish();
                //PrintDutAttribute(sDUT);
                MessageBox.Show(String.Format("请确认IP方向!"), "Try Again", MessageBoxButtons.OK);
                return;
            }
            else if (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut] < VoutIPThreshold)
            {
                TrimFinish();
                //PrintDutAttribute(sDUT);
                MessageBox.Show(String.Format("请确认电流为{0}A!!!", IP), "Try Again", MessageBoxButtons.OK);
                return;
            }

            if (TargetOffset == 2.5)
            {
                if (dMultiSiteVout0A[idut] < 2.25 || dMultiSiteVout0A[idut] > 2.8)
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_OFFSET_ABN;
                    TrimFinish();
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    this.txt_Status_AutoTab.ForeColor = Color.Red;
                    this.txt_Status_AutoTab.Text = "FAIL!";
                    return;
                }
            }
            else if (TargetOffset == 1.65)
            {
                if (dMultiSiteVout0A[idut] < 1.0 || dMultiSiteVout0A[idut] > 2.5)
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_OFFSET_ABN;
                    TrimFinish();
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    this.txt_Status_AutoTab.ForeColor = Color.Red;
                    this.txt_Status_AutoTab.Text = "FAIL!";
                    return;
                }
            }

            #endregion  Get Vout@0A

            #region No need Trim case
            if ((TargetOffset - 0.001) <= dMultiSiteVout0A[idut] && dMultiSiteVout0A[idut] <= (TargetOffset + 0.001)
                && (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= (TargetVoltage_customer + 0.001)
                && (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= (TargetVoltage_customer - 0.001))
            {
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_EXT);
                Delay(Delay_Sync);
                RePower();
                //Delay(Delay_Sync);
                EnterTestMode();
                RegisterWrite(5, new uint[10] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 
                    0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut], 0x84, 0x07 });
                BurstRead(0x80, 5, tempReadback);
                /* fuse */
                FuseClockOn(DeviceAddress, (double)num_UD_pulsewidth_ow_EngT.Value, (double)numUD_pulsedurationtime_ow_EngT.Value);
                DisplayOperateMes("Processing...");
                //Delay(Delay_Fuse);
                ReloadPreset();
                Delay(Delay_Sync);
                BurstRead(0x80, 5, tempReadback);
                Delay(Delay_Sync);
                /* Margianl read, compare with writed code; 
                    * if ( = ), go on
                    * else bMarginal = true; */
                MarginalReadPreset();
                Delay(Delay_Sync);
                BurstRead(0x80, 5, tempReadback);
                bMarginal = false;

                if (bMASK)
                {
                    if (((tempReadback[0] & 0xE0) != (MultiSiteReg0[idut] & 0xE0)) | (tempReadback[1] & 0x81) != (MultiSiteReg1[idut] & 0x81) |
                        (tempReadback[2] & 0x99) != (MultiSiteReg2[idut] & 0x99) |
                        (tempReadback[3] & 0x83) != (MultiSiteReg3[idut] & 0x83) | (tempReadback[4] < 1))
                        bMarginal = true;
                }
                else
                {
                    if (((tempReadback[0] & 0xFF) != (MultiSiteReg0[idut] & 0xFF)) | (tempReadback[1] & 0xFF) != (MultiSiteReg1[idut] & 0xFF) |
                        (tempReadback[2] & 0xFF) != (MultiSiteReg2[idut] & 0xFF) |
                        (tempReadback[3] & 0xFF) != (MultiSiteReg3[idut] & 0xFF) | (tempReadback[4] < 1))
                        bMarginal = true;
                }

                if (bSAFEREAD)
                {
                    //Delay(Delay_Sync);
                    SafetyReadPreset();
                    Delay(Delay_Sync);
                    BurstRead(0x80, 5, tempReadback);
                    bSafety = false;
                    if (((tempReadback[0] & 0xFF) != (MultiSiteReg0[idut] & 0xFF)) | (tempReadback[1] & 0xFF) != (MultiSiteReg1[idut] & 0xFF) |
                            (tempReadback[2] & 0xFF) != (MultiSiteReg2[idut] & 0xFF) | 
                            (tempReadback[3] & 0xFF) != (MultiSiteReg3[idut] & 0xFF) | (tempReadback[4] < 1))
                        bSafety = true;
                }

                //capture Vout
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_5V);
                RePower();
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);
                Delay(Delay_Sync);
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);

                Delay(Delay_Sync);
                dMultiSiteVout0A[idut] = AverageVout();
                sDUT.dVout0ATrimmed = dMultiSiteVout0A[idut];
                DisplayOperateMes("Vout" + " @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

                /* Change Current to IP  */
                if (ProgramMode == 0)
                {
                    if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                        DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", IP));
                    else
                    {
                        DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        TrimFinish();
                        return;
                    }
                }
                else if(ProgramMode == 1)
                {
                    dr = MessageBox.Show(String.Format("请将电流升至{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                    {
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        PowerOff();
                        RestoreRegValue();
                        return;
                    }
                }
                else if (ProgramMode == 2)
                {
                    //IP ON
                    MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                    Delay(Delay_Sync);
                    MultiSiteSocketSelect(0);       //set epio1 = high; epio3 = low
                }

                Delay(Delay_Fuse);
                dMultiSiteVoutIP[idut] = AverageVout();
                sDUT.dVoutIPTrimmed = dMultiSiteVoutIP[idut];
                DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));
                //oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_5V);

                sDUT.bReadMarginal = bMarginal;
                sDUT.bReadSafety = bSafety;

                if (!(bMarginal | bSafety))
                {
                    DisplayOperateMes("DUT" + idut.ToString() + "Pass! Bin Normal");
                    //uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_NORMAL;
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_1;
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    this.txt_Status_AutoTab.ForeColor = Color.Green;
                    this.txt_Status_AutoTab.Text = "PASS!";
                    DisplayOperateMes("Bin" + " = " + uDutTrimResult[idut].ToString());
                    DisplayOperateMes("Marginal Read ->" + bMarginal.ToString());
                    DisplayOperateMes("Safety REad ->" + bSafety.ToString());
                    MultiSiteDisplayResult(uDutTrimResult);
                    TrimFinish();
                    PrintDutAttribute(sDUT);
                    return;
                }
                else
                {
                    DisplayOperateMes("DUT" + idut.ToString() + "Pass! Bin Mriginal");
                    //uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_MARGINAL;
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_4;
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    this.txt_Status_AutoTab.ForeColor = Color.Green;
                    if (bMRE)
                        this.txt_Status_AutoTab.Text = "FAIL!";
                    else
                        this.txt_Status_AutoTab.Text = "PASS!";

                    DisplayOperateMes("Bin" + " = " + uDutTrimResult[idut].ToString());
                    DisplayOperateMes("Marginal Read ->" + bMarginal.ToString());
                    DisplayOperateMes("Safety Read ->" + bSafety.ToString());
                    MultiSiteDisplayResult(uDutTrimResult);
                    TrimFinish();
                    PrintDutAttribute(sDUT);
                    return;
                }
            }


            #endregion No need Trim case

            #region For low sensitivity case, with IP

            dGainTest = 1000d * (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) / IP;
            if (dGainTest < (TargetGain_customer * ThresholdOfGain))
            {
                dGainTestMinusTarget = dGainTest / TargetGain_customer;
                dGainPreset = RoughTable_Customer[0][MultiSiteRoughGainCodeIndex[idut]] / 100d;

                if (this.cmb_IPRange_PreT.SelectedItem.ToString() == "1.5x610")
                {
                    if (dGainTestMinusTarget >= dGainPreset)
                    {
                        MultiSiteRoughGainCodeIndex[idut] = (uint)LookupRoughGain_Customer
                            (TargetGain_customer * 100d / dGainTest * dGainPreset, RoughTable_Customer);
                        /* Rough Gain Code*/
                        bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
                        MultiSiteReg0[idut] &= ~bit_op_mask;
                        MultiSiteReg0[idut] |= Convert.ToUInt32(RoughTable_Customer[1][MultiSiteRoughGainCodeIndex[idut]]);

                        bit_op_mask = bit0_Mask;
                        MultiSiteReg1[idut] &= ~bit_op_mask;
                        MultiSiteReg1[idut] |= Convert.ToUInt32(RoughTable_Customer[2][MultiSiteRoughGainCodeIndex[idut]]);
                    }
                    else
                    {
                        DisplayOperateMes("DUT" + idut.ToString() + " Sensitivity is NOT enough!", Color.Red);
                        bDutValid[idut] = false;
                        uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_LOW_SENSITIVITY;
                        TrimFinish();
                        sDUT.iErrorCode = uDutTrimResult[idut];
                        //this.txt_Status_AutoTab.ForeColor = Color.Red;
                        //this.txt_Status_AutoTab.Text = "MOA!";
                        PrintDutAttribute(sDUT);
                        //dr = MessageBox.Show(String.Format("灵敏度过低，交研发部重新编程！"), "Warning", MessageBoxButtons.OK);
                        this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                        this.txt_Status_AutoTab.Text = "低敏!";
                        return;
                    }

                }
                else
                {
                    if (dGainTestMinusTarget >= dGainPreset)
                    {
                        MultiSiteRoughGainCodeIndex[idut] = (uint)LookupRoughGain_Customer
                            (TargetGain_customer * 100d / dGainTest * dGainPreset, RoughTable_Customer);
                        /* Rough Gain Code*/
                        bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
                        MultiSiteReg0[idut] &= ~bit_op_mask;
                        MultiSiteReg0[idut] |= Convert.ToUInt32(RoughTable_Customer[1][MultiSiteRoughGainCodeIndex[idut]]);

                        bit_op_mask = bit0_Mask;
                        MultiSiteReg1[idut] &= ~bit_op_mask;
                        MultiSiteReg1[idut] |= Convert.ToUInt32(RoughTable_Customer[2][MultiSiteRoughGainCodeIndex[idut]]);
                    }
                    else
                    {
                        if (dGainTest * 1.5 / dGainPreset >= (TargetGain_customer * ThresholdOfGain))
                        {
                            MultiSiteRoughGainCodeIndex[idut] = (uint)LookupRoughGain_Customer((TargetGain_customer 
                                * 100d / (dGainTest * 1.5d) * dGainPreset), RoughTable_Customer);
                            MultiSiteRoughGainCodeIndex[idut] -= 1;
                            MultiSiteReg3[idut] |= 0xC0;
                            /* Rough Gain Code*/
                            bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
                            MultiSiteReg0[idut] &= ~bit_op_mask;
                            MultiSiteReg0[idut] |= Convert.ToUInt32(RoughTable_Customer[1][MultiSiteRoughGainCodeIndex[idut]]);

                            bit_op_mask = bit0_Mask;
                            MultiSiteReg1[idut] &= ~bit_op_mask;
                            MultiSiteReg1[idut] |= Convert.ToUInt32(RoughTable_Customer[2][MultiSiteRoughGainCodeIndex[idut]]);
                        }
                        else
                        {
                            DisplayOperateMes("DUT" + idut.ToString() + " Sensitivity is NOT enough!", Color.Red);
                            bDutValid[idut] = false;
                            uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_LOW_SENSITIVITY;
                            TrimFinish();
                            sDUT.iErrorCode = uDutTrimResult[idut];
                            //this.txt_Status_AutoTab.ForeColor = Color.Red;
                            //this.txt_Status_AutoTab.Text = "MOA!";
                            PrintDutAttribute(sDUT);
                            //dr = MessageBox.Show(String.Format("灵敏度过低，交研发部重新编程！"), "Warning", MessageBoxButtons.OK);
                            this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                            this.txt_Status_AutoTab.Text = "低敏!";
                            return;
                        }
                    }
                }


                DisplayOperateMes("RoughGainCodeIndex of DUT" + " = " + MultiSiteRoughGainCodeIndex[idut].ToString("F0"));
                DisplayOperateMes("SelectedRoughGain = " + RoughTable_Customer[0][MultiSiteRoughGainCodeIndex[idut]].ToString());
                DisplayOperateMes("CalcCode:");
                DisplayOperateMes("0x80 = 0x" + MultiSiteReg0[idut].ToString("X2"));
                DisplayOperateMes("0x81 = 0x" + MultiSiteReg1[idut].ToString("X2"));
                DisplayOperateMes("0x82 = 0x" + MultiSiteReg2[idut].ToString("X2"));
                DisplayOperateMes("0x83 = 0x" + MultiSiteReg3[idut].ToString("X2"));

                /*  power on */
                RePower();
                EnterTestMode();
                RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
                BurstRead(0x80, 5, tempReadback);
                /* Get vout @ IP */
                EnterNomalMode();

                /* Change Current to IP  */
                if (ProgramMode == 0)
                {
                    if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                        DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", IP));
                    else
                    {
                        DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        TrimFinish();
                        return;
                    }
                }
                else if (ProgramMode == 1)
                {
                    dr = MessageBox.Show(String.Format("请将电流升至{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                    {
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        PowerOff();
                        RestoreRegValue();
                        return;
                    }
                }
                else if (ProgramMode == 2)
                {
                    //IP ON
                    MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                    Delay(Delay_Sync);
                    MultiSiteSocketSelect(0);       //set epio1 = high; epio3 = low
                }

                Delay(Delay_Fuse);
                dMultiSiteVoutIP[idut] = AverageVout();
                sDUT.dVoutIPMiddle = dMultiSiteVoutIP[idut];
                DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));


                /*Judge PreSet gain; delta Vout target >= delta Vout test * 86.07% */
                if (dMultiSiteVoutIP[idut] > saturationVout)
                {
                    //decrease gain preset
                    MultiSiteRoughGainCodeIndex[idut] -= 1;
                    /* Rough Gain Code*/
                    bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
                    MultiSiteReg0[idut] &= ~bit_op_mask;
                    MultiSiteReg0[idut] |= Convert.ToUInt32(RoughTable_Customer[1][MultiSiteRoughGainCodeIndex[idut]]);

                    bit_op_mask = bit0_Mask;
                    MultiSiteReg1[idut] &= ~bit_op_mask;
                    MultiSiteReg1[idut] |= Convert.ToUInt32(RoughTable_Customer[2][MultiSiteRoughGainCodeIndex[idut]]);

                    /*  power on */
                    RePower();
                    EnterTestMode();
                    RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
                    BurstRead(0x80, 5, tempReadback);
                    /* Get vout @ IP */
                    EnterNomalMode();
                    Delay(Delay_Fuse);
                    dMultiSiteVoutIP[idut] = AverageVout();
                    sDUT.dVoutIPMiddle = dMultiSiteVoutIP[idut];
                    DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));

                    /*Judge PreSet gain; delta Vout target >= delta Vout test * 86.07% */
                    if (dMultiSiteVoutIP[idut] > saturationVout)
                    {
                        DisplayOperateMes("Module" + " Vout is SATURATION!", Color.Red);
                        uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_VOUT_SATURATION;
                        TrimFinish();
                        sDUT.iErrorCode = uDutTrimResult[idut];
                        //this.txt_Status_AutoTab.ForeColor = Color.Red;
                        //this.txt_Status_AutoTab.Text = "MOA!";
                        PrintDutAttribute(sDUT);
                        //dr = MessageBox.Show(String.Format("输出饱和，交研发部重新编程！"), "Warning", MessageBoxButtons.OK);
                        this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                        this.txt_Status_AutoTab.Text = "饱和!";
                        return;
                    }
                }

                /* Change Current to 0A */
                if (ProgramMode == 0)
                {
                    if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0u))
                        DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", 0u));
                    else
                    {
                        DisplayOperateMes(string.Format("Set Current to {0}A failed!", 0u));
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        TrimFinish();
                        return;
                    }
                }
                else if(ProgramMode == 1)
                {
                    dr = MessageBox.Show(String.Format("请将电流降至0A"), "Change Current", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                    {
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        PowerOff();
                        RestoreRegValue();
                        return;
                    }
                }
                else if (ProgramMode == 2)
                {
                    //IP OFF
                    MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                    Delay(Delay_Sync);
                    MultiSiteSocketSelect(9);       //set epio1 = low; epio3 = high
                }

                /*  power on */
                Delay(Delay_Fuse);
                this.txt_Status_AutoTab.Text = "Processing!";
                dMultiSiteVout0A[idut] = AverageVout();
                sDUT.dVout0AMiddle = dMultiSiteVout0A[idut];
                DisplayOperateMes("Vout" + " @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

                //V0A is abnormal
                //if( Math.Abs(sDUT.dVout0AMiddle - sDUT.dVout0ANative) > 0.005 )
                //{
                //    dr = MessageBox.Show(String.Format("Vout @ 0A is abnormal"), "Warning!", MessageBoxButtons.OK);
                //    if (dr == DialogResult.OK)
                //    {
                //        DisplayOperateMes("V0A abnormal, Rebuild rough gain code for low gain case!");
                //        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                //        PowerOff();
                //        RestoreReg80ToReg83Value();
                //        return;
                //    }
                //}
            }

            #endregion For low sensitivity case, with IP

            #region Adapting algorithm

            tempG1 = RoughTable_Customer[0][MultiSiteRoughGainCodeIndex[idut]] / 100d;
            tempG2 = (TargetGain_customer / ((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) / IP)) / 1000d;

            autoAdaptingGoughGain = tempG1 * tempG2 * 100d;
            DisplayOperateMes("IdealGoughGain = " + autoAdaptingGoughGain.ToString("F4"));

            Ix_forAutoAdaptingRoughGain = LookupRoughGain_Customer(autoAdaptingGoughGain, RoughTable_Customer);
            autoAdaptingPresionGain = 100d * autoAdaptingGoughGain / RoughTable_Customer[0][Ix_forAutoAdaptingRoughGain];
            if (autoAdaptingPresionGain > 100.5)    //10mV
            {
                DisplayOperateMes( "AdaptingPresionGain = " + autoAdaptingPresionGain.ToString("F3"));
                this.txt_Status_AutoTab.ForeColor = Color.Red;
                this.txt_Status_AutoTab.Text = "FAIL!";
                Delay(10);
                TrimFinish();
                return;
            }
            Ix_forAutoAdaptingPresionGain = LookupPreciseGain_Customer(autoAdaptingPresionGain, PreciseTable_Customer);
            if (bAutoTrimTest)
            {
                DisplayOperateMes("IP = " + IP.ToString("F0"));
                DisplayOperateMes("TargetGain_customer" + idut.ToString() + " = " + TargetGain_customer.ToString("F4"));
                DisplayOperateMes("(dMultiSiteVoutIP - dMultiSiteVout0A)/IP = " + ((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) / IP).ToString("F4"));
                DisplayOperateMes("tempG1" + idut.ToString() + " = " + tempG1.ToString("F4"));
                DisplayOperateMes("tempG2" + idut.ToString() + " = " + tempG2.ToString("F4"));
                DisplayOperateMes("Ix_forAutoAdaptingRoughGain" + idut.ToString() + " = " + Ix_forAutoAdaptingRoughGain.ToString("F0"));
                DisplayOperateMes("Ix_forAutoAdaptingPresionGain" + idut.ToString() + " = " + Ix_forAutoAdaptingPresionGain.ToString("F0"));
                DisplayOperateMes("autoAdaptingGoughGain" + idut.ToString() + " = " + RoughTable_Customer[0][Ix_forAutoAdaptingRoughGain].ToString("F4"));
                DisplayOperateMes("autoAdaptingPresionGain" + idut.ToString() + " = " + PreciseTable_Customer[0][Ix_forAutoAdaptingPresionGain].ToString("F4"));
            }

            /* Rough Gain Code*/
            bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
            MultiSiteReg0[idut] &= ~bit_op_mask;
            MultiSiteReg0[idut] |= Convert.ToUInt32(RoughTable_Customer[1][Ix_forAutoAdaptingRoughGain]);

            bit_op_mask = bit0_Mask;
            MultiSiteReg1[idut] &= ~bit_op_mask;
            MultiSiteReg1[idut] |= Convert.ToUInt32(RoughTable_Customer[2][Ix_forAutoAdaptingRoughGain]);

            if (bAutoTrimTest)
            {
                DisplayOperateMes("Rough Gain RegValue80 = 0x" + MultiSiteReg0[idut].ToString("X2"));
                DisplayOperateMes("Rough Gain RegValue81 = 0x" + MultiSiteReg1[idut].ToString("X2"));
            }

            /* Presion Gain Code*/
            bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask | bit3_Mask | bit4_Mask;
            MultiSiteReg0[idut] &= ~bit_op_mask;
            MultiSiteReg0[idut] |= Convert.ToUInt32(PreciseTable_Customer[1][Ix_forAutoAdaptingPresionGain]);

            if (bAutoTrimTest)
            {
                DisplayOperateMes("Precesion Gain RegValue80 = 0x" + MultiSiteReg0[idut].ToString("X2"));
                DisplayOperateMes("***new add approach***");
            }

            RePower();
            EnterTestMode();
            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            EnterNomalMode();

            #region /* Change Current to IP  */
            if (ProgramMode == 0)
            {
                //if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, Convert.ToUInt32(IP)))
                if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                    DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", IP));
                else
                {
                    DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    TrimFinish();
                    return;
                }
            }
            else if (ProgramMode == 1)
            {
                dr = MessageBox.Show(String.Format("请将电流升至{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    PowerOff();
                    RestoreRegValue();
                    return;
                }
            }
            else if (ProgramMode == 2)
            {
                //IP ON
                MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                Delay(Delay_Sync);
                MultiSiteSocketSelect(0);       //set epio1 = high; epio3 = low
            }
            #endregion

            Delay(Delay_Fuse);
            dMultiSiteVoutIP[idut] = AverageVout();
            DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));

            #region /* Change Current to 0A */
            if (ProgramMode == 0)
            {
                //if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, 0u))
                if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0u))
                    DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", 0u));
                else
                {
                    DisplayOperateMes(string.Format("Set Current to {0}A failed!", 0u));
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    TrimFinish();
                    return;
                }
            }
            else if (ProgramMode == 1)
            {
                dr = MessageBox.Show(String.Format("请将电流降至0A"), "Change Current", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    PowerOff();
                    RestoreRegValue();
                    return;
                }
            }
            else if (ProgramMode == 2)
            {
                //IP OFF
                MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                Delay(Delay_Sync);
                MultiSiteSocketSelect(9);       //set epio1 = low; epio3 = high
            }
            #endregion

            /*  power on */
            Delay(Delay_Fuse);
            //this.txt_Status_AutoTab.Text = "Processing!";
            dMultiSiteVout0A[idut] = AverageVout();
            DisplayOperateMes("DUT" + " Vout @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

            #region Offset Fine Tuning
            if (((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) * 1000 - TargetGain_customer * IP) > 4 )
            {
                /* Presion Gain Code*/
                bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask | bit3_Mask | bit4_Mask;
                MultiSiteReg0[idut] &= ~bit_op_mask;
                MultiSiteReg0[idut] |= Convert.ToUInt32(PreciseTable_Customer[1][Ix_forAutoAdaptingPresionGain + 1]);
            }
            else if (((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) * 1000 - TargetGain_customer * IP) < -4 )
            {
                /* Presion Gain Code*/
                bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask | bit3_Mask | bit4_Mask;
                MultiSiteReg0[idut] &= ~bit_op_mask;
                MultiSiteReg0[idut] |= Convert.ToUInt32(PreciseTable_Customer[1][Ix_forAutoAdaptingPresionGain - 1]);
            }

            #endregion

            if (bAutoTrimTest)
                DisplayOperateMes("***new approach end***");

            RePower();
            EnterTestMode();
            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            EnterNomalMode();
            Delay(Delay_Fuse);
            dMultiSiteVout0A[idut] = AverageVout();
            dVout_0A_Temp = dMultiSiteVout0A[idut];
            if (bAutoTrimTest)
                DisplayOperateMes("DUT" + " Vout @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

            /* Offset trim code calculate */
            Vout_0A = dMultiSiteVout0A[idut];

            //btn_offset_Click(null, null);
            uint[] regTMultiSite = new uint[3];

            MultiSiteOffsetAlg(regTMultiSite);
            MultiSiteReg1[idut] |= regTMultiSite[0];
            MultiSiteReg2[idut] |= regTMultiSite[1];
            MultiSiteReg3[idut] |= regTMultiSite[2];

            bit_op_mask = bit2_Mask | bit3_Mask | bit4_Mask | bit5_Mask;
            ix_forOffsetIndex_Rough = 0;
            ix_forOffsetIndex_Rough = LookupOffsetIndex(MultiSiteReg3[idut] & bit_op_mask, OffsetTableB_Customer);
            ix_forOffsetIndex_Rough_Complementary = ix_forOffsetIndex_Rough;
            DisplayOperateMes("\r\nProcessing...");

            /* Repower on 5V */
            RePower();
            EnterTestMode();
            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            EnterNomalMode();
            Delay(Delay_Fuse);
            dMultiSiteVout0A[idut] = AverageVout();
            sDUT.dVout0AMiddle = dMultiSiteVout0A[idut];
            DisplayOperateMes("MultiSiteReg3 = 0x" + MultiSiteReg3[idut].ToString("X2"));
            DisplayOperateMes("ix_forOffsetIndex_Rough = " + ix_forOffsetIndex_Rough.ToString());
            DisplayOperateMes("dMultiSiteVout0A = " + dMultiSiteVout0A[idut].ToString("F3"));

            //V0A is abnormal
            //if (Math.Abs(sDUT.dVout0AMiddle - dVout_0A_Temp) > 0.005)
            //{
            //    dr = MessageBox.Show(String.Format("Vout @ 0A is abnormal"), "Warning!", MessageBoxButtons.OKCancel);
            //    if (dr == DialogResult.Cancel)
            //    {
            //        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
            //        PowerOff();
            //        RestoreReg80ToReg83Value();
            //        return;
            //    }
            //}

            if (dMultiSiteVout0A[idut] > TargetOffset)
            {
                if (ix_forOffsetIndex_Rough == 7)
                    ix_forOffsetIndex_Rough = 7;
                else if (ix_forOffsetIndex_Rough == 15)
                    ix_forOffsetIndex_Rough = 0;
                else
                    ix_forOffsetIndex_Rough += 1;
            }
            else if (dMultiSiteVout0A[idut] < TargetOffset)
            {
                if (ix_forOffsetIndex_Rough == 8)
                    ix_forOffsetIndex_Rough = 8;
                else if (ix_forOffsetIndex_Rough == 0)
                    ix_forOffsetIndex_Rough = 15;
                else
                    ix_forOffsetIndex_Rough -= 1;
            }
            bit_op_mask = bit2_Mask | bit3_Mask | bit4_Mask | bit5_Mask;
            MultiSiteReg3[idut] &= ~bit_op_mask;
            MultiSiteReg3[idut] |= Convert.ToUInt32(OffsetTableB_Customer[1][ix_forOffsetIndex_Rough]);

            RePower();
            EnterTestMode();
            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            EnterNomalMode();
            Delay(Delay_Fuse);
            dMultiSiteVout_0A_Complementary = AverageVout();
            DisplayOperateMes("\r\nMultiSiteReg3 = 0x" + MultiSiteReg3[idut].ToString("X2"));
            DisplayOperateMes("ix_forOffsetIndex_Rough = " + ix_forOffsetIndex_Rough.ToString());
            DisplayOperateMes("dMultiSiteVout_0A_Complementary = " + dMultiSiteVout_0A_Complementary.ToString("F3"));

            //V0A is abnormal
            //if (Math.Abs(sDUT.dVout0AMiddle - dMultiSiteVout_0A_Complementary) > 0.005)
            //{
            //    dr = MessageBox.Show(String.Format("Vout @ 0A is abnormal"), "Warning!", MessageBoxButtons.OKCancel);
            //    if (dr == DialogResult.Cancel)
            //    {
            //        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
            //        PowerOff();
            //        RestoreReg80ToReg83Value();
            //        return;
            //    }
            //}

            if (Math.Abs(dMultiSiteVout0A[idut] - TargetOffset) < Math.Abs(dMultiSiteVout_0A_Complementary - TargetOffset))
            {
                bit_op_mask = bit2_Mask | bit3_Mask | bit4_Mask | bit5_Mask;
                MultiSiteReg3[idut] &= ~bit_op_mask;
                MultiSiteReg3[idut] |= Convert.ToUInt32(OffsetTableB_Customer[1][ix_forOffsetIndex_Rough_Complementary]);
                DisplayOperateMes("Last MultiSiteReg3 = 0x" + MultiSiteReg3[idut].ToString("X2"));
            }
            else
            {
                bit_op_mask = bit2_Mask | bit3_Mask | bit4_Mask | bit5_Mask;
                MultiSiteReg3[idut] &= ~bit_op_mask;
                MultiSiteReg3[idut] |= Convert.ToUInt32(OffsetTableB_Customer[1][ix_forOffsetIndex_Rough]);
                DisplayOperateMes("Last MultiSiteReg3 = 0x" + MultiSiteReg3[idut].ToString("X2"));
            }

            DisplayOperateMes("Processing...");

            #endregion Adapting algorithm

            #region Fuse
            //Fuse
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_EXT);
            RePower();
            EnterTestMode();
            RegisterWrite(5, new uint[10] { 0x80, MultiSiteReg0[idut], 
                0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 
                0x83, MultiSiteReg3[idut], 0x84, 0x07 });
            BurstRead(0x80, 5, tempReadback);
            FuseClockOn(DeviceAddress, (double)num_UD_pulsewidth_ow_EngT.Value, (double)numUD_pulsedurationtime_ow_EngT.Value);
            DisplayOperateMes("Trimming...");
            //Delay(Delay_Fuse);

            ReloadPreset();
            Delay(Delay_Sync);
            BurstRead(0x80, 5, tempReadback);
            if (tempReadback[4] == 0)
            {
                RePower();
                EnterTestMode();
                RegisterWrite(5, new uint[10] { 0x80, MultiSiteReg0[idut], 
                    0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 
                    0x83, MultiSiteReg3[idut], 0x84, 0x07 });
                BurstRead(0x80, 5, tempReadback);
                FuseClockOn(DeviceAddress, (double)num_UD_pulsewidth_ow_EngT.Value, (double)numUD_pulsedurationtime_ow_EngT.Value);
                DisplayOperateMes("Trimming...");
                //Delay(Delay_Fuse);
            }
            Delay(Delay_Sync);
            /* Margianl read, compare with writed code; 
                * if ( = ), go on
                * else bMarginal = true; */
            MarginalReadPreset();
            Delay(Delay_Sync);
            BurstRead(0x80, 5, tempReadback);
            bMarginal = false;
            if (bMASK)
            {
                if (((tempReadback[0] & 0xE0) != (MultiSiteReg0[idut] & 0xE0)) | (tempReadback[1] & 0x81) != (MultiSiteReg1[idut] & 0x81) |
                    (tempReadback[2] & 0x99) != (MultiSiteReg2[idut] & 0x99) | (tempReadback[3] & 0x83) != (MultiSiteReg3[idut] & 0x83) | 
                    (tempReadback[4] < 1))
                    bMarginal = true;
            }
            else
            {
                if (((tempReadback[0] & 0xFF) != (MultiSiteReg0[idut] & 0xFF)) | (tempReadback[1] & 0xFF) != (MultiSiteReg1[idut] & 0xFF) |
                        (tempReadback[2] & 0xFF) != (MultiSiteReg2[idut] & 0xFF) | (tempReadback[3] & 0xFF) != (MultiSiteReg3[idut] & 0xFF) | 
                        (tempReadback[4] < 1))
                    bMarginal = true;
            }

            if (bSAFEREAD)
            {
                //Delay(Delay_Sync);
                SafetyReadPreset();
                Delay(Delay_Sync);
                BurstRead(0x80, 5, tempReadback);
                bSafety = false;
                if (((tempReadback[0] & 0xFF) != (MultiSiteReg0[idut] & 0xFF)) | (tempReadback[1] & 0xFF) != (MultiSiteReg1[idut] & 0xFF) |
                        (tempReadback[2] & 0xFF) != (MultiSiteReg2[idut] & 0xFF) | (tempReadback[3] & 0xFF) != (MultiSiteReg3[idut] & 0xFF) | 
                        (tempReadback[4] < 1))
                    bSafety = true;
            }

            sDUT.bReadMarginal = bMarginal;
            sDUT.bReadSafety = bSafety;

            if (!(bMarginal | bSafety))
            {
                DisplayOperateMes("DUT" + "Pass! Bin Normal");
                uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_NORMAL;
            }
            else
            {
                DisplayOperateMes("DUT" + "Pass! Bin Mriginal");
                uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_MARGINAL;
            }
            //sDUT.iErrorCode = uDutTrimResult[idut];

            #endregion

            #region Bin
            /* Repower on 5V */
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_5V);
            RePower();
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);
            Delay(Delay_Sync);
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);

            Delay(Delay_Sync);
            dMultiSiteVout0A[idut] = AverageVout();
            sDUT.dVout0ATrimmed = dMultiSiteVout0A[idut];
            DisplayOperateMes("Vout" + " @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

            #region /* Change Current to IP  */
            if (ProgramMode == 0)
            {
                if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                    DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", IP));
                else
                {
                    DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    TrimFinish();
                    return;
                }
            }
            else if (ProgramMode == 1)
            {
                dr = MessageBox.Show(String.Format("请将电流升至{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    PowerOff();
                    RestoreRegValue();
                    return;
                }
            }
            else if (ProgramMode == 2)
            {
                //IP ON
                MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                Delay(Delay_Sync);
                MultiSiteSocketSelect(0);       //set epio1 = high; epio3 = low
            }
            #endregion

            Delay(Delay_Fuse);
            dMultiSiteVoutIP[idut] = AverageVout();
            sDUT.dVoutIPTrimmed = dMultiSiteVoutIP[idut];
            DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));

            #region Bin Sort
            if (uDutTrimResult[idut] == (uint)PRGMRSULT.DUT_BIN_MARGINAL)
            {
                if (TargetOffset * (1 - 0.001) <= dMultiSiteVout0A[idut] && dMultiSiteVout0A[idut] <= TargetOffset * (1 + 0.001) && 
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= TargetVoltage_customer * (1 + 0.001) && 
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= TargetVoltage_customer * (1 - 0.001))
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_4;
                    //this.txt_Status_AutoTab.ForeColor = Color.Green;
                    if (bMRE)
                    {
                        this.txt_Status_AutoTab.ForeColor = Color.Red;
                        this.txt_Status_AutoTab.Text = "FAIL!";
                    }
                    else
                    {
                        this.txt_Status_AutoTab.ForeColor = Color.Green;
                        this.txt_Status_AutoTab.Text = "PASS!";
                    }
                }
                else if (TargetOffset * (1 - bin2accuracy / 100d) <= dMultiSiteVout0A[idut] && 
                    dMultiSiteVout0A[idut] <= TargetOffset * (1 + bin2accuracy / 100d) && 
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= TargetVoltage_customer * (1 + bin3accuracy / 100d) && 
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= TargetVoltage_customer * (1 - bin3accuracy / 100d))
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_5;
                    //this.txt_Status_AutoTab.ForeColor = Color.Green;
                    if (bMRE)
                    {
                        this.txt_Status_AutoTab.ForeColor = Color.Red;
                        this.txt_Status_AutoTab.Text = "FAIL!";
                    }
                    else
                    {
                        this.txt_Status_AutoTab.ForeColor = Color.Green;
                        this.txt_Status_AutoTab.Text = "PASS!";
                    }
                }
                else if (TargetOffset * (1 - bin2accuracy / 100d) <= dMultiSiteVout0A[idut] && 
                    dMultiSiteVout0A[idut] <= TargetOffset * (1 + bin2accuracy / 100d) && 
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= TargetVoltage_customer * (1 + bin3accuracy / 100d) && 
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= TargetVoltage_customer * (1 - bin3accuracy / 100d))
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_6;
                    //this.txt_Status_AutoTab.ForeColor = Color.Green;
                    if (bMRE)
                    {
                        this.txt_Status_AutoTab.ForeColor = Color.Red;
                        this.txt_Status_AutoTab.Text = "FAIL!";
                    }
                    else
                    {
                        this.txt_Status_AutoTab.ForeColor = Color.Green;
                        this.txt_Status_AutoTab.Text = "PASS!";
                    }
                }
                else
                {
                    this.txt_Status_AutoTab.ForeColor = Color.Red;
                    this.txt_Status_AutoTab.Text = "FAIL!";
                }

            }

            /* bin1,2,3 */
            //if ((!bMarginal) && (!bSafety))
            else if (uDutTrimResult[idut] == (uint)PRGMRSULT.DUT_BIN_NORMAL)
            {
                if (TargetOffset * (1 - 0.001) <= dMultiSiteVout0A[idut] && dMultiSiteVout0A[idut] <= TargetOffset * (1 + 0.001) && 
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= TargetVoltage_customer * (1 + 0.001) && 
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= TargetVoltage_customer * (1 - 0.001))
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_1;
                    this.txt_Status_AutoTab.ForeColor = Color.Green;
                    this.txt_Status_AutoTab.Text = "PASS!";
                }
                else if (TargetOffset * (1 - bin2accuracy / 100d) <= dMultiSiteVout0A[idut] && 
                    dMultiSiteVout0A[idut] <= TargetOffset * (1 + bin2accuracy / 100d) && 
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= TargetVoltage_customer * (1 + bin3accuracy / 100d) && 
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= TargetVoltage_customer * (1 - bin3accuracy / 100d))
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_2;
                    this.txt_Status_AutoTab.ForeColor = Color.Green;
                    this.txt_Status_AutoTab.Text = "PASS!";
                }
                else if (TargetOffset * (1 - bin2accuracy / 100d) <= dMultiSiteVout0A[idut] && 
                    dMultiSiteVout0A[idut] <= TargetOffset * (1 + bin2accuracy / 100d) && 
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= TargetVoltage_customer * (1 + bin3accuracy / 100d) && 
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= TargetVoltage_customer * (1 - bin3accuracy / 100d))
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_3;
                    this.txt_Status_AutoTab.ForeColor = Color.Green;
                    this.txt_Status_AutoTab.Text = "PASS!";
                }
                else
                {
                    this.txt_Status_AutoTab.ForeColor = Color.Red;
                    this.txt_Status_AutoTab.Text = "FAIL!";
                }
            }
            #endregion 

            #endregion Bin

            #region Display Result and Reset parameters
            DisplayOperateMes("Bin" + " = " + uDutTrimResult[idut].ToString());
            MultiSiteDisplayResult(uDutTrimResult);
            TrimFinish();
            sDUT.iErrorCode = uDutTrimResult[idut];
            PrintDutAttribute(sDUT);
            DisplayOperateMes("Next...");
            #endregion Display Result and Reset parameters
        }
        //Single Site
        private void AutoTrim_SL510_DiffMode()
        {
            #region Define Parameters
            DialogResult dr;
            bool bMarginal = false;
            bool bSafety = false;
            //uint[] tempReadback = new uint[5];
            double dVout_0A_Temp = 0;
            double dVip_Target = TargetOffset + TargetVoltage_customer;
            double dGainTestMinusTarget = 1;
            double dGainTest = 0;
            ModuleAttribute sDUT;
            sDUT.dIQ = 0;
            sDUT.dVoutIPNative = 0;
            sDUT.dVout0ANative = 0;
            sDUT.dVoutIPMiddle = 0;
            sDUT.dVout0AMiddle = 0;
            sDUT.dVoutIPTrimmed = 0;
            sDUT.dVout0ATrimmed = 0;
            sDUT.iErrorCode = 00;
            sDUT.bDigitalCommFail = false;
            sDUT.bNormalModeFail = false;
            sDUT.bReadMarginal = false;
            sDUT.bReadSafety = false;
            sDUT.bTrimmed = false;

            // PARAMETERS DEFINE FOR MULTISITE
            uint idut = 0;
            uint uDutCount = 16;
            //bool bValidRound = false;
            //bool bSecondCurrentOn = false;
            double dModuleCurrent = 0;
            bool[] bGainBoost = new bool[16];
            bool[] bDutValid = new bool[16];
            bool[] bDutNoNeedTrim = new bool[16];
            uint[] uDutTrimResult = new uint[16];
            double[] dMultiSiteVoutIP = new double[16];
            double[] dMultiSiteVout0A = new double[16];

            /* autoAdaptingGoughGain algorithm*/
            double autoAdaptingGoughGain = 0;
            double autoAdaptingPresionGain = 0;
            double tempG1 = 0;
            double tempG2 = 0;
            double dGainPreset = 0;
            int Ix_forAutoAdaptingRoughGain = 0;
            int Ix_forAutoAdaptingPresionGain = 0;

            int ix_forOffsetIndex_Rough = 0;
            int ix_forOffsetIndex_Rough_Complementary = 0;
            double dMultiSiteVout_0A_Complementary = 0;

            DisplayOperateMes("\r\n**************" + DateTime.Now.ToString() + "**************");
            DisplayOperateMes("Start...");
            this.txt_Status_AutoTab.ForeColor = Color.Black;
            this.txt_Status_AutoTab.Text = "START!";

            for (uint i = 0; i < uDutCount; i++)
            {
                dMultiSiteVoutIP[i] = 0d;
                dMultiSiteVout0A[i] = 0d;

                MultiSiteReg0[i] = Reg80Value;
                MultiSiteReg1[i] = Reg81Value;
                MultiSiteReg2[i] = Reg82Value;
                MultiSiteReg3[i] = Reg83Value;
                MultiSiteReg4[i] = Reg84Value;
                MultiSiteReg5[i] = Reg85Value;
                MultiSiteReg6[i] = Reg86Value;
                MultiSiteReg7[i] = Reg87Value;

                MultiSiteRoughGainCodeIndex[i] = Ix_ForRoughGainCtrl;

                uDutTrimResult[i] = 0u;
                bDutNoNeedTrim[i] = false;
                bDutValid[i] = false;
                bGainBoost[i] = false;
            }
            #endregion Define Parameters

            #region Get module current
            //clear log
            DisplayOperateMesClear();
            /*  power on */
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_5V);
            RePower();
            Delay(Delay_Sync);
            this.txt_Status_AutoTab.Text = "Trimming!";
            /* Get module current */
            if (oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VCS))
            {
                if (bAutoTrimTest)
                    DisplayOperateMes("Set ADC VIN to VCS");
            }
            else
            {
                DisplayOperateMes("Set ADC VIN to VCS failed", Color.Red);
                PowerOff();
                return;
            }
            Delay(Delay_Sync);
            if (oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_SET_CURRENT_SENCE))
            {
                if (bAutoTrimTest)
                    DisplayOperateMes("Set ADC current sensor");
            }

            this.txt_ModuleCurrent_EngT.Text = GetModuleCurrent().ToString("F1");
            this.txt_ModuleCurrent_PreT.Text = this.txt_ModuleCurrent_EngT.Text;


            dModuleCurrent = GetModuleCurrent();
            sDUT.dIQ = dModuleCurrent;
            if (dCurrentDownLimit > dModuleCurrent)
            {
                DisplayOperateMes("Module " + " current is " + dModuleCurrent.ToString("F3"), Color.Red);
                //uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_CURRENT_ABNORMAL;
                PowerOff();
                //PrintDutAttribute(sDUT);
                MessageBox.Show(String.Format("电流偏低，检查模组是否连接！"), "Warning", MessageBoxButtons.OK);
                return;
            }
            else if (dModuleCurrent > dCurrentUpLimit)
            {
                DisplayOperateMes("Module " + " current is " + dModuleCurrent.ToString("F3"), Color.Red);
                uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_CURRENT_HIGH;
                PowerOff();
                sDUT.iErrorCode = uDutTrimResult[idut];
                PrintDutAttribute(sDUT);
                //MessageBox.Show(String.Format("电流异常，模块短路或损坏！"), "Error", MessageBoxButtons.OK);
                this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                this.txt_Status_AutoTab.Text = "短路!";
                return;
            }
            else
                DisplayOperateMes("Module " + " current is " + dModuleCurrent.ToString("F3"));

            #endregion Get module current

            #region Saturation judgement


            //Redundency delay in case of power off failure.
            Delay(Delay_Sync);
            EnterTestMode();
            //Delay(Delay_Sync);
            BurstRead(0x80, 5, tempReadback);
            if (tempReadback[0] + tempReadback[1] + tempReadback[2] + tempReadback[3] + tempReadback[4] != 0)
            {
                DisplayOperateMes("DUT" + " has some bits Blown!", Color.Red);
                uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_TRIMMED_SOMEBITS;
                TrimFinish();
                sDUT.bTrimmed = false;
                sDUT.iErrorCode = uDutTrimResult[idut];
                PrintDutAttribute(sDUT);
                this.txt_Status_AutoTab.ForeColor = Color.Red;
                this.txt_Status_AutoTab.Text = "FAIL!";
                return;
            }

            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            BurstRead(0x80, 5, tempReadback);
            if (tempReadback[0] != MultiSiteReg0[idut] || tempReadback[1] != MultiSiteReg1[idut]
                || tempReadback[2] != MultiSiteReg2[idut] || tempReadback[3] != MultiSiteReg3[idut])
            {
                if (tempReadback[0] + tempReadback[1] + tempReadback[2] + tempReadback[3] + tempReadback[4] == 0)
                {
                    RePower();
                    Delay(Delay_Sync);
                    oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);
                    Delay(Delay_Sync);
                    oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);
                    Delay(Delay_Fuse);
                    dMultiSiteVout0A[idut] = GetVout();
                    if (dMultiSiteVout0A[idut] < 4.5 && dMultiSiteVout0A[idut] > 1.5)
                    {
                        DisplayOperateMes("DUT Trimmed!", Color.Red);
                        uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_TRIMMRD_ALREADY;
                        TrimFinish();
                        sDUT.bDigitalCommFail = true;
                        sDUT.bTrimmed = true;
                        sDUT.iErrorCode = uDutTrimResult[idut];
                        PrintDutAttribute(sDUT);
                        //MessageBox.Show(String.Format("模组已编程，交至研发部！"), "Error", MessageBoxButtons.OK);
                        this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                        this.txt_Status_AutoTab.Text = "混料!";
                        return;
                    }
                    else
                    {
                        DisplayOperateMes("VOUT Short!", Color.Red);
                        uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_VOUT_SHORT;
                        TrimFinish();
                        sDUT.bDigitalCommFail = true;
                        sDUT.iErrorCode = uDutTrimResult[idut];
                        PrintDutAttribute(sDUT);
                        //MessageBox.Show(String.Format("输出管脚短路！", Color.YellowGreen), "Warning", MessageBoxButtons.OK);
                        this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                        this.txt_Status_AutoTab.Text = "短路!";
                        return;
                    }
                }
                else
                {
                    DisplayOperateMes("DUT digital communication fail!", Color.Red);
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_COMM_FAIL;
                    TrimFinish();
                    sDUT.bDigitalCommFail = true;
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    //MessageBox.Show(String.Format("输出管脚短路！", Color.YellowGreen), "Warning", MessageBoxButtons.OK);
                    this.txt_Status_AutoTab.ForeColor = Color.Red;
                    this.txt_Status_AutoTab.Text = "FAIL!";
                    return;
                }
            }
            /* Get vout @ IP */
            EnterNomalMode();

            /* Change Current to IP  */
            if (ProgramMode == 0)
            {
                if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                    DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", IP));
                else
                {
                    DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                    TrimFinish();
                    return;
                }
            }
            else if (ProgramMode == 1)
            {
                dr = MessageBox.Show(String.Format("请将电流升至{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    PowerOff();
                    RestoreRegValue();
                    return;
                }
            }
            else if (ProgramMode == 2)
            {
                //IP ON
                MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                Delay(Delay_Sync);
                MultiSiteSocketSelect(0);       //set epio1 = high; epio3 = low
            }


            //oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);
            Delay(Delay_Fuse);
            //dMultiSiteVoutIP[idut] = AverageVout();
            dMultiSiteVoutIP[idut] = GetVout();
            TargetOffset = GetVref();
            sDUT.dVoutIPNative = dMultiSiteVoutIP[idut];
            DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));

            /*Judge PreSet gain; delta Vout target >= delta Vout test * 86.07% */
            if (dMultiSiteVoutIP[idut] > saturationVout)
            {
                if (ProgramMode == 0)
                {
                    oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0);
                    Delay(Delay_Sync);
                    oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, 5);
                    Delay(Delay_Sync);
                    if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                        DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", 5));
                    else
                    {
                        DisplayOperateMes(string.Format("Set Current to {0}A failed!", 5));
                        TrimFinish();
                        return;
                    }
                }
                else if (ProgramMode == 1)
                {
                    dr = MessageBox.Show(String.Format("请将电流升至{0}A", 5), "Change Current", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                    {
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        PowerOff();
                        RestoreRegValue();
                        return;
                    }
                }
                else if (ProgramMode == 2)
                {
                    //IP ON
                    MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                    Delay(Delay_Sync);
                    MultiSiteSocketSelect(0);       //set epio1 = high; epio3 = low
                }

                Delay(Delay_Fuse);
                dMultiSiteVoutIP[idut] = GetVout();
                sDUT.dVoutIPNative = dMultiSiteVoutIP[idut];
                DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));

                //set current back to IP
                if (ProgramMode == 0)
                {
                    oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0);
                    Delay(Delay_Sync);
                    oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, Convert.ToUInt32(IP));
                }

                if (dMultiSiteVoutIP[idut] > saturationVout)
                {
                    DisplayOperateMes("Module" + " Vout is VDD!", Color.Red);
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_VOUT_VDD;
                    TrimFinish();
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                    this.txt_Status_AutoTab.Text = "短路!";
                    return;
                }
                else
                {
                    //dr = MessageBox.Show(String.Format("输出饱和，交研发部重新编程！"), "Warning", MessageBoxButtons.OK);
                    TrimFinish();
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_VOUT_SATURATION;
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                    this.txt_Status_AutoTab.Text = "饱和!";
                    return;
                }
            }
            else if (dMultiSiteVoutIP[idut] < minimumVoutIP)
            {
                DisplayOperateMes("Module" + " Vout is too Low!", Color.Red);
                uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_VOUT_LOW;
                TrimFinish();
                sDUT.iErrorCode = uDutTrimResult[idut];
                PrintDutAttribute(sDUT);
                this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                this.txt_Status_AutoTab.Text = "短路!";
                return;
            }

            #endregion Saturation judgement

            #region Get Vout@0A
            /* Change Current to 0A */
            if (ProgramMode == 0)
            {
                if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0u))
                    DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", 0u));
                else
                {
                    DisplayOperateMes(string.Format("Set Current to {0}A failed!", 0u));
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    TrimFinish();
                    return;
                }
            }
            else if (ProgramMode == 1)
            {
                dr = MessageBox.Show(String.Format("请将IP降至0A!"), "Try Again", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    PowerOff();
                    RestoreRegValue();
                    return;
                }
            }
            else if (ProgramMode == 2)
            {
                //IP OFF
                MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                Delay(Delay_Sync);
                MultiSiteSocketSelect(9);       //set epio1 = low; epio3 = high
            }

            Delay(Delay_Fuse);
            dMultiSiteVout0A[idut] = GetVout();
            sDUT.dVout0ANative = dMultiSiteVout0A[idut];
            DisplayOperateMes("Vout" + " @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));
            DisplayOperateMes("TargetOffset = " + TargetOffset.ToString("F3"));

            if (dMultiSiteVoutIP[idut] < dMultiSiteVout0A[idut])
            {
                TrimFinish();
                //PrintDutAttribute(sDUT);
                MessageBox.Show(String.Format("请确认IP方向!"), "Try Again", MessageBoxButtons.OK);
                return;
            }
            else if (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut] < VoutIPThreshold)
            {
                TrimFinish();
                //PrintDutAttribute(sDUT);
                MessageBox.Show(String.Format("请确认电流为{0}A!!!", IP), "Try Again", MessageBoxButtons.OK);
                return;
            }

            if (TargetOffset == 2.5)
            {
                if (dMultiSiteVout0A[idut] < 2.25 || dMultiSiteVout0A[idut] > 2.8)
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_OFFSET_ABN;
                    TrimFinish();
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    this.txt_Status_AutoTab.ForeColor = Color.Red;
                    this.txt_Status_AutoTab.Text = "FAIL!";
                    return;
                }
            }
            else if (TargetOffset == 1.65)
            {
                if (dMultiSiteVout0A[idut] < 1.4 || dMultiSiteVout0A[idut] > 2.0)
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_OFFSET_ABN;
                    TrimFinish();
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    this.txt_Status_AutoTab.ForeColor = Color.Red;
                    this.txt_Status_AutoTab.Text = "FAIL!";
                    return;
                }
            }
            else
            {
                if (dMultiSiteVout0A[idut] < TargetOffset * 0.97 || dMultiSiteVout0A[idut] > TargetOffset * 1.03)
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_OFFSET_ABN;
                    TrimFinish();
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    this.txt_Status_AutoTab.ForeColor = Color.Red;
                    this.txt_Status_AutoTab.Text = "FAIL!";
                    return;
                }
            }

            #endregion  Get Vout@0A

            #region No need Trim case
            if ((TargetOffset - 0.004) <= dMultiSiteVout0A[idut] && dMultiSiteVout0A[idut] <= (TargetOffset + 0.004)
                && (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= (TargetVoltage_customer + 0.004)
                && (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= (TargetVoltage_customer - 0.004))
            {
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_EXT);
                Delay(Delay_Sync);
                RePower();
                //Delay(Delay_Sync);
                EnterTestMode();
                RegisterWrite(5, new uint[10] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 
                    0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut], 0x84, 0x07 });
                BurstRead(0x80, 5, tempReadback);
                /* fuse */
                FuseClockOn(DeviceAddress, (double)num_UD_pulsewidth_ow_EngT.Value, (double)numUD_pulsedurationtime_ow_EngT.Value);
                DisplayOperateMes("Processing...");
                //Delay(Delay_Fuse);
                ReloadPreset();
                Delay(Delay_Sync);
                BurstRead(0x80, 5, tempReadback);
                Delay(Delay_Sync);
                /* Margianl read, compare with writed code; 
                    * if ( = ), go on
                    * else bMarginal = true; */
                MarginalReadPreset();
                Delay(Delay_Sync);
                BurstRead(0x80, 5, tempReadback);
                bMarginal = false;

                if (bMASK)
                {
                    if (((tempReadback[0] & 0xE0) != (MultiSiteReg0[idut] & 0xE0)) | (tempReadback[1] & 0x81) != (MultiSiteReg1[idut] & 0x81) |
                        (tempReadback[2] & 0x99) != (MultiSiteReg2[idut] & 0x99) |
                        (tempReadback[3] & 0x83) != (MultiSiteReg3[idut] & 0x83) | (tempReadback[4] < 1))
                        bMarginal = true;
                }
                else
                {
                    if (((tempReadback[0] & 0xFF) != (MultiSiteReg0[idut] & 0xFF)) | (tempReadback[1] & 0xFF) != (MultiSiteReg1[idut] & 0xFF) |
                        (tempReadback[2] & 0xFF) != (MultiSiteReg2[idut] & 0xFF) |
                        (tempReadback[3] & 0xFF) != (MultiSiteReg3[idut] & 0xFF) | (tempReadback[4] < 1))
                        bMarginal = true;
                }

                if (bSAFEREAD)
                {
                    //Delay(Delay_Sync);
                    SafetyReadPreset();
                    Delay(Delay_Sync);
                    BurstRead(0x80, 5, tempReadback);
                    bSafety = false;
                    if (((tempReadback[0] & 0xFF) != (MultiSiteReg0[idut] & 0xFF)) | (tempReadback[1] & 0xFF) != (MultiSiteReg1[idut] & 0xFF) |
                            (tempReadback[2] & 0xFF) != (MultiSiteReg2[idut] & 0xFF) |
                            (tempReadback[3] & 0xFF) != (MultiSiteReg3[idut] & 0xFF) | (tempReadback[4] < 1))
                        bSafety = true;
                }

                //capture Vout
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_5V);
                RePower();
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);
                Delay(Delay_Sync);
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);

                Delay(Delay_Sync);
                dMultiSiteVout0A[idut] = GetVout();
                sDUT.dVout0ATrimmed = dMultiSiteVout0A[idut];
                DisplayOperateMes("Vout" + " @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

                /* Change Current to IP  */
                if (ProgramMode == 0)
                {
                    if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                        DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", IP));
                    else
                    {
                        DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        TrimFinish();
                        return;
                    }
                }
                else if(ProgramMode == 1)
                {
                    dr = MessageBox.Show(String.Format("请将电流升至{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                    {
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        PowerOff();
                        RestoreRegValue();
                        return;
                    }
                }
                else if (ProgramMode == 2)
                {
                    //IP ON
                    MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                    Delay(Delay_Sync);
                    MultiSiteSocketSelect(0);       //set epio1 = high; epio3 = low
                }

                Delay(Delay_Fuse);
                dMultiSiteVoutIP[idut] = GetVout();
                sDUT.dVoutIPTrimmed = dMultiSiteVoutIP[idut];
                DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));

                sDUT.bReadMarginal = bMarginal;
                sDUT.bReadSafety = bSafety;

                if (!(bMarginal | bSafety))
                {
                    DisplayOperateMes("DUT" + idut.ToString() + "Pass! Bin Normal");
                    //uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_NORMAL;
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_1;
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    this.txt_Status_AutoTab.ForeColor = Color.Green;
                    this.txt_Status_AutoTab.Text = "PASS!";
                    DisplayOperateMes("Bin" + " = " + uDutTrimResult[idut].ToString());
                    DisplayOperateMes("Marginal Read ->" + bMarginal.ToString());
                    DisplayOperateMes("Safety REad ->" + bSafety.ToString());
                    MultiSiteDisplayResult(uDutTrimResult);
                    TrimFinish();
                    PrintDutAttribute(sDUT);
                    return;
                }
                else
                {
                    DisplayOperateMes("DUT" + idut.ToString() + "Pass! Bin Mriginal");
                    //uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_MARGINAL;
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_4;
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    this.txt_Status_AutoTab.ForeColor = Color.Green;
                    if (bMRE)
                        this.txt_Status_AutoTab.Text = "FAIL!";
                    else
                        this.txt_Status_AutoTab.Text = "PASS!";

                    DisplayOperateMes("Bin" + " = " + uDutTrimResult[idut].ToString());
                    DisplayOperateMes("Marginal Read ->" + bMarginal.ToString());
                    DisplayOperateMes("Safety Read ->" + bSafety.ToString());
                    MultiSiteDisplayResult(uDutTrimResult);
                    TrimFinish();
                    PrintDutAttribute(sDUT);
                    return;
                }
            }


            #endregion No need Trim case

            #region For low sensitivity case, with IP

            dGainTest = 1000d * (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) / IP;
            if (dGainTest < (TargetGain_customer * ThresholdOfGain))
            {
                dGainTestMinusTarget = dGainTest / TargetGain_customer;
                dGainPreset = RoughTable_Customer[0][MultiSiteRoughGainCodeIndex[idut]] / 100d;

                if (this.cmb_IPRange_PreT.SelectedItem.ToString() == "1.5x610")
                {
                    if (dGainTestMinusTarget >= dGainPreset)
                    {
                        MultiSiteRoughGainCodeIndex[idut] = (uint)LookupRoughGain_Customer
                            (TargetGain_customer * 100d / dGainTest * dGainPreset, RoughTable_Customer);
                        /* Rough Gain Code*/
                        bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
                        MultiSiteReg0[idut] &= ~bit_op_mask;
                        MultiSiteReg0[idut] |= Convert.ToUInt32(RoughTable_Customer[1][MultiSiteRoughGainCodeIndex[idut]]);

                        bit_op_mask = bit0_Mask;
                        MultiSiteReg1[idut] &= ~bit_op_mask;
                        MultiSiteReg1[idut] |= Convert.ToUInt32(RoughTable_Customer[2][MultiSiteRoughGainCodeIndex[idut]]);
                    }
                    else
                    {
                        DisplayOperateMes("DUT" + idut.ToString() + " Sensitivity is NOT enough!", Color.Red);
                        bDutValid[idut] = false;
                        uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_LOW_SENSITIVITY;
                        TrimFinish();
                        sDUT.iErrorCode = uDutTrimResult[idut];
                        //this.txt_Status_AutoTab.ForeColor = Color.Red;
                        //this.txt_Status_AutoTab.Text = "MOA!";
                        PrintDutAttribute(sDUT);
                        //dr = MessageBox.Show(String.Format("灵敏度过低，交研发部重新编程！"), "Warning", MessageBoxButtons.OK);
                        this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                        this.txt_Status_AutoTab.Text = "低敏!";
                        return;
                    }

                }
                else
                {
                    if (dGainTestMinusTarget >= dGainPreset)
                    {
                        MultiSiteRoughGainCodeIndex[idut] = (uint)LookupRoughGain_Customer
                            (TargetGain_customer * 100d / dGainTest * dGainPreset, RoughTable_Customer);
                        /* Rough Gain Code*/
                        bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
                        MultiSiteReg0[idut] &= ~bit_op_mask;
                        MultiSiteReg0[idut] |= Convert.ToUInt32(RoughTable_Customer[1][MultiSiteRoughGainCodeIndex[idut]]);

                        bit_op_mask = bit0_Mask;
                        MultiSiteReg1[idut] &= ~bit_op_mask;
                        MultiSiteReg1[idut] |= Convert.ToUInt32(RoughTable_Customer[2][MultiSiteRoughGainCodeIndex[idut]]);
                    }
                    else
                    {
                        if (dGainTest * 1.5 / dGainPreset >= (TargetGain_customer * ThresholdOfGain))
                        {
                            MultiSiteRoughGainCodeIndex[idut] = (uint)LookupRoughGain_Customer((TargetGain_customer
                                * 100d / (dGainTest * 1.5d) * dGainPreset), RoughTable_Customer);
                            MultiSiteRoughGainCodeIndex[idut] -= 1;
                            MultiSiteReg3[idut] |= 0xC0;
                            /* Rough Gain Code*/
                            bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
                            MultiSiteReg0[idut] &= ~bit_op_mask;
                            MultiSiteReg0[idut] |= Convert.ToUInt32(RoughTable_Customer[1][MultiSiteRoughGainCodeIndex[idut]]);

                            bit_op_mask = bit0_Mask;
                            MultiSiteReg1[idut] &= ~bit_op_mask;
                            MultiSiteReg1[idut] |= Convert.ToUInt32(RoughTable_Customer[2][MultiSiteRoughGainCodeIndex[idut]]);
                        }
                        else
                        {
                            DisplayOperateMes("DUT" + idut.ToString() + " Sensitivity is NOT enough!", Color.Red);
                            bDutValid[idut] = false;
                            uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_LOW_SENSITIVITY;
                            TrimFinish();
                            sDUT.iErrorCode = uDutTrimResult[idut];
                            //this.txt_Status_AutoTab.ForeColor = Color.Red;
                            //this.txt_Status_AutoTab.Text = "MOA!";
                            PrintDutAttribute(sDUT);
                            //dr = MessageBox.Show(String.Format("灵敏度过低，交研发部重新编程！"), "Warning", MessageBoxButtons.OK);
                            this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                            this.txt_Status_AutoTab.Text = "低敏!";
                            return;
                        }
                    }
                }


                DisplayOperateMes("RoughGainCodeIndex of DUT" + " = " + MultiSiteRoughGainCodeIndex[idut].ToString("F0"));
                DisplayOperateMes("SelectedRoughGain = " + RoughTable_Customer[0][MultiSiteRoughGainCodeIndex[idut]].ToString());
                DisplayOperateMes("CalcCode:");
                DisplayOperateMes("0x80 = 0x" + MultiSiteReg0[idut].ToString("X2"));
                DisplayOperateMes("0x81 = 0x" + MultiSiteReg1[idut].ToString("X2"));
                DisplayOperateMes("0x82 = 0x" + MultiSiteReg2[idut].ToString("X2"));
                DisplayOperateMes("0x83 = 0x" + MultiSiteReg3[idut].ToString("X2"));

                /*  power on */
                RePower();
                EnterTestMode();
                RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
                BurstRead(0x80, 5, tempReadback);
                /* Get vout @ IP */
                EnterNomalMode();

                /* Change Current to IP  */
                if (ProgramMode == 0)
                {
                    if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                        DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", IP));
                    else
                    {
                        DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        TrimFinish();
                        return;
                    }
                }
                else if(ProgramMode == 1)
                {
                    dr = MessageBox.Show(String.Format("请将电流升至{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                    {
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        PowerOff();
                        RestoreRegValue();
                        return;
                    }
                }
                else if (ProgramMode == 2)
                {
                    //IP ON
                    MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                    Delay(Delay_Sync);
                    MultiSiteSocketSelect(0);       //set epio1 = high; epio3 = low
                }

                Delay(Delay_Fuse);
                dMultiSiteVoutIP[idut] = GetVout();
                sDUT.dVoutIPMiddle = dMultiSiteVoutIP[idut];
                DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));


                /*Judge PreSet gain; delta Vout target >= delta Vout test * 86.07% */
                if (dMultiSiteVoutIP[idut] > saturationVout)
                {
                    //decrease gain preset
                    MultiSiteRoughGainCodeIndex[idut] -= 1;
                    /* Rough Gain Code*/
                    bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
                    MultiSiteReg0[idut] &= ~bit_op_mask;
                    MultiSiteReg0[idut] |= Convert.ToUInt32(RoughTable_Customer[1][MultiSiteRoughGainCodeIndex[idut]]);

                    bit_op_mask = bit0_Mask;
                    MultiSiteReg1[idut] &= ~bit_op_mask;
                    MultiSiteReg1[idut] |= Convert.ToUInt32(RoughTable_Customer[2][MultiSiteRoughGainCodeIndex[idut]]);

                    /*  power on */
                    RePower();
                    EnterTestMode();
                    RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
                    BurstRead(0x80, 5, tempReadback);
                    /* Get vout @ IP */
                    EnterNomalMode();
                    Delay(Delay_Fuse);
                    dMultiSiteVoutIP[idut] = GetVout();
                    sDUT.dVoutIPMiddle = dMultiSiteVoutIP[idut];
                    DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));

                    /*Judge PreSet gain; delta Vout target >= delta Vout test * 86.07% */
                    if (dMultiSiteVoutIP[idut] > saturationVout)
                    {
                        DisplayOperateMes("Module" + " Vout is SATURATION!", Color.Red);
                        uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_VOUT_SATURATION;
                        TrimFinish();
                        sDUT.iErrorCode = uDutTrimResult[idut];
                        //this.txt_Status_AutoTab.ForeColor = Color.Red;
                        //this.txt_Status_AutoTab.Text = "MOA!";
                        PrintDutAttribute(sDUT);
                        //dr = MessageBox.Show(String.Format("输出饱和，交研发部重新编程！"), "Warning", MessageBoxButtons.OK);
                        this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                        this.txt_Status_AutoTab.Text = "饱和!";
                        return;
                    }
                }

                /* Change Current to 0A */
                if (ProgramMode == 0)
                {
                    if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0u))
                        DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", 0u));
                    else
                    {
                        DisplayOperateMes(string.Format("Set Current to {0}A failed!", 0u));
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        TrimFinish();
                        return;
                    }
                }
                else if (ProgramMode == 1)
                {
                    dr = MessageBox.Show(String.Format("请将电流降至0A"), "Change Current", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                    {
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        PowerOff();
                        RestoreRegValue();
                        return;
                    }
                }
                else if (ProgramMode == 2)
                {
                    //IP OFF
                    MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                    Delay(Delay_Sync);
                    MultiSiteSocketSelect(9);       //set epio1 = low; epio3 = high
                }

                /*  power on */
                Delay(Delay_Fuse);
                this.txt_Status_AutoTab.Text = "Processing!";
                dMultiSiteVout0A[idut] = GetVout();
                sDUT.dVout0AMiddle = dMultiSiteVout0A[idut];
                DisplayOperateMes("Vout" + " @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

                //V0A is abnormal
                //if( Math.Abs(sDUT.dVout0AMiddle - sDUT.dVout0ANative) > 0.005 )
                //{
                //    dr = MessageBox.Show(String.Format("Vout @ 0A is abnormal"), "Warning!", MessageBoxButtons.OK);
                //    if (dr == DialogResult.OK)
                //    {
                //        DisplayOperateMes("V0A abnormal, Rebuild rough gain code for low gain case!");
                //        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                //        PowerOff();
                //        RestoreReg80ToReg83Value();
                //        return;
                //    }
                //}
            }

            #endregion For low sensitivity case, with IP

            #region Adapting algorithm

            tempG1 = RoughTable_Customer[0][MultiSiteRoughGainCodeIndex[idut]] / 100d;
            tempG2 = (TargetGain_customer / ((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) / IP)) / 1000d;

            autoAdaptingGoughGain = tempG1 * tempG2 * 100d;
            DisplayOperateMes("IdealGoughGain = " + autoAdaptingGoughGain.ToString("F4"));

            Ix_forAutoAdaptingRoughGain = LookupRoughGain_Customer(autoAdaptingGoughGain, RoughTable_Customer);
            autoAdaptingPresionGain = 100d * autoAdaptingGoughGain / RoughTable_Customer[0][Ix_forAutoAdaptingRoughGain];
            Ix_forAutoAdaptingPresionGain = LookupPreciseGain_Customer(autoAdaptingPresionGain, PreciseTable_Customer);
            if (bAutoTrimTest)
            {
                DisplayOperateMes("IP = " + IP.ToString("F0"));
                DisplayOperateMes("TargetGain_customer" + idut.ToString() + " = " + TargetGain_customer.ToString("F4"));
                DisplayOperateMes("(dMultiSiteVoutIP - dMultiSiteVout0A)/IP = " + ((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) / IP).ToString("F4"));
                DisplayOperateMes("tempG1" + idut.ToString() + " = " + tempG1.ToString("F4"));
                DisplayOperateMes("tempG2" + idut.ToString() + " = " + tempG2.ToString("F4"));
                DisplayOperateMes("Ix_forAutoAdaptingRoughGain" + idut.ToString() + " = " + Ix_forAutoAdaptingRoughGain.ToString("F0"));
                DisplayOperateMes("Ix_forAutoAdaptingPresionGain" + idut.ToString() + " = " + Ix_forAutoAdaptingPresionGain.ToString("F0"));
                DisplayOperateMes("autoAdaptingGoughGain" + idut.ToString() + " = " + RoughTable_Customer[0][Ix_forAutoAdaptingRoughGain].ToString("F4"));
                DisplayOperateMes("autoAdaptingPresionGain" + idut.ToString() + " = " + PreciseTable_Customer[0][Ix_forAutoAdaptingPresionGain].ToString("F4"));
            }

            /* Rough Gain Code*/
            bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
            MultiSiteReg0[idut] &= ~bit_op_mask;
            MultiSiteReg0[idut] |= Convert.ToUInt32(RoughTable_Customer[1][Ix_forAutoAdaptingRoughGain]);

            bit_op_mask = bit0_Mask;
            MultiSiteReg1[idut] &= ~bit_op_mask;
            MultiSiteReg1[idut] |= Convert.ToUInt32(RoughTable_Customer[2][Ix_forAutoAdaptingRoughGain]);

            if (bAutoTrimTest)
            {
                DisplayOperateMes("Rough Gain RegValue80 = 0x" + MultiSiteReg0[idut].ToString("X2"));
                DisplayOperateMes("Rough Gain RegValue81 = 0x" + MultiSiteReg1[idut].ToString("X2"));
            }

            /* Presion Gain Code*/
            bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask | bit3_Mask | bit4_Mask;
            MultiSiteReg0[idut] &= ~bit_op_mask;
            MultiSiteReg0[idut] |= Convert.ToUInt32(PreciseTable_Customer[1][Ix_forAutoAdaptingPresionGain]);

            if (bAutoTrimTest)
            {
                DisplayOperateMes("Precesion Gain RegValue80 = 0x" + MultiSiteReg0[idut].ToString("X2"));
                DisplayOperateMes("***new add approach***");
            }

            RePower();
            EnterTestMode();
            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            EnterNomalMode();

            /* Change Current to IP  */
            if (ProgramMode == 0)
            {
                if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                    DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", IP));
                else
                {
                    DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    TrimFinish();
                    return;
                }
            }
            else if(ProgramMode == 1)
            {
                dr = MessageBox.Show(String.Format("请将电流升至{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    PowerOff();
                    RestoreRegValue();
                    return;
                }
            }
            else if (ProgramMode == 2)
            {
                //IP ON
                MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                Delay(Delay_Sync);
                MultiSiteSocketSelect(0);       //set epio1 = high; epio3 = low
            }

            Delay(Delay_Fuse);
            dMultiSiteVoutIP[idut] = GetVout();
            DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));

            /* Change Current to 0A */
            if (ProgramMode == 0)
            {
                if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0u))
                    DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", 0u));
                else
                {
                    DisplayOperateMes(string.Format("Set Current to {0}A failed!", 0u));
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    TrimFinish();
                    return;
                }
            }
            else if(ProgramMode == 1)
            {
                dr = MessageBox.Show(String.Format("请将电流降至0A"), "Change Current", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    PowerOff();
                    RestoreRegValue();
                    return;
                }
            }
            else if (ProgramMode == 2)
            {
                //IP OFF
                MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                Delay(Delay_Sync);
                MultiSiteSocketSelect(9);       //set epio1 = low; epio3 = high
            }

            /*  power on */
            Delay(Delay_Fuse);
            //this.txt_Status_AutoTab.Text = "Processing!";
            dMultiSiteVout0A[idut] = GetVout();
            DisplayOperateMes("DUT" + " Vout @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

            if (((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) * 1000 - TargetGain_customer * IP) > 4)
            {
                /* Presion Gain Code*/
                bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask | bit3_Mask | bit4_Mask;
                MultiSiteReg0[idut] &= ~bit_op_mask;
                MultiSiteReg0[idut] |= Convert.ToUInt32(PreciseTable_Customer[1][Ix_forAutoAdaptingPresionGain + 1]);
            }
            else if (((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) * 1000 - TargetGain_customer * IP) < -4)
            {
                /* Presion Gain Code*/
                bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask | bit3_Mask | bit4_Mask;
                MultiSiteReg0[idut] &= ~bit_op_mask;
                MultiSiteReg0[idut] |= Convert.ToUInt32(PreciseTable_Customer[1][Ix_forAutoAdaptingPresionGain - 1]);
            }



            if (bAutoTrimTest)
                DisplayOperateMes("***new approach end***");

            RePower();
            EnterTestMode();
            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            EnterNomalMode();
            Delay(Delay_Fuse);
            dMultiSiteVout0A[idut] = GetVout();
            TargetOffset = GetVref() - 0.004;
            dVout_0A_Temp = dMultiSiteVout0A[idut];
            if (bAutoTrimTest)
                DisplayOperateMes("DUT" + " Vout @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));


            /* Offset trim code calculate */
            Vout_0A = dMultiSiteVout0A[idut];

            //btn_offset_Click(null, null);
            uint[] regTMultiSite = new uint[3];

            DiffModeOffsetAlg(regTMultiSite);
            MultiSiteReg1[idut] |= regTMultiSite[0];
            MultiSiteReg2[idut] |= regTMultiSite[1];
            MultiSiteReg3[idut] |= regTMultiSite[2];

            bit_op_mask = bit2_Mask | bit3_Mask | bit4_Mask | bit5_Mask;
            ix_forOffsetIndex_Rough = 0;
            ix_forOffsetIndex_Rough = LookupOffsetIndex(MultiSiteReg3[idut] & bit_op_mask, OffsetTableB_Customer);
            ix_forOffsetIndex_Rough_Complementary = ix_forOffsetIndex_Rough;
            DisplayOperateMes("\r\nProcessing...");

            /* Repower on 5V */
            RePower();
            EnterTestMode();
            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            EnterNomalMode();
            Delay(Delay_Fuse);
            dMultiSiteVout0A[idut] = GetVout();
            sDUT.dVout0AMiddle = dMultiSiteVout0A[idut];
            DisplayOperateMes("MultiSiteReg3 = 0x" + MultiSiteReg3[idut].ToString("X2"));
            DisplayOperateMes("ix_forOffsetIndex_Rough = " + ix_forOffsetIndex_Rough.ToString());
            DisplayOperateMes("dMultiSiteVout0A = " + dMultiSiteVout0A[idut].ToString("F3"));

            if (dMultiSiteVout0A[idut] > TargetOffset)
            {
                if (ix_forOffsetIndex_Rough == 7)
                    ix_forOffsetIndex_Rough = 7;
                else if (ix_forOffsetIndex_Rough == 15)
                    ix_forOffsetIndex_Rough = 0;
                else
                    ix_forOffsetIndex_Rough += 1;
            }
            else if (dMultiSiteVout0A[idut] < TargetOffset)
            {
                if (ix_forOffsetIndex_Rough == 8)
                    ix_forOffsetIndex_Rough = 8;
                else if (ix_forOffsetIndex_Rough == 0)
                    ix_forOffsetIndex_Rough = 15;
                else
                    ix_forOffsetIndex_Rough -= 1;
            }
            bit_op_mask = bit2_Mask | bit3_Mask | bit4_Mask | bit5_Mask;
            MultiSiteReg3[idut] &= ~bit_op_mask;
            MultiSiteReg3[idut] |= Convert.ToUInt32(OffsetTableB_Customer[1][ix_forOffsetIndex_Rough]);

            RePower();
            EnterTestMode();
            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            EnterNomalMode();
            Delay(Delay_Fuse);
            dMultiSiteVout_0A_Complementary = GetVout();
            DisplayOperateMes("\r\nMultiSiteReg3 = 0x" + MultiSiteReg3[idut].ToString("X2"));
            DisplayOperateMes("ix_forOffsetIndex_Rough = " + ix_forOffsetIndex_Rough.ToString());
            DisplayOperateMes("dMultiSiteVout_0A_Complementary = " + dMultiSiteVout_0A_Complementary.ToString("F3"));

            //V0A is abnormal
            //if (Math.Abs(sDUT.dVout0AMiddle - dMultiSiteVout_0A_Complementary) > 0.005)
            //{
            //    dr = MessageBox.Show(String.Format("Vout @ 0A is abnormal"), "Warning!", MessageBoxButtons.OKCancel);
            //    if (dr == DialogResult.Cancel)
            //    {
            //        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
            //        PowerOff();
            //        RestoreReg80ToReg83Value();
            //        return;
            //    }
            //}

            if (Math.Abs(dMultiSiteVout0A[idut] - TargetOffset) < Math.Abs(dMultiSiteVout_0A_Complementary - TargetOffset))
            {
                bit_op_mask = bit2_Mask | bit3_Mask | bit4_Mask | bit5_Mask;
                MultiSiteReg3[idut] &= ~bit_op_mask;
                MultiSiteReg3[idut] |= Convert.ToUInt32(OffsetTableB_Customer[1][ix_forOffsetIndex_Rough_Complementary]);
                DisplayOperateMes("Last MultiSiteReg3 = 0x" + MultiSiteReg3[idut].ToString("X2"));
            }
            else
            {
                bit_op_mask = bit2_Mask | bit3_Mask | bit4_Mask | bit5_Mask;
                MultiSiteReg3[idut] &= ~bit_op_mask;
                MultiSiteReg3[idut] |= Convert.ToUInt32(OffsetTableB_Customer[1][ix_forOffsetIndex_Rough]);
                DisplayOperateMes("Last MultiSiteReg3 = 0x" + MultiSiteReg3[idut].ToString("X2"));
            }

            DisplayOperateMes("Processing...");

            #endregion Adapting algorithm

            #region Fuse
            //Fuse
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_EXT);
            RePower();
            EnterTestMode();
            RegisterWrite(5, new uint[10] { 0x80, MultiSiteReg0[idut], 
                0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 
                0x83, MultiSiteReg3[idut], 0x84, 0x07 });
            BurstRead(0x80, 5, tempReadback);
            FuseClockOn(DeviceAddress, (double)num_UD_pulsewidth_ow_EngT.Value, (double)numUD_pulsedurationtime_ow_EngT.Value);
            DisplayOperateMes("Trimming...");
            //Delay(Delay_Fuse);

            ReloadPreset();
            Delay(Delay_Sync);
            BurstRead(0x80, 5, tempReadback);
            if (tempReadback[4] == 0)
            {
                RePower();
                EnterTestMode();
                RegisterWrite(5, new uint[10] { 0x80, MultiSiteReg0[idut], 
                    0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 
                    0x83, MultiSiteReg3[idut], 0x84, 0x07 });
                BurstRead(0x80, 5, tempReadback);
                FuseClockOn(DeviceAddress, (double)num_UD_pulsewidth_ow_EngT.Value, (double)numUD_pulsedurationtime_ow_EngT.Value);
                DisplayOperateMes("Trimming...");
                //Delay(Delay_Fuse);
            }
            Delay(Delay_Sync);
            /* Margianl read, compare with writed code; 
                * if ( = ), go on
                * else bMarginal = true; */
            MarginalReadPreset();
            Delay(Delay_Sync);
            BurstRead(0x80, 5, tempReadback);
            bMarginal = false;
            if (bMASK)
            {
                if (((tempReadback[0] & 0xE0) != (MultiSiteReg0[idut] & 0xE0)) | (tempReadback[1] & 0x81) != (MultiSiteReg1[idut] & 0x81) |
                    (tempReadback[2] & 0x99) != (MultiSiteReg2[idut] & 0x99) | (tempReadback[3] & 0x83) != (MultiSiteReg3[idut] & 0x83) |
                    (tempReadback[4] < 1))
                    bMarginal = true;
            }
            else
            {
                if (((tempReadback[0] & 0xFF) != (MultiSiteReg0[idut] & 0xFF)) | (tempReadback[1] & 0xFF) != (MultiSiteReg1[idut] & 0xFF) |
                        (tempReadback[2] & 0xFF) != (MultiSiteReg2[idut] & 0xFF) | (tempReadback[3] & 0xFF) != (MultiSiteReg3[idut] & 0xFF) |
                        (tempReadback[4] < 1))
                    bMarginal = true;
            }

            if (bSAFEREAD)
            {
                //Delay(Delay_Sync);
                SafetyReadPreset();
                Delay(Delay_Sync);
                BurstRead(0x80, 5, tempReadback);
                bSafety = false;
                if (((tempReadback[0] & 0xFF) != (MultiSiteReg0[idut] & 0xFF)) | (tempReadback[1] & 0xFF) != (MultiSiteReg1[idut] & 0xFF) |
                        (tempReadback[2] & 0xFF) != (MultiSiteReg2[idut] & 0xFF) | (tempReadback[3] & 0xFF) != (MultiSiteReg3[idut] & 0xFF) |
                        (tempReadback[4] < 1))
                    bSafety = true;
            }

            sDUT.bReadMarginal = bMarginal;
            sDUT.bReadSafety = bSafety;

            if (!(bMarginal | bSafety))
            {
                DisplayOperateMes("DUT" + "Pass! Bin Normal");
                uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_NORMAL;
            }
            else
            {
                DisplayOperateMes("DUT" + "Pass! Bin Mriginal");
                uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_MARGINAL;
            }
            //sDUT.iErrorCode = uDutTrimResult[idut];

            #endregion

            #region Bin
            /* Repower on 5V */
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_5V);
            RePower();
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);
            Delay(Delay_Sync);
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);

            Delay(Delay_Sync);
            dMultiSiteVout0A[idut] = GetVout();
            TargetOffset = GetVref();
            sDUT.dVout0ATrimmed = dMultiSiteVout0A[idut];
            DisplayOperateMes("Vout" + " @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

            /* Change Current to IP  */
            if (ProgramMode == 0)
            {
                if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                    DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", IP));
                else
                {
                    DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    TrimFinish();
                    return;
                }
            }
            else if (ProgramMode == 1)
            {
                dr = MessageBox.Show(String.Format("请将电流升至{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    PowerOff();
                    RestoreRegValue();
                    return;
                }
            }
            else if (ProgramMode == 2)
            {
                //IP ON
                MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                Delay(Delay_Sync);
                MultiSiteSocketSelect(0);       //set epio1 = high; epio3 = low
            }

            Delay(Delay_Fuse);
            dMultiSiteVoutIP[idut] = GetVout();
            sDUT.dVoutIPTrimmed = dMultiSiteVoutIP[idut];
            DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));

            if (uDutTrimResult[idut] == (uint)PRGMRSULT.DUT_BIN_MARGINAL)
            {
                if (TargetOffset * (1 - 0.001) <= dMultiSiteVout0A[idut] && dMultiSiteVout0A[idut] <= TargetOffset * (1 + 0.001) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= TargetVoltage_customer * (1 + 0.001) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= TargetVoltage_customer * (1 - 0.001))
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_4;
                    //this.txt_Status_AutoTab.ForeColor = Color.Green;
                    if (bMRE)
                    {
                        this.txt_Status_AutoTab.ForeColor = Color.Red;
                        this.txt_Status_AutoTab.Text = "FAIL!";
                    }
                    else
                    {
                        this.txt_Status_AutoTab.ForeColor = Color.Green;
                        this.txt_Status_AutoTab.Text = "PASS!";
                    }
                }
                else if (TargetOffset * (1 - bin2accuracy / 100d) <= dMultiSiteVout0A[idut] &&
                    dMultiSiteVout0A[idut] <= TargetOffset * (1 + bin2accuracy / 100d) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= TargetVoltage_customer * (1 + bin3accuracy / 100d) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= TargetVoltage_customer * (1 - bin3accuracy / 100d))
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_5;
                    //this.txt_Status_AutoTab.ForeColor = Color.Green;
                    if (bMRE)
                    {
                        this.txt_Status_AutoTab.ForeColor = Color.Red;
                        this.txt_Status_AutoTab.Text = "FAIL!";
                    }
                    else
                    {
                        this.txt_Status_AutoTab.ForeColor = Color.Green;
                        this.txt_Status_AutoTab.Text = "PASS!";
                    }
                }
                else if (TargetOffset * (1 - bin2accuracy / 100d) <= dMultiSiteVout0A[idut] &&
                    dMultiSiteVout0A[idut] <= TargetOffset * (1 + bin2accuracy / 100d) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= TargetVoltage_customer * (1 + bin3accuracy / 100d) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= TargetVoltage_customer * (1 - bin3accuracy / 100d))
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_6;
                    //this.txt_Status_AutoTab.ForeColor = Color.Green;
                    if (bMRE)
                    {
                        this.txt_Status_AutoTab.ForeColor = Color.Red;
                        this.txt_Status_AutoTab.Text = "FAIL!";
                    }
                    else
                    {
                        this.txt_Status_AutoTab.ForeColor = Color.Green;
                        this.txt_Status_AutoTab.Text = "PASS!";
                    }
                }
                else
                {
                    this.txt_Status_AutoTab.ForeColor = Color.Red;
                    this.txt_Status_AutoTab.Text = "FAIL!";
                }

            }

            /* bin1,2,3 */
            //if ((!bMarginal) && (!bSafety))
            else if (uDutTrimResult[idut] == (uint)PRGMRSULT.DUT_BIN_NORMAL)
            {
                if (TargetOffset * (1 - 0.001) <= dMultiSiteVout0A[idut] && dMultiSiteVout0A[idut] <= TargetOffset * (1 + 0.001) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= TargetVoltage_customer * (1 + 0.001) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= TargetVoltage_customer * (1 - 0.001))
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_1;
                    this.txt_Status_AutoTab.ForeColor = Color.Green;
                    this.txt_Status_AutoTab.Text = "PASS!";
                }
                else if (TargetOffset * (1 - bin2accuracy / 100d) <= dMultiSiteVout0A[idut] &&
                    dMultiSiteVout0A[idut] <= TargetOffset * (1 + bin2accuracy / 100d) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= TargetVoltage_customer * (1 + bin3accuracy / 100d) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= TargetVoltage_customer * (1 - bin3accuracy / 100d))
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_2;
                    this.txt_Status_AutoTab.ForeColor = Color.Green;
                    this.txt_Status_AutoTab.Text = "PASS!";
                }
                else if (TargetOffset * (1 - bin2accuracy / 100d) <= dMultiSiteVout0A[idut] &&
                    dMultiSiteVout0A[idut] <= TargetOffset * (1 + bin2accuracy / 100d) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= TargetVoltage_customer * (1 + bin3accuracy / 100d) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= TargetVoltage_customer * (1 - bin3accuracy / 100d))
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_3;
                    this.txt_Status_AutoTab.ForeColor = Color.Green;
                    this.txt_Status_AutoTab.Text = "PASS!";
                }
                else
                {
                    this.txt_Status_AutoTab.ForeColor = Color.Red;
                    this.txt_Status_AutoTab.Text = "FAIL!";
                }
            }

            #endregion Bin

            #region Display Result and Reset parameters
            DisplayOperateMes("Bin" + " = " + uDutTrimResult[idut].ToString());
            MultiSiteDisplayResult(uDutTrimResult);
            TrimFinish();
            sDUT.iErrorCode = uDutTrimResult[idut];
            PrintDutAttribute(sDUT);
            DisplayOperateMes("Next...");
            #endregion Display Result and Reset parameters
        }

        private void AutoTrim_SL620A_SingleEnd()
        {
            #region Define Parameters
            DialogResult dr;
            bool bMarginal = false;
            bool bSafety = false;
            //uint[] tempReadback = new uint[5];
            double dVout_0A_Temp = 0;
            //TargetOffset = 2.5;
            double dVip_Target = TargetOffset + TargetVoltage_customer;
            //double dGainTestMinusTarget = 1;
            //double dGainTest = 0;
            ModuleAttribute sDUT;
            sDUT.dIQ = 0;
            sDUT.dVoutIPNative = 0;
            sDUT.dVout0ANative = 0;
            sDUT.dVoutIPMiddle = 0;
            sDUT.dVout0AMiddle = 0;
            sDUT.dVoutIPTrimmed = 0;
            sDUT.dVout0ATrimmed = 0;
            sDUT.iErrorCode = 00;
            sDUT.bDigitalCommFail = false;
            sDUT.bNormalModeFail = false;
            sDUT.bReadMarginal = false;
            sDUT.bReadSafety = false;
            sDUT.bTrimmed = false;

            // PARAMETERS DEFINE FOR MULTISITE
            uint idut = 0;
            uint uDutCount = 16;
            //bool bValidRound = false;
            //bool bSecondCurrentOn = false;
            double dModuleCurrent = 0;
            bool[] bGainBoost = new bool[16];
            bool[] bDutValid = new bool[16];
            bool[] bDutNoNeedTrim = new bool[16];
            uint[] uDutTrimResult = new uint[16];
            double[] dMultiSiteVoutIP = new double[16];
            double[] dMultiSiteVout0A = new double[16];

            /* autoAdaptingGoughGain algorithm*/
            double autoAdaptingGoughGain = 0;
            double autoAdaptingPresionGain = 0;
            double tempG1 = 0;
            double tempG2 = 0;
            //double dGainPreset = 0;
            int Ix_forAutoAdaptingRoughGain = 0;
            int Ix_forAutoAdaptingPresionGain = 0;

            int ix_forOffsetIndex_Rough = 0;
            int ix_forOffsetIndex_Rough_Complementary = 0;
            double dMultiSiteVout_0A_Complementary = 0;

            DisplayOperateMes("\r\n************" + DateTime.Now.ToString() + "************");
            DisplayOperateMes("Start...");
            this.txt_Status_AutoTab.ForeColor = Color.Black;
            this.txt_Status_AutoTab.Text = "START!";

            for (uint i = 0; i < uDutCount; i++)
            {
                dMultiSiteVoutIP[i] = 0d;
                dMultiSiteVout0A[i] = 0d;

                MultiSiteReg0[i] = Reg80Value;
                MultiSiteReg1[i] = Reg81Value;
                MultiSiteReg2[i] = Reg82Value;
                MultiSiteReg3[i] = Reg83Value;
                MultiSiteReg4[i] = Reg84Value;
                MultiSiteReg5[i] = Reg85Value;
                MultiSiteReg6[i] = Reg86Value;
                MultiSiteReg7[i] = Reg87Value;

                MultiSiteRoughGainCodeIndex[i] = preSetCoareseGainCode;

                uDutTrimResult[i] = 0u;
                bDutNoNeedTrim[i] = false;
                bDutValid[i] = false;
                bGainBoost[i] = false;
            }
            #endregion Define Parameters

            #region Get module current
            /*  power on */
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_5V);
            RePower();
            Delay(Delay_Sync);

            if (!this.cb_MeasureiQ_AutoTab.Checked)
            {
                /* Get module current */
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VCS);
                Delay(Delay_Sync);
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_SET_CURRENT_SENCE);

                this.txt_ModuleCurrent_EngT.Text = GetModuleCurrent().ToString("F1");
                this.txt_ModuleCurrent_PreT.Text = this.txt_ModuleCurrent_EngT.Text;


                dModuleCurrent = GetModuleCurrent();
                sDUT.dIQ = dModuleCurrent;
                if (dCurrentDownLimit > dModuleCurrent)
                {
                    DisplayOperateMes("Module " + " current is " + dModuleCurrent.ToString("F3"), Color.Red);
                    //uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_CURRENT_ABNORMAL;
                    PowerOff();
                    //PrintDutAttribute(sDUT);
                    MessageBox.Show(String.Format("电流偏低，检查模组是否连接！"), "Warning", MessageBoxButtons.OK);
                    return;
                }
                else if (dModuleCurrent > dCurrentUpLimit)
                {
                    DisplayOperateMes("Module " + " current is " + dModuleCurrent.ToString("F3"), Color.Red);
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_CURRENT_HIGH;
                    PowerOff();
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    //MessageBox.Show(String.Format("电流异常，模块短路或损坏！"), "Error", MessageBoxButtons.OK);
                    this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                    this.txt_Status_AutoTab.Text = "短路!";
                    return;
                }
                else
                    DisplayOperateMes("Module " + " current is " + dModuleCurrent.ToString("F3"));
            }
            #endregion Get module current 

            #region Saturation judgement


            //Redundency delay in case of power off failure.
            Delay(Delay_Sync);
            EnterTestMode();
            //Delay(Delay_Sync);
            BurstRead(0x80, 5, tempReadback);
            if (tempReadback[0] + tempReadback[1] + tempReadback[2] + tempReadback[3] + tempReadback[4] != 0)
            {
                DisplayOperateMes("DUT" + " has some bits Blown!", Color.Red);
                uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_TRIMMED_SOMEBITS;
                TrimFinish();
                sDUT.bTrimmed = false;
                sDUT.iErrorCode = uDutTrimResult[idut];
                PrintDutAttribute(sDUT);
                this.txt_Status_AutoTab.ForeColor = Color.Red;
                this.txt_Status_AutoTab.Text = "FAIL!";
                return;
            }



            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            DisplayOperateMes(string.Format("0x80 = 0x{0};\r\n0x81 = 0x{1};\r\n0x82 = 0x{2};\r\n0x83 = 0x{3}", 
                MultiSiteReg0[idut].ToString("X2"), MultiSiteReg1[idut].ToString("X2"), MultiSiteReg2[idut].ToString("X2"), MultiSiteReg3[idut].ToString("X2")));
            Delay(Delay_Sync);
            RegisterWrite(4, new uint[8] { 0x84, MultiSiteReg4[idut], 0x85, MultiSiteReg5[idut], 0x86, MultiSiteReg6[idut], 0x87, MultiSiteReg7[idut] });
            Delay(Delay_Sync);
            BurstRead(0x80, 5, tempReadback);
            if (tempReadback[0] != MultiSiteReg0[idut] || tempReadback[1] != MultiSiteReg1[idut]
                || tempReadback[2] != MultiSiteReg2[idut] || tempReadback[3] != MultiSiteReg3[idut])
            {
                if (tempReadback[0] + tempReadback[1] + tempReadback[2] + tempReadback[3] + tempReadback[4] == 0)
                {
                    RePower();
                    Delay(Delay_Sync);
                    oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);
                    Delay(Delay_Sync);
                    oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);
                    Delay(Delay_Fuse);
                    dMultiSiteVout0A[idut] = AverageVout();
                    if (dMultiSiteVout0A[idut] < 4.5 && dMultiSiteVout0A[idut] > 1.5)
                    {
                        DisplayOperateMes("DUT Trimmed!", Color.Red);
                        uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_TRIMMRD_ALREADY;
                        TrimFinish();
                        sDUT.bDigitalCommFail = true;
                        sDUT.bTrimmed = true;
                        sDUT.iErrorCode = uDutTrimResult[idut];
                        PrintDutAttribute(sDUT);
                        //MessageBox.Show(String.Format("模组已编程，交至研发部！"), "Error", MessageBoxButtons.OK);
                        this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                        this.txt_Status_AutoTab.Text = "混料!";
                        return;
                    }
                    else
                    {
                        DisplayOperateMes("VOUT Short!", Color.Red);
                        uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_VOUT_SHORT;
                        TrimFinish();
                        sDUT.bDigitalCommFail = true;
                        sDUT.iErrorCode = uDutTrimResult[idut];
                        PrintDutAttribute(sDUT);
                        //MessageBox.Show(String.Format("输出管脚短路！", Color.YellowGreen), "Warning", MessageBoxButtons.OK);
                        this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                        this.txt_Status_AutoTab.Text = "短路!";
                        return;
                    }
                }
                else
                {
                    DisplayOperateMes("DUT digital communication fail!", Color.Red);
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_COMM_FAIL;
                    TrimFinish();
                    sDUT.bDigitalCommFail = true;
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    //MessageBox.Show(String.Format("输出管脚短路！", Color.YellowGreen), "Warning", MessageBoxButtons.OK);
                    this.txt_Status_AutoTab.ForeColor = Color.Red;
                    this.txt_Status_AutoTab.Text = "FAIL!";
                    return;
                }
            }
            /* Get vout @ IP */
            EnterNomalMode();

            /* Change Current to IP  */
            if (ProgramMode == 0)
            {
                if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                    DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", IP));
                else
                {
                    DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                    TrimFinish();
                    return;
                }
            }
            else if (ProgramMode == 1)
            {
                dr = MessageBox.Show(String.Format("请将电流升至{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    PowerOff();
                    RestoreRegValue();
                    return;
                }
            }
            else if (ProgramMode == 2)
            {
                //set epio1 and epio3 to low
                MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                Delay(Delay_Sync);
                MultiSiteSocketSelect(0);       //set epio1 = high; epio3 = low
            }


            //oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);
            Delay(Delay_Fuse);
            dMultiSiteVoutIP[idut] = AverageVout();
            sDUT.dVoutIPNative = dMultiSiteVoutIP[idut];
            DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));

            /*Judge PreSet gain; delta Vout target >= delta Vout test * 86.07% */
            if (dMultiSiteVoutIP[idut] > saturationVout)
            {
                if (ProgramMode == 0)
                {
                    oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0);
                    Delay(Delay_Sync);
                    oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, 5);
                    Delay(Delay_Sync);
                    if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                        DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", 5));
                    else
                    {
                        DisplayOperateMes(string.Format("Set Current to {0}A failed!", 5));
                        TrimFinish();
                        return;
                    }
                }
                else if (ProgramMode == 1)
                {
                    dr = MessageBox.Show(String.Format("请将电流升至{0}A", 5), "Change Current", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                    {
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        PowerOff();
                        RestoreRegValue();
                        return;
                    }
                }
                else if (ProgramMode == 2)
                {
                    MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                    Delay(Delay_Sync);
                    MultiSiteSocketSelect(0);       //set epio1 = high; epio3 = low
                }


                Delay(Delay_Fuse);
                dMultiSiteVoutIP[idut] = AverageVout();
                sDUT.dVoutIPNative = dMultiSiteVoutIP[idut];
                DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));

                //set current back to IP
                if (ProgramMode == 0)
                {
                    oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0);
                    Delay(Delay_Sync);
                    oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, Convert.ToUInt32(IP));
                }


                if (dMultiSiteVoutIP[idut] > saturationVout)
                {
                    DisplayOperateMes("Module" + " Vout is VDD!", Color.Red);
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_VOUT_VDD;
                    TrimFinish();
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                    this.txt_Status_AutoTab.Text = "短路!";
                    return;
                }
                else
                {
                    //dr = MessageBox.Show(String.Format("输出饱和，交研发部重新编程！"), "Warning", MessageBoxButtons.OK);
                    TrimFinish();
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_VOUT_SATURATION;
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                    this.txt_Status_AutoTab.Text = "饱和!";
                    return;
                }
            }
            else if (dMultiSiteVoutIP[idut] < minimumVoutIP)
            {
                DisplayOperateMes("Module" + " Vout is too Low!", Color.Red);
                uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_VOUT_LOW;
                TrimFinish();
                sDUT.iErrorCode = uDutTrimResult[idut];
                PrintDutAttribute(sDUT);
                this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                this.txt_Status_AutoTab.Text = "短路!";
                return;
            }

            #endregion Saturation judgement

            #region Get Vout@0A
            /* Change Current to 0A */
            if (ProgramMode == 0)
            {
                if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0u))
                    DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", 0u));
                else
                {
                    DisplayOperateMes(string.Format("Set Current to {0}A failed!", 0u));
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    TrimFinish();
                    return;
                }
            }
            else if(ProgramMode == 1)
            {
                dr = MessageBox.Show(String.Format("请将IP降至0A!"), "Try Again", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    PowerOff();
                    RestoreRegValue();
                    return;
                }
            }
            else if (ProgramMode == 2)
            {
                MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                Delay(Delay_Sync);
                MultiSiteSocketSelect(9);       //set epio1 = low; epio3 = high
            }

            Delay(Delay_Fuse);
            dMultiSiteVout0A[idut] = AverageVout();
            sDUT.dVout0ANative = dMultiSiteVout0A[idut];
            DisplayOperateMes("Vout" + " @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

            if (dMultiSiteVoutIP[idut] < dMultiSiteVout0A[idut])
            {
                TrimFinish();
                //PrintDutAttribute(sDUT);
                MessageBox.Show(String.Format("请确认IP方向!"), "Try Again", MessageBoxButtons.OK);
                return;
            }
            else if (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut] < VoutIPThreshold)
            {
                TrimFinish();
                //PrintDutAttribute(sDUT);
                MessageBox.Show(String.Format("请确认电流为{0}A!!!", IP), "Try Again", MessageBoxButtons.OK);
                return;
            }

            if (TargetOffset == 2.5)
            {
                if (dMultiSiteVout0A[idut] < 2.25 || dMultiSiteVout0A[idut] > 2.8)
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_OFFSET_ABN;
                    TrimFinish();
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    this.txt_Status_AutoTab.ForeColor = Color.Red;
                    this.txt_Status_AutoTab.Text = "FAIL!";
                    return;
                }
            }
            else if (TargetOffset == 1.65)
            {
                if (dMultiSiteVout0A[idut] < 1.0 || dMultiSiteVout0A[idut] > 2.5)
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_OFFSET_ABN;
                    TrimFinish();
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    this.txt_Status_AutoTab.ForeColor = Color.Red;
                    this.txt_Status_AutoTab.Text = "FAIL!";
                    return;
                }
            }

            #endregion  Get Vout@0A

            #region No need Trim case
            if ((TargetOffset - 0.001) <= dMultiSiteVout0A[idut] && dMultiSiteVout0A[idut] <= (TargetOffset + 0.001)
                && (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= (TargetVoltage_customer + 0.001)
                && (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= (TargetVoltage_customer - 0.001))
            {
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_EXT);
                Delay(Delay_Sync);
                RePower();
                //Delay(Delay_Sync);
                EnterTestMode();

                RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
                Delay(Delay_Sync);
                RegisterWrite(4, new uint[8] { 0x84, MultiSiteReg4[idut], 0x85, MultiSiteReg5[idut], 0x86, MultiSiteReg6[idut], 0x87, MultiSiteReg7[idut] });
                Delay(Delay_Sync);


                BurstRead(0x80, 5, tempReadback);
                /* fuse */
                FuseClockOn(DeviceAddress, (double)num_UD_pulsewidth_ow_EngT.Value, (double)numUD_pulsedurationtime_ow_EngT.Value);
                DisplayOperateMes("Processing...");
                //Delay(Delay_Fuse);             

                //capture Vout
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_5V);
                RePower();
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);
                Delay(Delay_Sync);
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);

                Delay(Delay_Sync);
                dMultiSiteVout0A[idut] = AverageVout();
                sDUT.dVout0ATrimmed = dMultiSiteVout0A[idut];
                DisplayOperateMes("Vout" + " @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

                /* Change Current to IP  */
                if (ProgramMode == 0)
                {
                    if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                        DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", IP));
                    else
                    {
                        DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        TrimFinish();
                        return;
                    }
                }
                else if(ProgramMode == 1)
                {
                    dr = MessageBox.Show(String.Format("请将电流升至{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                    {
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        PowerOff();
                        RestoreRegValue();
                        return;
                    }
                }
                else if (ProgramMode == 2)
                {
                    MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                    Delay(Delay_Sync);
                    MultiSiteSocketSelect(0);       //set epio1 = high; epio3 = low
                }

                Delay(Delay_Fuse);
                dMultiSiteVoutIP[idut] = AverageVout();
                sDUT.dVoutIPTrimmed = dMultiSiteVoutIP[idut];
                DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));
                //oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_5V);

                DisplayOperateMes("DUT" + idut.ToString() + "Pass! Bin Normal");
                //uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_NORMAL;
                uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_1;
                sDUT.iErrorCode = uDutTrimResult[idut];
                this.txt_Status_AutoTab.ForeColor = Color.Green;
                this.txt_Status_AutoTab.Text = "PASS!";
                DisplayOperateMes("Bin" + " = " + uDutTrimResult[idut].ToString());
                DisplayOperateMes("Marginal Read ->" + bMarginal.ToString());
                DisplayOperateMes("Safety REad ->" + bSafety.ToString());
                MultiSiteDisplayResult(uDutTrimResult);
                TrimFinish();
                PrintDutAttribute(sDUT);
                return;
                
            }


            #endregion No need Trim case

            #region Adapting algorithm

            tempG1 = sl620CoarseGainTable[0][MultiSiteRoughGainCodeIndex[idut]] / 100d;
            tempG2 = (TargetGain_customer / ((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) / IP)) / 1000d;

            autoAdaptingGoughGain = tempG1 * tempG2 * 100d;
            DisplayOperateMes("IdealGoughGain = " + autoAdaptingGoughGain.ToString("F4"));

            Ix_forAutoAdaptingRoughGain = LookupCoarseGain_SL620(autoAdaptingGoughGain, sl620CoarseGainTable);
            autoAdaptingPresionGain = 100d * autoAdaptingGoughGain / sl620CoarseGainTable[0][Ix_forAutoAdaptingRoughGain];
            //Ix_forAutoAdaptingPresionGain = LookupFineGain_SL620(autoAdaptingPresionGain, sl620FineGainTable);
            if (bAutoTrimTest)
            {
                DisplayOperateMes("IP = " + IP.ToString("F0"));
                DisplayOperateMes("TargetGain_customer" + idut.ToString() + " = " + TargetGain_customer.ToString("F4"));
                DisplayOperateMes("(dMultiSiteVoutIP - dMultiSiteVout0A)/IP = " + ((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) / IP).ToString("F4"));
                DisplayOperateMes("Pre-set gain" + idut.ToString() + " = " + tempG1.ToString("F4"));
                DisplayOperateMes("Target gain / Test gain" + idut.ToString() + " = " + tempG2.ToString("F4"));
                DisplayOperateMes("Ix_forAutoAdaptingRoughGain" + idut.ToString() + " = " + Ix_forAutoAdaptingRoughGain.ToString("F0"));
                DisplayOperateMes("Ix_forAutoAdaptingPresionGain" + idut.ToString() + " = " + Ix_forAutoAdaptingPresionGain.ToString("F0"));
                DisplayOperateMes("autoAdaptingGoughGain" + idut.ToString() + " = " + sl620CoarseGainTable[0][Ix_forAutoAdaptingRoughGain].ToString("F4"));
                DisplayOperateMes("autoAdaptingPresionGain" + idut.ToString() + " = " + sl620FineGainTable[0][Ix_forAutoAdaptingPresionGain].ToString("F4"));
            }

            /* Rough Gain Code*/
            bit_op_mask = bit4_Mask | bit5_Mask | bit6_Mask | bit7_Mask;
            MultiSiteReg1[idut] &= ~bit_op_mask;
            MultiSiteReg1[idut] |= Convert.ToUInt32(sl620CoarseGainTable[1][Ix_forAutoAdaptingRoughGain]);


            if (bAutoTrimTest)
            {
                //DisplayOperateMes("Rough Gain RegValue80 = 0x" + MultiSiteReg0[idut].ToString("X2"));
                DisplayOperateMes("Rough Gain RegValue81 = 0x" + MultiSiteReg1[idut].ToString("X2"));
            }

            /******************************************************************************
            *
            *   Coarse Offset Alg
            *
            ******************************************************************************/
            RePower();
            EnterTestMode();

            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            Delay(Delay_Sync);
            RegisterWrite(4, new uint[8] { 0x84, MultiSiteReg4[idut], 0x85, MultiSiteReg5[idut], 0x86, MultiSiteReg6[idut], 0x87, MultiSiteReg7[idut] });
            Delay(Delay_Sync);

            //RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            EnterNomalMode();
            Delay(Delay_Fuse);
            /* Offset trim code calculate */
            dMultiSiteVout0A[idut] = AverageVout();
            Vout_0A = dMultiSiteVout0A[idut];


            //bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask | bit3_Mask | bit4_Mask;
            uint ix_CoarseOffsetCode = 0;
            if (Vout_0A > TargetOffset)
            {
                ix_CoarseOffsetCode = Convert.ToUInt32(Math.Round(1000d * (1.0d - TargetOffset / Vout_0A) / 5));
                if (ix_CoarseOffsetCode > 15)
                    ix_CoarseOffsetCode = 15;

                //autoAdaptingGoughGain = sl620CoarseGainTable[0][Ix_forAutoAdaptingRoughGain] / (1.0 - ix_CoarseOffsetCode * 0.005);
                autoAdaptingGoughGain = tempG2 * tempG1 * 100d / (1.0 - ix_CoarseOffsetCode * 0.005);

                if (TargetOffset == 2.5)
                    Ix_forAutoAdaptingRoughGain = LookupCoarseGain_SL620(autoAdaptingGoughGain, sl620CoarseGainTable);
                //1.65V case
                else
                    Ix_forAutoAdaptingRoughGain = LookupCoarseGain_SL620(autoAdaptingGoughGain, sl620CoarseGainTable); 
                /* Rough Gain Code*/
                bit_op_mask = bit4_Mask | bit5_Mask | bit6_Mask | bit7_Mask;
                MultiSiteReg1[idut] &= ~bit_op_mask;
                MultiSiteReg1[idut] |= Convert.ToUInt32(sl620CoarseGainTable[1][Ix_forAutoAdaptingRoughGain]);
                

            }
            else if (Vout_0A < TargetOffset)
            {
                ix_CoarseOffsetCode = 31 - Convert.ToUInt32(Math.Round(1000d * (TargetOffset / Vout_0A - 1.0d) / 5));
                //if (ix_CoarseOffsetCode == 32)
                //    ix_CoarseOffsetCode = 0;
                if (ix_CoarseOffsetCode < 16)
                    ix_CoarseOffsetCode = 16;

                //autoAdaptingGoughGain = sl620CoarseGainTable[0][Ix_forAutoAdaptingRoughGain] / (1.0 + ix_CoarseOffsetCode * 0.005);
                autoAdaptingGoughGain = tempG2 * tempG1 * 100d / (1.0 + (31 - ix_CoarseOffsetCode) * 0.005);

                if (TargetOffset == 2.5)
                    Ix_forAutoAdaptingRoughGain = LookupCoarseGain_SL620(autoAdaptingGoughGain, sl620CoarseGainTable);
                //1.65V case
                else
                    Ix_forAutoAdaptingRoughGain = LookupCoarseGain_SL620(autoAdaptingGoughGain, sl620CoarseGainTable); 

                /* Rough Gain Code*/
                bit_op_mask = bit4_Mask | bit5_Mask | bit6_Mask | bit7_Mask;
                MultiSiteReg1[idut] &= ~bit_op_mask;
                MultiSiteReg1[idut] |= Convert.ToUInt32(sl620CoarseGainTable[1][Ix_forAutoAdaptingRoughGain]);
            }

            bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask | bit3_Mask | bit4_Mask;

            MultiSiteReg6[idut] &= ~bit_op_mask;
            MultiSiteReg6[idut] |= ix_CoarseOffsetCode;

            DisplayOperateMes("Vout_0A = " + Vout_0A.ToString("F3"));
            DisplayOperateMes("ix_CoarseOffsetCode = " + ix_CoarseOffsetCode.ToString());
            /////////////////////////////////////////////////////////////////////////////////////////////////

            


            RePower();
            EnterTestMode();

            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            Delay(Delay_Sync);
            RegisterWrite(4, new uint[8] { 0x84, MultiSiteReg4[idut], 0x85, MultiSiteReg5[idut], 0x86, MultiSiteReg6[idut], 0x87, MultiSiteReg7[idut] });
            Delay(Delay_Sync);

            EnterNomalMode();

            #region /* Change Current to IP  */
            if (ProgramMode == 0)
            {
                if ( !oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                {
                    DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    TrimFinish();
                    return;
                }
            }
            else if(ProgramMode == 1)
            {
                dr = MessageBox.Show(String.Format("请将电流升至{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    PowerOff();
                    RestoreRegValue();
                    return;
                }
            }
            else if (ProgramMode == 2)
            {
                MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                Delay(Delay_Sync);
                MultiSiteSocketSelect(0);       //set epio1 = high; epio3 = low
            }
            #endregion

            Delay(Delay_Fuse);
            dMultiSiteVoutIP[idut] = AverageVout();
            DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));

            #region Change Current to 0A */
            if (ProgramMode == 0)
            {
                if ( !oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0u))
                {
                    DisplayOperateMes(string.Format("Set Current to {0}A failed!", 0u));
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    TrimFinish();
                    return;
                }
            }
            else if(ProgramMode == 1)
            {
                dr = MessageBox.Show(String.Format("请将电流降至0A"), "Change Current", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    PowerOff();
                    RestoreRegValue();
                    return;
                }
            }
            else if (ProgramMode == 2)
            {
                MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                Delay(Delay_Sync);
                MultiSiteSocketSelect(9);       //set epio1 = low; epio3 = high
            }
            #endregion

            /*  power on */
            Delay(Delay_Fuse);
            dMultiSiteVout0A[idut] = AverageVout();
            DisplayOperateMes("DUT" + " Vout @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

            double gainTest = 0;

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            gainTest = (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) * 1000d / IP;

            if (gainTest < TargetGain_customer * 0.995)
            {
                DisplayOperateMes("Tuning coarse gain", Color.DarkRed);

                /* Rough Gain Code*/
                if (Ix_forAutoAdaptingRoughGain > 0)
                {
                    bit_op_mask = bit4_Mask | bit5_Mask | bit6_Mask | bit7_Mask;
                    MultiSiteReg1[idut] &= ~bit_op_mask;
                    MultiSiteReg1[idut] |= Convert.ToUInt32(sl620CoarseGainTable[1][Ix_forAutoAdaptingRoughGain - 1]);

                    RePower();
                    EnterTestMode();

                    RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
                    Delay(Delay_Sync);
                    RegisterWrite(4, new uint[8] { 0x84, MultiSiteReg4[idut], 0x85, MultiSiteReg5[idut], 0x86, MultiSiteReg6[idut], 0x87, MultiSiteReg7[idut] });
                    Delay(Delay_Sync);

                    EnterNomalMode();

                    #region /* Change Current to IP  */
                    if (ProgramMode == 0)
                    {
                        if (!oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                        {
                            DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                            DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                            TrimFinish();
                            return;
                        }
                    }
                    else if (ProgramMode == 1)
                    {
                        dr = MessageBox.Show(String.Format("请将电流升至{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                        if (dr == DialogResult.Cancel)
                        {
                            DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                            PowerOff();
                            RestoreRegValue();
                            return;
                        }
                    }
                    else if (ProgramMode == 2)
                    {
                        MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                        Delay(Delay_Sync);
                        MultiSiteSocketSelect(0);       //set epio1 = high; epio3 = low
                    }
                    #endregion

                    Delay(Delay_Fuse);
                    dMultiSiteVoutIP[idut] = AverageVout();
                    DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));

                    #region Change Current to 0A */
                    if (ProgramMode == 0)
                    {
                        if (!oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0u))
                        {
                            DisplayOperateMes(string.Format("Set Current to {0}A failed!", 0u));
                            DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                            TrimFinish();
                            return;
                        }
                    }
                    else if (ProgramMode == 1)
                    {
                        dr = MessageBox.Show(String.Format("请将电流降至0A"), "Change Current", MessageBoxButtons.OKCancel);
                        if (dr == DialogResult.Cancel)
                        {
                            DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                            PowerOff();
                            RestoreRegValue();
                            return;
                        }
                    }
                    else if (ProgramMode == 2)
                    {
                        MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                        Delay(Delay_Sync);
                        MultiSiteSocketSelect(9);       //set epio1 = low; epio3 = high
                    }
                    #endregion

                    /*  power on */
                    Delay(Delay_Fuse);
                    dMultiSiteVout0A[idut] = AverageVout();
                    DisplayOperateMes("DUT" + " Vout @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

                    gainTest = 0;

                    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    gainTest = (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) * 1000d / IP;
                }
                else
                {
                    DisplayOperateMes( "coarsegaincode = " + Ix_forAutoAdaptingRoughGain.ToString());
                    DisplayOperateMes("不适合做此产品，此芯片可用灵敏度更低的产品！", Color.DarkRed);
                    return;
                }
                
            }

            autoAdaptingPresionGain = 100d * (TargetGain_customer / gainTest);
            Ix_forAutoAdaptingPresionGain = LookupFineGain_SL620(autoAdaptingPresionGain, sl620FineGainTable);

            /* Fine Gain Code*/
            bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
            MultiSiteReg6[idut] &= ~bit_op_mask;
            MultiSiteReg6[idut] |= Convert.ToUInt32(sl620FineGainTable[1][Ix_forAutoAdaptingPresionGain]);

            bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
            MultiSiteReg7[idut] &= ~bit_op_mask;
            MultiSiteReg7[idut] |= Convert.ToUInt32(sl620FineGainTable[2][Ix_forAutoAdaptingPresionGain]);

            if (bAutoTrimTest)
            {
                DisplayOperateMes("Precesion Gain RegValue86 = 0x" + MultiSiteReg6[idut].ToString("X2"));
                DisplayOperateMes("Precesion Gain RegValue87 = 0x" + MultiSiteReg7[idut].ToString("X2"));
                DisplayOperateMes("***new add approach***");
            }
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////         

            RePower();
            EnterTestMode();

            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            Delay(Delay_Sync*2);
            RegisterWrite(4, new uint[8] { 0x84, MultiSiteReg4[idut], 0x85, MultiSiteReg5[idut], 0x86, MultiSiteReg6[idut], 0x87, MultiSiteReg7[idut] });
            Delay(Delay_Sync);

            BurstRead(0x85, 4, tempReadback);

            //RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            EnterNomalMode();
            Delay(Delay_Fuse);
            dMultiSiteVout0A[idut] = AverageVout();
            Vout_0A = dMultiSiteVout0A[idut];

            ///////////////////////////////////////////////////////////////////////////////////////////////////
            bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask | bit3_Mask | bit4_Mask;
            uint ix_FineOffsetCode = 0;
            if (Vout_0A > TargetOffset)
                ix_FineOffsetCode = Convert.ToUInt32(Math.Round(1000d * (1.0d - TargetOffset / Vout_0A) / 1.5));
            else if (Vout_0A < TargetOffset)
                ix_FineOffsetCode = 31 - Convert.ToUInt32(Math.Round(1000d * (TargetOffset / Vout_0A - 1.0d) / 1.5));

            MultiSiteReg7[idut] &= ~bit_op_mask;
            MultiSiteReg7[idut] |= ix_FineOffsetCode;

            //ix_forOffsetIndex_Rough = LookupOffsetIndex(MultiSiteReg3[idut] & bit_op_mask, OffsetTableB_Customer);
            //ix_FineOffsetCode = ix_forOffsetIndex_Rough;
            DisplayOperateMes("Vout_0A = " + Vout_0A.ToString("F3"));
            DisplayOperateMes("ix_FineOffsetCode = " + ix_FineOffsetCode.ToString());
            DisplayOperateMes("\r\nProcessing...");

            //*******************Additinal test case ***********************
            RePower();
            EnterTestMode();

            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            Delay(Delay_Sync);
            RegisterWrite(4, new uint[8] { 0x84, MultiSiteReg4[idut], 0x85, MultiSiteReg5[idut], 0x86, MultiSiteReg6[idut], 0x87, MultiSiteReg7[idut] });
            Delay(Delay_Sync);

            EnterNomalMode();

            Delay(Delay_Power);
            dMultiSiteVout0A[idut] = AverageVout();
            DisplayOperateMes("DUT" + " Vout @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

            //gainTest = (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) * 1000d / IP;
            if (dMultiSiteVout0A[idut] - TargetOffset >= 0.004)
            {
                //ix_FineOffsetCode += Convert.ToUInt32(Math.Round((dMultiSiteVout0A[idut] - TargetOffset) / 0.004));
                //DisplayOperateMes("ix_FineOffsetCode = " + ix_FineOffsetCode.ToString());

                if (ix_FineOffsetCode == 15)
                    ix_FineOffsetCode = 15;
                else if (ix_FineOffsetCode == 31)
                    ix_FineOffsetCode = 0;
                else
                    ix_FineOffsetCode++;

                DisplayOperateMes("ix_FineOffsetCode = " + ix_FineOffsetCode.ToString());

                /* Fine offset Code*/
                bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask | bit3_Mask | bit4_Mask;
                MultiSiteReg7[idut] &= ~bit_op_mask;
                MultiSiteReg7[idut] |= ix_FineOffsetCode;
            }
            else if (TargetOffset - dMultiSiteVout0A[idut] >= 0.004)
            {
                if (ix_FineOffsetCode == 0)
                    ix_FineOffsetCode = 31;
                else if (ix_FineOffsetCode == 16)
                    ix_FineOffsetCode = 16;
                else
                    ix_FineOffsetCode--;

                DisplayOperateMes("ix_FineOffsetCode = " + ix_FineOffsetCode.ToString());

                /* Fine offset Code*/
                bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask | bit3_Mask | bit4_Mask;
                MultiSiteReg7[idut] &= ~bit_op_mask;
                MultiSiteReg7[idut] |= ix_FineOffsetCode;
            }
            //********************************************************

            DisplayOperateMes("Processing...");

            #endregion Adapting algorithm

            #region Fuse
            //Fuse
            //oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_EXT);
            RePower();
            EnterTestMode();

            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            Delay(Delay_Sync);
            RegisterWrite(4, new uint[8] { 0x84, MultiSiteReg4[idut], 0x85, MultiSiteReg5[idut], 0x86, MultiSiteReg6[idut], 0x87, MultiSiteReg7[idut] });
            Delay(Delay_Sync);
            RegisterWrite(1, new uint[2] {0x88, 0x02});

            if (this.cb_BypFuse_AutoTab.Checked)
            {
                printRegValue();
                TrimFinish();
                return;
            }

            FuseClockOn(DeviceAddress, (double)num_UD_pulsewidth_ow_EngT.Value, (double)numUD_pulsedurationtime_ow_EngT.Value);
            DisplayOperateMes("Trimming...");

            #endregion

            #region Bin

            if (this.cb_AutoTab_Retest.Text == "Yes")
            {
                /* Repower on 5V */
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);
                Delay(Delay_Sync);
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_5V);
                RePower();
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);
                Delay(Delay_Fuse);

                dMultiSiteVout0A[idut] = AverageVout();
                sDUT.dVout0ATrimmed = dMultiSiteVout0A[idut];
                DisplayOperateMes("Vout" + " @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

                /* Change Current to IP  */
                if (ProgramMode == 0)
                {
                    //if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, Convert.ToUInt32(IP)))
                    if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                        DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", IP));
                    else
                    {
                        DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        TrimFinish();
                        return;
                    }
                }
                else if(ProgramMode == 1)
                {
                    dr = MessageBox.Show(String.Format("请将电流升至{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                    {
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        PowerOff();
                        RestoreRegValue();
                        return;
                    }
                }
                else if (ProgramMode == 2)
                {
                    MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                    Delay(Delay_Sync);
                    MultiSiteSocketSelect(0);       //set epio1 = high; epio3 = low
                }

                Delay(Delay_Fuse);
                dMultiSiteVoutIP[idut] = AverageVout();
                sDUT.dVoutIPTrimmed = dMultiSiteVoutIP[idut];
                DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));

                /* bin1,2,3 */
                if (TargetOffset * (1 - 0.002) <= dMultiSiteVout0A[idut] && dMultiSiteVout0A[idut] <= TargetOffset * (1 + 0.002) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= TargetVoltage_customer * (1 + 0.002) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= TargetVoltage_customer * (1 - 0.002))
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_1;
                    this.txt_Status_AutoTab.ForeColor = Color.Green;
                    this.txt_Status_AutoTab.Text = "PASS!";
                }
                else if (TargetOffset * (1 - bin2accuracy / 100d) <= dMultiSiteVout0A[idut] &&
                    dMultiSiteVout0A[idut] <= TargetOffset * (1 + bin2accuracy / 100d) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= TargetVoltage_customer * (1 + bin3accuracy / 100d) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= TargetVoltage_customer * (1 - bin3accuracy / 100d))
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_2;
                    this.txt_Status_AutoTab.ForeColor = Color.Green;
                    this.txt_Status_AutoTab.Text = "PASS!";
                }
                else if (TargetOffset * (1 - bin2accuracy / 100d) <= dMultiSiteVout0A[idut] &&
                    dMultiSiteVout0A[idut] <= TargetOffset * (1 + bin2accuracy / 100d) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= TargetVoltage_customer * (1 + bin3accuracy / 100d) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= TargetVoltage_customer * (1 - bin3accuracy / 100d))
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_3;
                    this.txt_Status_AutoTab.ForeColor = Color.Green;
                    this.txt_Status_AutoTab.Text = "PASS!";
                }
                else
                {
                    this.txt_Status_AutoTab.ForeColor = Color.Red;
                    this.txt_Status_AutoTab.Text = "FAIL!";
                }
            }
            else if (this.cb_AutoTab_Retest.Text == "No")
            {
                /* Repower on 5V */
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);
                Delay(Delay_Sync);
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_5V);
                RePower();
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);
                Delay(Delay_Fuse);

                dMultiSiteVout0A[idut] = AverageVout();
                sDUT.dVout0ATrimmed = dMultiSiteVout0A[idut];
                DisplayOperateMes("Vout" + " @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

                /* bin1,2,3 */
                if (TargetOffset * (1 - 0.002) <= dMultiSiteVout0A[idut] && dMultiSiteVout0A[idut] <= TargetOffset * (1 + 0.002))
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_1;
                    this.txt_Status_AutoTab.ForeColor = Color.Green;
                    this.txt_Status_AutoTab.Text = "PASS!";
                }
                else if (TargetOffset * (1 - bin2accuracy / 100d) <= dMultiSiteVout0A[idut] &&
                    dMultiSiteVout0A[idut] <= TargetOffset * (1 + bin2accuracy / 100d))
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_2;
                    this.txt_Status_AutoTab.ForeColor = Color.Green;
                    this.txt_Status_AutoTab.Text = "PASS!";
                }
                else
                {
                    this.txt_Status_AutoTab.ForeColor = Color.Red;
                    this.txt_Status_AutoTab.Text = "FAIL!";
                }
            }

            #endregion Bin

            #region Display Result and Reset parameters
            DisplayOperateMes("Bin" + " = " + uDutTrimResult[idut].ToString());
            MultiSiteDisplayResult(uDutTrimResult);
            TrimFinish();
            sDUT.iErrorCode = uDutTrimResult[idut];
            PrintDutAttribute(sDUT);
            DisplayOperateMes("Next...");
            #endregion Display Result and Reset parameters
        }

        private void AutoTrim_SL620_SingleEnd()
        {
            #region Define Parameters
            DialogResult dr;
            bool bMarginal = false;
            bool bSafety = false;
            //uint[] tempReadback = new uint[5];
            double dVout_0A_Temp = 0;
            //TargetOffset = 2.5;
            double dVip_Target = TargetOffset + TargetVoltage_customer;
            //double dGainTestMinusTarget = 1;
            //double dGainTest = 0;
            ModuleAttribute sDUT;
            sDUT.dIQ = 0;
            sDUT.dVoutIPNative = 0;
            sDUT.dVout0ANative = 0;
            sDUT.dVoutIPMiddle = 0;
            sDUT.dVout0AMiddle = 0;
            sDUT.dVoutIPTrimmed = 0;
            sDUT.dVout0ATrimmed = 0;
            sDUT.iErrorCode = 00;
            sDUT.bDigitalCommFail = false;
            sDUT.bNormalModeFail = false;
            sDUT.bReadMarginal = false;
            sDUT.bReadSafety = false;
            sDUT.bTrimmed = false;

            // PARAMETERS DEFINE FOR MULTISITE
            uint idut = 0;
            uint uDutCount = 16;
            //bool bValidRound = false;
            //bool bSecondCurrentOn = false;
            double dModuleCurrent = 0;
            bool[] bGainBoost = new bool[16];
            bool[] bDutValid = new bool[16];
            bool[] bDutNoNeedTrim = new bool[16];
            uint[] uDutTrimResult = new uint[16];
            double[] dMultiSiteVoutIP = new double[16];
            double[] dMultiSiteVout0A = new double[16];

            /* autoAdaptingGoughGain algorithm*/
            double autoAdaptingGoughGain = 0;
            double autoAdaptingPresionGain = 0;
            double tempG1 = 0;
            double tempG2 = 0;
            //double dGainPreset = 0;
            int Ix_forAutoAdaptingRoughGain = 0;
            int Ix_forAutoAdaptingPresionGain = 0;

            int ix_forOffsetIndex_Rough = 0;
            int ix_forOffsetIndex_Rough_Complementary = 0;
            double dMultiSiteVout_0A_Complementary = 0;

            DisplayOperateMes("\r\n************" + DateTime.Now.ToString() + "************");
            DisplayOperateMes("Start...");
            this.txt_Status_AutoTab.ForeColor = Color.Black;
            this.txt_Status_AutoTab.Text = "START!";

            for (uint i = 0; i < uDutCount; i++)
            {
                dMultiSiteVoutIP[i] = 0d;
                dMultiSiteVout0A[i] = 0d;

                MultiSiteReg0[i] = Reg80Value;
                MultiSiteReg1[i] = Reg81Value;
                MultiSiteReg2[i] = Reg82Value;
                MultiSiteReg3[i] = Reg83Value;
                MultiSiteReg4[i] = Reg84Value;
                MultiSiteReg5[i] = Reg85Value;
                MultiSiteReg6[i] = Reg86Value;
                MultiSiteReg7[i] = Reg87Value;

                MultiSiteRoughGainCodeIndex[i] = preSetCoareseGainCode;

                uDutTrimResult[i] = 0u;
                bDutNoNeedTrim[i] = false;
                bDutValid[i] = false;
                bGainBoost[i] = false;
            }
            #endregion Define Parameters

            #region Get module current
            /*  power on */
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_5V);
            RePower();
            Delay(Delay_Sync);

            if (!this.cb_MeasureiQ_AutoTab.Checked)
            {
                /* Get module current */
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VCS);
                Delay(Delay_Sync);
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_SET_CURRENT_SENCE);

                this.txt_ModuleCurrent_EngT.Text = GetModuleCurrent().ToString("F1");
                this.txt_ModuleCurrent_PreT.Text = this.txt_ModuleCurrent_EngT.Text;


                dModuleCurrent = GetModuleCurrent();
                sDUT.dIQ = dModuleCurrent;
                if (dCurrentDownLimit > dModuleCurrent)
                {
                    DisplayOperateMes("Module " + " current is " + dModuleCurrent.ToString("F3"), Color.Red);
                    //uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_CURRENT_ABNORMAL;
                    PowerOff();
                    //PrintDutAttribute(sDUT);
                    MessageBox.Show(String.Format("电流偏低，检查模组是否连接！"), "Warning", MessageBoxButtons.OK);
                    return;
                }
                else if (dModuleCurrent > dCurrentUpLimit)
                {
                    DisplayOperateMes("Module " + " current is " + dModuleCurrent.ToString("F3"), Color.Red);
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_CURRENT_HIGH;
                    PowerOff();
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    //MessageBox.Show(String.Format("电流异常，模块短路或损坏！"), "Error", MessageBoxButtons.OK);
                    this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                    this.txt_Status_AutoTab.Text = "短路!";
                    return;
                }
                else
                    DisplayOperateMes("Module " + " current is " + dModuleCurrent.ToString("F3"));
            }
            #endregion Get module current

            #region Saturation judgement


            //Redundency delay in case of power off failure.
            Delay(Delay_Sync);
            EnterTestMode();
            //Delay(Delay_Sync);
            BurstRead(0x80, 5, tempReadback);
            if (tempReadback[0] + tempReadback[1] + tempReadback[2] + tempReadback[3] + tempReadback[4] != 0)
            {
                DisplayOperateMes("DUT" + " has some bits Blown!", Color.Red);
                uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_TRIMMED_SOMEBITS;
                TrimFinish();
                sDUT.bTrimmed = false;
                sDUT.iErrorCode = uDutTrimResult[idut];
                PrintDutAttribute(sDUT);
                this.txt_Status_AutoTab.ForeColor = Color.Red;
                this.txt_Status_AutoTab.Text = "FAIL!";
                return;
            }



            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            DisplayOperateMes(string.Format("0x80 = 0x{0};\r\n0x81 = 0x{1};\r\n0x82 = 0x{2};\r\n0x83 = 0x{3}",
                MultiSiteReg0[idut].ToString("X2"), MultiSiteReg1[idut].ToString("X2"), MultiSiteReg2[idut].ToString("X2"), MultiSiteReg3[idut].ToString("X2")));
            Delay(Delay_Sync);
            RegisterWrite(4, new uint[8] { 0x84, MultiSiteReg4[idut], 0x85, MultiSiteReg5[idut], 0x86, MultiSiteReg6[idut], 0x87, MultiSiteReg7[idut] });
            Delay(Delay_Sync);
            BurstRead(0x80, 5, tempReadback);
            if (tempReadback[0] != MultiSiteReg0[idut] || tempReadback[1] != MultiSiteReg1[idut]
                || tempReadback[2] != MultiSiteReg2[idut] || tempReadback[3] != MultiSiteReg3[idut])
            {
                if (tempReadback[0] + tempReadback[1] + tempReadback[2] + tempReadback[3] + tempReadback[4] == 0)
                {
                    RePower();
                    Delay(Delay_Sync);
                    oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);
                    Delay(Delay_Sync);
                    oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);
                    Delay(Delay_Fuse);
                    dMultiSiteVout0A[idut] = AverageVout();
                    if (dMultiSiteVout0A[idut] < 4.5 && dMultiSiteVout0A[idut] > 1.5)
                    {
                        DisplayOperateMes("DUT Trimmed!", Color.Red);
                        uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_TRIMMRD_ALREADY;
                        TrimFinish();
                        sDUT.bDigitalCommFail = true;
                        sDUT.bTrimmed = true;
                        sDUT.iErrorCode = uDutTrimResult[idut];
                        PrintDutAttribute(sDUT);
                        //MessageBox.Show(String.Format("模组已编程，交至研发部！"), "Error", MessageBoxButtons.OK);
                        this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                        this.txt_Status_AutoTab.Text = "混料!";
                        return;
                    }
                    else
                    {
                        DisplayOperateMes("VOUT Short!", Color.Red);
                        uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_VOUT_SHORT;
                        TrimFinish();
                        sDUT.bDigitalCommFail = true;
                        sDUT.iErrorCode = uDutTrimResult[idut];
                        PrintDutAttribute(sDUT);
                        //MessageBox.Show(String.Format("输出管脚短路！", Color.YellowGreen), "Warning", MessageBoxButtons.OK);
                        this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                        this.txt_Status_AutoTab.Text = "短路!";
                        return;
                    }
                }
                else
                {
                    DisplayOperateMes("DUT digital communication fail!", Color.Red);
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_COMM_FAIL;
                    TrimFinish();
                    sDUT.bDigitalCommFail = true;
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    //MessageBox.Show(String.Format("输出管脚短路！", Color.YellowGreen), "Warning", MessageBoxButtons.OK);
                    this.txt_Status_AutoTab.ForeColor = Color.Red;
                    this.txt_Status_AutoTab.Text = "FAIL!";
                    return;
                }
            }
            /* Get vout @ IP */
            EnterNomalMode();

            /* Change Current to IP  */
            if (ProgramMode == 0)
            {
                if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                    DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", IP));
                else
                {
                    DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                    TrimFinish();
                    return;
                }
            }
            else if (ProgramMode == 1)
            {
                dr = MessageBox.Show(String.Format("请将电流升至{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    PowerOff();
                    RestoreRegValue();
                    return;
                }
            }
            else if (ProgramMode == 2)
            {
                //set epio1 and epio3 to low
                MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                Delay(Delay_Sync);
                MultiSiteSocketSelect(0);       //set epio1 = high; epio3 = low
            }


            //oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);
            Delay(Delay_Fuse);
            dMultiSiteVoutIP[idut] = AverageVout();
            sDUT.dVoutIPNative = dMultiSiteVoutIP[idut];
            DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));

            /*Judge PreSet gain; delta Vout target >= delta Vout test * 86.07% */
            if (dMultiSiteVoutIP[idut] > saturationVout)
            {
                if (ProgramMode == 0)
                {
                    oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0);
                    Delay(Delay_Sync);
                    oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, 5);
                    Delay(Delay_Sync);
                    if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                        DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", 5));
                    else
                    {
                        DisplayOperateMes(string.Format("Set Current to {0}A failed!", 5));
                        TrimFinish();
                        return;
                    }
                }
                else if (ProgramMode == 1)
                {
                    dr = MessageBox.Show(String.Format("请将电流升至{0}A", 5), "Change Current", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                    {
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        PowerOff();
                        RestoreRegValue();
                        return;
                    }
                }
                else if (ProgramMode == 2)
                {
                    MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                    Delay(Delay_Sync);
                    MultiSiteSocketSelect(0);       //set epio1 = high; epio3 = low
                }


                Delay(Delay_Fuse);
                dMultiSiteVoutIP[idut] = AverageVout();
                sDUT.dVoutIPNative = dMultiSiteVoutIP[idut];
                DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));

                //set current back to IP
                if (ProgramMode == 0)
                {
                    oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0);
                    Delay(Delay_Sync);
                    oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, Convert.ToUInt32(IP));
                }


                if (dMultiSiteVoutIP[idut] > saturationVout)
                {
                    DisplayOperateMes("Module" + " Vout is VDD!", Color.Red);
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_VOUT_VDD;
                    TrimFinish();
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                    this.txt_Status_AutoTab.Text = "短路!";
                    return;
                }
                else
                {
                    //dr = MessageBox.Show(String.Format("输出饱和，交研发部重新编程！"), "Warning", MessageBoxButtons.OK);
                    TrimFinish();
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_VOUT_SATURATION;
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                    this.txt_Status_AutoTab.Text = "饱和!";
                    return;
                }
            }
            else if (dMultiSiteVoutIP[idut] < minimumVoutIP)
            {
                DisplayOperateMes("Module" + " Vout is too Low!", Color.Red);
                uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_VOUT_LOW;
                TrimFinish();
                sDUT.iErrorCode = uDutTrimResult[idut];
                PrintDutAttribute(sDUT);
                this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                this.txt_Status_AutoTab.Text = "短路!";
                return;
            }

            #endregion Saturation judgement

            #region Get Vout@0A
            /* Change Current to 0A */
            if (ProgramMode == 0)
            {
                if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0u))
                    DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", 0u));
                else
                {
                    DisplayOperateMes(string.Format("Set Current to {0}A failed!", 0u));
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    TrimFinish();
                    return;
                }
            }
            else if (ProgramMode == 1)
            {
                dr = MessageBox.Show(String.Format("请将IP降至0A!"), "Try Again", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    PowerOff();
                    RestoreRegValue();
                    return;
                }
            }
            else if (ProgramMode == 2)
            {
                MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                Delay(Delay_Sync);
                MultiSiteSocketSelect(9);       //set epio1 = low; epio3 = high
            }

            Delay(Delay_Fuse);
            dMultiSiteVout0A[idut] = AverageVout();
            sDUT.dVout0ANative = dMultiSiteVout0A[idut];
            DisplayOperateMes("Vout" + " @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

            if (dMultiSiteVoutIP[idut] < dMultiSiteVout0A[idut])
            {
                TrimFinish();
                //PrintDutAttribute(sDUT);
                MessageBox.Show(String.Format("请确认IP方向!"), "Try Again", MessageBoxButtons.OK);
                return;
            }
            else if (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut] < VoutIPThreshold)
            {
                TrimFinish();
                //PrintDutAttribute(sDUT);
                MessageBox.Show(String.Format("请确认电流为{0}A!!!", IP), "Try Again", MessageBoxButtons.OK);
                return;
            }

            if (TargetOffset == 2.5)
            {
                if (dMultiSiteVout0A[idut] < 2.25 || dMultiSiteVout0A[idut] > 2.8)
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_OFFSET_ABN;
                    TrimFinish();
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    this.txt_Status_AutoTab.ForeColor = Color.Red;
                    this.txt_Status_AutoTab.Text = "FAIL!";
                    return;
                }
            }
            else if (TargetOffset == 1.65)
            {
                if (dMultiSiteVout0A[idut] < 1.0 || dMultiSiteVout0A[idut] > 2.5)
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_OFFSET_ABN;
                    TrimFinish();
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    this.txt_Status_AutoTab.ForeColor = Color.Red;
                    this.txt_Status_AutoTab.Text = "FAIL!";
                    return;
                }
            }

            #endregion  Get Vout@0A

            #region No need Trim case
            if ((TargetOffset - 0.001) <= dMultiSiteVout0A[idut] && dMultiSiteVout0A[idut] <= (TargetOffset + 0.001)
                && (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= (TargetVoltage_customer + 0.001)
                && (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= (TargetVoltage_customer - 0.001))
            {
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_EXT);
                Delay(Delay_Sync);
                RePower();
                //Delay(Delay_Sync);
                EnterTestMode();

                RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
                Delay(Delay_Sync);
                RegisterWrite(4, new uint[8] { 0x84, MultiSiteReg4[idut], 0x85, MultiSiteReg5[idut], 0x86, MultiSiteReg6[idut], 0x87, MultiSiteReg7[idut] });
                Delay(Delay_Sync);


                BurstRead(0x80, 5, tempReadback);
                /* fuse */
                FuseClockOn(DeviceAddress, (double)num_UD_pulsewidth_ow_EngT.Value, (double)numUD_pulsedurationtime_ow_EngT.Value);
                DisplayOperateMes("Processing...");
                //Delay(Delay_Fuse);             

                //capture Vout
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_5V);
                RePower();
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);
                Delay(Delay_Sync);
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);

                Delay(Delay_Sync);
                dMultiSiteVout0A[idut] = AverageVout();
                sDUT.dVout0ATrimmed = dMultiSiteVout0A[idut];
                DisplayOperateMes("Vout" + " @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

                /* Change Current to IP  */
                if (ProgramMode == 0)
                {
                    if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                        DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", IP));
                    else
                    {
                        DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        TrimFinish();
                        return;
                    }
                }
                else if (ProgramMode == 1)
                {
                    dr = MessageBox.Show(String.Format("请将电流升至{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                    {
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        PowerOff();
                        RestoreRegValue();
                        return;
                    }
                }
                else if (ProgramMode == 2)
                {
                    MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                    Delay(Delay_Sync);
                    MultiSiteSocketSelect(0);       //set epio1 = high; epio3 = low
                }

                Delay(Delay_Fuse);
                dMultiSiteVoutIP[idut] = AverageVout();
                sDUT.dVoutIPTrimmed = dMultiSiteVoutIP[idut];
                DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));
                //oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_5V);

                DisplayOperateMes("DUT" + idut.ToString() + "Pass! Bin Normal");
                //uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_NORMAL;
                uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_1;
                sDUT.iErrorCode = uDutTrimResult[idut];
                this.txt_Status_AutoTab.ForeColor = Color.Green;
                this.txt_Status_AutoTab.Text = "PASS!";
                DisplayOperateMes("Bin" + " = " + uDutTrimResult[idut].ToString());
                DisplayOperateMes("Marginal Read ->" + bMarginal.ToString());
                DisplayOperateMes("Safety REad ->" + bSafety.ToString());
                MultiSiteDisplayResult(uDutTrimResult);
                TrimFinish();
                PrintDutAttribute(sDUT);
                return;

            }


            #endregion No need Trim case

            #region Adapting algorithm

            tempG1 = sl620CoarseGainTable[0][MultiSiteRoughGainCodeIndex[idut]] / 100d;
            tempG2 = (TargetGain_customer / ((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) / IP)) / 1000d;

            autoAdaptingGoughGain = tempG1 * tempG2 * 100d;
            DisplayOperateMes("IdealGoughGain = " + autoAdaptingGoughGain.ToString("F4"));

            Ix_forAutoAdaptingRoughGain = LookupCoarseGain_SL620(autoAdaptingGoughGain, sl620CoarseGainTable);
            autoAdaptingPresionGain = 100d * autoAdaptingGoughGain / sl620CoarseGainTable[0][Ix_forAutoAdaptingRoughGain];
            //Ix_forAutoAdaptingPresionGain = LookupFineGain_SL620(autoAdaptingPresionGain, sl620FineGainTable);
            if (bAutoTrimTest)
            {
                DisplayOperateMes("IP = " + IP.ToString("F0"));
                DisplayOperateMes("TargetGain_customer" + idut.ToString() + " = " + TargetGain_customer.ToString("F4"));
                DisplayOperateMes("(dMultiSiteVoutIP - dMultiSiteVout0A)/IP = " + ((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) / IP).ToString("F4"));
                DisplayOperateMes("Pre-set gain" + idut.ToString() + " = " + tempG1.ToString("F4"));
                DisplayOperateMes("Target gain / Test gain" + idut.ToString() + " = " + tempG2.ToString("F4"));
                DisplayOperateMes("Ix_forAutoAdaptingRoughGain" + idut.ToString() + " = " + Ix_forAutoAdaptingRoughGain.ToString("F0"));
                DisplayOperateMes("Ix_forAutoAdaptingPresionGain" + idut.ToString() + " = " + Ix_forAutoAdaptingPresionGain.ToString("F0"));
                DisplayOperateMes("autoAdaptingGoughGain" + idut.ToString() + " = " + sl620CoarseGainTable[0][Ix_forAutoAdaptingRoughGain].ToString("F4"));
                DisplayOperateMes("autoAdaptingPresionGain" + idut.ToString() + " = " + sl620FineGainTable[0][Ix_forAutoAdaptingPresionGain].ToString("F4"));
            }

            /* Rough Gain Code*/
            bit_op_mask = bit4_Mask | bit5_Mask | bit6_Mask | bit7_Mask;
            MultiSiteReg1[idut] &= ~bit_op_mask;
            MultiSiteReg1[idut] |= Convert.ToUInt32(sl620CoarseGainTable[1][Ix_forAutoAdaptingRoughGain]);


            if (bAutoTrimTest)
            {
                //DisplayOperateMes("Rough Gain RegValue80 = 0x" + MultiSiteReg0[idut].ToString("X2"));
                DisplayOperateMes("Rough Gain RegValue81 = 0x" + MultiSiteReg1[idut].ToString("X2"));
            }

            /******************************************************************************
            *
            *   Coarse Offset Alg
            *
            ******************************************************************************/
            RePower();
            EnterTestMode();

            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            Delay(Delay_Sync);
            RegisterWrite(4, new uint[8] { 0x84, MultiSiteReg4[idut], 0x85, MultiSiteReg5[idut], 0x86, MultiSiteReg6[idut], 0x87, MultiSiteReg7[idut] });
            Delay(Delay_Sync);

            //RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            EnterNomalMode();
            Delay(Delay_Fuse);
            /* Offset trim code calculate */
            dMultiSiteVout0A[idut] = AverageVout();
            Vout_0A = dMultiSiteVout0A[idut];


            //bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask | bit3_Mask | bit4_Mask;
            uint ix_CoarseOffsetCode = 0;
            if (Vout_0A > TargetOffset)
            {
                ix_CoarseOffsetCode = Convert.ToUInt32(Math.Round(1000d * (1.0d - TargetOffset / Vout_0A) / 5));
                if (ix_CoarseOffsetCode > 15)
                    ix_CoarseOffsetCode = 15;

                //autoAdaptingGoughGain = sl620CoarseGainTable[0][Ix_forAutoAdaptingRoughGain] / (1.0 - ix_CoarseOffsetCode * 0.005);
                //autoAdaptingGoughGain = tempG2 * tempG1 * 100d / (1.0 - ix_CoarseOffsetCode * 0.005);

                //if (TargetOffset == 2.5)
                //    Ix_forAutoAdaptingRoughGain = LookupCoarseGain_SL620(autoAdaptingGoughGain, sl620CoarseGainTable);
                ////1.65V case
                //else
                //    Ix_forAutoAdaptingRoughGain = LookupCoarseGain_SL620(autoAdaptingGoughGain, sl620CoarseGainTable);
                ///* Rough Gain Code*/
                //bit_op_mask = bit4_Mask | bit5_Mask | bit6_Mask | bit7_Mask;
                //MultiSiteReg1[idut] &= ~bit_op_mask;
                //MultiSiteReg1[idut] |= Convert.ToUInt32(sl620CoarseGainTable[1][Ix_forAutoAdaptingRoughGain]);


            }
            else if (Vout_0A < TargetOffset)
            {
                ix_CoarseOffsetCode = 31 - Convert.ToUInt32(Math.Round(1000d * (TargetOffset / Vout_0A - 1.0d) / 5));
                //if (ix_CoarseOffsetCode == 32)
                //    ix_CoarseOffsetCode = 0;
                if (ix_CoarseOffsetCode < 16)
                    ix_CoarseOffsetCode = 16;

                //autoAdaptingGoughGain = sl620CoarseGainTable[0][Ix_forAutoAdaptingRoughGain] / (1.0 + ix_CoarseOffsetCode * 0.005);
                //autoAdaptingGoughGain = tempG2 * tempG1 * 100d / (1.0 + (31 - ix_CoarseOffsetCode) * 0.005);

                //if (TargetOffset == 2.5)
                //    Ix_forAutoAdaptingRoughGain = LookupCoarseGain_SL620(autoAdaptingGoughGain, sl620CoarseGainTable);
                ////1.65V case
                //else
                //    Ix_forAutoAdaptingRoughGain = LookupCoarseGain_SL620(autoAdaptingGoughGain, sl620CoarseGainTable);

                ///* Rough Gain Code*/
                //bit_op_mask = bit4_Mask | bit5_Mask | bit6_Mask | bit7_Mask;
                //MultiSiteReg1[idut] &= ~bit_op_mask;
                //MultiSiteReg1[idut] |= Convert.ToUInt32(sl620CoarseGainTable[1][Ix_forAutoAdaptingRoughGain]);
            }

            bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask | bit3_Mask | bit4_Mask;

            MultiSiteReg6[idut] &= ~bit_op_mask;
            MultiSiteReg6[idut] |= ix_CoarseOffsetCode;

            DisplayOperateMes("Vout_0A = " + Vout_0A.ToString("F3"));
            DisplayOperateMes("ix_CoarseOffsetCode = " + ix_CoarseOffsetCode.ToString());
            /////////////////////////////////////////////////////////////////////////////////////////////////




            RePower();
            EnterTestMode();

            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            Delay(Delay_Sync);
            RegisterWrite(4, new uint[8] { 0x84, MultiSiteReg4[idut], 0x85, MultiSiteReg5[idut], 0x86, MultiSiteReg6[idut], 0x87, MultiSiteReg7[idut] });
            Delay(Delay_Sync);

            EnterNomalMode();

            #region /* Change Current to IP  */
            if (ProgramMode == 0)
            {
                if (!oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                {
                    DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    TrimFinish();
                    return;
                }
            }
            else if (ProgramMode == 1)
            {
                dr = MessageBox.Show(String.Format("请将电流升至{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    PowerOff();
                    RestoreRegValue();
                    return;
                }
            }
            else if (ProgramMode == 2)
            {
                MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                Delay(Delay_Sync);
                MultiSiteSocketSelect(0);       //set epio1 = high; epio3 = low
            }
            #endregion

            Delay(Delay_Fuse);
            dMultiSiteVoutIP[idut] = AverageVout();
            DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));

            #region Change Current to 0A */
            if (ProgramMode == 0)
            {
                if (!oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0u))
                {
                    DisplayOperateMes(string.Format("Set Current to {0}A failed!", 0u));
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    TrimFinish();
                    return;
                }
            }
            else if (ProgramMode == 1)
            {
                dr = MessageBox.Show(String.Format("请将电流降至0A"), "Change Current", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    PowerOff();
                    RestoreRegValue();
                    return;
                }
            }
            else if (ProgramMode == 2)
            {
                MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                Delay(Delay_Sync);
                MultiSiteSocketSelect(9);       //set epio1 = low; epio3 = high
            }
            #endregion

            /*  power on */
            Delay(Delay_Fuse);
            dMultiSiteVout0A[idut] = AverageVout();
            DisplayOperateMes("DUT" + " Vout @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

            double gainTest = 0;

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            gainTest = (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) * 1000d / IP;

            if (gainTest < TargetGain_customer * 0.995)
            {
                DisplayOperateMes("Tuning coarse gain", Color.DarkRed);

                /* Rough Gain Code*/
                if (Ix_forAutoAdaptingRoughGain > 0)
                {
                    bit_op_mask = bit4_Mask | bit5_Mask | bit6_Mask | bit7_Mask;
                    MultiSiteReg1[idut] &= ~bit_op_mask;
                    MultiSiteReg1[idut] |= Convert.ToUInt32(sl620CoarseGainTable[1][Ix_forAutoAdaptingRoughGain - 1]);

                    RePower();
                    EnterTestMode();

                    RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
                    Delay(Delay_Sync);
                    RegisterWrite(4, new uint[8] { 0x84, MultiSiteReg4[idut], 0x85, MultiSiteReg5[idut], 0x86, MultiSiteReg6[idut], 0x87, MultiSiteReg7[idut] });
                    Delay(Delay_Sync);

                    EnterNomalMode();

                    #region /* Change Current to IP  */
                    if (ProgramMode == 0)
                    {
                        if (!oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                        {
                            DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                            DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                            TrimFinish();
                            return;
                        }
                    }
                    else if (ProgramMode == 1)
                    {
                        dr = MessageBox.Show(String.Format("请将电流升至{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                        if (dr == DialogResult.Cancel)
                        {
                            DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                            PowerOff();
                            RestoreRegValue();
                            return;
                        }
                    }
                    else if (ProgramMode == 2)
                    {
                        MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                        Delay(Delay_Sync);
                        MultiSiteSocketSelect(0);       //set epio1 = high; epio3 = low
                    }
                    #endregion

                    Delay(Delay_Fuse);
                    dMultiSiteVoutIP[idut] = AverageVout();
                    DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));

                    #region Change Current to 0A */
                    if (ProgramMode == 0)
                    {
                        if (!oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0u))
                        {
                            DisplayOperateMes(string.Format("Set Current to {0}A failed!", 0u));
                            DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                            TrimFinish();
                            return;
                        }
                    }
                    else if (ProgramMode == 1)
                    {
                        dr = MessageBox.Show(String.Format("请将电流降至0A"), "Change Current", MessageBoxButtons.OKCancel);
                        if (dr == DialogResult.Cancel)
                        {
                            DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                            PowerOff();
                            RestoreRegValue();
                            return;
                        }
                    }
                    else if (ProgramMode == 2)
                    {
                        MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                        Delay(Delay_Sync);
                        MultiSiteSocketSelect(9);       //set epio1 = low; epio3 = high
                    }
                    #endregion

                    /*  power on */
                    Delay(Delay_Fuse);
                    dMultiSiteVout0A[idut] = AverageVout();
                    DisplayOperateMes("DUT" + " Vout @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

                    gainTest = 0;

                    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    gainTest = (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) * 1000d / IP;
                }
                else
                {
                    DisplayOperateMes("coarsegaincode = " + Ix_forAutoAdaptingRoughGain.ToString());
                    DisplayOperateMes("不适合做此产品，此芯片可用灵敏度更低的产品！", Color.DarkRed);
                    return;
                }

            }

            autoAdaptingPresionGain = 100d * (TargetGain_customer / gainTest);
            Ix_forAutoAdaptingPresionGain = LookupFineGain_SL620(autoAdaptingPresionGain, sl620FineGainTable);

            /* Fine Gain Code*/
            bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
            MultiSiteReg6[idut] &= ~bit_op_mask;
            MultiSiteReg6[idut] |= Convert.ToUInt32(sl620FineGainTable[1][Ix_forAutoAdaptingPresionGain]);

            bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
            MultiSiteReg7[idut] &= ~bit_op_mask;
            MultiSiteReg7[idut] |= Convert.ToUInt32(sl620FineGainTable[2][Ix_forAutoAdaptingPresionGain]);

            if (bAutoTrimTest)
            {
                DisplayOperateMes("Precesion Gain RegValue86 = 0x" + MultiSiteReg6[idut].ToString("X2"));
                DisplayOperateMes("Precesion Gain RegValue87 = 0x" + MultiSiteReg7[idut].ToString("X2"));
                DisplayOperateMes("***new add approach***");
            }
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////         

            RePower();
            EnterTestMode();

            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            Delay(Delay_Sync * 2);
            RegisterWrite(4, new uint[8] { 0x84, MultiSiteReg4[idut], 0x85, MultiSiteReg5[idut], 0x86, MultiSiteReg6[idut], 0x87, MultiSiteReg7[idut] });
            Delay(Delay_Sync);

            BurstRead(0x80, 4, tempReadback);
            BurstRead(0x85, 4, tempReadback);

            //RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            EnterNomalMode();
            Delay(Delay_Fuse);
            dMultiSiteVout0A[idut] = AverageVout();
            Vout_0A = dMultiSiteVout0A[idut];

            ///////////////////////////////////////////////////////////////////////////////////////////////////
            bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask | bit3_Mask | bit4_Mask;
            uint ix_FineOffsetCode = 0;
            if (Vout_0A > TargetOffset)
                ix_FineOffsetCode = Convert.ToUInt32(Math.Round(1000d * (1.0d - TargetOffset / Vout_0A) / 1.5));
            else if (Vout_0A < TargetOffset)
                ix_FineOffsetCode = 31 - Convert.ToUInt32(Math.Round(1000d * (TargetOffset / Vout_0A - 1.0d) / 1.5));

            MultiSiteReg7[idut] &= ~bit_op_mask;
            MultiSiteReg7[idut] |= ix_FineOffsetCode;

            //ix_forOffsetIndex_Rough = LookupOffsetIndex(MultiSiteReg3[idut] & bit_op_mask, OffsetTableB_Customer);
            //ix_FineOffsetCode = ix_forOffsetIndex_Rough;
            DisplayOperateMes("Vout_0A = " + Vout_0A.ToString("F3"));
            DisplayOperateMes("ix_FineOffsetCode = " + ix_FineOffsetCode.ToString());
            DisplayOperateMes("\r\nProcessing...");

            //*******************Additinal test case ***********************
            RePower();
            EnterTestMode();

            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            Delay(Delay_Sync);
            RegisterWrite(4, new uint[8] { 0x84, MultiSiteReg4[idut], 0x85, MultiSiteReg5[idut], 0x86, MultiSiteReg6[idut], 0x87, MultiSiteReg7[idut] });
            Delay(Delay_Sync);

            EnterNomalMode();

            Delay(Delay_Power);
            dMultiSiteVout0A[idut] = AverageVout();
            DisplayOperateMes("DUT" + " Vout @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

            //gainTest = (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) * 1000d / IP;
            if (dMultiSiteVout0A[idut] - TargetOffset >= 0.004)
            {
                //ix_FineOffsetCode += Convert.ToUInt32(Math.Round((dMultiSiteVout0A[idut] - TargetOffset) / 0.004));
                //DisplayOperateMes("ix_FineOffsetCode = " + ix_FineOffsetCode.ToString());

                if (ix_FineOffsetCode == 15)
                    ix_FineOffsetCode = 15;
                else if (ix_FineOffsetCode == 31)
                    ix_FineOffsetCode = 0;
                else
                    ix_FineOffsetCode++;

                DisplayOperateMes("ix_FineOffsetCode = " + ix_FineOffsetCode.ToString());

                /* Fine offset Code*/
                bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask | bit3_Mask | bit4_Mask;
                MultiSiteReg7[idut] &= ~bit_op_mask;
                MultiSiteReg7[idut] |= ix_FineOffsetCode;
            }
            else if (TargetOffset - dMultiSiteVout0A[idut] >= 0.004)
            {
                if (ix_FineOffsetCode == 0)
                    ix_FineOffsetCode = 31;
                else if (ix_FineOffsetCode == 16)
                    ix_FineOffsetCode = 16;
                else
                    ix_FineOffsetCode--;

                DisplayOperateMes("ix_FineOffsetCode = " + ix_FineOffsetCode.ToString());

                /* Fine offset Code*/
                bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask | bit3_Mask | bit4_Mask;
                MultiSiteReg7[idut] &= ~bit_op_mask;
                MultiSiteReg7[idut] |= ix_FineOffsetCode;
            }
            //********************************************************

            DisplayOperateMes("Processing...");

            #endregion Adapting algorithm

            #region Fuse
            //Fuse
            //oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_EXT);
            RePower();
            EnterTestMode();

            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            Delay(Delay_Sync);
            RegisterWrite(4, new uint[8] { 0x84, MultiSiteReg4[idut], 0x85, MultiSiteReg5[idut], 0x86, MultiSiteReg6[idut], 0x87, MultiSiteReg7[idut] });
            Delay(Delay_Sync);
            RegisterWrite(1, new uint[2] { 0x88, 0x02 });

            if (this.cb_BypFuse_AutoTab.Checked)
            {
                printRegValue();
                TrimFinish();
                return;
            }

            FuseClockOn(DeviceAddress, (double)num_UD_pulsewidth_ow_EngT.Value, (double)numUD_pulsedurationtime_ow_EngT.Value);
            DisplayOperateMes("Trimming...");

            #endregion

            #region Bin

            if (this.cb_AutoTab_Retest.Text == "Yes")
            {
                /* Repower on 5V */
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);
                Delay(Delay_Sync);
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_5V);
                RePower();
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);
                Delay(Delay_Fuse);

                dMultiSiteVout0A[idut] = AverageVout();
                sDUT.dVout0ATrimmed = dMultiSiteVout0A[idut];
                DisplayOperateMes("Vout" + " @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

                /* Change Current to IP  */
                if (ProgramMode == 0)
                {
                    //if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, Convert.ToUInt32(IP)))
                    if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                        DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", IP));
                    else
                    {
                        DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        TrimFinish();
                        return;
                    }
                }
                else if (ProgramMode == 1)
                {
                    dr = MessageBox.Show(String.Format("请将电流升至{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                    {
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        PowerOff();
                        RestoreRegValue();
                        return;
                    }
                }
                else if (ProgramMode == 2)
                {
                    MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                    Delay(Delay_Sync);
                    MultiSiteSocketSelect(0);       //set epio1 = high; epio3 = low
                }

                Delay(Delay_Fuse);
                dMultiSiteVoutIP[idut] = AverageVout();
                sDUT.dVoutIPTrimmed = dMultiSiteVoutIP[idut];
                DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));

                /* bin1,2,3 */
                if (TargetOffset * (1 - 0.002) <= dMultiSiteVout0A[idut] && dMultiSiteVout0A[idut] <= TargetOffset * (1 + 0.002) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= TargetVoltage_customer * (1 + 0.002) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= TargetVoltage_customer * (1 - 0.002))
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_1;
                    this.txt_Status_AutoTab.ForeColor = Color.Green;
                    this.txt_Status_AutoTab.Text = "PASS!";
                }
                else if (TargetOffset * (1 - bin2accuracy / 100d) <= dMultiSiteVout0A[idut] &&
                    dMultiSiteVout0A[idut] <= TargetOffset * (1 + bin2accuracy / 100d) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= TargetVoltage_customer * (1 + bin3accuracy / 100d) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= TargetVoltage_customer * (1 - bin3accuracy / 100d))
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_2;
                    this.txt_Status_AutoTab.ForeColor = Color.Green;
                    this.txt_Status_AutoTab.Text = "PASS!";
                }
                else if (TargetOffset * (1 - bin2accuracy / 100d) <= dMultiSiteVout0A[idut] &&
                    dMultiSiteVout0A[idut] <= TargetOffset * (1 + bin2accuracy / 100d) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= TargetVoltage_customer * (1 + bin3accuracy / 100d) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= TargetVoltage_customer * (1 - bin3accuracy / 100d))
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_3;
                    this.txt_Status_AutoTab.ForeColor = Color.Green;
                    this.txt_Status_AutoTab.Text = "PASS!";
                }
                else
                {
                    this.txt_Status_AutoTab.ForeColor = Color.Red;
                    this.txt_Status_AutoTab.Text = "FAIL!";
                }
            }
            else if (this.cb_AutoTab_Retest.Text == "No")
            {
                /* Repower on 5V */
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);
                Delay(Delay_Sync);
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_5V);
                RePower();
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);
                Delay(Delay_Fuse);

                dMultiSiteVout0A[idut] = AverageVout();
                sDUT.dVout0ATrimmed = dMultiSiteVout0A[idut];
                DisplayOperateMes("Vout" + " @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

                /* bin1,2,3 */
                if (TargetOffset * (1 - 0.002) <= dMultiSiteVout0A[idut] && dMultiSiteVout0A[idut] <= TargetOffset * (1 + 0.002))
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_1;
                    this.txt_Status_AutoTab.ForeColor = Color.Green;
                    this.txt_Status_AutoTab.Text = "PASS!";
                }
                else if (TargetOffset * (1 - bin2accuracy / 100d) <= dMultiSiteVout0A[idut] &&
                    dMultiSiteVout0A[idut] <= TargetOffset * (1 + bin2accuracy / 100d))
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_2;
                    this.txt_Status_AutoTab.ForeColor = Color.Green;
                    this.txt_Status_AutoTab.Text = "PASS!";
                }
                else
                {
                    this.txt_Status_AutoTab.ForeColor = Color.Red;
                    this.txt_Status_AutoTab.Text = "FAIL!";
                }
            }

            #endregion Bin

            #region Display Result and Reset parameters
            DisplayOperateMes("Bin" + " = " + uDutTrimResult[idut].ToString());
            MultiSiteDisplayResult(uDutTrimResult);
            TrimFinish();
            sDUT.iErrorCode = uDutTrimResult[idut];
            PrintDutAttribute(sDUT);
            DisplayOperateMes("Next...");
            #endregion Display Result and Reset parameters
        }

        private void AutoTrim_SL620_SingleEnd_HalfVDD()
        {
            #region Define Parameters
            DialogResult dr;
            bool bMarginal = false;
            bool bSafety = false;
            //uint[] tempReadback = new uint[5];
            double dVout_0A_Temp = 0;
            //TargetOffset = 2.5;
            double dVip_Target = TargetOffset + TargetVoltage_customer;
            //double dGainTestMinusTarget = 1;
            //double dGainTest = 0;
            ModuleAttribute sDUT;
            sDUT.dIQ = 0;
            sDUT.dVoutIPNative = 0;
            sDUT.dVout0ANative = 0;
            sDUT.dVoutIPMiddle = 0;
            sDUT.dVout0AMiddle = 0;
            sDUT.dVoutIPTrimmed = 0;
            sDUT.dVout0ATrimmed = 0;
            sDUT.iErrorCode = 00;
            sDUT.bDigitalCommFail = false;
            sDUT.bNormalModeFail = false;
            sDUT.bReadMarginal = false;
            sDUT.bReadSafety = false;
            sDUT.bTrimmed = false;

            // PARAMETERS DEFINE FOR MULTISITE
            uint idut = 0;
            uint uDutCount = 16;
            //bool bValidRound = false;
            //bool bSecondCurrentOn = false;
            double dModuleCurrent = 0;
            bool[] bGainBoost = new bool[16];
            bool[] bDutValid = new bool[16];
            bool[] bDutNoNeedTrim = new bool[16];
            uint[] uDutTrimResult = new uint[16];
            double[] dMultiSiteVoutIP = new double[16];
            double[] dMultiSiteVout0A = new double[16];

            /* autoAdaptingGoughGain algorithm*/
            double autoAdaptingGoughGain = 0;
            double autoAdaptingPresionGain = 0;
            double tempG1 = 0;
            double tempG2 = 0;
            //double dGainPreset = 0;
            int Ix_forAutoAdaptingRoughGain = 0;
            int Ix_forAutoAdaptingPresionGain = 0;

            DisplayOperateMes("\r\n************" + DateTime.Now.ToString() + "************");
            DisplayOperateMes("Start...");
            this.txt_Status_AutoTab.ForeColor = Color.Black;
            this.txt_Status_AutoTab.Text = "START!";

            for (uint i = 0; i < uDutCount; i++)
            {
                dMultiSiteVoutIP[i] = 0d;
                dMultiSiteVout0A[i] = 0d;

                MultiSiteReg0[i] = Reg80Value;
                MultiSiteReg1[i] = Reg81Value;
                MultiSiteReg2[i] = Reg82Value;
                MultiSiteReg3[i] = Reg83Value;
                MultiSiteReg4[i] = Reg84Value;
                MultiSiteReg5[i] = Reg85Value;
                MultiSiteReg6[i] = Reg86Value;
                MultiSiteReg7[i] = Reg87Value;

                MultiSiteRoughGainCodeIndex[i] = preSetCoareseGainCode;

                uDutTrimResult[i] = 0u;
                bDutNoNeedTrim[i] = false;
                bDutValid[i] = false;
                bGainBoost[i] = false;
            }
            #endregion Define Parameters

            #region Get module current
            //clear log
            //DisplayOperateMesClear();
            /*  power on */
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_5V);
            RePower();
            Delay(Delay_Sync);
            this.txt_Status_AutoTab.Text = "Trimming!";

            if (!this.cb_MeasureiQ_AutoTab.Checked)
            {
                /* Get module current */
                if (!oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VCS))
                {
                    DisplayOperateMes("Set ADC VIN to VCS failed", Color.Red);
                    PowerOff();
                    return;
                }
                Delay(Delay_Sync);
                if (oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_SET_CURRENT_SENCE))


                    this.txt_ModuleCurrent_EngT.Text = GetModuleCurrent().ToString("F1");
                this.txt_ModuleCurrent_PreT.Text = this.txt_ModuleCurrent_EngT.Text;


                dModuleCurrent = GetModuleCurrent();
                sDUT.dIQ = dModuleCurrent;
                if (dCurrentDownLimit > dModuleCurrent)
                {
                    DisplayOperateMes("Module " + " current is " + dModuleCurrent.ToString("F3"), Color.Red);
                    PowerOff();
                    MessageBox.Show(String.Format("电流偏低，检查模组是否连接！"), "Warning", MessageBoxButtons.OK);
                    return;
                }
                else if (dModuleCurrent > dCurrentUpLimit)
                {
                    DisplayOperateMes("Module " + " current is " + dModuleCurrent.ToString("F3"), Color.Red);
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_CURRENT_HIGH;
                    PowerOff();
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                    this.txt_Status_AutoTab.Text = "短路!";
                    return;
                }
                else
                    DisplayOperateMes("Module " + " current is " + dModuleCurrent.ToString("F3"));
            }

            #endregion Get module current

            #region Saturation judgement


            //Redundency delay in case of power off failure.
            Delay(Delay_Sync);
            EnterTestMode();
            //Delay(Delay_Sync);
            BurstRead(0x80, 5, tempReadback);
            if (tempReadback[0] + tempReadback[1] + tempReadback[2] + tempReadback[3] + tempReadback[4] != 0)
            {
                DisplayOperateMes("DUT" + " has some bits Blown!", Color.Red);
                uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_TRIMMED_SOMEBITS;
                TrimFinish();
                sDUT.bTrimmed = false;
                sDUT.iErrorCode = uDutTrimResult[idut];
                PrintDutAttribute(sDUT);
                this.txt_Status_AutoTab.ForeColor = Color.Red;
                this.txt_Status_AutoTab.Text = "FAIL!";
                return;
            }



            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            DisplayOperateMes(string.Format("0x80 = 0x{0};\r\n0x81 = 0x{1};\r\n0x82 = 0x{2};\r\n0x83 = 0x{3}",
                MultiSiteReg0[idut].ToString("X2"), MultiSiteReg1[idut].ToString("X2"), MultiSiteReg2[idut].ToString("X2"), MultiSiteReg3[idut].ToString("X2")));
            Delay(Delay_Sync);
            RegisterWrite(4, new uint[8] { 0x84, MultiSiteReg4[idut], 0x85, MultiSiteReg5[idut], 0x86, MultiSiteReg6[idut], 0x87, MultiSiteReg7[idut] });
            Delay(Delay_Sync);
            BurstRead(0x80, 5, tempReadback);
            if (tempReadback[0] != MultiSiteReg0[idut] || tempReadback[1] != MultiSiteReg1[idut]
                || tempReadback[2] != MultiSiteReg2[idut] || tempReadback[3] != MultiSiteReg3[idut])
            {
                if (tempReadback[0] + tempReadback[1] + tempReadback[2] + tempReadback[3] + tempReadback[4] == 0)
                {
                    RePower();
                    Delay(Delay_Sync);
                    oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);
                    Delay(Delay_Sync);
                    oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);
                    Delay(Delay_Fuse);
                    dMultiSiteVout0A[idut] = AverageVout();
                    if (dMultiSiteVout0A[idut] < 4.5 && dMultiSiteVout0A[idut] > 1.5)
                    {
                        DisplayOperateMes("DUT Trimmed!", Color.Red);
                        uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_TRIMMRD_ALREADY;
                        TrimFinish();
                        sDUT.bDigitalCommFail = true;
                        sDUT.bTrimmed = true;
                        sDUT.iErrorCode = uDutTrimResult[idut];
                        PrintDutAttribute(sDUT);
                        //MessageBox.Show(String.Format("模组已编程，交至研发部！"), "Error", MessageBoxButtons.OK);
                        this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                        this.txt_Status_AutoTab.Text = "混料!";
                        return;
                    }
                    else
                    {
                        DisplayOperateMes("VOUT Short!", Color.Red);
                        uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_VOUT_SHORT;
                        TrimFinish();
                        sDUT.bDigitalCommFail = true;
                        sDUT.iErrorCode = uDutTrimResult[idut];
                        PrintDutAttribute(sDUT);
                        //MessageBox.Show(String.Format("输出管脚短路！", Color.YellowGreen), "Warning", MessageBoxButtons.OK);
                        this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                        this.txt_Status_AutoTab.Text = "短路!";
                        return;
                    }
                }
                else
                {
                    DisplayOperateMes("DUT digital communication fail!", Color.Red);
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_COMM_FAIL;
                    TrimFinish();
                    sDUT.bDigitalCommFail = true;
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    //MessageBox.Show(String.Format("输出管脚短路！", Color.YellowGreen), "Warning", MessageBoxButtons.OK);
                    this.txt_Status_AutoTab.ForeColor = Color.Red;
                    this.txt_Status_AutoTab.Text = "FAIL!";
                    return;
                }
            }
            /* Get vout @ IP */
            EnterNomalMode();

            #region /* Change Current to IP  */
            if (ProgramMode == 0)
            {
                //if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, Convert.ToUInt32(IP)))
                if ( !oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                {
                    DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                    TrimFinish();
                    return;
                }
            }
            else if (ProgramMode == 1)
            {
                dr = MessageBox.Show(String.Format("请将电流升至{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    PowerOff();
                    RestoreRegValue();
                    return;
                }
            }
            else if (ProgramMode == 2)
            {
                MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                Delay(Delay_Sync);
                MultiSiteSocketSelect(0);       //set epio1 = high; epio3 = low
            }
            #endregion 

            //oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);
            Delay(Delay_Fuse);
            dMultiSiteVoutIP[idut] = AverageVout();
            sDUT.dVoutIPNative = dMultiSiteVoutIP[idut];
            DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));

            /*Judge PreSet gain; delta Vout target >= delta Vout test * 86.07% */
            if (dMultiSiteVoutIP[idut] > saturationVout)
            {
                if (ProgramMode == 0)
                {
                    oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0);
                    Delay(Delay_Sync);
                    oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, 5);
                    Delay(Delay_Sync);
                    if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                        DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", 5));
                    else
                    {
                        DisplayOperateMes(string.Format("Set Current to {0}A failed!", 5));
                        TrimFinish();
                        return;
                    }
                }
                else if (ProgramMode == 1)
                {
                    dr = MessageBox.Show(String.Format("请将电流升至{0}A", 5), "Change Current", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                    {
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        PowerOff();
                        RestoreRegValue();
                        return;
                    }
                }
                else if (ProgramMode == 2)
                {
                    MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                    Delay(Delay_Sync);
                    MultiSiteSocketSelect(0);       //set epio1 = high; epio3 = low
                }

                Delay(Delay_Fuse);
                dMultiSiteVoutIP[idut] = AverageVout();
                sDUT.dVoutIPNative = dMultiSiteVoutIP[idut];
                DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));

                //set current back to IP
                if (ProgramMode == 0)
                {
                    oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0);
                    Delay(Delay_Sync);
                    oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, Convert.ToUInt32(IP));
                }

                if (dMultiSiteVoutIP[idut] > saturationVout)
                {
                    DisplayOperateMes("Module" + " Vout is VDD!", Color.Red);
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_VOUT_VDD;
                    TrimFinish();
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                    this.txt_Status_AutoTab.Text = "短路!";
                    return;
                }
                else
                {
                    //dr = MessageBox.Show(String.Format("输出饱和，交研发部重新编程！"), "Warning", MessageBoxButtons.OK);
                    TrimFinish();
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_VOUT_SATURATION;
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                    this.txt_Status_AutoTab.Text = "饱和!";
                    return;
                }
            }
            else if (dMultiSiteVoutIP[idut] < minimumVoutIP)
            {
                DisplayOperateMes("Module" + " Vout is too Low!", Color.Red);
                uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_VOUT_LOW;
                TrimFinish();
                sDUT.iErrorCode = uDutTrimResult[idut];
                PrintDutAttribute(sDUT);
                this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                this.txt_Status_AutoTab.Text = "短路!";
                return;
            }

            #endregion Saturation judgement

            #region Get Vout@0A

            #region /* Change Current to 0A */
            if (ProgramMode == 0)
            {
                //if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, 0u))
                if ( !oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0u))
                {
                    DisplayOperateMes(string.Format("Set Current to {0}A failed!", 0u));
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    TrimFinish();
                    return;
                }
            }
            else if(ProgramMode == 1)
            {
                dr = MessageBox.Show(String.Format("请将IP降至0A!"), "Try Again", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    PowerOff();
                    RestoreRegValue();
                    return;
                }
            }
            else if (ProgramMode == 2)
            {
                MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                Delay(Delay_Sync);
                MultiSiteSocketSelect(9);       //set epio1 = low; epio3 = high
            }
            #endregion

            Delay(Delay_Fuse);
            dMultiSiteVout0A[idut] = AverageVout();
            sDUT.dVout0ANative = dMultiSiteVout0A[idut];
            DisplayOperateMes("Vout" + " @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

            if (dMultiSiteVoutIP[idut] < dMultiSiteVout0A[idut])
            {
                TrimFinish();
                //PrintDutAttribute(sDUT);
                MessageBox.Show(String.Format("请确认IP方向!"), "Try Again", MessageBoxButtons.OK);
                return;
            }
            else if (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut] < VoutIPThreshold)
            {
                TrimFinish();
                //PrintDutAttribute(sDUT);
                MessageBox.Show(String.Format("请确认电流为{0}A!!!", IP), "Try Again", MessageBoxButtons.OK);
                return;
            }

            if (TargetOffset == 2.5)
            {
                if (dMultiSiteVout0A[idut] < 2.25 || dMultiSiteVout0A[idut] > 2.8)
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_OFFSET_ABN;
                    TrimFinish();
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    this.txt_Status_AutoTab.ForeColor = Color.Red;
                    this.txt_Status_AutoTab.Text = "FAIL!";
                    return;
                }
            }
            else if (TargetOffset == 1.65)
            {
                if (dMultiSiteVout0A[idut] < 1.0 || dMultiSiteVout0A[idut] > 2.5)
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_OFFSET_ABN;
                    TrimFinish();
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    this.txt_Status_AutoTab.ForeColor = Color.Red;
                    this.txt_Status_AutoTab.Text = "FAIL!";
                    return;
                }
            }

            if ((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut])*1000 / IP < TargetGain_customer * 0.998)
            {
                if (MultiSiteRoughGainCodeIndex[idut] > 0)
                {
                    MultiSiteRoughGainCodeIndex[idut]--;
                    /* Rough Gain Code*/
                    bit_op_mask = bit4_Mask | bit5_Mask | bit6_Mask | bit7_Mask;
                    MultiSiteReg1[idut] &= ~bit_op_mask;
                    MultiSiteReg1[idut] |= Convert.ToUInt32(sl620CoarseGainTable[1][MultiSiteRoughGainCodeIndex[idut]]);

                    RePower();
                    Delay(Delay_Sync);
                    EnterTestMode();

                    RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
                    DisplayOperateMes(string.Format("0x80 = 0x{0}; 0x81 = 0x{1}; 0x82 = 0x{2}; 0x83 = 0x{3}",
                        MultiSiteReg0[idut].ToString("X2"), MultiSiteReg1[idut].ToString("X2"), MultiSiteReg2[idut].ToString("X2"), MultiSiteReg3[idut].ToString("X2")));
                    Delay(Delay_Sync);
                    RegisterWrite(4, new uint[8] { 0x84, MultiSiteReg4[idut], 0x85, MultiSiteReg5[idut], 0x86, MultiSiteReg6[idut], 0x87, MultiSiteReg7[idut] });
                    Delay(Delay_Sync);
                    //BurstRead(0x80, 5, tempReadback);
                    /* Get vout @ IP */
                    EnterNomalMode();

                    #region /* Change Current to IP  */
                    if (ProgramMode == 0)
                    {
                        //if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, Convert.ToUInt32(IP)))
                        if (!oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                        {
                            DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                            TrimFinish();
                            return;
                        }
                    }
                    else if (ProgramMode == 1)
                    {
                        dr = MessageBox.Show(String.Format("请将电流升至{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                        if (dr == DialogResult.Cancel)
                        {
                            DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                            PowerOff();
                            RestoreRegValue();
                            return;
                        }
                    }
                    else if (ProgramMode == 2)
                    {
                        MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                        Delay(Delay_Sync);
                        MultiSiteSocketSelect(0);       //set epio1 = high; epio3 = low
                    }
                    #endregion

                    //oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);
                    Delay(Delay_Fuse);
                    dMultiSiteVoutIP[idut] = AverageVout();
                    sDUT.dVoutIPNative = dMultiSiteVoutIP[idut];
                    DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));
                }
                else
                {
                    DisplayOperateMes("灵敏度不够！！！");
                    TrimFinish();
                    return;
                }
            }

            #endregion  Get Vout@0A

            #region No need Trim case
            if ((TargetOffset - 0.001) <= dMultiSiteVout0A[idut] && dMultiSiteVout0A[idut] <= (TargetOffset + 0.001)
                && (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= (TargetVoltage_customer + 0.001)
                && (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= (TargetVoltage_customer - 0.001))
            {
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_EXT);
                Delay(Delay_Sync);
                RePower();
                //Delay(Delay_Sync);
                EnterTestMode();

                RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
                Delay(Delay_Sync);
                RegisterWrite(4, new uint[8] { 0x84, MultiSiteReg4[idut], 0x85, MultiSiteReg5[idut], 0x86, MultiSiteReg6[idut], 0x87, MultiSiteReg7[idut] });
                Delay(Delay_Sync);


                BurstRead(0x80, 5, tempReadback);
                /* fuse */
                FuseClockOn(DeviceAddress, (double)num_UD_pulsewidth_ow_EngT.Value, (double)numUD_pulsedurationtime_ow_EngT.Value);
                DisplayOperateMes("Processing...");
                //Delay(Delay_Fuse);             

                //capture Vout
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_5V);
                RePower();
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);
                Delay(Delay_Sync);
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);

                Delay(Delay_Sync);
                dMultiSiteVout0A[idut] = AverageVout();
                sDUT.dVout0ATrimmed = dMultiSiteVout0A[idut];
                DisplayOperateMes("Vout" + " @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

                /* Change Current to IP  */
                if (ProgramMode == 0)
                {
                    //if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, Convert.ToUInt32(IP)))
                    if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                        DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", IP));
                    else
                    {
                        DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        TrimFinish();
                        return;
                    }
                }
                else if(ProgramMode == 1)
                {
                    dr = MessageBox.Show(String.Format("请将电流升至{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                    {
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        PowerOff();
                        RestoreRegValue();
                        return;
                    }
                }
                else if (ProgramMode == 2)
                {
                    MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                    Delay(Delay_Sync);
                    MultiSiteSocketSelect(0);       //set epio1 = high; epio3 = low
                }

                Delay(Delay_Fuse);
                dMultiSiteVoutIP[idut] = AverageVout();
                sDUT.dVoutIPTrimmed = dMultiSiteVoutIP[idut];
                DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));
                //oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_5V);

                DisplayOperateMes("DUT" + idut.ToString() + "Pass! Bin Normal");
                //uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_NORMAL;
                uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_1;
                sDUT.iErrorCode = uDutTrimResult[idut];
                this.txt_Status_AutoTab.ForeColor = Color.Green;
                this.txt_Status_AutoTab.Text = "PASS!";
                DisplayOperateMes("Bin" + " = " + uDutTrimResult[idut].ToString());
                DisplayOperateMes("Marginal Read ->" + bMarginal.ToString());
                DisplayOperateMes("Safety REad ->" + bSafety.ToString());
                MultiSiteDisplayResult(uDutTrimResult);
                TrimFinish();
                PrintDutAttribute(sDUT);

                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_WRITE_BIN_ONE); //EPIO10
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_WRITE_EOT); //EPIO10

                return;

            }


            #endregion No need Trim case

            #region Adapting algorithm

            tempG1 = sl620CoarseGainTable[0][MultiSiteRoughGainCodeIndex[idut]] / 100d;
            tempG2 = (TargetGain_customer / ((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) / IP)) / 1000d;

            autoAdaptingGoughGain = tempG1 * tempG2 * 100d;
            DisplayOperateMes("IdealGoughGain = " + autoAdaptingGoughGain.ToString("F4"));

            Ix_forAutoAdaptingRoughGain = LookupCoarseGain_SL620(autoAdaptingGoughGain, sl620CoarseGainTable);
            autoAdaptingPresionGain = 100d * autoAdaptingGoughGain / sl620CoarseGainTable[0][Ix_forAutoAdaptingRoughGain];
            Ix_forAutoAdaptingPresionGain = LookupFineGain_SL620(autoAdaptingPresionGain, sl620FineGainTable);
            if (bAutoTrimTest)
            {
                DisplayOperateMes("IP = " + IP.ToString("F0"));
                DisplayOperateMes("TargetGain_customer" + idut.ToString() + " = " + TargetGain_customer.ToString("F4"));
                DisplayOperateMes("(dMultiSiteVoutIP - dMultiSiteVout0A)/IP = " + ((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) / IP).ToString("F4"));
                DisplayOperateMes("Pre-set gain" + idut.ToString() + " = " + tempG1.ToString("F4"));
                DisplayOperateMes("Target gain / Test gain" + idut.ToString() + " = " + tempG2.ToString("F4"));
                DisplayOperateMes("Ix_forAutoAdaptingRoughGain" + idut.ToString() + " = " + Ix_forAutoAdaptingRoughGain.ToString("F0"));
                DisplayOperateMes("Ix_forAutoAdaptingPresionGain" + idut.ToString() + " = " + Ix_forAutoAdaptingPresionGain.ToString("F0"));
                DisplayOperateMes("autoAdaptingGoughGain" + idut.ToString() + " = " + sl620CoarseGainTable[0][Ix_forAutoAdaptingRoughGain].ToString("F4"));
                DisplayOperateMes("autoAdaptingPresionGain" + idut.ToString() + " = " + sl620FineGainTable[0][Ix_forAutoAdaptingPresionGain].ToString("F4"));
            }

            /* Rough Gain Code*/
            bit_op_mask = bit4_Mask | bit5_Mask | bit6_Mask | bit7_Mask;
            MultiSiteReg1[idut] &= ~bit_op_mask;
            MultiSiteReg1[idut] |= Convert.ToUInt32(sl620CoarseGainTable[1][Ix_forAutoAdaptingRoughGain]);


            if (bAutoTrimTest)
            {
                //DisplayOperateMes("Rough Gain RegValue80 = 0x" + MultiSiteReg0[idut].ToString("X2"));
                DisplayOperateMes("Rough Gain RegValue81 = 0x" + MultiSiteReg1[idut].ToString("X2"));
            }

            /* Fine Gain Code*/
            bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
            MultiSiteReg6[idut] &= ~bit_op_mask;
            MultiSiteReg6[idut] |= Convert.ToUInt32(sl620FineGainTable[1][Ix_forAutoAdaptingPresionGain]);

            bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
            MultiSiteReg7[idut] &= ~bit_op_mask;
            MultiSiteReg7[idut] |= Convert.ToUInt32(sl620FineGainTable[2][Ix_forAutoAdaptingPresionGain]);

            if (bAutoTrimTest)
            {
                DisplayOperateMes("Precesion Gain RegValue86 = 0x" + MultiSiteReg6[idut].ToString("X2"));
                DisplayOperateMes("Precesion Gain RegValue87 = 0x" + MultiSiteReg7[idut].ToString("X2"));
                DisplayOperateMes("***new add approach***");
            }

            RePower();
            EnterTestMode();

            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            Delay(Delay_Sync);
            RegisterWrite(4, new uint[8] { 0x84, MultiSiteReg4[idut], 0x85, MultiSiteReg5[idut], 0x86, MultiSiteReg6[idut], 0x87, MultiSiteReg7[idut] });
            Delay(Delay_Sync);

            //RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            EnterNomalMode();

            /* Change Current to IP  */
            if (ProgramMode == 0)
            {
                if ( !oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                {
                    DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    TrimFinish();
                    return;
                }
            }
            else if(ProgramMode == 1)
            {
                dr = MessageBox.Show(String.Format("请将电流升至{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    PowerOff();
                    RestoreRegValue();
                    return;
                }
            }
            else if (ProgramMode == 2)
            {
                MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                Delay(Delay_Sync);
                MultiSiteSocketSelect(0);       //set epio1 = high; epio3 = low
            }

            Delay(Delay_Fuse);
            dMultiSiteVoutIP[idut] = AverageVout();
            DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));

            /* Change Current to 0A */
            if (ProgramMode == 0)
            {
                if ( !oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0u))
                {
                    DisplayOperateMes(string.Format("Set Current to {0}A failed!", 0u));
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    TrimFinish();
                    return;
                }
            }
            else if(ProgramMode == 1)
            {
                dr = MessageBox.Show(String.Format("请将电流降至0A"), "Change Current", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    PowerOff();
                    RestoreRegValue();
                    return;
                }
            }
            else if (ProgramMode == 2)
            {
                MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                Delay(Delay_Sync);
                MultiSiteSocketSelect(9);       //set epio1 = low; epio3 = high
            }

            /*  power on */
            Delay(Delay_Fuse);
            //this.txt_Status_AutoTab.Text = "Processing!";
            dMultiSiteVout0A[idut] = AverageVout();
            DisplayOperateMes("DUT" + " Vout @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

            if ( ((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) * 1000 - TargetGain_customer * IP) > 4
              && ((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) * 1000 - TargetGain_customer * IP) <= 8)
            {
                if (Ix_forAutoAdaptingPresionGain < 63)
                    Ix_forAutoAdaptingPresionGain++;
                /* Presion Gain Code*/
                bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
                MultiSiteReg6[idut] &= ~bit_op_mask;
                MultiSiteReg6[idut] |= Convert.ToUInt32(sl620FineGainTable[1][Ix_forAutoAdaptingPresionGain]);

                bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
                MultiSiteReg7[idut] &= ~bit_op_mask;
                MultiSiteReg7[idut] |= Convert.ToUInt32(sl620FineGainTable[2][Ix_forAutoAdaptingPresionGain]);
            }
            else if (((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) * 1000 - TargetGain_customer * IP) > 8
                  && ((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) * 1000 - TargetGain_customer * IP) <= 12)
            {
                if (Ix_forAutoAdaptingPresionGain < 62)
                    Ix_forAutoAdaptingPresionGain += 2;
                else if (Ix_forAutoAdaptingPresionGain == 62)
                    Ix_forAutoAdaptingPresionGain++;
                /* Presion Gain Code*/
                bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
                MultiSiteReg6[idut] &= ~bit_op_mask;
                MultiSiteReg6[idut] |= Convert.ToUInt32(sl620FineGainTable[1][Ix_forAutoAdaptingPresionGain]);

                bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
                MultiSiteReg7[idut] &= ~bit_op_mask;
                MultiSiteReg7[idut] |= Convert.ToUInt32(sl620FineGainTable[2][Ix_forAutoAdaptingPresionGain]);
            }
            else if (((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) * 1000 - TargetGain_customer * IP) < -2 &&
                     ((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) * 1000 - TargetGain_customer * IP) >= -6 )
            {
                if (Ix_forAutoAdaptingPresionGain >= 1)
                    Ix_forAutoAdaptingPresionGain--;
                /* Presion Gain Code*/
                bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
                MultiSiteReg6[idut] &= ~bit_op_mask;
                MultiSiteReg6[idut] |= Convert.ToUInt32(sl620FineGainTable[1][Ix_forAutoAdaptingPresionGain]);

                bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
                MultiSiteReg7[idut] &= ~bit_op_mask;
                MultiSiteReg7[idut] |= Convert.ToUInt32(sl620FineGainTable[2][Ix_forAutoAdaptingPresionGain]);
            }
            else if (((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) * 1000 - TargetGain_customer * IP) < -6 &&
                 ((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) * 1000 - TargetGain_customer * IP) >= -10)
            {
                if (Ix_forAutoAdaptingPresionGain >= 2)
                    Ix_forAutoAdaptingPresionGain -= 2;
                else if (Ix_forAutoAdaptingPresionGain == 1)
                    Ix_forAutoAdaptingPresionGain--;
                /* Presion Gain Code*/
                bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
                MultiSiteReg6[idut] &= ~bit_op_mask;
                MultiSiteReg6[idut] |= Convert.ToUInt32(sl620FineGainTable[1][Ix_forAutoAdaptingPresionGain]);

                bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
                MultiSiteReg7[idut] &= ~bit_op_mask;
                MultiSiteReg7[idut] |= Convert.ToUInt32(sl620FineGainTable[2][Ix_forAutoAdaptingPresionGain]);
            }
            else if (((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) * 1000 - TargetGain_customer * IP) < -10 &&
                 ((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) * 1000 - TargetGain_customer * IP) > 12)
            {
                DisplayOperateMes("###Caution### preset is too low", Color.DarkRed);
                return;
            }


            if (bAutoTrimTest)
                DisplayOperateMes("***new approach end***");

            //RePower();
            //EnterTestMode();
            //RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            //EnterNomalMode();
            //Delay(Delay_Fuse);
            //dMultiSiteVout0A[idut] = AverageVout();
            dVout_0A_Temp = dMultiSiteVout0A[idut];
            if (bAutoTrimTest)
                DisplayOperateMes("DUT" + " Vout @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

            //V0A is abnormal
            //if (Math.Abs(dVout_0A_Temp - sDUT.dVout0ANative) > 0.010)
            //{
            //    dr = MessageBox.Show(String.Format("Vout @ 0A is abnormal"), "Warning!", MessageBoxButtons.OKCancel);
            //    if (dr == DialogResult.Cancel)
            //    {
            //        DisplayOperateMes("V0A abnormal, Rebuild rough gain and precision gain code!");
            //        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
            //        PowerOff();
            //        RestoreReg80ToReg83Value();
            //        return;
            //    }
            //}

            /* Offset trim code calculate */
            Vout_0A = dMultiSiteVout0A[idut];

            if (TargetOffset == 0.5)
            {
                if (Vout_0A < 0.455 || Vout_0A > 0.555)
                {
                    DisplayOperateMes("###Caution### Offset NOT trimmable!", Color.DarkRed);

                    oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_WRITE_BIN_RECYCLE); //EPIO10
                    oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_WRITE_EOT); //EPIO10

                    return;
                }
            }

            //btn_offset_Click(null, null);
            //uint[] regTMultiSite = new uint[3];

            //MultiSiteOffsetAlg(regTMultiSite);
            //MultiSiteReg1[idut] |= regTMultiSite[0];
            //MultiSiteReg2[idut] |= regTMultiSite[1];
            //MultiSiteReg3[idut] |= regTMultiSite[2];

            bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask | bit3_Mask | bit4_Mask;
            uint ix_CoarseOffsetCode = 0;
            if (Vout_0A > TargetOffset)
            {
                ix_CoarseOffsetCode = Convert.ToUInt32(Math.Round(1000d * (1.0d - TargetOffset / Vout_0A) / 5));
                if (ix_CoarseOffsetCode > 15)
                    ix_CoarseOffsetCode = 15;
            }
            else if (Vout_0A < TargetOffset)
            {
                ix_CoarseOffsetCode = 32 - Convert.ToUInt32(Math.Round(1000d * (TargetOffset / Vout_0A - 1.0d) / 5));
                if (ix_CoarseOffsetCode == 32)
                    ix_CoarseOffsetCode = 0;
                else if (ix_CoarseOffsetCode < 16)
                    ix_CoarseOffsetCode = 16;
            }

            MultiSiteReg6[idut] &= ~bit_op_mask;
            MultiSiteReg6[idut] |= ix_CoarseOffsetCode;

            DisplayOperateMes("Vout_0A = " + Vout_0A.ToString("F3"));
            DisplayOperateMes("ix_CoarseOffsetCode = " + ix_CoarseOffsetCode.ToString());

            RePower();
            EnterTestMode();

            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            Delay(Delay_Sync);
            RegisterWrite(4, new uint[8] { 0x84, MultiSiteReg4[idut], 0x85, MultiSiteReg5[idut], 0x86, MultiSiteReg6[idut], 0x87, MultiSiteReg7[idut] });
            Delay(Delay_Sync);

            //normal mode
            EnterNomalMode();
            Delay(Delay_Fuse);
            dMultiSiteVout0A[idut] = AverageVout();
            Vout_0A = dMultiSiteVout0A[idut];

            //***************************************************
            bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask | bit3_Mask | bit4_Mask;
            uint ix_FineOffsetCode = 0;

            //if (Vout_0A > TargetOffset)
            //    ix_FineOffsetCode = Convert.ToUInt32(Math.Round(1000d * ( 1.0d - TargetOffset / Vout_0A) / 1.5));
            //else if (Vout_0A < TargetOffset)
            //    ix_FineOffsetCode = 31 - Convert.ToUInt32(Math.Round(1000d * (TargetOffset / Vout_0A - 1.0d) / 1.5));

            //MultiSiteReg7[idut] &= ~bit_op_mask;
            //MultiSiteReg7[idut] |= ix_FineOffsetCode;

            if (Vout_0A > TargetOffset)
                ix_FineOffsetCode = Convert.ToUInt32(Math.Round(1000d * (1.0d - TargetOffset / Vout_0A) / 1.5));
            else if (Vout_0A < TargetOffset)
                ix_FineOffsetCode = 31 - Convert.ToUInt32(Math.Round(1000d * (TargetOffset / Vout_0A - 1.0d) / 1.5));

            MultiSiteReg7[idut] &= ~bit_op_mask;
            MultiSiteReg7[idut] |= ix_FineOffsetCode;
            //***************************************************

            DisplayOperateMes("Vout_0A = " + Vout_0A.ToString("F3"));
            DisplayOperateMes("ix_FineOffsetCode = " + ix_FineOffsetCode.ToString());
            DisplayOperateMes("\r\nProcessing...");

            //*******************Additinal test case ***********************
            RePower();
            EnterTestMode();

            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            Delay(Delay_Sync);
            RegisterWrite(4, new uint[8] { 0x84, MultiSiteReg4[idut], 0x85, MultiSiteReg5[idut], 0x86, MultiSiteReg6[idut], 0x87, MultiSiteReg7[idut] });
            Delay(Delay_Sync);

            EnterNomalMode();

            Delay(Delay_Power);
            dMultiSiteVout0A[idut] = AverageVout();
            DisplayOperateMes("DUT" + " Vout @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

            //gainTest = (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) * 1000d / IP;
            if (dMultiSiteVout0A[idut] - TargetOffset >= 0.004)
            {
                //ix_FineOffsetCode += Convert.ToUInt32(Math.Round((dMultiSiteVout0A[idut] - TargetOffset) / 0.004));
                //DisplayOperateMes("ix_FineOffsetCode = " + ix_FineOffsetCode.ToString());

                if (ix_FineOffsetCode == 15)
                    ix_FineOffsetCode = 15;
                else if (ix_FineOffsetCode == 31)
                    ix_FineOffsetCode = 0;
                else
                    ix_FineOffsetCode++;

                DisplayOperateMes("ix_FineOffsetCode = " + ix_FineOffsetCode.ToString());

                /* Fine offset Code*/
                bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask | bit3_Mask | bit4_Mask;
                MultiSiteReg7[idut] &= ~bit_op_mask;
                MultiSiteReg7[idut] |= ix_FineOffsetCode;
            }
            else if (TargetOffset - dMultiSiteVout0A[idut] >= 0.004)
            {
                if (ix_FineOffsetCode == 0)
                    ix_FineOffsetCode = 31;
                else if (ix_FineOffsetCode == 16)
                    ix_FineOffsetCode = 16;
                else
                    ix_FineOffsetCode--;

                DisplayOperateMes("ix_FineOffsetCode = " + ix_FineOffsetCode.ToString());

                /* Fine offset Code*/
                bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask | bit3_Mask | bit4_Mask;
                MultiSiteReg7[idut] &= ~bit_op_mask;
                MultiSiteReg7[idut] |= ix_FineOffsetCode;
            }
            //********************************************************

            DisplayOperateMes("Processing...");

            #endregion Adapting algorithm

            #region Fuse
            //Fuse
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_EXT);
            RePower();
            EnterTestMode();

            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            Delay(Delay_Sync);
            RegisterWrite(4, new uint[8] { 0x84, MultiSiteReg4[idut], 0x85, MultiSiteReg5[idut], 0x86, MultiSiteReg6[idut], 0x87, MultiSiteReg7[idut] });
            Delay(Delay_Sync);
            RegisterWrite(1, new uint[2] { 0x88, 0x02 });

            if (this.cb_BypFuse_AutoTab.Checked)
            {
                printRegValue();
                TrimFinish();
                return;
            }

            FuseClockOn(DeviceAddress, (double)num_UD_pulsewidth_ow_EngT.Value, (double)numUD_pulsedurationtime_ow_EngT.Value);
            DisplayOperateMes("Trimming...");

            #endregion

            #region Bin
            /* Repower on 5V */
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);
            Delay(Delay_Sync);
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_5V);
            RePower();
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);
            Delay(Delay_Fuse);

            dMultiSiteVout0A[idut] = AverageVout();
            sDUT.dVout0ATrimmed = dMultiSiteVout0A[idut];
            DisplayOperateMes("Vout" + " @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

            if (this.cb_AutoTab_Retest.SelectedIndex == 0)
            {
                /* Change Current to IP  */
                if (ProgramMode == 0)
                {
                    if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                        DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", IP));
                    else
                    {
                        DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        TrimFinish();
                        return;
                    }
                }
                else if(ProgramMode == 1)
                {
                    dr = MessageBox.Show(String.Format("请将电流升至{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                    {
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        PowerOff();
                        RestoreRegValue();
                        return;
                    }
                }
                else if (ProgramMode == 2)
                {
                    MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                    Delay(Delay_Sync);
                    MultiSiteSocketSelect(0);       //set epio1 = high; epio3 = low
                }

                Delay(Delay_Fuse);
                dMultiSiteVoutIP[idut] = AverageVout();
                sDUT.dVoutIPTrimmed = dMultiSiteVoutIP[idut];
                DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));

                /* bin1,2,3 */
                if (TargetOffset * (1 - 0.002) <= dMultiSiteVout0A[idut] && dMultiSiteVout0A[idut] <= TargetOffset * (1 + 0.002) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= TargetVoltage_customer * (1 + 0.002) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= TargetVoltage_customer * (1 - 0.002))
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_1;
                    this.txt_Status_AutoTab.ForeColor = Color.Green;
                    this.txt_Status_AutoTab.Text = "PASS!";

                    oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_WRITE_BIN_ONE); //EPIO10
                    oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_WRITE_EOT); //EPIO10
                }
                else if (TargetOffset * (1 - bin2accuracy / 100d) <= dMultiSiteVout0A[idut] &&
                    dMultiSiteVout0A[idut] <= TargetOffset * (1 + bin2accuracy / 100d) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= TargetVoltage_customer * (1 + bin3accuracy / 100d) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= TargetVoltage_customer * (1 - bin3accuracy / 100d))
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_2;
                    this.txt_Status_AutoTab.ForeColor = Color.Green;
                    this.txt_Status_AutoTab.Text = "PASS!";

                    oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_WRITE_BIN_TWO); //EPIO10
                    oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_WRITE_EOT); //EPIO10
                }
                else if (TargetOffset * (1 - bin2accuracy / 100d) <= dMultiSiteVout0A[idut] &&
                    dMultiSiteVout0A[idut] <= TargetOffset * (1 + bin2accuracy / 100d) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= TargetVoltage_customer * (1 + bin3accuracy / 100d) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= TargetVoltage_customer * (1 - bin3accuracy / 100d))
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_3;
                    this.txt_Status_AutoTab.ForeColor = Color.Green;
                    this.txt_Status_AutoTab.Text = "PASS!";

                    //oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_WRITE_BIN_ONE); //EPIO10
                    //oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_WRITE_EOT); //EPIO10
                }
                else
                {
                    this.txt_Status_AutoTab.ForeColor = Color.Red;
                    this.txt_Status_AutoTab.Text = "FAIL!";

                    oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_WRITE_BIN_FAIL); //EPIO10
                    oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_WRITE_EOT); //EPIO10
                }
            }
            else
            {
                /* bin1,2,3 */
                if (TargetOffset * (1 - bin2accuracy / 100d) <= dMultiSiteVout0A[idut] && dMultiSiteVout0A[idut] <= TargetOffset * (1 + bin2accuracy / 100d))
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_1;
                    this.txt_Status_AutoTab.ForeColor = Color.Green;
                    this.txt_Status_AutoTab.Text = "PASS!";

                    oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_WRITE_BIN_ONE); //EPIO10
                    oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_WRITE_EOT); //EPIO10
                }
                else
                {
                    this.txt_Status_AutoTab.ForeColor = Color.Red;
                    this.txt_Status_AutoTab.Text = "FAIL!";

                    oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_WRITE_BIN_FAIL); //EPIO10
                    oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_WRITE_EOT); //EPIO10
                }
            }

            #endregion Bin

            #region Display Result and Reset parameters
            DisplayOperateMes("Bin" + " = " + uDutTrimResult[idut].ToString());
            MultiSiteDisplayResult(uDutTrimResult);
            TrimFinish();
            sDUT.iErrorCode = uDutTrimResult[idut];
            PrintDutAttribute(sDUT);
            DisplayOperateMes("Next...");
            #endregion Display Result and Reset parameters
        }

        private void AutoTrim_SL620_DiffMode()
        {
            #region Define Parameters
            DialogResult dr;
            bool bMarginal = false;
            bool bSafety = false;
            //uint[] tempReadback = new uint[5];
            double dVout_0A_Temp = 0;
            //TargetOffset = 2.5;
            double dVip_Target = TargetOffset + TargetVoltage_customer;
            //double dGainTestMinusTarget = 1;
            //double dGainTest = 0;
            ModuleAttribute sDUT;
            sDUT.dIQ = 0;
            sDUT.dVoutIPNative = 0;
            sDUT.dVout0ANative = 0;
            sDUT.dVoutIPMiddle = 0;
            sDUT.dVout0AMiddle = 0;
            sDUT.dVoutIPTrimmed = 0;
            sDUT.dVout0ATrimmed = 0;
            sDUT.iErrorCode = 00;
            sDUT.bDigitalCommFail = false;
            sDUT.bNormalModeFail = false;
            sDUT.bReadMarginal = false;
            sDUT.bReadSafety = false;
            sDUT.bTrimmed = false;

            // PARAMETERS DEFINE FOR MULTISITE
            uint idut = 0;
            uint uDutCount = 16;
            //bool bValidRound = false;
            //bool bSecondCurrentOn = false;
            double dModuleCurrent = 0;
            bool[] bGainBoost = new bool[16];
            bool[] bDutValid = new bool[16];
            bool[] bDutNoNeedTrim = new bool[16];
            uint[] uDutTrimResult = new uint[16];
            double[] dMultiSiteVoutIP = new double[16];
            double[] dMultiSiteVout0A = new double[16];

            /* autoAdaptingGoughGain algorithm*/
            double autoAdaptingGoughGain = 0;
            double autoAdaptingPresionGain = 0;
            double tempG1 = 0;
            double tempG2 = 0;
            //double dGainPreset = 0;
            int Ix_forAutoAdaptingRoughGain = 0;
            int Ix_forAutoAdaptingPresionGain = 0;

            DisplayOperateMes("\r\n************" + DateTime.Now.ToString() + "************");
            DisplayOperateMes("Start...");
            this.txt_Status_AutoTab.ForeColor = Color.Black;
            this.txt_Status_AutoTab.Text = "START!";

            for (uint i = 0; i < uDutCount; i++)
            {
                dMultiSiteVoutIP[i] = 0d;
                dMultiSiteVout0A[i] = 0d;

                MultiSiteReg0[i] = Reg80Value;
                MultiSiteReg1[i] = Reg81Value;
                MultiSiteReg2[i] = Reg82Value;
                MultiSiteReg3[i] = Reg83Value;
                MultiSiteReg4[i] = Reg84Value;
                MultiSiteReg5[i] = Reg85Value;
                MultiSiteReg6[i] = Reg86Value;
                MultiSiteReg7[i] = Reg87Value;

                MultiSiteRoughGainCodeIndex[i] = preSetCoareseGainCode;

                uDutTrimResult[i] = 0u;
                bDutNoNeedTrim[i] = false;
                bDutValid[i] = false;
                bGainBoost[i] = false;
            }
            #endregion Define Parameters

            #region Get module current
            //clear log
            //DisplayOperateMesClear();
            /*  power on */
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_5V);
            RePower();
            Delay(Delay_Sync);
            this.txt_Status_AutoTab.Text = "Trimming!";

            if (!this.cb_MeasureiQ_AutoTab.Checked)
            {
                /* Get module current */
                if (!oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VCS))
                {
                    DisplayOperateMes("Set ADC VIN to VCS failed", Color.Red);
                    PowerOff();
                    return;
                }
                Delay(Delay_Sync);
                if (oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_SET_CURRENT_SENCE))


                    this.txt_ModuleCurrent_EngT.Text = GetModuleCurrent().ToString("F1");
                this.txt_ModuleCurrent_PreT.Text = this.txt_ModuleCurrent_EngT.Text;


                dModuleCurrent = GetModuleCurrent();
                sDUT.dIQ = dModuleCurrent;
                //if (dCurrentDownLimit > dModuleCurrent)
                //{
                //    DisplayOperateMes("Module " + " current is " + dModuleCurrent.ToString("F3"), Color.Red);
                //    PowerOff();
                //    MessageBox.Show(String.Format("电流偏低，检查模组是否连接！"), "Warning", MessageBoxButtons.OK);
                //    return;
                //}
                //else if (dModuleCurrent > dCurrentUpLimit)
                //{
                //    DisplayOperateMes("Module " + " current is " + dModuleCurrent.ToString("F3"), Color.Red);
                //    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_CURRENT_HIGH;
                //    PowerOff();
                //    sDUT.iErrorCode = uDutTrimResult[idut];
                //    PrintDutAttribute(sDUT);
                //    this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                //    this.txt_Status_AutoTab.Text = "短路!";
                //    return;
                //}
                //else
                    DisplayOperateMes("Module " + " current is " + dModuleCurrent.ToString("F3"));
            }

            #endregion Get module current

            #region Saturation judgement
            //Redundency delay in case of power off failure.
            Delay(Delay_Sync);
            EnterTestMode();
            //Delay(Delay_Sync);
            BurstRead(0x80, 5, tempReadback);
            if (tempReadback[0] + tempReadback[1] + tempReadback[2] + tempReadback[3] + tempReadback[4] != 0)
            {
                DisplayOperateMes("DUT" + " has some bits Blown!", Color.Red);
                uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_TRIMMED_SOMEBITS;
                TrimFinish();
                sDUT.bTrimmed = false;
                sDUT.iErrorCode = uDutTrimResult[idut];
                PrintDutAttribute(sDUT);
                this.txt_Status_AutoTab.ForeColor = Color.Red;
                this.txt_Status_AutoTab.Text = "FAIL!";
                return;
            }



            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            DisplayOperateMes(string.Format("0x80 = 0x{0};\r\n0x81 = 0x{1};\r\n0x82 = 0x{2};\r\n0x83 = 0x{3}",
                MultiSiteReg0[idut].ToString("X2"), MultiSiteReg1[idut].ToString("X2"), MultiSiteReg2[idut].ToString("X2"), MultiSiteReg3[idut].ToString("X2")));
            Delay(Delay_Sync);
            RegisterWrite(4, new uint[8] { 0x84, MultiSiteReg4[idut], 0x85, MultiSiteReg5[idut], 0x86, MultiSiteReg6[idut], 0x87, MultiSiteReg7[idut] });
            Delay(Delay_Sync);
            BurstRead(0x80, 5, tempReadback);
            if (tempReadback[0] != MultiSiteReg0[idut] || tempReadback[1] != MultiSiteReg1[idut]
                || tempReadback[2] != MultiSiteReg2[idut] || tempReadback[3] != MultiSiteReg3[idut])
            {
                if (tempReadback[0] + tempReadback[1] + tempReadback[2] + tempReadback[3] + tempReadback[4] == 0)
                {
                    RePower();
                    Delay(Delay_Sync);
                    oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);
                    Delay(Delay_Sync);
                    oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);
                    Delay(Delay_Fuse);
                    dMultiSiteVout0A[idut] = AverageVout();
                    if (dMultiSiteVout0A[idut] < 4.5 && dMultiSiteVout0A[idut] > 1.5)
                    {
                        DisplayOperateMes("DUT Trimmed!", Color.Red);
                        uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_TRIMMRD_ALREADY;
                        TrimFinish();
                        sDUT.bDigitalCommFail = true;
                        sDUT.bTrimmed = true;
                        sDUT.iErrorCode = uDutTrimResult[idut];
                        PrintDutAttribute(sDUT);
                        //MessageBox.Show(String.Format("模组已编程，交至研发部！"), "Error", MessageBoxButtons.OK);
                        this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                        this.txt_Status_AutoTab.Text = "混料!";
                        return;
                    }
                    else
                    {
                        DisplayOperateMes("VOUT Short!", Color.Red);
                        uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_VOUT_SHORT;
                        TrimFinish();
                        sDUT.bDigitalCommFail = true;
                        sDUT.iErrorCode = uDutTrimResult[idut];
                        PrintDutAttribute(sDUT);
                        //MessageBox.Show(String.Format("输出管脚短路！", Color.YellowGreen), "Warning", MessageBoxButtons.OK);
                        this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                        this.txt_Status_AutoTab.Text = "短路!";
                        return;
                    }
                }
                else
                {
                    DisplayOperateMes("DUT digital communication fail!", Color.Red);
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_COMM_FAIL;
                    TrimFinish();
                    sDUT.bDigitalCommFail = true;
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    //MessageBox.Show(String.Format("输出管脚短路！", Color.YellowGreen), "Warning", MessageBoxButtons.OK);
                    this.txt_Status_AutoTab.ForeColor = Color.Red;
                    this.txt_Status_AutoTab.Text = "FAIL!";
                    return;
                }
            }
            /* Get vout @ IP */
            EnterNomalMode();

            #region /* Change Current to IP  */
            if (ProgramMode == 0)
            {
                //if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, Convert.ToUInt32(IP)))
                if (!oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                {
                    DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                    TrimFinish();
                    return;
                }
            }
            else if (ProgramMode == 1)
            {
                dr = MessageBox.Show(String.Format("请将电流升至{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    PowerOff();
                    RestoreRegValue();
                    return;
                }
            }
            else if (ProgramMode == 2)
            {
                MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                Delay(Delay_Sync);
                MultiSiteSocketSelect(0);       //set epio1 = high; epio3 = low
            }
            #endregion

            Delay(Delay_Fuse);
            dMultiSiteVoutIP[idut] = AverageVout();
            sDUT.dVoutIPNative = dMultiSiteVoutIP[idut];
            DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));

           
            if (dMultiSiteVoutIP[idut] < minimumVoutIP)
            {
                DisplayOperateMes("Module" + " Vout is too Low!", Color.Red);
                uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_VOUT_LOW;
                TrimFinish();
                sDUT.iErrorCode = uDutTrimResult[idut];
                PrintDutAttribute(sDUT);
                this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                this.txt_Status_AutoTab.Text = "短路!";
                return;
            }

            #endregion Saturation judgement

            #region Get Vout@0A

            #region /* Change Current to 0A */
            if (ProgramMode == 0)
            {
                //if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, 0u))
                if (!oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0u))
                {
                    DisplayOperateMes(string.Format("Set Current to {0}A failed!", 0u));
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    TrimFinish();
                    return;
                }
            }
            else if (ProgramMode == 1)
            {
                dr = MessageBox.Show(String.Format("请将IP降至0A!"), "Try Again", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    PowerOff();
                    RestoreRegValue();
                    return;
                }
            }
            else if (ProgramMode == 2)
            {
                MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                Delay(Delay_Sync);
                MultiSiteSocketSelect(9);       //set epio1 = low; epio3 = high
            }
            #endregion

            Delay(Delay_Fuse);

            //###########################################################################
            //this.TargetOffset = GetVref();
            //###########################################################################

            dMultiSiteVout0A[idut] = GetVout();

            sDUT.dVout0ANative = dMultiSiteVout0A[idut];
            DisplayOperateMes("Vout" + " @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

            if (dMultiSiteVoutIP[idut] < dMultiSiteVout0A[idut])
            {
                TrimFinish();
                //PrintDutAttribute(sDUT);
                MessageBox.Show(String.Format("请确认IP方向!"), "Try Again", MessageBoxButtons.OK);
                return;
            }
            else if (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut] < VoutIPThreshold)
            {
                TrimFinish();
                //PrintDutAttribute(sDUT);
                MessageBox.Show(String.Format("请确认电流为{0}A!!!", IP), "Try Again", MessageBoxButtons.OK);
                return;
            }

            if (TargetOffset == 2.5)
            {
                if (dMultiSiteVout0A[idut] < 2.25 || dMultiSiteVout0A[idut] > 2.8)
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_OFFSET_ABN;
                    TrimFinish();
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    this.txt_Status_AutoTab.ForeColor = Color.Red;
                    this.txt_Status_AutoTab.Text = "FAIL!";
                    return;
                }
            }
            else if (TargetOffset == 1.65)
            {
                if (dMultiSiteVout0A[idut] < 1.0 || dMultiSiteVout0A[idut] > 2.5)
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_OFFSET_ABN;
                    TrimFinish();
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    this.txt_Status_AutoTab.ForeColor = Color.Red;
                    this.txt_Status_AutoTab.Text = "FAIL!";
                    return;
                }
            }

            if ((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) * 1000 / IP < TargetGain_customer * 0.998)
            {
                DisplayOperateMes("灵敏度不够！！！");
                TrimFinish();
                return;
            }

            #endregion  Get Vout@0A

            #region No need Trim case
            if ((TargetOffset - 0.001) <= dMultiSiteVout0A[idut] && dMultiSiteVout0A[idut] <= (TargetOffset + 0.001)
                && (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= (TargetVoltage_customer + 0.001)
                && (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= (TargetVoltage_customer - 0.001))
            {
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_EXT);
                Delay(Delay_Sync);
                RePower();
                //Delay(Delay_Sync);
                EnterTestMode();

                RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
                Delay(Delay_Sync);
                RegisterWrite(4, new uint[8] { 0x84, MultiSiteReg4[idut], 0x85, MultiSiteReg5[idut], 0x86, MultiSiteReg6[idut], 0x87, MultiSiteReg7[idut] });
                Delay(Delay_Sync);


                BurstRead(0x80, 5, tempReadback);
                /* fuse */
                FuseClockOn(DeviceAddress, (double)num_UD_pulsewidth_ow_EngT.Value, (double)numUD_pulsedurationtime_ow_EngT.Value);
                DisplayOperateMes("Processing...");
                //Delay(Delay_Fuse);             

                //capture Vout
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_5V);
                RePower();
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);
                Delay(Delay_Sync);
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);

                Delay(Delay_Sync);
                dMultiSiteVout0A[idut] = AverageVout();
                sDUT.dVout0ATrimmed = dMultiSiteVout0A[idut];
                DisplayOperateMes("Vout" + " @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

                /* Change Current to IP  */
                if (ProgramMode == 0)
                {
                    //if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, Convert.ToUInt32(IP)))
                    if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                        DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", IP));
                    else
                    {
                        DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        TrimFinish();
                        return;
                    }
                }
                else if (ProgramMode == 1)
                {
                    dr = MessageBox.Show(String.Format("请将电流升至{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                    {
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        PowerOff();
                        RestoreRegValue();
                        return;
                    }
                }
                else if (ProgramMode == 2)
                {
                    MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                    Delay(Delay_Sync);
                    MultiSiteSocketSelect(0);       //set epio1 = high; epio3 = low
                }

                Delay(Delay_Fuse);
                dMultiSiteVoutIP[idut] = AverageVout();
                sDUT.dVoutIPTrimmed = dMultiSiteVoutIP[idut];
                DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));
                //oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_5V);

                DisplayOperateMes("DUT" + idut.ToString() + "Pass! Bin Normal");
                //uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_NORMAL;
                uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_1;
                sDUT.iErrorCode = uDutTrimResult[idut];
                this.txt_Status_AutoTab.ForeColor = Color.Green;
                this.txt_Status_AutoTab.Text = "PASS!";
                DisplayOperateMes("Bin" + " = " + uDutTrimResult[idut].ToString());
                DisplayOperateMes("Marginal Read ->" + bMarginal.ToString());
                DisplayOperateMes("Safety REad ->" + bSafety.ToString());
                MultiSiteDisplayResult(uDutTrimResult);
                TrimFinish();
                PrintDutAttribute(sDUT);
                return;

            }


            #endregion No need Trim case

            #region Adapting algorithm

            tempG1 = sl620CoarseGainTable[0][MultiSiteRoughGainCodeIndex[idut]] / 100d;
            tempG2 = (TargetGain_customer / ((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) / IP)) / 1000d;

            autoAdaptingGoughGain = tempG1 * tempG2 * 100d;
            DisplayOperateMes("IdealGoughGain = " + autoAdaptingGoughGain.ToString("F4"));

            Ix_forAutoAdaptingRoughGain = LookupCoarseGain_SL620(autoAdaptingGoughGain, sl620CoarseGainTable);
            autoAdaptingPresionGain = 100d * autoAdaptingGoughGain / sl620CoarseGainTable[0][Ix_forAutoAdaptingRoughGain];
            Ix_forAutoAdaptingPresionGain = LookupFineGain_SL620(autoAdaptingPresionGain, sl620FineGainTable);
            if (bAutoTrimTest)
            {
                DisplayOperateMes("IP = " + IP.ToString("F0"));
                DisplayOperateMes("TargetGain_customer" + idut.ToString() + " = " + TargetGain_customer.ToString("F4"));
                DisplayOperateMes("(dMultiSiteVoutIP - dMultiSiteVout0A)/IP = " + ((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) / IP).ToString("F4"));
                DisplayOperateMes("Pre-set gain" + idut.ToString() + " = " + tempG1.ToString("F4"));
                DisplayOperateMes("Target gain / Test gain" + idut.ToString() + " = " + tempG2.ToString("F4"));
                DisplayOperateMes("Ix_forAutoAdaptingRoughGain" + idut.ToString() + " = " + Ix_forAutoAdaptingRoughGain.ToString("F0"));
                DisplayOperateMes("Ix_forAutoAdaptingPresionGain" + idut.ToString() + " = " + Ix_forAutoAdaptingPresionGain.ToString("F0"));
                DisplayOperateMes("autoAdaptingGoughGain" + idut.ToString() + " = " + sl620CoarseGainTable[0][Ix_forAutoAdaptingRoughGain].ToString("F4"));
                DisplayOperateMes("autoAdaptingPresionGain" + idut.ToString() + " = " + sl620FineGainTable[0][Ix_forAutoAdaptingPresionGain].ToString("F4"));
            }

            /* Rough Gain Code*/
            bit_op_mask = bit4_Mask | bit5_Mask | bit6_Mask | bit7_Mask;
            MultiSiteReg1[idut] &= ~bit_op_mask;
            MultiSiteReg1[idut] |= Convert.ToUInt32(sl620CoarseGainTable[1][Ix_forAutoAdaptingRoughGain]);


            if (bAutoTrimTest)
            {
                //DisplayOperateMes("Rough Gain RegValue80 = 0x" + MultiSiteReg0[idut].ToString("X2"));
                DisplayOperateMes("Rough Gain RegValue81 = 0x" + MultiSiteReg1[idut].ToString("X2"));
            }

            /* Fine Gain Code*/
            bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
            MultiSiteReg6[idut] &= ~bit_op_mask;
            MultiSiteReg6[idut] |= Convert.ToUInt32(sl620FineGainTable[1][Ix_forAutoAdaptingPresionGain]);

            bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
            MultiSiteReg7[idut] &= ~bit_op_mask;
            MultiSiteReg7[idut] |= Convert.ToUInt32(sl620FineGainTable[2][Ix_forAutoAdaptingPresionGain]);

            if (bAutoTrimTest)
            {
                DisplayOperateMes("Precesion Gain RegValue86 = 0x" + MultiSiteReg6[idut].ToString("X2"));
                DisplayOperateMes("Precesion Gain RegValue87 = 0x" + MultiSiteReg7[idut].ToString("X2"));
                DisplayOperateMes("***new add approach***");
            }

            RePower();
            EnterTestMode();

            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            Delay(Delay_Sync);
            RegisterWrite(4, new uint[8] { 0x84, MultiSiteReg4[idut], 0x85, MultiSiteReg5[idut], 0x86, MultiSiteReg6[idut], 0x87, MultiSiteReg7[idut] });
            Delay(Delay_Sync);

            //RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            EnterNomalMode();

            /* Change Current to IP  */
            if (ProgramMode == 0)
            {
                if (!oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                {
                    DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    TrimFinish();
                    return;
                }
            }
            else if (ProgramMode == 1)
            {
                dr = MessageBox.Show(String.Format("请将电流升至{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    PowerOff();
                    RestoreRegValue();
                    return;
                }
            }
            else if (ProgramMode == 2)
            {
                MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                Delay(Delay_Sync);
                MultiSiteSocketSelect(0);       //set epio1 = high; epio3 = low
            }

            Delay(Delay_Fuse);
            dMultiSiteVoutIP[idut] = AverageVout();
            DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));

            /* Change Current to 0A */
            if (ProgramMode == 0)
            {
                if (!oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0u))
                {
                    DisplayOperateMes(string.Format("Set Current to {0}A failed!", 0u));
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    TrimFinish();
                    return;
                }
            }
            else if (ProgramMode == 1)
            {
                dr = MessageBox.Show(String.Format("请将电流降至0A"), "Change Current", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    PowerOff();
                    RestoreRegValue();
                    return;
                }
            }
            else if (ProgramMode == 2)
            {
                MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                Delay(Delay_Sync);
                MultiSiteSocketSelect(9);       //set epio1 = low; epio3 = high
            }

            /*  power on */
            Delay(Delay_Fuse);
            //this.txt_Status_AutoTab.Text = "Processing!";
            dMultiSiteVout0A[idut] = AverageVout();
            DisplayOperateMes("DUT" + " Vout @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

            if (((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) * 1000 - TargetGain_customer * IP) > 4
              && ((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) * 1000 - TargetGain_customer * IP) <= 8)
            {
                if (Ix_forAutoAdaptingPresionGain < 63)
                    Ix_forAutoAdaptingPresionGain++;
                /* Presion Gain Code*/
                bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
                MultiSiteReg6[idut] &= ~bit_op_mask;
                MultiSiteReg6[idut] |= Convert.ToUInt32(sl620FineGainTable[1][Ix_forAutoAdaptingPresionGain]);

                bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
                MultiSiteReg7[idut] &= ~bit_op_mask;
                MultiSiteReg7[idut] |= Convert.ToUInt32(sl620FineGainTable[2][Ix_forAutoAdaptingPresionGain]);
            }
            else if (((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) * 1000 - TargetGain_customer * IP) > 8
                  && ((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) * 1000 - TargetGain_customer * IP) <= 12)
            {
                if (Ix_forAutoAdaptingPresionGain < 62)
                    Ix_forAutoAdaptingPresionGain += 2;
                else if (Ix_forAutoAdaptingPresionGain == 62)
                    Ix_forAutoAdaptingPresionGain++;
                /* Presion Gain Code*/
                bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
                MultiSiteReg6[idut] &= ~bit_op_mask;
                MultiSiteReg6[idut] |= Convert.ToUInt32(sl620FineGainTable[1][Ix_forAutoAdaptingPresionGain]);

                bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
                MultiSiteReg7[idut] &= ~bit_op_mask;
                MultiSiteReg7[idut] |= Convert.ToUInt32(sl620FineGainTable[2][Ix_forAutoAdaptingPresionGain]);
            }
            else if (((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) * 1000 - TargetGain_customer * IP) < -2 &&
                     ((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) * 1000 - TargetGain_customer * IP) >= -6)
            {
                if (Ix_forAutoAdaptingPresionGain >= 1)
                    Ix_forAutoAdaptingPresionGain--;
                /* Presion Gain Code*/
                bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
                MultiSiteReg6[idut] &= ~bit_op_mask;
                MultiSiteReg6[idut] |= Convert.ToUInt32(sl620FineGainTable[1][Ix_forAutoAdaptingPresionGain]);

                bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
                MultiSiteReg7[idut] &= ~bit_op_mask;
                MultiSiteReg7[idut] |= Convert.ToUInt32(sl620FineGainTable[2][Ix_forAutoAdaptingPresionGain]);
            }
            else if (((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) * 1000 - TargetGain_customer * IP) < -6 &&
                 ((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) * 1000 - TargetGain_customer * IP) >= -10)
            {
                if (Ix_forAutoAdaptingPresionGain >= 2)
                    Ix_forAutoAdaptingPresionGain -= 2;
                else if (Ix_forAutoAdaptingPresionGain == 1)
                    Ix_forAutoAdaptingPresionGain--;
                /* Presion Gain Code*/
                bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
                MultiSiteReg6[idut] &= ~bit_op_mask;
                MultiSiteReg6[idut] |= Convert.ToUInt32(sl620FineGainTable[1][Ix_forAutoAdaptingPresionGain]);

                bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
                MultiSiteReg7[idut] &= ~bit_op_mask;
                MultiSiteReg7[idut] |= Convert.ToUInt32(sl620FineGainTable[2][Ix_forAutoAdaptingPresionGain]);
            }
            else if (((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) * 1000 - TargetGain_customer * IP) < -10 &&
                 ((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) * 1000 - TargetGain_customer * IP) > 12)
            {
                DisplayOperateMes("###Caution### preset is too low", Color.DarkRed);
                return;
            }


            if (bAutoTrimTest)
                DisplayOperateMes("***new approach end***");


            //###########################################################################
            this.TargetOffset = GetVref();
            //###########################################################################
            dMultiSiteVout0A[idut] = GetVout();

            dVout_0A_Temp = dMultiSiteVout0A[idut];
            //if (bAutoTrimTest)
            DisplayOperateMes("DUT" + " Vref = " + TargetOffset.ToString("F3"));
            DisplayOperateMes("DUT" + " Vout @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));


            /* Offset trim code calculate */
            Vout_0A = dMultiSiteVout0A[idut];

            if (TargetOffset == 0.5)
            {
                if (Vout_0A < 0.455 || Vout_0A > 0.555)
                {
                    DisplayOperateMes("###Caution### Offset NOT trimmable!", Color.DarkRed);
                    return;
                }
            }

            bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask | bit3_Mask | bit4_Mask;
            uint ix_CoarseOffsetCode = 0;
            if (Vout_0A > TargetOffset)
            {
                ix_CoarseOffsetCode = Convert.ToUInt32(Math.Round(1000d * (1.0d - TargetOffset / Vout_0A) / 5));
                if (ix_CoarseOffsetCode > 15)
                    ix_CoarseOffsetCode = 15;
            }
            else if (Vout_0A < TargetOffset)
            {
                ix_CoarseOffsetCode = 32 - Convert.ToUInt32(Math.Round(1000d * (TargetOffset / Vout_0A - 1.0d) / 5));
                if (ix_CoarseOffsetCode == 32)
                    ix_CoarseOffsetCode = 0;
                else if (ix_CoarseOffsetCode < 16)
                    ix_CoarseOffsetCode = 16;
            }

            MultiSiteReg6[idut] &= ~bit_op_mask;
            MultiSiteReg6[idut] |= ix_CoarseOffsetCode;

            DisplayOperateMes("Vout_0A = " + Vout_0A.ToString("F3"));
            DisplayOperateMes("ix_CoarseOffsetCode = " + ix_CoarseOffsetCode.ToString());

            RePower();
            EnterTestMode();

            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            Delay(Delay_Sync);
            RegisterWrite(4, new uint[8] { 0x84, MultiSiteReg4[idut], 0x85, MultiSiteReg5[idut], 0x86, MultiSiteReg6[idut], 0x87, MultiSiteReg7[idut] });
            Delay(Delay_Sync);

            //normal mode
            EnterNomalMode();
            Delay(Delay_Fuse);
            //###########################################################################
            this.TargetOffset = GetVref();
            //###########################################################################
            dMultiSiteVout0A[idut] = GetVout();
            //dMultiSiteVout0A[idut] = AverageVout();
            Vout_0A = dMultiSiteVout0A[idut];

            //***************************************************
            bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask | bit3_Mask | bit4_Mask;
            uint ix_FineOffsetCode = 0;

            if (Vout_0A > TargetOffset)
                ix_FineOffsetCode = Convert.ToUInt32(Math.Round(1000d * (1.0d - TargetOffset / Vout_0A) / 1.5));
            else if (Vout_0A < TargetOffset)
                ix_FineOffsetCode = 31 - Convert.ToUInt32(Math.Round(1000d * (TargetOffset / Vout_0A - 1.0d) / 1.5));

            MultiSiteReg7[idut] &= ~bit_op_mask;
            MultiSiteReg7[idut] |= ix_FineOffsetCode;
            //***************************************************
            DisplayOperateMes("DUT" + " Vref = " + TargetOffset.ToString("F3"));
            DisplayOperateMes("Vout_0A = " + Vout_0A.ToString("F3"));
            DisplayOperateMes("ix_FineOffsetCode = " + ix_FineOffsetCode.ToString());
            DisplayOperateMes("\r\nProcessing...");


            DisplayOperateMes("Processing...");

            #endregion Adapting algorithm

            #region Fuse
            //Fuse
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_EXT);
            RePower();
            EnterTestMode();

            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            Delay(Delay_Sync);
            RegisterWrite(4, new uint[8] { 0x84, MultiSiteReg4[idut], 0x85, MultiSiteReg5[idut], 0x86, MultiSiteReg6[idut], 0x87, MultiSiteReg7[idut] });
            Delay(Delay_Sync);
            RegisterWrite(1, new uint[2] { 0x88, 0x02 });

            if (this.cb_BypFuse_AutoTab.Checked)
            {
                printRegValue();
                TrimFinish();
                return;
            }

            FuseClockOn(DeviceAddress, (double)num_UD_pulsewidth_ow_EngT.Value, (double)numUD_pulsedurationtime_ow_EngT.Value);
            DisplayOperateMes("Trimming...");

            #endregion

            #region Bin
            /* Repower on 5V */
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);
            Delay(Delay_Sync);
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_5V);
            RePower();
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);
            Delay(Delay_Fuse);

            //###########################################################################
            this.TargetOffset = GetVref();
            //###########################################################################
            dMultiSiteVout0A[idut] = GetVout();
            sDUT.dVout0ATrimmed = dMultiSiteVout0A[idut];

            DisplayOperateMes("DUT" + " Vref = " + TargetOffset.ToString("F3"));
            DisplayOperateMes("Vout" + " @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

            if (this.cb_AutoTab_Retest.SelectedIndex == 0)
            {
                /* Change Current to IP  */
                if (ProgramMode == 0)
                {
                    if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                        DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", IP));
                    else
                    {
                        DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        TrimFinish();
                        return;
                    }
                }
                else if (ProgramMode == 1)
                {
                    dr = MessageBox.Show(String.Format("请将电流升至{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                    {
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        PowerOff();
                        RestoreRegValue();
                        return;
                    }
                }
                else if (ProgramMode == 2)
                {
                    MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                    Delay(Delay_Sync);
                    MultiSiteSocketSelect(0);       //set epio1 = high; epio3 = low
                }

                Delay(Delay_Fuse);
                dMultiSiteVoutIP[idut] = AverageVout();
                sDUT.dVoutIPTrimmed = dMultiSiteVoutIP[idut];
                DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));

                /* bin1,2,3 */
                if (TargetOffset * (1 - 0.002) <= dMultiSiteVout0A[idut] && dMultiSiteVout0A[idut] <= TargetOffset * (1 + 0.002) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= TargetVoltage_customer * (1 + 0.002) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= TargetVoltage_customer * (1 - 0.002))
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_1;
                    this.txt_Status_AutoTab.ForeColor = Color.Green;
                    this.txt_Status_AutoTab.Text = "PASS!";
                }
                else if (TargetOffset * (1 - bin2accuracy / 100d) <= dMultiSiteVout0A[idut] &&
                    dMultiSiteVout0A[idut] <= TargetOffset * (1 + bin2accuracy / 100d) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= TargetVoltage_customer * (1 + bin3accuracy / 100d) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= TargetVoltage_customer * (1 - bin3accuracy / 100d))
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_2;
                    this.txt_Status_AutoTab.ForeColor = Color.Green;
                    this.txt_Status_AutoTab.Text = "PASS!";
                }
                else if (TargetOffset * (1 - bin2accuracy / 100d) <= dMultiSiteVout0A[idut] &&
                    dMultiSiteVout0A[idut] <= TargetOffset * (1 + bin2accuracy / 100d) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= TargetVoltage_customer * (1 + bin3accuracy / 100d) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= TargetVoltage_customer * (1 - bin3accuracy / 100d))
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_3;
                    this.txt_Status_AutoTab.ForeColor = Color.Green;
                    this.txt_Status_AutoTab.Text = "PASS!";
                }
                else
                {
                    this.txt_Status_AutoTab.ForeColor = Color.Red;
                    this.txt_Status_AutoTab.Text = "FAIL!";
                }
            }
            else
            {
                /* bin1,2,3 */
                if (TargetOffset * (1 - bin2accuracy / 100d) <= dMultiSiteVout0A[idut] && dMultiSiteVout0A[idut] <= TargetOffset * (1 + bin2accuracy / 100d))
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_1;
                    this.txt_Status_AutoTab.ForeColor = Color.Green;
                    this.txt_Status_AutoTab.Text = "PASS!";
                }
                else
                {
                    this.txt_Status_AutoTab.ForeColor = Color.Red;
                    this.txt_Status_AutoTab.Text = "FAIL!";
                }
            }

            #endregion Bin

            #region Display Result and Reset parameters
            DisplayOperateMes("Bin" + " = " + uDutTrimResult[idut].ToString());
            MultiSiteDisplayResult(uDutTrimResult);
            TrimFinish();
            sDUT.iErrorCode = uDutTrimResult[idut];
            PrintDutAttribute(sDUT);
            DisplayOperateMes("Next...");
            #endregion Display Result and Reset parameters
        }

        private void AutoTrim_SL620_1V65()
        {
            #region Define Parameters
            DialogResult dr;
            bool bMarginal = false;
            bool bSafety = false;
            //uint[] tempReadback = new uint[5];
            double dVout_0A_Temp = 0;
            //TargetOffset = 2.5;
            double dVip_Target = TargetOffset + TargetVoltage_customer;
            //double dGainTestMinusTarget = 1;
            //double dGainTest = 0;
            ModuleAttribute sDUT;
            sDUT.dIQ = 0;
            sDUT.dVoutIPNative = 0;
            sDUT.dVout0ANative = 0;
            sDUT.dVoutIPMiddle = 0;
            sDUT.dVout0AMiddle = 0;
            sDUT.dVoutIPTrimmed = 0;
            sDUT.dVout0ATrimmed = 0;
            sDUT.iErrorCode = 00;
            sDUT.bDigitalCommFail = false;
            sDUT.bNormalModeFail = false;
            sDUT.bReadMarginal = false;
            sDUT.bReadSafety = false;
            sDUT.bTrimmed = false;

            // PARAMETERS DEFINE FOR MULTISITE
            uint idut = 0;
            uint uDutCount = 16;
            //bool bValidRound = false;
            //bool bSecondCurrentOn = false;
            double dModuleCurrent = 0;
            bool[] bGainBoost = new bool[16];
            bool[] bDutValid = new bool[16];
            bool[] bDutNoNeedTrim = new bool[16];
            uint[] uDutTrimResult = new uint[16];
            double[] dMultiSiteVoutIP = new double[16];
            double[] dMultiSiteVout0A = new double[16];

            /* autoAdaptingGoughGain algorithm*/
            double autoAdaptingGoughGain = 0;
            double autoAdaptingPresionGain = 0;
            double tempG1 = 0;
            double tempG2 = 0;
            //double dGainPreset = 0;
            int Ix_forAutoAdaptingRoughGain = 0;
            int Ix_forAutoAdaptingPresionGain = 0;

            int ix_forOffsetIndex_Rough = 0;
            int ix_forOffsetIndex_Rough_Complementary = 0;
            double dMultiSiteVout_0A_Complementary = 0;

            DisplayOperateMes("\r\n************" + DateTime.Now.ToString() + "************");
            DisplayOperateMes("Start...");
            this.txt_Status_AutoTab.ForeColor = Color.Black;
            this.txt_Status_AutoTab.Text = "START!";

            for (uint i = 0; i < uDutCount; i++)
            {
                dMultiSiteVoutIP[i] = 0d;
                dMultiSiteVout0A[i] = 0d;

                MultiSiteReg0[i] = Reg80Value;
                MultiSiteReg1[i] = Reg81Value;
                MultiSiteReg2[i] = Reg82Value;
                MultiSiteReg3[i] = Reg83Value;
                MultiSiteReg4[i] = Reg84Value;
                MultiSiteReg5[i] = Reg85Value;
                MultiSiteReg6[i] = Reg86Value;
                MultiSiteReg7[i] = Reg87Value;

                MultiSiteRoughGainCodeIndex[i] = preSetCoareseGainCode;

                uDutTrimResult[i] = 0u;
                bDutNoNeedTrim[i] = false;
                bDutValid[i] = false;
                bGainBoost[i] = false;
            }
            #endregion Define Parameters

            #region Get module current
            /*  power on */
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_5V);
            RePower();
            Delay(Delay_Sync);

            if (!this.cb_MeasureiQ_AutoTab.Checked)
            {
                /* Get module current */
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VCS);
                Delay(Delay_Sync);
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_SET_CURRENT_SENCE);

                this.txt_ModuleCurrent_EngT.Text = GetModuleCurrent().ToString("F1");
                this.txt_ModuleCurrent_PreT.Text = this.txt_ModuleCurrent_EngT.Text;


                dModuleCurrent = GetModuleCurrent();
                sDUT.dIQ = dModuleCurrent;
                if (dCurrentDownLimit > dModuleCurrent)
                {
                    DisplayOperateMes("Module " + " current is " + dModuleCurrent.ToString("F3"), Color.Red);
                    //uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_CURRENT_ABNORMAL;
                    PowerOff();
                    //PrintDutAttribute(sDUT);
                    MessageBox.Show(String.Format("电流偏低，检查模组是否连接！"), "Warning", MessageBoxButtons.OK);
                    return;
                }
                else if (dModuleCurrent > dCurrentUpLimit)
                {
                    DisplayOperateMes("Module " + " current is " + dModuleCurrent.ToString("F3"), Color.Red);
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_CURRENT_HIGH;
                    PowerOff();
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    //MessageBox.Show(String.Format("电流异常，模块短路或损坏！"), "Error", MessageBoxButtons.OK);
                    this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                    this.txt_Status_AutoTab.Text = "短路!";
                    return;
                }
                else
                    DisplayOperateMes("Module " + " current is " + dModuleCurrent.ToString("F3"));
            }
            #endregion Get module current

            #region Saturation judgement


            //Redundency delay in case of power off failure.
            Delay(Delay_Sync);
            EnterTestMode();
            //Delay(Delay_Sync);
            BurstRead(0x80, 5, tempReadback);
            if (tempReadback[0] + tempReadback[1] + tempReadback[2] + tempReadback[3] + tempReadback[4] != 0)
            {
                DisplayOperateMes("DUT" + " has some bits Blown!", Color.Red);
                uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_TRIMMED_SOMEBITS;
                TrimFinish();
                sDUT.bTrimmed = false;
                sDUT.iErrorCode = uDutTrimResult[idut];
                PrintDutAttribute(sDUT);
                this.txt_Status_AutoTab.ForeColor = Color.Red;
                this.txt_Status_AutoTab.Text = "FAIL!";
                return;
            }



            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            DisplayOperateMes(string.Format("0x80 = 0x{0};\r\n0x81 = 0x{1};\r\n0x82 = 0x{2};\r\n0x83 = 0x{3}",
                MultiSiteReg0[idut].ToString("X2"), MultiSiteReg1[idut].ToString("X2"), MultiSiteReg2[idut].ToString("X2"), MultiSiteReg3[idut].ToString("X2")));
            Delay(Delay_Sync);
            RegisterWrite(4, new uint[8] { 0x84, MultiSiteReg4[idut], 0x85, MultiSiteReg5[idut], 0x86, MultiSiteReg6[idut], 0x87, MultiSiteReg7[idut] });
            Delay(Delay_Sync);
            BurstRead(0x80, 5, tempReadback);
            if (tempReadback[0] != MultiSiteReg0[idut] || tempReadback[1] != MultiSiteReg1[idut]
                || tempReadback[2] != MultiSiteReg2[idut] || tempReadback[3] != MultiSiteReg3[idut])
            {
                if (tempReadback[0] + tempReadback[1] + tempReadback[2] + tempReadback[3] + tempReadback[4] == 0)
                {
                    RePower();
                    Delay(Delay_Sync);
                    oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);
                    Delay(Delay_Sync);
                    oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);
                    Delay(Delay_Fuse);
                    dMultiSiteVout0A[idut] = AverageVout();
                    if (dMultiSiteVout0A[idut] < 4.5 && dMultiSiteVout0A[idut] > 1.5)
                    {
                        DisplayOperateMes("DUT Trimmed!", Color.Red);
                        uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_TRIMMRD_ALREADY;
                        TrimFinish();
                        sDUT.bDigitalCommFail = true;
                        sDUT.bTrimmed = true;
                        sDUT.iErrorCode = uDutTrimResult[idut];
                        PrintDutAttribute(sDUT);
                        //MessageBox.Show(String.Format("模组已编程，交至研发部！"), "Error", MessageBoxButtons.OK);
                        this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                        this.txt_Status_AutoTab.Text = "混料!";
                        return;
                    }
                    else
                    {
                        DisplayOperateMes("VOUT Short!", Color.Red);
                        uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_VOUT_SHORT;
                        TrimFinish();
                        sDUT.bDigitalCommFail = true;
                        sDUT.iErrorCode = uDutTrimResult[idut];
                        PrintDutAttribute(sDUT);
                        //MessageBox.Show(String.Format("输出管脚短路！", Color.YellowGreen), "Warning", MessageBoxButtons.OK);
                        this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                        this.txt_Status_AutoTab.Text = "短路!";
                        return;
                    }
                }
                else
                {
                    DisplayOperateMes("DUT digital communication fail!", Color.Red);
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_COMM_FAIL;
                    TrimFinish();
                    sDUT.bDigitalCommFail = true;
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    //MessageBox.Show(String.Format("输出管脚短路！", Color.YellowGreen), "Warning", MessageBoxButtons.OK);
                    this.txt_Status_AutoTab.ForeColor = Color.Red;
                    this.txt_Status_AutoTab.Text = "FAIL!";
                    return;
                }
            }
            /* Get vout @ IP */
            EnterNomalMode();

            /* Change Current to IP  */
            if (ProgramMode == 0)
            {
                if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                    DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", IP));
                else
                {
                    DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                    TrimFinish();
                    return;
                }
            }
            else if (ProgramMode == 1)
            {
                dr = MessageBox.Show(String.Format("请将电流升至{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    PowerOff();
                    RestoreRegValue();
                    return;
                }
            }
            else if (ProgramMode == 2)
            {
                //set epio1 and epio3 to low
                MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                Delay(Delay_Sync);
                MultiSiteSocketSelect(0);       //set epio1 = high; epio3 = low
            }


            //oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);
            Delay(Delay_Fuse);
            dMultiSiteVoutIP[idut] = AverageVout();
            sDUT.dVoutIPNative = dMultiSiteVoutIP[idut];
            DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));

            /*Judge PreSet gain; delta Vout target >= delta Vout test * 86.07% */
            if (dMultiSiteVoutIP[idut] > saturationVout)
            {
                if (ProgramMode == 0)
                {
                    oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0);
                    Delay(Delay_Sync);
                    oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, 5);
                    Delay(Delay_Sync);
                    if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                        DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", 5));
                    else
                    {
                        DisplayOperateMes(string.Format("Set Current to {0}A failed!", 5));
                        TrimFinish();
                        return;
                    }
                }
                else if (ProgramMode == 1)
                {
                    dr = MessageBox.Show(String.Format("请将电流升至{0}A", 5), "Change Current", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                    {
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        PowerOff();
                        RestoreRegValue();
                        return;
                    }
                }
                else if (ProgramMode == 2)
                {
                    MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                    Delay(Delay_Sync);
                    MultiSiteSocketSelect(0);       //set epio1 = high; epio3 = low
                }


                Delay(Delay_Fuse);
                dMultiSiteVoutIP[idut] = AverageVout();
                sDUT.dVoutIPNative = dMultiSiteVoutIP[idut];
                DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));

                //set current back to IP
                if (ProgramMode == 0)
                {
                    oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0);
                    Delay(Delay_Sync);
                    oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, Convert.ToUInt32(IP));
                }


                if (dMultiSiteVoutIP[idut] > saturationVout)
                {
                    DisplayOperateMes("Module" + " Vout is VDD!", Color.Red);
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_VOUT_VDD;
                    TrimFinish();
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                    this.txt_Status_AutoTab.Text = "短路!";
                    return;
                }
                else
                {
                    //dr = MessageBox.Show(String.Format("输出饱和，交研发部重新编程！"), "Warning", MessageBoxButtons.OK);
                    TrimFinish();
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_VOUT_SATURATION;
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                    this.txt_Status_AutoTab.Text = "饱和!";
                    return;
                }
            }
            else if (dMultiSiteVoutIP[idut] < minimumVoutIP)
            {
                DisplayOperateMes("Module" + " Vout is too Low!", Color.Red);
                uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_VOUT_LOW;
                TrimFinish();
                sDUT.iErrorCode = uDutTrimResult[idut];
                PrintDutAttribute(sDUT);
                this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                this.txt_Status_AutoTab.Text = "短路!";
                return;
            }

            #endregion Saturation judgement

            #region Get Vout@0A
            /* Change Current to 0A */
            if (ProgramMode == 0)
            {
                if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0u))
                    DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", 0u));
                else
                {
                    DisplayOperateMes(string.Format("Set Current to {0}A failed!", 0u));
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    TrimFinish();
                    return;
                }
            }
            else if (ProgramMode == 1)
            {
                dr = MessageBox.Show(String.Format("请将IP降至0A!"), "Try Again", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    PowerOff();
                    RestoreRegValue();
                    return;
                }
            }
            else if (ProgramMode == 2)
            {
                MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                Delay(Delay_Sync);
                MultiSiteSocketSelect(9);       //set epio1 = low; epio3 = high
            }

            Delay(Delay_Fuse);
            dMultiSiteVout0A[idut] = AverageVout();
            sDUT.dVout0ANative = dMultiSiteVout0A[idut];
            DisplayOperateMes("Vout" + " @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

            if (dMultiSiteVoutIP[idut] < dMultiSiteVout0A[idut])
            {
                TrimFinish();
                //PrintDutAttribute(sDUT);
                MessageBox.Show(String.Format("请确认IP方向!"), "Try Again", MessageBoxButtons.OK);
                return;
            }
            else if (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut] < VoutIPThreshold)
            {
                TrimFinish();
                //PrintDutAttribute(sDUT);
                MessageBox.Show(String.Format("请确认电流为{0}A!!!", IP), "Try Again", MessageBoxButtons.OK);
                return;
            }

            if (TargetOffset == 2.5)
            {
                if (dMultiSiteVout0A[idut] < 2.25 || dMultiSiteVout0A[idut] > 2.8)
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_OFFSET_ABN;
                    TrimFinish();
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    this.txt_Status_AutoTab.ForeColor = Color.Red;
                    this.txt_Status_AutoTab.Text = "FAIL!";
                    return;
                }
            }
            else if (TargetOffset == 1.65)
            {
                if (dMultiSiteVout0A[idut] < 1.0 || dMultiSiteVout0A[idut] > 2.5)
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_OFFSET_ABN;
                    TrimFinish();
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    this.txt_Status_AutoTab.ForeColor = Color.Red;
                    this.txt_Status_AutoTab.Text = "FAIL!";
                    return;
                }
            }

            #endregion  Get Vout@0A

            #region No need Trim case
            if ((TargetOffset - 0.001) <= dMultiSiteVout0A[idut] && dMultiSiteVout0A[idut] <= (TargetOffset + 0.001)
                && (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= (TargetVoltage_customer + 0.001)
                && (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= (TargetVoltage_customer - 0.001))
            {
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_EXT);
                Delay(Delay_Sync);
                RePower();
                //Delay(Delay_Sync);
                EnterTestMode();

                RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
                Delay(Delay_Sync);
                RegisterWrite(4, new uint[8] { 0x84, MultiSiteReg4[idut], 0x85, MultiSiteReg5[idut], 0x86, MultiSiteReg6[idut], 0x87, MultiSiteReg7[idut] });
                Delay(Delay_Sync);


                BurstRead(0x80, 5, tempReadback);
                /* fuse */
                FuseClockOn(DeviceAddress, (double)num_UD_pulsewidth_ow_EngT.Value, (double)numUD_pulsedurationtime_ow_EngT.Value);
                DisplayOperateMes("Processing...");
                //Delay(Delay_Fuse);             

                //capture Vout
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_5V);
                RePower();
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);
                Delay(Delay_Sync);
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);

                Delay(Delay_Sync);
                dMultiSiteVout0A[idut] = AverageVout();
                sDUT.dVout0ATrimmed = dMultiSiteVout0A[idut];
                DisplayOperateMes("Vout" + " @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

                /* Change Current to IP  */
                if (ProgramMode == 0)
                {
                    if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                        DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", IP));
                    else
                    {
                        DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        TrimFinish();
                        return;
                    }
                }
                else if (ProgramMode == 1)
                {
                    dr = MessageBox.Show(String.Format("请将电流升至{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                    {
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        PowerOff();
                        RestoreRegValue();
                        return;
                    }
                }
                else if (ProgramMode == 2)
                {
                    MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                    Delay(Delay_Sync);
                    MultiSiteSocketSelect(0);       //set epio1 = high; epio3 = low
                }

                Delay(Delay_Fuse);
                dMultiSiteVoutIP[idut] = AverageVout();
                sDUT.dVoutIPTrimmed = dMultiSiteVoutIP[idut];
                DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));
                //oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_5V);

                DisplayOperateMes("DUT" + idut.ToString() + "Pass! Bin Normal");
                //uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_NORMAL;
                uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_1;
                sDUT.iErrorCode = uDutTrimResult[idut];
                this.txt_Status_AutoTab.ForeColor = Color.Green;
                this.txt_Status_AutoTab.Text = "PASS!";
                DisplayOperateMes("Bin" + " = " + uDutTrimResult[idut].ToString());
                DisplayOperateMes("Marginal Read ->" + bMarginal.ToString());
                DisplayOperateMes("Safety REad ->" + bSafety.ToString());
                MultiSiteDisplayResult(uDutTrimResult);
                TrimFinish();
                PrintDutAttribute(sDUT);
                return;

            }


            #endregion No need Trim case

            #region Adapting algorithm

            tempG1 = sl620CoarseGainTable[0][MultiSiteRoughGainCodeIndex[idut]] / 100d;
            tempG2 = (TargetGain_customer / ((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) / IP)) / 1000d;

            autoAdaptingGoughGain = tempG1 * tempG2 * 100d;
            DisplayOperateMes("IdealGoughGain = " + autoAdaptingGoughGain.ToString("F4"));

            Ix_forAutoAdaptingRoughGain = LookupCoarseGain_SL620(autoAdaptingGoughGain, sl620CoarseGainTable);
            autoAdaptingPresionGain = 100d * autoAdaptingGoughGain / sl620CoarseGainTable[0][Ix_forAutoAdaptingRoughGain];
            //Ix_forAutoAdaptingPresionGain = LookupFineGain_SL620(autoAdaptingPresionGain, sl620FineGainTable);
            if (bAutoTrimTest)
            {
                DisplayOperateMes("IP = " + IP.ToString("F0"));
                DisplayOperateMes("TargetGain_customer" + idut.ToString() + " = " + TargetGain_customer.ToString("F4"));
                DisplayOperateMes("(dMultiSiteVoutIP - dMultiSiteVout0A)/IP = " + ((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) / IP).ToString("F4"));
                DisplayOperateMes("Pre-set gain" + idut.ToString() + " = " + tempG1.ToString("F4"));
                DisplayOperateMes("Target gain / Test gain" + idut.ToString() + " = " + tempG2.ToString("F4"));
                DisplayOperateMes("Ix_forAutoAdaptingRoughGain" + idut.ToString() + " = " + Ix_forAutoAdaptingRoughGain.ToString("F0"));
                DisplayOperateMes("Ix_forAutoAdaptingPresionGain" + idut.ToString() + " = " + Ix_forAutoAdaptingPresionGain.ToString("F0"));
                DisplayOperateMes("autoAdaptingGoughGain" + idut.ToString() + " = " + sl620CoarseGainTable[0][Ix_forAutoAdaptingRoughGain].ToString("F4"));
                DisplayOperateMes("autoAdaptingPresionGain" + idut.ToString() + " = " + sl620FineGainTable[0][Ix_forAutoAdaptingPresionGain].ToString("F4"));
            }

            /* Rough Gain Code*/
            bit_op_mask = bit4_Mask | bit5_Mask | bit6_Mask | bit7_Mask;
            MultiSiteReg1[idut] &= ~bit_op_mask;
            MultiSiteReg1[idut] |= Convert.ToUInt32(sl620CoarseGainTable[1][Ix_forAutoAdaptingRoughGain]);


            if (bAutoTrimTest)
            {
                //DisplayOperateMes("Rough Gain RegValue80 = 0x" + MultiSiteReg0[idut].ToString("X2"));
                DisplayOperateMes("Rough Gain RegValue81 = 0x" + MultiSiteReg1[idut].ToString("X2"));
            }

            /******************************************************************************
            *
            *   Coarse Offset Alg
            *
            ******************************************************************************/
            RePower();
            EnterTestMode();

            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            Delay(Delay_Sync);
            RegisterWrite(4, new uint[8] { 0x84, MultiSiteReg4[idut], 0x85, MultiSiteReg5[idut], 0x86, MultiSiteReg6[idut], 0x87, MultiSiteReg7[idut] });
            Delay(Delay_Sync);

            //RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            EnterNomalMode();
            Delay(Delay_Fuse);
            /* Offset trim code calculate */
            dMultiSiteVout0A[idut] = AverageVout();
            Vout_0A = dMultiSiteVout0A[idut];


            bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask | bit3_Mask | bit4_Mask;
            uint ix_CoarseOffsetCode = 0;
            if (Vout_0A > TargetOffset)
            {
                ix_CoarseOffsetCode = Convert.ToUInt32(Math.Round(1000d * (1.0d - TargetOffset / Vout_0A) / 5));
                if (ix_CoarseOffsetCode > 15)
                    ix_CoarseOffsetCode = 15;

                //autoAdaptingGoughGain = sl620CoarseGainTable[0][Ix_forAutoAdaptingRoughGain] / (1.0 - ix_CoarseOffsetCode * 0.005);
                autoAdaptingGoughGain = tempG2 * tempG1 * 100d / (1.0 - ix_CoarseOffsetCode * 0.005);

                if (TargetOffset == 2.5)
                    Ix_forAutoAdaptingRoughGain = LookupCoarseGain_SL620(autoAdaptingGoughGain, sl620CoarseGainTable);
                //1.65V case
                else
                    Ix_forAutoAdaptingRoughGain = LookupCoarseGain_SL620(autoAdaptingGoughGain, sl620CoarseGainTable);
                /* Rough Gain Code*/
                bit_op_mask = bit4_Mask | bit5_Mask | bit6_Mask | bit7_Mask;
                MultiSiteReg1[idut] &= ~bit_op_mask;
                MultiSiteReg1[idut] |= Convert.ToUInt32(sl620CoarseGainTable[1][Ix_forAutoAdaptingRoughGain]);


            }
            else if (Vout_0A < TargetOffset)
            {
                ix_CoarseOffsetCode = 31 - Convert.ToUInt32(Math.Round(1000d * (TargetOffset / Vout_0A - 1.0d) / 5));
                //if (ix_CoarseOffsetCode == 32)
                //    ix_CoarseOffsetCode = 0;
                if (ix_CoarseOffsetCode < 16)
                    ix_CoarseOffsetCode = 16;

                //autoAdaptingGoughGain = sl620CoarseGainTable[0][Ix_forAutoAdaptingRoughGain] / (1.0 + ix_CoarseOffsetCode * 0.005);
                autoAdaptingGoughGain = tempG2 * tempG1 * 100d / (1.0 + (31 - ix_CoarseOffsetCode) * 0.005);

                if (TargetOffset == 2.5)
                    Ix_forAutoAdaptingRoughGain = LookupCoarseGain_SL620(autoAdaptingGoughGain, sl620CoarseGainTable);
                //1.65V case
                else
                    Ix_forAutoAdaptingRoughGain = LookupCoarseGain_SL620(autoAdaptingGoughGain, sl620CoarseGainTable);

                /* Rough Gain Code*/
                bit_op_mask = bit4_Mask | bit5_Mask | bit6_Mask | bit7_Mask;
                MultiSiteReg1[idut] &= ~bit_op_mask;
                MultiSiteReg1[idut] |= Convert.ToUInt32(sl620CoarseGainTable[1][Ix_forAutoAdaptingRoughGain]);
            }

            MultiSiteReg6[idut] &= ~bit_op_mask;
            MultiSiteReg6[idut] |= ix_CoarseOffsetCode;

            DisplayOperateMes("Vout_0A = " + Vout_0A.ToString("F3"));
            DisplayOperateMes("ix_CoarseOffsetCode = " + ix_CoarseOffsetCode.ToString());
            /////////////////////////////////////////////////////////////////////////////////////////////////




            RePower();
            EnterTestMode();

            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            Delay(Delay_Sync);
            RegisterWrite(4, new uint[8] { 0x84, MultiSiteReg4[idut], 0x85, MultiSiteReg5[idut], 0x86, MultiSiteReg6[idut], 0x87, MultiSiteReg7[idut] });
            Delay(Delay_Sync);

            EnterNomalMode();

            #region /* Change Current to IP  */
            if (ProgramMode == 0)
            {
                if (!oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                {
                    DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    TrimFinish();
                    return;
                }
            }
            else if (ProgramMode == 1)
            {
                dr = MessageBox.Show(String.Format("请将电流升至{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    PowerOff();
                    RestoreRegValue();
                    return;
                }
            }
            else if (ProgramMode == 2)
            {
                MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                Delay(Delay_Sync);
                MultiSiteSocketSelect(0);       //set epio1 = high; epio3 = low
            }
            #endregion

            Delay(Delay_Fuse);
            dMultiSiteVoutIP[idut] = AverageVout();
            DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));

            #region Change Current to 0A */
            if (ProgramMode == 0)
            {
                if (!oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0u))
                {
                    DisplayOperateMes(string.Format("Set Current to {0}A failed!", 0u));
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    TrimFinish();
                    return;
                }
            }
            else if (ProgramMode == 1)
            {
                dr = MessageBox.Show(String.Format("请将电流降至0A"), "Change Current", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    PowerOff();
                    RestoreRegValue();
                    return;
                }
            }
            else if (ProgramMode == 2)
            {
                MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                Delay(Delay_Sync);
                MultiSiteSocketSelect(9);       //set epio1 = low; epio3 = high
            }
            #endregion

            /*  power on */
            Delay(Delay_Fuse);
            dMultiSiteVout0A[idut] = AverageVout();
            DisplayOperateMes("DUT" + " Vout @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

            double gainTest = 0;

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            gainTest = (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) * 1000d / IP;

            if (gainTest < TargetGain_customer * 0.995)
            {
                DisplayOperateMes("Tuning coarse gain", Color.DarkRed);

                /* Rough Gain Code*/
                if (Ix_forAutoAdaptingRoughGain > 0)
                {
                    bit_op_mask = bit4_Mask | bit5_Mask | bit6_Mask | bit7_Mask;
                    MultiSiteReg1[idut] &= ~bit_op_mask;
                    MultiSiteReg1[idut] |= Convert.ToUInt32(sl620CoarseGainTable[1][Ix_forAutoAdaptingRoughGain - 1]);

                    RePower();
                    EnterTestMode();

                    RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
                    Delay(Delay_Sync);
                    RegisterWrite(4, new uint[8] { 0x84, MultiSiteReg4[idut], 0x85, MultiSiteReg5[idut], 0x86, MultiSiteReg6[idut], 0x87, MultiSiteReg7[idut] });
                    Delay(Delay_Sync);

                    EnterNomalMode();

                    #region /* Change Current to IP  */
                    if (ProgramMode == 0)
                    {
                        if (!oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                        {
                            DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                            DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                            TrimFinish();
                            return;
                        }
                    }
                    else if (ProgramMode == 1)
                    {
                        dr = MessageBox.Show(String.Format("请将电流升至{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                        if (dr == DialogResult.Cancel)
                        {
                            DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                            PowerOff();
                            RestoreRegValue();
                            return;
                        }
                    }
                    else if (ProgramMode == 2)
                    {
                        MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                        Delay(Delay_Sync);
                        MultiSiteSocketSelect(0);       //set epio1 = high; epio3 = low
                    }
                    #endregion

                    Delay(Delay_Fuse);
                    dMultiSiteVoutIP[idut] = AverageVout();
                    DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));

                    #region Change Current to 0A */
                    if (ProgramMode == 0)
                    {
                        if (!oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0u))
                        {
                            DisplayOperateMes(string.Format("Set Current to {0}A failed!", 0u));
                            DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                            TrimFinish();
                            return;
                        }
                    }
                    else if (ProgramMode == 1)
                    {
                        dr = MessageBox.Show(String.Format("请将电流降至0A"), "Change Current", MessageBoxButtons.OKCancel);
                        if (dr == DialogResult.Cancel)
                        {
                            DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                            PowerOff();
                            RestoreRegValue();
                            return;
                        }
                    }
                    else if (ProgramMode == 2)
                    {
                        MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                        Delay(Delay_Sync);
                        MultiSiteSocketSelect(9);       //set epio1 = low; epio3 = high
                    }
                    #endregion

                    /*  power on */
                    Delay(Delay_Fuse);
                    dMultiSiteVout0A[idut] = AverageVout();
                    DisplayOperateMes("DUT" + " Vout @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

                    gainTest = 0;

                    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    gainTest = (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) * 1000d / IP;
                }
                else
                {
                    DisplayOperateMes("coarsegaincode = " + Ix_forAutoAdaptingRoughGain.ToString());
                    DisplayOperateMes("不适合做此产品，此芯片可用灵敏度更低的产品！", Color.DarkRed);
                    return;
                }

            }

            autoAdaptingPresionGain = 100d * (TargetGain_customer / gainTest);
            Ix_forAutoAdaptingPresionGain = LookupFineGain_SL620(autoAdaptingPresionGain, sl620FineGainTable);
            DisplayOperateMes("Ix_forAutoAdaptingPresionGain = " + Ix_forAutoAdaptingPresionGain.ToString());

            /* Fine Gain Code*/
            bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
            MultiSiteReg6[idut] &= ~bit_op_mask;
            MultiSiteReg6[idut] |= Convert.ToUInt32(sl620FineGainTable[1][Ix_forAutoAdaptingPresionGain]);

            bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
            MultiSiteReg7[idut] &= ~bit_op_mask;
            MultiSiteReg7[idut] |= Convert.ToUInt32(sl620FineGainTable[2][Ix_forAutoAdaptingPresionGain]);

            if (bAutoTrimTest)
            {
                DisplayOperateMes("Precesion Gain RegValue86 = 0x" + MultiSiteReg6[idut].ToString("X2"));
                DisplayOperateMes("Precesion Gain RegValue87 = 0x" + MultiSiteReg7[idut].ToString("X2"));
                DisplayOperateMes("***new add approach***");
            }
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////         

            RePower();
            EnterTestMode();

            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            Delay(Delay_Sync * 2);
            RegisterWrite(4, new uint[8] { 0x84, MultiSiteReg4[idut], 0x85, MultiSiteReg5[idut], 0x86, MultiSiteReg6[idut], 0x87, MultiSiteReg7[idut] });
            Delay(Delay_Sync);

            BurstRead(0x85, 4, tempReadback);

            //RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            EnterNomalMode();
            Delay(Delay_Fuse);
            dMultiSiteVout0A[idut] = AverageVout();
            Vout_0A = dMultiSiteVout0A[idut];

            ///////////////////////////////////////////////////////////////////////////////////////////////////
            bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask | bit3_Mask | bit4_Mask;
            uint ix_FineOffsetCode = 0;
            if (Vout_0A > TargetOffset)
                ix_FineOffsetCode = Convert.ToUInt32(Math.Round(1000d * (1.0d - TargetOffset / Vout_0A) / 1.5));
            else if (Vout_0A < TargetOffset)
                ix_FineOffsetCode = 31 - Convert.ToUInt32(Math.Round(1000d * (TargetOffset / Vout_0A - 1.0d) / 1.5));

            MultiSiteReg7[idut] &= ~bit_op_mask;
            MultiSiteReg7[idut] |= ix_FineOffsetCode;

            //ix_forOffsetIndex_Rough = LookupOffsetIndex(MultiSiteReg3[idut] & bit_op_mask, OffsetTableB_Customer);
            //ix_FineOffsetCode = ix_forOffsetIndex_Rough;
            DisplayOperateMes("Vout_0A = " + Vout_0A.ToString("F3"));
            DisplayOperateMes("ix_FineOffsetCode = " + ix_FineOffsetCode.ToString());
            DisplayOperateMes("\r\nProcessing...");


            //*******************1.65V case ***********************
            RePower();
            EnterTestMode();

            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            Delay(Delay_Sync);
            RegisterWrite(4, new uint[8] { 0x84, MultiSiteReg4[idut], 0x85, MultiSiteReg5[idut], 0x86, MultiSiteReg6[idut], 0x87, MultiSiteReg7[idut] });
            Delay(Delay_Sync);

            EnterNomalMode();

            #region /* Change Current to IP  */
            if (ProgramMode == 0)
            {
                if (!oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                {
                    DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    TrimFinish();
                    return;
                }
            }
            else if (ProgramMode == 1)
            {
                dr = MessageBox.Show(String.Format("请将电流升至{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    PowerOff();
                    RestoreRegValue();
                    return;
                }
            }
            else if (ProgramMode == 2)
            {
                MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                Delay(Delay_Sync);
                MultiSiteSocketSelect(0);       //set epio1 = high; epio3 = low
            }
            #endregion

            Delay(Delay_Fuse);
            dMultiSiteVoutIP[idut] = AverageVout();
            DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));

            #region Change Current to 0A */
            if (ProgramMode == 0)
            {
                if (!oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0u))
                {
                    DisplayOperateMes(string.Format("Set Current to {0}A failed!", 0u));
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    TrimFinish();
                    return;
                }
            }
            else if (ProgramMode == 1)
            {
                dr = MessageBox.Show(String.Format("请将电流降至0A"), "Change Current", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    PowerOff();
                    RestoreRegValue();
                    return;
                }
            }
            else if (ProgramMode == 2)
            {
                MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                Delay(Delay_Sync);
                MultiSiteSocketSelect(9);       //set epio1 = low; epio3 = high
            }
            #endregion

            /*  power on */
            //Delay(Delay_Fuse);
            //dMultiSiteVout0A[idut] = AverageVout();
            //DisplayOperateMes("DUT" + " Vout @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

            //gainTest = (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) * 1000d / IP;
            if (dMultiSiteVoutIP[idut] - dVip_Target > 0.004)
            {
                Ix_forAutoAdaptingPresionGain += Convert.ToInt32(Math.Round((dMultiSiteVoutIP[idut] - dVip_Target) / 0.003));
                DisplayOperateMes("Ix_forAutoAdaptingPresionGain = " + Ix_forAutoAdaptingPresionGain.ToString());

                if (Ix_forAutoAdaptingPresionGain > 63)
                    Ix_forAutoAdaptingPresionGain = 63;

                /* Fine Gain Code*/
                bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
                MultiSiteReg6[idut] &= ~bit_op_mask;
                MultiSiteReg6[idut] |= Convert.ToUInt32(sl620FineGainTable[1][Ix_forAutoAdaptingPresionGain]);

                bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
                MultiSiteReg7[idut] &= ~bit_op_mask;
                MultiSiteReg7[idut] |= Convert.ToUInt32(sl620FineGainTable[2][Ix_forAutoAdaptingPresionGain]);
            }
            else if ( dVip_Target - dMultiSiteVoutIP[idut] > 0.005)
            {
                Ix_forAutoAdaptingPresionGain -= Convert.ToInt32(Math.Round((dVip_Target - dMultiSiteVoutIP[idut]) / 0.003));
                DisplayOperateMes("Ix_forAutoAdaptingPresionGain = " + Ix_forAutoAdaptingPresionGain.ToString());

                if (Ix_forAutoAdaptingPresionGain < 0)
                    Ix_forAutoAdaptingPresionGain = 0;

                /* Fine Gain Code*/
                bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
                MultiSiteReg6[idut] &= ~bit_op_mask;
                MultiSiteReg6[idut] |= Convert.ToUInt32(sl620FineGainTable[1][Ix_forAutoAdaptingPresionGain]);

                bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
                MultiSiteReg7[idut] &= ~bit_op_mask;
                MultiSiteReg7[idut] |= Convert.ToUInt32(sl620FineGainTable[2][Ix_forAutoAdaptingPresionGain]);
            }
            //********************************************************


            DisplayOperateMes("Processing...");

            #endregion Adapting algorithm

            #region Fuse
            //Fuse
            //oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_EXT);
            RePower();
            EnterTestMode();

            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            Delay(Delay_Sync);
            RegisterWrite(4, new uint[8] { 0x84, MultiSiteReg4[idut], 0x85, MultiSiteReg5[idut], 0x86, MultiSiteReg6[idut], 0x87, MultiSiteReg7[idut] });
            Delay(Delay_Sync);
            RegisterWrite(1, new uint[2] { 0x88, 0x02 });

            if (this.cb_BypFuse_AutoTab.Checked)
            {
                printRegValue();
                TrimFinish();
                return;
            }

            FuseClockOn(DeviceAddress, (double)num_UD_pulsewidth_ow_EngT.Value, (double)numUD_pulsedurationtime_ow_EngT.Value);
            DisplayOperateMes("Trimming...");

            #endregion

            #region Bin

            if (this.cb_AutoTab_Retest.Text == "Yes")
            {
                /* Repower on 5V */
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);
                Delay(Delay_Sync);
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_5V);
                RePower();
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);
                Delay(Delay_Fuse);

                dMultiSiteVout0A[idut] = AverageVout();
                sDUT.dVout0ATrimmed = dMultiSiteVout0A[idut];
                DisplayOperateMes("Vout" + " @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

                /* Change Current to IP  */
                if (ProgramMode == 0)
                {
                    //if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, Convert.ToUInt32(IP)))
                    if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                        DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", IP));
                    else
                    {
                        DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        TrimFinish();
                        return;
                    }
                }
                else if (ProgramMode == 1)
                {
                    dr = MessageBox.Show(String.Format("请将电流升至{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                    {
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        PowerOff();
                        RestoreRegValue();
                        return;
                    }
                }
                else if (ProgramMode == 2)
                {
                    MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                    Delay(Delay_Sync);
                    MultiSiteSocketSelect(0);       //set epio1 = high; epio3 = low
                }

                Delay(Delay_Fuse);
                dMultiSiteVoutIP[idut] = AverageVout();
                sDUT.dVoutIPTrimmed = dMultiSiteVoutIP[idut];
                DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));

                /* bin1,2,3 */
                if (TargetOffset * (1 - 0.002) <= dMultiSiteVout0A[idut] && dMultiSiteVout0A[idut] <= TargetOffset * (1 + 0.002) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= TargetVoltage_customer * (1 + 0.002) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= TargetVoltage_customer * (1 - 0.002))
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_1;
                    this.txt_Status_AutoTab.ForeColor = Color.Green;
                    this.txt_Status_AutoTab.Text = "PASS!";
                }
                else if (TargetOffset * (1 - bin2accuracy / 100d) <= dMultiSiteVout0A[idut] &&
                    dMultiSiteVout0A[idut] <= TargetOffset * (1 + bin2accuracy / 100d) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= TargetVoltage_customer * (1 + bin3accuracy / 100d) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= TargetVoltage_customer * (1 - bin3accuracy / 100d))
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_2;
                    this.txt_Status_AutoTab.ForeColor = Color.Green;
                    this.txt_Status_AutoTab.Text = "PASS!";
                }
                else if (TargetOffset * (1 - bin2accuracy / 100d) <= dMultiSiteVout0A[idut] &&
                    dMultiSiteVout0A[idut] <= TargetOffset * (1 + bin2accuracy / 100d) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= TargetVoltage_customer * (1 + bin3accuracy / 100d) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= TargetVoltage_customer * (1 - bin3accuracy / 100d))
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_3;
                    this.txt_Status_AutoTab.ForeColor = Color.Green;
                    this.txt_Status_AutoTab.Text = "PASS!";
                }
                else
                {
                    this.txt_Status_AutoTab.ForeColor = Color.Red;
                    this.txt_Status_AutoTab.Text = "FAIL!";
                }
            }
            else if (this.cb_AutoTab_Retest.Text == "No")
            {
                /* Repower on 5V */
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);
                Delay(Delay_Sync);
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_5V);
                RePower();
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);
                Delay(Delay_Fuse);

                dMultiSiteVout0A[idut] = AverageVout();
                sDUT.dVout0ATrimmed = dMultiSiteVout0A[idut];
                DisplayOperateMes("Vout" + " @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

                /* bin1,2,3 */
                if (TargetOffset * (1 - 0.002) <= dMultiSiteVout0A[idut] && dMultiSiteVout0A[idut] <= TargetOffset * (1 + 0.002))
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_1;
                    this.txt_Status_AutoTab.ForeColor = Color.Green;
                    this.txt_Status_AutoTab.Text = "PASS!";
                }
                else if (TargetOffset * (1 - bin2accuracy / 100d) <= dMultiSiteVout0A[idut] &&
                    dMultiSiteVout0A[idut] <= TargetOffset * (1 + bin2accuracy / 100d))
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_2;
                    this.txt_Status_AutoTab.ForeColor = Color.Green;
                    this.txt_Status_AutoTab.Text = "PASS!";
                }
                else
                {
                    this.txt_Status_AutoTab.ForeColor = Color.Red;
                    this.txt_Status_AutoTab.Text = "FAIL!";
                }
            }

            #endregion Bin

            #region Display Result and Reset parameters
            DisplayOperateMes("Bin" + " = " + uDutTrimResult[idut].ToString());
            MultiSiteDisplayResult(uDutTrimResult);
            TrimFinish();
            sDUT.iErrorCode = uDutTrimResult[idut];
            PrintDutAttribute(sDUT);
            DisplayOperateMes("Next...");
            #endregion Display Result and Reset parameters
        }

        private void DualPartMode()
        { 
        
        }
         
        private double abs(double p)
        {
            throw new NotImplementedException();
        }


        //sel_vr button
        private void btn_sel_vr_Click(object sender, EventArgs e)
        {
            uint _dev_addr = 0x73;  //Device Address
            uint _reg_Addr;
            uint _reg_Value;


            //Enter normal mode
            _reg_Addr = 0x55;
            _reg_Value = 0xAA;
            if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
                DisplayAutoTrimOperateMes("Enter Test Mode Before Enter Normal Mode", true, 32);
            else
            {
                //DisplayAutoTrimResult(false);
                //DisplayAutoTrimResult(false, 0x0006, "I2C Conmunication Error!");
                return;
            }

            _reg_Addr = 0x82;
            _reg_Value = 0x08;
            if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
                DisplayAutoTrimOperateMes("Enter Normal Mode", true, 32);
            else
            {
                //DisplayAutoTrimResult(false);
                //DisplayAutoTrimResult(false, 0x0006, "I2C Conmunication Error!");
                return;
            }
        }

        private void btn_nc_1x_Click(object sender, EventArgs e)
        {
            uint _dev_addr = 0x73;  //Device Address
            uint _reg_Addr;
            uint _reg_Value;


            //Enter normal mode
            _reg_Addr = 0x55;
            _reg_Value = 0xAA;
            if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
                DisplayAutoTrimOperateMes("Enter Test Mode Before Enter Normal Mode", true, 32);
            else
            {
                //DisplayAutoTrimResult(false);
                //DisplayAutoTrimResult(false, 0x0006, "I2C Conmunication Error!");
                return;
            }

            _reg_Addr = 0x83;
            _reg_Value = 0x01;
            if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
                DisplayAutoTrimOperateMes("Write NC_1X", true, 32);
            else
            {
                //DisplayAutoTrimResult(false);
                //DisplayAutoTrimResult(false, 0x0006, "I2C Conmunication Error!");
                return;
            }
        }

        private void btn_ch_ck_Click(object sender, EventArgs e)
        {
            uint _dev_addr = 0x73;  //Device Address
            uint _reg_Addr;
            uint _reg_Value;


            //Enter normal mode
            _reg_Addr = 0x55;
            _reg_Value = 0xAA;
            if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
                DisplayAutoTrimOperateMes("Enter Test Mode Before Enter Normal Mode", true, 32);
            else
            {
                //DisplayAutoTrimResult(false);
                //DisplayAutoTrimResult(false, 0x0006, "I2C Conmunication Error!");
                return;
            }

            _reg_Addr = 0x82;
            _reg_Value = 0x80;
            if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
                DisplayAutoTrimOperateMes("Enter Normal Mode", true, 32);
            else
            {
                //DisplayAutoTrimResult(false);
                //DisplayAutoTrimResult(false, 0x0006, "I2C Conmunication Error!");
                return;
            }
        }

        private void btn_sel_cap_Click(object sender, EventArgs e)
        {
            uint _dev_addr = 0x73;  //Device Address
            uint _reg_Addr;
            uint _reg_Value;


            //Enter normal mode
            _reg_Addr = 0x55;
            _reg_Value = 0xAA;
            if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
                DisplayAutoTrimOperateMes("Enter Test Mode Before Enter Normal Mode", true, 32);
            else
            {
                //DisplayAutoTrimResult(false);
                //DisplayAutoTrimResult(false, 0x0006, "I2C Conmunication Error!");
                return;
            }

            _reg_Addr = 0x81;
            _reg_Value = 0x08;
            if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
                DisplayAutoTrimOperateMes("Enter Normal Mode", true, 32);
            else
            {
                //DisplayAutoTrimResult(false);
                //DisplayAutoTrimResult(false, 0x0006, "I2C Conmunication Error!");
                return;
            }
        }

        private void txt_dev_addr_onewire_EngT_TextChanged(object sender, EventArgs e)
        {
            string temp;
            try
            {
                temp = this.txt_dev_addr_onewire_EngT.Text.TrimStart("0x".ToCharArray()).TrimEnd("H".ToCharArray());
                this.DeviceAddress = UInt32.Parse((temp == "" ? "0" : temp), System.Globalization.NumberStyles.HexNumber);
            }
            catch
            {
                temp = string.Format("Device address set failed, will use default adrress {0}", this.DeviceAddress);
                DisplayOperateMes(temp, Color.Red);
                this.txt_dev_addr_onewire_EngT.Text = "0x" + this.DeviceAddress.ToString("X2");
            }
            finally 
            {
                //this.txt_dev_addr_onewire_EngT.Text = "0x" + this.DeviceAddress.ToString("X2");
            }
        }

        private void btn_Reset_EngT_Click(object sender, EventArgs e)
        {
            Console.WriteLine("Flash result->{0}", oneWrie_device.ResetBoard());
        }

        private void btn_ModuleCurrent_EngT_Click(object sender, EventArgs e)
        {
            if (! oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VCS))
                DisplayOperateMes("Set ADC VIN to VCS failed", Color.Red);

            if (! oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_SET_CURRENT_SENCE))
                DisplayOperateMes("Set ADC current sensor failed", Color.Red);

            this.txt_ModuleCurrent_EngT.Text = GetModuleCurrent().ToString("F1");
            this.txt_ModuleCurrent_PreT.Text = this.txt_ModuleCurrent_EngT.Text;

            DisplayOperateMes("IQ = " + this.txt_ModuleCurrent_PreT.Text + "mA");
        }

        private void txt_sampleNum_EngT_TextChanged(object sender, EventArgs e)
        {
            string temp;
            try
            {
                temp = this.txt_sampleNum_EngT.Text;
                SampleRateNum = UInt32.Parse((temp == "" ? "0" : temp));
            }
            catch
            {
                temp = string.Format("Sample rate number set failed, will use default value {0}", this.SampleRateNum);
                DisplayOperateMes(temp, Color.Red);
            }
            finally 
            {
                this.txt_sampleNum_EngT.Text = this.SampleRateNum.ToString();
            }
        }

        private void txt_sampleRate_EngT_TextChanged(object sender, EventArgs e)
        {
            string temp;
            try
            {
                temp = this.txt_sampleRate_EngT.Text;
                SampleRate = UInt32.Parse((temp == "" ? "0" : temp));   //Get the KHz value
                SampleRate *= 1000;     //Change to Hz
            }
            catch
            {
                temp = string.Format("Sample rate set failed, will use default value {0}", this.SampleRate/1000);
                DisplayOperateMes(temp, Color.Red);
            }
            finally
            {
                this.txt_sampleRate_EngT.Text = (this.SampleRate / 1000).ToString();
            }
        }

        private void btn_VoutIP_EngT_Click(object sender, EventArgs e)
        {
            //oneWrie_device.ADCSigPathSet(OneWireInterface.ADCControlCommand.ADC_VIN_TO_VOUT);
            rbt_signalPathSeting_AIn_EngT.Checked = true;
            rbt_signalPathSeting_Vout_EngT.Checked = true;

            Vout_IP = AverageVout();
            DisplayOperateMes("Vout @ IP = " + Vout_IP.ToString("F3"));
        }

        private void btn_Vout0A_EngT_Click(object sender, EventArgs e)
        {
            //oneWrie_device.ADCSigPathSet(OneWireInterface.ADCControlCommand.ADC_VIN_TO_VOUT);
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);
            rbt_signalPathSeting_AIn_EngT.Checked = true;
            rbt_signalPathSeting_Vout_EngT.Checked = true;

            Vout_0A = AverageVout();
            DisplayOperateMes("Vout @ 0A = " + Vout_0A.ToString("F3"));
        }

        private void btn_Vout_PreT_Click(object sender, EventArgs e)
        {
            //RePower();
            //MultiSiteSocketSelect(0);
            //EnterTestMode();

            //int wrNum = 4;
            //uint[] data = new uint[2 * wrNum];
            //data[0] = 0x80;
            //data[1] = Reg80Value;
            //data[2] = 0x81;
            //data[3] = Reg81Value;
            //data[4] = 0x82;
            //data[5] = Reg82Value;
            //data[6] = 0x83;
            //data[7] = Reg83Value;

            //if (!RegisterWrite(wrNum, data))
            //   DisplayOperateMes("Register write failed!", Color.Red);

            //EnterNomalMode();

            //Delay(Delay_Fuse);

            txt_PresetVoutIP_PreT.Text = AverageVout().ToString("F3");
        }

        private void btn_GainCtrlPlus_PreT_Click(object sender, EventArgs e)
        {
            //oneWrie_device.ADCSigPathSet(OneWireInterface.ADCControlCommand.ADC_VOUT_WITHOUT_CAP);

            //RePower();

            //EnterTestMode();

            if (Ix_ForRoughGainCtrl < 15)
                Ix_ForRoughGainCtrl++;
            else
                DisplayOperateMes("Reach to Max Coarse Gain!", Color.DarkRed);

            int wrNum = 4;
            uint[] data = new uint[2 * wrNum];
            data[0] = 0x80;
            data[1] = Convert.ToUInt32(RoughTable_Customer[1][Ix_ForRoughGainCtrl]);     //Reg0x80
            data[2] = 0x81;
            data[3] = Convert.ToUInt32(RoughTable_Customer[2][Ix_ForRoughGainCtrl]);   //Reg0x81
            data[4] = 0x82;
            data[5] = Reg82Value;                                                        //Reg0x82
            data[6] = 0x83;
            data[7] = Reg83Value;                                                        //Reg0x83

            //back up to register 
            /* bit5 & bit6 & bit7 of 0x80 */
            bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
            Reg80Value &= ~bit_op_mask;
            Reg80Value |= data[1];

            /* bit0 of 0x81 */
            bit_op_mask = bit0_Mask;
            Reg81Value &= ~bit_op_mask;
            Reg81Value |= data[3];

            //if (!RegisterWrite(wrNum, data))
             //   DisplayOperateMes("Register write failed!", Color.Red);

            //EnterNomalMode();
            //txt_PresetVoutIP_PreT.Text = AverageVout().ToString("F3");

            //oneWrie_device.ADCSigPathSet(OneWireInterface.ADCControlCommand.ADC_VOUT_WITH_CAP);
        }

        private void btn_GainCtrlMinus_PreT_Click(object sender, EventArgs e)
        {
            //oneWrie_device.ADCSigPathSet(OneWireInterface.ADCControlCommand.ADC_VOUT_WITHOUT_CAP);

            //RePower();

            //EnterTestMode();

            if (Ix_ForRoughGainCtrl > 0)
                Ix_ForRoughGainCtrl--;
            else
                DisplayOperateMes("Reach to Min Coarse Gain!", Color.DarkRed);

            int wrNum = 4;
            uint[] data = new uint[2 * wrNum];
            data[0] = 0x80;
            data[1] = Convert.ToUInt32(RoughTable_Customer[1][Ix_ForRoughGainCtrl]);     //Reg0x80
            data[2] = 0x81;
            data[3] = Convert.ToUInt32(RoughTable_Customer[2][Ix_ForRoughGainCtrl]);     //Reg0x81
            data[4] = 0x82;
            data[5] = Reg82Value;                                                        //Reg0x82
            data[6] = 0x83;
            data[7] = Reg83Value;                                                        //Reg0x83

            //back up to register 
            /* bit5 & bit6 & bit7 of 0x80 */
            bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
            Reg80Value &= ~bit_op_mask;
            Reg80Value |= data[1];

            /* bit0 of 0x81 */
            bit_op_mask = bit0_Mask;
            Reg81Value &= ~bit_op_mask;
            Reg81Value |= data[3];

            //if (!RegisterWrite(wrNum, data))
            //    DisplayOperateMes("Register write failed!", Color.Red);

            //EnterNomalMode();
            //txt_PresetVoutIP_PreT.Text = AverageVout().ToString("F3");

            //oneWrie_device.ADCSigPathSet(OneWireInterface.ADCControlCommand.ADC_VOUT_WITH_CAP);
        }

        private void cmb_Module_EngT_SelectedIndexChanged(object sender, EventArgs e)
        {
            ModuleTypeIndex = (sender as ComboBox).SelectedIndex;

            //if (ModuleTypeIndex == 2)
            //{
            //    TargetOffset = 1.65;
            //    saturationVout = 3.25;
            //}
            //else if (ModuleTypeIndex == 1 )
            //{
            //    TargetOffset = 2.5;
            //    saturationVout = 4.9;
            //}
            //else 
            //{
            //    //TargetOffset = 2.5;
            //    saturationVout = 4.9;
            //}
        }

        private void numUD_SlopeK_ValueChanged(object sender, EventArgs e)
        {
            this.k_slope = (double)this.numUD_SlopeK.Value;
        }

        private void numUD_OffsetB_ValueChanged(object sender, EventArgs e)
        {
            this.b_offset = (double)this.numUD_OffsetB.Value;
        }

        private void txt_IP_EngT_TextChanged(object sender, EventArgs e)
        {
            string temp;
            try
            {
                temp = (sender as TextBox).Text;
                this.IP = double.Parse(temp); 
            }
            catch
            {
                temp = string.Format("IP set failed, will use default value {0}", this.IP);
                DisplayOperateMes(temp, Color.Red);
            }
            finally
            {
                this.IP = this.IP;  //force update GUI
            }

            TargetGain_customer = targetVoltage_customer * 1000d / IP;
        }

        private void cmb_SensitivityAdapt_PreT_SelectedIndexChanged(object sender, EventArgs e)
        {
            /* bit0 & bit1 of 0x83 */
            bit_op_mask = bit0_Mask | bit1_Mask;
            uint[] valueTable = new uint[3]
            {
                0x0,
                0x03,
                0x02
            };

            int ix_TableStart = this.cmb_SensitivityAdapt_PreT.SelectedIndex;
            //back up to register and update GUI
            Reg83Value &= ~bit_op_mask;
            Reg83Value |= valueTable[ix_TableStart];
            this.txt_SensitivityAdapt_AutoT.Text = this.cmb_SensitivityAdapt_PreT.SelectedItem.ToString();
        }

        private void cmb_TempCmp_PreT_SelectedIndexChanged(object sender, EventArgs e)
        {
            /* bit4 & bit5 & bit6 of 0x81 */
            bit_op_mask = bit4_Mask | bit5_Mask | bit6_Mask;
            uint[] valueTable = new uint[8]
            {
                0x0,
                0x10,
                0x20,
                0x30,
                0x40,
                0x50,
                0x60,
                0x70
            };

            int ix_TableStart = this.cmb_TempCmp_PreT.SelectedIndex;
            //back up to register and update GUI
            Reg81Value &= ~bit_op_mask;
            Reg81Value |= valueTable[ix_TableStart];            
            this.txt_TempComp_AutoT.Text = this.cmb_TempCmp_PreT.SelectedItem.ToString();
        }

        private void cmb_IPRange_PreT_SelectedIndexChanged(object sender, EventArgs e)
        {
            /* bit7 of 0x82 and 0x83 */
            bit_op_mask = bit7_Mask | bit6_Mask;
            uint[] valueTable = new uint[10]
            {
                0x0,0x0,
                0x0,0x40,
                0x0,0x80,
                0x0,0xC0,
                0x80,0x0 
            };

            int ix_TableStart = this.cmb_IPRange_PreT.SelectedIndex * 2;
            //back up to register and update GUI
            Reg82Value &= ~bit7_Mask;
            Reg82Value |= valueTable[ix_TableStart];
            Reg83Value &= ~bit_op_mask;
            Reg83Value |= valueTable[ix_TableStart + 1];
            this.txt_IPRange_AutoT.Text = this.cmb_IPRange_PreT.SelectedItem.ToString();
        }

        private void cmb_SensingDirection_EngT_SelectedIndexChanged(object sender, EventArgs e)
        {
            /* bit5 & bit6 of 0x82 */
            bit_op_mask = bit5_Mask | bit6_Mask;
            uint[] valueTable = new uint[4]
            {
                0x0,
                0x20,
                0x40,
                0x60
            };

            int ix_TableStart = this.cmb_SensingDirection_EngT.SelectedIndex;
            //back up to register and update GUI
            Reg82Value &= ~bit_op_mask;
            Reg82Value |= valueTable[ix_TableStart];
        }

        private void cmb_OffsetOption_EngT_SelectedIndexChanged(object sender, EventArgs e)
        {
            /* bit3 & bit4 of 0x82 */
            bit_op_mask = bit3_Mask | bit4_Mask;
            uint[] valueTable = new uint[4]
            {
                0x0,
                0x08,
                0x10,
                0x18
            };

            int ix_TableStart = this.cmb_OffsetOption_EngT.SelectedIndex;
            //back up to register and update GUI
            Reg82Value &= ~bit_op_mask;
            Reg82Value |= valueTable[ix_TableStart];        //Reg0x82
        }

        private void cmb_PolaritySelect_EngT_SelectedIndexChanged(object sender, EventArgs e)
        {
            /* bit1 & bit2 of 0x81 */
            bit_op_mask = bit1_Mask | bit2_Mask;
            uint[] valueTable = new uint[3]
            {
                0x0,
                0x04,
                0x06
            };

            int ix_TableStart = this.cmb_PolaritySelect_EngT.SelectedIndex;
            //back up to register and update GUI
            Reg81Value &= ~bit_op_mask;
            Reg81Value |= valueTable[ix_TableStart];        //Reg0x81
        }

        private void cmb_SocketType_AutoT_SelectedIndexChanged(object sender, EventArgs e)
        {
            ProductType = this.cb_ProductSeries_AutoTab.SelectedIndex;
            //if (ProductType == 0)
            //    DisplayOperateMes("SL610 Single End");
            //else if (ProductType == 1)
            //    DisplayOperateMes("SL610 Differential");
            if (ProductType == 0)
                DisplayOperateMes("SL622 Single End");
            else if (ProductType == 1)
                DisplayOperateMes("SL622 Differential");
            else if (ProductType == 2)
                DisplayOperateMes("SC780");
            else if (ProductType == 3)
                DisplayOperateMes("SC810");
            else if (ProductType == 4)
                DisplayOperateMes("SC813");
            else if (ProductType == 5)
                DisplayOperateMes("SC820");
            else if (ProductType == 6)
                DisplayOperateMes("SL62xA");
            else
                DisplayOperateMes("Invalid Socket Type", Color.DarkRed); ;
        }

        private void rbtn_VoutOptionHigh_EngT_CheckedChanged(object sender, EventArgs e)
        {
            /* bit6 of 0x83 */
            //bit_op_mask = bit6_Mask;
            //Reg83Value &= ~bit_op_mask;
            //if (this.rbtn_VoutOptionHigh_EngT.Checked)
            //{
            //    Reg83Value |= 0x40;
            //}
            //else
            //{
            //    Reg83Value |= 0x0;
            //}
        }

        private void rbtn_InsideFilterOff_EngT_CheckedChanged(object sender, EventArgs e)
        {
            /* bit3 of 0x81 */
            bit_op_mask = bit3_Mask;
            Reg81Value &= ~bit_op_mask;
            if(rbtn_InsideFilterOff_EngT.Checked)
            {
                Reg81Value |= 0x08;
            }
            else
            {
                Reg81Value |= 0x0;
            }
        }
        
        private void btn_SaveConfig_PreT_Click(object sender, EventArgs e)
        {
            try
            {
                SaveFileDialog ofd = new SaveFileDialog();
                ofd.Title = "Please Select File Localtion....";
                //ofd.Filter = @"Config|*.cfg|Config";
                ofd.InitialDirectory = System.Windows.Forms.Application.StartupPath;
                //ofd.Multiselect = true;
                ofd.ShowDialog();
                string path = ofd.FileName;

                //string filename = System.Windows.Forms.Application.StartupPath;;
                //filename += @"\config.cfg";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    StreamWriter sw = File.CreateText(path);
                    sw.WriteLine("/* Current Sensor Console configs, CopyRight of SenkoMicro, Inc */");
                
                /* ******************************************************
                 * module type, Current Range, Sensitivity adapt, Temprature Cmp, and preset gain 
                 * combobox type: name|combobox index|selected item text
                 * preset gain: name|index in table|percentage
                 *******************************************************/
                string msg;
                // module type: 
                msg = string.Format("module type|{0}|{1}",
                    this.cmb_Module_PreT.SelectedIndex.ToString(), this.cmb_Module_PreT.SelectedItem.ToString());
                sw.WriteLine(msg);

                // Current Range
                msg = string.Format("IP Range|{0}|{1}",
                    this.cmb_IPRange_PreT.SelectedIndex.ToString(), this.cmb_IPRange_PreT.SelectedItem.ToString());
                sw.WriteLine(msg);

                // Sensitivity Adapt
                msg = string.Format("Sensitivity Adapt|{0}|{1}",
                    this.cmb_SensitivityAdapt_PreT.SelectedIndex.ToString(), this.cmb_SensitivityAdapt_PreT.SelectedItem.ToString());
                sw.WriteLine(msg);

                // Temprature Compensation
                msg = string.Format("Temprature Compensation|{0}|{1}",
                    this.cmb_TempCmp_PreT.SelectedIndex.ToString(), this.cmb_TempCmp_PreT.SelectedItem.ToString());
                sw.WriteLine(msg);

                // Chosen Gain
                msg = string.Format("Preset Gain|{0}|{1}",
                    this.Ix_ForRoughGainCtrl.ToString(), RoughTable_Customer[0][Ix_ForRoughGainCtrl].ToString("F2"));
                sw.WriteLine(msg);

                // Target Voltage
                msg = string.Format("Target Voltage|{0}",
                    this.txt_targetvoltage_PreT.Text );
                sw.WriteLine(msg);

                // IP
                msg = string.Format("IP|{0}",
                    this.txt_IP_PreT.Text );
                sw.WriteLine(msg);

                // ADC Offset
                msg = string.Format("ADC Offset|{0}",
                    this.txt_AdcOffset_PreT.Text);
                sw.WriteLine(msg);

                // Vout @ 0A
                msg = string.Format("Voffset|{0}|{1}",
                    this.cmb_Voffset_PreT.SelectedIndex.ToString(), this.txt_VoutOffset_AutoT.Text);
                    //this.cmb_Voffset_PreT.SelectedIndex.ToString(), this.cmb_Voffset_PreT.SelectedItem.ToString());
                sw.WriteLine(msg);

                // bin2 accuracy
                msg = string.Format("bin2 accuracy|{0}",
                    this.txt_bin2accuracy_PreT.Text);
                sw.WriteLine(msg);

                // bin3 accuracy
                msg = string.Format("bin3 accuracy|{0}",
                    this.txt_bin3accuracy_PreT.Text);
                sw.WriteLine(msg);

                // Tab visible code
                msg = string.Format("TVC|{0}",
                    this.uTabVisibleCode);
                sw.WriteLine(msg);

                // MRE display or not
                msg = string.Format("MRE|{0}",
                    Convert.ToUInt32(bMRE));
                sw.WriteLine(msg);

                // MASK or NOT
                msg = string.Format("MASK|{0}",
                    Convert.ToUInt32(bMASK));
                sw.WriteLine(msg);

                // SAFETY READ or NOT
                msg = string.Format("SAFEREAD|{0}",
                    Convert.ToUInt32(bSAFEREAD));
                sw.WriteLine(msg);

                // Senseing Directon
                msg = string.Format("Sensing Direction |{0}|{1}",
                    this.cmb_PreTrim_SensorDirection.SelectedIndex.ToString(), this.cmb_PreTrim_SensorDirection.SelectedItem.ToString());
                sw.WriteLine(msg);
                
                //vout capture latency of IP ON
                msg = string.Format("Delay | {0}", this.txt_Delay_PreT.Text);
                sw.WriteLine(msg);

                /*********************************************
                 *  new for SL620 silicon a and b
                 *  Hao.Ding
                 *  2017/09/19
                 *********************************************/

                //Product name
                msg = string.Format("Product Name | {0}", this.cb_ProductSeries_AutoTab.SelectedIndex);
                sw.WriteLine(msg);

                //Program Mode
                msg = string.Format("Program Mode | {0}", this.cmb_ProgramMode_AutoT.SelectedIndex);
                sw.WriteLine(msg);

                //Test after Trim
                msg = string.Format("Test after Trim | {0}", this.cb_AutoTab_Retest.SelectedIndex);
                sw.WriteLine(msg);

                //Cust TC for 620a/b
                msg = string.Format("Cust TC |{0}", this.txt_SL620TC_AutoTab.Text);
                sw.WriteLine(msg);

                //iHall Setting, gain -17%, -33%, +17%
                msg = string.Format("iHall -33% | {0}", this.cb_iHallOption_AutoTab.SelectedIndex);
                sw.WriteLine(msg);

                //S2_Double Setting, gain * 2
                msg = string.Format("S2_Double | {0}", this.cb_s2double_AutoTab.Checked);
                sw.WriteLine(msg);

                //Chop_CK_Disable Setting, gain * 0.5
                msg = string.Format("Chop_Ck_Dis | {0}", this.cb_ChopCkDis_AutoTab.Checked);
                sw.WriteLine(msg);

                //byPass iQ measurment
                msg = string.Format("Bypass iQ Measurement | {0}", this.cb_MeasureiQ_AutoTab.Checked);
                sw.WriteLine(msg);

                //S3_Out_Drv, gian+25%
                msg = string.Format("S3_Out_Drv | {0}", this.cb_s3drv_autoTab.Checked);
                sw.WriteLine(msg);

                //byPass Fuse
                msg = string.Format("Bypass Fuse | {0}", this.cb_BypFuse_AutoTab.Checked);
                sw.WriteLine(msg);

                sw.Close();

                }
                else
                    return;
            }
            catch
            {
                MessageBox.Show("Save config file failed!");
            }
        }

        private void btn_loadconfig_AutoT_Click(object sender, EventArgs e)
        {
            try
            {
                string filename = "";
                OpenFileDialog fdlg = new OpenFileDialog();
                fdlg.Title = "Open Config File";
                fdlg.InitialDirectory = System.Windows.Forms.Application.StartupPath;
                fdlg.Filter = "Config File (*.cfg)|*.cfg";
                /* 
                 * FilterIndex 属性用于选择了何种文件类型,缺省设置为0,系统取Filter属性设置第一项 
                 * ,相当于FilterIndex 属性设置为1.如果你编了3个文件类型，当FilterIndex ＝2时是指第2个. 
                 */
                //fdlg.FilterIndex = 2;
                /* 
                 *如果值为false，那么下一次选择文件的初始目录是上一次你选择的那个目录， 
                 *不固定；如果值为true，每次打开这个对话框初始目录不随你的选择而改变，是固定的   
                 */
                fdlg.RestoreDirectory = true;
                if (fdlg.ShowDialog() == DialogResult.OK)
                {
                    filename = System.IO.Path.GetFileName(fdlg.FileName);
                    lb_ProductName_AutoTab.Text = System.IO.Path.GetFileNameWithoutExtension(fdlg.FileName);
                }
                else
                    return;

                //string filename = System.Windows.Forms.Application.StartupPath;
                //filename += @"\config.cfg";

                StreamReader sr = new StreamReader(filename);
                string comment = sr.ReadLine();
                string[] msg;
                int ix;
                /* ******************************************************
                 * module type, Current Range, Sensitivity adapt, Temprature Cmp, and preset gain 
                 * combobox type: name|combobox index|selected item text
                 * preset gain: name|index in table|percentage
                 *******************************************************/

                //Product Series
                msg = sr.ReadLine().Split("|".ToArray());
                this.cb_ProductSeries_AutoTab.SelectedIndex = int.Parse(msg[1]);

                //// module type
                //msg = sr.ReadLine().Split("|".ToCharArray());
                //ix = int.Parse(msg[1]);
                //this.cmb_Module_PreT.SelectedIndex = ix;
                //this.txt_ModuleType_AutoT.Text = msg[2];

                //// IP Range
                //msg = sr.ReadLine().Split("|".ToCharArray());
                //ix = int.Parse(msg[1]);
                //this.cmb_IPRange_PreT.SelectedIndex = ix;

                //// Sensitivity adapt
                //msg = sr.ReadLine().Split("|".ToCharArray());
                //ix = int.Parse(msg[1]);
                //this.cmb_SensitivityAdapt_PreT.SelectedIndex = ix;

                //// Temprature Compensation
                //msg = sr.ReadLine().Split("|".ToCharArray());
                //ix = int.Parse(msg[1]);
                //this.cmb_TempCmp_PreT.SelectedIndex = ix;

                // Preset Gain
                msg = sr.ReadLine().Split("|".ToCharArray());
                preSetCoareseGainCode = uint.Parse(msg[1]);
                //preSetCoareseGainCode = uint.Parse(msg[1]);

                // Target Voltage
                msg = sr.ReadLine().Split("|".ToCharArray());
                //ix = int.Parse(msg[1]);
                this.txt_TargertVoltage_AutoT.Text = msg[1];

                // IP
                msg = sr.ReadLine().Split("|".ToCharArray());
                //ix = int.Parse(msg[1]);
                this.txt_IP_AutoT.Text = msg[1];

                // ADC Offset
                msg = sr.ReadLine().Split("|".ToCharArray());
                //ix = int.Parse(msg[1]);
                this.txt_AdcOffset_AutoT.Text = msg[1];
                AdcOffset = double.Parse(msg[1]);

                // Vout @ 0A
                msg = sr.ReadLine().Split("|".ToCharArray());
                ix = int.Parse(msg[1]);
                this.cb_V0AOption_AutoTab.SelectedIndex = ix;
                this.txt_VoutOffset_AutoT.Text = msg[2];

                // bin2 accuracy
                msg = sr.ReadLine().Split("|".ToCharArray());
                //ix = int.Parse(msg[1]);
                bin2accuracy = double.Parse(msg[1]);
                //this.txt_bin2accuracy_PreT.Text = msg[1];
                this.txt_BinError_AutoT.Text = bin2accuracy.ToString();

                //// bin3 accuracy
                //msg = sr.ReadLine().Split("|".ToCharArray());
                ////ix = int.Parse(msg[1]);
                //bin3accuracy = double.Parse(msg[1]);
                ////this.txt_bin3accuracy_PreT.Text = msg[1];
                //this.txt_bin3accuracy_PreT.Text = bin3accuracy.ToString();

                // Tab visible code
                msg = sr.ReadLine().Split("|".ToCharArray());
                //ix = int.Parse(msg[1]);
                uTabVisibleCode = uint.Parse(msg[1]);

                ////MRE diapaly or not
                //msg = sr.ReadLine().Split("|".ToCharArray());
                //bMRE = Convert.ToBoolean(uint.Parse(msg[1]));

                ////MASK or NOT
                //msg = sr.ReadLine().Split("|".ToCharArray());
                //bMASK = Convert.ToBoolean(uint.Parse(msg[1]));

                ////SAFETY READ or NOT
                //msg = sr.ReadLine().Split("|".ToCharArray());
                //bSAFEREAD = Convert.ToBoolean(uint.Parse(msg[1]));

                // Sensing Direction
                msg = sr.ReadLine().Split("|".ToCharArray());
                //ix = bool.Parse(msg[1]);
                if (msg[1] == "True")
                    this.cb_InvertSens_AutoTab.Checked = true;
                else if(ix == 1)
                    this.cb_InvertSens_AutoTab.Checked = false;
                //this.cmb_PreTrim_SensorDirection.SelectedIndex = ix;

                // Delay
                msg = sr.ReadLine().Split("|".ToArray());
                Delay_Fuse = int.Parse(msg[1]);
                this.txt_IpDelay_AutoT.Text = double.Parse(msg[1]).ToString();

                /*********************************************
                 *  new for SL620 silicon a and b
                 *  Hao.Ding
                 *  2017/09/19
                 *********************************************/

                

                //Program mode
                msg = sr.ReadLine().Split("|".ToArray());
                this.cmb_ProgramMode_AutoT.SelectedIndex = int.Parse(msg[1]);

                //Test after Trim
                msg = sr.ReadLine().Split("|".ToArray());
                this.cb_AutoTab_Retest.SelectedIndex = int.Parse(msg[1]);

                //Custmize TC
                msg = sr.ReadLine().Split("|".ToArray());
                this.cb_CustTc_AutoTab.Checked = bool.Parse(msg[1]);

                //TC value
                msg = sr.ReadLine().Split("|".ToArray());
                this.txt_SL620TC_AutoTab.Text = msg[1];

                //iHall Setting, gain -33%
                msg = sr.ReadLine().Split("|".ToArray());
                this.cb_iHallOption_AutoTab.SelectedIndex = int.Parse( msg[1] );

                //S2_Double Setting, gain * 2
                msg = sr.ReadLine().Split("|".ToArray());
                this.cb_s2double_AutoTab.Checked = bool.Parse(msg[1]);

                //Chop_CK_Disable Setting, gain * 0.5
                msg = sr.ReadLine().Split("|".ToArray());
                this.cb_ChopCkDis_AutoTab.Checked = bool.Parse(msg[1]);

                //byPass iQ measurment
                msg = sr.ReadLine().Split("|".ToArray());
                this.cb_MeasureiQ_AutoTab.Checked = bool.Parse(msg[1]);

                //S3_Out_Drv, gian+25%
                msg = sr.ReadLine().Split("|".ToArray());
                this.cb_s3drv_autoTab.Checked = bool.Parse(msg[1]);

                //byPass Fuse
                msg = sr.ReadLine().Split("|".ToArray());
                this.cb_BypFuse_AutoTab.Checked = bool.Parse(msg[1]);

                //8x Halls
                msg = sr.ReadLine().Split("|".ToArray());
                this.cb_8xHalls_AutoTab.Checked = bool.Parse(msg[1]);

                sr.Close();

                //Backup value for autotrim
                //StoreRegValue();
            }
            catch
            {
                MessageBox.Show("Load config file failed, please choose correct file!");
            }
        }

        private void btn_SaveConfig_AutoT_Click(object sender, EventArgs e)
        {
            try
            {
                string path = "";
                SaveFileDialog ofd = new SaveFileDialog();
                ofd.Title = "Please Select File Localtion....";
                ofd.Filter = "Config File (*.cfg)|*.cfg";
                ofd.InitialDirectory = System.Windows.Forms.Application.StartupPath;
                ofd.RestoreDirectory = true;
                //ofd.Multiselect = true;
                ofd.ShowDialog();

                path = ofd.FileName;

                //string filename = System.Windows.Forms.Application.StartupPath;;
                //filename += @"\config.cfg";
                //if (ofd.ShowDialog() == DialogResult.OK)
                //{
                StreamWriter sw = File.CreateText(path);
                sw.WriteLine("/* Current Sensor Console configs, CopyRight of SenkoMicro, Inc */");

                /* ******************************************************
                 * module type, Current Range, Sensitivity adapt, Temprature Cmp, and preset gain 
                 * combobox type: name|combobox index|selected item text
                 * preset gain: name|index in table|percentage
                 *******************************************************/
                string msg;
                //// module type: 
                //msg = string.Format("module type|{0}|{1}",
                //    this.cmb_Module_PreT.SelectedIndex.ToString(), this.cmb_Module_PreT.SelectedItem.ToString());
                //sw.WriteLine(msg);

                //// Current Range
                //msg = string.Format("IP Range|{0}|{1}",
                //    this.cmb_IPRange_PreT.SelectedIndex.ToString(), this.cmb_IPRange_PreT.SelectedItem.ToString());
                //sw.WriteLine(msg);

                //// Sensitivity Adapt
                //msg = string.Format("Sensitivity Adapt|{0}|{1}",
                //    this.cmb_SensitivityAdapt_PreT.SelectedIndex.ToString(), this.cmb_SensitivityAdapt_PreT.SelectedItem.ToString());
                //sw.WriteLine(msg);

                //// Temprature Compensation
                //msg = string.Format("Temprature Compensation|{0}|{1}",
                //    this.cmb_TempCmp_PreT.SelectedIndex.ToString(), this.cmb_TempCmp_PreT.SelectedItem.ToString());
                //sw.WriteLine(msg);

                //Product name
                msg = string.Format("Product Name|{0}|{1}", this.cb_ProductSeries_AutoTab.SelectedIndex, this.cb_ProductSeries_AutoTab.SelectedItem.ToString());
                sw.WriteLine(msg);

                // Chosen Gain
                msg = string.Format("Preset Gain|{0}", this.preSetCoareseGainCode.ToString());
                sw.WriteLine(msg);

                // Target Voltage
                msg = string.Format("Target Voltage|{0}", this.txt_TargertVoltage_AutoT.Text);
                sw.WriteLine(msg);

                // IP
                msg = string.Format("IP|{0}", this.txt_IP_AutoT.Text);
                sw.WriteLine(msg);

                // ADC Offset
                msg = string.Format("ADC Offset|{0}", this.txt_AdcOffset_AutoT.Text);
                sw.WriteLine(msg);

                // Vout @ 0A
                msg = string.Format("Voffset|{0}|{1}",
                    this.cb_V0AOption_AutoTab.SelectedIndex.ToString(), this.txt_VoutOffset_AutoT.Text);
                sw.WriteLine(msg);

                // bin2 accuracy
                msg = string.Format("Bin Error|{0}",
                    this.txt_BinError_AutoT.Text);
                sw.WriteLine(msg);

                //// bin3 accuracy
                //msg = string.Format("bin3 accuracy|{0}",
                //    this.txt_bin3accuracy_PreT.Text);
                //sw.WriteLine(msg);

                // Tab visible code
                msg = string.Format("TVC|{0}",
                    this.uTabVisibleCode);
                sw.WriteLine(msg);

                //// MRE display or not
                //msg = string.Format("MRE|{0}",
                //    Convert.ToUInt32(bMRE));
                //sw.WriteLine(msg);

                //// MASK or NOT
                //msg = string.Format("MASK|{0}",
                //    Convert.ToUInt32(bMASK));
                //sw.WriteLine(msg);

                //// SAFETY READ or NOT
                //msg = string.Format("SAFEREAD|{0}",
                //    Convert.ToUInt32(bSAFEREAD));
                //sw.WriteLine(msg);

                // Senseing Directon
                msg = string.Format("Invert Sensing Direction|{0}", this.cb_InvertSens_AutoTab.Checked);
                sw.WriteLine(msg);

                //vout capture latency of IP ON
                msg = string.Format("Delay|{0}", this.txt_IpDelay_AutoT.Text);
                sw.WriteLine(msg);

                /*********************************************
                 *  new for SL620 silicon a and b
                 *  Hao.Ding
                 *  2017/09/19
                 *********************************************/



                //Program Mode
                msg = string.Format("Program Mode|{0}|{1}", this.cmb_ProgramMode_AutoT.SelectedIndex, this.cmb_ProgramMode_AutoT.SelectedItem.ToString());
                sw.WriteLine(msg);

                //Test after Trim
                msg = string.Format("Test after Trim|{0}|{1}", this.cb_AutoTab_Retest.SelectedIndex, this.cb_AutoTab_Retest.SelectedItem.ToString());
                sw.WriteLine(msg);

                //Cust TC
                msg = string.Format("Custmized TC|{0}", this.cb_CustTc_AutoTab.Checked);
                sw.WriteLine(msg);

                //TC Value
                msg = string.Format("TC Value|{0}", this.txt_SL620TC_AutoTab.Text);
                sw.WriteLine(msg);

                //iHall Setting, gain -17%, -33%, +17%
                msg = string.Format("iHall Option|{0}|{1}", this.cb_iHallOption_AutoTab.SelectedIndex, this.cb_iHallOption_AutoTab.SelectedItem.ToString());
                sw.WriteLine(msg);

                //S2_Double Setting, gain * 2
                msg = string.Format("S2_Double|{0}", this.cb_s2double_AutoTab.Checked);
                sw.WriteLine(msg);

                //Chop_CK_Disable Setting, gain * 0.5
                msg = string.Format("Chop_Ck_Dis|{0}", this.cb_ChopCkDis_AutoTab.Checked);
                sw.WriteLine(msg);

                //byPass iQ measurment
                msg = string.Format("Bypass iQ Measurement|{0}", this.cb_MeasureiQ_AutoTab.Checked);
                sw.WriteLine(msg);

                //S3_Out_Drv, gian+25%
                msg = string.Format("S3_Out_Drv|{0}", this.cb_s3drv_autoTab.Checked);
                sw.WriteLine(msg);

                //byPass Fuse
                msg = string.Format("Bypass Fuse|{0}", this.cb_BypFuse_AutoTab.Checked);
                sw.WriteLine(msg);

                //8x Halls
                msg = string.Format("8x Halls|{0}", this.cb_8xHalls_AutoTab.Checked);
                sw.WriteLine(msg);

                //Big Cap
                msg = string.Format("Big Cap|{0}", this.cb_BigCap_AutoTab.Checked);
                sw.WriteLine(msg);

                //Fast Start up
                msg = string.Format("Fast Start up|{0}", this.cb_FastStart_AutoTab.Checked);
                sw.WriteLine(msg);

                sw.Close();

                //}
                //else
                //return;
            }
            catch
            {
                MessageBox.Show("Save config file failed!");
            }
        }

        private void initConfigFile()
        {
            try
            {
                string filename = System.Windows.Forms.Application.StartupPath;
                filename += @"\config.cfg";

                StreamReader sr = new StreamReader(filename);
                string comment = sr.ReadLine();
                string[] msg;
                int ix;
                /* ******************************************************
                 * module type, Current Range, Sensitivity adapt, Temprature Cmp, and preset gain 
                 * combobox type: name|combobox index|selected item text
                 * preset gain: name|index in table|percentage
                 *******************************************************/
                // module type
                msg = sr.ReadLine().Split("|".ToCharArray());
                ix = int.Parse(msg[1]);
                this.cmb_Module_PreT.SelectedIndex = ix;
                this.txt_ModuleType_AutoT.Text = msg[2];

                // IP Range
                msg = sr.ReadLine().Split("|".ToCharArray());
                ix = int.Parse(msg[1]);
                this.cmb_IPRange_PreT.SelectedIndex = ix;

                // Sensitivity adapt
                msg = sr.ReadLine().Split("|".ToCharArray());
                ix = int.Parse(msg[1]);
                this.cmb_SensitivityAdapt_PreT.SelectedIndex = ix;

                // Temprature Compensation
                msg = sr.ReadLine().Split("|".ToCharArray());
                ix = int.Parse(msg[1]);
                this.cmb_TempCmp_PreT.SelectedIndex = ix;

                // Preset Gain
                msg = sr.ReadLine().Split("|".ToCharArray());
                Ix_ForRoughGainCtrl = uint.Parse(msg[1]);
                //preSetCoareseGainCode = uint.Parse(msg[1]);

                // Target Voltage
                msg = sr.ReadLine().Split("|".ToCharArray());
                //ix = int.Parse(msg[1]);
                this.txt_targetvoltage_PreT.Text = msg[1];

                // IP
                msg = sr.ReadLine().Split("|".ToCharArray());
                //ix = int.Parse(msg[1]);
                this.txt_IP_PreT.Text = msg[1];

                // ADC Offset
                msg = sr.ReadLine().Split("|".ToCharArray());
                //ix = int.Parse(msg[1]);
                this.txt_AdcOffset_AutoT.Text = msg[1];
                AdcOffset = double.Parse(msg[1]);

                // Vout @ 0A
                msg = sr.ReadLine().Split("|".ToCharArray());
                ix = int.Parse(msg[1]);
                this.cmb_Voffset_PreT.SelectedIndex = ix;
                this.txt_VoutOffset_AutoT.Text = msg[2];

                // bin2 accuracy
                msg = sr.ReadLine().Split("|".ToCharArray());
                //ix = int.Parse(msg[1]);
                bin2accuracy = double.Parse(msg[1]);
                //this.txt_bin2accuracy_PreT.Text = msg[1];
                this.txt_bin2accuracy_PreT.Text = bin2accuracy.ToString();

                // bin3 accuracy
                msg = sr.ReadLine().Split("|".ToCharArray());
                //ix = int.Parse(msg[1]);
                bin3accuracy = double.Parse(msg[1]);
                //this.txt_bin3accuracy_PreT.Text = msg[1];
                this.txt_bin3accuracy_PreT.Text = bin3accuracy.ToString();

                // Tab visible code
                msg = sr.ReadLine().Split("|".ToCharArray());
                //ix = int.Parse(msg[1]);
                uTabVisibleCode = uint.Parse(msg[1]);

                //MRE diapaly or not
                msg = sr.ReadLine().Split("|".ToCharArray());
                bMRE = Convert.ToBoolean(uint.Parse(msg[1]));

                //MASK or NOT
                msg = sr.ReadLine().Split("|".ToCharArray());
                bMASK = Convert.ToBoolean(uint.Parse(msg[1]));

                //SAFETY READ or NOT
                msg = sr.ReadLine().Split("|".ToCharArray());
                bSAFEREAD = Convert.ToBoolean(uint.Parse(msg[1]));

                // Sensing Direction
                msg = sr.ReadLine().Split("|".ToCharArray());
                ix = int.Parse(msg[1]);
                if (ix == 0)
                    this.cmb_SensingDirection_EngT.SelectedIndex = 0;
                else if (ix == 1)
                    this.cmb_SensingDirection_EngT.SelectedIndex = 2;
                this.cmb_PreTrim_SensorDirection.SelectedIndex = ix;

                // Delay
                msg = sr.ReadLine().Split("|".ToArray());
                Delay_Fuse = int.Parse(msg[1]);
                this.txt_Delay_PreT.Text = double.Parse(msg[1]).ToString();

                /*********************************************
                 *  new for SL620 silicon a and b
                 *  Hao.Ding
                 *  2017/09/19
                 *********************************************/

                //Product Series
                msg = sr.ReadLine().Split("|".ToArray());
                this.cb_ProductSeries_AutoTab.SelectedIndex = int.Parse(msg[1]);

                //Program mode
                msg = sr.ReadLine().Split("|".ToArray());
                this.cmb_ProgramMode_AutoT.SelectedIndex = int.Parse(msg[1]);

                //Test after Trim
                msg = sr.ReadLine().Split("|".ToArray());
                this.cb_AutoTab_Retest.SelectedIndex = int.Parse(msg[1]);

                //Cust TC
                msg = sr.ReadLine().Split("|".ToArray());
                this.txt_SL620TC_AutoTab.Text = msg[1];

                //iHall Setting, gain -33%
                msg = sr.ReadLine().Split("|".ToArray());
                this.cb_iHallOption_AutoTab.SelectedIndex = int.Parse(msg[1]);

                //S2_Double Setting, gain * 2
                msg = sr.ReadLine().Split("|".ToArray());
                this.cb_s2double_AutoTab.Checked = bool.Parse(msg[1]);

                //Chop_CK_Disable Setting, gain * 0.5
                msg = sr.ReadLine().Split("|".ToArray());
                this.cb_ChopCkDis_AutoTab.Checked = bool.Parse(msg[1]);

                //byPass iQ measurment
                msg = sr.ReadLine().Split("|".ToArray());
                this.cb_MeasureiQ_AutoTab.Checked = bool.Parse(msg[1]);

                //S3_Out_Drv, gian+25%
                msg = sr.ReadLine().Split("|".ToArray());
                this.cb_s3drv_autoTab.Checked = bool.Parse(msg[1]);

                //byPass Fuse
                msg = sr.ReadLine().Split("|".ToArray());
                this.cb_BypFuse_AutoTab.Checked = bool.Parse(msg[1]);

                sr.Close();

                //Backup value for autotrim
                StoreRegValue();
            }
            catch
            {
                MessageBox.Show("Load config file failed, please choose correct file!");
            }
        }
       
        private void txt_targetvoltage_PreT_TextChanged(object sender, EventArgs e)
        {
            //targetVoltage_customer = double.Parse((sender as TextBox).Text);
            //TargetGain_customer = (targetVoltage_customer * 2000d) / IP;

            try
            {
                //temp = (4500d - 2000d) / double.Parse(this.txt_TargetGain.Text);
                if ((sender as TextBox).Text.ToCharArray()[(sender as TextBox).Text.Length - 1].ToString() == ".")
                    return;
                TargetVoltage_customer = double.Parse((sender as TextBox).Text);
                //TargetGain_customer = (double.Parse((sender as TextBox).Text) * 2000d)/IP;
            }
            catch
            {
                string tempStr = string.Format("Target voltage set failed, will use default value {0}", this.TargetVoltage_customer);
                DisplayOperateMes(tempStr, Color.Red);
            }
            finally
            {
                //TargetVoltage_customer = TargetVoltage_customer;      //Force to update text to default.
            }

            TargetGain_customer = (TargetVoltage_customer * 1000d) / IP;
        }

        private void txt_ChosenGain_PreT_TextChanged(object sender, EventArgs e)
        {
            //data[1] = Convert.ToUInt32(RoughTable_Customer[1][Ix_ForRoughGainCtrl]);     //Reg0x80
            //data[3] = Convert.ToUInt32(RoughTable_Customer[2][Ix_ForRoughGainCtrl]);     //Reg0x81

            //Reset rough gain used register bits
            /* bit5 & bit6 & bit7 of 0x80 */
            bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
            Reg80Value &= ~bit_op_mask;
            Reg80Value |= Convert.ToUInt32(RoughTable_Customer[1][Ix_ForRoughGainCtrl]);     //Reg0x80[1];

            /* bit0 of 0x81 */
            bit_op_mask = bit0_Mask;
            Reg81Value &= ~bit_op_mask;
            Reg81Value |= Convert.ToUInt32(RoughTable_Customer[2][Ix_ForRoughGainCtrl]);     //Reg0x81;
        }

        private void txt_reg80_EngT_TextChanged(object sender, EventArgs e)
        {
            this.txt_Reg80_PreT.Text = this.txt_reg80_EngT.Text;
        }

        private void txt_reg81_EngT_TextChanged(object sender, EventArgs e)
        {
            this.txt_Reg81_PreT.Text = this.txt_reg81_EngT.Text;
        }

        private void txt_reg82_EngT_TextChanged(object sender, EventArgs e)
        {
            this.txt_Reg82_PreT.Text = this.txt_reg82_EngT.Text;
        }

        private void txt_reg83_EngT_TextChanged(object sender, EventArgs e)
        {
            this.txt_Reg83_PreT.Text = this.txt_reg83_EngT.Text;
        }

        private void btn_AdcOut_EngT_Click(object sender, EventArgs e)
        {
            double temp = 0;
            //oneWrie_device.ADCSigPathSet(OneWireInterface.ADCControlCommand.ADC_VIN_TO_VOUT);
            rbt_signalPathSeting_AIn_EngT.Checked = true;
            //rbt_signalPathSeting_Vout_EngT.Checked = true;
            temp = AverageVout();
            this.txt_AdcOut_EngT.Text = temp.ToString("F3");
            //Vout_0A = AverageVout();
            DisplayOperateMes("ADC Out = " + temp.ToString("F3"));
        }

        private void txt_AdcOffset_PreT_TextChanged(object sender, EventArgs e)
        {
            if ((sender as TextBox).Text.ToCharArray()[(sender as TextBox).Text.Length - 1].ToString() == ".")
                return;
            AdcOffset = double.Parse((sender as TextBox).Text);
            //AadcOffset = AadcOffset;
        }

        private void cmb_Voffset_PreT_SelectedIndexChanged(object sender, EventArgs e)
        {
            int ix = 0;
            ix = this.cmb_Voffset_PreT.SelectedIndex;
            if (ix == 0)
            {
                TargetOffset = 2.5;
                bit_op_mask = bit3_Mask | bit4_Mask;
                Reg82Value &= ~bit_op_mask;
                Reg82Value |= 0x00;        //Reg0x82
                //Reg82Value = 0x18;
                this.cmb_OffsetOption_EngT.SelectedIndex = 0;
                this.txt_VoutOffset_AutoT.Text = "2.5V";
            }
            else if (ix == 1)
            {
                TargetOffset = 2.5;
                bit_op_mask = bit3_Mask | bit4_Mask;
                Reg82Value &= ~bit_op_mask;
                Reg82Value |= 0x08;        //Reg0x82
                //Reg82Value = 0x00;
                this.cmb_OffsetOption_EngT.SelectedIndex = 1;
                this.txt_VoutOffset_AutoT.Text = "2.5V";
            }
            else if (ix == 2)
            {
                if (ModuleTypeIndex == 2)
                    TargetOffset = 1.65;
                else
                    TargetOffset = 2.5;
                bit_op_mask = bit3_Mask | bit4_Mask;
                Reg82Value &= ~bit_op_mask;
                Reg82Value |= 0x10;        //Reg0x82
                //Reg82Value = 0x00;
                this.cmb_OffsetOption_EngT.SelectedIndex = 2;
                this.txt_VoutOffset_AutoT.Text = "0.5VCC";
            }
            else if (ix == 3)
            {
                TargetOffset = 1.65;
                bit_op_mask = bit3_Mask | bit4_Mask;
                Reg82Value &= ~bit_op_mask;
                Reg82Value |= 0x18;        //Reg0x82
                //Reg82Value = 0x00;
                this.cmb_OffsetOption_EngT.SelectedIndex = 3;
                this.txt_VoutOffset_AutoT.Text = "1.65V";
            }
            else if (ix == 4)
            {
                TargetOffset = 0.5;
                this.txt_VoutOffset_AutoT.Text = "0.1VCC";
            }
        }

        private void btn_Vout_AutoT_Click(object sender, EventArgs e)
        {
            //uint uDutCount = 16;
            uint idut = 0;
            double[] uVout = new double[16];

            PowerOn();

            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);
            Delay(Delay_Sync);
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);
            Delay(Delay_Sync);
            //RePower();
            //for (idut = 0; idut < uDutCount; idut++)
            {
                //MultiSiteSocketSelect(idut);
                //Delay(Delay_Power);
                //EnterTestMode();
                //RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
                //Delay(Delay_Sync);
                //RegisterWrite(4, new uint[8] { 0x84, MultiSiteReg4[idut], 0x85, MultiSiteReg5[idut], 0x86, MultiSiteReg6[idut], 0x87, MultiSiteReg7[idut] });
                //Delay(Delay_Sync);

                //EnterNomalMode();
                Delay(300);
                uVout[idut] = AverageVout();
                DisplayOperateMes("Vout = " + uVout[idut].ToString("F3") + "V");
            }

            //MultiSiteDisplayVout(uVout);
        }

        private void btn_EngTab_Connect_Click(object sender, EventArgs e)
        {
            bool result = false;
            //#region One wire
            //if (!bUsbConnected)
            result = oneWrie_device.ConnectDevice();

            if (result)
            {
                this.toolStripStatusLabel_Connection.BackColor = Color.YellowGreen;
                this.toolStripStatusLabel_Connection.Text = "Connected";
                btn_GetFW_OneWire_Click(null, null);
                bUsbConnected = true;
            }
            else
            {
                this.toolStripStatusLabel_Connection.BackColor = Color.IndianRed;
                this.toolStripStatusLabel_Connection.Text = "Disconnected";
            }
            //#endregion

            //UART Initialization
            if (oneWrie_device.UARTInitilize(9600, 1))
                DisplayOperateMes("UART Initilize succeeded!");
            else
                DisplayOperateMes("UART Initilize failed!");
            //ding hao
            Delay(Delay_Power);
            //DisplayAutoTrimOperateMes("Delay 300ms");

            //1. Current Remote CTL
            if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_REMOTE, 0))
                DisplayOperateMes("Set Current Remote succeeded!");
            else
                DisplayOperateMes("Set Current Remote failed!");

            //Delay 300ms
            //Thread.Sleep(300);
            Delay(Delay_Power);
            //DisplayAutoTrimOperateMes("Delay 300ms");

            //2. Current On
            //if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0))
            if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, Convert.ToUInt32(IP)))
                DisplayOperateMes("Set Current to IP succeeded!");
            else
                DisplayOperateMes("Set Current to IP failed!");

            //Delay 300ms
            Delay(Delay_Power);
            //DisplayOperateMes("Delay 300ms");

            //3. Set Voltage
            if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETVOLT, 6u))
                DisplayOperateMes(string.Format("Set Voltage to {0}V succeeded!", 6));
            else
                DisplayOperateMes(string.Format("Set Voltage to {0}V failed!", 6));

            //numUD_pilotwidth_ow_ValueChanged(null,null);
            //numUD_pilotwidth_ow_ValueChanged(null,null);
            //num_UD_pulsewidth_ow_ValueChanged
        }

        private void btn_EngTab_Ipoff_Click(object sender, EventArgs e)
        {
            //Set Voltage
            //if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, 0u))
            if(oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0))
                DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", 0));
            else
            {
                DisplayOperateMes(string.Format("Set Current to {0}A failed!", 0));
            }
        }

        private void btn_EngTab_Ipon_Click(object sender, EventArgs e)
        {
            //Set Voltage
            //if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, Convert.ToUInt32(IP)))
            if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0))
                DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", IP));
            else
            {
                DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
            }
        }

        private void cmb_ProgramMode_AutoT_SelectedIndexChanged(object sender, EventArgs e)
        {
            ProgramMode = this.cmb_ProgramMode_AutoT.SelectedIndex;
            if (ProgramMode == 0)
                DisplayOperateMes("Automatic Program");
            else if (ProgramMode == 1)
                DisplayOperateMes("Manual Program");
            else if (ProgramMode == 2)
                DisplayOperateMes("DualRelay Program");
            else
                DisplayOperateMes("Invalid Program Mode", Color.DarkRed);
        }       

        private void btn_StartPoint_BrakeT_Click(object sender, EventArgs e)
        {
            double dStartPoint = 0;
            //double dStopPoint = 0;
            bool bTerminate = false;
            Ix_OffsetA_Brake = 0;
            Ix_OffsetB_Brake = 0;
            //uint[] BrakeReg = new uint[5];

            //BrakeReg = [0;0;0;0;0];

            while (!bTerminate)
            {
                RePower();
                EnterTestMode();
                BurstRead(0x80, 5, tempReadback);
                if (tempReadback[0] + tempReadback[1] + tempReadback[2] + tempReadback[3] + tempReadback[4] != 0)
                {
                    DisplayOperateMes("DUT" + " has been Blown!", Color.Red);
                    PowerOff();
                    return;
                }

                RegisterWrite(4, new uint[8] { 0x80, BrakeReg[0], 0x81, BrakeReg[1], 0x82, BrakeReg[2], 0x83, BrakeReg[3] });
                BurstRead(0x80, 5, tempReadback);
                if (tempReadback[0] != BrakeReg[0] || tempReadback[1] != BrakeReg[1] || tempReadback[2] != BrakeReg[2] || tempReadback[3] != BrakeReg[3])
                {
                    DisplayOperateMes("DUT digital communication fail!", Color.Red);
                    PowerOff();
                    return;
                }
                
                EnterNomalMode();
                dStartPoint = AverageVout();
                DisplayOperateMes("start point = " + dStartPoint.ToString("F3"));
                if (dStartPoint < 0.09)
                {
                    bTerminate = true;
                }

                if (Ix_OffsetA_Brake < 8)
                {
                    bit_op_mask = bit7_Mask;
                    BrakeReg[1] &= ~bit_op_mask;
                    BrakeReg[1] |= Convert.ToUInt32(OffsetTableA_Customer[1][Ix_OffsetA_Brake]);

                    bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask;
                    BrakeReg[2] &= ~bit_op_mask;
                    BrakeReg[2] |= Convert.ToUInt32(OffsetTableA_Customer[2][Ix_OffsetA_Brake]);

                    Ix_OffsetA_Brake++;
                }
                else if (Ix_OffsetB_Brake < 8)
                {
                    bit_op_mask = bit2_Mask | bit3_Mask | bit4_Mask | bit5_Mask;
                    BrakeReg[3] &= ~bit_op_mask;
                    BrakeReg[3] |= Convert.ToUInt32(OffsetTableB_Customer[1][Ix_OffsetB_Brake]);

                    Ix_OffsetB_Brake++;
                }
                else
                {
                    DisplayOperateMes("Unable to adjust start point!", Color.Red);
                    PowerOff();
                    bTerminate = true;
                }
            }
        }

        private void btn_StopPoint_BrakeT_Click(object sender, EventArgs e)
        {
            double dStopPoint = 0;
            bool bTerminate = false;
            Ix_GainRough_Brake = 0;
            Ix_GainPrecision_Brake = 0;
            //uint[] BrakeReg = new uint[5];
            BrakeReg[0] |= 0xE0;
            BrakeReg[1] |= 0x01;

            //BrakeReg = [0;0;0;0;0];

            while (!bTerminate)
            {
                RePower();
                EnterTestMode();
                BurstRead(0x80, 5, tempReadback);
                if (tempReadback[0] + tempReadback[1] + tempReadback[2] + tempReadback[3] + tempReadback[4] != 0)
                {
                    DisplayOperateMes("DUT" + " has been Blown!", Color.Red);
                    PowerOff();
                    return;
                }

                RegisterWrite(4, new uint[8] { 0x80, BrakeReg[0], 0x81, BrakeReg[1], 0x82, BrakeReg[2], 0x83, BrakeReg[3] });
                BurstRead(0x80, 5, tempReadback);
                if (tempReadback[0] != BrakeReg[0] || tempReadback[1] != BrakeReg[1] || tempReadback[2] != BrakeReg[2] || tempReadback[3] != BrakeReg[3])
                {
                    DisplayOperateMes("DUT digital communication fail!", Color.Red);
                    PowerOff();
                    return;
                }

                EnterNomalMode();
                dStopPoint = AverageVout();
                DisplayOperateMes("stop point = " + dStopPoint.ToString("F3"));
                if (dStopPoint >= 4.9)
                {
                    bTerminate = true;
                }

                if (Ix_GainRough_Brake < 16)
                {
                    bit_op_mask = bit7_Mask | bit6_Mask | bit5_Mask ;
                    BrakeReg[0] &= ~bit_op_mask;
                    BrakeReg[0] |= Convert.ToUInt32(RoughTable_Customer[1][Ix_GainRough_Brake]);

                    bit_op_mask = bit0_Mask;
                    BrakeReg[1] &= ~bit_op_mask;
                    BrakeReg[1] |= Convert.ToUInt32(RoughTable_Customer[2][Ix_GainRough_Brake]);

                    Ix_GainRough_Brake++;
                }
                else
                {
                    DisplayOperateMes("Unable to adjust stop point!", Color.Red);
                    PowerOff();
                    bTerminate = true;
                }
            }

            bTerminate = false;
            while (!bTerminate)
            {
                RePower();
                EnterTestMode();
                BurstRead(0x80, 5, tempReadback);
                if (tempReadback[0] + tempReadback[1] + tempReadback[2] + tempReadback[3] + tempReadback[4] != 0)
                {
                    DisplayOperateMes("DUT" + " has been Blown!", Color.Red);
                    PowerOff();
                    return;
                }

                RegisterWrite(4, new uint[8] { 0x80, BrakeReg[0], 0x81, BrakeReg[1], 0x82, BrakeReg[2], 0x83, BrakeReg[3] });
                BurstRead(0x80, 5, tempReadback);
                if (tempReadback[0] != BrakeReg[0] || tempReadback[1] != BrakeReg[1] || tempReadback[2] != BrakeReg[2] || tempReadback[3] != BrakeReg[3])
                {
                    DisplayOperateMes("DUT digital communication fail!", Color.Red);
                    PowerOff();
                    return;
                }

                EnterNomalMode();
                dStopPoint = AverageVout();
                DisplayOperateMes("stop point = " + dStopPoint.ToString("F3"));
                if (dStopPoint <= 4.9)
                {
                    bTerminate = true;
                }

                if (Ix_GainPrecision_Brake < 32)
                {
                    bit_op_mask = bit4_Mask | bit3_Mask | bit2_Mask | bit1_Mask | bit0_Mask;
                    BrakeReg[0] &= ~bit_op_mask;
                    BrakeReg[0] |= Convert.ToUInt32(PreciseTable_Customer[1][Ix_GainPrecision_Brake]);

                    Ix_GainPrecision_Brake++;
                }
                else
                {
                    DisplayOperateMes("Unable to adjust stop point!", Color.Red);
                    PowerOff();
                    bTerminate = true;
                }
            }
            Ix_GainPrecision_Brake--;
            bit_op_mask = bit4_Mask | bit3_Mask | bit2_Mask | bit1_Mask | bit0_Mask;
            BrakeReg[0] &= ~bit_op_mask;
            BrakeReg[0] |= Convert.ToUInt32(PreciseTable_Customer[1][Ix_GainPrecision_Brake]);
        }

        private void btn_Fuse_BrakeT_Click(object sender, EventArgs e)
        {
            bool bMarginal = false;

            //Fuse
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_EXT);
            RePower();
            EnterTestMode();
            RegisterWrite(5, new uint[10] { 0x80, BrakeReg[0], 0x81, BrakeReg[1], 0x82, BrakeReg[2], 0x83, BrakeReg[3], 0x84, 0x07 });
            BurstRead(0x80, 5, tempReadback);
            FuseClockOn(DeviceAddress, (double)num_UD_pulsewidth_ow_EngT.Value, (double)numUD_pulsedurationtime_ow_EngT.Value);
            DisplayOperateMes("Trimming...");
            //Delay(Delay_Fuse);

            ReloadPreset();
            Delay(Delay_Sync);
            BurstRead(0x80, 5, tempReadback);
            if (tempReadback[4] == 0)
            {
                RePower();
                EnterTestMode();
                RegisterWrite(5, new uint[10] { 0x80, BrakeReg[0], 0x81, BrakeReg[1], 0x82, BrakeReg[2], 0x83, BrakeReg[3], 0x84, 0x07 });
                BurstRead(0x80, 5, tempReadback);
                FuseClockOn(DeviceAddress, (double)num_UD_pulsewidth_ow_EngT.Value, (double)numUD_pulsedurationtime_ow_EngT.Value);
                DisplayOperateMes("Trimming...");
                //Delay(Delay_Fuse);
            }
            Delay(Delay_Sync);

            MarginalReadPreset();
            Delay(Delay_Sync);
            BurstRead(0x80, 5, tempReadback);
            bMarginal = false;
            if (bMASK)
            {
                if (((tempReadback[0] & 0xE0) != (BrakeReg[0] & 0xE0)) | (tempReadback[1] & 0x81) != (BrakeReg[1] & 0x81) |
                    (tempReadback[2] & 0x99) != (BrakeReg[2] & 0x99) | (tempReadback[3] & 0x83) != (BrakeReg[3] & 0x83) | (tempReadback[4] < 1))
                    bMarginal = true;
            }
            else
            {
                if (((tempReadback[0] & 0xFF) != (BrakeReg[0] & 0xFF)) | (tempReadback[1] & 0xFF) != (BrakeReg[1] & 0xFF) |
                        (tempReadback[2] & 0xFF) != (BrakeReg[2] & 0xFF) | (tempReadback[3] & 0xFF) != (BrakeReg[3] & 0xFF) | (tempReadback[4] < 7))
                    bMarginal = true;
            }
            if (bMarginal)
            {
                DisplayOperateMes("MRE");
            }

            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_5V);
            PowerOff();

        }
       
        private void btn_CommunicationTest_Click(object sender, EventArgs e)
        {
            //bool bCommPass = false;

            //oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_5V);
            //RePower();
            //EnterTestMode();
            //RegisterWrite(5, new uint[10] { 0x80, 0xAA, 0x81, 0xAA, 0x82, 0xAA, 0x83, 0xAA, 0x84, 0x07 });
            ////DisplayOperateMes("Write In Data is: ");
            //DisplayOperateMes("Reg{0} = 0xAA");
            //DisplayOperateMes("Reg{1} = 0xAA");
            //DisplayOperateMes("Reg{2} = 0xAA");
            //DisplayOperateMes("Reg{3} = 0xAA");
            //DisplayOperateMes("Reg{4} = 0x07");
            //BurstRead(0x80, 5, tempReadback);

            //if (tempReadback[0]!=0xAA || tempReadback[1]!=0xAA || tempReadback[2]!=0xAA || tempReadback[3]!=0xAA || tempReadback[4]!=0x07)
            //{
            //    //bCommPass = false;
            //    DisplayOperateMes("Communication Fail!", Color.Red);
            //    return;
            //}

            //Delay(Delay_Sync);

            //RegisterWrite(5, new uint[10] { 0x80, 0x55, 0x81, 0x55, 0x82, 0x55, 0x83, 0x55, 0x84, 0x07 });
            //DisplayOperateMes("Write In Data is: ");
            //DisplayOperateMes("Reg{0} = 0x55");
            //DisplayOperateMes("Reg{1} = 0x55");
            //DisplayOperateMes("Reg{2} = 0x55");
            //DisplayOperateMes("Reg{3} = 0x55");
            //DisplayOperateMes("Reg{4} = 0x07");
            //BurstRead(0x80, 5, tempReadback);

            //if (tempReadback[0] != 0x55 || tempReadback[1] != 0x55 || tempReadback[2] != 0x55 || tempReadback[3] != 0x55 || tempReadback[4] != 0x07)
            //{
            //    //bCommPass = false;
            //    DisplayOperateMes("Communication Fail!", Color.Red);
            //    return;
            //}

            if(Control.ModifierKeys == Keys.Shift)
            {
                DisplayOperateMes("Show Key Pass! ");
                this.cb_InvertSens_AutoTab.Enabled = true;
                this.cb_ChopCkDis_AutoTab.Enabled = true;
                this.cb_s2double_AutoTab.Enabled = true;
                this.cb_s3drv_autoTab.Enabled = true;
                this.cb_MeasureiQ_AutoTab.Enabled = true;
                this.cb_CustTc_AutoTab.Enabled = true;
                this.cb_BypFuse_AutoTab.Enabled = true;
                this.cb_8xHalls_AutoTab.Enabled = true;
                this.cb_BigCap_AutoTab.Enabled = true;
                this.cb_FastStart_AutoTab.Enabled = true;
            }
            if(Control.ModifierKeys == Keys.Control)
            {
                DisplayOperateMes("Hide Key Pass! ");
                //this.cb_InvertSens_AutoTab.Enabled = false;
                this.cb_ChopCkDis_AutoTab.Enabled = false;
                this.cb_s2double_AutoTab.Enabled = false;
                this.cb_s3drv_autoTab.Enabled = false;
                //this.cb_MeasureiQ_AutoTab.Enabled = false;
                //this.cb_CustTc_AutoTab.Enabled = false;
                //this.cb_BypFuse_AutoTab.Enabled = false;
                this.cb_8xHalls_AutoTab.Enabled = false;
                this.cb_BigCap_AutoTab.Enabled = false;
                this.cb_FastStart_AutoTab.Enabled = false;
            }
            if(Control.ModifierKeys == Keys.Alt)
            {
                DisplayOperateMes("Show all tabs! ");
                
                this.tabControl1.TabPages.Insert(0, EngineeringTab);
                //this.tabControl1.Controls.Remove(PriTrimTab);
                this.tabControl1.TabPages.Insert(1, PriTrimTab);
                this.tabControl1.TabPages.Insert(2, BrakeTab);
            }
        }                  

        private void btn_SafetyHighRead_EngT_Click(object sender, EventArgs e)
        {
            rbt_signalPathSeting_Vout_EngT.Checked = true;
            rbt_signalPathSeting_Config_EngT.Checked = true;

            SafetyHighReadPreset();
        }             

        private void btn_BrakeTab_InitializeUart_Click(object sender, EventArgs e)
        {
            #region UART Initialize
            //if (ProgramMode == 0)
            {

                //UART Initialization
                if (oneWrie_device.UARTInitilize(9600, 1))
                    DisplayOperateMes("UART Initilize succeeded!");
                else
                    DisplayOperateMes("UART Initilize failed!");
                //ding hao
                Delay(Delay_Sync);
                //DisplayAutoTrimOperateMes("Delay 300ms");

                //1. Current Remote CTL
                //if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_REMOTE, 0))
                //    DisplayOperateMes("Set Current Remote succeeded!");
                //else
                //    DisplayOperateMes("Set Current Remote failed!");

                //Delay 300ms
                //Thread.Sleep(300);
                Delay(Delay_Sync);
                //DisplayAutoTrimOperateMes("Delay 300ms");

                //2. Current On
                if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0))
                    DisplayOperateMes("Set Current On succeeded!");
                else
                    DisplayOperateMes("Set Current On failed!");

                //Delay 300ms
                Delay(Delay_Sync);
                //DisplayOperateMes("Delay 300ms");

                //3. Set Voltage
                if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETVOLT, 6u))
                    DisplayOperateMes(string.Format("Set Voltage to {0}V succeeded!", 6));
                else
                    DisplayOperateMes(string.Format("Set Voltage to {0}V failed!", 6));


                //Delay 300ms
                Delay(Delay_Sync);
                //DisplayOperateMes("Delay 300ms");


            }
            #endregion UART Initialize





        }

        private void txt_Delay_PreT_TextChanged(object sender, EventArgs e)
        {
            Delay_Fuse = int.Parse(txt_Delay_PreT.Text);
        }
            
        private void btn_EngTab_FT_Click(object sender, EventArgs e)
        {
            #region Define Parameters
            DialogResult dr;
            bool bMarginal = false;
            bool bSafety = false;
            //uint[] tempReadback = new uint[5];
            double dVout_0A_Temp = 0;
            double dVip_Target = TargetOffset + TargetVoltage_customer;
            double dGainTestMinusTarget = 1;
            double dGainTest = 0;
            ModuleAttribute sDUT;
            sDUT.dIQ = 0;
            sDUT.dVoutIPNative = 0;
            sDUT.dVout0ANative = 0;
            sDUT.dVoutIPMiddle = 0;
            sDUT.dVout0AMiddle = 0;
            sDUT.dVoutIPTrimmed = 0;
            sDUT.dVout0ATrimmed = 0;
            sDUT.iErrorCode = 00;
            sDUT.bDigitalCommFail = false;
            sDUT.bNormalModeFail = false;
            sDUT.bReadMarginal = false;
            sDUT.bReadSafety = false;
            sDUT.bTrimmed = false;

            // PARAMETERS DEFINE FOR MULTISITE
            uint idut = 0;
            uint uDutCount = 16;
            //bool bValidRound = false;
            //bool bSecondCurrentOn = false;
            double dModuleCurrent = 0;
            bool[] bGainBoost = new bool[16];
            bool[] bDutValid = new bool[16];
            bool[] bDutNoNeedTrim = new bool[16];
            uint[] uDutTrimResult = new uint[16];
            double[] dMultiSiteVoutIP = new double[16];
            double[] dMultiSiteVout0A = new double[16];

            /* autoAdaptingGoughGain algorithm*/
            double autoAdaptingGoughGain = 0;
            double autoAdaptingPresionGain = 0;
            double tempG1 = 0;
            double tempG2 = 0;
            double dGainPreset = 0;
            int Ix_forAutoAdaptingRoughGain = 0;
            int Ix_forAutoAdaptingPresionGain = 0;

            int ix_forOffsetIndex_Rough = 0;
            int ix_forOffsetIndex_Rough_Complementary = 0;
            double dMultiSiteVout_0A_Complementary = 0;

            DisplayOperateMes("\r\n**************" + DateTime.Now.ToString() + "**************");
            DisplayOperateMes("Start...");
            this.txt_Status_AutoTab.ForeColor = Color.Black;
            this.txt_Status_AutoTab.Text = "START!";

            for (uint i = 0; i < uDutCount; i++)
            {
                dMultiSiteVoutIP[i] = 0d;
                dMultiSiteVout0A[i] = 0d;

                MultiSiteReg0[i] = Reg80Value;
                MultiSiteReg1[i] = Reg81Value;
                MultiSiteReg2[i] = Reg82Value;
                MultiSiteReg3[i] = Reg83Value;

                MultiSiteRoughGainCodeIndex[i] = Ix_ForRoughGainCtrl;

                uDutTrimResult[i] = 0u;
                bDutNoNeedTrim[i] = false;
                bDutValid[i] = false;
                bGainBoost[i] = false;
            }
            #endregion Define Parameters

            #region Get module current
            //clear log
            DisplayOperateMesClear();
            /*  power on */
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_5V);
            RePower();
            Delay(Delay_Sync);
            this.txt_Status_AutoTab.Text = "Trimming!";
            /* Get module current */
            if (oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VCS))
            {
                if (bAutoTrimTest)
                    DisplayOperateMes("Set ADC VIN to VCS");
            }
            else
            {
                DisplayOperateMes("Set ADC VIN to VCS failed", Color.Red);
                PowerOff();
                return;
            }
            Delay(Delay_Sync);
            if (oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_SET_CURRENT_SENCE))
            {
                if (bAutoTrimTest)
                    DisplayOperateMes("Set ADC current sensor");
            }

            this.txt_ModuleCurrent_EngT.Text = GetModuleCurrent().ToString("F1");
            this.txt_ModuleCurrent_PreT.Text = this.txt_ModuleCurrent_EngT.Text;


            dModuleCurrent = GetModuleCurrent();
            sDUT.dIQ = dModuleCurrent;
            if (dCurrentDownLimit > dModuleCurrent)
            {
                DisplayOperateMes("Module " + " current is " + dModuleCurrent.ToString("F3"), Color.Red);
                //uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_CURRENT_ABNORMAL;
                PowerOff();
                //PrintDutAttribute(sDUT);
                MessageBox.Show(String.Format("电流偏低，检查模组是否连接！"), "Warning", MessageBoxButtons.OK);
                return;
            }
            else if (dModuleCurrent > dCurrentUpLimit)
            {
                DisplayOperateMes("Module " + " current is " + dModuleCurrent.ToString("F3"), Color.Red);
                uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_CURRENT_HIGH;
                PowerOff();
                sDUT.iErrorCode = uDutTrimResult[idut];
                PrintDutAttribute(sDUT);
                //MessageBox.Show(String.Format("电流异常，模块短路或损坏！"), "Error", MessageBoxButtons.OK);
                this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                this.txt_Status_AutoTab.Text = "短路!";
                return;
            }
            else
                DisplayOperateMes("Module " + " current is " + dModuleCurrent.ToString("F3"));

            #endregion Get module current

            #region UART Initialize
            if (ProgramMode == 0)
            {
                //if (ProgramMode == 0 && bUartInit == false)
                //{

                //UART Initialization
                if (oneWrie_device.UARTInitilize(9600, 1))
                    DisplayOperateMes("UART Initilize succeeded!");
                else
                    DisplayOperateMes("UART Initilize failed!");
                //ding hao
                Delay(Delay_Power);
                //DisplayAutoTrimOperateMes("Delay 300ms");

                //1. Current Remote CTL
                if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_REMOTE, 0))
                    DisplayOperateMes("Set Current Remote succeeded!");
                else
                    DisplayOperateMes("Set Current Remote failed!");

                //Delay 300ms
                //Thread.Sleep(300);
                Delay(Delay_Power);
                //DisplayAutoTrimOperateMes("Delay 300ms");

                //2. Current On
                //if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0))
                if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, Convert.ToUInt32(IP)))
                    DisplayOperateMes("Set Current to IP succeeded!");
                else
                    DisplayOperateMes("Set Current to IP failed!");

                //Delay 300ms
                Delay(Delay_Power);
                //DisplayOperateMes("Delay 300ms");

                //3. Set Voltage
                if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETVOLT, 6u))
                    DisplayOperateMes(string.Format("Set Voltage to {0}V succeeded!", 6));
                else
                    DisplayOperateMes(string.Format("Set Voltage to {0}V failed!", 6));


                //Delay 300ms
                Delay(Delay_Power);
                //DisplayOperateMes("Delay 300ms");

                //bUartInit = true;
                //}
            }
            #endregion UART Initialize

            #region Communication Test
            Delay(Delay_Sync);
            EnterTestMode();
            //Delay(Delay_Sync);
            BurstRead(0x80, 5, tempReadback);
            if (tempReadback[0] + tempReadback[1] + tempReadback[2] + tempReadback[3] + tempReadback[4] != 0)
            {
                DisplayOperateMes("DUT" + " has some bits Blown!", Color.Red);
                uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_TRIMMED_SOMEBITS;
                TrimFinish();
                sDUT.bTrimmed = false;
                sDUT.iErrorCode = uDutTrimResult[idut];
                PrintDutAttribute(sDUT);
                this.txt_Status_AutoTab.ForeColor = Color.Red;
                this.txt_Status_AutoTab.Text = "FAIL!";
                return;
            }
            else
            {
                Delay(Delay_Sync);
                RegisterWrite(5, new uint[10] { 0x80, 0xFF, 0x81, 0xFF, 0x82, 0xFF, 0x83, 0xFF, 0x84, 0x07 });
                BurstRead(0x80, 5, tempReadback);
                if (tempReadback[0] != 0xFF || tempReadback[1] != 0xFF
                    || tempReadback[2] != 0xFF || tempReadback[3] != 0xFF || tempReadback[4] != 0x07)
                {
                    DisplayOperateMes("Communication Test Fail!", Color.Red);
                }
            }


            #endregion Communication Test

            #region Saturation judgement

            RePower();
            Delay(Delay_Sync);
            //Redundency delay in case of power off failure.
            //Delay(Delay_Sync);
            //EnterTestMode();
            ////Delay(Delay_Sync);
            //BurstRead(0x80, 5, tempReadback);
            //if (tempReadback[0] + tempReadback[1] + tempReadback[2] + tempReadback[3] + tempReadback[4] != 0)
            //{
            //    DisplayOperateMes("DUT" + " has some bits Blown!", Color.Red);
            //    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_TRIMMED_SOMEBITS;
            //    TrimFinish();
            //    sDUT.bTrimmed = false;
            //    sDUT.iErrorCode = uDutTrimResult[idut];
            //    PrintDutAttribute(sDUT);
            //    this.txt_Status_AutoTab.ForeColor = Color.Red;
            //    this.txt_Status_AutoTab.Text = "FAIL!";
            //    return;
            //}

            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            BurstRead(0x80, 5, tempReadback);
            if (tempReadback[0] != MultiSiteReg0[idut] || tempReadback[1] != MultiSiteReg1[idut]
                || tempReadback[2] != MultiSiteReg2[idut] || tempReadback[3] != MultiSiteReg3[idut])
            {
                if (tempReadback[0] + tempReadback[1] + tempReadback[2] + tempReadback[3] + tempReadback[4] == 0)
                {
                    RePower();
                    Delay(Delay_Sync);
                    oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);
                    Delay(Delay_Sync);
                    oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);
                    Delay(Delay_Fuse);
                    dMultiSiteVout0A[idut] = AverageVout();
                    if (dMultiSiteVout0A[idut] < 4.5 && dMultiSiteVout0A[idut] > 1.5)
                    {
                        DisplayOperateMes("DUT Trimmed!", Color.Red);
                        uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_TRIMMRD_ALREADY;
                        TrimFinish();
                        sDUT.bDigitalCommFail = true;
                        sDUT.bTrimmed = true;
                        sDUT.iErrorCode = uDutTrimResult[idut];
                        PrintDutAttribute(sDUT);
                        //MessageBox.Show(String.Format("模组已编程，交至研发部！"), "Error", MessageBoxButtons.OK);
                        this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                        this.txt_Status_AutoTab.Text = "混料!";
                        return;
                    }
                    else
                    {
                        DisplayOperateMes("VOUT Short!", Color.Red);
                        uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_VOUT_SHORT;
                        TrimFinish();
                        sDUT.bDigitalCommFail = true;
                        sDUT.iErrorCode = uDutTrimResult[idut];
                        PrintDutAttribute(sDUT);
                        //MessageBox.Show(String.Format("输出管脚短路！", Color.YellowGreen), "Warning", MessageBoxButtons.OK);
                        this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                        this.txt_Status_AutoTab.Text = "短路!";
                        return;
                    }
                }
                else
                {
                    DisplayOperateMes("DUT digital communication fail!", Color.Red);
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_COMM_FAIL;
                    TrimFinish();
                    sDUT.bDigitalCommFail = true;
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    //MessageBox.Show(String.Format("输出管脚短路！", Color.YellowGreen), "Warning", MessageBoxButtons.OK);
                    this.txt_Status_AutoTab.ForeColor = Color.Red;
                    this.txt_Status_AutoTab.Text = "FAIL!";
                    return;
                }
            }
            /* Get vout @ IP */
            EnterNomalMode();

            /* Change Current to IP  */
            //dr = MessageBox.Show(String.Format("Please Change Current To {0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
            //3. Set Voltage
            if (ProgramMode == 0)
            {
                //if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, Convert.ToUInt32(IP)))
                if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                    DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", IP));
                else
                {
                    DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                    TrimFinish();
                    return;
                }
            }
            else if (ProgramMode == 1)
            {
                dr = MessageBox.Show(String.Format("请将电流升至{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    PowerOff();
                    RestoreRegValue();
                    return;
                }
            }


            //oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);
            Delay(Delay_Fuse);
            dMultiSiteVoutIP[idut] = AverageVout();
            sDUT.dVoutIPNative = dMultiSiteVoutIP[idut];
            DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));

            /*Judge PreSet gain; delta Vout target >= delta Vout test * 86.07% */
            if (dMultiSiteVoutIP[idut] > saturationVout)
            {
                if (ProgramMode == 0)
                {
                    oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0);
                    Delay(Delay_Sync);
                    oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, 5);
                    Delay(Delay_Sync);
                    if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                        DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", 5));
                    else
                    {
                        DisplayOperateMes(string.Format("Set Current to {0}A failed!", 5));
                        TrimFinish();
                        return;
                    }
                }
                else if (ProgramMode == 1)
                {
                    dr = MessageBox.Show(String.Format("请将电流升至{0}A", 5), "Change Current", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                    {
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        PowerOff();
                        RestoreRegValue();
                        return;
                    }
                }

                Delay(Delay_Fuse);
                dMultiSiteVoutIP[idut] = AverageVout();
                sDUT.dVoutIPNative = dMultiSiteVoutIP[idut];
                DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));

                //set current back to IP
                if (ProgramMode == 0)
                {
                    oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0);
                    Delay(Delay_Sync);
                    oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, Convert.ToUInt32(IP));
                }

                if (dMultiSiteVoutIP[idut] > saturationVout)
                {
                    DisplayOperateMes("Module" + " Vout is VDD!", Color.Red);
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_VOUT_VDD;
                    TrimFinish();
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                    this.txt_Status_AutoTab.Text = "短路!";
                    return;
                }
                else
                {
                    //dr = MessageBox.Show(String.Format("输出饱和，交研发部重新编程！"), "Warning", MessageBoxButtons.OK);
                    TrimFinish();
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_VOUT_SATURATION;
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                    this.txt_Status_AutoTab.Text = "饱和!";
                    return;
                }
            }
            else if (dMultiSiteVoutIP[idut] < minimumVoutIP)
            {
                DisplayOperateMes("Module" + " Vout is too Low!", Color.Red);
                uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_VOUT_LOW;
                TrimFinish();
                sDUT.iErrorCode = uDutTrimResult[idut];
                PrintDutAttribute(sDUT);
                this.txt_Status_AutoTab.ForeColor = Color.Yellow;
                this.txt_Status_AutoTab.Text = "短路!";
                return;
            }

            #endregion Saturation judgement

            #region Get Vout@0A
            /* Change Current to 0A */
            //3. Set Voltage
            if (ProgramMode == 0)
            {
                //if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, 0u))
                if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0u))
                    DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", 0u));
                else
                {
                    DisplayOperateMes(string.Format("Set Current to {0}A failed!", 0u));
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    TrimFinish();
                    return;
                }
            }
            else
            {
                dr = MessageBox.Show(String.Format("请将IP降至0A!"), "Try Again", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    PowerOff();
                    RestoreRegValue();
                    return;
                }
            }

            Delay(Delay_Fuse);
            dMultiSiteVout0A[idut] = AverageVout();
            sDUT.dVout0ANative = dMultiSiteVout0A[idut];
            DisplayOperateMes("Vout" + " @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

            if (dMultiSiteVoutIP[idut] < dMultiSiteVout0A[idut])
            {
                TrimFinish();
                //PrintDutAttribute(sDUT);
                MessageBox.Show(String.Format("请确认IP方向!"), "Try Again", MessageBoxButtons.OK);
                return;
            }
            else if (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut] < VoutIPThreshold)
            {
                TrimFinish();
                //PrintDutAttribute(sDUT);
                MessageBox.Show(String.Format("请确认电流为{0}A!!!", IP), "Try Again", MessageBoxButtons.OK);
                return;
            }

            if (TargetOffset == 2.5)
            {
                if (dMultiSiteVout0A[idut] < 2.25 || dMultiSiteVout0A[idut] > 2.8)
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_OFFSET_ABN;
                    TrimFinish();
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    this.txt_Status_AutoTab.ForeColor = Color.Red;
                    this.txt_Status_AutoTab.Text = "FAIL!";
                    return;
                }
            }
            else if (TargetOffset == 1.65)
            {
                if (dMultiSiteVout0A[idut] < 1.0 || dMultiSiteVout0A[idut] > 2.5)
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_OFFSET_ABN;
                    TrimFinish();
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    this.txt_Status_AutoTab.ForeColor = Color.Red;
                    this.txt_Status_AutoTab.Text = "FAIL!";
                    return;
                }
            }

            if ((dMultiSiteVout0A[idut] - dMultiSiteVout0A[idut]) < 2)
                DisplayOperateMes("Gain is too low!", Color.Red); ;

            #endregion  Get Vout@0A
        }       

        private void CurrentSensorConsole_Load(object sender, EventArgs e)
        {

        }
        #endregion Events

        #region Brake
        private void btn_UpdateStartPoint_BrakeT_Click(object sender, EventArgs e)
        {
            RePower();
            EnterTestMode();
            RegisterWrite(4,new uint[8]{0x80,Reg80Value,0x81,Reg81Value,0x82,Reg82Value,0x83,Reg83Value});
            EnterNomalMode();
            Delay(500);
            BkAttri.StartPoint = AverageVout();
            UpdateBrakeTab();
        }

        private void btn_UpdateStopPoint_BrakeT_Click(object sender, EventArgs e)
        {
            RePower();
            EnterTestMode();
            RegisterWrite(4, new uint[8] { 0x80, Reg80Value, 0x81, Reg81Value, 0x82, Reg82Value, 0x83, Reg83Value });
            EnterNomalMode();
            Delay(500);
            BkAttri.StopPoint = AverageVout();
            UpdateBrakeTab();
        }

        private void btn_DRUp_BrakeT_Click(object sender, EventArgs e)
        {
            btn_GainCtrlPlus_PreT_Click(null,null);
            UpdateBrakeTab();
        }

        private void btn_DRDown_BrakeT_Click(object sender, EventArgs e)
        {
            btn_GainCtrlMinus_PreT_Click(null,null);
            UpdateBrakeTab();
        }

        private void btn_OffsetUp_BrakeT_Click(object sender, EventArgs e)
        {
            if (Ix_ForOffsetATable == 0)
                Ix_ForOffsetATable = 15;
            else if (Ix_ForOffsetATable == 8)
                DisplayOperateMes("Reach to Max Coarse Offset!", Color.DarkRed);
            else
                Ix_ForOffsetATable--;

            bit_op_mask = bit7_Mask;
            Reg81Value &= ~bit_op_mask;
            bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask ;
            Reg82Value &= ~bit_op_mask;

            Reg81Value |= Convert.ToUInt32(OffsetTableA_Customer[1][Ix_ForOffsetATable]);
            Reg82Value |= Convert.ToUInt32(OffsetTableA_Customer[2][Ix_ForOffsetATable]);

            UpdateBrakeTab();
        }

        private void btn_OffsetDown_BrakeT_Click(object sender, EventArgs e)
        {
            if (Ix_ForOffsetATable == 15)
                Ix_ForOffsetATable = 0;
            else if (Ix_ForOffsetATable == 7)
                DisplayOperateMes("Reach to Min Coarse Offset!", Color.DarkRed);
            else
                Ix_ForOffsetATable++;

            bit_op_mask = bit7_Mask;
            Reg81Value &= ~bit_op_mask;
            bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask;
            Reg82Value &= ~bit_op_mask;

            Reg81Value |= Convert.ToUInt32(OffsetTableA_Customer[1][Ix_ForOffsetATable]);
            Reg82Value |= Convert.ToUInt32(OffsetTableA_Customer[2][Ix_ForOffsetATable]);

            UpdateBrakeTab();
        }

        private void UpdateBrakeTab()
        {
            this.txt_StartPoint_BrakeT.Text = BkAttri.StartPoint.ToString("F3");
            this.txt_StopPoint_BrakeT.Text = BkAttri.StopPoint.ToString("F3");
            this.txt_DynamicRange_BrakeT.Text = (BkAttri.StopPoint - BkAttri.StartPoint).ToString("F3");

            BkAttri.targetStartPoint  = Convert.ToDouble(this.txt_TargetStartPoint_BrakeT.Text);
            BkAttri.targetStopPoint = Convert.ToDouble(this.txt_TargetStopPoint_BrakeT.Text);
            this.txt_TargetDynamicRange_BrakeT.Text = (BkAttri.targetStopPoint - BkAttri.targetStartPoint).ToString("F3");
        }

        private void BrakeTab_Click(object sender, EventArgs e)
        {
            UpdateBrakeTab();
        }

        private void txt_TargetStartPoint_BrakeT_TextChanged(object sender, EventArgs e)
        {
            UpdateBrakeTab();
        }

        private void txt_TargetStopPoint_BrakeT_TextChanged(object sender, EventArgs e)
        {
            UpdateBrakeTab();
        }

        private void btn_FineDRUp_BrakeT_Click(object sender, EventArgs e)
        {
            if (Ix_ForPrecisonGainCtrl == 0)
                DisplayOperateMes("Reach to Max fine Gain!", Color.DarkRed);
            else
            {
                Ix_ForPrecisonGainCtrl--;

                /* Presion Gain Code*/
                bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask | bit3_Mask | bit4_Mask;
                Reg80Value &= ~bit_op_mask;
                Reg80Value |= Convert.ToUInt32(PreciseTable_Customer[1][Ix_ForPrecisonGainCtrl]);
            }

        }

        private void btn_FineDRDown_BrakeT_Click(object sender, EventArgs e)
        {
            if (Ix_ForPrecisonGainCtrl == 31)
                DisplayOperateMes("Reach to Min fine Gain!", Color.DarkRed);
            else
            {
                Ix_ForPrecisonGainCtrl++;

                /* Presion Gain Code*/
                bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask | bit3_Mask | bit4_Mask;
                Reg80Value &= ~bit_op_mask;
                Reg80Value |= Convert.ToUInt32(PreciseTable_Customer[1][Ix_ForPrecisonGainCtrl]);
            }
        }

        private void btn_FineOffsetUp_BrakeT_Click(object sender, EventArgs e)
        {
            if (Ix_ForOffsetBTable == 0)
                Ix_ForOffsetBTable = 15;
            else if(Ix_ForOffsetBTable == 8)
                DisplayOperateMes("Reach to Max Fine Offset!", Color.DarkRed);
            else
                Ix_ForOffsetBTable--;

            bit_op_mask = bit2_Mask | bit3_Mask | bit4_Mask | bit5_Mask;
            Reg83Value &= ~bit_op_mask;
            Reg83Value |= Convert.ToUInt32(OffsetTableB_Customer[1][Ix_ForOffsetBTable]);
        }

        private void btn_FineOffsetDown_BrakeT_Click(object sender, EventArgs e)
        {
            if (Ix_ForOffsetBTable == 15)
                Ix_ForOffsetBTable = 0;
            else if (Ix_ForOffsetBTable == 7)
                DisplayOperateMes("Reach to Min Fine Offset!", Color.DarkRed);
            else
                Ix_ForOffsetBTable++;

            bit_op_mask = bit2_Mask | bit3_Mask | bit4_Mask | bit5_Mask;
            Reg83Value &= ~bit_op_mask;
            Reg83Value |= Convert.ToUInt32(OffsetTableB_Customer[1][Ix_ForOffsetBTable]);
        }

        #endregion Brake

        #region New approch
        private bool InitProductAttri()
        {
            //string fName;
            //OpenFileDialog openFileDialog = new OpenFileDialog();
            //fName = System.Environment.CurrentDirectory;
            //openFileDialog.InitialDirectory = fName;
            //openFileDialog.Filter = "cfg|*.*|cfg file|*.cfg|all files|*.*";
            //openFileDialog.RestoreDirectory = true;
            //openFileDialog.FilterIndex = 1;
            //if (openFileDialog.ShowDialog() == DialogResult.OK)
            //    fName = openFileDialog.FileName;
            //else
            //    return false;
            refPart.uProductID = 620;
            refPart.bDebug = false;
            refPart.uIP = 20;
            refPart.dIQn = 0;
            refPart.dIQp = 0;
            refPart.dVipPostGainTrim = 0;
            refPart.dVoffsetPostGainTrim = 0;
            refPart.dVoffsetPreTrim = 0;
            refPart.dVoutIPPreTrim = 0;
            refPart.dVref = 0;
            refPart.dVtargetOffset = 2.5;
            refPart.dVtargetOutIP = 2;
            refPart.uRegTable = new uint[18];
            for (uint i = 0; i < 18; i++)
            {
                refPart.uRegTable[2 * i + 0] = 0x80 + i;
                refPart.uRegTable[2 * i + 1] = 0x00;
            }
            refPart.uCoarseGainIndex = 0;
            refPart.uCoarseOffsetIndex = 0;
            refPart.uFineGainIndex = 0;
            refPart.uFineOffsetIndex = 0;
            refPart.uReturnCode = 0x00;

            return true;
        }

        private void RestoreProductAttri()
        {
            newPart = refPart;
        }

        private bool InitIPSupply(uint powerModule)
        {
            if (powerModule == 0x00)
            {
                //UART Initialization
                if (!oneWrie_device.UARTInitilize(9600, 1))
                {
                    DisplayOperateMes("UART Initilize failed!");
                    return false;
                }

                Delay(Delay_Sync);

                //1. Current Remote CTL
                if (!oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_REMOTE, 0))
                {
                    DisplayOperateMes("Set Current Remote failed!");
                    return false;
                }

                Delay(Delay_Sync);

                //2. Current
                if (!oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR,
                    Convert.ToUInt32(IP)))
                {
                    DisplayOperateMes("Set Current to IP Failed!");
                    return false;
                }

                Delay(Delay_Sync);

                //3. Set Voltage
                if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETVOLT, 6u))
                {
                    DisplayOperateMes(string.Format("Set Voltage to {0}V failed!", 6));
                    return false;
                }

                Delay(Delay_Sync);

                return true;
            }
            else
            {
                return false;
            }
        }

        private bool IPON(uint powerModule)
        {
            return true;
        }

        private bool IPOFF(uint powerModule)
        {
            return true;
        }

        private bool EnterTestMode(uint pID)
        {
            return true;
        }

        private bool EnterNormalMode(uint pID)
        {
            return true;
        }

        private double ReadVout()
        {
            return AverageVout();
        }

        private double ReadVref()
        {
            return AverageVout();
        }

        private double ReadIq()
        {
            return AverageVout();
        }

        private bool RegWrite()
        {
            return true;
        }

        private bool RegRead()
        {
            return true;
        }

        private void Fuse()
        { 
        
        }



        #endregion 

        private void cmb_PreTrim_SensorDirection_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.cmb_PreTrim_SensorDirection.SelectedIndex == 0)
                this.cmb_SensingDirection_EngT.SelectedIndex = 0;
            else if (this.cmb_PreTrim_SensorDirection.SelectedIndex == 1)
                this.cmb_SensingDirection_EngT.SelectedIndex = 2;

            /* bit5 & bit6 of 0x82 */
            //bit_op_mask = bit5_Mask | bit6_Mask;
            //uint[] valueTable = new uint[4]
            //{
            //    0x0,
            //    0x20,
            //    0x40,
            //    0x60
            //};

            //int ix_TableStart = this.cmb_SensingDirection_EngT.SelectedIndex;
            ////back up to register and update GUI
            //Reg82Value &= ~bit_op_mask;
            //Reg82Value |= valueTable[ix_TableStart];
        }

        private void keyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //this.cb_iHallDecrease_AutoTab.Visible = true;
            this.cb_ChopCkDis_AutoTab.Visible = true;
        }

        private void printRegValue()
        {
            DisplayOperateMes("Reg0x80 = 0x" + MultiSiteReg0[0].ToString("X2"));
            DisplayOperateMes("Reg0x81 = 0x" + MultiSiteReg1[0].ToString("X2"));
            DisplayOperateMes("Reg0x82 = 0x" + MultiSiteReg2[0].ToString("X2"));
            DisplayOperateMes("Reg0x83 = 0x" + MultiSiteReg3[0].ToString("X2"));

            DisplayOperateMes("Reg0x84 = 0x" + MultiSiteReg4[0].ToString("X2"));
            DisplayOperateMes("Reg0x85 = 0x" + MultiSiteReg5[0].ToString("X2"));
            DisplayOperateMes("Reg0x86 = 0x" + MultiSiteReg6[0].ToString("X2"));
            DisplayOperateMes("Reg0x87 = 0x" + MultiSiteReg7[0].ToString("X2"));

            DisplayOperateMes(MultiSiteReg0[0].ToString("X2") + "," + MultiSiteReg1[0].ToString("X2") + "," + MultiSiteReg2[0].ToString("X2") + "," + MultiSiteReg3[0].ToString("X2") + "," +
                              MultiSiteReg4[0].ToString("X2") + "," + MultiSiteReg5[0].ToString("X2") + "," + MultiSiteReg6[0].ToString("X2") + "," + MultiSiteReg7[0].ToString("X2"));
        }

        private void cb_SelectedDut_AutoTab_SelectedIndexChanged(object sender, EventArgs e)
        {
            MultiSiteSocketSelect( Convert.ToUInt32( this.cb_SelectedDut_AutoTab.SelectedIndex) );
            DisplayOperateMes("Selected DUT is " + this.cb_SelectedDut_AutoTab.SelectedIndex.ToString());
        }

        private void btn_AutoSet_AutoT_Click(object sender, EventArgs e)
        {
            #region Var Definetion
            DialogResult dr;
            DisplayOperateMesClear();
            IP = Convert.ToDouble(this.txt_IP_AutoT.Text);
            TargetOffset = Convert.ToDouble(this.txt_VoutOffset_AutoT.Text);
            TargetGain_customer = Convert.ToDouble(this.txt_TargertVoltage_AutoT.Text);
            double V0A_Pretrim = 0;
            double Vip_Pretrim = 0;
            double coarse_PretrimGain = 0;
            #endregion

            #region Check HW connection
            if (!bUsbConnected)
            {
                DisplayOperateMes("Please Confirm HW Connection!", Color.Red);
                return;
            }
            #endregion

            #region IP Initialize
            if (ProgramMode == 0)
            {
                //if (ProgramMode == 0 && bUartInit == false)
                //{

                //UART Initialization
                if (!oneWrie_device.UARTInitilize(9600, 1))
                    DisplayOperateMes("UART Initilize failed!");
                //ding hao
                Delay(Delay_Power);

                //1. Current Remote CTL
                if (!oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_REMOTE, 0))
                    DisplayOperateMes("Set Current Remote failed!");

                Delay(Delay_Power);

                if (!oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, Convert.ToUInt32(IP)))
                    DisplayOperateMes("Set Current to IP failed!");

                Delay(Delay_Power);

                //3. Set Voltage
                if (!oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETVOLT, 6u))
                    DisplayOperateMes(string.Format("Set Voltage to {0}V failed!", 6));

                Delay(Delay_Power);

            }
            else if (ProgramMode == 2)
            {
                MultiSiteSocketSelect(1);   //epio1,3 = high

                if (!bDualRelayIpOn)
                {
                    bDualRelayIpOn = true;
                    dr = MessageBox.Show(String.Format("请将电流升至{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                    {
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        PowerOff();
                        RestoreRegValue();
                        return;
                    }
                }
            }
            #endregion IP Initialize          

            RePower();
            Delay(Delay_Sync);
            RegisterWrite(4, new uint[8] { 0x80, Reg80Value, 0x81, Reg81Value, 0x82, Reg82Value, 0x83, Reg83Value });
            Delay(Delay_Sync);
            BurstRead(0x80, 5, tempReadback);
            EnterNomalMode();
            Delay(Delay_Fuse);
            V0A_Pretrim = AverageVout();

            #region /* Change Current to IP  */
            if (ProgramMode == 0)
            {
                if (!oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                {
                    DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                    TrimFinish();
                    return;
                }
            }
            else if (ProgramMode == 1)
            {
                dr = MessageBox.Show(String.Format("请将电流升至{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    PowerOff();
                    RestoreRegValue();
                    return;
                }
            }
            else if (ProgramMode == 2)
            {
                MultiSiteSocketSelect(1);   //epio1,3 = high
                Delay(Delay_Sync);
                MultiSiteSocketSelect(0);   //set epio1 = high; epio3 = low
            }
            #endregion

            Delay(Delay_Fuse);
            Vip_Pretrim = AverageVout();
            DisplayOperateMes("Vout@0A = " + V0A_Pretrim.ToString("F3"));
            DisplayOperateMes("Vout@IP_1 = " + Vip_Pretrim.ToString("F3"));


            if (Vip_Pretrim - V0A_Pretrim < 0)
            {
                DisplayOperateMes("请确认IP方向！");
                TrimFinish();
                return;
            }
            else if (Vip_Pretrim - V0A_Pretrim < 0.005d)
            {
                DisplayOperateMes("请确认IP是否ON！");
                TrimFinish();
                return;
            }

            if (Vip_Pretrim > 4.8)
            {

                DisplayOperateMes("编程电流过大，输出饱和！");
                TrimFinish();
                return;

            }

            //coarse_PretrimGain = 2.0d * 12.7d / (Vip_Pretrim - V0A_Pretrim);
            coarse_PretrimGain = TargetVoltage_customer * 12.7d / (Vip_Pretrim - V0A_Pretrim);
            DisplayOperateMes("coarse_PretrimGain = " + coarse_PretrimGain.ToString("F3"));


            if (coarse_PretrimGain >= 200)
            {
                DisplayOperateMes("产品灵敏度要求过高！");
                TrimFinish();
                return;
            }
            else if (coarse_PretrimGain < 11)
            {
                DisplayOperateMes("产品灵敏度要求过低！");
                TrimFinish();
                return;
            }



            RePower();
            Delay(Delay_Sync);
            RegisterWrite(4, new uint[8] { 0x80, Reg80Value, 0x81, Reg81Value, 0x82, Reg82Value, 0x83, Reg83Value });
            Delay(Delay_Sync);
            BurstRead(0x80, 5, tempReadback);
            EnterNomalMode();
            Delay(Delay_Fuse);
            Vip_Pretrim = AverageVout();
            DisplayOperateMes("Vout@IP_2 = " + Vip_Pretrim.ToString("F3"));
            if (Vip_Pretrim < 4.5)
            {
                if (preSetCoareseGainCode > 0)
                    preSetCoareseGainCode--;
                Reg81Value = 0x03 + preSetCoareseGainCode * 16;
            }
            else if (Vip_Pretrim > 4.9)
            {
                if (preSetCoareseGainCode == 15)
                {
                    DisplayOperateMes("Saturation!", Color.Red);
                    TrimFinish();
                    return;
                }
                else
                {
                    preSetCoareseGainCode++;
                    Reg81Value = 0x03 + preSetCoareseGainCode * 16;
                }
            }

            #region /* Change Current to 0A */
            if (ProgramMode == 0)
            {
                if (!oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0u))
                {
                    DisplayOperateMes(string.Format("Set Current to {0}A failed!", 0u));
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    TrimFinish();
                    return;
                }
            }
            else if (ProgramMode == 1)
            {
                dr = MessageBox.Show(String.Format("请将IP降至0A!"), "Try Again", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    PowerOff();
                    RestoreRegValue();
                    return;
                }
            }
            else if (ProgramMode == 2)
            {
                //set epio1 and epio3 to low
                MultiSiteSocketSelect(1);       //set epio1 and epio3 to high
                Delay(Delay_Sync);
                MultiSiteSocketSelect(9);       //set epio1 = low; epio3 = high
            }
            #endregion

            Delay(Delay_Fuse);
            V0A_Pretrim = AverageVout();
            DisplayOperateMes("Vout@0A_2 = " + V0A_Pretrim.ToString("F3"));
        }

        private void cb_V0AOption_AutoTab_SelectedIndexChanged(object sender, EventArgs e)
        {
            int ix = 0;
            ix = this.cb_V0AOption_AutoTab.SelectedIndex;
            if (ix == 0)
            {
                //this.cmb_OffsetOption_EngT.SelectedIndex = 0;
                this.txt_VoutOffset_AutoT.Text = "2.5";
            }
            else if (ix == 1)
            {
                //this.cmb_OffsetOption_EngT.SelectedIndex = 2;
                this.txt_VoutOffset_AutoT.Text = "2.5";
            }
            else if (ix == 2)
            {
                //this.cmb_OffsetOption_EngT.SelectedIndex = 3;
                this.txt_VoutOffset_AutoT.Text = "1.65";
            }
            else if (ix == 3)
            {
                this.txt_VoutOffset_AutoT.Text = "0.52";
            }
        }

        private void btn_PowerOnTest_AutoT_Click(object sender, EventArgs e)
        {
            double vout = 0;

            if (!bUsbConnected)
            {
                DisplayOperateMes("Please Confirm HW Connection!", Color.Red);
                return;
            }

            RePower();

            Delay(Delay_Fuse);

            btn_ModuleCurrent_EngT_Click(null, null);

            Delay(Delay_Sync);
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);

            Delay(Delay_Sync);
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);

            Delay(Delay_Fuse);

            vout = AverageVout();
            DisplayOperateMes("Vout_Init = " + vout.ToString("F3") + "V");

            EnterNomalMode();
            Delay(100);
            vout= AverageVout();
            DisplayOperateMes("Vout_Normal = " +  vout.ToString("F3") + "V");
        }

        private void txt_TargertVoltage_AutoT_TextChanged(object sender, EventArgs e)
        {
            double gain = 0;
            double Ip = 10;
            if (this.txt_TargertVoltage_AutoT.Text != "" && this.txt_IP_AutoT.Text != "")
            {
                gain = Convert.ToDouble(this.txt_TargertVoltage_AutoT.Text);
                Ip = Convert.ToDouble(this.txt_IP_AutoT.Text);

                if (Ip != 0)
                    this.txt_TargetGain_AutoT.Text = (gain * 1000d / Ip).ToString("F1");
                else
                    this.txt_TargetGain_AutoT.Text = "Error";
            }
        }

        private void txt_IP_AutoT_TextChanged(object sender, EventArgs e)
        {
            double gain = 0;
            double Ip = 10;
            if (this.txt_TargertVoltage_AutoT.Text != "" && this.txt_IP_AutoT.Text != "")
            {
                gain = Convert.ToDouble(this.txt_TargertVoltage_AutoT.Text);
                Ip = Convert.ToDouble(this.txt_IP_AutoT.Text);

                if (Ip != 0)
                    this.txt_TargetGain_AutoT.Text = (gain * 1000d / Ip).ToString("F1");
                else
                    this.txt_TargetGain_AutoT.Text = "Error";
            } 
        }

        private void txt_AdcOffset_AutoT_TextChanged(object sender, EventArgs e)
        {
            AdcOffset = Convert.ToDouble(this.txt_AdcOffset_AutoT.Text);
        }

        private void btn_Ft_AutoT_Click(object sender, EventArgs e)
        {
            int i = 0;

            while (true)
            {
                i++;
                Delay(100);
                if (oneWrie_device.SDPSingalPathReadSot())
                {
                    DisplayOperateMes("SOT is assert! --- " + i.ToString());
                    Delay(200);
                    oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_WRITE_EOT); //EPIO9
                    oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_WRITE_BIN_FAIL); //EPIO8
                    oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_WRITE_BIN_ONE); //EPIO10
                    oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_WRITE_BIN_TWO); //EPIO11
                    //oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_WRITE_BIN_FAIL); //EPIO8
                    oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_WRITE_BIN_RECYCLE); //EPIO12
                }
                else
                {
                    DisplayOperateMes("No SOT! --- " + i.ToString());
                    Delay(200);
                }
            }
        }





    }

    
}