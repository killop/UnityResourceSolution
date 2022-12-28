using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;

using UnityEngine;

namespace UnityEditor.Build.Pipeline.Utilities.USerialize
{
    /*
     * Main USerialize serialzation class.  Used to write instances of types to a stream
     * 
     * To Serialize an object to a stream use code such as
     * 
     *   MemoryStream stream = new MemoryStream();
     *   USerialize.Serializer serializer = new USerialize.Serializer();
     *   serializer.Serialize(stream, myClassInstance, 1);   // '1' is the version of our object, we can read this value back at deserialization time if we want to determine the version of our data
     */
    internal class Serializer
    {
        // Data for a single field of a single type we have serialized.  We cache information obtained from the reflection API here as the reflection API can be slow to query
        internal class FieldData
        {
            internal FieldInfo m_FieldInfo;
            internal string m_Name;
            internal int m_NameIndex;
            internal Type m_ElementType;
            internal bool m_ElementTypeIsPrimitive;
            internal bool m_ElementTypeIsClass;
            internal bool m_ElementTypeIsValueType;
            internal DataType m_DataType = DataType.Invalid;
            internal object m_Getter;
        }

        // Data for a type we have serialized.  We cache information obtained from the reflection API here as the reflection API can be slow to query
        internal class TypeData
        {
            internal FieldData[] m_Fields;
            internal string m_AssemblyQualifiedName;
            internal int m_AssemblyQualifiedNameIndex;
        }

        // Simple string table, our serialized data contains two of these, one for the names of the types and fields serialized and one for the values of any string fields or arrays/lists using strings
        internal class StringTable
        {
            List<string> m_Strings = new List<string>();
            Dictionary<string, int> m_Index = new Dictionary<string, int>();

            /// <summary>
            /// Clear the data in this stringtable to make it empty
            /// </summary>
            internal void Clear()
            {
                m_Index.Clear();
                m_Strings.Clear();
            }

            /// <summary>
            /// Write the strings from this stringtable to a binary writer one after another
            /// </summary>
            /// <param name="writer">Writer to write the strings to</param>
            /// <returns>the byte position in the stream being written to where the strings start, this is the current position in the stream when the function was called</returns>
            internal long Write(BinaryWriter writer)
            {
                long stringTableBytePosition = writer.Seek(0, SeekOrigin.Current);

                writer.Write(m_Strings.Count);
                m_Strings.ForEach((item) => writer.Write(item));

                return stringTableBytePosition;
            }

            /// <summary>
            /// Return the index of a string in the stringtable if it exists, if it does not exist add it then return it's index
            /// </summary>
            /// <param name="stringToAddOrFind"></param>
            /// <returns></returns>
            internal int GetStringIndex(string stringToAddOrFind)
            {
                if (!m_Index.TryGetValue(stringToAddOrFind, out int stringIndex))
                {
                    stringIndex = m_Strings.Count;
                    m_Strings.Add(stringToAddOrFind);
                    m_Index.Add(stringToAddOrFind, stringIndex);
                }
                return stringIndex;
            }
        }

        // Byte values we store in the stream to signify whether a reference type is null (and thus absent from the stream) or not (and thus it's data comes next)
        internal const byte IsNull = 0;
        internal const byte NotNull = 1;

        // Custom serializers can be provided by the client code to implement serialization for types that cannot be adequately handled by the generic reflection based code
        // Client code can pass an array of custom serializers and their associated types to the Serializer constructor or can call AddCustomSerializer() to add individual custom serializers at any time prior to serialization taking place
        Dictionary<Type, ICustomSerializer> m_CustomSerializers = new Dictionary<Type, ICustomSerializer>();

        // Cache of TypeData instances for all the types we've been asked to serialize so far.  Only type specific data is stored here not instance specific data and this cache *is not* cleared between calls to Serialize() so using the same 
        // Serializer instance to write multiple instances of the same types achieves a significant performance benefit by being able to re-use type information without having to call slow reflection APIs again.
        Dictionary<Type, TypeData> m_TypeDataCache = new Dictionary<Type, TypeData>();

