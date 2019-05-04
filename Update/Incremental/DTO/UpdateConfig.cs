using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Update.Incremental.DTO
{
    /// <summary>
    /// 增量更新的设置
    /// </summary>
    public class UpdateConfig
    {
        /// <summary>
        /// 要替换升级的软件的目录
        /// </summary>
        public string SoftDir;

        /// <summary>
        /// 自己工作保存的临时文件,下载文件等等的目录
        /// </summary>
        public string CacheDir;

        /// <summary>
        /// 当前软件版本文件路径
        /// </summary>
        public string curVersionFile;

        /// <summary>
        /// 新版本的软件信息文件的URL
        /// </summary>
        public string newVersionUrl;

        /// <summary>
        /// 当移动文件前需要保证关闭的进程名
        /// </summary>
        public string[] NeedCloseExeName;

        /// <summary>
        /// 当升级成功之后需要启动的exe名
        /// </summary>
        public string[] StartUpExeName;

        /// <summary>
        /// 当升级成功之后需要启动的Windows Server名
        /// </summary>
        public string[] StartUpWinServerName;
    }
}
