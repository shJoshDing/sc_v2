using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Collections;

namespace ADI.DMY2
{
    public class BasicParm
    {
        public BasicParm()
        { }

        #region ADMP44X param
        internal enum _PIPE_TYPE
        {
            READ_PIPE,
            WRITE_PIPE
        }

        private static bool use_async = true;
        internal static bool USE_ASYNC
        {
            get { return use_async; }
            //set { use_async = value ;}
        }

        private static bool use_sync = false;
        internal static bool USE_SYNC
        {
            get { return use_sync; }
            //set { use_async = value ;}
        }

        // if connect to device
        private static bool b_connectToDev = false;
        internal static bool b_ConnectToDev
        {
            get { return b_connectToDev; }
            set { b_connectToDev = value; }
        }

        private static bool b_hwI2cMode = true;
        internal static bool b_HWI2CMode
        {
            get { return b_hwI2cMode; }
            set { b_hwI2cMode = value; }
        }

        // if recording
        private static bool b_keepRecording = false;
        internal static bool b_KeepRecording
        {
            get { return b_keepRecording; }
            set { b_keepRecording = value; }
        }

        private static int reg_num = 25;
        internal static int REG_NUM
        {
            get { return reg_num; }
        }

        private static int hid_reg_num = 25;
        internal static int HID_REG_NUM
        {
            get { return hid_reg_num; }
        }

        private static int stuff_num = 2;
        internal static int STUFF_NUM
        {
            get { return stuff_num; }
        }

        private static byte i2c_Address = (byte)0x28;
        internal static byte I2C_Address
        {
            get { return i2c_Address; }
            set
            {
                //Console.WriteLine("I2C Address is:{0}->{1}", i2c_Address, value);
                i2c_Address = value;
            }
        }

        private static int m_int_tdmmode = 1;
        internal static int M_INT_TDMODE
        {
            get { return m_int_tdmmode; }
            set { m_int_tdmmode = value; }
        }

        private static uint max_io_wait = 2000;		// timeout for USB transfers in msecs
        internal static uint MAX_IO_WAIT
        {
            get { return max_io_wait; }
        }
        //define register struct

        #endregion ADMP44X param

        #region afx.h
        internal const uint NULL = 0;
        #endregion

        #region WinNT.h
        internal const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;

        internal const uint GENERIC_READ = 0x80000000;
        internal const uint GENERIC_WRITE = 0x40000000;
        internal const uint GENERIC_EXECUTE = 0x20000000;
        internal const uint GENERIC_ALL = 0x10000000;

        internal const uint FILE_SHARE_READ = 0x00000001;
        internal const uint FILE_SHARE_WRITE = 0x00000002;
        internal const uint FILE_SHARE_DELETE = 0x00000004;
        #endregion

        #region WinBase.h
        internal const uint CREATE_NEW = 1;
        internal const uint CREATE_ALWAYS = 2;
        internal const uint OPEN_EXISTING = 3;
        internal const uint OPEN_ALWAYS = 4;
        internal const uint TRUNCATE_EXISTING = 5;
        internal const uint FILE_FLAG_WRITE_THROUGH = 0x80000000;
        internal const uint FILE_FLAG_OVERLAPPED = 0x40000000;
        internal const uint FILE_FLAG_NO_BUFFERING = 0x20000000;
        internal const uint FILE_FLAG_RANDOM_ACCESS = 0x10000000;
        internal const uint FILE_FLAG_SEQUENTIAL_SCAN = 0x08000000;
        internal const uint FILE_FLAG_DELETE_ON_CLOSE = 0x04000000;
        internal const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
        internal const uint FILE_FLAG_POSIX_SEMANTICS = 0x01000000;
        internal const uint FILE_FLAG_OPEN_REPARSE_POINT = 0x00200000;
        internal const uint FILE_FLAG_OPEN_NO_RECALL = 0x00100000;
        internal const uint FILE_FLAG_FIRST_PIPE_INSTANCE = 0x00080000;
        #endregion

        #region vinuser.h
        public const int WM_DEVICECHANGE = 0x0219;
        #endregion
    }

    /// <summary>
    /// This Class provide a function for initialize(Connect and get W/R handle) the device by GUID.
    /// Also it provide two basic interface function to Read and Write the device 
    /// </summary>
    internal class InitDeviceByGUID
    {
        /// <summary>
        /// Initialized by GUID
        /// </summary>
        /// <param name="_devGUID">Your device's GUID</param>
        internal InitDeviceByGUID(Guid _devGUID)
        {
            devGUID = _devGUID;
        }

        int g_lTotalDevices = 0;                     // total devices detected
        internal int TotalDevices
        {
            get { return g_lTotalDevices; }
            set
            {
                if (((g_lTotalDevices == 0) & (value > 0)) | ((g_lTotalDevices > 0) & (value == 0)))
                    b_shouldReconnect = true;
                else
                    b_shouldReconnect = false;
                g_lTotalDevices = value;
            }
        }

        private bool b_shouldReconnect = false;

        uint g_unDeviceNumber = 0;					// device number selected
        internal uint DeviceNumber
        {
            get { return g_unDeviceNumber; }
        }

        IntPtr g_hWrite = IntPtr.Zero;               //USB device write handle
        internal IntPtr WriteHandle
        {
            get { return g_hWrite; }
        }

        IntPtr g_hRead = IntPtr.Zero;                //USB deviec read handle
        internal IntPtr ReadHandle
        {
            get { return g_hRead; }
        }

        IntPtr g_hWriteEvent = IntPtr.Zero;          // Write event handle for ASYNC IO
        internal IntPtr Event_ghWrite
        {
            get { return g_hWriteEvent; }
        }

        IntPtr g_hReadEvent = IntPtr.Zero;           // read event handle for ASYNC IO     
        internal IntPtr Event_ghRead
        {
            get { return g_hReadEvent; }
        }

        Guid devGUID = Guid.Empty;
        internal Guid DevGUID
        {
            get { return devGUID; }
        }

        GeneralAPI.OVERLAPPED g_ReadOverlapped;		// for asynchronous IO reads for ASYNC IO
        GeneralAPI.OVERLAPPED g_WriteOverlapped;		// for asynchronous IO writes for ASYNC IO

        /// <summary>
        /// Device initializer, connected to the device by GUID and get the R/W handle
        /// </summary>
        /// <returns></returns>
        internal bool DeviceInitializer()
        {
            bool result;
            TotalDevices = QueryNumDevices(ref devGUID);

            if (!b_shouldReconnect)
                return false; ;

            result = ConnectToDevice(BasicParm.USE_ASYNC);
            BasicParm.b_ConnectToDev = result;

            if (!result)
            {
                return result;
            }

            #region overlapped initialize
            //write overlapped initialize
            g_WriteOverlapped.EventHandle = g_hWriteEvent;
            g_WriteOverlapped.Internal = UIntPtr.Zero;
            g_WriteOverlapped.InternalHigh = UIntPtr.Zero;
            g_WriteOverlapped.Offset = 0;
            g_WriteOverlapped.OffsetHigh = 0;

            //read overlapped initialize
            g_ReadOverlapped.EventHandle = g_hReadEvent;
            g_ReadOverlapped.Internal = UIntPtr.Zero;
            g_ReadOverlapped.InternalHigh = UIntPtr.Zero;
            g_ReadOverlapped.Offset = 0;
            g_ReadOverlapped.OffsetHigh = 0;
            #endregion

            return result;
        }

