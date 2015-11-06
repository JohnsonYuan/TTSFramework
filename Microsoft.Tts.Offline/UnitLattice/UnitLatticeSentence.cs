//----------------------------------------------------------------------------
// <copyright file="UnitLatticeSentence.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This class represents the sentence in a unit lattice file
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
    /// Unit lattice sentence class.
    /// </summary>
    public class UnitLatticeSentence : IComparable
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the UnitLatticeSentence class.
        /// </summary>
        public UnitLatticeSentence()
        {
            this.Phones = new Collection<UnitLatticePhone>();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets sentence id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets sentence text.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets phones in the sentence.
        /// </summary>
        public Collection<UnitLatticePhone> Phones { get; set; }

        #endregion

        #region public operations

        /// <summary>
        /// Load one sentence from the xmltextreader.
        /// </summary>
        /// <param name="reader">XmlTextReader to read XML file.</param>
        /// <param name="contentController">Content controller.</param>
        public void Load(XmlTextReader reader, object contentController)
        {
            Debug.Assert(reader != null, "XmlTextReader is null");

            // Get sentence id, text
            this.Id = reader.GetAttribute("id");
            this.Text = reader.GetAttribute("txt");

            // get the phones
            if (!reader.IsEmptyElement)
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.Name == "phone")
                    {
                        UnitLatticePhone phone = new UnitLatticePhone();
                        phone.Load(reader, contentController);

                        this.Phones.Add(phone);
                    }
                    else if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "sentence")
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Writes the sentence to xml writer.
        /// </summary>
        /// <param name="writer">XmlWriter to save XML file.</param>
        /// <param name="contentController">Content controller.</param>
        public void WriteToXml(XmlWriter writer, object contentController)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            // write <sentence> node and its attributes
            writer.WriteStartElement("sentence");
            writer.WriteAttributeString("id", this.Id);
            writer.WriteAttributeString("txt", this.Text);

            // write phones
            foreach (UnitLatticePhone phone in this.Phones)
            {
                phone.WriteToXml(writer, contentController);
            }

            writer.WriteEndElement();
        }

        /// <summary>
        /// Implement the IComparable interface.
        /// </summary>
        /// <param name="obj">Object.</param>
        /// <returns>Positive when current is largeer, zero when equal, negative when current is smaller.</returns>
        public int CompareTo(object obj)
        {
            UnitLatticeSentence sentence = obj as UnitLatticeSentence;
            return this.Id.CompareTo(sentence.Id);
        }

        #endregion
    }
}