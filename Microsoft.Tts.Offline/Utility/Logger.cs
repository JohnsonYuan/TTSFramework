//----------------------------------------------------------------------------
// <copyright file="Logger.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     1) Define a interface for logger.
//     2) Implement three logger classes.
//         TextLogger    -> Log message to text file.
//         ConsoleLogger -> Trace logging message to console.
//         NullLogger    -> Do nothing when call LogMessage() method for some special purpose such as test.
//
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Logger interface.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// The delegate of update event handler.
        /// </summary>
        /// <param name="sender">Indicate who is the sender of this update.</param>
        /// <param name="eventArg">The LoggerEventArgs what want to be dispatched.</param>
        void Update(Observable<LoggerEventArgs> sender, LoggerEventArgs eventArg);

        /// <summary>
        /// Log message to logger.
        /// </summary>
        void Log();

        /// <summary>
        /// Log message to logger with special format of string.
        /// </summary>
        /// <param name="format">Format string.</param>
        /// <param name="args">Arguments for format string.</param>
        void Log(string format, params object[] args);

        /// <summary>
        /// Log message to logger with new line.
        /// </summary>
        void LogLine();

        /// <summary>
        /// Log message to logger with special format of string and with new line.
        /// </summary>
        /// <param name="format">Format string.</param>
        /// <param name="args">Arguments for format string.</param>
        void LogLine(string format, params object[] args);

        /// <summary>
        /// Log error message to logger.
        /// </summary>
        void LogError();

        /// <summary>
        /// Log error message to logger with special format of string.
        /// </summary>
        /// <param name="format">Format string.</param>
        /// <param name="args">Arguments for format string.</param>
        void LogError(string format, params object[] args);

        /// <summary>
        /// Log error message to logger with new line.
        /// </summary>
        void LogErrorLine();

        /// <summary>
        /// Log error message to logger with special format of string and with new line.
        /// </summary>
        /// <param name="format">Format string.</param>
        /// <param name="args">Arguments for format string.</param>
        void LogErrorLine(string format, params object[] args);

        /// <summary>
        /// Log warning message to logger.
        /// </summary>
        void LogWarning();

        /// <summary>
        /// Log warning message to logger with special format of string.
        /// </summary>
        /// <param name="format">Format string.</param>
        /// <param name="args">Arguments for format string.</param>
        void LogWarning(string format, params object[] args);

        /// <summary>
        /// Log warning message to logger with new line.
        /// </summary>
        void LogWarningLine();

        /// <summary>
        /// Log warning message to logger with special format of string and with new line.
        /// </summary>
        /// <param name="format">Format string.</param>
        /// <param name="args">Arguments for format string.</param>
        void LogWarningLine(string format, params object[] args);

        /// <summary>
        /// Clean the logger.
        /// </summary>
        void Reset();
    }

    /// <summary>
    /// The Observer argument for ILogger.
    /// </summary>
    public class LoggerEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the format string, which is the first argument in Log() method.
        /// </summary>
        public string Format { get; set; }

        /// <summary>
        /// Gets or sets the arguments for format string, which is the second argument in Log() method.
        /// </summary>
        public object[] Args { get; set; }
    }

    /// <summary>
    /// Null logger, do nothing.
    /// </summary>
    public class NullLogger : ILogger
    {
        #region ILogger Members

        /// <summary>
        /// The delegate of update event handler.
        /// </summary>
        /// <param name="sender">Indicate who is the sender of this update.</param>
        /// <param name="eventArg">The LoggerEventArgs what want to be dispatched.</param>
        public void Update(Observable<LoggerEventArgs> sender, LoggerEventArgs eventArg)
        {
        }

        /// <summary>
        /// Log message to logger.
        /// </summary>
        public void Log()
        {
        }

        /// <summary>
        /// Log message to logger with special format of string.
        /// </summary>
        /// <param name="format">Format string.</param>
        /// <param name="args">Arguments for format string.</param>
        public void Log(string format, params object[] args)
        {
        }

        /// <summary>
        /// Log message to logger with new line.
        /// </summary>
        public void LogLine()
        {
        }

        /// <summary>
        /// Log message to logger with special format of string and with new line.
        /// </summary>
        /// <param name="format">Format string.</param>
        /// <param name="args">Arguments for format string.</param>
        public void LogLine(string format, params object[] args)
        {
        }

        /// <summary>
        /// Log error message to logger.
        /// </summary>
        public void LogError()
        {
        }

        /// <summary>
        /// Log error message to logger with special format of string.
        /// </summary>
        /// <param name="format">Format string.</param>
        /// <param name="args">Arguments for format string.</param>
        public void LogError(string format, params object[] args)
        {
        }

        /// <summary>
        /// Log error message to logger with new line.
        /// </summary>
        public void LogErrorLine()
        {
        }

        /// <summary>
        /// Log error message to logger with special format of string and with new line.
        /// </summary>
        /// <param name="format">Format string.</param>
        /// <param name="args">Arguments for format string.</param>
        public void LogErrorLine(string format, params object[] args)
        {
        }

        /// <summary>
        /// Log warning message to logger.
        /// </summary>
        public void LogWarning()
        {
        }

        /// <summary>
        /// Log warning message to logger with special format of string.
        /// </summary>
        /// <param name="format">Format string.</param>
        /// <param name="args">Arguments for format string.</param>
        public void LogWarning(string format, params object[] args)
        {
        }

        /// <summary>
        /// Log warning message to logger with new line.
        /// </summary>
        public void LogWarningLine()
        {
        }

        /// <summary>
        /// Log warning message to logger with special format of string and with new line.
        /// </summary>
        /// <param name="format">Format string.</param>
        /// <param name="args">Arguments for format string.</param>
        public void LogWarningLine(string format, params object[] args)
        {
        }

        /// <summary>
        /// Clean the logger.
        /// </summary>
        public void Reset()
        {
        }

        #endregion
    }

    /// <summary>
    /// Collects several objects of loggers, such as TextLogger, ConsoleLogger,
    /// Then process them.
    /// </summary>
    public class ChainLogger : ILogger
    {
        #region Fields

        /// <summary>
        /// Collects the objects of loggers.
        /// </summary>
        private Collection<ILogger> _loggers = new Collection<ILogger>();

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the ChainLogger class with objects of ILogger.
        /// </summary>
        /// <param name="loggers">The collection of objects of loggers.</param>
        public ChainLogger(params ILogger[] loggers)
        {
            Helper.ThrowIfNull(loggers);
            loggers.ForEach(l => _loggers.Add(l));
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the Loggers.
        /// </summary>
        public Collection<ILogger> Loggers
        {
            get { return _loggers; }
        }

        #endregion

        #region ILogger Members

        /// <summary>
        /// The delegate of update event handler.
        /// </summary>
        /// <param name="sender">Indicate who is the sender of this update.</param>
        /// <param name="eventArg">The LoggerEventArgs what want to be dispatched.</param>
        public void Update(Observable<LoggerEventArgs> sender, LoggerEventArgs eventArg)
        {
            Log(eventArg.Format, eventArg.Args);
        }

        /// <summary>
        /// Log message to logger.
        /// </summary>
        public void Log()
        {
            _loggers.ForEach(l => l.Log());
        }

        /// <summary>
        /// Log message to logger with special format of string.
        /// </summary>
        /// <param name="format">Format string.</param>
        /// <param name="args">Arguments for format string.</param>
        public void Log(string format, params object[] args)
        {
            _loggers.ForEach(l => l.Log(format, args));
        }

        /// <summary>
        /// Log message to logger with new line.
        /// </summary>
        public void LogLine()
        {
            _loggers.ForEach(l => l.LogLine());
        }

        /// <summary>
        /// Log message to logger with special format of string and with new line.
        /// </summary>
        /// <param name="format">Format string.</param>
        /// <param name="args">Arguments for format string.</param>
        public void LogLine(string format, params object[] args)
        {
            _loggers.ForEach(l => l.LogLine(format, args));
        }

        /// <summary>
        /// Log error message to logger.
        /// </summary>
        public void LogError()
        {
            _loggers.ForEach(l => l.LogError());
        }

        /// <summary>
        /// Log error message to logger with special format of string.
        /// </summary>
        /// <param name="format">Format string.</param>
        /// <param name="args">Arguments for format string.</param>
        public void LogError(string format, params object[] args)
        {
            _loggers.ForEach(l => l.LogError(format, args));
        }

        /// <summary>
        /// Log error message to logger with new line.
        /// </summary>
        public void LogErrorLine()
        {
            _loggers.ForEach(l => l.LogErrorLine());
        }

        /// <summary>
        /// Log error message to logger with special format of string and with new line.
        /// </summary>
        /// <param name="format">Format string.</param>
        /// <param name="args">Arguments for format string.</param>
        public void LogErrorLine(string format, params object[] args)
        {
            _loggers.ForEach(l => l.LogErrorLine(format, args));
        }

        /// <summary>
        /// Log warning message to logger.
        /// </summary>
        public void LogWarning()
        {
            _loggers.ForEach(l => l.LogWarning());
        }

        /// <summary>
        /// Log warning message to logger with special format of string.
        /// </summary>
        /// <param name="format">Format string.</param>
        /// <param name="args">Arguments for format string.</param>
        public void LogWarning(string format, params object[] args)
        {
            _loggers.ForEach(l => l.LogWarning(format, args));
        }

        /// <summary>
        /// Log warning message to logger with new line.
        /// </summary>
        public void LogWarningLine()
        {
            _loggers.ForEach(l => l.LogWarningLine());
        }

        /// <summary>
        /// Log warning message to logger with special format of string and with new line.
        /// </summary>
        /// <param name="format">Format string.</param>
        /// <param name="args">Arguments for format string.</param>
        public void LogWarningLine(string format, params object[] args)
        {
            _loggers.ForEach(l => l.LogWarningLine(format, args));
        }

        /// <summary>
        /// Clean the logger.
        /// </summary>
        public void Reset()
        {
            _loggers.ForEach(l => l.Reset());
        }

        #endregion
    }

    /// <summary>
    /// Text file logger.
    /// </summary>
    public class TextLogger : ILogger
    {
        #region Fields

        private string _filePath;
        private bool _isTraced;

        private string _format;
        private object[] _args;
        private bool _newLine;
        private Encoding _encoding;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the TextLogger class with the log file path.
        /// </summary>
        /// <param name="filePath">File to log into.</param>
        public TextLogger(string filePath)
            : this(filePath, Encoding.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the TextLogger class with the log file path and characters encoding.
        /// </summary>
        /// <param name="filePath">File to log into.</param>
        /// <param name="encoding">The character encoding to use.</param>
        public TextLogger(string filePath, Encoding encoding)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            _filePath = filePath;
            Helper.EnsureFolderExistForFile(_filePath);
            _encoding = encoding;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether open trace.
        /// </summary>
        public bool IsTraced
        {
            get { return _isTraced; }
            set { _isTraced = value; }
        }

        #endregion

        #region ILogger Members

        /// <summary>
        /// The delegate of update event handler.
        /// </summary>
        /// <param name="sender">Indicate who is the sender of this update.</param>
        /// <param name="eventArg">The LoggerEventArgs what want to be dispatched.</param>
        public void Update(Observable<LoggerEventArgs> sender, LoggerEventArgs eventArg)
        {
            Log(eventArg.Format, eventArg.Args);
        }

        /// <summary>
        /// Log message to logger.
        /// </summary>
        public void Log()
        {
            _format = null;
            _args = null;
            _newLine = false;

            LogInfo();
        }

        /// <summary>
        /// Log message to logger with special format of string.
        /// </summary>
        /// <param name="format">Format string.</param>
        /// <param name="args">Arguments for format string.</param>
        public void Log(string format, params object[] args)
        {
            _format = format;
            _args = args;
            _newLine = false;

            LogInfo();
        }

        /// <summary>
        /// Log message to logger with new line.
        /// </summary>
        public void LogLine()
        {
            _format = null;
            _args = null;
            _newLine = true;

            LogInfo();
        }

        /// <summary>
        /// Log message to logger with special format of string and with new line.
        /// </summary>
        /// <param name="format">Format string.</param>
        /// <param name="args">Arguments for format string.</param>
        public void LogLine(string format, params object[] args)
        {
            _format = format;
            _args = args;
            _newLine = true;

            LogInfo();
        }

        /// <summary>
        /// Log error message to logger.
        /// </summary>
        public void LogError()
        {
            Log();
        }

        /// <summary>
        /// Log error message to logger with special format of string.
        /// </summary>
        /// <param name="format">Format string.</param>
        /// <param name="args">Arguments for format string.</param>
        public void LogError(string format, params object[] args)
        {
            Log(format, args);
        }

        /// <summary>
        /// Log a new error line.
        /// </summary>
        public void LogErrorLine()
        {
            LogLine();
        }

        /// <summary>
        /// Log error message to logger with special format of string and with new line.
        /// </summary>
        /// <param name="format">Format string.</param>
        /// <param name="args">Arguments for format string.</param>
        public void LogErrorLine(string format, params object[] args)
        {
            LogLine(format, args);
        }

        /// <summary>
        /// Log a new warning line.
        /// </summary>
        public void LogWarning()
        {
            Log();
        }

        /// <summary>
        /// Log warning message to logger with special format of string.
        /// </summary>
        /// <param name="format">Format string.</param>
        /// <param name="args">Arguments for format string.</param>
        public void LogWarning(string format, params object[] args)
        {
            Log(format, args);
        }

        /// <summary>
        /// Log warning message to logger with new line.
        /// </summary>
        public void LogWarningLine()
        {
            LogLine();
        }

        /// <summary>
        /// Log warning message to logger with special format of string and with new line.
        /// </summary>
        /// <param name="format">Format string.</param>
        /// <param name="args">Arguments for format string.</param>
        public void LogWarningLine(string format, params object[] args)
        {
            LogLine(format, args);
        }

        /// <summary>
        /// Clean the logger.
        /// </summary>
        public void Reset()
        {
            if (File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Performances actual log here.
        /// </summary>
        private void LogInfo()
        {
            lock (_filePath)
            {
                // Given we will lock _filePath before writing, so it will be safe to open file as read/write
                // By this way, it will reduce the numebr of potential confliction of file open for writing between threads.
                FileStream fs = new FileStream(_filePath,
                    FileMode.OpenOrCreate | FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                try
                {
                    using (StreamWriter sw = new StreamWriter(fs, _encoding))
                    {
                        fs = null;
                        LogInfo(sw);
                    }
                }
                finally
                {
                    if (null != fs)
                    {
                        fs.Dispose();
                    }
                }
            }
        }

        /// <summary>
        /// Performances actual log here through specific writer.
        /// </summary>
        /// <param name="writer">Writer used to log information.</param>
        private void LogInfo(StreamWriter writer)
        {
            if (!string.IsNullOrEmpty(_format))
            {
                writer.Write(_format, _args);
                if (IsTraced)
                {
                    Trace.Write(Helper.NeutralFormat(_format, _args));
                }
            }

            if (_newLine)
            {
                writer.WriteLine();
                if (IsTraced)
                {
                    Trace.WriteLine(string.Empty);
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Console logger.
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        #region Fields

        private const ConsoleColor ErrorMessageColor = ConsoleColor.Red;
        private const ConsoleColor WarningMessageColor = ConsoleColor.Yellow;

        private ConsoleColor _foregroundColor;
        private ConsoleColor _backgroundColor;

        private string _format;
        private object[] _args;
        private bool _newLine;
        private bool _isError;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the ConsoleLogger class with default color.
        /// </summary>
        public ConsoleLogger()
        {
            _foregroundColor = Console.ForegroundColor;
            _backgroundColor = Console.BackgroundColor;
        }

        /// <summary>
        /// Initializes a new instance of the ConsoleLogger class with foreground color.
        /// </summary>
        /// <param name="foregroundColor">Foreground color.</param>
        public ConsoleLogger(ConsoleColor foregroundColor)
        {
            _foregroundColor = foregroundColor;
            _backgroundColor = Console.BackgroundColor;
        }

        /// <summary>
        /// Initializes a new instance of the ConsoleLogger class with foreground and background color.
        /// </summary>
        /// <param name="foregroundColor">Foreground color.</param>
        /// <param name="backgroundColor">Background color.</param>
        public ConsoleLogger(ConsoleColor foregroundColor, ConsoleColor backgroundColor)
        {
            _foregroundColor = foregroundColor;
            _backgroundColor = backgroundColor;
        }

        #endregion

        #region ILogger Members

        /// <summary>
        /// The delegate of update event handler.
        /// </summary>
        /// <param name="sender">Indicate who is the sender of this update.</param>
        /// <param name="eventArg">The LoggerEventArgs what want to be dispatched.</param>
        public void Update(Observable<LoggerEventArgs> sender, LoggerEventArgs eventArg)
        {
            Log(eventArg.Format, eventArg.Args);
        }

        /// <summary>
        /// Log message to logger.
        /// </summary>
        public void Log()
        {
            _format = null;
            _args = null;
            _newLine = false;

            LogInfo();
        }

        /// <summary>
        /// Log message to logger with special format of string.
        /// </summary>
        /// <param name="format">Format string.</param>
        /// <param name="args">Arguments for format string.</param>
        public void Log(string format, params object[] args)
        {
            _format = format;
            _args = args;
            _newLine = false;

            LogInfo();
        }

        /// <summary>
        /// Log a new line.
        /// </summary>
        public void LogLine()
        {
            _format = null;
            _args = null;
            _newLine = true;

            LogInfo();
        }

        /// <summary>
        /// Log message to logger with special format of string and with new line.
        /// </summary>
        /// <param name="format">Format string.</param>
        /// <param name="args">Arguments for format string.</param>
        public void LogLine(string format, params object[] args)
        {
            _format = format;
            _args = args;
            _newLine = true;

            LogInfo();
        }

        /// <summary>
        /// Log error message to logger.
        /// </summary>
        public void LogError()
        {
            ConsoleColor backupFC = _foregroundColor;
            _foregroundColor = ErrorMessageColor;
            bool backupIsError = _isError;
            _isError = true;

            Log();

            _foregroundColor = backupFC;
            _isError = backupIsError;
        }

        /// <summary>
        /// Log error message to logger with special format of string.
        /// </summary>
        /// <param name="format">Format string.</param>
        /// <param name="args">Arguments for format string.</param>
        public void LogError(string format, params object[] args)
        {
            ConsoleColor backupFC = _foregroundColor;
            _foregroundColor = ErrorMessageColor;
            bool backupIsError = _isError;
            _isError = true;

            Log(format, args);

            _foregroundColor = backupFC;
            _isError = backupIsError;
        }

        /// <summary>
        /// Log a new error line.
        /// </summary>
        public void LogErrorLine()
        {
            ConsoleColor backupFC = _foregroundColor;
            _foregroundColor = ErrorMessageColor;
            bool backupIsError = _isError;
            _isError = true;

            LogLine();

            _foregroundColor = backupFC;
            _isError = backupIsError;
        }

        /// <summary>
        /// Log error message to logger with special format of string and with new line.
        /// </summary>
        /// <param name="format">Format string.</param>
        /// <param name="args">Arguments for format string.</param>
        public void LogErrorLine(string format, params object[] args)
        {
            ConsoleColor backupFC = _foregroundColor;
            _foregroundColor = ErrorMessageColor;
            bool backupIsError = _isError;
            _isError = true;

            LogLine(format, args);

            _foregroundColor = backupFC;
            _isError = backupIsError;
        }

        /// <summary>
        /// Log a new warning line.
        /// </summary>
        public void LogWarning()
        {
            ConsoleColor backupFC = _foregroundColor;
            _foregroundColor = WarningMessageColor;
            bool backupIsError = _isError;
            _isError = true;

            Log();

            _foregroundColor = backupFC;
            _isError = backupIsError;
        }

        /// <summary>
        /// Log warning message to logger with special format of string.
        /// </summary>
        /// <param name="format">Format string.</param>
        /// <param name="args">Arguments for format string.</param>
        public void LogWarning(string format, params object[] args)
        {
            ConsoleColor backupFC = _foregroundColor;
            _foregroundColor = WarningMessageColor;
            bool backupIsError = _isError;
            _isError = true;

            Log(format, args);

            _foregroundColor = backupFC;
            _isError = backupIsError;
        }

        /// <summary>
        /// Log a new warning line.
        /// </summary>
        public void LogWarningLine()
        {
            ConsoleColor backupFC = _foregroundColor;
            _foregroundColor = WarningMessageColor;
            bool backupIsError = _isError;
            _isError = true;

            LogLine();

            _foregroundColor = backupFC;
            _isError = backupIsError;
        }

        /// <summary>
        /// Log warning message to logger with special format of string and with new line.
        /// </summary>
        /// <param name="format">Format string.</param>
        /// <param name="args">Arguments for format string.</param>
        public void LogWarningLine(string format, params object[] args)
        {
            ConsoleColor backupFC = _foregroundColor;
            _foregroundColor = WarningMessageColor;
            bool backupIsError = _isError;
            _isError = true;

            LogLine(format, args);

            _foregroundColor = backupFC;
            _isError = backupIsError;
        }

        /// <summary>
        /// Clean the logger.
        /// </summary>
        public void Reset()
        {
            Console.Clear();
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Do actual log here.
        /// </summary>
        private void LogInfo()
        {
            ConsoleColor backupFC = Console.ForegroundColor;
            ConsoleColor backupBC = Console.BackgroundColor;
            Console.ForegroundColor = _foregroundColor;
            Console.BackgroundColor = _backgroundColor;

            if (!string.IsNullOrEmpty(_format))
            {
                if (_isError)
                {
                    Console.Error.Write(_format, _args);
                }
                else
                {
                    Console.Write(_format, _args);
                }
            }

            if (_newLine)
            {
                if (_isError)
                {
                    Console.Error.WriteLine();
                }
                else
                {
                    Console.WriteLine();
                }
            }

            Console.ForegroundColor = backupFC;
            Console.BackgroundColor = backupBC;
        }

        #endregion
    }

    /// <summary>
    /// Console progress logger.
    /// </summary>
    public class ConsoleProgressLogger
    {
        #region Fields

        /// <summary>
        /// Default message format.
        /// </summary>
        private const string DefaultMessageFormat = "{0}% items processed ...";

        private ConsoleColor _progressMessageColor;
        private ConsoleColor _summaryMessageColor;
        private int _totalItemsCount;
        private int _processedPercentage;
        private int _previousShownPercentage;
        private int _cursorLeft;
        private bool _enableConsoleOutput;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the ConsoleProgressLogger class.
        /// </summary>
        /// <param name="totalItemsCount">Total items count.</param>
        public ConsoleProgressLogger(int totalItemsCount)
            : this(totalItemsCount, Console.ForegroundColor, Console.ForegroundColor)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ConsoleProgressLogger class.
        /// </summary>
        /// <param name="totalItemsCount">Total items count.</param>
        /// <param name="progressMessageColor">Process message color.</param>
        /// <param name="summaryMessageColor">Summary message color.</param>
        public ConsoleProgressLogger(int totalItemsCount, ConsoleColor progressMessageColor,
            ConsoleColor summaryMessageColor)
        {
            _processedPercentage = 0;
            _previousShownPercentage = 0;
            _totalItemsCount = totalItemsCount;
            DetectConsole();
            if (_enableConsoleOutput)
            {
                _progressMessageColor = progressMessageColor;
                _summaryMessageColor = summaryMessageColor;
            }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Log message to show the progress of tool runing.
        /// </summary>
        /// <param name="processedItemsCount">Processed items count.</param>
        public void LogProgress(int processedItemsCount)
        {
            if (_enableConsoleOutput)
            {
                _processedPercentage = (processedItemsCount * 100) / _totalItemsCount;

                // If the percentage change, show message.
                if ((_processedPercentage - _previousShownPercentage) >= 1)
                {
                    _previousShownPercentage = _processedPercentage;
                    ConsoleColor backupFC = Console.ForegroundColor;
                    Console.ForegroundColor = _progressMessageColor;

                    Console.Write(DefaultMessageFormat, _processedPercentage);
                    Console.SetCursorPosition(_cursorLeft, Console.CursorTop);

                    if (processedItemsCount == _totalItemsCount)
                    {
                        Console.WriteLine();
                    }

                    Console.ForegroundColor = backupFC;
                }
            }
        }

        /// <summary>
        /// Log message to show the summary of tool running.
        /// </summary>
        /// <param name="processedItemsCount">Processed items count.</param>
        public void LogSummary(int processedItemsCount)
        {
            if (_enableConsoleOutput)
            {
                ConsoleColor backupFC = Console.ForegroundColor;
                Console.ForegroundColor = _summaryMessageColor;

                if (processedItemsCount == 0)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "There is no items processed. Exit.");
                    throw new InvalidDataException(message);
                }

                Console.WriteLine("Process complete:");
                Console.WriteLine("    Total processed : {0}", processedItemsCount);
                Console.WriteLine("    Total ignored   : {0}", _totalItemsCount - processedItemsCount);

                Console.ForegroundColor = backupFC;
            }
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Detect the console visible can be shift or not.
        /// </summary>
        private void DetectConsole()
        {
            _enableConsoleOutput = true;
            try
            {
                ConsoleColor backupFC = Console.ForegroundColor;
                Console.ForegroundColor = _progressMessageColor;
                Console.ForegroundColor = backupFC;
                _cursorLeft = Console.CursorLeft;
                Console.SetCursorPosition(_cursorLeft, Console.CursorTop);
            }
            catch (IOException)
            {
                _enableConsoleOutput = false;
            }
        }

        #endregion
    }

    /// <summary>
    /// Provide a thread safe operation on logger objects.
    /// </summary>
    public class SyncedLogger : ILogger
    {
        #region Fields

        private ILogger _innerLogger = null;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncedLogger"/> class.
        /// </summary>
        /// <param name="innerLogger">The real inner logger.</param>
        public SyncedLogger(ILogger innerLogger)
        {
            _innerLogger = innerLogger;
        }

        #endregion

        #region ILogger Members

        /// <summary>
        /// Log message to logger with special format of string.
        /// </summary>
        /// <param name="format">Format string.</param>
        /// <param name="args">Arguments for format string.</param>
        public void Log(string format, params object[] args)
        {
            lock (this)
            {
                _innerLogger.Log(format, args);
            }
        }

        /// <summary>
        /// Log message to logger.
        /// </summary>
        public void Log()
        {
            lock (this)
            {
                _innerLogger.Log();
            }
        }

        /// <summary>
        /// Log error message to logger with special format of string.
        /// </summary>
        /// <param name="format">Format string.</param>
        /// <param name="args">Arguments for format string.</param>
        public void LogError(string format, params object[] args)
        {
            lock (this)
            {
                _innerLogger.LogError(format, args);
            }
        }

        /// <summary>
        /// Log error message to logger.
        /// </summary>
        public void LogError()
        {
            lock (this)
            {
                _innerLogger.LogError();
            }
        }

        /// <summary>
        /// Log error message to logger with special format of string and with new line.
        /// </summary>
        /// <param name="format">Format string.</param>
        /// <param name="args">Arguments for format string.</param>
        public void LogErrorLine(string format, params object[] args)
        {
            lock (this)
            {
                _innerLogger.LogErrorLine(format, args);
            }
        }

        /// <summary>
        /// Log error message to logger with new line.
        /// </summary>
        public void LogErrorLine()
        {
            lock (this)
            {
                _innerLogger.LogErrorLine();
            }
        }

        /// <summary>
        /// Log message to logger with special format of string and with new line.
        /// </summary>
        /// <param name="format">Format string.</param>
        /// <param name="args">Arguments for format string.</param>
        public void LogLine(string format, params object[] args)
        {
            lock (this)
            {
                _innerLogger.LogLine(format, args);
            }
        }

        /// <summary>
        /// Log message to logger with new line.
        /// </summary>
        public void LogLine()
        {
            lock (this)
            {
                _innerLogger.LogLine();
            }
        }

        /// <summary>
        /// Log warning message to logger with special format of string.
        /// </summary>
        /// <param name="format">Format string.</param>
        /// <param name="args">Arguments for format string.</param>
        public void LogWarning(string format, params object[] args)
        {
            lock (this)
            {
                _innerLogger.LogWarning(format, args);
            }
        }

        /// <summary>
        /// Log warning message to logger.
        /// </summary>
        public void LogWarning()
        {
            lock (this)
            {
                _innerLogger.LogWarning();
            }
        }

        /// <summary>
        /// Log warning message to logger with special format of string and with new line.
        /// </summary>
        /// <param name="format">Format string.</param>
        /// <param name="args">Arguments for format string.</param>
        public void LogWarningLine(string format, params object[] args)
        {
            lock (this)
            {
                _innerLogger.LogWarningLine(format, args);
            }
        }

        /// <summary>
        /// Log warning message to logger with new line.
        /// </summary>
        public void LogWarningLine()
        {
            lock (this)
            {
                _innerLogger.LogWarningLine();
            }
        }

        /// <summary>
        /// Clean the logger.
        /// </summary>
        public void Reset()
        {
            lock (this)
            {
                _innerLogger.Reset();
            }
        }

        /// <summary>
        /// The delegate of update event handler.
        /// </summary>
        /// <param name="sender">Indicate who is the sender of this update.</param>
        /// <param name="eventArg">The LoggerEventArgs what want to be dispatched.</param>
        public void Update(Observable<LoggerEventArgs> sender, LoggerEventArgs eventArg)
        {
            lock (this)
            {
                _innerLogger.Update(sender, eventArg);
            }
        }

        #endregion
    }
}