        // Accessing Type.AssemblyQualifiedName can be slow so we keep this cache mapping types to the string table indices in the type/field string table of the assembly qualified name of types being serialized.
        // Each time Serialize() is called new stringtables are emitted so this is cleared before each call but still provides a measurable speed increase vs. accessing Type.AssemblyQualifiedName each time it's needed
        Dictionary<Type, int> m_TypeQualifiedNameIndices = new Dictionary<Type, int>();

        // String table of strings from field values encountered
        StringTable m_DataStringTable = new StringTable();

        // String table of type and field names encountered
        StringTable m_TypeStringTable = new StringTable();

        // Writer we are writing out serialized data to
        BinaryWriter m_Writer;

        // Serialization data format version number.  Written to the stream to provide a means for upgrade should it be necessary in the future
        internal const byte SerializationVersion = 1;

        internal Serializer()
        {
        }

        internal Serializer(params ICustomSerializer[] customSerializers)
        {
            if (customSerializers != null)
                Array.ForEach(customSerializers, (customSerializer) => AddCustomSerializer(customSerializer));
        }

        internal void AddCustomSerializer(ICustomSerializer customSerializer)
        {
            m_CustomSerializers.Add(customSerializer.GetType(), customSerializer);
        }

        /// <summary>
        /// Clear data that we cache about types and object contents that can change between objects.
        /// </summary>
        void ClearPerObjectCachedData()
        {
            // Reset the type/field name and data string tables to empty for each object.
            m_DataStringTable.Clear();
            m_TypeStringTable.Clear();

            // Clear the type and field name indices from the cached type data as each object's type string table is distinct so the indices from any previously written objects will be invalid
            foreach (KeyValuePair<Type, TypeData> typeDataCacheEntry in m_TypeDataCache)
            {
                typeDataCacheEntry.Value.m_AssemblyQualifiedNameIndex = -1;

                foreach (FieldData fieldData in typeDataCacheEntry.Value.m_Fields)
                {
                    fieldData.m_NameIndex = -1;
                }
            }

            // Clear the assembly qualified type name cache as the indices of the assembly qualified type names in the type stringtable will likely be different for this object than the previous one serialized
            m_TypeQualifiedNameIndices.Clear();
        }


        // Main serialization function.  Serializes 'objectToSerialize' to the given stream inserting the supplied object version number in the data.  The object version number can be obtained by DeSerializer.ObjectVersion when deserializing the data
        internal void Serialize(Stream stream, object objectToSerialize, int objectVersion)
        {
            ClearPerObjectCachedData();

            m_Writer = new BinaryWriter(stream);

            // Version number for the serialization file format itself
            m_Writer.Write(SerializationVersion);

            // Client code version number for their own use
            m_Writer.Write(objectVersion);

            // Leave space for the offsets to the type and data stringtables data that we write after the serialization data proper
            long stringTableOffsetPosition = m_Writer.Seek(0, SeekOrigin.Current);
            m_Writer.Write(0uL);    // Space for the offset to the type string table data
            m_Writer.Write(0uL);    // Space for the offset to the data string table data

            // Write serialization data for the object
            WriteObject(objectToSerialize);

            // Write the type and data stringtables then fill in their position offsets at the start of the data we left space for earlier
            long typeStringTableBytePos = m_TypeStringTable.Write(m_Writer);
            long dataStringTableBytePos = m_DataStringTable.Write(m_Writer);

            m_Writer.Seek((int)stringTableOffsetPosition, SeekOrigin.Begin);
            m_Writer.Write(typeStringTableBytePos);
            m_Writer.Write(dataStringTableBytePos);

            m_Writer.Flush();
        }

        // Call to start writing directly to a stream, used primarily for testing USerialize functions in isolation
        internal void StartWritingToStream(Stream stream)
        {
            m_Writer = new BinaryWriter(stream);
        }

