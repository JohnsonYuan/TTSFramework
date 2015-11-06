//----------------------------------------------------------------------------
// <copyright file="MulanLogger.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements class MulanLogger
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Tts.Offline.Utility;
    using Microsoft.Tts.Offline.Viterbi;

    /// <summary>
    /// Mulan logger for handle Mulan TTS engine tracing log.
    /// </summary>
    public static class MulanLogger
    {
        #region Public types
        /// <summary>
        /// Tag types.
        /// </summary>
        public enum Tag
        {
            /// <summary>
            /// Normalizaed text.
            /// </summary>
            NormText,

            /// <summary>
            /// Annotated text, with break level and emphasis marks.
            /// </summary>
            BreakAndEmph,

            /// <summary>
            /// Pitch annotation.
            /// </summary>
            Pitch,

            /// <summary>
            /// Pronunciation string of text.
            /// </summary>
            Pronun,

            /// <summary>
            /// Unit specification for searching and selection.
            /// </summary>
            UnitVector,

            /// <summary>
            /// Unit control list.
            /// </summary>
            UnitControl,

            /// <summary>
            /// Viterbi searching space dumping.
            /// </summary>
            CandidateDump,

            /// <summary>
            /// Selected all route dump .
            /// </summary>
            RouteDump,

            /// <summary>
            /// Average concatecost.
            /// </summary>
            AverageConcateCost,

            /// <summary>
            /// Index of each units in this utterance.
            /// </summary>
            Index,

            /// <summary>
            /// Target cost for corresponding unit.
            /// </summary>
            TargetCost,

            /// <summary>
            /// Concatenate cost for corresponding unit.
            /// </summary>
            ConcateCost,

            /// <summary>
            /// Finally selected wave unit vector.
            /// </summary>
            WaveUnitSel,

            /// <summary>
            /// Loader will ignore the unknown tagger.
            /// </summary>
            Unknown,
        }
        #endregion

        #region I/O operations

        /// <summary>
        /// Read utterance instance from text reader.
        /// </summary>
        /// <param name="tr">Text reader to read from.</param>
        /// <param name="language">Language of the utterance.</param>
        /// <param name="engine">Engine of the utterance.</param>
        /// <returns>Utterance instance.</returns>
        public static TtsUtterance ReadAllData(TextReader tr, Language language, EngineType engine)
        {
            if (tr == null)
            {
                throw new ArgumentNullException("tr");
            }

            TtsUtterance utterance = new TtsUtterance(language, engine);

            string line = null;
            bool done = false;
            while ((line = tr.ReadLine()) != null)
            {
            DO_WITH_TAG:
                if (!IsTag(line))
                {
                    continue;
                }

                try
                {
                    Tag tag = ParseTag(line);

                    // handle each kind tags
                    switch (tag)
                    {
                        case Tag.NormText:
                            line = HandleTagNormText(utterance, line, tr);
                            break;
                        case Tag.BreakAndEmph:
                            line = HandleTagBreakAndEmph(utterance, line, tr);
                            break;
                        case Tag.Pitch:
                            line = MoveToNextTag(tr);
                            break;
                        case Tag.Pronun:
                            line = HandleTagPronun(utterance, line, tr);
                            break;
                        case Tag.UnitVector:
                            line = HandleTagUnitVector(utterance, line, tr);
                            break;
                        case Tag.UnitControl:
                            line = MoveToNextTag(tr);
                            break;
                        case Tag.CandidateDump:
                            if (utterance.Viterbi == null)
                            {
                                utterance.Viterbi = new ViterbiSearch();
                            }

                            line = HandleTagCandidateDump(utterance, line, tr);
                            break;
                        case Tag.RouteDump:
                            line = HandleTagRouteDump(utterance, line, tr);
                            break;
                        case Tag.AverageConcateCost:
                            line = MoveToNextTag(tr);
                            break;
                        case Tag.Index:
                            line = MoveToNextTag(tr);
                            break;
                        case Tag.TargetCost:
                            line = MoveToNextTag(tr);
                            break;
                        case Tag.ConcateCost:
                            line = MoveToNextTag(tr);
                            break;
                        case Tag.WaveUnitSel:
                            line = HandleTagWaveUnitSel(utterance, line, tr);
                            done = true;
                            break;
                        case Tag.Unknown:
                            line = null;
                            break;
                        default:
                            break;
                    }
                }
                catch (InvalidDataException ide)
                {
                    // if parsing failed, return null
                    System.Diagnostics.Trace.WriteLine(ide.Message);
                    throw;
                }

                if (done)
                {
                    break;
                }

                if (!string.IsNullOrEmpty(line))
                {
                    goto DO_WITH_TAG;
                }
            }

            PostRead(utterance);

            return utterance;
        }

        /// <summary>
        /// Load Tts utterance from Mulan TTS engine trace log.
        /// </summary>
        /// <param name="filePath">Log file path.</param>
        /// <param name="language">Language of the utterance.</param>
        /// <param name="engine">Engine type of the utterance.</param>
        /// <returns>TtsUtterance object.</returns>
        public static TtsUtterance ReadAllData(string filePath,
            Language language, EngineType engine)
        {
            if (!File.Exists(filePath))
            {
                throw Helper.CreateException(typeof(FileNotFoundException),
                    filePath);
            }

            using (TextReader tr = new StreamReader(filePath))
            {
                return ReadAllData(tr, language, engine);
            }
        }

        /// <summary>
        /// Save the utterance in Mulan TTS log formated text file.
        /// </summary>
        /// <param name="filePath">Target file path to save utterance.</param>
        /// <param name="utterance">Utterance instance to save.</param>
        public static void Save(string filePath, TtsUtterance utterance)
        {
            if (utterance == null)
            {
                throw new ArgumentNullException("utterance");
            }

            if (utterance.Script == null)
            {
                throw new ArgumentException("utterance.Script is null");
            }

            if (utterance.Script.Units == null)
            {
                throw new ArgumentException("utterance.Script.Units is null");
            }

            Save(filePath, utterance, 0, utterance.Script.Units.Count);
        }

        /// <summary>
        /// Save the utterance in Mulan TTS log formated text file.
        /// </summary>
        /// <param name="filePath">Target file path to save utterance.</param>
        /// <param name="utterance">Utterance instance to save.</param>
        /// <param name="minUnitIndex">Minimum unit index to start to save.</param>
        /// <param name="maxUnitIndex">Maximum unit index to stop from saving.</param>
        public static void Save(string filePath, TtsUtterance utterance,
            int minUnitIndex, int maxUnitIndex)
        {
            if (utterance == null)
            {
                throw new ArgumentNullException("utterance");
            }

            using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.Unicode))
            {
                sw.WriteLine("<NormText> " + utterance.TNedText);
                if (utterance.Script != null && utterance.Script.Units != null)
                {
                    sw.WriteLine("<UnitVector>");
                    for (int i = minUnitIndex; i < maxUnitIndex; i++)
                    {
                        TtsUnit unit = utterance.Script.Units[i];
                        string line = string.Format(CultureInfo.InvariantCulture,
                            "{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} 0 0 {10} {11} 0 0 0 100 0 0 0 0 {12}",
                            (int)unit.Feature.PosInSentence,
                            (int)unit.Feature.PosInWord,
                            (int)unit.Feature.PosInSyllable,
                            (int)unit.Feature.LeftContextPhone,
                            (int)unit.Feature.RightContextPhone,
                            (int)unit.Feature.LeftContextTone,
                            (int)unit.Feature.RightContextTone,
                            (int)unit.Feature.TtsStress,
                            (int)unit.Feature.TtsEmphasis,
                            (int)unit.Feature.TtsWordTone,

                            // break level, punctuation
                            Localor.MapLanguageId(unit.MetaUnit.Language),
                            unit.MetaUnit.Id,

                            // left context feature
                            // right context feature
                            // control flag
                            // volumn
                            // rate
                            // pitch
                            // voice
                            // user break
                            unit.MetaUnit.Name);
                        sw.WriteLine(line);

                        if (unit.TtsBreak == TtsBreak.Sentence)
                        {
                            sw.WriteLine("7 0 0 14 2 0 0 0 1 {0} 0 {1} 0 0 0 0 0 0 0 0 750 -SIL-",
                                (int)unit.TtsBreak, (int)unit.Language);
                        }
                        else if (unit.TtsBreak == TtsBreak.IntonationPhrase)
                        {
                            sw.WriteLine("7 0 0 14 2 0 0 0 1 {0} 0 {1} 0 0 0 0 0 0 0 0 400 -SIL-",
                                (int)unit.TtsBreak, (int)unit.Language);
                        }
                    }

                    sw.Write("\r\n");
                }

                sw.WriteLine("<WaveUnitSel>");
                int index = 0;
                if (utterance.Viterbi != null
                    && utterance.Viterbi.SelectedRoute != null
                    && utterance.Viterbi.CostNodeClusters != null
                    && utterance.Viterbi.SelectedRoute.CostNodes != null)
                {
                    for (int i = minUnitIndex; i < maxUnitIndex; i++)
                    {
                        CostNode node = utterance.Viterbi.SelectedRoute.CostNodes[i];
                        CostNodeCluster cluter = utterance.Viterbi.CostNodeClusters[i];
                        System.Diagnostics.Debug.Assert(cluter != null);
                        if (cluter == null)
                        {
                            continue;
                        }

                        TtsUnit unit = cluter.TtsUnit;
                        System.Diagnostics.Debug.Assert(unit != null, "unit should not be null here");

                        index = SaveUnit(sw, index, unit, node);
                    }
                }
            }
        }

        #endregion

        #region Help functions

        /// <summary>
        /// Post read processing.
        /// </summary>
        /// <param name="utterance">Utterance to handle.</param>
        private static void PostRead(TtsUtterance utterance)
        {
            if (utterance.Viterbi != null)
            {
                utterance.Viterbi.SortNodeRoutes();
                utterance.Viterbi.SelectedRoute =
                    utterance.Viterbi.FindRoute(utterance.WaveUnits);
            }
        }

        /// <summary>
        /// Save information of one unit into log file.
        /// </summary>
        /// <param name="writer">Stream writer to save the information.</param>
        /// <param name="index">Index of the unit to save.</param>
        /// <param name="expectedUnit">Expected unit from front-end.</param>
        /// <param name="selectedNode">Selected node through unit selection.</param>
        /// <returns>Next unit index.</returns>
        private static int SaveUnit(StreamWriter writer, int index,
            TtsUnit expectedUnit, CostNode selectedNode)
        {
            writer.Write(index.ToString(CultureInfo.InvariantCulture) + " ");
            ++index;
            writer.Write(index.ToString(CultureInfo.InvariantCulture) + " ");
            writer.Write(expectedUnit.MetaUnit.Id.ToString(CultureInfo.InvariantCulture) + " ");
            writer.Write(selectedNode.WaveUnit.SampleOffset.ToString(CultureInfo.InvariantCulture) + " ");
            writer.Write(selectedNode.WaveUnit.SampleLength.ToString(CultureInfo.InvariantCulture) + " ");

            TtsUnitFeature selFea = selectedNode.WaveUnit.Features;
            if (selFea != null)
            {
                StringBuilder builder = new StringBuilder();

                builder.AppendFormat(CultureInfo.InvariantCulture,
                    "{0}/ {1} ",
                    (int)selFea.PosInSentence, (int)expectedUnit.Feature.PosInSentence);
                builder.AppendFormat(CultureInfo.InvariantCulture,
                    "{0}/{1} ",
                    (int)selFea.PosInWord, (int)expectedUnit.Feature.PosInWord);
                builder.AppendFormat(CultureInfo.InvariantCulture,
                    "{0}/{1} ",
                    (int)selFea.PosInSyllable, (int)expectedUnit.Feature.PosInSyllable);

                builder.AppendFormat(CultureInfo.InvariantCulture,
                    "{0}/{1} ",
                    (int)selFea.LeftContextPhone, (int)expectedUnit.Feature.LeftContextPhone);
                builder.AppendFormat(CultureInfo.InvariantCulture,
                    "{0}/{1} ",
                    (int)selFea.RightContextPhone, (int)expectedUnit.Feature.RightContextPhone);

                builder.AppendFormat(CultureInfo.InvariantCulture,
                    "{0}/{1} ",
                    (int)selFea.LeftContextTone, (int)expectedUnit.Feature.LeftContextTone);
                builder.AppendFormat(CultureInfo.InvariantCulture,
                    "{0}/{1} ",
                    (int)selFea.RightContextTone, (int)expectedUnit.Feature.RightContextTone);

                builder.AppendFormat(CultureInfo.InvariantCulture,
                    "{0}/ {1} ",
                    (int)selFea.TtsStress, (int)expectedUnit.Feature.TtsStress);
                builder.AppendFormat(CultureInfo.InvariantCulture,
                    "{0}/{1} ",
                    (int)selFea.TtsEmphasis, (int)expectedUnit.Feature.TtsEmphasis);

                builder.AppendFormat(CultureInfo.InvariantCulture,
                    "{0}/{1} ",
                    (int)selFea.TtsWordTone, (int)expectedUnit.Feature.TtsWordTone);

                writer.Write(builder.ToString());
            }

            writer.Write("\r\n");
            return index;
        }

        /// <summary>
        /// Tell whether this line start a new tag section.
        /// Tag format:
        ///     <![CDATA[ <TagName> ]]>.
        /// </summary>
        /// <param name="line">Line to test.</param>
        /// <returns>Boolean.</returns>
        private static bool IsTag(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return false;
            }

            Match m = Regex.Match(line, @"\<.*\>");
            return m.Success;
        }

        /// <summary>
        /// Move and find the next tag section.
        /// </summary>
        /// <param name="tr">Text reader to read line from.</param>
        /// <returns>Not null, a new tag found; else end.</returns>
        private static string MoveToNextTag(TextReader tr)
        {
            string line = null;
            while ((line = tr.ReadLine()) != null)
            {
                if (IsTag(line))
                {
                    return line;
                }
            }

            return line;
        }

        #endregion

        #region Handle tags, naming pattern "HandleTag[tag name]"

        /// <summary>
        /// Handle Tag.NormText.
        /// </summary>
        /// <param name="utterance">Utterance to fill in.</param>
        /// <param name="line">Section starting line.</param>
        /// <param name="tr">Text data.</param>
        /// <returns>Next tag, or null for end.</returns>
        private static string HandleTagNormText(TtsUtterance utterance,
            string line, TextReader tr)
        {
            System.Diagnostics.Debug.Assert(ParseTag(line) == Tag.NormText);
            Match m = Regex.Match(line, @"\>(.*)");
            System.Diagnostics.Debug.Assert(m.Success);
            utterance.RawText = m.Groups[1].Value.Trim();
            utterance.TNedText = m.Groups[1].Value.Trim();

            if (string.IsNullOrEmpty(utterance.RawText))
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "nomalized text of utterence should not be empty.");
                throw new InvalidDataException(message);
            }

            return MoveToNextTag(tr);
        }

        /// <summary>
        /// Handle Tag.BreakAndEmph.
        /// </summary>
        /// <param name="utterance">Utterance to fill in.</param>
        /// <param name="line">Section starting line.</param>
        /// <param name="tr">Text data.</param>
        /// <returns>Next tag, or null for end.</returns>
        private static string HandleTagBreakAndEmph(TtsUtterance utterance,
            string line, TextReader tr)
        {
            System.Diagnostics.Debug.Assert(ParseTag(line) == Tag.BreakAndEmph);
            Match m = Regex.Match(line, @"\>(.*)");
            System.Diagnostics.Debug.Assert(m.Success);
            utterance.Script.Sentence = m.Groups[1].Value;
            return MoveToNextTag(tr);
        }

        /// <summary>
        /// Handle Tag.Pronun.
        /// </summary>
        /// <param name="utterance">Utterance to fill in.</param>
        /// <param name="line">Section starting line.</param>
        /// <param name="tr">Text data.</param>
        /// <returns>Next tag, or null for end.</returns>
        private static string HandleTagPronun(TtsUtterance utterance,
            string line, TextReader tr)
        {
            System.Diagnostics.Debug.Assert(ParseTag(line) == Tag.Pronun);
            Match m = Regex.Match(line, @"\>(.*)");
            System.Diagnostics.Debug.Assert(m.Success);
            string content = m.Groups[1].Value;
            utterance.Script.Pronunciation = content;

            return MoveToNextTag(tr);
        }

        /// <summary>
        /// Handle Tag.UnitVector.
        /// </summary>
        /// <param name="utterance">Utterance to fill in.</param>
        /// <param name="line">Section starting line.</param>
        /// <param name="tr">Text data.</param>
        /// <returns>Next tag, or null for end.</returns>
        private static string HandleTagUnitVector(TtsUtterance utterance,
            string line, TextReader tr)
        {
            System.Diagnostics.Debug.Assert(ParseTag(line) == Tag.UnitVector);
            utterance.Script.Units.Clear();

            while ((line = tr.ReadLine()) != null)
            {
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                if (IsTag(line))
                {
                    return line;
                }

                TtsUnit unit = ParseTtsUnit(line, utterance.Script.Language);
                if (string.Compare(unit.MetaUnit.Name, "_sil_", StringComparison.OrdinalIgnoreCase) == 0 ||
                    string.Compare(unit.MetaUnit.Name, "-sil-", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    // skip it
                }
                else
                {
                    utterance.Script.Units.Add(unit);
                }
            }

            return MoveToNextTag(tr);
        }

        /// <summary>
        /// Handle Tag.CandidateDump.
        /// </summary>
        /// <param name="utterance">Utterance to fill in.</param>
        /// <param name="line">Section starting line.</param>
        /// <param name="tr">Text data.</param>
        /// <returns>Next tag, or null for end.</returns>
        private static string HandleTagCandidateDump(TtsUtterance utterance,
            string line, TextReader tr)
        {
            Debug.Assert(ParseTag(line) == Tag.CandidateDump);
            while ((line = tr.ReadLine()) != null)
            {
            DO_WITH_CANDIDATE:

                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                if (IsTag(line))
                {
                    return line;
                }

                if (line.StartsWith("candidate", StringComparison.Ordinal))
                {
                    CostNodeCluster cluster = new CostNodeCluster();

                    string[] items = line.Split(new char[] { ' ' },
                        StringSplitOptions.RemoveEmptyEntries);
                    cluster.Index = int.Parse(items[1], CultureInfo.InvariantCulture);
                    cluster.TtsUnit = utterance.Script.Units[cluster.Index];
                    while ((line = tr.ReadLine()) != null)
                    {
                        if (string.IsNullOrEmpty(line))
                        {
                            continue;
                        }

                        if (line.StartsWith("unit", StringComparison.Ordinal))
                        {
                            CostNode node = ParseCostNode(line);
                            node.ClusterIndex = cluster.Index;
                            cluster.AddNode(node);
                            continue;
                        }

                        break;
                    }

                    utterance.Viterbi.CostNodeClusters.Add(cluster);

                    goto DO_WITH_CANDIDATE;
                }
            }

            return MoveToNextTag(tr);
        }

        /// <summary>
        /// Handle Tag.RouteDump.
        /// </summary>
        /// <param name="utterance">Utterance to fill in.</param>
        /// <param name="line">Section starting line.</param>
        /// <param name="tr">Text data.</param>
        /// <returns>Next tag, or null for end.</returns>
        private static string HandleTagRouteDump(TtsUtterance utterance,
            string line, TextReader tr)
        {
            System.Diagnostics.Debug.Assert(ParseTag(line) == Tag.RouteDump);
            while ((line = tr.ReadLine()) != null)
            {
            DO_WITH_CANDIDATE:

                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                if (IsTag(line))
                {
                    return line;
                }

                if (line.StartsWith("route", StringComparison.Ordinal))
                {
                    NodeRoute route = new NodeRoute();

                    string[] items = line.Split(new char[] { ' ' },
                        StringSplitOptions.RemoveEmptyEntries);
                    route.Index = int.Parse(items[1], CultureInfo.InvariantCulture);
                    while ((line = tr.ReadLine()) != null)
                    {
                        if (string.IsNullOrEmpty(line))
                        {
                            continue;
                        }

                        if (line.StartsWith("unit", StringComparison.Ordinal))
                        {
                            CostNode tempnode = ParseCostNode(line);
                            CostNodeCluster cluster =
                                utterance.Viterbi.CostNodeClusters[tempnode.Index];
                            CostNode node = cluster.IndexedNodes[tempnode.Key];
                            route.CostNodes.Add(node);
                            continue;
                        }

                        break;
                    }

                    route.ReverseCostNodes();

                    // route.CostNodes.Sort();
                    utterance.Viterbi.NodeRoutes.Add(route);

                    goto DO_WITH_CANDIDATE;
                }
            }

            return MoveToNextTag(tr);
        }

        /// <summary>
        /// Handle Tag.WaveUnitSel.
        /// </summary>
        /// <param name="utterance">Utterance to fill in.</param>
        /// <param name="line">Section starting line.</param>
        /// <param name="tr">Text data.</param>
        /// <returns>Next tag, or null for end.</returns>
        private static string HandleTagWaveUnitSel(TtsUtterance utterance,
            string line, TextReader tr)
        {
            Debug.Assert(ParseTag(line) == Tag.WaveUnitSel);

            while ((line = tr.ReadLine()) != null)
            {
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                if (IsTag(line))
                {
                    break;
                }

                WaveUnit unit = ParseWaveUnitForWaveUnitSel(line);
                utterance.WaveUnits.Add(unit);
            }

            if (IsTag(line))
            {
                // this should be next tag
                return line;
            }
            else
            {
                return MoveToNextTag(tr);
            }
        }

        #endregion

        #region Detailed parsing

        /// <summary>
        /// Parse for tag type from line.
        /// </summary>
        /// <param name="line">Line to parse.</param>
        /// <returns>Tag type.</returns>
        private static Tag ParseTag(string line)
        {
            Match m = Regex.Match(line, @"\<(.*)\>");
            System.Diagnostics.Debug.Assert(m.Success);

            Tag tag;
            try
            {
                tag = (Tag)Enum.Parse(typeof(Tag), m.Groups[1].Value);
            }
            catch (ArgumentException)
            {
                tag = Tag.Unknown;
            }

            return tag;
        }

        /// <summary>
        /// Parse cost node string.
        /// </summary>
        /// <example>
        ///     Unit 3 1201786:1259 0 3 2 27 19 0 0 1 0 1.388950 5.729300.
        /// </example>
        /// <param name="line">Line to parse.</param>
        /// <returns>CostNode.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1631:DocumentationMustMeetCharacterPercentage", Justification = "Reviewed.")]
        private static CostNode ParseCostNode(string line)
        {
            CostNode node = new CostNode();

            string[] items = line.Split(new char[] { ' ' },
                StringSplitOptions.RemoveEmptyEntries);
            System.Diagnostics.Debug.Assert(items.Length == 14 || items.Length == 5 || items.Length == 3);
            node.Index = int.Parse(items[1], CultureInfo.InvariantCulture);

            node.WaveUnit = ParseWaveUnitForViterbi(items);
            if (items.Length > 3)
            {
                node.TargetCost = float.Parse(items[items.Length - 2], CultureInfo.InvariantCulture);
                node.RouteCost = float.Parse(items[items.Length - 1], CultureInfo.InvariantCulture);
            }

            return node;
        }

        /// <summary>
        /// Parse WaveUnit for WaveUnitSel section data.
        /// </summary>
        /// <example>
        ///    0    0         -1 80
        ///    1  217   33113977 3934 10/ 3 0/0 2/2 17/ 0 38/38 0/0 0/0 1/1 0/0
        ///    2    0         -1 12000.
        /// </example>
        /// <param name="line">Line to parse.</param>
        /// <returns>Wave unit.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1631:DocumentationMustMeetCharacterPercentage", Justification = "Reviewed.")]
        private static WaveUnit ParseWaveUnitForWaveUnitSel(string line)
        {
            // from Mulan dump

            // remove desired values
            line = Regex.Replace(line, @"/ *\d", string.Empty);

            string[] items = line.Split(new char[] { ' ' },
                                    StringSplitOptions.RemoveEmptyEntries);

            WaveUnit item = new WaveUnit();
            if (items[2] == "-1")
            {
                item.Name = Phoneme.SilencePhone;
                item.SampleLength = long.Parse(items[3], CultureInfo.InvariantCulture);
            }
            else
            {
                item.SampleOffset = long.Parse(items[2], CultureInfo.InvariantCulture);
                item.SampleLength = long.Parse(items[3], CultureInfo.InvariantCulture);

                if (WaveUnit.VoiceFont != null)
                {
                    Dictionary<long, WaveUnit> wus = WaveUnit.VoiceFont.WaveUnits;
                    if (wus.ContainsKey(item.SampleOffset))
                    {
                        return wus[item.SampleOffset];
                    }
                    else
                    {
                        wus.Add(item.SampleOffset, item);
                    }
                }

                item.Features = new TtsUnitFeature();
                item.Features.Parse(items, 4);
            }

            return item;
        }

        /// <summary>
        /// Parse WaveUnit for Viterbi.
        /// </summary>
        /// <example>
        /// Unit 0 1633894:3966 0 3 2 31 1 0 0 1 0 1.636750 1.636750
        /// Or
        /// Unit 0 1633894:3966 1.636750 1.636750.
        /// </example>
        /// <param name="items">Data to parse.</param>
        /// <returns>WaveUnit.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1631:DocumentationMustMeetCharacterPercentage", Justification = "Reviewed.")]
        private static WaveUnit ParseWaveUnitForViterbi(string[] items)
        {
            // from Mulan dump
            string[] pos = items[2].Split(new char[] { ':' },
                StringSplitOptions.RemoveEmptyEntries);
            uint waveOffset = uint.Parse(pos[0], CultureInfo.InvariantCulture);
            uint waveLength = uint.Parse(pos[1], CultureInfo.InvariantCulture);

            WaveUnit item = new WaveUnit();
            item.SampleOffset = waveOffset;
            item.SampleLength = waveLength;
            if (WaveUnit.VoiceFont != null)
            {
                Dictionary<long, WaveUnit> wus = WaveUnit.VoiceFont.WaveUnits;

                if (wus.ContainsKey(item.SampleOffset))
                {
                    return wus[item.SampleOffset];
                }
                else
                {
                    wus.Add(item.SampleOffset, item);
                }
            }

            if (items.Length == 13)
            {
                item.Features = new TtsUnitFeature();
                item.Features.Parse(items, 3);
            }

            return item;
        }

        /// <summary>
        /// Parse TtsUnit .
        /// </summary>
        /// <example>
        /// 10  0  2 17 38  0  0  1  0  1  1 1033  217 33554870      438  3 100  0  0  0  0 w+eh+l
        /// 10  2  0 21  8  0  0  0  0  0  1 1033   10  8412674  8412674  0 100  0  0  0  0 k
        /// 10  2  2 38 17  0  0  0  0  5  1 1033   79       61  8423798  0 100  0  0  0  0 ax+m
        /// 0  0  0  0  0  0  0  0  0  5  0    0    0        0        0  0 100  0  0  0 750 _sil_.
        /// </example>
        /// <param name="line">Line to parse.</param>
        /// <param name="language">Language of the unit to build.</param>
        /// <returns>TtsUnit.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1631:DocumentationMustMeetCharacterPercentage", Justification = "Reviewed.")]
        private static TtsUnit ParseTtsUnit(string line, Language language)
        {
            TtsUnit unit = new TtsUnit(language);
            string[] items = line.Split(new char[] { ' ' },
                StringSplitOptions.RemoveEmptyEntries);
            unit.Feature = new TtsUnitFeature();
            unit.Feature.Parse(items, 0);
            unit.MetaUnit = ParseMetaUnitForViterbi(line);

            return unit;
        }

        /// <summary>
        /// Parse MetaUnit for Viterbi.
        /// </summary>
        /// <example>
        /// 10  0  2 17 38  0  0  1  0  1  1 1033  217 33554870      438  3 100  0  0  0  0 w+eh+l.
        /// </example>
        /// <param name="line">Unit feature string line.</param>
        /// <returns>TtsMetaUnit.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1631:DocumentationMustMeetCharacterPercentage", Justification = "Reviewed.")]
        private static TtsMetaUnit ParseMetaUnitForViterbi(string line)
        {
            string[] items = line.Split(new char[] { ' ' },
                StringSplitOptions.RemoveEmptyEntries);
            System.Diagnostics.Debug.Assert(items.Length > 13);

            int languageValue = int.Parse(items[12], CultureInfo.InvariantCulture);
            int unitId = int.Parse(items[13], CultureInfo.InvariantCulture);

            TtsMetaUnit mu = new TtsMetaUnit(Localor.MapLanguageId(languageValue));
            mu.Id = unitId;
            mu.Name = items[items.Length - 1];

            return mu;
        }

        #endregion
    }
}