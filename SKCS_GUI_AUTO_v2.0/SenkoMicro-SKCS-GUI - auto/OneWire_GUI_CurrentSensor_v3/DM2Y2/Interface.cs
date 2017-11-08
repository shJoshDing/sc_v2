using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Reflection;
using System.Collections;


namespace ADI.DMY2
{
    /// <summary>
    /// Public interfaces for Speaker protection project.
    /// </summary>
    public partial class SPKProtection
    {
        InitDeviceByGUID myDevice;// = new InitDeviceByGUID(deviceGUID);
        private Guid GUID_CLASS_BF_USB_BULKADI = new Guid("eb8322c5-8b49-4feb-ae6e-c99b2b232045");
        private Guid DMY2_GUID = new Guid("89982a59-5eea-45aa-af97-52ec351018c2");
        //private Guid DMY2_GUID = new Guid("89982a59-5eea-45aa-af97-52ec351018c2");
        //private Guid DMY2_GUID = new Guid("2d314667-539c-4a4a-85e4-0ed6be34c5fe");
        private Guid deviceGUID = Guid.Empty;

        private static int len_writeBuf = 128;      //128 个int数据
        private byte[] writeBuffer = new byte[len_writeBuf * 4];         //Buffer for write operation.
        private int[] writeBuffer_current = new int[len_writeBuf];
        private double[] writeBuffer_last = new double[len_writeBuf];
        private int[] readBuffer = new int[len_writeBuf];
        GeneralMethods func = new GeneralMethods();
        //private byte[] ReadBuffer;                          //Buffer for read operation.
        double coeff_log10To2 = Math.Log(10, 2) / 20d;

        private static bool deviceBusy = false;
        public static bool DeviceBusy
        {
            get { return deviceBusy; }
        }

        #region 开放的接口
        #region 1.连接设备
        /// <summary>
        /// 连接默认GUID设备（"eb8322c5-8b49-4feb-ae6e-c99b2b232045"），并获取读写句柄。
        /// </summary>
        /// <returns>返回连接成功与否，True：连接成功。False：失败。</returns>
        public bool ConnectDevice()
        {
            bool result = false;
            //deviceGUID = DMY2_GUID;
            deviceGUID = GUID_CLASS_BF_USB_BULKADI;
            myDevice = new InitDeviceByGUID(deviceGUID);
            result = myDevice.DeviceInitializer();
            return result;
        }

        /// <summary>
        /// 更具_guid连接设备（"eb8322c5-8b49-4feb-ae6e-c99b2b232045"），并获取读写句柄。
        /// </summary>
        /// <param name="_guid">Format：XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX</param>
        /// <returns>返回连接成功与否，True：连接成功。False：失败。</returns>
        public bool ConnectDevice(string _guid)
        {
            bool result = false;
            try
            {
                deviceGUID = new Guid(_guid);
            }
            catch
            {
                return false;
            }
            myDevice = new InitDeviceByGUID(deviceGUID);
            result = myDevice.DeviceInitializer();
            return result;
        }
        #endregion 1.连接设备

        #region 2.获取设备信息
        /// <summary>
        /// 获取设备数
        /// </summary>
        public int TotalDevices
        { get { return myDevice.TotalDevices; } }

        /// <summary>
        /// 获取当前GUID
        /// </summary>
        public string GUID
        { get { return deviceGUID.ToString(); } }

        #endregion 2.获取设备信息

        #region 3.读写设备
        private bool Write(byte[] writeData)
        {
            for (int i = 0; i < writeData.Length; i++)
            {
                writeBuffer[i] = writeData[i];
            }
            return myDevice.UsbReportWrite(writeBuffer);
        }

        private bool Write(int[] writeData)
        {
            //for (int i = 0; i < writeData.Length; i++)
            //{
            //    writeBuffer[i] = writeData[i];
            //}
            return myDevice.UsbReportWrite(writeData);
        }

        public bool Read(byte[] readData)
        {
            //for (int i = 0; i < writeData.Length; i++)
            //{
            //    writeBuffer[i] = writeData[i];
            //}
            //Console.WriteLine(myDevice.UsbReportWrite(readData));

            //byte[] readData = new byte[length];
            //myDevice.UsbReportRead(readData);

            //return readData;
            return myDevice.UsbReportRead(readData);
        }

        public bool Read(int[] readData)
        {
            return myDevice.UsbReportRead(readData);
        }

        public bool Write(USB_COMMAND command, double[] writeData, string drcFPFormate)
        {
            switch (command)
            {
                case USB_COMMAND.HW_Start:
                    return StartHardWare();
                case USB_COMMAND.HW_Stop:
                    return StopHardWare();
                case USB_COMMAND.Write_Param:
                    return ParamWrite(writeData, drcFPFormate);
                case USB_COMMAND.Read_Status:
                    return StatusRead(writeData);
                case USB_COMMAND.Training:
                    return Training();
                case USB_COMMAND.ModuleEnable:
                    return ModuleEnadleSetting(writeData);
                default:
                    return false;
            }
        }

        //public bool Write(ADMP521_USB_COMMAND command, uint[] writeData)
        //{          
        //    return myDevice.UsbReportWrite(writeData);            
        //}

        //public bool Read(uint[] writeData)
        //{
        //    return StatusRead(writeData);
        //}

        private void Test(double[] data, double[] result)
        {
            DateTime dt = DateTime.Now;
            #region Int32 转换为bytes
            //int[] testInt = new int[500];
            //for (int i = 0; i < testInt.Length; i++)
            //    testInt[i] = 1 << i;

            //byte[] testByte = new byte[testInt.Length * 4];
            //for (int i = 0; i < testInt.Length; i++)
            //{
            //    BitConverter.GetBytes(testInt[i]).CopyTo(testByte, i * 4);
            //}
            #endregion Int32 转换为bytes
            //double[] willCon = data;
            #region Test 定点数和浮点数相互转换

            ConvertFixPToFloatP(result, ConvertFloatPToFixP(data, "5.27"), 0, data.Length, "");
            #endregion

            Console.WriteLine((DateTime.Now - dt).TotalMilliseconds);
        }
        #endregion 读写设备

        #region 4.数据处理
        //#region 参数定义
        //private int delta = 50;                             //图形上网格长度。
        //private int deltaY_QPoint;                          //Q点和附近点默认的Y值差值。
        //private Point[] BasePoints = new Point[32];         //基准曲线点，由白噪声扫描得出。
        //private int[] Y_BasePoints = new int[32];           //基准曲线点Y坐标。
        //private int defaultIndexQ;                          //Q点默认的Index.
        //List<Point> PLForFix = new List<System.Drawing.Point> { };      //提供一个处理数据中间结果的容器。 
        //#endregion 参数定义

        //#region 方法
        //private void Calibrate()
        //{
        //    GetDefaultQPointX();
        //}

        //private int GetQPointXZone(int[] _YValue_BaseP)
        //{
        //    int index;
        //    int max;
        //    if (_YValue_BaseP == null)
        //    {
        //        max = Y_BasePoints.Max();
        //        for (index = 0; index < Y_BasePoints.Length; index++)
        //            if (max == Y_BasePoints[index])
        //            {
        //                defaultIndexQ = index;
        //                break;
        //            }
        //    }
        //    else
        //    {
        //        max = _YValue_BaseP.Max();
        //        for (index = 0; index < _YValue_BaseP.Length; index++)
        //            if (max == _YValue_BaseP[index])
        //                break;
        //    }

        //    return index;
        //}

        //private int GetDefaultQPointX()
        //{
        //    int index;
        //    int max;
        //    //int[] YValue = (int[])Y_BasePoints.Clone();
        //    max = Y_BasePoints.Max();
        //    if (max == 0)
        //        return -1;
        //    for (index = 0; index < Y_BasePoints.Length; index++)
        //        if (max == Y_BasePoints[index])
        //        {
        //            defaultIndexQ = index;
        //            break;
        //        }
        //    int temp1 = Y_BasePoints[index - 1];
        //    int temp2 = Y_BasePoints[index + 1];
        //    deltaY_QPoint = (temp1 > temp2) ? (max - temp1) : (max - temp2);

        //    return index;
        //}

        ///// <summary>
        ///// 获取谐振点。
        ///// </summary>
        ///// <param name="_YValue">待处理的数据。</param>
        ///// <returns></returns>
        //private int GetQPointX(int[] _YValue)
        //{
        //    int[] YValue = new int[_YValue.Length];
        //    _YValue.CopyTo(YValue, 0);

        //    //int max = YValue.Max();
        //    int index_Max = GetMaxIndex(YValue);
        //    int max = YValue[index_Max];
        //    YValue[index_Max] = 0;
        //    int index_secMax = GetMaxIndex(YValue);
        //    int secMax = YValue[index_secMax];
        //    YValue[index_secMax] = 0;
        //    int index_thirdMax =  GetMaxIndex(YValue);
        //    int thirdMax = YValue[index_thirdMax];

        //    //todo 根据这三个值判断Qpoint， 一样大则根据 defaultIndexQ 修正Qpoint
        //    //1. 如果最大值明显比第二大的值高出很多，则返回Max的索引。
        //    if((max - secMax) >  (deltaY_QPoint * 0.75)) 
        //        return index_Max;
        //    else if((max - thirdMax) < (deltaY_QPoint * 0.75))   //三个差不多大
        //    {
        //        int[] tempIndex = new int[]{index_Max,index_secMax,index_thirdMax};
        //        Array.Sort(tempIndex);
        //        //2. 如果三个点连续
        //        if(tempIndex[0] + tempIndex[2] == 2 * tempIndex[1])
        //        {
        //            _YValue[tempIndex[1]] = secMax + deltaY_QPoint;
        //            return tempIndex[1];
        //        }
        //        //3. 如果三个点不是连续的
        //        if(max - secMax > 0.5 * deltaY_QPoint)
        //            _YValue[index_Max] = _YValue[index_secMax] + deltaY_QPoint;
        //    }
        //    else  //Max和secMax差值不是很大时
        //    {
        //        //if max 和secMax 差值很小，max 和 thirdMax差值较大
        //        if((max - secMax) < (deltaY_QPoint * 0.4))
        //        {
        //            if(Math.Abs(index_Max - defaultIndexQ) > Math.Abs(index_secMax - defaultIndexQ))
        //            {
        //                _YValue[index_secMax] = _YValue[index_thirdMax] + deltaY_QPoint;
        //                _YValue[index_Max] = _YValue[index_thirdMax];
        //                return index_secMax;
        //            }
        //            else
        //            {
        //                _YValue[index_Max] = _YValue[index_thirdMax] + deltaY_QPoint;
        //                _YValue[index_secMax] = _YValue[index_thirdMax];
        //                return index_Max;
        //            }

        //        }
        //        else
        //        {
        //            _YValue[index_Max] = _YValue[index_secMax] + deltaY_QPoint;
        //            return index_Max;
        //        }

        //    }

        //    return 0;
        //}

        ////private int 

        //private Point[] FixPoints(int[] YValue)
        //{
        //    PLForFix.Clear();
        //    List<Point> tempPoint = new List<System.Drawing.Point> { };
        //    List<double> tempSlop = new List<double> { };
        //    int temp;
        //    int currentMax;
        //    int currentMaxIndex;
        //    int currentIndex;       //当前fix的数据Index

        //    #region 1.获取当前数据的Qindex
        //    currentMaxIndex = GetQPointX(YValue);
        //    currentMax = YValue[currentMaxIndex];

        //    //int QIndex_Default = GetQPointX(YValue);
        //    //if(QIndex_Default <=0)
        //    //    return null;
        //    //currentMax = YValue[QIndex_Default - 1];
        //    //currentMaxIndex = QIndex_Default - 1;
        //    //for (int i = 0; i < 2; i++)
        //    //{
        //    //    if (YValue[QIndex_Default + i] > currentMax)
        //    //    {
        //    //        currentMax = YValue[QIndex_Default + i];
        //    //        currentMaxIndex = QIndex_Default + i;
        //    //    }
        //    //}
        //    #endregion

        //    #region 2.处理前半条曲线
        //    tempPoint.Add(new Point(currentMaxIndex, currentMax));
        //    for (int i = currentMaxIndex - 1; i > 0; i--)
        //    {

        //    }
        //    #endregion 


        //    #region 3.处理后半天曲线
        //    #endregion


        //    #region
        //    #endregion
        //}
        //#endregion 方法

        public void Calibrate(int[] basePY)
        {
            //Array.Copy(basePY, Y_BasePoints, basePY.Length);
            basePY.CopyTo(Y_BasePoints, 0);
            GetDefaultQPointX();
        }

        #endregion 4.数据处理

        #endregion 开放的接口

        #region 测试接口
        /// <summary>
        /// 获取一组测试波形数据。
        /// </summary>
        /// <param name="length">返回数组长度</param>
        /// <param name="amplitude">波形最大幅值</param>
        /// <param name="wavType">波形种类，有Line，Sawtooth，Sine，Square</param>
        /// <returns>返回length长度的byte数组。</returns>
        public byte[] Test_GetWav(int length, byte amplitude, TestWavType wavType)
        {
            byte[] result = new byte[length];
            int index;
            switch (wavType)
            {
                case TestWavType.Line:
                    for (index = 0; index < length; index++)
                    {
                        result[index] = amplitude;
                    }
                    break;
                case TestWavType.Sawtooth:
                    int unit = amplitude / 4;
                    for (index = 0; index < length / 4; index++)
                    {
                        if (index % 2 == 0)
                            for (int i = 0; i < 4; i++)
                            {
                                result[index * 4 + i] = (byte)(unit * (i + 1));
                            }
                        else
                            for (int i = 3; i >= 0; i--)
                            {
                                result[index * 4 + i] = (byte)(unit * (4 - i));
                            }
                    }
                    break;
                case TestWavType.Sine:
                    for (index = 0; index < length; index++)
                    {
                        result[index] = (byte)(Math.Sin(Math.PI / index) * amplitude / 2 + amplitude / 2);
                    }
                    break;
                case TestWavType.Square:
                    for (index = 0; index < length / 4; index++)
                    {
                        if (index % 2 == 0)
                            for (int i = 0; i < 4; i++)
                            {
                                result[index * 4 + i] = 0;
                            }
                        else
                            for (int i = 0; i < 4; i++)
                            {
                                result[index * 4 + i] = amplitude;
                            }
                    }
                    break;
                default:
                    break;
            }
            return result;
        }

        public double[] Test_ReadFromText(string path)
        {
            double[] result;
            List<double> readDataX = new List<double> { };
            List<double> readDataY = new List<double> { };

            string str_regFilePath = path;//ofd.FileName;
            StreamReader strRead = File.OpenText(str_regFilePath);
            strRead.ReadLine();
            string[] strArray_readLine = new string[2];

            #region Read data from local file
            int index = 0;
            while (!strRead.EndOfStream)
            {
                strArray_readLine = strRead.ReadLine().Trim().Split(',');
                try
                {
                    readDataX.Add(Convert.ToDouble(strArray_readLine[0]));
                    readDataY.Add(Convert.ToDouble(strArray_readLine[1]));
                }
                catch
                {
                    strRead.Close();
                    MessageBox.Show("Read error, please check the data file.");
                    return null;
                }
            }
            strRead.Close();
            result = new double[2 * readDataX.Count];
            readDataX.CopyTo(result, 0);
            readDataY.CopyTo(result, readDataX.Count);
            return result;
            #endregion

        }

        /// <summary>
        /// 波形种类
        /// </summary>
        public enum TestWavType
        {
            Line,
            Sawtooth,
            Sine,
            Square
        }
        #endregion 测试接口
    }

    /// <summary>
    /// Private interfaces for Speaker protection project.
    /// </summary>
    public partial class SPKProtection
    {
        #region Private 方法
        #region 4.数据处理
        #region 参数定义
        private int delta = 50;                             //图形上网格长度。
        private int deltaY_QPoint;                          //Q点和附近点默认的Y值差值。
        private int deltaY_StartPoint;                      //曲线起始处的DeltaY
        private Point[] BasePoints = new Point[32];         //基准曲线点，由白噪声扫描得出。
        private int[] Y_BasePoints = new int[32];           //基准曲线点Y坐标。
        private int defaultIndexQ;                          //Q点默认的Index.
        private double disRatioAdjaPoint = 0.7;             //相邻两个点距的长度比，此比例为长得那部分。
        private double riseRatio = 1.05;                    //相邻delta增长系数
        public double RiseRatio
        {
            set { riseRatio = value; }
            get { return riseRatio; }
        }
        private double margin = 0.9;                        //判断
        List<int> LPForFix = new List<int> { };      //提供一个处理数据中间结果的容器。 
        #endregion 参数定义

        #region 方法
        //private void Calibrate()
        //{
        //    GetDefaultQPointX();
        //}

        /// <summary>
        /// 获取最大值的索引，如果有多个最大值，则取第一个索引。
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private int GetMaxIndex(int[] data)
        {
            int max = data.Max();
            for (int index = 0; index < data.Length; index++)
                if (max == data[index])
                {
                    return index;
                }

            return -1;
        }

        private int GetQPointXZone(int[] _YValue_BaseP)
        {
            int index;
            int max;
            if (_YValue_BaseP == null)
            {
                max = Y_BasePoints.Max();
                for (index = 0; index < Y_BasePoints.Length; index++)
                    if (max == Y_BasePoints[index])
                    {
                        defaultIndexQ = index;
                        break;
                    }
            }
            else
            {
                max = _YValue_BaseP.Max();
                for (index = 0; index < _YValue_BaseP.Length; index++)
                    if (max == _YValue_BaseP[index])
                        break;
            }

            return index;
        }

        /// <summary>
        /// 根据白噪声扫出来的波形获取默认的Q点位置
        /// </summary>
        /// <returns></returns>
        private int GetDefaultQPointX()
        {
            int index;
            int max;
            //int[] YValue = (int[])Y_BasePoints.Clone();
            max = Y_BasePoints.Max();
            if (max == 0)
                return -1;
            for (index = 0; index < Y_BasePoints.Length; index++)
                if (max == Y_BasePoints[index])
                {
                    defaultIndexQ = index;
                    break;
                }
            int temp1 = Y_BasePoints[index - 1];
            int temp2 = Y_BasePoints[index + 1];
            //默认Q点附近的Y差值，取Q点左右差值小的。
            deltaY_QPoint = (temp1 > temp2) ? (max - temp1) : (max - temp2);

            //起始处的deltaY取第一个，第二个差值的平均值。
            deltaY_StartPoint = (Y_BasePoints[2] - Y_BasePoints[0]) / 2;

            return index;
        }

        /// <summary>
        /// 获取谐振点。
        /// </summary>
        /// <param name="_YValue">待处理的数据。</param>
        /// <returns></returns>
        private int GetQPointX(int[] _YValue)
        {
            int[] YValue = new int[_YValue.Length];
            _YValue.CopyTo(YValue, 0);

            //int max = YValue.Max();
            int index_Max = GetMaxIndex(YValue);
            int max = YValue[index_Max];
            YValue[index_Max] = 0;
            int index_secMax = GetMaxIndex(YValue);
            int secMax = YValue[index_secMax];
            YValue[index_secMax] = 0;
            int index_thirdMax = GetMaxIndex(YValue);
            int thirdMax = YValue[index_thirdMax];

            //todo 根据这三个值判断Qpoint， 一样大则根据 defaultIndexQ 修正Qpoint
            //1. 如果最大值明显比第二大的值高出很多，则返回Max的索引。
            if ((max - secMax) > (deltaY_QPoint * 0.75))
                return index_Max;
            else if ((max - thirdMax) < (deltaY_QPoint * 0.75))   //三个差不多大
            {
                int[] tempIndex = new int[] { index_Max, index_secMax, index_thirdMax };
                Array.Sort(tempIndex);
                //2. 如果三个点连续
                if (tempIndex[0] + tempIndex[2] == 2 * tempIndex[1])
                {
                    _YValue[tempIndex[1]] = secMax + deltaY_QPoint;
                    return tempIndex[1];
                }
                //3. 如果三个点不是连续的
                if (max - secMax > 0.5 * deltaY_QPoint)
                {
                    _YValue[index_Max] = _YValue[index_secMax] + deltaY_QPoint;
                    return index_Max;
                }
            }
            else  //Max和secMax差值不是很大时
            {
                //if max 和secMax 差值很小，max 和 thirdMax差值较大
                if ((max - secMax) < (deltaY_QPoint * 0.3))
                {
                    //if (Math.Abs(index_Max - defaultIndexQ) > Math.Abs(index_secMax - defaultIndexQ))
                    //{
                    //    _YValue[index_secMax] = _YValue[index_thirdMax] + deltaY_QPoint;
                    //    _YValue[index_Max] = _YValue[index_thirdMax];
                    //    return index_secMax;
                    //}
                    //else
                    //{
                    //    _YValue[index_Max] = _YValue[index_thirdMax] + deltaY_QPoint;
                    //    _YValue[index_secMax] = _YValue[index_thirdMax];
                    //    return index_Max;
                    //}
                    return index_Max;
                }
                else
                {
                    _YValue[index_Max] = _YValue[index_secMax] + deltaY_QPoint;
                    return index_Max;
                }

            }

            return index_Max;
        }

