// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GetF0NccfJob.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//   Get F0 and Nccf feature job, used for when cosmos enabled.
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
    /// Get F0 and Nccf feature.
    /// </summary>
    [CLSCompliantAttribute(false)]
    public class GetF0NccfJob : JobProcessor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GetF0NccfJob" /> class.
        /// </summary>
        /// <param name="frameBias">Frame bias.</param>
        /// <param name="secondsPerFrame">Seconds per frame.</param>
        /// <param name="minF0Value">Min F0 value.</param>
        /// <param name="maxF0Value">Max F0 value.</param>
        /// <param name="cosmosPath">Cosmos path.</param>
        /// <param name="fileSS">File SS.</param>
        public GetF0NccfJob(int frameBias, float secondsPerFrame, float minF0Value, float maxF0Value, string cosmosPath, string fileSS)
            : this()
        {
            this.Job.ReplaceVariable["COSMOSDLL"] = TmocPath.Combine(cosmosPath, this.Job.ReplaceVariable["COSMOSDLL"]);
            this.Job.ReplaceVariable["OFFLINEDLL"] = TmocPath.Combine(cosmosPath, this.Job.ReplaceVariable["OFFLINEDLL"]);
            this.Job.ReplaceVariable["GETF0TOOl"] = TmocPath.Combine(cosmosPath, this.Job.ReplaceVariable["GETF0TOOl"]);
            this.Job.ReplaceVariable["WAVEDATACORPUS"] = TmocPath.Combine(cosmosPath, "Data", fileSS);
            this.Job.ReplaceVariable["WAVEDATACORPUSWITHF0NCCF"] = TmocPath.Combine(cosmosPath, this.Job.ReplaceVariable["WAVEDATACORPUSWITHF0NCCF"]);
            this.Job.ReplaceVariable["GETF0CONFIG"] = TmocPath.Combine(cosmosPath, this.Job.ReplaceVariable["GETF0CONFIG"]);
            this.Job.ReplaceVariable["FRAMEBIAS"] = frameBias.ToString();
            this.Job.ReplaceVariable["SECONDSPERFRAME"] = secondsPerFrame.ToString();
            this.Job.ReplaceVariable["MINF0VALUE"] = minF0Value.ToString();
            this.Job.ReplaceVariable["MAXF0VALUE"] = maxF0Value.ToString();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GetF0NccfJob" /> class.
        /// </summary>
        public GetF0NccfJob()
        {
            this.Job.ResourceScript = "Microsoft.Tts.Cosmos.Handler.GetF0NccfJob";
            this.Job.ReplaceVariable = new Dictionary<string, string>
            {
                { "COSMOSDLL", @"Tools/Microsoft.Tts.Cosmos.dll" },
                { "OFFLINEDLL", @"Tools/Microsoft.Tts.Offline.dll" },
                { "GETF0TOOl", @"Tools/Extern/get_f0.exe" },
                { "WAVEDATACORPUS", string.Empty },
                { "WAVEDATACORPUSWITHF0NCCF", @"Data/InputF0NCCF.ss" },
                { "GETF0CONFIG", @"Tools/Conf/get_f0.conf" },
                { "FRAMEBIAS", "0" },
                { "SECONDSPERFRAME", string.Empty },
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
            string f0Config = Path.GetFileName(args[0]);
            int frameBias = int.Parse(args[1]);
            float secondPerFrame = float.Parse(args[2]);
            float minf0Value = float.Parse(args[3]);
            float maxf0Value = float.Parse(args[4]);

            Directory.CreateDirectory("f0Nccf");
            Directory.CreateDirectory("f0");
            Directory.CreateDirectory("Nccf");

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
                string waveFile = JobBase.GenerateLocalFile(waveId, row["WaveBinary"].Binary, FileExtensions.Waveform);
                string f0NccfFile = Path.Combine("f0Nccf", waveId + "." + FileExtensions.F0File);

                string argument = F0ExtractorCOSMOS.GenerateExtractF0NccfArgument(f0Config, waveFile, f0NccfFile, frameBias, secondPerFrame, minf0Value, maxf0Value);
                CommandLine.RunCommand(Path.GetFileName(this.Job.ReplaceVariable["GETF0TOOl"]), argument, null);

                string f0File = Path.Combine("f0", waveId + "." + FileExtensions.F0File);
                string nccfFile = Path.Combine("Nccf", waveId + "." + FileExtensions.F0File);
                string[] argumentList = { f0NccfFile, nccfFile, f0File };
                F0ExtractorCOSMOS.ExtractF0NccfOneFile(argumentList, null);

                var f0Ouput = JobBase.GetTextFile(f0File);
                var nccfOuput = JobBase.GetTextFile(nccfFile);

                outputRow["NCCF"].Set(nccfOuput);
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
            ScopeRuntime.Schema outputSchema = new ScopeRuntime.Schema("WaveID:string, WaveBinary:byte[], WaveAlignments:string, RawF0:string, LPCC:byte[], OF0:string, LSP:byte[], Pow:string, MBE:string, NCCF:string");
            return outputSchema;
        }

        #endregion
    }
}
