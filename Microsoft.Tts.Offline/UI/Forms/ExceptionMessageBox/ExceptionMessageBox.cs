//----------------------------------------------------------------------------
// <copyright file="ExceptionMessageBox.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements Form to show top-level exception message.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Forms
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data;
    using System.Drawing;
    using System.Text;
    using System.Windows.Forms;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Exception message box show top-level exception.
    /// </summary>
    public partial class ExceptionMessageBox : Form
    {
        private const string ExceptionMessageBoxTitle = "Exception caught in application!";

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionMessageBox"/> class.
        /// </summary>
        /// <param name="exception">Exception to be shown.</param>
        public ExceptionMessageBox(Exception exception) : this()
        {
            InitializeCustomComponent(exception);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionMessageBox"/> class.
        /// </summary>
        public ExceptionMessageBox()
        {
            InitializeComponent();
        }

        #endregion

        /// <summary>
        /// Initialize custom component.
        /// </summary>
        /// <param name="exception">Exception need to be report.</param>
        private void InitializeCustomComponent(Exception exception)
        {
            Text = ExceptionMessageBoxTitle;
            if (exception != null)
            {
                _errorMessageTextBox.Text = Helper.BuildExceptionMessage(exception);
                _stackTraceTextBox.Text = exception.StackTrace;
            }
        }

        /// <summary>
        /// When user click "Continue" button, ignore the excption..
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event argument.</param>
        private void OnContinueButtonClick(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// When user click "Abort" button, exit the application.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event argument.</param>
        private void OnAbortButtonClick(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}