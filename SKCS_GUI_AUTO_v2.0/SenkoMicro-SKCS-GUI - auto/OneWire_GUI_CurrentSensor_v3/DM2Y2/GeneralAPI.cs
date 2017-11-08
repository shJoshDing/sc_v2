using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;

namespace ADI.DMY2
{
    internal class GeneralAPI
    {
        #region user32.dll & setupapi.dll
        public const int DIGCF_ALLCLASSES = (0x00000004);
        public const int DIGCF_PRESENT = (0x00000002);
        public const int INVALID_HANDLE_VALUE = -1;
        public const int SPDRP_DEVICEDESC = (0x00000000);
        public const int MAX_DEV_LEN = 1000;
        public const int DEVICE_NOTIFY_WINDOW_HANDLE = (0x00000000);
        public const int DEVICE_NOTIFY_SERVICE_HANDLE = (0x00000001);
        public const int DEVICE_NOTIFY_ALL_INTERFACE_CLASSES = (0x00000004);
        public const int DBT_DEVTYP_DEVICEINTERFACE = (0x00000005);
        public const int DBT_DEVNODES_CHANGED = (0x0007);
        public const int WM_DEVICECHANGE = (0x0219);
        public const int DIF_PROPERTYCHANGE = (0x00000012);
        public const int DICS_FLAG_GLOBAL = (0x00000001);
        public const int DICS_FLAG_CONFIGSPECIFIC = (0x00000002);
        public const int DICS_ENABLE = (0x00000001);
        public const int DICS_DISABLE = (0x00000002);
        public const int DIGCF_DEVICEINTERFACE = 0x00000010;

        /// <summary>
        /// 注册设备或者设备类型，在指定的窗口返回相关的信息
        /// </summary>
        /// <param name="hRecipient"></param>
        /// <param name="NotificationFilter"></param>
        /// <param name="Flags"></param>
        /// <returns></returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr RegisterDeviceNotification(IntPtr hRecipient, DEV_BROADCAST_DEVICEINTERFACE NotificationFilter, UInt32 Flags);

        /// <summary>
        /// 通过名柄，关闭指定设备的信息。
        /// </summary>
        /// <param name="hHandle"></param>
        /// <returns></returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern uint UnregisterDeviceNotification(IntPtr hHandle);

        /// <summary>
        /// 获取一个指定类别或全部类别的所有已安装设备的信息
        /// </summary>
        /// <param name="gClass"></param>
        /// <param name="iEnumerator"></param>
        /// <param name="hParent"></param>
        /// <param name="nFlags"></param>
        /// <returns></returns>
        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetupDiGetClassDevs(ref Guid gClass, UInt32 iEnumerator, IntPtr hParent, UInt32 nFlags);

        /// <summary>
        /// 销毁一个设备信息集合，并且释放所有关联的内存
        /// </summary>
        /// <param name="lpInfoSet"></param>
        /// <returns></returns>
        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern int SetupDiDestroyDeviceInfoList(IntPtr lpInfoSet);

        /// <summary>
        /// 枚举指定设备信息集合的成员，并将数据放在SP_DEVINFO_DATA中
        /// </summary>
        /// <param name="lpInfoSet"></param>
        /// <param name="dwIndex"></param>
        /// <param name="devInfoData"></param>
        /// <returns></returns>
        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern bool SetupDiEnumDeviceInfo(IntPtr lpInfoSet,
            UInt32 dwIndex,
            ref SP_DEVINFO_DATA devInfoData);

        /// <summary>
        /// 枚举指定设备接口
        /// </summary>
        /// <param name="lpInfoSet"></param>
        /// <param name="devInfoData"></param>
        /// <param name="interfaceCalssGuid"></param>
        /// <param name="memberIndex"></param>
        /// <param name="devInterfaceData"></param>
        /// <returns></returns>
        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool SetupDiEnumDeviceInterfaces(
            IntPtr devlpInfoSet,
            //int devInfoData, 
            IntPtr devInfoData,
            ref Guid interfaceCalssGuid,
            UInt32 memberIndex,
            ref SP_DEVICE_INTERFACE_DATA devInterfaceData);

        /// <summary>
        /// 枚举指定设备接口的详细信息
        /// </summary>
        /// <param name="lpInfoSet"></param>
        /// <param name="devInfoData"></param>
        /// <param name="interfaceCalssGuid"></param>
        /// <param name="memberIndex"></param>
        /// <param name="devInterfaceData"></param>
        /// <returns></returns>
        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern Boolean SetupDiGetDeviceInterfaceDetail(
            IntPtr DeviceInfoSet,
            ref SP_DEVICE_INTERFACE_DATA DeviceIntefaceData,
            //SP_INTERFACE_DEVICE_DETAIL_DATA DeviceInterfaceDetailData, 
            IntPtr DeviceInterfaceDetailData,
            int DeviceInterfacedetailDatasize,
            ref int DeviceInterfacedetaildataSize,
            SP_DEVINFO_DATA deviceInfoData
            //ref SP_DEVINFO_DATA DeviceInfoData
            //IntPtr DeviceInfoData
            //SP_DEVINFO_DATA deviceInfoData
            );

