//----------------------------------------------------------------------------
// <copyright file="Signal.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements signal/command definitions
// </summary>
//----------------------------------------------------------------------------

namespace DistributeComputing
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Basic signal which used across the system.
    /// </summary>
    public enum BasicSignal
    {
        /// <summary>
        /// End socket.
        /// </summary>
        QuitUdpSocket
    }

    /// <summary>
    /// MessageType, as XML document root element tag name.
    /// </summary>
    public enum MessageType
    {
        /// <summary>
        /// Control.
        /// </summary>
        Control,

        /// <summary>
        /// Resource management, mainly on execution node.
        /// </summary>
        Resource,

        /// <summary>
        /// Job management.
        /// </summary>
        JobManage,

        /// <summary>
        /// Status report.
        /// </summary>
        Report,

        /// <summary>
        /// Job submit.
        /// </summary>
        Job
    }

    /// <summary>
    /// Command type.
    /// </summary>
    public enum CommandType
    {
        /// <summary>
        /// None, or undefined command.
        /// </summary>
        Undefined,

        /// <summary>
        /// Node registry command.
        /// </summary>
        Registry,

        /// <summary>
        /// Block node from the grid.
        /// </summary>
        Block,

        /// <summary>
        /// Unblock node.
        /// </summary>
        Unblock,

        /// <summary>
        /// Stop node from working, this means the working thread will be free.
        /// </summary>
        StopWork,

        /// <summary>
        /// Start the working thread of node.
        /// </summary>
        StartWork,

        /// <summary>
        /// Job schedule.
        /// </summary>
        JobSchedule,

        /// <summary>
        /// Job done notification command.
        /// </summary>
        JobDone,

        /// <summary>
        /// Summit job to server.
        /// </summary>
        JobSubmit,

        /// <summary>
        /// Job query for server.
        /// </summary>
        JobQuery,

        /// <summary>
        /// Job status for server.
        /// </summary>
        JobStatus
    }

    /// <summary>
    /// Node reporting type.
    /// </summary>
    public enum ReportType
    {
        /// <summary>
        /// For information.
        /// </summary>
        Info,

        /// <summary>
        /// Encount error.
        /// </summary>
        Error
    }
}