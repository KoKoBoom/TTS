using Baidu.Aip.Speech;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace AAI.WEB.Controllers
{
    /// <summary>
    /// 语音识别
    /// </summary>
    public class SpeechRecognitionController : Controller
    {
        private readonly Asr _asrClient;
        private readonly Tts _ttsClient;

        public SpeechRecognitionController()
        {
            _asrClient = new Asr("FDMQIcuMyqMUuBPZVi9WHhYZ", "f5GmDPs1t34unCekiP5L0yEGQv7UG6qk");
            _ttsClient = new Tts("FDMQIcuMyqMUuBPZVi9WHhYZ", "f5GmDPs1t34unCekiP5L0yEGQv7UG6qk");
        }

        // 识别本地文件   
        public void AsrData()
        {
            var data = System.IO.File.ReadAllBytes(@"C:\Users\Admin\Source\Repos\TTS\AAI.WEB\App_LocalResources\2017年8月8日 16_49_17.wav");
            var result = _asrClient.Recognize(data, "wav", 8000);
            Response.Write(result);
        }
    }
}