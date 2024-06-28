using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using xuexue.common;

namespace Update
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            // 只运行一个程序
            bool mutexCreated;
            String mutexName = "DDCC6B50-902B-418E-BAF6-D494EC9FE132";
            m_Mutex = new Mutex(true, mutexName, out mutexCreated);
            if (!mutexCreated) {
                m_Mutex = null;
                return;
            }

            LogHelper.AppName = "Update";
            LogHelper.Setup();

            CheckAdministrator();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        /// <summary>
        /// 只运行一个程序的锁
        /// </summary>
        static private Mutex m_Mutex;

        /// <summary>
        /// 使用管理员权限启动程序
        /// </summary>
        private static void CheckAdministrator()
        {
            var wi = WindowsIdentity.GetCurrent();
            var wp = new WindowsPrincipal(wi);

            bool runAsAdmin = wp.IsInRole(WindowsBuiltInRole.Administrator);

            if (!runAsAdmin) {
                // It is not possible to launch a ClickOnce app as administrator directly,  
                // so instead we launch the app as administrator in a new process.  
                var processInfo = new ProcessStartInfo(Assembly.GetExecutingAssembly().CodeBase);

                // The following properties run the new process as administrator  
                processInfo.UseShellExecute = true;
                processInfo.Verb = "runas";
                // Start the new process  
                try {
                    Process.Start(processInfo);

                } catch (Exception ex) {
                }

                // Shut down the current process  
                Environment.Exit(0);
            }
        }
    }
}
