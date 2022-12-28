using System;
using System.Reflection;

namespace Daihenka.AssetPipeline.ReflectionMagic
{
    /// <summary>
    /// Provides an mechanism to access properties through the <see cref="IDynamicMember"/> abstraction.
    /// </summary>
    internal class DynamicProperty : IDynamicMember
    {
        readonly PropertyInfo m_PropertyInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicProperty"/> class wrapping the specified property.
        /// </summary>
        /// <param name="property">The property info to wrap.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="property"/> is <c>null</c>.</exception>
        internal DynamicProperty(PropertyInfo property)
        {
            m_PropertyInfo = property ?? throw new ArgumentNullException(nameof(property));
        }

        public Type PropertyType => m_PropertyInfo.PropertyType;

        string IDynamicMember.Name => m_PropertyInfo.Name;

        object IDynamicMember.GetValue(object obj, object[] index)
        {
            return m_PropertyInfo.GetValue(obj, index);
        }

        void IDynamicMember.SetValue(object obj, object value, object[] index)
        {
            m_PropertyInfo.SetValue(obj, value, index);
        }
    }
}