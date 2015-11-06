//----------------------------------------------------------------------------
// <copyright file="ScriptAcoustics.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements script acoustics class
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using Microsoft.Tts.Offline.Common;

    /// <summary>
    /// Definition of interval value mode.
    /// </summary>
    public enum IntervalValueMode
    {
        /// <summary>
        /// Absolute value.
        /// </summary>
        Absolute = 0,

        /// <summary>
        /// Relative value.
        /// </summary>
        Relative,
    }

    /// <summary>
    /// Definition of script acoustic chunk encoding type.
    /// </summary>
    public enum ScriptAcousticChunkEncoding
    {
        /// <summary>
        /// Unknown.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Text.
        /// </summary>
        Text,

        /// <summary>
        /// Base64Binary.
        /// </summary>
        Base64Binary,

        /// <summary>
        /// HexBinary.
        /// </summary>
        HexBinary,
    }

    /// <summary>
    /// Definition of script acoustic chunk type.
    /// </summary>
    public enum ScriptUvSegType
    {
        /// <summary>
        /// Unknown.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Voiced.
        /// </summary>
        Voiced,

        /// <summary>
        /// Unvoiced.
        /// </summary>
        Unvoiced,

        /// <summary>
        /// Silence.
        /// </summary>
        Silence,

        /// <summary>
        /// Mixed.
        /// </summary>
        Mixed
    }

    /// <summary>
    /// Interface for script element.
    /// </summary>
    public interface IScriptElement
    {
        /// <summary>
        /// Generate the IScriptElement object from the xml doc indicated by reader.
        /// </summary>
        /// <param name="reader">Xml text reader.</param>
        void ParseFromXml(XmlTextReader reader);

        /// <summary>
        /// Check whether the element is valid or not.
        /// </summary>
        /// <param name="itemID">ID of the script item.</param>
        /// <param name="nodePath">Path of the node.</param>
        /// <param name="scope">The validation scope.</param>
        /// <param name="errors">Contains errors found at present.</param>
        /// <returns>Valid or not. Always true if no validation is performed.</returns>
        bool IsValid(string itemID, string nodePath, XmlScriptValidationScope scope, ErrorSet errors);

        /// <summary>
        /// Write the IScriptElement object to xml script.
        /// </summary>
        /// <param name="writer">Xml writer.</param>
        void WriteToXml(XmlWriter writer);
    }

    /// <summary>
    /// Time interval class.
    /// </summary>
    public class TimeInterval
    {
        #region Private fields

        /// <summary>
        /// Const element name for begin in absolute mode.
        /// </summary>
        private const string AbsoluteBegin = "begin";

        /// <summary>
        /// Const element name for end in absolute mode.
        /// </summary>
        private const string AbsoluteEnd = "end";

        /// <summary>
        /// Const element name for begin in relative mode.
        /// </summary>
        private const string RelativeBegin = "rbegin";

        /// <summary>
        /// Const element name for end in relative mode.
        /// </summary>
        private const string RelativeEnd = "rend";

        /// <summary>
        /// Interval value mode.
        /// </summary>
        private IntervalValueMode _valueMode;

        /// <summary>
        /// Beginning of the interval, in milliseconds.
        /// </summary>
        private int _begin;

        /// <summary>
        /// End of the interval, in milliseconds.
        /// </summary>
        private int _end;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeInterval"/> class.
        /// </summary>
        /// <param name="valueMode">Value mode.</param>
        public TimeInterval(IntervalValueMode valueMode)
        {
            _valueMode = valueMode;
            _begin = 0;
            _end = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeInterval"/> class.
        /// </summary>
        /// <param name="valueMode">Value mode.</param>
        /// <param name="begin">Beginning of the interval.</param>
        /// <param name="end">End of the interval.</param>
        public TimeInterval(IntervalValueMode valueMode, int begin, int end)
        {
            if (begin < 0)
            {
                throw new ArgumentOutOfRangeException("begin", "can't be negative");
            }
            else if (end <= 0)
            {
                throw new ArgumentOutOfRangeException("end", "can't be zero or negative");
            }

            _valueMode = valueMode;
            _begin = begin;
            _end = end;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Beginning of the interval, in milliseconds.
        /// </summary>
        public int Begin
        {
            get
            {
                return _begin;
            }

            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value", "can't be negative");
                }

                _begin = value;
            }
        }

        /// <summary>
        /// Gets or sets End of the interval, in milliseconds.
        /// </summary>
        public int End
        {
            get
            {
                return _end;
            }

            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException("value", "can't be zero or negative");
                }

                _end = value;
            }
        }

        /// <summary>
        /// Gets Duration calculated from interval.
        /// </summary>
        public int IntervalDuration
        {
            get
            {
                return _end - _begin;
            }
        }

        #endregion

        #region protected methods

        /// <summary>
        /// Generate the TimeInterval object from the xml doc indicated by reader.
        /// </summary>
        /// <param name="reader">Xml reader.</param>
        protected void InnerParseFromXml(XmlReader reader)
        {
            string begin = string.Empty;
            string end = string.Empty;

            switch (_valueMode)
            {
                case IntervalValueMode.Absolute:
                    begin = reader.GetAttribute(AbsoluteBegin);
                    end = reader.GetAttribute(AbsoluteEnd);
                    break;

                case IntervalValueMode.Relative:
                    begin = reader.GetAttribute(RelativeBegin);
                    end = reader.GetAttribute(RelativeEnd);
                    break;
            }

            if (string.IsNullOrEmpty(begin))
            {
                _begin = 0;
            }
            else
            {
                _begin = int.Parse(begin, CultureInfo.InvariantCulture);
            }

            if (string.IsNullOrEmpty(end))
            {
                _end = 0;
            }
            else
            {
                _end = int.Parse(end, CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Check whether the element is valid or not.
        /// </summary>
        /// <returns>Valid or not.</returns>
        protected bool InnerIsValid()
        {
            bool valid = true;

            if (_begin < 0 || _begin >= _end)
            {
                valid = false;
            }

            return valid;
        }

        /// <summary>
        /// Write the TimeInterval object to xml script.
        /// </summary>
        /// <param name="writer">Xml writer.</param>
        protected void InnerWriteToXml(XmlWriter writer)
        {
            string beginName = string.Empty;
            string endName = string.Empty;

            switch (_valueMode)
            {
                case IntervalValueMode.Absolute:
                    beginName = AbsoluteBegin;
                    endName = AbsoluteEnd;
                    break;

                case IntervalValueMode.Relative:
                    beginName = RelativeBegin;
                    endName = RelativeEnd;
                    break;
            }

            writer.WriteAttributeString(beginName, _begin.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString(endName, _end.ToString(CultureInfo.InvariantCulture));
        }

        #endregion
    }

    /// <summary>
    /// Script unvoiced-voiced segment interval class.
    /// </summary>
    public class ScriptUvSegInterval : TimeInterval, IScriptElement
    {
        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptUvSegInterval"/> class.
        /// </summary>
        public ScriptUvSegInterval()
            : base(IntervalValueMode.Relative)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptUvSegInterval"/> class.
        /// </summary>
        /// <param name="begin">Beginning of the interval.</param>
        /// <param name="end">End of the interval.</param>
        public ScriptUvSegInterval(int begin, int end)
            : base(IntervalValueMode.Relative, begin, end)
        {
        }

        #endregion

        #region IScriptElement operations

        /// <summary>
        /// Generate the ScriptUvSegInterval object from the xml doc indicated by reader.
        /// </summary>
        /// <param name="reader">Xml text reader.</param>
        public void ParseFromXml(XmlTextReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            InnerParseFromXml(reader);
        }

        /// <summary>
        /// Check whether the element is valid or not.
        /// </summary>
        /// <param name="itemID">ID of the script item.</param>
        /// <param name="nodePath">Path of the node.</param>
        /// <param name="scope">The validation scope.</param>
        /// <param name="errors">Contains errors found at present.</param>
        /// <returns>Valid or not. Always true if no validation is performed.</returns>
        public bool IsValid(string itemID, string nodePath, XmlScriptValidationScope scope, ErrorSet errors)
        {
            bool valid = true;

            if ((scope & XmlScriptValidationScope.UvSegInterval) == XmlScriptValidationScope.UvSegInterval)
            {
                if (!InnerIsValid())
                {
                    errors.Add(ScriptError.UvSegIntervalError, itemID, nodePath,
                        Begin.ToString(CultureInfo.InvariantCulture), End.ToString(CultureInfo.InvariantCulture));
                    valid = false;
                }
            }

            return valid;
        }

        /// <summary>
        /// Write the ScriptUvSegInterval object to xml script.
        /// </summary>
        /// <param name="writer">Xml writer.</param>
        public void WriteToXml(XmlWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            writer.WriteStartElement("interval");
            InnerWriteToXml(writer);
            writer.WriteEndElement();
        }

        #endregion
    }

    /// <summary>
    /// Script unvoiced-voiced segment interval class.
    /// </summary>
    public class SegmentInterval : TimeInterval, IScriptElement
    {
        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="SegmentInterval"/> class.
        /// </summary>
        public SegmentInterval()
            : base(IntervalValueMode.Absolute)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SegmentInterval"/> class.
        /// </summary>
        /// <param name="begin">Beginning of the interval.</param>
        /// <param name="end">End of the interval.</param>
        public SegmentInterval(int begin, int end)
            : base(IntervalValueMode.Absolute, begin, end)
        {
        }

        #endregion

        #region IScriptElement operations

        /// <summary>
        /// Generate the ScriptUvSegInterval object from the xml doc indicated by reader.
        /// </summary>
        /// <param name="reader">Xml text reader.</param>
        public void ParseFromXml(XmlTextReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            InnerParseFromXml(reader);
        }

        /// <summary>
        /// Check whether the element is valid or not.
        /// </summary>
        /// <param name="itemID">ID of the script item.</param>
        /// <param name="nodePath">Path of the node.</param>
        /// <param name="scope">The validation scope.</param>
        /// <param name="errors">Contains errors found at present.</param>
        /// <returns>Valid or not. Always true if no validation is performed.</returns>
        public bool IsValid(string itemID, string nodePath, XmlScriptValidationScope scope, ErrorSet errors)
        {
            bool valid = true;

            if ((scope & XmlScriptValidationScope.SegmentInterval) == XmlScriptValidationScope.SegmentInterval)
            {
                if (!InnerIsValid())
                {
                    errors.Add(ScriptError.SegmentIntervalError, itemID, nodePath,
                        Begin.ToString(CultureInfo.InvariantCulture), End.ToString(CultureInfo.InvariantCulture));
                    valid = false;
                }
            }

            return valid;
        }

        /// <summary>
        /// Write the ScriptUvSegInterval object to xml script.
        /// </summary>
        /// <param name="writer">Xml writer.</param>
        public void WriteToXml(XmlWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            writer.WriteStartElement("segment");
            InnerWriteToXml(writer);
            writer.WriteEndElement();
        }

        #endregion
    }

    /// <summary>
    /// Script acoustic chunk class.
    /// </summary>
    public class ScriptAcousticChunk
    {
        #region public static methods

        /// <summary>
        /// Convert chunk encoding from string type to enum type.
        /// </summary>
        /// <param name="encoding">String type of the chunk encoding.</param>
        /// <returns>Enum type of the chunk encoding.</returns>
        public static ScriptAcousticChunkEncoding StringToChunkEncoding(string encoding)
        {
            ScriptAcousticChunkEncoding chunkEncoding;
            if (encoding == "text")
            {
                chunkEncoding = ScriptAcousticChunkEncoding.Text;
            }
            else if (encoding == "base64Binary")
            {
                chunkEncoding = ScriptAcousticChunkEncoding.Base64Binary;
            }
            else if (encoding == "hexBinary")
            {
                chunkEncoding = ScriptAcousticChunkEncoding.HexBinary;
            }
            else
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Invalid chunk data encoding type \"{0}\" found.", encoding);
                throw new InvalidDataException(message);
            }

            return chunkEncoding;
        }

        /// <summary>
        /// Convert chunk encoding from enum type to string type.
        /// </summary>
        /// <param name="chunkEncoding">Enum type of the chunk encoding.</param>
        /// <returns>String type of the chunk encoding.</returns>
        public static string ChunkEncodingToString(ScriptAcousticChunkEncoding chunkEncoding)
        {
            string type;
            switch (chunkEncoding)
            {
                case ScriptAcousticChunkEncoding.Unknown:
                default:
                    type = "unknown";
                    break;
                case ScriptAcousticChunkEncoding.Text:
                    type = "text";
                    break;
                case ScriptAcousticChunkEncoding.Base64Binary:
                    type = "base64Binary";
                    break;
                case ScriptAcousticChunkEncoding.HexBinary:
                    type = "hexBinary";
                    break;
            }

            return type;
        }

        /// <summary>
        /// Get chunk type and chunk data from the xml doc indicated by reader.
        /// </summary>
        /// <param name="reader">Xml text reader.</param>
        /// <param name="chunkEncoding">Encoding of the chunk.</param>
        /// <returns>Chunk data parsed from xml script.</returns>
        public static Collection<float> ParseFromXml(XmlTextReader reader, ScriptAcousticChunkEncoding chunkEncoding)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            if (chunkEncoding == ScriptAcousticChunkEncoding.Unknown)
            {
                throw new ArgumentOutOfRangeException("chunkEncoding", "can't be unknown");
            }

            if (reader.NodeType != XmlNodeType.Text)
            {
                throw new InvalidDataException("Text node is expected at ScriptAcousticChunk parsing beginning.");
            }

            Collection<float> chunkData = new Collection<float>();

            switch (chunkEncoding)
            {
                case ScriptAcousticChunkEncoding.Text:
                    ParseTextData(reader, chunkData);
                    break;
                case ScriptAcousticChunkEncoding.Base64Binary:
                    ParseBase64BinaryData(reader, chunkData);
                    break;
                case ScriptAcousticChunkEncoding.HexBinary:
                    ParseHexBinaryData(reader, chunkData);
                    break;
            }

            if (reader.NodeType != XmlNodeType.EndElement)
            {
                throw new InvalidDataException("EndElement is expected at ScriptAcousticChunk parsing end.");
            }

            return chunkData;
        }

        /// <summary>
        /// Write the acoustic chunk data to xml script.
        /// </summary>
        /// <param name="writer">Xml writer.</param>
        /// <param name="chunkEncoding">Encoding of the chunk.</param>
        /// <param name="chunkData">Chunk data.</param>
        public static void WriteToXml(XmlWriter writer, ScriptAcousticChunkEncoding chunkEncoding,
            Collection<float> chunkData)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            if (chunkEncoding == ScriptAcousticChunkEncoding.Unknown)
            {
                throw new ArgumentOutOfRangeException("chunkEncoding", "can't be unknown");
            }

            switch (chunkEncoding)
            {
                case ScriptAcousticChunkEncoding.Text:
                    WriteTextData(writer, chunkData);
                    break;
                case ScriptAcousticChunkEncoding.Base64Binary:
                    WriteBase64BinaryData(writer, chunkData);
                    break;
                case ScriptAcousticChunkEncoding.HexBinary:
                    WriteHexBinaryData(writer, chunkData);
                    break;
            }
        }

        #endregion

        #region private static methods

        /// <summary>
        /// Parse text chunk data from the xml doc indicated by reader.
        /// </summary>
        /// <param name="reader">Xml text reader.</param>
        /// <param name="chunkData">Chunk data.</param>
        private static void ParseTextData(XmlTextReader reader, Collection<float> chunkData)
        {
            Debug.Assert(reader != null);
            Debug.Assert(reader.NodeType == XmlNodeType.Text);

            string[] items = reader.Value.Split(new char[] { ' ', '\t', '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries);
            foreach (string value in items)
            {
                chunkData.Add(float.Parse(value, CultureInfo.InvariantCulture));
            }

            reader.Read();

            Debug.Assert(reader.NodeType == XmlNodeType.EndElement);
        }

        /// <summary>
        /// Parse base64Binary chunk data from the xml doc indicated by reader.
        /// </summary>
        /// <param name="reader">Xml text reader.</param>
        /// <param name="chunkData">Chunk data.</param>
        private static void ParseBase64BinaryData(XmlTextReader reader, Collection<float> chunkData)
        {
            Debug.Assert(reader != null);
            Debug.Assert(reader.NodeType == XmlNodeType.Text);

            ParseBinaryData(reader, ScriptAcousticChunkEncoding.Base64Binary, chunkData);

            Debug.Assert(reader.NodeType == XmlNodeType.EndElement);
        }

        /// <summary>
        /// Parse hexBinary chunk data from the xml doc indicated by reader.
        /// </summary>
        /// <param name="reader">Xml text reader.</param>
        /// <param name="chunkData">Chunk data.</param>
        private static void ParseHexBinaryData(XmlTextReader reader, Collection<float> chunkData)
        {
            Debug.Assert(reader != null);
            Debug.Assert(reader.NodeType == XmlNodeType.Text);

            ParseBinaryData(reader, ScriptAcousticChunkEncoding.HexBinary, chunkData);

            Debug.Assert(reader.NodeType == XmlNodeType.EndElement);
        }

        /// <summary>
        /// Parse binary chunk data according to encoding type from the xml doc indicated by reader.
        /// </summary>
        /// <param name="reader">Xml text reader.</param>
        /// <param name="chunkEncoding">Encoding of the chunk.</param>
        /// <param name="chunkData">Chunk data.</param>
        private static void ParseBinaryData(XmlTextReader reader, ScriptAcousticChunkEncoding chunkEncoding,
            Collection<float> chunkData)
        {
            Debug.Assert(reader != null);
            Debug.Assert(chunkEncoding == ScriptAcousticChunkEncoding.Base64Binary ||
                chunkEncoding == ScriptAcousticChunkEncoding.HexBinary);
            Debug.Assert(reader.NodeType == XmlNodeType.Text);

            const int FloatCount = 200;
            int bufSize = sizeof(float) * FloatCount;
            byte[] bytes = new byte[bufSize];
            while (reader.NodeType == XmlNodeType.Text)
            {
                int len = 0;
                switch (chunkEncoding)
                {
                    case ScriptAcousticChunkEncoding.Base64Binary:
                        len = reader.ReadContentAsBase64(bytes, 0, bufSize);
                        break;
                    case ScriptAcousticChunkEncoding.HexBinary:
                        len = reader.ReadContentAsBinHex(bytes, 0, bufSize);
                        break;
                }

                if ((len % sizeof(float)) != 0)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Size of binary chunk data isn't multiple of sizeof(float).");
                    throw new InvalidDataException(message);
                }

                for (int i = 0; i < len; i += sizeof(float))
                {
                    chunkData.Add(BitConverter.ToSingle(bytes, i));
                }
            }

            Debug.Assert(reader.NodeType == XmlNodeType.EndElement);
        }

        /// <summary>
        /// Write the text chunk data to xml script.
        /// </summary>
        /// <param name="writer">Xml writer.</param>
        /// <param name="chunkData">Chunk data.</param>
        private static void WriteTextData(XmlWriter writer, Collection<float> chunkData)
        {
            Debug.Assert(writer != null);

            for (int i = 0; i < chunkData.Count; i++)
            {
                if (i > 0)
                {
                    writer.WriteString(" ");
                }

                writer.WriteValue(chunkData[i]);
            }
        }

        /// <summary>
        /// Write base64Binary chunk data to xml script.
        /// </summary>
        /// <param name="writer">Xml writer.</param>
        /// <param name="chunkData">Chunk data.</param>
        private static void WriteBase64BinaryData(XmlWriter writer, Collection<float> chunkData)
        {
            Debug.Assert(writer != null);

            WriteBinaryData(writer, ScriptAcousticChunkEncoding.Base64Binary, chunkData);
        }

        /// <summary>
        /// Write hexBinary chunk data to xml script.
        /// </summary>
        /// <param name="writer">Xml writer.</param>
        /// <param name="chunkData">Chunk data.</param>
        private static void WriteHexBinaryData(XmlWriter writer, Collection<float> chunkData)
        {
            Debug.Assert(writer != null);

            WriteBinaryData(writer, ScriptAcousticChunkEncoding.HexBinary, chunkData);
        }

        /// <summary>
        /// Write binary chunk data according to encoding type to xml script.
        /// </summary>
        /// <param name="writer">Xml writer.</param>
        /// <param name="chunkEncoding">Encoding of the chunk.</param>
        /// <param name="chunkData">Chunk data.</param>
        private static void WriteBinaryData(XmlWriter writer, ScriptAcousticChunkEncoding chunkEncoding,
            Collection<float> chunkData)
        {
            Debug.Assert(writer != null);
            Debug.Assert(chunkEncoding == ScriptAcousticChunkEncoding.Base64Binary ||
                chunkEncoding == ScriptAcousticChunkEncoding.HexBinary);

            int bufSize = chunkData.Count * sizeof(float);
            byte[] bytes = new byte[bufSize];
            int bytesIndex = 0;
            for (int i = 0; i < chunkData.Count; i++)
            {
                byte[] floatBytes = BitConverter.GetBytes(chunkData[i]);
                for (int j = 0; j < sizeof(float); j++)
                {
                    bytes[bytesIndex] = floatBytes[j];
                    bytesIndex ++;
                }
            }

            switch (chunkEncoding)
            {
                case ScriptAcousticChunkEncoding.Base64Binary:
                    writer.WriteBase64(bytes, 0, bufSize);
                    break;
                case ScriptAcousticChunkEncoding.HexBinary:
                    writer.WriteBinHex(bytes, 0, bufSize);
                    break;
            }
        }

        #endregion
    }

    /// <summary>
    /// Script f0 contour class.
    /// </summary>
    public class ScriptF0Contour : IScriptElement
    {
        #region Private fields

        private const string DefaultName = "f0";

        /// <summary>
        /// Chunk coding of the element.
        /// </summary>
        private ScriptAcousticChunkEncoding _chunkEncoding = ScriptAcousticChunkEncoding.Unknown;

        /// <summary>
        /// F0 contour.
        /// </summary>
        private Collection<float> _contour;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptF0Contour"/> class.
        /// </summary>
        /// <param name="name">Name of the contour.</param>
        public ScriptF0Contour(string name = DefaultName)
        {
            _chunkEncoding = ScriptAcousticChunkEncoding.Unknown;
            _contour = new Collection<float>();
            Name = name;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptF0Contour"/> class.
        /// </summary>
        /// <param name="chunkEncoding">Encoding of the chunk.</param>
        /// <param name="contour">Contour.</param>
        /// <param name="name">Name of the contour.</param>
        public ScriptF0Contour(ScriptAcousticChunkEncoding chunkEncoding, Collection<float> contour, string name = DefaultName)
       {
            if (chunkEncoding == ScriptAcousticChunkEncoding.Unknown)
            {
                throw new ArgumentOutOfRangeException("chunkEncoding", "can't be ScriptAcousticChunkEncoding.Unknown");
            }
            else if (contour == null)
            {
                throw new ArgumentNullException("contour");
            }

            _chunkEncoding = chunkEncoding;
            _contour = contour;
            Name = name;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Encoding of the acoustic chunk.
        /// </summary>
        public ScriptAcousticChunkEncoding ChunkEncoding
        {
            get
            {
                return _chunkEncoding;
            }

            set
            {
                if (value == ScriptAcousticChunkEncoding.Unknown)
                {
                    throw new ArgumentOutOfRangeException("value", "can't be ScriptAcousticChunkEncoding.Unknown");
                }

                _chunkEncoding = value;
            }
        }

        /// <summary>
        /// Gets F0 contour.
        /// </summary>
        public Collection<float> Contour
        {
            get
            {
                return _contour;
            }
        }

        /// <summary>
        /// Gets or sets the name of the f0 contour: f0, qf0 (quantized f0), etc.
        /// </summary>
        public string Name { get; set; }

        #endregion

        #region IScriptElement operations

        /// <summary>
        /// Generate the ScriptF0Contour object from the xml doc indicated by reader.
        /// </summary>
        /// <param name="reader">Xml text reader.</param>
        public void ParseFromXml(XmlTextReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            string encoding = reader.GetAttribute("type");
            _chunkEncoding = ScriptAcousticChunk.StringToChunkEncoding(encoding);

            if (!reader.IsEmptyElement)
            {
                if (reader.Read())
                {
                    _contour = ScriptAcousticChunk.ParseFromXml(reader, _chunkEncoding);
                }
            }
        }

        /// <summary>
        /// Check whether the element is valid or not.
        /// </summary>
        /// <param name="itemID">ID of the script item.</param>
        /// <param name="nodePath">Path of the node.</param>
        /// <param name="scope">The validation scope.</param>
        /// <param name="errors">Contains errors found at present.</param>
        /// <returns>Valid or not. Always true if no validation is performed.</returns>
        public bool IsValid(string itemID, string nodePath, XmlScriptValidationScope scope, ErrorSet errors)
        {
            bool valid = true;

            if ((scope & XmlScriptValidationScope.F0) == XmlScriptValidationScope.F0)
            {
                foreach (float f0 in _contour)
                {
                    if (f0 < 0)
                    {
                        errors.Add(ScriptError.F0Error, itemID, nodePath, f0.ToString(CultureInfo.InvariantCulture));
                        valid = false;
                        break;
                    }
                }
            }

            return valid;
        }

        /// <summary>
        /// Write the ScriptF0Contour object to xml script.
        /// </summary>
        /// <param name="writer">Xml writer.</param>
        public void WriteToXml(XmlWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            if (_chunkEncoding == ScriptAcousticChunkEncoding.Unknown)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "_chunkEncoding must not be ScriptAcousticChunkEncoding.Unknown before write to xml script.");
                throw new InvalidDataException(message);
            }
            else if (_contour == null)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "_contour must not be null before write to xml script.");
                throw new InvalidDataException(message);
            }

            writer.WriteStartElement(Name);

            writer.WriteAttributeString("type", ScriptAcousticChunk.ChunkEncodingToString(_chunkEncoding));

            ScriptAcousticChunk.WriteToXml(writer, _chunkEncoding, _contour);

            writer.WriteEndElement();
        }

        #endregion
    }

    /// <summary>
    /// Script Power contour class.
    /// </summary>
    public class ScriptPowerContour : IScriptElement
    {
        #region Private fields

        private const string DefaultName = "pow";

        /// <summary>
        /// Chunk coding of the element.
        /// </summary>
        private ScriptAcousticChunkEncoding _chunkEncoding = ScriptAcousticChunkEncoding.Unknown;

        /// <summary>
        /// F0 contour.
        /// </summary>
        private Collection<float> _contour;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptPowerContour" /> class.
        /// </summary>
        /// <param name="name">Name of the contour.</param>
        public ScriptPowerContour(string name = DefaultName)
        {
            _chunkEncoding = ScriptAcousticChunkEncoding.Unknown;
            _contour = new Collection<float>();
            Name = name;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptPowerContour" /> class.
        /// </summary>
        /// <param name="chunkEncoding">Encoding of the chunk.</param>
        /// <param name="contour">Contour.</param>
        /// <param name="name">Name of the contour.</param>
        public ScriptPowerContour(ScriptAcousticChunkEncoding chunkEncoding, Collection<float> contour, string name = DefaultName)
        {
            if (chunkEncoding == ScriptAcousticChunkEncoding.Unknown)
            {
                throw new ArgumentOutOfRangeException("chunkEncoding", "can't be ScriptAcousticChunkEncoding.Unknown");
            }
            else if (contour == null)
            {
                throw new ArgumentNullException("contour");
            }

            _chunkEncoding = chunkEncoding;
            _contour = contour;
            Name = name;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the encoding of the acoustic chunk.
        /// </summary>
        public ScriptAcousticChunkEncoding ChunkEncoding
        {
            get
            {
                return _chunkEncoding;
            }

            set
            {
                if (value == ScriptAcousticChunkEncoding.Unknown)
                {
                    throw new ArgumentOutOfRangeException("value", "can't be ScriptAcousticChunkEncoding.Unknown");
                }

                _chunkEncoding = value;
            }
        }

        /// <summary>
        /// Gets power contour.
        /// </summary>
        public Collection<float> Contour
        {
            get
            {
                return _contour;
            }
        }

        /// <summary>
        /// Gets or sets the name of the power contour: pow, qpow (quantized power), etc.
        /// </summary>
        public string Name { get; set; }

        #endregion

        #region IScriptElement operations

        /// <summary>
        /// Generate the ScriptF0Contour object from the xml doc indicated by reader.
        /// </summary>
        /// <param name="reader">Xml text reader.</param>
        public void ParseFromXml(XmlTextReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            string encoding = reader.GetAttribute("type");
            _chunkEncoding = ScriptAcousticChunk.StringToChunkEncoding(encoding);

            if (!reader.IsEmptyElement)
            {
                if (reader.Read())
                {
                    _contour = ScriptAcousticChunk.ParseFromXml(reader, _chunkEncoding);
                }
            }
        }

        /// <summary>
        /// Check whether the element is valid or not.
        /// </summary>
        /// <param name="itemID">ID of the script item.</param>
        /// <param name="nodePath">Path of the node.</param>
        /// <param name="scope">The validation scope.</param>
        /// <param name="errors">Contains errors found at present.</param>
        /// <returns>Valid or not. Always true if no validation is performed.</returns>
        public bool IsValid(string itemID, string nodePath, XmlScriptValidationScope scope, ErrorSet errors)
        {
            bool valid = true;

            if ((scope & XmlScriptValidationScope.Power) == XmlScriptValidationScope.Power)
            {
                foreach (float pow in _contour)
                {
                    if (pow < 0)
                    {
                        errors.Add(ScriptError.F0Error, itemID, nodePath, pow.ToString(CultureInfo.InvariantCulture));
                        valid = false;
                        break;
                    }
                }
            }

            return valid;
        }

        /// <summary>
        /// Write the ScriptF0Contour object to xml script.
        /// </summary>
        /// <param name="writer">Xml writer.</param>
        public void WriteToXml(XmlWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            if (_chunkEncoding == ScriptAcousticChunkEncoding.Unknown)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "_chunkEncoding must not be ScriptAcousticChunkEncoding.Unknown before write to xml script.");
                throw new InvalidDataException(message);
            }
            else if (_contour == null)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "_contour must not be null before write to xml script.");
                throw new InvalidDataException(message);
            }

            writer.WriteStartElement(Name);

            writer.WriteAttributeString("type", ScriptAcousticChunk.ChunkEncodingToString(_chunkEncoding));

            ScriptAcousticChunk.WriteToXml(writer, _chunkEncoding, _contour);

            writer.WriteEndElement();
        }

        #endregion
    }

    /// <summary>
    /// Definition of script voice/unvoice type.
    /// </summary>
    public class ScriptUvSeg : IScriptElement
    {
        #region Private fields

        /// <summary>
        /// Minimum (exclusive) f0 value allowed.
        /// </summary>
        private const float MinF0Value = 1e-3F;

        /// <summary>
        /// UV segment type.
        /// </summary>
        private ScriptUvSegType _segType = ScriptUvSegType.Unknown;

        /// <summary>
        /// UV segment element.
        /// </summary>
        private ScriptUvSegInterval _interval;

        /// <summary>
        /// F0 contour element.
        /// </summary>
        private ScriptF0Contour _f0Contour;

        /// <summary>
        /// Quantized f0 contour element.
        /// </summary>
        private ScriptF0Contour _f0ContourQuantized;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptUvSeg"/> class.
        /// </summary>
        public ScriptUvSeg()
        {
            _segType = ScriptUvSegType.Unknown;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptUvSeg"/> class.
        /// </summary>
        /// <param name="segType">Unvoiced-voiced segment type.</param>
        public ScriptUvSeg(ScriptUvSegType segType)
        {
            if (segType == ScriptUvSegType.Unknown)
            {
                throw new ArgumentOutOfRangeException("segType", "can't be ScriptUvSegType.Unknown");
            }

            _segType = segType;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Type of the unvoiced-voiced segment.
        /// </summary>
        public ScriptUvSegType SegType
        {
            get
            {
                return _segType;
            }

            set
            {
                if (value == ScriptUvSegType.Unknown)
                {
                    throw new ArgumentOutOfRangeException("value", "can't be ScriptUvSegType.Unknown");
                }

                _segType = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this object has unvoiced-voiced segment interval value or not.
        /// </summary>
        public bool HasIntervalValue
        {
            get
            {
                return _interval != null;
            }
        }

        /// <summary>
        /// Gets or sets Interval of the unvoiced-voiced segment.
        /// </summary>
        public ScriptUvSegInterval Interval
        {
            get
            {
                return _interval;
            }

            set
            {
                _interval = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this object has F0 contour value.
        /// </summary>
        public bool HasF0ContourValue
        {
            get
            {
                return _f0Contour != null;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this object has F0 contour value.
        /// </summary>
        public bool HasQuantizedF0ContourValue
        {
            get
            {
                return _f0ContourQuantized != null;
            }
        }

        /// <summary>
        /// Gets or sets F0 contour.
        /// </summary>
        public ScriptF0Contour F0Contour
        {
            get
            {
                return _f0Contour;
            }

            set
            {
                _f0Contour = value;
            }
        }

        /// <summary>
        /// Gets or sets quantized F0 contour.
        /// </summary>
        public ScriptF0Contour QuantizedF0Contour
        {
            get
            {
                return _f0ContourQuantized;
            }

            set
            {
                _f0ContourQuantized = value;
            }
        }

        #endregion

        #region public static methods

        /// <summary>
        /// Convert unvoiced-voiced segment type from string type to enum type.
        /// </summary>
        /// <param name="type">String type of the unvoiced-voiced segment type.</param>
        /// <returns>Enum type of the unvoiced-voiced segment type.</returns>
        public static ScriptUvSegType FromStringToUvSegType(string type)
        {
            ScriptUvSegType segType;
            if (type == "u")
            {
                segType = ScriptUvSegType.Unvoiced;
            }
            else if (type == "v")
            {
                segType = ScriptUvSegType.Voiced;
            }
            else if (type == "sil")
            {
                segType = ScriptUvSegType.Silence;
            }
            else if (type == "mixed")
            {
                segType = ScriptUvSegType.Mixed;
            }
            else
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Invalid unvoiced-voiced segment type \"{0}\" found.", type);
                throw new ArgumentException(message, "type");
            }

            return segType;
        }

        /// <summary>
        /// Convert unvoiced-voiced segment type from enum type to string type.
        /// </summary>
        /// <param name="segType">Enum type of the unvoiced-voiced segment type.</param>
        /// <returns>String type of the unvoiced-voiced segment type.</returns>
        public static string FromUvSegTypeToString(ScriptUvSegType segType)
        {
            string type;
            switch (segType)
            {
                case ScriptUvSegType.Unknown:
                default:
                    type = "unknown";
                    break;
                case ScriptUvSegType.Unvoiced:
                    type = "u";
                    break;
                case ScriptUvSegType.Voiced:
                    type = "v";
                    break;
                case ScriptUvSegType.Silence:
                    type = "sil";
                    break;
                case ScriptUvSegType.Mixed:
                    type = "mixed";
                    break;
            }

            return type;
        }

        #endregion

        #region IScriptElement operations

        /// <summary>
        /// Generate the ScriptUvSeg object from the xml doc indicated by reader.
        /// </summary>
        /// <param name="reader">Xml text reader.</param>
        public void ParseFromXml(XmlTextReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            string type = reader.GetAttribute("type");
            _segType = FromStringToUvSegType(type);

            if (!reader.IsEmptyElement)
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.Name == "interval")
                    {
                        _interval = new ScriptUvSegInterval();
                        _interval.ParseFromXml(reader);
                    }
                    else if (reader.NodeType == XmlNodeType.Element && reader.Name == "f0")
                    {
                        _f0Contour = new ScriptF0Contour(reader.Name);
                        _f0Contour.ParseFromXml(reader);
                    }
                    else if (reader.NodeType == XmlNodeType.Element && reader.Name == "qf0")
                    {
                        _f0ContourQuantized = new ScriptF0Contour(reader.Name);
                        _f0ContourQuantized.ParseFromXml(reader);
                    }
                    else if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "uvseg")
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Check whether the element is valid or not.
        /// </summary>
        /// <param name="itemID">ID of the script item.</param>
        /// <param name="nodePath">Path of the node.</param>
        /// <param name="scope">The validation scope.</param>
        /// <param name="errors">Contains errors found at present.</param>
        /// <returns>Valid or not. Always true if no validation is performed.</returns>
        public bool IsValid(string itemID, string nodePath, XmlScriptValidationScope scope, ErrorSet errors)
        {
            bool valid = true;
            if (HasIntervalValue)
            {
                if (!_interval.IsValid(itemID, nodePath, scope, errors))
                {
                    valid = false;
                }
            }

            if (HasF0ContourValue)
            {
                if (!_f0Contour.IsValid(itemID, nodePath, scope, errors))
                {
                    valid = false;
                }
            }

            if ((scope & XmlScriptValidationScope.F0AndUvType) == XmlScriptValidationScope.F0AndUvType)
            {
                switch (_segType)
                {
                    case ScriptUvSegType.Unknown:
                        string message = string.Format(CultureInfo.InvariantCulture,
                            "Invalid unvoiced-voiced segment type ScriptUvSegType.Unknown.");
                        throw new InvalidDataException(message);

                    case ScriptUvSegType.Voiced:
                        if (HasF0ContourValue)
                        {
                            foreach (float f0 in _f0Contour.Contour)
                            {
                                if (f0 >= 0 && f0 <= MinF0Value)
                                {
                                    errors.Add(ScriptError.F0AndUvTypeError, itemID, nodePath);
                                    valid = false;
                                    break;
                                }
                            }
                        }

                        break;

                    case ScriptUvSegType.Unvoiced:
                        if (HasF0ContourValue)
                        {
                            foreach (float f0 in _f0Contour.Contour)
                            {
                                if (f0 > MinF0Value)
                                {
                                    errors.Add(ScriptError.F0AndUvTypeError, itemID, nodePath);
                                    valid = false;
                                    break;
                                }
                            }
                        }

                        break;

                    case ScriptUvSegType.Silence:
                        if (HasF0ContourValue)
                        {
                            foreach (float f0 in _f0Contour.Contour)
                            {
                                if (f0 > MinF0Value)
                                {
                                    errors.Add(ScriptError.F0AndUvTypeError, itemID, nodePath);
                                    valid = false;
                                    break;
                                }
                            }
                        }

                        break;

                    case ScriptUvSegType.Mixed:
                        break;
                }
            }

            return valid;
        }

        /// <summary>
        /// Write the ScriptUvSeg object to xml script.
        /// </summary>
        /// <param name="writer">Xml writer.</param>
        public void WriteToXml(XmlWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            writer.WriteStartElement("uvseg");

            writer.WriteAttributeString("type", FromUvSegTypeToString(_segType));

            if (HasIntervalValue)
            {
                _interval.WriteToXml(writer);
            }

            if (HasF0ContourValue)
            {
                _f0Contour.WriteToXml(writer);
            }

            if (HasQuantizedF0ContourValue)
            {
                _f0ContourQuantized.WriteToXml(writer);
            }

            writer.WriteEndElement();
        }

        #endregion

        #region public methods

        /// <summary>
        /// Remove the unvoiced-voiced segment interval value.
        /// </summary>
        public void RemoveIntervalValue()
        {
            _interval = null;
        }

        /// <summary>
        /// Remove the unvoiced-voiced F0 contour value.
        /// </summary>
        public void RemoveF0ContourValue()
        {
            _f0Contour = null;
        }

        #endregion
    }

    /// <summary>
    /// Script acoustics class.
    /// </summary>
    public class ScriptAcoustics : IScriptElement
    {
        #region Private fields

        /// <summary>
        /// Duration, in milliseconds.
        /// </summary>
        private int _duration;

        /// <summary>
        /// Quantized duration.
        /// </summary>
        private int _quanDuration;

        /// <summary>
        /// Element segment element.
        /// </summary>
        private Collection<SegmentInterval> _segmentIntervals = new Collection<SegmentInterval>();

        /// <summary>
        /// UV segment elements.
        /// </summary>
        private Collection<ScriptUvSeg> _scriptUvSegs = new Collection<ScriptUvSeg>();

        /// <summary>
        /// Power contour element.
        /// </summary>
        private ScriptPowerContour _powContour;

        /// <summary>
        /// Quantized power contour element.
        /// </summary>
        private ScriptPowerContour _powContourQuantized;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptAcoustics"/> class.
        /// </summary>
        public ScriptAcoustics()
        {
            _duration = 0;
            _quanDuration = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptAcoustics"/> class.
        /// </summary>
        /// <param name="duration">Duration.</param>
        public ScriptAcoustics(int duration)
        {
            if (duration <= 0)
            {
                throw new ArgumentOutOfRangeException("duration", "can't be zero or negative");
            }

            _duration = duration;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether this object has duration value.
        /// </summary>
        public bool HasDurationValue
        {
            get
            {
                return _duration > 0;
            }            
        }

        /// <summary>
        /// Gets a value indicating whether this object has a quantilized duration value.
        /// </summary>
        public bool HasQuanDurationValue
        {
            get
            {
                return _quanDuration > 0;
            }
        }

        /// <summary>
        /// Gets or sets Duration of the acoustics, in milliseconds.
        /// </summary>
        public int Duration
        {
            get
            {
                return _duration;
            }

            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException("value", "can't be zero or negative");
                }

                _duration = value;
            }
        }

        /// <summary>
        /// Gets or sets quantized duration of the acoustics.
        /// </summary>
        public int QuanDuration
        {
            get
            {
                return _quanDuration;
            }

            set
            {
                _quanDuration = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this object has Power contour value.
        /// </summary>
        public bool HasPowerContourValue
        {
            get
            {
                return _powContour != null;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this object has Power contour value.
        /// </summary>
        public bool HasQuantizedPowerContourValue
        {
            get
            {
                return _powContourQuantized != null;
            }
        }

        /// <summary>
        /// Gets or sets power contour.
        /// </summary>
        public ScriptPowerContour PowerContour
        {
            get
            {
                return _powContour;
            }

            set
            {
                _powContour = value;
            }
        }

        /// <summary>
        /// Gets or sets quantized Power contour.
        /// </summary>
        public ScriptPowerContour QuantizedPowerContour
        {
            get
            {
                return _powContourQuantized;
            }

            set
            {
                _powContourQuantized = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this object has segment interval or not.
        /// </summary>
        public bool HasSegmentInterval
        {
            get
            {
                return _segmentIntervals.Count > 0;
            }
        }

        /// <summary>
        /// Gets or sets Segment interval.
        /// </summary>
        public Collection<SegmentInterval> SegmentIntervals
        {
            get
            {
                return _segmentIntervals;
            }

            set
            {
                _segmentIntervals = value;
            }
        }

        /// <summary>
        /// Gets Unvoiced-voiced segments in the acoustics.
        /// </summary>
        public Collection<ScriptUvSeg> UvSegs
        {
            get
            {
                return _scriptUvSegs;
            }
        }

        #endregion

        #region IScriptElement operations

        /// <summary>
        /// Generate the ScriptAcoustics object from the xml doc indicated by reader.
        /// </summary>
        /// <param name="reader">Xml text reader.</param>
        public void ParseFromXml(XmlTextReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            string dura = reader.GetAttribute("dura");

            if (string.IsNullOrEmpty(dura))
            {
                _duration = 0;
            }
            else
            {
                _duration = int.Parse(dura, CultureInfo.InvariantCulture);
            }

            string qdura = reader.GetAttribute("qdura");

            if (string.IsNullOrEmpty(qdura))
            {
                _quanDuration = 0;
            }
            else
            {
                _quanDuration = int.Parse(qdura, CultureInfo.InvariantCulture);
            }

            _scriptUvSegs = new Collection<ScriptUvSeg>();

            if (!reader.IsEmptyElement)
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.Name == "segment")
                    {
                        SegmentInterval segmentInterval = new SegmentInterval();
                        segmentInterval.ParseFromXml(reader);
                        _segmentIntervals.Add(segmentInterval);
                    }
                    else if (reader.NodeType == XmlNodeType.Element && reader.Name == "uvseg")
                    {
                        ScriptUvSeg uvSeg = new ScriptUvSeg();
                        uvSeg.ParseFromXml(reader);

                        _scriptUvSegs.Add(uvSeg);
                    }
                    else if (reader.NodeType == XmlNodeType.Element && reader.Name == "pow")
                    {
                        _powContour = new ScriptPowerContour(reader.Name);
                        _powContour.ParseFromXml(reader);
                    }
                    else if (reader.NodeType == XmlNodeType.Element && reader.Name == "qpow")
                    {
                        _powContourQuantized = new ScriptPowerContour(reader.Name);
                        _powContourQuantized.ParseFromXml(reader);
                    }
                    else if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "acoustics")
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Check whether the element is valid or not.
        /// </summary>
        /// <param name="itemID">ID of the script item.</param>
        /// <param name="nodePath">Path of the node.</param>
        /// <param name="scope">The validation scope.</param>
        /// <param name="errors">Contains errors found at present.</param>
        /// <returns>Valid or not. Always true if no validation is performed.</returns>
        public bool IsValid(string itemID, string nodePath, XmlScriptValidationScope scope, ErrorSet errors)
        {
            bool valid = true;

            foreach (ScriptUvSeg uvSeg in _scriptUvSegs)
            {
                if (!uvSeg.IsValid(itemID, nodePath, scope, errors))
                {
                    valid = false;
                }
            }

            if (HasSegmentInterval)
            {
                valid = _segmentIntervals.All(seg => seg.IsValid(itemID, nodePath, scope, errors));
            }

            int preBegin = 0;
            int preEnd = 0;
            foreach (ScriptUvSeg uvSeg in _scriptUvSegs)
            {
                if (uvSeg.HasIntervalValue)
                {
                    if ((scope & XmlScriptValidationScope.UvSegSequence) == XmlScriptValidationScope.UvSegSequence)
                    {
                        if (uvSeg.Interval.End <= preBegin)
                        {
                            errors.Add(ScriptError.UvSegOrderError, itemID, nodePath);
                            valid = false;
                            break;
                        }
                        else if (uvSeg.Interval.Begin < preEnd)
                        {
                            errors.Add(ScriptError.UvSegOverlappingError, itemID, nodePath);
                            valid = false;
                            break;
                        }
                        else
                        {
                            preBegin = uvSeg.Interval.Begin;
                            preEnd = uvSeg.Interval.End;
                        }
                    }

                    if (((scope & XmlScriptValidationScope.DurationAndInterval) == XmlScriptValidationScope.DurationAndInterval)
                        && HasDurationValue)
                    {
                        if (uvSeg.Interval.End > _duration)
                        {
                            errors.Add(ScriptError.DurationAndIntervalError, itemID, nodePath);
                            valid = false;
                            break;
                        }
                    }

                    if (((scope & XmlScriptValidationScope.SegmentDurationAndInterval) == XmlScriptValidationScope.SegmentDurationAndInterval)
                        && HasSegmentInterval)
                    {
                        if (uvSeg.Interval.End > SegmentIntervals.Sum(seg => seg.IntervalDuration))
                        {
                            errors.Add(ScriptError.SegmentDurationAndIntervalError, itemID, nodePath);
                            valid = false;
                            break;
                        }
                    }
                }
            }

            if (((scope & XmlScriptValidationScope.DurationAndSegment) == XmlScriptValidationScope.DurationAndSegment)
                && HasDurationValue && HasSegmentInterval && _duration != SegmentIntervals.Sum(seg => seg.IntervalDuration))
            {
                errors.Add(ScriptError.DurationAndSegmentError, itemID, nodePath,
                    _duration.ToString(CultureInfo.InvariantCulture),
                    SegmentIntervals.First().Begin.ToString(CultureInfo.InvariantCulture),
                    SegmentIntervals.Last().End.ToString(CultureInfo.InvariantCulture));
                valid = false;
            }

            return valid;
        }

        /// <summary>
        /// Write the ScriptAcoustics object to xml script.
        /// </summary>
        /// <param name="writer">Xml writer.</param>
        public void WriteToXml(XmlWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            writer.WriteStartElement("acoustics");

            if (HasDurationValue)
            {
                writer.WriteAttributeString("dura", _duration.ToString(CultureInfo.InvariantCulture));
            }

            if (HasQuanDurationValue)
            {
                writer.WriteAttributeString("qdura", _quanDuration.ToString(CultureInfo.InvariantCulture));
            }

            foreach (var seg in _segmentIntervals)
            {
                seg.WriteToXml(writer);
            }

            if (_scriptUvSegs != null)
            {
                foreach (ScriptUvSeg uvSeg in _scriptUvSegs)
                {
                    uvSeg.WriteToXml(writer);
                }
            }

            if (HasPowerContourValue)
            {
                _powContour.WriteToXml(writer);
            }

            if (HasQuantizedPowerContourValue)
            {
                _powContourQuantized.WriteToXml(writer);
            }

            writer.WriteEndElement();
        }

        #endregion

        #region public methods

        /// <summary>
        /// Remove the duration vlaue.
        /// </summary>
        public void RemoveDurationValue()
        {
            _duration = 0;
        }

        /// <summary>
        /// Remove the power vlaue.
        /// </summary>
        public void RemovePowerValue()
        {
            _powContour = null;
        }

        /// <summary>
        /// Remove the quantized power vlaue.
        /// </summary>
        public void RemoveQuantizedPowerValue()
        {
            _powContourQuantized = null;
        }

        /// <summary>
        /// Remove the segment interval value.
        /// </summary>
        public void RemoveSegmentInterval()
        {
            _segmentIntervals.Clear();
        }

        /// <summary>
        /// Clear all unvoiced-voiced segs object.
        /// </summary>
        public void ClearAllUvSegs()
        {
            _scriptUvSegs.Clear();
        }

        /// <summary>
        /// Add a unvoiced-voiced segs object.
        /// </summary>
        /// <param name="uvseg">Uv segment.</param>
        public void AddUvSeg(ScriptUvSeg uvseg)
        {
            _scriptUvSegs.Add(uvseg);
        }

        #endregion
    }
}