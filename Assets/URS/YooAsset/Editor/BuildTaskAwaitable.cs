
using UnityEditor;
using System;
using System.IO;
using UnityEngine;
using TagInfo = System.Collections.Generic.Dictionary<Bewildered.SmartLibrary.TagRule.TagRuleType, string>;
using Context = System.Collections.Generic.Dictionary<string, object>;
using Debug = UnityEngine.Debug;
using NinjaBeats;


namespace URS
{
    public static partial class Build
    {
        public class BuildTaskAwaitable : System.Runtime.CompilerServices.INotifyCompletion
        {
            protected Action Continuation;
            public object Result { get; protected set; }
            public bool IsCompleted { get; protected set; }
            
            public void OnCompleted(Action continuation)
            {
                if (IsCompleted)
                {
                    continuation?.Invoke();
                }
                else
                {
                    if (continuation != null)
                        Continuation += continuation;
                }
            }

            public void SetDone()
            {
                if (IsCompleted)
                    return;
                Result = null;
                IsCompleted = true;
                Continuation?.Invoke();
            }

            public object GetResult() => Result;

            public BuildTaskAwaitable GetAwaiter() => this;
        }

        public static BuildTaskAwaitable WaitBuildTask()
        {
            BuildTaskAwaitable r = new();
            EditorApplication.CallbackFunction func = null;
            func = () =>
            {
                if (hasTask)
                {
                    if (_buildTaskWorkSpaces[0].hasException)
                        EditorApplication.Exit(1);
                    return;   
                }

                EditorApplication.update -= func;    
                r.SetDone();
            };
            EditorApplication.update += func;
            return r;
        }

 
    }
}
   
