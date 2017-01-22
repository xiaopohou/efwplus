using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using EFWCoreLib.CoreFrame.Common;
using EFWCoreLib.CoreFrame.Init;
using EFWCoreLib.WcfFrame.DataSerialize;
using EFWCoreLib.WcfFrame.ServerManage;
using EFWCoreLib.WcfFrame.Utility;
using EFWCoreLib.WcfFrame.Utility.Mongodb;
using EFWCoreLib.WcfFrame.WcfHandler;

namespace EFWCoreLib.WcfFrame
{
    public class WcfGlobal
    {
        private static bool IsStartBase = false;//是否开启数据服务
        private static bool IsStartRoute = false;//是否开启路由服务 
        /// <summary>
        /// 调试模式
        /// </summary>
        public static bool IsDebug = false;
        public static string Identify = "";//中间件唯一标识
        public static string HostName = "";//中间件显示名称
        public static bool IsToken = false;
        public static string ns = "http://www.efwplus.cn/";
        public static string MongoConnStr = "";//mongo连接字符串

        static ServiceHost mAppHost = null;
        static ServiceHost mFileHost = null;
        static ServiceHost mRouterHost = null;
        static ServiceHost mFileRouterHost = null;

        public static void MainBase()
        {
            if (IsStartBase == true) return;
            IsStartBase = true;//设置为开启

            IsDebug = HostSettingConfig.GetValue("debug") == "1" ? true : false;
            HostName = HostSettingConfig.GetValue("hostname");
            IsToken = HostSettingConfig.GetValue("token") == "1" ? true : false;
            MongoConnStr = HostSettingConfig.GetValue("mongodb_conn");

            WcfGlobal.Run(StartType.BaseService);
            WcfGlobal.Run(StartType.FileService);
            WcfGlobal.Run(StartType.SuperClient);
            WcfGlobal.Run(StartType.MiddlewareTask);
            WcfGlobal.Run(StartType.PublishService);

            GetAllConfig();//获取所有配置
        }

        public static void ExitBase()
        {
            if (IsStartBase == false) return;
            IsStartBase = false;//设置为开启

            MiddlewareLogHelper.WriterLog(LogType.MidLog, true, Color.Red, "正在准备关闭中间件服务，请等待...");
            ClientLinkManage.UnAllConnection();//关闭所有连接

            WcfGlobal.Quit(StartType.PublishService);
            WcfGlobal.Quit(StartType.MiddlewareTask);
            WcfGlobal.Quit(StartType.SuperClient);
            WcfGlobal.Quit(StartType.BaseService);
            WcfGlobal.Quit(StartType.FileService);
            
            //WcfGlobal.Quit(StartType.MongoDB);
            //WcfGlobal.Quit(StartType.Nginx);
        }

        public static void MainRoute()
        {
            if (IsStartRoute == true) return;
            IsStartRoute = true;//设置为开启

            WcfGlobal.Run(StartType.RouterBaseService);
            WcfGlobal.Run(StartType.RouterFileService);
        }

        public static void ExitRoute()
        {
            if (IsStartRoute == false) return;
            IsStartRoute = false;//设置为开启

            WcfGlobal.Quit(StartType.RouterBaseService);
            WcfGlobal.Quit(StartType.RouterFileService);
        }

