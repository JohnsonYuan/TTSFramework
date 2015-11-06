//----------------------------------------------------------------------------
// <copyright file="ViewModelBase.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     definition of abstract class ViewDataBase
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.UI.Controls.Data
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Reflection;
    using System.Text;

    /// <summary>
    /// Base class of view models.
    /// </summary>
    public abstract class ViewDataBase : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Members

        /// <summary>
        /// Notify property changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region public method

        /// <summary>
        /// Memorywise copy the properties from src.
        /// </summary>
        /// <param name="src">Src object.</param>
        public void CopyPropertiesFrom(ViewDataBase src)
        {
            if (src.GetType() != GetType())
            {
                throw new Exception("data 1 and data 2 must be the same type");
            }

            PropertyInfo[] infos = src.GetType().GetProperties();
            foreach (PropertyInfo info in infos)
            {
                info.SetValue(
                    this, info.GetValue(src, null), null);
            }
        }

        #endregion

        #region protected method

        /// <summary>
        /// Notify property changed.
        /// </summary>
        /// <param name="name">Property name.</param>
        protected void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        #endregion
    }

    /// <summary>
    /// Class of ViewDataPropertyBinder.
    /// This class is used to binding all the properties in 2 objects in two-way mode.
    /// </summary>
    public class ViewDataPropertyBinder : IDisposable
    {
        #region fields
        
        private ViewDataBase _data1;
        
        private ViewDataBase _data2;
        
        private Type _dataType;
        
        private Dictionary<string, PropertyInfo> _properties = new Dictionary<string, PropertyInfo>();
        
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewDataPropertyBinder"/> class.
        /// </summary>
        /// <param name="data1">Data 1.</param>
        /// <param name="data2">Data 2.</param>
        public ViewDataPropertyBinder(ViewDataBase data1, ViewDataBase data2)
        {
            if (data1.GetType() != data2.GetType())
            {
                throw new Exception("data 1 and data 2 must be the same type");
            }

            _dataType = data1.GetType();
            PropertyInfo[] infos = _dataType.GetProperties();
            foreach (PropertyInfo info in infos)
            {
                _properties.Add(info.Name, info);
            }

            _data1 = data1;
            _data2 = data2;
            data1.PropertyChanged += OnData1PropertyChanged;
            data2.PropertyChanged += OnData2PropertyChanged;
            ExcludedProperties = new Collection<string>();
        }

        /// <summary>
        /// Gets or sets The exclued properties during the properties copying.
        /// </summary>
        public Collection<string> ExcludedProperties
        {
            get;
            set;
        }

        #region IDisposable Members

        /// <summary>
        /// Dispose the object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose the object.
        /// </summary>
        /// <param name="disposing">Disposing flag.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _data1.PropertyChanged -= OnData1PropertyChanged;
                _data2.PropertyChanged -= OnData2PropertyChanged;
            }
        }

        #endregion

        private void OnData2PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (ExcludedProperties == null ||
                !ExcludedProperties.Contains(e.PropertyName))
            {
                PropertyInfo propertyInfo = _properties[e.PropertyName];
                _data1.PropertyChanged -= OnData1PropertyChanged;
                propertyInfo.SetValue(_data1,
                    propertyInfo.GetValue(sender, null),
                    null);
                _data1.PropertyChanged += OnData1PropertyChanged;
            }
        }

        private void OnData1PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (ExcludedProperties == null ||
                !ExcludedProperties.Contains(e.PropertyName))
            {
                PropertyInfo propertyInfo = _properties[e.PropertyName];
                _data2.PropertyChanged -= OnData1PropertyChanged;
                propertyInfo.SetValue(_data2,
                    propertyInfo.GetValue(sender, null),
                    null);
                _data2.PropertyChanged += OnData1PropertyChanged;
            }
        }
    }
}