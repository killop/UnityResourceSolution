using System;
using System.IO;

namespace MHLab.Patch.Core.IO
{
    public static class PathsManager
    {
        public static string GetDirectoryPath(string path)
        {
            return Path.GetDirectoryName(path);
        }

        public static string GetDirectoryParent(string path)
        {
            return Directory.GetParent(path).FullName;
        }

        public static string GetFilename(string path)
        {
            return Path.GetFileName(path);
        }

        public static string Combine(params string[] paths)
        {
            return Path.Combine(paths);
        }

        public static string Combine(string path1, string path2)
        {
            return Path.Combine(path1, path2);
        }

        public static string UriCombine(params string[] uriParts)
        {
            string uri = string.Empty;
            if (uriParts != null && uriParts.Length > 0)
            {
                char[] trims = new char[] { '\\', '/' };
                uri = (uriParts[0] ?? string.Empty).TrimEnd(trims);
                for (int i = 1; i < uriParts.Length; i++)
                {
                    uri = string.Format("{0}/{1}", uri.TrimEnd(trims), (uriParts[i] ?? string.Empty).TrimStart(trims));
                }
            }
            return uri;
        }

        public static string GetSpecialPath(Environment.SpecialFolder specialFolder)
        {
            return Environment.GetFolderPath(specialFolder);
        }
    }
}
