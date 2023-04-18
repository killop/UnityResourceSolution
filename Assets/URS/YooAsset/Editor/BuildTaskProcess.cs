using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using UnityEngine;
using TagInfo = System.Collections.Generic.Dictionary<Bewildered.SmartLibrary.TagRule.TagRuleType, string>;
using Context = System.Collections.Generic.Dictionary<string, object>;
using Debug = UnityEngine.Debug;
namespace URS 
{
    public static partial class Build
    {
        public static void UploadCdnSyn(string channel)
        {
            var context = new Context();
            var buildWS = new BuildTaskWorkSpace();
            buildWS.Init(context);
            buildWS.EnqueueTask(GenUploadCdnBuildTask(channel)) ;
            Build.AddBuildTaskWorkSpace(buildWS);
        }

        public class BuildTaskExcuteCmd : BuildTask
        {
            private string mCmd = null;
            private string mCmdArg = null;
            public BuildTaskExcuteCmd(string cmd, string cmdArg) {
                mCmd = cmd;
                mCmdArg= cmdArg;
            }
            public override void BeginTask()
            {
                base.BeginTask();
                SimpleExec.Command.RunAsync(mCmd, mCmdArg).Wait();
                this.FinishTask();
            }
        }
    }
}


