using System;
using System.IO;
using UnityEngine;
using UnityEditor;

using Object = UnityEngine.Object;

namespace Bewildered.SmartLibrary
{
    /// <summary>
    /// A wrapper around an asset Object for effecient referencing and accessing information about it.
    /// </summary>
    [Serializable]
    public class LibraryItem
    {
        [SerializeField] private string _guid;

        [NonSerialized] private string _fileName;
        [NonSerialized] private string _path;
        [NonSerialized] private int _instanceID;
        [NonSerialized] private Type _type;
        
        /// <summary>
        /// Returns the GUID of the asset referenced by the <see cref="LibraryItem"/>.
        /// </summary>
        public string GUID
        {
            get { return _guid; }
        }

        /// <summary>
        /// Returns the InstanceID of the asset referenced by the <see cref="LibraryItem"/>.
        /// </summary>
        public int InstanceID
        {
            get
            {
                if (_instanceID == 0)
                    _instanceID = AssetUtility.GetMainAssetInstanceIDFromGUID(_guid);

                return _instanceID;
            }
        }

        /// <summary>
        /// Returns the project relative path of the asset referenced by the <see cref="LibraryItem"/>.
        /// </summary>
        public string AssetPath
        {
            get
            {
                SetFileNameAndPath();

                return _path;
            }
        }

        /// <summary>
        /// Returns the unedited file name of the asset referenced by the <see cref="LibraryItem"/>. Does not include extention.
        /// </summary>
        public string Name
        {
            get
            {
                SetFileNameAndPath();

                return _fileName;
            }
        }

        /// <summary>
        /// Returns the pretty name of the file name of the asset referenced by the <see cref="LibraryItem"/>.
        /// </summary>
        public string DisplayName
        {
            get
            {
                SetFileNameAndPath();

                return LibraryUtility.PrettyFileName(_fileName);
            }
        }

        /// <summary>
        /// Returns the name of the <see cref="System.Type"/> of the asset referenced by the <see cref="LibraryItem"/>.
        /// </summary>
        public Type Type
        {
            get
            {
                if (_type == null)
                    _type = AssetDatabase.GetMainAssetTypeAtPath(AssetPath);

                if (_type == null)
                    _type = typeof(Object);

                return _type;
            }
        }

        public LibraryItem(Object obj)
        {
            if (!AssetDatabase.Contains(obj))
            {
                Debug.LogWarning($"Cannot make LibraryItem from object '{(obj == null ? null : obj.name)}' as it is not an asset.");
            }
            else
            {
                _path = AssetDatabase.GetAssetPath(obj);
                _guid = AssetDatabase.AssetPathToGUID(_path);
                _fileName = Path.GetFileNameWithoutExtension(_path);
                _instanceID = 0;
                _type = AssetDatabase.GetMainAssetTypeAtPath(_path);
            }
        }

        public LibraryItem(string guid)
        {
            _path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(_path))
                throw new ArgumentException($"No asset associated with the provided guid '{guid}'.", nameof(guid));

            _guid = guid;
            _fileName = Path.GetFileNameWithoutExtension(_path);
            _instanceID = 0;
            _type = AssetDatabase.GetMainAssetTypeAtPath(_path);
        }

        public static implicit operator LibraryItem(Object obj)
        {
            return GetItemInstance(obj);
        }

        private void SetFileNameAndPath()
        {
            if (string.IsNullOrEmpty(_fileName))
            {
                _path = AssetDatabase.GUIDToAssetPath(_guid);
                _fileName = Path.GetFileNameWithoutExtension(_path);
            }
        }

        internal void ClearNonSerializedFields()
        {
            _path = "";
            _fileName = "";
            _type = null;
        }

        public override string ToString()
        {
            return $"(path:{AssetDatabase.GUIDToAssetPath(_guid)}, guid:{_guid})";
        }

        public static LibraryItem GetItemInstance(Object obj)
        {
            return GetItemInstance(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj)));
        }
        public static LibraryItem GetItemInstanceByPath(string path)
        {
            return GetItemInstance(AssetDatabase.AssetPathToGUID(path));
        }
        public static LibraryItem GetItemInstance(string guid)
        {
            if (!LibraryDatabase.RootCollection.AssetGuidToItem.TryGetValue(guid, out var item))
            {
                item = new LibraryItem(guid);
                LibraryDatabase.RootCollection.AssetGuidToItem.Add(guid, item);
            }

            return item;
        }
    }
}
