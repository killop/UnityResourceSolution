using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;

static class ThreadingManager
{
    public enum ThreadQueues
    {
        SaveQueue,
        UploadQueue,
        PruneQueue,
        TotalQueues
    }

    static Task[] m_Tasks = new Task[(int)ThreadQueues.TotalQueues];

    static ThreadingManager()
    {
        EditorApplication.quitting += WaitForOutstandingTasks;
        AssemblyReloadEvents.beforeAssemblyReload += WaitForOutstandingTasks;
    }

    internal static void WaitForOutstandingTasks()
    {
        var tasks = m_Tasks.Where(x => x != null).ToArray();
        m_Tasks = new Task[(int)ThreadQueues.TotalQueues];
        if (tasks.Length > 0)
            Task.WaitAll(tasks);
    }

    internal static void QueueTask(ThreadQueues queue, Action<object> action, object state)
    {
        var task = m_Tasks[(int)queue];
        if (queue == ThreadQueues.PruneQueue)
        {
            // Prune tasks need to run after any existing queued tasks
            var tasks = m_Tasks.Where(x => x != null).ToArray();
            m_Tasks = new Task[(int)ThreadQueues.TotalQueues];
            if (tasks.Length > 0)
                task = Task.WhenAll(tasks).ContinueWith(delegate { action.Invoke(state); });
            else
                task = Task.Factory.StartNew(action, state);
        }
        else if (task == null)
        {
            // New Upload or Save tasks need to be done after any queued prune tasks
            var pruneTask = m_Tasks[(int)ThreadQueues.PruneQueue];
            if (pruneTask != null)
                task = pruneTask.ContinueWith(delegate { action.Invoke(state); });
            else
                task = Task.Factory.StartNew(action, state);
        }
        else
            task = task.ContinueWith(delegate { action.Invoke(state); });
        m_Tasks[(int)queue] = task;
    }
}
