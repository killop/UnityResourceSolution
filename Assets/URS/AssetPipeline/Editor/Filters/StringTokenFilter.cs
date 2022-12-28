using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Daihenka.AssetPipeline.Filters
{
    [Serializable]
    public class StringTokenFilter
    {
        public string name;
        public string rulePattern;

        public int[] GetRegexGroups()
        {
            return new Regex(name).GetGroupNumbers();
        }

        public string GetName(string assetPath, string pattern = "")
        {
            rulePattern = pattern;
            var nameStr = name;
            nameStr = nameStr.Replace("(.)", Path.GetFileName(Path.GetDirectoryName(assetPath)));
            nameStr = nameStr.Replace("(assetName)", Path.GetFileNameWithoutExtension(assetPath));
            nameStr = nameStr.Replace("(assetExt)", Path.GetExtension(assetPath));
            var rulePathRegex = Regex.Match(assetPath, rulePattern);
            if (rulePathRegex.Success)
            {
                for (var i = 1; i < rulePathRegex.Groups.Count; i++)
                {
                    nameStr = nameStr.Replace($"(${i})", rulePathRegex.Groups[i].Value);
                }
            }

            var relativePathRegex = Regex.Match(nameStr, @"(\([\.\/]+\))");
            if (relativePathRegex.Success)
            {
                for (var i = 1; i < relativePathRegex.Groups.Count; i++)
                {
                    var test = relativePathRegex.Groups[i].Value;
                    test = test.Substring(1, test.Length - 2);
                    var testSplit = test.Split(new[] {@"/"}, StringSplitOptions.RemoveEmptyEntries);
                    var replacement = GetRelativeParentName(assetPath, testSplit.Length);
                    nameStr = nameStr.Replace(relativePathRegex.Groups[i].Value, replacement);
                }
            }

            return nameStr;
        }

        string GetRelativeParentName(string assetPath, int depth)
        {
            var dirInfo = new DirectoryInfo(Path.GetDirectoryName(assetPath));
            for (var i = 0; i < depth; i++)
            {
                dirInfo = dirInfo.Parent;
            }

            return dirInfo.Name;
        }
    }
}