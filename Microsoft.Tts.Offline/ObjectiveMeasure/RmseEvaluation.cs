//----------------------------------------------------------------------------
// <copyright file="RmseEvaluation.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This class calculate the Root-Mean-Square-Error distance (Euclidean Distance)
//     and correlation coefficient between two groups of acoustic features
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.ObjectiveMeasure
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Htk;
    using Microsoft.Tts.Offline.Utility;
    using Microsoft.Tts.Offline.Waveform;

    /// <summary>
    /// This class calculate the Root-Mean-Square-Error distance (Euclidean Distance)
    /// And correlation coefficient between two groups of acoustic features.
    /// </summary>
    public static class RmseEvaluation
    {
        #region Fields

        // Some constant used in this class

        /// <summary>
        /// Dimension for FFT.
        /// </summary>
        public const int FFTDim = 1024;

        /// <summary>
        /// Time length for a frame, by second.
        /// </summary>
        public const double FrameLength = 0.005;

        /// <summary>
        /// Pi.
        /// </summary>
        public const double Pi = Math.PI;

        /// <summary>
        /// Fake RUS duration state to 2 to load the duration file. 
        /// </summary>
        public const int RusDurationState = 2;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the number of states in one phone.
        /// </summary>
        public static int StateCount { get; set; }

        #endregion

        #region Public Methods

        #region Process SPS

        /// <summary>
        /// Given reference and target duration, f0 and lsp file for a sentence,
        /// Return the evaluation result for this sentence.
        /// </summary>
        /// <param name="referenceFiles">
        /// Reference duration, f0, and lsp files.
        /// </param>
        /// <param name="targetFiles">
        /// Target duration, f0, and lsp files.
        /// </param>
        /// <param name="referenceLpcOrder">
        /// Dimension of reference SPS voice font, e.g. 16 or 40.
        /// </param>
        /// <param name="targetLpcOrder">
        /// Dimension of target SPS voice font, e.g. 16 or 40.
        /// </param>
        /// <param name="voicedUnvoicedThreshold">
        /// The threshold to judge whether a frame is voiced or unvoiced, by Hz.
        /// </param>
        /// <param name="lowerBoundFrequency">
        /// Lower bound of frequency band on which to calculate the spectrum distance.
        /// </param>
        /// <param name="upperBoundFrequency">
        /// Upper bound of frequency band on which to calculate the spectrum distance.
        /// </param>
        /// <param name="sampleFrequency">
        /// Sample frequency of the voice font.
        /// </param>
        /// <returns>The evaluation result for this sentence.</returns>
        public static FullEvaluationResult Process(
            DataFiles referenceFiles,
            DataFiles targetFiles,
            int referenceLpcOrder,
            int targetLpcOrder,
            double voicedUnvoicedThreshold,
            double lowerBoundFrequency,
            double upperBoundFrequency,
            double sampleFrequency)
        {
            return Process(
                referenceFiles, targetFiles, new List<PhoneWordMap>(), referenceLpcOrder, targetLpcOrder,
                voicedUnvoicedThreshold, lowerBoundFrequency, upperBoundFrequency, sampleFrequency);
        }

        /// <summary>
        /// Given reference and target duration, f0 and lsp file for a sentence,
        /// Return the evaluation result for this sentence.
        /// </summary>
        /// <param name="referenceFiles">
        /// Reference duration, f0, and lsp files.
        /// </param>
        /// <param name="targetFiles">
        /// Target duration, f0, and lsp files.
        /// </param>
        /// <param name="phoneWordMaps">
        /// The mapping information between words and phonemes.
        /// </param>
        /// <param name="referenceLpcOrder">
        /// Dimension of reference SPS voice font, e.g. 16 or 40.
        /// </param>
        /// <param name="targetLpcOrder">
        /// Dimension of target SPS voice font, e.g. 16 or 40.
        /// </param>
        /// <param name="voicedUnvoicedThreshold">
        /// The threshold to judge whether a frame is voiced or unvoiced, by Hz.
        /// </param>
        /// <param name="lowerBoundFrequency">
        /// Lower bound of frequency band on which to calculate the spectrum distance.
        /// </param>
        /// <param name="upperBoundFrequency">
        /// Upper bound of frequency band on which to calculate the spectrum distance.
        /// </param>
        /// <param name="sampleFrequency">
        /// Sample frequency of the voice font.
        /// </param>
        /// <returns>The evaluation result for this sentence.</returns>
        public static FullEvaluationResult Process(
            DataFiles referenceFiles,
            DataFiles targetFiles,
            List<PhoneWordMap> phoneWordMaps,
            int referenceLpcOrder,
            int targetLpcOrder,
            double voicedUnvoicedThreshold,
            double lowerBoundFrequency,
            double upperBoundFrequency,
            double sampleFrequency)
        {
            FullEvaluationResult result = new FullEvaluationResult();
            result.ReferenceFiles = referenceFiles;
            result.TargetFiles = targetFiles;

            int phonemeCount = 0;
            phoneWordMaps.ForEach(x => phonemeCount += x.PhonemeCount);
            result.PhoneLevelResult = new PhoneLevelResult[phonemeCount].ToList();
            int startPhoneIndex = 0;
            foreach (PhoneWordMap phoneWordMap in phoneWordMaps)
            {
                for (int i = startPhoneIndex; i < startPhoneIndex + phoneWordMap.PhonemeCount; ++i)
                {
                    result.PhoneLevelResult[i] = new PhoneLevelResult()
                    {
                        WordOffset = phoneWordMap.WordOffset,
                        WordLength = phoneWordMap.WordLength
                    };
                }

                startPhoneIndex += phoneWordMap.PhonemeCount;
            }

            ////#region Process Duration

            List<List<int>> referenceDurationList = LoadDuration(referenceFiles.DurationFile);
            List<List<int>> targetDurationList = LoadDuration(targetFiles.DurationFile);
            if (phonemeCount.Equals(referenceDurationList.Count) &&
                phonemeCount.Equals(targetDurationList.Count))
            {
                for (int i = 0; i < phonemeCount; ++i)
                {
                    result.PhoneLevelResult[i].DurationDistance =
                        Math.Abs(referenceDurationList[i].Sum() - targetDurationList[i].Sum()) * FrameLength;
                }
            }
            else if (phoneWordMaps.Count != 0)
            {
                throw new DataMisalignedException(
                    "Phoneme counts don't match between phoneme word map info and duration file.");
            }

            result.Duration = ProcessDuration(referenceDurationList, targetDurationList);
            result.Duration.MaxDistance =
                GetPhoneLevelMaximumDistance(result.PhoneLevelResult, x => x.DurationDistance);

            ////#endregion

            ////#region Process F0 and voiced/unvoiced frames

            List<double> referenceF0List = LoadF0(referenceFiles.F0File);
            List<double> targetF0List = LoadF0(targetFiles.F0File);

            int referenceTotalDuration = referenceDurationList.Sum(x => x.Sum());
            int targetTotalDuration = targetDurationList.Sum(x => x.Sum());
            List<int> frameCountList = null;
            if (referenceF0List.Count == referenceTotalDuration &&
                targetF0List.Count == referenceTotalDuration)
            {
                frameCountList = new List<int>(referenceDurationList.Select(x => x.Sum()));
            }
            else if (referenceF0List.Count == targetTotalDuration &&
                targetF0List.Count == targetTotalDuration)
            {
                frameCountList = new List<int>(targetDurationList.Select(x => x.Sum()));
            }
            else
            {
                throw new DataMisalignedException(
                    "Total frame count of F0 doesn't match total duration.");
            }
            
            int startFrame = 0;
            List<List<int>> phoneLevelBothVoicedList = new List<int>[frameCountList.Count].ToList();
            for (int i = 0; i < phonemeCount; ++i)
            {
                List<double> referenceF0SubList = new List<double>();
                List<double> targetF0SubList = new List<double>();
                for (int j = startFrame; j < startFrame + frameCountList[i]; ++j)
                {
                    referenceF0SubList.Add(referenceF0List[j]);
                    targetF0SubList.Add(targetF0List[j]);
                }

                List<int> voicedList;
                ProcessUV(referenceF0SubList, targetF0SubList, voicedUnvoicedThreshold, out voicedList);
                phoneLevelBothVoicedList[i] = voicedList;
                result.PhoneLevelResult[i].F0Distance =
                    ProcessF0(referenceF0SubList, targetF0SubList, phoneLevelBothVoicedList[i]).RMSE;
                startFrame += frameCountList[i];
            }

            List<int> bothVoicedList;
            result.UVInfo =
                ProcessUV(referenceF0List, targetF0List, voicedUnvoicedThreshold, out bothVoicedList);
            result.F0 = ProcessF0(referenceF0List, targetF0List, bothVoicedList);
            result.F0.MaxDistance = GetPhoneLevelMaximumDistance(result.PhoneLevelResult, x => x.F0Distance);

            ////#endregion

            ////#region Process state level F0 and voiced/unvoiced frames

            List<double> referenceF0List_StateLevel =
                GetStateLevelF0(referenceF0List, referenceDurationList, voicedUnvoicedThreshold);
            List<double> targetF0List_StateLevel =
                GetStateLevelF0(targetF0List, referenceDurationList, voicedUnvoicedThreshold);
            List<int> bothVoicedList_StateLevel;
            result.UVInfo_StateLevel = ProcessUV(
                referenceF0List_StateLevel, targetF0List_StateLevel,
                voicedUnvoicedThreshold, out bothVoicedList_StateLevel);
            result.F0_StateLevel = ProcessF0(
                referenceF0List_StateLevel, targetF0List_StateLevel, bothVoicedList_StateLevel);

            ////#endregion

            ////#region Process LSP, this requires the reference LSP and target LSP has same dimension

            List<List<double>> referenceLspList = LoadLsp(referenceFiles.LspFile, referenceLpcOrder);
            List<List<double>> targetLspList = LoadLsp(targetFiles.LspFile, targetLpcOrder);
            if (referenceLspList.Count != referenceF0List.Count ||
                targetLspList.Count != targetF0List.Count)
            {
                throw new DataMisalignedException("LSP frame count doesn't match F0 frame count");
            }

            startFrame = 0;
            for (int i = 0; i < phonemeCount; ++i)
            {
                List<List<double>> referenceLspSubList =
                    referenceLspList.Skip(startFrame).Take(frameCountList[i]).ToList();
                List<List<double>> targetLspSubList =
                    targetLspList.Skip(startFrame).Take(frameCountList[i]).ToList();
                result.PhoneLevelResult[i].LspDistance = ProcessLsp(
                    FilterElementByIndex(referenceLspSubList, phoneLevelBothVoicedList[i]),
                    FilterElementByIndex(targetLspSubList, phoneLevelBothVoicedList[i])).RMSE;
                startFrame += frameCountList[i];
            }

            if (referenceLpcOrder == targetLpcOrder)
            {
                result.Lsp = ProcessLsp(referenceLspList, targetLspList);
                result.Lsp.MaxDistance = GetPhoneLevelMaximumDistance(result.PhoneLevelResult, x => x.LspDistance);
            }
            else
            {
                result.Lsp.RMSE = double.NaN;
                result.Lsp.MaxDistance = double.NaN;
                result.Lsp.CorrelationCoefficient = double.NaN;
                result.Lsp.InusedFrameNumber = -1;
            }

            ////#endregion
/*
            #region Process Log spectrum without gain

            List<List<double>> referenceFrequencyList =
                LspListToFrequencyList(referenceLspList, referenceLpcOrder);
            List<List<double>> targetFrequencyList = LspListToFrequencyList(targetLspList, targetLpcOrder);
            result.LogFrequency = ProcessSpectrum(
                FilterElementByIndex<List<double>>(referenceFrequencyList, bothVoicedList),
                FilterElementByIndex<List<double>>(targetFrequencyList, bothVoicedList),
                lowerBoundFrequency, upperBoundFrequency, sampleFrequency);

            #endregion
*/
            ////#region Process Gain

            List<double> referenceGainList = GetGain(referenceLspList);
            List<double> targetGainList = GetGain(targetLspList);
            startFrame = 0;
            for (int i = 0; i < phonemeCount; ++i)
            {
                List<double> referenceGainSubList = new List<double>();
                List<double> targetGainSubList = new List<double>();
                for (int j = startFrame; j < startFrame + frameCountList[i]; ++j)
                {
                    referenceGainSubList.Add(referenceGainList[j]);
                    targetGainSubList.Add(targetGainList[j]);
                }

                result.PhoneLevelResult[i].GainDistance =
                    ProcessGain(referenceGainSubList, targetGainSubList, phoneLevelBothVoicedList[i]).RMSE;
                startFrame += frameCountList[i];
            }

            result.Gain = ProcessGain(referenceGainList, targetGainList, bothVoicedList);
            result.Gain.MaxDistance =
                GetPhoneLevelMaximumDistance(result.PhoneLevelResult, x => x.GainDistance);

            ////#endregion
/*
            #region Process Log spectrum with gain

            List<List<double>> referenceSpectrumList =
                FrequencyListToSpectrumList(referenceFrequencyList, referenceGainList);
            List<List<double>> targetSpectrumList =
                FrequencyListToSpectrumList(targetFrequencyList, targetGainList);
            startFrame = 0;
            for (int i = 0; i < phonemeCount; ++i)
            {
                List<List<double>> referenceSpectrumSubList =
                    referenceSpectrumList.Skip(startFrame).Take(frameCountList[i]).ToList();
                List<List<double>> targetSpectrumSubList =
                    targetSpectrumList.Skip(startFrame).Take(frameCountList[i]).ToList();
                result.PhoneLevelResult[i].LogSpectrumDistance = ProcessSpectrum(
                    FilterElementByIndex(referenceSpectrumSubList, phoneLevelBothVoicedList[i]),
                    FilterElementByIndex(targetSpectrumSubList, phoneLevelBothVoicedList[i]),
                    lowerBoundFrequency, upperBoundFrequency, sampleFrequency).RMSE;
                startFrame += frameCountList[i];
            }

            result.LogSpectrum = ProcessSpectrum(
                FilterElementByIndex(referenceSpectrumList, bothVoicedList),
                FilterElementByIndex(targetSpectrumList, bothVoicedList),
                lowerBoundFrequency, upperBoundFrequency, sampleFrequency);
            result.LogSpectrum.MaxDistance =
                GetPhoneLevelMaximumDistance(result.PhoneLevelResult, x => x.LogSpectrumDistance);

            #endregion
*/
            return result;
        }

        /// <summary>
        /// Calculate RMSE distance of two lists of duration.
        /// </summary>
        /// <param name="referenceDurationList">Reference duration list.</param>
        /// <param name="targetDurationList">Target duration list.</param>
        /// <returns>
        /// Evaluation result, correlation coefficient and in-used frame number is set to void.
        /// </returns>
        public static EvaluationResult ProcessDuration(
            List<List<int>> referenceDurationList, List<List<int>> targetDurationList)
        {
            if (StateCount <= 0)
            {
                throw new InvalidOperationException(
                    Helper.NeutralFormat("The StateCount [{0}] should be set and be positive.", StateCount));
            }

            Func<List<int>, double> averageDelegate = new Func<List<int>, double>(Average<int>);
            List<double> referenceAverageFramesList =
                UnaryVectorCalculation<List<int>, double>(referenceDurationList, averageDelegate);
            List<double> targetAverageFramesList =
                UnaryVectorCalculation<List<int>, double>(targetDurationList, averageDelegate);

            EvaluationResult result = new EvaluationResult()
            {
                RMSE = Rmse<double>(
                    referenceAverageFramesList, targetAverageFramesList) * StateCount * FrameLength,
                MaxDistance = double.NaN,
                CorrelationCoefficient = double.NaN,
                InusedFrameNumber = -1
            };

            return result;
        }

        /// <summary>
        /// Calculate RMSE distance and correlation coefficient of two lists of F0.
        /// </summary>
        /// <param name="referenceF0List">Reference F0 list.</param>
        /// <param name="targetF0List">Target F0 list.</param>
        /// <param name="inusedList">
        /// Maybe you don't want to do calculation on the whole list, this is the mask.
        /// </param>
        /// <returns>
        /// Evaluation result, containing RMSE distance, correlation coefficient and inused frame number.
        /// </returns>
        public static EvaluationResult ProcessF0(
            List<double> referenceF0List, List<double> targetF0List, List<int> inusedList)
        {
            List<double> referenceF0ListInused = FilterElementByIndex(referenceF0List, inusedList);
            List<double> targetF0ListInused = FilterElementByIndex(targetF0List, inusedList);
            EvaluationResult result = new EvaluationResult()
            {
                RMSE = Rmse<double>(referenceF0ListInused, targetF0ListInused),
                MaxDistance = double.NaN,
                CorrelationCoefficient =
                    CorrelationCoefficient<double>(referenceF0ListInused, targetF0ListInused),
                InusedFrameNumber = inusedList.Count
            };

            return result;
        }

        /// <summary>
        /// Calculate RMSE distance of two lists of LSP.
        /// </summary>
        /// <param name="referenceLspList">Reference LSP list.</param>
        /// <param name="targetLspList">Target LSP list.</param>
        /// <returns>Evaluation result, correlation coefficient is set to void.</returns>
        public static EvaluationResult ProcessLsp(
            List<List<double>> referenceLspList, List<List<double>> targetLspList)
        {
            List<List<double>> referenceLspListWithoutGain = RemoveGain(referenceLspList, false);
            List<List<double>> targetLspListWithoutGain = RemoveGain(targetLspList, false);
            Func<List<double>, List<double>, double> rmseDelegate =
                new Func<List<double>, List<double>, double>(Rmse<double>);
            List<double> lspRmseList = BinaryVectorCalculation<List<double>, List<double>, double>(
                referenceLspListWithoutGain, targetLspListWithoutGain, rmseDelegate);
            EvaluationResult result = new EvaluationResult()
            {
                RMSE = Average<double>(lspRmseList),
                MaxDistance = double.NaN,
                CorrelationCoefficient = double.NaN,
                InusedFrameNumber = referenceLspList.Count
            };

            return result;
        }

        /// <summary>
        /// Calculate Weighted Euclidean distance of two lists of LSP.
        /// </summary>
        /// <param name="referenceLspList">Reference LSP list.</param>
        /// <param name="targetLspList">Target LSP list.</param>
        /// <param name="calculatedDimension">Calculated Dimension.</param>
        /// <returns>Evaluation result, correlation coefficient is set to void.</returns>
        public static EvaluationResult ProcessWeightedLsp(
            List<List<double>> referenceLspList, List<List<double>> targetLspList, int calculatedDimension)
        {
            if (referenceLspList == null || targetLspList == null)
            {
                throw new ArgumentNullException("the referenceLspList or the targetLspList");
            }

            if (referenceLspList.Count != targetLspList.Count)
            {
                throw new ArgumentException(
                   "The reference element number doesn't match the target element number");
            }

            if (referenceLspList.Count == 0)
            {
                throw new ArgumentException("The list is empty.");
            }

            List<List<double>> referenceLspListWithoutGain = RemoveGain(referenceLspList, false);
            List<List<double>> targetLspListWithoutGain = RemoveGain(targetLspList, false);
            EvaluationResult result = new EvaluationResult();

            int elementCount = referenceLspListWithoutGain.Count;
            List<double> lspEuclideanDistanceResultList = new List<double>();
            for (int i = 0; i < elementCount; ++i)
            {
                lspEuclideanDistanceResultList.Add(WeightedLspEuclideanDistance(referenceLspListWithoutGain[i], targetLspListWithoutGain[i], calculatedDimension));
            }

            result.RMSE = Average<double>(lspEuclideanDistanceResultList);
            result.MaxDistance = lspEuclideanDistanceResultList.Max();
            result.CorrelationCoefficient = double.NaN;
            result.InusedFrameNumber = referenceLspListWithoutGain.Count;

            return result;
        }

        /// <summary>
        /// Calculate RMSE distance of two lists of spectrum.
        /// </summary>
        /// <param name="referenceSpectrumList">Reference spectrum list.</param>
        /// <param name="targetSpectrumList">Target spectrum list.</param>
        /// <param name="lowerBoundFrequency">Lower bound of frequency band to do band filter.</param>
        /// <param name="upperBoundFrequency">Upper bound of frequency band to do band filter.</param>
        /// <param name="sampleFrequency">Sample frequency for the voice.</param>
        /// <returns>Evaluation result, correlation coefficient is set to void.</returns>
        public static EvaluationResult ProcessSpectrum(
            List<List<double>> referenceSpectrumList, List<List<double>> targetSpectrumList,
            double lowerBoundFrequency, double upperBoundFrequency, double sampleFrequency)
        {
            List<List<double>> referenceSpectrumFilteredList = FrequencyBandFilter(
                referenceSpectrumList, lowerBoundFrequency, upperBoundFrequency, sampleFrequency);
            List<List<double>> targetSpectrumFilteredList = FrequencyBandFilter(
                targetSpectrumList, lowerBoundFrequency, upperBoundFrequency, sampleFrequency);
            Func<double, double> log10Delegate = new Func<double, double>(Math.Log10);
            List<List<double>> referenceLogSpectrumList =
                UnaryMatrixCalculation<double, double>(referenceSpectrumFilteredList, log10Delegate);
            List<List<double>> targetLogSpectrumList =
                UnaryMatrixCalculation<double, double>(targetSpectrumFilteredList, log10Delegate);
            Func<List<double>, List<double>, double> rmseDelegate =
                new Func<List<double>, List<double>, double>(Rmse<double>);
            List<double> logSpectrumRmseList = BinaryVectorCalculation<List<double>, List<double>, double>(
                referenceLogSpectrumList, targetLogSpectrumList, rmseDelegate);
            EvaluationResult result = new EvaluationResult()
            {
                RMSE = Average<double>(logSpectrumRmseList) * 20,
                MaxDistance = double.NaN,
                CorrelationCoefficient = double.NaN,
                InusedFrameNumber = referenceSpectrumList.Count
            };

            return result;
        }

        /// <summary>
        /// Calculate RMSE distance and correlation coefficient of two lists of F0.
        /// </summary>
        /// <param name="referenceGainList">Reference gain list.</param>
        /// <param name="targetGainList">Target gain list.</param>
        /// <param name="inusedList">
        /// Maybe you don't want to do calculation on the whole list, this is the mask.
        /// </param>
        /// <returns>
        /// Evaluation result, containing RMSE distance, correlation coefficient and inused frame number.
        /// </returns>
        public static EvaluationResult ProcessGain(
            List<double> referenceGainList, List<double> targetGainList, List<int> inusedList)
        {
            List<double> referenceGainListInused = FilterElementByIndex<double>(referenceGainList, inusedList);
            List<double> targetGainListInused = FilterElementByIndex<double>(targetGainList, inusedList);
            Func<double, double> log10Delegate = new Func<double, double>(Math.Log10);
            List<double> referenceLogGainListInused =
                UnaryVectorCalculation<double, double>(referenceGainListInused, log10Delegate);
            List<double> targetLogGainListInused =
                UnaryVectorCalculation<double, double>(targetGainListInused, log10Delegate);
            EvaluationResult result = new EvaluationResult()
            {
                RMSE = Rmse<double>(referenceLogGainListInused, targetLogGainListInused) * 20,
                MaxDistance = double.NaN,
                CorrelationCoefficient =
                    CorrelationCoefficient<double>(referenceLogGainListInused, targetLogGainListInused),
                InusedFrameNumber = inusedList.Count
            };

            return result;
        }

        /// <summary>
        /// Statistic on voiced/unvoiced frames number.
        /// </summary>
        /// <param name="referenceF0List">Reference F0 list.</param>
        /// <param name="targetF0List">Target F0 list.</param>
        /// <param name="voicedUnvoicedThreshold">Threshold to judge voiced/unvoiced frame, by Hz.</param>
        /// <param name="bothVoicedList">
        /// Return a list containing frame indexes which is voiced in both reference and target.
        /// </param>
        /// <returns>Voiced/unvoiced statistic result.</returns>
        public static UVStatistic ProcessUV(
            List<double> referenceF0List, List<double> targetF0List,
            double voicedUnvoicedThreshold, out List<int> bothVoicedList)
        {
            int voicedReferenceFrameCount = 0;
            int voicedTargetFrameCount = 0;
            bothVoicedList = FilterUnvoicedF0(
                referenceF0List, targetF0List, voicedUnvoicedThreshold,
                out voicedReferenceFrameCount, out voicedTargetFrameCount);
            UVStatistic uvInfo = new UVStatistic()
            {
                VoicedFrameNumberInReference = voicedReferenceFrameCount,
                UnvoicedFrameNumberInReference =
                    referenceF0List.Count - voicedReferenceFrameCount,
                VoicedFrameNumberInTarget = voicedTargetFrameCount,
                UnvoicedFrameNumberInTarget =
                    targetF0List.Count - voicedTargetFrameCount,
                VoicedFrameNumberInBoth = bothVoicedList.Count,
                UnvoicedFrameNumberInBoth = referenceF0List.Count -
                    voicedReferenceFrameCount - voicedTargetFrameCount + bothVoicedList.Count,
                UnexpectedVoicedFrameNumber = voicedTargetFrameCount - bothVoicedList.Count,
                UnexpectedUnvoicedFrameNumber = targetF0List.Count -
                    (referenceF0List.Count - voicedReferenceFrameCount) - bothVoicedList.Count
            };

            return uvInfo;
        }

        /// <summary>
        /// Calculate average value from a list of FullEvaluationResult.
        /// </summary>
        /// <param name="evaluationResultList">
        /// A list of FullEvaluationResult.
        /// </param>
        /// <returns>
        /// Average value on each acoustic feature.
        /// </returns>
        public static EvaluationSummary CalculateSummary(List<FullEvaluationResult> evaluationResultList)
        {
            if (evaluationResultList == null)
            {
                throw new ArgumentNullException("evaluationResultList");
            }

            if (evaluationResultList.Count == 0)
            {
                throw new ArgumentException("The list is empty.");
            }

            EvaluationSummary summary = new EvaluationSummary();
            summary.RMSE_Duration = 0.0;
            summary.RMSE_F0 = 0.0;
            summary.CorrelationCoefficient_F0 = 0.0;
            summary.RMSE_F0_StateLevel = 0.0;
            summary.CorrelationCoefficient_F0_StateLevel = 0.0;
            summary.RMSE_Lsp = 0.0;
            summary.RMSE_LogSpectrumWithGain = 0.0;
            summary.RMSE_LogSpectrumWithoutGain = 0.0;
            summary.RMSE_Gain = 0.0;
            summary.CorrelationCoefficient_Gain = 0.0;
            foreach (FullEvaluationResult result in evaluationResultList)
            {
                summary.RMSE_Duration += result.Duration.RMSE;
                summary.RMSE_F0 += result.F0.RMSE;
                summary.CorrelationCoefficient_F0 += result.F0.CorrelationCoefficient;
                summary.RMSE_F0_StateLevel += result.F0_StateLevel.RMSE;
                summary.CorrelationCoefficient_F0_StateLevel += result.F0_StateLevel.CorrelationCoefficient;
                summary.RMSE_Lsp += result.Lsp.RMSE;
                summary.RMSE_LogSpectrumWithGain += result.LogSpectrum.RMSE;
                summary.RMSE_LogSpectrumWithoutGain += result.LogFrequency.RMSE;
                summary.RMSE_Gain += result.Gain.RMSE;
                summary.CorrelationCoefficient_Gain += result.Gain.CorrelationCoefficient;
            }

            summary.RMSE_Duration /= evaluationResultList.Count;
            summary.RMSE_F0 /= evaluationResultList.Count;
            summary.CorrelationCoefficient_F0 /= evaluationResultList.Count;
            summary.RMSE_F0_StateLevel /= evaluationResultList.Count;
            summary.CorrelationCoefficient_F0_StateLevel /= evaluationResultList.Count;
            summary.RMSE_Lsp /= evaluationResultList.Count;
            summary.RMSE_LogSpectrumWithGain /= evaluationResultList.Count;
            summary.RMSE_LogSpectrumWithoutGain /= evaluationResultList.Count;
            summary.RMSE_Gain /= evaluationResultList.Count;
            summary.CorrelationCoefficient_Gain /= evaluationResultList.Count;
            return summary;
        }

        /// <summary>
        /// Write the evaluation result for all sentences and the summary into several files.
        /// </summary>
        /// <param name="resultList">FullEvaluationResult list.</param>
        /// <param name="summary">EvaluationSummary.</param>
        /// <param name="reportFolder">Folder to output the result files and summary file.</param>
        public static void WriteResult(
            List<FullEvaluationResult> resultList,
            EvaluationSummary summary,
            string reportFolder)
        {
            Helper.EnsureFolderExist(reportFolder);
            using (StreamWriter durationResultWriter = new StreamWriter(
                Path.Combine(reportFolder, "Dur_EucDis.txt"), false, Encoding.Unicode))
            using (StreamWriter f0ResultWriter = new StreamWriter(
                Path.Combine(reportFolder, "F0_EucDis.txt"), false, Encoding.Unicode))
            using (StreamWriter f0_StateLevelResultWriter = new StreamWriter(
                Path.Combine(reportFolder, "F0_EucDis_StateLevel.txt"), false, Encoding.Unicode))
            using (StreamWriter lspResultWriter = new StreamWriter(
                Path.Combine(reportFolder, "LSP_EucDis.txt"), false, Encoding.Unicode))
            using (StreamWriter spectrumResultWriter = new StreamWriter(
                Path.Combine(reportFolder, "LogSpe_EucDis.txt"), false, Encoding.Unicode))
            {
                durationResultWriter.WriteLine("Column 1 - the 1st duration file");
                durationResultWriter.WriteLine("Column 2 - the 2nd duration file");
                durationResultWriter.WriteLine("Column 3 - the value of RMSE of durations");
                durationResultWriter.WriteLine("Column 4 - the value of maximum distance of durations");
                durationResultWriter.WriteLine(string.Empty);

                f0ResultWriter.WriteLine("Column  1 - the name of your original F0 file");
                f0ResultWriter.WriteLine("Column  2 - the number of voiced frames in the original file");
                f0ResultWriter.WriteLine("Column  3 - the number of unvoiced frames in the original file");
                f0ResultWriter.WriteLine("Column  4 - the name of your predicted F0 file");
                f0ResultWriter.WriteLine("Column  5 - the number of voiced frames in the predicted file");
                f0ResultWriter.WriteLine("Column  6 - the number of unvoiced frames in the predicted file");
                f0ResultWriter.WriteLine("Column  7 - the RMSE of F0");
                f0ResultWriter.WriteLine("Column  8 - the maximum distance of F0");
                f0ResultWriter.WriteLine(
                    "Column  9 - the number of voiced frames which appear at the same time in the two files");
                f0ResultWriter.WriteLine(
                    "Column 10 - the number of unvoiced frames which appear at the same time in the two files");
                f0ResultWriter.WriteLine(
                    "Column 11 - the number of voiced frames which should be unvoiced in the predicted file");
                f0ResultWriter.WriteLine(
                    "Column 12 - the number of unvoiced frames which should be voiced in the predicted file");
                f0ResultWriter.WriteLine("Column 13 - the correlation coefficient of the two files");
                f0ResultWriter.WriteLine(string.Empty);

                f0_StateLevelResultWriter.WriteLine("Column  1 - the name of your original F0 file");
                f0_StateLevelResultWriter.WriteLine(
                    "Column  2 - the number of voiced frames in the original file");
                f0_StateLevelResultWriter.WriteLine(
                    "Column  3 - the number of unvoiced frames in the original file");
                f0_StateLevelResultWriter.WriteLine("Column  4 - the name of your predicted F0 file");
                f0_StateLevelResultWriter.WriteLine(
                    "Column  5 - the number of voiced frames in the predicted file");
                f0_StateLevelResultWriter.WriteLine(
                    "Column  6 - the number of unvoiced frames in the predicted file");
                f0_StateLevelResultWriter.WriteLine("Column  7 - the RMSE of F0");
                f0_StateLevelResultWriter.WriteLine(
                    "Column  8 - the number of voiced frames which appear at the same time in the two files");
                f0_StateLevelResultWriter.WriteLine(
                    "Column  9 - the number of unvoiced frames which appear at the same time in the two files");
                f0_StateLevelResultWriter.WriteLine(
                    "Column 10 - the number of voiced frames which should be unvoiced in the predicted file");
                f0_StateLevelResultWriter.WriteLine(
                    "Column 11 - the number of unvoiced frames which should be voiced in the predicted file");
                f0_StateLevelResultWriter.WriteLine("Column 12 - the correlation coefficient of the two files");
                f0_StateLevelResultWriter.WriteLine(string.Empty);

                lspResultWriter.WriteLine("Column 1 - the 1st LSP file (reference)");
                lspResultWriter.WriteLine("Column 2 - the 2nd LSP file (target)");
                lspResultWriter.WriteLine("Column 3 - the value of RMSE of LSP");
                lspResultWriter.WriteLine("Column 4 - maximum distance of LSP");
                lspResultWriter.WriteLine("Column 5 - the number of frames used when calculating");
                lspResultWriter.WriteLine(string.Empty);

                spectrumResultWriter.WriteLine("Column  1: LSP file name (reference)");
                spectrumResultWriter.WriteLine("Column  2: LSP file name (target)");
                spectrumResultWriter.WriteLine("Column  3: log-spectral RMSE distance considering gains");
                spectrumResultWriter.WriteLine("Column  4: log-spectral maximum distance considering gains");
                spectrumResultWriter.WriteLine("Column  5: log-spectral RMSE distance ignoring gains");
                spectrumResultWriter.WriteLine("Column  6: the number of voiced frames");
                spectrumResultWriter.WriteLine("Column  7: RMSE of gains");
                spectrumResultWriter.WriteLine("Column  8: maximum distance of gains");
                spectrumResultWriter.WriteLine("Column  9: correlation coefficient of gains");
                spectrumResultWriter.WriteLine(string.Empty);

                int sentenceId = 0;
                foreach (FullEvaluationResult result in resultList)
                {
                    durationResultWriter.WriteLine(string.Format("{0}\t{1}\t{2}\t{3}",
                        result.ReferenceFiles.DurationFile,
                        result.TargetFiles.DurationFile,
                        result.Duration.RMSE,
                        result.Duration.MaxDistance));
                    f0ResultWriter.WriteLine(
                        string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}",
                        result.ReferenceFiles.F0File,
                        result.UVInfo.VoicedFrameNumberInReference,
                        result.UVInfo.UnvoicedFrameNumberInReference,
                        result.TargetFiles.F0File,
                        result.UVInfo.VoicedFrameNumberInTarget,
                        result.UVInfo.UnvoicedFrameNumberInTarget,
                        result.F0.RMSE,
                        result.F0.MaxDistance,
                        result.UVInfo.VoicedFrameNumberInBoth,
                        result.UVInfo.UnvoicedFrameNumberInBoth,
                        result.UVInfo.UnexpectedVoicedFrameNumber,
                        result.UVInfo.UnexpectedUnvoicedFrameNumber,
                        result.F0.CorrelationCoefficient));
                    f0_StateLevelResultWriter.WriteLine(
                        string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}",
                        result.ReferenceFiles.F0File,
                        result.UVInfo_StateLevel.VoicedFrameNumberInReference,
                        result.UVInfo_StateLevel.UnvoicedFrameNumberInReference,
                        result.TargetFiles.F0File,
                        result.UVInfo_StateLevel.VoicedFrameNumberInTarget,
                        result.UVInfo_StateLevel.UnvoicedFrameNumberInTarget,
                        result.F0_StateLevel.RMSE,
                        result.UVInfo_StateLevel.VoicedFrameNumberInBoth,
                        result.UVInfo_StateLevel.UnvoicedFrameNumberInBoth,
                        result.UVInfo_StateLevel.UnexpectedVoicedFrameNumber,
                        result.UVInfo_StateLevel.UnexpectedUnvoicedFrameNumber,
                        result.F0_StateLevel.CorrelationCoefficient));
                    lspResultWriter.WriteLine(string.Format("{0}\t{1}\t{2}\t{3}\t{4}",
                        result.ReferenceFiles.LspFile,
                        result.TargetFiles.LspFile,
                        result.Lsp.RMSE,
                        result.Lsp.MaxDistance,
                        result.Lsp.InusedFrameNumber));
                    spectrumResultWriter.WriteLine(
                        string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}",
                        result.ReferenceFiles.LspFile,
                        result.TargetFiles.LspFile,
                        result.LogSpectrum.RMSE,
                        result.LogSpectrum.MaxDistance,
                        result.LogFrequency.RMSE,
                        result.LogSpectrum.InusedFrameNumber,
                        result.Gain.RMSE,
                        result.Gain.MaxDistance,
                        result.Gain.CorrelationCoefficient));
                    if (result.PhoneLevelResult.Count > 0)
                    {
                        string phoneLevelResultDir = Path.Combine(reportFolder, "PhoneLevelResult");
                        Helper.EnsureFolderExist(phoneLevelResultDir);
                        using (StreamWriter phoneLevelResultWriter = new StreamWriter(
                            Path.Combine(phoneLevelResultDir, sentenceId.ToString("d10") + ".log")))
                        {
                            foreach (PhoneLevelResult phoneLevelResult in result.PhoneLevelResult)
                            {
                                phoneLevelResultWriter.WriteLine(string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}",
                                    phoneLevelResult.WordOffset, phoneLevelResult.WordLength,
                                    phoneLevelResult.DurationDistance, phoneLevelResult.F0Distance,
                                    phoneLevelResult.LspDistance, phoneLevelResult.GainDistance));
                            }
                        }
                    }

                    ++sentenceId;
                }

                using (StreamWriter summaryWriter = new StreamWriter(
                Path.Combine(reportFolder, "Final_Result.txt"), false, Encoding.Unicode))
                {
                    summaryWriter.WriteLine(string.Format("Average of {0} sentences:", resultList.Count));
                    summaryWriter.WriteLine(string.Empty);
                    summaryWriter.WriteLine(string.Empty);
                    summaryWriter.WriteLine(string.Format(
                        "RMSE of duration:\t{0} (second/phone)", summary.RMSE_Duration));
                    summaryWriter.WriteLine(string.Empty);
                    summaryWriter.WriteLine(string.Format("RMSE of F0:\t{0} (Hz/frame)", summary.RMSE_F0));
                    summaryWriter.WriteLine(string.Format(
                        "CorrCoef of F0:\t{0}", summary.CorrelationCoefficient_F0));
                    summaryWriter.WriteLine(string.Empty);
                    summaryWriter.WriteLine(string.Format(
                        "RMSE of F0 (StateLevel):\t{0} (Hz/state)", summary.RMSE_F0_StateLevel));
                    summaryWriter.WriteLine(string.Format(
                        "CorrCoef of F0 (StateLevel):\t{0}", summary.CorrelationCoefficient_F0_StateLevel));
                    summaryWriter.WriteLine(string.Empty);
                    summaryWriter.WriteLine(string.Format(
                        "RMSE of LSP:\t{0} (2pi rad/frame/dimension)", summary.RMSE_Lsp));
                    summaryWriter.WriteLine(string.Empty);
                    summaryWriter.WriteLine(string.Format(
                        "RMSE of log spectrum:\t{0} (dB)", summary.RMSE_LogSpectrumWithGain));
                    summaryWriter.WriteLine(string.Empty);
                    summaryWriter.WriteLine(string.Format(
                        "RMSE of log frequency:\t{0} (dB)", summary.RMSE_LogSpectrumWithoutGain));
                    summaryWriter.WriteLine(string.Empty);
                    summaryWriter.WriteLine(string.Format("RMSE of gain:\t{0} (dB)", summary.RMSE_Gain));
                    summaryWriter.WriteLine(string.Format(
                        "CorrCoef of gain:\t{0}", summary.CorrelationCoefficient_Gain));
                }
            }
        }

        #endregion

        #region Process RUS 

        /// <summary>
        /// Given reference and target duration, f0 and lsp file for a sentence,
        /// Return the evaluation result for this sentence.
        /// </summary>
        /// <param name="referenceFiles">
        /// Reference duration, f0, and lsp files, it is from SPS. 
        /// </param>
        /// <param name="targetFiles">
        /// Target duration, f0, and lsp files, it is from RUS best path. 
        /// </param>
        /// <param name="referenceLpcOrder">
        /// Dimension of reference HTS voice font, e.g. 16 or 40.
        /// </param>
        /// <param name="targetLpcOrder">
        /// Dimension of target HTS voice font, e.g. 16 or 40.
        /// </param>
        /// <param name="voicedUnvoicedThreshold">
        /// The threshold to judge whether a frame is voiced or unvoiced, by Hz.
        /// </param>
        /// <param name="calculatedDimension">
        /// The calculated dimension against weighted lsp distance.
        /// </param>
        /// <param name="lowerBoundFrequency">
        /// Lower bound of frequency band on which to calculate the spectrum distance.
        /// </param>
        /// <param name="upperBoundFrequency">
        /// Upper bound of frequency band on which to calculate the spectrum distance.
        /// </param>
        /// <param name="sampleFrequency">
        /// Sample frequency of the voice font.
        /// </param>
        /// <param name="bestPath">
        /// BestPath of RUS.
        /// </param>
        /// <returns>FullEvaluationResult.</returns>
        public static FullEvaluationResult ProcessRUS(
            DataFiles referenceFiles,
            DataFiles targetFiles,
            int referenceLpcOrder,
            int targetLpcOrder,
            double voicedUnvoicedThreshold,
            int calculatedDimension,
            double lowerBoundFrequency,
            double upperBoundFrequency,
            int sampleFrequency,
            UnitLatticeSentence bestPath)
        {
            FullEvaluationResult result = new FullEvaluationResult();
            result.ReferenceFiles = referenceFiles;
            result.TargetFiles = targetFiles;

            result.PhoneLevelResult = GeneratePhoneLevelInfo(bestPath);

            // F0 and voiced/unvoiced frames, OutlierF0 frame
            List<double> referencef0List = LoadF0(referenceFiles.F0File);
            List<double> targetf0List = LoadF0(targetFiles.F0File);
            List<int> bothVoicedList;
            result.UVInfo = ProcessUV(referencef0List, targetf0List, voicedUnvoicedThreshold, out bothVoicedList);
            result.F0 = ProcessF0(referencef0List, targetf0List, bothVoicedList);
            result.OutlierF0 = ProcessF0Outlier(referencef0List, targetf0List, bothVoicedList);

            // Caculate phone levle F0
            foreach (PhoneLevelResult phoneLevelResult in result.PhoneLevelResult)
            {
                List<double> referencePhoneLevelF0 = referencef0List.Where((value, index) => index >= phoneLevelResult.StartFrame && index < (phoneLevelResult.StartFrame + phoneLevelResult.FrameLength)).ToList();
                List<double> targetPhoneLevelF0 = targetf0List.Where((value, index) => index >= phoneLevelResult.StartFrame && index < (phoneLevelResult.StartFrame + phoneLevelResult.FrameLength)).ToList();

                List<int> bothVoicedListPhoneLevel;
                phoneLevelResult.UVInfo = ProcessUV(referencePhoneLevelF0, targetPhoneLevelF0, voicedUnvoicedThreshold, out bothVoicedListPhoneLevel);
                EvaluationResult f0 = ProcessF0(referencePhoneLevelF0, targetPhoneLevelF0, bothVoicedListPhoneLevel);
                phoneLevelResult.F0Distance = f0.RMSE;
                phoneLevelResult.F0Correlation = f0.CorrelationCoefficient;
                phoneLevelResult.BothVoicedList = bothVoicedListPhoneLevel;

                OutlierF0Statistic outlierF0 = ProcessF0Outlier(referencePhoneLevelF0, targetPhoneLevelF0, bothVoicedListPhoneLevel);
                phoneLevelResult.F0OutlierFrameNumber = outlierF0.OutlierFrameNumber;
            }

            // LSP, this process requires the reference LSP and target LSP has same dimension
            List<List<double>> referenceLspList = LoadLsp(referenceFiles.LspFile, referenceLpcOrder);
            List<List<double>> targetLspList = LoadLsp(targetFiles.LspFile, targetLpcOrder);
            if (referenceLpcOrder == targetLpcOrder)
            {
                result.Lsp = ProcessWeightedLsp(referenceLspList, targetLspList, calculatedDimension);
            }
            else
            {
                result.Lsp.RMSE = double.NaN;
                result.Lsp.MaxDistance = double.NaN;
                result.Lsp.CorrelationCoefficient = double.NaN;
                result.Lsp.InusedFrameNumber = -1;
            }

            // Phone levle weighted LSP 
            foreach (PhoneLevelResult phoneLevelResult in result.PhoneLevelResult)
            {
                List<List<double>> referencePhoneLevelLspList = referenceLspList.Where((value, index) => index >= phoneLevelResult.StartFrame && index < (phoneLevelResult.StartFrame + phoneLevelResult.FrameLength)).ToList();
                List<List<double>> targetPhoneLevelLspList = targetLspList.Where((value, index) => index >= phoneLevelResult.StartFrame && index < (phoneLevelResult.StartFrame + phoneLevelResult.FrameLength)).ToList();
                EvaluationResult lsp = ProcessWeightedLsp(referencePhoneLevelLspList, targetPhoneLevelLspList, calculatedDimension);
                phoneLevelResult.WeightedLSPDistance = lsp.RMSE;
            }

            // Log spectrum without gain in voiced part
            List<List<double>> referenceVoicedFrequencyList = LspListToFrequencyList(
                FilterElementByIndex<List<double>>(referenceLspList, bothVoicedList),
                referenceLpcOrder);
            List<List<double>> targetVoicedFrequencyList = LspListToFrequencyList(
                FilterElementByIndex<List<double>>(targetLspList, bothVoicedList),
                targetLpcOrder);
            result.LogFrequencyVoiced = ProcessSpectrum(referenceVoicedFrequencyList, targetVoicedFrequencyList,
                lowerBoundFrequency, upperBoundFrequency, sampleFrequency);

            // Log spectrum without gain of both voiced and unvoiced part
            List<List<double>> referenceFrequencyList = LspListToFrequencyList(referenceLspList, referenceLpcOrder);
            List<List<double>> targetFrequencyList = LspListToFrequencyList(targetLspList, targetLpcOrder);
            result.LogFrequency = ProcessSpectrum(referenceFrequencyList, targetFrequencyList,
                lowerBoundFrequency, upperBoundFrequency, sampleFrequency);

            // Phone level spectrum caculation
            foreach (PhoneLevelResult phoneLevelResult in result.PhoneLevelResult)
            {
                List<List<double>> referencePhoneLevelFrequencyList = referenceFrequencyList.Where((value, index) => index >= phoneLevelResult.StartFrame && index < (phoneLevelResult.StartFrame + phoneLevelResult.FrameLength)).ToList();
                List<List<double>> targetPhoneLevelFrequencyList = targetFrequencyList.Where((value, index) => index >= phoneLevelResult.StartFrame && index < (phoneLevelResult.StartFrame + phoneLevelResult.FrameLength)).ToList();
                EvaluationResult spectrum = ProcessSpectrum(referencePhoneLevelFrequencyList, targetPhoneLevelFrequencyList, lowerBoundFrequency, upperBoundFrequency, sampleFrequency);
                phoneLevelResult.LogSpectrumDistance = spectrum.RMSE;
            }
            
            // Gain
            List<double> referenceGainList = GetGain(referenceLspList);
            List<double> targetGainList = GetGain(targetLspList);
            result.Gain = ProcessGain(referenceGainList, targetGainList, bothVoicedList);

            // Phone level Gain distance
            foreach (PhoneLevelResult phoneLevelResult in result.PhoneLevelResult)
            {
                List<double> referencePhoneLevelGain = referenceGainList.Where((value, index) => index >= phoneLevelResult.StartFrame && index < (phoneLevelResult.StartFrame + phoneLevelResult.FrameLength)).ToList();
                List<double> targetPhoneLevelGain = targetGainList.Where((value, index) => index >= phoneLevelResult.StartFrame && index < (phoneLevelResult.StartFrame + phoneLevelResult.FrameLength)).ToList();
                EvaluationResult gain = ProcessGain(referencePhoneLevelGain, targetPhoneLevelGain, phoneLevelResult.BothVoicedList);
                phoneLevelResult.GainDistance = gain.RMSE;
            }

            // Duration
            List<List<int>> referenceDurationList = LoadDuration(referenceFiles.DurationFile);
            List<List<int>> targetDurationList = LoadDuration(targetFiles.DurationFile);
            result.Duration = ProcessDuration(referenceDurationList, targetDurationList);

            // Concatenation cost and Continues units in the best path
            result.CCInfo = ProcessCC(bestPath);
            result.VoiceCCInfo = ProcessVoicedCC(bestPath, targetf0List, voicedUnvoicedThreshold, sampleFrequency);
            result.ContinueUnitInfo = ProcessContinueUnitCount(bestPath);

            return result;
        }

        /// <summary>
        /// Calculate outlier frame which the distance between reference f0 and target f0 larger than 10hz.
        /// </summary>
        /// <param name="referenceF0List">Reference f0 list.</param>
        /// <param name="targetF0List">Target f0 list.</param>
        /// <param name="inusedList">
        /// Maybe you don't want to do calculation on the whole list, this is the mask.
        /// </param>
        /// <returns>
        /// Outlier f0 info, containing outlier frame number, total frame number frame number, outlier ratio.
        /// </returns>
        public static OutlierF0Statistic ProcessF0Outlier(
            List<double> referenceF0List, List<double> targetF0List, List<int> inusedList)
        {
            OutlierF0Statistic result = new OutlierF0Statistic();

            List<double> referenceF0ListInused = FilterElementByIndex(referenceF0List, inusedList);
            List<double> targetF0ListInused = FilterElementByIndex(targetF0List, inusedList);

            double outlierF0Threshhold = 10.0;
            int outlierFrameNumber = 0;
            for (int i = 0; i < referenceF0ListInused.Count; i++)
            {
                if (Math.Abs(referenceF0ListInused[i] - targetF0ListInused[i]) > outlierF0Threshhold)
                {
                    outlierFrameNumber++;
                }
            }

            result.OutlierFrameNumber = outlierFrameNumber;
            result.TotalFrameNumber = inusedList.Count;
            result.OutlierRatio = (double)outlierFrameNumber / inusedList.Count;

            return result;
        }

        /// <summary>
        /// Process the concatenation info of the best path.
        /// </summary>
        /// <param name="bestPath">Best path.</param>
        /// <returns>ConcatenationCostInfo.</returns>
        public static ConcatenationCostInfo ProcessCC(UnitLatticeSentence bestPath)
        {
            ConcatenationCostInfo conInfo = new ConcatenationCostInfo();
            conInfo.BoundaryNumber = bestPath.Phones.Count - 1;
            conInfo.TotalConcatenationCost = 0.0; 
            
            // start from index 1, because first node don't have CC and continue info
            for (int i = 1; i < bestPath.Phones.Count; i++)
            {
                UnitLatticePhone unitLatticePhone = bestPath.Phones[i];
                if (unitLatticePhone.Candidates.Count == 1)
                {
                    UnitLatticePhoneCandidate unitLatticePhoneCandidate = unitLatticePhone.Candidates[0];
                    conInfo.TotalConcatenationCost += unitLatticePhoneCandidate.ConCost;
                }
                else
                {
                    throw new ApplicationException("The candidate on the best path larger than 1");
                }
           }

           return conInfo;
        }

        /// <summary>
        /// Process the voiced boundary of the bestpath.
        /// </summary>
        /// <param name="bestPath">Best path.</param>
        /// <param name="bestPathF0s">Bestpath F0 info.</param>
        /// <param name="voicedUnvoicedThreshold">Voiced and unvoiced threshold.</param>
        /// <param name="sampleRate">Sample rate of the wave.</param>
        /// <returns>ConcatenationCostInfo.</returns>
        public static ConcatenationCostInfo ProcessVoicedCC(UnitLatticeSentence bestPath, List<double> bestPathF0s, double voicedUnvoicedThreshold, int sampleRate)
        {
            ConcatenationCostInfo conInfo = new ConcatenationCostInfo();
            conInfo.BoundaryNumber = 0;
            conInfo.TotalConcatenationCost = 0.0;
            int endFrameIndex = 0;  // record the end frame index of prevous unit in the generated wave

            // start from index 1, because first node don't have boundary with left phone
            for (int i = 1; i < bestPath.Phones.Count; i++)
            {
                // Get curent phone 
                UnitLatticePhone unitLatticePhone = bestPath.Phones[i];
                if (unitLatticePhone.Candidates.Count == 1)
                {
                    UnitLatticePhoneCandidate unitLatticePhoneCandidate = unitLatticePhone.Candidates[0];

                    // If there is silence phone on the left, that is not the voiced boundary, otherwise count the voiced boundary. 
                    if (!bestPath.Phones[i - 1].Text.Equals("sil") && !unitLatticePhoneCandidate.Text.Equals("sil"))
                    {
                        if (bestPathF0s[endFrameIndex] > voicedUnvoicedThreshold && bestPathF0s[endFrameIndex + 1] > voicedUnvoicedThreshold)
                        {
                            conInfo.BoundaryNumber++;
                            conInfo.TotalConcatenationCost += unitLatticePhoneCandidate.ConCost;
                        }
                    }

                    endFrameIndex += unitLatticePhoneCandidate.FrameLength;
                }
                else
                {
                    throw new ApplicationException("The candidate on the best path larger than 1");
                }
            }

            return conInfo;
        }

        /// <summary>
        /// Process the continue unit info.
        /// </summary>
        /// <param name="bestPath">Best path.</param>
        /// <returns>ContinueUnitInfo.</returns>
        public static ContinueUnitInfo ProcessContinueUnitCount(UnitLatticeSentence bestPath)
        {
            ContinueUnitInfo continueUnitInfo = new ContinueUnitInfo();
            continueUnitInfo.BoundaryNumber = bestPath.Phones.Count - 1;
            continueUnitInfo.TotalContinueUnitNumber = 0;

            // start from index 1, because first node don't have CC and continue info
            for (int i = 1; i < bestPath.Phones.Count; i++)
            {
                UnitLatticePhone unitLatticePhone = bestPath.Phones[i];
                if (unitLatticePhone.Candidates.Count == 1)
                {
                    UnitLatticePhoneCandidate unitLatticePhoneCandidate = unitLatticePhone.Candidates[0];
                    continueUnitInfo.TotalContinueUnitNumber += unitLatticePhoneCandidate.IsContinue ? 1 : 0;
                }
                else
                {
                    throw new ApplicationException("The candidate on the best path larger than 1");
                }
            }

            return continueUnitInfo;
        }

        /// <summary>
        /// Calculate average value from a list of FullEvaluationResult.
        /// </summary>
        /// <param name="evaluationResultList">
        /// A list of FullEvaluationResult.
        /// </param>
        /// <returns>
        /// Average value on each acoustic feature.
        /// </returns>
        public static EvaluationSummary CalculateSummaryRUS(List<FullEvaluationResult> evaluationResultList)
        {
            if (evaluationResultList == null)
            {
                throw new ArgumentNullException("evaluationResultList");
            }

            if (evaluationResultList.Count == 0)
            {
                throw new ArgumentException("The list is empty.");
            }

            EvaluationSummary summary = new EvaluationSummary();
            OutlierF0Statistic outlierF0Summary = new OutlierF0Statistic();
            UVStatistic uvStatisticSummary = new UVStatistic();
            ConcatenationCostInfo ccInfoSummary = new ConcatenationCostInfo();
            ConcatenationCostInfo voicedCCInfoSummary = new ConcatenationCostInfo();
            ContinueUnitInfo continureUnitInfoSummary = new ContinueUnitInfo();

            summary.RMSE_F0 = 0.0;
            summary.CorrelationCoefficient_F0 = 0.0;
            summary.OutlierF0Ratio = 0.0;
            summary.UVMismatchRatio = 0.0; 
            summary.RMSE_Lsp = 0.0;
            summary.RMSE_LogSpectrumWithoutGainVoiced = 0.0;
            summary.RMSE_LogSpectrumWithoutGain = 0.0; 
            summary.RMSE_Gain = 0.0;
            summary.CorrelationCoefficient_Gain = 0.0;
            summary.RMSE_Duration = 0.0;
            summary.CC = 0.0;
            summary.VoicedCC = 0.0;
            summary.ContiueUnitRatio = 0.0;
            outlierF0Summary.OutlierFrameNumber = 0;
            outlierF0Summary.TotalFrameNumber = 0;
            uvStatisticSummary.UnexpectedVoicedFrameNumber = 0;
            uvStatisticSummary.UnexpectedUnvoicedFrameNumber = 0;
            uvStatisticSummary.VoicedFrameNumberInReference = 0;
            uvStatisticSummary.UnvoicedFrameNumberInReference = 0;
            ccInfoSummary.BoundaryNumber = 0;
            ccInfoSummary.TotalConcatenationCost = 0.0;
            voicedCCInfoSummary.BoundaryNumber = 0;
            voicedCCInfoSummary.TotalConcatenationCost = 0.0;
            continureUnitInfoSummary.BoundaryNumber = 0;
            continureUnitInfoSummary.TotalContinueUnitNumber = 0;

            foreach (FullEvaluationResult result in evaluationResultList)
            {
                summary.RMSE_F0 += result.F0.RMSE;
                summary.CorrelationCoefficient_F0 += result.F0.CorrelationCoefficient;
                outlierF0Summary.OutlierFrameNumber += result.OutlierF0.OutlierFrameNumber;
                outlierF0Summary.TotalFrameNumber += result.OutlierF0.TotalFrameNumber;
                uvStatisticSummary.UnexpectedVoicedFrameNumber += result.UVInfo.UnexpectedVoicedFrameNumber;
                uvStatisticSummary.UnexpectedUnvoicedFrameNumber += result.UVInfo.UnexpectedUnvoicedFrameNumber;
                uvStatisticSummary.VoicedFrameNumberInReference += result.UVInfo.VoicedFrameNumberInReference;
                uvStatisticSummary.UnvoicedFrameNumberInReference += result.UVInfo.UnvoicedFrameNumberInReference;
                summary.RMSE_Lsp += result.Lsp.RMSE;
                summary.RMSE_LogSpectrumWithoutGainVoiced += result.LogFrequencyVoiced.RMSE;
                summary.RMSE_LogSpectrumWithoutGain += result.LogFrequency.RMSE;
                summary.RMSE_Gain += result.Gain.RMSE;
                summary.CorrelationCoefficient_Gain += result.Gain.CorrelationCoefficient;
                summary.RMSE_Duration += result.Duration.RMSE;
                ccInfoSummary.BoundaryNumber += result.CCInfo.BoundaryNumber;
                ccInfoSummary.TotalConcatenationCost += result.CCInfo.TotalConcatenationCost;
                voicedCCInfoSummary.BoundaryNumber += result.VoiceCCInfo.BoundaryNumber;
                voicedCCInfoSummary.TotalConcatenationCost += result.VoiceCCInfo.TotalConcatenationCost;
                continureUnitInfoSummary.BoundaryNumber += result.ContinueUnitInfo.BoundaryNumber;
                continureUnitInfoSummary.TotalContinueUnitNumber += result.ContinueUnitInfo.TotalContinueUnitNumber;
            }

            summary.RMSE_F0 /= evaluationResultList.Count;
            summary.CorrelationCoefficient_F0 /= evaluationResultList.Count;
            summary.RMSE_Lsp /= evaluationResultList.Count;
            summary.RMSE_LogSpectrumWithoutGainVoiced /= evaluationResultList.Count;
            summary.RMSE_LogSpectrumWithoutGain /= evaluationResultList.Count;
            summary.RMSE_Gain /= evaluationResultList.Count;
            summary.CorrelationCoefficient_Gain /= evaluationResultList.Count;
            summary.RMSE_Duration /= evaluationResultList.Count;
            summary.OutlierF0Ratio = (double)outlierF0Summary.OutlierFrameNumber / outlierF0Summary.TotalFrameNumber;
            summary.UVMismatchRatio = (double)uvStatisticSummary.UnexpectedVoicedFrameNumber / (uvStatisticSummary.VoicedFrameNumberInReference + uvStatisticSummary.UnvoicedFrameNumberInReference);
            summary.VUMismatchRatio = (double)uvStatisticSummary.UnexpectedUnvoicedFrameNumber / (uvStatisticSummary.VoicedFrameNumberInReference + uvStatisticSummary.UnvoicedFrameNumberInReference);
            summary.CC = (double)ccInfoSummary.TotalConcatenationCost / ccInfoSummary.BoundaryNumber;
            summary.VoicedCC = (double)voicedCCInfoSummary.TotalConcatenationCost / voicedCCInfoSummary.BoundaryNumber;
            summary.ContiueUnitRatio = (double)continureUnitInfoSummary.TotalContinueUnitNumber / continureUnitInfoSummary.BoundaryNumber;
            return summary;
        }

        /// <summary>
        /// RUS version of Write the evaluation result for all sentences and the summary into several files.
        /// </summary>
        /// <param name="resultList">FullEvaluationResult list.</param>
        /// <param name="summary">EvaluationSummary.</param>
        /// <param name="reportFolder">Folder to output the result files and summary file.</param>
        public static void WriteResultRUS(
            List<FullEvaluationResult> resultList,
            EvaluationSummary summary,
            string reportFolder)
        {
            Helper.EnsureFolderExist(reportFolder);

            using (StreamWriter f0ResultWriter = new StreamWriter(
                Path.Combine(reportFolder, "F0_EucDis.txt"), false, Encoding.Unicode))
            using (StreamWriter lspResultWriter = new StreamWriter(
                Path.Combine(reportFolder, "LSP_EucDis.txt"), false, Encoding.Unicode))
            using (StreamWriter spectrumResultWriter = new StreamWriter(
                Path.Combine(reportFolder, "LogSpe_EucDis.txt"), false, Encoding.Unicode))
            using (StreamWriter ccResultWriter = new StreamWriter(
                Path.Combine(reportFolder, "CC.txt"), false, Encoding.Unicode))
            using (StreamWriter durationResultWriter = new StreamWriter(
                Path.Combine(reportFolder, "Dur_EucDis.txt"), false, Encoding.Unicode))
            using (StreamWriter phoneLevelResultWriter = new StreamWriter(
                Path.Combine(reportFolder, "PhoneLevelResult.txt"), false, Encoding.Unicode))
            {
                f0ResultWriter.WriteLine("Column  1 - the name of your original F0 file");
                f0ResultWriter.WriteLine("Column  2 - the number of voiced frames in the original file");
                f0ResultWriter.WriteLine("Column  3 - the number of unvoiced frames in the original file");
                f0ResultWriter.WriteLine("Column  4 - the name of your predicted F0 file");
                f0ResultWriter.WriteLine("Column  5 - the number of voiced frames in the predicted file");
                f0ResultWriter.WriteLine("Column  6 - the number of unvoiced frames in the predicted file");
                f0ResultWriter.WriteLine("Column  7 - the RMSE of F0");
                f0ResultWriter.WriteLine("Column  8 - the maximum distance of F0");
                f0ResultWriter.WriteLine(
                    "Column  9 - the number of voiced frames which appear at the same time in the two files");
                f0ResultWriter.WriteLine(
                    "Column 10 - the number of unvoiced frames which appear at the same time in the two files");
                f0ResultWriter.WriteLine(
                    "Column 11 - the number of voiced frames which should be unvoiced in the predicted file");
                f0ResultWriter.WriteLine(
                    "Column 12 - the number of unvoiced frames which should be voiced in the predicted file");
                f0ResultWriter.WriteLine("Column 13 - the correlation coefficient of the two files");
                f0ResultWriter.WriteLine("Column 14 - the outlier frame number");
                f0ResultWriter.WriteLine("Column 15 - the outlier frame ratio");
                f0ResultWriter.WriteLine(string.Empty);

                lspResultWriter.WriteLine("Column 1 - the 1st LSP file (reference)");
                lspResultWriter.WriteLine("Column 2 - the 2nd LSP file (target)");
                lspResultWriter.WriteLine("Column 3 - the value of Weighted Eulidean distance of LSP");
                lspResultWriter.WriteLine("Column 4 - the value of maximum Weighted Eulidean distance of LSP");
                lspResultWriter.WriteLine("Column 5 - the number of frames used when calculating");
                lspResultWriter.WriteLine(string.Empty);

                spectrumResultWriter.WriteLine("Column  1: LSP file name (reference)");
                spectrumResultWriter.WriteLine("Column  2: LSP file name (target)");
                spectrumResultWriter.WriteLine("Column  3: log-spectral RMSE distance in voiced part and without gains");
                spectrumResultWriter.WriteLine("Column  4: log-spectral maximum distance voiced part and without gains");
                spectrumResultWriter.WriteLine("Column  5: log-spectral RMSE distance in ignoring gains");
                spectrumResultWriter.WriteLine("Column  6: log-spectral maximum distance ignoring gains");
                spectrumResultWriter.WriteLine("Column  7: the number of voiced frames");
                spectrumResultWriter.WriteLine("Column  8: RMSE of gains");
                spectrumResultWriter.WriteLine("Column  9: maximum distance of gains");
                spectrumResultWriter.WriteLine("Column 10: correlation coefficient of gains");
                spectrumResultWriter.WriteLine(string.Empty);

                durationResultWriter.WriteLine("Column 1 - the 1st duration file");
                durationResultWriter.WriteLine("Column 2 - the 2nd duration file");
                durationResultWriter.WriteLine("Column 3 - the value of RMSE of durations");
                durationResultWriter.WriteLine("Column 4 - the value of maximum distance of durations");
                durationResultWriter.WriteLine(string.Empty);

                ccResultWriter.WriteLine("Column 1 - the target F0 file");
                ccResultWriter.WriteLine("Column 2 - the average CC of the sentence");
                ccResultWriter.WriteLine("Column 3 - the average boundary CC of the sentence");
                ccResultWriter.WriteLine("Column 4 - the continue unit ratio of the sentence");

                phoneLevelResultWriter.WriteLine("Column 1 - the target F0 file");
                phoneLevelResultWriter.WriteLine("Column 2 - the phone string");
                phoneLevelResultWriter.WriteLine("Column 3 - the phone's start frame index");
                phoneLevelResultWriter.WriteLine("Column 4 - the phone's frame length");
                phoneLevelResultWriter.WriteLine("Column 5 - the F0 distance");
                phoneLevelResultWriter.WriteLine("Column 6 - the F0 corelation");
                phoneLevelResultWriter.WriteLine("Column 7 - the F0 outlier frame number");
                phoneLevelResultWriter.WriteLine("Column 8 - the weighted LSP distance");
                phoneLevelResultWriter.WriteLine("Column 9 - the UV mismatch ratio");
                phoneLevelResultWriter.WriteLine("Column 10 - the Log spectrum distance");
                phoneLevelResultWriter.WriteLine("Column 11 - the Gain distance");

                foreach (FullEvaluationResult result in resultList)
                {
                    durationResultWriter.WriteLine(string.Format("{0}\t{1}\t{2}\t{3}",
                        result.ReferenceFiles.DurationFile,
                        result.TargetFiles.DurationFile,
                        result.Duration.RMSE,
                        result.Duration.MaxDistance));

                    f0ResultWriter.WriteLine(
                        string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6:F3}\t{7:F3}\t{8}\t{9}\t{10}\t{11}\t{12:F3}\t{13}\t{14}",
                        result.ReferenceFiles.F0File,
                        result.UVInfo.VoicedFrameNumberInReference,
                        result.UVInfo.UnvoicedFrameNumberInReference,
                        result.TargetFiles.F0File,
                        result.UVInfo.VoicedFrameNumberInTarget,
                        result.UVInfo.UnvoicedFrameNumberInTarget,
                        result.F0.RMSE,
                        result.F0.MaxDistance,
                        result.UVInfo.VoicedFrameNumberInBoth,
                        result.UVInfo.UnvoicedFrameNumberInBoth,
                        result.UVInfo.UnexpectedVoicedFrameNumber,
                        result.UVInfo.UnexpectedUnvoicedFrameNumber,
                        result.F0.CorrelationCoefficient,
                        result.OutlierF0.OutlierFrameNumber,
                        result.OutlierF0.OutlierRatio));

                    lspResultWriter.WriteLine(string.Format("{0}\t{1}\t{2:F3}\t{3:F3}\t{4}",
                        result.ReferenceFiles.LspFile,
                        result.TargetFiles.LspFile,
                        result.Lsp.RMSE,
                        result.Lsp.MaxDistance,
                        result.Lsp.InusedFrameNumber));

                    spectrumResultWriter.WriteLine(
                        string.Format("{0}\t{1}\t{2:F3}\t{3:F3}\t{4:F3}\t{5:F3}\t{6}\t{7:F3}\t{8:F3}\t{9:F3}",
                        result.ReferenceFiles.LspFile,
                        result.TargetFiles.LspFile,
                        result.LogFrequencyVoiced.RMSE,
                        result.LogFrequencyVoiced.MaxDistance,
                        result.LogFrequency.RMSE,
                        result.LogFrequency.MaxDistance,
                        result.LogFrequency.InusedFrameNumber,
                        result.Gain.RMSE,
                        result.Gain.MaxDistance,
                        result.Gain.CorrelationCoefficient));

                    ccResultWriter.WriteLine(string.Format("{0}\t{1}\t{2}\t{3}",
                        result.ReferenceFiles.F0File,
                        result.CCInfo.TotalConcatenationCost / result.CCInfo.BoundaryNumber,
                        result.VoiceCCInfo.TotalConcatenationCost / result.VoiceCCInfo.BoundaryNumber,
                        (float)result.ContinueUnitInfo.TotalContinueUnitNumber / result.ContinueUnitInfo.BoundaryNumber));

                    foreach (PhoneLevelResult phoneLevelResult in result.PhoneLevelResult)
                    {
                        phoneLevelResultWriter.WriteLine(string.Format("{0}\t{1}\t{2}\t{3}\t{4:F3}\t{5:F3}\t{6}\t{7:F3}\t{8:F3}\t{9:F3}\t{10:F3}",
                            result.ReferenceFiles.F0File, phoneLevelResult.PhoneString, phoneLevelResult.StartFrame,
                            phoneLevelResult.FrameLength, phoneLevelResult.F0Distance, phoneLevelResult.F0Correlation,
                            phoneLevelResult.F0OutlierFrameNumber, phoneLevelResult.WeightedLSPDistance, 
                            phoneLevelResult.UVInfo.UVMismatchRatio, phoneLevelResult.LogSpectrumDistance, phoneLevelResult.GainDistance));
                    }
                }

                using (StreamWriter summaryWriter = new StreamWriter(
                Path.Combine(reportFolder, "Final_Result.txt"), false, Encoding.Unicode))
                {
                    summaryWriter.WriteLine(string.Format("Average of {0} sentences:", resultList.Count));
                    summaryWriter.WriteLine(string.Empty);
                    summaryWriter.WriteLine(string.Empty);
                    summaryWriter.WriteLine(string.Format("RMSE of F0:\t{0} (Hz/frame)", summary.RMSE_F0));
                    summaryWriter.WriteLine(string.Format(
                        "CorrCoef of F0:\t{0}", summary.CorrelationCoefficient_F0));
                    summaryWriter.WriteLine(string.Format(
                        "Outlier F0 Ratio:\t{0}", summary.OutlierF0Ratio));
                    summaryWriter.WriteLine(string.Format(
                        "UV mismatch:\t{0}", summary.UVMismatchRatio));
                        summaryWriter.WriteLine(string.Format(
                        "VU mismatch:\t{0}", summary.VUMismatchRatio));
                    summaryWriter.WriteLine(string.Empty);
                    summaryWriter.WriteLine(string.Format(
                        "Weighted RMSE of LSP:\t{0} ", summary.RMSE_Lsp));
                    summaryWriter.WriteLine(string.Empty);
                    summaryWriter.WriteLine(string.Format(
                        "RMSE of log spectrum in voiced frame:\t{0} (dB)", summary.RMSE_LogSpectrumWithoutGainVoiced));
                    summaryWriter.WriteLine(string.Format(
                        "RMSE of log spectrum:\t{0} (dB)", summary.RMSE_LogSpectrumWithoutGain));
                    summaryWriter.WriteLine(string.Empty);
                    summaryWriter.WriteLine(string.Format("RMSE of gain:\t{0} (dB)", summary.RMSE_Gain));
                    summaryWriter.WriteLine(string.Format(
                        "CorrCoef of gain:\t{0}", summary.CorrelationCoefficient_Gain));
                    summaryWriter.WriteLine(string.Empty);
                    summaryWriter.WriteLine(string.Format(
                        "RMSE of duration:\t{0} (second/phone)", summary.RMSE_Duration));
                    summaryWriter.WriteLine(string.Empty);
                    summaryWriter.WriteLine(string.Format(
                        "Average CC:\t{0}", summary.CC));
                    summaryWriter.WriteLine(string.Format(
                        "Average CC in voiced part:\t{0}", summary.VoicedCC));
                    summaryWriter.WriteLine(string.Format(
                        "Continure ratio:\t{0}", summary.ContiueUnitRatio));
                }
            }
        }

        #endregion

        #region Generic methods

        /// <summary>
        /// Calculate the RMSE distance between two lists of elements.
        /// </summary>
        /// <typeparam name="T">Type of elements in the list.</typeparam>
        /// <param name="referenceList">Reference list.</param>
        /// <param name="targetList">Target list.</param>
        /// <returns>RMSE distance.</returns>
        public static double Rmse<T>(List<T> referenceList, List<T> targetList)
        {
            if ((int)Type.GetTypeCode(typeof(T)) < (int)TypeCode.Int16 ||
                (int)Type.GetTypeCode(typeof(T)) > (int)TypeCode.Decimal)
            {
                throw new ArgumentException("The element type is not numerical type.");
            }

            if (referenceList == null)
            {
                throw new ArgumentNullException("referenceList");
            }

            if (targetList == null)
            {
                throw new ArgumentNullException("targetList");
            }

            if (referenceList.Count != targetList.Count)
            {
                throw new ArgumentException(
                    "The reference element number doesn't match the target element number");
            }

            double distance = double.NaN;
            if (referenceList.Count != 0)
            {
                int elementCount = referenceList.Count;
                distance = 0.0;
                for (int i = 0; i < elementCount; ++i)
                {
                    distance += Math.Pow((
                        double.Parse(referenceList[i].ToString()) -
                        double.Parse(targetList[i].ToString())), 2.0);
                }

                distance = Math.Sqrt(distance / elementCount);
            }

            return distance;
        }

        /// <summary>
        /// Original lsp list contains lsp and log gain information.
        /// This method remove log gain and do normalization on it.
        /// </summary>
        /// <param name="originalLspList">Original lsp list (containing gain).</param>
        /// <param name="withNormalization">Whether do normalization on lsp.</param>
        /// <returns>Gain removed lsp list.</returns>
        public static List<List<double>> RemoveGain(
            List<List<double>> originalLspList, bool withNormalization)
        {
            List<List<double>> resultLspList = new List<List<double>>();
            foreach (List<double> originalFrame in originalLspList)
            {
                List<double> newFrame = new List<double>();
                for (int i = 0; i < originalFrame.Count - 1; ++i)
                {
                    if (withNormalization)
                    {
                        newFrame.Add(Math.Cos(originalFrame[i] * 2.0 * Pi));
                    }
                    else
                    {
                        newFrame.Add(originalFrame[i]);
                    }
                }

                resultLspList.Add(newFrame);
            }

            return resultLspList;
        }

        /// <summary>
        /// Calculate the weighted Euclidean distance to calculate LSP distance
        /// Weight is calculated based on the adjacent distance of the notes. 
        /// Weight = 1 / (R(i) - R(i-1)) + 1 / (R(i+1) - R(i)).
        /// </summary>
        /// <param name="referenceList">Reference list.</param>
        /// <param name="targetList">Target list.</param>
        /// <param name="calculatedDimension">Calculated dimension.</param>
        /// <returns>Weighted Euclidean distance.</returns>
        public static double WeightedLspEuclideanDistance(List<double> referenceList, List<double> targetList, int calculatedDimension)
        {
            if (referenceList == null || targetList == null)
            {
                throw new ArgumentNullException("the referenceList or the targetList");
            }

            if (referenceList.Count != targetList.Count)
            {
                throw new ArgumentException(
                    "The reference element number doesn't match the target element number");
            }

            if (referenceList.Count == 0)
            {
                throw new ArgumentException("The list is empty.");
            }

            if (calculatedDimension == 0)
            {
                throw new ArgumentException("The calculated dimension is zero.");
            }

            if (referenceList.Count < calculatedDimension)
            {
                throw new ArgumentException("The calculateDimension is too large.");
            }

            int elementCount = referenceList.Count;
            double distance = 0.0;
            double weight = 0.0;
            double maxLSP = 0.5;

            for (int i = 0; i < calculatedDimension; ++i)
            {
                double reference = referenceList[i];
                double target = targetList[i];
                double referencePrev = 0.0;
                double referenceNext = 0.0; 

                if (i == 0)
                {
                    referencePrev = 0.0;
                    referenceNext = referenceList[i + 1];
                }
                else if (i == elementCount - 1)
                {
                    referencePrev = referenceList[i - 1];
                    referenceNext = maxLSP;
                }
                else
                {
                    referencePrev = referenceList[i - 1];
                    referenceNext = referenceList[i + 1];
                }

                weight = (1 / Math.Abs(reference - referencePrev)) + (1 / Math.Abs(referenceNext - reference));
                distance += weight * Math.Pow(reference - target, 2.0);
            }

            distance = Math.Sqrt(distance / calculatedDimension);

            return distance;
        }

        /// <summary>
        /// Calculate the maximum distance between two lists of elements.
        /// </summary>
        /// <typeparam name="T">Type of elements in the list.</typeparam>
        /// <param name="referenceList">Reference list.</param>
        /// <param name="targetList">Target list.</param>
        /// <returns>Maximum distance.</returns>
        public static double MaxDistance<T>(List<T> referenceList, List<T> targetList)
        {
            if ((int)Type.GetTypeCode(typeof(T)) < (int)TypeCode.Int16 ||
                (int)Type.GetTypeCode(typeof(T)) > (int)TypeCode.Decimal)
            {
                throw new ArgumentException("The element type is not numerical type.");
            }

            if (referenceList == null)
            {
                throw new ArgumentNullException("referenceList");
            }

            if (targetList == null)
            {
                throw new ArgumentNullException("referenceList");
            }

            if (referenceList.Count != targetList.Count)
            {
                throw new ArgumentException(
                    "The reference element number doesn't match the target element number");
            }

            if (referenceList.Count == 0)
            {
                throw new ArgumentException("The list is empty.");
            }

            int elementCount = referenceList.Count;
            double maxDistance = 0.0;
            for (int i = 0; i < elementCount; ++i)
            {
                double distance = Math.Abs(double.Parse(referenceList[i].ToString()) -
                    double.Parse(targetList[i].ToString()));
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                }
            }

            return maxDistance;
        }

        /// <summary>
        /// Calculate the correlation coefficient between two lists of elements.
        /// </summary>
        /// <typeparam name="T">Type of elements in the list.</typeparam>
        /// <param name="referenceList">Reference list.</param>
        /// <param name="targetList">Target list.</param>
        /// <returns>Correlation coefficient.</returns>
        public static double CorrelationCoefficient<T>(
            List<T> referenceList,
            List<T> targetList)
        {
            if ((int)Type.GetTypeCode(typeof(T)) < (int)TypeCode.Int16 ||
                (int)Type.GetTypeCode(typeof(T)) > (int)TypeCode.Decimal)
            {
                throw new ArgumentException("The element type is not numerical type.");
            }

            if (referenceList == null)
            {
                throw new ArgumentNullException("referenceList");
            }

            if (targetList == null)
            {
                throw new ArgumentNullException("referenceList");
            }

            if (referenceList.Count != targetList.Count)
            {
                throw new ArgumentException(
                    "The reference element number doesn't match the target element number");
            }

            if (referenceList.Count != targetList.Count)
            {
                throw new ArgumentException(
                    "The reference frame number doesn't match the target frame number");
            }

            double correlationCoefficient = double.NaN;

            // To calculate correlation coefficient, there must be at least 2 elements in each list
            if (referenceList.Count > 1)
            {
                int elementCount = referenceList.Count;
                double sumX = 0.0;
                double sumY = 0.0;
                double sumX2 = 0.0;
                double sumY2 = 0.0;
                double sumXY = 0.0;
                for (int i = 0; i < elementCount; ++i)
                {
                    sumX += double.Parse(referenceList[i].ToString());
                    sumY += double.Parse(targetList[i].ToString());
                    sumX2 +=
                        double.Parse(referenceList[i].ToString()) * double.Parse(referenceList[i].ToString());
                    sumY2 += double.Parse(targetList[i].ToString()) * double.Parse(targetList[i].ToString());
                    sumXY += double.Parse(referenceList[i].ToString()) * double.Parse(targetList[i].ToString());
                }

                correlationCoefficient = ((elementCount * sumXY) - (sumX * sumY)) /
                    Math.Sqrt(((elementCount * sumX2) - (sumX * sumX)) * ((elementCount * sumY2) - (sumY * sumY)));
            }

            return correlationCoefficient;
        }

        /// <summary>
        /// Calculate the average value of a list of elements.
        /// </summary>
        /// <typeparam name="T">Type of elements in the list.</typeparam>
        /// <param name="elementList">Element list.</param>
        /// <returns>Average.</returns>
        public static double Average<T>(List<T> elementList)
        {
            if ((int)Type.GetTypeCode(typeof(T)) < (int)TypeCode.Int16 ||
                (int)Type.GetTypeCode(typeof(T)) > (int)TypeCode.Decimal)
            {
                throw new ArgumentException("The element type is not numerical type.");
            }

            if (elementList == null)
            {
                throw new ArgumentNullException("elementList");
            }

            double average = double.NaN;
            if (elementList.Count != 0)
            {
                average = 0.0;
                foreach (T element in elementList)
                {
                    average += double.Parse(element.ToString());
                }

                average /= elementList.Count;
            }

            return average;
        }

        /// <summary>
        /// Do unary operation on each of a list of elements.
        /// </summary>
        /// <typeparam name="TOperand">Type of operand.</typeparam>
        /// <typeparam name="TReturn">Type of result.</typeparam>
        /// <param name="operandList">Operand list.</param>
        /// <param name="unaryOperator">A unary operator, e.g. Log, Exp, or a customized function.</param>
        /// <returns>Result list.</returns>
        public static List<TReturn> UnaryVectorCalculation<TOperand, TReturn>(
            List<TOperand> operandList, Func<TOperand, TReturn> unaryOperator)
        {
            if (operandList == null)
            {
                throw new ArgumentNullException("elementList");
            }

            List<TReturn> resultList = new List<TReturn>();
            foreach (TOperand operand in operandList)
            {
                resultList.Add(unaryOperator(operand));
            }

            return resultList;
        }

        /// <summary>
        /// Do binary operation on each of a list of elements.
        /// </summary>
        /// <typeparam name="TOperand1">Type of first operand.</typeparam>
        /// <typeparam name="TOperand2">Type of second operand.</typeparam>
        /// <typeparam name="TReturn">Type of result.</typeparam>
        /// <param name="operandList1">List of first operand.</param>
        /// <param name="operandList2">List of second operand.</param>
        /// <param name="binaryOperator">A binary operator, e.g. Pow, Multiply.</param>
        /// <returns>List of result.</returns>
        public static List<TReturn> BinaryVectorCalculation<TOperand1, TOperand2, TReturn>(
            List<TOperand1> operandList1, List<TOperand2> operandList2,
            Func<TOperand1, TOperand2, TReturn> binaryOperator)
        {
            if (operandList1 == null)
            {
                throw new ArgumentNullException("operandList1");
            }

            if (operandList2 == null)
            {
                throw new ArgumentNullException("operandList2");
            }

            if (operandList1.Count != operandList2.Count)
            {
                throw new ArgumentException(
                    "The reference element number doesn't match the target element number");
            }

            int elementCount = operandList1.Count;
            List<TReturn> resultList = new List<TReturn>();
            for (int i = 0; i < elementCount; ++i)
            {
                resultList.Add(binaryOperator(operandList1[i], operandList2[i]));
            }

            return resultList;
        }

        /// <summary>
        /// Do unary operation on each of a matrix of elements.
        /// </summary>
        /// <typeparam name="TOperand">Type of operand.</typeparam>
        /// <typeparam name="TReturn">Type of result.</typeparam>
        /// <param name="operandMatrix">Operand matrix.</param>
        /// <param name="unaryOperator">A unary operator, e.g. Log, Exp, or a customized function.</param>
        /// <returns>Result matrix.</returns>
        public static List<List<TReturn>> UnaryMatrixCalculation<TOperand, TReturn>(
            List<List<TOperand>> operandMatrix, Func<TOperand, TReturn> unaryOperator)
        {
            if (operandMatrix == null)
            {
                throw new ArgumentNullException("elementList");
            }

            List<List<TReturn>> resultMatrix = new List<List<TReturn>>();
            foreach (List<TOperand> operandList in operandMatrix)
            {
                resultMatrix.Add(UnaryVectorCalculation<TOperand, TReturn>(operandList, unaryOperator));
            }

            return resultMatrix;
        }

        /// <summary>
        /// Select the element from element list whose index is in index list.
        /// </summary>
        /// <typeparam name="T">Type of element.</typeparam>
        /// <param name="elementList">The element list.</param>
        /// <param name="indexList">The index list (can also called mask).</param>
        /// <returns>The filtered element list.</returns>
        public static List<T> FilterElementByIndex<T>(List<T> elementList, List<int> indexList)
        {
            List<T> resultList = new List<T>();

            foreach (int index in indexList)
            {
                if (index < 0 || index >= elementList.Count)
                {
                    throw new ArgumentOutOfRangeException();
                }
                else
                {
                    resultList.Add(elementList[index]);
                }
            }

            return resultList;
        }

        #endregion

        #region Data loader

        /// <summary>
        /// Load the duration for each state of each phone for a sentence from a file.
        /// </summary>
        /// <param name="durationFile">The file containing duration info for a sentence.</param>
        /// <returns>A list of duration, by number of frames.</returns>
        public static List<List<int>> LoadDuration(string durationFile)
        {
            string durationSeparator = " \t";
            Helper.CheckFileExists(durationFile);
            List<List<int>> durationList = new List<List<int>>();
            using (StreamReader durationReader = new StreamReader(durationFile, Encoding.Unicode))
            {
                while (!durationReader.EndOfStream)
                {
                    List<int> phoneDuration = new List<int>();
                    for (int i = 0; i < StateCount; ++i)
                    {
                        string durationLine = durationReader.ReadLine();
                        string[] durationFields = durationLine.Split(
                            durationSeparator.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                        int frameCount = int.Parse(durationFields[2]);
                        phoneDuration.Add(frameCount);
                    }

                    for (int i = 0; i < SpsModeling.DefaultStateCount - StateCount; ++i)
                    {
                        durationReader.ReadLine();
                    }

                    durationList.Add(phoneDuration);
                }
            }

            return durationList;
        }

        /// <summary>
        /// Load the f0 for each frame for a sentence from a file.
        /// </summary>
        /// <param name="f0File">The file containing f0 info for a sentence.</param>
        /// <returns>A list of f0, by Hz.</returns>
        public static List<double> LoadF0(string f0File)
        {
            Helper.CheckFileExists(f0File);
            List<double> f0List = new List<double>();
            FileInfo f0FileInfo = new FileInfo(f0File);

            // F0 for each frame is stored in a float
            int frameCount = (int)f0FileInfo.Length / sizeof(float);
            FileStream file = File.OpenRead(f0File);
            try
            {
                using (BinaryReader f0Reader = new BinaryReader(file))
                {
                    file = null;
                    for (int i = 0; i < frameCount; ++i)
                    {
                        float f0 = f0Reader.ReadSingle();
                        f0List.Add(f0);
                    }
                }

                return f0List;
            }
            finally
            {
                if (null != file)
                {
                    file.Dispose();
                }
            }
        }

        /// <summary>
        /// Load the lsp on difference dimensions for each frame for a sentence from a file.
        /// </summary>
        /// <param name="lspFile">The file containing lsp info for a sentence.</param>
        /// <param name="lpcOrder">The LPC order, 40 is a usual value.</param>
        /// <returns>A list of lsp, on each dimension of each frame.</returns>
        public static List<List<double>> LoadLsp(string lspFile, int lpcOrder)
        {
            Helper.CheckFileExists(lspFile);
            List<List<double>> lspList = new List<List<double>>();
            FileInfo lspFileInfo = new FileInfo(lspFile);

            // Lsp for each frame is stored in (lpcOrder + 1) dimensions,
            // and each dimension is stored in a float
            int frameCount = (int)lspFileInfo.Length / (lpcOrder + 1) / sizeof(float);
            FileStream file = File.OpenRead(lspFile);
            try
            {
                using (BinaryReader lspReader = new BinaryReader(file))
                {
                    file = null;
                    for (int i = 0; i < frameCount; ++i)
                    {
                        List<double> frameLsp = new List<double>();
                        for (int j = 0; j < lpcOrder + 1; ++j)
                        {
                            float lsp = lspReader.ReadSingle();
                            frameLsp.Add(lsp);
                        }

                        lspList.Add(frameLsp);
                    }
                }

                return lspList;
            }
            finally
            {
                if (null != file)
                {
                    file.Dispose();
                }
            }
        }

        #endregion

        /// <summary>
        /// Get acoustic data file list from given data folder.
        /// </summary>
        /// <param name="dataFolder">Data folder.</param>
        /// <returns>Data file list.</returns>
        public static List<RmseEvaluation.DataFiles> GetDataFileList(string dataFolder)
        {
            List<RmseEvaluation.DataFiles> dataFileList = new List<RmseEvaluation.DataFiles>();
            string[] durationFiles = Directory.GetFiles(
                Path.Combine(dataFolder, "duration"), "*.dur", SearchOption.AllDirectories);
            string[] f0Files = Directory.GetFiles(
                Path.Combine(dataFolder, "f0"), "*f0", SearchOption.AllDirectories);
            string[] lspFiles = Directory.GetFiles(
                Path.Combine(dataFolder, "lsp"), "*.lsp", SearchOption.AllDirectories);

            // Check whether the file numbers in each folder are consistent
            if (durationFiles.Length != f0Files.Length || durationFiles.Length != lspFiles.Length || f0Files.Length != lspFiles.Length)
            {
                throw new ApplicationException("the Data files are corrupt!");
            }

            for (int i = 0; i < durationFiles.Length; ++i)
            {
                RmseEvaluation.DataFiles dataFiles = new RmseEvaluation.DataFiles();
                dataFiles.DurationFile = durationFiles[i];
                dataFiles.F0File = f0Files[i];
                dataFiles.LspFile = lspFiles[i];
                dataFileList.Add(dataFiles);
            }

            return dataFileList;
        }

        /// <summary>
        /// Calculate LPC order by F0 file and LSP file.
        /// </summary>
        /// <param name="f0File">F0 file.</param>
        /// <param name="lspFile">LSP file.</param>
        /// <returns>LPC order.</returns>
        public static int GetLpcOrder(string f0File, string lspFile)
        {
            FileInfo f0FileInfo = new FileInfo(f0File);
            FileInfo lspFileInfo = new FileInfo(lspFile);
            return (int)((lspFileInfo.Length / f0FileInfo.Length) - 1);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Calculate maximum distance on phoneme level.
        /// </summary>
        /// <param name="phoneLevelResult">The original phoneme level result.</param>
        /// <param name="featureSelector">The acoustic feature selector.</param>
        /// <returns>The maximum distance, will be NaN if all the phonemes are silence.</returns>
        private static double GetPhoneLevelMaximumDistance(
            List<PhoneLevelResult> phoneLevelResult, Func<PhoneLevelResult, double> featureSelector)
        {
            double result = double.NaN;
            if (phoneLevelResult.Count > 0)
            {
                IEnumerable<PhoneLevelResult> nonSilenceResult =
                    phoneLevelResult.Where(x => x.WordLength > 0);
                if (nonSilenceResult.Count() > 0)
                {
                    result = nonSilenceResult.Select<PhoneLevelResult, double>(featureSelector).Max();
                }
            }

            return result;
        }

        /// <summary>
        /// Get the index list for voiced frames on both reference and target.
        /// </summary>
        /// <param name="referenceF0List">Reference F0 list.</param>
        /// <param name="targetF0List">Target F0 list.</param>
        /// <param name="voicedUnvoicedThreshold">
        /// The threshold to judge whether a frame is voiced or unvoiced, by Hz.
        /// </param>
        /// <param name="voicedOfReference">Number of voiced frames in reference list.</param>
        /// <param name="voicedOfTarget">Number of voiced frames in target list.</param>
        /// <returns>A list of integers representing the index of both voiced frames.</returns>
        private static List<int> FilterUnvoicedF0(
            List<double> referenceF0List,
            List<double> targetF0List,
            double voicedUnvoicedThreshold,
            out int voicedOfReference,
            out int voicedOfTarget)
        {
            if (referenceF0List.Count != targetF0List.Count)
            {
                throw new ArgumentException(
                    "The reference frame number doesn't match the target frame number");
            }

            int frameCount = referenceF0List.Count;
            List<int> bothVoicedList = new List<int>();
            voicedOfReference = 0;
            voicedOfTarget = 0;
            for (int i = 0; i < frameCount; ++i)
            {
                if (referenceF0List[i] > voicedUnvoicedThreshold)
                {
                    ++voicedOfReference;
                }

                if (targetF0List[i] > voicedUnvoicedThreshold)
                {
                    ++voicedOfTarget;
                }

                if (referenceF0List[i] > voicedUnvoicedThreshold &&
                    targetF0List[i] > voicedUnvoicedThreshold)
                {
                    bothVoicedList.Add(i);
                }
            }

            return bothVoicedList;
        }

        /// <summary>
        /// Get the average f0 on each state according to the given duration list.
        /// </summary>
        /// <param name="f0List">F0 list.</param>
        /// <param name="durationList">Duration list.</param>
        /// <param name="voicedUnvoicedThreshold">
        /// The threshold to judge whether a frame is voiced or unvoiced, by Hz.
        /// </param>
        /// <returns>The average f0 list.</returns>
        private static List<double> GetStateLevelF0(
            List<double> f0List,
            List<List<int>> durationList,
            double voicedUnvoicedThreshold)
        {
            int totalFrameCount = 0;
            foreach (List<int> phoneDuration in durationList)
            {
                foreach (int stateDuration in phoneDuration)
                {
                    totalFrameCount += stateDuration;
                }
            }

            if (totalFrameCount != f0List.Count)
            {
                throw new ArgumentException(
                    "The frame number in f0 list doesn't match that in duration list.");
            }

            List<double> stateLevelF0List = new List<double>();
            int frameIndex = 0;
            foreach (List<int> phoneDuration in durationList)
            {
                foreach (int stateDuration in phoneDuration)
                {
                    double stateF0 = 0.0;
                    bool hasUnvoiced = false;
                    for (int i = 0; i < stateDuration; ++i)
                    {
                        if (f0List[frameIndex] < voicedUnvoicedThreshold)
                        {
                            stateF0 = 0.0;
                            hasUnvoiced = true;
                        }
                        else
                        {
                            if (!hasUnvoiced)
                            {
                                stateF0 += f0List[frameIndex];
                            }
                        }

                        ++frameIndex;
                    }

                    stateF0 /= stateDuration;
                    stateLevelF0List.Add(stateF0);
                }
            }

            return stateLevelF0List;
        }

        /// <summary>
        /// Original lsp list contains lsp and log gain information.
        /// This method extracts gain from original lsp list.
        /// </summary>
        /// <param name="originalLspList">Original lsp list (containing gain).</param>
        /// <returns>Gain list.</returns>
        private static List<double> GetGain(List<List<double>> originalLspList)
        {
            List<double> gainList = new List<double>();
            foreach (List<double> originalFrame in originalLspList)
            {
                gainList.Add(Math.Exp(originalFrame[originalFrame.Count - 1]));
            }

            return gainList;
        }

        /// <summary>
        /// Transform a lsp list to a frequency (spectrum without gain) list.
        /// </summary>
        /// <param name="lspList">Lsp list.</param>
        /// <param name="lpcOrder">Dimension of HTS voice font, e.g. 16 or 40.</param>
        /// <returns>Frequency (spectrum without gain) list.</returns>
        private static List<List<double>> LspListToFrequencyList(
            List<List<double>> lspList, int lpcOrder)
        {
            List<List<double>> normalizedLspList = RemoveGain(lspList, true);

            List<List<double>> lpcList = new List<List<double>>();
            foreach (List<double> lspFrame in normalizedLspList)
            {
                lpcList.Add(LspToLpcOneFrame(lspFrame, lpcOrder));
            }

            List<List<double>> frequencyList = new List<List<double>>();
            foreach (List<double> lpcFrame in lpcList)
            {
                frequencyList.Add(LpcToSpectrumOneFrame(lpcFrame, lpcOrder, 1.0));
            }

            return frequencyList;
        }

        /// <summary>
        /// Transform frequency list to spectrum list by adding gain.
        /// </summary>
        /// <param name="frequencyList">Frequency (spectrum without gain) list.</param>
        /// <param name="gainList">Gain (not logarithmic) list.</param>
        /// <returns>Spectrum list.</returns>
        private static List<List<double>> FrequencyListToSpectrumList(
            List<List<double>> frequencyList,
            List<double> gainList)
        {
            if (frequencyList.Count != gainList.Count)
            {
                throw new ArgumentException("The frequency frame number doesn't match the gain frame number");
            }

            List<List<double>> spectrumList = new List<List<double>>();
            int frameCount = frequencyList.Count;
            for (int i = 0; i < frameCount; ++i)
            {
                List<double> spectrumFrame = new List<double>();
                foreach (double frequency in frequencyList[i])
                {
                    spectrumFrame.Add(frequency * gainList[i]);
                }

                spectrumList.Add(spectrumFrame);
            }

            return spectrumList;
        }

        /// <summary>
        /// Transform a lsp frame to a lpc frame
        /// Note: 1: Using the Chebyshev polynomial evaluation
        ///       2: This is a positive system: H(z) = 1 + A(z).
        /// </summary>
        /// <param name="lspFrame">A lsp frame.</param>
        /// <param name="lpcOrder">Dimension of HTS voice font, e.g. 16 or 40.</param>
        /// <returns>A LPC frame.</returns>
        private static List<double> LspToLpcOneFrame(List<double> lspFrame, int lpcOrder)
        {
            double dXin1, dXin2, dXout1, dXout2;
            int dN1, dN2, dN3, dN4 = 0;
            List<double> window = new List<double>();
            for (int i = 0; i < (lpcOrder * 2) + 2; ++i)
            {
                window.Add(0.0);
            }

            dXin1 = 1.0;
            dXin2 = 1.0;

            ////#region reconstruct P(z) and Q(z) by 1 - 2dXz(-1) + z(-2) where X is the LSP

            for (int j = 0; j < lpcOrder / 2; ++j)
            {
                dN1 = j * 4;
                dN2 = dN1 + 1;
                dN3 = dN2 + 1;
                dN4 = dN3 + 1;
                dXout1 = dXin1 - (2 * (lspFrame[2 * j] * window[dN1])) + window[dN2];
                dXout2 = dXin2 - (2 * (lspFrame[(2 * j) + 1] * window[dN3])) + window[dN4];
                window[dN2] = window[dN1];
                window[dN4] = window[dN3];
                window[dN1] = dXin1;
                window[dN3] = dXin2;
                dXin1 = dXout1;
                dXin2 = dXout2;
            }

            window[dN4 + 1] = dXin1;
            window[dN4 + 2] = dXin2;

            dXin1 = 0.0;
            dXin2 = 0.0;

            ////#endregion

            List<double> resultList = new List<double>(lpcOrder);
            for (int i = 0; i < lpcOrder; ++i)
            {
                for (int j = 0; j < lpcOrder / 2; ++j)
                {
                    dN1 = j * 4;
                    dN2 = dN1 + 1;
                    dN3 = dN2 + 1;
                    dN4 = dN3 + 1;
                    dXout1 = dXin1 - (2 * lspFrame[2 * j] * window[dN1]) + window[dN2];
                    dXout2 = dXin2 - (2 * lspFrame[(2 * j) + 1] * window[dN3]) + window[dN4];
                    window[dN2] = window[dN1];
                    window[dN4] = window[dN3];
                    window[dN1] = dXin1;
                    window[dN3] = dXin2;
                    dXin1 = dXout1;
                    dXin2 = dXout2;
                }

                dXout1 = dXin1 + window[dN4 + 1];
                dXout2 = dXin2 - window[dN4 + 2];
                resultList.Add((dXout1 + dXout2) * 0.5);
                window[dN4 + 1] = dXin1;
                window[dN4 + 2] = dXin2;

                dXin1 = 0.0;
                dXin2 = 0.0;
            }

            return resultList;
        }

        /// <summary>
        /// Transform a lpc frame to a spectrum frame, using real FFT.
        /// </summary>
        /// <param name="lpcFrame">A LPC frame.</param>
        /// <param name="lpcOrder">Dimension of HTS voice font, e.g. 16 or 40.</param>
        /// <param name="gain">
        /// If this value is set to 1.0, the result will not contain gain information.
        /// </param>
        /// <returns>A spectrum frame.</returns>
        private static List<double> LpcToSpectrumOneFrame(List<double> lpcFrame, int lpcOrder, double gain)
        {
            Complex[] lpcComplexes = new Complex[FFTDim];
            lpcComplexes[0] = new Complex(1.0, 0.0);
            for (int i = 1; i < FFTDim; ++i)
            {
                if (i < lpcOrder + 1)
                {
                    lpcComplexes[i] = new Complex(lpcFrame[i - 1], 0.0);
                }
                else
                {
                    lpcComplexes[i] = new Complex();
                }
            }

            Fft.Transfer(false, ref lpcComplexes);

            List<double> resultList = new List<double>();
            for (int i = 0; i < FFTDim / 2; ++i)
            {
                resultList.Add(gain / lpcComplexes[i].Mode());
            }

            return resultList;
        }

        /// <summary>
        /// Filter spectrum by specified frequency band, on each frame of the given list.
        /// </summary>
        /// <param name="frameList">Input frame list.</param>
        /// <param name="lowerBoundFrequency">Lower bound of the frequency band.</param>
        /// <param name="upperBoundFrequency">Upper bound of the frequency band.</param>
        /// <param name="sampleFrequency">Sample frequency of the voice.</param>
        /// <returns>Spectrum filtered frame list.</returns>
        private static List<List<double>> FrequencyBandFilter(
            List<List<double>> frameList,
            double lowerBoundFrequency,
            double upperBoundFrequency,
            double sampleFrequency)
        {
            if (frameList == null)
            {
                throw new ArgumentNullException("frameList");
            }

            if (lowerBoundFrequency >= upperBoundFrequency)
            {
                throw new ArgumentException(
                    "LowerBoundFrequency should be less than UpperBoundFrequency.");
            }

            if (upperBoundFrequency * 2 > sampleFrequency)
            {
                throw new ArgumentException(
                    "double UpperBoundFrequency should be less than SampleFrequency");
            }

            List<List<double>> resultFrameList = new List<List<double>>();

            foreach (List<double> frame in frameList)
            {
                List<double> filteredFrame = new List<double>();
                int fftDimension = frame.Count * 2;
                int startFftDimension = (int)(lowerBoundFrequency / sampleFrequency * fftDimension);
                int endFftDimension = (int)(upperBoundFrequency / sampleFrequency * fftDimension) - 1;
                for (int i = startFftDimension; i <= endFftDimension; ++i)
                {
                    filteredFrame.Add(frame[i]);
                }

                resultFrameList.Add(filteredFrame);
            }

            return resultFrameList;
        }

        /// <summary>
        /// Get the phone level info of start frame, frame length, phone string. 
        /// </summary>
        /// <param name="bestPath">BestPath.</param>
        /// <returns>PhoneLevelResults.</returns>
        private static List<PhoneLevelResult> GeneratePhoneLevelInfo(UnitLatticeSentence bestPath)
        {
            List<PhoneLevelResult> result = new List<PhoneLevelResult>();
            int endFrameIndex = 0;  // record the end frame index of prevous unit in the generated wave
            // start from index 1, because first node don't have boundary with left phone
            for (int i = 0; i < bestPath.Phones.Count; i++)
            {
                // Get curent phone 
                UnitLatticePhone unitLatticePhone = bestPath.Phones[i];

                if (unitLatticePhone.Candidates.Count == 1)
                {
                    UnitLatticePhoneCandidate unitLatticePhoneCandidate = unitLatticePhone.Candidates[0];

                    // only caculate no silence phone
                    if (!unitLatticePhone.Text.Equals("sil"))
                    {
                        result.Add(new PhoneLevelResult()
                        {
                            PhoneString = unitLatticePhoneCandidate.Text,
                            StartFrame = endFrameIndex,
                            FrameLength = unitLatticePhoneCandidate.FrameLength
                        });
                    }

                    endFrameIndex += unitLatticePhoneCandidate.FrameLength;
                }
                else
                {
                    throw new ApplicationException("The candidate on the best path larger than 1");
                }
            }

            return result;
        }

        #endregion

        #region Structures

        /// <summary>
        /// This structure is used to store the mapping information between word and phonemes,
        /// For a single sentence.
        /// </summary>
        public struct PhoneWordMap
        {
            /// <summary>
            /// The offset in original text of the word that the phonemes belongs to.
            /// </summary>
            public int WordOffset;

            /// <summary>
            /// The length in original text of the word that the phonemes belongs to.
            /// </summary>
            public int WordLength;

            /// <summary>
            /// The phoneme count in current word.
            /// </summary>
            public int PhonemeCount;
        }

        /// <summary>
        /// This structure is used to store the full evaluation result for a sentence,
        /// Including input file path, RMSE distance, correlation coefficient, voice/unvoiced frame number, etc,
        /// On different acoustic features.
        /// </summary>
        public struct FullEvaluationResult
        {
            /// <summary>
            /// Files for reference acoustic data.
            /// </summary>
            public DataFiles ReferenceFiles;

            /// <summary>
            /// Files for target acoustic data.
            /// </summary>
            public DataFiles TargetFiles;

            /// <summary>
            /// Voiced/unvoiced frame number statistic information for F0.
            /// </summary>
            public UVStatistic UVInfo;

            /// <summary>
            /// Voiced/unvoiced frame number statistic information for state level F0.
            /// </summary>
            public UVStatistic UVInfo_StateLevel;

            /// <summary>
            /// Evaluation result for duration.
            /// </summary>
            public EvaluationResult Duration;

            /// <summary>
            /// Evaluation result for F0.
            /// </summary>
            public EvaluationResult F0;

            /// <summary>
            /// Evalation result for state level F0.
            /// </summary>
            public EvaluationResult F0_StateLevel;

            /// <summary>
            /// Evaluation result for LSP.
            /// </summary>
            public EvaluationResult Lsp;

            /// <summary>
            /// Evaluation result for logarithmic spectrum with gain.
            /// </summary>
            public EvaluationResult LogSpectrum;

            /// <summary>
            /// Evaluation result for logarithmic spectrum without gain.
            /// </summary>
            public EvaluationResult LogFrequency;

            /// <summary>
            /// Evaluation result for logarithmic gain.
            /// </summary>
            public EvaluationResult Gain;

            /// <summary>
            /// Phone level evaluation result.
            /// </summary>
            public List<PhoneLevelResult> PhoneLevelResult;

            /// <summary>
            /// Evaluation result of Outlier F0 frame info, only used in RUS.
            /// </summary>
            public OutlierF0Statistic OutlierF0; 

            /// <summary>
            /// Evaluation result for logarithmic specutrum of low frequency part and voiced part. 
            /// </summary> 
            public EvaluationResult LogFrequencyVoiced;

            /// <summary>
            /// Evaluation result of concatenation cost.
            /// </summary>
            public ConcatenationCostInfo CCInfo;

            /// <summary>
            /// Evaluation result of voiced boundy for concatenation result.
            /// </summary>
            public ConcatenationCostInfo VoiceCCInfo;

            /// <summary>
            /// Evaluation result of continues units in recording info.
            /// </summary>
            public ContinueUnitInfo ContinueUnitInfo;
        }

        /// <summary>
        /// This structure is used to store the evaluation result on a single acoustic feature.
        /// </summary>
        public struct EvaluationResult
        {
            /// <summary>
            /// Inused frame number to calculation the result.
            /// </summary>
            public int InusedFrameNumber;

            /// <summary>
            /// Root-Mean-Square-Error distance.
            /// </summary>
            public double RMSE;

            /// <summary>
            /// Maximum distance.
            /// </summary>
            public double MaxDistance;

            /// <summary>
            /// Correlation coefficient.
            /// </summary>
            public double CorrelationCoefficient;
        }

        /// <summary>
        /// This structure is used to store the concatenation cost info.
        /// </summary>
        public struct ConcatenationCostInfo
        {
            /// <summary>
            /// Boundary number.
            /// </summary>
            public int BoundaryNumber;

            /// <summary>
            /// Sum of the concatenation cost of the sentences.
            /// </summary>
            public double TotalConcatenationCost;
        }

        /// <summary>
        /// This structure is used to store the continue unit info.
        /// </summary>
        public struct ContinueUnitInfo
        {
            /// <summary>
            /// Boundary number.
            /// </summary>
            public int BoundaryNumber;

            /// <summary>
            /// Sum of the total continue units in the sentences. 
            /// </summary>
            public int TotalContinueUnitNumber;
        }

        /// <summary>
        /// This structure is used to store the paths of acoustic data files.
        /// </summary>
        public struct DataFiles
        {
            /// <summary>
            /// Path of duration file, text format.
            /// </summary>
            public string DurationFile;

            /// <summary>
            /// Path of F0 file, binary format, SPS use F0 while RUS use F0 to calculate.
            /// </summary>
            public string F0File;

            /// <summary>
            /// Path of lsp file, binary format.
            /// </summary>
            public string LspFile;
        }

        /// <summary>
        /// This structure is used to store the voiced/unvoiced frames statistic.
        /// </summary>
        public struct UVStatistic
        {
            /// <summary>
            /// Voiced frame number in reference F0 list.
            /// </summary>
            public int VoicedFrameNumberInReference;

            /// <summary>
            /// Unvoiced frame number in reference F0 list.
            /// </summary>
            public int UnvoicedFrameNumberInReference;

            /// <summary>
            /// Voiced frame number in target F0 list.
            /// </summary>
            public int VoicedFrameNumberInTarget;

            /// <summary>
            /// Unvoiced frame number in target F0 list.
            /// </summary>
            public int UnvoicedFrameNumberInTarget;

            /// <summary>
            /// Number of frames that are voiced in both reference F0 list and target F0 list.
            /// </summary>
            public int VoicedFrameNumberInBoth;

            /// <summary>
            /// Number of frames that are unvoiced in both reference F0 list and target F0 list.
            /// </summary>
            public int UnvoicedFrameNumberInBoth;

            /// <summary>
            /// Number of frames that are unvoiced in reference F0 list but voiced in target F0 list.
            /// </summary>
            public int UnexpectedVoicedFrameNumber;

            /// <summary>
            /// Number of frames that are voiced in reference F0 list but unvoiced in target F0 list.
            /// </summary>
            public int UnexpectedUnvoicedFrameNumber;

            /// <summary>
            /// Gets Total mismatch ratio.
            /// </summary>
            public double UVMismatchRatio
            {
                get
                {
                    int totalFrame = VoicedFrameNumberInReference + UnvoicedFrameNumberInReference;
                    if (totalFrame == 0)
                    {
                        return 0;
                    }
                    else
                    {
                        int totalMisMatch = UnexpectedUnvoicedFrameNumber + UnexpectedVoicedFrameNumber;
                        return (double)totalMisMatch / totalFrame;
                    }
                }
            }
        }

        /// <summary>
        /// This structure is used to store the outlier f0 frame which the distance is bigger than 10 hz. 
        /// </summary>
        public struct OutlierF0Statistic
        {
            /// <summary>
            /// Outlier frame number .
            /// </summary>
            public int OutlierFrameNumber;

            /// <summary>
            /// Total frame number .
            /// </summary>
            public int TotalFrameNumber;

            /// <summary>
            /// Outlier frame ratio .
            /// </summary>
            public double OutlierRatio;
        }

        /// <summary>
        /// This structure is used to store the final evaluation summary,
        /// Which is actually the average of a group of FullEvaluationResult.
        /// </summary>
        public struct EvaluationSummary
        {
            /// <summary>
            /// Average RMSE of duration.
            /// </summary>
            public double RMSE_Duration;

            /// <summary>
            /// Average RMSE of F0.
            /// </summary>
            public double RMSE_F0;

            /// <summary>
            /// Average correlation coefficient of F0.
            /// </summary>
            public double CorrelationCoefficient_F0;

            /// <summary>
            /// Average RMSE of state level F0.
            /// </summary>
            public double RMSE_F0_StateLevel;

            /// <summary>
            /// Average correlation coefficient of state level F0.
            /// </summary>
            public double CorrelationCoefficient_F0_StateLevel;

            /// <summary>
            /// Average RMSE of LSP.
            /// </summary>
            public double RMSE_Lsp;

            /// <summary>
            /// Average RMSE of logarithmic spectrum with gain.
            /// </summary>
            public double RMSE_LogSpectrumWithGain;

            /// <summary>
            /// Average RMSE of logarithmic spectrum without gain in Voiced part.
            /// </summary>
            public double RMSE_LogSpectrumWithoutGainVoiced;

            /// <summary>
            /// Average RMSE of logarithmic spectrum without gain.
            /// </summary>
            public double RMSE_LogSpectrumWithoutGain;

            /// <summary>
            /// Average RMSE of gain.
            /// </summary>
            public double RMSE_Gain;

            /// <summary>
            /// Average correlation coefficient of gain.
            /// </summary>
            public double CorrelationCoefficient_Gain;

            /// <summary>
            /// Outlier F0 Frame ratio .
            /// </summary>
            public double OutlierF0Ratio;

            /// <summary>
            /// UV mismatch ratio.
            /// </summary>
            public double UVMismatchRatio; 

            /// <summary>
            /// VU mismatch ratio.
            /// </summary>
            public double VUMismatchRatio;

            /// <summary>
            /// Average CC.
            /// </summary>
            public double CC;

            /// <summary>
            /// Average voiced boundary CC.
            /// </summary>
            public double VoicedCC;

            /// <summary>
            /// Continues ratio.
            /// </summary>
            public double ContiueUnitRatio;
        }

        #endregion

        #region SubClasses

        /// <summary>
        /// This class is used to store the evalution result on a single phoneme.
        /// </summary>
        public class PhoneLevelResult
        {
            /// <summary>
            /// The phone string of the test phone.
            /// </summary>
            public string PhoneString;

            /// <summary>
            /// The offset in original text of the word that current phoneme belongs to.
            /// </summary>
            public int WordOffset;

            /// <summary>
            /// The length in original text of the word that current phoneme belongs to.
            /// </summary>
            public int WordLength;

            /// <summary>
            /// The phoneme's start frame .
            /// </summary>
            public int StartFrame;

            /// <summary>
            /// The phoneme's frame length.
            /// </summary>
            public int FrameLength;

            /// <summary>
            /// The phoneme level RMSE distance of duration.
            /// </summary>
            public double DurationDistance;

            /// <summary>
            /// The phoneme level RMSE distance of F0.
            /// </summary>
            public double F0Distance;

            /// <summary>
            /// The phoneme level Corelation of F0.
            /// </summary>
            public double F0Correlation;

            /// <summary>
            /// The phoneme level outlier frame number.
            /// </summary>
            public int F0OutlierFrameNumber;

            /// <summary>
            /// The phoneme level LSP distance.
            /// </summary>
            public double LspDistance;

            /// <summary>
            /// The phoneme level weighted LSP distance.
            /// </summary>
            public double WeightedLSPDistance;

            /// <summary>
            /// Phone levle UV mismatch ratio.
            /// </summary>
            public UVStatistic UVInfo;

            /// <summary>
            /// The phoneme level RMSE distance of logarithmic spectrum.
            /// </summary>
            public double LogSpectrumDistance;

            /// <summary>
            /// The phoneme level RMSE distance of gain.
            /// </summary>
            public double GainDistance;

            /// <summary>
            /// Both voiced frame list in both reference and target. 
            /// </summary>
            public List<int> BothVoicedList;
        }

        #endregion
    }
}