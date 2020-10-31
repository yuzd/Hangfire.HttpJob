using System;
using Newtonsoft.Json;

namespace Hangfire.HttpJob.Agent.Util
{
    internal static class CodingUtil
    {


        public static T ToJson<T>(this string str)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(str);
            }
            catch (Exception )
            {
                return default(T);
            }

        }

        ///<summary>
        ///由秒数得到日期几天几小时。。。
        ///</summary
        ///<param name="t">秒数</param>
        ///<param name="type">0：转换后带秒，1:转换后不带秒</param>
        ///<returns>几天几小时几分几秒</returns>
        public static string ParseTimeSeconds(int t, int type = 0)
        {
            string r = "";
            int day, hour, minute, second;
            if (t >= 86400) //天,
            {
                day = Convert.ToInt16(t / 86400);
                hour = Convert.ToInt16((t % 86400) / 3600);
                minute = Convert.ToInt16((t % 86400 % 3600) / 60);
                second = Convert.ToInt16(t % 86400 % 3600 % 60);
                if (type == 0)
                    r = day + ("D") + hour + ("H") + minute + ("M") + second + ("S");
                else
                    r = day + ("D") + hour + ("H") + minute + ("M");

            }
            else if (t >= 3600)//时,
            {
                hour = Convert.ToInt16(t / 3600);
                minute = Convert.ToInt16((t % 3600) / 60);
                second = Convert.ToInt16(t % 3600 % 60);
                if (type == 0)
                    r = hour + ("H") + minute + ("M") + second + ("S");
                else
                    r = hour + ("H") + minute + ("M");
            }
            else if (t >= 60)//分
            {
                minute = Convert.ToInt16(t / 60);
                second = Convert.ToInt16(t % 60);
                r = minute + ("M") + second + ("S");
            }
            else
            {
                second = Convert.ToInt16(t);
                r = second + ("S");
            }
            return r;
        }

    }
}