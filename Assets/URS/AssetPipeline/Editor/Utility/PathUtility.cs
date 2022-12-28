using System.IO;

namespace Daihenka.AssetPipeline
{
    internal static class PathUtility
    {
        public static void CreateParentDirectoryIfNeeded(string path)
        {
            CreateDirectoryIfNeeded(Directory.GetParent(path).FullName);
        }

        public static void CreateDirectoryIfNeeded(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public static string FixPathSeparators(this string path)
        {
            return path.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }
}