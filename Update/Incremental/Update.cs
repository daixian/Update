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
using xuexue.utility;
using System.Diagnostics;

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
        public async Task Start(string configPath, Action<string> setMessage, Action<int> setProgress)
        {
            DLog.dlog_init("log", "F3dUpdate", DLog.INIT_RELATIVE.MODULE, false);

            //载入配置
            setMessage("载入配置...");
            config = JsonMapper.ToObject<UpdateConfig>(File.ReadAllText(configPath));
            if (config != null)
                DLog.LogI($"DoUpdate.Start():载入配置文件{configPath}成功,SoftName:{config.SoftName}");
            else
            {
                DLog.LogE($"DoUpdate.Start():载入配置文件{configPath}失败!");
                return;
            }

            //联网得到最新json内容
            setMessage("联网得到最新版本内容...");
            bool isGetNewVersionInfo = false;
            for (int i = 0; i < config.newVersionUrl.Length; i++)
            {
                if (!isGetNewVersionInfo)//有一个地址能成功就不再试后面的地址了
                {
                    await Http.Get(config.newVersionUrl[i]).OnSuccess((rtext) =>
                        {
                            if (!string.IsNullOrEmpty(rtext))
                            {
                                try
                                {
                                    newVersionSoft = JsonMapper.ToObject<SoftFile>(rtext);
                                    isGetNewVersionInfo = true;
                                    DLog.LogI($"DoUpdate.Start():联网获取最新软件版本成功,url={config.newVersionUrl[i]},最新版本为v{newVersionSoft.version[0]}.{newVersionSoft.version[1]}.{newVersionSoft.version[2]}.{newVersionSoft.version[3]}");
                                }
                                catch (Exception)
                                {
                                    newVersionSoft = null;
                                }
                            }

                        }).OnFail((e) =>
                        {
                            DLog.LogW($"DoUpdate.Start():联网获取最新软件版本失败,url={config.newVersionUrl[i]}");
                        }).GoAsync();


                }
            }
            if (newVersionSoft == null)
            {
                DLog.LogE($"DoUpdate.Start():联网获取最新软件地址失败...");
                setMessage("联网获取最新软件地址失败...");
                return;
            }
            setMessage("联网得到最新版本内容成功!");

            //载入本地当前软件版本信息
            setMessage("载入本地当前软件版本信息...");
            if (File.Exists(config.curVersionFile))
            {
                var sr = File.OpenText(config.curVersionFile);
                curVersionSoft = JsonMapper.ToObject<SoftFile>(sr);
                sr.Close();
                DLog.LogI($"DoUpdate.Start():当前软件版本为v{curVersionSoft.version[0]}.{curVersionSoft.version[1]}.{curVersionSoft.version[2]}.{curVersionSoft.version[3]}");
            }
            else
            {
                setMessage("没有找到本地当前软件版本信息...");
            }

            DownloadList downloadList = IncrementalUpdate.CompareToDownloadList(curVersionSoft, newVersionSoft);
            DLog.LogI($"DoUpdate.Start():有{downloadList.files.Count}个需要下载项.");
            if (downloadList.files.Count == 0)
            {
                DLog.LogI($"DoUpdate.Start():文件都是最新的,不需要下载!");
                setMessage("文件都是最新的,不需要更新!");
                return;
            }
            DirectoryInfo downlodeDir = new DirectoryInfo(Path.Combine(config.CacheDir, "download"));//下载到临时文件夹的download文件夹
            if (!downlodeDir.Exists)
                Directory.CreateDirectory(downlodeDir.FullName);

            //删除download文件夹里面所有不在下载列表里的文件,好方便等会无脑拷贝
            FileInfo[] fiIndw = downlodeDir.GetFiles("*", SearchOption.AllDirectories);
            for (int i = 0; i < fiIndw.Length; i++)
            {
                string rp = downlodeDir.RelativePath(fiIndw[i]);
                if (!downloadList.IsRelativePathInFiles(rp))
                {
                    fiIndw[i].Delete();
                }
            }

            bool isError = true;
            while (isError)
            {
                isError = false;//开始启动刷一遍下载的时候是error置为0的
                //下载每一项
                int doneCount = 0;
                foreach (var item in downloadList.files)
                {
                    FileInfo dwfilePath = new FileInfo(Path.Combine(downlodeDir.FullName, item.relativePath));
                    FileInfo targefilePath = new FileInfo(Path.Combine(config.SoftDir, item.relativePath));
                    //判断文件是否已经存在了,就跳过到下一个文件
                    if (dwfilePath.Exists && dwfilePath.SHA256() == item.SHA256)
                    {
                        setMessage($"{item.relativePath} cache!");
                    }
                    else if (targefilePath.Exists && targefilePath.SHA256() == item.SHA256)
                    {
                        setMessage($"{item.relativePath} 目标位置文件是最新!");
                    }
                    else
                    {
                        setMessage($"下载:{item.relativePath}...");
                        //如果文件不存在才下载
                        await Http.Get(item.url).DownloadTo(dwfilePath.FullName).OnFail((e) =>
                        {
                            DLog.LogE($"DoUpdate.Start():下载文件失败...  {item.url}");
                            isError = true;
                        }).GoAsync();
                    }
                    doneCount++;
                    setProgress(doneCount * 100 / downloadList.files.Count);
                }
            }

            setMessage($"所有文件下载完成!");

            foreach (var item in config.NeedCloseExeName)
            {
                while (KillProcess(item))//一直关闭到找不到这个进程
                {
                    await Task.Delay(500);
                }
            }
            setMessage($"启动拷贝文件程序...");

            //到了此处应该所有文件都下载完成了
            FileInfo movefileEXE = new FileInfo("./movefile.exe");
            if (movefileEXE.Exists)
                Process.Start(movefileEXE.FullName);
        }


        /// <summary>
        /// 已经关闭了没有找到进程才会返回false
        /// </summary>
        /// <param name="name">进程名不包括exe</param>
        /// <returns></returns>
        private bool KillProcess(string name)
        {
            Process[] pro = Process.GetProcesses();//获取已开启的所有进程
            //遍历所有查找到的进程
            for (int i = 0; i < pro.Length; i++)
            {
                //判断此进程是否是要查找的进程
                if (pro[i].ProcessName.ToString().ToLower() == name.ToLower())
                {
                    DLog.LogI("DoUpdate.killProcess():找到了进程,尝试关闭" + name);
                    try
                    {
                        pro[i].Kill();//结束进程
                    }
                    catch (Exception e)
                    {
                        DLog.LogE("DoUpdate.killProcess():kill失败！e=" + e.Message);
                    }
                    return true;
                }
            }
            DLog.LogI("DoUpdate.killProcess():没有找到进程" + name);
            return false;
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
