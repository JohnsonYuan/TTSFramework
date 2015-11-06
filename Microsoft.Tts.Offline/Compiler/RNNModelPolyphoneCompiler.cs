//----------------------------------------------------------------------------
// <copyright file="RNNModelPolyphoneCompiler.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements RNN Model Compiler, which is used to pack rnn model files
// </summary>
//----------------------------------------------------------------------------
namespace Microsoft.Tts.Offline.Compiler
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Text;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// RNN model error definition.
    /// </summary>
    public enum RNNModelCompilerError
    {
        /// <summary>
        /// Model Data Not Found
        /// Parameters: 
        /// {0}: path of rnn model data.
        /// </summary>
        [ErrorAttribute(Message = "RNN model '{0}' could not be found.")]
        ModelDataNotFound,

        /// <summary>
        /// Invalid Format About RNN Model Polyphonic Character List File.
        /// {0}: path of rnn model polyphonic charcter list file.
        /// </summary>
        [ErrorAttribute(Message = "Invalid Format About rnn modle polyphonic character file '{0}'.")]
        InvalidCharacterListFormat,

        /// <summary>
        /// Polyphonic Character List File Not Found
        /// Parameters: 
        /// {0}: path of rnn model  polyphonic charcter list file.
        /// </summary>
        [ErrorAttribute(Message = "RNN model polyphonic character file could not be found in '{0}'.")]
        PolyphonicCharFileNotFound,

        /// <summary>
        /// Polyphonic Character Not Found
        /// Parameters: 
        /// {0}: path of rnn model polyphonic charcter list file.
        /// </summary>
        [ErrorAttribute(Message = "RNN model polyphonic character could not be found in '{0}'.")]
        PolyphonicCharNotFound,
    }

    /// <summary>
    /// Compile RNN model.
    /// </summary>
    public class RNNModelCompiler
    {
        /// <summary>
        /// Prevents a default instance of the <see cref="RNNModelCompiler"/> class from being created.
        /// </summary>
        private RNNModelCompiler()
        {
        }

        /// <summary>
        /// Compile rnn model files into binary file.
        /// </summary>
        /// <param name="rnnModelPath">RNN model file path.</param>
        /// <param name="outputStream">Output Stream.</param>
        /// <param name="addedFileNames">Output parameter - rnn model files added to binary. now we have only one rnn model. keep this parameter is for future to maintain more than one rnn model.</param>
        /// <returns>ErrorSet.</returns>
        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "Ignore.")]
        public static ErrorSet Compile(string rnnModelPath, Stream outputStream, Collection<string> addedFileNames)
        {
            if (string.IsNullOrEmpty(rnnModelPath))
            {
                throw new ArgumentNullException("rnnModelPath");
            }

            if (outputStream == null)
            {
                throw new ArgumentNullException("outputStream");
            }

            if (addedFileNames == null)
            {
                throw new ArgumentNullException("addedFileNames");
            }

            ErrorSet errorSet = new ErrorSet();

            if (!File.Exists(rnnModelPath))
            {
                errorSet.Add(RNNModelCompilerError.ModelDataNotFound, rnnModelPath);
            }
            else
            {
                BinaryWriter bw = new BinaryWriter(outputStream, Encoding.Unicode);
                Dictionary<string, float> polyCharactersInfo = null;
                List<string> polyphones = null;
                List<float> thresholds = null;

                // load polyphonic characters that should be enabled in product.
                string polyphonicCharFile = Path.Combine(new DirectoryInfo(Path.GetDirectoryName(rnnModelPath)).Parent.FullName, "RNNPolyphoneList.txt");
                if (File.Exists(polyphonicCharFile))
                {
                    // If the list file is existed, load it.
                    polyCharactersInfo = LoadPolyphonicInfo(polyphonicCharFile, errorSet);
                }
                else
                {
                    errorSet.Add(RNNModelCompilerError.PolyphonicCharFileNotFound, polyphonicCharFile);
                }

                polyphones = GetPolyphonicChars(polyCharactersInfo);
                thresholds = GetPolyphonicThreshold(polyCharactersInfo);

                uint polyCharCount = 0;
                uint modelOffset = 0;

                // write the count of polyphonic characters and polyphonic characters
                using (StringPool plycharSp = new StringPool())
                {
                    Collection<int> polycharOffsets = new Collection<int>();
                    StringPool.WordsToStringPool(polyphones, plycharSp, polycharOffsets);

                    polyCharCount = (uint)polycharOffsets.Count;

                    bw.Write(modelOffset);
                    bw.Write(polyCharCount);

                    foreach (float threshold in thresholds)
                    {
                        bw.Write(threshold);
                    }

                    byte[] plycharPool = plycharSp.ToArray();
                    foreach (int offset in polycharOffsets)
                    {
                        bw.Write((uint)offset);
                    }

                    bw.Write(plycharPool, 0, plycharPool.Length);
                }

                modelOffset = (uint)bw.BaseStream.Position;

                // write rnn models
                using (FileStream fs = new FileStream(rnnModelPath, FileMode.Open, FileAccess.Read))
                using (BinaryReader br = new BinaryReader(fs))
                {
                    bw.Write(br.ReadBytes((int)fs.Length));
                }

                bw.Flush();

                bw.Seek(0, SeekOrigin.Begin);
                bw.Write(modelOffset);

                bw.Flush();

                addedFileNames.Add(rnnModelPath);
            }

            return errorSet;
        }

        /// <summary>
        /// Load Polyphonic Characters That Should Be Enabled in Product.
        /// </summary>
        /// <param name="listFile">List File Path.</param>
        /// <param name="errorSet">Error set.</param>
        /// <returns>Polyphonic characters set.</returns>
        private static Dictionary<string, float> LoadPolyphonicInfo(string listFile, ErrorSet errorSet)
        {
            Dictionary<string, float> polyCharactersInfo = new Dictionary<string, float>();

            using (StreamReader sr = new StreamReader(listFile))
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();

                    if (!string.IsNullOrEmpty(line))
                    {
                        string[] parts = line.Split(new char[] { '\t' });

                        if (parts.Length == 2)
                        {
                            string character = parts[0];
                            float threshold = float.Parse(parts[1]);
                            if (character.Length > 1)
                            {
                                errorSet.Add(RNNModelCompilerError.InvalidCharacterListFormat, listFile);
                            }
                            else
                            {
                                polyCharactersInfo.Add(character, threshold);
                            }
                        }
                        else
                        {
                            errorSet.Add(RNNModelCompilerError.InvalidCharacterListFormat, listFile);
                        }
                    }
                }
            }

            if (polyCharactersInfo.Count == 0)
            {
                errorSet.Add(RNNModelCompilerError.PolyphonicCharNotFound, listFile);
            }

            return polyCharactersInfo;
        }

        /// <summary>
        /// Load Polyphonic Characters That Should Be Enabled in Product.
        /// </summary>
        /// <param name="polyCharactersInfo">PolyCharactersInfo.</param>
        /// <returns>Polyphonic characters set.</returns>
        private static List<string> GetPolyphonicChars(Dictionary<string, float> polyCharactersInfo)
        {
            List<string> polyphones = new List<string>();

            foreach (var paris in polyCharactersInfo)
            {
                polyphones.Add(paris.Key);
            }

            return polyphones;
        }

        /// <summary>
        /// Get Polyphonic Characters Threshold Which will Be Used in Product.
        /// </summary>
        /// <param name="polyCharactersInfo">PolyCharactersInfo.</param>
        /// <returns>Polyphonic characters  Threshold set.</returns>
        private static List<float> GetPolyphonicThreshold(Dictionary<string, float> polyCharactersInfo)
        {
            List<float> polyphoneThreshold = new List<float>();

            foreach (var paris in polyCharactersInfo)
            {
                polyphoneThreshold.Add(paris.Value);
            }

            return polyphoneThreshold;
        }
    }
}