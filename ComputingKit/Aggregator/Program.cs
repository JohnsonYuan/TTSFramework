//----------------------------------------------------------------------------
// <copyright file="Program.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements Aggregator Tool.
// </summary>
//----------------------------------------------------------------------------

namespace Aggregator
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using DistributeComputing;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// ProcessServer arguments.
    /// </summary>
    [Comment("Aggregator tool manages Distribute Computing resources in the computing kit family.")]
    public class Arguments
    {
        #region Fields

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1051:DoNotDeclareVisibleInstanceFields", Justification = "Ignore.")]
        [Argument("lport", Description = "Local port to listen for coming data packet",
            Optional = false, UsagePlaceholder = "localPort")]
        private int _localPort = 0;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets local port to listen incoming packet.
        /// </summary>
        public int LocalPort
        {
            get { return _localPort; }
            set { _localPort = value; }
        }

        #endregion
    }

    /// <summary>
    /// Program of aggregator.
    /// </summary>
    internal class Program
    {
        #region Operations

        /// <summary>
        /// Main function of aggregator.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <returns>Error code, 0 for succeeded.</returns>
        private static int Main(string[] args)
        {
            return ConsoleApp<Arguments>.Run(args, Process);
        }

        /// <summary>
        /// Running the Aggregator service.
        /// </summary>
        /// <param name="arguments">Aggregator arguments.</param>
        /// <returns>If it finished successfully, then return successful code.</returns>
        private static int Process(Arguments arguments)
        {
            using (DistributeComputing.IProcessNode aggregatorNode
                = new DistributeComputing.AggregatorServer(SocketHelper.LocalIP, arguments.LocalPort))
            {
                aggregatorNode.Start();
                aggregatorNode.Run();
                aggregatorNode.Stop();

                return ExitCode.NoError;
            }
        }

        #endregion
    }
}
