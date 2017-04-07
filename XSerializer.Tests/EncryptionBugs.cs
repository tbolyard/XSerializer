﻿using NUnit.Framework;
using System.Collections.Generic;
using XSerializer.Encryption;
using XSerializer.Tests.Encryption;

namespace XSerializer.Tests
{
    public class EncryptionBugs
    {
        private static readonly IEncryptionMechanism _encryptionMechanism = new EncryptionMarker();
        private static readonly Base64EncryptionMechanism _base64EncryptionMechanism = new Base64EncryptionMechanism();

        public class Foo
        {
            [Encrypt]
            public string Bar { get; set; }
        }
        
        public class Baz
        {
            public Baz()
            {
                Quxes = new List<Qux>();
            }

            public List<Qux> Quxes { get; set; }
        }

        [Encrypt]
        public class Qux
        { 
            public int? Grault { get; set; }
        }

        [Test]
        public void EmptyElementWithPropertyMarkedWithEncryptAttributeDoesNotThrow()
        {
            const string xml = @"<Foo><Bar></Bar></Foo>";

            var serializer = new XmlSerializer<Foo>(x => x
                .WithEncryptionMechanism(_encryptionMechanism)
                .WithEncryptKey(typeof(Foo)));

            Assert.That(() => serializer.Deserialize(xml), Throws.Nothing);
        }

        [TestCase("Boots &amp; cats &amp; boots &amp; cats", "Boots & cats & boots & cats")]
        [TestCase("&quot;Double quotes&quot;", "\"Double quotes\"")]
        [TestCase("One &lt; two", "One < two")]
        [TestCase("Two &gt; one", "Two > one")]
        [TestCase("What&apos;s up?", "What's up?")]
        public void XmlSerializer_PropertyMarkedEncryptContainingEscapedCharsIsDeserialized_CharactersAreUnescaped(string escapedText, string expectedValue)
        {
            var serializer = new XmlSerializer<Foo>(x => x
            .WithEncryptionMechanism(_base64EncryptionMechanism)
            .WithEncryptKey(typeof(Foo)));

            var xml = string.Format("<Foo><Bar>{0}</Bar></Foo>", _base64EncryptionMechanism.Encrypt(escapedText));

            Assert.AreEqual(expectedValue, serializer.Deserialize(xml).Bar);
        }

        [TestCase("Qm9vdHMgJmFtcDsgY2F0cyAmYW1wOyBib290cyAmYW1wOyBjYXRz", "Qm9vdHMgJmFtcDsgY2F0cyAmYW1wOyBib290cyAmYW1wOyBjYXRz")]
        public void XmlSerializer_PropertyMarkedWithEncryptAttributeContainingBase64String_ReturnsBase64String(string escapedText, string expectedValue)
        {
            // No encryption mechanism specified
            var serializer = new XmlSerializer<Foo>();

            var xml = string.Format("<Foo><Bar>{0}</Bar></Foo>", escapedText);

            Assert.AreEqual(expectedValue, serializer.Deserialize(xml).Bar);
        }

        [Test]
        public void ClassMarkedWithEncryptAttributeWithNullablePropertySetToNullDoesNotThrowOnDeserialization()
        {
            var serializer = new XmlSerializer<Baz>(x => x
                .WithEncryptionMechanism(_encryptionMechanism)
                .WithEncryptKey(typeof(Baz)));

            var xml = serializer.Serialize(new Baz { Quxes = { new Qux { Grault = null } } });

            Assert.That(() => serializer.Deserialize(xml), Throws.Nothing);
        }

        public class EncryptionMarker : IEncryptionMechanism
        {
            public string Encrypt(string plainText)
            {
                return "ENCRYPTED(" + plainText
                    .Replace("[", @"\[").Replace('<', '[')
                    .Replace("]", @"\]").Replace('>', ']') + ")";
            }

            string IEncryptionMechanism.Encrypt(string plainText, object encryptKey, SerializationState serializationState)
            {
                return Encrypt(plainText);
            }

            public string Decrypt(string cipherText)
            {
                return cipherText.Substring(10, cipherText.Length - 11)
                    .Replace(@"\[", "!!11@@22##33").Replace('[', '<').Replace("!!11@@22##33", "[")
                    .Replace(@"\]", "!!11@@22##33").Replace(']', '>').Replace("!!11@@22##33", "]");
            }

            string IEncryptionMechanism.Decrypt(string cipherText, object encryptKey, SerializationState serializationState)
            {
                return Decrypt(cipherText);
            }
        }
    }
}
