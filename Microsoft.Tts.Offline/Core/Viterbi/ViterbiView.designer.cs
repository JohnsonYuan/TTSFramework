//----------------------------------------------------------------------------
// <copyright file="ViterbiView.designer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements ViterbiView
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Viterbi
{
    using System.Windows.Forms;

    /// <summary>
    /// ViterbiView.
    /// </summary>
    public partial class ViterbiView
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// It is used for enable scrolling of panel.
        /// </summary>
        private Button _dummyButton;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">True if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
                if (_mutex != null)
                {
                    ((System.IDisposable)_mutex).Dispose();
                    _mutex = null;
                }
            }

            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// The contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
           
            this.ResumeLayout(false);

            _dummyButton = new Button();
            _dummyButton.Size = new System.Drawing.Size(0, 0);
            this.Controls.Add(_dummyButton);
        }

        #endregion
    }
}