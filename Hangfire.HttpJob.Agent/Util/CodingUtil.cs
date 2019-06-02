using System;

namespace Hangfire.HttpJob.Agent.Util
{
    internal static class CodingUtil
    {

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
                    r = day + ("day") + hour + ("hour") + minute + ("minute") + second + ("second");
                else
                    r = day + ("day") + hour + ("hour") + minute + ("minute");

            }
            else if (t >= 3600)//时,
            {
                hour = Convert.ToInt16(t / 3600);
                minute = Convert.ToInt16((t % 3600) / 60);
                second = Convert.ToInt16(t % 3600 % 60);
                if (type == 0)
                    r = hour + ("hour") + minute + ("minute") + second + ("second");
                else
                    r = hour + ("hour") + minute + ("minute");
            }
            else if (t >= 60)//分
            {
                minute = Convert.ToInt16(t / 60);
                second = Convert.ToInt16(t % 60);
                r = minute + ("minute") + second + ("second");
            }
            else
            {
                second = Convert.ToInt16(t);
                r = second + ("second");
            }
            return r;
        }

    }
}