        private int GetBaseY(int[] _YValue)
        {
            int temp = 0;
            int[] YValue = new int[_YValue.Length];
            _YValue.CopyTo(YValue, 0);
            Array.Sort(YValue);         //先排序（从小到大）
            //取倒数第三个至第八个值，取平均值设为Base值
            //for (int i = 3; i < 8; i++)
            //{
            //    temp += YValue[i];
            //}
            //temp /= 5;
            temp = YValue[0];
            return temp;
        }

        /// <summary>
        /// 根据单调减进行第一遍过滤。
        /// </summary>
        /// <param name="_YValue">需要Fix的数据源</param>
        /// <param name="maxIndex">Q点Index</param>
        /// <param name="baseY">Y值得基准线</param>
        /// <param name="b_firstHalf">是否修复前半个波形（True：前半，False：后半）</param>
        /// <param name="LP">修复好的点</param>
        private void FixByMonotony(int[] _YValue, int maxIndex, int baseY, bool b_firstHalf, List<int> LP)
        {
            LP.Clear();
            //LP.Add(new Point(maxIndex, _YValue[maxIndex]));
            LP.Add(_YValue[maxIndex]);
            int tempIndex = 0;
            int tempDelta;
            if (b_firstHalf)
            {
                #region First half
                for (int i = maxIndex - 1; i >= 0; i--)
                {
                    //如果出现单调性突变的值则进行修正
                    if (_YValue[i] >= _YValue[i + 1])
                    {
                        #region 1.与前一个点i+1取平均值，如果平均值小于点i+2，则在平均值附近生产两个点
                        if (i + 2 <= maxIndex)
                        {
                            //判断是否比点i+2还大，如果大，则根据点i之后的点处理点i，不fix点i+1
                            if (_YValue[i] > _YValue[i + 2])
                            {
                                while (i - tempIndex >= 0)
                                {
                                    if ((_YValue[i - tempIndex] < _YValue[i + 1]) && (_YValue[i - tempIndex] >= baseY))
                                    {
                                        tempDelta = _YValue[i + 1] - _YValue[i - tempIndex];
                                        _YValue[i] = _YValue[i + 1] - Convert.ToInt32(Math.Pow(disRatioAdjaPoint, tempIndex) * tempDelta);
                                        //LP.Add(new Point(i, _YValue[i]));
                                        LP.Add(_YValue[i]);
                                        tempIndex = 0;
                                        break;
                                    }
                                    else if (i - tempIndex == 0)
                                    {
                                        if (i == 0)
                                        {
                                            _YValue[i] = _YValue[i + 1] - Convert.ToInt32((_YValue[i + 2] - _YValue[i + 1]) * disRatioAdjaPoint);
                                        }
                                        else
                                        {
                                            tempDelta = _YValue[i + 1] - baseY;
                                            _YValue[i] = _YValue[i + 1] - Convert.ToInt32(Math.Pow(disRatioAdjaPoint, tempIndex) * tempDelta);
                                        }
                                        LP.Add(_YValue[i]);
                                        //LP.Add(new Point(i, _YValue[i]));
                                        tempIndex = 0;
                                        break;
                                    }
                                    tempIndex++;
                                }
                            }
                            //只比点i+1大，则取平均值，将i+1增大，同时将点i减小(不取平均值)
                            else
                            {
                                ////fix 点i+1
                                //int tempAvg = (_YValue[i] + _YValue[i + 1]) / 2;
                                //tempDelta = _YValue[i + 2] - tempAvg;
                                //_YValue[i + 1] = _YValue[i + 2] - Convert.ToInt32(disRatioAdjaPoint * tempDelta);
                                //LP.RemoveAt(LP.Count - 1 );                                        
                                //LP.Add(_YValue[i + 1]);

                                //fix 点i
                                while (i - tempIndex >= 0)
                                {
                                    if ((_YValue[i - tempIndex] < _YValue[i + 1]) && (_YValue[i - tempIndex] >= baseY))
                                    {
                                        tempDelta = _YValue[i + 1] - _YValue[i - tempIndex];
                                        _YValue[i] = _YValue[i + 1] - Convert.ToInt32(Math.Pow(disRatioAdjaPoint, tempIndex) * tempDelta);
                                        //LP.Add(new Point(i, _YValue[i]));
                                        LP.Add(_YValue[i]);
                                        tempIndex = 0;
                                        break;
                                    }
                                    else if (i - tempIndex == 0)
                                    {
                                        tempDelta = _YValue[i + 1] - baseY;
                                        _YValue[i] = _YValue[i + 1] - Convert.ToInt32(Math.Pow(disRatioAdjaPoint, tempIndex) * tempDelta);
                                        LP.Add(_YValue[i]);
                                        //LP.Add(new Point(i, _YValue[i]));
                                        tempIndex = 0;
                                        break;
                                    }
                                    tempIndex++;
                                }
                            }
                        }
                        #endregion
                        #region 2.如果为和Max相邻的点
                        else
                        {
                            _YValue[i] = _YValue[i + 1] - deltaY_QPoint;
                            LP.Add(_YValue[i]);
                            //LP.Add(new Point(i, _YValue[i]));
                        }
                        #endregion
                    }
                    // 如果比基准值还小，则需稍微调大点
                    else if (_YValue[i] < baseY)
                    {
                        while (i - tempIndex >= 0)
                        {
                            if ((_YValue[i - tempIndex] < _YValue[i + 1]) && (_YValue[i - tempIndex] >= baseY))
                            {
                                tempDelta = _YValue[i + 1] - _YValue[i - tempIndex];
                                _YValue[i] = _YValue[i + 1] - Convert.ToInt32(Math.Pow(disRatioAdjaPoint, tempIndex) * tempDelta);
                                LP.Add(_YValue[i]);
                                //LP.Add(new Point(i, _YValue[i]));
                                tempIndex = 0;
                                break;
                            }
                            else if (i - tempIndex == 0)
                            {
                                tempDelta = _YValue[i + 1] - baseY;
                                _YValue[i] = _YValue[i + 1] - Convert.ToInt32(Math.Pow(disRatioAdjaPoint, tempIndex) * tempDelta);
                                LP.Add(_YValue[i]);
                                //LP.Add(new Point(i, _YValue[i]));
                                tempIndex = 0;
                                break;
                            }
                            tempIndex++;
                        }
                    }
                    //不做处理
                    else
                        LP.Add(_YValue[i]);
                    //LP.Add(new Point(i, _YValue[i]));
                }
                #endregion First half
            }
            else
            {
                #region Second half
                for (int i = maxIndex + 1; i < _YValue.Length; i++)
                {
                    //如果出现单调性突变的值则进行修正
                    if (_YValue[i] >= _YValue[i - 1])
                    {
                        #region 1.与前一个点i-1取平均值，如果平均值小于点i-2，则在平均值附近生产两个点
                        if (i - 2 >= maxIndex)
                        {
                            //判断是否比点i-2还大，如果大，则根据点i之后的点处理点i，不fix点i-1
                            if (_YValue[i] > _YValue[i - 2])
                            {
                                while (i + tempIndex < _YValue.Length)
                                {
                                    if ((_YValue[i + tempIndex] < _YValue[i - 1]) && (_YValue[i + tempIndex] >= baseY))
                                    {
                                        tempDelta = _YValue[i - 1] - _YValue[i + tempIndex];
                                        _YValue[i] = _YValue[i - 1] - Convert.ToInt32(Math.Pow(disRatioAdjaPoint, tempIndex) * tempDelta);
                                        LP.Add(_YValue[i]);
                                        //LP.Add(new Point(i, _YValue[i]));
                                        tempIndex = 0;
                                        break;
                                    }
                                    else if (i + tempIndex == _YValue.Length - 1)
                                    {
                                        tempDelta = _YValue[i - 1] - baseY;
                                        _YValue[i] = _YValue[i - 1] - Convert.ToInt32(Math.Pow(disRatioAdjaPoint, tempIndex) * tempDelta);
                                        //LP.Add(new Point(i, _YValue[i]));
                                        LP.Add(_YValue[i]);
                                        tempIndex = 0;
                                        break;
                                    }
                                    tempIndex++;
                                }
                            }
                            //只比点i-1大，则取平均值，将i-1增大，同时将点i减小
                            else
                            {
                                //fix 点i-1
                                //int tempAvg = (_YValue[i] + _YValue[i - 1]) / 2;
                                //tempDelta = _YValue[i - 2] - tempAvg;
                                //_YValue[i - 1] = _YValue[i - 2] - Convert.ToInt32(disRatioAdjaPoint * tempDelta);
                                //LP.RemoveAt(LP.Count - 1);
                                //LP.Add(_YValue[i - 1]);

                                //fix 点i
                                while (i + tempIndex < _YValue.Length)
                                {
                                    if ((_YValue[i + tempIndex] < _YValue[i - 1]) && (_YValue[i + tempIndex] >= baseY))
                                    {
                                        tempDelta = _YValue[i - 1] - _YValue[i + tempIndex];
                                        _YValue[i] = _YValue[i - 1] - Convert.ToInt32(Math.Pow(disRatioAdjaPoint, tempIndex) * tempDelta);
                                        LP.Add(_YValue[i]);
                                        //LP.Add(new Point(i, _YValue[i]));
                                        tempIndex = 0;
                                        break;
                                    }
                                    else if (i + tempIndex == _YValue.Length - 1)
                                    {
                                        tempDelta = _YValue[i - 1] - baseY;
                                        _YValue[i] = _YValue[i - 1] - Convert.ToInt32(Math.Pow(disRatioAdjaPoint, tempIndex) * tempDelta);
                                        LP.Add(_YValue[i]);
                                        //LP.Add(new Point(i, _YValue[i]));
                                        tempIndex = 0;
                                        break;
                                    }
                                    tempIndex++;
                                }
                            }
                        }
                        #endregion
                        #region 2.如果为和Max相邻的点
                        else
                        {
                            _YValue[i] = _YValue[i - 1] - deltaY_QPoint;
                            LP.Add(_YValue[i]);
                            //LP.Add(new Point(i, _YValue[i]));
                        }
                        #endregion
                    }
                    // 如果比基准值还小，则需稍微调大点
                    else if (_YValue[i] < baseY)
                    {
                        while (i + tempIndex < _YValue.Length)
                        {
                            if ((_YValue[i + tempIndex] < _YValue[i - 1]) && (_YValue[i + tempIndex] >= baseY))
                            {
                                tempDelta = _YValue[i - 1] - _YValue[i + tempIndex];
                                _YValue[i] = _YValue[i - 1] - Convert.ToInt32(Math.Pow(disRatioAdjaPoint, tempIndex) * tempDelta);
                                LP.Add(_YValue[i]);
                                //LP.Add(new Point(i, _YValue[i]));
                                tempIndex = 0;
                                break;
                            }
                            else if (i - tempIndex == _YValue.Length - 1)
                            {
                                tempDelta = _YValue[i - 1] - baseY;
                                _YValue[i] = _YValue[i - 1] - Convert.ToInt32(Math.Pow(disRatioAdjaPoint, tempIndex) * tempDelta);
                                LP.Add(_YValue[i]);
                                //LP.Add(new Point(i, _YValue[i]));
                                tempIndex = 0;
                                break;
                            }
                            tempIndex++;
                        }
                    }
                    //不做处理
                    else
                        //LP.Add(new Point(i, _YValue[i]));
                        LP.Add(_YValue[i]);
                }
                #endregion Second half
            }
        }

