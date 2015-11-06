//----------------------------------------------------------------------------
// <copyright file="ExcelHelper.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This is an office excel processing helper class.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Office
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Reflection;
    using Microsoft.Office.Interop.Excel;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Class of ExcelHelper.
    /// </summary>
    [CLSCompliant(false)]
    public class ExcelHelper : IDisposable
    {
        #region fields

        /// <summary>
        /// Excel application.
        /// </summary>
        private string _fileName;
        private Application _excel;
        private _Workbook _workbook;
        private _Worksheet _worksheet;
        private int _currentSheetIndex;

        #endregion

        #region constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ExcelHelper"/> class.
        /// </summary>
        /// <param name="fileName">File name.</param>
        public ExcelHelper(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentNullException("fileName");
            }

            _fileName = fileName;
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets or sets File name.
        /// </summary>
        public string FileName
        {
            get { return _fileName; }
            set { _fileName = value; }
        }

        /// <summary>
        /// Gets or sets Work book of Excel.
        /// </summary>
        public _Workbook Workbook
        {
            get { return _workbook; }
            set { _workbook = value; }
        }

        #endregion
       
        #region methods

        /// <summary>
        /// Select cell from a sheet.
        /// </summary>
        /// <param name="sheet">Sheet.</param>
        /// <param name="rowIndex">Row index.</param>
        /// <param name="columnIndex">Column index.</param>
        /// <returns>Cell.</returns>
        public static Range SelectCell(_Worksheet sheet, int rowIndex, int columnIndex)
        {
            Range cell = sheet.Cells[rowIndex, columnIndex] as Range;
            if (cell == null)
            {
                throw new InvalidCastException("Error in getting Excel object model.");
            }

            return cell;
        }

        /// <summary>
        /// Get range from a sheet.
        /// </summary>
        /// <param name="sheet">Sheet.</param>
        /// <param name="leftCell">Left cell.</param>
        /// <param name="rightCell">Right cell.</param>
        /// <returns>Range.</returns>
        public static Range GetRange(_Worksheet sheet, object leftCell, object rightCell)
        {
            Range range = sheet.get_Range(leftCell, rightCell) as Range;
            if (range == null)
            {
                throw new InvalidCastException("Error in getting Excel range.");
            }

            return range;
        }

        /// <summary>
        /// Create excel file.
        /// </summary>
        public void CreateExcelFile()
        {
            _excel = new Microsoft.Office.Interop.Excel.Application();
            if (_excel == null)
            {
                throw new InvalidProgramException("Error in opening Excel application.");
            }

            _excel.Visible = false;
            _workbook = (_Workbook)_excel.Workbooks.Add(XlSheetType.xlWorksheet);
            _worksheet = (_Worksheet)_workbook.ActiveSheet;
            _currentSheetIndex = 0;
            if (_workbook == null || _worksheet == null)
            {
                throw new InvalidProgramException("Error in getting Excel object model.");
            }
        }
                
        /// <summary>
        /// Create new sheet in currently active workbook.
        /// </summary>
        /// <param name="strSheetName">The sheet name.</param>
        /// <param name="sheetContent">The sheet content.</param>
        public void AddNewSheet(string strSheetName,
            ExcelReporter.SheetContent sheetContent)
        {
            Helper.ThrowIfNull(_workbook);

            if (_currentSheetIndex > 0)
            {
                _worksheet = (_Worksheet)_workbook.Worksheets.Add(Missing.Value,
                    _workbook.ActiveSheet,
                    1,
                    XlSheetType.xlWorksheet);
            }

            _worksheet.Name = strSheetName;
            string finalColumnIndex = string.Empty;
            string finalCellIndex = string.Empty;
            string finalRawIndex = string.Empty;
            string categoryTitle = string.Empty;
            Collection<string> columnTitles = new Collection<string>();
            int inColumn = 1;
            foreach (ExcelReporter.ColumnContent column in sheetContent.Columns)
            {
                _worksheet.Cells[1, inColumn] = column.Title;
                if (string.IsNullOrEmpty(categoryTitle))
                {
                    categoryTitle = column.Title;
                }
                else
                {
                    columnTitles.Add(column.Title);
                }

                int rawEndIndex = column.RecordCount + 1;
                string columnIndex = GetColumnIndex(inColumn);
                string endIndex = columnIndex + rawEndIndex.ToString();

                switch (column.Type)
                {
                    case ExcelReporter.ContentTypeCode.ColoredScript:
                        for (int rawIndex = 2; rawIndex <= rawEndIndex; rawIndex++)
                        {
                            SetColoredTextScript(SelectCell(_worksheet, rawIndex, inColumn),
                                (ExcelReporter.ColoredScript)column.RecordCollection[rawIndex - 2]);
                        }

                        break;
                    case ExcelReporter.ContentTypeCode.HyperLink:
                        for (int rawIndex = 2; rawIndex <= rawEndIndex; rawIndex++)
                        {
                            SetHyperlinks(SelectCell(_worksheet, rawIndex, inColumn),
                                (ExcelReporter.Hyperlink)column.RecordCollection[rawIndex - 2]);
                        }

                        break;
                    default:
                        for (int rawIndex = 2; rawIndex <= rawEndIndex; rawIndex++)
                        {
                            _worksheet.Cells[rawIndex, inColumn] = column.RecordCollection[rawIndex - 2];
                        }

                        break;
                }

                finalRawIndex = rawEndIndex.ToString();
                finalColumnIndex = columnIndex;
                finalCellIndex = endIndex;
                if (column.Width == ExcelReporter.Width.AutoFit)
                {
                    _worksheet.get_Range(finalColumnIndex + "1", finalCellIndex).EntireColumn.AutoFit();
                }
                else if (column.Width == ExcelReporter.Width.Narrow)
                {
                    _worksheet.get_Range(finalColumnIndex + "1", finalCellIndex).ColumnWidth = 50;
                }
                else if (column.Width == ExcelReporter.Width.Medium)
                {
                    _worksheet.get_Range(finalColumnIndex + "1", finalCellIndex).ColumnWidth = 100;
                }
                else if (column.Width == ExcelReporter.Width.Wide)
                {
                    _worksheet.get_Range(finalColumnIndex + "1", finalCellIndex).ColumnWidth = 150;
                }

                if (column.WarpText)
                {
                    _worksheet.get_Range(finalColumnIndex + "1", finalCellIndex).EntireColumn.WrapText = true;
                }

                inColumn++;
            }

            string finalTitleIndex = finalColumnIndex + "1";
            _worksheet.get_Range("A1", finalTitleIndex).Font.Bold = true;
            _worksheet.get_Range("A1", finalTitleIndex).Font.ColorIndex = 1;
            _worksheet.get_Range("A1", finalTitleIndex).Font.Size = 12;
            _worksheet.get_Range("A1", finalTitleIndex).VerticalAlignment = XlVAlign.xlVAlignCenter;
            _worksheet.get_Range("A1", finalTitleIndex).HorizontalAlignment = XlHAlign.xlHAlignCenter;
            _worksheet.get_Range("A1", finalTitleIndex).EntireRow.WrapText = true;

            _worksheet.get_Range("A2", finalCellIndex).Font.Size = 11;
            _worksheet.get_Range("A2", finalCellIndex).VerticalAlignment = XlVAlign.xlVAlignCenter;
            _worksheet.get_Range("A2", finalCellIndex).HorizontalAlignment = XlHAlign.xlHAlignLeft;

            switch (sheetContent.RowHeight)
            {
                case ExcelReporter.Height.Short:
                    _worksheet.get_Range("A2", finalCellIndex).EntireRow.RowHeight = 18;
                    break;
                case ExcelReporter.Height.Medium:
                    _worksheet.get_Range("A2", finalCellIndex).EntireRow.RowHeight = 28;
                    break;
                case ExcelReporter.Height.Tall:
                    _worksheet.get_Range("A2", finalCellIndex).EntireRow.RowHeight = 38;
                    break;
                default:
                    break;
            }

            if (sheetContent.DrawChart)
            {
                DrawChartOnSheet(strSheetName,
                    categoryTitle,
                    "values",
                    columnTitles.Count,
                    finalRawIndex,
                    finalColumnIndex,
                    finalCellIndex,
                    columnTitles);
            }

            _currentSheetIndex++;
        }
                
        /// <summary>
        /// Save excel workbook.
        /// </summary>
        public void SaveExcel()
        {
            object missing = Type.Missing;
            _Worksheet activeSheet = _workbook.Sheets[1] as _Worksheet;
            if (activeSheet == null)
            {
                throw new InvalidCastException("Error in getting Excel object model.");
            }

            activeSheet.Activate();

            // delete output file if exists
            Helper.EnsureFolderExistForFile(_fileName);
            Helper.SafeDelete(_fileName);
            _workbook.SaveAs(_fileName, missing, missing, missing,
                missing, missing, XlSaveAsAccessMode.xlShared, missing, missing,
                missing, missing, missing);
            _workbook.Saved = true;
        }

        /// <summary>
        /// Close workbook and quit excel.
        /// </summary>
        public void CloseExcel()
        {
            if (_excel != null)
            {
                if (_excel.ActiveWorkbook != null)
                {
                    _excel.ActiveWorkbook.Close(false, Type.Missing, false);
                }

                _excel.UserControl = false;
                _excel.Quit();
                _excel = null;
                GC.Collect();
            }
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Dispose resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose resources.
        /// </summary>
        /// <param name="disposing">Explicit dispose.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                CloseExcel();
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Get column index by integer index.
        /// </summary>
        /// <param name="index">The integer index.</param>
        /// <returns>String.</returns>
        private string GetColumnIndex(int index)
        {
            // there are 26 English characters
            const int EnCharNumber = 26;
            string strIndex = string.Empty;

            // the input index starts from 1
            // substact by 1, then starts from 0
            index -= 1;
            strIndex = ((char)((index % EnCharNumber) + 'A')).ToString();

            index /= EnCharNumber;
            while (index > 0)
            {
                char columnIndex = (char)((index % EnCharNumber) + 'A');
                strIndex = columnIndex.ToString() + strIndex;

                index /= EnCharNumber;
            }

            return strIndex;
        }

        /// <summary>
        /// Draw chart for a specific sheet (specifically, distribution chart or similar chart)
        /// Format of the cell:
        ///     Column 1:  the X asix name + X asix values
        ///     Column 2 ... finalColumnIndex:  The Y asix name + Y asix values.
        /// </summary>
        /// <param name="chartTitle">The title of the chart.</param>
        /// <param name="categoryTitle">The category title.</param>
        /// <param name="valueTitle">The value title.</param>
        /// <param name="seriesNum">The number of the serieses.</param>
        /// <param name="finalRawIndex">The final raw index.</param>
        /// <param name="finalCoumnIndex">The final column index.</param>
        /// <param name="finalCellIndex">The final cell index.</param>
        /// <param name="seriesNames">The names of the series.</param>
        private void DrawChartOnSheet(string chartTitle,
            string categoryTitle,
            string valueTitle,
            int seriesNum,
            string finalRawIndex,
            string finalCoumnIndex,
            string finalCellIndex,
            Collection<string> seriesNames)
        {
            Series objSeries;
            Range objRange;
            _Chart objChart;

            // Add a Chart for the selected data.
            objChart = (_Chart)_workbook.Charts.Add(Missing.Value,
                _worksheet,
                1,
                Missing.Value);

            // select the data to be drawed
            objRange = _worksheet.get_Range("B2", finalCellIndex);

            // Use the ChartWizard to create a new chart from the selected data.
            objChart.ChartWizard(objRange, XlChartType.xlLineMarkers, Missing.Value,
                XlRowCol.xlColumns, seriesNum, seriesNames, true,
                chartTitle, categoryTitle, valueTitle, Missing.Value);

            // Delete the first series because it is the X axis
            objSeries = (Series)objChart.SeriesCollection(1);
            objSeries.Delete();

            // Set the X axis values
            objSeries = (Series)objChart.SeriesCollection(1);
            objSeries.XValues = _worksheet.get_Range("A2:" + "A" + finalRawIndex, Missing.Value);

            // Attach the chart to the sheet
            objChart.Location(XlChartLocation.xlLocationAsObject, _worksheet.Name);

            // Move the chart so as not to cover the data.
            objRange = (Range)_worksheet.Rows.get_Item(2, Missing.Value);
            _worksheet.Shapes.Item("Chart 1").Top = (float)(double)objRange.Top;
            objRange = (Range)_worksheet.Columns.get_Item((int)(seriesNum + 3), Missing.Value);
            _worksheet.Shapes.Item("Chart 1").Left = (float)(double)objRange.Left;
        }

        /// <summary>
        /// Highlight specific text segement .
        /// </summary>
        /// <param name="cell">The cell to work on.</param>
        /// <param name="coloredScript">The colored script.</param>
        private void SetColoredTextScript(Range cell, ExcelReporter.ColoredScript coloredScript)
        {
            // set the value
            cell.Value = coloredScript.Text;

            // set the color
            foreach (ExcelReporter.ColoredTextSegment segment in coloredScript.ColoredTextSegments)
            {
                Characters characters = cell.get_Characters(segment.StartIndex, segment.Length);
                characters.Font.ColorIndex = segment.Color;
            }
        }

        /// <summary>
        /// Set hyperlinks.
        /// </summary>
        /// <param name="cell">The cell to work on.</param>
        /// <param name="hyperlink">The hyperlinks to be associated in the given cell.</param>
        private void SetHyperlinks(Range cell, ExcelReporter.Hyperlink hyperlink)
        {
            // set the value
            cell.Hyperlinks.Add(cell, hyperlink.Address, Type.Missing, Type.Missing, hyperlink.TextToDisplay);
        }

        #endregion
    }
}