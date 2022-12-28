using System;
using System.Collections.Generic;
using System.Linq;
using Daihenka.AssetPipeline.ReflectionMagic;
using UnityEditor;
using UnityEngine;
using UnityResources = UnityEngine.Resources;

namespace Daihenka.AssetPipeline
{
    internal static class EditorWindowUtility
    {
        static readonly Type ContainerWindowType;
        static readonly Type DockAreaType;

        static EditorWindowUtility()
        {
            ContainerWindowType = UnityEditorDynamic.UnityEditorAssembly.GetType("UnityEditor.ContainerWindow", true);
            DockAreaType = UnityEditorDynamic.UnityEditorAssembly.GetType("UnityEditor.DockArea", true);
        }

        public static bool KeyPressed<T>(this T s, string controlName, KeyCode key, out T fieldValue)
        {
            fieldValue = s;
            return GUI.GetNameOfFocusedControl() == controlName && (Event.current.type == EventType.KeyUp && Event.current.keyCode == key);
        }

        public static EditorWindow[] GetAllEditorWindows()
        {
            var editorWindows = new List<EditorWindow>();
            var containerWindows = ContainerWindowType.AsDynamicType().windows;
            foreach (var containerWindow in containerWindows)
            {
                var dynamicContainerWindow = ((object) containerWindow).AsDynamic();
                foreach (var view in dynamicContainerWindow.rootView.allChildren)
                {
                    var dynamicView = ((object) view).AsDynamic();
                    if (!DockAreaType.IsInstanceOfTypeNullable((object) view))
                    {
                        continue;
                    }

                    foreach (var pane in dynamicView.m_Panes)
                    {
                        var win = (EditorWindow) pane;
                        if (win)
                        {
                            editorWindows.Add(win);
                        }
                    }
                }
            }

            return editorWindows.ToArray();
        }

        public static T[] GetWindows<T>() where T : EditorWindow
        {
            return GetAllEditorWindows().Where(x => x is T).Cast<T>().ToArray();
        }


        public static bool TryDockNextTo(this EditorWindow window, params Type[] windowTypes)
        {
            var containerWindows = ContainerWindowType.AsDynamicType().windows;
            foreach (var type in windowTypes)
            {
                foreach (var containerWindow in containerWindows)
                {
                    var dynamicContainerWindow = ((object) containerWindow).AsDynamic();
                    foreach (var view in dynamicContainerWindow.rootView.allChildren)
                    {
                        var dynamicView = ((object) view).AsDynamic();
                        if (!DockAreaType.IsInstanceOfTypeNullable((object) view))
                        {
                            continue;
                        }

                        foreach (var pane in dynamicView.m_Panes)
                        {
                            if (pane.GetType() == type)
                            {
                                dynamicView.AddTab(window, true);
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        public static bool IsNullable(this Type type)
        {
            // http://stackoverflow.com/a/1770232
            return type.IsReferenceType() || Nullable.GetUnderlyingType(type) != null;
        }

        public static bool IsReferenceType(this Type type)
        {
            return !type.IsValueType;
        }

        public static bool IsInstanceOfTypeNullable(this Type type, object value)
        {
            return value == null ? type.IsNullable() : type.IsInstanceOfType(value);
        }
    }
}