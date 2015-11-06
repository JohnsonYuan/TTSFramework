//----------------------------------------------------------------------------
// <copyright file="AppConfigManager.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements AppConfigManager class
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Config
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Configuration;
    using System.Diagnostics;
    using System.Text;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// App config manager.
    /// </summary>
    public class AppConfigManager
    {
        #region Fields

        private const string AppConfigErrorLevelSectionName = "ErrorLevel";

        private static AppConfigManager _instance;

        private Dictionary<string, ErrorSeverity> _errorSeverityDict =
            new Dictionary<string, ErrorSeverity>();

        #endregion

        #region Construction

        /// <summary>
        /// Prevents a default instance of the <see cref="AppConfigManager"/> class from being created.
        /// </summary>
        private AppConfigManager()
        {
            NameValueCollection nameValueCollection = (NameValueCollection)
                ConfigurationManager.GetSection(AppConfigErrorLevelSectionName);

            if (nameValueCollection != null)
            {
                foreach (string name in nameValueCollection.AllKeys)
                {
                    ErrorSeverity severity = (ErrorSeverity)Enum.Parse(
                        typeof(ErrorSeverity), nameValueCollection[name], true);
                    _errorSeverityDict.Add(name, severity);
                }
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets Singleton instance.
        /// </summary>
        public static AppConfigManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new AppConfigManager();
                }

                return _instance;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Update severity of the error.
        /// </summary>
        /// <param name="error">Error.</param>
        public void UpdateSeverity(Error error)
        {
            error.Severity = GetSeverity(error);
        }

        /// <summary>
        /// Get severity of the error.
        /// </summary>
        /// <param name="error">Error.</param>
        /// <returns>Severity of the error.</returns>
        public ErrorSeverity GetSeverity(Error error)
        {
            string errorKey = GetErrorKey(error);
            ErrorSeverity severity = error.Severity;
            if (_errorSeverityDict.ContainsKey(errorKey))
            {
                severity = _errorSeverityDict[errorKey];
            }

            return severity;
        }

        /// <summary>
        /// Get error key.
        /// </summary>
        /// <param name="error">Error.</param>
        /// <returns>Error key string.</returns>
        private string GetErrorKey(Error error)
        {
            Debug.Assert(error.Enum != null);
            string key = Helper.NeutralFormat("{0}.{1}",
                error.Enum.GetType().FullName.Replace('+', '.'),
                error.Enum.ToString());
            return key;
        }

        #endregion
    }
}