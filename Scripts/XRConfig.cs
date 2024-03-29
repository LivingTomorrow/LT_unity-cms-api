using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;
using UnityEngine;

namespace LivingTomorrow.CMSApi
{
    [Serializable]
    public class XRConfig
    {
        public Hashtable globalConfig = new Hashtable();
        public Hashtable scenes = new Hashtable();

        [HideInInspector]
        public string[] scenarioEvents = { };

        [NonSerialized]
        public Dictionary<int, int> sceneOrderFromId = new Dictionary<int, int>();

        /*public Dictionary<string, object> globalConfig = new Dictionary<string, object>();
        public Dictionary<string,LT_Scene> scenes = new Dictionary<string,LT_Scene>();*/
        public void addGlobalConfig(string _id, object _config)
        {
            globalConfig.Add(_id, _config);
        }
        public void addScene(string _id, LT_Scene _scene)
        {
            scenes.Add(_id, _scene);
        }
        public void DeserializeHashtables()
        {
            sceneOrderFromId = new Dictionary<int, int>();
            Hashtable _globalConfig = new Hashtable();
            Hashtable _scenes = new Hashtable();
            string _jsonstring;
            ConfigParam _cp;
            foreach (DictionaryEntry _DE in globalConfig)
            {
                _jsonstring = ((object)_DE.Value).ToString();
                _cp = JsonConvert.DeserializeObject<ConfigParam>(_jsonstring);
                switch (_cp.type)
                {
                    case "string":
                        _globalConfig.Add(_DE.Key, JsonConvert.DeserializeObject<ConfigParamString>(_jsonstring));
                        break;
                    case "bool":
                        _globalConfig.Add(_DE.Key, JsonConvert.DeserializeObject<ConfigParamBool>(_jsonstring));
                        break;
                    case "integer":
                        _globalConfig.Add(_DE.Key, JsonConvert.DeserializeObject<ConfigParamInt>(_jsonstring));
                        break;
                }
            }
            globalConfig = _globalConfig;
            foreach (DictionaryEntry _DE in scenes)
            {
                _jsonstring = ((object)_DE.Value).ToString();
                _scenes.Add(_DE.Key, JsonConvert.DeserializeObject<LT_Scene>(_jsonstring));
                ((LT_Scene)_scenes[_DE.Key]).DeserializeHashtables();
                LT_Scene s = ((LT_Scene)_scenes[_DE.Key]);
                sceneOrderFromId[s.id] = s.order;
            }
            scenes = _scenes;
        }
    }
    [Serializable]
    public class ConfigParam
    {
        public string displayName;
        [HideInInspector]
        public string type;
    }


    [Serializable]
    public class ConfigParamBoolId
    {
        [Tooltip("Id used to retrieve the parameter data.")]
        public string id;
        public ConfigParamBool config;
    }

    [Serializable]
    public class ConfigParamBool : ConfigParam
    {
        [Tooltip("Default value.")]
        public bool value;
        [Tooltip("If this parameter is checked, it means that changing the value of this parameter may cause unexpected results.")]
        public bool advanced;

        public ConfigParamBool()
        {
            type = "bool";
        }

        public ConfigParamBool(string _displayName, bool _value, bool _advanced = false)
        {
            type = "bool";
            displayName = _displayName;
            value = _value;
            advanced = _advanced;
        }
    }


    [Serializable]
    public class ConfigParamIntId
    {
        [Tooltip("Id used to retrieve the parameter data.")]
        public string id;
        public ConfigParamInt config;
    }

    [Serializable]
    public class ConfigParamInt : ConfigParam
    {
        [Tooltip("Default value")]
        public int value;
        [Tooltip("Smallest allowed value.")]
        public int min;
        [Tooltip("Largest allowed value.")]
        public int max;
        [Tooltip("If this parameter is checked, it means that changing the value of this parameter may cause unexpected results.")]
        public bool advanced;

        public string options
        {
            get
            {
                return min.ToString() + "," + max.ToString();
            }
        }

        public ConfigParamInt()
        {
            type = "integer";
        }

        public ConfigParamInt(string _displayName, int _value, int _min, int _max, bool _advanced = false)
        {
            type = "integer";
            displayName = _displayName;
            value = _value;
            min = _min;
            max = _max;
            advanced = _advanced;
        }
    }

    [Serializable]
    public class ConfigParamStringId
    {
        [Tooltip("Id used to retrieve the parameter data.")]
        public string id;
        public ConfigParamString config;
    }

    [Serializable]
    public class ConfigParamString : ConfigParam
    {
        [Tooltip("Use this field to set predetermined options. Use comma separated values.")]
        public string options;
        [Tooltip("Default value.")]
        public string value;
        [Tooltip("If this parameter is checked, it means that changing the value of this parameter may cause unexpected results.")]
        public bool advanced;

