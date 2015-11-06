//----------------------------------------------------------------------------
// <copyright file="VoiceFontSet.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements VoiceFontSet
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Windows.Forms;
    using System.Xml;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Data loading event handler.
    /// </summary>
    /// <param name="percent">Percent of data loaded.</param>
    public delegate void DataLoadingEventHandler(int percent);

    /// <summary>
    /// All data loaded event handler.
    /// </summary>
    public delegate void AllDataLoadedEventHandler();

    /// <summary>
    /// Voice font set class to manage all a collection of voice font.
    /// </summary>
    public class VoiceFontSet : IDisposable
    {
        #region Fields

        private IntPtr _hanlde = IntPtr.Zero;
        private Thread _dataLoading;
        private DummyForm _dummyForm = new DummyForm();
        private ManualResetEvent _eventDatabaseReady = new ManualResetEvent(false);

        private Dictionary<string, VoiceFont> _voiceFonts =
                                        new Dictionary<string, VoiceFont>();

        #endregion

        #region Constructions

        /// <summary>
        /// Initializes a new instance of the <see cref="VoiceFontSet"/> class.
        /// </summary>
        public VoiceFontSet()
        {
            OnDataLoading = delegate
            {
            };

            OnAllDataLoaded = delegate
            {
            };

            _hanlde = _dummyForm.Handle;
            _dummyForm.OnMessage += new OnMessageEventHandler(OnDummyFormMessage);
        }

        #endregion

        #region Internal struction definition

        /// <summary>
        /// On message event handling.
        /// </summary>
        /// <param name="m">Message.</param>
        private delegate void OnMessageEventHandler(ref Message m);

        #endregion

        #region Events

        /// <summary>
        /// Event of data loading status updated.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1009:DeclareEventHandlersCorrectly", Justification = "Ignore.")]
        public event DataLoadingEventHandler OnDataLoading;

        /// <summary>
        /// Event of data loaded.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1009:DeclareEventHandlersCorrectly", Justification = "Ignore.")]
        public event AllDataLoadedEventHandler OnAllDataLoaded;

        #endregion

        #region Properties

        /// <summary>
        /// Gets Windows event set while database is ready for using.
        /// </summary>
        public ManualResetEvent EventDatabaseReady
        {
            get { return _eventDatabaseReady; }
        }

        /// <summary>
        /// Gets Data loading worker thread.
        /// </summary>
        public Thread DataLoading
        {
            get { return _dataLoading; }
        }

        /// <summary>
        /// Gets Voice font dictionary indexed by voice name.
        /// </summary>
        public Dictionary<string, VoiceFont> VoiceFonts
        {
            get { return _voiceFonts; }
        }

        #endregion

        #region Loading operations

        /// <summary>
        /// Load voice font set from configuration file.
        /// </summary>
        /// <param name="configFilePath">Configuration file path.</param>
        public void Load(string configFilePath)
        {
            XmlDocument dom = new XmlDocument();
            dom.Load(configFilePath);
            Load(dom);
        }

        /// <summary>
        /// Doing load voice font database.
        /// </summary>
        public void DoLoadDatabase()
        {
            foreach (VoiceFont dm in VoiceFonts.Values)
            {
                dm.Initialize();
            }

            _eventDatabaseReady.Set();

            if (!Helper.PostMessage(this._hanlde,
                VoiceFont.LoadingStatusChanged, (IntPtr)101, IntPtr.Zero))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Dispose this object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            System.GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose this object.
        /// </summary>
        /// <param name="disposing">Flag indicating whether delete unmanaged resource.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _dummyForm.Close();
                _eventDatabaseReady.Close();
            }
        }

        #endregion

        #region Event handlers

        /// <summary>
        /// Handle OnMessage event of _formDummy.
        /// </summary>
        /// <param name="m">Message.</param>
        private void OnDummyFormMessage(ref Message m)
        {
            if (m.Msg == VoiceFont.LoadingStatusChanged)
            {
                OnDataLoading((int)m.WParam);
            }
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Load voice font from XML DOM object.
        /// </summary>
        /// <param name="configDom">Configuration DOM object.</param>
        private void Load(XmlDocument configDom)
        {
            _voiceFonts.Clear();

            foreach (XmlNode node in
                configDom.SelectNodes("checker/datasettings/datasetting"))
            {
                VoiceFont dm = new VoiceFont(this._dummyForm.Handle);
                dm.ParseConfig((XmlElement)node);

                VoiceFonts.Add(dm.TokenId, dm);
            }

            // Start a another thread to load data
            if (_dataLoading != null && !_dataLoading.IsAlive)
            {
                _dataLoading.Abort();
            }

            _dataLoading = new Thread(new ThreadStart(DoLoadDatabase));
            _dataLoading.SetApartmentState(ApartmentState.STA);

            // _dataLoading.Priority = ThreadPriority.BelowNormal;
            _dataLoading.Start();
        }

        #endregion

        #region Private types

        /// <summary>
        /// Dummy form to receive Windows message for according thread 
        /// Communication for WinForm.
        /// </summary>
        private class DummyForm : Form
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="DummyForm"/> class.
            /// </summary>
            public DummyForm()
            {
                OnMessage = delegate
                    {
                    };
            }

            /// <summary>
            /// OnMessage event.
            /// </summary>
            public event OnMessageEventHandler OnMessage;

            /// <summary>
            /// Wnd message procedure.
            /// </summary>
            /// <param name="m">Message.</param>
            protected override void WndProc(ref Message m)
            {
                if (m.Msg == VoiceFont.LoadingStatusChanged)
                {
                    OnMessage(ref m);
                }

                base.WndProc(ref m);
            }

            /// <summary>
            /// Dispose the unmanaged resource.
            /// </summary>
            /// <param name="disposing">Flag indicating whether to dispose
            /// unmanaged resouece.</param>
            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
            }
        }

        #endregion
    }
}