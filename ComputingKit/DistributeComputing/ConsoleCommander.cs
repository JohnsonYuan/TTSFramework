//----------------------------------------------------------------------------
// <copyright file="ConsoleCommander.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements ConsoleCommander
// </summary>
//----------------------------------------------------------------------------

namespace DistributeComputing
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Basic command item iterface.
    /// </summary>
    public interface ICommandItem
    {
        /// <summary>
        /// Gets Name of this command.
        /// </summary>
        string Name
        {
            get;
        }

        /// <summary>
        /// Gets Basic usage.
        /// </summary>
        string Usage
        {
            get;
        }

        /// <summary>
        /// Execution of this command.
        /// </summary>
        /// <param name="items">Arguments to execute.</param>
        /// <returns>True if executed, otherwise false.</returns>
        bool Execute(string[] items);
    }

    /// <summary>
    /// Do list out job or node data.
    /// </summary>
    public class ListItem : ICommandItem, IDisposable
    {
        #region Fields

        private ProcessNode _pn;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="ListItem"/> class.
        /// </summary>
        /// <param name="node">Process node.</param>
        public ListItem(ProcessNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

            _pn = node;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets Name of this command.
        /// </summary>
        public string Name
        {
            get { return "list"; }
        }

        /// <summary>
        /// Gets Basic usage.
        /// </summary>
        public string Usage
        {
            get
            {
                return "list nodes [status]:busy, free\r\n" +
                    "list jobs [status]:Nonscheduled, Dispatched, RemoteRunning, LocalRunning, Done\r\n";
            }
        }

        #endregion

        #region Public operations

        /// <summary>
        /// Execution of this command.
        /// </summary>
        /// <param name="items">Arguments to execute.</param>
        /// <returns>True if executed, otherwise false.</returns>
        public bool Execute(string[] items)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }

            if (items.Length >= 2)
            {
                switch (items[1])
                {
                    case "jobs":
                        ListJobs(items);
                        return true;
                    case "nodes":
                        ListNodes(items);
                        return true;
                    default:
                        break;
                }
            }

            return false;
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Dispose resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose resources.
        /// </summary>
        /// <param name="disposing">Disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // clean up managed resources
                // This is a container object, _pn's ownership is not belong to this object.
                // Don't call _pn.Dispose() in this object.
            }

            // clean up unmanaged resources
        }

        #endregion

        #region Private operations

        /// <summary>
        /// List all jobs.
        /// </summary>
        /// <param name="items">Parameters.</param>
        private void ListJobs(string[] items)
        {
            lock (((IProcessNode)_pn).ToScheduleJobs)
            {
                if (((IProcessNode)_pn).ToScheduleJobs.Count == 0)
                {
                    Console.WriteLine("No job found");
                }
                else
                {
                    foreach (Job job in ((IProcessNode)_pn).ToScheduleJobs.Values)
                    {
                        if (items.Length == 3)
                        {
                            JobStatus status = JobStatus.Nonscheduled;
                            try
                            {
                                status = (JobStatus)Enum.Parse(typeof(JobStatus), items[2]);
                            }
                            catch (ArgumentException ae)
                            {
                                Console.Error.WriteLine("Parse job status error, {0} \r\n {1}",
                                    ae.Message, ae.StackTrace);
                                return;
                            }

                            if (job.Status != status)
                            {
                                continue;
                            }
                        }

                        Console.Write("Task:{0} job:{1} status:{2}",
                            job.TaskName, job.Name, job.Status);
                        if (job.RunningNode != null)
                        {
                            Console.WriteLine(" run on {0}", job.RunningNode.ToString());
                        }
                        else
                        {
                            Console.WriteLine(string.Empty);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// List all nodes.
        /// </summary>
        /// <param name="items">Parameters.</param>
        private void ListNodes(string[] items)
        {
            lock (((IProcessNode)_pn).Nodes)
            {
                if (((IProcessNode)_pn).Nodes.Count == 0)
                {
                    Console.WriteLine("No node found");
                }
                else
                {
                    SortedList<string, NodeInfo> sortedNodes = new SortedList<string, NodeInfo>();
                    foreach (NodeInfo ni in ((IProcessNode)_pn).Nodes.Values)
                    {
                        sortedNodes.Add(ni.Name + "@" + ni.Port.ToString(CultureInfo.InvariantCulture), ni);
                    }

                    Console.WriteLine("Normal nodes:");
                    foreach (string key in sortedNodes.Keys)
                    {
                        NodeInfo ni = sortedNodes[key];
                        if (items.Length == 3)
                        {
                            bool busy = false;
                            if (items[2] == "busy")
                            {
                                busy = true;
                            }
                            else if (items[2] == "free")
                            {
                                busy = false;
                            }
                            else
                            {
                                Console.Error.WriteLine("Unknowned node status [{0}], " +
                                    "which should be busy or free", items[2]);
                                return;
                            }

                            if (ni.Busy != busy)
                            {
                                continue;
                            }
                        }

                        Console.WriteLine("{0} {1} {2} done:{3} jobHandling:{4}",
                            ni.NodeType, ni.ToString(), ni.Busy ? "busy" : "free",
                            ni.DoneCount, ni.JobWorking ? "on" : "off");
                    }
                }
            }

            lock (((IProcessNode)_pn).BlockedNodes)
            {
                if (((IProcessNode)_pn).BlockedNodes.Count > 0)
                {
                    Console.WriteLine("Blocked nodes:");
                    foreach (NodeInfo ni in ((IProcessNode)_pn).BlockedNodes.Values)
                    {
                        Console.WriteLine("{0} {1} {2}",
                            ni.NodeType, ni.ToString(), ni.Busy ? "busy" : "free");
                    }
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Job item.
    /// </summary>
    public class JobItem : ICommandItem, IDisposable
    {
        #region Fields

        private ProcessNode _pn;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="JobItem"/> class.
        /// </summary>
        /// <param name="node">Process node.</param>
        public JobItem(ProcessNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

            _pn = node;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets Name of this command.
        /// </summary>
        public string Name
        {
            get { return "job"; }
        }

        /// <summary>
        /// Gets Basic usage.
        /// </summary>
        public string Usage
        {
            get { return "job reschedule all\r\n"; }
        }

        #endregion

        #region Public operations

        /// <summary>
        /// Execution of this command.
        /// </summary>
        /// <param name="items">Arguments to execute.</param>
        /// <returns>True if executed, otherwise false.</returns>
        public bool Execute(string[] items)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }

            if (items.Length == 3)
            {
                if (items[1] == "reschedule" && items[2] == "all")
                {
                    ControlJob();
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Dispose resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose resources.
        /// </summary>
        /// <param name="disposing">Disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // clean up managed resources
                // This is a container object, _pn's ownership is not belong to this object.
                // Don't call _pn.Dispose() in this object.
            }

            // clean up unmanaged resources
        }

        #endregion

        #region Private operations

        /// <summary>
        /// Control job status.
        /// </summary>
        private void ControlJob()
        {
            lock (((IProcessNode)_pn).ToScheduleJobs)
            {
                if (((IProcessNode)_pn).ToScheduleJobs.Count == 0)
                {
                    Console.WriteLine("No job found");
                }
                else
                {
                    foreach (Job job in ((IProcessNode)_pn).ToScheduleJobs.Values)
                    {
                        job.Status = JobStatus.Nonscheduled;
                        Console.WriteLine("Task:{0} job:{1} re-scheduled",
                            job.TaskName, job.Name);
                    }
                }
            }
        }

        #endregion
}

    /// <summary>
    /// Query.
    /// </summary>
    public class QueryItem : ICommandItem, IDisposable
    {
        #region Fields

        private ProcessNode _pn;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryItem"/> class.
        /// </summary>
        /// <param name="node">Process node.</param>
        public QueryItem(ProcessNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

            _pn = node;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets Command name.
        /// </summary>
        public string Name
        {
            get { return "query"; }
        }

        /// <summary>
        /// Gets Command usaage.
        /// </summary>
        public string Usage
        {
            get { return "query ip:port\r\n"; }
        }

        #endregion

        #region Public operations

        /// <summary>
        /// Execution of this command.
        /// </summary>
        /// <param name="items">Arguments to execute.</param>
        /// <returns>True if executed, otherwise false.</returns>
        public bool Execute(string[] items)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }

            if (items.Length == 2)
            {
                Match m = Regex.Match(items[1], @"(\d+\.\d+\.\d+\.\d+):(\d+)");
                if (!m.Success)
                {
                    Console.WriteLine("{0} is invalid ip point, which " +
                        "should be ip:port, like 10.0.0.1:9980", items[1]);
                    return false;
                }

                string ip = m.Groups[1].Value;
                int port = 0;
                if (int.TryParse(m.Groups[2].Value, NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out port))
                {
                    _pn.Query(ip, port);
                }
                else
                {
                    Console.WriteLine("{0} is not valid integer number", items[2]);
                }
            }

            return false;
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Dispose resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose resources.
        /// </summary>
        /// <param name="disposing">Disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // clean up managed resources
                // This is a container object, _pn's ownership is not belong to this object.
                // Don't call _pn.Dispose() in this object.
            }

            // clean up unmanaged resources
        }

        #endregion
    }

    /// <summary>
    /// Block.
    /// </summary>
    public class BlockItem : ICommandItem, IDisposable
    {
        #region Fields

        private ProcessNode _pn;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="BlockItem"/> class.
        /// </summary>
        /// <param name="node">Process node.</param>
        public BlockItem(ProcessNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

            _pn = node;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets Name of this command.
        /// </summary>
        public string Name
        {
            get { return "block"; }
        }

        /// <summary>
        /// Gets Basic usage.
        /// </summary>
        public string Usage
        {
            get { return "block node ip:port\r\nblock -node ip:port\r\n"; }
        }

        #endregion

        #region Public operations

        /// <summary>
        /// Execution of this command.
        /// </summary>
        /// <param name="items">Arguments to execute.</param>
        /// <returns>True if executed, otherwise false.</returns>
        public bool Execute(string[] items)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }

            if (items.Length == 3)
            {
                if (items[1] == "node")
                {
                    BlockNode(items[2]);
                    return true;
                }
                else if (items[1] == "-node")
                {
                    UnblockNode(items[2]);
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Dispose resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose resources.
        /// </summary>
        /// <param name="disposing">Disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // clean up managed resources
                // This is a container object, _pn's ownership is not belong to this object.
                // Don't call _pn.Dispose() in this object.
            }

            // clean up unmanaged resources
        }

        #endregion

        #region Private operations

        /// <summary>
        /// Performance unblock on a given node.
        /// </summary>
        /// <param name="id">Node id.</param>
        private void UnblockNode(string id)
        {
            if (_pn.SelfInfo.NodeType == NodeType.Aggregator)
            {
                if (id == "all")
                {
                    lock (((IProcessNode)_pn).BlockedNodes)
                    {
                        foreach (NodeInfo ni in ((IProcessNode)_pn).BlockedNodes.Values)
                        {
                            _pn.UnblockNode(ni.Id);
                        }
                    }
                }
                else
                {
                    _pn.UnblockNode(id);
                }
            }
            else
            {
                Console.WriteLine("Only aggregator can use this command");
            }
        }

        /// <summary>
        /// Performance block on a given node.
        /// </summary>
        /// <param name="id">Node id.</param>
        private void BlockNode(string id)
        {
            if (_pn.SelfInfo.NodeType == NodeType.Aggregator)
            {
                if (id == "all")
                {
                    lock (((IProcessNode)_pn).Nodes)
                    {
                        foreach (NodeInfo ni in ((IProcessNode)_pn).Nodes.Values)
                        {
                            if (ni.NodeType == NodeType.Execution)
                            {
                                _pn.BlockNode(ni.Id);
                            }
                        }
                    }
                }
                else
                {
                    _pn.BlockNode(id);
                }
            }
            else
            {
                Console.WriteLine("Only aggregator can use this command");
            }
        }

        #endregion
    }

    /// <summary>
    /// Exit.
    /// </summary>
    public class ExitItem : ICommandItem, IDisposable
    {
        #region Fields

        private ProcessNode _pn;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="ExitItem"/> class.
        /// </summary>
        /// <param name="node">Process node.</param>
        public ExitItem(ProcessNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

            _pn = node;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets Name of this command.
        /// </summary>
        public string Name
        {
            get { return "exit"; }
        }

        /// <summary>
        /// Gets Basic usage.
        /// </summary>
        public string Usage
        {
            get { return "exit\r\n"; }
        }

        #endregion

        #region Public operations

        /// <summary>
        /// Execution of this command.
        /// </summary>
        /// <param name="items">Arguments to execute.</param>
        /// <returns>True if executed, otherwise false.</returns>
        public bool Execute(string[] items)
        {
            ((IProcessNode)_pn).Stop();
            return true;
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Dispose resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose resources.
        /// </summary>
        /// <param name="disposing">Disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // clean up managed resources
                // This is a container object, _pn's ownership is not belong to this object.
                // Don't call _pn.Dispose() in this object.
            }

            // clean up unmanaged resources
        }

        #endregion
    }

    /// <summary>
    /// Help command.
    /// </summary>
    public class HelpItem : ICommandItem
    {
        #region Fields

        private Collection<ICommandItem> _commandItems;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="HelpItem"/> class.
        /// </summary>
        /// <param name="items">Command items.</param>
        public HelpItem(Collection<ICommandItem> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }

            _commandItems = items;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets Command items.
        /// </summary>
        public Collection<ICommandItem> CommandItems
        {
            get { return _commandItems; }
        }

        /// <summary>
        /// Gets Name of this command.
        /// </summary>
        public string Name
        {
            get { return "help"; }
        }

        /// <summary>
        /// Gets Basic usage.
        /// </summary>
        public string Usage
        {
            get { return "help\r\n"; }
        }

        #endregion

        #region Public operations

        /// <summary>
        /// Execution of this command.
        /// </summary>
        /// <param name="items">Arguments to execute.</param>
        /// <returns>True if executed, otherwise false.</returns>
        public bool Execute(string[] items)
        {
            if (_commandItems != null)
            {
                foreach (ICommandItem item in CommandItems)
                {
                    string[] lines = item.Usage.Split(
                        new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string line in lines)
                    {
                        Console.WriteLine(line);
                    }
                }
            }

            return true;
        }

        #endregion
    }

    /// <summary>
    /// Working commad.
    /// </summary>
    public class WorkingItem : ICommandItem, IDisposable
    {
        #region Fields

        private ProcessNode _pn;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkingItem"/> class.
        /// </summary>
        /// <param name="node">Process node.</param>
        public WorkingItem(ProcessNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

            _pn = node;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets Name of this command.
        /// </summary>
        public string Name
        {
            get { return "working"; }
        }

        /// <summary>
        /// Gets Basic usage.
        /// </summary>
        public string Usage
        {
            get { return "working node ip:port\r\n working -node ip:port\r\n"; }
        }

        #endregion

        #region Public operations

        /// <summary>
        /// Execution of this command.
        /// </summary>
        /// <param name="items">Arguments to execute.</param>
        /// <returns>True if executed, otherwise false.</returns>
        public bool Execute(string[] items)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }

            if (items.Length == 3)
            {
                SetNodeWorking(items);
                return true;
            }

            return false;
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Dispose resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose resources.
        /// </summary>
        /// <param name="disposing">Disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // clean up managed resources
                // This is a container object, _pn's ownership is not belong to this object.
                // Don't call _pn.Dispose() in this object.
            }

            // clean up unmanaged resources
        }

        #endregion

        #region Private operations

        /// <summary>
        /// Set node working status.
        /// </summary>
        /// <param name="items">Parameters.</param>
        private void SetNodeWorking(string[] items)
        {
            if (_pn.SelfInfo.NodeType == NodeType.Aggregator)
            {
                bool status = (items[1] == "node") ? true : false;
                if (items[2] == "all")
                {
                    lock (((IProcessNode)_pn).Nodes)
                    {
                        foreach (NodeInfo ni in ((IProcessNode)_pn).Nodes.Values)
                        {
                            _pn.SetWorkingStatus(ni.Id, status);
                        }
                    }
                }
                else
                {
                    _pn.SetWorkingStatus(items[2], status);
                }
            }
            else
            {
                Console.WriteLine("Only aggregator can use this command");
            }
        }

        #endregion
    }

    /// <summary>
    /// ConsoleCommander.
    /// </summary>
    public class ConsoleCommander
    {
        #region Fields

        private Collection<ICommandItem> _commandItems = new Collection<ICommandItem>();
        private HelpItem _helpItem; 

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleCommander"/> class.
        /// </summary>
        /// <param name="processNode">Process node.</param>
        public ConsoleCommander(ProcessNode processNode)
        {
            _helpItem = new HelpItem(_commandItems);

            _commandItems.Add(new ListItem(processNode));
            _commandItems.Add(new JobItem(processNode));
            _commandItems.Add(new BlockItem(processNode));
            _commandItems.Add(new WorkingItem(processNode));
            _commandItems.Add(new ExitItem(processNode));
            _commandItems.Add(new QueryItem(processNode));
            _commandItems.Add(_helpItem);
        }

        #endregion

        #region Public operations

        /// <summary>
        /// Handle command.
        /// </summary>
        /// <param name="command">Command.</param>
        public void Handle(string command)
        {
            DispatchCommand(command);
        }

        /// <summary>
        /// Dispatch command.
        /// </summary>
        /// <param name="command">Command.</param>
        public void DispatchCommand(string command)
        {
            if (string.IsNullOrEmpty(command))
            {
                _helpItem.Execute(null);
                return;
            }

            string[] items = command.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            bool handled = false;
            foreach (ICommandItem commandItem in _commandItems)
            {
                if (items[0] == commandItem.Name)
                {
                    handled = commandItem.Execute(items);
                    if (handled)
                    {
                        return;
                    }
                }
            }
        }

        #endregion
    }
}