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

using NUnit.Framework;
using UnityIO;
using UnityIO.Interfaces;

public class GetFilesTests : UnityIOTestBase
{
    [Test(Description = "Check if you can find assets only at the top level in " + UNIT_TEST_LOADING_TEST_ASSETS)]
    public void GetTopLevelFiles()
    {
        // Setup our test. 
        var loadingDir = SetupAssetLoadingTest();
        // Load all our assets
        var files = loadingDir.GetFiles();
        // Should only be root level which has a total of 3 files.
        Assert.AreEqual(files.Count, 3, "There should be 3 files in the root of the testing directory");
    }

    [Test(Description = "Check if you can find assets only at the top level with filter in " + UNIT_TEST_LOADING_TEST_ASSETS)]
    public void GetRecursiveFiles()
    {
        // Setup our test. 
        var loadingDir = SetupAssetLoadingTest();
        // Get all
        var files = loadingDir.GetFiles(recursive: true);
        // Should be 10 assets
        Assert.AreEqual(files.Count, 10, "There should be 10 files in the testing directory");
    }

    [Test(Description = "Checks if you can verify if you can find only assets in the top level directory with a filter. In this case any file with the .anim extension in " + UNIT_TEST_LOADING_TEST_ASSETS)]
    public void GetTopLevelWithFilters()
    {
        // Setup our test. 
        var loadingDir = SetupAssetLoadingTest();
        // We are going to try to only find files ending with .anim
        var files = loadingDir.GetFiles(filter:"*.anim");
        // There should be four. 
        Assert.AreEqual(files.Count, 1, "There should be 1 file at the root that ends with .anim in our testing directory");
    }

    [Test(Description = "Checks if you can verify if you can find all assets with a filter. In this case any file with the .anim extension in " + UNIT_TEST_LOADING_TEST_ASSETS)]
    public void GetRecursiveWithFilters()
    {
        // Setup our test. 
        var loadingDir = SetupAssetLoadingTest();
        // We are going to try to only find files ending with .anim
        var files = loadingDir.GetFiles(filter: "*.anim", recursive:true);
        // There should be four. 
        Assert.AreEqual(files.Count, 4, "There should be 1 file at the root that ends with .anim in our testing directory");
    }

    [Test(Description = "Test to make sure the meta information about the file is correct")]
    public void FileNameProperties()
    {
        // Setup our test. 
        var loadingDir = SetupAssetLoadingTest();
        // We are going to try to only find files ending with .anim
        var file = loadingDir.GetFiles(filter: "*.anim").FirstOrDefault();
        // Make sure it's the correct directory.
        Assert.AreEqual(loadingDir.path, file.directory.path, "The directory of the file does not match.");
        // Make sure it's the correct extension.
        Assert.AreEqual(".anim", file.extension, "The extension of this class should be '.anim'.");
        // Make sure it's the correct extension.
        Assert.AreEqual("Misc Animation", file.nameWithoutExtension, "The name of this class is not correct.");
    }
}
