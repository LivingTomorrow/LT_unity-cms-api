using NativeWebSocket;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using static LivingTomorrow.CMSApi.Consts;

namespace LivingTomorrow.CMSApi
{
    public class WebSocketManager : Singleton<WebSocketManager>
    {
        protected static WebSocket websocket;
        public static StatusUpdateMessage LatestStatusUpdate = null;
        public UnityEvent OnWebSocketConnectingEvent;
        public UnityEvent OnWebSocketOpenedEvent;
        public UnityEvent OnWebSocketClosedEvent;
        public static UnityEvent OnWebSocketReconnectedEvent = new();
        public UnityEvent<string> OnWebSocketErrorEvent;
        public static UnityEvent<CommandMessage> OnWebSocketCommandReceivedEvent = new();
        public static UnityEvent<StatusUpdateMessage> OnWebSocketSatusSentEvent = new();
        public UnityEvent<CommandMessage> OnSendConnectedClientsEvent;
        public UnityEvent<CommandMessage> OnReceivedError;
        public UnityEvent OnUsersAssigned;
        public UnityEvent OnUsersUnassigned;
        public UnityEvent<CommandMessage> OnClientDataUpdate;
        public UnityEvent OnRestart;

        public static string WebSocketsURI { get; set; }

        void Update()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            websocket?.DispatchMessageQueue();
#endif
        }



        async void SendWebSocketMessage()
        {
            if (websocket.State == WebSocketState.Open)
            {
                // Sending bytes
                await websocket.Send(new byte[] { 10, 20, 30 });

                // Sending plain text
                await websocket.SendText("plain text message");
            }
        }

