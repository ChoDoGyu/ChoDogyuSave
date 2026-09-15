using System;
using System.Text;
using CDG.Core.Results;
using CDG.Save.Serialization;
using NUnit.Framework;

namespace CDG.Save.Tests.Runtime.Serialization
{
    public class JsonUtilitySaveSerializerTests
    {
        private JsonUtilitySaveSerializer serializer;

        [SetUp]
        public void SetUp()
        {
            serializer = new JsonUtilitySaveSerializer();
        }

        [Test]
        public void Serialize_ValidData_ReturnsSuccessWithBytes()
        {
            TestSaveData data = CreateTestData();

            Result<byte[]> result = serializer.Serialize(data);

            Assert.IsTrue(result.IsSuccess);
            Assert.IsNotNull(result.Value);
            Assert.IsNotEmpty(result.Value);
        }

        [Test]
        public void Deserialize_SerializedData_RestoresOriginalValues()
        {
            TestSaveData original = CreateTestData();

            Result<byte[]> serializeResult = serializer.Serialize(original);
            Result<TestSaveData> deserializeResult = serializer.Deserialize<TestSaveData>(serializeResult.Value);

            Assert.IsTrue(serializeResult.IsSuccess);
            Assert.IsTrue(deserializeResult.IsSuccess);
            Assert.AreEqual(original.PlayerName, deserializeResult.Value.PlayerName);
            Assert.AreEqual(original.Level, deserializeResult.Value.Level);
            CollectionAssert.AreEqual(original.ItemIds, deserializeResult.Value.ItemIds);
        }

        [Test]
        public void Serialize_ValidData_ProducesUtf8Json()
        {
            TestSaveData data = CreateTestData();

            Result<byte[]> result = serializer.Serialize(data);
            string json = Encoding.UTF8.GetString(result.Value);

            Assert.IsTrue(result.IsSuccess);
            StringAssert.Contains("\"PlayerName\":\"player\"", json);
            StringAssert.Contains("\"Level\":10", json);
        }

        [Test]
        public void Serialize_NullData_ReturnsInvalidDataFailure()
        {
            Result<byte[]> result = serializer.Serialize<TestSaveData>(null);

            Assert.IsTrue(result.IsFailure);
            Assert.AreEqual(SaveErrorCodes.InvalidData, result.Error.Code);
        }

        [Test]
        public void Deserialize_NullData_ReturnsInvalidDataFailure()
        {
            Result<TestSaveData> result = serializer.Deserialize<TestSaveData>(null);

            Assert.IsTrue(result.IsFailure);
            Assert.AreEqual(SaveErrorCodes.InvalidData, result.Error.Code);
        }

        [Test]
        public void Deserialize_EmptyData_ReturnsInvalidDataFailure()
        {
            Result<TestSaveData> result = serializer.Deserialize<TestSaveData>(Array.Empty<byte>());

            Assert.IsTrue(result.IsFailure);
            Assert.AreEqual(SaveErrorCodes.InvalidData, result.Error.Code);
        }

        [Test]
        public void Deserialize_InvalidUtf8_ReturnsDeserializationFailed()
        {
            byte[] invalidUtf8 = { 0xC3, 0x28 };

            Result<TestSaveData> result = serializer.Deserialize<TestSaveData>(invalidUtf8);

            Assert.IsTrue(result.IsFailure);
            Assert.AreEqual(SaveErrorCodes.DeserializationFailed, result.Error.Code);
        }

        [Test]
        public void Deserialize_InvalidJson_ReturnsDeserializationFailed()
        {
            byte[] invalidJson = Encoding.UTF8.GetBytes("{ invalid json }");

            Result<TestSaveData> result = serializer.Deserialize<TestSaveData>(invalidJson);

            Assert.IsTrue(result.IsFailure);
            Assert.AreEqual(SaveErrorCodes.DeserializationFailed, result.Error.Code);
        }

        private static TestSaveData CreateTestData()
        {
            return new TestSaveData
            {
                PlayerName = "player",
                Level = 10,
                ItemIds = new[] { 101, 202, 303 }
            };
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