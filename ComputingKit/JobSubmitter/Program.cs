//----------------------------------------------------------------------------
// <copyright file="Program.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements Job Submit Tool.
// </summary>
//----------------------------------------------------------------------------

namespace JobSubmitter
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// ProcessServer arguments.
    /// </summary>
    [Comment("JobSubmitter tool submits jobs to ProcessServer.")]
    public class Arguments
    {
        #region Fields

        [Argument("sip", Description = "IP address of Process Server in the computing kit family",
            Optional = false, UsagePlaceholder = "serverIP")]
        private string _serverIp = string.Empty;

        [Argument("sport", Description = "Listening port of Process Server in the computing kit family",
            Optional = false, UsagePlaceholder = "serverPort")]
        private int _serverPort = 0;

        [Argument("exec", Description = "Specifies the location of the tool to execute",
            Optional = false, UsagePlaceholder = "exeToolPath")]
        private string _execPath = string.Empty;

        [Argument("args", Description = "Arguments to be passed for the execution tool",
            Optional = false, UsagePlaceholder = "args")]
        private string _argumentString = string.Empty;

        #endregion

        #region Properties

        /// <summary>
        /// Gets server IP.
        /// </summary>
        public string ServerIp
        {
            get { return _serverIp; }
        }

        /// <summary>
        /// Gets server port.
        /// </summary>
        public int ServerPort
        {
            get { return _serverPort; }
        }

        /// <summary>
        /// Gets execution command path.
        /// </summary>
        public string ExecPath
        {
            get { return _execPath; }
        }

        /// <summary>
        /// Gets arguments string for the command.
        /// </summary>
        public string ArgumentString
        {
            get { return _argumentString; }
        }

        #endregion
    }

    /// <summary>
    /// Job submitter application.
    /// </summary>
    internal class Program
    {
        #region Operations

        /// <summary>
        /// Main function of Job submitter.
        /// </summary>
        /// <param name="args">Argument strings.</param>
        /// <returns>Error code.</returns>
        private static int Main(string[] args)
        {
            return ConsoleApp<Arguments>.Run(args, Process);
        }

        /// <summary>
        /// Execute submit operation.
        /// </summary>
        /// <param name="arguments">Arguments.</param>
        /// <returns>If it finished successfully, then return successful code.</returns>
        private static int Process(Arguments arguments)
        {
            int ret = ExitCode.NoError;

            if (!Regex.Match(arguments.ServerIp, @"\d+\.\d+\.\d+\.\d+").Success)
            {
                Console.WriteLine(@"Invalid ip address, which should be like \d+\.\d+\.\d+\.\d+");

                ret = ExitCode.InvalidArgument;
            }
            else
            {
                string command = arguments.ExecPath;

                using (DistributeComputing.CommandLineSubmitter submitter =
                    new DistributeComputing.CommandLineSubmitter(arguments.ServerIp, arguments.ServerPort))
                {
                    string jobName = Path.GetFileNameWithoutExtension(command);
                    if (string.IsNullOrEmpty(jobName))
                    {
                        jobName = command;
                    }

                    jobName += Guid.NewGuid().ToString();

                    submitter.Submit("default", jobName, command, arguments.ArgumentString, null);
                }
            }

            return ret;
        }

        #endregion
    }
}
