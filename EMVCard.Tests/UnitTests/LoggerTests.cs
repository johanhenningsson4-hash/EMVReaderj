/*=========================================================================================
'  EMVCard.Tests - Logger Unit Tests
'  
'  Description: Comprehensive tests for the Logger class
'==========================================================================================*/

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EMVCard;

namespace EMVCard.Tests.UnitTests
{
    [TestClass]
    public class LoggerTests
    {
        private string testLogDirectory;
        private string originalLogDirectory;
        private bool originalIsEnabled;
        private long originalMaxFileSize;
        private int originalMaxFiles;

        [TestInitialize]
        public void Setup()
        {
            // Store original settings
            originalLogDirectory = Logger.LogDirectory;
            originalIsEnabled = Logger.IsEnabled;
            originalMaxFileSize = Logger.MaxLogFileSize;
            originalMaxFiles = Logger.MaxLogFiles;

            // Create temp directory for tests
            testLogDirectory = Path.Combine(Path.GetTempPath(), "EMVReaderTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(testLogDirectory);

            // Configure Logger for testing
            Logger.LogDirectory = testLogDirectory;
            Logger.IsEnabled = true;
            Logger.MaxLogFileSize = 1024; // 1KB for testing
            Logger.MaxLogFiles = 3;
            Logger.Initialize();
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Restore original settings
            Logger.LogDirectory = originalLogDirectory;
            Logger.IsEnabled = originalIsEnabled;
            Logger.MaxLogFileSize = originalMaxFileSize;
            Logger.MaxLogFiles = originalMaxFiles;

            // Clean up test directory
            if (Directory.Exists(testLogDirectory))
            {
                try
                {
                    Directory.Delete(testLogDirectory, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        [TestMethod]
        public void Logger_Initialize_CreatesLogDirectory()
        {
            // Arrange
            var newTestDir = Path.Combine(Path.GetTempPath(), "EMVReaderTests", Guid.NewGuid().ToString());
            Logger.LogDirectory = newTestDir;

            // Act
            Logger.Initialize();

            // Assert
            Assert.IsTrue(Directory.Exists(newTestDir));

            // Cleanup
            Directory.Delete(newTestDir, true);
        }

        [TestMethod]
        public void Logger_Info_CreatesLogEntry()
        {
            // Arrange
            string testMessage = "Test info message";

            // Act
            Logger.Info(testMessage);
            Thread.Sleep(100); // Allow file write

            // Assert
            var logFiles = Directory.GetFiles(testLogDirectory, "*.log");
            Assert.IsTrue(logFiles.Length > 0);

            var logContent = File.ReadAllText(logFiles[0]);
            Assert.IsTrue(logContent.Contains("[INFO]"));
            Assert.IsTrue(logContent.Contains(testMessage));
        }

        [TestMethod]
        public void Logger_Error_CreatesLogEntry()
        {
            // Arrange
            string testMessage = "Test error message";

            // Act
            Logger.Error(testMessage);
            Thread.Sleep(100);

            // Assert
            var logFiles = Directory.GetFiles(testLogDirectory, "*.log");
            Assert.IsTrue(logFiles.Length > 0);

            var logContent = File.ReadAllText(logFiles[0]);
            Assert.IsTrue(logContent.Contains("[ERROR]"));
            Assert.IsTrue(logContent.Contains(testMessage));
        }

        [TestMethod]
        public void Logger_ErrorWithException_IncludesStackTrace()
        {
            // Arrange
            string testMessage = "Test error with exception";
            var exception = new InvalidOperationException("Test exception");

            // Act
            Logger.Error(testMessage, exception);
            Thread.Sleep(100);

            // Assert
            var logFiles = Directory.GetFiles(testLogDirectory, "*.log");
            Assert.IsTrue(logFiles.Length > 0);

            var logContent = File.ReadAllText(logFiles[0]);
            Assert.IsTrue(logContent.Contains("[ERROR]"));
            Assert.IsTrue(logContent.Contains(testMessage));
            Assert.IsTrue(logContent.Contains("InvalidOperationException"));
            Assert.IsTrue(logContent.Contains("Test exception"));
            Assert.IsTrue(logContent.Contains("StackTrace"));
        }

        [TestMethod]
        public void Logger_Warning_CreatesLogEntry()
        {
            // Arrange
            string testMessage = "Test warning message";

            // Act
            Logger.Warning(testMessage);
            Thread.Sleep(100);

            // Assert
            var logFiles = Directory.GetFiles(testLogDirectory, "*.log");
            Assert.IsTrue(logFiles.Length > 0);

            var logContent = File.ReadAllText(logFiles[0]);
            Assert.IsTrue(logContent.Contains("[WARNING]"));
            Assert.IsTrue(logContent.Contains(testMessage));
        }

        [TestMethod]
        public void Logger_Debug_CreatesLogEntry()
        {
            // Arrange
            string testMessage = "Test debug message";

            // Act
            Logger.Debug(testMessage);
            Thread.Sleep(100);

            // Assert
            var logFiles = Directory.GetFiles(testLogDirectory, "*.log");
            Assert.IsTrue(logFiles.Length > 0);

            var logContent = File.ReadAllText(logFiles[0]);
            Assert.IsTrue(logContent.Contains("[DEBUG]"));
            Assert.IsTrue(logContent.Contains(testMessage));
        }

        [TestMethod]
        public void Logger_Disabled_DoesNotCreateLogEntry()
        {
            // Arrange
            Logger.IsEnabled = false;
            string testMessage = "This should not be logged";

            // Act
            Logger.Info(testMessage);
            Thread.Sleep(100);

            // Assert
            var logFiles = Directory.GetFiles(testLogDirectory, "*.log");
            if (logFiles.Length > 0)
            {
                var logContent = File.ReadAllText(logFiles[0]);
                Assert.IsFalse(logContent.Contains(testMessage));
            }
        }

        [TestMethod]
        public void Logger_LogRotation_RotatesWhenSizeExceeded()
        {
            // Arrange
            Logger.MaxLogFileSize = 100; // Very small for testing
            string longMessage = new string('A', 200); // Larger than max size

            // Act
            Logger.Info(longMessage);
            Thread.Sleep(100);
            Logger.Info("Second message");
            Thread.Sleep(100);

            // Assert
            var logFiles = Directory.GetFiles(testLogDirectory, "*.log");
            Assert.IsTrue(logFiles.Length > 1); // Should have rotated
        }

        [TestMethod]
        public void Logger_ThreadSafety_ConcurrentLogging()
        {
            // Arrange
            int threadCount = 10;
            int messagesPerThread = 10;
            var tasks = new Task[threadCount];

            // Act
            for (int i = 0; i < threadCount; i++)
            {
                int threadId = i;
                tasks[i] = Task.Run(() =>
                {
                    for (int j = 0; j < messagesPerThread; j++)
                    {
                        Logger.Info($"Thread {threadId}, Message {j}");
                    }
                });
            }

            Task.WaitAll(tasks);
            Thread.Sleep(500); // Allow all writes to complete

            // Assert
            var logFiles = Directory.GetFiles(testLogDirectory, "*.log");
            Assert.IsTrue(logFiles.Length > 0);

            int totalMessages = 0;
            foreach (var file in logFiles)
            {
                var content = File.ReadAllText(file);
                totalMessages += content.Split(new[] { "[INFO]" }, StringSplitOptions.RemoveEmptyEntries).Length - 1;
            }

            Assert.AreEqual(threadCount * messagesPerThread, totalMessages);
        }

        [TestMethod]
        public void Logger_GetLogFilePath_ReturnsCorrectPath()
        {
            // Act
            var logPath = Logger.GetLogFilePath();

            // Assert
            Assert.IsTrue(logPath.StartsWith(testLogDirectory));
            Assert.IsTrue(logPath.EndsWith(".log"));
            Assert.IsTrue(logPath.Contains("EMVReader_"));
        }

        [TestMethod]
        public void Logger_ClearAllLogs_RemovesAllLogFiles()
        {
            // Arrange
            Logger.Info("Test message 1");
            Logger.Info("Test message 2");
            Thread.Sleep(100);

            // Verify logs exist
            var logFiles = Directory.GetFiles(testLogDirectory, "*.log");
            Assert.IsTrue(logFiles.Length > 0);

            // Act
            Logger.ClearAllLogs();

            // Assert
            logFiles = Directory.GetFiles(testLogDirectory, "*.log");
            Assert.AreEqual(0, logFiles.Length);
        }

        [TestMethod]
        public void Logger_EmptyMessage_DoesNotLog()
        {
            // Act
            Logger.Info("");
            Logger.Info(null);
            Thread.Sleep(100);

            // Assert
            var logFiles = Directory.GetFiles(testLogDirectory, "*.log");
            if (logFiles.Length > 0)
            {
                var logContent = File.ReadAllText(logFiles[0]);
                // Should not contain empty entries
                Assert.IsFalse(logContent.Contains("[INFO] \r\n"));
            }
        }
    }
}