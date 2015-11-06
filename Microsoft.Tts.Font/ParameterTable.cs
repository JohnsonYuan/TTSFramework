//----------------------------------------------------------------------------
// <copyright file="ParameterTable.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module defines classes to write parameter table
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;
    using Microsoft.Tts.Offline.Config;
    using Microsoft.Tts.Offline.Utility;
    using Microsoft.Tts.ServiceProvider;

    /// <summary>
    /// Interface of parameter item.
    /// </summary>
    public interface IParameterItem
    {
        /// <summary>
        /// Gets the length of parameter data.
        /// </summary>
        int Length { get; }

        /// <summary>
        /// Load the parameter data from the bindary reader.
        /// </summary>
        /// <param name="reader">Binary reader.</param>
        /// <param name="length">Indicate the byte length.</param>
        void Load(BinaryReader reader, int length);
        
        /// <summary>
        /// Save the parameter data into binary format.
        /// </summary>
        /// <param name="writer">Binary writer.</param>
        /// <returns>How many bytes are written.</returns>
        int Save(BinaryWriter writer);

        /// <summary>
        /// Save the parameter data into string [].
        /// </summary>
        /// <returns>String .</returns>
        string[] SaveToString();

        /// <summary>
        /// Load the parameter data from string [].
        /// </summary>
        /// <param name="lines">String to contain the value.</param>
        void LoadFromString(string[] lines);
    }

    /// <summary>
    /// Class to handle parameters in array format.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    public class ArrayParameter<T> : IParameterItem
        where T : struct
    {
        public ArrayParameter(int itemSize)
        {
            ItemSize = itemSize;
        }

        /// <summary>
        /// Gets or sets float array.
        /// </summary>
        public T[] Array { get; set; }

        /// <summary>
        /// Gets the item size.
        /// </summary>
        public int ItemSize { get; private set; }

        /// <summary>
        /// Gets the length of parameter data.
        /// </summary>
        public int Length
        {
            get
            {
                return Array.Length * ItemSize;
            }
        }

        /// <summary>
        /// Load the parameter data from the bindary reader.
        /// </summary>
        /// <param name="reader">Binary reader.</param>
        /// <param name="length">Indicate the byte length.</param>
        public void Load(BinaryReader reader, int length)
        {
            if (length % ItemSize != 0)
            {
                throw new ArgumentException("the given length can't divide item size", "length");
            }

            int itemCount = length / ItemSize;
            Array = new T[itemCount];
            for (int index = 0; index < itemCount; index++)
            {
                byte[] buffer = reader.ReadBytes(ItemSize);
                Array[index] = Helper.FromBytes<T>(buffer);
            }
        }

        /// <summary>
        /// Save the parameter data into binary format.
        /// </summary>
        /// <param name="writer">Binary writer.</param>
        /// <returns>How many bytes are written.</returns>
        public int Save(BinaryWriter writer)
        {
            for (int index = 0; index < Array.Length; index++)
            {
                writer.Write(Helper.ToBytes(Array[index]));
            }

            return Length;
        }

        /// <summary>
        /// Save the parameter data into string [].
        /// </summary>
        /// <returns>String .</returns>
        public string[] SaveToString()
        {
            return Array.Select(v => v.ToString()).ToArray();
        }

        /// <summary>
        /// Load the parameter data from string [].
        /// </summary>
        /// <param name="lines">Lines.</param>
        public void LoadFromString(string[] lines)
        {
            int itemCount = lines.Length;
            Array = new T[itemCount];
            for (int index = 0; index < itemCount; index++)
            {
                string line = lines[index];
                if (typeof(T) == typeof(int))
                {
                    Array[index] = (T)Convert.ChangeType(int.Parse(line), typeof(T));
                }
                else if (typeof(T) == typeof(float))
                {
                    Array[index] = (T)Convert.ChangeType(float.Parse(line), typeof(T));
                }
                else
                {
                    throw new NotImplementedException(
                        Helper.NeutralFormat("LoadFromString() is not implemented for type {0}", typeof(T)));
                }
            }
        }
    }

    /// <summary>
    /// Class for parameter Table.
    /// </summary>
    public class ParameterTable
    {
        /// <summary>
        /// Const value for file tag.
        /// </summary>
        private const uint FileTag = 0x54504542;

        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterTable"/> class.
        /// </summary>
        public ParameterTable()
        {
            Parameters = new Dictionary<RuntimeParameter, IParameterItem>();
        }

        /// <summary>
        /// Gets or sets Construction function.
        /// </summary>
        public uint BuildNumber
        {
            get;
            set;
        }

        /// <summary>
        /// Gets Holds table file header data.
        /// </summary>
        public Dictionary<RuntimeParameter, IParameterItem> Parameters
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets voice font header data.
        /// </summary>
        public VoiceFontHeader VoiceFontHeaderData
        {
            get;
            private set;
        }

        /// <summary>
        /// Load overall the parameter talbe from file.
        /// </summary>
        /// <param name="fileName">File name of source file.</param>
        public void Load(string fileName)
        {
            Parameters.Clear();

            FileStream file = new FileStream(fileName, FileMode.Open);
            try
            {
                using (BinaryReader reader = new BinaryReader(file))
                {
                    file = null;

                    VoiceFontHeaderData = new VoiceFontHeader();
                    VoiceFontHeaderData.Load(reader);
                    if (VoiceFontHeaderData.FileTag != FileTag)
                    {
                        throw new InvalidDataException(Helper.NeutralFormat("Unsupported file tag \"{0}\"", Helper.UintToString(VoiceFontHeaderData.FileTag)));
                    }

                    if (VoiceFontHeaderData.FormatTag != VoiceFontTag.FmtIdParameterTable)
                    {
                        throw new InvalidDataException(Helper.NeutralFormat("Unsupported GUID \"{0}\"", VoiceFontHeaderData.FormatTag.ToString("D", CultureInfo.InvariantCulture)));
                    }

                    int parametersCount = reader.ReadInt32();
                    if (parametersCount < 0)
                    {
                        throw new InvalidDataException(Helper.NeutralFormat("Unsupported Parameters Count \"{0}\"", parametersCount));
                    }

                    List<Pair<RuntimeParameter, int>> indexs = new List<Pair<RuntimeParameter, int>>();
                    for (int index = 0; index < parametersCount; index++)
                    {
                        RuntimeParameter parameter = (RuntimeParameter)reader.ReadInt32();
                        int offset = reader.ReadInt32();
                        int length = reader.ReadInt32();
                        if (index == 0 && offset != 0)
                        {
                            throw new InvalidDataException(Helper.NeutralFormat("Offset must start from 0, current is \"{0}\"", offset));
                        }

                        indexs.Add(new Pair<RuntimeParameter, int>(parameter, length));
                    }

                    // load data
                    foreach (Pair<RuntimeParameter, int> index in indexs)
                    {
                        IParameterItem item = CreateParameterTable(index.Left);
                        item.Load(reader, index.Right);
                        Parameters.Add(index.Left, item);
                    }
                }
            }
            finally
            {
                if (null != file)
                {
                    file.Dispose();
                }
            }
        }

        /// <summary>
        /// Save overall the parameter table into file.
        /// </summary>
        /// <param name="fileName">File name of target file.</param>
        public void Save(string fileName)
        {
            VoiceFontHeader voiceFontHeader = new VoiceFontHeader
            {
                FileTag = FileTag,
                FormatTag = VoiceFontTag.FmtIdParameterTable,
                DataSize = (ulong)GetFileDataSize(),
                Version = (uint)FormatVersion.Tts30,
                Build = BuildNumber
            };

            Helper.EnsureFolderExistForFile(fileName);

            FileStream file = new FileStream(fileName, FileMode.Create);
            try
            {
                using (BinaryWriter writer = new BinaryWriter(file))
                {
                    file = null;
                    int offset = 0;
                    voiceFontHeader.Save(writer);

                    long positionOfIndex = writer.BaseStream.Position;
                    writer.Write(Parameters.Count);
                    foreach (RuntimeParameter parameter in Parameters.Keys)
                    {
                        writer.Write((int)parameter);
                        writer.Write(offset);
                        writer.Write(Parameters[parameter].Length);
                        offset += Parameters[parameter].Length;
                    }

                    foreach (RuntimeParameter parameter in Parameters.Keys)
                    {
                        Parameters[parameter].Save(writer);
                    }

                    Debug.Assert(writer.BaseStream.Position - positionOfIndex == (long)voiceFontHeader.DataSize, "Data size calculation is wrong");
                }
            }
            finally
            {
                if (null != file)
                {
                    file.Dispose();
                }
            }
        }

        /// <summary>
        /// Save overall the parameter table into file in plain text format.
        /// </summary>
        /// <param name="fileName">File name of target file.</param>
        public void SaveToText(string fileName)
        {
            Helper.EnsureFolderExistForFile(fileName);
            using (StreamWriter writer = new StreamWriter(fileName))
            {
                foreach (RuntimeParameter parameter in Parameters.Keys)
                {
                    string[] outputText = Parameters[parameter].SaveToString();
                    writer.WriteLine("KEY:{0} LineNum:{1}", Enum.GetName(typeof(RuntimeParameter), parameter), outputText.Length);
                    foreach (string text in outputText)
                    {
                        writer.WriteLine("\t{0}", text);
                    }
                }
            }
        }

        /// <summary>
        /// Load overall the parameter table from file in plain text format.
        /// </summary>
        /// <param name="fileName">File name of target file.</param>
        public void LoadFromText(string fileName)
        {
            Parameters.Clear();

            using (StreamReader reader = new StreamReader(fileName))
            {
                // load data
                while (!reader.EndOfStream)
                {
                    string title = reader.ReadLine();
                    string tableName = string.Empty;
                    int tableTextLength = 0;

                    Match match = Regex.Match(title, @"KEY:(.+) LineNum:(\d+)");
                    if (match.Success && match.Groups.Count == 3)
                    {
                        tableName = match.Groups[1].Value;
                        tableTextLength = int.Parse(match.Groups[2].Value);
                    }
                    else
                    {
                        throw new InvalidDataException(Helper.NeutralFormat("format is invalid for parameter table title: {0}", title));
                    }

                    RuntimeParameter parameter = (RuntimeParameter)Enum.Parse(typeof(RuntimeParameter), tableName);
                    IParameterItem item = CreateParameterTable(parameter);
                    string[] tableContext = new string[tableTextLength];
                    for (int index = 0; index < tableTextLength; index++)
                    {
                        tableContext[index] = reader.ReadLine().TrimStart('\t');
                    }

                    item.LoadFromString(tableContext);
                    Parameters.Add(parameter, item);
                }
            }
        }

        /// <summary>
        /// Create parameter item according to the parameter name.
        /// </summary>
        /// <param name="parameter">Parameter name.</param>
        /// <returns>Parameter item.</returns>
        private IParameterItem CreateParameterTable(RuntimeParameter parameter)
        {
            IParameterItem item = null;

            // new feature need to add corresponding ParameterItem class here.
            // ToDO, update below code according to new parameter
            switch (parameter)
            {
                case RuntimeParameter.RPARAM_UNITLATTICEGEN_DURSHORTERTHRESHOLD:
                case RuntimeParameter.RPARAM_UNITLATTICEGEN_DURLONGERTHRESHOLD:
                    throw new NotImplementedException(Enum.GetName(typeof(RuntimeParameter), parameter));
                case RuntimeParameter.RPARAM_UNITLATTICEGEN_SPECTRUMFILTERTHRESHOLD:
                case RuntimeParameter.RPARAM_UNITLATTICEGEN_FRAMESHIFTING:
                case RuntimeParameter.RPARAM_UNITLATTICEGEN_SPECTURMORDERONWLD:
                case RuntimeParameter.RPARAM_UNITLATTICEGEN_LATCANDREMAINING:
                case RuntimeParameter.RPARAM_UNITSELECTION_CCTFSRANGE:
                case RuntimeParameter.RPARAM_UNITLATTICEGEN_PRESELECTCANDNUM:
                case RuntimeParameter.RPARAM_UNITLATTICEGEN_SPECTURMORDER:
                case RuntimeParameter.RPARAM_UNITSELECTION_DOPOLYFIT:
                case RuntimeParameter.RPARAM_UNITSELECTION_DODCTFIT:
                case RuntimeParameter.RPARAM_UNITSELECTION_PHOWINDLENTHFORSMOOTH:
                case RuntimeParameter.RPARAM_UNITSELECTION_ORDERFORFIT:
                case RuntimeParameter.RPARAM_UNITSELECTION_MINF0CONTOURLENTH:
                case RuntimeParameter.RPARAM_UNITSELECTION_MAXF0CONTOURLENTH:
                case RuntimeParameter.RPARAM_UNITSELECTION_SCALELENGTH:
                    item = new ArrayParameter<int>(sizeof(int)); 
                    break;
                case RuntimeParameter.RPARAM_UNITSELECTION_TARGETCOSTWEIGHT:
                case RuntimeParameter.RPARAM_UNITLATTICEGEN_SPECTTARGETCOSTWEIGHT:
                case RuntimeParameter.RPARAM_UNITLATTICEGEN_PITCHTARGETCOSTWEIGHT:
                case RuntimeParameter.RPARAM_UNITLATTICEGEN_POWERTARGETCOSTWEIGHT:
                case RuntimeParameter.RPARAM_UNITSELECTION_SPECTLCONCCOSTWEIGHT:
                case RuntimeParameter.RPARAM_UNITSELECTION_SPECTRCONCCOSTWEIGHT:
                case RuntimeParameter.RPARAM_UNITSELECTION_PITCHLCONCCOSTWEIGHT:
                case RuntimeParameter.RPARAM_UNITSELECTION_PITCHRCONCCOSTWEIGHT:
                case RuntimeParameter.RPARAM_UNITSELECTION_POWERLCONCCOSTWEIGHT:
                case RuntimeParameter.RPARAM_UNITSELECTION_POWERRCONCCOSTWEIGHT:
                case RuntimeParameter.RPARAM_UNITSELECTION_AVGSPECTRUMCOST:
                case RuntimeParameter.RPARAM_UNITSELECTION_COSTABLE:
                case RuntimeParameter.RPARAM_UNITSELECTION_SMOOTHTABLE:
                case RuntimeParameter.RPARAM_UNITSELECTION_POWTABLE:
                case RuntimeParameter.RPARAM_UNITLATTICEGEN_PITCHTARGETCOSTWEIGHT_REALLSP:
                case RuntimeParameter.RPARAM_LAST:   // it is just for unit test
                    item = new ArrayParameter<float>(sizeof(float)); 
                    break;
                default:
                    throw new InvalidDataException(
                        Helper.NeutralFormat("Invalidate runtime parameter ID \"{0}\"", parameter));
            }

            return item;
        }

        /// <summary>
        /// Get voice font file data size.
        /// </summary>
        /// <returns>Voice font file data size.</returns>
        private long GetFileDataSize()
        {
            long size = sizeof(int) + ((sizeof(RuntimeParameter) + (2 * sizeof(int))) * Parameters.Count);
            foreach (RuntimeParameter parameter in Parameters.Keys)
            {
                Debug.Assert(Parameters[parameter].Length >= 0);
                size += Parameters[parameter].Length;
            }

            return size;
        }
    }
}