using System;

namespace MHLab.Patch.Core.Octodiff
{
    internal sealed class CorruptFileFormatException : Exception
    {
        public CorruptFileFormatException(string message) : base(message)
        {
        }
    }
}