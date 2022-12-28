using System;
using System.Collections.Generic;
using System.IO;

namespace MHLab.Patch.Core.IO
{
    public interface IFileSystem
    {
        void CreateDirectory(FilePath path);

        FilePath GetDirectoryPath(FilePath path);

        FilePath GetCurrentDirectory();

        bool IsDirectoryEmpty(FilePath path);

        void DeleteDirectory(FilePath path);

        FilePath GetApplicationDataPath(string folderName);

        FilePath CombinePaths(params string[] paths);

        FilePath CombinePaths(string path1, string path2);

        FilePath CombineUri(params string[] uris);

        FilePath SanitizePath(FilePath path);

        Stream CreateFile(FilePath path);

        string GetFilename(FilePath path);

        FilePath[] GetFilesList(FilePath path);

        LocalFileInfo[] GetFilesInfo(FilePath path);

        void GetFilesInfo(FilePath path, out LocalFileInfo[] fileInfo, out Dictionary<string, LocalFileInfo> fileInfoMap);

        LocalFileInfo GetFileInfo(FilePath path);

        Stream GetFileStream(FilePath path, FileMode fileMode, FileAccess fileAccess, FileShare fileShare);

        string ReadAllTextFromFile(FilePath path);

        void WriteAllTextToFile(FilePath path, string content);

        byte[] ReadAllBytesFromFile(FilePath path);

        void WriteAllBytesToFile(FilePath path, byte[] content);

        bool FileExists(FilePath path);

        void CopyFile(FilePath sourcePath, FilePath destinationPath);

        void MoveFile(FilePath sourcePath, FilePath destinationPath);

        void RenameFile(FilePath sourcePath, FilePath destinationPath);

        bool IsFileLocked(FilePath path);

        void UnlockFile(FilePath path);

        bool IsDirectoryWritable(FilePath path, bool throwOnFail = false);

        int DeleteMultipleFiles(FilePath path, string pattern);

        int DeleteTemporaryDeletingFiles(FilePath path);

        string GetTemporaryDeletingFileName(FilePath path);

        void DeleteFile(FilePath path);

        long GetAvailableDiskSpace(FilePath path);

        void SetFileAttributes(FilePath path, FileAttributes attributes);

        void SetLastWriteTime(FilePath path, DateTime date);

        void EnsureShortcutOnDesktop(FilePath path, string shortcutName);

        void DirectoryRename(string directoryPath, string name);

        void EnsureFileDirectory(string filePath);


    }
}