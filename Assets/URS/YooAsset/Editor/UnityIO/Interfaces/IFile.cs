/*>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
UnityIO was released with an MIT License.
Arther: Byron Mayne
Twitter: @ByMayne


IT License

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
using UnityEngine;

namespace UnityIO.Interfaces
{
    /// <summary>
    /// IFile is the root interface that we use to handle all file actions. <see cref="UnityIO.Classes.File"/> to
    /// see the implementation.
    /// </summary>
    public interface IFile
    {
        /// <summary>
        /// Gets the path to this file starting from the root of the Unity project. 
        /// </summary>
        string path { get; }

        /// <summary>
        /// Gets the name of this file with it's extension included.
        /// </summary>
        string name { get; }

        /// <summary>
        /// Gets the name of this file without it's extension.
        /// </summary>
        string nameWithoutExtension { get; }

        /// <summary>
        /// Gets the extension of this file.
        /// </summary>
        string extension { get; }

        /// <summary>
        /// Returns the back directory that this file exists in.
        /// </summary>
        IDirectory directory { get; }

        /// <summary>
        /// Deletes this file.
        /// </summary>
        void Delete();

        /// <summary>
        /// Renames the files throws an exception if a file already exists with the same name.
        /// </summary>
        /// <param name="name"></param>
        void Rename(string name);

        /// <summary>
        /// Moves a file from one location to another will force the name to be unique
        /// if a file already exists with the same name. 
        /// </summary>
        /// <param name="directory"></param>
        void Move(string directory);

        /// <summary>
        /// Moves a file from one location to another will force the name to be unique
        /// if a file already exists with the same name. 
        /// </summary>
        /// <param name="directory"></param>
        void Move(IDirectory directory);

        /// <summary>
        /// Copies the file on disk and will force it to have a unique name (appending a number to
        /// the end.
        /// </summary>
        IFile Duplicate();

        /// <summary>
        /// Copies the file on disk and renames it. Name must be unique or an exception is thrown. 
        /// </summary>
        IFile Duplicate(string newName);

        /// <summary>
        /// Loads the <see cref="UnityEngine.Object"/> asset that this file points too. 
        /// </summary>
        /// <returns></returns>
        Object LoadAsset();

        /// <summary>
        /// Load a type of <see cref="UnityEngine.Object"/> from disk that this file points too. 
        /// </summary>
        /// <typeparam name="T">The type of object you want to load</typeparam>
        T LoadAsset<T>() where T : Object;
    }
}
