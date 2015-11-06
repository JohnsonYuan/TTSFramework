//----------------------------------------------------------------------------
// <copyright file="Command.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements command line tasks
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Security.Permissions;
    using System.Text;
    using System.Threading;

    /// <summary>
    /// Class to handle command relative task.
    /// </summary>
    [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
    public static class CommandLine
    {
        /// <summary>
        /// Lock object to protect Process.Start.
        /// </summary>
        private static object _processStartLock = new object();

        private static Encoding defaultOutputEncoding = Encoding.UTF8;
        private static Encoding defaultErrorEncoding = Encoding.UTF8;

        #region Public static operations

        /// <summary>
        /// Run command in shell without retrieve the output of the target process.
        /// </summary>
        /// <param name="command">Command to run.</param>
        /// <param name="arguments">Argument string.</param>
        /// <param name="workingDirectory">Working directory.</param>
        /// <returns>True for return zero or not.</returns>
        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        public static bool RunCommand(string command, string arguments, string workingDirectory)
        {
            if (string.IsNullOrEmpty(workingDirectory))
            {
                workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            }

            return RunCommand(command, arguments, workingDirectory, null, null, null) == 0;
        }

        /// <summary>
        /// Run command line with given parameters.
        /// CreateNoWindow is true; RedirectStandardOutput is false; RedirectStandardError is false.
        /// </summary>
        /// <param name="command">Command line to be executed.</param>
        /// <param name="arguments">Command line argument.</param>
        /// <param name="useShellExecute">Whether use shell to execute.</param>
        /// <param name="waitDone">Indicates whether waiting for target process exiting.</param>
        /// <param name="workingDirectory">Working direcotry.</param>
        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        public static void RunCommand(string command, string arguments,
            bool useShellExecute, bool waitDone, string workingDirectory)
        {
            if (string.IsNullOrEmpty(command))
            {
                throw new ArgumentNullException("command");
            }

            if (string.IsNullOrEmpty(arguments))
            {
                arguments = string.Empty;
            }

            if (!Directory.Exists(workingDirectory))
            {
                throw new ArgumentException(workingDirectory,
                    new DirectoryNotFoundException(workingDirectory));
            }

            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = command;
            psi.Arguments = arguments;
            psi.WorkingDirectory = workingDirectory;
            psi.UseShellExecute = useShellExecute;
            psi.CreateNoWindow = true;
            psi.RedirectStandardOutput = false;
            psi.RedirectStandardError = false;

            Process process = Process.Start(psi);

            if (waitDone)
            {
                process.WaitForExit();
            }
        }

        /// <summary>
        /// Run command in shell without retrieve the output of the target process.
        /// </summary>
        /// <param name="command">Command to run.</param>
        /// <param name="arguments">Argument string.</param>
        /// <param name="workingDirectory">Working directory.</param>
        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        public static void RunCommandAsync(string command, string arguments, string workingDirectory)
        {
            // Use and empty EventHandler, it will never care about the process exit event.
            using (Process p = RunCommandHelper(command, arguments, workingDirectory, null, null,
                new EventHandler(delegate(object o, EventArgs ea) { }), null, null))
            {
            }
        }

        /// <summary>
        /// Run command in shell without retrieve the output of the target process.
        /// </summary>
        /// <param name="command">Command to run.</param>
        /// <param name="arguments">Argument string.</param>
        /// <param name="standardOutputFileName">File path to redirect standard output for.</param>
        /// <param name="append">Append mode or not.</param>
        /// <param name="workingDirectory">Working directory.</param>
        /// <returns>Extern application return code.</returns>
        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        public static int RunCommandToFile(string command, string arguments,
            string standardOutputFileName, bool append,
            string workingDirectory)
        {
            return RunCommandToFile(command, arguments, standardOutputFileName,
                null, append, workingDirectory);
        }

        /// <summary>
        /// Run command in shell without retrieve the output of the target process.
        /// </summary>
        /// <param name="command">Command to run.</param>
        /// <param name="arguments">Argument string.</param>
        /// <param name="standardOutputFileName">File path to redirect standard output for.</param>
        /// <param name="errorOutputFileName">File path to redirect standard error output for.</param>
        /// <param name="append">Append mode or not.</param>
        /// <param name="workingDirectory">Working directory.</param>
        /// <returns>Extern application return code.</returns>
        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        public static int RunCommandToFile(string command, string arguments,
            string standardOutputFileName, string errorOutputFileName, bool append,
            string workingDirectory)
        {
            if (!string.IsNullOrEmpty(standardOutputFileName))
            {
                Helper.EnsureFolderExistForFile(standardOutputFileName);
            }

            if (!string.IsNullOrEmpty(errorOutputFileName))
            {
                Helper.EnsureFolderExistForFile(errorOutputFileName);
            }

            using (StreamWriter standardWriter = string.IsNullOrEmpty(standardOutputFileName)
                ? null : new StreamWriter(standardOutputFileName, append))
            using (StreamWriter errorWriter = string.IsNullOrEmpty(errorOutputFileName)
                ? null : new StreamWriter(errorOutputFileName, append))
            {
                return RunCommand(command, arguments, workingDirectory, standardWriter, errorWriter, null);
            }
        }

        /// <summary>
        /// Run command in shell.
        /// If exitcode is not zero, it will throw exception.
        /// </summary>
        /// <param name="command">Command to run.</param>
        /// <param name="arguments">Argument string.</param>
        /// <param name="workingDirectory">Working directory.</param>
        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        public static void SuccessRunCommand(string command, string arguments, string workingDirectory)
        {
            int exitCode = 0;
            exitCode = RunCommand(command, arguments, workingDirectory, null, null, null);
            ThrowExceptionForInvalidExitCode(command, arguments, exitCode);
        }

        /// <summary>
        /// Run command in shell, and retrieve the output of the target process.
        /// If exitcode is not zero, it will throw exception and without output.
        /// </summary>
        /// <param name="command">Command to run.</param>
        /// <param name="arguments">Argument string.</param>
        /// <param name="workingDirectory">Working directory.</param>
        /// <returns>Output string.</returns>
        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        public static string RunCommandWithOutput(string command,
            string arguments, string workingDirectory)
        {
            int exitCode = 0;
            StringBuilder sb = new StringBuilder();
            using (StringWriter sw = new StringWriter(sb, CultureInfo.InvariantCulture))
            {
                exitCode = RunCommand(command, arguments, workingDirectory, sw, null, null);
            }

            ThrowExceptionForInvalidExitCode(command, arguments, exitCode);

            return sb.ToString();
        }

        /// <summary>
        /// Run command in shell, and retrieve the output and error of the target process.
        /// </summary>
        /// <param name="command">Command to run.</param>
        /// <param name="arguments">Argument string.</param>
        /// <param name="workingDirectory">Working directory.</param>
        /// <param name="log">Log including output and error.</param>
        /// <returns>Exit code.</returns>
        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        public static int RunCommandWithOutputAndError(string command,
            string arguments, string workingDirectory, ref string log)
        {
            int exitCode = 0;
            StringBuilder sb = new StringBuilder();
            using (StringWriter sw = new StringWriter(sb, CultureInfo.InvariantCulture))
            {
                exitCode = RunCommand(command, arguments, workingDirectory, sw, sw, null);
            }

            log = sb.ToString();

            return exitCode;
        }

        /// <summary>
        /// Run command line with exit event to control exit.
        /// </summary>
        /// <param name="command">Command to run.</param>
        /// <param name="arguments">Argument string.</param>
        /// <param name="exitEvent">Event to terminate the extern application.</param>
        /// <param name="workingDirectory">Working directory.</param>
        /// <returns>True if extern application return 0, otherwise false.</returns>
        public static bool RunCommand(string command, string arguments,
            WaitHandle exitEvent, string workingDirectory)
        {
            return RunCommand(command, arguments, workingDirectory, null, null, exitEvent) == 0;
        }

        /// <summary>
        /// Command line runner, this fuction will redirect process output to console.
        /// </summary>
        /// <param name="command">Command to run.</param>
        /// <param name="arguments">Argument string.</param>
        /// <param name="workingDirectory">Working directory.</param>
        /// <param name="standardOutput">Standard output writer.</param>
        /// <param name="standardError">Error output writer.</param>
        /// <param name="exitEvent">Event to terminate the extern application.</param>
        /// <returns>Extern application return code.</returns>
        public static int RunCommand(string command, string arguments, string workingDirectory,
            TextWriter standardOutput, TextWriter standardError, WaitHandle exitEvent)
        {
            return RunCommand(command, arguments, workingDirectory,
                defaultOutputEncoding, defaultErrorEncoding,
                standardOutput, standardError,
                exitEvent, false);
        }

        /// <summary>
        /// Command line runner, this fuction will redirect process output to console.
        /// </summary>
        /// <param name="command">Command to run.</param>
        /// <param name="arguments">Argument string.</param>
        /// <param name="workingDirectory">Working directory.</param>
        /// <param name="standardOuputEncoding">Standard output encoding.</param>
        /// <param name="standardErrorEncoding">Standard error encoding.</param>
        /// <param name="standardOutput">Standard output writer.</param>
        /// <param name="standardError">Error output writer.</param>
        /// <param name="exitEvent">Event to terminate the extern application.</param>
        /// <param name="keepBlankLog">Whether keep blank line log.</param>
        /// <returns>Extern application return code.</returns>
        public static int RunCommand(string command, string arguments, string workingDirectory,
            Encoding standardOuputEncoding, Encoding standardErrorEncoding,
            TextWriter standardOutput, TextWriter standardError, WaitHandle exitEvent, bool keepBlankLog)
        {
            using (Stream outputStream = Console.OpenStandardOutput())
            using (Stream errorStream = Console.OpenStandardError())
            using (ManualResetEvent exited = new ManualResetEvent(false))
            {
                TextWriter outputWriter =
                standardOutput == null ? new StreamWriter(outputStream) : standardOutput;
                TextWriter errorWriter =
                standardError == null ? new StreamWriter(errorStream) : standardError;
                
                // Create a delegate to read from StandardOutput
                DataReceivedEventHandler outputDataReceviedHandle =
                    delegate(object sendingProcess, DataReceivedEventArgs outputLine)
                    {
                        if (keepBlankLog ||
                            (!string.IsNullOrEmpty(outputLine.Data) && !string.IsNullOrEmpty(outputLine.Data.Trim())))
                        {
                            outputWriter.WriteLine(outputLine.Data);
                            outputWriter.Flush();
                        }
                    };

                // Create a delegate to read from StandardError
                DataReceivedEventHandler errorDataReceviedHandle =
                    delegate(object sendingProcess, DataReceivedEventArgs errorLine)
                    {
                        if (keepBlankLog ||
                            (!string.IsNullOrEmpty(errorLine.Data) && !string.IsNullOrEmpty(errorLine.Data.Trim())))
                        {
                            errorWriter.WriteLine(errorLine.Data);
                            errorWriter.Flush();
                        }
                    };

                // Create a delegate wait process exit and set exit event.
                EventHandler exitedHandle = delegate(object o, EventArgs e)
                {
                    exited.Set();
                };

                using (Process process = RunCommandHelper(command, arguments, workingDirectory,
                    outputDataReceviedHandle, errorDataReceviedHandle, exitedHandle,
                    standardOuputEncoding, standardErrorEncoding))
                {
                    if (exitEvent == null)
                    {
                        exited.WaitOne();
                    }
                    else
                    {
                        WaitHandle.WaitAny(new WaitHandle[] { exitEvent, exited });
                    }

                    if (!process.HasExited)
                    {
                        try
                        {
                            process.Kill();
                        }
                        catch (InvalidOperationException)
                        {
                            // Process already exit between those two function call "p.HasExited/p.Kill()"
                            process.WaitForExit();  // This line just for avoid presharp warning 56502
                        }
                    }

                    process.WaitForExit();

                    try
                    {
                        if (outputDataReceviedHandle != null)
                        {
                            process.CancelOutputRead();
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        // Ignore invalid option on the process instance
                    }
                    finally
                    {
                        if (standardOutput == null)
                        {
                            outputWriter.Close();
                        }
                    }

                    try
                    {
                        if (errorDataReceviedHandle != null)
                        {
                            process.CancelErrorRead();
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        // Ignore invalid option on the process instance
                    }
                    finally
                    {
                        if (standardError == null)
                        {
                            errorWriter.Close();
                        }
                    }

                    return process.ExitCode;
                }
            }
        }

        #endregion

        #region Private static operations

        /// <summary>
        /// Throw exception if exit code is not zero.
        /// </summary>
        /// <param name="command">Command.</param>
        /// <param name="arguments">Arguments.</param>
        /// <param name="exitCode">Exit code.</param>
        private static void ThrowExceptionForInvalidExitCode(string command, string arguments, int exitCode)
        {
            if (exitCode != 0)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "the following command failed with exit code {0}. {1} {2} {3}",
                    exitCode, Environment.NewLine, command, arguments);
                throw new GeneralException(message);
            }
        }

        /// <summary>
        /// Asynchronously command line runner helper.
        /// </summary>
        /// <param name="command">Command to run.</param>
        /// <param name="arguments">Argument string.</param>
        /// <param name="workingDirectory">Working directory.</param>
        /// <param name="outputDataReceviedHandle">Normal output data reveiver.</param>
        /// <param name="errorDataReceviedHandle">Error output data reveiver.</param>
        /// <param name="exitedHandle">Event to terminate the extern application.</param>
        /// <param name="outputEncoding">Output encoding.</param>
        /// <param name="errorEncoding">Error encoding.</param>
        /// <returns>Process created.</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Ignore.")]
        private static Process RunCommandHelper(string command, string arguments, string workingDirectory,
            DataReceivedEventHandler outputDataReceviedHandle,
            DataReceivedEventHandler errorDataReceviedHandle,
            EventHandler exitedHandle,
            Encoding outputEncoding, Encoding errorEncoding)
        {
            if (string.IsNullOrEmpty(command))
            {
                throw new ArgumentNullException("command");
            }

            if (string.IsNullOrEmpty(arguments))
            {
                arguments = string.Empty;
            }

            if (string.IsNullOrEmpty(workingDirectory))
            {
                workingDirectory = string.Empty;
            }
            else if (!Directory.Exists(workingDirectory))
            {
                throw new ArgumentException(workingDirectory,
                    new DirectoryNotFoundException(workingDirectory));
            }

            if (exitedHandle == null)
            {
                throw new ArgumentNullException("exitedHandle");
            }

            Process process = new Process();
            process.StartInfo.FileName = command;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.WorkingDirectory = workingDirectory;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.EnableRaisingEvents = true;
            process.Exited += exitedHandle;
            process.StartInfo.StandardOutputEncoding = outputEncoding;
            process.StartInfo.StandardErrorEncoding = errorEncoding;

            if (outputDataReceviedHandle != null)
            {
                process.StartInfo.RedirectStandardOutput = true;
                process.OutputDataReceived += outputDataReceviedHandle;
            }

            if (errorDataReceviedHandle != null)
            {
                process.StartInfo.RedirectStandardError = true;
                process.ErrorDataReceived += errorDataReceviedHandle;
            }

            try
            {
                lock (_processStartLock)
                {
                    process.Start();
                }
            }
            catch (System.ComponentModel.Win32Exception we)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "the following command failed to start with error code '{3}':{0} [{1} {2}]",
                    Environment.NewLine, command, arguments, we.ErrorCode);
                throw new GeneralException(message);
            }

            if (outputDataReceviedHandle != null)
            {
                process.BeginOutputReadLine();
            }

            if (errorDataReceviedHandle != null)
            {
                process.BeginErrorReadLine();
            }

            return process;       
        }

        #endregion
    }
}