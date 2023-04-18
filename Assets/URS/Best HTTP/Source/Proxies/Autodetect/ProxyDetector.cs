#if !BESTHTTP_DISABLE_PROXY && (!UNITY_WEBGL || UNITY_EDITOR)
using System;

using BestHTTP.Core;

namespace BestHTTP.Proxies.Autodetect
{
    public interface IProxyDetector
    {
        Proxy GetProxy(HTTPRequest request);
    }

    public enum ProxyDetectionMode
    {
        /// <summary>
        /// In Continouos mode the ProxyDetector will check for a proxy for every request.
        /// </summary>
        Continouos,

        /// <summary>
        /// This mode will cache the first Proxy found and use it for consecutive requests.
        /// </summary>
        CacheFirstFound
    }

    public sealed class ProxyDetector
    {
        public static IProxyDetector[] GetDefaultDetectors() => new IProxyDetector[] {
                // HTTPManager.Proxy has the highest priority
                new ProgrammaticallyAddedProxyDetector(),

                // then comes the environment set
                new EnvironmentProxyDetector(),

                // .net framework's detector
                new FrameworkProxyDetector(),

#if UNITY_ANDROID && !UNITY_EDITOR
                new AndroidProxyDetector(),
#endif
            };

        private IProxyDetector[] _proxyDetectors;
        private ProxyDetectionMode _detectionMode;
        private bool _attached;

        public ProxyDetector()
            : this(ProxyDetectionMode.CacheFirstFound, GetDefaultDetectors())
        { }

        public ProxyDetector(ProxyDetectionMode detectionMode)
            :this(detectionMode, GetDefaultDetectors())
        { }

        public ProxyDetector(ProxyDetectionMode detectionMode, IProxyDetector[] proxyDetectors)
        {
            this._detectionMode = detectionMode;
            this._proxyDetectors = proxyDetectors;

            if (this._proxyDetectors != null)
                Reattach();
        }

        public void Reattach()
        {
            HTTPManager.Logger.Information(nameof(ProxyDetector), $"{nameof(Reattach)}({this._attached})");

            if (!this._attached)
            {
                RequestEventHelper.OnEvent += OnRequestEvent;
                this._attached = true;
            }
        }

        /// <summary>
        /// Call Detach() to disable ProxyDetector's logic to find and set a proxy.
        /// </summary>
        public void Detach()
        {
            HTTPManager.Logger.Information(nameof(ProxyDetector), $"{nameof(Detach)}({this._attached})");

            if (this._attached)
            {
                RequestEventHelper.OnEvent -= OnRequestEvent;
                this._attached = false;
            }
        }

        private void OnRequestEvent(RequestEventInfo @event)
        {
            // The Resend event is raised for every request when it's queued up (sent or redirected).
            if (@event.Event == RequestEvents.Resend && @event.SourceRequest.Proxy == null)
            {
                Uri uri = @event.SourceRequest.CurrentUri;

                HTTPManager.Logger.Information(nameof(ProxyDetector), $"OnRequestEvent(RequestEvents.Resend) uri: '{uri}'", @event.SourceRequest.Context);

                try
                {
                    foreach (var detector in this._proxyDetectors)
                    {
                        if (detector == null)
                            continue;

                        if (HTTPManager.Logger.Level == Logger.Loglevels.All)
                            HTTPManager.Logger.Verbose(nameof(ProxyDetector), $"Calling {detector.GetType().Name}'s GetProxy", @event.SourceRequest.Context);

                        var proxy = detector.GetProxy(@event.SourceRequest);
                        if (proxy != null && proxy.UseProxyForAddress(uri))
                        {
                            if (HTTPManager.Logger.Level == Logger.Loglevels.All)
                                HTTPManager.Logger.Verbose(nameof(ProxyDetector), $"[{detector.GetType().Name}] Proxy found: {proxy.Address} ", @event.SourceRequest.Context);

                            switch (this._detectionMode)
                            {
                                case ProxyDetectionMode.Continouos:
                                    @event.SourceRequest.Proxy = proxy;
                                    break;

                                case ProxyDetectionMode.CacheFirstFound:
                                    HTTPManager.Proxy = @event.SourceRequest.Proxy = proxy;

                                    HTTPManager.Logger.Verbose(nameof(ProxyDetector), $"Proxy cached in HTTPManager.Proxy!", @event.SourceRequest.Context);

                                    Detach();
                                    break;
                            }

                            return;
                        }
                    }

                    HTTPManager.Logger.Information(nameof(ProxyDetector), $"No Proxy for '{uri}'.", @event.SourceRequest.Context);
                }
                catch (Exception ex)
                {
                    if (HTTPManager.Logger.Level == BestHTTP.Logger.Loglevels.All)
                        HTTPManager.Logger.Exception(nameof(ProxyDetector), $"GetProxyFor({@event.SourceRequest.CurrentUri})", ex, @event.SourceRequest.Context);
                }
            }
        }
    }
}

#endif
