using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Linq.Expressions;

using UnityEngine;

namespace UnityEditor.Build.Pipeline.Utilities.USerialize
{
    /*
     * Main USerialize deserialzation class.  Used to read instances of types from a stream
     * 
     * To DeSerialize an object from a stream use code such as
     * 
     *    USerialize.DeSerializer deserializer = new USerialize.DeSerializer();
     *    CachedInfo info = deserializer.DeSerialize<CachedInfo>(new MemoryStream(File.ReadAllBytes(filepath), false));
     *
     */
    internal class DeSerializer
    {
        // Flag that can be set to true prior to deserialization to cause a detailed log of the data read to be emitted to the console.  Will slow down deserialization considerably
        internal bool EmitVerboseLog = false;

        // Cached data about a field in a type we've encountered
        internal class FieldData
        {
            internal FieldInfo m_FieldInfo;
            internal object m_Setter;
            internal object m_Getter;
        }

        // Cached data about a type we've encountered
        internal class TypeData
        {
            // Data for each of the fields in this type, keyed by field name
            internal Dictionary<string, FieldData> m_Fields;

            // LUT that maps field indices in the serialized data to fields in the object in memory.  Reset every time a new object is deserialized in case they were created at different times and the data has changed
            internal FieldData[] m_FieldIndex;

            // Type of this type
            internal Type m_Type;

            // Custom delegate we've been given by the client code to create an instance of this type.  It is much faster to have the client code create the instances directly than to use the generic Activator system
            internal ObjectFactory m_ObjectFactory;
        }

        // Delegate type the client can supply to create instances of a specific type. It is much faster to have the client code create the instances directly than to use the generic Activator system
        internal delegate object ObjectFactory();

        // Custom deserializers the client code has supplied to deserialize specific types.  Usually used for types that the standard reflection based serialization cannot cope with.  Keyed by Type
        Dictionary<Type, ICustomSerializer> m_CustomSerializers = new Dictionary<Type, ICustomSerializer>();

        // Delegates supplied by the client to create instances of specific types, keyed by type name. It is much faster to have the client code create the instances directly than to use the generic Activator system.
        Dictionary<Type, ObjectFactory> m_ObjectFactories = new Dictionary<Type, ObjectFactory>();

        // Cache of data about types we have encountered, provides better performance than querying the slow reflection API repeatedly
        Dictionary<string, TypeData> m_TypeDataCache = new Dictionary<string, TypeData>();

        // Version of the object that was deserialized.  This is the value supplied by the client code when Serializer.Serialize() was originally called
        int m_ObjectVersion;
        internal int ObjectVersion { get { return m_ObjectVersion; } }

        // Reader we are using to read bytes from the stream
        BinaryReader m_Reader;

        // Version of the serialization format itself read from the stream.  Exists to provide data upgrade potential in the future
        byte m_SerializationVersion;

        // The type/field name stringtable read from the stream
        string[] m_TypeStringTable;
        long m_TypeStringTableBytePos;

        // The data value stringtable read from the stream
        string[] m_DataStringTable;
        long m_DataStringTableBytePos;

        // LUT to get TypeData from a type name index for this object. Maps type name indices in the serialized data to TypeData entries in m_TypeDataCache (which persists between objects)
        TypeData[] m_TypeDataByTypeNameIndex;

        // LUT to get a Type instance from a type name index for this object.
        Type[] m_TypeByTypeNameIndex;

        internal DeSerializer()
        {
        }

        internal DeSerializer(ICustomSerializer[] customSerializers, (Type, ObjectFactory)[] objectFactories)
        {
            if (customSerializers != null)
                Array.ForEach(customSerializers, (customSerializer) => AddCustomSerializer(customSerializer));

            if (objectFactories != null)
                Array.ForEach(objectFactories, (item) => AddObjectFactory(item.Item1, item.Item2));
        }

        string[] ReadStringTable(long streamPosition)
        {
            m_Reader.BaseStream.Position = streamPosition;

            int numStrings = m_Reader.ReadInt32();
            string[] stringTable = new string[numStrings];
            for (int stringNum = 0; stringNum < numStrings; stringNum++)
            {
                stringTable[stringNum] = m_Reader.ReadString();
            }
            return stringTable;
        }

