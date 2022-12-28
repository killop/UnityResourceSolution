using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace Daihenka.AssetPipeline.ReflectionMagic
{
    public class PrivateReflectionDynamicObjectInstance : PrivateReflectionDynamicObjectBase
    {
        static readonly ConcurrentDictionary<Type, IDictionary<string, IDynamicMember>> s_PropertiesOnType = new ConcurrentDictionary<Type, IDictionary<string, IDynamicMember>>();

        readonly object m_Instance;

        /// <summary>
        /// Initializes a new instance of the <see cref="PrivateReflectionDynamicObjectInstance"/> class, wrapping the specified object.
        /// </summary>
        /// <param name="instance">The object to wrap.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="instance"/> is <c>null</c>.</exception>
        public PrivateReflectionDynamicObjectInstance(object instance)
        {
            m_Instance = instance ?? throw new ArgumentNullException(nameof(instance));
        }

        protected override IDictionary<Type, IDictionary<string, IDynamicMember>> PropertiesOnType => s_PropertiesOnType;

        // For instance calls, we get the type from the instance
        protected override Type TargetType => m_Instance.GetType();

        protected override object Instance => m_Instance;

        /// <summary>
        /// The object that the <see cref="PrivateReflectionDynamicObjectInstance"/> wraps.
        /// </summary>
        public override object RealObject => Instance;

        protected override BindingFlags BindingFlags => BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    }
}