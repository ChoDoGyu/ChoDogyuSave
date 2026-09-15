using System;
using System.Collections.Generic;
using CDG.Core.Results;
using CDG.Save.Serialization;
using CDG.Save.Storage;
using NUnit.Framework;

namespace CDG.Save.Tests.Runtime
{
    public class SaveServiceSaveTests
    {
        [Test]
        public void Save_NullSlot_ThrowsArgumentNullException()
        {
            SpySaveSerializer serializer = new SpySaveSerializer();
            RecordingSaveStorage storage = new RecordingSaveStorage();
            SaveService service = new SaveService(storage, serializer);

            Assert.Throws<ArgumentNullException>(() =>
                service.Save(null, new TestSaveData()));
        }

        [Test]
        public void Save_SerializationFails_ReturnsFailureWithoutWritingStorage()
        {
            SpySaveSerializer serializer = new SpySaveSerializer
            {
                SerializeResult = Result<byte[]>.Failure(new ResultError(
                    SaveErrorCodes.SerializationFailed,
                    "serialization failed"))
            };

            RecordingSaveStorage storage = new RecordingSaveStorage();
            SaveService service = new SaveService(storage, serializer);

            Result result = service.Save(new SaveSlot("slot-a"), new TestSaveData());

            Assert.IsTrue(result.IsFailure);
            Assert.AreEqual(SaveErrorCodes.SerializationFailed, result.Error.Code);
            Assert.AreEqual(1, serializer.SerializeCallCount);
            Assert.AreEqual(0, storage.WriteOrder.Count);
        }

        [Test]
        public void Save_ValidData_SerializesOnceAndWritesSameBytesToBothCopies()
        {
            byte[] serializedData = { 1, 2, 3 };

            SpySaveSerializer serializer = new SpySaveSerializer
            {
                SerializeResult = Result<byte[]>.Success(serializedData)
            };

            RecordingSaveStorage storage = new RecordingSaveStorage();
            SaveService service = new SaveService(storage, serializer);
            TestSaveData data = new TestSaveData();

            Result result = service.Save(new SaveSlot("slot-a"), data);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(1, serializer.SerializeCallCount);
            Assert.AreSame(data, serializer.LastSerializedData);

            CollectionAssert.AreEqual(
                new[] { SaveStorageCopy.Primary, SaveStorageCopy.Backup },
                storage.WriteOrder);

            Assert.AreSame(serializedData, storage.PrimaryData);
            Assert.AreSame(serializedData, storage.BackupData);
        }

        [Test]
        public void Save_PrimaryWriteFails_ReturnsPrimaryFailure()
        {
            SpySaveSerializer serializer = new SpySaveSerializer
            {
                SerializeResult = Result<byte[]>.Success(new byte[] { 1 })
            };

            RecordingSaveStorage storage = new RecordingSaveStorage
            {
                PrimaryWriteResult = Result.Failure(new ResultError(
                    SaveErrorCodes.StorageWriteFailed,
                    "primary failed"))
            };

            SaveService service = new SaveService(storage, serializer);

            Result result = service.Save(new SaveSlot("slot-a"), new TestSaveData());

            Assert.IsTrue(result.IsFailure);
            Assert.AreEqual(SaveErrorCodes.StorageWriteFailed, result.Error.Code);
            Assert.AreEqual(1, storage.WriteOrder.Count);
            Assert.AreEqual(SaveStorageCopy.Primary, storage.WriteOrder[0]);
        }

        [Test]
        public void Save_BackupWriteFails_ReturnsBackupWriteFailed()
        {
            SpySaveSerializer serializer = new SpySaveSerializer
            {
                SerializeResult = Result<byte[]>.Success(new byte[] { 1 })
            };

            RecordingSaveStorage storage = new RecordingSaveStorage
            {
                BackupWriteResult = Result.Failure(new ResultError(
                    SaveErrorCodes.StorageWriteFailed,
                    "backup failed"))
            };

            SaveService service = new SaveService(storage, serializer);

            Result result = service.Save(new SaveSlot("slot-a"), new TestSaveData());

            Assert.IsTrue(result.IsFailure);
            Assert.AreEqual(SaveErrorCodes.BackupWriteFailed, result.Error.Code);

            CollectionAssert.AreEqual(
                new[] { SaveStorageCopy.Primary, SaveStorageCopy.Backup },
                storage.WriteOrder);
        }

        private sealed class TestSaveData
        {
        }

        private sealed class SpySaveSerializer : ISaveSerializer
        {
            public Result<byte[]> SerializeResult { get; set; } = Result<byte[]>.Success(new byte[] { 1 });

            public int SerializeCallCount { get; private set; }
            public object LastSerializedData { get; private set; }

            public Result<byte[]> Serialize<T>(T data)
            {
                SerializeCallCount++;
                LastSerializedData = data;

                return SerializeResult;
            }

            public Result<T> Deserialize<T>(byte[] data)
            {
                throw new NotSupportedException();
            }
        }

        private sealed class RecordingSaveStorage : ISaveStorage
        {
            public Result PrimaryWriteResult { get; set; } = Result.Success();
            public Result BackupWriteResult { get; set; } = Result.Success();

            public List<SaveStorageCopy> WriteOrder { get; } = new List<SaveStorageCopy>();

            public byte[] PrimaryData { get; private set; }
            public byte[] BackupData { get; private set; }

            public Result Write(SaveSlot slot, SaveStorageCopy copy, byte[] data)
            {
                WriteOrder.Add(copy);

                if (copy == SaveStorageCopy.Primary)
                {
                    PrimaryData = data;
                    return PrimaryWriteResult;
                }

                BackupData = data;
                return BackupWriteResult;
            }

            public Result<byte[]> Read(SaveSlot slot, SaveStorageCopy copy)
            {
                throw new NotSupportedException();
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