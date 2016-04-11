//----------------------------------------------------------------------------
// <copyright file="TtsPosSet.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements TTS POS set
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Class of tts pos set.
    /// </summary>
    public class TtsPosSet : XmlDataFile
    {
        #region Fields

        private static XmlSchema _schema;
        private Dictionary<string, uint> _posSet = new Dictionary<string, uint>();
        private Dictionary<uint, string> _posIdSet = new Dictionary<uint, string>();
        private Dictionary<string, string> _posTaggingPos = new Dictionary<string, string>();
        private Dictionary<string, string> _posCategory = new Dictionary<string, string>();
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="TtsPosSet"/> class.
        /// </summary>
        /// <param name="language">Language.</param>
        public TtsPosSet(Language language) : base(language)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TtsPosSet"/> class.
        /// </summary>
        public TtsPosSet()
        {
        }

        #endregion

        #region Properties
        /// <summary>
        /// Gets Schema of PosTable.xml.
        /// </summary>
        public override System.Xml.Schema.XmlSchema Schema
        {
            get
            {
                if (_schema == null)
                {
                    _schema = XmlHelper.LoadSchemaFromResource("Microsoft.Tts.Offline.Schema.PosTable.xsd");
                }

                return _schema;
            }
        }

        /// <summary>
        /// Gets Pos Set.
        /// </summary>
        public Dictionary<string, uint> Items
        {
            get { return _posSet; }
        }

        /// <summary>
        /// Gets POS Id set.
        /// </summary>
        public Dictionary<uint, string> IdItems
        {
            get { return _posIdSet; }
        }

        /// <summary>
        /// Gets Category Tagging POS set.
        /// </summary>
        public Dictionary<string, string> CategoryTaggingPOS
        {
            get { return _posTaggingPos; }
        }

        /// <summary>
        /// Gets POS category dictionary.
        /// </summary>
        public Dictionary<string, string> PosCategory
        {
            get { return _posCategory; }
        }

        #endregion

        #region method

        /// <summary>
        /// Load POS set from lexicon schema file.
        /// </summary>
        /// <param name="lexiconSchemaFile">The lexicon schema file.</param>
        /// <returns>Loaded POS set object.</returns>
        public static TtsPosSet LoadFromFile(string lexiconSchemaFile)
        {
            return LoadFromFile(Language.Neutral, lexiconSchemaFile);
        }

        /// <summary>
        /// Load POS set from lexicon schema file.
        /// </summary>
        /// <param name="language">Language of the POS set.</param>
        /// <param name="lexiconSchemaFile">The lexicon schema file.</param>
        /// <returns>Loaded POS set object.</returns>
        public static TtsPosSet LoadFromFile(Language language, string lexiconSchemaFile)
        {
            LexicalAttributeSchema attributeSchema = new LexicalAttributeSchema(language);
            attributeSchema.Load(lexiconSchemaFile);
            attributeSchema.Validate();
            if (attributeSchema.ErrorSet.Contains(ErrorSeverity.MustFix))
            {
                attributeSchema.ErrorSet.Export(Console.Error);
                string message = Helper.NeutralFormat("Please fix the error of lexicon schema file [{0}]",
                    lexiconSchemaFile);
                throw new InvalidDataException(message);
            }

            return TtsPosSet.LoadPosTaggingPosFromSchema(attributeSchema);
        }

        /// <summary>
        /// Load TtsPosSet from LexicalAttributeSchema.
        /// </summary>
        /// <param name="schemaFilePath">LexicalAttributeSchema file path.</param>
        /// <returns>Loaded TtsPosSet.</returns>
        public static TtsPosSet LoadFromSchema(string schemaFilePath)
        {
            Helper.ThrowIfFileNotExist(schemaFilePath);
            LexicalAttributeSchema schema = new LexicalAttributeSchema();
            schema.Load(schemaFilePath);
            schema.Validate();
            if (schema.ErrorSet.Contains(ErrorSeverity.MustFix))
            {
                throw new InvalidDataException(
                    Helper.NeutralFormat("Error is found in lexicon schema file {0}, as {1}.",
                    schemaFilePath, schema.ErrorSet.ErrorsString()));
            }

            return TtsPosSet.LoadFromSchema(schema);
        }

        /// <summary>
        /// Load TtsPosSet from LexicalAttributeSchema.
        /// </summary>
        /// <param name="attributeSchema">LexicalAttributeSchema.</param>
        /// <returns>TtsPosSet.</returns>
        public static TtsPosSet LoadFromSchema(LexicalAttributeSchema attributeSchema)
        {
            if (attributeSchema == null)
            {
                throw new ArgumentNullException("attributeSchema");
            }

            TtsPosSet ttsPosSet = new TtsPosSet(attributeSchema.Language);
            if (attributeSchema.Categories == null || attributeSchema.Categories.Count == 0)
            {
                throw new ArgumentException("The categories in attributeSchma should not be null or empty.");
            }

            AttributeCategory posCategory = attributeSchema.Categories[0];
            if (!posCategory.Name.Equals("POS", StringComparison.Ordinal))
            {
                throw new ArgumentException(Helper.NeutralFormat(
                    "The 1st category in attribute schema should be \"{0}\"", "POS"));
            }

            string categoryTaggingPOS = null;
            AddPosFromCategory(ttsPosSet, posCategory, false, categoryTaggingPOS);
            return ttsPosSet;
        }

        /// <summary>
        /// Loading Postagging POS from LexicalAttributeSchema.
        /// </summary>
        /// <param name="attributeSchema">LexicalAttributeSchema.</param>
        /// <returns>TtsPosSet containing only Postagging POS.</returns>
        public static TtsPosSet LoadPosTaggingPosFromSchema(LexicalAttributeSchema attributeSchema)
        {
            if (attributeSchema == null)
            {
                throw new ArgumentNullException("attributeSchema");
            }

            TtsPosSet ttsPosSet = new TtsPosSet(attributeSchema.Language);
            if (attributeSchema.Categories == null || attributeSchema.Categories.Count == 0)
            {
                throw new ArgumentException("The categories in attributeSchma should not be null or empty.");
            }

            AttributeCategory posCategory = attributeSchema.Categories[0];
            if (!posCategory.Name.Equals("POS", StringComparison.Ordinal))
            {
                throw new ArgumentException(Helper.NeutralFormat(
                    "The 1st category in attribute schema should be \"{0}\"", "POS"));
            }

            AddPosTaggingPosFromCategory(ttsPosSet, posCategory);
            return ttsPosSet;
        }

        /// <summary>
        /// Whether the two TtsPosSet are identical.
        /// </summary>
        /// <param name="left">TtsPosSet left.</param>
        /// <param name="right">TtsPosSet right.</param>
        /// <returns>True or false.</returns>
        public static bool Equals(TtsPosSet left, TtsPosSet right)
        {
            if (!Helper.CompareDictionary<string, string>(left.PosCategory, right.PosCategory))
            {
                return false;
            }

            if (!Helper.CompareDictionary<uint, string>(left.IdItems, right.IdItems))
            {
                return false;
            }

            if (!Helper.CompareDictionary<string, uint>(left.Items, right.Items))
            {
                return false;
            }

            if (!left.Schema.Equals(right.Schema))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Add a new POS with name and hexId.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="hexId">HexId.</param>
        public void Add(string name, string hexId)
        {
            if (_posSet.ContainsKey(name))
            {
                this.ErrorSet.Add(PosErrorType.DuplicatePosName, name, hexId);
            }
            else
            {
                uint decimalId = uint.Parse(hexId, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
                if (_posIdSet.ContainsKey(decimalId))
                {
                    this.ErrorSet.Add(PosErrorType.DuplicatePosId, name, hexId);
                }
                else
                {
                    _posSet[name] = decimalId;
                    _posIdSet[decimalId] = name;
                }
            }
        }

        /// <summary>
        /// Add a new POS with name and decimal id.
        /// </summary>
        /// <param name="name">POS name.</param>
        /// <param name="decimalId">Decimal id.</param>
        /// <param name="categoryTaggingPOS">Category tagging pos of this POS name.</param>
        public void Add(string name, int decimalId, string categoryTaggingPOS)
        {
            uint id = Convert.ToUInt32(decimalId);
            if (_posSet.ContainsKey(name))
            {
                this.ErrorSet.Add(PosErrorType.DuplicatePosName, name, id.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                if (_posIdSet.ContainsKey(id))
                {
                    this.ErrorSet.Add(PosErrorType.DuplicatePosId, name, id.ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    _posSet[name] = id;
                    _posIdSet[id] = name;
                    _posTaggingPos[name] = categoryTaggingPOS;
                }
            }
        }

        /// <summary>
        /// Reset tts pos set for re-use.
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            _posSet.Clear();
            _posIdSet.Clear();
        }

        /// <summary>
        /// Load XML file.
        /// </summary>
        /// <param name="xmlDoc">Xml Document.</param>
        /// <param name="nsmgr">Nsmgr.</param>
        /// <param name="contentController">Content controller.</param>
        protected override void Load(XmlDocument xmlDoc, XmlNamespaceManager nsmgr, object contentController)
        {
            this.ErrorSet.Clear();
            Language language = Localor.StringToLanguage(xmlDoc.DocumentElement.Attributes["lang"].InnerText);
            if (!this.Language.Equals(Language.Neutral) && !language.Equals(this.Language))
            {
                this.ErrorSet.Add(CommonError.NotConsistentLanguage,
                    this.Language.ToString(), "initial one", language.ToString(), "pos set");
            }

            this.Language = language;

            XmlNodeList posNodeList = xmlDoc.DocumentElement.SelectNodes(@"//tts:posTable/tts:pos", nsmgr);
            foreach (XmlNode posNode in posNodeList)
            {
                Add(posNode.Attributes["name"].InnerText,
                    posNode.Attributes["id"].InnerText);
            }
        }

        /// <summary>
        /// PerformanceSave.
        /// </summary>
        /// <param name="writer">Writer.</param>
        /// <param name="contentController">Content controller.</param>
        protected override void PerformanceSave(XmlWriter writer, object contentController)
        {
            writer.WriteStartElement("posTable", Schema.TargetNamespace);
            writer.WriteAttributeString("lang", Localor.LanguageToString(Language));
            foreach (KeyValuePair<string, uint> pair in this._posSet)
            {
                writer.WriteStartElement("pos");
                writer.WriteAttributeString("name", pair.Key);
                string hexId = string.Format(CultureInfo.InvariantCulture, "{0:X}", pair.Value);
                while (hexId.Length < 4)
                {
                    hexId = "0" + hexId;
                }

                writer.WriteAttributeString("id", hexId);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        /// <summary>
        /// Add Postagging POS from attribute category.
        /// </summary>
        /// <param name="ttsPosSet">TTS POS set.</param>
        /// <param name="category">Attribute category.</param>
        private static void AddPosTaggingPosFromCategory(TtsPosSet ttsPosSet, AttributeCategory category)
        {
            if (ttsPosSet == null)
            {
                throw new ArgumentNullException("ttsPosSet");
            }

            if (category.Values != null)
            {
                foreach (AttributeValue value in category.Values)
                {
                    if (value.PosTagging)
                    {
                        ttsPosSet.Add(value.Name, value.Id, value.Name);
                    }

                    if (value.Categories != null)
                    {
                        foreach (AttributeCategory subCategory in value.Categories)
                        {
                            AddPosTaggingPosFromCategory(ttsPosSet, subCategory);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Add POS from attribute category into TtsPosSet.
        /// </summary>
        /// <param name="ttsPosSet">TtsPosSet.</param>
        /// <param name="category">Category.</param>
        /// <param name="posTagging">Whether current category is under postagging value node.</param>
        /// <param name="categoryTaggingPOS">The POSTagging POS of this category.</param>
        private static void AddPosFromCategory(TtsPosSet ttsPosSet, AttributeCategory category, bool posTagging, string categoryTaggingPOS)
        {
            if (category.Values != null)
            {
                foreach (AttributeValue value in category.Values)
                {
                    if (value.PosTagging || posTagging)
                    {
                        if (value.PosTagging)
                        {
                            categoryTaggingPOS = value.Name;
                        }

                        ttsPosSet.Add(value.Name, value.Id, categoryTaggingPOS);
                        if (ttsPosSet.PosCategory.ContainsKey(value.Name))
                        {
                            throw new InvalidDataException(Helper.NeutralFormat(
                                "Duplicate POS [{0}] in category [{1}] with the one in category [{2}]",
                                value.Name, category.Name, ttsPosSet.PosCategory[value.Name]));
                        }

                        ttsPosSet.PosCategory.Add(value.Name, category.Name);
                    }

                    if (value.Categories != null)
                    {
                        foreach (AttributeCategory subCategory in value.Categories)
                        {
                            AddPosFromCategory(ttsPosSet, subCategory, value.PosTagging || posTagging, categoryTaggingPOS);
                        }
                    }
                }
            }
        }

        #endregion
    }
}