        internal int QueryNumDevices(ref Guid guid)
        {
            int nDevices = 0;
            IntPtr hDevInfo = GeneralAPI.SetupDiGetClassDevs(ref guid,
                0, IntPtr.Zero, GeneralAPI.DIGCF_DEVICEINTERFACE | GeneralAPI.DIGCF_PRESENT);
            //IntPtr hDevInfo = GeneralAPI.SetupDiGetClassDevs(ref pguid,
            //   0, IntPtr.Zero, GeneralAPI.DIGCF_PRESENT);

            if (hDevInfo.ToInt32() == GeneralAPI.INVALID_HANDLE_VALUE)
            {
                throw new Exception("Invalid Handle!");
            }

            GeneralAPI.SP_DEVINFO_DATA deviceInfoData = new GeneralAPI.SP_DEVINFO_DATA();
            //deviceInfoData.cbSize = Marshal.SizeOf(deviceInfoData);//28=16+4+4+4
            //deviceInfoData.devInst = 0;
            //deviceInfoData.classGuid = System.Guid.Empty;  //System.Guid.Empty;
            //deviceInfoData.reserved = 0;

            GeneralAPI.SP_DEVICE_INTERFACE_DATA deviceInterfaceData = new GeneralAPI.SP_DEVICE_INTERFACE_DATA();
            deviceInterfaceData.cbSize = Marshal.SizeOf(deviceInterfaceData);
            deviceInterfaceData.flags = 0;
            deviceInterfaceData.interfaceclassGuid = System.Guid.Empty;
            deviceInterfaceData.reserved = 0;

            //GeneralAPI.HidD_GetHidGuid(ref guid);

            for (nDevices = 0; ; nDevices++)
            {
                //if(!GeneralAPI.SetupDiEnumDeviceInfo(hDevInfo, (uint)nDevices,deviceInfoData))
                if (!GeneralAPI.SetupDiEnumDeviceInterfaces(hDevInfo, IntPtr.Zero, ref guid, (UInt32)nDevices, ref deviceInterfaceData))
                {
                    int errorNo = Marshal.GetLastWin32Error();
                    if (errorNo == GeneralAPI.WinError.ERROR_NO_MORE_ITEMS)
                    {
                        break;
                    }
                    else
                    {
                        GeneralAPI.SetupDiDestroyDeviceInfoList(hDevInfo);
                        return -1;
                    }
                }
            }

            GeneralAPI.SetupDiDestroyDeviceInfoList(hDevInfo);

            //g_lTotalDevices = nDevices;
            return nDevices;
        }

        private bool ConnectToDevice(bool bUseAsyncTo)
        {
            if (g_lTotalDevices == 0)
            {
                Console.WriteLine("There is no device connected!");
                return false;
            }

            //Open write handle
            g_hWrite = OpenDeviceHandle(devGUID, BasicParm._PIPE_TYPE.WRITE_PIPE, g_unDeviceNumber, bUseAsyncTo);
            if (g_hWrite.ToInt32() == GeneralAPI.INVALID_HANDLE_VALUE)
            {
                Console.WriteLine("Can not get write handle!");
                return false;
            }

            //Open Read handle
            g_hRead = OpenDeviceHandle(devGUID, BasicParm._PIPE_TYPE.READ_PIPE, g_unDeviceNumber, bUseAsyncTo);
            if (g_hRead.ToInt32() == GeneralAPI.INVALID_HANDLE_VALUE)
            {
                Console.WriteLine("Can not get read handle!");
                return false;
            }


            return true;

        }

        private IntPtr OpenDeviceHandle(Guid pGuid, BasicParm._PIPE_TYPE pipe_type, uint ui_deviceNum, bool buseAsyncIo)
        {
            IntPtr hDev;
            //g_bUseAsyncIo = buseAsyncIo;
            IntPtr hdinfo = GeneralAPI.SetupDiGetClassDevs(ref pGuid, 0, IntPtr.Zero, GeneralAPI.DIGCF_DEVICEINTERFACE | GeneralAPI.DIGCF_PRESENT);
            if (hdinfo.ToInt32() == GeneralAPI.INVALID_HANDLE_VALUE)
            {
                return IntPtr.Zero;
                //throw new Exception("Invalid Handle!");
            }

            GeneralAPI.SP_DEVINFO_DATA deviceInfoData = new GeneralAPI.SP_DEVINFO_DATA();
            //deviceInfoData.cbSize = Marshal.SizeOf(deviceInfoData);//28=16+4+4+4
            //deviceInfoData.devInst = 0;
            //deviceInfoData.classGuid = System.Guid.Empty;
            //deviceInfoData.reserved = 0;

            GeneralAPI.SP_DEVICE_INTERFACE_DATA deviceInterfaceData = new GeneralAPI.SP_DEVICE_INTERFACE_DATA();
            deviceInterfaceData.cbSize = Marshal.SizeOf(deviceInterfaceData);
            deviceInterfaceData.flags = 0;
            deviceInterfaceData.interfaceclassGuid = System.Guid.Empty;
            deviceInterfaceData.reserved = 0;

            // see if the device with the corresponding DeviceNumber is present
            //if(!GeneralAPI.SetupDiEnumDeviceInfo(hdinfo, 0, deviceInfoData))
            if (!GeneralAPI.SetupDiEnumDeviceInterfaces(hdinfo, IntPtr.Zero, ref pGuid, ui_deviceNum, ref deviceInterfaceData))
            {
                GeneralAPI.SetupDiDestroyDeviceInfoList(hdinfo);
                return IntPtr.Zero;
            }

            int dwDeviceSize = 0;
            bool result = GeneralAPI.SetupDiGetDeviceInterfaceDetail(hdinfo, ref deviceInterfaceData, IntPtr.Zero, 0, ref dwDeviceSize, deviceInfoData);
            dwDeviceSize += 32;
            IntPtr detailDataBuffer = Marshal.AllocHGlobal(dwDeviceSize);
            if (detailDataBuffer == IntPtr.Zero)
            {
                GeneralAPI.SetupDiDestroyDeviceInfoList(hdinfo);
                return IntPtr.Zero;
            }

            GeneralAPI.SP_INTERFACE_DEVICE_DETAIL_DATA detailData = new GeneralAPI.SP_INTERFACE_DEVICE_DETAIL_DATA();
            detailData.cbSize = Marshal.SizeOf(typeof(GeneralAPI.SP_INTERFACE_DEVICE_DETAIL_DATA));//GeneralAPI.SP_INTERFACE_DEVICE_DETAIL_DATA);

            //Marshal.WriteInt32(detailDataBuffer, 4);          
            Marshal.StructureToPtr(detailData, detailDataBuffer, false);


            //int temp = 0;
            result = GeneralAPI.SetupDiGetDeviceInterfaceDetail(hdinfo, ref deviceInterfaceData, detailDataBuffer, dwDeviceSize, ref dwDeviceSize, deviceInfoData);

            if (!result)
            {
                GeneralAPI.SetupDiDestroyDeviceInfoList(hdinfo);
                Marshal.FreeHGlobal(detailDataBuffer);
                //MessageBox.Show("Get Device Interface Detail failed!");
                Console.WriteLine("Get Device Interface Detail failed!");
                return IntPtr.Zero;
            }

            IntPtr pdevicePathName = (IntPtr)((int)detailDataBuffer + 4);
            string devicePahtName = Marshal.PtrToStringAuto(pdevicePathName);
            Marshal.FreeHGlobal(detailDataBuffer);
            GeneralAPI.SetupDiDestroyDeviceInfoList(hdinfo);

            //string test = "\\\\?\\usb#vid_064b&pid_1212#6&2ed19cf&0&3#{eb8322c5-8b49-4feb-ae6e-c99b2b232045}";

            if (BasicParm._PIPE_TYPE.WRITE_PIPE == pipe_type)
            {
                if (buseAsyncIo)
                {
                    g_hWriteEvent = GeneralAPI.CreateEvent(IntPtr.Zero, true, true, null);
                    if (!GeneralAPI.ResetEvent(g_hWriteEvent))
                    {
                        //MessageBox.Show("Reset write Event failed!!");
                        Console.WriteLine("Reset write Event failed!!");
                    }
                }
                devicePahtName += "\\PIPE_0x01";

            }
            else if (BasicParm._PIPE_TYPE.READ_PIPE == pipe_type)
            {
                if (buseAsyncIo)
                {
                    g_hReadEvent = GeneralAPI.CreateEvent(IntPtr.Zero, true, true, null);
                    if (!GeneralAPI.ResetEvent(g_hReadEvent))
                    {
                        //MessageBox.Show("Reset Event failed!!!");
                        Console.WriteLine("Reset Event failed!!!");
                    }
                }
                devicePahtName += "\\PIPE_0x00";
            }
            else
            {
                return IntPtr.Zero;
            }

            // determine the attributes
            uint dwAttribs;
            if (buseAsyncIo)
                dwAttribs = BasicParm.FILE_FLAG_OVERLAPPED;
            else
                dwAttribs = BasicParm.FILE_ATTRIBUTE_NORMAL;
            hDev = GeneralAPI.CreateFile(
                        devicePahtName,
                        (uint)(BasicParm.GENERIC_READ | BasicParm.GENERIC_WRITE),
                        (uint)(BasicParm.FILE_SHARE_READ | BasicParm.FILE_SHARE_WRITE),
                        BasicParm.NULL,
                        BasicParm.OPEN_EXISTING,
                        dwAttribs,
                        BasicParm.NULL);
            return hDev;
        }