        // Call when we've finished writing to a stream, used primarily for testing USerialize functions in isolation
        internal void FinishWritingToStream()
        {
            m_Writer.Flush();
        }

        // Return the cached type data for a given type.  Will return it from the m_TypeDataCache cache if present otherwise will generate a new TypeData instance from the type and add it to the cache
        TypeData GetTypeData(Type type)
        {
            if (!m_TypeDataCache.TryGetValue(type, out TypeData typeData))
            {
                // Cache data about the fields that is slow to retrieve every time an instance of this type is processed

                FieldInfo[] fieldInfos = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                typeData = new TypeData() { m_Fields = new FieldData[fieldInfos.Length] };
                typeData.m_AssemblyQualifiedName = type.AssemblyQualifiedName;
                typeData.m_AssemblyQualifiedNameIndex = m_TypeStringTable.GetStringIndex(typeData.m_AssemblyQualifiedName);

                for (int fieldNum = 0; fieldNum < fieldInfos.Length; fieldNum++)
                {
                    FieldInfo field = fieldInfos[fieldNum];
                    FieldData fieldData = new FieldData();
                    fieldData.m_FieldInfo = field;
                    fieldData.m_Name = field.Name;
                    fieldData.m_NameIndex = m_TypeStringTable.GetStringIndex(fieldData.m_Name);

                    if (typeof(Array).IsAssignableFrom(field.FieldType))
                    {
                        fieldData.m_DataType = DataType.Array;
                        fieldData.m_ElementType = field.FieldType.GetElementType();
                        fieldData.m_ElementTypeIsPrimitive = fieldData.m_ElementType.IsPrimitive;
                        fieldData.m_ElementTypeIsClass = fieldData.m_ElementType.IsClass;
                        fieldData.m_ElementTypeIsValueType = fieldData.m_ElementType.IsValueType;
                        fieldData.m_Getter = CreateObjectGetter(type, field);
                    }
                    else if (field.FieldType.IsGenericType && (field.FieldType.GetGenericTypeDefinition() == typeof(List<>)))
                    {
                        fieldData.m_DataType = DataType.List;
                        fieldData.m_ElementType = field.FieldType.GetGenericArguments()[0];
                    }
                    else if (field.FieldType == typeof(GUID))
                    {
                        fieldData.m_DataType = DataType.Guid;
                        fieldData.m_Getter = CreateGetter<GUID>(type, field);
                    }
                    else if (field.FieldType == typeof(Hash128))
                    {
                        fieldData.m_DataType = DataType.Hash128;
                        fieldData.m_Getter = CreateGetter<Hash128>(type, field);
                    }
                    else if (field.FieldType.IsEnum)
                    {
                        fieldData.m_DataType = DataType.Enum;
                        fieldData.m_Getter = CreateObjectGetter(type, field);
                    }
                    else if (field.FieldType == typeof(String))
                    {
                        fieldData.m_DataType = DataType.String;
                        fieldData.m_Getter = CreateGetter<string>(type, field);
                    }
                    else if (field.FieldType.IsClass)
                    {
                        fieldData.m_DataType = DataType.Class;
                        fieldData.m_Getter = CreateObjectGetter(type, field);
                    }
                    else if (field.FieldType.IsValueType && (!field.FieldType.IsPrimitive))
                    {
                        fieldData.m_DataType = DataType.Struct;
                        fieldData.m_Getter = CreateObjectGetter(type, field);
                    }
                    else if (field.FieldType == typeof(byte))
                    {
                        fieldData.m_DataType = DataType.Byte;
                        fieldData.m_Getter = CreateGetter<byte>(type, field);
                    }
                    else if (field.FieldType == typeof(bool))
                    {
                        fieldData.m_DataType = DataType.Bool;
                        fieldData.m_Getter = CreateGetter<bool>(type, field);
                    }
                    else if (field.FieldType == typeof(int))
                    {
                        fieldData.m_DataType = DataType.Int;
                        fieldData.m_Getter = CreateGetter<int>(type, field);
                    }
                    else if (field.FieldType == typeof(uint))
                    {
                        fieldData.m_DataType = DataType.UInt;
                        fieldData.m_Getter = CreateGetter<uint>(type, field);
                    }
                    else if (field.FieldType == typeof(long))
                    {
                        fieldData.m_DataType = DataType.Long;
                        fieldData.m_Getter = CreateGetter<long>(type, field);
                    }
                    else if (field.FieldType == typeof(ulong))
                    {
                        fieldData.m_DataType = DataType.ULong;
                        fieldData.m_Getter = CreateGetter<ulong>(type, field);
                    }
                    else if (field.FieldType == typeof(Type))
                    {
                        fieldData.m_DataType = DataType.Type;
                        fieldData.m_Getter = CreateGetter<Type>(type, field);
                    }

                    typeData.m_Fields[fieldNum] = fieldData;
                }
                m_TypeDataCache.Add(type, typeData);
            }
            else if (typeData.m_AssemblyQualifiedNameIndex == -1)
            {
                // This type is in our cache but it hasn't been used by the object being serialized yet.  Find/add it's type and field names to the type string table so we have a valid index
                typeData.m_AssemblyQualifiedNameIndex = m_TypeStringTable.GetStringIndex(typeData.m_AssemblyQualifiedName);
                foreach (FieldData fieldData in typeData.m_Fields)
                {
                    fieldData.m_NameIndex = m_TypeStringTable.GetStringIndex(fieldData.m_Name);
                }

            }
            return typeData;
        }

