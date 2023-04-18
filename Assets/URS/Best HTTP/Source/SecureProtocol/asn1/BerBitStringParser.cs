#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;
using System.IO;

using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.IO;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1
{
    /// <summary>A parser for indefinite-length BIT STRINGs.</summary>
    internal class BerBitStringParser
        : Asn1BitStringParser
    {
        private readonly Asn1StreamParser m_parser;

        private ConstructedBitStream m_bitStream;

        internal BerBitStringParser(Asn1StreamParser parser)
        {
            m_parser = parser;
        }

        public Stream GetOctetStream()
        {
            return m_bitStream = new ConstructedBitStream(m_parser, true);
        }

        public Stream GetBitStream()
        {
            return m_bitStream = new ConstructedBitStream(m_parser, false);
        }

        public int PadBits
        {
            get { return m_bitStream.PadBits; }
        }

        public Asn1Object ToAsn1Object()
        {
            try
            {
                return Parse(m_parser);
            }
            catch (IOException e)
            {
                throw new Asn1ParsingException("IOException converting stream to byte array: " + e.Message, e);
            }
        }

        internal static BerBitString Parse(Asn1StreamParser sp)
        {
            ConstructedBitStream bitStream = new ConstructedBitStream(sp, false);
            byte[] data = Streams.ReadAll(bitStream);
            int padBits = bitStream.PadBits;
            return new BerBitString(data, padBits);
        }
    }
}
#pragma warning restore
#endif
