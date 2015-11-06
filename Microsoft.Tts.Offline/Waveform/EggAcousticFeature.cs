//----------------------------------------------------------------------------
// <copyright file="EggAcousticFeature.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This class get the pitch, pitch range and epoch data from the
//     laryngograph data(EGG signal).
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Waveform
{
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using Microsoft.Tts.Offline.Utility;
    using Microsoft.Tts.Offline.Waveform;

    /// <summary>
    /// Data range.
    /// </summary>
    /// <typeparam name="T">Type of the begin or end value of this range.</typeparam>
    public class DataRange<T>
    {
        #region Fields

        private T _begin;
        private T _end;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="DataRange{T}"/> class..
        /// </summary>
        /// <param name="begine">Begin of the range.</param>
        /// <param name="end">End of the range.</param>
        public DataRange(T begine, T end)
        {
            _begin = begine;
            _end = end;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Begine of this range.
        /// </summary>
        public T Begin
        {
            get { return _begin; }
            set { _begin = value; }
        }

        /// <summary>
        /// Gets or sets End of this range.
        /// </summary>
        public T End
        {
            get { return _end; }
            set { _end = value; }
        }

        #endregion
    }

    /// <summary>
    /// Egg Acoustic Feature calculation.
    /// </summary>
    public class EggAcousticFeature
    {
        #region Private field

        private float[] _eggData;

        private int[] _epoch;
        private int[] _epochPosition;

        // for 8k epoch data, which is calculated from _epoch array
        private int[] _epoch8k;

        private WaveFile _waveFile;

        private int _minPitchFreq = 30;

        private int _maxPitchFreq = 500;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets min pitch frequenct that used when estimate epoch in Load method.
        /// </summary>
        public int MinPitchFreq
        {
            get { return _minPitchFreq; }
            set { _minPitchFreq = value; }
        }

        /// <summary>
        /// Gets or sets max pitch frequenct that used when estimate epoch in Load method.
        /// </summary>
        public int MaxPitchFreq
        {
            get { return _maxPitchFreq; }
            set { _maxPitchFreq = value; }
        }

        /// <summary>
        /// Gets Loaded WaveFile.
        /// </summary>
        public WaveFile WaveFile
        {
            get { return _waveFile; }
        }

        /// <summary>
        /// Gets The data can be getted from WaveFile property,
        /// Keep it for compatible with older version.
        /// </summary>
        public int SamplesPerSecond
        {
            get
            {
                if (_waveFile != null)
                {
                    return _waveFile.Format.SamplesPerSecond;
                }

                // default sampling rate.
                return 16000;
            }
        }

        /// <summary>
        /// Gets Sample number count.
        /// </summary>
        public int SampleNumber
        {
            get
            {
                Debug.Assert(_eggData == null || _eggData.Length ==
                    Math.Abs(_epochPosition[_epochPosition.Length - 1]));
                return Math.Abs(_epochPosition[_epochPosition.Length - 1]);
            }
        }

        /// <summary>
        /// Gets Epoch data of this file.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1819:PropertiesShouldNotReturnArrays", Justification = "Ignore.")]
        public int[] Epoch
        {
            get { return _epoch; }
        }

        /// <summary>
        /// Gets 8k epoch data of this file.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1819:PropertiesShouldNotReturnArrays", Justification = "Ignore.")]
        public int[] Epoch8k
        {
            get
            {
                if (_epoch8k != null)
                {
                    return _epoch8k;
                }

                _epoch8k = EpochFile.EpochTo8k(_epoch);

                return _epoch8k;
            }
        }

        /// <summary>
        /// Gets Epoch related position of this file.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1819:PropertiesShouldNotReturnArrays", Justification = "Ignore.")]
        public int[] EpochPosition
        {
            get { return _epochPosition; }
        }

        #endregion

        #region Public static method

        /// <summary>
        /// Estimate epoch from EGG samples.
        /// </summary>
        /// <exception cref="System.ArgumentNullException">Thrown when
        /// EggSamples parameter is null or doesn't contain any elements.</exception>
        /// <exception cref="System.IO.InvalidDataException">Thrown when
        /// The calculated epoch number is less then 5.</exception>
        /// <param name="eggSamples">EGG waveform samples.</param>
        /// <param name="samplesPerSecond">Samples per second.</param>
        /// <param name="minPitchFreq">Minimum pitch frequency.</param>
        /// <param name="maxPitchFreq">Maximum pitch frequency.</param>
        /// <param name="positive">Positive waveform or not.</param>
        /// <returns>Epoch data.</returns>
        public static int[] EstimateEpochs(float[] eggSamples,
            int samplesPerSecond, int minPitchFreq, int maxPitchFreq,
            ref bool positive)
        {
            if (eggSamples == null || eggSamples.Length == 0)
            {
                throw new ArgumentNullException("eggSamples");
            }

            int maxPitchPeriod = samplesPerSecond / minPitchFreq;
            int minPitchPeriod = samplesPerSecond / maxPitchFreq;

            if (eggSamples.Length < maxPitchPeriod)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Invalid EGG data for the total EGG sample count [{0}] is less than maximum pitch period [{1}] for each pitch point.",
                    eggSamples.Length, maxPitchPeriod);
                throw new InvalidDataException(message);
            }

            float[] finalSamples = new float[eggSamples.Length];
            Preemphasis(eggSamples, finalSamples);

            float[] lowPassSamples = new float[eggSamples.Length];
            LowPass2(finalSamples, lowPassSamples);

            // compute sign and envelope of input and scale waveform
            positive = Envelope(lowPassSamples, finalSamples, minPitchPeriod,
                maxPitchPeriod);

            float[] positiveSamples = lowPassSamples;

            // find peaks in input
            int minPulseAmplitude = PeakPicking(positiveSamples, finalSamples,
                samplesPerSecond);

            RemoveCloseEpochs(finalSamples, minPitchPeriod);

            // obtain epoch sequence from processed input waveform
            float[] epochs = new float[eggSamples.Length];
            int epochNumber = GetEpochs(finalSamples, epochs);

            if (epochNumber <= 5)
            {
                string meesage = string.Format(CultureInfo.InvariantCulture,
                    "There are only {0} epoch points found in the data, which are too less for further epoch estimation",
                    epochNumber);
                throw new InvalidDataException(meesage);
            }

            // smooth the epoch sequence
            float[] smoothedEpochs = new float[eggSamples.Length];

            MedianSmoothing(epochs, smoothedEpochs, epochNumber, 5,
                minPitchPeriod, maxPitchPeriod);

            // add pulses so that two pulses are not farther than the maximum
            // pitch period. If high score add them as voiced, else as unvoiced
            SmoothEpochs(positiveSamples, finalSamples, epochs, smoothedEpochs,
                ref epochNumber, minPulseAmplitude);

            int[] finalEpochs = ArrayHelper.ToInt32<float>(epochs, epochNumber);

            return finalEpochs;
        }

        #endregion

        #region Pulic methods

        /// <summary>
        /// Convert wave16k+egg16k to epoch.
        /// </summary>
        /// <param name="wave16kFilePath">Specifies the directory of wave16k waveform files.</param>
        /// <param name="egg16kFilePath">Specifies the directory of EGG16k waveform files.</param>
        /// <param name="epochFilePath">Specifies the directory of epoch waveform files.</param>
        /// <param name="bandPassLowFreq">Specifies the low frequency of bandpass filter.</param>
        /// <param name="bandPassHighFreq">Specifies the high frequency of bandpass filter.</param>
        /// <param name="bandPassOrder">Specifies the order of bandpass filter.</param>
        /// <param name="larEpochMinPitch">Specifies the min pitch when doing lar epoch.</param>
        /// <param name="larEpochMaxPitch">Specifies the max pitch when doing lar epoch.</param>
        /// <param name="frameSize">Specifies the frameSize when adjust epoch.</param>
        /// <param name="lpcOrder">Specifies the LPC order.</param>
        /// <param name="adjustFreqOffset">Specifies offset when doing adjust frequency.</param>
        /// <returns>True.</returns>
        public static bool Egg2Epoch(string wave16kFilePath,
            string egg16kFilePath, string epochFilePath,
            double bandPassLowFreq, double bandPassHighFreq,
            int bandPassOrder, int larEpochMinPitch,
            int larEpochMaxPitch, int frameSize,
            int lpcOrder, int adjustFreqOffset)
        {
            if (string.IsNullOrEmpty(wave16kFilePath) || !File.Exists(wave16kFilePath))
            {
                throw Helper.CreateException(typeof(FileNotFoundException),
                    wave16kFilePath);
            }

            if (string.IsNullOrEmpty(egg16kFilePath) || !File.Exists(egg16kFilePath))
            {
                throw Helper.CreateException(typeof(FileNotFoundException),
                    egg16kFilePath);
            }

            // load egg16k and wave16k file
            WaveFile eggFile = new WaveFile();
            eggFile.Load(egg16kFilePath);
            WaveFile waveFile = new WaveFile();
            waveFile.Load(wave16kFilePath);
            if (waveFile.DataIn16Bits.Length != eggFile.DataIn16Bits.Length)
            {
                throw new InvalidDataException(Helper.NeutralFormat(
                    "Mismatch wave data length between wave file [{0}] and egg file [{1}]",
                    waveFile.DataIn16Bits.Length, eggFile.DataIn16Bits.Length));
            }

            BandpassFilter filter = new BandpassFilter(
                bandPassLowFreq, bandPassHighFreq,
                bandPassOrder, eggFile.Format.SamplesPerSecond);

            // filter egg16k to egg8k
            short[] egg8k = filter.Filter(eggFile.DataIn16Bits);

            // lar epoch from egg8k to epoch
            bool positive = true;
            float[] floatEgg8k = ArrayHelper.ToSingle<short>(egg8k);

            int[] epochs = null;
            try
            {
                epochs = EggAcousticFeature.EstimateEpochs(
                    floatEgg8k,
                    eggFile.Format.SamplesPerSecond,
                    larEpochMinPitch, larEpochMaxPitch, ref positive);
            }
            catch (InvalidDataException ide)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Fail to build epoch data from load EGG file [{0}].",
                    egg16kFilePath);
                throw new InvalidDataException(message, ide);
            }

            // use int[] between EstimateEpochs and AdjustFreq to keep
            // consistance with the origianl c++ arithematic.
            float[] floatEpochs = ArrayHelper.ToSingle<int>(epochs);

            // adjust epoch.
            int[] adjustedEpoch = AdjustFreq(waveFile.DataIn16Bits,
                floatEpochs, positive, waveFile.Format.SamplesPerSecond,
                frameSize, lpcOrder, adjustFreqOffset);

            // calculate final epoch
            FlipWobbleEpoch(adjustedEpoch);

            int[] epochOffsets = EpochFile.EpochToOffset(epochs);
            if (Math.Abs(epochOffsets[epochOffsets.Length - 1]) != eggFile.DataIn16Bits.Length)
            {
                throw new InvalidDataException(Helper.NeutralFormat(
                    "Invalid generated epoch length [{0}], mismatch with egg file sample length [{1}]",
                    epochOffsets[epochOffsets.Length - 1], eggFile.DataIn16Bits.Length));
            }

            // write epoch
            EpochFile.WriteAllDecodedData(epochFilePath, adjustedEpoch);

            return true;
        }

        /// <summary>
        /// Load a laryngograph wave file (EGG data).
        /// </summary>
        /// <exception cref="System.NotSupportedException">Thrown when the waveform file
        /// Is not 16 bit per sample.</exception>
        /// <param name="eggFilePath">File path of EGG16k waveform file.</param>
        public void Load(string eggFilePath)
        {
            _waveFile = new WaveFile();
            _waveFile.Load(eggFilePath);

            switch (_waveFile.Format.BitsPerSample)
            {
                case 16:
                    short[] data = ArrayHelper.BinaryConvertArray(
                        _waveFile.GetSoundData());
                    _eggData = new float[data.Length];
                    for (int i = 0; i < data.Length; ++i)
                    {
                        _eggData[i] = (float)data[i];
                    }

                    break;
                default:
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Only 16 bits per sample waveform is supported. But it is {0} bits per sample of waveform file [{1}].",
                        _waveFile.Format.BitsPerSample, eggFilePath);
                    throw new NotSupportedException(message);
            }

            bool positive = true;
            try
            {
                _epoch = EstimateEpochs(_eggData, WaveFile.Format.SamplesPerSecond,
                    _minPitchFreq, _maxPitchFreq, ref positive);
            }
            catch (InvalidDataException ide)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Fail to load EGG file [{0}] and build epoch data.",
                    eggFilePath);
                throw new InvalidDataException(message, ide);
            }

            _epoch8k = null;
            _epochPosition = EpochFile.EpochToOffset(_epoch);
        }

        /// <summary>
        /// Load uncompressed epoch data from file.
        /// </summary>
        /// <param name="epochFilePath">File path of epoch waveform file.</param>
        public void LoadEpoch(string epochFilePath)
        {
            // there is no epoch data assigned
            _eggData = null;
            _epoch = EpochFile.ReadAllDecodedData(epochFilePath, 0);
            _epoch8k = null;
            _epochPosition = EpochFile.EpochToOffset(_epoch);
        }

        /// <summary>
        /// Save epoch data to file.
        /// </summary>
        /// <param name="filePath">Target epoch file path to save.</param>
        public void SaveEpochFile(string filePath)
        {
            if (_epoch != null)
            {
                Helper.EnsureFolderExistForFile(filePath);
                FileStream filestream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                try
                {
                    using (BinaryWriter bw = new BinaryWriter(filestream))
                    {
                        filestream = null;
                        foreach (int i in _epoch)
                        {
                            bw.Write(i);
                        }
                    }
                }
                finally
                {
                    if (null != filestream)
                    {
                        filestream.Dispose();
                    }
                }
            }
        }

        /// <summary>
        /// AdjustAlignment() is used to adjust force alignment result
        /// Using epoch information, and experimental experience.
        /// </summary>
        /// <param name="sampleOffset">Sample offset.</param>
        /// <returns>Index of epoch.
        /// For i is index of _EpochPosition and the first element of
        /// _EpochPosition is not zero, so we need plus 1 to get the
        /// Index of epoch.
        /// </returns>
        public int AdjustAlignment(ref int sampleOffset)
        {
            Debug.Assert(_epochPosition != null);
            Debug.Assert(_epochPosition.Length > 1);

            // assert the length of EGG data is same as that of epoch position
            Debug.Assert(_eggData == null || _eggData.Length ==
                Math.Abs(_epochPosition[_epochPosition.Length - 1]));

            if (sampleOffset < 0)
            {
                sampleOffset = 0;
            }
            else if (sampleOffset >= Math.Abs(_epochPosition[_epochPosition.Length - 1]))
            {
                sampleOffset = Math.Abs(_epochPosition[_epochPosition.Length - 1]) - 1;
            }

            int i;
            for (i = 1; i < _epochPosition.Length; ++i)
            {
                if (Math.Abs(_epochPosition[i]) >= sampleOffset)
                {
                    break;
                }
            }

            // adjusting to the nearest epoch position.
            // usually voiced to voiced or unvoiced to unvoiced or
            // just want to align to epoch position
            if (sampleOffset - Math.Abs(_epochPosition[i - 1]) <
                Math.Abs(_epochPosition[i]) - sampleOffset)
            {
                --i;
            }

            sampleOffset = Math.Abs(_epochPosition[i]);

            // For i is index of epochPosition and the first element of epochPosition is not zero,
            // so we need plus 1 to get the index of epoch.
            // if you want calculate sum of epoch by index retured by this,
            // you should use like this : for (int i = start; i < stop; i++){ sum += epochs[i]; }
            Debug.Assert(i + 1 <= _epoch.Length);
            return i + 1;
        }

        /// <summary>
        /// Get the pitch and range of the first voiced period.
        /// </summary>
        /// <param name="sampleOffset">Sample offset.</param>
        /// <param name="sampleLength">Sample length.</param>
        /// <param name="averagePitch">Average pitch value.</param>
        /// <param name="pitchRange">Pitch Range.</param>
        /// <returns>True if voiced segment, otherwise false.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", Justification = "Ignore.")]
        public bool GetPitchAndRange(int sampleOffset, int sampleLength,
            out float averagePitch, out float pitchRange)
        {
            float[] validPitches = GetValidPitches(sampleOffset, sampleLength);

            if (validPitches == null)
            {
                // No voiced segment in the unit
                // All pitch values are zero for this unit.
                // Give return fake AveragePitch and PitchRange
                averagePitch = 180.0f;
                pitchRange = 18.0f;
            }
            else
            {
                float pitchSum = 0.0f;
                for (int index = 0; index < validPitches.Length; ++index)
                {
                    pitchSum += validPitches[index];
                }

                averagePitch = pitchSum / validPitches.Length;

                pitchSum = 0.0f;
                for (int index = 0; index < validPitches.Length; ++index)
                {
                    pitchSum += Math.Abs(validPitches[index] - averagePitch);
                }

                pitchRange = pitchSum / validPitches.Length;
            }

            return true;
        }

        /// <summary>
        /// Get valid pitches for given range of waveform samples.
        /// </summary>
        /// <param name="sampleOffset">The begin sample offset of the waveform.</param>
        /// <param name="sampleLength">The sample range of the waveform.</param>
        /// <returns>Pitches.</returns>
        public float[] GetValidPitches(int sampleOffset, int sampleLength)
        {
            int[] validEpochs = GetValidEpochs(sampleOffset, sampleLength);

            float[] validPitches = null;
            if (validEpochs != null)
            {
                validPitches = new float[validEpochs.Length];

                // If waveFile is null , use default sample rate.
                int samplesPerSecond = 16000;
                if (WaveFile != null)
                {
                    samplesPerSecond = WaveFile.Format.SamplesPerSecond;
                }

                for (int j = 0; j < validPitches.Length; ++j)
                {
                    validPitches[j] = samplesPerSecond / (float)validEpochs[j];
                }
            }

            return validPitches;
        }

        /// <summary>
        /// Get valid epochs for given range of waveform.
        /// </summary>
        /// <param name="sampleOffset">The begin sample offset of the waveform.</param>
        /// <param name="sampleLength">The sample range of the waveform.</param>
        /// <returns>Epochs.</returns>
        public int[] GetValidEpochs(int sampleOffset, int sampleLength)
        {
            DataRange<int> range = GetValidEpochRange(sampleOffset, sampleLength);

            int[] validEpochs = null;
            if (range.Begin != range.End)
            {
                validEpochs = new int[range.End - range.Begin];
                Buffer.BlockCopy(_epoch, range.Begin * 4, validEpochs, 0, validEpochs.Length * 4);
            }

            return validEpochs;
        }

        /// <summary>
        /// Get the valid (continued voiced) epoch range in the epoch data.
        /// </summary>
        /// <param name="sampleOffset">The begin sample offset of the waveform.</param>
        /// <param name="sampleLength">The sample length of the waveform.</param>
        /// <returns>Valid epoch range.</returns>
        public DataRange<int> GetValidEpochRange(int sampleOffset, int sampleLength)
        {
            const int MIN_PRESERVED_EPOCH_LENGTH = 10;
            const int DISCARD_LEFT = 1;
            const int DISCARD_RIGHT = 3;

            // Assert the length of EGG data is same as that of epoch position
            Debug.Assert(_eggData == null || _eggData.Length ==
                Math.Abs(_epochPosition[_epochPosition.Length - 1]));

            int sampleEnd = sampleOffset + sampleLength;
            if (sampleLength == 0)
            {
                sampleEnd = Math.Abs(_epochPosition[_epochPosition.Length - 1]) - 1;
            }

            AdjustAlignment(ref sampleOffset);
            AdjustAlignment(ref sampleEnd);
            Debug.Assert(sampleOffset <= sampleEnd);

            DataRange<int> range = FindEpochRange(sampleOffset, sampleEnd);
            Debug.Assert(range.Begin <= range.End);

            int index = 0;
            for (index = range.Begin; index < range.End && _epoch[index] < 0;)
            {
                ++index;
            }

            range.Begin = index;
            for (; index < range.End && _epoch[index] >= 0;)
            {
                ++index;
            }

            range.End = index;

            if (range.End - range.Begin > MIN_PRESERVED_EPOCH_LENGTH)
            {
                range.Begin += DISCARD_LEFT;
                range.End -= DISCARD_RIGHT;
            }

            Debug.Assert(range.Begin <= range.End);

            return range;
        }

        /// <summary>
        /// Get average epoch.
        /// </summary>
        /// <param name="sampleOffset">The begin sample offset of the waveform.</param>
        /// <param name="sampleLength">The sample length of the waveform.</param>
        /// <returns>Average epoch.</returns>
        public float GetAverageEpoch(int sampleOffset, int sampleLength)
        {
            float averageEpoch = 0.0f;

            int[] validEpochs = GetValidEpochs(sampleOffset, sampleLength);
            if (validEpochs != null && validEpochs.Length > 0)
            {
                float epochSum = 0.0f;
                for (int i = 0; i < validEpochs.Length; i++)
                {
                    epochSum += validEpochs[i];
                }

                averageEpoch = epochSum / validEpochs.Length;
            }

            return averageEpoch;
        }

        #endregion

        #region Private static methods

        /// <summary>
        /// Pick peak samples.
        /// </summary>
        /// <exception cref="System.OverflowException">Thrown when fail to calculate
        /// MinEnvelopAmplitude and minPulseAmplitude for overflow.</exception>
        /// <param name="inputSamples">Input waveform samples.</param>
        /// <param name="peakSamples">Peak samples.</param>
        /// <param name="samplesPerSecond">Samples per second.</param>
        /// <returns>Maximum pulse amplitude.</returns>
        private static int PeakPicking(float[] inputSamples,
            float[] peakSamples, int samplesPerSecond)
        {
            const float ENVELOPE_RATIO = 0.6f;

            // estimate cuttoffs
            float minEnvSampleValue = 1000.0f;
            float minPosSampleValue = 1000.0f;

            // treate the max sample value find in the last 1/40 second as minSampleValue.
            for (int i = inputSamples.Length - (samplesPerSecond / 40);
                i < inputSamples.Length; ++i)
            {
                if (peakSamples[i] > minEnvSampleValue)
                {
                    minEnvSampleValue = peakSamples[i];
                }

                if (inputSamples[i] > minPosSampleValue)
                {
                    minPosSampleValue = inputSamples[i];
                }
            }

            int minEnvelopAmplitude = checked((int)(1.5f * minEnvSampleValue));
            int maxPulseAmplitude = checked((int)(1.5f * minPosSampleValue));

            // use 7000 as max amplitude
            if (maxPulseAmplitude > 7000)
            {
                maxPulseAmplitude = 7000;
            }

            if (minEnvelopAmplitude > 7000)
            {
                minEnvelopAmplitude = 7000;
            }

            // select peak maximum, set rest to 0
            float maxValue = 0.0f;
            int maxIndex = 0;
            for (int i = 0; i < inputSamples.Length; ++i)
            {
                if (inputSamples[i] > 0.0f)
                {
                    // update maximum
                    if (inputSamples[i] > maxValue)
                    {
                        peakSamples[maxIndex] = 0.0f;
                        maxValue = inputSamples[i];
                        maxIndex = i;
                    }
                    else
                    {
                        peakSamples[i] = 0.0f;
                    }
                }
                else
                {
                    peakSamples[i] = 0.0f;
                    if (maxValue > 0.0f)
                    {
                        if (peakSamples[maxIndex] > minEnvelopAmplitude &&
                            inputSamples[maxIndex] > ENVELOPE_RATIO * peakSamples[maxIndex])
                        {
                            peakSamples[maxIndex] = inputSamples[maxIndex];
                        }
                        else
                        {
                            peakSamples[maxIndex] = 0.0f;
                        }

                        maxValue = 0.0f;
                        maxIndex = 0;
                    }
                }
            }

            // avoid peaks at borders
            if (peakSamples[0] > 0)
            {
                peakSamples[0] = 0.0f;
            }

            if (peakSamples[inputSamples.Length - 1] > 0)
            {
                peakSamples[inputSamples.Length - 1] = 0.0f;
            }

            return maxPulseAmplitude;
        }

        /// <summary>
        /// Envelope first detects the positive and negative amplitude
        /// Envelopes. The sign of the waveform is determined by picking
        /// Envelope with highest energy. The sign of the input waveform
        /// Is changed in case it is negative. After this the negative
        /// Values are clipped to zero. FInally, the signal is rescaled
        /// So that it can be stored as a WAV file.
        /// </summary>
        /// <param name="inputSamples">Input waveform samples.</param>
        /// <param name="outputSamples">Output waveform samples.</param>
        /// <param name="minPitchPeriod">Minimum pitch period.</param>
        /// <param name="maxPitchPeriod">Maximum pitch period.</param>
        /// <returns>True if positive waveform, otherwise false.</returns>
        private static bool Envelope(
            float[] inputSamples, float[] outputSamples,
            int minPitchPeriod, int maxPitchPeriod)
        {
            const int MAX_SAMPLE = 32000;

            float[] positive = new float[inputSamples.Length];
            float[] negative = new float[inputSamples.Length];
            float[] posEnvelope = new float[inputSamples.Length];
            float[] negEnvelope = new float[inputSamples.Length];

            // create positive and negative signals
            for (int i = 0; i < inputSamples.Length; ++i)
            {
                if (inputSamples[i] > 0)
                {
                    positive[i] = inputSamples[i];
                    negative[i] = 0.0f;
                }
                else
                {
                    positive[i] = 0.0f;
                    negative[i] = -inputSamples[i];
                }
            }

            // compute amplitude envelope for positive and negative parts
            float posEnergy = PosEnvelope(positive, posEnvelope,
                minPitchPeriod, maxPitchPeriod);

            float negEnergy = PosEnvelope(negative, negEnvelope,
                minPitchPeriod, maxPitchPeriod);

            // select sign by looking at part with highest energy
            if (negEnergy > posEnergy)
            {
                positive = negative;
                posEnvelope = negEnvelope;
            }

            LowPass(posEnvelope, outputSamples, maxPitchPeriod / 2);

            float inputAverage = 0;
            float outputAverage = 0;
            for (int i = 0; i < inputSamples.Length; ++i)
            {
                inputSamples[i] = positive[i];

                if (inputSamples[i] > 0)
                {
                    inputAverage += inputSamples[i];
                }
                else
                {
                    inputSamples[i] = 0;
                }

                if (outputSamples[i] > 0.0f)
                {
                    outputAverage += outputSamples[i];
                }
                else
                {
                    outputSamples[i] = 0;
                }
            }

            inputAverage /= inputSamples.Length;
            outputAverage /= inputSamples.Length;

            // find maximum and normalize to MAX_SAMPLE for display purposes
            float minEnergy, maxEnergy;

            MinMax(inputSamples, out minEnergy, out maxEnergy);
            float gain = MAX_SAMPLE / maxEnergy;
            for (int i = 0; i < inputSamples.Length; ++i)
            {
                inputSamples[i] *= gain;
            }

            MinMax(outputSamples, out minEnergy, out maxEnergy);
            gain = MAX_SAMPLE / maxEnergy;
            for (int i = 0; i < outputSamples.Length; ++i)
            {
                outputSamples[i] *= gain;
            }

            return posEnergy > negEnergy;
        }

        /// <summary>
        /// Envelope estimates the amplitude envelope of the input
        /// Signal. To do this an algorithm similar to a half-wave
        /// Rectification is used where the envelope tracks instant
        /// Increases in amplitude and then the envelope linearly decays
        /// After a small time. This is a fast method to estimate the
        /// Amplitude envelope. This routine will compute the positive
        /// Envelope (the negative pulses do not contribute).
        /// </summary>
        /// <param name="inputSamples">Input waveform samples.</param>
        /// <param name="outputSamples">Output waveform samples.</param>
        /// <param name="minPitchPeriod">Minimum pitch period.</param>
        /// <param name="maxPitchPeriod">Maximum pitch period.</param>
        /// <returns>Average sample energy.</returns>
        private static float PosEnvelope(
            float[] inputSamples, float[] outputSamples,
            int minPitchPeriod, int maxPitchPeriod)
        {
            float maxSampleEnergy;
            outputSamples[0] = maxSampleEnergy = inputSamples[0];

            if (outputSamples[0] < 0)
            {
                outputSamples[0] = 0.0f;
            }

            float beta = 1.0f / maxPitchPeriod;
            float alpha = maxSampleEnergy * beta;
            float sumSamplesEnergy = outputSamples[0];

            int sampleClipStep = 0;
            for (int i = 1; i < inputSamples.Length; ++i)
            {
                // update envelope
                if (sampleClipStep < minPitchPeriod)
                {
                    outputSamples[i] = maxSampleEnergy;
                    ++sampleClipStep;
                }
                else
                {
                    float temp = outputSamples[i - 1] - alpha;
                    outputSamples[i] = temp < 0 ? 0.0f : temp;
                }

                // update envelope if input exceeds it
                if (inputSamples[i] > outputSamples[i])
                {
                    outputSamples[i] = maxSampleEnergy = inputSamples[i];
                    sampleClipStep = 0;

                    alpha = maxSampleEnergy * beta;
                }

                sumSamplesEnergy += outputSamples[i];
            }

            return sumSamplesEnergy / inputSamples.Length;
        }

        /// <summary>
        /// Remove Epochs that are closer than the minimum pitch period.
        /// Set the positive epoch to zero if it following less then minPitchPeriod
        /// Coutt of non positive epoch.
        /// </summary>
        /// <param name="rawEpochData">Raw epoch data.</param>
        /// <param name="minPitchPeriod">Minimum pitch period.</param>
        private static void RemoveCloseEpochs(float[] rawEpochData,
            int minPitchPeriod)
        {
            int pitch = 0;
            for (int i = 0; i < rawEpochData.Length; ++i)
            {
                if (rawEpochData[i] > 0.0f)
                {
                    if (pitch < minPitchPeriod)
                    {
                        rawEpochData[i] = 0.0f;
                    }

                    pitch = 0;
                }

                ++pitch;
            }
        }

        /// <summary>
        /// Compute Epochs from sample signal.
        /// </summary>
        /// <param name="samples">Waveform samples.</param>
        /// <param name="epochs">Epoch data.</param>
        /// <returns>Epoch Number.</returns>
        private static int GetEpochs(float[] samples, float[] epochs)
        {
            // count number of epochs
            int epochNumber = 0;
            int pitch = 0;

            for (int i = 0; i < samples.Length; ++i)
            {
                if (samples[i] > 0.0f)
                {
                    epochs[epochNumber++] = pitch;
                    pitch = 0;
                }

                pitch++;
            }

            epochs[epochNumber++] = pitch;
            return epochNumber;
        }

        /// <summary>
        /// SmoothEpochs adds and removes epochs to complete a smooth
        /// Epoch sequence.
        /// </summary>
        /// <exception cref="System.OverflowException">Thrown when fail to convert
        /// Epoch from Single to int.</exception>
        /// <param name="positiveSamples">Positive waveform samples.</param>
        /// <param name="smoothSamples">Smoothed waveform samples.</param>
        /// <param name="epochs">Epoch data.</param>
        /// <param name="smoothEpochs">Smoothed epoch data.</param>
        /// <param name="epochNumber">Epoch number.</param>
        /// <param name="minPulseAmplitude">Minimum pulse amplitude.</param>
        private static void SmoothEpochs(float[] positiveSamples,
            float[] smoothSamples, float[] epochs, float[] smoothEpochs,
            ref int epochNumber, int minPulseAmplitude)
        {
            const float MAX_PITCH_RATIO = 1.7f;

            epochs[0] = -Math.Abs(epochs[0]);
            bool left = false;

            for (int i = 0; i < epochNumber; ++i)
            {
                if (Math.Abs(epochs[i]) > MAX_PITCH_RATIO * smoothEpochs[i])
                {
                    // determine whether to extend from the left or the right
                    if (i == 0)
                    {
                        left = false;
                    }
                    else if (i == epochNumber - 1)
                    {
                        left = true;
                    }
                    else if (epochs[i - 1] < 0)
                    {
                        left = false;
                    }
                    else
                    {
                        left = true;
                    }

                    bool voiced;
                    int maxIndex = FindEpoch(positiveSamples, epochs, i, left,
                         checked((int)smoothEpochs[i]), minPulseAmplitude, out voiced);

                    epochNumber = InsertEpoch(positiveSamples, smoothSamples,
                        epochs, smoothEpochs, epochNumber, voiced, maxIndex);

                    if (!left)
                    {
                        --i;
                    }
                }
            }
        }

        /// <summary>
        /// Finds at what sample to insert an epoch.
        /// </summary>
        /// <exception cref="System.OverflowException">Thrown when fail to convert
        /// Epoch from Single to int.</exception>
        /// <param name="positiveSamples">Positive waveform samples.</param>
        /// <param name="epochs">Epoch data.</param>
        /// <param name="epochIndex">Epoch index.</param>
        /// <param name="left">Search at the left side of epoch or not.</param>
        /// <param name="pitch">Pitch.</param>
        /// <param name="minPulseAmplitude">Minimum pulse amplitude.</param>
        /// <param name="voiced">Voiced epoch or not.</param>
        /// <returns>Index of maximum pulse.</returns>
        private static int FindEpoch(float[] positiveSamples,
            float[] epochs, int epochIndex, bool left, int pitch,
            int minPulseAmplitude, out bool voiced)
        {
            int baseSample = 0;
            for (int i = 0; i < epochIndex; ++i)
            {
                baseSample += checked((int)Math.Abs(epochs[i]));
            }

            // find new pulse from the left
            int firstPulse = 0;
            if (left)
            {
                firstPulse = baseSample + pitch;
            }
            else
            {
                firstPulse = baseSample + checked((int)Math.Abs(epochs[epochIndex])) - pitch;
            }

            int delta = checked((int)(0.2f * pitch));
            int startSample = firstPulse - delta;
            int stopSample = firstPulse + delta;

            // find maximum value in range
            int maxIndex = startSample;
            float maxEnergy = positiveSamples[startSample];
            for (int j = startSample; j <= stopSample; ++j)
            {
                if (maxEnergy < positiveSamples[j])
                {
                    maxEnergy = positiveSamples[j];
                    maxIndex = j;
                }
            }

            // set epoch time and voiced decision
            if (maxEnergy > minPulseAmplitude)
            {
                voiced = true;
            }
            else
            {
                voiced = false;
                maxIndex = firstPulse;
            }

            Debug.Assert((maxIndex > baseSample) &&
                (maxIndex < baseSample + Math.Abs(epochs[epochIndex])));

            return maxIndex;
        }

        /// <summary>
        /// Inserts an epoch (voiced or unvoiced) at a given sample.
        /// </summary>
        /// <exception cref="System.OverflowException">Thrown when fail to convert
        /// Epoch from Single to int.</exception>
        /// <param name="positiveSamples">Positive waveform samples.</param>
        /// <param name="smoothSamples">Smoothed waveform samples.</param>
        /// <param name="epochs">Epoch data.</param>
        /// <param name="smoothEpochs">Smoothed epoch data.</param>
        /// <param name="epochNumber">Epoch number.</param>
        /// <param name="voiced">Voiced epoch or not.</param>
        /// <param name="sampleIndex">Sample index.</param>
        /// <returns>New epoch number.</returns>
        private static int InsertEpoch(float[] positiveSamples,
            float[] smoothSamples, float[] epochs, float[] smoothEpochs,
            int epochNumber, bool voiced, int sampleIndex)
        {
            // find out what epoch sampleIndex falls into
            int epochPosition;
            int baseSample = 0;
            for (epochPosition = 0; epochPosition < epochNumber &&
                baseSample < sampleIndex; ++epochPosition)
            {
                baseSample += checked((int)Math.Abs(epochs[epochPosition]));
            }

            epochPosition--;
            baseSample -= checked((int)Math.Abs(epochs[epochPosition]));

            // shift all epochs by 1
            for (int j = epochNumber; j > epochPosition; --j)
            {
                epochs[j] = epochs[j - 1];
                smoothEpochs[j] = smoothEpochs[j - 1];
            }

            ++epochNumber;

            // now we have to modify epochs i and i+1 accordingly
            int epochValue = sampleIndex - baseSample;
            int i1 = baseSample + checked((int)Math.Abs((int)epochs[epochPosition])) - sampleIndex;

            if (voiced)
            {
                epochs[epochPosition + 1] = i1;
                smoothSamples[sampleIndex] = positiveSamples[sampleIndex];
            }
            else
            {
                epochs[epochPosition + 1] = -i1;
                smoothSamples[sampleIndex] = -1000.0f;
            }

            epochs[epochPosition] = epochs[epochPosition] > 0 ? epochValue : -epochValue;

            return epochNumber;
        }

        #endregion

        #region DSP Methods

        /// <summary>
        /// MinMax computes tha maximum and minimum values of an input array.
        /// </summary>
        /// <param name="inputSamples">Input samples.</param>
        /// <param name="minValue">Minimum value.</param>
        /// <param name="maxValue">Maximum value.</param>
        private static void MinMax(float[] inputSamples,
            out float minValue, out float maxValue)
        {
            Debug.Assert(inputSamples.Length > 0);

            minValue = inputSamples[0];
            maxValue = inputSamples[0];

            for (int i = 1; i < inputSamples.Length; ++i)
            {
                if (inputSamples[i] > maxValue)
                {
                    maxValue = inputSamples[i];
                }
                else if (inputSamples[i] < minValue)
                {
                    minValue = inputSamples[i];
                }
            }
        }

        /// <summary>
        /// The Preemphasis routine applies preemphasis on the input
        /// Data. Essentially this is a high pass filter that is very
        /// Efficient and simple.
        /// </summary>
        /// <param name="inputSamples">Input samples.</param>
        /// <param name="outputSamples">Output samples.</param>
        private static void Preemphasis(float[] inputSamples, float[] outputSamples)
        {
            const float Alpha = 0.999f;

            Debug.Assert(inputSamples.Length == outputSamples.Length);
            for (int i = 0; i < outputSamples.Length - 1; ++i)
            {
                outputSamples[i] = inputSamples[i + 1] - (Alpha * inputSamples[i]);
            }

            outputSamples[outputSamples.Length - 1] = 0.0f;
        }

        /// <summary>
        /// LowPass filter using a rectangular window. It is very fast
        /// Though not very sharp. In general, probably good enough.
        /// </summary>
        /// <param name="inputEpochs">Input epoch data.</param>
        /// <param name="outputEpochs">Output epoch data.</param>
        /// <param name="windowSize">Window size.</param>
        private static void LowPass(float[] inputEpochs, float[] outputEpochs,
            int windowSize)
        {
            Debug.Assert(inputEpochs.Length == outputEpochs.Length);

            float gain = 1.0f / ((2 * windowSize) + 1);

            float sum = 0.0f;
            for (int i = 0; i <= 2 * windowSize; ++i)
            {
                sum += inputEpochs[i];
            }

            float average = sum * gain;

            int epochPosition;
            for (epochPosition = 0; epochPosition <= windowSize;
                ++epochPosition)
            {
                outputEpochs[epochPosition] = average;
            }

            for (; epochPosition < inputEpochs.Length - windowSize;
                ++epochPosition)
            {
                sum += inputEpochs[epochPosition + windowSize]
                    - inputEpochs[epochPosition - windowSize - 1];
                average = sum * gain;
                outputEpochs[epochPosition] = average;
            }

            for (; epochPosition < inputEpochs.Length; ++epochPosition)
            {
                outputEpochs[epochPosition] = average;
            }
        }

        /// <summary>
        /// A low-pass filter is a filter that passes low frequencies well,
        /// But attenuates (or reduces) frequencies higher than the cutoff frequency.
        /// </summary>
        /// <param name="inputSamples">Input waveform samples.</param>
        /// <param name="outputSamples">Output waveform samples.</param>
        private static void LowPass2(float[] inputSamples,
            float[] outputSamples)
        {
            Debug.Assert(inputSamples.Length == outputSamples.Length);

            // LowPass coefficients
            float[] filter = new float[21]
                {
                    0.003283f,
                    0.005198f,
                    0.010104f,
                    0.018676f,
                    0.030909f,
                    0.046028f,
                    0.062561f,
                    0.078558f,
                    0.091925f,
                    0.100803f,
                    0.103914f,
                    0.100803f,
                    0.091925f,
                    0.078558f,
                    0.062561f,
                    0.046028f,
                    0.030909f,
                    0.018676f,
                    0.010104f,
                    0.005198f,
                    0.003283f
                };

            Array.Clear(outputSamples, 0, outputSamples.Length);

            for (int i = 10; i < outputSamples.Length - 10; ++i)
            {
                for (int j = 0; j < 21; ++j)
                {
                    outputSamples[i] += inputSamples[i + j - 10] * filter[j];
                }
            }
        }

        /// <summary>
        /// Smooth the epoch sequence.
        /// </summary>
        /// <param name="inputEpochs">Input epoch data.</param>
        /// <param name="outputEpochs">Ouput epoch data.</param>
        /// <param name="epochNumber">Epoch number.</param>
        /// <param name="order">Order of smoothing.</param>
        /// <param name="minValue">Minimum value.</param>
        /// <param name="maxValue">Maximum value.</param>
        private static void MedianSmoothing(float[] inputEpochs, float[] outputEpochs,
            int epochNumber, int order, int minValue, int maxValue)
        {
            Debug.Assert(order % 2 > 0);

            float[] bufferEpochs = new float[order];

            for (int i = order / 2; i < epochNumber - (order / 2); i++)
            {
                for (int j = 0; j < order; j++)
                {
                    bufferEpochs[j] = inputEpochs[i - (order / 2) + j];
                }

                outputEpochs[i] = Middle(bufferEpochs, order, minValue, maxValue);
            }

            // deal with the first half window that haven't smoothed before
            for (int i = 0; i < order / 2; i++)
            {
                outputEpochs[i] = outputEpochs[order / 2];
            }

            // deal with the last half window that haven't smoothed before
            for (int i = epochNumber - (order / 2); i < epochNumber; i++)
            {
                outputEpochs[i] = outputEpochs[epochNumber - 1 - (order / 2)];
            }
        }

        /// <summary>
        /// Calculate the property epoch value that make epoch array more smooth.
        /// </summary>
        /// <param name="bufferEpochs">Epoch data.</param>
        /// <param name="order">Order of smoothing.</param>
        /// <param name="minValue">Minimum value.</param>
        /// <param name="maxValue">Maximum value.</param>
        /// <returns>New epoch value.</returns>
        private static float Middle(float[] bufferEpochs, int order,
            int minValue, int maxValue)
        {
            int minIndex, maxIndex;

            Array.Sort<float>(bufferEpochs);

            // find the first epoch that bigger than minValue
            for (minIndex = 0; minIndex < order; ++minIndex)
            {
                if (bufferEpochs[minIndex] > minValue)
                {
                    break;
                }
            }

            // find the last epoch that less than maxValue
            for (maxIndex = order - 1; maxIndex >= 0; --maxIndex)
            {
                if (bufferEpochs[maxIndex] < maxValue)
                {
                    break;
                }
            }

            if (maxIndex >= minIndex)
            {
                return bufferEpochs[(maxIndex + minIndex) / 2];
            }
            else
            {
                return ((float)(maxValue + minValue)) / 2;
            }
        }

        #endregion

        #region Pirvate static method used for egg2epoch

        /// <summary>
        /// Adjust epoch, response with AdjustFreq tool.
        /// </summary>
        /// <exception cref="System.OverflowException">Thrown when fail to convert
        /// Epoch from Single to int.</exception>
        /// <param name="waveSamples">EGG waveform sampples.</param>
        /// <param name="epochs">Epoch data.</param>
        /// <param name="positive">Positive waveform.</param>
        /// <param name="samplesPerSecond">Samples per second.</param>
        /// <param name="frameSize">Frame size.</param>
        /// <param name="lpcOrder">Order og LPC.</param>
        /// <param name="offsetBenchmark">Offset bench mark.</param>
        /// <returns>New epoch data.</returns>
        private static int[] AdjustFreq(short[] waveSamples, float[] epochs,
            bool positive, int samplesPerSecond, int frameSize,
            int lpcOrder, int offsetBenchmark)
        {
            float[] window = LpcAnalyzer.CreateHammingWindow(frameSize);

            // remove DC component
            float[] tempWaveSamples = LpcAnalyzer.RemoveDirectCurrent(waveSamples);
            if (!positive)
            {
                // invert if negative polarity epochs
                LpcAnalyzer.Invert(tempWaveSamples);
            }

            int epochStart = 0;
            int epochEnd = 0;

            // determine cutting points
            LpcAnalyzer.AdjustStartStop(epochs, ref epochStart, ref epochEnd);

            // computes LPC parameters for specified region
            int sampleOffset = 0;
            for (int i = 0; i < epochStart; i++)
            {
                sampleOffset += checked((int)Math.Abs(epochs[i]));
            }

            int sampleNumber = 0;
            for (int i = epochStart; i < epochEnd; i++)
            {
                sampleNumber += checked((int)Math.Abs(epochs[i]));
            }

            int epochNumber = epochEnd - epochStart;

            float[] lpcData = new float[epochNumber * (1 + lpcOrder)];
            float[] residualData = new float[sampleNumber];

            LpcAnalyzer.Analyze(epochs, epochStart, epochNumber,
                window, frameSize,
                tempWaveSamples, sampleNumber, samplesPerSecond,
                lpcData, lpcOrder);

            // compute residual signal for desired region
            int currSampleOffset = sampleOffset;
            int residualSampleOffset = 0;
            int lpcFrameOffset = 0;

            for (int i = epochStart; i < epochEnd; i++)
            {
                LpcAnalyzer.InverseLpcFilter(tempWaveSamples, currSampleOffset,
                     checked((int)Math.Abs(epochs[i])), lpcData, lpcFrameOffset,
                     lpcOrder, residualData, residualSampleOffset);

                currSampleOffset += checked((int)Math.Abs(epochs[i]));
                residualSampleOffset += checked((int)Math.Abs(epochs[i]));

                lpcFrameOffset += lpcOrder + 1;
            }

            int[] epochOffsets = new int[epochNumber];
            int epochOffsetNumber = 0;
            int sampleStart = checked((int)Math.Abs(epochs[epochStart]));
            for (int i = 1; i < epochNumber; i++)
            {
                if (epochs[epochStart + i] < 0)
                {
                    sampleStart += checked((int)Math.Abs(epochs[epochStart + i]));
                    continue;
                }

                float maxResidualValue = 0;
                int maxResidualIndex = 0;

                for (int j = sampleStart - checked((int)(Math.Abs(epochs[epochStart + i - 1]) / 2));
                    j < sampleStart + (Math.Abs(epochs[epochStart + i]) / 2); j++)
                {
                    if (Math.Abs(residualData[j]) > maxResidualValue)
                    {
                        maxResidualValue = Math.Abs(residualData[j]);
                        maxResidualIndex = j;
                    }
                }

                epochOffsets[epochOffsetNumber++] = maxResidualIndex - sampleStart;

                sampleStart += checked((int)Math.Abs(epochs[epochStart + i]));
            }

            Array.Sort<int>(epochOffsets, 0, epochOffsetNumber);
            return HandleCutPoint(epochOffsets, epochOffsetNumber,
                epochs, epochStart, epochNumber, offsetBenchmark);
        }

        /// <summary>
        /// We could handle even/odd, but really it should be very stationary,
        /// And we're only doing integer offsets anyway, so it doesn't matter
        /// Then, insert a sample delay, so that the cut-point does not occur
        /// At the maximum energy point.median below iOffset = rgiOffset[i/2]
        /// - cOffset; even more robust : find range of 3 (now 5) that is a mode,
        /// Then find the mode within this. I.E. more offsets are 25, 26, or 27
        /// Than any other three consecutive offsets. Then choose the one of 25,
        /// 26, or 27 that has the most.  This will prevent a single frequent
        /// Number from being the offset (sometimes happens for certain vowels
        /// And nasals, I think).
        /// </summary>
        /// <param name="epochOffsets">Epoch offet array.</param>
        /// <param name="epochOffsetNumber">Number of epoch offset.</param>
        /// <param name="epochs">Epoch data.</param>
        /// <param name="epochOffset">Epoch offset.</param>
        /// <param name="epochNumber">Epoch number.</param>
        /// <param name="offsetBenchmark">Offset benchmark.</param>
        /// <returns>New epoch data.</returns>
        private static int[] HandleCutPoint(int[] epochOffsets,
            int epochOffsetNumber, float[] epochs, int epochOffset,
            int epochNumber, int offsetBenchmark)
        {
            int currSampleOffset = epochOffsets[0];
            int sampleOffsetWithMaxInterval = currSampleOffset;
            int maxOffsetsLength = 1;
            int maxOffsetsIndex = 0;
            int beginOffsetsIndex = 0;
            int offsetsIndex = 1;

            for (; offsetsIndex < epochOffsetNumber; offsetsIndex++)
            {
                if (epochOffsets[offsetsIndex] > currSampleOffset + 5)
                {
                    // find range of samples more that 5, then it may be a mode
                    // then find the mode within this
                    if (offsetsIndex - beginOffsetsIndex > maxOffsetsLength)
                    {
                        maxOffsetsLength = offsetsIndex - beginOffsetsIndex;
                        sampleOffsetWithMaxInterval = currSampleOffset;
                        maxOffsetsIndex = beginOffsetsIndex;
                    }

                    // skip zero epoch points, i.e. the sample count of that epoch is 0
                    for (offsetsIndex = beginOffsetsIndex;
                        offsetsIndex < epochOffsetNumber; offsetsIndex++)
                    {
                        if (epochOffsets[offsetsIndex] != currSampleOffset)
                        {
                            break;
                        }
                    }

                    beginOffsetsIndex = offsetsIndex;
                    currSampleOffset = epochOffsets[offsetsIndex];
                }
            }

            if (offsetsIndex - beginOffsetsIndex > maxOffsetsLength)
            {
                maxOffsetsLength = offsetsIndex - beginOffsetsIndex;
                sampleOffsetWithMaxInterval = currSampleOffset;
                maxOffsetsIndex = beginOffsetsIndex;
            }

            // sampleOffsetWithMaxInterval is now the first of three
            // possibilities, and starts at beginOffsetsIndex. find
            // which three it is.
            offsetsIndex = maxOffsetsIndex;
            maxOffsetsIndex = sampleOffsetWithMaxInterval;
            maxOffsetsLength = 0;
            currSampleOffset = sampleOffsetWithMaxInterval;
            beginOffsetsIndex = offsetsIndex;

            for (offsetsIndex++; (offsetsIndex < epochOffsetNumber) &&
                (epochOffsets[offsetsIndex] < maxOffsetsIndex + 5);
                offsetsIndex++)
            {
                if (epochOffsets[offsetsIndex] != currSampleOffset)
                {
                    if (offsetsIndex - beginOffsetsIndex > maxOffsetsLength)
                    {
                        maxOffsetsLength = offsetsIndex - beginOffsetsIndex;
                        sampleOffsetWithMaxInterval = currSampleOffset;
                    }

                    beginOffsetsIndex = offsetsIndex;
                    currSampleOffset = epochOffsets[offsetsIndex];
                }
            }

            if (offsetsIndex - beginOffsetsIndex > maxOffsetsLength)
            {
                maxOffsetsLength = offsetsIndex - beginOffsetsIndex;
                sampleOffsetWithMaxInterval = currSampleOffset;
            }

            // get the offsetSize used for adjust epochs
            int offsetSize = sampleOffsetWithMaxInterval - offsetBenchmark;

            // Should make sure keep the same sign when moving.
            maxOffsetsLength = checked((int)Math.Min(Math.Abs(epochs[0]),
                Math.Abs(epochs[epochNumber - 1 + epochOffset])));
            if (maxOffsetsLength > 0)
            {
                maxOffsetsIndex--;
            }

            if (Math.Abs(offsetSize) > maxOffsetsLength)
            {
                offsetSize = Math.Sign(offsetSize) * maxOffsetsLength;
            }

            if (epochs[0] < 0)
            {
                epochs[0] -= offsetSize;
            }
            else
            {
                epochs[0] += offsetSize;
            }

            // correct last frame the opposite way so we still have the
            // proper number of samples
            if (epochs[epochNumber - 1 + epochOffset] < 0)
            {
                epochs[epochNumber - 1 + epochOffset] += offsetSize;
            }
            else
            {
                epochs[epochNumber - 1 + epochOffset] -= offsetSize;
            }

            int[] outEpochs = ArrayHelper.ToInt32<float>(epochs);

            return outEpochs;
        }

        /// <summary>
        /// Get final epoch , response with read_freq tool.
        /// </summary>
        /// <exception cref="System.ArgumentNullException">Thrown when
        /// The epochs parameter is null or doesn't contain any elements.</exception>
        /// <param name="epochs">Epoch data to flip.</param>
        private static void FlipWobbleEpoch(int[] epochs)
        {
            const int EpochShortestNegative = 4;

            if (epochs == null || epochs.Length == 0)
            {
                throw new ArgumentNullException("epochs");
            }

            int flag = 0;
            for (int i = 0; i < epochs.Length; i++)
            {
                if (epochs[i] < 0)
                {
                    flag++;
                }
                else
                {
                    if (flag > 0 && flag < EpochShortestNegative)
                    {
                        for (int t = 0; t < flag; t++)
                        {
                            epochs[i - t - 1] = -epochs[i - t - 1];
                        }
                    }

                    flag = 0;
                }
            }

            for (int i = 1; i < epochs.Length - 1; i++)
            {
                if (epochs[i] > 0 &&
                    epochs[i - 1] < 0 &&
                    epochs[i + 1] < 0)
                {
                    epochs[i] = -epochs[i];
                }
            }
        }

        #endregion

        #region Private instance methods

        /// <summary>
        /// Find the epochs range for a given samples range.
        /// </summary>
        /// <param name="sampleOffset">The beginning offset of the samples range.</param>
        /// <param name="sampleEnd">The end offset of the samples range.</param>
        /// <returns>Epoch range with begin and end offset of the found epochs range.</returns>
        private DataRange<int> FindEpochRange(int sampleOffset, int sampleEnd)
        {
            DataRange<int> range = new DataRange<int>(-1, 0);
            int epochLength = 0;

            for (int index = 0; index < _epochPosition.Length; index++)
            {
                if (Math.Abs(_epochPosition[index]) > sampleOffset)
                {
                    if (range.Begin == -1)
                    {
                        range.Begin = index;
                    }

                    if (Math.Abs(_epochPosition[index]) <= sampleEnd)
                    {
                        epochLength++;
                    }
                }
            }

            range.End = range.Begin + epochLength;

            return range;
        }

        #endregion
    }
}