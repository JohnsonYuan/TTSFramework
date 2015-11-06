//----------------------------------------------------------------------------
// <copyright file="ManagedThreadPool.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      a managed thread pool
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Utility
{
    using System;
    using System.Collections;
    using System.Threading;

    /// <summary>
    /// Simple managed thread pool.
    /// </summary>
    public class ManagedThreadPool
    {
        private static int _maxWorkerThreads = 25;
        private static Queue _waitingCallbacks;
        private static Semaphore _workerThreadNeeded;
        private static AutoResetEvent _workerWorkItemDone;
        private static ArrayList _workerThreads;
        private static int _threadsInUse;
        private static object _poolLock = new object();

        /// <summary>
        /// Initializes static members of the <see cref="ManagedThreadPool"/> class.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline", Justification = "Performance is not concern here")]
        static ManagedThreadPool()
        {
            Initialize();
        }

        /// <summary>
        /// Prevents a default instance of the <see cref="ManagedThreadPool"/> class from being created.
        /// </summary>
        private ManagedThreadPool()
        {
        }

        /// <summary>
        /// Gets or sets Maximum threads.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Ignore.")]
        public static int MaxThreads
        {
            get
            {
                return _maxWorkerThreads;
            }

            set
            {
                int temp = value;
                if (temp != _maxWorkerThreads)
                {
                    _maxWorkerThreads = temp;
                    Reset();
                }
            }
        }

        /// <summary>
        /// Gets Waiting callback.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Ignore.")]
        public static int WaitingCallbacks
        {
            get
            {
                lock (_poolLock)
                {
                    return _waitingCallbacks.Count;
                }
            }
        }

        /// <summary>
        /// Gets The active thread number.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Ignore.")]
        public static int ThreadsInUse
        {
            get
            {
                return _threadsInUse;
            }
        }

        /// <summary>
        /// Queue work items.
        /// </summary>
        /// <param name="callback">Callback work item.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Ignore.")]
        public static void QueueUserWorkItem(WaitCallback callback)
        {
            // Queue the delegate with no state
            QueueUserWorkItem(callback, null);
        }

        /// <summary>
        /// Queue work item with state.
        /// </summary>
        /// <param name="callback">Callback item.</param>
        /// <param name="state">State.</param>
        public static void QueueUserWorkItem(WaitCallback callback, object state)
        {
            // Create a waiting callback that contains the delegate and its state.
            // At it to the processing queue, and signal that data is waiting.
            WaitingCallback waiting = new WaitingCallback(callback, state);
            lock (_poolLock)
            {
                _waitingCallbacks.Enqueue(waiting);
            }

            _workerThreadNeeded.Release();
        }

        /// <summary>
        /// Wait for work items are done.
        /// </summary>
        public static void WaitForDone()
        {
            while (true)
            {
                _workerWorkItemDone.WaitOne();

                if (WaitingCallbacks == 0 && ThreadsInUse == 0)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Reset all.
        /// </summary>
        public static void Reset()
        {
            lock (_poolLock)
            {
                foreach (object obj in _waitingCallbacks)
                {
                    WaitingCallback callback = (WaitingCallback)obj;
                    if (callback.State is IDisposable)
                    {
                        ((IDisposable)callback.State).Dispose();
                    }
                }

                foreach (Thread thread in _workerThreads)
                {
                    if (thread != null)
                    {
                        thread.Abort("reset");
                    }
                }

                Initialize();
            }
        }

        /// <summary>
        /// Do the work items.
        /// </summary>
        private static void ProcessQueuedItems()
        {
            // Process indefinitely
            while (true)
            {
                _workerThreadNeeded.WaitOne();
                WaitingCallback callback = null;
                lock (_poolLock)
                {
                    if (_waitingCallbacks.Count > 0)
                    {
                        callback = (WaitingCallback)_waitingCallbacks.Dequeue();
                    }
                }

                if (callback != null)
                {
                    try
                    {
                        Interlocked.Increment(ref _threadsInUse);
                        callback.Callback(callback.State);
                    }
                    finally
                    {
                        Interlocked.Decrement(ref _threadsInUse);
                    }
                }

                _workerWorkItemDone.Set();
            }
        }

        /// <summary>
        /// Initialize thread pool.
        /// </summary>
        private static void Initialize()
        {
            // Create our thread stores; we handle synchronization ourself
            // as we may run into situations where multiple operations need to be atomic.
            // We keep track of the threads we've created just for good measure; not actually
            // needed for any core functionality.
            _waitingCallbacks = new Queue();
            _workerThreads = new ArrayList();
            _threadsInUse = 0;

            // Create our "thread needed" event
            _workerThreadNeeded = new Semaphore(0, int.MaxValue);
            _workerWorkItemDone = new AutoResetEvent(false);

            // Create all of the worker threads
            for (int i = 0; i < _maxWorkerThreads; i++)
            {
                // Create a new thread and add it to the list of threads.
                Thread newThread = new Thread(new ThreadStart(ProcessQueuedItems));
                _workerThreads.Add(newThread);
                newThread.Name = "ManagedPoolThread #" + i;
                newThread.IsBackground = true;
                newThread.Start();
            }
        }

        /// <summary>
        /// A work item.
        /// </summary>
        private class WaitingCallback
        {
            private WaitCallback _callback;
            private object _state;

            /// <summary>
            /// Initializes a new instance of the <see cref="WaitingCallback"/> class.
            /// </summary>
            /// <param name="callback">Waitcallback.</param>
            /// <param name="state">State.</param>
            public WaitingCallback(WaitCallback callback, object state)
            {
                _callback = callback;
                _state = state;
            }

            /// <summary>
            /// Gets Callback.
            /// </summary>
            public WaitCallback Callback
            {
                get { return _callback; }
            }

            /// <summary>
            /// Gets State.
            /// </summary>
            public object State
            {
                get { return _state; }
            }
        }
    }
}