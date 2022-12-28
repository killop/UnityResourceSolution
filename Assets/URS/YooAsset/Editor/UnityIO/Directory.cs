/*>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
UnityIO was released with an MIT License.
Arther: Byron Mayne
Twitter: @ByMayne


MIT License

Copyright(c) 2016 Byron Mayne

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>*/

using UnityIO.Interfaces;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Collections;
using System.Collections.Generic;
using sIO = System.IO;
using UnityIO.Exceptions;
using UnityIO.AssetDatabaseWrapper;

namespace UnityIO.Classes
{
    public class Directory : IDirectory
    {
        private string m_Path;

        /// <summary>
        /// Gets the full path to this directory starting from the root of the 
        /// Unity project. 
        /// </summary>
        public string path
        {
            get { return m_Path; }
        }

        /// <summary>
        /// Gets the location of this directory starting from the root of the computer.
        /// </summary>
        public string systemPath
        {
            get { return IO.AssetPathToSystemPath(m_Path); }
        }

        /// <summary>
        /// Gets the name of this directory and not it's path or full path.
        /// </summary>
        public string name
        {
            get
            {
                // Get the starting index of the last slash
                int start = m_Path.LastIndexOf(IO.PATH_SPLITTER) + 1;
                // Get the total length of our name
                int length = m_Path.Length - start;
                // Return just the name. 
                return m_Path.Substring(start, length);
            }
        }

        /// <summary>
        /// Creates a new Directory objects.
        /// </summary>
        /// <param name="directoryPath"></param>
        public Directory(string directoryPath)
        {
            IO.ValidatePath(directoryPath);
            m_Path = directoryPath;
        }

        /// <summary>
        /// Returns back a sub directory of this directory if it exists 
        /// otherwise returns a null file object. 
        /// </summary>
        /// <param name="name">The directory you want to find</param>
        /// <returns>The sub directory or a null directory object</returns>
        public IDirectory this[string directoryPath]
        {
            get
            {
                IO.ValidatePath(directoryPath);
                if (SubDirectoryExists(directoryPath))
                {
                    return new Directory(m_Path + IO.PATH_SPLITTER + directoryPath);
                }
                else
                {
                    throw  new DirectoryNotFoundException("UnityIO: A directory was not found at " + directoryPath);
                }
            }
        }

        /// <summary>
        /// Returns back the parent of this directory and if there is no parent
        /// this returns back the null file.
        /// </summary>
        public IDirectory parent
        {
            get
            {
                // Get the last index of the slash 
                int directoryStart = m_Path.LastIndexOf(IO.PATH_SPLITTER);
                // Make sure it's greater then -1 meaning it has a slash  
                if (directoryStart > 0)
                {
                    int pathLength = m_Path.Length;
                    // Get the newLenght 
                    int newLength = (pathLength - (pathLength - directoryStart));
                    // Create our new path 
                    return new Directory(m_Path.Substring(0, newLength));
                }

                return NullFile.SHARED_INSTANCE;
            }
        }



        /// <summary>
        /// Creates the directory on disk if it does not already exist. If sent in a nested directory the
        /// full path will be created. 
        /// </summary>
        /// <param name="directoryPath">The path to the directory that you want to create.</param>
        /// <returns></returns>
        public IDirectory CreateDirectory(string directoryPath)
        {
            string workingPath = m_Path;
            IDirectory directory = null;
            if (!SubDirectoryExists(directoryPath))
            {
                string[] paths = directoryPath.Split(IO.PATH_SPLITTER);
                for (int i = 0; i < paths.Length; i++)
                {
                    if (!SubDirectoryExists(workingPath + IO.PATH_SPLITTER + paths[i]))
                    {
                        AssetDatabase.CreateFolder(workingPath, paths[i]);
                    }
                    workingPath += IO.PATH_SPLITTER + paths[i];
                }
                directory = new Directory(workingPath);
            }
            else
            {
                directory = new Directory(m_Path + IO.PATH_SPLITTER + directoryPath);
            }
       
            return directory;
        }

