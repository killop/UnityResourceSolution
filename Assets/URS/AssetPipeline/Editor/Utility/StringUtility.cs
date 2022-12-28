using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Daihenka.AssetPipeline
{
    internal static class StringUtility
    {
        static readonly Regex s_CamelCaseValidation = new Regex(@"^[a-z]([A-Z0-9]*[a-z][a-z0-9]*[A-Z]|[a-z0-9]*[A-Z][A-Z0-9]*[a-z])*[A-Za-z0-9]*$");
        static readonly Regex s_PascalCaseValidation = new Regex(@"^[A-Z]([A-Z0-9]*[a-z][a-z0-9]*[A-Z]|[a-z0-9]*[A-Z][A-Z0-9]*[a-z])*[A-Za-z0-9]*$");
        static readonly Regex s_KebabCaseValidation = new Regex(@"^([a-z]{1,})(-[a-z0-9]{1,})*$");
        static readonly Regex s_SnakeCaseValidation = new Regex(@"^([a-z]{1,})(_[a-z0-9]{1,})*$");
        static readonly Regex s_UpperSnakeCaseValidation = new Regex(@"^([A-Z]{1,})(_[A-Z0-9]{1,})*$");
        static readonly Regex s_StringCasePattern = new Regex(@"[A-Z]{2,}(?=[A-Z][a-z]+[0-9]*|\b)|[A-Z]?[a-z]+[0-9]*|[A-Z]|[0-9]+");

        public static IList<string> ToStrings(this MatchCollection matchCollection)
        {
            var result = new List<string>();
            foreach (Match match in matchCollection)
            {
                result.Add(match.Value);
            }

            return result;
        }

        public static StringConventionFlags GetStringConvention(this string str)
        {
            var flags = StringConventionFlags.None;
            if (str.IsCamelCase())
            {
                flags |= StringConventionFlags.CamelCase;
            }

            if (str.IsPascalCase())
            {
                flags |= StringConventionFlags.PascalCase;
            }

            if (str.IsSnakeCase())
            {
                flags |= StringConventionFlags.SnakeCase;
            }

            if (str.IsUpperSnakeCase())
            {
                flags |= StringConventionFlags.UpperSnakeCase;
            }

            if (str.IsKebabCase())
            {
                flags |= StringConventionFlags.KebabCase;
            }

            if (str.IsLowerCase())
            {
                flags |= StringConventionFlags.LowerCase;
            }

            if (str.IsUpperCase())
            {
                flags |= StringConventionFlags.UpperCase;
            }

            return flags;
        }

        public static string ToConvention(this string str, StringConvention convention)
        {
            switch (convention)
            {
                case StringConvention.CamelCase:
                    return str.ToCamelCase();
                case StringConvention.PascalCase:
                    return str.ToPascalCase();
                case StringConvention.SnakeCase:
                    return str.ToSnakeCase();
                case StringConvention.KebabCase:
                    return str.ToKebabCase();
                case StringConvention.UpperSnakeCase:
                    return str.ToUpperSnakeCase();
                case StringConvention.LowerCase:
                    return str.ToLowerInvariant();
                case StringConvention.UpperCase:
                    return str.ToUpperInvariant();
                default:
                    return str;
            }
        }

        public static string ToTitleCase(this string str)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(ToWordsString(str));
        }

        public static string ToPascalCase(this string str)
        {
            return CultureInfo.CurrentCulture.TextInfo
                .ToTitleCase(ToWordsString(str))
                .Replace(" ", "");
        }

        public static string ToCamelCase(this string str)
        {
            if (str.Length < 2)
            {
                return str.ToLowerInvariant();
            }

            var s = CultureInfo.CurrentCulture.TextInfo
                .ToTitleCase(ToWordsString(str))
                .Replace(" ", "");
            return char.ToLower(s[0]) + s.Substring(1);
        }

        public static string ToKebabCase(this string str)
        {
            return ToWordsString(str, "-");
        }

        public static string ToSnakeCase(this string str)
        {
            return ToWordsString(str, "_");
        }

        public static string ToUpperSnakeCase(this string str)
        {
            return ToWordsString(str, "_").ToUpperInvariant();
        }

        static string ToWordsString(string str, string separator = " ")
        {
            return string.Join(separator, s_StringCasePattern.Matches(str).ToStrings()).ToLowerInvariant();
        }

        public static bool IsKebabCase(this string str)
        {
            return s_KebabCaseValidation.IsMatch(str);
        }

        public static bool IsSnakeCase(this string str)
        {
            return s_SnakeCaseValidation.IsMatch(str);
        }

        public static bool IsUpperSnakeCase(this string str)
        {
            return s_UpperSnakeCaseValidation.IsMatch(str);
        }

        public static bool IsPascalCase(this string str)
        {
            return s_PascalCaseValidation.IsMatch(str);
        }

        public static bool IsCamelCase(this string str)
        {
            return s_CamelCaseValidation.IsMatch(str);
        }

        public static bool IsLowerCase(this string str)
        {
            return str.ToLowerInvariant() == str;
        }

        public static bool IsUpperCase(this string str)
        {
            return str.ToUpperInvariant() == str;
        }

        public static string WildcardToRegex(this string pattern, bool includeEndMarker = true)
        {
            var result = "^" + Regex.Escape(pattern)
                .Replace(@"\*", "(.*)")
                .Replace(@"\?", "(.)");
            return includeEndMarker ? result + "$" : result;
        }

        public static bool Contains(this string source, string value, StringComparison comparisonType)
        {
            return source?.IndexOf(value, comparisonType) >= 0;
        }
    }
}