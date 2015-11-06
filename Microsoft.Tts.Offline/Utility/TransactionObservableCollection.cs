//----------------------------------------------------------------------------
// <copyright file="TransactionObservableCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This is an array conversing helper class. It helps to processing
//     the wave file in different bit per sample.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Utility
{
    using System;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Globalization;

    /// <summary>
    /// The observalbe collection supports transaction changing.
    /// </summary>
    /// <typeparam name="T">Type of the event argument.</typeparam>
    public class TransactionObservableCollection<T> : ObservableCollection<T>
    {
        private NotifyCollectionChangedEventArgs _lastChangedArgs = null;

        private bool _fEnableTransaction = true;

        /// <summary>
        /// The transaction change handler.
        /// </summary>
        public event NotifyCollectionChangedEventHandler TransactionChangeHandler;

        /// <summary>
        /// Gets a value indicating whether IsTransaction.
        /// </summary>
        public bool IsTransaction
        {
            get
            {
                return _fEnableTransaction;
            }
        }

        /// <summary>
        /// StartTransaction.
        /// </summary>
        public void StartTransaction()
        {
            if (_fEnableTransaction == false)
            {
                _fEnableTransaction = true;
                _lastChangedArgs = null;
            }
        }

        /// <summary>
        /// EndTransaction.
        /// </summary>
        public void EndTransaction()
        {
            if (_fEnableTransaction == true)
            {
                _fEnableTransaction = false;
                if (_lastChangedArgs != null && TransactionChangeHandler != null)
                {
                    TransactionChangeHandler(this, _lastChangedArgs);
                }
            }
        }

        /// <summary>
        /// OnCollectionChanged.
        /// </summary>
        /// <param name="e">Event.</param>
        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            _lastChangedArgs = e;
            base.OnCollectionChanged(e);
        }
    }
}