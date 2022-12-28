using System;

namespace MHLab.Patch.Core.Octodiff
{
    internal sealed class UsageException : Exception
    {
        public UsageException(string message) : base(message)
        {
            
        }
    }
}