        void ReadStringTables()
        {
            m_TypeStringTableBytePos = m_Reader.ReadInt64();
            m_DataStringTableBytePos = m_Reader.ReadInt64();

            long dataStartPos = m_Reader.BaseStream.Position;

            m_TypeStringTable = ReadStringTable(m_TypeStringTableBytePos);
            m_DataStringTable = ReadStringTable(m_DataStringTableBytePos);

            m_Reader.BaseStream.Position = dataStartPos;
        }

        // Clear data that we cache about types and object contents that can change between objects.
        void ClearPerObjectCachedData()
        {
            // Clear the field index -> field data LUT as this can change between objects
            foreach (KeyValuePair<string, TypeData> typeDataCacheEntry in m_TypeDataCache)
            {
                typeDataCacheEntry.Value.m_FieldIndex = null;
            }

            // Clear the type name index -> type data LUT as this can change between objects
            m_TypeDataByTypeNameIndex = new TypeData[m_TypeStringTable.Length];
            m_TypeByTypeNameIndex = new Type[m_TypeStringTable.Length];
        }

        // Main deserialize function.  Creates and reads an instance of the specified type from the stream
        internal ClassType DeSerialize<ClassType>(Stream stream) where ClassType : new()
        {
            m_Reader = new BinaryReader(stream);

            m_SerializationVersion = m_Reader.ReadByte();
            if (m_SerializationVersion != Serializer.SerializationVersion)
                throw new InvalidDataException($"Data stream is using an incompatible serialization format.  Stream is version {m_SerializationVersion} but code requires version {Serializer.SerializationVersion}.  The stream should be re-created with the current code");

            m_ObjectVersion = m_Reader.ReadInt32();

            ReadStringTables();

            ClearPerObjectCachedData();

            ClassType instance = (ClassType)ReadObject(0);

            if (m_Reader.BaseStream.Position != m_TypeStringTableBytePos)
                throw new InvalidDataException($"Did not read entire stream in DeSerialize.  Read to +{m_Reader.BaseStream.Position} but expected +{m_TypeStringTableBytePos}");

            // NOTE: The reader is deliberately not disposed here as doing so would also close the stream but we rely on the outer code to manage the lifetime of the stream

            return instance;
        }

        // Call to start readingdirectly from a stream, used primarily for testing USerialize functions in isolation
        internal void StartReadingFromStream(Stream stream)
        {
            m_Reader = new BinaryReader(stream);
        }

        // Return the object version from a serialized stream without doing anything else, this is the object version passed in the original Serializer.Serialize() call that created the stream.
        // Resets the stream read position back to where it was on entry before returning
        internal int GetObjectVersion(Stream stream)
        {
            long startPos = m_Reader.BaseStream.Position;

            m_Reader.ReadByte();        // Serialization version
            int objectVersion = m_Reader.ReadInt32();

            m_Reader.BaseStream.Position = startPos;

            return objectVersion;
        }

        // Add a custom deserializer function to handle a specific type
        internal void AddCustomSerializer(ICustomSerializer customSerializer)
        {
            m_CustomSerializers.Add(customSerializer.GetType(), customSerializer);
        }

        // Add an object factory function that can create instances of a named type
        internal void AddObjectFactory(Type type, ObjectFactory objectFactory)
        {
            m_ObjectFactories.Add(type, objectFactory);
        }

