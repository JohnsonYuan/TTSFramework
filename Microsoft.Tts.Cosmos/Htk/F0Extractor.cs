// --------------------------------------------------------------------------------------------------------------------
// <copyright file="F0Extractor.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//   This module defines a common library to extract f0 file by using uv prediction.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Tts.Cosmos.Htk
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Microsoft.Tts.Cosmos.FlowEngine;
    using Microsoft.Tts.Cosmos.TMOC;
    using Microsoft.Tts.Cosmos.Utility;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.FlowEngine;
    using Microsoft.Tts.Offline.Htk;
    using Microsoft.Tts.Offline.Utility;
    using Microsoft.Tts.Offline.Waveform;

    /// <summary>
    /// The class to extract more accurate f0 by uv model.
    /// </summary>
    public class F0ExtractorCOSMOS
    {
        #region Fields

        // FFT dimension
        private const int FFTDim = 512;

        // minimum value for non-zero division or log
        private const float Minimum = 0.000001f;

        // lpc order
        private const int LpcOrder = 12;

        // uv feature dimension
        // Basic features: nccf, cross zero, energy, atutocorrelation, lpc, lpc residual error
        private const int BasicDimension = 6;

        // Expanded features (6*3): [previous frame, current frame, next freame]
        private const int ExpandDimension = 18;

        // Added delta features (18*3)
        private const int DeltaDimension = 54;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="F0ExtractorCOSMOS" /> class.
        /// </summary>
        /// <param name="fileMap">File list map.</param>
        /// <param name="waveDir">Input wave directory.</param>
        /// <param name="workDir">Output data direcotry.</param>
        /// <param name="getF0Tool">The path of get_f0.exe file.</param>
        /// <param name="getF0Config">The path of get_f0.config file.</param>
        /// <param name="straightTool">The path of straight_all.exe file.</param>
        /// <param name="svmScaleTool">The path of svm-scale.exe file.</param>
        /// <param name="svmPredictTool">The path of svm-predit.exe file.</param>
        /// <param name="modelFilePath">Svm model file path.</param>
        /// <param name="minF0">The min f0 value.</param>
        /// <param name="maxF0">The max f0 value.</param>
        /// <param name="secondsPerFrame">The length of one frame in seconds.</param>
        /// <param name="samplesPerSecond">The sample number in one second.</param>
        /// <param name="dimension">Uv feature dimension.</param>
        /// <param name="frameBias">Frame bias of f0.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="enableCosmos">Enable cosmos.</param>
        /// <param name="cosmosPath">Cosmos path.</param>
        /// <param name="fileSS">The structured stream file.</param>
        /// <exception cref="DirectoryNotFoundException">Exception.</exception>
        /// <exception cref="FileNotFoundException">Exception.</exception>
        /// <exception cref="ArgumentNullException">Exception.</exception>
        /// <exception cref="ArgumentException">Exception</exception>
        public F0ExtractorCOSMOS(string fileMap, string waveDir, string workDir, string getF0Tool, string getF0Config, string straightTool, string svmScaleTool, string svmPredictTool, string modelFilePath,
            float minF0, float maxF0, float secondsPerFrame, int samplesPerSecond, int dimension, int frameBias, ILogger logger,
            bool enableCosmos = false, string cosmosPath = " ", string fileSS = " ")
        {
            // check input arguments
            Helper.ThrowIfDirectoryNotExist(waveDir);
            Helper.ThrowIfFileNotExist(modelFilePath);

            // check dimensions
            if (dimension != BasicDimension && dimension != ExpandDimension && dimension != DeltaDimension)
            {
                throw new ArgumentException("Invalid dimension value, it should be 6/18/54");
            }

            // check f0 range
            if (minF0 > maxF0 || minF0 < 0)
            {
                throw new ArgumentException("Invalid F0 range.");
            }

            // check seconds per frame
            if (secondsPerFrame <= Minimum)
            {
                throw new ArgumentException("Invalid seconds of per frame.");
            }

            // check sample rate 
            if (samplesPerSecond <= Minimum)
            {
                throw new ArgumentException("Invalid sample rate.");
            }

            // check frame bias 
            if (frameBias < -10 || frameBias > 10)
            {
                throw new ArgumentException("Frame bias of f0 should be in the range [-10, 10].");
            }

            // assign values to fields
            WorkDir = workDir;
            Helper.EnsureFolderExist(workDir);
            WaveDir = waveDir;
            ModelFilePath = modelFilePath;
            Dimension = dimension;
            MinF0Value = minF0;
            MaxF0Value = maxF0;
            SecondsPerFrame = secondsPerFrame;
            FrameShift = (int)Math.Ceiling(samplesPerSecond * secondsPerFrame);
            FrameLength = FrameShift * 2;
            FrameBias = frameBias;

            // check logger
            Logger = (logger == null) ? new NullLogger() : logger;

            // get file list
            if (string.IsNullOrWhiteSpace(fileMap) || !File.Exists(fileMap))
            {
                FileMap = FileListMap.CreateInstance(waveDir, string.Empty.AppendExtensionName(FileExtensions.Waveform.ToLowerInvariant()));
            }
            else
            {
                FileMap = new FileListMap();
                FileMap.Load(fileMap);
            }

            if (FileMap.Map.Count == 0)
            {
                throw new ArgumentException("Empty wave folder!");
            }

            EnableCosmos = enableCosmos;
            CosmosPath = cosmosPath;
            FileSS = fileSS;

            // make intermediate directories
            IntermediateDir = Path.Combine(workDir, "Intermediate");
            GetF0Tool = getF0Tool;
            Helper.ThrowIfFileNotExist(GetF0Tool);
            GetF0Config = getF0Config;
            Helper.ThrowIfFileNotExist(GetF0Config);
            StraightTool = straightTool;
            Helper.ThrowIfFileNotExist(StraightTool);
            SvmScaleTool = svmScaleTool;
            Helper.ThrowIfFileNotExist(SvmScaleTool);
            SvmPredictTool = svmPredictTool;
            Helper.ThrowIfFileNotExist(SvmPredictTool);
            F0NccfDir = Path.Combine(IntermediateDir, "get_f0");
            Helper.EnsureFolderExist(F0NccfDir);
            RealtedFeaDir = Path.Combine(IntermediateDir, "relatedFeatures");
            Helper.EnsureFolderExist(RealtedFeaDir);
            NccfDir = Path.Combine(IntermediateDir, "nccf");
            Helper.EnsureFolderExist(NccfDir);
            LPCDir = Path.Combine(IntermediateDir, "lpc");
            Helper.EnsureFolderExist(LPCDir);
            ResidualDir = Path.Combine(IntermediateDir, "residual");
            Helper.EnsureFolderExist(ResidualDir);
            F0Dir = Path.Combine(IntermediateDir, "if0");
            Helper.EnsureFolderExist(F0Dir);
            MergedFeaDir = Path.Combine(IntermediateDir, "mergedFea");
            Helper.EnsureFolderExist(MergedFeaDir);
            ExpandFeaDir = Path.Combine(IntermediateDir, "expandedFea");
            Helper.EnsureFolderExist(ExpandFeaDir);
            SvmFeaDir = Path.Combine(IntermediateDir, "formatFea");
            Helper.EnsureFolderExist(SvmFeaDir);
            ScaledSvmFeaDir = Path.Combine(IntermediateDir, "scaledFea");
            Helper.EnsureFolderExist(ScaledSvmFeaDir);
            UVDir = Path.Combine(IntermediateDir, "uv");
            Helper.EnsureFolderExist(UVDir);
            SmoothedF0Dir = Path.Combine(WorkDir, "sf0");
            Helper.EnsureFolderExist(SmoothedF0Dir);
        }

        #endregion

        #region Properties

        private bool EnableCosmos { get; set; }

        private string CosmosPath { get; set; }

        private string FileSS { get; set; }

        /// <summary>
        /// Gets or sets wave directory.
        /// </summary>
        private string WaveDir { get; set; }

        /// <summary>
        /// Gets or sets work directory.
        /// </summary>
        private string WorkDir { get; set; }

        /// <summary>
        /// Gets or sets svm model file path.
        /// </summary>
        private string ModelFilePath { get; set; }

        /// <summary>
        /// Gets or sets the minimal F0 value.
        /// </summary>
        private float MinF0Value { get; set; }

        /// <summary>
        /// Gets or sets the maximum f0 value.
        /// </summary>
        private float MaxF0Value { get; set; }

        /// <summary>
        /// Gets or sets the length of one frame in seconds.
        /// </summary>
        private float SecondsPerFrame { get; set; }

        /// <summary>
        /// Gets or sets the sample count of one frame shift.
        /// </summary>
        private int FrameShift { get; set; }

        /// <summary>
        /// Gets or sets the sample count of one frame.
        /// </summary>
        private int FrameLength { get; set; }

        /// <summary>
        /// Gets or sets uv feature dimension.
        /// </summary>
        private int Dimension { get; set; }

        /// <summary>
        /// Gets or sets frame bias.
        /// </summary>
        private int FrameBias { get; set; }

        /// <summary>
        /// Gets or sets logger.
        /// </summary>
        private ILogger Logger { get; set; }

        /// <summary>
        /// Gets or sets file map list.
        /// </summary>
        private FileListMap FileMap { get; set; }

        /// <summary>
        /// Gets or sets intermediate directory.
        /// </summary>
        private string IntermediateDir { get; set; }

        /// <summary>
        /// Gets or sets the get_f0.exe.
        /// </summary>
        private string GetF0Tool { get; set; }

        /// <summary>
        /// Gets or sets the tool configuration file for get_f0.exe.
        /// </summary>
        private string GetF0Config { get; set; }

        /// <summary>
        /// Gets or sets the get_f0.exe.
        /// </summary>
        private string StraightTool { get; set; }

        /// <summary>
        /// Gets or sets the svm-scale.exe.
        /// </summary>
        private string SvmScaleTool { get; set; }

        /// <summary>
        /// Gets or sets the svm-predict.exe.
        /// </summary>
        private string SvmPredictTool { get; set; }

        /// <summary>
        /// Gets or sets directory for f0_nccf data.
        /// </summary>
        private string F0NccfDir { get; set; }

        /// <summary>
        /// Gets or sets directory for related features.
        /// </summary>
        private string RealtedFeaDir { get; set; }

        /// <summary>
        /// Gets or sets directory for nccf features.
        /// </summary>
        private string NccfDir { get; set; }

        /// <summary>
        /// Gets or sets directory for lpc features.
        /// </summary>
        private string LPCDir { get; set; }

        /// <summary>
        /// Gets or sets directory for lpc residual error.
        /// </summary>
        private string ResidualDir { get; set; }

        /// <summary>
        /// Gets or sets directory for f0 features.
        /// </summary>
        private string F0Dir { get; set; }

        /// <summary>
        /// Gets or sets directory for merged six basic features.
        /// </summary>
        private string MergedFeaDir { get; set; }

        /// <summary>
        /// Gets or sets directory for expanded features.
        /// </summary>
        private string ExpandFeaDir { get; set; }

        /// <summary>
        /// Gets or sets directory for svm features.
        /// </summary>
        private string SvmFeaDir { get; set; }

        /// <summary>
        /// Gets or sets directory for scaled svm features.
        /// </summary>
        private string ScaledSvmFeaDir { get; set; }

        /// <summary>
        /// Gets or sets directory for uv result.
        /// </summary>
        private string UVDir { get; set; }

        /// <summary>
        /// Gets or sets directory for smoothed f0 files.
        /// </summary>
        private string SmoothedF0Dir { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Extract lpc residual error.
        /// </summary>
        /// <param name="args">Arguments: wave file, lpc file, lpc error file.</param>
        /// <param name="logWriter">LogWriter to implement parallel computing interface.</param>
        /// <exception cref="ArgumentException">Exception.</exception>
        public static void ExtractLpcResidualErrorOneFile(string[] args, TextWriter logWriter)
        {
            // check arguments
            if (args.Length < 3)
            {
                throw new ArgumentException("Arguments for ExtractLpcResidualErrorOneFile: input wave file, input lpc file, output lpc error file");
            }

            // check input and output file
            string wavePath = args[0];
            string lpcFile = args[1];
            string errorFile = args[2];
            int frameShift = int.Parse(args[3]);
            int frameLength = int.Parse(args[4]);

            // output <zeroCrossing energy autoCorrelation>
            List<double[]> lpcData = new List<double[]>();
            foreach (string line in Helper.FileLines(lpcFile))
            {
                string[] fields = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                double[] data = fields.Select(i => double.Parse(i, CultureInfo.InvariantCulture)).ToArray();
                lpcData.Add(data);
            }

            using (StreamWriter sw = new StreamWriter(errorFile, false))
            {
                // load wave
                WaveFile waveFile = new WaveFile();
                waveFile.Load(wavePath);
                short[] waveData = ArrayHelper.BinaryConvertArray(waveFile.GetSoundData());

                // calculate residual error
                for (int i = 0; i < lpcData.Count; i++)
                {
                    int pos = (i + 1) * frameShift;
                    int nbegin = pos - (frameLength / 2);
                    int nend = pos + (frameLength / 2);
                    double energy = 0;

                    // calculate actual value
                    if (nend <= waveData.Length && nbegin >= 0)
                    {
                        for (int j = nbegin; j < nend; j++)
                        {
                            energy += waveData[j] * waveData[j];
                        }

                        energy = energy / (double)frameLength;
                        double tempt_energy = energy;
                        energy = 10 * Math.Log(Minimum + energy);

                        // calculate prediction value
                        double prediction = 0;
                        for (int k = 0; k < LpcOrder; k++)
                        {
                            double denergy = 0;
                            for (int j = nbegin; j < nend; j++)
                            {
                                if (j - k > 0)
                                {
                                    denergy += waveData[j] * waveData[j - k];
                                }
                            }

                            prediction += lpcData[i][k] * (denergy / (double)frameLength);
                        }

                        prediction = prediction + tempt_energy;
                        prediction = 10 * Math.Log(Math.Abs(prediction) + Minimum);

                        // output residual error
                        sw.WriteLine("{0:F6} {1:F6}", lpcData[i][0], energy - prediction);
                    }
                }
            }
        }

        /// <summary>
        /// Merge 6 basic features.
        /// </summary>
        /// <param name="args">Arguments: related fea file, residual fea file, nccf fea file, merged fea file.</param>
        /// <param name="logWriter">LogWriter to implement parallel computing interface.</param>
        /// <exception cref="ArgumentException">Exception.</exception>
        public static void MergeFeaturesOneFile(string[] args, TextWriter logWriter)
        {
            // check arguments
            if (args.Length < 4)
            {
                throw new ArgumentException("Arguments for MergeFeaturesOneFile:  related fea file, residual fea file, nccf fea file, merged fea file");
            }

            // check input and output file
            string relatedFeaFile = args[0];
            string residualFeaFile = args[1];
            string nccfFeaFile = args[2];
            string mergedFeaFile = args[3];

            using (StreamReader srRelatedFea = new StreamReader(relatedFeaFile))
            {
                using (StreamReader srResidualFea = new StreamReader(residualFeaFile))
                {
                    using (StreamReader srNccfFea = new StreamReader(nccfFeaFile))
                    {
                        using (StreamWriter sw = new StreamWriter(mergedFeaFile, false))
                        {
                            string oneRelatedFea = null;
                            string oneResidualFea = null;
                            string oneNccfFea = null;
                            while ((oneRelatedFea = srRelatedFea.ReadLine()) != null)
                            {
                                oneResidualFea = srResidualFea.ReadLine();
                                if (oneResidualFea == null)
                                {
                                    break;
                                }

                                oneNccfFea = srNccfFea.ReadLine();
                                if (oneNccfFea == null)
                                {
                                    break;
                                }

                                sw.WriteLine("{0} {1} {2}", oneRelatedFea, oneResidualFea, oneNccfFea);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Expand features: add previous frame, next frame and calculate delta.
        /// </summary>
        /// <param name="args">Arguments: basic six fea file, expanded fea file.</param>
        /// <param name="logWriter">LogWriter to implement parallel computing interface.</param>
        /// <exception cref="ArgumentException">Exception.</exception>
        public static void ExpandFeaturesOneFile(string[] args, TextWriter logWriter)
        {
            // check arguments
            if (args.Length < 2)
            {
                throw new ArgumentException("Arguments for ExpandFeaturesOneFile:  basic fea file, expanded fea file");
            }

            // check input and output file
            string basicFeaFile = args[0];
            string expandedFeaFile = args[1];
            int dimension = int.Parse(args[2]);

            List<string> basicData = new List<string>();
            foreach (string line in Helper.FileLines(basicFeaFile))
            {
                basicData.Add(line);
            }

            using (StreamWriter sw = new StreamWriter(expandedFeaFile, false))
            {
                // first line
                int i = 0;
                sw.WriteLine("{0} {1} {2}", basicData[i], basicData[i], basicData[i + 1]);
                for (i = 1; i < basicData.Count - 1; ++i)
                {
                    sw.WriteLine("{0} {1} {2}", basicData[i - 1], basicData[i], basicData[i + 1]);
                }

                // last line
                sw.WriteLine("{0} {1} {2}", basicData[i - 1], basicData[i], basicData[i]);
            }

            basicData.Clear();

            // calculate delta
            if (dimension == DeltaDimension)
            {
                List<double[]> expanddata = new List<double[]>();
                List<string> strExpandData = new List<string>();
                foreach (string line in Helper.FileLines(expandedFeaFile))
                {
                    strExpandData.Add(line);
                    string[] fields = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    double[] row = fields.Select(k => double.Parse(k, CultureInfo.InvariantCulture)).ToArray();
                    expanddata.Add(row);
                }

                // calculate delta: <original data> ==> <original data data2 data3>
                using (StreamWriter sw = new StreamWriter(expandedFeaFile, false))
                {
                    // first line
                    int i = 0;
                    StringBuilder sb2 = new StringBuilder();
                    StringBuilder sb3 = new StringBuilder();
                    for (int j = 0; j < ExpandDimension; ++j)
                    {
                        double delta = expanddata[i + 1][j] - expanddata[i][j];
                        sb2.AppendFormat(" {0:F6}", 0.5 * delta);
                        sb3.AppendFormat(" {0:F6}", delta);
                    }

                    sw.WriteLine(strExpandData[i] + sb2.ToString() + sb3.ToString());

                    // other lines
                    for (i = 1; i < expanddata.Count - 1; ++i)
                    {
                        sb2.Clear();
                        sb3.Clear();
                        for (int j = 0; j < ExpandDimension; ++j)
                        {
                            sb2.AppendFormat(" {0:F6}", 0.5 * (expanddata[i + 1][j] - expanddata[i - 1][j]));
                            sb3.AppendFormat(" {0:F6}", expanddata[i + 1][j] + expanddata[i - 1][j] - (2 * expanddata[i][j]));
                        }

                        sw.WriteLine(strExpandData[i] + sb2.ToString() + sb3.ToString());
                    }

                    // last line
                    sb2.Clear();
                    sb3.Clear();
                    for (int j = 0; j < ExpandDimension; ++j)
                    {
                        sb2.AppendFormat(" {0:F6}", 0.5 * (expanddata[i][j] - expanddata[i - 1][j]));
                        sb3.AppendFormat(" {0:F6}", expanddata[i - 1][j] - expanddata[i][j]);
                    }

                    sw.WriteLine(strExpandData[i] + sb2.ToString() + sb3.ToString());
                }
            }
        }

        /// <summary>
        /// Format features into svm style: lable 1:fea1 2:fea2.
        /// </summary>
        /// <param name="args">Arguments: f0 file, raw fea file, svm fea file.</param>
        /// <param name="logWriter">LogWriter to implement parallel computing interface.</param>
        /// <exception cref="ArgumentException">Exception.</exception>
        public static void FormatFeaturesOneFile(string[] args, TextWriter logWriter)
        {
            // check arguments
            if (args.Length < 3)
            {
                throw new ArgumentException("Arguments for FormatFeaturesOneFile:  f0 file, raw fea file, svm fea file");
            }

            // check input and output file
            string f0File = args[0];
            string rawFeaFile = args[1];
            string svmFeaFile = args[2];

            using (StreamReader srF0 = new StreamReader(f0File))
            using (StreamReader srRawFea = new StreamReader(rawFeaFile))
            using (StreamWriter sw = new StreamWriter(svmFeaFile, false))
            {
                string oneF0 = null;
                string oneRawFea = null;
                while ((oneF0 = srF0.ReadLine()) != null)
                {
                    oneRawFea = srRawFea.ReadLine();
                    if (oneRawFea == null)
                    {
                        break;
                    }

                    StringBuilder sb = new StringBuilder();

                    // label=1: f0>0 otherwise 0
                    double f0 = double.Parse(oneF0);
                    sb.Append(f0 > 0 ? "1" : "0");

                    string[] fields = oneRawFea.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < fields.Length; ++i)
                    {
                        // <label 1:fea1 2:fea2>
                        sb.AppendFormat(" {0}:{1}", i + 1, fields[i]);
                    }

                    sw.WriteLine(sb.ToString());
                }
            }
        }

        /// <summary>
        /// Extract related features from wave: zero crossing, energy, autocorrelation.
        /// </summary>
        /// <param name="args">Arguments: wave file, fea file.</param>
        /// <param name="logWriter">LogWriter to implement parallel computing interface.</param>
        /// <exception cref="ArgumentException">Exception.</exception>
        public static void ExtractRelatedFeaturesOneFile(string[] args, TextWriter logWriter)
        {
            // check arguments.
            if (args.Length < 2)
            {
                throw new ArgumentException("Arguments for ExtractRelatedFeaturesOneFile: input wave file, output fea file");
            }

            // check input and output file.
            string wavePath = args[0];
            string feaFile = args[1];
            int frameShift = int.Parse(args[2]);
            int framelength = int.Parse(args[3]);

            // output <zeroCrossing energy autoCorrelation>.
            using (StreamWriter sw = new StreamWriter(feaFile, false))
            {
                // load wave
                WaveFile waveFile = new WaveFile();
                waveFile.Load(wavePath);
                short[] waveData = ArrayHelper.BinaryConvertArray(waveFile.GetSoundData());

                // calculate features.
                for (int i = 0;; ++i)
                {
                    if ((((i + 1) * frameShift) + (framelength / 2)) > waveData.Length)
                    {
                        break;
                    }

                    int nzero = 0;
                    double energy = 0;
                    double autoCorr = 0;
                    double dsum = 0;
                    double product1 = 0;
                    double product2 = 0;

                    int pos = (i + 1) * frameShift;
                    int nbegin = pos - (framelength / 2);
                    int nend = pos + (framelength / 2);

                    if (nend <= waveData.Length && nbegin >= 0)
                    {
                        if (nbegin == 0)
                        {
                            // process each frame.
                            int j = nbegin;
                            for (; j < nend - 1; ++j)
                            {
                                if ((waveData[j] < 0 && waveData[j + 1] > 0)
                                    || (waveData[j] > 0 && waveData[j + 1] < 0)
                                    || (waveData[j] == 0 && waveData[j + 1] != 0))
                                {
                                    nzero++;
                                }

                                energy += waveData[j] * waveData[j];
                            }

                            // calculate energy.
                            energy += waveData[j] * waveData[j];
                            energy = energy / framelength;
                            energy = 10 * Math.Log(Minimum + energy);
                        }
                        else
                        {
                            // process each frame.
                            int j = nbegin;
                            for (; j < nend - 1; ++j)
                            {
                                if ((waveData[j] < 0 && waveData[j + 1] > 0)
                                    || (waveData[j] > 0 && waveData[j + 1] < 0)
                                    || (waveData[j] == 0 && waveData[j + 1] != 0))
                                {
                                    nzero++;
                                }

                                energy += waveData[j] * waveData[j];

                                dsum += waveData[j] * waveData[j - 1];
                                product1 += waveData[j] * waveData[j];
                                product2 += waveData[j - 1] * waveData[j - 1];
                            }

                            // calculate energy.
                            energy += waveData[j] * waveData[j];
                            energy = energy / framelength;
                            energy = 10 * Math.Log(Minimum + energy);

                            // calculate auto correlation.
                            dsum += waveData[j] * waveData[j - 1];
                            product1 += waveData[j] * waveData[j];
                            product2 += waveData[j - 1] * waveData[j - 1];
                            autoCorr = dsum / Math.Sqrt(product1 * product2);
                        }
                    }

                    sw.WriteLine("{0} {1:F6} {2:F6}", nzero, energy, autoCorr);
                }
            }
        }

        /// <summary>
        /// Generate argument for Lpc extractor.
        /// </summary>
        /// <param name="waveFile">Wave file.</param>
        /// <param name="f0File">F0 file.</param>
        /// <param name="lpcFile">Lpc file.</param>
        /// <param name="of0File">OF0 file.</param>
        /// <param name="fftDim">FFT Dim.</param>
        /// <param name="lpcOrder">Lpc ordier.</param>
        /// <param name="secondsPerFrame">Seconds per frame.</param>
        /// <returns>Neutral format.</returns>
        public static string GenerateExtractLpcArgument(string waveFile, string f0File, string lpcFile, string of0File,
                                                int fftDim, int lpcOrder, float secondsPerFrame)
        {
            return Helper.NeutralFormat(" \"{0}\" \"{1}\" \"{2}\" \"{3}\" 16 {4} {5} {6} {7}", waveFile, f0File,
                                        lpcFile, of0File, fftDim, lpcOrder, secondsPerFrame, secondsPerFrame);
        }

        /// <summary>
        /// Generate argument for F0 extractor.
        /// </summary>
        /// <param name="getF0Config">F0 config.</param>
        /// <param name="waveFile">Wave file.</param>
        /// <param name="f0NccfFile">Nccf file.</param>
        /// <param name="frameBias">Frame bias.</param>
        /// <param name="secondsPerFrame">Seconds per frame.</param>
        /// <param name="minF0Value">Min F0 value.</param>
        /// <param name="maxF0Value">Max F0 vlaue.</param>
        /// <returns>The arguements.</returns>
        public static string GenerateExtractF0NccfArgument(string getF0Config, string waveFile, string f0NccfFile,
                        int frameBias, float secondsPerFrame, float minF0Value, float maxF0Value)
        {
            return (frameBias == 0) ? Helper.NeutralFormat(" -C \"{0}\" -i 2 -r {1} -n {2} -x {3} \"{4}\" \"{5}\"",
                                        getF0Config, secondsPerFrame, minF0Value, maxF0Value, waveFile, f0NccfFile) :
                                        Helper.NeutralFormat(" -C \"{0}\" -i 2 -g {1} -r {2} -n {3} -x {4} \"{5}\" \"{6}\"",
                                        getF0Config, frameBias, secondsPerFrame, minF0Value, maxF0Value, waveFile, f0NccfFile);
        }

        /// <summary>
        /// Process one f0 nccf file.
        /// </summary>
        /// <param name="args">Arguments: f0_nccf file, nccf file, f0 file.</param>
        /// <param name="logWriter">LogWriter to implement parallel computing interface.</param>
        /// <exception cref="ArgumentException">Exception.</exception>
        public static void ExtractF0NccfOneFile(string[] args, TextWriter logWriter)
        {
            // check arguments
            if (args.Length < 3)
            {
                throw new ArgumentException("Arguments for ExtractF0NccfOneFile: input f0_nccf file, output nccf file, out f0 file");
            }

            // check input and output file
            string f0NccfFile = args[0];
            string nccfFile = args[1];
            string f0File = args[2];

            // output the seconf field:nccf
            using (StreamWriter f0Writer = new StreamWriter(f0File, false))
            {
                using (StreamWriter nccfWriter = new StreamWriter(nccfFile, false))
                {
                    foreach (string line in Helper.FileLines(f0NccfFile))
                    {
                        // input format: "f0 nccf"
                        string[] fields = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (fields.Length == 2)
                        {
                            f0Writer.WriteLine(fields[0]);
                            nccfWriter.WriteLine(fields[1]);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Smooth f0 for one wave.
        /// </summary>
        /// <param name="args">Arguments: f0 file, raw fea file, svm fea file.</param>
        /// <param name="logWriter">LogWriter to implement parallel computing interface.</param>
        /// <exception cref="ArgumentException">Exception.</exception>
        public static void SmoothOneF0File(string[] args, TextWriter logWriter)
        {
            // check arguments
            if (args.Length < 3)
            {
                throw new ArgumentException("Arguments for FormatFeaturesOneFile:  f0 file, raw fea file, svm fea file");
            }

            string f0File = args[0];
            string uvFile = args[1];
            string smoothF0File = args[2];
            float minF0Value = float.Parse(args[3]);
            float maxF0Value = float.Parse(args[4]);

            TextF0File f0 = new TextF0File();
            f0.Load(f0File);
            f0.Smooth(2, 2, minF0Value, maxF0Value, uvFile);
            f0.Save(smoothF0File);
        }

        /// <summary>
        /// Smooth f0.
        /// </summary>
        /// <param name="if0Dir">Directory of input f0.</param>
        /// <param name="uvDir">Directory of uv data.</param>
        /// <param name="ifCosmos">Represents if this is a Cosmos operation.</param>
        /// <exception cref="ArgumentNullException">Exception.</exception>
        public void SmoothF0(string if0Dir, string uvDir, bool ifCosmos)
        {
            Logger.LogLine("Start to smooth f0...");
            if (!EnableCosmos && if0Dir == null)
            {
                throw new ArgumentNullException("if0Dir can't be null.");
            }

            if (!EnableCosmos && uvDir == null)
            {
                throw new ArgumentNullException("uvDir can't be null.");
            }

            switch (FlowHandler_Cosmos.GetCurrentComputationPlatform(ifCosmos))
            {
                case ComputationPlatform.MultiThreadPlatform:
                    {
                        var mulComputePlatform = (MultiThreadParallelComputation)FlowHandler.ParaComputePlatform;
                        mulComputePlatform.ParallelParameters = GenerateSmoothF0Commands(if0Dir, uvDir);
                        FlowHandler.ParaComputePlatform.Execute();
                    }

                    break;
                case ComputationPlatform.CosmosPlatform:
                    {
                        JobProcessor jb = new SmoothF0WithUVJob(CosmosPath, MinF0Value, MaxF0Value);
                        var cosmosjob = jb.Job.GenerateTemplate();
                        var cosmosComputePlatform = (CosmosParallelCompuation)FlowHandler_Cosmos.ParaComputePlatform_Cosmos;
                        cosmosComputePlatform.NebularArgument = 1500000;
                        cosmosComputePlatform.JobScript = cosmosjob;
                        cosmosComputePlatform.JobName = "SmoothF0Job";
                        cosmosComputePlatform.JobDisplayName = "SmoothF0Job";
                        FlowHandler_Cosmos.ParaComputePlatform_Cosmos.Execute();
                    }

                    break;
                default:
                    throw new Exception("Only multithread and cosmos platform can be used currently, please check the platform you use!");
            }

            Logger.LogLine("Finish smoothing f0.");
        }

        /// <summary>
        /// Extract enhanced F0.
        /// </summary>
        /// <param name="ifCosmos">Represents if this is a Cosmos operation.</param>
        public void ExtractEnhancedF0(bool ifCosmos)
        {
            Logger.LogLine("Start to extract enhanced f0...");
            ExtractFeatures(ifCosmos);
            PredictUV(ScaledSvmFeaDir, ifCosmos);
            SmoothF0(F0Dir, UVDir, ifCosmos);
            Logger.LogLine("Finish extracting enhanced f0.");
        }

        /// <summary>
        /// Extract features for uv prediction.
        /// </summary>
        /// <param name="ifCosmos">Represents if this is a Cosmos operation.</param>
        public void ExtractFeatures(bool ifCosmos)
        {
            Logger.LogLine("Start to extract features...");
            ExtractBasicFeatures(ifCosmos);
            MergeFeatures(ifCosmos);
            ExpandFeatures(ifCosmos);
            FormatFeatures(ifCosmos);
            ScaleFeatures(ifCosmos);
            Logger.LogLine("Finish extracting features.");
        }

        /// <summary>
        /// Predict uv results.
        /// </summary>
        /// <param name="feaDir">Direcotry of feature files.</param>
        /// <param name="ifCosmos">Represents if this is a Cosmos operation.</param>
        /// <exception cref="ArgumentNullException">Exception.</exception>
        public void PredictUV(string feaDir, bool ifCosmos)
        {
            Logger.LogLine("Start to predict uv...");
            if (!EnableCosmos && feaDir == null)
            {
                throw new ArgumentNullException("FeaDir can't be null.");
            }

            switch (FlowHandler_Cosmos.GetCurrentComputationPlatform(ifCosmos))
            {
                case ComputationPlatform.MultiThreadPlatform:
                    {
                        var mulComputePlatform = (MultiThreadParallelComputation)FlowHandler.ParaComputePlatform;
                        mulComputePlatform.ParallelParameters = GeneratePredictUVCommands(feaDir);
                        FlowHandler.ParaComputePlatform.Execute();
                    }

                    break;
                case ComputationPlatform.CosmosPlatform:
                    {
                        string svmPredictToolOnVC = TmocPath.Combine(CosmosPath, "Tools/SVM", Path.GetFileName(SvmPredictTool));
                        if (!TmocFile.Exists(svmPredictToolOnVC))
                        {
                            TmocFile.Copy(SvmPredictTool, svmPredictToolOnVC);
                        }

                        string modelFileOnVC = TmocPath.Combine(CosmosPath, "Tools/SVM", Path.GetFileName(ModelFilePath));
                        if (!TmocFile.Exists(modelFileOnVC))
                        {
                            TmocFile.Copy(ModelFilePath, modelFileOnVC);
                        }

                        JobProcessor jb = new PredictUVJob(CosmosPath);
                        var cosmosjob = jb.Job.GenerateTemplate();
                        var cosmosComputePlatform = (CosmosParallelCompuation)FlowHandler_Cosmos.ParaComputePlatform_Cosmos;
                        cosmosComputePlatform.NebularArgument = 1500000;
                        cosmosComputePlatform.JobScript = cosmosjob;
                        cosmosComputePlatform.JobName = "PredictUVJob";
                        cosmosComputePlatform.JobDisplayName = "PredictUVJob";
                        FlowHandler_Cosmos.ParaComputePlatform_Cosmos.Execute();
                    }

                    break;
                default:
                    throw new Exception("Only multithread and cosmos platform can be used currently, please check the platform you use!");
            }

            Logger.LogLine("Finish predicting uv.");
        }

        private List<ParallelParameter> GeneratePredictUVCommands(string feaDir)
        {
            List<ParallelParameter> parallelCommands = new List<ParallelParameter>();
            foreach (string sid in FileMap.Map.Keys)
            {
                // check feature file
                string feaFile = FileMap.BuildPath(feaDir, sid, FileExtensions.Text);
                if (!File.Exists(feaFile))
                {
                    Logger.LogLine("Fail to get scaled svm features for sentence {0}", sid);
                    continue;
                }

                // predict one sentence's uv
                string uvFile = FileMap.BuildPath(UVDir, sid, FileExtensions.Text);
                Helper.EnsureFolderExistForFile(uvFile);
                ParallelCommand command = new ParallelCommand
                {
                    ExecutableFile = SvmPredictTool,
                    Argument = Helper.NeutralFormat(" \"{0}\" \"{1}\" \"{2}\"", feaFile, ModelFilePath, uvFile),
                    Description = Helper.NeutralFormat("Predict uv for sentence \"{0}\"", sid),
                    Logger = Logger
                };

                parallelCommands.Add(command);
            }

            return parallelCommands;
        }

        private List<ParallelParameter> GenerateSmoothF0Commands(string if0Dir, string uvDir)
        {
            List<ParallelParameter> parallelCommands = new List<ParallelParameter>();
            foreach (string sid in FileMap.Map.Keys)
            {
                // check input file
                string f0File = FileMap.BuildPath(if0Dir, sid, FileExtensions.F0File);
                if (!File.Exists(f0File))
                {
                    Logger.LogLine("Fail to get f0 file for sentence {0}", sid);
                    continue;
                }

                string uvFile = FileMap.BuildPath(uvDir, sid, FileExtensions.Text);
                if (!File.Exists(uvFile))
                {
                    Logger.LogLine("Fail to get uv file for sentence {0}", sid);
                    continue;
                }

                // smooth one sentence's f0
                string smoothedF0File = FileMap.BuildPath(SmoothedF0Dir, sid, FileExtensions.F0File);
                Helper.EnsureFolderExistForFile(smoothedF0File);
                string[] argument = { f0File, uvFile, smoothedF0File, MinF0Value.ToString(), MaxF0Value.ToString() };
                ParallelMethod<string[]> method = new ParallelMethod<string[]>
                {
                    Argument = argument,
                    CallBackMethod = SmoothOneF0File,
                    Description = Helper.NeutralFormat("Get smoothed f0 for sentence \"{0}\"", sid),
                    Logger = Logger,
                };

                parallelCommands.Add(method);
            }

            return parallelCommands;
        }

        /// <summary>
        /// Extract 6 basic features.
        /// </summary>
        /// <param name="ifCosmos">Represents if this is a Cosmos operation.</param>
        private void ExtractBasicFeatures(bool ifCosmos)
        {
            // process f0_ncc file
            ExtractF0Nccf(ifCosmos);

            // get related features 
            ExtractRelatedFeatures(ifCosmos);

            // get lpc
            ExtractLpc(ifCosmos);

            // get lpc error
            ExtractLpcResidualError(ifCosmos);
        }

        /// <summary>
        /// Extract NCCF.
        /// </summary>
        /// <param name="ifCosmos">Represents if this is a Cosmos operation.</param>
        private void ExtractF0Nccf(bool ifCosmos)
        {
            Logger.LogLine("Start to extract f0 and Nccf...");

            switch (FlowHandler_Cosmos.GetCurrentComputationPlatform(ifCosmos))
            {
                case ComputationPlatform.MultiThreadPlatform:
                    {
                        var mulComputePlatform = (MultiThreadParallelComputation)FlowHandler.ParaComputePlatform;
                        mulComputePlatform.ParallelParameters = GenerateExtractF0NccfCommands();
                        FlowHandler.ParaComputePlatform.Execute();
                    }

                    break;
                case ComputationPlatform.CosmosPlatform:
                    {
                        JobProcessor jb = new GetF0NccfJob(FrameBias, SecondsPerFrame, MinF0Value, MaxF0Value, CosmosPath, FileSS);
                        var cosmosjob = jb.Job.GenerateTemplate();
                        var cosmosComputePlatform = (CosmosParallelCompuation)FlowHandler_Cosmos.ParaComputePlatform_Cosmos;
                        cosmosComputePlatform.NebularArgument = 1500000;
                        cosmosComputePlatform.JobScript = cosmosjob;
                        cosmosComputePlatform.JobName = "ExtractF0NccfJob";
                        cosmosComputePlatform.JobDisplayName = "ExtractF0NccfJob";
                        FlowHandler_Cosmos.ParaComputePlatform_Cosmos.Execute();
                    }

                    break;
                default:
                    throw new Exception("Only multithread and cosmos platform can be used currently, please check the platform you use!");
            }

            if (!EnableCosmos)
            {
                SplitF0NccfFileIntoSpearatedFile();
            }

            Logger.LogLine("Finish extracting f0 and Nccf.");
        }

        private List<ParallelParameter> GenerateExtractF0NccfCommands()
        {
            List<ParallelParameter> parallelCommands = new List<ParallelParameter>();

            // extract f0_nccf from wave
            foreach (string sid in FileMap.Map.Keys)
            {
                // process one wave file
                string waveFile = FileMap.BuildPath(WaveDir, sid, FileExtensions.Waveform);

                // 1) extract <f0 nccf> from one wave file
                string f0NccfFile = FileMap.BuildPath(F0NccfDir, sid, FileExtensions.F0File);
                Helper.EnsureFolderExistForFile(f0NccfFile);
                ParallelCommand command = new ParallelCommand
                {
                    ExecutableFile = GetF0Tool,

                    // TODO: need to evaluate "-b 1" furthermore.
                    Argument = GenerateExtractF0NccfArgument(GetF0Config, waveFile, f0NccfFile, FrameBias, SecondsPerFrame, MinF0Value, MaxF0Value),
                    Description = Helper.NeutralFormat("Get f0_nccf for sentence \"{0}\"", sid),
                    Logger = Logger,
                };

                parallelCommands.Add(command);
            }

            return parallelCommands;
        }

        private void SplitF0NccfFileIntoSpearatedFile()
        {
            List<ParallelParameter> parallelCommands = new List<ParallelParameter>();

            // split f0 and nccf.
            foreach (string sid in FileMap.Map.Keys)
            {
                // process one f0_nccf file.
                string f0NccfFile = FileMap.BuildPath(F0NccfDir, sid, FileExtensions.F0File);
                if (!File.Exists(f0NccfFile))
                {
                    Logger.LogLine("Fail to get f0_nccf for sentence {0}", sid);
                    continue;
                }

                // output file.
                string nccfFile = FileMap.BuildPath(NccfDir, sid, FileExtensions.F0File);
                Helper.EnsureFolderExistForFile(nccfFile);

                string f0File = FileMap.BuildPath(F0Dir, sid, FileExtensions.F0File);
                Helper.EnsureFolderExistForFile(f0File);

                // parallel computing.
                string[] argument = { f0NccfFile, nccfFile, f0File };
                ParallelMethod<string[]> method = new ParallelMethod<string[]>
                {
                    Argument = argument,
                    CallBackMethod = ExtractF0NccfOneFile,
                    Description = Helper.NeutralFormat("Get f0 and nccf of sentence \"{0}\"", sid),
                    Logger = Logger,
                };

                parallelCommands.Add(method);
            }

            FlowHandler.ParallelComputing(parallelCommands);
        }

        /// <summary>
        /// Extarct related features: zero crossing, energy, autocorrelation.
        /// </summary>
        /// <param name="ifCosmos">Represents if this is a Cosmos operation.</param>
        private void ExtractRelatedFeatures(bool ifCosmos)
        {
            Logger.LogLine("Start to extract realted features...");

            switch (FlowHandler_Cosmos.GetCurrentComputationPlatform(ifCosmos))
            {
                case ComputationPlatform.MultiThreadPlatform:
                    {
                        var mulComputePlatform = (MultiThreadParallelComputation)FlowHandler.ParaComputePlatform;
                        mulComputePlatform.ParallelParameters = GenerateExtractRelatedFeaturesCommands();
                        FlowHandler.ParaComputePlatform.Execute();
                    }

                    break;
                case ComputationPlatform.CosmosPlatform:
                    {
                        JobProcessor jb = new GetRelatedFeatureJob(CosmosPath, FrameShift, FrameLength);
                        var cosmosjob = jb.Job.GenerateTemplate();
                        var cosmosComputePlatform = (CosmosParallelCompuation)FlowHandler_Cosmos.ParaComputePlatform_Cosmos;
                        cosmosComputePlatform.NebularArgument = 1500000;
                        cosmosComputePlatform.JobScript = cosmosjob;
                        cosmosComputePlatform.JobName = "GetRelatedFeaturesJob";
                        cosmosComputePlatform.JobDisplayName = "GetRelatedFeaturesJob";
                        FlowHandler_Cosmos.ParaComputePlatform_Cosmos.Execute();
                    }

                    break;
                default:
                    throw new Exception("Only multithread and cosmos platform can be used currently, please check the platform you use!");
            }

            Logger.LogLine("Finish extracting realted features.");
        }

        private List<ParallelParameter> GenerateExtractRelatedFeaturesCommands()
        {
            List<ParallelParameter> parallelCommands = new List<ParallelParameter>();
            foreach (string sid in FileMap.Map.Keys)
            {
                // process one wave file.
                string waveFile = FileMap.BuildPath(WaveDir, sid, FileExtensions.Waveform);
                string relatedfeaFile = FileMap.BuildPath(RealtedFeaDir, sid, FileExtensions.Text);
                Helper.EnsureFolderExistForFile(relatedfeaFile);

                string[] argument = { waveFile, relatedfeaFile, FrameShift.ToString(), FrameLength.ToString() };
                ParallelMethod<string[]> method = new ParallelMethod<string[]>
                {
                    Argument = argument,
                    CallBackMethod = ExtractRelatedFeaturesOneFile,
                    Description = Helper.NeutralFormat("Get related features for sentence \"{0}\"", sid),
                    Logger = Logger,
                };

                parallelCommands.Add(method);
            }

            return parallelCommands;
        }

        /// <summary>
        /// Extract wave.
        /// </summary>
        /// <param name="ifCosmos">Represents if this is a Cosmos operation.</param>
        private void ExtractLpc(bool ifCosmos)
        {
            Logger.LogLine("Start to extract lpc...");

            switch (FlowHandler_Cosmos.GetCurrentComputationPlatform(ifCosmos))
            {
                case ComputationPlatform.MultiThreadPlatform:
                    {
                        var mulComputePlatform = (MultiThreadParallelComputation)FlowHandler.ParaComputePlatform;
                        mulComputePlatform.ParallelParameters = GenerateExtractLpcCommands();
                        FlowHandler.ParaComputePlatform.Execute();
                    }

                    break;
                case ComputationPlatform.CosmosPlatform:
                    {
                        JobProcessor jb = new GetLpcJob(CosmosPath, FFTDim, LpcOrder, SecondsPerFrame);
                        var cosmosjob = jb.Job.GenerateTemplate();
                        var cosmosComputePlatform = (CosmosParallelCompuation)FlowHandler_Cosmos.ParaComputePlatform_Cosmos;
                        cosmosComputePlatform.NebularArgument = 1500000;
                        cosmosComputePlatform.JobScript = cosmosjob;
                        cosmosComputePlatform.JobName = "ExtractLpcJob";
                        cosmosComputePlatform.JobDisplayName = "ExtractLpcJob";
                        FlowHandler_Cosmos.ParaComputePlatform_Cosmos.Execute();
                    }

                    break;
                default:
                    throw new Exception("Only multithread and cosmos platform can be used currently, please check the platform you use!");
            }

            Logger.LogLine("Finish extracting lpc.");
        }

        private List<ParallelParameter> GenerateExtractLpcCommands()
        {
            List<ParallelParameter> parallelCommands = new List<ParallelParameter>();
            string of0Dir = Path.Combine(IntermediateDir, "lpc_of0");
            Helper.EnsureFolderExist(of0Dir);

            // extract direct features from wave.
            foreach (string sid in FileMap.Map.Keys)
            {
                // extract lpc features from one wave file.
                string waveFile = FileMap.BuildPath(WaveDir, sid, FileExtensions.Waveform);
                string f0File = FileMap.BuildPath(F0Dir, sid, FileExtensions.F0File);
                if (!File.Exists(f0File))
                {
                    Logger.LogLine("Fail to get f0 for sentence {0}", sid);
                    continue;
                }

                // output file.
                string lpcFile = FileMap.BuildPath(LPCDir, sid, FileExtensions.LpcFile);
                Helper.EnsureFolderExistForFile(lpcFile);
                string of0File = FileMap.BuildPath(of0Dir, sid, FileExtensions.F0File);
                Helper.EnsureFolderExistForFile(of0File);

                // parallel computing.
                ParallelCommand command = new ParallelCommand
                {
                    // mode = 16: dump plain text of lpc.
                    ExecutableFile = StraightTool,
                    Argument = GenerateExtractLpcArgument(waveFile, f0File, lpcFile, of0File, FFTDim, LpcOrder, SecondsPerFrame),
                    Description = Helper.NeutralFormat("Get lpc from sentence \"{0}\"", sid),
                    Logger = Logger,
                };

                parallelCommands.Add(command);
            }

            return parallelCommands;
        }

        /// <summary>
        /// Extract lpc residual error.
        /// </summary>
        /// <param name="ifCosmos">Represents if this is a Cosmos operation.</param>
        private void ExtractLpcResidualError(bool ifCosmos)
        {
            Logger.LogLine("Start to extract lpc residual error...");

            switch (FlowHandler_Cosmos.GetCurrentComputationPlatform(ifCosmos))
            {
                case ComputationPlatform.MultiThreadPlatform:
                    {
                        var mulComputePlatform = (MultiThreadParallelComputation)FlowHandler.ParaComputePlatform;
                        mulComputePlatform.ParallelParameters = GenerateExtractLpcResidualErrorCommands();
                        FlowHandler.ParaComputePlatform.Execute();
                    }

                    break;
                case ComputationPlatform.CosmosPlatform:
                    {
                        JobProcessor jb = new GetLpcResidualErrorJob(CosmosPath, FrameShift, FrameLength);
                        var cosmosjob = jb.Job.GenerateTemplate();
                        var cosmosComputePlatform = (CosmosParallelCompuation)FlowHandler_Cosmos.ParaComputePlatform_Cosmos;
                        cosmosComputePlatform.NebularArgument = 1500000;
                        cosmosComputePlatform.JobScript = cosmosjob;
                        cosmosComputePlatform.JobName = "ExtractLpcResidualErrorJob";
                        cosmosComputePlatform.JobDisplayName = "ExtractLpcResidualErrorJob";
                        FlowHandler_Cosmos.ParaComputePlatform_Cosmos.Execute();
                    }

                    break;
                default:
                    throw new Exception("Only multithread and cosmos platform can be used currently, please check the platform you use!");
            }

            Logger.LogLine("Finish extracting lpc residual error.");
        }

        private List<ParallelParameter> GenerateExtractLpcResidualErrorCommands()
        {
            List<ParallelParameter> parallelCommands = new List<ParallelParameter>();

            // extract direct features from wave
            foreach (string sid in FileMap.Map.Keys)
            {
                // process one wave file
                string waveFile = FileMap.BuildPath(WaveDir, sid, FileExtensions.Waveform);
                string lpcFile = FileMap.BuildPath(LPCDir, sid, FileExtensions.LpcFile);
                if (!File.Exists(lpcFile))
                {
                    Logger.LogLine("Fail to get lpc for sentence {0}", sid);
                    continue;
                }

                string errorFile = FileMap.BuildPath(ResidualDir, sid, FileExtensions.Text);
                Helper.EnsureFolderExistForFile(errorFile);

                string[] argument = { waveFile, lpcFile, errorFile, FrameShift.ToString(), FrameLength.ToString() };
                ParallelMethod<string[]> method = new ParallelMethod<string[]>
                {
                    Argument = argument,
                    CallBackMethod = ExtractLpcResidualErrorOneFile,
                    Description = Helper.NeutralFormat("Get lpc for sentence \"{0}\"", sid),
                    Logger = Logger,
                };

                parallelCommands.Add(method);
            }

            return parallelCommands;
        }

        /// <summary>
        /// Merge 6 features.
        /// </summary>
        /// <param name="ifCosmos">Represents if this is a Cosmos operation.</param>
        private void MergeFeatures(bool ifCosmos)
        {
            Logger.LogLine("Start to merge features...");

            switch (FlowHandler_Cosmos.GetCurrentComputationPlatform(ifCosmos))
            {
                case ComputationPlatform.MultiThreadPlatform:
                    {
                        var mulComputePlatform = (MultiThreadParallelComputation)FlowHandler.ParaComputePlatform;
                        mulComputePlatform.ParallelParameters = GenerateMergeFeatureCommands();
                        FlowHandler.ParaComputePlatform.Execute();
                    }

                    break;
                case ComputationPlatform.CosmosPlatform:
                    {
                        JobProcessor jb = new MergeFeatureJob(CosmosPath);
                        var cosmosjob = jb.Job.GenerateTemplate();
                        var cosmosComputePlatform = (CosmosParallelCompuation)FlowHandler_Cosmos.ParaComputePlatform_Cosmos;
                        cosmosComputePlatform.NebularArgument = 1500000;
                        cosmosComputePlatform.JobScript = cosmosjob;
                        cosmosComputePlatform.JobName = "MergeFeatureJob";
                        cosmosComputePlatform.JobDisplayName = "MergeFeatureJob";
                        FlowHandler_Cosmos.ParaComputePlatform_Cosmos.Execute();
                    }

                    break;
                default:
                    throw new Exception("Only multithread and cosmos platform can be used currently, please check the platform you use!");
            }

            Logger.LogLine("Finish merging features.");
        }

        private List<ParallelParameter> GenerateMergeFeatureCommands()
        {
            List<ParallelParameter> parallelCommands = new List<ParallelParameter>();
            foreach (string sid in FileMap.Map.Keys)
            {
                string relatedfeaFile = FileMap.BuildPath(RealtedFeaDir, sid, FileExtensions.Text);
                if (!File.Exists(relatedfeaFile))
                {
                    Logger.LogLine("Fail to get related features for sentence {0}", sid);
                    continue;
                }

                string residualFile = FileMap.BuildPath(ResidualDir, sid, FileExtensions.Text);
                if (!File.Exists(residualFile))
                {
                    Logger.LogLine("Fail to get lpc residual features for sentence {0}", sid);
                    continue;
                }

                string nccfFile = FileMap.BuildPath(NccfDir, sid, FileExtensions.F0File);
                if (!File.Exists(nccfFile))
                {
                    Logger.LogLine("Fail to get nccf features for sentence {0}", sid);
                    continue;
                }

                string mergedFeaFile = FileMap.BuildPath(MergedFeaDir, sid, FileExtensions.Text);
                Helper.EnsureFolderExistForFile(mergedFeaFile);

                string[] argument = { relatedfeaFile, residualFile, nccfFile, mergedFeaFile };
                ParallelMethod<string[]> method = new ParallelMethod<string[]>
                {
                    Argument = argument,
                    CallBackMethod = MergeFeaturesOneFile,
                    Description = Helper.NeutralFormat("Get merged features for sentence \"{0}\"", sid),
                    Logger = Logger,
                };

                parallelCommands.Add(method);
            }

            return parallelCommands;
        }

        /// <summary>
        /// Calculate expand and delta features.
        /// </summary>
        /// <param name="ifCosmos">Represents if this is a Cosmos operation.</param>
        private void ExpandFeatures(bool ifCosmos)
        {
            Logger.LogLine("Start to expand features...");

            if (Dimension == BasicDimension)
            {
                Helper.CopyDirectory(MergedFeaDir, ExpandFeaDir, true);
                return;
            }

            switch (FlowHandler_Cosmos.GetCurrentComputationPlatform(ifCosmos))
            {
                case ComputationPlatform.MultiThreadPlatform:
                    {
                        var mulComputePlatform = (MultiThreadParallelComputation)FlowHandler.ParaComputePlatform;
                        mulComputePlatform.ParallelParameters = GenerateExpandFeatureCommands();
                        FlowHandler.ParaComputePlatform.Execute();
                    }

                    break;
                case ComputationPlatform.CosmosPlatform:
                    {
                        JobProcessor jb = new ExpandFeatureJob(CosmosPath, Dimension);
                        var cosmosjob = jb.Job.GenerateTemplate();
                        var cosmosComputePlatform = (CosmosParallelCompuation)FlowHandler_Cosmos.ParaComputePlatform_Cosmos;
                        cosmosComputePlatform.NebularArgument = 1500000;
                        cosmosComputePlatform.JobScript = cosmosjob;
                        cosmosComputePlatform.JobName = "ExpandFeatureJob";
                        cosmosComputePlatform.JobDisplayName = "ExpandFeatureJob";
                        FlowHandler_Cosmos.ParaComputePlatform_Cosmos.Execute();
                    }

                    break;
                default:
                    throw new Exception("Only multithread and cosmos platform can be used currently, please check the platform you use!");
            }

            Logger.LogLine("Finish expanding features.");
        }

        private List<ParallelParameter> GenerateExpandFeatureCommands()
        {
            List<ParallelParameter> parallelCommands = new List<ParallelParameter>();
            foreach (string sid in FileMap.Map.Keys)
            {
                string basicFeaFile = FileMap.BuildPath(MergedFeaDir, sid, FileExtensions.Text);
                if (!File.Exists(basicFeaFile))
                {
                    Logger.LogLine("Fail to merge features for sentence {0}", sid);
                    continue;
                }

                string expandFeaFile = FileMap.BuildPath(ExpandFeaDir, sid, FileExtensions.Text);
                Helper.EnsureFolderExistForFile(expandFeaFile);

                string[] argument = { basicFeaFile, expandFeaFile, Dimension.ToString() };
                ParallelMethod<string[]> method = new ParallelMethod<string[]>
                {
                    Argument = argument,
                    CallBackMethod = ExpandFeaturesOneFile,
                    Description = Helper.NeutralFormat("Get merged features for sentence \"{0}\"", sid),
                    Logger = Logger,
                };

                parallelCommands.Add(method);
            }

            return parallelCommands;
        }

        /// <summary>
        /// Format features.
        /// </summary>
        /// <param name="ifCosmos">Represents if this is a Cosmos operation.</param>
        private void FormatFeatures(bool ifCosmos)
        {
            Logger.LogLine("Start to format features...");

            switch (FlowHandler_Cosmos.GetCurrentComputationPlatform(ifCosmos))
            {
                case ComputationPlatform.MultiThreadPlatform:
                    {
                        var mulComputePlatform = (MultiThreadParallelComputation)FlowHandler.ParaComputePlatform;
                        mulComputePlatform.ParallelParameters = GenerateFormatFeatureCommands();
                        FlowHandler.ParaComputePlatform.Execute();
                    }

                    break;
                case ComputationPlatform.CosmosPlatform:
                    {
                        JobProcessor jb = new FormatFeatureJob(CosmosPath);
                        var cosmosjob = jb.Job.GenerateTemplate();
                        var cosmosComputePlatform = (CosmosParallelCompuation)FlowHandler_Cosmos.ParaComputePlatform_Cosmos;
                        cosmosComputePlatform.NebularArgument = 1500000;
                        cosmosComputePlatform.JobScript = cosmosjob;
                        cosmosComputePlatform.JobName = "FormatFeatureJob";
                        cosmosComputePlatform.JobDisplayName = "FormatFeatureJob";
                        FlowHandler_Cosmos.ParaComputePlatform_Cosmos.Execute();
                    }

                    break;
                default:
                    throw new Exception("Only multithread and cosmos platform can be used currently, please check the platform you use!");
            }

            Logger.LogLine("Finish formating features.");
        }

        private List<ParallelParameter> GenerateFormatFeatureCommands()
        {
            List<ParallelParameter> parallelCommands = new List<ParallelParameter>();
            foreach (string sid in FileMap.Map.Keys)
            {
                string f0File = FileMap.BuildPath(F0Dir, sid, FileExtensions.F0File);
                if (!File.Exists(f0File))
                {
                    Logger.LogLine("Fail to get f0 file for sentence {0}", sid);
                    continue;
                }

                string expandFeaFile = FileMap.BuildPath(ExpandFeaDir, sid, FileExtensions.Text);
                if (!File.Exists(expandFeaFile))
                {
                    Logger.LogLine("Fail to get expanded feature file for sentence {0}", sid);
                    continue;
                }

                string svmFeaFile = FileMap.BuildPath(SvmFeaDir, sid, FileExtensions.Text);
                Helper.EnsureFolderExistForFile(svmFeaFile);

                string[] argument = { f0File, expandFeaFile, svmFeaFile };
                ParallelMethod<string[]> method = new ParallelMethod<string[]>
                {
                    Argument = argument,
                    CallBackMethod = FormatFeaturesOneFile,
                    Description = Helper.NeutralFormat("Get format features for sentence \"{0}\"", sid),
                    Logger = Logger,
                };

                parallelCommands.Add(method);
            }

            return parallelCommands;
        }

        /// <summary>
        /// Scale features.
        /// </summary>
        /// <param name="ifCosmos">Represents if this is a Cosmos operation.</param>
        private void ScaleFeatures(bool ifCosmos)
        {
            Logger.LogLine("Start to scale features...");

            string rangeFile = null;
            if (!EnableCosmos)
            {
                rangeFile = GetFeatureRange();
            }
            else
            {
                string svmScaleToolOnVC = TmocPath.Combine(CosmosPath, "Tools/SVM", Path.GetFileName(SvmScaleTool));
                if (!TmocFile.Exists(svmScaleToolOnVC))
                {
                    TmocFile.Copy(SvmScaleTool, svmScaleToolOnVC);
                }

                JobProcessor jb = new ConvertSVMToStreamJob(CosmosPath);
                var cosmosjob = jb.Job.GenerateTemplate();
                var cosmosComputePlatform = (CosmosParallelCompuation)FlowHandler_Cosmos.ParaComputePlatform_Cosmos;
                cosmosComputePlatform.NebularArgument = 1500000;
                cosmosComputePlatform.JobScript = cosmosjob;
                cosmosComputePlatform.JobName = "ConvertSVMToStreamJob";
                cosmosComputePlatform.JobDisplayName = "ConvertSVMToStreamJob";
                FlowHandler_Cosmos.ParaComputePlatform_Cosmos.Execute();

                rangeFile = PrepareFeatureRangeForCosmos();
                TmocFile.Copy(rangeFile, TmocPath.Combine(CosmosPath, "Data/SVM_all.range"));
            }

            switch (FlowHandler_Cosmos.GetCurrentComputationPlatform(ifCosmos))
            {
                case ComputationPlatform.MultiThreadPlatform:
                    {
                        var mulComputePlatform = (MultiThreadParallelComputation)FlowHandler.ParaComputePlatform;
                        mulComputePlatform.ParallelParameters = GenerateScaleFeatureCommands(rangeFile);
                        FlowHandler.ParaComputePlatform.Execute();
                    }

                    break;
                case ComputationPlatform.CosmosPlatform:
                    {
                        JobProcessor jb = new ScaleFeatureJob(CosmosPath);
                        var cosmosjob = jb.Job.GenerateTemplate();
                        var cosmosComputePlatform = (CosmosParallelCompuation)FlowHandler_Cosmos.ParaComputePlatform_Cosmos;
                        cosmosComputePlatform.NebularArgument = 1500000;
                        cosmosComputePlatform.JobScript = cosmosjob;
                        cosmosComputePlatform.JobName = "ScaleFeatureJob";
                        cosmosComputePlatform.JobDisplayName = "ScaleFeatureJob";
                        FlowHandler_Cosmos.ParaComputePlatform_Cosmos.Execute();
                    }

                    break;
                default:
                    throw new Exception("Only multithread and cosmos platform can be used currently, please check the platform you use!");
            }

            Logger.LogLine("Finish scaling features.");
        }

        private List<ParallelParameter> GenerateScaleFeatureCommands(string rangeFile)
        {
            List<ParallelParameter> parallelCommands = new List<ParallelParameter>();
            foreach (string sid in FileMap.Map.Keys)
            {
                // scale features by feature range file
                string feaFile = FileMap.BuildPath(SvmFeaDir, sid, FileExtensions.Text);
                if (!File.Exists(feaFile))
                {
                    Logger.LogLine("Fail to get svm features for sentence {0}", sid);
                    continue;
                }

                string scaledFeaFile = FileMap.BuildPath(ScaledSvmFeaDir, sid, FileExtensions.Text);
                Helper.EnsureFolderExistForFile(scaledFeaFile);
                Helper.SafeDelete(scaledFeaFile);

                ParallelCommand command = new ParallelCommand
                {
                    ExecutableFile = SvmScaleTool,
                    Argument = Helper.NeutralFormat(" -r \"{0}\" \"{1}\"", rangeFile, feaFile),
                    Description = Helper.NeutralFormat("Scale fea for sentence \"{0}\"", sid),
                    Logger = Logger,
                    ResultFileLogger = new TextLogger(scaledFeaFile)
                };

                parallelCommands.Add(command);
            }

            return parallelCommands;
        }

        private string PrepareFeatureRangeForCosmos()
        {
            string allFeaFile = Path.Combine(IntermediateDir, "SVM_all.txt");
            string rangeFile = Path.Combine(IntermediateDir, "SVM_all.range");
            string allScaledFile = Path.Combine(IntermediateDir, "SVM_all_scaled.txt");

            string svmListFile = Path.Combine(IntermediateDir, "InputWaveIDSVM.tsv");
            TmocFile.Copy(TmocPath.Combine(CosmosPath, "Data/InputWaveIDSVM.ss"), svmListFile);

            var svmFeatureList = File.ReadLines(svmListFile);
            using (StreamWriter sw = new StreamWriter(allFeaFile, false))
            {
                foreach (var svmFeature in svmFeatureList)
                {
                    var feature = svmFeature.Split(new char[] { '\t' });
                    string feaFile = JobBase.GenerateLocalFile(feature[0], TmocFile.NormalizeOutput(feature[1], false), FileExtensions.Text, false, SvmFeaDir);
                    foreach (string line in Helper.FileLines(feaFile))
                    {
                        sw.WriteLine(line);
                    }
                }
            }

            // get each features' range
            CommandLine.RunCommandToFile(SvmScaleTool, " -s " + Helper.FileToShortPath(rangeFile) + " " + Helper.FileToShortPath(allFeaFile),
                allScaledFile, false, System.Environment.CurrentDirectory);
            return rangeFile;
        }

        private string GetFeatureRange()
        {
            // get feature range
            // merge all features into one file
            string allFeaFile = Path.Combine(IntermediateDir, "SVM_all.txt");
            string rangeFile = Path.Combine(IntermediateDir, "SVM_all.range");
            string allScaledFile = Path.Combine(IntermediateDir, "SVM_all_scaled.txt");

            using (StreamWriter sw = new StreamWriter(allFeaFile, false))
            {
                foreach (string sid in FileMap.Map.Keys)
                {
                    // scale features by feature range file
                    string feaFile = FileMap.BuildPath(SvmFeaDir, sid, FileExtensions.Text);
                    if (!File.Exists(feaFile))
                    {
                        Logger.LogLine("Fail to get svm features for sentence {0}", sid);
                        continue;
                    }

                    foreach (string line in Helper.FileLines(feaFile))
                    {
                        sw.WriteLine(line);
                    }
                }
            }

            // get each features' range
            CommandLine.RunCommandToFile(SvmScaleTool, " -s " + Helper.FileToShortPath(rangeFile) + " " + Helper.FileToShortPath(allFeaFile),
                allScaledFile, false, System.Environment.CurrentDirectory);
            return rangeFile;
        }

        #endregion
    }
}
