//----------------------------------------------------------------------------
// <copyright file="IProcessNode.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements interface of process node
// </summary>
//----------------------------------------------------------------------------

namespace DistributeComputing
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    /// <summary>
    /// Process node interface.
    /// </summary>
    public interface IProcessNode : IDisposable
    {
        /// <summary>
        /// Gets or sets AggregatorIp.
        /// </summary>
        string AggregatorIP
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets AggregatorPort.
        /// </summary>
        int AggregatorPort
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether Node busy status.
        /// </summary>
        bool Busy
        {
            get;
            set;
        }

        /// <summary>
        /// Gets All corresponding nodes for this node.
        /// </summary>
        Dictionary<string, NodeInfo> Nodes
        {
            get;
        }

        /// <summary>
        /// Gets Correspoding nodes which are blocked.
        /// </summary>
        Dictionary<string, NodeInfo> BlockedNodes
        {
            get;
        }

        /// <summary>
        /// Gets Wedges added into this node.
        /// </summary>
        Collection<IWedge> IWedges
        {
            get;
        }

        /// <summary>
        /// Gets Done jobs collection, indexing by job guid.
        /// </summary>
        Dictionary<string, Job> DoneJobs
        {
            get;
        }

        /// <summary>
        /// Gets Job under schedule.
        /// </summary>
        Dictionary<string, Job> ToScheduleJobs
        {
            get;
        }

        /// <summary>
        /// Enqueue a done job item to queue.
        /// </summary>
        /// <param name="job">Job.</param>
        void EnqueueDoneJob(Job job);

        /// <summary>
        /// Enqueue a job item to schedule.
        /// </summary>
        /// <param name="job">Job.</param>
        void EnqueueToScheduleJob(Job job);

        /// <summary>
        /// Reset all jobs.
        /// </summary>
        void ResetJobs();

        /// <summary>
        /// Wait for all job done.
        /// </summary>
        void WaitForAllJobsDone();

        /// <summary>
        /// Start this node.
        /// </summary>
        void Start();

        /// <summary>
        /// Execute all wedges of this node with the data.
        /// </summary>
        /// <param name="data">Data.</param>
        void Execute(object data);

        /// <summary>
        /// Stop this node.
        /// </summary>
        void Stop();

        /// <summary>
        /// Running.
        /// </summary>
        void Run();

        /// <summary>
        /// Report message.
        /// </summary>
        /// <param name="message">Message.</param>
        /// <param name="reportType">Report type.</param>
        void Report(string message, string reportType);
    }
}