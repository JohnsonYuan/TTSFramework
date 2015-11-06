//----------------------------------------------------------------------------
// <copyright file="VisualDimensionGraph.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      class of VisualDimensionGraph
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.UI.Controls.Trajectory.Data
{
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using Microsoft.Tts.Offline.Utility;
    using Microsoft.Tts.UI.Controls.Trajectory;

    /// <summary>
    /// Class of VisualDimensionGraph.
    /// </summary>
    public class VisualDimensionGraph
    {
        #region contructor

        /// <summary>
        /// Prevents a default instance of the <see cref="VisualDimensionGraph"/> class from being created.
        /// </summary>
        private VisualDimensionGraph()
        {
            DisplayController = new VisualDisplayController();
            TimeAxis = new VisualTimeAxis();
            WordSegments = new Collection<VisualSegment>();
            PhoneSegments = new Collection<VisualSegment>();
            SelectedFrameIndexes = new ObservableCollection<int>();
            PhoneDurations = new VisualDurations();
            WaveSamples = new VisualLinerSamples();
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets StaticTrajectory view data.
        /// </summary>
        public VisualTrajectoryBase StaticTrajectory { get; private set; }

        /// <summary>
        /// Gets DeltaTrajectory view data.
        /// </summary>
        public VisualTrajectoryBase DeltaTrajectory { get; private set; }

        /// <summary>
        /// Gets AccelerationTrajectory view data.
        /// </summary>
        public VisualTrajectoryBase AccelerationTrajectory { get; private set; }

        /// <summary>
        /// Gets display controller data.
        /// </summary>
        public VisualDisplayController DisplayController { get; private set; }

        /// <summary>
        /// Gets time axis data.
        /// </summary>
        public VisualTimeAxis TimeAxis { get; private set; }

        /// <summary>
        /// Gets word segments.
        /// </summary>
        public Collection<VisualSegment> WordSegments { get; private set; }

        /// <summary>
        /// Gets phone segments.
        /// </summary>
        public Collection<VisualSegment> PhoneSegments { get; private set; }

        /// <summary>
        /// Gets Selected frame index.
        /// </summary>
        public ObservableCollection<int> SelectedFrameIndexes { get; private set; }

        /// <summary>
        /// Gets selected dimensions.
        /// </summary>
        public ObservableCollection<int> SelectedDimensions { get; private set; }

        /// <summary>
        /// Gets or sets The phone durations.
        /// </summary>
        public VisualDurations PhoneDurations { get; set; }

        /// <summary>
        /// Gets or sets The wave samples.
        /// </summary>
        public VisualLinerSamples WaveSamples { get; set; }

        #endregion

        #region methods

        /// <summary>
        /// Create a view data for MultiDimensionGraph.
        /// </summary>
        /// <returns>Visual dimension graph view data.</returns>
        public static VisualDimensionGraph CreateMultiDimensionGraphData()
        {
            VisualDimensionGraph data = new VisualDimensionGraph();
            data.SelectedDimensions = new ObservableCollection<int>();
            data.StaticTrajectory = new VisualMultiTrajectory(data.SelectedDimensions);
            data.StaticTrajectory.TimeAxis = data.TimeAxis;
            data.DeltaTrajectory = new VisualMultiTrajectory(data.SelectedDimensions);
            data.DeltaTrajectory.TimeAxis = data.TimeAxis;
            data.AccelerationTrajectory = new VisualMultiTrajectory(data.SelectedDimensions);
            data.AccelerationTrajectory.TimeAxis = data.TimeAxis;
            data.PhoneDurations.TimeAxis = data.TimeAxis;
            return data;
        }

        /// <summary>
        /// Create a view data for SingleDimensionGraph.
        /// </summary>
        /// <returns>Visual dimension graph view data.</returns>
        public static VisualDimensionGraph CreateSingleDimensionGraphData()
        {
            VisualDimensionGraph data = new VisualDimensionGraph();
            data.StaticTrajectory = new VisualSingleTrajectory();
            data.StaticTrajectory.TimeAxis = data.TimeAxis;
            data.DeltaTrajectory = new VisualSingleTrajectory();
            data.DeltaTrajectory.TimeAxis = data.TimeAxis;
            data.AccelerationTrajectory = new VisualSingleTrajectory();
            data.AccelerationTrajectory.TimeAxis = data.TimeAxis;
            data.PhoneDurations.TimeAxis = data.TimeAxis;
            return data;
        }

        /// <summary>
        /// Clear data.
        /// </summary>
        public void Clear()
        {
            WordSegments.Clear();
            PhoneSegments.Clear();
            StaticTrajectory.Clear();
            DeltaTrajectory.Clear();
            AccelerationTrajectory.Clear();
        }

        /// <summary>
        /// Update time axis.
        /// </summary>
        /// <param name="frameCount">Frame count.</param>
        public void UpdateTimeAxis(int frameCount)
        {
            StaticTrajectory.CalculateValueRange();
            DeltaTrajectory.CalculateValueRange();
            AccelerationTrajectory.CalculateValueRange();

            TimeAxis.SetDuration(frameCount);
            if (TimeAxis.StartingTime > TimeAxis.Duration)
            {
                TimeAxis.StartingTime = TimeAxis.Duration;
            }
        }

        /// <summary>
        /// Set starting time according to selected word index.
        /// </summary>
        /// <param name="wordIndex">Word index.</param>
        /// <param name="width">Graph width.</param>
        public void SetStartingTimeToWord(int wordIndex, double width)
        {
            Debug.Assert(wordIndex < WordSegments.Count && wordIndex >= 0,
                "Invalid word index");
            Debug.Assert(width >= 0, "Width should be larger than zero.");
            Debug.Assert(TimeAxis.Duration > 0, "Duration should be larger than zero.");

            if (TimeAxis.ZoomMode == ZoomMode.FrameLevel)
            {
                int frameIndex = WordSegments[wordIndex].StartFrameIndex;
                double startingTime = TimeAxis.SampleInterval * frameIndex;
                startingTime = Math.Min(startingTime, TimeAxis.Duration - width);
                if (startingTime < 0)
                {
                    startingTime = 0;
                }

                startingTime = Math.Floor(startingTime / TimeAxis.SampleInterval) * TimeAxis.SampleInterval;
                TimeAxis.StartingTime = startingTime;
            }
        }

        /// <summary>
        /// Set starting time according to selected phone index.
        /// </summary>
        /// <param name="phoneIndex">Phone index.</param>
        /// <param name="width">Graph width.</param>
        public void SetStartingTimeToPhone(int phoneIndex, double width)
        {
            Debug.Assert(phoneIndex < PhoneSegments.Count && phoneIndex >= 0,
                "Invalid phone index");
            Debug.Assert(width >= 0, "Width should be larger than zero.");
            Debug.Assert(TimeAxis.Duration > 0, "Duration should be larger than zero.");

            if (TimeAxis.ZoomMode == ZoomMode.FrameLevel)
            {
                int frameIndex = PhoneSegments[phoneIndex].StartFrameIndex;
                double startingTime = TimeAxis.SampleInterval * frameIndex;
                startingTime = Math.Min(startingTime, TimeAxis.Duration - width);
                if (startingTime < 0)
                {
                    startingTime = 0;
                }

                startingTime = Math.Floor(startingTime / TimeAxis.SampleInterval) * TimeAxis.SampleInterval;
                TimeAxis.StartingTime = startingTime;
            }
        }

        /// <summary>
        /// Set starting time to specific frame.
        /// </summary>
        /// <param name="frameIndex">Frame index.</param>
        /// <param name="width">Graph width.</param>
        public void SetStartingTimeToFrame(int frameIndex, double width)
        {
            Debug.Assert(frameIndex >= 0);
            Debug.Assert(width >= 0, "Width should be larger than zero.");
            Debug.Assert(TimeAxis.Duration > 0, "Duration should be larger than zero.");

            if (TimeAxis.ZoomMode == ZoomMode.FrameLevel)
            {
                double startingTime = TimeAxis.SampleInterval * frameIndex;
                Debug.Assert(startingTime <= TimeAxis.Duration);
                double halfWidthDur = ViewHelper.PixelToTimeSpan(width / 2.0, TimeAxis.ZoomScale);
                startingTime -= halfWidthDur;
                startingTime = Math.Max(startingTime, 0);
                startingTime = Math.Min(startingTime, TimeAxis.Duration - (2.0 * halfWidthDur));
                if (startingTime < 0)
                {
                    startingTime = 0;
                }

                startingTime = Math.Floor(startingTime / TimeAxis.SampleInterval) * TimeAxis.SampleInterval;
                TimeAxis.StartingTime = startingTime;
            }
        }

        #endregion
    }
}