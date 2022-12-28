namespace MHLab.Patch.Core.IO
{
    public readonly struct FilePath
    {
        public readonly string BasePath;
        public readonly string FullPath;

        public FilePath(string basePath, string fullPath)
        {
            BasePath = basePath;
            FullPath = fullPath;
        }

        public static explicit operator FilePath(string path) => new FilePath(path, path);
    }
}