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
    using Data;

    /// <summary>
    /// Interaction logic for SpectrumGraph.xaml.
    /// </summary>
    public partial class SpectrumGraph : UserControl
    {
        private VisualSpectrumForm _spectrumData;

        private UIElemHandlersStub _uiElementHandlerStub;

        public SpectrumGraph()
        {
            InitializeComponent();
            _uiElementHandlerStub = new UIElemHandlersStub(this);
        }

        public void SetDataContext(VisualSpectrumForm spectrumData)
        {
            DataContext = spectrumData;
            _uiElementHandlerStub.InstallUnInstallRenderHandler(spectrumData, _spectrumData);
            _spectrumData = spectrumData;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            //// double[][] spectrum = _spectrumData.Spectrum;
            //// for (int windowIndex = windowOffset; windowIndex < windowEndIndex; windowIndex++)
            //// {
            ////    double[] data = spectrum[windowIndex];
            ////    if (data == null)
            ////    {
            ////        System.Diagnostics.Debug.Assert(false);
            ////        continue;
            ////    }

            ////    int length = data.Length;

            ////    float x = (windowIndex - windowOffset) * stepX;

            ////    for (int j = 0; j < length; ++j)
            ////    {
            ////        float value = data[j];
            ////        float y = (length - j) * stepY - 1;

            ////        int i = (int)(value / 18);
            ////        i = (i > 256) ? 255 : i;

            ////        byte color = (byte)(128 - i / 2);
            ////        for (float rx = -stepX / 2; rx < stepX / 2; rx++)
            ////        {
            ////            for (float ry = -stepY / 2; ry < stepY / 2; ry++)
            ////            {
            ////                int px = (int)(x + rx + offsetX);
            ////                int py = (int)(y + ry);
            ////                double distance = Math.Sqrt(rx * rx + ry * ry);
            ////                byte alpha = (distance > 256) ? (byte)255 : (byte)distance;
            ////                if (px >= 0 && px < clip.Width
            ////                    && py >= 0 && py < clip.Height)
            ////                {
            ////                    unsafeMap.SetAt((int)px, (int)py, (byte)i, color, 0, alpha);
            ////                }
            ////            }
            ////        }

            ////        unsafeMap.SetAt((int)(x + offsetX), (int)y, (byte)i, (byte)(128 - i / 2), 0, 0);
            ////    }
            //// }
        }
    }
}