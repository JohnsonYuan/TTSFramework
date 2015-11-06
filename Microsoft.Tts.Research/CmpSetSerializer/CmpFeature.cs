//----------------------------------------------------------------------------
// <copyright file="CmpFeature.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements cmp feature class
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Research
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Describe bit mask for feature representation.
    /// </summary>
    public enum FeatureDescBit
    {
        /// <summary>
        /// None.
        /// </summary>
        None = 0x00000000,

        /// <summary>
        /// Lsp.
        /// </summary>
        Lsp = 0x00000001,

        /// <summary>
        /// Gain.
        /// </summary>
        Gain = 0x00000002,

        /// <summary>
        /// F0.
        /// </summary>
        F0 = 0x00000004,

        /// <summary>
        /// Mbe.
        /// </summary>
        Mbe = 0x00000008,

        /// <summary>
        /// Power.
        /// </summary>
        Power = 0x00000010,

        /// <summary>
        /// Guidance Lsp.
        /// </summary>
        GuidanceLsp = 0x00000020,

        /// <summary>
        /// Static.
        /// </summary>
        Static = 0x00000040,

        /// <summary>
        /// Delta.
        /// </summary>
        delta = 0x00000080,

        /// <summary>
        /// Acceleration.
        /// </summary>
        Acceleration = 0x00000100,

        /// <summary>
        /// All.
        /// </summary>
        All = FeatureDescBit.Lsp | FeatureDescBit.Gain | FeatureDescBit.F0 | FeatureDescBit.Mbe | FeatureDescBit.Power | FeatureDescBit.GuidanceLsp | 
            FeatureDescBit.Static | FeatureDescBit.delta | FeatureDescBit.Acceleration,
    }

    /// <summary>
    /// Defines the cmp feature data and operations.
    /// </summary>
    public class CmpFeature
    {
        #region fields

        /// <summary>
        /// Lsp static feature.
        /// </summary>
        private List<float> _lspStatic = new List<float>();

        /// <summary>
        /// Lsp delta feature.
        /// </summary>
        private List<float> _lspDelta = new List<float>();

        /// <summary>
        /// Lsp acceleration feature.
        /// </summary>
        private List<float> _lspAcceleration = new List<float>();

        /// <summary>
        /// Gain static feature.
        /// </summary>
        private float _gainStatic;

        /// <summary>
        /// Gain delta feature.
        /// </summary>
        private float _gainDelta;

        /// <summary>
        /// Gain acceleration feature.
        /// </summary>
        private float _gainAcceleration;

        /// <summary>
        /// F0 static feature, in log domain.
        /// </summary>
        private float _f0Static;

        /// <summary>
        /// F0 delta feature.
        /// </summary>
        private float _f0Delta;

        /// <summary>
        /// F0 acceleration feature.
        /// </summary>
        private float _f0Acceleration;

        /// <summary>
        /// Mbe static feature.
        /// </summary>
        private List<float> _mbeStatic = new List<float>();

        /// <summary>
        /// Mbe delta feature.
        /// </summary>
        private List<float> _mbeDelta = new List<float>();

        /// <summary>
        /// Mbe acceleration feature.
        /// </summary>
        private List<float> _mbeAcceleration = new List<float>();

        /// <summary>
        /// Power static feature.
        /// </summary>
        private float _powerStatic;

        /// <summary>
        /// Power delta feature.
        /// </summary>
        private float _powerDelta;

        /// <summary>
        /// Power accelaration feature.
        /// </summary>
        private float _powerAcceleration;

        /// <summary>
        /// Guidance Lsp static feature.
        /// </summary>
        private List<float> _guidanceLspStatic = new List<float>();

        /// <summary>
        /// Guidance Lsp delta feature.
        /// </summary>
        private List<float> _guidanceLspDelta = new List<float>();

        /// <summary>
        /// Guidance Lsp acceleration feature.
        /// </summary>
        private List<float> _guidanceLspAcceleration = new List<float>();

        #endregion

        #region public types

        /// <summary>
        /// F0 convert mode.
        /// </summary>
        public enum F0ConvertMode
        {
            /// <summary>
            /// Convert from linear domain to log domain.
            /// </summary>
            LinearToLog = 0,

            /// <summary>
            /// Convert from log domain to linear domain.
            /// </summary>
            LogToLinear,
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets lsp static feature.
        /// </summary>
        public List<float> LspStatic
        {
            get
            {
                return _lspStatic;
            }
        }

        /// <summary>
        /// Gets lsp delta feature.
        /// </summary>
        public List<float> LspDelta
        {
            get
            {
                return _lspDelta;
            }
        }

        /// <summary>
        /// Gets lsp acceleration feature.
        /// </summary>
        public List<float> LspAcceleration
        {
            get
            {
                return _lspAcceleration;
            }
        }

        /// <summary>
        /// Gets or sets gain static feature.
        /// </summary>
        public float GainStatic
        {
            get
            {
                return _gainStatic;
            }

            set
            {
                _gainStatic = value;
            }
        }

        /// <summary>
        /// Gets or sets gain delta feature.
        /// </summary>
        public float GainDelta
        {
            get
            {
                return _gainDelta;
            }

            set
            {
                _gainDelta = value;
            }
        }

        /// <summary>
        /// Gets or sets gain acceleration feature.
        /// </summary>
        public float GainAcceleration
        {
            get
            {
                return _gainAcceleration;
            }

            set
            {
                _gainAcceleration = value;
            }
        }

        /// <summary>
        /// Gets or sets f0 static feature, in log domain.
        /// </summary>
        public float F0Static
        {
            get
            {
                return _f0Static;
            }

            set
            {
                _f0Static = value;
            }
        }

        /// <summary>
        /// Gets or sets f0 delta feature.
        /// </summary>
        public float F0Delta
        {
            get
            {
                return _f0Delta;
            }

            set
            {
                _f0Delta = value;
            }
        }

        /// <summary>
        /// Gets or sets f0 acceleration feature.
        /// </summary>
        public float F0Acceleration
        {
            get
            {
                return _f0Acceleration;
            }

            set
            {
                _f0Acceleration = value;
            }
        }

        /// <summary>
        /// Gets or sets power static feature.
        /// </summary>
        public float PowerStatic
        {
            get
            {
                return _powerStatic;
            }

            set
            {
                _powerStatic = value;
            }
        }

        /// <summary>
        /// Gets or sets power delta feature.
        /// </summary>
        public float PowerDelta
        {
            get
            {
                return _powerDelta;
            }

            set
            {
                _powerDelta = value;
            }
        }

        /// <summary>
        /// Gets or sets power acceleration feature.
        /// </summary>
        public float PowerAcceleration
        {
            get
            {
                return _powerAcceleration;
            }

            set
            {
                _powerAcceleration = value;
            }
        }

        /// <summary>
        /// Gets mbe static feature.
        /// </summary>
        public List<float> MbeStatic
        {
            get
            {
                return _mbeStatic;
            }
        }

        /// <summary>
        /// Gets mbe delta feature.
        /// </summary>
        public List<float> MbeDelta
        {
            get
            {
                return _mbeDelta;
            }
        }

        /// <summary>
        /// Gets mbe acceleration feature.
        /// </summary>
        public List<float> MbeAcceleration
        {
            get
            {
                return _mbeAcceleration;
            }
        }

        /// <summary>
        /// Gets Guidance Lsp static feature.
        /// </summary>
        public List<float> GuidanceLspStatic
        {
            get
            {
                return _guidanceLspStatic;
            }
        }

        /// <summary>
        /// Gets Guidance Lsp delta feature.
        /// </summary>
        public List<float> GuidanceLspDelta
        {
            get
            {
                return _guidanceLspDelta;
            }
        }

        /// <summary>
        /// Gets Guidance Lsp acceleration feature.
        /// </summary>
        public List<float> GuidanceLspAcceleration
        {
            get
            {
                return _guidanceLspAcceleration;
            }
        }

        #endregion

        #region public static methods

        /// <summary>
        /// Parse binary data to get a CmpFeature object.
        /// </summary>
        /// <param name="reader">BinaryReader for the data to be load.</param>
        /// <param name="recordSize">Size of each record.</param>
        /// <param name="lspOrder">Order of lsp.</param>
        /// <param name="mbeOrder">Order of mbe, if no mbe feature in cmp file, please input 0. Or,input it's real value.</param>
        /// <param name="powerOrder">Order of power, if no power feature in cmp file, please input 0. Or,input it's real value.</param>
        /// <param name="guidanceLspOrder">Order of guidance lsp, if no guidance lsp feature in cmp file, please input 0. Or,input it's real value.</param>
        /// <returns>The generated CmpFeature object.</returns>
        public static CmpFeature ParseCmpFeature(BinaryReader reader, int recordSize, int lspOrder, int mbeOrder, int powerOrder, int guidanceLspOrder)
        {
            int gainAndF0Order = 2;
            int placeHolder = gainAndF0Order + mbeOrder + powerOrder + guidanceLspOrder;
            const int FeatureStreams = 3;

            if (checked(lspOrder + placeHolder) * FeatureStreams * sizeof(float) != recordSize)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Inconsistency between recordeSize ({0}) and lspOrder ({1}).",
                    recordSize, lspOrder);
                throw new InvalidDataException(message);
            }

            bool withMbe = (0 == mbeOrder) ? false : true;
            bool withPower = (0 == powerOrder) ? false : true;
            bool withGuidanceLsp = (0 == guidanceLspOrder) ? false : true;

            CmpFeature cmpFeature = new CmpFeature();

            byte[] buffer = reader.ReadBytes(recordSize);
            int indexBuffer = 0;

            ParseVector(cmpFeature.LspStatic, lspOrder, buffer, ref indexBuffer);

            cmpFeature.GainStatic = ParseSingle(buffer, ref indexBuffer);

            ParseVector(cmpFeature.LspDelta, lspOrder, buffer, ref indexBuffer);

            cmpFeature.GainDelta = ParseSingle(buffer, ref indexBuffer);

            ParseVector(cmpFeature.LspAcceleration, lspOrder, buffer, ref indexBuffer);

            cmpFeature.GainAcceleration = ParseSingle(buffer, ref indexBuffer);

            cmpFeature.F0Static = ParseSingle(buffer, ref indexBuffer);

            cmpFeature.F0Delta = ParseSingle(buffer, ref indexBuffer);

            cmpFeature.F0Acceleration = ParseSingle(buffer, ref indexBuffer);

            if (withMbe)
            {
                ParseVector(cmpFeature.MbeStatic, mbeOrder, buffer, ref indexBuffer);

                ParseVector(cmpFeature.MbeDelta, mbeOrder, buffer, ref indexBuffer);

                ParseVector(cmpFeature.MbeAcceleration, mbeOrder, buffer, ref indexBuffer);
            }
                
            if (withPower)
            {
                cmpFeature.PowerStatic = ParseSingle(buffer, ref indexBuffer);

                cmpFeature.PowerDelta = ParseSingle(buffer, ref indexBuffer);

                cmpFeature.PowerAcceleration = ParseSingle(buffer, ref indexBuffer);
            }

            if (withGuidanceLsp)
            {
                ParseVector(cmpFeature.GuidanceLspStatic, guidanceLspOrder, buffer, ref indexBuffer);

                ParseVector(cmpFeature.GuidanceLspDelta, guidanceLspOrder, buffer, ref indexBuffer);

                ParseVector(cmpFeature.GuidanceLspAcceleration, guidanceLspOrder, buffer, ref indexBuffer);
            } 
            
            const float UnvoicedFlag = -1e+9F;
            ResetUnvoicedF0(cmpFeature, UnvoicedFlag);

            return cmpFeature;
        }

        /// <summary>
        /// Convert f0 from linear domain to log domain or vice versa.
        /// </summary>
        /// <param name="f0">The original f0 value.</param>
        /// <param name="convertMode">F0 convert mode.</param>
        /// <returns>The converted f0 value.</returns>
        public static float ConvertF0(float f0, F0ConvertMode convertMode)
        {
            float f0Result = 0;

            if (f0 > 0)
            {
                switch (convertMode)
                {
                    case F0ConvertMode.LinearToLog:
                        f0Result = (float)Math.Log(f0);
                        break;

                    case F0ConvertMode.LogToLinear:
                        f0Result = (float)Math.Exp(f0);
                        break;

                    default:
                        throw new InvalidOperationException("Invalid f0 convert mode");
                }
            }

            return f0Result;
        }

        #endregion

        #region public methods

        /// <summary>
        /// Get feature vector according to description bits.
        /// The order shoud be:
        ///     1. lsp static, gain static
        ///     2. lsp delta, gain delta
        ///     3. lsp acceleration, gain acceleration
        ///     4. f0 static (in log domain), f0 delta, f0 acceleration.
        /// </summary>
        /// <param name="featureDescBit">Describing what data to be extracted.</param>
        /// <returns>The feature vector.</returns>
        public List<float> GetFeature(FeatureDescBit featureDescBit)
        {
            List<float> featureVec = new List<float>();

            if ((featureDescBit & FeatureDescBit.Static) == FeatureDescBit.Static)
            {
                if ((featureDescBit & FeatureDescBit.Lsp) == FeatureDescBit.Lsp)
                {
                    featureVec.AddRange(_lspStatic);
                }

                if ((featureDescBit & FeatureDescBit.Gain) == FeatureDescBit.Gain)
                {
                    featureVec.Add(_gainStatic);
                }
            }

            if ((featureDescBit & FeatureDescBit.delta) == FeatureDescBit.delta)
            {
                if ((featureDescBit & FeatureDescBit.Lsp) == FeatureDescBit.Lsp)
                {
                    featureVec.AddRange(_lspDelta);
                }

                if ((featureDescBit & FeatureDescBit.Gain) == FeatureDescBit.Gain)
                {
                    featureVec.Add(_gainDelta);
                }
            }

            if ((featureDescBit & FeatureDescBit.Acceleration) == FeatureDescBit.Acceleration)
            {
                if ((featureDescBit & FeatureDescBit.Lsp) == FeatureDescBit.Lsp)
                {
                    featureVec.AddRange(_lspAcceleration);
                }

                if ((featureDescBit & FeatureDescBit.Gain) == FeatureDescBit.Gain)
                {
                    featureVec.Add(_gainAcceleration);
                }
            }

            if ((featureDescBit & FeatureDescBit.F0) == FeatureDescBit.F0)
            {
                if ((featureDescBit & FeatureDescBit.Static) == FeatureDescBit.Static)
                {
                    featureVec.Add(_f0Static);
                }

                if ((featureDescBit & FeatureDescBit.delta) == FeatureDescBit.delta)
                {
                    featureVec.Add(_f0Delta);
                }

                if ((featureDescBit & FeatureDescBit.Acceleration) == FeatureDescBit.Acceleration)
                {
                    featureVec.Add(_f0Acceleration);
                }
            }

            if ((featureDescBit & FeatureDescBit.Mbe) == FeatureDescBit.Mbe)
            {
                if ((featureDescBit & FeatureDescBit.Static) == FeatureDescBit.Static)
                {
                    featureVec.AddRange(_mbeStatic);
                }

                if ((featureDescBit & FeatureDescBit.delta) == FeatureDescBit.delta)
                {
                    featureVec.AddRange(_mbeDelta);
                }

                if ((featureDescBit & FeatureDescBit.Acceleration) == FeatureDescBit.Acceleration)
                {
                    featureVec.AddRange(_mbeAcceleration);
                }
            }

            if ((featureDescBit & FeatureDescBit.Power) == FeatureDescBit.Power)
            {
                if ((featureDescBit & FeatureDescBit.Static) == FeatureDescBit.Static)
                {
                    featureVec.Add(_powerStatic);
                }

                if ((featureDescBit & FeatureDescBit.delta) == FeatureDescBit.delta)
                {
                    featureVec.Add(_powerDelta);
                }

                if ((featureDescBit & FeatureDescBit.Acceleration) == FeatureDescBit.Acceleration)
                {
                    featureVec.Add(_powerAcceleration);
                }
            }

            if ((featureDescBit & FeatureDescBit.GuidanceLsp) == FeatureDescBit.GuidanceLsp)
            {
                if ((featureDescBit & FeatureDescBit.Static) == FeatureDescBit.Static)
                {
                    featureVec.AddRange(_guidanceLspStatic);
                }

                if ((featureDescBit & FeatureDescBit.delta) == FeatureDescBit.delta)
                {
                    featureVec.AddRange(_guidanceLspDelta);
                }

                if ((featureDescBit & FeatureDescBit.Acceleration) == FeatureDescBit.Acceleration)
                {
                    featureVec.AddRange(_guidanceLspAcceleration);
                }
            }

            return featureVec;
        }

        #endregion

        #region Object override

        /// <summary>
        /// Override Equals().
        /// Whether is this obejct equal to another.
        /// </summary>
        /// <param name="obj">The object to be compared.</param>
        /// <returns>True if equal, false if not.</returns>
        public override bool Equals(object obj)
        {
            CmpFeature rhs = obj as CmpFeature;
            if (!_lspStatic.SequenceEqual(rhs._lspStatic))
            {
                return false;
            }
            else if (!_lspDelta.SequenceEqual(rhs._lspDelta))
            {
                return false;
            }
            else if (!_lspAcceleration.SequenceEqual(rhs._lspAcceleration))
            {
                return false;
            }
            else if (_gainStatic != rhs._gainStatic || _gainDelta != rhs._gainDelta || _gainAcceleration != rhs._gainAcceleration)
            {
                return false;
            }
            else if (_f0Static != rhs._f0Static || _f0Delta != rhs._f0Delta || _f0Acceleration != rhs._f0Acceleration)
            {
                return false;
            }
            else if (!_mbeStatic.SequenceEqual(rhs._mbeStatic))
            {
                return false;
            }
            else if (!_mbeDelta.SequenceEqual(rhs._mbeDelta))
            {
                return false;
            }
            else if (!_mbeAcceleration.SequenceEqual(rhs._mbeAcceleration))
            {
                return false;
            }
            else if (_powerStatic != rhs._powerStatic || _powerDelta != rhs._powerDelta || _powerAcceleration != rhs._powerAcceleration)
            {
                return false;
            }
            else if (!_guidanceLspStatic.SequenceEqual(rhs._guidanceLspStatic))
            {
                return false;
            }
            else if (!_guidanceLspDelta.SequenceEqual(rhs._guidanceLspDelta))
            {
                return false;
            }
            else if (!_guidanceLspAcceleration.SequenceEqual(rhs._guidanceLspAcceleration))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Override GetHashCode().
        /// </summary>
        /// <returns>Hash code.</returns>
        public override int GetHashCode()
        {
            return _lspStatic.GetHashCode() ^ _lspDelta.GetHashCode() ^ _lspAcceleration.GetHashCode()
                ^ _gainStatic.GetHashCode() ^ _gainDelta.GetHashCode() ^ _gainAcceleration.GetHashCode()
                ^ _f0Static.GetHashCode() ^ _f0Delta.GetHashCode() ^ _f0Acceleration.GetHashCode()
                ^ _mbeStatic.GetHashCode() ^ _mbeDelta.GetHashCode() ^ _mbeAcceleration.GetHashCode()
                ^ _powerStatic.GetHashCode() ^ _powerDelta.GetHashCode() ^ _powerAcceleration.GetHashCode()
                ^ _guidanceLspStatic.GetHashCode() ^ _guidanceLspDelta.GetHashCode() ^ _guidanceLspAcceleration.GetHashCode();
        }

        #endregion

        #region private static methods

        /// <summary>
        /// Parse a set of data and append to vector.
        /// </summary>
        /// <param name="vector">The target collection.</param>
        /// <param name="order">Order of data to be parsed.</param>
        /// <param name="buffer">Buffer which stores data to be parsed.</param>
        /// <param name="indexBuffer">Index of data into the buffer.</param>
        private static void ParseVector(List<float> vector, int order, byte[] buffer, ref int indexBuffer)
        {
            for (int i = 0; i < order; i++)
            {
                vector.Add(BitConverter.ToSingle(buffer, indexBuffer));
                indexBuffer += sizeof(float);
            }
        }

        /// <summary>
        /// Parse a float data in a buffer.
        /// </summary>
        /// <param name="buffer">Buffer which stores data to be parsed.</param>
        /// <param name="indexBuffer">Index of data into the buffer.</param>
        /// <returns>The single value.</returns>
        private static float ParseSingle(byte[] buffer, ref int indexBuffer)
        {
            float value = BitConverter.ToSingle(buffer, indexBuffer);
            indexBuffer += sizeof(float);
            return value;
        }

        /// <summary>
        /// Reset unvoiced f0 features from negative flag to 0.
        /// </summary>
        /// <param name="feature">The feature to be modified.</param>
        /// <param name="unvoicedFlag">A negative unvoiced flag. F0 featur value less than that will be set to 0.</param>
        private static void ResetUnvoicedF0(CmpFeature feature, float unvoicedFlag)
        {
            Debug.Assert(unvoicedFlag < 0);

            if (feature._f0Static < unvoicedFlag)
            {
                feature._f0Static = 0F;
            }

            if (feature._f0Delta < unvoicedFlag)
            {
                feature._f0Delta = 0F;
            }

            if (feature._f0Acceleration < unvoicedFlag)
            {
                feature._f0Acceleration = 0F;
            }
        }

        #endregion
    }

    /// <summary>
    /// Define a rude comparer for CmpFeature.
    /// </summary>
    public class CmpFeatureRudeCompare : IEqualityComparer<CmpFeature>
    {
        /// <summary>
        /// Delta for comparison.
        /// </summary>
        private float _delta;

        /// <summary>
        /// Initializes a new instance of the <see cref="CmpFeatureRudeCompare"/> class.
        /// </summary>
        /// <param name="delta">Delta for comparison.</param>
        public CmpFeatureRudeCompare(float delta)
        {
            _delta = delta;
        }

        /// <summary>
        /// Override Equals().
        /// </summary>
        /// <param name="x">The left-hand-side.</param>
        /// <param name="y">The right-hand-side.</param>
        /// <returns>True if equal, false if not.</returns>
        public bool Equals(CmpFeature x, CmpFeature y)
        {
            SingleRudeCompare singleCompare = new SingleRudeCompare(_delta);

            if (!x.LspStatic.SequenceEqual(y.LspStatic, singleCompare))
            {
                return false;
            }
            else if (!x.LspDelta.SequenceEqual(y.LspDelta, singleCompare))
            {
                return false;
            }
            else if (!x.LspAcceleration.SequenceEqual(y.LspAcceleration, singleCompare))
            {
                return false;
            }
            else if (!singleCompare.Equals(x.GainStatic, y.GainStatic)
                || !singleCompare.Equals(x.GainDelta, y.GainDelta)
                || !singleCompare.Equals(x.GainAcceleration, y.GainAcceleration))
            {
                return false;
            }
            else if (!singleCompare.Equals(x.F0Static, y.F0Static)
                || !singleCompare.Equals(x.F0Delta, y.F0Delta)
                || !singleCompare.Equals(x.F0Acceleration, y.F0Acceleration))
            {
                return false;
            }
            else if (!x.MbeStatic.SequenceEqual(y.MbeStatic, singleCompare))
            {
                return false;
            }
            else if (!x.MbeDelta.SequenceEqual(y.MbeDelta, singleCompare))
            {
                return false;
            }
            else if (!x.MbeAcceleration.SequenceEqual(y.MbeAcceleration, singleCompare))
            {
                return false;
            }
            else if (!singleCompare.Equals(x.PowerStatic, y.PowerStatic)
                || !singleCompare.Equals(x.PowerDelta, y.PowerDelta)
                || !singleCompare.Equals(x.PowerAcceleration, y.PowerAcceleration))
            {
                return false;
            }
            else if (!x.GuidanceLspStatic.SequenceEqual(y.GuidanceLspStatic, singleCompare))
            {
                return false;
            }
            else if (!x.GuidanceLspDelta.SequenceEqual(y.GuidanceLspDelta, singleCompare))
            {
                return false;
            }
            else if (!x.GuidanceLspAcceleration.SequenceEqual(y.GuidanceLspAcceleration, singleCompare))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Override GetHashCode();.
        /// </summary>
        /// <param name="obj">CmpFeature.</param>
        /// <returns>Hash code.</returns>
        public int GetHashCode(CmpFeature obj)
        {
            return obj.GetHashCode();
        }
    }
}