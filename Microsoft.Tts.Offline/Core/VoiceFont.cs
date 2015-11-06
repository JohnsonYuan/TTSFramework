//----------------------------------------------------------------------------
// <copyright file="VoiceFont.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements voice font class
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Xml;
    using Microsoft.Tts.Offline.Cart;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.Utility;
    using Microsoft.Tts.Offline.Waveform;

    /// <summary>
    /// Voice font data.
    /// </summary>
    public class VoiceFont
    {
        #region Fields

        /// <summary>
        /// Message id of voice font loading status changed event .
        /// </summary>
        public const int LoadingStatusChanged = 0x0403;

        private IntPtr _handleForMsg = IntPtr.Zero;

        private FileListMap _fileMap;
        private ScriptFile _scriptFile;

        private Dictionary<string, TtsUtterance> _utterances;

        private Dictionary<long, WaveUnit> _waveUnits = new Dictionary<long, WaveUnit>();
        private string _unitFeatureFilePath;

        private Collection<string> _segmentDirectories = new Collection<string>();
        private Collection<string> _wave16kDirectories = new Collection<string>();
        private WeightTable _weightTable;
        private CartTreeManager _cartTreeManager;
        private string _tokenId;
        private string _voiceName;
        private Language _primaryLanguage;
        private EngineType _engineType;

        private string _fontPath;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="VoiceFont"/> class.
        /// </summary>
        /// <param name="hwnd">Window handle to recieve loading event.</param>
        public VoiceFont(IntPtr hwnd)
        {
            DataLoadingStatusChanged =
                delegate
                {
                };

            _handleForMsg = hwnd;

            this.DataLoadingStatusChanged +=
                new EventHandler<DataLoadingEventArgs>(DataManager_DataLoadingStatusChanged);
        }

        #endregion

        #region Events

        /// <summary>
        /// Data loading status changed event.
        /// </summary>
        public event EventHandler<DataLoadingEventArgs> DataLoadingStatusChanged;

        #endregion

        #region Enum

        /// <summary>
        /// Font file type.
        /// </summary>
        public enum FontFileType
        {
            /// <summary>
            /// Not font file type.
            /// </summary>
            None,

            /// <summary>
            /// Brk(Break model) file.
            /// </summary>
            Brk,

            /// <summary>
            /// Emp(Emphasis model) file.
            /// </summary>
            Emp
        }

        #endregion

        #region Operations

        /// <summary>
        /// Gets or sets Location of the voice font.
        /// </summary>
        public string FontPath
        {
            get
            {
                return _fontPath;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _fontPath = value;
            }
        }

        /// <summary>
        /// Gets or sets Name of this voice font.
        /// </summary>
        public string VoiceName
        {
            get
            {
                return _voiceName;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _voiceName = value;
            }
        }

        /// <summary>
        /// Gets or sets Voice token id used by SAPI in registry to identify the voice.
        /// </summary>
        public string TokenId
        {
            get
            {
                return _tokenId;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _tokenId = value;
            }
        }

        /// <summary>
        /// Gets Samples per second of this voice font's speech data.
        /// </summary>
        public int SamplesPerSecond
        {
            get { return ReadSamplesPerSecond(this.FontPath); }
        }

        /// <summary>
        /// Gets or sets The location of unit feature file.
        /// </summary>
        public string UnitFeatureFilePath
        {
            get
            {
                return _unitFeatureFilePath;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _unitFeatureFilePath = value;
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets EngineType.
        /// </summary>
        public EngineType EngineType
        {
            get { return _engineType; }
            set { _engineType = value; }
        }

        /// <summary>
        /// Gets or sets PrimaryLanguage.
        /// </summary>
        public Language PrimaryLanguage
        {
            get { return _primaryLanguage; }
            set { _primaryLanguage = value; }
        }

        /// <summary>
        /// Gets or sets CartTreeManager.
        /// </summary>
        public CartTreeManager CartTreeManager
        {
            get
            {
                return _cartTreeManager;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _cartTreeManager = value;
            }
        }

        /// <summary>
        /// Gets or sets WeightTable.
        /// </summary>
        public WeightTable WeightTable
        {
            get
            {
                return _weightTable;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _weightTable = value;
            }
        }

        /// <summary>
        /// Gets Wave16kDir.
        /// </summary>
        public Collection<string> Wave16kDirectories
        {
            get { return _wave16kDirectories; }
        }

        /// <summary>
        /// Gets SegmentDir.
        /// </summary>
        public Collection<string> SegmentDirectories
        {
            get { return _segmentDirectories; }
        }

        /// <summary>
        /// Gets WaveUnits.
        /// </summary>
        public Dictionary<long, WaveUnit> WaveUnits
        {
            get { return _waveUnits; }
        }

        /// <summary>
        /// Gets Utterance.
        /// </summary>
        public Dictionary<string, TtsUtterance> Utterances
        {
            get { return _utterances; }
        }

        /// <summary>
        /// Gets or sets File list map for this voice font.
        /// </summary>
        public FileListMap FileMap
        {
            get
            {
                return _fileMap;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _fileMap = value;
            }
        }

        /// <summary>
        /// Gets or sets Script file for this voice font.
        /// </summary>
        public ScriptFile ScriptFile
        {
            get
            {
                return _scriptFile;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _scriptFile = value;
            }
        }

        /// <summary>
        /// Gets Segmentation file path manager.
        /// </summary>
        public SegmentFilePaths Segments
        {
            get { return new SegmentFilePaths(this); }
        }

        /// <summary>
        /// Gets 16k Hz waveform file path manager.
        /// </summary>
        public Wave16kFilePaths Wave16ks
        {
            get { return new Wave16kFilePaths(this); }
        }

        #endregion

        #region Static operations

        /// <summary>
        /// Check data file consistence between script file and filemap file.
        /// </summary>
        /// <param name="fileMap">File list map.</param>
        /// <param name="script">Script file instance.</param>
        /// <returns>Data error set found.</returns>
        public static DataErrorSet ValidateDataAlignment(FileListMap fileMap,
            ScriptFile script)
        {
            // Parameters validation
            if (fileMap == null)
            {
                throw new ArgumentNullException("fileMap");
            }

            if (fileMap.Map == null)
            {
                throw new ArgumentException("fileMap.Map is null");
            }

            if (fileMap.Map.Keys == null)
            {
                throw new ArgumentException("fileMap.Map.Keys is null");
            }

            if (string.IsNullOrEmpty(fileMap.FilePath))
            {
                throw new ArgumentException("fileMap.FilePath is null");
            }

            if (script == null)
            {
                throw new ArgumentNullException("script");
            }

            if (script.Items == null)
            {
                throw new ArgumentException("script.Items is null");
            }

            if (script.Items.Keys == null)
            {
                throw new ArgumentException("script.Items.Keys is null");
            }

            if (string.IsNullOrEmpty(script.FilePath))
            {
                throw new ArgumentException("script.FilePath is null");
            }

            DataErrorSet errorSet = new DataErrorSet();

            // go through sentence ids listed in the filemap first
            foreach (string sid in fileMap.Map.Keys)
            {
                if (!script.Items.ContainsKey(sid))
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Sentence [{0}] is found in the filemap [{1}], but not listed in script file [{2}].",
                        sid, fileMap.FilePath, script.FilePath);
                    errorSet.Errors.Add(new DataError(script.FilePath, message, sid));
                }
            }

            // check sentence ids in the script file
            foreach (string sid in script.Items.Keys)
            {
                if (!fileMap.Map.ContainsKey(sid))
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Sentence [{0}] is found in the script [{1}], but not listed in filemap [{2}].",
                        sid, script.FilePath, fileMap.FilePath);
                    errorSet.Errors.Add(new DataError(script.FilePath, message, sid));
                }
            }

            return errorSet;
        }

        /// <summary>
        /// Check waveform files consistence between waveform and
        /// Referrence waveform files with the filemap.
        /// </summary>
        /// <param name="fileMap">File list map listed the sentences to validate.</param>
        /// <param name="waveDir">Base directory of waveform file.</param>
        /// <param name="refWaveDir">Directory of referrence waveform file.</param>
        /// <param name="refName">The name of the referrence waveform directory.</param>
        /// <returns>Data error set found.</returns>
        public static DataErrorSet ValidateWaveAlignment(FileListMap fileMap, string waveDir,
            string refWaveDir, string refName)
        {
            if (fileMap == null)
            {
                throw new ArgumentNullException("fileMap");
            }

            if (fileMap.Map == null)
            {
                throw new ArgumentException("fileMap.Map is null");
            }

            if (fileMap.Map.Keys == null)
            {
                throw new ArgumentException("fileMap.Map.Keys is null");
            }

            if (string.IsNullOrEmpty(refName))
            {
                throw new ArgumentNullException("refName");
            }

            if (string.IsNullOrEmpty(refWaveDir))
            {
                throw new ArgumentNullException("refWaveDir");
            }

            DataErrorSet errorSet = new DataErrorSet();

            foreach (string sid in fileMap.Map.Keys)
            {
                try
                {
                    string refFile = Path.Combine(refWaveDir, fileMap.Map[sid] + ".wav");
                    string waveFile = Path.Combine(waveDir, fileMap.Map[sid] + ".wav");

                    int waveSampleCount = 0;
                    int refSampleCount = 0;
                    WaveFormat waveFormat = new WaveFormat();
                    WaveFormat refWaveFormat = new WaveFormat();

                    StringBuilder sb = new StringBuilder();

                    // validate referrence file existance
                    if (!File.Exists(refFile))
                    {
                        sb.AppendFormat(CultureInfo.InvariantCulture,
                            "{0} file [{0}] does not exist.", refName, refFile);
                    }
                    else
                    {
                        refSampleCount = WaveFile.ReadSampleCount(refFile);
                        refWaveFormat = WaveFile.ReadFormat(refFile);
                    }

                    // validate waveform file existance
                    if (!File.Exists(waveFile))
                    {
                        sb.AppendFormat(CultureInfo.InvariantCulture,
                            "Wave file [{0}] does not exist.", waveFile);
                    }
                    else
                    {
                        waveSampleCount = WaveFile.ReadSampleCount(waveFile);
                        waveFormat = WaveFile.ReadFormat(waveFile);
                    }

                    // validate content consistence
                    if (waveSampleCount != 0 && refSampleCount != 0
                        && waveSampleCount != refSampleCount)
                    {
                        sb.AppendFormat(CultureInfo.InvariantCulture,
                            "The sample count is not the same between waveform file [{0}] and {1} file [{2}].",
                            waveFile, refName, refFile);
                    }

                    if (!waveFormat.Equals(refWaveFormat))
                    {
                        sb.AppendFormat(CultureInfo.InvariantCulture,
                            "The waveform format is not the same between waveform file [{0}] and {1} file [{2}].",
                            waveFile, refName, refFile);
                    }

                    if (sb.Length > 0)
                    {
                        errorSet.Errors.Add(new DataError(string.Empty, sb.ToString(), sid));
                    }
                }
                catch (InvalidDataException ide)
                {
                    string message = Helper.BuildExceptionMessage(ide);
                    errorSet.Errors.Add(new DataError(string.Empty, message, sid));
                }
            }

            return errorSet;
        }

        /// <summary>
        /// Check data file consistence between segment, epoch and wave16k files with the filemap.
        /// </summary>
        /// <param name="fileMap">File list map.</param>
        /// <param name="wave16kDir">16k Hz waveform file directory.</param>
        /// <param name="epochDir">Epoch file directory.</param>
        /// <param name="segmentDir">Segmentation file directory.</param>
        /// <returns>Data error set found.</returns>
        public static DataErrorSet ValidateDataAlignment(
            FileListMap fileMap, string wave16kDir, string epochDir, string segmentDir)
        {
            // Parameter validation
            if (fileMap == null)
            {
                throw new ArgumentNullException("fileMap");
            }

            if (fileMap.Map == null)
            {
                throw new ArgumentException("fileMap.Map is null");
            }

            if (fileMap.Map.Keys == null)
            {
                throw new ArgumentException("fileMap.Map.Keys is null");
            }

            DataErrorSet errorSet = new DataErrorSet();

            // go through the file list map for each sentence
            foreach (string sid in fileMap.Map.Keys)
            {
                try
                {
                    string alignmentFile = Path.Combine(segmentDir, fileMap.Map[sid] + ".txt");
                    string epochFile = Path.Combine(epochDir, fileMap.Map[sid] + ".epoch");
                    string wave16kFile = Path.Combine(wave16kDir, fileMap.Map[sid] + ".wav");

                    StringBuilder error = new StringBuilder();

                    StringBuilder warning = ValidateDataAlignment(alignmentFile, epochFile, wave16kFile, error);
                    if (error.Length > 0)
                    {
                        errorSet.Errors.Add(new DataError(string.Empty, error.ToString(), sid));
                    }

                    if (warning.Length > 0)
                    {
                        errorSet.Errors.Add(new DataError(string.Empty, warning.ToString()));
                    }
                }
                catch (InvalidDataException ide)
                {
                    string message = Helper.BuildExceptionMessage(ide);
                    errorSet.Errors.Add(new DataError(string.Empty, message, sid));
                }
            }

            return errorSet;
        }

        /// <summary>
        /// Check data file consistence between segment, and wave files with the file map.
        /// </summary>
        /// <param name="fileMap">File list map.</param>
        /// <param name="waveDir">Waveform file directory.</param>
        /// <param name="segmentDir">Segmentation file directory.</param>
        /// <returns>Data error set found.</returns>
        public static DataErrorSet ValidateDataAlignment(
            FileListMap fileMap, string waveDir, string segmentDir)
        {
            // Parameter validation
            if (fileMap == null)
            {
                throw new ArgumentNullException("fileMap");
            }

            if (fileMap.Map == null)
            {
                throw new ArgumentException("fileMap.Map is null");
            }

            if (fileMap.Map.Keys == null)
            {
                throw new ArgumentException("fileMap.Map.Keys is null");
            }

            DataErrorSet errorSet = new DataErrorSet();

            // go through the file list map for each sentence
            foreach (string sid in fileMap.Map.Keys)
            {
                try
                {
                    string alignmentFile = Path.Combine(segmentDir, fileMap.Map[sid] + ".txt");
                    string waveFile = Path.Combine(waveDir, fileMap.Map[sid] + ".wav");

                    StringBuilder error = new StringBuilder();

                    ValidateDataAlignment(alignmentFile, waveFile, error);
                    if (error.Length > 0)
                    {
                        errorSet.Errors.Add(new DataError(string.Empty, error.ToString(), sid));
                    }
                }
                catch (InvalidDataException ide)
                {
                    string message = Helper.BuildExceptionMessage(ide);
                    errorSet.Errors.Add(new DataError(string.Empty, message, sid));
                }
            }

            return errorSet;
        }

        /// <summary>
        /// Check data consistence between script file and segmentation files.
        /// </summary>
        /// <param name="fileMap">File list map.</param>
        /// <param name="script">Script file instance.</param>
        /// <param name="segmentDir">Segment file directory.</param>
        /// <returns>Data error set found.</returns>
        public static DataErrorSet ValidateDataAlignment(
            FileListMap fileMap, ScriptFile script, string segmentDir)
        {
            // Parameters validation
            if (string.IsNullOrEmpty(segmentDir))
            {
                throw new ArgumentNullException("segmentDir");
            }

            if (fileMap == null)
            {
                throw new ArgumentNullException("fileMap");
            }

            if (fileMap.Map == null)
            {
                throw new ArgumentException("fileMap.Map is null");
            }

            if (fileMap.Map.Keys == null)
            {
                throw new ArgumentException("fileMap.Map.Keys is null");
            }

            if (script == null)
            {
                throw new ArgumentNullException("script");
            }

            if (script.Items == null)
            {
                throw new ArgumentException("script.Items is null");
            }

            if (script.Items.Values == null)
            {
                throw new ArgumentException("script.Items.Values is null");
            } 

            DataErrorSet errorSet = new DataErrorSet();

            foreach (ScriptItem item in script.Items.Values)
            {
                try
                {
                    if (!fileMap.Map.ContainsKey(item.Id))
                    {
                        errorSet.Errors.Add(new DataError(script.FilePath,
                            "File list map does not contain sentences.", item.Id));
                        continue;
                    }

                    ValidateDataAlignment(script, item, fileMap, segmentDir, errorSet, false);
                }
                catch (InvalidDataException ide)
                {
                    errorSet.Errors.Add(new DataError(script.FilePath,
                        Helper.BuildExceptionMessage(ide), item.Id));
                }
            }

            foreach (string sid in fileMap.Map.Keys)
            {
                if (!script.Items.ContainsKey(sid))
                {
                    errorSet.Errors.Add(new DataError(script.FilePath,
                        "script file does not contain the sentence.", sid));
                }
            }

            return errorSet;
        }

        /// <summary>
        /// Extract acoustic features for a given script file.
        /// </summary>
        /// <param name="script">Script file instance.</param>
        /// <param name="fileMap">File list map.</param>
        /// <param name="segmentDir">Segmentation file directory.</param>
        /// <param name="wave16kDir">16k Hz waveform file directory.</param>
        /// <param name="epochDir">Epoch file directory.</param>
        /// <param name="targetFilePath">Target acoustic file path.</param>
        public static void ExtractAcoustic(ScriptFile script, FileListMap fileMap,
            string segmentDir, string wave16kDir, string epochDir, string targetFilePath)
        {
            // Parameters validation
            if (script == null)
            {
                throw new ArgumentNullException("script");
            }

            if (string.IsNullOrEmpty(script.FilePath))
            {
                throw new ArgumentException("script.FilePath is null");
            }

            if (script.Items == null)
            {
                throw new ArgumentException("script.Items is null");
            }

            if (fileMap == null)
            {
                throw new ArgumentNullException("fileMap");
            }

            if (fileMap.Map == null)
            {
                throw new ArgumentException("fileMap.Map is null");
            }

            if (fileMap.Map.Keys == null)
            {
                throw new ArgumentException("fileMap.Map.Keys is null");
            }

            if (string.IsNullOrEmpty(segmentDir))
            {
                throw new ArgumentNullException("segmentDir");
            }

            if (string.IsNullOrEmpty(wave16kDir))
            {
                throw new ArgumentNullException("wave16kDir");
            }

            if (string.IsNullOrEmpty(epochDir))
            {
                throw new ArgumentNullException("epochDir");
            }

            if (!Directory.Exists(segmentDir))
            {
                throw Helper.CreateException(typeof(DirectoryNotFoundException),
                    segmentDir);
            }

            if (!Directory.Exists(wave16kDir))
            {
                throw Helper.CreateException(typeof(DirectoryNotFoundException),
                    wave16kDir);
            }

            if (!Directory.Exists(epochDir))
            {
                throw Helper.CreateException(typeof(DirectoryNotFoundException),
                    epochDir);
            }

            if (string.IsNullOrEmpty(targetFilePath))
            {
                throw new ArgumentNullException("targetFilePath");
            }

            Helper.EnsureFolderExistForFile(targetFilePath);

            using (StreamWriter sw = new StreamWriter(targetFilePath))
            {
                // iterate each script item or sentence
                foreach (string sid in fileMap.Map.Keys)
                {
                    if (!script.Items.ContainsKey(sid))
                    {
                        string message = string.Format(CultureInfo.InvariantCulture,
                            "Sentence [{0}] does not exist in script file [{1}].",
                            sid, script.FilePath);
                        throw new InvalidDataException(message);
                    }

                    ExtractAcoustic(sw, script, sid, fileMap, segmentDir, wave16kDir, epochDir);
                }
            }
        }

        /// <summary>
        /// Read samples per second of the speech data in the voice font.
        /// </summary>
        /// <param name="fontPath">Voice font path.</param>
        /// <returns>Samples per second of data for the given voice font.</returns>
        public static int ReadSamplesPerSecond(string fontPath)
        {
            if (string.IsNullOrEmpty(fontPath))
            {
                throw new ArgumentNullException("fontPath");
            }

            int samplesPerSecond = 16000;
            string wihFile = fontPath + ".wih";
            if (File.Exists(wihFile))
            {
                FileStream fs = new FileStream(wihFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                try
                {
                    using (BinaryReader br = new BinaryReader(fs))
                    {
                        fs = null;

                        try
                        {
                            // move ahead 8 bytes
                            br.ReadInt64();
                            samplesPerSecond = br.ReadInt32();
                        }
                        catch (EndOfStreamException ese)
                        {
                            string message = string.Format(CultureInfo.InvariantCulture,
                                "Fail to read samples per second from WIH file [{0}] for invalid data.",
                                wihFile);
                            throw new InvalidDataException(message, ese);
                        }
                    }
                }
                finally
                {
                    if (null != fs)
                    {
                        fs.Dispose();
                    }
                }
            }

            return samplesPerSecond;
        }

        /// <summary>
        /// Check data consistence between script item and segmentation file.
        /// </summary>
        /// <param name="script">Script file instance.</param>
        /// <param name="item">Script item.</param>
        /// <param name="fileMap">File list map.</param>
        /// <param name="segmentDir">Segment file directory.</param>
        /// <param name="errorSet">Data error set found.</param>
        /// <param name="phoneBasedSegment">Phone based alignment or unit based alignment.</param>
        public static void ValidateDataAlignment(ScriptFile script, ScriptItem item,
            FileListMap fileMap, string segmentDir, DataErrorSet errorSet, bool phoneBasedSegment)
        {
            string segmentFilePath = Path.Combine(segmentDir, fileMap.Map[item.Id] + ".txt");

            SegmentFile segmentFile = new SegmentFile();
            segmentFile.Load(segmentFilePath);

            if (segmentFile.WaveSegments.Count == 0)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "There is no valid alignment data into alignment file.");
                errorSet.Errors.Add(new DataError(segmentFilePath, message, item.Id));
            }
            else if (!segmentFile.WaveSegments[segmentFile.WaveSegments.Count - 1].IsSilencePhone)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "The alignment file is invalid, for without silence segment at the end.");
                errorSet.Errors.Add(new DataError(segmentFilePath, message, item.Id));
            }
            else if (!phoneBasedSegment && item.Units.Count != segmentFile.NonSilenceWaveSegments.Count)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "script units {0} do not match with non-silence segments {1} in segmentation file.",
                    item.Units.Count, segmentFile.NonSilenceWaveSegments.Count);
                errorSet.Errors.Add(new DataError(script.FilePath, message, item.Id));
            }
            else if (phoneBasedSegment && item.GetPhones().Length != segmentFile.NonSilenceWaveSegments.Count)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "script phones {0} do not match with non-silence segments {1} in segmentation file.",
                    item.GetPhones().Length, segmentFile.NonSilenceWaveSegments.Count);
                errorSet.Errors.Add(new DataError(script.FilePath, message, item.Id));
            } 
            else
            {
                // go through each segments
                if (phoneBasedSegment)
                {
                    string[] phones = item.GetPhones();
                    for (int i = 0; i < segmentFile.NonSilenceWaveSegments.Count; i++)
                    {
                        WaveSegment segment = segmentFile.NonSilenceWaveSegments[i];
                        
                        if (segment.Label != phones[i])
                        {
                            string message = string.Format(CultureInfo.InvariantCulture,
                                "phone [{0}/{1}] at {2} does not match between script and segment.",
                                WaveSegment.FormatLabel(phones[i]), segment.Label, i);
                            errorSet.Errors.Add(new DataError(script.FilePath, message, item.Id));
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < segmentFile.NonSilenceWaveSegments.Count; i++)
                    {
                        WaveSegment segment = segmentFile.NonSilenceWaveSegments[i];
                        TtsUnit unit = item.Units[i];

                        if (segment.Label != WaveSegment.FormatLabel(unit.MetaUnit.Name))
                        {
                            string message = string.Format(CultureInfo.InvariantCulture,
                                "units [{0}/{1}] at {2} do not match between script and segment.",
                                WaveSegment.FormatLabel(unit.MetaUnit.Name), segment.Label, i);
                            errorSet.Errors.Add(new DataError(script.FilePath, message, item.Id));
                        }
                    }
                }
            }
        }

        #endregion

        #region Public operations

        /// <summary>
        /// Initialize this voice font instance.
        /// </summary>
        public void Initialize()
        {
            DataLoadingStatusChanged(this, new DataLoadingEventArgs(0));

            // ScriptFile.Initialize
            DataLoadingStatusChanged(this, new DataLoadingEventArgs(30));

            // _fileList.LoadFileMap(Setting.MapFile);
            DataLoadingStatusChanged(this, new DataLoadingEventArgs(40));

            // _weightTable.LoadTextFile(Setting.WeightTableFile);
            DataLoadingStatusChanged(this, new DataLoadingEventArgs(50));

            _cartTreeManager.Initialize(_primaryLanguage, _engineType,
                _cartTreeManager.CartQuestionFile);

            WaveUnit.ReadAllData(WaveUnits, UnitFeatureFilePath, this.SamplesPerSecond);
            DataLoadingStatusChanged(this, new DataLoadingEventArgs(100));
        }

        /// <summary>
        /// Build utterance instances.
        /// </summary>
        public void BuildUtterances()
        {
            _utterances = new Dictionary<string, TtsUtterance>();
            System.Diagnostics.Debug.Assert(_scriptFile != null);
            System.Diagnostics.Debug.Assert(_fileMap != null);

            // TODO
        }

        /// <summary>
        /// Parse voice font configuration.
        /// </summary>
        /// <param name="config">Configuration in XmlElement.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1059:MembersShouldNotExposeCertainConcreteTypes",
            MessageId = "System.Xml.XmlNode", Justification = "Ignore.")]
        public void ParseConfig(XmlElement config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            Debug.Assert(!string.IsNullOrEmpty(config.GetAttribute("language")));
            Debug.Assert(!string.IsNullOrEmpty(config.GetAttribute("engine")));
            _primaryLanguage = Localor.StringToLanguage(
                                            config.GetAttribute("language"));
            _engineType = (EngineType)Enum.Parse(typeof(EngineType),
                                            config.GetAttribute("engine"));

            XmlElement eleLangData = config.SelectSingleNode("languageData") as XmlElement;
            VoiceCreationLanguageData languageData = new VoiceCreationLanguageData();
            if (eleLangData != null)
            {
                languageData.ParseLanguageDataFromXmlElement(true, eleLangData);
                languageData.SetLanguageData(_primaryLanguage);
            }
            else
            {
                languageData.CartQuestions = config.SelectSingleNode("question/@path").InnerText;
            }

            _voiceName = config.GetAttribute("voiceName");
            _tokenId = config.GetAttribute("tokenId");

            _fontPath = config.SelectSingleNode("font/@path").InnerText;

            ScriptFile = Localor.CreateScriptFile(_primaryLanguage, _engineType);
            ScriptFile.Load(config.SelectSingleNode("script/@path").InnerText);

            FileMap = new FileListMap();
            FileMap.Load(config.SelectSingleNode("filemap/@path").InnerText);

            _weightTable = new WeightTable(_primaryLanguage, _engineType);
            _weightTable.Load(config.SelectSingleNode("weighttable/@path").InnerText);

            _cartTreeManager = new CartTreeManager();
            _cartTreeManager.CartTreeDir = config.SelectSingleNode("treedir/@path").InnerText;
            if (!Directory.Exists(_cartTreeManager.CartTreeDir))
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "The treeDir path does not exist at [{0}]",
                    _cartTreeManager.CartTreeDir);
                throw new DirectoryNotFoundException(message);
            }

            _cartTreeManager.CartQuestionFile = languageData.CartQuestions;
            if (!File.Exists(_cartTreeManager.CartQuestionFile))
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "The tree question file path does not exist at [{0}]",
                    _cartTreeManager.CartQuestionFile);
                throw new DirectoryNotFoundException(message);
            }

            _cartTreeManager.UnitDescriptFile = config.SelectSingleNode("unitdescript/@path").InnerText;
            if (!File.Exists(_cartTreeManager.UnitDescriptFile))
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "The unit description file path does not exist at [{0}]",
                    _cartTreeManager.UnitDescriptFile);
                throw new DirectoryNotFoundException(message);
            }

            _unitFeatureFilePath = config.SelectSingleNode("wavesequence/@path").InnerText;
            if (!File.Exists(_unitFeatureFilePath))
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "The wave sequence file path does not exist at [{0}]",
                    _unitFeatureFilePath);
                throw new DirectoryNotFoundException(message);
            }

            _wave16kDirectories.Clear();
            foreach (XmlNode dirNode in config.SelectNodes("wave16k/@path"))
            {
                string waveDir = dirNode.InnerText.Trim();
                if (!Directory.Exists(waveDir))
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "The wave16k path does not exist at [{0}]",
                        waveDir);
                    throw new DirectoryNotFoundException(message);
                }

                _wave16kDirectories.Add(waveDir);
            }

            _segmentDirectories.Clear();
            foreach (XmlNode dirNode in config.SelectNodes("segment/@path"))
            {
                string alignmentDir = dirNode.InnerText.Trim();
                if (!Directory.Exists(alignmentDir))
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "The alignment path does not exist at [{0}]",
                        alignmentDir);
                    throw new DirectoryNotFoundException(message);
                }

                _segmentDirectories.Add(alignmentDir);
            }
        }

        #endregion

        #region Private operations

        /// <summary>
        /// Check data file consistence between segment, epoch and wave16k file.
        /// </summary>
        /// <param name="alignmentFile">Alignment file.</param>
        /// <param name="epochFile">Epoch file.</param>
        /// <param name="wave16kFile">16k Hz waveform file.</param>
        /// <param name="error">Error message.</param>
        /// <returns>Data warning message.</returns>
        private static StringBuilder ValidateDataAlignment(string alignmentFile,
            string epochFile, string wave16kFile, StringBuilder error)
        {
            // waveform file total sample count
            int waveSampleCount = 0;

            // epoch scope defining the sample count covered by the epoch file
            int epochScope = 0;
            int[] epochs = null;
            int[] epochOffsets = null;

            int lastSilenceAlign = ValidateAlignmentFile(alignmentFile, error);

            // Validate epoch file existance
            if (!File.Exists(epochFile))
            {
                error.AppendFormat(CultureInfo.InvariantCulture,
                    "Epoch file [{0}] does not exist.", epochFile);
            }
            else
            {
                epochs = EpochFile.ReadAllDecodedData(epochFile, 0);
                epochOffsets = EpochFile.EpochToOffset(epochs);
                epochScope = Math.Abs(epochOffsets[epochOffsets.Length - 1]);
            }

            // Validate waveform file existance
            if (!File.Exists(wave16kFile))
            {
                error.AppendFormat(CultureInfo.InvariantCulture,
                    "Wave 16k file [{0}] does not exist.", wave16kFile);
            }
            else
            {
                waveSampleCount = WaveFile.ReadSampleCount(wave16kFile);
            }

            // Validate content
            // test the duration consistence between waveform and epoch file
            if (waveSampleCount != 0 && epochScope != 0
                && waveSampleCount != epochScope)
            {
                error.AppendFormat(CultureInfo.InvariantCulture,
                    "The sample count is un-consistent between waveform file [{0}] and epoch file [{1}].",
                    wave16kFile, epochFile);
            }

            // test the duration consistence between waveform and alignment file
            if (waveSampleCount != 0 && lastSilenceAlign != 0
                && lastSilenceAlign > waveSampleCount)
            {
                error.AppendFormat(CultureInfo.InvariantCulture,
                    "The last silence alignment of alignment file [{0}] is beyond the total length waveform file [{1}].",
                    alignmentFile, wave16kFile);
            }

            // the epoch from last silence should all be negative
            StringBuilder warning = ValidateEndingEpoch(alignmentFile, epochFile,
                epochs, epochOffsets, lastSilenceAlign);

            return warning;
        }

        /// <summary>
        /// Check data file consistence between segment and wave file.
        /// </summary>
        /// <param name="alignmentFile">Alignment file.</param>
        /// <param name="waveFile">Waveform file.</param>
        /// <param name="errorMessage">Error message.</param>
        private static void ValidateDataAlignment(string alignmentFile,
           string waveFile, StringBuilder errorMessage)
        {
            // waveform file total sample count
            int waveSampleCount = 0;
            int samplesPerSecond = 0;
            int lastSilenceAlign = ValidateAlignmentFile(alignmentFile, errorMessage);

            // Validate waveform file existance
            if (!File.Exists(waveFile))
            {
                errorMessage.AppendFormat(CultureInfo.InvariantCulture,
                    "Wave file [{0}] does not exist.", waveFile);
            }
            else
            {
                waveSampleCount = WaveFile.ReadSampleCount(waveFile);
                samplesPerSecond = WaveFile.ReadFormat(waveFile).SamplesPerSecond;
            }

            if (samplesPerSecond != 16000)
            {
                lastSilenceAlign = lastSilenceAlign * samplesPerSecond / 16000;
            }

            // Validate content
            // test the duration consistence between waveform and alignment file
            if (waveSampleCount != 0 && lastSilenceAlign != 0
                && lastSilenceAlign > waveSampleCount)
            {
                errorMessage.AppendFormat(CultureInfo.InvariantCulture,
                    "The last silence alignment of alignment file [{0}] is beyond the total length waveform file [{1}].",
                    alignmentFile, waveFile);
            }
        }
        
        /// <summary>
        /// Validate alingment file.
        /// </summary>
        /// <param name="alignmentFile">Alignment file to validate.</param>
        /// <param name="builder">String builder for error message.</param>
        /// <returns>The position of the last silence alignment.</returns>
        private static int ValidateAlignmentFile(string alignmentFile, StringBuilder builder)
        {
            // sample position of the last silence alignment
            int lastSilenceAlign = 0;

            // validate the file present or not
            // and count the duration of the content
            // Validate alignment file existance
            if (!File.Exists(alignmentFile))
            {
                builder.AppendFormat(CultureInfo.InvariantCulture,
                    "Alignment file [{0}] does not exist.",
                    alignmentFile);
            }
            else
            {
                SegmentFile segFile = new SegmentFile();
                segFile.Load(alignmentFile);
                WaveSegment lastSeg = segFile.WaveSegments[segFile.WaveSegments.Count - 1];
                if (lastSeg.IsSilencePhone)
                {
                    // the last one should be silence of the segment file
                    lastSilenceAlign = (int)(lastSeg.StartTime * 16000);
                }
                else
                {
                    builder.AppendFormat(CultureInfo.InvariantCulture,
                        "The ending segment of alignment file [{0}] is not silence.",
                        alignmentFile);
                }
            }

            return lastSilenceAlign;
        }

        /// <summary>
        /// Validate ending epoch data, which should be negative value for silence.
        /// </summary>
        /// <param name="alignmentFile">Alignment file path.</param>
        /// <param name="epochFile">Epoch file path.</param>
        /// <param name="epochs">Epochs data.</param>
        /// <param name="epochOffsets">Epochs offset.</param>
        /// <param name="lastSilenceAlign">Last silence alignment position.</param>
        /// <returns>Warining message.</returns>
        private static StringBuilder ValidateEndingEpoch(string alignmentFile,
            string epochFile, int[] epochs, int[] epochOffsets, int lastSilenceAlign)
        {
            StringBuilder warning = new StringBuilder();
            if (epochOffsets != null && lastSilenceAlign != 0)
            {
                int voicedEpochCount = 0;
                StringBuilder detailed = new StringBuilder();
                for (int i = 0; i < epochOffsets.Length; i++)
                {
                    if (Math.Abs(epochOffsets[i]) > lastSilenceAlign
                        && epochOffsets[i] > 0)
                    {
                        voicedEpochCount++;
                        if (detailed.Length > 0)
                        {
                            detailed.Append(",");
                        }

                        detailed.AppendFormat(CultureInfo.InvariantCulture,
                            "{0}", epochs[i]);
                    }
                }

                if (voicedEpochCount > 0)
                {
                    string str1 = "Warning: the epoch data in file [{0}] should";
                    string str2 = " be negative (invalid) for those speech data ";
                    string str3 = "after the begin of last silence in alignment file ";
                    string str4 = "[{1}]. There are {2} invalid epochs with values: {3}.";
                    
                    // we are not sure about this noise. it requires manual confirm
                    warning.AppendFormat(CultureInfo.InvariantCulture,
                        str1 + str2 + str3 + str4,
                        epochFile, alignmentFile, voicedEpochCount, detailed);
                }
            }

            return warning;
        }

        /// <summary>
        /// Extract acoustic features for a given sentence.
        /// </summary>
        /// <param name="writer">Stream writer to write acoustic features.</param>
        /// <param name="script">Script file instance.</param>
        /// <param name="sid">Sentence id.</param>
        /// <param name="fileMap">File list map.</param>
        /// <param name="segmentDir">Segmentation file directory.</param>
        /// <param name="wave16kDir">16k Hz waveform file directory.</param>
        /// <param name="epochDir">Epoch file directory.</param>
        private static void ExtractAcoustic(StreamWriter writer, ScriptFile script, string sid,
            FileListMap fileMap, string segmentDir, string wave16kDir, string epochDir)
        {
            ScriptItem scriptItem = script.Items[sid];

            // find the absolute file paths for each kind data file 
            string wave16kFilePath = Path.Combine(wave16kDir, fileMap.Map[scriptItem.Id] + ".wav");
            string epochFilePath = Path.Combine(epochDir, fileMap.Map[scriptItem.Id] + ".epoch");
            string segmentFilePath = Path.Combine(segmentDir, fileMap.Map[scriptItem.Id] + ".txt");

            // load data files
            SegmentFile segFile = new SegmentFile();
            segFile.Load(segmentFilePath);

            EggAcousticFeature eggFile = new EggAcousticFeature();
            eggFile.LoadEpoch(epochFilePath);

            WaveAcousticFeature waveFile = new WaveAcousticFeature();
            waveFile.Load(wave16kFilePath);

            // calculate acoustic features for each segments in the files
            int totalCount = segFile.NonSilenceWaveSegments.Count;
            if (scriptItem.Units.Count != totalCount)
            {
                string str1 = "Unit number mis-matched between sentence [{0}] in ";
                string str2 = "script file [{1}] and in the alignment file [{2}]. ";
                string str3 = "There are {3} units in script but {4} units in alignment.";
                string message = string.Format(CultureInfo.InvariantCulture,
                    str1 + str2 + str3,
                    sid, script.FilePath, segmentFilePath,
                    scriptItem.Units.Count, totalCount);
                throw new InvalidDataException(message);
            }

            for (int i = 0; i < totalCount; i++)
            {
                // for each wave segment
                WaveSegment ws = segFile.NonSilenceWaveSegments[i];

                // get unit sample scope
                int sampleOffset = (int)(ws.StartTime * waveFile.SamplesPerSecond);
                int sampleLength = (int)(ws.Duration * waveFile.SamplesPerSecond);
                int sampleEnd = sampleOffset + sampleLength;

                int epochOffset = 0;
                int epochEnd = 0;

                // calculate average pitch, pitch average
                float averagePitch, pitchRange;
                eggFile.GetPitchAndRange(sampleOffset,
                    sampleLength, out averagePitch, out pitchRange);
                ws.AveragePitch = averagePitch;
                ws.PitchRange = pitchRange;

                // calculate root mean square, and before that ajust the segment alignment with
                // the epoch data
                epochOffset = eggFile.AdjustAlignment(ref sampleOffset);
                epochEnd = eggFile.AdjustAlignment(ref sampleEnd);

                if (epochOffset > epochEnd)
                {
                    string info = string.Format(CultureInfo.InvariantCulture,
                        "epochOffset[{0}] should not be bigger than epochEnd[{1}]",
                        epochOffset, epochEnd);
                    throw new InvalidDataException(info);
                }

                if (sampleEnd > waveFile.SampleNumber)
                {
                    string str1 = "Mis-match found between alignment file [{0}] and waveform file [{1}], ";
                    string str2 = "for the end sample of alignment is [{2}] but";
                    string str3 = " the total sample number of waveform file is [{3}].";
                    string info = string.Format(CultureInfo.InvariantCulture,
                        str1 + str2 + str3,
                        segmentFilePath, wave16kFilePath,
                        epochEnd, waveFile.SampleNumber);

                    throw new InvalidDataException(info);
                }

                ws.RootMeanSquare = waveFile.CalculateRms(sampleOffset, sampleEnd - sampleOffset);

                // calculate epoch
                int epoch16KCompressLength = EpochFile.CompressEpoch(eggFile.Epoch,
                    epochOffset, epochEnd - epochOffset, null);
                int epoch8KCompressLength = EpochFile.CompressEpoch(eggFile.Epoch8k,
                    epochOffset, epochEnd - epochOffset, null);

                // leave (epoch offset in sentence) (epoch length)
                // (16k compressed epoch lenght) (8k compressed epoch lenght) as zero
                string message = string.Format(CultureInfo.InvariantCulture,
                    "{0,12} {1,3} {2,9:0.000000} {3,9:0.000000} {4,7} {5,5} {6,4} {7,3} {8,3} {9,3} {10,7:0.0} {11,5:0.0} {12,4:0.0} {13}",
                    scriptItem.Id, i,
                    ws.StartTime, ws.Duration, sampleOffset, sampleEnd - sampleOffset,
                    epochOffset, epochEnd - epochOffset,
                    epoch16KCompressLength, epoch8KCompressLength,
                    ws.RootMeanSquare, ws.AveragePitch, ws.PitchRange,
                    scriptItem.Units[i].FullName);

                writer.WriteLine(message);
            }
        }

        #endregion

        #region Private event handling

        /// <summary>
        /// Handle DataLoadingStatusChanged event of DataManager.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void DataManager_DataLoadingStatusChanged(object sender,
            DataLoadingEventArgs e)
        {
            if (!Helper.PostMessage(this._handleForMsg,
                VoiceFont.LoadingStatusChanged, (IntPtr)e.Percent, IntPtr.Zero))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        #endregion

        #region Public types

        /// <summary>
        /// Segmentation file path manager.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1034:NestedTypesShouldNotBeVisible", Justification = "Ignore.")]
        public class SegmentFilePaths
        {
            /// <summary>
            /// Voice font.
            /// </summary>
            private readonly VoiceFont outer;

            /// <summary>
            /// Initializes a new instance of the <see cref="SegmentFilePaths"/> class.
            /// Construction of segmentation file path manager.
            /// </summary>
            /// <param name="outer">Voice font instance.</param>
            internal SegmentFilePaths(VoiceFont outer)
            {
                this.outer = outer;
            }

            /// <summary>
            /// Gets segmentation file path with a given sentence id.
            /// </summary>
            /// <param name="index">Sentence id.</param>
            /// <returns>Segmentation file path.</returns>
            public string this[string index]
            {
                get
                {
                    foreach (string dir in outer.SegmentDirectories)
                    {
                        string testFilePath =
                            Path.Combine(dir, outer.FileMap.Map[index] + ".txt");
                        if (File.Exists(testFilePath))
                        {
                            return testFilePath;
                        }
                    }

                    return null;
                }
            }
        }

        /// <summary>
        /// 16k Hz waveform file path manager.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1034:NestedTypesShouldNotBeVisible", Justification = "Ignore.")]
        public class Wave16kFilePaths
        {
            /// <summary>
            /// Voice font.
            /// </summary>
            private readonly VoiceFont outer;

            /// <summary>
            /// Initializes a new instance of the <see cref="Wave16kFilePaths"/> class.
            /// Construction of 16k Hz waveform file path manager.
            /// </summary>
            /// <param name="outer">Voice font instance.</param>
            internal Wave16kFilePaths(VoiceFont outer)
            {
                this.outer = outer;
            }

            /// <summary>
            /// Gets 16k Hz waveform file path with a given sentence id.
            /// </summary>
            /// <param name="index">Sentence id.</param>
            /// <returns>16k Hz waveform file path.</returns>
            public string this[string index]
            {
                get
                {
                    foreach (string dir in outer.Wave16kDirectories)
                    {
                        string testFilePath =
                            Path.Combine(dir, outer.FileMap.Map[index] + ".wav");
                        if (File.Exists(testFilePath))
                        {
                            return testFilePath;
                        }
                    }

                    return null;
                }
            }
        }

        #endregion
    }
}