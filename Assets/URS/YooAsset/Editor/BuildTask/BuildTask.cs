using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Context = System.Collections.Generic.Dictionary<string, object>;

public class BuildTask 
{
    public const string CONTEXT_VERSION_DIRECTORY = "version_directory";
    // public const string CONTEXT_RAW_FILES = "raw_files";
    public const string CONTEXT_ASSET_DATABASE_AUTO_SAVE = "asset_database_auto_save";
    public const string CONTEXT_ASSET_INFO = "asset_info";
    public const string CONTEXT_BUNDLE_INFO = "bundle_info";
    public const string CONTEXT_BUNDLE_RESULT = "bundle_result";
    public const string CONTEXT_GLOBAL_BUNDLE_EXTRA_ASSET = "global_bundle_extra_asset";

    public const string CONTEXT_SPRITE_CHECHER = "sprite_in_atlas_checker";
    public const string CONTEXT_FILE_ADDITION_INFO = "addition_file_info";
    public const string CONTEXT_COPY_STREAM_TARGET_VERSION = "copy_stream_target_version";
    //public const string TEMP_VERSION_DIRECTORY = "temp_version_directory";


    public enum ETaskStatus
    {
        NotStart,
        Working,
        Finish,
    }
    public void SetContext(Context context)
    {
        _context = context;
    }
    public T GetData<T>(string key) where T : class {
        if (_context.ContainsKey(key))
        {
            var ob = (_context[key]);
            return ob as T;
        }
        return default(T);
    }

    public T GetOrAddData<T>(string key) where T : class, new()
    {
        if (_context.ContainsKey(key))
        {
            var ob = (_context[key]);
            return ob as T;
        }
        else {
            T newOb = new T();
            _context[key] = newOb;  
            return newOb;   
        }
    }
    public void SetData(string key,object ob) 
    {
        _context[key] = ob;
    }
    public ETaskStatus _status = ETaskStatus.NotStart;
    public Context _context = null;
    private System.DateTime _begin;
    public void Update()
    {
        if (_status == ETaskStatus.Working)
        {
            OnTaskUpdate();
        }
    }
    public virtual void BeginTask()
    {
        Debug.Log($"[Begin task] {this.GetType().Name} ");
        _status = ETaskStatus.Working;
        _begin = System.DateTime.Now;
    }
    public virtual void OnTaskUpdate()
    {

    }
    public virtual void FinishTask()
    {
        var duration= System.DateTime.Now- _begin;
        Debug.Log($"[Finish task] {this.GetType().Name}  duration {duration.TotalMilliseconds}");
        _status = ETaskStatus.Finish;
    }

    public bool IsFinished()
    {
        return this._status == ETaskStatus.Finish;
    }

}
