using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace LivingTomorrow.CMSApi
{
    public class MediaManager : Singleton<MediaManager>
    {
        public Hashtable MediaItems = new();

        [HideInInspector]
        public UnityEvent OnMediaFileInfoFetchedEvent;


        public void LoadNewGameConfig(XRConfig _cfg)
        {
            var _MIS = new Hashtable();
            foreach (string _sceneid in _cfg.scenes.Keys)
            {
                foreach (DictionaryEntry _DE in ((LT_Scene)_cfg.scenes[_sceneid]).mediaItems)
                {
                    var mi = (MediaItem)_DE.Value;
                    mi.sceneid = _sceneid;
                    mi.uid = (string)_DE.Key;
                    //_MIS.Add(_sceneid + _DE.Key, mi);
                    _MIS.Add(_DE.Key, mi);
                }
            }
            MediaItems = _MIS;
        }

        public static string GetMediaPath(string key)
        {
            var m = (MediaItem)Instance.MediaItems[key];
            if (m != null)
            {
                return m.GetMediaFileByLocale(0, LivingTomorrowGameManager.Instance.Language).path;
            }

            //WebSocketManager.SendStatusUpdate(new StatusUpdateMessage());
            return null;
        }

        public void GetMediaFiles()
        {
            // create list of id's
            var ids = new List<string>();
            foreach (DictionaryEntry de in MediaItems)
            {
                var mi = (MediaItem)de.Value;
                string key = (string)de.Key;

                //Debug.Log("key: " + key);

                //LT_Scene scene = (LT_Scene)GameManager.Instance.XRConfig.scenes[mi.sceneid];
                //if (scene.active)
                //{
                for (int i = 0; i < Mathf.Min(mi.mediaItemIds.Length, mi.maxcount); i++)//Loop through items for this mediaitem.
                {
                    string _MediaItemId = mi.mediaItemIds[i];
                    //Debug.Log("MEDIAITEM: " + key + " : " + _MediaItemId);
                    if (!ids.Contains(_MediaItemId))
                    {
                        ids.Add(_MediaItemId);
                    }
                }
                //}
            }

            StartCoroutine(GetMediaFiles(ids));

            /*foreach (DictionaryEntry _DE in MediaItems)
            {
                StartCoroutine(GetMediaFiles((string)_DE.Key, (MediaItem)_DE.Value));
            }*/
        }

        public void AddMediaItem(string _sceneid, string _uid, string _displayName, string _type, int _maxcount, string[] _mediaItemIndexes) => MediaItems.Add(_sceneid + _uid, new MediaItem(_sceneid, _uid, _displayName, _type, _maxcount, _mediaItemIndexes));

        public MediaItem getMediaItem(string _SceneName, string _MediaItemName)
        {
            var _i = (MediaItem)MediaItems[_SceneName + _MediaItemName];
            return _i;
        }

        public string[] getMediaItemIds(string _SceneName, string _MediaItemName)
        {
            var _i = getMediaItem(_SceneName, _MediaItemName);
            if (_i != null)
                return _i.mediaItemIds;
            return new string[] { };
        }

        IEnumerator GetMediaFiles(List<string> ids)
        {
            yield return null;

            bool _success = false;
            while (_success == false)//Retry until successful
            {
                var mibd = new MediaItemsByIds();
                mibd.ByIds = ids.ToArray();

                if (mibd.ByIds.Length > 0)
                {
                    string jsonString = JsonConvert.SerializeObject(mibd);

                    //string jsonString = "{ \"ByIds\": [\"140bc25c-a726-44ae-898c-3ee0d047c3e2\"]}";

                    // Create a new UnityWebRequest to post data to a URL
                    // Why String.Empty? Setting postdata to jsonString causes a memory leak.
                    using (var _uwr = UnityWebRequest.Post(Consts.MediaItemsURI, String.Empty))
                    {

                        var _cert = new CertHandlerForceAcceptAll();
                        _uwr.certificateHandler = _cert;

                        _uwr.SetRequestHeader("Accept", "application/json; charset=UTF-8");

                        // Set the request header to specify that we are sending JSON data
                        _uwr.SetRequestHeader("Content-Type", "application/json");

                        // Set the request body to the JSON string
                        _uwr.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonString));

                        Debug.Log("CMS API | Media Manager | GetMediaFiles: sending request to " + Consts.MediaItemsURI);
                        yield return _uwr.SendWebRequest();

                        _cert?.Dispose();

                        switch (_uwr.result)
                        {
                            case UnityWebRequest.Result.ConnectionError:
                                Debug.LogError("CMS API | Media Manager | GetMediaFiles item Connection Error: " + _uwr.error);
                                yield return new WaitForSeconds(10);
                                break;
                            case UnityWebRequest.Result.DataProcessingError:
                                Debug.LogError("CMS API | Media Manager | GetMediaFiles item Data Processing Error: " + _uwr.error);
                                yield return new WaitForSeconds(10);
                                break;
                            case UnityWebRequest.Result.ProtocolError:
                                Debug.LogError("CMS API | Media Manager | GetMediaFiles item Protocol Error: " + _uwr.error);
                                yield return new WaitForSeconds(10);
                                break;
                            case UnityWebRequest.Result.Success:
                                _success = true;
                                Debug.Log("CMS API | Media Manager | GetMediaFiles item received data: " + _uwr.downloadHandler.text);
                                var _AMIs = JsonConvert.DeserializeObject<List<API_MediaItem>>(_uwr.downloadHandler.text);


                                foreach (var ami in _AMIs)
                                {
                                    // find the mediaItem for this id
                                    foreach (DictionaryEntry de in MediaItems)
                                    {
                                        var mi = (MediaItem)de.Value;
                                        string key = (string)de.Key;
                                        for (int i = 0; i < Mathf.Min(mi.mediaItemIds.Length, mi.maxcount); i++)//Loop through items for this mediaitem.
                                        {
                                            string _MediaItemId = mi.mediaItemIds[i];
                                            if (_MediaItemId == ami.id)
                                            {
                                                //Debug.Log("found MEDIAITEM: " + key + " : " + _MediaItemId);
                                                string _pathprefix = mi.type == "audio" ? "/Audio/" : (mi.type == "360mp4" ? "/360Videos/" : "/Videos/");
                                                foreach (var _auri in ami.uris)
                                                {
                                                    string[] parts = _auri.uri.Split('/');
                                                    mi.AddMediaFile(i, _auri.locale.ToLower(), _auri.uri, _pathprefix + parts[parts.Length - 1], _auri.filesize, ami.metadata);
                                                }
                                            }
                                        }
                                    }

                                }

                                //_uwr.Dispose();

                                OnMediaFileInfoFetchedEvent.Invoke();



                                //MediaItem _MI = (MediaItem)MediaItems[_Key];
                                /*string _pathprefix = _MediaItem.type == "audio" ? "/Audio/" : (_MediaItem.type == "360mp4" ? "/360Videos/" : "/Videos/");
                                foreach (API_uri _auri in _AMI.uris)
                                {
                                    string[] parts = _auri.uri.Split('/');
                                    _MediaItem.AddMediaFile(i, _auri.locale.ToLower(), _auri.uri, _pathprefix + parts[parts.Length - 1], _auri.filesize);
                                }*/

                                break;
                        }

                        //_uwr.Dispose();
                    }
                }
                else
                {
                    _success = true;
                    OnMediaFileInfoFetchedEvent.Invoke();
                }

            }
        }

        IEnumerator GetMediaFiles(string _Key, MediaItem _MediaItem)
        {
            for (int i = 0; i < Mathf.Min(_MediaItem.mediaItemIds.Length, _MediaItem.maxcount); i++)//Loop through items for this mediaitem.
            {
                string _MediaItemId = _MediaItem.mediaItemIds[i];
                string _uri = Consts.MediaItemsURI + _MediaItemId;
                bool _success = false;
                while (_success == false)//Retry until successful
                {
                    using (var _uwr = UnityWebRequest.Get(_uri))
                    {
                        var _cert = new CertHandlerForceAcceptAll();
                        _uwr.certificateHandler = _cert;
                        _uwr.SetRequestHeader("Accept", "application/json; charset=UTF-8");
                        _uwr.SetRequestHeader("Content-Type", "application/json; charset=UTF-8");
                        Debug.Log("CMS API | Media Manager | GetMediaFiles: sending request to " + _uri);
                        // Request and wait for the response
                        yield return _uwr.SendWebRequest();
                        _cert?.Dispose();
                        switch (_uwr.result)
                        {
                            case UnityWebRequest.Result.ConnectionError:
                                Debug.LogError("CMS API | Media Manager | GetMediaFiles (" + _Key + ") item " + i + " Connection Error: " + _uwr.error);
                                yield return new WaitForSeconds(10);
                                break;
                            case UnityWebRequest.Result.DataProcessingError:
                                Debug.LogError("CMS API | Media Manager | GetMediaFiles (" + _Key + ") item " + i + " Data Processing Error: " + _uwr.error);
                                yield return new WaitForSeconds(10);
                                break;
                            case UnityWebRequest.Result.ProtocolError:
                                Debug.LogError("CMS API | Media Manager | GetMediaFiles (" + _Key + ") item " + i + " Protocol Error: " + _uwr.error);
                                yield return new WaitForSeconds(10);
                                break;
                            case UnityWebRequest.Result.Success:
                                _success = true;
                                Debug.Log("CMS API | Media Manager | GetMediaFiles (" + _Key + ") item " + i + " received data: " + _uwr.downloadHandler.text);
                                var _AMI = JsonConvert.DeserializeObject<API_MediaItem>(_uwr.downloadHandler.text);
                                //MediaItem _MI = (MediaItem)MediaItems[_Key];
                                string _pathprefix = _MediaItem.type == "audio" ? "/Audio/" : (_MediaItem.type == "360mp4" ? "/360Videos/" : "/Videos/");
                                foreach (var _auri in _AMI.uris)
                                {
                                    string[] parts = _auri.uri.Split('/');
                                    _MediaItem.AddMediaFile(i, _auri.locale.ToLower(), _auri.uri, _pathprefix + parts[parts.Length - 1], _auri.filesize, _AMI.metadata);
                                }
                                /*if (_AMI.AssetBundleSlots != null && _AMI.AssetBundleSlots.Length > 0)
                                {
                                    _MediaItem.AssetBundleSlots = _AMI.AssetBundleSlots;
                                }*/
                                break;
                        }
                    }
                }
            }
            _MediaItem.mediaFileInfofetched = true;
            int _fetchedamount = 0;
            foreach (MediaItem _MI in MediaItems.Values)
            {
                if (_MI.mediaFileInfofetched)
                {
                    _fetchedamount++;
                }
            }

            if (_fetchedamount >= MediaItems.Count)
            {
                OnMediaFileInfoFetchedEvent.Invoke();
            }
        }
    }

    [Serializable]
    public class MediaItemsByIds
    {
        // Define the property that you want to serialize as an array of strings
        public string[] ByIds { get; set; }
    }

    [Serializable]
    public class MediaItem
    {
        [JsonIgnore]
        public string sceneid { get; set; }//scene id

        [JsonIgnore]
        public string uid { get; set; }//uid

        [JsonIgnore]
        public bool mediaFileInfofetched { get; set; }//true when mediafile info has been fetched

        [JsonIgnore]
        public Hashtable metadata { get; set; }//Media Item Settings from CMS (one per media item)

        public string displayname { get; set; }//displayed name for users
        public string type { get; set; }//type of media item (mp4, audio, 360video, img, ... ==> See CMS!
        public int maxcount { get; set; }//Maximum amount of items
        public string[] mediaItemIds { get; set; }//unique id in the CMS system

        [JsonIgnore]
        public Hashtable[] mediaFiles { get; set; }//media files

        public string assetbundleSlot { get; set; }

        public MediaItem(string _sceneid, string _uid, string _displayname, string _type, int _MaxCount, string[] _mediaItemId, string _assetbundleSlot = "")
        {
            sceneid = _sceneid;
            uid = _uid;
            displayname = _displayname;
            type = _type;
            mediaItemIds = _mediaItemId;
            maxcount = _MaxCount;
            mediaFileInfofetched = false;
            assetbundleSlot = _assetbundleSlot;
        }
        public void AddMediaFile(int mediaItemIdIndex, string _locale, string _url, string _path, int _filesize, MediaItemMetadata _metadata)
        {
            //if (mediaFiles == null) mediaFiles = new Hashtable();
            if (mediaItemIdIndex >= mediaItemIds.Length | mediaItemIdIndex >= maxcount)
            {
                Debug.Log("Media Manager | Trying to add media file for item that does not exist. MaxCount = " + maxcount + ", ActualCount = " + mediaItemIds.Length + ", requested item = " + mediaItemIdIndex + 1);
                return;
            }

            metadata ??= new Hashtable(); //Create hashtable if it doesn't exist.

            if (mediaFiles == null)
            {
                mediaFiles = new Hashtable[mediaItemIds.Length]; //Redim the array if it doesn't exist.
                for (int i = 0; i < mediaItemIds.Length; i++)
                    mediaFiles[i] = new Hashtable(); //Create a new hashtable instance
            }
            if (metadata[mediaItemIdIndex] == null && _metadata != null)
            {
                metadata.Add(mediaItemIdIndex, _metadata);
            }
            else if (metadata[mediaItemIdIndex] == null && _metadata == null)
            {
                // Set a default value to prevent crash
                Debug.LogWarning("-----------------------------------------------------------------------------------------------------------------------------------");
                Debug.LogWarning("CMS API | Media Manager | Metadata for mediafile doesn't exist. Adding a default value. Make sure to add metadata in DB (CMS --> media_items)");
                Debug.LogWarning("-----------------------------------------------------------------------------------------------------------------------------------");
                var defaultValue = new MediaItemMetadata() { _360mp4 = new _360mp4Metadata() { Mapping = "Lattitude Longitude Layout", Exposure = 1, Rotation = 0, _3DLayout = "Over Under", ImageType = "360 Degrees", TintColor = "#808080", RenderTextureSizeX = 4096, RenderTextureSizeY = 2048 } };

                metadata.Add(mediaItemIdIndex, defaultValue);
            }
            if (mediaFiles[mediaItemIdIndex][_locale] == null)
            {
                mediaFiles[mediaItemIdIndex].Add(_locale, new MediaFile(_url, _path, _filesize));
            }
            else
            {
                Debug.Log("CMS API | Media Manager | Trying to add media file for locale " + _locale + " which is already present!");
            }
            return;
        }
        public MediaFile GetMediaFileByLocale(int _mediaItemIdIndex, string _locale)
        {
            if (mediaFiles != null)
            {
                var _mf = (MediaFile)mediaFiles[_mediaItemIdIndex][_locale];
                if (_mf != null)
                    return _mf; //First try to return the correct locale
                foreach (DictionaryEntry de in mediaFiles[_mediaItemIdIndex])
                    return (MediaFile)de.Value; //Then just take the first one found
            }
            return null;//If no locales are present, return nothing
        }
        public bool MediaFileHasBeenDownloaded(int _mediaItemIdIndex, string _locale)
        {
            var _mf = GetMediaFileByLocale(_mediaItemIdIndex, _locale);
            if (_mf != null)
                return _mf.HasBeenDownloaded();
            return false;
        }
    }
}
[Serializable]
public class _360mp4Metadata
{
    public string Mapping { get; set; }
    [JsonProperty("Image Type")]
    public string ImageType { get; set; }
    [JsonProperty("3D Layout")]
    public string _3DLayout { get; set; }
    public float Rotation { get; set; }
    public float Exposure { get; set; }
    [JsonProperty("Tint Color")]
    public string TintColor { get; set; }
    [JsonProperty("Render Texture Size x")]
    public int RenderTextureSizeX { get; set; }
    [JsonProperty("Render Texture Size y")]
    public int RenderTextureSizeY { get; set; }
}

