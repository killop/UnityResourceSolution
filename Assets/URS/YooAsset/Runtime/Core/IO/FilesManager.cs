using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MHLab.Patch.Core.IO
{
    public static class FilesManager
    {
        public static bool Exists(string path)
        {
            return File.Exists(path);
        }

        public static string[] GetFiles(string path, string pattern = "*")
        {
            return Directory.GetFiles(path, pattern, SearchOption.AllDirectories).Where(f => !IsFileOsRelated(f)).ToArray();
        }

        private static bool IsFileOsRelated(string filePath)
        {
            var fileName = Path.GetFileName(filePath);

            return fileName == ".DS_Store" ||
                   fileName == "desktop.ini";
        }

        public static LocalFileInfo[] GetFilesInfo(string rootPath)
        {
            var files = GetFiles(rootPath);
            var infos = new LocalFileInfo[files.Length];

            for (int i = 0; i < files.Length; i++)
            {
                var currentPath = files[i];
                var info = new FileInfo(currentPath);

                var localInfo = new LocalFileInfo();
                localInfo.Size = info.Length;
                localInfo.LastWriting = info.LastWriteTimeUtc;
                localInfo.Attributes = info.Attributes;
                localInfo.RelativePath = SanitizeToRelativePath(rootPath, currentPath);

                infos[i] = localInfo;
            }

            return infos;
        }

        public static void GetFilesInfo(string rootPath, out LocalFileInfo[] fileInfoArray, out Dictionary<string, LocalFileInfo> fileInfoMap)
        {
            var files = GetFiles(rootPath);
            fileInfoArray = new LocalFileInfo[files.Length];
            fileInfoMap = new Dictionary<string, LocalFileInfo>();

            for (int i = 0; i < files.Length; i++)
            {
                var currentPath = files[i];
                var info        = new FileInfo(currentPath);

                var localInfo = new LocalFileInfo();
                localInfo.Size         = info.Length;
                localInfo.LastWriting  = info.LastWriteTime;
                localInfo.Attributes   = info.Attributes;
                localInfo.RelativePath = SanitizeToRelativePath(rootPath, currentPath);

                fileInfoArray[i] = localInfo;
                fileInfoMap.Add(localInfo.RelativePath, localInfo);
            }
        }

        public static LocalFileInfo GetFileInfo(string filePath)
        {
            var info = new FileInfo(filePath);

            var localInfo = new LocalFileInfo();
            localInfo.Size = info.Length;
            localInfo.LastWriting = info.LastWriteTimeUtc;
            localInfo.Attributes = info.Attributes;
            localInfo.RelativePath = SanitizeToRelativePath(PathsManager.GetDirectoryPath(filePath), filePath);

            return localInfo;
        }

        public static string SanitizeToRelativePath(string rootPath, string fullPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath)) return fullPath;

            fullPath = SanitizePath(fullPath);
            rootPath = SanitizePath(rootPath);
            
            var relativePath = SanitizePath(fullPath.Replace(rootPath, string.Empty));
            if (relativePath.StartsWith("/"))
                relativePath = relativePath.Substring(1);

            return relativePath;
        }

        public static string SanitizePath(string path)
        {
            return path.Replace('\\', '/');
        }

        public static void Delete(string path)
        {
            if (!Exists(path)) return;

            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                File.Delete(path);
            }
            catch (DirectoryNotFoundException)
            {
                return;
            }
            catch (FileNotFoundException)
            {
                return;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Rename(path, GetTemporaryDeletingFileName(path));
            }
        }

        public static int DeleteMultiple(string directory, string pattern)
        {
            var files = GetFiles(directory, pattern);
            foreach (string file in files) Delete(file);
            return files.Length;
        }

        public static void Rename(string path, string newPath)
        {
            File.Move(path, newPath);
        }

        public static void Copy(string sourcePath, string destinationPath, bool overwrite = true)
        {
            DirectoriesManager.Create(PathsManager.GetDirectoryPath(destinationPath));
            File.Copy(sourcePath, destinationPath, overwrite);
        }

        public static void Move(string source, string dest)
        {
            try
            {
                DirectoriesManager.Create(Path.GetDirectoryName(dest));
                File.Move(source, dest);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static void EnsureShortcutOnDesktop(string targetFile, string shortcutName)
        {
            var shortcutFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), shortcutName + ".url");
            if(!Exists(shortcutFile)) CreateShortcut(targetFile, shortcutFile);
        }

        public static void CreateShortcut(string targetFile, string shortcutFile)
        {
            File.Delete(shortcutFile);
            using (var fs = new FileStream(shortcutFile, FileMode.Create, FileAccess.ReadWrite))
            {
                using (var writer = new StreamWriter(fs))
                {
                    writer.WriteLine("[InternetShortcut]");
                    writer.WriteLine("URL=file:///" + targetFile);
                    writer.WriteLine("IconIndex=0");
                    writer.WriteLine("WorkingDirectory=" + PathsManager.GetDirectoryPath(targetFile));
                    writer.WriteLine("IconFile=" + targetFile.Replace('\\', '/'));
                    writer.Flush();
                }
            }
        }

        public static bool IsDirectoryWritable(string directoryPath, bool throwOnFail = false)
        {
            try
            {
                DirectoriesManager.Create(directoryPath);
                var path = Path.Combine(directoryPath, Path.GetRandomFileName());
                using (var fileStream = File.Create(path, 1, FileOptions.DeleteOnClose))
                {

                }

                return true;
            }
            catch (UnauthorizedAccessException)
            {
                if (throwOnFail) throw;
                return false;
            }
        }

        public static bool IsFileLocked(string targetFile)
        {
            try
            {
                using (File.Open(targetFile, FileMode.Open))
                {
                }
            }
            catch (FileNotFoundException)
            {
                GC.WaitForPendingFinalizers();
                return false;
            }
            catch (IOException e)
            {
                const int ERROR_SHARING_VIOLATION = 0x20;
                const int ERROR_LOCK_VIOLATION = 0x21;
                int errorCode = e.HResult & 0x0000FFFF;
                return errorCode == ERROR_SHARING_VIOLATION || errorCode == ERROR_LOCK_VIOLATION;
            }

            GC.WaitForPendingFinalizers();
            return false;
        }

        public static string GetTemporaryDeletingFileName(string filePath)
        {
            return filePath + ".temp.delete_me";
        }

        public static int DeleteTemporaryDeletingFiles(string folderPath)
        {
            return DeleteMultiple(folderPath, "*.temp.delete_me");
        }

        public static long GetAvailableDiskSpace(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return 0;

            try
            {
                var driveInfo = new DriveInfo(path);

                if (driveInfo.IsReady)
                {
                    return driveInfo.AvailableFreeSpace;
                }
            }
            catch
            {
                return 0;
            }

            return 0;
        }
    }
}
