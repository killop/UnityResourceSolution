using System;
using System.IO;
using System.Threading;
using NUnit.Framework;
using UnityEditor.Build.CacheServer;
using UnityEngine;
using Random = System.Random;

namespace UnityEditor.CacheServerTests
{
    [TestFixture]
    public class Tests
    {
        private const string KTestHost = "127.0.0.1";
        private const string KInvalidTestHost = "192.0.2.1";
        private Random rand;

        private static int TestPort
        {
            get { return LocalCacheServer.Port; }
        }

        private FileId GenerateFileId()
        {
            if (rand == null)
                rand = new Random();
            var guid = new byte[16];
            var hash = new byte[16];
            rand.NextBytes(guid);
            rand.NextBytes(hash);
            return FileId.From(guid, hash);
        }

        [OneTimeSetUp]
        public void BeforeAll()
        {
            var cachePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            LocalCacheServer.Setup(1024 * 1024, cachePath);
            rand = new Random();
        }

        [OneTimeTearDown]
        public void AfterAll()
        {
            LocalCacheServer.Clear();
        }

        [Test]
        public void FileDownloadItem()
        {
            var fileId = GenerateFileId();
            var readStream = new ByteArrayStream(128 * 1024);

            var client = new Client(KTestHost, TestPort);
            client.Connect();

            client.BeginTransaction(fileId);
            client.Upload(FileType.Asset, readStream);
            client.EndTransaction();
            Thread.Sleep(50); // give the server a little time to finish the transaction

            var targetFile = Path.GetTempFileName();
            var downloadItem = new FileDownloadItem(fileId, FileType.Asset, targetFile);

            var mre = new ManualResetEvent(false);
            Exception err = null;
            client.DownloadFinished += (sender, args) =>
            {
                try
                {
                    Assert.AreEqual(DownloadResult.Success, args.Result);
                    Assert.AreEqual(args.DownloadItem.Id, fileId);
                    Assert.IsTrue(File.Exists(targetFile));

                    var fileBytes = File.ReadAllBytes(targetFile);
                    Assert.IsTrue(Util.ByteArraysAreEqual(readStream.BackingBuffer, fileBytes));
                }
                catch (Exception e)
                {
                    err = e;
                }
                finally
                {
                    if (File.Exists(targetFile))
                        File.Delete(targetFile);

                    mre.Set();
                }
            };

            client.QueueDownload(downloadItem);
            Assert.IsTrue(mre.WaitOne(2000));

            if (err != null)
                throw err;
        }

        [Test]
        public void QueueDownloadFromDownloadFinishedCallback()
        {
            var fileId = GenerateFileId();
            var readStream = new ByteArrayStream(128 * 1024);

            var client = new Client(KTestHost, TestPort);
            client.Connect();

            client.BeginTransaction(fileId);
            client.Upload(FileType.Asset, readStream);
            client.EndTransaction();
            Thread.Sleep(50); // give the server a little time to finish the transaction

            var targetFile1 = Path.GetTempFileName();
            var downloadItem1 = new FileDownloadItem(fileId, FileType.Asset, targetFile1);

            var targetFile2 = Path.GetTempFileName();
            var downloadItem2 = new FileDownloadItem(fileId, FileType.Asset, targetFile2);

            var mre = new ManualResetEvent(false);
            Exception err = null;
            client.DownloadFinished += (sender1, args1) =>
            {
                try
                {
                    Assert.AreEqual(DownloadResult.Success, args1.Result);
                    Assert.AreEqual(args1.DownloadItem.Id, fileId);
                    Assert.IsTrue(File.Exists(targetFile1));

                    var fileBytes = File.ReadAllBytes(targetFile1);
                    Assert.IsTrue(Util.ByteArraysAreEqual(readStream.BackingBuffer, fileBytes));
                }
                catch (Exception e)
                {
                    err = e;
                }
                finally
                {
                    if (File.Exists(targetFile1))
                        File.Delete(targetFile1);

                    client.ResetDownloadFinishedEventHandler();
                    client.DownloadFinished += (sender2, args2) =>
                    {
                        try
                        {
                            Assert.AreEqual(DownloadResult.Success, args2.Result);
                            Assert.AreEqual(args2.DownloadItem.Id, fileId);
                            Assert.IsTrue(File.Exists(targetFile2));

                            var fileBytes = File.ReadAllBytes(targetFile2);
                            Assert.IsTrue(Util.ByteArraysAreEqual(readStream.BackingBuffer, fileBytes));
                        }
                        catch (Exception e)
                        {
                            err = e;
                        }
                        finally
                        {
                            if (File.Exists(targetFile2))
                                File.Delete(targetFile2);

                            mre.Set();
                        }
                    };
                    client.QueueDownload(downloadItem2);
                }
            };

            client.QueueDownload(downloadItem1);
            Assert.IsTrue(mre.WaitOne(2000));

            if (err != null)
                throw err;
        }

