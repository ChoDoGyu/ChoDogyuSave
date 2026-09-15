using System;
using System.Collections.Generic;
using CDG.Core.Results;
using CDG.Save.Serialization;
using CDG.Save.Storage;
using NUnit.Framework;

namespace CDG.Save.Tests.Runtime
{
    public class SaveServiceRecoveryTests
    {
        [Test]
        public void Load_PrimaryDeserializeFailsAndBackupSucceeds_ReturnsLoadedFromBackup()
        {
            byte[] primaryData = { 1 };
            byte[] backupData = { 2 };
            TestSaveData backupValue = new TestSaveData();

            RecordingSaveStorage storage = new RecordingSaveStorage
            {
                PrimaryReadResult = Result<byte[]>.Success(primaryData),
                BackupReadResult = Result<byte[]>.Success(backupData)
            };

            SequenceSaveSerializer serializer = new SequenceSaveSerializer();
            serializer.AddDeserializeResult(Result<TestSaveData>.Failure(new ResultError(
                SaveErrorCodes.DeserializationFailed,
                "primary corrupt")));
            serializer.AddDeserializeResult(Result<TestSaveData>.Success(backupValue));

            SaveService service = new SaveService(storage, serializer);

            Result<LoadResult<TestSaveData>> result =
                service.Load<TestSaveData>(new SaveSlot("slot-a"));

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(SaveLoadStatus.LoadedFromBackup, result.Value.Status);
            Assert.AreSame(backupValue, result.Value.Value);

            CollectionAssert.AreEqual(
                new[] { SaveStorageCopy.Primary, SaveStorageCopy.Backup },
                storage.ReadOrder);

            Assert.AreEqual(2, serializer.DeserializeCallCount);
        }

        [Test]
        public void Load_PrimaryReadFailsAndBackupSucceeds_ReturnsLoadedFromBackup()
        {
            TestSaveData backupValue = new TestSaveData();

            RecordingSaveStorage storage = new RecordingSaveStorage
            {
                PrimaryReadResult = Result<byte[]>.Failure(new ResultError(
                    SaveErrorCodes.StorageReadFailed,
                    "primary read failed")),
                BackupReadResult = Result<byte[]>.Success(new byte[] { 2 })
            };

            SequenceSaveSerializer serializer = new SequenceSaveSerializer();
            serializer.AddDeserializeResult(Result<TestSaveData>.Success(backupValue));

            SaveService service = new SaveService(storage, serializer);

            Result<LoadResult<TestSaveData>> result =
                service.Load<TestSaveData>(new SaveSlot("slot-a"));

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(SaveLoadStatus.LoadedFromBackup, result.Value.Status);
            Assert.AreSame(backupValue, result.Value.Value);
            Assert.AreEqual(1, serializer.DeserializeCallCount);
        }

        [Test]
        public void Load_PrimaryCorruptAndBackupMissing_ReturnsCorruptedData()
        {
            RecordingSaveStorage storage = new RecordingSaveStorage
            {
                PrimaryReadResult = Result<byte[]>.Success(new byte[] { 1 }),
                BackupReadResult = MissingResult()
            };

            SequenceSaveSerializer serializer = new SequenceSaveSerializer();
            serializer.AddDeserializeResult(Result<TestSaveData>.Failure(new ResultError(
                SaveErrorCodes.DeserializationFailed,
                "primary corrupt")));

            SaveService service = new SaveService(storage, serializer);

            Result<LoadResult<TestSaveData>> result =
                service.Load<TestSaveData>(new SaveSlot("slot-a"));

            Assert.IsTrue(result.IsFailure);
            Assert.AreEqual(SaveErrorCodes.CorruptedData, result.Error.Code);
        }

        [Test]
        public void Load_PrimaryAndBackupCorrupt_ReturnsCorruptedData()
        {
            RecordingSaveStorage storage = new RecordingSaveStorage
            {
                PrimaryReadResult = Result<byte[]>.Success(new byte[] { 1 }),
                BackupReadResult = Result<byte[]>.Success(new byte[] { 2 })
            };

            SequenceSaveSerializer serializer = new SequenceSaveSerializer();
            serializer.AddDeserializeResult(Result<TestSaveData>.Failure(new ResultError(
                SaveErrorCodes.DeserializationFailed,
                "primary corrupt")));
            serializer.AddDeserializeResult(Result<TestSaveData>.Failure(new ResultError(
                SaveErrorCodes.DeserializationFailed,
                "backup corrupt")));

            SaveService service = new SaveService(storage, serializer);

            Result<LoadResult<TestSaveData>> result =
                service.Load<TestSaveData>(new SaveSlot("slot-a"));

            Assert.IsTrue(result.IsFailure);
            Assert.AreEqual(SaveErrorCodes.CorruptedData, result.Error.Code);
            Assert.AreEqual(2, serializer.DeserializeCallCount);
        }

        [Test]
        public void Load_PrimaryReadFailsAndBackupMissing_ReturnsPrimaryReadFailure()
        {
            RecordingSaveStorage storage = new RecordingSaveStorage
            {
                PrimaryReadResult = Result<byte[]>.Failure(new ResultError(
                    SaveErrorCodes.StorageReadFailed,
                    "primary read failed")),
                BackupReadResult = MissingResult()
            };

            SaveService service = new SaveService(storage, new SequenceSaveSerializer());

            Result<LoadResult<TestSaveData>> result =
                service.Load<TestSaveData>(new SaveSlot("slot-a"));

            Assert.IsTrue(result.IsFailure);
            Assert.AreEqual(SaveErrorCodes.StorageReadFailed, result.Error.Code);
        }

        [Test]
        public void Load_PrimaryReadFailsAndBackupReadFails_ReturnsBackupReadFailure()
        {
            RecordingSaveStorage storage = new RecordingSaveStorage
            {
                PrimaryReadResult = Result<byte[]>.Failure(new ResultError(
                    SaveErrorCodes.StorageReadFailed,
                    "primary read failed")),
                BackupReadResult = Result<byte[]>.Failure(new ResultError(
                    SaveErrorCodes.StorageReadFailed,
                    "backup read failed"))
            };

            SaveService service = new SaveService(storage, new SequenceSaveSerializer());

            Result<LoadResult<TestSaveData>> result =
                service.Load<TestSaveData>(new SaveSlot("slot-a"));

            Assert.IsTrue(result.IsFailure);
            Assert.AreEqual(SaveErrorCodes.StorageReadFailed, result.Error.Code);
            Assert.AreEqual("backup read failed", result.Error.Message);
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

        private sealed class SequenceSaveSerializer : ISaveSerializer
        {
            private readonly Queue<object> deserializeResults = new Queue<object>();

            public int DeserializeCallCount { get; private set; }

            public void AddDeserializeResult<T>(Result<T> result)
            {
                deserializeResults.Enqueue(result);
            }

            public Result<byte[]> Serialize<T>(T data)
            {
                throw new NotSupportedException();
            }

            public Result<T> Deserialize<T>(byte[] data)
            {
                DeserializeCallCount++;
                return (Result<T>)deserializeResults.Dequeue();
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