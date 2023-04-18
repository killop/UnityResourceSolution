//This file was automatically generated by kuroneko.
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace NinjaBeats.ReflectionHelper
{
	public partial struct UnityEditor_GUISlideGroup
	{
		
		/// <summary>
		/// <see cref="UnityEditor.GUISlideGroup"/>
		/// </summary>
		public static Type __type__ { get; } = EditorUtils.GetTypeByFullName("UnityEditor.GUISlideGroup");
		
		
		public static UnityEditor_GUISlideGroup current
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new UnityEditor_GUISlideGroup(__current?.GetValue(null));
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => __current?.SetValue(null, value.__self__);
		}
		
		public System.Collections.Generic.Dictionary<int, UnityEngine.Rect> animIDs
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => (System.Collections.Generic.Dictionary<int, UnityEngine.Rect>)(__animIDs?.GetValue(__self__));
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => __animIDs?.SetValue(__self__, value);
		}
		
		public static float kLerp
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => (float)(__kLerp?.GetValue(null));
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => __kLerp?.SetValue(null, value);
		}
		
		public static float kSnap
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => (float)(__kSnap?.GetValue(null));
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => __kSnap?.SetValue(null, value);
		}
		
		public void Begin()
		{
			__Begin?.Invoke(__self__, System.Array.Empty<object>());
		}
		
		public void End()
		{
			__End?.Invoke(__self__, System.Array.Empty<object>());
		}
		
		public void Reset()
		{
			__Reset?.Invoke(__self__, System.Array.Empty<object>());
		}
		
		public UnityEngine.Rect BeginHorizontal(int id, UnityEngine.GUILayoutOption[] options)
		{
			var __pool__ = FixedArrayPool<object>.Shared(2);
			var __params__ = __pool__.Rent();
			__params__[0] = id;
			__params__[1] = options;
			var __result__ = __BeginHorizontal?.Invoke(__self__, __params__);
			__pool__.Return(__params__);
			return __result__ != null ? (UnityEngine.Rect)(__result__) : default;
		}
		
		public void EndHorizontal()
		{
			__EndHorizontal?.Invoke(__self__, System.Array.Empty<object>());
		}
		
		public UnityEngine.Rect GetRect(int id, UnityEngine.Rect r)
		{
			var __pool__ = FixedArrayPool<object>.Shared(2);
			var __params__ = __pool__.Rent();
			__params__[0] = id;
			__params__[1] = r;
			var __result__ = __GetRect?.Invoke(__self__, __params__);
			__pool__.Return(__params__);
			return __result__ != null ? (UnityEngine.Rect)(__result__) : default;
		}
		
		public UnityEngine.Rect GetRect(int id, UnityEngine.Rect r, out bool changed)
		{
			var __pool__ = FixedArrayPool<object>.Shared(3);
			var __params__ = __pool__.Rent();
			__params__[0] = id;
			__params__[1] = r;
			__params__[2] = null;
			var __result__ = __GetRect__2?.Invoke(__self__, __params__);
			changed = (bool)(__params__[2]);
			__pool__.Return(__params__);
			return __result__ != null ? (UnityEngine.Rect)(__result__) : default;
		}
		
		public UnityEditor_GUISlideGroup(object __self__) => this.__self__ = __self__ as object;
		public object __self__;
		public bool __valid__
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => __self__ != null && __type__ != null;
		}
		
		private static FieldInfo ___current;
		private static FieldInfo __current
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ___current ??= __type__?.GetField("current", (BindingFlags)(-1));
		}
		
		private static FieldInfo ___animIDs;
		private static FieldInfo __animIDs
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ___animIDs ??= __type__?.GetField("animIDs", (BindingFlags)(-1));
		}
		
		private static FieldInfo ___kLerp;
		private static FieldInfo __kLerp
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ___kLerp ??= __type__?.GetField("kLerp", (BindingFlags)(-1));
		}
		
		private static FieldInfo ___kSnap;
		private static FieldInfo __kSnap
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ___kSnap ??= __type__?.GetField("kSnap", (BindingFlags)(-1));
		}
		
		private static MethodInfo ___Begin;
		private static MethodInfo __Begin
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ___Begin ??= __type__?.GetMethodInfoByParameterTypeNames("Begin");
		}
		
		private static MethodInfo ___End;
		private static MethodInfo __End
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ___End ??= __type__?.GetMethodInfoByParameterTypeNames("End");
		}
		
		private static MethodInfo ___Reset;
		private static MethodInfo __Reset
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ___Reset ??= __type__?.GetMethodInfoByParameterTypeNames("Reset");
		}
		
		private static MethodInfo ___BeginHorizontal;
		private static MethodInfo __BeginHorizontal
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ___BeginHorizontal ??= __type__?.GetMethodInfoByParameterTypeNames("BeginHorizontal", "System.Int32", "UnityEngine.GUILayoutOption[]");
		}
		
		private static MethodInfo ___EndHorizontal;
		private static MethodInfo __EndHorizontal
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ___EndHorizontal ??= __type__?.GetMethodInfoByParameterTypeNames("EndHorizontal");
		}
		
		private static MethodInfo ___GetRect;
		private static MethodInfo __GetRect
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ___GetRect ??= __type__?.GetMethodInfoByParameterTypeNames("GetRect", "System.Int32", "UnityEngine.Rect");
		}
		
		private static MethodInfo ___GetRect__2;
		private static MethodInfo __GetRect__2
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ___GetRect__2 ??= __type__?.GetMethodInfoByParameterTypeNames("GetRect", "System.Int32", "UnityEngine.Rect", "System.Boolean&");
		}
	}
}