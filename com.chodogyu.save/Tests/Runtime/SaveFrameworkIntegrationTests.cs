using System;
using System.IO;
using CDG.Core.Results;
using CDG.Save.Serialization;
using CDG.Save.Storage;
using NUnit.Framework;

namespace CDG.Save.Tests.Runtime
{
    public class SaveFrameworkIntegrationTests
    {
        private string rootPath;
        private FileSaveStorage storage;
        private SaveService service;
        private SaveSlot slot;

        [SetUp]
        public void SetUp()
        {
            rootPath = Path.Combine(
                Path.GetTempPath(),
                "cdg-save-integration-tests",
                Guid.NewGuid().ToString("N"));

            storage = new FileSaveStorage(rootPath);
            service = new SaveService(storage, new JsonUtilitySaveSerializer());
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
        public void SaveAndLoad_ValidData_RoundTripsThroughPrimary()
        {
            TestSaveData original = CreateSaveData("플레이어", 12, 3, 7, 9);

            Result saveResult = service.Save(slot, original);
            Result<LoadResult<TestSaveData>> loadResult = service.Load<TestSaveData>(slot);

            Assert.IsTrue(saveResult.IsSuccess);
            Assert.IsTrue(loadResult.IsSuccess);
            Assert.AreEqual(SaveLoadStatus.LoadedFromPrimary, loadResult.Value.Status);

            AssertSaveData(
                loadResult.Value.Value,
                "플레이어",
                12,
                new[] { 3, 7, 9 });
        }

        [Test]
        public void Save_ValidData_CreatesMatchingPrimaryAndBackupFiles()
        {
            TestSaveData data = CreateSaveData("player", 5, 1, 2, 3);

            Result result = service.Save(slot, data);

            string primaryPath = storage.GetPath(slot, SaveStorageCopy.Primary);
            string backupPath = storage.GetPath(slot, SaveStorageCopy.Backup);

            Assert.IsTrue(result.IsSuccess);
            Assert.IsTrue(File.Exists(primaryPath));
            Assert.IsTrue(File.Exists(backupPath));

            CollectionAssert.AreEqual(
                File.ReadAllBytes(primaryPath),
                File.ReadAllBytes(backupPath));

            Assert.IsFalse(File.Exists(storage.GetTempPath(slot)));
        }

        [Test]
        public void Load_PrimaryMissing_RecoversFromBackup()
        {
            TestSaveData data = CreateSaveData("backup-player", 8, 4, 5);

            Result saveResult = service.Save(slot, data);

            Assert.IsTrue(saveResult.IsSuccess);

            File.Delete(storage.GetPath(slot, SaveStorageCopy.Primary));

            Result<LoadResult<TestSaveData>> loadResult = service.Load<TestSaveData>(slot);

            Assert.IsTrue(loadResult.IsSuccess);
            Assert.AreEqual(SaveLoadStatus.LoadedFromBackup, loadResult.Value.Status);

            AssertSaveData(
                loadResult.Value.Value,
                "backup-player",
                8,
                new[] { 4, 5 });
        }

        [Test]
        public void Load_PrimaryCorrupted_RecoversFromBackup()
        {
            TestSaveData data = CreateSaveData("recovery-player", 15, 10, 20);

            Result saveResult = service.Save(slot, data);

            Assert.IsTrue(saveResult.IsSuccess);

            File.WriteAllBytes(
                storage.GetPath(slot, SaveStorageCopy.Primary),
                new byte[] { 0xFF, 0xFF });

            Result<LoadResult<TestSaveData>> loadResult = service.Load<TestSaveData>(slot);

            Assert.IsTrue(loadResult.IsSuccess);
            Assert.AreEqual(SaveLoadStatus.LoadedFromBackup, loadResult.Value.Status);

            AssertSaveData(
                loadResult.Value.Value,
                "recovery-player",
                15,
                new[] { 10, 20 });
        }

        [Test]
        public void Load_PrimaryAndBackupCorrupted_ReturnsCorruptedData()
        {
            Result saveResult = service.Save(
                slot,
                CreateSaveData("player", 1, 1));

            Assert.IsTrue(saveResult.IsSuccess);

            byte[] corruptedData = { 0xFF, 0xFF };

            File.WriteAllBytes(
                storage.GetPath(slot, SaveStorageCopy.Primary),
                corruptedData);

            File.WriteAllBytes(
                storage.GetPath(slot, SaveStorageCopy.Backup),
                corruptedData);

            Result<LoadResult<TestSaveData>> loadResult = service.Load<TestSaveData>(slot);

            Assert.IsTrue(loadResult.IsFailure);
            Assert.AreEqual(SaveErrorCodes.CorruptedData, loadResult.Error.Code);
        }

        [Test]
        public void Delete_AfterSave_RemovesBothCopiesAndLoadReturnsNotFound()
        {
            Result saveResult = service.Save(
                slot,
                CreateSaveData("player", 3, 1, 2));

            Assert.IsTrue(saveResult.IsSuccess);

            Result<bool> existsBeforeDelete = service.Exists(slot);
            Result deleteResult = service.Delete(slot);
            Result<bool> existsAfterDelete = service.Exists(slot);
            Result<LoadResult<TestSaveData>> loadResult = service.Load<TestSaveData>(slot);

            Assert.IsTrue(existsBeforeDelete.IsSuccess);
            Assert.IsTrue(existsBeforeDelete.Value);

            Assert.IsTrue(deleteResult.IsSuccess);

            Assert.IsTrue(existsAfterDelete.IsSuccess);
            Assert.IsFalse(existsAfterDelete.Value);

            Assert.IsFalse(File.Exists(storage.GetPath(slot, SaveStorageCopy.Primary)));
            Assert.IsFalse(File.Exists(storage.GetPath(slot, SaveStorageCopy.Backup)));

            Assert.IsTrue(loadResult.IsSuccess);
            Assert.AreEqual(SaveLoadStatus.NotFound, loadResult.Value.Status);
            Assert.IsFalse(loadResult.Value.IsFound);
        }

        [Test]
        public void Save_ExistingSlot_UpdatesBothCopiesWithLatestData()
        {
            Result firstSaveResult = service.Save(
                slot,
                CreateSaveData("first", 1, 1));

            Result secondSaveResult = service.Save(
                slot,
                CreateSaveData("second", 99, 7, 8, 9));

            Assert.IsTrue(firstSaveResult.IsSuccess);
            Assert.IsTrue(secondSaveResult.IsSuccess);

            string primaryPath = storage.GetPath(slot, SaveStorageCopy.Primary);
            string backupPath = storage.GetPath(slot, SaveStorageCopy.Backup);

            CollectionAssert.AreEqual(
                File.ReadAllBytes(primaryPath),
                File.ReadAllBytes(backupPath));

            Result<LoadResult<TestSaveData>> loadResult = service.Load<TestSaveData>(slot);

            Assert.IsTrue(loadResult.IsSuccess);
            Assert.AreEqual(SaveLoadStatus.LoadedFromPrimary, loadResult.Value.Status);

            AssertSaveData(
                loadResult.Value.Value,
                "second",
                99,
                new[] { 7, 8, 9 });
        }

        [Test]
        public void SeparateSlots_KeepDataIndependent()
        {
            SaveSlot firstSlot = new SaveSlot("slot-a");
            SaveSlot secondSlot = new SaveSlot("slot-b");

            Result firstSaveResult = service.Save(
                firstSlot,
                CreateSaveData("first", 10, 1));

            Result secondSaveResult = service.Save(
                secondSlot,
                CreateSaveData("second", 20, 2));

            Assert.IsTrue(firstSaveResult.IsSuccess);
            Assert.IsTrue(secondSaveResult.IsSuccess);

            Result<LoadResult<TestSaveData>> firstLoadResult =
                service.Load<TestSaveData>(firstSlot);

            Result<LoadResult<TestSaveData>> secondLoadResult =
                service.Load<TestSaveData>(secondSlot);

            Assert.IsTrue(firstLoadResult.IsSuccess);
            Assert.IsTrue(secondLoadResult.IsSuccess);

            AssertSaveData(
                firstLoadResult.Value.Value,
                "first",
                10,
                new[] { 1 });

            AssertSaveData(
                secondLoadResult.Value.Value,
                "second",
                20,
                new[] { 2 });
        }

        private static TestSaveData CreateSaveData(string playerName, int level, params int[] itemIds)
        {
            return new TestSaveData
            {
                PlayerName = playerName,
                Level = level,
                ItemIds = itemIds
            };
        }

        private static void AssertSaveData(TestSaveData actual, string expectedPlayerName, int expectedLevel, int[] expectedItemIds)
        {
            Assert.AreEqual(expectedPlayerName, actual.PlayerName);
            Assert.AreEqual(expectedLevel, actual.Level);
            CollectionAssert.AreEqual(expectedItemIds, actual.ItemIds);
        }

        [Serializable]
        private sealed class TestSaveData
        {
            public string PlayerName;
            public int Level;
            public int[] ItemIds;
        }
    }
}