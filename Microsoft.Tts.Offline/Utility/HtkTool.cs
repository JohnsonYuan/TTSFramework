//----------------------------------------------------------------------------
// <copyright file="HtkTool.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements class to manage Htk(HMM toolkit) tools
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// HTK tools.
    /// </summary>
    public class HtkTool
    {
        #region Fields

        private string _htkDir;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Directory of HTK tools.
        /// </summary>
        public string HtkDir
        {
            get
            {
                return _htkDir;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _htkDir = value;
            }
        }

        /// <summary>
        /// Gets Find certian tool.
        /// </summary>
        /// <param name="name">Tool name.</param>
        /// <returns>Tool path.</returns>
        public string this[string name]
        {
            get { return (HtkDir == null) ? null : Path.Combine(HtkDir, name); }
        }

        #endregion

        #region Public static operations

        /// <summary>
        /// Read a sentence block from MLF file.
        /// </summary>
        /// <param name="mlfReader">Reader of MLF file.</param>
        /// <returns>Line collection of one block.</returns>
        public static Collection<string> ReadOneBlock(StreamReader mlfReader)
        {
            if (mlfReader == null)
            {
                throw new ArgumentNullException("mlfReader");
            }

            string line = mlfReader.ReadLine();
            if (mlfReader.EndOfStream)
            {
                Debug.Assert(string.IsNullOrEmpty(line));
                return null;
            }

            Collection<string> lines = new Collection<string>();
            Match match = Regex.Match(line, @"/([^/]*)\.(rec|lab)");
            if (!match.Success)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Invalid section header found at {0}", line);
                throw new InvalidDataException(message);
            }

            lines.Add(line);

            while ((line = mlfReader.ReadLine()) != null)
            {
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                lines.Add(line);
                if (line == ".")
                {
                    break;
                }
            }

            // Should end with "."
            if (line != ".")
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Invalid section footer found at {0}", line);
                throw new InvalidDataException(message);
            }

            return lines;
        }

        /// <summary>
        /// Verify whether a MLF file is in well-format.
        /// </summary>
        /// <param name="filePath">File path.</param>
        /// <returns>True if passed, otherwise false.</returns>
        public static bool VerifyMlfFormat(string filePath)
        {
            using (StreamReader sr = new StreamReader(filePath))
            {
                string line = null;
                line = sr.ReadLine();
                if (line != "#!MLF!#")
                {
                    return false;
                }

                bool validSection = false;
                while ((line = sr.ReadLine()) != null)
                {
                    validSection = false;

                    // this line should be header of certain section
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line == ".")
                        {
                            // find ending mark of section, start new section
                            validSection = true;
                            break;
                        }
                    }
                }

                return validSection;
            }
        }

        /// <summary>
        /// Verify whether a SCP file is in well-format.
        /// </summary>
        /// <param name="filePath">File to verify.</param>
        /// <returns>True if passed, otherwise false.</returns>
        public static bool VerifyScpFormat(string filePath)
        {
            using (StreamReader sr = new StreamReader(filePath))
            {
                string line = null;
                while ((line = sr.ReadLine()) != null)
                {
                    string[] items = line.Split(new char[] { ' ' },
                        StringSplitOptions.RemoveEmptyEntries);
                    if (items.Length != 1 && items.Length != 2)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        #endregion

        #region Operations

        /// <summary>
        /// Clone this instance.
        /// </summary>
        /// <returns>New HtkTool instance.</returns>
        public HtkTool Clone()
        {
            HtkTool tools = new HtkTool();
            tools.HtkDir = HtkDir;

            return tools;
        }

        #endregion
    }
}