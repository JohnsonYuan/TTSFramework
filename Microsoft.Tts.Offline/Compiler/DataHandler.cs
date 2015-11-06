//----------------------------------------------------------------------------
// <copyright file="DataHandler.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      Data Handler for Raw data
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Compiler
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Xml;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Utility;
    
    /// <summary>
    /// Data handler.
    /// </summary>
    public class DataHandler
    {
        #region Fields
        private string _name;
        private Language _language = Language.Neutral;
        private string _path;
        private string _relativePath;
        private object _object;
        private bool _processedLoad;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="DataHandler"/> class.
        /// </summary>
        /// <param name="name">Name.</param>
        public DataHandler(string name)
        {
            _name = name;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets Language.
        /// </summary>
        public Language Language
        {
            get
            { 
                return _language; 
            }
        }

        /// <summary>
        /// Gets Data Name.
        /// </summary>
        public string Name
        {
            get 
            { 
                return _name; 
            }
        }

        /// <summary>
        /// Gets or sets Raw Data Path.
        /// </summary>
        public string Path
        {
            get
            { 
                return _path;
            }

            set 
            { 
                _path = value; 
            }
        }

        /// <summary>
        /// Gets or sets Data relative path.
        /// </summary>
        public string RelativePath
        {
            get
            { 
                return _relativePath;
            }

            set 
            { 
                _relativePath = value;
            }
        }

        #endregion

        #region public methods
        /// <summary>
        /// Set the language.
        /// </summary>
        /// <param name="language">Language.</param>
        public virtual void SetLanguage(Language language)
        {
            _language = language;
        }

        /// <summary>
        /// Get the object.
        /// </summary>
        /// <param name="errorSet">ErrorSet.</param>
        /// <returns>Object.</returns>
        public object GetObject(ErrorSet errorSet)
        {
            if (errorSet == null)
            {
                throw new ArgumentNullException("errorSet");
            }

            if (!_processedLoad && _object == null)
            {
                _processedLoad = true;
                if (string.IsNullOrEmpty(this.Path))
                {
                    errorSet.Add(DataCompilerError.PathNotInitialized, this.Name);
                }
                else if (!File.Exists(this.Path))
                {
                    errorSet.Add(DataCompilerError.RawDataNotFound, this.Name,
                        this.Path);
                }
                else
                {
                    _object = LoadDataObject(errorSet);
                }
            }
            else if (_processedLoad && _object == null)
            {
                errorSet.Add(DataCompilerError.RawDataError, this.Name);
            }

            return _object;
        }

        /// <summary>
        /// Set the object.
        /// </summary>
        /// <param name="obj">Object.</param>
        public void SetObject(object obj)
        {
            _object = obj;
        }

        #endregion

        /// <summary>
        /// Load data object.
        /// </summary>
        /// <param name="errorSet">ErrorSet.</param>
        /// <returns>Data object.</returns>
        internal virtual object LoadDataObject(ErrorSet errorSet)
        {
            return null;
        }
    }
}