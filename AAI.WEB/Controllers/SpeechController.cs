using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace AAI.WEB.Controllers
{
    public class SpeechController : Controller
    {
        // GET: Speech
        public ActionResult Index()
        {
            return View();
        }

        #region 语音合成
        /// <summary>
        /// 语音合成
        /// </summary>
        /// <param name="appid"></param>
        /// <param name="secretid"></param>
        /// <param name="secretkey"></param>
        /// <param name="text"></param>
        /// <param name="mp3AbsolutePath"></param>
        /// <param name="mp3Filename"></param>
        /// <returns></returns>
        public string TextToSpeech(string appid, string secretid, string secretkey, string text, string mp3AbsolutePath = "", string mp3Filename = "")
        {
            //边界符
            var boundary = "-------------------" + DateTime.Now.Ticks.ToString("x");

            var url = GetFullUrl(appid, secretid);
            //创建请求
            var webRequest = (HttpWebRequest)WebRequest.Create(url);
            //设置请求头
            SetHeaders(webRequest, url, secretkey, boundary);
            //将数据写入Body并获取写入后的数据
            var strBody = GetRequestBody(boundary, text);
            //将Body写入到请求流中
            WriteRequestStreamFormBody(webRequest, strBody);
            //获取结果
            string responseResult = GetResponseResult(webRequest);
            var resJson = JObject.Parse(responseResult);
            if (resJson.Value<string>("code").Equals("0"))
            {
                //转换为mp3保存
                Base64ToAudio(resJson.Value<string>("speech"), mp3AbsolutePath, mp3Filename);
            }
            else
            {
                //转换失败，具体查看错误码
            }
            return responseResult;
        }
        #endregion

        #region 使用密钥（secretkey）对url进行加密，得到签名
        /// <summary>
        /// 使用密钥（secretkey）对url进行加密，得到签名
        /// </summary>
        /// <param name="urlSorted">排序后的url地址</param>
        /// <param name="secretkey">密钥</param>
        /// <returns></returns>
        private string GetAuthorization(string urlSorted, string secretkey)
        {
            return ("POST" + urlSorted.Substring(8)).ToHMACSHA1(secretkey);
        }
        #endregion

        #region 获取排序后的完整路径地址
        /// <summary>
        /// 获取排序后的完整路径地址
        /// </summary>
        /// <param name="appid"></param>
        /// <param name="secretid"></param>
        /// <returns></returns>
        private string GetFullUrl(string appid, string secretid)
        {

            /* 
             * 返回值示例：
             * 
             * https://aai.qcloud.com/tts/v1/20170111
             * ?expired=1484113583
             * &nonce=1675199141
             * &person=0
             * &projectid=0
             * &secretid=AKIDlfdHxN0ntSVt4KPH0xXWnGl21UUFNoO5
             * &speech_format=mp3
             * &speed=0
             * &sub_service_type=0
             * &timestamp=1484109983
             * &volume=3
             * 
             */

            StringBuilder url = new StringBuilder("https://aai.qcloud.com/tts/v1/" + appid + "?");
            SortedDictionary<string, object> dic = new SortedDictionary<string, object>();
            dic.Add("projectid", 0);            //腾讯云项目 ID，不填为默认项目，即0，总长度不超过1024字节
            dic.Add("sub_service_type", 0);     //子服务类型。0：短文本实时合成。目前只支持短文本实时合成
            dic.Add("speech_format", "mp3");    //合成语音格式，目前支持MP3格式
            dic.Add("volume", 3);               //音量，默认为5，取值范围为0-10
            dic.Add("person", 0);               //发音人，目前仅支持0，女声
            dic.Add("speed", 0);                //语速，默认值为0，取值范围为-40到40，1表示加速到原来的1.1倍，-1为相对于正常语速放慢1.1倍
            dic.Add("secretid", secretid);      //官网云API密钥中获得的SecretId
            dic.Add("timestamp", DateTime.Now.ToUnixDate());    //当前时间戳，是一个符合 UNIX Epoch 时间戳规范的数值，单位为秒
            dic.Add("expired", DateTime.Now.AddMinutes(5).ToUnixDate());    //签名的有效期，是一个符合 UNIX Epoch 时间戳规范的数值，单位为秒；expired 必须大于 timestamp 且 expired - timestamp 小于90天
            dic.Add("nonce", new Random().Next(10000000, 99999999));    //随机正整数。用户需自行生成，最长10位

            foreach (var item in dic)
            {
                url.Append(item.Key);
                url.Append("=");
                url.Append(item.Value);
                url.Append("&");
            }
            return url.Remove(url.Length - 1, 1).ToString();  //移除末尾的 &
        }
        #endregion

        #region 设置Header
        /// <summary>
        /// 设置Header
        /// </summary>
        /// <param name="webRequest"></param>
        /// <param name="url"></param>
        /// <param name="secretkey"></param>
        /// <param name="boundary"></param>
        private void SetHeaders(HttpWebRequest webRequest, string url, string secretkey, string boundary)
        {
            //设置属性
            webRequest.Host = "aai.qcloud.com";
            webRequest.Method = "POST";
            webRequest.Timeout = 30000;
            webRequest.ContentType = "multipart/form-data; boundary=" + boundary;
            webRequest.Headers.Add("Authorization", GetAuthorization(url, secretkey));
        }
        #endregion

        #region 获取设置文字内容后的Body
        /// <summary>
        /// 获取设置文字内容后的Body
        /// </summary>
        /// <param name="boundary"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        private string GetRequestBody(string boundary, string text)
        {
            //设置需要转语音的文字内容
            string strBody = GetTextBody(boundary, text);

            //在这里可以添加其他的Body内容
            //strBody += GetOtherBody();

            //设置结尾
            strBody += "--" + boundary + "--\r\n";
            return strBody;
        }
        #endregion

        #region 获取设置文字内容后的Body段
        /// <summary>
        /// 设置文字内容
        /// </summary>
        /// <param name="boundary"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        private string GetTextBody(string boundary, string text)
        {
            return string.Format("--" + boundary +
                                   "\r\nContent-Disposition: form-data; name=\"text\"; filename=\"file1.txt\"" +
                                   "\r\nContent-Type: text/plain" +
                                   "\r\n\r\n{0}\r\n", text);
        }
        #endregion

        #region 将Body写入流
        /// <summary>
        /// 将Body写入流
        /// </summary>
        /// <param name="memoryStream"></param>
        /// <param name="body"></param>
        void WirteStreamFromBody(MemoryStream memoryStream, string body)
        {
            var strBodyBytes = Encoding.UTF8.GetBytes(body);
            memoryStream.Write(strBodyBytes, 0, strBodyBytes.Length);
        }
        #endregion

        #region 将body流写入到请求流中
        /// <summary>
        /// 将body流写入到请求流中
        /// </summary>
        /// <param name="webRequest"></param>
        /// <param name="memStream"></param>
        private static void WriteRequestStreamFormBody(HttpWebRequest webRequest, string body)
        {
            var strBodyBytes = Encoding.UTF8.GetBytes(body);
            using (var requestStream = webRequest.GetRequestStream())
            {
                requestStream.Write(strBodyBytes, 0, strBodyBytes.Length);
            }
        }
        #endregion

        #region 获取响应结果
        /// <summary>
        /// 获取响应内容
        /// </summary>
        /// <param name="webRequest"></param>
        /// <returns></returns>
        private static string GetResponseResult(HttpWebRequest webRequest)
        {
            string responseResult;
            using (var httpWebResponse = (HttpWebResponse)webRequest.GetResponse())
            {
                using (var httpStreamReader = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.GetEncoding("utf-8")))
                {
                    responseResult = httpStreamReader.ReadToEnd();
                }
            }
            webRequest.Abort();
            return responseResult;
        }
        #endregion

        #region base64转换mp3
        /// <summary>
        /// base64转换mp3
        /// </summary>
        /// <param name="strBase64"></param>
        /// <param name="absolutePath"></param>
        /// <param name="filename"></param>
        public void Base64ToAudio(string strBase64, string absolutePath = "", string filename = "")
        {
            try
            {
                absolutePath = (absolutePath != null && !"".Equals(absolutePath.Trim())) ? absolutePath : Server.MapPath("~/audio/");
                filename = (filename != null && !"".Equals(filename.Trim())) ? filename : DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".mp3";
                byte[] audioByteArray = Convert.FromBase64String(strBase64);
                using (var mp3File = System.IO.File.Create(absolutePath + filename, audioByteArray.Length))
                {
                    mp3File.Write(audioByteArray, 0, audioByteArray.Length);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        public void SendPost()
        {
            var appid = "1253324443";
            var secretid = "AKIDKZeR7gZRdifS7dq87wFUgstnWApKKx8H";
            var secretkey = "DF8qfU8jDXhZM0WzJXcqwtgLtHVn4GNN";

            TextToSpeech(appid, secretid, secretkey, "Hello World！");

            //StringBuilder url = new StringBuilder("https://aai.qcloud.com/tts/v1/" + appid + "?");
            //url.Append("expired=" + DateTime.Now.AddHours(1).ToUnixDate());
            //url.Append("&nonce=" + new Random().Next(100000000, 999999999));
            //url.Append("&person=0");
            //url.Append("&projectid=0");
            //url.Append("&secretid=" + secretid);
            //url.Append("&speech_format=mp3");
            //url.Append("&speed=0");
            //url.Append("&sub_service_type=0");
            //url.Append("&timestamp=" + DateTime.Now.ToUnixDate());
            //url.Append("&volume=3");

            //string responseContent;
            //var memStream = new MemoryStream();
            //var webRequest = (HttpWebRequest)WebRequest.Create(url.ToString());

            ////边界符
            //var boundary = "-------------------" + DateTime.Now.Ticks.ToString("x");

            ////设置属性
            //webRequest.Host = "aai.qcloud.com";
            //webRequest.Method = "POST";
            //webRequest.Timeout = 30000;
            //webRequest.ContentType = "multipart/form-data; boundary=" + boundary;
            //webRequest.Headers.Add("Authorization", ("POST" + url.ToString().Substring(8)).ToHMACSHA1(secretkey));

            //#region 写入需要转换的文字
            //var strBody = string.Format("--" + boundary +
            //                       "\r\nContent-Disposition: form-data; name=\"text\"; filename=\"file1.txt\"" +
            //                       "\r\nContent-Type: text/plain" +
            //                       "\r\n\r\n{0}\r\n", "你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好");
            //#endregion

            //strBody += "--" + boundary + "--\r\n";

            //var strBodyBytes = Encoding.UTF8.GetBytes(strBody);
            //using (var requestStream = webRequest.GetRequestStream())
            //{
            //    requestStream.Write(strBodyBytes, 0, strBodyBytes.Length);
            //}
            //var httpWebResponse = (HttpWebResponse)webRequest.GetResponse();
            //using (var httpStreamReader = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.GetEncoding("utf-8")))
            //{
            //    responseContent = httpStreamReader.ReadToEnd();
            //}
            //httpWebResponse.Close();
            //webRequest.Abort();
            //var resJson = JObject.Parse(responseContent);
            //Base64ToAudio(resJson.Value<string>("speech"), "D://Audio/");
        }
    }
}