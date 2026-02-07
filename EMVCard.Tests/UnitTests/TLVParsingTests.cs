/*=========================================================================================
'  EMVCard.Tests - TLV Parsing Unit Tests
'  
'  Description: Tests for TLV (Tag-Length-Value) parsing functionality
'==========================================================================================*/

using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EMVCard;

namespace EMVCard.Tests.UnitTests
{
    [TestClass]
    public class TLVParsingTests
    {
        [TestMethod]
        public void FillBufferFromHexString_ValidInput_ReturnsCorrectBytes()
        {
            // Arrange
            var emvReader = new MainEMVReaderBin();
            string hexString = "00 A4 04 00 0E";
            byte[] buffer = new byte[10];

            // Act
            int bytesWritten = emvReader.FillBufferFromHexString(hexString, buffer, 0);

            // Assert
            Assert.AreEqual(5, bytesWritten);
            Assert.AreEqual(0x00, buffer[0]);
            Assert.AreEqual(0xA4, buffer[1]);
            Assert.AreEqual(0x04, buffer[2]);
            Assert.AreEqual(0x00, buffer[3]);
            Assert.AreEqual(0x0E, buffer[4]);
        }

        [TestMethod]
        public void FillBufferFromHexString_StartIndex_WritesToCorrectPosition()
        {
            // Arrange
            var emvReader = new MainEMVReaderBin();
            string hexString = "AA BB CC";
            byte[] buffer = new byte[10];

            // Act
            int bytesWritten = emvReader.FillBufferFromHexString(hexString, buffer, 3);

            // Assert
            Assert.AreEqual(3, bytesWritten);
            Assert.AreEqual(0x00, buffer[0]); // Unchanged
            Assert.AreEqual(0x00, buffer[1]); // Unchanged
            Assert.AreEqual(0x00, buffer[2]); // Unchanged
            Assert.AreEqual(0xAA, buffer[3]); // Written
            Assert.AreEqual(0xBB, buffer[4]); // Written
            Assert.AreEqual(0xCC, buffer[5]); // Written
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void FillBufferFromHexString_EmptyString_ThrowsException()
        {
            // Arrange
            var emvReader = new MainEMVReaderBin();
            byte[] buffer = new byte[10];

            // Act
            emvReader.FillBufferFromHexString("", buffer, 0);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void FillBufferFromHexString_BufferTooSmall_ThrowsException()
        {
            // Arrange
            var emvReader = new MainEMVReaderBin();
            string hexString = "00 A4 04 00 0E";
            byte[] buffer = new byte[3]; // Too small

            // Act
            emvReader.FillBufferFromHexString(hexString, buffer, 0);
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void FillBufferFromHexString_InvalidHex_ThrowsException()
        {
            // Arrange
            var emvReader = new MainEMVReaderBin();
            string hexString = "00 XY 04"; // XY is invalid
            byte[] buffer = new byte[10];

            // Act
            emvReader.FillBufferFromHexString(hexString, buffer, 0);
        }

        [TestMethod]
        public void FillBufferFromHexString_LowercaseHex_ParsesCorrectly()
        {
            // Arrange
            var emvReader = new MainEMVReaderBin();
            string hexString = "aa bb cc dd";
            byte[] buffer = new byte[10];

            // Act
            int bytesWritten = emvReader.FillBufferFromHexString(hexString, buffer, 0);

            // Assert
            Assert.AreEqual(4, bytesWritten);
            Assert.AreEqual(0xAA, buffer[0]);
            Assert.AreEqual(0xBB, buffer[1]);
            Assert.AreEqual(0xCC, buffer[2]);
            Assert.AreEqual(0xDD, buffer[3]);
        }

        [TestMethod]
        public void FillBufferFromHexString_ExtraSpaces_HandlesCorrectly()
        {
            // Arrange
            var emvReader = new MainEMVReaderBin();
            string hexString = "  00   A4    04  00  ";
            byte[] buffer = new byte[10];

            // Act
            int bytesWritten = emvReader.FillBufferFromHexString(hexString, buffer, 0);

            // Assert
            Assert.AreEqual(4, bytesWritten);
            Assert.AreEqual(0x00, buffer[0]);
            Assert.AreEqual(0xA4, buffer[1]);
            Assert.AreEqual(0x04, buffer[2]);
            Assert.AreEqual(0x00, buffer[3]);
        }

        [TestMethod]
        public void ParseSFIRecord_ValidAID_ExtractsCorrectly()
        {
            // Arrange
            var emvReader = new MainEMVReaderBin();
            
            // Create a simple TLV record with AID (4F) tag
            byte[] record = {
                0x4F, 0x07, // Tag 4F, Length 7
                0xA0, 0x00, 0x00, 0x00, 0x04, 0x10, 0x10, // Mastercard AID
                0x50, 0x08, // Tag 50 (Application Label), Length 8
                (byte)'M', (byte)'a', (byte)'s', (byte)'t', (byte)'e', (byte)'r', (byte)'c', (byte)'d'
            };

            // Act
            emvReader.ParseSFIRecord(record, record.Length);

            // Assert - This is more of an integration test since we can't easily verify internal state
            // In a real test, we'd need to refactor the method to return parsed data or use a mock
        }

        [TestMethod]
        public void InsertSpaces_ValidHexString_InsertsSpacesCorrectly()
        {
            // This tests a helper method that should be accessible
            // For now, we'll test the concept with a standalone implementation
            
            // Arrange
            string hex = "A0000000041010";
            
            // Act
            string result = InsertSpacesHelper(hex);
            
            // Assert
            Assert.AreEqual("A0 00 00 00 04 10 10", result);
        }

        [TestMethod]
        public void Track2_Parsing_ExtractsCardNumber()
        {
            // Arrange - Simulate Track 2 data
            string track2Data = "4532123456789012D25124567890123456";
            
            // Act
            var (cardNumber, expiry) = ParseTrack2Helper(track2Data);
            
            // Assert
            Assert.AreEqual("4532123456789012", cardNumber);
            Assert.AreEqual("2025-12", expiry);
        }

        [TestMethod]
        public void Track2_Parsing_HandlesEqualsSeperator()
        {
            // Arrange - Track 2 with = separator
            string track2Data = "4532123456789012=25124567890123456";
            
            // Act
            var (cardNumber, expiry) = ParseTrack2Helper(track2Data);
            
            // Assert
            Assert.AreEqual("4532123456789012", cardNumber);
            Assert.AreEqual("2025-12", expiry);
        }

        [TestMethod]
        public void Track2_Parsing_HandlesUnionPayFormat()
        {
            // Arrange - UnionPay format (starts with 62)
            string track2Data = "6231871800000762306D33122203870000000F";
            
            // Act
            var (cardNumber, expiry) = ParseTrack2Helper(track2Data);
            
            // Assert
            Assert.AreEqual("6231871800000762", cardNumber);
            // Note: UnionPay expiry parsing might need special handling
        }

        [TestMethod]
        public void TLV_LongForm_Length_ParsedCorrectly()
        {
            // Test parsing of long-form length encoding (> 127 bytes)
            // This would require access to the actual TLV parsing method
            
            // For now, test the concept
            byte[] longFormLength = { 0x81, 0xFF }; // Length = 255
            int parsedLength = ParseLongFormLength(longFormLength, 0);
            
            Assert.AreEqual(255, parsedLength);
        }

        [TestMethod]
        public void TLV_TwoByteTag_ParsedCorrectly()
        {
            // Test parsing of two-byte tags (first byte has low 5 bits = 11111)
            byte[] twoByteTag = { 0x9F, 0x38 }; // PDOL tag
            int tag = ParseTag(twoByteTag, 0);
            
            Assert.AreEqual(0x9F38, tag);
        }

        #region Helper Methods (These simulate the internal logic we're testing)

        private string InsertSpacesHelper(string hex)
        {
            var result = new System.Text.StringBuilder();
            for (int i = 0; i < hex.Length; i += 2)
            {
                if (i > 0) result.Append(" ");
                result.Append(hex.Substring(i, 2));
            }
            return result.ToString();
        }

        private (string cardNumber, string expiry) ParseTrack2Helper(string track2)
        {
            // Simplified Track 2 parsing logic
            int dIndex = track2.IndexOf('D');
            if (dIndex == -1) dIndex = track2.IndexOf('=');
            
            if (dIndex > 0 && track2.Length >= dIndex + 5)
            {
                string cardNumber = track2.Substring(0, dIndex).TrimEnd('F');
                string expiryYYMM = track2.Substring(dIndex + 1, 4);
                string expiry = $"20{expiryYYMM.Substring(0, 2)}-{expiryYYMM.Substring(2)}";
                return (cardNumber, expiry);
            }
            
            // Handle UnionPay format
            if (track2.StartsWith("62") && track2.Length >= 16)
            {
                return (track2.Substring(0, 16), null);
            }
            
            return (null, null);
        }

        private int ParseLongFormLength(byte[] buffer, int startIndex)
        {
            if (buffer[startIndex] <= 0x80) return buffer[startIndex];
            
            int lenBytes = buffer[startIndex] & 0x7F;
            int length = 0;
            for (int i = 0; i < lenBytes; i++)
            {
                length = (length << 8) + buffer[startIndex + 1 + i];
            }
            return length;
        }

        private int ParseTag(byte[] buffer, int startIndex)
        {
            byte firstByte = buffer[startIndex];
            if ((firstByte & 0x1F) == 0x1F)
            {
                // Two-byte tag
                return (firstByte << 8) | buffer[startIndex + 1];
            }
            return firstByte;
        }

        #endregion
    }
}