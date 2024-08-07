﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using xuexue.utility;
using xuexue.utility.Incremental;

namespace CalcSHA256
{
    class Program
    {
        static void Main(string[] args)
        {
            DirectoryInfo root = new DirectoryInfo(rootDir);
            DirectoryInfo[] dis = root.GetDirectories("*", SearchOption.AllDirectories);
            foreach (var di in dis) {
                if (di.Parent.Parent.FullName == root.FullName &&
                    Regex.IsMatch(di.Name, @"^v\d+\.\d+\.\d+.\d+$")) {
                    Console.WriteLine($"找到了一个文件夹:{di.FullName}");
                    FileInfo fiVerJson = new FileInfo(di.FullName + ".json");

                    if (fiVerJson.Exists) {
                        fiVerJson.Delete();
                        Console.WriteLine($"这个版本已经计算过SHA256,删除掉旧文件...");
                    }

                    //if (!fiVerJson.Exists) {
                    //Console.WriteLine($"这个版本没有计算过SHA256,开始计算...");

                    List<uint> ver = new List<uint>();
                    foreach (Match m in Regex.Matches(di.Name, @"\d+")) {
                        ver.Add(Convert.ToUInt32(m.Value));
                    }

                    IncrementalUpdate.CreateSoftVersionFile(softVerson_rootPath, ver.ToArray(), rootUrl: softVerson_rootURL + root.RelativePath(di) + "/",
                        di.FullName, fiVerJson.FullName,
                      debugRootUrl: softVerson_rootURLDebug + root.RelativePath(di) + "/");
                    Console.WriteLine($"计算结束,保存到{fiVerJson.FullName}");

                    //}
                    //else {
                    //    Console.WriteLine($"这个版本已经计算过SHA256,跳过...");
                    //}

                }
            }

            Console.WriteLine($"执行完毕,按键退出...");
            Console.ReadKey();
        }

        static string rootDir = ".";

        //等以后做成读取当前项目文件夹的配置内容
        static string softVerson_rootPath = "C:\\Program Files\\TrackingService";
        static string softVerson_rootURL = "http://hrd.kmaxxr.com/update/";
        static string softVerson_rootURLDebug = "https://hp.xuexuesoft.com:8010/update/";
    }
}
