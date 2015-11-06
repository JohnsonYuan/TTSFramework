//----------------------------------------------------------------------------
// <copyright file="ComputingAssist.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements Computing Assist
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Utility
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;
    using System.Threading;

    /// <summary>
    /// Callback function for computing assistance.
    /// </summary>
    /// <typeparam name="TKey">Type of the key.</typeparam>
    /// <typeparam name="TValue">Type of the value, this should be pair with key value.</typeparam>
    /// <typeparam name="TReturn">Type of return value.</typeparam>
    /// <param name="key">The key of the job item.</param>
    /// <param name="value">The value of the data to process.</param>
    /// <returns>Processing result of this item.</returns>
    public delegate TReturn ComputingCallback<TKey, TValue, TReturn>(TKey key, TValue value);

    /// <summary>
    /// Computing model control the parallel model.
    /// </summary>
    public enum ComputingModel
    {
        /// <summary>
        /// Run all tasks in single-threading model, this is in current Master thread.
        /// </summary>
        SingleThread,

        /// <summary>
        /// Run all tasks in multi-threading model.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "MultiThread", Justification = "it is better to name in this way here")]
        MultiThread,

        /// <summary>
        /// Run tasks in SingleThread or MultiThread mode, depending on the context:
        /// 1. Only one job or not
        /// 2. Single processor environment.
        /// </summary>
        BetterFiting
    }

    /// <summary>
    /// Computing assistant
    /// 1. Optimize single threading on single processor machine and single work item
    /// 2. Leverage multi-core on the machine via ThreadPool.
    /// </summary>
    public static class ComputingAssist
    {
        #region Fields

        private static ComputingModel _computingModel = ComputingModel.BetterFiting;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Computing model.
        /// </summary>
        public static ComputingModel Model
        {
            get { return _computingModel; }
            set { _computingModel = value; }
        }

        #endregion

        #region Public operations

        /// <summary>
        /// Build task mapping with keys and value/context.
        /// </summary>
        /// <typeparam name="TKey">Type of elements in the keys collection.</typeparam>
        /// <typeparam name="TValue">Type of value.</typeparam>
        /// <param name="keys">Key collection to serve as key in final mapping.</param>
        /// <param name="value">Value to server as value for each key in final mapping.</param>
        /// <returns>Built mapping.</returns>
        public static Dictionary<TKey, TValue> BuildMap<TKey, TValue>(IEnumerable<TKey> keys, TValue value)
        {
            if (keys == null)
            {
                throw new ArgumentNullException("keys");
            }

            Dictionary<TKey, TValue> tasks = new Dictionary<TKey, TValue>();
            foreach (TKey key in keys)
            {
                tasks.Add(key, value);
            }

            return tasks;
        }

        /// <summary>
        /// Execute jobs with given mapping and executing function.
        /// The return value is mapped to the same indexing of the input value parameters.
        /// </summary>
        /// <typeparam name="TValue">Type of value which is for 
        /// Input parameter of the executing function.</typeparam>
        /// <typeparam name="TReturn">Type of the return of the executing function.</typeparam>
        /// <param name="items">Collection of input parameter of the executing function.</param>
        /// <param name="function">Executing function.</param>
        /// <returns>Jobs executing result.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "Ignore."),
            System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Ignore.")]
        public static ComputingReturn<TValue, TReturn>[] Execute<TValue, TReturn>(
            IEnumerable<TValue> items, ComputingCallback<int, TValue, TReturn> function)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }

            if (function == null)
            {
                throw new ArgumentNullException("function");
            }

            Dictionary<int, TValue> itemsWrap = new Dictionary<int, TValue>();
            int index = 0;
            foreach (TValue value in items)
            {
                itemsWrap.Add(index++, value);
            }

            Dictionary<int, ComputingReturn<TValue, TReturn>> rets =
                Execute<int, TValue, TReturn>(itemsWrap, function);

            // Sort returns with the same order of input parameters
            List<ComputingReturn<TValue, TReturn>> finalRets = new List<ComputingReturn<TValue, TReturn>>(rets.Count);
            SortedDictionary<int, ComputingReturn<TValue, TReturn>> sortedRets =
                new SortedDictionary<int, ComputingReturn<TValue, TReturn>>(rets);

            foreach (int key in sortedRets.Keys)
            {
                finalRets.Add(sortedRets[key]);
            }

            return finalRets.ToArray();
        }

        /// <summary>
        /// Execute jobs with given mapping and executing function.
        /// The return value is mapped to the same indexing of the input value parameters.
        /// </summary>
        /// <typeparam name="TKey">Type of the key used in directory.</typeparam>
        /// <typeparam name="TValue">Type of value which is for 
        /// Input parameter of the executing function.</typeparam>
        /// <typeparam name="TReturn">Type of the return of the executing function.</typeparam>
        /// <param name="items">Collection of input parameter of the executing function.</param>
        /// <param name="function">Executing function.</param>
        /// <returns>Jobs executing result.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "Ignore."),
            System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Ignore.")]
        public static Dictionary<TKey, ComputingReturn<TValue, TReturn>> Execute<TKey, TValue, TReturn>(
            IDictionary<TKey, TValue> items, ComputingCallback<TKey, TValue, TReturn> function)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }

            if (function == null)
            {
                throw new ArgumentNullException("function");
            }

            Dictionary<TKey, ComputingReturn<TValue, TReturn>> rets = null;

            if ((Model == ComputingModel.SingleThread) ||
                (Model == ComputingModel.BetterFiting && BetterInSingleThread(items.Count)))
            {
                rets = ExecuteInSingleThread<TKey, TReturn, TValue>(items, function);
            }
            else if (items.Count != 0)
            {
                rets = ExecuteViaThreadPool<TKey, TReturn, TValue>(items, function);
            }
            else
            {
                rets = new Dictionary<TKey, ComputingReturn<TValue, TReturn>>();
            }

            return rets;
        }

        #endregion

        #region Private operations

        /// <summary>
        /// Determine whether single thread model is suitable for current task.
        /// </summary>
        /// <param name="itemCount">The number of jobs to process.</param>
        /// <returns>Bool, true idecating single-threading moel is better, otherwise false.</returns>
        private static bool BetterInSingleThread(int itemCount)
        {
            return itemCount == 1 || Environment.ProcessorCount == 1;
        }

        /// <summary>
        /// Split tasks into sub group for scheduling.
        /// </summary>
        /// <typeparam name="TKey">Type of the key elements in the keys collection.</typeparam>
        /// <param name="keys">Value collection to split.</param>
        /// <returns>Sub groups of the key collection.</returns>
        private static List<TKey>[] Split<TKey>(List<TKey> keys)
        {
            // This parameter is used to control max job groups to split, which 
            // controls the number of work into the thread pool.
            const int ParallelParam = 10;

            List<List<TKey>> jobs = new List<List<TKey>>();

            for (int i = 0; i < keys.Count; i++)
            {
                int currentIndex = i % (Environment.ProcessorCount * ParallelParam);
                if (jobs.Count <= currentIndex)
                {
                    jobs.Add(new List<TKey>());
                }

                jobs[currentIndex].Add(keys[i]);
            }

            return jobs.ToArray();
        }

        /// <summary>
        /// Execute jobs via threading pool.
        /// </summary>
        /// <typeparam name="TKey">Type of the key used in directory.</typeparam>
        /// <typeparam name="TReturn">Type of the return of the executing function.</typeparam>
        /// <typeparam name="TValue">Type of value which is for 
        /// Input parameter of the executing function.</typeparam>
        /// <param name="items">Collection of input parameter of the executing function.</param>
        /// <param name="function">Executing function.</param>
        /// <returns>Jobs executing result.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage",
            "CA2201:DoNotRaiseReservedExceptionTypes", Justification = "Ignore.")]
        private static Dictionary<TKey, ComputingReturn<TValue, TReturn>>
            ExecuteViaThreadPool<TKey, TReturn, TValue>(
            IDictionary<TKey, TValue> items, ComputingCallback<TKey, TValue, TReturn> function)
        {
            Dictionary<TKey, ComputingReturn<TValue, TReturn>> rets =
                new Dictionary<TKey, ComputingReturn<TValue, TReturn>>(items.Count);

            ManualResetEvent doneEvent = new ManualResetEvent(false);

            int remainCount = items.Count;
            List<TKey> keys = new List<TKey>(items.Keys);
            List<TKey>[] jobs = Split(keys);

            for (int i = 0; i < jobs.Length; i++)
            {
                List<TKey> subKeys = jobs[i];

                WaitCallback callback = delegate
                    {
                        for (int j = 0; j < subKeys.Count; j++)
                        {
                            TKey key = subKeys[j];
                            ComputingReturn<TValue, TReturn> ret = new ComputingReturn<TValue, TReturn>();
                            try
                            {
                                ret.Param = items[key];

                                // No cancellable operation supported
                                ret.Value = function.Invoke(key, items[key]);
                            }
                            catch (Exception e)
                            {
                                // How to notify the failure operation
                                ret.Exception = e;
                            }
                            finally
                            {
                                lock (rets)
                                {
                                    rets.Add(key, ret);
                                }

                                int decrementedValue = Interlocked.Decrement(ref remainCount);
                                if (decrementedValue == 0)
                                {
                                    doneEvent.Set();
                                }
                            }
                        }
                    };

                bool queued = ThreadPool.QueueUserWorkItem(callback);
                if (!queued)
                {
                    string message = Helper.NeutralFormat("The work item could not be queued.");
                    throw new OutOfMemoryException(message);
                }
            }

            doneEvent.WaitOne();

            return rets;
        }

        /// <summary>
        /// Execute jobs via current thread, this is single threading.
        /// </summary>
        /// <typeparam name="TKey">Type of the key used in directory.</typeparam>
        /// <typeparam name="TReturn">Type of the return of the executing function.</typeparam>
        /// <typeparam name="TValue">Type of value which is for 
        /// Input parameter of the executing function.</typeparam>
        /// <param name="items">Collection of input parameter of the executing function.</param>
        /// <param name="function">Executing function.</param>
        /// <returns>Jobs executing result.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Ignore.")]
        private static Dictionary<TKey, ComputingReturn<TValue, TReturn>>
            ExecuteInSingleThread<TKey, TReturn, TValue>(
            IDictionary<TKey, TValue> items, ComputingCallback<TKey, TValue, TReturn> function)
        {
            Dictionary<TKey, ComputingReturn<TValue, TReturn>> rets =
                new Dictionary<TKey, ComputingReturn<TValue, TReturn>>();

            foreach (TKey key in items.Keys)
            {
                ComputingReturn<TValue, TReturn> ret = new ComputingReturn<TValue, TReturn>();
                ret.Param = items[key];

                try
                {
                    ret.Value = function.Invoke(key, items[key]);
                }
                catch (Exception e)
                {
                    // Can not filter and rethrow here, since rethrown exception will terminate the process
                    // or cause a host to unload the app domain
                    ret.Exception = e;
                }

                rets.Add(key, ret);
            }

            return rets;
        }

        #endregion
    }

    /// <summary>
    /// Computing return type, containing the return value and environments.
    /// </summary>
    /// <typeparam name="TParam">Parameter associated with current result.</typeparam>
    /// <typeparam name="TValue">Type of the result value.</typeparam>
    public class ComputingReturn<TParam, TValue>
    {
        #region Fields

        private TParam _param;
        private TValue _value;
        private Exception _exception;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Value of the return.
        /// </summary>
        public TValue Value
        {
            get
            {
                return _value;
            }

            set
            {
                if (!(value is ValueType) && value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _value = value;
            }
        }

        /// <summary>
        /// Gets or sets Exception catched during the processing, if existing.
        /// </summary>
        public Exception Exception
        {
            get
            {
                return _exception;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _exception = value;
            }
        }

        /// <summary>
        /// Gets or sets Parameters for current return value.
        /// </summary>
        public TParam Param
        {
            get
            {
                return _param;
            }

            set
            {
                if (!(value is ValueType) && value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _param = value;
            }
        }

        #endregion
    }
}