        /// <summary>
        /// 获取指定设备的属性
        /// </summary>
        /// <param name="lpInfoSet"></param>
        /// <param name="DeviceInfoData"></param>
        /// <param name="Property"></param>
        /// <param name="PropertyRegDataType"></param>
        /// <param name="PropertyBuffer"></param>
        /// <param name="PropertyBufferSize"></param>
        /// <param name="RequiredSize"></param>
        /// <returns></returns>
        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern bool SetupDiGetDeviceRegistryProperty(
            IntPtr lpInfoSet,
            SP_DEVINFO_DATA DeviceInfoData,
            UInt32 Property,
            UInt32 PropertyRegDataType,
            StringBuilder PropertyBuffer,
            UInt32 PropertyBufferSize,
            IntPtr RequiredSize);

        /// <summary>
        /// 停用设备
        /// </summary>
        /// <param name="DeviceInfoSet"></param>
        /// <param name="DeviceInfoData"></param>
        /// <param name="ClassInstallParams"></param>
        /// <param name="ClassInstallParamsSize"></param>
        /// <returns></returns>
        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool SetupDiSetClassInstallParams(IntPtr DeviceInfoSet, IntPtr DeviceInfoData, IntPtr ClassInstallParams, int ClassInstallParamsSize);

        /// <summary>
        /// 启用设备
        /// </summary>
        /// <param name="InstallFunction"></param>
        /// <param name="DeviceInfoSet"></param>
        /// <param name="DeviceInfoData"></param>
        /// <returns></returns>
        [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
        public static extern Boolean SetupDiCallClassInstaller(UInt32 InstallFunction, IntPtr DeviceInfoSet, IntPtr DeviceInfoData);

        /// <summary>
        /// RegisterDeviceNotification所需参数
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct DEV_BROADCAST_HANDLE
        {
            public int dbch_size;
            public int dbch_devicetype;
            public int dbch_reserved;
            public IntPtr dbch_handle;
            public IntPtr dbch_hdevnotify;
            public Guid dbch_eventguid;
            public long dbch_nameoffset;
            public byte dbch_data;
            public byte dbch_data1;
        }

        // WM_DEVICECHANGE message
        [StructLayout(LayoutKind.Sequential)]
        public struct DEV_BROADCAST_DEVICEINTERFACE
        {
            public int dbcc_size;
            public int dbcc_devicetype;
            public int dbcc_reserved;
        }

        /// <summary>
        /// 设备信息数据
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        //public struct SP_DEVINFO_DATA
        //{
        //    public int cbSize;
        //    public Guid classGuid;
        //    public int devInst;
        //    public UInt32 reserved;
        //};
        public class SP_DEVINFO_DATA
        {
            public int cbSize = Marshal.SizeOf(typeof(SP_DEVINFO_DATA));
            public Guid classGuid = Guid.Empty;
            public int devInst = 0;
            public int reserved = 0;
        };

        /// <summary>
        /// 设备接口
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct SP_DEVICE_INTERFACE_DATA
        {
            public int cbSize;
            public Guid interfaceclassGuid;
            public int flags;
            public int reserved;
        };

        /// <summary>
        /// 设备接口
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        public struct SP_INTERFACE_DEVICE_DETAIL_DATA
        {
            public int cbSize;
            //[MarshalAs(UnmanagedType.LPTStr)]
            public short devicePath;
        }

        /// <summary>
        /// 属性变更参数
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public class SP_PROPCHANGE_PARAMS
        {
            public SP_CLASSINSTALL_HEADER ClassInstallHeader = new SP_CLASSINSTALL_HEADER();
            public int StateChange;
            public int Scope;
            public int HwProfile;
        };

        /// <summary>
        /// 设备安装
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct SP_DEVINSTALL_PARAMS
        {
            public int cbSize;
            public int Flags;
            public int FlagsEx;
            public IntPtr hwndParent;
            public IntPtr InstallMsgHandler;
            public IntPtr InstallMsgHandlerContext;
            public IntPtr FileQueue;
            public IntPtr ClassInstallReserved;
            public int Reserved;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string DriverPath;
        };

        [StructLayout(LayoutKind.Sequential)]
        public class SP_CLASSINSTALL_HEADER
        {
            public int cbSize;
            public int InstallFunction;
        };
        #endregion

        #region hid.dll
        //Step1: Obtaining the Device Interface Guid
        [DllImport("hid.dll")]
        public static extern void HidD_GetHidGuid(ref Guid GUID);

        [StructLayout(LayoutKind.Sequential)]
        public struct HIDD_ATTRIBUTES
        {
            public Int32 Size;
            public Int16 VendorID;
            public Int16 ProductID;
            public Int16 VersionNumber;

        }

        [DllImport("hid.dll", SetLastError = true)]
        public static extern Boolean HidD_GetAttributes(IntPtr HidDeviceObject, ref HIDD_ATTRIBUTES Attributes);
        #endregion

        #region kernel 32.dll
        //获取设备文件
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateFile(
            string lpFileName,                            // file name
            uint dwDesiredAccess,                         // access mode
            uint dwShareMode,                             // share mode
            uint lpSecurityAttributes,                    // SD
            uint dwCreationDisposition,                   // how to create
            uint dwFlagsAndAttributes,                    // file attributes
            uint hTemplateFile                            // handle to template file
            );
        //读取设备文件
        [DllImport("Kernel32.dll", SetLastError = true)]
        public static extern bool ReadFile(
            IntPtr hFile,
            byte[] lpBuffer,
            //IntPtr lpBuffer,
            uint nNumberOfBytesToRead,
            ref uint lpNumberOfBytesRead,
            IntPtr lpOverlapped
            );

        [DllImport("Kernel32.dll", SetLastError = true)]
        public static extern bool ReadFile(
            IntPtr hFile,
            int[] lpBuffer,
            //IntPtr lpBuffer,
            uint nNumberOfBytesToRead,
            ref uint lpNumberOfBytesRead,
            IntPtr lpOverlapped
            );

        [DllImport("Kernel32.dll", SetLastError = true)]
        public static extern bool ReadFile(
            IntPtr hFile,
            UInt32[] lpBuffer,
            //IntPtr lpBuffer,
            uint nNumberOfBytesToRead,
            ref uint lpNumberOfBytesRead,
            IntPtr lpOverlapped
            );

        [DllImport("Kernel32.dll", SetLastError = true)]
        public static extern bool ReadFile(
            IntPtr hFile,
            UInt16[] lpBuffer,
            //IntPtr lpBuffer,
            uint nNumberOfBytesToRead,
            ref uint lpNumberOfBytesRead,
            IntPtr lpOverlapped
            );

        //写设备文件
        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        //[DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool WriteFile(
            IntPtr hFile,
            //byte[] lpBuffer,
            int[] lpBuffer,
            //IntPtr lpBuffer,
            //ref _Hid_Out_Rpt lpBuffer,
            uint nNumberOfBytesToRead,
            ref uint lpNumberOfBytesRead,
            IntPtr lpOverlapped
            //[In] ref NativeOverlapped lpOverlapped
            //ref OVERLAPPED lpOverlapped
            );

        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        //[DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool WriteFile(
            IntPtr hFile,
            //byte[] lpBuffer,
            uint[] lpBuffer,
            //IntPtr lpBuffer,
            //ref _Hid_Out_Rpt lpBuffer,
            uint nNumberOfBytesToRead,
            ref uint lpNumberOfBytesRead,
            IntPtr lpOverlapped
            //[In] ref NativeOverlapped lpOverlapped
            //ref OVERLAPPED lpOverlapped
            );

        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        public static extern bool WriteFile(
            IntPtr hFile,
            byte[] lpBuffer,
            //UInt32[] lpBuffer,
            //IntPtr lpBuffer,
            //ref _Hid_Out_Rpt lpBuffer,
            uint nNumberOfBytesToRead,
            ref uint lpNumberOfBytesRead,
            IntPtr lpOverlapped
            //[In] ref NativeOverlapped lpOverlapped
            //ref OVERLAPPED lpOverlapped
            );

