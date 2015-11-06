//----------------------------------------------------------------------------
// <copyright file="ConfigurationInclude.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements the ConfigurationInclude class.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.FlowEngine
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml;
    using System.Xml.Schema;
    using Offline.Utility;

    /// <summary>
    /// Class to process the include element in configuration file.
    /// </summary>
    public class ConfigurationInclude : ConfigurationItemBase
    {
        #region Fields

        /// <summary>
        /// The name of include element.
        /// </summary>
        public const string ElementName = "include";

        /// <summary>
        /// The name of the "src" attribute.
        /// </summary>
        public const string SrcAttribute = "src";

        /// <summary>
        /// The name of the "skip" attribute.
        /// </summary>
        public const string SkipAttribute = "skip";

        #endregion

        #region Propeties

        /// <summary>
        /// Gets or sets the include source.
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the skip attribute.
        /// </summary>
        public bool Skip { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Parses the included file.
        /// </summary>
        /// <param name="resolver">The resolver to resolve the include file.</param>
        /// <returns>The Dictionary of ConfigurationModule.</returns>
        public Dictionary<string, ConfigurationModule> ParsesInclude(Configuration.ConfigurationResolver resolver)
        {
            Configuration config = new Configuration();

            config.ConfigurationResolve += resolver;

            config.Load(Source);

            Dictionary<string, ConfigurationModule> modules = config.GetAllModules();

            // Overwrite the inputs in the included file according to the inputs.
            foreach (ConfigurationInput item in Inputs.Values)
            {
                if (!ConfigurationReference.IsReference(item.Name))
                {
                    throw new ConfigurationException(
                        Helper.NeutralFormat("Input \"{0}\" in include element isn't reference", item.Name));
                }

                ConfigurationReference reference = new ConfigurationReference();
                reference.Parse(item.Name);

                if (!modules.ContainsKey(reference.Module))
                {
                    throw new ConfigurationException(
                        Helper.NeutralFormat(
                            "No such module name \"{0}\" found in include file, one of below expected - {1}", reference.Module,
                            string.Join(",", modules.Keys.ToArray())));
                }

                if (reference.Name == ConfigurationModule.SkipAttribute)
                {
                    // Overwrite the skip attribute.
                    modules[reference.Module].Skip = bool.Parse(item.Value);
                }
                else if (reference.Name == ConfigurationModule.KeepIntermediateDataAttribute)
                {
                    // Overwrite the keep intermediate data attribute.
                    modules[reference.Module].KeepIntermediateData = bool.Parse(item.Value);
                }
                else
                {
                    if (modules[reference.Module].Inputs.ContainsKey(reference.Name))
                    {
                        // Overwrite the exist input.
                        modules[reference.Module].Inputs[reference.Name].Value = item.Value;
                    }
                    else
                    {
                        // Create a new input.
                        ConfigurationInput input = new ConfigurationInput
                        {
                            Name = reference.Name,
                            Value = item.Value
                        };

                        modules[reference.Module].Inputs.Add(input.Name, input);
                    }

                    modules[reference.Module].Inputs[reference.Name].IsCdataSection = item.IsCdataSection;
                }
            }

            return modules;
        }

        #endregion

        #region Override Methods

        /// <summary>
        /// Loads the configuration include element from a given XmlElement.
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

            Source = element.Attributes[SrcAttribute].Value;

            if (string.IsNullOrEmpty(Source))
            {
                throw new ArgumentException(Helper.NeutralFormat("\"{0}\" element has an empty attribute \"{1}\"",
                    ElementName, SrcAttribute));
            }

            Skip = element.HasAttribute(SkipAttribute) ?
                bool.Parse(element.Attributes[SkipAttribute].Value) :
                false;

            LoadInputs(element);
        }

        /// <summary>
        /// Converts the configuration include element to XmlElement.
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
            e.SetAttribute(SrcAttribute, Source);

            foreach (string name in Inputs.Keys.SortBy(o => o))
            {
                e.AppendChild(Inputs[name].ToElement(doc, schema));
            }

            return e;
        }

        #endregion
    }
}