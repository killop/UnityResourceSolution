using System;
using System.Collections.Generic;
using System.Text;
using Daihenka.AssetPipeline.NamingConvention;

namespace Daihenka.AssetPipeline
{
    [Serializable]
    public class NamingConventionRule
    {
        static readonly StringBuilder s_StringBuilder = new StringBuilder();

        Template m_Template;
        public ITemplateResolver resolver;
        public string name;
        public string pattern;
        public Anchor anchor;

        public string ExpandedPattern => Template.expandedPattern;

        public Template Template
        {
            get
            {
                if (m_Template == null || m_Template.Name != name || m_Template.Pattern != pattern || m_Template.Anchor != anchor || m_Template.DefaultValueConvention != AssetPipelineSettings.Settings.DefaultPathVariableConvention)
                {
                    m_Template = new Template(name, pattern, AssetPipelineSettings.Settings.DefaultPathVariableConvention, anchor, templateResolver: resolver);
                }

                return m_Template;
            }
        }

        public List<string> Variables => Template.Keys;
        public Dictionary<string, string> VariableDictionary => Template.KeysDictionary();
        public List<string> References => Template.References;

        public bool IsValid
        {
            get
            {
                try
                {
                    return anchor == Anchor.AllPaths || !string.IsNullOrEmpty(ExpandedPattern);
                }
                catch
                {
                    return false;
                }
            }
        }

        public TemplateKeys VariableConventions => Template.KeyConventions();

        public bool IsMatch(string path, bool ignoreConventions = false)
        {
            if (anchor == Anchor.AllPaths)
            {
                return true;
            }

            var t = Template;
            TemplateData data;
            try
            {
                data = t.Parse(path);
            }
            catch
            {
                return false;
            }

            var templateKeys = t.Keys;
            if (data.Keys.Count != templateKeys.Count)
            {
                return false;
            }

            for (var i = 0; i < templateKeys.Count; i++)
            {
                if (!data.ContainsKey(templateKeys[i]))
                {
                    return false;
                }
            }

            return ignoreConventions || !data.HasFailures;
        }

        public TemplateData Parse(string path, bool ignoreConventions = false)
        {
            if (anchor == Anchor.AllPaths)
            {
                return new TemplateData();
            }

            var t = Template;
            TemplateData data;
            try
            {
                data = t.Parse(path);
            }
            catch
            {
                throw new FormatException($"Path {path} does not match pattern {pattern}");
            }

            var templateKeys = t.Keys;
            if (data.Keys.Count != templateKeys.Count)
            {
                throw new FormatException($"Path {path} does not match pattern {pattern}");
            }

            for (var i = 0; i < templateKeys.Count; i++)
            {
                if (!data.ContainsKey(templateKeys[i]))
                {
                    throw new FormatException($"Path {path} does not match pattern {pattern}");
                }
            }

            if (ignoreConventions || !data.HasFailures)
            {
                return data;
            }

            s_StringBuilder.Clear();
            s_StringBuilder.AppendLine($"Path {path} has parameters that do not match the specified naming conventions:");
            AddsInvalidDataToStringBuilder(data);
            throw new FormatException(s_StringBuilder.ToString());
        }

        public string Format(Dictionary<string, string> data, bool convertParametersToConventions = false)
        {
            var t = Template;

            var validatedData = t.ValidateData(data);
            if (convertParametersToConventions && validatedData.HasFailures)
            {
                var keys = validatedData.Keys;
                foreach (var key in keys)
                {
                    if (validatedData[key].isValid)
                    {
                        continue;
                    }

                    data[key] = data[key].ToConvention(validatedData[key].convention);
                }
            }

            if (validatedData.HasFailures)
            {
                s_StringBuilder.Clear();
                s_StringBuilder.AppendLine("Data has parameters that do not match the specified naming conventions:");
                AddsInvalidDataToStringBuilder(validatedData);
                throw new FormatException(s_StringBuilder.ToString());
            }

            return Template.Format(data);
        }

        public NamingConventionRule() : this("", "")
        {
        }

        public NamingConventionRule(string name, string pattern, Anchor anchor = Anchor.Start, ITemplateResolver resolver = null)
        {
            this.name = name;
            this.pattern = pattern;
            this.anchor = anchor;
            this.resolver = resolver ?? new GenericResolver();
        }

        void AddsInvalidDataToStringBuilder(TemplateData data)
        {
            foreach (var kvp in data)
            {
                if (!kvp.Value.isValid)
                {
                    s_StringBuilder.AppendLine($"{{{kvp.Key}}} {kvp.Value.value} does not conform to {kvp.Value.convention}");
                }
            }
        }
    }

    internal static class StringConventionUtility
    {
        public static StringConventionFlags ToFlag(this StringConvention value)
        {
            return (StringConventionFlags) value;
        }

        public static StringConvention ToNonFlag(this StringConventionFlags value)
        {
            return (StringConvention) value;
        }
    }

    [Serializable]
    public class VariableConventionData
    {
        public string name;
        public StringConvention convention;

        public VariableConventionData(string name, StringConvention convention)
        {
            this.name = name;
            this.convention = convention;
        }
    }
}