# Build Logging

Scriptable Build Pipeline has a profiling instrumentation system enabling build performance logging. By default, building AssetBundles will create a .json log file in the Trace Event Profiler Format within the target output directory. The file contains timing measurements of various build tasks and can be viewed using the [Trace Event Profiling Tool](https://www.chromium.org/developers/how-tos/trace-event-profiling-tool).

The default logger can be overriden by passing in an [IBuildLogger](xref:UnityEditor.Build.Pipeline.Interfaces.IBuildLogger) object as a context object input. This could be useful if you want to log performance data in a different format or want the build events to be added to a custom performance repot. The [BuildLog](xref:UnityEditor.Build.Pipeline.Utilities.BuildLog) class implements [IBuildLogger](xref:UnityEditor.Build.Pipeline.Interfaces.IBuildLogger) and is used as the default logger.


# Adding Custom Instrumentation

If you are creating or modifying build tasks that could affect build performance, you should consider adding instrumentation blocks to your new code. You can do this by calling the [IBuildLogger](xref:UnityEditor.Build.Pipeline.Interfaces.IBuildLogger) methods directly or using the [ScopedStep](xref:UnityEditor.Build.Pipeline.Interfaces.BuildLoggerExternsions) and [AddEntrySafe](xref:UnityEditor.Build.Pipeline.Interfaces.BuildLoggerExternsions) extension methods.
