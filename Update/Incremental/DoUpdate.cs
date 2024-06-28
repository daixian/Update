using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Update.Incremental.DTO;
using Ionic.Zip;
using xuexue.utility.Incremental.DTO;
using System.Windows.Forms;
using System.Threading;
using xuexue.utility.Incremental;
using xuexue;
using xuexue.utility;
using System.Diagnostics;
using Flurl.Http;
using Newtonsoft.Json;
using xuexue.common;
using log4net;

namespace Update.Incremental
{
    /// <summary>
    /// 升级的逻辑
    /// </summary>
    public class DoUpdate
    {
        private static readonly ILog Log =
            LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// 创建一个默认的配置文件
        /// </summary>
        /// <param name="configPath"></param>
        public static void CreatConfigFile(string configPath)
        {
            UpdateConfig defaultConfig = new UpdateConfig();

            string json = JsonConvert.SerializeObject(defaultConfig, Formatting.Indented);
            File.WriteAllText(configPath, json);
        }

        /// <summary>
        /// 执行一次升级的流程
        /// </summary>
        public async Task Start(string configPath, Action<string> setMessage, Action<int> setProgress)
        {

            string movedoneFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "movedone");
            if (File.Exists(movedoneFile))
                File.Delete(movedoneFile);

            //载入配置
            setMessage("载入配置...");
            Log.Info($"DoUpdate.Start():载入配置文件..");
            try {
                //config = JsonMapper.ToObject<UpdateConfig>(File.ReadAllText(configPath));
                config = JsonConvert.DeserializeObject<UpdateConfig>(File.ReadAllText(configPath));

            }
            catch (Exception e) {
                Log.Error($"DoUpdate.Start():载入配置文件异常{e.Message}");
            }
            if (config != null)
                Log.Info($"DoUpdate.Start():载入配置文件{configPath}成功,SoftName:{config.SoftName}");
            else {
                Log.Error($"DoUpdate.Start():载入配置文件{configPath}失败!");
                return;
            }

            //联网得到最新json内容
            setMessage("联网得到最新版本内容...");
            bool isGetNewVersionInfo = false;
            for (int i = 0; i < config.newVersionUrl.Length; i++) {
                if (!isGetNewVersionInfo)//有一个地址能成功就不再试后面的地址了
                {
                    try {
                        string rtext = await config.newVersionUrl[i].GetStringAsync();
                        //newVersionSoft = JsonMapper.ToObject<SoftFile>(rtext);
                        newVersionSoft = JsonConvert.DeserializeObject<SoftFile>(rtext);
                        isGetNewVersionInfo = true;
                        Log.Info($"DoUpdate.Start():联网获取最新软件版本成功,url={config.newVersionUrl[i]},最新版本为v{newVersionSoft.version[0]}.{newVersionSoft.version[1]}.{newVersionSoft.version[2]}.{newVersionSoft.version[3]}");

                    }
                    catch (Exception e) {
                        newVersionSoft = null;
                        Log.Warn($"DoUpdate.Start():联网获取最新软件版本失败,url={config.newVersionUrl[i]},err={e.Message}");
                    }
                }
            }
            if (newVersionSoft == null) {
                Log.Error($"DoUpdate.Start():联网获取最新软件地址失败...");
                setMessage("联网获取最新软件地址失败...");
                return;
            }
            setMessage("联网得到最新版本内容成功!");

            //载入本地当前软件版本信息
            setMessage("载入本地当前软件版本信息...");
            if (File.Exists(config.curVersionFile)) {
                //var sr = File.OpenText(config.curVersionFile);
                //curVersionSoft = JsonMapper.ToObject<SoftFile>(sr);
                //sr.Close();
                curVersionSoft = JsonConvert.DeserializeObject<SoftFile>(File.ReadAllText(config.curVersionFile));
                Log.Info($"DoUpdate.Start():当前软件版本为v{curVersionSoft.version[0]}.{curVersionSoft.version[1]}.{curVersionSoft.version[2]}.{curVersionSoft.version[3]}");
            }
            else {
                setMessage("没有找到本地当前软件版本信息...");
            }

