//----------------------------------------------------------------------------
// <copyright file="processserver.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements process server node
// </summary>
//----------------------------------------------------------------------------

namespace DistributeComputing
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Xml;
    using DistributeComputing.Properties;

    /// <summary>
    /// Process server node.
    /// </summary>
    public class ProcessServer : ProcessNode
    {
        #region Constant definition

        /// <summary>
        /// Process server udp message log filename.
        /// </summary>
        public const string MessageLogFileName = "ProcessServerMessages.log";

        #endregion

        #region Fields

        private int _serverPort;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessServer"/> class.
        /// </summary>
        /// <param name="aggregatorIp">Aggregator IP address string.</param>
        /// <param name="aggregatorPort">Aggregator listening port.</param>
        /// <param name="serverPort">Server port.</param>
        public ProcessServer(string aggregatorIp, int aggregatorPort, int serverPort)
            : base(aggregatorIp, aggregatorPort, NodeType.Coordinator)
        {
            _serverPort = serverPort;
            IWedges.Add(new CommandLineServerWedge());

            this.MessageSentEvent += new EventHandler<UdpMessageEventArgs>(ProcessServer_MessageSentEvent);
            this.MessageReceivedEvent +=
                new EventHandler<UdpMessageEventArgs>(ProcessServer_MessageReceivedEvent);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Server listening socket port.
        /// </summary>
        public int ServerPort
        {
            get { return _serverPort; }
            set { _serverPort = value; }
        }

        /// <summary>
        /// Gets Listen port of this server.
        /// </summary>
        public override int ListenPort
        {
            get { return ServerPort; }
        }

        #endregion

        #region Override methods

        /// <summary>
        /// Dispose job.
        /// </summary>
        /// <param name="job">Job to dispose.</param>
        protected override void DispatchJob(Job job)
        {
            // dead lock while _toScheduleJobs with ((IProcessNode)this).Nodes
            lock (((IProcessNode)this).Nodes)
            {
                NodeInfo ci = FindMaxCapabilityNode(((IProcessNode)this).Nodes, job);

                if (ci != null && !ci.Busy && ci.NodeType == NodeType.Execution)
                {
                    Console.WriteLine("{0} Dispatch job {1} out to {2}",
                        DateTime.Now.ToLongTimeString(), Path.GetFileName(job.Name),
                        ci.ToString());

                    string message = job.ToXml();
                    SendUdpMessage(message, ci.IP, ci.Port);
                    job.Status = JobStatus.Dispatched;
                    ci.Busy = true;
                }
            }
        }

        /// <summary>
        /// Dispatch message.
        /// </summary>
        /// <param name="dom">Xml document object to load message.</param>
        /// <param name="message">XML message.</param>
        /// <returns>True if dispatched, otherwise false.</returns>
        protected override bool DispatchMessage(XmlDocument dom, string message)
        {
            dom.LoadXml(message);
            CommandType command = CommandType.Undefined;
            XmlElement eleNode = null;
            NodeInfo pni = null;
            MessageType messageType = (MessageType)Enum.Parse(typeof(MessageType), dom.DocumentElement.Name);
            switch (messageType)
            {
                case MessageType.Control:
                    eleNode = (XmlElement)dom.DocumentElement.SelectSingleNode("Node");
                    pni = FindNodeInfo(eleNode, true);
                    System.Diagnostics.Debug.Assert(pni != null);

                    command = (CommandType)Enum.Parse(typeof(CommandType),
                        dom.DocumentElement.GetAttribute("command"));
                    if (command == CommandType.JobDone)
                    {
                        HandleJobeDoneCommand(dom, pni);
                    }
                    else if (command == CommandType.JobSchedule)
                    {
                        HandleScheduleCommand(dom, pni);
                    }

                    break;
                case MessageType.Resource:
                    // Resource update
                    XmlNodeList xmlNodes = dom.SelectNodes("Resource/Node");
                    foreach (XmlNode xmlNode in xmlNodes)
                    {
                        FindNodeInfo((XmlElement)xmlNode, true);
                    }

                    break;
                case MessageType.JobManage:
                    command = (CommandType)Enum.Parse(typeof(CommandType),
                        dom.DocumentElement.GetAttribute("command"));
                    if (command == CommandType.JobSubmit)
                    {
                        HandleJobSubmit(dom);
                    }
                    else if (command == CommandType.JobQuery)
                    {
                        HandleQueryCommand(dom, ref eleNode, ref pni);
                    }
                    else if (command == CommandType.JobStatus)
                    {
                        eleNode = (XmlElement)dom.DocumentElement.SelectSingleNode("Node");
                        pni = FindNodeInfo(eleNode, false);
                        Console.WriteLine("{0} Node status report: " +
                            "non-scheduled job {1}, dispatched job {2}, running job {3}",
                            DateTime.Now.ToShortTimeString(),
                            dom.DocumentElement.GetAttribute("non-scheduled"),
                            dom.DocumentElement.GetAttribute("dispatched"),
                            dom.DocumentElement.GetAttribute("running"));
                    }

                    break;
                default:
                    break;
            }

            return true;
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

        #region Private methods

        /// <summary>
        /// Not thread safe.
        /// </summary>
        /// <param name="nodes">Node dictionary.</param>
        /// <param name="job">Job.</param>
        /// <returns>Node info.</returns>
        private static NodeInfo FindMaxCapabilityNode(Dictionary<string, NodeInfo> nodes, Job job)
        {
            NodeInfo foundMaxPowerNode = null;
            NodeInfo foundMatchNode = null;
            foreach (NodeInfo ci in nodes.Values)
            {
                if (!ci.Busy && ci.NodeType == NodeType.Execution)
                {
                    if (foundMaxPowerNode == null)
                    {
                        foundMaxPowerNode = ci;
                        continue;
                    }
                    else if (foundMaxPowerNode.DoneCount < ci.DoneCount)
                    {
                        foundMaxPowerNode = ci;
                    }

                    if (ci.LastDoneTaskName == job.TaskName)
                    {
                        if (foundMatchNode == null)
                        {
                            foundMatchNode = ci;
                        }
                        else if (foundMatchNode.DoneCount < ci.DoneCount)
                        {
                            foundMatchNode = ci;
                        }
                    }
                }
            }

            if (foundMatchNode != null)
            {
                return foundMatchNode;
            }
            else
            {
                return foundMaxPowerNode;
            }
        }

        /// <summary>
        /// Log message into log file.
        /// </summary>
        /// <param name="message">Message to log.</param>
        /// <param name="title">Title of the message.</param>
        private static void LogMessage(string message, string title)
        {
            string appDir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            string logFilePath = Path.Combine(appDir, MessageLogFileName);

            if (File.Exists(logFilePath))
            {
                FileInfo fi = new FileInfo(logFilePath);
                if (fi.Length > 1000 * 1000 * 10)
                {
                    fi.Delete();
                }
            }

            string logItem = string.Format(CultureInfo.InvariantCulture,
                Resources.MessageLogFormat,
                DateTime.Now.ToLongDateString(), DateTime.Now.ToLongTimeString(), title, message);
            try
            {
                File.AppendAllText(logFilePath, logItem, Encoding.Unicode);
            }
            catch (IOException ioe)
            {
                Console.WriteLine("Log message encounters error:{0}", ioe.Message);
            }
        }

        /// <summary>
        /// Handle query command.
        /// </summary>
        /// <param name="dom">XML message.</param>
        /// <param name="eleNode">Node in the message.</param>
        /// <param name="pni">Node info.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization",
            "CA1305:SpecifyIFormatProvider", MessageId = "System.int.ToString", Justification = "Ignore.")]
        private void HandleQueryCommand(XmlDocument dom, ref XmlElement eleNode, ref NodeInfo pni)
        {
            eleNode = (XmlElement)dom.DocumentElement.SelectSingleNode("Node");
            string taskName = null;
            if (dom.DocumentElement.HasAttribute("taskName"))
            {
                taskName = dom.DocumentElement.GetAttribute("taskName");
            }

            pni = FindNodeInfo(eleNode, false);

            XmlDocument repDom = new XmlDocument();
            XmlElement repEle = repDom.CreateElement(MessageType.JobManage.ToString());
            repEle.SetAttribute("command", CommandType.JobStatus.ToString());
            repEle.SetAttribute("running", 
                (StatisticsJob(taskName, JobStatus.RemoteRunning) + StatisticsJob(taskName, JobStatus.LocalRunning)).ToString(CultureInfo.InvariantCulture));
            repEle.SetAttribute("non-scheduled",
                StatisticsJob(taskName, JobStatus.Nonscheduled).ToString(CultureInfo.InvariantCulture));
            repEle.SetAttribute("dispatched",
                StatisticsJob(taskName, JobStatus.Dispatched).ToString(CultureInfo.InvariantCulture));
            repEle.AppendChild(SelfInfo.ToXml(repDom));
            SendUdpMessage(repEle.OuterXml, pni.IP, pni.Port);
        }

        /// <summary>
        /// Handle job submittion.
        /// </summary>
        /// <param name="dom">XML message.</param>
        private void HandleJobSubmit(XmlDocument dom)
        {
            XmlElement jobEle = (XmlElement)dom.DocumentElement.SelectSingleNode(MessageType.Job.ToString());
            string taskName = jobEle.GetAttribute("taskName");
            string wedgeName = jobEle.GetAttribute("wedgeName");
            IServerWedge wedge = (IServerWedge)FindSuitableWedge(wedgeName);
            Job job = null;
            if (wedge == null)
            {
                job = new Job("default", taskName);
            }
            else
            {
                job = wedge.CreateJob();
            }

            job.ParseXml(jobEle);

            ((IProcessNode)this).EnqueueToScheduleJob(job);
        }

        /// <summary>
        /// Hanlde schedule command.
        /// </summary>
        /// <param name="dom">XML message.</param>
        /// <param name="pni">Node info.</param>
        private void HandleScheduleCommand(XmlDocument dom, NodeInfo pni)
        {
            string guid = dom.DocumentElement.SelectSingleNode("@guid").InnerText;
            string result = dom.DocumentElement.SelectSingleNode("@result").InnerText;

            lock (((IProcessNode)this).ToScheduleJobs)
            {
                foreach (string key in ((IProcessNode)this).ToScheduleJobs.Keys)
                {
                    Job job = ((IProcessNode)this).ToScheduleJobs[key];
                    if (job.Guid == guid)
                    {
                        if (result == SuccessFlag)
                        {
                            ((IProcessNode)this).ToScheduleJobs[key].Status = JobStatus.RemoteRunning;
                            Console.WriteLine("{0} Dispatch[OK] job {1} out to {2} at {3}:{4}",
                                DateTime.Now.ToLongTimeString(), Path.GetFileName(job.Name),
                                pni.Name, pni.IP, pni.Port);
                            job.RunningNode = pni;
                        }
                        else if (result == FailFlag)
                        {
                            // re-dispatch
                            ((IProcessNode)this).ToScheduleJobs[key].Status = JobStatus.Nonscheduled;
                            Console.WriteLine("{0} Dispatch[Fail] job {1} out to {2} at {3}:{4}",
                                DateTime.Now.ToLongTimeString(), Path.GetFileName(job.Name),
                                pni.Name, pni.IP, pni.Port);
                        }
                        else
                        {
                            System.Diagnostics.Debug.Assert(false);
                            string message = string.Format(CultureInfo.InvariantCulture,
                                "Unknown result flag [{0}] found", result);
                            throw new InvalidDataException(message);
                        }

                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Handle job done command.
        /// </summary>
        /// <param name="dom">XML message.</param>
        /// <param name="pni">Node info.</param>
        private void HandleJobeDoneCommand(XmlDocument dom, NodeInfo pni)
        {
            string guid = dom.DocumentElement.SelectSingleNode("@guid").InnerText;
            string result = dom.DocumentElement.SelectSingleNode("@result").InnerText;

            Collection<Job> doneJobs = new Collection<Job>();
            lock (((IProcessNode)this).ToScheduleJobs)
            {
                foreach (Job job in ((IProcessNode)this).ToScheduleJobs.Values)
                {
                    if (job.Guid == guid)
                    {
                        if (result == SuccessFlag)
                        {
                            if (!string.IsNullOrEmpty(job.DoneFile) 
                                && !File.Exists(job.DoneFile))
                            {
                                job.Status = JobStatus.Nonscheduled;
                                Console.WriteLine("{0} jobDone[Fail] for " +
                                    "doneFile{1} not found in job {2} out to {3} at {4}:{5}",
                                    DateTime.Now.ToLongTimeString(),
                                    job.DoneFile, Path.GetFileName(job.Name),
                                    pni.Name, pni.IP, pni.Port);
                            }
                            else
                            {
                                doneJobs.Add(job);
                                Console.WriteLine("{0} jobDone[OK] {1} out to {2} at {3}:{4}",
                                    DateTime.Now.ToLongTimeString(), Path.GetFileName(job.Name),
                                    pni.Name, pni.IP, pni.Port);
                                pni.DoneCount++;

                                // hack here
                                if (!string.IsNullOrEmpty(job.TaskName))
                                {
                                    pni.LastDoneTaskName = job.TaskName;
                                }
                            }

                            break;
                        }
                        else if (result == FailFlag)
                        {
                            // re-schedule this
                            job.Status = JobStatus.Nonscheduled;
                            Console.WriteLine("{0} jobDone[Fail] {1} out to {2} at {3}:{4}",
                                DateTime.Now.ToLongTimeString(), Path.GetFileName(job.Name),
                                pni.Name, pni.IP, pni.Port);
                            pni.Busy = true;
                            pni.DoneCount--;
                        }
                    }
                }

                foreach (Job job in doneJobs)
                {
                    ((IProcessNode)this).EnqueueDoneJob(job);
                }
            }
        }

        /// <summary>
        /// Statistics job status.
        /// </summary>
        /// <param name="taskName">Task name.</param>
        /// <param name="jobStatus">Job status.</param>
        /// <returns>Count of job with the same status.</returns>
        private int StatisticsJob(string taskName, JobStatus jobStatus)
        {
            int count = 0;
            lock (((IProcessNode)this).ToScheduleJobs)
            {
                foreach (Job job in ((IProcessNode)this).ToScheduleJobs.Values)
                {
                    if (job.Status == jobStatus
                        && (string.IsNullOrEmpty(taskName) || job.TaskName == taskName))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        #endregion

        #region event handling

        /// <summary>
        /// Handle MessageReceivedEvent of ProcessServer.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event argument.</param>
        private void ProcessServer_MessageReceivedEvent(object sender, UdpMessageEventArgs e)
        {
            LogMessage(e.Message, "received");
        }

        /// <summary>
        /// Handle MessageSentEvent of ProcessServer.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event argument.</param>
        private void ProcessServer_MessageSentEvent(object sender, UdpMessageEventArgs e)
        {
            LogMessage(e.Message, "sent");
        }

        #endregion
    }
}