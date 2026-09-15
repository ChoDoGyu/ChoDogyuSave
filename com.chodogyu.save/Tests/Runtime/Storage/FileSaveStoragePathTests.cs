using System;
using System.IO;
using CDG.Save.Storage;
using NUnit.Framework;

namespace CDG.Save.Tests.Runtime.Storage
{
    public class FileSaveStoragePathTests
    {
        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_InvalidRootPath_ThrowsArgumentException(string rootPath)
        {
            Assert.Throws<ArgumentException>(() => new FileSaveStorage(rootPath));
        }

        [Test]
        public void GetPath_Primary_ReturnsSavePath()
        {
            string rootPath = Path.Combine(Path.GetTempPath(), "cdg-save-tests");
            FileSaveStorage storage = new FileSaveStorage(rootPath);
            SaveSlot slot = new SaveSlot("slot-a");

            string path = storage.GetPath(slot, SaveStorageCopy.Primary);

            Assert.AreEqual(
                Path.Combine(Path.GetFullPath(rootPath), "slot-a.save"),
                path);
        }

        [Test]
        public void GetPath_Backup_ReturnsBackupPath()
        {
            string rootPath = Path.Combine(Path.GetTempPath(), "cdg-save-tests");
            FileSaveStorage storage = new FileSaveStorage(rootPath);
            SaveSlot slot = new SaveSlot("slot-a");

            string path = storage.GetPath(slot, SaveStorageCopy.Backup);

            Assert.AreEqual(
                Path.Combine(Path.GetFullPath(rootPath), "slot-a.save.bak"),
                path);
        }

        [Test]
        public void GetTempPath_ReturnsTempPath()
        {
            string rootPath = Path.Combine(Path.GetTempPath(), "cdg-save-tests");
            FileSaveStorage storage = new FileSaveStorage(rootPath);
            SaveSlot slot = new SaveSlot("slot-a");

            string path = storage.GetTempPath(slot);

            Assert.AreEqual(
                Path.Combine(Path.GetFullPath(rootPath), "slot-a.save.tmp"),
                path);
        }

        [Test]
        public void GetPath_RelativeRoot_NormalizesToFullPath()
        {
            string rootPath = Path.Combine(".", "cdg-save-tests");
            FileSaveStorage storage = new FileSaveStorage(rootPath);
            SaveSlot slot = new SaveSlot("slot-a");

            string path = storage.GetPath(slot, SaveStorageCopy.Primary);

            Assert.IsTrue(Path.IsPathRooted(path));
            Assert.AreEqual(
                Path.Combine(Path.GetFullPath(rootPath), "slot-a.save"),
                path);
        }

        [Test]
        public void GetPath_InvalidCopy_ThrowsArgumentOutOfRangeException()
        {
            FileSaveStorage storage = new FileSaveStorage(Path.GetTempPath());
            SaveSlot slot = new SaveSlot("slot-a");

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                storage.GetPath(slot, (SaveStorageCopy)999));
        }

        [Test]
        public void GetPath_NullSlot_ThrowsArgumentNullException()
        {
            FileSaveStorage storage = new FileSaveStorage(Path.GetTempPath());

            Assert.Throws<ArgumentNullException>(() =>
                storage.GetPath(null, SaveStorageCopy.Primary));
        }

        [Test]
        public void GetTempPath_NullSlot_ThrowsArgumentNullException()
        {
            FileSaveStorage storage = new FileSaveStorage(Path.GetTempPath());

            Assert.Throws<ArgumentNullException>(() =>
                storage.GetTempPath(null));
        }
    }
}