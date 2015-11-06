//----------------------------------------------------------------------------
// <copyright file="MessageReportForm.Designer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements Form to show error set message.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Forms
{
    /// <summary>
    /// Form for reporting error set.
    /// </summary>
    public partial class MessageReportForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TextBox _errorSetReportTextBox;
        private System.Windows.Forms.Button _okayButton;

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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// The contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this._errorSetReportTextBox = new System.Windows.Forms.TextBox();
            this._okayButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // _errorSetReportTextBox
            // 
            this._errorSetReportTextBox.Location = new System.Drawing.Point(12, 12);
            this._errorSetReportTextBox.Multiline = true;
            this._errorSetReportTextBox.Name = "_errorSetReportTextBox";
            this._errorSetReportTextBox.ReadOnly = true;
            this._errorSetReportTextBox.Size = new System.Drawing.Size(577, 250);
            this._errorSetReportTextBox.TabIndex = 0;
            // 
            // _OKButton
            // 
            this._okayButton.Location = new System.Drawing.Point(260, 272);
            this._okayButton.Name = "_OKButton";
            this._okayButton.Size = new System.Drawing.Size(75, 23);
            this._okayButton.TabIndex = 1;
            this._okayButton.Text = "OK";
            this._okayButton.UseVisualStyleBackColor = true;
            this._okayButton.Click += new System.EventHandler(this.OnOKButtonClick);
            // 
            // ErrorSetReportForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(601, 307);
            this.Controls.Add(this._okayButton);
            this.Controls.Add(this._errorSetReportTextBox);
            this.Name = "ErrorSetReportForm";
            this.Text = "Error set report";
            this.TopMost = true;
            this.Shown += new System.EventHandler(this.OnErrorSetReportFormShown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
    }
}