using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditorInternal;

namespace NinjaBeats
{
    public partial class EditorUtils
    {
        public static bool RepaintScene = false;

        public class RepaintSceneScope : IDisposable
        {
            public RepaintSceneScope()
            {
                RepaintScene = false;
            }

            public void Dispose()
            {
                if (RepaintScene)
                    SceneView.RepaintAll();
            }
        }

        public struct HandlesColorScope : IDisposable
        {
            Color color;

            public HandlesColorScope(Color c)
            {
                color = Handles.color;
                Handles.color = c;
            }

            public void Dispose()
            {
                Handles.color = color;
            }
        }

        public struct GizmosColorScope : IDisposable
        {
            Color color;

            public GizmosColorScope(Color c)
            {
                color = Gizmos.color;
                Gizmos.color = c;
            }

            public void Dispose()
            {
                Gizmos.color = color;
            }
        }

        public struct ColorScope : IDisposable
        {
            Color color;

            public ColorScope(Color c, bool enable = true)
            {
                if (!enable)
                    c = GUI.color;
                
                color = GUI.color;
                GUI.color = c;
            }

            public void Dispose()
            {
                GUI.color = color;
            }
        }
        
        public struct ContentColorScope : IDisposable
        {
            Color contentColor;

            public ContentColorScope(Color c, bool enable = true)
            {
                if (!enable)
                    c = GUI.contentColor;
                
                contentColor = GUI.contentColor;
                GUI.contentColor = c;
            }

            public void Dispose()
            {
                GUI.contentColor = contentColor;
            }
        }

        public struct BackgroundColorScope : IDisposable
        {
            Color color;

            public BackgroundColorScope(Color c)
            {
                color = GUI.backgroundColor;
                GUI.backgroundColor = c;
            }

            public void Dispose()
            {
                GUI.backgroundColor = color;
            }
        }

        public struct _ErrorGroupScope : IDisposable
        {
            ColorScope? sub;

            public _ErrorGroupScope(bool error)
            {
                if (error)
                {
                    sub = new ColorScope(Color.red);
                    ErrorCheckSetError();
                }
                else
                {
                    sub = null;   
                }
            }

            public void Dispose()
            {
                sub?.Dispose();
                sub = null;
            }
        }

        public static _ErrorGroupScope ErrorGroupScope(bool error = true) => new _ErrorGroupScope(error);
        
        
        private static List<bool> s_ErrorGroupStack = new List<bool>();

        private static void ErrorCheckSetError()
        {
            for (int i = 0; i < s_ErrorGroupStack.Count; ++i)
                s_ErrorGroupStack[i] = true;
        }

        public static void BeginErrorCheck()
        {
            s_ErrorGroupStack.Add(false);
        }

        public static bool EndErrorCheck()
        {
            if (s_ErrorGroupStack.Count == 0)
            {
                Debug.LogError($"EndErrorCheck Error, s_ErrorGroupStack.Count == 0");
                return true;
            }

            var error = s_ErrorGroupStack[s_ErrorGroupStack.Count - 1];
            s_ErrorGroupStack.RemoveAt(s_ErrorGroupStack.Count - 1);
            return error;
        }
        
        public struct _ErrorCheckScope : IDisposable
        {
            private int stackIdx;

            public bool error
            {
                get
                {
                    if (s_ErrorGroupStack.IdxValid(stackIdx))
                        return s_ErrorGroupStack[stackIdx];
                    
                    Debug.LogError($"_ErrorCheckScope Error, s_ErrorGroupStack.Count == 0");
                    return true;
                }
            }
            
            public _ErrorCheckScope(int stackIdx)
            {
                this.stackIdx = stackIdx;
            }
            public void Dispose()
            {
                EndErrorCheck();
            }
        }

        public static _ErrorCheckScope ErrorCheckScope()
        {
            BeginErrorCheck();
            return new(s_ErrorGroupStack.Count - 1);
        }

        public struct _LabelWidthScope : IDisposable
        {
            float width;

            public _LabelWidthScope(float width)
            {
                this.width = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = width;
            }

            public void Dispose()
            {
                EditorGUIUtility.labelWidth = this.width;
            }
        }

        public static _LabelWidthScope LabelWidthScope(float width) => new _LabelWidthScope(width);
        

        public struct GUIContentScope : IDisposable
        {
            public GUIContent content;

            public GUIContentScope(string text)
            {
                content = SharedObjectPool<GUIContent>.Get();
                content.text = text;
            }

            public void Dispose()
            {
                if (content != null)
                {
                    content.text = "";
                    SharedObjectPool<GUIContent>.Release(content);
                }
            }
        }

        public struct _ShowMixedValueScopeImpl : IDisposable
        {
            bool showMixedValue;

            public _ShowMixedValueScopeImpl(bool value)
            {
                showMixedValue = EditorGUI.showMixedValue;
                EditorGUI.showMixedValue = value;
            }

