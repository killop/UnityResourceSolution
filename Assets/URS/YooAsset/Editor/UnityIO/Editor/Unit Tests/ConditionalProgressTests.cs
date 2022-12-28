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
using UnityEngine;
using UnityEditor;
using UnityIO;
using Object = UnityEngine.Object;

public class ConditionalProgressTests : UnityIOTestBase
{
    [Test]
    public void ConditionFolderRoot()
    {
        // Only create Sub Directory if Conditional Progress exists.
        IO.Root.IfSubDirectoryExists("Conditional Progress").CreateDirectory("Sub Directory");
        // It should not exists
        Assert.False(IO.Root.SubDirectoryExists("Conditional Progress/Sub Directory"));
        // Then really create it
        IO.Root.CreateDirectory("Conditional Progress");
        // Then try conditional again
        IO.Root.IfSubDirectoryExists("Conditional Progress").CreateDirectory("Sub Directory");
        // It should not exists since we created the parent directory
        Assert.True(IO.Root.SubDirectoryExists("Conditional Progress/Sub Directory"));
        // Cleanup
        IO.Root.IfSubDirectoryExists("Conditional Progress").Delete();
    }

    [Test]
    public void EmptyCheck()
    {
        // Create a new directory
        IO.Root.CreateDirectory("If Empty");
        // Check if it's empty
        Assert.True(IO.Root["If Empty"].IsEmpty(assetsOnly: false), "This should empty");
        // Add a sub folder
        IO.Root["If Empty"].CreateDirectory("Sub Directory");
        // This should be false since we say we want to include sub folders.
        Assert.False(IO.Root["If Empty"].IsEmpty(assetsOnly: false), "This should have passed because we have one folder.");
        // This should be false because we only care about Assets. 
        Assert.False(IO.Root["If Empty"].IsEmpty(assetsOnly: true), "This should empty");
        // Cleanup
        IO.Root["If Empty"].Delete();
    }

    [Test]
    public void ConditionEmptyTest()
    {
        // Create a new directory
        IO.Root.CreateDirectory("Conditional Empty").CreateDirectory("Sub Directory");
        // Destroy it if it's empty (it's not).
        IO.Root["Conditional Empty"].IfEmpty(assetsOnly: false).Delete();
        // Check if it was deleted (it should not have been).
        Assert.True(IO.Root.SubDirectoryExists("Conditional Empty"), "The folder should not have been deleted");
        // Delete the next level instead this should work.
        IO.Root["Conditional Empty/Sub Directory"].IfEmpty(assetsOnly: false).Delete();
        // Check if it was deleted (it should have been).
        bool directroyStillExists = IO.Root.SubDirectoryExists("Conditional Empty/Sub Directory");
        // Clean up if the test failed
        IO.Root["Conditional Empty"].Delete();
        // Finish Test
        Assert.False(directroyStillExists);
    }

    [Test]
    [Description("Tests to see if we can cancel execution if a file does not exist. Tests both loading a valid fail and an invalid one.")]
    public void IfFileExists()
    {
        // Get our testingDir
        var testingDir = SetupAssetLoadingTest();
        // Load our asset only if it exists.
        GameObject loadedAsset = testingDir.IfFileExists("Misc Prefab").LoadAsset<GameObject>();
        // Our Asset should exist
        Assert.IsNotNull(loadedAsset, "We should have been able to load this asset");
        // Log output
        TestLog("Loaded " + loadedAsset.name + " successfully");
        // Load an invalid object.
        GameObject invalidObject = testingDir.IfFileExists("Fake Prefab").LoadAsset<GameObject>();
        // It should be null
        Assert.IsNull(invalidObject, "We should not have been able to load the asset named.");
    }
}
