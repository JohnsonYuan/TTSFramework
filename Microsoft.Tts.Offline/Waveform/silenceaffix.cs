//----------------------------------------------------------------------------
// <copyright file="SilenceAffix.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements class to prefix and subfix silence to waveform file
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Waveform
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Utility;
    using Microsoft.Tts.Offline.Waveform;

    /// <summary>
    /// Class definition for silence affixation.
    /// </summary>
    public static class SilenceAffix
    {
        #region Public static operations

        /// <summary>
        /// Affix silence to waveform files directory, prefix and subfix.
        /// </summary>
        /// <param name="silenceDuration">Silence duration to affix.</param>
        /// <param name="sourceWave16kDir">Source waveform files directory.</param>
        /// <param name="targetWave16kDir">Target waveform files directory.</param>
        public static void AffixWaveFile(float silenceDuration,
            string sourceWave16kDir, string targetWave16kDir)
        {
            WaveFile wf = BuildSilenceWaveFile(silenceDuration);
            AffixWaveFiles(sourceWave16kDir, targetWave16kDir, wf);
        }

        /// <summary>
        /// Shift segment data with certain silence duration.
        /// </summary>
        /// <param name="silenceDuration">Silence duration in second.</param>
        /// <param name="sourceDir">Source segment directory.</param>
        /// <param name="targetDir">Target segment directory.</param>
        /// <returns>Data error set found.</returns>
        public static DataErrorSet ShiftSegmentFiles(float silenceDuration,
            string sourceDir, string targetDir)
        {
            DataErrorSet errorSet = new DataErrorSet();
            SegmentFile sf = new SegmentFile();
            Dictionary<string, string> sourceMap =
                Microsoft.Tts.Offline.FileListMap.Build(sourceDir, ".txt");
            foreach (string sid in sourceMap.Keys)
            {
                string sourceFilePath = null;
                string dstFilePath = null;
                try
                {
                    dstFilePath = Path.Combine(targetDir, sourceMap[sid] + ".txt");
                    if (File.Exists(dstFilePath))
                    {
                        continue;
                    }

                    sourceFilePath = Path.Combine(sourceDir, sourceMap[sid] + ".txt");
                    Helper.EnsureFolderExistForFile(dstFilePath);

                    sf.Load(sourceFilePath);
                    sf.Shift(silenceDuration);

                    sf.Save(dstFilePath);
                }
                catch (InvalidDataException ide)
                {
                    errorSet.Errors.Add(new DataError(sourceFilePath,
                        Helper.BuildExceptionMessage(ide), sid));
                }
            }

            return errorSet;
        }

        #endregion

        #region Private static operations

        /// <summary>
        /// Build silence waveform file.
        /// </summary>
        /// <param name="silenceDuration">Silence duration.</param>
        /// <returns>Silence waveform file path.</returns>
        private static WaveFile BuildSilenceWaveFile(float silenceDuration)
        {
            WaveFile wf = new WaveFile();
            WaveFormat fmt = new WaveFormat();
            fmt.Channels = 1;
            fmt.BlockAlign = 2;
            fmt.BitsPerSample = 16;
            fmt.ExtSize = 0;
            fmt.FormatTag = WaveFormatTag.Pcm;
            fmt.SamplesPerSecond = 16000;
            fmt.AverageBytesPerSecond = 32000;
            wf.Format = fmt;

            RiffChunk wave = wf.Riff.GetChunk(Riff.IdData);
            int sampleCount = (int)(silenceDuration * fmt.AverageBytesPerSecond);
            sampleCount -= sampleCount % 2; // align
            wave.SetData(new byte[sampleCount]);
            wave.Size = wave.GetData().Length;

            return wf;
        }

        /// <summary>
        /// Affix waveform file to certain waveform file.
        /// </summary>
        /// <param name="sourceWaveDir">Source waveform file directory.</param>
        /// <param name="targetWaveDir">Target waveform file directory.</param>
        /// <param name="affixingFile">Affixing waveform file.</param>
        private static void AffixWaveFiles(string sourceWaveDir,
            string targetWaveDir, WaveFile affixingFile)
        {
            Dictionary<string, string> srcMap =
                Microsoft.Tts.Offline.FileListMap.Build(sourceWaveDir, ".wav");
            foreach (string id in srcMap.Keys)
            {
                string dstFilePath = Path.Combine(targetWaveDir, srcMap[id] + ".wav");
                if (File.Exists(dstFilePath))
                {
                    continue;
                }

                string srcFilePath = Path.Combine(sourceWaveDir, srcMap[id] + ".wav");

                Helper.EnsureFolderExistForFile(dstFilePath);

                WaveFile tgtWf = new WaveFile();
                WaveFile srcWf = new WaveFile();
                srcWf.Load(srcFilePath);

                tgtWf.Append(affixingFile);
                tgtWf.Append(srcWf);
                tgtWf.Append(affixingFile);

                tgtWf.Save(dstFilePath);
            }
        }

        #endregion
    }
}