        /// <summary>
        /// Write an array to the connected device .
        /// </summary>
        /// <param name="outPutReport_arr"></param>
        /// <returns></returns>
        internal bool UsbReportWrite(byte[] outPutReport_arr)
        {
            bool status = false;
            uint nBytes = 0;

            if (!GeneralAPI.ResetEvent(g_hWriteEvent))
            {
                Console.WriteLine("Reset write Event failed!!!");
            }

            //GCHandle pinnedBuffer = GCHandle.Alloc(outPutReport_arr, GCHandleType.Pinned);
            GCHandle pinnedOverlapped = GCHandle.Alloc(g_WriteOverlapped, GCHandleType.Pinned);

            // write file
            status = GeneralAPI.WriteFile(g_hWrite, outPutReport_arr, (uint)outPutReport_arr.Length, ref nBytes, pinnedOverlapped.AddrOfPinnedObject());
            if (status == false)
            {
                int error = Marshal.GetLastWin32Error();
                if (error == 997)
                {
                    status = true;
                }
            }

            if (GeneralAPI.WaitForSingleObject(g_hWriteEvent, BasicParm.MAX_IO_WAIT) != 0)
            {
                status = false;
            }

            if (!GeneralAPI.GetOverlappedResult(g_hWrite, pinnedOverlapped.AddrOfPinnedObject(), out nBytes, 0))
            {
                status = false;
            }

            if (!status)
                GeneralAPI.CancelIo(g_hWrite);

            if (nBytes != (uint)outPutReport_arr.Length)
            {
                status = false;
            }

            return status;
        }

        /// <summary>
        /// Write an array to the connected device .
        /// </summary>
        /// <param name="outPutReport_arr"></param>
        /// <returns></returns>
        internal bool UsbReportWrite(int[] outPutReport_arr)
        {
            bool status = false;
            uint nBytes = 0;

            if (!GeneralAPI.ResetEvent(g_hWriteEvent))
            {
                Console.WriteLine("Reset write Event failed!!!");
            }

            //GCHandle pinnedBuffer = GCHandle.Alloc(outPutReport_arr, GCHandleType.Pinned);
            GCHandle pinnedOverlapped = GCHandle.Alloc(g_WriteOverlapped, GCHandleType.Pinned);

            // write file
            status = GeneralAPI.WriteFile(g_hWrite, outPutReport_arr, (uint)outPutReport_arr.Length * 4, ref nBytes, pinnedOverlapped.AddrOfPinnedObject());
            if (status == false)
            {
                int error = Marshal.GetLastWin32Error();
                if (error == 997)
                {
                    status = true;
                }
            }

            if (GeneralAPI.WaitForSingleObject(g_hWriteEvent, BasicParm.MAX_IO_WAIT) != 0)
            {
                status = false;
            }

            if (!GeneralAPI.GetOverlappedResult(g_hWrite, pinnedOverlapped.AddrOfPinnedObject(), out nBytes, 0))
            {
                status = false;
            }

            if (!status)
                GeneralAPI.CancelIo(g_hWrite);

            if (nBytes != (uint)outPutReport_arr.Length * 4)
            {
                status = false;
            }

            return status;
        }

        /// <summary>
        /// Write an array to the connected device .
        /// </summary>
        /// <param name="outPutReport_arr"></param>
        /// <returns></returns>
        internal bool UsbReportWrite(uint[] outPutReport_arr)
        {
            bool status = false;
            uint nBytes = 0;

            if (!GeneralAPI.ResetEvent(g_hWriteEvent))
            {
                Console.WriteLine("Reset write Event failed!!!");
            }

            //GCHandle pinnedBuffer = GCHandle.Alloc(outPutReport_arr, GCHandleType.Pinned);
            GCHandle pinnedOverlapped = GCHandle.Alloc(g_WriteOverlapped, GCHandleType.Pinned);

            // write file
            status = GeneralAPI.WriteFile(g_hWrite, outPutReport_arr, (uint)outPutReport_arr.Length * 4, ref nBytes, pinnedOverlapped.AddrOfPinnedObject());
            if (status == false)
            {
                int error = Marshal.GetLastWin32Error();
                if (error == 997)
                {
                    status = true;
                }
            }

            if (GeneralAPI.WaitForSingleObject(g_hWriteEvent, BasicParm.MAX_IO_WAIT) != 0)
            {
                status = false;
            }

            if (!GeneralAPI.GetOverlappedResult(g_hWrite, pinnedOverlapped.AddrOfPinnedObject(), out nBytes, 0))
            {
                status = false;
            }

            if (!status)
                GeneralAPI.CancelIo(g_hWrite);

            if (nBytes != (uint)outPutReport_arr.Length * 4)
            {
                status = false;
            }

            return status;
        }

