﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFWCoreLib.CoreFrame.Common;
using EFWCoreLib.CoreFrame.Init;
using EFWCoreLib.WebFrame.WebAPI;

namespace EFWCoreLib.WebApiFrame
{
    /// <summary>
    /// WebApi服务启动程序
    /// </summary>
    public class WebApiGlobal
    {
        public static bool IsDebug = false;
        public static bool IsToken = false;
        static WebApiSelfHosting webapiHost = null;
        public static string PluginPath;
        public static void Main()
        {
            PluginPath = AppDomain.CurrentDomain.BaseDirectory + @"\ModulePlugin";
            IsDebug = HostSettingConfig.GetValue("debug") == "1" ? true : false;
            IsToken = HostSettingConfig.GetValue("token") == "1" ? true : false;
            webapiHost = new WebApiSelfHosting(System.Configuration.ConfigurationSettings.AppSettings["WebApiUri"]);
            webapiHost.StartHost();

            MiddlewareLogHelper.WriterLog(LogType.MidLog, true, Color.Blue, "WebAPI服务启动完成");

            //if (Convert.ToInt32(HostSettingConfig.GetValue("nginx")) == 1)
            //{
            //    NginxManager.StartWeb();//开启Nginx
            //    MiddlewareLogHelper.WriterLog(LogType.MidLog, true, Color.Blue, "Nginx启动完成");
            //}
        }

        public static void Exit()
        {
            webapiHost.StopHost();
            MiddlewareLogHelper.WriterLog(LogType.MidLog, true, Color.Red, "WebAPI服务已关闭！");

            //NginxManager.StopWeb();
            //MiddlewareLogHelper.WriterLog(LogType.MidLog, true, Color.Red, "Nginx已停止");

        }
    }
}
