//----------------------------------------------------------------------------
// <copyright file="FontHelper.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements FontHelper
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Test
{
    using System;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Security.Permissions;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Config;
    using Microsoft.Tts.Offline.Utility;
    using Offline = Microsoft.Tts.Offline;

    /// <summary>
    /// Font helper class.
    /// </summary>
    [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
    public static class FontHelper
    {
        /// <summary>
        /// Prepare voice font used by test.
        /// </summary>
        /// <param name="outputDir">Voice font output dir.</param>
        /// <returns>Font version information.</returns>
        public static FontVersion PrepareVoiceFontEnUSTom(string outputDir)
        {
            Helper.EnsureFolderExist(outputDir);
            string tomDataSite = Path.Combine(Helper.FindTestDataPath(), @"en-US\Tom\DataFiles");
            FontVersion fontVersion = new FontVersion("M1033SVR", WaveCompressCatalog.Unc,
                Offline.Waveform.WaveFormatTag.Mulaw, 8000, 1);

            Collection<FontVersion> fontVersions = new Collection<FontVersion>();
            fontVersions.Add(fontVersion);

            PrepareVoiceFont(Language.EnUS, string.Empty, string.Empty, null, fontVersions,
                Path.Combine(tomDataSite, "Script.txt"), Path.Combine(tomDataSite, "Segment"),
                Path.Combine(tomDataSite, "Wave16k"), Path.Combine(tomDataSite, "Wave16kFiltered"),
                string.Empty,
                Path.Combine(tomDataSite, "Epoch"), outputDir);
            return fontVersion;
        }

        /// <summary>
        /// Prepare voice font used by test.
        /// </summary>
        /// <param name="outputDir">Voice font output dir.</param>
        /// <returns>Font version information.</returns>
        public static FontVersion PrepareVoiceFontEnUSSamantha(string outputDir)
        {
            Helper.EnsureFolderExist(outputDir);
            string samanthaDataSite = Path.Combine(Helper.FindTestDataPath(), @"en-US\Samantha\DataFiles");
            FontVersion fontVersion = new FontVersion("M1033SVR", WaveCompressCatalog.Unc,
                Offline.Waveform.WaveFormatTag.Mulaw, 8000, 1);

            Collection<FontVersion> fontVersions = new Collection<FontVersion>();
            fontVersions.Add(fontVersion);

            string unitUnitTable = Localor.GetLanguageDataFile(Language.EnUS, Localor.UnitTableFileName);
            string phoneUnitTable = Path.Combine(Helper.FindTestDataPath(),
                @"en-US\Samantha\DataFiles\Meta\PhoneUnitTable.xml");
            Localor.SetLanguageDataFile(Language.EnUS, Localor.UnitTableFileName, phoneUnitTable);

            PrepareVoiceFont(Language.EnUS, string.Empty, string.Empty, null, fontVersions,
                Path.Combine(samanthaDataSite, @"Script\Script.txt"), Path.Combine(samanthaDataSite, @"Alignment\PhoneSegment"),
                Path.Combine(samanthaDataSite, @"Speech\Wave16k"), Path.Combine(samanthaDataSite, @"Speech\Wave16kFiltered"),
                string.Empty, Path.Combine(samanthaDataSite, @"Speech\Epoch"), outputDir);

            if (string.IsNullOrEmpty(unitUnitTable))
            {
                Localor.ClearLanguageDataFile(Language.EnUS, Localor.UnitTableFileName);
            }
            else
            {
                Localor.SetLanguageDataFile(Language.EnUS, Localor.UnitTableFileName, unitUnitTable);
            }

            return fontVersion;
        }

        /// <summary>
        /// Run FontCompiler.
        /// </summary>
        /// <param name="language">Language to be processed.</param>
        /// <param name="languageDataDir">Language data dir.</param>
        /// <param name="voiceDataDir">Voice data dir.</param>
        /// <param name="importList">Import list.</param>
        /// <param name="fontVersions">Font versions.</param>
        /// <param name="scriptPath">Script file path, can be eighter file or dir.</param>
        /// <param name="alignmentDir">Alignment dir.</param>
        /// <param name="wave16kDir">Wave16k dir.</param>
        /// <param name="wave16kFilteredDir">Wave16k filtered dir.</param>
        /// <param name="dropUnitList">Drop unit list file path.</param>
        /// <param name="epochDir">Epoch dir.</param>
        /// <param name="outputDir">Output font dir.</param>
        private static void PrepareVoiceFont(Language language,
            string languageDataDir, string voiceDataDir,
            Collection<string> importList, Collection<FontVersion> fontVersions,
            string scriptPath, string alignmentDir,
            string wave16kDir, string wave16kFilteredDir,
            string dropUnitList,
            string epochDir, string outputDir)
        {
            // Validation
            if (!string.IsNullOrEmpty(languageDataDir) && !Directory.Exists(languageDataDir))
            {
                throw Helper.CreateException(typeof(DirectoryNotFoundException), languageDataDir);
            }

            if (!string.IsNullOrEmpty(voiceDataDir) && !Directory.Exists(voiceDataDir))
            {
                throw Helper.CreateException(typeof(DirectoryNotFoundException), voiceDataDir);
            }

            if (!File.Exists(scriptPath) && !Directory.Exists(scriptPath))
            {
                throw new ArgumentException(Helper.NeutralFormat(
                    "Invalid script dir or file [{0}]", scriptPath));
            }

            if (!Directory.Exists(alignmentDir))
            {
                throw Helper.CreateException(typeof(DirectoryNotFoundException), alignmentDir);
            }

            if (!Directory.Exists(wave16kDir))
            {
                throw Helper.CreateException(typeof(DirectoryNotFoundException), wave16kDir);
            }

            if (!Directory.Exists(wave16kFilteredDir))
            {
                throw Helper.CreateException(typeof(DirectoryNotFoundException), wave16kFilteredDir);
            }

            if (!Directory.Exists(epochDir))
            {
                throw Helper.CreateException(typeof(DirectoryNotFoundException), epochDir);
            }

            if (fontVersions == null || fontVersions.Count == 0)
            {
                throw new ArgumentNullException("fontVersions");
            }

            // Build FontCompiler config
            FontCompilerConfig fontCompilerConfig = new FontCompilerConfig();
            fontCompilerConfig.Language = language;
            if (!string.IsNullOrEmpty(languageDataDir))
            {
                fontCompilerConfig.LanguageData.DataDir = languageDataDir;
                fontCompilerConfig.LanguageData.PhoneSet = Localor.PhoneSetFileName;
                fontCompilerConfig.LanguageData.TtsToSapiVisemeId = Localor.TtsToSapiVisemeIdFileName;
                fontCompilerConfig.LanguageData.UnitTable = Localor.UnitTableFileName;
                fontCompilerConfig.LanguageData.CartQuestions = "WholeCartQuestionSet.txt";
                if (File.Exists(Path.Combine(languageDataDir, Localor.FontMetaFileName)))
                {
                    fontCompilerConfig.LanguageData.FontMeta = Localor.FontMetaFileName;
                }
            }

            if (!string.IsNullOrEmpty(voiceDataDir))
            {
                fontCompilerConfig.VoiceDataDir = voiceDataDir;
            }

            foreach (FontVersion version in fontVersions)
            {
                fontCompilerConfig.Versions.Add(version);
            }

            // Only generate wave16kNorm or wave16kFilteredNorm bad unit list or
            // user specify other bad unit list, then all bad unit list will be
            // merge to this "dropUnitList" file.
            if (!string.IsNullOrEmpty(dropUnitList) && File.Exists(dropUnitList))
            {
                fontCompilerConfig.DropUnitList = dropUnitList;
            }

            if (importList != null)
            {
                fontCompilerConfig.Import.AddRange(importList);
            }

            fontCompilerConfig.ScriptPath = scriptPath;
            fontCompilerConfig.AlignmentDir = alignmentDir;
            fontCompilerConfig.Wave16kDir = wave16kDir;
            fontCompilerConfig.Wave16kFilteredDir = wave16kFilteredDir;
            fontCompilerConfig.EpochDir = epochDir;
            fontCompilerConfig.OutputDir = outputDir;

            string toolWorkSite = Helper.FindTestRootPath();
            fontCompilerConfig.WorksiteDir = toolWorkSite;

            string configFilePath = Helper.TempFullPath;

            fontCompilerConfig.Save(configFilePath);

            string[] arguments = new string[]
            {
                "-config", configFilePath
            };

            string toolPath = Path.Combine(toolWorkSite, "FontCompiler.exe");
            CommandLine.SuccessRunCommand(toolPath, Helper.BuildArgument(arguments),
                Environment.CurrentDirectory);
        }
    }
}