        /// <summary>
        /// Read from the connected device, and return the data in outPutReport_arr
        /// </summary>
        /// <param name="outPutReport_arr"></param>
        /// <returns></returns>
        internal bool UsbReportRead(byte[] outPutReport_arr)
        {
            bool status = false;
            uint nBytes = 0;

            if (!GeneralAPI.ResetEvent(g_hReadEvent))
            {
                //MessageBox.Show("Reset read Event Failed!!!");
                Console.WriteLine("Reset read Event Failed!!!");
            }

            //GCHandle pinnedBuffer = GCHandle.Alloc(outPutReport_arr, GCHandleType.Pinned);
            GCHandle pinnedOverlapped = GCHandle.Alloc(g_ReadOverlapped, GCHandleType.Pinned);

            // Read file
            status = GeneralAPI.ReadFile(g_hRead, outPutReport_arr, (uint)outPutReport_arr.Length, ref nBytes, pinnedOverlapped.AddrOfPinnedObject());
            //Thread.Sleep(2000);
            //uint temp = GeneralAPI.WaitForSingleObject(g_hReadEvent, ParamDefine.MAX_IO_WAIT);
            //status = GeneralAPI.WriteFile(g_hWrite, outPutReport_arr, (uint)64, ref nBytes, pinnedOverlapped.AddrOfPinnedObject());
            if (status == false)
            {
                int error = Marshal.GetLastWin32Error();
                if (error == 997)
                {
                    status = true;
                    //Console.WriteLine("error = {0}", error);
                }
            }


            if (GeneralAPI.WaitForSingleObject(g_hReadEvent, BasicParm.MAX_IO_WAIT) != 0)
            {
                status = false;
                //Console.WriteLine("WaitForSingleObject wait time out,nBytes={0}", nBytes);
            }

            if (!GeneralAPI.GetOverlappedResult(g_hRead, pinnedOverlapped.AddrOfPinnedObject(), out nBytes, 0))
            {
                status = false;
                //Console.WriteLine("GetOverlappedResult ,nBytes={0}", nBytes);
            }

            if (!status)
                GeneralAPI.CancelIo(g_hRead);

            if (nBytes != (uint)outPutReport_arr.Length)
            {
                status = false;
            }

            return status;
        }

        /// <summary>
        /// Read from the connected device, and return the data in outPutReport_arr
        /// </summary>
        /// <param name="outPutReport_arr"></param>
        /// <returns></returns>
        internal bool UsbReportRead(int[] outPutReport_arr)
        {
            bool status = false;
            uint nBytes = 0;

            if (!GeneralAPI.ResetEvent(g_hReadEvent))
            {
                //MessageBox.Show("Reset read Event Failed!!!");
                Console.WriteLine("Reset read Event Failed!!!");
            }

            //GCHandle pinnedBuffer = GCHandle.Alloc(outPutReport_arr, GCHandleType.Pinned);
            GCHandle pinnedOverlapped = GCHandle.Alloc(g_ReadOverlapped, GCHandleType.Pinned);

            // Read file
            status = GeneralAPI.ReadFile(g_hRead, outPutReport_arr, (uint)outPutReport_arr.Length * 4, ref nBytes, pinnedOverlapped.AddrOfPinnedObject());
            //Thread.Sleep(2000);
            //uint temp = GeneralAPI.WaitForSingleObject(g_hReadEvent, ParamDefine.MAX_IO_WAIT);
            //status = GeneralAPI.WriteFile(g_hWrite, outPutReport_arr, (uint)64, ref nBytes, pinnedOverlapped.AddrOfPinnedObject());
            if (status == false)
            {
                int error = Marshal.GetLastWin32Error();
                if (error == 997)
                {
                    status = true;
                    //Console.WriteLine("error = {0}", error);
                }
            }


            if (GeneralAPI.WaitForSingleObject(g_hReadEvent, BasicParm.MAX_IO_WAIT) != 0)
            {
                status = false;
                //Console.WriteLine("WaitForSingleObject wait time out,nBytes={0}", nBytes);
            }

            if (!GeneralAPI.GetOverlappedResult(g_hRead, pinnedOverlapped.AddrOfPinnedObject(), out nBytes, 0))
            {
                status = false;
                //Console.WriteLine("GetOverlappedResult ,nBytes={0}", nBytes);
            }

            if (!status)
                GeneralAPI.CancelIo(g_hRead);

            if (nBytes != (uint)outPutReport_arr.Length * 4)
            {
                status = false;
            }

            return status;
        }

        /// <summary>
        /// Read from the connected device, and return the data in outPutReport_arr
        /// </summary>
        /// <param name="outPutReport_arr"></param>
        /// <returns></returns>
        internal bool UsbReportRead(uint[] outPutReport_arr)
        {
            bool status = false;
            uint nBytes = 0;

            if (!GeneralAPI.ResetEvent(g_hReadEvent))
            {
                Console.WriteLine("Reset read Event Failed!!!");
            }

            //GCHandle pinnedBuffer = GCHandle.Alloc(outPutReport_arr, GCHandleType.Pinned);
            GCHandle pinnedOverlapped = GCHandle.Alloc(g_ReadOverlapped, GCHandleType.Pinned);

            // Read file
            status = GeneralAPI.ReadFile(g_hRead, outPutReport_arr, (uint)outPutReport_arr.Length * 4, ref nBytes, pinnedOverlapped.AddrOfPinnedObject());
            if (status == false)
            {
                int error = Marshal.GetLastWin32Error();
                if (error == 997)
                {
                    status = true;
                    //Console.WriteLine("error = {0}", error);
                }
            }


            if (GeneralAPI.WaitForSingleObject(g_hReadEvent, BasicParm.MAX_IO_WAIT) != 0)
            {
                status = false;
                Console.WriteLine("WaitForSingleObject wait time out,nBytes={0}", nBytes);
            }

            if (!GeneralAPI.GetOverlappedResult(g_hRead, pinnedOverlapped.AddrOfPinnedObject(), out nBytes, 0))
            {
                status = false;
                Console.WriteLine("GetOverlappedResult ,nBytes={0}", nBytes);
            }

            if (!status)
                GeneralAPI.CancelIo(g_hRead);

            if (nBytes != (uint)outPutReport_arr.Length * 4)
            {
                status = false;
            }

            return status;
        }

        /// <summary>
        /// Read from the connected device, and return the data in outPutReport_arr
        /// </summary>
        /// <param name="outPutReport_arr"></param>
        /// <returns></returns>
        internal bool UsbReportRead(UInt16[] outPutReport_arr)
        {
            bool status = false;
            uint nBytes = 0;

            if (!GeneralAPI.ResetEvent(g_hReadEvent))
            {
                Console.WriteLine("Reset read Event Failed!!!");
            }

            //GCHandle pinnedBuffer = GCHandle.Alloc(outPutReport_arr, GCHandleType.Pinned);
            GCHandle pinnedOverlapped = GCHandle.Alloc(g_ReadOverlapped, GCHandleType.Pinned);

            // Read file
            status = GeneralAPI.ReadFile(g_hRead, outPutReport_arr, (uint)outPutReport_arr.Length * 2, ref nBytes, pinnedOverlapped.AddrOfPinnedObject());
            if (status == false)
            {
                int error = Marshal.GetLastWin32Error();
                if (error == 997)
                {
                    status = true;
                    //Console.WriteLine("error = {0}", error);
                }
            }


            if (GeneralAPI.WaitForSingleObject(g_hReadEvent, BasicParm.MAX_IO_WAIT) != 0)
            {
                status = false;
                Console.WriteLine("WaitForSingleObject wait time out,nBytes={0}", nBytes);
            }

            if (!GeneralAPI.GetOverlappedResult(g_hRead, pinnedOverlapped.AddrOfPinnedObject(), out nBytes, 0))
            {
                status = false;
                Console.WriteLine("GetOverlappedResult ,nBytes={0}", nBytes);
            }

            if (!status)
                GeneralAPI.CancelIo(g_hRead);

            if (nBytes != (uint)outPutReport_arr.Length * 2)
            {
                status = false;
            }

            return status;
        }
    }

