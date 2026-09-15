using System;
using NUnit.Framework;

namespace CDG.Save.Tests.Runtime
{
    public class SaveSlotTests
    {
        [Test]
        public void Constructor_ValidName_StoresName()
        {
            SaveSlot slot = new SaveSlot("slot-01");

            Assert.AreEqual("slot-01", slot.Name);
        }

        [Test]
        public void Default_HasDefaultName()
        {
            Assert.AreEqual("default", SaveSlot.Default.Name);
        }

        [TestCase("a")]
        [TestCase("slot")]
        [TestCase("slot01")]
        [TestCase("slot-01")]
        [TestCase("slot_01")]
        [TestCase("0")]
        public void Constructor_AllowedCharacters_DoesNotThrow(string name)
        {
            Assert.DoesNotThrow(() => new SaveSlot(name));
        }

        [Test]
        public void Constructor_MaxLengthName_DoesNotThrow()
        {
            string name = new string('a', SaveSlot.MaxLength);

            Assert.DoesNotThrow(() => new SaveSlot(name));
        }

        [Test]
        public void Constructor_OverMaxLength_ThrowsArgumentException()
        {
            string name = new string('a', SaveSlot.MaxLength + 1);

            Assert.Throws<ArgumentException>(() => new SaveSlot(name));
        }

        [TestCase(null)]
        [TestCase("")]
        public void Constructor_EmptyName_ThrowsArgumentException(string name)
        {
            Assert.Throws<ArgumentException>(() => new SaveSlot(name));
        }

        [TestCase("Slot")]
        [TestCase("SLOT")]
        [TestCase("slot 01")]
        [TestCase("slot.01")]
        [TestCase("slot/01")]
        [TestCase("slot\\01")]
        [TestCase("../slot")]
        [TestCase("slot01!")]
        [TestCase("슬롯")]
        public void Constructor_InvalidCharacter_ThrowsArgumentException(string name)
        {
            Assert.Throws<ArgumentException>(() => new SaveSlot(name));
        }

        [Test]
        public void Equals_SameName_ReturnsTrue()
        {
            SaveSlot first = new SaveSlot("slot-01");
            SaveSlot second = new SaveSlot("slot-01");

            Assert.IsTrue(first.Equals(second));
            Assert.IsTrue(first == second);
            Assert.IsFalse(first != second);
        }

        [Test]
        public void Equals_DifferentName_ReturnsFalse()
        {
            SaveSlot first = new SaveSlot("slot-01");
            SaveSlot second = new SaveSlot("slot-02");

            Assert.IsFalse(first.Equals(second));
            Assert.IsFalse(first == second);
            Assert.IsTrue(first != second);
        }

        [Test]
        public void GetHashCode_SameName_ReturnsSameHashCode()
        {
            SaveSlot first = new SaveSlot("slot-01");
            SaveSlot second = new SaveSlot("slot-01");

            Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
        }

        [Test]
        public void ToString_ReturnsName()
        {
            SaveSlot slot = new SaveSlot("slot-01");

            Assert.AreEqual("slot-01", slot.ToString());
        }
    }
}