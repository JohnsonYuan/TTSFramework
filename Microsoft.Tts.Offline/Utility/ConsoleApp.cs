//----------------------------------------------------------------------------
// <copyright file="ConsoleApp.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements help functions for standardizing the main function
//     of Program.cs.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Utility
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using Microsoft.Tts.Offline.FlowEngine;

    /// <summary>
    /// Defines the Validate methods that the inherit class must implement to
    /// Evaluate the parameters.
    /// </summary>
    public interface IValidator
    {
        /// <summary>
        /// Evaluates parameters.
        /// </summary>
        void Validate();
    }

    /// <summary>
    /// Logs sink interafce.
    /// </summary>
    public interface ILogSink
    {
        #region Properties

        /// <summary>
        /// Gets log file path.
        /// </summary>
        string LogFilePath { get; }

        #endregion
    }

    /// <summary>
    /// This class can standardize the "main" function, unify its content.
    /// </summary>
    /// <typeparam name="T">The pattern type.</typeparam>
    public static class ConsoleApp<T> where T : new()
    {
        #region Public static methods

        /// <summary>
        /// Standardizes the "main" function, 
        /// 1. It can handle all the thrown exceptions.
        /// 2. It has the same pattern of return code.
        /// 3. Its parameters have the same format.
        /// </summary>
        /// <param name="arguments">The arguments of the calling main function.</param>
        /// <param name="process">The main process function.</param>
        /// <returns>The error code.</returns>
        public static int Run(string[] arguments, Func<T, int> process)
        {
            Helper.ThrowIfNull(arguments);
            Helper.ThrowIfNull(process);

            ConsoleLogger consoleLogger = new ConsoleLogger();

            return Run(arguments, process, consoleLogger);
        }

        /// <summary>
        /// Standardizes the "main" function, 
        /// 1. It can handle all the thrown exceptions.
        /// 2. It has the same pattern of return code.
        /// 3. Its parameters have the same format.
        /// </summary>
        /// <param name="arguments">The arguments of the calling main function.</param>
        /// <param name="process">The main process function.</param>
        /// <param name="logger">The logger used for log.</param>
        /// <returns>The error code.</returns>
        public static int Run(string[] arguments, Func<T, int> process, ILogger logger)
        {
            Helper.ThrowIfNull(arguments);
            Helper.ThrowIfNull(process);
            Helper.ThrowIfNull(logger);

            return Run(arguments, process,
                new Func<T, ILogger, Exception, int>((a, i, e) => { return ExitCode.NoError; }),
                logger);
        }

        /// <summary>
        /// Standardizes the "main" function, 
        /// 1. It can handle all the thrown exceptions.
        /// 2. It has the same pattern of return code.
        /// 3. Its parameters have the same format.
        /// </summary>
        /// <param name="arguments">The arguments of the calling main function.</param>
        /// <param name="process">The main process function.</param>
        /// <param name="errorHandle">The delegate function of error Handle.</param>
        /// <param name="logger">The logger used for log.</param>
        /// <returns>The error code.</returns>
        public static int Run(string[] arguments, Func<T, int> process, Func<T, ILogger, Exception, int> errorHandle, ILogger logger)
        {
            DateTime startTime = DateTime.Now;

            Helper.ThrowIfNull(arguments);
            Helper.ThrowIfNull(process);
            Helper.ThrowIfNull(errorHandle);
            Helper.ThrowIfNull(logger);

            T arg = new T();
            ILogger usedLogger = logger;
            TextLogger logfile = null;
            int ret = ExitCode.NoError;

            try
            {
                try
                {
                    CommandLineParser.Parse(arguments, arg);
                    IValidator validator = arg as IValidator;
                    if (validator != null)
                    {
                        validator.Validate();
                    }

                    if (arg is ILogSink && !string.IsNullOrEmpty(((ILogSink)arg).LogFilePath))
                    {
                        if (File.Exists(((ILogSink)arg).LogFilePath))
                        {
                            DateTime lastUpdateTime = File.GetLastWriteTime(((ILogSink)arg).LogFilePath);
                            
                            string logPath = Path.GetDirectoryName(((ILogSink)arg).LogFilePath);
                            string neuFormat = Helper.NeutralFormat("{0}.{1:yyyyMMdd-HHmmss}", Path.GetFileNameWithoutExtension(((ILogSink)arg).LogFilePath), lastUpdateTime)
                                .AppendExtensionName(Path.GetExtension(((ILogSink)arg).LogFilePath));

                            string targetName = Path.Combine(logPath, neuFormat);

                            Helper.SafeDelete(targetName);
                            File.Move(((ILogSink)arg).LogFilePath, targetName);
                        }

                        Helper.EnsureFolderExistForFile(((ILogSink)arg).LogFilePath);

                        logfile = new TextLogger(((ILogSink)arg).LogFilePath, Encoding.Unicode);
                        usedLogger = new ChainLogger(logger, logfile);
                    }
                }
                catch (CommandLineParseException cpe)
                {
                    if (cpe.ErrorString == CommandLineParseException.ErrorStringHelp)
                    {
                        CommandLineParser.PrintUsage(arg);
                    }
                    else
                    {
                        usedLogger.LogErrorLine(cpe.Message, arg);
                    }

                    return ExitCode.InvalidArgument;
                }

                ret = process(arg);
                logger.LogLine("Elapsed time: {0}.", DateTime.Now.Subtract(startTime).ToString());

                return ret;
            }
            catch (Exception e)
            {
                if (errorHandle != null)
                {
                    ret = errorHandle(arg, usedLogger, e);
                }

                if (ret != ExitCode.NoError)
                {
                    return ret;
                }
                else if (Helper.FilterException(e))
                {
                    ret = Helper.GetExceptionErrorCode(e);

                    logger.LogErrorLine("The process failed. Exception:");
                    usedLogger.LogErrorLine("{0}", Helper.BuildExceptionMessage(e));

                    if (arg is ILogSink && !string.IsNullOrEmpty(((ILogSink)arg).LogFilePath))
                    {
                        logger.LogLine("Detailed log could be found at \"{0}\".", ((ILogSink)arg).LogFilePath);
                    }

                    return ret;
                }

                throw;
            }
        }

        #endregion
    }
}