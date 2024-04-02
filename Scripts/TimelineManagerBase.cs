using System;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LivingTomorrow.CMSApi
{
    public class TimelineManagerBase : MonoBehaviour
    {
        public SceneConfig sceneConfig;
        protected Hashtable _MediaAssets;
        protected string _SceneName = "";
        protected int _SceneIndex = 0;
        protected string _error = null;
        protected int _currentsection = 1;
        protected bool _currentIdleState = false;
        protected long _lastProgress = 1;
        protected long _lastMaxProgress = 1;
        protected StatusUpdateMessage.StatusEnum? _lastSentStatusMessage = null;

        protected virtual void OnEnable()
        {
            if (sceneConfig == null)
            {
                Debug.LogError("CMS API | TimelineManagerBase | OnEnable: Scene Config is not assigned. Unable to continue. Disabling script.");
                enabled = false;
                return;
            }
            var scenes = LivingTomorrowGameManager.Instance?.gameConfig?.SceneConfigs;
            if (scenes == null)
            {
                Debug.LogError("CMS API | TimelineManagerBase | OnEnable: Scene Configs is not assigned. Unable to continue. Disabling script.");
                enabled = false;
                return;
            }
            _SceneIndex = sceneConfig.Index;
            _SceneName = sceneConfig.sceneName;
        }

        protected virtual void Start()
        {
            WebSocketManager.OnWebSocketCommandReceivedEvent.AddListener(OnReceivedWebsocketCommand);
            WebSocketManager.OnWebSocketReconnectedEvent.AddListener(OnWebsocketReconnected);
        }

        protected virtual void OnDestroy()
        {
            WebSocketManager.OnWebSocketCommandReceivedEvent?.RemoveListener(OnReceivedWebsocketCommand);
            WebSocketManager.OnWebSocketReconnectedEvent?.RemoveListener(OnWebsocketReconnected);
        }

        public void OnReceivedWebsocketCommand(CommandMessage _cmd)
        {
            switch (_cmd.command)
            {
                case "play":
                    PlayCommand();
                    break;
                case "next":
                    NextCommand();
                    break;
                case "previous":
                    PreviousCommand();
                    break;
                case "restart":
                    ResetGame();
                    break;
            }
        }

        protected virtual void ResetGame()
        {

        }

        protected virtual void PlayCommand()
        {

        }
        protected virtual void NextCommand()
        {

        }
        protected virtual void PreviousCommand()
        {

        }

        protected virtual void OnWebsocketReconnected()
        {
            if(_lastSentStatusMessage == null)
            {
                return;
            }
            if (_error != null)
            {
                WebSocketManager.SendStatusUpdate(new StatusUpdateMessage(_SceneIndex, _currentsection, StatusUpdateMessage.LogLevel.Info, "TimelineManager | Status Update", (StatusUpdateMessage.StatusEnum)_lastSentStatusMessage, _error, _lastProgress, _lastMaxProgress));
            }
            else
            {
                WebSocketManager.SendStatusUpdate(new StatusUpdateMessage(_SceneIndex, _currentsection, StatusUpdateMessage.LogLevel.Error, _error, (StatusUpdateMessage.StatusEnum)_lastSentStatusMessage, _error, _lastProgress, _lastMaxProgress));
            }
        }

        protected internal void ws_status(long _progress, long _maxprogress)
        {
            _lastProgress = _progress;
            _lastMaxProgress = _maxprogress;

            if (!WebSocketManager.hasInstance)
                return;

            if (_error == null)
            {
                WebSocketManager.SendStatusUpdate(new StatusUpdateMessage(_SceneIndex, _currentsection, StatusUpdateMessage.LogLevel.Info, "TimelineManager | Status Update", calcStatusEnum(), _error, _progress, _maxprogress));
            }
            else
            {
                WebSocketManager.SendStatusUpdate(new StatusUpdateMessage(_SceneIndex, _currentsection, StatusUpdateMessage.LogLevel.Error, _error, calcStatusEnum(), _error, _progress, _maxprogress));
            }
        }

        protected internal virtual void ws_status(long _progress, long _maxprogress, StatusUpdateMessage.StatusEnum status)
        {
            _lastSentStatusMessage = status;
            _lastProgress = _progress;
            _lastMaxProgress = _maxprogress;

            if (!WebSocketManager.hasInstance)
                return;

            if (_error == null)
            {
                WebSocketManager.SendStatusUpdate(new StatusUpdateMessage(_SceneIndex, _currentsection, StatusUpdateMessage.LogLevel.Info, "TimelineManager | Status Update", status, _error, _progress, _maxprogress));
            }
            else
            {
                WebSocketManager.SendStatusUpdate(new StatusUpdateMessage(_SceneIndex, _currentsection, StatusUpdateMessage.LogLevel.Error, _error, status, _error, _progress, _maxprogress));
            }
        }
        protected internal virtual void ws_status(int section, long _progress, long _maxprogress, StatusUpdateMessage.StatusEnum status)
        {
            _currentsection = section;
            _lastSentStatusMessage = status;
            _lastProgress = _progress;
            _lastMaxProgress = _maxprogress;

            if (!WebSocketManager.hasInstance)
                return;

            if (_error == null)
            {
                WebSocketManager.SendStatusUpdate(new StatusUpdateMessage(_SceneIndex, _currentsection, StatusUpdateMessage.LogLevel.Info, "TimelineManager | Status Update", status, _error, _progress, _maxprogress));
            }
            else
            {
                WebSocketManager.SendStatusUpdate(new StatusUpdateMessage(_SceneIndex, _currentsection, StatusUpdateMessage.LogLevel.Error, _error, status, _error, _progress, _maxprogress));
            }
        }

        protected internal virtual void ws_scenarioEvent(string scenarioEvent)
        {
            WebSocketManager.SendScenarioEvent(new ScenarioEventMessage(scenarioEvent));
        }

        private StatusUpdateMessage.StatusEnum calcStatusEnum()
        {
            _lastSentStatusMessage = StatusUpdateMessage.StatusEnum.Busy;
            if (_currentIdleState)
            {
                _lastSentStatusMessage = StatusUpdateMessage.StatusEnum.Idle;
            }
            return (StatusUpdateMessage.StatusEnum)_lastSentStatusMessage;
        }

        /// <summary>
        /// Sends a status update to the Living Tomorrow backend.
        /// </summary>
        /// <param name="sequenceName">Sequence name as defined in the SequenceConfig inside the attached SceneConfig.</param>
        /// <param name="progress">The progress of the current sequence element.</param>
        /// <param name="maxprogress">The maximum progress of the current sequence element.</param>
        public void SendSequenceStatus(string sequenceName, long progress, long maxprogress)
        {
            var sequence = sceneConfig.Sequences.FirstOrDefault(x => x.sequenceName == sequenceName);
            if (sequence == null)
            {
                Debug.LogError($"CMS API | TimelineManagerBase | SendSequenceStatus : Failed to send sequence status. Sequence with name {sequenceName} not found.");
                return;
            }

            ws_status(sequence.Index, progress, maxprogress, sequence.status);
        }

        /// <summary>
        /// Sends a status update to the Living Tomorrow backend.
        /// </summary>
        /// <param name="sequenceName">Sequence name as defined in the SequenceConfig inside the attached SceneConfig.</param>
        public void SendSequenceStatus(string sequenceName)
        {
            SendSequenceStatus(sequenceName, 0, 1);
        }

        /// <summary>
        /// Sends a status update to the Living Tomorrow backend.
        /// </summary>
        /// <param name="index">Index of the SequenceConfig inside the attached SceneConfig.</param>
        /// <param name="progress">The progress of the current sequence element.</param>
        /// <param name="maxprogress">The maximum progress of the current sequence element.</param>
        public void SendSequenceStatus(int index, long progress, long maxprogress)
        {
            var sequence = sceneConfig.Sequences.ElementAtOrDefault(index);
            if (sequence == null)
            {
                Debug.LogError($"CMS API | TimelineManagerBase | SendSequenceStatus: Sequence with index {index} not found.");
                return;
            }

            ws_status(sequence.Index, progress, maxprogress, sequence.status);
        }

        /// <summary>
        /// Sends a status update to the Living Tomorrow backend.
        /// </summary>
        /// <param name="index">Index of the SequenceConfig inside the attached SceneConfig.</param>
        public void SendSequenceStatus(int index)
        {
            SendSequenceStatus(index, 0, 1);
        }

        /// <summary>
        /// Retrieve the value of a scene specific string parameter.
        /// </summary>
        /// <param name="id">The id as defined in the scene config.</param>
        /// <returns></returns>
        public string GetStringParam(string id)
        {
            LivingTomorrowGameManager.getSceneConfigString(_SceneName, id, out string result);
            if (result == null)
            {
                Debug.LogError($"CMS API | TimelineManagerBase | GetStringParam: Can't find string param of scene config {_SceneName} id: {id}");
            }
            return result;
        }

        /// <summary>
        /// Retrieve the value of a scene specific bool parameter.
        /// </summary>
        /// <param name="id">The id as defined in the scene config.</param>
        /// <returns></returns>
        public bool? GetBoolParam(string id)
        {
            LivingTomorrowGameManager.getSceneConfigBool(_SceneName, id, out bool? result);
            if (result == null)
            {
                Debug.LogError($"CMS API | TimelineManagerBase | GetBoolParam: Can't find bool param of scene config {_SceneName} id: {id}");
            }
            return result;
        }

        /// <summary>
        /// Retrieve the value of a scene specific int parameter.
        /// </summary>
        /// <param name="id">The id as defined in the scene config.</param>
        /// <returns></returns>
        public int? GetIntParam(string id)
        {
            LivingTomorrowGameManager.GetSceneConfigFloat(_SceneName, id, out float? result);
            if (result == null)
            {
                Debug.LogError($"CMS API | TimelineManagerBase | GetIntParam | Can't find int param of scene config {_SceneName} id: {id}");
                return null;
            }
            return (int)result;
        }

        /// <summary>
        /// Publishes a scenario event to trigger an action during a demo at Living Tomorrow.
        /// </summary>
        /// <param name="scenarioEvent">The scenario event as defined in the game configuration.</param>
        public void PublishScenarioEvent(string scenarioEvent)
        {
            var result = LivingTomorrowGameManager.Instance.gameConfig?.ScenarioEvents?.Contains(scenarioEvent);
            if (result == null)
            {
                Debug.LogError($"CMS API | TimelineManagerBase | PublishScenarioEvent: Unknown scenario event: '{scenarioEvent}'.");
            }
            ws_scenarioEvent(scenarioEvent);
        }
    }
#if UNITY_EDITOR
    [CustomEditor(typeof(TimelineManagerBase), true)]
    public class TimelineManagerBaseInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            TimelineManagerBase manager = target as TimelineManagerBase;

            DrawDefaultInspector();
            if (manager.sceneConfig == null)
            {
                EditorGUILayout.HelpBox("Scene Config is not assigned! This component may not work as intended.", MessageType.Error);
            }
        }
    }
#endif
}