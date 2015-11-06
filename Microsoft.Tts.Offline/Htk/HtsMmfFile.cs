//----------------------------------------------------------------------------
// <copyright file="HtsMmfFile.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements HTS MMF file
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Htk
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// HTS MMF file.
    /// </summary>
    public class HtsMmfFile
    {
        #region Field

        private HmmModelType _modelType;
        private Dictionary<string, HmmStream> _namedStreams = new Dictionary<string, HmmStream>();
        private Dictionary<int, HmmStream> _positionedStreams = new Dictionary<int, HmmStream>();
        private IEnumerable<HmmStream> _streams;
        private ModelDistributionType _distribution;

        private Dictionary<HmmModelType, string> _algorithmIds = new Dictionary<HmmModelType, string>()
        {
            { HmmModelType.Lsp, "Linear Spectrum Pair" },
            { HmmModelType.FundamentalFrequency, "Log Fundamental Frequency" },
            { HmmModelType.StateDuration, "Duration" },
            { HmmModelType.PhoneDuration, "Phone Duration" },
            { HmmModelType.Mbe, "Multi-Band Excitation" },
            { HmmModelType.Power, "Power" },
            { HmmModelType.GuidanceLsp, "GuidanceLsp" },
        };

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the HtsMmfFile class.
        /// </summary>
        /// <param name="modelType">Model type of this file.</param>
        public HtsMmfFile(HmmModelType modelType)
        {
            _modelType = modelType;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets File path of this MMF file.
        /// </summary>
        public string FilePath
        {
            get;
            set;
        }

        /// <summary>
        /// Gets Model type.
        /// </summary>
        public HmmModelType ModelType
        {
            get { return _modelType; }
        }

        /// <summary>
        /// Gets or sets Multi-space distribution if
        /// Two gaussians in the stream, and mean and variance of the second one are all zero.
        /// </summary>
        public ModelDistributionType Distribution
        {
            get { return _distribution; }
            set { _distribution = value; }
        }

        /// <summary>
        /// Gets or sets Stream count.
        /// </summary>
        [CLSCompliant(false)]
        public uint StreamCount
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets Stream indexes.
        /// </summary>
        public int[] StreamIndexes
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets Array of stream width for each stream.
        /// </summary>
        public int[] StreamWidths
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets Key, state macro name, i.e. "dh_logF0_s2_93-2"
        /// Value, HMM Stream, with Gaussians.
        /// </summary>
        public IEnumerable<HmmStream> Streams
        {
            get { return _streams; }
            set { _streams = value; }
        }

        /// <summary>
        /// Gets Name-indexed HMM streams.
        /// </summary>
        public Dictionary<string, HmmStream> NamedStreams
        {
            get { return _namedStreams; }
        }

        /// <summary>
        /// Gets Mixture count of the Gaussian distribution, excluding zero-dimension mixture.
        /// </summary>
        public int GaussianMixtureCount
        {
            get { return Streams.First().Gaussians.Where(g => g.Length > 0).Count(); }
        }

        /// <summary>
        /// Gets Dictionary, offset to stream.
        /// </summary>
        public Dictionary<int, HmmStream> PositionedStreams
        {
            get { return _positionedStreams; }
        }

        /// <summary>
        /// Gets or sets The ID string of algorithm.
        /// </summary>
        public string AlgorithmId
        {
            get { return _algorithmIds[ModelType]; }
            set { Debug.Assert(ModelType == _algorithmIds.Keys.Where(k => _algorithmIds[k] == value).First()); }
        }

        #endregion

        #region Public operations

        /// <summary>
        /// Load gaussian models in MMF file.
        /// </summary>
        /// <param name="mmfPath">The path of MMF file.</param>
        public void Load(string mmfPath)
        {
            Helper.ThrowIfNull(mmfPath);
            FilePath = mmfPath;
            _streams = HmmReader.Streams(FilePath).Where(s => s.ModelType == ModelType);
            StreamWidths = HmmReader.ReadtStreamWidths(FilePath, StreamIndexes);

            Distribution = DetectDistribution(_streams.First(s => s.ModelType == ModelType).Gaussians);
            Distribution = Distribution == ModelDistributionType.NotDefined ? ModelDistributionType.Continuous : Distribution;
        }

        /// <summary>
        /// Detects gaussian distribution type according gaussians.
        /// </summary>
        /// <param name="gaussians">Gaussians.</param>
        /// <returns>Gaussian distribution type.</returns>
        private static ModelDistributionType DetectDistribution(Gaussian[] gaussians)
        {
            Helper.ThrowIfNull(gaussians);
            ModelDistributionType type = ModelDistributionType.NotDefined;
            if (gaussians.Length == 2)
            {
                if (gaussians[1].Mean.Count(m => m != 0) == 0 &&
                    gaussians[1].Variance.Count(m => m != 0) == 0)
                {
                    type = ModelDistributionType.Msd;
                }
            }

            return type;
        }

        #endregion
    }
}