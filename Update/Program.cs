using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

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
            if (!mutexCreated)
            {
                m_Mutex = null;
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        /// <summary>
        /// 只运行一个程序的锁
        /// </summary>
        static private Mutex m_Mutex;
    }
}
