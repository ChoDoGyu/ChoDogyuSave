using System;
using System.IO;
using CDG.Core.Results;
using CDG.Save.Storage;
using NUnit.Framework;

namespace CDG.Save.Tests.Runtime.Storage
{
    public class FileSaveStorageTests
    {
        private string rootPath;
        private FileSaveStorage storage;
        private SaveSlot slot;

        [SetUp]
        public void SetUp()
        {
            rootPath = Path.Combine(Path.GetTempPath(), "cdg-save-tests", Guid.NewGuid().ToString("N"));
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
        public void Write_Primary_CreatesDirectoryAndFile()
        {
            byte[] data = { 1, 2, 3 };

            Result result = storage.Write(slot, SaveStorageCopy.Primary, data);

            Assert.IsTrue(result.IsSuccess);
            Assert.IsTrue(Directory.Exists(rootPath));
            Assert.IsTrue(File.Exists(storage.GetPath(slot, SaveStorageCopy.Primary)));
            CollectionAssert.AreEqual(data, File.ReadAllBytes(storage.GetPath(slot, SaveStorageCopy.Primary)));
        }

        [Test]
        public void Write_Backup_CreatesBackupFile()
        {
            byte[] data = { 4, 5, 6 };

            Result result = storage.Write(slot, SaveStorageCopy.Backup, data);

            Assert.IsTrue(result.IsSuccess);
            Assert.IsTrue(File.Exists(storage.GetPath(slot, SaveStorageCopy.Backup)));
            CollectionAssert.AreEqual(data, File.ReadAllBytes(storage.GetPath(slot, SaveStorageCopy.Backup)));
        }

        [Test]
        public void Write_ExistingFile_OverwritesData()
        {
            byte[] firstData = { 1, 2, 3 };
            byte[] secondData = { 7, 8, 9 };

            storage.Write(slot, SaveStorageCopy.Primary, firstData);
            Result result = storage.Write(slot, SaveStorageCopy.Primary, secondData);

            Assert.IsTrue(result.IsSuccess);
            CollectionAssert.AreEqual(
                secondData,
                File.ReadAllBytes(storage.GetPath(slot, SaveStorageCopy.Primary)));
        }

        [Test]
        public void Write_EmptyData_CreatesEmptyFile()
        {
            Result result = storage.Write(slot, SaveStorageCopy.Primary, Array.Empty<byte>());

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(
                0,
                File.ReadAllBytes(storage.GetPath(slot, SaveStorageCopy.Primary)).Length);
        }

        [Test]
        public void Write_NullData_ReturnsInvalidDataFailure()
        {
            Result result = storage.Write(slot, SaveStorageCopy.Primary, null);

            Assert.IsTrue(result.IsFailure);
            Assert.AreEqual(SaveErrorCodes.InvalidData, result.Error.Code);
        }

        [Test]
        public void Write_InvalidRoot_ReturnsStorageWriteFailed()
        {
            Directory.CreateDirectory(rootPath);

            string blockingRootPath = Path.Combine(rootPath, "root-file");
            File.WriteAllText(blockingRootPath, "blocking");

            FileSaveStorage invalidStorage = new FileSaveStorage(blockingRootPath);

            Result result = invalidStorage.Write(slot, SaveStorageCopy.Primary, new byte[] { 1 });

            Assert.IsTrue(result.IsFailure);
            Assert.AreEqual(SaveErrorCodes.StorageWriteFailed, result.Error.Code);
        }

        [Test]
        public void Read_ExistingFile_ReturnsBytes()
        {
            byte[] expected = { 10, 20, 30 };

            Directory.CreateDirectory(rootPath);
            File.WriteAllBytes(storage.GetPath(slot, SaveStorageCopy.Primary), expected);

            Result<byte[]> result = storage.Read(slot, SaveStorageCopy.Primary);

            Assert.IsTrue(result.IsSuccess);
            CollectionAssert.AreEqual(expected, result.Value);
        }

        [Test]
        public void Read_EmptyFile_ReturnsEmptyBytes()
        {
            Directory.CreateDirectory(rootPath);
            File.WriteAllBytes(storage.GetPath(slot, SaveStorageCopy.Primary), Array.Empty<byte>());

            Result<byte[]> result = storage.Read(slot, SaveStorageCopy.Primary);

            Assert.IsTrue(result.IsSuccess);
            Assert.IsEmpty(result.Value);
        }

        [Test]
        public void Read_MissingFile_ReturnsStorageNotFound()
        {
            Result<byte[]> result = storage.Read(slot, SaveStorageCopy.Primary);

            Assert.IsTrue(result.IsFailure);
            Assert.AreEqual(SaveErrorCodes.StorageNotFound, result.Error.Code);
        }

        [Test]
        public void Exists_ExistingFile_ReturnsTrue()
        {
            Directory.CreateDirectory(rootPath);
            File.WriteAllBytes(storage.GetPath(slot, SaveStorageCopy.Primary), new byte[] { 1 });

            Result<bool> result = storage.Exists(slot, SaveStorageCopy.Primary);

            Assert.IsTrue(result.IsSuccess);
            Assert.IsTrue(result.Value);
        }

        [Test]
        public void Exists_MissingFile_ReturnsFalse()
        {
            Result<bool> result = storage.Exists(slot, SaveStorageCopy.Primary);

            Assert.IsTrue(result.IsSuccess);
            Assert.IsFalse(result.Value);
        }

        [Test]
        public void Delete_ExistingFile_DeletesFile()
        {
            Directory.CreateDirectory(rootPath);

            string path = storage.GetPath(slot, SaveStorageCopy.Primary);
            File.WriteAllBytes(path, new byte[] { 1 });

            Result result = storage.Delete(slot, SaveStorageCopy.Primary);

            Assert.IsTrue(result.IsSuccess);
            Assert.IsFalse(File.Exists(path));
        }

        [Test]
        public void Delete_MissingFile_ReturnsSuccess()
        {
            Result result = storage.Delete(slot, SaveStorageCopy.Primary);

            Assert.IsTrue(result.IsSuccess);
        }
    }
}