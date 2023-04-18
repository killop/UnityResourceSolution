#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;

using BestHTTP.SecureProtocol.Org.BouncyCastle.Security;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Paddings
{
    /// <summary>Block cipher padders are expected to conform to this interface.</summary>
    public interface IBlockCipherPadding
    {
        /// <summary>Initialise the padder.</summary>
        /// <param name="random">A source of randomness, if any required.</param>
        void Init(SecureRandom random);

        /// <summary>The name of the algorithm this padder implements.</summary>
        string PaddingName { get; }

        /// <summary>Add padding to the passed in block.</summary>
        /// <param name="input">the block to add padding to.</param>
        /// <param name="inOff">the offset into the block the padding is to start at.</param>
        /// <returns>the number of bytes of padding added.</returns>
        int AddPadding(byte[] input, int inOff);

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER || _UNITY_2021_2_OR_NEWER_
        /// <summary>Add padding to the passed in block.</summary>
        /// <param name="block">the block to add padding to.</param>
        /// <param name="position">the offset into the block the padding is to start at.</param>
        /// <returns>the number of bytes of padding added.</returns>
        int AddPadding(Span<byte> block, int position);
#endif

        /// <summary>Determine the length of padding present in the passed in block.</summary>
        /// <param name="input">the block to check padding for.</param>
        /// <returns>the number of bytes of padding present.</returns>
        int PadCount(byte[] input);

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER || _UNITY_2021_2_OR_NEWER_
        /// <summary>Determine the length of padding present in the passed in block.</summary>
        /// <param name="block">the block to check padding for.</param>
        /// <returns>the number of bytes of padding present.</returns>
        int PadCount(ReadOnlySpan<byte> block);
#endif
    }
}
#pragma warning restore
#endif
