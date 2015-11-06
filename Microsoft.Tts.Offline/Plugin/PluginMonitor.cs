//----------------------------------------------------------------------------
// <copyright file="PluginMonitor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements light weight plug-in monitor
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Plugin
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// Plug-in monitor.
    /// </summary>
    public class PluginMonitor
    {
        #region Fields

        private Dictionary<Type, PluginAttribute> _registeredPlugins =
            new Dictionary<Type, PluginAttribute>();

        private string _folderPath = string.Empty;

        #endregion

        /// <summary>
        /// Gets registered plugins.
        /// </summary>
        public Dictionary<Type, PluginAttribute> RegisteredPlugins
        {
            get { return _registeredPlugins; }
        }

        #region Methods

        /// <summary>
        /// Look up plug-ins in a folder.
        /// </summary>
        /// <param name="type">Interface type.</param>
        /// <param name="pluginFolderPath">Plug-in folder path.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability",
            "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Reflection.Assembly.LoadFile", Justification = "Ignore.")]
        public void FindPlugins(Type type, string pluginFolderPath)
        {
            if (string.IsNullOrEmpty(pluginFolderPath))
            {
                throw new ArgumentNullException("pluginFolderPath");
            }

            if (!Directory.Exists(pluginFolderPath))
            {
                throw new ArgumentException("Plugin folder path not exists");
            }

            if (!pluginFolderPath.Equals(_folderPath))
            {
                _folderPath = pluginFolderPath;
                DirectoryInfo dir = new DirectoryInfo(pluginFolderPath);
                foreach (FileInfo dllFile in dir.GetFiles("*.dll"))
                {
                    Assembly assembly = Assembly.LoadFile(dllFile.FullName);
                    foreach (Type pluginType in assembly.GetTypes())
                    {
                        if (pluginType.IsSubclassOf(type) || pluginType.GetInterface(type.Name) != null)
                        {
                            PluginAttribute[] attributes = (PluginAttribute[])
                                pluginType.GetCustomAttributes(typeof(PluginAttribute), false);
                            if (attributes.Length != 1)
                            {
                                throw new ArgumentException("Plug-in attribute not found");
                            }

                            if (!_registeredPlugins.ContainsKey(pluginType))
                            {
                                _registeredPlugins.Add(pluginType, attributes[0]);
                            }
                        }
                    }
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Plug-in attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class PluginAttribute : Attribute
    {
        #region Fields

        private static readonly string DefaultVersion = "0.0.0.0";
        private readonly string _name;
        private string _versionTag = DefaultVersion;
        private Version _version = new Version(DefaultVersion);
        private string _publisher = string.Empty;
        private string _description = string.Empty;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginAttribute"/> class.
        /// </summary>
        /// <param name="name">Plug-in name.</param>
        public PluginAttribute(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            _name = name;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets plug-in name.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Gets or sets version.
        /// </summary>
        public string VersionTag
        {
            get
            {
                return _versionTag;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _version = new Version(value);
                _versionTag = value;
            }
        }

        /// <summary>
        /// Gets version.
        /// </summary>
        public Version Version
        {
            get { return _version; }
        }

        /// <summary>
        /// Gets or sets publisher.
        /// </summary>
        public string Publisher
        {
            get
            {
                return _publisher;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _publisher = value;
            }
        }

        #endregion
    }
}