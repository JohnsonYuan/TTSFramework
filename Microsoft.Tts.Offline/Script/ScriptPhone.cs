//----------------------------------------------------------------------------
// <copyright file="ScriptPhone.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements script phone class
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Xml;
    using Microsoft.Tts.Offline.Common;

    /// <summary>
    /// Script phone class.
    /// </summary>
    public class ScriptPhone : ScriptAcousticsHolder
    {
        #region Fields

        private const string DefaultTone = "0";
        private string _name;
        private string _tone;
        private string _sentenceId = string.Empty;
        private int _unitIndex = -1;
        private bool _valid = true;
        private TtsStress _stress = TtsStress.None;
        private ScriptSyllable _syllable;
        private Collection<ScriptState> _states = new Collection<ScriptState>();

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptPhone"/> class.
        /// </summary>
        /// <param name="name">The phone name.</param>
        public ScriptPhone(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            Name = name;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Phone name.
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _name = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this is a valid phone.
        /// </summary>
        public bool Valid
        {
            get
            {
                return _valid;
            }

            set
            {
                _valid = value;
            }
        }

        /// <summary>
        /// Gets or sets Phone's tone.
        /// </summary>
        public string Tone
        {
            get
            {
                return _tone;
            }

            set
            {
                if (string.IsNullOrEmpty("value"))
                {
                    throw new ArgumentNullException("value");
                }

                _tone = value;
            }
        }

        /// <summary>
        /// Gets or sets the source sentence id of this phone.
        /// </summary>
        public string SentenceId
        {
            get
            {
                return _sentenceId;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _sentenceId = value;
            }
        }

        /// <summary>
        /// Gets or sets the unit index.
        /// </summary>
        public int UnitIndex
        {
            get
            {
                return _unitIndex;
            }

            set
            {
                if (value < 0)
                {
                    throw new Exception("The unit index should not be less than 0");
                }

                _unitIndex = value;
            }
        }

        /// <summary>
        /// Gets or sets TtsStress.
        /// </summary>
        public TtsStress Stress
        {
            get { return _stress; }
            set { _stress = value; }
        }

        /// <summary>
        /// Gets or sets The syllable this phone belongs to.
        /// </summary>
        public ScriptSyllable Syllable
        {
            get
            {
                return _syllable;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _syllable = value;
            }
        }

        /// <summary>
        /// Gets The states this phone has.
        /// </summary>
        public Collection<ScriptState> States
        {
            get
            {
                return _states;
            }
        }

        #endregion

        #region public methods

        /// <summary>
        /// Check whether the element is valid or not.
        /// </summary>
        /// <param name="itemID">ID of the script item.</param>
        /// <param name="nodePath">Path of the node.</param>
        /// <param name="scope">The validation setting.</param>
        /// <param name="errors">Contains errors found at present.</param>
        /// <returns>Valid or not. Always true if no validation is performed.</returns>
        public bool IsValid(string itemID, string nodePath, XmlScriptValidationScope scope, ErrorSet errors)
        {
            bool valid = true;

            if (HasAcousticsValue)
            {
                string path = string.Format(CultureInfo.InvariantCulture, "{0}.Acoustics", nodePath);
                if (!Acoustics.IsValid(itemID, path, scope, errors))
                {
                    valid = false;
                }
            }

            return valid;
        }

        /// <summary>
        /// Write phone to xml.
        /// </summary>
        /// <param name="writer">XmlWriter.</param>
        public void WriteToXml(XmlWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            // write <ph> node and its attributes
            writer.WriteStartElement("ph");

            writer.WriteAttributeString("v", Name);

            if (!Valid)
            {
                writer.WriteAttributeString("valid", "false");
            }

            if (!string.IsNullOrEmpty(Tone) && !string.Equals(Tone, DefaultTone))
            {
                writer.WriteAttributeString("tone", Tone);
            }

            string stressName = ScriptSyllable.StressToString(Stress);
            if (!string.IsNullOrEmpty(stressName))
            {
                writer.WriteAttributeString("stress", stressName);
            }

            if (!string.IsNullOrEmpty(SentenceId))
            {
                writer.WriteAttributeString("sentenceID", SentenceId);
            }

            if (UnitIndex >= 0)
            {
                writer.WriteAttributeString("unitIndex", UnitIndex.ToString(CultureInfo.InvariantCulture));
            }

            if (States.Count != 0)
            {
                writer.WriteStartElement("states");
                foreach (ScriptState state in States)
                {
                    state.WriteToXml(writer);
                }

                writer.WriteEndElement();
            }

            if (HasAcousticsValue)
            {
                Acoustics.WriteToXml(writer);
            }

            writer.WriteEndElement();
        }

        #endregion
    }
}