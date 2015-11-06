//----------------------------------------------------------------------------
// <copyright file="ExitCode.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      ExitCode
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Define the Error Number.
    /// </summary>
    public sealed class ExitCode
    {
        #region Fields

        /// <summary>
        /// No Error.
        /// </summary>
        public const int NoError = 0;

        /// <summary>
        /// CloseError: ({targetname}:{type}) �{reason}�.
        /// </summary>
        public const int CloseError = 1;

        /// <summary>
        /// InvalidArgument: ({targetname}:{type}) [{action}], �{reason}�.
        /// </summary>
        public const int InvalidArgument = -1;

        /// <summary>
        /// InvalidData: ({targetname}:{type}) [{action}], '{reason}'.
        /// </summary>
        public const int InvalidData = 6;

        /// <summary>
        /// InvalidOperation.
        /// </summary>
        public const int InvalidOperation = 7;

        /// <summary>
        /// InvalidType.
        /// </summary>
        public const int InvalidType = 8;

        /// <summary>
        /// OpenError.
        /// </summary>
        public const int OpenError = 15;

        /// <summary>
        /// OperationTimeout.
        /// </summary>
        public const int OperationTimeout = 16;

        /// <summary>
        /// ParseError .
        /// </summary>
        public const int ParseError = 17;

        /// <summary>
        /// ReadError.
        /// </summary>
        public const int ReadError = 19;

        /// <summary>
        /// WriteError.
        /// </summary>
        public const int WriteError = 24;

        /// <summary>
        /// Others.
        /// </summary>
        public const int GenericError = 999;

        #endregion

        /// <summary>
        /// Prevents a default instance of the <see cref="ExitCode"/> class from being created.
        /// </summary>
        private ExitCode()
        {
        }
    }
}