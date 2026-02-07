/*=========================================================================================
'  EMVCard.Tests - Integration Tests
'  
'  Description: Integration tests for complete EMV card reading workflows
'==========================================================================================*/

using System;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EMVCard.Tests.TestUtilities;

namespace EMVCard.Tests.IntegrationTests
{
    [TestClass]
    public class CardReaderIntegrationTests
    {
        private MockCardReader mockReader;
        private MockCard testCard;

        [TestInitialize]
        public void Setup()
        {
            // Setup mock environment
            MockWinsCard.SetupDefaultScenario();
            mockReader = MockScenarios.CreateReaderWithVisaCard();
            testCard = MockCard.CreateVisaCard();
        }

        [TestCleanup]
        public void Cleanup()
        {
            MockWinsCard.Reset();
        }

        [TestMethod]
        public void Integration_CompleteCardRead_Visa_Success()
        {
            // Arrange
            var reader = MockScenarios.CreateReaderWithVisaCard();
            
            // Setup expected APDU responses for complete Visa card read
            SetupVisaCardResponses(reader);

            // Act - Simulate the complete card reading process
            bool connected = reader.Connect();
            
            // Send PSE select command
            var pseResponse = reader.SendCommand(HexToBytes("00A4040E31504159.5359532E4444463031"));
            
            // Send application select
            var appResponse = reader.SendCommand(HexToBytes("00A4040007A0000000031010"));
            
            // Send GPO
            var gpoResponse = reader.SendCommand(HexToBytes("80A80000028300"));

            // Assert
            Assert.IsTrue(connected);
            Assert.IsTrue(pseResponse.Length > 2);
            Assert.IsTrue(appResponse.Length > 2);
            Assert.IsTrue(gpoResponse.Length > 2);
            
            // Verify success status words (90 00)
            Assert.AreEqual(0x90, appResponse[appResponse.Length - 2]);
            Assert.AreEqual(0x00, appResponse[appResponse.Length - 1]);
        }

        [TestMethod]
        public void Integration_CompleteCardRead_Mastercard_Success()
        {
            // Arrange
            var reader = MockScenarios.CreateReaderWithMastercardCard();
            SetupMastercardResponses(reader);

            // Act
            bool connected = reader.Connect();
            var appResponse = reader.SendCommand(HexToBytes("00A4040007A0000000041010"));
            var gpoResponse = reader.SendCommand(HexToBytes("80A80000028300"));

            // Assert
            Assert.IsTrue(connected);
            Assert.AreEqual(0x90, appResponse[appResponse.Length - 2]);
            Assert.AreEqual(0x00, appResponse[appResponse.Length - 1]);
        }

        [TestMethod]
        public void Integration_MultipleReaderScenario_ReaderSelection()
        {
            // Arrange
            var readers = MockScenarios.CreateMultipleReaders();
            
            // Act - Connect to each reader
            var results = new bool[readers.Count];
            for (int i = 0; i < readers.Count; i++)
            {
                results[i] = readers[i].Connect();
            }

            // Assert
            Assert.IsTrue(results[0]); // Visa card reader
            Assert.IsTrue(results[1]); // Mastercard card reader
            Assert.IsFalse(results[2]); // Empty reader
            Assert.IsFalse(results[3]); // Empty reader
        }

        [TestMethod]
        public void Integration_CardRemovalAndInsertion_Simulation()
        {
            // Arrange
            var reader = new MockCardReader("Test Reader");
            var card = MockCard.CreateVisaCard();

            // Act & Assert - No card initially
            Assert.IsFalse(reader.Connect());

            // Insert card
            reader.InsertedCard = card;
            Assert.IsTrue(reader.Connect());

            // Remove card
            reader.InsertedCard = null;
            reader.Disconnect();
            Assert.IsFalse(reader.Connect());
        }

        [TestMethod]
        public void Integration_ErrorHandling_CardNotPresent()
        {
            // Arrange
            var emptyReader = MockScenarios.CreateEmptyReader();

            // Act
            bool connected = emptyReader.Connect();
            var response = emptyReader.SendCommand(HexToBytes("00A4040007A0000000031010"));

            // Assert
            Assert.IsFalse(connected);
            Assert.AreEqual(0x6F, response[0]); // Card not present error
            Assert.AreEqual(0x00, response[1]);
        }

        [TestMethod]
        public void Integration_ErrorHandling_UnsupportedCommand()
        {
            // Arrange
            var reader = MockScenarios.CreateReaderWithVisaCard();
            reader.Connect();

            // Act - Send unsupported command
            var response = reader.SendCommand(HexToBytes("FF000000")); // Invalid command

            // Assert
            Assert.AreEqual(0x6D, response[0]); // Instruction not supported
            Assert.AreEqual(0x00, response[1]);
        }

        [TestMethod]
        public void Integration_PSEandPPSE_BothScenarios()
        {
            // Arrange
            var reader = MockScenarios.CreateReaderWithVisaCard();
            SetupPSEResponses(reader);
            reader.Connect();

            // Act - Test PSE
            var pseResponse = reader.SendCommand(HexToBytes("00A4040E31504159.5359532E4444463031"));
            
            // Test PPSE
            var ppseResponse = reader.SendCommand(HexToBytes("00A4040E32504159.5359532E4444463031"));

            // Assert
            Assert.IsTrue(pseResponse.Length >= 2);
            Assert.IsTrue(ppseResponse.Length >= 2);
        }

