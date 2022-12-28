using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Daihenka.AssetPipeline
{
    internal static class AssetProcessorUtility
    {
        public static Texture GetIcon(this Type processorType)
        {
            var attr = processorType.GetCustomAttribute<AssetProcessorDescriptionAttribute>();
            return attr != null ? attr.Icon : EditorGUIUtility.FindTexture("_Popup@2x");
        }

        public static string GetProcessorName(this Type processorType, bool useFullName = false)
        {
            var processorNameAttribute = processorType.GetCustomAttribute<AssetProcessorDescriptionAttribute>();
            if (processorNameAttribute != null)
            {
                return string.IsNullOrEmpty(processorNameAttribute.Name) ? ObjectNames.NicifyVariableName(processorType.Name) : processorNameAttribute.Name;
            }

            var name = ObjectNames.NicifyVariableName(processorType.Name);
            return useFullName ? processorType.FullName.Replace(processorType.Name, name).Replace(".", "/") : name;
        }

        internal static bool HasOverriddenMethod(this Type type, string methodName, BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public)
        {
            return type.GetMethods(bindingFlags).Any(x => x.Name == methodName && x.DeclaringType == type);
        }

        internal static bool HasOverriddenMethods(this Type type, IList<string> methodNames, BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public)
        {
            return type.GetMethods(bindingFlags).Any(x => methodNames.Contains(x.Name) && x.DeclaringType == type);
        }
    }
}