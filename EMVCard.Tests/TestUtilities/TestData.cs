/*=========================================================================================
'  EMVCard.Tests - Test Data
'  
'  Description: Static test data for EMV card testing scenarios
'==========================================================================================*/

using System;
using System.Collections.Generic;

namespace EMVCard.Tests.TestUtilities
{
    /// <summary>
    /// Contains static test data for various EMV card scenarios
    /// </summary>
    public static class TestData
    {
        /// <summary>
        /// Common EMV Tag definitions for testing
        /// </summary>
        public static class EMVTags
        {
            public const int AID = 0x4F;
            public const int ApplicationLabel = 0x50;
            public const int PAN = 0x5A;
            public const int ExpiryDate = 0x5F24;
            public const int CardholderName = 0x5F20;
            public const int Track2Data = 0x57;
            public const int Track2Equivalent = 0x9F6B;
            public const int PDOL = 0x9F38;
            public const int AFL = 0x94;
            public const int PreferredName = 0x9F12;
            public const int Priority = 0x87;
        }

        /// <summary>
        /// Sample ATR (Answer To Reset) values for different card types
        /// </summary>
        public static class ATRSamples
        {
            public static readonly byte[] VisaContact = {
                0x3B, 0x8F, 0x80, 0x01, 0x80, 0x4F, 0x0C, 0xA0, 0x00, 0x00, 0x00, 0x03,
                0x06, 0x03, 0x00, 0x03, 0x00, 0x00, 0x00, 0x68
            };

            public static readonly byte[] MastercardContact = {
                0x3B, 0x7F, 0x96, 0x00, 0x00, 0x80, 0x31, 0xB8, 0x64, 0x04, 0x85, 0x03,
                0x00, 0x31, 0x80, 0x66, 0xB0, 0x84, 0x0C, 0x01, 0x6E
            };

            public static readonly byte[] UnionPayContact = {
                0x3B, 0x9F, 0x95, 0x81, 0x31, 0xFE, 0x9F, 0x00, 0x65, 0x46, 0x53, 0x05,
                0x30, 0x06, 0x71, 0xDF, 0x00, 0x00, 0x00, 0x00, 0x6A
            };

            public static readonly byte[] ContactlessType4 = {
                0x3B, 0x8F, 0x80, 0x01, 0x80, 0x4F, 0x0C, 0xA0, 0x00, 0x00, 0x00, 0x03,
                0x06, 0x11, 0x00, 0x3B, 0x00, 0x00, 0x00, 0x42
            };
        }

        /// <summary>
        /// Sample AID (Application Identifier) values
        /// </summary>
        public static class AIDSamples
        {
            public const string Visa = "A0000000031010";
            public const string VisaElectron = "A0000000032010";
            public const string VisaPlus = "A0000000038010";
            public const string Mastercard = "A0000000041010";
            public const string MastercardMaestro = "A0000000043060";
            public const string AmericanExpress = "A00000002501";
            public const string UnionPay = "A000000333010101";
            public const string UnionPayCredit = "A000000333010102";
            public const string UnionPayDebit = "A000000333010103";
            public const string DinersClub = "A0000001523010";
            public const string Discover = "A0000001524010";
        }

        /// <summary>
        /// Sample card data for different card types
        /// </summary>
        public static class CardSamples
        {
            public static readonly CardTestData Visa = new CardTestData
            {
                CardType = "Visa",
                PAN = "4532123456789012",
                ExpiryDate = "2025-12",
                CardholderName = "JOHN DOE",
                Track2Data = "4532123456789012D25124567890123456",
                AID = AIDSamples.Visa,
                ApplicationLabel = "VISA CREDIT",
                ATR = ATRSamples.VisaContact
            };

            public static readonly CardTestData Mastercard = new CardTestData
            {
                CardType = "Mastercard",
                PAN = "5555666677778888",
                ExpiryDate = "2024-06",
                CardholderName = "JANE SMITH",
                Track2Data = "5555666677778888D24064567890123456",
                AID = AIDSamples.Mastercard,
                ApplicationLabel = "MASTERCARD",
                ATR = ATRSamples.MastercardContact
            };

