//----------------------------------------------------------------------------
// <copyright file="Configuration.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements Tts model trainer configuration.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.FlowEngine
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Class to hold all configuration exception.
    /// </summary>
    [Serializable]
    public class ConfigurationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the ConfigurationException class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public ConfigurationException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ConfigurationException class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public ConfigurationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Class to process the reference in configuration.
    /// </summary>
    public class ConfigurationReference
    {
        #region Fields

        /// <summary>
        /// The character indicating whether the string is reference or not.
        /// </summary>
        private const char ReferenceCharacter = '$';

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the ConfigurationReference class.
        /// </summary>
        public ConfigurationReference()
        {
        }

        /// <summary>
        /// Initializes a new instance of the ConfigurationReference class.
        /// </summary>
        /// <param name="module">The referenced module.</param>
        /// <param name="name">The referenced name.</param>
        public ConfigurationReference(string module, string name)
        {
            Module = module;
            Name = name;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the referenced module.
        /// </summary>
        public string Module { get; set; }

        /// <summary>
        /// Gets or sets the referenced name.
        /// </summary>
        public string Name { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Checks the input string is a reference or not.
        /// </summary>
        /// <param name="value">The given string to check.</param>
        /// <returns>True indicates the given string is a reference, otherwise indicates a constant value.</returns>
        public static bool IsReference(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                return value[0] == ReferenceCharacter;
            }

            return false;
        }

        /// <summary>
        /// Parses the reference string.
        /// </summary>
        /// <param name="value">The given string to parse.</param>
        public void Parse(string value)
        {
            if (!IsReference(value))
            {
                throw new ArgumentException(Helper.NeutralFormat("\"{0}\" is not a reference", value));
            }

            // string before the last '.' will be treated as module name.
            // This is to enable "xx.xx.xxx" nameing style to differencitate multiple include.
            int index = value.LastIndexOf('.');
            if (index <= 0 || index == value.Length)
            {
                throw new ArgumentException(Helper.NeutralFormat("\"{0}\" is not a valid reference", value));
            }

            // Since there first charactor is '$'.
            Module = value.Substring(1, index - 1);
            Name = value.Substring(index + 1);
        }

        /// <summary>
        /// Gets the string to present this object.
        /// </summary>
        /// <returns>A string to present this object.</returns>
        public override string ToString()
        {
            return ReferenceCharacter + Module + '.' + Name;
        }

        #endregion
    }

    /// <summary>
    /// Class to process the module element in configuration file.
    /// </summary>
    public abstract class ConfigurationItemBase
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the ConfigurationItemBase class.
        /// </summary>
        protected ConfigurationItemBase()
        {
            Inputs = new Dictionary<string, ConfigurationInput>();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the all inputs in this configuration item.
        /// </summary>
        public Dictionary<string, ConfigurationInput> Inputs { get; protected set; }

        #endregion

        #region Abstract Methods

        /// <summary>
        /// Loads the configuration element from a given XmlElement.
        /// </summary>
        /// <param name="element">The given XmlElement to load.</param>
        public abstract void Load(XmlElement element);

        /// <summary>
        /// Converts the configuration element to XmlElement.
        /// </summary>
        /// <param name="doc">The given XmlDocument who will be the document of returned XmlElement.</param>
        /// <param name="schema">The given XmlSchema.</param>
        /// <returns>The XmlElement holds this element.</returns>
        public abstract XmlElement ToElement(XmlDocument doc, XmlSchema schema);

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets input value in this configuration item. If the given input is not exist a new input will be added.
        /// </summary>
        /// <param name="name">The given input name.</param>
        /// <param name="value">The value want to set to the given input.</param>
        /// <param name="isCdataSection">The value to indicate whether the value is a CDATA section.</param>
        public void SetInputValue(string name, string value, bool isCdataSection)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            if (Inputs.ContainsKey(name))
            {
                Inputs[name].Value = value;
                Inputs[name].IsCdataSection = isCdataSection;
            }
            else
            {
                ConfigurationInput input = new ConfigurationInput
                {
                    Name = name,
                    Value = value,
                    IsCdataSection = isCdataSection,
                };

                Inputs.Add(input.Name, input);
            }
        }

        /// <summary>
        /// Tests whether this configuration item contains the given input.
        /// </summary>
        /// <param name="name">The given input name.</param>
        /// <returns>Bool.</returns>
        public bool ContainsInput(string name)
        {
            return Inputs.ContainsKey(name);
        }

        /// <summary>
        /// Gets input value in this configuration item. If the given input is not exist a new input will be added.
        /// </summary>
        /// <param name="name">The given input name.</param>
        /// <returns>The value of the given input.</returns>
        public string GetInputValue(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            if (!ContainsInput(name))
            {
                throw new InvalidDataException(Helper.NeutralFormat("No input \"{0}\" found", name));
            }

            return Inputs[name].Value;
        }

        /// <summary>
        /// Removes the given input in this configuration item.
        /// </summary>
        /// <param name="name">The given input name.</param>
        public void RemoveInput(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            if (!ContainsInput(name))
            {
                throw new InvalidDataException(Helper.NeutralFormat("No input \"{0}\" found", name));
            }

            Inputs.Remove(name);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Loads the inputs sub-element of this element.
        /// </summary>
        /// <param name="element">The given XmlElement to load input sub-element.</param>
        protected void LoadInputs(XmlElement element)
        {
            Inputs.Clear();
            foreach (XmlNode n in element.ChildNodes)
            {
                XmlElement e = n as XmlElement;
                if (e != null && e.LocalName == ConfigurationInput.InputElementName)
                {
                    ConfigurationInput input = new ConfigurationInput();
                    input.Load(e);
                    if (Inputs.ContainsKey(input.Name))
                    {
                        throw new ConfigurationException(
                            Helper.NeutralFormat("Input \"{0}\" exists already with value is \"{1}\"", input.Name,
                                Inputs[input.Name].Value));
                    }

                    Inputs.Add(input.Name, input);
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Class to load/save configuration.
    /// </summary>
    public class Configuration
    {
        #region Fields

        /// <summary>
        /// Reference configuration prefix.
        /// </summary>
        public const char RefFilePrefix = '#';

        /// <summary>
        /// The root element name of this configuration.
        /// </summary>
        public const string RootElementName = "flow";

        /// <summary>
        /// The schema of this configuration.
        /// </summary>
        private static XmlSchema _schema;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the Configuration class.
        /// </summary>
        public Configuration()
        {
            Items = new List<ConfigurationItemBase>();
        }

        #endregion

        #region Delegates and Events

        /// <summary>
        /// The delegate to resolve the inside configuration file.
        /// </summary>
        /// <param name="sender">The Configuration object to send this event.</param>
        /// <param name="eventArgs">The event argements.</param>
        /// <returns>The stream contains the inside configuration file.</returns>
        public delegate Stream ConfigurationResolver(Configuration sender, ResolveEventArgs eventArgs);

        /// <summary>
        /// The event to resolve the inside configuration file.
        /// </summary>
        public event ConfigurationResolver ConfigurationResolve;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the configuration schema.
        /// </summary>
        public static XmlSchema Schema
        {
            get
            {
                if (_schema == null)
                {
                    Assembly assembly = typeof(Configuration).Assembly;
                    _schema = XmlHelper.LoadSchemaFromResource(assembly, "Microsoft.Tts.Offline.Schema.FlowEngineConfig.xsd");
                }

                return _schema;
            }
        }

        /// <summary>
        /// Gets the configuration items.
        /// </summary>
        public List<ConfigurationItemBase> Items { get; private set; }

        /// <summary>
        /// Gets or sets handler namespace.
        /// </summary>
        public string HandlerNamespace { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Tests whether the given module is in this configuration.
        /// </summary>
        /// <param name="module">The given module name.</param>
        /// <returns>True indicates the module exists. Otherwise false.</returns>
        public bool ContainsModule(string module)
        {
            if (string.IsNullOrEmpty(module))
            {
                throw new ArgumentNullException("module");
            }

            return Items.Count(o => o is ConfigurationModule && ((ConfigurationModule)o).Name.Equals(module)) > 0;
        }

        /// <summary>
        /// Finds the given module in this configuration.
        /// </summary>
        /// <param name="module">The given module name.</param>
        /// <returns>The module want to find.</returns>
        public ConfigurationModule FindModule(string module)
        {
            if (string.IsNullOrEmpty(module))
            {
                throw new ArgumentNullException("module");
            }

            ConfigurationModule item;
            try
            {
                item =
                    Items.Single(o => o is ConfigurationModule && ((ConfigurationModule)o).Name.Equals(module)) as
                        ConfigurationModule;
            }
            catch (InvalidOperationException e)
            {
                // No such module find.
                throw new ConfigurationException(Helper.NeutralFormat("Module \"{0}\" not found", module), e);
            }

            return item;
        }

        /// <summary>
        /// Removes the given module in this configuration.
        /// </summary>
        /// <param name="module">The module.</param>
        public void RemoveModule(string module)
        {
            if (string.IsNullOrEmpty(module))
            {
                throw new ArgumentNullException("module");
            }

            int index = -1;
            for (int i = 0; i < Items.Count; ++i)
            {
                if (Items[i] is ConfigurationModule && ((ConfigurationModule)Items[i]).Name.Equals(module))
                {
                    index = i;
                    break;
                }
            }

            if (index < 0)
            {
                // No such module find.
                throw new ConfigurationException(Helper.NeutralFormat("Module \"{0}\" not found", module));
            }

            Items.RemoveAt(index);
        }

        /// <summary>
        /// Tests whether the given include is in this configuration.
        /// </summary>
        /// <param name="src">The srouce of the include.</param>
        /// <returns>True indicates the include exists. Otherwise false.</returns>
        public bool ContainsInclude(string src)
        {
            if (string.IsNullOrEmpty(src))
            {
                throw new ArgumentNullException("src");
            }

            return Items.Count(o => o is ConfigurationInclude && ((ConfigurationInclude)o).Source.Equals(src)) > 0;
        }

       /// <summary>
        /// Finds the given includes in this configuration.
        /// </summary>
        /// <param name="src">The srouce of the include.</param>
        /// <returns>A Dictionary object to contains the includes, whose key is the index of the include
        /// and value is the include.</returns>
        public Dictionary<int, ConfigurationInclude> FindIncludes(string src)
        {
            if (string.IsNullOrEmpty(src))
            {
                throw new ArgumentNullException("src");
            }

            Dictionary<int, ConfigurationInclude> includes = new Dictionary<int, ConfigurationInclude>();
            for (int i = 0; i < Items.Count; ++i)
            {
                if (Items[i] is ConfigurationInclude && ((ConfigurationInclude)Items[i]).Source.Equals(src))
                {
                    includes.Add(i, Items[i] as ConfigurationInclude);
                }
            }

            if (includes.Count == 0)
            {
                throw new InvalidDataException(Helper.NeutralFormat("No include found which source is \"{0}\"", src));
            }

            return includes;
        }

        /// <summary>
        /// Removes the given item in this configuration.
        /// </summary>
        /// <param name="item">The given configuration item.</param>
        public void RemoveConfigurationItem(ConfigurationItemBase item)
        {
            Items.Remove(item);
        }

        /// <summary>
        /// Inserts all modules from given configuration into this configuration.
        /// </summary>
        /// <param name="config">The given configuration.</param>
        /// <param name="index">The beginning index to insert all modules.</param>
        public void InsertAllModules(Configuration config, int index)
        {
            if (index < 0 || index > Items.Count)
            {
                throw new InvalidDataException(Helper.NeutralFormat("The insertion index \"{0}\" is invalid", index));
            }

            foreach (ConfigurationItemBase item in config.Items)
            {
                if (item is ConfigurationModule)
                {
                    ConfigurationModule module = item as ConfigurationModule;
                    if (ContainsModule(module.Name))
                    {
                        throw new ConfigurationException(
                            Helper.NeutralFormat("Module \"{0}\" exists already", module.Name));
                    }

                    Items.Insert(index, (ConfigurationItemBase)module);
                    ++index;
                }
                else if (item is ConfigurationInclude)
                {
                    ConfigurationInclude include = item as ConfigurationInclude;
                    foreach (ConfigurationModule module in include.ParsesInclude(ConfigurationResolve).Values)
                    {
                        if (ContainsModule(module.Name))
                        {
                            throw new ConfigurationException(
                                Helper.NeutralFormat("Module \"{0}\" exists already", module.Name));
                        }

                        if (include.Skip)
                        {
                            // If include Skip attribute is set as "true", then sub module will all be skipped.
                            module.Skip = include.Skip;
                        }

                        Items.Insert(index, (ConfigurationItemBase)module);
                        ++index;
                    }
                }
                else
                {
                    throw new ConfigurationException(Helper.NeutralFormat("Unknown item \"{0}\"", item.ToString()));
                }
            }
        }

        /// <summary>
        /// Add configuration element to the configuration.
        /// </summary>
        /// <param name="inputElementName"> The input element name. .</param>
        /// <param name="elementAttribute"> The input element attribute. .</param>
        public void AddConfigElement(string inputElementName, string elementAttribute)
        {
            try
            {
                if (string.IsNullOrEmpty(inputElementName))
                {
                    throw new ArgumentNullException(Helper.NeutralFormat("The input inputElementName {0} is invalid.", inputElementName));
                }

                if (!string.IsNullOrEmpty(elementAttribute))
                {
                    foreach (ConfigurationItemBase item in Items)
                    {
                        if (item is ConfigurationInclude)
                        {
                            ConfigurationInput addedElement = new ConfigurationInput
                            {
                                Name = inputElementName,
                                Value = elementAttribute,
                                IsCdataSection = false,
                            };

                            ((ConfigurationInclude)item).Inputs.Add(inputElementName, addedElement);
                        }
                    }
                }
            }
            catch (Exception exp)
            {
                throw new ConfigurationException(Helper.NeutralFormat("Add configuration element to the config file failed. Exception message: {0}.", Helper.BuildExceptionMessage(exp)));
            }
        }

        /// <summary>
        /// Skip all modules in this configuration.
        /// </summary>
        /// <param name="skip">The value indicating whether to skip.</param>
        public void SkipAllModules(bool skip)
        {
            foreach (ConfigurationItemBase item in Items)
            {
                if (item is ConfigurationModule)
                {
                    ConfigurationModule module = item as ConfigurationModule;
                    module.Skip = skip;
                }
                else if (item is ConfigurationInclude)
                {
                    ConfigurationInclude include = item as ConfigurationInclude;
                    foreach (ConfigurationModule module in include.ParsesInclude(ConfigurationResolve).Values)
                    {
                        module.Skip = skip;
                    }
                }
                else
                {
                    throw new ConfigurationException(Helper.NeutralFormat("Unknown item \"{0}\"", item.ToString()));
                }
            }
        }

        /// <summary>
        /// Gets the all modules in this configuration file in order.
        /// </summary>
        /// <returns>The all modules in this configuration file.</returns>
        public Dictionary<string, ConfigurationModule> GetAllModules()
        {
            Dictionary<string, ConfigurationModule> modules =
                new Dictionary<string, ConfigurationModule>();
            foreach (ConfigurationItemBase item in Items)
            {
                if (item is ConfigurationModule)
                {
                    ConfigurationModule module = item as ConfigurationModule;
                    if (modules.ContainsKey(module.Name))
                    {
                        throw new ConfigurationException(
                            Helper.NeutralFormat("Module \"{0}\" exists already with type is \"{1}\"", module.Name,
                                modules[module.Name].FullType));
                    }

                    modules.Add(module.Name, module);
                }
                else if (item is ConfigurationInclude)
                {
                    ConfigurationInclude include = item as ConfigurationInclude;
                    foreach (ConfigurationModule module in include.ParsesInclude(ConfigurationResolve).Values)
                    {
                        if (modules.ContainsKey(module.Name))
                        {
                            throw new ConfigurationException(
                                Helper.NeutralFormat("Module \"{0}\" exists already with type is \"{1}\"", module.Name,
                                    modules[module.Name].FullType));
                        }

                        if (include.Skip)
                        {
                            // If include Skip attribute is set as "true", then sub module will all be skipped.
                            module.Skip = include.Skip;
                        }

                        modules.Add(module.Name, module);
                    }
                }
                else
                {
                    throw new ConfigurationException(Helper.NeutralFormat("Unknown item \"{0}\"", item.ToString()));
                }
            }

            return modules;
        }

        /// <summary>
        /// Loads the configuration from the given file name/URL.
        /// </summary>
        /// <remarks>If the input configFile begins with character '#', it means the file is an internal resource.</remarks>
        /// <param name="configFile">The given file name/URL to read.</param>
        public void Load(string configFile)
        {
            if (string.IsNullOrEmpty(configFile))
            {
                throw new ArgumentNullException("configFile");
            }

            if (configFile[0] == RefFilePrefix)
            {
                configFile = configFile.Substring(1);
                if (ConfigurationResolve == null)
                {
                    throw new ConfigurationException(
                        Helper.NeutralFormat("No resolver provider for inside configuration name \"{0}\"", configFile));
                }

                Stream stream = ConfigurationResolve(this, new ResolveEventArgs(configFile));

                if (stream == null)
                {
                    throw new ConfigurationException(
                        Helper.NeutralFormat("Cannot resolve inside configuration file \"{0}\"", configFile));
                }

                if (!stream.CanRead)
                {
                    throw new ConfigurationException(
                        Helper.NeutralFormat("Cannot read the resolved configuration file \"{0}\"", configFile));
                }

                Load(stream);
            }
            else
            {
                if (!File.Exists(configFile))
                {
                    throw new ConfigurationException(Helper.NeutralFormat("Cannot find the configuration file \"{0}\"",
                        configFile));
                }

                try
                {
                    XmlHelper.Validate(configFile, Schema);
                }
                catch (InvalidDataException ide)
                {
                    throw new ConfigurationException(
                        Helper.NeutralFormat("Error occurs in configuration file \"{0}\"", configFile), ide);
                }

                XmlDocument doc = new XmlDocument();
                doc.Load(configFile);

                Load(doc);
            }
        }

        /// <summary>
        /// Loads the configuration from the given stream.
        /// </summary>
        /// <param name="stream">The given stream to read.</param>
        public void Load(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            try
            {
                XmlHelper.Validate(stream, Schema);
            }
            catch (InvalidDataException ide)
            {
                throw new ConfigurationException(Helper.NeutralFormat("Error occured in configuration stream", stream), ide);
            }

            XmlDocument doc = new XmlDocument();
            doc.Load(stream);

            Load(doc);
        }

        /// <summary>
        /// Saves the configuration to the given file name/URL.
        /// </summary>
        /// <param name="configFile">The given file name/URL to write.</param>
        public void Save(string configFile)
        {
            if (string.IsNullOrEmpty(configFile))
            {
                throw new ArgumentNullException("configFile");
            }

            Helper.EnsureFolderExistForFile(configFile);
            ToDocument().Save(configFile);
        }

        /// <summary>
        /// Saves the configuration to the given stream.
        /// </summary>
        /// <param name="stream">The given stream to write.</param>
        public void Save(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            ToDocument().Save(stream);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Loads configuration from the XmlDocument.
        /// </summary>
        /// <param name="doc">The given XmlDocument.</param>
        private void Load(XmlDocument doc)
        {
            XmlElement e = doc.DocumentElement;
            if (e == null)
            {
                throw new ConfigurationException("No document element found");
            }

            if (e.Name != RootElementName)
            {
                throw new ConfigurationException(Helper.NeutralFormat("\"{0}\" element expected but \"{1}\" inputed",
                    RootElementName, e.Name));
            }

            XmlNode attrNode = e.Attributes["handlerNamespace"];
            if (attrNode != null)
            {
                HandlerNamespace = attrNode.Value;
            }

            Items.Clear();
            foreach (XmlNode subNode in e.ChildNodes)
            {
                XmlElement subElement = subNode as XmlElement;
                if (subElement == null)
                {
                    continue;
                }

                ConfigurationItemBase item;
                switch (subElement.Name)
                {
                    case ConfigurationModule.ElementName:
                        item = new ConfigurationModule()
                        {
                            Namespace = HandlerNamespace,
                        };

                        break;
                    case ConfigurationInclude.ElementName:
                        item = new ConfigurationInclude();
                        break;
                    default:
                        throw new ConfigurationException(
                            Helper.NeutralFormat("Unsupported \"{0}\" element found",
                                subElement.Name));
                }

                item.Load(subElement);
                Items.Add(item);
            }
        }

        /// <summary>
        /// Converts the configuration to XmlDocument.
        /// </summary>
        /// <returns>The converted XmlDocument.</returns>
        private XmlDocument ToDocument()
        {
            XmlDocument doc = new XmlDocument();
            doc.NameTable.Add(Schema.TargetNamespace);

            XmlElement root = doc.CreateElement(RootElementName, Schema.TargetNamespace);
            if (!string.IsNullOrEmpty(HandlerNamespace))
            {
                root.SetAttribute("handlerNamespace", HandlerNamespace);
            }

            doc.AppendChild(root);
            doc.InsertBefore(doc.CreateXmlDeclaration("1.0", "utf-8", null), root);

            foreach (ConfigurationItemBase item in Items)
            {
                root.AppendChild(item.ToElement(doc, Schema));
            }

            return doc;
        }

        #endregion
    }
}