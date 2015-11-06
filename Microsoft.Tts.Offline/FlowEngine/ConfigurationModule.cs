//----------------------------------------------------------------------------
// <copyright file="ConfigurationModule.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements the ConfigurationModule class.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.FlowEngine
{
    using System;
    using System.Globalization;
    using System.Xml;
    using System.Xml.Schema;
    using Offline.Utility;

    /// <summary>
    /// Class to process the module element in configuration file.
    /// </summary>
    public class ConfigurationModule : ConfigurationItemBase
    {
        #region Fields

        /// <summary>
        /// The name of module element.
        /// </summary>
        public const string ElementName = "module";

        /// <summary>
        /// The name of the "name" attribute.
        /// </summary>
        public const string NameAttribute = "name";

        /// <summary>
        /// The name of the "type" attribute.
        /// </summary>
        public const string TypeAttribute = "type";

        /// <summary>
        /// The name of the "skip" attribute.
        /// </summary>
        public const string SkipAttribute = "skip";

        /// <summary>
        /// The name of the "keepIntermediateData" attribute.
        /// </summary>
        public const string KeepIntermediateDataAttribute = "keepIntermediateData";

        #endregion

        #region Propeties

        /// <summary>
        /// Gets or sets the namespace of module type.
        /// </summary>
        public string Namespace { get; set; }

        /// <summary>
        /// Gets or sets the name of module element.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the type of module element.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to skip this module.
        /// </summary>
        public bool Skip { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to keep the intermediate data of this module.
        /// </summary>
        public bool KeepIntermediateData { get; set; }

        /// <summary>
        /// Gets full type of this module.
        /// </summary>
        public string FullType
        {
            get
            {
                Helper.ThrowIfNull(Type);
                string fullType = Type;
                if (!string.IsNullOrEmpty(Namespace) &&
                    !Type.Contains(Delimitor.PeriodChar.ToString(CultureInfo.InvariantCulture)))
                {
                    fullType = Helper.NeutralFormat("{0}.{1}", Namespace, Type);
                }

                return fullType;
            }
        }

        #endregion

        #region Override Methods

        /// <summary>
        /// Loads the configuration module element from a given XmlElement.
        /// </summary>
        /// <param name="element">The given XmlElement to load.</param>
        public override void Load(XmlElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            if (element.Name != ElementName)
            {
                throw new ArgumentException(Helper.NeutralFormat("\"{0}\" expected by \"{1}\" inputed", ElementName,
                    element.Name));
            }

            Type = element.Attributes[TypeAttribute].Value;
            Name = element.Attributes[NameAttribute].Value;
            Skip = element.HasAttribute(SkipAttribute) ?
                bool.Parse(element.Attributes[SkipAttribute].Value) :
                false;
            KeepIntermediateData = element.HasAttribute(KeepIntermediateDataAttribute) ?
                bool.Parse(element.Attributes[KeepIntermediateDataAttribute].Value) :
                false;

            if (string.IsNullOrEmpty(Type))
            {
                throw new ArgumentException(Helper.NeutralFormat("\"{0}\" element has an empty attribute \"{1}\"",
                    ElementName, TypeAttribute));
            }

            if (string.IsNullOrEmpty(Name))
            {
                throw new ArgumentException(Helper.NeutralFormat("\"{0}\" element has an empty attribute \"{1}\"",
                    ElementName, NameAttribute));
            }

            LoadInputs(element);
        }

        /// <summary>
        /// Converts the configuration module element to XmlElement.
        /// </summary>
        /// <param name="doc">The given XmlDocument who will be the document of returned XmlElement.</param>
        /// <param name="schema">The given XmlSchema.</param>
        /// <returns>The XmlElement holds this input element.</returns>
        public override XmlElement ToElement(XmlDocument doc, XmlSchema schema)
        {
            if (doc == null)
            {
                throw new ArgumentNullException("doc");
            }

            if (schema == null)
            {
                throw new ArgumentNullException("schema");
            }

            XmlElement e = doc.CreateElement(ElementName, schema.TargetNamespace);
            e.SetAttribute(TypeAttribute, Type);
            e.SetAttribute(NameAttribute, Name);
            e.SetAttribute(SkipAttribute, Skip.ToString(CultureInfo.InvariantCulture).ToLowerInvariant());
            e.SetAttribute(KeepIntermediateDataAttribute,
                KeepIntermediateData.ToString(CultureInfo.InvariantCulture).ToLowerInvariant());

            foreach (string name in Inputs.Keys.SortBy(o => o))
            {
                e.AppendChild(Inputs[name].ToElement(doc, schema));
            }

            return e;
        }

        #endregion
    }
}