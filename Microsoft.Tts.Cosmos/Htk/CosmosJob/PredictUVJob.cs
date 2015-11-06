// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PredictUVJob.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//   Predict UV job, used for when cosmos enabled.
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
    /// Predict UV.
    /// </summary>
    [CLSCompliantAttribute(false)]
    public class PredictUVJob : JobProcessor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PredictUVJob" /> class.
        /// </summary>
        /// <param name="cosmosPath">Cosmos path.</param>
        public PredictUVJob(string cosmosPath)
            : this()
        {
            this.Job.ReplaceVariable["COSMOSDLL"] = TmocPath.Combine(cosmosPath, this.Job.ReplaceVariable["COSMOSDLL"]);
            this.Job.ReplaceVariable["OFFLINEDLL"] = TmocPath.Combine(cosmosPath, this.Job.ReplaceVariable["OFFLINEDLL"]);
            this.Job.ReplaceVariable["SVMPREDICTTOOL"] = TmocPath.Combine(cosmosPath, this.Job.ReplaceVariable["SVMPREDICTTOOL"]);
            this.Job.ReplaceVariable["UVMODELFILE"] = TmocPath.Combine(cosmosPath, this.Job.ReplaceVariable["UVMODELFILE"]);
            this.Job.ReplaceVariable["WAVEDATACORPUS"] = TmocPath.Combine(cosmosPath, this.Job.ReplaceVariable["WAVEDATACORPUS"]);
            this.Job.ReplaceVariable["WAVEDATACORPUSWITHF0UV"] = TmocPath.Combine(cosmosPath, this.Job.ReplaceVariable["WAVEDATACORPUSWITHF0UV"]);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PredictUVJob" /> class.
        /// </summary>
        public PredictUVJob()
        {
            this.Job.ResourceScript = "Microsoft.Tts.Cosmos.Handler.PredictUVJob";
            this.Job.ReplaceVariable = new Dictionary<string, string>
            {
                { "COSMOSDLL", @"Tools/Microsoft.Tts.Cosmos.dll" },
                { "OFFLINEDLL", @"Tools/Microsoft.Tts.Offline.dll" },
                { "SVMPREDICTTOOL", @"Tools/SVM/svm-predict.exe" },
                { "UVMODELFILE", @"Tools/SVM/uv.model" },
                { "WAVEDATACORPUS", "Data/InputSSVM.ss" },
                { "WAVEDATACORPUSWITHF0UV", @"Data/InputUV.ss" },
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
            string uvDir = "uv";
            string scaledSVMDir = "scaledSVM";
            Directory.CreateDirectory(uvDir);
            Directory.CreateDirectory(scaledSVMDir);

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
                string scaledSVMFile = JobBase.GenerateLocalFile(waveId, row["SSVM"].String, FileExtensions.Text, false, scaledSVMDir);
                string uvFile = Path.Combine(uvDir, waveId + "." + FileExtensions.Text);
                string argument = Helper.NeutralFormat(" \"{0}\" \"{1}\" \"{2}\"", scaledSVMFile, Path.GetFileName(this.Job.ReplaceVariable["UVMODELFILE"]), uvFile);

                CommandLine.RunCommand(Path.GetFileName(this.Job.ReplaceVariable["SVMPREDICTTOOL"]),
                    argument, "./");
                outputRow["UV"].Set(JobBase.GetTextFile(uvFile));
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
            ScopeRuntime.Schema outputSchema = new ScopeRuntime.Schema("WaveID:string, WaveBinary:byte[], WaveAlignments:string, RawF0:string, LPCC:byte[], OF0:string, LSP:byte[], Pow:string, MBE:string, UV:string");
            return outputSchema;
        }
        #endregion
    }
}
