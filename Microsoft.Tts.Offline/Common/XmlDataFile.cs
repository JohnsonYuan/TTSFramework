//----------------------------------------------------------------------------
// <copyright file="XmlDataFile.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements XML Data File class
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Common
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Base class for Xml Data file.
    /// </summary>
    public abstract class XmlDataFile
    {
        /// <summary>
        /// Whether data has been validated.
        /// </summary>
        protected bool validated;

        #region private variables
        private Language _language = Language.Neutral;
        private ErrorSet _errorSet = new ErrorSet();
        private bool _performanceLoad = true;
        private Encoding _encoding = Encoding.Default;
        private string _filePath;
        private XmlWriterSettings _saveSettings = new XmlWriterSettings();

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlDataFile"/> class.
        /// </summary>
        public XmlDataFile()
        {
            _saveSettings.Encoding = Encoding.Unicode;
            _saveSettings.Indent = true;
            _saveSettings.IndentChars = "  ";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlDataFile"/> class.
        /// </summary>
        /// <param name="language">Language of this data.</param>
        public XmlDataFile(Language language)
            : this()
        {
            _language = language;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets Configuration schema.
        /// </summary>
        public virtual XmlSchema Schema
        {
            get 
            { 
                return null; 
            }
        }

        /// <summary>
        /// Gets or sets Language.
        /// </summary>
        public Language Language
        {
            get { return _language; }
            set { _language = value; }
        }

        /// <summary>
        /// Gets or sets Xml encoding.
        /// </summary>
        public Encoding Encoding
        {
            get 
            { 
                return _encoding; 
            }

            set 
            {
                _encoding = value; 
            }
        }

        /// <summary>
        /// Gets or sets File path.
        /// </summary>
        public string FilePath
        {
            get
            { 
                return _filePath; 
            }

            set 
            { 
                _filePath = value; 
            }
        }

        /// <summary>
        /// Gets Data error.
        /// </summary>
        public ErrorSet ErrorSet
        {
            get 
            { 
                return _errorSet; 
            }
        }

        /// <summary>
        /// Gets or sets Save xml file format settings.
        /// </summary>
        public XmlWriterSettings SaveSettings
        {
            get 
            { 
                return _saveSettings;
            }

            set 
            { 
                _saveSettings = value;
            }
        }

        #endregion

        #region public methods

        /// <summary>
        /// Load file from given location.
        /// </summary>
        /// <param name="filePath">Location of the file to load from.</param>
        public void Load(string filePath)
        {
            Load(filePath, null);
        }

        /// <summary>
        /// Load file from given location.
        /// </summary>
        /// <param name="filePath">Location of the file to load from.</param>
        /// <param name="contentController">Content controller.</param>
        public void Load(string filePath, object contentController)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            try
            {
                _encoding = XmlHelper.GetEncoding(filePath);
                using (StreamReader reader = new StreamReader(filePath, _encoding))
                {
                    Reset();
                    _filePath = filePath;
                    LoadInternal(reader, contentController);
                }
            }
            catch (InvalidDataException ide)
            {
                string message = Helper.NeutralFormat(
                    "Loading file [{0}] error is found.", filePath);
                throw new InvalidDataException(message, ide);
            }
        }

        /// <summary>
        /// Load from stream reader, including the schema checking
        /// Using PerformanceLoad() first, if it is not overrided, use Load().
        /// </summary>
        /// <param name="reader">Stream to load from.</param>
        public void Load(StreamReader reader)
        {
            Reset();
            LoadInternal(reader, null);
        }

        /// <summary>
        /// Load from stream reader, including the schema checking
        /// Using PerformanceLoad() first, if it is not overrided, use Load().
        /// </summary>
        /// <param name="reader">Stream to load from.</param>
        /// <param name="contentController">Content controller.</param>
        public void Load(StreamReader reader, object contentController)
        {
            Reset();
            LoadInternal(reader, contentController);
        }

        /// <summary>
        /// Save the file to a new one, after saving as, if user save again, it will saved to
        /// The changed file path.
        /// </summary>
        /// <param name="filePath">File to be saved as.</param>
        public void SaveAs(string filePath)
        {
            Save(filePath, null, null);
            _filePath = filePath;
        }

        /// <summary>
        /// Save the file to a new one, after saving as, if user save again, it will saved to
        /// The changed file path.
        /// </summary>
        /// <param name="filePath">File to be saved as.</param>
        /// <param name="encoding">Encoding.</param>
        /// <param name="contentController">Content controller.</param>
        public void SaveAs(string filePath, Encoding encoding, object contentController)
        {
            Save(filePath, encoding, contentController);
            _filePath = filePath;
        }

        /// <summary>
        /// Save into target file using default encoding; 
        /// If the data are loaded from original file, use the encoding of the input file;
        /// If not, use the Encoding.Default.
        /// </summary>
        /// <param name="filePath">Target file path.</param>
        public void Save(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            Save(filePath, _encoding);
        }

        /// <summary>
        /// Save into target file.
        /// </summary>
        /// <param name="filePath">Target file path.</param>
        /// <param name="encoding">Encoding of the target file.</param>
        public void Save(string filePath, Encoding encoding)
        {
            Save(filePath, encoding, null);
        }

        /// <summary>
        /// Save into target file.
        /// </summary>
        /// <param name="filePath">Target file path.</param>
        /// <param name="encoding">Encoding of the target file.</param>
        /// <param name="contentController">Content controller.</param>
        public void Save(string filePath, Encoding encoding, object contentController)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            if (encoding == null)
            {
                encoding = _encoding;
            }

            _saveSettings.Encoding = encoding;

            Helper.EnsureFolderExistForFile(filePath);
            using (XmlWriter writer = XmlWriter.Create(filePath, _saveSettings))
            {
                writer.WriteStartDocument();
                Save(writer, contentController);
                writer.WriteEndDocument();
            }
        }

        /// <summary>
        /// Save data into target XML writer.
        /// </summary>
        /// <param name="writer">Writer stream.</param>
        /// <param name="contentController">Content controller.</param>
        public void Save(XmlWriter writer, object contentController)
        {
            PerformanceSave(writer, contentController);
        }

        /// <summary>
        /// Validate.
        /// </summary>
        public virtual void Validate()
        {
            validated = true;
        }

        /// <summary>
        /// Validate.
        /// </summary>
        /// <param name="setting">Validation setting.</param>
        public virtual void Validate(XmlValidateSetting setting)
        {
            string message = string.Format(CultureInfo.InvariantCulture,
                "Not implemented in base class. Need to be implemented in derived classes.");
            throw new NotImplementedException(message);
        }

        /// <summary>
        /// Reset xml data file for re-use.
        /// </summary>
        public virtual void Reset()
        {
            _language = Language.Neutral;
            _errorSet.Clear();
            _performanceLoad = true;
            _encoding = Encoding.Default;
            _filePath = string.Empty;
        }

        /// <summary>
        /// Forced Validate.
        /// </summary>
        public void ForcedValidate()
        {
            validated = false;
            Validate();
        }

        #endregion

        #region protected methods

        /// <summary>
        /// Convert hex string in XML file to unicode format.
        /// </summary>
        /// <param name="from">Original string read from XML file.</param>
        /// <returns>Converted unicode format string.</returns>
        protected static string ReplaceHexString(string from)
        {
            string[] items = from.Split(new char[] { ' ' },
                StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < items.Length; i++)
            {
                Match match = Regex.Match(from, @"&#x(\S+);");
                if (match.Success)
                {
                    StringBuilder builder = new StringBuilder();
                    while (match.Success)
                    {
                        short val = short.Parse(match.Groups[1].Value, System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                        string phone = Encoding.Unicode.GetString(BitConverter.GetBytes(val));

                        builder.Append(phone);
                        match = match.NextMatch();
                    }

                    items[i] = builder.ToString();
                }
            }

            return string.Join(" ", items);
        }

        /// <summary>
        /// Performance load.
        /// </summary>
        /// <param name="reader">Stream reader.</param>
        /// <param name="contentController">Content controller.</param>
        protected virtual void PerformanceLoad(StreamReader reader, object contentController)
        {
            _performanceLoad = false;
        }

        /// <summary>
        /// Load data file from XML document.
        /// </summary>
        /// <param name="xmlDoc">XML document to load from.</param>
        /// <param name="nsmgr">Namespace, with local tag 'tts'.</param>
        /// <param name="contentController">Content controller.</param>
        protected virtual void Load(XmlDocument xmlDoc, XmlNamespaceManager nsmgr, object contentController)
        {
        }

        /// <summary>
        /// Performance save operations.
        /// </summary>
        /// <param name="writer">Writer.</param>
        /// <param name="contentController">Content controller.</param>
        protected virtual void PerformanceSave(XmlWriter writer, object contentController)
        {
        }

        #endregion

        #region private methods

        /// <summary>
        /// Load from stream reader, including the schema checking
        /// Using PerformanceLoad() first, if it is not overrided, use Load().
        /// </summary>
        /// <param name="reader">Stream to load from.</param>
        /// <param name="contentController">Content controller.</param>
        private void LoadInternal(StreamReader reader, object contentController)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            if (reader.CurrentEncoding == null)
            {
                string message = Helper.NeutralFormat("reader.CurrentEncoding should not be null.");
                throw new ArgumentException(message);
            }

            if (reader.BaseStream == null)
            {
                string message = Helper.NeutralFormat("reader.BaseStream should not be null.");
                throw new ArgumentException(message);
            }

            _encoding = reader.CurrentEncoding;

            XmlHelper.Validate(reader.BaseStream, Schema);

            // do PerformanceLoad first, if it is not overrided, then use XmlDocument to load
            PerformanceLoad(reader, contentController);
            if (!_performanceLoad)
            {
                XmlDocument xmlDoc = new XmlFileDocument();
                xmlDoc.Load(reader);
                XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
                if (Schema.TargetNamespace != null)
                {
                    nsmgr.AddNamespace("tts", Schema.TargetNamespace);
                }

                Load(xmlDoc, nsmgr, contentController);
            }

            validated = false;
        }

        #endregion
    }
}