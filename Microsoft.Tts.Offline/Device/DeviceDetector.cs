//----------------------------------------------------------------------------
// <copyright file="DeviceDetector.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements device detector.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Device
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Device detector.
    /// </summary>
    public static class DeviceDetector
    {
        /// <summary>
        /// Device type.
        /// </summary>
        public enum DeviceType
        {
            /// <summary>
            /// Audio device type.
            /// </summary>
            Audio,
        }

        /// <summary>
        /// Check whether is installed.
        /// </summary>
        /// <param name="deviceType">Device type.</param>
        /// <returns>Whether is installed.</returns>
        public static bool IsInstalled(DeviceType deviceType)
        {
            bool isInstalled = false;
            switch (deviceType)
            {
                case DeviceType.Audio:
                    isInstalled = IsAudioInstalled();
                    break;
                default:
                    throw new NotSupportedException(Helper.NeutralFormat(
                        "Not supported device type [{0}]", deviceType.ToString()));
            }

            return isInstalled;
        }

        /// <summary>
        /// Check whether audio is installed.
        /// </summary>
        /// <returns>Whether audio is installed.</returns>
        private static bool IsAudioInstalled()
        {
            bool isInstalled = false;
            try
            {
                isInstalled = NativeMethods.WaveOutGetNumDevs() > 0 && NativeMethods.MidiOutGetNumDevs() > 0;
            }
            catch (Exception)
            {
                isInstalled = false;
            }

            return isInstalled;
        }
    }

    internal static class NativeMethods
    {
        /// <summary>
        /// Get wave out device number.
        /// </summary>
        /// <returns>Wave out device number.</returns>
        [DllImport("Winmm.dll", EntryPoint = "waveOutGetNumDevs", CharSet = CharSet.Auto)]
        internal static extern int WaveOutGetNumDevs();

        /// <summary>
        /// Get midi out device number.
        /// </summary>
        /// <returns>Midi out device number.</returns>
        [DllImport("Winmm.dll", EntryPoint = "midiOutGetNumDevs", CharSet = CharSet.Auto)]
        internal static extern int MidiOutGetNumDevs();
    }
}