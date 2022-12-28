using System;
using System.Reflection;
using System.Runtime.InteropServices;

using UnityEngine;

namespace UnityEditor.Build.Pipeline.Utilities.USerialize
{
    // Data types that USerialize can emit to the stream
    internal enum DataType
    {
        Byte,
        Bool,
        Int,
        UInt,
        Long,
        ULong,
        String,
        Class,
        Struct,
        Enum,
        Array,
        List,
        Custom,
        Type,
        Guid,
        Hash128,
        Invalid
    }

    // Custom serializers can be provided by the client code to implement serialization for types that cannot be adequately handled by the generic reflection based code
    // A custom serializer is a class that implements this ICustomSerializer interface to provide functions to serialize data for a type to the stream and recreate an instance of the type from a serialized data stream
    // Client code can pass an array of custom serializers and their associated types to the Serializer/Deserializer constructor or can call AddCustomSerializer() to add individual custom serializers at any time prior to serialization taking place
    internal interface ICustomSerializer
    {
        // Return the type that this custom serializer deals with
        Type GetType();

        // Serializer function to convert an instance of the type into a serialized stream.
        void USerializer(Serializer serializer, object value);

        // Deserializer function to create an instance of the type from a previously serialized stream
        object UDeSerializer(DeSerializer deserializer);
    }

    internal static class USerialize
    {
        // Reserved value for string indices representing an invalid string index, maximum value that can be written by Serializer.WriteStringIndex() or read by DeSerializer.ReadStringIndex()
        internal const int InvalidStringIndex = int.MaxValue;
    }
}
