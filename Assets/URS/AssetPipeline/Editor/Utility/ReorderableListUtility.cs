using UnityEditor;

namespace Daihenka.AssetPipeline
{
    internal static class ReorderableListUtility
    {
        public static bool HasSelection(this UnityEditorInternal.ReorderableList list)
        {
            return list.index > -1 && ((list.serializedProperty != null && list.serializedProperty.arraySize > list.index) || (list.list != null && list.list.Count > list.index));
        }

        public static SerializedProperty GetArrayElement(this UnityEditorInternal.ReorderableList list, int index)
        {
            if (list.serializedProperty != null && list.serializedProperty.arraySize > index)
            {
                return list.serializedProperty.GetArrayElementAtIndex(index);
            }

            return null;
        }
    }
}