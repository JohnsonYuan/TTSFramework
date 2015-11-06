//----------------------------------------------------------------------------
// <copyright file="VisualLspErrorData.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      class of VisualLspErrorData
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.UI.Controls.Trajectory.Data
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using Microsoft.Tts.Offline.Utility;
    using Microsoft.Tts.UI.Controls.Data;

    /// <summary>
    /// Class of view data of LSP error.
    /// </summary>
    public class VisualLspErrorData : ViewDataBase
    {
        #region fields

        private const string PreviousPagesLabel = "<<";
        private const string NextPagesLabel = ">>";
        private const int MaxDisplayedPageCount = 20;

        private int entryCountPerPage = 100;
        private int maxDisplayedPageIndexCount = 3;

        private int _errorCount = 0;
        private bool _loadingData = false;
        private int _currentPage = 0;

        private bool _sortByFrame = false;
        private List<VisualErrorData> _errorDataSource = new List<VisualErrorData>();

        #endregion

        #region constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualLspErrorData"/> class.
        /// </summary>
        public VisualLspErrorData()
        {
            ErrorData = new ObservableCollection<VisualErrorData>();
            Pages = new ObservableCollection<string>();
            ErrorFrames = new List<int>();
            Reset();
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets or sets dimension interval.
        /// </summary>
        public int DimensionInterval { get; set; }

        /// <summary>
        /// Gets or sets minimum threshold.
        /// </summary>
        public double MinThreshold { get; set; }

        /// <summary>
        /// Gets or sets maximum threshold.
        /// </summary>
        public double MaxThreshold { get; set; }

        /// <summary>
        /// Gets max page count.
        /// </summary>
        public int MaxPageCount
        {
            get
            {
                return (int)Math.Ceiling((double)_errorDataSource.Count / (double)entryCountPerPage);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to sort by frame index.
        /// </summary>
        public bool SortByFrame
        {
            get
            {
                return _sortByFrame; 
            }

            set 
            { 
                _sortByFrame = value;
                NotifyPropertyChanged("SortByFrame");
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether it's loading data.
        /// </summary>
        public bool LoadingData
        {
            get
            {
                return _loadingData;
            }

            set
            {
                _loadingData = value;
                NotifyPropertyChanged("LoadingData");
            }
        }

        /// <summary>
        /// Gets or sets error count.
        /// </summary>
        public int ErrorCount 
        {
            get
            {
                return _errorCount;
            }

            set
            {
                _errorCount = value;
                NotifyPropertyChanged("ErrorCount");
            }
        }

        /// <summary>
        /// Gets or sets diplayed error data.
        /// </summary>
        public ObservableCollection<VisualErrorData> ErrorData { get; set; }

        /// <summary>
        /// Gets or sets page indexes.
        /// </summary>
        public ObservableCollection<string> Pages { get; set; }

        /// <summary>
        /// Gets error frames.
        /// </summary>
        public List<int> ErrorFrames { get; private set; }

        #endregion

        #region methods

        /// <summary>
        /// Reset.
        /// </summary>
        public void Reset()
        {
            ErrorCount = 0;
            DimensionInterval = -1;
            MinThreshold = double.NaN;
            MaxThreshold = double.NaN;
            _errorDataSource.Clear();
            ErrorData.Clear();
            Pages.Clear();
            ErrorFrames.Clear();
        }

        /// <summary>
        /// Build error data.
        /// </summary>
        /// <param name="data">Trajectory data.</param>
        public void BuildErrorData(List<VisualSingleTrajectory> data)
        {
            Debug.Assert(data != null);

            if (DimensionInterval < 1 || DimensionInterval >= data.Count)
            {
                throw new InvalidDataException("Dimension interval is not properly set.");
            }

            if (double.IsNaN(MinThreshold) && double.IsNaN(MaxThreshold))
            {
                throw new InvalidDataException("Threshold is not properly set.");
            }

            if (MinThreshold > MaxThreshold)
            {
                throw new InvalidDataException("Max threshold should be larger or equal to min threadshold.");
            }

            Debug.Assert(ValidateSourceData(data));

            MinThreshold = double.IsNaN(MinThreshold) ? int.MinValue : MinThreshold;
            MaxThreshold = double.IsNaN(MaxThreshold) ? int.MaxValue : MaxThreshold;

            _errorDataSource.Clear();
            ErrorFrames.Clear();
            int frameCount = data[0].Means.Count;
            int errorCount = -1;
            for (int frameIndex = 0; frameIndex < frameCount; ++frameIndex)
            {
                bool hasError = false;
                for (int trajIndex = 0; trajIndex < data.Count - DimensionInterval; ++trajIndex)
                {
                    double interval = data[trajIndex + DimensionInterval].GeneratedParameters[frameIndex] -
                        data[trajIndex].GeneratedParameters[frameIndex];
                    if (interval < MinThreshold || interval > MaxThreshold)
                    {
                        hasError = true;
                        if (++errorCount < MaxDisplayedPageCount * entryCountPerPage)
                        {
                            VisualErrorData errorData = new VisualErrorData(errorCount,
                                frameIndex, interval, trajIndex + 1, trajIndex + DimensionInterval + 1);
                            _errorDataSource.Add(errorData);
                        }
                    }
                }

                if (hasError)
                {
                    ErrorFrames.Add(frameIndex);
                }
            }

            if (!SortByFrame)
            {
                _errorDataSource.Sort();
            }

            ErrorCount = errorCount;
        }

        /// <summary>
        /// Update page indexes.
        /// </summary>
        /// <param name="currentPage">Current page index.</param>
        public void UpdatePages(int currentPage)
        {
            Pages.Clear();
            _currentPage = currentPage;
            if (_currentPage > 0)
            {
                Pages.Add(PreviousPagesLabel);
            }

            for (; currentPage < _currentPage + maxDisplayedPageIndexCount &&
                currentPage < MaxPageCount && currentPage < MaxDisplayedPageCount;
                ++currentPage)
            {
                Pages.Add((currentPage + 1).ToString());
            }

            if (currentPage < MaxPageCount - 1)
            {
                Pages.Add(NextPagesLabel);
            }
        }

        /// <summary>
        /// Fetch error data.
        /// </summary>
        /// <param name="pageIndex">Page index.</param>
        public void FetchErrorData(string pageIndex)
        {
            Debug.Assert(pageIndex != null);

            int curPageIndex = _currentPage;
            switch (pageIndex)
            {
                case PreviousPagesLabel:
                    curPageIndex -= maxDisplayedPageIndexCount;
                    curPageIndex = curPageIndex >= 0 ? curPageIndex : 0;
                    UpdatePages(curPageIndex);
                    break;
                case NextPagesLabel:
                    curPageIndex += maxDisplayedPageIndexCount;
                    curPageIndex = curPageIndex + maxDisplayedPageIndexCount < MaxPageCount ?
                        curPageIndex : MaxPageCount - maxDisplayedPageIndexCount;
                    UpdatePages(curPageIndex);
                    break;
                default:
                    bool succeeded = int.TryParse(pageIndex, out curPageIndex);
                    Debug.Assert(succeeded && curPageIndex > 0);
                    curPageIndex -= 1;
                    ErrorData.Clear();
                    int startIndex = curPageIndex * entryCountPerPage;
                    int endIndex = (curPageIndex + 1) * entryCountPerPage;
                    for (int index = startIndex; index < endIndex && index < _errorDataSource.Count; ++index)
                    {
                        ErrorData.Add(_errorDataSource[index]);
                    }

                    curPageIndex = curPageIndex - 1 >= 0 ? curPageIndex - 1 : 0;
                    UpdatePages(curPageIndex);
                    break;
            }
        }

        /// <summary>
        /// Validate source data.
        /// </summary>
        /// <param name="data">Trajectory data.</param>
        /// <returns>True if data is valid.</returns>
        private bool ValidateSourceData(List<VisualSingleTrajectory> data)
        {
            Debug.Assert(data != null);

            bool valid = true;
            if (data == null)
            {
                valid = false;
            }
            else
            {
                int frameCount = data[0].Means.Count;
                foreach (VisualSingleTrajectory traj in data)
                {
                    valid = traj.GeneratedParameters.Count == frameCount;
                    if (!valid)
                    {
                        break;
                    }
                }
            }

            return valid;
        }

        #endregion

        #region sub-class

        /// <summary>
        /// Class of VisualErrorData.
        /// </summary>
        public class VisualErrorData : ViewDataBase, IComparable
        {
            #region constructor

            /// <summary>
            /// Initializes a new instance of the <see cref="VisualErrorData"/> class.
            /// </summary>
            /// <param name="errorIndex">Error index.</param>
            /// <param name="frameIndex">Frame index.</param>
            /// <param name="errorValue">Error value.</param>
            /// <param name="startDimensionIndex">Start dimension index.</param>
            /// <param name="endDimensionIndex">End dimension index.</param>
            public VisualErrorData(int errorIndex, int frameIndex, double errorValue, int startDimensionIndex,
                int endDimensionIndex)
            {
                ErrorIndex = errorIndex;
                FrameIndex = frameIndex;
                ErrorValue = errorValue;
                StartDimensionIndex = startDimensionIndex;
                EndDimensionIndex = endDimensionIndex;
            }

            #endregion

            #region properties

            /// <summary>
            /// Gets error index.
            /// </summary>
            public int ErrorIndex { get; private set; }

            /// <summary>
            /// Gets frame index.
            /// </summary>
            public int FrameIndex { get; private set; }

            /// <summary>
            /// Gets error value.
            /// </summary>
            public double ErrorValue { get; private set; }

            /// <summary>
            /// Gets start dimension index.
            /// </summary>
            public int StartDimensionIndex { get; private set; }

            /// <summary>
            /// Gets end dimension index.
            /// </summary>
            public int EndDimensionIndex { get; private set; }

            /// <summary>
            /// Gets error entry.
            /// </summary>
            public string ErrorEntry
            {
                get
                {
                    return Helper.NeutralFormat("Frame {0}: {1}({2},{3})", FrameIndex,
                        ErrorValue.ToString("0.0000"), StartDimensionIndex, EndDimensionIndex);
                }
            }

            #endregion

            #region IComparable Members

            /// <summary>
            /// Compare function.
            /// </summary>
            /// <param name="obj">Object to compare.</param>
            /// <returns>Compare result.</returns>
            public int CompareTo(object obj)
            {
                int result = 0;
                if (obj is VisualErrorData)
                {
                    VisualErrorData data = obj as VisualErrorData;
                    result = ErrorValue.CompareTo(data.ErrorValue);
                    if (result == 0)
                    {
                        result = FrameIndex.CompareTo(data.FrameIndex);
                    }

                    if (result == 0)
                    {
                        result = StartDimensionIndex.CompareTo(data.StartDimensionIndex);
                    }
                }
                else
                {
                    throw new ArgumentException();
                }

                return result;
            }

            #endregion
        }

        #endregion
    }
}