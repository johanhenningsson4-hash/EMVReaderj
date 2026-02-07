/*=========================================================================================
'  EMVCard.Tests - Mock Objects
'  
'  Description: Mock objects for testing without hardware dependencies
'==========================================================================================*/

using System;
using System.Collections.Generic;
using System.Linq;

namespace EMVCard.Tests.TestUtilities
{
    /// <summary>
    /// Mock smart card reader for testing
    /// </summary>
    public class MockCardReader
    {
        public string Name { get; set; }
        public bool IsConnected { get; private set; }
        public MockCard InsertedCard { get; set; }
        public List<MockAPDUResponse> ResponseQueue { get; private set; }

        public MockCardReader(string name)
        {
            Name = name;
            ResponseQueue = new List<MockAPDUResponse>();
        }

        public bool Connect()
        {
            IsConnected = InsertedCard != null;
            return IsConnected;
        }

        public void Disconnect()
        {
            IsConnected = false;
        }

        public byte[] SendCommand(byte[] command)
        {
            if (!IsConnected || InsertedCard == null)
                return new byte[] { 0x6F, 0x00 }; // Card not present

            var commandHex = BitConverter.ToString(command).Replace("-", "");
            var response = ResponseQueue.FirstOrDefault(r => r.Command.Equals(commandHex, StringComparison.OrdinalIgnoreCase));
            
            if (response != null)
            {
                ResponseQueue.Remove(response);
                return HexStringToBytes(response.Response);
            }

            // Default response for unknown commands
            return new byte[] { 0x6D, 0x00 }; // Instruction not supported
        }

        public void QueueResponse(string command, string response)
        {
            ResponseQueue.Add(new MockAPDUResponse { Command = command, Response = response });
        }

        private byte[] HexStringToBytes(string hex)
        {
            hex = hex.Replace(" ", "");
            return Enumerable.Range(0, hex.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                .ToArray();
        }
    }

    /// <summary>
    /// Mock EMV card with predefined data
    /// </summary>
    public class MockCard
    {
        public string CardType { get; set; }
        public string PAN { get; set; }
        public string ExpiryDate { get; set; }
        public string CardholderName { get; set; }
        public string Track2Data { get; set; }
        public List<MockApplication> Applications { get; set; }
        public byte[] ATR { get; set; }

        public MockCard()
        {
            Applications = new List<MockApplication>();
        }

        public static MockCard CreateVisaCard()
        {
            return new MockCard
            {
                CardType = "Visa",
                PAN = "4532123456789012",
                ExpiryDate = "2025-12",
                CardholderName = "JOHN DOE",
                Track2Data = "4532123456789012D25124567890123456",
                ATR = new byte[] { 0x3B, 0x8F, 0x80, 0x01, 0x80, 0x4F, 0x0C, 0xA0, 0x00, 0x00, 0x00, 0x03, 0x06, 0x03, 0x00, 0x03, 0x00, 0x00, 0x00, 0x68 },
                Applications = new List<MockApplication>
                {
                    new MockApplication
                    {
                        AID = "A0000000031010",
                        Label = "VISA CREDIT",
                        Priority = 1
                    }
                }
            };
        }

        public static MockCard CreateMastercardCard()
        {
            return new MockCard
            {
                CardType = "Mastercard",
                PAN = "5555666677778888",
                ExpiryDate = "2024-06",
                CardholderName = "JANE SMITH",
                Track2Data = "5555666677778888D24064567890123456",
                ATR = new byte[] { 0x3B, 0x7F, 0x96, 0x00, 0x00, 0x80, 0x31, 0xB8, 0x64, 0x04, 0x85, 0x03, 0x00, 0x31, 0x80, 0x66, 0xB0, 0x84, 0x0C, 0x01, 0x6E },
                Applications = new List<MockApplication>
                {
                    new MockApplication
                    {
                        AID = "A0000000041010",
                        Label = "MASTERCARD",
                        Priority = 1
                    }
                }
            };
        }