        /// <summary>
        /// Creates a new file in this directory with the name an extension
        /// sent in.
        /// </summary>
        /// <typeparam name="T">The type of Unity asset you want to create.</typeparam>
        /// <param name="name">The name of the asset and extension that you want to call it.</param>
        /// <param name="asset">The asset data itself.</param>
        /// <returns>The new IFile.</returns>
        public IFile CreateFile<T>(string name, T asset) where T : Object
        {
            File newFile = new File(m_Path + name);
            AssetDatabase.CreateAsset(asset, newFile.path);
            return newFile;
        }

        /// <summary>
        /// Deletes this directory and all it's sub directories and children. 
        /// </summary>
        public void Delete()
        {
            AssetDatabase.DeleteAsset(m_Path);
        }

        /// <summary>
        /// Finds a sub directory of this directory and deletes it if
        /// it does exist otherwise has no effect. 
        /// </summary>
        /// <param name="directroyName">The sub directory you want to delete.</param>
        public void DeleteSubDirectory(string directroyName)
        {
            IDirectory directoryToDelete = this[directroyName];
            directoryToDelete.Delete();
        }

        /// <summary>
        /// Our internal function which is used by all of the GetFilesFunctions. Used to search
        /// for files in the current directory. 
        /// </summary>
        /// <param name="filter">Which filter should be used to search</param>
        /// <param name="recursive">If we should also search sub directories.</param>
        /// <returns></returns>
        private IFiles GetFiles_Internal(string filter, bool recursive)
        {
            sIO.SearchOption options;
            if (recursive)
            {
                options = sIO.SearchOption.AllDirectories;
            }
            else
            {
                options = sIO.SearchOption.TopDirectoryOnly;
            }

            string systemPath = Application.dataPath.Replace("Assets", m_Path);

            IFiles iFiles = new Files();

            string[] serachResult = sIO.Directory.GetFiles(systemPath, filter, options);
            for (int i = 0; i < serachResult.Length; i++)
            {
                if (!serachResult[i].EndsWith(".meta"))
                {
                    string unityPath = AssetDatabase.GetProjectRelativePath(serachResult[i]);
                    iFiles.Add(new File(unityPath));
                }
            }

            return iFiles;
        }

        /// <summary>
        /// Gets all the Unity files that are at the top level of this directory.
        /// </summary>
        public IFiles GetFiles()
        {
            return GetFiles_Internal("*", recursive:false);
        }

        /// <summary>
        /// Gets all the Unity files that are at the top level of this directory with a filter.
        /// </summary>
        public IFiles GetFiles(string filter)
        {
            return GetFiles_Internal(filter, recursive:false);
        }

        /// <summary>
        /// Gets all the Unity files that are in this directory with an option to look recessively.
        /// </summary>
        public IFiles GetFiles(bool recursive)
        {
            return GetFiles_Internal("*", recursive);
        }

        /// <summary>
        /// Gets all the Unity files that are in this directory with an option to look recessively with a filter.
        /// </summary>
        public IFiles GetFiles(string filter, bool recursive)
        {
            return GetFiles_Internal(filter, recursive);
        }

        /// <summary>
        /// If the directory exists this will return that directory. If the directory does not
        /// exist it will return a NullFile directory which will make all the following function
        /// calls not have any effect. This allows you to chain requests and only continue execution
        /// if the directory exists. 
        /// </summary>
        /// <param name="directoryPath">The path to the directory you are trying to find.</param>
        /// <returns>The IDirectory class or a NullFile if ti does not exist.</returns>
        public IDirectory IfSubDirectoryExists(string directoryPath)
        {
            if (SubDirectoryExists(directoryPath))
            {
                return this[directoryPath];
            }
            else
            {
                return NullFile.SHARED_INSTANCE;
            }
        }

        /// <summary>
        /// If the directory does not exist this will return the current directory otherwise if it
        /// does it will return a null file. 
        /// </summary>
        /// <param name="directoryPath">The path to the directory you are trying to find.</param>
        /// <returns>This directory class or a NullFile if ti does exist.</returns>
        public IDirectory IfSubDirectoryDoesNotExist(string directoryPath)
        {
            if (!SubDirectoryExists(directoryPath))
            {
                return this;
            }
            else
            {
                return NullFile.SHARED_INSTANCE;
            }
        }

