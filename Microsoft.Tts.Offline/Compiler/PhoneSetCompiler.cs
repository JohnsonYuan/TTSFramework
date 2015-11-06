//----------------------------------------------------------------------------
// <copyright file="PhoneSetCompiler.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements Phone Set Compiler
// </summary>
//----------------------------------------------------------------------------
namespace Microsoft.Tts.Offline.Compiler
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Phone Data Structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 4)]
    internal struct PhoneData
    {
        private ushort _id;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
        private string _name;
        private uint _duration;
        private uint _feature;

        /// <summary>
        /// Initializes a new instance of the <see cref="PhoneData"/> struct.
        /// </summary>
        /// <param name="name">Phone name.</param>
        /// <param name="id">Phone id.</param>
        /// <param name="feature">Phone feature.</param>
        public PhoneData(string name, ushort id, uint feature)
        {
            _name = name.ToUpperInvariant();
            _id = id;
            _duration = 0;
            _feature = feature;
        }
    }

    /// <summary>
    /// Phone Set Compiler.
    /// </summary>
    public class PhoneSetCompiler
    {
        /// <summary>
        /// Prevents a default instance of the <see cref="PhoneSetCompiler"/> class from being created.
        /// </summary>
        private PhoneSetCompiler()
        {
        }

        /// <summary>
        /// Compiler.
        /// </summary>
        /// <param name="phoneSet">Phone Set.</param>
        /// <param name="outputStream">OutputStream.</param>
        /// <returns>ErrorSet.</returns>
        public static ErrorSet Compile(TtsPhoneSet phoneSet, Stream outputStream)
        {
            if (phoneSet == null)
            {
                throw new ArgumentNullException("phoneSet");
            }

            if (outputStream == null)
            {
                throw new ArgumentNullException("outputStream");
            }

            phoneSet.Validate();
            ErrorSet errorSet = phoneSet.ErrorSet;
            if (!errorSet.Contains(ErrorSeverity.MustFix))
            {
                BinaryWriter bw = new BinaryWriter(outputStream);
                {
                    // write the size of the structure for verification purchase
                    bw.Write((uint)Marshal.SizeOf(typeof(PhoneData)));

                    bw.Write((uint)phoneSet.Phones.Count);

                    foreach (Phone phone in phoneSet.Phones)
                    {
                        // Skips those features whose id is greater than uint.MaxValue.
                        uint feature = (uint)phone.FeatureId;
                        PhoneData phoneData = new PhoneData(phone.CompilingName,
                            Convert.ToUInt16(phone.Id),
                            feature);
                        bw.Write(Helper.ToBytes(phoneData));
                    }
                }
            }

            return errorSet;
        }
    }
}