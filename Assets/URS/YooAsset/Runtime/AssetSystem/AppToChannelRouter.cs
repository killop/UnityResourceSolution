using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using URS;
using YooAsset.Utility;


[Serializable]
public class AppToChannelRouter 
{
    [SerializeField]
    public AppToChannelItem[] Items;

    [NonSerialized]
    private Dictionary<string, AppToChannelItem> _itemMap = new Dictionary<string, AppToChannelItem>();

    public AppToChannelItem GetChanel(string appId) 
    {
        if (_itemMap.TryGetValue(appId, out var result)) 
        {
            return result;
        }
        UnityEngine.Debug.LogError("Can not find AppToChannel appid " + appId);
        return null;
       
    }
    /// <summary>
    /// 序列化
    /// </summary>
    public static void Serialize(string savePath, AppToChannelRouter router, bool pretty = false)
  {
      string json = JsonUtility.ToJson(router, pretty);
      FileUtility.CreateFile(savePath, json);
  }
  /// <summary>
  /// 反序列化
  /// </summary>
  public static AppToChannelRouter Deserialize(string jsonData)
  {
      AppToChannelRouter router = JsonUtility.FromJson<AppToChannelRouter>(jsonData);
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
            Items = new AppToChannelItem[0];
        }
        if (_itemMap == null)
        {
            _itemMap = new Dictionary<string, AppToChannelItem>();
        }
        for (int i = 0; i < Items.Length; i++)
        {
            var item = Items[i];
            if (item != null)
            {
                if (!string.IsNullOrEmpty(item.AppId))
                {
                    if (!_itemMap.ContainsKey(item.AppId))
                    {
                        _itemMap[item.AppId] = item;
                    }
                }
                else
                {
                    Debug.LogError($"{item.AppId} AppId is null");
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
public class AppToChannelItem
{
    [SerializeField]
    public string AppId;

    [SerializeField]
    public string ChannelId;

    [SerializeField]
    public string VersionCode;
}