            DownloadList downloadList = IncrementalUpdate.CompareToDownloadList(curVersionSoft, newVersionSoft);
            Log.Info($"DoUpdate.Start():有{downloadList.files.Count}个需要下载项.");
            if (downloadList.files.Count == 0) {
                Log.Info($"DoUpdate.Start():文件都是最新的,不需要下载!");
                setMessage("文件都是最新的,不需要更新!");
                return;
            }

            DirectoryInfo downlodeDir = new DirectoryInfo(Path.Combine(config.CacheDir, "download"));//下载到临时文件夹的download文件夹
            if (!downlodeDir.Exists)
                Directory.CreateDirectory(downlodeDir.FullName);

            Log.Info($"DoUpdate.Start():downlodeDir={downlodeDir.FullName}");
            //删除download文件夹里面所有不在下载列表里的文件,好方便等会无脑拷贝
            FileInfo[] fiIndw = downlodeDir.GetFiles("*", SearchOption.AllDirectories);
            for (int i = 0; i < fiIndw.Length; i++) {
                string rp = downlodeDir.RelativePath(fiIndw[i]);
                if (!downloadList.IsRelativePathInFiles(rp)) {
                    fiIndw[i].Delete();
                }
            }

            bool isError = true;
            while (isError) {
                isError = false;//开始启动刷一遍下载的时候是error置为0的
                //下载每一项
                int doneCount = 0;
                foreach (var item in downloadList.files) {
                    try {
                        FileInfo dwfilePath = new FileInfo(Path.Combine(downlodeDir.FullName, item.relativePath));
                        FileInfo targefilePath = new FileInfo(Path.Combine(config.SoftDir, item.relativePath));
                        //判断文件是否已经存在了,就跳过到下一个文件
                        if (targefilePath.Exists && targefilePath.SHA256() == item.SHA256) {
                            Log.Info($"{item.relativePath} 目标位置文件是最新,不需要下载!");
                            setMessage($"{item.relativePath} 目标位置文件是最新,不需要下载!");
                            if (dwfilePath.Exists) {
                                dwfilePath.Delete();
                            }
                        }
                        else if (dwfilePath.Exists && dwfilePath.SHA256() == item.SHA256) {
                            Log.Info($"{item.relativePath} 有缓存文件了!");
                            setMessage($"{item.relativePath} 有缓存文件了!");
                        }
                        else {
                            Log.Info($"下载:{item.relativePath}...");
                            setMessage($"下载:{item.relativePath}...");
                            //如果文件不存在才下载
                            try {
                                await item.url.DownloadFileAsync(dwfilePath.Directory.FullName);
                                dwfilePath.Refresh();//需要刷新一下
                                //下载完成之后校验sha256
                                if (dwfilePath.Exists && (dwfilePath.SHA256() == item.SHA256)) {
                                    Log.Info($"下载:{item.relativePath}成功,校验SHA256通过!");
                                }
                                else {
                                    Log.Error($"DoUpdate.Start():校验文件SHA256失败{item.relativePath}");
                                    isError = true;
                                }
                            }
                            catch (Exception e) {
                                Log.Error($"DoUpdate.Start():下载文件失败...  {item.url},err={e.Message}");
                                isError = true;
                            }
                        }
                        doneCount++;
                        setProgress(doneCount * 100 / downloadList.files.Count);
                    }
                    catch (Exception e) {
                        Log.Error($"下载文件发生错误!" + e.Message);
                        setMessage($"下载文件发生错误!" + e.Message);
                        isError = true;
                    }
                }
            }

            FileInfo[] needCopyFis = downlodeDir.GetFiles("*", SearchOption.AllDirectories);
            if (needCopyFis.Length == 0) {
                Log.Info($"没有需要更新的文件!");
                setMessage($"没有需要更新的文件!");
                return;
            }
            Log.Info($"所有文件下载完成!");
            setMessage($"所有文件下载完成!");

