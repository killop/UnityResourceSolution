using UnityEditor;
using UnityEngine;

namespace UnityIO
{
    /// <summary>
    /// This class contains some useful content menus for Unity. 
    /// </summary>
    public static class ContextMenus
    {
        [MenuItem("Assets/UnityIO/Copy System Path")]
        public static void CopySystemPath()
        {
            // Get our object. 
            Object selectedObject = Selection.activeObject;
            // Get the system path
            string systemPath = Application.dataPath;
            // Get asset path
            string asssetPath = UnityEditor.AssetDatabase.GetAssetPath(selectedObject);
            // Make sure we have a path
            if(string.IsNullOrEmpty(asssetPath))
            {
                return;
            }
            // Remove the 'Assets' part.
            asssetPath = asssetPath.Substring(6, asssetPath.Length - 6);
            // full path
            string fullPath = systemPath + asssetPath;
            // print
            EditorGUIUtility.systemCopyBuffer = fullPath;

        }

        [MenuItem("Assets/UnityIO/Copy Unity Path")]
        public static void CopyAssetPath()
        {
            // Get our object. 
            Object selectedObject = Selection.activeObject;
            // return it's path
            string unityPath = UnityEditor.AssetDatabase.GetAssetPath(selectedObject);
            // COpy to buffer
            EditorGUIUtility.systemCopyBuffer = unityPath;
        }

        [MenuItem("Assets/UnityIO/Copy Unity Path", true)]
        [MenuItem("Assets/UnityIO/Copy System Path", true)]
        public static bool CopyPathValidation()
        {
            Object selected = Selection.activeObject;

            if(selected == null)
            {
                return false;
            }

            // Things that exist in the scene don't have an asset path. 
            return !string.IsNullOrEmpty(UnityEditor.AssetDatabase.GetAssetPath(selected));
        }
    }
}
