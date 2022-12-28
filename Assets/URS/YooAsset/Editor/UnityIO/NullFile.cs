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

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityIO.Interfaces;

namespace UnityIO.Classes
{
    public class NullFile : IDirectory, IFile, IFiles
    {
        public static NullFile SHARED_INSTANCE = new NullFile();

        public string path
        {
            get { return "Null"; }
        }

        public string systemPath
        {
            get { return "Null"; }
        }

        public IDirectory this[string name]
        {
            get
            {
                return SHARED_INSTANCE;
            }
        }

        public IDirectory directory
        {
            get
            {
                return SHARED_INSTANCE;
            }
        }

        public IDirectory parent
        {
            get { return this; }
        }

        public int Count
        {
            get
            {
                return 0;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public string name
        {
            get
            {
                return string.Empty;
            }
        }

        public string nameWithoutExtension
        {
            get
            {
                return string.Empty;
            }
        }

        public string extension
        {
            get
            {
                return string.Empty;
            }
        }

        public IFile this[int index]
        {
            get
            {
                return null;
            }

            set
            {
                
                // Nothing
            }
        }

        public IDirectory CreateDirectory(string name)
        {
            return SHARED_INSTANCE;
        }

        public void Delete()
        {
        }

        public void DeleteSubDirectory(string directroyName)
        {
        }

        public bool SubDirectoryExists(string directoryName)
        {
            return false;
        }



        public void Duplicate()
        {
        }

        public void Duplicate(string newName)
        {
        }

        public IDirectory GetDirectory(string name)
        {
            return SHARED_INSTANCE;
        }

        public IFiles GetFiles()
        {
            return SHARED_INSTANCE;
        }

        public IFiles GetFiles(string filter)
        {
            return SHARED_INSTANCE;
        }

        public IFiles GetFiles(bool recursive)
        {
            return SHARED_INSTANCE;
        }

        public IFiles GetFiles(string filter, bool recursive)
        {
            return SHARED_INSTANCE;
        }

        public IFiles GetFiles<T>() where T : UnityEngine.Object
        {
            return SHARED_INSTANCE;
        }

        public IFiles GetFiles<T>(string filter) where T : UnityEngine.Object
        {
            return SHARED_INSTANCE;
        }

        public IFiles GetFiles<T>(bool recursive) where T : UnityEngine.Object
        {
            return SHARED_INSTANCE;
        }

        public IFiles GetFiles<T>(string filter, bool recursive)
        {
            return SHARED_INSTANCE;
        }

        public IDirectory IfSubDirectoryExists(string name)
        {
            return SHARED_INSTANCE;
        }

        public IDirectory IfSubDirectoryDoesNotExist(string directoryName)
        {
            return SHARED_INSTANCE;
        }

        public IDirectory IfEmpty(bool assetsOnly)
        {
            return SHARED_INSTANCE;
        }

        public bool IsEmpty(bool assetsOnly)
        {
            return true;
        }

        public IFile IfFileExists(string name)
        {
            return SHARED_INSTANCE;
        }

        public IDirectory IfNotEmpty(bool assetsOnly)
        {
            return SHARED_INSTANCE;
        }

        public void Move(string moveDirectory)
        {

        }

        public void Move(IDirectory moveDirectory)
        {

        }

        public void Rename(string newName)
        {
        }

        IFile IFile.Duplicate()
        {
            return SHARED_INSTANCE;
        }

        IFile IFile.Duplicate(string newName)
        {
            return SHARED_INSTANCE;
        }

        public IEnumerator<IDirectory> GetEnumerator()
        {
            yield return null;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            yield return null;
        }

        public UnityEngine.Object LoadAsset()
        {
            return null;
        }

        public T LoadAsset<T>() where T : UnityEngine.Object
        {
            return null;
        }

        public IList<T> LoadAllofType<T>() where T : UnityEngine.Object
        {
            return null;
        }

        public IFile FirstOrDefault()
        {
            return null;
        }

        public int IndexOf(IFile item)
        {
            return 0;
        }

        public void Insert(int index, IFile item)
        {
            
        }

        public void RemoveAt(int index)
        {
           
        }

        public void Add(IFile item)
        {
           
        }

        public void Clear()
        {
            
        }

        public bool Contains(IFile item)
        {
            return false;
        }

        public void CopyTo(IFile[] array, int arrayIndex)
        {
            
        }

        public bool Remove(IFile item)
        {
            return false;
        }

        IEnumerator<IFile> IEnumerable<IFile>.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public IFile CreateFile<T>(string name, T asset) where T : UnityEngine.Object
        {
            return SHARED_INSTANCE;
        }

        public int Compare(IDirectory x, IDirectory y)
        {
            return -10;
        }

        public bool Equals(IDirectory other)
        {
            return other is NullFile;
        }
    }
}