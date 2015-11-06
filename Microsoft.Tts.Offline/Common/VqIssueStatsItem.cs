// --------------------------------------------------------------------------------------------------------------------
// <copyright file="VqIssueStatsItem.cs" company="Microsoft">
//   All rights reserved.
// </copyright>
// <summary>
//   This module defines the class to calculate VQ issue statistics.
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
    using Microsoft.Tts.Offline.Utility;
    
    /// <summary>
    /// The VQ issue item class.
    /// </summary>
    public class VqIssueStatsItem : IComparable
    {
        #region Private Fields

        private string _name;
        private string _label;
        private string _leftContext;
        private string _rightContext;
        private VqIssue.Category _category;
        private int _totalCount;
        private int _errorCount;
        private List<string> _errorRecords;
        private int _warningCount;
        private List<string> _warningRecords;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="VqIssueStatsItem"/> class.
        /// </summary>
        /// <param name="name">The name of the item, can be phone name, word gramephe and sentence ID.</param>
        /// <param name="label">The description of the item.</param>
        /// <param name="leftContext">The left context of this item.</param>
        /// <param name="rightContext">The right context of this item.</param>
        /// <param name="category">The issue category of the item.</param>
        public VqIssueStatsItem(string name, string label, string leftContext, string rightContext, VqIssue.Category category)
        {
            _name = name;
            _label = label;
            _leftContext = leftContext;
            _rightContext = rightContext;
            _category = category;
            _errorRecords = new List<string>();
            _warningRecords = new List<string>();
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
        /// Gets The label to distinct the item, can be sentence ID, word grapheme, unit/phone name,
        /// Congtext word or anything.
        /// </summary>
        public string Label
        {
            get
            {
                return _label;
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
        /// Gets The total count.
        /// </summary>
        public int TotalCount
        {
            get
            {
                return _totalCount;
            }
        }

        /// <summary>
        /// Gets The error count.
        /// </summary>
        public int ErrorCount
        {
            get
            {
                return _errorCount;
            }
        }

        /// <summary>
        /// Gets The error ratio.
        /// </summary>
        public double ErrorRatio
        {
            get
            {
                return TotalCount > 0 ? ErrorCount * 1.0 / TotalCount : 0.0;
            }
        }

        /// <summary>
        /// Gets The error record list.
        /// </summary>
        public List<string> ErrorRecords
        {
            get
            {
                return _errorRecords;
            }
        }

        /// <summary>
        /// Gets The warning count.
        /// </summary>
        public int WarningCount
        {
            get
            {
                return _warningCount;
            }
        }

        /// <summary>
        /// Gets The warning ratio.
        /// </summary>
        public double WarningRatio
        {
            get
            {
                return TotalCount > 0 ? WarningCount * 1.0 / TotalCount : 0.0;
            }
        }

        /// <summary>
        /// Gets The warning record list.
        /// </summary>
        public List<string> WarningRecords
        {
            get
            {
                return _warningRecords;
            }
        }

        #endregion
        
        #region Public Methods

        /// <summary>
        /// The realization for the compare function
        /// The object will be sorted with following ordering:
        ///    Name : ascending
        ///    Label: ascending
        ///    Left context:   ascending
        ///    Right context:  ascending
        ///    Issue category: descending
        ///    Issue severity: descending
        ///    Issue count:    descending.
        /// </summary>
        /// <param name="obj">The object to compare with.</param>
        /// <returns>Return retValue.</returns>
        public int CompareTo(object obj)
        {
            int retValue = 0;
            if (obj == null)
            {
                retValue = 1;
            }
            else
            {
                VqIssueStatsItem item2Compare = obj as VqIssueStatsItem;
                if (item2Compare == null)
                {
                    throw new ArgumentException("Object is not a VqIssueStatsItem");
                }

                if (IssueCategory != item2Compare.IssueCategory)
                {
                    retValue = item2Compare.IssueCategory - IssueCategory;
                }
                else if (ErrorCount != item2Compare.ErrorCount)
                {
                    retValue = item2Compare.ErrorCount - ErrorCount;
                }
                else if (WarningCount != item2Compare.WarningCount)
                {
                    retValue = item2Compare.WarningCount - WarningCount;
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
            sb.AppendFormat("{0}::{1}::{2}::{3}::{4}", Name, Label, LeftContext, RightContext, IssueCategory.ToString());
            return sb.ToString().ToLower();
        }

        /// <summary>
        /// Update the stats item with a specific VQ issue item.
        /// </summary>
        /// <param name="vqIssueItem">Update with the specific VQ issue item.</param>
        /// <returns>Bool.</returns>
        public bool Update(VqIssueItem vqIssueItem)
        {
            ++_totalCount;
            if (vqIssueItem.ToString(false) == ToString())
            {
                if (vqIssueItem.IssueSeverity == VqIssue.Severity.Warning)
                {
                    ++_warningCount;
                    if (!_warningRecords.Contains(vqIssueItem.Parent.ToLower()))
                    {
                        _warningRecords.Add(vqIssueItem.Parent.ToLower());
                    }
                }
                else if (vqIssueItem.IssueSeverity == VqIssue.Severity.Error)
                {
                    ++_errorCount;
                    if (!_errorRecords.Contains(vqIssueItem.Parent.ToLower()))
                    {
                        _errorRecords.Add(vqIssueItem.Parent.ToLower());
                    }
                }
            }

            return vqIssueItem.ToString(false) == ToString();
        }

        #endregion
    }
}
