//----------------------------------------------------------------------------
// <copyright file="ScriptFileCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements XML script file collection.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Script
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// XML script file collection.
    /// </summary>
    public class ScriptFileCollection
    {
        #region Fields

        private bool _loadComments = false;
        private Language _language = Language.Neutral;
        private Collection<XmlScriptFile> _xmlScriptFiles = new Collection<XmlScriptFile>();

        // This may be script dir or script file path
        private string _scriptPath;

        #endregion

        #region Constructions

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptFileCollection"/> class.
        /// </summary>
        /// <param name="loadComment">Whether load comments.</param>
        public ScriptFileCollection(bool loadComment)
        {
            _loadComments = loadComment;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets Xml script file collection.
        /// </summary>
        public Collection<XmlScriptFile> XmlScriptFiles
        {
            get { return _xmlScriptFiles; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether load comments.
        /// </summary>
        public bool LoadComments
        {
            get { return _loadComments; }
            set { _loadComments = value; }
        }

        /// <summary>
        /// Gets Script path.
        /// </summary>
        public string ScriptPath
        {
            get { return _scriptPath; }
        }

        /// <summary>
        /// Gets or sets Xml script file collection.
        /// </summary>
        public Language Language
        {
            get { return _language; }
            set { _language = value; }
        }

        #endregion

        #region Public operation.

        /// <summary>
        /// Reset the script file collection.
        /// </summary>
        public void Reset()
        {
            _xmlScriptFiles.Clear();
        }

        /// <summary>
        /// Find items.
        /// </summary>
        /// <param name="itemId">Item ID to be founded.</param>
        /// <param name="searchDeletedItems">Whetehr search deleted items.</param>
        /// <returns>Founded item.</returns>
        public ScriptItem FindItem(string itemId, bool searchDeletedItems)
        {
            ScriptItem scriptItem = null;
            foreach (XmlScriptFile xmlScriptFile in _xmlScriptFiles)
            {
                scriptItem = xmlScriptFile.GetItem(itemId, searchDeletedItems);
                if (scriptItem != null)
                {
                    break;
                }
            }

            return scriptItem;
        }

        /// <summary>
        /// Indicating whether the script item has been deleted.
        /// </summary>
        /// <param name="scriptItem">ScriptItem.</param>
        /// <param name="status">Status.</param>
        /// <returns>Whether the script item has been deleted.</returns>
        public bool IsDeletedItem(ScriptItem scriptItem, ref TtsXmlStatus status)
        {
            bool isDeletedItem = false;
            status = null;
            foreach (XmlScriptFile xmlScriptFile in _xmlScriptFiles)
            {
                if (xmlScriptFile.DeletedItemsDict.ContainsKey(scriptItem))
                {
                    isDeletedItem = true;
                    status = xmlScriptFile.DeletedItemsDict[scriptItem];
                    break;
                }
            }

            return isDeletedItem;
        }

        /// <summary>
        /// Get script sentence.
        /// </summary>
        /// <param name="sentenceId">Sentence ID to be search.</param>
        /// <param name="searchDeletedSentence">Whether search deleted sentence.</param>
        /// <returns>Founded sentence.</returns>
        public ScriptSentence FindSentence(string sentenceId, bool searchDeletedSentence)
        {
            ScriptSentence sentence = null;
            foreach (XmlScriptFile xmlScriptFile in _xmlScriptFiles)
            {
                sentence = xmlScriptFile.GetSentence(sentenceId, searchDeletedSentence);
                if (sentence != null)
                {
                    break;
                }
            }

            return sentence;
        }

        /// <summary>
        /// Whether the item has been deleted.
        /// </summary>
        /// <param name="itemId">Item id to be found.</param>
        /// <returns>Whether the item is deleted..</returns>
        public bool IsDeletedItem(string itemId)
        {
            bool isDeletedItem = false;
            foreach (XmlScriptFile xmlScriptFile in _xmlScriptFiles)
            {
                if (xmlScriptFile.IsDeletedItem(itemId))
                {
                    isDeletedItem = true;
                    break;
                }
            }

            return isDeletedItem;
        }

        /// <summary>
        /// Get script items in the script file collection.
        /// </summary>
        /// <param name="containDeletedItems">Whether contains deleted items.</param>
        /// <returns>Enumerator of the lines in the given file.</returns>
        public IEnumerable<ScriptItem> ScriptItems(bool containDeletedItems)
        {
            foreach (XmlScriptFile scriptFile in _xmlScriptFiles)
            {
                foreach (ScriptItem scriptItem in scriptFile.Items)
                {
                    yield return scriptItem;
                }

                if (containDeletedItems)
                {
                    foreach (ScriptItem scriptItem in scriptFile.DeletedItemsDict.Keys)
                    {
                        yield return scriptItem;
                    }
                }
            }
        }

        /// <summary>
        /// Get script sentences in the script file collection.
        /// </summary>
        /// <param name="containDeletedItems">Whether contains deleted items.</param>
        /// <returns>Enumerator of the lines in the given file.</returns>
        public IEnumerable<ScriptSentence> ScriptSentences(bool containDeletedItems)
        {
            foreach (XmlScriptFile scriptFile in _xmlScriptFiles)
            {
                foreach (ScriptItem scriptItem in scriptFile.Items)
                {
                    foreach (ScriptSentence sentence in scriptItem.Sentences)
                    {
                        yield return sentence;
                    }
                }

                if (containDeletedItems)
                {
                    foreach (ScriptItem scriptItem in scriptFile.DeletedItemsDict.Keys)
                    {
                        foreach (ScriptSentence sentence in scriptItem.Sentences)
                        {
                            yield return sentence;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Save all script files.
        /// </summary>
        public void Save()
        {
            foreach (XmlScriptFile xmlScriptFile in _xmlScriptFiles)
            {
                XmlScriptFile.ContentControler controler = new XmlScriptFile.ContentControler();
                controler.SaveComments = _loadComments;
                xmlScriptFile.Save(xmlScriptFile.FilePath, null, controler);
            }
        }

        /// <summary>
        /// Save script files to the target file/dir.
        /// </summary>
        /// <param name="scriptPath">Target script file/dir path.</param>
        /// <param name="isDir">Whether the script path is dir.</param>
        /// <param name="saveAs">If save as need call SaveAs in XmlDataFile.</param>
        /// <param name="saveComment">Whether save comment.</param>
        public void Save(string scriptPath, bool isDir, bool saveAs, bool saveComment)
        {
            if (!isDir && _xmlScriptFiles.Count != 1)
            {
                throw new ArgumentException(Helper.NeutralFormat(
                    "Script file collection can only contains 1 element when the path [{0}] is not dir", scriptPath));
            }

            if (isDir)
            {
                Helper.EnsureFolderExist(scriptPath);
                foreach (XmlScriptFile xmlScriptFile in _xmlScriptFiles)
                {
                    string path = Path.Combine(scriptPath, Path.GetFileName(xmlScriptFile.FilePath));
                    if (saveComment)
                    {
                        xmlScriptFile.Save(path);
                    }
                    else
                    {
                        ExportSortedCleanXmlScriptFiles(xmlScriptFile, path);
                    }

                    if (saveAs)
                    {
                        xmlScriptFile.FilePath = path;
                    }
                }
            }
            else
            {
                Debug.Assert(_xmlScriptFiles.Count == 1);
                Helper.EnsureFolderExistForFile(scriptPath);
                if (saveComment)
                {
                    _xmlScriptFiles[0].Save(scriptPath);
                }
                else
                {
                    ExportSortedCleanXmlScriptFiles(_xmlScriptFiles[0], scriptPath);
                }

                if (saveAs)
                {
                    _xmlScriptFiles[0].FilePath = scriptPath;
                }
            }
        }

        /// <summary>
        /// Load script files from path, support laod from file or directory.
        /// Items whose pronunciation can't map using the phoneMap will be added
        /// To _unmappedItems.
        /// </summary>
        /// <param name="scriptPath">Script file path or dir.</param>
        /// <returns>Errors of the script.</returns>
        public ErrorSet Load(string scriptPath)
        {
            if (string.IsNullOrEmpty(scriptPath))
            {
                throw new ArgumentNullException("scriptPath");
            }

            if (!File.Exists(scriptPath) && !Directory.Exists(scriptPath))
            {
                throw new ArgumentException(Helper.NeutralFormat("Can't file script dir/file [{0}]", scriptPath));
            }

            Reset();
            _scriptPath = scriptPath;
            ErrorSet errorSet = new ErrorSet();
            if (File.Exists(scriptPath))
            {
                LoadScript(scriptPath, errorSet);
            }
            else if (Directory.Exists(scriptPath))
            {
                foreach (string filePath in Directory.GetFiles(scriptPath, "*" + XmlScriptFile.Extension))
                {
                    LoadScript(filePath, errorSet);
                }
            }

            return errorSet;
        }

        /// <summary>
        /// Sort XML script file items.
        /// </summary>
        /// <param name="scriptFile">Script file path.</param>
        /// <param name="targetFilePath">Target file path.</param>
        private static void ExportSortedCleanXmlScriptFiles(XmlScriptFile scriptFile, string targetFilePath)
        {
            XmlScriptFile sortedCleanScriptFile = new XmlScriptFile();
            sortedCleanScriptFile.Language = scriptFile.Language;
            sortedCleanScriptFile.Encoding = scriptFile.Encoding;
            SortedDictionary<string, ScriptItem> sortedItems = new SortedDictionary<string, ScriptItem>();

            foreach (KeyValuePair<string, ScriptItem> pair in scriptFile.ItemDic)
            {
                sortedItems.Add(pair.Key, pair.Value);
            }

            foreach (KeyValuePair<string, ScriptItem> pair in sortedItems)
            {
                sortedCleanScriptFile.Items.Add(pair.Value);
                sortedCleanScriptFile.ItemDic.Add(pair.Key, pair.Value);
            }

            XmlScriptFile.ContentControler controler = new XmlScriptFile.ContentControler();
            controler.SaveComments = false;
            sortedCleanScriptFile.Save(targetFilePath, scriptFile.Encoding, controler);
        }

        /// <summary>
        /// Load script.
        /// </summary>
        /// <param name="scriptPath">ScriptPath.</param>
        /// <param name="errorSet">ErrorSet.</param>
        private void LoadScript(string scriptPath, ErrorSet errorSet)
        {
            XmlScriptFile xmlScript = new XmlScriptFile();
            XmlScriptFile.ContentControler controler = new XmlScriptFile.ContentControler();
            controler.LoadComments = _loadComments;
            xmlScript.Load(scriptPath, controler);
            if (_language == xmlScript.Language)
            {
                _xmlScriptFiles.Add(xmlScript);
                foreach (Error error in xmlScript.ErrorSet.Errors)
                {
                    errorSet.Add(new Error(ScriptError.ScriptCollectionError,
                        Path.GetFileName(scriptPath),
                        error.ToString()));
                }
            }
            else
            {
                errorSet.Add(ScriptError.InvalidLanguage,
                    Localor.LanguageToString(xmlScript.Language),
                    Localor.LanguageToString(_language),
                    Path.GetFileName(scriptPath));
            }
        }

        #endregion
    }
}