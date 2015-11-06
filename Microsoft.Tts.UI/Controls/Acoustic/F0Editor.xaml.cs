//----------------------------------------------------------------------------
// <copyright file="F0Editor.xaml.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      The implement of F0 editor.
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
    /// Interaction logic for F0Editor.xaml.
    /// </summary>
    public partial class F0Editor : UserControl
    {
        private VisualF0 _visualF0 = new VisualF0();

        private bool _fIsDragging = false;

        private bool _fHoverSeleting = false;

        private bool _isEnableEdit = false;

        public F0Editor()
        {
            InitializeComponent();
            _linerGraph.CanvasMouseCapture = true;
            MouseEventBroadcast.SetupBroadcast(_grid);
            IsEnableEdit = true;
        }

        public bool IsEnableEdit
        {
            get
            {
                return _isEnableEdit;
            }

            set
            {
                _isEnableEdit = value;
                _linerGraph.IsEditable = _isEnableEdit;
            }
        }

        public Collection<int> SelectedFrames
        {
            get
            {
                return _highlightFrames.SelectedFrames;
            }
        }

        public void RemoveContextMenu()
        {
            _grid.ContextMenu = null;
        }

        public void SetUpContextMenu()
        {
            _grid.ContextMenu = Resources["F0EditorContextMenu"] as ContextMenu;
        }

        public void SetDataContext(VisualF0 visualF0)
        {
            _linerGraph.SetDataContext(visualF0);
            VisualLinerSamples guidelineSamples = new VisualLinerSamples();
            guidelineSamples.TimeAxis = visualF0.TimeAxis;
            guidelineSamples.YAxis = visualF0.YAxis;
            guidelineSamples.Samples = visualF0.GuideLineSamples;
            _guidelineGraph.SetDataContext(guidelineSamples);
            _highlightFrames.TimeAxis = visualF0.TimeAxis;
            _visualF0 = visualF0;
        }

        private void OnResetTuneMenuItemClick(object sender, RoutedEventArgs e)
        {
            Cursor old = _linerGraph.Cursor;
            _linerGraph.Cursor = Cursors.Wait;
            Collection<int> selected = _highlightFrames.SelectedFrames;
            _visualF0.Samples.StartTransaction();
            foreach (int n in selected)
            {
                _visualF0.Samples[n] = _visualF0.GuideLineSamples[n];
            }

            _visualF0.Samples.EndTransaction();
            _linerGraph.Cursor = old;
        }

        private void OnDragSelectedMenuItemClick(object sender, RoutedEventArgs e)
        {
            if (!_isEnableEdit)
            {
                return;
            }

            _linerGraph.IsEditable = false;
            _linerGraph.IsForceUpdateSamples = true;
            _linerGraph.Cursor = Cursors.Hand;
            _fIsDragging = true;
            _visualF0.Samples.StartTransaction();
        }

        private void OnSelectFramesMenuItemClick(object sender, RoutedEventArgs e)
        {
            if (!_isEnableEdit)
            {
                return;
            }

            if (!_fIsDragging)
            {
                _fHoverSeleting = true;
                _linerGraph.IsEditable = false;
                _highlightFrames.IsHoverSelect = true;
            }
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!_isEnableEdit)
            {
                return;
            }

            if (_fIsDragging)
            {
                _linerGraph.Cursor = Cursors.Wait;
                _visualF0.Samples.EndTransaction();
                _fIsDragging = false;
                _linerGraph.IsEditable = true;
                _linerGraph.IsForceUpdateSamples = false;
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isEnableEdit)
            {
                return;
            }

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
                    if (_linerGraph.IsValidSampleValue(_visualF0.Samples[nIndex]) &&
                        selected.Contains(nIndex))
                    {
                        double delta = sampleValue - _visualF0.Samples[nIndex];
                        foreach (int n in selected)
                        {
                            _visualF0.Samples[n] += delta;
                        }
                    }
                }
            }
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isEnableEdit)
            {
                return;
            }

            if (_fHoverSeleting)
            {
                _fHoverSeleting = false;
                _linerGraph.IsEditable = true;
                _highlightFrames.IsHoverSelect = false;
            }
        }
    }
}