        /// <summary>
        /// Finds if this files exists in this directory. 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public IFile IfFileExists(string fileName)
        {
            // Get all files with our filter
            IFile file = GetFiles(filter:"*" + fileName + "*", recursive: false).FirstOrDefault();
            return file;
        }

        /// <summary>
        /// Returns true if the sub directory exists and false 
        /// if it does not.
        /// </summary>
        /// <param name="directoryName">The directory you want to check if it exists.</param>
        /// <returns>True if it exists and false if it does not.</returns>
        public bool SubDirectoryExists(string directoryPath)
        {
            IO.ValidatePath(directoryPath);
            return AssetDatabase.IsValidFolder(m_Path + '/' + directoryPath);
        }

        /// <summary>
        /// Checks this directory to see if any assets are contained inside of it
        /// or any of it's sub folder. 
        /// </summary>
        /// <param name="assetOnly">If this directory contains only other empty sub directories it will be considered empty otherwise it will not be.</param>
        /// <returns>true if it's empty and false if it's not</returns>
        public bool IsEmpty(bool assetOnly = false)
        {
            // This is the only way in Unity to check if a folder has anything.
            int assetCount = AssetDatabase.FindAssets(string.Empty, new string[] { m_Path }).Length;

            if (!assetOnly)
            {
                assetCount += AssetDatabase.GetSubFolders(m_Path).Length;
            }

            return assetCount == 0;
        }

        /// <summary>
        /// Duplicates the current directory in the same place but gives it a unique
        /// name by adding an incrementing number at the end. 
        /// </summary>
        public void Duplicate()
        {
            string copyDir = AssetDatabase.GenerateUniqueAssetPath(m_Path);
            AssetDatabase.CopyAsset(m_Path, copyDir);
        }

        /// <summary>
        /// Duplicates a directory and renames it. The new name is the full name
        /// mapped from the root of the assets folder.
        /// </summary>
        public void Duplicate(string copyDirectroy)
        {
            string uniquePath = AssetDatabase.GenerateUniqueAssetPath(copyDirectroy);
            AssetDatabase.CopyAsset(m_Path, uniquePath);
        }

        /// <summary>
        /// Moves a directory from one path to another. If a directory of the 
        /// same name already exists there it will give it a unique name. 
        /// </summary>
        /// <param name="moveDirectroy">The directory you want to move this directory in too</param>
        public void Move(IDirectory targetDirectory)
        {
            Move(targetDirectory.path);
        }

        /// <summary>
        /// Moves a directory from one path to another. If a directory of the 
        /// same name already exists there it will give it a unique name. 
        /// </summary>
        /// <param name="moveDirectroy">The directory you want to move too</param>
        public void Move(string targetDirectory)
        {
            int start = targetDirectory.LastIndexOf('/');
            int length = targetDirectory.Length - start;
            string name = targetDirectory.Substring(start, length);

			if (!IO.IsValidFileName(name))
            {
                throw new InvalidNameException("The name '" + name + "' contains invalid characters");
            }

            string error = AssetDatabase.ValidateMoveAsset(m_Path, targetDirectory);

            if(!string.IsNullOrEmpty(error))
            {
                throw new MoveException(error, m_Path, targetDirectory);
            }
            else
            {
                AssetDatabase.MoveAsset(m_Path, targetDirectory);
            }
        }

        /// <summary>
        /// Renames our directory to the name of our choice.
        /// </summary>
        public void Rename(string newName)
        {
            // Make sure we sent an argument.
            if (string.IsNullOrEmpty(newName))
            {
                throw new System.ArgumentNullException("You can't send a empty or null string to rename an asset. Trying to rename " + m_Path);
            }

            // And it's a valid name./
            if (!IO.IsValidFileName(newName))
            {
                throw new InvalidNameException("The name '" + newName + "' contains invalid characters");
            }

            if (newName.Contains("/"))
            {
                throw new RenameException("Rename can't be used to change a files location use Move(string newPath) instead.", m_Path, newName);
            }

            int slashIndex = m_Path.LastIndexOf('/') + 1;
            string subPath = m_Path.Substring(0, slashIndex);
            string newPath = subPath + newName;

            Object preExistingAsset = AssetDatabase.LoadAssetAtPath<Object>(newPath);

            if(preExistingAsset != null)
            {
                throw new DirectroyAlreadyExistsException("Rename can't be completed since an asset already exists with that name at path " + newPath);
            }

            AssetDatabase.RenameAsset(m_Path, newName);
        }

