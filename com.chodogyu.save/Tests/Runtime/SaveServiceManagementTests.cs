using System;
using System.Collections.Generic;
using CDG.Core.Results;
using CDG.Save.Serialization;
using CDG.Save.Storage;
using NUnit.Framework;

namespace CDG.Save.Tests.Runtime
{
    public class SaveServiceManagementTests
    {
        [Test]
        public void Exists_NullSlot_ThrowsArgumentNullException()
        {
            SaveService service = CreateService(new RecordingSaveStorage());

            Assert.Throws<ArgumentNullException>(() =>
                service.Exists(null));
        }

        [Test]
        public void Exists_PrimaryExists_ReturnsTrueWithoutCheckingBackup()
        {
            RecordingSaveStorage storage = new RecordingSaveStorage
            {
                PrimaryExistsResult = Result<bool>.Success(true),
                BackupExistsResult = Result<bool>.Success(false)
            };

            SaveService service = CreateService(storage);

            Result<bool> result = service.Exists(new SaveSlot("slot-a"));

            Assert.IsTrue(result.IsSuccess);
            Assert.IsTrue(result.Value);

            CollectionAssert.AreEqual(
                new[] { SaveStorageCopy.Primary },
                storage.ExistsOrder);
        }

        [Test]
        public void Exists_PrimaryMissingAndBackupExists_ReturnsTrue()
        {
            RecordingSaveStorage storage = new RecordingSaveStorage
            {
                PrimaryExistsResult = Result<bool>.Success(false),
                BackupExistsResult = Result<bool>.Success(true)
            };

            SaveService service = CreateService(storage);

            Result<bool> result = service.Exists(new SaveSlot("slot-a"));

            Assert.IsTrue(result.IsSuccess);
            Assert.IsTrue(result.Value);

            CollectionAssert.AreEqual(
                new[] { SaveStorageCopy.Primary, SaveStorageCopy.Backup },
                storage.ExistsOrder);
        }

        [Test]
        public void Exists_PrimaryAndBackupMissing_ReturnsFalse()
        {
            RecordingSaveStorage storage = new RecordingSaveStorage
            {
                PrimaryExistsResult = Result<bool>.Success(false),
                BackupExistsResult = Result<bool>.Success(false)
            };

            SaveService service = CreateService(storage);

            Result<bool> result = service.Exists(new SaveSlot("slot-a"));

            Assert.IsTrue(result.IsSuccess);
            Assert.IsFalse(result.Value);
        }

        [Test]
        public void Exists_PrimaryCheckFails_ReturnsFailureWithoutCheckingBackup()
        {
            RecordingSaveStorage storage = new RecordingSaveStorage
            {
                PrimaryExistsResult = Result<bool>.Failure(new ResultError(
                    SaveErrorCodes.StorageReadFailed,
                    "primary exists failed"))
            };

            SaveService service = CreateService(storage);

            Result<bool> result = service.Exists(new SaveSlot("slot-a"));

            Assert.IsTrue(result.IsFailure);
            Assert.AreEqual(SaveErrorCodes.StorageReadFailed, result.Error.Code);

            CollectionAssert.AreEqual(
                new[] { SaveStorageCopy.Primary },
                storage.ExistsOrder);
        }

        [Test]
        public void Exists_BackupCheckFails_ReturnsFailure()
        {
            RecordingSaveStorage storage = new RecordingSaveStorage
            {
                PrimaryExistsResult = Result<bool>.Success(false),
                BackupExistsResult = Result<bool>.Failure(new ResultError(
                    SaveErrorCodes.StorageReadFailed,
                    "backup exists failed"))
            };

            SaveService service = CreateService(storage);

            Result<bool> result = service.Exists(new SaveSlot("slot-a"));

            Assert.IsTrue(result.IsFailure);
            Assert.AreEqual(SaveErrorCodes.StorageReadFailed, result.Error.Code);
        }

        [Test]
        public void Delete_NullSlot_ThrowsArgumentNullException()
        {
            SaveService service = CreateService(new RecordingSaveStorage());

            Assert.Throws<ArgumentNullException>(() =>
                service.Delete(null));
        }

        [Test]
        public void Delete_BothSucceed_DeletesPrimaryThenBackup()
        {
            RecordingSaveStorage storage = new RecordingSaveStorage();
            SaveService service = CreateService(storage);

            Result result = service.Delete(new SaveSlot("slot-a"));

            Assert.IsTrue(result.IsSuccess);

            CollectionAssert.AreEqual(
                new[] { SaveStorageCopy.Primary, SaveStorageCopy.Backup },
                storage.DeleteOrder);
        }

        [Test]
        public void Delete_PrimaryFails_ReturnsFailureWithoutDeletingBackup()
        {
            RecordingSaveStorage storage = new RecordingSaveStorage
            {
                PrimaryDeleteResult = Result.Failure(new ResultError(
                    SaveErrorCodes.StorageDeleteFailed,
                    "primary delete failed"))
            };

            SaveService service = CreateService(storage);

            Result result = service.Delete(new SaveSlot("slot-a"));

            Assert.IsTrue(result.IsFailure);
            Assert.AreEqual(SaveErrorCodes.StorageDeleteFailed, result.Error.Code);

            CollectionAssert.AreEqual(
                new[] { SaveStorageCopy.Primary },
                storage.DeleteOrder);
        }

        [Test]
        public void Delete_BackupFails_ReturnsFailureAfterPrimaryDelete()
        {
            RecordingSaveStorage storage = new RecordingSaveStorage
            {
                BackupDeleteResult = Result.Failure(new ResultError(
                    SaveErrorCodes.StorageDeleteFailed,
                    "backup delete failed"))
            };

            SaveService service = CreateService(storage);

            Result result = service.Delete(new SaveSlot("slot-a"));

            Assert.IsTrue(result.IsFailure);
            Assert.AreEqual(SaveErrorCodes.StorageDeleteFailed, result.Error.Code);

            CollectionAssert.AreEqual(
                new[] { SaveStorageCopy.Primary, SaveStorageCopy.Backup },
                storage.DeleteOrder);
        }

        private static SaveService CreateService(RecordingSaveStorage storage)
        {
            return new SaveService(storage, new StubSaveSerializer());
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

        private sealed class RecordingSaveStorage : ISaveStorage
        {
            public Result<bool> PrimaryExistsResult { get; set; } = Result<bool>.Success(false);
            public Result<bool> BackupExistsResult { get; set; } = Result<bool>.Success(false);

            public Result PrimaryDeleteResult { get; set; } = Result.Success();
            public Result BackupDeleteResult { get; set; } = Result.Success();

            public List<SaveStorageCopy> ExistsOrder { get; } = new List<SaveStorageCopy>();
            public List<SaveStorageCopy> DeleteOrder { get; } = new List<SaveStorageCopy>();

            public Result Write(SaveSlot slot, SaveStorageCopy copy, byte[] data)
            {
                throw new NotSupportedException();
            }

            public Result<byte[]> Read(SaveSlot slot, SaveStorageCopy copy)
            {
                throw new NotSupportedException();
            }

            public Result<bool> Exists(SaveSlot slot, SaveStorageCopy copy)
            {
                ExistsOrder.Add(copy);

                return copy == SaveStorageCopy.Primary
                    ? PrimaryExistsResult
                    : BackupExistsResult;
            }

            public Result Delete(SaveSlot slot, SaveStorageCopy copy)
            {
                DeleteOrder.Add(copy);

                return copy == SaveStorageCopy.Primary
                    ? PrimaryDeleteResult
                    : BackupDeleteResult;
            }
        }
    }
}