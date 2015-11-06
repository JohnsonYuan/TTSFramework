//----------------------------------------------------------------------------
// <copyright file="FlowHandler.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module defines the FlowHandler class and some of helper
//   classes used to assemble the pipeline of training.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Cosmos.FlowEngine
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using Microsoft.Tts.Cosmos.TMOC;
    using Microsoft.Tts.Cosmos.Utility;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.FlowEngine;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// This abstract class is used to organize the pipeline of the FlowHandler.
    /// Every step in FlowHandler will be extended from this class.
    /// </summary>
    [CLSCompliantAttribute(false)]
    public abstract class FlowHandler_Cosmos : FlowHandler
    {
        #region Fields

        /// <summary>
        /// The parallel Cosmos computation platform. 
        /// </summary>
        protected static ParallelComputation_COSMOS paraComputePlatform_Cosmos = null;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes static members of the <see cref="FlowHandler_Cosmos" /> class.
        /// Set the default maximum thread number as the processor number.
        /// </summary>
        static FlowHandler_Cosmos() 
        {
            MaxThreads = Environment.ProcessorCount;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FlowHandler_Cosmos" /> class.
        /// </summary>
        /// <param name="name">The given name of this handler.</param>
        protected FlowHandler_Cosmos(string name)
            : base(name)
        {
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the public Cosmos computation platform.
        /// </summary>
        public static ParallelComputation_COSMOS ParaComputePlatform_Cosmos
        {
            get
            {
                return paraComputePlatform_Cosmos;
            }

            private set
            {
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Return the paltform type.
        /// </summary>
        /// <returns>Computation platform.</returns>
        public static new ComputationPlatform GetParaPlatformType()
        {
            if (paraComputePlatform_Cosmos as CosmosParallelCompuation != null)
            {
                return ComputationPlatform.CosmosPlatform;
            }
            else
            {
                return ComputationPlatform.Unknown;
            }
        }

        /// <summary>
        /// Delete unnecessary file asynchronously.
        /// </summary>
        /// <param name="dataPath">The  full path of target file.</param>
        /// <param name="logger">The logger.</param>
        public static new void DeleteIntermediateDataAsyn(string dataPath, ILogger logger)
        {
            if (!string.IsNullOrEmpty(dataPath) && TmocFile.Exists(dataPath))
            {
                DataDeleteArgument argument = new DataDeleteArgument
                {
                    DataFullPath = dataPath,
                    Description = Helper.NeutralFormat("Delete Intermediate Data: {0}", dataPath),
                    Logger = logger ?? new NullLogger(),
                };

                Thread t = new Thread(new ParameterizedThreadStart(DeleteData));
                t.Start(argument);
            }
        }

        /// <summary>
        /// Get current computation platform .
        /// </summary>
        /// <param name="ifCosmos">Represents if this is a Cosmos operation.</param>
        /// <returns>ComputationPlatform variable .</returns>
        public static ComputationPlatform GetCurrentComputationPlatform(bool ifCosmos)
        {
            // Get ComputationPlatformType, becuase, now, there are two kinds of Computation Platform Type
            // One is COSMOS, the other is Offline
            // We need to judge that this is which one
            ComputationPlatform tempPlatformType = ComputationPlatform.Unknown;

            if (ifCosmos)
            {
                if (FlowHandler_Cosmos.GetParaPlatformType() != ComputationPlatform.Unknown)
                {
                    tempPlatformType = FlowHandler_Cosmos.GetParaPlatformType();
                }
                else
                {
                    tempPlatformType = ComputationPlatform.Unknown;
                }
            }
            else
            {
                if (FlowHandler.GetParaPlatformType() != ComputationPlatform.Unknown)
                {
                    tempPlatformType = FlowHandler.GetParaPlatformType();
                }
                else
                {
                    tempPlatformType = ComputationPlatform.Unknown;
                }
            }

            return tempPlatformType;
        }

        #region Abstract Methods

        /// <summary>
        /// The abstract method ValidateArguments() will be called to perform the validation
        /// For the inputs of this handler.
        /// This method won't be called if this handler is disabled.
        /// </summary>
        protected override void ValidateArguments()
        {
        }

        /// <summary>
        /// The abstract method Execute() will be called to perform the action of this handler.
        /// This method won't be called if this handler is disabled.
        /// </summary>
        protected override void Execute() 
        {
        }

        /// <summary>
        /// The abstract method ValidateResults(bool enable) will be used to fill result generated by this handler
        /// After Execute().
        /// This method will be called whenever this handler is disabled or not.
        /// </summary>
        /// <param name="enable">Indicator to whether flow is enable.</param>
        protected override void ValidateResults(bool enable)
        {
        }

        #endregion

        private static void DeleteData(object arg)
        {
            DataDeleteArgument deleteArg = arg as DataDeleteArgument;
            if (deleteArg == null)
            {
                throw new ArgumentException("Invalid argument in DeleteData method");
            }

            using (DelayedLogger fileWriter = new DelayedLogger(deleteArg.Logger))
            {
                string log = deleteArg.Description;

                if (Microsoft.Tts.Cosmos.TMOC.TmocDirectory.Exists(deleteArg.DataFullPath))
                {
                    Microsoft.Tts.Cosmos.TMOC.TmocDirectory.Delete(deleteArg.DataFullPath);
                }
                else
                {
                    Microsoft.Tts.Cosmos.TMOC.TmocFile.Delete(deleteArg.DataFullPath);
                }

                Console.WriteLine("{0}", log);
                fileWriter.Writer.WriteLine("{0}", log);
            }
        }

        #endregion
    }

    internal class DataDeleteArgument
    {
        public string DataFullPath { get; set; }

        public ILogger Logger { get; set; }

        public string Description { get; set; }

        public override string ToString()
        {
            return DataFullPath;
        }
    }
}