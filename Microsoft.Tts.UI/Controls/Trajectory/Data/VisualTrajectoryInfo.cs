//----------------------------------------------------------------------------
// <copyright file="VisualTrajectoryInfo.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      class of VisualTrajectoryInfo
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.UI.Controls.Trajectory.Data
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using Microsoft.Tts.UI.Controls.Data;

    /// <summary>
    /// Class of VisualTrajectoryInfo.
    /// </summary>
    public class VisualTrajectoryInfo : ViewDataBase
    {
        #region fields

        /// <summary>
        /// Word text.
        /// </summary>
        private string _word = string.Empty;

        /// <summary>
        /// Phone text.
        /// </summary>
        private string _phone = string.Empty;

        /// <summary>
        /// Time spot.
        /// </summary>
        private double _time = 0.0;

        /// <summary>
        /// Generated parameter value.
        /// </summary>
        private double _generatedParameter = 0.0;

        /// <summary>
        /// Candidates parameter.
        /// </summary>
        private double _candidatesParameter = 0.0;

        /// <summary>
        /// Whether show candidates parameter.
        /// </summary>
        private bool _showCandidatesParameter = false;

        /// <summary>
        /// Mean value.
        /// </summary>
        private double _mean = 0.0;

        /// <summary>
        /// Standard deviation value.
        /// </summary>
        private double _standardDeviation = 0.0;

        /// <summary>
        /// Frame index;.
        /// </summary>
        private int _frameIndex = 0;

        #endregion

        #region properties

        /// <summary>
        /// Gets or sets word text.
        /// </summary>
        public string Word
        {
            get
            {
                return _word;
            }

            set 
            {
                _word = value;
                NotifyPropertyChanged("Word");
            }
        }

        /// <summary>
        /// Gets or sets phone text.
        /// </summary>
        public string Phone
        {
            get 
            {
                return _phone; 
            }

            set 
            {
                _phone = value;
                NotifyPropertyChanged("Phone");
            }
        }

        /// <summary>
        /// Gets or sets time spot in seconds.
        /// </summary>
        public double Time
        {
            get
            {
                return _time;
            }

            set
            {
                _time = value;
                NotifyPropertyChanged("Time");
            }
        }

        /// <summary>
        /// Gets or sets generated parameter value.
        /// </summary>
        public double GeneratedParameter
        {
            get 
            {
                return _generatedParameter; 
            }

            set 
            {
                _generatedParameter = value;
                NotifyPropertyChanged("GeneratedParameter");
            }
        }

        /// <summary>
        /// Gets or sets candidates parameter.
        /// </summary>
        public double CandidatesParameter
        {
            get 
            {
                return _candidatesParameter; 
            }

            set 
            {
                _candidatesParameter = value;
                NotifyPropertyChanged("CandidatesParameter");
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether show candidates parameter.
        /// </summary>
        public bool ShowCandidatesParameter
        {
            get 
            {
                return _showCandidatesParameter; 
            }

            set 
            {
                _showCandidatesParameter = value;
                NotifyPropertyChanged("ShowCandidatesParameter");
            }
        }

        /// <summary>
        /// Gets or sets mean value.
        /// </summary>
        public double Mean
        {
            get
            {
                return _mean;
            }

            set
            {
                _mean = value;
                NotifyPropertyChanged("Mean");
            }
        }

        /// <summary>
        /// Gets or sets deviation value.
        /// </summary>
        public double StandardDeviation
        {
            get
            {
                return _standardDeviation;
            }

            set
            {
                _standardDeviation = value;
                NotifyPropertyChanged("StandardDeviation");
            }
        }

        /// <summary>
        /// Gets or sets frame index.
        /// </summary>
        public int FrameIndex
        {
            get
            {
                return _frameIndex;
            }

            set
            {
                _frameIndex = value;
                NotifyPropertyChanged("FrameIndex");
            }
        }

        #endregion
    }
}