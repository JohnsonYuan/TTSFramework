//----------------------------------------------------------------------------
// <copyright file="TtsTobiBoundaryToneSet.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements TTS ToBI Boundary Tone set
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Text;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Class of tts tobi Boundary tone set. 
    /// Tobi labels are enabled for ja-JP now, so a ToBI set must be defined to support the label conversion.
    /// It is designed as a common set to cover several other languages: en-US, French, and German. 
    /// Note that the set is case-sensitive.
    /// </summary>
    public class TtsTobiBoundaryToneSet
    {
        #region Fields

        private Dictionary<string, uint> _tobiBoundaryToneSet = new Dictionary<string, uint>();
        private Dictionary<uint, string> _tobiBoundaryToneIdSet = new Dictionary<uint, string>();
        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TtsTobiBoundaryToneSet"/> class.
        /// </summary>
        public TtsTobiBoundaryToneSet()
        {
            InitSet();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets Boundary tone Set.
        /// </summary>
        public Dictionary<string, uint> Items
        {
            get { return _tobiBoundaryToneSet; }
        }

        /// <summary>
        /// Gets Boundary tone Id set.
        /// </summary>
        public Dictionary<uint, string> IdItems
        {
            get { return _tobiBoundaryToneIdSet; }
        }

        #endregion

        #region method

        /// <summary>
        /// Initialize with boundary tone set.
        /// </summary>
        private void InitSet()
        {
            _tobiBoundaryToneSet.Add("NONE", (uint)TtsTobiBoundary.NoBoundaryTone);
            _tobiBoundaryToneSet.Add("L-", (uint)TtsTobiBoundary.LMinus);
            _tobiBoundaryToneSet.Add("H-", (uint)TtsTobiBoundary.HMinus);
            _tobiBoundaryToneSet.Add("L-L%", (uint)TtsTobiBoundary.LMinusLPerc);
            _tobiBoundaryToneSet.Add("L-H%", (uint)TtsTobiBoundary.LMinusHPerc);
            _tobiBoundaryToneSet.Add("H-H%", (uint)TtsTobiBoundary.HMinusHPerc);
            _tobiBoundaryToneSet.Add("H-L%", (uint)TtsTobiBoundary.HMinusLPerc);
            _tobiBoundaryToneSet.Add("S-", (uint)TtsTobiBoundary.SMinus);

            _tobiBoundaryToneIdSet.Add((uint)TtsTobiBoundary.NoBoundaryTone, "NONE");
            _tobiBoundaryToneIdSet.Add((uint)TtsTobiBoundary.LMinus, "L-");
            _tobiBoundaryToneIdSet.Add((uint)TtsTobiBoundary.HMinus, "H-");
            _tobiBoundaryToneIdSet.Add((uint)TtsTobiBoundary.LMinusLPerc, "L-L%");
            _tobiBoundaryToneIdSet.Add((uint)TtsTobiBoundary.LMinusHPerc, "L-H%");
            _tobiBoundaryToneIdSet.Add((uint)TtsTobiBoundary.HMinusHPerc, "H-H%");
            _tobiBoundaryToneIdSet.Add((uint)TtsTobiBoundary.HMinusLPerc, "H-L%");
            _tobiBoundaryToneIdSet.Add((uint)TtsTobiBoundary.SMinus, "S-");
        }

        #endregion
    }
}