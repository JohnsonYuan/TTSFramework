//----------------------------------------------------------------------------
// <copyright file="FontComparer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements font compare functions ignoring the build number.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Utility
{
    using System;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// The FontComparer class.
    /// </summary>
    public class FontComparer
    {
        /// <summary>
        /// The BuildNumber offset in voice font.
        /// </summary>
        public const int BuildNumberOffset = 0x1C;

        /// <summary>
        /// The BuildNumber size in voice font.
        /// </summary>
        public const int BuildNumberSize = sizeof(uint);

        /// <summary>
        /// The BuildNumber size for non SPS file in voice font.
        /// </summary>
        private const int TtsBuildNumberOffset = 0x18;

        /// <summary>
        /// Compares two voice font as binary mode, ignored the build number.
        /// </summary>
        /// <param name="leftFile">Path of left file to compare.</param>
        /// <param name="rightFile">Path of right file to compare.</param>
        /// <returns>True if equal, otherwise false.</returns>
        public static bool CompareVoiceFont(string leftFile, string rightFile)
        {
            var apmTag = BitConverter.ToInt32("APM ".Select(c => (byte)c).ToArray(), 0);
            var atmTag = BitConverter.ToInt32("ATM ".Select(c => (byte)c).ToArray(), 0);

            int buildNumberOffset = BuildNumberOffset;

            FileStream file = new FileStream(leftFile, FileMode.Open);
            try
            {
                using (BinaryReader reader = new BinaryReader(file))
                {
                    file = null;

                    try
                    {
                        int tag = reader.ReadInt32();

                        if (tag != apmTag && tag != atmTag)
                        {
                            buildNumberOffset = TtsBuildNumberOffset;
                        }
                    }
                    catch (EndOfStreamException)
                    {
                    }
                }

                using (FileStream leftStream =
                    new FileStream(leftFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (FileStream rightStream =
                    new FileStream(rightFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (leftStream.Length != rightStream.Length)
                    {
                        return false;
                    }

                    int bufferLen = 4 * 1024; // 4k
                    byte[] bufLeft = new byte[bufferLen];
                    byte[] bufRight = new byte[bufferLen];

                    int lenLeft, lenRight;
                    for (int offset = 0; offset < leftStream.Length; offset += lenLeft)
                    {
                        lenLeft = leftStream.Read(bufLeft, 0, bufferLen);
                        lenRight = rightStream.Read(bufRight, 0, bufferLen);

                        if (lenLeft != lenRight)
                        {
                            return false;
                        }

                        // Check whether the build number is covered or not.
                        if ((offset < buildNumberOffset && offset + lenLeft > buildNumberOffset)
                            || (offset > buildNumberOffset && offset < buildNumberOffset + BuildNumberSize))
                        {
                            // Clear the covered build number.
                            int start = Math.Max(offset, buildNumberOffset) - offset;
                            int length = Math.Min(BuildNumberSize, lenLeft - start);
                            for (int i = 0; i < length; ++i)
                            {
                                bufLeft[start + i] = 0;
                                bufRight[start + i] = 0;
                            }
                        }

                        for (int i = 0; i < lenLeft; ++i)
                        {
                            if (bufLeft[i] != bufRight[i])
                            {
                                return false;
                            }
                        }
                    }
                }

                return true;
            }
            finally
            {
                if (null != file)
                {
                    file.Dispose();
                }
            }
        }
    }
}