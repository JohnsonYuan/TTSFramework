//----------------------------------------------------------------------------
// <copyright file="ParallelCompuation.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This is a base class of parallel computation
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>
    /// Parallel Computation Enumeration.
    /// </summary>
    public enum ComputationPlatform
    {
        /// <summary>
        /// The platform for multithread compuation.
        /// </summary>
        MultiThreadPlatform,

        /// <summary>
        /// The platform for HPC compuation.
        /// </summary>
        HPCPlatform,

        /// <summary>
        /// The platform for single machine compuation.
        /// </summary>
        SingleMachinePlatform,

        /// <summary>
        /// The platform for COSMOS conmputation.
        /// </summary>
        CosmosPlatform,

        /// <summary>
        /// Unknown platform.
        /// </summary>
        Unknown
    }

    /// <summary>
    /// ParallelComputation base class.
    /// </summary>
    public abstract class ParallelComputation
    {
        /// <summary>
        /// The delegated preaparation work for computation platform.
        /// </summary>
        public delegate void PlatformPreparationWork();

        #region Properties

        /// <summary>
        /// Gets or sets the description for log for the parallel parameter.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the ILogger interface for the parallel parameter.
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Gets or sets the exception happened inner the thread for the parallel parameter.
        /// </summary>
        public Exception InnerException { get; protected set; }

        #endregion

        #region Methods

        /// <summary>
        /// The factory to build compuation platform.
        /// </summary>
        /// <param name="mode">The mode of compuation platform.</param>
        /// <param name="preparationWork">The preparation work.</param>
        /// <param name="computationPlatform">The platform for parallel computation.</param>
        /// <param name="xmlConfig">Xml config.</param>
        /// <param name="workingFolder">The working folder.</param>
        public static void CreateComputationPlatform(ComputationPlatform mode, PlatformPreparationWork preparationWork, out ParallelComputation computationPlatform, string xmlConfig = null, string workingFolder = null)
        {
            switch (mode)
            {
                case ComputationPlatform.MultiThreadPlatform:
                    computationPlatform = new MultiThreadParallelComputation();
                    break;
                case ComputationPlatform.SingleMachinePlatform:
                    computationPlatform = new SingleMachineComputation();
                    break;
                case ComputationPlatform.HPCPlatform:
                    computationPlatform = new HPCParallelComputaion();
                    break;
                default:
                    throw new ArgumentException("The invalid computation platform is given");
            }

            if (preparationWork != null)
            {
                preparationWork();
            }
        }

        /// <summary>
        /// Overload CreateComputationPlatform(ComputationPlatform mode, PlatformPreparationWork preparationWork) with the second para as null.
        /// </summary>
        /// <param name="mode">The mode of compuation platform.</param>
        /// <param name="comPlatform">The platform for parallel computation.</param>
        public static void CreateComputationPlatform(ComputationPlatform mode, out ParallelComputation comPlatform)
        {
            CreateComputationPlatform(mode, null, out comPlatform);
        }

        /// <summary>
        /// The specific execution.
        /// </summary>
        /// <returns>Bool.</returns>
        public bool Execute()
        {
            bool isSuccess = true;
            isSuccess = Initialize();

            if (isSuccess)
            {
                isSuccess = BroadCast();
            }

            if (isSuccess)
            {
                isSuccess = Reduce();
            }

            if (isSuccess)
            {
                isSuccess = ValidateResult();
            }

            return isSuccess;
        }

        /// <summary>
        /// The Initialize() methods to initalize the necessary configuration.
        /// </summary>
        /// <returns>Bool.</returns>
        protected virtual bool Initialize()
        {
            return true;
        }

        /// <summary>
        /// The BroadCast() methods to broadcast all task.
        /// </summary>
        /// <returns>Bool.</returns>
        protected virtual bool BroadCast()
        {
            return true;
        }

        /// <summary>
        /// The Reduce() methods to reduce all result to one file.
        /// </summary>
        /// <returns>Bool.</returns>
        protected virtual bool Reduce()
        {
            return true;
        }

        /// <summary>
        /// The ValidateResult() methods to validate the reuslt file.
        /// </summary>
        /// <returns>Bool.</returns>
        protected virtual bool ValidateResult()
        {
            return true;
        }

        #endregion
    }
}