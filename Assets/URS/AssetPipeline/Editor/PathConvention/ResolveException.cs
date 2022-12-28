using System;

namespace Daihenka.AssetPipeline.NamingConvention
{
    public class ResolveException : Exception
    {
        public ResolveException(string message) : base(message)
        {
        }
    }

    public class ValueException : Exception
    {
        public ValueException(string message) : base(message)
        {
        }
    }

    public class ParseException : Exception
    {
        public ParseException(string message) : base(message)
        {
        }
    }
}