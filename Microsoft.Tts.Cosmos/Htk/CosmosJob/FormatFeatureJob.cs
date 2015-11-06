// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FormatFeatureJob.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//   Format feature job, used for when cosmos enabled.
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
    /// Format feature job.
    /// </summary>
    [CLSCompliant(false)]
    public class FormatFeatureJob : JobProcessor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FormatFeatureJob" /> class.
        /// </summary>
        /// <param name="cosmosPath">Cosmos path.</param>
        public FormatFeatureJob(string cosmosPath)
            : this()
        {
            this.Job.ReplaceVariable["COSMOSDLL"] = TmocPath.Combine(cosmosPath, this.Job.ReplaceVariable["COSMOSDLL"]);
            this.Job.ReplaceVariable["OFFLINEDLL"] = TmocPath.Combine(cosmosPath, this.Job.ReplaceVariable["OFFLINEDLL"]);
            this.Job.ReplaceVariable["WAVEDATACORPUS"] = TmocPath.Combine(cosmosPath, this.Job.ReplaceVariable["WAVEDATACORPUS"]);
            this.Job.ReplaceVariable["WAVEDATACORPUSWITHF0SVM"] = TmocPath.Combine(cosmosPath, this.Job.ReplaceVariable["WAVEDATACORPUSWITHF0SVM"]);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FormatFeatureJob" /> class.
        /// </summary>
        public FormatFeatureJob()
        {
            this.Job.ResourceScript = "Microsoft.Tts.Cosmos.Handler.FormatFeatureJob";
            this.Job.ReplaceVariable = new Dictionary<string, string>
            {
                { "COSMOSDLL", @"Tools/Microsoft.Tts.Cosmos.dll" },
                { "OFFLINEDLL", @"Tools/Microsoft.Tts.Offline.dll" },
                { "WAVEDATACORPUS", "Data/InputEXP.ss" },
                { "WAVEDATACORPUSWITHF0SVM", @"Data/InputSVM.ss" },
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
            string f0Dir = "f0";
            string expandDir = "expand";
            string svmDir = "svm";
            Directory.CreateDirectory(f0Dir);
            Directory.CreateDirectory(expandDir);
            Directory.CreateDirectory(svmDir);

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
                string f0File = JobBase.GenerateLocalFile(waveId, row["RawF0"].String, FileExtensions.F0File, true, f0Dir);
                string expandFeatureFile = JobBase.GenerateLocalFile(waveId, row["EXP"].String, FileExtensions.Text, false, expandDir);
                string svmFile = Path.Combine(svmDir, waveId + "." + FileExtensions.Text);

                string[] argument = { f0File, expandFeatureFile, svmFile };
                F0ExtractorCOSMOS.FormatFeaturesOneFile(argument, null);

                outputRow["SVM"].Set(File.ReadAllText(svmFile));
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
            ScopeRuntime.Schema outputSchema = new ScopeRuntime.Schema("WaveID:string, WaveBinary:byte[], WaveAlignments:string, RawF0:string, LPCC:byte[], OF0:string, LSP:byte[], Pow:string, MBE:string, SVM:string");
            return outputSchema;
        }

        #endregion
    }
}