    #region ADMP Output report index defined
    internal class DrcParamIndex
    {
        public static readonly uint ERROR = 0;
        public static readonly uint MUTE_LEVEL = 1;
        public static readonly uint LT = 2;
        public static readonly uint CT = 3;
        public static readonly uint ET = 4;
        public static readonly uint NT = 5;
        public static readonly uint SMAX = 6;
        public static readonly uint SMIN = 7;
        public static readonly uint GAIN_ADJ = 8;
        public static readonly uint CS = 9;
        public static readonly uint ES = 10;

        public static readonly uint TA = 11;
        public static readonly uint TR = 12;
        public static readonly uint AT = 13;
        public static readonly uint RT = 14;
        public static readonly uint TAV = 15;
        public static readonly uint RIPPLE_TH = 16;

        //不需要转换为定点数
        public static readonly uint HD_SAMPLE = 17;
        public static readonly uint NG_HOLD_SAMPLE = 18;
        public static readonly uint ngen = 19;
        public static readonly uint peak_sel = 20;

        //参数个数
        public static readonly uint count_param = 21;
    }

    internal class Index
    {
        //public IndexWrite() { }
        #region write index
        // header: 4
        public static readonly uint command = 0;
        public static readonly uint unused1 = 1;
        public static readonly uint unused2 = 2;
        public static readonly uint unused3 = 3;
        //DRC base
        public static readonly uint drc1_base = 4;
        public static readonly uint drcL_base = drc1_base + DrcParamIndex.count_param; //4+21 = 25
        public static readonly uint drcM_base = drcL_base + DrcParamIndex.count_param; //25 + 21 = 46;
        public static readonly uint drcH_base = drcM_base + DrcParamIndex.count_param; //46 + 21 = 67;
        public static readonly uint drc5_base = drcH_base + DrcParamIndex.count_param; //67 + 21 = 88;

        //HPF addr
        public static readonly uint HPF1_index = drc5_base + DrcParamIndex.count_param; //88 + 21 = 109
        public static readonly uint HPF2_index = HPF1_index + 1; //110
        public static readonly uint HPF2_offset = HPF2_index + 1; //111
        public static readonly uint LPF1_index = HPF2_offset + 1; //112
        public static readonly uint CrossOver_BW = LPF1_index + 1; //113
        public static readonly uint CrossOver_Fr = CrossOver_BW + 1; //114
        public static readonly uint CrossOver_C1 = CrossOver_Fr + 1; //115
        public static readonly uint CrossOver_C2 = CrossOver_C1 + 1; //116
        public static readonly uint VolumeCtrl = 117u;      //117
        public static readonly uint Rrdc_T_low = 118u;
        public static readonly uint Rrdc_T_High = 119u;
        public static readonly uint Gain_coef_Low = 120u;      //计算DRC peak和rms时的gain
        public static readonly uint Gain_ref_max_Low = 121u;   //最大Gain_ref
        public static readonly uint Gain_coef_Mid = 122u;      //计算DRC peak和rms时的gain
        public static readonly uint Gain_ref_max_Mid = 123u;   //最大Gain_ref
        public static readonly uint Gain_coef_High = 124u;      //计算DRC peak和rms时的gain
        public static readonly uint Gain_ref_max_High = 125u;   //最大Gain_ref
        public static readonly uint IfAutoChange_f0 = 126u;    //是否根据f0自动切换系数
        public static readonly uint IfAutoChange_T = 127u;     //是否根据T自动切换系数

        #endregion write index

        #region read index
        public static readonly uint Rref = 0u;
        public static readonly uint Rrdc = 1u;
        public static readonly uint IV_p1 = 2u;
        public static readonly uint IV_p10 = 11u;
        public static readonly uint IV_p20 = 21u;
        public static readonly uint IV_p30 = 31u;
        public static readonly uint IV_p40 = 41u;
        public static readonly uint IV_p44 = 45u;
        public static readonly uint TestCount = 46u;
        public static readonly uint FirmVersion = 47u;
        public static readonly uint BWIndex = 48u;
        public static readonly uint I_F0 = 49u;
        public static readonly uint I_f1 = 50u;
        public static readonly uint I_f2 = 51u;
        public static readonly uint I_hp = 52u;
        public static readonly uint Rrdc_np = 53u;
        #endregion read index

        #region Module enable Index
        public static readonly uint Module_EQ_MDRC = 4u;
        public static readonly uint Module_CrossOver = 5u;
        public static readonly uint Module_LowDRC = 6u;
        public static readonly uint Module_MidDRC = 7u;
        public static readonly uint Module_HighDRC = 8u;
        public static readonly uint Module_HPF2 = 9u;
        public static readonly uint Module_Lpf1 = 10u;
        public static readonly uint Module_HPF1 = 11u;
        #endregion
    }
    #endregion

    #region Binary data operate
    /// <summary>
    /// Basic interface functions for wave file operate
    /// </summary>
    public class WaveOperate
    {
        public WaveOperate()
        {
            SetDefaultValue();
        }

        public long HeaderLength
        { get { return 56; } }

        private waveHeaderDefault _waveHeaderDefault = new waveHeaderDefault();
        public waveHeaderDefault WaveHeaderDefault
        {
            get { return _waveHeaderDefault; }
        }

        //give the default value of wave header, there are some parms should update when finished file writting
        private void SetDefaultValue()
        {
            _waveHeaderDefault.riffId = 0x46464952;        //'R''I''F''F'
            _waveHeaderDefault.riffSize = 48;			   //56(header len) - 8 = 48  need update
            _waveHeaderDefault.riffType = 0x45564157;	   //'W''A''V'E'
            _waveHeaderDefault.formatID = 0x20746D66;	   //'f''m''t'' '
            _waveHeaderDefault.formatSize = 0x00000010;	   //16 byte
            _waveHeaderDefault.formatFormatTag = 0x0001;   //format
            _waveHeaderDefault.formatChannels = 0x0002;	   //stereo
            _waveHeaderDefault.formatSamplesPerSec = 0xBB80;	   //sampling rate
            //_waveHeaderDefault.formatAvgBytesPerSec = 0x00046500;  //mono 24bits
            //_waveHeaderDefault.formatBlockAlign = 0x06;		//3 byte * 2 channel
            //_waveHeaderDefault.formatBitsPerSample = 0x18;	//bit length = 32
            _waveHeaderDefault.formatAvgBytesPerSec = 0x0002EE00;  //mono 16bits
            _waveHeaderDefault.formatBlockAlign = 0x04;		//2 byte * 2 channel
            _waveHeaderDefault.formatBitsPerSample = 0x10;	//bit length = 16
            _waveHeaderDefault.factID = 0x74636166;		    //'f''a''c''t'
            _waveHeaderDefault.factSize = 0x00000004;
            _waveHeaderDefault.factData = 0;            //need update
            _waveHeaderDefault.dataID = 0x61746164;		//'d''a''t''a'
            _waveHeaderDefault.dataSize = 0;            //need update

        }
        public struct waveHeaderDefault
        {
            public uint riffId;
            public uint riffSize;   //offset:4
            public uint riffType;
            public uint formatID;
            public uint formatSize;
            public ushort formatFormatTag;
            public ushort formatChannels;
            public uint formatSamplesPerSec;
            public uint formatAvgBytesPerSec;
            public ushort formatBlockAlign;
            public ushort formatBitsPerSample;
            public uint factID;
            public uint factSize;
            public uint factData;   //offset:44
            public uint dataID;
            public uint dataSize;   //offset:52
        }

