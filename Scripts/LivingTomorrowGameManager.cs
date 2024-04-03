using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using static UnityEngine.GraphicsBuffer;

namespace LivingTomorrow.CMSApi
{
    public class LivingTomorrowGameManager : Singleton<LivingTomorrowGameManager>
    {
        public static UnityEvent<string, int, int> OnLoadProgressUpdateEvent = new();
        public static UnityEvent<XRConfig> OnLoadingDone = new();
        [HideInInspector]
        public bool postConfigTemplate = false;
        [HideInInspector]
        public bool localeIsset = false;
        [HideInInspector]
        public XRConfig XRConfig;

        private string _currentLanguage = "nl";
        private string _error = null;
        private string cleanId = null;
        [HideInInspector]
        public UnityEvent OnLanguageChanged;

        public List<ConfigParamStringId> ConfigParamsString
        {
            get
            {
                return gameConfig?.GlobalStringParameters?.ToList();
            }
        }
        public List<ConfigParamBoolId> ConfigParamsBool
        {
            get
            {
                return gameConfig?.GlobalBoolParameters?.ToList();
            }
        }
        public List<ConfigParamIntId> ConfigParamsInt
        {
            get
            {
                return gameConfig?.GlobalIntParameters?.ToList();
            }
        }

        public GameConfig gameConfig;

        public DemoSettings demoSettings;

        [HideInInspector]
        public bool HardRebootExeOnReset = false;
        private void Start()
        {
            if (Instance != this)
            {
                Debug.Log("CMS API | LivingTomorrowGameManager | NEW INSTANCE, DON't CONTINUE");
                return;
            }

            var error = false;
            if (gameConfig == null)
            {
                error = true;
                Debug.LogError("CMS API | LivingTomorrowGameManager | Game Config is not assigned!");
            }
            if (demoSettings == null)
            {
                error = true;
                Debug.LogError("CMS API | LivingTomorrowGameManager | Demo Settings is not assigned!");
            }
            if (string.IsNullOrEmpty(gameConfig.version))
            {
                error = true;
                Debug.LogError("CMS API | LivingTomorrowGameManager | Game Config has no version!");
            }
            if (string.IsNullOrEmpty(gameConfig.version))
            {
                error = true;
                Debug.LogError("CMS API | LivingTomorrowGameManager | Game Config has no version!");
            }
            if (gameConfig.SceneConfigs == null || gameConfig.SceneConfigs.Length == 0)
            {
                error = true;
                Debug.LogError("CMS API | LivingTomorrowGameManager | Scene Configs missing. Please add at least one scene config.");
            }
            if (gameConfig.SceneConfigs.Contains(null))
            {
                error = true;
                Debug.LogError("CMS API | LivingTomorrowGameManager | One or more scene config has not been assigned!");
            }
            if (!demoSettings.standAloneMode && (string.IsNullOrEmpty(demoSettings.TourBackendURL) || string.IsNullOrEmpty(demoSettings.CmsURL) || string.IsNullOrEmpty(demoSettings.DemoID)))
            {
                error = true;
                Debug.LogError("CMS API | LivingTomorrowGameManager | Demo settings was incorrectly configured. One or more required fields are missing!");
            }
            if (error)
            {
                Debug.LogError("CMS API | LivingTomorrowGameManager | Incorrect configuration. Unable to continue. ");
                return;
            }

            TrapNextPreviousWSCommand = false;

            Utils.DelayedCall(1f, () =>
            {
                CreateDefaultXRConfig();

                if (demoSettings.standAloneMode)
                {
                    StartCoroutine(StartSimulatedLoadingProcedure());
                }
                else
                {
                    if (!DeviceInfo.Instance.deviceNameIsSet)
                    {
                        Debug.Log("CMS API | LivingTomorrowGameManager | Get Device Info");
                        OnLoadProgressUpdateEvent.Invoke("CMS API | LivingTomorrowGameManager | Get Device Info", 0, 3);
                        //WebSocketManager.SendStatusUpdate(new StatusUpdateMessage(0, 0, StatusUpdateMessage.LogLevel.Info, "Game Manager | Get Device Info", StatusUpdateMessage.StatusEnum.Busy, _error, 3, 3));
                        DeviceInfo.Instance.OnSuccesEvent.AddListener(OnGotDeviceInfo);
                        DeviceInfo.Instance.OnFailEvent.AddListener(OnFailedToGetDeviceInfo);
                        DeviceInfo.Instance.GetDeviceInfoNow();
                    }
                    else
                    {
                        StartCoroutine(GetWebSocketURI());
                    }
                }
            });

            WebSocketManager.Instance.OnRestart.AddListener(ResetGame);

        }