            public static readonly CardTestData UnionPay = new CardTestData
            {
                CardType = "UnionPay",
                PAN = "6231871800000762",
                ExpiryDate = "2023-12",
                CardholderName = "LI MING",
                Track2Data = "6231871800000762306D33122203870000000F",
                AID = AIDSamples.UnionPay,
                ApplicationLabel = "UNIONPAY CREDIT",
                ATR = ATRSamples.UnionPayContact
            };

            public static readonly CardTestData AmericanExpress = new CardTestData
            {
                CardType = "American Express",
                PAN = "371449635398431",
                ExpiryDate = "2026-03",
                CardholderName = "BOB JOHNSON",
                Track2Data = "371449635398431D26034567890123456",
                AID = AIDSamples.AmericanExpress,
                ApplicationLabel = "AMEX",
                ATR = ATRSamples.VisaContact // Generic contact ATR
            };
        }

        /// <summary>
        /// Sample APDU commands and responses
        /// </summary>
        public static class APDUSamples
        {
            public static readonly APDUPair SelectPSE = new APDUPair
            {
                Command = "00A4040E31504159.5359532E4444463031",
                Response = "6F2E840E31504159.5359532E4444463031A51C4F07A00000000310105010564953412043524544495487010150084D4153544552"
            };

            public static readonly APDUPair SelectPPSE = new APDUPair
            {
                Command = "00A4040E32504159.5359532E4444463031",
                Response = "6F2E840E32504159.5359532E4444463031A51C4F07A00000000310105010564953412043524544495487010150084D4153544552"
            };

            public static readonly APDUPair SelectVisaApp = new APDUPair
            {
                Command = "00A4040007A0000000031010",
                Response = "6F2A840EA000000003101050084D4153544552907A5F2D02656EA3148801015F2D02656E9F1101019F120A4D617374657243617264"
            };

            public static readonly APDUPair GPOSimple = new APDUPair
            {
                Command = "80A80000028300",
                Response = "80069F360200019F2608123456789ABCDEF09F2701809F1007123456789ABCDEF09F37049ABCDEF09000"
            };

            public static readonly APDUPair ReadRecord1_1 = new APDUPair
            {
                Command = "00B2010C00",
                Response = "70125A084532123456789012F5F24032512"
            };

            public static readonly APDUPair RecordNotFound = new APDUPair
            {
                Command = "00B2030C00",
                Response = "6A83"
            };
        }

        /// <summary>
        /// Sample TLV data for parsing tests
        /// </summary>
        public static class TLVSamples
        {
            // Simple TLV: Tag 5A (PAN), Length 8, Value (card number)
            public static readonly byte[] SimplePAN = {
                0x5A, 0x08, 0x45, 0x32, 0x12, 0x34, 0x56, 0x78, 0x90, 0x12
            };

            // Two-byte tag: 9F38 (PDOL)
            public static readonly byte[] PDOL = {
                0x9F, 0x38, 0x06, 0x9F, 0x66, 0x04, 0x9F, 0x02, 0x06, 0x9F, 0x03, 0x06
            };

            // Long form length: Tag 70, Length 129 bytes (0x81, 0x81)
            public static readonly byte[] LongFormLength = new byte[] {
                0x70, 0x81, 0x81 // Tag 70, Length encoding (129 bytes follow)
            }.Concat(new byte[129]).ToArray(); // Followed by 129 bytes of data

            // Nested TLV structure
            public static readonly byte[] NestedTLV = {
                0x70, 0x10, // Template tag, length 16
                0x5A, 0x08, 0x45, 0x32, 0x12, 0x34, 0x56, 0x78, 0x90, 0x12, // PAN
                0x5F, 0x24, 0x03, 0x25, 0x12, 0x31 // Expiry date
            };
        }

        /// <summary>
        /// Test scenarios for Track 2 parsing
        /// </summary>
        public static class Track2Samples
        {
            public static readonly Track2TestData StandardVisa = new Track2TestData
            {
                Track2Data = "4532123456789012D25124567890123456",
                ExpectedPAN = "4532123456789012",
                ExpectedExpiry = "2025-12",
                Separator = 'D'
            };

            public static readonly Track2TestData WithEqualsSeparator = new Track2TestData
            {
                Track2Data = "4532123456789012=25124567890123456",
                ExpectedPAN = "4532123456789012",
                ExpectedExpiry = "2025-12",
                Separator = '='
            };