        public static async void SendStatusUpdate(StatusUpdateMessage _msg)
        {
            string _data = JsonConvert.SerializeObject(_msg);
            var _cmd = new CommandMessage { command = "statusUpdate", data = _data };
            Debug.Log("CMS API | Websocket Manager | SendStatusUpdate : " + JsonConvert.SerializeObject(_cmd));
            byte[] _bytes = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(_cmd));
            if (websocket?.State == WebSocketState.Open)
                await websocket.Send(_bytes);
            LatestStatusUpdate = _msg;
            LivingTomorrowGameManager.OnLoadProgressUpdateEvent?.Invoke(_msg.logMsg, (int)_msg.progress, (int)_msg.maxProgress);
            OnWebSocketSatusSentEvent?.Invoke(_msg);
        }

        public static async void SendScenarioEvent(ScenarioEventMessage _msg)
        {
            // todo: pass prop with eventName and eventValue
            // use this to start a scenario in timeline manager. Only the master client should send these!

            string _data = JsonConvert.SerializeObject(_msg);
            var _cmd = new CommandMessage { command = "scenarioEvent", data = _data };
            Debug.Log("CMS API | Websocket Manager | Send Scenario Event: " + JsonConvert.SerializeObject(_cmd));
            byte[] _bytes = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(_cmd));
            if (websocket?.State == WebSocketState.Open)
                await websocket.Send(_bytes);
        }

        public static async void SendCommandMessage(CommandMessage _cmd)
        {
            Debug.Log("CMS API | Websocket Manager | Sending Command Message: " + JsonConvert.SerializeObject(_cmd));
            byte[] _bytes = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(_cmd));
            if (websocket?.State == WebSocketState.Open)
                await websocket.Send(_bytes);
        }

        public void OnDestroy()
        {
            //GameManager.Instance.ev_WSStatusUpdateMessage.RemoveListener(SendStatusUpdate);
        }

        private async void OnApplicationQuit()
        {
            if (websocket != null)
            {
                websocket.OnOpen -= OnWebSocketOpen;
                websocket.OnError -= OnWebSocketError;
                websocket.OnClose -= OnWebSocketClosed;
                websocket.OnMessage -= OnWebSocketMessage;
                await websocket.Close();
            }

            // Remove device battery event listeners
            if (DeviceInfo.Instance != null)
            {
                DeviceInfo.Instance.OnBatteryLevelChange.RemoveListener(TrySendingDeviceStatus);
                DeviceInfo.Instance.OnBatteryStatusChange.RemoveListener(TrySendingDeviceStatus);
            }
        }

        static IEnumerator Reconnect()
        {
            yield return new WaitForSeconds(5);
            Connect();
        }

        public static async void Connect() //Required to run async function from coroutine.
        {
            if (websocket != null && (websocket.State == WebSocketState.Open || websocket.State == WebSocketState.Connecting))
            {
                Debug.Log("CMS API | Websocket Manager | Socket already Connected, not attempting to reconnect.");
                return;
            }

            if (websocket != null)
            {
                // close previous connection
                Debug.Log("CMS API | Websocket Manager | Closing previous connection to websocket.");
                websocket.OnOpen -= OnWebSocketOpen;
                websocket.OnError -= OnWebSocketError;
                websocket.OnClose -= OnWebSocketClosed;
                websocket.OnMessage -= OnWebSocketMessage;

                await websocket.Close();
            }

            // Remove device battery event listeners
            if (DeviceInfo.Instance != null)
            {
                DeviceInfo.Instance.OnBatteryLevelChange.RemoveListener(TrySendingDeviceStatus);
                DeviceInfo.Instance.OnBatteryStatusChange.RemoveListener(TrySendingDeviceStatus);
            }

            string cleanId = (SystemInfo.deviceUniqueIdentifier + Consts.DemoId).Replace("-", "");
            Debug.Log("CMS API | Websocket Manager | Connecting to websocket at " + WebSocketsURI + " , with client id = " + "client-" + cleanId + " !");
            websocket = new WebSocket(WebSocketsURI, $"{Consts.CurrentAuthType}-" + cleanId);
            //websocket = new WebSocket("ws://wsfuturex.admin.livingtomorrow.com:1810", "client-" + cleanId);

            websocket.OnOpen += OnWebSocketOpen;
            websocket.OnError += OnWebSocketError;
            websocket.OnClose += OnWebSocketClosed;
            websocket.OnMessage += OnWebSocketMessage;

            // Add device battery event listeners
            if (DeviceInfo.Instance != null)
            {
                DeviceInfo.Instance.OnBatteryLevelChange.AddListener(TrySendingDeviceStatus);
                DeviceInfo.Instance.OnBatteryStatusChange.AddListener(TrySendingDeviceStatus);
            }

            Instance.OnWebSocketConnectingEvent.Invoke();
            await websocket.Connect();
        }

        public static void TrySendingDeviceStatus()
        {
            var deviceInfoInstance = DeviceInfo.Instance;
            var configVersionsData = new DeviceInfoStatusUpdateCommandData() { ConfigVersion = deviceInfoInstance.ConfigVersion, GameVersion = deviceInfoInstance.GameVersion, BatteryLevel = deviceInfoInstance.BatteryLevel, BatteryStatus = deviceInfoInstance.BatteryStatus };
            string configVersionsDataString = JsonConvert.SerializeObject(configVersionsData);

            SendCommandMessage(new CommandMessage() { command = "deviceInfoStatusUpdate", data = configVersionsDataString });
        }

        private static void HandleReceivedAuthMessage(string _message)
        {
            var Auth = JsonUtility.FromJson<WSMessage_auth>(_message);
            if (Auth?.Name?.Length > 0)
            {
                Debug.Log(string.Format("CMS API | Websocket Manager | Auth Message: Name= {0}, Id= {1}", Auth.Name, Auth.Id));
                if (OnWebSocketReconnectedEvent != null)
                {
                    // This should be true for every timelinemanager that inherits from TimelineManagerBase
                    OnWebSocketReconnectedEvent?.Invoke();
                }
                else if (LatestStatusUpdate != null)
                {
                    // This only occurs in timelinemanagers that don't inherit from TimelineManagerBase such as TimelineManagerIndustrialSite f.e.
                    SendStatusUpdate(LatestStatusUpdate);

                }

                // If GameVersion is not an empty string then the config was already fetched and should be posted again to WS
                if (DeviceInfo.Instance != null && DeviceInfo.Instance.GameVersion != string.Empty)
                {
                    TrySendingDeviceStatus();
                }
            }
        }

        internal static void OnWebSocketMessage(byte[] bytes)
        {
            string _message = System.Text.Encoding.UTF8.GetString(bytes);
            var _WSMessage = JsonUtility.FromJson<WSMessage>(_message);
            switch (_WSMessage._Type)
            {
                case WSMessage.eMessageType.auth:
                    HandleReceivedAuthMessage(_message);
                    break;
                default:
                    break;
            }
            var _cmd = JsonConvert.DeserializeObject<CommandMessage>(_message);
            if (_cmd.command?.Length > 0)
            {
                Debug.Log("CMS API | Websocket Manager | Command Message : " + _cmd.command);
                switch (_cmd.command)
                {
                    case "restart":
                        OnWebSocketCommandReceivedEvent?.Invoke(_cmd);
                        Instance.OnRestart.Invoke();
                        break;
                    case "error":
                        Instance.OnReceivedError.Invoke(_cmd);
                        break;
                    case "usersUnassigned":
                        Instance.OnUsersUnassigned.Invoke();
                        break;
                    case "sendConnectedClients":
                        Instance.OnSendConnectedClientsEvent.Invoke(_cmd);
                        break;
                    case "usersAssigned":
                        Instance.OnUsersAssigned.Invoke();
                        break;
                    case "clientDataUpdate":
                        Instance.OnClientDataUpdate.Invoke(_cmd);
                        break;
                    default:
                        OnWebSocketCommandReceivedEvent?.Invoke(_cmd);
                        break;
                }
            }
        }

        private static void OnWebSocketClosed(WebSocketCloseCode _closeCode)
        {
            Instance.OnWebSocketClosedEvent.Invoke();

            try
            {
                if (_closeCode != WebSocketCloseCode.Away && Instance != null && !m_ShuttingDown && Instance.gameObject.activeSelf)
                {
                    Debug.Log("CMS API | Websocket Manager | Connection closed, restarting connection after 5 seconds.");
                    Instance.StartCoroutine(Reconnect());
                }
                else
                {
                    Debug.LogWarning("CMS API | Websocket Manager | Connection closed, Game Manager destroyed.");
                }
            }
            catch (Exception e)
            {
                Debug.Log("CMS API | Websocket Manager | Connection closed, restart failed: " + e.Message);
            };
        }

        private static void OnWebSocketError(string errorMsg)
        {
            Debug.Log("CMS API | Websocket Manager | Error: " + errorMsg);
            Instance.OnWebSocketErrorEvent.Invoke(errorMsg);
        }

        private static void OnWebSocketOpen()
        {
            Debug.Log("CMS API | Websocket Manager | Connection open!");

            Instance.OnWebSocketOpenedEvent.Invoke();
        }

        public static async void SoftDisconnect()
        {
            await websocket.Close();
        }

        public static async void CloseConnection()
        {
            if (websocket != null)
            {
                websocket.OnOpen -= OnWebSocketOpen;
                websocket.OnError -= OnWebSocketError;
                websocket.OnClose -= OnWebSocketClosed;
                websocket.OnMessage -= OnWebSocketMessage;
                await websocket.Close();

                websocket = null;
            }

            // Remove device battery event listeners
            if (DeviceInfo.Instance != null)
            {
                DeviceInfo.Instance.OnBatteryLevelChange.RemoveListener(TrySendingDeviceStatus);
                DeviceInfo.Instance.OnBatteryStatusChange.RemoveListener(TrySendingDeviceStatus);
            }
        }
    }

    [Serializable]
    public class WSMessage
    {
        public enum eMessageType
        {
            auth = 0,
            keepAlive,
            clientStateChange,
            terminateClient,
            switchAutomaticMode
        }
        public eMessageType _Type;
    }

    [Serializable]
    public class WSMessage_auth : WSMessage
    {
        public string Id;
        public string Name;
    }
    [Serializable]
    public class CommandMessage
    {
        public string command { get; set; }
        public string data { get; set; }
    }

    [Serializable]
    public class DeviceInfoStatusUpdateCommandData
    {
        [JsonProperty("configVersion")]
        public uint ConfigVersion { get; set; }
        [JsonProperty("gameVersion")]
        public string GameVersion { get; set; }
        [JsonProperty("batteryLevel")]
        public double BatteryLevel { get; set; }
        [JsonProperty("batteryStatus")]
        public BatteryStatus BatteryStatus { get; set; }
    }

    [Serializable]
    public class StatusUpdateMessage
    {
        //public enum StatusEnum { Idle, Busy, Error };
        public enum StatusEnum { ForceIdle, Idle, Busy, Ended };
        public enum LogLevel { Info, Warning, Error };

        public int sequenceNumber { get; set; }
        public int sceneIndex { get; set; }
        public string logLevel { get; set; }
        public string logMsg { get; set; }
        public string statusText { get; set; }
        public string errorText { get; set; }
        public long progress { get; set; }
        public long maxProgress { get; set; }
        public int maxSceneOrderNo { get; set; }
        public StatusUpdateMessage(int _sceneIndex, int _sequenceNumber, LogLevel _logLevel, string _logMsg, StatusEnum _statusText, string _errorText, long _progress, long _maxProgress)
        {
            sceneIndex = _sceneIndex;
            sequenceNumber = _sequenceNumber;
            logLevel = _logLevel.ToString();
            logMsg = _logMsg;
            //statusText = (_errorText?.Length > 0) ? "Error" : _statusText.ToString();//Idle | Busy | Error
            statusText = _statusText.ToString(); //ForceIdle, Idle, Busy
            errorText = _errorText;
            progress = _progress;
            maxProgress = _maxProgress;

            if (LivingTomorrowGameManager.Instance.XRConfig.sceneOrderFromId.TryGetValue(_sceneIndex, out int order))
            {
                maxSceneOrderNo = order;
            }
        }
    }

    [Serializable]
    public class ScenarioEventMessage
    {
        public string scenarioEvent;
        public ScenarioEventMessage(string scenarioEvent) => this.scenarioEvent = scenarioEvent;
    }

    [Serializable]
    public class GetConnectedClientsData
    {
        public Client[] Clients { get; set; }
    }

    [Serializable]
    public class Client
    {
        public string Id { get; set; }
#nullable enable
        public User? User { get; set; }
#nullable disable
    }

    [Serializable]
    public class User
    {
        [JsonProperty("id")]
        public string Id { get; set; }
#nullable enable
        [JsonProperty("nickname")]
        public string? Nickname { get; set; }
        [JsonProperty("avatarUri")]
        public string? AvatarUri { get; set; }
#nullable disable
    }

    [Serializable]
    public class MinifiedClientData
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("userType")]
        public AuthType UserType { get; set; }
#nullable enable
        [JsonProperty("name")]
        public string? Name { get; set; }
        [JsonProperty("role")]
        public string? Role { get; set; }
        [JsonProperty("user")]
        public User? User { get; set; }
    }
}
