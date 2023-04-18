using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine.Assertions;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Daihenka.AssetPipeline.Import
{
    // TODO: Implement a version field for AssetImportProfiles that increment when they are changed
    public class ImportProfileUserData
    {
        static readonly List<ImportProfileUserData> s_Cache = new List<ImportProfileUserData>();
        const string searchString = "\"ImportProfileData\": { ";
        readonly AssetImporter m_Importer;
        string m_ImporterJson;
        ProfilesUserData m_ImporterProfileUserData;
        bool m_Modified;
        public bool IsModified => m_Modified;

        public static void ClearCache()
        {
            s_Cache.Clear();
        }

        public static ImportProfileUserData Get(string assetPath)
        {
            for (var i = s_Cache.Count - 1; i >= 0; i--)
            {
                if (s_Cache[i].m_Importer != null)
                {
                    if (s_Cache[i].m_Importer.assetPath == assetPath)
                    {
                        return s_Cache[i];
                    }
                }
                else
                {
                    s_Cache.RemoveAt(i); // File was likely moved
                }
            }

            var ud = new ImportProfileUserData(assetPath);
            if (ud.m_Importer != null)
            {
                s_Cache.Add(ud);
                return s_Cache[s_Cache.Count - 1];
            }

            return null;
        }

        public static void AddOrUpdateProcessor(string assetPath, AssetProcessor processor)
        {
            Get(assetPath)?.AddOrUpdateProcessor(processor);
        }

        public static bool HasProcessor(string assetPath, AssetProcessor processor)
        {
            var value = Get(assetPath)?.HasProcessor(processor);
            return value.HasValue && value.Value;
        }

        public ImportProfileUserData(string assetPath)
        {
            m_Importer = AssetImporter.GetAtPath(assetPath);
            if (m_Importer == null)
            {
                Debug.LogError($"Could not find AssetImporter for {assetPath}");
                return;
            }

            ParseUserData();
        }

        public AssetImporter GetAssetImporter() { return m_Importer; } 
        void ParseUserData()
        {
            GetImporterJson();
            var profilesUserData = new ProfilesUserData();
            if (!string.IsNullOrEmpty(m_ImporterJson))
            {
                profilesUserData = JsonUtility.FromJson<ProfilesUserData>(m_ImporterJson);
            }

            m_ImporterProfileUserData = profilesUserData;
        }

        void GetImporterJson()
        {
            Assert.IsNotNull(m_Importer);
            var userData = m_Importer.userData;
            var idfStartIndex = userData.IndexOf(searchString, StringComparison.Ordinal);
            var idfEndIndex = -1;
            if (idfStartIndex == -1)
            {
                return;
            }

            idfEndIndex = idfStartIndex + searchString.Length;
            var counter = 0;
            var startIndex = idfEndIndex;
            while (idfEndIndex < userData.Length)
            {
                if (userData[idfEndIndex] == '{')
                {
                    counter++;
                }
                else if (userData[idfEndIndex] == '}')
                {
                    counter--;
                }

                if (counter == -1)
                {
                    break;
                }

                ++idfEndIndex;
            }

            Assert.AreEqual(-1, counter);

            var length = idfEndIndex - startIndex;
            m_ImporterJson = userData.Substring(startIndex, length);
            if (m_ImporterJson.Length > 0)
            {
                for (var i = m_ImporterJson.Length - 1; i >= 0; i--)
                {
                    if (m_ImporterJson[i] == ' ')
                    {
                        m_ImporterJson = m_ImporterJson.Remove(i);
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        public void SaveUserData()
        {
            if (!m_Modified)
            {
                return;
            }

            var json = JsonUtility.ToJson(m_ImporterProfileUserData);
            if (string.Equals(json, m_ImporterJson))
            {
                return;
            }

            var importProfileUserData = $"{searchString}{json} }}";
            var userData = m_Importer.userData;

            var idfStartIndex = userData.IndexOf(searchString, StringComparison.Ordinal);
            var idfEndIndex = -1;
            if (idfStartIndex != -1)
            {
                idfEndIndex = idfStartIndex + searchString.Length;
                var counter = 0;
                while (idfEndIndex < userData.Length)
                {
                    if (userData[idfEndIndex] == '{')
                    {
                        counter++;
                    }
                    else if (userData[idfEndIndex] == '}')
                    {
                        counter--;
                    }

                    if (counter == -1)
                    {
                        break;
                    }

                    ++idfEndIndex;
                }

                Assert.AreEqual(-1, counter);
            }

            if (idfStartIndex >= 0 && idfEndIndex > idfStartIndex)
            {
                var length = idfEndIndex - idfStartIndex;
                if (userData.Length < idfStartIndex + length)
                {
                    Debug.LogError("Problem setting user data");
                }

                if (importProfileUserData == userData.Substring(idfStartIndex, length))
                {
                    Debug.LogError("Bad checks");
                    return;
                }

                userData = userData.Remove(idfStartIndex, (idfEndIndex - idfStartIndex) + 1);
            }
            else
            {
                idfStartIndex = 0;
            }

            m_ImporterJson = json;
            m_Importer.userData = userData.Insert(idfStartIndex, importProfileUserData);

            var stackTrace = new StackTrace();
            for (var i = 0; i < stackTrace.FrameCount; i++)
            {
                var frame = stackTrace.GetFrame(i);
                if (frame.GetMethod().Name == "OnPostprocessAllAssets")
                {
                    m_Importer.SaveAndReimport();
                    break;
                }
            }
        }

        public void AddOrUpdateProcessor(AssetProcessor processor)
        {
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(processor, out var guid, out long fileId))
            {
                m_ImporterProfileUserData.AddOrUpdate(guid, fileId, processor.Version);
                m_Modified = true;
            }
        }

        public bool HasProcessor(AssetProcessor processor)
        {
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(processor, out var guid, out long fileId))
            {
                return m_ImporterProfileUserData != null && m_ImporterProfileUserData.HasProcessor(guid, fileId, processor.Version);
            }

            // Should not hit this
            throw new ArgumentException($"{processor.GetType().FullName} is missing a guid and file id - likely Save Project has not been triggered");
        }
    }

    [Serializable]
    internal class ProfilesUserData
    {
        public List<ProcessorUserData> processorData;

        public void AddOrUpdate(string guid, long fileId, int version)
        {
            if (processorData == null)
            {
                processorData = new List<ProcessorUserData>();
            }

            for (var i = 0; i < processorData.Count; i++)
            {
                if (processorData[i].guid == guid && processorData[i].fileId == fileId)
                {
                    var processor = processorData[i];
                    processor.guid = guid;
                    processor.fileId = fileId;
                    processor.version = version;
                    processor.timestamp = DateTime.UtcNow.Ticks;
                    processorData[i] = processor;
                    return;
                }
            }

            processorData.Add(new ProcessorUserData(guid, fileId, version));
        }

        public bool HasProcessor(string guid, long fileId, int version)
        {
            return processorData != null && processorData.Any(p => p.guid == guid && p.fileId == fileId && p.version == version);
        }
    }

    [Serializable]
    internal class ProcessorUserData
    {
        public string guid;
        public long fileId;
        public long timestamp;
        public int version;

        public ProcessorUserData(string guid, long fileId, int version)
        {
            this.guid = guid;
            this.fileId = fileId;
            this.version = version;
            timestamp = DateTime.UtcNow.Ticks;
        }
    }
}