        // Create a function object to get the value from a field of type 'GetterType'.  It is much faster to call this compiled function object than to use the reflection API
        static Func<object, GetterType> CreateGetter<GetterType>(Type type, FieldInfo field)
        {
            ParameterExpression valueExp = Expression.Parameter(typeof(object), "value");
            return Expression.Lambda<Func<object, GetterType>>(Expression.Field(Expression.Convert(valueExp, type), field), valueExp).Compile();
        }

        // Create a function object to get the value from a field as a generic object.  It is much faster to call this compiled function object than to use the reflection API
        static Func<object, object> CreateObjectGetter(Type type, FieldInfo field)
        {
            ParameterExpression valueExp = Expression.Parameter(typeof(object), "value");
            return Expression.Lambda<Func<object, object>>(Expression.Convert(Expression.Field(Expression.Convert(valueExp, type), field), typeof(object)), valueExp).Compile();
        }

        // Write an object to the serialization stream
        void WriteObject(object objectToWrite)
        {
            if (!WriteNullFlag(objectToWrite))
                return;

            // Get information about the objects type then write the type/field stringtable index of it's type name and how many fields it has
            Type objectType = objectToWrite.GetType();
            TypeData typeData = GetTypeData(objectType);

            WriteStringIndex(typeData.m_AssemblyQualifiedNameIndex);

            if (typeData.m_Fields.Length > ushort.MaxValue)
                throw new InvalidDataException($"USerialize cannot serialize objects with more than {ushort.MaxValue} fields");
            m_Writer.Write((ushort)typeData.m_Fields.Length);

            // Process each field in turn
            foreach (FieldData field in typeData.m_Fields)
            {
                switch (field.m_DataType)
                {
                case DataType.Array:
                {
                    WriteFieldInfo(field, DataType.Array);
                    Array array = (Array)((Func<object, object>)field.m_Getter)(objectToWrite);
                    if (WriteNullFlag(array))
                    {
                        // We only support rank 1 for now
                        m_Writer.Write(array.Rank);
                        m_Writer.Write(array.Length);

                        if (array.Rank != 1)
                            throw new InvalidDataException($"USerialize currently doesn't support arrays with ranks other than one - field {field.m_Name} of type {field.m_FieldInfo.FieldType.Name} has rank {array.Rank}");

                        Type elementType = field.m_ElementType;
                        if (field.m_ElementTypeIsPrimitive)
                        {
                            // A primitive array, write the bytes as optimally as possible for types we support
                            if (elementType == typeof(byte))
                            {
                                // byte[]
                                m_Writer.Write((byte)DataType.Byte);
                                m_Writer.Write((byte[])array, 0, array.Length);
                            }
                            // Per customer request
                            else if (elementType == typeof(ulong))
                            {
                                ulong[] ulongArray = (ulong[])array;
                                m_Writer.Write((byte)DataType.ULong);
                                for (int elementIndex = 0; elementIndex < array.Length; elementIndex++)
                                { 
                                    m_Writer.Write(ulongArray[elementIndex]);
                                }
                            }
                            else
                                throw new InvalidDataException($"USerialize currently doesn't support primitive arrays of type {elementType.Name} - field {field.m_Name} of type {field.m_FieldInfo.FieldType.Name}");
                        }
                        else if (elementType == typeof(string))
                        {
                            // String[]
                            string[] stringArray = (string[])array;
                            m_Writer.Write((byte)DataType.String);
                            for (int elementIndex = 0; elementIndex < array.Length; elementIndex++)
                            {
                                WriteDataString(stringArray[elementIndex]);
                            }
                        }
                        else if (elementType == typeof(Type))
                        {
                            // Type[]
                            Type[] typeArray = (Type[])array;
                            m_Writer.Write((byte)DataType.Type);
                            for (int elementIndex = 0; elementIndex < array.Length; elementIndex++)
                            {
                                if (typeArray[elementIndex] != null)
                                    WriteStringIndex(GetTypeQualifiedNameIndex(typeArray[elementIndex]));
                                else
                                    WriteStringIndex(USerialize.InvalidStringIndex);

                            }
                        }
                        else if (field.m_ElementTypeIsClass)
                        {
                            // An array of class instances
                            m_Writer.Write((byte)DataType.Class);
                            WriteStringIndex(GetTypeQualifiedNameIndex(elementType));
                            for (int elementIndex = 0; elementIndex < array.Length; elementIndex++)
                            {
                                object elementToWrite = array.GetValue(elementIndex);

                                // If the element isn't null see if we have a custom serializer for the type of this instance.  
                                // The array type might be a base class for the actual instances which may not all be the same derived type so we use the runtime type of each instance individually to check for custom serializers rather than using the type of the array itself
                                if (elementToWrite != null)
                                {
                                    Type elementObjectType = elementToWrite.GetType();
                                    if (m_CustomSerializers.TryGetValue(elementObjectType, out ICustomSerializer customSerializer))
                                    {
                                        m_Writer.Write((byte)DataType.Custom);
                                        WriteStringIndex(GetTypeQualifiedNameIndex(elementObjectType));
                                        customSerializer.USerializer(this, elementToWrite);
                                    }
                                    else if (elementObjectType == typeof(string))
                                    {
                                        m_Writer.Write((byte)DataType.String);
                                        WriteDataString((string)elementToWrite);
                                    }
                                    else if (elementObjectType == typeof(Int32))
                                    {
                                        m_Writer.Write((byte)DataType.Int);
                                        m_Writer.Write((int)elementToWrite);
                                    }
                                    else
                                    {
                                        if (elementObjectType.IsPrimitive)
                                            throw new InvalidDataException($"USerialize cannot handle type '{elementObjectType.Name}' in object[] array '{objectType.Name}.{field.m_Name}'");
                                        m_Writer.Write((byte)DataType.Class);
                                        WriteObject(elementToWrite);
                                    }
                                }
                                else
                                {
                                    m_Writer.Write((byte)DataType.Class);
                                    m_Writer.Write(IsNull);
                                }
                            }
                        }
                        else if (field.m_ElementTypeIsValueType)
                        {
                            // An array of struct instances
                            m_Writer.Write((byte)DataType.Struct);
                            WriteStringIndex(GetTypeQualifiedNameIndex(elementType));
                            for (int elementIndex = 0; elementIndex < array.Length; elementIndex++)
                            {
                                WriteObject(array.GetValue(elementIndex));
                            }
                        }
                        else
                            throw new InvalidDataException($"USerialize doesn't support serializing array field {field.m_Name} of type {field.m_FieldInfo.FieldType.Name} which is of type {elementType.Name}");
                    }
                    break;
                }

                case DataType.List:
                {
                    // A List<>
                    WriteFieldInfo(field, DataType.List);
                    System.Collections.IList list = field.m_FieldInfo.GetValue(objectToWrite) as System.Collections.IList;
                    if (WriteNullFlag(list))
                    {
                        m_Writer.Write(list.Count);
                        WriteStringIndex(GetTypeQualifiedNameIndex(field.m_ElementType));
                        for (int elementIndex = 0; elementIndex < list.Count; elementIndex++)
                        {
                            WriteObject(list[elementIndex]);
                        }
                    }
                    break;
                }

                case DataType.Guid:
                {
                    // GUID instance
                    WriteFieldInfo(field, DataType.Guid);
                    GUID guid = ((Func<object, GUID>)field.m_Getter)(objectToWrite);
                    unsafe
                    {
                        UInt64* guidPtr = (UInt64*)&guid;
                        m_Writer.Write(guidPtr[0]);
                        m_Writer.Write(guidPtr[1]);
                    }
                    break;
                }

                case DataType.Hash128:
                {
                    // Hash128 instance
                    WriteFieldInfo(field, DataType.Hash128);
                    Hash128 hash = ((Func<object, Hash128>)field.m_Getter)(objectToWrite);
                    unsafe
                    {
                        UInt64* hashPtr = (UInt64*)&hash;
                        m_Writer.Write(hashPtr[0]);
                        m_Writer.Write(hashPtr[1]);
                    }
                    break;
                }

                case DataType.Enum:
                    // An enum, we write it's value as an Int32
                    WriteFieldInfo(field, DataType.Enum);
                    m_Writer.Write((int)((Func<object, object>)field.m_Getter)(objectToWrite));
                    break;

                case DataType.String:
                {
                    // String instance
                    WriteFieldInfo(field, DataType.String);
                    WriteDataString(((Func<object, string>)field.m_Getter)(objectToWrite));
                    break;
                }

                case DataType.Class:
                {
                    // Is a class instance. If the value isn't null check to see if we have been given a custom serializer for it's type.
                    // If the value is null or there is no custom serializer registered for the value's type write it as normal
                    // Note the type of the actual object is used to locate custom serializers rather than the type of the field in case the object is actually of a derived type
                    object fieldValue = ((Func<object, object>)field.m_Getter)(objectToWrite);
                    if ((fieldValue == null) || (!DoCustomSerialization(field, fieldValue)))
                    {
                        WriteFieldInfo(field, DataType.Class);
                        WriteObject(fieldValue);
                    }
                    break;
                }

                case DataType.Struct:
                {
                    // Is a struct instance. Check to see if we have been given a custom serializer for it's type, if not write it as normal
                    object fieldValue = ((Func<object, object>)field.m_Getter)(objectToWrite);
                    if (!DoCustomSerialization(field, fieldValue))
                    {
                        WriteFieldInfo(field, DataType.Struct);
                        WriteObject(fieldValue);
                    }
                    break;
                }

                case DataType.Byte:
                    WriteFieldInfo(field, DataType.Byte);
                    m_Writer.Write(((Func<object, byte>)field.m_Getter)(objectToWrite));
                    break;

                case DataType.Bool:
                    WriteFieldInfo(field, DataType.Bool);
                    m_Writer.Write(((Func<object, bool>)field.m_Getter)(objectToWrite));
                    break;

                case DataType.Int:
                    WriteFieldInfo(field, DataType.Int);
                    m_Writer.Write(((Func<object, int>)field.m_Getter)(objectToWrite));
                    break;

                case DataType.UInt:
                    WriteFieldInfo(field, DataType.UInt);
                    m_Writer.Write(((Func<object, uint>)field.m_Getter)(objectToWrite));
                    break;

                case DataType.Long:
                    WriteFieldInfo(field, DataType.Long);
                    m_Writer.Write(((Func<object, long>)field.m_Getter)(objectToWrite));
                    break;

                case DataType.ULong:
                    WriteFieldInfo(field, DataType.ULong);
                    m_Writer.Write(((Func<object, ulong>)field.m_Getter)(objectToWrite));
                    break;

                case DataType.Type:
                    WriteFieldInfo(field, DataType.Type);
                    WriteStringIndex(GetTypeQualifiedNameIndex(((Func<object, Type>)field.m_Getter)(objectToWrite)));
                    break;

                default:
                    throw new InvalidDataException($"USerialize doesn't know how to serialize field {objectType.Name}.{field.m_Name} of type {field.m_FieldInfo.FieldType.Name}");
                }
            }
        }

