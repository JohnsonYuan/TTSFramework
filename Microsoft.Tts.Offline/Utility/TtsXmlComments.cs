//----------------------------------------------------------------------------
// <copyright file="TtsXmlComments.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements class manageme TTS xml comments.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Xml;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Tts xml comment.
    /// </summary>
    public class TtsXmlComment
    {
        #region Fields

        private object _tag;
        private string _name;
        private string _value;
        private string _timestamp;

        #endregion

        #region Constructions

        /// <summary>
        /// Initializes a new instance of the <see cref="TtsXmlComment"/> class.
        /// </summary>
        public TtsXmlComment()
        {
            _timestamp = DateTime.Now.ToString(TtsXmlComments.TimeStampFormat, DateTimeFormatInfo.InvariantInfo);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TtsXmlComment"/> class.
        /// </summary>
        /// <param name="name">Name of the comment.</param>
        /// <param name="value">Value of the comment.</param>
        public TtsXmlComment(string name, string value)
            : this()
        {
            _name = name;
            _value = value;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Time stamp.
        /// </summary>
        public string Timestamp
        {
            get { return _timestamp; }
            set { _timestamp = value; }
        }

        /// <summary>
        /// Gets or sets Name of the comment.
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// Gets or sets Tag object of the comment.
        /// </summary>
        public object Tag
        {
            get { return _tag; }
            set { _tag = value; }
        }

        /// <summary>
        /// Gets or sets Value of the comment.
        /// </summary>
        public string Value
        {
            get { return _value; }
            set { _value = value; }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Parse node from XML node.
        /// </summary>
        /// <param name="node">XML node.</param>
        public void Parse(XmlNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

            XmlElement ele = (XmlElement)node;
            _name = ele.GetAttribute("name");
            _value = ele.GetAttribute("value");
            _timestamp = ele.GetAttribute("timestamp");
        }

        /// <summary>
        /// Parse comment from XML text reader.
        /// </summary>
        /// <param name="reader">XmlReader.</param>
        public void Parse(XmlReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            _name = reader.GetAttribute("name");
            _value = reader.GetAttribute("value");
            _timestamp = reader.GetAttribute("timestamp");
        }

        /// <summary>
        /// Write the item to xml writer.
        /// </summary>
        /// <param name="writer">XmlWriter.</param>
        public void WriteToXml(XmlWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            if (string.IsNullOrEmpty(_name))
            {
                throw new NullObjectFieldException("_name is null");
            }

            if (string.IsNullOrEmpty(_value))
            {
                throw new NullObjectFieldException("_value is null");
            }

            writer.WriteStartElement("comment");
            writer.WriteAttributeString("name", _name);
            writer.WriteAttributeString("value", _value);
            if (!string.IsNullOrEmpty(_timestamp))
            {
                writer.WriteAttributeString("timestamp", _timestamp);
            }

            writer.WriteEndElement();
        }

        /// <summary>
        /// Create xml element.
        /// </summary>
        /// <param name="dom">Xml document.</param>
        /// <param name="xmlNamespace">Xml namespace.</param>
        /// <returns>Created xml element.</returns>
        public XmlElement CreateXmlElement(XmlDocument dom, string xmlNamespace)
        {
            if (dom == null)
            {
                throw new ArgumentNullException("dom");
            }

            if (string.IsNullOrEmpty(xmlNamespace))
            {
                throw new ArgumentNullException("xmlNamespace");
            }

            if (string.IsNullOrEmpty(_name))
            {
                throw new NullObjectFieldException("_name is null");
            }

            if (string.IsNullOrEmpty(_value))
            {
                throw new NullObjectFieldException("_value is null");
            }

            XmlElement commentEle = dom.CreateElement("comment", xmlNamespace);
            commentEle.SetAttribute("name", _name);
            commentEle.SetAttribute("value", _value);
            if (!string.IsNullOrEmpty(_timestamp))
            {
                commentEle.SetAttribute("timestamp", _timestamp);
            }

            return commentEle;
        }

        #endregion
    }

    /// <summary>
    /// Tts XML status.
    /// </summary>
    public class TtsXmlStatus
    {
        #region Fields

        /// <summary>
        /// Unset status position.
        /// </summary>
        public const int UnsetPosition = -1;

        private object _tag;
        private string _severity;

        private string _name;
        private EditStatus _status = EditStatus.Original;
        private string _originalValue;
        private int _position = UnsetPosition;
        private int _delIndex = UnsetPosition;
        private string _comment;
        private string _timestamp;

        #endregion

        #region Constructions

        /// <summary>
        /// Initializes a new instance of the <see cref="TtsXmlStatus"/> class.
        /// </summary>
        public TtsXmlStatus()
        {
            _timestamp = DateTime.Now.ToString(TtsXmlComments.TimeStampFormat, DateTimeFormatInfo.InvariantInfo);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TtsXmlStatus"/> class.
        /// </summary>
        /// <param name="name">Status name.</param>
        public TtsXmlStatus(string name) :
            this()
        {
            _name = name;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TtsXmlStatus"/> class.
        /// </summary>
        /// <param name="name">Status name.</param>
        /// <param name="status">Status.</param>
        public TtsXmlStatus(string name, EditStatus status) :
            this(name)
        {
            _status = status;
        }

        #endregion

        #region Enum

        /// <summary>
        /// Edit status.
        /// </summary>
        public enum EditStatus
        {
            /// <summary>
            /// Original status.
            /// </summary>
            Original,

            /// <summary>
            /// Add status.
            /// </summary>
            Add,

            /// <summary>
            /// Modify status.
            /// </summary>
            Modify,

            /// <summary>
            /// Delete status.
            /// </summary>
            Delete
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Deleted index.
        /// </summary>
        public int DelIndex
        {
            get { return _delIndex; }
            set { _delIndex = value; }
        }

        /// <summary>
        /// Gets or sets Position.
        /// </summary>
        public int Position
        {
            get { return _position; }
            set { _position = value; }
        }

        /// <summary>
        /// Gets or sets Object tag.
        /// </summary>
        public object Tag
        {
            get { return _tag; }
            set { _tag = value; }
        }

        /// <summary>
        /// Gets or sets Severity string.
        /// </summary>
        public string Severity
        {
            get { return _severity; }
            set { _severity = value; }
        }

        /// <summary>
        /// Gets or sets Time stamp.
        /// </summary>
        public string Timestamp
        {
            get { return _timestamp; }
            set { _timestamp = value; }
        }

        /// <summary>
        /// Gets or sets Status name.
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
        /// Gets or sets Edit status.
        /// </summary>
        public EditStatus Status
        {
            get { return _status; }
            set { _status = value; }
        }

        /// <summary>
        /// Gets or sets Original value.
        /// </summary>
        public string OriginalValue
        {
            get { return _originalValue; }
            set { _originalValue = value; }
        }

        /// <summary>
        /// Gets or sets Edit comment.
        /// </summary>
        public string Comment
        {
            get { return _comment; }
            set { _comment = value; }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Parse status from xml node.
        /// </summary>
        /// <param name="node">XML node.</param>
        public void Parse(XmlNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

            XmlElement ele = (XmlElement)node;
            _originalValue = ele.Value;
            _status = (EditStatus)Enum.Parse(typeof(EditStatus), ele.GetAttribute("value"));
            string positionString = ele.GetAttribute("position");
            if (!string.IsNullOrEmpty(positionString))
            {
                if (!int.TryParse(positionString, out _position))
                {
                    throw new InvalidDataException(Helper.NeutralFormat(
                        "Can't prase position [{0}]", positionString));
                }
            }

            string delIndexString = ele.GetAttribute("delIndex");
            if (!string.IsNullOrEmpty(delIndexString))
            {
                if (!int.TryParse(delIndexString, out _delIndex))
                {
                    throw new InvalidDataException(Helper.NeutralFormat(
                        "Can't prase deleted index [{0}]", delIndexString));
                }
            }

            string severityString = ele.GetAttribute("severity");
            if (!string.IsNullOrEmpty(severityString))
            {
                _severity = severityString;
            }

            _timestamp = ele.GetAttribute("timestamp");
            _comment = ele.GetAttribute("comment");
        }

        /// <summary>
        /// Parse status from XML reader.
        /// </summary>
        /// <param name="reader">XML reader to parse from.</param>
        public void Parse(XmlReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            _name = reader.GetAttribute("name");
            _status = (EditStatus)Enum.Parse(typeof(EditStatus), reader.GetAttribute("value"));

            string positionString = reader.GetAttribute("position");
            if (!string.IsNullOrEmpty(positionString))
            {
                _position = int.Parse(positionString, CultureInfo.InvariantCulture);
            }

            string delIndexString = reader.GetAttribute("delIndex");
            if (!string.IsNullOrEmpty(delIndexString))
            {
                _delIndex = int.Parse(delIndexString, CultureInfo.InvariantCulture);
            }

            _comment = reader.GetAttribute("comment");
            _timestamp = reader.GetAttribute("timestamp");

            string severityString = reader.GetAttribute("severity");
            if (!string.IsNullOrEmpty(severityString))
            {
                _severity = severityString;
            }

            if (!reader.IsEmptyElement)
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.CDATA)
                    {
                        _originalValue = reader.Value;
                    }
                    else if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "status")
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Write the item to xml writer.
        /// </summary>
        /// <param name="writer">XmlWriter.</param>
        public void WriteToXml(XmlWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            writer.WriteStartElement("status");

            if (string.IsNullOrEmpty("_name"))
            {
                throw new NullObjectFieldException("_name is null");
            }

            writer.WriteAttributeString("name", _name);
            writer.WriteAttributeString("value", _status.ToString());
            if (!string.IsNullOrEmpty(_comment))
            {
                writer.WriteAttributeString("comment", _comment);
            }

            if (!string.IsNullOrEmpty(_timestamp))
            {
                writer.WriteAttributeString("timestamp", _timestamp);
            }

            if (_position != UnsetPosition)
            {
                writer.WriteAttributeString("position", _position.ToString(CultureInfo.InvariantCulture));
            }

            if (_delIndex != UnsetPosition)
            {
                writer.WriteAttributeString("delIndex", _delIndex.ToString(CultureInfo.InvariantCulture));
            }

            if (!string.IsNullOrEmpty(_severity))
            {
                writer.WriteAttributeString("severity", _severity);
            }

            if (!string.IsNullOrEmpty(_originalValue))
            {
                writer.WriteCData(_originalValue);
            }

            writer.WriteEndElement();
        }

        /// <summary>
        /// Create XML element.
        /// </summary>
        /// <param name="dom">XML document.</param>
        /// <param name="xmlNamespace">XML namespace.</param>
        /// <returns>Created XML element.</returns>
        public XmlElement CreateXmlElement(XmlDocument dom, string xmlNamespace)
        {
            if (dom == null)
            {
                throw new ArgumentNullException("dom");
            }

            if (string.IsNullOrEmpty(xmlNamespace))
            {
                throw new ArgumentNullException("xmlNamespace");
            }

            XmlElement commentEle = dom.CreateElement("status", xmlNamespace);
            commentEle.SetAttribute("name", _name);
            commentEle.SetAttribute("value", _status.ToString());
            if (!string.IsNullOrEmpty(_comment))
            {
                commentEle.SetAttribute("comment", _comment);
            }

            if (_position != UnsetPosition)
            {
                commentEle.SetAttribute("position", _position.ToString(CultureInfo.InvariantCulture));
            }

            if (_delIndex != UnsetPosition)
            {
                commentEle.SetAttribute("delIndex", _delIndex.ToString(CultureInfo.InvariantCulture));
            }

            if (!string.IsNullOrEmpty(_severity))
            {
                commentEle.SetAttribute("severity", _severity);
            }

            if (!string.IsNullOrEmpty(_timestamp))
            {
                commentEle.SetAttribute("timestamp", _timestamp);
            }

            commentEle.Value = _originalValue;
            return commentEle;
        }

        #endregion
    }

    /// <summary>
    /// Voice quality issues.
    /// </summary>
    public class TtsXmlVQIssue
    {
        #region Fields

        /// <summary>
        /// Issue severity.
        /// </summary>
        private VQSeverity _severity;

        /// <summary>
        /// Issue Type.
        /// </summary>
        private string _issueType; 

        #endregion

        #region Constructions

        /// <summary>
        /// Initializes a new instance of the <see cref="TtsXmlVQIssue"/> class.
        /// </summary>
        public TtsXmlVQIssue()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TtsXmlVQIssue"/> class.
        /// </summary>
        /// <param name="severity">Severity.</param>
        /// <param name="issueType">Issue type.</param>
        public TtsXmlVQIssue(VQSeverity severity, string issueType)
        {
            _severity = severity;
            _issueType = issueType;
        }

        #endregion 

        #region Enum

        /// <summary>
        /// Voice quality issue Severity.
        /// </summary>
        public enum VQSeverity
        {
            /// <summary>
            /// High severity.
            /// </summary>
            High,

            /// <summary>
            /// Medium severity.
            /// </summary>
            Medium,

            /// <summary>
            /// Low severity.
            /// </summary>
            Low
        }

        #endregion

        #region property

        /// <summary>
        /// Gets Issue severity.
        /// </summary>
        public VQSeverity Severity 
        { 
            get { return _severity; }
        }

        /// <summary>
        /// Gets Issue type.
        /// </summary>
        public string IssueType 
        { 
            get { return _issueType; }
        }

        #endregion 

        #region Public method

        /// <summary>
        /// Parse status from xml node.
        /// </summary>
        /// <param name="node">XML node.</param>
        public void Parse(XmlNode node)
        {
            Helper.ThrowIfNull(node);

            XmlElement ele = (XmlElement)node;

            string severityString = ele.GetAttribute("severity");
            if (!string.IsNullOrEmpty(severityString))
            {
                _severity = (VQSeverity)Enum.Parse(typeof(VQSeverity), severityString);
            }

            string issueTypeString = ele.GetAttribute("issueType");
            if (!string.IsNullOrEmpty(issueTypeString))
            {
                _issueType = issueTypeString;
            }
        }

        /// <summary>
        /// Parse status from XML reader.
        /// </summary>
        /// <param name="reader">XML reader to parse from.</param>
        public void Parse(XmlReader reader)
        {
            Helper.ThrowIfNull(reader);

            string severityString = reader.GetAttribute("severity");
            if (!string.IsNullOrEmpty(severityString))
            {
                _severity = (VQSeverity)Enum.Parse(typeof(VQSeverity), severityString);
            }

            string issueTypeString = reader.GetAttribute("issueType");
            if (!string.IsNullOrEmpty(issueTypeString))
            {
                _issueType = issueTypeString;
            }
        }

        /// <summary>
        /// Write the item to xml writer.
        /// </summary>
        /// <param name="writer">XmlWriter.</param>
        public void WriteToXml(XmlWriter writer)
        {
            Helper.ThrowIfNull(writer);

            writer.WriteStartElement("issue");

            writer.WriteAttributeString("severity", _severity.ToString());

            if (!string.IsNullOrEmpty(_issueType))
            {
                writer.WriteAttributeString("issueType", _issueType);
            }

            writer.WriteEndElement();
        }

        /// <summary>
        /// Create XML element.
        /// </summary>
        /// <param name="dom">XML document.</param>
        /// <param name="xmlNamespace">XML namespace.</param>
        /// <returns>Created XML element.</returns>
        public XmlElement CreateXmlElement(XmlDocument dom, string xmlNamespace)
        {
            Helper.ThrowIfNull(dom);

            if (string.IsNullOrEmpty(xmlNamespace))
            {
                throw new ArgumentNullException("xmlNamespace");
            }

            XmlElement commentEle = dom.CreateElement("issue", xmlNamespace);
            
            commentEle.SetAttribute("severity", _severity.ToString());
            if (!string.IsNullOrEmpty(_issueType))
            {
                commentEle.SetAttribute("issueType", _issueType);
            }

            return commentEle;
        }

        #endregion
    }

    /// <summary>
    /// Tts xml comments.
    /// </summary>
    public class TtsXmlComments
    {
        #region Fileds

        /// <summary>
        /// The elelment selves status element.
        /// </summary>
        public const string SelfStatusName = "this";

        /// <summary>
        /// Timestamp format.
        /// </summary>
        public const string TimeStampFormat = "yyyy/MM/dd HH:mm:ss";

        private object _tag;

        private Dictionary<string, Collection<TtsXmlStatus>> _ttsXmlStatusDict =
            new Dictionary<string, Collection<TtsXmlStatus>>();

        private Dictionary<string, Collection<TtsXmlComment>> _ttsXmlCommentDict =
            new Dictionary<string, Collection<TtsXmlComment>>();

        private Collection<TtsXmlVQIssue> _ttsXmlVQIssueCollection =
            new Collection<TtsXmlVQIssue>();

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="TtsXmlComments"/> class.
        /// </summary>
        public TtsXmlComments()
        {
        }

        #endregion

        #region Property

        /// <summary>
        /// Gets or sets Tag object.
        /// </summary>
        public object Tag
        {
            get { return _tag; }
            set { _tag = value; }
        }

        /// <summary>
        /// Gets XML status dictionary.
        /// </summary>
        public Dictionary<string, Collection<TtsXmlStatus>> TtsXmlStatusDict
        {
            get { return _ttsXmlStatusDict; }
        }

        /// <summary>
        /// Gets XML comment dictionary.
        /// </summary>
        public Dictionary<string, Collection<TtsXmlComment>> TtsXmlCommentDict
        {
            get { return _ttsXmlCommentDict; }
        }

        /// <summary>
        /// Gets XML VQ Issue Collection.
        /// </summary>
        public Collection<TtsXmlVQIssue> TtsXmlVQIssueCollection
        {
            get { return _ttsXmlVQIssueCollection; }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Reset the status and comments.
        /// </summary>
        public void Reset()
        {
            _ttsXmlStatusDict.Clear();
            _ttsXmlCommentDict.Clear();
            _ttsXmlVQIssueCollection.Clear();
        }

        /// <summary>
        /// Remove status.
        /// </summary>
        /// <param name="name">Status to be removed.</param>
        public void RemoveStatus(string name)
        {
            if (_ttsXmlStatusDict.ContainsKey(name))
            {
                _ttsXmlStatusDict.Remove(name);
            }
        }

        /// <summary>
        /// Get single status.
        /// </summary>
        /// <param name="name">Status to be get.</param>
        /// <returns>Result status.</returns>
        public TtsXmlStatus GetSingleStatus(string name)
        {
            TtsXmlStatus status = null;
            if (TtsXmlStatusDict.ContainsKey(name) &&
                TtsXmlStatusDict[name].Count > 0)
            {
                Debug.Assert(TtsXmlStatusDict[name].Count == 1);
                status = TtsXmlStatusDict[name][0];
            }

            return status;
        }

        /// <summary>
        /// Remove comment.
        /// </summary>
        /// <param name="name">Name of the comment to be removed.</param>
        public void RemoveComment(string name)
        {
            if (_ttsXmlCommentDict.ContainsKey(name))
            {
                _ttsXmlCommentDict.Remove(name);
            }
        }

        /// <summary>
        /// Get single comment.
        /// </summary>
        /// <param name="name">Name of the comment to get.</param>
        /// <returns>Comment value.</returns>
        public TtsXmlComment GetSingleComment(string name)
        {
            Collection<TtsXmlComment> comments = GetComments(name);
            Debug.Assert(comments.Count <= 1);
            TtsXmlComment comment = null;
            if (comments.Count > 0)
            {
                comment = comments[0];
            }

            return comment;
        }

        /// <summary>
        /// Comments to get.
        /// </summary>
        /// <param name="name">Comment name to get.</param>
        /// <returns>Comment values.</returns>
        public Collection<TtsXmlComment> GetComments(string name)
        {
            Collection<TtsXmlComment> values = new Collection<TtsXmlComment>();

            if (_ttsXmlCommentDict.ContainsKey(name))
            {
                foreach (TtsXmlComment comment in _ttsXmlCommentDict[name])
                {
                    values.Add(comment);
                }
            }

            return values;
        }

        /// <summary>
        /// Append comment.
        /// </summary>
        /// <param name="comment">Comment to append.</param>
        /// <param name="canBeMultiValue">Whether the comment can be mutlti value.</param>
        public void AppendComment(TtsXmlComment comment, bool canBeMultiValue)
        {
            if (comment == null)
            {
                throw new ArgumentNullException("comment");
            }

            if (string.IsNullOrEmpty(comment.Name))
            {
                throw new ArgumentNullException("comment");
            }

            if (!_ttsXmlCommentDict.ContainsKey(comment.Name))
            {
                _ttsXmlCommentDict.Add(comment.Name, new Collection<TtsXmlComment>());
            }

            if (!canBeMultiValue)
            {
                _ttsXmlCommentDict[comment.Name].Clear();
            }

            comment.Tag = this;
            _ttsXmlCommentDict[comment.Name].Add(comment);
        }

        /// <summary>
        /// Append status.
        /// </summary>
        /// <param name="status">Status of the .</param>
        /// <param name="canBeMultiValue">Whether the status can be mutlti value.</param>
        public void AppendStatus(TtsXmlStatus status, bool canBeMultiValue)
        {
            if (status == null)
            {
                throw new ArgumentNullException("status");
            }

            if (string.IsNullOrEmpty(status.Name))
            {
                throw new ArgumentNullException("status");
            }

            if (!_ttsXmlStatusDict.ContainsKey(status.Name))
            {
                _ttsXmlStatusDict.Add(status.Name, new Collection<TtsXmlStatus>());
            }

            if (!canBeMultiValue)
            {
                _ttsXmlStatusDict[status.Name].Clear();
            }

            status.Tag = this;
            _ttsXmlStatusDict[status.Name].Add(status);
        }

        /// <summary>
        /// Append VQ Issues.
        /// </summary>
        /// <param name="ttsXmlVQIssue">VQ Issue.</param>
        public void AppendVQIssue(TtsXmlVQIssue ttsXmlVQIssue)
        {
            Helper.ThrowIfNull(ttsXmlVQIssue);

            if (string.IsNullOrEmpty(ttsXmlVQIssue.IssueType))
            {
                throw new ArgumentNullException("ttsXmlVQIssue.IssueType");
            }

            _ttsXmlVQIssueCollection.Add(ttsXmlVQIssue);
        }

        /// <summary>
        /// Parse Comments from XML reader.
        /// </summary>
        /// <param name="reader">Xml reader.</param>
        public void Parse(XmlReader reader)
        {
            Helper.ThrowIfNull(reader);

            if (reader.IsEmptyElement)
            {
                return;
            }

            _ttsXmlCommentDict.Clear();
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "comment")
                {
                    TtsXmlComment ttsXmlComment = new TtsXmlComment();
                    ttsXmlComment.Parse(reader);
                    ttsXmlComment.Tag = this;
                    if (_ttsXmlCommentDict.ContainsKey(ttsXmlComment.Name))
                    {
                        throw new InvalidDataException(Helper.NeutralFormat(
                            "Duplicate comment name [{0}].", ttsXmlComment.Name));
                    }

                    if (!_ttsXmlCommentDict.ContainsKey(ttsXmlComment.Name))
                    {
                        _ttsXmlCommentDict.Add(ttsXmlComment.Name, new Collection<TtsXmlComment>());
                    }

                    _ttsXmlCommentDict[ttsXmlComment.Name].Add(ttsXmlComment);
                }
                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "status")
                {
                    TtsXmlStatus ttsXmlStatus = new TtsXmlStatus();
                    ttsXmlStatus.Parse(reader);
                    ttsXmlStatus.Tag = this;

                    if (!_ttsXmlStatusDict.ContainsKey(ttsXmlStatus.Name))
                    {
                        _ttsXmlStatusDict.Add(ttsXmlStatus.Name, new Collection<TtsXmlStatus>());
                    }

                    _ttsXmlStatusDict[ttsXmlStatus.Name].Add(ttsXmlStatus);
                }
                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "issue")
                {
                    TtsXmlVQIssue ttsVqIssue = new TtsXmlVQIssue();
                    ttsVqIssue.Parse(reader);
                    _ttsXmlVQIssueCollection.Add(ttsVqIssue);
                }
                else if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "comments")
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Parse TtsXmlComment list.
        /// </summary>
        /// <param name="node">XmlNode.</param>
        /// <param name="nsmgr">XmlNamespaceManager.</param>
        public void Parse(XmlNode node, XmlNamespaceManager nsmgr)
        {
            Helper.ThrowIfNull(node);
            Helper.ThrowIfNull(nsmgr);

            XmlNodeList xmlCommentNodes = node.SelectNodes(@"tts:comment", nsmgr);
            _ttsXmlCommentDict.Clear();
            foreach (XmlNode commentNode in xmlCommentNodes)
            {
                TtsXmlComment ttsXmlComment = new TtsXmlComment();
                ttsXmlComment.Parse(commentNode);
                ttsXmlComment.Tag = this;
                if (_ttsXmlCommentDict.ContainsKey(ttsXmlComment.Name))
                {
                    throw new InvalidDataException(Helper.NeutralFormat(
                        "Duplicate comment name [{0}].", ttsXmlComment.Name));
                }

                if (!_ttsXmlCommentDict.ContainsKey(ttsXmlComment.Name))
                {
                    _ttsXmlCommentDict.Add(ttsXmlComment.Name, new Collection<TtsXmlComment>());
                }

                _ttsXmlCommentDict[ttsXmlComment.Name].Add(ttsXmlComment);
            }

            XmlNodeList xmlStatusNodes = node.SelectNodes(@"tts:status", nsmgr);
            _ttsXmlStatusDict.Clear();
            foreach (XmlNode statusNode in xmlStatusNodes)
            {
                TtsXmlStatus ttsXmlStatus = new TtsXmlStatus();
                ttsXmlStatus.Parse(statusNode);
                ttsXmlStatus.Tag = this;
                if (_ttsXmlStatusDict.ContainsKey(ttsXmlStatus.Name))
                {
                    throw new InvalidDataException(Helper.NeutralFormat(
                        "Duplicate comment name [{0}].", ttsXmlStatus.Name));
                }

                if (!_ttsXmlStatusDict.ContainsKey(ttsXmlStatus.Name))
                {
                    _ttsXmlStatusDict.Add(ttsXmlStatus.Name, new Collection<TtsXmlStatus>());
                }

                _ttsXmlStatusDict[ttsXmlStatus.Name].Add(ttsXmlStatus);
            }

            XmlNodeList xmlVQIssuesNodes = node.SelectNodes(@"tts:issue", nsmgr);
            _ttsXmlVQIssueCollection.Clear();
            foreach (XmlNode ttsXmlNode in xmlVQIssuesNodes)
            {
                TtsXmlVQIssue xmlVQIssue = new TtsXmlVQIssue();
                xmlVQIssue.Parse(ttsXmlNode);
                _ttsXmlVQIssueCollection.Add(xmlVQIssue);
            }
        }

        /// <summary>
        /// Write the item to xml writer.
        /// </summary>
        /// <param name="writer">XmlWriter.</param>
        public void WriteToXml(XmlWriter writer)
        {
            Helper.ThrowIfNull(writer);

            if (_ttsXmlStatusDict.Count == 0 && _ttsXmlCommentDict.Count == 0 && _ttsXmlVQIssueCollection.Count == 0)
            {
                return;
            }

            writer.WriteStartElement("comments");

            foreach (Collection<TtsXmlComment> ttsXmlComment in _ttsXmlCommentDict.Values)
            {
                foreach (TtsXmlComment comment in ttsXmlComment)
                {
                    comment.WriteToXml(writer);
                }
            }

            foreach (Collection<TtsXmlStatus> ttsXmlStatus in _ttsXmlStatusDict.Values)
            {
                foreach (TtsXmlStatus status in ttsXmlStatus)
                {
                    status.WriteToXml(writer);
                }
            }

            foreach (TtsXmlVQIssue ttsXmlVQIssue in _ttsXmlVQIssueCollection)
            {
                ttsXmlVQIssue.WriteToXml(writer);
            }

            writer.WriteEndElement();
        }

        /// <summary>
        /// Create XML element of the TtsXMLComments.
        /// </summary>
        /// <param name="dom">Xml document.</param>
        /// <param name="xmlNamespace">Xml name space.</param>
        /// <returns>Created XML element.</returns>
        public XmlElement CreateXmlElement(XmlDocument dom, string xmlNamespace)
        {
            Helper.ThrowIfNull(dom);

            if (string.IsNullOrEmpty(xmlNamespace))
            {
                throw new ArgumentNullException("xmlNamespace");
            }

            if (_ttsXmlStatusDict.Count == 0 && _ttsXmlCommentDict.Count == 0 && _ttsXmlVQIssueCollection.Count == 0)
            {
                return null;
            }

            XmlElement commentsEle = dom.CreateElement("comments", xmlNamespace);

            foreach (Collection<TtsXmlComment> ttsXmlComment in _ttsXmlCommentDict.Values)
            {
                foreach (TtsXmlComment comment in ttsXmlComment)
                {
                    commentsEle.AppendChild(comment.CreateXmlElement(dom, xmlNamespace));
                }
            }

            foreach (Collection<TtsXmlStatus> ttsXmlStatus in _ttsXmlStatusDict.Values)
            {
                foreach (TtsXmlStatus status in ttsXmlStatus)
                {
                    commentsEle.AppendChild(status.CreateXmlElement(dom, xmlNamespace));
                }
            }

            foreach (TtsXmlVQIssue ttsXmlVQIssue in _ttsXmlVQIssueCollection)
            {
                commentsEle.AppendChild(ttsXmlVQIssue.CreateXmlElement(dom, xmlNamespace));
            }

            return commentsEle;
        }

        #endregion
    }
}