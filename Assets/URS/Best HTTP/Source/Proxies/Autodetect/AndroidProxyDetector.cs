#if !BESTHTTP_DISABLE_PROXY && UNITY_ANDROID && !UNITY_EDITOR
using System;

using UnityEngine;

namespace BestHTTP.Proxies.Autodetect
{
    public sealed class AndroidProxyDetector : IProxyDetector
    {
        private const string ClassPath = "com.besthttp.proxy.ProxyFinder";

        Proxy IProxyDetector.GetProxy(HTTPRequest request)
        {
            try
            {
                var proxyUrl = FindFor(request.CurrentUri.ToString());

                HTTPManager.Logger.Information(nameof(AndroidProxyDetector), $"{nameof(IProxyDetector.GetProxy)} - FindFor returned with proxyUrl: '{proxyUrl}'", request.Context);

                if (proxyUrl == null)
                    return null;

                if (proxyUrl.StartsWith("socks://", StringComparison.OrdinalIgnoreCase))
                {
                    return new SOCKSProxy(new Uri(proxyUrl), null);
                }
                else if (proxyUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                {
                    return new HTTPProxy(new Uri(proxyUrl));
                }
                else
                {
                    HTTPManager.Logger.Warning(nameof(AndroidProxyDetector), $"{nameof(IProxyDetector.GetProxy)} - FindFor returned with unknown format. proxyUrl: '{proxyUrl}'", request.Context);
                }
            }
            catch (Exception ex)
            {
                HTTPManager.Logger.Exception(nameof(AndroidProxyDetector), nameof(IProxyDetector.GetProxy), ex, request.Context);
            }

            return null;
        }

        private string FindFor(string uriStr) => Call<string>("FindFor", uriStr);

        private static void Call(string methodName, params object[] args)
        {
            using (var javaClass = new AndroidJavaClass(ClassPath))
                javaClass.CallStatic(methodName, args);
        }

        private static T Call<T>(string methodName, params object[] args)
        {
            using (var javaClass = new AndroidJavaClass(ClassPath))
                return javaClass.CallStatic<T>(methodName, args);
        }
    }
}
#endif
