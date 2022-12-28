using System;
using System.Reflection;

namespace Daihenka.AssetPipeline.ReflectionMagic
{
    /// <summary>
    /// Provides a mechanism to access fields through the <see cref="IDynamicMember"/> abstraction.
    /// </summary>
    internal class DynamicField : IDynamicMember
    {
        readonly FieldInfo m_FieldInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicField"/> class wrapping the specified field.
        /// </summary>
        /// <param name="field">The field info to wrap.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="field"/> is <c>null</c>.</exception>
        internal DynamicField(FieldInfo field)
        {
            m_FieldInfo = field ?? throw new ArgumentNullException(nameof(field));
        }

        public Type PropertyType => m_FieldInfo.FieldType;

        string IDynamicMember.Name => m_FieldInfo.Name;

        object IDynamicMember.GetValue(object obj, object[] index)
        {
            return m_FieldInfo.GetValue(obj);
        }

        void IDynamicMember.SetValue(object obj, object value, object[] index)
        {
            m_FieldInfo.SetValue(obj, value);
        }
    }
}