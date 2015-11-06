// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LinguisticFeatureConfig.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//   This module implements lingustic feature configuration
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Schema
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Htk;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Represents linguistic feature definition.
    /// </summary>
    public class LinguisticFeatureInfo
    {
        /// <summary>
        /// Gets or sets name of this linguistic feature.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this feature uses extended property.
        /// </summary>
        public bool ExtendedProperty { get; set; }

        /// <summary>
        /// Gets or sets the custom question category associate with this feature.
        /// Null or empty if no custom question used.
        /// </summary>
        public string QuestionCategory { get; set; }

        /// <summary>
        /// Gets or sets the linguistic feature type.
        /// </summary>
        public LingFeatureValueType ValueType { get; set; }

        /// <summary>
        /// Gets or sets the minimum value.
        /// </summary>
        public int MinValue { get; set; }

        /// <summary>
        /// Gets or sets the maximum value.
        /// </summary>
        public int MaxValue { get; set; }

        /// <summary>
        /// Gets or sets the question mode.
        /// </summary>
        public QuestionMode QuestionMode { get; set; }

        /// <summary>
        /// Gets or sets the name of the group to which this feature belongs.
        /// </summary>
        public string Group { get; set; }
    }

    /// <summary>
    /// The class to extract linguistic features.
    /// </summary>
    public class LinguisticFeatureConfig
    {
        #region Fields

        /// <summary>
        /// The schema.
        /// </summary>
        private static XmlSchema _schema;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="LinguisticFeatureConfig"/> class.
        /// </summary>
        public LinguisticFeatureConfig()
        {
            LingFeaList = new Collection<LinguisticFeatureInfo>();
            FeatureGroups = new Dictionary<string, Collection<LinguisticFeatureInfo>>();
        }

        #region Properties

        /// <summary>
        /// Gets configuration schema.
        /// </summary>
        public static XmlSchema Schema
        {
            get
            {
                if (_schema == null)
                {
                    Assembly assembly = Assembly.GetExecutingAssembly();
                    _schema = XmlHelper.LoadSchemaFromResource(assembly,
                        "Microsoft.Tts.Offline.Schema.LinguisticFeatureConfig.xsd");
                }

                return _schema;
            }
        }

        /// <summary>
        /// Gets or sets the TargetFeatureName.
        /// </summary>
        public string TargetFeatureName { get; set; }

        /// <summary>
        /// Gets or sets the TargetFeatureValue.
        /// </summary>
        public string TargetFeatureValue { get; set; }

        /// <summary>
        /// Gets linguistic feature list.
        /// </summary>
        public Collection<LinguisticFeatureInfo> LingFeaList
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the grouped feature list.
        /// </summary>
        public Dictionary<string, Collection<LinguisticFeatureInfo>> FeatureGroups
        {
            get;
            private set;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Load linguistic feature file from assembly resource.
        /// </summary>
        /// <param name="assembly">Assembly contains the linguistic feature configuration file.</param>
        /// <param name="resourcePath">Resource relative path.</param>
        public void Load(Assembly assembly, string resourcePath)
        {
            Helper.ThrowIfNull(assembly);
            Helper.ThrowIfNull(resourcePath);

            XmlDocument dom = new XmlDocument();
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(dom.NameTable);
            nsmgr.AddNamespace("tts", Schema.TargetNamespace);
            Stream stream = assembly.GetManifestResourceStream(resourcePath);
            if (stream == null)
            {
                const string Message = "Linguistic feature list can not be loaded from internal resources, "
                    + "Please offer the path of linguistic feature configuration file";
                throw new InvalidDataException(Message);
            }

            StreamReader resourceStream = new StreamReader(stream);
            XmlHelper.Validate(resourceStream.BaseStream, Schema);

            dom.Load(resourceStream);
            ParseFeatures(dom, nsmgr);
        }

        /// <summary>
        /// Loads the config file.
        /// </summary>
        /// <param name="filePath">
        /// Config file path.
        /// </param>
        public void Load(string filePath)
        {
            Helper.ThrowIfFileNotExist(filePath);
            XmlDocument dom = new XmlDocument();
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(dom.NameTable);
            nsmgr.AddNamespace("tts", Schema.TargetNamespace);

            if (!File.Exists(filePath))
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "The linguistic feature configuration file \"{0}\" not found",
                    filePath);
                throw new InvalidDataException(message);
            }

            try
            {
                XmlHelper.Validate(filePath, Schema);
            }
            catch (InvalidDataException ide)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Error found in the configuration file \"{0}\"",
                    filePath);
                throw new InvalidDataException(message, ide);
            }

            // Loads configuration.
            dom.Load(filePath);
            ParseFeatures(dom, nsmgr);
        }

        /// <summary>
        /// Parse features elements in XML file.
        /// </summary>
        /// <param name="dom">Xml document.</param>
        /// <param name="nsmgr">Xml namespace manager.</param>
        private void ParseFeatures(XmlDocument dom, XmlNamespaceManager nsmgr)
        {
            TargetFeatureName = dom.DocumentElement.GetAttribute("targetFeatureName");
            TargetFeatureValue = dom.DocumentElement.GetAttribute("targetFeatureValue");

            LingFeaList.Clear();
            FeatureGroups.Clear();
            foreach (XmlElement ele in dom.DocumentElement.SelectNodes(@"tts:feature", nsmgr))
            {
                if (bool.Parse(ele.GetAttribute("extract")))
                {
                    LinguisticFeatureInfo linguisticFeatureInfo = ParseFeatureInfo(ele);

                    if (LingFeaList.Any(item => item.Name == linguisticFeatureInfo.Name))
                    {
                        throw new InvalidDataException(
                            string.Format(CultureInfo.InvariantCulture,
                                "Duplicated feature: {0}",
                                linguisticFeatureInfo.Name));
                    }

                    LingFeaList.Add(linguisticFeatureInfo);
                }
            }

            foreach (XmlElement group in dom.DocumentElement.SelectNodes(@"tts:featureGroup", nsmgr))
            {
                string groupName = group.GetAttribute("name");
                if (FeatureGroups.ContainsKey(groupName))
                {
                    throw new InvalidDataException(
                               string.Format(CultureInfo.InvariantCulture,
                                   "Duplicated feature group: {0}",
                                   groupName));
                }

                Collection<LinguisticFeatureInfo> groupFeatures = new Collection<LinguisticFeatureInfo>();
                foreach (XmlElement ele in group.SelectNodes(@"tts:feature", nsmgr))
                {
                    if (bool.Parse(ele.GetAttribute("extract")))
                    {
                        LinguisticFeatureInfo linguisticFeatureInfo = ParseFeatureInfo(ele);

                        if (LingFeaList.Any(item => item.Name == linguisticFeatureInfo.Name))
                        {
                            throw new InvalidDataException(
                                string.Format(CultureInfo.InvariantCulture,
                                    "Duplicated feature: {0}",
                                    linguisticFeatureInfo.Name));
                        }

                        linguisticFeatureInfo.Group = groupName;

                        LingFeaList.Add(linguisticFeatureInfo);
                        groupFeatures.Add(linguisticFeatureInfo);
                    }
                }

                FeatureGroups.Add(groupName, groupFeatures);
            }
        }

        /// <summary>
        /// Parses LinguisticFeatureInfo from xml element.
        /// </summary>
        /// <param name="ele">The xml element.</param>
        /// <returns>LinguisticFeatureInfo object.</returns>
        private LinguisticFeatureInfo ParseFeatureInfo(XmlElement ele)
        {
            LinguisticFeatureInfo linguisticFeatureInfo = new LinguisticFeatureInfo
            {
                Name = ele.Attributes["name"].Value,
                ExtendedProperty = ele.Attributes["extendedProperty"] == null ?
                    false :
                    bool.Parse(ele.Attributes["extendedProperty"].Value),
                QuestionCategory = ele.Attributes["category"] == null ?
                    null :
                    ele.Attributes["category"].Value,
                ValueType = ele.Attributes["valueType"] == null ?
                    LingFeatureValueType.Null :
                (LingFeatureValueType)Enum.Parse(typeof(LingFeatureValueType), ele.Attributes["valueType"].Value),
                MinValue = ele.Attributes["minValue"] == null ?
                    -1 :
                    int.Parse(ele.Attributes["minValue"].Value),
                MaxValue = ele.Attributes["maxValue"] == null ?
                    -1 :
                    int.Parse(ele.Attributes["maxValue"].Value),
                QuestionMode = ele.Attributes["questionMode"] == null ?
                    QuestionMode.All :
                    (QuestionMode)Enum.Parse(typeof(QuestionMode), ele.Attributes["questionMode"].Value)
            };
            return linguisticFeatureInfo;
        }

        #endregion
    }
}