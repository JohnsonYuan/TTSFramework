// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MergeFeatureJob.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//   Merge feature job, used for when cosmos enabled.
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
    /// Merge feature.
    /// </summary>
    [CLSCompliantAttribute(false)]
    public class MergeFeatureJob : JobProcessor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MergeFeatureJob" /> class.
        /// </summary>
        /// <param name="cosmosPath">Cosmos path.</param>
        public MergeFeatureJob(string cosmosPath)
            : this()
        {
            this.Job.ReplaceVariable["COSMOSDLL"] = TmocPath.Combine(cosmosPath, this.Job.ReplaceVariable["COSMOSDLL"]);
            this.Job.ReplaceVariable["OFFLINEDLL"] = TmocPath.Combine(cosmosPath, this.Job.ReplaceVariable["OFFLINEDLL"]);
            this.Job.ReplaceVariable["WAVEDATACORPUS"] = TmocPath.Combine(cosmosPath, this.Job.ReplaceVariable["WAVEDATACORPUS"]);
            this.Job.ReplaceVariable["WAVEDATACORPUSWITHF0MERG"] = TmocPath.Combine(cosmosPath, this.Job.ReplaceVariable["WAVEDATACORPUSWITHF0MERG"]);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MergeFeatureJob" /> class.
        /// </summary>
        public MergeFeatureJob()
        {
            this.Job.ResourceScript = "Microsoft.Tts.Cosmos.Handler.MergeFeatureJob";
            this.Job.ReplaceVariable = new Dictionary<string, string>
            {
                { "COSMOSDLL", @"Tools/Microsoft.Tts.Cosmos.dll" },
                { "OFFLINEDLL", @"Tools/Microsoft.Tts.Offline.dll" },
                { "WAVEDATACORPUS", "Data/InputERR.ss" },
                { "WAVEDATACORPUSWITHF0MERG", @"Data/InputMERG.ss" },
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
            string relatedFeatureDir = "relatedFeature";
            string residualDir = "residual";
            string nccfDir = "nccf";
            string mergedDir = "merged";
            Directory.CreateDirectory(relatedFeatureDir);
            Directory.CreateDirectory(residualDir);
            Directory.CreateDirectory(nccfDir);
            Directory.CreateDirectory(mergedDir);

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
                string relatedFeatureFile = JobBase.GenerateLocalFile(waveId, row["RF"].String, FileExtensions.Text, false, relatedFeatureDir);
                string residualFile = JobBase.GenerateLocalFile(waveId, row["ERR"].String, FileExtensions.Text, false, residualDir);
                string nccfFile = JobBase.GenerateLocalFile(waveId, row["NCCF"].String, FileExtensions.F0File, true, nccfDir);
                string mergedFeatureFile = Path.Combine(mergedDir, waveId + "." + FileExtensions.Text);

                string[] argument = { relatedFeatureFile, residualFile, nccfFile, mergedFeatureFile };
                F0ExtractorCOSMOS.MergeFeaturesOneFile(argument, null);

                outputRow["MERG"].Set(File.ReadAllText(mergedFeatureFile));
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
        public override ScopeRuntime.Schema Produces(string[] requestedColumns, string[] args, ScopeRuntime.Schema inputSchema)
        {
            ScopeRuntime.Schema outputSchema = new ScopeRuntime.Schema("WaveID:string, WaveBinary:byte[], WaveAlignments:string, RawF0:string, LPCC:byte[], OF0:string, LSP:byte[], Pow:string, MBE:string, MERG:string");
            return outputSchema;
        }

        #endregion
    }
}
