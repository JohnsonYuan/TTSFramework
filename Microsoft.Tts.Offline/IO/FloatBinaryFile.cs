//----------------------------------------------------------------------------
// <copyright file="FloatBinaryFile.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      Binary file with float values
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System.Collections.ObjectModel;
    using System.IO;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Binary file with float values.
    /// </summary>
    public class FloatBinaryFile
    {
        #region Field

        /// <summary>
        /// Float values collection.
        /// </summary>
        private Collection<float> _values;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="FloatBinaryFile"/> class.
        /// </summary>
        public FloatBinaryFile()
        {
            _values = new Collection<float>();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Float values collection.
        /// </summary>
        public Collection<float> Values
        {
            get { return _values; }
            set { _values = value; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Save float values to a binary file.
        /// </summary>
        /// <param name="filePath">Float binary file path.</param>
        public void Save(string filePath)
        {
            Save(filePath, false);
        }

        /// <summary>
        /// Save float values to a readable txt file.
        /// </summary>
        /// <param name="filePath">Txt file path.</param>
        public void SaveAsTxt(string filePath)
        {
            Helper.EnsureFolderExistForFile(filePath);
            FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            try
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    fs = null;

                    foreach (float value in _values)
                    {
                        sw.WriteLine(value);
                    }
                }
            }
            finally
            {
                if (null != fs)
                {
                    fs.Dispose();
                }
            }
        }

        /// <summary>
        /// Save float values to a binary file.
        /// </summary>
        /// <param name="filePath">Float binary file path.</param>
        /// <param name="append">Append or not.</param>
        public void Save(string filePath, bool append)
        {
            Helper.EnsureFolderExistForFile(filePath);

            FileMode fileMode = append ? FileMode.Append : FileMode.Create;
            FileStream fs = new FileStream(filePath, fileMode, FileAccess.Write);
            try
            {
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    fs = null;

                    foreach (float value in _values)
                    {
                        bw.Write(value);
                    }
                }
            }
            finally
            {
                if (null != fs)
                {
                    fs.Dispose();
                }
            }
        }

        /// <summary>
        /// Load float values from a binary file.
        /// </summary>
        /// <param name="filePath">Float binary file path.</param>
        public void Load(string filePath)
        {
            Helper.CheckFileExists(filePath);
            FileInfo fileInfo = new FileInfo(filePath);
            int valueCount = (int)fileInfo.Length / sizeof(float);

            FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            try
            {
                using (BinaryReader br = new BinaryReader(fs))
                {
                    fs = null;

                    for (int i = 0; i < valueCount; ++i)
                    {
                        Values.Add(br.ReadSingle());
                    }
                }
            }
            finally
            {
                if (null != fs)
                {
                    fs.Dispose();
                }
            }
        }

        /// <summary>
        /// Remove all values.
        /// </summary>
        public void Reset()
        {
            Values.Clear();
        }

        #endregion
    }
}