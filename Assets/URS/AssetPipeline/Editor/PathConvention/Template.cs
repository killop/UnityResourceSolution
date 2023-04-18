using System;
using System.Collections.Generic;
using Debug = UnityEngine.Debug;
using System.Text.RegularExpressions;

namespace Daihenka.AssetPipeline.NamingConvention
{
    public class TemplateKeys : Dictionary<string, StringConvention>
    {
    }

    public class TemplateData : Dictionary<string, TemplateParsedData>
    {
        public TemplateData()
        {
        }

        public TemplateData(int capacity) : base(capacity)
        {
        }

        public bool HasFailures
        {
            get
            {
                foreach (var value in Values)
                {
                    if (!value.isValid)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }

    public struct TemplateParsedData
    {
        public string value;
        public bool isValid;
        public StringConvention convention;

        public TemplateParsedData(string value, StringConvention convention, bool isValid = true)
        {
            this.value = value;
            this.convention = convention;
            this.isValid = isValid;
        }
    }

    public class Template
    {
        static readonly Dictionary<string, StringConventionFlags> s_ConventionGroupSuffixMap = new Dictionary<string, StringConventionFlags>
        {
            {"_SNAKE", StringConventionFlags.SnakeCase},
            {"_USNAKE", StringConventionFlags.UpperSnakeCase},
            {"_KEBAB", StringConventionFlags.KebabCase},
            {"_CAMEL", StringConventionFlags.CamelCase},
            {"_PASCAL", StringConventionFlags.PascalCase},
            {"_UPPER", StringConventionFlags.UpperCase},
            {"_LOWER", StringConventionFlags.LowerCase},
            {"_NONE", StringConventionFlags.None},
        };

        static readonly Dictionary<string, string> s_ConventionExpressionMap = new Dictionary<string, string>
        {
            {@"\snake", "_SNAKE"},
            {@"\usnake", "_USNAKE"},
            {@"\kebab", "_KEBAB"},
            {@"\camel", "_CAMEL"},
            {@"\pascal", "_PASCAL"},
            {@"\upper", "_UPPER"},
            {@"\lower", "_LOWER"},
            {@"\none", "_NONE"},
        };

        static readonly Regex s_EscapeExpression = new Regex(@"(?<placeholder>{(.+?)(:(\\}|.)+?)?})|(?<other>.+?)");
        static readonly Regex s_ConvertPlaceholders = new Regex(@"{(?<placeholder>.+?)(:(?<expression>(\\}|.)+?))?}");
        static readonly Regex s_StripExpression = new Regex(@"{(.+?)(:(\\}|.)+?)}");
        static readonly Regex s_PlainPlaceholder = new Regex(@"{(.+?)}");
        static readonly Regex s_TemplateReference = new Regex(@"{@(?<reference>.+?)}");
        const string kAtCode = "_WXV_";
        const string kPeriodCode = "_LPD_";
        const string kDefaultPlaceholderExpression = @"[\w_.\-]+";

        public string Name;
        public string Pattern;
        public Anchor Anchor;
        public StringConvention DefaultValueConvention;
        public string DefaultPlaceholderExpression;
        public DuplicatePlaceholderMode DuplicatePlaceholderMode;
        public ITemplateResolver TemplateResolver;

        public string expandedPattern => s_TemplateReference.Replace(Pattern, ExpandReference);

        Regex cachedPatternRegex;
        Regex cachedExpandedPatternRegex;
        string cachedPatternFormatSpecification;
        string cachedExpandedPatternFormatSpecification;

        public Template(string name, string pattern, Anchor anchor = Anchor.Start, string defaultPlaceholderExpression = kDefaultPlaceholderExpression, DuplicatePlaceholderMode duplicatePlaceholderMode = DuplicatePlaceholderMode.Relaxed, ITemplateResolver templateResolver = null)
            : this(name, pattern, StringConvention.None, anchor, defaultPlaceholderExpression, duplicatePlaceholderMode, templateResolver)
        {
        }

        public Template(string name, string pattern, StringConvention defaultValueConvention, Anchor anchor = Anchor.Start, string defaultPlaceholderExpression = kDefaultPlaceholderExpression, DuplicatePlaceholderMode duplicatePlaceholderMode = DuplicatePlaceholderMode.Relaxed,
            ITemplateResolver templateResolver = null)
        {
            Name = name;
            Pattern = pattern;
            Anchor = anchor;
            DefaultValueConvention = defaultValueConvention;
            DefaultPlaceholderExpression = defaultPlaceholderExpression;
            DuplicatePlaceholderMode = duplicatePlaceholderMode;
            TemplateResolver = templateResolver;
            cachedPatternRegex = ConstructRegularExpression(pattern);
        }

        string ExpandReference(Match match)
        {
            var reference = match.Groups["reference"].Value;
            if (TemplateResolver == null)
            {
                throw new ResolveException($"Failed to resolve reference {reference} as no template resolver set");
            }

            var template = TemplateResolver.Get(reference);
            if (template == null)
            {
                throw new ResolveException($"Failed to resolve reference {reference} using template resolver");
            }

            return template.expandedPattern;
        }

        public TemplateKeys KeyConventions()
        {
            if (cachedExpandedPatternRegex == null)
            {
                cachedExpandedPatternRegex = ConstructRegularExpression(expandedPattern);
            }

            var parsed = new TemplateKeys();
            var groupNames = cachedExpandedPatternRegex.GetGroupNames();
            for (var i = 1; i < groupNames.Length; i++)
            {
                var groupName = groupNames[i];
                var key = groupName;
                var convention = DefaultValueConvention;
                var conventionSuffix = GetConventionSuffix(key);
                if (!string.IsNullOrEmpty(conventionSuffix))
                {
                    convention = s_ConventionGroupSuffixMap[conventionSuffix].ToNonFlag();
                    key = key.Substring(0, key.Length - conventionSuffix.Length);
                }
                else if (key.EndsWith("_EXPR"))
                {
                    convention = StringConvention.None;
                    key = key.Substring(0, key.Length - 5);
                }

                key = key.Substring(0, key.Length - 3);
                key = key.Replace(kPeriodCode, ".");
                if (!parsed.ContainsKey(key))
                {
                    parsed.Add(key, convention);
                }
                else
                {
                    parsed[key] = convention;
                }
            }

            return parsed;
        }

        static string GetConventionSuffix(string value)
        {
            foreach (var key in s_ConventionGroupSuffixMap.Keys)
            {
                if (value.EndsWith(key))
                {
                    return key;
                }
            }

            return null;
        }

        public TemplateData Parse(string path, bool throwOnConventionFailure = false)
        {
            if (cachedExpandedPatternRegex == null)
            {
                cachedExpandedPatternRegex = ConstructRegularExpression(expandedPattern);
            }

            var match = cachedExpandedPatternRegex.Match(path);
            if (match.Success)
            {
                var parsed = new TemplateData(match.Groups.Count - 1);
                for (var i = 1; i < match.Groups.Count; i++)
                {
                    var group = match.Groups[i];
                    var key = group.Name;
                    var convention = StringConventionFlags.None;
                    var conventionSuffix = GetConventionSuffix(key);
                    if (!string.IsNullOrEmpty(conventionSuffix))
                    {
                        convention = s_ConventionGroupSuffixMap[conventionSuffix];
                        key = key.Substring(0, key.Length - conventionSuffix.Length);
                    }
                    else if (key.EndsWith("_EXPR"))
                    {
                        convention = StringConventionFlags.None;
                        key = key.Substring(0, key.Length - 5);
                    }

                    key = key.Substring(0, key.Length - 3);
                    key = key.Replace(kPeriodCode, ".");
                    var value = group.Value;
                    var entry = new TemplateParsedData(value, convention.ToNonFlag());

                    if (convention != StringConventionFlags.None)
                    {
                        if ((value.GetStringConvention() & convention) != convention)
                        {
                            entry.isValid = false;
                            if (throwOnConventionFailure)
                            {
                                throw new ParseException($"Value for placeholder {key} did not match convention {entry.convention.ToString()}. Value was {value}");
                            }
                        }
                    }

                    if (DuplicatePlaceholderMode == DuplicatePlaceholderMode.Strict)
                    {
                        if (parsed.ContainsKey(key))
                        {
                            if (parsed[key].value != value)
                            {
                                throw new ParseException($"Different extracted values for placeholder {key} detected. Values were {parsed[key].value} and {value}");
                            }
                        }
                        else
                        {
                            parsed.Add(key, entry);
                        }
                    }
                    else
                    {
                        if (parsed.ContainsKey(key))
                        {
                            parsed[key] = entry;
                        }
                        else
                        {
                            parsed.Add(key, entry);
                        }
                    }
                }

                return parsed;
            }

            throw new ParseException($"Path {path} did not match template pattern");
        }

        public string Format(Dictionary<string, string> data)
        {
            var formatSpecification = ConstructExpandedPatternFormatSpecification();
            try
            {
                return s_PlainPlaceholder.Replace(formatSpecification, match => data[match.Groups[1].Value]);
            }
            catch
            {
                var message = "";
                var i = 0;
                foreach (var kvp in data)
                {
                    if (i > 0)
                    {
                        message += ", ";
                    }

                    message += $"{kvp.Key}: {kvp.Value}";
                    i++;
                }

                throw new FormatException($"Could not format data {{{message}}} due to missing key");
            }
        }

        public TemplateData ValidateData(Dictionary<string, string> data, bool throwOnConventionFailure = false)
        {
            var result = new TemplateData(data.Count);
            var keyConventions = KeyConventions();
            foreach (var kvp in data)
            {
                if (!keyConventions.ContainsKey(kvp.Key))
                {
                    continue;
                }

                var entry = new TemplateParsedData(kvp.Value, keyConventions[kvp.Key]);
                var convention = entry.convention.ToFlag();
                entry.isValid = (kvp.Value.GetStringConvention() & convention) != convention;

                if (!entry.isValid)
                {
                    throw new ParseException($"Value for placeholder {kvp.Key} did not match convention {entry.convention.ToString()}. Value was {entry.value}");
                }

                result.Add(kvp.Key, entry);
            }

            return result;
        }

        public Dictionary<string, string> KeysDictionary()
        {
            var result = new Dictionary<string, string>(Keys.Count);
            foreach (var key in Keys)
            {
                result.Add(key, string.Empty);
            }

            return result;
        }

        public List<string> Keys
        {
            get
            {
                var formatSpecification = ConstructExpandedPatternFormatSpecification();
                var result = new List<string>();
                var refer = s_PlainPlaceholder.Matches(formatSpecification);
                foreach (Match match in refer)
                {
                    result.Add(match.Value.Substring(1, match.Value.Length - 2));
                }

                return result;
            }
        }

        public List<string> References
        {
            get
            {
                var formatSpecification = ConstructPatternFormatSpecification();
                var result = new List<string>();
                var refer = s_TemplateReference.Matches(formatSpecification);
                foreach (Match match in refer)
                {
                    result.Add(match.Value);
                }

                return result;
            }
        }

        string ConstructPatternFormatSpecification()
        {
            if (string.IsNullOrEmpty(cachedPatternFormatSpecification))
            {
                cachedPatternFormatSpecification = ConstructFormatSpecification(Pattern);
            }

            return cachedPatternFormatSpecification;
        }

        string ConstructExpandedPatternFormatSpecification()
        {
            if (string.IsNullOrEmpty(cachedExpandedPatternFormatSpecification))
            {
                cachedExpandedPatternFormatSpecification = ConstructFormatSpecification(expandedPattern);
            }

            return cachedExpandedPatternFormatSpecification;
        }

        static string ConstructFormatSpecification(string rePattern)
        {
            return s_StripExpression.Replace(rePattern, @"{$1}");
        }

        Regex ConstructRegularExpression(string rePattern)
        {
            var placeholderCounts = new Dictionary<string, int>();
            var expression = s_EscapeExpression.Replace(rePattern, Escape);
            expression = s_ConvertPlaceholders.Replace(expression, match => Convert(match, placeholderCounts));

            if (expression.Contains(@"\{}"))
            {
                throw new ValueException($"Invalid pattern: {rePattern}");
            }

            if (Anchor == Anchor.Start || Anchor == Anchor.Exact)
            {
                expression = $"^{expression}";
            }

            if (Anchor == Anchor.End || Anchor == Anchor.Exact)
            {
                expression += "$";
            }
            if (Anchor == Anchor.Regex)
            {
                expression = rePattern;
            }

            Regex compiled;
            try
            {
                compiled = new Regex(expression);
            }
            catch
            {
                throw new ValueException($"Invalid pattern: {rePattern}");
            }

            return compiled;
        }

        string Convert(Match match, Dictionary<string, int> placeholderCount)
        {
            var placeholderName = match.Groups["placeholder"].Value;
            placeholderName = placeholderName.Replace("@", kAtCode);
            placeholderName = placeholderName.Replace(".", kPeriodCode);

            if (!placeholderCount.ContainsKey(placeholderName))
            {
                placeholderCount.Add(placeholderName, 0);
            }

            placeholderCount[placeholderName] += 1;

            placeholderName += placeholderCount[placeholderName].ToString("D3");
            string expression;
            if (match.Groups["expression"].Success)
            {
                expression = match.Groups["expression"].Value;
                if (s_ConventionExpressionMap.ContainsKey(expression))
                {
                    placeholderName += s_ConventionExpressionMap[expression];
                    expression = kDefaultPlaceholderExpression;
                }
                else
                {
                    placeholderName += "_EXPR";
                }
            }
            else
            {
                if (DefaultValueConvention != StringConvention.None)
                {
                    foreach (var kvp in s_ConventionGroupSuffixMap)
                    {
                        if ((int) kvp.Value == (int) DefaultValueConvention)
                        {
                            placeholderName += kvp.Key;
                            break;
                        }
                    }
                }

                expression = DefaultPlaceholderExpression;
            }

            expression = expression.Replace(@"\{", "{").Replace(@"\}", "}");

            return $"(?<{placeholderName}>{expression})";
        }

        static string Escape(Match match)
        {
            var other = match.Groups["other"];
            return other.Success ? Regex.Escape(other.Value) : match.Groups["placeholder"].Value;
        }
    }
}