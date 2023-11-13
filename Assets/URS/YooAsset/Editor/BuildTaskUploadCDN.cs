using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Threading.Tasks;

namespace URS
{
    public static partial class Build 
    {

        public static BuildTaskExcuteCmd GenUploadCdnBuildTask(string channel)
        {
            string cmd = @"coscmd";
            string cmdString = $" upload -rsy --delete {Application.dataPath}/../{Build.GetChannelRoot()}/{channel} /{channel}";
            // Debug.LogError(""+ cmdString);
            return new BuildTaskExcuteCmd(cmd, cmdString);
        }
        [MenuItem("URS/UploadCDNASyn(非阻塞上传cdn)")]
        public static Task GenUploadCdnTask(string channel)
        {
            string cmd = @"coscmd";
            string cmdString = $" upload -rsy --delete {Application.dataPath}/../{Build.GetChannelRoot()}/{channel} /{channel}";
            // Debug.LogError(""+ cmdString);
            var task= SimpleExec.Command.RunAsync(cmd, cmdString);
            return task;
        }
    }
}

