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
using UnityEditor;
using UnityEngine;
using UnityIO;
using UnityIO.Classes;
using UnityIO.Interfaces;
using AssetDatabase = UnityEditor.AssetDatabase;

public class FileChangesTests
{
    private IDirectory m_WorkingDirectroy; 

    [SetUp]
    public void Init()
    {
        // Creating our working Directory
        m_WorkingDirectroy = IO.Root.CreateDirectory(GetType().Name);
        // Create a prefab to work with.
        PrefabUtility.CreatePrefab(m_WorkingDirectroy.path + "/Cube.prefab", GameObject.CreatePrimitive(PrimitiveType.Cube));
        PrefabUtility.CreatePrefab(m_WorkingDirectroy.path + "/Cylinder.prefab", GameObject.CreatePrimitive(PrimitiveType.Cylinder));
        PrefabUtility.CreatePrefab(m_WorkingDirectroy.path + "/Plane.prefab", GameObject.CreatePrimitive(PrimitiveType.Plane));
    }

    [Test]
    [Description("Tests to see if we can duplicate a file")]
    public void DuplicateFile()
    {
        // Get our file
        IFile cube = m_WorkingDirectroy.GetFiles("*Cube*").FirstOrDefault();
        // Should not be null;
        Assert.IsNotInstanceOf<NullFile>(cube);
        // Duplicate our prefab
        var secondCube = cube.Duplicate();
        // Check if our first one still exists
        Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(cube.path));
        // And our second one is alive.
        Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(secondCube.path));
    }

    [Test]
    [Description("Tests to see if we can duplicate a file and give it a new name")]
    public void DuplicateFileWithNewName()
    {
        // Get our file
        IFile cube = m_WorkingDirectroy.GetFiles("*Cube*").FirstOrDefault();
        // Should not be null;
        Assert.IsNotInstanceOf<NullFile>(cube);
        // Duplicate our prefab
        var secondCube = cube.Duplicate("Super Box");
        // Check if our first one still exists
        Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(m_WorkingDirectroy.path + "/Cube.prefab"));
        // And our second one is alive.
        Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(m_WorkingDirectroy.path + "/Super Box.prefab"));
    }


    [Test]
    [Description("Tests to see if a file can be renamed.")]
    public void RenameFile()
    {
        // Get our file
        IFile cube = m_WorkingDirectroy.GetFiles("*Cylinder*").FirstOrDefault();
        // Should not be null;
        Assert.IsNotInstanceOf<NullFile>(cube);
        // Duplicate our prefab
        cube.Rename("Super Tube");
        // Check to make sure the original item does not exist
        Assert.IsNull(AssetDatabase.LoadAssetAtPath<GameObject>(m_WorkingDirectroy.path + "/Cylinder.prefab"), "Our old prefab still exists");
        // Check if the rename happened.
        Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(m_WorkingDirectroy.path + "/Super Tube.prefab"), "The renamed prefab could not be found");
    }


    [Test]
    [Description("Tests to see if a file can be moved.")]
    public void MoveFile()
    {
        // Create directory to move stuff into.
        var moveTo = m_WorkingDirectroy.CreateDirectory("MoveTo");
        // Get our file
        IFile cube = m_WorkingDirectroy.GetFiles("*Plane*").FirstOrDefault();
        // Should not be null;
        Assert.IsNotInstanceOf<NullFile>(cube);
        // Duplicate our prefab
        cube.Move(moveTo.path);
        // Check to make sure the original item does not exist
        Assert.IsNull(AssetDatabase.LoadAssetAtPath<GameObject>(m_WorkingDirectroy.path + "/Plane.prefab"), "Our old prefab still exists");
        // Check if the rename happened.
        Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(m_WorkingDirectroy.path + "/MoveTo/Plane.prefab"), "The renamed prefab could not be found");
    }

    [Test]
    [Description("Tests to make sure we can delete files.")]
    public void DeleteFile()
    {
        // Create a prefab to delete. 
        PrefabUtility.CreatePrefab(m_WorkingDirectroy.path + "/Delete Me.prefab", GameObject.CreatePrimitive(PrimitiveType.Plane));
        // Get our file
        var deleteMeFile = m_WorkingDirectroy.GetFiles("*Delete Me*").FirstOrDefault();
        // Delete it
        deleteMeFile.Delete();
        // Check to make sure the original item does not exist
        Assert.IsNull(AssetDatabase.LoadAssetAtPath<GameObject>(m_WorkingDirectroy.path + "/Delete Me.prefab"), "Our old prefab still exists");
    }



    [TearDown]
    public void Dispose()
    {
        IO.Root.IfSubDirectoryExists(GetType().Name).Delete();
    }
}
