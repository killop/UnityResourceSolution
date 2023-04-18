#if !BESTHTTP_DISABLE_PROXY && (!UNITY_WEBGL || UNITY_EDITOR)
namespace BestHTTP.Proxies.Autodetect
{
    /// <summary>
    /// This one just returns with HTTPManager.Proxy,
    /// so when ProgrammaticallyAddedProxyDetector is used in the first place for the ProxyDetector,
    /// HTTPManager.Proxy gets the highest priority.
    /// </summary>
    public sealed class ProgrammaticallyAddedProxyDetector : IProxyDetector
    {
        Proxy IProxyDetector.GetProxy(HTTPRequest request) => HTTPManager.Proxy;
    }
}
#endif
