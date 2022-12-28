using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Linq.Expressions;

namespace Bewildered.SmartLibrary
{
    internal static class TypeAccessor
    {
        public const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        public static MethodInfo GetMethod(Type type, string name, params Type[] parameterTypes)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type), "Provided type is null.");
            
            if (parameterTypes == null || parameterTypes.Length == 0)
                return type.GetMethod(name, Flags);
            else
                return type.GetMethod(name, Flags, Type.DefaultBinder, parameterTypes, null);
        }

        public static MethodInfo GetMethod<T>(string name, params Type[] parameterTypes)
        {
            return GetMethod(typeof(T), name, parameterTypes);
        }

        public static PropertyInfo GetProperty(Type type, string name)
        {
            return type.GetProperty(name, Flags);
        }

        public static FieldInfo GetField(Type type, string name)
        {
            return type.GetField(name, Flags);
        }

        public static IList CreateListOfType(Type itemType)
        {
            Type listType = typeof(List<>).MakeGenericType(itemType);
            return (IList)Activator.CreateInstance(listType);
        }

        public static T CreateDelegate<T>(this MethodInfo method, object target) where T : Delegate
        {
            return (T)method.CreateDelegate(target);
        }

        public static T CreateDelegate<T>(this MethodInfo method) where T : Delegate
        {
            return (T)method.CreateDelegate(typeof(T));
        }

        public static Delegate CreateDelegate(this MethodInfo methodInfo, object target)
        {
            Func<Type[], Type> getType;
            var isAction = methodInfo.ReturnType.Equals(typeof(void));
            var types = methodInfo.GetParameters().Select(p => p.ParameterType);

            if (isAction)
            {
                getType = Expression.GetActionType;
            }
            else
            {
                getType = Expression.GetFuncType;
                types = types.Concat(new[] { methodInfo.ReturnType });
            }

            if (methodInfo.IsStatic)
            {
                return Delegate.CreateDelegate(getType(types.ToArray()), methodInfo);
            }

            return Delegate.CreateDelegate(getType(types.ToArray()), target, methodInfo.Name);
        }
    } 
}
