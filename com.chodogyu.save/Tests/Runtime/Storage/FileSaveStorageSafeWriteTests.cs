using System;
using System.IO;
using CDG.Core.Results;
using CDG.Save.Storage;
using NUnit.Framework;

namespace CDG.Save.Tests.Runtime.Storage
{
    public class FileSaveStorageSafeWriteTests
    {
        private string rootPath;
        private FileSaveStorage storage;
        private SaveSlot slot;

        [SetUp]
        public void SetUp()
        {
            rootPath = Path.Combine(Path.GetTempPath(), "cdg-save-safe-write-tests", Guid.NewGuid().ToString("N"));
            storage = new FileSaveStorage(rootPath);
            slot = new SaveSlot("slot-a");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(rootPath))
            {
                Directory.Delete(rootPath, true);
            }
        }

        [Test]
        public void Write_NewFile_MovesTempToTargetAndRemovesTemp()
        {
            byte[] data = { 1, 2, 3 };

            Result result = storage.Write(slot, SaveStorageCopy.Primary, data);

            string targetPath = storage.GetPath(slot, SaveStorageCopy.Primary);
            string tempPath = storage.GetTempPath(slot);

            Assert.IsTrue(result.IsSuccess);
            Assert.IsTrue(File.Exists(targetPath));
            Assert.IsFalse(File.Exists(tempPath));
            CollectionAssert.AreEqual(data, File.ReadAllBytes(targetPath));
        }

        [Test]
        public void Write_ExistingFile_ReplacesTargetAndRemovesTemp()
        {
            byte[] oldData = { 1, 1, 1 };
            byte[] newData = { 9, 8, 7 };

            Result firstWrite = storage.Write(slot, SaveStorageCopy.Primary, oldData);
            Result secondWrite = storage.Write(slot, SaveStorageCopy.Primary, newData);

            string targetPath = storage.GetPath(slot, SaveStorageCopy.Primary);
            string tempPath = storage.GetTempPath(slot);

            Assert.IsTrue(firstWrite.IsSuccess);
            Assert.IsTrue(secondWrite.IsSuccess);
            Assert.IsTrue(File.Exists(targetPath));
            Assert.IsFalse(File.Exists(tempPath));
            CollectionAssert.AreEqual(newData, File.ReadAllBytes(targetPath));
        }

        [Test]
        public void Write_Backup_MovesTempToBackupAndRemovesTemp()
        {
            byte[] data = { 4, 5, 6 };

            Result result = storage.Write(slot, SaveStorageCopy.Backup, data);

            string backupPath = storage.GetPath(slot, SaveStorageCopy.Backup);
            string tempPath = storage.GetTempPath(slot);

            Assert.IsTrue(result.IsSuccess);
            Assert.IsTrue(File.Exists(backupPath));
            Assert.IsFalse(File.Exists(tempPath));
            CollectionAssert.AreEqual(data, File.ReadAllBytes(backupPath));
        }

        [Test]
        public void Write_TargetReplacementFails_RemovesTempAndReturnsStorageWriteFailed()
        {
            Directory.CreateDirectory(rootPath);

            string targetPath = storage.GetPath(slot, SaveStorageCopy.Primary);
            Directory.CreateDirectory(targetPath);

            Result result = storage.Write(slot, SaveStorageCopy.Primary, new byte[] { 1, 2, 3 });

            string tempPath = storage.GetTempPath(slot);

            Assert.IsTrue(result.IsFailure);
            Assert.AreEqual(SaveErrorCodes.StorageWriteFailed, result.Error.Code);
            Assert.IsFalse(File.Exists(tempPath));
            Assert.IsTrue(Directory.Exists(targetPath));
        }
    }
}