[Serializable]
public class MediaItemMetadata
{
    [JsonProperty("360mp4")]
    public _360mp4Metadata _360mp4 { get; set; }
}

public class MediaAssetBase
{
    public string sceneId { get; set; }
    public string uid { get; set; }
    public int mediaItemIdIndex { get; set; }
    public bool downloaded { get; set; }//When the item was successfully downloaded
    public bool ready { get; set; }//When the item is prepared and ready to be played
    public bool localerequired { get; set; }//When this item requires localisation to be set.
    public float downloadprogress { get; set; }
    public MediaAssetBase(string _uid, bool _localerequired, int _mediaItemIdIndex, string _sceneId)
    {
        sceneId = _sceneId;
        uid = _uid;
        mediaItemIdIndex = _mediaItemIdIndex;
        localerequired = _localerequired;
        downloaded = false;
        ready = false;
        downloadprogress = 0;
    }
}

public class MediaFile
{
    public MediaFile(string _url, string _path, int _filesize)
    {
        filesize = _filesize;
        url = _url;
        path = _path;
        downloading = false;
    }
    public string url { get; set; }//url as stored in the CMS (used for downloading the asset)
    public string path { get; set; }//local path to the downloaded media
    public int filesize { get; set; }//theoretical filesize of the downloaded file (to check if download was successfull)
    public bool downloading { get; set; }//Is this file being downloaded?
    public bool HasBeenDownloaded()
    {
        if (path.Length > 1)
            return File.Exists(path);
        return false;
    }
}

/*[Serializable]
public class API_MediaItem
{
    public API_uri[] uris;
    public AssetBundleSlot[] AssetBundleSlots;
    // add assetBundle stuff here
}
[Serializable]
public class API_uri
{
    public string uri;
    public string locale;
    public int filesize;
}
*/

[Serializable]
public class AssetBundleSlot
{
    public string id;
    public string name;
    public string created_at;
    public string updated_at;
}


[Serializable]
public class API_MediaItem
{
    // Define the properties that match the structure of the JSON file
    public string id { get; set; }
    public string name { get; set; }
    public API_uri[] uris { get; set; }
    public string[] captions { get; set; }
    public string thumbnail { get; set; }
    public string type { get; set; }
    public string screentype { get; set; }
    public MediaItemMetadata metadata { get; set; }
    public AssetBundleSlot[] AssetBundleSlots { get; set; }
}

// Define the class that represents the "uris" array
public class API_uri
{
    // Define the properties that match the structure of the JSON file
    public string uri { get; set; }
    public string locale { get; set; }
    public int filesize { get; set; }
}
