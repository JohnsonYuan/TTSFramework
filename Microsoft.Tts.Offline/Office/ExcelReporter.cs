//----------------------------------------------------------------------------
// <copyright file="ExcelReporter.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      The implementation of ExcelReporter class
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Office
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Text;
    using EXCEL = Microsoft.Office.Interop.Excel;

    /// <summary>
    /// This class do Excel reporting.
    /// </summary>
    public class ExcelReporter
    {
        #region private members

        /// <summary>
        /// The title of the report content.
        /// </summary>
        private string _filename;

        /// <summary>
        /// Excel sheet items.
        /// </summary>
        private Collection<SheetContent> _sheetItems;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ExcelReporter"/> class.
        /// </summary>
        /// <param name="filename">The specified file name.</param>
        public ExcelReporter(string filename)
        {
            _filename = filename;
            _sheetItems = new Collection<SheetContent>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExcelReporter"/> class.
        /// </summary>
        /// <param name="filename">The specified file name.</param>
        /// <param name="sheetItems">The sheet items.</param>
        public ExcelReporter(string filename, Collection<SheetContent> sheetItems)
        {
            _filename = filename;
            _sheetItems = sheetItems;
        }

        #endregion

        /// <summary>
        /// For Excel formatting
        /// The enum is used to define the column width.
        /// </summary>
        public enum Width
        {
            /// <summary>
            /// Do AutoFit.
            /// </summary>
            AutoFit,

            /// <summary>
            /// Set the width to 50.
            /// </summary>
            Narrow,

            /// <summary>
            /// Set the width to 100.
            /// </summary>
            Medium,

            /// <summary>
            /// Set the width to 150.
            /// </summary>
            Wide
        }

        /// <summary>
        /// For Excel formatting
        /// The enum is used to define the row height.
        /// </summary>
        public enum Height
        {
            /// <summary>
            /// Set the height to 18.
            /// </summary>
            Short,

            /// <summary>
            /// Set the height to 28.
            /// </summary>
            Medium,

            /// <summary>
            /// Set the height to 38.
            /// </summary>
            Tall
        }

        /// <summary>
        /// The enum for text color.
        /// </summary>
        public enum TextColor
        {
            /// <summary>
            /// The color index for Black in Excel is 1.
            /// </summary>
            Black = 1,

            /// <summary>
            /// The color index for Red in Excel is 3.
            /// </summary>
            Red = 3,

            /// <summary>
            /// The color index for Yellow in Excel is 6.
            /// </summary>
            Yellow = 6,

            /// <summary>
            /// The color index for Orange in Excel is 46.
            /// </summary>
            Orange = 46
        }

        /// <summary>
        /// This enum contains the customized type codes.
        /// </summary>
        public enum ContentTypeCode
        {
            /// <summary>
            /// Empty.
            /// </summary>
            Empty = 0,

            /// <summary>
            /// Boolean.
            /// </summary>
            Boolean = 1,

            /// <summary>
            /// Double.
            /// </summary>
            Double = 2,

            /// <summary>
            /// Float.
            /// </summary>
            Single = 3,

            /// <summary>
            /// Integer, include short, long.
            /// </summary>
            Integer = 4,

            /// <summary>
            /// Unsigned integer, include unsigned short, unsigned long.
            /// </summary>
            UnsignedInteger = 5,

            /// <summary>
            /// String.
            /// </summary>
            String = 6,

            /// <summary>
            /// Colored script.
            /// </summary>
            ColoredScript = 7,

            /// <summary>
            /// File hyper links.
            /// </summary>
            HyperLink = 8,
        }

        #region properties

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        public string FileName
        {
            get { return _filename; }
            set { _filename = value; }
        }

        /// <summary>
        /// Gets sheet items.
        /// </summary>
        public Collection<SheetContent> SheetItems
        {
            get
            {
                return _sheetItems;
            }
        }

        #endregion

        #region public methods

        /// <summary>
        /// Add normal sheet item to the report content.
        /// </summary>
        /// <param name="sheetItem">The sheet item to be added.</param>
        public void AddSheetItem(SheetContent sheetItem)
        {
            _sheetItems.Add(sheetItem);
        }

        /// <summary>
        /// Insert a sheet item to a particular position.
        /// </summary>
        /// <param name="index">The position to insert.</param>
        /// <param name="sheetItem">The sheet item to be inserted.</param>
        public void InsertSheetItem(int index, SheetContent sheetItem)
        {
            _sheetItems.Insert(index, sheetItem);
        }

        /// <summary>
        /// Add distribution sheet item to the report content.
        /// </summary>
        /// <typeparam name="T">The type of the distribution X Axis data, can be int, double, etc.</typeparam>
        /// <param name="sheetTitle">The sheet title.</param>
        /// <param name="xAxisTitle">The X Axis Title.</param>
        /// <param name="yAxisTitle">The Y Axis Title.</param>
        /// <param name="distribution">The distribution.</param>
        public void AddDistributionSheetItem<T>(string sheetTitle, string xAxisTitle, string yAxisTitle,
            Dictionary<T, double> distribution)
        {
            ColumnContent columnX = new ColumnContent(xAxisTitle, Width.AutoFit, ContentTypeCode.Double, false);
            ColumnContent columnY = new ColumnContent(yAxisTitle, Width.AutoFit, ContentTypeCode.Double, false);
            foreach (KeyValuePair<T, double> distributionItem in distribution)
            {
                columnX.RecordCollection.Add(distributionItem.Key);
                columnY.RecordCollection.Add(distributionItem.Value);
            }

            SheetContent sheetItem = new SheetContent(sheetTitle, true, new Collection<ColumnContent>() { columnX, columnY });
            AddSheetItem(sheetItem);
        }

        /// <summary>
        /// Add distribution sheet item to the report content.
        /// </summary>
        /// <typeparam name="T">The type of the distribution X Axis data, can be int, double, etc.</typeparam>
        /// <param name="sheetTitle">The sheet title.</param>
        /// <param name="xAxisTitle">The X Axis Title.</param>
        /// <param name="yAxisTitles">The Y Axis Titles.</param>
        /// <param name="distributions">The distributions.</param>
        public void AddDistributionSheetItem<T>(string sheetTitle, string xAxisTitle,
            Collection<string> yAxisTitles,
            Collection<Dictionary<T, double>> distributions)
        {
            if (yAxisTitles.Count != distributions.Count)
            {
                throw new Exception("The Y axis title count does not match the distribution count.");
            }

            Collection<ColumnContent> columns = new Collection<ColumnContent>();
            ColumnContent columnX = new ColumnContent(xAxisTitle, Width.AutoFit, ContentTypeCode.Double, false);
            bool xValuesCollected = false;
            int inDistribution = 0;
            foreach (Dictionary<T, double> distribution in distributions)
            {
                if (xValuesCollected && distribution.Count != columnX.RecordCount)
                {
                    throw new Exception("The X value counts of the distributions are not equal.");
                }

                ColumnContent columnY = new ColumnContent(yAxisTitles[inDistribution++], Width.AutoFit, ContentTypeCode.Double, false);
                foreach (KeyValuePair<T, double> distributionItem in distribution)
                {
                    if (!xValuesCollected)
                    {
                        columnX.RecordCollection.Add(distributionItem.Key);
                    }

                    columnY.RecordCollection.Add(distributionItem.Value);
                }

                if (!xValuesCollected)
                {
                    columns.Add(columnX);
                    xValuesCollected = true;
                }

                columns.Add(columnY);
            }

            SheetContent sheetItem = new SheetContent(sheetTitle, true, columns);
            AddSheetItem(sheetItem);
        }

        /// <summary>
        /// Remove an existing sheet item from the report content.
        /// </summary>
        /// <param name="index">The index of the sheet item to be removed.</param>
        public void RemoveSheetItem(int index)
        {
            if (_sheetItems.Count > index)
            {
                _sheetItems.RemoveAt(index);
            }
        }

        /// <summary>
        /// Report all the content to the excel file.
        /// </summary>
        public void Report()
        {
            try
            {
                using (ExcelHelper _simpleExcel = new ExcelHelper(_filename))
                {
                    _simpleExcel.CreateExcelFile();
                    foreach (SheetContent sheetItem in SheetItems)
                    {
                        _simpleExcel.AddNewSheet(sheetItem.Title, sheetItem);
                    }

                    _simpleExcel.SaveExcel();
                    _simpleExcel.CloseExcel();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Exception in ExcelReporter: ", ex);
            }
        }

        #endregion

        #region Public Internal Classes

        /// <summary>
        /// This struct represent a segment with color setting in a text script.
        /// </summary>
        public struct ColoredTextSegment
        {
            /// <summary>
            /// The text string.
            /// </summary>
            public string Text;

            /// <summary>
            /// The start index of the segment.
            /// </summary>
            public int StartIndex;

            /// <summary>
            /// The length of the segment.
            /// </summary>
            public int Length;

            /// <summary>
            /// The color setting of the segment.
            /// </summary>
            public TextColor Color;
        }

        /// <summary>
        /// This struct stores a simple hyperlink.
        /// </summary>
        public struct Hyperlink
        {
            /// <summary>
            /// Text string.
            /// </summary>
            public string TextToDisplay;

            /// <summary>
            /// The color settings.
            /// </summary>
            public string Address;
        }

        /// <summary>
        /// This class stores colored text script for EXCEL output.
        /// </summary>
        public class ColoredScript
        {
            #region private members

            /// <summary>
            /// Text string.
            /// </summary>
            private string _text;

            /// <summary>
            /// The color settings.
            /// </summary>
            private List<ColoredTextSegment> _coloredTextSegments;

            #endregion

            #region Constructor

            /// <summary>
            /// Initializes a new instance of the <see cref="ColoredScript"/> class.
            /// </summary>
            public ColoredScript()
            {
                _text = string.Empty;
                _coloredTextSegments = new List<ColoredTextSegment>();
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="ColoredScript"/> class.
            /// </summary>
            /// <param name="text">The text string.</param>
            public ColoredScript(string text)
            {
                _text = text;
                _coloredTextSegments = new List<ColoredTextSegment>();
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="ColoredScript"/> class.
            /// </summary>
            /// <param name="text">The text string.</param>
            /// <param name="coloredTextSegments">The colored text segment.</param>
            public ColoredScript(string text, List<ColoredTextSegment> coloredTextSegments)
            {
                _text = text;
                _coloredTextSegments = coloredTextSegments;
            }

            #endregion

            #region properties

            /// <summary>
            /// Gets or sets Text string.
            /// </summary>
            public string Text
            {
                get { return _text; }
                set { _text = value; }
            }

            /// <summary>
            /// Gets the colored text segments.
            /// </summary>
            public List<ColoredTextSegment> ColoredTextSegments
            {
                get { return _coloredTextSegments; }
            }

            #endregion

            #region public methods

            /// <summary>
            /// Add a colored text segment.
            /// </summary>
            /// <param name="text">The text of the segment.</param>
            /// <param name="startIndex">The start index.</param>
            /// <param name="length">The length of the colored text.</param>
            /// <param name="color">The exact color.</param>
            public void AddColoredTextSegment(string text, int startIndex, int length, TextColor color)
            {
                ColoredTextSegments.Add(new ColoredTextSegment() { Text = text, StartIndex = startIndex, Length = length, Color = color });
            }

            /// <summary>
            /// Add a colored text segment.
            /// </summary>
            /// <param name="segment">The colored text segment to be added.</param>
            public void AddColoredTextSegment(ColoredTextSegment segment)
            {
                ColoredTextSegments.Add(segment);
            }
            
            /// <summary>
            /// Append another colored script.
            /// </summary>
            /// <param name="coloredScriptToAppend">The colored script to be appended.</param>
            public void AppendColoredScript(ColoredScript coloredScriptToAppend)
            {
                Text += coloredScriptToAppend.Text;
                if (ColoredTextSegments.Count == 0)
                {
                    coloredScriptToAppend.ColoredTextSegments.ForEach(x => ColoredTextSegments.Add(x));
                }
                else
                {
                    int oldLength = Text.Length;
                    foreach (ColoredTextSegment segmentToAppend in coloredScriptToAppend.ColoredTextSegments)
                    {
                        int newStartIndex = segmentToAppend.StartIndex + oldLength;
                        ColoredTextSegments.Add(
                            new ColoredTextSegment()
                            {
                                Text = segmentToAppend.Text,
                                StartIndex = newStartIndex,
                                Length = segmentToAppend.Length,
                                Color = segmentToAppend.Color
                            });
                    }
                }
            }

            #endregion
        }

        /// <summary>
        /// This class stores all the report content of a column.
        /// </summary>
        public class ColumnContent
        {
            #region Private Fields
            /// <summary>
            /// The title of the column.
            /// </summary>
            private string _title;

            /// <summary>
            /// The column width setting.
            /// </summary>
            private Width _width;

            /// <summary>
            /// Whether to warp script.
            /// </summary>
            private bool _warpText;

            /// <summary>
            /// Column content data type.
            /// </summary>
            private ContentTypeCode _type;

            /// <summary>
            /// The data.
            /// </summary>
            private Collection<object> _columnData;

            #endregion

            #region Constructor

            /// <summary>
            /// Initializes a new instance of the <see cref="ColumnContent"/> class.
            /// </summary>
            /// <param name="title">The column title.</param>
            /// <param name="width">The column width setting.</param>
            /// <param name="type">The content type.</param>
            /// <param name="warpText">Whether to warp text.</param>
            public ColumnContent(string title, Width width, ContentTypeCode type, bool warpText)
            {
                _title = title;
                _width = width;
                _type = type;
                _warpText = warpText;
                _columnData = new Collection<object>();
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="ColumnContent"/> class.
            /// </summary>
            /// <param name="title">The column title.</param>
            /// <param name="width">The column width setting.</param>
            /// <param name="type">The content type.</param>
            /// <param name="warpText">Whether to warp text.</param>
            /// <param name="data">The data collection.</param>
            public ColumnContent(string title, Width width, ContentTypeCode type, bool warpText, Collection<object> data)
            {
                _title = title;
                _width = width;
                _type = type;
                _warpText = warpText;
                _columnData = data;
            }

            #endregion
            
            #region Properties

            /// <summary>
            /// Gets The title of the column.
            /// </summary>
            public string Title
            {
                get { return _title; }
            }

            /// <summary>
            /// Gets The column width setting.
            /// </summary>
            public Width Width
            {
                get { return _width; }
            }

            /// <summary>
            /// Gets The content data type.
            /// </summary>
            public ContentTypeCode Type
            {
                get { return _type; }
            }

            /// <summary>
            /// Gets a value indicating whether to warp text.
            /// </summary>
            public bool WarpText
            {
                get { return _warpText; }
            }

            /// <summary>
            /// Gets The record collection.
            /// </summary>
            public Collection<object> RecordCollection
            {
                get { return _columnData; }
            }

            /// <summary>
            /// Gets The record count.
            /// </summary>
            public int RecordCount
            {
                get { return _columnData.Count; }
            }

            #endregion
        }

        /// <summary>
        /// This class stores all the content of a sheet.
        /// </summary>
        public class SheetContent
        {
            #region Private fields

            /// <summary>
            /// The title of the column.
            /// </summary>
            private string _title;

            /// <summary>
            /// Whether to draw chart for the sheets.
            /// </summary>
            private bool _drawChart;

            /// <summary>
            /// Content of the columns.
            /// </summary>
            private Collection<ColumnContent> _columns;

            /// <summary>
            /// The row height setting.
            /// </summary>
            private Height _rowHeight;
            
            #endregion

            #region Constructor

            /// <summary>
            /// Initializes a new instance of the <see cref="SheetContent"/> class.
            /// </summary>
            /// <param name="title">The title of the sheet.</param>
            /// <param name="drawChart">Whether to draw a chart for the sheet.</param>
            public SheetContent(string title, bool drawChart)
            {
                _title = title;
                _drawChart = drawChart;
                _columns = new Collection<ColumnContent>();
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="SheetContent"/> class.
            /// </summary>
            /// <param name="title">The title of the sheet.</param>
            /// <param name="drawChart">Whether to draw a chart for the sheet.</param>
            /// <param name="columns">The column contents.</param>
            public SheetContent(string title, bool drawChart, Collection<ColumnContent> columns)
            {
                _title = title;
                _drawChart = drawChart;
                _columns = columns;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="SheetContent"/> class.
            /// </summary>
            /// <param name="title">The title of the sheet.</param>
            /// <param name="drawChart">Whether to draw a chart for the sheet.</param>
            /// <param name="rowHeight">The height setting of the sheet.</param>
            /// <param name="columns">The column contents.</param>
            public SheetContent(string title, bool drawChart, Height rowHeight, Collection<ColumnContent> columns)
            {
                _title = title;
                _drawChart = drawChart;
                _rowHeight = rowHeight;
                _columns = columns;
            }

            #endregion

            #region Properties

            /// <summary>
            /// Gets The title of the column.
            /// </summary>
            public string Title
            {
                get { return _title; }
            }

            /// <summary>
            /// Gets a value indicating whether to draw chart for the sheets.
            /// </summary>
            public bool DrawChart
            {
                get { return _drawChart; }
            }

            /// <summary>
            /// Gets The column content.
            /// </summary>
            public Collection<ColumnContent> Columns
            {
                get { return _columns; }
            }

            /// <summary>
            /// Gets the row height.
            /// </summary>
            public Height RowHeight
            {
                get { return _rowHeight; }
            }

            #endregion
        }

        #endregion
    }
}