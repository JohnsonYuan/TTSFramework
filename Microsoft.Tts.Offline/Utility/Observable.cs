//----------------------------------------------------------------------------
// <copyright file="Observable.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      This module implement the class Observable.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Utility
{
    /// <summary>
    /// This is a Observable base class, which enable the log or event can be dispatched
    /// To many Observer easily.
    /// </summary>
    /// <typeparam name="T">The object what you want to dispatched.</typeparam>
    public class Observable<T>
    {
        #region Delegates

        /// <summary>
        /// The delegate of update event handler.
        /// </summary>
        /// <param name="sender">Indicate who is the sender of this update.</param>
        /// <param name="update">The thing what want to be dispatched.</param>
        public delegate void UpdatedEventHandler(Observable<T> sender, T update);

        #endregion

        #region Events

        /// <summary>
        /// The event handler of all Observers.
        /// </summary>
        public event UpdatedEventHandler Observer;

        #endregion

        #region Methods

        /// <summary>
        /// This method is used to notify all Observer that there is something updated.
        /// </summary>
        /// <param name="update">The thing what want to be dispatched.</param>
        public void NotifyObservers(T update)
        {
            if (Observer != null)
            {
                Observer(this, update);
            }
        }

        #endregion
    }
}