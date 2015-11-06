//----------------------------------------------------------------------------
// <copyright file="ConsistencyChecker.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements HTS object model comparing
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Font.Hts
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Text;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Htk;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Interface for custom decision tree checking actions.
    /// </summary>
    public interface IDecisionTreeChecker
    {
        /// <summary>
        /// Check actual against expected decision tree.
        /// </summary>
        /// <param name="expected">Expected tree.</param>
        /// <param name="actual">Actual tree.</param>
        void Check(DecisionTree expected, DecisionTree actual);
    }

    /// <summary>
    /// Consistency checker for HTS font object models.
    /// </summary>
    public static class ConsistencyChecker
    {
        #region Check

        /// <summary>
        /// Validate cross read and write of HTS font.
        /// </summary>
        /// <param name="font">Font to test with.</param>
        /// <returns>True if pass.</returns>
        public static bool ValidateCrossSerialization(HtsFont font)
        {
            Helper.ThrowIfNull(font);

            HtsFontSerializer serializer = new HtsFontSerializer();

            using (MemoryStream firstStream = new MemoryStream())
            {
                DataWriter firstWriter = new DataWriter(firstStream);
                uint size = serializer.Write(font, firstWriter);

                using (HtsFont htsFont = new HtsFont(font.PhoneSet, font.PosSet))
                {
                    HtsFont firstDerivedFont = serializer.Read(htsFont, firstWriter.BaseStream.Excerpt(size));
                    ConsistencyChecker.Check(font, firstDerivedFont);

                    using (MemoryStream secondStream = new MemoryStream())
                    {
                        DataWriter secondWriter = new DataWriter(secondStream);
                        size = serializer.Write(firstDerivedFont, secondWriter);

                        using (HtsFont hts = new HtsFont(font.PhoneSet, font.PosSet))
                        {
                            HtsFont secondDerivedFont = serializer.Read(hts, secondWriter.BaseStream.Excerpt(size));
                            ConsistencyChecker.Check(firstDerivedFont, secondDerivedFont);
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Check the consistency between two fonts.
        /// </summary>
        /// <param name="left">Left font instance to compare.</param>
        /// <param name="right">Right font instance to compare.</param>
        public static void Check(HtsFont left, HtsFont right)
        {
            Helper.ThrowIfNull(left);
            Helper.ThrowIfNull(right);

            Check(left.Header, right.Header);
            Check(left.Questions, right.Questions);
            Check(left.Questions.Items == null ? left.UnionQuestions : left.Questions.Items,
                right.Questions.Items == null ? right.UnionQuestions : right.Questions.Items);
            foreach (Pair<HtsModel, HtsModel> pair in left.Models.Values.Pairs(right.Models.Values))
            {
                Check(pair.Left, pair.Right);
            }
        }

        /// <summary>
        /// Check the consistency between decision forests.
        /// </summary>
        /// <param name="left">Left decision forest.</param>
        /// <param name="right">Right decision forest.</param>
        /// <param name="customChecker">Custom decision tree checker.</param>
        public static void Check(DecisionForest left, DecisionForest right, IDecisionTreeChecker customChecker)
        {
            Helper.ThrowIfNull(left);
            Helper.ThrowIfNull(right);

            if (left.TreeList.Count != right.TreeList.Count)
            {
                throw new DataMismatchException(
                    Helper.NeutralFormat("Numbers of trees in two forests[{0}] do not equal with each other."));
            }

            for (int i = 0; i < left.TreeList.Count; i++)
            {
                customChecker.Check(left.TreeList[i], right.TreeList[i]);
                Check(left.TreeList[i].NodeList, right.TreeList[i].NodeList);
            }
        }

        /// <summary>
        /// Check the consistency between HTS font headers.
        /// </summary>
        /// <param name="left">Left font header.</param>
        /// <param name="right">Right font header.</param>
        internal static void Check(HtsFontHeader left, HtsFontHeader right)
        {
            Helper.ThrowIfNull(left);
            Helper.ThrowIfNull(right);

            byte[] leftBytes = left.ToBytes();
            byte[] rightBytes = right.ToBytes();
            Check(leftBytes, rightBytes);
        }

        /// <summary>
        /// Check the consistency between Hts models.
        /// </summary>
        /// <param name="left">Left model.</param>
        /// <param name="right">Right model.</param>
        internal static void Check(HtsModel left, HtsModel right)
        {
            Helper.ThrowIfNull(left);
            Helper.ThrowIfNull(right);

            Check(left.Header, right.Header);
            Check(left.WindowSet, right.WindowSet);
            Check(left.Forest, right.Forest, new HtsTreeChecker());
            Check(left.MmfFile, right.MmfFile, !left.Header.GaussianConfig.IsFixedPoint);
            Check(left.Font.StringPool, right.Font.StringPool);
        }

        /// <summary>
        /// Check the consistency between Hts MMF.
        /// </summary>
        /// <param name="left">Left MMF file instance.</param>
        /// <param name="right">Right MMF file instance.</param>
        /// <param name="compareData">To compare data.</param>
        internal static void Check(HtsMmfFile left, HtsMmfFile right, bool compareData)
        {
            Helper.ThrowIfNull(left);
            Helper.ThrowIfNull(right);

            Check(left.Streams.Where(s => s.ModelType == left.ModelType), right.Streams, compareData);
        }

        /// <summary>
        /// Check the consistency between HMM streams.
        /// </summary>
        /// <param name="lefts">Left streams.</param>
        /// <param name="rights">Right streams.</param>
        /// <param name="compareData">To compare data.</param>
        internal static void Check(IEnumerable<HmmStream> lefts, IEnumerable<HmmStream> rights, bool compareData)
        {
            Helper.ThrowIfNull(lefts);
            Helper.ThrowIfNull(rights);

            foreach (Pair<HmmStream, HmmStream> pair in lefts.Pairs(rights))
            {
                if (!HmmStreamComparer.IsEqual(pair.Left.Gaussians, pair.Right.Gaussians, compareData))
                {
                    throw new DataMismatchException(
                        Helper.NeutralFormat("HMM Streams of two lists do not equal with each other."));
                }
            }
        }

        /// <summary>
        /// Check consistency between Gaussian lists.
        /// </summary>
        /// <param name="lefts">Left Gaussian list.</param>
        /// <param name="rights">Right Gaussian list.</param>
        /// <param name="compareData">To compare data.</param>
        internal static void Check(Gaussian[] lefts, Gaussian[] rights, bool compareData)
        {
            Helper.ThrowIfNull(lefts);
            Helper.ThrowIfNull(rights);

            if (!HmmStreamComparer.IsEqual(lefts, rights, compareData))
            {
                throw new DataMismatchException(
                    Helper.NeutralFormat("Gaussians of two lists do not equal with each other."));
            }
        }

        /// <summary>
        /// Check the consistency between string memory pool.
        /// </summary>
        /// <param name="left">Left string memory pool.</param>
        /// <param name="right">Right string memory pool.</param>
        internal static void Check(StringPool left, StringPool right)
        {
            Helper.ThrowIfNull(left);
            Helper.ThrowIfNull(right);

            Check(left.ToArray(), right.ToArray());
        }

        /// <summary>
        /// Check the consistency between question set.
        /// </summary>
        /// <param name="left">Left question set.</param>
        /// <param name="right">Right question set.</param>
        internal static void Check(HtsQuestionSet left, HtsQuestionSet right)
        {
            Helper.ThrowIfNull(left);
            Helper.ThrowIfNull(right);

            if (left.Header.HasQuestionName != right.Header.HasQuestionName)
            {
                throw new DataMismatchException(
                    Helper.NeutralFormat("Flags indicating whether having question name do not equal with each other."));
            }

            Check(left.Items, right.Items);
        }

        /// <summary>
        /// Check the consistency between questions.
        /// </summary>
        /// <param name="left">Left questions.</param>
        /// <param name="right">Right questions.</param>
        internal static void Check(IEnumerable<Question> left, IEnumerable<Question> right)
        {
            Helper.ThrowIfNull(left);
            Helper.ThrowIfNull(right);

            foreach (Pair<Question, Question> pair in left.Pairs(right))
            {
                if (pair.Left.FeatureName != null && pair.Right.FeatureName != null)
                {
                    if (pair.Left.FeatureName != pair.Right.FeatureName)
                    {
                        throw new DataMismatchException(
                            Helper.NeutralFormat("Feature names of two questions do not equal with each other."));
                    }
                }

                if (pair.Left.Oper != pair.Right.Oper)
                {
                    throw new DataMismatchException(
                        Helper.NeutralFormat("Question operators of two questions do not equal with each other."));
                }

                Check(pair.Left.CodeValueSet, pair.Right.CodeValueSet);
            }
        }

        /// <summary>
        /// Check the consistency between decision trees.
        /// </summary>
        /// <param name="lefts">Left root node.</param>
        /// <param name="rights">Right root node.</param>
        internal static void Check(IList<DecisionTreeNode> lefts, IList<DecisionTreeNode> rights)
        {
            Helper.ThrowIfNull(lefts);
            Helper.ThrowIfNull(rights);

            List<DecisionTreeNode> leftNodes = new List<DecisionTreeNode>(lefts.First().LayeredNodes);
            List<DecisionTreeNode> rightNodes = new List<DecisionTreeNode>(rights.First().LayeredNodes);
            if (leftNodes.Count != rightNodes.Count)
            {
                throw new DataMismatchException(
                    Helper.NeutralFormat("The numbers of nodes in two decision trees do not equal with each other."));
            }

            for (int i = 0; i < leftNodes.Count(); i++)
            {
                if (leftNodes[i].NodeType != rightNodes[i].NodeType)
                {
                    throw new DataMismatchException(
                        Helper.NeutralFormat("Node types of two decision tree nodes do not equal with each other."));
                }

                if (leftNodes[i].Position != rightNodes[i].Position)
                {
                    throw new DataMismatchException(
                        Helper.NeutralFormat("Positions of two decision tree nodes do not equal with each other."));
                }
            }
        }

        /// <summary>
        /// Checks the consistency between binary serializers.
        /// </summary>
        /// <typeparam name="T">Type of the specified serializer.</typeparam>
        /// <param name="left">Left binary serializer.</param>
        /// <param name="right">Right binary serializer.</param>
        internal static void Check<T>(IBinarySerializer<T> left, IBinarySerializer<T> right)
        {
            Helper.ThrowIfNull(left);
            Helper.ThrowIfNull(right);

            Check(left.ToArray(), right.ToArray());
        }

        #endregion

        #region Support functions

        /// <summary>
        /// Check the consistency between element list.
        /// </summary>
        /// <typeparam name="T">Type of element.</typeparam>
        /// <param name="lefts">Left elements.</param>
        /// <param name="rights">Right elements.</param>
        private static void Check<T>(IEnumerable<T> lefts, IEnumerable<T> rights)
        {
            Helper.ThrowIfNull(lefts);
            Helper.ThrowIfNull(rights);

            int index = 0;
            foreach (Pair<T, T> pair in lefts.Pairs(rights))
            {
                if (!pair.Left.Equals(pair.Right))
                {
                    throw new DataMismatchException(
                        Helper.NeutralFormat("Values in Pair [{0}] do not equal with each other.", index));
                }

                index++;
            }
        }

        #endregion
    }

    /// <summary>
    /// Pair extension.
    /// </summary>
    public static class PairExtension
    {
        #region Operations
        /// <summary>
        /// Enumerate pairs from given lists.
        /// </summary>
        /// <typeparam name="T">Type of element.</typeparam>
        /// <param name="lefts">Left items.</param>
        /// <param name="rights">Right items.</param>
        /// <returns>Pair enumerator.</returns>
        internal static IEnumerable<Pair<T, T>> Pairs<T>(this IEnumerable<T> lefts, IEnumerable<T> rights)
        {
            Helper.ThrowIfNull(lefts);
            Helper.ThrowIfNull(rights);

            List<T> leftList = new List<T>(lefts);
            List<T> rightList = new List<T>(rights);
            if (leftList.Count != rightList.Count)
            {
                throw new DataMismatchException(
                    Helper.NeutralFormat("The number of elements in two lists do not equal with each other."));
            }

            for (int i = 0; i < leftList.Count(); i++)
            {
                yield return new Pair<T, T>(leftList[i], rightList[i]);
            }
        }
        #endregion
    }

    /// <summary>
    /// Custom checker for decision trees used in HTS.
    /// </summary>
    public class HtsTreeChecker : IDecisionTreeChecker
    {
        /// <summary>
        /// Checks consistency of HTS emitting state index between actual and expected tree.
        /// </summary>
        /// <param name="expected">Expected tree.</param>
        /// <param name="actual">Actual tree.</param>
        public void Check(DecisionTree expected, DecisionTree actual)
        {
            if (expected.EmittingStateIndex() != actual.EmittingStateIndex())
            {
                throw new DataMismatchException(
                    Helper.NeutralFormat("Emitting state index in trees [{0}] do not equal with each other.", expected.Name));
            }
        }
    }

    /// <summary>
    /// Pair of data.
    /// </summary>
    /// <typeparam name="TLeft">Type left object.</typeparam>
    /// <typeparam name="TRight">Type right object.</typeparam>
    public class Pair<TLeft, TRight>
    {
        #region Fields
        private TLeft _left;
        private TRight _right;
        #endregion

        #region Construction
        /// <summary>
        /// Initializes a new instance of the Pair class.
        /// </summary>
        /// <param name="left">Value to assign to Left field.</param>
        /// <param name="right">Value to assign to Right field.</param>
        public Pair(TLeft left, TRight right)
        {
            _left = left;
            _right = right;
        }
        #endregion

        #region Properties

        /// <summary>
        /// Gets left item of the pair.
        /// </summary>
        public TLeft Left
        {
            get { return _left; }
        }

        /// <summary>
        /// Gets right item of the pair.
        /// </summary>
        public TRight Right
        {
            get { return _right; }
        }
        #endregion
    }

    /// <summary>
    /// Defines the class for data mismatch exceptions.
    /// </summary>
    [Serializable]
    public class DataMismatchException : GeneralException
    {
        #region Construction

        /// <summary>
        /// Initializes a new instance of the DataMismatchException class.
        /// </summary>
        /// <param name="msg">Message of the exception.</param>
        public DataMismatchException(string msg) :
            base(msg)
        {
        }

        /// <summary>
        /// Initializes a new instance of the DataMismatchException class.
        /// </summary>
        /// <param name="msg">Message of the exception.</param>
        /// <param name="e">Inner exception.</param>
        public DataMismatchException(string msg, Exception e) :
            base(msg, e)
        {
        }

        #endregion
    }
}