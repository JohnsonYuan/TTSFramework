//----------------------------------------------------------------------------
// <copyright file="LexicalAttributeSchema.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements LexicalAttributeSchema class
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Core
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Lexical Attribute Schema Error.
    /// </summary>
    public enum LexicalAttributeSchemaError
    {
        /// <summary>
        /// Duplicate category name
        /// Parameters:
        /// {0}: duplicate category name.
        /// </summary>
        [ErrorAttribute(Message = "Duplicate category name \"{0}\" found in the schema")]
        DuplicateCategoryName,

        /// <summary>
        /// Not Continued Id
        /// Parameters:
        /// {0}: xml node
        /// {1}: id.
        /// </summary>
        [ErrorAttribute(Message = "Id error for the node \"{0}\" with id {1}: Id should be start from zero and be continued")]
        NotContinuedId,

        /// <summary>
        /// Id error
        /// Parameters:
        /// {0}: maximal id.
        /// </summary>
        [ErrorAttribute(Message = "Id should be less than the maximal number of {0}")]
        IdTooLarge,

        /// <summary>
        /// Duplicate POS value name
        /// Parameters:
        /// {0}: duplicate POS name.
        /// </summary>
        [ErrorAttribute(Message = "Duplicate POS value name \"{0}\" found in the schema")]
        DuplicatePosValueName,

        /// <summary>
        /// Duplicate POS value
        /// Parameters:
        /// {0}: duplicate POS value
        /// {1}: duplicate POS name
        /// {2}: duplicate existed POS name.
        /// </summary>
        [ErrorAttribute(Message = "Duplicate POS value [{0}] in name [{1}] with the name [{2}] found in the schema")]
        DuplicatePosValue,

        /// <summary>
        /// Empty Lexical attribute schema
        /// Parameters:
        /// {0}: POS category.
        /// </summary>
        [ErrorAttribute(Message = "Could not find POS category \"{0}\" in the schema")]
        MissPosCategory,

        /// <summary>
        /// Empty Lexical attribute schema.
        /// </summary>
        [ErrorAttribute(Message = "Empty schema")]
        EmptySchema,

        /// <summary>
        /// Miss unknown POS value
        /// Parameters:
        /// {0}: unknown POS name.
        /// </summary>
        [ErrorAttribute(Message = "Missing unknown POS definition with value \"{0}\" in the schema",
            Severity = ErrorSeverity.Warning)]
        MissUnknownPosValue,

        /// <summary>
        /// Unknown POS should be POS tagging POS.
        /// </summary>
        [ErrorAttribute(Message = "Unknown POS should be POS tagging POS",
            Severity = ErrorSeverity.Warning)]
        UnknownPosShouldBePosTaggingPos,

        /// <summary>
        /// Missing posTagging setting
        /// Parameters:
        /// {0}: POS value.
        /// </summary>
        [ErrorAttribute(Message = "posTagging property should be set to true for POS \"{0}\" or its parent POS",
            Severity = ErrorSeverity.Warning)]
        MissPosTaggingSetting
    }

    /// <summary>
    /// Attribute Category in the lexical attribute schema.
    /// </summary>
    public class AttributeCategory
    {
        #region Fields
        private string _name;
        private int _id;
        private float _mean, _invStdDev;
        private Collection<AttributeValue> _values = new Collection<AttributeValue>();
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeCategory"/> class.
        /// </summary>
        public AttributeCategory()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeCategory"/> class.
        /// </summary>
        /// <param name="name">Category name.</param>
        public AttributeCategory(string name)
        {
            _name = name;
        }

        #region Properties

        /// <summary>
        /// Gets or sets Name of category.
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// Gets or sets Category Id.
        /// </summary>
        public int Id
        {
            get { return _id; }
            set { _id = value; }
        }

        /// <summary>
        /// Gets or sets Category mean.
        /// </summary>
        public float Mean
        {
            get { return _mean; }
            set { _mean = value; }
        }

        /// <summary>
        /// Gets or sets Category InvStdDev.
        /// </summary>
        public float InvStdDev
        {
            get { return _invStdDev; }
            set { _invStdDev = value; }
        }

        /// <summary>
        /// Gets Category values.
        /// </summary>
        public Collection<AttributeValue> Values
        {
            get { return _values; }
        }

        #endregion

        #region Public methods
        /// <summary>
        /// Get the value according to the name.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <returns>Value.</returns>
        public AttributeValue GetValue(string name)
        {
            AttributeValue findValue = null;
            Debug.Assert(_values != null && _values.Count > 0);
            foreach (AttributeValue value in _values)
            {
                if (value.Name.Equals(name))
                {
                    findValue = value;
                    break;
                }
            }

            return findValue;
        }

        #endregion
    }

    /// <summary>
    /// Attribute value in the lexical attribute schema.
    /// </summary>
    public class AttributeValue
    {
        #region Fields
        private string _name;
        private int _id;
        private float _mean, _invStdDev;
        private bool _posTagging;
        private Collection<AttributeCategory> _categories;
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeValue"/> class.
        /// </summary>
        public AttributeValue()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeValue"/> class.
        /// </summary>
        /// <param name="name">Value name.</param>
        public AttributeValue(string name)
        {
            _name = name;
        }

        #region Properties
        /// <summary>
        /// Gets or sets Value name.
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// Gets or sets Value Id.
        /// </summary>
        public int Id
        {
            get { return _id; }
            set { _id = value; }
        }

        /// <summary>
        /// Gets or sets Category mean.
        /// </summary>
        public float Mean
        {
            get { return _mean; }
            set { _mean = value; }
        }

        /// <summary>
        /// Gets or sets Category InvStdDev.
        /// </summary>
        public float InvStdDev
        {
            get { return _invStdDev; }
            set { _invStdDev = value; }
        }

        /// <summary>
        /// Gets Categories under the value.
        /// </summary>
        public Collection<AttributeCategory> Categories
        {
            get { return _categories; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this value is used for POS tagging.
        /// </summary>
        public bool PosTagging
        {
            get { return _posTagging; }
            set { _posTagging = value; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Update the categoires collecton in the class.
        /// </summary>
        /// <param name="value">Value.</param>
        public void UpdateCategories(Collection<AttributeCategory> value)
        {
            _categories = value;
        }

        /// <summary>
        /// Get the category according to the name.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <returns>Category.</returns>
        public AttributeCategory GetCategory(string name)
        {
            AttributeCategory findCategory = null;
            if (_categories != null)
            {
                foreach (AttributeCategory category in _categories)
                {
                    if (category.Name.Equals(name))
                    {
                        findCategory = category;
                        break;
                    }
                }
            }

            return findCategory;
        }

        #endregion
    }

    /// <summary>
    /// Lexical Attribute Schema.
    /// </summary>
    public class LexicalAttributeSchema : XmlDataFile
    {
        /// <summary>
        /// POS category Name.
        /// </summary>
        public static string PosCategoryName = "POS";

        /// <summary>
        /// Unkonwn POS value.
        /// </summary>
        public static string UnknownPosValue = "unknown";

        #region Fields
        private static int _maxId = 65534;
        private static XmlSchema _schema;
        private string _version;
        private Collection<AttributeCategory> _categories = new Collection<AttributeCategory>();
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="LexicalAttributeSchema"/> class.
        /// </summary>
        /// <param name="language">Language.</param>
        public LexicalAttributeSchema(Language language)
            : base(language)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LexicalAttributeSchema"/> class.
        /// </summary>
        public LexicalAttributeSchema()
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
                    _schema = XmlHelper.LoadSchemaFromResource(
                        "Microsoft.Tts.Offline.Schema.LexicalAttributeSchema.xsd");
                }

                return _schema;
            }
        }

        /// <summary>
        /// Gets or sets Version.
        /// </summary>
        public string Version
        {
            get { return _version; }
            set { _version = value; }
        }

        /// <summary>
        /// Gets Main Categories under the root of the schema.
        /// </summary>
        public Collection<AttributeCategory> Categories
        {
            get { return _categories; }
        }

        #endregion

        #region methods

        /// <summary>
        /// Reset.
        /// </summary>
        public override void Reset()
        {
            _categories.Clear();
            base.Reset();
        }

        /// <summary>
        /// Validate whether the category contains the value.
        /// </summary>
        /// <param name="categoryName">Category name.</param>
        /// <param name="categoryValue">Category value.</param>
        /// <returns>Whether the category contains the value.</returns>
        public bool ValidateCategoryValue(string categoryName, string categoryValue)
        {
            if (string.IsNullOrEmpty(categoryName))
            {
                throw new ArgumentNullException("categoryName");
            }

            if (string.IsNullOrEmpty(categoryValue))
            {
                throw new ArgumentNullException("categoryValue");
            }

            AttributeCategory category = GetCategory(categoryName);
            if (category == null)
            {
                throw new ArgumentException(Helper.NeutralFormat(
                    "Can't find category [{0}] in schema file", categoryName));
            }

            bool containValue = false;
            foreach (AttributeValue attributeValue in category.Values)
            {
                if (attributeValue.Name.Equals(categoryValue))
                {
                    containValue = true;
                    break;
                }
            }

            return containValue;
        }

        /// <summary>
        /// Validate the lexical attribute schema.
        /// </summary>
        public override void Validate()
        {
            if (_categories == null || _categories.Count == 0)
            {
                this.ErrorSet.Add(LexicalAttributeSchemaError.EmptySchema);
            }
            else
            {
                ValidateCategoryName();
                ValidateCategoryValues();
                ValidatePosValue();
                ValidateId();
            }
        }

        /// <summary>
        /// Get the category according to the category name.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <returns>Category.</returns>
        public AttributeCategory GetRootCategory(string name)
        {
            AttributeCategory findCategory = null;
            foreach (AttributeCategory category in _categories)
            {
                if (category.Name.Equals(name))
                {
                    findCategory = category;
                    break;
                }
            }

            return findCategory;
        }

        /// <summary>
        /// Get the category according to name.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <returns>Attribute category.</returns>
        public AttributeCategory GetCategory(string name)
        {
            AttributeCategory category = null;
            foreach (AttributeCategory attributeCategory in _categories)
            {
                if (attributeCategory.Name.Equals(name, StringComparison.Ordinal))
                {
                    category = attributeCategory;
                    break;
                }
            }

            if (category == null)
            {
                foreach (AttributeCategory attributeCategory in _categories)
                {
                    category = GetCategory(attributeCategory, name);
                    if (category != null)
                    {
                        break;
                    }
                }
            }

            return category;
        }

        /// <summary>
        /// Search the category according the category name and 
        /// Generate the Equation string for the assigned value
        /// <param />
        /// Example:
        /// GenearteString("POS", "noun")
        /// The return value is 
        /// POS=NOM=1    NOM_CLASS=noun=3.
        /// </summary>
        /// <param name="categoryName">Category name under the root node.</param>
        /// <param name="value">The assigned value.</param>
        /// <returns>The Equation string for the value.</returns>
        public string GenerateString(string categoryName, string value)
        {
            string generatedString = string.Empty;
            foreach (AttributeCategory attributeCategory in _categories)
            {
                if (attributeCategory.Name.Equals(categoryName))
                {
                    generatedString = GenerateString(attributeCategory, value);

                    // Assume there is only one subnode with the same categoryName under the root
                    break;
                }
            }

            return generatedString;
        }

        /// <summary>
        /// Remove all the categories with no value under root.
        /// </summary>
        public void RemoveEmptyCategory()
        {
            foreach (AttributeCategory category in _categories)
            {
                RemoveEmptyCategory(category);
            }
        }

        /// <summary>
        /// Set continued ID from zero for the attribute schema.
        /// </summary>
        public void SetContinuedId()
        {
            int idIndex = 0;
            foreach (AttributeCategory category in Categories)
            {
                SetContinuedId(category, ref idIndex);
                if (idIndex > _maxId)
                {
                    break;
                }
            }

            if (idIndex > _maxId)
            {
                this.ErrorSet.Add(LexicalAttributeSchemaError.IdTooLarge, 
                    _maxId.ToString(CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// Get pos tagging pos.
        /// </summary>
        /// <param name="pos">Pos to be found pos tagging POS.</param>
        /// <returns>Nearst parent pos tagging POS.</returns>
        public string GetPosTaggingPos(string pos)
        {
            Stack<AttributeValue> posStack = new Stack<AttributeValue>();
            AttributeCategory ttsPosSetCategory = GetCategory(PosCategoryName);
            string posTaggingPos = string.Empty;
            foreach (AttributeValue attributeValue in ttsPosSetCategory.Values)
            {
                posTaggingPos = FindPosTaggingPos(posStack, attributeValue, pos);
                if (!string.IsNullOrEmpty(posTaggingPos))
                {
                    break;
                }
            }

            return posTaggingPos;
        }

        /// <summary>
        /// Load XML file.
        /// </summary>
        /// <param name="xmlDoc">XmlDoc.</param>
        /// <param name="nsmgr">Nsmgr.</param>
        /// <param name="contentController">Content controller.</param>
        protected override void Load(XmlDocument xmlDoc, XmlNamespaceManager nsmgr, object contentController)
        {
            this.ErrorSet.Clear();
            XmlNode rootNode = xmlDoc.DocumentElement.SelectSingleNode(@"//lexAttributeTable", nsmgr);
            Debug.Assert(rootNode != null);
            Debug.Assert(rootNode.Attributes["lang"] != null);
            Language language = Localor.StringToLanguage(rootNode.Attributes["lang"].InnerText);
            if (!this.Language.Equals(Language.Neutral) && !language.Equals(this.Language))
            {
                this.ErrorSet.Add(CommonError.NotConsistentLanguage,
                    this.Language.ToString(), "initial one", language.ToString(), "attribute schema");
            }

            this.Language = language;
            Debug.Assert(rootNode.Attributes["version"] != null);
            Version = rootNode.Attributes["version"].InnerText;

            XmlNodeList categoryNodeList = xmlDoc.DocumentElement.SelectNodes(
                @"//lexAttributeTable/Category", nsmgr);
            Debug.Assert(categoryNodeList.Count > 0);
 
            foreach (XmlNode categoryNode in categoryNodeList)
            { 
                AttributeCategory category = LoadXmlCategoryNode(categoryNode);
                Debug.Assert(category != null);
                _categories.Add(category);
            }
        }

        /// <summary>
        /// Save phone set to target file.
        /// </summary>
        /// <param name="writer">Writer file to save into.</param>
        /// <param name="contentController">Content controller.</param>
        protected override void PerformanceSave(XmlWriter writer, object contentController)
        {
            writer.WriteStartElement("lexAttributeTable");
            writer.WriteAttributeString("lang", Localor.LanguageToString(Language));
            writer.WriteAttributeString("version", _version);

            foreach (AttributeCategory category in _categories)
            {
                WriteXmlCategoryNode(writer, category);
            }

            writer.WriteEndElement();
        }

        /// <summary>
        /// Find parent nearest pos tagging POS.
        /// </summary>
        /// <param name="parentPosStack">Parent pos stack, record all parent pos.</param>
        /// <param name="attributeValue">Current attribute value.</param>
        /// <param name="pos">Pos to be found pos tagging pos.</param>
        /// <returns>Nearst parent pos tagging POS.</returns>
        private static string FindPosTaggingPos(Stack<AttributeValue> parentPosStack,
            AttributeValue attributeValue, string pos)
        {
            string posTaggingPos = string.Empty;
            parentPosStack.Push(attributeValue);

            if (attributeValue.Name.Equals(pos, StringComparison.Ordinal))
            {
                foreach (AttributeValue value in parentPosStack)
                {
                    if (value.PosTagging)
                    {
                        posTaggingPos = value.Name;
                        break;
                    }
                }
            }
            else
            {
                if (attributeValue.Categories != null)
                {
                    foreach (AttributeCategory category in attributeValue.Categories)
                    {
                        if (category.Values != null)
                        {
                            foreach (AttributeValue value in category.Values)
                            {
                                posTaggingPos = FindPosTaggingPos(parentPosStack, value, pos);
                                if (!string.IsNullOrEmpty(posTaggingPos))
                                {
                                    break;
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(posTaggingPos))
                        {
                            break;
                        }
                    }
                }
            }

            parentPosStack.Pop();

            return posTaggingPos;
        }

        /// <summary>
        /// Get the attribute category from the required category root and queried by its name.
        /// </summary>
        /// <param name="attributeCategory">Required category root.</param>
        /// <param name="name">Queied category name.</param>
        /// <returns>The arrribute category.</returns>
        private AttributeCategory GetCategory(AttributeCategory attributeCategory, string name)
        {
            if (attributeCategory == null)
            {
                throw new ArgumentNullException("attributeCategory");
            }

            AttributeCategory category = null;
            if (attributeCategory.Values != null && attributeCategory.Values.Count > 0)
            {
                foreach (AttributeValue value in attributeCategory.Values)
                {
                    if (value.Categories != null && value.Categories.Count > 0)
                    {
                        foreach (AttributeCategory subCategory in value.Categories)
                        {
                            category = GetCategory(subCategory, name);
                            if (category != null)
                            {
                                break;
                            }
                        }
                    }
                }
            }

            return category;
        }

        /// <summary>
        /// Validate POS value:
        /// 1. whether contains duplicate POS name
        /// 2. whether define unknown POS
        /// 3. whether unknown POS set posTagging to true
        /// 4. whether each POS or its parent has set posTagging to true;.
        /// </summary>
        private void ValidatePosValue()
        {
            Debug.Assert(_categories != null && _categories.Count > 0);
            if (!_categories[0].Name.Equals(PosCategoryName, StringComparison.Ordinal))
            {
                this.ErrorSet.Add(LexicalAttributeSchemaError.MissPosCategory, PosCategoryName);
            }
            else
            {
                Collection<string> posValueSet = new Collection<string>();
                CheckDuplicateValueName(posValueSet, _categories[0]);
                CheckPosTaggingFlag(_categories[0]);

                bool foundUnknownPos = false;
                foreach (AttributeValue value in _categories[0].Values)
                {
                    if (value.Name.Equals(UnknownPosValue, StringComparison.Ordinal))
                    {
                        if (!value.PosTagging)
                        {
                            this.ErrorSet.Add(LexicalAttributeSchemaError.UnknownPosShouldBePosTaggingPos);
                        }

                        foundUnknownPos = true;
                        break;
                    }
                }

                if (!foundUnknownPos)
                {
                    this.ErrorSet.Add(LexicalAttributeSchemaError.MissUnknownPosValue, UnknownPosValue);
                }
            }
        }

        /// <summary>
        /// Check whether exists duplicate name under the category
        /// Currently we only check value under POS category, as we need to ensure unique POS values.
        /// </summary>
        /// <param name="valueSet">Set of value name.</param>
        /// <param name="category">Category.</param>
        private void CheckDuplicateValueName(Collection<string> valueSet, AttributeCategory category)
        {
            if (category != null && category.Values != null)
            {
                foreach (AttributeValue value in category.Values)
                {
                    if (valueSet.Contains(value.Name))
                    {
                        this.ErrorSet.Add(LexicalAttributeSchemaError.DuplicatePosValueName, value.Name);
                    }
                    else
                    {
                        valueSet.Add(value.Name);
                    }

                    if (value.Categories != null)
                    {
                        foreach (AttributeCategory subCategory in value.Categories)
                        {
                            CheckDuplicateValueName(valueSet, subCategory);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Validate all category names under the root.
        /// </summary>
        private void ValidateCategoryName()
        {
            Debug.Assert(_categories != null && _categories.Count > 0);
            Collection<string> categorySet = new Collection<string>();
            foreach (AttributeCategory category in _categories)
            {
                ValidateCategoryName(categorySet, category);
            }
        }

        /// <summary>
        /// Validate category values.
        /// </summary>
        private void ValidateCategoryValues()
        {
            Dictionary<string, string> categoryValues = new Dictionary<string, string>();
            foreach (AttributeCategory category in _categories)
            {
                ValidateCategoryValues(category, categoryValues);
            }
        }

        /// <summary>
        /// Validate category values.
        /// </summary>
        /// <param name="category">Categories to be validated.</param>
        /// <param name="existedValues">Existed values.</param>
        private void ValidateCategoryValues(AttributeCategory category,
            Dictionary<string, string> existedValues)
        {
            foreach (AttributeValue attributeValue in category.Values)
            {
                if (!existedValues.ContainsKey(attributeValue.Name))
                {
                    existedValues.Add(attributeValue.Name, category.Name);
                }
                else
                {
                    this.ErrorSet.Add(new Error(LexicalAttributeSchemaError.DuplicatePosValue,
                        attributeValue.Name, category.Name, existedValues[attributeValue.Name]));
                }

                if (attributeValue.Categories != null)
                {
                    foreach (AttributeCategory subCategory in attributeValue.Categories)
                    {
                        ValidateCategoryValues(subCategory, existedValues);
                    }
                }
            }
        }

        /// <summary>
        /// Validate all the category names under the category.
        /// </summary>
        /// <param name="categorySet">Set of category name.</param>
        /// <param name="category">Category.</param>
        private void ValidateCategoryName(Collection<string> categorySet, AttributeCategory category)
        {
            if (category != null)
            {
                if (categorySet.Contains(category.Name))
                {
                    this.ErrorSet.Add(LexicalAttributeSchemaError.DuplicateCategoryName, category.Name);
                }
                else
                {
                    categorySet.Add(category.Name);
                }

                if (category.Values != null)
                {
                    foreach (AttributeValue value in category.Values)
                    {
                        if (value.Categories != null)
                        {
                            foreach (AttributeCategory subCategory in value.Categories)
                            {
                                ValidateCategoryName(categorySet, subCategory);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Validate continued ID for the attribute schema.
        /// </summary>
        private void ValidateId()
        {
            int idIndex = 0;
            bool errorFound = false;
            Debug.Assert(_categories != null && _categories.Count > 0);
            foreach (AttributeCategory category in Categories)
            {
                ValidateContinuedId(category, ref idIndex, ref errorFound);
                if (errorFound)
                {
                    break;
                }
            }

            if (idIndex > _maxId)
            {
                this.ErrorSet.Add(LexicalAttributeSchemaError.IdTooLarge,
                    _maxId.ToString(CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// Validate continued ID for each node under category node.
        /// </summary>
        /// <param name="category">Category node.</param>
        /// <param name="idIndex">First id.</param>
        /// <param name="errorFound">Whether found error.</param>
        private void ValidateContinuedId(AttributeCategory category, ref int idIndex, ref bool errorFound)
        {
            if (category != null)
            {
                if (category.Id != idIndex)
                {
                    this.ErrorSet.Add(LexicalAttributeSchemaError.NotContinuedId,
                        "category=" + category.Name,
                        category.Id.ToString(CultureInfo.InvariantCulture));
                    errorFound = true;
                }
                else
                {
                    idIndex++;
                    if (idIndex > _maxId)
                    {
                        errorFound = true;
                    }
                    else if (category.Values != null)
                    {
                        foreach (AttributeValue value in category.Values)
                        {
                            if (value.Id != idIndex)
                            {
                                this.ErrorSet.Add(LexicalAttributeSchemaError.NotContinuedId, 
                                    "value=" + value.Name,
                                    value.Id.ToString(CultureInfo.InvariantCulture));
                                errorFound = true;
                            }
                            else
                            {
                                idIndex++;
                                if (idIndex > _maxId)
                                {
                                    errorFound = true;
                                }
                                else if (value.Categories != null)
                                {
                                    foreach (AttributeCategory subCategory in value.Categories)
                                    {
                                        ValidateContinuedId(subCategory, ref idIndex, ref errorFound);
                                        if (errorFound)
                                        {
                                            break;
                                        }
                                    }
                                }
                            }

                            if (errorFound)
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Load category from xml node of category.
        /// </summary>
        /// <param name="categoryNode">Xml category node.</param>
        /// <returns>Attribute category.</returns>
        private AttributeCategory LoadXmlCategoryNode(XmlNode categoryNode)
        {
            AttributeCategory category = null;
            if (categoryNode.Name.Equals("Category"))
            {
                category = new AttributeCategory();
                Debug.Assert(categoryNode.Attributes["name"] != null);
                category.Name = categoryNode.Attributes["name"].InnerText.Trim();
                Debug.Assert(categoryNode.Attributes["ID"] != null);
                category.Id = int.Parse(categoryNode.Attributes["ID"].InnerText.Trim(), 
                    NumberFormatInfo.InvariantInfo);
                
                // Permit category load mean and invStdDev
                if (categoryNode.Attributes["Mean"] != null)
                {
                    category.Mean = float.Parse(categoryNode.Attributes["Mean"].InnerText.Trim(),
                         NumberFormatInfo.InvariantInfo);
                }
                else
                {
                    category.Mean = 0.0f;
                }

                if (categoryNode.Attributes["InvStdDev"] != null)
                {
                    category.InvStdDev = float.Parse(categoryNode.Attributes["InvStdDev"].InnerText.Trim(),
                       NumberFormatInfo.InvariantInfo);
                }
                else
                {
                    category.InvStdDev = 1.0f;
                }

                // Permit category dosen't have child
                if (categoryNode.HasChildNodes)
                {
                    foreach (XmlNode valueNode in categoryNode.ChildNodes)
                    {
                        if (valueNode.NodeType == XmlNodeType.Comment)
                        {
                            continue;
                        }

                        Debug.Assert(valueNode.Name.Equals("Value"));
                        if (valueNode.Name.Equals("Value"))
                        {
                            AttributeValue value = new AttributeValue();
                            Debug.Assert(valueNode.Attributes["name"] != null);
                            value.Name = valueNode.Attributes["name"].InnerText.Trim();
                            Debug.Assert(valueNode.Attributes["ID"] != null);
                            value.Id = int.Parse(valueNode.Attributes["ID"].InnerText.Trim(),
                                NumberFormatInfo.InvariantInfo);
                            if (valueNode.Attributes["posTagging"] != null)
                            {
                                value.PosTagging = bool.Parse(valueNode.Attributes["posTagging"].InnerText.Trim());
                            }

                            if (valueNode.HasChildNodes)
                            {
                                value.UpdateCategories(new Collection<AttributeCategory>());
                                foreach (XmlNode subCategoryNode in valueNode.ChildNodes)
                                {
                                    AttributeCategory subCategory = LoadXmlCategoryNode(subCategoryNode);
                                    value.Categories.Add(subCategory);
                                }
                            }

                            category.Values.Add(value);
                        }
                    }
                }
            }

            return category;
        }

        /// <summary>
        /// Write category node into xml file.
        /// </summary>
        /// <param name="writer">Xml writer.</param>
        /// <param name="category">Category.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Ignore.")]
        private void WriteXmlCategoryNode(XmlWriter writer, AttributeCategory category)
        {
            if (category != null)
            {
                writer.WriteStartElement("Category");
                writer.WriteAttributeString("name", category.Name);
                writer.WriteAttributeString("ID", category.Id.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("Mean", category.Mean.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("InvStdDev", category.InvStdDev.ToString(CultureInfo.InvariantCulture));
                if (category.Values != null)
                {
                    foreach (AttributeValue value in category.Values)
                    {
                        writer.WriteStartElement("Value");
                        writer.WriteAttributeString("name", value.Name);
                        writer.WriteAttributeString("ID", value.Id.ToString(CultureInfo.InvariantCulture)); 
                        if (value.PosTagging)
                        {
                            writer.WriteAttributeString("posTagging", 
                                value.PosTagging.ToString().ToLower(CultureInfo.InvariantCulture));
                        }

                        if (value.Categories != null)
                        {
                            foreach (AttributeCategory subCategory in value.Categories)
                            {
                                WriteXmlCategoryNode(writer, subCategory);
                            }
                        }

                        writer.WriteEndElement();
                    }
                }

                writer.WriteEndElement();
            }
        }

        /// <summary>
        /// Generate the equation string for the value under the attribute category
        /// <param />
        /// Example:
        /// GenearteString(node of "POS", "noun")
        /// The return value is 
        /// POS=NOM=1   NOM_CLASS=noun=3.
        /// </summary>
        /// <param name="attributeCategory">Attribute category used for search.</param>
        /// <param name="value">Value.</param>
        /// <returns>Equation string for the value; for example: POS=NOM=1    NOM_CLASS=noun=3.</returns>
        private string GenerateString(AttributeCategory attributeCategory, string value)
        {
            string concatenatedString = "\t";
            string generatedString = string.Empty;
            if (attributeCategory.Values != null)
            {
                foreach (AttributeValue attributeValue in attributeCategory.Values)
                {
                    if (attributeValue.Name.Equals(value))
                    {
                        generatedString = attributeCategory.Name +
                            "=" + value + "=" + attributeValue.Id.ToString(CultureInfo.InvariantCulture);
                        break;
                    }
                    else if (attributeValue.Categories != null && attributeValue.Categories.Count > 0)
                    {
                        string appendString = string.Empty;
                        foreach (AttributeCategory subCategory in attributeValue.Categories)
                        {
                            appendString = GenerateString(subCategory, value);
                            if (!string.IsNullOrEmpty(appendString))
                            {
                                break;
                            }
                        }

                        if (!string.IsNullOrEmpty(appendString))
                        {
                            generatedString = attributeCategory.Name +
                                "=" + attributeValue.Name + "=" + 
                                attributeValue.Id.ToString(CultureInfo.InvariantCulture) +
                                concatenatedString + appendString;
                            break;
                        }
                    }
                }
            }

            return generatedString;
        }

        /// <summary>
        /// Set continued ID for each node under category node.
        /// </summary>
        /// <param name="category">Category node.</param>
        /// <param name="idIndex">First id.</param>
        private void SetContinuedId(AttributeCategory category, ref int idIndex)
        {            
            if (category != null)
            {
                category.Id = idIndex++;
                if (category.Id < _maxId && category.Values != null)
                {
                    foreach (AttributeValue value in category.Values)
                    {
                        value.Id = idIndex++;
                        if (value.Id < _maxId && value.Categories != null)
                        {
                            foreach (AttributeCategory subCategory in value.Categories)
                            {
                                SetContinuedId(subCategory, ref idIndex);
                                if (idIndex > _maxId)
                                {
                                    break;
                                }
                            }
                        }

                        if (idIndex > _maxId)
                        {
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Remove all the categories with no value under the assigned category.
        /// </summary>
        /// <param name="category">Category.</param>
        private void RemoveEmptyCategory(AttributeCategory category)
        {
            foreach (AttributeValue value in category.Values)
            {
                if (value != null && value.Categories != null && value.Categories.Count > 0)
                {
                    Collection<AttributeCategory> removedCategories = new Collection<AttributeCategory>();
                    foreach (AttributeCategory subCategory in value.Categories)
                    {
                        if (subCategory.Values == null || subCategory.Values.Count == 0)
                        {
                            removedCategories.Add(subCategory);
                        }
                        else
                        {
                            RemoveEmptyCategory(subCategory);
                        }
                    }

                    foreach (AttributeCategory subCategory in removedCategories)
                    {
                        value.Categories.Remove(subCategory);
                    }
                }

                if (value != null && value.Categories != null && value.Categories.Count == 0)
                {
                    value.UpdateCategories(null);
                }
            }
        }

        /// <summary>
        /// Check POS tagging flag for each POS value whether set to true.
        /// </summary>
        /// <param name="category">Category.</param>
        private void CheckPosTaggingFlag(AttributeCategory category)
        {
            if (category == null)
            {
                throw new ArgumentNullException("category");
            }

            foreach (AttributeValue value in category.Values)
            {
                if (!value.PosTagging)
                {
                    if (value.Categories != null && value.Categories.Count > 0)
                    {
                        foreach (AttributeCategory subCategory in value.Categories)
                        {
                            CheckPosTaggingFlag(subCategory);
                        }
                    }
                    else
                    {
                        this.ErrorSet.Add(LexicalAttributeSchemaError.MissPosTaggingSetting, value.Name);
                    }
                }
            }
        }

        #endregion
    }
}