        //public struct waveDefaultHeader
        //{
        //    public int reffId;
        //}


        /// <summary>
        /// write a chunk wave data to file
        /// </summary>
        /// <param name="bwriter">writer handle</param>
        /// <param name="waveData">original data for writting</param>
        public void WriteWaveDataToFile(BinaryWriter bwriter, byte[] waveData)
        {
            bwriter.Write(waveData);
        }

        /// <summary>
        /// write a chunk wave data to file.
        /// </summary>
        /// <param name="bwriter">writer handle</param>
        /// <param name="waveData">original data for writting</param>
        public void WriteWaveDataToFile(BinaryWriter bwriter, Int16[] waveData)
        {
            bwriter.Write("temp");
        }

        /// <summary>
        ///  write a chunk wave data to file
        /// </summary>
        /// <param name="bwriter">writer handle</param>
        /// <param name="waveData">A container which store original data for writting</param>
        /// <param name="count">The number of element in the container</param>
        public void WriteWaveDataToFile(BinaryWriter bwriter, ArrayList waveData, int count)
        {
            for (int i = 0; i < count; i++)
            {
                bwriter.Write((byte[])waveData[i]);
            }
        }

        /// <summary>
        /// write a chunk wave data to file
        /// </summary>
        /// <param name="bwriter">writer handle</param>
        /// <param name="waveData">original data for writting</param>
        /// <param name="index">The starting point in buffer at which to begin writing.</param>
        /// <param name="count">The number of bytes to write. </param>
        public void WriteWaveDataToFile(BinaryWriter bwriter, byte[] waveData, int index, int count)
        {
            bwriter.Write(waveData, index, count);
        }

        public void WriteWaveDataToFile(BinaryWriter bwriter, byte[][] waveData)
        {
            Console.WriteLine("WriteWaveDataToFile will write {0} times", waveData.Length);
            for (int i = 0; i < waveData.Length; i++)
            {
                bwriter.Write(waveData[i]);
            }
        }

        public void WriteWaveDataToFile(BinaryWriter bwriter, byte[][] waveData, int count)
        {
            Console.WriteLine("WriteWaveDataToFile will write {0} times", count);
            for (int i = 0; i < count; i++)
            {
                bwriter.Write(waveData[i]);
            }
        }

        /// <summary>
        /// write an array to several files
        /// </summary>
        /// <param name="bwriters">writer handles</param>
        /// <param name="waveData">original data for writting</param>
        /// <param name="_countFile">the number of files be written</param>
        /// <param name="_countBytes">the number of bytes to write</param>
        public void WriteWaveDataToFiles(BinaryWriter[] bwriters, byte[] waveData, int _countFiles, int _countBytes)
        {
            int writeInterval = waveData.Length / _countFiles;
            //int writeLength = writeInterval * _wordLength / 4;
            for (int i = 0; i < _countFiles; i++)
            {
                bwriters[i].Write(waveData, writeInterval * i, _countBytes);
            }
        }

        /// <summary>
        /// write an array to several files
        /// </summary>
        /// <param name="bwriters">writer handles</param>
        /// <param name="waveData">original data for writting</param>
        /// <param name="_playbackMode">play back mode:I2S, TDM4, TDM8, TDM16</param>
        /// <param name="_wordLength">significance bit number in every four bits</param>
        /// <returns></returns>
        public bool WriteWaveDataToFiles(BinaryWriter[] bwriters, byte[] waveData, string _playbackMode, int _wordLength)
        {
            bool result = true;
            int _countFiles = 0;
            switch (_playbackMode)
            {
                case "I2S":
                    _countFiles = 1;
                    break;
                case "TDM4":
                case "TDM8":
                case "TDM16":
                    _countFiles = Convert.ToInt32(_playbackMode.TrimStart("TDM".ToCharArray(0, 3)));
                    break;
                default:
                    Console.WriteLine("Play back mode Select faile");
                    result = false;
                    break;
            }
            int writeInterval = waveData.Length / _countFiles;
            int writeLength = writeInterval * _wordLength / 4;
            for (int i = 0; i < _countFiles; i++)
            {
                bwriters[i].Write(waveData, writeInterval * i, writeLength);
            }
            return result;
        }

        /// <summary>
        /// write a buffer to several files
        /// </summary>
        /// <param name="bwriters">writer handles</param>
        /// <param name="waveData">two-dimensional original data for writting</param>
        /// <param name="count">The number of 64KB buffer will be processed.</param>
        /// <param name="_playbackMode">play back mode:I2S, TDM4, TDM8, TDM16</param>
        /// <param name="_wordLength">significance bit number in every four bits</param>
        /// <returns></returns>
        public bool WriteWaveDataToFiles(BinaryWriter[] bwriters, byte[][] waveData, int count, string _playbackMode, int _wordLength)
        {
            bool result = true;
            int _countFiles = 0;
            //int bufSizeOnceRead = 0x10000; 
            int usefulBufSizeOnceRead = 0xFFC0;        //Useful byte num in once read buffer
            int baseLengthOneChannel = 3;
            switch (_playbackMode)
            {
                case "I2S":
                    _countFiles = 1;
                    break;
                case "TDM4":
                case "TDM8":
                case "TDM16":
                    _countFiles = Convert.ToInt32(_playbackMode.TrimStart("TDM".ToCharArray(0, 3)));
                    break;
                default:
                    Console.WriteLine("Play back mode Select faile");
                    result = false;
                    break;
            }

            //int writeInterval = waveData.Length / _countFiles;
            //int writeLength = writeInterval * _wordLength / 4;
            //count = count / _countFiles;  //精度损失了，用double

            int writeLength = usefulBufSizeOnceRead * count * _wordLength / (_countFiles * baseLengthOneChannel);// baseLengthOneChannel;
            Console.WriteLine("Each wav file write length:{0}", writeLength);
            for (int i = 0; i < _countFiles; i++)
            {
                bwriters[i].Write(waveData[i], 0, writeLength);
            }
            return result;
        }

        /// <summary>
        /// write a buffer to several files
        /// </summary>
        /// <param name="bwriters">writer handles</param>
        /// <param name="waveData">original data for writting</param>
        /// <param name="_playbackMode">play back mode:I2S, TDM4, TDM8, TDM16</param>
        /// <param name="_wordLength">significance bit number in every four bits</param>
        /// <param name="countBytes">The number of bytes will write.</param>
        /// <returns></returns>
        public bool WriteWaveDataToFiles(BinaryWriter[] bwriters, byte[] waveData, string _playbackMode, int _wordLength, int countBytes)
        {
            bool result = true;
            int _countFiles = 0;
            switch (_playbackMode)
            {
                case "I2S":
                    _countFiles = 1;
                    break;
                case "TDM4":
                case "TDM8":
                case "TDM16":
                    _countFiles = Convert.ToInt32(_playbackMode.TrimStart("TDM".ToCharArray(0, 3)));
                    break;
                default:
                    Console.WriteLine("Play back mode Select faile");
                    result = false;
                    break;
            }
            int writeInterval = waveData.Length / _countFiles;
            int rawLength = countBytes / _countFiles;
            int writeLength = rawLength * _wordLength / 4;
            for (int i = 0; i < _countFiles; i++)
            {
                bwriters[i].Write(waveData, writeInterval * i, writeLength);
            }
            return result;
        }

