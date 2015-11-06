//----------------------------------------------------------------------------
// <copyright file="WaveFile.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements Class to manage wave file
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Waveform
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Security;
    using System.Text;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Class to manage waveform file.
    /// </summary>
    public class WaveFile
    {
        #region Private fields

        private WaveFormat _format;
        private Riff _riff;

        private string _filePath;
        private float[][] _spectrum;
        private short[] _dataIn16Bits;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Spectrum (FFT) data of this wavefile.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1819:PropertiesShouldNotReturnArrays", Justification = "Ignore."),
        System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1804:RemoveUnusedLocals", MessageId = "a", Justification = "Ignore.")]
        public float[][] Spectrum
        {
            get
            {
                if (_spectrum == null)
                {
                    _spectrum = Fft.Transfer(DataIn16Bits);
                }

                return _spectrum;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _spectrum = value;
            }
        }

        /// <summary>
        /// Gets the audio data in 16bits PCM format.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1819:PropertiesShouldNotReturnArrays", Justification = "Ignore.")]
        public short[] DataIn16Bits
        {
            get
            {
                if (_dataIn16Bits == null)
                {
                    if (Format.BlockAlign != 2 || Format.FormatTag != WaveFormatTag.Pcm)
                    {
                        string message = Helper.NeutralFormat(
                            "Only supports PCM and 2 bytes alignment, while it is [{0}] and [{1}].",
                            Format.FormatTag, Format.BlockAlign);
                        throw new NotSupportedException(message);
                    }

                    _dataIn16Bits = new short[GetSoundData().Length / sizeof(short)];
                    Buffer.BlockCopy(GetSoundData(), 0, _dataIn16Bits, 0, GetSoundData().Length);
                }

                return _dataIn16Bits;
            }
        }

        /// <summary>
        /// Gets Waveform time duration in second.
        /// </summary>
        public float Duration
        {
            get
            {
                RiffChunk dataChunk = _riff.GetChunk(Riff.IdData);
                return (float)dataChunk.GetData().Length / _format.AverageBytesPerSecond;
            }
        }

        /// <summary>
        /// Gets or sets Riff for this waveform file.
        /// </summary>
        public Riff Riff
        {
            get
            {
                return _riff;
            }

            set
            {
                if (value != null)
                {
                    _riff = value;
                    RiffChunk fmtChunk = _riff.GetChunk(Riff.IdFormat);
                    _format = new WaveFormat();
                    _format.Load(fmtChunk.GetData());

                    Validate();
                }
                else
                {
                    throw new ArgumentNullException("value");
                }
            }
        }

        /// <summary>
        /// Gets or sets Waveform file format.
        /// </summary>
        public WaveFormat Format
        {
            get
            {
                return _format;
            }

            set
            {
                WaveFormat.Validate(value);

                _format = value;
                if (_riff == null)
                {
                    Initialze();
                }

                RiffChunk fmtChunk = _riff.GetChunk(Riff.IdFormat);
                if (fmtChunk == null)
                {
                    fmtChunk = new RiffChunk();
                    fmtChunk.Id = Riff.IdFormat;
                    _riff.Chunks.Add(fmtChunk);
                }

                fmtChunk.SetData(_format.ToBytes());
                fmtChunk.Size = fmtChunk.GetData().Length;
            }
        }

        /// <summary>
        /// Gets File path.
        /// </summary>
        public string FilePath
        {
            get { return _filePath; }
        }

        #endregion

        #region Public static methods

        /// <summary>
        /// Detect noise in waveform.
        /// </summary>
        /// <param name="samples">Waveform samples.</param>
        /// <returns>Count of noise points.</returns>
        public static long DetectNoise(short[] samples)
        {
            return (long)Microsoft.Tts.ServiceProvider.WaveGenerator.DetectNoise16k16bMono(samples);
        }

        /// <summary>
        /// Read wave file format.
        /// </summary>
        /// <param name="filePath">Waveform file to read format.</param>
        /// <returns>WaveFormat.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Ignore.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "Ignore.")]
        public static WaveFormat ReadFormat(string filePath)
        {
            WaveFormat format;

            try
            {
                using (FileStream fs =
                    new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (BinaryReader br = new BinaryReader(fs))
                {
                    Riff riff = new Riff();
                    riff.LoadHead(br);

                    RiffChunk fmt = new RiffChunk();
                    fmt.Load(br, Riff.IdFormat);

                    format = new WaveFormat();
                    format.Load(fmt.GetData());
                }
            }
            catch (InvalidDataException ide)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Fail to read format of waveform file [{0}] for invalid data.",
                    filePath);
                throw new InvalidDataException(message, ide);
            }

            return format;
        }

        /// <summary>
        /// Read the sample count of a waveform file.
        /// </summary>
        /// <param name="filePath">Waveform file to read sample count.</param>
        /// <returns>Sample count of the waveform file. if -1 is returned.</returns>
        public static int ReadSampleCount(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            WaveFormat format;

            try
            {
                FileStream fs = new FileStream(filePath,
                    FileMode.Open, FileAccess.Read, FileShare.Read);
                try
                {
                    using (BinaryReader br = new BinaryReader(fs))
                    {
                        fs = null;
                        Riff riff = new Riff();
                        riff.LoadHead(br);

                        RiffChunk fmt = new RiffChunk();
                        fmt.Load(br, Riff.IdFormat);

                        format = new WaveFormat();
                        format.Load(fmt.GetData());

                        int chunkId = 0;
                        int dataSize = 0;
                        do
                        {
                            chunkId = br.ReadInt32();
                            dataSize = br.ReadInt32();
                            if (dataSize < 0)
                            {
                                string message = string.Format(CultureInfo.InvariantCulture,
                                    "Invalid data size [{0}], which should not be negative integer.",
                                    dataSize);
                                throw new InvalidDataException(message);
                            }

                            long currPos = br.BaseStream.Position;
                            long newPos = br.BaseStream.Seek(dataSize, SeekOrigin.Current);
                            if (newPos != currPos + dataSize)
                            {
                                string message = string.Format(CultureInfo.InvariantCulture,
                                    "Invalid data size [{0}], which may be too large.",
                                    dataSize);
                                throw new InvalidDataException(message);
                            }
                        }
                        while (Riff.IdData != chunkId);

                        if (Riff.IdData != chunkId)
                        {
                            string message = string.Format(CultureInfo.InvariantCulture,
                                "Invalid waveform format for not data chunk found in file {0}",
                                filePath);
                            throw new InvalidDataException(message);
                        }

                        return dataSize / (format.BlockAlign * format.Channels);
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
            catch (InvalidDataException ide)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Fail to read sample count of waveform file [{0}] for invalid data.",
                    filePath);
                throw new InvalidDataException(message, ide);
            }
            catch (EndOfStreamException ese)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Fail to read sample count of waveform file [{0}] for no enough data.",
                    filePath);
                throw new InvalidDataException(message, ese);
            }
        }

        /// <summary>
        /// Load a waveform file from file.
        /// </summary>
        /// <param name="filePath">Waveform file to load from.</param>
        /// <returns>WaveFile.</returns>
        public static WaveFile ReadWaveFile(string filePath)
        {
            WaveFile waveFile = new WaveFile();
            waveFile.Load(filePath);

            return waveFile;
        }

        /// <summary>
        /// Write a Single[] to a WAV file. Mostly for debugging purposes, this
        /// Routine generates a WAV file whose duration is the same as
        /// That of the original waveform.
        /// </summary>
        /// <param name="filePath">Target file to save.</param>
        /// <param name="outWave">Waveform samples to save.</param>
        /// <param name="samplesPerSecond">Samples per second.</param>
        public static void WriteWaveFile(string filePath, float[] outWave, int samplesPerSecond)
        {
            short[] waveData = ArrayHelper.ToInt16<float>(outWave);

            WaveFormat waveFormat = new WaveFormat();
            waveFormat.Channels = 1;
            waveFormat.BlockAlign = 2;
            waveFormat.BitsPerSample = 16;
            waveFormat.ExtSize = 0;
            waveFormat.FormatTag = WaveFormatTag.Pcm;
            waveFormat.SamplesPerSecond = samplesPerSecond;
            waveFormat.AverageBytesPerSecond = checked(samplesPerSecond * 2);

            WaveFile waveFile = new WaveFile();
            waveFile.Format = waveFormat;

            RiffChunk waveDataChunk = waveFile.Riff.GetChunk(Riff.IdData);
            byte[] byteData = ArrayHelper.BinaryConvertArray(waveData);
            waveDataChunk.SetData(byteData);
            waveDataChunk.Size = waveDataChunk.GetData().Length;

            waveFile.Save(filePath);
        }

        /// <summary>
        /// Split a 2-channel waveform file into two waveform file.
        /// </summary>
        /// <param name="waveFile">Source 2-channel waveform file instance.</param>
        /// <returns>2 waveform files in the collection.</returns>
        public static WaveFile[] SplitIntoTwoChannels(WaveFile waveFile)
        {
            if (waveFile == null)
            {
                throw new ArgumentNullException("waveFile");
            }

            if (waveFile.Format.Channels != 2)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Only support split two channels waveform files.");
                throw new NotSupportedException(message);
            }

            WaveFormat targetFormat = new WaveFormat();
            targetFormat = waveFile.Format;
            targetFormat.Channels = 1;
            targetFormat.BlockAlign /= waveFile.Format.Channels;
            targetFormat.AverageBytesPerSecond /= waveFile.Format.Channels;

            byte[][] channels = waveFile.SplitChannels();
            if (channels.Length != 2)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Invalid channel number [{0}] found for splitting [{1}], which should equal to 2.",
                    channels.Length, waveFile.FilePath);
                throw new InvalidDataException(message);
            }

            WaveFile[] files = new WaveFile[waveFile.Format.Channels];

            files[0] = new WaveFile();
            files[0].Format = targetFormat;

            RiffChunk firstChannelWaveDataChunk = files[0].Riff.GetChunk(Riff.IdData);
            firstChannelWaveDataChunk.SetData(channels[0]);

            files[1] = new WaveFile();
            files[1].Format = targetFormat;

            RiffChunk secondChannelWaveDataChunk = files[1].Riff.GetChunk(Riff.IdData);
            secondChannelWaveDataChunk.SetData(channels[1]);

            return files;
        }

        /// <summary>
        /// Merge two waveform files into 2-channel waveform file.
        /// </summary>
        /// <param name="leftFile">Left waveform file for left channel, i.e. first channel.</param>
        /// <param name="rightFile">Right waveform file for left channel, i.e. second channel.</param>
        /// <returns>Merged waveform file.</returns>
        public static WaveFile MergeTwoChannels(WaveFile leftFile, WaveFile rightFile)
        {
            if (leftFile == null)
            {
                throw new ArgumentNullException("leftFile");
            }

            if (rightFile == null)
            {
                throw new ArgumentNullException("rightFile");
            }

            if (leftFile.Format != rightFile.Format)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Both waveform files should share the same formant.");
                throw new InvalidDataException(message);
            }

            if (leftFile.GetSoundData().Length != rightFile.GetSoundData().Length)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Both waveform files should have the same samples.");
                throw new InvalidDataException(message);
            }

            if (leftFile.Format.Channels != 1)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Only single channel waveform file is supported to merge.");
                throw new InvalidDataException(message);
            }

            WaveFile targetFile = new WaveFile();
            WaveFormat format = leftFile.Format;
            format.Channels = 2;
            format.AverageBytesPerSecond *= format.Channels;
            format.BlockAlign *= format.Channels;
            targetFile.Format = format;

            byte[] data = new byte[leftFile.GetSoundData().Length * format.Channels];

            for (int i = 0; i < leftFile.GetSoundData().Length; i += leftFile.Format.BlockAlign)
            {
                Buffer.BlockCopy(leftFile.GetSoundData(), i,
                    data, i * format.Channels, leftFile.Format.BlockAlign);
                Buffer.BlockCopy(rightFile.GetSoundData(), i,
                    data, (i * format.Channels) + leftFile.Format.BlockAlign, leftFile.Format.BlockAlign);
            }

            RiffChunk chunk = targetFile.Riff.GetChunk(Riff.IdData);
            chunk.SetData(data);

            return targetFile;
        }

        #endregion

        #region Public IO methods

        /// <summary>
        /// Save this instance to waveform file.
        /// </summary>
        /// <param name="filePath">Target file path.</param>
        public void Save(string filePath)
        {
            Validate();
            Riff.SaveWaveFile(filePath, _riff);
        }

        /// <summary>
        /// Load wavefile instance from waveform file path.
        /// </summary>
        /// <param name="filePath">Source file to load.</param>
        public void Load(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new ArgumentException(filePath, new FileNotFoundException(filePath));
            }

            _filePath = filePath;
            Riff riff = Riff.ReadWaveFile(filePath);

            try
            {
                this.Riff = riff;
            }
            catch (InvalidDataException ide)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Fail to read waveform file [{0}] for invalid data.",
                    filePath);
                throw new InvalidDataException(message, ide);
            }
        }

        /// <summary>
        /// Load wavefile instance from waveform stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        public void Load(Stream stream)
        {
            _filePath = string.Empty;

            Riff riff = Riff.ReadWave(stream);

            try
            {
                this.Riff = riff;
            }
            catch (InvalidDataException ide)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Fail to read stream for invalid data.");
                throw new InvalidDataException(message, ide);
            }
        }

        #endregion

        #region Public edit methods

        /// <summary>
        /// Split all channels to byte array.
        /// </summary>
        /// <returns>Splitted channels data.</returns>
        public byte[][] SplitChannels()
        {
            if (_format.Channels <= 0)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Channel number should not be less than 1, invalid channel number [{0}] in file [{1}].",
                    _format.Channels, _filePath);
                throw new InvalidDataException(message);
            }

            byte[] sourceSamples = GetSoundData();

            if (sourceSamples == null)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Wave data is empty in wavefile, please load waveform file first.");
                throw new InvalidDataException(message);
            }

            byte[][] channels = new byte[_format.Channels][];

            for (int i = 0; i < _format.Channels; i++)
            {
                channels[i] = new byte[sourceSamples.Length / _format.Channels];
            }

            int channelBlockAlign = _format.BlockAlign / _format.Channels;
            int sampleNumber = sourceSamples.Length / _format.BlockAlign;

            for (int i = 0; i < _format.Channels; i++)
            {
                int channelOffset = i * channelBlockAlign;

                for (int j = 0; j < sampleNumber; j++)
                {
                    int sourceChannelBlockOffset = (j * _format.BlockAlign) + channelOffset;
                    int targetChannelBlockOffset = j * channelBlockAlign;

                    for (int k = 0; k < channelBlockAlign; k++)
                    {
                        channels[i][targetChannelBlockOffset + k] =
                            sourceSamples[sourceChannelBlockOffset + k];
                    }
                }
            }

            return channels;
        }

        /// <summary>
        /// Cut certain piece of data in this waveform file.
        /// </summary>
        /// <param name="startTime">Start time in second.</param>
        /// <param name="duration">Waveform time duration in second.</param>
        /// <returns>Cut wavefile.</returns>
        public WaveFile Cut(double startTime, double duration)
        {
            if (startTime < 0.0f)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "The start time  [{0}] of location in waveform should not be negative.",
                    startTime);
                throw new ArgumentException(message);
            }

            if (duration <= 0.0f)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "The duration time  [{0}] of location in waveform should be greater than zero.",
                    duration);
                throw new ArgumentException(message);
            }

            WaveFile wf = new WaveFile();
            wf.Riff = DoCut(startTime, duration);
            return wf;
        }

        /// <summary>
        /// Append other wavefile instance to this instance.
        /// </summary>
        /// <param name="wf">Wave file.</param>
        public void Append(WaveFile wf)
        {
            if (wf == null)
            {
                throw new ArgumentNullException("wf");
            }

            if (_riff == null)
            {
                Initialze();
                Format = wf.Format;
            }

            if (!Format.Equals(wf.Format))
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Current format should not be different with the waveform file to append.");
                throw new ArgumentException(message, "wf");
            }

            RiffChunk dataChunk = _riff.GetChunk(Riff.IdData);
            if (dataChunk == null)
            {
                dataChunk = new RiffChunk();
                dataChunk.Id = Riff.IdData;
                _riff.Chunks.Add(dataChunk);
            }

            dataChunk.Append(wf.GetSoundData());
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Get waveform data in byte array.
        /// </summary>
        /// <returns>Sound data.</returns>
        public byte[] GetSoundData()
        {
            if (_riff == null)
            {
                return null;
            }

            RiffChunk dataChunk = _riff.GetChunk(Riff.IdData);
            return dataChunk.GetData();
        }

        /// <summary>
        /// Change amplitude of this waveform file. please call 
        /// CalcMaxAmplifyFactor function first to get the max factor to avoid 
        /// Get the voice data clipped.
        /// This function will not apply DC (Direct Current) bias adjustation.
        /// </summary>
        /// <param name="factor">
        /// If the factor is less than 1.0 decreases the amplitude;
        /// If the factor is greater than 1.0 increases the amplitude.
        /// if use a negative number to invert the phase of the speech data.</param>
        public void Amplify(double factor)
        {
            EnsureTwoBytesPerSample();
            short[] dataInShort = DataIn16Bits;
            Amplify(dataInShort, factor);
            Buffer.BlockCopy(dataInShort, 0, GetSoundData(), 0, GetSoundData().Length);
        }

        /// <summary>
        /// Check wave file's format according to setting value.
        /// </summary>
        /// <param name="samplesPerSecond">Sample per second.</param>
        /// <param name="channel">Channel number.</param>
        /// <param name="bytesPerSample">Bytes per sample.</param>
        public void CheckWaveFormat(int samplesPerSecond, int channel, int bytesPerSample)
        {
            if (this.Format.SamplesPerSecond != samplesPerSecond)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "The wave file[{0}]'s sample rate [{1}] is not equal to setting value [{2}].",
                    this.FilePath, this.Format.SamplesPerSecond, samplesPerSecond);
                throw new InvalidDataException(message);
            }

            if (this.Format.Channels != channel)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "The wave file[{0}]'s channel number [{1}] is not equal to setting value [{2}].",
                    this.FilePath, this.Format.Channels, channel);
                throw new InvalidDataException(message);
            }

            int bytes = this.Format.BlockAlign / this.Format.Channels;
            if (bytes != bytesPerSample)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                   "The wave file[{0}]'s bytes per sample [{1}] is not equal to setting value [{2}].",
                   this.FilePath, bytes, bytesPerSample);
                throw new NotSupportedException(message);
            }
        }

        /// <summary>
        /// Change amplitude of this waveform file.
        /// The clipped sample will be reported 
        /// The length of clipped = length of amplified data + 1, first element of clipped is used to save the number of clipped samples
        /// The other elements save the postion of clipped samples
        /// This function will not apply DC (Direct Current) bias adjustation.
        /// </summary>
        /// <param name="factor">
        /// If the factor is less than 1.0 decreases the amplitude;
        /// If the factor is greater than 1.0 increases the amplitude.
        /// if use a negative number to invert the phase of the speech data.</param>
        /// <param name="clipped">Clipped sample position.</param>
        public void AmplifyClippedReport(double factor, int[] clipped)
        {
            EnsureTwoBytesPerSample();
            short[] dataInShort = DataIn16Bits;
            AmplifyClippedReport(dataInShort, factor, clipped);
            Buffer.BlockCopy(dataInShort, 0, GetSoundData(), 0, GetSoundData().Length);
        }
        
        /// <summary>
        /// Calculate the amplifying factor for all the samples as loud as 
        /// Possible without clipping.
        /// </summary>
        /// <returns>Maximum amplifying factor for this waveform file.</returns>
        public double CalcMaxAmplifyFactor()
        {
            EnsureTwoBytesPerSample();

            short maxVolume = SearchMaxVolume(DataIn16Bits);
            if (maxVolume == 0)
            {
                return 0;
            }
            else if (maxVolume > 0)
            {
                return (double)short.MaxValue / maxVolume;
            }
            else
            {
                return (double)short.MinValue / maxVolume;
            }
        }
        #endregion

        #region Static operations

        /// <summary>
        /// Amplify byte stream (16bits audio data).
        /// </summary>
        /// <param name="data">Data to be amplified.</param>
        /// <param name="factor">Amplify factor.</param>
        private static void Amplify(short[] data, double factor)
        {
            for (int i = 0; i < data.Length; i++)
            {
                double value = data[i] * factor;
                if (value >= 0)
                {
                    value += 0.5f;
                    if (value > short.MaxValue)
                    {
                        // clipped
                        data[i] = short.MaxValue;
                    }
                    else
                    {
                        data[i] = NumberConverter.Double2Int16(value);
                    }
                }
                else
                {
                    value -= 0.5f;
                    if (value < short.MinValue)
                    {
                        // clipped
                        data[i] = short.MinValue;
                    }
                    else
                    {
                        data[i] = NumberConverter.Double2Int16(value);
                    }
                }
            }
        }

         /// <summary>
        /// Amplify byte stream (16bits audio data). Report the clipped sample
        /// The length of clipped = length of data + 1, first element of clipped is used to save the number of clipped samples
        /// The other elements save the postion of clipped samples.
        /// </summary>
        /// <param name="data">Data to be amplified.</param>
        /// <param name="factor">Amplify factor.</param>
        /// <param name="clipped">Clipped sample position.</param>
        private static void AmplifyClippedReport(short[] data, double factor, int[] clipped)
        {
            int clippedCounter = 1;
            for (int i = 0; i < data.Length; i++)
            {
                double value = data[i] * factor;
                if (value >= 0)
                {
                    value += 0.5f;
                    if (value > short.MaxValue)
                    {
                        // clipped
                        data[i] = short.MaxValue;
                        clipped[clippedCounter] = i;
                        clippedCounter ++;
                   }
                    else
                    {
                        data[i] = NumberConverter.Double2Int16(value);
                    }
                }
                else
                {
                    value -= 0.5f;
                    if (value < short.MinValue)
                    {
                        // clipped
                        data[i] = short.MinValue;
                        clipped[clippedCounter] = i;
                        clippedCounter ++;
                    }
                    else
                    {
                        data[i] = NumberConverter.Double2Int16(value);
                    }
                }
            }

            clipped[0] = clippedCounter - 1;
        }

        /// <summary>
        /// Search max volume.
        /// </summary>
        /// <param name="data">Samples.</param>
        /// <returns>Maximum volume.</returns>
        private static short SearchMaxVolume(short[] data)
        {
            short maxValue = 0;
            int maxAbsValue = 0;
            for (int i = 0; i < data.Length; i++)
            {
                // if don't use value directly, when the value = short.MinValue
                // for the return type of Math.Abs is short, so it will overflow
                if (Math.Abs(checked((int)data[i])) > maxAbsValue)
                {
                    maxAbsValue = Math.Abs(checked((int)data[i]));
                    maxValue = data[i];
                }
            }

            return maxValue;
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Initialize this instance.
        /// </summary>
        private void Initialze()
        {
            _riff = new Riff();
            RiffChunk dataChunk = new RiffChunk();
            dataChunk.Id = Riff.IdData;
            _riff.Chunks.Add(dataChunk);
        }

        /// <summary>
        /// Ensure there are two bytes for each sample.
        /// </summary>
        private void EnsureTwoBytesPerSample()
        {
            int bytesPerSample = this.Format.BlockAlign / this.Format.Channels;
            if (bytesPerSample != 2)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Only 2 bytes per sample of waveform is supported to" + "calculate maximum amplify factor. But {0} bytes per sample is required.",
                    bytesPerSample);
                throw new NotSupportedException(message);
            }
        }

        /// <summary>
        /// Exactly do cut operation.
        /// </summary>
        /// <param name="starttime">Start time in second.</param>
        /// <param name="duration">Waveform time duration in second.</param>
        /// <returns>Riff of the cut piece.</returns>
        private Riff DoCut(double starttime, double duration)
        {
            int samplePosition = checked((int)(starttime * Format.SamplesPerSecond));
            int sampleNumber = checked((int)Math.Round(duration * Format.SamplesPerSecond));
            if (sampleNumber == 0)
            {
                sampleNumber = 1;
            }

            return DoCut(samplePosition, sampleNumber);
        }

        /// <summary>
        /// Exactly do cut operation.
        /// </summary>
        /// <param name="samplePosition">Start sample position.</param>
        /// <param name="sampleNumber">Waveform sample length.</param>
        /// <returns>Riff of the cut piece.</returns>
        private Riff DoCut(int samplePosition, int sampleNumber)
        {
            if (this.Format.Channels != 1)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Does not support to cut part waveform in a multi-channel wave file instance.");
                throw new NotSupportedException(message);
            }

            Riff riff = new Riff();
            riff.RiffType = Riff.IdWave;

            RiffChunk fmt = this.Riff.GetChunk(Riff.IdFormat);
            riff.Chunks.Add(fmt);

            RiffChunk data = new RiffChunk();
            data.Id = Riff.IdData;

            int startbyte = samplePosition * this.Format.BlockAlign;
            int sizebyte = sampleNumber * this.Format.BlockAlign;

            data.SetData(new byte[sizebyte]);
            byte[] src = this.GetSoundData();
            int maxSizeToCopy = src.Length - startbyte;
            int sizeToCopy = sizebyte <= maxSizeToCopy ? sizebyte : maxSizeToCopy;
            Array.Copy(src, startbyte, data.GetData(), 0, sizeToCopy);
            riff.Chunks.Add(data);
            data.Size = sizebyte;

            riff.Size = fmt.TotalSize + data.TotalSize + 4;

            return riff;
        }

        /// <summary>
        /// Validate.
        /// </summary>
        private void Validate()
        {
            RiffChunk fmtChunk = _riff.GetChunk(Riff.IdFormat);
            if (fmtChunk == null)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "There is no fmt (Format) chunk in the wave file.");
                throw new InvalidDataException(message);
            }

            RiffChunk dataChunk = _riff.GetChunk(Riff.IdData);
            if (dataChunk == null)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "There is no Data chunk in the wave file.");
                throw new InvalidDataException(message);
            }

            if (dataChunk.GetData().Length % Format.BlockAlign != 0)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Data lenght [{0}] does not align with the block align [{1}].",
                    dataChunk.GetData().Length, Format.BlockAlign);
                throw new InvalidDataException(message);
            }
        }

        #endregion
    }
}