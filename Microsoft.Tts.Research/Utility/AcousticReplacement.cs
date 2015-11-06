//----------------------------------------------------------------------------
// <copyright file="AcousticReplacement.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      New Duration and F0 Mapping Algorithm
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Research
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// New Duration and F0 Mapping Algorithm.
    /// </summary>
    public class AcousticReplacement
    {
        /// <summary>
        /// Extropolation mode when a target point is beyond the head or tail of the source points.
        /// </summary>
        public enum F0ExtrapolationMode
        {
            /// <summary>
            /// Use the 2 ending points of the source to do linear extropolation.
            /// </summary>
            Linear = 0,

            /// <summary>
            /// Just copy the ending point of the source.
            /// </summary>
            Copy,
        }

        /// <summary>
        /// Duration mapping algorithm.
        /// </summary>
        /// <param name="sourceTotalDur">Given source duration.</param>
        /// <param name="targetDur">Target duration.</param>
        /// <param name="exceptionTimes">The count of zero duration.</param>
        /// <returns>True if successful, false if not.</returns>
        public static bool MapDuration(uint sourceTotalDur, ref uint[] targetDur, out uint exceptionTimes)
        {
            if (targetDur == null)
            {
                throw new ArgumentNullException("targetDur");
            }
            
            uint newDur = 0;
            uint targetTotalDur = 0;
            int durCount = targetDur.Length;
            uint[] tmpTargetDur = new uint[durCount];

            for (int i = 0; i < durCount; ++i)
            {
                tmpTargetDur[i] = targetDur[i];
                targetTotalDur += targetDur[i];
            }

            exceptionTimes = 0;
            for (int i = 0; i < durCount; ++i)
            {
                float ratio = sourceTotalDur / (float)targetTotalDur;
                float fd = targetDur[i] * ratio;
                uint allocate = (uint)Math.Round(fd);
                if (allocate == 0)
                {
                    ++exceptionTimes;
                    targetDur[i] = 1;
                }
                else
                {
                    targetDur[i] = allocate;
                }

                newDur += targetDur[i];
                targetTotalDur -= tmpTargetDur[i];
                sourceTotalDur -= allocate;
            }

            Debug.Assert(targetTotalDur == 0 && sourceTotalDur == 0);
            return true;
        }

        /// <summary>
        /// Mapping new target F0s.
        /// </summary>
        /// <param name="targetF0Count">Target F0 count.</param>
        /// <param name="sourceF0">Source F0s.</param>
        /// <param name="targetF0">Return new F0s.</param>
        /// <param name="extrapolationMode">Extropolation mode.</param>
        /// <returns>True if successful, false if not.</returns>
        public static bool MapF0(int targetF0Count, float[] sourceF0, out float[] targetF0,
            F0ExtrapolationMode extrapolationMode)
        {
            if (targetF0Count <= 0)
            {
                throw new ArgumentOutOfRangeException("targetF0Count");
            }

            if (sourceF0 == null)
            {
                throw new ArgumentNullException("sourceF0");
            }
            
            targetF0 = new float[targetF0Count];

            if (sourceF0.Length == 1)
            {
                for (int i = 0; i < targetF0Count; i++)
                {
                    targetF0[i] = sourceF0[0];
                }

                return true;
            }

            double between = (double)sourceF0.Length / targetF0Count;

            for (int i = 0; i < targetF0Count; i++)
            { 
                double pos = ((i + 0.5) * between) - 0.5;
                int pos1 = (int)Math.Floor(pos);
                int pos2 = (int)Math.Ceiling(pos);

                if (pos1 == -1)
                {
                    pos1++;
                    if (extrapolationMode == F0ExtrapolationMode.Linear)
                    {
                        pos2++;
                    }
                }
                else if (pos2 == sourceF0.Length)
                {
                    pos2--;
                    if (extrapolationMode == F0ExtrapolationMode.Linear)
                    {
                        pos1--;
                    }
                }

                if (pos1 == pos2)
                {
                    targetF0[i] = sourceF0[pos1];
                }
                else
                {
                    targetF0[i] = (float)(sourceF0[pos1] + ((sourceF0[pos2] - sourceF0[pos1]) /
                        (double)(pos2 - pos1) * (pos - pos1)));
                }
            }
            
            return true;
        }
    }
}