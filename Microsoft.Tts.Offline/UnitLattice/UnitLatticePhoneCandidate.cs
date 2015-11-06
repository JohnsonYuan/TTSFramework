//----------------------------------------------------------------------------
// <copyright file="UnitLatticePhoneCandidate.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This class represents the phone candidates of a phone in unit lattice file
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Xml;
    using System.Xml.Schema;

    /// <summary>
    /// Unit lattice phone candidate class.
    /// </summary>
    public class UnitLatticePhoneCandidate
    {
        #region Constructor
        /// <summary>
        /// Initializes a new instance of the UnitLatticePhoneCandidate class.
        /// </summary>
        public UnitLatticePhoneCandidate()
        {
            this.RouteCost = float.MinValue;
            this.F0Cost = float.MinValue;
            this.SpectCost = float.MinValue;
            this.PrecedeNodeIdx = int.MinValue;
            this.ConCost = float.MinValue;
            this.IsContinue = false;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets candidate phone label text.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets candidate index.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Gets or sets sentence id where the candidate phone is from.
        /// </summary>
        public string SentenceId { get; set; }

        /// <summary>
        /// Gets or sets non-silence phone index of the candidate in recording sentence.
        /// </summary>
        public int IndexNonSilence { get; set; }

        /// <summary>
        /// Gets or sets of the startFrame index in the recording wave.
        /// </summary>
        public int StartFrameIndex { get; set; }

        /// <summary>
        /// Gets or sets of the Frame Length in the recording wave.
        /// </summary>
        public int FrameLength { get; set; }

        /// <summary>
        /// Gets or sets the cost of the whole path ended with this instance.
        /// </summary>
        public float RouteCost { get; set; }

        /// <summary>
        /// Gets or sets target cost of the candidate.
        /// </summary>
        public float TargetCost { get; set; }

        /// <summary>
        /// Gets or sets the F0 cost.
        /// </summary>
        public float F0Cost { get; set; }

        /// <summary>
        /// Gets or sets the Lsf cost.
        /// </summary>
        public float SpectCost { get; set; }

        /// <summary>
        /// Gets or sets of the concatenation cost of the previous node.
        /// </summary>
        public float ConCost { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the canidate is continous with the previous node.
        /// </summary>
        public bool IsContinue { get; set; }

        /// <summary>
        /// Gets or sets start time of the candidate phone in raw recording (time unit is ms).
        /// </summary>
        public int StartTime { get; set; }

        /// <summary>
        /// Gets or sets candidate phone length (forced alignment) in raw recording (time unit is ms).
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// Gets or sets the previous node which giving the least path cost to this node.
        /// </summary>
        public int PrecedeNodeIdx { get; set; }

        /// <summary>
        /// Gets or sets the log infomation.
        /// </summary>
        public string Log { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the unit candidate is from NUS inventory.
        /// </summary>
        public bool IsNusCandidate { get; set; }

        /// <summary>
        /// Gets or sets of Wave unit index.
        /// </summary>
        public int WuiIndex { get; set; }
        
        #endregion

        #region public static methods

        /// <summary>
        /// Checks whether two UnitLatticePhoneCandidate are equal.
        /// </summary>
        /// <param name="refCandidate">Reference UnitLatticePhoneCandidate.</param>
        /// <param name="trgCandidate">Target UnitLatticePhoneCandidate.</param>
        /// <returns>Bool that represents equal or not.</returns>
        public static bool Equals(UnitLatticePhoneCandidate refCandidate, UnitLatticePhoneCandidate trgCandidate)
        {
            Debug.Assert(refCandidate != null, "refCandidate is null");
            Debug.Assert(trgCandidate != null, "trgCandidate is null");
            Debug.Assert(string.Equals(refCandidate.Text, trgCandidate.Text), "The phone text of refCandidate and trgCandidate are not equal");

            bool isEqual = false;

            // The two candidates are equal if they have the same recording sentence id and index of none silence phone
            if (string.Equals(refCandidate.SentenceId, trgCandidate.SentenceId) &&
                refCandidate.IndexNonSilence == trgCandidate.IndexNonSilence)
            {
                isEqual = true;
            }

            return isEqual;
        }

        #endregion

        #region public operations

        /// <summary>
        /// Load one phone candidate from the xmltextreader.
        /// </summary>
        /// <param name="reader">XmlTextReader to read XML file.</param>
        /// <param name="contentController">Content controller.</param>
        public void Load(XmlTextReader reader, object contentController)
        {
            Debug.Assert(reader != null, "XmlTextReader is null");

            // Get phone candidate attributes
            this.Text = reader.GetAttribute("txt");
            this.Index = int.Parse(reader.GetAttribute("idx"));
            this.SentenceId = reader.GetAttribute("sentId");
            this.IndexNonSilence = int.Parse(reader.GetAttribute("idxNonSil"));
            this.StartFrameIndex = int.Parse(reader.GetAttribute("startFrame"));
            this.FrameLength = int.Parse(reader.GetAttribute("frameLen"));
            this.RouteCost = float.Parse(reader.GetAttribute("routeCost"));
            this.TargetCost = float.Parse(reader.GetAttribute("tgtCost"));
            this.F0Cost = float.Parse(reader.GetAttribute("f0Cost"));
            this.SpectCost = float.Parse(reader.GetAttribute("spectCost"));
            this.ConCost = float.Parse(reader.GetAttribute("conCost"));
            this.IsContinue = bool.Parse(reader.GetAttribute("continue"));
            this.StartTime = int.Parse(reader.GetAttribute("startTime"));
            this.Length = int.Parse(reader.GetAttribute("length"));
            this.PrecedeNodeIdx = int.Parse(reader.GetAttribute("preIdx"));
            this.Log = reader.GetAttribute("log");
        }

        /// <summary>
        /// Writes the phone candidate to xml writer.
        /// </summary>
        /// <param name="writer">XmlWriter to save XML file.</param>
        /// <param name="contentController">Content controller.</param>
        public void WriteToXml(XmlWriter writer, object contentController)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            // write <cand> node and its attributes
            writer.WriteStartElement("cand");
            writer.WriteAttributeString("txt", this.Text);
            writer.WriteAttributeString("idx", this.Index.ToString());
            writer.WriteAttributeString("sentId", this.SentenceId);
            writer.WriteAttributeString("idxNonSil", this.IndexNonSilence.ToString());
            writer.WriteAttributeString("startFrame", this.StartFrameIndex.ToString());
            writer.WriteAttributeString("frameLen", this.FrameLength.ToString());
            writer.WriteAttributeString("routeCost", this.RouteCost.ToString("F3"));
            writer.WriteAttributeString("tgtCost", this.TargetCost.ToString("F3"));
            writer.WriteAttributeString("f0Cost", this.F0Cost.ToString("F3"));
            writer.WriteAttributeString("spectCost", this.SpectCost.ToString("F3"));
            writer.WriteAttributeString("conCost", this.ConCost.ToString("F3"));
            writer.WriteAttributeString("continue", this.IsContinue.ToString());
            writer.WriteAttributeString("startTime", this.StartTime.ToString());
            writer.WriteAttributeString("length", this.Length.ToString());
            writer.WriteAttributeString("preIdx", this.PrecedeNodeIdx.ToString());
            writer.WriteAttributeString("log", this.Log);

            writer.WriteEndElement();
        }

        #endregion
    }
}