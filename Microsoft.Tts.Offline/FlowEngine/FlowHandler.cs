//----------------------------------------------------------------------------
// <copyright file="FlowHandler.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module defines the FlowHandler class and some of helper
//   classes used to assemble the pipeline of training.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.FlowEngine
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// This abstract class is used to organize the pipeline of the FlowHandler.
    /// Every step in FlowHandler will be extended from this class.
    /// </summary>
    public abstract class FlowHandler : Observable<LoggerEventArgs>
    {
        #region Fields

        /// <summary>
        /// Warning file name of each hanlder.
        /// </summary>
        public const string WarningFileName = "warning.txt";

        /// <summary>
        /// The parallel computation platform. 
        /// </summary>
        [CLSCompliantAttribute(false)]
        protected static ParallelComputation paraComputePlatform = null;

        /// <summary>
        /// The ILogger interface to log the runtime information.
        /// </summary>
        private ILogger logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes static members of the FlowHandler class.
        /// Set the default maximum thread number as the processor number.
        /// </summary>
        static FlowHandler()
        {
            MaxThreads = Environment.ProcessorCount;
        }

        /// <summary>
        /// Initializes a new instance of the FlowHandler class.
        /// </summary>
        /// <param name="name">The given name of this handler.</param>
        protected FlowHandler(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(name);
            }

            Name = name;
            RuntimeOutputs = new Dictionary<string, object>();
            StepStatusRecorder = null;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the public computation platform.
        /// </summary>
        public static ParallelComputation ParaComputePlatform
        {
            get
            {
                return paraComputePlatform;
            }

            private set
            {
            }
        }

        /// <summary>
        /// Gets or sets the maximum thread number.
        /// </summary>
        public static int MaxThreads
        {
            get
            {
                return ManagedThreadPool.MaxThreads;
            }

            set
            {
                ManagedThreadPool.MaxThreads = value;
            }
        }

        /// <summary>
        /// Gets or sets the working directory for this object.
        /// Input of this handler.
        /// </summary>
        public string InWorkingDirectory { get; set; }

        /// <summary>
        /// Gets the runtime input, this is used for letting module to abtain parameter
        /// At runtime, not only from configuration file.
        /// </summary>
        public Dictionary<string, object> RuntimeOutputs { get; private set; }

        /// <summary>
        /// Gets or sets the ILogger interface.
        /// Input of this handler.
        /// </summary>
        public ILogger Logger
        {
            get
            {
                return logger;
            }

            set
            {
                if (logger != null)
                {
                    Observer -= logger.Update;
                }

                logger = value;
                if (logger != null)
                {
                    Observer += logger.Update;
                }
            }
        }

        /// <summary>
        /// Gets or sets the warning writer.
        /// </summary>
        public StreamWriter WarningWriter { get; set; }

        /// <summary>
        /// Gets the name of this handler.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets or sets the status recorder of this handler.
        /// </summary>
        public StatusRecorder StepStatusRecorder { get; set; }

        /// <summary>
        /// Gets the directory to place the intermediate data.
        /// </summary>
        public string IntermediateDataDirectory
        {
            get
            {
                return Path.Combine(ResultDirectory, "Intermediate");
            }
        }

        /// <summary>
        /// Gets the directory to place the result data.
        /// </summary>
        public string ResultDirectory
        {
            get
            {
                return Path.Combine(InWorkingDirectory, Name);
            }
        }

        /// <summary>
        /// Gets or sets the description of this handler.
        /// </summary>
        public string Description { get; protected set; }

        /// <summary>
        /// Gets the warning file path.
        /// </summary>
        public string WarningFilePath
        {
            get
            {
                return Path.Combine(ResultDirectory, WarningFileName);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Return the paltform type.
        /// </summary>
        /// <returns>Computation platform.</returns>
        public static ComputationPlatform GetParaPlatformType()
        {
            if (paraComputePlatform as MultiThreadParallelComputation != null)
            {
                return ComputationPlatform.MultiThreadPlatform;
            }
            else if (paraComputePlatform as SingleMachineComputation != null)
            {
                return ComputationPlatform.SingleMachinePlatform;
            }
            else if (paraComputePlatform as HPCParallelComputaion != null)
            {
                return ComputationPlatform.HPCPlatform;
            }
            else
            {
                return ComputationPlatform.Unknown;
            }
        }

        /// <summary>
        /// Delete unnecessary file asynchronously.
        /// </summary>
        /// <param name="dataPath">The  full path of target file.</param>
        /// <param name="logger">The logger.</param>
        public static void DeleteIntermediateDataAsyn(string dataPath, ILogger logger)
        {
            if (!string.IsNullOrEmpty(dataPath) && File.Exists(dataPath))
            {
                DataDeleteArgument argument = new DataDeleteArgument
                {
                    DataFullPath = dataPath,
                    Description = Helper.NeutralFormat("Delete Intermediate Data: {0}", dataPath),
                    Logger = logger ?? new NullLogger(),
                };

                Thread t = new Thread(new ParameterizedThreadStart(DeleteData));
                t.Start(argument);
            }
        }

        /// <summary>
        /// The method to perform parallel computing.
        /// </summary>
        /// <param name="parallelParameter">The instance of ParallelParameter, which is a parallel computation unit.</param>
        public static void ParallelComputing(ParallelParameter parallelParameter)
        {
            ParallelComputing(new List<ParallelParameter> { parallelParameter });
        }

        /// <summary>
        /// The method to perform parallel computing.
        /// </summary>
        /// <param name="parallelParameters">The collection for ParallelParameter, which is a parallel computation unit.</param>
        public static void ParallelComputing(IEnumerable<ParallelParameter> parallelParameters)
        {
            if (parallelParameters.Count() == 0)
            {
                throw new ArgumentException("Empty parameters for parallel!");
            }
            else if (parallelParameters.Count() == 1)
            {
                foreach (ParallelParameter parallelParameter in parallelParameters)
                {
                    parallelParameter.Invoke(parallelParameter);
                }
            }
            else
            {
                foreach (ParallelParameter parallelParameter in parallelParameters)
                {
                    ManagedThreadPool.QueueUserWorkItem(parallelParameter.Invoke, parallelParameter);
                }

                ManagedThreadPool.WaitForDone();
            }

            parallelParameters.Where(p => p.InnerException != null).ForEach(
                p => System.Diagnostics.Trace.WriteLine(Helper.BuildExceptionMessage(p.InnerException)));

            if (parallelParameters.Where(p => p.InnerException != null).Count() > 0)
            {
                var exps = parallelParameters.Where(p => p.InnerException != null)
                    .Select(p => p.InnerException);

                ExceptionCollection es =
                    new ExceptionCollection("Exceptions are found inside some of the paralleled threads:" +
                        exps.Select(e => Helper.BuildExceptionMessage(e)).Concatenate(Environment.NewLine));
                es.Exceptions.AddRange(exps);

                throw es;
            }
        }

        /// <summary>
        /// The method PrepareRuntimeInput() will be called to perform fill the runtime parameter of this handler.
        /// </summary>
        public virtual void PrepareRuntimeInputs()
        {
        }

        /// <summary>
        /// The main entry to execute this handler.
        /// </summary>
        /// <param name="enable">Indicator to this handler is enabled or disabled. If this handler is
        /// Disabled, the abstract Execute() methods will not be invoked. However, the abstract ValidateResults(bool enable)
        /// methods will be invoked to setup the result of this handler.</param>
        /// <param name="keepIntermediateData">Indicator to whether keep the intermediate data.</param>
        public void Execute(bool enable, bool keepIntermediateData)
        {
            // Get the exact insert position of current module in the status file.
            EnsureCurrentStepExist(Name);

            if (enable)
            {
                // Validates the arguments.
                ValidateArguments();

                // Creates the working directory.
                if (string.IsNullOrEmpty(InWorkingDirectory))
                {
                    throw new InvalidDataException("Working directory folder is null or empty");
                }

                Helper.EnsureFolderExist(ResultDirectory);
                Helper.EnsureFolderExist(IntermediateDataDirectory);

                // The stream writer to write warnings.
                WarningWriter = new StreamWriter(WarningFilePath);

                // Executes this handler.
                try
                {
                    DateTime startTime = DateTime.Now;
                    Log("{0}: start \"{1}\": {2}.", startTime.ToString(), Name, Description);

                    Execute();

                    DateTime endTime = DateTime.Now;
                    Log("{0}: \"{1}\": {2} finished. Elapsed time [{3}].{4}",
                        endTime.ToString(),
                        Name,
                        Description,
                        (endTime - startTime).ToString(),
                        Environment.NewLine);
                }
                finally
                {
                    // Close the writer and delete the warning file if no warnings.
                    WarningWriter.Close();
                    if (new FileInfo(WarningFilePath).Length == 0)
                    {
                        Helper.SafeDelete(WarningFilePath);
                    }

                    // Removes the empty directory to satisfy the user.
                    if (!keepIntermediateData || Helper.IsEmptyDirectory(IntermediateDataDirectory))
                    {
                        Helper.SafeDelete(IntermediateDataDirectory);
                    }
                }

                if (Helper.IsEmptyDirectory(ResultDirectory))
                {
                    Helper.SafeDelete(ResultDirectory);
                }

                RecordCurrentStep(Name);
            }

            ValidateResults(enable);
        }

        /// <summary>
        /// Write the given string into all registered Observer.
        /// </summary>
        /// <param name="format">The format of the string.</param>
        /// <param name="arg">The argument of the string.</param>
        public void Log(string format, params object[] arg)
        {
            LoggerEventArgs eventArg = new LoggerEventArgs { Format = format + Environment.NewLine, Args = arg };
            NotifyObservers(eventArg);
        }

        /// <summary>
        /// Write the given string into all registered Observer with time info.
        /// </summary>
        /// <param name="format">The format of the string.</param>
        /// <param name="arg">The argument of the string.</param>
        public void LogWithTime(string format, params object[] arg)
        {
            var time = "@" + DateTime.Now.ToString(CultureInfo.InvariantCulture);

            LoggerEventArgs eventArg = new LoggerEventArgs { Format = format + time + Environment.NewLine, Args = arg };
            NotifyObservers(eventArg);
        }

        /// <summary>
        /// Get the output under the result directory.
        /// </summary>
        /// <param name="outPath">Out path.</param>
        /// <returns>Path under result directory.</returns>
        internal string GetOutPathUnderResultDirectory(string outPath)
        {
            if (!Path.IsPathRooted(outPath) && !string.IsNullOrEmpty(ResultDirectory))
            {
                outPath = Path.Combine(ResultDirectory, outPath);
            }

            return outPath;
        }

        /// <summary>
        /// Throws FileNotFoundException() when the given file does not exist.
        /// </summary>
        /// <param name="file">The given file to be validated.</param>
        /// <param name="exceptionFormat">The format for the message in the thrown exception when dependency checking is failed.</param>
        /// <param name="exceptionArg">The argument for the message in the thrown exception when dependency checking is failed.</param>
        protected static void EnsureFileExist(string file, string exceptionFormat, params object[] exceptionArg)
        {
            if (!File.Exists(file))
            {
                if (exceptionArg.Length != 0)
                {
                    throw new FileNotFoundException(Helper.NeutralFormat(exceptionFormat, exceptionArg));
                }
                else
                {
                    throw new FileNotFoundException(exceptionFormat, file);
                }
            }
        }

        /// <summary>
        /// Throws DirectoryNotFoundException() when the given directory does not exist.
        /// </summary>
        /// <param name="directory">The given directory to be validated.</param>
        /// <param name="exceptionFormat">The format for the message in the thrown exception when dependency checking is failed.</param>
        /// <param name="exceptionArg">The argument for the message in the thrown exception when dependency checking is failed.</param>
        protected static void EnsureDirectoryExist(string directory, string exceptionFormat, params object[] exceptionArg)
        {
            if (!Directory.Exists(directory))
            {
                throw new DirectoryNotFoundException(Helper.NeutralFormat(exceptionFormat, exceptionArg));
            }
        }

        /// <summary>
        /// Throws ArgumentOutOfRangeException() when the given value is out of the range [minValue, maxValue].
        /// </summary>
        /// <typeparam name="T">The type of the given value.</typeparam>
        /// <param name="value">The given value to be validated.</param>
        /// <param name="minValue">The minimum boundary value.</param>
        /// <param name="maxValue">The maximum boundary value.</param>
        /// <param name="exceptionFormat">The format for the message in the thrown exception when dependency checking is failed.</param>
        /// <param name="exceptionArg">The argument for the message in the thrown exception when dependency checking is failed.</param>
        protected static void EnsureRange<T>(T value, T minValue, T maxValue, string exceptionFormat, params object[] exceptionArg)
            where T : IComparable<T>
        {
            if (value.CompareTo(maxValue) > 0 || value.CompareTo(minValue) < 0)
            {
                throw new ArgumentOutOfRangeException(Helper.NeutralFormat(exceptionFormat, exceptionArg));
            }
        }

        /// <summary>
        /// Throws ArgumentException() when the given value is not in valid set.
        /// </summary>
        /// <typeparam name="T">The type of the given value.</typeparam>
        /// <param name="value">The given value to be validated.</param>
        /// <param name="fullSet">The full set of valid value.</param>
        /// <param name="exceptionFormat">The format for the message in the thrown exception when dependency checking is failed.</param>
        /// <param name="exceptionArg">The argument for the message in the thrown exception when dependency checking is failed.</param>
        protected static void EnsureEnumable<T>(T value, ICollection<T> fullSet, string exceptionFormat, params object[] exceptionArg)
        {
            if (!fullSet.Contains(value))
            {
                throw new ArgumentException(Helper.NeutralFormat(exceptionFormat, exceptionArg));
            }
        }

        /// <summary>
        /// Throws ArgumentException() when the given value is not a valid Enum value.
        /// </summary>
        /// <param name="type">The type of the Enum.</param>
        /// <param name="value">The given string value.</param>
        /// <param name="exceptionFormat">The format for the message in the thrown exception when dependency checking is failed.</param>
        /// <param name="exceptionArg">The argument for the message in the thrown exception when dependency checking is failed.</param>
        protected static void EnsureEnum(Type type, string value, string exceptionFormat, params object[] exceptionArg)
        {
            if (!type.IsEnum)
            {
                throw new ArgumentException("Not an Enum type");
            }

            try
            {
                Enum.Parse(type, value, true);
            }
            catch (ArgumentException e)
            {
                throw new ArgumentException(Helper.NeutralFormat(exceptionFormat, exceptionArg), e);
            }
        }

        /// <summary>
        /// Throws ArgumentException() when the given value is null or empty.
        /// </summary>
        /// <param name="str">The given string to test.</param>
        /// <param name="exceptionFormat">The format for the message in the thrown exception when dependency checking is failed.</param>
        /// <param name="exceptionArg">The argument for the message in the thrown exception when dependency checking is failed.</param>
        protected static void EnsureNotNull(string str, string exceptionFormat, params object[] exceptionArg)
        {
            if (string.IsNullOrEmpty(str))
            {
                throw new ArgumentException(Helper.NeutralFormat(exceptionFormat, exceptionArg));
            }
        }

        /// <summary>
        /// Throws ArgumentException() when the given value is null.
        /// </summary>
        /// <param name="obj">The given value to test.</param>
        /// <param name="exceptionFormat">The format for the message in the thrown exception when dependency checking is failed.</param>
        /// <param name="exceptionArg">The argument for the message in the thrown exception when dependency checking is failed.</param>
        protected static void EnsureNotNull(object obj, string exceptionFormat, params object[] exceptionArg)
        {
            if (obj == null)
            {
                throw new ArgumentException(Helper.NeutralFormat(exceptionFormat, exceptionArg));
            }
        }

        #region Abstract Methods

        /// <summary>
        /// The abstract method ValidateArguments() will be called to perform the validation
        /// For the inputs of this handler.
        /// This method won't be called if this handler is disabled.
        /// </summary>
        protected abstract void ValidateArguments();

        /// <summary>
        /// The abstract method Execute() will be called to perform the action of this handler.
        /// This method won't be called if this handler is disabled.
        /// </summary>
        protected abstract void Execute();

        /// <summary>
        /// The abstract method ValidateResults(bool enable) will be used to fill result generated by this handler
        /// After Execute().
        /// This method will be called whenever this handler is disabled or not.
        /// </summary>
        /// <param name="enable">Indicator to whether flow is enable.</param>
        protected abstract void ValidateResults(bool enable);

        #endregion

        /// <summary>
        /// Test whether the step exists in the finished steps array of StatusRecorder.
        /// </summary>
        /// <param name="currentStep"> The step name need to check. .</param>
        /// <returns> Whether the step exists or not. .</returns>
        protected bool EnsureCurrentStepExist(string currentStep)
        {
            bool ret = false;

            if (StepStatusRecorder != null)
            {
                ret = StepStatusRecorder.CheckStepStatus(currentStep);
            }

            return ret;
        }

        /// <summary>
        /// If the StepStatusRecorder is not null, recorde the currentStep,
        /// Otherwise not.
        /// </summary>
        /// <param name="currentStep"> The step name need to record. .</param>
        protected void RecordCurrentStep(string currentStep)
        {
            if (StepStatusRecorder != null)
            {
                StepStatusRecorder.RecordCurrentStatus(currentStep);
            }
        }

        private static void DeleteData(object arg)
        {
            DataDeleteArgument deleteArg = arg as DataDeleteArgument;
            if (deleteArg == null)
            {
                throw new ArgumentException("Invalid argument in DeleteData method");
            }

            using (DelayedLogger fileWriter = new DelayedLogger(deleteArg.Logger))
            {
                string log = deleteArg.Description;

                if (Directory.Exists(deleteArg.DataFullPath))
                {
                    DeleteOnLocal(deleteArg.DataFullPath);
                }
                else
                {
                    if (File.Exists(deleteArg.DataFullPath))
                    {
                        RemoveReadOnlyAttr(deleteArg.DataFullPath);
                        File.Delete(deleteArg.DataFullPath);
                    }
                }

                Console.WriteLine("{0}", log);
                fileWriter.Writer.WriteLine("{0}", log);
            }
        }

        /// <summary>
        /// Delete of a local directory, enhancement of Directory.Delete.
        /// Resursively.
        /// </summary>
        /// <param name="pathname">Path name.</param>
        private static void DeleteOnLocal(string pathname)
        {
            if (Directory.Exists(pathname))
            {
                string[] fileArr = Directory.GetFiles(pathname, "*", SearchOption.AllDirectories);
                foreach (var fname in fileArr)
                {
                    RemoveReadOnlyAttr(fname);
                }

                Directory.Delete(pathname, true);
            }
        }

        /// <summary>
        /// Remove the read only attribute of the files.
        /// </summary>
        /// <param name="fname">The fname.</param>
        private static void RemoveReadOnlyAttr(string fname)
        {
            FileAttributes attributes = File.GetAttributes(fname);
            if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            {
                // make the file writable
                attributes = RemoveAttribute(attributes, FileAttributes.ReadOnly);
                File.SetAttributes(fname, attributes);
            }
        }

        private static FileAttributes RemoveAttribute(FileAttributes attributes, FileAttributes attributesToRemove)
        {
            return attributes & ~attributesToRemove;
        }

        #endregion
    }

    internal class DataDeleteArgument
    {
        public string DataFullPath { get; set; }

        public ILogger Logger { get; set; }

        public string Description { get; set; }

        public override string ToString()
        {
            return DataFullPath;
        }
    }
}