        private int[] FixBySlope(List<int> _LP, int maxIndex)
        {
            int[] willFix = _LP.ToArray();
            int indexStart;
            int indexEnd;
            int referMaxDelta;
            List<int> deltaY = new List<int> { };
            List<int> deltaY_Refer = new List<int> { };

            #region First half  Method：从起始往Q点Fix
            ////如果Q点附近的点很靠近Q点，则只fix到点maxIndex-1。
            //if (willFix[maxIndex] - willFix[maxIndex - 1] < deltaY_QPoint * 0.75)
            //    indexEnd = maxIndex - 1;
            //else
            //    indexEnd = maxIndex;
            ////Calc the deltaY of the first half curve.
            //for (indexStart = 1; indexStart < indexEnd; indexStart++)
            //{
            //    deltaY.Add(willFix[indexStart] - willFix[indexStart - 1]);
            //}

            ////fix the deltaY
            //int temp;
            //for (int i = 1; i < deltaY.Count; i++)
            //{
            //    //斜率减小才进行修正
            //    if (deltaY[i] < deltaY[i - 1])
            //    {
            //        //如果是第一个Delta则直接生产新delta
            //        if( i == 1)
            //        {
            //            temp = deltaY[i] + deltaY[i-1];
            //            //deltaY[i - 1] = Convert.ToInt32( temp * (1 - disRatioAdjaPoint));
            //            //deltaY[i] = temp - deltaY[i - 1];
            //            deltaY[i - 1] = 3;
            //            deltaY[i] = 8;
            //            deltaY[i + 1] = temp - deltaY[i] - deltaY[i - 1];
            //        }
            //        //否则再根据上上个delta判断
            //        else
            //        {
            //            temp = deltaY[i] + deltaY[i - 1];
            //            //重新计算的上一个delta后比上上个D大，则直接引用此次计算结果。
            //            if(temp * (1 - disRatioAdjaPoint) >= deltaY[i -2])
            //            {
            //                deltaY[i - 1] = Convert.ToInt32( temp * (1 - disRatioAdjaPoint));
            //                deltaY[i] = temp - deltaY[i - 1];
            //            }
            //            //否则根据上上一个D的1.1倍生产此Delta
            //            else
            //            {
            //                deltaY[i - 1] = Convert.ToInt32(deltaY[i - 2] * riseRatio);
            //                if(temp - deltaY[i - 1] >= deltaY[i - 1]) //剩下的delta i 比 D i-1 大则直接用此结果
            //                    deltaY[i] = temp - deltaY[i - 1];
            //                else// 否则根据D i-1 的1.1倍计算D i，如果i+1存在，则在i+1上减去多余的部分
            //                {
            //                    if (i == deltaY.Count - 1)
            //                    {
            //                        deltaY[i - 1] = Convert.ToInt32(temp * 0.4);
            //                        deltaY[i] = temp - deltaY[i - 1];
            //                    }
            //                    else
            //                    {
            //                        deltaY[i] = Convert.ToInt32(deltaY[i - 1] * riseRatio);
            //                        deltaY[i + 1] -= temp - deltaY[i - 1] - deltaY[i];
            //                    }
            //                }
            //            }
            //        }
            //    }
            //}

            ////Fix Y Value of each point on the first half curve.修正总幅值增量的80%即可
            //temp = Convert.ToInt32((willFix[indexEnd - 1 ] - willFix[0]) * 0.8);
            //int tempSum = 0;
            //for (int i = 0; i < deltaY.Count; i++)
            //{
            //    willFix[i + 1] = willFix[i] + deltaY[i];
            //}

            //deltaY_Refer.Clear();
            //deltaY_Refer.Add(willFix[indexEnd] - willFix[indexEnd - 1]);
            //for (int i = deltaY.Count - 1; i >= 0; i--)
            //{
            //    tempSum += deltaY[i];
            //    deltaY_Refer.Add(deltaY[i]);
            //    if (tempSum >= temp)
            //        break;
            //}

            ////打印deltaY
            //for (int i = 0; i < deltaY.Count; i++)
            //{
            //    Console.Write("delta{0}:{1} ",i,deltaY[i]);
            //}
            //Console.WriteLine();
            //referMaxDelta = deltaY[deltaY.Count - 1];
            //deltaY.Clear();
            #endregion First half  Method：从起始往Q点Fix

            #region First half  Method：从Q点往前Fix
            //如果Q点附近的点很靠近Q点，则只fix到点maxIndex-1。
            if (willFix[maxIndex] - willFix[maxIndex - 1] < deltaY_QPoint * 0.75)
                indexStart = maxIndex - 1;
            else
                indexStart = maxIndex;
            //Calc the deltaY of the first half curve.
            for (; indexStart > 0; indexStart--)
            {
                deltaY.Add(willFix[indexStart] - willFix[indexStart - 1]);
            }

            //fix the deltaY（根据：斜率应该单减）
            int temp;
            int tempIndex;
            #region fix deltaY
            for (int i = 1; i < deltaY.Count; i++)
            {
                //斜率增大才进行修正(出现error point)
                if (deltaY[i] > deltaY[i - 1])
                {
                    tempIndex = 1;
                    //如果是第一个Delta则……
                    if (i == 1)
                    {
                        //temp = deltaY[i] + deltaY[i - 1];
                        ////deltaY[i - 1] = Convert.ToInt32( temp * (1 - disRatioAdjaPoint));
                        ////deltaY[i] = temp - deltaY[i - 1];
                        //deltaY[i - 1] = 3;
                        //deltaY[i] = 8;
                        //deltaY[i + 1] = temp - deltaY[i] - deltaY[i - 1];
                    }
                    //i!= 1，则修正0~i的delta
                    else
                    {
                        if (deltaY[i - 1] > 10)
                        {
                            temp = deltaY[i];
                            while (i - tempIndex >= 0)
                            {
                                if ((deltaY[i] < deltaY[i - tempIndex]) | (i - tempIndex == 0))
                                {
                                    //temp += deltaY[i - tempIndex];
                                    for (int j = tempIndex - 1; j > 0; j--)
                                    {
                                        deltaY[i - j] = Convert.ToInt32(temp * disRatioAdjaPoint);
                                        temp -= deltaY[i - j];
                                    }
                                    deltaY[i] = temp;
                                    break;
                                }

                                temp += deltaY[i - tempIndex];
                                tempIndex++;
                            }
                        }
                        else
                        {
                            temp = deltaY[i] + deltaY[i - 1];
                            deltaY[i - 1] = Convert.ToInt32(temp * disRatioAdjaPoint);
                            deltaY[i] = temp - deltaY[i - 1];
                        }


                        //temp = deltaY[i] + deltaY[i - 1];
                        ////重新计算的上一个delta后比上上个D大，则直接引用此次计算结果。
                        //if (temp * (1 - disRatioAdjaPoint) >= deltaY[i - 2])
                        //{
                        //    deltaY[i - 1] = Convert.ToInt32(temp * (1 - disRatioAdjaPoint));
                        //    deltaY[i] = temp - deltaY[i - 1];
                        //}
                        ////否则根据上上一个D的1.1倍生产此Delta
                        //else
                        //{
                        //    deltaY[i - 1] = Convert.ToInt32(deltaY[i - 2] * riseRatio);
                        //    if (temp - deltaY[i - 1] >= deltaY[i - 1]) //剩下的delta i 比 D i-1 大则直接用此结果
                        //        deltaY[i] = temp - deltaY[i - 1];
                        //    else// 否则根据D i-1 的1.1倍计算D i，如果i+1存在，则在i+1上减去多余的部分
                        //    {
                        //        if (i == deltaY.Count - 1)
                        //        {
                        //            deltaY[i - 1] = Convert.ToInt32(temp * 0.4);
                        //            deltaY[i] = temp - deltaY[i - 1];
                        //        }
                        //        else
                        //        {
                        //            deltaY[i] = Convert.ToInt32(deltaY[i - 1] * riseRatio);
                        //            deltaY[i + 1] -= temp - deltaY[i - 1] - deltaY[i];
                        //        }
                        //    }
                        //}
                    }
                }
            }
            #endregion fix deltaY

            //Fix Y Value of each point on the first half curve.修正总幅值增量的80%即可
            temp = Convert.ToInt32((willFix[indexStart] - willFix[0]) * 0.8);
            //int tempSum = 0;
            for (int i = 0; i < deltaY.Count; i++)
            {
                willFix[i + 1] = willFix[i] + deltaY[deltaY.Count - 1 - i];
            }

            //deltaY_Refer.Clear();
            //deltaY_Refer.Add(willFix[indexStart] - willFix[indexStart - 1]);
            //for (int i = deltaY.Count - 1; i >= 0; i--)
            //{
            //    tempSum += deltaY[i];
            //    deltaY_Refer.Add(deltaY[i]);
            //    if (tempSum >= temp)
            //        break;
            //}

            //打印deltaY
            for (int i = 0; i < deltaY.Count; i++)
            {
                Console.Write("delta{0}:{1} ", i, deltaY[i]);
            }
            Console.WriteLine();

            //referMaxDelta = deltaY[deltaY.Count - 1];
            deltaY.Clear();
            #endregion First half  Method：从Q点往前Fix

            #region Second Half Method: 从Q点开始往后休
            //如果Q点附近的点很靠近Q点，则只fix到点maxIndex-1。
            if (willFix[maxIndex] - willFix[maxIndex + 1] < deltaY_QPoint * 0.75)
                indexStart = maxIndex + 1;
            else
                indexStart = maxIndex;
            tempIndex = indexStart;
            //Calc the deltaY of the Second half curve.
            for (; tempIndex < willFix.Length - 1; tempIndex++)
            {
                deltaY.Add(willFix[tempIndex] - willFix[tempIndex + 1]);
            }

            #region fix deltaY（根据：斜率（负数）应该单增）
            for (int i = 1; i < deltaY.Count; i++)
            {
                //斜率增大才进行修正(出现error point)
                if (deltaY[i] > deltaY[i - 1])
                {
                    tempIndex = 1;
                    //如果是第一个Delta则……
                    if (i == 1)
                    {
                        //temp = deltaY[i] + deltaY[i - 1];
                        ////deltaY[i - 1] = Convert.ToInt32( temp * (1 - disRatioAdjaPoint));
                        ////deltaY[i] = temp - deltaY[i - 1];
                        //deltaY[i - 1] = 3;
                        //deltaY[i] = 8;
                        //deltaY[i + 1] = temp - deltaY[i] - deltaY[i - 1];
                    }
                    //i!= 1，则修正0~i的delta
                    else
                    {
                        if (deltaY[i - 1] > 10 | i <= 6)
                        {
                            temp = deltaY[i];
                            while (i - tempIndex >= 0)
                            {
                                if ((deltaY[i] < deltaY[i - tempIndex]) | (i - tempIndex == 0))
                                {
                                    //temp += deltaY[i - tempIndex];
                                    for (int j = tempIndex - 1; j > 0; j--)
                                    {
                                        deltaY[i - j] = Convert.ToInt32(temp * disRatioAdjaPoint);
                                        temp -= deltaY[i - j];
                                    }
                                    deltaY[i] = temp;
                                    break;
                                }

                                temp += deltaY[i - tempIndex];
                                tempIndex++;
                            }
                        }
                        else
                        {
                            temp = deltaY[i] + deltaY[i - 1];
                            deltaY[i - 1] = Convert.ToInt32(temp * disRatioAdjaPoint);
                            deltaY[i] = temp - deltaY[i - 1];
                        }
                    }
                }
            }
            #endregion fix deltaY

            #region Fix the deltaY by reference deltaY
            //for (int i = 0; i < deltaY_Refer.Count; i++)
            //{
            //    temp = Convert.ToInt32(deltaY_Refer[i] * 0.9);
            //    if (deltaY[i] < temp)
            //    {
            //        deltaY[i + 1] -= temp - deltaY[i];
            //        deltaY[i] = temp;
            //    }
            //    temp = Convert.ToInt32(deltaY_Refer[i] * 1.1);
            //    if (deltaY[i] > temp)
            //    {
            //        deltaY[i + 1] += deltaY[i] - temp;
            //        deltaY[i] = temp;
            //    }
            //}

            ////Fix the error points behind last fixd
            //int count = 0;
            //tempSum = 0;
            //if (deltaY[deltaY_Refer.Count] < 2)
            //{
            //    for (int i = deltaY_Refer.Count; i < deltaY.Count; i++)
            //    {
            //        count += deltaY[i];
            //        if (count >= deltaY[deltaY_Refer.Count - 1] * 0.8)
            //        {
            //            for (tempIndex = deltaY_Refer.Count; tempIndex < i; tempIndex++)
            //            {
            //                deltaY[tempIndex] = Convert.ToInt32(count * Math.Pow(disRatioAdjaPoint, (i - tempIndex)));
            //                count -= deltaY[tempIndex];
            //                //tempSum += deltaY[tempIndex];
            //            };
            //            deltaY[tempIndex] = count;// -tempSum;
            //            break;
            //        }
            //    }
            //}

            #endregion

            #region fix the remainder deltaY;
            //tempIndex = 0;
            //for (int i = 1; i < deltaY.Count; i++)
            //{
            //    //斜率减小才进行修正
            //    if (deltaY[i] > deltaY[i - 1])
            //    {
            //        //如果是最后一个Delta则直接根据D i-1生产新delta
            //        if (i == deltaY.Count - 1)
            //        {
            //            deltaY[i] = deltaY[i - 1];
            //            //temp = deltaY[i] + deltaY[i - 1];
            //            //deltaY[i - 1] = Convert.ToInt32(temp * (1 - disRatioAdjaPoint));
            //            //deltaY[i] = temp - deltaY[i - 1];
            //        }
            //        else if( i == 1)
            //        {
            //            temp = deltaY[i] + deltaY[i - 1];
            //            deltaY[i - 1] = Convert.ToInt32(temp * disRatioAdjaPoint);
            //            deltaY[i] = temp - deltaY[i - 1];
            //        }
            //        //否则再根据上上个delta判断
            //        else
            //        {
            //            temp = deltaY[i] + deltaY[i - 1];
            //            if (temp * disRatioAdjaPoint > deltaY[i - 2])
            //            {
            //                deltaY[i - 1] = Convert.ToInt32(deltaY[i - 2] * 0.9);
            //                if (temp - deltaY[i - 1] > deltaY[i - 1])
            //                {
            //                    deltaY[i] = Convert.ToInt32(deltaY[i - 1] * 0.9);
            //                    if (i + 1 < deltaY.Count)
            //                        deltaY[i + 1] += temp - deltaY[i - 1] - deltaY[i];
            //                }
            //            }
            //        }
            //    }
            //}

            #endregion fix the remainder deltaY;

            //Fix Y Value of each point on the first half curve.
            for (int i = 0; i < deltaY.Count; i++)
            {
                willFix[indexStart + i + 1] = willFix[indexStart + i] - deltaY[i];
            }
            deltaY.Clear();

            #region Comment out
            ////Calc the deltaY of the Second half curve.
            //for (indexStart = willFix.Length - 10; indexStart > indexEnd; indexStart--)
            //{
            //    deltaY.Add(willFix[indexStart - 1] - willFix[indexStart]);
            //}

            ////fix the deltaY
            //for (int i = 1; i < deltaY.Count; i++)
            //{
            //    //斜率减小才进行修正
            //    if (deltaY[i] < deltaY[i - 1])
            //    {
            //        //如果是第一个Delta则直接生产新delta
            //        if (i == 1)
            //        {
            //            temp = deltaY[i] + deltaY[i - 1];
            //            deltaY[i - 1] = Convert.ToInt32(temp * (1 - disRatioAdjaPoint));
            //            deltaY[i] = temp - deltaY[i - 1];
            //        }
            //        //否则再根据上上个delta判断
            //        else
            //        {
            //            temp = deltaY[i] + deltaY[i - 1];
            //            //重新计算的上一个delta后比上上个D大，则直接引用此次计算结果。
            //            if (temp * (1 - disRatioAdjaPoint) >= deltaY[i - 2])
            //            {
            //                deltaY[i - 1] = Convert.ToInt32(temp * (1 - disRatioAdjaPoint));
            //                deltaY[i] = temp - deltaY[i - 1];
            //            }
            //            //否则根据上上一个D的1.1倍生产此Delta
            //            else
            //            {
            //                deltaY[i - 1] = Convert.ToInt32(deltaY[i - 2] * riseRatio);
            //                if (temp - deltaY[i - 1] >= deltaY[i - 1]) //剩下的delta i 比 D i-1 大则直接用此结果
            //                    deltaY[i] = temp - deltaY[i - 1];
            //                else// 否则根据D i-1 的1.1倍计算D i，如果i+1存在，则在i+1上减去多余的部分
            //                {
            //                    if (i == deltaY.Count - 1)
            //                        deltaY[i] = temp - deltaY[i - 1];
            //                    else
            //                    {
            //                        deltaY[i] = Convert.ToInt32(deltaY[i - 1] * riseRatio);
            //                        deltaY[i + 1] -= temp - deltaY[i - 1] - deltaY[i];
            //                    }
            //                }
            //            }
            //        }
            //    }
            //}

            //Fix Y Value of each point on the first half curve.
            //for (int i = 0; i < deltaY.Count; i++)
            //{
            //    willFix[willFix.Length - 10 - 1 - i] = willFix[willFix.Length - 10 - i] + deltaY[i];
            //}
            //deltaY.Clear();
            #endregion Comment out

            #endregion

            return willFix;
        }

        public int[] FixPointsY(int[] YValue, bool b_fixBySlop)
        {
            LPForFix.Clear();
            List<int> tempPoint = new List<int> { };
            List<double> tempSlop = new List<double> { };
            //int temp;
            int currentMax;
            int currentBaseY;          //标记本曲线的基准线
            int currentMaxIndex;
            //int currentIndex;         //当前fix的数据Index

            #region 1.获取当前数据的Qindex 和最基准线
            currentMaxIndex = GetQPointX(YValue);
            currentMax = YValue[currentMaxIndex];

            currentBaseY = GetBaseY(YValue);
            //int QIndex_Default = GetQPointX(YValue);
            //if(QIndex_Default <=0)
            //    return null;
            //currentMax = YValue[QIndex_Default - 1];
            //currentMaxIndex = QIndex_Default - 1;
            //for (int i = 0; i < 2; i++)
            //{
            //    if (YValue[QIndex_Default + i] > currentMax)
            //    {
            //        currentMax = YValue[QIndex_Default + i];
            //        currentMaxIndex = QIndex_Default + i;
            //    }
            //}
            #endregion

            #region 2.处理前半条曲线
            //tempPoint.Add(new Point(currentMaxIndex, currentMax));
            //// a）根据单调减进行第一遍过滤。
            //for (int i = currentMaxIndex - 1; i > 0; i--)
            //{
            //    //if(YValue[i] < YValue[i + 1])
            //    //    tem
            //}
            FixByMonotony(YValue, currentMaxIndex, currentBaseY, true, tempPoint);
            for (int i = tempPoint.Count - 1; i > 0; i--)
            {
                LPForFix.Add(tempPoint[i]);
            }
            #endregion


            #region 3.处理后半条曲线
            FixByMonotony(YValue, currentMaxIndex, currentBaseY, false, tempPoint);
            //LPForFix.AddRange(tempPoint);
            for (int i = 0; i < tempPoint.Count; i++)
            {
                LPForFix.Add(tempPoint[i]);
            }
            #endregion


            #region
            #endregion

            if (b_fixBySlop)
                return FixBySlope(LPForFix, currentMaxIndex);
            else
                return LPForFix.ToArray();
        }
        #endregion 方法

        #region DSP Write
        /// <summary>
        /// 启动硬件DSP
        /// </summary>
        /// <returns></returns>
        private bool StartHardWare()
        {
            writeBuffer_current[Index.command] = (int)USB_COMMAND.HW_Start;

            if (!Device_Idle())
                return false;
            deviceBusy = true; //独占设备
            bool result = myDevice.UsbReportWrite(writeBuffer_current);
            deviceBusy = false;
            return result;
        }

        /// <summary>
        /// 停止硬件
        /// </summary>
        /// <returns></returns>
        private bool StopHardWare()
        {
            writeBuffer_current[Index.command] = (int)USB_COMMAND.HW_Stop;

            if (!Device_Idle())
                return false;
            deviceBusy = true; //独占设备
            bool result = myDevice.UsbReportWrite(writeBuffer_current);
            deviceBusy = false;
            return result;
        }

        /// <summary>
        /// DSP算法Training
        /// </summary>
        /// <returns></returns>
        private bool Training()
        {
            //GeneralMethods.ConvIntArrToByteArr((int)USB_COMMAND.Training).CopyTo(writeBuffer, Index.command);
            writeBuffer_current[Index.command] = (int)USB_COMMAND.Training;
            //int count = 0;
            //while (deviceBusy && count++ < 20)
            //{ Thread.Sleep(5); }
            if (!Device_Idle())
                return false;
            deviceBusy = true; //独占设备
            bool result = myDevice.UsbReportWrite(writeBuffer_current);
            deviceBusy = false;
            return result;
        }

        /// <summary>
        /// 设置各个模块的Enable属性。
        /// </summary>
        /// <returns></returns>
        private bool ModuleEnadleSetting(double[] _data)
        {
            writeBuffer_current[Index.command] = (int)USB_COMMAND.ModuleEnable;
            writeBuffer_current[Index.Module_EQ_MDRC] = Convert.ToInt32(_data[Index.Module_EQ_MDRC]);
            writeBuffer_current[Index.Module_CrossOver] = Convert.ToInt32(_data[Index.Module_CrossOver]);
            writeBuffer_current[Index.Module_LowDRC] = Convert.ToInt32(_data[Index.Module_LowDRC]);
            writeBuffer_current[Index.Module_MidDRC] = Convert.ToInt32(_data[Index.Module_MidDRC]);
            writeBuffer_current[Index.Module_HighDRC] = Convert.ToInt32(_data[Index.Module_HighDRC]);
            writeBuffer_current[Index.Module_HPF2] = Convert.ToInt32(_data[Index.Module_HPF2]);
            writeBuffer_current[Index.Module_Lpf1] = Convert.ToInt32(_data[Index.Module_Lpf1]);
            writeBuffer_current[Index.Module_HPF1] = Convert.ToInt32(_data[Index.Module_HPF1]);

            if (!Device_Idle())
                return false;
            deviceBusy = true; //独占设备
            bool result = myDevice.UsbReportWrite(writeBuffer_current);
            deviceBusy = false;
            return result;
        }