            public static readonly Track2TestData UnionPaySpecial = new Track2TestData
            {
                Track2Data = "6231871800000762306D33122203870000000F",
                ExpectedPAN = "6231871800000762",
                ExpectedExpiry = null, // Special parsing required
                Separator = 'D'
            };

            public static readonly Track2TestData WithPadding = new Track2TestData
            {
                Track2Data = "453212345678901FD25124567890123456",
                ExpectedPAN = "453212345678901", // F padding removed
                ExpectedExpiry = "2025-12",
                Separator = 'D'
            };

            public static readonly Track2TestData FixedFormat = new Track2TestData
            {
                Track2Data = "453212345678901225124567890123456789", // No separator
                ExpectedPAN = "4532123456789012",
                ExpectedExpiry = "2025-12",
                Separator = '\0' // No separator
            };
        }

        /// <summary>
        /// Error scenarios for testing
        /// </summary>
        public static class ErrorScenarios
        {
            public static readonly ErrorTestData CardNotPresent = new ErrorTestData
            {
                Description = "Card not present",
                Command = "00A4040007A0000000031010",
                ExpectedSW1 = 0x6F,
                ExpectedSW2 = 0x00
            };

            public static readonly ErrorTestData InstructionNotSupported = new ErrorTestData
            {
                Description = "Instruction not supported",
                Command = "FF000000",
                ExpectedSW1 = 0x6D,
                ExpectedSW2 = 0x00
            };

            public static readonly ErrorTestData FileNotFound = new ErrorTestData
            {
                Description = "File not found",
                Command = "00A4040007A0000000999999",
                ExpectedSW1 = 0x6A,
                ExpectedSW2 = 0x82
            };

            public static readonly ErrorTestData RecordNotFound = new ErrorTestData
            {
                Description = "Record not found",
                Command = "00B2FF0C00",
                ExpectedSW1 = 0x6A,
                ExpectedSW2 = 0x83
            };

            public static readonly ErrorTestData WrongLength = new ErrorTestData
            {
                Description = "Wrong length",
                Command = "00A404000FA000000003101099", // Wrong Lc
                ExpectedSW1 = 0x6C,
                ExpectedSW2 = 0x07 // Correct length
            };
        }

        /// <summary>
        /// Performance test data
        /// </summary>
        public static class PerformanceTestData
        {
            public const int SmallBufferSize = 256;
            public const int MediumBufferSize = 1024;
            public const int LargeBufferSize = 4096;
            public const int VeryLargeBufferSize = 64 * 1024; // 64KB

            public const int SmallIterationCount = 100;
            public const int MediumIterationCount = 1000;
            public const int LargeIterationCount = 10000;

            public static readonly string SmallHexString = string.Join(" ", 
                Enumerable.Range(0, 32).Select(i => i.ToString("X2")));

            public static readonly string LargeHexString = string.Join(" ", 
                Enumerable.Range(0, 512).Select(i => (i % 256).ToString("X2")));
        }

        #region Helper Classes

        public class CardTestData
        {
            public string CardType { get; set; }
            public string PAN { get; set; }
            public string ExpiryDate { get; set; }
            public string CardholderName { get; set; }
            public string Track2Data { get; set; }
            public string AID { get; set; }
            public string ApplicationLabel { get; set; }
            public byte[] ATR { get; set; }
        }

        public class APDUPair
        {
            public string Command { get; set; }
            public string Response { get; set; }
        }

        public class Track2TestData
        {
            public string Track2Data { get; set; }
            public string ExpectedPAN { get; set; }
            public string ExpectedExpiry { get; set; }
            public char Separator { get; set; }
        }

        public class ErrorTestData
        {
            public string Description { get; set; }
            public string Command { get; set; }
            public byte ExpectedSW1 { get; set; }
            public byte ExpectedSW2 { get; set; }
        }

        #endregion

        #region Extension Methods

        public static byte[] HexStringToBytes(this string hexString)
        {
            hexString = hexString.Replace(" ", "").Replace(".", "");
            return Enumerable.Range(0, hexString.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hexString.Substring(x, 2), 16))
                .ToArray();
        }

        public static string BytesToHexString(this byte[] bytes, bool includeSpaces = true)
        {
            var hex = BitConverter.ToString(bytes);
            return includeSpaces ? hex.Replace("-", " ") : hex.Replace("-", "");
        }

        #endregion
    }
}