        [StructLayout(LayoutKind.Sequential)]
        public struct OVERLAPPED
        {
            public UIntPtr Internal;
            public UIntPtr InternalHigh;
            public int Offset;
            public int OffsetHigh;
            public IntPtr EventHandle;
        }

        //Creat Event for asynchronous read/write
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CreateEvent(
            IntPtr lpEventAttributes,    // LPSECURITY_ATTRIBUTES
            //int bManualReset,          // BOOL
            //int bInitialState,         // BOOL
            bool bManualReset,           // BOOL
            bool bInitialState,          // BOOL
            string lpName);              // LPCTSTR

        //reset event
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool ResetEvent(IntPtr hEvent);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern UInt32 WaitForSingleObject(IntPtr hHandle, UInt32 dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetOverlappedResult(
          IntPtr hFile,                         // HANDLE   
          IntPtr OverlappedData,                // LPOVERLAPPED   
          out uint lpNumberOfBytesTransferred,  // LPDWORD   
          int bWait);                           // BOOL   

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CancelIo(IntPtr hFile);


        [DllImportAttribute("kernel32.dll", EntryPoint = "OpenProcess")]
        public static extern IntPtr OpenProcess(
            int dwDesiredAccess,
            bool bInheritHandle,
            int dwProcessId);
        #endregion

        #region Another

        #endregion

        #region WinError.h define
        public sealed class WinError
        {
            public const int ERROR_NO_MORE_ITEMS = 259;
        }
        #endregion

    }
}

