using System;
using UnityEngine;
using UnityEditor;

namespace Bewildered.SmartLibrary
{
    /// <summary>
    /// The types of prefabs there are that can be checked for by the <see cref="PrefabRule"/>.
    /// </summary>
    public enum PrefabType { Any, Regular, Model, Variant }
    
    /// <summary>
    /// Checks if an object is a prefab of the specified type.
    /// </summary>
    public class PrefabRule : LibraryRuleBase
    {
        [SerializeField] private PrefabType _prefabType;

        /// <summary>
        /// The type of prefab to check for.
        /// </summary>
        public PrefabType PrefabType
        {
            get { return _prefabType; }
        }
        
        /// <inheritdoc/>
        public override string SearchQuery
        {
            get { return "prefab:" + _prefabType.ToString().ToLowerInvariant(); }
        }
        
        /// <inheritdoc/>
        public override bool Matches(LibraryItem item)
        {
            using (var scope = new AssetUtility.LoadAssetScope(item.GUID))
            {
                if (scope.Asset == null)
                    return false;
                
                switch (_prefabType)
                {
                    case PrefabType.Any:
                        return PrefabUtility.IsPartOfAnyPrefab(scope.Asset);
                    case PrefabType.Regular:
                        return PrefabUtility.GetPrefabAssetType(scope.Asset) == PrefabAssetType.Regular;
                    case PrefabType.Model:
                        return PrefabUtility.GetPrefabAssetType(scope.Asset) == PrefabAssetType.Model;
                    case PrefabType.Variant:
                        return PrefabUtility.IsPartOfVariantPrefab(scope.Asset);
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}
