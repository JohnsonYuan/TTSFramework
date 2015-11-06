//----------------------------------------------------------------------------
// <copyright file="ProcessNode.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements process node
// </summary>
//----------------------------------------------------------------------------

namespace DistributeComputing
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Xml;
    using DistributeComputing.Properties;

    /// <summary>
    /// Process node.
    /// </summary>
    public class ProcessNode : IDisposable, DistributeComputing.IProcessNode
    {
        #region Fields

        #region Const Fields

        /// <summary>
        /// Default wedge name.
        /// </summary>
        public const string DefaultWedgeName = CommandLineServerWedge.WedgeName;

        /// <summary>
        /// Default task name.
        /// </summary>
        public const string DefaultTaskName = "default";

        /// <summary>
        /// Falg of succeeded.
        /// </summary>
        protected const string SuccessFlag = "OK";

        /// <summary>
        /// Flag of failed.
        /// </summary>
        protected const string FailFlag = "Fail";

        /// <summary>
        /// Dispatched timeout duration.
        /// </summary>
        protected const long DispatchedTimeout = 1000 * 10;

        /// <summary>
        /// Job running timeout duration.
        /// </summary>
        protected const long JobRunningTimeout = 1000 * 60 * 60;

        #endregion

        // monitoring and notifying corresponding nodes
        private Thread _activeMonitorThread;

        // processing incoming messages
        private Thread _messageProcessThread;

        // scheduling processing
        private Thread _jobScheduleThread;

        // job executing processing
        private Thread _workingThread;

        private Dictionary<string, Job> _localToDoJobs = new Dictionary<string, Job>();
        private Dictionary<string, Job> _schedulingJobs = new Dictionary<string, Job>();
        private Dictionary<string, Job> _doneJobs = new Dictionary<string, Job>();

        private ManualResetEvent _exitEvent = new ManualResetEvent(false);
        private ManualResetEvent _jobExitEvent = new ManualResetEvent(false);

        private UdpClient _udpReceiver;
        private UdpClient _udpSender = new UdpClient();

        private Queue<string> _receivedMessages = new Queue<string>();
        private Dictionary<string, int> _runedCommands = new Dictionary<string, int>();

        #region Corresponding nodes, which communicate with this node

        private Dictionary<string, NodeInfo> _correspondingNodes = new Dictionary<string, NodeInfo>();
        private Dictionary<string, NodeInfo> _blockNodes = new Dictionary<string, NodeInfo>();
        private Collection<IWedge> _wedges = new Collection<IWedge>();

        #endregion

        #region node information

        private NodeType _nodeType;
        private NodeInfo _selfInfo;
        private string _aggregatorIP;
        private int _aggregatorPort;

        #endregion

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessNode"/> class.
        /// </summary>
        /// <param name="aggregatorIP">Aggregator IP address string.</param>
        /// <param name="aggregatorPort">Aggregator listening port.</param>
        /// <param name="nodeType">Node type.</param>
        public ProcessNode(string aggregatorIP, int aggregatorPort,
            NodeType nodeType)
        {
            if (string.IsNullOrEmpty(aggregatorIP))
            {
                throw new ArgumentNullException("aggregatorIP");
            }

            NodeAddedEvent = delegate
            {
            };

            NodeRemovedEvent = delegate
            {
            };

            NodeUpdatedEvent = delegate
            {
            };

            NodeReportedEvent = delegate
            {
            };

            MessageReceivedEvent = delegate
            {
            };

            MessageSentEvent = delegate
            {
            };

            _aggregatorIP = aggregatorIP;
            _aggregatorPort = aggregatorPort;
            _nodeType = nodeType;

            HookEvents();
        }

        #endregion

        #region Events

        /// <summary>
        /// Node added event.
        /// </summary>
        public event EventHandler<NodeInfoEventArgs> NodeAddedEvent;

        /// <summary>
        /// Node removed event.
        /// </summary>
        public event EventHandler<NodeInfoEventArgs> NodeRemovedEvent;

        /// <summary>
        /// Node updated event.
        /// </summary>
        public event EventHandler<NodeInfoEventArgs> NodeUpdatedEvent;

        /// <summary>
        /// Node reported event.
        /// </summary>
        public event EventHandler<NodeInfoEventArgs> NodeReportedEvent;

        /// <summary>
        /// Message received event.
        /// </summary>
        public event EventHandler<UdpMessageEventArgs> MessageReceivedEvent;

        /// <summary>
        /// Message sent event.
        /// </summary>
        public event EventHandler<UdpMessageEventArgs> MessageSentEvent;

        #endregion

        #region Properties

        /// <summary>
        /// Gets IClientWedges.
        /// </summary>
        public Collection<IWedge> IWedges
        {
            get { return _wedges; }
        }

        /// <summary>
        /// Gets Node collection.
        /// </summary>
        public Dictionary<string, NodeInfo> Nodes
        {
            get { return _correspondingNodes; }
        }

        /// <summary>
        /// Gets Node collection blocked from function.
        /// </summary>
        public Dictionary<string, NodeInfo> BlockedNodes
        {
            get { return _blockNodes; }
        }

        /// <summary>
        /// Gets Job collection, which has already done.
        /// </summary>
        public Dictionary<string, Job> DoneJobs
        {
            get { return _doneJobs; }
        }

        /// <summary>
        /// Gets Job collection to schedule.
        /// </summary>
        public Dictionary<string, Job> ToScheduleJobs
        {
            get { return _schedulingJobs; }
        }

        /// <summary>
        /// Gets or sets IP address of aggregator.
        /// </summary>
        public string AggregatorIP
        {
            get
            {
                return _aggregatorIP;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _aggregatorIP = value;
            }
        }

        /// <summary>
        /// Gets or sets Port of aggregator server.
        /// </summary>
        public int AggregatorPort
        {
            get { return _aggregatorPort; }
            set { _aggregatorPort = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether Busy status of this node.
        /// </summary>
        public bool Busy
        {
            get { return _selfInfo.Busy; }
            set { _selfInfo.Busy = value; }
        }

        /// <summary>
        /// Gets This node information.
        /// </summary>
        public NodeInfo SelfInfo
        {
            get
            {
                if (_selfInfo == null)
                {
                    _selfInfo = new NodeInfo();

                    _selfInfo.Name = System.Environment.MachineName;
                    _selfInfo.IP = SocketHelper.LocalIP;
                    _selfInfo.Port = ListenPort;

                    // default
                    _selfInfo.NodeType = _nodeType;
                }

                return _selfInfo;
            }
        }

        /// <summary>
        /// Gets Which port does this node udp receiver listen for.
        /// </summary>
        public virtual int ListenPort
        {
            get { return 0; }
        }

        #endregion

        #region Public procedures

        /// <summary>
        /// Send message.
        /// </summary>
        /// <param name="client">UDP client used to send.</param>
        /// <param name="message">Message.</param>
        /// <param name="ip">Target IP address.</param>
        /// <param name="port">Target port.</param>
        public static void SendMessage(UdpClient client, string message, string ip, int port)
        {
            if (client == null)
            {
                throw new ArgumentNullException("client");
            }

            byte[] data = Encoding.Unicode.GetBytes(message);
            try
            {
                client.Send(data, data.Length, ip, port);
            }
            catch (SocketException se)
            {
                Console.Error.WriteLine("Error for sending message {0} to {1}:{2} \r\n{3}\r\n{4}",
                    message, ip, port, se.Message, se.StackTrace);
                Thread.Sleep(200);

                // resend it
                client.Send(data, data.Length, ip, port);
            }
        }

        /// <summary>
        /// Block node.
        /// </summary>
        /// <param name="id">Node id.</param>
        public void BlockNode(string id)
        {
            lock (_correspondingNodes)
            {
                if (!_correspondingNodes.ContainsKey(id))
                {
                    return;
                }

                ControlMessage cm = new ControlMessage(CommandType.Block.ToString(), "null", string.Empty);
                string message = cm.ToXml(_correspondingNodes[id]);
                SendToAll(message);
            }

            lock (_schedulingJobs)
            {
                foreach (Job job in _schedulingJobs.Values)
                {
                    if (job.RunningNode != null && job.RunningNode.Id == id)
                    {
                        // reschedule this job
                        job.Status = JobStatus.Nonscheduled;
                        Console.WriteLine("{0} Job {1}, which runs on removed node {2}, is recheduled.",
                            DateTime.Now.ToShortTimeString(), job.Name, id);
                    }
                }
            }
        }

        /// <summary>
        /// Unblock node.
        /// </summary>
        /// <param name="id">Node id.</param>
        public void UnblockNode(string id)
        {
            lock (_blockNodes)
            {
                if (!_blockNodes.ContainsKey(id))
                {
                    return;
                }

                ControlMessage cm = new ControlMessage(CommandType.Unblock.ToString(), "null", string.Empty);
                string message = cm.ToXml(_blockNodes[id]);
                SendToAll(message);
            }
        }

        /// <summary>
        /// Set working status.
        /// </summary>
        /// <param name="id">Node id.</param>
        /// <param name="status">Busy status.</param>
        public void SetWorkingStatus(string id, bool status)
        {
            lock (_correspondingNodes)
            {
                CommandType command = status ? CommandType.StartWork : CommandType.StopWork;
                if (_correspondingNodes.ContainsKey(id))
                {
                    NodeInfo ni = _correspondingNodes[id];

                    ControlMessage cm = new ControlMessage(command.ToString(), "null", string.Empty);
                    string message = cm.ToXml(ni);
                    SendUdpMessage(message, ni.IP, ni.Port);
                }
            }
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Dispose resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            System.GC.SuppressFinalize(this);
        }

        #endregion

        #region IProcessNode Members

        /// <summary>
        /// Enqueue a finished job.
        /// </summary>
        /// <param name="job">Job to enqueue.</param>
        public void EnqueueDoneJob(Job job)
        {
            if (job == null)
            {
                throw new ArgumentNullException("job");
            }

            lock (_schedulingJobs)
            {
                if (_schedulingJobs.ContainsValue(job))
                {
                    _schedulingJobs.Remove(job.Guid);
                }
            }

            lock (_localToDoJobs)
            {
                if (_localToDoJobs.ContainsValue(job))
                {
                    _localToDoJobs.Remove(job.Guid);
                }
            }

            lock (_doneJobs)
            {
                job.Status = JobStatus.Done;

                if (!_doneJobs.ContainsKey(job.Guid))
                {
                    _doneJobs.Add(job.Guid, job);
                }
            }
        }

        /// <summary>
        /// Send query command to a target machine.
        /// </summary>
        /// <param name="ip">IP address of the target machine.</param>
        /// <param name="port">Port of the target machine.</param>
        public void Query(string ip, int port)
        {
            XmlDocument dom = new XmlDocument();
            XmlElement ele = dom.CreateElement(MessageType.JobManage.ToString());
            ele.SetAttribute("command", CommandType.JobQuery.ToString());

            XmlElement nodeEle = SelfInfo.ToXml(dom);
            ele.AppendChild(nodeEle);

            SendUdpMessage(ele.OuterXml, ip, port);
        }

        /// <summary>
        /// Enqueue a job into scheduling.
        /// </summary>
        /// <param name="job">Job to enqueue.</param>
        public void EnqueueToScheduleJob(Job job)
        {
            if (job == null)
            {
                throw new ArgumentNullException("job");
            }

            lock (_schedulingJobs)
            {
                if (!_schedulingJobs.ContainsKey(job.Guid))
                {
                    _schedulingJobs.Add(job.Guid, job);
                }
            }
        }

        /// <summary>
        /// Reset jobs.
        /// </summary>
        public void ResetJobs()
        {
            lock (_schedulingJobs)
            {
                _schedulingJobs.Clear();
            }

            lock (_localToDoJobs)
            {
                _localToDoJobs.Clear();
            }

            ResetDoneJobs();
        }

        /// <summary>
        /// Start node.
        /// </summary>
        public void Start()
        {
            BeforeStart();

            SelfInfo.Port = ListenPort;
            _udpReceiver = new UdpClient(ListenPort);
            _udpReceiver.BeginReceive(new AsyncCallback(ReceivePackageProcedure), this);

            _activeMonitorThread = new Thread(new ThreadStart(MonitorProcedure));
            _activeMonitorThread.Start();

            _messageProcessThread = new Thread(new ThreadStart(MessageDispatchProcedure));
            _messageProcessThread.Start();

            _workingThread = new Thread(new ThreadStart(LocalJobProcessProcedure));
            _workingThread.Priority = Thread.CurrentThread.Priority;
            _workingThread.Start();

            _jobScheduleThread = new Thread(new ThreadStart(ScheduleJobProcedure));
            _jobScheduleThread.Start();

            switch (SelfInfo.NodeType)
            {
                case NodeType.Execution:
                    _workingThread.Priority = ThreadPriority.Lowest;
                    break;
                case NodeType.Aggregator:
                    break;
                case NodeType.Coordinator:
                    break;
                default:
                    break;
            }

            AfterStart();
        }

        /// <summary>
        /// Execute with given data.
        /// </summary>
        /// <param name="data">Data.</param>
        public void Execute(object data)
        {
            foreach (IWedge wedge in _wedges)
            {
                wedge.Execute(this, data);
            }
        }

        /// <summary>
        /// Run mode in execution.
        /// </summary>
        public void Run()
        {
            ConsoleCommander commander = new ConsoleCommander(this);
            while (true)
            {
                if (_exitEvent.WaitOne(100, false))
                {
                    break;
                }

                try
                {
                    string command = Console.ReadLine();
                    commander.Handle(command);
                }
                catch (IOException ioe)
                {
                    Console.WriteLine("ProcessNode encounteres error: {0}\r\n{1}",
                        ioe.Message, ioe.StackTrace);
                }
            }
        }

        /// <summary>
        /// Stop the operation of node.
        /// </summary>
        public void Stop()
        {
            _exitEvent.Set();
            _jobExitEvent.Set();

            lock (_localToDoJobs)
            {
                foreach (Job job in _localToDoJobs.Values)
                {
                    OnJobFail(job);
                }
            }

            SendUdpMessage(BasicSignal.QuitUdpSocket.ToString(), SelfInfo.IP, SelfInfo.Port);

            _workingThread.Join(10000);

            AfterStop();

            foreach (string command in _runedCommands.Keys)
            {
                CleanUpLocalTemporaryData(command);
            }
        }

        /// <summary>
        /// Wait for all jobs done.
        /// </summary>
        public void WaitForAllJobsDone()
        {
            while (true)
            {
                if (_localToDoJobs.Count > 0
                    || _schedulingJobs.Count > 0)
                {
                    Thread.Sleep(100);
                }
                else
                {
                    // done
                    break;
                }
            }
        }

        /// <summary>
        /// Report message to all nodes.
        /// </summary>
        /// <param name="message">Message.</param>
        /// <param name="reportType">Reporting type.</param>
        public void Report(string message, string reportType)
        {
            XmlDocument dom = new XmlDocument();
            XmlElement ele = dom.CreateElement(MessageType.Report.ToString());
            ele.SetAttribute("command", reportType);

            ele.SetAttribute("message", message);
            XmlElement nodeEle = SelfInfo.ToXml(dom);
            ele.AppendChild(nodeEle);

            SendToAll(ele.OuterXml);
        }

        #endregion

        #region Protected extentionable methods

        /// <summary>
        /// Hook before node start to work.
        /// </summary>
        protected virtual void BeforeStart()
        {
        }

        /// <summary>
        /// Hook after node start to work.
        /// </summary>
        protected virtual void AfterStart()
        {
        }

        /// <summary>
        /// Hook before node send message to all corresponding nodes.
        /// </summary>
        /// <param name="message">Message.</param>
        protected virtual void BeforeSendToAll(string message)
        {
        }

        /// <summary>
        /// Hook after node stop to work.
        /// </summary>
        protected virtual void AfterStop()
        {
        }

        /// <summary>
        /// Post processing after monitor tick.
        /// </summary>
        protected virtual void AfterMonitorTick()
        {
        }

        /// <summary>
        /// Dispatch message, which is received from other nodes.
        /// </summary>
        /// <param name="dom">XML message.</param>
        /// <param name="message">Message string.</param>
        /// <returns>True if dispatched, otherwise false.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1059:MembersShouldNotExposeCertainConcreteTypes", MessageId = "System.Xml.XmlNode", Justification = "Ignore.")]
        protected virtual bool DispatchMessage(XmlDocument dom, string message)
        {
            return false;
        }

        /// <summary>
        /// Dispatch job, and diterminate how to run this job.
        /// </summary>
        /// <param name="job">Job.</param>
        protected virtual void DispatchJob(Job job)
        {
        }

        /// <summary>
        /// Process this job.
        /// </summary>
        /// <param name="job">Job.</param>
        /// <param name="exitEvent">Exiting event.</param>
        /// <returns>True if processed, otherwise false.</returns>
        protected virtual bool ProcessJob(Job job, ManualResetEvent exitEvent)
        {
            return false;
        }

        /// <summary>
        /// Clean up when job done.
        /// </summary>
        /// <param name="job">Job.</param>
        protected virtual void CleanUpJob(Job job)
        {
        }

        /// <summary>
        /// Failure notification.
        /// </summary>
        /// <param name="job">Job.</param>
        protected virtual void OnJobFail(Job job)
        {
            string message = BuildXmlCommand(CommandType.JobDone.ToString(), job.Guid, FailFlag);
            SendToAll(message);
        }

        #endregion

        #region Job collection management

        /// <summary>
        /// Dequeue job item from local todo job collection.
        /// </summary>
        /// <returns>Job instance.</returns>
        protected Job DequeueLocalToDoJob()
        {
            lock (_localToDoJobs)
            {
                if (_localToDoJobs.Count == 0)
                {
                    return null;
                }

                Job job = null;
                foreach (string guid in _localToDoJobs.Keys)
                {
                    job = _localToDoJobs[guid];
                    break;
                }

                if (job != null)
                {
                    _localToDoJobs.Remove(job.Guid);
                }

                return job;
            }
        }

        #endregion

        #region Command management

        /// <summary>
        /// Build command in xml presentation.
        /// </summary>
        /// <param name="command">Command.</param>
        /// <param name="guid">Guid.</param>
        /// <param name="result">Result.</param>
        /// <returns>XML message.</returns>
        protected string BuildXmlCommand(string command, string guid, string result)
        {
            ControlMessage cm = new ControlMessage(command, guid, result);
            return cm.ToXml(SelfInfo);
        }

        #endregion

        #region Communication operations

        /// <summary>
        /// Send message to all corresponding nodes.
        /// </summary>
        /// <param name="message">Message to send.</param>
        protected void SendToAll(string message)
        {
            BeforeSendToAll(message);

            lock (_correspondingNodes)
            {
                foreach (NodeInfo ci in _correspondingNodes.Values)
                {
                    SendUdpMessage(message, ci.IP, ci.Port);
                }
            }
        }

        /// <summary>
        /// Send message to all node.
        /// </summary>
        /// <param name="message">Message.</param>
        /// <param name="nodeType">Node type.</param>
        protected void SendToAll(string message, NodeType nodeType)
        {
            lock (((IProcessNode)this).Nodes)
            {
                foreach (NodeInfo info in ((IProcessNode)this).Nodes.Values)
                {
                    if (info.NodeType == nodeType)
                    {
                        SendUdpMessage(message, info.IP, info.Port);
                    }
                }
            }
        }

        /// <summary>
        /// Send UDP message to node.
        /// </summary>
        /// <param name="message">Message.</param>
        /// <param name="ip">Target IP address.</param>
        /// <param name="port">Target port.</param>
        protected void SendUdpMessage(string message, string ip, int port)
        {
            SendMessage(_udpSender, message, ip, port);
            MessageSentEvent(this, new UdpMessageEventArgs(message));
        }

        #endregion

        #region Protect operations

        /// <summary>
        /// Find suitable wedge.
        /// </summary>
        /// <param name="wedgeName">Wedge name.</param>
        /// <returns>IWedge.</returns>
        protected IWedge FindSuitableWedge(string wedgeName)
        {
            if (_wedges.Count == 0)
            {
                return null;
            }
            else
            {
                if (wedgeName == "null")
                {
                    return _wedges[0];
                }
                else
                {
                    foreach (IWedge wedge in ((IProcessNode)this).IWedges)
                    {
                        if (wedge.WedgeName == wedgeName)
                        {
                            return wedge;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Dispose resources.
        /// </summary>
        /// <param name="disposing">Disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _udpReceiver.Close();
                _udpSender.Close();
                _exitEvent.Close();
                _jobExitEvent.Close();
            }
        }

        /// <summary>
        /// Scheduling job procedure.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Ignore.")]
        protected void ScheduleJobProcedure()
        {
            Collection<Job> gotoLocalJobs = new Collection<Job>();
            Collection<Job> gotoDispatchJobs = new Collection<Job>();
            while (true)
            {
                try
                {
                    if (_exitEvent.WaitOne(100, false))
                    {
                        break;
                    }
                }
                catch (ObjectDisposedException)
                {
                    // _exitEvent is disposed in other thread, so quit it
                    break;
                }

                ScheduleJob(gotoLocalJobs, gotoDispatchJobs);
            }
        }

        /// <summary>
        /// Local processing job procedure.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Ignore.")]
        protected void LocalJobProcessProcedure()
        {
            while (true)
            {
                if (_exitEvent.WaitOne(100, false))
                {
                    break;
                }

                while (_localToDoJobs.Count != 0)
                {
                    try
                    {
                        Job job = null;
                        lock (_localToDoJobs)
                        {
                            if (_localToDoJobs.Count == 0)
                            {
                                continue;
                            }

                            job = (Job)DequeueLocalToDoJob();
                        }

                        if (job == null)
                        {
                            continue;
                        }

                        try
                        {
                            if (_jobExitEvent.WaitOne(0, false))
                            {
                                OnJobFail(job);
                                string info = string.Format(CultureInfo.InvariantCulture,
                                    Resources.JobStopMessage, job.Name);
                                ((IProcessNode)this).Report(info, ReportType.Info.ToString());
                                job = null;
                                continue;
                            }

                            ((IProcessNode)this).Busy = true;
                            if (!string.IsNullOrEmpty(job.Command))
                            {
                                if (!_runedCommands.ContainsKey(job.Command))
                                {
                                    _runedCommands.Add(job.Command, 1);
                                }
                                else
                                {
                                    _runedCommands[job.Command]++;
                                }
                            }

                            bool result = ProcessJob(job, _jobExitEvent);
                            if (!result)
                            {
                                OnJobFail(job);
                                job = null;
                            }

                            ((IProcessNode)this).Busy = false;
                        }
                        catch (Exception e)
                        {
                            ((IProcessNode)this).Busy = false;
                            Console.Error.WriteLine(e.Message);
                            Console.Error.WriteLine(e.StackTrace);

                            OnJobFail(job);
                            ((IProcessNode)this).Report(e.Message + "\r\n" + e.StackTrace,
                                ReportType.Error.ToString());
                            job = null;

                            if (FilterException(e))
                            {
                                throw;
                            }
                        }

                        if (job != null)
                        {
                            // successed
                            ((IProcessNode)this).EnqueueDoneJob(job);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine("{0} LocalJobProcessProcedure encountered "
                            + "error message {1}.\r\n {2} ",
                        DateTime.Now.ToShortTimeString(), e.Message, e.StackTrace);

                        if (FilterException(e))
                        {
                            throw;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Message dispatching procedure.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Ignore.")]
        protected void MessageDispatchProcedure()
        {
            XmlDocument dom = new XmlDocument();
            while (true)
            {
                if (_exitEvent.WaitOne(100, false))
                {
                    break;
                }

                while (_receivedMessages.Count > 0)
                {
                    string message = null;
                    lock (_receivedMessages)
                    {
                        message = _receivedMessages.Dequeue();
                    }

                    if (string.IsNullOrEmpty(message))
                    {
                        continue;
                    }

                    try
                    {
                        MessageReceivedEvent(this, new UdpMessageEventArgs(message));

                        bool handleDone = false;
                        string command = null;
                        dom.LoadXml(message);
                        MessageType messageType = (MessageType)Enum.Parse(typeof(MessageType),
                            dom.DocumentElement.Name);
                        switch (messageType)
                        {
                            case MessageType.Control:
                                command = dom.DocumentElement.SelectSingleNode("@command").InnerText;
                                handleDone = HandleCommand(dom, command);
                                break;
                            case MessageType.Report:
                                command = dom.DocumentElement.SelectSingleNode("@command").InnerText;
                                handleDone = HandleReport(dom, command);
                                break;
                            default:
                                break;
                        }

                        if (!handleDone)
                        {
                            DispatchMessage(dom, message);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine("{0} MessageDispatchProcedure encountered "
                            + "error message {1}.\r\n {2} \r\n {3} ",
                        DateTime.Now.ToShortTimeString(), e.Message, e.StackTrace, message);

                        if (FilterException(e))
                        {
                            throw;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Monitoring and notification procedure.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Ignore.")]
        protected void MonitorProcedure()
        {
            bool firstIntoFreeStatus = true;
            int index = 0;
            while (true)
            {
                if (_exitEvent.WaitOne(1000, false))
                {
                    break;
                }

                try
                {
                    EnsureBindToAggregatorNode();

                    CheckTimeOutNodes();

                    if (index % 3 == 0)
                    {
                        // 10 seconds for each heart beat
                        string message = BuildXmlCommand(CommandType.Registry.ToString(), SelfInfo.Guid, string.Empty);
                        SendToAll(message);

                        // ((IProcessNode)this).AggregatorIp, ((IProcessNode)this).AggregatorPort);
                    }

                    if (SelfInfo.NodeType == NodeType.Execution && !SelfInfo.Busy)
                    {
                        if (SelfInfo.FreeTime > new TimeSpan(0, 0, 10))
                        {
                            if (firstIntoFreeStatus)
                            {
                                string message = string.Format(CultureInfo.InvariantCulture,
                                    Resources.NodeIdleMessage,
                                    DateTime.Now.ToShortTimeString());
                                Console.Write(message);
                                firstIntoFreeStatus = false;
                            }

                            Console.Write(".");
                        }
                    }
                    else
                    {
                        firstIntoFreeStatus = true;
                    }

                    AfterMonitorTick();
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("{0} MonitorProcedure encountered "
                        + "error message {1}.\r\n {2} ",
                    DateTime.Now.ToShortTimeString(), e.Message, e.StackTrace);

                    if (FilterException(e))
                    {
                        throw;
                    }
                }

                index++;
            }
        }

        /// <summary>
        /// Receiving UDP package procedure.
        /// </summary>
        /// <param name="ar">IAsyncResult.</param>
        protected void ReceivePackageProcedure(IAsyncResult ar)
        {
            ProcessNode client = (ProcessNode)ar.AsyncState;
            IPEndPoint ep = new IPEndPoint(0, 0);
            try
            {
                byte[] data = client._udpReceiver.EndReceive(ar, ref ep);
                string log = UnicodeEncoding.Unicode.GetString(data);
                if (log == BasicSignal.QuitUdpSocket.ToString())
                {
                    client._udpReceiver.Close();
                    client._exitEvent.Set();
                    return;
                }

                lock (client._receivedMessages)
                {
                    client._receivedMessages.Enqueue(log);
                }

                client._udpReceiver.BeginReceive(new AsyncCallback(ReceivePackageProcedure), client);
            }
            catch (SocketException se)
            {
                // Continue listening
                Console.Error.WriteLine(se.Message);
                client._udpReceiver.BeginReceive(new AsyncCallback(ReceivePackageProcedure), client);
            }
            catch (ObjectDisposedException ode)
            {
                Console.Error.WriteLine(ode.Message);
            }
        }

        /// <summary>
        /// Find node information object from xml command.
        /// </summary>
        /// <param name="element">Node information.</param>
        /// <param name="registry">Do registry or not.</param>
        /// <returns>NodeInfo.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1059:MembersShouldNotExposeCertainConcreteTypes", MessageId = "System.Xml.XmlNode", Justification = "Ignore.")]
        protected NodeInfo FindNodeInfo(XmlElement element, bool registry)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            NodeInfo nodeInfo = NodeInfo.Parse(element);
            if (SelfInfo.Id == nodeInfo.Id)
            {
                nodeInfo.Name = SelfInfo.Name;
            }

            NodeInfo ci = null;
            lock (_correspondingNodes)
            {
                if (!_correspondingNodes.ContainsKey(nodeInfo.Id))
                {
                    if (registry)
                    {
                        RegistryNode(nodeInfo);
                    }

                    ci = nodeInfo;
                }
                else
                {
                    ci = _correspondingNodes[nodeInfo.Id];
                }
            }

            ci.Copy(nodeInfo);
            ci.ActiveTime = DateTime.Now;

            return ci;
        }

        #endregion

        #region Private static operations

        /// <summary>
        /// A central function to define fatal exceptions in a maintainable fashion.
        /// </summary>
        /// <param name="e">Exception.</param>
        /// <returns>True if filtered, otherwise false.</returns>
        private static bool FilterException(Exception e)
        {
            if (e == null)
            {
                throw new ArgumentNullException("e");
            }

            return false;
        }

        #endregion

        #region Node management

        /// <summary>
        /// Registry node.
        /// </summary>
        /// <param name="ni">Node info to registry.</param>
        private void RegistryNode(NodeInfo ni)
        {
            lock (_correspondingNodes)
            {
                if (!_correspondingNodes.ContainsKey(ni.Id))
                {
                    _correspondingNodes.Add(ni.Id, ni);
                    NodeAddedEvent(this, new NodeInfoEventArgs(ni));
                }
            }
        }

        /// <summary>
        /// Tell a node is blocked.
        /// </summary>
        /// <param name="info">Node info.</param>
        /// <returns>True if blocked, otherwise false.</returns>
        private bool IsBlockNode(NodeInfo info)
        {
            lock (_blockNodes)
            {
                if (_blockNodes.ContainsKey(info.Id))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Check active timeout nodes.
        /// </summary>
        private void CheckTimeOutNodes()
        {
            Collection<string> nodeToRemove = new Collection<string>();
            lock (_correspondingNodes)
            {
                foreach (NodeInfo ci in _correspondingNodes.Values)
                {
                    long diffMs = (DateTime.Now.Ticks - ci.ActiveTime.Ticks) / 10000;
                    if (diffMs > 1000 * 60 && !ci.Fixed)
                    {
                        nodeToRemove.Add(ci.Id);
                    }
                }

                foreach (string id in nodeToRemove)
                {
                    NodeInfo ni = _correspondingNodes[id];
                    _correspondingNodes.Remove(id);
                    NodeRemovedEvent(this, new NodeInfoEventArgs(ni));
                }
            }
        }

        #endregion

        #region Private operations

        /// <summary>
        /// Scheduling job aligning with goto local jobs and goto dispathing jobs.
        /// </summary>
        /// <param name="gotoLocalJobs">Goto local jobs.</param>
        /// <param name="gotoDispatchJobs">Goto dispathing jobs.</param>
        private void ScheduleJob(Collection<Job> gotoLocalJobs, Collection<Job> gotoDispatchJobs)
        {
            try
            {
                PrepareScheduling(gotoLocalJobs, gotoDispatchJobs);

                foreach (Job job in gotoDispatchJobs)
                {
                    DispatchJob(job);
                }

                // process ongoing jobs
                lock (_localToDoJobs)
                {
                    Collection<Job> rescheduleJobs = new Collection<Job>();
                    foreach (string key in _localToDoJobs.Keys)
                    {
                        Job job = _localToDoJobs[key];
                        if (job.Status == JobStatus.LocalRunning)
                        {
                            if (job.Ticks > ProcessNode.JobRunningTimeout)
                            {
                                // to re-schedule this work item
                                rescheduleJobs.Add(job);
                            }
                        }
                    }

                    foreach (Job job in rescheduleJobs)
                    {
                        _localToDoJobs.Remove(job.Guid);
                        job.Status = JobStatus.Nonscheduled;
                        lock (_schedulingJobs)
                        {
                            if (!_schedulingJobs.ContainsKey(job.Guid))
                            {
                                _schedulingJobs.Add(job.Guid, job);
                            }
                        }
                    }
                }

                // process done jobs
                lock (_doneJobs)
                {
                    foreach (Job job in _doneJobs.Values)
                    {
                        CleanUpJob(job);
                    }
                }

                ResetDoneJobs();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("{0} ScheduleJobProcedure encountered "
                    + "error message {1}.\r\n {2} ",
                DateTime.Now.ToShortTimeString(), e.Message, e.StackTrace);

                if (FilterException(e))
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Prepare scheduling.
        /// </summary>
        /// <param name="gotoLocalJobs">Goto local jobs.</param>
        /// <param name="gotoDispatchJobs">Goto dispathing jobs.</param>
        private void PrepareScheduling(Collection<Job> gotoLocalJobs, Collection<Job> gotoDispatchJobs)
        {
            lock (_schedulingJobs)
            {
                gotoLocalJobs.Clear();
                gotoDispatchJobs.Clear();

                // process un-done jobs
                foreach (string key in _schedulingJobs.Keys)
                {
                    Job job = _schedulingJobs[key];
                    if (job.Status == JobStatus.Nonscheduled)
                    {
                        gotoDispatchJobs.Add(job);
                    }
                    else if (job.Status == JobStatus.Dispatched)
                    {
                        // System.DateTime.Ticks will give you the number of 100-nanosecond intervals
                        if (job.Ticks > ProcessNode.DispatchedTimeout)
                        {
                            // to re-schedule this work item
                            job.Status = JobStatus.Nonscheduled;
                        }
                    }
                    else if (job.Status == JobStatus.RemoteRunning)
                    {
                        if (job.Ticks > ProcessNode.JobRunningTimeout)
                        {
                            // to re-schedule this work item
                            job.Status = JobStatus.Nonscheduled;
                        }
                    }
                    else if (job.Status == JobStatus.LocalRunning)
                    {
                        gotoLocalJobs.Add(job);
                    }
                }

                foreach (Job job in gotoLocalJobs)
                {
                    if (job.Status == JobStatus.LocalRunning)
                    {
                        lock (_localToDoJobs)
                        {
                            if (!_localToDoJobs.ContainsKey(job.Guid))
                            {
                                _localToDoJobs.Add(job.Guid, job);
                            }
                            else
                            {
                                // TODO: error handling here
                            }
                        }
                    }
                    else if (job.Status == JobStatus.Done)
                    {
                        ((IProcessNode)this).EnqueueDoneJob(job);
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false);
                        string message = string.Format(CultureInfo.InvariantCulture,
                        "Unknown job status [{0}] found", job.Status);
                        throw new InvalidDataException(message);
                    }

                    _schedulingJobs.Remove(job.Guid);
                }
            }
        }

        /// <summary>
        /// Handle report.
        /// </summary>
        /// <param name="dom">XML message.</param>
        /// <param name="command">Command.</param>
        /// <returns>True if handled, otherwise false.</returns>
        private bool HandleReport(XmlDocument dom, string command)
        {
            XmlElement eleNode = (XmlElement)dom.DocumentElement.SelectSingleNode("Node");
            NodeInfo ni = FindNodeInfo(eleNode, true);
            if (command == ReportType.Error.ToString())
            {
                string errorMessage = dom.DocumentElement.SelectSingleNode("@message").InnerText;
                Console.WriteLine("{0} Node Reported error from {1} \r\n{2}",
                    DateTime.Now.ToShortTimeString(), ni.ToString(), errorMessage);
                NodeReportedEvent(this, new NodeInfoEventArgs(ni, dom));

                return true;
            }
            else if (command == ReportType.Info.ToString())
            {
                string infoMessage = dom.DocumentElement.SelectSingleNode("@message").InnerText;
                Console.WriteLine("{0} Node Reported info from {1} \r\n{2}",
                    DateTime.Now.ToShortTimeString(), ni.ToString(), infoMessage);
                NodeReportedEvent(this, new NodeInfoEventArgs(ni, dom));

                return true;
            }

            return false;
        }

        /// <summary>
        /// Handle command.
        /// </summary>
        /// <param name="dom">XML message.</param>
        /// <param name="command">Command.</param>
        /// <returns>True if handled, otherwise false.</returns>
        private bool HandleCommand(XmlDocument dom, string command)
        {
            XmlElement eleNode = null;
            NodeInfo ni = null;
            CommandType commandType = (CommandType)Enum.Parse(typeof(CommandType), command);
            switch (commandType)
            {
                case CommandType.Registry:
                    eleNode = (XmlElement)dom.DocumentElement.SelectSingleNode("Node");
                    ni = FindNodeInfo(eleNode, false);
                    if (IsBlockNode(ni))
                    {
                        // Console.WriteLine("{0} Node {1} Blocked from registration.",
                        // DateTime.Now.ToShortTimeString(), ni.ToString());
                    }
                    else
                    {
                        RegistryNode(ni);
                    }

                    return true;
                case CommandType.Block:
                    eleNode = (XmlElement)dom.DocumentElement.SelectSingleNode("Node");
                    ni = FindNodeInfo(eleNode, false);
                    DoBlockNode(ni);
                    Console.WriteLine("{0} Node {1} is blocked.",
                        DateTime.Now.ToShortTimeString(), ni.ToString());
                    return true;
                case CommandType.Unblock:
                    eleNode = (XmlElement)dom.DocumentElement.SelectSingleNode("Node");
                    ni = FindNodeInfo(eleNode, false);
                    lock (_blockNodes)
                    {
                        if (_blockNodes.ContainsKey(ni.Id))
                        {
                            _blockNodes.Remove(ni.Id);
                        }
                    }

                    Console.WriteLine("{0} Node {1} is unblocked.",
                        DateTime.Now.ToShortTimeString(), ni.ToString());
                    return true;
                case CommandType.StartWork:
                    _jobExitEvent.Reset();
                    Console.WriteLine("{0} This Node {1} starts working.",
                        DateTime.Now.ToShortTimeString(), SelfInfo.ToString());
                    return true;
                case CommandType.StopWork:
                    _jobExitEvent.Set();
                    Console.WriteLine("{0} This Node {1} stops working.",
                        DateTime.Now.ToShortTimeString(), SelfInfo.ToString());
                    return true;
                default:
                    break;
            }

            return false;
        }

        /// <summary>
        /// Clean up local temporary data.
        /// </summary>
        /// <param name="command">Command.</param>
        private void CleanUpLocalTemporaryData(string command)
        {
            lock (_wedges)
            {
                foreach (IWedge wedge in _wedges)
                {
                    wedge.CleanUp(command);
                }
            }
        }

        /// <summary>
        /// Performance action to block node.
        /// </summary>
        /// <param name="ni">Node to block.</param>
        private void DoBlockNode(NodeInfo ni)
        {
            lock (_blockNodes)
            {
                if (!_blockNodes.ContainsKey(ni.Id))
                {
                    _blockNodes.Add(ni.Id, ni);
                }
            }

            lock (_correspondingNodes)
            {
                if (_correspondingNodes.ContainsKey(ni.Id))
                {
                    _correspondingNodes.Remove(ni.Id);
                    NodeRemovedEvent(this, new NodeInfoEventArgs(ni, "node is blocked out"));
                }
            }
        }

        /// <summary>
        /// Reset done job collections.
        /// </summary>
        private void ResetDoneJobs()
        {
            lock (_doneJobs)
            {
                _doneJobs.Clear();
            }
        }

        /// <summary>
        /// Re-schedule job for a node.
        /// </summary>
        /// <param name="info">Node info.</param>
        private void RescheduleJobOnNode(NodeInfo info)
        {
            if (info == null)
            {
                return;
            }

            lock (_schedulingJobs)
            {
                if (_schedulingJobs.Values == null)
                {
                    return;
                }

                foreach (Job job in _schedulingJobs.Values)
                {
                    if (job.RunningNode != null && job.RunningNode.Id == info.Id)
                    {
                        job.Status = JobStatus.Nonscheduled;
                    }
                }
            }
        }

        /// <summary>
        /// Ensure current node binds to aggregator node.
        /// </summary>
        private void EnsureBindToAggregatorNode()
        {
            lock (_correspondingNodes)
            {
                if (!_correspondingNodes.ContainsKey(AggregatorIP
                    + ":" + AggregatorPort.ToString(CultureInfo.InvariantCulture)))
                {
                    NodeInfo pni = new NodeInfo();
                    pni.Fixed = true;
                    pni.IP = AggregatorIP;
                    pni.Port = AggregatorPort;
                    pni.Name = "unknowned";
                    pni.Busy = false;
                    pni.ActiveTime = DateTime.Now;
                    pni.NodeType = NodeType.Aggregator;
                    if (!_correspondingNodes.ContainsKey(pni.Id))
                    {
                        _correspondingNodes.Add(pni.Id, pni);
                        NodeAddedEvent(this, new NodeInfoEventArgs(pni));
                    }
                }
            }
        }

        #endregion

        #region Events handling

        /// <summary>
        /// Hook events.
        /// </summary>
        private void HookEvents()
        {
            this.NodeAddedEvent += new EventHandler<NodeInfoEventArgs>(ProcessNode_NodeAddedEvent);
            this.NodeRemovedEvent += new EventHandler<NodeInfoEventArgs>(ProcessNode_NodeRemovedEvent);
        }

        /// <summary>
        /// Handle NodeRemovedEvent event.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event argument.</param>
        private void ProcessNode_NodeRemovedEvent(object sender, NodeInfoEventArgs e)
        {
            Console.WriteLine("{0} Node Removed {1}",
                DateTime.Now.ToShortTimeString(), e.NodeInfo.ToString());
            NodeUpdatedEvent(this, new NodeInfoEventArgs(e.NodeInfo, "node disconnected with this node"));
            e.NodeInfo.StatusUpdatedEvent -=
                new EventHandler<NodeInfoEventArgs>(OnNodeInfoStatusUpdated);
            RescheduleJobOnNode(e.NodeInfo);
        }

        /// <summary>
        /// Handle NodeAddedEvent event.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event argument.</param>
        private void ProcessNode_NodeAddedEvent(object sender, NodeInfoEventArgs e)
        {
            Console.WriteLine("{0} {1} Node Added {2}",
                DateTime.Now.ToShortTimeString(), e.NodeInfo.NodeType, e.NodeInfo.ToString());
            NodeUpdatedEvent(this, new NodeInfoEventArgs(e.NodeInfo, "new node connected with this node"));
            e.NodeInfo.StatusUpdatedEvent +=
                new EventHandler<NodeInfoEventArgs>(OnNodeInfoStatusUpdated);
        }

        /// <summary>
        /// Handle StatusUpdatedEvent event.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event argument.</param>
        private void OnNodeInfoStatusUpdated(object sender, NodeInfoEventArgs e)
        {
            Console.WriteLine("{0} {1} Node Updated {2} for {3}",
                DateTime.Now.ToShortTimeString(), e.NodeInfo.NodeType, e.NodeInfo.ToString(), e.Remark);
            NodeUpdatedEvent(this, e);
        }

        #endregion
    }
}