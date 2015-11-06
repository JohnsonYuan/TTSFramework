//----------------------------------------------------------------------------
// <copyright file="TobiLabel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements ToBI label class
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System.Text.RegularExpressions;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Enum of UPL lattice prosody type.
    /// </summary>
    public enum ProsodyType
    {
        /// <summary>
        /// Undefined type.
        /// </summary>
        Undefined,

        /// <summary>
        /// Prosody break.
        /// </summary>
        Break,

        /// <summary>
        /// Prosody boundary tone.
        /// </summary>
        BoundaryTone,

        /// <summary>
        /// Prosody pitch accent.
        /// </summary>
        PitchAccent,

        /// <summary>
        /// Prosody Emphasis.
        /// </summary>
        Emphasis
    }

    /// <summary>
    /// ToBI label class.
    /// </summary>
    public class TobiLabel
    {
        /// <summary>
        /// Initializes a new instance of the TobiLabel class.
        /// </summary>
        /// <param name="label">ToBI label.</param>
        public TobiLabel(string label)
        {
            Set(label);
        }

        /// <summary>
        /// Gets or sets ToBI label.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Gets or sets uncertain suffix.
        /// </summary>
        public string UncertainSuffix { get; set; }

        /// <summary>
        /// Gets a value indicating whether is uncertain property.
        /// </summary>
        public bool Uncertain
        {
            get
            {
                return !string.IsNullOrEmpty(UncertainSuffix);
            }
        }

        /// <summary>
        /// Gets uncertain char at the end of label.
        /// </summary>
        private static char UncertainChar
        {
            get
            {
                return Delimitor.ColonChar;
            }
        }

        /// <summary>
        /// Gets break uncertain char at the end of label.
        /// </summary>
        private static char BreakUncertainChar
        {
            get
            {
                return Delimitor.DashChar;
            }
        }

        /// <summary>
        /// Create ToBI label.
        /// </summary>
        /// <param name="label">ToBI label.</param>
        /// <returns>Created ToBI label.</returns>
        public static TobiLabel Create(string label)
        {
            TobiLabel tobiLabel = null;
            if (!string.IsNullOrEmpty(label))
            {
                tobiLabel = new TobiLabel(label);
            }

            return tobiLabel;
        }

        /// <summary>
        /// Set ToBI label.
        /// </summary>
        /// <param name="label">ToBI label.</param>
        public void Set(string label)
        {
            const string UncertainBreakIndexPattern = @"^[0-9]+\-$";
            Helper.ThrowIfNull(label);
            label = label.Trim();
            Label = label;
            if (label[label.Length - 1] == UncertainChar ||
                Regex.Match(label, UncertainBreakIndexPattern).Success)
            {
                UncertainSuffix = label[label.Length - 1].ToString();
                Label = label.Substring(0, label.Length - 1);
            }
        }

        /// <summary>
        /// Convert ToBI label to string.
        /// </summary>
        /// <returns>String value of the ToBI label.</returns>
        public override string ToString()
        {
            string label = Label;
            if (!string.IsNullOrEmpty(UncertainSuffix))
            {
                label += UncertainSuffix;
            }

            return label;
        }

        #region ICloneable Members

        /// <summary>
        /// Clone the TobiLabel.
        /// </summary>
        /// <returns>Cloned TobiLabel.</returns>
        public TobiLabel Clone()
        {
            return new TobiLabel(ToString());
        }

        #endregion
    }
}