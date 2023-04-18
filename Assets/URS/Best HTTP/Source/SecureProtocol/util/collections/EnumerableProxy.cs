#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;
using System.Collections.Generic;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.Collections
{
	internal sealed class EnumerableProxy<T>
		: IEnumerable<T>
	{
		private readonly IEnumerable<T> m_target;

		internal EnumerableProxy(IEnumerable<T> target)
		{
			if (target == null)
				throw new ArgumentNullException(nameof(target));

			m_target = target;
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return m_target.GetEnumerator();
		}

		public IEnumerator<T> GetEnumerator()
		{
			return m_target.GetEnumerator();
		}
	}
}
#pragma warning restore
#endif
