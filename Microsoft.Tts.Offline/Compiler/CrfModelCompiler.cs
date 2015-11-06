//----------------------------------------------------------------------------
// <copyright file="CrfModelCompiler.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements CRF Model Compiler, which is used to pack crf model files
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
    /// CRF model error definition.
    /// </summary>
    public enum CrfModelCompilerError
    {
        /// <summary>
        /// Data Folder Not Found
        /// Parameters: 
        /// {0}: path of crf model data folder.
        /// </summary>
        [ErrorAttribute(Message = "Crf model folder '{0}' could not be found.")]
        DataFolderNotFound,

        /// <summary>
        /// Invalid Crf Model File.
        /// </summary>
        [ErrorAttribute(Message = "Invalid crf modle file '{0}' whose size mod 4 is not zero.")]
        InvalidCrfModel,

        /// <summary>
        /// Invalid Format About Crf Model Mapping File.
        /// {0}: path of crf model mapping file.
        /// </summary>
        [ErrorAttribute(Message = "Invalid Format About crf modle mapping file '{0}'.")]
        InvalidMappingFormat,

        /// <summary>
        /// Mapping File Not Found
        /// Parameters: 
        /// {0}: path of crf model mapping file.
        /// </summary>
        [ErrorAttribute(Message = "Crf model mapping file could not be found in '{0}'.")]
        MappingFileNotFound,

        /// <summary>
        /// Mapping Data Not Found
        /// Parameters: 
        /// {0}: path of crf model mapping file.
        /// </summary>
        [ErrorAttribute(Message = "Crf model mapping data could not be found in '{0}'.")]
        MappingDataNotFound,
    }

    /// <summary>
    /// Compile CRF model.
    /// </summary>
    public class CrfModelCompiler
    {
        /// <summary>
        /// Prevents a default instance of the <see cref="CrfModelCompiler"/> class from being created.
        /// </summary>
        private CrfModelCompiler()
        {
        }

        /// <summary>
        /// Compile crf model files into binary file.
        /// </summary>
        /// <param name="crfModelDir">Directory of crf model files.</param>
        /// <param name="outputStream">Output Stream.</param>
        /// <param name="addedFileNames">Output parameter - crf model files added to binary.</param>
        /// <param name="lang">Language info.</param>
        /// <returns>ErrorSet.</returns>
        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "Ignore.")]
        public static ErrorSet Compile(string crfModelDir, Stream outputStream,
            Collection<string> addedFileNames, Language lang)
        {
            if (string.IsNullOrEmpty(crfModelDir))
            {
                throw new ArgumentNullException("crfModelDir");
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

            if (!Directory.Exists(crfModelDir))
            {
                errorSet.Add(CrfModelCompilerError.DataFolderNotFound, crfModelDir);
            }
            else
            {
                BinaryWriter bw = new BinaryWriter(outputStream, Encoding.Unicode);
                List<byte[]> crfModels = new List<byte[]>();
                List<string> crfTags = new List<string>();
                Dictionary<string, string> localizedNameMapping = null;

                // if language = zh-cn or ja-jp, we should use their localized name as crf model tag.
                if (lang == Language.ZhCN || lang == Language.JaJP)
                {
                    string crfModelNameMappingFile = Path.Combine(new DirectoryInfo(crfModelDir).Parent.FullName, "CRFLocalizedMapping.txt");
                    if (File.Exists(crfModelNameMappingFile))
                    {
                        // If the mapping file is existed, load it.
                        localizedNameMapping = LocalizeCRFModelName(crfModelNameMappingFile, errorSet);
                    }
                    else
                    {
                        errorSet.Add(CrfModelCompilerError.MappingFileNotFound, crfModelNameMappingFile);
                    }
                }

                string[] crfModelFileNames = Directory.GetFiles(crfModelDir, "*.crf", SearchOption.TopDirectoryOnly);
                foreach (string crfModelFileName in crfModelFileNames)
                {
                    if (localizedNameMapping != null)
                    {
                        // If mapping is existed, replace the crfTag name.
                        string crfModelName = Path.GetFileName(crfModelFileName);
                        if (localizedNameMapping.ContainsKey(crfModelName))
                        {
                            crfTags.Add(localizedNameMapping[crfModelName].ToUpper()); // case insensitive
                        }
                    }
                    else
                    {
                        crfTags.Add(Path.GetFileNameWithoutExtension(crfModelFileName).ToUpper());  // case insensitive
                    }

                    using (FileStream fs = new FileStream(crfModelFileName, FileMode.Open, FileAccess.Read))
                    using (BinaryReader br = new BinaryReader(fs))
                    {
                        crfModels.Add(br.ReadBytes((int)fs.Length));
                    }

                    addedFileNames.Add(crfModelFileName);
                }

                using (StringPool crfModelSp = new StringPool())
                {
                    using (StringPool crfTagSp = new StringPool())
                    {
                        Collection<int> crfModelsOffsets = new Collection<int>();
                        Collection<int> crfTagsOffsets = new Collection<int>();

                        // Add models to StringPool
                        foreach (byte[] model in crfModels)
                        {
                            crfModelsOffsets.Add(crfModelSp.PutBuffer(model));
                        }

                        // Add tags to StringPool
                        StringPool.WordsToStringPool(crfTags, crfTagSp, crfTagsOffsets);
                        uint tagOffset = 0;
                        uint modelOffset = 0;
                        bw.Write(tagOffset);
                        bw.Write(modelOffset);

                        bw.Write((uint)crfTagsOffsets.Count);

                        for (int i = 0; i < crfModelsOffsets.Count; i++)
                        {
                            uint offset = (uint)crfModelsOffsets[i];
                            if ((offset % 4) != 0)
                            {
                                errorSet.Add(CrfModelCompilerError.InvalidCrfModel, crfModelFileNames[i]);
                            }

                            bw.Write(offset);
                        }

                        foreach (int offset in crfTagsOffsets)
                        {
                            bw.Write((uint)offset);
                        }

                        modelOffset = (uint)bw.BaseStream.Position;
                        Debug.Assert((modelOffset % 4) == 0);
                        byte[] crfModelPool = crfModelSp.ToArray();
                        bw.Write(crfModelPool, 0, crfModelPool.Length);

                        tagOffset = (uint)bw.BaseStream.Position;
                        Debug.Assert((tagOffset % 4) == 0);
                        byte[] crfTagPool = crfTagSp.ToArray();
                        bw.Write(crfTagPool, 0, crfTagPool.Length);

                        bw.Flush();

                        // Update offset value.
                        bw.Seek(0, SeekOrigin.Begin);
                        bw.Write(tagOffset);
                        bw.Write(modelOffset);

                        bw.Flush();
                    }
                }
            }

            return errorSet;
        }

        /// <summary>
        /// Load CRF model name mapping(model name and localized name).
        /// </summary>
        /// <param name="mappingFile">Mapping File Path.</param>
        /// <param name="errorSet">Error set.</param>
        /// <returns>CRF name mapping set.</returns>
        private static Dictionary<string, string> LocalizeCRFModelName(string mappingFile, ErrorSet errorSet)
        {
            Dictionary<string, string> cfrModelNameMappings = new Dictionary<string, string>();

            // start flag of crf model mapping data.
            const string MappingFlag = "Map between polyphony model:";

            try
            {
                using (StreamReader sr = new StreamReader(mappingFile))
                {
                    // if the first line is correct, read the file to end.
                    if (string.Compare(MappingFlag, sr.ReadLine()) == 0)
                    {
                        while (!sr.EndOfStream)
                        {
                            string[] mapping = sr.ReadLine().Split('\t');

                            if (string.Compare("Being_used", mapping[3]) == 0)
                            {
                                cfrModelNameMappings.Add(mapping[2], mapping[0]);
                            }
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch (IndexOutOfRangeException)
            {
                errorSet.Add(CrfModelCompilerError.InvalidMappingFormat, mappingFile);
            }

            if (cfrModelNameMappings.Count == 0)
            {
                errorSet.Add(CrfModelCompilerError.MappingDataNotFound, mappingFile);
            }

            return cfrModelNameMappings;
        }
    }
}