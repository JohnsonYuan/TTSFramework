//----------------------------------------------------------------------------
// <copyright file="IClientWedge.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements interface of client wedge
// </summary>
//----------------------------------------------------------------------------

namespace DistributeComputing
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;

    /// <summary>
    /// Interface of client wedge.
    /// </summary>
    public interface IClientWedge : IWedge
    {
        /// <summary>
        /// Client to process a special job item.
        /// </summary>
        /// <param name="job">Job to process.</param>
        /// <param name="exitEvent">Existing event.</param>
        /// <returns>True if processed, otherwise false.</returns>
        bool ProcessJob(Job job, WaitHandle exitEvent);
    }
}