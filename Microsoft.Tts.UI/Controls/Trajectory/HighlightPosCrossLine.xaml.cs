namespace Microsoft.Tts.UI.Controls.Trajectory
{
    using System;
    using System.Collections.Generic;
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

    /// <summary>
    /// Interaction logic for HighlightPosCrossLine.xaml.
    /// </summary>
    public partial class HighlightPosCrossLine : UserControl
    {
        /// <summary>
        /// Deviation fill brush.
        /// </summary>
        public static readonly DependencyProperty LineBrushProperty =
            DependencyProperty.Register("LineBrush", typeof(Brush), typeof(HighlightPosCrossLine),
            new FrameworkPropertyMetadata(new SolidColorBrush(Colors.Red),
            FrameworkPropertyMetadataOptions.AffectsRender));

        private Point _lastPoint = new Point();

        public HighlightPosCrossLine()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Gets or sets the brush used to draw the lines.
        /// </summary>
        public Brush LineBrush
        {
            get { return (Brush)this.GetValue(LineBrushProperty); }
            set { this.SetValue(LineBrushProperty, value); }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            Pen pen = new Pen(LineBrush, 1);
            GuidelineSet gs = new GuidelineSet();
            gs.GuidelinesX.Clear();
            gs.GuidelinesY.Clear();
            gs.GuidelinesY.Add(_lastPoint.Y - 0.5);
            gs.GuidelinesY.Add(_lastPoint.Y + 0.5);
            drawingContext.PushGuidelineSet(gs.Clone());
            drawingContext.DrawLine(pen, new Point(0, _lastPoint.Y), new Point(ActualWidth, _lastPoint.Y));
            drawingContext.PushGuidelineSet(gs.Clone());
            gs.GuidelinesX.Clear();
            gs.GuidelinesY.Clear();
            gs.GuidelinesX.Add(_lastPoint.X - 0.5);
            gs.GuidelinesX.Add(_lastPoint.X + 0.5);
            drawingContext.PushGuidelineSet(gs.Clone());
            drawingContext.DrawLine(pen, new Point(_lastPoint.X, 0), new Point(_lastPoint.X, ActualHeight));
            drawingContext.PushGuidelineSet(gs.Clone());
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            _lastPoint = e.GetPosition(this);
            InvalidateVisual();
        }
    }
}