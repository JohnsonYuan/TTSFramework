//----------------------------------------------------------------------------
// <copyright file="AcousticFeatureExtractor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This is a helper class of export SPS&RUS acoustic data 
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.ServiceProvider.Extension
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Utility;
    using Microsoft.Tts.Offline.Waveform;
    using SP = Microsoft.Tts.ServiceProvider;

    /// <summary>
    /// This class is the helper class to export SPS and RUS acoustic data.
    /// </summary>
    public static class AcousticFeatureExtractor
    {
        public static int RusDurationState = 2;

        private const float MinimumLogF0 = -1.0e10f;   

        /// <summary>
        /// Export SPS Duration file from utterance.
        /// </summary>
        /// <param name="utterance">Utterance.</param>
        /// <param name="durFile">Output duration file.</param>
        public static void ExportSPSDurations(SP.TtsUtterance utterance, DurationFile durFile)
        {
            ExportSPSDurations(utterance, durFile, false);
        }

        /// <summary>
        /// Export SPS Duration file from utterance.
        /// </summary>
        /// <param name="utterance">Utterance.</param>
        /// <param name="durFile">Output duration file.</param>
        /// <param name="withSilence">Whether export duration for silence phoneme.</param>
        public static void ExportSPSDurations(SP.TtsUtterance utterance, DurationFile durFile, bool withSilence)
        {
            for (int unitIndex = 0; unitIndex < utterance.Acoustic.Units.Count; unitIndex++)
            {
                if (withSilence || !utterance.Acoustic.Units[unitIndex].IsSilence)
                {
                    SP.MemoryArray<uint> phoneDurations = utterance.Acoustic.Durations[unitIndex];

                    uint startFrameIndex = 0;
                    uint frameCount = 0;
                    utterance.Acoustic.GetFrames(unitIndex, ref startFrameIndex, ref frameCount);

                    // Check frame count consistency
                    uint n = 0;
                    for (int phoneDurationIndex = 0; phoneDurationIndex < phoneDurations.Length; ++phoneDurationIndex)
                    {
                        n += phoneDurations[phoneDurationIndex];
                    }

                    Debug.Assert(n == frameCount);

                    // Dump state duration
                    PhoneStateDuration stateDuration = new PhoneStateDuration();
                    stateDuration.PhoneLabel = utterance.Acoustic.Units[unitIndex].Name;
                    stateDuration.FramesInState = new int[Microsoft.Tts.Offline.Htk.SpsModeling.DefaultStateCount];

                    for (int phoneDurationIndex = 0; phoneDurationIndex < phoneDurations.Length; phoneDurationIndex++)
                    {
                        stateDuration.FramesInState[phoneDurationIndex] = (int)phoneDurations[phoneDurationIndex];
                    }

                    durFile.Durations.Add(stateDuration);
                }
            }
        }

        /// <summary>
        /// Export SPS Duration feature from utterance for the specific model.
        /// </summary>
        /// <param name="utterance">Utterance.</param>
        /// <param name="modelStartIndex">The starting position of the model from which you would like to export the related acoutic feature.</param>
        /// <param name="length">The number of model in the exporting.</param>
        /// <param name="durFile">Output duration file.</param>
        public static void ExportSPSDurations(SP.TtsUtterance utterance, int modelStartIndex, int length, DurationFile durFile)
        {
            for (int modelIndex = modelStartIndex; modelIndex < modelStartIndex + length; modelIndex++)
            {
                if (utterance.Acoustic.Units[modelIndex].IsSilence)
                {
                    continue;
                }

                SP.MemoryArray<uint> phoneDuration = utterance.Acoustic.Durations[modelIndex];
                uint startFrameIndex = 0;
                uint frameCount = 0;
                utterance.Acoustic.GetFrames(modelIndex, ref startFrameIndex, ref frameCount);

                uint fCount = 0;
                for (int i = 0; i < phoneDuration.Length; i++)
                {
                    fCount += phoneDuration[i];
                }

                Debug.Assert(frameCount == fCount);

                PhoneStateDuration stateDuration = new PhoneStateDuration();
                stateDuration.PhoneLabel = utterance.Acoustic.Units[modelIndex].Name;
                stateDuration.FramesInState = new int[Microsoft.Tts.Offline.Htk.SpsModeling.DefaultStateCount];
                for (int j = 0; j < phoneDuration.Length; j++)
                {
                    stateDuration.FramesInState[j] = (int)phoneDuration[j];
                }

                durFile.Durations.Add(stateDuration);
            }
        }

        /// <summary>
        /// Transfer the 5 state duration file to 2 state. 
        /// </summary>
        /// <param name="durFile">Output duration file of two state.</param>
        /// <param name="originalDuration">Original duration file of 5 state.</param>
        public static void TransferSPSDurationFile(DurationFile durFile, DurationFile originalDuration)
        {
            foreach (PhoneStateDuration duration in originalDuration.Durations)
            {
                int[] frames = duration.FramesInState;
                int frameNumberThirdState = frames[2];
                int left = frameNumberThirdState % 2 == 0 ? frameNumberThirdState / 2 : (frameNumberThirdState + 1) / 2;
                int right = frameNumberThirdState - left;

                PhoneStateDuration transferedDuration = new PhoneStateDuration();
                transferedDuration.PhoneLabel = duration.PhoneLabel;
                transferedDuration.FramesInState = new int[] { frames[0] + frames[1] + left, right + frames[3] + frames[4] };

                durFile.Durations.Add(transferedDuration);
            }
        }

        /// <summary>
        /// Export SPS's log F0 from utterance and write to a float binary file.
        /// </summary>
        /// <param name="utterance">Utterance.</param>
        /// <param name="logf0File">Output log f0 file.</param>
        public static void ExportSPSLogF0s(SP.TtsUtterance utterance, FloatBinaryFile logf0File)
        {
            SP.MemoryMatrix<float> phoneF0 = utterance.Acoustic.F0s;
            for (int modelIndex = 0; modelIndex < utterance.Acoustic.Units.Count; modelIndex++)
            {
                if (utterance.Acoustic.Units[modelIndex].IsSilence)
                {
                    continue;
                }

                uint startFrame = 0;
                uint frameLength = 0;
                utterance.Acoustic.GetFrames(modelIndex, ref startFrame, ref frameLength);
                for (uint i = startFrame; i < startFrame + frameLength; i++)
                {
                    float f0 = phoneF0[(int)i][0];
                    if (f0 > 0.0)
                    {
                        logf0File.Values.Add((float)Math.Log(f0));
                    }
                    else
                    {
                        logf0File.Values.Add(MinimumLogF0);
                    }
                }
            }
        }

        /// <summary>
        /// Export SPS's log F0 from utterance for the specific model
        /// And write to a float binary file.
        /// </summary>
        /// <param name="utterance">Utterance.</param>
        /// <param name="modelStartIndex">The starting position of the model from which you would like to export the related acoutic feature.</param>
        /// <param name="length">The number of model in the exporting.</param>
        /// <param name="logf0File">Output log f0 file.</param>
        public static void ExportSPSLogF0s(SP.TtsUtterance utterance, int modelStartIndex, int length, FloatBinaryFile logf0File)
        {
            SP.MemoryMatrix<float> phoneF0 = utterance.Acoustic.F0s;
            for (int modelIndex = modelStartIndex; modelIndex < modelStartIndex + length; modelIndex++)
            {
                if (utterance.Acoustic.Units[modelIndex].IsSilence)
                {
                    continue;
                }

                uint startFrame = 0;
                uint frameLength = 0;
                utterance.Acoustic.GetFrames(modelIndex, ref startFrame, ref frameLength);
                for (uint i = startFrame; i < startFrame + frameLength; i++)
                {
                    float f0 = phoneF0[(int)i][0];
                    if (f0 > 0.0)
                    {
                        logf0File.Values.Add((float)Math.Log(f0));
                    }
                    else
                    {
                        logf0File.Values.Add(MinimumLogF0);
                    }
                }
            }
        }

        /// <summary>
        /// Export SPS's F0 from utterance and write to a float binary file.
        /// </summary>
        /// <param name="utterance">Utterance.</param>
        /// <param name="f0File">Output f0 file.</param>
        /// <param name="withSilence">Whether export f0 for silence phonemes.</param>
        public static void ExportSPSF0s(SP.TtsUtterance utterance, FloatBinaryFile f0File, bool withSilence)
        {
            float[,] f0s = ToTwoDimensionArray<float>(utterance.Acoustic.F0s);

            for (int unitIndex = 0; unitIndex < utterance.Acoustic.Units.Count; unitIndex++)
            {
                // Filter out slience unit because those units are not handled by Rus backend. 
                if (!utterance.Acoustic.Units[unitIndex].IsSilence)
                {
                    uint startFrameIndex = 0;
                    uint frameCount = 0;
                    utterance.Acoustic.GetFrames(unitIndex, ref startFrameIndex, ref frameCount);

                    for (uint i = startFrameIndex; i < startFrameIndex + frameCount; i++)
                    {
                        f0File.Values.Add((float)f0s[i, 0]);
                    }
                }
            }
        }

        /// <summary>
        /// Export SPS's F0 from utterance for the specific model
        /// And write to a float binary file.
        /// </summary>
        /// <param name="utterance">Utterance.</param>
        /// <param name="modelStartIndex">The starting position of the model from which you would like to export the related acoutic feature.</param>
        /// <param name="length">The number of model in the exporting.</param>
        /// <param name="f0File">Output f0 file.</param>
        public static void ExportSPSF0s(SP.TtsUtterance utterance, int modelStartIndex, int length, FloatBinaryFile f0File)
        {
            SP.MemoryMatrix<float> phoneF0 = utterance.Acoustic.F0s;
            for (int modelIndex = modelStartIndex; modelIndex < modelStartIndex + length; modelIndex++)
            {
                if (utterance.Acoustic.Units[modelIndex].IsSilence)
                {
                    continue;
                }

                uint startFrame = 0;
                uint frameLength = 0;
                utterance.Acoustic.GetFrames(modelIndex, ref startFrame, ref frameLength);
                for (uint i = startFrame; i < startFrame + frameLength; i++)
                {
                    float f0 = phoneF0[(int)i][0];
                    f0File.Values.Add(f0);
                }
            }
        }

        /// <summary>
        /// Export SPS's Lsp and Gain to a float binary file.
        /// </summary>
        /// <param name="utterance">Utterance.</param>
        /// <param name="lspFile">LspFile.</param>
        public static void ExportSPSLspsGains(SP.TtsUtterance utterance, FloatBinaryFile lspFile)
        {
            ExportSPSLspsGains(utterance, lspFile, false);
        }

        /// <summary>
        /// Export SPS's Lsp and Gain to a float binary file.
        /// </summary>
        /// <param name="utterance">Utterance.</param>
        /// <param name="lspFile">LspFile.</param>
        /// <param name="withSilence">Whether export the Lsp and Gain for silence phonemes.</param>
        public static void ExportSPSLspsGains(SP.TtsUtterance utterance, FloatBinaryFile lspFile, bool withSilence)
        {
            int lsfOrder = utterance.Acoustic.Lsfs.Column;
            float[,] lsfs = ToTwoDimensionArray<float>(utterance.Acoustic.Lsfs);
            float[,] gains = ToTwoDimensionArray<float>(utterance.Acoustic.Gains);

            int columns = lsfs.GetLength(1);

            for (int unitIndex = 0; unitIndex < utterance.Acoustic.Units.Count; unitIndex++)
            {
                if (withSilence || !utterance.Acoustic.Units[unitIndex].IsSilence)
                {
                    uint startFrameIndex = 0;
                    uint frameCount = 0;
                    utterance.Acoustic.GetFrames(unitIndex, ref startFrameIndex, ref frameCount);

                    for (int lspRowIndex = (int)startFrameIndex; lspRowIndex < startFrameIndex + frameCount; lspRowIndex++)
                    {
                        for (int lspColIndex = 0; lspColIndex < lsfOrder; lspColIndex++)
                        {
                            lspFile.Values.Add(lsfs[lspRowIndex, lspColIndex]);
                        }

                        lspFile.Values.Add(gains[lspRowIndex, 0]);
                    }
                }
            }
        }

        /// <summary>
        /// Export SPS's Lsp and Gain for the specific model
        /// And write to a float binary file.
        /// </summary>
        /// <param name="utterance">Utterance.</param>
        /// <param name="modelStartIndex">The starting position of the model from which you would like to export the related acoutic feature.</param>
        /// <param name="length">The number of model in the exporting.</param>
        /// <param name="lspFile">Output lsp file.</param>
        public static void ExportSPSLspsGains(SP.TtsUtterance utterance, int modelStartIndex, int length, FloatBinaryFile lspFile)
        {
            int lsfOrder = utterance.Acoustic.Lsfs.Column;
            float[,] lsfs = ToTwoDimensionArray<float>(utterance.Acoustic.Lsfs);
            float[,] gains = ToTwoDimensionArray<float>(utterance.Acoustic.Gains);

            for (int modelIndex = modelStartIndex; modelIndex < modelStartIndex + length; modelIndex++)
            {
                if (utterance.Acoustic.Units[modelIndex].IsSilence)
                {
                    continue;
                }

                uint frameStart = 0;
                uint frameLenght = 0;

                utterance.Acoustic.GetFrames(modelIndex, ref frameStart, ref frameLenght);
                for (int rowIndex = (int)frameStart; rowIndex < frameStart + frameLenght; rowIndex++)
                {
                    for (int columnIndex = 0; columnIndex < lsfOrder; columnIndex++)
                    {
                        lspFile.Values.Add(lsfs[rowIndex, columnIndex]);
                    }

                    lspFile.Values.Add(gains[rowIndex, 0]);
                }
            }
        }

        /// <summary>
        /// Export Log NusUnit's log F0(fix point) of NuSps to file.
        /// </summary>
        /// <param name="utterance">Utterance.</param>
        /// <param name="logF0File">Output f0 file.</param>
        public static void ExportNuSpsOriLogF0FixPoint(SP.TtsUtterance utterance, FloatBinaryFile logF0File)
        {
            if (utterance.NusUnits == null || utterance.NusUnits.Count < 1)
            {
                throw new Exception("TtsUtterance.NusUnits is null");
            }

            const int MaxLogF0DownScaleFactor = 7;
            const int ShortMaxF0 = 32768;

            foreach (SP.TtsNusUnit nusUnit in utterance.NusUnits)
            {
                MemoryMatrix<long> logF0sData = nusUnit.LogF0FixPoint;
                Debug.Assert(logF0sData != null, "NusUnit.LofF0FixPoint should is null value.");

                int frameCount = logF0sData.Row;
                int featureOrder = logF0sData.Column;
                for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
                {
                    for (int orderIndex = 0; orderIndex < featureOrder; orderIndex++)
                    {
                        long f0Value = logF0sData[frameIndex][orderIndex];
                        float f0ValueFloat = (float)f0Value * MaxLogF0DownScaleFactor / ShortMaxF0;
                        logF0File.Values.Add(f0ValueFloat);
                    }
                }
            }
        }

        /// <summary>
        /// Export Log NusUnit's log F0(float point) of NuSps to file.
        /// </summary>
        /// <param name="utterance">Utterance.</param>
        /// <param name="logF0File">Output f0 file.</param>
        public static void ExportNuSpsOriLogF0FloatPoint(SP.TtsUtterance utterance, FloatBinaryFile logF0File)
        {
            if (utterance.NusUnits == null || utterance.NusUnits.Count < 1)
            {
                throw new Exception("TtsUtterance.NusUnits is null");
            }

            foreach (TtsNusUnit nusUnit in utterance.NusUnits)
            {
                MemoryMatrix<float> logF0data = nusUnit.LogF0FloatPoint;
                SaveNuSpsFeatureFloatPointToFile(logF0data, logF0File);
            }
        }

        /// <summary>
        /// Export Log NusUnit's Lsp and Gain(fix point) of NuSps to file.
        /// </summary>
        /// <param name="utterance">Utterance.</param>
        /// <param name="lspGainFile">Output lspGain file.</param>
        public static void ExportNuSpsOriLspsGainFixPoint(SP.TtsUtterance utterance, FloatBinaryFile lspGainFile)
        {
            if (utterance.NusUnits == null || utterance.NusUnits.Count < 1)
            {
                throw new Exception("TtsUtterance.NusUnits is null");
            }

            const int ShortMax = 32767;
            const int MaxGainMean = 10;

            foreach (TtsNusUnit nusUnit in utterance.NusUnits)
            {
                int lsfOrder = utterance.Acoustic.Lsfs.Column;
                int gainOrder = utterance.Acoustic.Gains.Column;

                MemoryMatrix<long> lspGainData = nusUnit.LsfGainFixPoint;
                Debug.Assert(lspGainData != null, "nusUnit.LsfGainFixPoint is Null value");

                int frameCount = lspGainData.Row;
                int featureOrder = lspGainData.Column;
                for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
                {
                    int orderIndex = 0;
                    for (int lspOrderIndex = 0; lspOrderIndex < lsfOrder; lspOrderIndex++, orderIndex++)
                    {
                        long lspValue = lspGainData[frameIndex][orderIndex];
                        float lspValueFloat = (float)lspValue / ShortMax;
                        lspGainFile.Values.Add(lspValueFloat);
                    }

                    for (int gainOrderIndex = 0; gainOrderIndex < gainOrder; gainOrderIndex++, orderIndex++)
                    {
                        long gainValue = lspGainData[frameIndex][orderIndex];
                        float gainValueFloat = (float)gainValue / ShortMax * MaxGainMean;
                        lspGainFile.Values.Add(gainValueFloat);
                    }
                }
            }
        }

        /// <summary>
        /// Export Log NusUnit's Lsp and Gain(float point) of NuSps to file.
        /// </summary>
        /// <param name="utterance">Utterance.</param>
        /// <param name="lspGainFile">Output lspGain file.</param>
        public static void ExportNuSpsOriLspGainFloatPoint(SP.TtsUtterance utterance, FloatBinaryFile lspGainFile)
        {
            if (utterance.NusUnits == null || utterance.NusUnits.Count < 1)
            {
                throw new Exception("TtsUtterance.NusUnits is null");
            }

            foreach (TtsNusUnit nusUnit in utterance.NusUnits)
            {
                MemoryMatrix<float> lsfData = nusUnit.LsfFloatPoint;
                MemoryMatrix<float> gainData = nusUnit.GainFloatPoint;
                int frameCount = lsfData.Row;
                int lspOrder = lsfData.Column;
                int gainOrder = gainData.Column;

                Debug.Assert(lsfData != null, "lsfData is null");
                Debug.Assert(gainData != null, "gainData is null");

                for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
                {
                    for (int lspIndex = 0; lspIndex < lspOrder; lspIndex++)
                    {
                        lspGainFile.Values.Add(lsfData[frameIndex][lspIndex]);
                    }

                    for (int gainIndex = 0; gainIndex < gainOrder; gainIndex++)
                    {
                        lspGainFile.Values.Add(gainData[frameIndex][gainIndex]);
                    }
                }
            }
        }

        /// <summary>
        /// Export all of the SPS's acoutic feature from utterance for the specific part, say, nuu or dynamic part .
        /// </summary>
        /// <param name="utterance">Utterance.</param>
        /// <param name="nuuOnly">True, only exprot the acoustic feature of all nuu unit; False, only exprot the acoustic feature of all units in dynamic part of sentence.</param>
        /// <param name="f0BeforeLog">True, export the F0 data before log; False, will export the Log F0 data to file.</param>
        /// <param name="durationFile">Duration file.</param>
        /// <param name="f0File">F0 file.</param>
        /// <param name="lspFile">Lsp file.</param>
        public static void ExportSPSAcousticFeaturePartly(SP.TtsUtterance utterance, bool nuuOnly, bool f0BeforeLog,
            DurationFile durationFile, FloatBinaryFile f0File, FloatBinaryFile lspFile)
        {
            Dictionary<IntPtr, SP.TtsNusUnit> nusUnitDic = new Dictionary<IntPtr, SP.TtsNusUnit>();
            foreach (SP.TtsNusUnit nusUnit in utterance.NusUnits)
            {
                nusUnitDic[nusUnit.FirstChild.PhonePtr] = nusUnit;
            }

            for (int index = 0; index < utterance.Phones.Count; index++)
            {
                int nuuModelStart = 0;
                int nuuModelLength = 1;
                bool nuuGet = false;

                SP.TtsPhone phone = utterance.Phones[index];
                if (nusUnitDic.ContainsKey(phone.PhonePtr))
                {
                    nuuGet = true;

                    nuuModelStart = index;
                    SP.TtsNusUnit nusUnit = nusUnitDic[phone.PhonePtr];
                    while (phone.PhonePtr != nusUnit.LastChild.PhonePtr)
                    {
                        index++;
                        nuuModelLength++;
                        phone = utterance.Phones[index];
                    }
                }

                if (nuuGet && nuuOnly)
                {
                    SaveAcouticFeatureToFile(utterance, nuuModelStart, nuuModelLength, f0BeforeLog, durationFile, f0File, lspFile);
                }
                else if (!nuuGet && !nuuOnly)
                {
                    SaveAcouticFeatureToFile(utterance, index, 1, f0BeforeLog, durationFile, f0File, lspFile);
                }
            }
        }

        /// <summary>
        /// Export f0 info from the best path to a float binary file.
        /// </summary>
        /// <param name="utterance">Wave unit info on the best path.</param>
        /// <param name="wuiManager">Wui manager to get the sentence ID.</param>
        /// <param name="f0File">F0 file.</param>
        /// <param name="interMediaData">Inter media data.</param>
        public static void ExportBestPathF0s(SP.TtsUtterance utterance, WuiManager wuiManager, FloatBinaryFile f0File, string interMediaData)
        {
            List<double> f0s = new List<double>();
            string f0DataPath = Path.Combine(interMediaData, @"data\f0");
           
            // Work through the node on the best path
            int bestNodeIndex = (int)utterance.UnitLattice.WucList[utterance.Units.Count - 1].BestNodeIndex;
            for (int i = utterance.Units.Count - 1; i >= 0; i--)
            {
                // Get the previous node on the best path
                SP.WaveUnitCostNode costNode = utterance.UnitLattice.WucList[i].WucNodeList[bestNodeIndex];
                bestNodeIndex = costNode.PrecedeNodeIndex;

                // Filter out slience unit because those units are not handled by Rus backend. 
                if (!utterance.Units[i].IsSilence)
                {
                    WaveUnitInfo waveUnitInfo = costNode.WaveUnitInfo;
                    string recordingSentID = wuiManager.GetSentenceId(waveUnitInfo);
                    string recordingFileName = recordingSentID + ".f0";
                    string recordingFilePath = Directory.GetFiles(f0DataPath, recordingFileName, SearchOption.AllDirectories)[0];
                    List<double> unitF0s = LoadFrameF0(recordingFilePath, costNode.RecordingFrameStartIndex, costNode.FrameLength);
                    f0s.InsertRange(0, unitF0s);
                }
            }

            foreach (double f0 in f0s)
            {
                f0File.Values.Add((float)f0);
            }
        }

        /// <summary>
        /// Export Lsp and Gain from the best path to a float binary file.
        /// </summary>
        /// <param name="utterance">Wave unit info on the best path.</param>
        /// <param name="wuiManager">Wui manager to get the sentence ID.</param>
        /// <param name="lspFile">Output lsp file.</param>
        /// <param name="interMediaData">Voice font intermedia data.</param>
        /// <param name="lpcOrder">LpcOrder.</param>
        public static void ExportBestPathLspsGains(SP.TtsUtterance utterance, WuiManager wuiManager, FloatBinaryFile lspFile, string interMediaData, int lpcOrder)
        {
            List<List<double>> lspGain = new List<List<double>>();
            string lspDataPath = Path.Combine(interMediaData, @"data\Intermediate\lsp");

            // Work thourgh the best path and get the LSP info
            int bestNodeIndex = (int)utterance.UnitLattice.WucList[utterance.Units.Count - 1].BestNodeIndex;
            for (int i = utterance.Units.Count - 1; i >= 0; i--)
            {
                // Get the previous node on the best path
                SP.WaveUnitCostNode costNode = utterance.UnitLattice.WucList[i].WucNodeList[bestNodeIndex];
                bestNodeIndex = costNode.PrecedeNodeIndex;

                // Filter out slience unit because those units are not handled by Rus backend. 
                if (!utterance.Units[i].IsSilence)
                {
                    WaveUnitInfo waveUnitInfo = costNode.WaveUnitInfo;
                    string recordingSentID = wuiManager.GetSentenceId(waveUnitInfo);
                    string recordingFileName = recordingSentID + ".lsp";
                    string recordingFilePath = Directory.GetFiles(lspDataPath, recordingFileName, SearchOption.AllDirectories)[0];
                    List<List<double>> unistLspGainData = LoadFrameLsp(recordingFilePath, costNode.RecordingFrameStartIndex, costNode.FrameLength, lpcOrder);
                    lspGain.InsertRange(0, unistLspGainData);
                }
            }

            foreach (List<double> lspGainofOneFrame in lspGain)
            {
                foreach (double value in lspGainofOneFrame)
                {
                    lspFile.Values.Add((float)value);
                }
            }
        }

        /// <summary>
        /// Export duration info of the best bath to a float binary file.
        /// </summary>
        /// <param name="utterance">Utterance.</param>
        /// <param name="durFile">Output duration file.</param>
        public static void ExportBestPathDuration(SP.TtsUtterance utterance, DurationFile durFile)
        {
            // Work through the node on the best path
            int bestNodeIndex = (int)utterance.UnitLattice.WucList[utterance.Units.Count - 1].BestNodeIndex;
            for (int i = utterance.Units.Count - 1; i >= 0; i--)
            {
                // Filter out slience unit because those units are not handled by Rus backend. 
                if (!utterance.Units[i].IsSilence)
                {
                    // Because we search from back to front, the first unit is right phone . 
                    SP.WaveUnitCostNode rightPhoneCostNode = utterance.UnitLattice.WucList[i].WucNodeList[bestNodeIndex];
                    
                    // Get the left phone best node index
                    bestNodeIndex = rightPhoneCostNode.PrecedeNodeIndex;
                    i--;
                    SP.WaveUnitCostNode leftPhoneCostNode = utterance.UnitLattice.WucList[i].WucNodeList[bestNodeIndex];

                    string leftPhoneName = utterance.Units[i].UnitText.Split('_')[1];
                    string rightPhoneName = utterance.Units[i + 1].UnitText.Split('_')[1];
                    Debug.Assert(leftPhoneName == rightPhoneName, "The leftphone and rightphone does not match!");

                    PhoneStateDuration stateDuration = new PhoneStateDuration();
                    stateDuration.PhoneLabel = leftPhoneName;
                    
                    // assume the state is 2 for RUS, since can't get the 5 state frame number from service provider
                    stateDuration.FramesInState = new int[] { (int)leftPhoneCostNode.FrameLength, (int)rightPhoneCostNode.FrameLength };
                    durFile.Durations.Insert(0, stateDuration);
                }

                // Get the previous node on the best path
                SP.WaveUnitCostNode costNode = utterance.UnitLattice.WucList[i].WucNodeList[bestNodeIndex];
                bestNodeIndex = costNode.PrecedeNodeIndex;
            }
        }

        /// <summary>
        /// Get two types CC matrix of the unit lattice,
        /// One with the concost value calcuated from online,
        /// Another with the concost value get from runtime.
        /// </summary>
        /// <param name="ttsUtterance">Utterance.</param>
        /// <param name="wuiManager">WUI manager.</param>
        /// <param name="marginDataLengthInSamples">Margin data length, unit is sample.</param>
        /// <param name="windowLength">Window length, unit is smaple.</param>
        /// <param name="samplersPerFrame">Data length of a frame in samples.</param>
        /// <param name="waveFileMap">Recording wave file map.</param>
        /// <param name="recordingWaveRootDirPath">Recording wave root dir.</param>
        /// <param name="ccMatrixOnlineList">The corr coef matrix list calculated by online.</param>
        /// <param name="ccMatrixRuntimeList">The corr coef matrix list get from runtime.</param>
        /// <param name="bestPathCCOnlineList">The corr coef value list on best path calculated by online.</param>
        /// <param name="bestPathCCRuntimeList">The corr coef value list on best path get from runtime.</param>
        public static void ExtractTwoTypesCCTable(SP.TtsUtterance ttsUtterance,
                                                  SP.WuiManager wuiManager,
                                                  int marginDataLengthInSamples,
                                                  int windowLength,
                                                  int samplersPerFrame,
                                                  FileListMap waveFileMap,
                                                  string recordingWaveRootDirPath,
                                                  out List<float[,]> ccMatrixOnlineList,
                                                  out List<float[,]> ccMatrixRuntimeList,
                                                  out List<float> bestPathCCOnlineList,
                                                  out List<float> bestPathCCRuntimeList)
        {
            List<UnitCandidatesTailHeadMargin> costNodeFrameMarginDataList =
                new List<UnitCandidatesTailHeadMargin>(ttsUtterance.Units.Count);
            using (SP.CrossCorrelation xcorr = new SP.CrossCorrelation(windowLength))
            {
                for (int i = 0; i < ttsUtterance.Units.Count; i++)
                {
                    SP.TtsUnit ttsUnit = ttsUtterance.Units[i];

                    UnitCandidatesTailHeadMargin costListFrameMarginWaveData =
                        new UnitCandidatesTailHeadMargin(ttsUtterance.UnitLattice.WucList[i],
                                            wuiManager,
                                            ttsUnit.IsSilence,
                                            waveFileMap,
                                            recordingWaveRootDirPath,
                                            marginDataLengthInSamples,
                                            samplersPerFrame);

                    costNodeFrameMarginDataList.Add(costListFrameMarginWaveData);
                }

                // Construct the best path cost node index list,
                // start from index = n-1 unit, end to index = 0 unit,
                // 1. Find the lowest score candidate in the last column
                // 2. Trace back the lattice for the best path
                LinkedList<int> bestPathCostNodeIndexList = new LinkedList<int>();
                int bestNodeIndex = (int)ttsUtterance.UnitLattice.WucList[ttsUtterance.Units.Count - 1].BestNodeIndex;
                for (int curUnitIndex = ttsUtterance.Units.Count - 1; curUnitIndex >= 0; curUnitIndex--)
                {
                    bestPathCostNodeIndexList.AddFirst(bestNodeIndex);
                    bestNodeIndex = ttsUtterance.UnitLattice.WucList[curUnitIndex].WucNodeList[bestNodeIndex].PrecedeNodeIndex;
                }

                ccMatrixOnlineList = new List<float[,]>();
                ccMatrixRuntimeList = new List<float[,]>();
                bestPathCCOnlineList = new List<float>();
                bestPathCCRuntimeList = new List<float>();

                for (int curUnitIndex = 1; curUnitIndex < ttsUtterance.Units.Count; curUnitIndex++)
                {
                    int prevUnitIndex = curUnitIndex - 1;
                    float[,] ccMatrixOnline, ccMatrixRuntime;
                    float onlineBestPathCC, runtimeBestPathCC;
                    SP.WaveUnitCostNode curBestCostNode;
                   
                    // Calculate the cc value online
                    UnitCandidatesTailHeadMargin curNodeCostListWaveData = costNodeFrameMarginDataList[curUnitIndex];
                    UnitCandidatesTailHeadMargin prevNodeCostListWaveData = costNodeFrameMarginDataList[prevUnitIndex];
                    
                    // Get best path cost node index
                    int curBestNodeIndex = bestPathCostNodeIndexList.ElementAt(curUnitIndex);
                    int prevBestNodeIndex = bestPathCostNodeIndexList.ElementAt(curUnitIndex - 1);
                    curBestCostNode = ttsUtterance.UnitLattice.WucList[curUnitIndex].WucNodeList[curBestNodeIndex];

                    if (prevNodeCostListWaveData.IsSilence || curNodeCostListWaveData.IsSilence)
                    {
                        ccMatrixOnline = new float[,] { { 0 } };
                        ccMatrixRuntime = new float[,] { { 0 } };
                        onlineBestPathCC = 0;
                        runtimeBestPathCC = 1 - curBestCostNode.ConCost;
                    }
                    else
                    {
                        // use matrix CC calculation for saving time
                        SP.ValueLocation[,] varLocs =
                                xcorr.FindMaxCorrCoef(prevNodeCostListWaveData.TailMargins,
                                                      curNodeCostListWaveData.HeadMargins);
                        int rowsLen = varLocs.GetUpperBound(0) + 1;
                        int colsLen = varLocs.GetUpperBound(1) + 1;
                        ccMatrixOnline = new float[rowsLen, colsLen];
                        for (int rowIndex = varLocs.GetLowerBound(0); rowIndex < rowsLen; rowIndex++)
                        {
                            for (int columnIndex = varLocs.GetLowerBound(1); columnIndex < colsLen; columnIndex++)
                            {
                                ccMatrixOnline[rowIndex, columnIndex] = varLocs[rowIndex, columnIndex].Value;
                            }
                        }

                        // get correspdonding unit cost matrix from TTS runtime
                        ccMatrixRuntime = ttsUtterance.UnitLattice.WucList[curUnitIndex].JoinCostMatrix;
                        
                        // transfer to CC matrix
                        rowsLen = ccMatrixRuntime.GetUpperBound(0) + 1;
                        colsLen = ccMatrixRuntime.GetUpperBound(1) + 1;
                        for (int rowIndex = ccMatrixRuntime.GetLowerBound(0); rowIndex < rowsLen; rowIndex++)
                        {
                            for (int columnIndex = ccMatrixRuntime.GetLowerBound(1); columnIndex < colsLen; columnIndex++)
                            {
                                ccMatrixRuntime[rowIndex, columnIndex] = 1 - ccMatrixRuntime[rowIndex, columnIndex];
                            }
                        }

                        Debug.Assert(ccMatrixOnline.Rank == ccMatrixRuntime.Rank,
                            "CCMatrixOnline and CCMatrixRuntime have different dimension");
                        Debug.Assert(ccMatrixOnline.GetUpperBound(0) == ccMatrixRuntime.GetUpperBound(0),
                            "CCMatrixOnline and CCMatrixRuntime have different column size");

                        // Get the CC on best path
                        onlineBestPathCC = ccMatrixOnline[prevBestNodeIndex, curBestNodeIndex];
                        runtimeBestPathCC = 1 - curBestCostNode.ConCost;
                    }

                    ccMatrixOnlineList.Add(ccMatrixOnline);
                    ccMatrixRuntimeList.Add(ccMatrixRuntime);
                    bestPathCCOnlineList.Add(onlineBestPathCC);
                    bestPathCCRuntimeList.Add(runtimeBestPathCC);
                }
            }

            Debug.Assert(ccMatrixOnlineList.Count == ccMatrixRuntimeList.Count,
                "Utterance has different count of CC matrixs between online and runtime");
            Debug.Assert(bestPathCCOnlineList.Count == bestPathCCRuntimeList.Count,
                "Utterance has different count of CC on best path between online and runtime");
        }

        /// <summary>
        /// Load F0 from start frame index with specific frame length.
        /// </summary>
        /// <param name="f0File">F0 file name.</param>
        /// <param name="startFrameIndex">Start frame index.</param>
        /// <param name="frameLength">Index length.</param>
        /// <returns>FrameF0Data.</returns>
        public static List<double> LoadFrameF0(string f0File, int startFrameIndex, int frameLength)
        {
            List<double> f0Data = LoadF0Data(f0File);
            List<double> frameF0Data = new List<double>(f0Data.Where((value, index) => index >= startFrameIndex && index < (startFrameIndex + frameLength)));
            return frameF0Data;
        }

        /// <summary>
        /// Load Lsp data from start frame index with specific frame length.
        /// </summary>
        /// <param name="lspFile">Lsp file.</param>
        /// <param name="startFrameIndex">Start frame index.</param>
        /// <param name="frameLength">Frame length.</param>
        /// <param name="lpcOrder">Lpc dimension.</param>
        /// <returns>FrameLspData.</returns>
        public static List<List<double>> LoadFrameLsp(string lspFile, int startFrameIndex, int frameLength, int lpcOrder)
        {
            List<List<double>> lspData = Microsoft.Tts.Offline.ObjectiveMeasure.RmseEvaluation.LoadLsp(lspFile, lpcOrder);
            List<List<double>> frameLspData = new List<List<double>>(lspData.Where((value, index) => index >= startFrameIndex && index < (startFrameIndex + frameLength)));
            return frameLspData;
        }

        /// <summary>
        /// Convert two dimension int arrary to two dimension UInt32 array.
        /// </summary>
        /// <param name="source">Source.</param>
        /// <returns>Uint.</returns>
        public static uint[,] ToUIntArray(int[,] source)
        {
            uint[,] target = new uint[source.GetLength(0), source.GetLength(1)];

            for (int i = 0; i < source.GetLength(0); ++i)
            {
                for (int j = 0; j < source.GetLength(1); ++j)
                {
                    target[i, j] = (uint)source[i, j];
                }
            }

            return target;
        }

        /// <summary>
        /// Set the the value in the SP.MemoryMatrix to the source with an offset. 
        /// </summary>
        /// <typeparam name="T">Type.</typeparam>
        /// <param name="source">Source matrix.</param>
        /// <param name="memoryMatrix">Target memory matrix.</param>
        /// <param name="offset">Offset.</param>
        public static void SetValues<T>(T[,] source, SP.MemoryMatrix<T> memoryMatrix, int offset) where T : struct
        {
            for (int i = 0; i < memoryMatrix.Row; ++i)
            {
                for (int j = 0; j < memoryMatrix.Column; ++j)
                {
                    memoryMatrix[i][j] = source[i + offset, j];
                }
            }
        }

        private static void SaveNuSpsFeatureFloatPointToFile(MemoryMatrix<float> data, FloatBinaryFile file)
        {
            if (data == null)
            {
                return;
            }

            int frameCount = data.Row;
            int featureOrder = data.Column;
            for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                for (int orderIndex = 0; orderIndex < featureOrder; orderIndex++)
                {
                    file.Values.Add(data[frameIndex][orderIndex]);
                }
            }
        }

        private static void SaveAcouticFeatureToFile(SP.TtsUtterance utterance, int modelStart, int modelLength, bool f0BeforeLog, DurationFile durationFile, FloatBinaryFile f0File, FloatBinaryFile lspFile)
        {
            ExportSPSDurations(utterance, modelStart, modelLength, durationFile);
            if (f0BeforeLog)
            {
                ExportSPSF0s(utterance, modelStart, modelLength, f0File);
            }
            else
            {
                ExportSPSLogF0s(utterance, modelStart, modelLength, f0File);
            }

            ExportSPSLspsGains(utterance, modelStart, modelLength, lspFile);
        }

        /// <summary>
        /// Load F0 data from the f0 file.
        /// </summary>
        /// <param name="f0File">F0 file name.</param>
        /// <returns>Listed f0 value.</returns>
        private static List<double> LoadF0Data(string f0File)
        {
            List<float> f0DataInFile = File.ReadAllLines(f0File).Select(p => float.Parse(p)).ToList();
            List<double> f0Data = new List<double>();
            foreach (float value in f0DataInFile)
            {
                f0Data.Add(value);
            }

            return f0Data;
        }

        /// <summary>
        /// Template method to convert SP.MemoryMatrix to the two dimension arrary.
        /// </summary>
        /// <typeparam name="T">Type.</typeparam>
        /// <param name="memoryMatrix">Memory matrix.</param>
        /// <returns>Array.</returns>
        private static T[,] ToTwoDimensionArray<T>(SP.MemoryMatrix<T> memoryMatrix) where T : struct
        {
            T[,] array = new T[memoryMatrix.Row, memoryMatrix.Column];

            for (int i = 0; i < memoryMatrix.Row; ++i)
            {
                for (int j = 0; j < memoryMatrix.Column; ++j)
                {
                    array[i, j] = memoryMatrix[i][j];
                }
            }

            return array;
        }
    }

    /// <summary>
    /// Class for store the tail margin and head margin wave data
    /// For the unit candidates.
    /// </summary>
    internal class UnitCandidatesTailHeadMargin
    {
        /// <summary>
        /// Arrary for store each node's frame tail margin wave data.
        /// </summary>
        private List<short[]> _tailMarginWaveData;
        
        /// <summary>
        /// Arrary for store each node's frame head margin wave data.
        /// </summary>
        private List<short[]> _headMarginWaveData;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitCandidatesTailHeadMargin"/> class.
        /// Construct the candidates tail margin and head margin wave data of a unit.
        /// </summary>
        /// <param name="costList">The wave unit cost node list.</param>
        /// <param name="wuiManager">WUIManager.</param>
        /// <param name="isSilence">Silence unit.</param>
        /// <param name="waveFileMap">Recording wave file map.</param>
        /// <param name="recordingWaveDirPath">Recording wave root dir.</param>
        /// <param name="marginDataLengthInSamples">Margin length in samples.</param>
        /// <param name="samplesPerFrame">Data length of frame in samples.</param>
        public UnitCandidatesTailHeadMargin(SP.WaveUnitCostList costList,
                               SP.WuiManager wuiManager,
                               bool isSilence,
                               FileListMap waveFileMap,
                               string recordingWaveDirPath,
                               int marginDataLengthInSamples,
                               int samplesPerFrame)
        {
            IsSilence = isSilence;

            if (!IsSilence)
            {
                CostNodeCount = costList.WucNodeList.Count;

                // get tail and head margin data for each candidate
                _tailMarginWaveData = new List<short[]>(costList.WucNodeList.Count);
                _headMarginWaveData = new List<short[]>(costList.WucNodeList.Count);
                foreach (SP.WaveUnitCostNode costNode in costList.WucNodeList)
                {
                    // load recording wave file
                    string recordingSentID = wuiManager.GetSentenceId(costNode.WaveUnitInfo);
                    WaveFile waveFile = new WaveFile();
                    string recordingWavePath = waveFileMap.BuildPath(recordingWaveDirPath,
                                                 recordingSentID,
                                                 "wav");
                    waveFile.Load(recordingWavePath);

                    // get tail margin
                    int frameTailOffset = costNode.RecordingFrameEndIndex * samplesPerFrame;
                    int tailMarginStartOffset = frameTailOffset - (marginDataLengthInSamples / 2);
                    int tailMarginEndOffset = tailMarginStartOffset + marginDataLengthInSamples;

                    Debug.Assert(tailMarginStartOffset < waveFile.DataIn16Bits.Length);
                    Debug.Assert(tailMarginEndOffset < waveFile.DataIn16Bits.Length);
                    short[] tailMargin = waveFile.DataIn16Bits
                                         .Where((sample, index) => index >= tailMarginStartOffset
                                         && index < tailMarginEndOffset).ToArray();
                    _tailMarginWaveData.Add(tailMargin);

                    // get head margin
                    int frameHeadOffset = costNode.RecordingFrameStartIndex * samplesPerFrame;
                    int headMarginStartOffset = frameHeadOffset - marginDataLengthInSamples;
                    int headMarginEndOffset = headMarginStartOffset + (2 * marginDataLengthInSamples);
                    short[] headMargin = waveFile.DataIn16Bits
                                         .Where((sample, index) => index >= headMarginStartOffset
                                         && index < headMarginEndOffset).ToArray();
                    _headMarginWaveData.Add(headMargin);
                }
            }
            else
            {
                _tailMarginWaveData = null;
                _headMarginWaveData = null;
            }
        }

        /// <summary>
        /// Gets a value indicating whether unit is silence.
        /// </summary>
        public bool IsSilence { get; private set; }

        /// <summary>
        /// Gets tail margins of unit.
        /// </summary>
        public short[][] TailMargins
        {
            get
            {
                return _tailMarginWaveData.ToArray();
            }
        }

        /// <summary>
        /// Gets head margins of unit.
        /// </summary>
        public short[][] HeadMargins
        {
            get
            {
                return _headMarginWaveData.ToArray();
            }
        }

        /// <summary>
        /// Gets cost nodes count for cost list.
        /// </summary>
        public int CostNodeCount { get; private set; }
    }
}