using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LivingTomorrow.CMSApi
{
    [CreateAssetMenu(fileName = "SceneConfig", menuName = "LivingTomorrow/SceneConfig")]

    public class SceneConfig : ScriptableObject
    {
        internal int Index { get; set; }
        [Tooltip("The scene name can be used to retrieve configuration data.")]
        public string sceneName;
        public string displayName;
        [SerializeField]
        private SequenceConfig[] sequences;
        public SequenceConfig[] Sequences { get 
            {
                for (int i = 0; i < sequences.Length; i++)
                {
                    sequences[i].Index = i;
                }
                return sequences;
            } 
        }
        [SerializeField]
        [Tooltip("Global parameters that can be used to allow Living Tomorrow to customize certain string variables in the application. This will be displayed as a dropdown menu, so the options field is required.")]
        internal ConfigParamStringId[] StringParameters;
        [SerializeField]
        [Tooltip("Scene specific parameters that can be used to allow Living Tomorrow to customize certain bool variables in the scene.")]
        internal ConfigParamBoolId[] BoolParameters;
        [SerializeField]
        [Tooltip("Scene specific parameters that can be used to allow Living Tomorrow to customize certain int variables in the scene.")]
        internal ConfigParamIntId[] IntParameters;
        private Hashtable CreateSequenceElements()
        {
            var _ReturnValue = new Hashtable();
            int index = 0;
            foreach (var sq in sequences)
            {
                var sequenceElement = new LT_SequenceElement(index, sq.displayName, sq.status == StatusUpdateMessage.StatusEnum.Idle, false, null);
                _ReturnValue.Add(sq.sequenceName, sequenceElement);
                index++;
            }
            return _ReturnValue;
        }
        public LT_Scene ToLT_Scene(int index)
        {
            var scene = new LT_Scene(index, sceneName, displayName);
            scene.sequenceElements = CreateSequenceElements();
            foreach (var item in StringParameters)
            {
                item.config.type = "string";
                scene.addSceneConfig(item.id, item.config);
            }
            foreach (var item in BoolParameters)
            {
                item.config.type = "bool";
                scene.addSceneConfig(item.id, item.config);
            }
            foreach (var item in IntParameters)
            {
                item.config.type = "integer";
                scene.addSceneConfig(item.id, item.config);
            }
            return scene;
        }
    }
    [CustomEditor(typeof(SceneConfig))]
    public class SceneConfigInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            SceneConfig sceneConfig = (SceneConfig)target;
            DrawDefaultInspector();
            if (string.IsNullOrEmpty(sceneConfig.sceneName))
            {
                EditorGUILayout.HelpBox("'Scene Name' is not set. This is required to retrieve the scene configuration.", MessageType.Error);
            }
            if (string.IsNullOrEmpty(sceneConfig.displayName))
            {
                EditorGUILayout.HelpBox("'Display Name' is not set.", MessageType.Error);
            }
            if (sceneConfig.Sequences == null || sceneConfig.Sequences.Length == 0)
            {
                EditorGUILayout.HelpBox("Sequences missing. Please add at least one sequence config.", MessageType.Error);
            }
            if (sceneConfig.Sequences.Contains(null))
            {
                EditorGUILayout.HelpBox("One or more sequence config has not been assigned!", MessageType.Error);
            }
        }
    }
}

