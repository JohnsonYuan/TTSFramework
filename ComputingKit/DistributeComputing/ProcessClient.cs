//----------------------------------------------------------------------------
// <copyright file="ProcessClient.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements ProcessClient
// </summary>
//----------------------------------------------------------------------------

namespace DistributeComputing
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Xml;

    /// <summary>
    /// Process client node.
    /// </summary>
    public class ProcessClient : ProcessNode
    {
        #region Fields

        private int _clientPort;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessClient"/> class.
        /// </summary>
        /// <param name="aggregatorIp">Aggregator IP address string.</param>
        /// <param name="aggregatorPort">Aggregator listening port.</param>
        /// <param name="clientPort">Client port.</param>
        public ProcessClient(string aggregatorIp, int aggregatorPort, int clientPort)
            : base(aggregatorIp, aggregatorPort, NodeType.Execution)
        {
            ClientPort = clientPort;

            IWedges.Add(new CommandLineClientWedge());
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Client port.
        /// </summary>
        public int ClientPort
        {
            get { return _clientPort; }
            set { _clientPort = value; }
        }

        /// <summary>
        /// Gets Listening port.
        /// </summary>
        public override int ListenPort
        {
            get { return ClientPort; }
        }

        #endregion

        #region Override methods

        /// <summary>
        /// Clean up when job done.
        /// </summary>
        /// <param name="job">Job.</param>
        protected override void CleanUpJob(Job job)
        {
            string message = BuildXmlCommand(CommandType.JobDone.ToString(), job.Guid, SuccessFlag);
            SendToAll(message);
        }

        /// <summary>
        /// Dispatch message, which is received from other nodes.
        /// </summary>
        /// <param name="dom">XML message.</param>
        /// <param name="message">Message string.</param>
        /// <returns>True if dispatched, otherwise false.</returns>
        protected override bool DispatchMessage(XmlDocument dom, string message)
        {
            string outMessage = null;
            dom.LoadXml(message);
            MessageType messageType = (MessageType)Enum.Parse(typeof(MessageType), dom.DocumentElement.Name);
            switch (messageType)
            {
                case MessageType.Job:
                    string wedgeName = dom.DocumentElement.GetAttribute("wedgeName");
                    Job item = CreateJob(wedgeName);
                    item.ParseXml(message);
                    if (!((IProcessNode)this).Busy)
                    {
                        ((IProcessNode)this).Busy = true;
                        ((IProcessNode)this).EnqueueToScheduleJob(item);
                        outMessage =
                            BuildXmlCommand(CommandType.JobSchedule.ToString(), item.Guid, SuccessFlag);
                    }
                    else
                    {
                        outMessage =
                            BuildXmlCommand(CommandType.JobSchedule.ToString(), item.Guid, FailFlag);
                    }

                    SendToAll(outMessage);
                    return true;
                default:
                    Console.WriteLine("unknowed message {0}", message);
                    break;
            }

            return false;
        }

        /// <summary>
        /// Dispatch job, and diterminate how to run this job.
        /// </summary>
        /// <param name="job">Job.</param>
        protected override void DispatchJob(Job job)
        {
            job.Status = JobStatus.LocalRunning;
        }

        /// <summary>
        /// Failure notification.
        /// </summary>
        /// <param name="job">Job.</param>
        protected override void OnJobFail(Job job)
        {
            string message = BuildXmlCommand(CommandType.JobDone.ToString(), job.Guid, FailFlag);
            SendToAll(message);
        }

        /// <summary>
        /// Process this job.
        /// </summary>
        /// <param name="job">Job.</param>
        /// <param name="exitEvent">Exiting event.</param>
        /// <returns>True if processed, otherwise false.</returns>
        protected override bool ProcessJob(Job job, ManualResetEvent exitEvent)
        {
            IWedge wedge = FindSuitableWedge(job.WedgeName);
            if (wedge == null)
            {
                return false;
            }
            else
            {
                IClientWedge clientWedge = wedge as IClientWedge;
                if (clientWedge == null)
                {
                    return false;
                }
                else
                {
                    return clientWedge.ProcessJob(job, exitEvent);
                }
            }
        }

        /// <summary>
        /// Dispose resources.
        /// </summary>
        /// <param name="disposing">Disposing.</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        #endregion

        #region Client specialized methods

        /// <summary>
        /// Create a new job instance from wedge name.
        /// </summary>
        /// <param name="wedgeName">Wedge name.</param>
        /// <returns>Job instance.</returns>
        protected Job CreateJob(string wedgeName)
        {
            IWedge wedge = FindSuitableWedge(wedgeName);
            if (wedge == null)
            {
                // use default job
                return new Job(CommandLineServerWedge.WedgeName, "null");
            }
            else
            {
                IClientWedge clientWedge = wedge as IClientWedge;
                if (clientWedge == null)
                {
                    return new Job(CommandLineServerWedge.WedgeName, "null");
                }
                else
                {
                    return clientWedge.CreateJob();
                }
            }
        }

        #endregion
    }
}