        private IEnumerator StartSimulatedLoadingProcedure()
        {
            OnLoadProgressUpdateEvent.Invoke("CMS API | LivingTomorrowGameManager | Getting Device Info", 0, 4);
            yield return new WaitForSeconds(1);
            OnLoadProgressUpdateEvent.Invoke("CMS API | LivingTomorrowGameManager | Got Device Info", 1, 4);
            yield return new WaitForSeconds(1);
            OnLoadProgressUpdateEvent.Invoke("Getting WebSocketURI from API", 1, 4);
            yield return new WaitForSeconds(1);
            OnLoadProgressUpdateEvent.Invoke("CMS API | Websocket Manager | Socket Open.", 2, 4);
            yield return new WaitForSeconds(1);
            OnLoadProgressUpdateEvent.Invoke("CMS API | LivingTomorrowGameManager | Get Configuration", 3, 4);
            yield return new WaitForSeconds(1);
            OnLoadProgressUpdateEvent.Invoke("CMS API | LivingTomorrowGameManager | Received Configuration", 4, 4);
            yield return new WaitForSeconds(1);
            OnLoadingDone.Invoke(XRConfig);
        }

        private void OnDestroy()
        {
            WebSocketManager.Instance.OnRestart.RemoveListener(ResetGame);
        }

        private void OnFailedToGetDeviceInfo(string errorMsg)
        {
            //WebSocketManager.SendStatusUpdate(new StatusUpdateMessage(0, 0, StatusUpdateMessage.LogLevel.Error, "Game Manager | Failed Getting Device Info", StatusUpdateMessage.StatusEnum.Busy, _error, 4, 5));

            OnLoadProgressUpdateEvent.Invoke("CMS API | LivingTomorrowGameManager | Failed Getting Device Info, trying again...", 1, 4);
            Debug.LogWarning("CMS API | LivingTomorrowGameManager | Could not get device: " + errorMsg);

            Utils.DelayedCall(4, DeviceInfo.Instance.GetDeviceInfoNow);
        }

        private void OnGotDeviceInfo(string deviceName)
        {
            Debug.Log("CMS API | LivingTomorrowGameManager | OnGotDeviceInfo");
            OnLoadProgressUpdateEvent.Invoke("CMS API | LivingTomorrowGameManager | Got Device Info", 1, 4);
            DeviceInfo.Instance.OnSuccesEvent.RemoveListener(OnGotDeviceInfo);
            DeviceInfo.Instance.OnFailEvent.RemoveListener(OnFailedToGetDeviceInfo);

            //WebSocketManager.SendStatusUpdate(new StatusUpdateMessage(0, 0, StatusUpdateMessage.LogLevel.Info, "Game Manager | Got Device Info", StatusUpdateMessage.StatusEnum.Busy, _error, 4, 5));

            StartCoroutine(GetWebSocketURI());
        }

        protected void CreateDefaultXRConfig()
        {
            XRConfig = new XRConfig();

            string[] eventNames = gameConfig.ScenarioEvents;
            if (eventNames != null)
            {
                XRConfig.scenarioEvents = eventNames;
            }

            // ADD GLOBAL CONFIG HERE
            string[] languages = Enum.GetNames(typeof(Consts.Languages));
            XRConfig.addGlobalConfig("locale", new ConfigParamString("Language", "auto", string.Join(',', languages)));

            // parsing extra global configs from inspector
            foreach (var configParam in ConfigParamsString)
            {
                configParam.config.type = "string";
                XRConfig.addGlobalConfig(configParam.id, configParam.config);
            }
            foreach (var configParam in ConfigParamsBool)
            {
                configParam.config.type = "bool";
                XRConfig.addGlobalConfig(configParam.id, configParam.config);
            }
            foreach (var configParam in ConfigParamsInt)
            {
                configParam.config.type = "integer";
                XRConfig.addGlobalConfig(configParam.id, configParam.config);
            }

            if (gameConfig?.SceneConfigs != null && gameConfig.SceneConfigs.Length > 0)
            {
                foreach (var sceneConfig in gameConfig.SceneConfigs)
                {
                    var scene = sceneConfig.ToLT_Scene(sceneConfig.Index);
                    if (scene != null)
                    {
                        XRConfig.addScene(scene.name, scene);
                    }
                }
            }
        }

