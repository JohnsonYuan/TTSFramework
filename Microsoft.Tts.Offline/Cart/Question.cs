//----------------------------------------------------------------------------
// <copyright file="Question.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements CART question
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Cart
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Not, '~' operation.
    /// </summary>
    public class NotOperator
    {
        #region Fields

        private bool _not;
        private int _featureId;

        private MetaCart _metaCart;
        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="NotOperator"/> class.
        /// </summary>
        /// <param name="metaCart">CART metadata.</param>
        /// <param name="not">Is not operator.</param>
        /// <param name="id">Feature id.</param>
        public NotOperator(MetaCart metaCart, bool not, int id)
        {
            if (metaCart == null)
            {
                throw new ArgumentNullException("metaCart");
            }

            if (metaCart.Features == null)
            {
                throw new ArgumentException("metaCart.Features is null");
            }

            _metaCart = metaCart;

            _not = not;

            Debug.Assert(metaCart.Features.ContainsKey(id));
            _featureId = id;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets MetaCart.
        /// </summary>
        public MetaCart MetaCart
        {
            get { return _metaCart; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this is a NOT operation.
        /// </summary>
        public bool IsNot
        {
            get { return _not; }
            set { _not = value; }
        }

        /// <summary>
        /// Gets or sets Feature Identify, indicate which feature to test for this question.
        /// </summary>
        public int FeatureId
        {
            get { return _featureId; }
            set { _featureId = value; }
        }

        /// <summary>
        /// Gets Descript this operator in meta feature data.
        /// </summary>
        public string Description
        {
            get
            {
                if (IsNot)
                {
                    return "~{" + MetaCart.Features[FeatureId].Description + "}";
                }
                else
                {
                    return "{" + MetaCart.Features[FeatureId].Description + "}";
                }
            }
        }
        #endregion

        #region Presentation

        /// <summary>
        /// Convert to string presentation.
        /// </summary>
        /// <returns>String presentation.</returns>
        public override string ToString()
        {
            if (IsNot)
            {
                return "~" + FeatureId.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                return FeatureId.ToString(CultureInfo.InvariantCulture);
            }
        }

        #endregion
    }

    /// <summary>
    /// AND operation.
    /// <![CDATA['&']]>
    /// </summary>
    public class AndOperator
    {
        #region Fields

        private Collection<NotOperator> _notOperators = new Collection<NotOperator>();

        private MetaCart _metaCart;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="AndOperator"/> class.
        /// </summary>
        /// <param name="metaCart">CART metadata.</param>
        public AndOperator(MetaCart metaCart)
        {
            if (metaCart == null)
            {
                throw new ArgumentNullException("metaCart");
            }

            _metaCart = metaCart;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets MetaCart.
        /// </summary>
        public MetaCart MetaCart
        {
            get { return _metaCart; }
        }

        /// <summary>
        /// Gets on these NotOps.
        /// </summary>
        public Collection<NotOperator> NotOperators
        {
            get { return _notOperators; }
        }

        /// <summary>
        /// Gets Descript this operator in meta feature data.
        /// </summary>
        public string Description
        {
            get
            {
                StringBuilder ret = new StringBuilder();
                foreach (NotOperator notop in NotOperators)
                {
                    if (ret.Length != 0)
                    {
                        ret.Append("&");
                    }

                    ret.Append(notop.Description);
                }

                return ret.ToString();
            }
        }

        #endregion

        #region Operations

        /// <summary>
        /// Pare OR string.
        /// </summary>
        /// <param name="or">OR string to parse.</param>
        public void Parse(string or)
        {
            if (string.IsNullOrEmpty(or))
            {
                throw new ArgumentNullException("or");
            }

            NotOperators.Clear();

            string[] items = or.Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string item in items)
            {
                if (item.StartsWith("~", StringComparison.Ordinal))
                {
                    NotOperators.Add(new NotOperator(MetaCart, true,
                        int.Parse(item.Substring(1), CultureInfo.InvariantCulture)));
                }
                else
                {
                    NotOperators.Add(new NotOperator(MetaCart, false,
                        int.Parse(item, CultureInfo.InvariantCulture)));
                }
            }
        }

        /// <summary>
        /// Verify whether feature is satisfy with this question.
        /// </summary>
        /// <param name="feature">Feature.</param>
        /// <returns>True if the feature satisfies this logic, otherwise false.</returns>
        public bool Test(TtsUnitFeature feature)
        {
            foreach (NotOperator nop in NotOperators)
            {
                bool fpass = TestFeature(nop.FeatureId, feature);
                if ((!fpass && !nop.IsNot) || (fpass && nop.IsNot))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Verify on a special feature whether feature is satisfy with this question.
        /// </summary>
        /// <param name="featureId">Feature id.</param>
        /// <param name="feature">Feature.</param>
        /// <returns>True if this logic satisfies the feature, otherwise false.</returns>
        public bool TestFeature(int featureId, TtsUnitFeature feature)
        {
            return MetaCart.Features[featureId].Test(feature);
        }

        #endregion

        #region Presentation

        /// <summary>
        /// Convert to string presentation.
        /// </summary>
        /// <returns>String presentation.</returns>
        public override string ToString()
        {
            StringBuilder ret = new StringBuilder();
            foreach (NotOperator notop in NotOperators)
            {
                if (ret.Length != 0)
                {
                    ret.Append("&");
                }

                ret.Append(notop.ToString());
            }

            return ret.ToString();
        }

        #endregion
    }

    /// <summary>
    /// CART question.
    /// </summary>
    public class Question
    {
        #region Fields

        private Collection<AndOperator> _andOperators;
        private MetaCart _metaCart;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="Question"/> class.
        /// </summary>
        /// <param name="metaCart">CART metadata.</param>
        public Question(MetaCart metaCart)
        {
            if (metaCart == null)
            {
                throw new ArgumentNullException("metaCart");
            }

            _metaCart = metaCart;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets MetaCart.
        /// </summary>
        public MetaCart MetaCart
        {
            get { return _metaCart; }
        }

        /// <summary>
        /// Gets '|' execute OR operation on all these _andOperators.
        /// </summary>
        public Collection<AndOperator> AndOperators
        {
            get { return _andOperators; }
        }

        /// <summary>
        /// Gets Decription.
        /// </summary>
        public string Description
        {
            get
            {
                StringBuilder ret = new StringBuilder();

                foreach (AndOperator andop in _andOperators)
                {
                    if (ret.Length != 0)
                    {
                        ret.Append("|");
                    }

                    ret.Append(andop.Description);
                }

                return ret.ToString();
            }
        }

        #endregion

        #region Question file management

        /// <summary>
        /// This will convert the cart question file from description strings
        /// Into binary id data
        /// For example, convert:
        ///     0 PosInSyllable Onset
        /// To:
        ///     0   2 0.
        /// </summary>
        /// <param name="sourceFile">Source CART question file in naming.</param>
        /// <param name="targetFile">Target CART question file in ids.</param>
        /// <param name="language">Which language the CART question is for.</param>
        /// <param name="engine">Which engine the CART question is for.</param>
        public static void String2IdConvert(string sourceFile,
            string targetFile, Language language, EngineType engine)
        {
            Phoneme phoneme = Localor.GetPhoneme(language, engine);
            Helper.EnsureFolderExistForFile(targetFile);
            using (StreamWriter sw = new StreamWriter(targetFile))
            using (StreamReader sr = new StreamReader(sourceFile))
            {
                string line = null;
                while ((line = sr.ReadLine()) != null)
                {
                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }

                    string[] items = line.Split(new char[] { ' ' },
                    StringSplitOptions.RemoveEmptyEntries);

                    string index = items[0];
                    TtsFeature feature = (TtsFeature)Enum.Parse(typeof(TtsFeature), items[1]);
                    string[] values = items[2].Split(new char[] { ',' },
                        StringSplitOptions.RemoveEmptyEntries);

                    StringBuilder sb = new StringBuilder();
                    foreach (string val in values)
                    {
                        if (sb.Length > 0)
                        {
                            sb.Append(",");
                        }

                        int valId = 0;
                        switch (feature)
                        {
                            case TtsFeature.PosInSentence:
                                PosInSentence pis = (PosInSentence)Enum.Parse(typeof(PosInSentence), val);
                                valId = (int)pis;
                                break;
                            case TtsFeature.PosInWord:
                                PosInWord piw = (PosInWord)Enum.Parse(typeof(PosInWord), val);
                                valId = (int)piw;
                                break;
                            case TtsFeature.PosInSyllable:
                                PosInSyllable piy = (PosInSyllable)Enum.Parse(typeof(PosInSyllable), val);
                                valId = (int)piy;
                                break;
                            case TtsFeature.LeftContextPhone:
                                valId = phoneme.TtsPhone2Id(val);
                                break;
                            case TtsFeature.RightContextPhone:
                                valId = phoneme.TtsPhone2Id(val);
                                break;
                            case TtsFeature.LeftContextTone:
                                valId = phoneme.ToneManager.GetContextToneId(val);
                                break;
                            case TtsFeature.RightContextTone:
                                valId = phoneme.ToneManager.GetContextToneId(val);
                                break;
                            case TtsFeature.TtsStress:
                                TtsStress stress = (TtsStress)Enum.Parse(typeof(TtsStress), val);
                                valId = (int)stress;
                                break;
                            case TtsFeature.TtsEmphasis:
                                TtsEmphasis emphasis = (TtsEmphasis)Enum.Parse(typeof(TtsEmphasis), val);
                                valId = (int)emphasis;
                                break;
                            default:
                                break;
                        }

                        sb.Append(valId.ToString(CultureInfo.InvariantCulture));
                    }

                    sw.WriteLine(Helper.NeutralFormat("{0} {1} {2}", index, (int)feature, sb.ToString()));
                }
            }
        }

        /// <summary>
        /// This will convert the cart question file from binary id data into
        /// Description strings
        /// For example, convert:
        ///     0   2 0
        /// To:
        ///     0 PosInSyllable Onset.
        /// </summary>
        /// <param name="sourceFile">Source CART question file in ids.</param>
        /// <param name="targetFile">Target CART question file in naming.</param>
        /// <param name="language">Which language the CART question is for.</param>
        /// <param name="engine">Which engine the CART question is for.</param>
        public static void Id2StringConvert(string sourceFile,
            string targetFile, Language language, EngineType engine)
        {
            Phoneme phoneme = Localor.GetPhoneme(language, engine);

            Helper.EnsureFolderExistForFile(targetFile);
            using (StreamWriter sw = new StreamWriter(targetFile))
            using (StreamReader sr = new StreamReader(sourceFile))
            {
                string line = null;
                while ((line = sr.ReadLine()) != null)
                {
                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }

                    string[] items = line.Split(new char[] { ' ' },
                        StringSplitOptions.RemoveEmptyEntries);

                    string index = items[0];
                    TtsFeature feature =
                        (TtsFeature)int.Parse(items[1], CultureInfo.InvariantCulture);
                    string[] values = items[2].Split(new char[] { ',' },
                        StringSplitOptions.RemoveEmptyEntries);

                    StringBuilder sb = new StringBuilder();
                    foreach (string val in values)
                    {
                        int valId = int.Parse(val, CultureInfo.InvariantCulture);
                        if (sb.Length > 0)
                        {
                            sb.Append(",");
                        }

                        switch (feature)
                        {
                            case TtsFeature.PosInSentence:
                                Debug.Assert(valId >= 0 && valId <= (int)PosInSentence.Quest);
                                PosInSentence pis = (PosInSentence)valId;
                                sb.Append(pis.ToString());
                                break;

                            case TtsFeature.PosInWord:
                                Debug.Assert(valId >= 0 && valId <= (int)PosInWord.Mono);
                                PosInWord piw = (PosInWord)valId;
                                sb.Append(piw.ToString());
                                break;

                            case TtsFeature.PosInSyllable:
                                Debug.Assert(valId >= 0 && valId <= (int)PosInSyllable.Coda);
                                PosInSyllable piy = (PosInSyllable)valId;
                                sb.Append(piy.ToString());
                                break;

                            case TtsFeature.LeftContextPhone:
                                Debug.Assert(!string.IsNullOrEmpty(phoneme.TtsId2Phone(valId)));
                                sb.Append(phoneme.TtsId2Phone(valId));
                                break;

                            case TtsFeature.RightContextPhone:
                                Debug.Assert(!string.IsNullOrEmpty(phoneme.TtsId2Phone(valId)));
                                sb.Append(phoneme.TtsId2Phone(valId));
                                break;

                            case TtsFeature.LeftContextTone:
                                Debug.Assert(phoneme.ToneManager.ContextIdMap.ContainsKey(valId));
                                sb.Append(phoneme.ToneManager.GetNameFromContextId(valId));
                                break;
                            case TtsFeature.RightContextTone:
                                Debug.Assert(phoneme.ToneManager.ContextIdMap.ContainsKey(valId));
                                sb.Append(phoneme.ToneManager.GetNameFromContextId(valId));
                                break;
                            case TtsFeature.TtsStress:
                                Debug.Assert(valId >= 0 && valId <= (int)TtsStress.Tertiary);
                                TtsStress stress = (TtsStress)valId;
                                sb.Append(stress.ToString());
                                break;

                            case TtsFeature.TtsEmphasis:
                                Debug.Assert(valId >= 0 && valId <= (int)TtsEmphasis.Yes);
                                TtsEmphasis emphasis = (TtsEmphasis)valId;
                                sb.Append(emphasis.ToString());
                                break;
                            default:
                                break;
                        }
                    }

                    sw.WriteLine(Helper.NeutralFormat("{0} {1} {2}", index, feature, sb.ToString()));
                }
            }
        }

        /// <summary>
        /// Filter the Ids question file into a feature-specified question file.
        /// </summary>
        /// <param name="sourceFile">Source file.</param>
        /// <param name="targetFile">Target file after filtered.</param>
        /// <param name="feature">Feature used to filter.</param>
        public static void FilterQuestion(string sourceFile,
            string targetFile, TtsFeature feature)
        {
            Helper.EnsureFolderExistForFile(targetFile);
            using (StreamWriter sw = new StreamWriter(targetFile))
            using (StreamReader sr = new StreamReader(sourceFile))
            {
                string line = null;
                while ((line = sr.ReadLine()) != null)
                {
                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }

                    string[] items = line.Split(new char[] { ' ' },
                        StringSplitOptions.RemoveEmptyEntries);

                    TtsFeature currentFeature =
                        (TtsFeature)int.Parse(items[1], CultureInfo.InvariantCulture);

                    if (currentFeature == feature)
                    {
                        sw.WriteLine(line);
                    }
                }
            }
        }

        #endregion

        #region Operations

        /// <summary>
        /// Parsing question logic.
        /// </summary>
        /// <param name="logic">Logic string to parse.</param>
        public void Parse(string logic)
        {
            if (string.IsNullOrEmpty(logic))
            {
                throw new ArgumentNullException("logic");
            }

            _andOperators = new Collection<AndOperator>();
            string[] ors = logic.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string or in ors)
            {
                AndOperator op = new AndOperator(MetaCart);
                op.Parse(or);
                _andOperators.Add(op);
            }
        }

        /// <summary>
        /// Verify whether feature is satisfy with this question.
        /// </summary>
        /// <param name="feature">Feature.</param>
        /// <returns>True if the feature satisfies this question , otherwise false.</returns>
        public bool Test(TtsUnitFeature feature)
        {
            foreach (AndOperator andop in _andOperators)
            {
                if (andop.Test(feature))
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Presentation

        /// <summary>
        /// Convert to string presentation.
        /// </summary>
        /// <returns>String presentation.</returns>
        public override string ToString()
        {
            StringBuilder ret = new StringBuilder();
            foreach (AndOperator andop in _andOperators)
            {
                if (ret.Length != 0)
                {
                    ret.Append("|");
                }

                ret.Append(andop.ToString());
            }

            return ret.ToString();
        }

        #endregion
    }
}