//----------------------------------------------------------------------------
// <copyright file="ScriptNamedEntity.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements script named entity
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using Microsoft.Tts.Offline.Utility;
    using ScriptReviewer;

    /// <summary>
    /// Script named entity.
    /// </summary>
    public class ScriptNamedEntity
    {
        /// <summary>
        /// The default POS string for field PosString.
        /// </summary>
        public static readonly string DefaultEmptyPosString;

        /// <summary>
        /// The default POS string for field PosString.
        /// </summary>
        public static readonly string DefaultEntityPosString;

        /// <summary>
        /// Initializes static members of the ScriptNamedEntity class.
        /// </summary>
        static ScriptNamedEntity()
        {
            DefaultEmptyPosString = PartOfSpeech.Unknown.ToString().ToLowerInvariant();
            DefaultEntityPosString = PartOfSpeech.Noun.ToString().ToLowerInvariant();
        }

        /// <summary>
        /// Initializes a new instance of the ScriptNamedEntity class.
        /// </summary>
        public ScriptNamedEntity()
        {
            PosString = DefaultEmptyPosString;
        }

        /// <summary>
        /// Gets the sentence ID of the script named entity.
        /// </summary>
        [ListItem(Index = 0, Name = "Sentence ID", Width = 150)]
        public string SentenceId
        {
            get
            {
                string sentenceId = string.Empty;
                if (Start != null)
                {
                    ScriptSentence sentence = Start.Sentence;
                    Debug.Assert(sentence != null, "ScriptSentence of start word should not be null.");
                    Debug.Assert(sentence.ScriptItem != null, "ScriptItem of start word should not be null.");
                    sentenceId = sentence.ScriptItem.GetSentenceId(sentence);
                }

                return sentenceId;
            }
        }

        /// <summary>
        /// Gets or sets the text string for this named entity.
        /// </summary>
        [ListItem(Index = 1, Name = "Text", Width = 150)]
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the named entity type of this instance.
        /// </summary>
        [ListItem(Index = 2, Name = "Domain Name", Width = 122)]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the part of speech string.
        /// </summary>
        [ListItem(Index = 3, Name = "POS", Width = 122)]
        public string PosString { get; set; }

        /// <summary>
        /// Gets the start index of the first word of this named entity in the grapheme-contained word list.
        /// </summary>
        [ListItem(Index = 4, Name = "Start index", Width = 122)]
        public int StartIndex
        {
            get
            {
                int index = -1;
                if (Start != null)
                {
                    index = Start.Sentence.TextWords.IndexOf(Start);
                }

                return index;
            }
        }

        /// <summary>
        /// Gets the end index of the first word of this named entity in the grapheme-contained word list.
        /// </summary>
        [ListItem(Index = 5, Name = "End index", Width = 122)]
        public int EndIndex
        {
            get
            {
                int index = -1;
                if (End != null)
                {
                    index = End.Sentence.TextWords.IndexOf(End);
                }

                return index;
            }
        }

        /// <summary>
        /// Gets the number of words of this named entity.
        /// </summary>
        [ListItem(Index = 6, Name = "Word count", Width = 122)]
        public int Count
        {
            get { return EndIndex - StartIndex + 1; }
        }

        /// <summary>
        /// Gets or sets the first word of this named entity.
        /// </summary>
        public ScriptWord Start { get; set; }

        /// <summary>
        /// Gets or sets the end word of this named entity.
        /// </summary>
        public ScriptWord End { get; set; }

        /// <summary>
        /// Gets the word collection scoped by this instance.
        /// </summary>
        public IEnumerable<ScriptWord> Words
        {
            get
            {
                int begin = Start.Sentence.Words.IndexOf(Start);
                int end = Start.Sentence.Words.IndexOf(End);
                for (int i = begin; i <= end; i++)
                {
                    yield return Start.Sentence.Words[i];
                }
            }
        }

        /// <summary>
        /// Gets the hash code for this instance.
        /// </summary>
        /// <returns>Hash code returned.</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Tests whether current instance equals with the other object.
        /// </summary>
        /// <param name="obj">The other object to test equal with.</param>
        /// <returns>True if equals, false otherwise.</returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            ScriptNamedEntity other = obj as ScriptNamedEntity;
            if (other == null)
            {
                return false;
            }

            return (Start == other.Start && End == other.End) &&
                Type == other.Type &&
                PosString == other.PosString;
        }

        /// <summary>
        /// Writes named entity to xml.
        /// </summary>
        /// <param name="writer">XmlWriter instance to performance writing.</param>
        /// <param name="scriptContentController">XmlScriptFile.ContentControler.</param>
        public void WriteToXml(XmlWriter writer, XmlScriptFile.ContentControler scriptContentController)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            if (scriptContentController == null)
            {
                throw new ArgumentNullException("scriptContentController");
            }

            if (Start.Sentence != End.Sentence)
            {
                throw new InvalidDataException(Helper.NeutralFormat("Named entity should not be cross sentences boundary."));
            }

            writer.WriteStartElement("ne");

            Validate();
            Text = Start.Sentence.TextWords.Skip(StartIndex).Take(EndIndex - StartIndex + 1)
                .Select(w => w.Grapheme).Concatenate(" ");

            if (string.IsNullOrEmpty(Text))
            {
                throw new InvalidDataException(Helper.NeutralFormat("Does not support empty-grapheme named entity."));
            }

            if (string.IsNullOrEmpty(Type))
            {
                throw new InvalidDataException(Helper.NeutralFormat("Does not support null typed named entity."));
            }

            writer.WriteAttributeString("s", StartIndex.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("e", EndIndex.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("type", Type);
            writer.WriteAttributeString("v", Text);

            if (!string.IsNullOrEmpty(PosString) &&
                !PosString.Equals(DefaultEmptyPosString, StringComparison.OrdinalIgnoreCase))
            {
                writer.WriteAttributeString("pos", PosString);
            }

            writer.WriteEndElement();
        }

        /// <summary>
        /// Validates the state of this instance, for example the reference.
        /// </summary>
        public void Validate()
        {
            IList<ScriptWord> words = Start.Sentence.TextWords;
            if (StartIndex > EndIndex)
            {
                throw new InvalidDataException(Helper.NeutralFormat(
                    "The start [{0}] index should not be bigger than the end [{1}] index of named entity [{2}].",
                    StartIndex, EndIndex, Text));
            }

            if (words.Count <= StartIndex || words.Count <= EndIndex)
            {
                throw new InvalidDataException(Helper.NeutralFormat(
                    "The start [{0}] or end [{1}] indexes of named entity [{2}] is beyond all grapheme-words.",
                    StartIndex, EndIndex, Text));
            }
        }

        /// <summary>
        /// Converts to the string represent of this instance.
        /// </summary>
        /// <returns>The string represent of this instance.</returns>
        public override string ToString()
        {
            return Words.Select(w => w.Grapheme).Concatenate(" ");
        }

        /// <summary>
        /// Named entity type values static class.
        /// </summary>
        public class NamedEntityTypeValues
        {
            /// <summary>
            /// Phrase named entity.
            /// </summary>
            public const string Phrase = "sp:phrase";
        }
    }
}