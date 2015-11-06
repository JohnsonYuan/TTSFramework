//----------------------------------------------------------------------------
// <copyright file="ParallelCompuation.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This is a base class of parallel computation
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Cosmos.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// ParallelComputation base class for COSMOS.
    /// </summary>
    public abstract class ParallelComputation_COSMOS : ParallelComputation
    {
        #region Methods

        /// <summary>
        /// The factory to build compuation platform.
        /// </summary>
        /// <param name="mode">The mode of compuation platform.</param>
        /// <param name="preparationWork">The preparation work.</param>
        /// <param name="computationPlatform">The platform for parallel computation.</param>
        /// <param name="xmlConfig">Xml config.</param>
        /// <param name="workingFolder">The working folder.</param>
        public static void CreateComputationPlatform(ComputationPlatform mode, PlatformPreparationWork preparationWork, out ParallelComputation_COSMOS computationPlatform, string xmlConfig = null, string workingFolder = null)
        {
            switch (mode)
            {
                case ComputationPlatform.CosmosPlatform:
                    computationPlatform = new CosmosParallelCompuation(xmlConfig, workingFolder);
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
        public static void CreateComputationPlatform(ComputationPlatform mode, out ParallelComputation_COSMOS comPlatform)
        {
            CreateComputationPlatform(mode, null, out comPlatform);
        }

        #endregion
    }
}