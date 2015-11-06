//----------------------------------------------------------------------------
// <copyright file="TrajectoryInfoUserControl.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     The code logic of TrajectoryInfoUserControl
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.UI.Controls.Trajectory
{
    using System;
    using System.Globalization;
    using System.Windows.Controls;
    using System.Windows.Data;

    /// <summary>
    /// Interaction logic for TrajectoryInfoUserControl.xaml.
    /// </summary>
    public partial class TrajectoryInfoUserControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TrajectoryInfoUserControl"/> class.
        /// </summary>
        public TrajectoryInfoUserControl()
        {
            InitializeComponent();
        }
    }

    /// <summary>
    /// Class of DoubleConverter, convert a double to a formated string.
    /// </summary>
    [ValueConversion(typeof(double), typeof(string))]
    internal class DoubleConverter : IValueConverter
    {
        /// <summary>
        /// Convert to string, convert to 2 digits after decimal point.
        /// </summary>
        /// <param name="value">Value to convert.</param>
        /// <param name="targetType">Target type.</param>
        /// <param name="parameter">Input parameter.</param>
        /// <param name="culture">Culture info.</param>
        /// <returns>Value converted.</returns>
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            double num = (double)value;
            return double.IsNegativeInfinity(num) ? "N/A" :
                num.ToString("0.0000", culture);
        }

        /// <summary>
        /// Convert back to double, do nothing as data binding will be one-way.
        /// </summary>
        /// <param name="value">Value to convert.</param>
        /// <param name="targetType">Target type.</param>
        /// <param name="parameter">Input parameter.</param>
        /// <param name="culture">Culture info.</param>
        /// <returns>Throw exception.</returns>
        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            return new NotImplementedException();
        }
    }
}