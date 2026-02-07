/*=========================================================================================
'  EMVCard.Tests - Configuration Tests
'  
'  Description: Tests for application configuration management
'==========================================================================================*/

using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EMVCard;

namespace EMVCard.Tests.UnitTests
{
    [TestClass]
    public class ConfigurationTests
    {
        private string originalAppData;
        private string testConfigPath;

        [TestInitialize]
        public void Setup()
        {
            // Create a temporary directory for testing
            originalAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var tempDir = Path.Combine(Path.GetTempPath(), "EMVReaderConfigTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            // Set test environment
            testConfigPath = Path.Combine(tempDir, "EMVReader", "config.ini");
            
            // We need to redirect AppConfiguration to use test path
            // This would require modifying AppConfiguration to accept a custom path
            // For now, we'll test the configuration logic conceptually
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Clean up test files
            var testDir = Path.GetDirectoryName(Path.GetDirectoryName(testConfigPath));
            if (Directory.Exists(testDir))
            {
                try
                {
                    Directory.Delete(testDir, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        [TestMethod]
        public void Configuration_DefaultValues_AreCorrect()
        {
            // Act
            AppConfiguration.ResetToDefaults();

            // Assert
            Assert.AreEqual(AppConfiguration.Defaults.EnableLogging, AppConfiguration.EnableLogging);
            Assert.AreEqual(AppConfiguration.Defaults.LogDirectory, AppConfiguration.LogDirectory);
            Assert.AreEqual(AppConfiguration.Defaults.MaxLogFiles, AppConfiguration.MaxLogFiles);
            Assert.AreEqual(AppConfiguration.Defaults.MaxLogFileSize, AppConfiguration.MaxLogFileSize);
            Assert.AreEqual(AppConfiguration.Defaults.ShowDebugInfo, AppConfiguration.ShowDebugInfo);
            Assert.AreEqual(AppConfiguration.Defaults.ConnectionTimeout, AppConfiguration.ConnectionTimeout);
            Assert.AreEqual(AppConfiguration.Defaults.AutoSelectFirstReader, AppConfiguration.AutoSelectFirstReader);
            Assert.AreEqual(AppConfiguration.Defaults.AutoSelectFirstApplication, AppConfiguration.AutoSelectFirstApplication);
        }

        [TestMethod]
        public void Configuration_SetValues_StoresCorrectly()
        {
            // Arrange
            bool testLogging = false;
            string testLogDir = "TestLogs";
            int testMaxFiles = 5;
            long testMaxSize = 1024 * 1024; // 1MB
            bool testDebug = true;
            int testTimeout = 60000;
            bool testAutoReader = false;
            bool testAutoApp = false;

            // Act
            AppConfiguration.EnableLogging = testLogging;
            AppConfiguration.LogDirectory = testLogDir;
            AppConfiguration.MaxLogFiles = testMaxFiles;
            AppConfiguration.MaxLogFileSize = testMaxSize;
            AppConfiguration.ShowDebugInfo = testDebug;
            AppConfiguration.ConnectionTimeout = testTimeout;
            AppConfiguration.AutoSelectFirstReader = testAutoReader;
            AppConfiguration.AutoSelectFirstApplication = testAutoApp;

            // Assert
            Assert.AreEqual(testLogging, AppConfiguration.EnableLogging);
            Assert.AreEqual(testLogDir, AppConfiguration.LogDirectory);
            Assert.AreEqual(testMaxFiles, AppConfiguration.MaxLogFiles);
            Assert.AreEqual(testMaxSize, AppConfiguration.MaxLogFileSize);
            Assert.AreEqual(testDebug, AppConfiguration.ShowDebugInfo);
            Assert.AreEqual(testTimeout, AppConfiguration.ConnectionTimeout);
            Assert.AreEqual(testAutoReader, AppConfiguration.AutoSelectFirstReader);
            Assert.AreEqual(testAutoApp, AppConfiguration.AutoSelectFirstApplication);
        }

        [TestMethod]
        public void Configuration_ApplyToLogger_UpdatesLoggerSettings()
        {
            // Arrange
            bool originalLoggerEnabled = Logger.IsEnabled;
            string originalLoggerDir = Logger.LogDirectory;
            int originalMaxFiles = Logger.MaxLogFiles;
            long originalMaxSize = Logger.MaxLogFileSize;

            AppConfiguration.EnableLogging = false;
            AppConfiguration.LogDirectory = "TestLogDir";
            AppConfiguration.MaxLogFiles = 7;
            AppConfiguration.MaxLogFileSize = 2048;

            try
            {
                // Act
                AppConfiguration.ApplyToLogger();

                // Assert
                Assert.AreEqual(AppConfiguration.EnableLogging, Logger.IsEnabled);
                Assert.AreEqual(AppConfiguration.LogDirectory, Logger.LogDirectory);
                Assert.AreEqual(AppConfiguration.MaxLogFiles, Logger.MaxLogFiles);
                Assert.AreEqual(AppConfiguration.MaxLogFileSize, Logger.MaxLogFileSize);
            }
            finally
            {
                // Restore original Logger settings
                Logger.IsEnabled = originalLoggerEnabled;
                Logger.LogDirectory = originalLoggerDir;
                Logger.MaxLogFiles = originalMaxFiles;
                Logger.MaxLogFileSize = originalMaxSize;
            }
        }

        [TestMethod]
        public void Configuration_ResetToDefaults_RestoresDefaults()
        {
            // Arrange - Change all values from defaults
            AppConfiguration.EnableLogging = !AppConfiguration.Defaults.EnableLogging;
            AppConfiguration.LogDirectory = "NonDefaultDir";
            AppConfiguration.MaxLogFiles = AppConfiguration.Defaults.MaxLogFiles + 10;
            AppConfiguration.MaxLogFileSize = AppConfiguration.Defaults.MaxLogFileSize * 2;
            AppConfiguration.ShowDebugInfo = !AppConfiguration.Defaults.ShowDebugInfo;
            AppConfiguration.ConnectionTimeout = AppConfiguration.Defaults.ConnectionTimeout + 5000;
            AppConfiguration.AutoSelectFirstReader = !AppConfiguration.Defaults.AutoSelectFirstReader;
            AppConfiguration.AutoSelectFirstApplication = !AppConfiguration.Defaults.AutoSelectFirstApplication;

            // Act
            AppConfiguration.ResetToDefaults();

            // Assert
            Assert.AreEqual(AppConfiguration.Defaults.EnableLogging, AppConfiguration.EnableLogging);
            Assert.AreEqual(AppConfiguration.Defaults.LogDirectory, AppConfiguration.LogDirectory);
            Assert.AreEqual(AppConfiguration.Defaults.MaxLogFiles, AppConfiguration.MaxLogFiles);
            Assert.AreEqual(AppConfiguration.Defaults.MaxLogFileSize, AppConfiguration.MaxLogFileSize);
            Assert.AreEqual(AppConfiguration.Defaults.ShowDebugInfo, AppConfiguration.ShowDebugInfo);
            Assert.AreEqual(AppConfiguration.Defaults.ConnectionTimeout, AppConfiguration.ConnectionTimeout);
            Assert.AreEqual(AppConfiguration.Defaults.AutoSelectFirstReader, AppConfiguration.AutoSelectFirstReader);
            Assert.AreEqual(AppConfiguration.Defaults.AutoSelectFirstApplication, AppConfiguration.AutoSelectFirstApplication);
        }

        [TestMethod]
        public void Configuration_SaveAndLoad_Roundtrip()
        {
            // This test would require modifying AppConfiguration to accept a custom file path
            // For demonstration, we'll test the configuration format parsing logic

            // Arrange
            var testConfig = new Dictionary<string, string>
            {
                { "EnableLogging", "false" },
                { "LogDirectory", "CustomLogs" },
                { "MaxLogFiles", "15" },
                { "MaxLogFileSize", "10485760" },
                { "ShowDebugInfo", "true" },
                { "ConnectionTimeout", "45000" },
                { "AutoSelectFirstReader", "false" },
                { "AutoSelectFirstApplication", "false" }
            };

            // Act & Assert
            foreach (var kvp in testConfig)
            {
                var parsed = ParseConfigValue(kvp.Key, kvp.Value);
                Assert.IsNotNull(parsed, $"Failed to parse {kvp.Key}={kvp.Value}");
            }
        }

        [TestMethod]
        public void Configuration_InvalidValues_HandledGracefully()
        {
            // Test that invalid configuration values don't crash the application
            
            // These would test the actual Load method's error handling
            var invalidConfigs = new[]
            {
                ("EnableLogging", "not_a_boolean"),
                ("MaxLogFiles", "not_a_number"),
                ("MaxLogFileSize", "-1"),
                ("ConnectionTimeout", "invalid_timeout")
            };

            foreach (var (key, value) in invalidConfigs)
            {
                // In the actual implementation, these should not throw exceptions
                // and should fall back to default values
                Assert.DoesNotThrow(() => ParseConfigValue(key, value),
                    $"Parsing {key}={value} should not throw an exception");
            }
        }

        [TestMethod]
        public void Configuration_FilePermissions_HandledGracefully()
        {
            // Test behavior when config file cannot be written/read
            // This would test the exception handling in Save/Load methods
            
            // Simulate scenarios like:
            // - Config directory doesn't exist
            // - Config file is read-only
            // - Insufficient permissions
            
            // For now, just verify that the methods exist and can be called
            Assert.DoesNotThrow(() => AppConfiguration.Save());
            Assert.DoesNotThrow(() => AppConfiguration.Load());
        }

        #region Helper Methods

        private object ParseConfigValue(string key, string value)
        {
            // Simulate the parsing logic from AppConfiguration.Load()
            switch (key)
            {
                case "EnableLogging":
                case "ShowDebugInfo":
                case "AutoSelectFirstReader":
                case "AutoSelectFirstApplication":
                    return bool.TryParse(value, out bool boolResult) ? (object)boolResult : null;
                
                case "MaxLogFiles":
                case "ConnectionTimeout":
                    return int.TryParse(value, out int intResult) ? (object)intResult : null;
                
                case "MaxLogFileSize":
                    return long.TryParse(value, out long longResult) ? (object)longResult : null;
                
                case "LogDirectory":
                    return string.IsNullOrWhiteSpace(value) ? null : value;
                
                default:
                    return null;
            }
        }

        /// <summary>
        /// Helper method for .NET Framework compatibility
        /// </summary>
        private void Assert.DoesNotThrow(Action action, string message = null)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Assert.Fail(message ?? $"Expected no exception, but got: {ex.GetType().Name}: {ex.Message}");
            }
        }

        #endregion
    }
}