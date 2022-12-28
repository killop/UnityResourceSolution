using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace Bewildered.SmartLibrary
{
    /// <summary>
    /// Stores data for the library or collections to use during the editor session.
    /// </summary>
    internal class SessionData : ScriptableSingleton<SessionData>
    {
        [SerializeField] private RootLibraryCollection _rootCollection;
        
        [SerializeField] private SerializableDictionary<UniqueID, LibraryCollection> _idToCollectionMap =
            new SerializableDictionary<UniqueID, LibraryCollection>();

        [SerializeField] private SerializableDictionary<TypeReference, List<LibraryCollection>> _typeCollectionPairs =
            new SerializableDictionary<TypeReference, List<LibraryCollection>>();

        internal RootLibraryCollection RootCollection
        {
            get
            {
                if (!Directory.Exists(LibraryConstants.CollectionsPath) && _rootCollection == null)
                {
                    LibraryData.ConvertToNewSave();
                }

                if (_rootCollection == null)
                {
                    var roots = Resources.FindObjectsOfTypeAll<RootLibraryCollection>();

                    if (roots.Length > 0)
                        _rootCollection = roots[0];
                }
                
                if (_rootCollection == null)
                {
                    LibraryUtility.LoadAllCollections();
                }
                
                return _rootCollection;
            }
            set { _rootCollection = value; }
        }

        public SerializableDictionary<UniqueID, LibraryCollection> IDToCollectionMap
        {
            get { return _idToCollectionMap; }
            set { _idToCollectionMap = value; }
        }

        public SerializableDictionary<TypeReference, List<LibraryCollection>> TypeCollectionPairs
        {
            get { return _typeCollectionPairs; }
            set { _typeCollectionPairs = value; }
        }
        
        /// <summary>
        /// Caches data about a specified <see cref="LibraryCollection"/> for efficient lookups later.
        /// </summary>
        /// <param name="collection">The <see cref="LibraryCollection"/> to cache the data for.</param>
        /// <param name="registerUndo">
        /// Whether performing an undo should undo the caching of the data for the <see cref="LibraryCollection"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">The <paramref name="collection"/> is <c>null</c>.</exception>
        /// <remarks>
        /// Caches the <see cref="Type"/> and <see cref="LibraryCollection.ID"/>
        /// of the <see cref="LibraryCollection"/>.
        /// </remarks>
        internal static void CacheCollectionData(LibraryCollection collection, bool registerUndo = true)
        {
            if (collection == null)
                throw new ArgumentNullException();
            
            if (registerUndo)
                Undo.RegisterCompleteObjectUndo(instance, "Cached collection data to SessionData");

            // This CacheCollectionData method is called in the method that loads collections which
            // creates and populates the id map. So we need to check if it has already been added.
            // We also cannot use the index accessor as that would modify the dictionary even if nothing changed.
            if (!instance._idToCollectionMap.ContainsKey(collection.ID))
                instance._idToCollectionMap.Add(collection.ID, collection);
            
            var collectionType = new TypeReference(collection.GetType());
            if (!instance.TypeCollectionPairs.ContainsKey(collectionType))
                instance.TypeCollectionPairs.Add(collectionType, new List<LibraryCollection>());
            instance.TypeCollectionPairs[collectionType].Add(collection);
        }
        
        /// <summary>
        /// Removes the cached data of a specified <see cref="LibraryCollection"/>.
        /// </summary>
        /// <param name="collection">The <see cref="LibraryCollection"/> to remove the data for.</param>
        /// <param name="registerUndo">
        /// Whether performing an undo will undo the removing of the cached data for the <see cref="LibraryCollection"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">The <paramref name="collection"/> is <c>null</c>.</exception>
        internal static void RemoveCollectionData(LibraryCollection collection, bool registerUndo = true)
        {
            if (collection == null)
                throw new ArgumentNullException();
            
            if (registerUndo)
                Undo.RegisterCompleteObjectUndo(instance, "Removed collection data from SessionData");

            instance._idToCollectionMap.Remove(collection.ID);
            
            var collectionType = new TypeReference(collection.GetType());
            if (instance.TypeCollectionPairs.ContainsKey(collectionType))
                instance.TypeCollectionPairs[collectionType].Remove(collection);
        }
    }
}
