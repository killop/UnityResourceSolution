using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using URS;
using YooAsset.Utility;
using System.Text;
using System.Text.RegularExpressions;

[Serializable]
public class AppVersionRouter 
{
    [SerializeField]
    public AppVersionItem[] Items;

    [SerializeField]

    public string DefaultVersion;

    [NonSerialized]
    private Dictionary<string, AppVersionItem> _itemMap = new Dictionary<string, AppVersionItem>();

    public string GetChannel(string applicationVersion) 
    {
        foreach (var key in _itemMap.Keys)
        {
            Regex regex= new Regex(key);
            bool match = regex.IsMatch(applicationVersion);
            if (match) 
            {
                return _itemMap[key].VersionCode;
            }
        }
        return DefaultVersion;
    }
    /// <summary>
    /// 序列化
    /// </summary>
    public static void Serialize(string savePath, AppVersionRouter router, bool pretty = false)
   {
      string json = JsonUtility.ToJson(router, pretty);
      FileUtility.CreateFile(savePath, json);
    }
  /// <summary>
  /// 反序列化
  /// </summary>
  public static AppVersionRouter Deserialize(string jsonData)
  {
      AppVersionRouter router = JsonUtility.FromJson<AppVersionRouter>(jsonData);
      if (router != null)
      {
            router.AfterDeserialize();
      }
      return router;
  }
    public void AfterDeserialize()
    {
        if (Items == null)
        {
            Items = new AppVersionItem[0];
        }
        if (_itemMap == null)
        {
            _itemMap = new Dictionary<string, AppVersionItem>();
        }
        for (int i = 0; i < Items.Length; i++)
        {
            var item = Items[i];
            if (item != null)
            {
                if (!string.IsNullOrEmpty(item.ApplicationVersion))
                {
                    if (!_itemMap.ContainsKey(item.ApplicationVersion))
                    {
                        _itemMap[item.ApplicationVersion] = item;
                    }
                }
                else
                {
                    Debug.LogError($"{item.ApplicationVersion} AppId is null");
                }
            }
            else
            {
                Debug.LogError($"{i} filemeta is null");
            }
        }
    }
}
[Serializable]
public class AppVersionItem
{
    [SerializeField]
    public string ApplicationVersion;

    [SerializeField]
    public string VersionCode;
}
