using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEditor;

namespace Bewildered.SmartLibrary
{
    // ===Explanation===
    // We want the in-memory collections to match the on disk files
    // so that data is not lost when using a version control system with others
    // and changes are pulled from the main repo.
     
    // While the ItemsChanged and HierarchyChanged events can be used for the most
    // common changes. Changes to the settings (properties) of a collection is more difficult.
    
    // To that end we cache a collection every time it changes and save it to disk
    // after no changes to a collection have been made for a short period of time.
    // We delay because actions like changing a float/int property in the inspector would
    // send change events each frame as the value was dragged.
    
    /// <summary>
    /// Handles saving <see cref="LibraryCollection"/>s to disk when changes are made.
    /// </summary>
    [InitializeOnLoad]
    internal static class CollectionSaveSync
    {
        private static readonly Queue<LibraryCollection> _queuedSaveCollections = new Queue<LibraryCollection>();
        private static readonly Stopwatch _timer = new Stopwatch();
        private static readonly long _saveDelayMilliseconds = 1500;
        
        static CollectionSaveSync()
        {
            ObjectChangeEvents.changesPublished += OnObjectChangesPublished;
            EditorApplication.update += SaveCollectionsTask;
            
            // We save before reloading the assemblies because the collections in the list will be lost otherwise.
            AssemblyReloadEvents.beforeAssemblyReload += SaveAllChanges;
            // We save before quitting in-case a change is made and
            // the user quits right after before the delay has finished.
            EditorApplication.quitting += SaveAllChanges;
        }

        private static void OnObjectChangesPublished(ref ObjectChangeEventStream stream)
        {
            for (int i = 0; i < stream.length; i++)
            {
                if (stream.GetEventType(i) == ObjectChangeKind.ChangeAssetObjectProperties)
                {
                    stream.GetChangeAssetObjectPropertiesEvent(i, out ChangeAssetObjectPropertiesEventArgs data);
                    var obj = EditorUtility.InstanceIDToObject(data.instanceId);
                    
                    if (obj is LibraryCollection collection)
                    {
                        EnqueueCollection(collection);
                    }
                }
            }
        }
        
        private static void SaveCollectionsTask()
        {
            if (_queuedSaveCollections.Count > 0 && _timer.ElapsedMilliseconds > _saveDelayMilliseconds)
            {
                SaveAllChanges();
            }
        }

        private static void SaveAllChanges()
        {
            foreach (var collection in _queuedSaveCollections)
            {
                if (collection != null) // Could be null if the collection was destroyed.
                    LibraryUtility.SaveCollection(collection);
            }
            _queuedSaveCollections.Clear();
            _timer.Stop();
        }

        internal static void EnqueueCollection(LibraryCollection collection)
        {
            // We add the collection that was changed to the queue to be saved if it was not already.
            // We don't add multiple as there is no reason to save the same collection multiple times.
            if (!_queuedSaveCollections.Contains(collection))
            {
                _queuedSaveCollections.Enqueue(collection);
                _timer.Restart();
            }
        }
    }
}
