// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SmoothF0Job.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//   Smooth F0 used for when cosmos enabled.
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
    /// Smooth F0.
    /// </summary>
    [CLSCompliantAttribute(false)]
    public class SmoothF0WithUVJob : JobProcessor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SmoothF0WithUVJob" /> class.
        /// </summary>
        /// <param name="cosmosPath">Cosmos path.</param>
        /// <param name="minF0Value">Min F0 value.</param>
        /// <param name="maxF0Value">Max F0 value.</param>
        public SmoothF0WithUVJob(string cosmosPath, float minF0Value, float maxF0Value)
            : this()
        {
            this.Job.ReplaceVariable["COSMOSDLL"] = TmocPath.Combine(cosmosPath, this.Job.ReplaceVariable["COSMOSDLL"]);
            this.Job.ReplaceVariable["OFFLINEDLL"] = TmocPath.Combine(cosmosPath, this.Job.ReplaceVariable["OFFLINEDLL"]);
            this.Job.ReplaceVariable["WAVEDATACORPUS"] = TmocPath.Combine(cosmosPath, this.Job.ReplaceVariable["WAVEDATACORPUS"]);
            this.Job.ReplaceVariable["WAVEDATACORPUSWITHSF0"] = TmocPath.Combine(cosmosPath, this.Job.ReplaceVariable["WAVEDATACORPUSWITHSF0"]);
            this.Job.ReplaceVariable["MINF0VALUE"] = minF0Value.ToString();
            this.Job.ReplaceVariable["MAXF0VALUE"] = maxF0Value.ToString();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SmoothF0WithUVJob" /> class.
        /// </summary>
        public SmoothF0WithUVJob()
        {
            this.Job.ResourceScript = "Microsoft.Tts.Cosmos.Handler.SmoothF0Job";
            this.Job.ReplaceVariable = new Dictionary<string, string>
            {
                { "COSMOSDLL", @"Tools/Microsoft.Tts.Cosmos.dll" },
                { "OFFLINEDLL", @"Tools/Microsoft.Tts.Offline.dll" },
                { "WAVEDATACORPUS", "Data/InputUV.ss" },
                { "WAVEDATACORPUSWITHSF0", @"Data/InputSF0.ss" },
                { "MINF0VALUE", string.Empty },
                { "MAXF0VALUE", string.Empty },
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
            float minF0Value = float.Parse(args[0]);
            float maxF0Value = float.Parse(args[1]);
            string uvDir = "uv";
            string fZeroDir = "f0";
            string smoothedFZeroDir = "smoothedF0";

            Directory.CreateDirectory(uvDir);
            Directory.CreateDirectory(fZeroDir);
            Directory.CreateDirectory(smoothedFZeroDir);

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
                string f0File = JobBase.GenerateLocalFile(waveId, row["RawF0"].String, FileExtensions.F0File, true, fZeroDir);
                string uvFile = JobBase.GenerateLocalFile(waveId, row["UV"].String, FileExtensions.Text, true, uvDir);
                string smoothedF0File = Path.Combine(smoothedFZeroDir, waveId + "." + FileExtensions.F0File);
                string[] argument = { f0File, uvFile, smoothedF0File, minF0Value.ToString(), maxF0Value.ToString() };
                F0ExtractorCOSMOS.SmoothOneF0File(argument, null);
                outputRow["SF0"].Set(JobBase.GetTextFile(smoothedF0File));
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
            ScopeRuntime.Schema outputSchema = new ScopeRuntime.Schema("WaveID:string, WaveBinary:byte[], WaveAlignments:string, RawF0:string, LPCC:byte[], OF0:string, LSP:byte[], Pow:string, MBE:string, SF0:string");
            return outputSchema;
        }

        #endregion
    }
}
