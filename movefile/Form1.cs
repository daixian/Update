using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading;

namespace movefile
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 配置文件
        /// </summary>
        private string configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "movefile.conf");

        /// <summary>
        /// 要拷贝的源文件夹
        /// </summary>
        private string sourceDir;

        /// <summary>
        /// 目标文件夹
        /// </summary>
        private string targetDir;

        /// <summary>
        /// 要启动的exe
        /// </summary>
        private List<string> listStartEXE = new List<string>();

        /// <summary>
        /// 要启动的服务
        /// </summary>
        private List<string> listStartServer = new List<string>();

        private void Form1_Load(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                MoveFile();
            });
        }

        private void MoveFile()
        {
            if (File.Exists("./movedone"))
                File.Delete("./movedone");

            setProgress(0);

            //读一下文件
            if (File.Exists(configFile))
            {
                string[] lines = File.ReadAllLines(configFile);
                sourceDir = lines[0];//第一行表示源文件夹位置
                targetDir = lines[1];//第二行表示目标文件夹

                for (int i = 2; i < lines.Length; i++)
                {
                    //这是一个要启动的exe的配置
                    Match m = Regex.Match(lines[i], @"^exe\s*:\s*");
                    if (m.Success)
                    {
                        listStartEXE.Add(lines[i].Substring(m.Index + m.Length));
                        continue;
                    }
                    m = Regex.Match(lines[i], @"^server\s*:\s*");
                    if (m.Success)
                    {
                        listStartServer.Add(lines[i].Substring(m.Index + m.Length));
                        continue;
                    }
                }
            }
            else
            {
                return;
            }

            DirectoryInfo di = new DirectoryInfo(sourceDir);
            if (!di.Exists)
            {
                return;
            }

            FileInfo[] fis = di.GetFiles("*", SearchOption.AllDirectories);//无筛选的得到所有文件
            for (int i = 0; i < fis.Length; i++)
            {
                string relativePath;
                //生成相对路径:这个文件的完整目录中替换根目录的部分即可,最后切分文件夹都使用斜杠/ (unity的API中基本是/)
                //相对路径结果前面不带斜杠
                if (di.FullName.EndsWith("\\") || di.FullName.EndsWith("/"))
                {
                    relativePath = fis[i].FullName.Substring(di.FullName.Length).Replace("\\", "/");
                }
                else
                {
                    //为了相对路径结果前面不带斜杠,所以+1
                    relativePath = fis[i].FullName.Substring(di.FullName.Length + 1).Replace("\\", "/");
                }

                //文件拷贝过去
                FileInfo targetFile = new FileInfo(Path.Combine(targetDir, relativePath));
                if (!targetFile.Directory.Exists)
                {
                    Directory.CreateDirectory(targetFile.Directory.FullName);
                }
                File.Copy(fis[i].FullName, targetFile.FullName, true);
                //设置进度
                setProgress(i * 100 / fis.Length);
            }
            setProgress(100);
            //创建一个文件标记拷贝完成了
            File.WriteAllLines("./movedone", new string[] { sourceDir, targetDir });

            for (int i = 0; i < listStartEXE.Count; i++)
            {
                StartEXE(listStartEXE[i]);
            }

            for (int i = 0; i < listStartServer.Count; i++)
            {
                StartServer(listStartServer[i]);
            }

            //关闭自己算了
            endCopy();
        }

        private void setProgress(int value)
        {
            this.Invoke(new Action(() =>
            {
                this.progressBar1.Value = value;
            }));
        }

        private void endCopy()
        {
            this.Invoke(new Action(() =>
            {
                TipWin tipSuccess = new TipWin();
                tipSuccess.Show();
                this.Hide();
            }));
        }

        private void StartEXE(string fileName)
        {
            try
            {
                FileInfo fi = new FileInfo(fileName);
                if (fi.Exists)
                {
                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = fi.FullName;
                    psi.WorkingDirectory = fi.Directory.FullName;
                    Process.Start(psi);
                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// 启动服务
        /// </summary>
        /// <returns></returns>
        public void StartServer(string name)
        {

            ServiceController sc = null;
            try
            {
                //DLog.LogI("ServerHelper.StartServer():创建服务控制,准备启动服务！");
                sc = new ServiceController(name);

                //开启服务
                if ((sc.Status.Equals(ServiceControllerStatus.Stopped)) || (sc.Status.Equals(ServiceControllerStatus.StopPending)))
                {
                    sc.Start();
                    sc.Refresh();
                }
                // DLog.LogI("ServerHelper.StartServer():服务启动成功！");
            }
            catch (Exception e)
            {
                //DLog.LogE("ServerHelper.StartServer():尝试开始服务失败！e=" + e.Message);
            }
            finally
            {
                if (sc != null)
                    sc.Close();
            }

        }
    }
}