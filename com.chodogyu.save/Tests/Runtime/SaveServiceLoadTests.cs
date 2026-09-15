using System;
using System.Collections.Generic;
using CDG.Core.Results;
using CDG.Save.Serialization;
using CDG.Save.Storage;
using NUnit.Framework;

namespace CDG.Save.Tests.Runtime
{
    public class SaveServiceLoadTests
    {
        [Test]
        public void Load_NullSlot_ThrowsArgumentNullException()
        {
            RecordingSaveStorage storage = new RecordingSaveStorage();
            SpySaveSerializer serializer = new SpySaveSerializer();
            SaveService service = new SaveService(storage, serializer);

            Assert.Throws<ArgumentNullException>(() =>
                service.Load<TestSaveData>(null));
        }

        [Test]
        public void Load_PrimaryExists_ReturnsLoadedFromPrimary()
        {
            byte[] primaryData = { 1, 2, 3 };
            TestSaveData loadedData = new TestSaveData();

            RecordingSaveStorage storage = new RecordingSaveStorage
            {
                PrimaryReadResult = Result<byte[]>.Success(primaryData)
            };

            SpySaveSerializer serializer = new SpySaveSerializer
            {
                DeserializedValue = loadedData
            };

            SaveService service = new SaveService(storage, serializer);

            Result<LoadResult<TestSaveData>> result =
                service.Load<TestSaveData>(new SaveSlot("slot-a"));

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(SaveLoadStatus.LoadedFromPrimary, result.Value.Status);
            Assert.IsTrue(result.Value.IsFound);
            Assert.AreSame(loadedData, result.Value.Value);

            CollectionAssert.AreEqual(
                new[] { SaveStorageCopy.Primary },
                storage.ReadOrder);

            Assert.AreEqual(1, serializer.DeserializeCallCount);
            Assert.AreSame(primaryData, serializer.LastDeserializedData);
        }

        [Test]
        public void Load_PrimaryMissingAndBackupExists_ReturnsLoadedFromBackup()
        {
            byte[] backupData = { 4, 5, 6 };
            TestSaveData loadedData = new TestSaveData();

            RecordingSaveStorage storage = new RecordingSaveStorage
            {
                PrimaryReadResult = MissingResult(),
                BackupReadResult = Result<byte[]>.Success(backupData)
            };

            SpySaveSerializer serializer = new SpySaveSerializer
            {
                DeserializedValue = loadedData
            };

            SaveService service = new SaveService(storage, serializer);

            Result<LoadResult<TestSaveData>> result =
                service.Load<TestSaveData>(new SaveSlot("slot-a"));

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(SaveLoadStatus.LoadedFromBackup, result.Value.Status);
            Assert.IsTrue(result.Value.IsFound);
            Assert.AreSame(loadedData, result.Value.Value);

            CollectionAssert.AreEqual(
                new[] { SaveStorageCopy.Primary, SaveStorageCopy.Backup },
                storage.ReadOrder);

            Assert.AreEqual(1, serializer.DeserializeCallCount);
            Assert.AreSame(backupData, serializer.LastDeserializedData);
        }

        [Test]
        public void Load_PrimaryAndBackupMissing_ReturnsNotFound()
        {
            RecordingSaveStorage storage = new RecordingSaveStorage
            {
                PrimaryReadResult = MissingResult(),
                BackupReadResult = MissingResult()
            };

            SpySaveSerializer serializer = new SpySaveSerializer();
            SaveService service = new SaveService(storage, serializer);

            Result<LoadResult<TestSaveData>> result =
                service.Load<TestSaveData>(new SaveSlot("slot-a"));

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(SaveLoadStatus.NotFound, result.Value.Status);
            Assert.IsFalse(result.Value.IsFound);

            CollectionAssert.AreEqual(
                new[] { SaveStorageCopy.Primary, SaveStorageCopy.Backup },
                storage.ReadOrder);

            Assert.AreEqual(0, serializer.DeserializeCallCount);
        }

        private static Result<byte[]> MissingResult()
        {
            return Result<byte[]>.Failure(new ResultError(
                SaveErrorCodes.StorageNotFound,
                "save not found"));
        }

        private sealed class TestSaveData
        {
        }

        private sealed class SpySaveSerializer : ISaveSerializer
        {
            public object DeserializedValue { get; set; }

            public int DeserializeCallCount { get; private set; }
            public byte[] LastDeserializedData { get; private set; }

            public Result<byte[]> Serialize<T>(T data)
            {
                throw new NotSupportedException();
            }

            public Result<T> Deserialize<T>(byte[] data)
            {
                DeserializeCallCount++;
                LastDeserializedData = data;

                return Result<T>.Success((T)DeserializedValue);
            }
        }

        private sealed class RecordingSaveStorage : ISaveStorage
        {
            public Result<byte[]> PrimaryReadResult { get; set; } = MissingResult();
            public Result<byte[]> BackupReadResult { get; set; } = MissingResult();

            public List<SaveStorageCopy> ReadOrder { get; } = new List<SaveStorageCopy>();

            public Result Write(SaveSlot slot, SaveStorageCopy copy, byte[] data)
            {
                throw new NotSupportedException();
            }

            public Result<byte[]> Read(SaveSlot slot, SaveStorageCopy copy)
            {
                ReadOrder.Add(copy);

                return copy == SaveStorageCopy.Primary
                    ? PrimaryReadResult
                    : BackupReadResult;
            }

            public Result<bool> Exists(SaveSlot slot, SaveStorageCopy copy)
            {
                throw new NotSupportedException();
            }

            public Result Delete(SaveSlot slot, SaveStorageCopy copy)
            {
                throw new NotSupportedException();
            }
        }
    }
}