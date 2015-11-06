//----------------------------------------------------------------------------
// <copyright file="StatisticFile.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module defines a common library to manipulate Htk statistic file.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Htk
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// The options for statistic file split.
    /// </summary>
    public enum StatisticFileSplitOptions
    {
        /// <summary>
        /// Split by phoneme.
        /// </summary>
        Phoneme,
    }

    /// <summary>
    /// One item of the Htk statistic file.
    /// </summary>
    public class StatisticItem
    {
        #region Fields

        /// <summary>
        /// The split characters for statistic item.
        /// </summary>
        private static readonly char[] SplitChars = new char[] { ' ' };

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the Htk label of this item.
        /// </summary>
        public Label Label { get; set; }

        /// <summary>
        /// Gets or sets the number of times the state occurred.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Gets or sets the occupancy of each state.
        /// </summary>
        public float[] Occupancies { get; set; }

        #endregion

        #region Static Methods

        /// <summary>
        /// Parses a line to generate a statistic item.
        /// </summary>
        /// <param name="line">A line in statistic file.</param>
        /// <returns>The corresponding statistic item object.</returns>
        public static StatisticItem Parse(string line)
        {
            StatisticItem item = new StatisticItem();
            string[] columns = line.Split(SplitChars, StringSplitOptions.RemoveEmptyEntries);
            if (columns.Length < 4)
            {
                throw new InvalidDataException(Helper.NeutralFormat("Unsupported statistic data format \"{0}\"", line));
            }

            item.Label = new Label();
            item.Label.Text = columns[1].Trim('"');
            item.Count = int.Parse(columns[2]);
            List<float> occupancies = new List<float>();
            for (int i = 3; i < columns.Length; ++i)
            {
                occupancies.Add(float.Parse(columns[i]));
            }

            item.Occupancies = occupancies.ToArray();
            return item;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Converts this object to string.
        /// </summary>
        /// <returns>The string represent the object.</returns>
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(Helper.NeutralFormat("\"{0}\" {1,4}", Label.Text, Count));
            foreach (float occupancy in Occupancies)
            {
                builder.Append(Helper.NeutralFormat("    {0:F11}", occupancy));
            }

            return builder.ToString();
        }

        #endregion
    }

    /// <summary>
    /// The statistic file object.
    /// </summary>
    public class StatisticFile
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the StatisticFile class as an empty object.
        /// </summary>
        public StatisticFile()
        {
            Items = new List<StatisticItem>();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the list of statistic items.
        /// </summary>
        public IList<StatisticItem> Items { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Loads the statistic file.
        /// </summary>
        /// <param name="statisticFile">The statistic file name.</param>
        public void Load(string statisticFile)
        {
            using (StreamReader reader = new StreamReader(statisticFile))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    Items.Add(StatisticItem.Parse(line));
                }
            }
        }

        /// <summary>
        /// Saves the object into a statistic file.
        /// </summary>
        /// <param name="statisticFile">The statistc file name.</param>
        public void Save(string statisticFile)
        {
            using (StreamWriter writer = new StreamWriter(statisticFile, false, Encoding.ASCII))
            {
                for (int i = 0; i < Items.Count; ++i)
                {
                    writer.WriteLine("{0,7} {1}", i, Items[i].ToString());
                }
            }
        }

        /// <summary>
        /// Splits the statistic file.
        /// </summary>
        /// <param name="option">The split options.</param>
        /// <returns>A dictionry whose key is phoneme and value is statistic file object.</returns>
        public Dictionary<string, StatisticFile> Split(StatisticFileSplitOptions option)
        {
            if (option != StatisticFileSplitOptions.Phoneme)
            {
                throw new NotSupportedException("Only StatisticFileSplitOptions.Phoneme is supported now");
            }

            Dictionary<string, StatisticFile> phoneIndexedFile = new Dictionary<string, StatisticFile>();
            foreach (StatisticItem item in Items)
            {
                string phone = item.Label.CentralPhoneme;
                if (!phoneIndexedFile.ContainsKey(phone))
                {
                    phoneIndexedFile.Add(phone, new StatisticFile());
                }

                phoneIndexedFile[phone].Items.Add(item);
            }

            return phoneIndexedFile;
        }

        /// <summary>
        /// Generates the full list file.
        /// </summary>
        /// <param name="fullListFile">The full list file name.</param>
        public void GenerateFullListFile(string fullListFile)
        {
            using (StreamWriter writer = new StreamWriter(fullListFile, false, Encoding.ASCII))
            {
                foreach (StatisticItem item in Items)
                {
                    writer.WriteLine(item.Label.Text);
                }
            }
        }

        #endregion
    }
}