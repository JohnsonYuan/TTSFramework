//----------------------------------------------------------------------------
// <copyright file="EpochExtractor.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements EggConverter
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Waveform
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Description of EggConverter class.
    /// </summary>
    public static class EpochExtractor
    {
        #region Public static operations

        /// <summary>
        /// Extract epoch data from EGG files.
        /// </summary>
        /// <param name="mapFile">Map file list.</param>
        /// <param name="wave16kDir">16k Hz waveform directory.</param>
        /// <param name="egg16kDir">16k Hz EGG directory.</param>
        /// <param name="epochDir">Epoch directory.</param>
        /// <param name="skipExist">Falg to indicate whether skipping existing target file.</param>
        public static void EggToEpoch(string mapFile, string wave16kDir,
            string egg16kDir, string epochDir, bool skipExist)
        {
            // Const parameter used to generate epoch from wave16k and egg16k file.
            const double FilterLowFreq = 60.0;
            const double FilterHighFreq = 3600.0;
            const int BandPassOrder = 1000;
            const int LarEpochMinPitch = 110;
            const int LarEpochMaxPitch = 500;
            const int FrameSize = checked((int)(0.025f * 16000));
            const int LpcOrder = 20;
            const int AdjustFreqOffset = 10;

            // Validate file/dir parameter
            if (!File.Exists(mapFile))
            {
                throw Helper.CreateException(typeof(FileNotFoundException),
                    mapFile);
            }

            if (!Directory.Exists(wave16kDir))
            {
                throw Helper.CreateException(typeof(DirectoryNotFoundException),
                    wave16kDir);
            }

            if (!Directory.Exists(egg16kDir))
            {
                throw Helper.CreateException(typeof(DirectoryNotFoundException),
                    egg16kDir);
            }

            if (string.IsNullOrEmpty(epochDir))
            {
                throw new ArgumentNullException("epochDir");
            }

            FileListMap fileMap = new FileListMap();
            fileMap.Load(mapFile);

            ConsoleLogger logger = new ConsoleLogger();

            // Validate the consistence between the EGG and waveform files
            DataErrorSet errorSet = VoiceFont.ValidateWaveAlignment(fileMap, wave16kDir,
                egg16kDir, "EGG");
            if (errorSet.Errors.Count > 0)
            {
                foreach (DataError error in errorSet.Errors)
                {
                    logger.LogLine(error.ToString());

                    if (fileMap.Map.ContainsKey(error.SentenceId))
                    {
                        fileMap.Map.Remove(error.SentenceId);
                    }
                }
            }

            // Performance converting
            Helper.EnsureFolderExist(epochDir);
            foreach (string sid in fileMap.Map.Keys)
            {
                string wave16kFile = Path.Combine(wave16kDir,
                    fileMap.Map[sid] + ".wav");
                string egg16kFile = Path.Combine(egg16kDir,
                    fileMap.Map[sid] + ".wav");
                string epochFile = Path.Combine(epochDir,
                    fileMap.Map[sid] + ".epoch");

                if (!File.Exists(wave16kFile) || !File.Exists(egg16kFile))
                {
                    if (!File.Exists(wave16kFile))
                    {
                        logger.LogLine("Can't find file : {0}", wave16kFile);
                    }
                    else if (!File.Exists(egg16kFile))
                    {
                        logger.LogLine("Can't find file : {0}", egg16kFile);
                    }

                    continue;
                }

                Helper.EnsureFolderExistForFile(epochFile);

                // Performance EGG to Epoch conversion while
                // 1. not skipping the existing epoch file is asked
                // 2. Or the target epoch file does not exist
                // 3. Or the target epoch file is with zero length
                if (!skipExist ||
                    !File.Exists(epochFile) ||
                    new FileInfo(epochFile).Length == 0)
                {
                    EggAcousticFeature.Egg2Epoch(wave16kFile, egg16kFile,
                        epochFile, FilterLowFreq, FilterHighFreq, BandPassOrder,
                        LarEpochMinPitch, LarEpochMaxPitch, FrameSize, LpcOrder,
                        AdjustFreqOffset);
                }
            }
        }

        #endregion
    }
}