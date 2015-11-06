//----------------------------------------------------------------------------
// <copyright file="ScriptTraversal.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements script traversal
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Script
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Script item processor.
    /// </summary>
    public interface IScriptItemProcessor
    {
        /// <summary>
        /// Process item method.
        /// </summary>
        /// <param name="scriptItem">Script item.</param>
        /// <param name="parameters">Parameters.</param>
        void ProcessItem(ScriptItem scriptItem, object parameters);
    }

    /// <summary>
    /// Script traversal.
    /// </summary>
    public class ScriptTraversal
    {
        #region Public static methods

        /// <summary>
        /// Traverse script directory.
        /// </summary>
        /// <param name="visitor">Script item processor interface.</param>
        /// <param name="importDataPathList">File list for validation.</param>
        /// <param name="inScriptDir">Input script directory.</param>
        /// <param name="outScriptDir">Output script directory.</param>
        /// <param name="parameters">Parameters.</param>
        public static void TraverseScriptDir(IScriptItemProcessor visitor, Dictionary<string, string> importDataPathList, 
            string inScriptDir, string outScriptDir, object parameters)
        {
            TraverseScriptDir(visitor, importDataPathList, inScriptDir, outScriptDir, null, parameters);
        }

        /// <summary>
        /// Traverse script directory.
        /// </summary>
        /// <param name="visitor">Script item processor interface.</param>
        /// <param name="importDataPathList">File list for validation.</param>
        /// <param name="inScriptDir">Input script directory.</param>
        /// <param name="outScriptDir">Output script directory.</param>
        /// <param name="logger">Text logger.</param>
        /// <param name="parameters">Parameters.</param>
        public static void TraverseScriptDir(IScriptItemProcessor visitor, Dictionary<string, string> importDataPathList, 
            string inScriptDir, string outScriptDir, TextLogger logger,  
            object parameters)
        {
            if (string.IsNullOrEmpty(inScriptDir))
            {
                throw new ArgumentNullException("inScriptDir");
            }

            if (!Directory.Exists(inScriptDir))
            {
                throw new DirectoryNotFoundException(inScriptDir);
            }
           
            string pattern = @"*" + XmlScriptFile.Extension;
            foreach (string relativeFilePath in Helper.GetSubFilesRelativePath(
                inScriptDir, pattern))
            {
                string sourcePath = Path.Combine(inScriptDir, relativeFilePath);
                string targetFilePath = string.Empty;
                if (!string.IsNullOrEmpty(outScriptDir))
                {
                    targetFilePath = Path.Combine(outScriptDir, relativeFilePath);
                }

                TraverseScriptFile(visitor, importDataPathList, sourcePath, targetFilePath, logger, 
                    parameters);
            }
        }

        /// <summary>
        /// Traverse script file.
        /// </summary>
        /// <param name="visitor">Script item processor interface.</param>
        /// <param name="importDataPathList">File list for validation.</param>
        /// <param name="inScriptFile">Input script file.</param>
        /// <param name="outScriptFile">Output script file.</param>
        /// <param name="parameters">Parameters.</param>
        public static void TraverseScriptFile(IScriptItemProcessor visitor, Dictionary<string, string> importDataPathList, 
            string inScriptFile, string outScriptFile, object parameters)
        {
            TraverseScriptFile(visitor, importDataPathList, inScriptFile, outScriptFile, null, parameters);
        }

        /// <summary>
        /// Traverse script file.
        /// </summary>
        /// <param name="visitor">Script item processor interface.</param>
        /// <param name="importDataPathList">File list for importing data model.</param>
        /// <param name="inScriptFile">Input script file.</param>
        /// <param name="outScriptFile">Output script file.</param>
        /// <param name="logger">Text logger.</param>
        /// <param name="parameters">Parameters.</param>
        public static void TraverseScriptFile(IScriptItemProcessor visitor, Dictionary<string, string> importDataPathList, 
            string inScriptFile, string outScriptFile, TextLogger logger,
            object parameters)
        {
            if (string.IsNullOrEmpty(inScriptFile))
            {
                throw new ArgumentNullException("inScriptFile");
            }

            if (!File.Exists(inScriptFile))
            {
                throw new FileNotFoundException(inScriptFile);
            }

            XmlScriptFile script = new XmlScriptFile();
            string message = Helper.NeutralFormat("Processing file : {0}", inScriptFile);
            Console.WriteLine(message);
            string fileName = Path.GetFileName(inScriptFile);

            try
            {
                script.Load(inScriptFile);
                List<string> listDiscard = new List<string>();

                foreach (ScriptItem scriptItem in script.Items)
                {
                    try
                    {
                        // When the mode is "Import F0/Power/Segment", the importDataPathList will not be null.
                        if (importDataPathList == null || importDataPathList.ContainsKey(scriptItem.Id))
                        {
                            visitor.ProcessItem(scriptItem, parameters);
                        }
                        else
                        {
                            listDiscard.Add(scriptItem.Id);
                        }
                    }
                    catch (InvalidDataException exception)
                    {
                        if (logger != null)
                        {
                            logger.LogLine("ERROR : [File {0}][Item {1}]{2}", fileName,
                                scriptItem.Id, exception.Message);
                        }
                    }
                }

                foreach (string id in listDiscard)
                {
                    script.Remove(id);
                }

                if (!string.IsNullOrEmpty(outScriptFile))
                {
                    Helper.TestWritable(outScriptFile);
                    script.Save(outScriptFile, Encoding.Unicode);
                }
            }
            catch (InvalidDataException exception)
            {
                if (logger != null)
                {
                    logger.LogLine("ERROR : [File {0}]{1}", fileName, exception.Message);
                }
            }
        }

        #endregion
    }   
}