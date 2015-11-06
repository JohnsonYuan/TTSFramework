//----------------------------------------------------------------------------
// <copyright file="SfsTool.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements class to manage SFS(Speech Filing System) tools
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.IO;
    using System.Security.Permissions;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Tts.Offline.Waveform;

    /// <summary>
    /// Sfs tool. SFS (Speech Filing System).
    /// </summary>
    public class SfsTool
    {
        #region Fields
        private string _sfsDir;
        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="SfsTool"/> class.
        /// </summary>
        /// <param name="sfsDir">SFS tools directory.</param>
        public SfsTool(string sfsDir)
        {
            if (string.IsNullOrEmpty(sfsDir))
            {
                _sfsDir = string.Empty;
            }
            else
            {
                _sfsDir = sfsDir;
            }
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets Find certian tool.
        /// </summary>
        /// <param name="name">Tool name.</param>
        /// <returns>Tool path.</returns>
        public string this[string name]
        {
            get
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw new ArgumentNullException("name");
                }

                return string.IsNullOrEmpty(_sfsDir) ? name : Path.Combine(_sfsDir, name);
            }
        }
        #endregion

        #region Static operations

        /// <summary>
        /// Resample 44k Hz Waveform files down to 16k Hz waveform files.
        /// </summary>
        /// <param name="mapFile">Map file list.</param>
        /// <param name="wave44kDir">44k Hz Waveform directory.</param>
        /// <param name="wave16kDir">16k Hz Waveform directory.</param>
        /// <param name="sfsToolsDir">SFS tools directory.</param>
        /// <param name="skipExist">Skip if the target file already exists.</param>
        /// <returns>Skipped sentence id collection.</returns>
        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        public static Collection<string> Resample(string mapFile,
            string wave44kDir, string wave16kDir, string sfsToolsDir,
            bool skipExist)
        {
            if (!Directory.Exists(wave16kDir))
            {
                Directory.CreateDirectory(wave16kDir);
            }

            Collection<string> skipedIds = new Collection<string>();
            Dictionary<string, string> fileMap = FileListMap.ReadAllData(mapFile);
            SfsTool sfs = new SfsTool(sfsToolsDir);

            // go throught each item in the filemap
            foreach (string sid in fileMap.Keys)
            {
                // prepare the absoluted file path
                string wave44kFile = Path.Combine(wave44kDir, fileMap[sid] + ".wav");
                if (!File.Exists(wave44kFile))
                {
                    throw Helper.CreateException(typeof(FileNotFoundException),
                        wave44kFile);
                }

                string wave16kFilePath = Path.Combine(wave16kDir, fileMap[sid] + ".wav");

                if (File.Exists(wave16kFilePath) && skipExist)
                {
                    skipedIds.Add(sid);
                    continue;
                }

                Helper.EnsureFolderExistForFile(wave16kFilePath);
                Helper.TestWritable(wave16kFilePath);

                // do resample
                sfs.Resample(wave44kFile, wave16kFilePath);
            }

            return skipedIds;
        }

        /// <summary>
        /// Calculate the fundamental frequency track from waveform file.
        /// The detailed feature information will be saved in the temprorary file
        /// Path, and the path will be returned to the caller.
        /// </summary>
        /// <param name="wave16kFile">Waveform file to calculate track.</param>
        /// <param name="sfsToolPath">SFS tools used to perform calculation.</param>
        /// <returns>Fundamental frequency track file.</returns>
        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        public static string FundamentalFrequencyTrack(string wave16kFile,
            string sfsToolPath)
        {
            if (string.IsNullOrEmpty(wave16kFile))
            {
                throw new ArgumentNullException("wave16kFile");
            }

            if (string.IsNullOrEmpty(sfsToolPath))
            {
                throw new ArgumentNullException("sfsToolPath");
            }

            if (!File.Exists(wave16kFile))
            {
                throw Helper.CreateException(typeof(FileNotFoundException),
                    wave16kFile);
            }

            string tempsfsfile = Helper.GetTempFileName();
            Helper.SafeDelete(tempsfsfile);
            SfsTool sfs = new SfsTool(sfsToolPath);
            CommandLine.RunCommandWithOutput(sfs["cnv2sfs.exe"],
                " \"" + wave16kFile + "\" \"" + tempsfsfile + "\"",
                System.Environment.CurrentDirectory);

            CommandLine.RunCommandWithOutput(sfs["fxrapt.exe"],
                "-i sp \"" + tempsfsfile + "\"",
                System.Environment.CurrentDirectory);

            string outputFile = Helper.GetTempFileName();
            CommandLine.RunCommandWithOutput(sfs["fxlist.exe"],
                "-i 4.01 -o  \"" + outputFile + "\" \"" + tempsfsfile + "\"",
                System.Environment.CurrentDirectory);

            File.Delete(tempsfsfile);
            return outputFile;
        }

        #endregion

        #region Operations

        /// <summary>
        /// Resample waveform file.
        /// </summary>
        /// <param name="sourceFilePath">Source waveform file, in 44KHz.</param>
        /// <param name="targetFilePath">Output waveform file, in 16KHz.</param>
        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        public void Resample(string sourceFilePath, string targetFilePath)
        {
            const int TargetSamplesPerSecond = 16000;

            if (!File.Exists(sourceFilePath))
            {
                throw new ArgumentException(sourceFilePath, new FileNotFoundException(sourceFilePath));
            }

            if (string.IsNullOrEmpty(targetFilePath))
            {
                throw new ArgumentNullException(targetFilePath);
            }

            // validate data before resampling,
            // if failed, InvalidDataException will be threw out
            {
                WaveFile wf = new WaveFile();
                wf.Load(sourceFilePath);
                if (wf.Format.SamplesPerSecond == TargetSamplesPerSecond)
                {
                    // not need resampling, just do copy
                    File.Copy(sourceFilePath, targetFilePath, true);
                    return;
                }
            }

            string tempsfsfile = Helper.GetTempFileName() + ".sfs";
            if (File.Exists(tempsfsfile))
            {
                File.Delete(tempsfsfile);
            }

            // prepare SFS file
            string arg = string.Format(CultureInfo.InvariantCulture,
                "\"{0}\" \"{1}\"", sourceFilePath, tempsfsfile);
            CommandLine.RunCommandWithOutput(this["cnv2sfs.exe"],
                arg, System.Environment.CurrentDirectory);

            // re-sample the wave channel
            Helper.EnsureFolderExistForFile(targetFilePath);
            arg = string.Format(CultureInfo.InvariantCulture,
                "-i sp -f {0} \"{1}\"", TargetSamplesPerSecond, tempsfsfile);
            CommandLine.RunCommandWithOutput(this["resamp.exe"],
                arg, System.Environment.CurrentDirectory);

            arg = string.Format(CultureInfo.InvariantCulture,
                "-i sp -o  \"{0}\" \"{1}\"", targetFilePath, tempsfsfile);
            CommandLine.RunCommandWithOutput(this["sfs2wav.exe"],
                arg, System.Environment.CurrentDirectory);

            if (File.Exists(tempsfsfile))
            {
                File.Delete(tempsfsfile);
            }
        }

        /// <summary>
        /// Build annotation SFS file with waveform file and annotation file.
        /// </summary>
        /// <param name="sfsFilePath">SFS file.</param>
        /// <param name="waveformFilePath">Waveform file.</param>
        /// <param name="annotationFilePath">Annotation file.</param>
        /// <param name="type">Annotation type label.</param>
        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        public void BuildAnnotation(string sfsFilePath,
            string waveformFilePath, string annotationFilePath, string type)
        {
            // Parameters validation
            if (!File.Exists(waveformFilePath))
            {
                throw Helper.CreateException(typeof(FileNotFoundException),
                    waveformFilePath);
            }

            if (!File.Exists(annotationFilePath))
            {
                throw Helper.CreateException(typeof(FileNotFoundException),
                    annotationFilePath);
            }

            if (string.IsNullOrEmpty(type))
            {
                throw new ArgumentNullException("type");
            }

            if (File.Exists(sfsFilePath))
            {
                File.Delete(sfsFilePath);
            }

            Helper.EnsureFolderExistForFile(sfsFilePath);
            CommandLine.RunCommandWithOutput(this["cnv2sfs.exe"],
                "\"" + waveformFilePath + "\" \"" + sfsFilePath + "\"",
                System.Environment.CurrentDirectory);
            CommandLine.RunCommandWithOutput(this["anload.exe"],
                "-t " + type + " \"" + annotationFilePath + "\" \"" + sfsFilePath + "\"",
                System.Environment.CurrentDirectory);
        }

        /// <summary>
        /// Create an empty sfs file.
        /// </summary>
        /// <param name="sfsFilePath">Sfs file path.</param>
        /// <param name="overwrite">Whether to overwrite the existing file.</param>
        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        public void CreateSfsFile(string sfsFilePath, bool overwrite)
        {
            if (string.IsNullOrEmpty(sfsFilePath))
            {
                throw new ArgumentNullException("sfsFilePath");
            }

            if (File.Exists(sfsFilePath))
            {
                if (overwrite)
                {
                    Helper.SafeDelete(sfsFilePath);
                }
                else
                {
                    throw new InvalidOperationException(Helper.NeutralFormat("File [{0}} already exists", sfsFilePath));
                }
            }

            Helper.EnsureFolderExistForFile(sfsFilePath);
            string arguments = string.Format(CultureInfo.InvariantCulture, "-n \"{0}\"", sfsFilePath);

            CommandLine.RunCommandWithOutput(this["hed.exe"], arguments, System.Environment.CurrentDirectory);
        }

        /// <summary>
        /// Load audio file to sfs file
        /// Note that cnv2sfs.exe will always generate a new sfs file.
        /// </summary>
        /// <param name="audioFile">Audio file path.</param>
        /// <param name="sfsFile">Sfs file path.</param>
        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        public void LoadAudioFile(string audioFile, string sfsFile)
        {
            if (string.IsNullOrEmpty(audioFile))
            {
                throw new ArgumentNullException("audioFile");
            }

            if (string.IsNullOrEmpty(sfsFile))
            {
                throw new ArgumentNullException("sfsFile");
            }

            if (!File.Exists(audioFile))
            {
                throw Helper.CreateException(typeof(FileNotFoundException), audioFile);
            }

            string arguments = string.Format(CultureInfo.InvariantCulture, "\"{0}\" \"{1}\"",
                audioFile, sfsFile);
            if (File.Exists(sfsFile))
            {
                Helper.SafeDelete(sfsFile);
            }

            CommandLine.RunCommandWithOutput(this["cnv2sfs.exe"], arguments, System.Environment.CurrentDirectory);
        }

        /// <summary>
        /// Load annotation file to sfs file.
        /// </summary>
        /// <param name="annotationFile">Annotation file.</param>
        /// <param name="sfsFile">Sfs file.</param>
        /// <param name="type">Annotation type(header).</param>
        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        public void LoadAnnotationFile(string annotationFile, string sfsFile, string type)
        {
            if (string.IsNullOrEmpty(annotationFile))
            {
                throw new ArgumentNullException("annotationFile");
            }

            if (string.IsNullOrEmpty(sfsFile))
            {
                throw new ArgumentNullException("sfsFile");
            }

            if (!File.Exists(annotationFile))
            {
                throw Helper.CreateException(typeof(FileNotFoundException), annotationFile);
            }

            if (!File.Exists(sfsFile))
            {
                throw Helper.CreateException(typeof(FileNotFoundException), sfsFile);
            }

            string typeArgument = string.Empty;
            if (!string.IsNullOrEmpty(type))
            {
                typeArgument = string.Format(CultureInfo.InvariantCulture, "-t \"{0}\"", type);
            }

            string arguments = string.Format(CultureInfo.InvariantCulture, "{0} \"{1}\" \"{2}\"",
                typeArgument, annotationFile, sfsFile);
            CommandLine.RunCommandWithOutput(this["anload.exe"], arguments, System.Environment.CurrentDirectory);
        }

        /// <summary>
        /// Load wave file to sfs file
        /// Note that cnv2sfs.exe will always generate a new sfs file.
        /// </summary>
        /// <param name="waveFile">Audio file path.</param>
        /// <param name="sfsFile">Sfs file path.</param>
        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        public void ImportWavFile(string waveFile, string sfsFile)
        {
            if (string.IsNullOrEmpty(waveFile))
            {
                throw new ArgumentNullException("audioFile");
            }

            if (string.IsNullOrEmpty(sfsFile))
            {
                throw new ArgumentNullException("sfsFile");
            }

            if (!File.Exists(waveFile))
            {
                throw Helper.CreateException(typeof(FileNotFoundException), waveFile);
            }

            if (!File.Exists(sfsFile))
            {
                throw Helper.CreateException(typeof(FileNotFoundException), sfsFile);
            }

            string arguments = string.Format(CultureInfo.InvariantCulture, "-isp -tWAV \"{0}\" \"{1}\"",
                waveFile, sfsFile);

            CommandLine.RunCommandWithOutput(this["slink.exe"], arguments, System.Environment.CurrentDirectory);
        }

        /// <summary>
        /// Load fundamental frequency to sfs file .
        /// </summary>
        /// <param name="fxFile">Fundamental frequency file.</param>
        /// <param name="sfsFile">Sfs file.</param>
        /// <param name="frequency">Frequency.</param>
        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        public void LoadFxFile(string fxFile, string sfsFile, float frequency)
        {
            if (string.IsNullOrEmpty(fxFile))
            {
                throw new ArgumentNullException("fxFile");
            }

            if (string.IsNullOrEmpty(sfsFile))
            {
                throw new ArgumentNullException("sfsFile");
            }

            if (!File.Exists(fxFile))
            {
                throw Helper.CreateException(typeof(FileNotFoundException), fxFile);
            }

            if (!File.Exists(sfsFile))
            {
                throw Helper.CreateException(typeof(FileNotFoundException), sfsFile);
            }

            if (!(frequency > 0))
            {
                throw new ArgumentOutOfRangeException("frequency", "Frequency can only be positive.");
            }

            string arguments = string.Format(CultureInfo.InvariantCulture, "-f {0} \"{1}\" \"{2}\"",
                frequency, fxFile, sfsFile);
            CommandLine.RunCommandWithOutput(this["fxload.exe"], arguments, System.Environment.CurrentDirectory);
        }

        #endregion
    }
}