        // Get the type data for the specified type.  If it exists in the cache it is returned directly, otherwise the type data is gathered and stored in the cache before being returned
        TypeData GetTypeData(string assemblyQualifiedTypeName)
        {
            if (!m_TypeDataCache.TryGetValue(assemblyQualifiedTypeName, out TypeData typeData))
            {
                typeData = new TypeData();
                typeData.m_Fields = new Dictionary<string, FieldData>();
                typeData.m_Type = Type.GetType(assemblyQualifiedTypeName);
                if (typeData.m_Type == null)
                    throw new InvalidDataException($"Could not create type for '{assemblyQualifiedTypeName}'");

                m_ObjectFactories.TryGetValue(typeData.m_Type, out typeData.m_ObjectFactory);

                FieldInfo[] fieldArray = typeData.m_Type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                for (int fieldNum = 0; fieldNum < fieldArray.Length; fieldNum++)
                {
                    FieldData fieldData = new FieldData();
                    fieldData.m_FieldInfo = fieldArray[fieldNum];

                    if (fieldData.m_FieldInfo.FieldType == typeof(int))
                        fieldData.m_Setter = CreateSetter<int>(typeData.m_Type, fieldData.m_FieldInfo);
                    else if (fieldData.m_FieldInfo.FieldType == typeof(uint))
                        fieldData.m_Setter = CreateSetter<uint>(typeData.m_Type, fieldData.m_FieldInfo);
                    else if (fieldData.m_FieldInfo.FieldType == typeof(long))
                        fieldData.m_Setter = CreateSetter<long>(typeData.m_Type, fieldData.m_FieldInfo);
                    else if (fieldData.m_FieldInfo.FieldType == typeof(ulong))
                        fieldData.m_Setter = CreateSetter<ulong>(typeData.m_Type, fieldData.m_FieldInfo);
                    else if (fieldData.m_FieldInfo.FieldType == typeof(byte))
                        fieldData.m_Setter = CreateSetter<byte>(typeData.m_Type, fieldData.m_FieldInfo);
                    else if (fieldData.m_FieldInfo.FieldType == typeof(bool))
                        fieldData.m_Setter = CreateSetter<bool>(typeData.m_Type, fieldData.m_FieldInfo);
                    else if (fieldData.m_FieldInfo.FieldType == typeof(string))
                        fieldData.m_Setter = CreateSetter<string>(typeData.m_Type, fieldData.m_FieldInfo);
                    else if (fieldData.m_FieldInfo.FieldType == typeof(GUID))
                        fieldData.m_Setter = CreateSetter<GUID>(typeData.m_Type, fieldData.m_FieldInfo);
                    else if (fieldData.m_FieldInfo.FieldType == typeof(Hash128))
                        fieldData.m_Setter = CreateSetter<Hash128>(typeData.m_Type, fieldData.m_FieldInfo);
                    else if (fieldData.m_FieldInfo.FieldType == typeof(Type))
                        fieldData.m_Setter = CreateSetter<Type>(typeData.m_Type, fieldData.m_FieldInfo);
                    else if (fieldData.m_FieldInfo.FieldType == typeof(byte[]))
                    {
                        fieldData.m_Setter = CreateSetter<byte[]>(typeData.m_Type, fieldData.m_FieldInfo);
                        fieldData.m_Getter = CreateObjectGetter(typeData.m_Type, fieldData.m_FieldInfo);
                    }
                    // Per customer request
                    else if (fieldData.m_FieldInfo.FieldType == typeof(ulong[]))
                    {
                        fieldData.m_Setter = CreateSetter<ulong[]>(typeData.m_Type, fieldData.m_FieldInfo);
                        fieldData.m_Getter = CreateObjectGetter(typeData.m_Type, fieldData.m_FieldInfo);
                    }
                    else if (fieldData.m_FieldInfo.FieldType == typeof(string[]))
                    {
                        fieldData.m_Setter = CreateSetter<string[]>(typeData.m_Type, fieldData.m_FieldInfo);
                        fieldData.m_Getter = CreateObjectGetter(typeData.m_Type, fieldData.m_FieldInfo);
                    }
                    else if (fieldData.m_FieldInfo.FieldType == typeof(Type[]))
                    {
                        fieldData.m_Setter = CreateSetter<Type[]>(typeData.m_Type, fieldData.m_FieldInfo);
                        fieldData.m_Getter = CreateObjectGetter(typeData.m_Type, fieldData.m_FieldInfo);
                    }
                    else if (fieldData.m_FieldInfo.FieldType.IsEnum)
                        fieldData.m_Setter = CreateObjectSetter<int>(typeData.m_Type, fieldData.m_FieldInfo);
                    else if (fieldData.m_FieldInfo.FieldType.IsValueType && (!fieldData.m_FieldInfo.FieldType.IsPrimitive))
                        fieldData.m_Setter = CreateObjectSetter<object>(typeData.m_Type, fieldData.m_FieldInfo);
                    else if (typeof(Array).IsAssignableFrom(fieldData.m_FieldInfo.FieldType))
                    {
                        fieldData.m_Setter = CreateObjectSetter<object>(typeData.m_Type, fieldData.m_FieldInfo);
                        fieldData.m_Getter = CreateObjectGetter(typeData.m_Type, fieldData.m_FieldInfo);
                    }

                    typeData.m_Fields.Add(fieldData.m_FieldInfo.Name, fieldData);
                }
                m_TypeDataCache.Add(assemblyQualifiedTypeName, typeData);
            }
            return typeData;
        }

