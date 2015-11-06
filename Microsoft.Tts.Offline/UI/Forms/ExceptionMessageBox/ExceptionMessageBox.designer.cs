//----------------------------------------------------------------------------
// <copyright file="ExceptionMessageBox.designer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements Form to show top-level exception message.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Forms
{
    /// <summary>
    /// Exception message box show top-level exception.
    /// </summary>
    public partial class ExceptionMessageBox
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TextBox _stackTraceTextBox;
        private System.Windows.Forms.Button _continueButton;
        private System.Windows.Forms.Button _abortButton;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox _errorMessageTextBox;

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
            this._stackTraceTextBox = new System.Windows.Forms.TextBox();
            this._continueButton = new System.Windows.Forms.Button();
            this._abortButton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this._errorMessageTextBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // _stackTraceTextBox
            // 
            this._stackTraceTextBox.Location = new System.Drawing.Point(37, 169);
            this._stackTraceTextBox.Multiline = true;
            this._stackTraceTextBox.Name = "_stackTraceTextBox";
            this._stackTraceTextBox.ReadOnly = true;
            this._stackTraceTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this._stackTraceTextBox.Size = new System.Drawing.Size(509, 178);
            this._stackTraceTextBox.TabIndex = 0;
            // 
            // _continueButton
            // 
            this._continueButton.Location = new System.Drawing.Point(135, 372);
            this._continueButton.Name = "_continueButton";
            this._continueButton.Size = new System.Drawing.Size(75, 23);
            this._continueButton.TabIndex = 1;
            this._continueButton.Text = "Continue";
            this._continueButton.UseVisualStyleBackColor = true;
            this._continueButton.Click += new System.EventHandler(this.OnContinueButtonClick);
            // 
            // _abortButton
            // 
            this._abortButton.Location = new System.Drawing.Point(337, 372);
            this._abortButton.Name = "_abortButton";
            this._abortButton.Size = new System.Drawing.Size(75, 23);
            this._abortButton.TabIndex = 2;
            this._abortButton.Text = "Abort";
            this._abortButton.UseVisualStyleBackColor = true;
            this._abortButton.Click += new System.EventHandler(this.OnAbortButtonClick);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(34, 142);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(65, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Stack trace:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(34, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(77, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Error message:";
            // 
            // _errorMessageTextBox
            // 
            this._errorMessageTextBox.Location = new System.Drawing.Point(37, 40);
            this._errorMessageTextBox.Multiline = true;
            this._errorMessageTextBox.Name = "_errorMessageTextBox";
            this._errorMessageTextBox.ReadOnly = true;
            this._errorMessageTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this._errorMessageTextBox.Size = new System.Drawing.Size(509, 89);
            this._errorMessageTextBox.TabIndex = 5;
            // 
            // ExceptionMessageBox
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(567, 410);
            this.Controls.Add(this._errorMessageTextBox);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this._abortButton);
            this.Controls.Add(this._continueButton);
            this.Controls.Add(this._stackTraceTextBox);
            this.Name = "ExceptionMessageBox";
            this.Text = "ExceptionMessageBox";
            this.TopMost = true;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
    }
}