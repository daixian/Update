using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xuexue.common
{
    /// <summary>
    /// log4net的帮助库.
    /// </summary>
    public class LogHelper
    {
        /// <summary>
        /// 日志使用的App名字
        /// </summary>
        public static string AppName;

        /// <summary>
        /// 在程序启动的时候设置一次,使用代码来代替xml的设置文件.
        /// 这个配置生成的日志文件形如"C:/logs/Auxiliary/2021-05-29-191824.log"
        /// </summary>
        /// <param name="logDir">日志文件的目录形如C:\\logs\\AssemblyName\\</param>
        public static void Setup(string logDir = null)
        {
            try {
                // 写入文档时的日志格式
                var patternLayout = new PatternLayout {
                    // 加%method应该会有反射.看需求是否加
                    ConversionPattern = "%date [%thread] %-5level %logger.%method %message%newline"
                };
                patternLayout.ActivateOptions();

                // 这个exe的名字,不带exe
                if (string.IsNullOrEmpty(AppName)) {
                    AppName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
                }

                // 文档日志
                var roller = new RollingFileAppender {
                    //g代表的是公元,所以必须要\g ,如果要向java那样按日期来回滚的话,那么这里就留文件夹路径
                    File = $"C:\\logs\\{AppName}\\{DateTime.Now.ToString("yyyy-MM-dd-HHmmss.lo\\g")}",
                    AppendToFile = true,
                    Encoding = Encoding.UTF8,
                    DatePattern = "yyyy-MM-dd'.log'",//yyyy-MM-dd-HHmmss.log 这样写的话每分钟都会回滚
                    RollingStyle = RollingFileAppender.RollingMode.Once,//注意这里的模式不能是Date,否则上面的毫秒的文件名会导致它不停的创建日志文件.
                    MaxSizeRollBackups = 0,
                    MaximumFileSize = "10MB",
                    StaticLogFileName = false,
                    Layout = patternLayout
                };

                // 如果有设置日志文件夹那么就使用设置的日志文件夹
                if (logDir != null) {
                    if (!logDir.EndsWith("\\") && !logDir.EndsWith("/")) {
                        logDir += Path.DirectorySeparatorChar;
                    }
                    roller.File = logDir;
                }
                roller.ActivateOptions();

                // 命令行日志格式
                patternLayout = new PatternLayout {
                    // 加%method应该会有反射.看需求是否加
                    ConversionPattern = "%date %level %logger.%method %line - %message %newline"
                };
                patternLayout.ActivateOptions();

                // 命令行日志
                var consoleAppender = new ConsoleAppender {
                    Name = "console",
                    Layout = patternLayout
                };
                consoleAppender.ActivateOptions();

                Hierarchy hierarchy = (Hierarchy)LogManager.GetRepository();
                hierarchy.Root.Level = Level.All;
                hierarchy.Root.AddAppender(roller);
                hierarchy.Root.AddAppender(consoleAppender);
                hierarchy.Configured = true;
            } catch (Exception) {

            }
        }

        /// <summary>
        /// 清理日志文件夹log4net没有这个功能.
        /// </summary>
        /// <param name="logDir"></param>
        public static void ClearLogDir(string logDir = null)
        {
            try {
                // 这个exe的名字,不带exe 
                if (string.IsNullOrEmpty(AppName)) {
                    AppName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
                }

                string curLogDir = $"C:\\logs\\{AppName}\\";
                if (logDir != null) {
                    curLogDir = logDir;
                }

                string[] files = Directory.GetFiles(curLogDir);
                for (int i = 0; i < files.Length; i++) {
                    FileInfo logFile = new FileInfo(files[i]);
                    if (logFile.Extension.ToLower() == ".log") {
                        //清理3天前的文件?
                        if (logFile.LastWriteTime < DateTime.Now.AddDays(-3)) {
                            logFile.Delete();
                        }
                    }
                }
            } catch (Exception e) {

            }

        }
    }


}
