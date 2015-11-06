// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GetLpcJob.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//   Get Lpc job, used for when cosmos enabled.
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
    /// Get Lpc.
    /// </summary>
    [CLSCompliantAttribute(false)]
    public class GetLpcJob : JobProcessor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GetLpcJob" /> class.
        /// </summary>
        /// <param name="cosmosPath">Cosmos path.</param>
        /// <param name="fftDIM">Fft dim.</param>
        /// <param name="lpcOrder">Lpc order.</param>
        /// <param name="secondsPerFrame">Seconds per frame.</param>
        public GetLpcJob(string cosmosPath, int fftDIM, int lpcOrder, float secondsPerFrame)
            : this()
        {
            this.Job.ReplaceVariable["COSMOSDLL"] = TmocPath.Combine(cosmosPath, this.Job.ReplaceVariable["COSMOSDLL"]);
            this.Job.ReplaceVariable["OFFLINEDLL"] = TmocPath.Combine(cosmosPath, this.Job.ReplaceVariable["OFFLINEDLL"]);
            this.Job.ReplaceVariable["GETLPCTOOl"] = TmocPath.Combine(cosmosPath, this.Job.ReplaceVariable["GETLPCTOOl"]);
            this.Job.ReplaceVariable["WAVEDATACORPUS"] = TmocPath.Combine(cosmosPath, this.Job.ReplaceVariable["WAVEDATACORPUS"]);
            this.Job.ReplaceVariable["WAVEDATACORPUSWITHF0NCCFRFLPC"] = TmocPath.Combine(cosmosPath, this.Job.ReplaceVariable["WAVEDATACORPUSWITHF0NCCFRFLPC"]);
            this.Job.ReplaceVariable["FFTDIM"] = fftDIM.ToString();
            this.Job.ReplaceVariable["LPCORDER"] = lpcOrder.ToString();
            this.Job.ReplaceVariable["SECONDSPERFRAME"] = secondsPerFrame.ToString();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GetLpcJob" /> class.
        /// </summary>
        public GetLpcJob()
        {
            this.Job.ResourceScript = "Microsoft.Tts.Cosmos.Handler.GetLpcJob";
            this.Job.ReplaceVariable = new Dictionary<string, string>
            {
                { "COSMOSDLL", @"Tools/Microsoft.Tts.Cosmos.dll" },
                { "OFFLINEDLL", @"Tools/Microsoft.Tts.Offline.dll" },
                { "GETLPCTOOl", @"Tools/Extern/STRAIGHT_All.exe" },
                { "WAVEDATACORPUS", "Data/InputRF.ss" },
                { "WAVEDATACORPUSWITHF0NCCFRFLPC", @"Data/InputLPC.ss" },
                { "FFTDIM", string.Empty },
                { "LPCORDER", string.Empty },
                { "SECONDSPERFRAME", string.Empty },
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
            int fftDIM = int.Parse(args[0]);
            int lpcOrder = int.Parse(args[1]);
            float secondsPerFrame = float.Parse(args[2]);

            string waveDir = "wave";
            string f0Dir = "f0";
            string lpcDir = "lpc";
            string lpcF0Dir = "lpc_of0";
            Directory.CreateDirectory(waveDir);
            Directory.CreateDirectory(f0Dir);
            Directory.CreateDirectory(lpcDir);
            Directory.CreateDirectory(lpcF0Dir);

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
                string f0File = JobBase.GenerateLocalFile(waveId, row["RawF0"].String, FileExtensions.F0File, true, f0Dir);
                string lpcFile = Path.Combine(lpcDir, waveId + "." + FileExtensions.LpcFile);
                string of0File = Path.Combine(lpcF0Dir, waveId + "." + FileExtensions.F0File);

                string argument = F0ExtractorCOSMOS.GenerateExtractLpcArgument(waveFile, f0File, lpcFile, of0File, fftDIM, lpcOrder, secondsPerFrame);
                CommandLine.RunCommand(Path.GetFileName(this.Job.ReplaceVariable["GETLPCTOOl"]), argument, null);
                outputRow["LPC"].Set(File.ReadAllText(lpcFile));
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
            ScopeRuntime.Schema outputSchema = new ScopeRuntime.Schema("WaveID:string, WaveBinary:byte[], WaveAlignments:string, RawF0:string, LPCC:byte[], OF0:string, LSP:byte[], Pow:string, MBE:string, NCCF:string, RF:string, LPC:string");
            return outputSchema;
        }

        #endregion
    }
}
