using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Bewildered.SmartLibrary;
using System.IO;
using System;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Tasks;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using TagInfo = System.Collections.Generic.Dictionary<Bewildered.SmartLibrary.TagRule.TagRuleType, string>;
using URS;
using YooAsset.Utility;
using System.Linq;
using YooAsset;
using UnityEditor.Build.Content;
using BuildCompression = UnityEngine.BuildCompression;
using UnityEditor.Search;
using Context = System.Collections.Generic.Dictionary<string, object>;
/// <summary>
/// The BuildTask used to extract write data from the build.
/// </summary>
public class ExtractDataTask : IBuildTask
{
    /// <summary>
    /// The ExtractDataTask version.
    /// </summary>
    public int Version { get { return 1; } }

    /// <summary>
    /// Get the injected dependency data of the task.
    /// </summary>
    public IDependencyData DependencyData { get { return m_DependencyData; } }

    /// <summary>
    /// Get the injected write data of the task.
    /// </summary>
    public IBundleWriteData WriteData { get { return m_WriteData; } }

    /// <summary>
    /// Get the injected build cache of the task.
    /// </summary>
    public IBuildCache BuildCache { get { return m_BuildCache; } }

    /// <summary>
    /// The build context of the task.
    /// </summary>
    public IBuildContext BuildContext { get { return m_BuildContext; } }

#pragma warning disable 649
    [InjectContext(ContextUsage.In)]
    IDependencyData m_DependencyData;

    [InjectContext(ContextUsage.In)]
    IBundleWriteData m_WriteData;

    [InjectContext(ContextUsage.In)]
    IBuildCache m_BuildCache;

    [InjectContext(ContextUsage.In)]
    internal IBuildContext m_BuildContext;
#pragma warning restore 649

    /// <summary>
    /// Runs the ExtractDataTask.  The data for this task is all injected context so no operations are performed in the Run step.
    /// </summary>
    /// <returns>Success.</returns>
    public ReturnCode Run()
    {
        return ReturnCode.Success;
    }
}