        // Create a function object to set the value of a field of type 'SetterType'.  Calling this compiled function object is much faster than using the reflection API
        static Func<object, SetterType, SetterType> CreateSetter<SetterType>(Type type, FieldInfo field)
        {
            ParameterExpression valueExp = Expression.Parameter(field.FieldType, "value");
            ParameterExpression targetExp = Expression.Parameter(typeof(object), "target");
            MemberExpression fieldExp = (type.IsValueType && (!type.IsPrimitive)) ? Expression.Field(Expression.Unbox(targetExp, type), field) : Expression.Field(Expression.Convert(targetExp, type), field);
            return Expression.Lambda<Func<object, SetterType, SetterType>>(Expression.Assign(fieldExp, valueExp), targetExp, valueExp).Compile();
        }

        // Create a function object to set the value of a field of generic object type that is stored in the stream with type 'StorageType'.  Calling this compiled function object is much faster than using the reflection API
        static Func<object, StorageType, StorageType> CreateObjectSetter<StorageType>(Type type, FieldInfo field)
        {
            ParameterExpression valueExp = Expression.Parameter(typeof(StorageType), "value");
            ParameterExpression targetExp = Expression.Parameter(typeof(object), "target");
            MemberExpression fieldExp = (type.IsValueType && (!type.IsPrimitive)) ? Expression.Field(Expression.Unbox(targetExp, type), field) : Expression.Field(Expression.Convert(targetExp, type), field);
            BinaryExpression assignExp = Expression.Assign(fieldExp, Expression.Convert(valueExp, field.FieldType));
            return Expression.Lambda<Func<object, StorageType, StorageType>>(Expression.Convert(assignExp, typeof(StorageType)), targetExp, valueExp).Compile();
        }

        // Create a function object to get the value from a field as a generic object.  It is much faster to call this compiled function object than to use the reflection API
        static Func<object, object> CreateObjectGetter(Type type, FieldInfo field)
        {
            ParameterExpression valueExp = Expression.Parameter(typeof(object), "value");
            return Expression.Lambda<Func<object, object>>(Expression.Convert(Expression.Field(Expression.Convert(valueExp, type), field), typeof(object)), valueExp).Compile();
        }

        // Return the TypeData for a type from the index of it's type name in the type/field stringtable
        TypeData GetTypeDataFromTypeNameIndex(int typeNameIndex)
        {
            // Populate the m_TypeDataByTypeNameIndex LUT with type data based on this index if not already set so we can get the type info from the index faster in the future
            if (m_TypeDataByTypeNameIndex[typeNameIndex] == null)
            {
                m_TypeDataByTypeNameIndex[typeNameIndex] = GetTypeData(m_TypeStringTable[typeNameIndex]);
                m_TypeByTypeNameIndex[typeNameIndex] = m_TypeDataByTypeNameIndex[typeNameIndex].m_Type;
            }
            return m_TypeDataByTypeNameIndex[typeNameIndex];
        }

        // Read an object from the stream
        object ReadObject(int depth)
        {
            if (!ReadNullFlag())
                return null;

            // Get the TypeData for the type of this object
            TypeData typeData = GetTypeDataFromTypeNameIndex(ReadStringIndex());

            // Give custom object factories a chance to create the instance first as this is faster than the generic Activator call.  
            // If no instance is created (either because no factory is registered for this type or the factory didn't produce an instance) then we call Activator as a fallback
            object objectRead = typeData.m_ObjectFactory?.Invoke();
            if (objectRead == null)
                objectRead = Activator.CreateInstance(typeData.m_Type);

