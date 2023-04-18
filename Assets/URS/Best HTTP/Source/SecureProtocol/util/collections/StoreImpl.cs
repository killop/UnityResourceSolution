#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;
using System.Collections.Generic;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.Collections
{
    internal sealed class StoreImpl<T>
        : IStore<T>
    {
        private readonly List<T> m_contents;

        internal StoreImpl(IEnumerable<T> e)
        {
            m_contents = new List<T>(e);
        }

        IEnumerable<T> IStore<T>.EnumerateMatches(ISelector<T> selector)
        {
            foreach (T candidate in m_contents)
            {
                if (selector == null || selector.Match(candidate))
                    yield return candidate;
            }
        }
    }
}
#pragma warning restore
#endif
