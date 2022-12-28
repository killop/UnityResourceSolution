using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Bewildered.SmartLibrary;
using UnityEditor.SearchService;
using UnityEditor.Search;

public class BuildTaskUpdateCollection : BuildTask
{
    public class CollectionSearchTask
    {
        public LibraryCollection collection;
        public bool started = false;
    }
    private List<CollectionSearchTask> _tasks = null;
    public override void BeginTask()
    {
        base.BeginTask();
      //  SearchService.Refresh();
        LibraryDatabase.RootCollection.RemoveInvailidItem();
        EditorUtility.DisplayProgressBar("UpdateCollections", "开始收集打包资源", 0);
        _tasks = new List<CollectionSearchTask>();
        var root = SessionData.instance;
        if (root != null)
        {
            foreach (var kv in root.IDToCollectionMap)
            {
                var collection = kv.Value;
                _tasks.Add(new CollectionSearchTask()
                {
                    collection = collection,
                    started = false
                }) ;
            }
        }
    }

    public override void OnTaskUpdate()
    {
        base.OnTaskUpdate();
        if (_tasks.Count > 0)
        {
            var lastIndex = _tasks.Count - 1;
            var last = _tasks[lastIndex];
            if (!last.started)
            {
                last.started = true;
                last.collection.UpdateItems(true);
                return;
            }
            else
            {
                if (last.collection is SmartCollection)
                {
                    var sm = last.collection as SmartCollection;
                    if (sm.IsSearching)
                    {
                        return;
                    }
                    else
                    {
                        _tasks.RemoveAt(lastIndex);
                        return;
                    }
                }
                else
                {
                    _tasks.RemoveAt(lastIndex);
                    return;
                }
            }
        }
        else
        {
            this.FinishTask();
            return;
        }
    }

    public override void FinishTask()
    {
        _tasks = null;
        EditorUtility.ClearProgressBar();
        base.FinishTask();
    }
}
