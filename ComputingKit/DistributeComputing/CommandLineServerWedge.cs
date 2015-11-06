//----------------------------------------------------------------------------
// <copyright file="CommandLineServerWedge.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements commandline server-side wedge
// </summary>
//----------------------------------------------------------------------------

namespace DistributeComputing
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Commandline server-side wedge.
    /// </summary>
    public sealed class CommandLineServerWedge : IServerWedge
    {
        #region Fields

        public const string WedgeName = "CommandLine";

        #endregion

        #region IWedge Members

        /// <summary>
        /// Gets Wedge name.
        /// </summary>
        string IWedge.WedgeName
        {
            get { return CommandLineServerWedge.WedgeName; }
        }

        /// <summary>
        /// Execute on wedge.
        /// </summary>
        /// <param name="node">Process node.</param>
        /// <param name="data">Data.</param>
        /// <returns>True if processed, otherwise false.</returns>
        bool IWedge.Execute(IProcessNode node, object data)
        {
            return true;
        }

        /// <summary>
        /// Create a new job item special for this client wedge.
        /// </summary>
        /// <returns>Job instance created.</returns>
        Job IWedge.CreateJob()
        {
            return new Job(((IWedge)this).WedgeName, "null");
        }

        /// <summary>
        /// Clean up temporary resources used by this wedge.
        /// </summary>
        /// <param name="command">Command.</param>
        /// <returns>True if cleaned, otherwise false.</returns>
        bool IWedge.CleanUp(string command)
        {
            return true;
        }

        #endregion
    }
}