using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using xuexue.LitJson;

namespace Update.Incremental.DTO
{
    /// <summary>
    /// 增量更新的设置
    /// </summary>
    [xuexueJsonClass]
    public class UpdateConfig
    {
        /// <summary>
        /// 这个要更新的软件名
        /// </summary>
        public string SoftName = "MRSystem";

        /// <summary>
        /// 要替换升级的软件的目录
        /// </summary>
        public string SoftDir = @"C:\Program Files\TrackingService";

        /// <summary>
        /// 自己工作保存的临时文件,下载文件等等的目录
        /// </summary>
        public string CacheDir = "./cache";

        /// <summary>
        /// 当前软件版本文件路径
        /// </summary>
        public string curVersionFile = "./SoftVersion.json";

        /// <summary>
        /// 新版本的软件信息文件的URL
        /// </summary>
        public string[] newVersionUrl = new string[] { "https://home.xuexuesoft.com:8010/update/TrackingService/v1.0.0.0.json" };

        /// <summary>
        /// 当移动文件前需要保证关闭的进程名
        /// </summary>
        public string[] NeedCloseExeName = new string[] { "TrackerService", "Tracking", "Diagnosis" };

        /// <summary>
        /// 当升级成功之后需要启动的exe名
        /// </summary>
        public string[] StartUpExeName = new string[] { };

        /// <summary>
        /// 当升级成功之后需要启动的Windows Server名
        /// </summary>
        public string[] StartUpWinServerName = new string[] { "TrackerService" };
    }
}
