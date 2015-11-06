//----------------------------------------------------------------------------
// <copyright file="PosSetCompiler.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements Pos Set Compiler
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Compiler
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Pos Data Structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 4)]
    internal struct PosData
    {
        public uint Id;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string Name;

        /// <summary>
        /// Initializes a new instance of the <see cref="PosData"/> struct.
        /// </summary>
        /// <param name="name">POS name.</param>
        /// <param name="id">POS id.</param>
        public PosData(string name, uint id)
        {
            Name = name;
            Id = id;
        }
    }

    /// <summary>
    /// POS set compiler.
    /// </summary>
    public class PosSetCompiler
    {
        /// <summary>
        /// Prevents a default instance of the <see cref="PosSetCompiler"/> class from being created.
        /// </summary>
        private PosSetCompiler()
        {
        }

        /// <summary>
        /// Compiler.
        /// </summary>
        /// <param name="posSet">POS set.</param>
        /// <param name="outputStream">Output Stream.</param>
        /// <returns>ErrorSet.</returns>
        public static ErrorSet Compile(TtsPosSet posSet, Stream outputStream)
        {
            if (posSet == null)
            {
                throw new ArgumentNullException("posSet");
            }

            if (outputStream == null)
            {
                throw new ArgumentNullException("outputStream");
            }

            posSet.Validate();
            ErrorSet errorSet = posSet.ErrorSet;
            if (!errorSet.Contains(ErrorSeverity.MustFix))
            {
                SortedDictionary<uint, string> sortedPosSet = new SortedDictionary<uint, string>();
                foreach (KeyValuePair<uint, string> pair in posSet.IdItems)
                {
                    sortedPosSet.Add(pair.Key, pair.Value);
                }

                BinaryWriter bw = new BinaryWriter(outputStream);
                {
                    // write the size of the structure for verification purchase
                    bw.Write(((uint)Marshal.SizeOf(typeof(PosData))));

                    // write the phoneme count
                    bw.Write((uint)posSet.IdItems.Count);

                    // write phonemes array
                    foreach (KeyValuePair<uint, string> pair in sortedPosSet)
                    {
                        PosData posData = new PosData(pair.Value, pair.Key);
                        bw.Write(Helper.ToBytes(posData));
                    }
                }
            }

            return errorSet;
        }

        /// <summary>
        /// Compile the POS set into PosTaggerPos binary.
        /// </summary>
        /// <param name="posSet">POS set.</param>
        /// <param name="outputStream">Output stream.</param>
        /// <returns>Error set.</returns>
        public static ErrorSet CompilePosTaggerPos(TtsPosSet posSet, Stream outputStream)
        {
            if (posSet == null)
            {
                throw new ArgumentNullException("posSet");
            }

            if (outputStream == null)
            {
                throw new ArgumentNullException("outputStream");
            }

            posSet.Validate();
            ErrorSet errorSet = posSet.ErrorSet;
            if (!errorSet.Contains(ErrorSeverity.MustFix))
            {
                BinaryWriter bw = new BinaryWriter(outputStream);
                if (bw != null)
                {
                    bw.Write(Convert.ToUInt32(posSet.Items.Count));
                    foreach (KeyValuePair<string, uint> pair in posSet.Items)
                    {
                        // Warning: do not check the value whether larger than maximal of 16bit.
                        Debug.Assert(pair.Value <= 0xFFFE);
                        bw.Write(Convert.ToUInt16(pair.Value));
                    }
                }
            }

            return errorSet;
        }
    }
}