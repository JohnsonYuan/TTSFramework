//----------------------------------------------------------------------------
// <copyright file="UnitCandidate.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module defines a common library to manipulate Htk training file.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Htk
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Xml.Serialization;
    using Microsoft.Tts.Offline.Utility;
    using Microsoft.Tts.Offline.Waveform;

    /// <summary>
    /// Unit Candidate Type. Currently, support phone and halfphone only. Could add incrementally.
    /// </summary>
    public enum UnitCandidateType
    {
        /// <summary>
        /// Phone.
        /// </summary>
        Phone,

        /// <summary>
        /// Half phone.
        /// </summary>
        Halfphone,
    }

    /// <summary>
    /// The candidate is the unit instance in each sentence, which represent a segmentation of waveform.
    /// </summary>
    [Serializable]
    public abstract class UnitCandidate
    {
        #region Fields

        /// <summary>
        /// The unique identifier for this candidate in this unit type. InvalidId means it not tagged id yet or it have no id.
        /// </summary>
        public const int InvalidId = -1;

        [NonSerialized]
        private Label _label;

        [NonSerialized]
        private Sentence _sentence;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Unit candidate type.
        /// </summary>
        public UnitCandidateType Type { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier for this candidate in this unit type.
        /// It is assigned to build voice font and may difference in each run time.
        /// After preselection tree is loaded, if the Id is negative value, it means this candidate is not used in the
        /// Voice font.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier for this candidate in overall voice font.
        /// It is assigned to build voice font and may difference in each run time.
        /// After preselection tree is loaded, if the Id is negative value, it means this candidate is not used in the
        /// Voice font.
        /// </summary>
        public int GlobalId { get; set; }

        /// <summary>
        /// Gets or sets the label of this Candidate.
        /// </summary>
        public Label Label 
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
        /// Gets or sets  the name of the candidate.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets  the left phoneme of this phoneme.
        /// </summary>
        public string LeftPhoneme { get; set; }

        /// <summary>
        /// Gets or sets  the right phoneme of this phoneme.
        /// </summary>
        public string RightPhoneme { get; set; }

        /// <summary>
        /// Gets or sets the sentence which this candidate belongs to.
        /// </summary>
        public Sentence Sentence 
        {
            get
            {
                return _sentence;
            }

            set
            {
                _sentence = value;
            }
        }

        /// <summary>
        /// Gets or sets the index of this candidate in the sentences.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Gets or sets the index of this candidate in those non-silence candidate of the sentences.
        /// </summary>
        public int IndexOfNonSilence { get; set; }

        /// <summary>
        /// Gets or sets the start time of this candidate, in 1.0e-7s.
        /// </summary>
        public long StartTime { get; set; }

        /// <summary>
        /// Gets or sets the end time of this candidate, in 1.0e-7s.
        /// </summary>
        public long EndTime { get; set; }

        /// <summary>
        /// Gets or sets the start time of this candidate, in second.
        /// </summary>
        public float StartTimeInSecond
        {
            get
            {
                return StartTime * Segment.HtkTimeUnit;
            }

            set
            {
                StartTime = (long)Math.Round(value / (Segment.HtkTimeUnit * 10000)) * 10000;
            }
        }

        /// <summary>
        /// Gets or sets the end time of this candidate, in second.
        /// </summary>
        public float EndTimeInSecond
        {
            get
            {
                return EndTime * Segment.HtkTimeUnit;
            }

            set
            {
                EndTime = (long)Math.Round(value / (Segment.HtkTimeUnit * 10000)) * 10000;
            }
        }

        /// <summary>
        /// Gets the start frame index of this phoneme.
        /// </summary>
        public int StartFrame
        {
            get
            {
                return (int)Math.Round(StartTimeInSecond * 1000 / Sentence.TrainingSet.MilliSecondsPerFrame);
            }
        }

        /// <summary>
        /// Gets the end frame index of this phoneme.
        /// </summary>
        public int EndFrame
        {
            get
            {
                return (int)Math.Round(EndTimeInSecond * 1000 / Sentence.TrainingSet.MilliSecondsPerFrame);
            }
        }

        /// <summary>
        /// Gets the full-context label.
        /// </summary>
        public string FullContextLabel
        {
            get { return Label.Text; }
        }

        /// <summary>
        /// Gets or sets the head margin for the candidate.
        /// </summary>
        public short[] HeadMargin { get; set; }

        /// <summary>
        /// Gets or sets the tail margin for the candidate.
        /// </summary>
        public short[] TailMargin { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the candidate holdon attribute.
        /// </summary>
        public bool MustHold { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the candidate is part of silence.
        /// </summary>
        public bool SilenceCandidate { get; set; }

        /// <summary>
        /// Gets a value indicating whether this unit candidate belongs to unit inventory.
        /// </summary>
        public bool ExistInUnitInventory
        {
            get
            {
                return Id != InvalidId;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Loads wave margin for the candidate.
        /// </summary>
        /// <param name="wave">WaveFile from which to load the data.</param>
        /// <param name="marginLength">Cross correlation margin length in millisecond.</param>
        public void LoadMargin(WaveFile wave, int marginLength)
        {
            if (Id != InvalidId)
            {
                double durationInSecond = marginLength * 0.001;
                if (StartTimeInSecond - durationInSecond < 0 ||
                    wave.Duration < StartTimeInSecond + durationInSecond ||
                    EndTimeInSecond - (durationInSecond / 2) < 0 ||
                    wave.Duration < EndTimeInSecond + (durationInSecond / 2))
                {
                    throw new InvalidDataException("Wave duration is shorter than expected duration of margin");
                }

                HeadMargin = wave.Cut(StartTimeInSecond - durationInSecond, durationInSecond * 2).DataIn16Bits;
                TailMargin = wave.Cut(EndTimeInSecond - (durationInSecond / 2), durationInSecond).DataIn16Bits;

                int headSampleCount = (int)(durationInSecond * 2 * wave.Format.SamplesPerSecond);
                if (headSampleCount != HeadMargin.Length || headSampleCount != TailMargin.Length * 2)
                {
                    throw new InvalidDataException("Margin data is not correctly generated");
                }
            }
        }

        /// <summary>
        /// Resets the candidate Id to negative value.
        /// </summary>
        public void ResetId()
        {
            Id = InvalidId;
            GlobalId = InvalidId;
        }

        /// <summary>
        /// Get focus area in decision tree.
        /// </summary>
        /// <param name="streamIdxOfGuidanceLsp">Stream index of guidance LSP.</param>///
        /// <returns>Cluster area.</returns>
        public abstract string GetClusterArea(int streamIdxOfGuidanceLsp);

        #endregion
    }

    /// <summary>
    /// Class to represent phone candidate, contain the specific data for phone candidate.
    /// </summary>
    public class PhoneCandidate : UnitCandidate
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PhoneCandidate"/> class.
        /// </summary>
        /// <param name="phoneme">Phone segment link.</param>
        public PhoneCandidate(PhoneSegment phoneme)
        {
            Type = UnitCandidateType.Phone;
            Id = InvalidId;
            Label = phoneme.Label;
            Name = phoneme.Name;
            LeftPhoneme = phoneme.LeftPhoneme;
            RightPhoneme = phoneme.RightPhoneme;
            StartTime = phoneme.StartTime;
            EndTime = phoneme.EndTime;
            Index = phoneme.Index;
            IndexOfNonSilence = phoneme.IndexOfNonSilence;
            MustHold = false;
            PhoneSegmentLink = phoneme;
            Sentence = phoneme.Sentence;
            SilenceCandidate = phoneme.Name == Phoneme.ToHtk(Phoneme.SilencePhone);
        }

        /// <summary>
        /// Gets or sets Link to corresponding phone segment.
        /// </summary>
        public PhoneSegment PhoneSegmentLink { get; set; }

        /// <summary>
        /// Get cluster area, for phone, the cluster area is whole states.
        /// Generally, only Lsp and LogF0 are used for clustering. If guidanceLsp is modeled, Lsp is replaced by guidanceLsp.
        /// </summary>
        /// <param name="streamIdxOfGuidanceLsp">Stream index of guidance LSP.</param>///
        /// <returns>Cluster area.</returns>
        public override string GetClusterArea(int streamIdxOfGuidanceLsp)
        {
            string area = Helper.NeutralFormat("{{*-{0}+*", PhoneSegmentLink.Name);
            area += ".state[2, 6]";

            // stream info: Lsp 0, LogF0 1-3
            area += streamIdxOfGuidanceLsp > 4 ? Helper.NeutralFormat(".stream[2-4,{0}]}}", streamIdxOfGuidanceLsp) : ".stream[1-4]}";
            return area;
        }
    }

    /// <summary>
    /// Class to represent half phone candidate, contain the specific data for half phone.
    /// </summary>
    [Serializable]
    public class HalfPhoneCandidate : UnitCandidate
    {
        [NonSerialized]
        private PhoneSegment _phoneSegmentLink;

        /// <summary>
        /// Initializes a new instance of the <see cref="HalfPhoneCandidate"/> class.
        /// </summary>
        /// <param name="phoneme">Phone segment link.</param>
        /// <param name="isLeftHalfPhone">Flag to left or right half phone.</param>
        public HalfPhoneCandidate(PhoneSegment phoneme, bool isLeftHalfPhone)
        {
            Type = UnitCandidateType.Halfphone;
            Id = InvalidId;
            Label = phoneme.Label;

            // bug #93090, TBD: Offline should call serviceProvider to get half phone name and boundary
            IsLeftHalfPhone = isLeftHalfPhone;
            Name = isLeftHalfPhone ? "hpl_" + phoneme.Name : "hpr_" + phoneme.Name;
            int middleTime = (int)(((phoneme.StateAlignments[2].StartTime + phoneme.StateAlignments[2].EndTime) / 2 / 50000.0) + 0.6) * 50000;
            StartTime = isLeftHalfPhone ? phoneme.StartTime : middleTime;
            EndTime = isLeftHalfPhone ? middleTime : phoneme.EndTime;

            LeftPhoneme = phoneme.LeftPhoneme;
            RightPhoneme = phoneme.RightPhoneme;
            Index = isLeftHalfPhone ? phoneme.Index * 2 : (phoneme.Index * 2) + 1;
            IndexOfNonSilence = isLeftHalfPhone ? phoneme.IndexOfNonSilence * 2 : (phoneme.IndexOfNonSilence * 2) + 1;
            MustHold = false;
            PhoneSegmentLink = phoneme;
            Sentence = phoneme.Sentence;
            SilenceCandidate = phoneme.Name == Phoneme.ToHtk(Phoneme.SilencePhone);
        }

        /// <summary>
        /// Gets or sets Link to corresponding phone segment.
        /// </summary>
        public PhoneSegment PhoneSegmentLink 
        { 
            get 
            {
                return _phoneSegmentLink;
            }

            set
            {
                _phoneSegmentLink = value;
            } 
        }

        /// <summary>
        /// Gets or sets a value indicating whether Flag for left half phone or right half phone.
        /// </summary>
        public bool IsLeftHalfPhone { get; set; }

        /// <summary>
        /// Get cluster area, for half phone, the cluster area is first 2 states for left half phone, and last 2 states for right half phone.
        /// Generally, only Lsp and LogF0 are used for clustering. If guidanceLsp is modeled, Lsp is replaced by guidanceLsp.
        /// </summary>
        /// <param name="streamIdxOfGuidanceLsp">Stream index of guidance LSP.</param>///
        /// <returns>Cluster area.</returns>
        public override string GetClusterArea(int streamIdxOfGuidanceLsp)
        {
            string area = Helper.NeutralFormat("{{*-{0}+*", PhoneSegmentLink.Name);
            area += IsLeftHalfPhone ? ".state[2, 3]" : ".state[5, 6]";

            // stream info: Lsp 0, LogF0 1-3
            area += streamIdxOfGuidanceLsp > 4 ? Helper.NeutralFormat(".stream[2-4,{0}]}}", streamIdxOfGuidanceLsp) : ".stream[1-4]}";
            return area;
        }
    }
}