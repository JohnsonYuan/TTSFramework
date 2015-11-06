//----------------------------------------------------------------------------
// <copyright file="BasicTypeExtension.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements help functions
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Basic type extension.
    /// </summary>
    public static class BasicTypeExtension
    {
        /// <summary>
        /// Convert bool value to XML string.
        /// </summary>
        /// <param name="boolValue">Bool value.</param>
        /// <returns>XML bool string value.</returns>
        public static string ToXmlValue(this bool boolValue)
        {
            return boolValue.ToString(CultureInfo.InvariantCulture).ToLowerInvariant();
        }

        /// <summary>
        /// InsertBoolParameter.
        /// </summary>
        /// <param name="arguments">Arguments.</param>
        /// <param name="parameterName">ParameterName.</param>
        /// <param name="parameterValue">ParameterValue.</param>
        /// <returns>Inserted bool parameter.</returns>
        public static string[] InsertBoolParameter(this string[] arguments,
            string parameterName, bool parameterValue)
        {
            Helper.ThrowIfNull(arguments);
            Helper.ThrowIfNull(parameterName);
            string[] newArgs = arguments;
            if (parameterValue)
            {
                newArgs = new string[arguments.Length + 1];
                newArgs[0] = parameterName;
                for (int i = 0; i < arguments.Length; i++)
                {
                    newArgs[i + 1] = arguments[i];
                }
            }

            return newArgs;
        }

        /// <summary>
        /// InsertBoolParameter.
        /// </summary>
        /// <param name="arguments">Arguments.</param>
        /// <param name="parameterName">ParameterName.</param>
        /// <param name="parameterValue">ParameterValue.</param>
        /// <returns>Inserted optional parameter.</returns>
        public static string[] InsertOptionalParameter(this string[] arguments,
            string parameterName, string parameterValue)
        {
            Helper.ThrowIfNull(arguments);
            string[] newArgs = arguments;
            if (!string.IsNullOrEmpty(parameterValue))
            {
                newArgs = new string[arguments.Length + 2];
                newArgs[0] = parameterName;
                newArgs[1] = parameterValue;
                for (int i = 0; i < arguments.Length; i++)
                {
                    newArgs[i + 2] = arguments[i];
                }
            }

            return newArgs;
        }
    }
}