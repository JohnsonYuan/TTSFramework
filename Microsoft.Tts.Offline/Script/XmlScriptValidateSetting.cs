//----------------------------------------------------------------------------
// <copyright file="XmlScriptValidateSetting.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements the XML script validation setting class
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using Microsoft.Tts.Offline.Common;

    /// <summary>
    /// Definition of validation info.
    /// </summary>
    public enum XmlScriptValidationScope
    {
        /// <summary>
        /// None.
        /// </summary>
        None = 0x00000000,

        /// <summary>
        /// POS.
        /// </summary>
        POS = 0x00000001,

        /// <summary>
        /// Pronunciation.
        /// </summary>
        Pronunciation = 0x00000002,

        /// <summary>
        /// Interval of unvoiced-voiced segment.
        /// </summary>
        UvSegInterval = 0x00000004,

        /// <summary>
        /// Order and non overlap between unvoiced-voiced segs.
        /// </summary>
        UvSegSequence = 0x00000008,

        /// <summary>
        /// F0 contour.
        /// </summary>
        F0 = 0x00000010,

        /// <summary>
        /// Consistency between f0 and unvoiced-voiced type.
        /// </summary>
        F0AndUvType = 0x00000020,

        /// <summary>
        /// Consistency between duration and unvoiced-voiced segment interval.
        /// </summary>
        DurationAndInterval = 0x00000040,

        /// <summary>
        /// Interval of element segment.
        /// </summary>
        SegmentInterval = 0x00000080,

        /// <summary>
        /// Correctness of segment sequence.
        /// </summary>
        SegmentSequence = 0x00000100,

        /// <summary>
        /// Consistency between segment duration and unvoiced-voiced segment interval.
        /// </summary>
        SegmentDurationAndInterval = 0x00000200,

        /// <summary>
        /// Consistency between duration and segment.
        /// </summary>
        DurationAndSegment = 0x00000400,

        /// <summary>
        /// Power contour.
        /// </summary>
        Power = 0x00001000,

        /// <summary>
        /// All acoustics.
        /// </summary>
        Acoustics = XmlScriptValidationScope.UvSegInterval | XmlScriptValidationScope.UvSegSequence |
            XmlScriptValidationScope.F0 | XmlScriptValidationScope.F0AndUvType |
            XmlScriptValidationScope.DurationAndInterval | XmlScriptValidationScope.SegmentInterval |
            XmlScriptValidationScope.SegmentSequence | XmlScriptValidationScope.SegmentDurationAndInterval |
            XmlScriptValidationScope.DurationAndSegment | XmlScriptValidationScope.Power,

        /// <summary>
        /// All.
        /// </summary>
        All = XmlScriptValidationScope.POS | XmlScriptValidationScope.Pronunciation |
            XmlScriptValidationScope.Acoustics,
    }

    /// <summary>
    /// XML Script file class.
    /// </summary>
    public class XmlScriptValidateSetting : XmlValidateSetting
    {
        #region Private fields

        private XmlScriptValidationScope _scope;
        private TtsPhoneSet _phoneSet;
        private TtsPosSet _posSet;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlScriptValidateSetting"/> class.
        /// </summary>
        public XmlScriptValidateSetting()
        {
            _scope = XmlScriptValidationScope.None;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlScriptValidateSetting"/> class.
        /// </summary>
        /// <param name="phoneSet">Phone set.</param>
        /// <param name="posSet">POS set.</param>
        public XmlScriptValidateSetting(TtsPhoneSet phoneSet, TtsPosSet posSet) 
        {
            _scope = XmlScriptValidationScope.None;

            if (phoneSet != null)
            {
                _scope |= XmlScriptValidationScope.Pronunciation;
            }

            if (posSet != null)
            {
                _scope |= XmlScriptValidationScope.POS;
            }

            _phoneSet = phoneSet;
            _posSet = posSet;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Phone set.
        /// </summary>
        public XmlScriptValidationScope ValidationScope
        {
            get
            {
                return _scope;
            }

            set
            {
                _scope = value;
            }
        }

        /// <summary>
        /// Gets or sets Phone set.
        /// </summary>
        public TtsPhoneSet PhoneSet
        {
            get
            {
                return _phoneSet;
            }

            set
            {
                _phoneSet = value;
            }
        }

        /// <summary>
        /// Gets or sets POS set.
        /// </summary>
        public TtsPosSet PosSet
        {
            get
            {
                return _posSet;
            }

            set
            {
                _posSet = value;
            }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Verify the validity of the object.
        /// </summary>
        public void VerifySetting()
        {
            if ((_scope & XmlScriptValidationScope.Pronunciation) == XmlScriptValidationScope.Pronunciation &&
                _phoneSet == null)
            {
                throw new ArgumentException("A non-null phoneSet is expected when XmlScriptValidationScope.Pronunciation is designated.");
            }

            if ((_scope & XmlScriptValidationScope.POS) == XmlScriptValidationScope.POS &&
                _posSet == null)
            {
                throw new ArgumentException("A non-null posSet is expected when XmlScriptValidationScope.POS is designated.");
            }
        }

        #endregion
    }
}