        IEnumerator GetWebSocketURI()
        {
            OnLoadProgressUpdateEvent.Invoke("Getting WebSocketURI from API", 1, 4);
            bool _success = false;
            while (_success == false)//Retry until successful
            {
                using var _uwr = UnityWebRequest.Get(Consts.GetDemoMetaDataURI);
                var _cert = new CertHandlerForceAcceptAll();
                _uwr.certificateHandler = _cert;
                _uwr.SetRequestHeader("Accept", "application/json; charset=UTF-8");
                _uwr.SetRequestHeader("Content-Type", "application/json; charset=UTF-8");
                Debug.Log("CMS API | LivingTomorrowGameManager | GetWebSocketURI: sending request to " + Consts.GetDemoMetaDataURI);
                //Request and wait for the response
                yield return _uwr.SendWebRequest();
                switch (_uwr.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                        Debug.LogError("CMS API | LivingTomorrowGameManager | GetWebSocketURI Connection Error: " + _uwr.error);
                        OnLoadProgressUpdateEvent.Invoke("GetWebSocketURI Connection Error: " + _uwr.error, 1, 4);
                        yield return new WaitForSeconds(10);
                        break;
                    case UnityWebRequest.Result.DataProcessingError:
                        Debug.LogError("CMS API | LivingTomorrowGameManager | GetWebSocketURI Data Processing Error: " + _uwr.error);
                        OnLoadProgressUpdateEvent.Invoke("GetWebSocketURI Data Processing Error: " + _uwr.error, 1, 4);
                        yield return new WaitForSeconds(10);
                        break;
                    case UnityWebRequest.Result.ProtocolError:
                        Debug.LogError("CMS API | LivingTomorrowGameManager | GetWebSocketURI Protocol Error: " + _uwr.error);
                        OnLoadProgressUpdateEvent.Invoke("GetWebSocketURI Protocol Error: " + _uwr.error, 1, 4);
                        yield return new WaitForSeconds(10);
                        break;
                    case UnityWebRequest.Result.Success:
                        _success = true;
                        Debug.Log("CMS API | LivingTomorrowGameManager | GetWebSocketURI received data: " + _uwr.downloadHandler.text);
                        if (_uwr.downloadHandler.text == "")
                        {
                            Debug.LogError("CMS API | LivingTomorrowGameManager | Did not receive metaData from API, can't start SocketConnection");
                            OnLoadProgressUpdateEvent.Invoke("GetWebSocketURI Protocol Error: Did not receive metaData from API, can't start SocketConnection", 1, 4);
                        }
                        else
                        {
                            // Find the closest version 
                            var demoMetaData = JsonConvert.DeserializeObject<GetDemoMetaDataClass>(_uwr.downloadHandler.text);
                            var validUris = demoMetaData.websockets.FindAll((v1) =>
                            {
                                var metaVersion = new Version(v1.minVersion);
                                var myVersion = new Version(gameConfig.version);
                                return metaVersion <= myVersion;
                            });
                            validUris.Sort((v1, v2) =>
                            {
                                var version1 = new Version(v1.minVersion);
                                var version2 = new Version(v2.minVersion);
                                return version2.CompareTo(version1);
                            });

                            // todo!! if validUris is 0 show correct notification


                            // Connect the websocket with the received IP
                            cleanId = (SystemInfo.deviceUniqueIdentifier + Consts.DemoId).Replace("-", "");
                            WebSocketManager.WebSocketsURI = validUris[0].wsUrl;
                            WebSocketManager.Instance.OnWebSocketConnectingEvent.AddListener(HandleWebSockerConnecting);
                            WebSocketManager.Instance.OnWebSocketOpenedEvent.AddListener(OnWebSocketOpened);
                            WebSocketManager.Instance.OnWebSocketErrorEvent.AddListener(OnWebSocketError);
                            WebSocketManager.Instance.OnClientDataUpdate.AddListener(HandleClientDataUpdate);
                            WebSocketManager.Connect();
                        }

                        break;
                }
                _cert?.Dispose();
            }
        }

        private void HandleClientDataUpdate(CommandMessage commandMessage)
        {
            var clientUpdateData = JsonConvert.DeserializeObject<MinifiedClientData>(commandMessage?.data);

            Debug.Log($"CMS API | LivingTomorrowGameManager | Received client data update: {JsonConvert.SerializeObject(clientUpdateData, Formatting.Indented)}");

        }

