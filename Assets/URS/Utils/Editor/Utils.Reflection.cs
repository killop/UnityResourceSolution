using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace NinjaBeats
{

    public static partial class Utils
    {

        /// <summary>
        /// return Attribute.IsDefined(m, typeof(T));
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="m"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasAttribute<T>(this MemberInfo m) where T : Attribute
        {
            return Attribute.IsDefined(m, typeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetValue<T>(this FieldInfo self, object value = null)
        {
            if (self == null)
                return default;
            return (T)self.GetValue(value);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetValue<T>(this PropertyInfo self, object value = null)
        {
            if (self == null)
                return default;
            return (T)self.GetValue(value);
        }
        
        static ConcurrentDictionary<Type, bool> s_RealPublicMap = new ();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsRealPublicImpl(this Type self)
        {
            if (!s_RealPublicMap.TryGetValue(self, out var value))
            {
                value = true;
                var it = self;
                while (it.IsNested)
                {
                    if (!it.IsNestedPublic)
                    {
                        value = false;
                        break;
                    }
                    
                    it = it.DeclaringType;
                }

                if (value && !it.IsPublic && !it.IsGenericParameter)
                    value = false;

                if (value && self.IsGenericType)
                {
                    foreach (var gt in self.GetGenericArguments())
                    {
                        if (!gt.IsRealPublicImpl())
                        {
                            value = false;
                            break;
                        }
                    }
                }

                if (value && self.HasElementType)
                {
                    if (!self.GetElementType().IsRealPublicImpl())
                        value = false;
                }

                s_RealPublicMap.TryAdd(self, value);
            }
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsGetRealPublic(this PropertyInfo self)
        {
            if (self.CanRead && !self.GetMethod.IsPublic)
                return false;
            return self.DeclaringType.IsRealPublic();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSetRealPublic(this PropertyInfo self)
        {
            if (self.CanWrite && !self.SetMethod.IsPublic)
                return false;
            return self.DeclaringType.IsRealPublic();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRealPublic<T>(this T self) where T : MemberInfo => MemberInfoTraits<T>.IsRealPublic(self);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRealStatic<T>(this T self) where T : MemberInfo => MemberInfoTraits<T>.IsRealStatic(self);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRealCanRead<T>(this T self) where T : MemberInfo => MemberInfoTraits<T>.IsRealCanRead(self);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRealCanWrite<T>(this T self) where T : MemberInfo => MemberInfoTraits<T>.IsRealCanWrite(self);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Type GetRealMemberType<T>(this T self) where T : MemberInfo => MemberInfoTraits<T>.GetRealMemberType(self);

        private static bool s_MemberInfoTraitsInited = false;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void MemberInfoTraitsInit()
        {
            if (s_MemberInfoTraitsInited)
                return;

            MemberInfoTraitsInitImpl();
        }
        
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        private static void MemberInfoTraitsInitImpl()
        {
            s_MemberInfoTraitsInited = true;
            
            MemberInfoTraits<FieldInfo>.IsRealPublic = x => x.IsPublic && x.DeclaringType.IsRealPublic();
            MemberInfoTraits<PropertyInfo>.IsRealPublic = x =>
            {
                if (x.CanRead && !x.GetMethod.IsPublic)
                    return false;
                if (x.CanWrite && !x.SetMethod.IsPublic)
                    return false;
                return x.DeclaringType.IsRealPublic();
            };
            MemberInfoTraits<MethodInfo>.IsRealPublic = x => x.IsPublic && x.DeclaringType.IsRealPublic();
            MemberInfoTraits<ConstructorInfo>.IsRealPublic = x => x.IsPublic && x.DeclaringType.IsRealPublic();
            MemberInfoTraits<EventInfo>.IsRealPublic = x =>
            {
                if (x.AddMethod?.IsPublic == false)
                    return false;
                if (x.RemoveMethod?.IsPublic == false)
                    return false;
                if (x.RaiseMethod?.IsPublic == false)
                    return false;
                return x.DeclaringType.IsRealPublic();
            };
            MemberInfoTraits<Type>.IsRealPublic = IsRealPublicImpl;
            MemberInfoTraits<MemberInfo>.IsRealPublic = x => x switch
            {
                FieldInfo fi => fi.IsRealPublic(),
                PropertyInfo pi => pi.IsRealPublic(),
                MethodInfo mi => mi.IsRealPublic(),
                ConstructorInfo ci => ci.IsRealPublic(),
                EventInfo ei => ei.IsRealPublic(),
                Type t => t.IsRealPublic(),
                _ => false
            };

            MemberInfoTraits<FieldInfo>.IsRealStatic = x => x.IsStatic;
            MemberInfoTraits<PropertyInfo>.IsRealStatic = x =>
            {
                if (x.CanRead && !x.GetMethod.IsStatic)
                    return false;
                if (x.CanWrite && !x.SetMethod.IsStatic)
                    return false;
                return true;
            };
            MemberInfoTraits<MethodInfo>.IsRealStatic = x => x.IsStatic;
            MemberInfoTraits<ConstructorInfo>.IsRealStatic = x => x.IsStatic;
            MemberInfoTraits<EventInfo>.IsRealStatic = x =>
            {
                if (x.AddMethod?.IsStatic == false)
                    return false;
                if (x.RemoveMethod?.IsStatic == false)
                    return false;
                if (x.RaiseMethod?.IsStatic == false)
                    return false;
                return true;
            };
            MemberInfoTraits<Type>.IsRealStatic = x => x.IsAbstract && x.IsSealed;
            MemberInfoTraits<MemberInfo>.IsRealStatic = x => x switch
            {
                FieldInfo fi => fi.IsRealStatic(),
                PropertyInfo pi => pi.IsRealStatic(),
                MethodInfo mi => mi.IsRealStatic(),
                ConstructorInfo ci => ci.IsRealStatic(),
                EventInfo ei => ei.IsRealStatic(),
                Type t => t.IsRealStatic(),
                _ => false
            };
            
            MemberInfoTraits<FieldInfo>.GetRealMemberType = x => x.FieldType;
            MemberInfoTraits<PropertyInfo>.GetRealMemberType = x => x.PropertyType;
            MemberInfoTraits<MethodInfo>.GetRealMemberType = x => x.ReturnType;
            MemberInfoTraits<ConstructorInfo>.GetRealMemberType = x => x.DeclaringType;
            MemberInfoTraits<EventInfo>.GetRealMemberType = x => x.EventHandlerType;
            MemberInfoTraits<Type>.GetRealMemberType = x => x;
            MemberInfoTraits<MemberInfo>.GetRealMemberType = x => x switch
            {
                FieldInfo fi => fi.GetRealMemberType(),
                PropertyInfo pi => pi.GetRealMemberType(),
                MethodInfo mi => mi.GetRealMemberType(),
                ConstructorInfo ci => ci.GetRealMemberType(),
                EventInfo ei => ei.GetRealMemberType(),
                Type t => t.GetRealMemberType(),
                _ => null
            };

            MemberInfoTraits<FieldInfo>.IsRealCanRead = x => true;
            MemberInfoTraits<PropertyInfo>.IsRealCanRead = x => x.CanRead;
            MemberInfoTraits<MethodInfo>.IsRealCanRead = x => false;
            MemberInfoTraits<ConstructorInfo>.IsRealCanRead = x => false;
            MemberInfoTraits<EventInfo>.IsRealCanRead = x => false;
            MemberInfoTraits<Type>.IsRealCanRead = x => false;
            MemberInfoTraits<MemberInfo>.IsRealCanRead = x => x switch
            {
                FieldInfo fi => true,
                PropertyInfo pi => pi.CanRead,
                _ => false
            };
            
            MemberInfoTraits<FieldInfo>.IsRealCanWrite = x => true;
            MemberInfoTraits<PropertyInfo>.IsRealCanWrite = x => x.CanWrite;
            MemberInfoTraits<MethodInfo>.IsRealCanWrite = x => false;
            MemberInfoTraits<ConstructorInfo>.IsRealCanWrite = x => false;
            MemberInfoTraits<EventInfo>.IsRealCanWrite = x => false;
            MemberInfoTraits<Type>.IsRealCanWrite = x => false;
            MemberInfoTraits<MemberInfo>.IsRealCanWrite = x => x switch
            {
                FieldInfo fi => true,
                PropertyInfo pi => pi.CanWrite,
                _ => false
            };
            
        }
        public class MemberInfoTraits<T> where T : MemberInfo
        {
            private static Func<T, bool> s_IsRealPublic;
            private static Func<T, bool> s_IsRealStatic;
            private static Func<T, bool> s_IsRealCanRead;
            private static Func<T, bool> s_IsRealCanWrite;
            private static Func<T, Type> s_GetRealMemberType;
            private static Type s_Type = typeof(T);

            public static Func<T, bool> IsRealPublic
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    MemberInfoTraitsInit();
                    return s_IsRealPublic;
                }
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                set => s_IsRealPublic = value;
            }
            public static Func<T, bool> IsRealStatic
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    MemberInfoTraitsInit();
                    return s_IsRealStatic;
                }
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                set => s_IsRealStatic = value;
            }
            public static Func<T, bool> IsRealCanRead
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    MemberInfoTraitsInit();
                    return s_IsRealCanRead;
                }
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                set => s_IsRealCanRead = value;
            }
            public static Func<T, bool> IsRealCanWrite
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    MemberInfoTraitsInit();
                    return s_IsRealCanWrite;
                }
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                set => s_IsRealCanWrite = value;
            }
            public static Func<T, Type> GetRealMemberType
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    MemberInfoTraitsInit();
                    return s_GetRealMemberType;
                }
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                set => s_GetRealMemberType = value;
            }
        }


        static Dictionary<Type, bool> s_ValueTypeMap = new Dictionary<Type, bool>();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValueTypeFast(this Type self)
        {
            if (!s_ValueTypeMap.TryGetValue(self, out var value))
            {
                value = self.IsValueType;
                s_ValueTypeMap.Add(self, value);
            }
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNullable(this Type type)
        {
            return type.IsValueType && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsOneRankArray(this Type type)
        {
            return type.IsArray && type.GetArrayRank() == 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Is<T>(this Type type) => type.Is(typeof(T));
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Is(this Type type , Type baseType)
        {
            if (type == null)
                return false;

            return baseType.IsAssignableFrom(type);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object CreateInstance(this Type type)
        {
            return type != null ? Activator.CreateInstance(type) : null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object CreateInstance<T>()
        {
            return CreateInstance(typeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Cast<T>(this Delegate self) where T : Delegate => (T)self.Cast(typeof(T));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Delegate Cast(this Delegate self, Type type) => self != null && type != null ? Delegate.CreateDelegate(type, self.Target, self.Method) : null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object GetDefaultValue(FieldInfo field)
        {
            return field.GetValue(CreateInstance(field.DeclaringType));
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsArrayOrGenericList(this Type type)
        {
            return type.IsArray || type.IsGenericList();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsGenericList(this Type type)
        {
            return type.IsGenericType && typeof(IList).IsAssignableFrom(type);
        }
        
        public static bool GetArrayOrListElementType(this Type type, out Type elementType)
        {
            elementType = null;
            if (type.IsArray)
            {
                elementType = type.GetElementType();
                return true;
            }

            if (type.IsGenericType && typeof(IList).IsAssignableFrom(type))
            {
                elementType = type.GetGenericArguments().IdxOrDefault(0);
                return true;
            }

            return false;
        }


        public static bool IsGenericDictionary(this Type type)
        {
            if (typeof(IDictionary).IsAssignableFrom(type) && type.IsGenericType)
            {
                var types = type.GetGenericArguments();
                if (types.Length == 2)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool GetDictionaryElementType(this Type type, out Type keyType, out Type valueType)
        {
            keyType = null;
            valueType = null;
            if (typeof(IDictionary).IsAssignableFrom(type) && type.IsGenericType)
            {
                var types = type.GetGenericArguments();
                if (types.Length == 2)
                {
                    keyType = types[0];
                    valueType = types[1];
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断指定的类型 <paramref name="type"/> 是否是指定泛型类型的子类型，或实现了指定泛型接口。
        /// </summary>
        /// <param name="type">需要测试的类型。</param>
        /// <param name="generic">泛型接口类型，传入 typeof(IXxx&lt;&gt;)</param>
        /// <returns>如果是泛型接口的子类型，则返回 true，否则返回 false。</returns>
        public static bool HasImplementedRawGeneric(this Type type, Type generic)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (generic == null) throw new ArgumentNullException(nameof(generic));

            // 测试接口。
            var isTheRawGenericType = type.GetInterfaces().Any(IsTheRawGenericType);
            if (isTheRawGenericType) return true;

            // 测试类型。
            while (type != null && type != typeof(object))
            {
                isTheRawGenericType = IsTheRawGenericType(type);
                if (isTheRawGenericType) return true;
                type = type.BaseType;
            }

            // 没有找到任何匹配的接口或类型。
            return false;

            // 测试某个类型是否是指定的原始接口。
            bool IsTheRawGenericType(Type test)
                => generic == (test.IsGenericType ? test.GetGenericTypeDefinition() : test);
        }

        public static FieldInfo GetFieldInfo(this Type self, string name)
        {
            var r = self.GetField(name, (BindingFlags)(-1));
            if (r != null)
                return r;
            if (self.BaseType != null)
                return self.BaseType.GetFieldInfo(name);
            return null;
        }

        public static PropertyInfo GetPropertyInfo(this Type self, string name)
        {
            var r = self.GetProperty(name, (BindingFlags)(-1));
            if (r != null)
                return r;
            if (self.BaseType != null)
                return self.BaseType.GetPropertyInfo(name);
            return null;
        }


        public static MethodInfo GetAnyMethodInfo(this Type self, string name)
        {
            foreach (var method in self.GetMethods((BindingFlags)(-1)))
            {
                if (method.Name != name)
                    continue;
                return method;
            }

            if (self.BaseType != null)
                return self.BaseType.GetAnyMethodInfo(name);
            return null;
        }

        public static MethodInfo GetMethodInfoByParameterTypeNames(this Type self, string name, params string[] parameterTypes)
        {
            int paramCount = parameterTypes?.Length ?? 0;
            foreach (var method in self.GetMethods((BindingFlags)(-1)))
            {
                if (method.Name != name)
                    continue;
                var parameters = method.GetParameters();
                if (parameters.Length != paramCount)
                    continue;

                if (parameters.IsEquals(parameterTypes, (x, y) => x.ParameterType.FullName == y))
                    return method;
            }

            return self.BaseType?.GetMethodInfoByParameterTypeNames(name, parameterTypes);
        }
        
        public static MethodInfo GetMethodInfoByParameterTypes(this Type self, string name, params Type[] parameterTypes)
        {
            int paramCount = parameterTypes?.Length ?? 0;
            foreach (var method in self.GetMethods((BindingFlags)(-1)))
            {
                if (method.Name != name)
                    continue;
                var parameters = method.GetParameters();
                if (parameters.Length != paramCount)
                    continue;

                if (parameters.IsEquals(parameterTypes, (x, y) => (x.ParameterType.IsByRef ? x.ParameterType.GetElementType() : x.ParameterType) == y))
                    return method;
            }

            return self.BaseType?.GetMethodInfoByParameterTypes(name, parameterTypes);
        }
        
        public static ConstructorInfo GetConstructorInfoByParameterTypes(this Type self, params Type[] parameterTypes)
        {
            int paramCount = parameterTypes?.Length ?? 0;
            foreach (var constructor in self.GetConstructors((BindingFlags)(-1)))
            {
                var parameters = constructor.GetParameters();
                if (parameters.Length != paramCount)
                    continue;

                if (parameters.IsEquals(parameterTypes, (x, y) => (x.ParameterType.IsByRef ? x.ParameterType.GetElementType() : x.ParameterType) == y))
                    return constructor;
            }

            return null;
        }

        public static MethodInfo GetMethodInfo(this Type self, string name, int paramCount)
        {
            foreach (var method in self.GetMethods((BindingFlags)(-1)))
            {
                if (method.Name != name)
                    continue;
                var parameters = method.GetParameters();
                if (parameters.Length == paramCount)
                    return method;
            }

            if (self.BaseType != null)
                return self.BaseType.GetMethodInfo(name, paramCount);
            return null;
        }

        public static MethodInfo GetMethodInfo<T>(this Type self, string name)
        {
            foreach (var method in self.GetMethods((BindingFlags)(-1)))
            {
                if (method.Name != name)
                    continue;
                var parameters = method.GetParameters();
                if (parameters.Length == 1 && parameters[0].ParameterType == typeof(T))
                    return method;
            }

            if (self.BaseType != null)
                return self.BaseType.GetMethodInfo<T>(name);
            return null;
        }

        public static MethodInfo GetMethodInfo<T1, T2>(this Type self, string name)
        {
            foreach (var method in self.GetMethods((BindingFlags)(-1)))
            {
                if (method.Name != name)
                    continue;
                var parameters = method.GetParameters();
                if (parameters.Length == 2 && parameters[0].ParameterType == typeof(T1) &&
                    parameters[1].ParameterType == typeof(T2))
                    return method;
            }

            if (self.BaseType != null)
                return self.BaseType.GetMethodInfo<T1, T2>(name);
            return null;
        }

        public static MethodInfo GetMethodInfo<T1, T2, T3>(this Type self, string name)
        {
            foreach (var method in self.GetMethods((BindingFlags)(-1)))
            {
                if (method.Name != name)
                    continue;
                var parameters = method.GetParameters();
                if (parameters.Length == 3 && parameters[0].ParameterType == typeof(T1) &&
                    parameters[1].ParameterType == typeof(T2) && parameters[2].ParameterType == typeof(T3))
                    return method;
            }

            if (self.BaseType != null)
                return self.BaseType.GetMethodInfo<T1, T2, T3>(name);
            return null;
        }

        public static MethodInfo GetMethodInfo<T1, T2, T3, T4>(this Type self, string name)
        {
            foreach (var method in self.GetMethods((BindingFlags)(-1)))
            {
                if (method.Name != name)
                    continue;
                var parameters = method.GetParameters();
                if (parameters.Length == 4 && parameters[0].ParameterType == typeof(T1) &&
                    parameters[1].ParameterType == typeof(T2) && parameters[2].ParameterType == typeof(T3) &&
                    parameters[3].ParameterType == typeof(T4))
                    return method;
            }

            if (self.BaseType != null)
                return self.BaseType.GetMethodInfo<T1, T2, T3, T4>(name);
            return null;
        }

        public static MethodInfo GetMethodInfo<T1, T2, T3, T4, T5>(this Type self, string name)
        {
            foreach (var method in self.GetMethods((BindingFlags)(-1)))
            {
                if (method.Name != name)
                    continue;
                var parameters = method.GetParameters();
                if (parameters.Length == 5 && parameters[0].ParameterType == typeof(T1) &&
                    parameters[1].ParameterType == typeof(T2) && parameters[2].ParameterType == typeof(T3) &&
                    parameters[3].ParameterType == typeof(T4) && parameters[4].ParameterType == typeof(T5))
                    return method;
            }

            if (self.BaseType != null)
                return self.BaseType.GetMethodInfo<T1, T2, T3, T4, T5>(name);
            return null;
        }

        public static MethodInfo GetMethodInfo<T1, T2, T3, T4, T5, T6>(this Type self, string name)
        {
            foreach (var method in self.GetMethods((BindingFlags)(-1)))
            {
                if (method.Name != name)
                    continue;
                var parameters = method.GetParameters();
                if (parameters.Length == 6 && parameters[0].ParameterType == typeof(T1) &&
                    parameters[1].ParameterType == typeof(T2) && parameters[2].ParameterType == typeof(T3) &&
                    parameters[3].ParameterType == typeof(T4) && parameters[4].ParameterType == typeof(T5) &&
                    parameters[5].ParameterType == typeof(T6))
                    return method;
            }

            if (self.BaseType != null)
                return self.BaseType.GetMethodInfo<T1, T2, T3, T4, T5, T6>(name);
            return null;
        }


        static object[] _sParam0Array = new object[0];
        static FixedArrayPool<object> _sParam1ArrayPool = new FixedArrayPool<object>(1);
        static FixedArrayPool<object> _sParam2ArrayPool = new FixedArrayPool<object>(2);
        static FixedArrayPool<object> _sParam3ArrayPool = new FixedArrayPool<object>(3);
        static FixedArrayPool<object> _sParam4ArrayPool = new FixedArrayPool<object>(4);

        public static void Invoke(this MethodInfo self, object obj)
        {
            self.Invoke(obj, _sParam0Array);
        }

        public static void Invoke(this MethodInfo self, object obj, object t1)
        {
            var ps = _sParam1ArrayPool.Rent();
            ps[0] = t1;
            self.Invoke(obj, ps);
            _sParam1ArrayPool.Return(ps);
        }

        public static void Invoke(this MethodInfo self, object obj, object t1, object t2)
        {
            var ps = _sParam2ArrayPool.Rent();
            ps[0] = t1;
            ps[1] = t2;
            self.Invoke(obj, ps);
            _sParam2ArrayPool.Return(ps);
        }

        public static void Invoke(this MethodInfo self, object obj, object t1, object t2, object t3)
        {
            var ps = _sParam3ArrayPool.Rent();
            ps[0] = t1;
            ps[1] = t2;
            ps[2] = t3;
            self.Invoke(obj, ps);
            _sParam3ArrayPool.Return(ps);
        }
        
        public static void Invoke(this MethodInfo self, object obj, object t1, object t2, object t3, object t4)
        {
            var ps = _sParam4ArrayPool.Rent();
            ps[0] = t1;
            ps[1] = t2;
            ps[2] = t3;
            ps[3] = t4;
            self.Invoke(obj, ps);
            _sParam4ArrayPool.Return(ps);
        }

        public static R Invoke<R>(this MethodInfo self, object obj)
        {
            return (R)self.Invoke(obj, _sParam0Array);
        }

        public static R Invoke<R>(this MethodInfo self, object obj, object t1)
        {
            var ps = _sParam1ArrayPool.Rent();
            ps[0] = t1;
            var r = self.Invoke(obj, ps);
            _sParam1ArrayPool.Return(ps);
            return (R)r;
        }

        public static R Invoke<R>(this MethodInfo self, object obj, object t1, object t2)
        {
            var ps = _sParam2ArrayPool.Rent();
            ps[0] = t1;
            ps[1] = t2;
            var r = self.Invoke(obj, ps);
            _sParam2ArrayPool.Return(ps);
            return (R)r;
        }

        public static R Invoke<R>(this MethodInfo self, object obj, object t1, object t2, object t3)
        {
            var ps = _sParam3ArrayPool.Rent();
            ps[0] = t1;
            ps[1] = t2;
            ps[2] = t3;
            var r = self.Invoke(obj, ps);
            _sParam3ArrayPool.Return(ps);
            return (R)r;
        }
        
        public static R Invoke<R>(this MethodInfo self, object obj, object t1, object t2, object t3, object t4)
        {
            var ps = _sParam4ArrayPool.Rent();
            ps[0] = t1;
            ps[1] = t2;
            ps[2] = t3;
            ps[3] = t4;
            var r = self.Invoke(obj, ps);
            _sParam4ArrayPool.Return(ps);
            return (R)r;
        }
    }
}