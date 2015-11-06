//----------------------------------------------------------------------------
// <copyright file="DomainIndexFile.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements domain index file class
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Config;
    using Microsoft.Tts.Offline.Interop;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Domain index file header serialization data block.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct DomainIndexFileHeaderSerial
    {
        #region Fields

        internal uint Tag;                // Tag: "NUM ", "NAM " etc
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        internal uint Size;               // Length in byte of data followed by this field of UNT file
        internal uint VersionNumber;      // Version Number
        internal uint BuildNumber;        // Build Number
        internal uint LanguageId;         // Lanuage Id
        internal int HashTableItemCount;  // Total count of hash table item
        internal int HashTableOffset;     // Offset of hash table chunck
        internal int HashTableSize;       // Size of hash table chunck (in byte)
        internal int FeatureDataOffset;   // Offset of feature data chunck
        internal int FeatureDataSize;     // Size of feature data chunck (in byte)
        internal int StringPoolOffset;    // Offset of string pool
        internal int StringPoolSize;      // Sizde of string pool (in byte)

        #endregion

        #region Operations

        /// <summary>
        /// Read a DomainIndexFileHeaderSerial block from bindary stream.
        /// </summary>
        /// <param name="br">Binary reader to read data for domain index file.</param>
        /// <returns>Domain index file header serial.</returns>
        public static DomainIndexFileHeaderSerial Read(BinaryReader br)
        {
            int size = Marshal.SizeOf(typeof(DomainIndexFileHeaderSerial));
            byte[] buff = br.ReadBytes(size);

            if (buff.Length != size)
            {
                string message = Helper.NeutralFormat("Malformed data found, " +
                    "for there is no enough data for unit header.");
                throw new InvalidDataException(message);
            }

            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(size);
                Marshal.Copy(buff, 0, ptr, size);
                return (DomainIndexFileHeaderSerial)Marshal.PtrToStructure(ptr,
                    typeof(DomainIndexFileHeaderSerial));
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        /// <summary>
        /// Converts this instance into byte array.
        /// </summary>
        /// <returns>Byte array presenting this instance.</returns>
        public byte[] ToBytes()
        {
            byte[] buff = new byte[Marshal.SizeOf(typeof(DomainIndexFileHeaderSerial))];

            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(buff.Length);
                Marshal.StructureToPtr(this, ptr, false);
                Marshal.Copy(ptr, buff, 0, buff.Length);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }

            return buff;
        }

        #endregion
    }

    /// <summary>
    /// Feature data item in feature data chunck.
    /// </summary>
    public class FeatureDataItem
    {
        #region Fields

        private List<int> _unitIndexes = new List<int>();

        #endregion

        #region Properties

        /// <summary>
        /// Gets Unit index.
        /// </summary>
        public List<int> UnitIndexes
        {
            get { return _unitIndexes; }
        }

        /// <summary>
        /// Gets Binary size.
        /// </summary>
        public int BinarySize
        {
            get
            {
                // Unit Type ID (ushort) and Index Count (ushort)
                int size = 2 + 2;
                size += 4 * UnitIndexes.Count;
                return size;
            }
        }

        #endregion
    }

    /// <summary>
    /// Domain index item.
    /// </summary>
    public class DomainIndexItem
    {
        #region Fields

        private string _word;
        private int _featureDataOffset;
        private int _stringPoolOffset;
        private int _nextItemIndex;

        private Collection<FeatureDataItem> _featureItems = new Collection<FeatureDataItem>();

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Word text.
        /// </summary>
        public string Word
        {
            get
            {
                return _word;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _word = value;
            }
        }

        /// <summary>
        /// Gets or sets Offset in feature data chunck.
        /// </summary>
        public int FeatureDataOffset
        {
            get { return _featureDataOffset; }
            set { _featureDataOffset = value; }
        }

        /// <summary>
        /// Gets or sets Offset in string pool.
        /// </summary>
        public int StringPoolOffset
        {
            get { return _stringPoolOffset; }
            set { _stringPoolOffset = value; }
        }

        /// <summary>
        /// Gets or sets Next item index.
        /// </summary>
        public int NextItemIndex
        {
            get { return _nextItemIndex; }
            set { _nextItemIndex = value; }
        }

        /// <summary>
        /// Gets Feature data item.
        /// </summary>
        public Collection<FeatureDataItem> FeatureItems
        {
            get { return _featureItems; }
        }

        /// <summary>
        /// Gets Size of feature data.
        /// </summary>
        public int FeatureItemBinarySize
        {
            get
            {
                // Total Unit Count ( 4 bytes)
                int size = 4;

                // 2-byte count, 4-byte UnitIndex
                foreach (FeatureDataItem item in _featureItems)
                {
                    size += item.BinarySize;
                }

                return size;
            }
        }

        /// <summary>
        /// Gets Size of word in string pool.
        /// </summary>
        public int StringBinarySize
        {
            get
            {
                int size = 0;

                if (!string.IsNullOrEmpty(_word))
                {
                    size = Encoding.Unicode.GetByteCount(_word);

                    // 2-byte unicode string end
                    size += 2;
                }

                return size;
            }
        }

        #endregion

        #region Public Operations

        /// <summary>
        /// Write feature item into feature data chunck.
        /// </summary>
        /// <param name="bw">Binary writer.</param>
        public void WriteFeatureData(BinaryWriter bw)
        {
            if (bw == null)
            {
                throw new ArgumentNullException("bw");
            }

            if (_featureItems.Count > 0)
            {
                bw.Write((uint)_featureItems.Count);
                foreach (FeatureDataItem item in _featureItems)
                {
                    // Padding (2-byte padding for 4-byte alignment)
                    Debug.Assert(item.UnitIndexes.Count < ushort.MaxValue);
                    bw.Write((ushort)item.UnitIndexes.Count);

                    // UintIndex (uint)
                    foreach (int index in item.UnitIndexes)
                    {
                        bw.Write((uint)index);
                    }
                }
            }
        }

        /// <summary>
        /// Write word text into string pool.
        /// </summary>
        /// <param name="bw">Binary writer.</param>
        public void WriteWordText(BinaryWriter bw)
        {
            if (bw == null)
            {
                throw new ArgumentNullException("bw");
            }

            if (!string.IsNullOrEmpty(_word))
            {
                // Word text
                bw.Write(Encoding.Unicode.GetBytes(_word));

                // Write Unicode string ending
                bw.Write((ushort)0);
            }
        }

        /// <summary>
        /// Write hash table item.
        /// </summary>
        /// <param name="bw">Binary writer.</param>
        public void WriteHashtable(BinaryWriter bw)
        {
            if (bw == null)
            {
                throw new ArgumentNullException("bw");
            }

            // Offset in string pool
            bw.Write((uint)_stringPoolOffset);

            // Offset in feature data chunck
            bw.Write((uint)_featureDataOffset);

            // Next item index in hash table
            bw.Write((int)_nextItemIndex);
        }

        /// <summary>
        /// Load hash table.
        /// </summary>
        /// <param name="data">Hash table bytes.</param>
        /// <param name="offset">Start offset in hash table chunck.</param>
        /// <returns>Size of loaded data.</returns>
        public int LoadHashtable(byte[] data, int offset)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            int size = 0;

            // Offset in string pool
            _stringPoolOffset = BitConverter.ToInt32(data, offset);
            checked
            {
                offset += sizeof(int);
            }

            size += sizeof(int);

            // Offset in feature data chunck
            _featureDataOffset = BitConverter.ToInt32(data, offset);
            checked
            {
                offset += sizeof(int);
            }

            size += sizeof(int);

            // Next item index
            _nextItemIndex = BitConverter.ToInt32(data, offset);
            size += sizeof(int);

            return size;
        }

        /// <summary>
        /// Load feature data.
        /// </summary>
        /// <param name="data">Feature data bytes.</param>
        /// <param name="offset">Start offset in feature data chunck.</param>
        public void LoadFeatureData(byte[] data, int offset)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            uint count = BitConverter.ToUInt32(data, offset);
            checked
            {
                offset += sizeof(uint);
            }

            for (int i = 0; i < count; i++)
            {
                FeatureDataItem item = new FeatureDataItem();

                // Number of Unit Indexes
                ushort unitCount = BitConverter.ToUInt16(data, offset);
                checked
                {
                    offset += sizeof(ushort);
                }

                // UnitIndexes (uint)
                item.UnitIndexes.Clear();
                for (ushort unitIndex = 0; unitIndex < unitCount; unitIndex++)
                {
                    int index = BitConverter.ToInt32(data, offset);
                    item.UnitIndexes.Add(index);
                    checked
                    {
                        offset += sizeof(int);
                    }
                }

                _featureItems.Add(item);
            }
        }

        /// <summary>
        /// Load word text.
        /// </summary>
        /// <param name="data">String pool bytes.</param>
        /// <param name="offset">Start offset in string pool.</param>
        public void LoadWordText(byte[] data, int offset)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            int length = 0;
            for (int pos = offset; pos < data.Length; pos += 2)
            {
                // Find the end of string
                if (data[pos] == 0 && data[pos + 1] == 0)
                {
                    length = pos - offset;
                    break;
                }
            }

            if (length == 0)
            {
                string message = Helper.NeutralFormat("Found malformed data: " +
                    "Empty word text");
                throw new ArgumentNullException(message);
            }

            _word = Encoding.Unicode.GetString(data, offset, length);
        }

        #endregion
    }

    /// <summary>
    /// Domain index file.
    /// </summary>
    [CLSCompliantAttribute(false)]
    public class DomainIndexFile
    {
        #region Fields

        private DomainIndexItem[] _items;
        private Language _language;
        private FontSectionTag _tag = FontSectionTag.Unknown;

        #endregion

        #region Properties

        /// <summary>
        /// Gets Language.
        /// </summary>
        public Language Language
        {
            get { return _language; }
        }

        /// <summary>
        /// Gets Font section tag.
        /// </summary>
        public FontSectionTag Tag
        {
            get { return _tag; }
        }

        #endregion

        #region Public Operations

        /// <summary>
        /// Load domain index file.
        /// </summary>
        /// <param name="filePath">File path.</param>
        public void Load(string filePath)
        {
            FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read,
                FileShare.Read);
            try
            {
                int nLength = (int)fs.Length;
                using (BinaryReader br = new BinaryReader(fs))
                {
                    fs = null;

                    // Load header
                    DomainIndexFileHeaderSerial header = DomainIndexFileHeaderSerial.Read(br);
                    if (header.HashTableItemCount <= 0)
                    {
                        string message = Helper.NeutralFormat("Zero hash table item in file [{0}]",
                            filePath);
                        throw new InvalidDataException(message);
                    }

                    // Verify file size
                    if (nLength != header.Size + 8)
                    {
                        string message = Helper.NeutralFormat("Found malformed data: Expected data size = {0}," +
                            "Real file size = {1}", header.Size + 8, nLength);
                        throw new InvalidDataException(message);
                    }

                    int headerSize = Marshal.SizeOf(typeof(DomainIndexFileHeaderSerial));
                    long expectedFileSize = headerSize + header.HashTableSize + header.FeatureDataSize +
                        header.StringPoolSize;
                    if (nLength != expectedFileSize)
                    {
                        string message = Helper.NeutralFormat("Found malformed data: header size = {0}," +
                            "hash table size = {1}, feature data size = {2}, string pool size = {3}," +
                            "real file size = {4}", headerSize, header.HashTableSize, header.FeatureDataOffset,
                            header.StringPoolSize, nLength);
                        throw new InvalidDataException(message);
                    }

                    _language = (Language)header.LanguageId;
                    _tag = (FontSectionTag)header.Tag;

                    // Load hash table data
                    byte[] hashTableChunck = br.ReadBytes(header.HashTableSize);

                    // Load feature data
                    byte[] featureDataChunck = br.ReadBytes(header.FeatureDataSize);

                    // Load string pool
                    byte[] stringPool = br.ReadBytes(header.StringPoolSize);

                    _items = new DomainIndexItem[header.HashTableItemCount];
                    int offset = 0;

                    for (int i = 0; i < _items.Length; i++)
                    {
                        DomainIndexItem item = new DomainIndexItem();

                        // Load hash table
                        offset += item.LoadHashtable(hashTableChunck, offset);

                        // Load feature data
                        item.LoadFeatureData(featureDataChunck, item.FeatureDataOffset);

                        // Load word text
                        item.LoadWordText(stringPool, item.StringPoolOffset);

                        _items[i] = item;
                    }
                }
            }
            finally
            {
                if (null != fs)
                {
                    fs.Dispose();
                }
            }
        }

        /// <summary>
        /// Save domain index file.
        /// </summary>
        /// <param name="filePath">File path.</param>
        /// <param name="buildNumber">Build number.</param>
        public void Save(string filePath, FontBuildNumber buildNumber)
        {
            // Header size
            int headerSize = Marshal.SizeOf(typeof(DomainIndexFileHeaderSerial));

            // Hash table size
            // One hash table item
            //   1. Offset in string pool (uint)
            //   2. Offset in feature data (uint)
            //   3. Index of next item in hash table (int)
            int hashTableSize = _items.Length * (sizeof(uint) + sizeof(uint) + sizeof(int));

            // Feature data size & string pool size
            int featureDataSize = 0;
            int stringPoolSize = 0;
            for (int i = 0; i < _items.Length; i++)
            {
                Debug.Assert(_items[i] != null);
                featureDataSize += _items[i].FeatureItemBinarySize;
                stringPoolSize += _items[i].StringBinarySize;
            }

            DomainIndexFileHeaderSerial header;

            header.Tag = (uint)_tag;
            header.Size = (uint)(headerSize - 8 + hashTableSize + featureDataSize + stringPoolSize);
            header.VersionNumber = (uint)FormatVersion.Tts30;
            header.BuildNumber = (uint)buildNumber.ToInt32();
            header.LanguageId = (uint)_language;
            header.HashTableItemCount = _items.Length;
            header.HashTableOffset = headerSize;
            header.HashTableSize = hashTableSize;
            header.FeatureDataOffset = header.HashTableOffset + hashTableSize;
            header.FeatureDataSize = featureDataSize;
            header.StringPoolOffset = header.FeatureDataOffset + featureDataSize;
            header.StringPoolSize = stringPoolSize;

            FileStream fs = new FileStream(filePath, FileMode.Create);
            try
            {
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    fs = null;

                    // Write header
                    bw.Write(header.ToBytes());

                    // Write hash table
                    foreach (DomainIndexItem item in _items)
                    {
                        item.WriteHashtable(bw);
                    }

                    // Write feature data
                    foreach (DomainIndexItem item in _items)
                    {
                        item.WriteFeatureData(bw);
                    }

                    // Write string pool
                    foreach (DomainIndexItem item in _items)
                    {
                        item.WriteWordText(bw);
                    }
                }
            }
            finally
            {
                if (null != fs)
                {
                    fs.Dispose();
                }
            }
        }

        /// <summary>
        /// Create domain index file.
        /// </summary>
        /// <param name="scriptFile">Script file.</param>
        /// <param name="domainList">Domain list.</param>
        /// <param name="uif">Name indexed unit features.</param>
        public void Create(XmlScriptFile scriptFile, DomainConfigList domainList, UnitIndexingFile uif)
        {
            // Parameters Validation
            if (scriptFile == null)
            {
                throw new ArgumentNullException("scriptFile");
            }

            if (domainList == null)
            {
                throw new ArgumentNullException("domainList");
            }

            if (uif == null)
            {
                throw new ArgumentNullException("uif");
            }

            Dictionary<string, DomainIndexItem> items =
                new Dictionary<string, DomainIndexItem>(StringComparer.Ordinal);

            _language = scriptFile.Language;
            _tag = domainList.FontTag;
            Phoneme phoneme = Localor.GetPhoneme(_language);
            SliceData sliceData = Localor.GetSliceData(_language);
            foreach (ScriptItem scriptItem in scriptFile.Items)
            {
                if (!domainList.Contains(scriptItem.Id))
                {
                    continue;
                }

                Collection<TtsUnit> itemUnits = scriptItem.GetUnits(phoneme, sliceData);
                Collection<ScriptWord> allPronouncedNormalWords = scriptItem.AllPronouncedNormalWords;
                for (int i = 0; i < allPronouncedNormalWords.Count; i++)
                {
                    ScriptWord word = allPronouncedNormalWords[i];

                    string text;
                    if (domainList.Domain == ScriptDomain.Number)
                    {
                        text = GetNumberDomainWordText(word, scriptItem.Id, i,
                            (domainList as NumberDomainConfigList).Digitals);
                    }
                    else if (domainList.Domain == ScriptDomain.Acronym)
                    {
                        text = GetAcronymDomainWordText(word, scriptItem.Id, i,
                            (domainList as AcronymDomainConfigList).Acronyms);
                    }
                    else if (domainList.Domain == ScriptDomain.Letter)
                    {
                        // Use pronunciation phone ids as key
                        text = GetPhoneIds(word);
                    }
                    else
                    {
                        text = word.Grapheme.ToUpperInvariant();
                    }

                    if (items.ContainsKey(text) &&
                        domainList.Domain != ScriptDomain.Letter)
                    {
                        // Skip duplicate word, except Letter domain
                        continue;
                    }

                    DomainIndexItem item = null;
                    if (!items.ContainsKey(text))
                    {
                        item = new DomainIndexItem();
                        item.Word = text;
                    }
                    else
                    {
                        item = items[text];
                    }

                    bool skipped = false;
                    Collection<TtsUnit> wordUnits = word.GetUnits(phoneme, sliceData);
                    for (int wordUnitIndex = 0; wordUnitIndex < wordUnits.Count; wordUnitIndex++)
                    {
                        TtsUnit unit = wordUnits[wordUnitIndex];
                        FeatureDataItem featureItem = new FeatureDataItem();

                        int indexOfNonSilence = itemUnits.IndexOf(unit);
                        Debug.Assert(indexOfNonSilence >= 0 && indexOfNonSilence < itemUnits.Count);

                        int unitOffset = uif.SearchCandidateOffset(unit.MetaUnit.Name, scriptItem.Id, (uint)indexOfNonSilence);
                        if (unitOffset == -1)
                        {
                            // Skip this word
                            skipped = true;
                            break;
                        }

                        if (item.FeatureItems.Count == wordUnitIndex)
                        {
                            featureItem.UnitIndexes.Add(unitOffset);
                            item.FeatureItems.Add(featureItem); // [].UnitIndexes.Add(unitOffset);
                        }
                        else
                        {
                            item.FeatureItems[wordUnitIndex].UnitIndexes.Add(unitOffset);
                        }
                    }

                    if (!skipped && !items.ContainsKey(item.Word))
                    {
                        items.Add(item.Word, item);
                    }
                }
            }

            _items = BuildHashTable(items.Values);
        }

        #endregion

        #region Private Operations

        /// <summary>
        /// Get pronunciation id string for letter domain.
        /// </summary>
        /// <param name="word">Given ScriptWord.</param>
        /// <returns>Pronunciation id string.</returns>
        private static string GetPhoneIds(ScriptWord word)
        {
            Phoneme phoneme = Localor.GetPhoneme(word.Language);
            string[] phones = word.Pronunciation.Split(new char[] { ' ' },
                StringSplitOptions.RemoveEmptyEntries);

            StringBuilder phoneIds = new StringBuilder();
            foreach (string phone in phones)
            {
                if (phone == TtsUnit.UnitDelimiter)
                {
                    continue;
                }

                if (phoneme.TtsPhoneIds.ContainsKey(phone))
                {
                    phoneIds.Append((char)phoneme.TtsPhoneIds[phone]);
                }
            }

            return phoneIds.ToString();
        }

        /// <summary>
        /// Get word text for number domain.
        /// </summary>
        /// <param name="word">Given ScriptWord.</param>
        /// <param name="sentenceId">Sentence Id.</param>
        /// <param name="wordIndex">Word index.</param>
        /// <param name="digitals">Digital words.</param>
        /// <returns>Word text.</returns>
        private static string GetNumberDomainWordText(ScriptWord word, string sentenceId,
            int wordIndex, Dictionary<string, DigitalWordItem> digitals)
        {
            string text = word.Grapheme.ToUpperInvariant();

            string key = DigitalWordItem.GetKey(sentenceId, wordIndex);
            if (digitals.ContainsKey(key))
            {
                text += "@" + digitals[key].Group.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            else
            {
                if (word.Break > TtsBreak.Word)
                {
                    text += "#3";
                }
                else
                {
                    text += "#1";
                }
            }

            return text;
        }

        /// <summary>
        /// Get word text for acronym domain.
        /// </summary>
        /// <param name="word">Given ScriptWord.</param>
        /// <param name="sentenceId">Sentence Id.</param>
        /// <param name="wordIndex">Word index.</param>
        /// <param name="acronyms">Acronym words.</param>
        /// <returns>Word text.</returns>
        private static string GetAcronymDomainWordText(ScriptWord word, string sentenceId,
            int wordIndex, Dictionary<string, AcronymWordItem> acronyms)
        {
            string key = AcronymWordItem.GetKey(sentenceId, wordIndex);

            string text = acronyms[key].Word.ToUpperInvariant();
            
            text += "@" + acronyms[key].Group.ToString(System.Globalization.CultureInfo.InvariantCulture);

            return text;
        }

        /// <summary>
        /// Build hash table.
        /// </summary>
        /// <param name="items">Domain index items.</param>
        /// <returns>Hash table contains domain index items.</returns>
        private static DomainIndexItem[] BuildHashTable(ICollection<DomainIndexItem> items)
        {
            SortedDictionary<int, Collection<DomainIndexItem>> dict =
                new SortedDictionary<int, Collection<DomainIndexItem>>();

            foreach (DomainIndexItem item in items)
            {
                int hashValue = (int)GetHashValue(item.Word, items.Count);
                Debug.Assert(hashValue >= 0 && hashValue < items.Count);

                if (!dict.ContainsKey(hashValue))
                {
                    dict.Add(hashValue, new Collection<DomainIndexItem>());
                }

                dict[hashValue].Add(item);
            }

            DomainIndexItem[] array = new DomainIndexItem[items.Count];
            foreach (int hashValue in dict.Keys)
            {
                Collection<DomainIndexItem> domainIndexItems = dict[hashValue];
                Debug.Assert(array[hashValue] == null);
                array[hashValue] = domainIndexItems[0];
                domainIndexItems[0].NextItemIndex = -1;
            }

            int pos = 0;
            foreach (int hashValue in dict.Keys)
            {
                Collection<DomainIndexItem> domainIndexItems = dict[hashValue];
                for (int i = 1; i < domainIndexItems.Count; i++)
                {
                    for (; pos < array.Length; pos++)
                    {
                        if (array[pos] == null)
                        {
                            break;
                        }
                    }

                    Debug.Assert(pos < array.Length);
                    array[pos] = domainIndexItems[i];
                    domainIndexItems[i].NextItemIndex = -1;
                    domainIndexItems[i - 1].NextItemIndex = pos;
                }
            }

            int featureDataOffset = 0;
            int stringPoolOffset = 0;

            for (int i = 0; i < array.Length; i++)
            {
                array[i].FeatureDataOffset = featureDataOffset;
                array[i].StringPoolOffset = stringPoolOffset;

                featureDataOffset += array[i].FeatureItemBinarySize;
                stringPoolOffset += array[i].StringBinarySize;
            }

            return array;
        }

        /// <summary>
        /// Get has code as position in hash table.
        /// </summary>
        /// <param name="text">Given word text.</param>
        /// <param name="length">Given hash table length.</param>
        /// <returns>Hash code.</returns>
        private static int GetHashValue(string text, int length)
        {
            int hashValue = 0;
            char previousChar = (char)0;
            char[] chars = text.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                char currentChar = chars[i];
                char upperChar = char.ToUpperInvariant(currentChar);
                hashValue += (upperChar << (previousChar & 0x1F)) + (previousChar << (upperChar & 0x1F));
                previousChar = upperChar;
            }

            return (int)(((uint)((hashValue << 16) - hashValue)) % length);
        }

        #endregion
    }
}