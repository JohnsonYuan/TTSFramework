//----------------------------------------------------------------------------
// <copyright file="HtkFile.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements class to operate HTK file.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Htk
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Htk file operator.
    /// </summary>
    public class HtkFile
    {
        /// <summary>
        /// HTK file body offset.
        /// </summary>
        public const int HtkBodyOffset = 12;

        /// <summary>
        /// Initializes a new instance of the <see cref="HtkFile"/> class.
        /// </summary>
        public HtkFile()
        {
            Frames = new Dictionary<int, byte[]>();
        }

        /// <summary>
        /// Gets or sets Frame number of this HTK file.
        /// </summary>
        public int SampleCount { get; set; }

        /// <summary>
        /// Gets or sets the frame duration, in 100ns units.
        /// </summary>
        public int SampleDurationIn100ns { get; set; }

        /// <summary>
        /// Gets or sets How many bytes per frame, frame definition:.
        /// </summary>
        public int BytesPerSample { get; set; }

        /// <summary>
        /// Gets or sets Parameter kind of this HTK file.
        /// </summary>
        public short ParmKind { get; set; }

        /// <summary>
        /// Gets Loaded frames.
        /// </summary>
        public Dictionary<int, byte[]> Frames { get; private set; }

        /// <summary>
        /// Load frames from HTK file.
        /// </summary>
        /// <param name="htkFilePath">HTK file path.</param>
        /// <param name="frameIndexes">Frames index to be loaded.</param>
        /// <returns>Read HtkFile.</returns>
        public static HtkFile Read(string htkFilePath, IEnumerable<int> frameIndexes)
        {
            Helper.ThrowIfFileNotExist(htkFilePath);
            Helper.ThrowIfNull(frameIndexes);

            HtkFile htkFile = new HtkFile();
            FileStream fs = new FileStream(htkFilePath, FileMode.Open, FileAccess.Read);
            try
            {
                using (BinaryReader reader = new BinaryReader(fs))
                {
                    fs = null;
                    htkFile.LoadHeader(reader);
                    foreach (int frameIndex in frameIndexes.Distinct())
                    {
                        reader.BaseStream.Seek(HtkBodyOffset + (frameIndex * htkFile.BytesPerSample),
                            SeekOrigin.Begin);

                        byte[] buffer = reader.ReadBytes(htkFile.BytesPerSample);

                        if (buffer.Length != htkFile.BytesPerSample)
                        {
                            throw new InvalidDataException(Helper.NeutralFormat(
                                "Invalid frame length [{0}], expected [{1}]",
                                buffer.Length, htkFile.BytesPerSample));
                        }

                        htkFile.Frames[frameIndex] = buffer;
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

            return htkFile;
        }

        /// <summary>
        /// Load frames during specified duration.
        /// </summary>
        /// <param name="htkFilePath">HTK file path.</param>
        /// <returns>Read HtkFile.</returns>
        public static HtkFile Read(string htkFilePath)
        {
            Helper.ThrowIfFileNotExist(htkFilePath);

            HtkFile htkFile = new HtkFile();
            FileStream fs = new FileStream(htkFilePath, FileMode.Open, FileAccess.Read);
            try
            {
                using (BinaryReader reader = new BinaryReader(fs))
                {
                    fs = null;
                    htkFile.LoadHeader(reader);
                    int htkBodyLength = htkFile.SampleCount * htkFile.BytesPerSample;
                    byte[] bodyBuffer = reader.ReadBytes(htkBodyLength);
                    if (bodyBuffer.Length != htkBodyLength)
                    {
                        throw new InvalidDataException(Helper.NeutralFormat(
                            "Invalid frame length [{0}], expected [{1}]",
                            bodyBuffer.Length, htkFile.BytesPerSample));
                    }

                    for (int i = 0; i < htkFile.SampleCount; i++)
                    {
                        byte[] frameBuffer = new byte[htkFile.BytesPerSample];
                        Buffer.BlockCopy(bodyBuffer, htkFile.BytesPerSample * i, frameBuffer, 0, htkFile.BytesPerSample);
                        htkFile.Frames[i] = frameBuffer;
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

            return htkFile;
        }

        /// <summary>
        /// Load HTK file header.
        /// </summary>
        /// <param name="reader">HTK file BinaryReader.</param>
        private void LoadHeader(BinaryReader reader)
        {
            SampleCount = reader.ReadInt32();
            SampleDurationIn100ns = reader.ReadInt32();
            BytesPerSample = reader.ReadInt16();
            ParmKind = reader.ReadInt16();
        }
    }

    /// <summary>
    /// TTS HTK file.
    /// </summary>
    public class TtsHtkFile
    {
        /// <summary>
        /// TTS 3 streams : state, delta, acc.
        /// </summary>
        public const int TtsStreamCount = 3;

        /// <summary>
        /// Initializes a new instance of the <see cref="TtsHtkFile"/> class.
        /// </summary>
        /// <param name="htkFile">Common HTK file class.</param>
        public TtsHtkFile(HtkFile htkFile)
        {
            Helper.ThrowIfNull(htkFile);
            TtsHtkFrames = new Dictionary<int, TtsHtkFrame>();

            LspOrder = (htkFile.BytesPerSample / (TtsStreamCount * sizeof(float))) - 2; // gain and f0 takes 1 dimension respectively
            MbeOrder = 0;
            IsPowerUsed = false;
            GuidanceLspOrder = 0;

            foreach (int frameIndex in htkFile.Frames.Keys)
            {
                TtsHtkFrame ttsHtkFrame = new TtsHtkFrame(LspOrder)
                {
                    FrameIndex = frameIndex,
                };

                ttsHtkFrame.LoadFrame(htkFile.Frames[frameIndex], 0, IsPowerUsed);
                TtsHtkFrames[frameIndex] = ttsHtkFrame;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TtsHtkFile"/> class.
        /// </summary>
        /// <param name="htkFile">Common HTK file class.</param>
        /// <param name="isMbeUsed">Whether multi-band excitation is used.</param>
        /// <param name="mbeOrder">Multi-band excitation order.</param>
        /// <param name="isPowerUsed">Whether power is used.</param>
        /// <param name="isGuidanceLspUsed">Whether guidanceLsp is used.</param>
        /// <param name="guidanceLspOrder">GuidanceLsp order.</param>
        public TtsHtkFile(HtkFile htkFile, bool isMbeUsed, int mbeOrder, bool isPowerUsed, bool isGuidanceLspUsed, int guidanceLspOrder)
        {
            Helper.ThrowIfNull(htkFile);
            TtsHtkFrames = new Dictionary<int, TtsHtkFrame>();

            if (isMbeUsed)
            {
                MbeOrder = mbeOrder;
            }
            else
            {
                MbeOrder = 0;
            }

            if (isGuidanceLspUsed)
            {
                GuidanceLspOrder = guidanceLspOrder;
            }
            else
            {
                GuidanceLspOrder = 0;
            }

            IsPowerUsed = isPowerUsed;

            // LspOrder = Total order - Gain(1) - F0(1) - MbeOrder - guidanceLspOrder - power(1)
            LspOrder = (htkFile.BytesPerSample / (TtsStreamCount * sizeof(float))) - 2 - MbeOrder - GuidanceLspOrder;
            if (IsPowerUsed)
            {
                LspOrder--;
            }

            foreach (int frameIndex in htkFile.Frames.Keys)
            {
                TtsHtkFrame ttsHtkFrame = new TtsHtkFrame(LspOrder, MbeOrder)
                {
                    FrameIndex = frameIndex,
                };

                ttsHtkFrame.LoadFrame(htkFile.Frames[frameIndex], 0, IsPowerUsed);
                TtsHtkFrames[frameIndex] = ttsHtkFrame;
            }
        }

        /// <summary>
        /// Gets or sets Lsp order.
        /// </summary>
        public int LspOrder { get; set; }

        /// <summary>
        /// Gets or sets Multi-band excitation order.
        /// </summary>
        public int MbeOrder { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Power flag.
        /// </summary>
        public bool IsPowerUsed { get; set; }

        /// <summary>
        /// Gets or sets GuidanceLsp order.
        /// </summary>
        public int GuidanceLspOrder { get; set; }

        /// <summary>
        /// Gets Tts HTK frames.
        /// </summary>
        public Dictionary<int, TtsHtkFrame> TtsHtkFrames { get; private set; }
    }

    /// <summary>
    /// TTS HTK frame.
    /// </summary>
    public class TtsHtkFrame
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TtsHtkFrame"/> class.
        /// </summary>
        /// <param name="lspOrder">LspOrder.</param>
        public TtsHtkFrame(int lspOrder)
        {
            Static = new HtkFrameStream(lspOrder, 0, 0);
            Delta = new HtkFrameStream(lspOrder, 0, 0);
            Acc = new HtkFrameStream(lspOrder, 0, 0);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TtsHtkFrame"/> class.
        /// </summary>
        /// <param name="lspOrder">LspOrder.</param>
        /// <param name="mbeOrder">MbeOrder.</param>
        public TtsHtkFrame(int lspOrder, int mbeOrder)
        {
            Static = new HtkFrameStream(lspOrder, mbeOrder, 0);
            Delta = new HtkFrameStream(lspOrder, mbeOrder, 0);
            Acc = new HtkFrameStream(lspOrder, mbeOrder, 0);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TtsHtkFrame"/> class.
        /// </summary>
        /// <param name="lspOrder">LspOrder.</param>
        /// <param name="mbeOrder">MbeOrder.</param>
        /// <param name="guidanceLspOrder">GuidanceLspOrder.</param>
        public TtsHtkFrame(int lspOrder, int mbeOrder, int guidanceLspOrder)
        {
            Static = new HtkFrameStream(lspOrder, mbeOrder, guidanceLspOrder);
            Delta = new HtkFrameStream(lspOrder, mbeOrder, guidanceLspOrder);
            Acc = new HtkFrameStream(lspOrder, mbeOrder, guidanceLspOrder);
        }

        /// <summary>
        /// Gets or sets Frame index of this frame.
        /// </summary>
        public int FrameIndex { get; set; }

        /// <summary>
        /// Gets Static stream.
        /// </summary>
        public HtkFrameStream Static { get; private set; }

        /// <summary>
        /// Gets Delta parameter.
        /// </summary>
        public HtkFrameStream Delta { get; private set; }

        /// <summary>
        /// Gets Acc parameter.
        /// </summary>
        public HtkFrameStream Acc { get; private set; }

        /// <summary>
        /// Load frame from byte buffer, here is the HTK frame structure:
        ///     (lsp + gain) + delta(lsp + gain) + acc(lsp + gain) + f0 + deltaf0 + accF0 + mbe(mbeOrder)
        ///     + deltambe(mbeOrder) + accmbe(mbeOrder) + [power + deltapower + accpowerer] + [guidanceLsp + deltaguidanceLsp + accguidanceLsp]..
        /// </summary>
        /// <param name="buffer">Frame buffer.</param>
        /// <param name="offset">Frame offset.</param>
        /// <param name="isPowerUsed">Power flag.</param>
        /// <returns>Frame bytes.</returns>
        public int LoadFrame(byte[] buffer, int offset, bool isPowerUsed)
        {
            // Load static lsp and gain
            offset += Static.LoadLsp(buffer, offset);
            Static.Gain = BitConverter.ToSingle(buffer, offset);
            offset += HtkFrameStream.ParameterVariableLength;

            // Load delta lsp and gain
            offset += Delta.LoadLsp(buffer, offset);
            Delta.Gain = BitConverter.ToSingle(buffer, offset);
            offset += HtkFrameStream.ParameterVariableLength;

            // Load acc lsp and gain
            offset += Acc.LoadLsp(buffer, offset);
            Acc.Gain = BitConverter.ToSingle(buffer, offset);
            offset += HtkFrameStream.ParameterVariableLength;

            // Load Static f0
            Static.F0 = BitConverter.ToSingle(buffer, offset);
            offset += HtkFrameStream.ParameterVariableLength;

            // Load delta f0
            Delta.F0 = BitConverter.ToSingle(buffer, offset);
            offset += HtkFrameStream.ParameterVariableLength;

            // Load acc f0
            Acc.F0 = BitConverter.ToSingle(buffer, offset);
            offset += HtkFrameStream.ParameterVariableLength;

            if (Static.Mbe != null)
            {
                // Load static mbe
                offset += Static.LoadMbe(buffer, offset);

                // Load delta mbe
                offset += Delta.LoadMbe(buffer, offset);

                // Load acc mbe
                offset += Acc.LoadMbe(buffer, offset);
            }

            if (isPowerUsed)
            {
                // Load Static f0
                Static.Power = BitConverter.ToSingle(buffer, offset);
                offset += HtkFrameStream.ParameterVariableLength;

                // Load delta f0
                Delta.Power = BitConverter.ToSingle(buffer, offset);
                offset += HtkFrameStream.ParameterVariableLength;

                // Load acc f0
                Acc.Power = BitConverter.ToSingle(buffer, offset);
                offset += HtkFrameStream.ParameterVariableLength;
            }

            if (Static.GuidanceLsp != null)
            {
                // Load static lsp
                offset += Static.LoadGuidanceLsp(buffer, offset);

                // Load delta lsp
                offset += Delta.LoadGuidanceLsp(buffer, offset);

                // Load acc lsp
                offset += Acc.LoadGuidanceLsp(buffer, offset);
            }

            return offset;
        }
    }

    /// <summary>
    /// HTK frame parameter.
    /// </summary>
    public class HtkFrameStream
    {
        /// <summary>
        /// Parameter variable length.
        /// </summary>
        public const int ParameterVariableLength = sizeof(float);

        private float _f0;

        /// <summary>
        /// Initializes a new instance of the <see cref="HtkFrameStream"/> class.
        /// </summary>
        public HtkFrameStream()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HtkFrameStream"/> class.
        /// </summary>
        /// <param name="lspOrder">Lsp order.</param>
        /// <param name="mbeOrder">Mbe order.</param>
        /// <param name="guidanceLspOrder">GuidanceLsp order.</param>
        public HtkFrameStream(int lspOrder, int mbeOrder, int guidanceLspOrder)
        {
            if (lspOrder <= 0)
            {
                throw new ArgumentException("LSP order should not less than 1");
            }

            Lsp = new float[lspOrder];
            if (mbeOrder > 0)
            {
                Mbe = new float[mbeOrder];
            }

            if (guidanceLspOrder > 0)
            {
                GuidanceLsp = new float[guidanceLspOrder];
            }
        }

        /// <summary>
        /// Gets or sets F0 value of the frame.
        /// </summary>
        public float F0
        {
            get
            {
                return _f0;
            }

            set
            {
                _f0 = Math.Max(value, 0);
            }
        }

        /// <summary>
        /// Gets or sets Gain value of the frame.
        /// </summary>
        public float Gain { get; set; }

        /// <summary>
        /// Gets or sets LSP value of the frame.
        /// </summary>
        public float[] Lsp { get; set; }

        /// <summary>
        /// Gets or sets Mbe value of the frame.
        /// </summary>
        public float[] Mbe { get; set; }

        /// <summary>
        /// Gets or sets Power value of the frame.
        /// </summary>
        public float Power { get; set; }

        /// <summary>
        /// Gets or sets CodecLSP value of the frame.
        /// </summary>
        public float[] GuidanceLsp { get; set; }

        /// <summary>
        /// Load lsp data.
        /// </summary>
        /// <param name="buffer">Frame buffer.</param>
        /// <param name="offset">Frame parameter offset.</param>
        /// <returns>Frame paramter length.</returns>
        public int LoadLsp(byte[] buffer, int offset)
        {
            Debug.Assert(Lsp != null);
            int length = 0;
            for (int i = 0; i < Lsp.Length; i++)
            {
                Lsp[i] = BitConverter.ToSingle(buffer, offset + length);
                length += ParameterVariableLength;
            }

            return length;
        }

        /// <summary>
        /// Load mbe data.
        /// </summary>
        /// <param name="buffer">Frame buffer.</param>
        /// <param name="offset">Frame parameter offset.</param>
        /// <returns>Frame paramter length.</returns>
        public int LoadMbe(byte[] buffer, int offset)
        {
            Debug.Assert(Mbe != null);
            int length = 0;
            for (int i = 0; i < Mbe.Length; i++)
            {
                Mbe[i] = BitConverter.ToSingle(buffer, offset + length);
                length += ParameterVariableLength;
            }

            return length;
        }

        /// <summary>
        /// Load GuidanceLsp data.
        /// </summary>
        /// <param name="buffer">Frame buffer.</param>
        /// <param name="offset">Frame parameter offset.</param>
        /// <returns>Frame paramter length.</returns>
        public int LoadGuidanceLsp(byte[] buffer, int offset)
        {
            Debug.Assert(GuidanceLsp != null);
            int length = 0;
            for (int i = 0; i < GuidanceLsp.Length; i++)
            {
                GuidanceLsp[i] = BitConverter.ToSingle(buffer, offset + length);
                length += ParameterVariableLength;
            }

            return length;
        }
    }

    /// <summary>
    /// Objective model to load guidanceLsp file, which only contain Lsp, without Gain.
    /// </summary>
    public class GuidanceLspFile
    {
        /// <summary>
        /// Load the GuidanceLsp file on difference dimensions for each frame for a sentence from a file.
        /// </summary>
        /// <typeparam name="T">Data type.</typeparam>
        /// <param name="lspFile">The file containing guidanceLsp info for a sentence.</param>
        /// <param name="lspOrder">The guidance Lsp order.</param>
        /// <returns>A list of lsp, on each dimension of each frame.</returns>
        public static List<List<T>> LoadLsp<T>(string lspFile, int lspOrder)
        {
            return LspFile.LoadData<T>(lspFile, lspOrder - 1, 0, lspOrder);
        }
    }

    /// <summary>
    /// Objective model to load lsp file.
    /// </summary>
    public class LspFile
    {
        /// <summary>
        /// Load the lsp on difference dimensions for each frame for a sentence from a file.
        /// </summary>
        /// <typeparam name="T">Data type.</typeparam>
        /// <param name="lspFile">The file containing lsp info for a sentence.</param>
        /// <param name="lspOrder">The LPC order, 40 is a usual value.</param>
        /// <returns>A list of lsp, on each dimension of each frame.</returns>
        public static List<List<T>> LoadLspWithGain<T>(string lspFile, int lspOrder)
        {
            return LoadData<T>(lspFile, lspOrder, 0, lspOrder + 1);
        }

        /// <summary>
        /// Load the lsp on difference dimensions for each frame for a sentence from a file.
        /// </summary>
        /// <typeparam name="T">Data type.</typeparam>
        /// <param name="lspFile">The file containing lsp info for a sentence.</param>
        /// <param name="lspOrder">The LPC order, 40 is a usual value.</param>
        /// <returns>A list of lsp, on each dimension of each frame.</returns>
        public static List<List<T>> LoadLspWithoutGain<T>(string lspFile, int lspOrder)
        {
            return LoadData<T>(lspFile, lspOrder, 0, lspOrder);
        }

        /// <summary>
        /// Load the lsp on difference dimensions for each frame for a sentence from a file.
        /// </summary>
        /// <typeparam name="T">Data type.</typeparam>
        /// <param name="lspFile">The file containing lsp info for a sentence.</param>
        /// <param name="lspOrder">The LPC order, 40 is a usual value.</param>
        /// <returns>A list of lsp, on each dimension of each frame.</returns>
        public static List<List<T>> LoadGain<T>(string lspFile, int lspOrder)
        {
            return LoadData<T>(lspFile, lspOrder, lspOrder, lspOrder + 1);
        }

        /// <summary>
        /// Load the lsp on difference dimensions for each frame for a sentence from a file.
        /// </summary>
        /// <typeparam name="T">Data type.</typeparam>
        /// <param name="lspFile">The file containing lsp info for a sentence.</param>
        /// <param name="lspOrder">The LPC order, 40 is a usual value.</param>
        /// <param name="startOrder">Start order to add into list.</param>
        /// <param name="endOrder">End order to add into list.</param>
        /// <returns>A list of lsp, on each dimension of each frame.</returns>
        public static List<List<T>> LoadData<T>(string lspFile, int lspOrder, int startOrder, int endOrder)
        {
            if (startOrder < 0 || startOrder >= endOrder || endOrder > lspOrder + 1)
            {
                throw new ArgumentException(
                    Helper.NeutralFormat("invalidate error, start:{0}, end:{1}, all:{2}", startOrder, endOrder, lspOrder));
            }

            Helper.CheckFileExists(lspFile);
            FileInfo lspFileInfo = new FileInfo(lspFile);
            
            // Lsp for each frame is stored in (lpcOrder + 1) dimensions,
            // and each dimension is stored in a float
            int frameBytes = (lspOrder + 1) * sizeof(float);
            if (lspFileInfo.Length % frameBytes != 0)
            {
                throw new InvalidDataException(Helper.NeutralFormat("Mismatch between lspFile and lspOrder, {0} vs. {1}", lspFile, lspOrder));
            }

            int frameCount = (int)lspFileInfo.Length / frameBytes;
            List<List<T>> dataList = new List<List<T>>();

            FileStream file = File.OpenRead(lspFile);
            try
            {
                using (BinaryReader lspReader = new BinaryReader(file))
                {
                    file = null;
                    for (int frame = 0; frame < frameCount; ++frame)
                    {
                        List<T> frameData = new List<T>();
                        for (int order = 0; order < lspOrder + 1; ++order)
                        {
                            float value = lspReader.ReadSingle();
                            if (order >= startOrder && order < endOrder)
                            {
                                T lsp = (T)Convert.ChangeType(value, typeof(T));
                                frameData.Add(lsp);
                            }
                        }

                        dataList.Add(frameData);
                    }
                }

                return dataList;
            }
            finally
            {
                if (null != file)
                {
                    file.Dispose();
                }
            }
        }

        /// <summary>
        /// Load the lsp on difference dimensions for each frame for a sentence from a file.
        /// </summary>
        /// <typeparam name="T">Data type.</typeparam>
        /// <param name="lspFile">The file containing lsp info for a sentence.</param>
        /// <param name="lspData">The Lsp data.</param>
        public static void WriteData<T>(string lspFile, List<List<T>> lspData)
        {
            if (lspData.Count == 0 || lspData[0].Count == 0)
            {
                throw new ArgumentException(
                    Helper.NeutralFormat("invalidate error, frame number:{0}, lsp order:{1}", lspData.Count, lspData[0].Count));
            }

            FileStream file = File.OpenWrite(lspFile);
            try
            {
                using (BinaryWriter lpccWriter = new BinaryWriter(file))
                {
                    file = null;

                    for (int frame = 0; frame < lspData.Count; ++frame)
                    {
                        for (int order = 0; order < lspData[frame].Count; ++order)
                        {
                            lpccWriter.Write((float)Convert.ChangeType(lspData[frame][order], typeof(float)));
                        }
                    }
                }
            }
            finally
            {
                if (null != file)
                {
                    file.Dispose();
                }
            }
        }
    }
}