        [Test]
        public void Connect()
        {
            var client = new Client(KTestHost, TestPort);
            client.Connect();
            client.Close();
        }

        [Test]
        public void ConnectTimeout()
        {
            var client = new Client(KInvalidTestHost, TestPort);
            TimeoutException err = null;
            try
            {
                client.Connect(0);
            }
            catch (TimeoutException e)
            {
                err = e;
            }
            finally
            {
                client.Close();
                Debug.Assert(err != null);
            }
        }

        [Test]
        public void TransactionIsolation()
        {
            var fileId = GenerateFileId();
            var readStream = new ByteArrayStream(16 * 1024);

            var client = new Client(KTestHost, TestPort);
            client.Connect();

            Assert.Throws<TransactionIsolationException>(() => client.Upload(FileType.Asset, readStream));
            Assert.Throws<TransactionIsolationException>(() => client.EndTransaction());

            // Back-to-back begin transactions are allowed
            client.BeginTransaction(fileId);
            Assert.DoesNotThrow(() => client.BeginTransaction(fileId));
        }

        [Test]
        public void UploadDownloadOne()
        {
            var fileId = GenerateFileId();
            var readStream = new ByteArrayStream(16 * 1024);

            var client = new Client(KTestHost, TestPort);
            client.Connect();

            client.BeginTransaction(fileId);
            client.Upload(FileType.Asset, readStream);
            client.EndTransaction();
            Thread.Sleep(50); // give the server a little time to finish the transaction

            var downloadItem = new TestDownloadItem(fileId, FileType.Asset);

            client.QueueDownload(downloadItem);

            Exception err = null;
            var mre = new ManualResetEvent(false);
            client.DownloadFinished += (sender, args) =>
            {
                try
                {
                    Assert.AreEqual(0, args.DownloadQueueLength);
                    Assert.AreEqual(DownloadResult.Success, args.Result);
                    Assert.AreEqual(fileId, args.DownloadItem.Id);
                }
                catch (Exception e)
                {
                    err = e;
                }
                finally
                {
                    mre.Set();
                }
            };

            Assert.IsTrue(mre.WaitOne(2000));

            if (err != null)
                throw err;

            Assert.IsTrue(Util.ByteArraysAreEqual(readStream.BackingBuffer, downloadItem.Bytes));
        }

        [Test]
        public void DownloadMany()
        {
            const int fileCount = 5;

            var fileIds = new FileId[fileCount];
            var fileStreams = new ByteArrayStream[fileCount];

            var client = new Client(KTestHost, TestPort);
            client.Connect();

            // Upload files
            var rand = new Random();
            for (var i = 0; i < fileCount; i++)
            {
                fileIds[i] = GenerateFileId();
                fileStreams[i] = new ByteArrayStream(rand.Next(64 * 1024, 128 * 1024));

                client.BeginTransaction(fileIds[i]);
                client.Upload(FileType.Asset, fileStreams[i]);
                client.EndTransaction();
            }

            Thread.Sleep(50);

            // Download
            var receivedCount = 0;
            Exception err = null;
            var mre = new ManualResetEvent(false);
            client.DownloadFinished += (sender, args) =>
            {
                try
                {
                    Assert.AreEqual(args.Result, DownloadResult.Success);
                    Assert.AreEqual(args.DownloadItem.Id, fileIds[receivedCount]);

                    var downloadItem = (TestDownloadItem)args.DownloadItem;
                    Assert.IsTrue(Util.ByteArraysAreEqual(fileStreams[receivedCount].BackingBuffer, downloadItem.Bytes));

                    receivedCount++;
                    Assert.AreEqual(fileCount - receivedCount, args.DownloadQueueLength);
                }
                catch (Exception e)
                {
                    err = e;
                }
                finally
                {
                    if (err != null || receivedCount == fileCount)
                        mre.Set();
                }
            };

            for (var i = 0; i < fileCount; i++)
                client.QueueDownload(new TestDownloadItem(fileIds[i], FileType.Asset));

            Assert.AreEqual(fileCount, client.DownloadQueueLength);

            Assert.IsTrue(mre.WaitOne(2000));

            if (err != null)
                throw err;

            Assert.AreEqual(fileCount, receivedCount);
        }

