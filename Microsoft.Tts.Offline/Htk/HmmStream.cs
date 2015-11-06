//----------------------------------------------------------------------------
// <copyright file="HmmStream.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements Dynamic Window Set
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Htk
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Distribution type.
    /// </summary>
    public enum ModelDistributionType
    {
        /// <summary>
        /// Not defined.
        /// </summary>
        NotDefined = 0,

        /// <summary>
        /// Continuous.
        /// </summary>
        Continuous,

        /// <summary>
        /// Multi-space distribution.
        /// </summary>
        Msd
    }

    /// <summary>
    /// Covariance type.
    /// </summary>
    public enum CovarianceType
    {
        /// <summary>
        /// Variance.
        /// </summary>
        Variance = 0,

        /// <summary>
        /// InvCovar.
        /// </summary>
        InvCovar = 1,

        /// <summary>
        /// LltCovar.
        /// </summary>
        LltCovar = 2
    }

    /// <summary>
    /// Gaussian distribution.
    /// </summary>
    public struct Gaussian
    {
        /// <summary>
        /// Weight.
        /// </summary>
        public float Weight;

        /// <summary>
        /// Global constant.
        /// </summary>
        public float GlobalConstant;

        /// <summary>
        /// Vector length.
        /// </summary>
        public int Length;

        /// <summary>
        /// Mean.
        /// </summary>
        public double[] Mean;

        /// <summary>
        /// Variance.
        /// </summary>
        public double[] Variance;

        /// <summary>
        /// Value to indicate whether values in Gaussian is fixed point.
        /// </summary>
        public bool IsFixedPoint;

        /// <summary>
        /// Initializes a new instance of the <see cref="Gaussian"/> struct.
        /// </summary>
        /// <param name="weight">Weight of current distribution.</param>
        /// <param name="length">Length of dimension of current distribution.</param>
        public Gaussian(float weight, int length)
        {
            Weight = weight;
            Length = length;
            Mean = new double[length];
            Variance = new double[length];
            GlobalConstant = 0.0f;
            IsFixedPoint = false;
        }
    }

    /// <summary>
    /// Function to address name schema of HTK format.
    /// </summary>
    public static class HmmStreamName
    {
        /// <summary>
        /// Stream index if it is not encoded in stream name.
        /// </summary>
        public const int NoStreamIndex = -1;

        /// <summary>
        /// Gets phone label by parsing the stream macro name.
        /// </summary>
        /// <param name="macro">Stream macro name.</param>
        /// <returns>Phone label.</returns>
        public static string ParsePhoneLabel(string macro)
        {
            string name = macro.Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries)[0];
            if (HmmNameEncoding.GetModelType(name) != HmmModelType.Invalid)
            {
                name = Phoneme.AnyPhone;
            }

            return name;
        }

        /// <summary>
        /// Parse model type from HMM state macro name in HMM file, for example: "r_logF0_s5_218-4"
        /// Here, we can not use underscore as delimiter, as phone may have understand in it as like zh-CN phone 97 set.
        /// </summary>
        /// <param name="streamMacroName">HMM stream macro name.</param>
        /// <returns>Model Type.</returns>
        public static HmmModelType ParseModelType(string streamMacroName)
        {
            HmmModelType modelType = HmmModelType.Invalid;
            foreach (object model in Enum.GetValues(typeof(HmmModelType)))
            {
                string acousticName = HmmNameEncoding.GetAcousticFeatureName((HmmModelType)model);

                if (string.IsNullOrEmpty(acousticName))
                {
                    continue;
                }

                if (streamMacroName.IndexOf("_" + acousticName + "_", StringComparison.Ordinal) > 0)
                {
                    modelType = (HmmModelType)model;
                    break;
                }

                // Phone independent model, in which the name starts directly with acoustic feature name
                if (streamMacroName.StartsWith(acousticName + "_", StringComparison.Ordinal))
                {
                    modelType = (HmmModelType)model;
                    break;
                }
            }

            return modelType;
        }

        /// <summary>
        /// Parse state index in the stream macro name.
        /// </summary>
        /// <param name="macro">Stream macro name.</param>
        /// <returns>State index.</returns>
        public static int ParseStateIndex(string macro)
        {
            int indexes = 0;
            Match match = Regex.Match(macro, @"_s(\d+)_");
            Debug.Assert(match.Success);

            if (match.Success)
            {
                indexes = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                Debug.Assert(indexes >= 2, "Support HTK state index in tree should start from 2.");
            }

            return indexes;
        }

        /// <summary>
        /// Parse stream index from HMM state macro name in HMM file,
        ///     for example: "r_logF0_s5_218-4", return 4
        ///     "r_lsp_s5_218", return 0.
        /// </summary>
        /// <param name="streamMacroName">HMM macro name.</param>
        /// <returns>Dynamic order.</returns>
        public static int ParseStreamIndex(string streamMacroName)
        {
            int index = NoStreamIndex;
            if (streamMacroName[streamMacroName.Length - 2] == '-')
            {
                index = int.Parse(streamMacroName.Substring(streamMacroName.Length - 1));
            }

            return index;
        }

        /// <summary>
        /// Parse macro name without stream index.
        /// </summary>
        /// <param name="streamMacroName">HMM macro name.</param>
        /// <returns>Macro name.</returns>
        public static string GetStreamIndexFreeName(string streamMacroName)
        {
            if (streamMacroName[streamMacroName.Length - 2] == '-')
            {
                return streamMacroName.Substring(0, streamMacroName.Length - 2);
            }

            return streamMacroName;
        }
    }

    /// <summary>
    /// HMM Stream with gaussian models.
    /// </summary>
    public class HmmStream
    {
        /// <summary>
        /// Gets or sets HTK macro name
        /// I.e. "f_dur_s2_5", "f_dur_s2_5-3".
        /// </summary>
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets Gaussian models of this stream, with mixture count.
        /// </summary>
        public Gaussian[] Gaussians
        {
            get;
            set;
        }

        /// <summary>
        /// Gets Phone label.
        /// </summary>
        public string PhoneLabel
        {
            get { return HmmStreamName.ParsePhoneLabel(Name); }
        }

        /// <summary>
        /// Gets Model type.
        /// </summary>
        public HmmModelType ModelType
        {
            get { return HmmStreamName.ParseModelType(Name); }
        }

        /// <summary>
        /// Gets State index.
        /// </summary>
        public int StateIndex
        {
            get { return HmmStreamName.ParseStateIndex(Name); }
        }

        /// <summary>
        /// Gets Stream index.
        /// </summary>
        public int StreamIndex
        {
            get { return HmmStreamName.ParseStreamIndex(Name); }
        }

        /// <summary>
        /// Gets or sets Position in binary data.
        /// </summary>
        public int Position
        {
            get;
            set;
        }

        /// <summary>
        /// Prune dimension in HMM Streams.
        /// </summary>
        /// <param name="streams">Streams to prune.</param>
        /// <param name="keepDimension">First dimensions to keep.</param>
        /// <returns>Pruned streams.</returns>
        public static IEnumerable<HmmStream> Prune(IEnumerable<HmmStream> streams, int keepDimension)
        {
            Helper.ThrowIfNull(streams);
            foreach (HmmStream stream in streams)
            {
                HmmStream prunedStream = Prune(stream, keepDimension);
                yield return prunedStream;
            }
        }

        /// <summary>
        /// Prune dimension in HMM Stream.
        /// </summary>
        /// <param name="stream">Stream to prune.</param>
        /// <param name="keepDimension">First dimensions to keep.</param>
        /// <returns>Pruned stream.</returns>
        private static HmmStream Prune(HmmStream stream, int keepDimension)
        {
            Helper.ThrowIfNull(stream);
            HmmStream prunedStream = new HmmStream();
            prunedStream.Name = stream.Name;
            prunedStream.Gaussians = new Gaussian[stream.Gaussians.Length];
            for (int i = 0; i < stream.Gaussians.Length; i++)
            {
                prunedStream.Gaussians[i].Weight = stream.Gaussians[i].Weight;
                prunedStream.Gaussians[i].Variance = stream.Gaussians[i].Variance;
                prunedStream.Gaussians[i].Mean = stream.Gaussians[i].Mean.Take(keepDimension).ToArray();
                prunedStream.Gaussians[i].Variance = stream.Gaussians[i].Variance.Take(keepDimension).ToArray();
                prunedStream.Gaussians[i].Length = Math.Min(keepDimension, stream.Gaussians[i].Length);
            }

            return prunedStream;
        }
    }

    /// <summary>
    /// HMM stream comparer.
    /// </summary>
    public class HmmStreamComparer
    {
        /// <summary>
        /// Tell whether two HMM streams equals with each other.
        /// </summary>
        /// <param name="left">Left stream object.</param>
        /// <param name="right">Right stream object.</param>
        /// <param name="comareData">Flag to indicate whether comparing data in Gaussian distribution.</param>
        /// <returns>True if equal, otherwise false.</returns>
        public static bool IsEqual(HmmStream left, HmmStream right, bool comareData)
        {
            Helper.ThrowIfNull(left);
            Helper.ThrowIfNull(right);
            return (left.Name == null || right.Name == null || IsNameEqual(left, right)) &&
                IsEqual(left.Gaussians, right.Gaussians, comareData);
        }

        /// <summary>
        /// Tell whether two stream macro name equal with each other.
        /// It will ignore the compare on intern leaf node index.
        /// </summary>
        /// <param name="left">Left.</param>
        /// <param name="right">Right.</param>
        /// <returns>True if yes, otherwise false.</returns>
        public static bool IsNameEqual(HmmStream left, HmmStream right)
        {
            Helper.ThrowIfNull(left);
            Helper.ThrowIfNull(right);
            return left.PhoneLabel == right.PhoneLabel &&
                left.ModelType == right.ModelType &&
                left.StateIndex == right.StateIndex &&
                left.StreamIndex == right.StreamIndex;
        }

        /// <summary>
        /// Test whether two Gaussian lists are equal.
        /// </summary>
        /// <param name="lefts">Left list.</param>
        /// <param name="rights">Right list.</param>
        /// <param name="comareData">To compare data.</param>
        /// <returns>True if yes, otherwise false.</returns>
        public static bool IsEqual(Gaussian[] lefts, Gaussian[] rights, bool comareData)
        {
            Helper.ThrowIfNull(lefts);
            Helper.ThrowIfNull(rights);
            List<Gaussian> leftList = new List<Gaussian>(lefts.Where(g => g.Length > 0));
            List<Gaussian> rightList = new List<Gaussian>(rights.Where(g => g.Length > 0));

            if (leftList.Count != rightList.Count)
            {
                return false;
            }

            for (int i = 0; i < leftList.Count; i++)
            {
                if (!IsEqual(leftList[i], rightList[i], comareData))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Tell whether two HMM Gaussian distributions equals with each other.
        /// </summary>
        /// <param name="left">Left Gaussian distribution.</param>
        /// <param name="right">Right Gaussian distribution.</param>
        /// <param name="comareData">Flag to indicate whether comparing data in Gaussian distribution.</param>
        /// <returns>True if equal, otherwise false.</returns>
        public static bool IsEqual(Gaussian left, Gaussian right, bool comareData)
        {
            Helper.ThrowIfNull(left);
            Helper.ThrowIfNull(right);
            return (float.IsNaN(left.Weight) || float.IsNaN(right.Weight) || left.Weight == right.Weight) &&
                left.Length == right.Length &&
                IsEqual(left.Mean, right.Mean, comareData) &&
                IsEqual(left.Variance, right.Variance, comareData);
        }

        /// <summary>
        /// Equal of two float lists.
        /// </summary>
        /// <param name="left">Left list.</param>
        /// <param name="right">Right list.</param>
        /// <param name="comareData">To compare data.</param>
        /// <returns>True if equal, otherwise false.</returns>
        private static bool IsEqual(double[] left, double[] right, bool comareData)
        {
            Helper.ThrowIfNull(left);
            Helper.ThrowIfNull(right);
            if (left.Length != right.Length)
            {
                return false;
            }

            if (comareData)
            {
                for (int i = 0; i < left.Length; i++)
                {
                    if (Math.Abs(left[i] - right[i]) > Math.Abs(right[i]) * 0.00001f &&
                        Math.Abs(left[i] - right[i]) > 0.00001f)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}