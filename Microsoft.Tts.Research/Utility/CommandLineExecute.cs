//----------------------------------------------------------------------------
// <copyright file="CommandLineExecute.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements the command line execution class
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Research
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Security.Permissions;
    using System.Threading;

    /// <summary>
    /// Command line execute class.
    /// <param />
    /// Because Microsoft.Tts.Offline.Utility.CommandLine class can't handle binary output,
    /// So we develop this class instead.
    /// <param />
    /// [TBD] We need to update Microsoft.Tts.Offline.Utility.CommandLine class in the future accordingly.
    /// </summary>
    [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
    public class CommandLineExecute
    {
        /// <summary>
        /// Run a command line tool.
        /// </summary>
        /// <param name="toolPath">Path of the command line tool.</param>
        /// <param name="arguments">Arguments.</param>
        /// <param name="inputFile">Input file, redirect from standard input.</param>
        /// <param name="outputFile">Output file, redirect from standard output.</param>
        /// <param name="error">Error information.</param>
        /// <returns>True if no error, else false.</returns>
        public static bool Execute(string toolPath, string arguments, string inputFile, string outputFile, out string error)
        {
            using (Process p = new Process())
            {
                p.StartInfo.FileName = toolPath;
                p.StartInfo.Arguments = arguments;

                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.Start();

                Stream outputReader = p.StandardOutput.BaseStream;
                using (FileStream outputWriter = new FileStream(outputFile, FileMode.Create))
                {
                    DataRedirect dataRedirectOutput = new DataRedirect(outputReader, outputWriter);
                    Thread thread = new Thread(new ThreadStart(dataRedirectOutput.Process));
                    thread.Start();

                    using (FileStream inputReader = new FileStream(inputFile, FileMode.Open))
                    {
                        Stream inputWriter = p.StandardInput.BaseStream;
                        DataRedirect dataRedirectInput = new DataRedirect(inputReader, inputWriter);

                        dataRedirectInput.Process();

                        inputWriter.Close();
                    }

                    thread.Join();
                }

                outputReader.Close();

                p.WaitForExit();
                error = p.StandardError.ReadToEnd();
            }
    
            return string.IsNullOrEmpty(error) ? true : false;
        }

        private class DataRedirect
        {
            private Stream _reader;
            private Stream _writer;

            public DataRedirect(Stream reader, Stream writer)
            {
                _reader = reader;
                _writer = writer;
            }

            public void Process()
            {
                const int BufLen = 1024;
                byte[] byteBuffer = new byte[BufLen];
                int bytesRead;

                while ((bytesRead = _reader.Read(byteBuffer, 0, BufLen)) > 0)
                {
                    _writer.Write(byteBuffer, 0, bytesRead);
                }
            }
        }
    }
}