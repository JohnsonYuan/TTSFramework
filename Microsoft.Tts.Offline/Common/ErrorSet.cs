//----------------------------------------------------------------------------
// <copyright file="ErrorSet.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements Error set class
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Common
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Common Error.
    /// </summary>
    public enum CommonError
    {
        /// <summary>
        /// Not consistent language between two objects.
        /// </summary>
        [ErrorAttribute(Message = "Not consistent language identifer between {0} in {1} vs {2} in {3}")]
        NotConsistentLanguage,

        /// <summary>
        /// Empty symbol.
        /// </summary>
        [ErrorAttribute(Message = "Failed to parse Enum [{0}] from value [{1}]",
            Severity = ErrorSeverity.MustFix)]
        FailedParseEnum
    }

    /// <summary>
    /// Error set.
    /// </summary>
    public class ErrorSet
    {
        #region private variables
        private Collection<Error> _errorSet = new Collection<Error>();
        private Dictionary<Error, int> _errorDict = new Dictionary<Error, int>();
        private Dictionary<ErrorSeverity, int> _errorSeverityCount =
            new Dictionary<ErrorSeverity, int>();
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorSet"/> class.
        /// </summary>
        public ErrorSet()
        {
            foreach (ErrorSeverity errorSeverity in Enum.GetValues(typeof(ErrorSeverity)))
            {
                _errorSeverityCount[errorSeverity] = 0;
            }
        }

        /// <summary>
        /// Gets Error number.
        /// </summary>
        public int Count
        {
            get { return _errorSet.Count; }
        }

        #region public methods

        /// <summary>
        /// Gets Error collection in this set.
        /// </summary>
        public ReadOnlyCollection<Error> Errors
        {
            get { return new ReadOnlyCollection<Error>(_errorSet); }
        }

        /// <summary>
        /// Add an error to the error set.
        /// </summary>
        /// <param name="error">New error.</param>
        public void Add(Error error)
        {
            if (error != null)
            {
                if (_errorDict.ContainsKey(error))
                {
                    _errorDict[error]++;
                }
                else
                {
                    _errorSet.Add(error);
                    _errorDict.Add(error, 1);
                    _errorSeverityCount[error.Severity]++;
                }
            }
        }

        /// <summary>
        /// Add an error set to current error set.
        /// </summary>
        /// <param name="errorSet">Error set to be added.</param>
        public void AddRange(ErrorSet errorSet)
        {
            foreach (Error error in errorSet.Errors)
            {
                Add(error);
            }
        }

        /// <summary>
        /// Add an error to the error set.
        /// </summary>
        /// <param name="errorEnum">Error enum.</param>
        /// <param name="argList">Argument list.</param>
        public void Add(Enum errorEnum, params string[] argList)
        {
            Error error = new Error(errorEnum, argList);
            Add(error);
        }

        /// <summary>
        /// Add an error containing the inner error to the error set.
        /// </summary>
        /// <param name="errorEnum">Error enum.</param>
        /// <param name="innerError">Inner error.</param>
        /// <param name="argList">Argument list.</param>
        public void Add(Enum errorEnum, Error innerError, params string[] argList)
        {
            Error error = new Error(errorEnum, innerError, argList);
            Add(error);
        }

        /// <summary>
        /// Remove an error from the error set.
        /// </summary>
        /// <param name="error">Error to be removed.</param>
        /// <returns>Whether the error is successfully removed.</returns>
        public bool Remove(Error error)
        {
            bool removed = false;

            if (error != null && _errorDict.ContainsKey(error))
            {
                _errorSeverityCount[error.Severity]--;
                _errorDict[error]--;
                _errorSet.Remove(error);

                if (_errorDict[error] == 0)
                {
                    _errorDict.Remove(error);
                }

                removed = true;
            }

            return removed;
        }

        /// <summary>
        /// Remove an error set from current error set.
        /// </summary>
        /// <param name="errorSet">Error set to be removed.</param>
        /// <returns>Error set be actually removed.</returns>
        public ErrorSet RemoveRange(ErrorSet errorSet)
        {
            ErrorSet removedErrorSet = new ErrorSet();

            if (errorSet != null)
            {
                foreach (Error error in errorSet.Errors)
                {
                    if (Remove(error))
                    {
                        removedErrorSet.Add(error);
                    }
                }
            }

            return removedErrorSet;
        }

        /// <summary>
        /// Clear the error set.
        /// </summary>
        public void Clear()
        {
            _errorSet.Clear();
            _errorDict.Clear();
        }

        /// <summary>
        /// Merge with another error set.
        /// </summary>
        /// <param name="errorSet">Another error set.</param>
        public void Merge(ErrorSet errorSet)
        {
            if (errorSet != null)
            {
                foreach (Error error in errorSet.Errors)
                {
                    Add(error);
                }
            }
        }

        /// <summary>
        /// Check the error set whether containing the error with certain severity.
        /// </summary>
        /// <param name="severity">Quried error severity.</param>
        /// <returns>True for containing, otherwise false.</returns>
        public bool Contains(ErrorSeverity severity)
        {
            return _errorSeverityCount[severity] > 0;
        }

        /// <summary>
        /// Get the count of errors with certain severity.
        /// </summary>
        /// <param name="severity">Error severity.</param>
        /// <returns>Count of error.</returns>
        public int GetSeverityCount(ErrorSeverity severity)
        {
            return _errorSeverityCount[severity];
        }

        /// <summary>
        /// Set severity of all errors to specified severity.
        /// </summary>
        /// <param name="severity">Severity specified to set to.</param>
        public void SetSeverity(ErrorSeverity severity)
        {
            if (!Enum.IsDefined(typeof(ErrorSeverity), severity))
            {
                return;
            }

            foreach (Error error in _errorSet)
            {
                if (error.Severity != severity)
                {
                    _errorSeverityCount[error.Severity]--;
                    error.Severity = severity;
                    _errorSeverityCount[error.Severity]++;
                }
            }
        }

        /// <summary>
        /// Check whether contains the special error enum.
        /// </summary>
        /// <param name="errorEnum">Error enum.</param>
        /// <returns>True for ccontaining, otherwise false.</returns>
        public bool Contains(Enum errorEnum)
        {
            bool found = false;
            foreach (Error error in _errorSet)
            {
                if (found = error.Enum.Equals(errorEnum))
                {
                    break;
                }
            }

            return found;
        }

        /// <summary>
        /// Export the error report.
        /// </summary>
        /// <param name="textWriter">Text writer.</param>
        public void Export(TextWriter textWriter)
        {
            if (textWriter != null)
            {
                foreach (Error error in _errorSet)
                {
                    textWriter.WriteLine(error.ToString());
                }
            }
        }

        /// <summary>
        /// Append this instance to text file.
        /// </summary>
        /// <param name="filePath">Target file to append.</param>
        /// <param name="title">Tag of the errors.</param>
        /// <param name="append">Whether appending.</param>
        public void Export(string filePath, string title, bool append)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            if (string.IsNullOrEmpty(title))
            {
                throw new ArgumentNullException("filePath");
            }

            Helper.EnsureFolderExistForFile(filePath);
            using (StreamWriter sw = new StreamWriter(filePath, append, Encoding.Unicode))
            {
                sw.WriteLine("<{0}>", title);
                foreach (Error error in _errorSet)
                {
                    sw.WriteLine(error.ToString());
                }

                sw.WriteLine();
            }
        }

        /// <summary>
        /// Override ErrorSet ToString method.
        /// </summary>
        /// <returns>String value represents the error set.</returns>
        public string ErrorsString()
        {
            return ErrorsString(false);
        }

        /// <summary>
        /// Override ErrorSet ToString method.
        /// </summary>
        /// <param name="sort">Whether need sort the error.</param>
        /// <returns>String value represents the error set.</returns>
        public string ErrorsString(bool sort)
        {
            Collection<Error> errorSet = new Collection<Error>();
            if (!sort)
            {
                errorSet = _errorSet;
            }
            else
            {
                SortedDictionary<ErrorSeverity, SortedDictionary<Enum, Collection<Error>>> sortedErrors =
                    new SortedDictionary<ErrorSeverity, SortedDictionary<Enum, Collection<Error>>>();

                foreach (Error error in _errorSet)
                {
                    if (!sortedErrors.ContainsKey(error.Severity))
                    {
                        sortedErrors.Add(error.Severity, new SortedDictionary<Enum, Collection<Error>>());
                    }

                    if (!sortedErrors[error.Severity].ContainsKey(error.Enum))
                    {
                        sortedErrors[error.Severity].Add(error.Enum, new Collection<Error>());
                    }

                    sortedErrors[error.Severity][error.Enum].Add(error);
                }

                sortedErrors.Values.ForEach(severityErrors => severityErrors.Values.ForEach(
                    typeErrors => typeErrors.ForEach(error => errorSet.Add(error))));
            }

            StringBuilder sb = new StringBuilder();
            errorSet.ForEach(error => sb.AppendLine(error.ToString()));
            return sb.ToString();
        }

        /// <summary>
        /// Export the errors into string collection.
        /// </summary>
        /// <param name="errors">Error string.</param>
        public void Export(Collection<string> errors)
        {
            if (errors != null)
            {
                foreach (Error error in _errorSet)
                {
                    errors.Add(error.ToString());
                }
            }
        }

        #endregion
    }
}