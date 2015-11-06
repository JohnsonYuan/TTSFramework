//----------------------------------------------------------------------------
// <copyright file="FlowEngine.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module defines the Engine class and some of helper
//   classes used to assemble the pipeline of training.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.FlowEngine
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// A helper class to get the information of FlowHandler.
    /// </summary>
    public class HandlerTypeInfo
    {
        #region Fields

        /// <summary>
        /// The all loaded assemblies.
        /// </summary>
        private static Assembly[] _loadedAssemblies;

        /// <summary>
        /// The all handler type info in those loaded assemblies.
        /// </summary>
        private static Dictionary<string, HandlerTypeInfo> _allHandlerTypeInfos;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the HandlerTypeInfo class.
        /// </summary>
        /// <param name="type">The given type of the class.</param>
        public HandlerTypeInfo(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            if (!type.IsSubclassOf(typeof(FlowHandler)))
            {
                throw new ArgumentException(
                    Helper.NeutralFormat("Type \"{0}\" is not a subclass of type FlowHandler", type.Name));
            }

            Type = type;
            Inputs = new Dictionary<string, PropertyInfo>();
            Parsers = new Dictionary<string, MethodInfo>();
            Outputs = new Dictionary<string, PropertyInfo>();
            PropertyInfo[] pis = Type.GetProperties();

            foreach (PropertyInfo pi in pis)
            {
                if (pi.Name.StartsWith("In") && (pi.Name.Length > 2 && char.IsUpper(pi.Name[2])))
                {
                    Inputs.Add(pi.Name.Substring(2), pi);
                }

                if (pi.Name.StartsWith("Out") && (pi.Name.Length > 3 && char.IsUpper(pi.Name[3])))
                {
                    Outputs.Add(pi.Name.Substring(3), pi);
                }
            }

            MethodInfo[] mis = Type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (MethodInfo mi in mis)
            {
                ParameterInfo[] infos = mi.GetParameters();
                if (infos.Length != 1 || infos[0].ParameterType != typeof(string))
                {
                    continue;
                }

                if (mi.Name.StartsWith("Parse") && (mi.Name.Length > 5 && char.IsUpper(mi.Name[5])))
                {
                    Parsers.Add(mi.Name.Substring(5), mi);
                }
            }
        }

        #endregion

        #region Properties
        
        /// <summary>
        /// Gets the all handler type info.
        /// </summary>
        public static Dictionary<string, HandlerTypeInfo> AllHandlerTypeInfos
        {
            get
            {
                if (_loadedAssemblies == null ||
                    AppDomain.CurrentDomain.GetAssemblies().Length != _loadedAssemblies.Length)
                {
                    GetAllHandlerTypeInfo();
                }

                return _allHandlerTypeInfos;
            }
        }

        /// <summary>
        /// Gets the type name of the FlowHandler.
        /// </summary>
        public string FullName
        {
            get
            {
                return Type.FullName;
            }
        }

        /// <summary>
        /// Gets the type of the FlowHandler.
        /// </summary>
        public Type Type { get; private set; }

        /// <summary>
        /// Gets the all inputs of the FlowHandler.
        /// </summary>
        public Dictionary<string, PropertyInfo> Inputs { get; private set; }

        /// <summary>
        /// Gets the all parsers handler of the FlowHandler.
        /// </summary>
        public Dictionary<string, MethodInfo> Parsers { get; private set; }

        /// <summary>
        /// Gets the all outputs of the FlowHandler.
        /// </summary>
        public Dictionary<string, PropertyInfo> Outputs { get; private set; }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the information of all types which are extended from the FlowHandler class.
        /// </summary>
        private static void GetAllHandlerTypeInfo()
        {
            _allHandlerTypeInfos = new Dictionary<string, HandlerTypeInfo>();
            _loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in _loadedAssemblies)
            {
                Type[] types = assembly.GetTypes();
                foreach (Type type in types)
                {
                    if (type.IsSubclassOf(typeof(FlowHandler)))
                    {
                        HandlerTypeInfo info = new HandlerTypeInfo(type);
                        if (_allHandlerTypeInfos.ContainsKey(info.FullName))
                        {
                            throw new InvalidDataException(Helper.NeutralFormat(
                                "Duplicate handler [{0}] in [{1}] and [{2}] assembly",
                                info.FullName, info.Type.Assembly.Location,
                                assembly.Location));
                        }

                        _allHandlerTypeInfos.Add(info.FullName, info);
                    }
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// A class to store the all information of a single engine item.
    /// </summary>
    public class FlowItem
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the FlowItem class.
        /// </summary>
        /// <param name="config">The given configuration module item.</param>
        public FlowItem(ConfigurationModule config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            Config = config;
            if (!HandlerTypeInfo.AllHandlerTypeInfos.ContainsKey(Config.FullType))
            {
                throw new ConfigurationException(
                    Helper.NeutralFormat("Module type \"{0}\" not found, one of the below expected - {1}",
                        Config.FullType, string.Join(",", HandlerTypeInfo.AllHandlerTypeInfos.Keys.ToArray())));
            }

            TypeInfo = HandlerTypeInfo.AllHandlerTypeInfos[Config.FullType];

            // Creates the corresponding handler.
            Handler = Activator.CreateInstance(TypeInfo.Type, Config.Name) as FlowHandler;
            if (Handler == null)
            {
                throw new ConfigurationException(
                    Helper.NeutralFormat("Module type \"{0}\" is not an instance of the FlowHandler class.", Config.FullType));
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the configuratuion module item.
        /// </summary>
        public ConfigurationModule Config { get; private set; }

        /// <summary>
        /// Gets the handler object.
        /// </summary>
        public FlowHandler Handler { get; private set; }

        /// <summary>
        /// Gets the handler type info.
        /// </summary>
        public HandlerTypeInfo TypeInfo { get; private set; }

        /// <summary>
        /// Gets the name of this item.
        /// </summary>
        public string Name
        {
            get
            {
                return Config.Name;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initializes the logger of this engine item.
        /// </summary>
        /// <param name="detailLogger">The logger which will contains more detailed log.</param>
        /// <param name="compactLogger">The logger which will contains only the important log.</param>
        public void InitializeLogger(ILogger detailLogger, ILogger compactLogger)
        {
            Handler.Logger = detailLogger;
            Handler.Observer += compactLogger.Update;
        }

        /// <summary>
        /// Initializes the inputs of this engine item.
        /// </summary>
        /// <param name="items">The previous engine items.</param>
        public void InitializeInputs(Dictionary<string, FlowItem> items)
        {
            foreach (ConfigurationInput input in Config.Inputs.Values)
            {
                ValidateInput(input);

                // Sets the value.
                if (input.IsCdataSection)
                {
                    // CDATA is always constant value.
                    ParseCdata(input.Name, input.Value);
                }
                else
                {
                    if (!ConfigurationReference.IsReference(input.Value))
                    {
                        // Sets the constant value.
                        SetValue(input.Name, input.Value);
                    }
                    else
                    {
                        // Parse the reference and test whether it is assignable.
                        ConfigurationReference reference = new ConfigurationReference();
                        reference.Parse(input.Value);
                        if (!items.ContainsKey(reference.Module))
                        {
                            throw new ConfigurationException(
                                Helper.NeutralFormat("Module \"{0}\" not found, one of below expected - {1}",
                                    reference.Module, string.Join(",", items.Keys.ToArray())));
                        }

                        items[reference.Module].ValidateOutput(reference.Name);

                        // Tests assignability.
                        Type inputType = TypeInfo.Inputs[input.Name].PropertyType;
                        Type outputType = items[reference.Module].TypeInfo.Outputs[reference.Name].PropertyType;
                        if (!inputType.IsAssignableFrom(outputType))
                        {
                            throw new ConfigurationException(
                                Helper.NeutralFormat(
                                    "Type mismatched between input \"{0}\"({1}) of module \"{2}\" and output \"{3}\"({4}) of module \"{5}\"",
                                    input.Name, inputType.Name, Name, reference.Name, outputType.Name, reference.Module));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Updates the inputs to setup the default parameters transmission.
        /// If there is no anything found, the default value will be used.
        /// </summary>
        /// <param name="items">The previous engine items.</param>
        public void UpdateImplictInputs(Dictionary<string, FlowItem> items)
        {
            foreach (KeyValuePair<string, PropertyInfo> pair in TypeInfo.Inputs)
            {
                string name = pair.Key;
                PropertyInfo pi = pair.Value;

                // If there is no explict definition, needs to find the implict transmission.
                if (!Config.Inputs.ContainsKey(name))
                {
                    // Finds the last one which has the same name and is assignable.
                    foreach (FlowItem item in items.Values.Reverse())
                    {
                        if (item.TypeInfo.Outputs.ContainsKey(name) &&
                            pi.PropertyType.IsAssignableFrom(item.TypeInfo.Outputs[name].PropertyType))
                        {
                            // Adds a input item.
                            ConfigurationInput input = new ConfigurationInput
                            {
                                Name = name,
                                Value = new ConfigurationReference(item.Name, name).ToString(),
                                IsCdataSection = false,
                            };

                            Config.Inputs.Add(input.Name, input);
                            break;
                        }
                    }
                }
            }

            foreach (KeyValuePair<string, PropertyInfo> pair in TypeInfo.Inputs)
            {
                string name = pair.Key;
                PropertyInfo pi = pair.Value;

                if (!Config.Inputs.ContainsKey(name))
                {
                    // Gets the default value of this instance.
                    object obj = pi.GetValue(Handler, null);

                    ConfigurationInput input = new ConfigurationInput
                    {
                        Name = name,
                        Value = (obj == null) ? string.Empty : obj.ToString(),
                        IsCdataSection = false,
                    };

                    Config.Inputs.Add(input.Name, input);
                }
            }

            foreach (KeyValuePair<string, MethodInfo> pair in TypeInfo.Parsers)
            {
                string name = pair.Key;

                if (!Config.Inputs.ContainsKey(name))
                {
                    ConfigurationInput input = new ConfigurationInput
                    {
                        Name = name,
                        Value = string.Empty,
                        IsCdataSection = true,
                    };

                    Config.Inputs.Add(input.Name, input);
                }
            }
        }

        /// <summary>
        /// Sets the value to the given instance.
        /// </summary>
        /// <param name="name">The property's name of the given instance will be set the value to.</param>
        /// <param name="value">The value will be set to the instance.</param>
        public void SetValue(string name, string value)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            if (!TypeInfo.Inputs.ContainsKey(name))
            {
                throw new ConfigurationException(
                    Helper.NeutralFormat("Input property \"{0}\" not found in module \"{1}\"", name, Name));
            }

            PropertyInfo pi = TypeInfo.Inputs[name];

            // Sets the value as null if it's null or empty.
            if (string.IsNullOrEmpty(value))
            {
                pi.SetValue(Handler, null, null);
            }
            else if (pi.PropertyType == typeof(string))
            {
                pi.SetValue(Handler, value, null);
            }
            else if (pi.PropertyType.IsEnum)
            {
                if (pi.PropertyType.Equals(typeof(Language)))
                {
                    pi.SetValue(Handler, Localor.StringToLanguage(value), null);
                }
                else
                {
                    pi.SetValue(Handler, Enum.Parse(pi.PropertyType, value, true), null);
                }
            }
            else
            {
                MethodInfo mi = pi.PropertyType.GetMethod("Parse", new[] { typeof(string) });
                if (mi == null)
                {
                    throw new ConfigurationException(
                        Helper.NeutralFormat("\"Parse\" function not found - cannot convert string to type \"{0}\"",
                            pi.PropertyType.Name));
                }

                pi.SetValue(Handler, mi.Invoke(null, new object[] { value }), null);
            }
        }

        /// <summary>
        /// Sets the value to the given instance from another instance.
        /// </summary>
        /// <param name="inputName">The property's name of the given instance will be set the value to.</param>
        /// <param name="item">The instance which will be get the value from.</param>
        /// <param name="outputName">The property's name of the given instatnce will be get the value from.</param>
        public void SetValue(string inputName, FlowItem item, string outputName)
        {
            if (string.IsNullOrEmpty(inputName))
            {
                throw new ArgumentNullException("inputName");
            }

            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (string.IsNullOrEmpty(outputName))
            {
                throw new ArgumentNullException("outputName");
            }

            if (!TypeInfo.Inputs.ContainsKey(inputName))
            {
                throw new ArgumentException(Helper.NeutralFormat("Input property \"{0}\" not found in model \"{1}\"",
                    inputName, Name));
            }

            if (!item.TypeInfo.Outputs.ContainsKey(outputName))
            {
                throw new ArgumentException(Helper.NeutralFormat("Input property \"{0}\" not found in model \"{1}\"",
                    outputName, item.Name));
            }

            TypeInfo.Inputs[inputName].SetValue(Handler, item.TypeInfo.Outputs[outputName].GetValue(item.Handler, null),
                null);
        }

        /// <summary>
        /// Parses CDATA section in input.
        /// </summary>
        /// <param name="name">The method's name of the given instance will be used to parse the CDATA.</param>
        /// <param name="cdata">The CDATA will be parsed.</param>
        public void ParseCdata(string name, string cdata)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            if (!TypeInfo.Parsers.ContainsKey(name))
            {
                throw new ConfigurationException(
                    Helper.NeutralFormat(
                        "Input \"{0}\" is defined to parse a CDATA element, but not found in module \"{1}\"", name, Name));
            }

            if (!string.IsNullOrEmpty(cdata))
            {
                TypeInfo.Parsers[name].Invoke(Handler, new object[] { cdata });
            }
        }

        /// <summary>
        /// Validates the name of the input from configuration.
        /// </summary>
        /// <param name="input">The configuration input.</param>
        private void ValidateInput(ConfigurationInput input)
        {
            // All name of input should be constant value.
            if (ConfigurationReference.IsReference(input.Name))
            {
                throw new ConfigurationException(
                    Helper.NeutralFormat(
                        "Module \"{0}\" has a reference input name \"{1}\", which is not allowed here", Name, input.Name));
            }

            if (input.IsCdataSection)
            {
                // The input should be found in type info parsers.
                if (!TypeInfo.Parsers.ContainsKey(input.Name))
                {
                    throw new ConfigurationException(
                        Helper.NeutralFormat("Parser name \"{0}\" not found in module \"{1}\", one of below expected - {2}",
                            input.Name, Name, string.Join(",", TypeInfo.Parsers.Keys.ToArray())));
                }
            }
            else
            {
                // The input should be found in the type info inputs.
                if (!TypeInfo.Inputs.ContainsKey(input.Name))
                {
                    throw new ConfigurationException(
                        Helper.NeutralFormat("Input name \"{0}\" not found in module \"{1}\", one of below expected - {2}",
                            input.Name, Name, string.Join(",", TypeInfo.Inputs.Keys.ToArray())));
                }
            }
        }

        /// <summary>
        /// Validates the name of the output from configuration.
        /// </summary>
        /// <param name="name">The name of the output.</param>
        private void ValidateOutput(string name)
        {
            // All name of input should be constant value.
            if (ConfigurationReference.IsReference(name))
            {
                throw new ArgumentException("No reference output \"{0}\" allowed here", name);
            }

            // The input should be found in the type info.
            if (!TypeInfo.Outputs.ContainsKey(name))
            {
                throw new ConfigurationException(
                    Helper.NeutralFormat("Output name \"{0}\" not found in module \"{1}\", one of below expected - {2}",
                        name, Name, string.Join(",", TypeInfo.Outputs.Keys.ToArray())));
            }
        }

        #endregion
    }

    /// <summary>
    /// A class works as an engine to run the inputed pipeline.
    /// </summary>
    public class FlowEngine
    {
        #region Fields

        /// <summary>
        /// The pipeline setup according to the configuration items.
        /// </summary>
        private readonly Dictionary<string, FlowItem> _items;

        /// <summary>
        /// The logger to log the all dumped information.
        /// </summary>
        private readonly ILogger _detailLogger;

        /// <summary>
        /// The logger to log the important information.
        /// </summary>
        private readonly ILogger _compactLogger;

        private readonly string _engineName = "Flow engine";

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the FlowEngine class.
        /// </summary>
        /// <param name="configItems"> The given all configuration items. .</param>
        /// <param name="logFile"> The given file name to store the log. .</param>
        public FlowEngine(Dictionary<string, ConfigurationModule> configItems, string logFile)
            : this(configItems, logFile, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the FlowEngine class.
        /// </summary>
        /// <param name="configItems"> The given all configuration items. .</param>
        /// <param name="logFile"> The given file name to store the log. .</param>
        /// <param name="statusRecorder"> The status recorder for the engine. .</param>
        public FlowEngine(Dictionary<string, ConfigurationModule> configItems, string logFile, StatusRecorder statusRecorder)
        {
            if (configItems == null)
            {
                throw new ArgumentNullException("configItems");
            }

            if (string.IsNullOrEmpty(logFile))
            {
                throw new ArgumentNullException("logFile");
            }

            // Initializes the loggers.
            Helper.TestWritable(logFile);
            _detailLogger = new TextLogger(logFile, Encoding.Unicode);
            _compactLogger = new ConsoleLogger();

            if (statusRecorder != null)
            {
                SkipFinishedModules(statusRecorder, configItems);
            }

            // Creates the object to place the items.
            _items = new Dictionary<string, FlowItem>();

            try
            {
                // Setups the pipeline.
                foreach (ConfigurationModule config in configItems.Values)
                {
                    // Creates the item.
                    FlowItem item = new FlowItem(config);
                    item.Handler.StepStatusRecorder = statusRecorder;

                    // Initializes the inputs.
                    item.InitializeInputs(_items);

                    // Updates the implicit inputs.
                    item.UpdateImplictInputs(_items);

                    // Initializes the loggers.
                    item.InitializeLogger(_detailLogger, _compactLogger);

                    // Adds the item into the pipeline.
                    _items.Add(item.Name, item);
                }
            }
            catch (ConfigurationException e)
            {
                Log("Failed to setup the pipeline because of exception - {0}{1}.", Environment.NewLine,
                    Helper.BuildExceptionMessage(e));
                throw;
            }

            Log("All configuration loaded and pipeline is ready.");
            _detailLogger.LogLine("The configuration file can be re-written as:");
            _detailLogger.LogLine("{0}{1}", ToString(), Environment.NewLine);
        }

        /// <summary>
        /// Initializes a new instance of the FlowEngine class.
        /// </summary>
        /// <param name="engineName">Engine name.</param>
        /// <param name="configItems">The given all configuration items.</param>
        /// <param name="logFile">The given file name to store the log.</param>
        public FlowEngine(string engineName, Dictionary<string, ConfigurationModule> configItems, string logFile)
            : this(configItems, logFile)
        {
            _engineName = engineName;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Get a string to present the configuration.
        /// </summary>
        /// <returns>The string to present the configuration of this object.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "Ignore.")]
        public override sealed string ToString()
        {
            Configuration config = new Configuration();
            foreach (FlowItem item in _items.Values)
            {
                config.Items.Add(item.Config);
            }

            using (MemoryStream stream = new MemoryStream())
            {
                config.Save(stream);
                stream.Seek(0, SeekOrigin.Begin);
                using (StreamReader sr = new StreamReader(stream))
                {
                    return sr.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// Execute the pipeline.
        /// </summary>
        public void Execute()
        {
            DateTime startTime = DateTime.Now;
            Log("{0} started at time {1}.", _engineName, startTime.ToString());

            foreach (FlowItem item in _items.Values)
            {
                try
                {
                    // Check reference input.
                    foreach (ConfigurationInput input in item.Config.Inputs.Values)
                    {
                        if (ConfigurationReference.IsReference(input.Value))
                        {
                            ConfigurationReference reference = new ConfigurationReference();
                            reference.Parse(input.Value);

                            if (!_items.ContainsKey(reference.Module))
                            {
                                throw new ConfigurationException(
                                    Helper.NeutralFormat("Module \"{0}\" not found, one of below expected - {1}",
                                        reference.Module, string.Join(",", _items.Keys.ToArray())));
                            }

                            // Finds the reference model and assign its value to this object.
                            item.SetValue(input.Name, _items[reference.Module], reference.Name);
                        }
                    }

                    // Give the handler a chance to obtain parameters in runtime.
                    item.Handler.PrepareRuntimeInputs();

                    // Use this collection to avoid changing the directory while enumerate it.
                    Collection<string> runtimeOutputKeys = new Collection<string>();
                    item.Handler.RuntimeOutputs.Keys.ForEach(k => runtimeOutputKeys.Add(k));

                    // Fill runtime inputs value
                    foreach (string referencName in runtimeOutputKeys)
                    {
                        ConfigurationReference reference = new ConfigurationReference();
                        reference.Parse(referencName);
                        if (!_items.ContainsKey(reference.Module))
                        {
                            throw new ConfigurationException(
                                Helper.NeutralFormat("Module \"{0}\" not found, one of below expected - {1}",
                                    reference.Module, string.Join(",", _items.Keys.ToArray())));
                        }

                        if (!_items[reference.Module].TypeInfo.Outputs.ContainsKey(reference.Name))
                        {
                            throw new InvalidDataException(Helper.NeutralFormat(
                                "Can't find output property [{0}] in module [{1}]",
                                reference.Name, reference.Module));
                        }

                        // Finds the reference model and assign its value to this object.
                        item.Handler.RuntimeOutputs[referencName] =
                            _items[reference.Module].TypeInfo.Outputs[reference.Name].GetValue(
                            _items[reference.Module].Handler, null);
                    }

                    // Executes the single item.
                    item.Handler.Execute(!item.Config.Skip, item.Config.KeepIntermediateData);
                }
                catch (Exception e)
                {
                    string log = Helper.NeutralFormat("Exception thrown - {0}!{1}{3} failed when module \"{2}\" was running.{1}",
                        Helper.BuildExceptionMessage(e),
                        Environment.NewLine, item.Name, _engineName);
                    Log(log);
                    throw;
                }
            }

            foreach (FlowItem item in _items.Values)
            {
                if (item.Handler is IDisposable)
                {
                    ((IDisposable)item.Handler).Dispose();
                }

                if (!string.IsNullOrEmpty(item.Handler.WarningFilePath) && File.Exists(item.Handler.WarningFilePath))
                {
                    int warningCount = Helper.FileLines(item.Handler.WarningFilePath, true).Count();
                    if (warningCount > 0)
                    {
                        Log("There are [{0}] lines of warnings of handler \"{1}\", warning file path: {2}",
                            warningCount, item.Handler.Name, item.Handler.WarningFilePath);
                    }
                }
            }

            DateTime endTime = DateTime.Now;
            Log("{0} finished at time {1}. Totally elapsed time [{2}].", _engineName, endTime.ToString(),
                (endTime - startTime).ToString());
        }

        /// <summary>
        /// Finds the engine items according to the function.
        /// </summary>
        /// <param name="func">The function for the given engine item.</param>
        /// <returns>The engine items whose meet the function.</returns>
        public IEnumerable<FlowItem> FindItem(Func<FlowItem, bool> func)
        {
            if (func == null)
            {
                throw new ArgumentNullException("func");
            }

            int count = _items.Count(o => func(o.Value));

            // No such module find.
            if (count < 1)
            {
                throw new InvalidDataException("FlowEngine item not found");
            }

            return _items.Select(o => o.Value).Where(func);
        }

        /// <summary>
        /// Finds the engine item according to the name.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <returns>The engine item.</returns>
        public FlowItem FindItem(string name)
        {
            FlowItem item = null;
            if (_items.ContainsKey(name))
            {
                item = _items[name];
            }

            return item;
        }

        /// <summary>
        /// Writes the log into log file and Console.
        /// </summary>
        /// <param name="format">Format string.</param>
        /// <param name="args">Arguments for format string.</param>
        private void Log(string format, params object[] args)
        {
            _detailLogger.LogLine(format, args);
            _compactLogger.LogLine(format, args);
        }

        /// <summary>
        /// Process the status information.
        /// </summary>
        /// <param name="statusRecorder"> The status recorder with status information. .</param>
        /// <param name="configItems"> The given all configuration items. .</param>
        private void SkipFinishedModules(StatusRecorder statusRecorder, Dictionary<string, ConfigurationModule> configItems)
        {
            foreach (ConfigurationModule configItem in configItems.Values)
            {
                if (statusRecorder.FinishedSteps.IndexOf(configItem.Name) > -1)
                {
                    configItem.Skip = true;
                }
            }
        }

        #endregion
    }
}