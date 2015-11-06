namespace Microsoft.Tts.UI.Controls.Trajectory
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
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

    /// <summary>
    /// Interaction logic for WaveFormGraph.xaml.
    /// </summary>
    public partial class WaveFormGraph : UserControl
    {
        public WaveFormGraph()
        {
            InitializeComponent();
        }

        public IntervalLinerGraph LinerGraph
        {
            get
            {
                return _intervalLinerGraph;
            }
        }
    }
}