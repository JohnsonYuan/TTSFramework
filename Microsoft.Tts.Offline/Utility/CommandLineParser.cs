    //----------------------------------------------------------------------------
// <copyright file="CommandLineParser.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements Class to parse input from command line.
//
//     Command Line Parser (CLP)
//
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Utility
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Security.Permissions;
    using System.Text;

    /// <summary>
    /// Attribute type.
    /// </summary>
    public enum InOutType
    {
        /// <summary>
        /// Unknown.
        /// </summary>
        Unknown,

        /// <summary>
        /// Argument for input.
        /// </summary>
        In,

        /// <summary>
        /// Argument for output.
        /// </summary>
        Out,

        /// <summary>
        /// Argument for input or output, not recommend use.
        /// </summary>
        InOut
    }

    /// <summary>
    /// This custom attribute used to add a comment in the usage printing.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class CommentAttribute : Attribute
    {
        #region Fields

        private readonly string _headComment;
        private readonly string _rearComment = "Copyright (C) Microsoft Corporation. All rights reserved.";

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="CommentAttribute"/> class.
        /// </summary>
        /// <param name="headComment">Head comment.</param>
        public CommentAttribute(string headComment)
        {
#pragma warning disable 1691  // avoid the csc warning for "the unknown warning number 56507"
#pragma warning disable 56507 // avoid presharp warning for "flag == null --> string.IsNullOrEmpty(flag)"
            if (headComment == null)
            {
                throw new ArgumentNullException("headComment");
            }

#pragma warning restore 56507
#pragma warning restore 1691

            _headComment = headComment;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommentAttribute"/> class.
        /// </summary>
        /// <param name="headComment">Head comment.</param>
        /// <param name="rearComment">Rear comment.</param>
        public CommentAttribute(string headComment, string rearComment)
        {
#pragma warning disable 1691  // avoid the csc warning for "the unknown warning number 56507"
#pragma warning disable 56507 // avoid presharp warning for "flag == null --> string.IsNullOrEmpty(flag)"
            if (headComment == null)
            {
                throw new ArgumentNullException("headComment");
            }

            if (rearComment == null)
            {
                throw new ArgumentNullException("rearComment");
            }
#pragma warning restore 56507
#pragma warning restore 1691

            _headComment = headComment;
            _rearComment = rearComment;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets Head comment, when printing usage.
        /// </summary>
        public string HeadComment
        {
            get { return _headComment; }
        }

        /// <summary>
        /// Gets Rear comment, when printing usage.
        /// </summary>
        public string RearComment
        {
            get { return _rearComment; }
        }

        #endregion
    }

    /// <summary>
    /// This custom attribute used to mark the which field in a class is a command line input target.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = false)]
    public sealed class ArgumentAttribute : Attribute
    {
        #region Fields

        private readonly string _flag;
        private string _description = string.Empty;
        private string _usagePlaceholder;
        private bool _optional;
        private bool _hidden;
        private InOutType _inoutType;
        private string _requiredModes;
        private string _optionalModes;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="ArgumentAttribute"/> class.
        /// </summary>
        /// <param name="optionName">Flag string for this attribute.</param>
        public ArgumentAttribute(string optionName)
        {
#pragma warning disable 1691  // avoid the csc warning for "the unknown warning number 56507"
#pragma warning disable 56507 // avoid presharp warning for "flag == null --> string.IsNullOrEmpty(flag)"
            if (optionName == null)
            {
                throw new ArgumentNullException("optionName");
            }
#pragma warning restore 56507
#pragma warning restore 1691

            _flag = optionName;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets The parse recognising flag.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Ignore.")]
        public string OptionName
        {
            get { return _flag.ToLower(CultureInfo.InvariantCulture); }
        }

        /// <summary>
        /// Gets or sets Description will display in the PrintUsage method.
        /// </summary>
        public string Description
        {
            get
            {
                return _description;
            }

            set
            {
#pragma warning disable 1691  // avoid the csc warning for "the unknown warning number 56507"
#pragma warning disable 56507 // avoid presharp warning for "flag == null --> string.IsNullOrEmpty(flag)"
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
#pragma warning restore 56507
#pragma warning restore 1691

                _description = value;
            }
        }

        /// <summary>
        /// Gets or sets In the PrintUsage method this will display a place hold for a parameter.
        /// </summary>
        public string UsagePlaceholder
        {
            get
            {
                return _usagePlaceholder;
            }

            set
            {
#pragma warning disable 1691  // avoid the csc warning for "the unknown warning number 56507"
#pragma warning disable 56507 // avoid presharp warning for "flag == null --> string.IsNullOrEmpty(flag)"
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
#pragma warning restore 56507
#pragma warning restore 1691

                _usagePlaceholder = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether (optional = true) means not necessarily in the command-line.
        /// </summary>
        public bool Optional
        {
            get { return _optional; }
            set { _optional = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether (Hidden = true) means this option will not be printed in the command-line.
        /// While one option is set with Hidden, the Optional must be true.
        /// </summary>
        public bool Hidden
        {
            get { return _hidden; }
            set { _hidden = value; }
        }

        /// <summary>
        /// Gets or sets The in/out type of argument.
        /// </summary>
        public InOutType InOutType
        {
            get { return _inoutType; }
            set { _inoutType = value; }
        }

        /// <summary>
        /// Gets or sets The modes require this argument.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Ignore.")]
        public string RequiredModes
        {
            get
            {
                return _requiredModes;
            }

            set
            {
                _requiredModes = value.ToLower(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Gets or sets The modes optionally require this argument.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Ignore.")]
        public string OptionalModes
        {
            get
            {
                return _optionalModes;
            }

            set
            {
                _optionalModes = value.ToLower(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Get required modes in an array.
        /// </summary>
        /// <returns>Mode array.</returns>
        public string[] GetRequiredModeArray()
        {
            string[] modes = null;
            if (!string.IsNullOrEmpty(_requiredModes))
            {
                modes = _requiredModes.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            }

            return modes;
        }

        /// <summary>
        /// Get optional modes in an array.
        /// </summary>
        /// <returns>Mode array.</returns>
        public string[] GetOptionalModeArray()
        {
            string[] modes = null;
            if (!string.IsNullOrEmpty(_optionalModes))
            {
                modes = _optionalModes.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            }

            return modes;
        }

        #endregion
    }

    /// <summary>
    /// When the CommandLineParser meet an unacceptabile command line
    /// Parameter, it will throw the CommandLineParseException. If the
    /// CLP meet another arguments error by anaylse the target object,
    /// It will throw the ArgumentException defined by .NET framework.
    /// </summary>
    [Serializable]
    public class CommandLineParseException : Exception
    {
        #region Constances Fields

        /// <summary>
        /// The error string is "help".
        /// </summary>
        public const string ErrorStringHelp = "help";

        #endregion

        #region Fields

        private readonly string _errorString;

        #endregion

        #region Contructions

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineParseException"/> class.
        /// </summary>
        /// <param name="message">Message.</param>
        /// <param name="error">Error string.</param>
        public CommandLineParseException(string message, string error) : base(message)
        {
            _errorString = string.IsNullOrEmpty(error) ? string.Empty : error;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineParseException"/> class.
        /// </summary>
        public CommandLineParseException() : base()
        {
            _errorString = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineParseException"/> class.
        /// </summary>
        /// <param name="message">Message.</param>
        public CommandLineParseException(string message) : base(message)
        {
            _errorString = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineParseException"/> class.
        /// </summary>
        /// <param name="message">Message.</param>
        /// <param name="inner">Inner exception.</param>
        public CommandLineParseException(string message, Exception inner) : base(message, inner)
        {
            _errorString = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineParseException"/> class.
        /// </summary>
        /// <param name="info">Serialization info.</param>
        /// <param name="context">Streaming context.</param>
        protected CommandLineParseException(SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
            _errorString = string.Empty;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets To save the error string.
        /// </summary>
        public string ErrorString
        {
            get { return _errorString; }
        }

        #endregion

        #region Operations

        /// <summary>
        /// This method is required by serialization.
        /// </summary>
        /// <param name="info">Serialization info.</param>
        /// <param name="context">Streaming context.</param>
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }

        #endregion
    }

    /// <summary>
    /// User Interface class, it only include two static methods for user.
    /// </summary>
    public class CommandLineParser
    {
        #region Construction

        /// <summary>
        /// Prevents a default instance of the <see cref="CommandLineParser"/> class from being created.
        /// Instances of types that define only static members
        /// Do not need to be created. Compilers will automatically
        /// Add a public default constructor if no constructor
        /// Is specified. To prevent this, adding an empty private
        /// Constructor may be required.
        /// </summary>
        private CommandLineParser()
        {
        }

        #endregion

        #region Public static methods

        /// <summary>
        /// Parse the command-line array args, and save the value to the target class.
        /// </summary>
        /// <param name="args">Argument string array.</param>
        /// <param name="target">Target object to associate parsing and save result.</param>
        public static void Parse(string[] args, object target)
        {
            ClpHelper.CheckTarget(target);
            ClpHelper.CheckArgs(args, target);

            InternalFlags internalTarget = new InternalFlags();
            ClpHelper helper = new ClpHelper(target, internalTarget);

            helper.ParseArgs(args);

            if (!string.IsNullOrEmpty(internalTarget.ConfigFile))
            {
                args = ClpHelper.GetStringsFromConfigFile(internalTarget.ConfigFile);
                helper.ParseArgs(args);
            }

            if (internalTarget.NeedHelp)
            {
                throw new CommandLineParseException(string.Empty, "help");
            }

            helper.CheckAllRequiredDestination();
        }

        /// <summary>
        /// Print out the usage to users.
        /// </summary>
        /// <param name="target">Target object to reflect usage information.</param>
        public static void PrintUsage(object target)
        {
            string usage = BuildUsage(target);
            Console.WriteLine();
            Console.WriteLine(usage);
        }

        /// <summary>
        /// Handle exception of commandline parser.
        /// </summary>
        /// <param name="target">Target object to reflect usage information.</param>
        /// <param name="exception">Exception.</param>
        public static void HandleException(object target, Exception exception)
        {
            if (!string.IsNullOrEmpty(exception.Message))
            {
                Console.Error.WriteLine(exception.Message);
            }
            else
            {
                PrintUsage(target);
            }
        }

        /// <summary>
        /// Analyse the target class, and use the information defined in CommentAttribute
        /// And ArgumentAttribute to output the usage to users.
        /// </summary>
        /// <param name="target">Target object to reflect usage information.</param>
        /// <returns>Usage string.</returns>
        public static string BuildUsage(object target)
        {
            ClpHelper.CheckTarget(target);

            CommentAttribute[] ca = (CommentAttribute[])
                target.GetType().GetCustomAttributes(typeof(CommentAttribute), false);

            StringBuilder sb = new StringBuilder();

            if (ca.Length == 1 && !string.IsNullOrEmpty(ca[0].HeadComment))
            {
                sb.AppendLine(ca[0].HeadComment);
            }

            sb.AppendLine();

            Assembly entryAssm = Assembly.GetEntryAssembly();

            // entryAssm is a null reference when a managed assembly has been loaded 
            // from an unmanaged application; Currently we don't allow such calling.
            // But when calling by our Nunit test framework, this value is null
            if (entryAssm != null)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture,
                    "Version {0}", entryAssm.GetName().Version.ToString());
                sb.AppendLine();
            }

            sb.Append(ClpHelper.BuildUsageLine(target));
            sb.Append(ClpHelper.BuildOptionsString(target));

            if (ca.Length == 1 && !string.IsNullOrEmpty(ca[0].RearComment))
            {
                sb.AppendLine();
                sb.AppendLine(ca[0].RearComment);
            }

            return sb.ToString();
        }

        #endregion

        #region Private types

        /// <summary>
        /// Command line parser helper class.
        /// </summary>
        private class ClpHelper
        {
            #region Fields
            public const BindingFlags AllFieldBindingFlags =
                BindingFlags.Instance | BindingFlags.Static |
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly;

            public const string Mode = "mode";

            // const members.
            private const int MaxCommandLineStringNumber = 800;
            private const int MaxConfigFileSize = 32 * 1024; // 32k

            private string _modeString;

            // class members.
            private object _target;
            private InternalFlags _internalTarget;
            private Dictionary<string, Destination> _destMap = new Dictionary<string, Destination>();
            #endregion

            #region Construction

            /// <summary>
            /// Initializes a new instance of the <see cref="ClpHelper"/> class.
            /// </summary>
            /// <param name="target">Target object to reflect usage information.</param>
            /// <param name="internalTarget">Internal flags.</param>
            public ClpHelper(object target, InternalFlags internalTarget)
            {
                _target = target;
                _internalTarget = internalTarget;   // interal flags class, include "-h","-?","-help","-C"

                ParseTheDestination(target);
            }

            #endregion

            #region Public static methods

            /// <summary>
            /// Check the target objcet, which is to save the value, to avoid misuse.
            /// </summary>
            /// <param name="target">Target object to reflect usage information.</param>
            public static void CheckTarget(object target)
            {
                if (target == null)
                {
                    throw new ArgumentNullException("target");
                }

                if (!target.GetType().IsClass)
                {
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                        "Object target is not a class."), "target");
                }

                // Check each field of the target class to ensure that every field, which wanted to be
                // filled, has defined a static TryParse(string, out Value) funtion. In the parsing time,
                // the CLP class will use this static function to parse the string to value.
                foreach (FieldInfo fieldInfo in target.GetType().GetFields(ClpHelper.AllFieldBindingFlags))
                {
                    if (fieldInfo.IsDefined(typeof(ArgumentAttribute), false))
                    {
                        Type type = fieldInfo.FieldType;
                        if (type.IsArray)
                        {
                            type = type.GetElementType();
                        }

                        // string Type don't need a TryParse function, so skip check it.
                        if (type == typeof(string))
                        {
                            continue;
                        }

                        Type reftype = Type.GetType(type.ToString() + "&");
                        if (reftype == null)
                        {
                            throw new ArgumentException(
                                "This Type does not exist in this assembly GetType(" + type + ")failed.",
                                fieldInfo.ToString());
                        }

                        MethodInfo mi = type.GetMethod("TryParse", new Type[] { typeof(string), reftype });
                        if (mi == null)
                        {
                            throw new ArgumentException(
                                "Type " + type + " don't have a TryParse(string, out Value) method.",
                                fieldInfo.ToString());
                        }
                    }
                }
            }

            /// <summary>
            /// Check args from static Main() function, to avoid misuse this library.
            /// </summary>
            /// <param name="args">Argument string array.</param>
            /// <param name="target">Target object to reflect usage information.</param>
            public static void CheckArgs(string[] args, object target)
            {
                if (args == null)
                {
                    string message = string.Format(CultureInfo.InvariantCulture, "Empty parameters.");
                    throw new CommandLineParseException(message);
                }

                int requiredArgumentCount = GetRequiredArgumentCount(target);
                if (args.Length == 0)
                {
                    // if there is no parameter given
                    if (requiredArgumentCount > 0)
                    {
                        // some parameters are required
                        throw new CommandLineParseException(string.Empty, "help");
                    }

                    // run the application with default option values
                }

                if (args.Length > MaxCommandLineStringNumber)
                {
                    throw new CommandLineParseException(string.Format(CultureInfo.InvariantCulture,
                        "Input parameter number is larger than {0}.", MaxCommandLineStringNumber), "args");
                }

                for (int i = 0; i < args.Length; ++i)
                {
                    if (string.IsNullOrEmpty(args[i]))
                    {
                        string message = string.Format(CultureInfo.InvariantCulture,
                            "The {0}(th) parameter in the command line could not be null or empty.",
                            i + 1);
                        throw new CommandLineParseException(message);
                    }
                }
            }

            /// <summary>
            /// Parse the configuration file into a string[], this string[] will be send to the
            /// ParseArgs(string[] args). This function will do some simple check of the
            /// Command line, the first character the config line in the file must '-',
            /// Otherwise, this line will not be parsed.
            /// </summary>
            /// <param name="filePath">Configuration file path.</param>
            /// <returns>Configuration strings.</returns>
            public static string[] GetStringsFromConfigFile(string filePath)
            {
                if (!File.Exists(filePath))
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "The configuration file [{0}] can not found.",
                        filePath);
                    throw new CommandLineParseException(message, filePath);
                }

                FileInfo fileInfo = new FileInfo(filePath);

                if (fileInfo.Length > MaxConfigFileSize)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Not supported configuration file [{0}], for the size of it is bigger than {1} byte.",
                        filePath, MaxConfigFileSize);
                    throw new CommandLineParseException(message, filePath);
                }

                string[] lines;
                using (StreamReader streamFile = new StreamReader(filePath))
                {
                    lines = streamFile.ReadToEnd().Split(Environment.NewLine.ToCharArray());
                }

                List<string> strList = new List<string>();

                // Go through the file, and expand the listed parameters
                // into the List<string> of existing parameters.
                foreach (string line in lines)
                {
                    string trimedLine = line.Trim();

                    if (trimedLine.IndexOf('-') == 0)
                    {
                        string[] strArray = trimedLine.Split(new char[] { ' ', '\t' });
                        foreach (string str in strArray)
                        {
                            if (!string.IsNullOrEmpty(str))
                            {
                                strList.Add(str);
                            }
                        }
                    }
                }

                return strList.ToArray();
            }

            /// <summary>
            /// Count the number of required arguments.
            /// </summary>
            /// <param name="target">Target object to reflect usage information.</param>
            /// <returns>The number of required arguments.</returns>
            public static int GetRequiredArgumentCount(object target)
            {
                int count = 0;
                foreach (FieldInfo field in target.GetType().GetFields(AllFieldBindingFlags))
                {
                    ArgumentAttribute argument = GetFieldArgumentAttribute(field);
                    if (argument == null)
                    {
                        continue;       // skip those field that don't define the ArgumentAttribute.
                    }

                    if (!argument.Optional)
                    {
                        count++;
                    }
                }

                return count;
            }

            /// <summary>
            /// Build the useage line. First print the file name of current execution files.
            /// And then, print the each flag of these options.
            /// </summary>
            /// <param name="target">Target object to reflect usage information.</param>
            /// <returns>Useage string.</returns>
            public static string BuildUsageLine(object target)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat(CultureInfo.InvariantCulture,
                    "Usage:{0}", Environment.NewLine);

                string[] allModes = GetAllModes(target);
                if (allModes != null)
                {
                    foreach (string mode in allModes)
                    {
                        sb.AppendFormat(CultureInfo.InvariantCulture, @"  Mode ""{0}"" has following usage: {1}", mode, Environment.NewLine);
                        sb.AppendFormat(CultureInfo.InvariantCulture,
                            "  {0} -mode {1}", AppDomain.CurrentDomain.FriendlyName, mode);

                        foreach (FieldInfo field in target.GetType().GetFields(AllFieldBindingFlags))
                        {
                            ArgumentAttribute argument = GetFieldArgumentAttribute(field);
                            if (argument == null)
                            {
                                continue;       // skip those field that don't define the ArgumentAttribute.
                            }

                            if (argument.OptionName == ClpHelper.Mode)
                            {
                                continue;
                            }

                            string[] optionalModes = argument.GetOptionalModeArray();
                            string[] requiredModes = argument.GetRequiredModeArray();
                            if (requiredModes == null && optionalModes == null)
                            {
                                // should not print out hidden argument
                                if (!argument.Hidden)
                                {
                                    if (argument.Optional)
                                    {
                                        sb.AppendFormat(CultureInfo.InvariantCulture,
                                            " [{0}]", GetFlagAndPlaceHolderString(argument));
                                    }
                                    else
                                    {
                                        sb.AppendFormat(CultureInfo.InvariantCulture,
                                            " {0}", GetFlagAndPlaceHolderString(argument));
                                    }
                                }
                            }
                            else
                            {
                                if ((optionalModes != null) && IsInArray(optionalModes, mode))
                                {
                                    sb.AppendFormat(CultureInfo.InvariantCulture,
                                        " [{0}]", GetFlagAndPlaceHolderString(argument));
                                }
                                else if (requiredModes != null && IsInArray(requiredModes, mode))
                                {
                                    sb.AppendFormat(CultureInfo.InvariantCulture,
                                        " {0}", GetFlagAndPlaceHolderString(argument));
                                }
                            }
                        }

                        sb.AppendLine(string.Empty);
                        sb.AppendLine(string.Empty);
                    }
                }
                else
                {
                    sb.AppendFormat(CultureInfo.InvariantCulture,
                        "  {0}", AppDomain.CurrentDomain.FriendlyName);

                    foreach (FieldInfo field in target.GetType().GetFields(AllFieldBindingFlags))
                    {
                        ArgumentAttribute argument = GetFieldArgumentAttribute(field);
                        if (argument == null)
                        {
                            continue;       // skip those field that don't define the ArgumentAttribute.
                        }

                        string optionLine = BuildOptionLine(argument);
                        sb.Append(optionLine);
                    }
                }

                sb.AppendLine();
                sb.AppendLine();

                return sb.ToString();
            }

            /// <summary>
            /// Print flag and description of each options.
            /// </summary>
            /// <param name="target">Target object to reflect usage information.</param>
            /// <returns>Flag and description string of each options.</returns>
            public static string BuildOptionsString(object target)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("  Options\tDescriptions");
                sb.Append(BuildOptionsString(target, null));
                return sb.ToString();
            }

            #endregion

            #region Public methods

            /// <summary>
            /// Parse the args string from the static Main() or from configuration file.
            /// </summary>
            /// <param name="args">Argument string array.</param>
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Ignore.")]
            public void ParseArgs(string[] args)
            {
                Destination destination = null;

                foreach (string str in args)
                {
                    Destination dest = IsFlagStringAndGetTheDestination(str);

                    // Is a flag string
                    if (dest != null)
                    {
                        if (destination != null)
                        {
                            destination.Save(_target);
                        }

                        destination = dest;

                        if (destination.AlreadySaved)
                        {
                            string message = string.Format(CultureInfo.InvariantCulture,
                                "The option flag [-{0}] could not be dupalicated.",
                                destination.Argument.OptionName);
                            throw new CommandLineParseException(message, str);
                        }
                    }
                    else
                    {
                        if (destination == null)
                        {
                            destination = SaveValueStringToEmptyFlag(str);
                        }
                        else
                        {
                            if (!destination.TryToAddValue(_target, str))
                            {
                                destination.Save(_target);
                                destination = SaveValueStringToEmptyFlag(str);
                            }
                        }

                        if (destination != null)
                        {
                            if (destination.Argument.OptionName == ClpHelper.Mode)
                            {
                                _modeString = str.ToLower(CultureInfo.InvariantCulture);
                            }
                        }
                    }
                }

                // deal with the last flag
                if (destination != null) 
                {
                    destination.Save(_target);
                }
            }

            /// <summary>
            /// By the end of the command line parsing, we must make sure that all non-optional
            /// Flags have been given by the tool user.
            /// </summary>
            public void CheckAllRequiredDestination()
            {
                string[] allModes = GetAllModes(_target);
                foreach (Destination destination in _destMap.Values)
                {
                    bool requiredMissing = false;
                    if (destination.InternalTarget != null)
                    {
                        continue;
                    }

                    if (allModes != null)
                    {
                        Debug.Assert(_destMap.ContainsKey(ClpHelper.Mode));
                        if (!string.IsNullOrEmpty(_modeString))
                        {
                            string[] requireModes = destination.Argument.GetRequiredModeArray();
                            string[] optionalModes = destination.Argument.GetOptionalModeArray();
                            if (requireModes == null)
                            {
                                if (optionalModes == null)
                                {
                                    // if required modes and optional modes are all empty
                                    // Means the argument is commonly optional or not in all modes.
                                    // we can use the Optional flag to simplify
                                    requiredMissing = !destination.Argument.Optional && !destination.AlreadySaved;
                                }
                            }
                            else
                            {
                                if (IsInArray(requireModes, _modeString))
                                {
                                    requiredMissing = !destination.AlreadySaved;
                                }
                            }

                            if (destination.AlreadySaved && (optionalModes != null || requireModes != null))
                            {
                                if ((requireModes == null && !IsInArray(optionalModes, _modeString)) ||
                                    (optionalModes == null && !IsInArray(requireModes, _modeString)) ||
                                    (requireModes != null && optionalModes != null &&
                                    !IsInArray(optionalModes, _modeString) && !IsInArray(requireModes, _modeString)))
                                {
                                    string message = string.Format(CultureInfo.InvariantCulture,
                                        "Parameter [{0}] is not needed for mode [{1}].",
                                        destination.Argument.OptionName, _modeString);
                                    throw new CommandLineParseException(message);
                                }
                            }
                        }
                        else
                        {
                            string message = string.Format(CultureInfo.InvariantCulture,
                                "The mode option is required for the command.");
                            throw new CommandLineParseException(message);
                        }
                    }
                    else 
                    {
                        requiredMissing = !destination.Argument.Optional && !destination.AlreadySaved;
                    }

                    if (requiredMissing)
                    {
                        string optionLine = BuildOptionLine(destination.Argument);
                        string message = string.Format(CultureInfo.InvariantCulture,
                            "The option '{0}' is required for the command.", optionLine.Trim());
                        throw new CommandLineParseException(message,
                            "-" + destination.Argument.OptionName);
                    }
                }
            }

            #endregion

            #region Private static methods

            /// <summary>
            /// Check if a value is in array.
            /// </summary>
            /// <param name="arr">Array.</param>
            /// <param name="value">Value.</param>
            /// <returns>Boolean.</returns>
            private static bool IsInArray(string[] arr, string value)
            {
                bool found = false;
                for (int i = 0; i < arr.Length; i++)
                {
                    if (arr[i] == value)
                    {
                        found = true;
                        break;
                    }
                }

                return found;
            }

            /// <summary>
            /// Check if the modes are all appeared in totalModes.
            /// </summary>
            /// <param name="totalModes">TotalModes.</param>
            /// <param name="modes">Modes.</param>
            private static void CheckModeArray(string[] totalModes, string[] modes)
            {
                Debug.Assert(totalModes != null);
                if (modes == null)
                {
                    return;
                }

                string msg = "Mode {0} should be listed in mode argument's Modes string.";
                if (modes != null)
                {
                    for (int i = 0; i < modes.Length; i++)
                    {
                        if (!IsInArray(totalModes, modes[i]))
                        {
                            throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, msg, modes[i]));
                        }
                    }
                }
            }

            /// <summary>
            /// Build option string for a mode.
            /// </summary>
            /// <param name="target">Target.</param>
            /// <param name="mode">Mode.</param>
            /// <returns>String.</returns>
            private static string BuildOptionsString(object target, string mode)
            {
                StringBuilder sb = new StringBuilder();
                foreach (FieldInfo field in target.GetType().GetFields(AllFieldBindingFlags))
                {
                    ArgumentAttribute argument = GetFieldArgumentAttribute(field);
                    if (argument == null)
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(mode))
                    {
                        string[] modeArray = argument.GetRequiredModeArray();
                        if (modeArray != null)
                        {
                            bool found = IsInArray(modeArray, mode);
                            if (!found)
                            {
                                continue;
                            }
                        }
                    }

                    if (!argument.Hidden)
                    {
                        string str = field.FieldType.ToString();
                        int i = str.LastIndexOf('.');
                        str = str.Substring(i == -1 ? 0 : i + 1);

                        sb.AppendFormat(CultureInfo.InvariantCulture,
                            "  {0}{1}\t\t({3}) {2}", GetFlagAndPlaceHolderString(argument),
                            Environment.NewLine, argument.Description, str);
                        if (argument.InOutType != InOutType.Unknown)
                        {
                            sb.AppendFormat(CultureInfo.InvariantCulture, " [{0}]",
                                Enum.GetName(typeof(InOutType), argument.InOutType));
                        }

                        sb.Append(Environment.NewLine);
                    }
                    else
                    {
                        if (!argument.Optional)
                        {
                            string message = string.Format(CultureInfo.InvariantCulture,
                                "Argument for {0} can be hidden but can not be optional at the meantime.",
                                field.Name);
                            Debug.Assert(argument.Optional, message);
                            throw new ArgumentException(message);
                        }
                    }
                }

                return sb.ToString();
            }

            /// <summary>
            /// Get all modes supported.
            /// </summary>
            /// <param name="target">Target.</param>
            /// <returns>Modes.</returns>
            private static string[] GetAllModes(object target)
            {
                ArgumentAttribute modeArgument = null;
                foreach (FieldInfo field in target.GetType().GetFields(AllFieldBindingFlags))
                {
                    ArgumentAttribute argument = GetFieldArgumentAttribute(field);
                    if (argument == null)
                    {
                        continue;
                    }

                    if (argument.OptionName == ClpHelper.Mode)
                    {
                        modeArgument = argument;
                        break;
                    }
                }

                if (modeArgument == null)
                {
                    return null;
                }

                string[] modeArray = modeArgument.GetRequiredModeArray();
                if (modeArray == null || modeArray.Length == 0)
                {
                    return null;
                }

                foreach (FieldInfo field in target.GetType().GetFields(AllFieldBindingFlags))
                {
                    foreach (FieldInfo fieldInfo in target.GetType().GetFields(AllFieldBindingFlags))
                    {
                        ArgumentAttribute argument = GetFieldArgumentAttribute(fieldInfo);
                        if (argument == null)
                        {
                            continue;
                        }

                        string[] requiredModes = argument.GetRequiredModeArray();
                        string[] optionalModes = argument.GetOptionalModeArray();
                        CheckModeArray(modeArray, requiredModes);
                        CheckModeArray(modeArray, optionalModes);
                        if (requiredModes != null && optionalModes != null)
                        {
                            for (int i = 0; i < requiredModes.Length; i++)
                            {
                                if (IsInArray(optionalModes, requiredModes[i]))
                                {
                                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                                        "Required modes {0} is conflicted with optional modes {1}",
                                        argument.RequiredModes,
                                        argument.OptionalModes)); 
                                }
                            }
                        }
                    }
                }

                return modeArray;
            }

            /// <summary>
            /// Build optional line string for a argument.
            /// </summary>
            /// <param name="argument">Argument attribute.</param>
            /// <returns>Optional line string.</returns>
            private static string BuildOptionLine(ArgumentAttribute argument)
            {
                StringBuilder sb = new StringBuilder();
                if (!argument.Hidden)
                {
                    if (argument.Optional)
                    {
                        sb.AppendFormat(CultureInfo.InvariantCulture,
                            " [{0}]", GetFlagAndPlaceHolderString(argument));
                    }
                    else
                    {
                        sb.AppendFormat(CultureInfo.InvariantCulture,
                            " {0}", GetFlagAndPlaceHolderString(argument));
                    }
                }
                else
                {
                    if (!argument.Optional)
                    {
                        string message = string.Format(CultureInfo.InvariantCulture,
                            "Argument for -{0} can be hidden but can not be optional at the meantime.",
                            argument.OptionName);
                        Debug.Assert(argument.Optional, message);
                        throw new ArgumentException(message);
                    }
                }

                return sb.ToString();
            }

            /// <summary>
            /// Get the ArgumentAttribute from the field. If the field don't define this
            /// Custom attribute, it will return null.
            /// </summary>
            /// <param name="fieldInfo">Field information.</param>
            /// <returns>Argument attribute associated with the field.</returns>
            private static ArgumentAttribute GetFieldArgumentAttribute(FieldInfo fieldInfo)
            {
                ArgumentAttribute[] argument =
                    (ArgumentAttribute[])fieldInfo.GetCustomAttributes(typeof(ArgumentAttribute), false);

                return argument.Length == 1 ? argument[0] : null;
            }

            /// <summary>
            /// When output the usage, this function will generate the flag string
            /// Such as "-time n1 n2..." string.
            /// </summary>
            /// <param name="argument">Argument attribute.</param>
            /// <returns>Argument presentation on command line.</returns>
            private static string GetFlagAndPlaceHolderString(ArgumentAttribute argument)
            {
                return (!string.IsNullOrEmpty(argument.OptionName) ? "-" : string.Empty) +
                    argument.OptionName +
                    (string.IsNullOrEmpty(argument.UsagePlaceholder) ?
                        string.Empty : " " + argument.UsagePlaceholder);
            }

            /// <summary>
            /// Call by the GetFlagAndPlaceHolderString() function, and generate the frendly
            /// Name of each parameter in command line, such as "n1 n2 ..." string.
            /// </summary>
            /// <param name="fieldInfo">Field information.</param>
            /// <returns>Field name of the argument.</returns>
            private static string GetFieldFriendlyTypeName(FieldInfo fieldInfo)
            {
                Type type = fieldInfo.FieldType.IsArray ?
                    fieldInfo.FieldType.GetElementType() : fieldInfo.FieldType;

                string str;
                if (type == typeof(bool))
                {
                    str = string.Empty;
                }
                else
                {
                    // Use the Type name's first character,
                    // for example: System.int -> i, System.double -> d
                    str = type.ToString();
                    int i = str.LastIndexOf('.');
                    i = i == -1 ? 0 : i + 1;
                    str = char.ToLower(str[i], CultureInfo.CurrentCulture).ToString();
                }

                return fieldInfo.FieldType.IsArray ? str + "1 " + str + "2 ..." : str;
            }

            #endregion

            #region Private methods

            /// <summary>
            /// Check and parse the internal target and external target, then push the result
            /// Of parsing into the DestMap.
            /// Internal target class has predefined some flags, such as "-h", "-C"
            /// External target class are defined by the library users.
            /// </summary>
            /// <param name="target">Target object to reflect usage information.</param>
            private void ParseTheDestination(object target)
            {
                // Check and parse the internal target, so use Debug.Assert to catch the error.
                foreach (FieldInfo fieldInfo in typeof(InternalFlags).GetFields(AllFieldBindingFlags))
                {
                    ArgumentAttribute argument = GetFieldArgumentAttribute(fieldInfo);
                    if (string.IsNullOrEmpty(argument.UsagePlaceholder))
                    {
                        argument.UsagePlaceholder = GetFieldFriendlyTypeName(fieldInfo);
                    }

                    Debug.Assert(argument != null);

                    Destination destination = new Destination(fieldInfo, argument, _internalTarget);

                    Debug.Assert(destination.Argument.OptionName.Length != 0);
                    Debug.Assert(char.IsLetter(destination.Argument.OptionName[0]) ||
                        destination.Argument.OptionName[0] == '?');

                    // Assert there is no duplicate flag in the user defined argument class.
                    Debug.Assert(!_destMap.ContainsKey(destination.Argument.OptionName));

                    _destMap.Add(destination.Argument.OptionName, destination);
                }

                // Check and parse the external target, so use throw exception
                // to handle the unexpect target difine.
                foreach (FieldInfo fieldInfo in target.GetType().GetFields(AllFieldBindingFlags))
                {
                    ArgumentAttribute argument = GetFieldArgumentAttribute(fieldInfo);
                    if (argument == null)
                    {
                        continue;
                    }

                    Destination destination = new Destination(fieldInfo, argument, null);

                    // Assert user don't define a non-letter as a flag in the user defined argument class.
                    if (destination.Argument.OptionName.Length > 0 && !char.IsLetter(destination.Argument.OptionName[0]))
                    {
                        throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                            "User can't define a non-letter flag ({0}).", destination.Argument.OptionName[0]),
                            destination.Argument.OptionName[0].ToString());
                    }

                    // Assert there is no duplicate flag in the user defined argument class.
                    if (_destMap.ContainsKey(destination.Argument.OptionName))
                    {
                        throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                            "Duplicate flag are defined in the argument class."),
                            destination.Argument.OptionName);
                    }

                    _destMap.Add(destination.Argument.OptionName, destination);
                }
            }

            /// <summary>
            /// Check the given string is a flag, if so, get the corresponding destination class of the flag.
            /// </summary>
            /// <param name="str">String to test.</param>
            /// <returns>Destination.</returns>
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Ignore.")]
            private Destination IsFlagStringAndGetTheDestination(string str)
            {
                Debug.Assert(!string.IsNullOrEmpty(str));

                if (str.Length < 2 || str[0] != '-'
                    || (!char.IsLetter(str[1]) && str[1] != '?'))
                {
                    return null;
                }

                str = str.Substring(1).ToLower(CultureInfo.InvariantCulture);
                return _destMap.ContainsKey(str) ? _destMap[str] : null;
            }

            /// <summary>
            /// Save the given string to the empty flag("") destination class.
            /// </summary>
            /// <param name="str">Flag string to save, not the realy null Flag, is the "" Flag.</param>
            /// <returns>Destination.</returns>
            private Destination SaveValueStringToEmptyFlag(string str)
            {
                Destination destination = _destMap.ContainsKey(string.Empty) ? _destMap[string.Empty] : null;

                if (destination == null || destination.AlreadySaved ||
                    !destination.TryToAddValue(_target, str))
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendFormat("Unrecognized command {0}. ", str);

                    Assembly assembly = Assembly.GetEntryAssembly();
                    if (assembly != null)
                    {
                        sb.AppendFormat(CultureInfo.InvariantCulture,
                            "Run '{0} -?' for help.", Path.GetFileName(assembly.Location));
                    }

                    throw new CommandLineParseException(sb.ToString(), str);
                }

                return destination;
            }

            #endregion
        }

        /// <summary>
        /// Private class, to hold the information of the target object.
        /// </summary>
        private class Destination
        {
            #region Field

            private FieldInfo _fieldInfo;
            private ArgumentAttribute _argument;

            // Hold the internal target, it distinguish
            private InternalFlags _internalTarget;
            private bool _alreadySaved;

            // A internal target to a external target. If it is external, this member is null.
            private ArrayList _parameterList;

            #endregion

            #region Construction

            /// <summary>
            /// Initializes a new instance of the <see cref="Destination"/> class.
            /// </summary>
            /// <param name="fieldInfo">Field information.</param>
            /// <param name="argument">Argument attribute.</param>
            /// <param name="internalTarget">Internal flags.</param>
            public Destination(FieldInfo fieldInfo, ArgumentAttribute argument, InternalFlags internalTarget)
            {
                _fieldInfo = fieldInfo;
                _argument = argument;
                _internalTarget = internalTarget;

                // _AlreadySaved = false;
                _parameterList = fieldInfo.FieldType.IsArray ? new ArrayList() : null;
            }

            #endregion

            #region Properties

            /// <summary>
            /// Gets internal target.
            /// </summary>
            public InternalFlags InternalTarget
            {
                get { return _internalTarget; }
            }

            /// <summary>
            /// Gets Argument attribute.
            /// </summary>
            public ArgumentAttribute Argument
            {
                get { return _argument; }
            }

            /// <summary>
            /// Gets a value indicating whether Value already saved.
            /// </summary>
            public bool AlreadySaved
            {
                get { return _alreadySaved; }
            }

            #endregion

            #region Class Methods

            /// <summary>
            /// Parse the string to given type of value.
            /// </summary>
            /// <param name="type">Type of value.</param>
            /// <param name="str">String to parse.</param>
            /// <returns>Result value.</returns>
            public static object TryParseStringToValue(Type type, string str)
            {
                object obj = null;

                if (type == typeof(string))
                {
                    // string to string, don't need parse.
                    obj = str;
                }
                else if (type == typeof(sbyte) || type == typeof(byte) ||
                         type == typeof(short) || type == typeof(ushort) ||
                         type == typeof(int) || type == typeof(uint) ||
                         type == typeof(long) || type == typeof(ulong))
                {
                    // Use the dec style to parse the string into integer value frist.
                    // If it failed, then use the hex sytle to parse it again.
                    obj = TryParse(str, type, NumberStyles.Integer | NumberStyles.AllowThousands);
                    if (obj == null && str.Substring(0, 2) == "0x")
                    {
                        obj = TryParse(str.Substring(2), type, NumberStyles.HexNumber);
                    }
                }
                else if (type == typeof(double) || type == typeof(float))
                {
                    // Use float style to parse the string into float value.
                    obj = TryParse(str, type, NumberStyles.Float | NumberStyles.AllowThousands);
                }
                else
                {
                    // Use the default style to parse the string.
                    obj = TryParse(str, type);
                }

                return obj;
            }

            /// <summary>
            /// Try to and a value to the target. Frist prase the string form parameter
            /// To a given value. And then, save the value to a target field or a value
            /// List.
            /// </summary>
            /// <param name="target">Target object to reflect usage information.</param>
            /// <param name="str">String value to add.</param>
            /// <returns>True if succeeded, otherwise false.</returns>
            public bool TryToAddValue(object target, string str)
            {
                if (_alreadySaved)
                {
                    return false;
                }

                if (_internalTarget != null)
                {
                    target = _internalTarget;
                }

                // If this field is an array, it will save the prased value into an value list.
                // Otherwise, it will save the parse value to the field of the target directly.
                if (_fieldInfo.FieldType.IsArray)
                {
                    object value = TryParseStringToValue(_fieldInfo.FieldType.GetElementType(), str);
                    if (value == null)
                    {
                        return false;
                    }

                    _parameterList.Add(value);
                }
                else
                {
                    object value = TryParseStringToValue(_fieldInfo.FieldType, str);
                    if (value == null)
                    {
                        return false;
                    }

                    _fieldInfo.SetValue(target, value);
                    _alreadySaved = true;
                }

                return true;
            }

            /// <summary>
            /// Save function will do some cleanup of the value save.
            /// </summary>
            /// <param name="target">Target object to reflect usage information.</param>
            public void Save(object target)
            {
                if (_internalTarget != null)
                {
                    target = _internalTarget;
                }

                if (_fieldInfo.FieldType.IsArray)
                {
                    // When the filed is an array, this function will save all values in the ParameterList
                    // into the array field.
                    Debug.Assert(!_alreadySaved);
                    Array array = (Array)_fieldInfo.GetValue(target);
                    if (array != null && array.Length != _parameterList.Count)
                    {
                        string message = string.Format(CultureInfo.InvariantCulture,
                            "For option flag -{0}, the parameter number is {1}, which is not as expected {2}.",
                            _argument.OptionName, _parameterList.Count, array.Length);
                        throw new CommandLineParseException(message,
                            "-" + _argument.OptionName);
                    }

                    _fieldInfo.SetValue(target,
                        _parameterList.ToArray(_fieldInfo.FieldType.GetElementType()));
                }
                else if (_fieldInfo.FieldType == typeof(bool))
                {
                    if (!_alreadySaved)
                    {
                        bool b = (bool)_fieldInfo.GetValue(target);
                        b = !b;
                        _fieldInfo.SetValue(target, b);
                    }
                }
                else if (!_alreadySaved)
                {        // Other types do nothing, only check its already saved,
                         // beacuse the value must be saved in the TryToAddValue();
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "The option flag [-{0}] needs {1} parameter.",
                        _argument.OptionName, _argument.UsagePlaceholder);
                    throw new CommandLineParseException(message, "-" + _argument.OptionName);
                }

                _alreadySaved = true;
            }

            #endregion

            #region Private static methods

            /// <summary>
            /// Use the given style to parse the string to given type of value.
            /// </summary>
            /// <param name="str">String to parse.</param>
            /// <param name="type">Type of value.</param>
            /// <param name="ns">Number styles.</param>
            /// <returns>Result value.</returns>
            private static object TryParse(string str, Type type, NumberStyles ns)
            {
                // Use reflection to dynamic load the TryParse function of given type.
                Type[] typeArgs = new Type[] 
                        {
                            typeof(string),
                            typeof(NumberStyles),
                            typeof(IFormatProvider),
                            Type.GetType(type.ToString() + "&")
                        };

                MethodInfo mi = type.GetMethod("TryParse", typeArgs);

                // Initilze these four parameters of the Tryparse funtion.
                object[] objArgs = new object[]
                {
                    str,
                    ns,
                    CultureInfo.InvariantCulture,
                    Activator.CreateInstance(type) 
                };

                return DoTryParse(mi, objArgs);
            }

            /// <summary>
            /// Use the defalut style to parse the string to given type of value.
            /// </summary>
            /// <param name="str">String to parse.</param>
            /// <param name="type">Type of value.</param>
            /// <returns>Result value.</returns>
            private static object TryParse(string str, Type type)
            {
                // Use reflection to dynamic load the TryParse function of given type.
                MethodInfo mi = type.GetMethod("TryParse", new Type[] { typeof(string), Type.GetType(type.ToString() + "&") });

                // Initilze these two parameters of the Tryparse funtion.
                object[] objArgs = new object[] { str, Activator.CreateInstance(type) };

                return DoTryParse(mi, objArgs);
            }

            /// <summary>
            /// Run the TryParse function by the given method and parameters.
            /// </summary>
            /// <param name="methodInfo">Method information.</param>
            /// <param name="methodArgs">Method arguments.</param>
            /// <returns>Result value.</returns>
            private static object DoTryParse(MethodInfo methodInfo, object[] methodArgs)
            {
                object retVal = methodInfo.Invoke(null, methodArgs);

                if (!(retVal is bool))
                {
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                        "TryParse() method must return a bool value."),
                        methodArgs[methodArgs.Length - 1].GetType().ToString());
                }

                // the last parameter of TryParse method is a reference of a value.
                // Therefore, it will return the last value of the parameter array.
                // If the TryParse function failed when parsing, this DoTryParse will
                // return null.
                return (bool)retVal ? methodArgs[methodArgs.Length - 1] : null;
            }

            #endregion
        }

        /// <summary>
        /// This class is defined to take the internal flags, such as -h, -?, -C, and etc.
        /// When the parse begin to parse the target object, it will parse is class's object
        /// First. So, the parse can first put the internal flags into the DestMap to avoid
        /// Library user redifined those flags. And When finish parsed all flags, The library
        /// Will check the property NeedHelp to determinated those flags are appeared in the
        /// Command line.
        /// </summary>
        private sealed class InternalFlags
        {
            #region Fields

            [Argument("h", Description = "Help", Optional = true)]
            private bool _needHelp1;

            [Argument("?", Description = "Help", Optional = true)]
            private bool _needHelp2;

            [Argument("help", Description = "Help", Optional = true)]
            private bool _needHelp3;

            [Argument("conf", Description = "Configuration file", Optional = true)]
            private string _configFile;   // use internal instead of private to avoid unusing warning.

            #endregion

            #region Construction

            /// <summary>
            /// Initializes a new instance of the <see cref="InternalFlags"/> class.
            /// </summary>
            public InternalFlags()
            {
                _needHelp1 = _needHelp2 = _needHelp3 = false;
            }

            #endregion

            #region Properties

            /// <summary>
            /// Gets a value indicating whether Flag indicating whether user requires help.
            /// </summary>
            public bool NeedHelp
            {
                get { return _needHelp1 || _needHelp2 || _needHelp3; }
            }

            /// <summary>
            /// Gets or sets Configuration file path.
            /// </summary>
            public string ConfigFile
            {
                get { return _configFile; }
                set { _configFile = value; }
            }

            #endregion
        }

        #endregion
    }
}