            public void Dispose()
            {
                EditorGUI.showMixedValue = showMixedValue;
            }
        }

        public static _ShowMixedValueScopeImpl ShowMixedValueScope(bool showMixedValue) => new _ShowMixedValueScopeImpl(showMixedValue);

        public struct _HorizontalScopeImpl : IDisposable
        {
            public Rect rect;
            public void Dispose() => EditorGUILayout.EndHorizontal();
        }

        public static _HorizontalScopeImpl HorizontalScope(params GUILayoutOption[] options)
        {
            var r = new _HorizontalScopeImpl();
            r.rect = EditorGUILayout.BeginHorizontal(options);
            return r;
        }

        public static _HorizontalScopeImpl HorizontalScope(GUIStyle style, params GUILayoutOption[] options)
        {
            var r = new _HorizontalScopeImpl();
            r.rect = EditorGUILayout.BeginHorizontal(style, options);
            return r;
        }

        public struct _VerticalScopeImpl : IDisposable
        {
            public Rect rect;
            public void Dispose() => EditorGUILayout.EndVertical();
        }

        public static _VerticalScopeImpl VerticalScope(params GUILayoutOption[] options)
        {
            var r = new _VerticalScopeImpl();
            r.rect = EditorGUILayout.BeginVertical(options);
            return r;
        }

        public static _VerticalScopeImpl VerticalScope(GUIStyle style, params GUILayoutOption[] options)
        {
            var r = new _VerticalScopeImpl();
            r.rect = EditorGUILayout.BeginVertical(style, options);
            return r;
        }
        
        public struct _ShortLabelScope : IDisposable
        {
            private bool m_Enable;
            public _ShortLabelScope(bool enable) => m_Enable = enable;

            public void Dispose()
            {
                if (m_Enable)
                    EndShortLabel();
            }
        }

        private static int s_ShortLabelCount = 0;
        public static bool IsShortLabel => s_ShortLabelCount > 0;

        public static _ShortLabelScope ShortLabelScope(bool enable = true)
        {
            if (enable)
                BeginShortLabel();
            var r = new _ShortLabelScope(enable);
            return r;
        }

        public static void BeginShortLabel() => ++s_ShortLabelCount;
        public static void EndShortLabel() => --s_ShortLabelCount;

        public static void FlexibleLabel(string label, bool vertical)
        {
            using (var scope = new EditorUtils.GUIContentScope(label))
            {
                if (vertical)
                {
                    using (EditorUtils.VerticalScope(
                               EditorUtils.GUILayoutOption_Width(GUI.skin.label.CalcSize(scope.content).x)))
                    {
                        GUILayout.FlexibleSpace();
                        GUILayout.Label(scope.content);
                        GUILayout.FlexibleSpace();
                    }
                }
                else
                {
                    using (EditorUtils.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        GUILayout.Label(scope.content);
                        GUILayout.FlexibleSpace();
                    }
                }
            }
        }

        public delegate T GUIWithOptionDelegate<T>(T value, params GUILayoutOption[] options);
        public delegate T GUIWithOptionDelegate<T, T0>(T0 param0 ,T value, params GUILayoutOption[] options);
        public delegate T GUIDelegate<T>(T value);
        public delegate T GUIDelegate<T, T0>(T0 param0, T value);

        public static T ShortLabelGUI<T>(GUIWithOptionDelegate<T> func, string label, T value, params GUILayoutOption[] options)
        {
            using (EditorUtils.HorizontalScope())
            {
                EditorUtils.FlexibleLabel(label, true);
                return func(value, options);
            }
        }
        public static T ShortLabelGUI<T, T0>(GUIWithOptionDelegate<T, T0> func, string label, T0 param0, T value, params GUILayoutOption[] options)
        {
            using (EditorUtils.HorizontalScope())
            {
                EditorUtils.FlexibleLabel(label, true);
                return func(param0, value, options);
            }
        }
        public static T ShortLabelGUI<T>(GUIDelegate<T> func, string label, T value)
        {
            using (EditorUtils.HorizontalScope())
            {
                EditorUtils.FlexibleLabel(label, true);
                return func(value);
            }
        }
        public static T ShortLabelGUI<T, T0>(GUIDelegate<T, T0> func, string label, T0 param0, T value)
        {
            using (EditorUtils.HorizontalScope())
            {
                EditorUtils.FlexibleLabel(label, true);
                return func(param0, value);
            }
        }

        public static Type EditingUnityObjectExType = null;

        public struct _EditingUnityObjectExTypeScopeImpl : IDisposable
        {
            public _EditingUnityObjectExTypeScopeImpl(Type type) => EditingUnityObjectExType = type;
            public void Dispose() => EditingUnityObjectExType = null;
        }

        public static _EditingUnityObjectExTypeScopeImpl EditingUnityObjectExTypeScope(Type type) =>
            new _EditingUnityObjectExTypeScopeImpl(type);

    }
}