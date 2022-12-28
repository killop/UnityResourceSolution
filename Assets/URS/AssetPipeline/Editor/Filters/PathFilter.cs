using System;
using System.Text.RegularExpressions;

namespace Daihenka.AssetPipeline.Filters
{
    public enum StringMatchType
    {
        Equals = 0,
        Contains = 1,
        StartsWith = 2,
        EndsWith = 3,
        Wildcard = 4,
        Regex = 5
    }

    [Serializable]
    public class PathFilter : StringFilter
    {
    }

    [Serializable]
    public class StringFilter
    {
        public StringMatchType matchType;
        public string pattern;
        public bool ignoreCase;

        public StringFilter() : this(StringMatchType.Wildcard, "*")
        {
        }

        public StringFilter(StringMatchType matchType, string pattern, bool ignoreCase = false)
        {
            this.matchType = matchType;
            this.pattern = pattern;
            this.ignoreCase = ignoreCase;
        }

        public bool IsMatch(string input)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(pattern))
            {
                return false;
            }

            var comparisonType = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.CurrentCulture;
            switch (matchType)
            {
                case StringMatchType.Equals:
                    return string.Equals(input, pattern, comparisonType);
                case StringMatchType.Contains:
                    return input.Contains(pattern, comparisonType);
                case StringMatchType.StartsWith:
                    return input.StartsWith(pattern, comparisonType);
                case StringMatchType.EndsWith:
                    return input.EndsWith(pattern, comparisonType);
                case StringMatchType.Wildcard:
                    return Regex.Match(input, pattern.WildcardToRegex(false), ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None).Success;
                case StringMatchType.Regex:
                    return Regex.Match(input, pattern, ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None).Success;
            }

            return false;
        }
    }
}