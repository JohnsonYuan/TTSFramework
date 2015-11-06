//----------------------------------------------------------------------------
// <copyright file="WaveControl.designer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements WaveControl
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Waveform
{
    /// <summary>
    /// WaveControl.
    /// </summary>
    public partial class WaveControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        private WaveView _waveformView;
        private HorizontalScaleBar _horScaleBar;
        private VerticalScaleBar _verScaleBar;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">True if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
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
            this.DoubleBuffered = true;

            this.SuspendLayout();
            // 
            // WaveView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Name = "WaveView";

            _waveformView = new WaveView();
            _horScaleBar = new HorizontalScaleBar();
            _verScaleBar = new VerticalScaleBar();

            this.Controls.AddRange(new System.Windows.Forms.Control[] { _waveformView, _horScaleBar, _verScaleBar });

            this.Size = new System.Drawing.Size(554, 231);
            this.ResumeLayout(false);

        }

        #endregion
    }
}