using System.IO;
using UnityEditor.Build.CacheServer;
using UnityEngine;

namespace UnityEditor.Build.Pipeline.Utilities
{
    class CacheServerUploader
    {
        Hash128 m_GlobalHash;

        Client m_Client;

        struct WorkItem
        {
            public FileId fileId;
            public string artifactsPath;
            public MemoryStream stream;
        }

        public CacheServerUploader(string host, int port = 8126)
        {
            m_Client = new Client(host, port);
            m_Client.Connect();
            m_GlobalHash = new Hash128(0, 0, 0, BuildCache.k_CacheServerVersion);
        }

        public void SetGlobalHash(Hash128 hash)
        {
            m_GlobalHash = hash;
        }

        // We return from this function before all uploads are complete. So we must wait to dispose until all uploads are finished.
        public void QueueUpload(CacheEntry entry, string artifactsPath, MemoryStream stream)
        {
            var item = new WorkItem();
            string finalHash = HashingMethods.Calculate(entry.Hash, m_GlobalHash).ToString();
            item.fileId = FileId.From(entry.Guid.ToString(), finalHash);
            item.artifactsPath = artifactsPath;
            item.stream = stream;
            ThreadingManager.QueueTask(ThreadingManager.ThreadQueues.UploadQueue, UploadItem, item);
        }

        private void UploadItem(object state)
        {
            var item = (WorkItem)state;
            m_Client.BeginTransaction(item.fileId);
            m_Client.Upload(FileType.Info, item.stream);

            string artifactsZip = Path.GetTempFileName();
            if (FileCompressor.Compress(item.artifactsPath, artifactsZip))
            {
                using (var stream = new FileStream(artifactsZip, FileMode.Open, FileAccess.Read))
                    m_Client.Upload(FileType.Resource, stream);
            }
            File.Delete(artifactsZip);

            m_Client.EndTransaction();
        }
    }
}
