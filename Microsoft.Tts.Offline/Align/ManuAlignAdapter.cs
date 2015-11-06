//----------------------------------------------------------------------------
// <copyright file="ManuAlignAdapter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements class to adapt manual alignment with forced alignment data
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Class to adapt manu-alignment segment data to well-labeled segment data.
    /// To speed up manu-alignment, vender will use simplest symbols to represent
    /// Each phones during segmentation. This module will recover this to well-labeled.
    /// <param />
    /// Vender re-aligns all the segments in the segment data. He/she can only insert or delete
    /// Silence segments in original automatic alignment data, and keeps all other segments.
    /// <example>
    /// Given script entry:
    /// 100001 american.
    ///        ax - m eh 1 - r ax - k . ax n /
    /// Raw manual alignment data, deliveried by vender:
    /// 0.00000 sp
    /// 0.25800 a
    /// 0.37960 a
    /// 0.54910 a
    /// 0.70830 a
    /// 0.76960 a
    /// 0.91630 sp
    /// Based on forced alignment:
    /// 0.00000 sil
    /// 0.28000 ax
    /// 0.38000 m+eh
    /// 0.55000 r+ax
    /// 0.71000 k
    /// 0.76000 ax+n
    /// 0.92000 sil
    /// Adapted result as:
    /// 0.00000 sil
    /// 0.25800 ax
    /// 0.37960 m+eh
    /// 0.54910 r+ax
    /// 0.70830 k
    /// 0.76960 ax+n
    /// 0.91630 sil.
    /// </example>
    /// </summary>
    public static class ManuAlignAdapter
    {
        #region Public static methods

        /// <summary>
        /// Adapt raw manual alignment data to well-labeled alignment data.
        /// </summary>
        /// <param name="rawManuDir">Raw manual alignment data directory.</param>
        /// <param name="forcedDir">Forced alignment data directory.</param>
        /// <param name="manuDir">Adapted result directory.</param>
        /// <returns>Data error set found.</returns>
        public static DataErrorSet Adapt(string rawManuDir, string forcedDir, string manuDir)
        {
            if (!Directory.Exists(manuDir))
            {
                Directory.CreateDirectory(manuDir);
            }

            DataErrorSet errorSet = new DataErrorSet();

            Dictionary<string, string> rawManSegMap = FileListMap.Build(rawManuDir, ".txt");
            Dictionary<string, string> forceSegMap = FileListMap.Build(forcedDir, ".txt");

            foreach (string id in rawManSegMap.Keys)
            {
                if (!forceSegMap.ContainsKey(id))
                {
                    string message = "unexpected raw manual segment id, not in forced set: " + id;
                    Console.Error.WriteLine(message);
                    errorSet.Errors.Add(new DataError(Path.Combine(forcedDir, rawManSegMap[id] + ".txt"),
                        message, id));
                    continue;
                }

                string rawSegmentFilePath = Path.Combine(rawManuDir, rawManSegMap[id]) + ".txt";
                string forcedSegmentFilePath = Path.Combine(forcedDir, forceSegMap[id]) + ".txt";
                string adaptedSegmentFilePath = Path.Combine(manuDir, rawManSegMap[id]) + ".txt";
                Helper.EnsureFolderExistForFile(adaptedSegmentFilePath);

                Collection<WaveSegment> rawSegments = null;
                try
                {
                    rawSegments = SegmentFile.ReadAllData(rawSegmentFilePath);
                }
                catch (InvalidDataException ide)
                {
                    errorSet.Errors.Add(new DataError(rawSegmentFilePath,
                        Helper.BuildExceptionMessage(ide), id));
                    continue;
                }
                catch (FileLoadException fle)
                {
                    errorSet.Errors.Add(new DataError(rawSegmentFilePath,
                        Helper.BuildExceptionMessage(fle), id));
                    continue;
                }

                Collection<WaveSegment> forcedSegments = null;
                try
                {
                    forcedSegments = SegmentFile.ReadAllData(forcedSegmentFilePath);
                }
                catch (InvalidDataException ide)
                {
                    errorSet.Errors.Add(new DataError(forcedSegmentFilePath,
                       Helper.BuildExceptionMessage(ide), id));
                    continue;
                }
                catch (FileLoadException fle)
                {
                    errorSet.Errors.Add(new DataError(forcedSegmentFilePath,
                        Helper.BuildExceptionMessage(fle), id));
                    continue;
                }

                RemoveSilenceSegment(forcedSegments);

                if (!Adapting(rawSegments, forcedSegments, adaptedSegmentFilePath, false))
                {
                    Console.Error.WriteLine("unmatched segment number in sentence:" + id);
                    errorSet.Errors.Add(new DataError(rawSegmentFilePath, 
                        "unmatched segment number in sentence between forced align [" + forcedSegmentFilePath + "] and raw manual alignment file [" + rawSegmentFilePath + "]", 
                        id));
                    continue;
                }

                Adapting(rawSegments, forcedSegments, adaptedSegmentFilePath, true);
            }

            return errorSet;
        }

        #endregion

        #region Private static methods

        /// <summary>
        /// Remove all silence segment in the segment collection.
        /// </summary>
        /// <param name="segments">Segment collection.</param>
        private static void RemoveSilenceSegment(Collection<WaveSegment> segments)
        {
            for (int i = segments.Count - 1; i >= 0; i--)
            {
                if (segments[i].IsSilenceFeature)
                {
                    segments.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Adapt certain segment file for one sentence.
        /// </summary>
        /// <param name="rawManSegs">Raw manual segment collection.</param>
        /// <param name="forcedSegs">Automatic forced alignment segment collection.</param>
        /// <param name="adaptedSegFile">Adapted result file.</param>
        /// <param name="writing">Indicate whether write to file.</param>
        /// <returns>Whether both segments are matched.</returns>
        private static bool Adapting(Collection<WaveSegment> rawManSegs,
            Collection<WaveSegment> forcedSegs, string adaptedSegFile, bool writing)
        {
            bool matched = true;
            StreamWriter adaptedWriter = null;

            if (writing)
            {
                adaptedWriter = new StreamWriter(adaptedSegFile, false, Encoding.ASCII);
            }

            try
            {
                int j = 0;
                for (int i = 0; i < rawManSegs.Count; i++)
                {
                    WaveSegment rawManSym = rawManSegs[i];
                    WaveSegment forceSym = null;
                    if (forcedSegs.Count > j)
                    {
                        forceSym = forcedSegs[j];
                    }

                    if (rawManSym.Label == "a"
                        || rawManSym.Label == "a1")
                    {
                        // for normal phome set
                        if (forceSym == null)
                        {
                            matched = false;
                            continue;
                        }

                        if (writing)
                        {
                            adaptedWriter.WriteLine(rawManSym.StartTime.ToString("F5",
                                CultureInfo.InvariantCulture) + " " + forceSym.Label);
                        }

                        j++;
                    }
                    else if (rawManSym.Label == "s" || /* silence tag in ja-JP */
                        rawManSym.Label == "sp" || /*beginning/ending silence tag in en-US*/
                        rawManSym.Label == "sd" /*middle silence tag in en-US*/)
                    {
                        // for silence phone
                        if (writing)
                        {
                            adaptedWriter.WriteLine(rawManSym.StartTime.ToString("F5",
                                CultureInfo.InvariantCulture) + " " + Phoneme.SilencePhone);
                        }
                    }
                    else if (rawManSym.Label == "d" /* stop tag in ja-JP */)
                    {
                        if (forceSym == null)
                        {
                            matched = false;
                            continue;
                        }

                        if (writing)
                        {
                            adaptedWriter.WriteLine(rawManSym.StartTime.ToString("F5",
                                CultureInfo.InvariantCulture) + " stop");
                        }

                        if (forceSym.Label == "stop")
                        {
                            j++;
                        }
                    }
                    else
                    {
                        System.Diagnostics.Trace.WriteLine(rawManSym.Label);
                    }
                }
            }
            finally
            {
                if (adaptedWriter != null)
                {
                    adaptedWriter.Close();
                }
            }

            return matched;
        }

        #endregion
    }
}