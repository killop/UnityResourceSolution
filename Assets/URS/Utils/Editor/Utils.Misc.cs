using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine.Pool;

namespace NinjaBeats
{

    public unsafe struct UnsafeFileBuffer : IDisposable
    {
        public byte* buffer;
        public long length;

        public IntPtr ptr => new(buffer);
        public int ptrCount => (int)length;

        public static readonly UnsafeFileBuffer Empty = new();

        public bool isValid => buffer != null && length > 0;
        public void Dispose()
        {
            if (buffer != null)
            {
                UnsafeUtility.Free(buffer, Allocator.Temp);
                buffer = null;
            }    
        }
    }

    
    public static partial class Utils
    {
        public static UnityEngine.Pool.ObjectPool<StringBuilder> sStringBuilderPool =
            new UnityEngine.Pool.ObjectPool<StringBuilder>(
                () => new StringBuilder(),
                null,
                x => x.Clear()
            );

        public static int Compare(string a, string b)
        {
            if (a == null)
            {
                if (b == null)
                    return 0;
                else
                    return -1;
            }

            if (b == null)
                return 1;

            for (int i = 0; i < a.Length && i < b.Length; ++i)
            {
                if (a[i] < b[i])
                    return -1;
                else if (a[i] > b[i])
                    return 1;
            }

            if (a.Length < b.Length)
                return -1;
            else if (a.Length > b.Length)
                return 1;
            return 0;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetOrAddComponent<T>(this Component component) where T : Component
        {
            if (component.TryGetComponent<T>(out var r))
                return r;
            return component.gameObject.AddComponent<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            if (gameObject.TryGetComponent<T>(out var r))
                return r;
            return gameObject.AddComponent<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnableKeyword(this Material self, string name, bool value)
        {
            if (value)
                self.EnableKeyword(name);
            else
                self.DisableKeyword(name);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetKeyword(this Material self, string name) => self.IsKeywordEnabled(name) ? 1 : 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetKeyword(this Material self, string name, int value) =>
            self.EnableKeyword(name, value != 0);

        public static bool IsChildOf(this Transform self, Transform parent)
        {
            if (self == null || parent == null)
                return false;

            if (self == parent)
                return true;

            return IsChildOf(self.parent, parent);
        }

        public static string FullPathInHierarchy(this Transform self)
        {
            using (sStringBuilderPool.Get(out var sb))
            {
                for (Transform it = self; it != null; it = it.parent)
                {
                    if (it == self)
                    {
                        sb.Append(it.name);
                    }
                    else
                    {
                        sb.Insert(0, '/');
                        sb.Insert(0, it.name);
                    }
                }

                return sb.ToString();
            }
        }

        public static GameObject[] GetDontDestroyOnLoadObjects()
        {
            if (!Application.isPlaying)
                return new GameObject[0];
            GameObject temp = null;
            try
            {
                temp = new GameObject();
                UnityEngine.Object.DontDestroyOnLoad(temp);
                UnityEngine.SceneManagement.Scene dontDestroyOnLoad = temp.scene;
                UnityEngine.Object.DestroyImmediate(temp);
                temp = null;

                if (dontDestroyOnLoad != null)
                    return dontDestroyOnLoad.GetRootGameObjects();
                else
                    return new GameObject[0];
            }
            finally
            {
                if (temp != null)
                    UnityEngine.Object.DestroyImmediate(temp);
            }
        }

        private static int s_MaxFileMB = 10;

        public static bool FileExists(string path) => GetFileInfo(path, out _);
        public unsafe static bool GetFileInfo(string path, out long fileSize)
        {
            fileSize = 0;
            if (string.IsNullOrEmpty(path))
                return false;
            
            FileInfoResult info = new();
            var readHandle = AsyncReadManager.GetFileInfo(path, &info);
            readHandle.JobHandle.Complete();
            readHandle.Dispose();

            if (info.FileState == FileState.Exists)
            {
                fileSize = info.FileSize;
                return true;
            }
            return false;
        }

        public unsafe static UnsafeFileBuffer ReadFileUnsafeBuffer(string path)
        {
            if (!GetFileInfo(path, out var byteCount))
                return UnsafeFileBuffer.Empty;

            if (byteCount > s_MaxFileMB * 1024L * 1024L || byteCount > int.MaxValue)
            {
                Debug.LogError($"超过{s_MaxFileMB}MB");
                return UnsafeFileBuffer.Empty;
            }

            UnsafeFileBuffer self = new();
            self.length = byteCount;
            self.buffer = (byte*)UnsafeUtility.Malloc(byteCount, 16, Allocator.Temp);

            ReadCommand cmd;
            cmd.Offset = 0;
            cmd.Size = self.length;
            cmd.Buffer = self.buffer;

            var fileHandle = AsyncReadManager.OpenFileAsync(path);

            ReadCommandArray readCmdArray;
            readCmdArray.ReadCommands = &cmd;
            readCmdArray.CommandCount = 1;

            var readHandle = AsyncReadManager.Read(fileHandle, readCmdArray);

            var closeJob = fileHandle.Close(readHandle.JobHandle);
            closeJob.Complete();

            readHandle.Dispose();

            return self;
        }

    }
}