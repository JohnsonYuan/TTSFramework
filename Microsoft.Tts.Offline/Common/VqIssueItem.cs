// --------------------------------------------------------------------------------------------------------------------
// <copyright file="VqIssueItem.cs" company="Microsoft">
//   All rights reserved.
// </copyright>
// <summary>
//   This module defines the VqIssue and VqIssueItem class.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Common
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Text;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Office;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// The VQ issue class.
    /// </summary>
    public class VqIssue
    {
        /// <summary>
        /// The enum for issue category.
        /// </summary>
        public enum Category
        {
            /// <summary>
            /// No VQ issue.
            /// </summary>
            None,

            /// <summary>
            /// Naturalness VQ issue.
            /// </summary>
            Naturalness,

            /// <summary>
            /// Intelligibility VQ issue.
            /// </summary>
            Intelligibility
        }

        /// <summary>
        /// The enum for issue severity.
        /// </summary>
        public enum Severity
        {
            /// <summary>
            /// No severity.
            /// </summary>
            None,

            /// <summary>
            /// Warning.
            /// </summary>
            Warning,

            /// <summary>
            /// Error.
            /// </summary>
            Error
        }
    }

    /// <summary>
    /// The VQ issue item class.
    /// </summary>
    public class VqIssueItem : IComparable
    {
        #region Private Fields

        private string _name;
        private string _label;
        private string _leftContext;
        private string _rightContext;
        private VqIssue.Category _category;
        private VqIssue.Severity _severity;
        private string _parent;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="VqIssueItem"/> class.
        /// Construct only with the name.
        /// </summary>
        /// <param name="name">The name of the item, can be phone name, word gramephe and sentence ID.</param>
        public VqIssueItem(string name)
        {
            _name = name;
            _label = string.Empty;
            _leftContext = string.Empty;
            _rightContext = string.Empty;
            _category = VqIssue.Category.None;
            _severity = VqIssue.Severity.None;
            _parent = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VqIssueItem"/> class.
        /// Construct only with the name and label.
        /// </summary>
        /// <param name="name">The name of the item, can be phone name, word gramephe and sentence ID.</param>
        /// <param name="label">The description of the item.</param>
        public VqIssueItem(string name, string label)
        {
            _name = name;
            _label = label;
            _parent = string.Empty;
            _leftContext = string.Empty;
            _rightContext = string.Empty;
            _category = VqIssue.Category.None;
            _severity = VqIssue.Severity.None;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VqIssueItem"/> class.
        /// Construct with the label, issue category and severity.
        /// </summary>
        /// <param name="name">The name of the item, can be phone name, word gramephe and sentence ID.</param>
        /// <param name="label">The description of the item.</param>
        /// <param name="leftContext">The left context of this item.</param>
        /// <param name="rightContext">The right context of this item.</param>
        public VqIssueItem(string name, string label, string leftContext, string rightContext)
        {
            _name = name;
            _label = label;
            _leftContext = leftContext;
            _rightContext = rightContext;
            _category = VqIssue.Category.None;
            _severity = VqIssue.Severity.None;
            _parent = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VqIssueItem"/> class.
        /// Construct with the label, issue category and severity.
        /// </summary>
        /// <param name="name">The name of the item, can be phone name, word gramephe and sentence ID.</param>
        /// <param name="label">The description of the item.</param>
        /// <param name="leftContext">The left context of this item.</param>
        /// <param name="rightContext">The right context of this item.</param>
        /// <param name="category">The issue category of the item.</param>
        /// <param name="severity">The issue severity of the item.</param>
        public VqIssueItem(string name, string label, string leftContext, string rightContext, VqIssue.Category category, VqIssue.Severity severity)
        {
            _name = name;
            _label = label;
            _leftContext = leftContext;
            _rightContext = rightContext;
            _category = category;
            _severity = severity;
            _parent = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VqIssueItem"/> class.
        /// Construct with the label, issue category and severity.
        /// </summary>
        /// <param name="name">The name of the item, can be phone name, word gramephe and sentence ID.</param>
        /// <param name="label">The description of the item.</param>
        /// <param name="leftContext">The left context of this item.</param>
        /// <param name="rightContext">The right context of this item.</param>
        /// <param name="category">The issue category of the item.</param>
        /// <param name="severity">The issue severity of the item.</param>
        /// <param name="parent">The name of the parent of this item, e.g. for phone item, the parent item should be word.</param>
        public VqIssueItem(string name, string label, string leftContext, string rightContext, 
            VqIssue.Category category, VqIssue.Severity severity,
            string parent)
        {
            _name = name;
            _label = label;
            _leftContext = leftContext;
            _rightContext = rightContext;
            _category = category;
            _severity = severity;
            _parent = parent;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VqIssueItem"/> class.
        /// </summary>
        /// <param name="item">The item to copy from.</param>
        public VqIssueItem(VqIssueItem item)
        {
            _name = item.Name;
            _label = item.Label;
            _leftContext = item.LeftContext;
            _rightContext = item.RightContext;
            _category = item.IssueCategory;
            _severity = item.IssueSeverity;
            _parent = item.Parent;
        }

        #endregion
        
        #region Properties

        /// <summary>
        /// Gets The name of the item, can be sentence ID, word grapheme, unit/phone name.
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }
        }

        /// <summary>
        /// Gets or sets The label to distinct the item, can be sentence ID, word grapheme, unit/phone name,
        /// Congtext word or anything.
        /// </summary>
        public string Label
        {
            get
            {
                return _label;
            }

            set
            {
                _label = value;
            }
        }

        /// <summary>
        /// Gets or sets The left context word.
        /// </summary>
        public string LeftContext
        {
            get
            {
                return _leftContext;
            }

            set
            {
                _leftContext = value;
            }
        }

        /// <summary>
        /// Gets or sets The right context word.
        /// </summary>
        public string RightContext
        {
            get
            {
                return _rightContext;
            }

            set
            {
                _rightContext = value;
            }
        }

        /// <summary>
        /// Gets or sets The issue category.
        /// </summary>
        public VqIssue.Category IssueCategory
        {
            get
            {
                return _category;
            }

            set
            {
                _category = value;
            }
        }

        /// <summary>
        /// Gets or sets The issue severity.
        /// </summary>
        public VqIssue.Severity IssueSeverity
        {
            get
            {
                return _severity;
            }

            set
            {
                _severity = value;
            }
        }

        /// <summary>
        /// Gets or sets The parent for this item: for phone, the parent will be the word; for word, the parent will be the sentence id.
        /// </summary>
        public string Parent
        {
            get
            {
                return _parent;
            }

            set
            {
                _parent = value;
            }
        }

        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// The realization for the compare function
        /// The object will be sorted with following ordering:
        ///    Issue category: descending
        ///    Name : ascending
        ///    Label: ascending
        ///    Left context:   ascending
        ///    Right context:  ascending
        ///    Issue severity: descending.
        /// </summary>
        /// <param name="obj">The object to compare with.</param>
        /// <returns>RetValue.</returns>
        public int CompareTo(object obj)
        {
            int retValue = 0;
            if (obj == null)
            {
                retValue = 1;
            }
            else
            {
                VqIssueItem item2Compare = obj as VqIssueItem;
                if (item2Compare == null)
                {
                    throw new ArgumentException("Object is not a VqIssueItem");
                }

                if (IssueCategory != item2Compare.IssueCategory)
                {
                    retValue = item2Compare.IssueCategory - IssueCategory;
                }
                else if (Name.ToLower() != item2Compare.Name.ToLower())
                {
                    retValue = string.Compare(Name, item2Compare.Name, StringComparison.CurrentCultureIgnoreCase);
                }
                else if (Label.ToLower() != item2Compare.Label.ToLower())
                {
                    retValue = string.Compare(Label, item2Compare.Label, StringComparison.CurrentCultureIgnoreCase);
                }
                else if (LeftContext.ToLower() != item2Compare.LeftContext.ToLower())
                {
                    retValue = string.Compare(LeftContext, item2Compare.LeftContext, StringComparison.CurrentCultureIgnoreCase);
                }
                else if (RightContext.ToLower() != item2Compare.RightContext.ToLower())
                {
                    retValue = string.Compare(RightContext, item2Compare.RightContext, StringComparison.CurrentCultureIgnoreCase);
                }
                else if (IssueSeverity != item2Compare.IssueSeverity)
                {
                    retValue = item2Compare.IssueSeverity - IssueSeverity;
                }

                if (retValue < 0)
                {
                    retValue = -1;
                }
                else if (retValue > 0)
                {
                    retValue = 1;
                }
            }

            return retValue;
        }

        /// <summary>
        /// To string.
        /// </summary>
        /// <returns>String.</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0}::{1}::{2}::{3}::{4}::{5}", Name, Label, LeftContext, RightContext,
                IssueCategory.ToString(), IssueSeverity.ToString());

            return sb.ToString().ToLower();
        }

        /// <summary>
        /// To string (whether convert with severity).
        /// </summary>
        /// <param name="withSeverity">Whether convert to string with "severity".</param>
        /// <returns>OutString.</returns>
        public string ToString(bool withSeverity)
        {
            string outString = string.Empty;
            if (withSeverity)
            {
                outString = ToString();
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("{0}::{1}::{2}::{3}::{4}", Name, Label, LeftContext, RightContext,
                    IssueCategory.ToString());

                outString = sb.ToString().ToLower();
            }

            return outString;
        }

        /// <summary>
        /// Print the label into output with different color according to severity.
        /// </summary>
        public void PrintColoredLabelToConsole()
        {
            ConsoleColor _oldColor = Console.ForegroundColor;
            switch (IssueSeverity)
            {
                case VqIssue.Severity.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case VqIssue.Severity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                default:
                    break;
            }

            Console.Write(Name);
            Console.ForegroundColor = _oldColor;
        }

        /// <summary>
        /// Build text for colored cell string for Excel.
        /// </summary>
        /// <returns>The colored text segment.</returns>
        public ExcelReporter.ColoredTextSegment BuildColoredTextSegment()
        {
            ExcelReporter.ColoredTextSegment coloredTextSegment = new ExcelReporter.ColoredTextSegment()
            {  
                Text = Name,
                StartIndex = 0,
                Length = Name.Length,
                Color = ExcelReporter.TextColor.Black
            };

            if (IssueCategory != VqIssue.Category.None && IssueSeverity == VqIssue.Severity.Error)
            {
                coloredTextSegment.Color = ExcelReporter.TextColor.Red;
            }
            else if (IssueCategory != VqIssue.Category.None && IssueSeverity == VqIssue.Severity.Warning)
            {
                coloredTextSegment.Color = ExcelReporter.TextColor.Orange;
            }

            return coloredTextSegment;
        }

        /// <summary>
        /// Build text for colored cell string for Excel.
        /// </summary>
        /// <param name="withLabel">Whether to build the text segment with label.</param>
        /// <param name="withRightContext">Whether to build the text segment with right context.</param>
        /// <returns>The colored text segment.</returns>
        public ExcelReporter.ColoredTextSegment BuildColoredTextSegment(bool withLabel, bool withRightContext)
        {
            ExcelReporter.ColoredTextSegment coloredTextSegment = new ExcelReporter.ColoredTextSegment()
            {
                Text = Name,
                StartIndex = 0,
                Length = Name.Length,
                Color = ExcelReporter.TextColor.Black
            };

            if (withLabel)
            {
                coloredTextSegment.Text += " " + Label;
            }

            if (withRightContext)
            {
                coloredTextSegment.Text += " " + RightContext;
            }

            if (IssueCategory != VqIssue.Category.None && IssueSeverity == VqIssue.Severity.Error)
            {
                coloredTextSegment.Color = ExcelReporter.TextColor.Red;
            }
            else if (IssueCategory != VqIssue.Category.None && IssueSeverity == VqIssue.Severity.Warning)
            {
                coloredTextSegment.Color = ExcelReporter.TextColor.Orange;
            }

            return coloredTextSegment;
        }

        #endregion
    }
}