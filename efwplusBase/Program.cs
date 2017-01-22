using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EFWCoreLib.CoreFrame.Common;
using EFWCoreLib.WcfFrame;

namespace efwplusBase
{
    static class Program
    {
        /// <summary>
        /// 启动状态
        /// </summary>
        static HostState RunState { get; set; }
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        static void Main(string[] args)
        {
            try
            {
                setprivatepath();
                ipcHandler();

                RunState = HostState.NoOpen;
                btnStart();
                while (true)
                {
                    System.Threading.Thread.Sleep(30 * 1000);
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }
        }

        static void btnStart()
        {
            string identify;
            string expireDate;
            MiddlewareLogHelper.hostwcfMsg = new MiddlewareMsgHandler(ShowMsg);
            MiddlewareLogHelper.StartWriteFileLog();//开放中间件日志
            int res = efwplus.configuration.TimeCDKEY.InitRegedit(out expireDate, out identify);
            if (res == 0)
            {
                MiddlewareLogHelper.WriterLog("软件已注册，到期时间【" + expireDate + "】");
                WcfGlobal.Identify = identify;
                //EFWCoreLib.WcfFrame.ServerManage.ClientManage.clientinfoList = new ClientInfoListHandler(BindGridClient);
                //EFWCoreLib.WcfFrame.ServerManage.RouterManage.hostwcfRouter = new HostWCFRouterListHandler(BindGridRouter);
                WcfGlobal.MainBase();

                RunState = HostState.Opened;
            }
            else if (res == 1)
            {
                MiddlewareLogHelper.WriterLog("软件尚未注册，请注册软件");
            }
            else if (res == 2)
            {
                MiddlewareLogHelper.WriterLog("注册机器与本机不一致,请联系管理员");
            }
            else if (res == 3)
            {
                MiddlewareLogHelper.WriterLog("软件试用已到期");
            }
            else
            {
                MiddlewareLogHelper.WriterLog("软件运行出错，请重新启动");
            }
        }

        static void btnStop()
        {
            WcfGlobal.ExitBase();

            RunState = HostState.NoOpen;
        }

        static void ShowMsg(Color clr, DateTime time, string msg)
        {
            string text = ("[" + time.ToString("yyyy-MM-dd HH:mm:ss") + "] : " + msg);
            Console.WriteLine(text);
            ipcw.WriteData(text, IPCType.efwplusServerCmd);
            ipcw.WriteData(text, IPCType.efwplusServer);
        }

        /// <summary>
        /// 获取或设置应用程序基目录下的目录列表
        /// </summary>
        static void setprivatepath()
        {
            //AppDomain.CurrentDomain.SetupInformation.PrivateBinPath = @"Component;ModulePlugin\Books_Wcf\dll;ModulePlugin\WcfMainUIFrame\dll";
            string privatepath = @"Component";

            foreach (var p in efwplus.configuration.PluginSysManage.GetAllPlugin())
            {
                privatepath += ";" + p.path.Replace("plugin.xml", "dll");
            }

            AppDomain.CurrentDomain.SetData("PRIVATE_BINPATH", privatepath);
            AppDomain.CurrentDomain.SetData("BINPATH_PROBE_ONLY", privatepath);
            var m = typeof(AppDomainSetup).GetMethod("UpdateContextProperty", BindingFlags.NonPublic | BindingFlags.Static);
            var funsion = typeof(AppDomain).GetMethod("GetFusionContext", BindingFlags.NonPublic | BindingFlags.Instance);
            m.Invoke(null, new object[] { funsion.Invoke(AppDomain.CurrentDomain, null), "PRIVATE_BINPATH", privatepath });

        }

        static IPCWriteHelper ipcw;
        /// <summary>
        /// 进程通信
        /// </summary>
        static void ipcHandler()
        {
            IPCReceiveHelper ipcr = new IPCReceiveHelper();
            Action<string> action = ((string data) =>
            {
                //Console.WriteLine(data);
                ExecuteCmd(data);
            });
            ipcr.Init(action,IPCType.efwplusBase);

            ipcw = new IPCWriteHelper();
        }

        static void ExecuteCmd(string cmd)
        {
            try
            {
                if (cmd.IndexOf("efwplusbase:") == -1)
                {
                    return;
                }
                cmd = cmd.Replace("efwplusbase:", ""); 
                switch (cmd.ToLower())
                {
                    case "stop":
                        WcfGlobal.ExitBase();
                        break;
                    case "start":
                        WcfGlobal.MainBase();
                        break;
                    case "close":
                        Environment.Exit(0);
                        break;
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }

    public enum HostState
    {
        NoOpen, Opened
    }
}
