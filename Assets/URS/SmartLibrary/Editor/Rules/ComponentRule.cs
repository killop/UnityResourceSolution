using System;
using UnityEngine;

namespace Bewildered.SmartLibrary
{
    /// <summary>
    /// Checks if an object has a <see cref="Component"/> of the specified <see cref="System.Type"/>.
    /// </summary>
    public class ComponentRule : LibraryRuleBase
    {
        [SerializeField] private TypeReference _type = new TypeReference();

        /// <summary>
        /// The <see cref="System.Type"/> of <see cref="Component"/> that the object must have.
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

            using var scope = new AssetUtility.LoadAssetScope(item.GUID);
            
            if (scope.Asset is GameObject gameObjectAsset)
            {
                return gameObjectAsset.TryGetComponent(_type, out _);
            }

            return false;
        }
    }
}
