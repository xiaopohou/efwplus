using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using EFWCoreLib.CoreFrame.Common;
using efwplus.process;

namespace efwplusServer
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        static void Main(string[] args)
        {
            try
            {
                ipcHandler();

                efwplusBaseManager.StopBase();
                efwplusRouteManager.StopRoute();
                efwplusWebAPIManager.StopAPI();
                MongodbManager.StopDB();
                NginxManager.StopWeb();

                efwplusBaseManager.StartBase();
                efwplusRouteManager.StartRoute();
                efwplusWebAPIManager.StartAPI();
                MongodbManager.StartDB();
                NginxManager.StartWeb();


                //ipcw.WriteData("kakake", IPCType.efwplusBase);

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


        //static IPCWriteHelper ipcw;
        /// <summary>
        /// 进程通信
        /// </summary>
        static void ipcHandler()
        {
            IPCReceiveHelper ipcr = new IPCReceiveHelper();
            Action<string> action = ((string data) =>
            {
                if (string.IsNullOrEmpty(data) == false)
                {
                    if (data.IndexOf("efwplusservercmd:") == -1)
                    {
                        Console.WriteLine(data);
                        return;
                    }
                    //执行命令
                    data = data.Replace("efwplusservercmd:", "");
                    switch (data.ToLower())
                    {
                        case "exit":
                            efwplusBaseManager.StopBase();
                            efwplusRouteManager.StopRoute();
                            efwplusWebAPIManager.StopAPI();
                            MongodbManager.StopDB();
                            NginxManager.StopWeb();
                            Environment.Exit(0);
                            break;
                        case "restartbase":
                            efwplusBaseManager.StopBase();
                            efwplusBaseManager.StartBase();
                            break;
                        case "restartroute":
                            efwplusRouteManager.StopRoute();
                            efwplusRouteManager.StartRoute();
                            break;
                        case "restartwebapi":
                            efwplusWebAPIManager.StopAPI();
                            efwplusWebAPIManager.StartAPI();
                            break;
                        case "restartmongodb":
                            efwplusWebAPIManager.StopAPI();
                            efwplusWebAPIManager.StartAPI();
                            break;
                        case "restartnginx":
                            efwplusWebAPIManager.StopAPI();
                            efwplusWebAPIManager.StartAPI();
                            break;
                    }
                }
            });
            ipcr.Init(action, IPCType.efwplusServerCmd);

            //ipcw = new IPCWriteHelper();
        }

    }
 
}
