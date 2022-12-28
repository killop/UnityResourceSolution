using System;
using UnityEngine;

namespace Bewildered.SmartLibrary
{
     /// <summary>
    /// A stable, Globally Unique Identifier. Instances of prefabs will have the same Id as the source prefab, thus making it not unique for them.
    /// </summary>
    [Serializable]
    public struct UniqueID : ISerializationCallbackReceiver
    {
        private Guid _guid;

        // Use bytes because strings allocate memory and is twice as slow.
        [SerializeField] private byte[] _serializedGuid;

        public static readonly UniqueID Empty = new UniqueID() { _guid = Guid.Empty, _serializedGuid = null };

        public UniqueID(string id)
        {
            _guid = new Guid(id);
            _serializedGuid = _guid.ToByteArray();
        }

        public UniqueID(byte[] b)
        {
            _guid = new Guid(b);
            _serializedGuid = b;
        }

        public static UniqueID NewUniqueId()
        {
            var guid = Guid.NewGuid();
            UniqueID id = new UniqueID
            {
                _guid = guid,
                _serializedGuid = guid.ToByteArray()
            };
            return id;
        }

        public static bool operator ==(UniqueID lhs, UniqueID rhs)
        {
            return lhs._guid == rhs._guid;
        }

        public static bool operator !=(UniqueID lhs, UniqueID rhs)
        {
            return lhs._guid != rhs._guid;
        }

        public override bool Equals(object obj)
        {
            return obj is UniqueID id && _guid.Equals(id._guid);
        }

        public override int GetHashCode()
        {
            return _guid.GetHashCode();
        }

        public override string ToString()
        {
            return _guid.ToString();
        }

        public byte[] ToByteArray()
        {
            return _serializedGuid;
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            if (_guid != Guid.Empty)
            {
                _serializedGuid = _guid.ToByteArray();
            }
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (_serializedGuid != null && _serializedGuid.Length == 16)
            {
                _guid = new Guid(_serializedGuid);
            }
        }
    }
}
