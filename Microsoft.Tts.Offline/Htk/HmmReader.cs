//----------------------------------------------------------------------------
// <copyright file="HmmReader.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements HMM model loading
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Htk
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// HMM symbols, this enum is defined by HTK.
    /// Please do not change the order and the value for sub-items
    /// The name of the subitem is enforced to follow HTK definition, 
    /// So just forget the C# coding style here and do not change them.
    /// </summary>
    public enum HmmSymbol
    {
        /// <summary>
        /// BeginHMM.
        /// </summary>
        BeginHMM = 0,

        /// <summary>
        /// UseMac.
        /// </summary>
        UseMac,

        /// <summary>
        /// EndHMM.
        /// </summary>
        EndHMM,

        /// <summary>
        /// NumMixes.
        /// </summary>
        NumMixes,

        /// <summary>
        /// NumStates.
        /// </summary>
        NumStates,

        /// <summary>
        /// StreamInfo.
        /// </summary>
        StreamInfo,

        /// <summary>
        /// VecSize.
        /// </summary>
        VecSize,

        /// <summary>
        /// MSDINFO.
        /// </summary>
        MSDINFO,

        /// <summary>
        /// NDur.
        /// </summary>
        NDur,

        /// <summary>
        /// PDur.
        /// </summary>
        PDur,

        /// <summary>
        /// GDUR.
        /// </summary>
        GDUR,

        /// <summary>
        /// RelDur.
        /// </summary>
        RelDur,

        /// <summary>
        /// GenDur.
        /// </summary>
        GenDur,

        /// <summary>
        /// DiagCov.
        /// </summary>
        DiagCov,

        /// <summary>
        /// FullCov.
        /// </summary>
        FullCov,

        /// <summary>
        /// XFormCov.
        /// </summary>
        XFormCov,

        /// <summary>
        /// State.
        /// </summary>
        State,

        /// <summary>
        /// Tmix.
        /// </summary>
        Tmix,

        /// <summary>
        /// Mixture.
        /// </summary>
        Mixture,

        /// <summary>
        /// Stream.
        /// </summary>
        Stream,

        /// <summary>
        /// SWeight.
        /// </summary>
        SWeight,

        /// <summary>
        /// Mean.
        /// </summary>
        Mean,

        /// <summary>
        /// Variance.
        /// </summary>
        Variance,

        /// <summary>
        /// InvCovar.
        /// </summary>
        InvCovar,

        /// <summary>
        /// XForm.
        /// </summary>
        XForm,

        /// <summary>
        /// GConst.
        /// </summary>
        GConst,

        /// <summary>
        /// Duration.
        /// </summary>
        Duration,

        /// <summary>
        /// InvDiagCov.
        /// </summary>
        InvDiagCov,

        /// <summary>
        /// TransP.
        /// </summary>
        TransP,

        /// <summary>
        /// Dprob.
        /// </summary>
        Dprob,

        /// <summary>
        /// LltCov.
        /// </summary>
        LltCov,

        /// <summary>
        /// LltCovar.
        /// </summary>
        LltCovar,

        /// <summary>
        /// HmmSetID.
        /// </summary>
        HmmSetID = 119,

        /// <summary>
        /// ParmKind.
        /// </summary>
        ParmKind = 120,

        /// <summary>
        /// Macro.
        /// </summary>
        Macro = 121,

        /// <summary>
        /// EofSym.
        /// </summary>
        EofSym = 122,

        /// <summary>
        /// NullSym.
        /// </summary>
        NullSym = 123
    }

    /// <summary>
    /// HTK macro status during MMF read.
    /// </summary>
    public class Macro
    {
        /// <summary>
        /// Gets or sets Macro type.
        /// </summary>
        public char Type { get; set; }

        /// <summary>
        /// Gets or sets Current HMM reader instance.
        /// </summary>
        public HmmReader Reader { get; set; }
    }

    /// <summary>
    /// HMM model reader.
    /// </summary>
    public class HmmReader : IDisposable
    {
        #region Field

        private string _modelFile;
        private FileStream _hmmfs;
        private BinaryReader _hmmbr;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the HmmReader class.
        /// </summary>
        /// <param name="filePath">HMM model file path.</param>
        public HmmReader(string filePath)
        {
            Helper.ThrowIfNull(filePath);

            if (!File.Exists(filePath))
            {
                string message = Helper.NeutralFormat("The model file [{0}] could not be found!", filePath);
                throw new InvalidDataException(message);
            }

            _modelFile = Helper.GetLocalShareFileFullPath(filePath);
            _hmmfs = new FileStream(_modelFile, FileMode.Open, FileAccess.Read);
            _hmmbr = new BinaryReader(_hmmfs);
        }

        #endregion

        #region public operations

        /// <summary>
        /// Enumerate all HMM stream in MMF file.
        /// </summary>
        /// <param name="filePath">The location to MMF file.</param>
        /// <returns>HMM streams.</returns>
        public static IEnumerable<HmmStream> Streams(string filePath)
        {
            Helper.ThrowIfNull(filePath);
            foreach (Macro macro in Macros(filePath))
            {
                if (macro.Type == 'p' || macro.Type == 's')
                {
                    HmmStream hmmStream = macro.Reader.ReadStream();
                    yield return hmmStream;
                }

                if (macro.Type == 't')
                {
                    macro.Reader.ReadTransMatrix();
                }

                if (macro.Type == 'v')
                {
                    macro.Reader.ReadVariance();
                }
            }
        }

        /// <summary>
        /// Read stream widths.
        /// </summary>
        /// <param name="filePath">The location to MMF file.</param>
        /// <param name="streamIndexes">Stream indexes.</param>
        /// <returns>Stream widths.</returns>
        public static int[] ReadtStreamWidths(string filePath, int[] streamIndexes)
        {
            Helper.ThrowIfNull(filePath);
            Helper.ThrowIfNull(streamIndexes);
            foreach (Macro macro in Macros(filePath))
            {
                if (macro.Type == 'o')
                {
                    return ReadStreamWidths(macro.Reader, streamIndexes);
                }
            }

            return null;
        }

        /// <summary>
        /// Get Gaussians width information.
        /// </summary>
        /// <param name="hmr">Model reader.</param>
        /// <param name="streamIndexes">Stream indexes.</param>
        /// <returns>Stream Widths.</returns>
        public static int[] ReadStreamWidths(HmmReader hmr, int[] streamIndexes)
        {
            Helper.ThrowIfNull(hmr);
            Helper.ThrowIfNull(streamIndexes);
            HmmSymbol symbol = hmr.ReadNextSymbol();
            Debug.Assert(symbol == HmmSymbol.StreamInfo);

            short[] mmfStreamWidths = hmr.ReadInt16Array();
            Debug.Assert(streamIndexes.Length <= mmfStreamWidths.Length);

            int[] streamWidths = new int[streamIndexes.Length];
            for (int i = 0; i < streamIndexes.Length; i++)
            {
                streamWidths[i] = mmfStreamWidths[streamIndexes[i] - 1]; // To 0 based index
            }

            if (hmr.ReadNextSymbol() == HmmSymbol.MSDINFO)
            {
                hmr.ReadInt16Array();
            }

            return streamWidths;
        }

        /// <summary>
        /// Get state distribution and physical HMM model mapping [Key = "state name", Value = "HMM names"].
        /// </summary>
        /// <param name="fileName">Binary HMM model file.</param>
        /// <param name="stateTag">Tag indicating which model mapping to get.</param>
        /// <returns>The state distribution->model mapping.</returns>
        public static Dictionary<string, Collection<string>> GetStateAndHmmMapping(string fileName, string stateTag)
        {
            Helper.ThrowIfNull(fileName);
            Helper.ThrowIfNull(stateTag);

            Dictionary<string, Collection<string>> mapping = new Dictionary<string, Collection<string>>();
            using (HmmReader hr = new HmmReader(fileName))
            {
                bool isHmmPart = false;
                string hmmName = string.Empty;
                char macroType = ' ';
                while (hr.ReadNextMacro(out macroType))
                {
                    if (macroType.Equals('p') || macroType.Equals('s'))
                    {
                        string state = hr.ReadString();

                        if (isHmmPart)
                        {
                            if (mapping.ContainsKey(state))
                            {
                                mapping[state].Add(hmmName);
                            }
                        }
                        else
                        {
                            // check whether it's the required type
                            if (state.Contains(stateTag))
                            {
                                mapping.Add(state, new Collection<string>());
                            }

                            // Don't skip reading the Gaussian distribution!!
                            hr.ReadGaussian();
                        }
                    }
                    else if (macroType.Equals('t'))
                    {
                        if (!isHmmPart)
                        {
                            hr.ReadTransMatrix();
                        }
                    }
                    else if (macroType.Equals('h'))
                    {
                        hmmName = hr.ReadString();

                        if (!isHmmPart)
                        {
                            isHmmPart = true;
                        }
                    }
                    else if (macroType == 'v')
                    {
                        hr.ReadVariance();
                    }
                }
            }

            return mapping;
        }

        /// <summary>
        /// Get physical HMM and state distribution model mapping [Key = "HMM name", 
        /// Value = "model names". First dimension is state, second dimension is stream].
        /// </summary>
        /// <param name="mmfFileName">Binary HMM model file.</param>
        /// <returns>The model->state distribution mapping.</returns>
        public static Dictionary<string, string[,]> GetHmmAndStateMapping(string mmfFileName)
        {
            Helper.ThrowIfNull(mmfFileName);

            Dictionary<string, Collection<string>> hmmModels = new Dictionary<string, Collection<string>>();
            int streamNumber = -1;

            using (HmmReader hr = new HmmReader(mmfFileName))
            {             
                bool isHmmPart = false;
                string hmmName = string.Empty;
                char macroType = ' ';
                while (hr.ReadNextMacro(out macroType))
                {
                    if (macroType.Equals('p') || macroType.Equals('s'))
                    {
                        string state = hr.ReadString();

                        if (isHmmPart)
                        {
                            Debug.Assert(hmmModels.ContainsKey(hmmName));
                            hmmModels[hmmName].Add(state);
                        }
                        else
                        {
                            // Don't skip reading the Gaussian distribution!!
                            hr.ReadGaussian();
                        }
                    }
                    else if (macroType.Equals('t'))
                    {
                        if (!isHmmPart)
                        {
                            hr.ReadTransMatrix();
                        }
                    }
                    else if (macroType.Equals('h'))
                    {
                        hmmName = hr.ReadString();
                        if (!hmmModels.ContainsKey(hmmName))
                        {
                            hmmModels.Add(hmmName, new Collection<string>());
                        }

                        if (!isHmmPart)
                        {
                            isHmmPart = true;
                        }
                    }
                    else if (macroType.Equals('o'))
                    {
                        HmmSymbol symbol = hr.ReadNextSymbol();
                        Debug.Assert(symbol == HmmSymbol.StreamInfo);

                        short[] mmfStreamWidths = hr.ReadInt16Array();
                        streamNumber = mmfStreamWidths.Length;
                    }
                    else if (macroType == 'v')
                    {
                        hr.ReadVariance();
                    }
                }
            }

            Dictionary<string, string[,]> model2Streams = new Dictionary<string, string[,]>();
            foreach (KeyValuePair<string, Collection<string>> pair in hmmModels)
            {
                Debug.Assert(pair.Value.Count % streamNumber == 0);
                int stateNumber = pair.Value.Count / streamNumber;

                string[,] streams = new string[stateNumber, streamNumber];
                for (int i = 0; i < stateNumber; ++i)
                {
                    for (int j = 0; j < streamNumber; ++j)
                    {
                        streams[i, j] = pair.Value[(i * streamNumber) + j];
                    }
                }

                model2Streams.Add(pair.Key, streams);
            }

            return model2Streams;
        }

        /// <summary>
        /// Gets integer array from HMM model.
        /// </summary>
        /// <returns>Short array.</returns>
        public short[] ReadInt16Array()
        {
             short length = _hmmbr.ReadInt16();
             short[] values = new short[length];
             for (int i = 0; i < length; i++)
             {
                 values[i] = _hmmbr.ReadInt16();
             }

             return values;
        }

        /// <summary>
        /// Get float array from HMM model.
        /// </summary>
        /// <returns>Float array.</returns>
        public double[] ReadFloatArray()
        {
            short length = _hmmbr.ReadInt16();
            double[] values = new double[length];
            for (int i = 0; i < length; i++)
            {
                values[i] = _hmmbr.ReadSingle();
            }

            return values;
        }

        /// <summary>
        /// Reads one string from Model.
        /// </summary>
        /// <returns>String value.</returns>
        public string ReadString()
        {
            string name = string.Empty;
            char buff = ' ';
            while (buff == ' ')
            {
                buff = (char)_hmmbr.ReadByte();
            }

            do
            {
                buff = (char)_hmmbr.ReadByte();
                name = name + buff.ToString();
            }
            while (buff != '"');

            name = name.Remove(name.Length - 1, 1);
            return name;
        }

        /// <summary>
        /// Get a transition matrix.
        /// </summary>
        /// <returns>Transition matrix.</returns>
        public float[,] ReadTransMatrix()
        {
            HmmSymbol symbol = ReadNextSymbol();
            Debug.Assert(symbol == HmmSymbol.TransP);

            short stateCount = _hmmbr.ReadInt16();
            float[,] transMatrix = new float[stateCount, stateCount];
            for (int i = 0; i < stateCount; ++i)
            {
                for (int j = 0; j < stateCount; ++j)
                {
                    transMatrix[i, j] = _hmmbr.ReadSingle();
                }
            }

            return transMatrix;
        }

        /// <summary>
        /// Get varFloor array.
        /// </summary>
        /// <returns> VarFloor array. </returns>
        public double[] ReadVariance()
        {
            HmmSymbol symbol = ReadNextSymbol();
            Debug.Assert(symbol == HmmSymbol.Variance);
            return ReadFloatArray();
        }

        /// <summary>
        /// Get a Gaussian distribution.
        /// </summary>
        /// <returns>Gaussian distribution.</returns>
        public Gaussian[] ReadGaussian()
        {
            HmmSymbol symbol;
            int mixtureCount = 1;

            if ((symbol = ReadNextSymbol()) != HmmSymbol.Mean)
            {
                if ((symbol = ReadNextSymbol()) == HmmSymbol.NumMixes)
                {
                    mixtureCount = _hmmbr.ReadInt32();
                }
            }

            Gaussian[] stream = new Gaussian[mixtureCount];

            // Get Gaussian parameters.
            for (int i = 0; i < mixtureCount; i++)
            {
                if (symbol != HmmSymbol.Mean)
                {
                    // Get weight.
                    symbol = ReadNextSymbol();
                    Debug.Assert(symbol == HmmSymbol.Mixture);
                    _hmmbr.ReadInt16();
                    stream[i].Weight = _hmmbr.ReadSingle();
                    symbol = ReadNextSymbol();
                    Debug.Assert(symbol == HmmSymbol.Mean);
                }
                else
                {
                    stream[i].Weight = 1.0f;
                }

                stream[i].Mean = ReadFloatArray();
                stream[i].Length = stream[i].Mean.Length;

                symbol = ReadNextSymbol();
                Debug.Assert(symbol == HmmSymbol.Variance);
                stream[i].Variance = ReadFloatArray();
                Debug.Assert(stream[i].Length == stream[i].Variance.Length);

                symbol = ReadNextSymbol();
                Debug.Assert(symbol == HmmSymbol.GConst);
                stream[i].GlobalConstant = _hmmbr.ReadSingle();
            }

            return stream;
        }

        /// <summary>
        /// Get a stream distribution.
        /// </summary>
        /// <returns>HMM stream.</returns>
        public HmmStream ReadStream()
        {
            HmmStream hmmStream = new HmmStream();
            hmmStream.Name = ReadString();
            hmmStream.Gaussians = ReadGaussian();
            return hmmStream;
        }

        /// <summary>
        /// Close the reader.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            System.GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Get next HMM symbol.
        /// </summary>
        /// <returns>HMM symbol.</returns>
        public HmmSymbol ReadNextSymbol()
        {
            char ch = ' ';
            HmmSymbol symbol;
            
            try
            {
                while (ch != ':')
                {
                    ch = (char)_hmmbr.ReadByte();
                }

                ch = (char)_hmmbr.ReadByte();
                symbol = (HmmSymbol)ch;
            }
            catch (EndOfStreamException)
            {
                symbol = HmmSymbol.EofSym;
            }

            return symbol;
        }

        /// <summary>
        /// Dispose this object.
        /// </summary>
        /// <param name="disposing">Flag indicating whether delete managed resource.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _hmmfs.Close();
                _hmmbr.Close();
            }
        }

        /// <summary>
        /// Read macros in the MMF file.
        /// </summary>
        /// <param name="filePath">MMF file.</param>
        /// <returns>Macros.</returns>
        private static IEnumerable<Macro> Macros(string filePath)
        {
            Helper.ThrowIfNull(filePath);
            using (HmmReader reader = new HmmReader(filePath))
            {
                for (char macroType = ' '; reader.ReadNextMacro(out macroType);)
                {
                    Macro macro = new Macro() { Type = macroType, Reader = reader };
                    yield return macro;

                    if (macroType == 'h')
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Load model macro.
        /// </summary>
        /// <param name="type">Macro type.</param>
        /// <returns>Whether the macro is discovered successfully.</returns>
        private bool ReadNextMacro(out char type)
        {
            const string ValidType = "smuxdcrpabgfyjvitwho";
            char sym = ' ';
            type = '0';

            try
            {
                while (true)
                {
                    if (sym != '~')
                    {
                        if (_hmmbr.BaseStream.Position == _hmmbr.BaseStream.Length)
                        {
                            return false;
                        }

                        sym = (char)_hmmbr.ReadByte();
                    }

                    if (sym == '~')
                    {
                        sym = (char)_hmmbr.ReadByte();
                        if (sym != '~')
                        {
                            type = char.ToLower(sym, CultureInfo.InvariantCulture);
                            sym = (char)_hmmbr.ReadByte();
                            if (ValidType.IndexOf(type) >= 0)
                            {
                                if (sym == ' ' || sym == 0x0a)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            catch (EndOfStreamException)
            {
                return false;
            }
        }

        #endregion
    }
}