            //TODO:这里需要向追踪服务查询是否空闲
            if (!string.IsNullOrEmpty(config.CanMoveFileUrl)) {
                try {
                    Thread.Sleep(10 * 1000);//10秒后再开始查询这个,这样如果是开机没有人使用就会开始休眠
                    Log.Info($"查询是否可以移动文件...");
                    while (true) {
                        string canMoveFile = await config.CanMoveFileUrl.GetStringAsync();
                        Log.Info($"查询结果:{canMoveFile}");
                        if (canMoveFile == "true") {
                            Log.Info($"查询成功,可以开始移动文件!");
                            break;
                        }
                        else {
                            Log.Info($"当前有人正在使用程序,不能移动文件,等待30秒后再试!");
                            Thread.Sleep(30 * 1000);//30秒后再问一次
                        }
                    }
                }
                catch (Exception e) {
                    //如果异常那么也直接启动拷贝程序
                    Log.Info($"查询是否可以移动文件异常{e.Message}");
                }
            }

            if (config.CloseExeUrl != null) {
                foreach (var closeurl in config.CloseExeUrl) {
                    try {
                        if (!string.IsNullOrEmpty(closeurl)) {
                            Log.Info($"尝试发送主动关闭请求{closeurl}");
                            string res = await closeurl.GetStringAsync();
                            Log.Info($"发送主动关闭请求返回{res}");
                            if (res != null) {
                                Thread.Sleep(5 * 1000);//休眠5秒等人家关闭
                            }
                        }
                    }
                    catch (Exception e) {
                        Log.Info($"发送主动关闭请求异常{e.Message}");
                    }
                }
            }

            if (config.cmds != null) {
                Log.Info($"执行cmd命令,一共{config.cmds.Length}条..");
                RunCmd(config.cmds);
                //await Task.Delay(3000);
                // 这里由于子进程的原因,所以不要等待了,直接去下面关闭吧.
            }

            foreach (string item in config.NeedCloseExeName) {
                //一共尝试关闭5次吧
                int tryCount = 0;
                while (true) {
                    tryCount++;
                    bool processExist = KillProcess(item);
                    if (processExist) {
                        await Task.Delay(2000);//两秒一次吧
                    }
                    else {
                        //成功关闭了
                        break;
                    }
                    if (tryCount >= 5) {
                        Log.Info($"进程{item}关闭失败，但是可能该进程已经挂起，后面尝试移动文件吧.");
                        break;
                    }

                }
            }
            setMessage($"启动拷贝文件程序...");
            //到了此处应该所有文件都下载完成了
            FileInfo movefileEXE = new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "movefile.exe"));
            if (movefileEXE.Exists) {
                Log.Info($"启动拷贝文件程序" + movefileEXE.FullName);
                Process.Start(movefileEXE.FullName);
            }
            else {
                Log.Info($"启动拷贝文件程序 不存在" + movefileEXE.FullName);
            }
        }

        /// <summary>
        /// 执行命令行
        /// </summary>
        /// <param name="cmds"></param>
        private void RunCmd(string[] cmds)
        {
            if (cmds == null || cmds.Length == 0) {
                return;
            }
            try {
                Process process = new Process();
                process.StartInfo.FileName = "powershell.exe";
                process.StartInfo.UseShellExecute = false;//需要明确执行一个已知的程序,需要重定向输入和输出
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                for (int i = 0; i < cmds.Length; i++) {
                    Log.Info($"cmd执行:{cmds[i]}");
                    process.StandardInput.WriteLine(cmds[i]);
                    //Log.Info($"{process.StandardOutput.ReadToEndAsync}");
                }
                Log.Info($"cmd执行:exit");
                process.StandardInput.WriteLine("exit");
                Task.Run(() => {
                    Log.Info($"{process.StandardOutput.ReadToEnd()}");
                });

            }
            catch (Exception e) {
                Log.Error($"执行命令行异常" + e.Message);
            }
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
            for (int i = 0; i < pro.Length; i++) {
                //判断此进程是否是要查找的进程
                if (pro[i].ProcessName.ToString().ToLower() == name.ToLower()) {
                    Log.Info($"DoUpdate.killProcess():找到了进程 {name} ,尝试关闭");
                    try {
                        pro[i].Kill();//结束进程
                    }
                    catch (Exception e) {
                        Log.Error($"DoUpdate.killProcess():kill {name} 失败！e={e.Message}");
                    }
                    return true;
                }
            }
            Log.Info("DoUpdate.killProcess():没有找到进程" + name);
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
