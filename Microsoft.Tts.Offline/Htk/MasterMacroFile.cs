//----------------------------------------------------------------------------
// <copyright file="MasterMacroFile.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements Master Macro File
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Htk
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Microsoft.Tts.Offline.IO;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// The class to represent HTK Macro.
    /// </summary>
    public class MacroName
    {
        /// <summary>
        /// Indicator of the begin of macro in MMF file.
        /// </summary>
        public const string Indicator = "~";

        /// <summary>
        /// Gets or sets the symbol for this macro object.
        /// </summary>
        public string Symbol { get; set; }

        /// <summary>
        /// Gets or sets the name for this macro object.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Creates an instance of MacroName from a line of text string.
        /// </summary>
        /// <param name="line">A line of text string to create macro.</param>
        /// <returns>An instance of MacroName.</returns>
        public static MacroName Create(string line)
        {
            MacroName macro = new MacroName();
            macro.Parse(line);
            return macro;
        }

        /// <summary>
        /// Parses macro from line of text string.
        /// </summary>
        /// <param name="line">Line of text string to parse.</param>
        public void Parse(string line)
        {
            Helper.ThrowIfNull(line);

            string[] items = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (items.Length != 1 && items.Length != 2)
            {
                throw new InvalidDataException(Helper.NeutralFormat(
                    "Invalid macro is found for line [{0}], which is expected to be (<macro symbol>|<macro symbol> <macro name>).", line));
            }

            if (!items[0].StartsWith(MacroName.Indicator))
            {
                throw new InvalidDataException(Helper.NeutralFormat(
                    "Invalid macro symbol is found for line [{0}], which is expected to start with '~'.", line));
            }

            Symbol = items[0].Substring(1);

            if (Symbol.Length != 1)
            {
                throw new InvalidDataException(Helper.NeutralFormat(
                    "Only symbol with single-character is supported for macro, but it is [{0}] in line [{1}].", Symbol, line));
            }

            Name = items.Length > 1 ? items[1] : string.Empty;

            if (!string.IsNullOrEmpty(Name))
            {
                Name = Name.Trim('"');
                if (string.IsNullOrEmpty(Name))
                {
                    throw new InvalidDataException(Helper.NeutralFormat(
                        "Empty context in macro name in line [{0}].", line));
                }
            }
        }
    }

    /// <summary>
    /// The class to represent HTK Variance Floor.
    /// </summary>
    public class VarFloor
    {
        /// <summary>
        /// Gets or sets the macro name of this object.
        /// </summary>
        public MacroName Macro { get; set; }

        /// <summary>
        /// Gets or sets the variance of this object.
        /// </summary>
        public double[] Variance { get; set; }
    }

    /// <summary>
    /// The class to represent HTK state.
    /// </summary>
    public class HmmState
    {
        /// <summary>
        /// Initializes a new instance of the HmmState class.
        /// </summary>
        public HmmState()
        {
            Streams = new List<HmmStream>();
        }

        /// <summary>
        /// Gets the streams of this object.
        /// </summary>
        public List<HmmStream> Streams { get; private set; }
    }

    /// <summary>
    /// The class to represent HTK model.
    /// </summary>
    public class HmmModel
    {
        /// <summary>
        /// Initializes a new instance of the HmmModel class.
        /// </summary>
        public HmmModel()
        {
            States = new List<HmmState>();
        }

        /// <summary>
        /// Gets or sets the macro name of this object.
        /// </summary>
        public MacroName Macro { get; set; }

        /// <summary>
        /// Gets the states of this object.
        /// </summary>
        public List<HmmState> States { get; private set; }

        /// <summary>
        /// Gets or sets the transition of this object.
        /// </summary>
        public Transition Transition { get; set; }

        /// <summary>
        /// Corrects the variance of the model to be bigger than floor.
        /// If it is smaller than floor, use the value from floor for each dimension.
        /// </summary>
        /// <param name="floor">The variance floor values.</param>
        public void CorrectVariance(double[] floor)
        {
            foreach (var gaussian in States.SelectMany(s => s.Streams).SelectMany(s => s.Gaussians))
            {
                if (gaussian.Length != floor.Length)
                {
                    throw new InvalidDataException(Helper.NeutralFormat(
                        "The dimension of the variance floor [{0}] should equal with that of gaussian [{1}].",
                        floor.Length, gaussian.Length));
                }

                for (int i = 0; i < gaussian.Variance.Length; i++)
                {
                    gaussian.Variance[i] = (gaussian.Variance[i] >= floor[i]) ? gaussian.Variance[i] : floor[i];
                }
            }
        }
    }

    /// <summary>
    /// The class to represent HTK Transition.
    /// </summary>
    public class Transition
    {
        /// <summary>
        /// Gets or sets the macro name of this object.
        /// </summary>
        public MacroName Macro { get; set; }

        /// <summary>
        /// Gets or sets the transition probability matrix of this object.
        /// </summary>
        public double[,] Matrix { get; set; }
    }

    /// <summary>
    /// The class to represent HTK Master Macro File.
    /// </summary>
    public class MasterMacroFile
    {
        /// <summary>
        /// Initializes a new instance of the MasterMacroFile class.
        /// </summary>
        public MasterMacroFile()
        {
            Reset();
        }

        #region Properties

        /// <summary>
        /// Gets or sets the stream size information of this MMF file.
        /// </summary>
        public int[] StreamInfo { get; set; }

        /// <summary>
        /// Gets or sets the MSD information of this MMF file.
        /// </summary>
        public int[] MsdInfo { get; set; }

        /// <summary>
        /// Gets or sets the vector size of this MMF file.
        /// </summary>
        public int VecSize { get; set; }

        /// <summary>
        /// Gets or sets the vector information of this MMF file.
        /// </summary>
        public string[] VecInfo { get; set; }

        /// <summary>
        /// Gets the global variance floors used in this MMF file, keyed by macro name.
        /// </summary>
        public Dictionary<string, VarFloor> VarFloors { get; private set; }

        /// <summary>
        /// Gets the global transitions used in this MMF file, keyed by macro name.
        /// </summary>
        public Dictionary<string, Transition> Transitions { get; private set; }

        /// <summary>
        /// Gets the global streams used in this MMF file, keyed by macro name.
        /// </summary>
        public Dictionary<string, HmmStream> Streams { get; private set; }

        /// <summary>
        /// Gets the global models used in this MMF file, keyed by macro name.
        /// </summary>
        public Dictionary<string, HmmModel> Models { get; private set; }

        #endregion

        /// <summary>
        /// Loads the macros in Master Macro File from text file.
        /// </summary>
        /// <param name="filePath">The location of text file to read macros from.</param>
        /// <returns>Current instance.</returns>
        public MasterMacroFile LoadText(string filePath)
        {
            using (StreamReader reader = new StreamReader(filePath))
            {
                return Load(reader);
            }
        }

        /// <summary>
        /// Loads the macros in Master Macro File from text reader instance.
        /// </summary>
        /// <param name="reader">Text reader instance to read macros from.</param>
        /// <returns>MasterMacroFile.</returns>
        public MasterMacroFile Load(TextReader reader)
        {
            Helper.ThrowIfNull(reader);

            Reset();

            using (RewindableTextReader rewinder = new RewindableTextReader(reader))
            {
                Load(rewinder);
            }

            return this;
        }

        /// <summary>
        /// Saves macros of this instance in Master Macro File format into text writer instance.
        /// </summary>
        /// <param name="writer">The text writer instance to write data into.</param>
        public void Save(TextWriter writer)
        {
            Helper.ThrowIfNull(writer);

            SaveGlobalOptions(writer, StreamInfo, MsdInfo, VecSize, VecInfo);

            Transitions.Keys.ForEach(
                v =>
                writer.Write("~t \"{1}\"{0}{2}", Environment.NewLine, Transitions[v].Macro.Name, ToString(Transitions[v])));

            VarFloors.Keys.ForEach(
                v =>
                writer.Write("~v \"{1}\"{0}<VARIANCE> {2}{0} {3}{0}",
                    Environment.NewLine, VarFloors[v].Macro.Name, VarFloors[v].Variance.Length,
                    VarFloors[v].Variance.Select(i => i.ToString("e6", CultureInfo.InvariantCulture)).Concatenate(" ")));

            Streams.Keys.ForEach(
                v =>
                writer.Write("~s \"{1}\"{0}{2}", Environment.NewLine, v, ToString(Streams[v])));

            Models.Keys.ForEach(
                v =>
                writer.Write("{0}", ToString(Models[v], Streams, Transitions)));
        }

        #region Statci save operations

        /// <summary>
        /// Saves global options information into the text writer instances.
        /// </summary>
        /// <param name="writer">The text writer instance to write data into.</param>
        /// <param name="streamInfo">Stream information to save.</param>
        /// <param name="msdInfo">MSD information to save.</param>
        /// <param name="vecSize">Vector size to save.</param>
        /// <param name="vecInfo">Vector information to save.</param>
        private static void SaveGlobalOptions(TextWriter writer, int[] streamInfo, int[] msdInfo, int vecSize, string[] vecInfo)
        {
            Helper.ThrowIfNull(writer);
            Helper.ThrowIfNull(streamInfo);

            writer.WriteLine(MacroName.Indicator + "o");
            if (streamInfo != null && streamInfo.Length > 0)
            {
                writer.WriteLine("<STREAMINFO> {0} {1}", streamInfo.Length, streamInfo.Concatenate(" "));
            }

            if (msdInfo != null && msdInfo.Length > 0)
            {
                writer.WriteLine("<MSDINFO> {0} {1}", msdInfo.Length, msdInfo.Concatenate(" "));
            }

            writer.WriteLine("<VECSIZE> {0}{1}", vecSize,
                (vecInfo == null || vecInfo.Length == 0) ? string.Empty : vecInfo.Select(i => "<" + i + ">").Concatenate(string.Empty));
        }

        /// <summary>
        /// Converts the transition instance into string in MMF format.
        /// </summary>
        /// <param name="transition">The transition instance to convert.</param>
        /// <returns>Transition instance in string format.</returns>
        private static string ToString(Transition transition)
        {
            Helper.ThrowIfNull(transition);
            return Helper.NeutralFormat("<TRANSP> {1}{0}{2}",
                Environment.NewLine, transition.Matrix.GetLength(0), ToString(transition.Matrix));
        }

        /// <summary>
        /// Converts the HMM stream instance into string in MMF format.
        /// </summary>
        /// <param name="stream">The stream instance to convert.</param>
        /// <returns>Stream instance in string format.</returns>
        private static string ToString(HmmStream stream)
        {
            Helper.ThrowIfNull(stream);
            StringBuilder builder = new StringBuilder();
            if (stream.Gaussians.Length > 1)
            {
                builder.AppendFormat(CultureInfo.InvariantCulture,
                    "<NUMMIXES> {1}{0}", Environment.NewLine, stream.Gaussians.Length);
            }

            for (int i = 0; i < stream.Gaussians.Length; i++)
            {
                if (stream.Gaussians.Length > 1)
                {
                    builder.AppendFormat(CultureInfo.InvariantCulture,
                        "<MIXTURE> {1} {2:e6}{0}", Environment.NewLine, i + 1, stream.Gaussians[i].Weight);
                }

                builder.Append(ToString(stream.Gaussians[i]));
            }

            return builder.ToString();
        }

        /// <summary>
        /// Converts the HMM gaussian instance into string in MMF format.
        /// </summary>
        /// <param name="gaussian">The gaussian instance to convert.</param>
        /// <returns>Gaussian instance in string format.</returns>
        private static string ToString(Gaussian gaussian)
        {
            Helper.ThrowIfNull(gaussian);
            StringBuilder builder = new StringBuilder();

            if (gaussian.Length == 0)
            {
                builder.AppendFormat(CultureInfo.InvariantCulture,
                    "<MEAN> 0{0}<VARIANCE> 0{0}", Environment.NewLine);
                builder.AppendFormat(CultureInfo.InvariantCulture,
                    "<GCONST> {1:e6}{0}", Environment.NewLine, gaussian.GlobalConstant);
            }
            else
            {
                builder.AppendFormat(CultureInfo.InvariantCulture,
                    "<MEAN> {1}{0} {2}{0}<VARIANCE> {3}{0} {4}{0}",
                    Environment.NewLine,
                    gaussian.Mean.Length,
                    gaussian.Mean.Select(v => v.ToString("e6", CultureInfo.InvariantCulture)).Concatenate(" "),
                    gaussian.Variance.Length,
                    gaussian.Variance.Select(v => v.ToString("e6", CultureInfo.InvariantCulture)).Concatenate(" "));

                if (gaussian.GlobalConstant != 0.0f)
                {
                    builder.AppendFormat(CultureInfo.InvariantCulture,
                        "<GCONST> {1:e6}{0}", Environment.NewLine, gaussian.GlobalConstant);
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Converts double matrix into string in MMF format.
        /// </summary>
        /// <param name="values">Value matrix to convert.</param>
        /// <returns>Value matrix instance in string format.</returns>
        private static string ToString(double[,] values)
        {
            Helper.ThrowIfNull(values);
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < values.GetLength(0); i++)
            {
                for (int j = 0; j < values.GetLength(1); j++)
                {
                    builder.Append(" ");
                    builder.AppendFormat(CultureInfo.InvariantCulture, "{0:e6}", values[i, j]);
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }

        /// <summary>
        /// Converts the HMM model instance into string in MMF format.
        /// </summary>
        /// <param name="model">The model instance to convert.</param>
        /// <param name="streams">The global streams to refer if any.</param>
        /// <param name="transitions">The global transitions to refer if any.</param>
        /// <returns>Model instance in string format.</returns>
        private static string ToString(HmmModel model,
            Dictionary<string, HmmStream> streams, Dictionary<string, Transition> transitions)
        {
            Helper.ThrowIfNull(model);
            Helper.ThrowIfNull(streams);
            Helper.ThrowIfNull(transitions);

            StringBuilder builder = new StringBuilder();

            if (model.Macro.Name.StartsWith("\"") || model.Macro.Name.EndsWith("\""))
            {
                throw new InvalidDataException(Helper.NeutralFormat(
                    "Macro name of model should not start or end with '\"', but it is [{0}].", model.Macro.Name));
            }

            builder.AppendFormat(CultureInfo.InvariantCulture,
                "~h \"{1}\"{0}", Environment.NewLine, model.Macro.Name);
            builder.AppendFormat(CultureInfo.InvariantCulture,
                "<BEGINHMM>{0}<NUMSTATES> {1}{0}", Environment.NewLine, model.States.Count + 2);

            if (model.States.Count == 0)
            {
                throw new InvalidDataException(Helper.NeutralFormat(
                    "There is not state in the model [{0}].", model.Macro.Name));
            }

            for (int i = 0; i < model.States.Count; i++)
            {
                builder.AppendFormat(CultureInfo.InvariantCulture, "<STATE> {1}{0}", Environment.NewLine, i + 2);
                for (int j = 0; j < model.States[i].Streams.Count; j++)
                {
                    if (streams.Values.Any(v => v == model.States[i].Streams[j]))
                    {
                        // References to the global stream instances.
                        builder.AppendFormat(CultureInfo.InvariantCulture,
                            "~s \"{1}\"{0}", Environment.NewLine, model.States[i].Streams[j].Name);
                    }
                    else
                    {
                        // Embeds the stream information into the local data.
                        builder.AppendFormat(CultureInfo.InvariantCulture,
                            "<STREAM> {1}{0}", Environment.NewLine, j + 1);
                        builder.AppendFormat(ToString(model.States[i].Streams[j]));
                    }
                }
            }

            if (model.Transition != null)
            {
                if (transitions.Values.Any(t => t == model.Transition))
                {
                    // References to the global transition instances.
                    builder.AppendFormat(CultureInfo.InvariantCulture,
                        "~t \"{1}\"{0}", Environment.NewLine, model.Transition.Macro.Name);
                }
                else
                {
                    // Embeds the transition into the local data.
                    builder.AppendFormat(ToString(model.Transition));
                }
            }

            builder.AppendLine("<ENDHMM>");

            return builder.ToString();
        }

        #endregion

        #region Static load operations

        /// <summary>
        /// Reads one HMM model instance from text reader instance.
        /// </summary>
        /// <param name="rewinder">Rewindable text reader instance to read HMM model from.</param>
        /// <param name="streams">Global HMM streams if any reference.</param>
        /// <param name="transition">Global transitions if any reference.</param>
        /// <returns>HMM model instance loaded.</returns>
        private static HmmModel ReadModel(RewindableTextReader rewinder,
            Dictionary<string, HmmStream> streams, Dictionary<string, Transition> transition)
        {
            Helper.ThrowIfNull(rewinder);
            Helper.ThrowIfNull(streams);
            Helper.ThrowIfNull(transition);
            HmmModel model = new HmmModel();

            Ensure(rewinder.ReadLine(), "<BEGINHMM>");

            int stateCount = ParseTagValue(rewinder.ReadLine(), "<NUMSTATES>");

            // Excludes the entrance and existing states
            for (int i = 1; i < stateCount - 1; i++)
            {
                HmmState state = ReadState(rewinder, streams);
                model.States.Add(state);
            }

            if (rewinder.PeekLine().StartsWith("~t"))
            {
                MacroName macro = MacroName.Create(rewinder.ReadLine());
                if (!transition.ContainsKey(macro.Name))
                {
                    throw new InvalidDataException(Helper.NeutralFormat("Transition [{0}] is not found.", macro.Name));
                }

                model.Transition = transition[macro.Name];
            }
            else if (rewinder.PeekLine().StartsWith("<TRANSP>"))
            {
                model.Transition = new Transition()
                {
                    Matrix = ReadTransition(rewinder),
                };
            }

            Ensure(rewinder.ReadLine(), "<ENDHMM>");

            return model;
        }

        /// <summary>
        /// Reads one HMM state instance from text reader instance.
        /// </summary>
        /// <param name="rewinder">Rewindable text reader instance to read HMM model from.</param>
        /// <param name="streams">Global HMM streams if any reference.</param>
        /// <returns>HMM state instance loaded.</returns>
        private static HmmState ReadState(RewindableTextReader rewinder, Dictionary<string, HmmStream> streams)
        {
            Helper.ThrowIfNull(rewinder);
            Helper.ThrowIfNull(streams);
            HmmState state = new HmmState();

            int stateIndex = ParseTagValue(rewinder.ReadLine(), "<STATE>");
            string line = rewinder.PeekLine();

            if (line.StartsWith("<SWEIGHTS>"))
            {
                rewinder.ReadLine();
                line = rewinder.ReadLine();
                line = rewinder.PeekLine();
            }

            if (line.StartsWith(MacroName.Indicator))
            {
                MacroName macro = MacroName.Create(rewinder.ReadLine());
                if (macro.Symbol != "s")
                {
                    throw new InvalidDataException(
                        Helper.NeutralFormat("Stream symbol 's' is expected for line [{0}].", line));
                }

                if (!streams.ContainsKey(macro.Name))
                {
                    throw new InvalidDataException(Helper.NeutralFormat(
                        "Referenced macro [{0}] does not exist, used by line [{1}].", macro.Name, line));
                }

                state.Streams.Add(streams[macro.Name]);
            }
            else
            {
                while (line.StartsWith("<STREAM>"))
                {
                    HmmStream stream = ReadStream(rewinder);
                    state.Streams.Add(stream);
                    line = rewinder.PeekLine();
                }
            }

            return state;
        }

        /// <summary>
        /// Reads one HMM stream instance from text reader instance.
        /// </summary>
        /// <param name="rewinder">The text reader to read stream from.</param>
        /// <returns>HMM stream instance loaded.</returns>
        private static HmmStream ReadStream(RewindableTextReader rewinder)
        {
            Helper.ThrowIfNull(rewinder);
            HmmStream stream = new HmmStream();

            string line = rewinder.PeekLine();
            int streamIndex = 0;
            if (line.StartsWith("<STREAM>"))
            {
                streamIndex = ParseTagValue(rewinder.ReadLine(), "<STREAM>");
            }

            int mixtureNumber = 1;
            line = rewinder.PeekLine();
            if (line.StartsWith("<NUMMIXES>"))
            {
                mixtureNumber = ParseTagValue(rewinder.ReadLine(), "<NUMMIXES>");
            }

            List<Gaussian> gaussians = new List<Gaussian>();
            for (int i = 0; i < mixtureNumber; i++)
            {
                int mixtureIndex = 0;
                float weight = 0.0f;
                line = rewinder.PeekLine();
                if (line.StartsWith("<MIXTURE>"))
                {
                    string[] values = ParseTagValues(rewinder.ReadLine(), "<MIXTURE>");
                    mixtureIndex = int.Parse(values[0], CultureInfo.InvariantCulture);
                    weight = float.Parse(values[1], CultureInfo.InvariantCulture);

                    if (mixtureIndex != i + 1)
                    {
                        throw new InvalidDataException(
                            Helper.NeutralFormat("The mixture index is mis-matched between {0} and {1} in line [{2}]",
                            i, mixtureIndex, line));
                    }
                }

                Gaussian gaussian = ReadGaussian(rewinder);
                gaussian.Weight = weight;
                gaussians.Add(gaussian);
            }

            stream.Gaussians = gaussians.ToArray();

            return stream;
        }

        /// <summary>
        /// Reads one HMM gaussian instance from text reader instance.
        /// </summary>
        /// <param name="rewinder">The text reader to read gaussian from.</param>
        /// <returns>HMM gaussian instance loaded.</returns>
        private static Gaussian ReadGaussian(RewindableTextReader rewinder)
        {
            Helper.ThrowIfNull(rewinder);
            Gaussian gaussian = new Gaussian();

            gaussian.Mean = ReadTagArray(rewinder, "<MEAN>");
            gaussian.Variance = ReadTagArray(rewinder, "<VARIANCE>");

            if (gaussian.Mean.Length != gaussian.Variance.Length)
            {
                throw new InvalidDataException(Helper.NeutralFormat(
                    "The dimension of mean [{0}] should equal with that of variance [{1}].",
                    gaussian.Mean.Length, gaussian.Variance.Length));
            }

            gaussian.Length = gaussian.Mean.Length;

            string line = rewinder.PeekLine();
            if (line != null && line.StartsWith("<GCONST>"))
            {
                string[] items = ParseTagValues(rewinder.ReadLine(), "<GCONST>");
                if (items.Length != 1)
                {
                    throw new InvalidDataException(Helper.NeutralFormat(
                        "Single value is expected for <GCONST>, but it is with line [{0}].", line));
                }

                gaussian.GlobalConstant = float.Parse(items[0], CultureInfo.InvariantCulture);
            }

            return gaussian;
        }

        /// <summary>
        /// Reads one transition instance from text reader instance.
        /// </summary>
        /// <param name="rewinder">The text reader to read double matrix from.</param>
        /// <returns>Transition instance loaded.</returns>
        private static double[,] ReadTransition(RewindableTextReader rewinder)
        {
            Helper.ThrowIfNull(rewinder);
            int size = ParseTagValue(rewinder.ReadLine(), "<TRANSP>");

            double[,] values = ParseDoubleMatrix(
                rewinder.ReadLines(new string[] { "<", MacroName.Indicator }, false).ToArray(), size);

            return values;
        }

        /// <summary>
        /// Reads double matrix from the text reader with the expected tag.
        /// </summary>
        /// <param name="rewinder">Text reader to read array from.</param>
        /// <param name="tag">Expected tag to start in the stream.</param>
        /// <returns>Double matrix from the text reader.</returns>
        private static double[] ReadTagArray(RewindableTextReader rewinder, string tag)
        {
            Helper.ThrowIfNull(rewinder);
            Helper.ThrowIfNull(tag);

            string line = rewinder.ReadLine();
            int size = ParseTagValue(line, tag);

            double[] values = new double[0];
            if (size > 0)
            {
                values = ParseDoubleArray(rewinder.ReadLine());
                if (values.Length != size)
                {
                    throw new InvalidDataException(Helper.NeutralFormat("Mismatched value is found, " +
                        "declared as [{0}], but as [{1}].", size, values.Length));
                }
            }

            return values;
        }

        /// <summary>
        /// Parses string values from the line of text with the expected tag.
        /// </summary>
        /// <param name="line">The line of text to parse.</param>
        /// <param name="tag">Expected tag to start in the line.</param>
        /// <returns>String values parsed from the line.</returns>
        private static string[] ParseTagValues(string line, string tag)
        {
            if (string.IsNullOrEmpty(line) || !line.StartsWith(tag))
            {
                throw new InvalidDataException(Helper.NeutralFormat("Invalid line is found as [{0}], " +
                    "which is expected to start with [{1}].", line, tag));
            }

            string[] items = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            return items.Skip(1).ToArray();
        }

        /// <summary>
        /// Parses a integer value from the line of text with the expected tag.
        /// </summary>
        /// <param name="line">The line of text to parse.</param>
        /// <param name="tag">Expected tag to start in the line.</param>
        /// <returns>Integer value parsed from the line.</returns>
        private static int ParseTagValue(string line, string tag)
        {
            Helper.ThrowIfNull(line);
            Helper.ThrowIfNull(tag);

            string[] items = ParseTagValues(line, tag);
            if (items.Length != 1)
            {
                throw new InvalidDataException(Helper.NeutralFormat("Invalid line is found as [{0}]." +
                    "Should be ('{1}' <value>).", line, tag));
            }

            int size = int.Parse(items[0], CultureInfo.InvariantCulture);

            return size;
        }

        /// <summary>
        /// Parses int array (with size at the first element) from list of integers.
        /// </summary>
        /// <param name="values">List of integers to parse for int array.</param>
        /// <returns>Parsed int array.</returns>
        private static int[] ParseSizedArray(int[] values)
        {
            Helper.ThrowIfNull(values);

            if ((values.Length == 0) || values[0] != (values.Length - 1))
            {
                throw new InvalidDataException(Helper.NeutralFormat(
                    "The integer array should be as <count> <value>{<count>}, but it is [{0}]",
                    values.Concatenate(" ")));
            }

            // Skips the first element, which is the count of elements in the int array.
            return values.Skip(1).ToArray();
        }

        /// <summary>
        /// Parses int array from list of strings.
        /// </summary>
        /// <param name="values">List of strings to parse for int array.</param>
        /// <returns>Parsed int array.</returns>
        private static int[] ParseIntArray(IEnumerable<string> values)
        {
            Helper.ThrowIfNull(values);
            return values.Select(v => int.Parse(v, CultureInfo.InvariantCulture)).ToArray();
        }

        /// <summary>
        /// Parses double matrix from list of strings.
        /// </summary>
        /// <param name="lines">List of strings to parse for double matrix.</param>
        /// <param name="size">The expected dimension of the matrix.</param>
        /// <returns>Parsed double matrix.</returns>
        private static double[,] ParseDoubleMatrix(string[] lines, int size)
        {
            Helper.ThrowIfNull(lines);

            if (lines.Length != size)
            {
                throw new InvalidDataException(Helper.NeutralFormat(
                    "It is expected to be [{0}] lines of data, but it is [{1}].", size, lines.Length));
            }

            double[,] values = new double[size, size];
            for (int i = 0; i < size; i++)
            {
                double[] items = ParseDoubleArray(lines[i]);
                if (items.Length != size)
                {
                    throw new InvalidDataException(Helper.NeutralFormat(
                        "It is expected to be [{0}] items of data, but it is [{1}].", size, items.Length));
                }

                for (int j = 0; j < size; j++)
                {
                    values[i, j] = items[j];
                }
            }

            return values;
        }

        /// <summary>
        /// Parses double array from list of strings.
        /// </summary>
        /// <param name="line">A string to parse for double array.</param>
        /// <returns>Parsed double array.</returns>
        private static double[] ParseDoubleArray(string line)
        {
            Helper.ThrowIfNull(line);
            string[] items = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return items.Select(i => double.Parse(i, CultureInfo.InvariantCulture)).ToArray();
        }

        /// <summary>
        /// Ensures that two lines equal with each other, otherwise InvalidDataException will be thrown.
        /// </summary>
        /// <param name="left">The left string instance to test.</param>
        /// <param name="right">The right string instance to test.</param>
        private static void Ensure(string left, string right)
        {
            if (left != right)
            {
                throw new InvalidDataException(Helper.NeutralFormat(
                    "Line [{0}] is not matched as what is expected as [{1}]", left, right));
            }
        }

        #endregion

        #region Private operations

        /// <summary>
        /// Loads macros from rewindable reader instance.
        /// </summary>
        /// <param name="rewinder">The instance to read data from.</param>
        private void Load(RewindableTextReader rewinder)
        {
            Helper.ThrowIfNull(rewinder);

            string line = null;
            while ((line = rewinder.ReadLine()) != null)
            {
                MacroName macro = MacroName.Create(line);
                switch (macro.Symbol)
                {
                    case "o":
                        LoadGlobalOptions(rewinder);
                        break;
                    case "v":
                        VarFloor var = new VarFloor()
                        {
                            Macro = macro,
                            Variance = ReadTagArray(rewinder, "<VARIANCE>"),
                        };

                        VarFloors.Add(var.Macro.Name, var);
                        break;
                    case "t":
                        Transition trans = new Transition()
                        {
                            Macro = macro,
                            Matrix = ReadTransition(rewinder),
                        };

                        Transitions.Add(trans.Macro.Name, trans);
                        break;
                    case "s":
                        HmmStream stream = ReadStream(rewinder);
                        stream.Name = macro.Name;
                        Streams.Add(macro.Name, stream);
                        break;
                    case "h":
                        HmmModel model = ReadModel(rewinder, Streams, Transitions);
                        model.Macro = macro;
                        Models.Add(macro.Name, model);
                        break;
                    default:
                        throw new InvalidDataException(Helper.NeutralFormat(
                            "Macro name [{0}] is not supported in line [{1}].", macro.Name, macro.Name));
                }
            }
        }

        /// <summary>
        /// Loads global options information from text reader instance.
        /// </summary>
        /// <param name="rewinder">The rewindable text reader to load data from.</param>
        private void LoadGlobalOptions(RewindableTextReader rewinder)
        {
            Helper.ThrowIfNull(rewinder);

            foreach (string line in rewinder.ReadLines(new string[] { MacroName.Indicator }, false))
            {
                string[] items = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (items.Length <= 1)
                {
                    throw new InvalidDataException(Helper.NeutralFormat(
                        "The information in global options should be with (<tag> <value>+), but it is [{0}]", line));
                }

                switch (items[0])
                {
                    case "<STREAMINFO>":
                        StreamInfo = ParseSizedArray(ParseIntArray(items.Skip(1)));
                        break;
                    case "<MSDINFO>":
                        MsdInfo = ParseSizedArray(ParseIntArray(items.Skip(1)));
                        break;
                    case "<VECSIZE>":
                        LoadVecSize(items.Skip(1));
                        break;
                    default:
                        throw new InvalidDataException(Helper.NeutralFormat(
                            "The line [{0}] of global options is not supported.", line));
                }
            }
        }

        /// <summary>
        /// Loads vector size information from a list of strings.
        /// </summary>
        /// <param name="values">Vector information in string list to parse.</param>
        private void LoadVecSize(IEnumerable<string> values)
        {
            Helper.ThrowIfNull(values);
            string block = values.Concatenate(" ");
            string[] items = block.Split(new char[] { ' ', '<', '>' }, StringSplitOptions.RemoveEmptyEntries);
            VecSize = int.Parse(items[0], CultureInfo.InvariantCulture);
            VecInfo = items.Skip(1).ToArray();
        }

        /// <summary>
        /// Resets current instance to the initialization status.
        /// </summary>
        private void Reset()
        {
            StreamInfo = null;
            MsdInfo = null;
            VecSize = 0;
            VecInfo = null;
            VarFloors = new Dictionary<string, VarFloor>();
            Transitions = new Dictionary<string, Transition>();
            Streams = new Dictionary<string, HmmStream>();
            Models = new Dictionary<string, HmmModel>();
        }

        #endregion
    }
}