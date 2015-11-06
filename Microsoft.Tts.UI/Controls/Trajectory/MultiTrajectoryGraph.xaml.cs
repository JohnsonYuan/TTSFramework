//----------------------------------------------------------------------------
// <copyright file="MultiTrajectoryGraph.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     The code logic of MultiTrajectoryGraph
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.UI.Controls.Trajectory
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Media;
    using Data;

    /// <summary>
    /// Interaction logic for MultiTrajectoryGraph.xaml.
    /// </summary>
    public partial class MultiTrajectoryGraph : UserControl
    {
        #region fields

        /// <summary>
        /// Selected dimensions.
        /// </summary>
        public static readonly DependencyProperty SelectedDimensionsProperty =
            DependencyProperty.Register("SelectedDimensions", typeof(ObservableCollection<int>),
            typeof(MultiTrajectoryGraph), new FrameworkPropertyMetadata(null, 
            new PropertyChangedCallback(OnSelectedDimensionsChanged)));

        /// <summary>
        /// Stroke pen of parameter trajectories.
        /// </summary>
        private static Pen parameterTrajectoryStroke = new Pen(Brushes.DarkBlue, 2.5);

        /// <summary>
        /// Stroke pen of auxiliary parameter trajectories.
        /// </summary>
        private static Pen auxiliaryParameterTrajectoryStroke = new Pen(Brushes.Red, 2.5);

        /// <summary>
        /// View data of dimension graph.
        /// </summary>
        private VisualDimensionGraph _dimensionGraphData = null;

        /// <summary>
        /// View data of trajectory.
        /// </summary>
        private VisualMultiTrajectory _trajectoryData = null;

        /// <summary>
        /// Trajectory information.
        /// </summary>
        private VisualTrajectoryInfo _trajectoryInfo = new VisualTrajectoryInfo();

        /// <summary>
        /// Graphs of generated parameters.
        /// </summary>
        private List<IntervalLinerGraph> _parameterGraphs = new List<IntervalLinerGraph>();

        /// <summary>
        /// Graphs of auxiliary parameters.
        /// </summary>
        private List<IntervalLinerGraph> _auxiliaryParameterGraphs = new List<IntervalLinerGraph>();

        /// <summary>
        /// Graphs of means.
        /// </summary>
        private List<IntervalConstantGraph> _meanGraphs = new List<IntervalConstantGraph>();

        /// <summary>
        /// Hover frame indexes.
        /// </summary>
        private ObservableCollection<int> _hoverFrameIndexes = null;

        #endregion

        #region constructor

        /// <summary>
        /// Initializes a new instance of the MultiTrajectoryGraph class.
        /// </summary>
        public MultiTrajectoryGraph()
        {
            InitializeComponent();
            _trajectoryInfoTable.DataContext = _trajectoryInfo;
            Brush brush = new SolidColorBrush(Colors.OrangeRed)
            {
                Opacity = 0.5,
            };

            _selectedFrames.HighlightFill = brush;
            _hightlighFrames.HighlightFill = brush;
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets or sets selected dimensions.
        /// </summary>
        public ObservableCollection<int> SelectedDimensions
        {
            get
            {
                return (ObservableCollection<int>)this.GetValue(SelectedDimensionsProperty);
            }

            set 
            {
                this.SetValue(SelectedDimensionsProperty, value);
                ObservableCollection<int> dimensions = value;
                dimensions.CollectionChanged +=
                    delegate(object sender, NotifyCollectionChangedEventArgs e)
                    {
                        InvalidateVisual();
                    };
            }
        }

        #endregion

        #region public methods

        /// <summary>
        /// Set data context.
        /// </summary>
        /// <param name="graphData">Graph data.</param>
        /// <param name="trajectoryData">Trajectory data.</param>
        /// <param name="hoverFrameIndexes">Hover frame indexes.</param>
        public void SetDataContext(VisualDimensionGraph graphData,
            VisualMultiTrajectory trajectoryData,
            ObservableCollection<int> hoverFrameIndexes)
        {
            Debug.Assert(graphData != null && trajectoryData != null,
                "Can't set null data context to MultiTrajectoryGraph.");

            _dimensionGraphData = graphData;
            DataContext = _dimensionGraphData;

            _dimensionGraphData.DisplayController.PropertyChanged +=
                new PropertyChangedEventHandler(OnGraphVisibilityChanged);

            _frameline.TimeAxis = graphData.TimeAxis;
            _selectedFrames.TimeAxis = graphData.TimeAxis;
            _selectedFrames.HighlightFrames = graphData.SelectedFrameIndexes;
            _selectedMultiFrames.TimeAxis = graphData.TimeAxis;
            _hightlighFrames.TimeAxis = graphData.TimeAxis;
            _hightlighFrames.HighlightFrames = hoverFrameIndexes;
            _hoverFrameIndexes = hoverFrameIndexes;
            _trajectoryData = trajectoryData;
        }

        /// <summary>
        /// Create trajectories.
        /// </summary>
        public void CreateTrajectories()
        {
            _trajectoryGrid.Children.Clear();
            _parameterGraphs.Clear();
            _auxiliaryParameterGraphs.Clear();
            _meanGraphs.Clear();
            _trajectoryData.LinkAxis();

            foreach (VisualSingleTrajectory traj in _trajectoryData.Trajectories)
            {
                VisualLinerSamples visualLinerSamples = new VisualLinerSamples();
                visualLinerSamples.TimeAxis = traj.TimeAxis;
                visualLinerSamples.YAxis = traj.YAxis;
                visualLinerSamples.Samples = traj.GeneratedParameters;
                IntervalLinerGraph paraGraph = new IntervalLinerGraph();
                paraGraph.CurveStroke = parameterTrajectoryStroke.Brush;
                paraGraph.CurveStrokeThickness = parameterTrajectoryStroke.Thickness;
                paraGraph.Lowerbound = IntervalLinerGraph.DefaultLowerBound;
                paraGraph.Upperbound = IntervalLinerGraph.DefaultUpperBound;
                paraGraph.SetDataContext(visualLinerSamples);

                visualLinerSamples = new VisualLinerSamples();
                visualLinerSamples.TimeAxis = traj.TimeAxis;
                visualLinerSamples.YAxis = traj.YAxis;
                visualLinerSamples.Samples = traj.AuxiliaryParameters;
                IntervalLinerGraph auxiliaryGraph = new IntervalLinerGraph();
                auxiliaryGraph.CurveStroke = auxiliaryParameterTrajectoryStroke.Brush;
                auxiliaryGraph.CurveStrokeThickness = parameterTrajectoryStroke.Thickness;
                auxiliaryGraph.Lowerbound = IntervalLinerGraph.DefaultLowerBound;
                auxiliaryGraph.Upperbound = IntervalLinerGraph.DefaultUpperBound;
                auxiliaryGraph.SetDataContext(visualLinerSamples);

                VisualConstantSamples visualConstantSamples = new VisualConstantSamples();
                visualConstantSamples.TimeAxis = traj.TimeAxis;
                visualConstantSamples.YAxis = traj.YAxis;
                visualConstantSamples.Samples = traj.Means;
                visualConstantSamples.Deviations = traj.StandardDeviations;
                IntervalConstantGraph meanGraph = new IntervalConstantGraph();
                meanGraph.Lowerbound = IntervalConstantGraph.DefaultLowerBound;
                meanGraph.Upperbound = IntervalConstantGraph.DefaultUpperBound;
                meanGraph.SetDataContext(visualConstantSamples);

                Binding binding = new Binding("ActualWidth");
                binding.Source = _graphSystem;
                paraGraph.SetBinding(IntervalLinerGraph.WidthProperty, binding);
                auxiliaryGraph.SetBinding(IntervalLinerGraph.WidthProperty, binding);
                meanGraph.SetBinding(IntervalConstantGraph.WidthProperty, binding);
                binding = new Binding("ActualHeight");
                binding.Source = _graphSystem;
                paraGraph.SetBinding(IntervalLinerGraph.HeightProperty, binding);
                auxiliaryGraph.SetBinding(IntervalLinerGraph.HeightProperty, binding);
                meanGraph.SetBinding(IntervalConstantGraph.HeightProperty, binding);

                _parameterGraphs.Add(paraGraph);
                _auxiliaryParameterGraphs.Add(auxiliaryGraph);
                _meanGraphs.Add(meanGraph);
            }

            if (_dimensionGraphData.DisplayController.TrajectoryDisplayController.ModelDataShowed)
            {
                foreach (IntervalConstantGraph graph in _meanGraphs)
                {
                    _trajectoryGrid.Children.Add(graph);
                }
            }

            if (_dimensionGraphData.DisplayController.TrajectoryDisplayController.TrajectoryShowed)
            {
                foreach (IntervalLinerGraph graph in _parameterGraphs)
                {
                    _trajectoryGrid.Children.Add(graph);
                }
            }

            if (_dimensionGraphData.DisplayController.AuxiliaryTrajectoryEnabled &&
                _dimensionGraphData.DisplayController.TrajectoryDisplayController.AuxiliaryTrajectoryShowed)
            {
                foreach (IntervalLinerGraph graph in _auxiliaryParameterGraphs)
                {
                    _trajectoryGrid.Children.Add(graph);
                }
            }
        }

        /// <summary>
        /// Set multi selected frames.
        /// </summary>
        /// <param name="frames">Frame indexes.</param>
        public void SetMultiSelectedFrames(ObservableCollection<int> frames)
        {
            _selectedMultiFrames.HighlightFrames = frames;
        }

        #endregion

        #region UI event handlers

        /// <summary>
        /// On Render the graph.
        /// </summary>
        /// <param name="drawingContext">Drawing context.</param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            UpdateGraphVisibility();
        }

        /// <summary>
        /// On selected dimensions changed.
        /// </summary>
        /// <param name="dependencyObject">Dependency object.</param>
        /// <param name="e">Event argument.</param>
        private static void OnSelectedDimensionsChanged(DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs e)
        {
            Debug.Assert(dependencyObject != null && dependencyObject is MultiTrajectoryGraph, "dependencyObject should not be null, and must be of type MultiTrajectoryGraph.");
            Debug.Assert(e != null, "e should not be null!");
            Debug.Assert(e.NewValue != null && e.NewValue is ObservableCollection<int>, "e.NewValue should not be null, and must be of type ObservableCollection<int>.");

            MultiTrajectoryGraph graph = dependencyObject as MultiTrajectoryGraph;
            graph.SelectedDimensions = e.NewValue as ObservableCollection<int>;
        }

        /// <summary>
        /// On mouse moving in graph.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event argument.</param>
        private void OnMouseMoveInGraph(object sender, MouseEventArgs e)
        {
            Debug.Assert(sender != null, "Mouse move event should have a sender");

            Point mousePoint = e.GetPosition(sender as IInputElement);
            for (int index = 0; index < _parameterGraphs.Count; ++index)
            {
                IntervalLinerGraph paraGraph = _parameterGraphs[index];
                IntervalLinerGraph auxiliaryParaGraph = _auxiliaryParameterGraphs[index];
                IntervalConstantGraph meanGraph = _meanGraphs[index];

                bool hitParaGraph = paraGraph.InputHitTest(mousePoint) != null;
                bool hitAuxiliaryParaGraph = auxiliaryParaGraph.InputHitTest(mousePoint) != null;
                bool hitMeanGraph = meanGraph.InputHitTest(mousePoint) != null;

                if (hitParaGraph && meanGraph.Visibility == Visibility.Visible)
                {
                    paraGraph.CurveStroke = Brushes.Yellow;
                }
                else
                {
                    paraGraph.CurveStroke = parameterTrajectoryStroke.Brush;
                }

                if (hitAuxiliaryParaGraph && meanGraph.Visibility == Visibility.Visible)
                {
                    auxiliaryParaGraph.CurveStroke = Brushes.LightSkyBlue;
                }
                else
                {
                    auxiliaryParaGraph.CurveStroke = auxiliaryParameterTrajectoryStroke.Brush;
                }

                if (_dimensionGraphData.TimeAxis.ZoomMode == ZoomMode.FrameLevel)
                {
                    if (hitMeanGraph || hitParaGraph || hitAuxiliaryParaGraph)
                    {
                        _dataPopup.IsOpen = false;

                        TrajectoryHelper.BuildTrajectoryInfo(_dimensionGraphData, mousePoint.X,
                            ((VisualLinerSamples)paraGraph.DataContext).Samples,
                            ((VisualLinerSamples)auxiliaryParaGraph.DataContext).Samples,
                            ((VisualConstantSamples)meanGraph.DataContext).Samples,
                            ((VisualConstantSamples)meanGraph.DataContext).Deviations,
                            _trajectoryInfo);

                        _dataPopup.IsOpen = true;

                        break;
                    }
                    else
                    {
                        _dataPopup.IsOpen = false;
                    }
                }
            }

            // show highlight
            if (_hoverFrameIndexes != null)
            {
                TrajectoryHelper.GenerateSelectedFrameIndexes(mousePoint.X, _dimensionGraphData.TimeAxis,
                    _hoverFrameIndexes);
            }
        }

        /// <summary>
        /// On mouse leaves graph.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event argument.</param>
        private void OnMouseLeaveGraph(object sender, MouseEventArgs e)
        {
            _dataPopup.IsOpen = false;
            if (_hoverFrameIndexes != null)
            {
                _hoverFrameIndexes.Clear();
            }
        }

        /// <summary>
        /// On mouse up in graph.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event argument.</param>
        private void OnMouseUpInGraph(object sender, MouseButtonEventArgs e)
        {
            Debug.Assert(e != null, "e should not be null!");
            if (_hoverFrameIndexes != null)
            {
                _hoverFrameIndexes.Clear();
            }

            Point mousePoint = e.GetPosition(sender as IInputElement);

            // Show highlight
            TrajectoryHelper.GenerateSelectedFrameIndexes(mousePoint.X, _dimensionGraphData.TimeAxis,
                _dimensionGraphData.SelectedFrameIndexes);
        }

        #endregion

        #region private methods

        /// <summary>
        /// Update the visibility of sub graphs.
        /// </summary>
        private void UpdateGraphVisibility()
        {
            _trajectoryGrid.Children.Clear();
            for (int index = 0; index < _trajectoryData.Trajectories.Count; ++index)
            {
                if ((_dimensionGraphData.SelectedDimensions == null ||
                    _dimensionGraphData.SelectedDimensions.Contains(index + 1)) &&
                    _dimensionGraphData.DisplayController.TrajectoryDisplayController.ModelDataShowed)
                {
                    _trajectoryGrid.Children.Add(_meanGraphs[index]);
                }
            }

            for (int index = 0; index < _trajectoryData.Trajectories.Count; ++index)
            {
                if ((_dimensionGraphData.SelectedDimensions == null ||
                    _dimensionGraphData.SelectedDimensions.Contains(index + 1)) &&
                    _dimensionGraphData.DisplayController.TrajectoryDisplayController.TrajectoryShowed)
                {
                    _trajectoryGrid.Children.Add(_parameterGraphs[index]);
                }
            }

            for (int index = 0; index < _trajectoryData.Trajectories.Count; ++index)
            {
                if ((_dimensionGraphData.SelectedDimensions == null ||
                    _dimensionGraphData.SelectedDimensions.Contains(index + 1)) &&
                    _dimensionGraphData.DisplayController.AuxiliaryTrajectoryEnabled &&
                    _dimensionGraphData.DisplayController.TrajectoryDisplayController.AuxiliaryTrajectoryShowed)
                {
                    _trajectoryGrid.Children.Add(_auxiliaryParameterGraphs[index]);
                }
            }

            _trajectoryData.CalculateValueRange(_dimensionGraphData.SelectedDimensions);
        }

        /// <summary>
        /// Event handler when graph visibility changed.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void OnGraphVisibilityChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("TrajectoryShowed") ||
                e.PropertyName.Equals("AuxiliaryTrajectoryEnabled") ||
                e.PropertyName.Equals("AuxiliaryTrajectoryShowed") ||
                e.PropertyName.Equals("ModelDataShowed"))
            {
                UpdateGraphVisibility();
            }
        }

        #endregion
    }
}