//----------------------------------------------------------------------------
// <copyright file="CsvFile.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This is an excel CSV file operation class.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Office
{
    using System;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// CSV row definition.
    /// </summary>
    public class CsvRow
    {
        /// <summary>
        /// Initializes a new instance of the CsvRow class.
        /// </summary>
        /// <param name="columnCount">Column count of the CSV file.</param>
        public CsvRow(int columnCount)
        {
            if (columnCount <= 0)
            {
                throw new ArgumentException(Helper.NeutralFormat(
                    "Invalid column count [{0}]", columnCount));
            }

            Items = new string[columnCount];
        }

        /// <summary>
        /// Gets column items.
        /// </summary>
        public string[] Items { get; private set; }

        /// <summary>
        /// Gets or sets column item value.
        /// </summary>
        /// <param name="columnIndex">Column index.</param>
        /// <returns>Column cell value.</returns>
        public string this[int columnIndex]
        {
            get
            {
                Helper.ThrowIfNull(Items);
                if (columnIndex >= Items.Length)
                {
                    throw new ArgumentException(Helper.NeutralFormat(
                        "Index count [{0}] should be smaller than item count [{1}]",
                        columnIndex.ToString(CultureInfo.InvariantCulture),
                        Items.Length.ToString(CultureInfo.InvariantCulture)));
                }

                return Items[columnIndex];
            }

            set
            {
                Helper.ThrowIfNull(Items);
                if (columnIndex >= Items.Length)
                {
                    throw new ArgumentException(Helper.NeutralFormat(
                        "Index count [{0}] should be smaller than item count [{1}]",
                        columnIndex.ToString(CultureInfo.InvariantCulture),
                        Items.Length.ToString(CultureInfo.InvariantCulture)));
                }

                Items[columnIndex] = value;
            }
        } 

        /// <summary>
        /// String value of this row.
        /// </summary>
        /// <returns>String value.</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (Items != null)
            {
                for (int i = 0; i < Items.Length; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(CsvFile.ColumnDelimeter);
                    }

                    if (!string.IsNullOrEmpty(Items[i]))
                    {
                        sb.Append(Items[i]);
                    }
                }
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// CSV file definition.
    /// </summary>
    public class CsvFile
    {
        /// <summary>
        /// CSV file max row count.
        /// </summary>
        public const int MaxRowCount = 100000;

        /// <summary>
        /// Initializes a new instance of the CsvFile class.
        /// </summary>
        /// <param name="columnCount">Column count of the CSV file.</param>
        public CsvFile(int columnCount)
        {
            ColumnCount = columnCount;
            Rows = new Collection<CsvRow>();
        }

        /// <summary>
        /// Gets CSV file column delimeter.
        /// </summary>
        public static char ColumnDelimeter
        {
            get
            {
                return Delimitor.TabChar;
            }
        }

        /// <summary>
        /// Gets encoding of the CSV file.
        /// </summary>
        public static Encoding Encoding
        {
            get
            {
                return Encoding.Unicode;
            }
        }

        /// <summary>
        /// Gets column count of the CSV file.
        /// </summary>
        public int ColumnCount { get; private set; }

        /// <summary>
        /// Gets rows of the CSV file.
        /// </summary>
        public Collection<CsvRow> Rows { get; private set; }

        /// <summary>
        /// Gets or sets value to CSV file.
        /// </summary>
        /// <param name="rowIndex">Row index.</param>
        /// <param name="columnIndex">Column index.</param>
        /// <returns>Cell value.</returns>
        public string this[int rowIndex, int columnIndex]
        {
            get
            {
                Helper.ThrowIfNull(Rows);
                if (rowIndex < 0 || columnIndex < 0)
                {
                    throw new ArgumentException(Helper.NeutralFormat(
                        "Row index [{0}] and column index [{1}] should not be positive",
                        rowIndex.ToString(CultureInfo.InvariantCulture),
                        columnIndex.ToString(CultureInfo.InvariantCulture)));
                }

                if (rowIndex >= Rows.Count)
                {
                    throw new ArgumentException(Helper.NeutralFormat(
                        "Row index [{0}] should be smaller than row count [{1}]",
                        rowIndex.ToString(CultureInfo.InvariantCulture),
                        Rows.Count.ToString(CultureInfo.InvariantCulture)));
                }

                return Rows[rowIndex][columnIndex];
            }

            set
            {
                Helper.ThrowIfNull(Rows);
                if (rowIndex < 0 || columnIndex < 0)
                {
                    throw new ArgumentException(Helper.NeutralFormat(
                        "Row index [{0}] and column index [{1}] should not be positive",
                        rowIndex.ToString(CultureInfo.InvariantCulture),
                        columnIndex.ToString(CultureInfo.InvariantCulture)));
                }

                if (rowIndex >= MaxRowCount)
                {
                    throw new ArgumentException(Helper.NeutralFormat(
                        "Row index [{0}] should be smaller than max index [{1}]",
                        rowIndex.ToString(CultureInfo.InvariantCulture),
                        MaxRowCount.ToString(CultureInfo.InvariantCulture)));
                }

                if (rowIndex >= Rows.Count)
                {
                    int rowCount = Rows.Count;
                    for (int i = 0; i < rowIndex + 1 - rowCount; i++)
                    {
                        Rows.Add(new CsvRow(ColumnCount));
                    }
                }

                Rows[rowIndex][columnIndex] = value;
            }
        }

        /// <summary>
        /// Save CSV file.
        /// </summary>
        /// <param name="filePath">File path.</param>
        public void Save(string filePath)
        {
            Helper.ThrowIfNull(filePath);
            Helper.EnsureFolderExistForFile(filePath);
            using (StreamWriter sw = new StreamWriter(filePath, false, Encoding))
            {
                Rows.ForEach(r => sw.WriteLine(r.ToString()));
            }
        }
    }
}