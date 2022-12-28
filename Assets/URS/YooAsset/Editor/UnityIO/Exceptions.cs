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

namespace UnityIO.Exceptions
{
    public class FileNotFoundException : Exception
    {
        public FileNotFoundException(string message) : base(message)
        {

        }
    }

    public class FileAlreadyExistsException : Exception
    {
        public FileAlreadyExistsException(string message) : base(message)
        {

        }
    }

    public class DirectoryNotFoundException : Exception
    {
        public DirectoryNotFoundException(string message) : base(message)
        {

        }
    }

    public class MoveException : Exception
    {
        public static string Format(string message, string from, string to)
        {
            return "Unable to move " + from + " to " + to + " because " + message;
        }

        public MoveException(string message, string from, string to) : base(Format(message, from, to))
        {
        }
    }

    public class RenameException : Exception
    {
        public static string Format(string message, string from, string to)
        {
            return "Unable to rename " + from + " to " + to + " because " + message;
        }

        public RenameException(string message, string from, string to) : base(Format(message, from, to))
        {
        }
    }

    /// <summary>
    /// This exception is thrown when you try to rename or move a directory and one
    /// already exists at that location. 
    /// </summary>
    public class DirectroyAlreadyExistsException : Exception
    {
        public DirectroyAlreadyExistsException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// This exception is thrown when you try to name a file or directory that contains
    /// invalid characters. 
    /// </summary>
    public class InvalidNameException : Exception
    {
        public InvalidNameException(string message) : base(message)
        {
        }
    }
}
