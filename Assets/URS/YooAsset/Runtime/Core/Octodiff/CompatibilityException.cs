using System;

namespace MHLab.Patch.Core.Octodiff
{
    internal sealed class CompatibilityException : Exception
    {
        public CompatibilityException(string message) : base(message)
        {
            
        }
    }
}