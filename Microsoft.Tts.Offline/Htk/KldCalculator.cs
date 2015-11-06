//----------------------------------------------------------------------------
// <copyright file="KldCalculator.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module defines a helper class to facilitator the hkld usage
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Htk
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Security.Permissions;
    using System.Text;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Groups KLD table calculation inputs.
    /// </summary>
    public class KldCalculator
    {
        /// <summary>
        /// Gets or sets the intermediate path
        /// Input of this handler.
        /// </summary>
        public string IntermediatePath
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the hkld tool
        /// Input of this handler.
        /// </summary>
        public string HKldTool
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the configuration file for external Htk tool.
        /// Input of this handler.
        /// </summary>
        public string HtkToolConfigurationFile
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the file name of the full-context model from Hts.
        /// Input of this handler.
        /// </summary>
        public string FullContextModelFile
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the file name of the clustered full-context model from Hts.
        /// Input of this handler.
        /// </summary>
        public string ClusteredModelFile
        {
            get;
            set;
        }

        /// <summary>
        /// Process pairs of candidate group and leaf nodes for KLD calculation.
        /// Three steps are performed:
        /// 1. Prepare HKLD input.
        /// 2. Call HKLD for KLD calculation.
        /// 3. save kld result into a float array.
        /// </summary>
        /// <param name="pairs">Array to contain the two model names.</param>
        /// <param name="name">The id of this part.</param>
        /// <param name="logWriter">The log writer.</param>
        /// <returns>Kld result array.</returns>
        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        public float[] CalculateKld(Pair<string, string>[] pairs, string name, TextWriter logWriter)
        {
            string scriptFile = Path.Combine(
                IntermediatePath,
                string.Format(CultureInfo.InvariantCulture, "script.{0}", name));

            string fullContextHmmList = Path.Combine(
                IntermediatePath,
                string.Format(CultureInfo.InvariantCulture, "hmmList.fullcontext.{0}", name));

            string clusteredHmmList = Path.Combine(
                IntermediatePath,
                string.Format(CultureInfo.InvariantCulture, "hmmList.clustered.{0}", name));

            string outputFile = Path.Combine(
                IntermediatePath,
                string.Format(CultureInfo.InvariantCulture, "kld.{0}", name));

            PrepareHKldInput(pairs, scriptFile, fullContextHmmList, clusteredHmmList);

            RunHkld(scriptFile, outputFile, fullContextHmmList, clusteredHmmList, logWriter);

            var ret = CollectKldTables(pairs, outputFile, logWriter);

            File.Delete(outputFile);
            File.Delete(scriptFile);

            return ret;
        }

        /// <summary>
        /// Prepare input files for HKLD.
        /// </summary>
        /// <param name="pairs">Candidate group and leaf nodes pairs.</param>
        /// <param name="scriptFile">The HKLD script file to build.</param>
        /// <param name="fullContextHmmListFile">The full context HMM list file to build.</param>
        /// <param name="clusteredHmmListFile">The clustered HMM list file to build.</param>
        private static void PrepareHKldInput(IEnumerable<Pair<string, string>> pairs,
            string scriptFile,
            string fullContextHmmListFile,
            string clusteredHmmListFile)
        {
            Debug.Assert(pairs.Count() > 0, "No items for KLD calculation");

            PrepareHKldScript(pairs, scriptFile);

            PrepareHkldFullContextHmmList(pairs, fullContextHmmListFile);

            PrepareHkldClusteredHmmList(pairs, clusteredHmmListFile);
        }

        /// <summary>
        /// Prepare script file for HKLD.
        /// </summary>
        /// <param name="pairs">Candidate group and leaf nodes pairs.</param>
        /// <param name="scriptFile">The HKLD script file to build.</param>
        private static void PrepareHKldScript(
            IEnumerable<Pair<string, string>> pairs, string scriptFile)
        {
            using (StreamWriter scriptWriter = new StreamWriter(scriptFile))
            {
                foreach (Pair<string, string> item in pairs)
                {
                    scriptWriter.WriteLine(string.Format(CultureInfo.InvariantCulture, "~h {0} ~p {1}", item.Left, item.Right));
                }
            }
        }

        /// <summary>
        /// Prepare clustered HMM list for HKLD.
        /// </summary>
        /// <param name="pairs">Candidate group and leaf nodes pairs.</param>
        /// <param name="clusteredHmmListFile">The clustered HMM list file to build.</param>
        private static void PrepareHkldClusteredHmmList(
            IEnumerable<Pair<string, string>> pairs, string clusteredHmmListFile)
        {
            // The HMM list of clustered models isn't used.
            // But HTK doesn't allow empty list. So a single item list file is built
            using (StreamWriter clusteredHmmListWriter = new StreamWriter(clusteredHmmListFile))
            {
                clusteredHmmListWriter.WriteLine(TrimStateStreamInfo(pairs.First().Left));
            }
        }

        /// <summary>
        /// Prepare full context HMM list for HKLD.
        /// </summary>
        /// <param name="pairs">Candidate group and leaf nodes pairs.</param>
        /// <param name="fullContextHmmListFile">The full context HMM list file to build.</param>
        private static void PrepareHkldFullContextHmmList(
            IEnumerable<Pair<string, string>> pairs, string fullContextHmmListFile)
        {
            List<string> fullContextLabels = new List<string>();
            foreach (string fullContextLabel in pairs.Select(pair => pair.Left).Distinct())
            {
                fullContextLabels.Add(TrimStateStreamInfo(fullContextLabel));
            }

            File.WriteAllLines(fullContextHmmListFile, fullContextLabels.Distinct().ToArray());
        }

        /// <summary>
        /// Trim the state and stream info on given full context label.
        /// </summary>
        /// <param name="label">Given full context label.</param>
        /// <returns>The pure full context lable.</returns>
        private static string TrimStateStreamInfo(string label)
        {
            int index = Math.Min(label.IndexOf('['), label.IndexOf(@".stream"));

            if (index > 0)
            {
                label = label.Remove(index);
            }

            return label;
        }

        /// <summary>
        /// Save KLD cost tables from parsing HKLD output file.
        /// </summary>
        /// <param name="pairs">Candidate group and leaf nodes pairs.</param>
        /// <param name="hkldOutputFile">HKLD output file to parse.</param>
        /// <param name="logWriter">The log writer.</param>
        /// <returns>Kld result array .</returns>
        private float[] CollectKldTables(
            IEnumerable<Pair<string, string>> pairs,
            string hkldOutputFile, TextWriter logWriter)
        {
            List<float> kldResults = new List<float>();

            using (StreamReader reader = new StreamReader(hkldOutputFile))
            {
                foreach (Pair<string, string> item in pairs)
                {
                    string scriptLine = string.Format(CultureInfo.InvariantCulture, "~h {0} ~p {1}", item.Left, item.Right);
                    string kldResult = reader.ReadLine();

                    if (!kldResult.EndsWith(scriptLine))
                    {
                        throw new InvalidDataException(string.Format(CultureInfo.InvariantCulture, "Kld input/result mismatch, kld result \"{0}\"", kldResult));
                    }

                    string kldValueString = kldResult.Replace(" " + scriptLine, string.Empty);
                    kldResults.Add(float.Parse(kldValueString));
                }
            }

            return kldResults.ToArray();
        }

        /// <summary>
        /// Calculate KLD with HKLD.exe.
        /// </summary>
        /// <param name="scriptFile">The HKLD script file to build.</param>
        /// <param name="outputFile">The HKLD output file.</param>
        /// <param name="fullContextHmmListFile">The full context HMM list file to build.</param>
        /// <param name="clusteredHmmListFile">The clustered HMM list file to build.</param>
        /// <param name="logWriter">The log writer.</param>
        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        private void RunHkld(string scriptFile, string outputFile, string fullContextHmmListFile, string clusteredHmmListFile, TextWriter logWriter)
        {
            string argument = string.Format(
                    CultureInfo.InvariantCulture,
                    "-A -H \"{0}\" \"{1}\" -s \"{2}\" -o \"{3}\" -C \"{4}\" \"{5}\" \"{6}\"",
                    FullContextModelFile,
                    ClusteredModelFile,
                    scriptFile,
                    outputFile,
                    HtkToolConfigurationFile,
                    fullContextHmmListFile,
                    clusteredHmmListFile);

            int exitCode = CommandLine.RunCommand(
                HKldTool,
                argument,
                IntermediatePath,
                logWriter,
                logWriter,
                null);

            if (exitCode != ExitCode.NoError)
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "HKLD failed since input argument is wrong. exit code is {0}. See logs", exitCode));
            }
        }
    }
}