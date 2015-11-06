//----------------------------------------------------------------------------
// <copyright file="FeatureDocument.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements FeatureDocument
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Schema
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Feature test case label class.
    /// </summary>
    public class FeatureTestCaseLabel
    {
        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="FeatureTestCaseLabel" /> class.
        /// </summary>
        public FeatureTestCaseLabel()
        {
            Features = new Dictionary<string, string>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FeatureTestCaseLabel" /> class.
        /// </summary>
        /// <param name="lab">FeatureTestCaseLabel.</param>
        public FeatureTestCaseLabel(FeatureTestCaseLabel lab)
        {
            Features = new Dictionary<string, string>();
            PredictFeatureName = lab.PredictFeatureName;
            PredictFeatureValue = lab.PredictFeatureValue;
            ExpectedFeatureValue = lab.ExpectedFeatureValue;
            DefaultFeatureValue = lab.DefaultFeatureValue;
            Probability = lab.Probability;
            Confidence = lab.Confidence;
            Source = lab.Source;

            if (lab.Features.Count != 0)
            {
                foreach (string k in lab.Features.Keys)
                {
                    Features.Add(k, lab.Features[k]);
                }
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets text.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets predict feature name.
        /// </summary>
        public string PredictFeatureName { get; set; }

        /// <summary>
        /// Gets or sets predict feature value.
        /// </summary>
        public string PredictFeatureValue { get; set; }

        /// <summary>
        /// Gets or sets expected feature value.
        /// Can be default value.
        /// </summary>
        public string ExpectedFeatureValue { get; set; }

        /// <summary>
        /// Gets or sets default feature.
        /// </summary>
        public string DefaultFeatureValue { get; set; }

        /// <summary>
        /// Gets or sets probability.
        /// </summary>
        public float Probability { get; set; }

        /// <summary>
        /// Gets or sets the source of the prediction is made.
        /// Probably from a prediction model, or combined model.
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Gets or sets the confidence.
        /// </summary>
        public float Confidence { get; set; }

        /// <summary>
        /// Gets or sets the difference.
        /// </summary>
        public float Difference { get; set; }

        /// <summary>
        /// Gets the feature dictionary.
        /// </summary>
        public Dictionary<string, string> Features { get; private set; }

        /// <summary>
        /// Gets or sets the word type.
        /// </summary>
        public WordType WordType { get; set; }

        #endregion

        /// <summary>
        /// Parse test case label element.
        /// </summary>
        /// <param name="labelNode">XML test case label node.</param>
        /// <param name="nsmgr">Xml namespace manager.</param>
        public void Parse(XmlNode labelNode, XmlNamespaceManager nsmgr)
        {
            XmlAttribute attribute = labelNode.Attributes["text"];
            if (attribute != null)
            {
                Text = attribute.Value;
            }

            attribute = labelNode.Attributes["predictFeauteName"];
            if (attribute != null)
            {
                PredictFeatureName = attribute.Value;
            }

            attribute = labelNode.Attributes["predictFeatureValue"];
            if (attribute != null)
            {
                PredictFeatureValue = attribute.Value;
            }

            attribute = labelNode.Attributes["expectedFeatureValue"];
            if (attribute != null)
            {
                ExpectedFeatureValue = attribute.Value;
            }

            attribute = labelNode.Attributes["predictFeatureValue"];
            if (attribute != null)
            {
                PredictFeatureValue = attribute.Value;
            }

            attribute = labelNode.Attributes["probability"];
            if (attribute != null)
            {
                Probability = float.Parse(attribute.Value);
            }

            attribute = labelNode.Attributes["confidence"];
            if (attribute != null)
            {
                Confidence = float.Parse(attribute.Value);
            }

            attribute = labelNode.Attributes["difference"];
            if (attribute != null)
            {
                Difference = float.Parse(attribute.Value);
            }

            attribute = labelNode.Attributes["source"];
            if (attribute != null)
            {
                Source = attribute.Value;
            }

            attribute = labelNode.Attributes["wordType"];
            if (attribute != null)
            {
                WordType = (WordType)Enum.Parse(typeof(WordType), attribute.Value, false);
            }

            XmlNodeList featureNodeList = labelNode.SelectNodes(@"tts:feature", nsmgr);
            foreach (XmlNode featureNode in featureNodeList)
            {
                string name = featureNode.Attributes["name"].Value;
                string value = featureNode.InnerText;
                if (Features.ContainsKey(name))
                {
                    throw new InvalidDataException(Helper.NeutralFormat("Duplicated name [{0}] detected", name));
                }

                Features.Add(name, value);
            }
        }

        /// <summary>
        /// Save test case label.
        /// </summary>
        /// <param name="writer">Writer file to save into.</param>
        /// <param name="nameSpace">Name space.</param>
        public void PerformanceSave(XmlWriter writer, string nameSpace)
        {
            writer.WriteStartElement("label", nameSpace);
            if (!string.IsNullOrEmpty(PredictFeatureName))
            {
                writer.WriteAttributeString("predictFeatureName", PredictFeatureName);
                if (string.IsNullOrEmpty(PredictFeatureValue))
                {
                    throw new InvalidDataException(Helper.NeutralFormat(
                        "predictFeautureName should not be empty when for predictFeatureName [{0}]",
                        PredictFeatureName));
                }
            }

            if (!string.IsNullOrEmpty(Text))
            {
                writer.WriteAttributeString("text", Text);
            }

            if (!string.IsNullOrEmpty(ExpectedFeatureValue))
            {
                writer.WriteAttributeString("expectedFeatureValue", ExpectedFeatureValue);
            }

            if (!string.IsNullOrEmpty(PredictFeatureValue))
            {
                writer.WriteAttributeString("predictFeatureValue", PredictFeatureValue);
            }

            if (Probability > float.Epsilon)
            {
                writer.WriteAttributeString("probability", Probability.ToString());
            }

            if (Confidence > float.Epsilon)
            {
                writer.WriteAttributeString("confidence", Confidence.ToString());
            }

            if (Difference > float.Epsilon)
            {
                writer.WriteAttributeString("difference", Difference.ToString());
            }

            if (!string.IsNullOrEmpty(Source))
            {
                writer.WriteAttributeString("source", Source);
            }

            writer.WriteAttributeString("wordType", WordType.ToString());

            foreach (string name in Features.Keys)
            {
                writer.WriteStartElement("feature", nameSpace);
                writer.WriteAttributeString("name", name);
                writer.WriteValue(Features[name]);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }
    }

    /// <summary>
    /// Feature test case class.
    /// </summary>
    public class FeatureTestCase
    {
        #region Construction

        /// <summary>
        /// Initializes a new instance of the FeatureTestCase class.
        /// </summary>
        public FeatureTestCase()
        {
            Labels = new Collection<FeatureTestCaseLabel>();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets test case Id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets test case text.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets test case labels.
        /// </summary>
        public Collection<FeatureTestCaseLabel> Labels { get; private set; }

        #endregion

        /// <summary>
        /// Parse test case element.
        /// </summary>
        /// <param name="caseNode">XML test case node.</param>
        /// <param name="nsmgr">Xml namespace manager.</param>
        public void Parse(XmlNode caseNode, XmlNamespaceManager nsmgr)
        {
            Helper.ThrowIfNull(caseNode);
            Helper.ThrowIfNull(nsmgr);
            XmlNode textNode = caseNode.SelectSingleNode(@"tts:text", nsmgr);
            Helper.ThrowIfNull(textNode);
            Text = textNode.InnerText;
            Id = caseNode.Attributes["id"].InnerText;

            XmlNodeList labelNodeList = caseNode.SelectNodes(@"tts:labels/tts:label", nsmgr);
            foreach (XmlNode labelNode in labelNodeList)
            {
                FeatureTestCaseLabel label = new FeatureTestCaseLabel();
                label.Parse(labelNode, nsmgr);
                Labels.Add(label);
            }
        }

        /// <summary>
        /// Save test case to target file.
        /// </summary>
        /// <param name="writer">Writer file to save into.</param>
        /// <param name="nameSpace">Name space.</param>
        public void PerformanceSave(XmlWriter writer, string nameSpace)
        {
            writer.WriteStartElement("case", nameSpace);
            writer.WriteAttributeString("id", Id);

            writer.WriteStartElement("text", nameSpace);
            writer.WriteValue(Text);
            writer.WriteEndElement();

            writer.WriteStartElement("labels", nameSpace);
            foreach (FeatureTestCaseLabel label in Labels)
            {
                label.PerformanceSave(writer, nameSpace);
            }

            writer.WriteEndElement();

            writer.WriteEndElement();
        }
    }

    /// <summary>
    /// FeatureDocument class.
    /// </summary>
    public class FeatureDocument : XmlDataFile
    {
        #region Private fields

        private static XmlSchema _schema;

        #endregion
        
        #region Construction

        /// <summary>
        /// Initializes a new instance of the FeatureDocument class.
        /// </summary>
        /// <param name="language">Language of this feature document.</param>
        public FeatureDocument(Language language)
            : base(language)
        {
            TestCases = new Dictionary<string, FeatureTestCase>();
            FeatureValueSet = new SortedList<string, string>();
            Encoding = Encoding.Unicode;
        }

        /// <summary>
        /// Initializes a new instance of the FeatureDocument class.
        /// </summary>
        public FeatureDocument()
            : this(Language.Neutral)
        {
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the feature test cases.
        /// </summary>
        public Dictionary<string, FeatureTestCase> TestCases { get; private set; }

        /// <summary>
        /// Gets the feature value set.
        /// </summary>
        public SortedList<string, string> FeatureValueSet { get; private set; }

        /// <summary>
        /// Configuration schema.
        /// </summary>
        public override XmlSchema Schema
        {
            get
            {
                if (_schema == null)
                {
                    Assembly assembly = Assembly.GetExecutingAssembly();
                    _schema = XmlHelper.LoadSchemaFromResource(assembly,
                        "Microsoft.Tts.Offline.Schema.FeatureDocument.xsd");
                    Helper.ThrowIfNull(_schema);
                }

                return _schema;
            }
        }

        #endregion

        /// <summary>
        /// Build feature value set.
        /// </summary>
        public void BuildFeatureValueSet()
        {
            foreach (FeatureTestCase testCase in TestCases.Values)
            {
                foreach (FeatureTestCaseLabel label in testCase.Labels)
                {
                    if (!string.IsNullOrEmpty(label.ExpectedFeatureValue) && !FeatureValueSet.ContainsKey(label.ExpectedFeatureValue))
                    {
                        FeatureValueSet.Add(label.ExpectedFeatureValue, label.ExpectedFeatureValue);
                    }
                }
            }
        }

        /// <summary>
        /// Load FeatureDocument instance from stream reader.
        /// </summary>
        /// <param name="xmlDoc">Document to load phone set from.</param>
        /// <param name="nsmgr">Xml namespace manager.</param>
        /// <param name="contentController">Content controller.</param>
        protected override void Load(XmlDocument xmlDoc, XmlNamespaceManager nsmgr, object contentController)
        {
            if (xmlDoc == null)
            {
                throw new ArgumentNullException("xmlDoc");
            }

            if (nsmgr == null)
            {
                throw new ArgumentNullException("nsmgr");
            }

            Language language = Localor.StringToLanguage(xmlDoc.DocumentElement.Attributes["lang"].InnerText);
            if (!this.Language.Equals(Language.Neutral) && !language.Equals(this.Language))
            {
                this.ErrorSet.Add(CommonError.NotConsistentLanguage,
                    this.Language.ToString(), "initial one", language.ToString(), "feature document");
            }

            this.Language = language;
            XmlNodeList caseNodeList = xmlDoc.DocumentElement.SelectNodes(@"//tts:cases/tts:case", nsmgr);
            
            int caseIdx = 0;
            string id = string.Empty;
            foreach (XmlNode caseNode in caseNodeList)
            {
                FeatureTestCase testCase = new FeatureTestCase();
                testCase.Parse(caseNode, nsmgr);
                if (string.IsNullOrEmpty(testCase.Id))
                {
                    testCase.Id = string.Format("{0}", caseIdx);
                    caseIdx++;
                }

                TestCases.Add(testCase.Id, testCase);
            }

            BuildFeatureValueSet();
        }

        /// <summary>
        /// Save FeatureDocument to target file.
        /// </summary>
        /// <param name="writer">Writer file to save into.</param>
        /// <param name="contentController">Content controller.</param>
        protected override void PerformanceSave(XmlWriter writer, object contentController)
        {
            writer.WriteStartElement("cases", Schema.TargetNamespace);
            writer.WriteAttributeString("lang", Localor.LanguageToString(Language));

            foreach (FeatureTestCase testCase in TestCases.Values)
            {
                testCase.PerformanceSave(writer, Schema.TargetNamespace);
            }

            writer.WriteEndElement();
        }
    }
}