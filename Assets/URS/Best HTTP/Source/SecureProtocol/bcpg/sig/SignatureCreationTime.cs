#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;

using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Utilities;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.Date;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Bcpg.Sig
{
    /**
    * packet giving signature creation time.
    */
    public class SignatureCreationTime
        : SignatureSubpacket
    {
		protected static byte[] TimeToBytes(DateTime time)
        {
			long t = DateTimeUtilities.DateTimeToUnixMs(time) / 1000L;
            return Pack.UInt32_To_BE((uint)t);
        }

        public SignatureCreationTime(bool critical, bool isLongLength, byte[] data)
            : base(SignatureSubpacketTag.CreationTime, critical, isLongLength, data)
        {
        }

        public SignatureCreationTime(bool critical, DateTime date)
            : base(SignatureSubpacketTag.CreationTime, critical, false, TimeToBytes(date))
        {
        }

        public DateTime GetTime()
        {
            uint time = Pack.BE_To_UInt32(data, 0);
			return DateTimeUtilities.UnixMsToDateTime(time * 1000L);
        }
    }
}
#pragma warning restore
#endif
