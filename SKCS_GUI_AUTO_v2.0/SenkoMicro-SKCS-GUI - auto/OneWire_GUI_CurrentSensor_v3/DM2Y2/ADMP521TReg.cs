using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ADI.DMY2
{
    class ADMP521TReg
    {
        public ADMP521TReg()
        {
            CreatRegisters();
            SetDefaultValue();
        }

        #region Registers define    
        private Register Test_mode;
        private Register Test00;
        private Register Test01;
        private Register Test02;
        private Register Test03;
        private Register Test04;
        private Register Test05;
        private Register Test06;
        private Register SoftReset;
        #endregion Registers define

        public RegisterMap ADMP521TRegMap = new RegisterMap();
        private void CreatRegisters()
        {
            #region Register difine           
            Test_mode = new Register("Test_mode", 0xAA, "Test_Mode", 8);
            Test00 = new Register("Test00", 0xF0, "Test_ana_H", 8);
            Test01 = new Register("Test01", 0xF1, "Test_ana_L", 8);
            Test02 = new Register("Test02", 0xF2, "Trim_Sel", 1, "Fuse_en", 1, "Trim_load", 1, "ANA_T", 1, "Norm_T", 1,"LR_sel",1,"Norm_en",1,"Fuse_COPY",1);
            Test03 = new Register("Test03", 0xF3, "Chip_ID_H", 8);
            Test04 = new Register("Test04", 0xF4, "CHip_ID_L", 8);
            Test05 = new Register("Test05", 0xF5, "Gain", 5);
            Test06 = new Register("Test06", 0xF6, "Fuse_Version", 2, "Trim_master", 1);
            SoftReset = new Register("SoftReset", 0xFF, "SoftReset", 8); 
            #endregion Register difine

            #region Add all registers to regMap           
            ADMP521TRegMap.Add(Test_mode);
            ADMP521TRegMap.Add(Test00);
            ADMP521TRegMap.Add(Test01);
            ADMP521TRegMap.Add(Test02);
            ADMP521TRegMap.Add(Test03);
            ADMP521TRegMap.Add(Test04);
            ADMP521TRegMap.Add(Test05);
            ADMP521TRegMap.Add(Test06);
            ADMP521TRegMap.Add(SoftReset);
            #endregion Add all registers to regMap
        }

        private void SetDefaultValue()
        {           
            Test_mode.RegValue = 0x00; //Write 0x51 to enter test mode.   
            Test00.RegValue = 0x00;
            Test01.RegValue = 0x00;
            Test02.RegValue = 0x00;
            Test03.RegValue = 0x51;
            Test04.RegValue = 0x00;
            Test05.RegValue = 0x00;
            Test06.RegValue = 0x00;
            SoftReset.RegValue = 0x00; //Write 0xAA to reset all registers.
        }

        /// <summary>
        /// Get the register by register's address.
        /// </summary>
        /// <param name="_regAddress">Int type register address.</param>
        /// <returns></returns>
        public Register this[int index]
        {
            get 
            {
                try
                { return ADMP521TRegMap[index]; }
                catch 
                { return null; }
            }
        }

        /// <summary>
        /// Get the register by register's name.
        /// </summary>
        /// <param name="_regName">string type register name.</param>
        /// <returns></returns>
        public Register this[string _name]
        {
            get
            {
                try
                {return ADMP521TRegMap[_name];}
                catch
                {return null;}
            }
        }
    }
}