        private void OnWebSocketError(string msg)
        {
            WebSocketManager.Instance.OnWebSocketErrorEvent.RemoveListener(OnWebSocketError);
            OnLoadProgressUpdateEvent.Invoke("Websocket Manager | Socket Error: " + "'" + msg + "' | Reconnecting in 5 seconds...", 1, 4);
        }

        private void HandleWebSockerConnecting()
        {
            WebSocketManager.Instance.OnWebSocketErrorEvent.AddListener(OnWebSocketError);
            OnLoadProgressUpdateEvent.Invoke("Websocket Manager | Connecting to websocket at " + WebSocketManager.WebSocketsURI + " , with client id = " + $"{Consts.CurrentAuthType}-" + cleanId + " !", 1, 4);
        }


        private void OnWebSocketOpened()
        {
            WebSocketManager.Instance.OnWebSocketOpenedEvent.RemoveListener(OnWebSocketOpened);
            OnLoadProgressUpdateEvent.Invoke("Websocket Manager | Socket Open.", 2, 4);

            HandleConfiguration();
        }


        private void HandleConfiguration()
        {
            foreach (MediaItem _MI in MediaManager.Instance.MediaItems.Values)
            {
                ((LT_Scene)XRConfig.scenes[_MI.sceneid]).addMediaItem(_MI.uid, (MediaItem)MediaManager.Instance.MediaItems[_MI.sceneid + _MI.uid]);
            }

            if (Application.isEditor && Application.isPlaying && SceneManager.GetActiveScene().buildIndex == 0 && postConfigTemplate)
            {
                StartCoroutine(PostConfiguration());
            }
            else
            {
                StartCoroutine(GetConfiguration());
            }
        }

        IEnumerator PostConfiguration()
        {
            //Debug.Log("Post config " + JsonConvert.SerializeObject(GameConfig));

            string _postdata = JsonConvert.SerializeObject(new PostConfigurationClass(XRConfig, gameConfig.version));
            Debug.Log("Post _postdata " + _postdata);

            byte[] _bytes = System.Text.Encoding.UTF8.GetBytes(_postdata);
            using (var _uwr = UnityWebRequest.Put(Consts.PostGameConfigURI, _bytes))
            {
                var _cert = new CertHandlerForceAcceptAll();
                _uwr.certificateHandler = _cert;
                _uwr.method = "POST";
                _uwr.SetRequestHeader("Accept", "application/json; charset=UTF-8");
                _uwr.SetRequestHeader("Content-Type", "application/json; charset=UTF-8");
                Debug.Log("CMS API | LivingTomorrowGameManager | PostConfiguration: sending request to " + Consts.PostGameConfigURI + " data = " + _postdata);
                //Request and wait for the response
                yield return _uwr.SendWebRequest();
                switch (_uwr.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                        Debug.LogError("CMS API | LivingTomorrowGameManager | PostConfiguration Connection Error: " + _uwr.error);
                        break;
                    case UnityWebRequest.Result.DataProcessingError:
                        Debug.LogError("CMS API | LivingTomorrowGameManager | PostConfiguration Data Processing Error: " + _uwr.error);
                        break;
                    case UnityWebRequest.Result.ProtocolError:
                        Debug.LogError("CMS API | LivingTomorrowGameManager | PostConfiguration Protocol Error: " + _uwr.error);
                        break;
                    case UnityWebRequest.Result.Success:
                        Debug.Log("CMS API | LivingTomorrowGameManager | PostConfiguration received data: " + _uwr.downloadHandler.text);
                        break;
                }
                _cert?.Dispose();
            }
            yield return StartCoroutine(GetConfiguration());
        }

