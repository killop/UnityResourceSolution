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

public class ConvertPathTests : UnityIOTestBase
{
    [Test]
    public void AssetPathTOSystemPathConvert()
    {
        string systemPath = Application.dataPath + "/UnityIO/Editor/Unit Tests/Loading Assets/Misc Prefab.prefab";
        string assetPath = "Assets/UnityIO/Editor/Unit Tests/Loading Assets/Misc Prefab.prefab";
        string result = IO.AssetPathToSystemPath(assetPath);
        Assert.AreEqual(systemPath, result, "The path did not convert correctly");
    }

    [Test]
    public void SystemToAssetPathConvert()
    {
        string systemPath = Application.dataPath + "/UnityIO/Editor/Unit Tests/Loading Assets/Misc Prefab.prefab";
        string assetPath = "Assets/UnityIO/Editor/Unit Tests/Loading Assets/Misc Prefab.prefab";
        string result = IO.SystemToAssetPath(systemPath);
        Assert.AreEqual(assetPath, result, "The path did not convert correctly");
    }
}


