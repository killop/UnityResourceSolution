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
using NUnit.Framework;
using UnityIO;

public class CreatingDirectory
{

    [Test]
    public void CreateRootLevelDirectory()
    {
        IO.Root.CreateDirectory("CreateRootLevelDirectory");
        Assert.True(IO.Root.SubDirectoryExists("CreateRootLevelDirectory"));
        IO.Root.DeleteSubDirectory("CreateRootLevelDirectory");
    }

    [Test]
    public void CreateNestedDirectoryOneStep()
    {
        IO.Root.CreateDirectory("CreateNestedDirectoryOneStep/Folder One");
        Assert.True(IO.Root.SubDirectoryExists("CreateNestedDirectoryOneStep/Folder One"));
        IO.Root.DeleteSubDirectory("CreateNestedDirectoryOneStep");
    }

    [Test]
    public void CreateNestedDirectoryMultiStep()
    {
        IO.Root.CreateDirectory("CreateNestedDirectoryMultiStep").CreateDirectory("Folder One").CreateDirectory("Folder Two");
        Assert.True(IO.Root.SubDirectoryExists("CreateNestedDirectoryMultiStep/Folder One"));
        Assert.True(IO.Root.SubDirectoryExists("CreateNestedDirectoryMultiStep/Folder One/Folder Two"));
        IO.Root.DeleteSubDirectory("CreateNestedDirectoryMultiStep");
    }

    [Test]
    public void CreateNestedPreExistingRoot()
    {
        // Create the root by itself. 
        IO.Root.CreateDirectory("CreateNestedDirectoryMultiStep");
        // Create it again with a child 
        IO.Root.CreateDirectory("CreateNestedDirectoryMultiStep").CreateDirectory("MultiFolder_Temp");
        // Check if the child exists at the root
        bool directroyExistsInRoot = IO.Root.SubDirectoryExists("MultiFolder_Temp");
        // Clean up the root folder
        IO.Root["CreateNestedDirectoryMultiStep"].Delete();
        // If the test failed this folder will exist so we want to cleanup 
        IO.Root.IfSubDirectoryExists("CreateNestedDirectoryMultiStep").Delete();
        // Fail or pass the test.
        Assert.False(directroyExistsInRoot);
    }

}