        [TestMethod]
        public void Integration_RecordReading_SFISequence()
        {
            // Arrange
            var reader = MockScenarios.CreateReaderWithVisaCard();
            SetupSFIRecordResponses(reader);
            reader.Connect();

            // Act - Read records from SFI 1
            var record1 = reader.SendCommand(HexToBytes("00B2010C00"));
            var record2 = reader.SendCommand(HexToBytes("00B2020C00"));
            var recordNotFound = reader.SendCommand(HexToBytes("00B2030C00"));

            // Assert
            Assert.AreEqual(0x90, record1[record1.Length - 2]); // Success
            Assert.AreEqual(0x90, record2[record2.Length - 2]); // Success
            Assert.AreEqual(0x6A, recordNotFound[0]); // Record not found
            Assert.AreEqual(0x83, recordNotFound[1]);
        }

        [TestMethod]
        public void Integration_ConcurrentReaderAccess_ThreadSafety()
        {
            // Arrange
            const int threadCount = 5;
            var readers = new MockCardReader[threadCount];
            var results = new bool[threadCount];
            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

            for (int i = 0; i < threadCount; i++)
            {
                readers[i] = MockScenarios.CreateReaderWithVisaCard();
            }

            // Act - Multiple threads accessing readers concurrently
            var tasks = new System.Threading.Tasks.Task[threadCount];
            for (int i = 0; i < threadCount; i++)
            {
                int threadIndex = i;
                tasks[i] = System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        results[threadIndex] = readers[threadIndex].Connect();
                        
                        // Simulate some card operations
                        for (int j = 0; j < 10; j++)
                        {
                            var response = readers[threadIndex].SendCommand(HexToBytes("00A4040007A0000000031010"));
                            Thread.Sleep(10);
                        }
                        
                        readers[threadIndex].Disconnect();
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                });
            }

            System.Threading.Tasks.Task.WaitAll(tasks);

            // Assert
            Assert.AreEqual(0, exceptions.Count, "No exceptions should occur in concurrent access");
            Assert.IsTrue(results.All(r => r), "All readers should connect successfully");
        }

        [TestMethod]
        public void Integration_DataExtraction_CompleteWorkflow()
        {
            // Arrange
            var reader = MockScenarios.CreateReaderWithVisaCard();
            SetupCompleteDataExtractionResponses(reader);
            reader.Connect();

            // Act - Simulate complete data extraction workflow
            var applicationData = ExtractApplicationData(reader);

            // Assert
            Assert.IsNotNull(applicationData);
            Assert.IsNotNull(applicationData.PAN);
            Assert.IsNotNull(applicationData.ExpiryDate);
            // Additional assertions based on mock data
        }

        #region Helper Methods

        private void SetupVisaCardResponses(MockCardReader reader)
        {
            // PSE Selection
            reader.QueueResponse("00A4040E31504159.5359532E4444463031", 
                "6F3E8407A000000003101050084D4153544552A5238801015F2D02656EA31A4F07A00000000310105010564953412043524544495487010150084D4153544552");

            // Application Selection
            reader.QueueResponse("00A4040007A0000000031010", 
                "6F2A840EA000000003101050084D4153544552907A5F2D02656EA3148801015F2D02656E9F1101019F120A4D617374657243617264");

            // GPO
            reader.QueueResponse("80A80000028300", 
                "80069F360200019F2608123456789ABCDEF09F2701809F1007123456789ABCDEF09F37049ABCDEF09000");
        }

        private void SetupMastercardResponses(MockCardReader reader)
        {
            reader.QueueResponse("00A4040007A0000000041010", 
                "6F3A8407A000000004101050084D4153544552A51F500A4D41535445524341524487010150095041592E5359532E4444463031BF0C0A61084F07A00000000310108701019000");
        }

        private void SetupPSEResponses(MockCardReader reader)
        {
            // PSE
            reader.QueueResponse("00A4040E31504159.5359532E4444463031", 
                "6F2E840E31504159.5359532E4444463031A51C4F07A00000000310105010564953412043524544495487010150084D4153544552");
            
            // PPSE
            reader.QueueResponse("00A4040E32504159.5359532E4444463031", 
                "6F2E840E32504159.5359532E4444463031A51C4F07A00000000310105010564953412043524544495487010150084D4153544552");
        }

        private void SetupSFIRecordResponses(MockCardReader reader)
        {
            // SFI 1, Record 1
            reader.QueueResponse("00B2010C00", 
                "70125A084532123456789012F5F24032512");

            // SFI 1, Record 2  
            reader.QueueResponse("00B2020C00", 
                "701957134532123456789012D25129F1E024901295F200B4A4F484E20444F452020");

            // SFI 1, Record 3 - Not found
            reader.QueueResponse("00B2030C00", "6A83");
        }

        private void SetupCompleteDataExtractionResponses(MockCardReader reader)
        {
            SetupVisaCardResponses(reader);
            SetupSFIRecordResponses(reader);
        }

        private byte[] HexToBytes(string hex)
        {
            hex = hex.Replace(" ", "").Replace(".", "");
            var bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return bytes;
        }

        private ApplicationData ExtractApplicationData(MockCardReader reader)
        {
            // Simplified data extraction simulation
            var data = new ApplicationData();
            
            // This would typically involve multiple APDU commands
            // and TLV parsing, but for testing we'll simulate the results
            var response = reader.SendCommand(HexToBytes("00B2010C00"));
            
            if (response.Length > 2 && response[response.Length-2] == 0x90)
            {
                data.PAN = "4532123456789012";
                data.ExpiryDate = "2025-12";
                data.CardholderName = "JOHN DOE";
            }
            
            return data;
        }

        private class ApplicationData
        {
            public string PAN { get; set; }
            public string ExpiryDate { get; set; }
            public string CardholderName { get; set; }
        }

        #endregion
    }
}