        // Return the index in the type/field stringtable of the AssemblyQualifiedName of the given type.  Accessing Type.AssemblyQualifiedName can be slow so we use a cache
        // to store the string table indices of types we've encountered before
        int GetTypeQualifiedNameIndex(Type type)
        {
            if (type == null)
                return -1;

            if (!m_TypeQualifiedNameIndices.TryGetValue(type, out int qualifiedNameIndex))
            {
                qualifiedNameIndex = m_TypeStringTable.GetStringIndex(type.AssemblyQualifiedName);
                m_TypeQualifiedNameIndices.Add(type, qualifiedNameIndex);
            }
            return qualifiedNameIndex;
        }

        // Check to see if a custom serializer has been registered for the type of a given object.  If so call it and return true, otherwise return false
        bool DoCustomSerialization(FieldData field, object valueToSerialize)
        {
            Type valueType = valueToSerialize.GetType();
            if (m_CustomSerializers.TryGetValue(valueType, out ICustomSerializer customSerializer))
            {
                WriteFieldInfo(field, DataType.Custom);
                WriteStringIndex(GetTypeQualifiedNameIndex(valueType));
                customSerializer.USerializer(this, valueToSerialize);
                return true;
            }
            return false;
        }

        // Write a byte array to the stream.  The actual bytes are preceded by a flag byte indicating whether the array passed in was null or not
        internal void WriteBytes(byte[] bytes)
        {
            if (WriteNullFlag(bytes))
            {
                m_Writer.Write(bytes.Length);
                m_Writer.Write(bytes, 0, bytes.Length);
            }
        }

