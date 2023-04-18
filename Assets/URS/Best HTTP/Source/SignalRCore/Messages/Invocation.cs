#if !BESTHTTP_DISABLE_SIGNALR_CORE
using System;

namespace BestHTTP.SignalRCore.Messages
{
    [PlatformSupport.IL2CPP.Preserve]
    public struct Completion
    {
        [PlatformSupport.IL2CPP.Preserve] public MessageTypes type;
        [PlatformSupport.IL2CPP.Preserve] public string invocationId;
    }

    [PlatformSupport.IL2CPP.Preserve]
    public struct CompletionWithResult
    {
        [PlatformSupport.IL2CPP.Preserve] public MessageTypes type;
        [PlatformSupport.IL2CPP.Preserve] public string invocationId;
        [PlatformSupport.IL2CPP.Preserve] public object result;
    }

    [PlatformSupport.IL2CPP.Preserve]
    public struct CompletionWithError
    {
        [PlatformSupport.IL2CPP.Preserve] public MessageTypes type;
        [PlatformSupport.IL2CPP.Preserve] public string invocationId;
        [PlatformSupport.IL2CPP.Preserve] public string error;
    }

    [PlatformSupport.IL2CPP.Preserve]
    public struct StreamItemMessage
    {
        [PlatformSupport.IL2CPP.Preserve] public MessageTypes type;
        [PlatformSupport.IL2CPP.Preserve] public string invocationId;
        [PlatformSupport.IL2CPP.Preserve] public object item;
    }

    [PlatformSupport.IL2CPP.Preserve]
    public struct InvocationMessage
    {
        [PlatformSupport.IL2CPP.Preserve] public MessageTypes type;
        [PlatformSupport.IL2CPP.Preserve] public string invocationId;
        [PlatformSupport.IL2CPP.Preserve] public bool nonblocking;
        [PlatformSupport.IL2CPP.Preserve] public string target;
        [PlatformSupport.IL2CPP.Preserve] public object[] arguments;
    }

    [PlatformSupport.IL2CPP.Preserve]
    public struct UploadInvocationMessage
    {
        [PlatformSupport.IL2CPP.Preserve] public MessageTypes type;
        [PlatformSupport.IL2CPP.Preserve] public string invocationId;
        [PlatformSupport.IL2CPP.Preserve] public bool nonblocking;
        [PlatformSupport.IL2CPP.Preserve] public string target;
        [PlatformSupport.IL2CPP.Preserve] public object[] arguments;
        [PlatformSupport.IL2CPP.Preserve] public string[] streamIds;
    }

    [PlatformSupport.IL2CPP.Preserve]
    public struct CancelInvocationMessage
    {
        [PlatformSupport.IL2CPP.Preserve] public MessageTypes type { get { return MessageTypes.CancelInvocation; } }
        [PlatformSupport.IL2CPP.Preserve] public string invocationId;
    }

    [PlatformSupport.IL2CPP.Preserve]
    public struct PingMessage
    {
        [PlatformSupport.IL2CPP.Preserve] public MessageTypes type { get { return MessageTypes.Ping; } }
    }

    [PlatformSupport.IL2CPP.Preserve]
    public struct CloseMessage
    {
        [PlatformSupport.IL2CPP.Preserve] public MessageTypes type { get { return MessageTypes.Close; } }
    }

    [PlatformSupport.IL2CPP.Preserve]
    public struct CloseWithErrorMessage
    {
        [PlatformSupport.IL2CPP.Preserve] public MessageTypes type { get { return MessageTypes.Close; } }
        [PlatformSupport.IL2CPP.Preserve] public string error;
    }
}
#endif
