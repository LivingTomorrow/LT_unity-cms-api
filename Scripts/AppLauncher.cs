using Newtonsoft.Json;
using System;
using UnityEngine;

namespace LivingTomorrow.CMSApi
{
    public class AppLauncher : MonoBehaviour
    {
        private bool needsResetOnFocus = false;
        // Start is called before the first frame update
        void Start()
        {
            WebSocketManager.Instance.OnWebSocketCommandReceivedEvent.AddListener(HandleCommandMessage);
        }

        private void OnDestroy()
        {

            WebSocketManager.Instance.OnWebSocketCommandReceivedEvent.RemoveListener(HandleCommandMessage);
        }

        private void OnApplicationFocus(bool focus)
        {
#if UNITY_IOS
            //this needs to be here because apple does not allow the app to close using code
            if(focus && needsResetOnFocus)
            {
                needsResetOnFocus = false;
                LivingTomorrowGameManager.Instance.ResetGame();
                WebSocketManager.Connect();
            }
#endif
        }

        void HandleCommandMessage(CommandMessage commandMessage)
        {
            if (commandMessage != null && commandMessage.command == "launch")
            {
#if UNITY_ANDROID
            HandleLaunchCommandAndroid(commandMessage);
#endif

#if UNITY_STANDALONE_WIN
                HandleLaunchCommandWindows(commandMessage);
#endif

#if UNITY_IOS
                HandleLaunchCommandIOS(commandMessage);
#endif
            }
        }

        void HandleLaunchCommandAndroid(CommandMessage commandMessage)
        {
            if (String.IsNullOrEmpty(commandMessage.data))
            {
                return;
            }
            var launchData = JsonConvert.DeserializeObject<LaunchData>(commandMessage.data);
            var activityName = launchData?.Android;
            if (activityName == null)
            {
                Debug.LogWarning("CMS API | App Launcher | HandleLaunchCommandAndroid: Cannot launch android app without activity name.");
                return;
            }
            var currentAppId = Application.identifier;
            Debug.Log($"CMS API | App Launcher | HandleLaunchCommandAndroid: Current application identifier: {currentAppId}");
            Debug.Log($"CMS API | App Launcher | HandleLaunchCommandAndroid: Trying to launch activity: {activityName}");

            if (launchData.BundleId != null && launchData.BundleId == Application.identifier)
            {
                Debug.Log($"CMS API | App Launcher | HandleLaunchCommandAndroid: Application with bundle identifier {activityName} already started.");
                return;
            }
            try
            {
                AndroidJavaObject toLaunch = new AndroidJavaObject("android.content.Intent");
                AndroidJavaObject componentName = new AndroidJavaObject("android.content.ComponentName", activityName, "com.unity3d.player.UnityPlayerActivity");
                toLaunch.Call<AndroidJavaObject>("setComponent", componentName);
                AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                currentActivity.Call("startActivity", toLaunch);
                unityPlayer.Dispose();
                toLaunch.Dispose();
                WebSocketManager.CloseConnection();
                currentActivity.Call("finish");
            }
            catch (Exception)
            {
                var _error = $"CMS API | App Launcher | HandleLaunchCommandAndroid: Failed to launch {activityName}. Likely not installed";
                Debug.LogError($"CMS API | App Launcher | HandleLaunchCommandAndroid: Failed to launch {activityName}. Likely not installed");
                WebSocketManager.SendStatusUpdate(new StatusUpdateMessage(0, 0, StatusUpdateMessage.LogLevel.Error, _error, StatusUpdateMessage.StatusEnum.Busy, _error, 0, 1));
            }
        }
        void HandleLaunchCommandWindows(CommandMessage commandMessage)
        {
            if (String.IsNullOrEmpty(commandMessage.data))
            {
                return;
            }
            var launchData = JsonConvert.DeserializeObject<LaunchData>(commandMessage.data);
            var winAppDir = launchData?.Windows;
            if (winAppDir == null)
            {
                Debug.LogWarning("CMS API | App Launcher | HandleLaunchCommandWindows: Cannot launch windows app without directory.");
                return;
            }

            if (launchData.BundleId != null && launchData.BundleId == Application.identifier)
            {
                Debug.Log($"CMS API | App Launcher | HandleLaunchCommandWindows: Application with bundle identifier {launchData.BundleId} already started.");
                return;
            }

            try
            {
                var process = new System.Diagnostics.Process();
                process.StartInfo.WorkingDirectory = "C:\\AppLauncher";
                process.StartInfo.FileName = winAppDir;
                process.StartInfo.Arguments = "-screen-width 800 -screen-height 600 -screen-fullscreen 0";
                process.Start();
                WebSocketManager.CloseConnection();
                Utils.DelayedCall(0.5f, () => Application.Quit());
            }
            catch (Exception)
            {
                var _error = $"CMS API | App Launcher | HandleLaunchCommandWindows: Failed to launch {winAppDir}. Likely not installed";
                Debug.LogError($"CMS API App Launcher | HandleLaunchCommandWindows: Failed to launch {winAppDir}. Likely not installed");
                WebSocketManager.SendStatusUpdate(new StatusUpdateMessage(0, 0, StatusUpdateMessage.LogLevel.Error, _error, StatusUpdateMessage.StatusEnum.Busy, _error, 0, 1));
            }
        }
        void HandleLaunchCommandIOS(CommandMessage commandMessage)
        {
            if (String.IsNullOrEmpty(commandMessage.data))
            {
                return;
            }
            var launchData = JsonConvert.DeserializeObject<LaunchData>(commandMessage.data);
            var urlScheme = launchData?.IOS;
            if (urlScheme == null)
            {
                Debug.LogWarning("CMS API | App Launcher | HandleLaunchCommandIOS: Cannot launch iOS app without url scheme.");
                return;
            }

            if (launchData.BundleId != null && launchData.BundleId == Application.identifier)
            {
                Debug.Log($"CMS API | App Launcher | HandleLaunchCommandIOS: Application with bundle identifier {launchData.BundleId} already started.");
                return;
            }

            try
            {
                needsResetOnFocus = true;
                Application.OpenURL(launchData.IOS);
                WebSocketManager.SoftDisconnect();
            }
            catch (Exception)
            {
                needsResetOnFocus = false;
                var _error = $"CMS API | App Launcher | HandleLaunchCommandIOS: Failed to launch {urlScheme}. Likely not installed";
                Debug.LogError($"CMS API | App Launcher | HandleLaunchCommandIOS: Failed to launch {urlScheme}. Likely not installed");
                WebSocketManager.SendStatusUpdate(new StatusUpdateMessage(0, 0, StatusUpdateMessage.LogLevel.Error, _error, StatusUpdateMessage.StatusEnum.Busy, _error, 0, 1));
            }
        }
    }

    public class LaunchData
    {
#nullable enable
        [JsonProperty("bundleId")]
        public string? BundleId { get; set; }
        [JsonProperty("android")]
        public string? Android { get; set; }
        [JsonProperty("windows")]
        public string? Windows { get; set; }
        [JsonProperty("ios")]
        public string? IOS { get; set; }
#nullable disable
    }
}
