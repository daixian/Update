﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;


namespace Update.Incremental.DTO
{
    /// <summary>
    /// 增量更新的设置
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class UpdateConfig
    {
        /// <summary>
        /// 这个要更新的软件名
        /// </summary>
        [JsonProperty]
        public string SoftName = "TrackingService";

        /// <summary>
        /// 要替换升级的软件的目录
        /// </summary>
        [JsonProperty]
        public string SoftDir = @"C:\Program Files\TrackingService";

        /// <summary>
        /// 自己工作保存的临时文件,下载文件等等的目录
        /// </summary>
        [JsonProperty]
        public string CacheDir = "./cache";

        /// <summary>
        /// 当前软件版本文件路径
        /// </summary>
        [JsonProperty]
        public string curVersionFile = "./SoftVersion.json";

        /// <summary>
        /// 新版本的软件信息文件的URL
        /// </summary>
        [JsonProperty]
        public string[] newVersionUrl = new string[] { "http://hrd.kmaxxr.com/update/TrackingService/v1.0.0.0.json" };

        /// <summary>
        /// 查询当前是否可以移动文件的url:"http://127.0.0.1:42015/status/can_start_update"
        /// </summary>
        [JsonProperty]
        public string CanMoveFileUrl;

        /// <summary>
        /// 要发送http请求主动关闭的进程url
        /// </summary>
        [JsonProperty]
        public string[] CloseExeUrl = new string[] { "http://127.0.0.1:42015/app/exit" };

        /// <summary>
        /// 要执行的命令行.
        /// 这里有个坑,由于Update是AuxiliaryService的子进程,如果调用这个stop会把Update自己给结束掉.,所以不能调用这个.
        /// </summary>
        [JsonProperty]
        public string[] cmds = new string[] { "cd 'C:\\Program Files\\TrackingService'", 
                                              //".\\tool\\AuxiliaryService.exe stop",
                                              ".\\TrackerService.exe stop" };

        /// <summary>
        /// 当移动文件前需要保证关闭的进程名,
        /// 虽然Update是Auxiliary的子进程,但是kill这个Auxiliary并不会关闭Update自己.
        /// </summary>
        [JsonProperty]
        public string[] NeedCloseExeName = new string[] { "TrackerService", "Tracking", "Diagnosis" };

        /// <summary>
        /// 当升级成功之后需要启动的exe名
        /// </summary>
        [JsonProperty]
        public string[] StartUpExeName = new string[] { };

        /// <summary>
        /// 当升级成功之后需要启动的Windows Server名
        /// </summary>
        [JsonProperty]
        public string[] StartUpWinServerName = new string[] { "TrackerService" };
    }
}
