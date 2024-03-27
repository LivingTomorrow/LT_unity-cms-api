using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace LivingTomorrow.CMSApi
{

    [Serializable]
    public class GetDemoMetaDataClass
    {
        public List<WebSocketUris> websockets { get; set; }

    }

    [Serializable]
    public class WebSocketUris
    {
        public string minVersion { get; set; }
        public string wsUrl { get; set; }
        public string wssUrl { get; set; }
    }

    [Serializable]
    public class PostConfigurationClass
    {
        public string gameVersion { get; set; }
        public string timelineVersion { get; set; }
        public string configJson { get; set; }
        public string demoId { get; set; }
        public PostConfigurationClass(XRConfig _xrConfig, string _gameVersion)
        {
            Config config = new() { xrConfig = _xrConfig };
            gameVersion = _gameVersion;
            timelineVersion = Consts.TimelineVersion;
            demoId = Consts.DemoId;
            configJson = JsonConvert.SerializeObject(config);
        }
    }
    [Serializable]
    public class GetConfigurationClass
    {
        //public string id { get; set; }
        public string gameVersion { get; set; }
        public uint configVersion { get; set; }
        public string configJson { get; set; }

        public ConfigurationGroupClass group { get; set; }
        public class ConfigurationGroupClass
        {
            public string id { get; set; }
            public string name { get; set; }
            public string localeId { get; set; }
        }
    }
    public class CertHandlerForceAcceptAll : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData) => true;
    }
    [Serializable]
    public class PostDeviceInfoClass
    {
        public string headsetId = "";
        public string headsetName = "";
        public string demoId = "";
        public string userId = "";
        public string nickname = "";
        public PostDeviceInfoClass(string _headsetId, string _headsetName, string _demoId, string _userId, string _nickname)
        {
            headsetId = _headsetId;
            headsetName = _headsetName;
            userId = _userId;
            nickname = _nickname;
            demoId = _demoId;
        }
    }

    [Serializable]
    public class Config
    {
        public XRConfig xrConfig { get; set; }
        public JObject timelineConfig { get; set; }

        public Config() => timelineConfig = JObject.Parse(Consts.DefaultTimelineConfigString);

    }
}