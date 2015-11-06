//----------------------------------------------------------------------------
// <copyright file="TTSTobiAccentSet.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements TTS POS set
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Class of tts tobi accent set. 
    /// Tobi labels are enabled for ja-JP now, so a ToBI set must be defined to support the label conversion.
    /// It is designed as a common set to cover several other languages: en-US, French, and German. 
    /// Note that the set is case-sensitive.
    /// </summary>
    public class TtsTobiAccentSet
    {
        #region Fields

        private Dictionary<string, uint> _tobiaccentSet = new Dictionary<string, uint>();
        private Dictionary<uint, string> _tobiaccentIdSet = null;
        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TtsTobiAccentSet"/> class.
        /// </summary>
        public TtsTobiAccentSet()
        {
            InitSet();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets Pos Set.
        /// </summary>
        public Dictionary<string, uint> Items
        {
            get { return _tobiaccentSet; }
        }

        /// <summary>
        /// Gets POS Id set.
        /// </summary>
        public Dictionary<uint, string> IdItems
        {
            get { return _tobiaccentIdSet; }
        }

        #endregion

        #region method

        /// <summary>
        /// Initialize with common accent set.
        /// </summary>
        private void InitSet()
        {
            _tobiaccentSet.Add("NONE", (uint)TtsTobiAccent.NoAccent);
            _tobiaccentSet.Add("H*", (uint)TtsTobiAccent.HighStar);
            _tobiaccentSet.Add("!H*", (uint)TtsTobiAccent.DownHighStar);
            _tobiaccentSet.Add("L+H*", (uint)TtsTobiAccent.LowHighStar);
            _tobiaccentSet.Add("L+!H*", (uint)TtsTobiAccent.LowDownHighStar);
            _tobiaccentSet.Add("L*", (uint)TtsTobiAccent.LowStar);
            _tobiaccentSet.Add("L*+H", (uint)TtsTobiAccent.LowStarHigh);
            _tobiaccentSet.Add("R*", (uint)TtsTobiAccent.RisingStar);
            _tobiaccentSet.Add("H*+L*", (uint)TtsTobiAccent.HighStarLowStar);

            _tobiaccentIdSet = _tobiaccentSet.ToDictionary(p => p.Value, p => p.Key);
        }

        #endregion
    }
}