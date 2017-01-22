using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace EFWCoreLib.CoreFrame.Common
{
    public enum IPCType
    {
        efwplusBase,efwplusRoute,efwplusWebAPI,efwplusServer,efwplusServerCmd
    }
    public class IPCName
    {
        public static uint mapLength=10240;            //共享内存长
        public static string GetWriteMapName(IPCType type)
        {
            switch (type)
            {
                case IPCType.efwplusBase:
                    return "WriteMap_efwplusBase";
                case IPCType.efwplusRoute:
                    return "WriteMap_efwplusRoute";
                case IPCType.efwplusWebAPI:
                    return "WriteMap_efwplusWebAPI";
                case IPCType.efwplusServer:
                    return "WriteMap_efwplusServer";
                case IPCType.efwplusServerCmd:
                    return "WriteMap_efwplusServerCmd";
            }
            return "WriteMap";
        }

        public static string GetReadMapName(IPCType type)
        {
            switch (type)
            {
                case IPCType.efwplusBase:
                    return "ReadMap_efwplusBase";
                case IPCType.efwplusRoute:
                    return "ReadMap_efwplusRoute";
                case IPCType.efwplusWebAPI:
                    return "ReadMap_efwplusWebAPI";
                case IPCType.efwplusServer:
                    return "ReadMap_efwplusServer";
                case IPCType.efwplusServerCmd:
                    return "ReadMap_efwplusServerCmd";
            }
            return "ReadMap";
        }

        public static string GetshareMemoryName(IPCType type)
        {
            switch (type)
            {
                case IPCType.efwplusBase:
                    return "shareMemory_efwplusBase";
                case IPCType.efwplusRoute:
                    return "shareMemory_efwplusRoute";
                case IPCType.efwplusWebAPI:
                    return "shareMemory_efwplusWebAPI";
                case IPCType.efwplusServer:
                    return "shareMemory_efwplusServer";
                case IPCType.efwplusServerCmd:
                    return "shareMemory_efwplusServerCmd";
            }
            return "shareMemory";
        }
    }
    /// <summary>
    /// 进程间通信，读取数据
    /// </summary>
    public class IPCReceiveHelper
    {
        const int INVALID_HANDLE_VALUE = -1;
        const int PAGE_READWRITE = 0x04;

        [DllImport("User32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int cmdShow);
        [DllImport("User32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        //共享内存
        [DllImport("Kernel32.dll", EntryPoint = "CreateFileMapping")]
        private static extern IntPtr CreateFileMapping(IntPtr hFile, //HANDLE hFile,
         UInt32 lpAttributes,//LPSECURITY_ATTRIBUTES lpAttributes,  //0
         UInt32 flProtect,//DWORD flProtect
         UInt32 dwMaximumSizeHigh,//DWORD dwMaximumSizeHigh,
         UInt32 dwMaximumSizeLow,//DWORD dwMaximumSizeLow,
         string lpName//LPCTSTR lpName
         );

        [DllImport("Kernel32.dll", EntryPoint = "OpenFileMapping")]
        private static extern IntPtr OpenFileMapping(
         UInt32 dwDesiredAccess,//DWORD dwDesiredAccess,
         int bInheritHandle,//BOOL bInheritHandle,
         string lpName//LPCTSTR lpName
         );

        const int FILE_MAP_ALL_ACCESS = 0x0002;
        const int FILE_MAP_WRITE = 0x0002;

        [DllImport("Kernel32.dll", EntryPoint = "MapViewOfFile")]
        private static extern IntPtr MapViewOfFile(
         IntPtr hFileMappingObject,//HANDLE hFileMappingObject,
         UInt32 dwDesiredAccess,//DWORD dwDesiredAccess
         UInt32 dwFileOffsetHight,//DWORD dwFileOffsetHigh,
         UInt32 dwFileOffsetLow,//DWORD dwFileOffsetLow,
         UInt32 dwNumberOfBytesToMap//SIZE_T dwNumberOfBytesToMap
         );

        [DllImport("Kernel32.dll", EntryPoint = "UnmapViewOfFile")]
        private static extern int UnmapViewOfFile(IntPtr lpBaseAddress);

        [DllImport("Kernel32.dll", EntryPoint = "CloseHandle")]
        private static extern int CloseHandle(IntPtr hObject);

        private Semaphore m_Write;  //可写的信号
        private Semaphore m_Read;  //可读的信号
        private IntPtr handle;     //文件句柄
        private IntPtr addr;       //共享内存地址
        //uint mapLength;            //共享内存长

        //线程用来读取数据
        Thread threadRed;
        Action<string> dataAction;//数据委托
        ///<summary>
        /// 初始化共享内存数据 创建一个共享内存
        ///</summary>
        public void Init(Action<string> _dataAction, IPCType type)
        {
            dataAction = _dataAction;

            m_Write = new Semaphore(1, 1, IPCName.GetWriteMapName(type));//开始的时候有一个可以写
            m_Read = new Semaphore(0, 1, IPCName.GetReadMapName(type));//没有数据可读
            //mapLength = 10240000;
            IntPtr hFile = new IntPtr(INVALID_HANDLE_VALUE);
            handle = CreateFileMapping(hFile, 0, PAGE_READWRITE, 0, IPCName.mapLength, IPCName.GetshareMemoryName(type));
            addr = MapViewOfFile(handle, FILE_MAP_ALL_ACCESS, 0, 0, 0);

            //handle = OpenFileMapping(0x0002, 0, "shareMemory");
            //addr = MapViewOfFile(handle, FILE_MAP_ALL_ACCESS, 0, 0, 0);

            threadRed = new Thread(new ThreadStart(ReceiveData));
            threadRed.Start();
        }

        /// <summary>
        /// 线程启动从共享内存中获取数据信息 
        /// </summary>
        private void ReceiveData()
        {
            //myDelegate myI = new myDelegate(changeTxt);
            while (true)
            {
                try
                {
                    //m_Write = Semaphore.OpenExisting("WriteMap");
                    //m_Read = Semaphore.OpenExisting("ReadMap");
                    //handle = OpenFileMapping(FILE_MAP_WRITE, 0, "shareMemory");

                    //读取共享内存中的数据：
                    //是否有数据写过来
                    m_Read.WaitOne();
                    //IntPtr m_Sender = MapViewOfFile(handle, FILE_MAP_ALL_ACCESS, 0, 0, 0);
                    byte[] byteStr = new byte[IPCName.mapLength];
                    byteCopy(byteStr, addr);
                    string str = Encoding.Default.GetString(byteStr, 0, byteStr.Length);
                    //调用数据处理方法 处理读取到的数据
                    m_Write.Release();

                    if(dataAction!=null)
                    {
                        dataAction(str.Substring(0, str.IndexOf('\0')));
                    }
                    Thread.Sleep(100);
                }
                catch (WaitHandleCannotBeOpenedException)
                {
                    continue;
                    //Thread.Sleep(0);
                }
            }

        }
        //不安全的代码在项目生成的选项中选中允许不安全代码
        static unsafe void byteCopy(byte[] dst, IntPtr src)
        {
            fixed (byte* pDst = dst)
            {
                byte* pdst = pDst;
                byte* psrc = (byte*)src;
                while ((*pdst++ = *psrc++) != '\0')
                    ;
            }

        }
    }

    /// <summary>
    /// 进程间通信，写入数据
    /// </summary>
    public class IPCWriteHelper
    {
        const int INVALID_HANDLE_VALUE = -1;
        const int PAGE_READWRITE = 0x04;
        //共享内存
        [DllImport("Kernel32.dll", EntryPoint = "CreateFileMapping")]
        private static extern IntPtr CreateFileMapping(IntPtr hFile, //HANDLE hFile,
         UInt32 lpAttributes,//LPSECURITY_ATTRIBUTES lpAttributes,  //0
         UInt32 flProtect,//DWORD flProtect
         UInt32 dwMaximumSizeHigh,//DWORD dwMaximumSizeHigh,
         UInt32 dwMaximumSizeLow,//DWORD dwMaximumSizeLow,
         string lpName//LPCTSTR lpName
         );

        [DllImport("Kernel32.dll", EntryPoint = "OpenFileMapping")]
        private static extern IntPtr OpenFileMapping(
         UInt32 dwDesiredAccess,//DWORD dwDesiredAccess,
         int bInheritHandle,//BOOL bInheritHandle,
         string lpName//LPCTSTR lpName
         );

        const int FILE_MAP_ALL_ACCESS = 0x0002;
        const int FILE_MAP_WRITE = 0x0002;

        [DllImport("Kernel32.dll", EntryPoint = "MapViewOfFile")]
        private static extern IntPtr MapViewOfFile(
         IntPtr hFileMappingObject,//HANDLE hFileMappingObject,
         UInt32 dwDesiredAccess,//DWORD dwDesiredAccess
         UInt32 dwFileOffsetHight,//DWORD dwFileOffsetHigh,
         UInt32 dwFileOffsetLow,//DWORD dwFileOffsetLow,
         UInt32 dwNumberOfBytesToMap//SIZE_T dwNumberOfBytesToMap
         );

        [DllImport("Kernel32.dll", EntryPoint = "UnmapViewOfFile")]
        private static extern int UnmapViewOfFile(IntPtr lpBaseAddress);

        [DllImport("Kernel32.dll", EntryPoint = "CloseHandle")]
        private static extern int CloseHandle(IntPtr hObject);



        private Semaphore m_Write;  //可写的信号
        private Semaphore m_Read;  //可读的信号
        private IntPtr handle;     //文件句柄
        private IntPtr addr;       //共享内存地址
        //uint mapLength=10240000;            //共享内存长

        //Thread threadRed;

        public void WriteData(string data, IPCType type)
        {
            try
            {
                m_Write = Semaphore.OpenExisting(IPCName.GetWriteMapName(type));
                m_Read = Semaphore.OpenExisting(IPCName.GetReadMapName(type));
                handle = OpenFileMapping(FILE_MAP_WRITE, 0, IPCName.GetshareMemoryName(type));
                addr = MapViewOfFile(handle, FILE_MAP_ALL_ACCESS, 0, 0, 0);

                m_Write.WaitOne();
                byte[] sendStr = Encoding.Default.GetBytes(data + '\0');
                //如果要是超长的话，应另外处理，最好是分配足够的内存
                if (sendStr.Length < IPCName.mapLength)
                    Copy(sendStr, addr);

                m_Read.Release();
            }
            catch
            {
                //throw new Exception("不存在系统信号量!");
            }
        }

        static unsafe void Copy(byte[] byteSrc, IntPtr dst)
        {
            fixed (byte* pSrc = byteSrc)
            {
                byte* pDst = (byte*)dst;
                byte* psrc = pSrc;
                for (int i = 0; i < byteSrc.Length; i++)
                {
                    *pDst = *psrc;
                    pDst++;
                    psrc++;
                }
            }
        }
    }
}