        /// <summary>
        /// DSP算法所有参数的update
        /// </summary>
        /// <param name="_data">要写的param</param>
        /// <param name="formate">DRC参数的定点数格式，默认8.24</param>
        /// <returns></returns>
        private bool ParamWrite(double[] _data, string _formate)
        {
            //0. 判断是否需要进行写操作，如果和上次写的参数一样，则此次写操作不执行。
            //if (IfChanged(writeBuffer_last, _data))
            //    return false;
            //func.Write_LocalFile("c:\\TestMage.txt", _data);

            //StreamWriter strWrite;
            //try
            //{
            //    strWrite = File.CreateText("c:\\TestMage.txt");
            //}
            //catch
            //{
            //    return false;
            //}

            //string str_willWrite = "";
            //for (int i = 0; i < _data.Length; i++)
            //{
            //    str_willWrite += _data[i].ToString() + ",\r\n";
            //}
            //strWrite.Write(str_willWrite);
            //strWrite.Close();

            //1. command
            writeBuffer_current[Index.command] = (int)USB_COMMAND.Write_Param;

            /*Convert log10 to log2 dB*/
            ConvertToLog2(_data, Index.drc1_base, coeff_log10To2); //drc1
            ConvertToLog2(_data, Index.drcL_base, coeff_log10To2); //drc2-low
            ConvertToLog2(_data, Index.drcM_base, coeff_log10To2); //drc3-mid
            ConvertToLog2(_data, Index.drcH_base, coeff_log10To2); //drc4-high
            ConvertToLog2(_data, Index.drc5_base, coeff_log10To2); //drc5

            /*转换定点数*/
            //DRC1  4- 20     
            writeBuffer_current[Index.drc1_base + DrcParamIndex.ERROR] = ConvertFloatPToFixP(_data[Index.drc1_base + DrcParamIndex.ERROR], _formate);
            writeBuffer_current[Index.drc1_base + DrcParamIndex.MUTE_LEVEL] = ConvertFloatPToFixP(_data[Index.drc1_base + DrcParamIndex.MUTE_LEVEL], _formate);
            writeBuffer_current[Index.drc1_base + DrcParamIndex.LT] = ConvertFloatPToFixP(_data[Index.drc1_base + DrcParamIndex.LT], _formate);
            writeBuffer_current[Index.drc1_base + DrcParamIndex.CT] = ConvertFloatPToFixP(_data[Index.drc1_base + DrcParamIndex.CT], _formate);
            writeBuffer_current[Index.drc1_base + DrcParamIndex.ET] = ConvertFloatPToFixP(_data[Index.drc1_base + DrcParamIndex.ET], _formate);
            writeBuffer_current[Index.drc1_base + DrcParamIndex.NT] = ConvertFloatPToFixP(_data[Index.drc1_base + DrcParamIndex.NT], _formate);
            writeBuffer_current[Index.drc1_base + DrcParamIndex.SMAX] = ConvertFloatPToFixP(_data[Index.drc1_base + DrcParamIndex.SMAX], _formate);
            writeBuffer_current[Index.drc1_base + DrcParamIndex.SMIN] = ConvertFloatPToFixP(_data[Index.drc1_base + DrcParamIndex.SMIN], _formate);
            writeBuffer_current[Index.drc1_base + DrcParamIndex.GAIN_ADJ] = ConvertFloatPToFixP(_data[Index.drc1_base + DrcParamIndex.GAIN_ADJ], _formate);
            writeBuffer_current[Index.drc1_base + DrcParamIndex.CS] = ConvertFloatPToFixP(_data[Index.drc1_base + DrcParamIndex.CS], _formate);
            writeBuffer_current[Index.drc1_base + DrcParamIndex.ES] = ConvertFloatPToFixP(_data[Index.drc1_base + DrcParamIndex.ES], _formate);
            writeBuffer_current[Index.drc1_base + DrcParamIndex.TA] = ConvertFloatPToFixP(1 - Math.Pow(2, _data[Index.drc1_base + DrcParamIndex.TA]), "1.31");
            writeBuffer_current[Index.drc1_base + DrcParamIndex.TR] = ConvertFloatPToFixP(Math.Pow(2, _data[Index.drc1_base + DrcParamIndex.TR]), "1.31");
            writeBuffer_current[Index.drc1_base + DrcParamIndex.AT] = ConvertFloatPToFixP(Math.Pow(2, _data[Index.drc1_base + DrcParamIndex.AT]), _formate);
            writeBuffer_current[Index.drc1_base + DrcParamIndex.RT] = ConvertFloatPToFixP(Math.Pow(2, _data[Index.drc1_base + DrcParamIndex.RT]), _formate);
            writeBuffer_current[Index.drc1_base + DrcParamIndex.TAV] = ConvertFloatPToFixP(Math.Pow(2, _data[Index.drc1_base + DrcParamIndex.TAV]), "1.31");
            writeBuffer_current[Index.drc1_base + DrcParamIndex.RIPPLE_TH] = ConvertFloatPToFixP(8 * Math.Pow(2, _data[Index.drc1_base + DrcParamIndex.RIPPLE_TH]), _formate);
            for (int i = 0; i < 4; i++)
                writeBuffer_current[Index.drc1_base + DrcParamIndex.HD_SAMPLE + i] = Convert.ToInt32(_data[Index.drc1_base + DrcParamIndex.HD_SAMPLE + i]);

            //DRC2-low  25-41       
            writeBuffer_current[Index.drcL_base + DrcParamIndex.ERROR] = ConvertFloatPToFixP(_data[Index.drcL_base + DrcParamIndex.ERROR], _formate);
            writeBuffer_current[Index.drcL_base + DrcParamIndex.MUTE_LEVEL] = ConvertFloatPToFixP(_data[Index.drcL_base + DrcParamIndex.MUTE_LEVEL], _formate);
            writeBuffer_current[Index.drcL_base + DrcParamIndex.LT] = ConvertFloatPToFixP(_data[Index.drcL_base + DrcParamIndex.LT], _formate);
            writeBuffer_current[Index.drcL_base + DrcParamIndex.CT] = ConvertFloatPToFixP(_data[Index.drcL_base + DrcParamIndex.CT], _formate);
            writeBuffer_current[Index.drcL_base + DrcParamIndex.ET] = ConvertFloatPToFixP(_data[Index.drcL_base + DrcParamIndex.ET], _formate);
            writeBuffer_current[Index.drcL_base + DrcParamIndex.NT] = ConvertFloatPToFixP(_data[Index.drcL_base + DrcParamIndex.NT], _formate);
            writeBuffer_current[Index.drcL_base + DrcParamIndex.SMAX] = ConvertFloatPToFixP(_data[Index.drcL_base + DrcParamIndex.SMAX], _formate);
            writeBuffer_current[Index.drcL_base + DrcParamIndex.SMIN] = ConvertFloatPToFixP(_data[Index.drcL_base + DrcParamIndex.SMIN], _formate);
            writeBuffer_current[Index.drcL_base + DrcParamIndex.GAIN_ADJ] = ConvertFloatPToFixP(_data[Index.drcL_base + DrcParamIndex.GAIN_ADJ], _formate);
            writeBuffer_current[Index.drcL_base + DrcParamIndex.CS] = ConvertFloatPToFixP(_data[Index.drcL_base + DrcParamIndex.CS], _formate);
            writeBuffer_current[Index.drcL_base + DrcParamIndex.ES] = ConvertFloatPToFixP(_data[Index.drcL_base + DrcParamIndex.ES], _formate);
            writeBuffer_current[Index.drcL_base + DrcParamIndex.TA] = ConvertFloatPToFixP(1 - Math.Pow(2, _data[Index.drcL_base + DrcParamIndex.TA]), "1.31");
            writeBuffer_current[Index.drcL_base + DrcParamIndex.TR] = ConvertFloatPToFixP(Math.Pow(2, _data[Index.drcL_base + DrcParamIndex.TR]), "1.31");
            writeBuffer_current[Index.drcL_base + DrcParamIndex.AT] = ConvertFloatPToFixP(Math.Pow(2, _data[Index.drcL_base + DrcParamIndex.AT]), _formate);
            writeBuffer_current[Index.drcL_base + DrcParamIndex.RT] = ConvertFloatPToFixP(Math.Pow(2, _data[Index.drcL_base + DrcParamIndex.RT]), _formate);
            writeBuffer_current[Index.drcL_base + DrcParamIndex.TAV] = ConvertFloatPToFixP(Math.Pow(2, _data[Index.drcL_base + DrcParamIndex.TAV]), "1.31");
            writeBuffer_current[Index.drcL_base + DrcParamIndex.RIPPLE_TH] = ConvertFloatPToFixP(8 * Math.Pow(2, _data[Index.drcL_base + DrcParamIndex.RIPPLE_TH]), _formate);
            for (int i = 0; i < 4; i++)
                writeBuffer_current[Index.drcL_base + DrcParamIndex.HD_SAMPLE + i] = Convert.ToInt32(_data[Index.drcL_base + DrcParamIndex.HD_SAMPLE + i]);

            //DRC3-mid  46-62       
            writeBuffer_current[Index.drcM_base + DrcParamIndex.ERROR] = ConvertFloatPToFixP(_data[Index.drcM_base + DrcParamIndex.ERROR], _formate);
            writeBuffer_current[Index.drcM_base + DrcParamIndex.MUTE_LEVEL] = ConvertFloatPToFixP(_data[Index.drcM_base + DrcParamIndex.MUTE_LEVEL], _formate);
            writeBuffer_current[Index.drcM_base + DrcParamIndex.LT] = ConvertFloatPToFixP(_data[Index.drcM_base + DrcParamIndex.LT], _formate);
            writeBuffer_current[Index.drcM_base + DrcParamIndex.CT] = ConvertFloatPToFixP(_data[Index.drcM_base + DrcParamIndex.CT], _formate);
            writeBuffer_current[Index.drcM_base + DrcParamIndex.ET] = ConvertFloatPToFixP(_data[Index.drcM_base + DrcParamIndex.ET], _formate);
            writeBuffer_current[Index.drcM_base + DrcParamIndex.NT] = ConvertFloatPToFixP(_data[Index.drcM_base + DrcParamIndex.NT], _formate);
            writeBuffer_current[Index.drcM_base + DrcParamIndex.SMAX] = ConvertFloatPToFixP(_data[Index.drcM_base + DrcParamIndex.SMAX], _formate);
            writeBuffer_current[Index.drcM_base + DrcParamIndex.SMIN] = ConvertFloatPToFixP(_data[Index.drcM_base + DrcParamIndex.SMIN], _formate);
            writeBuffer_current[Index.drcM_base + DrcParamIndex.GAIN_ADJ] = ConvertFloatPToFixP(_data[Index.drcM_base + DrcParamIndex.GAIN_ADJ], _formate);
            writeBuffer_current[Index.drcM_base + DrcParamIndex.CS] = ConvertFloatPToFixP(_data[Index.drcM_base + DrcParamIndex.CS], _formate);
            writeBuffer_current[Index.drcM_base + DrcParamIndex.ES] = ConvertFloatPToFixP(_data[Index.drcM_base + DrcParamIndex.ES], _formate);
            writeBuffer_current[Index.drcM_base + DrcParamIndex.TA] = ConvertFloatPToFixP(1 - Math.Pow(2, _data[Index.drcM_base + DrcParamIndex.TA]), "1.31");
            writeBuffer_current[Index.drcM_base + DrcParamIndex.TR] = ConvertFloatPToFixP(Math.Pow(2, _data[Index.drcM_base + DrcParamIndex.TR]), "1.31");
            writeBuffer_current[Index.drcM_base + DrcParamIndex.AT] = ConvertFloatPToFixP(Math.Pow(2, _data[Index.drcM_base + DrcParamIndex.AT]), _formate);
            writeBuffer_current[Index.drcM_base + DrcParamIndex.RT] = ConvertFloatPToFixP(Math.Pow(2, _data[Index.drcM_base + DrcParamIndex.RT]), _formate);
            writeBuffer_current[Index.drcM_base + DrcParamIndex.TAV] = ConvertFloatPToFixP(Math.Pow(2, _data[Index.drcM_base + DrcParamIndex.TAV]), "1.31");
            writeBuffer_current[Index.drcM_base + DrcParamIndex.RIPPLE_TH] = ConvertFloatPToFixP(8 * Math.Pow(2, _data[Index.drcM_base + DrcParamIndex.RIPPLE_TH]), _formate);
            for (int i = 0; i < 4; i++)
                writeBuffer_current[Index.drcM_base + DrcParamIndex.HD_SAMPLE + i] = Convert.ToInt32(_data[Index.drcM_base + DrcParamIndex.HD_SAMPLE + i]);

            //DRC4-high  67-83   
            writeBuffer_current[Index.drcH_base + DrcParamIndex.ERROR] = ConvertFloatPToFixP(_data[Index.drcH_base + DrcParamIndex.ERROR], _formate);
            writeBuffer_current[Index.drcH_base + DrcParamIndex.MUTE_LEVEL] = ConvertFloatPToFixP(_data[Index.drcH_base + DrcParamIndex.MUTE_LEVEL], _formate);
            writeBuffer_current[Index.drcH_base + DrcParamIndex.LT] = ConvertFloatPToFixP(_data[Index.drcH_base + DrcParamIndex.LT], _formate);
            writeBuffer_current[Index.drcH_base + DrcParamIndex.CT] = ConvertFloatPToFixP(_data[Index.drcH_base + DrcParamIndex.CT], _formate);
            writeBuffer_current[Index.drcH_base + DrcParamIndex.ET] = ConvertFloatPToFixP(_data[Index.drcH_base + DrcParamIndex.ET], _formate);
            writeBuffer_current[Index.drcH_base + DrcParamIndex.NT] = ConvertFloatPToFixP(_data[Index.drcH_base + DrcParamIndex.NT], _formate);
            writeBuffer_current[Index.drcH_base + DrcParamIndex.SMAX] = ConvertFloatPToFixP(_data[Index.drcH_base + DrcParamIndex.SMAX], _formate);
            writeBuffer_current[Index.drcH_base + DrcParamIndex.SMIN] = ConvertFloatPToFixP(_data[Index.drcH_base + DrcParamIndex.SMIN], _formate);
            writeBuffer_current[Index.drcH_base + DrcParamIndex.GAIN_ADJ] = ConvertFloatPToFixP(_data[Index.drcH_base + DrcParamIndex.GAIN_ADJ], _formate);
            writeBuffer_current[Index.drcH_base + DrcParamIndex.CS] = ConvertFloatPToFixP(_data[Index.drcH_base + DrcParamIndex.CS], _formate);
            writeBuffer_current[Index.drcH_base + DrcParamIndex.ES] = ConvertFloatPToFixP(_data[Index.drcH_base + DrcParamIndex.ES], _formate);
            writeBuffer_current[Index.drcH_base + DrcParamIndex.TA] = ConvertFloatPToFixP(1 - Math.Pow(2, _data[Index.drcH_base + DrcParamIndex.TA]), "1.31");
            writeBuffer_current[Index.drcH_base + DrcParamIndex.TR] = ConvertFloatPToFixP(Math.Pow(2, _data[Index.drcH_base + DrcParamIndex.TR]), "1.31");
            writeBuffer_current[Index.drcH_base + DrcParamIndex.AT] = ConvertFloatPToFixP(Math.Pow(2, _data[Index.drcH_base + DrcParamIndex.AT]), _formate);
            writeBuffer_current[Index.drcH_base + DrcParamIndex.RT] = ConvertFloatPToFixP(Math.Pow(2, _data[Index.drcH_base + DrcParamIndex.RT]), _formate);
            writeBuffer_current[Index.drcH_base + DrcParamIndex.TAV] = ConvertFloatPToFixP(Math.Pow(2, _data[Index.drcH_base + DrcParamIndex.TAV]), "1.31");
            writeBuffer_current[Index.drcH_base + DrcParamIndex.RIPPLE_TH] = ConvertFloatPToFixP(8 * Math.Pow(2, _data[Index.drcH_base + DrcParamIndex.RIPPLE_TH]), _formate);
            for (int i = 0; i < 4; i++)
                writeBuffer_current[Index.drcH_base + DrcParamIndex.HD_SAMPLE + i] = Convert.ToInt32(_data[Index.drcH_base + DrcParamIndex.HD_SAMPLE + i]);

            //DRC5   88-104
            writeBuffer_current[Index.drc5_base + DrcParamIndex.ERROR] = ConvertFloatPToFixP(_data[Index.drc5_base + DrcParamIndex.ERROR], _formate);
            writeBuffer_current[Index.drc5_base + DrcParamIndex.MUTE_LEVEL] = ConvertFloatPToFixP(_data[Index.drc5_base + DrcParamIndex.MUTE_LEVEL], _formate);
            writeBuffer_current[Index.drc5_base + DrcParamIndex.LT] = ConvertFloatPToFixP(_data[Index.drc5_base + DrcParamIndex.LT], _formate);
            writeBuffer_current[Index.drc5_base + DrcParamIndex.CT] = ConvertFloatPToFixP(_data[Index.drc5_base + DrcParamIndex.CT], _formate);
            writeBuffer_current[Index.drc5_base + DrcParamIndex.ET] = ConvertFloatPToFixP(_data[Index.drc5_base + DrcParamIndex.ET], _formate);
            writeBuffer_current[Index.drc5_base + DrcParamIndex.NT] = ConvertFloatPToFixP(_data[Index.drc5_base + DrcParamIndex.NT], _formate);
            writeBuffer_current[Index.drc5_base + DrcParamIndex.SMAX] = ConvertFloatPToFixP(_data[Index.drc5_base + DrcParamIndex.SMAX], _formate);
            writeBuffer_current[Index.drc5_base + DrcParamIndex.SMIN] = ConvertFloatPToFixP(_data[Index.drc5_base + DrcParamIndex.SMIN], _formate);
            writeBuffer_current[Index.drc5_base + DrcParamIndex.GAIN_ADJ] = ConvertFloatPToFixP(_data[Index.drc5_base + DrcParamIndex.GAIN_ADJ], _formate);
            writeBuffer_current[Index.drc5_base + DrcParamIndex.CS] = ConvertFloatPToFixP(_data[Index.drc5_base + DrcParamIndex.CS], _formate);
            writeBuffer_current[Index.drc5_base + DrcParamIndex.ES] = ConvertFloatPToFixP(_data[Index.drc5_base + DrcParamIndex.ES], _formate);
            writeBuffer_current[Index.drc5_base + DrcParamIndex.TA] = ConvertFloatPToFixP(1 - Math.Pow(2, _data[Index.drc5_base + DrcParamIndex.TA]), "1.31");
            writeBuffer_current[Index.drc5_base + DrcParamIndex.TR] = ConvertFloatPToFixP(Math.Pow(2, _data[Index.drc5_base + DrcParamIndex.TR]), "1.31");
            writeBuffer_current[Index.drc5_base + DrcParamIndex.AT] = ConvertFloatPToFixP(Math.Pow(2, _data[Index.drc5_base + DrcParamIndex.AT]), _formate);
            writeBuffer_current[Index.drc5_base + DrcParamIndex.RT] = ConvertFloatPToFixP(Math.Pow(2, _data[Index.drc5_base + DrcParamIndex.RT]), _formate);
            writeBuffer_current[Index.drc5_base + DrcParamIndex.TAV] = ConvertFloatPToFixP(Math.Pow(2, _data[Index.drc5_base + DrcParamIndex.TAV]), "1.31");
            writeBuffer_current[Index.drc5_base + DrcParamIndex.RIPPLE_TH] = ConvertFloatPToFixP(8 * Math.Pow(2, _data[Index.drc5_base + DrcParamIndex.RIPPLE_TH]), _formate);
            for (int i = 0; i < 4; i++)
                writeBuffer_current[Index.drc5_base + DrcParamIndex.HD_SAMPLE + i] = Convert.ToInt32(_data[Index.drc5_base + DrcParamIndex.HD_SAMPLE + i]);

            //HPF
            writeBuffer_current[Index.HPF1_index] = Convert.ToInt32(_data[Index.HPF1_index]);  //index->不需要转为定点数
            writeBuffer_current[Index.HPF2_index] = Convert.ToInt32(_data[Index.HPF2_index]);  //index->不需要转为定点数
            writeBuffer_current[Index.HPF2_offset] = Convert.ToInt32(_data[Index.HPF2_offset]); //index->不需要转为定点数
            writeBuffer_current[Index.LPF1_index] = Convert.ToInt32(_data[Index.LPF1_index]);   //index->不需要转为定点数
            writeBuffer_current[Index.CrossOver_BW] = Convert.ToInt32(_data[Index.CrossOver_BW]);  //index->不需要转为定点数
            writeBuffer_current[Index.CrossOver_Fr] = Convert.ToInt32(_data[Index.CrossOver_Fr]);  //index->不需要转为定点数
            writeBuffer_current[Index.CrossOver_C1] = Convert.ToInt32(_data[Index.CrossOver_C1]);  //index->不需要转为定点数
            writeBuffer_current[Index.CrossOver_C2] = Convert.ToInt32(_data[Index.CrossOver_C2]);  //index->不需要转为定点数

            //Others
            //if (_data[Index.VolumeCtrl] >= 16)
            //    _data[Index.VolumeCtrl] = 15.99999; //保护volumeCtrl 参数不溢出<- DSP 参数采用5p27，最大值为15.99999……
            writeBuffer_current[Index.VolumeCtrl] = ConvertFloatPToFixP(_data[Index.VolumeCtrl], "5.27");
            writeBuffer_current[Index.Rrdc_T_low] = ConvertFloatPToFixP(_data[Index.Rrdc_T_low], "5.27");
            writeBuffer_current[Index.Rrdc_T_High] = ConvertFloatPToFixP(_data[Index.Rrdc_T_High], "5.27");
            writeBuffer_current[Index.Gain_coef_Low] = ConvertFloatPToFixP(_data[Index.Gain_coef_Low], "5.27");
            writeBuffer_current[Index.Gain_ref_max_Low] = ConvertFloatPToFixP(_data[Index.Gain_ref_max_Low], "5.27");
            writeBuffer_current[Index.Gain_coef_Mid] = ConvertFloatPToFixP(_data[Index.Gain_coef_Mid], "5.27");
            writeBuffer_current[Index.Gain_ref_max_Mid] = ConvertFloatPToFixP(_data[Index.Gain_ref_max_Mid], "5.27");
            writeBuffer_current[Index.Gain_coef_High] = ConvertFloatPToFixP(_data[Index.Gain_coef_High], "5.27");
            writeBuffer_current[Index.Gain_ref_max_High] = ConvertFloatPToFixP(_data[Index.Gain_ref_max_High], "5.27");

            //切换系数开关
            writeBuffer_current[Index.IfAutoChange_f0] = Convert.ToInt32(_data[Index.IfAutoChange_f0]);
            writeBuffer_current[Index.IfAutoChange_T] = Convert.ToInt32(_data[Index.IfAutoChange_T]);

            writeBuffer_last = (double[])_data.Clone(); //记录本次写操作的数据

            int count = 0;
            while (deviceBusy && count++ < 20)
            { Thread.Sleep(5); }
            deviceBusy = true; //独占设备
            bool result = myDevice.UsbReportWrite(writeBuffer_current);
            deviceBusy = false;
            return result;
        }

        /// <summary>
        /// DSP状态读取
        /// </summary>
        /// <param name="_param"></param>
        /// <returns></returns>
        public bool StatusRead(double[] _param)
        {
            //todo
            //int count = 0;
            //while (deviceBusy && count++ < 20)
            //{
            //    Thread.Sleep(5);
            //    if (count >= 20)
            //        return false;
            //}
            if (!Device_Idle())
                return false;

            deviceBusy = true; //独占设备

            bool result = false;
            writeBuffer_current[0] = (int)USB_COMMAND.Read_Status;
            if (myDevice.UsbReportWrite(writeBuffer_current))
                result = myDevice.UsbReportRead(readBuffer);
            deviceBusy = false; //释放设备

            ConvertFixPToFloatP(_param, readBuffer, 0, 46, "");
            _param[46] = Convert.ToInt32(readBuffer[46]);  //count
            _param[47] = (double)(readBuffer[47] >> 4) + Convert.ToDouble(readBuffer[47] & 0xF) / 100d;

            //后面的数据直接复制，不做处理。
            Array.Copy(readBuffer, 48, _param, 48, 127 - 48);

            //Rrdc_np
            _param[Index.Rrdc_np] = ConvertFixPToFloatP(readBuffer[Index.Rrdc_np], "");
            //_param = readBuffer;

            return result;
        }

        private bool StatusRead(int[] _param)
        {
            //todo
            bool result = false;
            writeBuffer_current[0] = (int)USB_COMMAND.Read_Status;
            if (myDevice.UsbReportWrite(writeBuffer_current))
                result = myDevice.UsbReportRead(_param);
            //_param = ConvertFixPToFloatP(readBuffer,0,128,"");
            //_param = readBuffer;

            return result;
        }
        #endregion DSP Write

        /// <summary>
        /// 判断设备是否空闲，如果空闲才可以进行本次读写操作
        /// </summary>
        /// <returns></returns>
        private bool Device_Idle()
        {
            int count = 0;
            while (deviceBusy && count++ < 20)
            { Thread.Sleep(5); }
            if (count >= 20)
                return false;
            return true;
        }

