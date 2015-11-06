//----------------------------------------------------------------------------
// <copyright file="WaveInventoryCreator.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements WaveInventoryCreator
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Interop
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using Microsoft.Tts.Offline.Config;
    using Microsoft.Tts.Offline.Htk;
    using Microsoft.Tts.Offline.Utility;
    using Microsoft.Tts.Offline.Waveform;

    /// <summary>
    /// The class is used to write wave forms into a wave inventory, and builds the UNT, WIH accordingly.
    /// </summary>
    [CLSCompliantAttribute(false)]
    public class WaveInventoryCreator : IDisposable
    {
        #region Fields
        /// <summary>
        /// The maximum margin in frame.
        /// </summary>
        private const int MaxMarginInFrame = 10;

        /// <summary>
        /// The file name of result file.
        /// </summary>
        private readonly string _fileName;

        /// <summary>
        /// The file stream of wave inventory file.
        /// </summary>
        private readonly FileStream _fileStream;

        /// <summary>
        /// The wave info header file.
        /// </summary>
        private readonly WaveInfoHeader _header;

        /// <summary>
        /// The unit indexing file.
        /// </summary>
        private readonly UnitIndexingFile _indexingFile;

        /// <summary>
        /// The mapping from unit string into id;.
        /// </summary>
        private readonly IDictionary<string, int> _namedUnitTypeId;

        /// <summary>
        /// The mapping from phone name into id;.
        /// </summary>
        private readonly IDictionary<string, int> _phoneIdMap;

        /// <summary>
        /// The margin length of data in sample count. for cross-correlation.
        /// </summary>
        private readonly int _ccMarginLength;

        /// <summary>
        /// The margin length of data in sample count. for Frame Shifting.
        /// </summary>
        private readonly int _fsMarginLength;

        /// <summary>
        /// The binary writer of the wave inventory file.
        /// </summary>
        private readonly BinaryWriter _writer;

        /// <summary>
        /// The obfuscation method.
        /// </summary>
        private readonly ObfuscationMethod _obfuscationMethod;

        /// <summary>
        /// The common voice font header.
        /// </summary>
        private VoiceFontHeader _voiceFontHeader;

        /// <summary>
        /// The wave data offset from file start.
        /// </summary>
        private long _dataOffset;

        /// <summary>
        /// Millisecond per sps frame.
        /// </summary>
        private int _millisecondPerFrame;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the WaveInventoryCreator class.
        /// </summary>
        /// <param name="resultFile">The file name of the result files without extension name.</param>
        /// <param name="header">The wave info header.</param>
        /// <param name="namedUnitTypeId">The Dictionary which key is unit name and the value is index type id.</param>
        /// <param name="phoneIdMap">Phone id map .</param>
        /// <param name="millisecondPerFrame">Millisecond per frame.</param>
        /// <param name="keepLeftRightFrameMargin">Keep left right frame margin.</param>
        public WaveInventoryCreator(string resultFile,
            WaveInfoHeader header,
            IDictionary<string, int> namedUnitTypeId,
            IDictionary<string, int> phoneIdMap,
            int millisecondPerFrame,
            int keepLeftRightFrameMargin)
            : this(resultFile, header, namedUnitTypeId, phoneIdMap, millisecondPerFrame, keepLeftRightFrameMargin, NullObsucator)
        {
        }

        /// <summary>
        /// Initializes a new instance of the WaveInventoryCreator class.
        /// </summary>
        /// <param name="resultFile">The file name of the result files without extension name.</param>
        /// <param name="header">The wave info header.</param>
        /// <param name="namedUnitTypeId">The Dictionary which key is unit name and the value is index type id.</param>
        /// <param name="phoneIdMap">Phone id map .</param>
        /// <param name="millisecondPerFrame">Millisecond per frame.</param>
        /// <param name="keepLeftRightFrameMargin">Keep left right frame margin.</param>
        /// <param name="obfuscationMethod">The given method to perform obfuscation for the wave data.</param>
        public WaveInventoryCreator(string resultFile,
            WaveInfoHeader header,
            IDictionary<string, int> namedUnitTypeId,
            IDictionary<string, int> phoneIdMap,
            int millisecondPerFrame,
            int keepLeftRightFrameMargin,
            ObfuscationMethod obfuscationMethod)
        {
            if (string.IsNullOrEmpty(resultFile))
            {
                throw new ArgumentNullException("resultFile");
            }

            if (header == null)
            {
                throw new ArgumentNullException("header");
            }

            if (namedUnitTypeId == null)
            {
                throw new ArgumentNullException("namedUnitTypeId");
            }

            if (phoneIdMap == null)
            {
                throw new ArgumentNullException("phoneIdMap");
            }

            if (obfuscationMethod == null)
            {
                throw new ArgumentNullException("obfuscationMethod");
            }

            if (millisecondPerFrame <= 0)
            {
                throw new ArgumentException("millisecondPerFrame");
            }

            if (keepLeftRightFrameMargin < 0)
            {
                throw new ArgumentException("keepLeftRightFrameMargin");
            }

            _obfuscationMethod = obfuscationMethod;
            _header = header;
            _header.Validate();
            _ccMarginLength = (int)(_header.CrossCorrelationMarginLength * 0.001f * _header.SamplesPerSecond);
            _fsMarginLength = (int)(keepLeftRightFrameMargin * millisecondPerFrame * 0.001f * _header.SamplesPerSecond);

            _namedUnitTypeId = namedUnitTypeId;
            _phoneIdMap = phoneIdMap;
            _indexingFile = new UnitIndexingFile(namedUnitTypeId);
            _fileName = resultFile;
            Helper.EnsureFolderExistForFile(_fileName);

            string fullFileName;
            switch (_header.Compression)
            {
                case WaveCompressCatalog.Unc:
                    fullFileName = _fileName.AppendExtensionName(FileExtensions.WaveInventory);
                    break;
                default:
                    throw new NotSupportedException("Only Unc is supported");
            }

            _fileStream = new FileStream(fullFileName, FileMode.Create);
            _writer = new BinaryWriter(_fileStream);

            _voiceFontHeader = new VoiceFontHeader
            {
                FileTag = 0x45564157,   // "WAVE"
                FormatTag = VoiceFontTag.FmtIdUncompressedWaveInventory,
                Version = 0,
                Build = 0,
                DataSize = 0,
            };

            _voiceFontHeader.Save(_writer);
            _dataOffset = _writer.BaseStream.Position;
            _millisecondPerFrame = millisecondPerFrame;

            LogFile = string.Empty;
        }

        #endregion

        #region Delegates

        /// <summary>
        /// Delegate use to obfuscate the wave form.
        /// </summary>
        /// <param name="data">The input data want to be obfuscated.</param>
        /// <param name="key">The given key to obfuscate the data.</param>
        public delegate void ObfuscationMethod(byte[] data, int key);

        #endregion

        #region property

        /// <summary>
        /// Gets or sets Log file name.
        /// </summary>
        public string LogFile { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// The member of IDisposable interface.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Save info into the files.
        /// </summary>
        public void Save()
        {
            _voiceFontHeader.DataSize = (ulong)(_writer.BaseStream.Position - _dataOffset);

            using (PositionRecover recover = new PositionRecover(_writer, 0))
            {
                _voiceFontHeader.Save(_writer);
            }

            _indexingFile.Save(_fileName.AppendExtensionName(FileExtensions.UnitIndexing));
            _header.Save(_fileName.AppendExtensionName(FileExtensions.WaveInfoHeader));

            if (!string.IsNullOrEmpty(LogFile))
            {
                _indexingFile.SaveToText(LogFile);
            }
        }

        /// <summary>
        /// Adds a sentence into wave inventory.
        /// </summary>
        /// <param name="sentence">The given sentence.</param>
        /// <param name="waveFileName">The corresponding wave form file name.</param>
        public void Add(Sentence sentence, string waveFileName)
        {
            WaveFile waveFile = new WaveFile();
            waveFile.Load(waveFileName);
            if (waveFile.Format.SamplesPerSecond != _header.SamplesPerSecond ||
                waveFile.Format.Channels != 1 ||
                waveFile.Format.FormatTag != WaveFormatTag.Pcm)
            {
                throw new NotSupportedException(Helper.NeutralFormat(
                    "The waveform format of file [{0}] is not supported.", waveFileName));
            }

            try
            {
                Add(sentence, waveFile);
            }
            catch (InvalidDataException e)
            {
                throw new InvalidDataException(Helper.NeutralFormat("It fails to process the file [{0}].", waveFileName), e);
            }
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        /// <param name="disposing">Disposing flag.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _writer.Close();

                if (null != _fileStream)
                {
                    _fileStream.Dispose();
                }

                if (null != _indexingFile)
                {
                    _indexingFile.Dispose();
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// A null obfuscator methods, nothing will be changed here.
        /// </summary>
        /// <param name="data">The input data want to be obfuscated.</param>
        /// <param name="key">The given key to obfuscate the data.</param>
        private static void NullObsucator(byte[] data, int key)
        {
        }

        /// <summary>
        /// Adds a sentence into wave inventory.
        /// </summary>
        /// <param name="sentence">The given sentence.</param>
        /// <param name="waveFile">The corresponding wave form file.</param>
        private void Add(Sentence sentence, WaveFile waveFile)
        {
            Debug.Assert(waveFile.Format.SamplesPerSecond == _header.SamplesPerSecond &&
                waveFile.Format.Channels == 1 &&
                waveFile.Format.FormatTag == WaveFormatTag.Pcm,
                "Only supports source waveform with single channel, PCM and same sampling rate.");

            // Here, I change the original design. Original design is not save the wave data of pruned candidate, but it will introduce bug when current frame shifting
            // design happens, so I change the design as to save all wave data into inventory file, it will make .WVE data size increases 30%. It is fine for M1.
            // Consider more candidates will be pruned in M2, so we need a refactor on wave inventory creation module. To ensure minimum disk size as well as no bug. 
            int firstValidIndex = sentence.Candidates.Count;
            for (int candIdx = 0; candIdx < sentence.Candidates.Count; candIdx++)
            {
                UnitCandidate candidate = sentence.Candidates[candIdx];

                int waveSampleOffsetInSentence = (int)((candidate.StartTimeInSecond * waveFile.Format.SamplesPerSecond) + 0.5f);
                int waveSampleLength = (int)(((candidate.EndTimeInSecond - candidate.StartTimeInSecond) * waveFile.Format.SamplesPerSecond) + 0.5f);
                if (candidate.Id != UnitCandidate.InvalidId)
                {
                    if (waveSampleLength > ushort.MaxValue)
                    {
                        throw new InvalidDataException(Helper.NeutralFormat(
                             "The wave sample length of {0}-th candidate in file {1}.wav overflows.", candIdx, sentence.Id));
                    }

                    WaveCandidateInfo candidateInfo = new WaveCandidateInfo
                    {
                        Name = candidate.Name,
                        Id = candidate.Id,
                        GlobalId = candidate.GlobalId,
                        SentenceId = candidate.Sentence.Id,
                        IndexOfNonSilence = (ushort)candidate.IndexOfNonSilence,
                        FrameIndexInSentence = (ushort)candidate.StartFrame,
                        FrameNumber = (ushort)(candidate.EndFrame - candidate.StartFrame),
                        FrameIndex = (uint)(sentence.GlobalFrameIndex + candidate.StartFrame),
                    };

                    if (firstValidIndex > candIdx && _indexingFile.SamplePerFrame == 0)
                    {
                        firstValidIndex = candIdx;
                        if (candidateInfo.FrameNumber != 0)
                        {
                            _indexingFile.SamplePerFrame = (uint)(waveSampleLength / candidateInfo.FrameNumber);
                        }
                    }
                    else
                    {
                        if (candidateInfo.FrameNumber != 0)
                        {
                            Debug.Assert(_indexingFile.SamplePerFrame == (uint)(waveSampleLength / candidateInfo.FrameNumber));
                        }
                    }

                    // calc left/right extensible margin, shift at most 1 units to ensure less than 1 unit. 
                    int leftMarginUnitIdx = Math.Max(0, candIdx - 1);
                    int rightMarginUnitIdx = Math.Min(candIdx + 1, sentence.Candidates.Count - 1);
                    int leftMarginFrame = candidate.StartFrame - sentence.Candidates[leftMarginUnitIdx].StartFrame;
                    int rightMarginFrame = sentence.Candidates[rightMarginUnitIdx].EndFrame - candidate.EndFrame;
                    Debug.Assert(leftMarginFrame >= 0 && rightMarginFrame >= 0);
                    candidateInfo.LeftMarginInFrame = (byte)Math.Min(leftMarginFrame, MaxMarginInFrame);
                    candidateInfo.RightMarginInFrame = (byte)Math.Min(rightMarginFrame, MaxMarginInFrame);

                    // Writes the current candidate, throw exception if unit index alignment is inconsistent with wave inventory.
                    long candidatePosition = candidateInfo.FrameIndex *                     // frame
                                             _millisecondPerFrame *                         // convert frame to millisecond
                                             (waveFile.Format.SamplesPerSecond / 1000) *    // get samples per milliseconds (1s == 1000ms), convert millisecond to sample
                                             _header.BytesPerSample;                        // convert sample to byte
                    long wavePosition = _writer.BaseStream.Position - _dataOffset;
                    if (candidatePosition != wavePosition)
                    {
                        throw new InvalidDataException(Helper.NeutralFormat(
                            "Frame {0} in sentence {1} starts at {2}, which is inconsistent with position in wave inventory {3}.\r\nPossible cause: bad MLF alignment.",
                            candidateInfo.FrameIndexInSentence, candidateInfo.SentenceId, candidateInfo.FrameIndex, wavePosition));
                    }

                    WriteIntoInventory(ConvertsWaveDataFormat(waveFile, waveSampleOffsetInSentence, waveSampleLength));

                    _indexingFile.Add(candidateInfo);
                }
                else
                {
                    WriteIntoInventory(ConvertsWaveDataFormat(waveFile, waveSampleOffsetInSentence, waveSampleLength));
                }
            }
        }

        /// <summary>
        /// Writes the left margin if possible.
        /// </summary>
        /// <param name="waveFile">The given wave file where the current candidate belongs to.</param>
        /// <param name="candidate">The current candidate.</param>
        /// <param name="candidateInfo">The candidate information of the current candidate.</param>
        private void WriteLeftMargin(WaveFile waveFile, UnitCandidate candidate, WaveCandidateInfo candidateInfo)
        {
            if (_ccMarginLength + _fsMarginLength > 0)
            {
                int leftMarginLength = _ccMarginLength + _fsMarginLength;
                int waveSampleOffsetInSentence = (int)((candidate.StartTimeInSecond * waveFile.Format.SamplesPerSecond) + 0.5f);

                // Left margin section.
                if (candidate.Index == 0)
                {
                    // It means the candidate is the first one, there is no previous candidate. So, writes some zero as margin.
                    WriteZeroMargin(leftMarginLength);
                }
                else if (candidate.Sentence.Candidates[candidate.Index - 1].Id == UnitCandidate.InvalidId)
                {
                    // There is a previous candidate and it isn't in the inventory. So, writes the previous candidate as margin.
                    int offset = (int)(waveSampleOffsetInSentence - leftMarginLength);
                    int count = leftMarginLength;
                    if (offset < 0)
                    {
                        // The margin is longer than the previous candidate, uses zero to fill them.
                        WriteZeroMargin(-offset);
                        count += offset;
                        offset = 0;
                    }

                    WriteIntoInventory(ConvertsWaveDataFormat(waveFile, offset, count));
                }
            }
        }

        /// <summary>
        /// Writes the right margin if possible.
        /// </summary>
        /// <param name="waveFile">The given wave file where the current candidate belongs to.</param>
        /// <param name="candidate">The current candidate.</param>
        /// <param name="candidateInfo">The candidate information of the current candidate.</param>
        private void WriteRightMargin(WaveFile waveFile, UnitCandidate candidate, WaveCandidateInfo candidateInfo)
        {
            if (_ccMarginLength + _fsMarginLength > 0)
            {
                int rightMarginLength = (_ccMarginLength / 2) + _fsMarginLength;
                int waveSampleOffsetInSentence = (int)((candidate.StartTimeInSecond * waveFile.Format.SamplesPerSecond) + 0.5f);
                int waveSampleLength = (int)(((candidate.EndTimeInSecond - candidate.StartTimeInSecond) * waveFile.Format.SamplesPerSecond) + 0.5f);

                // Right margin section.
                if (candidate.Index == candidate.Sentence.Candidates.Count - 1)
                {
                    // It means the candidate is the last one, there is no next candidate. So, writes some zero as margin.
                    WriteZeroMargin(rightMarginLength);
                }
                else if (candidate.Sentence.Candidates[candidate.Index + 1].Id == UnitCandidate.InvalidId)
                {
                    // There is a next candidate and it isn't in the inventory. So, writes the next candidate as margin.
                    int offset = (int)(waveSampleOffsetInSentence + waveSampleLength);
                    int count = (waveFile.GetSoundData().Length / (waveFile.Format.BitsPerSample / 8)) - offset;
                    if (count < rightMarginLength)
                    {
                        WriteIntoInventory(ConvertsWaveDataFormat(waveFile, offset, count));
                        WriteZeroMargin(rightMarginLength - count);
                    }
                    else
                    {
                        WriteIntoInventory(ConvertsWaveDataFormat(waveFile, offset, rightMarginLength));
                    }
                }
            }
        }

        /// <summary>
        /// Writes some zero data into inventory.
        /// </summary>
        /// <param name="count">The count in sample count of the data will be written.</param>
        private void WriteZeroMargin(int count)
        {
            if (count != 0)
            {
                byte[] zeroMargin = new byte[_header.BytesPerSample * count];
                WriteIntoInventory(zeroMargin);
            }
        }

        /// <summary>
        /// Writes the data into inventory.
        /// </summary>
        /// <param name="data">The given data to write.</param>
        private void WriteIntoInventory(byte[] data)
        {
            // Obfuscates the data if needed.
            if (_header.NeedObfuscation)
            {
                // The key in runtime is in byte count.
                _obfuscationMethod(data, (int)(_writer.BaseStream.Position - _dataOffset));
            }

            _writer.Write(data);
        }

        /// <summary>
        /// Converts the wave data into proper format.
        /// </summary>
        /// <param name="waveFile">The given WaveFile object in which the data will be converted.</param>
        /// <param name="offset">The offset in sample count of the data will be converted.</param>
        /// <param name="count">The count in sample count of the data will be converted.</param>
        /// <returns>Waveform data in supported format of voice font.</returns>
        private byte[] ConvertsWaveDataFormat(WaveFile waveFile, int offset, int count)
        {
            Debug.Assert(waveFile.Format.FormatTag == WaveFormatTag.Pcm, "The source format tag should be PCM.");
            byte[] data = new byte[count * _header.BytesPerSample];

            // Format conversion.
            if (_header.FormatCategory == WaveFormatTag.Pcm)
            {
                if (waveFile.Format.BitsPerSample == _header.BytesPerSample * 8)
                {
                    offset *= _header.BytesPerSample;
                    count *= _header.BytesPerSample;
                    Array.Copy(waveFile.GetSoundData(), offset, data, 0, count);
                }
                else if (_header.BytesPerSample == 1)
                {
                    short[] dataIn16Bits = waveFile.DataIn16Bits;
                    for (int i = 0; i < data.Length; ++i)
                    {
                        data[i] = (byte)((dataIn16Bits[i + offset] / 256) + 128); // Convert 16-bit to 8-bit.
                    }
                }
                else if (_header.BytesPerSample == 2)
                {
                    throw new NotSupportedException("It is unsupported to convert 8-bit to 16-bit");
                }
            }
            else if (_header.FormatCategory == WaveFormatTag.Mulaw)
            {
                Debug.Assert(_header.SamplesPerSecond == 8000, "Only supports 8k Hz for mulaw voice.");
                Debug.Assert(_header.BytesPerSample == 1, "Only supports 1 byte per sample for mulaw voice.");
                Debug.Assert(_header.Compression == WaveCompressCatalog.Unc, "Only supports uncompress encoding for mulaw voice.");

                if (waveFile.Format.SamplesPerSecond != 8000)
                {
                    string message = Helper.NeutralFormat(
                        "Samples per second [{0}] of source waveform file should be the same with that [{1}] of target voice.",
                        waveFile.Format.SamplesPerSecond, _header.SamplesPerSecond);
                    throw new InvalidDataException(message);
                }

                if (waveFile.Format.BitsPerSample != 16 || waveFile.Format.BlockAlign != 2)
                {
                    string message = Helper.NeutralFormat(
                        "Only supports 16 bits per sample and 2 bytes alignment, while that of source waveform file is [{0}] and [{1}].",
                        waveFile.Format.BitsPerSample, waveFile.Format.BlockAlign);
                    throw new InvalidDataException(message);
                }

                // Converts 16bits PCM samples to 8 bits Mulaw samples
                short[] soundData = waveFile.DataIn16Bits;
                for (int i = 0; i < count; i++)
                {
                    data[i] = SampleConverter.LinearToUlaw(soundData[offset + i]);
                }
            }
            else
            {
                // Bug #70735 is filed to track: Currently, Compress is not supported in RUS offline inventory building.
                throw new NotSupportedException(
                    Helper.NeutralFormat("Unsupported target format [{0}].", _header.FormatCategory));
            }

            return data;
        }

        #endregion
    }
}