        IEnumerator GetConfiguration()
        {
            yield return new WaitForSeconds(1);

            bool _success = false;
            while (_success == false)//Retry until successful
            {
                WebSocketManager.SendStatusUpdate(new StatusUpdateMessage(0, 0, StatusUpdateMessage.LogLevel.Info, "Game Manager | Get Configuration", StatusUpdateMessage.StatusEnum.Busy, _error, 3, 4));

                using var _uwr = UnityWebRequest.Get(Consts.GetGameConfigURI + Consts.DemoId + "/" + gameConfig.version);
                var _cert = new CertHandlerForceAcceptAll();
                _uwr.certificateHandler = _cert;
                _uwr.SetRequestHeader("Accept", "application/json; charset=UTF-8");
                _uwr.SetRequestHeader("Content-Type", "application/json; charset=UTF-8");
                Debug.Log("CMS API | LivingTomorrowGameManager | GetConfiguration: sending request to " + Consts.GetGameConfigURI + Consts.DemoId + "/" + gameConfig.version);
                //Request and wait for the response
                yield return _uwr.SendWebRequest();
                switch (_uwr.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                        Debug.LogError("CMS API | LivingTomorrowGameManager | GetConfiguration Connection Error: " + _uwr.error);
                        _error = "Cannot get configuration (connection)";
                        yield return new WaitForSeconds(10);
                        break;
                    case UnityWebRequest.Result.DataProcessingError:
                        Debug.LogError("CMS API | LivingTomorrowGameManager | GetConfiguration Data Processing Error: " + _uwr.error);
                        _error = "Cannot get configuration (data processing)";
                        yield return new WaitForSeconds(10);
                        break;
                    case UnityWebRequest.Result.ProtocolError:
                        Debug.LogError("CMS API | LivingTomorrowGameManager | GetConfiguration Protocol Error: " + _uwr.error);
                        _error = "Cannot get configuration (protocol)";
                        yield return new WaitForSeconds(10);
                        break;
                    case UnityWebRequest.Result.Success:
                        Debug.Log("CMS API | LivingTomorrowGameManager | GetConfiguration received data: " + _uwr.downloadHandler.text);
                        var _gcc = JsonConvert.DeserializeObject<GetConfigurationClass>(_uwr.downloadHandler.text);
                        if (_gcc?.gameVersion == gameConfig.version)
                        {
                            _error = null;
                            if (_gcc.group?.localeId != null)
                                Language = _gcc.group?.localeId.ToLower();
                            var _config = JsonConvert.DeserializeObject<Config>(_gcc.configJson);
                            var _newXRConfig = _config.xrConfig;
                            _newXRConfig.DeserializeHashtables();
                            _success = true;
                            OnGotServerConfiguration(_newXRConfig, _gcc.gameVersion, _gcc.configVersion);
                            break;
                        }
                        else
                        {
                            if (_gcc == null)
                            {
                                // todo better error
                                Debug.LogError("CMS API | LivingTomorrowGameManager | GetConfiguration Error: Configuration returned is empty string");
                                _error = "Empty configuration returned";
                            }
                            else
                            {
                                Debug.LogError("CMS API | LivingTomorrowGameManager | GetConfiguration Error: The game version is incorrect, (game = " + gameConfig.version + ", config = " + _gcc.gameVersion + ")");
                                _error = "Incorrect game version";
                            }
                        }

                        break;
                }
                _cert?.Dispose();

                if (_error != null)
                {
                    WebSocketManager.SendStatusUpdate(new StatusUpdateMessage(0, 0, StatusUpdateMessage.LogLevel.Error, "Game Manager | " + _error, StatusUpdateMessage.StatusEnum.Busy, _error, 0, 1));
                }

                yield return new WaitForSeconds(2);
            }
        }

        private void OnGotServerConfiguration(XRConfig newGameConfig, string gameVersion, uint configVersion)
        {
            WebSocketManager.SendStatusUpdate(new StatusUpdateMessage(0, 0, StatusUpdateMessage.LogLevel.Info, "Game Manager | Received Configuration", StatusUpdateMessage.StatusEnum.Busy, _error, 4, 4));

            XRConfig = newGameConfig;
            if (DeviceInfo.Instance != null)
            {
                DeviceInfo.Instance.GameVersion = gameVersion;
                DeviceInfo.Instance.ConfigVersion = configVersion;

                WebSocketManager.TrySendingDeviceStatus();
            }

            string _languagesetting = getConfigString("locale").value;
            if (_languagesetting != "auto")
                Language = _languagesetting;

            //Set the "active" and "useCutscene" flag of each scene based on its config
            foreach (string _sceneid in XRConfig.scenes.Keys)
            {
                var _Sc = (LT_Scene)XRConfig.scenes[_sceneid];
                var _act = (ConfigParamBool)_Sc.sceneConfig["active"];
                if (_act != null)
                    _Sc.active = _act.value;
                if (_Sc.sceneConfig.ContainsKey("useCutscene"))
                {
                    var _uC = (ConfigParamBool)_Sc.sceneConfig["useCutscene"];
                    if (_uC != null)
                    {
                        _Sc.useCutscene = _uC.value;
                    }
                }
            }

            OnLoadingDone.Invoke(XRConfig);
        }

