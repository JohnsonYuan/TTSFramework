//----------------------------------------------------------------------------
// <copyright file="IBinarySerializer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module defines a common library to binary serializer
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Font
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Binary serializer interface.
    /// </summary>
    /// <typeparam name="T">Type of data to support binary serializer.</typeparam>
    public interface IBinarySerializer<T>
    {
        /// <summary>
        /// Save instance into data writer.
        /// </summary>
        /// <param name="writer">Target data writer to serialize.</param>
        /// <returns>Number of bytes written out.</returns>
        uint Save(DataWriter writer);

        /// <summary>
        /// Load instance from binary writer.
        /// </summary>
        /// <param name="reader">Source data from binary reader.</param>
        /// <returns>Retrieved instance.</returns>
        T Load(BinaryReader reader);
    }

    /// <summary>
    /// Extension functions to binary serializer.
    /// </summary>
    public static class BinarySerializerExtension
    {
        /// <summary>
        /// Converts a binary serializer supported object to byte sequence.
        /// </summary>
        /// <typeparam name="T">Type of binary serializer support.</typeparam>
        /// <param name="serializer">Serializer.</param>
        /// <returns>Byte sequence.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Ignore.")]
        public static byte[] ToArray<T>(this IBinarySerializer<T> serializer)
        {
            Helper.ThrowIfNull(serializer);
            MemoryStream stream = new MemoryStream();  
            serializer.Save(new DataWriter(stream));
            return stream.ToArray();            
        }
    }
}