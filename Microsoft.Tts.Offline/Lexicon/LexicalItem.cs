//----------------------------------------------------------------------------
// <copyright file="LexicalItem.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements lexicon item
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Core
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Utility;
    using SP = Microsoft.Tts.ServiceProvider;

    /// <summary>
    /// Lexicon type.
    /// </summary>
    public enum LexiconType
    {
        /// <summary>
        /// Data from LTS module.
        /// </summary>
        LetterToSound,

        /// <summary>
        /// Default lexicon.
        /// </summary>
        Application,

        /// <summary>
        /// Additional lexicon.
        /// </summary>
        Customer
    }

    /// <summary>
    /// Enum of pos error.
    /// </summary>
    public enum PosError
    {
        /// <summary>
        /// Unrecognized pos.
        /// </summary>
        [ErrorAttribute(Message = "Unrecognized pos \"{0}\"")]
        UnrecognizedPos
    }

    /// <summary>
    /// Enum of gender error.
    /// </summary>
    public enum GenderError
    {
        /// <summary>
        /// Unrecognized gender.
        /// </summary>
        [ErrorAttribute(Message = "Unrecognized gender \"{0}\"")]
        UnrecognizedGender,

        /// <summary>
        /// Duplicate gender.
        /// </summary>
        [ErrorAttribute(Message = "Duplicate gender \"{0}\"")]
        DuplicateGender,
    }

    /// <summary>
    /// Enum of case error.
    /// </summary>
    public enum CaseError
    {
        /// <summary>
        /// Unrecognized case.
        /// </summary>
        [ErrorAttribute(Message = "Unrecognized case \"{0}\"")]
        UnrecognizedCase,

        /// <summary>
        /// Duplicate case.
        /// </summary>
        [ErrorAttribute(Message = "Duplicate case \"{0}\"")]
        DuplicateCase,
    }

    /// <summary>
    /// Enum of number error.
    /// </summary>
    public enum NumberError
    {
        /// <summary>
        /// Unrecognized number.
        /// </summary>
        [ErrorAttribute(Message = "Unrecognized number \"{0}\"")]
        UnrecognizedNumber,

        /// <summary>
        /// Duplicate number.
        /// </summary>
        [ErrorAttribute(Message = "Duplicate number \"{0}\"")]
        DuplicateNumber,
    }

    /// <summary>
    /// Enum of domain error.
    /// </summary>
    public enum DomainError
    {
        /// <summary>
        /// Empty domain.
        /// </summary>
        [ErrorAttribute(Message = "Empty domain value")]
        EmptyDomain,

        /// <summary>
        /// Duplicate domain.
        /// </summary>
        [ErrorAttribute(Message = "Duplicate domain \"{0}\"")]
        DuplicateDomain,

        /// <summary>
        /// Invalid domain tags.
        /// </summary>
        [ErrorAttribute(Message = "Domain tags should not be both in lexicon and property level")]
        InvalidDomainTags,
    }

    /// <summary>
    /// HistoryValue class manages tracking value change history.
    /// </summary>
    public class HistoryValue
    {
        /// <summary>
        /// Current value field.
        /// </summary>
        protected string currentValue;

        /// <summary>
        /// Old value field.
        /// </summary>
        protected string originalValue;

        #region Properties

        /// <summary>
        /// Gets or sets Current value property.
        /// </summary>
        public virtual string Value
        {
            get { return currentValue; }
            set { currentValue = value; }
        }

        /// <summary>
        /// Gets or sets Old value property.
        /// </summary>
        public virtual string OldValue
        {
            get { return originalValue; }
            set { originalValue = value; }
        }

        /// <summary>
        /// Gets or sets LexiconStatus.
        /// </summary>
        public Lexicon.LexiconStatus Status { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Compare objects that derived from HistoryValue.
        /// </summary>
        /// <param name="obj1">Object 1.</param>
        /// <param name="obj2">Object 2.</param>
        /// <returns>true for equal
        ///     (null, null) => equal
        ///     (null, deleted) => equal
        ///     (deleted, deleted) => equal.
        /// </returns>
        public static bool Equals(HistoryValue obj1, HistoryValue obj2)
        {
            bool equal = false;

            if (obj1 != null)
            {
                if (obj2 != null)
                {
                    equal = obj1.Equals(obj2);
                }
                else
                {
                    if (obj1.Status == Lexicon.LexiconStatus.Deleted)
                    {
                        // deleted == null
                        equal = true;
                    }
                }
            }
            else if (obj2 != null)
            {
                if (obj2.Status == Lexicon.LexiconStatus.Deleted)
                {
                    // null == deleted
                    equal = true;
                }
            }
            else
            {
                // null == null
                equal = true;
            }

            return equal;
        }

        /// <summary>
        /// Implicit convert from HistoryValue to string.
        /// </summary>
        /// <param name="value">HistoryValue.</param>
        /// <returns>String value.</returns>
        public static implicit operator string(HistoryValue value)
        {
            return value.Value;
        }

        /// <summary>
        /// Implicit convert from string to HistoryValue.
        /// </summary>
        /// <param name="value">String.</param>
        /// <returns>HistoryValue value.</returns>
        public static implicit operator HistoryValue(string value)
        {
            HistoryValue historyValue = new HistoryValue();
            historyValue.Value = value;
            return historyValue;
        }

        /// <summary>
        /// Remove change history.
        /// </summary>
        public void RemoveHistory()
        {
            OldValue = Value;
            Status = Lexicon.LexiconStatus.Original;
        }

        /// <summary>
        /// Change to new value.
        /// </summary>
        /// <param name="newValue">New value.</param>
        public void Change(string newValue)
        {
            Helper.ThrowIfNull(newValue);
            Value = newValue;
            if (Status == Lexicon.LexiconStatus.Added)
            {
                OldValue = newValue;
            }
            else
            {
                Status = Value == OldValue ? Lexicon.LexiconStatus.Original : Lexicon.LexiconStatus.Changed;
            }
        }

        /// <summary>
        /// Copy function.
        /// </summary>
        /// <param name="historyValue">HistoryValue.</param>
        public void CopyTo(HistoryValue historyValue)
        {
            if (historyValue == null)
            {
                throw new ArgumentNullException();
            }

            historyValue.Value = Value;
            historyValue.OldValue = OldValue;
            historyValue.Status = Status;
        }

        /// <summary>
        /// Override the Equals function.
        /// </summary>
        /// <param name="obj">Object.</param>
        /// <returns>True for equal.</returns>
        public override bool Equals(object obj)
        {
            bool equal = false;

            HistoryValue other = obj as HistoryValue;
            if (other != null)
            {
                if (Status == Lexicon.LexiconStatus.Deleted &&
                    other.Status == Lexicon.LexiconStatus.Deleted)
                {
                    // Both deleted treated as equal. Because they're the same when compile into lexicon binary.
                    equal = true;
                }
                else if (Status != Lexicon.LexiconStatus.Deleted &&
                    other.Status != Lexicon.LexiconStatus.Deleted)
                {
                    // Only compare value here.
                    // Because OldValue and Status(Add/Changed/Checked) will not affect the actual result.
                    equal = Value.Equals(other.Value);
                }
            }

            return equal;
        }

        /// <summary>
        /// Override the GetHashCode function.
        /// </summary>
        /// <returns>Hash code.</returns>
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        #endregion
    }

    /// <summary>
    /// Class of PropertyItem.
    /// </summary>
    public class PropertyItem : HistoryValue
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyItem"/> class.
        /// </summary>
        public PropertyItem()
            : this(string.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyItem"/> class.
        /// </summary>
        /// <param name="value">Value of PropertyItem.</param>
        public PropertyItem(string value)
            : this(value, value)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyItem"/> class.
        /// </summary>
        /// <param name="oldValue">Old value.</param>
        /// <param name="value">Current Value.</param>
        public PropertyItem(string oldValue, string value)
        {
            OldValue = string.IsNullOrEmpty(oldValue) ? string.Empty : oldValue.Trim();
            Value = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
        }

        #endregion
    }

    /// <summary>
    /// POS item.
    /// </summary>
    public class PosItem : PropertyItem
    {
        #region Fields

        private PartOfSpeech _pos;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PosItem"/> class.
        /// </summary>
        /// <param name="oldValue">Old value.</param>
        /// <param name="value">Current Value.</param>
        public PosItem(string oldValue, string value)
            : base(oldValue, value)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PosItem"/> class.
        /// </summary>
        /// <param name="value">Value.</param>
        public PosItem(string value)
            : base(value)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PosItem"/> class.
        /// </summary>
        public PosItem()
        {
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Pos, this property is overdue now.
        /// </summary>
        public PartOfSpeech Pos
        {
            get { return _pos; }
            set { _pos = value; }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Validate pos string.
        /// </summary>
        /// <param name="posStr">Pos string to be validated.</param>
        /// <param name="ttsPosSet">Tts pos set for validation.</param>
        /// <param name="attributeSchema">Lexicon attribute schema for validation.</param>
        /// <returns>Validate result.</returns>
        public static ErrorSet Validate(string posStr, TtsPosSet ttsPosSet,
            LexicalAttributeSchema attributeSchema)
        {
            ErrorSet errorSet = new ErrorSet();
            if ((attributeSchema != null && string.IsNullOrEmpty(attributeSchema.GenerateString(
                LexicalAttributeSchema.PosCategoryName, posStr))) ||
                (ttsPosSet != null && !ttsPosSet.Items.ContainsKey(posStr)))
            {
                errorSet.Add(PosError.UnrecognizedPos, posStr);
            }

            return errorSet;
        }

        /// <summary>
        /// Clone function.
        /// </summary>
        /// <returns>PosItem.</returns>
        public PosItem Clone()
        {
            PosItem clonedItem = new PosItem();
            this.CopyTo(clonedItem);
            clonedItem.Pos = _pos;
            return clonedItem;
        }

        #endregion

        #region Internal methods

        /// <summary>
        /// Load PosItem from XmlNode.
        /// </summary>
        /// <param name="propertyNode">XmlNode.</param>
        /// <param name="nsmgr">XmlNamespaceManager.</param>
        /// <returns>PosItem.</returns>
        internal static PosItem Load(XmlNode propertyNode, XmlNamespaceManager nsmgr)
        {
            Debug.Assert(propertyNode != null && nsmgr != null);

            PosItem posItem = null;
            if (propertyNode.SelectSingleNode("tts:pos", nsmgr) != null)
            {
                posItem = new PosItem(propertyNode.SelectSingleNode("tts:pos/@v", nsmgr).InnerText);
                XmlNode originalValueNode = propertyNode.SelectSingleNode("tts:pos/@vo", nsmgr);
                if (originalValueNode != null && !string.IsNullOrEmpty(originalValueNode.InnerText))
                {
                    posItem.OldValue = originalValueNode.InnerText;
                }
            }

            return posItem;
        }

        /// <summary>
        /// Write a pos item to the XML writer.
        /// </summary>
        /// <param name="writer">XML writer.</param>
        internal void WriteToXml(XmlWriter writer)
        {
            // Write out POS information if present
            if (!string.IsNullOrEmpty(Value))
            {
                writer.WriteStartElement("pos");
                writer.WriteAttributeString("v", Value);
                if (!string.IsNullOrEmpty(OldValue) &&
                    !Value.Equals(OldValue, StringComparison.Ordinal))
                {
                    writer.WriteAttributeString("vo", OldValue);
                }

                writer.WriteEndElement();
            }
        }

        #endregion
    }

    /// <summary>
    /// Gender item.
    /// </summary>
    public class GenderItem : PropertyItem
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GenderItem"/> class.
        /// </summary>
        /// <param name="oldValue">Old value.</param>
        /// <param name="value">Current Value.</param>
        public GenderItem(string oldValue, string value)
            : base(oldValue, value)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GenderItem"/> class.
        /// </summary>
        /// <param name="value">Value.</param>
        public GenderItem(string value)
            : base(value)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GenderItem"/> class.
        /// </summary>
        public GenderItem()
        {
        }

        #endregion

        #region Enums

        /// <summary>
        /// Three genders: use one bit for one gender
        /// Can be combinations of them.
        /// </summary>
        [FlagsAttribute]
        public enum Gender
        {
            /// <summary>
            /// Not support for gender.
            /// </summary>
            None = 0,

            /// <summary>
            /// Feminine.
            /// </summary>
            Feminine = 1,

            /// <summary>
            /// Masculine.
            /// </summary>
            Masculine = 2,

            /// <summary>
            /// Neuter.
            /// </summary>
            Neuter = 4,
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Validate gender string.
        /// </summary>
        /// <param name="genderStr">Gender string to be validated.</param>
        /// <returns>Validate error set.</returns>
        public static ErrorSet Validate(string genderStr)
        {
            int id;
            ErrorSet errorSet = GenderItem.StringToId(genderStr, out id);
            return errorSet;
        }

        /// <summary>
        /// Convert the gender string to gender id.
        /// </summary>
        /// <param name="gender">Gender string.</param>
        /// <param name="id">Gender id.</param>
        /// <returns>Error set.</returns>
        public static ErrorSet StringToId(string gender, out int id)
        {
            if (string.IsNullOrEmpty(gender))
            {
                throw new ArgumentNullException("gender");
            }

            id = 0;
            ErrorSet errorSet = new ErrorSet();
            for (int i = 0; i < gender.Length; i++)
            {
                Gender genderId = GetId(gender[i]);
                if (genderId == Gender.None)
                {
                    errorSet.Add(GenderError.UnrecognizedGender, gender[i].ToString());
                }
                else if ((((int)genderId) & id) != 0)
                {
                    errorSet.Add(GenderError.DuplicateGender, gender[i].ToString());
                }
                else
                {
                    id |= (int)genderId;
                }
            }

            return errorSet;
        }

        /// <summary>
        /// Get the gender according to the gender character.
        /// </summary>
        /// <param name="gender">Gender character.</param>
        /// <returns>Gender.</returns>
        public static Gender GetId(char gender)
        {
            Gender genderId = Gender.None;
            switch (gender)
            {
                case 'F':
                case 'f':
                    genderId = Gender.Feminine;
                    break;
                case 'M':
                case 'm':
                    genderId = Gender.Masculine;
                    break;
                case 'U':
                case 'u':
                    genderId = Gender.Neuter;
                    break;
                case 'N':
                case 'n':
                    genderId = Gender.Feminine | Gender.Masculine;
                    break;
            }

            return genderId;
        }

        /// <summary>
        /// Convert the gender string into gender list.
        /// </summary>
        /// <param name="gender">Gender string.</param>
        /// <param name="errorSet">Error set.</param>
        /// <returns>Gender list.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Ignore.")]
        public static ArrayList ConvertIntoArray(string gender, ErrorSet errorSet)
        {
            if (errorSet == null)
            {
                throw new ArgumentNullException("errorSet");
            }

            ArrayList arrayList = new ArrayList();

            int id = 0;
            errorSet.Merge(StringToId(gender, out id));
            foreach (int i in Enum.GetValues(typeof(Gender)))
            {
                if ((id & i) != 0)
                {
                    arrayList.Add(Enum.GetName(typeof(Gender), i).ToLower(CultureInfo.InvariantCulture));
                }
            }

            return arrayList;
        }

        /// <summary>
        /// Clone function.
        /// </summary>
        /// <returns>GenderItem.</returns>
        public GenderItem Clone()
        {
            GenderItem clonedItem = new GenderItem();
            this.CopyTo(clonedItem);
            return clonedItem;
        }

        /// <summary>
        /// Convert the gender item to attributeitem collection.
        /// </summary>
        /// <param name="errorSet">The Error set.</param>
        /// <returns>Attribute item collection.</returns>
        public Collection<AttributeItem> ToNewFormatAttribute(ErrorSet errorSet)
        {
            Collection<AttributeItem> convertedAttributes = new Collection<AttributeItem>();
            ArrayList genderValueList = ConvertIntoArray(Value, errorSet);
            foreach (object obj in genderValueList)
            {
                AttributeItem attributeItem = new AttributeItem(AttributeItem.GenderCategoryName,
                    obj.ToString());
                attributeItem.Status = Status;
                convertedAttributes.Add(attributeItem);
            }

            return convertedAttributes;
        }

        #endregion

        #region Internal methods

        /// <summary>
        /// Load GenderItem from XmlNode.
        /// </summary>
        /// <param name="propertyNode">XmlNode.</param>
        /// <param name="nsmgr">XmlNamespaceManager.</param>
        /// <returns>GenderItem.</returns>
        internal static GenderItem Load(XmlNode propertyNode, XmlNamespaceManager nsmgr)
        {
            Debug.Assert(propertyNode != null && nsmgr != null);

            GenderItem genderItem = null;
            if (propertyNode.SelectSingleNode("tts:gender", nsmgr) != null)
            {
                genderItem = new GenderItem(propertyNode.SelectSingleNode("tts:gender/@v", nsmgr).InnerText);
                XmlNode originalValueNode = propertyNode.SelectSingleNode("tts:gender/@vo", nsmgr);
                if (originalValueNode != null && !string.IsNullOrEmpty(originalValueNode.InnerText))
                {
                    genderItem.OldValue = originalValueNode.InnerText;
                }
            }

            return genderItem;
        }

        /// <summary>
        /// Write a gender item to the XML writer.
        /// </summary>
        /// <param name="writer">XML writer.</param>
        internal void WriteToXml(XmlWriter writer)
        {
            // Write out gender information if present
            if (!string.IsNullOrEmpty(Value))
            {
                writer.WriteStartElement("gender");
                writer.WriteAttributeString("v", Value);
                if (!string.IsNullOrEmpty(OldValue) &&
                    !Value.Equals(OldValue, StringComparison.Ordinal))
                {
                    writer.WriteAttributeString("vo", OldValue);
                }

                writer.WriteEndElement();
            }
        }

        #endregion
    }

    /// <summary>
    /// Case item.
    /// </summary>
    public class CaseItem : PropertyItem
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CaseItem"/> class.
        /// </summary>
        /// <param name="oldValue">Old value.</param>
        /// <param name="value">Current Value.</param>
        public CaseItem(string oldValue, string value)
            : base(oldValue, value)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CaseItem"/> class.
        /// </summary>
        /// <param name="value">Value.</param>
        public CaseItem(string value)
            : base(value)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CaseItem"/> class.
        /// </summary>
        public CaseItem()
        {
        }

        #endregion

        #region Enums

        /// <summary>
        /// Case types
        ///  n       Nominative
        ///  g       Genitive
        ///  d       Dative
        ///  a       Accusative.
        /// </summary>
        [FlagsAttribute]
        public enum GrammaticalCase
        {
            /// <summary>
            /// Not support for case.
            /// </summary>
            NotSupport = 0,

            /// <summary>
            /// Nominative.
            /// </summary>
            Nominative = 1,

            /// <summary>
            /// Genitive.
            /// </summary>
            Genitive = 2,

            /// <summary>
            /// Dative.
            /// </summary>
            Dative = 4,

            /// <summary>
            /// Accusative.
            /// </summary>
            Accusative = 8,
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Validate case string.
        /// </summary>
        /// <param name="caseStr">Case string to be validated.</param>
        /// <returns>Validate error set.</returns>
        public static ErrorSet Validate(string caseStr)
        {
            int caseId;
            ErrorSet errorSet = CaseItem.StringToId(caseStr, out caseId);
            return errorSet;
        }

        /// <summary>
        /// Convert the case string to case id. If all fail, id will be 0.
        /// </summary>
        /// <param name="caseStr">Case string.</param>
        /// <param name="id">Case id.</param>
        /// <returns>Error set.</returns>
        public static ErrorSet StringToId(string caseStr, out int id)
        {
            if (string.IsNullOrEmpty(caseStr))
            {
                throw new ArgumentNullException("caseStr");
            }

            id = 0;
            ErrorSet errorSet = new ErrorSet();
            for (int i = 0; i < caseStr.Length; i++)
            {
                int caseId = (int)GetId(caseStr[i]);
                if (caseId == 0)
                {
                    errorSet.Add(CaseError.UnrecognizedCase, caseStr[i].ToString());
                }
                else if ((caseId & id) != 0)
                {
                    errorSet.Add(CaseError.DuplicateCase, caseStr[i].ToString());
                }
                else
                {
                    id |= caseId;
                }
            }

            return errorSet;
        }

        /// <summary>
        /// Get the case according to the case character.
        /// </summary>
        /// <param name="grammaticalCase">Grammatical Case character.</param>
        /// <returns>Case.</returns>
        public static GrammaticalCase GetId(char grammaticalCase)
        {
            GrammaticalCase caseId = GrammaticalCase.NotSupport;
            switch (grammaticalCase)
            {
                case 'n':
                case 'N':
                    caseId = GrammaticalCase.Nominative;
                    break;
                case 'g':
                case 'G':
                    caseId = GrammaticalCase.Genitive;
                    break;
                case 'd':
                case 'D':
                    caseId = GrammaticalCase.Dative;
                    break;
                case 'a':
                case 'A':
                    caseId = GrammaticalCase.Accusative;
                    break;
            }

            return caseId;
        }

        /// <summary>
        /// Convert the case string into the case list.
        /// </summary>
        /// <param name="grammaticalCase">Grammatical Case string.</param>
        /// <param name="errorSet">Error set.</param>
        /// <returns>Case list.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Ignore.")]
        public static ArrayList ConvertIntoArray(string grammaticalCase, ErrorSet errorSet)
        {
            if (errorSet == null)
            {
                throw new ArgumentNullException("errorSet");
            }

            ArrayList arrayList = new ArrayList();

            int id = 0;
            errorSet.Merge(StringToId(grammaticalCase, out id));
            foreach (int i in Enum.GetValues(typeof(GrammaticalCase)))
            {
                if ((id & i) != 0)
                {
                    arrayList.Add(Enum.GetName(typeof(GrammaticalCase), i).ToLower(CultureInfo.InvariantCulture));
                }
            }

            return arrayList;
        }

        /// <summary>
        /// Clone function.
        /// </summary>
        /// <returns>CaseItem.</returns>
        public CaseItem Clone()
        {
            CaseItem clonedItem = new CaseItem();
            this.CopyTo(clonedItem);
            return clonedItem;
        }

        /// <summary>
        /// Convert the case item to attributeitem collection.
        /// </summary>
        /// <param name="errorSet">The Error set.</param>
        /// <returns>Attribute item collection.</returns>
        public Collection<AttributeItem> ToNewFormatAttribute(ErrorSet errorSet)
        {
            Collection<AttributeItem> convertedAttributes = new Collection<AttributeItem>();
            ArrayList caseValueList = ConvertIntoArray(Value, errorSet);
            foreach (object obj in caseValueList)
            {
                AttributeItem attributeItem = new AttributeItem(AttributeItem.CaseCategoryName,
                    obj.ToString());
                attributeItem.Status = Status;
                convertedAttributes.Add(attributeItem);
            }

            return convertedAttributes;
        }

        #endregion

        #region Internal methods

        /// <summary>
        /// Load CaseItem from XmlNode.
        /// </summary>
        /// <param name="propertyNode">XmlNode.</param>
        /// <param name="nsmgr">XmlNamespaceManager.</param>
        /// <returns>CaseItem.</returns>
        internal static CaseItem Load(XmlNode propertyNode, XmlNamespaceManager nsmgr)
        {
            Debug.Assert(propertyNode != null && nsmgr != null);

            CaseItem caseItem = null;
            if (propertyNode.SelectSingleNode("tts:case", nsmgr) != null)
            {
                caseItem = new CaseItem(propertyNode.SelectSingleNode("tts:case/@v", nsmgr).InnerText);
                XmlNode originalValueNode = propertyNode.SelectSingleNode("tts:case/@vo", nsmgr);
                if (originalValueNode != null && !string.IsNullOrEmpty(originalValueNode.InnerText))
                {
                    caseItem.OldValue = originalValueNode.InnerText;
                }
            }

            return caseItem;
        }

        /// <summary>
        /// Write a case item to the XML writer.
        /// </summary>
        /// <param name="writer">XML writer.</param>
        internal void WriteToXml(XmlWriter writer)
        {
            // Write out case information if present
            if (!string.IsNullOrEmpty(Value))
            {
                writer.WriteStartElement("case");
                writer.WriteAttributeString("v", Value);
                if (!string.IsNullOrEmpty(OldValue) &&
                    !Value.Equals(OldValue, StringComparison.Ordinal))
                {
                    writer.WriteAttributeString("vo", OldValue);
                }

                writer.WriteEndElement();
            }
        }

        #endregion
    }

    /// <summary>
    /// Number item.
    /// </summary>
    public class NumberItem : PropertyItem
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="NumberItem"/> class.
        /// </summary>
        /// <param name="oldValue">Old value.</param>
        /// <param name="value">Current Value.</param>
        public NumberItem(string oldValue, string value)
            : base(oldValue, value)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NumberItem"/> class.
        /// </summary>
        /// <param name="value">Value.</param>
        public NumberItem(string value)
            : base(value)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NumberItem"/> class.
        /// </summary>
        public NumberItem()
        {
        }

        #endregion

        #region Enums

        /// <summary>
        /// Number.
        /// </summary>
        public enum Number
        {
            /// <summary>
            /// Not support for number.
            /// </summary>
            NotSupport = 0,

            /// <summary>
            /// Singular, S.
            /// </summary>
            Singular = 1,

            /// <summary>
            /// Plural, P.
            /// </summary>
            Plural = 2,
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Convert the number string to gender id.
        /// </summary>
        /// <param name="number">Number string.</param>
        /// <param name="id">Number id.</param>
        /// <returns>Error set.</returns>
        public static ErrorSet StringToId(string number, out int id)
        {
            if (string.IsNullOrEmpty(number))
            {
                throw new ArgumentNullException("number");
            }

            id = 0;
            ErrorSet errorSet = new ErrorSet();
            for (int i = 0; i < number.Length; i++)
            {
                Number numberId = GetId(number[i]);
                if (numberId == Number.NotSupport)
                {
                    errorSet.Add(NumberError.UnrecognizedNumber, number[i].ToString());
                }
                else if ((((int)numberId) & id) != 0)
                {
                    errorSet.Add(NumberError.DuplicateNumber, number[i].ToString());
                }
                else
                {
                    id |= (int)numberId;
                }
            }

            return errorSet;
        }

        /// <summary>
        /// Get the number according to the number character.
        /// </summary>
        /// <param name="number">Number character.</param>
        /// <returns>Number.</returns>
        public static Number GetId(char number)
        {
            Number numberId = Number.NotSupport;
            switch (number)
            {
                case 'S':
                case 's':
                    numberId = Number.Singular;
                    break;
                case 'P':
                case 'p':
                    numberId = Number.Plural;
                    break;
            }

            return numberId;
        }

        /// <summary>
        /// Convert the number string into number list.
        /// </summary>
        /// <param name="number">Number string.</param>
        /// <param name="errorSet">Error set.</param>
        /// <returns>Number list.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Ignore.")]
        public static ArrayList ConvertIntoArray(string number, ErrorSet errorSet)
        {
            if (errorSet == null)
            {
                throw new ArgumentNullException("errorSet");
            }

            ArrayList arrayList = new ArrayList();

            int id = 0;
            errorSet.Merge(StringToId(number, out id));
            foreach (int i in Enum.GetValues(typeof(Number)))
            {
                if ((id & i) != 0)
                {
                    arrayList.Add(Enum.GetName(typeof(Number), i).ToLower(CultureInfo.InvariantCulture));
                }
            }

            return arrayList;
        }

        /// <summary>
        /// Clone function.
        /// </summary>
        /// <returns>NumberItem.</returns>
        public NumberItem Clone()
        {
            NumberItem clonedItem = new NumberItem();
            this.CopyTo(clonedItem);
            return clonedItem;
        }

        /// <summary>
        /// Convert the number item to attributeitem collection.
        /// </summary>
        /// <param name="errorSet">The Error set.</param>
        /// <returns>Attribute item collection.</returns>
        public Collection<AttributeItem> ToNewFormatAttribute(ErrorSet errorSet)
        {
            Collection<AttributeItem> convertedAttributes = new Collection<AttributeItem>();
            ArrayList numberValueList = ConvertIntoArray(Value, errorSet);
            foreach (object obj in numberValueList)
            {
                AttributeItem attributeItem = new AttributeItem(AttributeItem.NumberCategoryName,
                    obj.ToString());
                attributeItem.Status = Status;
                convertedAttributes.Add(attributeItem);
            }

            return convertedAttributes;
        }

        #endregion

        #region Internal methods

        /// <summary>
        /// Load NumberItem from XmlNode.
        /// </summary>
        /// <param name="propertyNode">XmlNode.</param>
        /// <param name="nsmgr">XmlNamespaceManager.</param>
        /// <returns>NumberItem.</returns>
        internal static NumberItem Load(XmlNode propertyNode, XmlNamespaceManager nsmgr)
        {
            Debug.Assert(propertyNode != null && nsmgr != null);

            NumberItem numberItem = null;
            if (propertyNode.SelectSingleNode("tts:number", nsmgr) != null)
            {
                numberItem = new NumberItem(propertyNode.SelectSingleNode("tts:number/@v", nsmgr).InnerText);
                XmlNode originalValueNode = propertyNode.SelectSingleNode("tts:number/@vo", nsmgr);
                if (originalValueNode != null && !string.IsNullOrEmpty(originalValueNode.InnerText))
                {
                    numberItem.OldValue = originalValueNode.InnerText;
                }
            }

            return numberItem;
        }

        /// <summary>
        /// Write a number item to the XML writer.
        /// </summary>
        /// <param name="writer">XML writer.</param>
        internal void WriteToXml(XmlWriter writer)
        {
            // Write out number information if present
            if (!string.IsNullOrEmpty(Value))
            {
                writer.WriteStartElement("number");
                writer.WriteAttributeString("v", Value);
                if (!string.IsNullOrEmpty(OldValue) &&
                    !Value.Equals(OldValue, StringComparison.Ordinal))
                {
                    writer.WriteAttributeString("vo", OldValue);
                }

                writer.WriteEndElement();
            }
        }

        #endregion
    }

    /// <summary>
    /// Domain item.
    /// </summary>
    public class DomainItem : HistoryValue
    {
        /// <summary>
        /// Default general domain value.
        /// </summary>
        public const string GeneralDomain = "general";

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainItem"/> class.
        /// </summary>
        /// <param name="oldValue">Old value.</param>
        /// <param name="value">Current Value.</param>
        public DomainItem(string oldValue, string value)
        {
            Helper.ThrowIfNull(oldValue);
            Helper.ThrowIfNull(value);

            OldValue = oldValue;
            Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainItem"/> class.
        /// </summary>
        /// <param name="value">Value.</param>
        public DomainItem(string value)
            : this(value, value)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainItem"/> class.
        /// </summary>
        public DomainItem()
            : this(DomainItem.GeneralDomain)
        {
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Value.
        /// </summary>
        public override string Value
        {
            get
            {
                return currentValue;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException();
                }

                currentValue = value.ToLowerInvariant();
            }
        }

        /// <summary>
        /// Gets or sets Old value.
        /// </summary>
        public override string OldValue
        {
            get
            {
                return originalValue;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException();
                }

                originalValue = value.ToLowerInvariant();
            }
        }

        /// <summary>
        /// Gets a value indicating whether Checked flag.
        /// </summary>
        public bool Checked
        {
            get { return Status != Lexicon.LexiconStatus.Original; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether unified pronunciation contains this DomainItem is
        /// Prefered as the first pronunciation in the domain lexicon.
        /// </summary>
        public bool IsFirstPronunciation { get; set; }

        #endregion

        #region Public methods

        /// <summary>
        /// Clone current domain.
        /// </summary>
        /// <returns>Cloned DomainItem.</returns>
        public DomainItem Clone()
        {
            DomainItem clonedItem = new DomainItem();
            this.CopyTo(clonedItem);
            clonedItem.IsFirstPronunciation = IsFirstPronunciation;
            return clonedItem;
        }

        #endregion

        #region Internal methods

        /// <summary>
        /// Load DomainItem.
        /// </summary>
        /// <param name="parentProperty">LexiconItemProperty.</param>
        /// <param name="domainNode">XmlNode.</param>
        /// <param name="nsmgr">XmlNamespaceManager.</param>
        /// <param name="contentController">Object.</param>
        /// <param name="errorSet">ErrorSet.</param>
        /// <returns>DomainItem.</returns>
        internal static DomainItem Load(LexiconItemProperty parentProperty, XmlNode domainNode, XmlNamespaceManager nsmgr, Lexicon.ContentControler contentController, ErrorSet errorSet)
        {
            Debug.Assert(parentProperty != null && parentProperty.Parent != null && parentProperty.Parent.Parent != null &&
                domainNode != null && contentController != null && nsmgr != null);

            DomainItem domainItem = new DomainItem();
            XmlElement domainElem = domainNode as XmlElement;
            Debug.Assert(domainElem != null);

            string domainStatusValue = domainElem.GetAttribute("s");
            if (!string.IsNullOrEmpty(domainStatusValue))
            {
                domainItem.Status = (Lexicon.LexiconStatus)Enum.Parse(typeof(Lexicon.LexiconStatus), domainStatusValue, true);

                // Lexicon object is shared with lexicon reviewer tool,
                // We drop those items if they have "deleted" status when it is not loaded by lexicon reviewer tool
                if (domainItem.Status == Lexicon.LexiconStatus.Deleted && !contentController.IsHistoryCheckingMode)
                {
                    domainItem = null;
                }
            }

            if (domainItem != null)
            {
                // Check whether pronunciation is prefered in this domain
                string preferedValue = domainElem.GetAttribute("p");
                if (!string.IsNullOrEmpty(preferedValue))
                {
                    domainItem.IsFirstPronunciation = bool.Parse(preferedValue);
                }

                string domainValue = domainElem.GetAttribute("v");
                string originalDomainValue = domainElem.GetAttribute("vo");
                if (string.IsNullOrEmpty(domainValue))
                {
                    Error error = new Error(DomainError.EmptyDomain);
                    errorSet.Add(LexiconError.DomainError,
                        error, parentProperty.Parent.Parent.Text, parentProperty.Parent.Symbolic);
                    domainItem = null;
                }
                else
                {
                    domainItem.Value = domainValue.ToLower();
                    if (!string.IsNullOrEmpty(originalDomainValue) &&
                        domainItem.Status != Lexicon.LexiconStatus.Original)
                    {
                        domainItem.OldValue = originalDomainValue.ToLower();
                    }
                    else
                    {
                        domainItem.OldValue = domainValue;
                    }
                }
            }

            return domainItem;
        }

        /// <summary>
        /// Write a domain item to the XML writer.
        /// </summary>
        /// <param name="writer">XML writer.</param>
        internal void WriteToXml(XmlWriter writer)
        {
            writer.WriteStartElement("domain");
            writer.WriteAttributeString("v", Value);
            if (Status != Lexicon.LexiconStatus.Original)
            {
                writer.WriteAttributeString("s", Status.ToString());
                if (!string.IsNullOrEmpty(OldValue) && !Value.Equals(OldValue, StringComparison.Ordinal))
                {
                    writer.WriteAttributeString("vo", OldValue);
                }
            }

            if (IsFirstPronunciation)
            {
                writer.WriteAttributeString("p", "true");
            }

            writer.WriteEndElement();
        }

        #endregion
    }

    /// <summary>
    /// Class of AttributeItem.
    /// </summary>
    public class AttributeItem : PropertyItem
    {
        #region Fileds

        /// <summary>
        /// Gender category name.
        /// </summary>
        public const string GenderCategoryName = "F_GENDER";

        /// <summary>
        /// Case category name.
        /// </summary>
        public const string CaseCategoryName = "F_CASE";

        /// <summary>
        /// Number category name.
        /// </summary>
        public const string NumberCategoryName = "F_NUMBER";

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeItem"/> class.
        /// </summary>
        public AttributeItem()
            : this(string.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeItem"/> class.
        /// </summary>
        /// <param name="categoryName">CategoryName of AttributeItem.</param>
        public AttributeItem(string categoryName)
            : this(categoryName, string.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeItem"/> class.
        /// </summary>
        /// <param name="categoryName">Category name.</param>
        /// <param name="categoryValue">Category value.</param>
        public AttributeItem(string categoryName, string categoryValue)
        {
            CategoryName = categoryName;
            Value = categoryValue;
            OldValue = categoryValue;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets CategoryName.
        /// </summary>
        public string CategoryName { get; set; }

        #endregion

        #region Public methods

        /// <summary>
        /// Clone current attribute.
        /// </summary>
        /// <returns>Cloned AttributeItem.</returns>
        public AttributeItem Clone()
        {
            AttributeItem clonedItem = new AttributeItem();
            this.CopyTo(clonedItem);
            clonedItem.CategoryName = CategoryName;
            return clonedItem;
        }

        /// <summary>
        /// Override the Equals function.
        /// </summary>
        /// <param name="obj">Object.</param>
        /// <returns>True for equal.</returns>
        public override bool Equals(object obj)
        {
            bool equal = false;

            AttributeItem other = obj as AttributeItem;
            if (other != null)
            {
                if (Status == Lexicon.LexiconStatus.Deleted &&
                    other.Status == Lexicon.LexiconStatus.Deleted)
                {
                    // Both deleted treated as equal. Because they're the same when compile into lexicon binary.
                    equal = true;
                }
                else if (Status != Lexicon.LexiconStatus.Deleted &&
                    other.Status != Lexicon.LexiconStatus.Deleted)
                {
                    if (base.Equals(obj) && CategoryName == other.CategoryName)
                    {
                        equal = true;
                    }
                }
            }

            return equal;
        }

        /// <summary>
        /// Override the GetHashCode function.
        /// </summary>
        /// <returns>Hash code.</returns>
        public override int GetHashCode()
        {
            return (base.GetHashCode() << 5) + CategoryName.GetHashCode();
        }

        #endregion

        #region Internal methods

        /// <summary>
        /// Load AttributeItem from XmlNode.
        /// </summary>
        /// <param name="parentProperty">LexiconItemProperty.</param>
        /// <param name="attributeNode">XmlNode.</param>
        /// <param name="nsmgr">XmlNamespaceManager.</param>
        /// <param name="contentController">Object.</param>
        /// <param name="errorSet">ErrorSet.</param>
        /// <returns>AttributeItem.</returns>
        internal static AttributeItem Load(LexiconItemProperty parentProperty, XmlNode attributeNode, XmlNamespaceManager nsmgr, Lexicon.ContentControler contentController, ErrorSet errorSet)
        {
            Debug.Assert(parentProperty != null && parentProperty.Parent != null && parentProperty.Parent.Parent != null &&
                attributeNode != null && contentController != null && nsmgr != null);

            AttributeItem attributeItem = new AttributeItem();

            XmlElement attributeElem = attributeNode as XmlElement;
            Debug.Assert(attributeElem != null);
            string attrStatusValue = attributeElem.GetAttribute("s");
            if (!string.IsNullOrEmpty(attrStatusValue))
            {
                attributeItem.Status = (Lexicon.LexiconStatus)Enum.Parse(
                    typeof(Lexicon.LexiconStatus), attrStatusValue, true);

                // Lexicon object is shared with lexicon reviewer tool,
                // We drop those items if they have "deleted" status when it is not loaded by lexicon reviewer tool
                if (attributeItem.Status == Lexicon.LexiconStatus.Deleted && !contentController.IsHistoryCheckingMode)
                {
                    attributeItem = null;
                }
            }

            if (attributeItem != null)
            {
                string category = attributeElem.GetAttribute("category");
                string value = attributeElem.GetAttribute("value");
                string originalValue = attributeElem.GetAttribute("vo");

                if (string.IsNullOrEmpty(category))
                {
                    Error error = new Error(LexicalAttributeError.EmptyCategory);
                    errorSet.Add(LexiconError.AttributeError,
                        error, parentProperty.Parent.Parent.Text, parentProperty.Parent.Symbolic);
                    attributeItem = null;
                }
                else if (string.IsNullOrEmpty(value))
                {
                    Error error = new Error(LexicalAttributeError.EmptyValue);
                    errorSet.Add(LexiconError.AttributeError,
                        error, parentProperty.Parent.Parent.Text, parentProperty.Parent.Symbolic);
                    attributeItem = null;
                }
                else
                {
                    attributeItem.Value = value;
                    attributeItem.CategoryName = category;
                    if (!string.IsNullOrEmpty(originalValue) &&
                        attributeItem.Status != Lexicon.LexiconStatus.Original)
                    {
                        attributeItem.OldValue = originalValue;
                    }
                    else
                    {
                        attributeItem.OldValue = value;
                    }
                }
            }

            return attributeItem;
        }

        /// <summary>
        /// Write an attribute item to the XML writer.
        /// </summary>
        /// <param name="writer">XML writer.</param>
        internal void WriteToXml(XmlWriter writer)
        {
            writer.WriteStartElement("attr");
            writer.WriteAttributeString("category", CategoryName);
            writer.WriteAttributeString("value", Value);
            if (Status != Lexicon.LexiconStatus.Original)
            {
                writer.WriteAttributeString("s", Status.ToString());
                if (!string.IsNullOrEmpty(OldValue) && !Value.Equals(OldValue, StringComparison.Ordinal))
                {
                    writer.WriteAttributeString("vo", OldValue);
                }
            }

            writer.WriteEndElement();
        }

        #endregion
    }

    /// <summary>
    /// Lexicon pronunciation.
    /// </summary>
    public class LexiconItemProperty
    {
        #region Fields

        private bool _valid = true;

        private PosItem _pos;
        private GenderItem _gender;
        private CaseItem _case;
        private NumberItem _number;

        private SortedDictionary<string, DomainItem> _domains = new SortedDictionary<string, DomainItem>();

        // key is the category name: F_CASE, F_GENDER or F_NUMBER, the value is the attribute value list for current key.
        private Dictionary<string, List<AttributeItem>> _attributes = new Dictionary<string, List<AttributeItem>>();

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="LexiconItemProperty"/> class.
        /// Default construction.
        /// </summary>
        public LexiconItemProperty()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LexiconItemProperty"/> class.
        /// Construction from POS item.
        /// </summary>
        /// <param name="pos">POS value of this property.</param>
        public LexiconItemProperty(PosItem pos)
        {
            if (pos == null)
            {
                throw new ArgumentNullException("pos");
            }

            PartOfSpeech = pos;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LexiconItemProperty"/> class.
        /// Construction from Gender item.
        /// </summary>
        /// <param name="gender">Gender value of this property.</param>
        public LexiconItemProperty(GenderItem gender)
        {
            if (gender == null)
            {
                throw new ArgumentNullException("gender");
            }

            Gender = gender;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LexiconItemProperty"/> class.
        /// Construction from Case item.
        /// </summary>
        /// <param name="caseItem">Case value of this property.</param>
        public LexiconItemProperty(CaseItem caseItem)
        {
            if (caseItem == null)
            {
                throw new ArgumentNullException("caseItem");
            }

            Case = caseItem;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LexiconItemProperty"/> class.
        /// Construction from Number item.
        /// </summary>
        /// <param name="number">Number value of this property.</param>
        public LexiconItemProperty(NumberItem number)
        {
            if (number == null)
            {
                throw new ArgumentNullException("number");
            }

            Number = number;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LexiconItemProperty"/> class.
        /// Construction from POS and Gender item.
        /// </summary>
        /// <param name="pos">POS value of this property.</param>
        /// <param name="gender">Gender value of this property.</param>
        public LexiconItemProperty(PosItem pos, GenderItem gender)
        {
            if (pos == null)
            {
                throw new ArgumentNullException("pos");
            }

            if (gender == null)
            {
                throw new ArgumentNullException("gender");
            }

            PartOfSpeech = pos;
            Gender = gender;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether this property is valid.
        /// If it contains bad POS, Case, Gender or Number, it should be invalid.
        /// </summary>
        public bool Valid
        {
            get { return _valid; }
            set { _valid = value; }
        }

        /// <summary>
        /// Gets or sets Part of speech, this is POS.
        /// </summary>
        public PosItem PartOfSpeech
        {
            get
            {
                return _pos;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _pos = value;
            }
        }

        /// <summary>
        /// Gets or sets Gender.
        /// </summary>
        public GenderItem Gender
        {
            get
            {
                return _gender;
            }

            set
            {
                _gender = value;
            }
        }

        /// <summary>
        /// Gets or sets Case.
        /// </summary>
        public CaseItem Case
        {
            get
            {
                return _case;
            }

            set
            {
                _case = value;
            }
        }

        /// <summary>
        /// Gets or sets Number.
        /// </summary>
        public NumberItem Number
        {
            get
            {
                return _number;
            }

            set
            {
                _number = value;
            }
        }

        /// <summary>
        /// Gets or sets Status.
        /// </summary>
        public Lexicon.LexiconStatus Status { get; set; }

        /// <summary>
        /// Gets Domains.
        /// </summary>
        public SortedDictionary<string, DomainItem> Domains
        {
            get { return _domains; }
        }

        /// <summary>
        /// Gets Attribute set.
        /// </summary>
        public Dictionary<string, List<AttributeItem>> AttributeSet
        {
            get { return _attributes; }
        }

        /// <summary>
        /// Gets Sorted Attribute Set.
        /// </summary>
        public SortedDictionary<string, List<AttributeItem>> SortedAttributeSet
        {
            get
            {
                SortedDictionary<string, List<AttributeItem>> sortedAttributeSet = new SortedDictionary<string, List<AttributeItem>>();
                foreach (KeyValuePair<string, List<AttributeItem>> pair in _attributes)
                {
                    sortedAttributeSet.Add(pair.Key, pair.Value);
                }

                return sortedAttributeSet;
            }
        }

        /// <summary>
        /// Gets or sets Parent LexiconPronunciation instance.
        /// </summary>
        public LexiconPronunciation Parent { get; set; }

        #endregion

        #region Public methods

        /// <summary>
        /// Whether equal for two attribute set.
        /// </summary>
        /// <param name="attributeSetA">Attribute Set A.</param>
        /// <param name="attributeSetB">Attribute Set B.</param>
        /// <returns>Whether equal.</returns>
        public static bool AttributeSetEqual(Dictionary<string, List<AttributeItem>> attributeSetA,
            Dictionary<string, List<AttributeItem>> attributeSetB)
        {
            return AttributeSetAContainsSetB(attributeSetA, attributeSetB) &&
                    AttributeSetAContainsSetB(attributeSetB, attributeSetA);
        }

        /// <summary>
        /// Whether attribute set A contains set B.
        /// </summary>
        /// <param name="attributeSetA">Attribute Set A.</param>
        /// <param name="attributeSetB">Attribute Set B.</param>
        /// <returns>Return true, when all attributes in set B are found in set A
        ///     attributeSetB = null, return true.
        ///     all attributes in set B are deleted, return true, even if attributeSetA = null.
        /// </returns>
        public static bool AttributeSetAContainsSetB(Dictionary<string, List<AttributeItem>> attributeSetA,
            Dictionary<string, List<AttributeItem>> attributeSetB)
        {
            if (attributeSetA == attributeSetB || attributeSetB == null)
            {
                return true;
            }

            foreach (string key in attributeSetB.Keys)
            {
                if (!attributeSetA.ContainsKey(key))
                {
                    return false;
                }
                else
                {
                    if (!AttributeAContainsAttributeB(attributeSetA[key], attributeSetB[key]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Whether equal for two domain set.
        /// </summary>
        /// <param name="domainSetA">DomainSet A.</param>
        /// <param name="domainSetB">DomainSet B.</param>
        /// <returns>Whether equal.</returns>
        public static bool DomainSetEqual(SortedDictionary<string, DomainItem> domainSetA,
            SortedDictionary<string, DomainItem> domainSetB)
        {
            return DomainSetAContainsSetB(domainSetA, domainSetB) &&
                    DomainSetAContainsSetB(domainSetB, domainSetA);
        }

        /// <summary>
        /// Equal without domain information.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>True for equal.</returns>
        public bool EqualsWithoutDomain(LexiconItemProperty property)
        {
            bool equal = false;
            if (property != null)
            {
                equal = HistoryValue.Equals(_pos, property.PartOfSpeech) &&
                        HistoryValue.Equals(_gender, property.Gender) &&
                        HistoryValue.Equals(_case, property.Case) &&
                        HistoryValue.Equals(_number, property.Number) &&
                        AttributeSetEqual(_attributes, property.AttributeSet);
            }

            return equal;
        }

        /// <summary>
        /// Override the Equals function.
        /// </summary>
        /// <param name="obj">Object.</param>
        /// <returns>True for equal.</returns>
        public override bool Equals(object obj)
        {
            bool equal = false;

            LexiconItemProperty other = obj as LexiconItemProperty;
            if (other != null)
            {
                if (Status == Lexicon.LexiconStatus.Deleted &&
                    other.Status == Lexicon.LexiconStatus.Deleted)
                {
                    // Both deleted treated as equal. Because they're the same when compile into lexicon binary.
                    equal = true;
                }
                else if (Status != Lexicon.LexiconStatus.Deleted &&
                    other.Status != Lexicon.LexiconStatus.Deleted)
                {
                    if (EqualsWithoutDomain(other) &&
                        DomainSetEqual(_domains, other.Domains))
                    {
                        equal = true;
                    }
                }
            }

            return equal;
        }

        /// <summary>
        /// Override the GetHashCode function.
        /// </summary>
        /// <returns>Hash code.</returns>
        public override int GetHashCode()
        {
            int hashCode = _pos.GetHashCode();
            hashCode = (hashCode << 5) + _gender.GetHashCode();
            hashCode = (hashCode << 5) + _case.GetHashCode();
            hashCode = (hashCode << 5) + _number.GetHashCode();
            hashCode = (hashCode << 5) + _attributes.GetHashCode();
            hashCode = (hashCode << 5) + _domains.GetHashCode();
            return hashCode;
        }

        /// <summary>
        /// Value of the lexicon items, mainly used for log the item.
        /// </summary>
        /// <returns>String value presents the item.</returns>
        public override string ToString()
        {
            StringBuilder propertyValue = new StringBuilder();
            propertyValue.Append(ToStringWithoutDomain());

            foreach (KeyValuePair<string, DomainItem> pair in Domains)
            {
                propertyValue.AppendFormat("Domain=[{0}];", pair.Value.Value);
            }

            return propertyValue.ToString();
        }

        /// <summary>
        /// Remove change history.
        /// </summary>
        public void RemoveHistory()
        {
            Status = Lexicon.LexiconStatus.Original;
            if (PartOfSpeech != null)
            {
                PartOfSpeech.RemoveHistory();
            }

            if (Gender != null)
            {
                Gender.RemoveHistory();
            }

            if (Case != null)
            {
                Case.RemoveHistory();
            }

            if (Number != null)
            {
                Number.RemoveHistory();
            }

            if (AttributeSet != null)
            {
                // Save the attribute that will be deleted
                List<AttributeItem> deleteAttribute = new List<AttributeItem>();

                foreach (string key in AttributeSet.Keys.ToArray())
                {
                    foreach (AttributeItem attr in AttributeSet[key])
                    {
                        if (attr.Status == Lexicon.LexiconStatus.Deleted)
                        {
                            // Record delete item
                            deleteAttribute.Add(attr);
                        }
                        else
                        {
                            attr.RemoveHistory();
                        }
                    }
                }

                foreach (AttributeItem deleteItem in deleteAttribute)
                {
                    AttributeSet[deleteItem.CategoryName].Remove(deleteItem);

                    if (AttributeSet[deleteItem.CategoryName].Count == 0)
                    {
                        AttributeSet.Remove(deleteItem.CategoryName);
                    }
                }
            }

            if (Domains != null)
            {
                foreach (string key in Domains.Keys.ToArray())
                {
                    if (Domains[key].Status == Lexicon.LexiconStatus.Deleted)
                    {
                        Domains.Remove(key);
                    }
                    else
                    {
                        Domains[key].RemoveHistory();
                    }
                }
            }
        }

        /// <summary>
        /// Add domain.
        /// </summary>
        /// <param name="domainItem">DomainItem.</param>
        /// <returns>Whether imported.</returns>
        public bool ImportDomainItem(DomainItem domainItem)
        {
            if (domainItem == null)
            {
                throw new ArgumentNullException();
            }

            bool imported = false;
            if (_domains.Count == 0)
            {
                _domains.Add(DomainItem.GeneralDomain, new DomainItem());
            }

            if (!_domains.ContainsKey(domainItem.Value))
            {
                _domains.Add(domainItem.Value, domainItem.Clone());
                imported = true;
            }

            return imported;
        }

        /// <summary>
        /// Change to another specified domain.
        /// </summary>
        /// <param name="domainItem">DomainItem.</param>
        public void ChangeDomain(DomainItem domainItem)
        {
            if (domainItem == null)
            {
                throw new ArgumentNullException();
            }

            _domains.Clear();
            _domains.Add(domainItem.Value, domainItem.Clone());
        }

        /// <summary>
        /// Value of the lexicon property items without domain information.
        /// </summary>
        /// <returns>
        /// String value without domain information presents the item, the format likes:gender=mail,femail; number=singule.
        /// </returns>
        public string ToStringWithoutDomain()
        {
            StringBuilder sb = new StringBuilder();
            if (PartOfSpeech != null)
            {
                sb.Append(Helper.NeutralFormat("Pos=[{0}];", PartOfSpeech.Value));
            }

            if (Gender != null)
            {
                sb.Append(Helper.NeutralFormat("Gender=[{0}];", Gender.Value));
            }

            if (Case != null)
            {
                sb.Append(Helper.NeutralFormat("Case=[{0}];", Case.Value));
            }

            if (Number != null)
            {
                sb.Append(Helper.NeutralFormat("Number=[{0}];", Number.Value));
            }

            foreach (KeyValuePair<string, List<AttributeItem>> pair in SortedAttributeSet)
            {
                sb.AppendFormat(" {0}=", pair.Key);

                for (int i = 0; i < pair.Value.Count - 1; ++i)
                {
                    sb.AppendFormat("{0}, ", pair.Value[i].Value);
                }

                sb.AppendFormat("{0};", pair.Value[pair.Value.Count - 1].Value);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Clone current property .
        /// </summary>
        /// <returns>Cloned LexiconItemProperty.</returns>
        public LexiconItemProperty Clone()
        {
            LexiconItemProperty clonedProperty = new LexiconItemProperty();
            clonedProperty.Valid = _valid;
            clonedProperty.Status = Status;

            if (_pos != null)
            {
                clonedProperty.PartOfSpeech = _pos.Clone();
            }

            if (_gender != null)
            {
                clonedProperty.Gender = _gender.Clone();
            }

            if (_case != null)
            {
                clonedProperty.Case = _case.Clone();
            }

            if (_number != null)
            {
                clonedProperty.Number = _number.Clone();
            }

            foreach (string domainName in _domains.Keys)
            {
                clonedProperty.Domains.Add(domainName, _domains[domainName].Clone());
            }

            foreach (string attributeName in _attributes.Keys)
            {
                foreach (AttributeItem attr in _attributes[attributeName])
                {
                    clonedProperty.AddAttribute(attr.Clone());
                }
            }

            return clonedProperty;
        }

        /// <summary>
        /// Add an attribute to now attribute list.
        /// </summary>
        /// <param name="attribute">New attribute.</param>
        public void AddAttribute(AttributeItem attribute)
        {
            if (_attributes.ContainsKey(attribute.CategoryName))
            {
                // if there are no duplicate attribute, add new attribute.
                if (!_attributes[attribute.CategoryName].Contains(attribute))
                {
                    _attributes[attribute.CategoryName].Add(attribute);
                }
            }
            else
            {
                List<AttributeItem> attrList = new List<AttributeItem>()
                {
                    attribute
                };

                _attributes.Add(attribute.CategoryName, attrList);
            }
        }

        /// <summary>
        /// RemoveAttributeValue.
        /// </summary>
        /// <param name="attrName">Attribute name.</param>
        /// <param name="attrValue">Attribute value.</param>
        public void RemoveAttributeValue(string attrName, string attrValue)
        {
            if (!AttributeSet.ContainsKey(attrName))
            {
                throw new InvalidOperationException(string.Format("Attribute {0} cannot be found.", attrName));
            }

            AttributeItem attribute = AttributeSet[attrName].Find(x => x.Value == attrValue);

            if (attribute == null)
            {
                throw new InvalidOperationException(string.Format("Attribute {0}={1} cannot be found.", attrName, attrValue));
            }

            if (Status == Lexicon.LexiconStatus.Added ||
                attribute.Status == Lexicon.LexiconStatus.Added)
            {
                AttributeSet[attribute.CategoryName].Remove(attribute);

                if (AttributeSet[attribute.CategoryName].Count == 0)
                {
                    AttributeSet.Remove(attribute.CategoryName);
                }
            }
            else
            {
                attribute.Status = Lexicon.LexiconStatus.Deleted;
                attribute.Value = attribute.OldValue;
            }
        }

        /// <summary>
        /// AddAttributeValue.
        /// </summary>
        /// <param name="attrName">Attribute name.</param>
        /// <param name="attrValue">Attribute value.</param>
        public void AddAttributeValue(string attrName, string attrValue)
        {
            if (AttributeSet.ContainsKey(attrName))
            {
                AttributeItem attribute = AttributeSet[attrName].Find(x => x.Value == attrValue);

                // if there has same attribute in original, update status
                if (attribute != null)
                {
                    if (attribute.Status == Lexicon.LexiconStatus.Deleted)
                    {
                        if (Status == Lexicon.LexiconStatus.Added)
                        {
                            attribute.Status = Lexicon.LexiconStatus.Added;
                        }
                        else
                        {
                            attribute.Status = Lexicon.LexiconStatus.Original;
                        }

                        attribute.Value = attrValue;
                    }
                }
                else
                {
                    AttributeItem attributeItem = new AttributeItem(attrName, attrValue);
                    attributeItem.Status = Lexicon.LexiconStatus.Added;
                    AddAttribute(attributeItem);
                }
            }
            else
            {
                AttributeItem attributeItem = new AttributeItem(attrName, attrValue);
                attributeItem.Status = Lexicon.LexiconStatus.Added;
                AddAttribute(attributeItem);
            }
        }

        /// <summary>
        /// RemoveDomainValue.
        /// </summary>
        /// <param name="domainValue">DomainValue.</param>
        public void RemoveDomainValue(string domainValue)
        {
            if (Status == Lexicon.LexiconStatus.Added ||
                Domains[domainValue].Status == Lexicon.LexiconStatus.Added)
            {
                Domains.Remove(domainValue);
            }
            else
            {
                Domains[domainValue].Status = Lexicon.LexiconStatus.Deleted;
                Domains[domainValue].Value = Domains[domainValue].OldValue;
            }
        }

        /// <summary>
        /// Convert the property's attribute from old format to new format.
        /// The old format is: gender v="MF" case v="nda" number v="S".
        /// The new format is: attr category="F_Gender" value="GENDER_masculine".
        /// </summary>
        /// <param name="errorSet">The error set.</param>
        /// <returns>Converted property.</returns>
        public LexiconItemProperty ToNewFormatAttributeProperty(ErrorSet errorSet)
        {
            LexiconItemProperty convertedProperty = this.Clone();

            if (Gender != null && !string.IsNullOrEmpty(Gender.Value))
            {
                convertedProperty.AttributeSet.Add(AttributeItem.GenderCategoryName, Gender.ToNewFormatAttribute(errorSet).ToList());
            }

            if (Case != null && !string.IsNullOrEmpty(Case.Value))
            {
                convertedProperty.AttributeSet.Add(AttributeItem.CaseCategoryName, Case.ToNewFormatAttribute(errorSet).ToList());
            }

            if (Number != null && !string.IsNullOrEmpty(Number.Value))
            {
                convertedProperty.AttributeSet.Add(AttributeItem.NumberCategoryName, Number.ToNewFormatAttribute(errorSet).ToList());
            }

            convertedProperty.Gender = null;
            convertedProperty.Case = null;
            convertedProperty.Number = null;

            return convertedProperty;
        }

        #endregion

        #region Internal methods

        /// <summary>
        /// Load LexiconItemProperty from XmlNode.
        /// </summary>
        /// <param name="parentLexPron">LexiconPronunciation.</param>
        /// <param name="propertyNode">XmlNode.</param>
        /// <param name="nsmgr">XmlNamespaceManager.</param>
        /// <param name="contentController">Object.</param>
        /// <param name="errorSet">ErrorSet.</param>
        /// <returns>LexiconItemProperty.</returns>
        internal static LexiconItemProperty Load(LexiconPronunciation parentLexPron, XmlNode propertyNode, XmlNamespaceManager nsmgr, Lexicon.ContentControler contentController, ErrorSet errorSet)
        {
            Debug.Assert(parentLexPron != null && parentLexPron.Parent != null && propertyNode != null &&
                nsmgr != null && contentController != null && errorSet != null);

            LexiconItemProperty property = new LexiconItemProperty();
            property.Parent = parentLexPron;

            XmlElement propertyElem = propertyNode as XmlElement;
            string stateValue = propertyElem.GetAttribute("s");
            if (!string.IsNullOrEmpty(stateValue))
            {
                property.Status = (Lexicon.LexiconStatus)Enum.Parse(typeof(Lexicon.LexiconStatus), stateValue, true);
            }

            if (!contentController.IsHistoryCheckingMode && property.Status == Lexicon.LexiconStatus.Deleted)
            {
                property = null;
            }
            else
            {
                PosItem posItem = PosItem.Load(propertyNode, nsmgr);
                if (posItem != null)
                {
                    property.PartOfSpeech = posItem;
                }

                GenderItem genderItem = GenderItem.Load(propertyNode, nsmgr);
                if (genderItem != null)
                {
                    property.Gender = genderItem;
                }

                CaseItem caseItem = CaseItem.Load(propertyNode, nsmgr);
                if (caseItem != null)
                {
                    property.Case = caseItem;
                }

                NumberItem numberItem = NumberItem.Load(propertyNode, nsmgr);
                if (numberItem != null)
                {
                    property.Number = numberItem;
                }

                foreach (XmlNode domainNode in propertyNode.SelectNodes("tts:domain", nsmgr))
                {
                    DomainItem domainItem = DomainItem.Load(property, domainNode, nsmgr, contentController, errorSet);
                    if (domainItem != null)
                    {
                        if (!property.Domains.ContainsKey(domainItem.Value))
                        {
                            property.Domains.Add(domainItem.Value, domainItem);
                        }
                        else
                        {
                            Error error = new Error(DomainError.DuplicateDomain, domainItem.Value);
                            errorSet.Add(LexiconError.DomainError,
                                error, parentLexPron.Parent.Text, parentLexPron.Symbolic);
                        }
                    }
                }

                string lexLevelDomain = (parentLexPron.Parent.Parent as Lexicon).DomainTag;
                if (property.Domains.Count == 0)
                {
                    if (string.IsNullOrEmpty(lexLevelDomain))
                    {
                        property.ChangeDomain(new DomainItem());
                    }
                    else
                    {
                        property.ChangeDomain(new DomainItem(lexLevelDomain));
                    }
                }
                else if (!string.IsNullOrEmpty(lexLevelDomain))
                {
                    Error error = new Error(DomainError.InvalidDomainTags);
                    errorSet.Add(LexiconError.DomainError,
                        error, parentLexPron.Parent.Text, parentLexPron.Symbolic);
                }

                foreach (XmlNode attributeNode in propertyNode.SelectNodes("tts:attr", nsmgr))
                {
                    AttributeItem attributeItem = AttributeItem.Load(property, attributeNode, nsmgr, contentController, errorSet);
                    if (attributeItem != null)
                    {
                        property.AddAttribute(attributeItem);
                    }
                }
            }

            return property;
        }

        /// <summary>
        /// Write a lexicon item property to the XML writer.
        /// </summary>
        /// <param name="writer">XML writer.</param>
        internal void WriteToXml(XmlWriter writer)
        {
            writer.WriteStartElement("pr");

            if (Status != Lexicon.LexiconStatus.Original)
            {
                writer.WriteAttributeString("s", Status.ToString());
            }

            if (_pos != null)
            {
                _pos.WriteToXml(writer);
            }

            if (_gender != null)
            {
                _gender.WriteToXml(writer);
            }

            if (_case != null)
            {
                _case.WriteToXml(writer);
            }

            if (_number != null)
            {
                _number.WriteToXml(writer);
            }

            // Write out attribute set
            foreach (KeyValuePair<string, List<AttributeItem>> pair in _attributes)
            {
                foreach (AttributeItem attr in pair.Value)
                {
                    if (string.IsNullOrEmpty(attr.CategoryName))
                    {
                        attr.CategoryName = pair.Key;
                    }

                    attr.WriteToXml(writer);
                }
            }

            // Write out domains
            if (_domains.Values.Count != 1 ||
                !_domains.Values.First().Value.Equals(DomainItem.GeneralDomain, StringComparison.Ordinal))
            {
                foreach (DomainItem domain in _domains.Values)
                {
                    domain.WriteToXml(writer);
                }
            }

            writer.WriteEndElement();
        }

        #endregion

        #region private methods

        private static bool AttributeAContainsAttributeB(List<AttributeItem> attributeA,
    List<AttributeItem> attributeB)
        {
            foreach (AttributeItem attr in attributeB)
            {
                if (attr.Status != Lexicon.LexiconStatus.Deleted)
                {
                    if (!attributeA.Contains(attr))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Whether domain set A contains set B.
        /// </summary>
        /// <param name="domainSetA">Domain Set A.</param>
        /// <param name="domainSetB">Domain Set B.</param>
        /// <returns>Return true, when all domains in set B are found in set A
        ///     domainSetB = null, return true.
        ///     all domains in set B are deleted, return true, even if domainSetA = null.
        /// </returns>
        private static bool DomainSetAContainsSetB(SortedDictionary<string, DomainItem> domainSetA,
            SortedDictionary<string, DomainItem> domainSetB)
        {
            bool found = true;

            if (domainSetB != null)
            {
                foreach (KeyValuePair<string, DomainItem> itemB in domainSetB)
                {
                    DomainItem domainItemB = itemB.Value;
                    if (domainItemB.Status != Lexicon.LexiconStatus.Deleted)
                    {
                        if (domainSetA != null)
                        {
                            if (domainSetA.ContainsKey(itemB.Key))
                            {
                                DomainItem domainItemA = domainSetA[itemB.Key];
                                found = domainItemA.Equals(domainItemB);
                            }
                            else
                            {
                                found = false;
                            }
                        }
                        else
                        {
                            found = false;
                        }
                    }

                    if (!found)
                    {
                        break;
                    }
                }
            }

            return found;
        }

        /// <summary>
        /// Transfer from a collection of attributeitem to a collection of collection.
        /// </summary>
        /// <param name="sourceCollection">Source collection.</param>
        /// <returns>Result collection of collection.</returns>
        private Collection<Collection<AttributeItem>> TransferCollection(Collection<AttributeItem> sourceCollection)
        {
            Collection<Collection<AttributeItem>> resultCollection = new Collection<Collection<AttributeItem>>();
            if (sourceCollection != null)
            {
                foreach (AttributeItem item in sourceCollection)
                {
                    Collection<AttributeItem> newItem = new Collection<AttributeItem>();
                    newItem.Add(item);
                    resultCollection.Add(newItem);
                }
            }

            return resultCollection;
        }

        /// <summary>
        /// Mutiply the collection for group the attributes.
        /// </summary>
        /// <param name="firstCollection">First collection.</param>
        /// <param name="secondCollection">Second collection.</param>
        /// <returns>Result mutiplied collection.</returns>
        private Collection<Collection<AttributeItem>> MultiplyCollection(
            Collection<Collection<AttributeItem>> firstCollection,
            Collection<AttributeItem> secondCollection)
        {
            Collection<Collection<AttributeItem>> resultCollection = new Collection<Collection<AttributeItem>>();
            if (firstCollection == null || firstCollection.Count == 0)
            {
                resultCollection = TransferCollection(secondCollection);
            }
            else if (secondCollection == null || secondCollection.Count == 0)
            {
                resultCollection = firstCollection;
            }
            else
            {
                foreach (Collection<AttributeItem> item1 in firstCollection)
                {
                    foreach (AttributeItem item2 in secondCollection)
                    {
                        Collection<AttributeItem> newAttrCollection = new Collection<AttributeItem>();
                        Helper.AppendCollection(newAttrCollection, item1);
                        newAttrCollection.Add(item2);
                        resultCollection.Add(newAttrCollection);
                    }
                }
            }

            return resultCollection;
        }

        #endregion
    }

    /// <summary>
    /// Pronunciation node in lexicon, containing several lexiconItemProperty.
    /// </summary>
    public class LexiconPronunciation
    {
        #region Fields

        /// <summary>
        /// Default pronunciation position index.
        /// </summary>
        public const int DefaultPositionIndex = -1;

        private bool _valid = true;
        private int _oldPosition = DefaultPositionIndex;

        private string _symbolic;
        private string _oldSymbolic;
        private Language _language;
        private LexiconType _type;

        private Collection<LexiconItemProperty> _properties = new Collection<LexiconItemProperty>();

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="LexiconPronunciation"/> class.
        /// </summary>
        public LexiconPronunciation()
            : this(Language.Neutral)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LexiconPronunciation"/> class.
        /// </summary>
        /// <param name="language">Language.</param>
        public LexiconPronunciation(Language language)
        {
            _language = language;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether the pronunciation is valid
        /// If it contains no valid property or its pronunciation string is not valid, should return invalid.
        /// </summary>
        public bool Valid
        {
            get { return _valid; }
            set { _valid = value; }
        }

        /// <summary>
        /// Gets or sets Frequency.
        /// </summary>
        public int Frequency { get; set; }

        /// <summary>
        /// Gets or sets Pronunciation's old position.
        /// </summary>
        public int OldPosition
        {
            get { return _oldPosition; }
            set { _oldPosition = value; }
        }

        /// <summary>
        /// Gets or sets PhoneString.
        /// </summary>
        public string Symbolic
        {
            get
            {
                return _symbolic;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _symbolic = value;
            }
        }

        /// <summary>
        /// Gets or sets Old PhoneString.
        /// </summary>
        public string OldSymbolic
        {
            get
            {
                return _oldSymbolic;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _oldSymbolic = value;
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
        /// Gets or sets Type.
        /// </summary>
        public LexiconType LexiconType
        {
            get { return _type; }
            set { _type = value; }
        }

        /// <summary>
        /// Gets or sets Status of the pronunciation
        /// When loading, if it is "deleted", it means this item is deleted.
        /// </summary>
        public Lexicon.LexiconStatus Status { get; set; }

        /// <summary>
        /// Gets Poses.
        /// </summary>
        public Collection<LexiconItemProperty> Properties
        {
            get { return _properties; }
        }

        /// <summary>
        /// Gets or sets Parent LexicalItem instance.
        /// </summary>
        public LexicalItem Parent { get; set; }

        #endregion

        #region Public methods

        /// <summary>
        /// Get spell out pronunciation.
        /// </summary>
        /// <param name="grapheme">Grapheme.</param>
        /// <param name="sp">ServiceProvider.</param>
        /// <returns>Spell out pronunciation.</returns>
        [CLSCompliant(false)]
        public static string GetSpellOutPronunciation(string grapheme, SP.ServiceProvider sp)
        {
            Debug.Assert(!string.IsNullOrEmpty(grapheme));
            Debug.Assert(sp != null);

            SP.Pronouncer pronouncer = sp.Engine.TextProcessor.SentenceAnalyzer.Pronouncer;
            SP.Phoneme phoneTable = sp.Engine.Phoneme;

            Debug.Assert(pronouncer != null);
            Debug.Assert(phoneTable != null);

            string pron = string.Empty;

            SP.TtsPronSource pronSource = SP.TtsPronSource.PS_NONE;
            string predictedPhoneIds = pronouncer.WordPronouncer.GetPronunciation(
                grapheme, SP.TtsPronGenerationType.PG_SPELL_AS_ACRONYM, SP.TtsDomain.TTS_DOMAIN_GENERAL, ref pronSource);

            if (!string.IsNullOrEmpty(predictedPhoneIds))
            {
                pron = phoneTable.PhoneIdsToPronunciation(predictedPhoneIds);
            }

            return pron;
        }

        /// <summary>
        /// Compare objects that derived from LexiconPronunciation.
        /// </summary>
        /// <param name="obj1">Object 1.</param>
        /// <param name="obj2">Object 2.</param>
        /// <returns>true for equal
        ///     (null, null) => equal
        ///     (null, deleted) => equal
        ///     (deleted, deleted) => equal.
        /// </returns>
        public static bool Equals(LexiconPronunciation obj1, LexiconPronunciation obj2)
        {
            if (obj1 == obj2)
            {
                return true;
            }

            if ((obj1 == null || obj1.Status == Lexicon.LexiconStatus.Deleted) &&
                (obj2 == null || obj2.Status == Lexicon.LexiconStatus.Deleted))
            {
                return true;
            }

            if (obj1.Language != obj2.Language ||
                obj1.LexiconType != obj2.LexiconType ||
                obj1.Status != obj2.Status ||
                obj1.Symbolic != obj2.Symbolic)
            {
                return false;
            }

            Collection<LexiconItemProperty> props1 = obj1.Properties;
            Collection<LexiconItemProperty> props2 = obj2.Properties;
            int propsLength = props1.Count;
            if (propsLength != props2.Count)
            {
                return false;
            }

            for (int j = 0; j < propsLength; j++)
            {
                if (!props1[j].Equals(props2[j]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// If pronunication is spell out.
        /// </summary>
        /// <param name="sp">ServiceProvider.</param>
        /// <returns>Whether pronunication is spell out.</returns>
        [CLSCompliant(false)]
        public bool IsSpellOut(SP.ServiceProvider sp)
        {
            if (sp == null)
            {
                return false;
            }

            string pron = LexiconPronunciation.GetSpellOutPronunciation(this.Parent.Grapheme, sp);

            if (string.IsNullOrEmpty(pron))
            {
                return false;
            }

            return Pronunciation.Equals(pron, this.Symbolic, true);
        }

        /// <summary>
        /// Update state.
        /// </summary>
        public void RefreshStatus()
        {
            int[] counts = new int[] { 0, 0, 0, 0 };

            foreach (LexiconItemProperty attrib in _properties)
            {
                counts[(int)attrib.Status]++;
            }

            if (counts[(int)Lexicon.LexiconStatus.Deleted] == _properties.Count)
            {
                // All attributes are in deleted state
                Status = Lexicon.LexiconStatus.Deleted;
                Symbolic = OldSymbolic;
            }
            else if (counts[(int)Lexicon.LexiconStatus.Added] == _properties.Count)
            {
                // All attributes are in added state
                Status = Lexicon.LexiconStatus.Added;
                OldSymbolic = Symbolic;
            }
            else if (counts[(int)Lexicon.LexiconStatus.Original] == _properties.Count &&
                OldSymbolic == Symbolic)
            {
                Debug.Assert(Status != Lexicon.LexiconStatus.Checked);

                // All attributes are in original state and current pronunciation text is same as original one
                Status = Lexicon.LexiconStatus.Original;
            }
            else
            {
                Status = Lexicon.LexiconStatus.Changed;
            }
        }

        /// <summary>
        /// Remove change history.
        /// </summary>
        public void RemoveHistory()
        {
            if (Status != Lexicon.LexiconStatus.Deleted && Status != Lexicon.LexiconStatus.Original)
            {
                for (int i = Properties.Count - 1; i >= 0; i--)
                {
                    if (Properties[i].Status == Lexicon.LexiconStatus.Deleted)
                    {
                        Properties.RemoveAt(i);
                    }
                    else
                    {
                        Properties[i].RemoveHistory();
                    }
                }

                OldSymbolic = Symbolic;
                Status = Lexicon.LexiconStatus.Original;
                OldPosition = LexiconPronunciation.DefaultPositionIndex;
            }
        }

        /// <summary>
        /// Import domain pronunciation.
        /// </summary>
        /// <param name="domainPron">Domain LexiconPronunciation.</param>
        /// <param name="domainTag">Domain tag.</param>
        /// <param name="first">Whether this pronunciation is the first one in domain lexicon.</param>
        /// <returns>Whether this LexiconPronunciation changed .</returns>
        public bool ImportDomainPronunciation(LexiconPronunciation domainPron, string domainTag, bool first)
        {
            Helper.ThrowIfNull(domainPron);
            Helper.ThrowIfNull(domainTag);

            if (!domainPron.OnlyContainsOneDomain(domainTag))
            {
                throw new InvalidDataException("It is invalid to include any other domain in property level.");
            }

            if (!first)
            {
                RemovePronunciationIsFirstTags(domainTag);
            }

            bool changed = false;
            foreach (LexiconItemProperty domainProperty in domainPron.Properties)
            {
                if (domainProperty.Gender != null ||
                    domainProperty.Case != null ||
                    domainProperty.Number != null)
                {
                    throw new InvalidDataException("domain lexicon contains old format <gender> <case> <number>. Please convert them to new format <attr> before import.");
                }

                // look for target property that domain tag will import to
                bool propertyImported = false;
                foreach (LexiconItemProperty targetProperty in _properties)
                {
                    if (targetProperty.Gender != null ||
                        targetProperty.Case != null ||
                        targetProperty.Number != null)
                    {
                        throw new InvalidDataException("target lexicon contains old format <gender> <case> <number>. Please convert them to new format <attr> before import.");
                    }

                    // If main lexicon and domain lexicon have same pos in <pr>, import the domain lexicon attributes to main lexicon.
                    if (HistoryValue.Equals(targetProperty.PartOfSpeech, domainProperty.PartOfSpeech))
                    {
                        propertyImported = true;

                        // found a proper <pr> to import domain tag.
                        DomainItem domainItem = domainProperty.Domains[domainTag];
                        Helper.ThrowIfNull(domainItem);
                        DomainItem newDomainItem = new DomainItem(domainItem.Value);
                        if (targetProperty.ImportDomainItem(newDomainItem))
                        {
                            changed = true;
                        }

                        targetProperty.Domains[domainTag].IsFirstPronunciation = first;

                        // Import domain lexicon attributes to main lexicon.
                        foreach (string attributeKey in domainProperty.AttributeSet.Keys)
                        {
                            if (targetProperty.AttributeSet.ContainsKey(attributeKey))
                            {
                                // Union main lexicon and domain lexicon and remove duplicate.
                                targetProperty.AttributeSet[attributeKey] = targetProperty.AttributeSet[attributeKey].Union(domainProperty.AttributeSet[attributeKey]).ToList();
                            }
                            else
                            {
                                targetProperty.AttributeSet.Add(attributeKey, domainProperty.AttributeSet[attributeKey]);
                            }
                        }
                    }
                }

                if (!propertyImported)
                {
                    // not found. Copy the whole <pr> from domain lexicon
                    LexiconItemProperty newProperty = domainProperty.Clone();
                    foreach (DomainItem domainItem in newProperty.Domains.Values)
                    {
                        domainItem.IsFirstPronunciation = first;
                    }

                    _properties.Add(newProperty);
                    changed = true;
                }
            }

            return changed;
        }

        /// <summary>
        /// Whether it is prefered as the first pronunciation in specified domain lexicon.
        /// </summary>
        /// <param name="domainTag">Domain tag.</param>
        /// <returns>Whether first.</returns>
        public bool IsFirstDomainPronunciation(string domainTag)
        {
            Helper.ThrowIfNull(domainTag);
            bool first = false;

            foreach (LexiconItemProperty property in _properties)
            {
                if (property.Domains.ContainsKey(domainTag))
                {
                    if (property.Domains[domainTag].IsFirstPronunciation)
                    {
                        first = true;
                        break;
                    }
                }
            }

            return first;
        }

        /// <summary>
        /// Check whether only contains one target domain.
        /// </summary>
        /// <param name="domainTag">Domain tag.</param>
        /// <returns>Whether only contains one domain.</returns>
        public bool OnlyContainsOneDomain(string domainTag)
        {
            Helper.ThrowIfNull(domainTag);

            bool valid = true;
            foreach (LexiconItemProperty domainProperty in _properties)
            {
                // It should not include any other domain in property level.
                if (domainProperty.Domains.Count != 1 || !domainProperty.Domains.ContainsKey(domainTag))
                {
                    valid = false;
                    break;
                }
            }

            return valid;
        }

        /// <summary>
        /// Convert to string presentation without domain information.
        /// </summary>
        /// <returns>String presentation without domain.</returns>
        public string ToStringWithoutDomain()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Symbolic);
            sb.AppendLine();

            for (int i = 0; i < Properties.Count; i++)
            {
                sb.Append(Properties[i].ToStringWithoutDomain());
                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Find the LexiconItemProperty node.
        /// </summary>
        /// <param name="pos">Pos.</param>
        /// <returns>Found LexiconItemProperty node.</returns>
        public LexiconItemProperty FindProperty(string pos)
        {
            if (string.IsNullOrEmpty(pos))
            {
                throw new ArgumentNullException("pos");
            }

            LexiconItemProperty foundProperty = null;
            foreach (LexiconItemProperty originalProperty in Properties)
            {
                if (originalProperty.PartOfSpeech.Value.Equals(pos, StringComparison.OrdinalIgnoreCase))
                {
                    foundProperty = originalProperty;
                    break;
                }
            }

            return foundProperty;
        }

        /// <summary>
        /// Clone current pronunciation.
        /// </summary>
        /// <returns>Cloned LexiconPronunciation.</returns>
        public LexiconPronunciation Clone()
        {
            LexiconPronunciation clonedPron = new LexiconPronunciation();
            clonedPron.Valid = _valid;
            clonedPron.OldPosition = _oldPosition;
            clonedPron._symbolic = _symbolic;
            clonedPron._oldSymbolic = _oldSymbolic;
            clonedPron.Frequency = Frequency;
            clonedPron.Language = _language;
            clonedPron.Status = Status;
            clonedPron.LexiconType = _type;

            foreach (LexiconItemProperty property in _properties)
            {
                LexiconItemProperty clonedProperty = property.Clone();
                clonedPron.Properties.Add(clonedProperty);
                clonedProperty.Parent = clonedPron;
            }

            return clonedPron;
        }

        /// <summary>
        /// Convert to new attribute format pronunciation.
        /// </summary>
        /// <param name="errorSet">The error set.</param>
        public void ToNewAttributeFormatPronunciation(ErrorSet errorSet)
        {
            Collection<LexiconItemProperty> convertedProperties = new Collection<LexiconItemProperty>();
            foreach (LexiconItemProperty property in _properties)
            {
                convertedProperties.Add(property.ToNewFormatAttributeProperty(errorSet));
            }

            _properties = convertedProperties;
        }

        #endregion

        #region Override operations

        /// <summary>
        /// Convert to string presentation.
        /// </summary>
        /// <returns>String presentation.</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Symbolic);
            sb.AppendLine();

            for (int i = 0; i < Properties.Count; i++)
            {
                sb.Append(Properties[i].ToString());
                sb.AppendLine();
            }

            return sb.ToString();
        }

        #endregion

        #region Internal methods

        /// <summary>
        /// Load LexiconPronunciation from XmlNode.
        /// </summary>
        /// <param name="parentLexItem">LexicalItem.</param>
        /// <param name="pronNode">XmlNode.</param>
        /// <param name="nsmgr">XmlNamespaceManager.</param>
        /// <param name="contentController">Object.</param>
        /// <param name="errorSet">ErrorSet.</param>
        /// <returns>LexiconPronunciation.</returns>
        internal static LexiconPronunciation Load(LexicalItem parentLexItem, XmlNode pronNode, XmlNamespaceManager nsmgr, Lexicon.ContentControler contentController, ErrorSet errorSet)
        {
            Debug.Assert(parentLexItem != null && pronNode != null &&
                nsmgr != null && contentController != null && errorSet != null);

            LexiconPronunciation lexPron = new LexiconPronunciation(parentLexItem.Language);
            lexPron.Parent = parentLexItem;

            XmlElement pronElem = pronNode as XmlElement;
            Debug.Assert(pronElem != null);

            string pronStatusValue = pronElem.GetAttribute("s");
            if (!string.IsNullOrEmpty(pronStatusValue))
            {
                lexPron.Status = (Lexicon.LexiconStatus)Enum.Parse(typeof(Lexicon.LexiconStatus),
                    pronStatusValue, true);
            }

            // Lexicon object is shared with lexicon reviewer tool,
            // We drop those items if they have "deleted" status when it is not loaded by lexicon reviewer tool
            if (!contentController.IsHistoryCheckingMode && lexPron.Status == Lexicon.LexiconStatus.Deleted)
            {
                lexPron = null;
            }
            else
            {
                Regex regex = new Regex(@"\s{2,}");
                lexPron.Symbolic = pronElem.GetAttribute("v").Trim();
                lexPron.Symbolic = regex.Replace(lexPron.Symbolic, " ").ToLowerInvariant();
                lexPron.OldSymbolic = lexPron.Symbolic;

                // Get pronunciation original position.
                string originalPronPosition = pronElem.GetAttribute("o");
                if (!string.IsNullOrEmpty(originalPronPosition))
                {
                    lexPron.OldPosition = int.Parse(originalPronPosition, CultureInfo.InvariantCulture);
                }

                if (lexPron.Status != Lexicon.LexiconStatus.Original)
                {
                    string originalPronText = pronElem.GetAttribute("vo");
                    if (!string.IsNullOrEmpty(originalPronText))
                    {
                        lexPron.OldSymbolic = originalPronText;
                    }
                }

                // Get word's frequency. If there's no such information, set frequency to zero
                int frequency = 0;
                int.TryParse(pronElem.GetAttribute("f"), out frequency);
                lexPron.Frequency = frequency;

                foreach (XmlNode propertyNode in pronNode.SelectNodes("tts:pr", nsmgr))
                {
                    LexiconItemProperty property = LexiconItemProperty.Load(lexPron, propertyNode, nsmgr, contentController, errorSet);
                    if (property != null)
                    {
                        if (contentController.IsHistoryCheckingMode || !lexPron.Properties.Contains(property))
                        {
                            lexPron.Properties.Add(property);
                        }
                        else
                        {
                            errorSet.Add(LexiconError.DuplicateProperty, parentLexItem.Text, lexPron.Symbolic);
                        }
                    }
                }
            }

            return lexPron;
        }

        /// <summary>
        /// Write a lexicon pronunciation to the XML writer.
        /// </summary>
        /// <param name="writer">XML writer.</param>
        internal void WriteToXml(XmlWriter writer)
        {
            writer.WriteStartElement("p");

            Debug.Assert(_symbolic != null);
            writer.WriteAttributeString("v", _symbolic);
            if (_oldPosition != LexiconPronunciation.DefaultPositionIndex)
            {
                writer.WriteAttributeString("o", _oldPosition.ToString(CultureInfo.InvariantCulture));
            }

            if (Frequency != 0)
            {
                writer.WriteAttributeString("f", Frequency.ToString(CultureInfo.InvariantCulture));
            }

            if (Status != Lexicon.LexiconStatus.Original)
            {
                writer.WriteAttributeString("s", Status.ToString());
                if (!string.IsNullOrEmpty(_symbolic) &&
                    !_symbolic.Equals(_oldSymbolic, StringComparison.Ordinal))
                {
                    writer.WriteAttributeString("vo", _oldSymbolic);
                }
            }

            Debug.Assert(_properties != null && _properties.Count > 0);
            foreach (LexiconItemProperty property in _properties)
            {
                property.WriteToXml(writer);
            }

            writer.WriteEndElement();
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Remove pronunciation prefered tag to false in DomainItem.
        /// </summary>
        /// <param name="domainTag">DomainTag.</param>
        private void RemovePronunciationIsFirstTags(string domainTag)
        {
            foreach (LexiconItemProperty property in _properties)
            {
                if (property.Domains.ContainsKey(domainTag))
                {
                    property.Domains[domainTag].IsFirstPronunciation = false;
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Lexicon item.
    /// </summary>
    public class LexicalItem
    {
        #region Fields

        private bool _valid = true;
        private int _frequency;
        private string _grapheme;
        private string _oldGrapheme;
        private string _text;
        private string _comment;
        private string _alias;
        private string _cacheValue;
        private bool _reviewed;

        private Language _language;
        private LexiconType _lexiconType = LexiconType.Application;
        private Collection<LexiconPronunciation> _pronunciations = new Collection<LexiconPronunciation>();

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="LexicalItem"/> class.
        /// </summary>
        public LexicalItem()
            : this(Language.Neutral)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LexicalItem"/> class.
        /// </summary>
        /// <param name="language">Language of this item.</param>
        public LexicalItem(Language language)
        {
            _language = language;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether this lexical item is valid
        /// If non of the pronunciation node is valid, should return false (invalid).
        /// </summary>
        public bool Valid
        {
            get { return _valid; }
            set { _valid = value; }
        }

        /// <summary>
        /// Gets a value indicating whether Checked flag
        /// Checked = true means this item is reviewed or edited by 1 LE and then status is "changed", "added",
        /// "deleted" or "checked"(checked means reviewed by 1 LE who feels this item is correct).
        /// Checked = false means this item is not reviewed by LE and then status is "original".
        /// </summary>
        public bool Checked
        {
            get { return Status != Lexicon.LexiconStatus.Original; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether Reviewed flag
        /// It means whether the word has been double reviewed by another LE.
        /// </summary>
        public bool Reviewed
        {
            get { return _reviewed; }
            set { _reviewed = value; }
        }

        /// <summary>
        /// Gets a value indicating whether the word need review.
        /// </summary>
        public bool NeedReview
        {
            get { return !Reviewed && Checked; }
        }

        /// <summary>
        /// Gets a value indicating whether this item is a polyphony item.
        /// </summary>
        public bool Polyphonic
        {
            get
            {
                if (_pronunciations == null)
                {
                    return false;
                }

                return _pronunciations.Count > 1;
            }
        }

        /// <summary>
        /// Gets or sets The frequency of this word.
        /// </summary>
        public int Frequency
        {
            get { return _frequency; }
            set { _frequency = value; }
        }

        /// <summary>
        /// Gets or sets Grapheme.
        /// </summary>
        public string Grapheme
        {
            get
            {
                return _grapheme;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _grapheme = value;
            }
        }

        /// <summary>
        /// Gets or sets Old Grapheme.
        /// </summary>
        public string OldGrapheme
        {
            get
            {
                return _oldGrapheme;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _oldGrapheme = value;
            }
        }

        /// <summary>
        /// Gets Grapheme text in lexicon xml.
        /// </summary>
        public string Text
        {
            get
            {
                return _text;
            }
        }

        /// <summary>
        /// Gets or sets Word comment in corpus.
        /// </summary>
        public string Comment
        {
            get { return _comment; }
            set { _comment = value; }
        }

        /// <summary>
        /// Gets or sets Alias for this entry.
        /// </summary>
        public string Alias
        {
            get { return _alias; }
            set { _alias = value; }
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
        /// Gets or sets LexiconType.
        /// </summary>
        public LexiconType LexiconType
        {
            get { return _lexiconType; }
            set { _lexiconType = value; }
        }

        /// <summary>
        /// Gets or sets Status of the lexicon item
        /// When loading, if it is "deleted", it means this item is deleted.
        /// </summary>
        public Lexicon.LexiconStatus Status { get; set; }

        /// <summary>
        /// Gets or sets Origin of the lexicon item.
        /// </summary>
        public Lexicon.LexiconOrigin Origin { get; set; }

        /// <summary>
        /// Gets Pronunciations.
        /// </summary>
        public Collection<LexiconPronunciation> Pronunciations
        {
            get { return _pronunciations; }
        }

        /// <summary>
        /// Gets or sets It is used to record its parent Lexicon instance in offline dll
        /// Or to record its partent LexiconDocument instance in lexicon reviewer.
        /// </summary>
        public object Parent { get; set; }

        /// <summary>
        /// Gets lexicon item cache value.
        /// </summary>
        public string CacheValue
        {
            get
            {
                return _cacheValue;
            }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Whether this word with pronunciation is expanded.
        /// </summary>
        /// <param name="word">Word.</param>
        /// <param name="language">Language.</param>
        /// <param name="pronunciation">Pronunciation.</param>
        /// <param name="sp">ServiceProvider.</param>
        /// <returns>Whether word with pronunciation is expanded.</returns>
        [CLSCompliant(false)]
        public static bool IsExpandedWord(string word, Language language,
            LexiconPronunciation pronunciation,
            SP.ServiceProvider sp)
        {
            // If the phone count is more than the letter count, the word is probably expanded.
            if (PhonesMoreThanLetters(word, pronunciation.Symbolic, language))
            {
                // if this word is not spell, it is treated as expanded.
                if (!pronunciation.IsSpellOut(sp))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// PhonesMoreThanLetters.
        /// </summary>
        /// <param name="word">Word.</param>
        /// <param name="pronunciation">Pronunciation.</param>
        /// <param name="language">Language.</param>
        /// <returns>Bool.</returns>
        public static bool PhonesMoreThanLetters(string word, string pronunciation, Language language)
        {
            // get phones
            string[] phones = Pronunciation.GetPurePhones(pronunciation);

            string purePron = string.Empty;
            foreach (string p in phones)
            {
                purePron += p + ' ';
            }

            purePron = purePron.TrimEnd();

            int phonesCount = phones.Length;

            // for some language, letter 'x' map to two phones 'k s', we need to count x as one phone.
            if (language == Language.EnUS ||
                language == Language.EnGB ||
                language == Language.EnIN ||
                language == Language.EnAU ||
                language == Language.EnCA ||
                language == Language.FrFR ||
                language == Language.FrCA ||
                language == Language.PlPL ||
                language == Language.EsES ||
                language == Language.EsMX ||
                language == Language.ItIT ||
                language == Language.DeDE ||
                language == Language.SvSE ||
                language == Language.PtBR ||
                language == Language.NbNO)
            {
                // get letter 'x' count.
                int xCount = word.ToLower().Count(x => x.Equals('x'));

                // each 'x', we need to sub a phone count.
                phonesCount = phonesCount - xCount;
            }

            // for Es-ES, because of some constraints, an epenthetic /e/ is inserted before word-initial cluster beginning with /s/
            // http://en.wikipedia.org/wiki/Spanish_phonology#Phonotactics
            if (language == Language.EsES)
            {
                if ((word.ToLower().StartsWith("s") && purePron.StartsWith("e s")) ||
                    (word.ToLower().StartsWith("w") && purePron.StartsWith("g w")))
                {
                    phonesCount--;
                }
            }

            // In en-US, en-GB, letter 'u' usually read as 'y uw' or 'y uh' or 'y ax' in a normal word.
            // We should count it as one phone
            if (language == Language.EnUS ||
                language == Language.EnIN ||
                language == Language.EnCA)
            {
                if (word.ToLower().Contains("u") &&
                    (pronunciation.Contains("y uw") || pronunciation.Contains("y uh") || pronunciation.Contains("y ax")))
                {
                    phonesCount--;
                }

                if ((language == Language.EnUS || language == Language.EnCA) &&
                   ((word.ToLower().Contains("sm") && pronunciation.Contains("z ax m")) ||
                    (word.ToLower().Contains("zi") && pronunciation.Contains("t s iy"))))
                {
                    phonesCount--;
                }
            }
            else if (language == Language.EnGB ||
                     language == Language.EnAU)
            {
                if (word.ToLower().Contains("u") &&
                    (pronunciation.Contains("j uw") || pronunciation.Contains("j uh") ||
                    pronunciation.Contains("j ur") || pronunciation.Contains("j oo") ||
                    pronunciation.Contains("j ax") || pronunciation.Contains("j w ax")))
                {
                    phonesCount--;
                }

                if (word.ToLower().Contains("sm") && pronunciation.Contains("z ax m"))
                {
                    phonesCount--;
                }

                if (word.ToLower().Contains("ir") && purePron.Contains("ay ax r"))
                {
                    phonesCount--;
                }
            }
            else if (language == Language.DeDE)
            {
                if (word.ToLower().Contains("u") && pronunciation.Contains("y uw"))
                {
                    phonesCount--;
                }
            }

            // for De-DE language, phone "gs" map to no letters, we don't need to count "gs" as one phone.
            if (language == Language.DeDE)
            {
                // get phone "gs" count.
                int gsCount = phones.Count(x => x.Equals("gs"));

                // each "gs", we need to sub a phone count.
                phonesCount = phonesCount - gsCount;
            }

            // for fr-FR, liaison phone "l_z" "l_n" "l_t" and "?" shouldn't be counted in.
            if (language == Language.FrFR || language == Language.FrCA)
            {
                foreach (string phone in phones)
                {
                    if (phone == "l_z" ||
                        phone == "l_n" ||
                        phone == "l_t" ||
                        phone == "?")
                    {
                        phonesCount--;
                    }
                }
            }

            // In ru-RU, in a normal word, letter 'е' usually read as 'y ih' or 'y e' or 'y o';letter 'ё' usually read as 'y o';
            // letter 'я' usually read as 'y ah' or 'y a' or 'y ih'; letter 'ю' usually read as 'y u'.
            // We should count it as one phone
            if (language == Language.RuRU)
            {
                if (word.ToLower().Contains("е") &&
                    (pronunciation.Contains("y ih") || pronunciation.Contains("y e") || pronunciation.Contains("y o")))
                {
                    phonesCount--;
                }

                if (word.ToLower().Contains("ё") && pronunciation.Contains("y o"))
                {
                    phonesCount--;
                }

                if (word.ToLower().Contains("я") &&
                    (pronunciation.Contains("y ah") || pronunciation.Contains("y a") || pronunciation.Contains("y ih")))
                {
                    phonesCount--;
                }

                if (word.ToLower().Contains("ю") && pronunciation.Contains("y u"))
                {
                    phonesCount--;
                }
            }

            // get letters count in word.
            int lettersCount = GetLettersCount(word);

            double ratio = 4.99 / 4.0;  // Allow one additional phone for each 4 letters. e.g.
            // "tl" read s "t ax l"
            // suffix "lism" read as "l ih z ax m"
            if (language == Language.DeDE)
            {
                ratio = 6.99 / 6.0;
            }

            if (phonesCount >= lettersCount * ratio)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Compare objects that derived from LexicalItem.
        /// </summary>
        /// <param name="obj1">Object 1.</param>
        /// <param name="obj2">Object 2.</param>
        /// <returns>true for equal
        ///     (null, null) => equal
        ///     (null, deleted) => equal
        ///     (deleted, deleted) => equal.
        /// </returns>
        public static bool Equals(LexicalItem obj1, LexicalItem obj2)
        {
            if (obj1 == obj2)
            {
                return true;
            }

            if ((obj1 == null || obj1.Status == Lexicon.LexiconStatus.Deleted) &&
                (obj2 == null || obj2.Status == Lexicon.LexiconStatus.Deleted))
            {
                return true;
            }

            if (obj1.Alias != obj2.Alias ||
                obj1.Grapheme != obj2.Grapheme ||
                obj1.Language != obj2.Language ||
                obj1.LexiconType != obj2.LexiconType ||
                obj1.Polyphonic != obj2.Polyphonic ||
                obj1.Status != obj2.Status ||
                obj1.Text != obj2.Text)
            {
                return false;
            }

            Collection<LexiconPronunciation> prons1 = obj1.Pronunciations;
            Collection<LexiconPronunciation> prons2 = obj2.Pronunciations;
            int pronsLength = prons1.Count;
            if (pronsLength != prons2.Count)
            {
                return false;
            }

            for (int i = 0; i < pronsLength; i++)
            {
                if (!LexiconPronunciation.Equals(prons1[i], prons2[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Remove change history.
        /// </summary>
        public void RemoveHistory()
        {
            if (Status == Lexicon.LexiconStatus.Added || Status == Lexicon.LexiconStatus.Changed)
            {
                for (int i = _pronunciations.Count - 1; i >= 0; i--)
                {
                    if (_pronunciations[i].Status == Lexicon.LexiconStatus.Deleted)
                    {
                        _pronunciations.RemoveAt(i);
                    }
                    else
                    {
                        _pronunciations[i].RemoveHistory();
                    }
                }

                RemoveHistoryOnPronPosition();
                OldGrapheme = Grapheme;
                Status = Lexicon.LexiconStatus.Original;
            }
        }

        /// <summary>
        /// Reject current pronunciation's position, recover to the original posotion.
        /// </summary>
        public void RejectPronPositionChange()
        {
            if (_pronunciations == null)
            {
                throw new ArgumentNullException("prons");
            }

            if (_pronunciations.Count == 0 ||
                _pronunciations[0].OldPosition == LexiconPronunciation.DefaultPositionIndex)
            {
                return;
            }

            List<LexiconPronunciation> pronList = new List<LexiconPronunciation>(_pronunciations);
            pronList.Sort(ComparePronOriginalPosition);
            _pronunciations.Clear();
            foreach (LexiconPronunciation pron in pronList)
            {
                _pronunciations.Add(pron);
            }

            RemoveHistoryOnPronPosition();
        }

        /// <summary>
        /// Set status of all domains to specified status
        /// Change all status if "forceApply" is set as True
        /// Change the statu only if its status is Original when "forceApply" is set as False.
        /// </summary>
        /// <param name="status">LexiconStatus.</param>
        /// <param name="forceApply">Whether force apply status.</param>
        public void SetStatusOnAllDomains(Lexicon.LexiconStatus status, bool forceApply)
        {
            foreach (LexiconPronunciation pron in _pronunciations)
            {
                foreach (LexiconItemProperty property in pron.Properties)
                {
                    foreach (DomainItem domain in property.Domains.Values)
                    {
                        if (forceApply)
                        {
                            domain.Status = status;
                        }
                        else if (domain.Status == Lexicon.LexiconStatus.Original)
                        {
                            domain.Status = status;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Clean all domains tags.
        /// </summary>
        public void CleanAllDomainTags()
        {
            // Clear domain tags when they will save for sharing an overall domain tag as follows:
            // <lexiconWords lang="en-US" domain="address" xmlns="http://schemas.microsoft.com/tts">
            foreach (LexiconPronunciation pron in _pronunciations)
            {
                foreach (LexiconItemProperty property in pron.Properties)
                {
                    property.Domains.Clear();
                }
            }
        }

        /// <summary>
        /// Update the state of LexicalItem.
        /// </summary>
        public void RefreshStatus()
        {
            int[] counts = new int[] { 0, 0, 0, 0 };

            foreach (LexiconPronunciation pron in _pronunciations)
            {
                counts[(int)pron.Status]++;
            }

            if (counts[(int)Lexicon.LexiconStatus.Deleted] == _pronunciations.Count)
            {
                // All pronunciations are in deleted state
                Status = Lexicon.LexiconStatus.Deleted;
                Grapheme = OldGrapheme;
            }
            else if (counts[(int)Lexicon.LexiconStatus.Added] == _pronunciations.Count)
            {
                // All pronunciations are in added state
                Status = Lexicon.LexiconStatus.Added;
                OldGrapheme = Grapheme;
            }
            else if (counts[(int)Lexicon.LexiconStatus.Original] == _pronunciations.Count &&
                OldGrapheme == Grapheme && (_pronunciations.Count == 0 ||
                _pronunciations[0].OldPosition == LexiconPronunciation.DefaultPositionIndex))
            {
                // All pronunciations are in original state and current word text is same as original one
                if (Status != Lexicon.LexiconStatus.Checked)
                {
                    Status = Lexicon.LexiconStatus.Original;
                }
            }
            else
            {
                Status = Lexicon.LexiconStatus.Changed;
            }
        }

        /// <summary>
        /// Check whether only contains one target domain.
        /// </summary>
        /// <param name="domainTag">Domain tag.</param>
        /// <returns>Whether only contains one domain.</returns>
        public bool OnlyContainsOneDomain(string domainTag)
        {
            Helper.ThrowIfNull(domainTag);

            bool valid = true;
            foreach (LexiconPronunciation pron in _pronunciations)
            {
                if (!pron.OnlyContainsOneDomain(domainTag))
                {
                    valid = false;
                    break;
                }
            }

            return valid;
        }

        /// <summary>
        /// Whether this word is an Acronym.
        /// </summary>
        /// <returns>Whether be Acronym.</returns>
        public bool IsAcronym()
        {
            const int MaxAcyonymLength = 4;

            bool isAcronym = false;
            if (!string.IsNullOrEmpty(_grapheme) && _grapheme.Length <= MaxAcyonymLength &&
                Helper.IsUpper(_grapheme))
            {
                isAcronym = true;
            }

            return isAcronym;
        }

        /// <summary>
        /// Whether this word is expanded in lexicon.
        /// </summary>
        /// <param name="domain">Domain.</param>
        /// <param name="sp">ServiceProvider.</param>
        /// <returns>Whether word is expanded.</returns>
        [CLSCompliant(false)]
        public bool IsExpandedWord(string domain, SP.ServiceProvider sp)
        {
            // Compare phone count for each pronunication, if phone count more than the length of the word,
            // We consider it is expanded.
            foreach (LexiconPronunciation pron in this.Pronunciations)
            {
                // if word is expanded in special domain.
                if (pron.Status != Lexicon.LexiconStatus.Deleted &&
                    pron.Properties.Where(x => x.Domains.ContainsKey(domain)).Count() != 0)
                {
                    if (LexicalItem.IsExpandedWord(this.Grapheme, Language, pron, sp))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Whether contains specified domain.
        /// </summary>
        /// <param name="domainTag">DomainTag.</param>
        /// <returns>Whether contains.</returns>
        public bool ContainsDomain(string domainTag)
        {
            if (string.IsNullOrEmpty(domainTag))
            {
                throw new ArgumentNullException();
            }

            bool contains = false;
            domainTag = domainTag.ToLower(CultureInfo.InvariantCulture);
            foreach (LexiconPronunciation pron in this._pronunciations)
            {
                foreach (LexiconItemProperty property in pron.Properties)
                {
                    if (property.Domains.ContainsKey(domainTag))
                    {
                        contains = true;
                        break;
                    }
                }

                if (contains)
                {
                    break;
                }
            }

            return contains;
        }

        /// <summary>
        /// Whether specified domain is reviewed.
        /// </summary>
        /// <param name="domainTag">Domain tag.</param>
        /// <returns>Whether reviewed.</returns>
        public bool IsReviewedDomain(string domainTag)
        {
            if (string.IsNullOrEmpty(domainTag))
            {
                throw new ArgumentNullException();
            }

            bool reviewed = false;
            domainTag = domainTag.ToLower(CultureInfo.InvariantCulture);
            foreach (LexiconPronunciation pron in this._pronunciations)
            {
                foreach (LexiconItemProperty property in pron.Properties)
                {
                    if (property.Domains.ContainsKey(domainTag))
                    {
                        // All domain pronunciation gets reviewed once
                        if (property.Domains[domainTag].Checked)
                        {
                            reviewed = true;
                            break;
                        }
                    }
                }

                if (reviewed)
                {
                    break;
                }
            }

            return reviewed;
        }

        /// <summary>
        /// Import domain LexicalItem.
        /// </summary>
        /// <param name="domainLexItem">LexicalItem.</param>
        /// <param name="domainTag">Domain tag.</param>
        /// <param name="trustDomainLexicon">Whether domain lexion is trusting.</param>
        /// <returns>ErrorSet.</returns>
        public ErrorSet ImportDomainLexicalItem(LexicalItem domainLexItem, string domainTag, bool trustDomainLexicon)
        {
            Helper.ThrowIfNull(domainLexItem);
            Helper.ThrowIfNull(domainTag);

            if (!domainLexItem.OnlyContainsOneDomain(domainTag))
            {
                throw new InvalidDataException("It is invalid to include any other domain in property level.");
            }

            bool imported = false;
            ErrorSet importError = new ErrorSet();
            bool isFirstPron = true;

            // Needn't set p="true" tag for general domain, 
            // because the order of pronunciation means the priority of general domain.
            domainTag = domainTag.ToLower(CultureInfo.InvariantCulture);
            if (domainTag.Equals(DomainItem.GeneralDomain))
            {
                isFirstPron = false;
            }

            if (!IsReviewedDomain(domainTag))
            {
                foreach (LexiconPronunciation domainPron in domainLexItem.Pronunciations)
                {
                    LexiconPronunciation duplicatePron = FindLexiconPronunciation(domainPron.Symbolic, false);
                    if (duplicatePron == null)
                    {
                        duplicatePron = FindLexiconPronunciation(domainPron.Symbolic, true);
                    }

                    if (duplicatePron != null)
                    {
                        if (duplicatePron.ImportDomainPronunciation(domainPron, domainTag, isFirstPron))
                        {
                            imported = true;
                        }

                        isFirstPron = false;
                    }
                    else
                    {
                        if (trustDomainLexicon)
                        {
                            // if trustDomainLexicon is true, add new pronunication to lexicon item.
                            _pronunciations.Add(domainPron);
                        }
                        else
                        {
                            importError.Add(LexiconError.NewDomainPronunciation, domainTag, domainLexItem.Grapheme, domainPron.Symbolic);
                        }
                    }
                }
            }

            if (imported)
            {
                Status = Lexicon.LexiconStatus.Original;
                Reviewed = false;
            }

            return importError;
        }

        /// <summary>
        /// Split to dictionary about domain LexicalItem
        /// The key is domain tag and the value is LexicalItem.
        /// </summary>
        /// <returns>Domain LexicalItem dictionary.</returns>
        public Dictionary<string, LexicalItem> SplitToDomainLexicalItems()
        {
            Dictionary<string, LexicalItem> domainLexItems = new Dictionary<string, LexicalItem>();
            foreach (LexiconPronunciation pron in _pronunciations)
            {
                foreach (LexiconItemProperty property in pron.Properties)
                {
                    foreach (KeyValuePair<string, DomainItem> pair in property.Domains)
                    {
                        LexicalItem targetLexItem = null;
                        if (domainLexItems.ContainsKey(pair.Key))
                        {
                            targetLexItem = domainLexItems[pair.Key];
                        }
                        else
                        {
                            targetLexItem = Clone();
                            targetLexItem.Pronunciations.Clear();
                            domainLexItems.Add(pair.Key, targetLexItem);
                        }

                        LexiconPronunciation targetPron = targetLexItem.FindLexiconPronunciation(pron.Symbolic, false);
                        if (targetPron == null)
                        {
                            targetPron = pron.Clone();
                            targetPron.Properties.Clear();
                            targetLexItem.Pronunciations.Add(targetPron);
                        }

                        bool propertyExist = false;
                        foreach (LexiconItemProperty targetProperty in targetPron.Properties)
                        {
                            if (targetProperty.EqualsWithoutDomain(property))
                            {
                                propertyExist = true;
                                break;
                            }
                        }

                        if (!propertyExist)
                        {
                            targetPron.Properties.Add(property.Clone());
                        }
                    }
                }
            }

            foreach (KeyValuePair<string, LexicalItem> lexItemPair in domainLexItems)
            {
                lexItemPair.Value.SortPronunciationsForDomain(lexItemPair.Key);
            }

            foreach (KeyValuePair<string, LexicalItem> lexItemPair in domainLexItems)
            {
                lexItemPair.Value.CleanAllDomainTags();
            }

            return domainLexItems;
        }

        /// <summary>
        /// Find LexiconPronunciation by symbolic.
        /// </summary>
        /// <param name="symbolic">Pronunciation symbolic.</param>
        /// <param name="ignore">Ignore stress and syllable.</param>
        /// <returns>LexiconPronunciation.</returns>
        public LexiconPronunciation FindLexiconPronunciation(string symbolic, bool ignore)
        {
            Helper.ThrowIfNull(symbolic);

            LexiconPronunciation pronunciation = null;
            if (ignore)
            {
                symbolic = Pronunciation.RemoveStress(symbolic);
                symbolic = Pronunciation.RemoveSyllable(symbolic);
            }

            foreach (LexiconPronunciation pron in _pronunciations)
            {
                string pronSymbolic = pron.Symbolic;
                if (ignore)
                {
                    pronSymbolic = Pronunciation.RemoveStress(pronSymbolic);
                    pronSymbolic = Pronunciation.RemoveSyllable(pronSymbolic);
                }

                if (symbolic.Equals(pronSymbolic, StringComparison.Ordinal))
                {
                    pronunciation = pron;
                    break;
                }
            }

            return pronunciation;
        }

        /// <summary>
        /// Validate lexicon item.
        /// </summary>
        /// <param name="ttsPhoneSet">Phone set to validate lexicon item's pronunciation.</param>
        /// <param name="ttsPosSet">Pos set of the lexicon item.</param>
        /// <param name="attributeSchema">Attribute schema.</param>
        /// <returns>Error set of the validation.</returns>
        public ErrorSet Validate(TtsPhoneSet ttsPhoneSet, TtsPosSet ttsPosSet,
            LexicalAttributeSchema attributeSchema)
        {
            Debug.Assert(ttsPhoneSet != null);
            Debug.Assert(ttsPosSet != null || attributeSchema != null);
            ErrorSet errorSet = new ErrorSet();

            // Merge duplicate pronunciation node
            Collection<LexiconPronunciation> distinctPronunciations = new Collection<LexiconPronunciation>();
            Dictionary<string, int> pronunciationIndex = new Dictionary<string, int>();
            int pronunciationCount = 0;
            foreach (LexiconPronunciation lexPron in Pronunciations)
            {
                // Validate duplicate pronunciation node
                if (pronunciationIndex.ContainsKey(lexPron.Symbolic))
                {
                    errorSet.Add(LexiconError.DuplicatePronunciationNode, Grapheme, lexPron.Symbolic);

                    lexPron.Valid = false;
                    foreach (LexiconItemProperty property in lexPron.Properties)
                    {
                        Collection<LexiconItemProperty> targetProperties =
                            distinctPronunciations[pronunciationIndex[lexPron.Symbolic]].Properties;
                        if (!targetProperties.Contains(property))
                        {
                            targetProperties.Add(property);
                        }
                        else
                        {
                            errorSet.Add(LexiconError.DuplicateProperty, Grapheme, lexPron.Symbolic);
                        }
                    }
                }
                else
                {
                    distinctPronunciations.Add(lexPron);
                    pronunciationIndex[lexPron.Symbolic] = pronunciationCount;
                    pronunciationCount++;
                }
            }

            _pronunciations = distinctPronunciations;

            int invalidPronNodeNum = 0;
            foreach (LexiconPronunciation lexPron in Pronunciations)
            {
                // lexPron.Valid will be false if contains error.
                ValidatePronunciation(Grapheme, lexPron, ttsPhoneSet, errorSet);

                // Validate the POS information
                int invalidPropertyNum = 0;
                foreach (LexiconItemProperty property in lexPron.Properties)
                {
                    // Lexicon schema ensures that the POS property is existed
                    Debug.Assert(property.PartOfSpeech != null);

                    if (PosItem.Validate(property.PartOfSpeech.Value,
                        ttsPosSet, attributeSchema).Count > 0)
                    {
                        errorSet.Add(LexiconError.UnrecognizedPos, Grapheme,
                            lexPron.Symbolic, property.PartOfSpeech.Value);
                        property.Valid = false;
                    }

                    if (attributeSchema != null)
                    {
                        ErrorSet attributeErrorSet = ValidateAttributeSet(property, attributeSchema);
                        foreach (Error error in attributeErrorSet.Errors)
                        {
                            errorSet.Add(LexiconError.AttributeError, error, Grapheme, lexPron.Symbolic);
                        }

                        if (attributeErrorSet.Contains(ErrorSeverity.MustFix))
                        {
                            property.Valid = false;
                        }
                    }

                    if (property.AttributeSet.Count > 0 &&
                        (property.Case != null || property.Gender != null || property.Number != null))
                    {
                        errorSet.Add(LexiconError.MixedPropertyDefinition, Grapheme, lexPron.Symbolic);
                        property.Valid = false;
                    }
                    else
                    {
                        ValidateCase(Grapheme, property, errorSet);
                        ValidateGender(Grapheme, property, errorSet);
                        ValidateNumber(Grapheme, property, errorSet);
                    }

                    if (!property.Valid)
                    {
                        invalidPropertyNum++;
                    }
                }

                if (lexPron.Properties.Count == invalidPropertyNum)
                {
                    lexPron.Valid = false;
                }

                if (!lexPron.Valid)
                {
                    invalidPronNodeNum++;
                }
            }

            if (Pronunciations.Count == invalidPronNodeNum)
            {
                Valid = false;
            }

            return errorSet;
        }

        /// <summary>
        /// Merge duplicate pronunciation.
        /// </summary>
        public void MergeDuplicatePronunciation()
        {
            // Merge duplicate pronunciation node
            Collection<LexiconPronunciation> distinctPronunciations = new Collection<LexiconPronunciation>();
            Dictionary<string, int> pronunciationIndex = new Dictionary<string, int>();
            int pronunciationCount = 0;
            foreach (LexiconPronunciation lexPron in Pronunciations)
            {
                if (pronunciationIndex.ContainsKey(lexPron.Symbolic))
                {
                    foreach (LexiconItemProperty property in lexPron.Properties)
                    {
                        Collection<LexiconItemProperty> targetProperties =
                            distinctPronunciations[pronunciationIndex[lexPron.Symbolic]].Properties;
                        if (!targetProperties.Contains(property))
                        {
                            targetProperties.Add(property);
                        }
                    }
                }
                else
                {
                    distinctPronunciations.Add(lexPron.Clone());
                    pronunciationIndex[lexPron.Symbolic] = pronunciationCount;
                    pronunciationCount++;
                }
            }

            _pronunciations = distinctPronunciations;
        }

        /// <summary>
        /// Clone current word.
        /// </summary>
        /// <returns>Cloned word.</returns>
        public LexicalItem Clone()
        {
            LexicalItem clonedWord = new LexicalItem();
            clonedWord.Alias = _alias;
            clonedWord.Comment = _comment;
            clonedWord.Frequency = _frequency;
            clonedWord.Language = _language;
            clonedWord.LexiconType = _lexiconType;
            clonedWord.Grapheme = _grapheme;
            clonedWord.OldGrapheme = _oldGrapheme;
            clonedWord.Reviewed = Reviewed;
            clonedWord.Status = Status;
            clonedWord._text = _text;
            clonedWord.Valid = _valid;
            clonedWord.Parent = Parent;

            foreach (LexiconPronunciation pron in _pronunciations)
            {
                LexiconPronunciation clonedPron = pron.Clone();
                clonedWord.Pronunciations.Add(clonedPron);
                clonedPron.Parent = clonedWord;
            }

            return clonedWord;
        }

        /// <summary>
        /// Verify if the LexicalItem only contains one prefered pronunciation of each domain.
        /// </summary>
        /// <returns>The ErrorSet.</returns>
        public ErrorSet VerifyOnlyOnePreferedPronOfEachDomain()
        {
            ErrorSet errorSet = new ErrorSet();
            Collection<string> preferedDomains = new Collection<string>();
            if (Status != Lexicon.LexiconStatus.Deleted)
            {
                foreach (LexiconPronunciation pron in Pronunciations)
                {
                    if (pron.Status != Lexicon.LexiconStatus.Deleted)
                    {
                        foreach (LexiconItemProperty property in pron.Properties)
                        {
                            if (property.Status != Lexicon.LexiconStatus.Deleted)
                            {
                                foreach (DomainItem domain in property.Domains.Values)
                                {
                                    if (domain.IsFirstPronunciation && domain.Status != Lexicon.LexiconStatus.Deleted)
                                    {
                                        if (preferedDomains.Contains(domain.Value))
                                        {
                                            errorSet.Add(new Error(LexicalItemError.MoreThanOnePreferedPronOfSpecificDomain,
                                                pron.Symbolic, domain.Value));
                                        }
                                        else
                                        {
                                            preferedDomains.Add(domain.Value);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return errorSet;
        }

        /// <summary>
        /// Convert to new attribute format lexical item.
        /// </summary>
        /// <param name="errorSet">The error set.</param>
        public void ToNewAttributeFormatLexicalItem(ErrorSet errorSet)
        {
            foreach (LexiconPronunciation pron in _pronunciations)
            {
                pron.ToNewAttributeFormatPronunciation(errorSet);
            }
        }

        /// <summary>
        /// Write a lexicon item to string.
        /// </summary>
        /// <returns>String of lexicon item.</returns>
        public string WriteToString()
        {
            StringBuilder sb = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.NamespaceHandling = NamespaceHandling.OmitDuplicates;
            StringWriter stringWriter = null;

            try
            {
                stringWriter = new StringWriter(sb);
                using (XmlWriter xmlWriter = XmlWriter.Create(stringWriter, settings))
                {
                    stringWriter = null;
                    WriteToXml(xmlWriter);
                }
            }
            finally
            {
                if (stringWriter != null)
                {
                    stringWriter.Dispose();
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Sets lexicon item cache value.
        /// </summary>
        public void SetCacheValue()
        {
            if (string.IsNullOrEmpty(_cacheValue))
            {
                _cacheValue = this.WriteToString();
            }
        }

        #endregion

        #region Override

        /// <summary>
        /// Convert to string presentation.
        /// </summary>
        /// <returns>String presentation.</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Word ");
            sb.Append(Grapheme);
            sb.AppendLine();

            for (int i = 0; i < Pronunciations.Count; i++)
            {
                LexiconPronunciation pronun = Pronunciations[i];
                sb.Append("Pronunciation");
                sb.Append(i.ToString(CultureInfo.InvariantCulture));
                sb.Append(" ");
                sb.Append(pronun.Symbolic);
                sb.AppendLine();

                sb.Append(Pronunciations[i].ToString());
            }

            return sb.ToString();
        }

        #endregion

        #region Internal methods

        /// <summary>
        /// MemberwiseClone.
        /// </summary>
        /// <returns>New instance.</returns>
        public new LexicalItem MemberwiseClone()
        {
            return (LexicalItem)base.MemberwiseClone();
        }

        /// <summary>
        /// Check whether the pronunciation exists.
        /// </summary>
        /// <param name="pron">Pronunciation.</param>
        /// <returns>True for existing, otherwise false.</returns>
        public bool ContainsPronunciation(string pron)
        {
            return FindPronunciation(pron) != null;
        }

        /// <summary>
        /// Find the pronunciation node.
        /// </summary>
        /// <param name="pron">Pronunciation.</param>
        /// <returns>Found pronunciation node.</returns>
        public LexiconPronunciation FindPronunciation(string pron)
        {
            if (string.IsNullOrEmpty(pron))
            {
                throw new ArgumentNullException("pron");
            }

            LexiconPronunciation foundPron = null;
            foreach (LexiconPronunciation originalPron in this.Pronunciations)
            {
                if (originalPron.Symbolic.Equals(pron, StringComparison.OrdinalIgnoreCase))
                {
                    foundPron = originalPron;
                    break;
                }
            }

            return foundPron;
        }

        /// <summary>
        /// Find the pronunciation node.
        /// </summary>
        /// <param name="pron">Pronunciation.</param>
        /// <param name="ignoreLiaison">True for ignoring Liaison.</param>
        /// <returns>Found pronunciation node.</returns>
        public LexiconPronunciation FindPronunciation(string pron, bool ignoreLiaison)
        {
            if (string.IsNullOrEmpty(pron))
            {
                throw new ArgumentNullException("pron");
            }

            LexiconPronunciation foundPron = null;
            if (!ignoreLiaison)
            {
                foundPron = FindPronunciation(pron);
            }
            else
            {
                pron = Pronunciation.RemoveLiaison(pron, _language);
                foreach (LexiconPronunciation originalPron in this.Pronunciations)
                {
                    string newPron = Pronunciation.RemoveLiaison(originalPron.Symbolic, _language);
                    if (newPron.Equals(pron, StringComparison.OrdinalIgnoreCase))
                    {
                        foundPron = originalPron;
                        break;
                    }
                }
            }

            return foundPron;
        }

        /// <summary>
        /// Add the pronunciations for new item into original item.
        /// </summary>
        /// <param name="newItem">New item.</param>
        public void AddRange(LexicalItem newItem)
        {
            if (newItem != null)
            {
                foreach (LexiconPronunciation pronunciation in newItem.Pronunciations)
                {
                    this.Pronunciations.Add(pronunciation);
                }
            }
        }

        /// <summary>
        /// Load LexicalItem from XmlNode.
        /// </summary>
        /// <param name="parentLexicon">Lexicon.</param>
        /// <param name="wordNode">XmlNode.</param>
        /// <param name="nsmgr">XmlNamespaceManager.</param>
        /// <param name="contentController">Object.</param>
        /// <param name="errorSet">ErrorSet.</param>
        /// <returns>LexicalItem.</returns>
        internal static LexicalItem Load(Lexicon parentLexicon, XmlNode wordNode, XmlNamespaceManager nsmgr, Lexicon.ContentControler contentController, ErrorSet errorSet)
        {
            Debug.Assert(parentLexicon != null && wordNode != null && nsmgr != null &&
                contentController != null && errorSet != null);

            XmlElement wordElement = wordNode as XmlElement;
            LexicalItem lexiconItem = new LexicalItem(parentLexicon.Language);
            lexiconItem.Parent = parentLexicon;

            string grapheme = wordElement.GetAttribute("v");
            if (string.IsNullOrEmpty(grapheme))
            {
                errorSet.Add(LexiconError.InvalidWordEntry,
                    new Error(WordEntryError.EmptyWord), grapheme);
                lexiconItem = null;
            }
            else if (!grapheme.Trim().Equals(grapheme, StringComparison.OrdinalIgnoreCase))
            {
                errorSet.Add(LexiconError.InvalidWordEntry,
                        new Error(WordEntryError.LeadingOrTrailingSpace), grapheme);
                lexiconItem = null;
            }
            else
            {
                Regex regex = new Regex("(  )|\t");
                if (regex.IsMatch(grapheme.Trim()))
                {
                    errorSet.Add(LexiconError.InvalidWordEntry,
                        new Error(WordEntryError.ContainingTabOrMultipleSpaces), grapheme);
                }
            }

            if (lexiconItem != null)
            {
                // Before share lexicon object to lexicon reviewer tool,
                // we drop those items if they have "deleted" status
                string statusValue = wordElement.GetAttribute("s");
                if (!string.IsNullOrEmpty(statusValue))
                {
                    lexiconItem.Status = (Lexicon.LexiconStatus)Enum.Parse(typeof(Lexicon.LexiconStatus),
                        statusValue, true);
                }

                if (!contentController.IsHistoryCheckingMode && lexiconItem.Status == Lexicon.LexiconStatus.Deleted)
                {
                    lexiconItem = null;
                }
                else
                {
                    lexiconItem.Alias = wordElement.GetAttribute("alias");
                    CultureInfo cultureInfo = new CultureInfo(Localor.LanguageToString(parentLexicon.Language), false);
                    lexiconItem._text = grapheme;
                    lexiconItem.Grapheme = contentController.IsCaseSensitive ? grapheme.Trim() : grapheme.Trim().ToLower(cultureInfo);
                    lexiconItem.OldGrapheme = lexiconItem.Grapheme;

                    // Check whether this word is reviewed
                    string reviewedValue = wordElement.GetAttribute("r");
                    if (!string.IsNullOrEmpty(reviewedValue))
                    {
                        lexiconItem.Reviewed = bool.Parse(reviewedValue);
                    }

                    // Get word's frequency. If there's no such information, set frequency to zero
                    int frequency = 0;
                    int.TryParse(wordElement.GetAttribute("f"), out frequency);
                    lexiconItem.Frequency = frequency;

                    // Load comment
                    lexiconItem.Comment = wordElement.GetAttribute("c");

                    if (lexiconItem.Status != Lexicon.LexiconStatus.Original)
                    {
                        // Get original word text.
                        string originalWordText = wordElement.GetAttribute("vo");
                        if (!string.IsNullOrEmpty(originalWordText))
                        {
                            lexiconItem.OldGrapheme = originalWordText;
                        }
                    }

                    foreach (XmlNode pronNode in wordNode.SelectNodes("tts:p", nsmgr))
                    {
                        LexiconPronunciation lexPron = LexiconPronunciation.Load(lexiconItem,
                            pronNode, nsmgr, contentController, errorSet);
                        if (lexPron != null)
                        {
                            lexiconItem.Pronunciations.Add(lexPron);
                        }
                    }
                }
            }

            return lexiconItem;
        }

        /// <summary>
        /// Write a lexicon item to the XML writer.
        /// </summary>
        /// <param name="writer">XML writer.</param>
        internal void WriteToXml(XmlWriter writer)
        {
            writer.WriteStartElement("w");
            writer.WriteAttributeString("v", _grapheme);
            if (!string.IsNullOrEmpty(_alias))
            {
                writer.WriteAttributeString("alias", _alias);
            }

            if (_frequency != 0)
            {
                writer.WriteAttributeString("f", _frequency.ToString(CultureInfo.InvariantCulture));
            }

            if (!string.IsNullOrEmpty(_comment))
            {
                writer.WriteAttributeString("c", _comment.Trim());
            }

            if (Status != Lexicon.LexiconStatus.Original)
            {
                writer.WriteAttributeString("s", Status.ToString());

                if (!string.IsNullOrEmpty(_oldGrapheme) && _grapheme != _oldGrapheme)
                {
                    writer.WriteAttributeString("vo", _oldGrapheme);
                }
            }

            if (Reviewed)
            {
                writer.WriteAttributeString("r", "true");
            }

            if (_pronunciations != null)
            {
                foreach (LexiconPronunciation lexPron in _pronunciations)
                {
                    lexPron.WriteToXml(writer);
                }
            }

            writer.WriteEndElement();
        }

        #endregion

        #region Private static instance method

        /// <summary>
        /// Get count of letters in word.
        /// </summary>
        /// <param name="word">Word.</param>
        /// <returns>Letters count.</returns>
        private static int GetLettersCount(string word)
        {
            // count all letters in word.
            return word.Count(x => char.IsLetter(x));
        }

        /// <summary>
        /// Validate case for the word.
        /// </summary>
        /// <param name="word">Word.</param>
        /// <param name="property">Lexicon item property.</param>
        /// <param name="errorSet">Error set.</param>
        private static void ValidateCase(string word, LexiconItemProperty property, ErrorSet errorSet)
        {
            // Validate the case property
            if (property.Case != null)
            {
                int caseId;
                ErrorSet caseErrorSet = CaseItem.StringToId(property.Case.Value, out caseId);
                foreach (Error error in caseErrorSet.Errors)
                {
                    errorSet.Add(LexiconError.CaseError, error, word);
                }

                if (caseErrorSet.Contains(ErrorSeverity.MustFix))
                {
                    property.Valid = false;
                }
            }
        }

        /// <summary>
        /// Validate gender for the word.
        /// </summary>
        /// <param name="word">Word.</param>
        /// <param name="property">Lexicon item property.</param>
        /// <param name="errorSet">Error set.</param>
        private static void ValidateGender(string word, LexiconItemProperty property, ErrorSet errorSet)
        {
            // Validate the gender property
            if (property.Gender != null)
            {
                ErrorSet genderErrorSet = GenderItem.Validate(property.Gender.Value);
                foreach (Error error in genderErrorSet.Errors)
                {
                    errorSet.Add(LexiconError.GenderError, error, word);
                }

                if (genderErrorSet.Contains(ErrorSeverity.MustFix))
                {
                    property.Valid = false;
                }
            }
        }

        /// <summary>
        /// Validate number for the word.
        /// </summary>
        /// <param name="word">Word.</param>
        /// <param name="property">Lexicon item property.</param>
        /// <param name="errorSet">Error set.</param>
        private static void ValidateNumber(string word, LexiconItemProperty property, ErrorSet errorSet)
        {
            // Validate the gender property
            if (property.Number != null)
            {
                int numberId;
                ErrorSet numberErrorSet = NumberItem.StringToId(property.Number.Value, out numberId);
                foreach (Error error in numberErrorSet.Errors)
                {
                    errorSet.Add(LexiconError.NumberError, error, word);
                }

                if (numberErrorSet.Contains(ErrorSeverity.MustFix))
                {
                    property.Valid = false;
                }
            }
        }

        /// <summary>
        /// Validate attribute set for the word.
        /// </summary>
        /// <param name="property">Lexicon item property.</param>
        /// <param name="attributeSchema">Lexical Attribute Schema.</param>
        /// <returns>Error set.</returns>
        private static ErrorSet ValidateAttributeSet(LexiconItemProperty property,
            LexicalAttributeSchema attributeSchema)
        {
            Debug.Assert(attributeSchema != null);
            ErrorSet attributeErrorSet = new ErrorSet();

            foreach (KeyValuePair<string, List<AttributeItem>> pair in property.AttributeSet)
            {
                foreach (AttributeItem attribute in pair.Value)
                {
                    AttributeCategory category = attributeSchema.GetRootCategory(pair.Key);
                    if (category == null)
                    {
                        attributeErrorSet.Add(LexicalAttributeError.InvalidCategory, pair.Key);
                    }
                    else if (category.Name.Equals(LexicalAttributeSchema.PosCategoryName, StringComparison.Ordinal))
                    {
                        attributeErrorSet.Add(LexicalAttributeError.InvalidDefinitionForPos, attribute.Value);
                    }
                    else
                    {
                        bool found = false;
                        foreach (AttributeValue value in category.Values)
                        {
                            if (value.Name.Equals(attribute.Value, StringComparison.Ordinal))
                            {
                                found = true;
                                break;
                            }
                        }

                        if (!found)
                        {
                            attributeErrorSet.Add(LexicalAttributeError.InvalidValue, attribute.Value, pair.Key);
                        }
                    }
                }
            }

            return attributeErrorSet;
        }

        /// <summary>
        /// Validate the pronunciation for the word.
        /// </summary>
        /// <param name="word">Word.</param>
        /// <param name="lexPron">Lexicon pronunciation.</param>
        /// <param name="ttsPhoneSet">TTS phone set.</param>
        /// <param name="errorSet">Error set.</param>
        private static void ValidatePronunciation(string word, LexiconPronunciation lexPron, TtsPhoneSet ttsPhoneSet,
            ErrorSet errorSet)
        {
            // Validate the pronunciation information
            ErrorSet pronErrorSet = Pronunciation.Validate(lexPron.Symbolic, ttsPhoneSet);
            bool invalid = false;
            foreach (Error error in pronErrorSet.Errors)
            {
                errorSet.Add(LexiconError.PronunciationError, error, word);
                if (error.Severity == ErrorSeverity.MustFix &&
                    !(error.Enum.Equals(PronunciationError.VowelAndSonorantCountLessThanMinimum) ||
                      error.Enum.Equals(PronunciationError.VowelAndSonorantCountGreaterThanMaximum) ||
                      error.Enum.Equals(PronunciationError.VowelCountLessThanMinimum) ||
                      error.Enum.Equals(PronunciationError.VowelCountGreaterThanMaximum)))
                {
                    invalid = true;
                }
            }

            lexPron.Valid = lexPron.Valid && !invalid;
        }

        /// <summary>
        /// Compare two pronunciation's original position.
        /// </summary>
        /// <param name="firstPron">First pronunciation to be compared.</param>
        /// <param name="secondPron">Second pronunciation to be compared.</param>
        /// <returns>
        /// Bigger than zero, firstPron's position bigger than the second one;
        /// Equal to zero, firstPron's position equal to the second one;
        /// less than zero, firstPron's position less than the second one.</returns>
        private static int ComparePronOriginalPosition(LexiconPronunciation firstPron, LexiconPronunciation secondPron)
        {
            return firstPron.OldPosition - secondPron.OldPosition;
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Remove history on pronunciation positions.
        /// </summary>
        private void RemoveHistoryOnPronPosition()
        {
            foreach (LexiconPronunciation pron in Pronunciations)
            {
                pron.OldPosition = LexiconPronunciation.DefaultPositionIndex;
            }
        }

        /// <summary>
        /// Sort pronunciations for domain.
        /// </summary>
        /// <param name="domainTag">Domain tag.</param>
        private void SortPronunciationsForDomain(string domainTag)
        {
            Collection<LexiconPronunciation> sortedProns = new Collection<LexiconPronunciation>();
            foreach (LexiconPronunciation pron in _pronunciations)
            {
                if (pron.IsFirstDomainPronunciation(domainTag))
                {
                    sortedProns.Insert(0, pron);
                }
                else
                {
                    sortedProns.Add(pron);
                }
            }

            _pronunciations = sortedProns;
        }

        #endregion
    }
}