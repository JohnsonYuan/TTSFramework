//----------------------------------------------------------------------------
// <copyright file="StatusRecorder.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements StatusRecorder for resuming VoiceModelTrainer.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// User Interface class, provides the function for end user to record the finished steps.
    /// </summary>
    public class StatusRecorder
    {
        #region Fields

        /// <summary>
        /// Status recorder schema.
        /// </summary>
        private XmlSchema _schema;

        /// <summary>
        /// The status file path.
        /// </summary>
        private string _statusFilePath;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the StatusRecorder class.
        /// </summary>
        /// <param name="statusFilePath">The status file path to record status information.</param>
        public StatusRecorder(string statusFilePath)
        {
            FinishedSteps = new List<string>();
            _statusFilePath = statusFilePath;
            IndexOfPreviousStep = -1;

            GetCurrentStatus();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the array to record finished steps.
        /// </summary>
        public List<string> FinishedSteps { get; set; }

        /// <summary>
        /// Gets or sets status file path (with externsion .xml).
        /// </summary>
        private string StatusFilePath
        {
            get
            {
                return _statusFilePath;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    _statusFilePath = string.Empty;
                }
                else
                {
                    _statusFilePath = value;
                }
            }
        }

        /// <summary>
        /// Gets status file schema.
        /// </summary>
        private XmlSchema ConfigSchema
        {
            get
            {
                if (_schema == null)
                {
                    Assembly assembly = Assembly.GetExecutingAssembly();
                    _schema = XmlHelper.LoadSchemaFromResource(assembly, "Microsoft.Tts.Offline.Schema.StatusRecorder.xsd");
                }

                return _schema;
            }
        }

        /// <summary>
        /// Gets or sets the index of previous step.
        /// </summary>
        private int IndexOfPreviousStep { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Checks whether the step is successfully finished last time.
        /// </summary>
        /// <param name="stepName">The step name.</param>
        /// <returns>Whether the step is successfully finished last time.</returns>
        public bool CheckStepStatus(string stepName)
        {
            stepName = stepName.Trim();

            if (string.IsNullOrEmpty(stepName))
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "The step name {0} for check is invalid", stepName);
                throw new InvalidDataException(message);
            }

            bool ret = false;
            int indexOfStepName = FinishedSteps.IndexOf(stepName);

            if (indexOfStepName > -1)
            {
                IndexOfPreviousStep = indexOfStepName;
                ret = true;
            }

            return ret;
        }

        /// <summary>
        /// Records current status.
        /// </summary>
        /// <param name="stepName">The status information needs to record.</param>
        public void RecordCurrentStatus(string stepName)
        {
            if (!Helper.IsValidPath(StatusFilePath))
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "The status file path {0} is invalid.", StatusFilePath);
                throw new InvalidDataException(message);
            }

            stepName = stepName.Trim();

            if (string.IsNullOrEmpty(stepName))
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Invalid step name [{0}]", stepName);
                throw new ArgumentException(message);
            }

            if (FinishedSteps.IndexOf(stepName) > -1)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                "The step {0} already exists.", stepName);
                throw new InvalidDataException(message);
            }

            IndexOfPreviousStep++;
            FinishedSteps.Insert(IndexOfPreviousStep, stepName);

            XmlDocument dom = new XmlDocument();
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(dom.NameTable);
            nsmgr.AddNamespace("tts", ConfigSchema.TargetNamespace);
            dom.NameTable.Add(ConfigSchema.TargetNamespace);

            XmlDeclaration declaration = dom.CreateXmlDeclaration("1.0", "utf-8", null);

            dom.AppendChild(declaration);

            XmlElement ele = dom.CreateElement("FinishedJob", ConfigSchema.TargetNamespace);

            dom.AppendChild(ele);

            foreach (string step in FinishedSteps)
            {
                XmlElement stepNameEle = dom.CreateElement("step", ConfigSchema.TargetNamespace);

                stepNameEle.SetAttribute("name", step);
                ele.AppendChild(stepNameEle);
            }

            dom.AppendChild(ele);

            lock (StatusFilePath)
            {
                dom.Save(StatusFilePath);
            }

            // Performance compatability format checking.
            XmlHelper.Validate(StatusFilePath, ConfigSchema);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Parses the status file.
        /// </summary>
        private void GetCurrentStatus()
        {
            // Check the status file first.
            try
            {
                if (!Helper.IsValidPath(StatusFilePath))
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "The status file path [{0}] is invalid.", StatusFilePath);
                    throw new InvalidDataException(message);
                }

                if (Helper.FileValidExists(StatusFilePath))
                {
                    XmlHelper.Validate(StatusFilePath, ConfigSchema);

                    // Load status file.
                    XmlDocument dom = new XmlDocument();
                    XmlNamespaceManager nsmgr = new XmlNamespaceManager(dom.NameTable);
                    nsmgr.AddNamespace("tts", ConfigSchema.TargetNamespace);
                    dom.Load(StatusFilePath);

                    // Test whether the namespace of the configuration file is designed.
                    if (string.Compare(dom.DocumentElement.NamespaceURI,
                        ConfigSchema.TargetNamespace, StringComparison.OrdinalIgnoreCase) != 0)
                    {
                        string str1 = "The StatusRecorder xml file [{0}] must use the schema namespace [{1}]. ";
                        string str2 = "Currently the StatusRecorder file uses namespace [{2}]";
                        string message = string.Format(CultureInfo.InvariantCulture,
                            str1 + str2,
                            StatusFilePath, ConfigSchema.TargetNamespace, dom.DocumentElement.NamespaceURI);
                        throw new InvalidDataException(message);
                    }

                    ParseFinishedSteps(dom, nsmgr);
                }
            }
            catch (Exception exp)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Parsing the current status of the StatusRecorder file [{0}] failed. {1} {2}",
                    StatusFilePath, System.Environment.NewLine, exp.Message);
                throw new InvalidDataException(message, exp);
            }
        }

        /// <summary>
        /// Parse the status file to get finished modules and steps.
        /// </summary>
        /// <param name="dom">StatusRecorder XML document.</param>
        /// <param name="nsmgr">XML namespace.</param>
        private void ParseFinishedSteps(XmlDocument dom, XmlNamespaceManager nsmgr)
        {
            Helper.ThrowIfNull(dom);
            Helper.ThrowIfNull(nsmgr);

            foreach (XmlNode moduleNode in dom.DocumentElement.SelectNodes(@"tts:step", nsmgr))
            {
                string moduleNodeName = string.Empty;

                XmlElement moduleNodeEle = moduleNode as XmlElement;
                moduleNodeName = moduleNodeEle.GetAttribute("name");
                FinishedSteps.Add(moduleNodeName);
            }
        }

        #endregion
    }
}