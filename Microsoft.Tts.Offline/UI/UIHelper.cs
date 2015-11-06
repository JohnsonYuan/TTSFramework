//----------------------------------------------------------------------------
// <copyright file="UIHelper.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements UI helper.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.UI
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Security;
    using System.Security.Permissions;
    using System.Text;
    using System.Threading;
    using System.Windows.Forms;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Forms;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// UI(User interface) error: including Console and WinForm Application.
    /// </summary>
    public enum UIError
    {
        /// <summary>
        /// Error language string.
        /// {0} : Error language string.
        /// </summary>
        [ErrorAttribute(Message = "Error language string [{0}], format should be like [en-US].")]
        ErrorLanguageString,

        /// <summary>
        /// Error language data file.
        /// {0} : Language the data file should be.
        /// {1} : File type of the language data file like: "phoneset.xml".
        /// {2} : Language of the data file.
        /// </summary>
        [ErrorAttribute(Message = "Load [{0}] [{1}] files error, the file's language is [{2}]")]
        ErrorLanguageDataFile
    }

    /// <summary>
    /// UI helper class.
    /// </summary>
    public static class UIHelper
    {
        /// <summary>
        /// Context menu script Y offset.
        /// </summary>
        public const int ContextMenuStripOffset = 12;

        /// <summary>
        /// Remove unicode control chars from user input.
        /// #PS12537.
        /// </summary>
        private static uint[] _unicodeControlCharsValue = new uint[]
        {
            8206,
            8207,
            8205,
            8204,
            8234,
            8235,
            8237,
            8238,
            8236,
            8302,
            8303,
            8299,
            8298,
            8301,
            8300,
            30,
            31
        };

        /// <summary>
        /// Remove unicode control chars from user input.
        /// </summary>
        /// <param name="source">User input from UI.</param>
        /// <returns>Cleaned string without unicode control char.</returns>
        public static string RemoveUnicodeControlChars(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                source = string.Empty;
            }

            StringBuilder sb = new StringBuilder();
            if (!string.IsNullOrEmpty(source))
            {
                List<uint> unicodeControlChars = new List<uint>();
                unicodeControlChars.AddRange(_unicodeControlCharsValue);
                foreach (char c in source)
                {
                    if (!unicodeControlChars.Contains(c))
                    {
                        sb.Append(c);
                    }
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Validate language string.
        /// </summary>
        /// <param name="languageStr">Language string.</param>
        /// <returns>Whether the language string is valid.</returns>
        public static bool ValidateLanguageString(string languageStr)
        {
            Error error = null;
            try
            {
                Language language = Localor.StringToLanguage(languageStr);
            }
            catch (ArgumentException)
            {
                error = new Error(
                    UIError.ErrorLanguageString, languageStr);
                Console.Error.WriteLine(error.ToString());
            }

            return error == null;
        }

        /// <summary>
        /// Report argument error.
        /// </summary>
        /// <param name="errorMessage">Error message.</param>
        /// <param name="target">Argument object.</param>
        public static void ReportArgumentError(string errorMessage, object target)
        {
            StringBuilder sb = new StringBuilder();
            if (!string.IsNullOrEmpty(errorMessage))
            {
                sb.AppendFormat("ERROR : {0}", errorMessage);
                sb.AppendLine();
                sb.AppendLine();
            }

            sb.AppendLine(CommandLineParser.BuildUsage(target));

            UIHelper.ReportMessage(null, "Promoting", sb.ToString());
        }

        /// <summary>
        /// Handle exception of commandline parser in UI application.
        /// </summary>
        /// <param name="exception">Exception.</param>
        /// <param name="target">Target object to reflect usage information.</param>
        public static void HandleArgumentException(Exception exception, object target)
        {
            ReportArgumentError(exception.Message, target);
        }

        /// <summary>
        /// Report error set in UI Application.
        /// </summary>
        /// <param name="owner">Error report form owner.</param>
        /// <param name="title">Error report form title.</param>
        /// <param name="message">Message to be reported.</param>
        public static void ReportMessage(IWin32Window owner, string title, string message)
        {
            if (owner == null)
            {
                owner = null;
            }

            if (!string.IsNullOrEmpty(message))
            {
                using (MessageReportForm form = new MessageReportForm())
                {
                    form.StartPosition = FormStartPosition.CenterScreen;
                    form.Message = message;
                    if (!string.IsNullOrEmpty(title))
                    {
                        form.Text = title;
                    }

                    if (owner != null)
                    {
                        form.ShowDialog(owner);
                    }
                    else
                    {
                        form.ShowDialog();
                    }
                }
            }
        }

        /// <summary>
        /// Process main form exception message.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2109:ReviewVisibleEventHandlers", Justification = "no security issue here")]
        public static void HandleApplicationThreadException(object sender, ThreadExceptionEventArgs e)
        {
            Trace.WriteLine(Helper.BuildExceptionMessage(e.Exception));
            Trace.WriteLine(e.Exception.StackTrace);

            using (ExceptionMessageBox exceptionMessageBox = new ExceptionMessageBox(e.Exception))
            {
                exceptionMessageBox.ShowDialog();
            }
        }

        /// <summary>
        /// Validate file exist.
        /// </summary>
        /// <param name="filePath">File path need validated.</param>
        /// <returns>Whether file exists.</returns>
        public static bool ValidateFileExist(string filePath)
        {
            bool validate = true;
            if (string.IsNullOrEmpty(filePath))
            {
                validate = false;
            }

            if (validate)
            {
                filePath = filePath.Trim();
                if (!Path.IsPathRooted(filePath))
                {
                    validate = false;
                }
            }

            if (validate && !File.Exists(filePath))
            {
                validate = false;
            }

            return validate;
        }

        /// <summary>
        /// Validate whether file exists.
        /// </summary>
        /// <param name="owner">Error message box owner.</param>
        /// <param name="label">Label presentes file tyep.</param>
        /// <param name="filePath">File path to be validate.</param>
        /// <returns>Whether the file existing.</returns>
        public static bool ValidateFileExist(IWin32Window owner, string label, string filePath)
        {
            bool isValid = ValidateNotEmptyString(owner, label, filePath);
            if (isValid)
            {
                filePath = filePath.Trim();
                string message = string.Empty;
                if (!Path.IsPathRooted(filePath))
                {
                    message = Helper.NeutralFormat("{0} : Please input absolute file path, " +
                        "invalid file path [{1}]", label, filePath);
                    isValid = false;
                }

                if (isValid && !File.Exists(filePath))
                {
                     message = Helper.NeutralFormat("{0}: Can't find file [{1}]!", label, filePath);
                    isValid = false;
                }

                if (!isValid && !string.IsNullOrEmpty(message))
                {
                    if (owner != null)
                    {
                        MessageBox.Show(owner, message, "Warning", MessageBoxButtons.OK,
                            MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, 0);
                    }
                    else
                    {
                        MessageBox.Show(message, "Warning", MessageBoxButtons.OK,
                            MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, 0);
                    }
                }
            }

            return isValid;
        }

        /// <summary>
        /// Validate whether it is empty string.
        /// </summary>
        /// <param name="owner">MessageBox owner.</param>
        /// <param name="label">Label for message information.</param>
        /// <param name="text">String for validation.</param>
        /// <returns>Whether the text is not empty.</returns>
        public static bool ValidateNotEmptyString(IWin32Window owner, string label, string text)
        {
            bool isValid = true;
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(text.Trim()))
            {
                string message = Helper.NeutralFormat("{0}: Empty Text!", label);
                if (owner != null)
                {
                    MessageBox.Show(owner, message, "Warning", MessageBoxButtons.OK,
                        MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, 0);
                }
                else
                {
                    MessageBox.Show(message, "Warning", MessageBoxButtons.OK,
                        MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, 0);
                }

                isValid = false;
            }

            return isValid;
        }

        /// <summary>
        /// Get UI application's title.
        /// </summary>
        /// <param name="filePath">If the application need load a file, show the file name in title.</param>
        /// <returns>Application's title.</returns>
        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        public static string ApplicationTitle(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                filePath = string.Empty;
            }

            string title = Path.GetFileNameWithoutExtension(
                Process.GetCurrentProcess().MainModule.FileName);
            if (!string.IsNullOrEmpty(filePath))
            {
                title = Helper.NeutralFormat("{0} - {1}",
                    Path.GetFileName(filePath), title);
            }

            return title;
        }

        /// <summary>
        /// Test whether .
        /// </summary>
        /// <param name="owner">MessageBox owner, null if don't need show the MessageBox.</param>
        /// <param name="filePath">File path to be tested.</param>
        /// <returns>Whether the file is writtable.</returns>
        public static bool EnsureFileWrittable(IWin32Window owner, string filePath)
        {
            return EnsureFileWrittable(owner, filePath, string.Empty, string.Empty);
        }

        /// <summary>
        /// Test whether .
        /// </summary>
        /// <param name="owner">MessageBox owner, null if don't need show the MessageBox.</param>
        /// <param name="filePath">File path to be tested.</param>
        /// <param name="title">Promote title.</param>
        /// <param name="message">Message.</param>
        /// <returns>Whether the file is writtable.</returns>
        public static bool EnsureFileWrittable(IWin32Window owner, string filePath, string title,
            string message)
        {
            if (owner == null)
            {
                // This checking needed by PREfast, if owner == null, don't need promote user dialog.
                owner = null;
            }

            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            string errorMessage = string.Empty;
            string stackTrace = string.Empty;

            bool overwrite = true;
            string promoteTitle = string.IsNullOrEmpty(title) ? "Test file writtable!" : title;

            if (File.Exists(filePath) && Helper.IsReadonlyFile(filePath))
            {
                string confirmMessage = Helper.NeutralFormat("Whether overwrite the readonly file [{0}]?", filePath);

                if (owner != null)
                {
                    overwrite = MessageBox.Show(owner, confirmMessage, promoteTitle, MessageBoxButtons.YesNo) == DialogResult.Yes;
                }
                else
                {
                    overwrite = MessageBox.Show(confirmMessage, promoteTitle, MessageBoxButtons.YesNo) == DialogResult.Yes;
                }

                if (overwrite && !Helper.SetFileReadOnly(filePath, false))
                {
                    errorMessage = Helper.NeutralFormat("Can't remove file's [{0}] readonly property.", filePath);
                }
            }

            if (overwrite && string.IsNullOrEmpty(errorMessage))
            {
                errorMessage = Helper.TestWritableWithoutException(filePath);
            }

            if (overwrite && owner != null && !string.IsNullOrEmpty(errorMessage))
            {
                if (!string.IsNullOrEmpty(message))
                {
                    errorMessage = Helper.NeutralFormat("Can't overwrite file file [{0}].{1}Because:{2}{3}{4}{5}",
                        filePath, Environment.NewLine, Environment.NewLine, errorMessage,
                        Environment.NewLine, message);
                }

                UIHelper.ReportMessage(owner, promoteTitle, errorMessage);
            }

            return overwrite && string.IsNullOrEmpty(errorMessage);
        }

        /// <summary>
        /// Set form to the center of the screen.
        /// </summary>
        /// <param name="form">Form to set location.</param>
        public static void SetToScreenCenter(Control form)
        {
            Screen screen = Screen.PrimaryScreen;
            form.Left = (screen.WorkingArea.Width - form.Width) / 2;
            form.Top = (screen.WorkingArea.Height - form.Height) / 2;
        }

        /// <summary>
        /// Clear control list.
        /// </summary>
        /// <param name="controlList">Control list.</param>
        public static void CleanControlList(List<Control> controlList)
        {
            foreach (Control c in controlList)
            {
                c.Dispose();
            }

            controlList.Clear();
        }
    }
}