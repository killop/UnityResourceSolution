using System;
using UnityEngine;

namespace Bewildered.SmartLibrary
{
    /// <summary>
    /// Check if an object's <see cref="System.Type"/> matches a specified type.
    /// </summary>
    public class TypeRule : LibraryRuleBase
    {
        [SerializeField] private TypeReference _type = new TypeReference();

        /// <summary>
        /// The <see cref="System.Type"/> that the object must be.
        /// </summary>
        public Type Type
        {
            get { return _type; }
            set { _type.Type = Type; }
        }

        /// <inheritdoc/>
        public override string SearchQuery
        {
            get { return _type.Type == null ? string.Empty : "t:" + _type.Type.Name; }
        }

        /// <inheritdoc/>
        public override bool Matches(LibraryItem item)
        {
            // If no type is set, we treat it like the rule doesn't exist so we return true.
            if (_type.Type == null)
                return true;

            return item.Type == _type.Type;
        }
    }
}