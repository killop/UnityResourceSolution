#if !UNITY_EDITOR
#define RELEASE_BUILD
#else
using UnityEditor;
using UnityEditorInternal;
using uAssetDatabase = UnityEditor.AssetDatabase;
#endif

using System;
using Object = UnityEngine.Object;
using UnityEngine;
using System.IO;

namespace UnityIO.AssetDatabaseWrapper
{
    internal static class AssetDatabase
    {
        /// <summary>
        /// Returns the root directory of our application <see cref="Application.dataPath"/>. In the editor
        /// this is the path to the root of the Unity project and on device
        /// this is the <see cref="Application.persistentDataPath"/>.
        /// </summary>
        public static string rootDirectory
        {
            get
            {
#if !RELEASE_BUILD
                return Application.dataPath;
#else
                return Application.persistentDataPath;
#endif
            }
        }

        /// <summary>
        /// Returns true if this path is relative to the current project
        /// and false if it's not. 
        /// </summary>
        public static bool IsRelativePath(string path)
        {
            return path.StartsWith(rootDirectory);
        }

        /// <summary>
        /// Returns the first asset object of type at given path assetPath.
        /// </summary>
        /// <typeparam name="T">The type you want to load</typeparam>
        /// <param name="path">The path of the asset</param>
        /// <returns>The loaded object</returns>
        public static T LoadAssetAtPath<T>(string assetPath) where T : Object
        {

            if(IsRelativePath(assetPath))
            {
                #if !RELEASE_BUILD
                return uAssetDatabase.LoadAssetAtPath<T>(assetPath);
                #endif
            }
            return null;
        }

        /// <summary>
        /// Returns the first asset object of type at given path assetPath.
        /// </summary>
        /// <param name="path">The path of the asset</param>
        /// <param name="type">The type you want to load</param>
        /// <returns>The loaded object</returns>
        public static Object LoadAssetAtPath(string assetPath, Type type)
        {
#if !RELEASE_BUILD
            return uAssetDatabase.LoadAssetAtPath(assetPath, type);
#else
             return null;
#endif
        }

        /// <summary>
        /// Deletes the asset file at path.
        // Returns true if the asset has been successfully deleted, false if it doesn't exit or couldn't be removed.
        /// </summary>
        public static bool DeleteAsset(string assetPath)
        {
#if !RELEASE_BUILD
            return uAssetDatabase.DeleteAsset(assetPath);
#else
            // Convert to system path
            string systemPath = IO.AssetPathToSystemPath(assetPath);
            // Check if it exists
            if(File.Exists(systemPath))
            {
                // Delete it
                File.Delete(systemPath);
                // Return true
                return true; 
            }
            else
            {
                return false;
            }
#endif

        }

        /// <summary>
        /// Creates a new unique path for an asset.
        /// </summary>
        public static string GenerateUniqueAssetPath(string assetPath)
        {
            // Get our system path since we need the full one. 
            string systemPath = IO.AssetPathToSystemPath(assetPath);
            // Create a holder for a unique one. 
            string uniquePath = systemPath;

            // Loop till max int (We should never have that many folder but we don't want to loop forever). 
            for (int i = 0; i < int.MaxValue; i++)
            {
                // If the file does not exist we can break. 
                if(!File.Exists(uniquePath))
                {
                    break;
                }
                // One with that name already exists so we append our number to the file name. 
                uniquePath = IO.AppendName(systemPath, " " + i.ToString());
            }
            return uniquePath;
        }

        /// <summary>
        /// Duplicates the asset at path and stores it at newPath.
        /// </summary>
        /// <returns>Returns true if the copy was successful.</returns>
        public static bool CopyAsset(string path, string newPath)
        {
#if !RELEASE_BUILD
            return uAssetDatabase.CopyAsset(path, newPath);
#else
             // Convert our path to a system path 
            string sourceFilePath = IO.AssetPathToSystemPath(path);
            // Get a unique one for our detestation path. 
            string destSystemPath = GenerateUniqueAssetPath(newPath);
            // Copy the file. 
            File.Copy(sourceFilePath, destSystemPath, false);
            // Return our result
            return true.
#endif
        }

        /// <summary>
        /// Returns true if the fileName is valid on the supported platform.
        /// </summary>
        /// <param name="fileName">The name of the file you want to check.</param>
        /// <returns>True if it's valid and false if it's not.</returns>
        public static bool IsValidFileName(string fileName)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();

            for(int i = 0; i < invalidChars.Length; i++)
            {
                if(fileName.IndexOf(invalidChars[i]) > -1)
                {
                    return false;
                }
            }

