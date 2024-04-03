using Newtonsoft.Json;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.Serialization;

namespace LivingTomorrow.CMSApi
{
    public class DeviceInfo : Singleton<DeviceInfo>
    {
        #region DeviceInfo Websocket Variables
        public string GameVersion { get; set; }
        public uint ConfigVersion { get; set; }
        public double BatteryLevel { get; set; }
        public BatteryStatus BatteryStatus { get; set; }
        #endregion
        #region DeviceInfo Websocket Variable ChangeEvents
        [HideInInspector]
        public UnityEvent OnBatteryLevelChange;
        [HideInInspector]
        public UnityEvent OnBatteryStatusChange;
        #endregion

        [HideInInspector]
        public bool deviceNameIsSet = false;

        private string _currentDeviceName;
        private string _error = null;

        [Tooltip("If checked, will simulate an OnFailEvent while in 'Stand Alone Mode'.")]
        public bool SimulateOnFail = false;

        public UnityEvent OnRequireNameInputEvent;

        public UnityEvent<string> OnFailEvent;

        public UnityEvent<string> OnSuccesEvent;

        [FormerlySerializedAs("OnStartPostHeadSetInfoEvent")]
        public UnityEvent OnStartPostDeviceInfoEvent;

        [HideInInspector]
        public bool ForceChooseName = false;

        public String DeviceName
        {
            get => _currentDeviceName;
            set
            {
                if (value?.Length > 0 && !deviceNameIsSet)
                {
                    _currentDeviceName = value;
                    deviceNameIsSet = true;
                    Debug.Log("CMS API | DeviceInfo | Device name set to " + value);

                    OnSuccesEvent?.Invoke(_currentDeviceName);
                }
            }
        }

        private void Awake()
        {
            BatteryLevel = Math.Round(SystemInfo.batteryLevel, 2);
            GameVersion = string.Empty;
            ConfigVersion = uint.MinValue;
            BatteryStatus = SystemInfo.batteryStatus;
        }

        private void Update()
        {
            double newBatteryLevel = Math.Round(SystemInfo.batteryLevel, 2);
            if (BatteryLevel != newBatteryLevel)
            {
                BatteryLevel = newBatteryLevel;
                OnBatteryLevelChange?.Invoke();
            }

            if (BatteryStatus != SystemInfo.batteryStatus)
            {
                BatteryStatus = SystemInfo.batteryStatus;
                OnBatteryStatusChange?.Invoke();
            }
        }

        public void OnFail()
        {
            Debug.LogWarning("CMS API | DeviceInfo | fail: " + _error);
            WebSocketManager.SendStatusUpdate(new StatusUpdateMessage(0, 0, StatusUpdateMessage.LogLevel.Error, _error, StatusUpdateMessage.StatusEnum.Busy, _error, 0, 1));
            OnFailEvent?.Invoke(_error);
        }

        public void GetDeviceInfoNow() => Instance.StartCoroutine(Instance.GetDeviceInfo());

