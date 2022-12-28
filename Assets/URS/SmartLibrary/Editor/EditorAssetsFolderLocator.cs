using System.IO;
using UnityEngine;
using UnityEditor;

namespace Bewildered.SmartLibrary
{
    internal class EditorAssetsFolderLocator : ScriptableObject
    {
        /// <summary>
        /// Reterns the project reletive path to the "SmartLibrary/Editor" folder in the assets folder.
        /// </summary>
        public static string GetFolderPath()
        {
            var locator = CreateInstance<EditorAssetsFolderLocator>();
            var script = MonoScript.FromScriptableObject(locator);

            string path = Path.GetDirectoryName(AssetDatabase.GetAssetPath(script));
            if (Path.DirectorySeparatorChar != '/')
                path = path.Replace(Path.DirectorySeparatorChar, '/');

            DestroyImmediate(locator);
            return path;
        }
    }
}
