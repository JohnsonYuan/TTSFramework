//----------------------------------------------------------------------------
// <copyright file="NNDynamicWindowSet.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements NN Dynamic Window Set
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Font.NN
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Htk;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// NN Dynamic windows for dynamic feature calculation.
    /// </summary>
    public class NNDynamicWindowSet : IBinarySerializer<NNDynamicWindowSet>
    {
        #region Public const fields

        /// <summary>
        /// Default LSP dynamic windows.
        /// </summary>
        public static NNDynamicWindowSet LspDefaultWindowSet = new NNDynamicWindowSet
        {
            _data = new float[][]
            {
                new float[] { 1.0f,     float.NaN,  float.NaN },
                new float[] { -0.5f,    0f,         0.5f },
                new float[] { 1f,       -2f,        1f }
            }
        };

        /// <summary>
        /// Default LogF0 dynamic windows.
        /// </summary>
        public static NNDynamicWindowSet LogF0DefaultWindowSet = new NNDynamicWindowSet
        {
            _data = new float[][]
            {
                new float[] { 1.0f, float.NaN, float.NaN },
                new float[] { -0.5f, 0f, 0.5f },
                new float[] { 1f, -2f, 1f }
            }
        };

        /// <summary>
        /// Default multi-band excitation dynamic windows.
        /// </summary>
        public static NNDynamicWindowSet MbeDefaultWindowSet = new NNDynamicWindowSet
        {
            _data = new float[][]
            {
                new float[] { 1.0f, float.NaN, float.NaN },
                new float[] { -0.5f, 0f, 0.5f },
                new float[] { 1f, -2f, 1f }
            }
        };

        /// <summary>
        /// Default power dynamic windows.
        /// </summary>
        public static NNDynamicWindowSet PowerDefaultWindowSet = new NNDynamicWindowSet
        {
            _data = new float[][]
            {
                new float[] { 1.0f, float.NaN, float.NaN },
                new float[] { -0.5f, 0f, 0.5f },
                new float[] { 1f, -2f, 1f }
            }
        };

        /// <summary>
        /// Default Duration dynamic windows.
        /// </summary>
        public static NNDynamicWindowSet DurationDefaultWindowSet = new NNDynamicWindowSet
        {
            _data = new float[][]
            {
                new float[] { float.NaN },
            }
        };

        #endregion

        #region Private fields

        private float[][] _data;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the number of dynamic order of this windows.
        /// </summary>
        public uint DynamicOrderCount
        {
            get { return (uint)_data.Length; }
        }

        #endregion

        #region Public operations

        /// <summary>
        /// Write windows coefficients to binary file.
        /// </summary>
        /// <param name="writer">Binary file writer.</param>
        /// <returns>Size of bytes written out.</returns>
        public uint Save(DataWriter writer)
        {
            Helper.ThrowIfNull(writer);

            uint size = 0;
            Debug.Assert(_data.Length > 0, "Zero length window is not supported.");
            Debug.Assert(_data.Max(r => r.Length) > 0, "Zero column is not supported.");
            if (!float.IsNaN(_data[0][0]))
            {
                int row = _data.Length;
                size += writer.Write((uint)row);

                for (int i = 0; i < row; i++)
                {
                    int validCount = _data[i].Count(d => !float.IsNaN(d));

                    size += writer.Write((uint)validCount);

                    foreach (float weight in _data[i])
                    {
                        if (!float.IsNaN(weight))
                        {
                            size += writer.Write(weight);
                        }
                    }
                }
            }
            else
            {
                size += writer.Write((uint)DataWriter.Padding);
            }

            return size;
        }

        /// <summary>
        /// Load dynamic window set from binary reader.
        /// </summary>
        /// <param name="reader">Binary reader.</param>
        /// <returns>Dynamic window set.</returns>
        public NNDynamicWindowSet Load(BinaryReader reader)
        {
            Helper.ThrowIfNull(reader);
            uint row = reader.ReadUInt32();
            if (row == 0)
            {
                _data = new float[][] { new float[] { float.NaN } };
            }
            else
            {
                _data = new float[row][];
                for (uint i = 0; i < row; i++)
                {
                    uint validComlune = reader.ReadUInt32();
                    _data[i] = new float[validComlune];
                    for (uint j = 0; j < validComlune; j++)
                    {
                        _data[i][j] = reader.ReadSingle();
                    }
                }
            }

            return this;
        }

        #endregion
    }
}