        public void ResetGame()
        {
            if (demoSettings.standAloneMode)
            {
                StartCoroutine(StartSimulatedLoadingProcedure());
            }
            else
            {
                StartCoroutine(GetConfiguration());
            }
        }

        #region getconfigs
        internal ConfigParamBool getConfigBool(string _configid) => (ConfigParamBool)XRConfig.globalConfig[_configid];
        internal ConfigParamInt getConfigInt(string _configid) => (ConfigParamInt)XRConfig.globalConfig[_configid];
        internal ConfigParamString getConfigString(string _configid) => (ConfigParamString)XRConfig.globalConfig[_configid];
        internal ConfigParamBool getSceneConfigBool(string _sceneid, string _configid) => (ConfigParamBool)((LT_Scene)XRConfig.scenes[_sceneid]).sceneConfig[_configid];
        internal ConfigParamInt getSceneConfigInt(string _sceneid, string _configid) => (ConfigParamInt)((LT_Scene)XRConfig.scenes[_sceneid]).sceneConfig[_configid];
        internal ConfigParamString getSceneConfigString(string _sceneid, string _configid) => (ConfigParamString)((LT_Scene)XRConfig.scenes[_sceneid]).sceneConfig[_configid];

        internal static void GetSceneConfigFloat(string _sceneid, string _configid, out float? value) => value = ((ConfigParamInt)((LT_Scene)Instance.XRConfig.scenes[_sceneid])?.sceneConfig[_configid])?.value;
        internal static void getSceneConfigString(string _sceneid, string _configid, out string value) => value = ((ConfigParamString)((LT_Scene)Instance.XRConfig.scenes[_sceneid])?.sceneConfig[_configid])?.value;
        internal static void getSceneConfigBool(string _sceneid, string _configid, out bool? value) => value = ((ConfigParamBool)((LT_Scene)Instance.XRConfig.scenes[_sceneid])?.sceneConfig[_configid])?.value;

        /// <summary>
        /// Retrieve the value of a global string parameter.
        /// </summary>
        /// <param name="id">The id as defined in the game config.</param>
        /// <returns></returns>
        public string GetStringParam(string id)
        {
            var result = getConfigString(id)?.value;
            if (result == null)
            {
                Debug.LogError($"CMS API | LivingTomorrowGameManager | Can't find game config string param id: {id}");
            }
            return result;
        }

        /// <summary>
        /// Retrieve the value of a bool string parameter.
        /// </summary>
        /// <param name="id">The id as defined in the game config.</param>
        /// <returns></returns>
        public bool? GetBoolParam(string id)
        {
            var result = getConfigBool(id)?.value;
            if (result == null)
            {
                Debug.LogError($"CMS API | LivingTomorrowGameManager | Can't find game config bool param id: {id}");
            }
            return result;
        }

        /// <summary>
        /// Retrieve the value of a global int parameter.
        /// </summary>
        /// <param name="id">The id as defined in the game config.</param>
        /// <returns></returns>
        public int? GetIntParam(string id)
        {
            var result = getConfigInt(id)?.value;
            if (result == null)
            {
                Debug.LogError($"CMS API | LivingTomorrowGameManager | Can't find game config int param id: {id}");
            }
            return result;
        }

        #endregion getconfigs
        public string Language
        {
            get => _currentLanguage;
            set
            {
                localeIsset = true;
                _currentLanguage = value;
                Debug.Log("CMS API | LivingTomorrowGameManager | Game Manager | Locale " + value + " set");
                OnLanguageChanged.Invoke();
            }
        }

        public Vector3 PlayerCamOffsetPosition { get; set; }
        public Vector3 PlayerCamOffsetRotation { get; set; }
        public bool TrapNextPreviousWSCommand { get; set; }//Some scenes treat the "next" command differently, they should not skip scenes, but media items instead!
    }
#if UNITY_EDITOR
    [CustomEditor(typeof(LivingTomorrowGameManager))]
    public class LivingTomorrowGameManagerInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            // Get a reference to the target script
            LivingTomorrowGameManager manager = (LivingTomorrowGameManager)target;

            DrawDefaultInspector();

            if (manager.gameConfig == null)
            {
                EditorGUILayout.HelpBox("Game Config is not assigned! This component may not work as intended.", MessageType.Error);
            }

            if (manager.demoSettings == null)
            {
                EditorGUILayout.HelpBox("Demo Settings is not assigned! This component may not work as intended.", MessageType.Error);
            }

        }
    }
#endif
}

