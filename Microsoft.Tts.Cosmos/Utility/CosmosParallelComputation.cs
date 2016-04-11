// ----------------------------------------------------------------------------
// <copyright file="CosmosParallelComputation.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// 
// <summary>
//     This is the class to invoke multi thread parallel compuation
// </summary>
// ----------------------------------------------------------------------------

namespace Microsoft.Tts.Cosmos.Utility
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using Microsoft.Tts.Cosmos.TMOC;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// The class of CosmosParallelCompuation.
    /// </summary>
    public class CosmosParallelCompuation : ParallelComputation_COSMOS
    {
        #region Fields
        private readonly TmocGlobal _instance = TmocGlobal.Instance;
        private string _jobScript = string.Empty;
        private string _jobName = string.Empty;
        private string _jobDisplayName = string.Empty;
        private int _nebularArg = 0;
        private int _tokens = 0;
        private int _defaultTokens = 0;
        private string _backupDir = null;
        private bool? _isLocalRun = null;
        #endregion

        #region constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosParallelCompuation" /> class. The constuctor for CosmosParallelCompuation class.
        /// </summary>
        /// <param name="inputConfig">Input config.</param>
        /// <param name="workingDir">Working folder.</param>
        public CosmosParallelCompuation(string inputConfig, string workingDir)
        {
            if (!string.IsNullOrEmpty(inputConfig) && File.Exists(inputConfig))
            {
                _instance.Initalize(inputConfig, workingDir);
            }
            else
            {
                throw new ArgumentException(string.Format("{0} config file doesn't existed", inputConfig));
            }
        }

        #endregion

        #region Property

        /// <summary>
        /// Gets or sets the script of the job.
        /// </summary>
        public string JobScript
        {
            get
            {
                return _jobScript;
            }

            set
            {
                _jobScript = value;
            }
        }

        /// <summary>
        /// Gets or sets the name of the job.
        /// </summary>
        public string JobName
        {
            get
            {
                return _jobName;
            }

            set
            {
                _jobName = value;
            }
        }

        /// <summary>
        /// Gets or sets the display of the job name.
        /// </summary>
        public string JobDisplayName
        {
            get
            {
                return _jobDisplayName;
            }

            set
            {
                _jobDisplayName = value;
            }
        }

        /// <summary>
        /// Gets or sets the argument of Nebular.
        /// </summary>
        public int NebularArgument
        {
            get
            {
                return _nebularArg;
            }

            set
            {
                _nebularArg = value;
            }
        }

        /// <summary>
        /// Gets or sets flag represent whether to run the job locally.
        /// </summary>
        public bool? IsLocalRun
        {
            get
            {
                return _isLocalRun;
            }

            set
            {
                _isLocalRun = value;
            }
        }

        /// <summary>
        /// Gets or sets the number of tokens.
        /// </summary>
        public int Tokens
        {
            get
            {
                return _tokens;
            }

            set
            {
                _tokens = value;
                if (_defaultTokens == 0)
                {
                    _defaultTokens = _tokens;
                }
            }
        }

        #endregion

        /// <summary>
        /// The SubmitJob() methods to submit job directly.
        /// </summary>
        /// <returns>Bollean value.</returns>
        public bool SubmitJob()
        {
            return BroadCast();
        }

        /// <summary>
        /// The Initialize() methods to initalize the necessary configuration.
        /// </summary>
        /// <returns>Bollean value.</returns>
        protected override bool Initialize()
        {
            return true;
        }

        /// <summary>
        /// The BroadCast() methods to initalize the necessary configuration.
        /// </summary>
        /// <returns>Bollean value.</returns>
        protected override bool BroadCast()
        {
            _instance.JobEngine.SubmitAndWaitJob(_jobScript, _jobName, _jobDisplayName, _backupDir, _nebularArg, _isLocalRun, 0, _tokens);
            _tokens = _defaultTokens;
            return true;
        }

        /// <summary>
        /// The Reduce() methods to initalize the necessary configuration.
        /// </summary>
        /// <returns>Bollean value.</returns>
        protected override bool Reduce()
        {
            return true;
        }

        /// <summary>
        /// The ValidateResult() methods to initalize the necessary configuration.
        /// </summary>
        /// <returns>Bollean value.</returns>
        protected override bool ValidateResult()
        {
            return true;
        }
    }
}