        // If the supplied object *is not* null write a byte with value NotNull (1) to the stream and return true
        // if the supplied object *is* null write a byte with value IsNull (0) to the stream and return false
        internal bool WriteNullFlag(object value)
        {
            if (value != null)
            {
                m_Writer.Write(NotNull);
                return true;
            }

            m_Writer.Write(IsNull);
            return false;
        }

        // Write a string table index.  There are almost never more than 32,767 strings so we use 15 bits by default for compactness.
        // If a string has an index more than 32,767 (i.e. 0x8000+) we store 0x8000 as a flag to signify this combined with the bottom 15 bits of the index.  Bits 15 to 30 are stored in the following 16 bits of data.
        internal void WriteStringIndex(int stringIndex)
        {
            if (stringIndex < 0x8000)
                m_Writer.Write((ushort)stringIndex);
            else
            {
                m_Writer.Write((ushort)(0x8000 | (stringIndex & 0x7FFF)));
                m_Writer.Write((ushort)(stringIndex >> 15));
            }
        }

        // Write meta-data for a field to the stream.  The index of the field's name in the type/field stringtable is written followed by a byte indicating the type of the data in the stream
        void WriteFieldInfo(FieldData field, DataType dataType)
        {
            WriteStringIndex(field.m_NameIndex);
            m_Writer.Write((byte)dataType);
        }

        // Write a field value string to the stream.  A IsNull/NotNull byte is written then if the string is not null the index of the string in the data stringtable
        internal void WriteDataString(string stringToWrite)
        {
            if (WriteNullFlag(stringToWrite))
                WriteStringIndex(m_DataStringTable.GetStringIndex(stringToWrite));
        }
    }
}
