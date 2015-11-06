//----------------------------------------------------------------------------
// <copyright file="CommandLineClientWedge.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements commandline client-side wedge
// </summary>
//----------------------------------------------------------------------------

namespace DistributeComputing
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Security.Permissions;
    using System.Text;
    using System.Threading;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Commandline client-side wedge.
    /// </summary>
    [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
    public class CommandLineClientWedge : IClientWedge
    {
        #region Properties

        /// <summary>
        /// Gets Working directory.
        /// </summary>
        public static string WorkDir
        {
            get
            {
                // some tools can not live with windows path, including whitespaces.
                // So convert this path to DOS 8.3 path
                string workDir = GetMainModuleDir();
                workDir = Path.Combine(Helper.ToShortPath(workDir), "DcWorkPlace");

                return workDir;
            }
        }

        #endregion

        #region IWedge Members

        /// <summary>
        /// Gets Wedge name.
        /// </summary>
        public string WedgeName
        {
            get { return CommandLineServerWedge.WedgeName; }
        }

        /// <summary>
        /// Execute on wedge.
        /// </summary>
        /// <param name="node">Process node.</param>
        /// <param name="data">Data.</param>
        /// <returns>True if processed, otherwise false.</returns>
        public bool Execute(IProcessNode node, object data)
        {
            return true;
        }

        /// <summary>
        /// Create a new job item special for this client wedge.
        /// </summary>
        /// <returns>Job instance created.</returns>
        public Job CreateJob()
        {
            return new Job(((IWedge)this).WedgeName, "null");
        }

        /// <summary>
        /// Clean up temporary resources used by this wedge.
        /// </summary>
        /// <param name="command">Command.</param>
        /// <returns>True if cleaned, otherwise false.</returns>
        public bool CleanUp(string command)
        {
            string localCommand = Map2Local(command);
            if (File.Exists(localCommand))
            {
                string taskDir = Path.GetDirectoryName(localCommand);
                string message = Helper.SafeDelete(taskDir);
                if (!string.IsNullOrEmpty(message))
                {
                    Console.Error.WriteLine("Fail to clean the directory {0} for {1}",
                           taskDir, message);
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region IClientWedge Members

        /// <summary>
        /// Client to process a special job item.
        /// </summary>
        /// <param name="job">Job to process.</param>
        /// <param name="exitEvent">Existing event.</param>
        /// <returns>True if processed, otherwise false.</returns>
        public bool ProcessJob(Job job, WaitHandle exitEvent)
        {
            if (job == null)
            {
                throw new ArgumentNullException("job");
            }

            if (exitEvent == null)
            {
                throw new ArgumentNullException("exitEvent");
            }

            // deploy application to local machine
            Console.WriteLine("{0} Start running job {1}",
                DateTime.Now.ToShortTimeString(), job.Name);
            string newCommandPath = DeployLocal(job.Command);
            bool result = CommandLine.RunCommand(newCommandPath, job.Arguments,
                exitEvent, Environment.CurrentDirectory);
            if (!string.IsNullOrEmpty(job.DoneFile) &&
                !File.Exists(job.DoneFile))
            {
                // testing on the done file, which is a flag indicating the job is done
                result = false;
            }

            Console.WriteLine("{0} Job {1} was {2}",
                DateTime.Now.ToShortTimeString(), job.Name, result ? "successfully done" : "failed");
            return result;
        }

        /// <summary>
        /// Map command to local machine.
        /// </summary>
        /// <param name="command">Command to run.</param>
        /// <returns>Local command string.</returns>
        private static string Map2Local(string command)
        {
            string localCommandPath = command;
            if (File.Exists(command))
            {
                string targetDir = Path.Combine(WorkDir, Path.GetFileNameWithoutExtension(command));
                localCommandPath = Path.Combine(targetDir, Path.GetFileName(command));
            }

            return localCommandPath;
        }

        /// <summary>
        /// Deploy command to local machine.
        /// </summary>
        /// <param name="commandPath">Source command path.</param>
        /// <returns>Local command string.</returns>
        private static string DeployLocal(string commandPath)
        {
            string localCommandPath = commandPath;
            if (File.Exists(commandPath))
            {
                string sourceDir = Path.GetDirectoryName(commandPath);
                string taskDir = Path.Combine(WorkDir, Path.GetFileNameWithoutExtension(commandPath));
                localCommandPath = Path.Combine(taskDir, Path.GetFileName(commandPath));

                if (File.Exists(localCommandPath))
                {
                    DateTime sourceUpdatedTime = File.GetLastWriteTime(commandPath);
                    DateTime localUpdatedTime = File.GetLastWriteTime(localCommandPath);

                    if (sourceUpdatedTime.Ticks == localUpdatedTime.Ticks)
                    {
                        // no update found, skip this
                        return localCommandPath;
                    }

                    // clear up
                    Helper.SafeDelete(taskDir);
                }

                Helper.CopyDirectory(sourceDir, taskDir, true);
            }

            return localCommandPath;
        }

        /// <summary>
        /// Get directory of main module location.
        /// </summary>
        /// <returns>Directory of main module location.</returns>
        private static string GetMainModuleDir()
        {
            string workDir = Process.GetCurrentProcess().MainModule.FileName;
            workDir = Path.GetDirectoryName(workDir);
            return workDir;
        } 

        #endregion
    }
}