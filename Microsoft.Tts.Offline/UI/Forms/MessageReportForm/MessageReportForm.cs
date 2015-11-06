//----------------------------------------------------------------------------
// <copyright file="MessageReportForm.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements Form to show error set message.
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
    using Microsoft.Tts.Offline.Common;

    /// <summary>
    /// Form for reporting error set.
    /// </summary>
    public partial class MessageReportForm : Form
    {
        private string _message;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageReportForm"/> class.
        /// </summary>
        public MessageReportForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Gets or sets Message to be shown.
        /// </summary>
        public string Message
        {
            get { return _message; }
            set { _message = value; }
        }

        /// <summary>
        /// On error set report form shown.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event argument.</param>
        private void OnErrorSetReportFormShown(object sender, EventArgs e)
        {
            _errorSetReportTextBox.Clear();
            if (!string.IsNullOrEmpty(_message))
            {
                _errorSetReportTextBox.Text = _message;
            }
        }

        /// <summary>
        /// On ok button clicked.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event argument.</param>
        private void OnOKButtonClick(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}