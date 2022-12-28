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
using UnityIO.Exceptions;
using sIO = System.IO;
using UnityIO.AssetDatabaseWrapper;

namespace UnityIO.Classes
{
    public class File : IFile
    {
        private string m_Directory;
        private string m_Extension;
        private string m_FileName;

        /// <summary>
        /// Gets the full path to this file starting from the Assets folder. 
        /// </summary>
        public string path
        {
            get { return m_Directory + "/" + m_FileName + m_Extension; }
        }

        /// <summary>
        /// Gets the path to this file starting from the root of your system.
        /// </summary>
        public string systemPath
        {
            get { return IO.AssetPathToSystemPath(path); }
        }

        /// <summary>
        /// Gets the name of this file with it's extension.
        /// </summary>
        public string name
        {
            get { return m_FileName + m_Extension; }
        }

        /// <summary>
        /// Gets the name of this file without it's extension.
        /// </summary>
        public string nameWithoutExtension
        {
            get { return m_FileName; }
        }

        /// <summary>
        /// Gets the extension of this file. 
        /// </summary>
        public string extension
        {
            get { return m_Extension; }
        }

        /// <summary>
        /// Creates a new instance of a file. 
        /// </summary>
        public File(string path)
        {
            m_Extension = sIO.Path.GetExtension(path);
            m_FileName = sIO.Path.GetFileNameWithoutExtension(path);
            m_Directory = sIO.Path.GetDirectoryName(path);
        }

        /// <summary>
        /// Returns the directory that this file exists in.
        /// </summary>
        public IDirectory directory
        {
            get
            {
                return new Directory(m_Directory);
            }
        }

        /// <summary>
        /// Delete this file from disk.
        /// </summary>
        public void Delete()
        {
            // Deletes the asset
            AssetDatabase.DeleteAsset(path);
        }

        public IFile Duplicate()
        {
            // Get our path. 
            string copyDir = AssetDatabase.GenerateUniqueAssetPath(path);
            // Copy our asset
            AssetDatabase.CopyAsset(path, copyDir);
            // Return new IFile
            return new File(copyDir);
        }

        /// <summary>
        /// Creates a copy of this file with a new name. The new name should not contain the extension
        /// that will be preserved automatically. 
        /// </summary>
        /// <param name="newName">The new name of the file (excluding the extension)</param>
        /// <returns></returns>
        public IFile Duplicate(string newName)
        {
            if(string.IsNullOrEmpty(newName))
            {
                throw new System.ArgumentNullException("You can't send a empty or null string to rename an asset. Trying to rename " + path);
            }
            // Make sure we don't have an extension. 
            if(!string.IsNullOrEmpty(sIO.Path.GetExtension(newName)))
            {
                throw new InvalidNameException("When you duplicate an asset it should not have an extension " + newName);
            }
            // Make sure it's a valid name. 
            if (!AssetDatabase.IsValidFileName(newName))
            {
                throw new InvalidNameException("The name '" + newName + "' contains invalid characters");
            }
            // Get our current directory
            string directory = System.IO.Path.GetDirectoryName(path);
            // and the extension
            string extension = System.IO.Path.GetExtension(path);
            // Get our path. 
            string copyDir = AssetDatabase.GenerateUniqueAssetPath(directory + "/" + newName + extension);
            // Copy our asset
            AssetDatabase.CopyAsset(path, copyDir);
            // Return new IFile
            return new File(copyDir);
        }

        /// <summary>
        /// Moves the files from it's current directory to another. 
        /// </summary>
        /// <param name="directroy">The directory you want to move it too</param>
        public void Move(IDirectory targetDirectory)
        {
            Move(targetDirectory.path);
        }

        /// <summary>
        /// Moves the files from it's current directory to another. 
        /// </summary>
        /// <param name="directroy">The directory you want to move it too</param>
        public void Move(string targetDirectory)
        {
            // Make sure we have a valid path
            IO.ValidatePath(targetDirectory);
            // And the directory exists
            if(!AssetDatabase.IsValidFolder(targetDirectory))
            {
                throw new DirectoryNotFoundException("Unable to find the directory at " + targetDirectory);
            }

            // Get the current name of our file.
            string name = System.IO.Path.GetFileName(path);

            // Append the name to the end. Move can't rename.
            targetDirectory = targetDirectory + "/" + name;

            // Check to see if there will be an error.
            string error = AssetDatabase.ValidateMoveAsset(path, targetDirectory);

            // CHeck
            if (!string.IsNullOrEmpty(error))
            {
                // We messed up.
                throw new MoveException(error, path, targetDirectory);
            }
            else
            {
                // Move it we are good to go.
                AssetDatabase.MoveAsset(path, targetDirectory);
            }
        }

        /// <summary>
        /// Renames this file to a new name. 
        /// </summary>
        public void Rename(string newName)
        {
            if (!UnityEditorInternal.InternalEditorUtility.IsValidFileName(newName))
            {
                throw new InvalidNameException("The name '" + newName + "' contains invalid characters");
            }

            if (newName.Contains("/"))
            {
                throw new RenameException("Rename can't be used to change a files location use Move(string newPath) instead.", path, newName);
            }

            string tempPath = m_Directory + "/" + newName;

            Object preExistingAsset = AssetDatabase.LoadAssetAtPath<Object>(tempPath);

            if (preExistingAsset != null)
            {
                throw new FileAlreadyExistsException("Rename can't be completed since an asset already exists with that name at path " + tempPath);
            }

            AssetDatabase.RenameAsset(tempPath, newName);
        }

        /// <summary>
        /// Loads the Unity asset at the files path. 
        /// </summary>
        /// <returns>Returns the asset</returns>
        public UnityEngine.Object LoadAsset()
        {
            return AssetDatabase.LoadAssetAtPath(path, typeof(Object));
        }

        /// <summary>
        /// Loads the Unity asset at the files path. 
        /// </summary>
        /// <returns>Returns the asset</returns>
        public T LoadAsset<T>() where T : UnityEngine.Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }
    }
}
