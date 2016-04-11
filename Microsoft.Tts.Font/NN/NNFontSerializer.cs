//----------------------------------------------------------------------------
// <copyright file="NNFontSerializer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements NN model compiling
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Font.NN
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.Htk;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Location for data in stream.
    /// </summary>
    public struct Location
    {
        public uint Offset;   // offset
        public uint Length;   // length

        /// <summary>
        /// Overwrites default Equals method.
        /// </summary>
        /// <param name="obj">The object to compare to.</param>
        /// <returns>True if equal; false otherwise.</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is Location))
            {
                return false;
            }

            Location other = (Location)obj;
            return Offset == other.Offset && Length == other.Length;
        }

        /// <summary>
        /// Overwrites default GetHashCode method.
        /// </summary>
        /// <returns>Hash code generated.</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    /// <summary>
    /// NN font serialization.
    /// </summary>
    public class NNFontSerializer
    {
        #region Fields

        #endregion

        #region Public operations

        /// <summary>
        /// Write out string pool with encrypt.
        /// </summary>
        /// <param name="stringPool">String pool.</param>
        /// <param name="writer">Binary data writer.</param>
        /// <returns>Size of bytes written out.</returns>
        public static uint Write(StringPool stringPool, DataWriter writer)
        {
            Helper.ThrowIfNull(stringPool);
            Helper.ThrowIfNull(writer);
            byte[] encryptedStringPool = new byte[stringPool.Length];
            Microsoft.Tts.ServiceProvider.HTSVoiceDataEncrypt.EncryptStringPool(
                stringPool.ToArray(), encryptedStringPool);
            return writer.Write(encryptedStringPool);
        }

        /// <summary>
        /// Writes font out into binary stream.
        /// </summary>
        /// <param name="font">NN font to write.</param>
        /// <param name="writer">Binary data.</param>
        /// <param name="language">Language.</param>
        /// <param name="schemaFile">Schema file.</param>
        /// <param name="outVarFile">Out var file.</param>
        /// <param name="phoneToIdIndexes">Phone id mapping.</param>
        /// <returns>Size of bytes written out.</returns>
        public uint Write(NNFont font, DataWriter writer, Language language, string schemaFile, string outVarFile, Dictionary<string, string> phoneToIdIndexes)
        {
            Helper.ThrowIfNull(font);
            Helper.ThrowIfNull(writer);
            Helper.ThrowIfNull(language);
            Helper.ThrowIfNull(phoneToIdIndexes);
            Helper.ThrowIfFileNotExist(schemaFile);
            Helper.ThrowIfFileNotExist(outVarFile);

            font.StringPool.Reset();

            // write header.
            uint size = WriteFontHeader(font, writer);

            // write feature and variance.
            font.Header.QuestionOffset = size;
            font.Header.QuestionSize = Write(writer, font.StringPool, language, schemaFile, outVarFile, phoneToIdIndexes);
            size += font.Header.QuestionSize;

            // write model.
            font.Header.ModelSetOffset = size;
            font.Header.ModelSetSize = WriteModels(font, writer);
            size += font.Header.ModelSetSize;

            // write string pool.
            font.Header.StringPoolOffset = size;
            font.Header.StringPoolSize = Write(font.StringPool, writer);
            size += (uint)font.Header.StringPoolSize;

            font.Header.CodebookOffset = 0;
            font.Header.CodebookSize = 0;

            font.Header.FontSize = size - font.Header.FontSizeOffset;
            using (PositionRecover recover = new PositionRecover(writer, 0))
            {
                font.Header.Write(writer);
            }

            return size;
        }

        /// <summary>
        /// Save feature set.
        /// </summary>
        /// <param name="writer">Binary data writer.</param>
        /// <param name="stringPool">String pool.</param>
        /// <param name="language">Language.</param>
        /// <param name="schemaFile">Schema file.</param>
        /// <param name="outVarFile">Out var file.</param>
        /// <param name="phoneToIdIndexes">Phone id mapping.</param>
        /// <returns>Size of bytes written out.</returns>
        public uint Write(DataWriter writer, StringPool stringPool,
            Language language, string schemaFile, string outVarFile, Dictionary<string, string> phoneToIdIndexes)
        {
            Helper.ThrowIfNull(writer);
            Helper.ThrowIfNull(language);
            Helper.ThrowIfNull(phoneToIdIndexes);
            Helper.ThrowIfFileNotExist(schemaFile);
            Helper.ThrowIfFileNotExist(outVarFile);

            uint size = 0;

            size += WriteSchema(language, schemaFile, phoneToIdIndexes, writer, stringPool);

            size += WriteOutVariance(outVarFile, writer);

            Debug.Assert(size % sizeof(uint) == 0, "Data must be 4-byte aligned.");

            return size;
        }

        /// <summary>
        /// Save schema (with feature set and it's value group, mean, variance).
        /// </summary>
        /// <param name="language">The language.</param>
        /// <param name="schemaFile">The schema File.</param>
        /// <param name="phoneToIdIndexes">Phone To Id Indexes.</param>
        /// <param name="writer">Writer.</param>
        /// <param name="stringPool">String pool.</param>
        /// <returns>Size of bytes written out.</returns>
        public uint WriteSchema(Language language, string schemaFile, Dictionary<string, string> phoneToIdIndexes, DataWriter writer, StringPool stringPool)
        {
            Helper.ThrowIfFileNotExist(schemaFile);
            Helper.ThrowIfNull(phoneToIdIndexes);
            Helper.ThrowIfNull(writer);
            Helper.ThrowIfNull(stringPool);
            Helper.ThrowIfNull(language);

            uint size = 0;

            LexicalAttributeSchema schema = new LexicalAttributeSchema(language);
            schema.Load(schemaFile);

            List<string> stateFeatureList = new List<string>();
            List<string> featureList = new List<string>();

            int stateFeatureCount = 0;

            for (int i = 0; i < schema.Categories.Count; i++)
            {
                string name = schema.Categories[i].Name.ToLower();

                if (name.IndexOf("state") >= 0)
                {
                    stateFeatureCount++;

                    if (!stateFeatureList.Contains(name))
                    {
                        stateFeatureList.Add(name);
                    }
                }

                if (!featureList.Contains(name))
                {
                    featureList.Add(name);
                }
            }

            // write state feature count.
            size += writer.Write((uint)stateFeatureList.Count);
            size += writer.Write((uint)stateFeatureCount);

            // write total feature count.
            size += writer.Write((uint)featureList.Count());

            Dictionary<string, uint> featureIndex = new Dictionary<string, uint>();

            uint index = 0;
            foreach (string feature in featureList)
            {
                size += writer.Write((uint)stringPool.Length);

                stringPool.PutString(feature);
                featureIndex.Add(feature, index++);
            }

            // write feature category
            size += writer.Write((uint)schema.Categories.Count);
            for (int i = 0; i < schema.Categories.Count; i++)
            {
                string featureName = schema.Categories[i].Name.ToLower();

                // feature index
                size += writer.Write((uint)featureIndex[featureName]);

                // mean
                size += writer.Write(schema.Categories[i].Mean);

                // invStdDev
                size += writer.Write(schema.Categories[i].InvStdDev);

                // value count
                size += writer.Write((uint)schema.Categories[i].Values.Count);
                for (int k = 0; k < schema.Categories[i].Values.Count; k++)
                {
                    string valueName = schema.Categories[i].Values[k].Name.ToLower();
                    string id = string.Empty;

                    if (phoneToIdIndexes.ContainsKey(valueName) && featureName.IndexOf("phoneidentity") >= 0)
                    {
                        id = phoneToIdIndexes[valueName];
                    }
                    else
                    {
                        id = valueName;
                    }

                    try
                    {
                        size += writer.Write(uint.Parse(id));
                    }
                    catch (System.FormatException)
                    {
                        continue;
                    }
                }
            }

            Debug.Assert(size % sizeof(uint) == 0, "Data must be 4-byte aligned.");

            return size;
        }

        /// <summary>
        /// Save acoustic feakture variacne.
        /// </summary>
        /// <param name="outVarFile">Acoustic variance file.</param>
        /// <param name="writer">Binary data writer.</param>
        /// <returns>Size of bytes written out.</returns>
        public uint WriteOutVariance(string outVarFile, DataWriter writer)
        {
            Helper.ThrowIfFileNotExist(outVarFile);
            Helper.ThrowIfNull(writer);

            uint size = 0;

            IEnumerable<string> allLines = Helper.AllFileLines(outVarFile);

            size += writer.Write((uint)allLines.Count());
            foreach (string line in allLines)
            {
                size += writer.Write(float.Parse(line));
            }

            Debug.Assert(size % sizeof(uint) == 0, "Data must be 4-byte aligned.");

            return size;
        }

        #endregion

        #region Protected operations

        /// <summary>
        /// Writes font header into binary stream.
        /// </summary>
        /// <param name="font">Font to write.</param>
        /// <param name="writer">Data binary writer.</param>
        /// <returns>Size of bytes written out.</returns>
        protected virtual uint WriteFontHeader(NNFont font, DataWriter writer)
        {
            Helper.ThrowIfNull(font);
            Helper.ThrowIfNull(writer);
            return font.Header.Write(writer);
        }

        #endregion

        #region Diagnostic assistant

        #endregion

        #region Private operations

        /// <summary>
        /// Write out locations.
        /// </summary>
        /// <param name="locations">Locations to write.</param>
        /// <param name="writer">Binary data writer.</param>
        /// <returns>Size of bytes written.</returns>
        private static uint Write(Location[] locations, DataWriter writer)
        {
            Helper.ThrowIfNull(locations);
            Helper.ThrowIfNull(writer);

            uint size = writer.Write(locations.Length);

            for (int i = 0; i < locations.Length; i++)
            {
                size += writer.Write(locations[i].Offset);
                size += writer.Write(locations[i].Length);
            }

            return size;
        }

        /// <summary>
        /// Writes font models out into binary stream.
        /// </summary>
        /// <param name="font">NN font to write.</param>
        /// <param name="writer">Binary data writer.</param>
        /// <returns>Size of bytes written out.</returns>
        private uint WriteModels(NNFont font, DataWriter writer)
        {
            Helper.ThrowIfNull(font);
            Helper.ThrowIfNull(writer);

            uint size = 0;

            // write positions.
            size += writer.Write(font.Models.Count);
            foreach (HmmModelType type in font.Models.Keys)
            {
                size += writer.Write((int)type);
                size += font.Models[type].Position.Save(writer);
            }

            // write windows.
            size += writer.Write(font.Models.Count - 1);
            foreach (HmmModelType type in font.Models.Keys)
            {
                if (type != HmmModelType.VoicedUnvoiced)
                {
                    size += writer.Write((int)type);
                    size += font.Models[type].WindowSet.Save(writer);
                }
            }

            return size;
        }

        #endregion
    }
}