        /// <summary>
        /// Write the whole default wav head to file
        /// </summary>
        /// <param name="bwriter"></param>
        /// <param name="_waveHeaderDefault"></param>
        public void WriteWaveHeadToFile(BinaryWriter bwriter, waveHeaderDefault _waveHeaderDefault)
        {
            bwriter.Write(_waveHeaderDefault.riffId);
            bwriter.Write(_waveHeaderDefault.riffSize);
            bwriter.Write(_waveHeaderDefault.riffType);
            bwriter.Write(_waveHeaderDefault.formatID);
            bwriter.Write(_waveHeaderDefault.formatSize);
            bwriter.Write(_waveHeaderDefault.formatFormatTag);
            bwriter.Write(_waveHeaderDefault.formatChannels);
            bwriter.Write(_waveHeaderDefault.formatSamplesPerSec);
            bwriter.Write(_waveHeaderDefault.formatAvgBytesPerSec);
            bwriter.Write(_waveHeaderDefault.formatBlockAlign);
            bwriter.Write(_waveHeaderDefault.formatBitsPerSample);
            bwriter.Write(_waveHeaderDefault.factID);
            bwriter.Write(_waveHeaderDefault.factSize);
            bwriter.Write(_waveHeaderDefault.factData);
            bwriter.Write(_waveHeaderDefault.dataID);
            bwriter.Write(_waveHeaderDefault.dataSize);
        }

        /// <summary>
        /// Write a single int type value to file
        /// </summary>
        /// <param name="bwriter"></param>
        /// <param name="intWaveHeaderValue"></param>
        public void WriteIntValueToFile(BinaryWriter bwriter, int intValue)
        {
            bwriter.Write(intValue);
        }

        /// <summary>
        /// write a single short type value to file
        /// </summary>
        /// <param name="bwriter"></param>
        /// <param name="shortWaveHeaderValue"></param>
        public void WriteShortValueToFile(BinaryWriter bwriter, short shortValue)
        {
            bwriter.Write(shortValue);
        }

        /// <summary>
        /// write a byte to file
        /// </summary>
        /// <param name="bwriter"></param>
        /// <param name="byteValue"></param>
        public void WriteByteValueToFile(BinaryWriter bwriter, byte byteValue)
        {
            bwriter.Write(byteValue);
        }
    }

    #endregion

    #region Register Map Definition
    public class Register
    {
        /// <summary>
        /// One Register(8 bits)
        /// </summary>
        /// <param name="_regName">This register's name.</param>
        /// <param name="_regAddress">This register's address.</param>
        /// <param name="_paras">All of the units, which make up the this register.And the params
        /// add from bit0 to bit7.
        /// Format:(p1+p2)->string p1_name,int p1_bitsCount,string p2_name,int p2_bitsCount</param>
        public Register(string _regName, int _regAddress, params object[] _paras)
        {
            this.regName = _regName;
            this.paras = _paras;
            this.regAddr = _regAddress;
            SeparateParamsToUnits();
        }

        private string regName;
        public string RegName
        {
            get { return regName; }
        }

        private int regAddr;
        public int RegAddress
        {
            get { return regAddr; }
        }

        private object[] paras;

        private byte regValue = 0;
        public byte RegValue
        {
            get { return regValue; }
            set
            {
                if (this.regValue != value)
                {
                    this.regValue = value;
                    SeparateParamsToUnits();
                }
            }
        }

        private struct UnitInReg
        {
            public string unitName;
            public int unitValue;
            public int startIndex;
            public int bitsCount;
            public object controllor;
        }

        #region Methods
        public void BindingWith(ref int value, string _uintName)
        {
            value = (int)ValuesTable[_uintName];
        }

        private Hashtable ValuesTable = new Hashtable();

        //private Hashtable UnitsValue
        //{
        //    get { return ValuesTable; }
        //}

        public int GetUnitValue(string _unitName)
        {
            int temp;
            try
            {
                temp = (int)ValuesTable[_unitName];
            }
            catch
            {
                Console.WriteLine("GetUnitValue {0} from Reg{1} failed.", _unitName, RegAddress.ToString("X"));
                return -1;
            }
            return temp;
        }

        public bool SetUnitValue(string _unitName, int _value)
        {
            int temp = (int)ValuesTable[_unitName];
            if (temp != _value)
                try
                {
                    ValuesTable[_unitName] = _value;
                    UpdataRegValue();
                }
                catch
                {
                    return false;
                }
            return true;
        }

        // Format:(p1+p2)->string p1_name,int p1_bits,string p2_name,int p2_bits
        private void SeparateParamsToUnits()
        {
            ValuesTable.Clear();
            int startIndex = 0;
            for (int i = 0; i < (int)(paras.Length / 2); i++)
            {
                ValuesTable.Add((string)paras[i * 2], CalcUnitValue(startIndex, (int)paras[i * 2 + 1]));   //The firstly unit start at bit0.
                startIndex += (int)paras[i * 2 + 1];    //The next unit start index.
            }
        }

        /// <summary>
        /// Set register value by each unit's value.
        /// </summary>
        private void UpdataRegValue()
        {
            int startIndex = 0;
            int temp = 0;
            for (int i = 0; i < (int)(paras.Length / 2); i++)
            {
                //Get unit name by paras array, and get each unit value from ValuesTable, which just updated.
                temp += CalcRegValue(startIndex, (int)ValuesTable[(string)paras[i * 2]]);
                startIndex += (int)paras[i * 2 + 1];
            }
            //for (int i = 0; i < units.Count; i++)
            //{
            //    temp += CalcRegValue(units[i].startIndex, units[i].unitValue);
            //}
            this.regValue = (byte)temp;
        }

        /// <summary>
        /// Calculate each unit value by register value.
        /// </summary>
        /// <param name="startIndex"></param>
        /// <param name="bitsCount"></param>
        /// <returns></returns>
        private int CalcUnitValue(int startIndex, int bitsCount)
        {
            int unitValue = 0;
            int flag = (int)(Math.Pow(2, (startIndex + bitsCount)) - Math.Pow(2, startIndex));
            unitValue = (regValue & flag) >> startIndex;

            return unitValue;
        }

        /// <summary>
        /// Calculate register value by each unit value.
        /// </summary>
        /// <returns></returns>
        private int CalcRegValue(int startIndex, int value)
        {
            return (value << startIndex);
        }
        //public void SetRegValue(byte _val)
        //{
        //    this.regValue = _val;
        //}

        //public byte GetRegValue()
        //{
        //    return this.regValue;
        //}
        #endregion
    }

    //class RegistersBlock
    //{
    //    private Register _reg;
    //    public RegistersBlock()
    //    { }
    //}

    public class RegisterMap
    {
        private List<Register> RegistersList = new List<Register> { };
        public RegisterMap()
        { }

        public void Add(Register _reg)
        {
            this.RegistersList.Add(_reg);
        }

        public void Remove(Register _reg)
        {
            this.RegistersList.Remove(_reg);
        }

