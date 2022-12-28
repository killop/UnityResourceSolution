using System;
using System.Reflection;
using Daihenka.AssetPipeline.Import;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Daihenka.AssetPipeline
{
    internal static class SerializedPropertyUtility
    {
        public static void PrepareEmbeddedObjects(this ScriptableObject instance, Object target, ImportAssetType assetType, Type type)
        {
            var methodInfo = type.GetMethod("PrepareEmbeddedObjects", BindingFlags.Instance | BindingFlags.NonPublic);
            if (methodInfo?.DeclaringType == type)
            {
                var objects = (Object[]) methodInfo.Invoke(instance, new object[] {assetType});
                if (objects != null && objects.Length > 0)
                {
                    foreach (var obj in objects)
                    {
                        obj.AddObjectToUnityAsset(target);
                    }

                    EditorUtility.SetDirty(target);
                }
            }
        }

        internal static T GetSerializedValue<T>(this SerializedProperty prop)
        {
            if (prop == null)
            {
                return default;
            }

            var path = prop.propertyPath.Replace(".Array.data[", "[");
            object obj = prop.serializedObject.targetObject;
            var elements = path.Split('.');
            foreach (var element in elements)
            {
                if (element.Contains("["))
                {
                    var elementName = element.Substring(0, element.IndexOf("["));
                    var index = System.Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
                    obj = GetValue_Imp(obj, elementName, index);
                }
                else
                {
                    obj = GetValue_Imp(obj, element);
                }
            }

            return (T) obj;
        }

        static object GetValue_Imp(object source, string name)
        {
            if (source == null)
            {
                return null;
            }

            var type = source.GetType();
            while (type != null)
            {
                var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (f != null)
                {
                    return f.GetValue(source);
                }

                var p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p != null)
                {
                    return p.GetValue(source, null);
                }

                type = type.BaseType;
            }

            return null;
        }

        static object GetValue_Imp(object source, string name, int index)
        {
            var enumerable = GetValue_Imp(source, name) as System.Collections.IEnumerable;
            if (enumerable == null)
            {
                return null;
            }

            var enm = enumerable.GetEnumerator();

            for (var i = 0; i <= index; i++)
            {
                if (!enm.MoveNext())
                {
                    return null;
                }
            }

            return enm.Current;
        }

        internal static string GetPropertyValueAsString(this SerializedProperty prop)
        {
            var value = prop.GetPropertyValue();
            if (value == null)
            {
                return string.Empty;
            }

            if (value is bool)
            {
                return (bool) value ? "1" : "0";
            }

            return value.ToString();
        }

        internal static object GetPropertyValue(this SerializedProperty prop)
        {
            if (prop == null) throw new ArgumentNullException("prop");

            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer:
                    return prop.intValue;
                case SerializedPropertyType.Boolean:
                    return prop.boolValue;
                case SerializedPropertyType.Float:
                    return prop.floatValue;
                case SerializedPropertyType.String:
                    return prop.stringValue;
                case SerializedPropertyType.Color:
                    return prop.colorValue;
                case SerializedPropertyType.ObjectReference:
                    return prop.objectReferenceValue;
                case SerializedPropertyType.LayerMask:
                    return (LayerMask) prop.intValue;
                case SerializedPropertyType.Enum:
                    return prop.enumValueIndex;
                case SerializedPropertyType.Vector2:
                    return prop.vector2Value;
                case SerializedPropertyType.Vector3:
                    return prop.vector3Value;
                case SerializedPropertyType.Vector4:
                    return prop.vector4Value;
                case SerializedPropertyType.Rect:
                    return prop.rectValue;
                case SerializedPropertyType.ArraySize:
                    return prop.arraySize;
                case SerializedPropertyType.Character:
                    return (char) prop.intValue;
                case SerializedPropertyType.AnimationCurve:
                    return prop.animationCurveValue;
                case SerializedPropertyType.Bounds:
                    return prop.boundsValue;
                case SerializedPropertyType.Gradient:
                    throw new InvalidOperationException("Can not handle Gradient types.");
            }

            return null;
        }
    }
}