            if (EmitVerboseLog)
                Debug.Log($"{new String(' ', (depth * 2) - ((depth > 0) ? 1 : 0))}ReadObject({typeData.m_Type.Name}) +{m_Reader.BaseStream.Position}");

            Dictionary<string, FieldData> fields = typeData.m_Fields;

            int numFields = m_Reader.ReadUInt16();

            // Initialise the index of field number -> field data for this type if it's the first time we've seen it
            if (typeData.m_FieldIndex == null)
                typeData.m_FieldIndex = new FieldData[numFields];

            for (int fieldNum = 0; fieldNum < numFields; fieldNum++)
            {
                string fieldName = m_TypeStringTable[ReadStringIndex()];

                FieldData field;
                if (typeData.m_FieldIndex[fieldNum] != null)
                    field = typeData.m_FieldIndex[fieldNum];
                else
                {
                    if (fields.TryGetValue(fieldName, out field))
                        typeData.m_FieldIndex[fieldNum] = field;
                }

                if (field != null)
                {
                    DataType fieldDataType = (DataType)m_Reader.ReadByte();

                    if (EmitVerboseLog)
                        Debug.Log($"{new String(' ', depth * 2)}Field {fieldName} -> {field?.m_FieldInfo.Name} ({fieldDataType}) +{m_Reader.BaseStream.Position}");

                    FieldInfo fieldInfo = field.m_FieldInfo;

                    switch (fieldDataType)
                    {
                    case DataType.Byte:
                        ((Func<object, byte, byte>)field.m_Setter)(objectRead, m_Reader.ReadByte());
                        break;

                    case DataType.Bool:
                        ((Func<object, bool, bool>)field.m_Setter)(objectRead, m_Reader.ReadBoolean());
                        break;

                    case DataType.Int:
                        ((Func<object, int, int>)field.m_Setter)(objectRead, m_Reader.ReadInt32());
                        break;

                    case DataType.UInt:
                        ((Func<object, uint, uint>)field.m_Setter)(objectRead, m_Reader.ReadUInt32());
                        break;

                    case DataType.Long:
                        ((Func<object, long, long>)field.m_Setter)(objectRead, m_Reader.ReadInt64());
                        break;

                    case DataType.ULong:
                        ((Func<object, ulong, ulong>)field.m_Setter)(objectRead, m_Reader.ReadUInt64());
                        break;

                    case DataType.Enum:
                        ((Func<object, int, int>)field.m_Setter)(objectRead, m_Reader.ReadInt32());
                        break;

                    case DataType.String:
                        ((Func<object, string, string>)field.m_Setter)(objectRead, ReadString());
                        break;

                    case DataType.Type:
                        ((Func<object, Type, Type>)field.m_Setter)(objectRead, GetTypeFromCache(ReadStringIndex()));
                        break;

                    case DataType.Class:
                        fieldInfo.SetValue(objectRead, ReadObject(depth + 1));
                        break;

                    case DataType.Struct:
                    {
                        object structObject = ReadObject(depth + 1);
                        ((Func<object, object, object>)field.m_Setter)(objectRead, structObject);
                        break;
                    }

                    case DataType.Array:
                        if (ReadNullFlag())
                        {
                            Array fieldArray;
                            if (field.m_Getter != null)
                                fieldArray = (Array)((Func<object, object>)field.m_Getter)(objectRead);
                            else
                                fieldArray = (Array)fieldInfo.GetValue(objectRead);

                            int rank = m_Reader.ReadInt32();
                            if (rank != 1)
                                throw new InvalidDataException($"USerialize currently doesn't support arrays with ranks other than one - data for field {fieldInfo.Name} of type {fieldInfo.FieldType.Name} has rank {rank}");
                            int length = m_Reader.ReadInt32();

                            DataType elementDataType = (DataType)m_Reader.ReadByte();

                            if (EmitVerboseLog)
                                Debug.Log($"{new String(' ', (depth + 1) * 2)}Array {elementDataType} [{length}] +{m_Reader.BaseStream.Position}");

                            switch (elementDataType)
                            {
                            case DataType.Byte:
                            {
                                byte[] byteArray = (byte[])Array.CreateInstance(typeof(byte), length);
                                ((Func<object, byte[], byte[]>)field.m_Setter)(objectRead, byteArray);
                                m_Reader.Read(byteArray, 0, length);
                                break;
                            }

                            // Per customer request
                            case DataType.ULong:
                            {
                                ulong[] ulongArray = (ulong[])Array.CreateInstance(typeof(ulong), length);
                                ((Func<object, ulong[], ulong[]>)field.m_Setter)(objectRead, ulongArray);
                                for (int elementNum = 0; elementNum < length; elementNum++)
                                {
                                    ulongArray[elementNum] = m_Reader.ReadUInt64();
                                }
                                break;
                            }

                            case DataType.String:
                            {
                                string[] stringArray = (string[])Array.CreateInstance(typeof(string), length);
                                ((Func<object, string[], string[]>)field.m_Setter)(objectRead, stringArray);
                                for (int elementNum = 0; elementNum < length; elementNum++)
                                {
                                    stringArray[elementNum] = ReadString();
                                }
                                break;
                            }

                            case DataType.Type:
                            {
                                Type[] typeArray = new Type[length];
                                ((Func<object, Type[], Type[]>)field.m_Setter)(objectRead, typeArray);
                                for (int elementNum = 0; elementNum < length; elementNum++)
                                {
                                    int elementTypeNameIndex = ReadStringIndex();
                                    if (elementTypeNameIndex != USerialize.InvalidStringIndex)
                                    {
                                        typeArray[elementNum] = GetTypeFromCache(elementTypeNameIndex);
                                        if (typeArray[elementNum] == null)
                                            throw new InvalidDataException($"Could not create Type for '{m_TypeStringTable[elementTypeNameIndex]}'");
                                    }

                                    if (EmitVerboseLog)
                                        Debug.Log($"{new String(' ', (depth + 2) * 2)}Type[{elementNum}] = '{m_TypeStringTable[elementTypeNameIndex]}' +{m_Reader.BaseStream.Position}");
                                }
                                break;
                            }

                            case DataType.Class:
                            {
                                Type arrayElementType = GetTypeFromCache(ReadStringIndex());
                                Array classArray = Array.CreateInstance(arrayElementType, length);
                                fieldInfo.SetValue(objectRead, classArray);
                                for (int elementNum = 0; elementNum < length; elementNum++)
                                {
                                    DataType objectDataType = (DataType)m_Reader.ReadByte();
                                    object elementObject = null;
                                    switch (objectDataType)
                                    {
                                    case DataType.Class:
                                        elementObject = ReadObject(depth + 2);
                                        break;

                                    case DataType.String:
                                        elementObject = ReadString();
                                        break;

                                    case DataType.Int:
                                        elementObject = m_Reader.ReadInt32();
                                        break;

                                    case DataType.Custom:
                                        elementObject = ReadCustomObject();
                                        break;

                                    default:
                                        throw new InvalidDataException($"Found unsupported data type '{objectDataType}' in class array '{typeData.m_Type.Name}.{fieldName}'");
                                    }
                                    classArray.SetValue(elementObject, elementNum);
                                }
                                break;
                            }

                            case DataType.Struct:
                            {
                                Type elementType = GetTypeFromCache(ReadStringIndex());
                                Array array = Array.CreateInstance(elementType, length);
                                fieldInfo.SetValue(objectRead, array);
                                for (int elementNum = 0; elementNum < length; elementNum++)
                                {
                                    array.SetValue(ReadObject(depth + 2), elementNum);
                                }
                                break;
                            }

                            default:
                                throw new InvalidDataException($"Unknown array element type {elementDataType} for field {fieldInfo.FieldType.Name}.{fieldInfo.Name}");
                            }
                        }
                        break;

                    case DataType.List:
                        if (ReadNullFlag())
                        {
                            int count = m_Reader.ReadInt32();
                            Type listType = GetTypeFromCache(ReadStringIndex());

                            if (EmitVerboseLog)
                                Debug.Log($"{new String(' ', (depth + 1) * 2)}List {listType.Name} [{count}] +{m_Reader.BaseStream.Position}");

                            System.Collections.IList list = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(listType), count);
                            for (int itemIndex = 0; itemIndex < count; itemIndex++)
                            {
                                object item = ReadObject(depth + 2);
                                if (item != null)
                                    list.Add(item);
                            }
                            fieldInfo.SetValue(objectRead, list);
                        }
                        break;

                    case DataType.Custom:
                        fieldInfo.SetValue(objectRead, ReadCustomObject());
                        break;

                    case DataType.Guid:
                    {
                        GUID guid;
                        unsafe
                        {
                            UInt64* guidPtr = (UInt64*)&guid;
                            guidPtr[0] = m_Reader.ReadUInt64();
                            guidPtr[1] = m_Reader.ReadUInt64();
                        }
                        ((Func<object, GUID, GUID>)field.m_Setter)(objectRead, guid);
                        break;
                    }

                    case DataType.Hash128:
                    {
                        Hash128 hash;
                        unsafe
                        {
                            UInt64* hashPtr = (UInt64*)&hash;
                            hashPtr[0] = m_Reader.ReadUInt64();
                            hashPtr[1] = m_Reader.ReadUInt64();
                        }
                        ((Func<object, Hash128, Hash128>)field.m_Setter)(objectRead, hash);
                        break;
                    }

                    default:
                        throw new InvalidDataException($"USerialize found unknown field data type '{fieldDataType}' on field '{typeData.m_Type.Name}.{fieldName}' in stream at +{m_Reader.BaseStream.Position}");
                    }
                }
                else
                {
                    // Didn't find a matching field in the object
                    throw new InvalidDataException($"USerialize found unknown field '{fieldName}' for type '{typeData.m_Type.Name}' in stream at +{m_Reader.BaseStream.Position}");
                }
            }

