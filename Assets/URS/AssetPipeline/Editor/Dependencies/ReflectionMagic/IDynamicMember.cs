using System;

namespace Daihenka.AssetPipeline.ReflectionMagic
{
    /// <summary>
    /// Defines an mechanism to access members (e.g. fields or properties) of objects in a consistent way.
    /// </summary>
    public interface IDynamicMember
    {
        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the type of the property.
        /// </summary>
        Type PropertyType { get; }

        /// <summary>
        /// Returns the property value of a specified object with optional index values for indexed properties.
        /// </summary>
        /// <param name="obj">The object whose property value will be returned. </param>
        /// <param name="index">Optional index values for indexed properties. The indexes of indexed properties are zero-based. This value should be null for non-indexed properties. </param>
        /// <returns>The member value of the specified object.</returns>
        object GetValue(object obj, object[] index);

        /// <summary>
        /// Sets the property value of a specified object with optional index values for index properties.
        /// </summary>
        /// <param name="obj">The object whose property value will be set. </param>
        /// <param name="value">The new property value. </param>
        /// <param name="index">Optional index values for indexed properties. This value should be null for non-indexed properties. </param>
        void SetValue(object obj, object value, object[] index);
    }
}