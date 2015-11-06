//----------------------------------------------------------------------------
// <copyright file="ScriptState.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements script state class
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using System.Xml;

    /// <summary>
    /// Script state class.
    /// </summary>
    public class ScriptState : ScriptAcousticsHolder
    {
        #region Fields

        private ScriptPhone _phone;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets The phone this state belongs to.
        /// </summary>
        public ScriptPhone Phone
        {
            get
            {
                return _phone;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _phone = value;
            }
        }

        #endregion

        #region public methods

        /// <summary>
        /// Write state to xml.
        /// </summary>
        /// <param name="writer">Xml writer.</param>
        public void WriteToXml(XmlWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            // Write <state> node and its attributes.
            writer.WriteStartElement("state");

            if (HasAcousticsValue)
            {
                Acoustics.WriteToXml(writer);
            }

            writer.WriteEndElement();
        }

        #endregion
    }
}