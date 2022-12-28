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

// Disable unused warning
#pragma warning disable 0168
using UnityEngine;
using UnityIO;
using UnityIO.Interfaces;

public class Documention
{
#pragma warning disable 0219
    /// <summary>
    /// Creates a directory the root of our project
    /// </summary>
    public void CreatingRootDirectory()
    {
        IO.Root.CreateDirectory("Favorite Animals");
    }

    /// <summary>
    /// Creates a nested directory in our project
    /// </summary>
    public void CreateNestedDirectory()
    {
        IO.Root.CreateDirectory("Favorite Animals/Cats");
    }

    /// <summary>
    /// A few more ways to create folders
    /// </summary>
    public void CreateNestedCatFolder()
    {
        // Create in a chain
        var catsFolder1 = IO.Root.CreateDirectory("Favorite Animals").CreateDirectory("Cats");

        // Break it down into two parts.
        var animals = IO.Root.CreateDirectory("Favorite Animals");
        var catsFolder2 = animals.CreateDirectory("Cats");

        // Do it in one step.
        var catsFolder3 = IO.Root.CreateDirectory("Favorite Animals/Cats");

        // Do it in one step with the helper
        var catsFolder4 = IO.Root.CreateDirectory("Favorite Animals" + IO.PATH_SPLITTER + "Cats");
    }

    /// <summary>
    /// Lets blow some things up
    /// </summary>
    public void Destroy()
    {
        // Get our directory and nuke it
        IO.Root["Favorite Animals"]["Cats"].Delete();

        // Delete our cats folder. 
        IO.Root.DeleteSubDirectory("Favorite Animals/Cats");
    }

    /// <summary>
    /// Lets see some explosions
    /// </summary>
    public void DeleteSomethingNotReal()
    {
        IO.Root["Favorite Animals"]["Dogs"].Delete(); // Does not exist
    }

    /// <summary>
    /// We should play it safe.
    /// </summary>
    public void ValidateBeforeDelete()
    {
        var favoriteAnimals = IO.Root["Favorite Animals"];

        if (favoriteAnimals.SubDirectoryExists("Dogs"))
        {
            favoriteAnimals.DeleteSubDirectory("Dogs");
        }
    }

    /// <summary>
    /// We should play it safe and make it easy
    /// </summary>
    public void EasyValidateBeforeDelete()
    {
        IO.Root["Favorite Animals"].IfSubDirectoryExists("Dogs").Delete();
    }

    /// <summary>
    /// Look at the length of that things!
    /// </summary>
    public void ILikeChains()
    {
        IO.Root["Favorite Animals"].IfSubDirectoryExists("Dogs").IfSubDirectoryExists("With Four Legs").IfSubDirectoryExists("Who stink").Delete();
    }

    /// <summary>
    /// Find, Create, and Destroy
    /// </summary>
    public void CreateAndDestroy()
    {
        IO.Root["Favorite Animals"].IfSubDirectoryExists("Dogs").CreateDirectory("Delete Me").Delete();
    }

    /// <summary>
    /// A simple example of getting all files from a folder called resources at Assets/Resources.
    /// </summary>
    public void GetResourceFiles()
    {
        // Get our directory
        IDirectory resourcesDirectory = IO.Root["Resources"];
        // Get all files.
        IFiles files = resourcesDirectory.GetFiles();

        // iterate over our files and print their names
        for (int i = 0; i < files.Count; i++)
        {
            Debug.Log(files[i].name);
        }
    }

    /// <summary>
    /// A simple example of getting all files from a folder called resources at Assets/Resources.
    /// </summary>
    public void GetFilesRecursively()
    {
        // Get our directory
        IDirectory resourcesDirectory = IO.Root["Resources"];
        // Get all files recursively
        IFiles files = resourcesDirectory.GetFiles(recursive: true);
    }

    public void SearchExamples()
    {
        // Returns every asset with 'Player' in it's name. 
        var playerFiles = IO.Root.GetFiles("*Player*");
        // Get everything named with 'Player' and '.anim' extension
        var playerAnimations = IO.Root.GetFiles("*Player*.anim");
        // get everything named 'Player' with one extra char maybe an 's'
        var playerChar = IO.Root.GetFiles("Player?");
    }

    /// <summary>
    /// Grabs all the files in the root directory and deletes them. 
    /// </summary>
    public void DeleteAFile()
    {
        // Get all our files. 
        var files = IO.Root.GetFiles();
        // Loop over them all and delete them
        for (int i = 0; i < files.Count; i++)
        {
            // Delete the file.
            files[i].Delete();
        }

        var file = IO.Root.GetFiles("DELETEME.txt").FirstOrDefault();

        /// Only delete our file if it exists. 
        IO.Root.IfFileExists("DeleteMe.txt").Delete();

        // Get the file we want to use.
        IFile fileToRename = IO.Root.GetFiles("DeleteMe.txt").FirstOrDefault();
        // Rename it
        fileToRename.Rename("DontDeleteMe.txt");

        IO.Root.GetFiles("DeleteMe.txt").FirstOrDefault().Rename("Delete All Of The Things.txt");

        // Get the file we want to use.
        IFile fileToMove = IO.Root.GetFiles("DeleteMe.txt").FirstOrDefault();
        // Move it
        fileToRename.Move(IO.Root["Resources"].path);

    }
}
