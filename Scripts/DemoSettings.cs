using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static LivingTomorrow.CMSApi.Consts;

namespace LivingTomorrow.CMSApi
{
    [CreateAssetMenu(fileName = "DemoSettings", menuName = "LivingTomorrow/Demo Settings")]
    public class DemoSettings : ScriptableObject
    {
        [SerializeField]
        public string TourBackendURL;
        [SerializeField]
        public string CmsURL;
        [SerializeField]
        public string DemoID;
        [SerializeField]
        public AuthType CurrentAuthType;
        [Tooltip("Set this to checked if you wish to test the application without connecting to the Living Tomorrow backend. This is for development purposes only.")]
        [SerializeField]
        public bool standAloneMode;
    }

    [CustomEditor(typeof(DemoSettings))]
    public class DemoSettingsInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            DemoSettings settings = (DemoSettings)target;
            DrawDefaultInspector();
            if (string.IsNullOrEmpty(settings.TourBackendURL) && !settings.standAloneMode)
            {
                EditorGUILayout.HelpBox("'TourBackendURL is required if stand alone mode is not checked!", MessageType.Error);
            }
            if (string.IsNullOrEmpty(settings.CmsURL) && !settings.standAloneMode)
            {
                EditorGUILayout.HelpBox("'Cms URL is required if stand alone mode is not checked!", MessageType.Error);
            }
            if (string.IsNullOrEmpty(settings.DemoID) && !settings.standAloneMode)
            {
                EditorGUILayout.HelpBox("'Demo ID is required if stand alone mode is not checked!", MessageType.Error);
            }
        }

    }
}

