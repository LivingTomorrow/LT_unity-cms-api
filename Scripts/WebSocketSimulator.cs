using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using static LivingTomorrow.CMSApi.StatusUpdateMessage;

namespace LivingTomorrow.CMSApi
{
    public class WebSocketSimulator : Singleton<WebSocketSimulator>
    {
        internal UnityEvent<SimulatedClientState> OnStateUpdate = new UnityEvent<SimulatedClientState>();
        public bool SimulateAutomaticCommands = false;
        public void SimulateCommandMessage(string command)
        {
            if (LivingTomorrowGameManager.Instance?.demoSettings?.standAloneMode == null || LivingTomorrowGameManager.Instance.demoSettings.standAloneMode == false)
            {
                Debug.LogError("CMS API | WebSocketSimulator | SimulateCommandMessage : Simulating websocket messages is only allowed in stand alone mode.");
                return;
            }
            CommandMessage msg = new CommandMessage();
            msg.command = command;
            byte[] _bytes = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(msg));
            WebSocketManager.OnWebSocketMessage(_bytes);
        }
        public SimulatedClientState state = new SimulatedClientState();
        public void Play()
        {
            SimulateCommandMessage("play");
        }
        public void Restart()
        {
            SimulateCommandMessage("restart");
        }

        public void Next()
        {
            SimulateCommandMessage("next");
        }

        public void Previous()
        {
            SimulateCommandMessage("previous");
        }


        private void Start()
        {
            if (WebSocketManager.hasInstance && WebSocketManager.Instance != null)
            {
                WebSocketManager.Instance?.OnWebSocketSatusSentEvent?.AddListener(HandleWebSocketStatusSent);
            }
        }

        private void OnDestroy()
        {
            if(WebSocketManager.hasInstance && WebSocketManager.Instance != null)
            {
                WebSocketManager.Instance?.OnWebSocketSatusSentEvent?.RemoveListener(HandleWebSocketStatusSent);
            }
        }

        private void HandleWebSocketStatusSent(StatusUpdateMessage message)
        {
            state.sceneIndex = message.sceneIndex;
            state.sequenceIndex = message.sequenceNumber;
            state.progress = message.progress;
            state.maxProgress = message.maxProgress;
            state.status = message.statusText;
            OnStateUpdate.Invoke(state);
            if (LivingTomorrowGameManager.Instance.demoSettings.standAloneMode == true && SimulateAutomaticCommands == true)
            {
                if(state.status == StatusEnum.Idle.ToString())
                {
                    Utils.DelayedCall(0.5f, () => SimulateCommandMessage("play"));
                }
                else if(state.status == StatusEnum.Ended.ToString())
                {
                    Utils.DelayedCall(0.5f, () => SimulateCommandMessage("restart"));
                }
                
            }
        }
    }
#if UNITY_EDITOR
    [CustomEditor(typeof(WebSocketSimulator))]
    public class WebSocketSimulatorInspector : Editor
    {
        private WebSocketSimulator sim;
        private void HandleStatusUpdate(SimulatedClientState status)
        {
            Repaint();
        }
        public override void OnInspectorGUI()
        {
            // Get a reference to the target script
            sim = (WebSocketSimulator)target;
            sim.OnStateUpdate?.RemoveListener(HandleStatusUpdate);
            sim.OnStateUpdate?.AddListener(HandleStatusUpdate);

            bool disabled = LivingTomorrowGameManager.hasInstance == false || LivingTomorrowGameManager.Instance == null || LivingTomorrowGameManager.Instance.demoSettings?.standAloneMode == false || !Application.isPlaying;

            if (disabled)
            {
                EditorGUILayout.HelpBox("The simulator commands can only be used when in stand alone mode when the application is running.", MessageType.Info);

            }

            // Display default inspector GUI
            DrawDefaultInspector();

            EditorGUI.BeginDisabledGroup(disabled);

            if (GUILayout.Button("Play / Continue"))
            {
                sim.Play();
            }
            if (GUILayout.Button("Restart"))
            {
                sim.Restart();
            }
            if (GUILayout.Button("Next"))
            {
                sim.Next();
            }
            if (GUILayout.Button("Previous"))
            {
                sim.Previous();
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(20);
            EditorGUILayout.LabelField("Status Information", EditorStyles.boldLabel);
            if (LivingTomorrowGameManager.hasInstance && LivingTomorrowGameManager.Instance != null && Application.isPlaying)
            {
                var sceneConfig = LivingTomorrowGameManager.Instance.gameConfig.SceneConfigs.FirstOrDefault(x => x.Index == sim.state.sceneIndex);
                var sequenceConfig = sceneConfig?.Sequences.FirstOrDefault(x => x.Index == sim.state.sequenceIndex);
                var sceneName = "";
                var sequenceName = "";
                if (sim.state.sceneIndex == 0)
                {
                    sceneName = "Initialization (internal scene configuration)";
                    sequenceName = "None";
                }
                else
                {
                    sceneName = sceneConfig?.displayName;
                    sequenceName = sequenceConfig?.displayName;
                }
                EditorGUILayout.LabelField("Scene:", sceneName);
                EditorGUILayout.LabelField("Sequence:", sequenceName);
                EditorGUILayout.LabelField("Status:", sim.state.status);
                if (sim.state.status == StatusEnum.Busy.ToString())
                {
                    EditorGUILayout.LabelField("Progress:", ((int)Math.Round((double)(100 * sim.state.progress) / sim.state.maxProgress)).ToString() + "%");
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Run the application to see simulated state information.", MessageType.Info);
            }
        }

        private void OnDestroy()
        {
            if(sim != null)
            {
                sim.OnStateUpdate?.RemoveListener(HandleStatusUpdate);
            }   
        }
    }
#endif
    public class SimulatedClientState
    {
        public int sceneIndex { get; set; }
        public int sequenceIndex { get; set; }
        public float progress { get; set; }
        public float maxProgress { get; set; }
        public string status { get; set; }

        public SimulatedClientState()
        {
            sceneIndex = 0;
            sequenceIndex = 0;
            progress = 0;
            maxProgress = 1;
            status = "Busy";
        }
    }
}

