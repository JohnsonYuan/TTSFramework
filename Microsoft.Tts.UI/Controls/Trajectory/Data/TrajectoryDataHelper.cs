//----------------------------------------------------------------------------
// <copyright file="TrajectoryDataHelper.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      class of visual graph elements
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.UI.Controls.Trajectory.Data
{
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    ////using Microsoft.Tts.Offline.Utility;
    using Microsoft.Tts.UI.Controls.Data;
    using Microsoft.Tts.UI.Controls.Trajectory;

    public class TrajectoryHelper
    {
        /// <summary>
        /// Calculate delta and acceleration parameters.
        ///     Currently here uses a fixed algorithm which
        ///           Delta(n) = (Static(n + 1) - Static(n - 1)) / 2
        ///           Acc(n) = (Static(n + 1) - 2 * Static(n) + Static(n - 1)
        ///     This shall be refined to user service provider to get the algorithm.
        /// </summary>
        /// <param name="index">Index values.</param>
        /// <param name="statics">Static values.</param>
        /// <param name="deltas">Delta values.</param>
        /// <param name="accelerations">Acceleration values.</param>
        public static void CalculateDeltaAndAccParameters(int index, Collection<double> statics,
            out double deltas, out double accelerations)
        {
            double leftValue = index - 1 >= 0 ? statics[index - 1] : IntervalLinerGraph.DefaultLowerBound;
            double rightValue = index + 1 < statics.Count ? statics[index + 1] : IntervalLinerGraph.DefaultLowerBound;
            double currentValue = statics[index];
            if (leftValue == IntervalLinerGraph.DefaultLowerBound ||
                rightValue == IntervalLinerGraph.DefaultLowerBound)
            {
                deltas = IntervalLinerGraph.DefaultLowerBound;
                accelerations = IntervalLinerGraph.DefaultLowerBound;
            }
            else
            {
                deltas = (rightValue - leftValue) / 2.0;
                if (currentValue == IntervalLinerGraph.DefaultLowerBound)
                {
                    accelerations = IntervalLinerGraph.DefaultLowerBound;
                }
                else
                {
                    accelerations = leftValue - (2.0 * currentValue) + rightValue;
                }
            }
        }

        /// <summary>
        /// Find segment by frame index.
        /// </summary>
        /// <param name="segments">Segment list.</param>
        /// <param name="frameIndex">Frame index.</param>
        /// <param name="segmentIndex">Segment index.</param>
        /// <returns>Segment found.</returns>
        public static VisualSegment FindSegment(Collection<VisualSegment> segments,
            int frameIndex, out int segmentIndex)
        {
            Debug.Assert(segments != null, "Segment collection should not be null.");
            Debug.Assert(ValidateSegments(segments), "Segment collection is invalid.");

            VisualSegment result = null;
            segmentIndex = -1;
            for (int index = 0; index < segments.Count; ++index)
            {
                if (segments[index].StartFrameIndex <= frameIndex &&
                    segments[index].EndFrameIndex >= frameIndex)
                {
                    result = segments[index];
                    segmentIndex = index;
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Build trajectory information.
        /// </summary>
        /// <param name="dimensionGraphData">Dimension graph data.</param>
        /// <param name="mousePos">Mouse position.</param>
        /// <param name="parameters">Parameter list.</param>
        /// <param name="auxiliaryParameters">Auxiliary parameters.</param>
        /// <param name="means">Mean list.</param>
        /// <param name="deviations">Deviation list.</param>
        /// <param name="trajectoryInfo">Trajectory info.</param>
        public static void BuildTrajectoryInfo(VisualDimensionGraph dimensionGraphData,
            double mousePos, Collection<double> parameters,
            Collection<double> auxiliaryParameters, Collection<double> means,
            Collection<double> deviations, VisualTrajectoryInfo trajectoryInfo)
        {
            double time = dimensionGraphData.TimeAxis.StartingTime +
                ViewHelper.PixelToTimeSpan(mousePos, dimensionGraphData.TimeAxis.ZoomScale);
            trajectoryInfo.Time = time / 1000.0;
            int frameIndex = (int)Math.Floor(time / dimensionGraphData.TimeAxis.SampleInterval);
            if (frameIndex >= means.Count)
            {
                frameIndex = means.Count - 1; // the graph board has width
            }

            trajectoryInfo.FrameIndex = frameIndex;
            if (parameters.Count > frameIndex)
            {
                trajectoryInfo.GeneratedParameter = parameters[frameIndex];
            }
            else
            {
                trajectoryInfo.GeneratedParameter = double.NegativeInfinity;
            }

            trajectoryInfo.Mean = means[frameIndex];
            trajectoryInfo.StandardDeviation = deviations[frameIndex];

            int segIndex = 0;
            VisualSegment segment = FindSegment(dimensionGraphData.WordSegments, frameIndex, out segIndex);
            if (segment != null)
            {
                trajectoryInfo.Word = segment.Text;
            }

            segment = FindSegment(dimensionGraphData.PhoneSegments, frameIndex, out segIndex);

            if (segment != null)
            {
                trajectoryInfo.Phone = segment.Text;
            }
            
            trajectoryInfo.ShowCandidatesParameter = dimensionGraphData.DisplayController.TrajectoryDisplayController.AuxiliaryTrajectoryShowed;
            if (trajectoryInfo.ShowCandidatesParameter)
            {
                if (auxiliaryParameters.Count > frameIndex)
                {
                    trajectoryInfo.CandidatesParameter = auxiliaryParameters[frameIndex];
                }
                else
                {
                    trajectoryInfo.CandidatesParameter = double.NegativeInfinity;
                }
            }
        }

        /// <summary>
        /// Generate selected frame indexes.
        /// </summary>
        /// <param name="mousePos">Mouse X-axis position.</param>
        /// <param name="timeAxis">Graph data.</param>
        /// <param name="selectedFrameIndexes">Selected indexes.</param>
        public static void GenerateSelectedFrameIndexes(double mousePos,
            VisualTimeAxis timeAxis, Collection<int> selectedFrameIndexes)
        {
            selectedFrameIndexes.Clear();
            if (timeAxis.ZoomMode == ZoomMode.FrameLevel)
            {
                int selectedIndex = GetFrameIndex(mousePos, timeAxis);
                selectedFrameIndexes.Add(selectedIndex);
            }
        }

        /// <summary>
        /// Get frame index.
        /// </summary>
        /// <param name="mousePos">Mouse X-axis position.</param>
        /// <param name="timeAxis">Graph data.</param>
        /// <returns>Frame index.</returns>
        public static int GetFrameIndex(double mousePos, VisualTimeAxis timeAxis)
        {
            int selectedIndex = -1;
            if (timeAxis.ZoomMode == ZoomMode.FrameLevel)
            {
                double actualTime = ViewHelper.PixelToTimeSpan(mousePos, timeAxis.ZoomScale);
                selectedIndex = (int)Math.Floor((actualTime + timeAxis.StartingTime) /
                    timeAxis.SampleInterval);
            }

            return selectedIndex;
        }

        /// <summary>
        /// Validate segments by checking the order of segments.
        /// </summary>
        /// <param name="segments">Segment collection.</param>
        /// <returns>True if valid.</returns>
        private static bool ValidateSegments(Collection<VisualSegment> segments)
        {
            bool valid = true;
            int currentFrameIndex = -1;
            for (int segIndex = 0; segIndex < segments.Count; ++segIndex)
            {
                VisualSegment segment = segments[segIndex];
                if (currentFrameIndex >= 0 && segment.StartFrameIndex != currentFrameIndex)
                {
                    valid = false;
                }

                // Segments may not have any frames (like punctuation won't generate any frames). 
                //     Its StartFrameIndex = EndFrameIndex + 1.
                if (segment.StartFrameIndex > segment.EndFrameIndex + 1)
                {
                    valid = false;
                }

                if (!valid)
                {
                    break;
                }

                currentFrameIndex = segment.EndFrameIndex + 1;
            }

            return valid;
        }
    }
}