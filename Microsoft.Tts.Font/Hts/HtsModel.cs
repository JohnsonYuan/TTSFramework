//----------------------------------------------------------------------------
// <copyright file="HtsModel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements HTS Model object model
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Font.Hts
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Htk;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Enum for extension area types.
    /// </summary>
    public enum ExtensionAreaType
    {
        /// <summary>
        /// Customized generation for F0.
        /// </summary>
        F0Customization = 1,
    }

    /// <summary>
    /// HTS F0 customized generation setting.
    /// </summary>
    public class HtsF0CustomizedGeneration
    {
        #region Data properties

        /// <summary>
        /// Gets or sets the window setting in customized generation.
        /// </summary>
        public List<float> Window
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the enhancing rate setting in customized generation.
        /// </summary>
        public float EnhanceRate
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the mean value in customized generation.
        /// </summary>
        public float Mean
        {
            get;
            set;
        }

        #endregion
    }

    /// <summary>
    /// HTS Model.
    /// </summary>
    public class HtsModel
    {
        #region Private fields
        private HtsModelHeader _modelHeader = new HtsModelHeader();
        private HtsFont _font;
        private TtsPhoneSet _phoneSet;
        private TtsPosSet _posSet;
        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="HtsModel" /> class.
        /// </summary>
        /// <param name="font">HTS font, which owns current HTS model.</param>
        /// <param name="phoneSet">The set of phone.</param>
        /// <param name="posSet">The set of part of speech.</param>
        public HtsModel(HtsFont font, TtsPhoneSet phoneSet, TtsPosSet posSet)
        {
            Helper.ThrowIfNull(font);
            Helper.ThrowIfNull(phoneSet);
            Helper.ThrowIfNull(posSet);
            _font = font;
            _phoneSet = phoneSet;
            _posSet = posSet;
        }

        #endregion

        #region Data properties

        /// <summary>
        /// Gets or sets the model header of this model.
        /// </summary>
        public HtsModelHeader Header
        {
            get { return _modelHeader; }
            set { _modelHeader = value; }
        }

        /// <summary>
        /// Gets or sets the HTS MMF file.
        /// </summary>
        public HtsMmfFile MmfFile
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the HTS transform file.
        /// </summary>
        public HtsTransformFile XformFile
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the transform mapping file.
        /// </summary>
        public AdaptationMappingFile MappingFile
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the decision forest.
        /// </summary>
        public DecisionForest Forest
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets dynamic windows.
        /// </summary>
        public DynamicWindowSet WindowSet
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the HTS font, to which this model belongs to.
        /// </summary>
        public HtsFont Font
        {
            get { return _font; }
            set { _font = value; }
        }

        #endregion

        #region Supporting properties

        /// <summary>
        /// Gets the phone set.
        /// </summary>
        public TtsPhoneSet PhoneSet
        {
            get { return _phoneSet; }
        }

        /// <summary>
        /// Gets the part of speech set.
        /// </summary>
        public TtsPosSet PosSet
        {
            get { return _posSet; }
        }

        /// <summary>
        /// Gets or sets merged stream indexes of current model.
        /// </summary>
        public int[] MergedStreamIndexes
        {
            get;
            set;
        }

        #endregion

        #region Operations
        /// <summary>
        /// Loads Hts model.
        /// </summary>
        /// <param name="treePath">The location of decision tree file.</param>
        /// <param name="mmfPath">The location of MMF file.</param>
        /// <param name="windowSet">Dynamic windows set.</param>
        /// <param name="customFeatures">Customized feature set.</param>
        public void Load(string treePath, string mmfPath, DynamicWindowSet windowSet, HashSet<string> customFeatures)
        {
            Helper.ThrowIfNull(treePath);
            Helper.ThrowIfNull(mmfPath);
            Helper.ThrowIfNull(windowSet);
            Helper.ThrowIfNull(customFeatures);

            WindowSet = windowSet;
            Forest = new DecisionForest(Path.GetFileName(treePath)) { PhoneSet = PhoneSet, PosSet = PosSet };
            Forest.Load(treePath);
            Forest.BuildPhones(PhoneSet);
            Header.ModelType = Forest.ModelType();
            foreach (Question question in Forest.QuestionList)
            {
                question.Language = PhoneSet.Language;
                question.ValueSetToCodeValueSet(PosSet, PhoneSet, customFeatures);
            }

            // Builds question list according to the question consequence in decision tree
            Forest.ReSortQuestions();

            MmfFile = new HtsMmfFile(Forest.ModelType())
            {
                StreamCount = (uint)Forest.StreamCount,
                StreamWidths = new int[Forest.StreamCount],
                StreamIndexes = Forest.StreamIndexes,
            };

            MmfFile.Load(mmfPath);

            Header.IsGaussian = true;
            Header.GaussianConfig = new GaussianConfig();
            Header.GaussianConfig.StaticVectorSize = CalculateStaticVectorSize();
        }

        /// <summary>
        /// Loads Hts transform model.
        /// </summary>
        /// <param name="treePath">The location of decision tree file.</param>
        /// <param name="mmfPath">The location of MMF file.</param>
        /// <param name="xformPath">The location of transform model file.</param>
        /// <param name="mappingPath">The location of transform mapping file.</param>
        /// <param name="windowSet">Dynamic windows set.</param>
        /// <param name="customFeatures">Customized feature set.</param>
        /// <param name="cmpMmf">The MasterMacroFile.</param>
        /// <param name="varFloorsFile">The varFloors file.</param>
        /// <param name="mgelrRefinedAlignmentMlf">The mgelr Refined alignment Mlf file.</param>
        /// <param name="streamRange">The stream range.</param>
        /// <param name="stateCount">The state count.</param>
        public void Load(string treePath, string mmfPath, string xformPath, string mappingPath, DynamicWindowSet windowSet, HashSet<string> customFeatures, MasterMacroFile cmpMmf, string varFloorsFile, string mgelrRefinedAlignmentMlf, string streamRange, int stateCount)
        {
            Helper.ThrowIfNull(treePath);
            Helper.ThrowIfNull(xformPath);
            Helper.ThrowIfNull(mappingPath);
            Helper.ThrowIfNull(windowSet);
            Helper.ThrowIfNull(customFeatures);
            Helper.ThrowIfNull(cmpMmf);
            Helper.ThrowIfFileNotExist(varFloorsFile);
            Helper.ThrowIfFileNotExist(mgelrRefinedAlignmentMlf);

            WindowSet = windowSet;
            Forest = new DecisionForest(Path.GetFileName(treePath)) { PhoneSet = PhoneSet, PosSet = PosSet };
            Forest.Load(treePath);
            Forest.BuildPhones(PhoneSet);
            Header.ModelType = Forest.ModelType();
            foreach (Question question in Forest.QuestionList)
            {
                question.Language = PhoneSet.Language;
                question.ValueSetToCodeValueSet(PosSet, PhoneSet, customFeatures);
            }

            // Builds question list according to the question consequence in decision tree
            Forest.ReSortQuestions();

            MmfFile = new HtsMmfFile(Forest.ModelType())
            {
                StreamCount = (uint)Forest.StreamCount,
                StreamWidths = new int[Forest.StreamCount],
                StreamIndexes = Forest.StreamIndexes,
            };

            MmfFile.Load(mmfPath);

            XformFile = new HtsTransformFile();
            XformFile.Load(xformPath);

            MappingFile = new AdaptationMappingFile();
            MappingFile.Load(mappingPath);

            Header.IsGaussian = false;
            Header.LinXformConfig = new LinXformConfig();
            Header.LinXformConfig.StaticVectorSize = CalculateStaticVectorSize();

            if (streamRange != null)
            {
                LoadStablenessData(Forest, cmpMmf, varFloorsFile, mgelrRefinedAlignmentMlf, streamRange, stateCount);
            }
        }

        /// <summary>
        /// Loads stableness data.
        /// </summary>
        /// <param name="forest">DecisionForest.</param>
        /// <param name="cmpMmf">The MasterMacroFile.</param>
        /// <param name="varFloorsFile">The varFloors file.</param>
        /// <param name="mgelrRefinedAlignmentMlf">The mgelr Refined alignment Mlf file.</param>
        /// <param name="streamRange">The stream range.</param>
        /// <param name="stateCount">The state count.</param>
        public void LoadStablenessData(DecisionForest forest, MasterMacroFile cmpMmf, string varFloorsFile, string mgelrRefinedAlignmentMlf, string streamRange, int stateCount)
        {
            int streamStartIndex = -1;
            int streamEndIndex = -1;

            string[] streamIndexs = streamRange.Split(new char[2] { '-', ',' });

            streamStartIndex = int.Parse(streamIndexs[0]);
            streamEndIndex = int.Parse(streamIndexs[streamIndexs.Length - 1]);

            if (streamStartIndex == -1)
            {
                return;
            }

            int allStreamMeanCeilingCount = 0;

            List<List<double>> streamMeanCeilingList = new List<List<double>>();
            List<List<double>> streamMeanFloorList = new List<List<double>>();

            if (forest.ModelType() != HmmModelType.StateDuration && forest.ModelType() != HmmModelType.PhoneDuration)
            {
                for (int streamIndex = streamStartIndex; streamIndex <= streamEndIndex; streamIndex++)
                {
                    int streamLength = cmpMmf.Models.Values.ToArray()[0].States[0].Streams[streamIndex - 1].Gaussians[0].Mean.Length;

                    allStreamMeanCeilingCount += streamLength;
                    streamMeanCeilingList.Add(new List<double>());
                    streamMeanFloorList.Add(new List<double>());

                    for (int j = 0; j < streamLength; j++)
                    {
                        streamMeanCeilingList[streamIndex - streamStartIndex].Add(double.MinValue);
                        streamMeanFloorList[streamIndex - streamStartIndex].Add(double.MaxValue);
                    }
                }

                foreach (HmmModel model in cmpMmf.Models.Values)
                {
                    for (int stateIndex = 0; stateIndex < stateCount; stateIndex++)
                    {
                        for (int streamIndex = streamStartIndex; streamIndex <= streamEndIndex; streamIndex++)
                        {
                            foreach (Gaussian gaussian in model.States[stateIndex].Streams[streamIndex - 1].Gaussians)
                            {
                                for (int k = 0; k < gaussian.Mean.Length; k++)
                                {
                                    if (streamMeanCeilingList[streamIndex - streamStartIndex][k] < gaussian.Mean[k])
                                    {
                                        streamMeanCeilingList[streamIndex - streamStartIndex][k] = gaussian.Mean[k];
                                    }

                                    if (streamMeanFloorList[streamIndex - streamStartIndex][k] > gaussian.Mean[k])
                                    {
                                        streamMeanFloorList[streamIndex - streamStartIndex][k] = gaussian.Mean[k];
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                TrainingSentenceSet sentenceSet = new TrainingSentenceSet();
                sentenceSet.Load(mgelrRefinedAlignmentMlf);

                double durationInterval = 50000.0;
                for (int streamIndex = streamStartIndex; streamIndex <= streamEndIndex; streamIndex++)
                {
                    int streamLength = 0;
                    if (forest.ModelType() == HmmModelType.StateDuration)
                    {
                        streamLength = sentenceSet.Sentences.Values.ToArray()[0].PhoneSegments[0].StateAlignments.Length;
                    }
                    else if (forest.ModelType() == HmmModelType.PhoneDuration)
                    {
                        streamLength = 1;
                    }

                    allStreamMeanCeilingCount += streamLength;
                    streamMeanCeilingList.Add(new List<double>());
                    streamMeanFloorList.Add(new List<double>());

                    for (int j = 0; j < streamLength; j++)
                    {
                        streamMeanCeilingList[streamIndex - streamStartIndex].Add(double.MinValue);
                        streamMeanFloorList[streamIndex - streamStartIndex].Add(double.MaxValue);
                    }

                    foreach (Sentence sentence in sentenceSet.Sentences.Values)
                    {
                        foreach (PhoneSegment phoneSegment in sentence.PhoneSegments)
                        {
                            if (phoneSegment.Name.ToLower().Equals("sil"))
                            {
                                continue;
                            }

                            for (int i = 0; i < streamLength; i++)
                            {
                                if (forest.ModelType() == HmmModelType.PhoneDuration)
                                {
                                    double phoneDuration = (phoneSegment.EndTime - phoneSegment.StartTime) / durationInterval;
                                    if (streamMeanCeilingList[streamIndex - streamStartIndex][i] < phoneDuration)
                                    {
                                        streamMeanCeilingList[streamIndex - streamStartIndex][i] = phoneDuration;
                                    }

                                    if (streamMeanFloorList[streamIndex - streamStartIndex][i] > phoneDuration)
                                    {
                                        streamMeanFloorList[streamIndex - streamStartIndex][i] = phoneDuration;
                                    }
                                }
                                else if (forest.ModelType() == HmmModelType.StateDuration)
                                {
                                    double stateDuration = (phoneSegment.StateAlignments[i].EndTime - phoneSegment.StateAlignments[i].StartTime) / durationInterval;

                                    if (streamMeanCeilingList[streamIndex - streamStartIndex][i] < stateDuration)
                                    {
                                        streamMeanCeilingList[streamIndex - streamStartIndex][i] = stateDuration;
                                    }

                                    if (streamMeanFloorList[streamIndex - streamStartIndex][i] > stateDuration)
                                    {
                                        streamMeanFloorList[streamIndex - streamStartIndex][i] = stateDuration;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (streamMeanCeilingList.Count > 0)
            {
                Header.LinXformConfig.MeanCeilings = new float[allStreamMeanCeilingCount];
                Header.LinXformConfig.MeanFloors = new float[allStreamMeanCeilingCount];

                int dimentionIndex = 0;
                for (int i = 0; i < streamMeanCeilingList.Count; i++)
                {
                    for (int j = 0; j < streamMeanCeilingList[i].Count; j++, dimentionIndex++)
                    {
                        Header.LinXformConfig.MeanFloors[dimentionIndex] = (float)streamMeanFloorList[i][j];
                        Header.LinXformConfig.MeanCeilings[dimentionIndex] = (float)streamMeanCeilingList[i][j];
                    }
                }

                if (forest.ModelType() != HmmModelType.StateDuration && forest.ModelType() != HmmModelType.PhoneDuration)
                {
                    List<List<float>> varFloorsList = new List<List<float>>();
                    using (StreamReader varFloorsReader = new StreamReader(varFloorsFile))
                    {
                        string line = null;
                        while ((line = varFloorsReader.ReadLine()) != null)
                        {
                            if (line.StartsWith("<Variance>"))
                            {
                                List<float> varList = new List<float>();
                                varFloorsList.Add(varList);

                                line = varFloorsReader.ReadLine();
                                while (line != null && !line.StartsWith("~v"))
                                {
                                    foreach (string var in line.Split(' '))
                                    {
                                        if (!string.IsNullOrWhiteSpace(var))
                                        {
                                            varList.Add(float.Parse(var));
                                        }
                                    }

                                    line = varFloorsReader.ReadLine();
                                }
                            }
                        }
                    }

                    Header.LinXformConfig.VarianceFloors = new float[allStreamMeanCeilingCount];
                    dimentionIndex = 0;
                    for (int streamIndex = streamStartIndex; streamIndex < streamEndIndex; streamIndex++)
                    {
                        for (int i = 0; i < varFloorsList[streamIndex - 1].Count; i++, dimentionIndex++)
                        {
                            Header.LinXformConfig.VarianceFloors[dimentionIndex] = varFloorsList[streamIndex - 1][i];
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Calculates static feature vector size form stream widths and dynamic windows.
        /// </summary>
        /// <returns>Static feature vector size.</returns>
        public uint CalculateStaticVectorSize()
        {
            return (uint)(MmfFile.StreamWidths.Sum() / WindowSet.DynamicOrderCount);
        }

        /// <summary>
        /// Merges acoustic parameters into one stream, take first stream as default stream.
        /// </summary>
        public void MergeStreams()
        {
            if (MmfFile.StreamIndexes.Length > 1)
            {
                MmfFile.Streams = MergeStreams(MmfFile.Streams, MmfFile.StreamIndexes);
                MergedStreamIndexes = MmfFile.StreamIndexes;
                MmfFile.StreamWidths = new int[] { MmfFile.StreamWidths.Sum() };
                int[] indexesToRemove = Forest.StreamIndexes.Skip((int)DynamicOrder.Static).ToArray();
                Forest.PruneStream(indexesToRemove);
            }
        }

        /// <summary>
        /// Prunes acceleration dynamic features from the model.
        /// </summary>
        public void PruneAcceleration()
        {
            if (MmfFile.StreamWidths.Length == 1)
            {
                MmfFile.StreamWidths[0] = MmfFile.StreamWidths[0] * (int)DynamicOrder.Delta / (int)DynamicOrder.Acceleration;
                MmfFile.Streams = HmmStream.Prune(MmfFile.Streams, MmfFile.StreamWidths[0]);
            }
            else if (MmfFile.StreamWidths.Length == (int)DynamicOrder.Acceleration)
            {
                Debug.Assert(MmfFile.StreamWidths.Length == Forest.StreamIndexes.Length,
                    "The count of Stream Widths should equal with the count of Stream Indexes.");

                // Assume the last stream here is the acceleration dynamic stream
                MmfFile.StreamWidths = MmfFile.StreamWidths.Take((int)DynamicOrder.Delta).ToArray();
                int[] indexesToRemove = Forest.StreamIndexes.Skip((int)DynamicOrder.Delta).ToArray();
                Forest.PruneStream(indexesToRemove);
            }
            else
            {
                throw new NotSupportedException(Helper.NeutralFormat("Only single stream or 3-stream pruning are supported."));
            }

            Header.GaussianConfig.StaticVectorSize = CalculateStaticVectorSize();
        }

        /// <summary>
        /// Builds names for HMM streams according to position information between streams and decision tree nodes.
        /// </summary>
        public void BuildStreamName()
        {
            SortedDictionary<int, DecisionTreeNode> positionedNodes = new SortedDictionary<int, DecisionTreeNode>();
            foreach (DecisionTree tree in Forest.TreeList)
            {
                int nodeIndex = 1;
                foreach (DecisionTreeNode node in tree.NodeList.Where(n => n.NodeType == DecisionTreeNodeType.Leaf))
                {
                    node.Name = Helper.NeutralFormat("{0}_{1}_s{2}_{3}",
                        tree.Phone, HmmNameEncoding.GetAcousticFeatureName(_modelHeader.ModelType),
                        tree.StateIndex(), nodeIndex);
                    foreach (int offset in node.RefDataOffsets)
                    {
                        positionedNodes.Add(offset, node);
                    }

                    nodeIndex++;
                }
            }

            List<HmmStream> orphanStreams = new List<HmmStream>();
            foreach (HmmStream stream in MmfFile.PositionedStreams.Values)
            {
                if (positionedNodes.ContainsKey(stream.Position))
                {
                    DecisionTreeNode node = positionedNodes[stream.Position];
                    if (node.RefDataOffsets.Length == 1)
                    {
                        stream.Name = node.Name;
                    }
                    else
                    {
                        int index = new List<int>(node.RefDataOffsets).IndexOf(stream.Position);
                        stream.Name = Helper.NeutralFormat("{0}-{1}",
                            node.Name, _modelHeader.StreamIndexes[index]);
                    }
                }
                else
                {
                    orphanStreams.Add(stream);
                    Trace.WriteLine(Helper.NeutralFormat(
                        "Stream at {0} is not indexed by any node of decision tree.", stream.Position));
                }
            }

            foreach (HmmStream stream in orphanStreams)
            {
                MmfFile.PositionedStreams.Remove(stream.Position);
                if (!string.IsNullOrEmpty(stream.Name) && MmfFile.NamedStreams.ContainsKey(stream.Name))
                {
                    MmfFile.NamedStreams.Remove(stream.Name);
                }
            }

            MmfFile.Streams = MmfFile.Streams.Except(orphanStreams);
        }

        #endregion

        #region Private operation

        /// <summary>
        /// Merges streams in the list according to HMM stream macro name pattern, as like
        ///     Streams
        ///         logF0_s4_4-2
        ///         logF0_s4_4-3
        ///         logF0_s4_4-4
        ///     Will be merged into one stream
        ///         logF0_s4_4
        ///     The maximum weight of these streams will be kept, and the Gaussian distributions
        ///     will be concatenated into one Gaussian distribution with more dimensions.
        /// </summary>
        /// <param name="sources">Source HMM streams to merge.</param>
        /// <param name="streamIndexes">Stream indexes.</param>
        /// <returns>Merged Streams.</returns>
        private static IEnumerable<HmmStream> MergeStreams(IEnumerable<HmmStream> sources, int[] streamIndexes)
        {
            Helper.ThrowIfNull(sources);
            Helper.ThrowIfNull(streamIndexes);

            List<HmmStream> session = new List<HmmStream>();
            Dictionary<string, HmmStream> cached = ToNamedStreams(sources);

            IEnumerable<string> mergedNames = cached.Keys.Select(s => s.Substring(0, s.Length - 2)).Distinct();
            foreach (string mergedName in mergedNames)
            {
                foreach (int index in streamIndexes)
                {
                    session.Add(cached[Helper.NeutralFormat("{0}-{1}", mergedName, index)]);
                }

                HmmStream result = MergeStreams(session);
                session.Clear();
                yield return result;
            }
        }

        /// <summary>
        /// Converts HMM streams into name-keyed streams.
        /// </summary>
        /// <param name="targets">Target HMM streams to map to name.</param>
        /// <returns>Name-indexes HMM streams.</returns>
        private static Dictionary<string, HmmStream> ToNamedStreams(IEnumerable<HmmStream> targets)
        {
            Helper.ThrowIfNull(targets);

            Dictionary<string, HmmStream> results = new Dictionary<string, HmmStream>();
            foreach (HmmStream stream in targets)
            {
                results.Add(stream.Name, stream);
            }

            return results;
        }

        /// <summary>
        /// Merges one session of HMM streams, all together.
        /// </summary>
        /// <param name="session">Session with HMM streams.</param>
        /// <returns>Merged HMM stream.</returns>
        private static HmmStream MergeStreams(IEnumerable<HmmStream> session)
        {
            Helper.ThrowIfNull(session);

            HmmStream result = new HmmStream();
            result.Name = session.First().Name;
            if (Regex.Match(result.Name, @"-\d$").Success)
            {
                result.Name = result.Name.Substring(0, result.Name.Length - 2); // Remove stream tag
            }

            Debug.Assert(session.Count(s => !s.Name.StartsWith(result.Name, StringComparison.Ordinal)) == 0,
                "All HMM streams in this session should share the same stream macro name, except stream index if has.");

            result.Gaussians = new Gaussian[session.First().Gaussians.Length];
            for (int i = 0; i < result.Gaussians.Length; i++)
            {
                result.Gaussians[i] = MergeGaussian(session.Select(s => s.Gaussians[i]));
            }

            return result;
        }

        /// <summary>
        /// Merges Gaussian distributions.
        /// </summary>
        /// <param name="gaussians">Collection of Gaussian distributions.</param>
        /// <returns>Merged Gaussian.</returns>
        private static Gaussian MergeGaussian(IEnumerable<Gaussian> gaussians)
        {
            Helper.ThrowIfNull(gaussians);
            Gaussian result = new Gaussian();
            result.Weight = gaussians.Max(g => g.Weight);
            result.Variance = gaussians.First().Variance;
            result.Length = gaussians.Sum(g => g.Length);
            result.Mean = new double[result.Length];
            result.Variance = new double[result.Length];
            int offset = 0;
            foreach (Gaussian gaussian in gaussians)
            {
                Debug.Assert(gaussian.Mean.Length == gaussian.Variance.Length,
                    "Lengths of Mean and Variance should equal with each other.");
                gaussian.Mean.CopyTo(result.Mean, offset);
                gaussian.Variance.CopyTo(result.Variance, offset);
                offset += gaussian.Mean.Length;
            }

            return result;
        }

        #endregion
    }

    /// <summary>
    /// Gaussian configuration for one model.
    /// </summary>
    public class GaussianConfig : IBinarySerializer<GaussianConfig>
    {
        #region Construction
        /// <summary>
        /// Initializes a new instance of the <see cref="GaussianConfig" /> class.
        /// </summary>
        public GaussianConfig()
        {
            HasWeight = true;
            HasMean = true;
            HasVariance = true;
            CovarianceType = CovarianceType.Variance;
            IsFixedPoint = false;
            MeanBits = sizeof(float) * 8;
            VarianceBits = sizeof(float) * 8;
            HasCodebook = false;
        }
        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets The number of mixture of the Gaussian distribution.
        /// </summary>
        public uint MixtureCount
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets The number of dimensions of static acoustic feature of current model.
        /// </summary>
        public uint StaticVectorSize
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether Flag on whether has Weight information in the Gaussian distribution.
        /// </summary>
        public bool HasWeight
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether has Mean information in the Gaussian distribution.
        /// </summary>
        public bool HasMean
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether has Variance information in the Gaussian distribution.
        /// </summary>
        public bool HasVariance
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether has Codebook information for de-quantization.
        /// </summary>
        public bool HasCodebook
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets Covariance type of Gaussian distribution.
        /// </summary>
        public CovarianceType CovarianceType
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the Gaussian distribution as fixed point.
        /// </summary>
        public bool IsFixedPoint
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets Number of bits of each value of mean.
        /// </summary>
        public uint MeanBits
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the number of bits of each value of variance.
        /// </summary>
        public uint VarianceBits
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to keep fixed point data in Gaussian model.
        /// </summary>
        public bool KeepFixedPoint { get; set; }

        #endregion

        #region IBinarySerializer<GaussianConfig> Members

        /// <summary>
        /// Save instance into data writer.
        /// </summary>
        /// <param name="writer">Target data writer to serialize.</param>
        /// <returns>Number of bytes written out.</returns>
        public uint Save(DataWriter writer)
        {
            Helper.ThrowIfNull(writer);
            Validate();
            uint size = 0;
            size += writer.Write((uint)MixtureCount);
            size += writer.Write((uint)StaticVectorSize);
            size += writer.Write((uint)(HasWeight ? 1 : 0));
            size += writer.Write((uint)(HasMean ? 1 : 0));
            size += writer.Write((uint)(HasVariance ? 1 : 0));
            size += writer.Write((uint)CovarianceType);
            size += writer.Write((uint)(IsFixedPoint ? 1 : 0));
            size += writer.Write((uint)MeanBits);
            size += writer.Write((uint)VarianceBits);
            size += writer.Write((uint)(HasCodebook ? 1 : 0));
            return size;
        }

        /// <summary>
        /// Load Gaussian configuration.
        /// </summary>
        /// <param name="reader">Binary reader.</param>
        /// <returns>Gaussian configuration.</returns>
        public GaussianConfig Load(BinaryReader reader)
        {
            Helper.ThrowIfNull(reader);
            MixtureCount = reader.ReadUInt32();
            StaticVectorSize = reader.ReadUInt32();
            HasWeight = reader.ReadUInt32() != 0;
            HasMean = reader.ReadUInt32() != 0;
            HasVariance = reader.ReadUInt32() != 0;
            CovarianceType = (CovarianceType)reader.ReadUInt32();
            IsFixedPoint = reader.ReadUInt32() != 0;
            MeanBits = reader.ReadUInt32();
            VarianceBits = reader.ReadUInt32();
            HasCodebook = reader.ReadUInt32() != 0;
            Validate();
            return this;
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validate instance status.
        /// </summary>
        private void Validate()
        {
            if ((!HasMean && !HasVariance) ||
                Enum.GetNames(typeof(CovarianceType)).Count(v => v == CovarianceType.ToString()) != 1)
            {
                throw new InvalidDataException();
            }

            if (MeanBits < 4 || MeanBits > 32 ||
                VarianceBits < 4 || VarianceBits > 32 ||
                HasCodebook != false)
            {
                throw new InvalidDataException();
            }
        }
        #endregion
    }

    /// <summary>
    /// LinXform configuration for one model.
    /// </summary>
    public class LinXformConfig : IBinarySerializer<LinXformConfig>
    {
        #region Construction
        /// <summary>
        /// Initializes a new instance of the <see cref="LinXformConfig" /> class.
        /// </summary>
        public LinXformConfig()
        {
            HasMeanXform = true;
            HasVarXform = true;
            HasMeanBias = true;
            HasVarBias = false;
            IsFixedPoint = false;
            BiasBits = sizeof(float) * 8;
            MatrixBits = sizeof(float) * 8;
            HasCodebook = false;
        }
        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets The number of dimensions of static acoustic feature of current model.
        /// </summary>
        public uint StaticVectorSize
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets The band width of mean transformation matrix.
        /// </summary>
        public uint MeanBandWidth
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets The number of blocks of mean transform matrix of current model.
        /// </summary>
        public uint MeanBlockNum
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets mean ceiling size.
        /// </summary>
        public uint MeanCeilingSize
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets mean floor size.
        /// </summary>
        public uint MeanFloorSize
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets The size of each mean transform matrix block of current model.
        /// </summary>
        public uint[] MeanBlockSizes
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets mean ceilng.
        /// </summary>
        public float[] MeanCeilings
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets mean floor.
        /// </summary>
        public float[] MeanFloors
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets The number of blocks of variance transform matrix of current model.
        /// </summary>
        public uint VarBlockNum
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets The size of each variance transform matrix block of current model.
        /// </summary>
        public uint[] VarBlockSizes
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets variance floor size.
        /// </summary>
        public uint VarianceFloorSize
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets variance floors.
        /// </summary>
        public float[] VarianceFloors
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether has linear transform for Gaussian mean.
        /// </summary>
        public bool HasMeanXform
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether has linear transform for Gaussian variance.
        /// </summary>
        public bool HasVarXform
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether has bias information in the mean linear transform.
        /// </summary>
        public bool HasMeanBias
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether has matrix information in the variance linear transform.
        /// </summary>
        public bool HasVarBias
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether has Codebook information for de-quantization.
        /// </summary>
        public bool HasCodebook
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the linear transform is fixed point.
        /// </summary>
        public bool IsFixedPoint
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets Number of bits of each value of bias.
        /// </summary>
        public uint BiasBits
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the number of bits of each value of matrix.
        /// </summary>
        public uint MatrixBits
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to keep fixed point data in linear transform.
        /// </summary>
        public bool KeepFixedPoint { get; set; }

        #endregion

        #region IBinarySerializer<LinXformConfig> Members

        /// <summary>
        /// Save instance into data writer.
        /// </summary>
        /// <param name="writer">Target data writer to serialize.</param>
        /// <returns>Number of bytes written out.</returns>
        public uint Save(DataWriter writer)
        {
            Helper.ThrowIfNull(writer);
            Validate();
            uint size = 0;
            size += writer.Write((uint)StaticVectorSize);
            size += writer.Write((uint)MeanBandWidth);
            size += writer.Write((uint)MeanBlockNum);
            for (uint i = 0; i < MeanBlockNum; i++)
            {
                size += writer.Write((uint)MeanBlockSizes[i]);
            }

            if (MeanCeilings != null)
            {
                size += writer.Write((uint)MeanCeilings.Length);
                for (uint i = 0; i < MeanCeilings.Length; i++)
                {
                    if (float.IsInfinity(MeanCeilings[i]))
                    {
                        throw new Exception("Infinity float in MeanCeilings: i = " + i);
                    }

                    writer.Write(MeanCeilings[i]);
                    size += sizeof(float);
                }

                size += writer.Write((uint)MeanFloors.Length);
                for (uint i = 0; i < MeanFloors.Length; i++)
                {
                    if (float.IsInfinity(MeanFloors[i]))
                    {
                        throw new Exception("Infinity float in MeanFloors: i = " + i);
                    }

                    writer.Write(MeanFloors[i]);
                    size += sizeof(float);
                }
            }
            else
            {
                size += writer.Write((uint)0);
                size += writer.Write((uint)0);
            }

            size += writer.Write((uint)VarBlockNum);
            for (uint i = 0; i < VarBlockNum; i++)
            {
                size += writer.Write((uint)VarBlockSizes[i]);
            }

            if (VarianceFloors != null)
            {
                size += writer.Write((uint)VarianceFloors.Length);
                for (uint i = 0; i < VarianceFloors.Length; i++)
                {
                    if (float.IsInfinity(VarianceFloors[i]))
                    {
                        throw new Exception("Infinity float in VarianceFloors: i = " + i);
                    }

                    writer.Write(VarianceFloors[i]);
                    size += sizeof(float);
                }
            }
            else
            {
                size += writer.Write((uint)0);
            }

            size += writer.Write((uint)(HasMeanXform ? 1 : 0));
            size += writer.Write((uint)(HasVarXform ? 1 : 0));
            size += writer.Write((uint)(HasMeanBias ? 1 : 0));
            size += writer.Write((uint)(HasVarBias ? 1 : 0));
            size += writer.Write((uint)(IsFixedPoint ? 1 : 0));
            size += writer.Write((uint)BiasBits);
            size += writer.Write((uint)MatrixBits);
            size += writer.Write((uint)(HasCodebook ? 1 : 0));
            return size;
        }

        /// <summary>
        /// Load Gaussian configuration.
        /// </summary>
        /// <param name="reader">Binary reader.</param>
        /// <returns>Gaussian configuration.</returns>
        public LinXformConfig Load(BinaryReader reader)
        {
            Helper.ThrowIfNull(reader);
            StaticVectorSize = reader.ReadUInt32();
            MeanBandWidth = reader.ReadUInt32();
            MeanBlockNum = reader.ReadUInt32();
            MeanBlockSizes = new uint[MeanBlockNum];
            for (uint i = 0; i < MeanBlockNum; i++)
            {
                MeanBlockSizes[i] = reader.ReadUInt32();
            }

            MeanCeilingSize = reader.ReadUInt32();
            MeanCeilings = new float[MeanCeilingSize];
            for (uint i = 0; i < MeanCeilingSize; i++)
            {
                MeanCeilings[i] = reader.ReadSingle();
            }

            MeanFloorSize = reader.ReadUInt32();
            MeanFloors = new float[MeanFloorSize];
            for (uint i = 0; i < MeanFloorSize; i++)
            {
                MeanFloors[i] = reader.ReadSingle();
            }

            VarBlockNum = reader.ReadUInt32();
            VarBlockSizes = new uint[VarBlockNum];
            for (uint i = 0; i < VarBlockNum; i++)
            {
                VarBlockSizes[i] = reader.ReadUInt32();
            }

            VarianceFloorSize = reader.ReadUInt32();
            VarianceFloors = new float[VarianceFloorSize];
            for (uint i = 0; i < VarianceFloorSize; i++)
            {
                VarianceFloors[i] = reader.ReadSingle();
            }

            HasMeanXform = reader.ReadUInt32() != 0;
            HasVarXform = reader.ReadUInt32() != 0;
            HasMeanBias = reader.ReadUInt32() != 0;
            HasVarBias = reader.ReadUInt32() != 0;
            IsFixedPoint = reader.ReadUInt32() != 0;
            BiasBits = reader.ReadUInt32();
            MatrixBits = reader.ReadUInt32();
            HasCodebook = reader.ReadUInt32() != 0;
            Validate();
            return this;
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validate instance status.
        /// </summary>
        private void Validate()
        {
            if (!HasMeanXform && !HasVarXform)
            {
                throw new InvalidDataException();
            }

            if (BiasBits < 4 || BiasBits > 32 ||
                MatrixBits < 4 || MatrixBits > 32 ||
                HasCodebook != false)
            {
                throw new InvalidDataException();
            }
        }
        #endregion
    }

    /// <summary>
    /// Model header.
    /// </summary>
    public class HtsModelHeader : IBinarySerializer<HtsModelHeader>
    {
        #region Construction
        /// <summary>
        /// Initializes a new instance of the <see cref="HtsModelHeader" /> class.
        /// </summary>
        /// <param name="isGuassian">If it's a Gaussian header.</param>
        public HtsModelHeader(bool isGuassian = true)
        {
            IsGaussian = isGuassian;
        }
        #endregion

        #region Fields
        /// <summary>
        /// Gets or sets model type of current model.
        /// </summary>
        public HmmModelType ModelType
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets location of the algorithm id string in the string pool.
        /// </summary>
        public uint AlgorithmIdOffset
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether current model has node ids for each child decision tree node.
        /// </summary>
        public uint HasChildNodeId
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets HMM model distribution type.
        /// </summary>
        public ModelDistributionType Distribution
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the number of state of the model.
        /// </summary>
        public uint StateCount
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the number of stream of this model.
        /// </summary>
        public uint StreamCount
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the array of stream widths of this model, aligned with the number of streams.
        /// </summary>
        public int[] StreamWidths
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the array of stream indexes of this model, aligned with the number of streams.
        /// </summary>
        public int[] StreamIndexes
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the offset position of the decision tree in font.
        /// </summary>
        public uint TreeOffset
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the size of the decision tree in font.
        /// </summary>
        public uint TreeSize
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the offset position of the HMM stream in font.
        /// </summary>
        public uint StreamOffset
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the size of the HMM stream in font.
        /// </summary>
        public uint StreamSize
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the dynamic window set used to calculate dynamic features.
        /// </summary>
        public DynamicWindowSet WindowSet
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether current model is Guassian or transformation.
        /// </summary>
        public bool IsGaussian
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets Gaussian distribution model configuration.
        /// </summary>
        public GaussianConfig GaussianConfig
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets linear transform model configuration.
        /// </summary>
        public LinXformConfig LinXformConfig
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets The size of the extension area of datat.
        /// </summary>
        public uint ExtensionAreaSize
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the F0 customization setting.
        /// </summary>
        public HtsF0CustomizedGeneration F0CustomizedGeneration
        {
            get;
            set;
        }

        #endregion

        #region Public operations

        /// <summary>
        /// Saves instance into data writer.
        /// </summary>
        /// <param name="writer">Target data writer to serialize.</param>
        /// <returns>Number of bytes written out.</returns>
        public uint Save(DataWriter writer)
        {
            Helper.ThrowIfNull(writer);
            uint size = 0;

            size += writer.Write((uint)ModelType);
            size += writer.Write((uint)AlgorithmIdOffset);
            size += writer.Write((uint)HasChildNodeId);
            Debug.Assert(HasChildNodeId == 0, "Not supported.");
            size += writer.Write((uint)Distribution);

            size += writer.Write((uint)StateCount);
            size += writer.Write((uint)StreamCount);
            for (uint i = 0; i < StreamCount; i++)
            {
                size += writer.Write((uint)StreamWidths[i]);
            }

            for (uint i = 0; i < StreamCount; i++)
            {
                size += writer.Write((uint)StreamIndexes[i]);
            }

            size += writer.Write((uint)TreeOffset);
            size += writer.Write((uint)TreeSize);
            size += writer.Write((uint)StreamOffset);
            size += writer.Write((uint)StreamSize);

            uint windowSetSize = WindowSet.Save(writer);

#if SERIALIZATION_CHECKING
            ConsistencyChecker.Check(WindowSet, new DynamicWindowSet().Load(writer.BaseStream.Excerpt(windowSetSize)));
#endif

            size += windowSetSize;

            uint configSize = 0;
            if (IsGaussian)
            {
                Debug.Assert(GaussianConfig != null, "Gaussian config should exist.");
                configSize = GaussianConfig.Save(writer);
#if SERIALIZATION_CHECKING
                ConsistencyChecker.Check(GaussianConfig, new GaussianConfig().Load(writer.BaseStream.Excerpt(gaussianSize)));
#endif
            }
            else
            {
                Debug.Assert(LinXformConfig != null, "Linear transform config should exist.");
                configSize = LinXformConfig.Save(writer);
            }

            size += configSize;

            if (F0CustomizedGeneration != null)
            {
                ExtensionAreaSize = (uint)((sizeof(uint) * 2) + (sizeof(float) * (F0CustomizedGeneration.Window.Count + 2)));
                size += writer.Write((uint)ExtensionAreaSize);

                size += writer.Write((uint)ExtensionAreaType.F0Customization);

                size += writer.Write((uint)F0CustomizedGeneration.Window.Count);
                for (int i = 0; i < F0CustomizedGeneration.Window.Count; i++)
                {
                    size += writer.Write(F0CustomizedGeneration.Window[i]);
                }

                size += writer.Write(F0CustomizedGeneration.EnhanceRate);
                size += writer.Write(F0CustomizedGeneration.Mean);
            }
            else
            {
                ExtensionAreaSize = 0;
                size += writer.Write((uint)ExtensionAreaSize);
            }

            Debug.Assert(size % sizeof(uint) == 0, "Data should be 4-byte aligned.");

            return size;
        }

        /// <summary>
        /// Loads model config from binary reader.
        /// </summary>
        /// <param name="reader">Binary reader.</param>
        /// <returns>Model config.</returns>
        public HtsModelHeader Load(BinaryReader reader)
        {
            Helper.ThrowIfNull(reader);
            ModelType = (HmmModelType)reader.ReadUInt32();
            AlgorithmIdOffset = reader.ReadUInt32();
            HasChildNodeId = reader.ReadUInt32();
            Debug.Assert(HasChildNodeId == 0, "Not supported.");
            Distribution = (ModelDistributionType)reader.ReadUInt32();

            StateCount = reader.ReadUInt32();
            StreamCount = reader.ReadUInt32();
            StreamWidths = new int[StreamCount];
            for (uint i = 0; i < StreamCount; i++)
            {
                StreamWidths[i] = (int)reader.ReadUInt32();
            }

            StreamIndexes = new int[StreamCount];
            for (uint i = 0; i < StreamCount; i++)
            {
                StreamIndexes[i] = (int)reader.ReadUInt32();
            }

            TreeOffset = reader.ReadUInt32();
            TreeSize = reader.ReadUInt32();
            StreamOffset = reader.ReadUInt32();
            StreamSize = reader.ReadUInt32();

            WindowSet = new DynamicWindowSet();
            WindowSet.Load(reader);

            if (IsGaussian)
            {
                GaussianConfig = new GaussianConfig();
                GaussianConfig.Load(reader);
            }
            else
            {
                LinXformConfig = new LinXformConfig();
                LinXformConfig.Load(reader);
            }

            ExtensionAreaSize = reader.ReadUInt32();

            if (ExtensionAreaSize != 0)
            {
                uint extensionType = reader.ReadUInt32();
                Debug.Assert(extensionType == (uint)ExtensionAreaType.F0Customization);

                F0CustomizedGeneration = new HtsF0CustomizedGeneration();

                uint windowLength = reader.ReadUInt32();
                F0CustomizedGeneration.Window = new List<float>();
                for (int i = 0; i < windowLength; ++i)
                {
                    F0CustomizedGeneration.Window.Add(reader.ReadSingle());
                }

                F0CustomizedGeneration.EnhanceRate = reader.ReadSingle();
                F0CustomizedGeneration.Mean = reader.ReadSingle();

                Debug.Assert(ExtensionAreaSize == (uint)((sizeof(uint) * 2) + (sizeof(float) * (windowLength + 2))));
            }

            return this;
        }
        #endregion
    }
}