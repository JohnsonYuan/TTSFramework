//----------------------------------------------------------------------------
// <copyright file="AcousticItem.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements acoustic item/information
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Text;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Acoustice information of utterance.
    /// </summary>
    public class AcousticItem
    {
        #region Fields

        private string _wave16kFilePath;
        private string _epochFilePath;

        private string _recordedWaveFilePath;
        private string _egg16kFilePath;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets EpochFilePath.
        /// </summary>
        public string EpochFilePath
        {
            get
            {
                return _epochFilePath;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                if (!File.Exists(value))
                {
                    throw Helper.CreateException(typeof(FileNotFoundException),
                        value);
                }

                _epochFilePath = value;
            }
        }

        /// <summary>
        /// Gets or sets EggFilePath.
        /// </summary>
        public string Egg16kFilePath
        {
            get
            {
                return _egg16kFilePath;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                if (!File.Exists(value))
                {
                    throw Helper.CreateException(typeof(FileNotFoundException),
                        value);
                }

                _egg16kFilePath = value;
            }
        }

        /// <summary>
        /// Gets or sets Wave16kFilePath.
        /// </summary>
        public string Wave16kFilePath
        {
            get
            {
                return _wave16kFilePath;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                if (!File.Exists(value))
                {
                    throw Helper.CreateException(typeof(FileNotFoundException),
                        value);
                }

                _wave16kFilePath = value;
            }
        }

        /// <summary>
        /// Gets or sets WaveFilePath.
        /// </summary>
        public string RecordedWaveFilePath
        {
            get
            {
                return _recordedWaveFilePath;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                if (!File.Exists(value))
                {
                    throw Helper.CreateException(typeof(FileNotFoundException),
                        value);
                }

                _recordedWaveFilePath = value;
            }
        }

        #endregion
    }
}