            return true; 
        }

        /// <summary>
        /// returns true if it exists, false otherwise false.
        /// </summary>
        public static bool IsValidFolder(string assetPath)
        {
#if !RELEASE_BUILD
            return uAssetDatabase.IsValidFolder(assetPath);
#else
            // Convert our path to a system path 
            string sourceFilePath = IO.AssetPathToSystemPath(assetPath);
            // Return if it exists or not.
            return Directory.Exists(sourceFilePath);
#endif
        }

        /// <summary>
        /// Checks if an asset file can be moved from one folder to another. (Without actually moving the file).
        /// </summary>
        /// <param name="oldPath">The path where the asset currently resides.</param>
        /// <param name="newPath">The path which the asset should be moved to.</param>
        /// <returns>An empty string if the asset can be moved, otherwise an error message.</returns>
        public static string ValidateMoveAsset(string oldPath, string newPath)
        {
#if !RELEASE_BUILD
            return uAssetDatabase.ValidateMoveAsset(oldPath, newPath);


#else
            oldPath = IO.AssetPathToSystemPath(oldPath);
            newPath = IO.AssetPathToSystemPath(newPath);

            if(File.Exists(newPath))
            {
                return "File Already Exists";
            }

            if(AssetDatabase.IsValidFileName(newPath))
            {
                return "Not valid file name";
            }

            return string.Empty;
#endif
        }

        /// <summary>
        /// Move an asset file from one folder to another.
        /// </summary>
        /// <param name="oldPath">The path where the asset currently resides.</param>
        /// <param name="newPath">The path which the asset should be moved to.</param>
        /// <returns></returns>
        public static string MoveAsset(string oldPath, string newPath)
        {
#if !RELEASE_BUILD
            return uAssetDatabase.MoveAsset(oldPath, newPath);
#else
            oldPath = IO.AssetPathToSystemPath(oldPath);
            newPath = IO.AssetPathToSystemPath(newPath);
            File.Move(oldPath, newPath);
#endif
        }

        /// <summary>
        /// Rename an asset file.
        /// </summary>
        /// <param name="pathName">The path where the asset currently resides.</param>
        /// <param name="newName">The new name which should be given to the asset.</param>
        /// <returns></returns>
        public static string RenameAsset(string pathName, string newName)
        {
#if !RELEASE_BUILD
            return uAssetDatabase.RenameAsset(pathName, newName);
#endif
            
        }

        /// <summary>
        /// Create a new folder.
        /// </summary>
        /// <param name="parentFolder">The name of the parent folder.</param>
        /// <param name="newFolderName">The name of the new folder.</param>
        /// <returns>The GUID of the newly created folder.</returns>
        public static string CreateFolder(string parentFolder, string newFolderName)
        {
#if !RELEASE_BUILD
            return uAssetDatabase.CreateFolder(parentFolder, newFolderName);
#endif
        }

        /// <summary>
        /// Search the asset database using the search filter string.
        /// </summary>
        /// <param name="filter">The filter string can contain search data. See below for details about this string.</param>
        /// <param name="searchInFolders">The folders where the search will start.</param>
        /// <returns>Array of matching asset. Note that GUIDs will be returned.</returns>
        public static string[] FindAssets(string filter, string[] searchInFolders)
        {
#if !RELEASE_BUILD
            return uAssetDatabase.FindAssets(filter, searchInFolders);
#endif
        }

        /// <summary>
        /// Given an absolute path to a directory, this method will return an array of all it's subdirectories.
        /// </summary>
        public static string[] GetSubFolders(string path)
        {
#if !RELEASE_BUILD
            return uAssetDatabase.GetSubFolders(path);
#endif
        }

        /// <summary>
        /// Converts a system path to a project local one.
        /// </summary>
        public static string GetProjectRelativePath(string path)
        {
#if !RELEASE_BUILD
            return FileUtil.GetProjectRelativePath(path);
#endif
        }

        /// <summary>
        /// Creates a new asset at path.
        /// You must ensure that the path uses a supported extension ('.mat' for materials, '.cubemap' for cubemaps, '.GUISkin' 
        /// for skins, '.anim' for animations and '.asset' for arbitrary other assets.)
        /// Note: Most of the uses of this function don't work on a build.
        /// </summary>
        public static void CreateAsset<T>(T asset, string path) where T : Object
        {
#if !RELEASE_BUILD
            uAssetDatabase.CreateAsset(asset, path);
#endif
        }
    }
}
