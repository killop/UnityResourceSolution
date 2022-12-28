using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MHLab.Patch.Core.IO
{
    public class FileSystem : IFileSystem
    {
        public void CreateDirectory(FilePath path)
        {
            DirectoriesManager.Create(path.FullPath);
        }

        public FilePath GetDirectoryPath(FilePath path)
        {
            return (FilePath)PathsManager.GetDirectoryPath(path.FullPath);
        }

        public FilePath GetCurrentDirectory()
        {
            return (FilePath)FilesManager.SanitizePath(Directory.GetCurrentDirectory());
        }

        public bool IsDirectoryEmpty(FilePath path)
        {
            return DirectoriesManager.IsEmpty(path.FullPath);
        }

        public void DeleteDirectory(FilePath path)
        {
            DirectoriesManager.Delete(path.FullPath);
        }

        public FilePath GetApplicationDataPath(string folderName)
        {
            return (FilePath)FilesManager.SanitizePath(
                PathsManager.Combine(
                    PathsManager.GetSpecialPath(Environment.SpecialFolder.ApplicationData),
                    folderName
                )
            );
        }

        public FilePath CombinePaths(params string[] paths)
        {
            return (FilePath)PathsManager.Combine(paths);
        }

        public FilePath CombinePaths(string path1, string path2)
        {
            return (FilePath)PathsManager.Combine(path1, path2);
        }

        public FilePath CombineUri(params string[] uris)
        {
            return (FilePath)PathsManager.UriCombine(uris);
        }

        public FilePath SanitizePath(FilePath path)
        {
            return (FilePath)FilesManager.SanitizePath(path.FullPath);
        }

        public Stream CreateFile(FilePath path)
        {
            return File.Create(path.FullPath);
        }

        public string GetFilename(FilePath path)
        {
            return PathsManager.GetFilename(path.FullPath);
        }

        public FilePath[] GetFilesList(FilePath path)
        {
            return FilesManager.GetFiles(path.FullPath).Select(stringPath => (FilePath)stringPath).ToArray();
        }

        public LocalFileInfo[] GetFilesInfo(FilePath path)
        {
            return FilesManager.GetFilesInfo(path.FullPath);
        }

        public void GetFilesInfo(FilePath                              path, out LocalFileInfo[] fileInfo,
                                 out Dictionary<string, LocalFileInfo> fileInfoMap)
        {
            FilesManager.GetFilesInfo(path.FullPath, out fileInfo, out fileInfoMap);
        }

        public LocalFileInfo GetFileInfo(FilePath path)
        {
            return FilesManager.GetFileInfo(path.FullPath);
        }

        public Stream GetFileStream(FilePath path, FileMode fileMode = FileMode.OpenOrCreate, FileAccess fileAccess = FileAccess.ReadWrite, FileShare fileShare = FileShare.ReadWrite)
        {
            var stream = new FileStream(path.FullPath, fileMode, fileAccess, fileShare);
            return stream;
        }

        public string ReadAllTextFromFile(FilePath path)
        {
            return File.ReadAllText(path.FullPath);
        }

        public void WriteAllTextToFile(FilePath path, string content)
        {
            File.WriteAllText(path.FullPath, content);
        }

        public byte[] ReadAllBytesFromFile(FilePath path)
        {
            return File.ReadAllBytes(path.FullPath);
        }

        public void WriteAllBytesToFile(FilePath path, byte[] content)
        {
            File.WriteAllBytes(path.FullPath, content);
        }

        public bool FileExists(FilePath path)
        {
            return FilesManager.Exists(path.FullPath);
        }

        public void CopyFile(FilePath sourcePath, FilePath destinationPath)
        {
            FilesManager.Copy(sourcePath.FullPath, destinationPath.FullPath);
        }

        public void MoveFile(FilePath sourcePath, FilePath destinationPath)
        {
            FilesManager.Move(sourcePath.FullPath, destinationPath.FullPath);
        }

        public void RenameFile(FilePath sourcePath, FilePath destinationPath)
        {
            FilesManager.Rename(sourcePath.FullPath, destinationPath.FullPath);
        }

        public bool IsFileLocked(FilePath path)
        {
            return FilesManager.IsFileLocked(path.FullPath);
        }

        public void UnlockFile(FilePath path)
        {
            if (IsFileLocked(path) == false) return;

            var renamedPath = (FilePath)GetTemporaryDeletingFileName(path);
            
            if (FileExists(renamedPath)) DeleteFile(renamedPath);
            
            RenameFile(path, renamedPath);
            CopyFile(renamedPath, path);
        }

        public bool IsDirectoryWritable(FilePath path, bool throwOnFail = false)
        {
            return FilesManager.IsDirectoryWritable(path.FullPath, throwOnFail);
        }

        public int DeleteMultipleFiles(FilePath path, string pattern)
        {
            return FilesManager.DeleteMultiple(path.FullPath, pattern);
        }

        public int DeleteTemporaryDeletingFiles(FilePath folderPath)
        {
            return DeleteMultipleFiles(folderPath, "*.temp.delete_me");
        }

        public string GetTemporaryDeletingFileName(FilePath path)
        {
            return FilesManager.GetTemporaryDeletingFileName(path.FullPath);
        }

        public void DeleteFile(FilePath path)
        {
            FilesManager.Delete(path.FullPath);
        }

        public long GetAvailableDiskSpace(FilePath path)
        {
            return FilesManager.GetAvailableDiskSpace(path.FullPath);
        }

        public void SetFileAttributes(FilePath path, FileAttributes attributes)
        {
            File.SetAttributes(path.FullPath, attributes);
        }

        public void SetLastWriteTime(FilePath path, DateTime date)
        {
            File.SetLastWriteTime(path.FullPath, date);
            File.SetLastWriteTimeUtc(path.FullPath, date);
        }

        public void EnsureShortcutOnDesktop(FilePath path, string shortcutName)
        {
            FilesManager.EnsureShortcutOnDesktop(path.FullPath, shortcutName);
        }

        public void DirectoryRename(string directoryPath, string name) 
        {
            DirectoriesManager.DirectoryRename(directoryPath, name);
        }

        public void EnsureFileDirectory(string filePath)
        {
            DirectoriesManager.EnsureFileDirectory(filePath);
        }
    }
}