        public static MockCard CreateUnionPayCard()
        {
            return new MockCard
            {
                CardType = "UnionPay",
                PAN = "6231871800000762",
                ExpiryDate = "2023-12",
                CardholderName = "LI MING",
                Track2Data = "6231871800000762306D33122203870000000F",
                ATR = new byte[] { 0x3B, 0x9F, 0x95, 0x81, 0x31, 0xFE, 0x9F, 0x00, 0x65, 0x46, 0x53, 0x05, 0x30, 0x06, 0x71, 0xDF, 0x00, 0x00, 0x00, 0x00, 0x6A },
                Applications = new List<MockApplication>
                {
                    new MockApplication
                    {
                        AID = "A000000333010101",
                        Label = "UNIONPAY CREDIT",
                        Priority = 1
                    }
                }
            };
        }
    }

    /// <summary>
    /// Mock application on a card
    /// </summary>
    public class MockApplication
    {
        public string AID { get; set; }
        public string Label { get; set; }
        public int Priority { get; set; }
        public Dictionary<string, string> Data { get; set; }

        public MockApplication()
        {
            Data = new Dictionary<string, string>();
        }
    }

    /// <summary>
    /// Mock APDU command/response pair
    /// </summary>
    public class MockAPDUResponse
    {
        public string Command { get; set; }
        public string Response { get; set; }
    }

    /// <summary>
    /// Factory for creating common mock scenarios
    /// </summary>
    public static class MockScenarios
    {
        public static MockCardReader CreateReaderWithVisaCard()
        {
            var reader = new MockCardReader("Mock ACS ACR122U");
            var card = MockCard.CreateVisaCard();
            reader.InsertedCard = card;

            // Setup common APDU responses
            reader.QueueResponse("00A4040007A0000000031010", "6F2A840EA000000003101050084D4153544552907A5F2D02656EA3148801015F2D02656E9F1101019F120A4D617374657243617264");
            reader.QueueResponse("80A80000028300", "80069F360200019F2608123456789ABCDEF09F2701809F1007123456789ABCDEF09F37049ABCDEF09000");

            return reader;
        }

        public static MockCardReader CreateReaderWithMastercardCard()
        {
            var reader = new MockCardReader("Mock ACR39U");
            var card = MockCard.CreateMastercardCard();
            reader.InsertedCard = card;

            // Setup Mastercard responses
            reader.QueueResponse("00A4040007A0000000041010", "6F3A8407A000000004101050084D4153544552A51F500A4D41535445524341524487010150095041592E5359532E4444463031BF0C0A61084F07A00000000310108701019000");
            
            return reader;
        }

        public static MockCardReader CreateEmptyReader()
        {
            return new MockCardReader("Mock Empty Reader");
        }

        public static List<MockCardReader> CreateMultipleReaders()
        {
            return new List<MockCardReader>
            {
                CreateReaderWithVisaCard(),
                CreateReaderWithMastercardCard(),
                CreateEmptyReader(),
                new MockCardReader("Mock Reader 4")
            };
        }
    }

    /// <summary>
    /// Mock for ModWinsCard64 static methods
    /// </summary>
    public static class MockWinsCard
    {
        public static List<MockCardReader> AvailableReaders { get; set; } = new List<MockCardReader>();
        public static MockCardReader ConnectedReader { get; set; }

        public static int EstablishContext()
        {
            return 0; // SCARD_S_SUCCESS
        }

        public static int ListReaders(out List<string> readers)
        {
            readers = AvailableReaders.Select(r => r.Name).ToList();
            return 0; // SCARD_S_SUCCESS
        }

        public static int Connect(string readerName, out MockCardReader reader)
        {
            reader = AvailableReaders.FirstOrDefault(r => r.Name == readerName);
            if (reader != null && reader.Connect())
            {
                ConnectedReader = reader;
                return 0; // SCARD_S_SUCCESS
            }
            return -1; // Error
        }

        public static int Disconnect()
        {
            if (ConnectedReader != null)
            {
                ConnectedReader.Disconnect();
                ConnectedReader = null;
                return 0;
            }
            return -1;
        }

        public static int Transmit(byte[] command, out byte[] response)
        {
            response = null;
            if (ConnectedReader == null)
                return -1;

            response = ConnectedReader.SendCommand(command);
            return 0;
        }

        public static void Reset()
        {
            AvailableReaders.Clear();
            ConnectedReader = null;
        }

        public static void SetupDefaultScenario()
        {
            Reset();
            AvailableReaders.AddRange(MockScenarios.CreateMultipleReaders());
        }
    }
}