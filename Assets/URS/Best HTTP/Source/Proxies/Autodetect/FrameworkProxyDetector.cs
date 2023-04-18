#if !BESTHTTP_DISABLE_PROXY && (!UNITY_WEBGL || UNITY_EDITOR)

using System;

namespace BestHTTP.Proxies.Autodetect
{
    /// <summary>
    /// This is a detector using the .net framework's implementation. It might work not just under Windows but MacOS and Linux too.
    /// </summary>
    /// <see cref="https://docs.microsoft.com/en-us/dotnet/api/system.net.http.httpclient.defaultproxy?view=net-6.0"/>
    public sealed class FrameworkProxyDetector : IProxyDetector
    {
        Proxy IProxyDetector.GetProxy(HTTPRequest request)
        {
            var detectedProxy = System.Net.WebRequest.GetSystemWebProxy() as System.Net.WebProxy;
            if (detectedProxy != null && detectedProxy.Address != null)
            {
                var proxyUri = detectedProxy.GetProxy(request.CurrentUri);
                if (proxyUri != null && !proxyUri.Equals(request.CurrentUri))
                {
                    if (proxyUri.Scheme.StartsWith("socks", StringComparison.OrdinalIgnoreCase))
                    {
                        return SetExceptionList(new SOCKSProxy(proxyUri, null), detectedProxy);
                    }
                    else if (proxyUri.Scheme.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        return SetExceptionList(new HTTPProxy(proxyUri), detectedProxy);
                    }
                    else
                    {
                        HTTPManager.Logger.Warning(nameof(FrameworkProxyDetector), $"{nameof(IProxyDetector.GetProxy)} - FindFor returned with unknown format. proxyUri: '{proxyUri}'", request.Context);
                    }
                }
            }

            return null;
        }

        private Proxy SetExceptionList(Proxy proxy, System.Net.WebProxy detectedProxy)
        {
            if (detectedProxy.BypassProxyOnLocal)
            {
                proxy.Exceptions = proxy.Exceptions ?? new System.Collections.Generic.List<string>();

                proxy.Exceptions.Add("localhost");
                proxy.Exceptions.Add("127.0.0.1");
            }

            // TODO: use BypassList to put more entries to the Exceptions list.
            // But because BypassList contains regex strings, we either
            //  1.) store and use regex strings in the Exception list (not backward compatible)
            //  2.) store non-regex strings but create a new list for regex
            //  3.) detect if the stored entry in the Exceptions list is regex or not and use it accordingly
            //  "^.*\\.httpbin\\.org$"
            // https://github.com/Benedicht/BestHTTP-Issues/issues/141

            return proxy;
        }
    }
}

#endif
