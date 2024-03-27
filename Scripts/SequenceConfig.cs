using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static LivingTomorrow.CMSApi.StatusUpdateMessage;

namespace LivingTomorrow.CMSApi
{
    [CreateAssetMenu(fileName = "SequenceConfig", menuName = "LivingTomorrow/SequenceConfig")]
    public class SequenceConfig : ScriptableObject
    {
        internal int Index { get; set; }
        public StatusEnum status = StatusEnum.Busy;
        public string sequenceName;
        public string displayName;
    }

    [CustomEditor(typeof(SequenceConfig))]
    public class SequenceConfigEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            SequenceConfig sequenceConfig = (SequenceConfig)target;
            DrawDefaultInspector();
            if (string.IsNullOrEmpty(sequenceConfig.sequenceName))
            {
                EditorGUILayout.HelpBox("'Sequence Name' is not set. This is required to retrieve the sequence configuration.", MessageType.Error);
            }
            if (string.IsNullOrEmpty(sequenceConfig.displayName))
            {
                EditorGUILayout.HelpBox("'Display Name' is not set.", MessageType.Error);
            }
        }

    }
}