        /// <summary>
        /// 判断是否需要更新参数
        /// </summary>
        /// <param name="_oldParam">上一次写入的数据</param>
        /// <param name="_newParam">当前要写入的数据</param>
        /// <returns></returns>
        private bool IfChanged(double[] _oldParam, double[] _newParam)
        {
            if (_oldParam.Length != _newParam.Length)
            {
                return false;
            }
            else
            {
                for (int i = 0; i < _newParam.Length; i++)
                {
                    if (_newParam[i] != _newParam[i])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 将log10的数转换为log2值
        /// </summary>
        /// <param name="data">全部的param</param>
        /// <param name="baseIndex">Drc参数开始的index</param>
        /// <param name="coeff">log10->log2的转换系数</param>
        private void ConvertToLog2(double[] data, uint baseIndex, double coeff)
        {
            data[baseIndex + DrcParamIndex.ERROR] *= coeff;
            data[baseIndex + DrcParamIndex.MUTE_LEVEL] *= coeff;
            data[baseIndex + DrcParamIndex.LT] *= coeff;
            data[baseIndex + DrcParamIndex.CT] *= coeff;
            data[baseIndex + DrcParamIndex.ET] *= coeff;
            data[baseIndex + DrcParamIndex.NT] *= coeff;
            data[baseIndex + DrcParamIndex.SMAX] *= coeff;
            data[baseIndex + DrcParamIndex.SMIN] *= coeff;
            data[baseIndex + DrcParamIndex.GAIN_ADJ] *= coeff;

            if (data[baseIndex + DrcParamIndex.LT] == data[baseIndex + DrcParamIndex.CT])
                data[baseIndex + DrcParamIndex.CS] = 1;
            else
                data[baseIndex + DrcParamIndex.CS] = (data[baseIndex + DrcParamIndex.SMAX] - data[baseIndex + DrcParamIndex.CT]) /
                    (data[baseIndex + DrcParamIndex.LT] - data[baseIndex + DrcParamIndex.CT]);

            if (data[baseIndex + DrcParamIndex.ET] == data[baseIndex + DrcParamIndex.NT])
                data[baseIndex + DrcParamIndex.ES] = 1;
            else
                data[baseIndex + DrcParamIndex.ES] = (data[baseIndex + DrcParamIndex.ET] - data[baseIndex + DrcParamIndex.SMIN]) /
                    (data[baseIndex + DrcParamIndex.ET] - data[baseIndex + DrcParamIndex.NT]);
        }

        /// <summary>
        /// 将浮点数转换为给定格式的定点数,格式为空则用默认格式。
        /// </summary>
        /// <param name="will_con">需要转换的浮点数</param>
        /// <param name="formate">将要转换的格式：exp "8.24"，默认为8.24</param>
        /// <returns></returns>
        public int ConvertFloatPToFixP(double will_con, string formate)
        {
            //默认为8.24
            int len_integer = 8;
            int len_decimal = 24;
            int result = 0;

            #region 判断转换格式
            //格式省略则默认8.24
            if (formate != "")
            {
                try
                {
                    string[] strForArray = formate.Split('.').ToArray();
                    if (strForArray.Length != 2)
                    {
                        return 0;
                    }
                    len_integer = Convert.ToInt32(strForArray[0]);
                    len_decimal = Convert.ToInt32(strForArray[1]);
                }
                catch
                {
                    return 0;
                }
            }
            #endregion 判断转换格式

            will_con *= Math.Pow(2, len_decimal);
            try
            {
                result = Convert.ToInt32(will_con);
            }
            catch
            { }

            return result;
        }

        /// <summary>
        /// 将浮点数转换为给定格式的定点数,格式为空则用默认格式。
        /// </summary>
        /// <param name="will_con">需要转换的浮点数</param>
        /// <param name="formate">将要转换的格式：exp "6.26"，默认为6.26</param>
        /// <returns></returns>
        public int[] ConvertFloatPToFixP(double[] will_con, string formate)
        {
            //默认为6.26
            int len_integer = 6;
            int len_decimal = 26;
            int[] result = new int[will_con.Length];

            #region 判断转换格式
            //格式省略则默认6.26
            if (formate != "")
            {
                try
                {
                    string[] strForArray = formate.Split('.').ToArray();
                    if (strForArray.Length != 2)
                    {
                        return null;
                    }
                    len_integer = Convert.ToInt32(strForArray[0]);
                    len_decimal = Convert.ToInt32(strForArray[1]);
                }
                catch
                {
                    return null;
                }
            }
            #endregion 判断转换格式

            for (int i = 0; i < will_con.Length; i++)
            {
                will_con[i] *= Math.Pow(2, len_decimal);
                try
                {
                    result[i] = Convert.ToInt32(will_con[i]);
                }
                catch
                { return null; }
            }

            return result;
        }

        /// <summary>
        /// 将定点数按给定格式转换为浮点数,格式为空则用默认格式。
        /// </summary>
        /// <param name="will_con">需要转换的定点数</param>
        /// <param name="index_from">开始转换的索引，从0开始</param>
        /// <param name="count">需要转换的个数</param>
        /// <param name="formate">将要转换的格式：exp "5.27"，默认为5.27</param>
        /// <returns></returns>
        private void ConvertFixPToFloatP(double[] result, int[] will_con, int index_from, int count, string formate)
        {
            //默认为5.27
            int len_integer = 5;
            int len_decimal = 27;
            //double[] result = new double[will_con.Length];

            #region 判断转换格式
            //格式省略则默认5.27
            if (formate != "")
            {
                try
                {
                    string[] strForArray = formate.Split('.').ToArray();
                    if (strForArray.Length != 2)
                    {
                        return;
                    }
                    len_integer = Convert.ToInt32(strForArray[0]);
                    len_decimal = Convert.ToInt32(strForArray[1]);
                }
                catch
                {
                    return;
                }
            }
            #endregion 判断转换格式

            for (int i = index_from; i < index_from + count; i++)
            {
                result[i] = (double)will_con[i] / Math.Pow(2, len_decimal);
            }

            return;
        }

        /// <summary>
        /// 将定点数按给定格式转换为浮点数,格式为空则用默认格式。
        /// </summary>
        /// <param name="will_con">需要转换的定点数</param>
        /// <param name="formate">将要转换的格式：exp "5.27"，默认为5.27</param>
        /// <returns></returns>
        private double ConvertFixPToFloatP(int will_con, string formate)
        {
            //默认为5.27
            int len_integer = 5;
            int len_decimal = 27;
            //double[] result = new double[will_con.Length];

            #region 判断转换格式
            //格式省略则默认5.27
            if (formate != "")
            {
                try
                {
                    string[] strForArray = formate.Split('.').ToArray();
                    if (strForArray.Length != 2)
                    {
                        return 0;
                    }
                    len_integer = Convert.ToInt32(strForArray[0]);
                    len_decimal = Convert.ToInt32(strForArray[1]);
                }
                catch
                {
                    return 0;
                }
            }
            #endregion 判断转换格式

            return (double)will_con / Math.Pow(2, len_decimal);
        }


        #endregion 4.数据处理
        #endregion Private 方法
        //private int 
    }

    /// <summary>
    /// Public interfaces for ADMP521.
    /// </summary>
    public partial class ADMP521T
    {
        #region 开放的接口
        #region 1.连接设备
        /// <summary>
        /// 连接默认GUID设备（"eb8322c5-8b49-4feb-ae6e-c99b2b232045"），并获取读写句柄。
        /// </summary>
        /// <returns>返回连接成功与否，True：连接成功。False：失败。</returns>
        public bool ConnectDevice()
        {
            bool result = false;
            //deviceGUID = DMY2_GUID;
            deviceGUID = GUID_CLASS_BF_USB_BULKADI;
            myDevice = new InitDeviceByGUID(deviceGUID);
            result = myDevice.DeviceInitializer();
            return result;
        }

        /// <summary>
        /// 根据_guid连接设备（"eb8322c5-8b49-4feb-ae6e-c99b2b232045"），并获取读写句柄。
        /// </summary>
        /// <param name="_guid">Format：XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX</param>
        /// <returns>返回连接成功与否，True：连接成功。False：失败。</returns>
        public bool ConnectDevice(string _guid)
        {
            bool result = false;
            try
            {
                deviceGUID = new Guid(_guid);
            }
            catch
            {
                return false;
            }
            myDevice = new InitDeviceByGUID(deviceGUID);
            result = myDevice.DeviceInitializer();
            return result;
        }
        #endregion 1.连接设备

        #region 2.获取设备信息
        /// <summary>
        /// 获取设备数
        /// </summary>
        public int TotalDevices
        { get { return myDevice.TotalDevices; } }

        /// <summary>
        /// 获取当前GUID
        /// </summary>
        public string GUID
        { get { return deviceGUID.ToString(); } }

        /// <summary>
        /// Dectect if device connecting status.
        /// </summary>
        public bool IfDeviceConnected
        {
            get
            {
                if (myDevice.QueryNumDevices(ref deviceGUID) != 0)
                    return true;
                else return false;
            }
        }

        /// <summary>
        /// return if the chip is under recording.
        /// </summary>
        public bool UnderRecording
        { get { return underRcording; } }

        #endregion 2.获取设备信息

        #region 3.读写设备

        /// <summary>
        /// Select initialization mode when chip power on.
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="audioFormate">audio formate:true->mono,false->stereo.</param>
        /// <returns></returns>
        public bool InitializationMode(ADMP521T_MODE_INIT mode, bool audioFormate)
        {
            //this value is necessary, it tall DSP just change mode, but do not do any thing else.
            buffer_uint[1] = 0;
            buffer_uint[3] = Convert.ToUInt32(audioFormate);
            switch (mode)
            {
                case ADMP521T_MODE_INIT.Normal:
                    buffer_uint[0] = (uint)ADMP521_USB_COMMAND.Mode_Normal;
                    return InitMode(buffer_uint);
                case ADMP521T_MODE_INIT.Test:
                    buffer_uint[0] = (uint)ADMP521_USB_COMMAND.Mode_Test;
                    return InitMode(buffer_uint);
                default:
                    return false;
            }
        }

        /// <summary>
        /// Select which mode you will enter.
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="audioFormate">audio formate:true->mono,false->stereo</param>
        /// <returns></returns>
        public bool SelectMode(ADMP521T_MODE mode, bool audioFormate)
        {
            //this value is necessary, it tall DSP just change mode, but do not do any thing else.
            buffer_uint[1] = 0;
            if (mode == ADMP521T_MODE.Normal)
                buffer_uint[1] = 1;
            buffer_uint[3] = Convert.ToUInt32(audioFormate);
            switch (mode)
            {
                case ADMP521T_MODE.Normal:
                    buffer_uint[0] = (uint)ADMP521_USB_COMMAND.Mode_Normal;
                    buffer_uint[1] = 1;
                    return InitMode(buffer_uint);
                case ADMP521T_MODE.Test:
                    buffer_uint[0] = (uint)ADMP521_USB_COMMAND.Mode_Test;
                    return InitMode(buffer_uint);
                case ADMP521T_MODE.Normal_Test:
                    buffer_uint[0] = (uint)ADMP521_USB_COMMAND.Mode_NormalTest;
                    return InitMode(buffer_uint);
                case ADMP521T_MODE.Fuse:
                    buffer_uint[0] = (uint)ADMP521_USB_COMMAND.Mode_Fuse;
                    return InitMode(buffer_uint);
                default:
                    return false;
            }

        }

        public bool Write(ADMP521_USB_COMMAND command, uint[] writeData)
        {
            return myDevice.UsbReportWrite(writeData);
        }

        /// <summary>
        /// I2C write function.
        /// </summary>
        /// <param name="writeNum">How many registers will you write to.</param>
        /// <param name="clk_Delay">Normally it is chip address. But when it is fuse mode, 
        /// it become the delay of half cycle.</param>
        /// <param name="writeData">1.length is double of writeNum. 2.Formate:regAdd0,regVal0,regAdd1,regVal1...</param>
        /// <param name="audioFormate">audio formate:true->mono,false->stereo.</param>
        /// <returns></returns>
        public bool Write(ADMP521T_MODE mode, uint writeNum, uint clk_Delay, uint[] writeData, bool audioFormate)
        {
            if (writeNum == 0)
                return false;

            buffer_uint[0] = (uint)mode;
            buffer_uint[1] = writeNum;
            buffer_uint[2] = clk_Delay;
            buffer_uint[3] = Convert.ToUInt32(audioFormate);
            //if(mode == ADMP521T_MODE.Fuse)
            //    buffer_uint[2] = 100;
            if (2 * writeNum != writeData.Length)
                return false;

            for (int i = 0; i < writeNum; i++)
            {
                buffer_uint[4 + 2 * i] = writeData[2 * i];
                buffer_uint[4 + 2 * i + 1] = writeData[2 * i + 1];
            }

            return myDevice.UsbReportWrite(buffer_uint);
        }

        /// <summary>
        /// I2C Read function.
        /// </summary>
        /// <param name="readNum">How many registers will you read.</param>
        /// <param name="chipAddr">Chip address.</param>
        /// <param name="_buffer">1.length is double of readNum. 2.Formate:regAdd0,regVal0,regAdd1,regVal1...</param>
        /// <param name="ifPostTrim">If under post trim mode.</param>
        /// <returns></returns>
        public bool I2CRead(uint readNum, uint chipAddr, uint[] _buffer, bool ifPostTrim)
        {
            buffer_uint[0] = (uint)ADMP521_USB_COMMAND.GPIO_I2C_Read;
            buffer_uint[1] = readNum;
            buffer_uint[2] = chipAddr;
            buffer_uint[3] = Convert.ToUInt32(ifPostTrim);
            if (2 * readNum != _buffer.Length)
                return false;

            for (int i = 0; i < buffer_uint[1]; i++)
            {
                buffer_uint[4 + 2 * i] = _buffer[2 * i];
                buffer_uint[4 + 2 * i + 1] = _buffer[2 * i + 1];
            }
            if (!myDevice.UsbReportWrite(buffer_uint))
            {
                Console.WriteLine("I2C read write ok");
                return false;   //Write read command failed.
            }
            if (!myDevice.UsbReportRead(buffer_uint))
            {
                Console.WriteLine("I2C read write ok");
                return false;    //Read register failed.
            }

            for (int i = 0; i < readNum; i++)
            {
                _buffer[2 * i] = buffer_uint[2 * i];
                _buffer[2 * i + 1] = buffer_uint[2 * i + 1];
            }
            return true;
        }

        /// <summary>
        /// Flash LED used for test Blackfin is alived.
        /// </summary>
        /// <returns></returns>
        public bool FlashLED()
        {
            buffer_uint[0] = (uint)ADMP521_USB_COMMAND.FlashLED;
            return myDevice.UsbReportWrite(buffer_uint);
        }

        /// <summary>
        /// Read data from FW, read size is _regs.length.
        /// </summary>
        /// <param name="_regs">Buffer used for save read data.And the use command must put in _regs[0].</param>
        /// <returns></returns>
        public bool Read(uint[] _regs)
        {
            buffer_uint[0] = _regs[0];
            if (!myDevice.UsbReportWrite(buffer_uint))
                return false;
            //Console.WriteLine("write read cmd result:{0}",myDevice.UsbReportWrite(buffer_uint));
            return myDevice.UsbReportRead(_regs);
            //return true;
        }

        /// <summary>
        /// Start recording.
        /// </summary>
        /// <param name="rawDataPath">where will the recording data save to.</param>
        /// <param name="ifCLKon">If clk is already on. True: already on. False: off now, should set on when recording.</param>
        /// <returns></returns>
        public bool PDMRecording(string rawDataPath, bool ifCLKon)
        {
            buffer_uint[1] = Convert.ToUInt32(ifCLKon);
            try
            {
                ParameterizedThreadStart parStart = new ParameterizedThreadStart(PDM_ReceiveData);
                T_pdmReveiveData = new Thread(parStart);
                T_pdmReveiveData.Start((object)rawDataPath);
            }
            catch
            { }

            Thread.Sleep(10);
            if (underRcording)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Stop recording.
        /// </summary>
        public void PDMStop()
        {
            underRcording = false;
        }

        /// <summary>
        /// Get the firm ware version.
        /// </summary>
        /// <returns></returns>
        public double GetFWVersion()
        {
            double result = 0;
            buffer_uint[0] = (uint)ADMP521_USB_COMMAND.ReadFW_Version;
            if (!myDevice.UsbReportWrite(buffer_uint))
                return 0;
            else
            {
                if (!myDevice.UsbReportRead(buffer_byte))
                    return 0;
                else
                {
                    result += (buffer_byte[0] & 0xF0) / 16;
                    result += (double)(buffer_byte[0] & 0x0F) / 100;
                    return result;
                }
            }
        }

        /// <summary>
        /// Set LR pin voltage level.
        /// </summary>
        /// <param name="high">If you want to set the LR pin with high level.</param>
        /// <param name="channel">Which channel will you control.0:Part 0, 1: Part 1,2:Both</param>
        /// <param name="ifNormalMode">If you set LR under normal mode.True:normal mode. false:other modes.</param>
        /// <returns></returns>
        public bool SetLR(bool high, uint channel, bool ifNormalMode)
        {
            bool result = false;
            buffer_uint[0] = (uint)ADMP521_USB_COMMAND.SetLR;
            buffer_uint[1] = Convert.ToUInt32(high);
            buffer_uint[2] = channel;
            buffer_uint[3] = Convert.ToUInt32(ifNormalMode);
            result = myDevice.UsbReportWrite(buffer_uint);
            Console.WriteLine("Set LR to {0} -> result:{1}", high, result);
            return result;
        }

        /// <summary>
        /// Set clock on or off.
        /// </summary>
        /// <param name="on">True:on; false:off.</param>
        /// <returns></returns>
        public bool ClkSwitch(bool on)
        {
            bool result = false;
            buffer_uint[0] = (uint)ADMP521_USB_COMMAND.OtherCtrl;
            buffer_uint[1] = (uint)OTHERCTRL_CMD.ClkSwitch;
            buffer_uint[2] = Convert.ToUInt32(on);
            result = myDevice.UsbReportWrite(buffer_uint);
            Console.WriteLine("Set clk to {0} -> result:{1}", on, result);
            return result;
        }

        /// <summary>
        /// Set post trim mode on or off.
        /// </summary>
        /// <param name="on">True:Enter post trim mode. False: Quit post trim mode.</param>
        /// <returns></returns>
        public bool SetPostTrim(bool on)
        {
            bool result = false;
            buffer_uint[0] = (uint)ADMP521_USB_COMMAND.OtherCtrl;
            buffer_uint[1] = (uint)OTHERCTRL_CMD.PostTrim;
            buffer_uint[2] = Convert.ToUInt32(on);
            result = myDevice.UsbReportWrite(buffer_uint);
            Console.WriteLine("Set Post trim to {0} -> result:{1}", on, result);
            return result;
        }

        #endregion 读写设备

        #region Test Interfaces
        public void TestMultThread(string rawDataPath)
        {
            ParameterizedThreadStart parStart = new ParameterizedThreadStart(Test_ReceiveData);
            T_pdmReveiveData = new Thread(parStart);
            T_pdmReveiveData.Start((object)rawDataPath);
        }

        private void Test_ReceiveData(object parObj)
        {
            string recRawDataPath = (string)parObj;
            BinaryWriter bw = new BinaryWriter(new FileStream(recRawDataPath, FileMode.Create));
            for (byte i = 0; i < buffer_uint.Length; i++)
                buffer_byte[i] = i;

            wavOperation.WriteWaveDataToFile(bw, buffer_byte);

            bw.Close();
        }

        public bool Test(bool rw)
        {
            buffer_uint[0] = (uint)ADMP521_USB_COMMAND.TestInterface;
            buffer_uint[1] = Convert.ToUInt32(rw);

            buffer_uint[4] = 0x01;
            buffer_uint[5] = 0x06;
            return myDevice.UsbReportWrite(buffer_uint);
        }
        #endregion Test Interfaces

        #endregion 开放的接口
    }

    /// <summary>
    /// Private interfaces for ADMP521T.
    /// </summary>
    public partial class ADMP521T
    {
        #region General Params definition
        InitDeviceByGUID myDevice;// = new InitDeviceByGUID(deviceGUID);
        private Guid GUID_CLASS_BF_USB_BULKADI = new Guid("eb8322c5-8b49-4feb-ae6e-c99b2b232045");
        private Guid deviceGUID = Guid.Empty;

        private byte[] buffer_byte = new byte[512];         //Buffer for write operation.
        private int[] buffer_int = new int[128];
        private uint[] buffer_uint = new uint[128];
        private byte[] buffer_rec = new byte[65536];        //0x10000->64K
        //private byte[][] buffer_rec_ping = new byte[];
        ADMP521_REC_BUF buffer_rec_ping = new ADMP521_REC_BUF();
        ADMP521_REC_BUF buffer_rec_pang = new ADMP521_REC_BUF();
        ADMP521TReg Regs = new ADMP521TReg();
        WaveOperate wavOperation = new WaveOperate();

        private static bool deviceBusy = false;
        public static bool DeviceBusy
        {
            get { return deviceBusy; }
        }

        //Thread for PDM recording
        Thread T_pdmReveiveData;        // 读录音数据线程
        Thread T_DataProcessing;   // 处理读取数据线程
        volatile string recRawDataPath;
        volatile bool underRcording = false;        //标记是否正在录音。
        #endregion Params definition

        #region 不开放接口
        #region R/W functions
        private bool Write(byte[] writeData)
        {
            if (writeData.Length < 512)
            {
                for (int i = 0; i < writeData.Length; i++)
                {
                    buffer_byte[i] = writeData[i];
                }
                return myDevice.UsbReportWrite(buffer_byte);
            }
            else
                return myDevice.UsbReportWrite(writeData);
        }

        private bool Write(int[] writeData)
        {
            if (writeData.Length < 128)
            {
                for (int i = 0; i < writeData.Length; i++)
                {
                    buffer_int[i] = writeData[i];
                }
                return myDevice.UsbReportWrite(buffer_byte);
            }
            else
                return myDevice.UsbReportWrite(writeData);
        }

        public bool Read(byte[] readData)
        {
            return myDevice.UsbReportRead(readData);
        }

        private bool Read(int[] readData)
        {
            if (readData.Length < 512)
            {
                for (int i = 0; i < readData.Length; i++)
                {
                    buffer_int[i] = readData[i];
                }
                return myDevice.UsbReportRead(buffer_int);
            }
            else
                return myDevice.UsbReportRead(readData);
        }

        /// <summary>
        /// Thread for PDM recoding   
        /// </summary>
        /// <param name="parObj"></param>   
        private void PDM_ReceiveData(object parObj)
        {
            string recRawDataPath = (string)parObj;
            if (File.Exists(recRawDataPath))
                File.Delete(recRawDataPath);
            BinaryWriter bw = new BinaryWriter(new FileStream(recRawDataPath, FileMode.Create));
            bool result;
            buffer_uint[0] = (uint)ADMP521_USB_COMMAND.SportPDM_Start;
            if (myDevice.UsbReportWrite(buffer_uint))
                underRcording = true;
            else
            {
                underRcording = false;
                return;
            }
            int count = 0;
            while (underRcording)
            {
                //Console.WriteLine("PDM Read result:No{0}->{1}", i, mydevice.Read(read_array));
                result = myDevice.UsbReportRead(buffer_rec);
                //Console.WriteLine("PDM Read result:No{0}->{1}", ++count, result);
                if (!result)
                {
                    underRcording = false;
                    break;
                }
                wavOperation.WriteWaveDataToFile(bw, buffer_rec);
            }

            bw.Close();

            buffer_uint[0] = (uint)ADMP521_USB_COMMAND.SportPDM_Stop;
            myDevice.UsbReportWrite(buffer_uint);
            //Console.WriteLine("PDM stop result:{0}", myDevice.UsbReportWrite(buffer_uint));                       
        }

        /// <summary>
        /// Select mode when chip power on.
        /// </summary>
        /// <returns></returns>
        private bool InitMode(uint[] _buffer)
        {
            bool result = myDevice.UsbReportWrite(_buffer);
            Console.WriteLine("Init Mode result:{0}", result);
            return result;
        }

        #endregion R/W functions

        #endregion 不开放接口

    }

    /// <summary>
    /// Public interfaces for ADMP521T RevB.
    /// </summary>
    public partial class ADMP521T_RevB
    {
        #region 开放的接口
        #region 1.连接设备
        /// <summary>
        /// 连接默认GUID设备（"eb8322c5-8b49-4feb-ae6e-c99b2b232045"），并获取读写句柄。
        /// </summary>
        /// <returns>返回连接成功与否，True：连接成功。False：失败。</returns>
        public bool ConnectDevice()
        {
            bool result = false;
            //deviceGUID = DMY2_GUID;
            deviceGUID = GUID_CLASS_BF_USB_BULKADI;
            myDevice = new InitDeviceByGUID(deviceGUID);
            result = myDevice.DeviceInitializer();
            return result;
        }

        /// <summary>
        /// 根据_guid连接设备（"eb8322c5-8b49-4feb-ae6e-c99b2b232045"），并获取读写句柄。
        /// </summary>
        /// <param name="_guid">Format：XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX</param>
        /// <returns>返回连接成功与否，True：连接成功。False：失败。</returns>
        public bool ConnectDevice(string _guid)
        {
            bool result = false;
            try
            {
                deviceGUID = new Guid(_guid);
            }
            catch
            {
                return false;
            }
            myDevice = new InitDeviceByGUID(deviceGUID);
            result = myDevice.DeviceInitializer();
            return result;
        }
        #endregion 1.连接设备

        #region 2.获取设备信息
        /// <summary>
        /// 获取设备数
        /// </summary>
        public int TotalDevices
        { get { return myDevice.TotalDevices; } }

        /// <summary>
        /// 获取当前GUID
        /// </summary>
        public string GUID
        { get { return deviceGUID.ToString(); } }

        /// <summary>
        /// Dectect if device connecting status.
        /// </summary>
        public bool IfDeviceConnected
        {
            get
            {
                if (myDevice.QueryNumDevices(ref deviceGUID) != 0)
                    return true;
                else return false;
            }
        }

        /// <summary>
        /// return if the chip is under recording.
        /// </summary>
        public bool UnderRecording
        { get { return underRcording; } }

        #endregion 2.获取设备信息

        #region 3.读写设备

        /// <summary>
        /// Select initialization mode when chip power on.
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="SDPorAP">SDPorAP:true->SDP mode,false->AP mode.</param>
        /// <returns></returns>
        public bool InitializationMode(ADMP521T_MODE_INIT mode, bool SDPorAP)
        {
            //this value is necessary, it tall DSP just change mode, but do not do any thing else.
            buffer_uint[1] = 0;
            buffer_uint[3] = Convert.ToUInt32(SDPorAP);
            switch (mode)
            {
                case ADMP521T_MODE_INIT.Normal:
                    buffer_uint[0] = (uint)ADMP521_USB_COMMAND.Mode_Normal;
                    return InitMode(buffer_uint);
                case ADMP521T_MODE_INIT.Test:
                    buffer_uint[0] = (uint)ADMP521_USB_COMMAND.Mode_Test;
                    return InitMode(buffer_uint);
                default:
                    return false;
            }
        }

        /// <summary>
        /// Select which mode you will enter.
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="SDPorAP">SDPorAP:true->SDP mode,false->AP mode.</param>
        /// <returns></returns>
        public bool SelectMode(ADMP521T_MODE mode, bool SDPorAP)
        {
            //this value is necessary, it tall DSP just change mode, but do not do any thing else.
            buffer_uint[1] = 0;
            if (mode == ADMP521T_MODE.Normal)
                buffer_uint[1] = 1;
            buffer_uint[3] = Convert.ToUInt32(SDPorAP);
            switch (mode)
            {
                case ADMP521T_MODE.Normal:
                    buffer_uint[0] = (uint)ADMP521_USB_COMMAND.Mode_Normal;
                    buffer_uint[1] = 1;
                    return InitMode(buffer_uint);
                case ADMP521T_MODE.Test:
                    buffer_uint[0] = (uint)ADMP521_USB_COMMAND.Mode_Test;
                    return InitMode(buffer_uint);
                case ADMP521T_MODE.Normal_Test:
                    buffer_uint[0] = (uint)ADMP521_USB_COMMAND.Mode_NormalTest;
                    return InitMode(buffer_uint);
                case ADMP521T_MODE.Fuse:
                    buffer_uint[0] = (uint)ADMP521_USB_COMMAND.Mode_Fuse;
                    return InitMode(buffer_uint);
                default:
                    return false;
            }

        }

        public bool Write(ADMP521_USB_COMMAND command, uint[] writeData)
        {
            return myDevice.UsbReportWrite(writeData);
        }

        /// <summary>
        /// I2C write function.
        /// </summary>
        /// <param name="writeNum">How many registers will you write to.</param>
        /// <param name="chipAddr_FuseDelay">Normally it is chip address. But when it is fuse mode, 
        /// it become the delay of half cycle.</param>
        /// <param name="writeData">1.length is double of writeNum. 2.Formate:regAdd0,regVal0,regAdd1,regVal1...</param>
        /// <param name="SDPorAP">SDPorAP:true->SDP mode,false->AP mode.</param>
        /// <returns></returns>
        public bool Write(ADMP521T_MODE mode, uint writeNum, uint chipAddr_FuseDelay, uint[] writeData, bool SDPorAP)
        {
            if (writeNum == 0)
                return false;

            buffer_uint[0] = (uint)mode;
            buffer_uint[1] = writeNum;
            buffer_uint[2] = chipAddr_FuseDelay;
            buffer_uint[3] = Convert.ToUInt32(SDPorAP);
            //if(mode == ADMP521T_MODE.Fuse)
            //    buffer_uint[2] = 100;
            if (2 * writeNum != writeData.Length)
                return false;

            for (int i = 0; i < writeNum; i++)
            {
                buffer_uint[4 + 2 * i] = writeData[2 * i];
                buffer_uint[4 + 2 * i + 1] = writeData[2 * i + 1];
            }

            return myDevice.UsbReportWrite(buffer_uint);
        }

        /// <summary>
        /// I2C Read function.
        /// </summary>
        /// <param name="readNum">How many registers will you read.</param>
        /// <param name="chipAddr">Chip address.</param>
        /// <param name="_buffer">1.length is double of readNum. 2.Formate:regAdd0,regVal0,regAdd1,regVal1...</param>
        /// <param name="ifPostTrim">If under post trim mode.</param>
        /// <returns></returns>
        public bool I2CRead(uint readNum, uint chipAddr, uint[] _buffer, bool ifPostTrim)
        {
            buffer_uint[0] = (uint)ADMP521_USB_COMMAND.GPIO_I2C_Read;
            buffer_uint[1] = readNum;
            buffer_uint[2] = chipAddr;
            buffer_uint[3] = Convert.ToUInt32(ifPostTrim);
            if (2 * readNum != _buffer.Length)
                return false;

            for (int i = 0; i < buffer_uint[1]; i++)
            {
                buffer_uint[4 + 2 * i] = _buffer[2 * i];
                buffer_uint[4 + 2 * i + 1] = _buffer[2 * i + 1];
            }
            if (!myDevice.UsbReportWrite(buffer_uint))
            {
                Console.WriteLine("I2C read write ok");
                return false;   //Write read command failed.
            }
            if (!myDevice.UsbReportRead(buffer_uint))
            {
                Console.WriteLine("I2C read write ok");
                return false;    //Read register failed.
            }

            for (int i = 0; i < readNum; i++)
            {
                _buffer[2 * i] = buffer_uint[2 * i];
                _buffer[2 * i + 1] = buffer_uint[2 * i + 1];
            }
            return true;
        }

        /// <summary>
        /// Flash LED used for test Blackfin is alived.
        /// </summary>
        /// <returns></returns>
        public bool FlashLED()
        {
            buffer_uint[0] = (uint)ADMP521_USB_COMMAND.FlashLED;
            return myDevice.UsbReportWrite(buffer_uint);
        }

        /// <summary>
        /// Read data from FW, read size is _regs.length.
        /// </summary>
        /// <param name="_regs">Buffer used for save read data.And the use command must put in _regs[0].</param>
        /// <returns></returns>
        public bool Read(uint[] _regs)
        {
            buffer_uint[0] = _regs[0];
            if (!myDevice.UsbReportWrite(buffer_uint))
                return false;
            //Console.WriteLine("write read cmd result:{0}",myDevice.UsbReportWrite(buffer_uint));
            return myDevice.UsbReportRead(_regs);
        }

        /// <summary>
        /// Start recording.
        /// </summary>
        /// <param name="rawDataPath">where will the recording data save to.</param>
        /// <param name="ifCLKon">If clk is already on. True: already on. False: off now, should set on when recording.</param>
        /// <returns></returns>
        public bool PDMRecording(string rawDataPath, bool ifCLKon)
        {
            buffer_uint[1] = Convert.ToUInt32(ifCLKon);
            try
            {
                ParameterizedThreadStart parStart = new ParameterizedThreadStart(PDM_ReceiveData);
                T_pdmReveiveData = new Thread(parStart);
                T_pdmReveiveData.Start((object)rawDataPath);
            }
            catch
            { }

            Thread.Sleep(10);
            if (underRcording)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Stop recording.
        /// </summary>
        public void PDMStop()
        {
            underRcording = false;
        }

        /// <summary>
        /// Get the firm ware version.
        /// </summary>
        /// <returns></returns>
        public double GetFWVersion()
        {
            double result = 0;
            buffer_uint[0] = (uint)ADMP521_USB_COMMAND.ReadFW_Version;
            if (!myDevice.UsbReportWrite(buffer_uint))
                return 0;
            else
            {
                if (!myDevice.UsbReportRead(buffer_byte))
                    return 0;
                else
                {
                    result += (buffer_byte[0] & 0xF0) / 16;
                    result += (double)(buffer_byte[0] & 0x0F) / 100;
                    return result;
                }
            }
        }

        /// <summary>
        /// Set LR pin voltage level.
        /// </summary>
        /// <param name="high">If you want to set the LR pin with high level.</param>
        /// <param name="channel">Which channel will you control.0:Part 0, 1: Part 1,2:Both</param>
        /// <param name="ifNormalMode">If you set LR under normal mode.True:normal mode. false:other modes.</param>
        /// <returns></returns>
        public bool SetLR(bool high, uint channel, bool ifNormalMode)
        {
            bool result = false;
            buffer_uint[0] = (uint)ADMP521_USB_COMMAND.SetLR;
            buffer_uint[1] = Convert.ToUInt32(high);
            buffer_uint[2] = channel;
            buffer_uint[3] = Convert.ToUInt32(ifNormalMode);
            result = myDevice.UsbReportWrite(buffer_uint);
            Console.WriteLine("Set LR to {0} -> result:{1}", high, result);
            return result;
        }

        /// <summary>
        /// Set clock on or off.
        /// </summary>
        /// <param name="on">True:on; false:off.</param>
        /// <returns></returns>
        public bool ClkSwitch(bool on)
        {
            bool result = false;
            buffer_uint[0] = (uint)ADMP521_USB_COMMAND.OtherCtrl;
            buffer_uint[1] = (uint)OTHERCTRL_CMD.ClkSwitch;
            buffer_uint[2] = Convert.ToUInt32(on);
            result = myDevice.UsbReportWrite(buffer_uint);
            Console.WriteLine("Set clk to {0} -> result:{1}", on, result);
            return result;
        }

        /// <summary>
        /// Set post trim mode on or off.
        /// </summary>
        /// <param name="on">True:Enter post trim mode. False: Quit post trim mode.</param>
        /// <returns></returns>
        public bool SetPostTrim(bool on)
        {
            bool result = false;
            buffer_uint[0] = (uint)ADMP521_USB_COMMAND.OtherCtrl;
            buffer_uint[1] = (uint)OTHERCTRL_CMD.PostTrim;
            buffer_uint[2] = Convert.ToUInt32(on);
            result = myDevice.UsbReportWrite(buffer_uint);
            Console.WriteLine("Set Post trim to {0} -> result:{1}", on, result);
            return result;
        }

        /// <summary>
        /// Switch between SDP and AP
        /// </summary>
        /// <param name="ifSDP">if it is true will select SDP, false AP</param>
        /// <returns></returns>
        public bool SwitchSDPorAP(bool ifSDP)
        {
            bool result = false;
            buffer_uint[0] = (uint)ADMP521_USB_COMMAND.OtherCtrl;
            buffer_uint[1] = (uint)OTHERCTRL_CMD.SDPorAP;
            buffer_uint[2] = Convert.ToUInt32(ifSDP);
            result = myDevice.UsbReportWrite(buffer_uint);
            Console.WriteLine("Select SDP or AP, true is SDP and false is AP:{0}, set result", ifSDP, result);
            return result;
        }

        #endregion 读写设备

        #region Test Interfaces
        public void TestMultThread(string rawDataPath)
        {
            ParameterizedThreadStart parStart = new ParameterizedThreadStart(Test_ReceiveData);
            T_pdmReveiveData = new Thread(parStart);
            T_pdmReveiveData.Start((object)rawDataPath);
        }

        private void Test_ReceiveData(object parObj)
        {
            string recRawDataPath = (string)parObj;
            BinaryWriter bw = new BinaryWriter(new FileStream(recRawDataPath, FileMode.Create));
            for (byte i = 0; i < buffer_uint.Length; i++)
                buffer_byte[i] = i;

            wavOperation.WriteWaveDataToFile(bw, buffer_byte);

            bw.Close();
        }

        public bool Test(bool rw)
        {
            buffer_uint[0] = (uint)ADMP521_USB_COMMAND.TestInterface;
            buffer_uint[1] = Convert.ToUInt32(rw);

            buffer_uint[4] = 0x01;
            buffer_uint[5] = 0x06;
            return myDevice.UsbReportWrite(buffer_uint);
        }
        #endregion Test Interfaces

        #endregion 开放的接口
    }

    /// <summary>
    /// Private interfaces for ADMP521T RevB.
    /// </summary>
    public partial class ADMP521T_RevB
    {
        #region General Params definition
        InitDeviceByGUID myDevice;// = new InitDeviceByGUID(deviceGUID);
        private Guid GUID_CLASS_BF_USB_BULKADI = new Guid("eb8322c5-8b49-4feb-ae6e-c99b2b232045");
        private Guid deviceGUID = Guid.Empty;

        private byte[] buffer_byte = new byte[512];         //Buffer for write operation.
        private int[] buffer_int = new int[128];
        private uint[] buffer_uint = new uint[128];
        private byte[] buffer_rec = new byte[65536];        //0x10000->64K
        //private byte[][] buffer_rec_ping = new byte[];
        ADMP521_REC_BUF buffer_rec_ping = new ADMP521_REC_BUF();
        ADMP521_REC_BUF buffer_rec_pang = new ADMP521_REC_BUF();
        ADMP521TReg Regs = new ADMP521TReg();
        WaveOperate wavOperation = new WaveOperate();

        private static bool deviceBusy = false;
        public static bool DeviceBusy
        {
            get { return deviceBusy; }
        }

        //Thread for PDM recording
        Thread T_pdmReveiveData;        // 读录音数据线程
        Thread T_DataProcessing;   // 处理读取数据线程
        volatile string recRawDataPath;
        volatile bool underRcording = false;        //标记是否正在录音。
        #endregion Params definition

        #region 不开放接口
        #region R/W functions
        private bool Write(byte[] writeData)
        {
            if (writeData.Length < 512)
            {
                for (int i = 0; i < writeData.Length; i++)
                {
                    buffer_byte[i] = writeData[i];
                }
                return myDevice.UsbReportWrite(buffer_byte);
            }
            else
                return myDevice.UsbReportWrite(writeData);
        }

        private bool Write(int[] writeData)
        {
            if (writeData.Length < 128)
            {
                for (int i = 0; i < writeData.Length; i++)
                {
                    buffer_int[i] = writeData[i];
                }
                return myDevice.UsbReportWrite(buffer_byte);
            }
            else
                return myDevice.UsbReportWrite(writeData);
        }

        public bool Read(byte[] readData)
        {
            return myDevice.UsbReportRead(readData);
        }

        private bool Read(int[] readData)
        {
            if (readData.Length < 512)
            {
                for (int i = 0; i < readData.Length; i++)
                {
                    buffer_int[i] = readData[i];
                }
                return myDevice.UsbReportRead(buffer_int);
            }
            else
                return myDevice.UsbReportRead(readData);
        }

        /// <summary>
        /// Thread for PDM recoding   
        /// </summary>
        /// <param name="parObj"></param>   
        private void PDM_ReceiveData(object parObj)
        {
            string recRawDataPath = (string)parObj;
            if (File.Exists(recRawDataPath))
                File.Delete(recRawDataPath);
            BinaryWriter bw = new BinaryWriter(new FileStream(recRawDataPath, FileMode.Create));
            bool result;
            buffer_uint[0] = (uint)ADMP521_USB_COMMAND.SportPDM_Start;
            if (myDevice.UsbReportWrite(buffer_uint))
                underRcording = true;
            else
            {
                underRcording = false;
                return;
            }
            int count = 0;
            while (underRcording)
            {
                //Console.WriteLine("PDM Read result:No{0}->{1}", i, mydevice.Read(read_array));
                result = myDevice.UsbReportRead(buffer_rec);
                //Console.WriteLine("PDM Read result:No{0}->{1}", ++count, result);
                if (!result)
                {
                    underRcording = false;
                    break;
                }
                wavOperation.WriteWaveDataToFile(bw, buffer_rec);
            }

            bw.Close();

            buffer_uint[0] = (uint)ADMP521_USB_COMMAND.SportPDM_Stop;
            myDevice.UsbReportWrite(buffer_uint);
            //Console.WriteLine("PDM stop result:{0}", myDevice.UsbReportWrite(buffer_uint));                       
        }

        /// <summary>
        /// Select mode when chip power on.
        /// </summary>
        /// <returns></returns>
        private bool InitMode(uint[] _buffer)
        {
            bool result = myDevice.UsbReportWrite(_buffer);
            Console.WriteLine("Init Mode result:{0}", result);
            return result;
        }

        #endregion R/W functions

        #endregion 不开放接口

    }


    /// <summary>
    /// Public interfaces for One Wire Interface.
    /// </summary>
    public partial class OneWireInterface
    {
        #region 开放的接口
        #region 1.连接设备
        /// <summary>
        /// 连接默认GUID设备（"eb8322c5-8b49-4feb-ae6e-c99b2b232045"），并获取读写句柄。
        /// </summary>
        /// <returns>返回连接成功与否，True：连接成功。False：失败。</returns>
        public bool ConnectDevice()
        {
            bool result = false;
            //deviceGUID = DMY2_GUID;
            deviceGUID = GUID_CLASS_BF_USB_BULKADI;
            myDevice = new InitDeviceByGUID(deviceGUID);
            result = myDevice.DeviceInitializer();
            return result;
        }

        /// <summary>
        /// 根据_guid连接设备（"eb8322c5-8b49-4feb-ae6e-c99b2b232045"），并获取读写句柄。
        /// </summary>
        /// <param name="_guid">Format：XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX</param>
        /// <returns>返回连接成功与否，True：连接成功。False：失败。</returns>
        public bool ConnectDevice(string _guid)
        {
            bool result = false;
            try
            {
                deviceGUID = new Guid(_guid);
            }
            catch
            {
                return false;
            }
            myDevice = new InitDeviceByGUID(deviceGUID);
            result = myDevice.DeviceInitializer();
            return result;
        }
        #endregion 1.连接设备

        #region 2.获取设备信息
        /// <summary>
        /// 获取设备数
        /// </summary>
        public int TotalDevices
        { get { return myDevice.TotalDevices; } }

        /// <summary>
        /// 获取当前GUID
        /// </summary>
        public string GUID
        { get { return deviceGUID.ToString(); } }

        /// <summary>
        /// Dectect if device connecting status.
        /// </summary>
        public bool IfDeviceConnected
        {
            get
            {
                if (myDevice.QueryNumDevices(ref deviceGUID) != 0)
                    return true;
                else return false;
            }
        }

        public static bool DeviceBusy
        {
            get { return deviceBusy; }
        }
        #endregion 2.获取设备信息

        #region 3.读写设备
        /// <summary>
        /// Single I2C write operation.
        /// </summary>
        /// <param name="dev_addr">Device address.</param>
        /// <param name="reg_addr">Register address.</param>
        /// <param name="reg_Data">Register date</param>
        /// <returns>True: write succeeded;False: write failed.</returns>
        public bool I2CWrite_Single(uint dev_addr, uint reg_addr, uint reg_Data)
        {
            buffer_uint[0] = (uint)OneWire_USB_COMMAND.I2C_Write_Single;        //Command
            buffer_uint[1] = dev_addr;                                          //Device address
            buffer_uint[4] = reg_addr;                                          //Register address
            buffer_uint[5] = reg_Data;                                          //Register date
            return myDevice.UsbReportWrite(buffer_uint);
        }

        /// <summary>
        /// Burst I2C write operation.
        /// </summary>
        /// <param name="dev_addr">Device address.</param>
        /// <param name="reg_addr">Register address.</param>
        /// <param name="reg_Data">Register date</param>
        /// <param name="write_number">How many registers will wirte</param>
        /// <returns>True: write succeeded;False: write failed.</returns>
        public bool I2CWrite_Burst(uint dev_addr, uint reg_addr, uint[] reg_Data, uint write_number)
        {
            buffer_uint[0] = (uint)OneWire_USB_COMMAND.I2C_Write_Burst;         //Command
            buffer_uint[1] = dev_addr;                                          //Device address
            buffer_uint[2] = write_number;                                      //Will write register number
            buffer_uint[4] = reg_addr;                                          //Register address
            for (int i = 0; i < write_number; i++)
            {
                buffer_uint[5 + i] = reg_Data[i];                                  //Register date                
            }
            return myDevice.UsbReportWrite(buffer_uint);
        }

        /// <summary>
        /// Single I2C Read function.
        /// </summary>
        /// <param name="dev_addr">Device address.</param>
        /// <param name="reg_addr">Register address.</param>
        /// <returns>Return info:
        /// 1.0x00-0xFF->Read succeeded 2. 0xFFF->Write "I2C Read" command failed 3.0xFFFF->Read back failed. </returns>
        public uint I2CRead_Single(uint dev_addr, uint reg_addr)
        {
            uint u_readfailed = 0x0000FFFF;
            uint u_writfailed = 0x00000FFF;
            buffer_uint[0] = (uint)OneWire_USB_COMMAND.I2C_Read_Single;
            buffer_uint[1] = dev_addr;
            buffer_uint[4] = reg_addr;
            if (!myDevice.UsbReportWrite(buffer_uint))
            {
                Console.WriteLine("I2C read failed->Write command fialed");
                return u_writfailed;   //Write read command failed.
            }
            buffer_byte[0] = 0x03;
            buffer_byte[3] = 0x50;
            buffer_byte[4] = 0x54;
            buffer_byte[8] = 0x00;
            if (!myDevice.UsbReportRead(buffer_byte))
            {
                Console.WriteLine("I2C read failed->Read command fialed");
                return u_readfailed;    //Read register failed.
            }
            buffer_uint[0] = (uint)buffer_byte[0];
            return buffer_uint[0];
        }

        /// <summary>
        /// Burst I2C Read function.
        /// </summary>
        /// <param name="dev_addr">Device address.</param>
        /// <param name="reg_addr">Register address.</param>
        /// <param name="read_number">How many registers will read.</param>
        /// <param name="reg_data">Use to store the read back register data.</param>
        /// <returns> Return info:
        /// 1.0x00->Read succeeded 2. 0x01-> read_number and reg_data length doesn't match.
        /// 3. 0x02->Write "I2C Read" command failed 4.0x03->Read back failed. 
        /// </returns>
        public uint I2CRead_Burst(uint dev_addr, uint reg_addr, uint read_number, uint[] reg_data)
        {
            uint setup_error = 0x01;
            uint write_error = 0x02;
            uint readback_error = 0x03;
            buffer_uint[0] = (uint)OneWire_USB_COMMAND.I2C_Read_Burst;
            buffer_uint[1] = dev_addr;
            buffer_uint[2] = read_number;
            buffer_uint[4] = reg_addr;

            if (read_number != reg_data.Length)
                return setup_error;

            if (!myDevice.UsbReportWrite(buffer_uint))
            {
                Console.WriteLine("I2C read failed->Write command fialed");
                return write_error;   //Write read command failed.
            }
            if (!myDevice.UsbReportRead(buffer_uint))
            {
                Console.WriteLine("I2C read failed->Read command fialed");
                return readback_error;    //Read register failed.
            }

            for (int i = 0; i < read_number; i++)
            {
                reg_data[i] = buffer_uint[i];
            }
            return 0;
        }

        /// <summary>
        /// Flash LED used for test Blackfin is alived.
        /// </summary>
        /// <returns>True->FlashLED command write succeeded. False->failed.</returns>
        public bool FlashLED()
        {
            buffer_uint[0] = (uint)OneWire_USB_COMMAND.FlashLED;
            return myDevice.UsbReportWrite(buffer_uint);
        }

        /// <summary>
        /// Get the firm ware version.
        /// </summary>
        /// <returns>return info:1.-1->write command failed.2.-2->readback failed.3.others->succeeded</returns>
        public double GetFWVersion()
        {
            double result = 0;
            buffer_uint[0] = (uint)OneWire_USB_COMMAND.ReadFW_Version;
            if (!myDevice.UsbReportWrite(buffer_uint))
                return -1;
            else
            {
                if (!myDevice.UsbReportRead(buffer_byte))
                {
                    return -2;
                }
                else
                {
                    result += (buffer_byte[0] & 0xF0) / 16;
                    result += (double)(buffer_byte[0] & 0x0F) / 100;
                    return result;
                }
            }
        }

        public bool ReprogrammingFW(uint offset, byte[] data)
        {
            bool ret = false;

            //int lenPerPacket = 512;
            //int totalLen = data.Length;
            //int numPacket = (int)Math.Ceiling((double)totalLen / (double)lenPerPacket);
            //int offset = 0;
            //for (int ix = 0; ix < numPacket; ix++)
            //{                
            //    /*Step1: Write command*/
            //    buffer_uint[0] = (uint)OneWire_USB_COMMAND.ADI_SDP_CMD_SDRAM_PROGRAM_BOOT;
            //    if (totalLen - offset >= 512)
            //        buffer_uint[1] = (uint)lenPerPacket;
            //    else
            //        buffer_uint[1] = (uint)(totalLen - offset);
            //    buffer_uint[4] = (uint)offset;
            //    if (ix != numPacket - 1)
            //        buffer_uint[5] = 0;
            //    else
            //        buffer_uint[5] = 0x02;

            //    if (!myDevice.UsbReportWrite(buffer_uint))
            //        return ret;

            //    /*Step2: Download data*/
            //    Array.Copy(data, offset, buffer_byte, 0, (int)buffer_uint[1]);
            //    offset += lenPerPacket;     //Move offset for next operation.
            //    if (!myDevice.UsbReportWrite(buffer_byte))
            //        return ret;
            //}

            /*Step1: Write command*/
            buffer_uint[0] = (uint)OneWire_USB_COMMAND.ADI_SDP_CMD_SDRAM_PROGRAM_BOOT;
            buffer_uint[1] = (uint)data.Length;
            buffer_uint[0] = offset;
            buffer_uint[1] = 0x02;
            if (!myDevice.UsbReportWrite(buffer_uint))
                return ret;

            /*Step2: Download data*/
            if (!myDevice.UsbReportWrite(data))
                return ret;
            return true;
        }

        /// <summary>
        /// Fuls clock switch
        /// </summary>
        /// <param name="clock_switch">True: clock on; Flase:clock off</param>
        /// <param name="pulsewidth">Set the pulse width.</param>
        /// <returns>True->Fuse command write succeeded. False->failed.</returns>
        public bool FuseClockSwitch(double pulsewidth, double duration_time)
        {
            //if (clock_switch)
            //{
            buffer_uint[0] = (uint)OneWire_USB_COMMAND.FuseOn;
            buffer_uint[1] = pulse_width_baseCount + (uint)((pulsewidth - 100) * 3 / 5);
            buffer_uint[2] = (uint)(duration_time * Math.Pow(10, 6) / pulsewidth);
            //}
            //else
            //{
            //    buffer_uint[0] = (uint)OneWire_USB_COMMAND.FuseOff;
            //}
            return myDevice.UsbReportWrite(buffer_uint);
        }

        public bool UpdateFusePulseWidth(FusePulseWidth pulsewidth)
        {
            buffer_uint[0] = (uint)OneWire_USB_COMMAND.Update_Fuse_Pulse_Width;
            return myDevice.UsbReportWrite(buffer_uint);
        }

        public bool SetPilotWidth(uint cycle_number)
        {
            buffer_uint[0] = (uint)OneWire_USB_COMMAND.SetPilot_Width;
            buffer_uint[1] = pilot_width_baseCount + (uint)((cycle_number - 200) * 3 / 5);
            buffer_uint[2] = cycle_number * 9 / 50;         //Boundary for zero and one judgement. =1.5 * pilot * 10^9 / 120 * 10^6 ns
            return myDevice.UsbReportWrite(buffer_uint);
        }

        #region OWI ADC Specially
        /// <summary>
        /// User Command for OWI ADC specially
        /// </summary>
        /// <param name="command">CMD ID</param>
        /// <param name="downCount">Info 1</param>
        /// <param name="upCount">Info 2</param>
        /// <param name="parameterNum">Info 3</param>
        /// <returns></returns>
        public bool UserCommand(uint command, uint downCount, uint upCount, uint parameterNum)
        {
            buffer_uint[0] = command;           //Command
            buffer_uint[1] = downCount;         //download count
            buffer_uint[2] = upCount;           //up count
            buffer_uint[3] = parameterNum;      //parameter number
            return myDevice.UsbReportWrite(buffer_uint);
        }




        #region UART Control
        public bool UARTInitilize(uint bitRate, uint stopBit)
        {
            buffer_uint[0] = (uint)OneWire_USB_COMMAND.ADI_SDP_CMD_UART_INIT;
            buffer_uint[1] = bitRate;
            buffer_uint[2] = stopBit;
            return myDevice.UsbReportWrite(buffer_uint);
        }

        public bool UARTRead()
        {
            //todo
            //buffer_uint[0] = (uint)OneWire_USB_COMMAND.ADI_SDP_CMD_UART_WRITE;
            //return myDevice.UsbReportWrite(buffer_uint);
            return false;
        }

        public bool UARTWrite(UARTControlCommand cmd, uint value)
        {
            buffer_uint[0] = (uint)OneWire_USB_COMMAND.ADI_SDP_CMD_UART_WRITE;
            buffer_uint[1] = (uint)cmd;
            buffer_uint[2] = (uint)value;
            return myDevice.UsbReportWrite(buffer_uint);
        }
        #endregion UART Control






        /// <summary>
        /// ReadBack the build infomation of current FW.
        /// </summary>
        /// <returns></returns>
        public byte[] GetFirmwareInfo()
        {
            byte[] readBackData = new byte[32];

            buffer_uint[0] = (uint)OneWire_USB_COMMAND.GetFirmwareVersion;
            buffer_uint[2] = 32;
            if (!myDevice.UsbReportWrite(buffer_uint))
            {
                Console.WriteLine("GetFirmwareInfo->Write command fialed");
                return null;   //Write read command failed.
            }

            if (!myDevice.UsbReportRead(buffer_byte))
            {
                Console.WriteLine("GetFirmwareInfo->Read command fialed");
                return null;    //Read command failed.
            }

            Array.Copy(buffer_byte, readBackData, 32);
            return readBackData;
        }

        public bool ResetBoard()
        {
            buffer_uint[0] = (uint)OneWire_USB_COMMAND.ResetBoard;
            return myDevice.UsbReportWrite(buffer_uint);
        }

        public bool ADCReset()
        {
            buffer_uint[0] = (uint)OneWire_USB_COMMAND.ADCReset;
            return myDevice.UsbReportWrite(buffer_uint);
        }

        public bool SDPSigPathInit()
        {
            buffer_uint[0] = (uint)OneWire_USB_COMMAND.ADI_SDP_CMD_SIGNALPATH_INIT;
            return myDevice.UsbReportWrite(buffer_uint);
        }

        public bool SDPSignalPathSet(SPControlCommand cmd)
        {
            buffer_uint[0] = (uint)OneWire_USB_COMMAND.ADI_SDP_CMD_SIGNALPATH_SET;
            buffer_uint[3] = (uint)cmd;
            return myDevice.UsbReportWrite(buffer_uint);
        }

        public bool SDPSingalPathReadSot()
        {
            //buffer_uint[0] = (uint)OneWire_USB_COMMAND.ADI_SDP_CMD_SIGNALPATH_READ_SOT;
            //buffer_uint[3] = (uint)cmd;
            //return myDevice.UsbReportWrite(buffer_uint);

            byte[] readBackData = new byte[32];

            buffer_uint[0] = (uint)OneWire_USB_COMMAND.ADI_SDP_CMD_SIGNALPATH_READ_SOT;
            //buffer_uint[0] = (uint)OneWire_USB_COMMAND.GetFirmwareVersion;
            buffer_uint[2] = 32;
            if (!myDevice.UsbReportWrite(buffer_uint))
            {
                Console.WriteLine("GetFirmwareInfo->Write command fialed");
                return false;   //Write read command failed.
            }

            if (!myDevice.UsbReportRead(buffer_byte))
            {
                Console.WriteLine("GetFirmwareInfo->Read command fialed");
                return false;    //Read command failed.
            }

            Array.Copy(buffer_byte, readBackData, 32);

            //if (readBackData[0] == 0x5A)
            if (readBackData[2] == 0x20)
                return true;
            else
                return false;

        }

        public bool SDPSignalPathGroupSel(SPControlCommand cmd)
        {
            buffer_uint[0] = (uint)OneWire_USB_COMMAND.ADI_SDP_CMD_SIGNALPATH_GROUP;
            buffer_uint[3] = (uint)cmd;
            return myDevice.UsbReportWrite(buffer_uint);
        }

        public bool SDPSignalPathSocketSel(uint cmd)
        {
            buffer_uint[0] = (uint)OneWire_USB_COMMAND.ADI_SDP_CMD_SIGNALPATH_SOCKET;
            buffer_uint[3] = (uint)cmd;
            return myDevice.UsbReportWrite(buffer_uint);
        }

        public bool GeneralBoolSet(OneWire_USB_COMMAND cmd, bool setValue)
        {
            uint realValue = setValue ? 1u : 0u;
            buffer_uint[0] = (uint)cmd;      //Command            
            buffer_uint[2] = realValue;      //Set Value -> upByteCount
            return myDevice.UsbReportWrite(buffer_uint);
        }


        public UInt16[] ADCSampleTransfer(uint downByteCount, uint upByteCount)
        {
            UInt16[] tempData = new ushort[upByteCount];
            uint bufferSize = (uint)buffer_u16.Length;
            buffer_uint[0] = (uint)OneWire_USB_COMMAND.AdcSampleTransfer;
            buffer_uint[1] = downByteCount;
            buffer_uint[2] = upByteCount;
            if (!myDevice.UsbReportWrite(buffer_uint))
            {
                Console.WriteLine("ADCSampleTransfer->Write command fialed");
                return null;   //Write read command failed.
            }

            uint count = upByteCount / bufferSize;
            for (uint i = 0; i < count; i++)
            {
                if (!myDevice.UsbReportRead(buffer_u16))
                {
                    Console.WriteLine("ADCSampleTransfer->Read command fialed");
                    return null;    //Read command failed.
                }
                Array.Copy(buffer_u16, 0, tempData, bufferSize * i, bufferSize);
            }

            if (upByteCount % bufferSize > 0)
            {
                if (!myDevice.UsbReportRead(buffer_u16))
                {
                    Console.WriteLine("ADCSampleTransfer->Read command fialed");
                    return null;    //Read command failed.
                }
                Array.Copy(buffer_u16, 0, tempData, bufferSize * count, upByteCount % bufferSize);
            }
            return tempData;
        }

        public double AverageADCSamples(UInt16[] samples)
        {
            //double avg = 0;
            //foreach (UInt16 item in samples)
            //{
            //    avg += item;
            //}

            //avg /= Convert.ToDouble(samples.Length);
            //return avg;

            int N = 5;
            double avg = 0;
            UInt16 temp = 0;
            //UInt16[] SampGroup = new UInt16[N];
            int nGroup = samples.Length / N;

            for (int j = 0; j < nGroup; j++)
            {
                for (int i = 0; i < N - 1; i++)
                {
                    if (samples[i + N * j] > samples[i + 1 + N * j])
                    {
                        temp = samples[i + N * j];
                        samples[i + N * j] = samples[i + 1 + N * j];
                        samples[i + N * j] = temp;
                    }
                }
                avg += samples[(N - 1) / 2 + N * j];
            }

            avg /= Convert.ToDouble(nGroup);
            return avg;
        }
        #endregion OWI ADC Specially

        public enum FusePulseWidth
        {
            width_100ns = 90,       //60 times core clock
            width_125ns = 93,
            width_150ns = 180,
            width_175ns = 225,
            width_200ns = 270
        }

        public enum SPControlCommand
        {
            SP_VOUT_WITH_CAP = 0x61,
            SP_VOUT_WITHOUT_CAP = 0x62,
            SP_VREF_WITH_CAP = 0x63,
            SP_VREF_WITHOUT_CAP = 0x64,
            SP_VIN_TO_VOUT = 0x65,
            SP_VIN_TO_VREF = 0x66,
            SP_CONFIG_TO_VOUT = 0x67,
            SP_CONFIG_TO_VREF = 0x68,
            SP_VDD_FROM_EXT = 0x69,
            SP_VDD_FROM_5V = 0x6A,
            SP_VDD_POWER_ON = 0x6B,
            SP_VDD_POWER_OFF = 0x6C,
            SP_MODULE_510OUT = 0x6D,
            SP_MODULE_AMPOUT = 0x6E,
            SP_VIN_TO_VCS = 0x6F,
            SP_SET_CURRENT_SENCE = 0X70,
            SP_BYPASS_CURRENT_SENCE = 0X71,
            SP_VIN_TO_510OUT = 0x72,
            SP_VIN_TO_MOUT = 0x73,
            SP_CONFIG_TO_510OUT = 0x74,
            SP_CONFIG_TO_MOUT = 0x75,
            SP_CONFIG_TO_VCS = 0x76,
            SP_MULTISITTE_GROUP_A = 0X79,
            SP_MULTISITTE_GROUP_B = 0X7A,
            SP_VDD_FROM_3V3 = 0x7B,
            //SP_READ_SOT = 0x7C,
            SP_WRITE_EOT = 0x7D,
            SP_WRITE_BIN_ONE = 0x7E,
            SP_WRITE_BIN_TWO = 0x7F,
            SP_WRITE_BIN_FAIL = 0x80,
            SP_WRITE_BIN_RECYCLE = 0x81
        }

        public enum UARTControlCommand
        {
            ADI_SDP_CMD_UART_REMOTE = 0x71,
            ADI_SDP_CMD_UART_LOCAL = 0x72,
            ADI_SDP_CMD_UART_OUTPUTON = 0x73,
            ADI_SDP_CMD_UART_OUTPUTOFF = 0x74,
            ADI_SDP_CMD_UART_SETVOLT = 0x75,
            ADI_SDP_CMD_UART_SETCURR = 0x76
        }

        #endregion 读写设备

        #region 4.OWCI AUX
        public bool SetPilotAux(uint pilot, uint divider)
        {
            buffer_uint[0] = (uint)OneWire_USB_COMMAND.OWCI_SET_PILOT_AUX;
            buffer_uint[1] = pilot;
            buffer_uint[4] = divider;
            //buffer_uint[2] = cycle_number * 9 / 50;         //Boundary for zero and one judgement. =1.5 * pilot * 10^9 / 120 * 10^6 ns
            return myDevice.UsbReportWrite(buffer_uint);
        }

        public bool I2CWriteSingleAux(uint dev_addr, uint reg_addr, uint reg_Data)
        {
            buffer_uint[0] = (uint)OneWire_USB_COMMAND.I2C_Write_Single;        //Command
            buffer_uint[1] = dev_addr;                                          //Device address
            buffer_uint[4] = reg_addr;                                          //Register address
            buffer_uint[5] = reg_Data;                                          //Register date
            return myDevice.UsbReportWrite(buffer_uint);
        }

        public uint I2CReadSingleAux(uint dev_addr, uint reg_addr)
        {
            uint u_readfailed = 0x0000FFFF;
            uint u_writfailed = 0x00000FFF;
            buffer_uint[0] = (uint)OneWire_USB_COMMAND.I2C_Read_Single;
            buffer_uint[1] = dev_addr;
            buffer_uint[4] = reg_addr;
            if (!myDevice.UsbReportWrite(buffer_uint))
            {
                Console.WriteLine("I2C read failed->Write command fialed");
                return u_writfailed;   //Write read command failed.
            }
            buffer_byte[0] = 0x03;
            buffer_byte[3] = 0x50;
            buffer_byte[4] = 0x54;
            buffer_byte[8] = 0x00;
            if (!myDevice.UsbReportRead(buffer_byte))
            {
                Console.WriteLine("I2C read failed->Read command fialed");
                return u_readfailed;    //Read register failed.
            }
            buffer_uint[0] = (uint)buffer_byte[0];
            return buffer_uint[0];
        }

        #endregion

        #region Test Interfaces
        public bool Test(bool rw)
        {
            FlashLED();

            buffer_uint[0] = (uint)OneWire_USB_COMMAND.TestInterface;
            buffer_uint[1] = Convert.ToUInt32(rw);

            buffer_uint[4] = 0x01;
            buffer_uint[5] = 0x06;
            myDevice.UsbReportWrite(buffer_uint);

            Console.WriteLine("read back succeeded{0}", myDevice.UsbReportRead(buffer_uint));
            Console.WriteLine("read back value is{0}", buffer_uint[0]);
            return true;
        }
        #endregion Test Interfaces

        #endregion 开放的接口
    }

    /// <summary>
    /// Private interfaces for One Wire Interface.
    /// </summary>
    public partial class OneWireInterface
    {
        #region General Params definition
        InitDeviceByGUID myDevice;// = new InitDeviceByGUID(deviceGUID);
        private Guid GUID_CLASS_BF_USB_BULKADI = new Guid("eb8322c5-8b49-4feb-ae6e-c99b2b232045");
        private Guid deviceGUID = Guid.Empty;

        /**************************************************************************
         * 	u32 cmd;              // command to execute
	     *  u32 downByteCount;    // number of bytes in next transfer down
	     *  u32 upByteCount;      // number of bytes expected in next transfer up
	     *  u32 numParam;         // number of valid parameters in u32ParamArray
	     *  u32 paramArray[124];  // Parameter array
         **************************************************************************/
        private byte[] buffer_byte = new byte[512];         //Buffer for write operation.
        private int[] buffer_int = new int[128];
        private uint[] buffer_uint = new uint[128];
        private UInt16[] buffer_u16 = new UInt16[256];
        private uint pulse_width_baseCount = 60;
        private uint pilot_width_baseCount = 80;
        private static bool deviceBusy = false;
        #endregion Params definition

        #region 不开放接口
        #region R/W functions

        #endregion R/W functions

        #endregion 不开放接口

    }


    /// <summary>
    /// 普通设备操作，读写等都不错处理。
    /// </summary>
    public class AnotherDevice
    {
        #region General Params definition
        InitDeviceByGUID myDevice;// = new InitDeviceByGUID(deviceGUID);
        private Guid GUID_CLASS_BF_USB_BULKADI = new Guid("eb8322c5-8b49-4feb-ae6e-c99b2b232045");
        //private Guid DMY2_GUID = new Guid("89982a59-5eea-45aa-af97-52ec351018c2");
        private Guid deviceGUID = Guid.Empty;

        private byte[] buffer_byte = new byte[512];         //Buffer for write operation.
        private int[] buffer_int = new int[128];

        private static bool deviceBusy = false;
        public static bool DeviceBusy
        {
            get { return deviceBusy; }
        }
        #endregion Params definition

        #region 开放的接口
        #region 1.连接设备
        /// <summary>
        /// 连接默认GUID设备（"eb8322c5-8b49-4feb-ae6e-c99b2b232045"），并获取读写句柄。
        /// </summary>
        /// <returns>返回连接成功与否，True：连接成功。False：失败。</returns>
        public bool ConnectDevice()
        {
            bool result = false;
            //deviceGUID = DMY2_GUID;
            deviceGUID = GUID_CLASS_BF_USB_BULKADI;
            myDevice = new InitDeviceByGUID(deviceGUID);
            result = myDevice.DeviceInitializer();
            return result;
        }

        /// <summary>
        /// 根据_guid连接设备（"eb8322c5-8b49-4feb-ae6e-c99b2b232045"），并获取读写句柄。
        /// </summary>
        /// <param name="_guid">Format：XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX</param>
        /// <returns>返回连接成功与否，True：连接成功。False：失败。</returns>
        public bool ConnectDevice(string _guid)
        {
            bool result = false;
            try
            {
                deviceGUID = new Guid(_guid);
            }
            catch
            {
                return false;
            }
            myDevice = new InitDeviceByGUID(deviceGUID);
            result = myDevice.DeviceInitializer();
            return result;
        }
        #endregion 1.连接设备

        #region 2.获取设备信息
        /// <summary>
        /// 获取设备数
        /// </summary>
        public int TotalDevices
        { get { return myDevice.TotalDevices; } }

        /// <summary>
        /// 获取当前GUID
        /// </summary>
        public string GUID
        { get { return deviceGUID.ToString(); } }

        #endregion 2.获取设备信息

        #region 3.读写设备
        public bool Write(byte[] writeData)
        {
            if (writeData.Length < 512)
            {
                for (int i = 0; i < writeData.Length; i++)
                {
                    buffer_byte[i] = writeData[i];
                }
                return myDevice.UsbReportWrite(buffer_byte);
            }
            else
                return myDevice.UsbReportWrite(writeData);
        }

        public bool Write(int[] writeData)
        {
            if (writeData.Length < 128)
            {
                for (int i = 0; i < writeData.Length; i++)
                {
                    buffer_int[i] = writeData[i];
                }
                return myDevice.UsbReportWrite(buffer_byte);
            }
            else
                return myDevice.UsbReportWrite(writeData);
        }

        public bool Read(byte[] readData)
        {
            if (readData.Length < 512)
            {
                for (int i = 0; i < readData.Length; i++)
                {
                    buffer_byte[i] = readData[i];
                }
                return myDevice.UsbReportRead(buffer_byte);
            }
            else
                return myDevice.UsbReportRead(readData);
        }

        public bool Read(int[] readData)
        {
            if (readData.Length < 512)
            {
                for (int i = 0; i < readData.Length; i++)
                {
                    buffer_int[i] = readData[i];
                }
                return myDevice.UsbReportRead(buffer_int);
            }
            else
                return myDevice.UsbReportRead(readData);
        }
        #endregion 读写设备

        #endregion 开放的接口
    }

    public class GeneralMethods
    {
        public GeneralMethods()
        {
            InitHashTable();    //This is necessary for look up from hash table
        }

        #region 1.读写本地文件
        /// <summary>
        /// 读本地IV数据
        /// </summary>
        /// <param name="path">IV数据路径</param>
        /// <returns></returns>
        public double[] Read_LocalIVCoefs(string path)
        {
            List<double> readData = new List<double> { };
            StreamReader strRead;
            try
            {
                strRead = File.OpenText(path);
            }
            catch
            {
                return null;
            }

            string[] strArray_readLine;

            #region Read data from local file
            while (!strRead.EndOfStream)
            {
                strArray_readLine = strRead.ReadLine().Trim().Split(',');
                try
                {
                    for (int i = 0; i < strArray_readLine.Length; i++)
                    {
                        if (strArray_readLine[i] == "")
                            continue;
                        readData.Add(Convert.ToDouble(strArray_readLine[i]));
                    }
                }
                catch
                {
                    strRead.Close();
                    MessageBox.Show("Read error, please check the data file.");
                    return null;
                }
            }
            strRead.Close();
            //double[] result = readData.ToArray();
            //return result;
            return readData.ToArray();
            #endregion

        }

        /// <summary>
        /// 将data数据写入本地文件。
        /// </summary>
        /// <param name="path">将要写入文件的路径。</param>
        /// <param name="data">要写的数据</param>
        /// <returns>标记是否写成功</returns>
        public bool Write_LocalFile(string path, double[] data)
        {
            StreamWriter strWrite;
            try
            {
                strWrite = File.CreateText(path);
            }
            catch
            {
                return false;
            }

            string str_willWrite = "";
            for (int i = 0; i < data.Length; i++)
            {
                str_willWrite += data[i].ToString() + ",";
            }
            strWrite.Write(str_willWrite);
            strWrite.Close();
            return true;
        }

        /// <summary>
        /// Get the current dll's verion.
        /// </summary>
        /// <returns></returns>
        public string GetDllVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        /// <summary>
        /// Convert the local raw data file to text file.
        /// </summary>
        /// <param name="path">raw data file</param>
        /// <returns></returns>
        public bool ConvertRawData2TextFile(string path)
        {
            BinaryReader br;
            TextWriter tw_ch0;
            TextWriter tw_ch1;
            try
            {
                br = new BinaryReader(File.OpenRead(path));
                tw_ch0 = File.CreateText(path.Replace(".dat", "_ch0.txt"));
                tw_ch1 = File.CreateText(path.Replace(".dat", "_ch1.txt"));
            }
            catch
            {
                return false;
            }
            int default1M = 0x100000;
            br.BaseStream.Seek(0, SeekOrigin.Begin);

            byte[] tempData;
            byte tempConveting;

            int handleCount = 1;
            int index_inLoop;
            StringBuilder tempText_ch0;
            StringBuilder tempText_ch1;
            int mask_ch0 = 0x55;
            int mask_ch1 = 0xAA;
            while (br.BaseStream.Position + default1M < br.BaseStream.Length)
            {
                Console.WriteLine("Converting the {0} * 1M ...", handleCount++);
                tempText_ch0 = new StringBuilder(default1M);
                tempText_ch1 = new StringBuilder(default1M);
                tempData = br.ReadBytes(default1M);   //Read data

                #region 将每两个bit转换为一个byte。 01b->0x01
                Console.WriteLine("Time:{0}", DateTime.Now.ToString() + DateTime.Now.Millisecond.ToString());
                for (int i = 0; i < tempData.Length / 4; i++)
                {
                    //Console.WriteLine("Time:{0}", DateTime.Now.ToString() + DateTime.Now.Millisecond.ToString());
                    index_inLoop = 3;
                    for (int j = 0; j < 4; j++)
                    {
                        tempConveting = tempData[4 * i + index_inLoop--];
                        tempText_ch0.Append((string)ht_hexToString[tempConveting & mask_ch0]);
                        tempText_ch1.Append((string)ht_hexToString[tempConveting & mask_ch1]);
                    }
                }
                Console.WriteLine("Time:{0}", DateTime.Now.ToString() + DateTime.Now.Millisecond.ToString());
                tw_ch0.Write(tempText_ch0.ToString());
                tw_ch1.Write(tempText_ch1.ToString());
                #endregion
            }

            Console.WriteLine("Converting the {0} * 1M ...", handleCount++);
            tempData = br.ReadBytes((int)(br.BaseStream.Length - br.BaseStream.Position));   //Read data
            tempText_ch0 = new StringBuilder(tempData.Length);
            tempText_ch1 = new StringBuilder(tempData.Length);

            Console.WriteLine("Time:{0}", DateTime.Now.ToString() + DateTime.Now.Millisecond.ToString());
            for (int i = 0; i < tempData.Length / 4; i++)
            {
                //Console.WriteLine("Time:{0}", DateTime.Now.ToLongTimeString());
                index_inLoop = 3;
                for (int j = 0; j < 4; j++)
                {
                    tempConveting = tempData[4 * i + index_inLoop--];
                    tempText_ch0.Append((string)ht_hexToString[tempConveting & mask_ch0]);
                    tempText_ch1.Append((string)ht_hexToString[tempConveting & mask_ch1]);
                }
            }
            Console.WriteLine("Time:{0}", DateTime.Now.ToString() + DateTime.Now.Millisecond.ToString());
            tw_ch0.Write(tempText_ch0.ToString());
            tw_ch1.Write(tempText_ch1.ToString());

            br.Close();
            tw_ch0.Close();
            tw_ch1.Close();
            return true;
        }

        #endregion 1.读写本地文件

        #region 2.数据处理
        /// <summary>
        /// 将Int32数组转换为Byte数组
        /// </summary>
        /// <param name="_willConv">需要转换的数据</param>
        /// <returns>返回byte数组</returns>
        public static byte[] ConvIntArrToByteArr(int[] _willConv)
        {
            byte[] byteArray = new byte[_willConv.Length * 4];
            for (int i = 0; i < _willConv.Length; i++)
            {
                BitConverter.GetBytes(_willConv[i]).CopyTo(byteArray, i * 4);
            }
            return byteArray;
        }

        public static byte[] ConvIntArrToByteArr(int _willConv)
        {
            return BitConverter.GetBytes(_willConv);
        }
        #endregion 2.数据处理

        #region Private params
        private Hashtable ht_hexToString = new Hashtable();
        private void InitHashTable()
        {
            ht_hexToString.Add(0x00, "0\r\n0\r\n0\r\n0\r\n");
            ht_hexToString.Add(0x01, "0\r\n0\r\n0\r\n1\r\n");
            ht_hexToString.Add(0x04, "0\r\n0\r\n1\r\n0\r\n");
            ht_hexToString.Add(0x05, "0\r\n0\r\n1\r\n1\r\n");
            ht_hexToString.Add(0x10, "0\r\n1\r\n0\r\n0\r\n");
            ht_hexToString.Add(0x11, "0\r\n1\r\n0\r\n1\r\n");
            ht_hexToString.Add(0x14, "0\r\n1\r\n1\r\n0\r\n");
            ht_hexToString.Add(0x15, "0\r\n1\r\n1\r\n1\r\n");
            ht_hexToString.Add(0x40, "1\r\n0\r\n0\r\n0\r\n");
            ht_hexToString.Add(0x41, "1\r\n0\r\n0\r\n1\r\n");
            ht_hexToString.Add(0x44, "1\r\n0\r\n1\r\n0\r\n");
            ht_hexToString.Add(0x45, "1\r\n0\r\n1\r\n1\r\n");
            ht_hexToString.Add(0x50, "1\r\n1\r\n0\r\n0\r\n");
            ht_hexToString.Add(0x51, "1\r\n1\r\n0\r\n1\r\n");
            ht_hexToString.Add(0x54, "1\r\n1\r\n1\r\n0\r\n");
            ht_hexToString.Add(0x55, "1\r\n1\r\n1\r\n1\r\n");

            ht_hexToString.Add(0x02, "0\r\n0\r\n0\r\n1\r\n");
            ht_hexToString.Add(0x08, "0\r\n0\r\n1\r\n0\r\n");
            ht_hexToString.Add(0x0A, "0\r\n0\r\n1\r\n1\r\n");
            ht_hexToString.Add(0x20, "0\r\n1\r\n0\r\n0\r\n");
            ht_hexToString.Add(0x22, "0\r\n1\r\n0\r\n1\r\n");
            ht_hexToString.Add(0x28, "0\r\n1\r\n1\r\n0\r\n");
            ht_hexToString.Add(0x2A, "0\r\n1\r\n1\r\n1\r\n");
            ht_hexToString.Add(0x80, "1\r\n0\r\n0\r\n0\r\n");
            ht_hexToString.Add(0x82, "1\r\n0\r\n0\r\n1\r\n");
            ht_hexToString.Add(0x88, "1\r\n0\r\n1\r\n0\r\n");
            ht_hexToString.Add(0x8A, "1\r\n0\r\n1\r\n1\r\n");
            ht_hexToString.Add(0xA0, "1\r\n1\r\n0\r\n0\r\n");
            ht_hexToString.Add(0xA2, "1\r\n1\r\n0\r\n1\r\n");
            ht_hexToString.Add(0xA8, "1\r\n1\r\n1\r\n0\r\n");
            ht_hexToString.Add(0xAA, "1\r\n1\r\n1\r\n1\r\n");
        }

        #endregion
    }
}

