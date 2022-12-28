using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

namespace Bewildered.SmartLibrary.UI
{
    [CustomEditor(typeof(LibraryData))]
    internal class LibraryDataEditor : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            return new VisualElement();
        }
    } 
}