        public ConfigParamString()
        {
            type = "string";
        }
        public ConfigParamString(string _displayName, string _value, string _options, bool _advanced = false)
        {
            type = "string";
            displayName = _displayName;
            value = _value;
            options = _options;
            advanced = _advanced;
        }
    }
    [Serializable]
    public class LT_Scene
    {
        public int id;
        public int order;

        [NonSerialized]
        public string name;

        public string displayName;
        public bool active;
        public bool useCutscene = false;
        public Hashtable mediaItems = new Hashtable();
        public Hashtable sceneConfig = new Hashtable();
        public Hashtable sequenceElements = new Hashtable();

        /*public Dictionary<string, MediaItem> mediaItems = new Dictionary<string, MediaItem>();
        public Dictionary<string, object> sceneConfig = new Dictionary<string, object>();
        public Dictionary<string, LT_SequenceElement> sequenceElements = new Dictionary<string, LT_SequenceElement>();*/
        [JsonConstructor]
        public LT_Scene(int _id, string _displayName, bool _active, int _order, string _name)
        {
            id = _id;
            displayName = _displayName;
            active = _active;
            order = _order;
            name = _name;
            addSceneConfig("active", new ConfigParamBool("Active", true));
        }

        public LT_Scene(int _index, string _name, string _displayName)
        {
            id = _index;
            displayName = _displayName;
            active = true;
            order = _index;
            name = _name;
            addSceneConfig("active", new ConfigParamBool("Active", true));
        }

        public void addMediaItem(string _id, MediaItem _MediaItem)
        {
            mediaItems.Add(_id, _MediaItem);
        }
        public void addSceneConfig(string _id, object _SceneConfig)
        {
            sceneConfig.Add(_id, _SceneConfig);
        }
        public void addSequenceElement(string _id, LT_SequenceElement _SequenceElement)
        {
            sequenceElements.Add(_id, _SequenceElement);
        }
        public void DeserializeHashtables()
        {
            Hashtable _sceneConfig = new Hashtable();
            Hashtable _mediaItems = new Hashtable();
            Hashtable _sequenceElements = new Hashtable();
            string _jsonstring;
            ConfigParam _cp;
            foreach (DictionaryEntry _DE in sceneConfig)
            {
                _jsonstring = ((object)_DE.Value).ToString();
                _cp = JsonConvert.DeserializeObject<ConfigParam>(_jsonstring);
                switch (_cp.type)
                {
                    case "string":
                        _sceneConfig.Add(_DE.Key, JsonConvert.DeserializeObject<ConfigParamString>(_jsonstring));
                        break;
                    case "bool":
                        _sceneConfig.Add(_DE.Key, JsonConvert.DeserializeObject<ConfigParamBool>(_jsonstring));
                        break;
                    case "integer":
                        _sceneConfig.Add(_DE.Key, JsonConvert.DeserializeObject<ConfigParamInt>(_jsonstring));
                        break;
                }
            }
            sceneConfig = _sceneConfig;
            foreach (DictionaryEntry _DE in mediaItems)
            {
                _jsonstring = ((object)_DE.Value).ToString();
                _mediaItems.Add(_DE.Key, JsonConvert.DeserializeObject<MediaItem>(_jsonstring));
            }
            mediaItems = _mediaItems;
            foreach (DictionaryEntry _DE in sequenceElements)
            {
                _jsonstring = ((object)_DE.Value).ToString();
                _sequenceElements.Add(_DE.Key, JsonConvert.DeserializeObject<LT_SequenceElement>(_jsonstring));
            }
            sequenceElements = _sequenceElements;
        }
    }
    //[Serializable]
    //public class LT_MediaItem
    //{
    //    public string displayName;
    //    public string type;
    //    public string mediaItemId;
    //}
    [Serializable]
    public class LT_SequenceElement
    {
        public string displayName;
        public bool idle;
        public bool seek;
        public int order;
        public List<string> controls;
        public LT_SequenceElement(int _order, string _displayName, bool _idle, bool _seek, List<string> _controls)
        {
            order = _order;
            displayName = _displayName;
            idle = _idle;
            seek = _seek;
            controls = _controls;
        }
    }

    public class SequenceInfo
    {

        public string DisplayName { get; set; }
        public StatusUpdateMessage.StatusEnum Status { get; set; }
        public static Hashtable CreateSequenceElements<T>(Dictionary<T, SequenceInfo> dict) where T : Enum
        {
            var _ReturnValue = new Hashtable();
            foreach (var kvp in dict)
            {
                int index;
                if (Enum.IsDefined(typeof(T), kvp.Key))
                {
                    index = (int)(object)kvp.Key;
                }
                else
                {
                    throw new ArgumentException($"Enum value {kvp.Key} cannot be converted to an integer.");
                }

                var sq = kvp.Value;
                var sequenceElement = new LT_SequenceElement(index, sq.DisplayName,sq.Status == StatusUpdateMessage.StatusEnum.Idle, false, null);
                _ReturnValue.Add(kvp.Key, sequenceElement);
            }
            return _ReturnValue;
        }
    }
}