        public static void Run(StartType type)
        {
            
            switch (type)
            {
                case StartType.BaseService:
                    mAppHost = new ServiceHost(typeof(BaseService));
                    //初始化连接池,默认10分钟清理连接
                    ClientLinkPoolCache.Init(true, 200, 30, 600, "wcfserver", 30);

                    AppGlobal.AppRootPath = System.Windows.Forms.Application.StartupPath + "\\";
                    AppGlobal.appType = AppType.WCF;
                    AppGlobal.IsSaas = System.Configuration.ConfigurationManager.AppSettings["IsSaas"] == "true" ? true : false;
                    AppGlobal.AppStart();


                    ClientManage.IsHeartbeat = HostSettingConfig.GetValue("heartbeat") == "1" ? true : false;
                    ClientManage.HeartbeatTime = Convert.ToInt32(HostSettingConfig.GetValue("heartbeattime"));
                    ClientManage.IsMessage = HostSettingConfig.GetValue("message") == "1" ? true : false;
                    ClientManage.MessageTime = Convert.ToInt32(HostSettingConfig.GetValue("messagetime"));
                    ClientManage.IsCompressJson = HostSettingConfig.GetValue("compress") == "1" ? true : false;
                    ClientManage.IsEncryptionJson = HostSettingConfig.GetValue("encryption") == "1" ? true : false;
                    ClientManage.IsToken = HostSettingConfig.GetValue("token") == "1" ? true : false;
                    ClientManage.serializeType = (SerializeType)Convert.ToInt32(HostSettingConfig.GetValue("serializetype"));
                    ClientManage.IsOverTime = HostSettingConfig.GetValue("overtime") == "1" ? true : false;
                    ClientManage.OverTime = Convert.ToInt32(HostSettingConfig.GetValue("overtimetime"));

                    ClientManage.StartHost();
                    mAppHost.Open();

                    MiddlewareLogHelper.WriterLog(LogType.MidLog, true, Color.Blue, "数据服务启动完成");
                    break;

                case StartType.FileService:
                    AppGlobal.AppRootPath = System.Windows.Forms.Application.StartupPath + "\\";

                    mFileHost = new ServiceHost(typeof(FileService));
                    mFileHost.Open();

                    MiddlewareLogHelper.WriterLog(LogType.MidLog, true, Color.Blue, "文件服务启动完成");
                    break;
                case StartType.RouterBaseService:
                    mRouterHost = new ServiceHost(typeof(RouterBaseService));
                    RouterManage.Start();
                    mRouterHost.Open();

                    MiddlewareLogHelper.WriterLog(LogType.MidLog, true, Color.Blue, "数据路由服务启动完成");
                    break;
                case StartType.RouterFileService:
                    mFileRouterHost = new ServiceHost(typeof(RouterFileService));
                    mFileRouterHost.Open();

                    MiddlewareLogHelper.WriterLog(LogType.MidLog, true, Color.Blue, "文件路由服务启动完成");
                    break;
                case StartType.SuperClient:
                    SuperClient.CreateSuperClient();
                    MiddlewareLogHelper.WriterLog(LogType.MidLog, true, Color.Blue, "超级客户端启动完成");
                    break;
                case StartType.MiddlewareTask:
                    MiddlewareTask.StartTask();//开启定时任务
                    MiddlewareLogHelper.WriterLog(LogType.MidLog, true, Color.Blue, "定时任务启动完成");
                    break;
                case StartType.PublishService://订阅
                    PublishServiceManage.InitPublishService();
                    PublishSubManager.StartPublish();
                    MiddlewareLogHelper.WriterLog(LogType.MidLog, true, Color.Blue, "发布订阅服务完成");
                    break;
                case StartType.MongoDB:
                    //MongodbManager.StartDB();//开启MongoDB
                    //MiddlewareLogHelper.WriterLog(LogType.MidLog, true, Color.Blue, "MongoDB启动完成");
                    break;

                case StartType.Nginx:
                    //NginxManager.StartWeb();//开启Nginx
                    //MiddlewareLogHelper.WriterLog(LogType.MidLog, true, Color.Blue, "Nginx启动完成");
                    break;
                case StartType.KillAllProcess:
                    //MongodbManager.StopDB();//停止MongoDB  清理掉所有子进程，因为主进程关闭子进程不关闭的话，占用的端口号一样不会释放
                    //NginxManager.StopWeb();
                    break;
            }

        }

        public static void Quit(StartType type)
        {
           
            switch (type)
            {
                case StartType.BaseService:
                    try
                    {
                        if (mAppHost != null)
                        {
                            //EFWCoreLib.WcfFrame.ClientLinkPoolCache.Dispose();
                            ClientManage.StopHost();
                            mAppHost.Close();
                            MiddlewareLogHelper.WriterLog(LogType.MidLog, true, Color.Red, "数据服务已关闭！");
                        }
                    }
                    catch
                    {
                        if (mAppHost != null)
                            mAppHost.Abort();
                    }
                    break;

                case StartType.FileService:
                    try
                    {
                        if (mFileHost != null)
                        {
                            mFileHost.Close();
                            MiddlewareLogHelper.WriterLog(LogType.MidLog, true, Color.Red, "文件传输服务已关闭！");
                        }
                    }
                    catch
                    {
                        if (mFileHost != null)
                            mFileHost.Abort();
                    }
                    break;
                case StartType.RouterBaseService:
                    try
                    {
                        if (mRouterHost != null)
                        {
                            mRouterHost.Close();
                            MiddlewareLogHelper.WriterLog(LogType.MidLog, true, Color.Red, "数据路由服务已关闭！");
                        }
                    }
                    catch
                    {
                        if (mRouterHost != null)
                            mRouterHost.Abort();
                    }
                    break;
                case StartType.RouterFileService:
                    try
                    {
                        if (mFileRouterHost != null)
                        {
                            mFileRouterHost.Close();
                            MiddlewareLogHelper.WriterLog(LogType.MidLog, true, Color.Red, "文件路由服务已关闭！");
                        }
                    }
                    catch
                    {
                        if (mFileRouterHost != null)
                            mFileRouterHost.Abort();
                    }
                    break;
                case StartType.SuperClient:
                    SuperClient.UnCreateSuperClient();
                    MiddlewareLogHelper.WriterLog(LogType.TimingTaskLog, true, System.Drawing.Color.Red, "超级客户端已关闭！");
                    break;
                case StartType.MiddlewareTask:
                    MiddlewareTask.StopTask();//停止任务
                    MiddlewareLogHelper.WriterLog(LogType.TimingTaskLog, true, System.Drawing.Color.Red, "定时任务已停止！");
                    break;
                case StartType.PublishService://订阅
                    MiddlewareLogHelper.WriterLog(LogType.MidLog, true, Color.Red, "订阅服务已停止");
                    break;
                case StartType.MongoDB:
                    //MongodbManager.StopDB();//停止MongoDB
                    //MiddlewareLogHelper.WriterLog(LogType.MidLog, true, Color.Red, "MongoDB已停止");
                    break;
                case StartType.Nginx:
                    //NginxManager.StopWeb();
                    //MiddlewareLogHelper.WriterLog(LogType.MidLog, true, Color.Red, "Nginx已停止");
                    break;
            }
        }

