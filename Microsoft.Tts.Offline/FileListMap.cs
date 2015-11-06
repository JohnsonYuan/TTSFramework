//----------------------------------------------------------------------------
// <copyright file="FileListMap.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements filelist map file
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
    using System.Text;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Class to manage file list map file
    /// File list map file has following format:
    /// ((sentence id) (relatice path))+.
    /// <example>
    /// 100001 Am_En101\100001
    /// 100002 Am_En101\100002.
    /// </example>
    /// Advantage of using file list map:
    /// 1) Group related files into same folder, and give the folder a meaningful label
    ///    this make the data easy to understand and management
    /// 2) Can divide all files into many subdirs, this can speed up the file searching.
    /// </summary>
    public class FileListMap
    {
        #region Fields

        private string _filePath;
        private Dictionary<string, string> _map;

        #endregion

        #region Properties

        /// <summary>
        /// Gets Map data.
        /// </summary>
        public Dictionary<string, string> Map
        {
            get { return _map; }
        }

        /// <summary>
        /// Gets or sets File path of this file list map.
        /// </summary>
        public string FilePath
        {
            get
            {
                return _filePath;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _filePath = value;
            }
        }

        #endregion

        #region Static operations

        /// <summary>
        /// Map filemap into folder indexing items 
        /// For example, it will map following items
        ///     10001 ENU001\10001
        ///     10002 ENU001\10002
        ///     10003 ENU002\10003
        /// Into
        ///     ENU001  10001
        ///             10002
        ///     ENU002  10003.
        /// </summary>
        /// <param name="map">File list map.</param>
        /// <returns>Folder indexing items.</returns>
        public static Dictionary<string, string[]> MapToFolder(Dictionary<string, string> map)
        {
            if (map == null)
            {
                throw new ArgumentNullException("map");
            }

            if (map.Keys == null)
            {
                throw new ArgumentException("map.Keys is null");
            }

            Dictionary<string, List<string>> temp = new Dictionary<string, List<string>>();
            foreach (string key in map.Keys)
            {
                string value = map[key];
                string[] items = value.Split(new char[] { '\\', ' ', '/' },
                    StringSplitOptions.RemoveEmptyEntries);
                Debug.Assert(items.Length >= 1);

                // default as current directory
                string folderName = ".";
                if (items.Length == 2)
                {
                    folderName = items[0];
                }

                if (!temp.ContainsKey(folderName))
                {
                    temp.Add(folderName, new List<string>());
                }

                Debug.Assert(key == items[items.Length - 1]);
                temp[folderName].Add(key);
            }

            Dictionary<string, string[]> rets = new Dictionary<string, string[]>();
            foreach (string folderName in temp.Keys)
            {
                rets.Add(folderName, temp[folderName].ToArray());
            }

            return rets;
        }

        /// <summary>
        /// Build file list instance from given file directory,
        /// The value in the dictionary would be sorted by the key.
        /// </summary>
        /// <param name="items">Directory to search, in 2 levels.</param>
        /// <returns>FileListMap.</returns>
        public static FileListMap CreateInstance(Dictionary<string, string> items)
        {
            FileListMap fileList = new FileListMap();
            fileList._map = items;
            return fileList;
        }

        /// <summary>
        /// Build file list instance from given file directory,
        /// The value in the dictionary would be sorted by the key.
        /// </summary>
        /// <param name="targetDir">Directory to search, in 2 levels.</param>
        /// <param name="fileExt">Such as ".wav" or ".txt".</param>
        /// <returns>FileListMap.</returns>
        public static FileListMap CreateInstance(string targetDir,
            string fileExt)
        {
            FileListMap fileList = new FileListMap();
            fileList._map = Build(targetDir, fileExt);

            return fileList;
        }

        /// <summary>
        /// Build filemap file from given  file directory,
        /// The value in the dictionary would be sorted by the key.
        /// </summary>
        /// <param name="targetDir">Directory to search, in 2 levels.</param>
        /// <param name="fileExt">Such as ".wav" or ".txt".</param>
        /// <returns>File list.</returns>
        public static Dictionary<string, string> Build(string targetDir, 
            string fileExt)
        {
            if (string.IsNullOrEmpty(fileExt))
            {
                throw new ArgumentNullException("fileExt");
            }

            if (fileExt != fileExt.ToLower(CultureInfo.CurrentCulture))
            {
                throw new InvalidDataException("only lower case extension supported");
            }

            if (!fileExt.StartsWith(".", StringComparison.CurrentCulture))
            {
                throw new InvalidDataException("extension should start with dot");
            }

            if (!Directory.Exists(targetDir))
            {
                throw Helper.CreateException(typeof(DirectoryNotFoundException),
                    targetDir);
            }

            // verify whether there exist same id
            // if fails, this will throw exception
            Dictionary<string, string> sentenceIdMap = new Dictionary<string, string>();

            // only support two-level folder
            // all subfolders
            SortedDictionary<string, string> sortMap = new SortedDictionary<string, string>();
            foreach (string subfolder in Directory.GetDirectories(targetDir))
            {
                foreach (string file in Directory.GetFiles(subfolder))
                {
                    if (Path.GetExtension(file).ToLower(CultureInfo.CurrentCulture) != fileExt)
                    {
                        continue;
                    }

                    string name = Path.GetFileNameWithoutExtension(file);
                    string folder = Path.GetFileName(subfolder);
                    if (sortMap.ContainsKey(name))
                    {
                        throw new InvalidDataException("duplicated file name detected: " + name);
                    }

                    sortMap.Add(name, folder + "\\" + name);
                }
            }

            // current folder
            foreach (string file in Directory.GetFiles(targetDir))
            {
                string ext = Path.GetExtension(file);
                if (string.IsNullOrEmpty(ext) || ext.ToLower(CultureInfo.CurrentCulture) != fileExt)
                {
                    continue;
                }

                string name = Path.GetFileNameWithoutExtension(file);
                if (sortMap.ContainsKey(name))
                {
                    throw new InvalidDataException("duplicated file name detected: " + name);
                }

                sortMap.Add(name, name);
            }

            foreach (string sid in sortMap.Keys)
            {
                sentenceIdMap.Add(sid, sortMap[sid]);
            }

            return sentenceIdMap;
        }

        /// <summary>
        /// Compare whether fileListA contains fileListB.
        /// </summary>
        /// <param name="fileListA">File list A.</param>
        /// <param name="fileListB">File list B.</param>
        /// <returns>Whether fileListA contains fileListB.</returns>
        public static bool Contains(Dictionary<string, string> fileListA,
            Dictionary<string, string> fileListB)
        {
            bool isContained = true;
            foreach (string sid in fileListB.Keys)
            {
                if (!fileListA.ContainsKey(sid) || fileListA[sid] != fileListB[sid])
                {
                    isContained = false;
                    break;
                }
            }

            return isContained;
        }

        /// <summary>
        /// Compare whether the two filelists are the same.
        /// </summary>
        /// <param name="fileListA">File list A.</param>
        /// <param name="fileListB">File list B.</param>
        /// <returns>Whether the two filelist are the same.</returns>
        public static bool Compare(Dictionary<string, string> fileListA,
            Dictionary<string, string> fileListB)
        {
            return Contains(fileListA, fileListB) && Contains(fileListB, fileListA);
        }

        /// <summary>
        /// Delete sentences contained in dirB from dirA.
        /// </summary>
        /// <param name="dirA">The dir to build file list, from which will delete
        /// the stencnes in dirB.</param>
        /// <param name="fileExtA">The file extension to build file list, from which
        /// will delete the stencnes in dirB.</param>
        /// <param name="dirB">The dir to build file list, sentences in this set
        /// fill be deleted form dirA fileList.</param>
        /// <param name="fileExtB">The file extension to build file list, sentences
        /// in this set fill be deleted form dirA fileList.</param>
        /// <returns>Result map.</returns>
        public static Dictionary<string, string> BuildSubMap(
            string dirA, string fileExtA, string dirB, string fileExtB)
        {
            Dictionary<string, string> mapA = Build(dirA, fileExtA);
            Dictionary<string, string> mapB = Build(dirB, fileExtB);
            BuildSubMap(mapA, mapB);
            return mapA;
        }

        /// <summary>
        /// Delete sentences contained in mapB from mapA.
        /// </summary>
        /// <param name="mapA">Filelist from which will delete the stencnes in dirB.</param>
        /// <param name="mapB">Sentences in this set fill be deleted form dirA fileList.</param>
        public static void BuildSubMap(Dictionary<string, string> mapA,
            Dictionary<string, string> mapB)
        {
            foreach (string sid in mapB.Keys)
            {
                if (mapA.ContainsKey(sid))
                {
                    mapA.Remove(sid);
                }
            }

            return;
        }

        /// <summary>
        /// Build intersection file map, which only includes those items in both directories.
        /// </summary>
        /// <param name="leftDir">Left direcroty.</param>
        /// <param name="rightDir">Right directory.</param>
        /// <param name="fileExt">File extension.</param>
        /// <returns>Map: sentence id to relative path.</returns>
        public static Dictionary<string, string> BuildIntersectionMap(string leftDir,
            string rightDir, string fileExt)
        {
            return BuildIntersectionMap(leftDir, fileExt, rightDir, fileExt);
        }

        /// <summary>
        /// Build intersection file map, which only includes those items in both directories.
        /// </summary>
        /// <param name="leftDir">Left direcroty.</param>
        /// <param name="leftFileExt">Left file extension.</param>
        /// <param name="rightDir">Right directory.</param>
        /// <param name="rightFileExt">Right file extension.</param>
        /// <returns>Map: sentence id to relative path.</returns>
        public static Dictionary<string, string> BuildIntersectionMap(string leftDir, string leftFileExt,
            string rightDir, string rightFileExt)
        {
            Dictionary<string, string> leftFileMap = FileListMap.Build(leftDir, leftFileExt);
            Dictionary<string, string> rightFileMap = FileListMap.Build(rightDir, rightFileExt);

            BuildIntersectionMap(leftFileMap, rightFileMap);

            return leftFileMap;
        }

        /// <summary>
        /// Build intersection map from given two maps,
        /// And un-common items will be removed from the operated map .
        /// </summary>
        /// <param name="operatedMap">First map to operate on and
        /// un-common items will be removed from this instance.</param>
        /// <param name="referenceMap">Second map to operate on, it is used for reference.</param>
        /// <returns>Item id list which is removed from operated map.</returns>
        public static Collection<string> BuildIntersectionMap(IDictionary<string, string> operatedMap,
            IDictionary<string, string> referenceMap)
        {
            Dictionary<string, string> errorDataMap = new Dictionary<string, string>();
            Collection<string> intersectionList = BuildIntersectionMap(operatedMap, referenceMap, errorDataMap);

            if (errorDataMap.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("The follwing files are in different sub-dictionarys[sid] [left dir] [right dir]");
                foreach (string sid in errorDataMap.Keys)
                {
                    sb.AppendLine(Helper.NeutralFormat("    [{0}]    [{1}]    [{2}]",
                        sid, errorDataMap[sid], referenceMap[sid]));
                }

                throw new InvalidDataException(sb.ToString());
            }

            return intersectionList;
        }

        /// <summary>
        /// Build intersection map from given two maps,
        /// And un-common items will be removed from the operated map .
        /// </summary>
        /// <param name="operatedMap">First map to operate on and
        /// un-common items will be removed from this instance.</param>
        /// <param name="referenceMap">Second map to operate on, it is used for reference.</param>
        /// <param name="errorDataMap">If there are the same key but different relative path
        /// In operatedMap and referenceMap, these keys will be saved in errorDataList parameter.
        /// </param>
        /// <returns>Item id list which is removed from operated map.</returns>
        public static Collection<string> BuildIntersectionMap(IDictionary<string, string> operatedMap,
            IDictionary<string, string> referenceMap, Dictionary<string, string> errorDataMap)
        {
            if (operatedMap == null)
            {
                throw new ArgumentNullException("operatedMap");
            }

            if (operatedMap.Keys == null)
            {
                string message = Helper.NeutralFormat("operatedMap.Keys should not be null.");
                throw new ArgumentException(message);
            }

            if (referenceMap == null)
            {
                throw new ArgumentNullException("referenceMap");
            }

            if (errorDataMap == null)
            {
                throw new ArgumentNullException("errorDataMap");
            }

            Collection<string> removedIds = new Collection<string>();

            // remove those items in left dir but nor in right dir
            // If the file is in different directory, should not contained in intersection map.
            // This map is used for path, so it should ignore case.
            foreach (string sid in operatedMap.Keys)
            {
                if (!referenceMap.ContainsKey(sid))
                {
                    removedIds.Add(sid);
                }
                else if (!operatedMap[sid].Equals(referenceMap[sid], StringComparison.OrdinalIgnoreCase))
                {
                    removedIds.Add(sid);
                    errorDataMap.Add(sid, operatedMap[sid]);
                }
            }

            foreach (string sid in removedIds)
            {
                System.Diagnostics.Debug.Assert(operatedMap.ContainsKey(sid));
                operatedMap.Remove(sid);
            }

            return removedIds;
        }

        /// <summary>
        /// Remove given id list from the keyed dictionary.
        /// </summary>
        /// <param name="dictionary">Dictionary to operate on.</param>
        /// <param name="removedList">Id list to remove.</param>
        public static void RemoveItems(IDictionary<string, string> dictionary,
            IEnumerable<string> removedList)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException("dictionary");
            }

            if (removedList == null)
            {
                throw new ArgumentNullException("removedList");
            }

            foreach (string id in removedList)
            {
                if (dictionary.ContainsKey(id))
                {
                    dictionary.Remove(id);
                }
            }
        }

        /// <summary>
        /// According to FileListMap file, group/move files
        /// Under fileDir with extension fileExt to sub directory.
        /// </summary>
        /// <param name="fileDir">Target file directory.</param>
        /// <param name="fileExt">File extension to process.</param>
        /// <param name="targetDir">Target directory to save grouped files.</param>
        /// <param name="mapFilePath">According map file.</param>
        /// <param name="writeOutToConsole">Whether to write out to console.</param>
        /// <param name="copy">Whether to copy or move file to new location.</param>
        public static void GroupFileIntoSubDir(string fileDir, string fileExt,
            string targetDir, string mapFilePath, bool writeOutToConsole, bool copy)
        {
            Dictionary<string, string> filemap = ReadAllData(mapFilePath);

            Collection<string> notExistFiles = new Collection<string>();

            foreach (string id in filemap.Keys)
            {
                string sourceFilePath = Path.Combine(fileDir, id) + fileExt;
                string targetFilePath = Path.Combine(targetDir, filemap[id]) + fileExt;
                if (File.Exists(sourceFilePath))
                {
                    string subdir = Path.GetDirectoryName(targetFilePath);
                    Helper.EnsureFolderExist(subdir);
                    if (copy)
                    {
                        File.Copy(sourceFilePath, targetFilePath, true);
                    }
                    else
                    {
                        Helper.SafeDelete(targetFilePath);
                        File.Move(sourceFilePath, targetFilePath);
                    }
                }
                else
                {
                    if (writeOutToConsole)
                    {
                        notExistFiles.Add(id);
                    }
                }
            }

            if (writeOutToConsole && notExistFiles.Count > 0)
            {
                Console.WriteLine("The following sentences are not found in dir :" + fileDir);
                foreach (string id in notExistFiles)
                {
                    Console.WriteLine(id);
                }
            }
        }

        /// <summary>
        /// Sentence id => relative location.
        /// </summary>
        /// <param name="mapFilePath">File list map file to read.</param>
        /// <param name="sort">Whether sort by key.</param>
        /// <returns>File list map.</returns>
        public static Dictionary<string, string> ReadAllData(string mapFilePath, bool sort)
        {
            if (string.IsNullOrEmpty(mapFilePath))
            {
                throw new ArgumentNullException("mapFilePath");
            }

            if (!File.Exists(mapFilePath))
            {
                throw Helper.CreateException(typeof(FileNotFoundException),
                    mapFilePath);
            }

            SortedDictionary<string, string> sortedMap = new SortedDictionary<string, string>();
            Dictionary<string, string> map = new Dictionary<string, string>();

            using (StreamReader sr = new StreamReader(mapFilePath))
            {
                string line = null;
                while ((line = sr.ReadLine()) != null)
                {
                    string[] items = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (items.Length != 2)
                    {
                        throw new InvalidDataException("Invalid format in file:"
                            + mapFilePath + ", with line " + line);
                    }

                    if (sort && !sortedMap.ContainsKey(items[0]))
                    {
                        sortedMap.Add(items[0], items[1]);
                    }
                    else if (!sort && !map.ContainsKey(items[0]))
                    {
                        map.Add(items[0], items[1]);
                    }
                    else
                    {
                        Console.WriteLine(items[0] + "exist");
                    }
                }
            }

            Dictionary<string, string> retMap = new Dictionary<string, string>();
            if (sort)
            {
                foreach (string sid in sortedMap.Keys)
                {
                    retMap.Add(sid, sortedMap[sid]);
                }
            }

            return sort ? retMap : map;
        }

        /// <summary>
        /// Sentence id => relative location.
        /// </summary>
        /// <param name="mapFilePath">File list map file to read.</param>
        /// <returns>File list map.</returns>
        public static Dictionary<string, string> ReadAllData(string mapFilePath)
        {
            if (string.IsNullOrEmpty(mapFilePath))
            {
                throw new ArgumentNullException("mapFilePath");
            }

            if (!File.Exists(mapFilePath))
            {
                throw Helper.CreateException(typeof(FileNotFoundException),
                    mapFilePath);
            }

            return ReadAllData(mapFilePath, true);
        }

        /// <summary>
        /// Write all data into file list file.
        /// </summary>
        /// <param name="fileMaps">File list.</param>
        /// <param name="filePath">Target file to save.</param>
        public static void WriteAllData(Dictionary<string, string> fileMaps,
            string filePath)
        {
            WriteAllData(fileMaps, filePath, -1);
        }

        /// <summary>
        /// Save filelist map to file.
        /// </summary>
        /// <param name="fileMaps">File list map.</param>
        /// <param name="filePath">Target file path to save.</param>
        /// <param name="maxEntryNumber">Maximum entry count to save.</param>
        public static void WriteAllData(Dictionary<string, string> fileMaps,
            string filePath, int maxEntryNumber)
        {
            if (fileMaps == null)
            {
                throw new ArgumentNullException("fileMaps");
            }

            if (fileMaps.Keys == null)
            {
                throw new ArgumentException("fileMaps.Keys is null");
            }

            Helper.EnsureFolderExistForFile(filePath);
            using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.ASCII))
            {
                int count = 0;
                foreach (string id in fileMaps.Keys)
                {
                    sw.WriteLine("{0} {1}", id, fileMaps[id]);
                    count++;
                    if (maxEntryNumber != -1 && maxEntryNumber < count)
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Remove error sentence from map file.
        /// </summary>
        /// <param name="errorSet">Data error set.</param>
        /// <param name="mapFilePath">File list map file path.</param>
        public static void RemoveErrorSentence(DataErrorSet errorSet, string mapFilePath)
        {
            if (errorSet == null)
            {
                throw new ArgumentNullException("errorSet");
            }

            if (errorSet.Errors == null)
            {
                throw new ArgumentException("errorSet.Errors is null");
            }

            Dictionary<string, string> map = FileListMap.ReadAllData(mapFilePath);
            foreach (DataError error in errorSet.Errors)
            {
                if (string.IsNullOrEmpty(error.SentenceId))
                {
                    continue;
                }

                if (map.ContainsKey(error.SentenceId))
                {
                    map.Remove(error.SentenceId);
                }
            }

            FileListMap.WriteAllData(map, mapFilePath);
        }

        /// <summary>
        /// Remove error sentence from map file.
        /// </summary>
        /// <param name="errorSet">Data error set.</param>
        /// <param name="mapFilePath">File list map file path.</param>
        public static void RemoveErrorSentence(ErrorSet errorSet, string mapFilePath)
        {
            if (errorSet == null)
            {
                throw new ArgumentNullException("errorSet");
            }

            if (string.IsNullOrEmpty(mapFilePath))
            {
                throw new ArgumentNullException("mapFilePath");
            }

            Dictionary<string, string> map = FileListMap.ReadAllData(mapFilePath);
            foreach (string id in ScriptHelper.GetNeedDeleteItemIds(errorSet))
            {
                if (map.ContainsKey(id))
                {
                    map.Remove(id);
                }
            }

            FileListMap.WriteAllData(map, mapFilePath);
        }

        /// <summary>
        /// Check the file map is invalid or not.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>Bool.</returns>
        public static bool IsValidFileListMap(string filePath)
        {
            bool isValidFileListMap = true;
            FileListMap fileListMap = new FileListMap();
            if (string.IsNullOrEmpty(filePath))
            {
                isValidFileListMap = false;
            }
            else
            {
                try
                {
                    fileListMap.Load(filePath);
                }
                catch
                {
                    isValidFileListMap = false;
                }
            }

            return isValidFileListMap;
        }

        /// <summary>
        /// Merge source file list to target file list.
        /// </summary>
        /// <param name="sourceFileListPath">SourceFileListPath.</param>
        /// <param name="targetFileListPath">TargetFileListPath.</param>
        /// <param name="removeNotExists">RemoveNotExists.</param>
        public static void Merge(string sourceFileListPath, string targetFileListPath,
            bool removeNotExists)
        {
            if (!File.Exists(targetFileListPath))
            {
                Helper.ThrowIfFileNotExist(sourceFileListPath);
                Helper.ForceCopyFile(sourceFileListPath, targetFileListPath);
                Helper.SetFileReadOnly(targetFileListPath, false);
            }
            else
            {
                Dictionary<string, string> targetFilelist = FileListMap.ReadAllData(targetFileListPath, false);
                Dictionary<string, string> sourceFilelist = FileListMap.ReadAllData(sourceFileListPath, false);
                if (removeNotExists)
                {
                    Collection<string> removedIds = new Collection<string>();
                    targetFilelist.Keys.ForEach(sid =>
                        {
                            if (!sourceFilelist.ContainsKey(sid))
                            {
                                removedIds.Add(sid);
                            }
                        });

                    removedIds.ForEach(sid => targetFilelist.Remove(sid));
                }

                sourceFilelist.ForEach(pair => targetFilelist[pair.Key] = pair.Value);
                FileListMap.WriteAllData(targetFilelist, targetFileListPath);
            }
        }

        #endregion

        #region Operations

        /// <summary>
        /// Build file path according to the given information.
        /// </summary>
        /// <param name="map">The given map. If it is null, the sentence id will be used directly.</param>
        /// <param name="folder">The given folder.</param>
        /// <param name="sid">Sentence id.</param>
        /// <param name="extension">Extension name.</param>
        /// <returns>File path.</returns>
        public static string BuildPath(FileListMap map, string folder, string sid, string extension)
        {
            if (string.IsNullOrEmpty(folder))
            {
                throw new ArgumentNullException("folder");
            }

            if (string.IsNullOrEmpty(sid))
            {
                throw new ArgumentNullException("sid");
            }

            // Here, extension can be null or emtpy.
            return map == null
                ? Path.Combine(folder, sid.AppendExtensionName(extension))
                : Path.Combine(folder, map.Map[sid].AppendExtensionName(extension));
        }

        /// <summary>
        /// Remove given id list from file map.
        /// </summary>
        /// <param name="removedList">Id list to remove.</param>
        public void RemoveItems(IEnumerable<string> removedList)
        {
            if (removedList == null)
            {
                throw new ArgumentNullException("removedList");
            }

            foreach (string id in removedList)
            {
                if (_map.ContainsKey(id))
                {
                    _map.Remove(id);
                }
            }
        }

        /// <summary>
        /// Load file list from file.
        /// </summary>
        /// <param name="filePath">File to load from.</param>
        public void Load(string filePath)
        {
            _map = ReadAllData(filePath);
            _filePath = filePath;
        }

        /// <summary>
        /// Save file list into file.
        /// </summary>
        /// <param name="filePath">Target file to save to.</param>
        public void Save(string filePath)
        {
            WriteAllData(Map, filePath);
        }

        /// <summary>
        /// Build file path.
        /// </summary>
        /// <param name="directory">Directory.</param>
        /// <param name="id">Sentence id.</param>
        /// <param name="extension">Extension name.</param>
        /// <returns>File path.</returns>
        public string BuildPath(string directory, string id, string extension)
        {
            return Path.Combine(directory, Map[id].AppendExtensionName(extension));
        }

        #endregion
    }
}