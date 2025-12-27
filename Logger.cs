/*=========================================================================================
'  Copyright(C):    Advanced Card Systems Ltd 
'  
'  Module :         Logger.cs
'   
'  Description:     Logging utility for EMV Card Reader application
'==========================================================================================*/

using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace EMVCard
{
    /// <summary>
    /// Log level enumeration
    /// </summary>
    public enum LogLevel
    {
        DEBUG,
        INFO,
        WARNING,
        ERROR,
        CRITICAL
    }

    /// <summary>
    /// Logger class for file and UI logging
    /// </summary>
    public class Logger
    {
        private static Logger _instance;
        private static readonly object _lock = new object();
        private string _logFilePath;
        private bool _enableFileLogging;
        private bool _enableConsoleLogging;
        private LogLevel _minimumLogLevel;
        private RichTextBox _logTextBox;

        /// <summary>
        /// Get the singleton instance of Logger
        /// </summary>
        public static Logger Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new Logger();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Private constructor for singleton pattern
        /// </summary>
        private Logger()
        {
            _enableFileLogging = true;
            _enableConsoleLogging = true;
            _minimumLogLevel = LogLevel.DEBUG;
            InitializeLogFile();
        }

        /// <summary>
        /// Initialize log file with timestamp
        /// </summary>
        private void InitializeLogFile()
        {
            try
            {
                string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                _logFilePath = Path.Combine(logDirectory, $"EMVReader_{timestamp}.log");

                // Write initial log entry
                WriteToFile($"=== EMV Card Reader Log Started at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize log file: {ex.Message}", "Logging Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _enableFileLogging = false;
            }
        }

        /// <summary>
        /// Set the RichTextBox control for UI logging
        /// </summary>
        public void SetLogTextBox(RichTextBox textBox)
        {
            _logTextBox = textBox;
        }

        /// <summary>
        /// Enable or disable file logging
        /// </summary>
        public void SetFileLogging(bool enabled)
        {
            _enableFileLogging = enabled;
        }

        /// <summary>
        /// Set minimum log level
        /// </summary>
        public void SetMinimumLogLevel(LogLevel level)
        {
            _minimumLogLevel = level;
        }

        /// <summary>
        /// Get current log file path
        /// </summary>
        public string GetLogFilePath()
        {
            return _logFilePath;
        }

        /// <summary>
        /// Write log message with specified level
        /// </summary>
        private void Log(LogLevel level, string message, Exception ex = null)
        {
            if (level < _minimumLogLevel)
                return;

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string logEntry = $"[{timestamp}] [{level}] {message}";

            if (ex != null)
            {
                logEntry += $"\nException: {ex.GetType().Name}: {ex.Message}\nStackTrace: {ex.StackTrace}";
            }

            // Write to file
            if (_enableFileLogging)
            {
                WriteToFile(logEntry);
            }

            // Write to UI
            if (_logTextBox != null && _logTextBox.InvokeRequired == false)
            {
                WriteToUI(logEntry, level);
            }
            else if (_logTextBox != null)
            {
                _logTextBox.Invoke(new Action(() => WriteToUI(logEntry, level)));
            }
        }

        /// <summary>
        /// Write message to log file
        /// </summary>
        private void WriteToFile(string message)
        {
            try
            {
                lock (_lock)
                {
                    File.AppendAllText(_logFilePath, message + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch (Exception)
            {
                // Silently fail to avoid recursive logging errors
            }
        }

        /// <summary>
        /// Write message to UI with color coding
        /// </summary>
        private void WriteToUI(string message, LogLevel level)
        {
            try
            {
                int startPos = _logTextBox.Text.Length;
                _logTextBox.AppendText(message + Environment.NewLine);

                // Color code based on log level
                _logTextBox.Select(startPos, message.Length);
                switch (level)
                {
                    case LogLevel.ERROR:
                    case LogLevel.CRITICAL:
                        _logTextBox.SelectionColor = System.Drawing.Color.Red;
                        break;
                    case LogLevel.WARNING:
                        _logTextBox.SelectionColor = System.Drawing.Color.Orange;
                        break;
                    case LogLevel.INFO:
                        _logTextBox.SelectionColor = System.Drawing.Color.Blue;
                        break;
                    case LogLevel.DEBUG:
                        _logTextBox.SelectionColor = System.Drawing.Color.Gray;
                        break;
                }

                _logTextBox.Select(_logTextBox.Text.Length, 0);
                _logTextBox.SelectionColor = _logTextBox.ForeColor;
                _logTextBox.ScrollToCaret();
            }
            catch (Exception)
            {
                // Silently fail to avoid UI errors
            }
        }

        // Public logging methods
        public void Debug(string message)
        {
            Log(LogLevel.DEBUG, message);
        }

        public void Info(string message)
        {
            Log(LogLevel.INFO, message);
        }

        public void Warning(string message)
        {
            Log(LogLevel.WARNING, message);
        }

        public void Error(string message, Exception ex = null)
        {
            Log(LogLevel.ERROR, message, ex);
        }

        public void Critical(string message, Exception ex = null)
        {
            Log(LogLevel.CRITICAL, message, ex);
        }

        /// <summary>
        /// Log APDU command (sent to card)
        /// </summary>
        public void LogAPDUCommand(byte[] apdu, int length)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("< APDU CMD: ");
            for (int i = 0; i < length; i++)
            {
                sb.AppendFormat("{0:X2} ", apdu[i]);
            }
            Info(sb.ToString().TrimEnd());
        }

        /// <summary>
        /// Log APDU response (received from card)
        /// </summary>
        public void LogAPDUResponse(byte[] response, int length)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("> APDU RSP: ");
            for (int i = 0; i < length; i++)
            {
                sb.AppendFormat("{0:X2} ", response[i]);
            }
            Info(sb.ToString().TrimEnd());
        }

        /// <summary>
        /// Log section separator
        /// </summary>
        public void LogSeparator(string sectionName = null)
        {
            string separator = sectionName != null 
                ? $"======== {sectionName} ========"
                : "=====================================";
            Info(separator);
        }

        /// <summary>
        /// Close log file
        /// </summary>
        public void Close()
        {
            if (_enableFileLogging)
            {
                WriteToFile($"=== EMV Card Reader Log Ended at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
            }
        }
    }
}
