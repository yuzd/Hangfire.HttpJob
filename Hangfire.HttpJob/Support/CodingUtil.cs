using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Hangfire.Logging;
using Newtonsoft.Json;

namespace Hangfire.HttpJob.Support
{
    public class CodingUtil
    {
        private static readonly ILog Logger = LogProvider.For<CodingUtil>();

        /// <summary>
        /// 启动配置
        /// </summary>
        public static HangfireHttpJobOptions HangfireHttpJobOptions = new HangfireHttpJobOptions();


        /// <summary>
        /// appsettions.json配置文件最后更新时间
        /// </summary>
        private static DateTime? _appJsonLastWriteTime;

        /// <summary>
        /// appsettions.json配置文件内容
        /// </summary>
        private static Dictionary<string, object> _appsettingsJson = new Dictionary<string, object>();

        /// <summary>
        /// 全局配置 每次会检测是否有改变
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, object> GetGlobalAppsettings()
        {
            var jsonFile = new FileInfo(HangfireHttpJobOptions.GlobalSettingJsonFilePath);
            if (jsonFile.Exists && (_appJsonLastWriteTime == null || _appJsonLastWriteTime != jsonFile.LastWriteTime))
            {
                _appJsonLastWriteTime = jsonFile.LastWriteTime;
                try
                {
                    _appsettingsJson = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(jsonFile.FullName));
                }
                catch (Exception e)
                {
                    Logger.WarnException($"HangfireHttpJobOptions.GlobalSettingJsonFilePath read fail", e);
                }
            }

            return _appsettingsJson;
        }





        /// <summary>
        /// MD5函数
        /// </summary>
        /// <param name="str">原始字符串</param>
        /// <returns>MD5结果</returns>
        public static string MD5(string str)
        {
            byte[] b = Encoding.UTF8.GetBytes(str);
            b = new MD5CryptoServiceProvider().ComputeHash(b);
            string ret = string.Empty;
            for (int i = 0; i < b.Length; i++)
            {
                ret += b[i].ToString("x").PadLeft(2, '0');
            }
            return ret;
        }

        public static T FromJson<T>(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception)
            {
                return default(T);
            }
        }
    }
}
