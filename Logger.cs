/*=========================================================================================
'  Copyright(C):    Advanced Card Systems Ltd 
'  
'  Module :         Logger.cs
'   
'  Description:     Logging utility for EMV card reader operations
'==========================================================================================*/

using System;
using System.IO;
using System.Text;

namespace EMVCard
{
    /// <summary>
    /// Provides logging functionality with file rotation and multiple severity levels
    /// </summary>
    public static class Logger
    {
        private static readonly object _lockObject = new object();
        private static string _logDirectory = "Logs";
        private static string _logFileName = "EMVReader";
        private static long _maxLogFileSize = 5 * 1024 * 1024; // 5 MB
        private static int _maxLogFiles = 10;
        private static bool _isEnabled = true;

        /// <summary>
        /// Log severity levels
        /// </summary>
        public enum LogLevel
        {
            Info,
            Warning,
            Error,
            Debug
        }

        /// <summary>
        /// Gets or sets whether logging is enabled
        /// </summary>
        public static bool IsEnabled
        {
            get { return _isEnabled; }
            set { _isEnabled = value; }
        }

        /// <summary>
        /// Gets or sets the log directory path
        /// </summary>
        public static string LogDirectory
        {
            get { return _logDirectory; }
            set { _logDirectory = value; }
        }

        /// <summary>
        /// Gets or sets the maximum log file size in bytes before rotation
        /// </summary>
        public static long MaxLogFileSize
        {
            get { return _maxLogFileSize; }
            set { _maxLogFileSize = value; }
        }

        /// <summary>
        /// Gets or sets the maximum number of log files to retain
        /// </summary>
        public static int MaxLogFiles
        {
            get { return _maxLogFiles; }
            set { _maxLogFiles = value; }
        }

        /// <summary>
        /// Initialize the logger and ensure log directory exists
        /// </summary>
        public static void Initialize()
        {
            try
            {
                if (!Directory.Exists(_logDirectory))
                {
                    Directory.CreateDirectory(_logDirectory);
                }
            }
            catch (Exception ex)
            {
                // If we can't create log directory, disable logging
                _isEnabled = false;
                System.Diagnostics.Debug.WriteLine($"Failed to initialize logger: {ex.Message}");
            }
        }

        /// <summary>
        /// Log an informational message
        /// </summary>
        public static void Info(string message)
        {
            Log(LogLevel.Info, message);
        }

        /// <summary>
        /// Log a warning message
        /// </summary>
        public static void Warning(string message)
        {
            Log(LogLevel.Warning, message);
        }

        /// <summary>
        /// Log an error message
        /// </summary>
        public static void Error(string message)
        {
            Log(LogLevel.Error, message);
        }

        /// <summary>
        /// Log an error with exception details
        /// </summary>
        public static void Error(string message, Exception ex)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(message);
            sb.AppendLine($"Exception: {ex.GetType().Name}");
            sb.AppendLine($"Message: {ex.Message}");
            sb.AppendLine($"StackTrace: {ex.StackTrace}");
            
            if (ex.InnerException != null)
            {
                sb.AppendLine($"Inner Exception: {ex.InnerException.Message}");
            }

            Log(LogLevel.Error, sb.ToString());
        }

        /// <summary>
        /// Log a debug message
        /// </summary>
        public static void Debug(string message)
        {
            Log(LogLevel.Debug, message);
        }

        /// <summary>
        /// Main logging method
        /// </summary>
        private static void Log(LogLevel level, string message)
        {
            if (!_isEnabled || string.IsNullOrEmpty(message))
                return;

            lock (_lockObject)
            {
                try
                {
                    string logFilePath = GetCurrentLogFilePath();
                    
                    // Check if rotation is needed
                    if (File.Exists(logFilePath))
                    {
                        FileInfo fileInfo = new FileInfo(logFilePath);
                        if (fileInfo.Length >= _maxLogFileSize)
                        {
                            RotateLogFiles();
                        }
                    }

                    // Format and write log entry
                    string logEntry = FormatLogEntry(level, message);
                    File.AppendAllText(logFilePath, logEntry, Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    // Log to debug output if file logging fails
                    System.Diagnostics.Debug.WriteLine($"Logging failed: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Format a log entry with timestamp and level
        /// </summary>
        private static string FormatLogEntry(LogLevel level, string message)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ");
            sb.Append($"[{level.ToString().ToUpper()}] ");
            sb.AppendLine(message);
            return sb.ToString();
        }

        /// <summary>
        /// Get the current log file path
        /// </summary>
        private static string GetCurrentLogFilePath()
        {
            string dateString = DateTime.Now.ToString("yyyyMMdd");
            return Path.Combine(_logDirectory, $"{_logFileName}_{dateString}.log");
        }

        /// <summary>
        /// Rotate log files when size limit is reached
        /// </summary>
        private static void RotateLogFiles()
        {
            try
            {
                // Get all log files sorted by creation time
                DirectoryInfo dirInfo = new DirectoryInfo(_logDirectory);
                FileInfo[] logFiles = dirInfo.GetFiles($"{_logFileName}_*.log");
                
                if (logFiles.Length >= _maxLogFiles)
                {
                    // Sort by creation time (oldest first)
                    Array.Sort(logFiles, (x, y) => x.CreationTime.CompareTo(y.CreationTime));
                    
                    // Delete oldest files to maintain max count
                    int filesToDelete = logFiles.Length - _maxLogFiles + 1;
                    for (int i = 0; i < filesToDelete && i < logFiles.Length; i++)
                    {
                        try
                        {
                            logFiles[i].Delete();
                        }
                        catch
                        {
                            // Ignore deletion errors
                        }
                    }
                }

                // Rename current log file with timestamp
                string currentLogPath = GetCurrentLogFilePath();
                if (File.Exists(currentLogPath))
                {
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string newPath = Path.Combine(_logDirectory, $"{_logFileName}_{timestamp}.log");
                    
                    // If file already exists with this timestamp, add a counter
                    int counter = 1;
                    while (File.Exists(newPath))
                    {
                        newPath = Path.Combine(_logDirectory, $"{_logFileName}_{timestamp}_{counter}.log");
                        counter++;
                    }
                    
                    File.Move(currentLogPath, newPath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Log rotation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Clear all log files
        /// </summary>
        public static void ClearAllLogs()
        {
            lock (_lockObject)
            {
                try
                {
                    if (Directory.Exists(_logDirectory))
                    {
                        DirectoryInfo dirInfo = new DirectoryInfo(_logDirectory);
                        FileInfo[] logFiles = dirInfo.GetFiles($"{_logFileName}_*.log");
                        
                        foreach (var file in logFiles)
                        {
                            try
                            {
                                file.Delete();
                            }
                            catch
                            {
                                // Ignore deletion errors
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to clear logs: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Get the path to the current log file
        /// </summary>
        public static string GetLogFilePath()
        {
            return GetCurrentLogFilePath();
        }
    }
}
