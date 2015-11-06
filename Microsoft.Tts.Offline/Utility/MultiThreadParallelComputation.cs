//----------------------------------------------------------------------------
// <copyright file="MultiThreadParallelComputation.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This is the class to invoke multi thread parallel compuation
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;

    /// <summary>
    /// Delegate reduce work.
    /// </summary>
    /// <param name="args">Arguements.</param>
    public delegate void ReduceWork(string[] args);

    /// <summary>
    /// The class of MultiThreadParallelComputation.
    /// </summary>
    public class MultiThreadParallelComputation : ParallelComputation
    {
        #region field
        private IEnumerable<ParallelParameter> parallelParameters = null;
        private int maxThreads;
        private string[] arguments = null;
        private ReduceWork singleReduce = null;
        #endregion

        #region constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiThreadParallelComputation" /> class, the constuctor for MultiThreadParallelComputation class.
        /// </summary>
        public MultiThreadParallelComputation()
        {
        }

        #endregion

        #region property

        /// <summary>
        /// Gets or sets single Reduce work.
        /// </summary>
        public ReduceWork SingleReduce
        {
            get
            {
                return singleReduce;
            }

            set
            {
                if (value != null)
                {
                    singleReduce = value;
                }
                else
                {
                    throw new ArgumentException("The reduce method should not be null");
                }
            }
        }

        /// <summary>
        /// Sets parallel parameters.
        /// </summary>
        public IEnumerable<ParallelParameter> ParallelParameters
        {
            set
            {
                if (value != null && value.Count() != 0)
                {
                    parallelParameters = value;
                }
                else
                {
                    throw new ArgumentException("Illegal input of parameters");
                }
            }
        }

        /// <summary>
        /// Gets or sets passed in reduce arguments.
        /// </summary>
        public string[] ReduceArguments
        {
            get
            {
                return arguments;
            }

            set
            {
                if (value != null && value.Count() > 0)
                {
                    arguments = value;
                }
                else
                {
                    throw new ArgumentException("Illegal Input of Parameters");
                }
            }
        }

        /// <summary>
        /// Gets or sets MaxThreads.
        /// </summary>
        public int MaxThreads
        {
            get
            {
                return maxThreads;
            }

            set
            {
                maxThreads = value;
            }
        }

        #endregion

        /// <summary>
        /// The Initialize() methods to initalize the necessary configuration.
        /// </summary>
        /// <returns>Bool.</returns>
        protected override bool Initialize()
        {
            if (maxThreads == 0)
            {
                maxThreads = Environment.ProcessorCount;
            }

            ManagedThreadPool.MaxThreads = maxThreads;

            if (parallelParameters == null || parallelParameters.Count() == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// The BroadCast() methods to initalize the necessary configuration.
        /// </summary>
        /// <returns>Bool.</returns>
        protected override bool BroadCast()
        {
            bool isSuccess = true;
            ParallelComputing();
            return isSuccess;
        }

        /// <summary>
        /// The Reduce() methods to initalize the necessary configuration.
        /// </summary>
        /// <returns>Bool.</returns>
        protected override bool Reduce()
        {
            if (singleReduce != null && arguments != null)
            {
                singleReduce(arguments);
            }

            return true;
        }

        /// <summary>
        /// The ValidateResult() methods to initalize the necessary configuration.
        /// </summary>
        /// <returns>Bool.</returns>
        protected override bool ValidateResult()
        {
            // Finish it elegantly
            singleReduce = null;
            arguments = null;
            parallelParameters = null;
            return true;
        }

        /// <summary>
        /// The method to perform parallel computing.
        /// </summary>
        private void ParallelComputing()
        {
            if (parallelParameters.Count() == 1)
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
    }

    /// <summary>
    /// The all content write to the logger will be delayed to until the object is disposing.
    /// This is a helper class to write logs from multi-thread into a same logger.
    /// </summary>
    public class DelayedLogger : IDisposable
    {
        #region Fields

        /// <summary>
        /// The ILogger.interface.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// A buffer to hold the log temporary.
        /// </summary>
        private StringBuilder _buffer;

        /// <summary>
        /// The logger writer.
        /// </summary>
        private TextWriter _writer;

        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the DelayedLogger class.
        /// </summary>
        /// <param name="logger">The ILogger interface.</param>
        public DelayedLogger(ILogger logger)
        {
            _logger = logger;
            _buffer = new StringBuilder();
            _writer = new StringWriter(_buffer, CultureInfo.InvariantCulture);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the TextWriter, which content will be written into the logger.
        /// </summary>
        public TextWriter Writer
        {
            get { return _writer; }
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// The Dispose() method for IDisposable interface. Here, the logged data will be written into the log file.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// The Dispose() method for IDisposable interface. Here, the logged data will be written into the log file.
        /// </summary>
        /// <param name="disposing">True.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _writer.Close();
                _writer = null;
                string log = _buffer.ToString();
                _buffer = null;
                if (_logger != null)
                {
                    lock (_logger)
                    {
                        _logger.Log("{0}", log);
                    }
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// An abstract class to represent the parameter for parallel computing.
    /// </summary>
    public abstract class ParallelParameter
    {
        #region Properties

        /// <summary>
        /// Gets or sets the description for log for the parallel parameter.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the ILogger interface for the parallel parameter.
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Gets or sets the ILogger interface for the parallel parameter.
        /// </summary>
        public ILogger FileLogger { get; set; }

        /// <summary>
        /// Gets or sets the exception happened inner the thread for the parallel parameter.
        /// </summary>
        public Exception InnerException { get; protected set; }

        #endregion

        #region Methods

        /// <summary>
        /// The abstract Invoke() methods to invoke the corresponding method or external command.
        /// </summary>
        /// <param name="state">The argument for Invoke() methods.</param>
        public abstract void Invoke(object state);

        #endregion
    }

    /// <summary>
    /// The external command info for parallel computing.
    /// </summary>
    public class ParallelCommand : ParallelParameter
    {
        #region Properties

        /// <summary>
        /// Gets or sets the executable file name for the external command.
        /// </summary>
        public string ExecutableFile { get; set; }

        /// <summary>
        /// Gets or sets the ILogger interface for the parallel parameter.
        /// </summary>
        public ILogger ResultFileLogger { get; set; }

        /// <summary>
        /// Gets or sets the argument for the external command.
        /// </summary>
        public string Argument { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Converts the instance into string.
        /// </summary>
        /// <returns>String to represent the instance.</returns>
        public override string ToString()
        {
            return Helper.NeutralFormat("{0} {1}", ExecutableFile, Argument);
        }

        /// <summary>
        /// The Invoke() methods to invoke the corresponding external command.
        /// </summary>
        /// <param name="state">The argument for Invoke() methods, will be an instance of ParallelCommand.</param>
        public override void Invoke(object state)
        {
            try
            {
                ParallelCommand command = state as ParallelCommand;
                if (command == null)
                {
                    throw new ArgumentException("Invalid argument in ParallelCommand.Invoke");
                }

                // delayedLogger is for standard output and standard error, redirectedLogger is for standard output.
                // If this command is redirected to a result file, redirectedLogger shoule be created. Otherwise, all logs will be written into delayedLogger.
                using (DelayedLogger delayedLogger = new DelayedLogger(command.Logger))
                    using (DelayedLogger redirectedLogger = new DelayedLogger(command.ResultFileLogger))
                {
                    string log = Helper.NeutralFormat(
                        "{0} starts at time {1}{2}{3} {4}",
                        command.Description,
                        DateTime.Now.ToString(),
                        Environment.NewLine,
                        command.ExecutableFile,
                        command.Argument);
                    Console.WriteLine("{0}", log);
                    delayedLogger.Writer.WriteLine("{0}", log);

                    int returnCode = CommandLine.RunCommand(
                        command.ExecutableFile,
                        command.Argument,
                        Environment.CurrentDirectory,
                            (command.ResultFileLogger == null) ? delayedLogger.Writer : redirectedLogger.Writer,
                        delayedLogger.Writer,
                        null);

                    if (returnCode != 0)
                    {
                        string message = Helper.NeutralFormat("The return code of the following command is {0}, " +
                            "not-zero, indicating error in the target command. {1} {2}",
                            returnCode, Environment.NewLine, command.ToString());

                        throw new InvalidOperationException(message);
                    }

                    log = Helper.NeutralFormat("{0} ends at time {1}", command.Description, DateTime.Now.ToString());
                    Console.WriteLine("{0}", log);
                    delayedLogger.Writer.WriteLine("{0}", log);
                }
            }
            catch (System.Exception ex)
            {
                // Caches the instance of the exception.
                InnerException = ex;
            }
        }

        #endregion
    }

    /// <summary>
    /// The method info for parallel computing.
    /// </summary>
    /// <typeparam name="T">The argument for call back method.</typeparam>
    public class ParallelMethod<T> : ParallelParameter
    {
        #region Delegates

        /// <summary>
        /// The delegate for method used to perform parallel computing.
        /// </summary>
        /// <typeparam name="TArgType">The argument for call back method.</typeparam>
        /// <param name="argument">The corresponding argument.</param>
        /// <param name="logWriter">The TextWriter for log.</param>
        public delegate void ParallelCallBackMethod<TArgType>(TArgType argument, TextWriter logWriter);

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the method for parallel computing.
        /// </summary>
        public ParallelCallBackMethod<T> CallBackMethod { get; set; }

        /// <summary>
        /// Gets or sets the argument will be passed to the method.
        /// </summary>
        public T Argument { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// The Invoke() methods to invoke the corresponding method.
        /// </summary>
        /// <param name="state">The argument for Invoke() methods, will be an instance of ParallelMethod.</param>
        public override void Invoke(object state)
        {
            ParallelMethod<T> parallelMethod = state as ParallelMethod<T>;
            if (parallelMethod == null)
            {
                throw new ArgumentException("Invalid argugment in ParallelMethod.Invoke");
            }

            using (DelayedLogger fileWriter = new DelayedLogger(parallelMethod.Logger))
            {
                string log = Helper.NeutralFormat(
                    "{0} starts at time {1}{2}{3} {4}",
                    parallelMethod.Description,
                    DateTime.Now.ToString(),
                    Environment.NewLine,
                    parallelMethod.CallBackMethod.Method.Name,
                    parallelMethod.Argument.ToString());
                Console.WriteLine("{0}", log);
                fileWriter.Writer.WriteLine("{0}", log);

                parallelMethod.CallBackMethod(parallelMethod.Argument, fileWriter.Writer);

                log = Helper.NeutralFormat("{0} ends at time {1}", parallelMethod.Description, DateTime.Now.ToString());
                Console.WriteLine("{0}", log);
                fileWriter.Writer.WriteLine("{0}", log);
            }
        }

        #endregion
    }
}
