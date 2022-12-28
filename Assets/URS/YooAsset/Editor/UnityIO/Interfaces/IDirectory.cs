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
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace UnityIO.Interfaces
{
    public interface IDirectory : IEnumerable<IDirectory>, IComparer<IDirectory>, IEquatable<IDirectory>
    {
        string path { get; }

        /// <summary>
        /// Gets the system path of this file. 
        /// </summary>
        string systemPath { get; }

        /// <summary>
        /// Returns just the name of the current directory.
        /// </summary>
        string name { get; }

        /// <summary>
        /// Get's the parent of this directory or a null file if this
        /// is the root. 
        /// </summary>
        IDirectory parent { get; }

        void Delete();
        void Duplicate();
        void Duplicate(string newName);

        void Move(string targetDirectory);
        void Move(IDirectory targetDirectory);

        void Rename(string newName);

        void DeleteSubDirectory(string directroyName);
        bool SubDirectoryExists(string directoryName);

        bool IsEmpty(bool assetsOnly);

        // Directories
        IDirectory this[string name] { get; }
        IDirectory CreateDirectory(string name);
       


        // Conditionals 
        IDirectory IfSubDirectoryExists(string name);
        IDirectory IfSubDirectoryDoesNotExist(string name);
        IDirectory IfEmpty(bool assetsOnly);
        IDirectory IfNotEmpty(bool assetsOnly);

        // IFIle
        IFile IfFileExists(string name);
        IFile CreateFile<T>(string name, T asset) where T : Object;

        // IFiles
        IFiles GetFiles();
        IFiles GetFiles(bool recursive);
        IFiles GetFiles(string filter);
        IFiles GetFiles(string filter, bool recursive);
    }
}
