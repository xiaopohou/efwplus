using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using EFWCoreLib.CoreFrame.Business.AttributeInfo;
using EFWCoreLib.CoreFrame.Common;
using EFWCoreLib.CoreFrame.Init;
using EFWCoreLib.WcfFrame;
using EFWCoreLib.WcfFrame.DataSerialize;
using EFWCoreLib.WebFrame.WebAPI;

namespace EFWCoreLib.WebAPI.Utility
{
    /// <summary>
    /// 主机配置
    /// http://localhost:8021/hostconfig/hostconfig/showconfiginfo
    /// </summary>
    [efwplusApiController(PluginName = "coresys")]
    public class HostConfigController : WebApiController
    {
        //获取配置信息
        [HttpGet]
        public List<HostRunConfigSubject> ShowConfigInfo()
        {
            try
            {
                //ServiceResponseData response = InvokeWcfService("HostConfig.Service", "HostConfigController", "GetConfigSubject");
                //return response.GetData<List<HostRunConfigSubject>>(0);
                return HostRunConfigInfo.GetConfigSubject();
            }
            catch(Exception e)
            {
                throw e;
            }
        }

        //执行命令
        [HttpGet]
        public bool ExecuteCmd(string id)
        {
            try
            {
                id = id.ToLower();
                IPCWriteHelper ipcw = new IPCWriteHelper();
                if (id.IndexOf("efwplusbase:") > -1)
                {
                    ipcw.WriteData(id, IPCType.efwplusBase);
                }
                if (id.IndexOf("efwplusroute:") > -1)
                {
                    ipcw.WriteData(id, IPCType.efwplusRoute);
                }
                if (id.IndexOf("efwpluswebapi:") > -1)
                {
                    ipcw.WriteData(id, IPCType.efwplusWebAPI);
                }
                else if (id.IndexOf("efwplusservercmd:") > -1)
                {
                    ipcw.WriteData(id, IPCType.efwplusServerCmd);
                }
                else if (id.IndexOf("efwplusserver:") > -1)
                {
                    ipcw.WriteData(id, IPCType.efwplusServer);
                }
                else
                {
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}
