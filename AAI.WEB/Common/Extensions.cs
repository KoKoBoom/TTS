using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace AAI.WEB
{
    public static class Extensions
    {
        #region DateTime时间格式转换为Unix时间戳格式
        /// <summary>
        /// DateTime时间格式转换为Unix时间戳格式
        /// </summary>
        /// <param name=”time”></param>
        /// <returns></returns>
        public static int ToUnixDate(this DateTime dateTime)
        {
            System.DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1));
            return (int)(dateTime - startTime).TotalSeconds;
        }
        #endregion

        #region 
        /// <summary>
        /// HMACSHA1 加密 
        /// </summary>
        /// <param name="mk">原文</param>
        /// <param name="secret">密钥</param>
        /// <returns></returns>
        public static string ToHMACSHA1(this string mk, string secret)
        {
            HMACSHA1 hmacsha1 = new HMACSHA1();
            hmacsha1.Key = Encoding.UTF8.GetBytes(secret);
            byte[] dataBuffer = Encoding.UTF8.GetBytes(mk);
            byte[] hashBytes = hmacsha1.ComputeHash(dataBuffer);
            return Convert.ToBase64String(hashBytes);
        }
        #endregion

    }
}