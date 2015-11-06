//----------------------------------------------------------------------------
// <copyright file="LangDataCompilerError.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements lexicon error class
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Compiler
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.Tts.Offline.Common;

    /// <summary>
    /// FrontEndCompilerError.
    /// </summary>
    public enum DataCompilerError
    {
        /// <summary>
        /// Tool not found
        /// Parameters: 
        /// {0}: tool name
        /// {1}: path of tool.
        /// </summary>
        [ErrorAttribute(Message = "Tool \"{0}\" could not be found: '{1}'.")]
        ToolNotFound,

        /// <summary>
        /// Raw data not found
        /// Parameters: 
        /// {0}: raw data name
        /// {1}: path of raw data.
        /// </summary>
        [ErrorAttribute(Message = "Raw data for \"{0}\" could not be found: '{1}'.")]
        RawDataNotFound,

        /// <summary>
        /// All Domain Raw data not found
        /// Parameters: 
        /// {0}: raw data domain.
        /// </summary>
        [ErrorAttribute(Message = "All domain raw data for \"{0}\" domain could not be found.")]
        AllDomainRawDataNotFound,

        /// <summary>
        /// Path is null or empty
        /// Parameters: 
        /// {0}: path of raw data.
        /// </summary>
        [ErrorAttribute(Message = "Path of '{0}' for raw data could not be null or empty.")]
        PathNotInitialized,

        /// <summary>
        /// Invalid module data
        /// Parameters: 
        /// {0}: module data name.
        /// </summary>
        [ErrorAttribute(Message = "Invalid Module data of \"{0}\".")]
        InvalidModuleData,

        /// <summary>
        /// Raw Data Error
        /// Parameters: 
        /// {0}: raw data name.
        /// </summary>
        [ErrorAttribute(Message = "Error was found in raw data: {0}.")]
        RawDataError,

        /// <summary>
        /// No domain data in unified raw data
        /// Parameters: 
        /// {0}: domain name
        /// {1}: raw data name.
        /// </summary>
        [ErrorAttribute(Message = "Data for \"{0}\" domain was not found in raw data: {1}.")]
        NoDomainDataInRawData,

        /// <summary>
        /// Compiling fail
        /// Parameters: 
        /// {0}: raw data name.
        /// </summary>
        [ErrorAttribute(Message = "Dependencies are not valid: \"{0}\".")]
        DependenciesNotValid,

        /// <summary>
        /// Skip Combining Data For Guid
        /// Parameters: 
        /// {0}: data guid
        /// {1}: data path.
        /// </summary>
        [ErrorAttribute(Message = "Skip data for guid {{{0}}} as the path not found: '{1}'.",
            Severity = ErrorSeverity.Warning)]
        SkipCombiningDataForGuid,

        /// <summary>
        /// Skip Combining Data
        /// Parameters: 
        /// {0}: data name
        /// {1}: data path.
        /// </summary>
        [ErrorAttribute(Message = "Skip data \"{0}\" as the path not found: '{1}'",
            Severity = ErrorSeverity.Warning)]
        SkipCombiningData,

        /// <summary>
        /// Zero Module Data
        /// Parameters: 
        /// {0}: data name.
        /// </summary>
        [ErrorAttribute(Message = "Skip data \"{0}\" as the data size is zero",
            Severity = ErrorSeverity.Warning)]
        ZeroModuleData,

        /// <summary>
        /// Necessary Module Data missing
        /// Parameters: 
        /// {0}: data name.
        /// </summary>
        [ErrorAttribute(Message = "Missing necessary Module Data \"{0}\" and automatically compiling it",
            Severity = ErrorSeverity.Warning)]
        NecessaryDataMissing,

        /// <summary>
        /// Domain Data Missing.
        /// Parameters: 
        /// {0}: domain data name.
        /// </summary>
        [ErrorAttribute(Message = "Could not find any data to combine in \"{0}\" domain.")]
        DomainDataMissing,

        /// <summary>
        /// Combination halt.
        /// </summary>
        [ErrorAttribute(Message = "Unable to combine the final data file.")]
        CombinationHalt,

        /// <summary>
        /// Invalid raw data
        /// Parameters: 
        /// {0}: raw data name.
        /// </summary>
        [ErrorAttribute(Message = "There is no such raw data \"{0}\".",
            Severity = ErrorSeverity.Warning)]
        InvalidRawData,

        /// <summary>
        /// Duplicate item key
        /// Parameters: 
        /// {0}: key name.
        /// </summary>
        [ErrorAttribute(Message = "There are duplicate keys: \"{0}\".",
            Severity = ErrorSeverity.MustFix)]
        DuplicateItemKey,

        /// <summary>
        /// Invalid binary data.
        /// Parameters: 
        /// {0}: binary data name.
        /// </summary>
        [ErrorAttribute(Message = "There is no such binary data \"{0}\".",
            Severity = ErrorSeverity.Warning)]
        InvalidBinaryData,

        /// <summary>
        /// Save Binary File Fail
        /// {0}: data name
        /// {1}: reason.
        /// </summary>
        [ErrorAttribute(Message = "Fail to save binary data file for \"{0}\" as: {1}",
            Severity = ErrorSeverity.Warning)]
        SaveBinaryFileFail,

        /// <summary>
        /// Composite Compiling Fail
        /// {0}: data name
        /// {1}: reason.
        /// </summary>
        [ErrorAttribute(Message = "Fail to do composite data compiling for \"{0}\" as {1}")]
        CompositeCompilingFail,

        /// <summary>
        /// Compiling Log With Data Name
        /// Parameters: 
        /// {0}: data name
        /// {1}: detail log.
        /// </summary>
        [ErrorAttribute(Message = "Log of Compiling data \"{0}\": {1}",
            Severity = ErrorSeverity.NoError)]
        CompilingLogWithDataName,

        /// <summary>
        /// Compiling Log.
        /// </summary>
        [ErrorAttribute(Message = "{0}",
            Severity = ErrorSeverity.NoError)]
        CompilingLog,

        /// <summary>
        /// Compiling Log with error
        /// Parameters: 
        /// {0}: data name
        /// {1}: detail log.
        /// </summary>
        [ErrorAttribute(Message = "Log of Compiling data \"{0}\" with error: {1}")]
        CompilingLogWithError,

        /// <summary>
        /// Compiling Log with warning
        /// Parameters: 
        /// {0}: data name
        /// {1}: detail log.
        /// </summary>
        [ErrorAttribute(Message = "Log of Compiling data \"{0}\" with warning: {1}",
            Severity = ErrorSeverity.Warning)]
        CompilingLogWithWarning,

        /// <summary>
        /// Invalid Guid string
        /// Parameters: 
        /// {0}: data name
        /// {1}: Guid string
        /// {2}: detail error message.
        /// </summary>
        [ErrorAttribute(Message = "Invalid Guid string {{{1}}} for data \"{0}\" : {2}")]
        InvalidGuidString
    }
}