        public void RemoveAt(int index)
        {
            this.RegistersList.RemoveAt(index);
        }

        public void Clear()
        {
            this.RegistersList.Clear();
        }

        public int Count()
        {
            return this.RegistersList.Count;
        }

        /// <summary>
        /// Get the register by index.
        /// </summary>
        /// <param name="index">Int type index.</param>
        /// <returns></returns>
        //public Register this[int index]
        //{
        //    get { return RegistersList[index];}
        //}

        /// <summary>
        /// Get the register by register's address.
        /// </summary>
        /// <param name="_regAddress">Int type register address.</param>
        /// <returns></returns>
        public Register this[int _regAddress]
        {
            get
            {
                foreach (Register reg in RegistersList)
                {
                    if (reg.RegAddress == _regAddress)
                        return reg;
                }
                return null;
            }
        }

        /// <summary>
        /// Get the register by register's name.
        /// </summary>
        /// <param name="_regName">string type register name.</param>
        /// <returns></returns>
        public Register this[string _regName]
        {
            get
            {
                foreach (Register reg in RegistersList)
                {
                    if (reg.RegName == _regName)
                        return reg;
                }
                return null;
            }
        }

        //private bool WriteAll()
        //{
        //    return false;
        //}

        //byte[] writeRegs;
        //public bool WriteRange(int startAdd, int count)
        //{
        //    writeRegs = new byte[count];
        //}


    }
    #endregion  Register Map Definition

    #region struct define
    #region SPK Protection
    public enum USB_COMMAND : int
    {
        Write_Param = 0x01000000,	//write all parameters
        Read_Status = 0x02000000,	//read all statuses
        HW_Start = 0x03000000,      //start DSP
        HW_Stop = 0x04000000,		//Stop DSP
        Training = 0x05000000,      //DSP training          
        ModuleEnable = 0x06000000   //各个模块Enable选项
    };
    #endregion

    #region ADMP521T
    public enum ADMP521_USB_COMMAND : uint
    {
        FlashLED = 0xCA000001,      //Flash LED
        SportPDM_Start = 0x51000001,      //PDM Recording Start
        SportPDM_Stop = 0x51000002,      //PDM Recording Stop
        //GPIO_I2C_W      =   0xCA000302,      //GPIO pseudo I2C Write
        GPIO_I2C_Read = 0x51000004,      //GPIO pseudo I2C Read
        Mode_Normal = 0x51000005,      //normal mode
        Mode_Test = 0x51000006,      //test mode
        Mode_NormalTest = 0x51000007,      //Normal test mode
        Mode_Fuse = 0x51000008,      //Fuse mode
        ReadFW_Version = 0x51000009,      //Read firm ware version
        SetLR = 0x5100000A,      //Set LR(Hight/Low)
        PostTrim = 0x5100000B,      //Enter PostTrim mode
        OtherCtrl = 0x5100000C,      //Other controls.
        TestInterface = 0x5100000F      //This interface is just for test.
    }

    public enum OTHERCTRL_CMD : uint
    {
        ClkSwitch = 1,                //control the pdm recording clk.(Pin: OE)
        PostTrim = 2,                //Set the chip if need work on post trim mode.
        SDPorAP = 3                 //Swtich between SDP and AP
    }

    public struct ADMP521_REC_BUF
    {
        public byte[][] data;
        public bool ifBufFull;
    }

    public enum ADMP521T_MODE_INIT : uint
    {
        Normal = 0x51000005,
        Test = 0x51000006
    }

    public enum ADMP521T_MODE : uint
    {
        Normal = 0x51000005,           //normal mode,
        Test = 0x51000006,             //test mode,
        Normal_Test = 0x51000007,      //Normal test mode,
        Fuse = 0x51000008,             //Fuse mode
        PostTrim = 0x5100000B,         //PostTrim mode
    }
    #endregion

    #region One Wire Interface
    public enum OneWire_USB_COMMAND : uint
    {
        FlashLED = 0xCA000001,           //Flash LED
        ADI_SDP_CMD_SDRAM_PROGRAM_BOOT = 0xCA000003,
        I2C_Write_Single = 0x50000002,           //Single I2C write
        I2C_Read_Single = 0x50000003,           //Single I2C Read
        I2C_Write_Burst = 0x50000004,           //Burst I2C write
        I2C_Read_Burst = 0x50000005,           //Burst I2C Read
        FuseOn = 0x50000006,           //Fuse On
        FuseOff = 0x50000007,           //Fuse Off
        SetPilot_Width = 0x50000008,           //Pilot width, how many clk cycle
        Update_Fuse_Pulse_Width = 0x50000009,       //update the pulse width
        CLK_Buffered = 0x5000000A,
        DATA_Buffered = 0x5000000B,
        SET_LR = 0x5000000C,
        OWCI_PIN = 0x5000000D,
        ReadFW_Version = 0x5000000E,           //Read firm ware version
        TestInterface = 0x5000000F,           //This interface is just for test.

        OWCI_WRITE_SINGLE_AUX = 0x30000001,
        //OWCI_WRITE_BURST_AUX      = 0x30000002,
        OWCI_READ_SINGLE_AUX = 0x30000003,
        //OWCI_WRITE_BURST_AUX      = 0x30000004,
        OWCI_SET_PILOT_AUX = 0x30000005,

        //OWI ADC specially
        GetFirmwareVersion = 0xCA000002,           //Get firm ware version
        ResetBoard = 0xCA000005,           //Reset Board
        //ADC_VOUT_WITH_CAP   = 0x60000001,
        //ADC_VOUT_WITHOUT_CAP = 0x60000002,
        //ADC_VREF_WITH_CAP   = 0x60000003,
        //ADC_VREF_WITHOUT_CAP = 0x60000004,
        //ADC_VIN_TO_VOUT     = 0x60000005,
        //ADC_VIN_TO_VREF     = 0x60000006,
        //ADC_CONFIG_TO_VOUT  = 0x60000007,
        //ADC_CONFIG_TO_VREF  = 0x60000008,
        //ADC_VDD_FROM_EXT    = 0x60000009,
        AdcSampleTransfer = 0x6000000A,           //AdcSampleTransfer
        ADCReset = 0x6000000C,           //Reset ADC
        //ADCSigPathSet       = 0x6000000B,           //ADCSigPathSet
        //ADCSigPathInit      = 0x6000000D,           //ADCSigPathInit
        //ADC_VDD_FROM_5V     = 0x60000010,

        //UART control
        ADI_SDP_CMD_GROUP_UART = 0x70000000,
        ADI_SDP_CMD_UART_INIT = 0x70000001,
        ADI_SDP_CMD_UART_WRITE = 0x70000002,
        ADI_SDP_CMD_UART_READ = 0x70000003,

        //SIGNAL PATH SETTING GROUP
        ADI_SDP_CMD_SIGNALPATH_SET = 0x8000000A,           //ADCSigPathSet
        ADI_SDP_CMD_SIGNALPATH_INIT = 0x8000000B,          //ADCSigPathInit
        ADI_SDP_CMD_SIGNALPATH_GROUP = 0x8000000C,
        ADI_SDP_CMD_SIGNALPATH_SOCKET = 0x8000000D,
        ADI_SDP_CMD_SIGNALPATH_READ_SOT = 0x8000000E
    }
    #endregion One Wire Interface

    #endregion
}
