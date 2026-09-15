using System;
using NUnit.Framework;

namespace CDG.Save.Tests.Runtime
{
    public class LoadResultTests
    {
        [Test]
        public void NotFound_HasNotFoundStatus()
        {
            LoadResult<string> result = LoadResult<string>.NotFound();

            Assert.AreEqual(SaveLoadStatus.NotFound, result.Status);
            Assert.IsFalse(result.IsFound);
        }

        [Test]
        public void NotFound_ValueAccess_ThrowsInvalidOperationException()
        {
            LoadResult<string> result = LoadResult<string>.NotFound();

            Assert.Throws<InvalidOperationException>(() =>
            {
                string value = result.Value;
            });
        }

        [Test]
        public void FromPrimary_HasPrimaryStatusAndValue()
        {
            LoadResult<string> result = LoadResult<string>.FromPrimary("primary-data");

            Assert.AreEqual(SaveLoadStatus.LoadedFromPrimary, result.Status);
            Assert.IsTrue(result.IsFound);
            Assert.AreEqual("primary-data", result.Value);
        }

        [Test]
        public void FromBackup_HasBackupStatusAndValue()
        {
            LoadResult<string> result = LoadResult<string>.FromBackup("backup-data");

            Assert.AreEqual(SaveLoadStatus.LoadedFromBackup, result.Status);
            Assert.IsTrue(result.IsFound);
            Assert.AreEqual("backup-data", result.Value);
        }

        [Test]
        public void FromPrimary_DefaultValueType_IsStillFound()
        {
            LoadResult<int> result = LoadResult<int>.FromPrimary(0);

            Assert.AreEqual(SaveLoadStatus.LoadedFromPrimary, result.Status);
            Assert.IsTrue(result.IsFound);
            Assert.AreEqual(0, result.Value);
        }

        [Test]
        public void FromPrimary_NullReferenceValue_IsStillFound()
        {
            LoadResult<string> result = LoadResult<string>.FromPrimary(null);

            Assert.AreEqual(SaveLoadStatus.LoadedFromPrimary, result.Status);
            Assert.IsTrue(result.IsFound);
            Assert.IsNull(result.Value);
        }

        [Test]
        public void FromBackup_NullReferenceValue_IsStillFound()
        {
            LoadResult<string> result = LoadResult<string>.FromBackup(null);

            Assert.AreEqual(SaveLoadStatus.LoadedFromBackup, result.Status);
            Assert.IsTrue(result.IsFound);
            Assert.IsNull(result.Value);
        }
    }
}