using System;
using UnityEngine;

namespace Bewildered.SmartLibrary
{
    /// <summary>
    /// A serializable reference to a <see cref="System.Type"/>.
    /// </summary>
    [Serializable]
    public class TypeReference : ISerializationCallbackReceiver
    {
        private Type _type = null;
        private bool _typeChanged = false;

        [SerializeField] private string _typeName = string.Empty;

        /// <summary>
        /// The <see cref="System.Type"/> being referenced by the <see cref="TypeReference"/>.
        /// </summary>
        public Type Type
        {
            get { return _type; }
            set
            {
                if (value != _type)
                {
                    _type = value;
                    _typeChanged = true;
                }
            }
        }

        public TypeReference() { }

        public TypeReference(Type type)
        {
            _type = type;
            _typeName = _type?.AssemblyQualifiedName;
        }

        public TypeReference(string assemblyQualifiedTypeName)
        {
            _typeName = assemblyQualifiedTypeName;
            _type = Type.GetType(_typeName);
        }

        public static implicit operator Type(TypeReference serializableType)
        {
            return serializableType?.Type;
        }

        public static implicit operator TypeReference(Type type)
        {
            return new TypeReference(type);
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            string currentTypeName = _type != null ? _type.AssemblyQualifiedName : string.Empty;

            if (!_typeChanged && currentTypeName != _typeName)
            {
                if (string.IsNullOrEmpty(_typeName))
                    _type = null;
                else
                    _type = Type.GetType(_typeName);
            }

            if (_type != null)
                _typeName = currentTypeName;

            _typeChanged = false;
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            _type = Type.GetType(_typeName);
        }

        public override int GetHashCode()
        {
            return _type != null ? _type.GetHashCode() : 0;
        }
    } 
}
