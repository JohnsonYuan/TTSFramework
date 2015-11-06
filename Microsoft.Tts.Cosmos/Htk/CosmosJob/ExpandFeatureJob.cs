// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExpandFeatureJob.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//   Expand feature job, used for when cosmos enabled.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Tts.Cosmos.Htk
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Microsoft.Tts.Cosmos.TMOC;
    using Microsoft.Tts.Offline.Utility;
    using ScopeRuntime;

    /// <summary>
    /// Expand feature.
    /// </summary>
    [CLSCompliant(false)]
    public class ExpandFeatureJob : JobProcessor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExpandFeatureJob" /> class.
        /// </summary>
        /// <param name="cosmosPath">The path of cosmos.</param>
        /// <param name="dimension">Dimension.</param>
        public ExpandFeatureJob(string cosmosPath, int dimension)
            : this()
        {
            this.Job.ReplaceVariable["COSMOSDLL"] = TmocPath.Combine(cosmosPath, this.Job.ReplaceVariable["COSMOSDLL"]);
            this.Job.ReplaceVariable["OFFLINEDLL"] = TmocPath.Combine(cosmosPath, this.Job.ReplaceVariable["OFFLINEDLL"]);
            this.Job.ReplaceVariable["WAVEDATACORPUS"] = TmocPath.Combine(cosmosPath, this.Job.ReplaceVariable["WAVEDATACORPUS"]);
            this.Job.ReplaceVariable["WAVEDATACORPUSWITHF0EXP"] = TmocPath.Combine(cosmosPath, this.Job.ReplaceVariable["WAVEDATACORPUSWITHF0EXP"]);
            this.Job.ReplaceVariable["DIMENSION"] = dimension.ToString();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpandFeatureJob" /> class.
        /// </summary>
        public ExpandFeatureJob()
        {
            this.Job.ResourceScript = "Microsoft.Tts.Cosmos.Handler.ExpandFeatureJob";
            this.Job.ReplaceVariable = new Dictionary<string, string>
            {
                { "COSMOSDLL", @"Tools/Microsoft.Tts.Cosmos.dll" },
                { "OFFLINEDLL", @"Tools/Microsoft.Tts.Offline.dll" },
                { "WAVEDATACORPUS", "Data/InputMERG.ss" },
                { "WAVEDATACORPUSWITHF0EXP", @"Data/InputEXP.ss" },
                { "DIMENSION", string.Empty },
            };
        }

        /// <summary>
        /// Generate tempalte.
        /// </summary>
        /// <returns>The template.</returns>
        public string GenerateTemplate()
        {
            return this.Job.GenerateTemplate();
        }

        #region Scope Process
        /// <summary>
        /// Main processing script.
        /// </summary>
        /// <param name="input">The input row.</param>
        /// <param name="outputRow">The output row.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>The IEnumerable output row.</returns>
        public override IEnumerable<Row> Process(RowSet input, Row outputRow, string[] args)
        {
            int dimension = int.Parse(args[0]);
            string mergedDir = "merged";
            string expandDir = "expand";
            Directory.CreateDirectory(mergedDir);
            Directory.CreateDirectory(expandDir);

            foreach (var row in input.Rows)
            {
                outputRow["WaveID"].Set(row["WaveID"].String);
                outputRow["WaveBinary"].Set(row["WaveBinary"].Binary);
                outputRow["WaveAlignments"].Set(row["WaveAlignments"].String);
                outputRow["RawF0"].Set(row["RawF0"].String);
                outputRow["LPCC"].Set(row["LPCC"].Binary);
                outputRow["OF0"].Set(row["OF0"].String);
                outputRow["LSP"].Set(row["LSP"].Binary);
                outputRow["Pow"].Set(row["Pow"].String);
                outputRow["MBE"].Set(row["MBE"].String);

                string waveId = row["WaveID"].String;
                string mergedFeatureFile = JobBase.GenerateLocalFile(waveId, row["MERG"].String, FileExtensions.Text, false, mergedDir);
                string expandFeatureFile = Path.Combine(expandDir, waveId + "." + FileExtensions.Text);

                string[] argument = { mergedFeatureFile, expandFeatureFile, dimension.ToString() };
                F0ExtractorCOSMOS.ExpandFeaturesOneFile(argument, null);

                outputRow["EXP"].Set(File.ReadAllText(expandFeatureFile));
                yield return outputRow;
            }
        }

        /// <summary>
        /// Schema of output.
        /// </summary>
        /// <param name="requestedColumns">The requested columns.</param>
        /// <param name="args">The arguments.</param>
        /// <param name="inputSchema">The input schema.</param>
        /// <returns>The output schema.</returns>
        [CLSCompliant(false)]
        public override ScopeRuntime.Schema Produces(string[] requestedColumns, string[] args, ScopeRuntime.Schema inputSchema)
        {
            ScopeRuntime.Schema outputSchema = new ScopeRuntime.Schema("WaveID:string, WaveBinary:byte[], WaveAlignments:string, RawF0:string, LPCC:byte[], OF0:string, LSP:byte[], Pow:string, MBE:string, EXP:string");
            return outputSchema;
        }
        #endregion
    }
}