        /// <summary>
        /// Returns this directory if it's empty otherwise it returns
        /// a null directory which will then ignore all other commands. 
        /// </summary>
        /// <param name="assetsOnly">If false sub directories folders will not count as content inside the folder. If true a folder
        /// just filled with empty folders will count as not being empty.</param>
        /// <returns>This directory if it's not empty otherwise a null file directory.</returns>
        public IDirectory IfEmpty(bool assetsOnly)
        {
            if(IsEmpty(assetsOnly))
            {
                return this;
            }
            else
            {
                return NullFile.SHARED_INSTANCE;
            }
        }

        /// <summary>
        /// Returns this directory if it's not empty otherwise it returns
        /// a null directory which will then ignore all other commands. 
        /// </summary>
        /// <param name="assetsOnly">If false sub directories folders will not count as content inside the folder. If true a folder
        /// just filled with empty folders will count as not being empty.</param>
        /// <returns>This directory if it's not empty otherwise a null file directory.</returns>
        public IDirectory IfNotEmpty(bool assetsOnly)
        {
            if (!IsEmpty(assetsOnly))
            {
                return this;
            }
            else
            {
                return NullFile.SHARED_INSTANCE;
            }
        }

        /// <summary>
        /// Loops over our directory recessively. 
        /// </summary>
        IEnumerator<IDirectory> IEnumerable<IDirectory>.GetEnumerator()
        {
            string[] subFolder = AssetDatabase.GetSubFolders(m_Path);
            yield return this;
            for (int i = 0; i < subFolder.Length; i++)
            {
                IEnumerable<IDirectory> enumerable = new Directory(subFolder[i]);
                IEnumerator<IDirectory> enumerator = enumerable.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    yield return enumerator.Current;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            string[] subFolder = AssetDatabase.GetSubFolders(m_Path);

            for (int i = 0; i < subFolder.Length; i++)
            {
                yield return new Directory(subFolder[i]);
            }
        }

        /// <summary>
        /// When converted to a string it will return it's full path.
        /// </summary>
        public override string ToString()
        {
            return m_Path;
        }

        /// <summary>
        /// Implementation of ICompair interface. 
        /// </summary>
        public int Compare(IDirectory x, IDirectory y)
        {
            return string.Compare(x.path, y.path);
        }

        /// <summary>
        /// Checks to see if the two directories point to the same path. Implementation of <see cref="IEquatable<IDirectory>"/>
        /// </summary>
        public bool Equals(IDirectory other)
        {
            return string.CompareOrdinal(path, other.path) == 0;
        }

        /// <summary>
        /// A test used to check if two directory classes point to the same class.
        /// </summary>
        public static bool operator ==(Directory lhs, Directory rhs)
        {
            return string.CompareOrdinal(lhs.path, rhs.path) == 0;
        }

        /// <summary>
        /// A test used to check if two directory classes don't point to the same type.
        /// </summary>
        public static bool operator !=(Directory lhs, Directory rhs)
        {
            return string.CompareOrdinal(lhs.path, rhs.path) == 0;
        }

        
        /// <summary>
        /// Allows us to explicitly convert a string to a new directory.
        /// </summary>
        public static explicit operator Directory(string directory)
        {
            return new Directory(directory);
        }

        /// <summary>
        /// Allows us to implicitly convert a directory to a string.
        /// </summary>
        public static implicit operator string(Directory directory)
        {
            return directory.path;
        }

        /// <summary>
        /// Checks to see if an object points to the same path. 
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is IDirectory)
            {
                return string.Compare(((IDirectory)obj).path, path) == 0;
            }
            return false;
        }

        /// <summary>
        /// Returns the hash code for this class.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return m_Path.GetHashCode();
        }

    }
}
