// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ScaleFeatureJob.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Scale feature used for when cosmos enabled.
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
    /// Scale feature.
    /// </summary>
    [CLSCompliantAttribute(false)]
    public class ScaleFeatureJob : JobProcessor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScaleFeatureJob" /> class.
        /// </summary>
        /// <param name="cosmosPath">Cosmos path.</param>
        public ScaleFeatureJob(string cosmosPath)
            : this()
        {
            this.Job.ReplaceVariable["COSMOSDLL"] = TmocPath.Combine(cosmosPath, this.Job.ReplaceVariable["COSMOSDLL"]);
            this.Job.ReplaceVariable["OFFLINEDLL"] = TmocPath.Combine(cosmosPath, this.Job.ReplaceVariable["OFFLINEDLL"]);
            this.Job.ReplaceVariable["SVMSCALETOOL"] = TmocPath.Combine(cosmosPath, this.Job.ReplaceVariable["SVMSCALETOOL"]);
            this.Job.ReplaceVariable["SVMRANGE"] = TmocPath.Combine(cosmosPath, this.Job.ReplaceVariable["SVMRANGE"]);
            this.Job.ReplaceVariable["WAVEDATACORPUS"] = TmocPath.Combine(cosmosPath, this.Job.ReplaceVariable["WAVEDATACORPUS"]);
            this.Job.ReplaceVariable["WAVEDATACORPUSWITHF0SSVM"] = TmocPath.Combine(cosmosPath, this.Job.ReplaceVariable["WAVEDATACORPUSWITHF0SSVM"]);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScaleFeatureJob" /> class.
        /// </summary>
        public ScaleFeatureJob()
        {
            this.Job.ResourceScript = "Microsoft.Tts.Cosmos.Handler.ScaleFeatureJob";
            this.Job.ReplaceVariable = new Dictionary<string, string>
            {
                { "COSMOSDLL", @"Tools/Microsoft.Tts.Cosmos.dll" },
                { "OFFLINEDLL", @"Tools/Microsoft.Tts.Offline.dll" },
                { "SVMSCALETOOL", @"Tools/SVM/svm-scale.exe" },
                { "SVMRANGE", @"Data/SVM_all.range" },
                { "WAVEDATACORPUS", "Data/InputSVM.ss" },
                { "WAVEDATACORPUSWITHF0SSVM", @"Data/InputSSVM.ss" },
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
            string svmDir = "svm";
            string scaledSVMDir = "scaledSVM";
            Directory.CreateDirectory(svmDir);
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
                string svmFile = JobBase.GenerateLocalFile(waveId, row["SVM"].String, FileExtensions.Text, false, svmDir);
                string scaledSVMFile = Path.Combine(scaledSVMDir, waveId + "." + FileExtensions.Text);
                string svmRangeFile = Path.GetFileName(this.Job.ReplaceVariable["SVMRANGE"]);

                // File.WriteAllText(svmRangeFile, TmocFile.NormalizeOutput(svmRangeFile)).
                string argument = Helper.NeutralFormat(" -r \"{0}\" \"{1}\"", svmRangeFile, svmFile);
                DelayedLogger logger = new DelayedLogger(new TextLogger(scaledSVMFile));
                CommandLine.RunCommand(Path.GetFileName(this.Job.ReplaceVariable["SVMSCALETOOL"]),
                    argument, Environment.CurrentDirectory, logger.Writer, logger.Writer, null);
                logger.Dispose();
                outputRow["SSVM"].Set(File.ReadAllText(scaledSVMFile));

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
            ScopeRuntime.Schema outputSchema = new ScopeRuntime.Schema("WaveID:string, WaveBinary:byte[], WaveAlignments:string, RawF0:string, LPCC:byte[], OF0:string, LSP:byte[], Pow:string, MBE:string, SSVM:string");
            return outputSchema;
        }
        #endregion
    }
}
