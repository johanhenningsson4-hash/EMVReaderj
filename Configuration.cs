/*=========================================================================================
'  Copyright(C):    Advanced Card Systems Ltd 
'  
'  Module :         Configuration.cs
'   
'  Description:     Application configuration management
'==========================================================================================*/

using System;
using System.Configuration;
using System.IO;

namespace EMVCard
{
    /// <summary>
    /// Application configuration management
    /// </summary>
    public static class AppConfiguration
    {
        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "EMVReader", "config.ini");

        // Default values
        public static class Defaults
        {
            public const bool EnableLogging = true;
            public const string LogDirectory = "Logs";
            public const int MaxLogFiles = 10;
            public const long MaxLogFileSize = 5 * 1024 * 1024; // 5MB
            public const bool ShowDebugInfo = false;
            public const int ConnectionTimeout = 30000; // 30 seconds
            public const bool AutoSelectFirstReader = true;
            public const bool AutoSelectFirstApplication = true;
        }

        // Configuration properties
        public static bool EnableLogging { get; set; } = Defaults.EnableLogging;
        public static string LogDirectory { get; set; } = Defaults.LogDirectory;
        public static int MaxLogFiles { get; set; } = Defaults.MaxLogFiles;
        public static long MaxLogFileSize { get; set; } = Defaults.MaxLogFileSize;
        public static bool ShowDebugInfo { get; set; } = Defaults.ShowDebugInfo;
        public static int ConnectionTimeout { get; set; } = Defaults.ConnectionTimeout;
        public static bool AutoSelectFirstReader { get; set; } = Defaults.AutoSelectFirstReader;
        public static bool AutoSelectFirstApplication { get; set; } = Defaults.AutoSelectFirstApplication;

        /// <summary>
        /// Load configuration from file
        /// </summary>
        public static void Load()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    // Create default configuration
                    Save();
                    return;
                }

                var lines = File.ReadAllLines(ConfigPath);
                foreach (var line in lines)
                {
                    var parts = line.Split('=');
                    if (parts.Length != 2) continue;

                    var key = parts[0].Trim();
                    var value = parts[1].Trim();

                    switch (key)
                    {
                        case nameof(EnableLogging):
                            if (bool.TryParse(value, out bool enableLogging))
                                EnableLogging = enableLogging;
                            break;
                        case nameof(LogDirectory):
                            LogDirectory = value;
                            break;
                        case nameof(MaxLogFiles):
                            if (int.TryParse(value, out int maxLogFiles))
                                MaxLogFiles = maxLogFiles;
                            break;
                        case nameof(MaxLogFileSize):
                            if (long.TryParse(value, out long maxLogFileSize))
                                MaxLogFileSize = maxLogFileSize;
                            break;
                        case nameof(ShowDebugInfo):
                            if (bool.TryParse(value, out bool showDebugInfo))
                                ShowDebugInfo = showDebugInfo;
                            break;
                        case nameof(ConnectionTimeout):
                            if (int.TryParse(value, out int connectionTimeout))
                                ConnectionTimeout = connectionTimeout;
                            break;
                        case nameof(AutoSelectFirstReader):
                            if (bool.TryParse(value, out bool autoSelectReader))
                                AutoSelectFirstReader = autoSelectReader;
                            break;
                        case nameof(AutoSelectFirstApplication):
                            if (bool.TryParse(value, out bool autoSelectApp))
                                AutoSelectFirstApplication = autoSelectApp;
                            break;
                    }
                }

                Logger.Info("Configuration loaded successfully");
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load configuration", ex);
                // Use defaults
            }
        }

        /// <summary>
        /// Save configuration to file
        /// </summary>
        public static void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(ConfigPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var config = new[]
                {
                    $"{nameof(EnableLogging)}={EnableLogging}",
                    $"{nameof(LogDirectory)}={LogDirectory}",
                    $"{nameof(MaxLogFiles)}={MaxLogFiles}",
                    $"{nameof(MaxLogFileSize)}={MaxLogFileSize}",
                    $"{nameof(ShowDebugInfo)}={ShowDebugInfo}",
                    $"{nameof(ConnectionTimeout)}={ConnectionTimeout}",
                    $"{nameof(AutoSelectFirstReader)}={AutoSelectFirstReader}",
                    $"{nameof(AutoSelectFirstApplication)}={AutoSelectFirstApplication}"
                };

                File.WriteAllLines(ConfigPath, config);
                Logger.Info("Configuration saved successfully");
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to save configuration", ex);
            }
        }

        /// <summary>
        /// Apply configuration to Logger
        /// </summary>
        public static void ApplyToLogger()
        {
            Logger.IsEnabled = EnableLogging;
            Logger.LogDirectory = LogDirectory;
            Logger.MaxLogFiles = MaxLogFiles;
            Logger.MaxLogFileSize = MaxLogFileSize;
        }

        /// <summary>
        /// Reset to defaults
        /// </summary>
        public static void ResetToDefaults()
        {
            EnableLogging = Defaults.EnableLogging;
            LogDirectory = Defaults.LogDirectory;
            MaxLogFiles = Defaults.MaxLogFiles;
            MaxLogFileSize = Defaults.MaxLogFileSize;
            ShowDebugInfo = Defaults.ShowDebugInfo;
            ConnectionTimeout = Defaults.ConnectionTimeout;
            AutoSelectFirstReader = Defaults.AutoSelectFirstReader;
            AutoSelectFirstApplication = Defaults.AutoSelectFirstApplication;
        }
    }
}