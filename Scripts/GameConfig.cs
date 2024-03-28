using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LivingTomorrow.CMSApi
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "LivingTomorrow/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Tooltip("The version of the configuration. If this configuration or any of the attached scene or sequence configs is modified, the version must be updated. Otherwise, the configuration will not be updated on the Living Tomorrow backend.")]
        public string version = "0.1.0";

        [Tooltip("The scene configurations to be used with this application.")]
        [SerializeField]
        private SceneConfig[] sceneConfigs;

        public SceneConfig[] SceneConfigs
        {
            get
            {
                if (sceneConfigs == null)
                {
                    return sceneConfigs;
                }
                for (int i = 0; i < sceneConfigs.Length; i++)
                {
                    if (sceneConfigs[i] == null)
                    {
                        break;
                    }
                    sceneConfigs[i].Index = i + 1;
                }
                return sceneConfigs;
            }
        }

        [Tooltip("A list of scenario events. These events can be triggered to make something happen during a demo at Living Tomorrow.")]
        public string[] ScenarioEvents;

        [Tooltip("Global parameters that can be used to allow Living Tomorrow to customize certain string variables in the application. This will be displayed as a dropdown menu, so the options field is required.")]
        public ConfigParamStringId[] GlobalStringParameters;
        [Tooltip("Global parameters that can be used to allow Living Tomorrow to customize certain bool variables in the application.")]
        public ConfigParamBoolId[] GlobalBoolParameters;
        [Tooltip("Global parameters that can be used to allow Living Tomorrow to customize certain int variables in the application.")]
        public ConfigParamIntId[] GlobalIntParameters;
    }
    [CustomEditor(typeof(GameConfig))]
    public class GameConfigInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            GameConfig gameConfig = (GameConfig)target;
            DrawDefaultInspector();
            if (string.IsNullOrEmpty(gameConfig.version))
            {
                EditorGUILayout.HelpBox("'Version' is not set. This is required to publish a configuration.", MessageType.Error);
            }
            if (gameConfig.SceneConfigs == null || gameConfig.SceneConfigs.Length == 0)
            {
                EditorGUILayout.HelpBox("Scene Configs missing. Please add at least one scene config.", MessageType.Error);
            }
            if (gameConfig.SceneConfigs?.Contains(null) == true)
            {
                EditorGUILayout.HelpBox("One or more scene config has not been assigned!", MessageType.Error);

            }
        }
        
    }
}

