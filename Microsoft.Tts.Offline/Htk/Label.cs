//----------------------------------------------------------------------------
// <copyright file="Label.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module defines a common library to manipulate Htk label.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Htk
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Htk Label type options, could be FullContext or MonoPhonme.
    /// </summary>
    public enum LabelTypeOptions
    {
        /// <summary>
        /// Full-context label, which contains many features in the label.
        /// </summary>
        FullContext,

        /// <summary>
        /// Mono phoneme label, only contains the phoneme name in the label.
        /// </summary>
        MonoPhoneme,
    }

    /// <summary>
    /// Htk Label alignment options.
    /// </summary>
    public enum LabelAlignOptions
    {
        /// <summary>
        /// Withou any alignment data.
        /// </summary>
        NoAlign,

        /// <summary>
        /// With phoneme-level alignment data.
        /// </summary>
        PhonemeAlign,

        /// <summary>
        /// With state-level alignment data.
        /// </summary>
        StateAlign,
    }

    /// <summary>
    /// A helper class to parse a string to get Label object or convert Label to text.
    /// </summary>
    public class LabelLine
    {
        #region Fields

        /// <summary>
        /// The split characters for the columns in a line.
        /// </summary>
        private static readonly char[] SplitterChars = new[] { ' ' };

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the Segment of this HtkLabel. null means not exist.
        /// </summary>
        public Segment Segment { get; set; }

        /// <summary>
        /// Gets or sets the state of this HtkLabel. Negative value (-1) means not exist.
        /// </summary>
        public int State { get; set; }

        /// <summary>
        /// Gets or sets the HtkLabel contained in this string.
        /// </summary>
        public Label Label { get; set; }

        /// <summary>
        /// Gets or sets the remaining parts which cannot parse here.
        /// </summary>
        public string[] Remaining { get; set; }

        #endregion

        #region Static Methods

        /// <summary>
        /// Parses a string and return a HtkLabelHelper instance by default LabelFeatureNameSet.
        /// </summary>
        /// <param name="value">The given string to be parsed.</param>
        /// <returns>The parsed HtkLabelHelper.</returns>
        public static LabelLine Parse(string value)
        {
            return Parse(value, LabelFeatureNameSet.Default);
        }

        /// <summary>
        /// Parses a string and return a HtkLabelHelper instance by given LabelFeatureNameSet.
        /// </summary>
        /// <param name="value">The given string to be parsed.</param>
        /// <param name="featureNames">The given feature names.</param>
        /// <returns>The parsed HtkLabelHelper.</returns>
        public static LabelLine Parse(string value, LabelFeatureNameSet featureNames)
        {
            LabelLine labelLine = new LabelLine
            {
                // Initialize state as a negative value.
                State = -1,
            };

            string labelText;
            string[] parts = value.Split(SplitterChars, StringSplitOptions.RemoveEmptyEntries);
            switch (parts.Length)
            {
                case 1:
                    labelText = parts[0];
                    break;
                case 2:
                    // Invalid currently.
                    throw new InvalidDataException(Helper.NeutralFormat("Unsupported data \"{0}\"", value));
                default:
                    labelLine.Segment = new Segment(long.Parse(parts[0]), long.Parse(parts[1]));
                    labelText = parts[2];

                    // Keep the remaining values.
                    if (parts.Length > 3)
                    {
                        labelLine.Remaining = new string[parts.Length - 3];
                        Array.Copy(parts, 3, labelLine.Remaining, 0, labelLine.Remaining.Length);
                    }

                    break;
            }

            labelLine.Label = new Label(featureNames);

            // Check there is state information or not.
            if (labelText[labelText.Length - 1] == ']')
            {
                // The state info is appended to the label text as: a-b+c+x...x[state]
                // Find the previous "["
                int index = labelText.LastIndexOf('[');
                if (index < 0)
                {
                    throw new InvalidDataException(Helper.NeutralFormat("Unsupport format \"{0}\"", labelText));
                }

                labelLine.Label.Text = labelText.Substring(0, index);
                labelLine.State = int.Parse(labelText.Substring(index + 1, labelText.Length - index - 2));
            }
            else
            {
                labelLine.Label.Text = labelText;
            }

            return labelLine;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Converts the HtkLabelHelper to string.
        /// </summary>
        /// <param name="typeOption">The given label type.</param>
        /// <param name="keepRemainingPart">Whether to keep the remaining part.</param>
        /// <returns>The string to indicate this object.</returns>
        public string ToString(LabelTypeOptions typeOption, bool keepRemainingPart)
        {
            StringBuilder textBuilder = new StringBuilder();

            if (Segment != null)
            {
                textBuilder.Append(Segment.StartTime);
                textBuilder.Append(SplitterChars[0]);
                textBuilder.Append(Segment.EndTime);
                textBuilder.Append(SplitterChars[0]);
            }

            switch (typeOption)
            {
                case LabelTypeOptions.FullContext:
                    textBuilder.Append(Label.Text);
                    break;
                case LabelTypeOptions.MonoPhoneme:
                    textBuilder.Append(Label.CentralPhoneme);
                    break;
                default:
                    throw new InvalidDataException("Unkown HtkLabelTypeOptions value");
            }

            if (State > 0)
            {
                textBuilder.Append("[" + State + "]");
            }

            if (keepRemainingPart && Remaining != null)
            {
                foreach (string remaining in Remaining)
                {
                    textBuilder.Append(SplitterChars[0]);
                    textBuilder.Append(remaining);
                }
            }

            return textBuilder.ToString();
        }

        #endregion
    }

    /// <summary>
    /// A helper class to hold the feature name information.
    /// </summary>
    public class LabelFeatureNameSet
    {
        #region Fields

        /// <summary>
        /// The all separator characters. This is in the order of the label.
        /// The label will be generated according to this list.
        /// </summary>
        public const string SeparatorChars = @"-++|&&;;#|='':$~!--@@%;$$!+:=|##&~^:+&-;||%~'" +
            @"=@!^=^~#:&%+@'-!;=$&@|~-'^%#;:#$;+!!$|^'+%:-=&:@~$=%|+;-^&'~@;!#!~+^$:|-&=#%@-:';&+~%" +
            @"!=-#^|$@+=!'|@^#~&^;%$'&!:%-$#@=:~;'%'!&|:;~|'@$-|!%&#+#=+$^+-~=;@:^!|;^@&$+'#-%==~~::!@#'$%%^^-";

        /// <summary>
        /// The feature name of central phoneme.
        /// </summary>
        public const string CentralPhonemeFeatureName = "Phone.PhoneIdentity";

        /// <summary>
        /// The feature name of left phoneme.
        /// </summary>
        public const string LeftPhonemeFeatureName = "Phone.PrevPhone.PhoneIdentity";

        /// <summary>
        /// The feature name of right phoneme.
        /// </summary>
        public const string RightPhonemeFeatureName = "Phone.NextPhone.PhoneIdentity";

        /// <summary>
        /// The mandatory features will be keep in the first of the all features.
        /// For example, currently, the "left-phoneme", "central-phoneme" and
        /// "right-phoneme" will be placed in this list.
        /// </summary>
        private static readonly string[] MandatoryFeatureNameArray = new[]
        {
            LeftPhonemeFeatureName,
            CentralPhonemeFeatureName,
            RightPhonemeFeatureName,
        };

        /// <summary>
        /// The all user create feature name set. The key is the name given by user.
        /// </summary>
        private static readonly Dictionary<string, LabelFeatureNameSet> NamedSet = new Dictionary<string, LabelFeatureNameSet>();

        /// <summary>
        /// The default set, which only contains the mandatory features.
        /// </summary>
        private static readonly LabelFeatureNameSet DefaultSet = new LabelFeatureNameSet();

        /// <summary>
        /// The mono label set, which only contains the central phoneme.
        /// </summary>
        private static readonly LabelFeatureNameSet MonoLabelFeatureNameSet = new LabelFeatureNameSet();

        /// <summary>
        /// The feature name set. The key is the feature name and the value is the index
        /// Of the feature in the label.
        /// </summary>
        private Dictionary<string, int> _featureNameToIndex = new Dictionary<string, int>();

        #endregion

        #region Constructor

        /// <summary>
        /// Prevents a default instance of the LabelFeatureNameSet class from being created.
        /// </summary>
        private LabelFeatureNameSet()
        {
            // Initialize the feature names to contain the mandatory feature names.
            for (int i = 0; i < MandatoryFeatureNameArray.Length; ++i)
            {
                _featureNameToIndex.Add(MandatoryFeatureNameArray[i], i);
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the mandatory features will be keep in the first of the all features.
        /// For example, currently, the "left-phoneme", "central-phoneme" and
        /// "right-phoneme" will be placed in this list.
        /// </summary>
        public static string[] MandatoryFeatureNames
        {
            get
            {
                return MandatoryFeatureNameArray.Clone() as string[];
            }
        }

        /// <summary>
        /// Gets the Triphone set, which is the same as the default now.
        /// </summary>
        public static LabelFeatureNameSet Triphone
        {
            get { return DefaultSet; }
        }

        /// <summary>
        /// Gets the default set, which only contains the mandatory features.
        /// </summary>
        public static LabelFeatureNameSet Default
        {
            get { return DefaultSet; }
        }

        /// <summary>
        /// Gets the mono label set, which only contains the central phoneme.
        /// </summary>
        public static LabelFeatureNameSet MonoLabel
        {
            get
            {
                // If the count of feature name is equal to that of mandantory feature names,
                // it means the MonoLabel is not initialized as well.
                if (MonoLabelFeatureNameSet._featureNameToIndex.Count == MandatoryFeatureNameArray.Length)
                {
                    // Initliaze MonoLabel to contain the central phone only.
                    MonoLabelFeatureNameSet._featureNameToIndex = new Dictionary<string, int>
                    {
                        { CentralPhonemeFeatureName, 0 },
                    };
                }

                return MonoLabelFeatureNameSet;
            }
        }

        /// <summary>
        /// Gets the count of the feature names.
        /// </summary>
        public int Count
        {
            get
            {
                return _featureNameToIndex.Count;
            }
        }

        /// <summary>
        /// Gets the current feature name list.
        /// </summary>
        public string[] FeatureNames
        {
            get
            {
                // Build the array of feature names in order.
                string[] featureNames = new string[_featureNameToIndex.Count];
                foreach (KeyValuePair<string, int> kvp in _featureNameToIndex)
                {
                    featureNames[kvp.Value] = kvp.Key;
                }

                return featureNames;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a new feature name set.
        /// </summary>
        /// <param name="setName">The user specified name of this set.</param>
        /// <param name="featureNames">The feature names in order.</param>
        /// <returns>The created feature name set.</returns>
        public static LabelFeatureNameSet Create(string setName, IList<string> featureNames)
        {
            if (NamedSet.ContainsKey(setName))
            {
                throw new InvalidOperationException(Helper.NeutralFormat("This is already a feature set named \"{0}\" exist", setName));
            }

            // Create a LabelFeatureNameSet which contains the mandotary feature name already.
            LabelFeatureNameSet set = new LabelFeatureNameSet();

            // Add the feature names one by one.
            int index = set._featureNameToIndex.Count;
            foreach (string featureName in featureNames)
            {
                if (!set._featureNameToIndex.ContainsKey(featureName))
                {
                    set._featureNameToIndex.Add(featureName, index++);
                }
            }

            // Whether the feature is too many?
            if (set._featureNameToIndex.Count > SeparatorChars.Length)
            {
                throw new InvalidDataException(Helper.NeutralFormat("The number of feature is too many for storage : {0}", set._featureNameToIndex.Count));
            }

            NamedSet.Add(setName, set);
            return set;
        }

        /// <summary>
        /// Tests the set named as given whether exists.
        /// </summary>
        /// <param name="setName">The given set name.</param>
        /// <returns>True or false to indicate whether exits.</returns>
        public static bool Exist(string setName)
        {
            return NamedSet.ContainsKey(setName);
        }

        /// <summary>
        /// Queries the set named as given.
        /// </summary>
        /// <param name="setName">The given set name.</param>
        /// <returns>The corresponding set.</returns>
        public static LabelFeatureNameSet Query(string setName)
        {
            return NamedSet[setName];
        }

        /// <summary>
        /// Removes the given set.
        /// </summary>
        /// <param name="setName">The given set name.</param>
        /// <returns>True to indicate the set is removed successfully, false to indicate the set is not found.</returns>
        public static bool Remove(string setName)
        {
            return NamedSet.Remove(setName);
        }

        /// <summary>
        /// Gets the index of the given feature.
        /// </summary>
        /// <param name="featureName">The name of the given feature.</param>
        /// <returns>The index of the feature.</returns>
        public int GetIndex(string featureName)
        {
            try
            {
                return _featureNameToIndex[featureName];
            }
            catch (KeyNotFoundException e)
            {
                throw new InvalidDataException(Helper.NeutralFormat("Unknown feature name \"{0}\"", featureName), e);
            }
        }

        /// <summary>
        /// Gets the feature name of the given index.
        /// </summary>
        /// <param name="indexOfFeature">The index of the feature.</param>
        /// <returns>The feature name of the given index.</returns>
        public string GetFeatureName(int indexOfFeature)
        {
            if (indexOfFeature < 0 || indexOfFeature >= _featureNameToIndex.Count)
            {
                throw new ArgumentOutOfRangeException("indexOfFeature");
            }

            foreach (KeyValuePair<string, int> kvp in _featureNameToIndex)
            {
                if (kvp.Value == indexOfFeature)
                {
                    return kvp.Key;
                }
            }

            throw new InvalidDataException("Corrupt feature set");
        }

        /// <summary>
        /// Gets the left separator of the given feature.
        /// </summary>
        /// <param name="featureName">The given feature name.</param>
        /// <returns>The left separator string.</returns>
        public string GetLeftSeparator(string featureName)
        {
            return GetLeftSeparator(GetIndex(featureName));
        }

        /// <summary>
        /// Gets the right separator of the given feature.
        /// </summary>
        /// <param name="featureName">The given feature name.</param>
        /// <returns>The right separator string.</returns>
        public string GetRightSeparator(string featureName)
        {
            return GetRightSeparator(GetIndex(featureName));
        }

        /// <summary>
        /// Gets the left separator of the given feature index.
        /// </summary>
        /// <param name="indexOfFeature">The given feature index.</param>
        /// <returns>The left separator string.</returns>
        public string GetLeftSeparator(int indexOfFeature)
        {
            return (indexOfFeature == 0) ? string.Empty : SeparatorChars.Substring(indexOfFeature - 1, 1);
        }

        /// <summary>
        /// Gets the right separator of the given feature index.
        /// </summary>
        /// <param name="indexOfFeature">The given feature index.</param>
        /// <returns>The right separator string.</returns>
        public string GetRightSeparator(int indexOfFeature)
        {
            return SeparatorChars.Substring(indexOfFeature, 1);
        }

        #endregion
    }

    /// <summary>
    /// This class is designed to hold the information about the htk label.
    /// Htk label looks like: 1) mono phone: "ax"
    ///                       2) tri-phone: "ax-b+eh"
    ///                       3) full-context: "ae-n+d+4|1...20-4".
    /// </summary>
    public class Label
    {
        #region Fields

        /// <summary>
        /// Gets the string to indicate the not available  or not applicable value.
        /// </summary>
        public const string NotApplicableFeatureValue = "null";

        /// <summary>
        /// The text of this label.
        /// </summary>
        private string _text;

        /// <summary>
        /// The all values of this feature vector.
        /// </summary>
        private string[] _featureValues;

        /// <summary>
        /// The LabelFeatureNameSet which contains all feature name and order info.
        /// </summary>
        private LabelFeatureNameSet _featureNames;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the Label class by using default LabelFeatureNameSet.
        /// </summary>
        public Label()
            : this(LabelFeatureNameSet.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Label class by using a given LabelFeatureNameSet.
        /// </summary>
        /// <param name="featureNams">The given feature name set.</param>
        public Label(LabelFeatureNameSet featureNams)
        {
            _featureNames = featureNams;
        }

        /// <summary>
        /// Initializes a new instance of the Label class as a copy of the given one.
        /// </summary>
        /// <param name="label">The given label to copy.</param>
        public Label(Label label)
        {
            _featureNames = label._featureNames;
            Text = label.Text;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the text of the label.
        /// Sets the text will cause a parsing for the text to fill all the feature.
        /// </summary>
        public string Text
        {
            get { return ToString(); }
            set { UpdateFields(value); }
        }

        /// <summary>
        /// Gets or sets the label feature name set.
        /// </summary>
        public LabelFeatureNameSet FeatureNameSet
        {
            get { return _featureNames; }
            set { _featureNames = value; }
        }

        /// <summary>
        /// Gets the count of feature values.
        /// </summary>
        public int FeatureValueCount
        {
            get
            {
                // Since the _feautureValues is null, the feature value count should be same as the featureNames' count.
                return (_featureValues == null) ? _featureNames.Count : _featureValues.Length;
            }
        }

        /// <summary>
        /// Gets or sets the central phoneme.
        /// </summary>
        public string CentralPhoneme
        {
            get
            {
                return GetFeatureValue(LabelFeatureNameSet.CentralPhonemeFeatureName);
            }

            set
            {
                SetFeatureValue(LabelFeatureNameSet.CentralPhonemeFeatureName, value);
            }
        }

        /// <summary>
        /// Gets or sets the left phoneme.
        /// </summary>
        public string LeftPhoneme
        {
            get
            {
                return GetFeatureValue(LabelFeatureNameSet.LeftPhonemeFeatureName);
            }

            set
            {
                SetFeatureValue(LabelFeatureNameSet.LeftPhonemeFeatureName, value);
            }
        }

        /// <summary>
        /// Gets or sets the right phoneme.
        /// </summary>
        public string RightPhoneme
        {
            get
            {
                return GetFeatureValue(LabelFeatureNameSet.RightPhonemeFeatureName);
            }

            set
            {
                SetFeatureValue(LabelFeatureNameSet.RightPhonemeFeatureName, value);
            }
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// Test whether the input value is not available (or not applicable) value.
        /// </summary>
        /// <param name="value">The input value to be tested.</param>
        /// <returns>Ture to indicate the value is not applicable value. Otherwise, false.</returns>
        public static bool IsNotApplicableValue(string value)
        {
            return NotApplicableFeatureValue == value;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Resizes the feature value according to the given LabelFeatureNameSet.
        /// </summary>
        /// <param name="set">The given LabelFeatureNameSet.</param>
        public void ResizeFeatureValue(LabelFeatureNameSet set)
        {
            _featureNames = set;

            // null _featureValues indidates it's initialized, so needn't update.
            if (_featureValues != null)
            {
                int oldLength = _featureValues.Length;
                Array.Resize(ref _featureValues, set.Count);

                // Set the new fields as NotApplicableFeatureValue.
                for (int i = oldLength; i < _featureValues.Length; ++i)
                {
                    _featureValues[i] = NotApplicableFeatureValue;
                }

                // The _text should be updated.
                _text = null;
            }
        }

        /// <summary>
        /// Gets the feature value of the given feature name.
        /// </summary>
        /// <param name="featureName">The given feature name.</param>
        /// <returns>The value in string type of the given feature.</returns>
        public string GetFeatureValue(string featureName)
        {
            int index = _featureNames.GetIndex(featureName);
            if (_featureValues == null)
            {
                // null _featureValues indidates it's initialized.
                return NotApplicableFeatureValue;
            }

            if (index < _featureValues.Length)
            {
                return _featureValues[index];
            }

            throw new InvalidDataException(Helper.NeutralFormat("This label cannot provide the feature \"{0}\"", featureName));
        }

        /// <summary>
        /// Set the feature value of the given feature name.
        /// </summary>
        /// <param name="featureName">The given feature name.</param>
        /// <param name="value">The value in string type of the given feature.</param>
        public void SetFeatureValue(string featureName, string value)
        {
            int index = _featureNames.GetIndex(featureName);
            if (index >= FeatureValueCount)
            {
                throw new InvalidDataException(Helper.NeutralFormat("This label doesn't contains the feature \"{0}\"", featureName));
            }

            if (_featureValues == null)
            {
                if (value != NotApplicableFeatureValue)
                {
                    // null _featureValues indidates it's initialized, needs intialize here.
                    _featureValues = new string[_featureNames.Count];
                    for (int i = 0; i < _featureValues.Length; ++i)
                    {
                        _featureValues[i] = NotApplicableFeatureValue;
                    }

                    _text = null;
                    _featureValues[index] = value;
                }
            }
            else
            {
                if (_featureValues[index] != value)
                {
                    // The text will be update since the value is updated. So, set the _text to null here.
                    _text = null;
                    _featureValues[index] = value;
                }
            }
        }

        /// <summary>
        /// Override the ToString() method to build the text of the label.
        /// </summary>
        /// <returns>The label text.</returns>
        public override string ToString()
        {
            // Only perform the actions when the _text is null.
            if (_text == null)
            {
                StringBuilder label = new StringBuilder();

                if (_featureValues == null)
                {
                    // null _featureValues indidates it's initialized, needs intialize here.
                    // The left of the first value has no separactor characters.
                    for (int i = 0; i < _featureNames.Count; ++i)
                    {
                        label.Append(NotApplicableFeatureValue);
                        label.Append(LabelFeatureNameSet.SeparatorChars[i]);
                    }
                }
                else
                {
                    // The left of the first value has no separactor characters.
                    for (int i = 0; i < _featureValues.Length; ++i)
                    {
                        label.Append(_featureValues[i]);
                        label.Append(LabelFeatureNameSet.SeparatorChars[i]);
                    }
                }

                _text = label.ToString();
            }

            return _text;
        }

        /// <summary>
        /// Override the Equals() method.
        /// </summary>
        /// <param name="obj">The given object to compare.</param>
        /// <returns>True indicate the two object is equal, otherwise false.</returns>
        public override bool Equals(object obj)
        {
            Label label = obj as Label;
            if (label == null)
            {
                return false;
            }

            return Text == label.Text;
        }

        /// <summary>
        /// Override the GetHashCode() method.
        /// </summary>
        /// <returns>The hash code of this object.</returns>
        public override int GetHashCode()
        {
            return Text.GetHashCode();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Updates the fields according to the given text.
        /// </summary>
        /// <param name="text">The given text of this label.</param>
        private void UpdateFields(string text)
        {
            string[] features = text.Split(LabelFeatureNameSet.SeparatorChars.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            switch (features.Length)
            {
                case 1:
                    // Just a central phoneme, update the LabelFeatureNameSet accordingly.
                    _featureNames = LabelFeatureNameSet.MonoLabel;
                    _featureValues = features;
                    _text = text;
                    break;
                case 2:
                    // Invalid text for Label.
                    throw new InvalidDataException(Helper.NeutralFormat("Invalid Htk label \"{0}\"", text));
                default:
                    if (_featureNames.Count != LabelFeatureNameSet.Default.Count &&
                        features.Length != _featureNames.Count)
                    {
                        // Since the number of feature is mismatch with the number of feature name, exception thrown.
                        throw new InvalidDataException("Unmatched feature value and feature name");
                    }

                    _featureValues = features;
                    _text = text;
                    break;
            }
        }

        #endregion
    }
}