            return objectRead;
        }

        // Read and deserialize data for an object that was serialized with a custom serializer
        object ReadCustomObject()
        {
            Type objectType = GetTypeFromCache(ReadStringIndex());
            if (m_CustomSerializers.TryGetValue(objectType, out ICustomSerializer customSerializer))
            {
                return customSerializer.UDeSerializer(this);
            }
            else
                throw new InvalidDataException($"Could not find custom deserializer for type {objectType.Name}, custom deserializers can be added prior to deserialization with AddCustomDeserializer()");
        }

        // Read a byte from the stream and return true if it has value NotNull (1)
        internal bool ReadNullFlag()
        {
            return (m_Reader.ReadByte() == Serializer.NotNull);
        }

        // Read a byte array from the stream.  Will return null if the array was null when serialized
        internal byte[] ReadBytes()
        {
            byte[] bytes = null;
            if (ReadNullFlag())
            {
                bytes = new byte[m_Reader.ReadInt32()];
                m_Reader.Read(bytes, 0, bytes.Length);
            }
            return bytes;
        }

        // Read a potentially null value string from the stream
        internal string ReadString()
        {
            if (ReadNullFlag())
                return m_DataStringTable[ReadStringIndex()];
            return null;
        }

        // Return a Type instance for a type from the type/field stringtable index of it's name.  Uses a cache of previously seen types to improve performance
        Type GetTypeFromCache(int typeNameIndex)
        {
            if (typeNameIndex >= 0)
            {
                if (m_TypeByTypeNameIndex[typeNameIndex] == null)
                    m_TypeByTypeNameIndex[typeNameIndex] = Type.GetType(m_TypeStringTable[typeNameIndex]);

                return m_TypeByTypeNameIndex[typeNameIndex];
            }
            return null;
        }

        // Read a string table index.  There are almost never more than 32,767 strings so we use 15 bits by default for compactness.
        // If a string has an index more than 32,767 (i.e. 0x8000+) we store 0x8000 as a flag to signify this combined with the bottom 15 bits of the index.  Bits 15 to 30 are stored in the following 16 bits of data.
        internal int ReadStringIndex()
        {
            int stringIndex = m_Reader.ReadUInt16();
            if ((stringIndex & 0x8000) != 0)
            {
                stringIndex = (stringIndex & 0x7FFF) | (((int)m_Reader.ReadUInt16()) << 15);
            }
            return stringIndex;
        }
    }
}
