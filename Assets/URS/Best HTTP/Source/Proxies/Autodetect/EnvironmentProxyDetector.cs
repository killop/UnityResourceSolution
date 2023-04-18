#if !BESTHTTP_DISABLE_PROXY && (!UNITY_WEBGL || UNITY_EDITOR)
using System;
using System.Linq;

using BestHTTP.Connections;

// Examples on proxy strings:
// https://gist.github.com/yougg/5d2b3353fc5e197a0917aae0b3287d64

namespace BestHTTP.Proxies.Autodetect
{
    /// <summary>
    /// Based on https://curl.se/docs/manual.html "Environment Variables" section.
    /// </summary>
    public sealed class EnvironmentProxyDetector : IProxyDetector
    {
        private Proxy _cachedProxy;

        Proxy IProxyDetector.GetProxy(HTTPRequest request)
        {
            if (this._cachedProxy != null)
                return this._cachedProxy;

            string proxyUrl = null;

            if (HTTPProtocolFactory.IsSecureProtocol(request.CurrentUri))
            {
                proxyUrl = GetEnv("HTTPS_PROXY");
                HTTPManager.Logger.Information(nameof(EnvironmentProxyDetector), $"{nameof(IProxyDetector.GetProxy)} - HTTPS_PROXY: '{proxyUrl}'", request.Context);
            }
            else
            {
                proxyUrl = GetEnv("HTTP_PROXY");
                HTTPManager.Logger.Information(nameof(EnvironmentProxyDetector), $"{nameof(IProxyDetector.GetProxy)} - HTTP_PROXY: '{proxyUrl}'", request.Context);
            }

            if (proxyUrl == null)
            {
                proxyUrl = GetEnv("ALL_PROXY");
            }
            else
                HTTPManager.Logger.Information(nameof(EnvironmentProxyDetector), $"{nameof(IProxyDetector.GetProxy)} - ALL_PROXY: '{proxyUrl}'", request.Context);

            if (string.IsNullOrEmpty(proxyUrl))
                return null;

            // if the url is just a host[:port], add the http:// part too. Checking for :// should keep and treat the socks:// scheme too.
            if (proxyUrl.IndexOf("://") == -1 && !proxyUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                proxyUrl = "http://" + proxyUrl;

            string exceptionList = null;
            try
            {
                var proxyUri = new Uri(proxyUrl);

                Proxy proxy = null;
                if (proxyUri.Scheme.StartsWith("socks", StringComparison.OrdinalIgnoreCase))
                    proxy = new SOCKSProxy(proxyUri, null);
                else
                    proxy = new HTTPProxy(proxyUri);

                // A comma-separated list of host names that should not go through any proxy is set in (only an asterisk, * matches all hosts)
                exceptionList = GetEnv("NO_PROXY");
                if (!string.IsNullOrEmpty(exceptionList))
                    proxy.Exceptions = exceptionList.Split(';').ToList<string>();

                return this._cachedProxy = proxy;
            }
            catch (Exception ex)
            {
                HTTPManager.Logger.Exception(nameof(EnvironmentProxyDetector), $"GetProxy - proxyUrl: '{proxyUrl}', exceptionList: '{exceptionList}'", ex, request.Context);
            }

            return null;
        }

        string GetEnv(string key) => System.Environment.GetEnvironmentVariable(key) ?? System.Environment.GetEnvironmentVariable(key.ToLowerInvariant());
    }
}
#endif
