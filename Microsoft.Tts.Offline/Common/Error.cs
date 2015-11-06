//----------------------------------------------------------------------------
// <copyright file="Error.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements Data Error class
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Common
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Reflection;
    using System.Text;
    using Microsoft.Tts.Offline.Config;

    /// <summary>
    /// Message Combination Manner definition.
    /// </summary>
    public enum MessageCombine
    {
        /// <summary>
        /// Directly concatenate the message.
        /// </summary>
        Concatenate,

        /// <summary>
        /// Put inner message nested inside the formatted message
        /// Inner Message is reserved as 1st parameter in the formatted message.
        /// </summary>
        Nested
    }

    /// <summary>
    /// Error severity.
    /// </summary>
    public enum ErrorSeverity
    {
        /// <summary>
        /// Default Severity
        /// If not fix, it will break the process.
        /// </summary>
        MustFix,

        /// <summary>
        /// Warning.
        /// </summary>
        Warning,

        /// <summary>
        /// No error, or just log information.
        /// </summary>
        NoError
    }

    /// <summary>
    /// Error Attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ErrorAttribute : Attribute
    {
        #region Fields
        private string _message;
        private MessageCombine _manner = MessageCombine.Concatenate;
        private string _concatedString = string.Empty;
        private ErrorSeverity _severity = ErrorSeverity.MustFix;
        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorAttribute"/> class.
        /// </summary>
        public ErrorAttribute()
        {
        }

        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets Error Message.
        /// </summary>
        public string Message
        {
            get { return _message; }
            set { _message = value; }
        }

        /// <summary>
        /// Gets or sets Message combination manner.
        /// </summary>
        public MessageCombine Manner
        {
            get { return _manner; }
            set { _manner = value; }
        }

        /// <summary>
        /// Gets or sets Concatenation string.
        /// </summary>
        public string ConcateString
        {
            get { return _concatedString; }
            set { _concatedString = value; }
        }

        /// <summary>
        /// Gets or sets Error severity.
        /// </summary>
        public ErrorSeverity Severity
        {
            get { return _severity; }
            set { _severity = value; }
        }

        #endregion

        #region public static methods
        /// <summary>
        /// Get attribute for the error enum.
        /// </summary>
        /// <param name="error">Error num.</param>
        /// <returns>Error attribute.</returns>
        public static ErrorAttribute GetAttribute(Enum error)
        {
            if (error == null)
            {
                throw new ArgumentNullException("error");
            }

            ErrorAttribute attribute = null;
            Type errorType = error.GetType();
            MemberInfo[] memInfo = errorType.GetMember(error.ToString());
            if (memInfo != null && memInfo.Length > 0)
            {
                object[] objs = memInfo[0].GetCustomAttributes(typeof(ErrorAttribute), false);
                Attribute[] attributes = objs as Attribute[];
                foreach (Attribute attr in attributes)
                {
                    attribute = attr as ErrorAttribute;
                    if (attribute != null)
                    {
                        break;
                    }
                }
            }

            if (attribute == null)
            {
                throw new System.ArgumentException(
                    string.Format(CultureInfo.InvariantCulture,
                        "Cann't find the ErrorAttribute for the Enum type which contains error of {0}.",
                        error.ToString()));
            }

            return attribute;
        }

        #endregion
    }

    /// <summary>
    /// Basic Error class.
    /// </summary>
    public class Error
    {
        #region Fields

        private Enum _error;
        private string[] _args;
        private ErrorAttribute _attribute;
        private Error _innerError;
        private ErrorSeverity _severity;

        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="Error"/> class.
        /// </summary>
        /// <param name="error">Error enum.</param>
        /// <param name="args">Argument list.</param>
        public Error(Enum error, params string[] args)
            : this(error, null, args)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Error"/> class.
        /// </summary>
        /// <param name="error">Error enum.</param>
        /// <param name="innerError">Inner error.</param>
        /// <param name="args">Argument list.</param>
        public Error(Enum error, Error innerError, params string[] args)
        {
            _innerError = innerError;
            _error = error;
            _args = args;
            _attribute = ErrorAttribute.GetAttribute(error);

            if (_innerError == null)
            {
                _severity = _attribute.Severity;
            }
            else
            {
                // set _severity as inner error severity
                _severity = _innerError.Severity;
            }

            ValidateArgument();
            AppConfigManager.Instance.UpdateSeverity(this);
        }

        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets Error severity.
        /// </summary>
        public ErrorSeverity Severity
        {
            get { return _severity; }
            set { _severity = value; }
        }

        /// <summary>
        /// Gets The error enum.
        /// </summary>
        public Enum Enum
        {
            get { return _error; }
        }

        /// <summary>
        /// Gets The inner error.
        /// </summary>
        public Error InnerError
        {
            get { return _innerError; }
        }

        /// <summary>
        /// Gets Argument list.
        /// </summary>
        public IList<string> Args
        {
            get { return _args; }
        }

        #endregion

        #region public methods

        /// <summary>
        /// Build error message according to error enum and argument list.
        /// </summary>
        /// <param name="errorEnum">Error enum.</param>
        /// <param name="argList">Argument list.</param>
        /// <returns>Error message.</returns>
        public static string BuildMessage(Enum errorEnum, params string[] argList)
        {
            Error error = new Error(errorEnum, argList);
            return error.ToString();
        }

        /// <summary>
        /// ToString.
        /// </summary>
        /// <returns>Expanded error message.</returns>
        public override string ToString()
        {
            return ToString(true, true);
        }

        /// <summary>
        /// ToString.
        /// </summary>
        /// <param name="inner">Whether containing inner error message.</param>
        /// <returns>Error message.</returns>
        public string ToString(bool inner)
        {
            string message = string.Empty;
            switch (_attribute.Manner)
            {
                case MessageCombine.Concatenate:
                    message = string.Format(CultureInfo.InvariantCulture, _attribute.Message, _args);
                    if (inner && _innerError != null)
                    {
                        message = message + _attribute.ConcateString + _innerError.ToString(true);
                    }

                    break;
                case MessageCombine.Nested:
                    string innerMessage = string.Empty;
                    if (inner && _innerError != null)
                    {
                        innerMessage = _innerError.ToString(true);
                    }

                    message = string.Format(CultureInfo.InvariantCulture, _attribute.Message,
                        AddHead(_args, innerMessage));
                    break;
            }

            return message.TrimEnd();
        }

        /// <summary>
        /// ToString with whether inner message and whether with prefix.
        /// </summary>
        /// <param name="inner">Whether inner message.</param>
        /// <param name="prefix">Whether with prefix.</param>
        /// <returns>Error message.</returns>
        public string ToString(bool inner, bool prefix)
        {
            string message = string.Empty;
            if (prefix)
            {
                message = BuildMessageWithPrefix(inner);
            }
            else
            {
                message = ToString(inner);
            }

            return message;
        }

        /// <summary>
        /// Override the Equals function.
        /// </summary>
        /// <param name="obj">Object.</param>
        /// <returns>True for equal.</returns>
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            Error error = (Error)obj;
            return _error.Equals(error.Enum) &&
                ((_innerError == null && error.InnerError == null) || _innerError.Equals(error.InnerError)) &&
                ArgumentEquals(error._args);
        }

        /// <summary>
        /// Override the GetHashCode function.
        /// </summary>
        /// <returns>Hash code.</returns>
        public override int GetHashCode()
        {
            int hashCode = _error.GetHashCode() ^ _error.GetType().GetHashCode();
            if (_innerError != null)
            {
                hashCode ^= _innerError.GetHashCode();
            }

            foreach (string arg in _args)
            {
                if (!string.IsNullOrEmpty(arg))
                {
                    hashCode ^= arg.GetHashCode();
                }
            }

            return hashCode;
        }
        #endregion

        #region private methods

        /// <summary>
        /// Insert string before the string array.
        /// </summary>
        /// <param name="list">String array.</param>
        /// <param name="head">The inserted head.</param>
        /// <returns>Added string array.</returns>
        private static string[] AddHead(string[] list, string head)
        {
            string[] newList = new string[list.Length + 1];
            newList[0] = head;
            for (int i = 1; i < newList.Length; i++)
            {
                newList[i] = list[i - 1];
            }

            return newList;
        }

        /// <summary>
        /// Build error string with prefix.
        /// </summary>
        /// <param name="inner">Whether build with inner message.</param>
        /// <returns>Error string with prefix.</returns>
        private string BuildMessageWithPrefix(bool inner)
        {
            string prefix = string.Empty;
            if (this.Severity == ErrorSeverity.MustFix)
            {
                prefix = "ERROR: ";
            }
            else if (this.Severity == ErrorSeverity.Warning)
            {
                prefix = "WARNING: ";
            }
            else if (this.Severity == ErrorSeverity.NoError)
            {
                prefix = "INFO: ";
            }

            return string.Format(CultureInfo.InvariantCulture, "{0}{1}", prefix, ToString(inner));
        }

        /// <summary>
        /// Validate the argument list whether match the error formatted message.
        /// </summary>
        private void ValidateArgument()
        {
            try
            {
                ToString(true);
            }
            catch (FormatException)
            {
                throw new ArgumentException(
                    "Argument number doesn't satisfy the requirement of the formatted attribute message.");
            }
        }

        /// <summary>
        /// Check whether the arguments are equals to this.args.
        /// </summary>
        /// <param name="args">Arguments.</param>
        /// <returns>True for equal.</returns>
        private bool ArgumentEquals(string[] args)
        {
            bool equal = true;
            if ((args == null && _args != null) ||
                (args != null && _args == null))
            {
                equal = false;
            }
            else if (args != null && _args != null)
            {
                if (args.Length != _args.Length)
                {
                    equal = false;
                }
                else
                {
                    for (int argIndex = 0; argIndex < args.Length; argIndex++)
                    {
                        if (args[argIndex] != _args[argIndex])
                        {
                            equal = false;
                            break;
                        }
                    }
                }
            }

            return equal;
        }

        #endregion
    }
}