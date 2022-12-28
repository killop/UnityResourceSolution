#if UNITY_2022_2_OR_NEWER
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

namespace UnityEditor.Build.Pipeline.Tests.ContentLoad
{

    [Serializable()]
    public class Catalog
    {
        [Serializable()]
        public class ContentFileInfo
        {
            public string Filename;
            public List<string> Dependencies;
        }

        [Serializable()]
        public class AddressableLocation
        {
            public string AddressableName;
            public string Filename;
            public ulong LFID;
        }

        public List<ContentFileInfo> ContentFiles = new List<ContentFileInfo>();
        public List<AddressableLocation> Locations = new List<AddressableLocation>();

        private Dictionary<string, AddressableLocation> AddressToLocation =
            new Dictionary<string, AddressableLocation>();

        private Dictionary<string, ContentFileInfo> FileToInfo = new Dictionary<string, ContentFileInfo>();

        public Catalog()
        {
        }

        unsafe public static string ReadAllTextVFS(string path)
        {
            FileInfoResult infoResult;
            ReadHandle h = AsyncReadManager.GetFileInfo(path, &infoResult);
            h.JobHandle.Complete();
            var getInfoStatus = h.Status;
            h.Dispose();

            if (getInfoStatus != ReadStatus.Complete)
                throw new Exception($"Could not get file info for path {path}");

            FileHandle fH = AsyncReadManager.OpenFileAsync(path);
            ReadCommand cmd;
            cmd.Buffer = UnsafeUtility.Malloc(infoResult.FileSize, 0, Unity.Collections.Allocator.Temp);
            cmd.Offset = 0;
            cmd.Size = infoResult.FileSize;
            var readHandle = AsyncReadManager.Read(path, &cmd, 1);
            readHandle.JobHandle.Complete();
            AsyncReadManager.CloseCachedFileAsync(path).Complete();

            var readResult = readHandle.Status;
            readHandle.Dispose();

            if (readResult != ReadStatus.Complete)
            {
                UnsafeUtility.Free(cmd.Buffer, Unity.Collections.Allocator.Temp);
                throw new Exception($"Failed to read data from {path}");
            }

            // Convert to string
            string text = System.Text.Encoding.Default.GetString((byte*) cmd.Buffer, (int) cmd.Size);

            UnsafeUtility.Free(cmd.Buffer, Unity.Collections.Allocator.Temp);
            return text;
        }

        public static Catalog LoadFromFile(string path)
        {
            string jsonText = ReadAllTextVFS(path);
            Catalog catalog = JsonUtility.FromJson<Catalog>(jsonText);
            catalog.OnDeserialize();
            return catalog;
        }

        public AddressableLocation GetLocation(string name)
        {
            return AddressToLocation[name];
        }

        public ContentFileInfo GetFileInfo(string filename)
        {
            return FileToInfo[filename];
        }

        void BuildMaps()
        {
            AddressToLocation = new Dictionary<string, AddressableLocation>();
            FileToInfo = new Dictionary<string, ContentFileInfo>();
            foreach (ContentFileInfo f in ContentFiles)
                FileToInfo[f.Filename] = f;
            foreach (AddressableLocation l in Locations)
            {
                AddressToLocation[l.AddressableName] = l;
            }
        }

        [OnDeserializing()]
        public void OnDeserialize()
        {
            BuildMaps();
        }
    }
}
#endif