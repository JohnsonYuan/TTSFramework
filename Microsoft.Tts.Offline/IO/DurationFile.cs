//----------------------------------------------------------------------------
// <copyright file="DurationFile.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      File with state durations of phones
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System.Collections;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// State duration in a state of a phone.
    /// </summary>
    public struct PhoneStateDuration
    {
        /// <summary>
        /// Phone label.
        /// </summary>
        public string PhoneLabel;

        /// <summary>
        /// Frame number (duration) in the five states.
        /// </summary>
        public int[] FramesInState;
    }

    /// <summary>
    /// File with state durations of phones.
    /// </summary>
    public class DurationFile
    {
        #region Field

        /// <summary>
        /// Duration collection.
        /// </summary>
        private Collection<PhoneStateDuration> _durations;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="DurationFile"/> class.
        /// </summary>
        /// <param name="stateCount">The number of states for each phone.</param>
        public DurationFile(int stateCount)
        {
            StateCount = stateCount;
            _durations = new Collection<PhoneStateDuration>();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the state count.
        /// </summary>
        public int StateCount { get; private set; }

        /// <summary>
        /// Gets or sets Duration Collection.
        /// </summary>
        public Collection<PhoneStateDuration> Durations
        {
            get { return _durations; }
            set { _durations = value; }
        }

        #endregion

        #region public operations

        /// <summary>
        /// Write state durations to a file
        /// File line format: phonelabel + state order number (1-5) + frames in the state.
        /// </summary>
        /// <param name="filePath">Duration file path.</param>
        public void Save(string filePath)
        {
            Save(filePath, false);
        }

        /// <summary>
        /// Write state durations to a file
        /// File line format: phonelabel + state order number (1-5) + frames in the state.
        /// </summary>
        /// <param name="filePath">Duration file path.</param>
        /// <param name="append">Append or not.</param>
        public void Save(string filePath, bool append)
        {
            Helper.EnsureFolderExistForFile(filePath);

            using (StreamWriter sw = new StreamWriter(filePath, append, Encoding.Unicode))
            {
                foreach (PhoneStateDuration duration in Durations)
                {
                    for (int i = 0; i < StateCount; i++)
                    {
                        int framesInState = 0;
                        if (duration.FramesInState.Length >= i + 1)
                        {
                            framesInState = duration.FramesInState[i];
                        }

                        sw.WriteLine(duration.PhoneLabel + "\t" + (i + 2) + "\t" +
                            framesInState);
                    }
                }
            }
        }

        /// <summary>
        /// Read state durations of phones from a state duration file.
        /// </summary>
        /// <param name="filePath">Duration file path.</param>
        public void Load(string filePath)
        {
            Reset();
            Helper.CheckFileExists(filePath);

            Collection<string> lines = new Collection<string>();
            foreach (string line in Helper.FileLines(filePath, Encoding.Unicode, true))
            {
                lines.Add(line);
            }

            for (int i = 0; i < lines.Count / StateCount; i++)
            {
                PhoneStateDuration duration = new PhoneStateDuration();
                duration.FramesInState = new int[StateCount];

                for (int j = 0; j < StateCount; j++)
                {
                    string[] values = lines[(i * StateCount) + j].Split('\t');
                    Debug.Assert(3 == values.Length);

                    duration.PhoneLabel = values[0];
                    duration.FramesInState[j] = int.Parse(values[2]);
                }

                Durations.Add(duration);
            }
        }

        /// <summary>
        /// Remove all values.
        /// </summary>
        public void Reset()
        {
            Durations.Clear();
        }

        /// <summary>
        /// Convert from one dimension structure to TwoDimensionArray.
        /// </summary>
        /// <returns>Two-dimension array.</returns>
        public int[,] ToTwoDimensionArray()
        {
            int[,] array = new int[Durations.Count, StateCount];
            for (int i = 0; i < Durations.Count; ++i)
            {
                for (int j = 0; j < StateCount; ++j)
                {
                    array[i, j] = Durations[i].FramesInState[j];
                }
            }

            return array;
        }

        #endregion
    }
}