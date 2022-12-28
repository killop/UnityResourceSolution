using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;


namespace Bewildered.SmartLibrary
{
    [AddComponentMenu("")] // An empty string will hide the component from the menu.
    public class CollectionDefaultParent : MonoBehaviour
    {
        [SerializeField] private List<UniqueID> _collectionIds = new List<UniqueID>();

        public List<UniqueID> CollectionIds
        {
            get { return _collectionIds; }
        }
    }
}
