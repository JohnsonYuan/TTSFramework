//----------------------------------------------------------------------------
// <copyright file="GainEditor.xaml.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      The implement of Gain editor.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.UI.Controls.Acoustic
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Text;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Navigation;
    using System.Windows.Shapes;
    using Data;
    using Microsoft.Tts.UI.Controls.Trajectory;
    using Microsoft.Tts.UI.Controls.Trajectory.Data;

    /// <summary>
    /// Interaction logic for GainEditor.xaml.
    /// </summary>
    public partial class GainEditor : UserControl
    {
        private VisualGain _visualGain = new VisualGain();

        private bool _fIsDragging = false;

        private bool _fHoverSeleting = false;

        public GainEditor()
        {
            InitializeComponent();
            _linerGraph.CanvasMouseCapture = true;
            MouseEventBroadcast.SetupBroadcast(_grid);
        }

        public void SetDataContext(VisualGain visualGain)
        {
            _linerGraph.SetDataContext(visualGain);
            VisualLinerSamples guideline = new VisualLinerSamples();
            guideline.TimeAxis = visualGain.TimeAxis;
            guideline.YAxis = visualGain.YAxis;
            guideline.Samples = visualGain.GuideLineSamples;
            _guidelineGraph.SetDataContext(guideline);
            _highlightFrames.TimeAxis = visualGain.TimeAxis;
            _visualGain = visualGain;
        }

        private void OnResetTuneMenuItemClick(object sender, RoutedEventArgs e)
        {
            Cursor old = _linerGraph.Cursor;
            _linerGraph.Cursor = Cursors.Wait;
            Collection<int> selected = _highlightFrames.SelectedFrames;
            _visualGain.Samples.StartTransaction();
            foreach (int n in selected)
            {
                _visualGain.Samples[n] = _visualGain.GuideLineSamples[n];
            }

            _visualGain.Samples.EndTransaction();
            _linerGraph.Cursor = old;
        }

        private void OnDragSelectedMenuItemClick(object sender, RoutedEventArgs e)
        {
            _linerGraph.IsEditable = false;
            _linerGraph.IsForceUpdateSamples = true;
            _linerGraph.Cursor = Cursors.Hand;
            _fIsDragging = true;
            _visualGain.Samples.StartTransaction();
        }

        private void OnSelectFramesMenuItemClick(object sender, RoutedEventArgs e)
        {
            if (!_fIsDragging)
            {
                _fHoverSeleting = true;
                _linerGraph.IsEditable = false;
                _highlightFrames.IsHoverSelect = true;
            }
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_fIsDragging)
            {
                _linerGraph.Cursor = Cursors.Wait;
                _visualGain.Samples.EndTransaction();
                _fIsDragging = false;
                _linerGraph.IsEditable = true;
                _linerGraph.IsForceUpdateSamples = false;
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_fIsDragging)
            {
                Collection<int> selected = _highlightFrames.SelectedFrames;
                if (selected.Count > 0)
                {
                    // Drag selected frames.
                    Point point = e.GetPosition(_linerGraph);
                    int nIndex = 0;
                    double sampleValue = 0;
                    _linerGraph.CreateSample(point, out nIndex, out sampleValue);
                    if (_linerGraph.IsValidSampleValue(_visualGain.Samples[nIndex]) &&
                        selected.Contains(nIndex))
                    {
                        double delta = sampleValue - _visualGain.Samples[nIndex];
                        foreach (int n in selected)
                        {
                            _visualGain.Samples[n] += delta;
                        }
                    }
                }
            }
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_fHoverSeleting)
            {
                _fHoverSeleting = false;
                _linerGraph.IsEditable = true;
                _highlightFrames.IsHoverSelect = false;
            }
        }
    }
}