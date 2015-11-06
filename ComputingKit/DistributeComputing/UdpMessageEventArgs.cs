//----------------------------------------------------------------------------
// <copyright file="UdpMessageEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements UdpMessage EventArgs
// </summary>
//----------------------------------------------------------------------------

namespace DistributeComputing
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// UdpMessageEventArgs.
    /// </summary>
    public class UdpMessageEventArgs : EventArgs
    {
        #region Fields

        private string _message;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpMessageEventArgs"/> class.
        /// </summary>
        /// <param name="message">Message.</param>
        public UdpMessageEventArgs(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentNullException("message");
            }

            Message = message;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Message.
        /// </summary>
        public string Message
        {
            get
            {
                return _message;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _message = value;
            }
        }

        #endregion
    }
}