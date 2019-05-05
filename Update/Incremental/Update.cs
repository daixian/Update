using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Update.Incremental.DTO;
using xuexue.LitJson;
using Ionic.Zip;
using JumpKick.HttpLib;
using xuexue.utility.Incremental.DTO;
using System.Windows.Forms;
using System.Threading;
using xuexue.utility.Incremental;
using xuexue;
namespace Update.Incremental
{
    /// <summary>
    /// 升级的逻辑
    /// </summary>
    public class DoUpdate
    {
        /// <summary>
        /// 创建一个默认的配置文件
        /// </summary>
        /// <param name="configPath"></param>
        public static void CreatConfigFile(string configPath)
        {
            UpdateConfig defaultConfig = new UpdateConfig();
            var sw = File.CreateText(configPath);
            JsonWriter jw = new JsonWriter(sw) { PrettyPrint = true };
            JsonMapper.ToJson(defaultConfig, jw);
            sw.Close();
        }

        /// <summary>
        /// 执行一次升级的流程
        /// </summary>
        public async void Start(string configPath)
        {
            DLog.dlog_init("log", "F3dUpdate", DLog.INIT_RELATIVE.MODULE, false);

            //载入配置
            config = JsonMapper.ToObject<UpdateConfig>(File.ReadAllText(configPath));
            if (config != null)
                DLog.LogI($"DoUpdate.Start():载入配置文件{configPath}成功,SoftName:{config.SoftName}");
            else
            {
                DLog.LogE($"DoUpdate.Start():载入配置文件{configPath}失败!");
                return;
            }


            //载入本地当前软件版本信息
            var sr = File.OpenText(config.curVersionFile);
            curVersionSoft = JsonMapper.ToObject<SoftFile>(sr);
            sr.Close();
            DLog.LogI($"DoUpdate.Start():当前软件版本为v{curVersionSoft.version[0]}.{curVersionSoft.version[1]}.{curVersionSoft.version[2]}.{curVersionSoft.version[3]}");

            //联网得到最新json内容
            bool isGetNewVersionInfo = false;
            for (int i = 0; i < config.newVersionUrl.Length; i++)
            {
                if (!isGetNewVersionInfo)//有一个地址能成功就不再试后面的地址了
                {
                    await Http.Get(config.newVersionUrl[i]).OnSuccess((rtext) =>
                        {
                            newVersionSoft = JsonMapper.ToObject<SoftFile>(rtext);
                            isGetNewVersionInfo = true;
                            DLog.LogI($"DoUpdate.Start():联网获取最新软件版本成功,url={config.newVersionUrl[i]},最新版本为v{newVersionSoft.version[0]}.{newVersionSoft.version[1]}.{newVersionSoft.version[2]}.{newVersionSoft.version[3]}");
                        }).OnFail((e) =>
                        {
                            DLog.LogW($"DoUpdate.Start():联网获取最新软件版本失败,url={config.newVersionUrl[i]}");
                        }).GoAsync();


                }
            }
            if (newVersionSoft == null)
            {
                DLog.LogE($"DoUpdate.Start():联网获取最新软件地址失败...");
                return;
            }

            DownloadList downloadList = IncrementalUpdate.CompareToDownloadList(curVersionSoft, newVersionSoft);
            DLog.LogI($"DoUpdate.Start():有{downloadList.files.Count}个需要下载项.");
            if (downloadList.files.Count == 0)
            {
                DLog.LogI($"DoUpdate.Start():文件都是最新的,不需要下载!");
                return;
            }
            string downlodeDir = Path.Combine(config.CacheDir, "download");//下载到临时文件夹的download文件夹
            Directory.CreateDirectory(downlodeDir);

            bool isError = true;
            while (isError)
            {
                isError = false;//开始启动刷一遍下载的时候是error置为0的
                //下载每一项
                foreach (var item in downloadList.files)
                {
                    string filePath = Path.Combine(downlodeDir, item.relativePath);
                    //判断文件是否已经存在了,就跳过到下一个文件

                    //如果文件不存在才下载
                    await Http.Get(item.url).DownloadTo(filePath).OnFail((e) =>
                    {
                        DLog.LogE($"DoUpdate.Start():下载文件失败...  {item.url}");
                        isError = true;
                    }).GoAsync();
                }
            }

            //到了此处应该所有文件都下载完成了

        }

        /// <summary>
        /// 整个升级的所有设置
        /// </summary>
        public UpdateConfig config;

        /// <summary>
        /// 当前版本软件的信息
        /// </summary>
        private SoftFile curVersionSoft = null;

        /// <summary>
        /// 新版本软件的信息
        /// </summary>
        private SoftFile newVersionSoft = null;

    }
}