        [Test]
        public void DonwloadFileNotFound()
        {
            var client = new Client(KTestHost, TestPort);
            client.Connect();

            var fileId = FileId.From(new byte[16], new byte[16]);

            var mre = new ManualResetEvent(false);
            var downloadItem = new TestDownloadItem(fileId, FileType.Asset);

            client.QueueDownload(downloadItem);

            Exception err = null;

            client.DownloadFinished += (sender, args) =>
            {
                try
                {
                    Assert.AreEqual(args.Result, DownloadResult.FileNotFound);
                    Assert.AreEqual(args.DownloadItem.Id, fileId);
                }
                catch (Exception e)
                {
                    err = e;
                }
                finally
                {
                    mre.Set();
                }
            };

            mre.WaitOne(500);

            if (err != null)
                throw err;
        }

        [Test]
        public void ResetDownloadFinishedEventHandler()
        {
            var fileId = GenerateFileId();
            var readStream = new ByteArrayStream(16 * 1024);

            var client = new Client(KTestHost, TestPort);
            client.Connect();

            client.BeginTransaction(fileId);
            client.Upload(FileType.Asset, readStream);
            client.EndTransaction();
            Thread.Sleep(50);

            var downloadItem = new TestDownloadItem(fileId, FileType.Asset);

            // Add two listeners that will assert if called
            client.DownloadFinished += (sender, args) => { Debug.Assert(false); };
            client.DownloadFinished += (sender, args) => { Debug.Assert(false); };

            // Clear the listeners so they will not be called
            client.ResetDownloadFinishedEventHandler();

            client.QueueDownload(downloadItem);

            var mre = new ManualResetEvent(false);
            ThreadPool.QueueUserWorkItem(state =>
            {
                while (client.DownloadQueueLength > 0)
                    Thread.Sleep(0);

                mre.Set();
            });

            Assert.IsTrue(mre.WaitOne(2000));
        }

        static void WriteBytesToStream(Stream stream, byte[] buffer, int offset, int count)
        {
            // Write to stream, and reset position for read
            stream.Write(buffer, offset, count);
            stream.Position -= count;
        }

        [Test]
        public void ReadHeader_AppendsDataToBuffer()
        {
            using (var stream = new MemoryStream())
            {
                var client = new Client(KTestHost, TestPort);
                client.m_stream = stream;

                Exception err = null;
                var resetEvent = new ManualResetEvent(false);
                var testBuffer = new byte[] { (byte)'-', (byte)'0' };

                WriteBytesToStream(stream, testBuffer, (int)stream.Length, 1);

                // Setup checks / callback
                client.OnReadHeader += (bytesRead, readBuffer) =>
                {
                    try
                    {
                        // Validate written and read bytes match
                        Assert.AreEqual((int)stream.Length, bytesRead);
                        // Validate buffer contents are correct up to the current written bytes
                        for (int i = 0; i < (int)stream.Length; i++)
                            Assert.AreEqual(testBuffer[i], readBuffer[i]);

                        if ((int)stream.Length < testBuffer.Length)
                            WriteBytesToStream(stream, testBuffer, (int)stream.Length, 1);
                        else
                            resetEvent.Set();
                    }
                    catch (Exception e)
                    {
                        err = e;
                        resetEvent.Set();
                    }
                };

                // Run test
                client.ReadNextDownloadResult();
                Assert.IsTrue(resetEvent.WaitOne(2000));

                if (err != null)
                    throw err;
            }
        }
    }
}
