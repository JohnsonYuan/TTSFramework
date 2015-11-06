//----------------------------------------------------------------------------
// <copyright file="OfflineException.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     Implementation for common exception class used in offline dll
//
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Security.Permissions;
    using System.Text;

    /// <summary>
    /// Exception for null object field.
    /// </summary>
    [Serializable]
    public class NullObjectFieldException : InvalidOperationException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NullObjectFieldException"/> class.
        /// </summary>
        /// <param name="msg">Msg.</param>
        public NullObjectFieldException(string msg)
            : base(msg)
        {
        }
    }

    /// <summary>
    /// Exception for general offline exception.
    /// </summary>
    [Serializable]
    public class GeneralException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GeneralException"/> class.
        /// </summary>
        /// <param name="msg">Msg.</param>
        public GeneralException(string msg)
            : base(msg)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GeneralException"/> class.
        /// </summary>
        /// <param name="msg">Msg.</param>
        /// <param name="e">E.</param>
        public GeneralException(string msg, Exception e)
            : base(msg, e)
        {
        }
    }

    /// <summary>
    /// Exception for general offline exception.
    /// </summary>
    [Serializable]
    public class ExceptionCollection : GeneralException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionCollection"/> class.
        /// </summary>
        /// <param name="msg">The message of the exception.</param>
        public ExceptionCollection(string msg)
            : this(msg, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionCollection"/> class.
        /// </summary>
        /// <param name="msg">The message of the exception.</param>
        /// <param name="e">The inner exception of this new exception.</param>
        public ExceptionCollection(string msg, Exception e)
            : base(msg, e)
        {
            Exceptions = new List<Exception>();
        }

        /// <summary>
        /// Gets or sets the instances of exceptions hosted by this collection.
        /// </summary>
        public List<Exception> Exceptions { get; set; }

        #region Operations

        /// <summary>
        /// Get object data.
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
}