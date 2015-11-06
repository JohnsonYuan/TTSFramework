//----------------------------------------------------------------------------
// <copyright file="CmpSetSerializer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements cmp set serializer class
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Research
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Globalization;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Information of the cmp feature set.
    /// </summary>
    public struct CmpSetInfo
    {
        /// <summary>
        /// Sample period in milliseconds.
        /// </summary>
        public int SamplePeriod;

        /// <summary>
        /// Size of each record in bytes.
        /// </summary>
        public int RecordSize;

        /// <summary>
        /// Lsp order.
        /// </summary>
        public int LspOrder;

        /// <summary>
        /// Mbe order.
        /// </summary>
        public int MbeOrder;

        /// <summary>
        /// Power order.
        /// </summary>
        public int PowerOrder;

        /// <summary>
        /// Guidance Lsp order.
        /// </summary>
        public int GuidanceLspOrder;
        
        /// <summary>
        /// Number of sentences.
        /// </summary>
        public int SentenceCount;
    }

    /// <summary>
    /// Information of the the cmp feature set for a single sentence.
    /// </summary>
    public struct SingleCmpInfo
    {
        /// <summary>
        /// Sentence ID.
        /// </summary>
        public string SentID;

        /// <summary>
        /// Number of samples for the sentence.
        /// </summary>
        public int SampleCount;

        /// <summary>
        /// Index into the all-in-one cmp file.
        /// </summary>
        public int IndexIntoFile;
    }

    /// <summary>
    /// Cmp set serializer class.
    /// </summary>
    public class CmpSetSerializer : IDisposable
    {
        #region fields

        /// <summary>
        /// The loading environment.
        /// </summary>
        private LoadingEnv _loadingEnv = new LoadingEnv();

        /// <summary>
        /// Track whether Dispose has been called.
        /// </summary>
        private bool _disposed = false;

        #endregion

        // Use C# destructor syntax for finalization code.
        // This destructor will run only if the Dispose method
        // does not get called.
        // It gives your base class the opportunity to finalize.
        // Do not provide destructors in types derived from this class.

        /// <summary>
        /// Finalizes an instance of the <see cref="CmpSetSerializer"/> class.
        /// </summary>
        ~CmpSetSerializer()
        {
            // Do not re-create Dispose clean-up code here.
            // Calling Dispose(false) is optimal in terms of
            // readability and maintainability.
            Dispose(false);
        }

        #region properties

        /// <summary>
        /// Gets Cmp set info.
        /// </summary>
        public CmpSetInfo CmpSetInfo
        {
            get
            {
                if (string.IsNullOrEmpty(_loadingEnv.CmpFileName))
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Attempt to get CmpSetInfo is forbidden if no cmp file is open.");
                    throw new InvalidOperationException(message);
                }

                return _loadingEnv.CmpSetInfo;
            }
        }

        /// <summary>
        /// Gets float cmp info dictionary.
        /// </summary>
        public Dictionary<string, SingleCmpInfo> SingleCmpInfoDict
        {
            get
            {
                if (string.IsNullOrEmpty(_loadingEnv.CmpFileName))
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Attempt to get SingleCmpInfo dictionary is forbidden if no cmp file is open.");
                    throw new InvalidOperationException(message);
                }

                return _loadingEnv.CmpInfoDict;
            }
        }

        #endregion

        #region public static methods

        /// <summary>
        /// Compile an all-in-one cmp set file from a set of separate cmp files.
        /// </summary>
        /// <param name="workingPath">Working path.</param>
        /// <param name="fileMapName">File map name.</param>
        /// <param name="resultCmpFile">Name of the all-in-one cmp file.</param>
        /// <param name="cmpSetInfo">Information of the cmp set.</param>
        /// <returns>Success or failure.</returns>
        public static bool CompileCmpFiles(string workingPath, string fileMapName,
            string resultCmpFile, ref CmpSetInfo cmpSetInfo)
        {
            SavingEnv savingEnv = new SavingEnv();
            savingEnv.WorkingPath = workingPath;
            savingEnv.FileMapName = fileMapName;
            savingEnv.ResultCmpFile = resultCmpFile;

            ParseSentPaths(savingEnv);

            GetCmpSetInfo(savingEnv, ref cmpSetInfo);

            GetCmpInfoDict(savingEnv);

            SaveAllInOneFile(savingEnv, cmpSetInfo);

            return true;
        }

        #endregion

        #region IDisposalbe

        // Implement IDisposable.
        // Do not make this method virtual.
        // A derived class should not be able to override this method.
        
        /// <summary>
        /// Dispose routine.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);

            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        #endregion

        #region public methods

        /// <summary>
        /// Open the all-in-one cmp set file.
        /// </summary>
        /// <param name="cmpFile">Name of the cmp file.</param>
        /// <param name="cmpSetInfo">Information of the cmp set.</param>
        /// <returns>Success or failure.</returns>
        public bool OpenCmpFile(string cmpFile, ref CmpSetInfo cmpSetInfo)
        {
            bool result = true;

            CloseCmpFile();

            if (File.Exists(cmpFile))
            {
                _loadingEnv.CmpFileName = cmpFile;
                _loadingEnv.CmpFileStream = File.Open(_loadingEnv.CmpFileName, FileMode.Open, FileAccess.Read);
                _loadingEnv.BinaryReader = new BinaryReader(_loadingEnv.CmpFileStream);

                BinaryReader reader = _loadingEnv.BinaryReader;

                cmpSetInfo.SamplePeriod = reader.ReadInt32();
                cmpSetInfo.RecordSize = reader.ReadInt32();
                cmpSetInfo.LspOrder = reader.ReadInt32();
                cmpSetInfo.MbeOrder = reader.ReadInt32();
                cmpSetInfo.PowerOrder = reader.ReadInt32();
                cmpSetInfo.GuidanceLspOrder = reader.ReadInt32();
                cmpSetInfo.SentenceCount = reader.ReadInt32();

                _loadingEnv.CmpSetInfo = cmpSetInfo;

                for (int i = 0; i < cmpSetInfo.SentenceCount; i++)
                {
                    SingleCmpInfo singleCmpInfo;

                    singleCmpInfo.SentID = reader.ReadString();
                    singleCmpInfo.SampleCount = reader.ReadInt32();
                    singleCmpInfo.IndexIntoFile = reader.ReadInt32();

                    _loadingEnv.CmpInfoDict.Add(singleCmpInfo.SentID, singleCmpInfo);
                }

                result = true;
            }
            else
            {
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Extract cmp features in the interval given for a certain sentence.
        /// The features are extracted in a adjusted interval since cmp feature is sampled discretely.
        /// Time are given in milliseconds.
        /// </summary>
        /// <param name="sentenceID">Sentence ID.</param>
        /// <param name="intervalBeg">Beginning of the interval.</param>
        /// <param name="intervalEnd">End of the interval.</param>
        /// <param name="adjustBeg">Ajdusted beginning of the interval.</param>
        /// <param name="adjustEnd">Ajdusted end of the interval.</param>
        /// <returns>Cmp features.</returns>
        public List<CmpFeature> ExtractCmpFeatures(string sentenceID, float intervalBeg, float intervalEnd,
            out float adjustBeg, out float adjustEnd)
        {
            if (string.IsNullOrEmpty(_loadingEnv.CmpFileName))
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Attempt to extraction cmp features is forbidden if no cmp file is open.");
                throw new InvalidOperationException(message);
            }

            if (intervalBeg < 0)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "intervalBeg ({0}) should be non-negative.", intervalBeg);
                throw new ArgumentOutOfRangeException(message);
            }

            if (intervalBeg >= intervalEnd)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "intervalBeg ({0}) should be less than intervalEnd ({1}).", intervalBeg, intervalEnd);
                throw new ArgumentOutOfRangeException(message);
            }

            if (!_loadingEnv.CmpInfoDict.ContainsKey(sentenceID))
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "sentenceID ({0}) doesn't appear in the cmp file.", sentenceID);
                throw new InvalidDataException(message);
            }

            SingleCmpInfo singleCmpInfo = _loadingEnv.CmpInfoDict[sentenceID];
            CmpSetInfo cmpSetInfo = _loadingEnv.CmpSetInfo;

            int beg = (int)Math.Round(intervalBeg / cmpSetInfo.SamplePeriod);
            int end = (int)Math.Round(intervalEnd / cmpSetInfo.SamplePeriod);

            if (beg >= singleCmpInfo.SampleCount)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "intervalBeg ({0}) is out of range of the sentence ({1}).", intervalBeg, sentenceID);
                throw new InvalidDataException(message);
            }

            if (end > singleCmpInfo.SampleCount)
            {
                end = singleCmpInfo.SampleCount;
            }

            if (beg == end)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "intervalBeg ({0}) and intervalEnd ({1}) contains 0 frame of the sentence ({2}).", intervalBeg, intervalEnd, sentenceID);
                throw new InvalidDataException(message);
            }

            adjustBeg = beg * cmpSetInfo.SamplePeriod;
            adjustEnd = end * cmpSetInfo.SamplePeriod;

            List<CmpFeature> cmpFeatures = new List<CmpFeature>(end - beg);

            int offset = singleCmpInfo.IndexIntoFile + (beg * cmpSetInfo.RecordSize);

            _loadingEnv.CmpFileStream.Seek(offset, SeekOrigin.Begin);
            for (int i = beg; i < end; i++)
            {
                cmpFeatures.Add(CmpFeature.ParseCmpFeature(_loadingEnv.BinaryReader, cmpSetInfo.RecordSize, cmpSetInfo.LspOrder, cmpSetInfo.MbeOrder, cmpSetInfo.PowerOrder, cmpSetInfo.GuidanceLspOrder));
            }

            return cmpFeatures;
        }

        /// <summary>
        /// Extract cmp features in the interval given for a certain sentence.
        /// The features are extracted in a adjusted interval since cmp feature is sampled discretely.
        /// Time are given in milliseconds.
        /// </summary>
        /// <param name="sentenceID">Sentence ID.</param>
        /// <param name="frameBegin">Beginning frame id of the interval(unit for example) in the sentence.</param>
        /// <param name="frameEnd">End frame id of the interval(unit for example) in the sentence.</param>
        /// <returns>Cmp features.</returns>
        public List<CmpFeature> ExtractCmpFeatures(string sentenceID, int frameBegin, int frameEnd)
        {
            CmpSetInfo cmpSetInfo = _loadingEnv.CmpSetInfo;
            float intervalBeigin = frameBegin * cmpSetInfo.SamplePeriod;
            float intervalend = frameEnd * cmpSetInfo.SamplePeriod;
            float outBegin = 0;
            float outEnd = 0;
            return ExtractCmpFeatures(sentenceID, intervalBeigin, intervalend, out outBegin, out outEnd);
        }

        /// <summary>
        /// Close the all-in-one cmp set file.
        /// </summary>
        /// <returns>Success or failure.</returns>
        public bool CloseCmpFile()
        {
            if (!string.IsNullOrEmpty(_loadingEnv.CmpFileName))
            {
                _loadingEnv.CmpFileName = string.Empty;
            }

            if (_loadingEnv.BinaryReader != null)
            {
                _loadingEnv.BinaryReader.Close();
                ((IDisposable)_loadingEnv.BinaryReader).Dispose();

                _loadingEnv.BinaryReader = null;
            }

            if (_loadingEnv.CmpFileStream != null)
            {
                _loadingEnv.CmpFileStream.Close();
                _loadingEnv.CmpFileStream.Dispose();

                _loadingEnv.CmpFileStream = null;
            }

            _loadingEnv.CmpInfoDict.Clear();

            return true;
        }

        #endregion

        #region private static methods

        /// <summary>
        /// Get the full path of a cmp file.
        /// </summary>
        /// <param name="workingPath">Working path.</param>
        /// <param name="partialPath">Partial path (file name chunk).</param>
        /// <returns>String.</returns>
        private static string FormCmpFullPath(string workingPath, string partialPath)
        {
            const string FileSuffix = ".cmp";
            return Path.Combine(workingPath, partialPath + FileSuffix);
        }

        /// <summary>
        /// Parse sentence IDs and paths.
        /// </summary>
        /// <param name="savingEnv">Working environment.</param>
        private static void ParseSentPaths(SavingEnv savingEnv)
        {
            using (StreamReader reader = new StreamReader(savingEnv.FileMapName))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    string[] tokens = line.Split(new char[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    if (tokens.Length == 2)
                    {
                        string id = tokens[0];
                        string path = tokens[1];
                        if (System.IO.File.Exists(FormCmpFullPath(savingEnv.WorkingPath, path)))
                        {
                            savingEnv.SentIDs.Add(id);
                            savingEnv.SentPaths.Add(path);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get general information of the cmp set.
        /// </summary>
        /// <param name="savingEnv">Working environment.</param>
        /// <param name="cmpSetInfo">Information of the cmp set.</param>
        private static void GetCmpSetInfo(SavingEnv savingEnv, ref CmpSetInfo cmpSetInfo)
        {
            cmpSetInfo.SentenceCount = savingEnv.SentIDs.Count;

            if (cmpSetInfo.SentenceCount > 0)
            {
                FileStream file = File.Open(FormCmpFullPath(savingEnv.WorkingPath, savingEnv.SentPaths[0]), FileMode.Open);
                try
                {
                    using (BinaryReader reader = new BinaryReader(file))
                    {
                        file = null;
                        reader.BaseStream.Seek(sizeof(int), SeekOrigin.Begin);    // number of samples in file

                        int samplePeriod = reader.ReadInt32();        // sample period in 100ns units
                        cmpSetInfo.SamplePeriod = samplePeriod / 10000;  // make it in milliseconds

                        short sampleSize = reader.ReadInt16();  // number of bytes per sample
                        cmpSetInfo.RecordSize = sampleSize;

                        int gainAndF0Order = 2;
                        int mbeOrder = cmpSetInfo.MbeOrder;
                        int powerOrder = cmpSetInfo.PowerOrder;
                        int guidanceLspOrder = cmpSetInfo.GuidanceLspOrder;
                        int placeHolder = gainAndF0Order + mbeOrder + powerOrder + guidanceLspOrder;

                        cmpSetInfo.LspOrder = (sampleSize / (3 * sizeof(float))) - placeHolder;
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
        }

        /// <summary>
        /// Get information of all the single cmp files.
        /// </summary>
        /// <param name="savingEnv">Working environment.</param>
        private static void GetCmpInfoDict(SavingEnv savingEnv)
        {
            for (int i = 0; i < savingEnv.SentPaths.Count; i++)
            {
                string sentID = savingEnv.SentIDs[i];
                string partialPath = savingEnv.SentPaths[i];

                FileStream file = File.Open(FormCmpFullPath(savingEnv.WorkingPath, partialPath), FileMode.Open);
                try
                {
                    using (BinaryReader reader = new BinaryReader(file))
                    {
                        file = null;
                        int sampleCount = reader.ReadInt32();   // number of samples in file

                        SingleCmpInfo singleCmpInfo = new SingleCmpInfo();
                        singleCmpInfo.SentID = sentID;
                        singleCmpInfo.SampleCount = sampleCount;

                        savingEnv.CmpInfoDict.Add(sentID, singleCmpInfo);
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
        }

        /// <summary>
        /// Copy all data from current position in reader to writer.
        /// </summary>
        /// <param name="reader">Binary reader.</param>
        /// <param name="writer">Binary writer.</param>
        private static void CopyStreamData(BinaryReader reader, BinaryWriter writer)
        {
            const int ArrayLength = 0x10000;

            while (true)
            {
                byte[] arrayRead = reader.ReadBytes(ArrayLength);
                int arraySize = arrayRead.GetLength(0);

                if (arraySize == 0)
                {
                    break;
                }

                writer.Write(arrayRead, 0, arraySize);
            }
        }

        /// <summary>
        /// Save the all-in-one cmp file.
        /// </summary>
        /// <param name="savingEnv">Working environment.</param>
        /// <param name="cmpSetInfo">Information of the cmp set.</param>
        private static void SaveAllInOneFile(SavingEnv savingEnv, CmpSetInfo cmpSetInfo)
        {
            FileStream stream = File.Open(savingEnv.ResultCmpFile, FileMode.Create);
            try
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    stream = null;
                    writer.Write(cmpSetInfo.SamplePeriod);
                    writer.Write(cmpSetInfo.RecordSize);
                    writer.Write(cmpSetInfo.LspOrder);
                    writer.Write(cmpSetInfo.MbeOrder);
                    writer.Write(cmpSetInfo.PowerOrder);
                    writer.Write(cmpSetInfo.GuidanceLspOrder);
                    writer.Write(cmpSetInfo.SentenceCount);

                    for (int i = 0; i < savingEnv.CmpInfoDict.Count; i++)
                    {
                        SingleCmpInfo singleCmpInfo = savingEnv.CmpInfoDict.Values.ElementAt(i);

                        writer.Write(singleCmpInfo.SentID);
                        writer.Write(singleCmpInfo.SampleCount);

                        // temporarily use indexIntoFile to denotes the position of itself in file
                        singleCmpInfo.IndexIntoFile = (int)writer.BaseStream.Position;
                        writer.Write(singleCmpInfo.IndexIntoFile);

                        savingEnv.CmpInfoDict[savingEnv.CmpInfoDict.Keys.ElementAt(i)] = singleCmpInfo;
                    }

                    int offset = (int)writer.BaseStream.Position;

                    foreach (KeyValuePair<string, SingleCmpInfo> kvp in savingEnv.CmpInfoDict)
                    {
                        SingleCmpInfo singleCmpInfo = kvp.Value;

                        writer.Seek(singleCmpInfo.IndexIntoFile, SeekOrigin.Begin);
                        singleCmpInfo.IndexIntoFile = offset;
                        writer.Write(singleCmpInfo.IndexIntoFile);

                        offset += cmpSetInfo.RecordSize * singleCmpInfo.SampleCount;
                    }

                    writer.Seek(0, SeekOrigin.End);

                    const int SingleCmpHeadSize = 12;

                    for (int i = 0; i < savingEnv.SentPaths.Count; i++)
                    {
                        string sentID = savingEnv.SentIDs[i];
                        string partialPath = savingEnv.SentPaths[i];

                        FileStream file = File.Open(FormCmpFullPath(savingEnv.WorkingPath, partialPath), FileMode.Open);
                        try
                        {
                            using (BinaryReader reader = new BinaryReader(file))
                            {
                                file = null;
                                reader.BaseStream.Seek(SingleCmpHeadSize, SeekOrigin.Begin);

                                CopyStreamData(reader, writer);
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
                }
            }
            finally
            {
                if (null != stream)
                {
                    stream.Dispose();
                }
            }
        }

        #endregion

        #region private methods

        // Dispose(bool disposing) executes in two distinct scenarios.
        // If disposing equals true, the method has been called directly
        // or indirectly by a user's code. Managed and unmanaged resources
        // can be disposed.
        // If disposing equals false, the method has been called by the
        // runtime from inside the finalizer and you should not reference
        // other objects. Only unmanaged resources can be disposed.
        
        /// <summary>
        /// Do the dispose work here.
        /// </summary>
        /// <param name="disposing">Whether the functions is called by user's code (true), or by finalizer (false).</param>
        private void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!_disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                }

                // Call the appropriate methods to clean up
                // unmanaged resources here.
                // If disposing is false,
                // only the following code is executed.
                CloseCmpFile();

                // Note disposing has been done.
                _disposed = true;
            }
        }

        #endregion

        #region private classes

        private class SavingEnv
        {
            #region fields

            /// <summary>
            /// Working path of the saving task.
            /// </summary>
            private string _workingPath;

            /// <summary>
            /// File map name.
            /// </summary>
            private string _fileMapName;

            /// <summary>
            /// Resultant all-in-one file name.
            /// </summary>
            private string _resultCmpFile;

            /// <summary>
            /// Sentence IDs.
            /// </summary>
            private StringCollection _sentIDs = new StringCollection();

            /// <summary>
            /// Partial path of sentence cmp files.
            /// </summary>
            private StringCollection _sentPaths = new StringCollection();

            /// <summary>
            /// All cmp single information set.
            /// </summary>
            private Dictionary<string, SingleCmpInfo> _cmpInfoDict = new Dictionary<string, SingleCmpInfo>();

            #endregion

            #region properties

            /// <summary>
            /// Gets or sets the woring path.
            /// </summary>
            public string WorkingPath
            {
                get
                {
                    return _workingPath;
                }

                set
                {
                    _workingPath = value;
                }
            }

            /// <summary>
            /// Gets or sets the file map name.
            /// </summary>
            public string FileMapName
            {
                get
                {
                    return _fileMapName;
                }

                set
                {
                    _fileMapName = value;
                }
            }

            /// <summary>
            /// Gets or sets the resultant all-in-one file name.
            /// </summary>
            public string ResultCmpFile
            {
                get
                {
                    return _resultCmpFile;
                }

                set
                {
                    _resultCmpFile = value;
                }
            }

            /// <summary>
            /// Gets the sentence IDs.
            /// </summary>
            public StringCollection SentIDs
            {
                get
                {
                    return _sentIDs;
                }
            }

            /// <summary>
            /// Gets the (partial) sentence paths.
            /// </summary>
            public StringCollection SentPaths
            {
                get
                {
                    return _sentPaths;
                }
            }

            /// <summary>
            /// Gets the single cmp information set.
            /// </summary>
            public Dictionary<string, SingleCmpInfo> CmpInfoDict
            {
                get
                {
                    return _cmpInfoDict;
                }
            }

            #endregion
        }

        private class LoadingEnv
        {
            #region fields

            /// <summary>
            /// The all-in-one cmp file name.
            /// </summary>
            private string _cmpFileName;

            /// <summary>
            /// Cmp file stream.
            /// </summary>
            private FileStream _cmpFileStream;

            /// <summary>
            /// Binary reader of the file stream.
            /// </summary>
            private BinaryReader _binaryReader;

            /// <summary>
            /// Cmp set info.
            /// </summary>
            private CmpSetInfo _cmpSetInfo = new CmpSetInfo();

            /// <summary>
            /// All cmp single information set.
            /// </summary>
            private Dictionary<string, SingleCmpInfo> _cmpInfoDict = new Dictionary<string, SingleCmpInfo>();

            #endregion

            #region properties

            /// <summary>
            /// Gets or sets the all-in-one cmp file.
            /// </summary>
            public string CmpFileName
            {
                get
                {
                    return _cmpFileName;
                }

                set
                {
                    _cmpFileName = value;
                }
            }

            /// <summary>
            /// Gets or sets the cmp file stream.
            /// </summary>
            public FileStream CmpFileStream
            {
                get
                {
                    return _cmpFileStream;
                }

                set
                {
                    _cmpFileStream = value;
                }
            }

            /// <summary>
            /// Gets or sets the binary reader of the file stream.
            /// </summary>
            public BinaryReader BinaryReader
            {
                get
                {
                    return _binaryReader;
                }

                set
                {
                    _binaryReader = value;
                }
            }

            /// <summary>
            /// Gets or sets the cmp set info.
            /// </summary>
            public CmpSetInfo CmpSetInfo
            {
                get
                {
                    return _cmpSetInfo;
                }

                set
                {
                    _cmpSetInfo = value;
                }
            }

            /// <summary>
            /// Gets the single cmp information set.
            /// </summary>
            public Dictionary<string, SingleCmpInfo> CmpInfoDict
            {
                get
                {
                    return _cmpInfoDict;
                }
            }

            #endregion
        }

        #endregion
    }
}