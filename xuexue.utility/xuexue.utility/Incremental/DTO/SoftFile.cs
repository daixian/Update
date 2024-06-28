﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace xuexue.utility.Incremental.DTO
{
    /// <summary>
    /// 一版软件
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class SoftFile
    {
        /// <summary>
        /// GNU 方案 :
        /// 命名规则：主版本号.子版本号[ .修正版本号[编译版本号]]
        /// 英文对照：Major_Version_Number.Minor_Version_Number[.Revision_Number[.Build_Number]]
        /// 示例：1.1.5,2.0, 2.1.0 build-1781
        /// </summary>
        [JsonProperty]
        public uint[] version = new uint[4];

        /// <summary>
        /// 在电脑上的安装文件夹的路径
        /// </summary>
        [JsonProperty]
        public string rootPath;

        /// <summary>
        /// 下载这些文件的URL
        /// </summary>
        [JsonProperty]
        public string rootUrl;

        /// <summary>
        /// 所有的文件项
        /// </summary>
        [JsonProperty]
        public List<Fileitem> files = new List<Fileitem>();

        /// <summary>
        /// 是否包含了某项文件
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        /// 
        public bool IsContainFile(Fileitem item)
        {
            for (int i = 0; i < files.Count; i++) {
                if (files[i] == item) {
                    return true;
                }
            }
            return false;
        }
    }
}