        private static void GetAllConfig()
        {
            #region 收集配置信息
            Action<HostRunConfigSubject> psAction = ((HostRunConfigSubject subject) =>
            {
                List<RemotePlugin> rpList = RemotePluginManage.GetRemotePlugin();
                if (rpList != null)
                {
                    HostRunConfigObject configObj;
                    foreach (var p in rpList)
                    {
                        configObj = new HostRunConfigObject();
                        configObj.Label = "远程插件";
                        configObj.Key = p.ServerIdentify;
                        configObj.Value = String.Join(",", p.plugin);
                        configObj.Memo = p.ServerIdentify + "\t" + String.Join(",", p.plugin);
                        subject.ConfigObjList.Add(configObj);
                    }
                }
            });
            Action<HostRunConfigSubject> pubsAction = ((HostRunConfigSubject subject) =>
            {
                if (PublishServiceManage.serviceList != null)
                {
                    HostRunConfigObject configObj;
                    foreach (var p in PublishServiceManage.serviceList)
                    {
                        configObj = new HostRunConfigObject();
                        configObj.Label = "发布服务";
                        configObj.Key = p.publishServiceName;
                        configObj.Value = p.publishServiceName;
                        configObj.Memo = (p.whether ? "已发布" : "未发布") + "\t" + p.publishServiceName + "\t" + p.explain;
                        subject.ConfigObjList.Add(configObj);
                    }
                }
            });
            Action<HostRunConfigSubject> subsAction = ((HostRunConfigSubject subject) =>
            {
                if (PublishSubManager.psubserviceList != null)
                {
                    HostRunConfigObject configObj;
                    foreach (var p in PublishSubManager.psubserviceList)
                    {
                        configObj = new HostRunConfigObject();
                        configObj.Label = "订阅服务";
                        configObj.Key = p.publishServiceName;
                        configObj.Value = p.publishServiceName;
                        configObj.Memo = (p.IsSub ? "已订阅" : "未订阅") + "\t" + p.publishServiceName + "\t" + p.explain;
                        subject.ConfigObjList.Add(configObj);
                    }
                }
            });
            Action<HostRunConfigSubject> taskAction = ((HostRunConfigSubject subject) =>
            {
                if (MiddlewareTask.TaskConfigList != null)
                {
                    HostRunConfigObject configObj;
                    foreach (var p in MiddlewareTask.TaskConfigList)
                    {
                        configObj = new HostRunConfigObject();
                        configObj.Label = p.taskname;
                        configObj.Key = p.taskname;
                        configObj.Value = p.taskname;
                        configObj.Memo = (p.qswitch ? "已开启" : "未开启") + "\t" + p.execfrequencyName + "\t" + p.shorttimeName + "\t" + p.serialorparallelName;
                        subject.ConfigObjList.Add(configObj);
                    }
                }
            });
            HostRunConfigInfo.LoadConfigInfo(Identify, psAction, pubsAction, subsAction, taskAction);
            #endregion
        }
    }

    public enum StartType
    {
        BaseService,
        FileService,
        RouterBaseService,
        RouterFileService,
        MiddlewareTask,
        SuperClient,
        PublishService,
        MongoDB,
        Nginx,
        KillAllProcess
    }
}
