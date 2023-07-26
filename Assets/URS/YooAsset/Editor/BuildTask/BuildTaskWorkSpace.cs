using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Context = System.Collections.Generic.Dictionary<string, object>;
public class BuildTaskWorkSpace 
{
    public Queue<BuildTask> _buildTasks = new Queue<BuildTask>();
    public BuildTask _currentBuildTask = null;
    public Context _context;
    private Exception _buildException;
    public bool hasException => _buildException != null;
    public BuildTaskWorkSpace Init(Context context)
    {
        this._context = context;
        _currentBuildTask = null;
        return this;
    }
    public BuildTaskWorkSpace EnqueueTask(BuildTask task)
    {
        _buildTasks.Enqueue(task);
        return this;
    }
    public bool HasAnyWork()
    {
        return _buildTasks.Count > 0;
    }
    public void DoNextTask()
    {
        if (_buildException != null)
            return;
        
        if (_buildTasks.Count > 0)
        {
            try
            {
                var task = _buildTasks.Peek();
                _currentBuildTask = task;
                _currentBuildTask.SetContext(this._context);
                _currentBuildTask.BeginTask();
            }
            catch (Exception e)
            {
                Debug.LogError($"{e.Message}\n{e.StackTrace}");
                _buildException = e;
            }
        }
    }

    public void StartAndWaitComplete()
    {
        this.DoNextTask();

        while (this.HasAnyWork() && !this.hasException)
        {
            this.Update();
        }
    }
    
    public void Update()
    {
        if (_buildException != null)
            return;
        
        if (_currentBuildTask != null)
        {
            try
            {
                _currentBuildTask.Update();
            }
            catch (Exception e)
            {
                Debug.LogError($"{e.Message}\n{e.StackTrace}");
                _buildException = e;
                return;
            }
            if (_currentBuildTask.IsFinished())
            {
                _buildTasks.Dequeue();
                _currentBuildTask = null;
                DoNextTask();
            }
        }
    }
}
