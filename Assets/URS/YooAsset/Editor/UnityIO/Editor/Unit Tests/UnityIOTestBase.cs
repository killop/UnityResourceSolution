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
using System;
using UnityEngine;
using UnityIO;
using UnityIO.Interfaces;

public class UnityIOTestBase
{
    public const string UNIT_TEST_LOADING_TEST_ASSETS = "UnityIO/Editor/Unit Tests/Loading Assets";

    /// <summary>
    /// Returns back an IDirectory for testing asset loading. 
    /// </summary>
    /// <returns></returns>
    public static IDirectory SetupAssetLoadingTest()
    {
        // We can only test if our testing directory exists
        Assume.That(IO.Root.SubDirectoryExists(UNIT_TEST_LOADING_TEST_ASSETS), "The testing directory this test is looking for does not exists at path '" + UNIT_TEST_LOADING_TEST_ASSETS + "'.");
        // Get our loading area
        return IO.Root[UNIT_TEST_LOADING_TEST_ASSETS];
    }

    /// <summary>
    /// Logs to the Unity Console.
    /// </summary>
    public void UnityLog(string log)
    {
        Debug.Log(log);
    }

    /// <summary>
    /// Logs to the test's console.
    /// </summary>
    public void TestLog(string log)
    {
        Console.WriteLine(log);
    }
}
