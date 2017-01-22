using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EFWCoreLib.CoreFrame.Common;
using EFWCoreLib.WebApiFrame;

namespace efwplusWebAPI
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                setprivatepath();

                MiddlewareLogHelper.hostwcfMsg = new MiddlewareMsgHandler(ShowMsg);
                MiddlewareLogHelper.StartWriteFileLog();//开放中间件日志

                WebApiGlobal.Main();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }


            while (true)
            {
                System.Threading.Thread.Sleep(30 * 1000);
            }
        }

        static void Exit()
        {
            WebApiGlobal.Exit();
        }

        static void ShowMsg(Color clr, DateTime time, string msg)
        {
            Console.WriteLine("[" + time.ToString("yyyy-MM-dd HH:mm:ss") + "] : " + msg);
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
    }
}
