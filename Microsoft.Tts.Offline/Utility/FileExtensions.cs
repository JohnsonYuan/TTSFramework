//----------------------------------------------------------------------------
// <copyright file="FileExtensions.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module defines class FileExtensions.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Utility
{
    using System;
    using System.IO;

    /// <summary>
    /// This class is used to hold and process the file extension names.
    /// </summary>
    public static class FileExtensions
    {
        #region Constants

        /// <summary>
        /// File extension delimeter.
        /// </summary>
        public const char FileExtensionDelimeter = '.';

        /// <summary>
        /// The extension name of the initial f0 file.
        /// </summary>
        public const string F0File = "f0";

        /// <summary>
        /// The extension name of the lsp file.
        /// </summary>
        public const string LspFile = "lsp";

        /// <summary>
        /// The extension name of the lpc file.
        /// </summary>
        public const string LpcFile = "lpc";

        /// <summary>
        /// The extension name of the lpcc file.
        /// </summary>
        public const string LpccFile = "lpcc";

        /// <summary>
        /// The extension name of the power file.
        /// </summary>
        public const string PowerFile = "pow";

        /// <summary>
        /// The extension name of the guidanceLsp file.
        /// </summary>
        public const string GuidanceLspFile = "glsp";

        /// <summary>
        /// The extension name of the cmp data file.
        /// </summary>
        public const string CmpDataFile = "cmp";

        /// <summary>
        /// The extension name of the Htk format parameter file.
        /// </summary>
        public const string HtkParameterFile = "hpf";

        /// <summary>
        /// The extension name of the master label file.
        /// </summary>
        public const string HtkMasterLabel = "mlf";

        /// <summary>
        /// The extension name of the full list file.
        /// </summary>
        public const string HtkHmmList = "list";

        /// <summary>
        /// The extension name of the script file.
        /// </summary>
        public const string HtkScript = "scp";

        /// <summary>
        /// The extension name of the HHEd script file.
        /// </summary>
        public const string HedScript = "hed";

        /// <summary>
        /// The extension name of the master macro file.
        /// </summary>
        public const string HtkMasterMacro = "mmf";

        /// <summary>
        /// The extension name of the cmp file, which contains the all acoustic data.
        /// </summary>
        public const string HtkCmp = "cmp";

        /// <summary>
        /// The extension name of the statistic file, which contains the statistic data.
        /// </summary>
        public const string HtkStatisticFile = "sts";

        /// <summary>
        /// The extension name of the decision tree file, which contains the decision tree structure.
        /// </summary>
        public const string HtkTree = "inf";

        /// <summary>
        /// The extension name of the candidate group file, which contains the candidate group data.
        /// </summary>
        public const string CandidateGroup = "cgf";

        /// <summary>
        /// The extension name of wave files.
        /// </summary>
        public const string Waveform = "wav";

        /// <summary>
        /// The extension name of the unit indexing file, which contains the indexing information of the units.
        /// </summary>
        public const string UnitIndexing = "unt";

        /// <summary>
        /// The extension name of the wave info header file, which contains the wave information header.
        /// </summary>
        public const string WaveInfoHeader = "wih";

        /// <summary>
        /// The extension name of the wave alignment file, which contains the segmentation of source units.
        /// </summary>
        public const string WaveAlignment = "alg";

        /// <summary>
        /// The extension name of the wave inventory file, which contains the uncompressed wave data of the all candidates.
        /// </summary>
        public const string WaveInventory = "wve";

        /// <summary>
        /// The extension name of the CC table file, which contains the cross correlation cost.
        /// </summary>
        public const string CcTable = "cct";

        /// <summary>
        /// The extension name of the cross domain CC table file, which contains the cross correlation cost of candidates in different domains.
        /// </summary>
        public const string CrossDomainCCTable = "dct";

        /// <summary>
        /// The extension name of the KLD table file, which contains the KLD cost.
        /// </summary>
        public const string KldTable = "klt";

        /// <summary>
        /// The extension name of the pre-selection data file, which contains the pre-selection data.
        /// </summary>
        public const string Preselection = "pst";

        /// <summary>
        /// The extension name of acoustic parameter model, which contains acoustic parameter model data.
        /// </summary>
        public const string AcousticParameterModel = "apm";

        /// <summary>
        /// The extension name of acoustic transform model, which contains acoustic parameter model data.
        /// </summary>
        public const string AcousticTransformModel = "atm";

        /// <summary>
        /// The extension name of the model wave mapping file, which contains the information for debugging.
        /// </summary>
        public const string ModelWaveMapping = "mwm";

        /// <summary>
        /// The extension name of the transform matrix and HMM model mapping file, which contains information for voice adaptation debugging.
        /// </summary>
        public const string TransformModelMapping = "tmm";

        /// <summary>
        /// The extension name of label file, which contains segmentation information.
        /// </summary>
        public const string LabelFile = "lab";

        /// <summary>
        /// The extension name of backend parameter file, which contains any table for backend.
        /// </summary>
        public const string BackendParameterFile = "BEP";

        /// <summary>
        /// The extension name of transform data file.
        /// </summary>
        public const string TransformData = "xform";

        /// <summary>
        /// The extension name of voice font enviroment file.
        /// </summary>
        public const string EnviromentFile = "ENV";

        /// <summary>
        /// The extension name of md5 list file.
        /// </summary>
        public const string Md5FileList = "md5list";

        /// <summary>
        /// The extension name of plain text file.
        /// </summary>
        public const string Text = "txt";

        /// <summary>
        /// The extension name of XMl file.
        /// </summary>
        public const string Xml = "xml";

        /// <summary>
        /// The extension name of configuration file.
        /// </summary>
        public const string Configuration = "config";

        /// <summary>
        /// The extension name of configuration file.
        /// </summary>
        public const string SpeechFilingSystem = "sfs";

        /// <summary>
        /// The extension name of voice font Common phrase file.
        /// </summary>
        public const string CommonPhraseFile = "cmp";

        /// <summary>
        /// The extension name of CSV file.
        /// </summary>
        public const string CsvFile = "csv";

        /// <summary>
        /// The extension name of excel file.
        /// </summary>
        public const string ExcelFile = "xlsx";

        /// <summary>
        /// The extension name of break index ToBI annotation file.
        /// </summary>
        public const string ToBIBreakIndexFile = "bi";

        /// <summary>
        /// The extension name of boundary tone annotation file.
        /// </summary>
        public const string ToBIToneFile = "tobi";

        /// <summary>
        /// The extension name of acoustic data table file.
        /// </summary>
        public const string AcousticTableFile = "acd";
        
        /// <summary>
        /// The extension name of wave suffer phone segment file.
        /// </summary>
        public const string ToBIWaveSufferPhoneSegFile = "ph";

        /// <summary>
        /// The extension name of wave suffer syllable segment file.
        /// </summary>
        public const string ToBIWaveSufferSyllableSegFile = "syl";

        /// <summary>
        /// The extension name of wave suffer word segment file.
        /// </summary>
        public const string ToBIWaveSufferWordSegFile = "words";

        /// <summary>
        /// The extension name of offline configuration file.
        /// </summary>
        public const string OfflineConfigurationFile = "cfg";

        /// <summary>
        /// The extension name of wave suffer marked file.
        /// </summary>
        public const string ToBIWaveSufferMarkedFile = "lab";

        /// <summary>
        /// The extension name of ToBI annotation pack.
        /// </summary>
        public const string ToBIAnnotationPackFile = "tpk";

        /// <summary>
        /// The extension name of speech recognition lattice file.
        /// </summary>
        public const string LatticeFile = "lat";

        /// <summary>
        /// The extension name of speech recognition result file.
        /// </summary>
        public const string RecognitionFile = "rec";

        /// <summary>
        /// The extension name of histogram equalization table file.
        /// </summary>
        public const string HeqFile = "heq";

        /// <summary>
        /// The extension name of pitch marker file.
        /// </summary>
        public const string PitchMarkerFile = "pitchmarker";

        /// <summary>
        /// The extension name of NN model, which contains NN model data.
        /// </summary>
        public const string NNModel = "nnm";

        #endregion

        #region Methods

        /// <summary>
        /// Ensure file extension without delimeter.
        /// </summary>
        /// <param name="extension">The given extension name.</param>
        /// <returns>File extension with delimeter.</returns>
        public static string EnsureExtensionWithoutDelimeter(this string extension)
        {
            string extensionWithoutDelimeter = string.Empty;
            if (!string.IsNullOrEmpty(extension))
            {
                if (extension[0] == FileExtensionDelimeter)
                {
                    extensionWithoutDelimeter = extension.Substring(1);
                }
                else
                {
                    extensionWithoutDelimeter = extension;
                }
            }

            return extensionWithoutDelimeter;
        }

        /// <summary>
        /// Ensure file extension with delimeter.
        /// </summary>
        /// <param name="extension">The given extension name.</param>
        /// <returns>File extension with delimeter.</returns>
        public static string EnsureExtensionWithDelimeter(this string extension)
        {
            string extensionWithDelimeter = extension;
            if (!string.IsNullOrEmpty(extension))
            {
                if (extension[0] != FileExtensionDelimeter)
                {
                    extensionWithDelimeter = FileExtensionDelimeter + extension;
                }
                else
                {
                    extensionWithDelimeter = extension;
                }
            }

            return extensionWithDelimeter;
        }

        /// <summary>
        /// Appends the given extension name to the file.
        /// </summary>
        /// <param name="file">The given file name without extension name.</param>
        /// <param name="extensionName">The given extension name.</param>
        /// <returns>The file name with given extension name.</returns>
        public static string AppendExtensionName(this string file, string extensionName)
        {
            return (string.IsNullOrEmpty(extensionName) || extensionName[0] == FileExtensionDelimeter) ? file + extensionName : file + FileExtensionDelimeter + extensionName;
        }

        /// <summary>
        /// Appends the given extension name to the file.
        /// </summary>
        /// <param name="file">The given file name without extension name.</param>
        /// <param name="extensionName">The given extension name.</param>
        /// <returns>Whether the file with the specified extension.</returns>
        public static bool IsWithFileExtension(this string file, string extensionName)
        {
            Helper.ThrowIfNull(file);
            Helper.ThrowIfNull(extensionName);

            if (extensionName[0] != FileExtensionDelimeter)
            {
                extensionName = FileExtensionDelimeter + extensionName;
            }

            return file.EndsWith(extensionName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Create search pattern with file extension.
        /// </summary>
        /// <param name="fileExtension">The given file name without extension name.</param>
        /// <returns>Whether the file with the specified extension.</returns>
        public static string CreateSearchPatternWithFileExtension(this string fileExtension)
        {
            Helper.ThrowIfNull(fileExtension);
            return "*".AppendExtensionName(fileExtension);
        }

        /// <summary>
        /// Remove file path extension.
        /// </summary>
        /// <param name="filePath">File path.</param>
        /// <returns>File path without extension.</returns>
        public static string RemoveFilePathExtension(this string filePath)
        {
            Helper.ThrowIfNull(filePath);
            return Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath));
        }

        #endregion
    }
}
