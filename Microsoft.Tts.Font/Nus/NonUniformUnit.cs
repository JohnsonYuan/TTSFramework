//----------------------------------------------------------------------------
// <copyright file="NonUniformUnit.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     Defines object model for NUS voice data building.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Font
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.Tts.Offline.Htk;
    using Microsoft.Tts.Offline.Utility;
    using Microsoft.Tts.ServiceProvider;
    using Microsoft.Tts.ServiceProvider.Extension;

    /// <summary>
    /// This mirrors runtime FeatureValue type and each field is writable.
    /// </summary>
    public class FeatureVal
    {
        public FeatureValueType ValueType { get; set; }

        public int IntValue { get; set; }

        public string StringValue { get; set; }
    }

    /// <summary>
    /// Phone plus feature for NUS detection.
    /// </summary>
    public class FeaturePhone : IEqualityComparer<FeatureVal>
    {
        public ushort PhoneId { get; set; }

        public FeatureVal[] Features { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            var p = obj as FeaturePhone;

            if (p == null)
            {
                return false;
            }

            return p.PhoneId == PhoneId &&
                Enumerable.SequenceEqual(p.Features, Features, this);
        }

        public override int GetHashCode()
        {
            return PhoneId;
        }

        #region IEqualityComparer<FeatureVal> Members

        public bool Equals(FeatureVal x, FeatureVal y)
        {
            bool ret;

            if (x != null && y != null)
            {
                ret = x.ValueType == y.ValueType &&
                    x.IntValue == y.IntValue && x.StringValue == y.StringValue;
            }
            else if (x == null && y == null)
            {
                ret = true;
            }
            else
            {
                ret = false;
            }

            return ret;
        }

        public int GetHashCode(FeatureVal obj)
        {
            int ret = obj.IntValue;

            if (!string.IsNullOrEmpty(obj.StringValue))
            {
                ret += obj.StringValue.GetHashCode();
            }

            return ret;
        }

        #endregion
    }

    /// <summary>
    /// Nuu prosody item.
    /// </summary>
    public class NUUProsodyItem
    {
        /// <summary>
        /// Default break level of NUU is BK_IDX_WORD(2).
        /// </summary>
        public static readonly byte DefaultBr = 2;

        /// <summary>
        /// Default tobi of NUU is K_NOBND(0).
        /// </summary>
        public static readonly byte DefaultTobi = 0;

        public NUUProsodyItem()
        {
            HeadBr = DefaultBr;
            TailBr = DefaultBr;
            HeadTobi = DefaultTobi;
            TailTobi = DefaultTobi;
        }

        /// <summary>
        /// Gets or sets head break level of NUU.
        /// </summary>
        public byte HeadBr { get; set; }

        /// <summary>
        /// Gets or sets tail break level of NUU.
        /// </summary>
        public byte TailBr { get; set; }

        /// <summary>
        /// Gets or sets head tobi of NUU.
        /// </summary>
        public byte HeadTobi { get; set; }

        /// <summary>
        /// Gets or sets tail tobi of NUU.
        /// </summary>
        public byte TailTobi { get; set; }

        public override bool Equals(object obj)
        {
            NUUProsodyItem item = obj as NUUProsodyItem;
            bool isEqual = false;
            if (HeadBr == item.HeadBr && TailBr == item.TailBr && HeadTobi == item.HeadTobi && TailTobi == item.TailTobi)
            {
                isEqual = true;
            }

            return isEqual;
        }

        public override int GetHashCode()
        {
            byte[] tempArray = new byte[] { HeadBr, TailBr, HeadTobi, TailTobi };
            return ((IStructuralEquatable)tempArray).GetHashCode(EqualityComparer<object>.Default);
        }
    }

    /// <summary>
    /// A sequence of feature phone.
    /// </summary>
    public class NonUniformUnit
    {
        // If template unit
        public bool IsTemplate { get; set; }

        // unit id
        public int Id { get; set; }

        // reference unit information in template unit
        public int[] Segment { get; set; }

        public FeaturePhone[] FeaturePhones { get; set; }

        // Header, includes feature precision(float point/fixed point), Lsp Order, f0 Order, Gain Order
        // a row corresponds to UNT indexes for a NUU
        public int[][] CandidateUntIndexes { get; set; }

        // pitch target for this non uniform unit.
        // the length equals to column number of CandidateUntIndexes.
        public float[] PitchTarget { get; set; }

        // Acoustic trajectory feature for SPE
        // F0 and gain is only one order, no more explanation
        // LSP has multiple order and we can still recognize it as one order, the order will be given by LSPOrder
        public float[] F0TrajectoryFeature { get; set; }

        public float[] LspTrajectoryFeature { get; set; }

        public float[] GainTrajectoryFeature { get; set; }

        public uint[] DurationOfPhone { get; set; }

        // Emotion.
        public uint Emotion { get; set; }

        // All Prosody Items for candidates
        public NUUProsodyItem[] CandidateProsodyItems { get; set; }

        // Best prosody item for candidates
        public NUUProsodyItem BestProsodyItem { get; set; }

        /// <summary>
        /// Add a Candidate Prosody Item.
        /// </summary>
        /// <param name="nuuProsodyItem">Nuu candidate prosody item.</param>
        public void AddCandidateProsodyItem(NUUProsodyItem nuuProsodyItem)
        {
            List<NUUProsodyItem> tempList = new List<NUUProsodyItem>();
            if (CandidateProsodyItems != null)
            {
                tempList = CandidateProsodyItems.ToList();
            }

            tempList.Add(nuuProsodyItem);
            CandidateProsodyItems = tempList.ToArray();
        }

        public void CalculateBestProsodyItem()
        {
            if (CandidateProsodyItems == null || CandidateProsodyItems.Count() <= 0)
            {
                throw new InvalidDataException("Invalid CandidateProsodyItems value, value should not be null or should contain at least one item!");
            }

            Dictionary<NUUProsodyItem, int> prosodyDict = new Dictionary<NUUProsodyItem, int>();
            int maxCount = 0;
            BestProsodyItem = CandidateProsodyItems[0];
            foreach (NUUProsodyItem candidateProsodyItem in CandidateProsodyItems)
            {
                if (!prosodyDict.ContainsKey(candidateProsodyItem))
                {
                    prosodyDict.Add(candidateProsodyItem, 1);
                }
                else
                {
                    prosodyDict[candidateProsodyItem] = prosodyDict[candidateProsodyItem] + 1;
                    if (prosodyDict[candidateProsodyItem] > maxCount)
                    {
                        maxCount = prosodyDict[candidateProsodyItem];
                        BestProsodyItem = candidateProsodyItem;
                    }
                }
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            var p = obj as NonUniformUnit;

            if (p == null)
            {
                return false;
            }

            return Enumerable.SequenceEqual(p.FeaturePhones, FeaturePhones) && (p.Emotion == Emotion);
        }

        public override int GetHashCode()
        {
            return FeaturePhones.Select(p => (int)p.PhoneId).ToArray().GetHash();
        }
    }

    /// <summary>
    /// Segment with acoustic feature/distance for NUS prune.
    /// </summary>
    public class SegmentPruneInfo
    {
        public SentenceSegment Segment { get; set; }

        public HtkFrameStream[][] PhoneFrames { get; set; }

        public AcousticDistance Distance { get; set; }
    }

    /// <summary>
    /// Phone segments in sentence.
    /// </summary>
    public class SentenceSegment
    {
        public string SentenceId { get; set; }

        public ItemRange PhoneRange { get; set; }

        /// <summary>
        /// Gets or sets feature frame for phone range.
        /// </summary>
        public HtkFrameStream[] Frames { get; set; }
    }

    /// <summary>
    /// NUS unit and its candidate.
    /// </summary>
    public class NonUniformUnitInfo
    {
        public NonUniformUnit Unit { get; set; }

        public SentenceSegment[] Segments { get; set; }

        public bool IsTemplate { get; set; }
    }

    /// <summary>
    /// Acoustic distance accumulator.
    /// </summary>
    public class AcousticDistance
    {
        public double F0 { get; set; }

        public double Power { get; set; }

        public double Lpcc { get; set; }
    }
}