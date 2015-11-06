//----------------------------------------------------------------------------
// <copyright file="UnitLatticePhone.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This class represents the phones in a sentence in unit lattice file
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Unit lattice phone class.
    /// </summary>
    public class UnitLatticePhone
    {
        #region Constructor
        /// <summary>
        /// Initializes a new instance of the UnitLatticePhone class.
        /// </summary>
        public UnitLatticePhone()
        {
            this.Candidates = new Collection<UnitLatticePhoneCandidate>();
            this.CandidateGroupId = int.MinValue;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets phone label text.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets triphone labels.
        /// </summary>
        public string Triphone { get; set; }

        /// <summary>
        /// Gets or sets candidate group id.
        /// </summary>
        public int CandidateGroupId { get; set; }

        /// <summary>
        /// Gets or sets log info.
        /// </summary>
        public string Log { get; set; }

        /// <summary>
        /// Gets or sets of the phone pointer.
        /// </summary>
        public IntPtr PhonePtr { get; set; }

        /// <summary>
        /// Gets or sets phone candiates.
        /// </summary>
        public Collection<UnitLatticePhoneCandidate> Candidates { get; set; }

        #endregion

        #region public static methods

        /// <summary>
        /// Gets the number of same candidates between two phones.
        /// </summary>
        /// <param name="reference">A phone of reference build.</param>
        /// <param name="target">A phone of target build.</param>
        /// <returns>The number of same candidates.</returns>
        public static int GetSameCandidateCount(UnitLatticePhone reference, UnitLatticePhone target)
        {
            Debug.Assert(reference != null, "reference is null");
            Debug.Assert(target != null, "target is null");
            Debug.Assert(string.Equals(reference.Text, target.Text), "The phone text of tgtPhone and tgtPhone are not equal");

            int sameNum = 0;

            for (int i = 0; i < reference.Candidates.Count; i++)
            {
                for (int j = 0; j < target.Candidates.Count; j++)
                {
                    if (UnitLatticePhoneCandidate.Equals(reference.Candidates[i], target.Candidates[j]))
                    {
                        sameNum++;
                        break;
                    }
                }
            }

            return sameNum;
        }

        #endregion

        #region public operations

        /// <summary>
        /// Load one phone from the xmltextreader.
        /// </summary>
        /// <param name="reader">XmlTextReader to read XML file.</param>
        /// <param name="contentController">Content controller.</param>
        public void Load(XmlTextReader reader, object contentController)
        {
            Debug.Assert(reader != null, "XmlTextReader is null");

            // Get phone text, triphone, group, candidateGroupId
            this.Text = reader.GetAttribute("txt");
            this.Triphone = reader.GetAttribute("triphone");
            this.Log = reader.GetAttribute("log");

            if (!string.IsNullOrEmpty(reader.GetAttribute("candidateGroupId")))
            {
                this.CandidateGroupId = int.Parse(reader.GetAttribute("candidateGroupId"));
            }

            // get the phone candidates
            if (!reader.IsEmptyElement)
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.Name == "cand")
                    {
                        UnitLatticePhoneCandidate candidate = new UnitLatticePhoneCandidate();
                        candidate.Load(reader, contentController);

                        this.Candidates.Add(candidate);
                    }
                    else if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "phone")
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Writes the phone to xml writer.
        /// </summary>
        /// <param name="writer">XmlWriter to save XML file.</param>
        /// <param name="contentController">Content controller.</param>
        public void WriteToXml(XmlWriter writer, object contentController)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            DoStatistic();

            // write <phone> node and its attributes
            writer.WriteStartElement("phone");
            writer.WriteAttributeString("txt", this.Text);
            writer.WriteAttributeString("triphone", this.Triphone);
            writer.WriteAttributeString("candidateNum", this.Candidates.Count.ToString());

            if (this.CandidateGroupId >= 0)
            {
                writer.WriteAttributeString("candidateGroupId", this.CandidateGroupId.ToString());
            }

            writer.WriteAttributeString("log", this.Log);

            // write phone candidates
            foreach (UnitLatticePhoneCandidate candidate in this.Candidates)
            {
                candidate.WriteToXml(writer, contentController);
            }

            writer.WriteEndElement();
        }

        #endregion

        #region private operation
        /// <summary>
        /// Do some statistic for all condidates, and save result into log info.
        /// </summary>
        private void DoStatistic()
        {
            // statistic, mean value of f0Cost, spectCost, tgtCost and conCost
            Log += Helper.NeutralFormat("avgF0Cost:{0:F3} avgSpectCost:{1:F3} avgTgtCost:{2:F3} avgConCost:{3:F3}", 
                Candidates.Average(c => c.F0Cost), Candidates.Average(c => c.SpectCost),
                Candidates.Average(c => c.TargetCost), Candidates.Average(c => c.ConCost));
        }
        #endregion
    }
}