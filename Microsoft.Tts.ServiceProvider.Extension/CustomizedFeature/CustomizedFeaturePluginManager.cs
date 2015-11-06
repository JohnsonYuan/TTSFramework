//----------------------------------------------------------------------------
// <copyright file="CustomizedFeaturePluginManager.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements customized feature extraction configuration
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.ServiceProvider.Extension
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Represents a Plugin definition.
    /// </summary>
    public class PluginInfo
    {
        /// <summary>
        /// Gets or sets the assembly path of this PluginInfo.
        /// </summary>
        public string AssemblyPath { get; set; }

        /// <summary>
        /// Gets or sets the class name of this PluginInfo.
        /// </summary>
        public string ClassName { get; set; }

        /// <summary>
        /// Gets or sets the configuration string of this PluginInfo.
        /// </summary>
        public string Configuration { get; set; }
    }

    /// <summary>
    /// Utility to get an IUtteranceExtender implemention.
    /// </summary>
    public class UtteranceExtenderFinder
    {
        /// <summary>
        /// Get the IUtteranceExtender implemention specified by the assembly and class.
        /// </summary>
        /// <param name="assemblyPath">Path of the assembly containing IUtteranceExtender implementation.</param>
        /// <param name="className">Class implements IUtteranceExtender.</param>
        /// <returns>The IUtteranceExtender implemention.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability",
            "CA2001:AvoidCallingProblematicMethods", Justification = "Ignore.")]
        public static IUtteranceExtender Find(string assemblyPath, string className)
        {
            if (!Path.IsPathRooted(assemblyPath))
            {
                string pluginDir = Path.GetDirectoryName(typeof(UtteranceExtenderFinder).Module.FullyQualifiedName);
                assemblyPath = Path.Combine(pluginDir, assemblyPath);
            }

            if (!File.Exists(assemblyPath))
            {
                throw new FileNotFoundException(
                    string.Format(CultureInfo.InvariantCulture, "plugin dll not found: {0}", assemblyPath));
            }

            Assembly assembly = Assembly.LoadFrom(assemblyPath);
            Type type = assembly.GetType(className);

            if (type == null)
            {
                throw new InvalidDataException(
                    string.Format(CultureInfo.InvariantCulture, "plugin class {0} not found in {1}", className, assemblyPath));
            }

            return (IUtteranceExtender)Activator.CreateInstance(type);
        }

        /// <summary>
        /// Load utterance extender plugins specifies in plugin infos.
        /// </summary>
        /// <param name="pluginInfos">Plug in infos.</param>
        /// <returns>List of utterance extenders.</returns>
        public static List<IUtteranceExtender> LoadUtteranceExtenders(List<PluginInfo> pluginInfos)
        {
            if (pluginInfos == null)
            {
                throw new ArgumentNullException("pluginInfos");
            }

            List<IUtteranceExtender> utteranceExtenders = new List<IUtteranceExtender>();

            foreach (PluginInfo pluginInfo in pluginInfos)
            {
                IUtteranceExtender extender = UtteranceExtenderFinder.Find(
                    pluginInfo.AssemblyPath, pluginInfo.ClassName);
                extender.Initialize(pluginInfo.Configuration);
                utteranceExtenders.Add(extender);
            }

            return utteranceExtenders;
        }
    }

    /// <summary>
    /// CustomizedFeaturePluginManager.
    /// </summary>
    public class CustomizedFeaturePluginManager
    {
        #region Public consts

        /// <summary>
        /// Attach before feature extraction.
        /// </summary>
        public const string AttachBeforeExtraction = "beforeFeatureExtraction";

        #endregion

        #region Fields

        /// <summary>
        /// Plugin group indexed by attach point.
        /// </summary>
        private Dictionary<string, List<PluginInfo>> _plugins =
            new Dictionary<string, List<PluginInfo>>();

        #endregion

        #region Public static properties

        /// <summary>
        /// Gets configuration schema.
        /// </summary>
        public static XmlSchemaInclude SchemaInclude
        {
            get
            {
                XmlSchemaInclude included = new XmlSchemaInclude();
                included.Schema =
                    XmlHelper.LoadSchemaFromResource("Microsoft.Tts.Offline.Schema.CustomizedFeatureManager.xsd");

                return included;
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Plugin group indexed by attach point.
        /// </summary>
        public Dictionary<string, List<PluginInfo>> Plugins
        {
            get { return _plugins; }
            set { _plugins = value; }
        }

        #endregion

        #region Public static members

        /// <summary>
        /// Parse config snippet and create the manager.
        /// </summary>
        /// <param name="nsmgr">Namespace manager.</param>
        /// <param name="configNode">Xml node containing the config.</param>
        /// <returns>The config object.</returns>
        public static CustomizedFeaturePluginManager ParseConfig(XmlNamespaceManager nsmgr, XmlNode configNode)
        {
            CustomizedFeaturePluginManager manager = null;
            if (nsmgr != null && configNode != null)
            {
                manager = new CustomizedFeaturePluginManager();
                manager.ParsePluginSettings(nsmgr, configNode);
            }

            return manager;
        }

        #endregion

        #region Public members

        /// <summary>
        /// Load utterance extenders.
        /// </summary>
        /// <returns>Loaded utterance extenders.</returns>
        public Dictionary<string, Collection<IUtteranceExtender>> LoadUtteranceExtenders()
        {
            Helper.ThrowIfNull(Plugins);
            Dictionary<string, Collection<IUtteranceExtender>> utteranceExtendersDict =
                new Dictionary<string, Collection<IUtteranceExtender>>();

            foreach (string attachPoint in Plugins.Keys)
            {
                List<IUtteranceExtender> extenders = UtteranceExtenderFinder.LoadUtteranceExtenders(
                    Plugins[attachPoint]);
                utteranceExtendersDict.Add(attachPoint, new Collection<IUtteranceExtender>());
                Helper.AppendCollection<IUtteranceExtender>(utteranceExtendersDict[attachPoint],
                    extenders);
            }

            return utteranceExtendersDict;
        }

        /// <summary>
        /// Get plugin list for the attach point.
        /// </summary>
        /// <param name="attachPoint">The attach point for the requested plug in.</param>
        /// <returns>
        /// Plugin list for given attchPoint, or null if not available.
        /// </returns>
        public List<PluginInfo> GetPlugins(string attachPoint)
        {
            List<PluginInfo> ret = null;

            if (_plugins.ContainsKey(attachPoint))
            {
                ret = _plugins[attachPoint];
            }

            return ret;
        }

        /// <summary>
        /// Load configuration from xml snippet.
        /// </summary>
        /// <param name="nsmgr">Namespace manager.</param>
        /// <param name="configNode">Xml node containing the config.</param>
        public void ParsePluginSettings(XmlNamespaceManager nsmgr, XmlNode configNode)
        {
            // argument checking
            if (nsmgr == null)
            {
                throw new ArgumentNullException("nsmgr");
            }

            if (configNode == null)
            {
                throw new ArgumentNullException("configNode");
            }

            if (configNode.Name != "customizedFeaturePlugin")
            {
                throw new InvalidDataException("customizedFeaturePlugin element expected!");
            }

            foreach (XmlNode pluginGroupNode in configNode.ChildNodes)
            {
                string attachPoint = pluginGroupNode.Attributes["attachPoint"].Value;

                if (_plugins.ContainsKey(attachPoint))
                {
                    throw new InvalidDataException(string.Format(CultureInfo.InvariantCulture, "duplicated attachPoint: {0}", attachPoint));
                }

                _plugins[attachPoint] = new List<PluginInfo>();

                foreach (XmlNode pluginNode in pluginGroupNode.ChildNodes)
                {
                    PluginInfo pluginInfo = new PluginInfo
                    {
                        AssemblyPath = pluginNode.Attributes["assembly"].Value,
                        ClassName = pluginNode.Attributes["class"].Value,
                        Configuration = pluginNode.Attributes["configuration"] == null ?
                                        string.Empty : pluginNode.Attributes["configuration"].Value
                    };

                    _plugins[attachPoint].Add(pluginInfo);
                }
            }
        }

        /// <summary>
        /// Save configuration file as a xml snippet.
        /// </summary>
        /// <param name="dom">Xml document to be saved into.</param>
        /// <param name="parent">Xml parent element to be saved into.</param>
        /// <param name="schema">Xml schema.</param>
        public void SavePluginSettings(XmlDocument dom, XmlElement parent, XmlSchema schema)
        {
            // argument checking
            if (dom == null)
            {
                throw new ArgumentNullException("dom");
            }

            if (parent == null)
            {
                throw new ArgumentNullException("parent");
            }

            if (schema == null)
            {
                throw new ArgumentNullException("schema");
            }

            if (_plugins != null)
            {
                // root element of this snippet
                XmlElement root = dom.CreateElement("customizedFeaturePlugin", schema.TargetNamespace);
                parent.AppendChild(root);

                foreach (KeyValuePair<string, List<PluginInfo>> pair in _plugins)
                {
                    XmlElement listRoot = XmlHelper.AppendElement(dom, root,
                        "pluginGroup", "attachPoint", pair.Key, schema);

                    foreach (PluginInfo plugin in pair.Value)
                    {
                        XmlElement pluginElem = XmlHelper.AppendElement(dom, listRoot,
                            "plugin", "assembly", plugin.AssemblyPath, schema);
                        pluginElem.SetAttribute("class", plugin.ClassName);
                        pluginElem.SetAttribute("configuration", plugin.Configuration);
                    }
                }
            }
        }

        #endregion
    }
}