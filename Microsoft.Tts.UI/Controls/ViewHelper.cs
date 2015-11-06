//----------------------------------------------------------------------------
// <copyright file="ViewHelper.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      Helper class for views
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.UI.Controls
{
    using System;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;
    using Microsoft.Win32;

    /// <summary>
    /// Class of ViewHelper.
    /// </summary>
    public class ViewHelper
    {
        /// <summary>
        /// 0.5 ms/pixel.
        /// </summary>
        public const double TimespanPerPixel = 0.5;

        /// <summary>
        /// Adjust ruler scale factor.
        /// </summary>
        /// <param name="factor">Scale factor to be adjusted.</param>
        /// <returns>Adjusted factor.</returns>
        public static double AdjustFactor(double factor)
        {
            double power = Math.Log10(factor);
            double powerValue = Math.Pow(10, Math.Floor(power));
            int adjustedFactor = (int)(factor / powerValue);

            if (adjustedFactor < 2)
            {
                adjustedFactor = 1;
            }
            else if (adjustedFactor >= 2 && adjustedFactor < 4)
            {
                adjustedFactor = 2;
            }
            else if (adjustedFactor >= 4)
            {
                adjustedFactor = 5;
            }

            return adjustedFactor * powerValue;
        }

        /// <summary>
        /// Open select file dialog.
        /// </summary>
        /// <param name="fileFilter">File filter.</param>
        /// <param name="originalFilePath">Original file path.</param>
        /// <returns>Selected file path. Returns original file path if no file is selected.</returns>
        public static string OpenSelectFileDialog(string fileFilter, string originalFilePath)
        {
            Debug.Assert(fileFilter != null, "File filter shouldn't be null.");

            string selectedFileName = string.Empty;
            string currentDir = Environment.CurrentDirectory;
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = fileFilter;

            if (openFileDialog.ShowDialog() == true)
            {
                selectedFileName = openFileDialog.FileName;
            }

            Environment.CurrentDirectory = currentDir;
            return string.IsNullOrEmpty(selectedFileName) ? originalFilePath : selectedFileName;
        }

        /// <summary>
        /// GetDisplayUnitIndex.
        /// </summary>
        /// <param name="unitIndex">Unit index.</param>
        /// <returns>Display unit index.</returns>
        public static int GetDisplayUnitIndex(int unitIndex)
        {
            // The clicked phone set to the first one in graph view,
            // Ut is inconvenient to observe the clicked 
            // So, Add one more phones in front of the clicked phone.
            // That the context of clicked phone is displayed in the graph view
            int unitOffset = -1;
            int index = unitIndex + unitOffset;

            return index >= 0 ? index : unitIndex;
        }

        /// <summary>
        /// Convert a timespan to pixels.
        /// </summary>
        /// <param name="timespan">Time span.</param>
        /// <param name="horizontalScale">Horizontal scale.</param>
        /// <returns>Pixel value.</returns>
        public static double TimespanToPixel(double timespan, double horizontalScale)
        {
            Debug.Assert(horizontalScale > 0.0, "Horizontal scale should be larger than zero.");
            return timespan / TimespanPerPixel * horizontalScale;
        }

        /// <summary>
        /// Convert a pixel to time span.
        /// </summary>
        /// <param name="pixel">Pixel value.</param>
        /// <param name="horizontalScale">Horizontal scale.</param>
        /// <returns>Time span.</returns>
        public static double PixelToTimeSpan(double pixel, double horizontalScale)
        {
            Debug.Assert(horizontalScale > 0.0, "Horizontal scale should be larger than zero.");
            return pixel * TimespanPerPixel / horizontalScale;
        }
    }

    /// <summary>
    /// Class of VisibilityConvertor.
    /// </summary>
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class VisibilityConvertor : IValueConverter
    {
        #region IValueConverter Members

        /// <summary>
        /// Convert a bool value to visibility.
        /// </summary>
        /// <param name="value">Bool value.</param>
        /// <param name="targetType">Target type.</param>
        /// <param name="parameter">Input parameter.</param>
        /// <param name="culture">Culture info.</param>
        /// <returns>Visibility. Visible if input true.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool visable = (bool)value;
            return visable ? Visibility.Visible : Visibility.Hidden;
        }

        /// <summary>
        /// Convert a visibility to a bool value.
        /// </summary>
        /// <param name="value">Visibility value.</param>
        /// <param name="targetType">Target type.</param>
        /// <param name="parameter">Input parameter.</param>
        /// <param name="culture">Culture info.</param>
        /// <returns>True if it's visible.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility visibility = (Visibility)value;
            return visibility == Visibility.Visible;
        }

        #endregion
    }

    /// <summary>
    /// Class of VisibilityHightConvertor.
    /// </summary>
    [ValueConversion(typeof(bool), typeof(string))]
    public class VisibilityHightConvertor : IValueConverter
    {
        #region IValueConverter Members

        /// <summary>
        /// Convert a bool value to visibility.
        /// </summary>
        /// <param name="value">Bool value.</param>
        /// <param name="targetType">Target type.</param>
        /// <param name="parameter">Input parameter.</param>
        /// <param name="culture">Culture info.</param>
        /// <returns>Visibility. Visible if input true.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool visable = (bool)value;
            return visable ? "Auto" : "0";
        }

        /// <summary>
        /// Convert a visibility to a bool value.
        /// </summary>
        /// <param name="value">Visibility value.</param>
        /// <param name="targetType">Target type.</param>
        /// <param name="parameter">Input parameter.</param>
        /// <param name="culture">Culture info.</param>
        /// <returns>True if it's visible.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("Don't support convert from height string to bool");
        }

        #endregion
    }

    /// <summary>
    /// Class of UIElemHandlersStub.
    /// </summary>
    public class UIElemHandlersStub
    {
        private UIElement _uiElem;

        /// <summary>
        /// Initializes a new instance of the <see cref="UIElemHandlersStub"/> class.
        /// </summary>
        /// <param name="uiElem">The stub host element.</param>
        public UIElemHandlersStub(UIElement uiElem)
        {
            _uiElem = uiElem;
        }

        /// <summary>
        /// Install and uninstall handler.
        /// </summary>
        /// <param name="installObj">Object to install on.</param>
        /// <param name="unInstallObj">Object to unstall on.</param>
        public void InstallUnInstallRenderHandler(INotifyCollectionChanged installObj, INotifyCollectionChanged unInstallObj)
        {
            if (unInstallObj != null)
            {
                unInstallObj.CollectionChanged -= RenderCollectionChanged;
            }

            if (installObj != null)
            {
                installObj.CollectionChanged += RenderCollectionChanged;
            }
        }

        /// <summary>
        /// Install and uninstall handler.
        /// </summary>
        /// <param name="installObj">Object to install on.</param>
        /// <param name="unInstallObj">Object to unstall on.</param>
        public void InstallUnInstallRenderHandler(INotifyPropertyChanged installObj, INotifyPropertyChanged unInstallObj)
        {
            if (unInstallObj != null)
            {
                unInstallObj.PropertyChanged -= RenderPropertyChanged;
            }

            if (installObj != null)
            {
                installObj.PropertyChanged += RenderPropertyChanged;
            }
        }

        private void RenderPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            _uiElem.InvalidateVisual();
        }

        private void RenderCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _uiElem.InvalidateVisual();
        }
    }
}