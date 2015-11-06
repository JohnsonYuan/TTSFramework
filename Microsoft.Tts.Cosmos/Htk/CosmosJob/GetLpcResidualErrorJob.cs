// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GetLpcResidualErrorJob.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//   Get Lpc Residual error job, used for when cosmos enabled.
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
    /// Get Lpc Residual error.
    /// </summary>
    [CLSCompliantAttribute(false)]
    public class GetLpcResidualErrorJob : JobProcessor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GetLpcResidualErrorJob" /> class.
        /// </summary>
        /// <param name="cosmosPath">Cosmos path.</param>
        /// <param name="frameShift">Frame shift.</param>
        /// <param name="frameLength">Frame length.</param>
        public GetLpcResidualErrorJob(string cosmosPath, int frameShift, int frameLength)
            : this()
        {
            this.Job.ReplaceVariable["COSMOSDLL"] = TmocPath.Combine(cosmosPath, this.Job.ReplaceVariable["COSMOSDLL"]);
            this.Job.ReplaceVariable["OFFLINEDLL"] = TmocPath.Combine(cosmosPath, this.Job.ReplaceVariable["OFFLINEDLL"]);
            this.Job.ReplaceVariable["WAVEDATACORPUS"] = TmocPath.Combine(cosmosPath, this.Job.ReplaceVariable["WAVEDATACORPUS"]);
            this.Job.ReplaceVariable["WAVEDATACORPUSWITHF0NCCFRFERR"] = TmocPath.Combine(cosmosPath, this.Job.ReplaceVariable["WAVEDATACORPUSWITHF0NCCFRFERR"]);
            this.Job.ReplaceVariable["FRAMESHIFT"] = frameShift.ToString();
            this.Job.ReplaceVariable["FRAMELENGTH"] = frameLength.ToString();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GetLpcResidualErrorJob" /> class.
        /// </summary>
        public GetLpcResidualErrorJob()
        {
            this.Job.ResourceScript = "Microsoft.Tts.Cosmos.Handler.GetLpcResidualErrorJob";
            this.Job.ReplaceVariable = new Dictionary<string, string>
            {
                { "COSMOSDLL", @"Tools/Microsoft.Tts.Cosmos.dll" },
                { "OFFLINEDLL", @"Tools/Microsoft.Tts.Offline.dll" },
                { "WAVEDATACORPUS", "Data/InputLPC.ss" },
                { "WAVEDATACORPUSWITHF0NCCFRFERR", @"Data/InputERR.ss" },
                { "FRAMESHIFT", string.Empty },
                { "FRAMELENGTH", string.Empty },
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
            string frameShift = args[0];
            string frameLength = args[1];

            string waveDir = "wave";
            string lpcDir = "lpc";
            string residual0Dir = "residual";
            Directory.CreateDirectory(waveDir);
            Directory.CreateDirectory(lpcDir);
            Directory.CreateDirectory(residual0Dir);

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
                outputRow["NCCF"].Set(row["NCCF"].String);
                outputRow["RF"].Set(row["RF"].String);

                string waveId = row["WaveID"].String;
                string waveFile = JobBase.GenerateLocalFile(waveId, row["WaveBinary"].Binary, FileExtensions.Waveform, waveDir);
                string lpcFile = JobBase.GenerateLocalFile(waveId, row["LPC"].String, FileExtensions.F0File, false, lpcDir);
                string errorFile = Path.Combine(residual0Dir, waveId + "." + FileExtensions.Text);

                string[] argument = { waveFile, lpcFile, errorFile, frameShift, frameLength };
                F0ExtractorCOSMOS.ExtractLpcResidualErrorOneFile(argument, null);

                outputRow["ERR"].Set(File.ReadAllText(errorFile));
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
            ScopeRuntime.Schema outputSchema = new ScopeRuntime.Schema("WaveID:string, WaveBinary:byte[], WaveAlignments:string, RawF0:string, LPCC:byte[], OF0:string, LSP:byte[], Pow:string, MBE:string, NCCF:string, RF:string, ERR:string");
            return outputSchema;
        }

        #endregion
    }
}
