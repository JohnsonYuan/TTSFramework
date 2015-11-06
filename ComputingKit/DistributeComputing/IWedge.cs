//----------------------------------------------------------------------------
// <copyright file="IWedge.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements wedge interface
// </summary>
//----------------------------------------------------------------------------

namespace DistributeComputing
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Wedge interface
    /// Wedge is used to support plug-in component in this system.
    /// </summary>
    public interface IWedge
    {
        /// <summary>
        /// Gets Wedge name.
        /// </summary>
        string WedgeName
        {
            get;
        }

        /// <summary>
        /// Execute on wedge.
        /// </summary>
        /// <param name="node">Process node.</param>
        /// <param name="data">Data.</param>
        /// <returns>True if processed, otherwise false.</returns>
        bool Execute(IProcessNode node, object data);

        /// <summary>
        /// Create a new job item special for this client wedge.
        /// </summary>
        /// <returns>Job instance created.</returns>
        Job CreateJob();

        /// <summary>
        /// Clean up temporary resources used by this wedge.
        /// </summary>
        /// <param name="command">Command.</param>
        /// <returns>True if cleaned, otherwise false.</returns>
        bool CleanUp(string command);
    }
}