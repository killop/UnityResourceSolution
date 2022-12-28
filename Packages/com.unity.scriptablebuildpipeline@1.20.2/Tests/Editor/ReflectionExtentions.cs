using UnityEditor.Build.Content;

namespace UnityEditor.Build.Pipeline.Tests
{
    internal static class ReflectionExtentions
    {
        public static void SetFileName(this ref ResourceFile file, string filename)
        {
#if UNITY_2019_3_OR_NEWER
            file.fileName = filename;
#else
            var fieldInfo = typeof(ResourceFile).GetField("m_FileName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            object boxed = file;
            fieldInfo.SetValue(boxed, filename);
            file = (ResourceFile)boxed;
#endif
        }

        public static void SetFileAlias(this ref ResourceFile file, string fileAlias)
        {
#if UNITY_2019_3_OR_NEWER
            file.fileAlias = fileAlias;
#else
            var fieldInfo = typeof(ResourceFile).GetField("m_FileAlias", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            object boxed = file;
            fieldInfo.SetValue(boxed, fileAlias);
            file = (ResourceFile)boxed;
#endif
        }

        public static void SetSerializedFile(this ref ResourceFile file, bool serializedFile)
        {
#if UNITY_2019_3_OR_NEWER
            file.serializedFile = serializedFile;
#else
            var fieldInfo = typeof(ResourceFile).GetField("m_SerializedFile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            object boxed = file;
            fieldInfo.SetValue(boxed, serializedFile);
            file = (ResourceFile)boxed;
#endif
        }

        public static void SetHeader(this ref ObjectSerializedInfo osi, SerializedLocation serializedLocation)
        {
            var fieldInfo = typeof(ObjectSerializedInfo).GetField("m_Header", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            object boxed = osi;
            fieldInfo.SetValue(boxed, serializedLocation);
            osi = (ObjectSerializedInfo)boxed;
        }

        public static void SetFileName(this ref SerializedLocation serializedLocation, string filename)
        {
            var fieldInfo = typeof(SerializedLocation).GetField("m_FileName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            object boxed = serializedLocation;
            fieldInfo.SetValue(boxed, filename);
            serializedLocation = (SerializedLocation)boxed;
        }

        public static void SetOffset(this ref SerializedLocation serializedLocation, ulong offset)
        {
            var fieldInfo = typeof(SerializedLocation).GetField("m_Offset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            object boxed = serializedLocation;
            fieldInfo.SetValue(boxed, offset);
            serializedLocation = (SerializedLocation)boxed;
        }

        public static void SetObjectIdentifier(this ref ObjectIdentifier obj, GUID guid, long localIdentifierInFile, FileType fileType, string filePath)
        {
            SetGuid(ref obj, guid);
            SetLocalIdentifierInFile(ref obj, localIdentifierInFile);
            SetFileType(ref obj, fileType);
            SetFilePath(ref obj, filePath);
        }

        public static void SetGuid(this ref ObjectIdentifier obj, GUID guid)
        {
            var fieldInfo = typeof(ObjectIdentifier).GetField("m_GUID", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            object boxed = obj;
            fieldInfo.SetValue(boxed, guid);
            obj = (ObjectIdentifier)boxed;
        }

        public static void SetLocalIdentifierInFile(this ref ObjectIdentifier obj, long localIdentifierInFile)
        {
            var fieldInfo = typeof(ObjectIdentifier).GetField("m_LocalIdentifierInFile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            object boxed = obj;
            fieldInfo.SetValue(boxed, localIdentifierInFile);
            obj = (ObjectIdentifier)boxed;
        }

        public static void SetFileType(this ref ObjectIdentifier obj, FileType fileType)
        {
            var fieldInfo = typeof(ObjectIdentifier).GetField("m_FileType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            object boxed = obj;
            fieldInfo.SetValue(boxed, fileType);
            obj = (ObjectIdentifier)boxed;
        }

        public static void SetFilePath(this ref ObjectIdentifier obj, string filePath)
        {
            var fieldInfo = typeof(ObjectIdentifier).GetField("m_FilePath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            object boxed = obj;
            fieldInfo.SetValue(boxed, filePath);
            obj = (ObjectIdentifier)boxed;
        }
    }
}
