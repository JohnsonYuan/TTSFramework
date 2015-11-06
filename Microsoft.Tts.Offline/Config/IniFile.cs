//----------------------------------------------------------------------------
// <copyright file="IniFile.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module explain *.Ini File
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Config
{
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Load from ini file.
    /// </summary>
    public class IniFile
    {
        #region Fields

        /// <summary>
        /// IDictionary[section, IList[key, value]].
        /// </summary>
        private IDictionary<string, IList<KeyValuePair<string, string>>> _sections =
            new Dictionary<string, IList<KeyValuePair<string, string>>>();
        #endregion

        #region Properties

        /// <summary>
        /// Gets All section members.
        /// IDictionary[section, IList[KeyValuePair[key, value]]].
        /// </summary>
        public IDictionary<string, IList<KeyValuePair<string, string>>> Sections
        {
            get { return _sections; }
        }
        #endregion

        #region Method

        /// <summary>
        /// Load *.ini File.
        /// </summary>
        /// <param name="filePath">Ini file path.</param>
        /// <exception cref="FileLoadException">Load file exception.</exception>
        public void Load(string filePath)
        {
            IList<KeyValuePair<string, string>> section = null;
            string[] lines = File.ReadAllLines(filePath);
            for (int index = 0; index < lines.Length; index++)
            {
                string line = lines[index].Trim();
                if (!string.IsNullOrEmpty(line))
                {
                    if (line[0] == '[')
                    {
                        if (line[line.Length - 1] != ']')
                        {
                            throw CreateException("Line should be end with ']'.", filePath, index);
                        }

                        section = new List<KeyValuePair<string, string>>();
                        string sectionName = line.Substring(1, line.Length - 2).Trim();
                        _sections.Add(sectionName, section);
                    }
                    else
                    {
                        int equalPos = line.IndexOf("=");
                        if (equalPos < 0)
                        {
                            throw CreateException("Expression should be: Key = Value.", filePath, index);
                        }

                        if (equalPos == 0)
                        {
                            throw CreateException("No key found.", filePath, index);
                        }

                        if (equalPos + 1 == line.Length)
                        {
                            throw CreateException("No value found.", filePath, index);
                        }

                        if (section == null)
                        {
                            throw CreateException("No section found. Please add a section line before current line.", filePath, index);
                        }

                        string key = line.Substring(0, equalPos).Trim();
                        string value = line.Substring(equalPos + 1).Trim();
                        section.Add(new KeyValuePair<string, string>(key, value));
                    }
                }
            }
        }

        /// <summary>
        /// Create a FileLoadException.
        /// </summary>
        /// <param name="msg">Show message.</param>
        /// <param name="file">File path.</param>
        /// <param name="line">Line num.</param>
        /// <returns>A new instance of FileLoadException.</returns>
        private static FileLoadException CreateException(string msg, string file, int line)
        {
            return new FileLoadException(string.Format("Error in \"{1}\" at line {2}: {0}", msg, file, line));
        }

        #endregion
    }
}