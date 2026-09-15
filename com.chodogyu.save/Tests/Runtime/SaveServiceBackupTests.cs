using System;
using System.Collections.Generic;
using CDG.Core.Results;
using CDG.Save.Serialization;
using CDG.Save.Storage;
using NUnit.Framework;

namespace CDG.Save.Tests.Runtime
{
    public class SaveServiceBackupTests
    {
        [Test]
        public void Constructor_NullStorage_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new SaveService(null, new StubSaveSerializer()));
        }

        [Test]
        public void Constructor_NullSerializer_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new SaveService(new FakeSaveStorage(), null));
        }

        [Test]
        public void WriteCopies_PrimaryAndBackupSucceed_WritesSameDataInOrder()
        {
            FakeSaveStorage storage = new FakeSaveStorage();
            SaveService service = new SaveService(storage, new StubSaveSerializer());
            SaveSlot slot = new SaveSlot("slot-a");
            byte[] data = { 1, 2, 3 };

            Result result = service.WriteCopies(slot, data);

            Assert.IsTrue(result.IsSuccess);
            CollectionAssert.AreEqual(
                new[] { SaveStorageCopy.Primary, SaveStorageCopy.Backup },
                storage.WriteOrder);
            Assert.AreSame(data, storage.PrimaryData);
            Assert.AreSame(data, storage.BackupData);
        }

        [Test]
        public void WriteCopies_PrimaryFails_DoesNotWriteBackup()
        {
            FakeSaveStorage storage = new FakeSaveStorage
            {
                PrimaryWriteResult = Result.Failure(new ResultError(
                    SaveErrorCodes.StorageWriteFailed,
                    "primary failed"))
            };

            SaveService service = new SaveService(storage, new StubSaveSerializer());

            Result result = service.WriteCopies(new SaveSlot("slot-a"), new byte[] { 1 });

            Assert.IsTrue(result.IsFailure);
            Assert.AreEqual(SaveErrorCodes.StorageWriteFailed, result.Error.Code);
            Assert.AreEqual(1, storage.WriteOrder.Count);
            Assert.AreEqual(SaveStorageCopy.Primary, storage.WriteOrder[0]);
        }

        [Test]
        public void WriteCopies_BackupFails_ReturnsBackupWriteFailed()
        {
            FakeSaveStorage storage = new FakeSaveStorage
            {
                BackupWriteResult = Result.Failure(new ResultError(
                    SaveErrorCodes.StorageWriteFailed,
                    "backup failed"))
            };

            SaveService service = new SaveService(storage, new StubSaveSerializer());

            Result result = service.WriteCopies(new SaveSlot("slot-a"), new byte[] { 1 });

            Assert.IsTrue(result.IsFailure);
            Assert.AreEqual(SaveErrorCodes.BackupWriteFailed, result.Error.Code);
            CollectionAssert.AreEqual(
                new[] { SaveStorageCopy.Primary, SaveStorageCopy.Backup },
                storage.WriteOrder);
        }

        private sealed class FakeSaveStorage : ISaveStorage
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

        private sealed class StubSaveSerializer : ISaveSerializer
        {
            public Result<byte[]> Serialize<T>(T data)
            {
                throw new NotSupportedException();
            }

            public Result<T> Deserialize<T>(byte[] data)
            {
                throw new NotSupportedException();
            }
        }
    }
}