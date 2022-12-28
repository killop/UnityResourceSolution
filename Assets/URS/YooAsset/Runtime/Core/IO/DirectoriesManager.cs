using System;
using System.IO;

namespace MHLab.Patch.Core.IO
{
    public static class DirectoriesManager
    {
        public static void Create(string path)
        {
            Directory.CreateDirectory(path);
        }

        public static void EnsureFileDirectory(string filePath)
        {
            var directoryName = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directoryName)) 
            {
                Directory.CreateDirectory(directoryName);
            }
        }

        public static string GetCurrentDirectory()
        {
            return Directory.GetCurrentDirectory();
        }

        public static bool IsEmpty(string path)
        {
            if (!Directory.Exists(path)) throw new DirectoryNotFoundException();

            string[] dirs = Directory.GetDirectories(path);
            string[] files = Directory.GetFiles(path);

            if (dirs.Length == 0 && files.Length == 0) return true;
            return false;
        }

        public static void Copy(string sourceFolder, string destFolder)
        {
            Create(destFolder);

            var files = Directory.GetFiles(sourceFolder, "*", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                string newFile = file.Replace(sourceFolder, destFolder);

                Create(Path.GetDirectoryName(newFile));
                File.Copy(file, newFile);
            }
        }

        public static bool Delete(string directory)
        {
            try
            {
                Clean(directory);
                Directory.Delete(directory);
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static void Clean(string path)
        {
            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                DirectoryInfo dir = new DirectoryInfo(path);

                foreach (FileInfo file in dir.GetFiles())
                {
                    file.Attributes &= ~FileAttributes.ReadOnly;
                    file.Delete();
                }

                foreach (DirectoryInfo subDirectory in dir.GetDirectories())
                {
                    DeleteRecursiveFolder(subDirectory.FullName);
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private static void DeleteRecursiveFolder(string pFolderPath)
        {
            foreach (string Folder in Directory.GetDirectories(pFolderPath))
            {
                DeleteRecursiveFolder(Folder);
            }

            foreach (string file in Directory.GetFiles(pFolderPath))
            {
                var pPath = Path.Combine(pFolderPath, file);
                FileInfo fi = new FileInfo(pPath);
                File.SetAttributes(fi.FullName, FileAttributes.Normal);
                File.Delete(fi.FullName);
            }
            Directory.Delete(pFolderPath);
        }

        public static void DirectoryRename(string direcoryPath, string name)
        {
            var di = new DirectoryInfo(direcoryPath);
            if (di == null)
            {
                throw new ArgumentNullException("di", "Directory info to rename cannot be null");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("New name cannot be null or blank", "name");
            }

            di.MoveTo(Path.Combine(di.Parent.FullName, name));

            return; //done
        }
    }
}