        IEnumerator GetDeviceInfo()
        {
            bool _success = false;
            while (_success == false)//Retry until successful
            {
                string cleanId = (SystemInfo.deviceUniqueIdentifier + Consts.DemoId).Replace("-", "");
                using (var _uwr = UnityWebRequest.Get(Consts.GetDeviceInfoURI + cleanId))
                {
                    var _cert = new CertHandlerForceAcceptAll();
                    _uwr.certificateHandler = _cert;
                    _uwr.SetRequestHeader("Accept", "application/json; charset=UTF-8");
                    _uwr.SetRequestHeader("Content-Type", "application/json; charset=UTF-8");
                    Debug.Log("CMS API | DeviceInfo | GetDeviceInfo: sending request to " + Consts.GetDeviceInfoURI + cleanId);
                    //Request and wait for the response
                    yield return _uwr.SendWebRequest();
                    _cert?.Dispose();
                    if (ForceChooseName || _uwr.responseCode == 404)
                    {
                        Debug.Log("CMS API | DeviceInfo | GetDeviceInfo: Not found. Choose name In Game.");
                        OnRequireNameInputEvent.Invoke();
                        _success = true;
                    }
                    else
                    {
                        switch (_uwr.result)
                        {
                            case UnityWebRequest.Result.ConnectionError:
                                Debug.LogError("CMS API | DeviceInfo | GetDeviceInfo Connection Error: " + _uwr.error);
                                _error = "Cannot get device info (connection)";
                                OnFail();
                                break;
                            case UnityWebRequest.Result.DataProcessingError:
                                Debug.LogError("CMS API | DeviceInfo | GetDeviceInfo Data Processing Error: " + _uwr.error);
                                _error = "Cannot get device info (data processing)";
                                OnFail();
                                break;
                            case UnityWebRequest.Result.ProtocolError:
                                Debug.LogError("CMS API | DeviceInfo | GetDeviceInfo Protocol Error: " + _uwr.error);
                                _error = "Cannot get device info (protocol)";
                                OnFail();
                                break;
                            case UnityWebRequest.Result.Success:
                                _success = true;
                                _error = null;
                                Debug.Log("CMS API | DeviceInfo | GetDeviceInfo received data: " + _uwr.downloadHandler.text);
                                var _hsi = JsonConvert.DeserializeObject<PostDeviceInfoClass>(_uwr.downloadHandler.text);
                                if (_hsi.headsetName?.Length > 0)
                                {
                                    DeviceName = _hsi.headsetName;
                                }
                                else
                                {
                                    Debug.Log("CMS API | DeviceInfo | GetDeviceInfo deviceName is empty. Choose name In Game.");
                                    OnRequireNameInputEvent.Invoke();
                                }
                                break;
                        }
                    }
                }
                yield return new WaitForSeconds(10);
            }
        }

        public void SetDeviceNameInGame(string _name) => StartCoroutine(PostDeviceInfo(_name));
        IEnumerator PostDeviceInfo(string _devicename)
        {
            OnStartPostDeviceInfoEvent.Invoke();
            bool _success = false;
            while (_success == false)//Retry until successful
            {
                string cleanId = (SystemInfo.deviceUniqueIdentifier + Consts.DemoId).Replace("-", "");
                string _postdata = JsonConvert.SerializeObject(new PostDeviceInfoClass(cleanId, _devicename, Consts.DemoId, null, null));
                byte[] _bytes = System.Text.Encoding.UTF8.GetBytes(_postdata);
                using (var _uwr = UnityWebRequest.Put(Consts.PostDeviceInfoURI, _bytes))
                {
                    var _cert = new CertHandlerForceAcceptAll();
                    _uwr.certificateHandler = _cert;
                    _uwr.method = "POST";
                    _uwr.SetRequestHeader("Accept", "application/json; charset=UTF-8");
                    _uwr.SetRequestHeader("Content-Type", "application/json; charset=UTF-8");
                    Debug.Log("CMS API | DeviceInfo | PostDeviceInfo: sending request to " + Consts.PostDeviceInfoURI + " data = " + _postdata);
                    //Request and wait for the response
                    yield return _uwr.SendWebRequest();
                    _cert?.Dispose();
                    switch (_uwr.result)
                    {
                        case UnityWebRequest.Result.ConnectionError:
                            Debug.LogError("CMS API | DeviceInfo | PostDeviceInfo Connection Error: " + _uwr.error);
                            _error = "Cannot post device info (connection)";
                            OnFail();
                            break;
                        case UnityWebRequest.Result.DataProcessingError:
                            Debug.LogError("CMS API | DeviceInfo | PostDeviceInfo Data Processing Error: " + _uwr.error);
                            _error = "Cannot post device info (data processing)";
                            OnFail();
                            break;
                        case UnityWebRequest.Result.ProtocolError:
                            Debug.LogError("CMS API | DeviceInfo | PostDeviceInfo Protocol Error: " + _uwr.error);
                            _error = "Cannot post device info (protocol)";
                            OnFail();
                            break;
                        case UnityWebRequest.Result.Success:
                            _success = true;
                            _error = null;
                            Debug.Log("CMS API | DeviceInfo | PostDeviceInfo received data: " + _uwr.downloadHandler.text);
                            var _hsi = JsonConvert.DeserializeObject<PostDeviceInfoClass>(_uwr.downloadHandler.text);
                            DeviceName = _hsi.headsetName;
                            break;
                    }
                }
                yield return new WaitForSeconds(10);
            }
        }

    }
}